// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Test;

/// <summary>
/// 飞书服务建造者模式使用示例
/// </summary>
public static class FeishuBuilderExamples
{
    /// <summary>
    /// 示例1：基本建造者模式 - 从配置文件
    /// </summary>
    public static void Example1_BasicBuilderWithConfiguration(IServiceCollection services, IConfiguration configuration)
    {
        // 基本建造者模式
        services.AddFeishuServices(configuration)
            .AddTokenManagers()
            .AddOrganizationApi()
            .AddMessageApi()
            .Build();
    }

    /// <summary>
    /// 示例2：代码配置方式
    /// </summary>
    public static void Example2_CodeConfiguration(IServiceCollection services)
    {
        // 代码配置方式
        services.AddFeishuServices(options =>
        {
            options.AppId = "your_app_id";
            options.AppSecret = "your_app_secret";
        })
        .AddTokenManagers()
        .AddMessageApi()
        .Build();
    }

    /// <summary>
    /// 示例3：模块化注册
    /// </summary>
    public static void Example3_ModularRegistration(IServiceCollection services, IConfiguration configuration)
    {
        // 只注册需要的服务模块
        services.AddFeishuServices(configuration)
            .AddTenantTokenManager()
            .AddAppTokenManager()
            .AddOrganizationApi()
            .Build();
    }

    /// <summary>
    /// 示例4：快速注册方法
    /// </summary>
    public static void Example4_QuickRegistration(IServiceCollection services, IConfiguration configuration)
    {
        // 快速注册组织管理服务
        services.AddFeishuOrganizationApi(configuration);

        // 或者快速注册所有服务
        // services.AddFeishuAllServices(configuration);
    }

    /// <summary>
    /// 示例5：模块化枚举注册
    /// </summary>
    public static void Example5_EnumModuleRegistration(IServiceCollection services, IConfiguration configuration)
    {
        // 使用枚举指定模块
        services.AddFeishuModules(configuration, new[]
        {
            FeishuModule.TokenManagement,
            FeishuModule.Organization,
            FeishuModule.Message
        });
    }

    /// <summary>
    /// 示例6：不同应用场景
    /// </summary>
    public static class Example6_ApplicationScenarios
    {
        /// <summary>
        /// 场景A：用户认证应用
        /// </summary>
        public static void UserAuthenticationApp(IServiceCollection services, IConfiguration configuration)
        {
            services.AddFeishuServices(configuration)
                .AddUserTokenManager()
                .AddAuthenticationApi()
                .Build();
        }

        /// <summary>
        /// 场景B：企业组织管理应用
        /// </summary>
        public static void EnterpriseManagementApp(IServiceCollection services, IConfiguration configuration)
        {
            services.AddFeishuServices(configuration)
                .AddTokenManagers()
                .AddOrganizationApi()
                .Build();
        }

        /// <summary>
        /// 场景C：消息机器人应用
        /// </summary>
        public static void MessageBotApp(IServiceCollection services, IConfiguration configuration)
        {
            services.AddFeishuServices(configuration)
                .AddTokenManagers()
                .AddMessageApi()
                .AddChatGroupApi()
                .Build();
        }

        /// <summary>
        /// 场景D：完整SaaS平台
        /// </summary>
        public static void FullSaaSPlatform(IServiceCollection services, IConfiguration configuration)
        {
            services.AddFeishuServices(configuration)
                .AddAllApis()
                .Build();
        }
    }

    /// <summary>
    /// 示例7：多环境配置
    /// </summary>
    public static void Example7_MultiEnvironmentConfiguration(IServiceCollection services, IConfiguration configuration)
    {
        // 根据环境变量或其他条件进行配置
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        if (environment == "Development")
        {
            services.AddFeishuServices(configuration, "Feishu:Development")
                .AddTokenManagers()
                .AddOrganizationApi()
                .Build();
        }
        else if (environment == "Production")
        {
            services.AddFeishuServices(configuration, "Feishu:Production")
                .AddAllApis()
                .Build();
        }
        else
        {
            // 默认配置
            services.AddFeishuServices(configuration)
                .AddTokenManagers()
                .AddMessageApi()
                .Build();
        }
    }

    /// <summary>
    /// 示例8：条件性注册
    /// </summary>
    public static void Example8_ConditionalRegistration(IServiceCollection services, IConfiguration configuration, bool enableChatGroups, bool enableOrganization)
    {
        var builder = services.AddFeishuServices(configuration)
            .AddTokenManagers()
            .AddMessageApi();

        if (enableChatGroups)
        {
            builder.AddChatGroupApi();
        }

        if (enableOrganization)
        {
            builder.AddOrganizationApi();
        }

        builder.Build();
    }

    /// <summary>
    /// 示例9：渐进式迁移
    /// </summary>
    public static class Example9_MigrationExamples
    {
        /// <summary>
        /// 旧方式（仍然支持）
        /// </summary>
        public static void OldWay(IServiceCollection services, IConfiguration configuration)
        {
            services.AddFeishuApiService(configuration);
        }

        /// <summary>
        /// 新方式 - 语法迁移
        /// </summary>
        public static void NewWaySyntax(IServiceCollection services, IConfiguration configuration)
        {
            services.AddFeishuServices(configuration)
                .AddAllApis()
                .Build();
        }

        /// <summary>
        /// 新方式 - 按需优化
        /// </summary>
        public static void NewWayOptimized(IServiceCollection services, IConfiguration configuration)
        {
            services.AddFeishuServices(configuration)
                .AddTokenManagers()
                .AddOrganizationApi()
                .AddMessageApi()
                .Build();
        }

        /// <summary>
        /// 新方式 - 精确定制
        /// </summary>
        public static void NewWayPrecise(IServiceCollection services, IConfiguration configuration)
        {
            services.AddFeishuServices(configuration)
                .AddTenantTokenManager()
                .AddAppTokenManager()
                .AddOrganizationApi()
                .Build();
        }
    }

    /// <summary>
    /// 示例10：自定义扩展（注意：扩展方法必须在顶级静态类中定义）
    /// </summary>
    // 自定义扩展方法示例 - 为特定应用场景创建预设
    public static IServiceCollection AddFeishuForECommerce(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddFeishuServices(configuration)
            .AddTokenManagers()
            .AddMessageApi()           // 客服消息
            .AddChatGroupApi()         // 客户群聊
            .Build();
    }

    // 自定义扩展方法示例 - HR管理系统
    public static IServiceCollection AddFeishuForHR(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddFeishuServices(configuration)
            .AddTokenManagers()
            .AddOrganizationApi()      // 员工、部门管理
            .Build();
    }

    // 自定义扩展方法示例 - 内部工具
    public static IServiceCollection AddFeishuForInternalTools(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddFeishuServices(configuration)
            .AddAppTokenManager()       // 只需要应用权限
            .AddAuthenticationApi()    // 用户认证
            .Build();
    }
}