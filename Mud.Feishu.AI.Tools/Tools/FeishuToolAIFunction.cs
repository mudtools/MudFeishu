// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools;

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
internal sealed class FeishuToolAIFunction : AIFunction
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

            tools.Add(ApplyApprovalGate(new FeishuToolAIFunction(definition, schemaJson, accessor), definition));
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
    /// 未启用任何工具 → 无 guidance → 指令装配与 Phase 0 一致。
    /// </para>
    /// <para>
    /// <b>排序＝丢弃优先级（R5 / B-12）</b>：超限时 <see cref="FeishuGuidanceComposer.Compose"/>
    /// 按<b>输入顺序</b>整域丢弃尾部域，故输入顺序即"域重要性"。原实现按<b>域名字母序</b>，
    /// 一旦超限就<b>确定性地</b>丢掉字母序靠后的域——实测在原 2048 预算下从第 7 个域
    /// <c>feishu</c> 起共 8 个域的 guidance 从未进入过 prompt（含 <c>im</c>/<c>task</c>/<c>mail</c>/<c>wiki</c>）。
    /// 现改为<b>按"该域已启用工具数"降序</b>，同数按域名字母序：
    /// <list type="bullet">
    /// <item>工具数多的域是Agent 最常命中的域（<c>im</c>/<c>task</c>/<c>calendar</c>/<c>bitable</c>/<c>drive</c>
    /// 各 5~8 个工具），其 guidance 最不该被丢；</item>
    /// <item>工具数少的域（<c>search</c>/<c>knowledge</c>/<c>feishu</c>/<c>wiki</c> 各 1~2 个）先于长尾被丢弃。</item>
    /// </list>
    /// <b>零人工配置</b>：优先级完全由 <see cref="FeishuToolRegistry.EnabledTools"/> 派生
    /// （无需新增配置键，也不违反 R-4 "上限固化常量、不设公开配置键"的治理）。
    /// 该顺序在"预算充足"（当前 16384 全域零丢弃）与"预算被调小"两种场景下都给出可断言的行为。
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

        // 域 = 工具名首个"." 之前的部分；按域聚合已启用工具数 → 丢弃优先级（工具数降序，同数按域名字母序）。
        var domainToolCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var tool in registry.EnabledTools)
        {
            var domain = DomainOf(tool.Name);
            if (domain.Length == 0)
            {
                continue;
            }

            domainToolCounts[domain] = domainToolCounts.TryGetValue(domain, out var count)
                ? count + 1
                : 1;
        }

        // OrderByDescending(工具数) → OrderBy(域名)：前者定重要性，后者保证同重要性的顺序确定（可 golden 比对）。
        var domains = domainToolCounts
            .OrderByDescending(static pair => pair.Value)
            .ThenBy(static pair => pair.Key, StringComparer.Ordinal);

        var blocks = new List<FeishuGuidanceBlock>();
        foreach (var domain in domains)
        {
            if (FeishuToolGuidance.ByDomain.TryGetValue(domain.Key, out var content))
            {
                blocks.Add(new FeishuGuidanceBlock(domain.Key, content));
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
