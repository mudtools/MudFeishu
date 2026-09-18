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
    private readonly string _appKey;

    // TMF-01：跨实例共享记账（D10）。静态注册表按 KeyPrefix 隔离：
    // 任意实例 SetAccessTokenAsync 均注册到共享表，ClearAsync 由此可删除
    // 「任意实例曾写入」的全部键——工厂每次 Create 新实例不再导致清库 no-op。
    // 原子性由 GetOrAdd/TryAdd 保证（注册永不丢失）；重复/过期注册只导致对
    // 不存在键的无害 no-op Remove（如并行测试使用不同 IMemoryCache 的场景）。
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> SharedTenantTypes = new();

    private ConcurrentDictionary<string, byte> SharedTypes
        => SharedTenantTypes.GetOrAdd(KeyPrefix, _ => new ConcurrentDictionary<string, byte>());

    /// <summary>
    /// TMA-10 / P2-1 修复：缓存 TTL 改为全量 expiresInSeconds（不再提前 10% 过期）。
    /// 原 EarlyRefreshRatio=0.9 导致缓存失效点 = Expire - 10% = Expire - 720s（飞书 token 7200s），
    /// 而恢复弃用阈值 = TokenRefreshThreshold（默认 300s），
    /// 此刻 store 中令牌剩余 约 720s > 300s 但缓存已失效，导致稳态 store 恢复永不命中。
    /// 改为全量后，缓存失效点 = Expire，此刻 store 中令牌剩余 约 0 &lt; restoreThreshold，正确弃用。
    /// </summary>
    private const double CacheTtlRatio = 1.0;

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

        SharedTypes.TryAdd(tokenType, 0);
        var key = BuildAccessTokenKey(tokenType);
        // TMA-10：缓存 TTL 使用全量 expiresInSeconds（不再提前 10% 过期）。
        var bufferedExpiry = TimeSpan.FromSeconds(Math.Max(1, (long)(expiresInSeconds * CacheTtlRatio)));
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
    /// <remarks>
    /// TMA-22 修复：refresh token 存储 TTL 由硬编码 30 天改为优先使用编码值中的过期时间，
    /// 缺失时才回落 30 天。但 IMemoryCache 场景下此方法不接收过期信息，保持 30 天默认。
    /// Redis 路径在 RedisTokenStore 中处理。
    /// TMA2-15 / P2-5：Memory 路径保留 30 天默认。仅本进程；过期语义由消费侧校验
    /// （TMA2-06 的 RefreshTokenExpireTime 校验）。
    /// </remarks>
    public Task SetRefreshTokenAsync(string tokenType, string refreshToken, CancellationToken cancellationToken = default)
    {
        // TMF-01（验收修复）：refresh 写入同样必须记账。若仅写 refresh 而从未写 access
        // （如 PersistUserTokenAsync 在 access 剩余秒数 ≤ 0 时只持久化 refresh），
        // 该 tokenType 不在共享记账中 → ClearAsync 无法删除其 refresh 键（清库盲区）。
        SharedTypes.TryAdd(tokenType, 0);
        var key = BuildRefreshTokenKey(tokenType);
        // TMA-22: 保持 30 天默认（IMemoryCache 路径无过期信息可用）。
        _cache.Set(key, refreshToken, TimeSpan.FromDays(30));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        SharedTypes.TryRemove(tokenType, out _);
        _cache.Remove(BuildAccessTokenKey(tokenType));
        _cache.Remove(BuildRefreshTokenKey(tokenType));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// TMA-16 / P2-3 修复：仅返回本进程已知类型。
    /// TMF-01：记账改为按 KeyPrefix 的跨实例共享表，语义从「本实例已知」放宽为
    /// 「本进程本前缀已知」——实例被重建（如配置热更新）后，旧实例写入的 tokenType
    /// 记账仍然可见，从而保证凭据变更清库（D10）的完整性。
    /// </remarks>
    public Task<IEnumerable<string>> GetTokenTypesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SharedTypes.Keys.AsEnumerable());
    }

    /// <inheritdoc />
    /// <remarks>
    /// TMF-01（D10）：清除「任意实例曾写入本 KeyPrefix」的全部租户令牌键。
    /// 先对共享记账做键快照再逐键删除，最后仅清空表内容（保留外层条目，
    /// 避免并发实例持有的孤儿字典）。与并发写入之间存在固有 TOCTOU 残余窗口
    /// （清库后写入的新令牌不受本次清库影响），与 Redis SCAN 语义一致。
    /// </remarks>
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        foreach (var tokenType in SharedTypes.Keys)
        {
            _cache.Remove(BuildAccessTokenKey(tokenType));
            _cache.Remove(BuildRefreshTokenKey(tokenType));
        }

        SharedTypes.Clear();
        return Task.CompletedTask;
    }

    // TMA2-02 / D8：令牌键构造收敛到 TokenKeyBuilder 统一产出。
    // 键前缀含 AppKey 维度，确保多应用场景下令牌互不覆盖。
    private string KeyPrefix => $"feishu:{_appKey}:token";
    private string BuildAccessTokenKey(string tokenType) => TokenKeyBuilder.TenantAccessKey(KeyPrefix, tokenType);
    private string BuildRefreshTokenKey(string tokenType) => TokenKeyBuilder.TenantRefreshKey(KeyPrefix, tokenType);
}

