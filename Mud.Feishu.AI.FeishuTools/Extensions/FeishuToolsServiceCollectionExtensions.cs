// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mud.Feishu.AI.FeishuTools.Internal;
using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools;

/// <summary>
/// Phase 1 只读工具包注册（T1-5/6/7/8）：10 个只读工具 + 执行链 + 白名单注册表 + 工具源桥。
/// </summary>
/// <remarks>
/// <para>
/// 前置要求：宿主已注册 <c>FeishuAgentOptions</c>（<c>AddFeishuAgent</c>）与飞书核心客户端
/// （Message/Bitable/Docx/Wiki/Search/Spreadsheets 六域的 <c>Add*Api()</c>）。
/// 授权钩子：<see cref="IToolExecutionAuthorizer"/> 由宿主按需注册到容器（SDK 不内建策略）；
/// 未注册时写类工具在 <c>EnforceToolAuthorization=true</c> 下默认拒绝（本包 10 工具全部只读）。
/// </para>
/// <para>
/// 白名单语义：工具默认「收进注册表不启用」；<c>FeishuAgent:Tools</c> 配置节
/// （<see cref="FeishuAgentOptions.Tools"/>）与 <paramref name="configure"/> 回调
/// （<see cref="FeishuToolRegistry.MapTool"/>）为两种显式启用方式。白名单中的未知名字 fail-fast。
/// </para>
/// </remarks>
public static class FeishuToolsServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Phase 1 只读工具包（10 工具 + 执行链）。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configure">注册表回调（可空；在配置白名单应用后执行，可再 <c>MapTool</c> 启用更多工具）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuReadonlyTools(
        this IServiceCollection services,
        Action<FeishuToolRegistry>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        // 执行链协作件。
        services.TryAddSingleton<IFeishuToolContextAccessor, FeishuToolContextAccessor>();
        services.TryAddSingleton<IFeishuAppContextScopeFactory, FeishuAppContextScopeFactory>();
        services.TryAddSingleton<FeishuToolBinding>();

        // 分域执行器（强类型客户端由宿主经 Add*Api() 注册）。
        services.TryAddSingleton<BitableTools>();
        services.TryAddSingleton<DocxTools>();
        services.TryAddSingleton<WikiTools>();
        services.TryAddSingleton<SearchTools>();
        services.TryAddSingleton<ImTools>();
        services.TryAddSingleton<SheetsTools>();

        // 注册表：10 个工具全部注册（不启用），白名单显式 Map。
        services.TryAddSingleton(sp =>
        {
            var registry = BuildRegistry(sp);

            // 配置面白名单（FeishuAgent:Tools）＝ MapTool 等价物；未知名字 fail-fast（防配置漂移）。
            var options = sp.GetRequiredService<IOptions<FeishuAgentOptions>>().Value;
            foreach (var name in options.Tools)
            {
                try
                {
                    registry.MapTool(name);
                }
                catch (KeyNotFoundException)
                {
                    throw new InvalidOperationException(
                        $"FeishuAgent:{nameof(FeishuAgentOptions.Tools)} 白名单包含未注册工具 '{name}'——工具名是模型可见契约，请对照工具契约表修正");
                }
            }

            configure?.Invoke(registry);
            return registry;
        });

        // 工具源桥：AddFeishuAgent 聚合全部工具源产出 ChatOptions.Tools。
        services.TryAddSingleton<FeishuAgentToolSource, FeishuToolsToolSource>();

        return services;
    }

    private static FeishuToolRegistry BuildRegistry(IServiceProvider sp)
    {
        var binding = sp.GetRequiredService<FeishuToolBinding>();
        var bitable = sp.GetRequiredService<BitableTools>();
        var docx = sp.GetRequiredService<DocxTools>();
        var wiki = sp.GetRequiredService<WikiTools>();
        var search = sp.GetRequiredService<SearchTools>();
        var im = sp.GetRequiredService<ImTools>();
        var sheets = sp.GetRequiredService<SheetsTools>();

        var registry = new FeishuToolRegistry();

        // Bitable（3）。
        Register(registry, FeishuToolNames.BitableListTables,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.BitableListTables), args, ctx,
                token => bitable.ListTablesAsync(args, token), ct));
        Register(registry, FeishuToolNames.BitableListFields,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.BitableListFields), args, ctx,
                token => bitable.ListFieldsAsync(args, token), ct));
        Register(registry, FeishuToolNames.BitableQueryRecords,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.BitableQueryRecords), args, ctx,
                token => bitable.QueryRecordsAsync(args, token), ct));

        // Docx（1）。
        Register(registry, FeishuToolNames.DocxGetRawContent,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.DocxGetRawContent), args, ctx,
                token => docx.GetRawContentAsync(args, token), ct));

        // Wiki（2）。
        Register(registry, FeishuToolNames.WikiGetNode,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.WikiGetNode), args, ctx,
                token => wiki.GetNodeAsync(args, token), ct));
        Register(registry, FeishuToolNames.WikiListNodes,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.WikiListNodes), args, ctx,
                token => wiki.ListNodesAsync(args, token), ct));

        // Search（1）。
        Register(registry, FeishuToolNames.SearchDocWiki,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.SearchDocWiki), args, ctx,
                token => search.SearchAsync(args, token), ct));

        // IM（1）。
        Register(registry, FeishuToolNames.ImGetHistoryMessages,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.ImGetHistoryMessages), args, ctx,
                token => im.GetHistoryAsync(args, token), ct));

        // Sheets（2）。
        Register(registry, FeishuToolNames.SheetsListSheets,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.SheetsListSheets), args, ctx,
                token => sheets.ListSheetsAsync(args, token), ct));
        Register(registry, FeishuToolNames.SheetsGetRangeValues,
            (args, ctx, ct) => binding.ExecuteAsync(Def(registry, FeishuToolNames.SheetsGetRangeValues), args, ctx,
                token => sheets.GetRangeValuesAsync(args, token), ct));

        return registry;
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
