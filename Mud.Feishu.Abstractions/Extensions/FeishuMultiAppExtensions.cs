// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mud.Feishu.Abstractions;
using Mud.HttpUtils;

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
        RegisterCoreServices(services, configs, configuration, sectionName);
        return services;
    }

    /// <summary>
    /// 检测并警告单应用模式注册
    /// </summary>
    /// <remarks>
    /// 检测到单应用模式的 TokenManager 注册时发出警告。
    /// 注意：由于桥接注册使用 <c>TryAddSingleton</c>（已存在则跳过），
    /// 单应用模式的注册实际上会优先生效，而非被忽略。
    /// 此警告提醒用户存在配置冲突，建议统一使用多应用模式。
    /// </remarks>
    private static void DetectAndWarnSingleAppRegistration(IServiceCollection services)
    {
        var hasTenantTokenManager = services.Any(s =>
            s.ServiceType == typeof(ITenantTokenManager) ||
            s.ServiceType == typeof(IAppTokenManager) ||
            s.ServiceType == typeof(IUserTokenManager));

        if (hasTenantTokenManager)
        {
            // m-1 修复：使用 Console.Error 替代 Debug.WriteLine，确保 Release 编译模式下警告可见。
            // ConfigureServices 阶段无法使用 ILogger（服务容器尚未构建），Console.Error 是唯一可靠的输出渠道。
            Console.Error.WriteLine(
                "[MudFeishu] 检测到已注册单应用模式的TokenManager。多应用模式已启用，由于使用 TryAddSingleton 语义，" +
                "单应用模式的TokenManager将优先生效（而非被忽略），可能导致多应用模式下默认应用令牌管理器桥接注册被跳过。" +
                "建议移除 AddTokenManagers() 等单应用API的调用，统一使用多应用模式。" +
                "如需按应用键获取令牌管理器，请使用 IFeishuTokenManagerResolver。" +
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

        // 委托给 List<FeishuAppConfig> 重载，统一执行验证 + 基础服务注册 + 核心服务注册流程
        return services.AddFeishuApp(configs);
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
    /// 注册飞书多应用支持，使用自定义的 <see cref="FeishuAppManager"/> 实现类型。
    /// </summary>
    /// <typeparam name="TAppManager">自定义应用管理器类型，必须继承 <see cref="FeishuAppManager"/>。</typeparam>
    /// <param name="services">服务集合。</param>
    /// <param name="configs">应用配置列表。</param>
    /// <returns>服务集合实例，支持链式调用。</returns>
    /// <exception cref="ArgumentNullException">当参数为 null 时抛出。</exception>
    /// <remarks>
    /// M-2 修复：允许用户继承 <see cref="FeishuAppManager"/> 并覆盖 <c>CreateAppContext</c> 方法，
    /// 以支持自定义 <see cref="IFeishuAppContext"/> 实现（如添加审计日志、动态配置热更新等）。
    /// <code>
    /// services.AddFeishuApp&lt;CustomFeishuAppManager&gt;(configs);
    /// </code>
    /// </remarks>
    public static IServiceCollection AddFeishuApp<TAppManager>(
        this IServiceCollection services,
        List<FeishuAppConfig> configs)
        where TAppManager : FeishuAppManager
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configs == null)
            throw new ArgumentNullException(nameof(configs));

        ValidateAndSetDefaultApp(configs);
        services.AddFeishuAppBaseServices(configs);

        // 注册自定义 AppManager 类型（覆盖默认的 FeishuAppManager 注册）
        services.AddSingleton<IFeishuAppManager>(sp =>
            (TAppManager)ActivatorUtilities.CreateInstance(sp, typeof(TAppManager), configs, sp.GetRequiredService<ILogger<TAppManager>>()));

        // 注册默认应用上下文、配置、令牌管理器解析器、桥接注册等
        RegisterCoreServicesWithoutAppManager(services, configs);

        return services;
    }

    /// <summary>
    /// 注册核心服务（应用管理器、令牌管理器解析器、默认应用上下文、配置、令牌刷新注册、桥接注册）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configs">应用配置列表</param>
    /// <param name="configuration">可选的 IConfiguration 实例，用于绑定 IOptionsMonitor 支持</param>
    /// <param name="sectionName">配置节名称，当提供 configuration 时使用</param>
    /// <remarks>
    /// 注册内容包括：
    /// <list type="bullet">
    ///   <item><see cref="IFeishuAppManager"/> - 飞书应用管理器（单例）</item>
    ///   <item><see cref="IFeishuTokenManagerResolver"/> - 令牌管理器解析器（推荐的多应用令牌访问方式）</item>
    ///   <item><see cref="IFeishuAppContext"/> - 默认应用上下文</item>
    ///   <item>桥接注册 - 将默认应用的令牌管理器暴露到 DI（向后兼容，仅默认应用）</item>
    ///   <item>FeishuTokenRegistrationService - 令牌注册托管服务（NET6+，启动时注册令牌到后台刷新服务）</item>
    /// </list>
    /// </remarks>
    private static void RegisterCoreServices(IServiceCollection services, List<FeishuAppConfig> configs, IConfiguration? configuration = null, string? sectionName = null)
    {
#if NET6_0_OR_GREATER
        // NET6+：使用 FeishuTokenRegistrationService（IHostedService）在应用启动时注册令牌
        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));
