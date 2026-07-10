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
/// 适用于分布式部署场景（如 Redis）。自定义存储实现（如 <c>RedisTokenStore</c>）通过 DI 注册后，
/// 由本工厂统一返回单例实例，避免 per-app 创建导致的连接池膨胀。
/// </remarks>
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
