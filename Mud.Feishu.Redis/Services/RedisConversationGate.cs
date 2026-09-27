// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Conversations;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// Redis 分布式会话闸门（AI-FD-D12 P2D-1 多实例实现）：<c>SET key token NX PX ttl</c> 租约——
/// 同键跨实例互斥；获取失败经有限次退避重试后<b>快速失败</b>（抛
/// <see cref="ConversationBusyException"/>），把节奏还给事件层幂等回滚 + 重投递，不静默排队。
/// </summary>
/// <remarks>
/// <para>
/// 依赖方向（纵向引用治理）：契约 <see cref="IConversationGate"/> 位于 Mud.Feishu.Abstractions，
/// 本包只依赖 Abstractions——包间不允许横向引用。
/// </para>
/// <para>
/// 租约 TTL 单一阈值源为 <see cref="FeishuConversationOptions.SessionTtl"/>（与 Redis 会话存储同源，
/// D9 阈值同源精神）：正常路径经比较删除（Lua CAS，只删自己的租约）即时放行；持有实例崩溃时
/// 租约至多存活 SessionTtl 后自动过期，期间同键新事件快速失败并由事件层重投递。
/// </para>
/// <para>
/// 键布局：最终存储键 = <see cref="ConversationKeyBuilder.ComposeNamespace"/>(gatePrefix, 会话键)，
/// 会话键必须由 <see cref="ConversationKeyBuilder"/> 构造——闸门不自行拼接键（D8 精神）；
/// 闸门前缀默认 <c>feishu:conversation:gate</c>，与会话存储键空间隔离。
/// </para>
/// </remarks>
public sealed class RedisConversationGate : IConversationGate
{
    /// <summary>比较并删除脚本：只释放自己持有的租约（防 TTL 过期后误删他人租约）。</summary>
    private const string CompareAndDeleteScript =
        "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end";

    /// <summary>获取失败的退避重试序列（毫秒；确定性倍增，有限次后快速失败）。</summary>
    private static readonly int[] RetryDelaysMs = [50, 100, 200];

    private const string DefaultGatePrefix = "feishu:conversation:gate";

    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _leaseTtl;
    private readonly string _gatePrefix;
    private readonly ILogger<RedisConversationGate> _logger;

    /// <summary>
    /// 初始化 Redis 会话闸门。
    /// </summary>
    /// <param name="redis">Redis 连接复用器。</param>
    /// <param name="options">会话配置（<see cref="FeishuConversationOptions.SessionTtl"/> 租约 TTL 单一阈值源）。</param>
    /// <param name="logger">日志记录器（可空，兜底 NullLogger）。</param>
    /// <param name="gatePrefix">闸门键命名空间前缀（多环境共用 Redis 时隔离键空间）。</param>
    public RedisConversationGate(
        IConnectionMultiplexer redis,
        IOptions<FeishuConversationOptions> options,
        ILogger<RedisConversationGate>? logger = null,
        string gatePrefix = DefaultGatePrefix)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _leaseTtl = (options ?? throw new ArgumentNullException(nameof(options))).Value.SessionTtl;
        if (_leaseTtl <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "会话租约 TTL 必须为正数（SessionTtl）");

        _logger = logger ?? NullLogger<RedisConversationGate>.Instance;
        _gatePrefix = string.IsNullOrWhiteSpace(gatePrefix) ? DefaultGatePrefix : gatePrefix!;
    }

    /// <inheritdoc />
    public async Task<IConversationGateHandle> AcquireAsync(string conversationKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationKey))
            throw new ArgumentException("会话键不能为空", nameof(conversationKey));

        var leaseKey = ConversationKeyBuilder.ComposeNamespace(_gatePrefix, conversationKey!);
        var database = _redis.GetDatabase();
        var token = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var acquired = await database.StringSetAsync(
                leaseKey, token, _leaseTtl, When.NotExists, CommandFlags.None).ConfigureAwait(false);
            if (acquired)
            {
                return new LeaseHandle(database, leaseKey, token, _logger);
            }

            if (attempt >= RetryDelaysMs.Length)
            {
                throw new ConversationBusyException(
                    conversationKey!,
                    $"会话闸门被占用（key: {leaseKey}）——同键事件正在其他实例处理，经 {RetryDelaysMs.Length + 1} 次尝试后快速失败，由事件层重投递承接");
            }

            await Task.Delay(RetryDelaysMs[attempt], cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class LeaseHandle : IConversationGateHandle
    {
        private readonly IDatabase _database;
        private readonly string _leaseKey;
        private readonly string _token;
        private readonly ILogger _logger;
        private int _released;

        public LeaseHandle(IDatabase database, string leaseKey, string token, ILogger logger)
        {
            _database = database;
            _leaseKey = leaseKey;
            _token = token;
            _logger = logger;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0)
            {
                return; // 幂等：finally + 显式双路径不重复放行。
            }

            // 同步 Dispose 契约下的受控后台释放；异常全部吞入日志（租约经 TTL 兜底过期）。
            _ = ReleaseLeaseAsync();
        }

        private async Task ReleaseLeaseAsync()
        {
            try
            {
                var released = (int)(await _database.ScriptEvaluateAsync(
                    CompareAndDeleteScript,
                    [(RedisKey)_leaseKey],
                    [(RedisValue)_token]).ConfigureAwait(false));
                if (released == 0)
                {
                    _logger.LogWarning("会话闸门租约释放落空（key: {LeaseKey}）——租约已过期或易主，由 TTL 兜底", _leaseKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "会话闸门租约释放失败（key: {LeaseKey}）——由 TTL 兜底", _leaseKey);
            }
        }
    }
}
