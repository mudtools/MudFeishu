// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 基于 IMemoryCache 的飞书令牌存储适配器
/// </summary>
/// <remarks>
/// 实现 Mud.HttpUtils v2.0 的 ITokenStore 接口，将令牌持久化到 IMemoryCache。
/// 适用于单实例部署场景，应用重启后令牌会丢失。
/// 对于多实例分布式部署，应使用 RedisTokenStore 替代。
/// </remarks>
public class FeishuTokenStore : ITokenStore
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _tokenTypes = new();
    private readonly string _appKey;

    /// <summary>
    /// 提前刷新缓冲比例（默认 0.9，即令牌有效期的 90%）。<br/>
    /// M-3 修复：直接使用飞书返回的 expires_in 作为绝对过期时间，在过期边界的高并发请求会同时触发刷新。
    /// 引入缓冲后，缓存提前 10% 过期，使 TokenRecoveryDelegatingHandler 或后台刷新服务有充足时间获取新令牌。
    /// </summary>
    private const double EarlyRefreshRatio = 0.9;

    /// <summary>
    /// 初始化 FeishuTokenStore 实例（使用默认 AppKey）
    /// </summary>
    /// <param name="cache">内存缓存实例</param>
    /// <remarks>
    /// 此构造函数用于 DI 容器单例注册场景（单应用模式或向后兼容）。<br/>
    /// 多应用场景下应使用 <see cref="FeishuTokenStore(IMemoryCache, string)"/> 构造函数传入 AppKey。
    /// </remarks>
    public FeishuTokenStore(IMemoryCache cache)
        : this(cache, "default")
    {
    }

    /// <summary>
    /// 初始化 FeishuTokenStore 实例（指定 AppKey 用于多应用隔离）
    /// </summary>
    /// <param name="cache">内存缓存实例</param>
    /// <param name="appKey">应用唯一标识，用于构建隔离的缓存键</param>
    /// <exception cref="ArgumentNullException">当 cache 为 null 或 appKey 为空时抛出</exception>
    /// <remarks>
    /// C-2 修复：多应用场景下，不同应用共享同一个 IMemoryCache（Singleton），
    /// 若缓存键不包含 AppKey 维度，后注册应用的令牌会覆盖先注册应用的令牌，导致鉴权失败或越权访问。
    /// </remarks>
    public FeishuTokenStore(IMemoryCache cache, string appKey)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("AppKey 不能为空", nameof(appKey));
        _appKey = appKey;
    }

    /// <inheritdoc />
    public Task<string?> GetAccessTokenAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        var key = BuildAccessTokenKey(tokenType);
        var token = _cache.Get<string>(key);
        return Task.FromResult(token);
    }

    /// <inheritdoc />
    public Task SetAccessTokenAsync(string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
    {
        // 输入验证：保留原始 API 契约，对非法 expiry 值（0 或负数）抛出 ArgumentOutOfRangeException。
        // 注意：M-3 修复引入的 Math.Max(1, ...) 仅用于处理"小正数经 EarlyRefreshRatio 计算后向下取整为 0"的边界场景，
        // 不应吞掉对非法输入的校验。
        if (expiresInSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(expiresInSeconds), expiresInSeconds, "过期时间必须为正数（秒）");

        _tokenTypes.TryAdd(tokenType, 0);
        var key = BuildAccessTokenKey(tokenType);
        // M-3 修复：使用提前刷新缓冲，实际缓存时间 = expiresInSeconds * EarlyRefreshRatio
        // Math.Max(1, ...) 防止 expiresInSeconds=1 时 (long)(1 * 0.9)=0 导致 IMemoryCache 抛 ArgumentOutOfRangeException
        var bufferedExpiry = TimeSpan.FromSeconds(Math.Max(1, (long)(expiresInSeconds * EarlyRefreshRatio)));
        _cache.Set(key, accessToken, bufferedExpiry);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetRefreshTokenAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        var key = BuildRefreshTokenKey(tokenType);
        var token = _cache.Get<string>(key);
        return Task.FromResult(token);
    }

    /// <inheritdoc />
    public Task SetRefreshTokenAsync(string tokenType, string refreshToken, CancellationToken cancellationToken = default)
    {
        var key = BuildRefreshTokenKey(tokenType);
        _cache.Set(key, refreshToken, TimeSpan.FromDays(30));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        _tokenTypes.TryRemove(tokenType, out _);
        _cache.Remove(BuildAccessTokenKey(tokenType));
        _cache.Remove(BuildRefreshTokenKey(tokenType));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetTokenTypesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tokenTypes.Keys.AsEnumerable());
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        foreach (var tokenType in _tokenTypes.Keys)
        {
            _cache.Remove(BuildAccessTokenKey(tokenType));
            _cache.Remove(BuildRefreshTokenKey(tokenType));
        }

        _tokenTypes.Clear();
        return Task.CompletedTask;
    }

    // C-2 修复：缓存键增加 AppKey 维度，确保多应用场景下令牌互不覆盖
    private string BuildAccessTokenKey(string tokenType) => $"feishu:{_appKey}:token:{tokenType}:access";
    private string BuildRefreshTokenKey(string tokenType) => $"feishu:{_appKey}:token:{tokenType}:refresh";
}

