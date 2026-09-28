// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools;

/// <summary>
/// 工具执行链（护城河核心，已决策⑥）：把模型 tool_call 接到强类型飞书接口。
/// </summary>
/// <remarks>
/// <para>
/// 执行顺序（不可变式）：
/// ① 上下文校验（缺 <c>AppKey</c> 即失败——多租户隔离禁止默认应用兜底，TMA2-20）→
/// ② 授权门禁（<see cref="IToolExecutionAuthorizer"/>；写类工具在
/// <c>EnforceToolAuthorization=true</c> 且未注册授权器时默认拒绝——拒绝时<b>不切换</b>租户上下文、
/// <b>零调用</b>下游接口）→
/// ③ <see cref="IFeishuAppContextScopeFactory.BeginScope"/> 租户切换（先于下游调用，
/// <c>finally</c> 释放）→
/// ④ 分域下游调用（强类型接口 + 解包 + 白名单投影 + 截断，由调用方委托承载）→
/// ⑤ 异常归一（除取消外转结构化错误文本回填模型，不抛裸异常）。
/// </para>
/// <para>
/// 全程 OTel：Span <c>feishu.agent.tool</c> 携带工具名/appKey/scopes/判定结果
/// （护城河硬验收③，Phase 1 §5）。
/// </para>
/// </remarks>
public sealed class FeishuToolBinding
{
    private readonly IFeishuAppContextScopeFactory _scopeFactory;
    private readonly FeishuAgentOptions _options;
    private readonly IToolExecutionAuthorizer? _authorizer;
    private readonly IToolResultShaper? _resultShaper;
    private readonly IToolExecutionAuditSink? _auditSink;
    private readonly ILogger? _logger;

    /// <summary>
    /// 初始化 <see cref="FeishuToolBinding"/>。
    /// </summary>
    /// <param name="scopeFactory">租户上下文作用域工厂。</param>
    /// <param name="options">Agent 配置（<see cref="FeishuAgentOptions.EnforceToolAuthorization"/> 消费点）。</param>
    /// <param name="authorizer">工具授权钩子（可空；SDK 不内建策略）。</param>
    /// <param name="resultShaper">结果整形钩子（可空；宿主注册后对投影结果做最终整形，P1D-2a）。</param>
    /// <param name="auditSink">结构化审计出口（可空；注册后投递允许/拒绝/错误三类审计事件，P1D-3b）。</param>
    /// <param name="logger">日志（可空）。</param>
    public FeishuToolBinding(
        IFeishuAppContextScopeFactory scopeFactory,
        IOptions<FeishuAgentOptions> options,
        IToolExecutionAuthorizer? authorizer = null,
        IToolResultShaper? resultShaper = null,
        IToolExecutionAuditSink? auditSink = null,
        ILogger<FeishuToolBinding>? logger = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _authorizer = authorizer;
        _resultShaper = resultShaper;
        _auditSink = auditSink;
        _logger = logger;
    }

