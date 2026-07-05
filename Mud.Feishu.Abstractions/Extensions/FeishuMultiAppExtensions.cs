// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 飞书多应用服务注册扩展
/// </summary>
/// <remarks>
/// 提供多飞书应用支持的服务注册扩展方法。
/// 支持从配置文件加载或通过代码配置两种方式。
/// </remarks>
public static class FeishuMultiAppExtensions
{
    /// <summary>
    /// 注册飞书多应用支持（从配置文件加载）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"FeishuApps"</param>
    /// <returns>服务集合实例，支持链式调用</returns>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当配置无效时抛出</exception>
    /// <remarks>
    /// 从配置文件加载飞书应用配置。
    /// 配置示例：
    /// <code>
    /// {
    ///   "FeishuApps": [
    ///     {
    ///       "AppKey": "default",
    ///       "AppId": "cli_xxx",
    ///       "AppSecret": "dsk_xxx",
    ///       "IsDefault": true
    ///     },
    ///     {
    ///       "AppKey": "hr-app",
    ///       "AppId": "cli_yyy",
    ///       "AppSecret": "dsk_yyy"
    ///     }
    ///   ]
    /// }
    /// </code>
    /// </remarks>
    public static IServiceCollection AddFeishuApp(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "FeishuApps")
    {
        return services.AddFeishuApp(
             validateConfig: null,
             configuration: configuration,
             sectionName: sectionName);
    }

    /// <summary>
    /// 注册飞书多应用支持（从配置文件加载）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"FeishuApps"</param>
    /// <param name="validateConfig">配置验证委托</param>
    /// <returns>服务集合实例，支持链式调用</returns>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当配置无效时抛出</exception>
    /// <remarks>
    /// 从配置文件加载飞书应用配置。
    /// 配置示例：
    /// <code>
    /// {
    ///   "FeishuApps": [
    ///     {
    ///       "AppKey": "default",
    ///       "AppId": "cli_xxx",
    ///       "AppSecret": "dsk_xxx",
    ///       "IsDefault": true
    ///     },
    ///     {
    ///       "AppKey": "hr-app",
    ///       "AppId": "cli_yyy",
    ///       "AppSecret": "dsk_yyy"
    ///     }
    ///   ]
    /// }
    /// </code>
    /// </remarks>
    public static IServiceCollection AddFeishuApp(
        this IServiceCollection services,
        Action<List<FeishuAppConfig>>? validateConfig,
        IConfiguration configuration,

        string sectionName = "FeishuApps")
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // 检测是否已注册单应用模式的TokenManager,给出警告
        DetectAndWarnSingleAppRegistration(services);

        // 先加载配置
        var configs = new List<FeishuAppConfig>();
        var section = configuration.GetSection(sectionName);
        section.Bind(configs);

        // 验证并设置默认应用
        ValidateAndSetDefaultApp(configs);

        validateConfig?.Invoke(configs);

        // 注册基础服务（HttpClient工厂）
        services.AddFeishuAppBaseServices(configs);

