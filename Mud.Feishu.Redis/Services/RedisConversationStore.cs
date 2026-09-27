// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions.Conversations;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// Redis 会话存储：<see cref="IConversationStore"/> 的分布式实现。
/// </summary>
/// <remarks>
/// <para>
/// 依赖方向（纵向引用治理）：Mud.Feishu.Redis → Mud.Feishu.Abstractions——会话存储契约
/// （<see cref="IConversationStore"/> 等）位于 Abstractions，包间不允许横向引用。
/// 载荷为 MAF <c>SerializeSessionAsync</c> 产出的会话 JSON，经
/// <see cref="SessionStoreEncoding"/> 编码为 <c>{expireTimestampMs}|{payload}</c>——
/// 与令牌存储（<c>TokenStoreHelper</c>）同一惯例；<b>无有效过期时间戳视为 miss</b>（TMA-15），
/// 读取侧时间戳为有效性权威，服务器键 TTL 仅作清理兜底（双写防提前逐出）。
/// </para>
/// <para>
/// 键布局：最终存储键 = <see cref="ConversationKeyBuilder.ComposeNamespace"/>(keyPrefix, 会话键)，
/// 会话键必须由 <see cref="ConversationKeyBuilder"/> 构造（D8 会话版）。
/// </para>
/// </remarks>
public sealed class RedisConversationStore : IConversationStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisConversationStore> _logger;
    private readonly string _keyPrefix;
    private readonly TimeSpan _ttl;

    /// <summary>
    /// 初始化 Redis 会话存储。
    /// </summary>
    /// <param name="redis">Redis 连接复用器。</param>
    /// <param name="ttl">会话 TTL（须为正；与 <c>FeishuAgentOptions.SessionTtl</c> 同源单一阈值）。</param>
    /// <param name="logger">日志记录器（可空，兜底 NullLogger，R-25）。</param>
    /// <param name="keyPrefix">Redis 命名空间前缀（多环境共用 Redis 时隔离键空间）。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ttl"/> 非正。</exception>
    public RedisConversationStore(
        IConnectionMultiplexer redis,
        TimeSpan ttl,
        ILogger<RedisConversationStore>? logger = null,
        string keyPrefix = "feishu:conversation")
    {
        if (redis is null)
            throw new ArgumentNullException(nameof(redis));
        if (ttl <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ttl), "会话 TTL 必须为正数");

        _redis = redis;
        _ttl = ttl;
        _logger = logger ?? NullLogger<RedisConversationStore>.Instance;
        _keyPrefix = string.IsNullOrWhiteSpace(keyPrefix) ? "feishu:conversation" : keyPrefix!;
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = _redis.GetDatabase();
        var stored = await database.StringGetAsync(BuildKey(key), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
        if (!stored.HasValue)
            return null;

        var value = stored.ToString();
        if (!SessionStoreEncoding.TryDecode(value, out var payload, out var expireAtMs))
        {
            // 旧格式/损坏数据：按 miss 丢弃（TMA-15），交由服务器 TTL 自然清理。
            _logger.LogDebug("会话存储值无有效过期时间戳，视为 miss：{KeyPrefix}:{Key}", _keyPrefix, key);
            return null;
        }

        if (expireAtMs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            return null;

        return payload;
    }

    /// <inheritdoc />
    public async Task SaveAsync(string key, string serializedSession, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(serializedSession))
            throw new ArgumentException("会话载荷不能为空", nameof(serializedSession));

        var expireAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (long)_ttl.TotalMilliseconds;
        var stored = SessionStoreEncoding.Encode(serializedSession, expireAtMs);

        var database = _redis.GetDatabase();
        // 服务器键 TTL 与载荷时间戳双写：过期后服务端自动清理，读侧时间戳仍兜底校验。
        // 显式 5 参重载（SE.Redis 3.x 存在多组 StringSetAsync 重载，避免绑定歧义）。
        await database.StringSetAsync(
            BuildKey(key),
            stored,
            _ttl,
            When.Always,
            RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = _redis.GetDatabase();
        await database.KeyDeleteAsync(BuildKey(key), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    private string BuildKey(string key)
        => ConversationKeyBuilder.ComposeNamespace(_keyPrefix, key);
}