    /// <summary>
    /// 执行一次工具调用：授权门禁 → 租户上下文切换 → 下游委托 → 异常归一。
    /// </summary>
    /// <param name="tool">工具定义（注册表条目，携带 scopes/IsWrite 元数据）。</param>
    /// <param name="arguments">模型 tool_call 入参。</param>
    /// <param name="context">执行上下文（appKey/chat/user，经 <c>IFeishuToolContextAccessor</c> 注入）。</param>
    /// <param name="invokeDownstream">分域下游调用（参数映射 → 强类型接口 → 解包/投影/截断）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>工具执行结果（成功结果或结构化错误）。</returns>
    public async Task<FeishuToolResult> ExecuteAsync(
        FeishuToolDefinition tool,
        IReadOnlyDictionary<string, object?> arguments,
        FeishuToolContext context,
        Func<CancellationToken, Task<FeishuToolResult>> invokeDownstream,
        CancellationToken cancellationToken = default)
    {
        if (tool is null)
            throw new ArgumentNullException(nameof(tool));
        if (arguments is null)
            throw new ArgumentNullException(nameof(arguments));
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        if (invokeDownstream is null)
            throw new ArgumentNullException(nameof(invokeDownstream));

        // ① 上下文校验：多租户隔离事实来源，缺 appKey 即失败（TMA2-20）。
        if (string.IsNullOrWhiteSpace(context.AppKey))
        {
            return FeishuToolResult.FromError(StructuredError(tool.Name, "工具执行上下文缺少 appKey——多租户隔离禁止默认应用兜底（TMA2-20）"));
        }

        using var activity = FeishuToolDiagnostics.StartToolActivity(
            tool.Name, context.AppKey, tool.RequiredScopes, tool.IsWrite, tool.Risk);

        var executionStopwatch = System.Diagnostics.Stopwatch.StartNew();

        // ①' 入站净化（AT-B19）：控制字符/危险 Unicode/独立 CR 一律**拒绝**（不静默剥离）。
        //     先于授权门禁——授权器可能把参数写审计，审计必须看到已净化形态（不变量 A5）。
        var argumentFailure = ToolArgumentSanitizer.Validate(arguments);
        if (argumentFailure is not null)
        {
            return await DenyAsync(
                tool, context, ToolErrorKind.InvalidArgs, $"invalid_args: {argumentFailure}",
                arguments, executionStopwatch, activity, cancellationToken).ConfigureAwait(false);
        }

        // ② 策略轴（AT-B13）：风险上限 / 身份闭集——早于授权门禁（优先级见 EvaluatePolicy）。
        var policyFailure = EvaluatePolicy(tool);
        if (policyFailure is not null)
        {
            return await DenyAsync(
                tool, context, ToolErrorKind.Forbidden, $"policy_denied: {policyFailure}",
                arguments, executionStopwatch, activity, cancellationToken).ConfigureAwait(false);
        }

        // ③ 授权门禁：拒绝时零调用下游、不切换租户上下文。
        var gate = await AuthorizeGateAsync(tool, arguments, context, cancellationToken).ConfigureAwait(false);
        if (!gate.Allowed)
        {
            return await DenyAsync(
                tool, context, gate.Kind, gate.Reason,
                arguments, executionStopwatch, activity, cancellationToken).ConfigureAwait(false);
        }

        activity?.SetTag(FeishuToolDiagnostics.TagDecision, FeishuToolDiagnostics.DecisionAllowed);

        // ④ 租户上下文切换（先于下游调用，作用域 finally 释放）。
        try
        {
            using var scope = _scopeFactory.BeginScope(context.AppKey);
            var result = await invokeDownstream(cancellationToken).ConfigureAwait(false);

            // ⑤ 内容安全（AT-F14）：在净化**之前**扫描原始文本（不变量 A9）——净化会剥离控制字符，
            //    而注入载荷常用不可见字符把关键词拆开，先净化就检测不到了。
            var safetyHits = ToolResultContentSafety.Scan(result.ToString(), _options.ContentSafetyMode);
            if (safetyHits.Count > 0 && _options.ContentSafetyMode == ContentSafetyModes.Block)
            {
                return await DenyAsync(
                    tool, context, ToolErrorKind.Forbidden,
                    $"content_safety_blocked: 工具结果命中内容安全规则 [{string.Join(",", safetyHits)}]",
                    arguments, executionStopwatch, activity, cancellationToken).ConfigureAwait(false);
            }

            // ⑥ 出站净化（强制阶段，不可关闭/不可替换）：工具结果 → 模型上下文是不可撤销出口，
            //    手机号/邮箱/凭据一旦进入上下文就无法召回。必须先于整形钩子与审计标记。
            result = SanitizeResult(result);

            // 内容安全命中（warn 模式）：在已净化文本前加标注（标注本身是干净文本，不受净化影响）。
            var annotation = ToolResultContentSafety.BuildAnnotation(safetyHits);
            if (annotation.Length > 0)
            {
                result = FeishuToolResult.FromText(annotation + result.ToString(), result.Truncated, result.TruncationReason);
            }

            activity?.SetTag(FeishuToolDiagnostics.TagTruncated, result.Truncated);

            FeishuToolDiagnostics.RecordDuration(tool.Name, context.AppKey, executionStopwatch.ElapsedMilliseconds);
            FeishuToolDiagnostics.RecordExecution(tool.Name, context.AppKey, FeishuMetrics.ToolOutcomes.Allowed);
            await WriteAuditWithIsolationAsync(
                tool, context, FeishuMetrics.ToolOutcomes.Allowed, null,
                ToolArgsDigester.Digest(arguments), executionStopwatch.ElapsedMilliseconds,
                cancellationToken).ConfigureAwait(false);

            // ⑦ 结果整形钩子（P1D-2a）：净化/投影/截断之后、回填之前；失败回退默认（异常隔离）。
            return await ShapeWithIsolationAsync(tool.Name, result, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // 取消即时传播（D15：TryMark 类进入处理态不受补偿约束）。
            throw;
        }
        catch (Exception ex)
        {
            // ⑤ 异常归一：结构化错误回填模型，不抛裸异常（总体设计 §4 不变式）。
            _logger?.LogWarning(ex, "工具 {ToolName} 执行失败（appKey: {AppKey}）", tool.Name, context.AppKey);
            var errorText = StructuredError(tool.Name, ToolErrorClassifier.Classify(ex), $"工具执行异常: {ex.Message}");
            FeishuToolDiagnostics.RecordExecution(tool.Name, context.AppKey, FeishuMetrics.ToolOutcomes.Error);
            FeishuToolDiagnostics.RecordDuration(tool.Name, context.AppKey, executionStopwatch.ElapsedMilliseconds);
            await WriteAuditWithIsolationAsync(
                tool, context, FeishuMetrics.ToolOutcomes.Error, ex.Message,
                ToolArgsDigester.Digest(arguments), executionStopwatch.ElapsedMilliseconds,
                CancellationToken.None).ConfigureAwait(false);
            return FeishuToolResult.FromError(errorText);
        }
    }

    /// <summary>
    /// 统一的拒绝出口（AT-B13 / R3 评审 C-4）：指标 + Span 判定 + 审计投递 + 结构化错误文本，
    /// <b>零调用下游、不切租户</b>。入站净化 / 策略轴 / 授权门禁 / 内容安全阻断四条路径共用，
    /// 确保"新增一个拒绝分支必然带上审计"（否则审计会成为可选步骤而被遗漏）。
    /// </summary>
    private async Task<FeishuToolResult> DenyAsync(
        FeishuToolDefinition tool,
        FeishuToolContext context,
        ToolErrorKind kind,
        string reason,
        IReadOnlyDictionary<string, object?> arguments,
        System.Diagnostics.Stopwatch executionStopwatch,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        activity?.SetTag(FeishuToolDiagnostics.TagDecision, FeishuToolDiagnostics.DecisionDenied);
        FeishuToolDiagnostics.RecordExecution(tool.Name, context.AppKey, FeishuMetrics.ToolOutcomes.Denied);
        await WriteAuditWithIsolationAsync(
            tool, context, FeishuMetrics.ToolOutcomes.Denied, reason,
            ToolArgsDigester.Digest(arguments), executionStopwatch.ElapsedMilliseconds,
            cancellationToken).ConfigureAwait(false);
        return FeishuToolResult.FromError(StructuredError(tool.Name, kind, reason));
    }

    /// <summary>
    /// P1D-3b 审计出口投递（异常隔离：sink 失败只记日志，绝不影响执行链；
    /// 入参摘要已在 SDK 侧脱敏——宿主 sink 不接触原始参数）。
    /// </summary>
    private async Task WriteAuditWithIsolationAsync(
        FeishuToolDefinition tool,
        FeishuToolContext context,
        string decision,
        string? reason,
        string argsDigest,
        long durationMs,
        CancellationToken cancellationToken)
    {
        if (_auditSink is null)
        {
            return;
        }

        try
        {
            await _auditSink.WriteAsync(new ToolExecutionAuditRecord(
                tool.Name,
                context.AppKey,
                tool.RequiredScopes,
                decision,
                reason,
                tool.IsWrite,
                argsDigest,
                durationMs,
                context.ConversationKey), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // 补偿性投递（对齐 D15 精神）：审计不得被取消中断，也不得影响执行链。
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "工具审计出口投递失败（tool: {ToolName}）——审计事件丢弃", tool.Name);
        }
    }

    /// <summary>
    /// 出站净化（P0 安全阶段）：剥离控制字符/ANSI 转义，脱敏凭据与个人敏感信息。
    /// </summary>
    /// <remarks>
    /// 无开关、无接口、不可被宿主绕过——见 <see cref="ToolResultSanitizer"/> 的取舍说明。
    /// 数据完整性优先：净化保持文本长度不变（掩码与正文等价替换位数可能不同，但不改变 JSON 结构）。
    /// </remarks>
    private static FeishuToolResult SanitizeResult(FeishuToolResult result)
    {
        var text = result.ToString();
        var sanitized = ToolResultSanitizer.Sanitize(text);

        // 无变化即复用原实例（避免每次调用都重建对象）。
        return string.Equals(sanitized, text, StringComparison.Ordinal)
            ? result
            : FeishuToolResult.FromText(sanitized, result.Truncated, result.TruncationReason);
    }

    private async Task<FeishuToolResult> ShapeWithIsolationAsync(string toolName, FeishuToolResult result, CancellationToken cancellationToken)
    {
        if (_resultShaper is null)
        {
            return result;
        }

        try
        {
            var text = result.ToString();
            var shaped = await _resultShaper
                .ShapeAsync(toolName, text, cancellationToken)
                .ConfigureAwait(false);
            return shaped is null
                ? result
                : FeishuToolResult.FromText(shaped, result.Truncated, result.TruncationReason);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "工具结果整形钩子失败，回退默认结果（tool: {ToolName}）", toolName);
            return result;
        }
    }