        // 注册核心服务（应用管理器、默认应用上下文、配置）
        RegisterCoreServices(services, configs);
        return services;
    }

    /// <summary>
    /// 检测并警告单应用模式注册
    /// </summary>
    private static void DetectAndWarnSingleAppRegistration(IServiceCollection services)
    {
        var hasTenantTokenManager = services.Any(s =>
            s.ServiceType == typeof(ITenantTokenManager) ||
            s.ServiceType == typeof(IAppTokenManager) ||
            s.ServiceType == typeof(IUserTokenManager));

        if (hasTenantTokenManager)
        {
            // 注意：此处无法使用 ILogger，因为服务容器尚未构建完成
            // 使用 Debug 输出作为 ConfigureServices 阶段的日志手段
            // 仅在 Debug 模式下可见，生产环境建议在应用启动后检查日志确认配置正确
            System.Diagnostics.Debug.WriteLine(
                "[MudFeishu] 检测到已注册单应用模式的TokenManager。多应用模式已启用,单应用模式的TokenManager将被忽略。" +
                "建议移除 AddTokenManagers() 等单应用API的调用。" +
                "请参考文档: https://github.com/mudtools/MudFeishu/wiki/Multi-App-Migration");
        }
    }

    /// <summary>
    /// 注册飞书多应用支持（使用代码配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">配置委托</param>
    /// <returns>服务集合实例，支持链式调用</returns>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当配置无效时抛出</exception>
    /// <remarks>
    /// 通过代码方式配置飞书应用。
    /// 配置示例：
    /// <code>
    /// services.AddFeishuApp(configure =>
    /// {
    ///     config.AddDefaultApp("default", "cli_xxx", "dsk_xxx");
    ///     config.AddApp("hr-app", "cli_yyy", "dsk_yyy", opt =>
    ///     {
    ///         opt.TimeOut = 45;
    ///         opt.RetryCount = 5;
    ///     });
    /// });
    /// </code>
    /// </remarks>
    public static IServiceCollection AddFeishuApp(
        this IServiceCollection services,
        Action<FeishuAppConfigBuilder> configure)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var builder = new FeishuAppConfigBuilder();
        configure(builder);
        var configs = builder.Build();

        // 验证并设置默认应用（必须在 AddFeishuAppBaseServices 之前调用）
        // 原因：AddFeishuAppBaseServices 内部读取 config.IsDefault 决定 AddMudHttpClient 的 setAsDefault 参数。
        // 若在 IsDefault 未确定时调用，所有 HttpClient 会以 setAsDefault:false 注册，导致默认 IEnhancedHttpClient 未绑定。
        ValidateAndSetDefaultApp(configs);

        // 注册基础服务（HttpClient工厂）
        services.AddFeishuAppBaseServices(configs);

        // 注册核心服务（应用管理器、默认应用上下文、配置）
        RegisterCoreServices(services, configs);

        return services;
    }

    /// <summary>
    /// 注册飞书多应用支持（使用预构建的配置列表）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configs">应用配置列表</param>
    /// <returns>服务集合实例，支持链式调用</returns>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当配置无效时抛出</exception>
    /// <remarks>
    /// 使用已构建好的配置列表进行注册。
    /// 配置示例：
    /// <code>
    /// var configs = new List&lt;FeishuAppConfig&gt;
    /// {
    ///     new FeishuAppConfig { AppKey = "default", AppId = "cli_xxx", AppSecret = "dsk_xxx", IsDefault = true },
    ///     new FeishuAppConfig { AppKey = "hr-app", AppId = "cli_yyy", AppSecret = "dsk_yyy" }
    /// };
    /// services.AddFeishuApp(configs);
    /// </code>
    /// </remarks>
    public static IServiceCollection AddFeishuApp(
        this IServiceCollection services,
        List<FeishuAppConfig> configs)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configs == null)
            throw new ArgumentNullException(nameof(configs));

        // 验证并设置默认应用（必须在 AddFeishuAppBaseServices 之前调用）
        // 原因：AddFeishuAppBaseServices 内部读取 config.IsDefault 决定 AddMudHttpClient 的 setAsDefault 参数。
        // 若在 IsDefault 未确定时调用，所有 HttpClient 会以 setAsDefault:false 注册，导致默认 IEnhancedHttpClient 未绑定。
        ValidateAndSetDefaultApp(configs);

        // 注册基础服务（HttpClient工厂）
        services.AddFeishuAppBaseServices(configs);

        // 注册核心服务（应用管理器、默认应用上下文、配置）
        RegisterCoreServices(services, configs);

        return services;
    }

    /// <summary>
    /// 注册核心服务（应用管理器、默认应用上下文、配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configs">应用配置列表</param>
    private static void RegisterCoreServices(IServiceCollection services, List<FeishuAppConfig> configs)
    {
        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        services.AddSingleton(sp =>
        {
            var appManager = sp.GetRequiredService<IFeishuAppManager>();
            return appManager.GetDefaultApp();
        });

        services.AddSingleton(configs);
        services.Configure<List<FeishuAppConfig>>(options =>
        {
            options.Clear();
            options.AddRange(configs);
        });
    }

    /// <summary>
    /// 验证并设置默认应用
    /// </summary>
    /// <param name="configs">应用配置列表</param>
    /// <exception cref="InvalidOperationException">当配置无效时抛出</exception>
    private static void ValidateAndSetDefaultApp(List<FeishuAppConfig> configs)
    {
        if (configs.Count == 0)
            throw new InvalidOperationException("至少需要配置一个飞书应用");

        // 检查是否有重复的AppKey
        var duplicateAppKeys = configs.GroupBy(c => c.AppKey, StringComparer.OrdinalIgnoreCase)
                                       .Where(g => g.Count() > 1)
                                       .Select(g => g.Key)
                                       .ToList();
        if (duplicateAppKeys.Any())
            throw new InvalidOperationException($"检测到重复的AppKey: {string.Join(", ", duplicateAppKeys)}");

        // 验证所有配置（包含自动推断逻辑）
        foreach (var config in configs)
        {
            config.Validate();
        }

        // 如果没有指定默认应用，设置第一个为默认
        // 注意：这里需要重新检查，因为 Validate() 中可能已经自动推断
        var defaultAppCount = configs.Count(c => c.IsDefault);
        if (defaultAppCount == 0)
        {
            configs[0].IsDefault = true;
        }
        else if (defaultAppCount > 1)
        {
            var defaultAppKeys = configs.Where(c => c.IsDefault).Select(c => c.AppKey);
            throw new InvalidOperationException(
                $"检测到多个 IsDefault=true 的应用（{string.Join(", ", defaultAppKeys)}），仅允许一个默认应用。请确保只有一个应用标记为 IsDefault=true。");
        }

        // 多应用模式下，弹性策略不一致的警告由 FeishuAppManager.WarnResilienceConfigMismatch 统一发出（运行时，使用 ILogger）。
        // v1.1 修复 P1：此前此处使用 Console.Error.WriteLine 发出重复警告，与 FeishuAppManager 的 _logger.LogWarning 重复触发。
        // 已移除此处的 Console.Error.WriteLine 警告，保留 FeishuAppManager 中更全面的运行时日志警告（覆盖所有弹性参数）。
    }
}
