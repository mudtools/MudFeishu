// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Knowledge;
using Mud.Feishu.AI.FeishuTools.Channels;
using Mud.Feishu.AI.FeishuTools.Events;
using Mud.Feishu.AI.FeishuTools.Internal;
using Mud.Feishu.AI.FeishuTools.Knowledge;
using Mud.Feishu.AI.FeishuTools.Registration;
using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools;

/// <summary>
/// 飞书工具包注册（Phase 1 只读 + Phase 2 写类/流式通道/RAG-A + AI-FD-D12 子域注册粒度）。
/// </summary>
/// <remarks>
/// <para>
/// <b>子域注册粒度（P1D-1c）</b>：<see cref="AddFeishuTools"/> 为「引入全部域」便捷入口
/// （等价于逐域扩展全调）；宿主可按需 <see cref="AddFeishuBitableTools"/>/<see cref="AddFeishuImTools"/>/
/// <see cref="AddFeishuWriteTools"/> 等逐域装配——域客户端缺席时该域执行器与注册器均不注册
/// （<see cref="IFeishuToolDomainRegistrar"/>），工具不进注册表，白名单映射期 fail-fast 报「未注册」，
/// 错误面从启动崩溃收敛为白名单期明确报错。
/// </para>
/// <para>
/// 前置要求：宿主已注册 <c>FeishuAgentOptions</c>（<c>AddFeishuAgent</c>）与所需域的飞书核心客户端
/// （对应域的 <c>Add*Api()</c>）。授权钩子 <see cref="IToolExecutionAuthorizer"/> 与结果整形钩子
/// <see cref="IToolResultShaper"/> 由宿主按需注册（SDK 不内建策略）；写工具在
/// <c>EnforceToolAuthorization=true</c> 且未注册授权器时默认拒绝（安全默认）。
/// </para>
/// <para>
/// 白名单语义（读写分离，Phase 2 §4）：工具默认「收进注册表不启用」；只读工具经
/// <c>FeishuAgent:Tools</c>（<see cref="FeishuAgentOptions.Tools"/>）启用，写类工具
/// <b>单独键控</b>经 <c>FeishuAgent:WriteAllowList</c>（<see cref="FeishuAgentOptions.WriteAllowList"/>，
/// 默认空=不启用任何写工具）启用且必须过授权门禁。名单放错类别的工具名 fail-fast。
/// </para>
/// </remarks>
public static class FeishuToolsServiceCollectionExtensions
{
    /// <summary>
    /// 注册飞书工具包（16 个只读 + 3 个写工具，全部注册、默认不启用）。
    /// </summary>
    /// <remarks>「引入全部域」便捷入口，产物与逐域扩展全调等价（等价性由用例锁定）。</remarks>
    /// <param name="services">服务集合。</param>
    /// <param name="configure">注册表回调（可空；在配置白名单应用后执行，可再 <c>MapTool</c> 启用更多工具）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure)
            .AddFeishuBitableToolsCore()
            .AddFeishuDocxToolsCore()
            .AddFeishuWikiToolsCore()
            .AddFeishuSearchToolsCore()
            .AddFeishuImToolsCore()
            .AddFeishuSheetsToolsCore()
            .AddFeishuDriveToolsCore()
            .AddFeishuKnowledgeToolsCore()
            .AddFeishuWriteToolsCore();

    /// <summary>
    /// 注册只读工具包（Phase 1 兼容入口；现等价 <see cref="AddFeishuTools"/>——写工具同批注册但
    /// 默认不启用，仅 <c>FeishuAgent:WriteAllowList</c> 显式启用，安全默认不变）。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configure">注册表回调（可空）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuReadonlyTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuTools(services, configure);

    /// <summary>
    /// 按域注册 Bitable 工具（P1D-1c）：4 个只读工具执行器 + 写执行器缺席。
    /// </summary>
    /// <remarks>要求宿主已注册 Bitable 三客户端；注册表单例只按首次注册生效（组合多域时回调只在首域传入）。</remarks>
    public static IServiceCollection AddFeishuBitableTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuBitableToolsCore();

