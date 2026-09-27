// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Mud.Feishu.AI.FeishuTools;

/// <summary>
/// 注册表工具 → MEAI <see cref="AIFunction"/> 桥接：Schema 取编译期生成常量
/// （<c>FeishuToolSchemas</c>，零运行时反射），执行经注册表 Handler（→ <see cref="FeishuToolBinding"/>）。
/// </summary>
/// <remarks>
/// 会话上下文（appKey/chat/user）经 <see cref="IFeishuToolContextAccessor"/> 异步流读取；
/// 缺失时结构化拒绝（多租户隔离禁止默认应用兜底，TMA2-20）。
/// </remarks>
public sealed class FeishuToolAIFunction : AIFunction
{
    private readonly FeishuToolDefinition _definition;
    private readonly IFeishuToolContextAccessor? _contextAccessor;
    private readonly string _schemaJson;

    /// <summary>
    /// 初始化 <see cref="FeishuToolAIFunction"/>。
    /// </summary>
    /// <param name="definition">注册表工具定义（含执行 Handler）。</param>
    /// <param name="schemaJson">编译期生成的 OpenAI-compatible Schema 常量。</param>
    /// <param name="contextAccessor">工具执行上下文访问器（可空；缺失时执行期结构化拒绝）。</param>
    public FeishuToolAIFunction(
        FeishuToolDefinition definition,
        string schemaJson,
        IFeishuToolContextAccessor? contextAccessor = null)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _schemaJson = string.IsNullOrWhiteSpace(schemaJson)
            ? throw new ArgumentException("Schema JSON 不能为空", nameof(schemaJson))
            : schemaJson;
        _contextAccessor = contextAccessor;
    }

    /// <inheritdoc />
    public override string Name => _definition.Name;

    /// <inheritdoc />
    public override string Description => _definition.Description;

    /// <inheritdoc />
    public override JsonElement JsonSchema
    {
        get
        {
            using var document = JsonDocument.Parse(_schemaJson);
            return document.RootElement.Clone();
        }
    }

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var context = _contextAccessor?.Current;
        if (context is null)
        {
            return FeishuToolBinding.StructuredError(
                _definition.Name,
                "工具执行上下文缺失（未设置 appKey）——请经 ConversationalFeishuEventHandler 或 IFeishuToolContextAccessor.Begin 注入");
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
}