#else
        // netstandard2.0：没有 IHostedService 生命周期，在 IFeishuAppManager 工厂中注册令牌。
        // REG-03 修复：原实现调用 GetAllApps() 强制创建所有应用，与 NET6+ 的懒加载策略不一致。
        // 改为仅在首次访问默认应用时注册其令牌管理器（通过 GetDefaultApp 触发懒加载），
        // 其余应用的令牌管理器在运行时按需注册（调用方通过 GetApp 触发懒加载后再注册到刷新服务）。
        // 注意：不能添加第二个 IFeishuAppManager 注册（会导致 GetRequiredService<IFeishuAppManager> 无限递归）
        services.AddSingleton<IFeishuAppManager>(sp =>
        {
            var appManager = new FeishuAppManager(
                sp,
                configs,
                sp.GetRequiredService<ILogger<FeishuAppManager>>());

            var refreshService = sp.GetService<ITokenRefreshBackgroundService>();
            if (refreshService != null)
            {
                // REG-03 修复：仅注册默认应用的令牌管理器（触发默认应用懒加载），
                // 其余应用在运行时按需注册，保持与 NET6+ 懒加载策略一致。
                try
                {
                    var defaultApp = appManager.GetDefaultApp();
                    refreshService.RegisterTokenManager(defaultApp.TenantTokenManager, $"tenant:{defaultApp.Config.AppKey}");
                    refreshService.RegisterTokenManager(defaultApp.AppTokenManager, $"app:{defaultApp.Config.AppKey}");
                }
                catch (InvalidOperationException)
                {
                    // 默认应用尚未配置或创建失败时忽略，令牌将退化为懒加载刷新
                }
            }
            else
            {
                // M-7 修复：netstandard2.0 路径下 ITokenRefreshBackgroundService 未注册时发出警告。
                Console.Error.WriteLine(
                    "[MudFeishu] 警告：ITokenRefreshBackgroundService 未注册，令牌将不会进行后台主动刷新。" +
                    "请在 AddFeishuApp 之前调用 AddTokenRefreshBackgroundService() 注册后台刷新服务，" +
                    "或确保 AddFeishuAppBaseServices 中的 AddTokenRefreshBackgroundService() 调用未被跳过。");
            }

            return appManager;
        });