    /// <summary>按域注册 Docx 工具（2 个只读）。</summary>
    public static IServiceCollection AddFeishuDocxTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuDocxToolsCore();

    /// <summary>按域注册 Wiki 工具（2 个只读）。</summary>
    public static IServiceCollection AddFeishuWikiTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuWikiToolsCore();

    /// <summary>按域注册 Search 工具（1 个只读）。</summary>
    public static IServiceCollection AddFeishuSearchTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuSearchToolsCore();

    /// <summary>按域注册 IM 工具（2 个只读；写工具经 <see cref="AddFeishuWriteTools"/>）。</summary>
    public static IServiceCollection AddFeishuImTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuImToolsCore();

    /// <summary>按域注册 Sheets 工具（2 个只读）。</summary>
    public static IServiceCollection AddFeishuSheetsTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuSheetsToolsCore();

    /// <summary>按域注册 Drive 工具（2 个只读，AI-FD-D12 P1D-1b 批次 A 新域）。</summary>
    public static IServiceCollection AddFeishuDriveTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuDriveToolsCore();

    /// <summary>按域注册 Knowledge 工具（<c>knowledge.search</c>；要求宿主已注册 <see cref="IRetriever"/> 实现，如 <c>AddFeishuAilyKnowledge</c>）。</summary>
    public static IServiceCollection AddFeishuKnowledgeTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuKnowledgeToolsCore();

    /// <summary>
    /// 注册写域工具（Phase 2：<c>im.send_message</c>/<c>bitable.add_record</c>/<c>approval.create_instance</c> +
    /// 授权链复用）；对应域客户端缺席时对应写工具不进注册表，白名单期 fail-fast。
    /// </summary>
    public static IServiceCollection AddFeishuWriteTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
        => AddFeishuToolInfrastructure(services, configure).AddFeishuWriteToolsCore();