    /// <summary>构造结构化错误文本（模型可读、带工具名前缀）。</summary>
    public static string StructuredError(string toolName, string reason)
        => $"[tool_error] {toolName}: {reason}";

    /// <summary>
    /// 构造带错误语义分类的结构化错误文本（AI-FD-D12 P1D-2b：分类 + 原因 + 建议——
    /// 授权拒绝与参数错误可区分，模型据此自我修正：重试 or 换参数 or 放弃）。
    /// </summary>
    /// <remarks>
    /// AT-B12（R3 评审 C-1）：<see cref="ToolErrorKind.NeedsConfirmation"/> 是<b>独立</b>语义，
    /// 不得复用 <see cref="ToolErrorKind.InvalidArgs"/> 的"请修正参数"后缀——待确认不是参数错，
    /// 让模型去改参数会让它陷入无意义的重试循环。
    /// </remarks>
    internal static string StructuredError(string toolName, ToolErrorKind kind, string reason) => kind switch
    {
        ToolErrorKind.Retryable => $"[tool_error] {toolName} (retryable): {reason}——服务端繁忙/网络异常，可稍后重试同一调用",
        ToolErrorKind.InvalidArgs => $"[tool_error] {toolName} (invalid_args): {reason}",
        ToolErrorKind.Forbidden => $"[tool_error] {toolName} (forbidden): {reason}——授权被拒绝，请放弃或改用只读方案",
        ToolErrorKind.NeedsConfirmation => $"[tool_error] {toolName} (needs_confirmation): {reason}——该操作需要用户确认后方可执行；请勿以同一参数重试，改为向用户说明并请求确认",
        _ => $"[tool_error] {toolName}: {reason}",
    };

