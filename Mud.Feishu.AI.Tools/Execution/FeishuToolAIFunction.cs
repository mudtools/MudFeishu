// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Mud.Feishu.AI.Tools.Execution;

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
/// <para>
/// <b>唯一例外：per-tool affordance（R-3 / F-7）</b>——它是<b>给模型看的决策指导</b>，必须落到
/// 模型必然读到的字段上。既然 <c>x-feishu</c> 不进 Schema 关键字空间，就由
/// <see cref="Description"/> 承载（<see cref="ToolAffordanceText.ForDescription"/> 追加，无资产时
/// 空串 ⇒ 描述逐字节不变）。两链（进程内 Agent / MCP 桥）共用本类型 ⇒ 描述逐字节一致，
/// 不存在"只改一条链"的退化面。
/// </para>
/// </remarks>
internal sealed class FeishuToolAIFunction : AIFunction
{
    private readonly FeishuToolDefinition _definition;
    private readonly IFeishuToolContextAccessor? _contextAccessor;
    private readonly JsonElement _cachedParameterSchema;
    private readonly string _description;

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

        // R-3 / F-7：affordance 同样在构造期解析一次并缓存（无资产 ⇒ 空串 ⇒ 描述逐字节不变）。
        _description = _definition.Description + ToolAffordanceText.ForDescription(schemaJson);
    }

    /// <inheritdoc />
    public override string Name => _definition.Name;

    /// <inheritdoc />
    public override string Description => _description;

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
            // 结构化错误出口：该路径在 FeishuToolBinding 之外（上下文缺失连执行链都进不去），
            // 故不能依赖执行链出口的错误载荷归一——这里直接产出带载荷的失败结果。
            return ToolResult.FromUnclassifiedError(
                FeishuToolBinding.StructuredError(
                    _definition.Name,
                    "工具执行上下文缺失（未设置 appKey）——请经 ConversationalFeishuEventHandler 或 IFeishuToolContextAccessor.Begin 注入"),
                _definition.Name,
                maxLength: 0);
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

            // T4 / D4 + P4-1：装饰顺序 = 审批标记（最外层）→ 预算 → 工具桥。
            // ⚠️ 审批标记**必须**留在最外层：MAF 的 FunctionInvokingChatClient 用
            //    `function is ApprovalRequiredAIFunction` 判定"这次调用要先走人工审批"，
            //    把预算装饰器套在它外面会让标记对 MAF 不可见 ⇒ 写工具的 HITL **静默失效**
            //    （模型可直接执行写操作，R2-1 之前那类"模型自批复"面会回来）。
            //    代价：预算耗尽时用户可能先被问一次审批——正确性优先于少一次交互。
            var toolBridge = new FeishuToolAIFunction(definition, schemaJson, accessor);
            var budgetedBridge = new ToolBudgetAIFunction(toolBridge);
            tools.Add(ApplyApprovalGate(budgetedBridge, definition));
        }

        return tools;
    }

    /// <summary>
    /// P4-1：写类工具包一层 MEAI <see cref="ApprovalRequiredAIFunction"/>，
    /// 把「人工确认」从执行链内部上移到 MAF 审批管线。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么必须在这一层</b>：改之前，拦截发生在 <c>FeishuToolBinding.AuthorizeGateAsync</c>
    /// （工具**被调用之后**），自研令牌要跨模型上下文往返，因此存在「模型自批复」面（R2-1 已缓解但未根除）。
    /// MAF 的 <c>FunctionInvokingChatClient</c> 会在**调用之前**把包装工具的调用改写成
    /// <see cref="ToolApprovalRequestContent"/>，且 <c>ApprovalResponseBindingChatClient</c>
    /// 只接受与「框架发出的请求」绑定的响应 ⇒ <b>批准只能来自宿主</b>，模型无法自批。
    /// </para>
    /// <para>
    /// <b>实测依据</b>（R2-P4-1 探针，非文档推断）：
    /// <list type="bullet">
    /// <item>包装后首轮模型回应为 <c>ToolApprovalRequestContent</c>（<c>RequestId</c> = <c>ficc_{callId}</c>），
    /// 工具<b>不会</b>被执行；</item>
    /// <item>把 <c>request.CreateResponse(approved: true, ...)</c> 作为用户消息内容再次 <c>RunAsync</c>，
    /// 第二轮才真正执行工具（<c>FunctionCallContent → FunctionResultContent → 文本回答</c>）；</item>
    /// <item>未包装时工具在首轮立即执行——两者对比确认了管线的拦截归因正确。</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>判据只用静态可判定项</b>：<see cref="FeishuToolDefinition.IsWrite"/>。
    /// 「是否包装」在 Agent 构造期一次性决定，而 <c>IToolExecutionAuthorizer</c> 的判定是
    /// 每次调用动态的——因此授权器在 P4-1 下退化为纯 <c>Allowed</c>/<c>Denied</c> 策略判定，
    /// <c>NeedsUserConfirmation</c> 由本包装承载。
    /// </para>
    /// </remarks>
    /// <param name="function">原始工具桥。</param>
    /// <param name="definition">工具定义。</param>
    /// <returns>包装后的工具（只读工具原样返回）。</returns>
    private static AIFunction ApplyApprovalGate(AIFunction function, FeishuToolDefinition definition)
        => definition.IsWrite ? new ApprovalRequiredAIFunction(function) : function;

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
    /// 未启用任何工具 → 无 guidance → 指令装配与 一致。
    /// </para>
    /// <para>
    /// 排序与形态判据的<b>唯一实现</b>在 <see cref="DomainGuidanceBlocks"/>（装配链与自检面共用同一份，
    /// 否则自检报出的预算余量与被丢的域就不是真实下发的那一份）：按「该域已启用工具数」降序、
    /// 同数按域名字母序——超限时 <see cref="GuidanceComposer.Compose"/> 按输入顺序整域丢弃尾部域，
    /// 故输入顺序即「域重要性」；形态（routing 每域一行摘要 / full 每域 L1 全文）由
    /// <see cref="FeishuAgentOptions.GuidanceMode"/> 决定，排序语义在两种形态下都不变。
    /// </para>
    /// </remarks>
    public override IReadOnlyList<GuidanceBlock> GetGuidance(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        return DomainGuidanceBlocks.Create(serviceProvider);
    }
}