    /// <summary>
    /// 注册分片编辑流式通道（Phase 2 T2-1 兼容入口，保持不变）：
    /// 仅注册 <see cref="EditMessageChannel"/> 单通道（不走卡片流降级链）。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuEditMessageChannel(this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.TryAddSingleton<EditMessageChannel>();
        services.TryAddSingleton<IMessageChannel>(static sp => sp.GetRequiredService<EditMessageChannel>());
        return services;
    }

    /// <summary>
    /// 注册流式通道降级链（AI-FD-D12 P2D-2a）：首选应用消息卡片流通道
    /// （<see cref="CardStreamMessageChannel"/>，单聊按用户投放），Begin 失败自动降级
    /// <see cref="EditMessageChannel"/>（分片编辑）——事件处理器零感知。
    /// </summary>
    /// <remarks>
    /// 要求宿主已注册 <c>IFeishuTenantV1Message</c>（编辑通道必需）；卡片流客户端
    /// <c>IFeishuTenantV2AppCardMessageStream</c> 缺席时链内仅剩编辑通道（注册期软缺席）。
    /// 群聊场景卡片流目标解析为不适用，直接走编辑通道。
    /// </remarks>
    /// <param name="services">服务集合。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuStreamingChannel(this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        // 卡片流客户端缺席 → 通道缺席（链内仅编辑通道），与写执行器同一软缺席模式。
        services.TryAddSingleton(static sp => sp.GetService<Mud.Feishu.IFeishuTenantV2AppCardMessageStream>() is { } card
            ? new CardStreamMessageChannel(
                card,
                sp.GetRequiredService<IFeishuAppContextScopeFactory>(),
                sp.GetRequiredService<IOptions<FeishuAgentOptions>>(),
                sp.GetService<ILogger<CardStreamMessageChannel>>())
            : null!);
        services.TryAddSingleton<EditMessageChannel>();

        services.TryAddSingleton<IMessageChannel>(static sp =>
        {
            var card = sp.GetService<CardStreamMessageChannel>();
            var edit = sp.GetRequiredService<EditMessageChannel>();
            return card is not null
                ? new StreamingChannelChain(sp.GetService<ILogger<StreamingChannelChain>>(), card, edit)
                : new StreamingChannelChain(sp.GetService<ILogger<StreamingChannelChain>>(), edit);
        });

        return services;
    }

    /// <summary>
    /// 注册知识检索上下文装配器（AI-FD-D12 P2D-4a 注入模式）：每轮把 <see cref="IRetriever"/>
    /// 召回切片注入 Prompt。宿主<b>显式装配</b>（不默认开启，省 token）——工具模式见
    /// <see cref="AddFeishuKnowledgeTools"/>；并用合法（注入给背景、工具给深挖）。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="order">装配顺序（缺省 100：默认装配器之后，问题文本先于知识注入）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuKnowledgeContext(this IServiceCollection services, int order = KnowledgeContextAssembler.DefaultOrder)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IContextAssembler>(sp =>
            new KnowledgeContextAssembler(
                sp.GetRequiredService<IRetriever>(),
                order)));
        return services;
    }

    /// <summary>
    /// 一行接入内置 IM 会话事件处理器（AI-FD-D12 P2D-5a）：消息 → 会话 → 模型 → 回复/流式。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 注册 <see cref="ImConversationOptions"/>（配置节 <c>FeishuAgent:ImConversation</c>）与
    /// <see cref="ImMessageConversationalEventHandler"/>（含 Bot 自激过滤、群聊 @ 过滤安全内建）。
    /// 内置上下文装配器集默认只启用 <see cref="ImConversationAssemblers.SenderInfo"/>（零外部调用）；
    /// 其余经 <paramref name="assemblers"/> 显式装配（P2D-5b）。事件订阅侧由宿主在
    /// WebSocket/Webhook 通道上 <c>AddHandler&lt;ImMessageConversationalEventHandler&gt;()</c>
    /// （通道工厂从自身注册表解析，不读全局 <c>IFeishuEventHandler</c> 注册）。
    /// </para>
    /// </remarks>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用配置（可空；提供时绑定 <c>FeishuAgent:ImConversation</c> 节）。</param>
    /// <param name="configure">编程式配置覆盖（可空；在配置绑定之后执行）。</param>
    /// <param name="assemblers">内置装配器集（默认仅 SenderInfo；QuoteMessage 需 Message 客户端、Knowledge 需 IRetriever）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuImConversationHandler(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<ImConversationOptions>? configure = null,
        ImConversationAssemblers assemblers = ImConversationAssemblers.SenderInfo)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var builder = services.AddOptions<ImConversationOptions>();
        if (configuration is not null)
        {
            // 源生成绑定（AOT-3）：Bind 调用点可被配置绑定源生成器拦截（net8+ 由根 props 启用）。
            var section = configuration.GetSection(ImConversationOptions.SectionName);
            builder.Configure(options => section.Bind(options));
        }

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        services.TryAddSingleton(static sp =>
        {
            var options = sp.GetRequiredService<IOptions<ImConversationOptions>>().Value;
            options.Validate();
            return Options.Create(options);
        });

        // 内置装配器集（P2D-5b）：默认只启用 SenderInfo（零外部调用）；其余宿主显式装配。
        if (assemblers.HasFlag(ImConversationAssemblers.SenderInfo))
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IContextAssembler, SenderInfoContextAssembler>());
        }

        if (assemblers.HasFlag(ImConversationAssemblers.QuoteMessage))
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IContextAssembler>(static sp =>
                new QuoteMessageContextAssembler(
                    sp.GetRequiredService<Mud.Feishu.IFeishuTenantV1Message>(),
                    sp.GetRequiredService<IFeishuAppContextScopeFactory>(),
                    sp.GetService<ILogger<QuoteMessageContextAssembler>>())));
        }

        if (assemblers.HasFlag(ImConversationAssemblers.Knowledge))
        {
            // 注入模式（P2D-4a）：要求宿主已注册 IRetriever（如 AddFeishuAilyKnowledge）。
            services.AddFeishuKnowledgeContext();
        }

        // 处理器本体：宿主经通道 AddHandler<ImMessageConversationalEventHandler>() 挂载（AddScoped 语义由通道补齐）。
        services.TryAddScoped<ImMessageConversationalEventHandler>();

        return services;
    }

    /// <summary>
    /// 注册 Aily 托管知识问答（RAG-A 默认路径，Phase 2 T2-6）：
    /// <see cref="IFeishuKnowledgeBase"/> + <see cref="IRetriever"/> 单实现双接口。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用配置（可空；提供时绑定 <c>FeishuAilyKnowledge</c> 节）。</param>
    /// <param name="configure">编程式配置覆盖（可空；在配置绑定之后执行）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuAilyKnowledge(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<AilyKnowledgeOptions>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var builder = services.AddOptions<AilyKnowledgeOptions>();
        if (configuration is not null)
        {
            // 源生成绑定（AOT-3）：Bind 调用点可被配置绑定源生成器拦截（net8+ 由根 props 启用）。
            var section = configuration.GetSection(AilyKnowledgeOptions.SectionName);
            builder.Configure(options => section.Bind(options));
        }

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        services.AddSingleton<AilyKnowledgeProvider>(static sp =>
        {
            var options = sp.GetRequiredService<IOptions<AilyKnowledgeOptions>>().Value;
            options.Validate();

            return new AilyKnowledgeProvider(
                sp.GetRequiredService<Mud.Feishu.IFeishuTenantV1AilyDataKnowledge>(),
                sp.GetRequiredService<IFeishuAppContextScopeFactory>(),
                Options.Create(options),
                sp.GetService<IFeishuToolContextAccessor>(),
                sp.GetService<ILogger<AilyKnowledgeProvider>>());
        });
        services.TryAddSingleton<IFeishuKnowledgeBase>(static sp => sp.GetRequiredService<AilyKnowledgeProvider>());
        services.TryAddSingleton<IRetriever>(static sp => sp.GetRequiredService<AilyKnowledgeProvider>());

        return services;
    }

    /// <summary>执行链协作件 + 注册表 + 工具源桥（幂等；各域扩展共同前置）。</summary>
    private static IServiceCollection AddFeishuToolInfrastructure(
        IServiceCollection services,
        Action<FeishuToolRegistry>? configure)
    {
        // 执行链协作件。
        services.TryAddSingleton<IFeishuToolContextAccessor, FeishuToolContextAccessor>();
        services.TryAddSingleton<IFeishuAppContextScopeFactory, FeishuAppContextScopeFactory>();
        services.TryAddSingleton<FeishuToolBinding>();

        // 注册表：全部已注册域的工具进表（不启用），白名单显式 Map（读写分离）。
        services.TryAddSingleton(sp => BuildRegistry(sp, configure));

        // 工具源桥：AddFeishuAgent 聚合全部工具源产出 ChatOptions.Tools。
        services.TryAddSingleton<FeishuAgentToolSource, FeishuToolsToolSource>();

        // 工具目录与 Schema 导出（P1D-4）：注册表之上的稳定契约（Phase 4 前置件）。
        services.TryAddSingleton<IToolCatalog>(static sp => FeishuToolCatalog.From(sp.GetRequiredService<FeishuToolRegistry>()));
        services.TryAddSingleton<IToolSchemaExporter, FeishuToolSchemaExporter>();

        return services;
    }

    private static IServiceCollection AddFeishuBitableToolsCore(this IServiceCollection services)
    {
        // 执行器：客户端缺席解析为 null（P1D-1c 软缺席——该域工具不进注册表，白名单期报错）。
        services.TryAddSingleton(static sp =>
        {
            var appTable = sp.GetService<Mud.Feishu.IFeishuTenantV1BitableAppTable>();
            var field = sp.GetService<Mud.Feishu.IFeishuTenantV1BitableField>();
            var record = sp.GetService<Mud.Feishu.IFeishuTenantV1BitableRecord>();
            return appTable is not null && field is not null && record is not null
                ? new BitableTools(appTable, field, record, sp.GetRequiredService<IOptions<FeishuAgentOptions>>())
                : null!;
        });
        services.AddToolDomainRegistrar(static sp => sp.GetService<BitableTools>() is { } executor
            ? new BitableToolDomainRegistrar(executor, sp.GetRequiredService<FeishuToolBinding>())
            : null);
        return services;
    }

    private static IServiceCollection AddFeishuDocxToolsCore(this IServiceCollection services)
    {
        services.TryAddSingleton(static sp => sp.GetService<Mud.Feishu.IFeishuTenantV1Docx>() is { } client
            ? new DocxTools(client, sp.GetRequiredService<IOptions<FeishuAgentOptions>>())
            : null!);
        services.AddToolDomainRegistrar(static sp => sp.GetService<DocxTools>() is { } executor
            ? new DocxToolDomainRegistrar(executor, sp.GetRequiredService<FeishuToolBinding>())
            : null);
        return services;
    }

    private static IServiceCollection AddFeishuWikiToolsCore(this IServiceCollection services)
    {
        services.TryAddSingleton(static sp => sp.GetService<Mud.Feishu.IFeishuTenantV2WikiNodes>() is { } client
            ? new WikiTools(client, sp.GetRequiredService<IOptions<FeishuAgentOptions>>())
            : null!);
        services.AddToolDomainRegistrar(static sp => sp.GetService<WikiTools>() is { } executor
            ? new WikiToolDomainRegistrar(executor, sp.GetRequiredService<FeishuToolBinding>())
            : null);
        return services;
    }

    private static IServiceCollection AddFeishuSearchToolsCore(this IServiceCollection services)
    {
        services.TryAddSingleton(static sp => sp.GetService<Mud.Feishu.IFeishuTenantV2SearchDocWiki>() is { } client
            ? new SearchTools(client, sp.GetRequiredService<IOptions<FeishuAgentOptions>>())
            : null!);
        services.AddToolDomainRegistrar(static sp => sp.GetService<SearchTools>() is { } executor
            ? new SearchToolDomainRegistrar(executor, sp.GetRequiredService<FeishuToolBinding>())
            : null);
        return services;
    }

    private static IServiceCollection AddFeishuImToolsCore(this IServiceCollection services)
    {
        services.TryAddSingleton(static sp => sp.GetService<Mud.Feishu.IFeishuTenantV1Message>() is { } client
            ? new ImTools(client, sp.GetRequiredService<IOptions<FeishuAgentOptions>>())
            : null!);
        services.AddToolDomainRegistrar(static sp => sp.GetService<ImTools>() is { } executor
            ? new ImToolDomainRegistrar(executor, sp.GetRequiredService<FeishuToolBinding>())
            : null);
        return services;
    }

    private static IServiceCollection AddFeishuSheetsToolsCore(this IServiceCollection services)
    {
        services.TryAddSingleton(static sp =>
        {
            var spreadsheets = sp.GetService<Mud.Feishu.IFeishuTenantV3Spreadsheets>();
            var data = sp.GetService<Mud.Feishu.IFeishuTenantV3SpreadsheetData>();
            return spreadsheets is not null && data is not null
                ? new SheetsTools(spreadsheets, data, sp.GetRequiredService<IOptions<FeishuAgentOptions>>())
                : null!;
        });
        services.AddToolDomainRegistrar(static sp => sp.GetService<SheetsTools>() is { } executor
            ? new SheetsToolDomainRegistrar(executor, sp.GetRequiredService<FeishuToolBinding>())
            : null);
        return services;
    }

    private static IServiceCollection AddFeishuDriveToolsCore(this IServiceCollection services)
    {
        services.TryAddSingleton(static sp =>
        {
            var folder = sp.GetService<Mud.Feishu.IFeishuTenantV1DriveFolder>();
            var files = sp.GetService<Mud.Feishu.IFeishuTenantV1DriveFiles>();
            return folder is not null && files is not null
                ? new DriveTools(folder, files, sp.GetRequiredService<IOptions<FeishuAgentOptions>>())
                : null!;
        });
        services.AddToolDomainRegistrar(static sp => sp.GetService<DriveTools>() is { } executor
            ? new DriveToolDomainRegistrar(executor, sp.GetRequiredService<FeishuToolBinding>())
            : null);
        return services;
    }

    private static IServiceCollection AddFeishuKnowledgeToolsCore(this IServiceCollection services)
    {
        // IRetriever 未注册（如未调 AddFeishuAilyKnowledge）→ 工具不进注册表（白名单期报错）。
        services.TryAddSingleton(static sp => sp.GetService<IRetriever>() is { } retriever
            ? new KnowledgeSearchTools(retriever, sp.GetRequiredService<IOptions<FeishuAgentOptions>>())
            : null!);
        services.AddToolDomainRegistrar(static sp => sp.GetService<KnowledgeSearchTools>() is { } executor
            ? new KnowledgeToolDomainRegistrar(executor, sp.GetRequiredService<FeishuToolBinding>())
            : null);
        return services;
    }

    private static IServiceCollection AddFeishuWriteToolsCore(this IServiceCollection services)
    {
        // 写执行器（Phase 2）：对应域客户端缺席时解析为 null（写工具不进注册表，白名单期 fail-fast）。
        services.TryAddSingleton(static sp => sp.GetService<Mud.Feishu.IFeishuTenantV1Message>() is { } message
            ? new MessageWriteTools(message)
            : null!);
        services.TryAddSingleton(static sp => sp.GetService<Mud.Feishu.IFeishuTenantV1BitableRecord>() is { } record
            ? new BitableWriteTools(record)
            : null!);
        services.TryAddSingleton(static sp => sp.GetService<Mud.Feishu.IFeishuTenantV4Approval>() is { } approval
            ? new ApprovalWriteTools(approval)
            : null!);
        services.AddToolDomainRegistrar(static sp => new WriteToolDomainRegistrar(
            sp.GetService<MessageWriteTools>(),
            sp.GetService<BitableWriteTools>(),
            sp.GetService<ApprovalWriteTools>(),
            sp.GetRequiredService<FeishuToolBinding>()));
        return services;
    }

    private static IServiceCollection AddToolDomainRegistrar<T>(this IServiceCollection services, Func<IServiceProvider, T?> factory)
        where T : class, IFeishuToolDomainRegistrar
        => services.AddSingleton<IFeishuToolDomainRegistrar>(sp => factory(sp)!);

    private static FeishuToolRegistry BuildRegistry(IServiceProvider sp, Action<FeishuToolRegistry>? configure)
    {
        var registry = new FeishuToolRegistry();

        // 域注册器扫描（P1D-1c）：执行器缺席的域（工厂返回 null）不产生注册器，对应工具不进表。
        // 注：IEnumerable 解析会包含工厂返回 null 的描述符，需显式跳过。
        foreach (var registrar in sp.GetServices<IFeishuToolDomainRegistrar>())
        {
            if (registrar is null)
            {
                continue;
            }

            registrar.Register(registry);
        }

        // 配置面白名单（读写分离；未知名字/放错类别 fail-fast——工具名是模型可见契约）。
        var options = sp.GetRequiredService<IOptions<FeishuAgentOptions>>().Value;
        foreach (var name in options.Tools)
        {
            MapWhitelist(registry, name, expectWrite: false, FeishuAgentOptions.SectionName, nameof(FeishuAgentOptions.Tools));
        }

        foreach (var name in options.WriteAllowList)
        {
            MapWhitelist(registry, name, expectWrite: true, FeishuAgentOptions.SectionName, nameof(FeishuAgentOptions.WriteAllowList));
        }

        configure?.Invoke(registry);
        return registry;
    }

    private static void MapWhitelist(
        FeishuToolRegistry registry, string name, bool expectWrite, string section, string propertyName)
    {
        if (!registry.TryGet(name, out var definition) || definition is null)
        {
            throw new InvalidOperationException(
                $"{section}:{propertyName} 白名单包含未注册工具 '{name}'——工具名是模型可见契约，请对照工具契约表修正（域客户端缺席时对应工具不注册）");
        }

        if (definition.IsWrite == expectWrite)
        {
            registry.MapTool(name);
            return;
        }

        throw expectWrite
            ? new InvalidOperationException(
                $"{section}:{propertyName}（写类白名单）不能包含只读工具 '{name}'——读写白名单分离，只读工具请经 FeishuAgent:Tools 启用")
            : new InvalidOperationException(
                $"{section}:{propertyName}（只读白名单）不能包含写类工具 '{name}'——写工具必须经 FeishuAgent:WriteAllowList 单独键控且过授权门禁（安全默认）");
    }
}
