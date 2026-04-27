// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.TokenManager;

namespace Mud.Feishu.Abstractions;


/// <summary>
/// 飞书应用上下文
/// </summary>
/// <remarks>
/// 封装单个飞书应用的所有资源和配置，包括：
/// - 应用配置信息
/// - 各种类型的令牌管理器（租户令牌、应用令牌、用户令牌）
/// - 认证API客户端
/// - HTTP客户端
/// 
/// 每个应用上下文是完全独立的，不同应用之间的配置、缓存和资源互不干扰。
/// 令牌缓存由 Mud.HttpUtils v2.0 的 TokenManagerBase 内部管理。
/// </remarks>
public class FeishuAppContext : IFeishuAppContext, IMudAppContext, IDisposable
{
    /// <summary>
    /// HTTP客户端
    /// </summary>
    /// <remarks>
    /// 用于发送HTTP请求到飞书API的客户端实例。
    /// 每个应用拥有独立的HTTP客户端实例。
    /// </remarks>
    public IEnhancedHttpClient HttpClient { get; private set; }


    /// <summary>
    /// 根据令牌类型获取对应的令牌管理器
    /// </summary>
    /// <param name="tokenType">令牌类型</param>
    /// <returns></returns>
    public ITokenManager GetTokenManager(string tokenType)
    {
        if (string.IsNullOrEmpty(tokenType))
            return TenantTokenManager; // 默认返回租户令牌管理器
        tokenType = tokenType.Trim().ToLower();
        return tokenType switch
        {
            "tenantaccesstoken" => TenantTokenManager,
            "appaccesstoken" => AppTokenManager,
            "useraccesstoken" => UserTokenManager,
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType), $"Unsupported token type: {tokenType}")
        };
    }

    /// <summary>
    /// 获取指定类型的令牌管理器（泛型版本）
    /// </summary>
    /// <typeparam name="T">令牌管理器类型</typeparam>
    /// <returns>指定类型的令牌管理器实例</returns>
    public T GetTokenManager<T>() where T : class, ITokenManager
    {
        if (typeof(T) == typeof(ITenantTokenManager) || typeof(T) == typeof(TenantTokenManager))
            return TenantTokenManager as T ?? throw new InvalidOperationException($"Cannot cast TenantTokenManager to {typeof(T).Name}");
        if (typeof(T) == typeof(IAppTokenManager) || typeof(T) == typeof(AppTokenManager))
            return AppTokenManager as T ?? throw new InvalidOperationException($"Cannot cast AppTokenManager to {typeof(T).Name}");
        if (typeof(T) == typeof(IFeishuUserTokenManager) || typeof(T) == typeof(UserTokenManager))
            return UserTokenManager as T ?? throw new InvalidOperationException($"Cannot cast UserTokenManager to {typeof(T).Name}");

        throw new InvalidOperationException($"Unsupported token manager type: {typeof(T).Name}");
    }

    /// <summary>
    /// 从应用上下文中获取指定类型的服务实例
    /// </summary>
    /// <typeparam name="T">要获取的服务类型</typeparam>
    /// <returns>指定类型的服务实例；如果服务未注册则返回 null</returns>
    public T? GetService<T>() where T : class
    {
        if (typeof(T) == typeof(IFeishuAuthentication))
            return Authentication as T;
        if (typeof(T) == typeof(IEnhancedHttpClient))
            return HttpClient as T;
        if (typeof(T) == typeof(ITenantTokenManager))
            return TenantTokenManager as T;
        if (typeof(T) == typeof(IAppTokenManager))
            return AppTokenManager as T;
        if (typeof(T) == typeof(IFeishuUserTokenManager))
            return UserTokenManager as T;

        return null;
    }

    /// <summary>
    /// 应用配置
    /// </summary>
    /// <remarks>
    /// 包含此应用的所有配置信息，如AppId、AppSecret、BaseUrl等。
    /// </remarks>
    public FeishuAppConfig Config { get; }

    /// <summary>
    /// 租户令牌管理器
    /// </summary>
    /// <remarks>
    /// 用于获取和管理租户访问令牌（Tenant Access Token）。
    /// 租户令牌用于租户级别的权限验证。
    /// </remarks>
    public ITenantTokenManager TenantTokenManager { get; }

    /// <summary>
    /// 应用令牌管理器
    /// </summary>
    /// <remarks>
    /// 用于获取和管理应用身份访问令牌（App Access Token）。
    /// 应用令牌用于应用级别的权限验证。
    /// </remarks>
    public IAppTokenManager AppTokenManager { get; }

    /// <summary>
    /// 用户令牌管理器
    /// </summary>
    /// <remarks>
    /// 用于获取和管理用户访问令牌（User Access Token）。
    /// 用户令牌通过OAuth授权流程获取，需要用户授权。
    /// </remarks>
    public IFeishuUserTokenManager UserTokenManager { get; }

    /// <summary>
    /// 认证API客户端
    /// </summary>
    /// <remarks>
    /// 用于调用飞书认证相关API的服务接口。
    /// </remarks>
    public IFeishuAuthentication Authentication { get; }

    /// <summary>
    /// 初始化飞书应用上下文
    /// </summary>
    /// <param name="config">应用配置</param>
    /// <param name="tenantTokenManager">租户令牌管理器</param>
    /// <param name="appTokenManager">应用令牌管理器</param>
    /// <param name="userTokenManager">用户令牌管理器</param>
    /// <param name="authenticationApi">认证API客户端</param>
    /// <param name="httpClient">HTTP客户端</param>
    /// <exception cref="ArgumentNullException">当任何必需参数为null时抛出</exception>
    public FeishuAppContext(
        FeishuAppConfig config,
        ITenantTokenManager tenantTokenManager,
        IAppTokenManager appTokenManager,
        IFeishuUserTokenManager userTokenManager,
        IFeishuAuthentication authenticationApi,
        IEnhancedHttpClient httpClient)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        TenantTokenManager = tenantTokenManager ?? throw new ArgumentNullException(nameof(tenantTokenManager));
        AppTokenManager = appTokenManager ?? throw new ArgumentNullException(nameof(appTokenManager));
        UserTokenManager = userTokenManager ?? throw new ArgumentNullException(nameof(userTokenManager));
        Authentication = authenticationApi ?? throw new ArgumentNullException(nameof(authenticationApi));
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <remarks>
    /// 清理应用上下文占用的资源，主要是令牌管理器。
    /// </remarks>
    public void Dispose()
    {
        if (TenantTokenManager is IDisposable disposableTenant)
        {
            disposableTenant.Dispose();
        }
        if (AppTokenManager is IDisposable disposableApp)
        {
            disposableApp.Dispose();
        }
        if (UserTokenManager is IDisposable disposableUser)
        {
            disposableUser.Dispose();
        }
    }

    /// <summary>
    /// 返回应用上下文的字符串表示
    /// </summary>
    /// <returns>应用上下文字符串</returns>
    public override string ToString()
    {
        return $"FeishuAppContext {{ AppKey: {Config.AppKey}, AppId: {Config.AppId} }}";
    }
}
