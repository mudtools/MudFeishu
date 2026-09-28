// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools;

/// <summary>
/// 注册表工具 → MEAI <see cref="AIFunction"/> 桥接：Schema 取编译期生成常量
/// （<c>FeishuToolSchemas</c>，零运行时反射），执行经注册表 Handler（→ <see cref="FeishuToolBinding"/>）。
/// </summary>
/// <remarks>
/// <para>
/// 会话上下文（appKey/chat/user）经 <see cref="IFeishuToolContextAccessor"/> 异步流读取；
/// 缺失时结构化拒绝（多租户隔离禁止默认应用兜底，TMA2-20）。
/// </para>
/// <para>
/// <b>Schema 口径</b>：<see cref="JsonSchema"/> 必须是<b>纯参数 JSON Schema</b>（MEAI 契约要求，
/// 见 <see cref="ToolSchemaJson"/>），故构造时从编译期信封常量中提取 <c>parameters</c> 并缓存；
/// 信封的 <c>name</c>/<c>description</c> 分别由 <see cref="Name"/>/<see cref="Description"/> 承载，
/// <c>x-feishu</c> 元数据由注册表定义承载——不重复进入 Schema 关键字空间。
/// </para>
/// </remarks>
public sealed class FeishuToolAIFunction : AIFunction
{
    private readonly FeishuToolDefinition _definition;
    private readonly IFeishuToolContextAccessor? _contextAccessor;
    private readonly JsonElement _cachedParameterSchema;

    /// <summary>
    /// 初始化 <see cref="FeishuToolAIFunction"/>。
    /// </summary>
    /// <param name="definition">注册表工具定义（含执行 Handler）。</param>
    /// <param name="schemaJson">编译期生成的工具描述符常量（信封形态，含 <c>parameters</c>）。</param>
    /// <param name="contextAccessor">工具执行上下文访问器（可空；缺失时执行期结构化拒绝）。</param>
    public FeishuToolAIFunction(
        FeishuToolDefinition definition,
        string schemaJson,
        IFeishuToolContextAccessor? contextAccessor = null)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        if (string.IsNullOrWhiteSpace(schemaJson))
            throw new ArgumentException("Schema JSON 不能为空", nameof(schemaJson));

        _contextAccessor = contextAccessor;
        // 构造时提取一次并缓存（修复 AI-FD-GAP P0-1 附注：每次调用 JsonDocument.Parse 的性能问题）。
        _cachedParameterSchema = ToolSchemaJson.ExtractParameters(schemaJson);
    }

    /// <inheritdoc />
    public override string Name => _definition.Name;

    /// <inheritdoc />
    public override string Description => _definition.Description;

    /// <inheritdoc />
    public override JsonElement JsonSchema => _cachedParameterSchema;

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var context = _contextAccessor?.Current;
        if (context is null)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                _definition.Name,
                "工具执行上下文缺失（未设置 appKey）——请经 ConversationalFeishuEventHandler 或 IFeishuToolContextAccessor.Begin 注入"));
        }

        // AIFunctionArguments 实现为 IReadOnlyDictionary<string, object?>（可空性注解仅编译期）。
        var dictionary = (IReadOnlyDictionary<string, object?>)arguments;
        return await _definition.Handler(dictionary, context, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// <see cref="FeishuAgentToolSource"/> 的飞书工具包实现：把白名单启用的注册表工具桥接为模型可见
/// <see cref="AIFunction"/> 列表。
/// </summary>
internal sealed class FeishuToolsToolSource : FeishuAgentToolSource
{
    /// <inheritdoc />
    public override IReadOnlyList<AIFunction> GetTools(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
            throw new ArgumentNullException(nameof(serviceProvider));

        var registry = serviceProvider.GetService<FeishuToolRegistry>();
        if (registry is null)
        {
            return [];
        }

        var accessor = serviceProvider.GetService<IFeishuToolContextAccessor>();
        var tools = new List<AIFunction>();
        foreach (var definition in registry.EnabledTools)
        {
            if (!FeishuToolSchemas.SchemaByToolName.TryGetValue(definition.Name, out var schemaJson))
            {
                // 契约守卫保证「注册名 ⊆ Schema 常量表」；此处兜底跳过而不是让整条管线崩溃。
                continue;
            }

            tools.Add(new FeishuToolAIFunction(definition, schemaJson, accessor));
        }

        return tools;
    }

    /// <summary>
    /// 返回<b>已启用工具所属域</b>的 guidance（WP6 / AT-F09）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 域 = 工具名首个 <c>.</c> 之前的部分（与 <c>CapabilityLookupTools</c> 的模块口径一致）；
    /// 数据源是生成器从 <c>Guidance/{domain}.md</c> 发射的编译期字典——<b>与工具面同源同 pass</b>，
    /// 故不存在"md 说的和工具面不一致"的漂移面。
    /// </para>
    /// <para>
    /// 口径与 <see cref="GetTools"/> 完全一致（<see cref="FeishuToolRegistry.EnabledTools"/>）：
    /// 未启用任何工具 → 无 guidance → 指令装配与 Phase 0 一致。
    /// </para>
    /// </remarks>
    public override IReadOnlyList<FeishuGuidanceBlock> GetGuidance(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
            throw new ArgumentNullException(nameof(serviceProvider));

        var registry = serviceProvider.GetService<FeishuToolRegistry>();
        if (registry is null)
        {
            return [];
        }

        // 按域名排序保证装配确定（同一启用集合 → 同一指令文本，便于 golden 式比对与缓存）。
        var domains = registry.EnabledTools
            .Select(static tool => DomainOf(tool.Name))
            .Where(static domain => domain.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static domain => domain, StringComparer.Ordinal);

        var blocks = new List<FeishuGuidanceBlock>();
        foreach (var domain in domains)
        {
            if (FeishuToolGuidance.ByDomain.TryGetValue(domain, out var content))
            {
                blocks.Add(new FeishuGuidanceBlock(domain, content));
            }
        }

        return blocks;
    }

    /// <summary>取工具名所属域（首个 <c>.</c> 之前的部分）。</summary>
    private static string DomainOf(string toolName)
    {
        // 注：netstandard2.0 没有 IndexOf(char, StringComparison)，用字符串重载。
        var index = toolName.IndexOf(".", StringComparison.Ordinal);
        return index > 0 ? toolName.Substring(0, index) : toolName;
    }
}