#endif

        RegisterCoreServicesWithoutAppManager(services, configs, configuration, sectionName);
    }

    /// <summary>
    /// 注册核心服务中除 IFeishuAppManager 之外的依赖项。
    /// 供 <see cref="RegisterCoreServices"/> 和 <see cref="AddFeishuApp{TAppManager}"/> 共用。
    /// </summary>
    private static void RegisterCoreServicesWithoutAppManager(IServiceCollection services, List<FeishuAppConfig> configs, IConfiguration? configuration = null, string? sectionName = null)
    {
        services.AddSingleton(sp =>
        {
            var appManager = sp.GetRequiredService<IFeishuAppManager>();
            return appManager.GetDefaultApp();
        });

        // 注册启动时验证过的配置快照（供内部组件使用，如 HttpClient 注册）
        services.AddSingleton(configs);

        // 注册 IOptions/IOptionsMonitor 绑定链路：
        // - 当提供 IConfiguration 时，使用配置节绑定以支持热更新（IOptionsMonitor<List<FeishuAppConfig>>）
        // - 当未提供 IConfiguration（如代码配置模式）时，回退到闭包绑定
        if (configuration != null && sectionName != null)
        {
            services.Configure<List<FeishuAppConfig>>(configuration.GetSection(sectionName));
        }
        else
        {
            services.Configure<List<FeishuAppConfig>>(options =>
            {
                options.Clear();
                options.AddRange(configs);
            });
        }

        // PostConfigure：统一执行 IsDefault 自动推断逻辑，确保 IOptions<T> 和 IOptionsMonitor<T> 的值与启动快照行为一致
        services.PostConfigure<List<FeishuAppConfig>>(options =>
        {
            // AppKey 为 "default" 时自动设置 IsDefault=true
            foreach (var config in options)
            {
                if (config.AppKey.Equals("default", StringComparison.OrdinalIgnoreCase))
                {
                    config.IsDefault = true;
                }
            }

            // 如果没有指定默认应用，设置第一个为默认
            if (options.Count > 0 && !options.Any(c => c.IsDefault))
            {
                options[0].IsDefault = true;
            }
        });

        // 注册令牌管理器解析器（多应用模式下获取令牌管理器的推荐方式）
        services.TryAddSingleton<IFeishuTokenManagerResolver, FeishuTokenManagerResolver>();

        // SR-P0-2 修复：FeishuTokenRegistrationService 已移至 AddFeishuAppBaseServices 中注册，
        // 确保在 TokenRefreshBackgroundService 之前启动，避免误报"未注册任何令牌管理器"警告。

        // 桥接注册（向后兼容）：将默认应用的令牌管理器暴露到 DI 容器。
        // 注意：仅暴露默认应用的令牌管理器，非默认应用请使用 IFeishuTokenManagerResolver。
        // 使用 TryAddSingleton 确保不覆盖应用层的自定义注册。
        services.TryAddSingleton<ITenantTokenManager>(sp =>
            sp.GetRequiredService<IFeishuTokenManagerResolver>().GetTenantTokenManager());
        services.TryAddSingleton<IAppTokenManager>(sp =>
            sp.GetRequiredService<IFeishuTokenManagerResolver>().GetAppTokenManager());
        services.TryAddSingleton<IFeishuUserTokenManager>(sp =>
            sp.GetRequiredService<IFeishuTokenManagerResolver>().GetUserTokenManager());
        services.TryAddSingleton<IUserTokenManager>(sp =>
            sp.GetRequiredService<IFeishuUserTokenManager>());
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

        // 验证所有配置
        foreach (var config in configs)
        {
            config.Validate();
        }

        // 自动推断默认应用：AppKey 为 "default" 时自动设置 IsDefault=true
        foreach (var config in configs)
        {
            if (config.AppKey.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                config.IsDefault = true;
            }
        }

        // 如果没有指定默认应用，设置第一个为默认
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

        // 多应用模式下，弹性策略不一致的信息由 FeishuAppManager.WarnResilienceConfigMismatch 统一发出（运行时，使用 ILogger）。
        // v1.1 修复 P1：此前此处使用 Console.Error.WriteLine 发出重复警告，与 FeishuAppManager 的日志重复触发。
        // 已移除此处的 Console.Error.WriteLine 警告，保留 FeishuAppManager 中更全面的运行时日志（覆盖所有弹性参数）。
        // P1-4 修复：Per-App 弹性策略已启用，不同应用的配置不再被忽略，日志级别从 Warning 降为 Information。
    }
}
