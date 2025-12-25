// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using System.Net;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 飞书服务建造者，用于按需注册飞书相关服务
/// </summary>
public class FeishuServiceBuilder
{
    private readonly IServiceCollection _services;
    private readonly FeishuServiceConfiguration _configuration = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="services">服务集合</param>
    internal FeishuServiceBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// 从配置文件读取配置
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder ConfigureFrom(IConfiguration configuration, string sectionName = "Feishu")
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _services.Configure<FeishuOptions>(options =>
        {
            options.AppId = configuration[$"{sectionName}:AppId"];
            options.AppSecret = configuration[$"{sectionName}:AppSecret"];
        });

        _configuration.IsConfigured = true;
        return this;
    }

    /// <summary>
    /// 使用代码配置
    /// </summary>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder ConfigureOptions(Action<FeishuOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        _services.Configure(configureOptions);
        _configuration.IsConfigured = true;
        return this;
    }

    /// <summary>
    /// 添加飞书HttpClient注册代码。
    /// </summary>
    /// <returns></returns>
    public FeishuServiceBuilder AddFeishuHttpClient()
    {
        if (_configuration.IsFeishuHttpClient) return this;

        _services.AddHttpClient<IEnhancedHttpClient, FeishuHttpClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<FeishuOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl ?? "https://open.feishu.cn");
            client.DefaultRequestHeaders.Add("User-Agent", "MudFeishuClient/1.0");
            int timeOut = 60;
            if (!string.IsNullOrEmpty(options.TimeOut) && int.TryParse(options.TimeOut, out int t))
                timeOut = t;
            client.Timeout = TimeSpan.FromSeconds(timeOut);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        }).AddTransientHttpErrorPolicy(policyBuilder =>
               policyBuilder.WaitAndRetryAsync(3, retryAttempt =>
               TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
        return this;
    }

    /// <summary>
    /// 添加令牌管理服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddTokenManagers()
    {
        if (!_configuration.TokenManagersAdded)
        {
            _services
                .AddSingleton<ITenantTokenManager, TenantTokenManager>()
                .AddSingleton<IAppTokenManager, AppTokenManager>()
                .AddSingleton<IUserTokenManager, UserTokenManager>();

            _configuration.TokenManagersAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加租户令牌管理服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddTenantTokenManager()
    {
        if (!_configuration.TenantTokenManagerAdded)
        {
            _services.AddSingleton<ITenantTokenManager, TenantTokenManager>();
            _configuration.TenantTokenManagerAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加应用令牌管理服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddAppTokenManager()
    {
        if (!_configuration.AppTokenManagerAdded)
        {
            _services.AddSingleton<IAppTokenManager, AppTokenManager>();
            _configuration.AppTokenManagerAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加用户令牌管理服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddUserTokenManager()
    {
        if (!_configuration.UserTokenManagerAdded)
        {
            _services.AddSingleton<IUserTokenManager, UserTokenManager>();
            _configuration.UserTokenManagerAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加组织管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddOrganizationApi()
    {
        if (!_configuration.OrganizationApiAdded)
        {
            AddFeishuHttpClient();
            AddTokenManagers(); // Organization API 通常需要令牌管理
            _services.AddOrganizationWebApiHttpClient();
            _configuration.OrganizationApiAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加消息管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddMessageApi()
    {
        if (!_configuration.MessageApiAdded)
        {
            AddFeishuHttpClient();
            AddTokenManagers(); // Message API 通常需要令牌管理
            _services.AddMessageWebApiHttpClient();
            _configuration.MessageApiAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加群聊管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddChatGroupApi()
    {
        if (!_configuration.ChatGroupApiAdded)
        {
            AddFeishuHttpClient();
            AddTokenManagers(); // ChatGroup API 通常需要令牌管理
            _services.AddChatGroupWebApiHttpClient();
            _configuration.ChatGroupApiAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加任务管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddTaskApi()
    {
        if (!_configuration.TaskApiAdded)
        {
            AddFeishuHttpClient();
            AddTokenManagers();
            _services.AddTaskWebApiHttpClient();
            _configuration.TaskApiAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加卡片管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddCardApi()
    {
        if (!_configuration.CardApiAdded)
        {
            AddFeishuHttpClient();
            AddTokenManagers();
            _services.AddCardsWebApiHttpClient();
            _configuration.CardApiAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加所有 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddAllApis()
    {
        return AddFeishuHttpClient()
               .AddTokenManagers()
               .AddOrganizationApi()
               .AddMessageApi()
               .AddChatGroupApi()
               .AddTaskApi()
               .AddCardApi();
    }

    /// <summary>
    /// 添加核心令牌管理服务（不包含具体的 API 客户端）
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddCoreServices()
    {
        return AddFeishuHttpClient().
               AddTokenManagers();
    }

    /// <summary>
    /// 添加认证 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddAuthenticationApi()
    {
        if (!_configuration.AuthenticationApiAdded)
        {
            _services.AddTransient<IFeishuV3AuthenticationApi, FeishuV3AuthenticationApi>();
            _configuration.AuthenticationApiAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 根据功能模块添加服务
    /// </summary>
    /// <param name="modules">功能模块</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddModules(params FeishuModule[] modules)
    {
        foreach (var module in modules)
        {
            switch (module)
            {
                case FeishuModule.TokenManagement:
                    AddTokenManagers();
                    break;
                case FeishuModule.Organization:
                    AddOrganizationApi();
                    break;
                case FeishuModule.Message:
                    AddMessageApi();
                    break;
                case FeishuModule.ChatGroup:
                    AddChatGroupApi();
                    break;
                case FeishuModule.Authentication:
                    AddAuthenticationApi();
                    break;
                case FeishuModule.All:
                    AddAllApis();
                    break;
            }
        }
        return this;
    }

    /// <summary>
    /// 构建服务注册
    /// </summary>
    /// <returns>服务集合，支持链式调用</returns>
    public IServiceCollection Build()
    {
        // 验证配置
        if (!_configuration.IsConfigured)
        {
            throw new InvalidOperationException("必须先配置 FeishuOptions，请使用 ConfigureFrom 或 ConfigureOptions 方法。");
        }

        // 验证至少添加了一个服务
        if (!_configuration.HasAnyService())
        {
            throw new InvalidOperationException("至少需要添加一个服务，请使用相应的 Add 方法。");
        }

        // 添加配置验证
        _services.AddOptions<FeishuOptions>()
                .Validate(options => ValidateFeishuOptionsInternal(options),
                    "飞书服务需要在配置文件中正确配置 AppId 和 AppSecret。")
                .ValidateOnStart();

        return _services;
    }

    /// <summary>
    /// 内部验证飞书选项的方法
    /// </summary>
    /// <param name="options">飞书选项</param>
    /// <returns>验证结果</returns>
    private static bool ValidateFeishuOptionsInternal(FeishuOptions options) =>
        !string.IsNullOrEmpty(options.AppId) && !string.IsNullOrEmpty(options.AppSecret);
}

/// <summary>
/// 飞书功能模块枚举
/// </summary>
public enum FeishuModule
{
    /// <summary>
    /// 令牌管理
    /// </summary>
    TokenManagement,

    /// <summary>
    /// 组织管理
    /// </summary>
    Organization,

    /// <summary>
    /// 消息管理
    /// </summary>
    Message,

    /// <summary>
    /// 群聊管理
    /// </summary>
    ChatGroup,

    /// <summary>
    /// 认证服务
    /// </summary>
    Authentication,

    /// <summary>
    /// 所有功能
    /// </summary>
    All
}

/// <summary>
/// 飞书服务配置内部状态
/// </summary>
internal class FeishuServiceConfiguration
{
    public bool IsConfigured { get; set; }
    public bool IsFeishuHttpClient { get; set; }
    public bool TokenManagersAdded { get; set; }
    public bool TenantTokenManagerAdded { get; set; }
    public bool AppTokenManagerAdded { get; set; }
    public bool UserTokenManagerAdded { get; set; }
    public bool OrganizationApiAdded { get; set; }
    public bool MessageApiAdded { get; set; }
    public bool ChatGroupApiAdded { get; set; }
    public bool CardApiAdded { get; set; }
    public bool TaskApiAdded { get; set; }
    public bool AuthenticationApiAdded { get; set; }

    /// <summary>
    /// 检查是否添加了任何服务
    /// </summary>
    /// <returns>是否添加了服务</returns>
    public bool HasAnyService()
    {
        return TokenManagersAdded ||
               TenantTokenManagerAdded ||
               AppTokenManagerAdded ||
               UserTokenManagerAdded ||
               OrganizationApiAdded ||
               MessageApiAdded ||
               ChatGroupApiAdded ||
               AuthenticationApiAdded;
    }
}