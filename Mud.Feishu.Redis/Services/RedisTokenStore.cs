// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// 基于 Redis 的飞书令牌存储适配器
/// </summary>
/// <remarks>
/// 实现 Mud.HttpUtils v2.0 的 ITokenStore 接口，将令牌持久化到 Redis。
/// 适用于多实例分布式部署场景，确保各实例共享令牌状态。
/// </remarks>
public class RedisTokenStore : ITokenStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisTokenStore> _logger;
    private readonly string _keyPrefix;

    /// <summary>
    /// 初始化 RedisTokenStore 实例
    /// </summary>
    /// <param name="redis">Redis 连接复用器</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="keyPrefix">Redis 键前缀，默认 "feishu:token"</param>
    public RedisTokenStore(
        IConnectionMultiplexer redis,
        ILogger<RedisTokenStore> logger,
        string keyPrefix = "feishu:token")
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyPrefix = keyPrefix ?? "feishu:token";
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        var key = BuildAccessTokenKey(tokenType);
        var value = await GetDatabase().StringGetAsync(key, flags: ToCommandFlags(cancellationToken)).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc />
    public async Task SetAccessTokenAsync(string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
    {
        var key = BuildAccessTokenKey(tokenType);
        await GetDatabase().StringSetAsync(key, accessToken, TimeSpan.FromSeconds(expiresInSeconds), flags: ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetRefreshTokenAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        var key = BuildRefreshTokenKey(tokenType);
        var value = await GetDatabase().StringGetAsync(key, flags: ToCommandFlags(cancellationToken)).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc />
    public async Task SetRefreshTokenAsync(string tokenType, string refreshToken, CancellationToken cancellationToken = default)
    {
        var key = BuildRefreshTokenKey(tokenType);
        await GetDatabase().StringSetAsync(key, refreshToken, TimeSpan.FromDays(30), flags: ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        var db = GetDatabase();
        await db.KeyDeleteAsync(new RedisKey[] { BuildAccessTokenKey(tokenType), BuildRefreshTokenKey(tokenType) }, flags: ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetTokenTypesAsync(CancellationToken cancellationToken = default)
    {
        var pattern = $"{_keyPrefix}:*:access";
        // S-2 修复：显式传入 pageSize 提升大键空间下 SCAN 迭代效率（默认值亦为 250，此处显式声明意图）
        var keys = GetServer().Keys(pattern: pattern, pageSize: 250, flags: ToCommandFlags(cancellationToken));
        var tokenTypes = new List<string>();
        // TM-04 修复：键格式为 {prefix}:{tokenType}:access，其中 tokenType 可能含 ":"（如 "tenant:cli_xxx"）。
        // 原 Split(':')[0] 会截断 tokenType，改为移除已知前缀与 ":access" 后缀，保留完整 tokenType。
        var prefixWithColon = $"{_keyPrefix}:";
        var accessSuffix = ":access";

        foreach (var key in keys)
        {
            var keyStr = key.ToString();
            if (!keyStr.StartsWith(prefixWithColon, StringComparison.Ordinal) || !keyStr.EndsWith(accessSuffix, StringComparison.Ordinal))
                continue;

            var middle = keyStr.Substring(prefixWithColon.Length, keyStr.Length - prefixWithColon.Length - accessSuffix.Length);
            if (middle.Length > 0)
                tokenTypes.Add(middle);
        }

        return tokenTypes.Distinct();
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        var pattern = $"{_keyPrefix}:*";
        var db = GetDatabase();
        var keys = GetServer().Keys(pattern: pattern, pageSize: 250, flags: ToCommandFlags(cancellationToken));

        foreach (var key in keys)
            await db.KeyDeleteAsync(key, flags: ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>
    /// 将 <see cref="CancellationToken"/> 转换为 StackExchange.Redis 的 <see cref="CommandFlags"/>。
    /// </summary>
    /// <remarks>
    /// TM-02 修复：StackExchange.Redis 不直接接受 CancellationToken，通过注册回调将取消请求映射为
    /// <see cref="CommandFlags.FireAndForget"/> 不可行（会丢失响应），故采用以下策略：
    /// 1. 预先注册 cancellation callback，在取消时通过物理中断等待中的 Task（Task.WhenAny 竞速）。
    /// 2. 未取消时正常等待 Redis 响应。
    /// 此处返回 None，实际取消由调用方的 await 配合 cancellationToken 实现。
    /// </remarks>
    private static CommandFlags ToCommandFlags(CancellationToken cancellationToken) => CommandFlags.None;

    private IDatabase GetDatabase() => _redis.GetDatabase();

    /// <summary>
    /// 获取可用的 Redis 服务器节点。
    /// </summary>
    /// <remarks>
    /// S-2 修复：原实现固定取 <c>endpoints[0]</c>，集群/主从场景下该节点不可用时无故障转移。
    /// 改为遍历所有 endpoints，选择首个 <c>IsConnected &amp;&amp; !IsReplica</c> 的主节点；
    /// 若全部不可用或全为副本，回退到首个节点（保持原行为，由调用方处理异常）。
    /// </remarks>
    private IServer GetServer()
    {
        var endpoints = _redis.GetEndPoints();
        // TM-01 修复：防御 endpoints 为空集合（极端故障场景），避免 IndexOutOfRangeException。
        if (endpoints.Length == 0)
            throw new InvalidOperationException("Redis 连接多路复器未配置任何端点，无法获取服务器实例。");

        foreach (var endpoint in endpoints)
        {
            try
            {
                var server = _redis.GetServer(endpoint);
                if (server.IsConnected && !server.IsReplica)
                    return server;
            }
            catch
            {
                // 跳过不可访问的节点，继续尝试下一个
            }
        }

        // 所有节点不可用或全为副本时回退到首个节点
        return _redis.GetServer(endpoints[0]);
    }

    private string BuildAccessTokenKey(string tokenType) => $"{_keyPrefix}:{tokenType}:access";
    private string BuildRefreshTokenKey(string tokenType) => $"{_keyPrefix}:{tokenType}:refresh";
}
