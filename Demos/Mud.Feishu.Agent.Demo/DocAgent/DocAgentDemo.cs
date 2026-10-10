// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Extensions;
using Mud.Feishu.AI.AgentTools;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 文档业务 AI 智能体控制台 Demo 的模式入口（配置 <c>FeishuDocAgent:Enabled=true</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>能力面</b>：6 个文档业务域客户端（docx / wiki / drive / sheets / bitable / search）+
/// 27 枚白名单工具（只读 14 + 写 13）+ 三道安全闸（框架审批管线 / 宿主授权器 / 执行审计）+
/// 控制台流式通道 + 6 部命中式剧本。
/// </para>
/// <para>
/// <b>装配顺序逐条都有理由</b>：域客户端必须先于 <c>AddFeishuTools</c>（域客户端缺席 ⇒ 该域工具
/// 不注册 ⇒ 白名单映射期 fail-fast）；宿主钩子必须在 Agent 首次解析（会构造工具面并包装写工具）之前注册。
/// </para>
/// <para>
/// <b><c>validateScopes: false</c></b>：<c>FeishuAgentToolSource.GetTools</c> 由 <b>Singleton</b> 的
/// <c>FeishuAgent</c> 工厂在<b>根容器</b>解析（SDK 显式传 <c>services: null</c> 以避免 Captive Dependency），
/// 本 Demo 的钩子全部注册为 Singleton，与之匹配；开启作用域校验只会产生误报。
/// </para>
/// <para>
/// <b>不做的事</b>（已决策，见设计文档 §0.4）：不接飞书镜像（<c>IMessageChannel</c> 只有控制台一个实现）、
/// 不接 OTel Exporter（只有控制台 Trace + 本地指标）、不接 Aily 托管知识。
/// </para>
/// </remarks>
public static class DocAgentDemo
{
    /// <summary>
    /// 运行文档业务智能体控制台（同步阻塞直至退出）。
    /// </summary>
    /// <returns>任务。</returns>
    /// <exception cref="InvalidOperationException">配置缺失/非法，或工具白名单与已注册域不匹配。</exception>
    public static async Task RunAsync(DemoConfigurationSources sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var settings = DocAgentSettings.FromConfiguration(sources.Configuration);
        settings.Validate();

        // ① 飞书多应用配置：配置文件已提供 FeishuApps 则原样使用，否则用 FeishuDemo 凭证合成单应用。
        var configuration = EnsureAppSection(sources.Configuration, settings);

        var services = new ServiceCollection();

        // ⓪ 运行时日志（Serilog：控制台 Warning 不干扰 REPL 流式渲染，文件全量 Information；
        // 配置文件提供 "Serilog" 节时以其为唯一事实源）。
        services.AddDemoLogging(sources.Configuration);

        // ② 租户与域客户端（必须先于 AddFeishuTools：域缺席 ⇒ 该域工具不注册）。
        services
            .AddFeishuApp(configuration, "FeishuApps")
            .AddFeishuServices(builder => builder
                .AddDocxApi()          // 云文档：正文 / 块树 / 块增删改 / Markdown 转换
                .AddWikiApi()          // 知识库：空间 / 节点树 / 节点解析 / 建节点 / 移动
                .AddDriveApi()         // 云盘：文件列举 / 元数据 / 建文件夹 / 移动 / 上传
                .AddSpreadsheetsApi()  // 电子表格：工作表列表 / 区域读 / 区域写 / 追加行
                .AddBiTableApi()       // 多维表格：表 / 字段 / 记录（只读）+ 新增记录
                .AddSearchApi());      // 云文档搜索（im 域已决策不做）

        // ③ 模型客户端 + Agent（白名单在此定型；Endpoint 非 HTTPS 时 AddFeishuOpenAIChatClient 抛错，
        // DocAgentSettings.Validate 已提前给出更友好的错误）。
        services.AddDemoAgent(
            new ChatModelSettings { ModelId = settings.ModelId, ApiKey = settings.ApiKey, Endpoint = settings.Endpoint },
            DocAgentSettings.AgentName,
            DocAgentPrompt.Instructions,
            options =>
            {
                options.Tools = [.. DocAgentSettings.ReadonlyTools];
                options.WriteAllowList = [.. DocAgentSettings.WriteTools];

                // AllowedIdentities 保持默认 ["tenant"]：文档业务域（docx/wiki/drive/sheets/bitable/search）
                // 全部是 tenant 身份——**这是按域事实推导的结果，不是复制粘贴**。
                // 对比：工具冒烟模式必须写 ["tenant","user"]，因为那边含 task.list_my_tasks（identity=user）。
                // MaxToolRisk 保持默认 "high-risk-write"（允许 docx.delete_blocks 进入审批管线）。
                // EnforceToolAuthorization 保持默认 true（写工具的安全底线）。
                options.MaxHistoryMessages = 50;
                options.MaxHistoryTokens = 8000;
                options.SummaryThreshold = settings.SummaryThreshold;
                options.MaxToolResultLength = 4000;
            });

        // ④ 工具面（读 options.Tools / WriteAllowList 建注册表；写工具在此被审批包装）。
        services.AddFeishuTools();

        // ⑤ 宿主钩子三件套（SDK 只给契约，策略全在 Demo）——必须在 Agent 首次解析前注册。
        services.AddSingleton<ConsoleRenderer>();
        services.AddSingleton(new ConsoleToolAuthorizerState(
            settings.AppKey, DocAgentSettings.AllowedScopes, settings.Policy));
        services.AddSingleton<IToolExecutionAuthorizer, ConsoleToolAuthorizer>();

        services.AddSingleton<ConsoleApprovalChannelState>();
        services.AddSingleton<IFeishuToolApprovalChannel, ConsoleApprovalChannel>();

        services.AddSingleton<InMemoryAuditSink>();
        services.AddSingleton<IToolExecutionAuditSink>(sp => sp.GetRequiredService<InMemoryAuditSink>());

        // ⑥ 附件落盘器（drive.upload_file 依赖它；缺失 ⇒ 该工具不注册 ⇒ 白名单映射期失败）。
        services.AddHttpClient();
        services.AddSingleton(sp => new DemoAttachmentStager(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
            settings.AttachmentMaxBytes));
        services.AddSingleton<IFeishuAttachmentStager>(sp => sp.GetRequiredService<DemoAttachmentStager>());

        // ⑦ 唯一通道实现直接暴露为 IMessageChannel（无装饰器、无注册顺序坑）。
        services.AddSingleton<ConsoleMessageChannel>();
        services.AddSingleton<IMessageChannel>(sp => sp.GetRequiredService<ConsoleMessageChannel>());

        // ⑧ 剧本（S1 需要可选的知识库空间 ID；装配期告警由宿主在横幅之后统一输出）。
        services.AddSingleton(_ => ScenarioBook.CreateDefault(settings));

        // ⑨ REPL（显式装配：避免依赖"可选参数由容器填默认值"这一非显然行为；
        // 日志经公共层注入——SDK 内部与 REPL 自身的日志统一走 Serilog）。
        services.AddSingleton(sp => new AgentConsoleLoop(
            settings,
            sp.GetRequiredService<FeishuAgent>(),
            sp.GetRequiredService<IFeishuToolContextAccessor>(),
            sp.GetRequiredService<IConversationGate>(),
            sp.GetRequiredService<IConversationStore>(),
            sp.GetRequiredService<IMessageChannel>(),
            sp.GetRequiredService<ConsoleMessageChannel>(),
            sp.GetRequiredService<ConsoleRenderer>(),
            sp.GetRequiredService<IFeishuToolApprovalChannel>(),
            sp.GetRequiredService<ConsoleApprovalChannelState>(),
            sp.GetRequiredService<ConsoleToolAuthorizerState>(),
            sp.GetRequiredService<InMemoryAuditSink>(),
            sp.GetRequiredService<FeishuToolRegistry>(),
            sp.GetRequiredService<ScenarioBook>(),
            sp.GetRequiredService<ILogger<AgentConsoleLoop>>()));

        var provider = services.BuildServiceProvider(validateScopes: false);

        try
        {
            var renderer = provider.GetRequiredService<ConsoleRenderer>();
            var consoleChannel = provider.GetRequiredService<ConsoleMessageChannel>();
            var scenarioBook = provider.GetRequiredService<ScenarioBook>();

            FeishuAgent agent;
            FeishuToolRegistry registry;
            try
            {
                agent = provider.GetRequiredService<FeishuAgent>();
                registry = provider.GetRequiredService<FeishuToolRegistry>();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("未注册工具", StringComparison.Ordinal))
            {
                // 域客户端缺席 ⇒ 该域工具不注册 ⇒ 白名单映射期报"未注册工具"（而非启动崩溃）。
                throw new InvalidOperationException(
                    $"工具白名单与当前注册的域客户端不匹配：{ex.Message}"
                    + "\n请检查 AddFeishuServices 是否注册了对应域（域客户端缺席 ⇒ 该域工具不注册）。",
                    ex);
            }

            PrintBanner(settings, renderer, agent, registry, consoleChannel, scenarioBook, sources.Files);

            var loop = provider.GetRequiredService<AgentConsoleLoop>();

            using var cts = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (_, e) =>
            {
                // Ctrl+C 优雅取消当前轮（而非硬杀进程，避免留下半截审计记录）。
                e.Cancel = true;
                cts.Cancel();
            };

            Console.CancelKeyPress += cancelHandler;
            try
            {
                await loop.RunAsync(cts.Token).ConfigureAwait(false);
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }
        finally
        {
            // DemoAttachmentStager 的临时目录需要清理 → 必须走异步释放。
            await provider.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 保证 <c>FeishuApps</c> 节存在：配置已提供则原样使用，否则用 <c>FeishuDemo</c> 的 AppId/AppSecret
    /// 与 <c>FeishuDocAgent</c> 的 AppKey 合成单应用。
    /// </summary>
    /// <param name="configuration">配置来源。</param>
    /// <param name="settings">已解析的配置（提供合成值）。</param>
    /// <returns>可供 <c>AddFeishuApp</c> 使用的配置（含 <c>FeishuApps</c> 节）。</returns>
    /// <remarks>
    /// <b>顺序即语义</b>：配置文件里显式写出的 <c>FeishuApps</c> 优先级高于由统一节合成的单应用配置
    /// （前者是 <c>AddFeishuApp</c> 真正消费的对象）；合成项仅在缺失时追加，
    /// 且追加在最后（覆盖最低优先级的空节）。模板不再包含 <c>FeishuApps</c>——
    /// 模型与飞书凭证的唯一来源是 <c>FeishuDemo</c> 节。
    /// </remarks>
    internal static IConfigurationRoot EnsureAppSection(IConfiguration configuration, DocAgentSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(configuration[$"{DemoAppConfig.SectionName}:0:AppId"]))
        {
            return configuration as IConfigurationRoot
                ?? new ConfigurationBuilder().AddConfiguration(configuration).Build();
        }

        return new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(DemoAppConfig.SingleAppKeys(settings.AppKey, settings.AppId, settings.AppSecret))
            .Build();
    }

    private static void PrintBanner(
        DocAgentSettings settings,
        ConsoleRenderer renderer,
        FeishuAgent agent,
        FeishuToolRegistry registry,
        ConsoleMessageChannel consoleChannel,
        ScenarioBook scenarioBook,
        IReadOnlyList<string> configurationFiles)
    {
        var guidance = agent.Guidance;
        var guidanceLength = Math.Max(0, guidance.Instructions.Length - DocAgentPrompt.Instructions.Length);
        var enabled = registry.EnabledTools;
        var readonlyCount = enabled.Count(static t => !t.IsWrite);

        var facts = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["配置来源"] = configurationFiles.Count == 0
                ? "（未发现 appsettings*.json）"
                : string.Join(" + ", configurationFiles),
            ["应用 appKey"] = $"{settings.AppKey}（AppId {DocAgentSettings.Mask(settings.AppId)}，Secret 已隐藏）",
            ["模型"] = settings.Endpoint is null
                ? $"{settings.ModelId}（SDK 默认端点）"
                : $"{settings.ModelId} @ {settings.Endpoint}",
            ["已启用工具"] = $"{enabled.Count} 个（只读 {readonlyCount} / 写 {enabled.Count - readonlyCount}）",
            ["身份闭集"] = "tenant（文档业务域全部 tenant；按域事实推导）",
            ["风险上限"] = "high-risk-write（允许 docx.delete_blocks 进入审批管线）",
            ["授权强制"] = "on（写工具未注册授权器即拒绝 → 本 Demo 已注册 ConsoleToolAuthorizer）",
            ["授权策略"] = settings.Policy,
            ["工具结果截断"] = "4000 字符（MaxToolResultLength，大文档读取必然截断）",
            ["会话记忆"] = $"50 条 / 8000 token / 摘要阈值 {settings.SummaryThreshold}",
            ["指令装配"] = $"{guidance.IncludedDomains.Count} 个域 guidance，共 {guidanceLength} / "
                + $"{FeishuGuidanceComposer.MaxGuidanceLength} 字符"
                + (guidance.Truncated ? "（已截断）" : "（未截断）"),
            ["回复通道"] = $"控制台（流式，唯一通道；分片阈值 {consoleChannel.ChunkLength} 字符 / "
                + $"≥{ConsoleMessageChannel.MinUpdateIntervalMs}ms）",
            ["剧本"] = $"{scenarioBook.All.Count} 部（/scenario 列表，/scenario kb-digest 直达）",
        };

        renderer.Banner("Mud.Feishu 文档业务 AI 智能体 · 控制台 Demo", facts);

        // guidance 被静默丢弃是"域引导凭空消失"的唯一可观测信号——必须黄色告警。
        if (guidance.Truncated)
        {
            renderer.Notice(
                NoticeLevel.Warn,
                $"域 guidance 超过 {FeishuGuidanceComposer.MaxGuidanceLength} 字符上限，已按域顺序丢弃尾部域："
                + $"{string.Join("、", guidance.OmittedDomains)}——模型将失去这些域的避坑知识（减少同时启用的域可缓解）。");
        }

        // 白名单工具全部启用（映射期 fail-fast 已保证）；此处只做"缺席可见性"（RK-12）。
        var missing = DocAgentSettings.ReadonlyTools
            .Concat(DocAgentSettings.WriteTools)
            .Except(enabled.Select(static t => t.Name), StringComparer.Ordinal)
            .ToArray();
        if (missing.Length > 0)
        {
            renderer.Notice(
                NoticeLevel.Warn,
                $"以下白名单工具未进入注册表（宿主钩子缺席？）：{string.Join("、", missing)}");
        }

        foreach (var warning in scenarioBook.Warnings)
        {
            renderer.Notice(NoticeLevel.Warn, warning);
        }

        renderer.Notice(
            NoticeLevel.Warn,
            "docx.delete_blocks / docx.replace_document 属不可撤销操作，请在测试空间与测试文档上演练；"
            + "默认 strict 策略下高风险写工具会被授权器独立拒绝（/policy ask 才真删）。");
    }
}
