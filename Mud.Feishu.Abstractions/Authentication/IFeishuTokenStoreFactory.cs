// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 飞书令牌存储工厂接口，负责为每个应用创建独立的 <see cref="ITokenStore"/> 实例。
/// </summary>
/// <remarks>
/// S-3 修复：替代 <c>FeishuAppManager.CreateAppContext</c> 中 <c>is FeishuTokenStore</c> 类型检查的脆弱设计。
/// 通过显式工厂接口由 DI 注册决定 per-app vs singleton 策略，遵循"显式依赖优于类型嗅探"原则。
/// </remarks>
public interface IFeishuTokenStoreFactory
{
    /// <summary>
    /// 为指定应用创建令牌存储实例。
    /// </summary>
    /// <param name="appKey">应用唯一标识，用于多应用隔离。</param>
    /// <returns>令牌存储与用户令牌存储的元组。</returns>
    (ITokenStore TokenStore, IUserTokenStore? UserTokenStore) Create(string appKey);
}

/// <summary>
/// 默认的 per-app 令牌存储工厂，为每个应用创建独立的 <see cref="FeishuTokenStore"/> 实例。
/// </summary>
/// <remarks>
/// 适用于单实例部署场景。多应用场景下每个应用拥有独立的内存缓存命名空间。
/// </remarks>
public class PerAppFeishuTokenStoreFactory : IFeishuTokenStoreFactory
{
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// 初始化 <see cref="PerAppFeishuTokenStoreFactory"/> 实例。
    /// </summary>
    /// <param name="memoryCache">内存缓存实例（Singleton，由各 FeishuTokenStore 通过 AppKey 隔离键空间）。</param>
    public PerAppFeishuTokenStoreFactory(IMemoryCache memoryCache)
        => _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

    /// <inheritdoc />
    public (ITokenStore TokenStore, IUserTokenStore? UserTokenStore) Create(string appKey)
    {
        var feishuTokenStore = new FeishuTokenStore(_memoryCache, appKey);
        var userTokenStore = new FeishuUserTokenStore(feishuTokenStore, _memoryCache, appKey);
        return (feishuTokenStore, userTokenStore);
    }
}

/// <summary>
/// 单例令牌存储工厂，返回 DI 容器中已注册的 <see cref="ITokenStore"/> 单例实例。
/// </summary>
/// <remarks>
/// <b>TMA-20 / P2-8 修复：已标记 [Obsolete]。</b>
/// 该工厂忽略 <c>appKey</c>，所有应用共享同一 <c>ITokenStore</c> 实例，
/// 会重现多应用令牌互相覆盖（TOK-1）。请使用 <c>PerAppRedisTokenStoreFactory</c> 或自定义 per-app 工厂。
/// </remarks>
[Obsolete("忽略 appKey，会导致多应用令牌互相覆盖；请使用 PerAppRedisTokenStoreFactory 或自定义 per-app 工厂")]
public class SingletonFeishuTokenStoreFactory : IFeishuTokenStoreFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 初始化 <see cref="SingletonFeishuTokenStoreFactory"/> 实例。
    /// </summary>
    /// <param name="serviceProvider">DI 服务提供者，用于解析已注册的 ITokenStore / IUserTokenStore。</param>
    public SingletonFeishuTokenStoreFactory(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public (ITokenStore TokenStore, IUserTokenStore? UserTokenStore) Create(string appKey)
    {
        var tokenStore = _serviceProvider.GetRequiredService<ITokenStore>();
        var userTokenStore = _serviceProvider.GetService<IUserTokenStore>();
        return (tokenStore, userTokenStore);
    }
}

/// <summary>
/// D10（TMF-01）：按 appKey 全量清除用户令牌的内部能力契约。
/// </summary>
/// <remarks>
/// 组件接口 <c>IUserTokenStore</c> 只有按 <c>(userId, tokenType)</c> 粒度的
/// <c>RemoveAsync</c>/<c>ClearUserAsync</c>，无「跨用户全清」方法；
/// 凭据变更清库（<c>FeishuAppManager.PurgeTokenStoreAsync</c>）需要整体清除该 appKey
/// 的全部用户令牌，本接口以 optional-capability 模式补齐——调用方按能力探测
/// （<c>is IFeishuUserTokenStorePurge</c>），未实现该能力的存储被跳过（租户侧清库不受影响）。
/// 实现方：<see cref="FeishuTokenStore"/> 同目录的 <see cref="FeishuUserTokenStore"/>（记账驱动）、
/// Redis 路径的 <c>RedisUserTokenStore</c>（SCAN 全用户键模式）、
/// 加密装饰器 <c>EncryptedUserTokenStore</c>（透传内层）。
/// </remarks>
internal interface IFeishuUserTokenStorePurge
{
    /// <summary>
    /// 清除当前键前缀（含 appKey 维度）下全部用户的持久化令牌。
    /// </summary>
    /// <remarks>
    /// 与并发写入之间存在固有残余窗口（清库期间新写入的令牌不受本次清库影响），
    /// 与 <see cref="ITokenStore.ClearAsync"/> 的 SCAN/快照语义一致。
    /// </remarks>
    /// <param name="cancellationToken">取消令牌</param>
    Task ClearAllUsersAsync(CancellationToken cancellationToken = default);
}