    /// <summary>按飞书业务 code 分类构造错误回填（<c>FeishuApiOutcome</c> 解包路径共用；分类器 internal，宿主不可见）。</summary>
    internal static string StructuredError(string toolName, int? apiCode, string reason)
        => StructuredError(toolName, ToolErrorClassifier.ClassifyCode(apiCode), reason);

    /// <summary>授权门禁判定结果（AT-B12：<see cref="ToolErrorKind"/> 随判定一起返回，避免调用方猜测语义）。</summary>
    private readonly record struct GateDecision(bool Allowed, string Reason, ToolErrorKind Kind)
    {
        /// <summary>放行。</summary>
        public static GateDecision Pass() => new(true, string.Empty, ToolErrorKind.ApiError);

        /// <summary>拒绝（带语义分类）。</summary>
        public static GateDecision Deny(string reason, ToolErrorKind kind) => new(false, reason, kind);
    }

    private async Task<GateDecision> AuthorizeGateAsync(
        FeishuToolDefinition tool,
        IReadOnlyDictionary<string, object?> arguments,
        FeishuToolContext context,
        CancellationToken cancellationToken)
    {
        if (_authorizer is null)
        {
            if (tool.IsWrite && _options.EnforceToolAuthorization)
            {
                return GateDecision.Deny(
                    "authorization_denied: 写类工具未注册 IToolExecutionAuthorizer，且 EnforceToolAuthorization=true——默认拒绝（安全默认，Phase 1 §3.3.4）",
                    ToolErrorKind.Forbidden);
            }

            // 只读工具默认 NotRequired（授权钩子预留；scopes 随 Schema 供宿主审计）。
            return GateDecision.Pass();
        }

        var result = await _authorizer
            .AuthorizeAsync(tool.Name, tool.RequiredScopes, tool.IsWrite, arguments, context, cancellationToken)
            .ConfigureAwait(false);
        if (result is null)
        {
            return GateDecision.Deny("authorization_denied: 授权器返回空结果——按拒绝处理（fail-closed）", ToolErrorKind.Forbidden);
        }

        return result.Decision switch
        {
            AuthorizationDecision.Allowed => GateDecision.Pass(),
            AuthorizationDecision.Denied => GateDecision.Deny(
                $"authorization_denied: {result.Reason ?? "授权被拒绝"}", ToolErrorKind.Forbidden),
            AuthorizationDecision.NeedsUserConfirmation => GateDecision.Deny(
                $"需要用户确认后才能执行（HITL，Phase 3 交付）：{result.Reason ?? "未提供原因"}",
                ToolErrorKind.NeedsConfirmation),
            _ => GateDecision.Deny(
                $"authorization_denied: 未知授权判定 {result.Decision}——按拒绝处理（fail-closed）", ToolErrorKind.Forbidden),
        };
    }

