// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.AI.CapabilityDemo.Common;
using Serilog;

namespace Mud.Feishu.AI.CapabilityDemo.Host;

/// <summary>
/// 唯一的装配根（顺序不可反，逐条有原因）。
/// </summary>
/// <remarks>
/// <para>① 日志 → ② 应用/租户 → ③ 域客户端 → ④ 模型 → ⑤ Agent+白名单 → ⑥ 工具面 → ⑦ 宿主钩子。</para>
/// <para>
/// <b>为什么域客户端必须早于 <c>AddFeishuTools</c></b>：域客户端缺席 ⇒ 该域工具不注册 ⇒
/// 白名单映射期报"未注册工具"。
/// </para>
/// <para>
/// <b>为什么钩子可以晚于 <c>AddFeishuTools</c></b>：注册表是<see cref="ServiceProvider"/>解析期构造的，
/// 钩子只要求"在首次解析 <see cref="FeishuAgent"/> 之前"登记完成。
/// </para>
/// <para>
/// <b><c>validateScopes: false</c></b>：Agent 工厂在根容器解析工具面（Singleton），
/// 开启作用域校验只会产生 Captive Dependency 误报。
/// </para>
/// </remarks>
internal static class CompositionRoot
{
    public const string ModelServiceKey = "cap-model";

    public static ServiceProvider Build(
        IConfiguration configuration,
        DemoSettings settings,
        ActProfile profile,
        bool includeModel = true,
        bool consoleAllowed = true,
        Action<IServiceCollection>? configureServices = null,
        Action<FeishuAgentOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();

        services.AddDemoLogging(configuration, consoleAllowed);

        services.AddFeishuApp(configuration, DemoSettings.AppSectionName);
        services.AddFeishuServices(profile.ConfigureClients);

        if (profile.ScriptedClient is not null)
        {
            // 脚本化模型：ModelServiceKey 必须为空，Agent 才会解析未键控 IChatClient。
            services.AddSingleton(profile.ScriptedClient);
        }
        else if (includeModel)
        {
            services.AddFeishuOpenAIChatClient(
                ModelServiceKey, settings.ModelId, settings.ApiKey, settings.Endpoint);
        }
        else
        {
            services.AddSingleton<IChatClient, UnusedChatClient>();
        }

        services.AddFeishuAgent(configuration, options =>
        {
            options.ModelServiceKey = profile.ScriptedClient is null && includeModel ? ModelServiceKey : null;
            options.Name = profile.AgentName;
            options.Instructions = profile.Instructions;
            options.Tools = [.. profile.ReadTools];
            options.WriteAllowList = [.. profile.WriteTools];
            options.MaxToolResultLength = 4000;
            profile.ConfigureOptions?.Invoke(options);
            configureOptions?.Invoke(options);
        });

        services.AddFeishuTools();

        // 宿主钩子三件套（策略全在 Demo；SDK 只给契约）。
        services.AddSingleton<InMemoryAuditSink>();
        services.AddSingleton<IToolExecutionAuditSink>(sp => sp.GetRequiredService<InMemoryAuditSink>());
        services.AddSingleton<PendingApprovalBoard>();
        services.AddSingleton(_ => new ConsoleAuthorizerState(settings.Policy));
        services.AddSingleton<IFeishuToolApprovalChannel, ConsoleApprovalChannel>();
        services.AddSingleton<IToolExecutionAuthorizer, ConsoleToolAuthorizer>();

        profile.ConfigureHooks?.Invoke(services);
        configureServices?.Invoke(services);

        return services.BuildServiceProvider(validateScopes: false);
    }

    /// <summary>
    /// 离线幕（A2 / A7 / 部分自检）：装配真实工具面，不碰模型、不发任何 HTTP。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么仍要 <c>AddFeishuApp</c> + <c>AddAllApis</c></b>：工具域注册器按「客户端在场」软缺席——
    /// 域客户端缺席时该域工具<b>根本不进注册表</b>（A2 的域矩阵会凭空少一半，白名单期则报"未注册工具"）。
    /// 占位凭证只用于让 HttpClient / 令牌管理器可构造；<b>令牌请求发生在首次真实 API 调用</b>，
    /// 离线幕与 <c>--verify</c> 都不触发。
    /// </para>
    /// <para>
    /// <b>作用域工厂的三个依赖</b>（<c>IAppContextHolder</c> / <c>IFeishuAppManager</c> /
    /// <c>IAppAccessAuthorizer</c>）全部由 <c>AddFeishuApp</c> 提供——不要在这里另加桩实现：
    /// 那会让演示验证的是桩而不是宿主真实要满足的装配。
    /// </para>
    /// </remarks>
    public static ServiceProvider BuildToolsOnly()
    {
        var configuration = DemoConfig.CreateVerifyConfiguration();

        return new ServiceCollection()
            // 复用 Program.Main 已配置好的根日志器（不再 ReadFrom.Configuration 覆盖一次：
            // 离线幕用占位配置，重配会把宿主配置的控制台 sink 丢掉）。
            .AddLogging(static logging => logging.AddSerilog(dispose: false))
            .AddFeishuApp(configuration, DemoSettings.AppSectionName)
            .AddFeishuServices(static builder => builder.AddAllApis())
            .AddSingleton(Options.Create(new FeishuAgentOptions { Instructions = "cap-offline" }))
            .AddFeishuTools()
            .BuildServiceProvider();
    }
}
