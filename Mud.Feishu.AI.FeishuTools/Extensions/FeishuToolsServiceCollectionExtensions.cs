// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Knowledge;
using Mud.Feishu.AI.FeishuTools.Channels;
using Mud.Feishu.AI.FeishuTools.Internal;
using Mud.Feishu.AI.FeishuTools.Knowledge;
using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools;

/// <summary>
/// 飞书工具包注册（Phase 1 只读 + Phase 2 写类/流式通道/RAG-A）。
/// </summary>
/// <remarks>
/// <para>
/// 前置要求：宿主已注册 <c>FeishuAgentOptions</c>（<c>AddFeishuAgent</c>）与飞书核心客户端
/// （对应域的 <c>Add*Api()</c>；写工具还要求 Message/Bitable/Approval 客户端，缺席时对应写工具
/// 不进注册表，白名单映射期 fail-fast 提示）。授权钩子 <see cref="IToolExecutionAuthorizer"/>
/// 由宿主按需注册（SDK 不内建策略）；写工具在 <c>EnforceToolAuthorization=true</c> 且未注册
/// 授权器时默认拒绝（安全默认）。
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
    /// 注册飞书工具包（Phase 1 十个只读工具 + Phase 2 三个写工具，全部注册、默认不启用）。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configure">注册表回调（可空；在配置白名单应用后执行，可再 <c>MapTool</c> 启用更多工具）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        // 执行链协作件。
        services.TryAddSingleton<IFeishuToolContextAccessor, FeishuToolContextAccessor>();
        services.TryAddSingleton<IFeishuAppContextScopeFactory, FeishuAppContextScopeFactory>();
        services.TryAddSingleton<FeishuToolBinding>();

        // 只读分域执行器（Phase 1；强类型客户端由宿主经 Add*Api() 注册，缺席即启动失败——保持既有语义）。
        services.TryAddSingleton<BitableTools>();
        services.TryAddSingleton<DocxTools>();
        services.TryAddSingleton<WikiTools>();
        services.TryAddSingleton<SearchTools>();
        services.TryAddSingleton<ImTools>();
        services.TryAddSingleton<SheetsTools>();

        // 写执行器（Phase 2）：对应域客户端缺席时解析为 null（写工具不进注册表，白名单期 fail-fast），
        // 使「只要只读工具」的宿主无需注册写域客户端。
        services.TryAddSingleton(static sp => sp.GetService<Mud.Feishu.IFeishuTenantV1Message>() is { } message
            ? new MessageWriteTools(message)
            : null!);
        services.TryAddSingleton(static sp => sp.GetService<Mud.Feishu.IFeishuTenantV1BitableRecord>() is { } record
            ? new BitableWriteTools(record)
            : null!);
        services.TryAddSingleton(static sp => sp.GetService<Mud.Feishu.IFeishuTenantV4Approval>() is { } approval
            ? new ApprovalWriteTools(approval)
            : null!);

        // 注册表：13 个工具全部注册（不启用），白名单显式 Map（读写分离）。
        services.TryAddSingleton(sp => BuildRegistry(sp, configure));

        // 工具源桥：AddFeishuAgent 聚合全部工具源产出 ChatOptions.Tools。
        services.TryAddSingleton<FeishuAgentToolSource, FeishuToolsToolSource>();

        return services;
    }

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
    /// 注册分片编辑流式通道（Phase 2 T2-1：<c>EditMessageAsync</c> 降级实现；
    /// 消息流卡片为后续通道实现切换）。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuEditMessageChannel(this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.TryAddSingleton<IMessageChannel, EditMessageChannel>();
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

    private static FeishuToolRegistry BuildRegistry(IServiceProvider sp, Action<FeishuToolRegistry>? configure)
    {
        var binding = sp.GetRequiredService<FeishuToolBinding>();
        var bitable = sp.GetRequiredService<BitableTools>();
        var docx = sp.GetRequiredService<DocxTools>();
        var wiki = sp.GetRequiredService<WikiTools>();
        var search = sp.GetRequiredService<SearchTools>();
        var im = sp.GetRequiredService<ImTools>();
        var sheets = sp.GetRequiredService<SheetsTools>();
        var messageWrite = sp.GetService<MessageWriteTools>();
        var bitableWrite = sp.GetService<BitableWriteTools>();
        var approvalWrite = sp.GetService<ApprovalWriteTools>();

        var registry = new FeishuToolRegistry();

        // 只读（Phase 1，10 个）。
        Register(registry, FeishuToolNames.BitableListTables,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.BitableListTables), args, ctx,
                token => bitable.ListTablesAsync(args, token), ct));
        Register(registry, FeishuToolNames.BitableListFields,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.BitableListFields), args, ctx,
                token => bitable.ListFieldsAsync(args, token), ct));
        Register(registry, FeishuToolNames.BitableQueryRecords,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.BitableQueryRecords), args, ctx,
                token => bitable.QueryRecordsAsync(args, token), ct));
        Register(registry, FeishuToolNames.DocxGetRawContent,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.DocxGetRawContent), args, ctx,
                token => docx.GetRawContentAsync(args, token), ct));
        Register(registry, FeishuToolNames.WikiGetNode,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.WikiGetNode), args, ctx,
                token => wiki.GetNodeAsync(args, token), ct));
        Register(registry, FeishuToolNames.WikiListNodes,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.WikiListNodes), args, ctx,
                token => wiki.ListNodesAsync(args, token), ct));
        Register(registry, FeishuToolNames.SearchDocWiki,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.SearchDocWiki), args, ctx,
                token => search.SearchAsync(args, token), ct));
        Register(registry, FeishuToolNames.ImGetHistoryMessages,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.ImGetHistoryMessages), args, ctx,
                token => im.GetHistoryAsync(args, token), ct));
        Register(registry, FeishuToolNames.SheetsListSheets,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.SheetsListSheets), args, ctx,
                token => sheets.ListSheetsAsync(args, token), ct));
        Register(registry, FeishuToolNames.SheetsGetRangeValues,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.SheetsGetRangeValues), args, ctx,
                token => sheets.GetRangeValuesAsync(args, token), ct));

        // 写类（Phase 2，3 个；对应域客户端缺席时跳过——宿主未接该域则工具不暴露）。
        if (messageWrite is not null)
        {
            Register(registry, FeishuToolNames.ImSendMessage,
                (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.ImSendMessage), args, ctx,
                    token => messageWrite.SendMessageAsync(args, token), ct));
        }

        if (bitableWrite is not null)
        {
            Register(registry, FeishuToolNames.BitableAddRecord,
                (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.BitableAddRecord), args, ctx,
                    token => bitableWrite.AddRecordAsync(args, token), ct));
        }

        if (approvalWrite is not null)
        {
            Register(registry, FeishuToolNames.ApprovalCreateInstance,
                (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.ApprovalCreateInstance), args, ctx,
                    token => approvalWrite.CreateInstanceAsync(args, token), ct));
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
                $"{section}:{propertyName} 白名单包含未注册工具 '{name}'——工具名是模型可见契约，请对照工具契约表修正");
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

    private static void Register(FeishuToolRegistry registry, string toolName, FeishuToolHandler handler)
    {
        if (!FeishuToolSchemas.SchemaByToolName.TryGetValue(toolName, out var schemaJson))
        {
            throw new InvalidOperationException(
                $"工具 '{toolName}' 缺少编译期 Schema——接口须标注 [FeishuTool]（契约守卫防漂移）");
        }

        registry.Register(FromSchema(schemaJson, handler));
    }

    /// <summary>按名取已注册定义（注册完成后调用；BuildRegistry 顺序保证存在性）。</summary>
    private static FeishuToolDefinition Def(FeishuToolRegistry registry, string toolName)
        => registry.TryGet(toolName, out var definition) && definition is not null
            ? definition
            : throw new InvalidOperationException($"工具 '{toolName}' 尚未注册（注册顺序错误）");

    /// <summary>
    /// 从编译期 Schema 常量提取注册表元数据（name/description/scopes/is_write 单一来源 = [FeishuTool] 特性）。
    /// </summary>
    private static FeishuToolDefinition FromSchema(string schemaJson, FeishuToolHandler handler)
    {
        using var document = JsonDocument.Parse(schemaJson);
        var root = document.RootElement;
        var name = root.GetProperty("name").GetString()
            ?? throw new InvalidOperationException("Schema 常量缺少 name");
        var description = root.TryGetProperty("description", out var descriptionElement)
            ? descriptionElement.GetString() ?? string.Empty
            : string.Empty;

        var extension = root.GetProperty("x-feishu");
        var scopes = extension.GetProperty("required_scopes")
            .EnumerateArray()
            .Select(static e => e.GetString() ?? string.Empty)
            .Where(static s => s.Length > 0)
            .ToArray();
        var isWrite = extension.GetProperty("is_write").GetBoolean();

        return new FeishuToolDefinition(name, description, scopes, isWrite, handler);
    }
}