    /// <summary>
    /// 策略轴判定（AT-B13）：风险上限与身份闭集——<b>早于授权门禁</b>，拒绝时零调用下游、不切租户。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 与授权门禁的<b>优先级（R3 评审 C-4）</b>：策略在前——宿主显式收敛（如 <c>MaxToolRisk=read</c>）
    /// 应当优先于"工具自身是否有授权器"这一执行细节。两者的拒绝文案前缀固定为
    /// <c>policy_denied:</c> 与 <c>authorization_denied:</c>，使模型/审计可区分"宿主策略禁止"与"授权被拒"。
    /// </para>
    /// <para>
    /// 拒绝原因闭集（写入审计 <c>reason</c> 与模型可见文本）：<c>risk_exceeded</c> / <c>identity_mismatch</c> /
    /// <c>tool_not_allowed</c>（配置非法时 fail-closed）。
    /// </para>
    /// </remarks>
    private string? EvaluatePolicy(FeishuToolDefinition tool)
    {
        if (!FeishuToolRiskNames.TryParse(_options.MaxToolRisk, out var maxRisk))
        {
            // 配置非法：fail-closed（Validate() 应在启动期拦住；此处是运行期最后一道）。
            return $"tool_not_allowed: 宿主配置 {nameof(FeishuAgentOptions.MaxToolRisk)}='{_options.MaxToolRisk}' 非法，"
                + $"合法值为 {FeishuToolRiskNames.AllowedValuesText}——按 fail-closed 拒绝";
        }

        if (tool.Risk > maxRisk)
        {
            return $"risk_exceeded: 工具 '{tool.Name}' 的风险为 {FeishuToolRiskNames.ToLiteral(tool.Risk)}，"
                + $"超过宿主配置上限 '{_options.MaxToolRisk}'";
        }

        if (!_options.AllowedIdentities.Contains(tool.Identity, StringComparer.Ordinal))
        {
            return $"identity_mismatch: 工具 '{tool.Name}' 的身份为 '{tool.Identity}'，"
                + $"不在宿主允许集合 [{string.Join(",", _options.AllowedIdentities)}] 内";
        }

        return null;
    }
}
