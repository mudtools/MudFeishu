// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 飞书令牌管理器工厂接口，负责为每个应用创建独立的 TenantTokenManager / AppTokenManager / UserTokenManager 实例。
/// </summary>
/// <remarks>
/// MA-02 修复：替代 <see cref="FeishuAppManager.CreateAppContext"/> 中直接 <c>new TenantTokenManager(...)</c> 的硬编码方式，
/// 使自定义 TokenManager 实现可通过注册自定义工厂接入 DI 容器，提升扩展性。
/// 默认实现 <see cref="DefaultFeishuTokenManagerFactory"/> 保持与原行为完全一致。
/// </remarks>
public interface IFeishuTokenManagerFactory
{
    /// <summary>
    /// 为指定应用创建令牌管理器三元组。
    /// </summary>
    /// <param name="config">应用配置</param>
    /// <param name="authenticationApi">飞书认证 API 实例</param>
    /// <param name="tokenStore">令牌存储（由 <see cref="IFeishuTokenStoreFactory"/> 创建）</param>
    /// <param name="userTokenStore">用户令牌存储（可为 null）</param>
    /// <returns>令牌管理器三元组（Tenant、App、User）</returns>
    (ITenantTokenManager TenantTokenManager, IAppTokenManager AppTokenManager, IFeishuUserTokenManager UserTokenManager) Create(
        FeishuAppConfig config,
        IFeishuAuthentication authenticationApi,
        ITokenStore tokenStore,
        IUserTokenStore? userTokenStore);
}

/// <summary>
/// 默认的飞书令牌管理器工厂，直接创建 <see cref="TenantTokenManager"/> / <see cref="AppTokenManager"/> / <see cref="UserTokenManager"/> 实例。
/// </summary>
/// <remarks>
/// 行为与 MA-02 修复前的 <see cref="FeishuAppManager.CreateAppContext"/> 完全一致，仅将创建逻辑提取到独立工厂。
/// 自定义实现可替换此工厂以注入自定义 TokenManager 类型。
/// </remarks>
public class DefaultFeishuTokenManagerFactory : IFeishuTokenManagerFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 初始化 <see cref="DefaultFeishuTokenManagerFactory"/> 实例。
    /// </summary>
    /// <param name="serviceProvider">DI 服务提供者，用于解析 ILogger 等依赖。</param>
    public DefaultFeishuTokenManagerFactory(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public (ITenantTokenManager TenantTokenManager, IAppTokenManager AppTokenManager, IFeishuUserTokenManager UserTokenManager) Create(
        FeishuAppConfig config,
        IFeishuAuthentication authenticationApi,
        ITokenStore tokenStore,
        IUserTokenStore? userTokenStore)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        if (authenticationApi == null)
            throw new ArgumentNullException(nameof(authenticationApi));
        if (tokenStore == null)
            throw new ArgumentNullException(nameof(tokenStore));

        var options = Options.Create(config);
        var currentUserContext = _serviceProvider.GetService<IFeishuCurrentUserContext>();

        var tenantTokenManager = new TenantTokenManager(
            authenticationApi,
            options,
            _serviceProvider.GetRequiredService<ILogger<TenantTokenManager>>(),
            tokenStore);

        var appTokenManager = new AppTokenManager(
            authenticationApi,
            options,
            _serviceProvider.GetRequiredService<ILogger<AppTokenManager>>(),
            tokenStore);

        var userTokenManager = new UserTokenManager(
            currentUserContext,
            authenticationApi,
            options,
            _serviceProvider.GetRequiredService<ILogger<UserTokenManager>>(),
            userTokenStore);

        return (tenantTokenManager, appTokenManager, userTokenManager);
    }
}
