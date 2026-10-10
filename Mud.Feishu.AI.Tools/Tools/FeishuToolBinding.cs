// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools;

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
internal sealed class FeishuToolBinding
{
    /// <summary>
    /// user 身份字面量（R-5：单源取自 <see cref="FeishuToolIdentityNames"/>——
    /// 此前本类私有一份 <c>"user"</c> 字面量，与契约/配置面构成第二真相源）。
    /// </summary>
    private const string IdentityUser = FeishuToolIdentityNames.User;

    private readonly IFeishuAppContextScopeFactory _scopeFactory;
    private readonly FeishuAgentOptions _options;
    private readonly IToolExecutionAuthorizer? _authorizer;
    private readonly IToolResultShaper? _resultShaper;
    private readonly IToolExecutionAuditSink? _auditSink;
    private readonly IFeishuCurrentUserContext? _currentUserContext;

    /// <summary>宿主人工批准通道（R2-1：确认令牌的唯一出模型面出口；未注册时 HITL 降级为纯提示）。</summary>
    private readonly IFeishuToolApprovalChannel? _approvalChannel;

    private readonly ILogger? _logger;
    private readonly Func<DateTimeOffset> _utcClock;

    /// <summary>身份闭集（构造期物化为 HashSet，热路径 O(1) 查找——A1）。</summary>
    private readonly HashSet<string> _allowedIdentities;

    /// <summary>
    /// 重试策略（B3 限流退避）：执行链是唯一消费点——只读工具的 retryable 错误在此自动退避重试，
    /// 写工具默认零重试（防重复副作用）。策略本身是纯函数，此处仅负责"判定 → 等待 → 重放"的编排。
    /// </summary>
    private readonly ToolRetryPolicy _retryPolicy;

    /// <summary>
    /// 初始化 <see cref="FeishuToolBinding"/>。
    /// </summary>
    /// <param name="scopeFactory">租户上下文作用域工厂。</param>
    /// <param name="options">Agent 配置（<see cref="FeishuAgentOptions.EnforceToolAuthorization"/> 消费点）。</param>
    /// <param name="authorizer">工具授权钩子（可空；SDK 不内建策略）。</param>
    /// <param name="resultShaper">结果整形钩子（可空；宿主注册后对投影结果做最终整形，P1D-2a）。</param>
    /// <param name="auditSink">结构化审计出口（可空；注册后投递允许/拒绝/错误三类审计事件，P1D-3b）。</param>
    /// <param name="currentUserContext">
    /// 当前用户上下文（可空；WP5 §5.3——user 身份工具执行期在此写入当前用户，
    /// 使用户令牌缓存查找键可用；<b>执行后必须成对清理</b>，防 AsyncLocal 跨用户泄漏）。
    /// </param>
    /// <param name="approvalChannel">
    /// 宿主人工批准通道（可空；<see cref="IFeishuToolApprovalChannel"/> 是 HITL 的宿主通知出口。
    /// 未注册 = HITL 降级为纯提示：模型只会收到"需要用户确认"，拿不到任何凭据，无法自批复。
    /// DI 场景无需显式注册：未注册的可空构造参数由容器传 <see langword="null"/>。
    /// </param>
    /// <param name="logger">日志（可空）。</param>
    /// <param name="utcClock">UTC 时钟（可空；确认有效期计算用，测试注入固定时钟——R-5）。</param>
    public FeishuToolBinding(
        IFeishuAppContextScopeFactory scopeFactory,
        IOptions<FeishuAgentOptions> options,
        IToolExecutionAuthorizer? authorizer = null,
        IToolResultShaper? resultShaper = null,
        IToolExecutionAuditSink? auditSink = null,
        IFeishuCurrentUserContext? currentUserContext = null,
        IFeishuToolApprovalChannel? approvalChannel = null,
        ILogger<FeishuToolBinding>? logger = null,
        Func<DateTimeOffset>? utcClock = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _authorizer = authorizer;
        _resultShaper = resultShaper;
        _auditSink = auditSink;
        _currentUserContext = currentUserContext;
        _approvalChannel = approvalChannel;
        _logger = logger;
        _utcClock = utcClock ?? (() => DateTimeOffset.UtcNow);
        _allowedIdentities = new HashSet<string>(_options.AllowedIdentities, StringComparer.Ordinal);
        _retryPolicy = new ToolRetryPolicy(_options.ToolRetry ?? new ToolRetryOptions());
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
            return EgressResult(
                tool.Name,
                "工具执行上下文缺少 appKey——多租户隔离禁止默认应用兜底（TMA2-20）");
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
                tool, context, ToolErrorCategory.Validation, ToolErrorSubtype.InvalidArgs, $"invalid_args: {argumentFailure}",
                arguments, executionStopwatch, activity, cancellationToken).ConfigureAwait(false);
        }

        // ② 策略轴（AT-B13）：风险上限 / 身份闭集——早于授权门禁（优先级见 EvaluatePolicy）。
        var policyFailure = EvaluatePolicy(tool);
        if (policyFailure is not null)
        {
            return await DenyAsync(
                tool, context, ToolErrorCategory.Policy, ToolErrorSubtype.ToolNotAllowed, $"policy_denied: {policyFailure}",
                arguments, executionStopwatch, activity, cancellationToken).ConfigureAwait(false);
        }

        // ③ 授权门禁：拒绝时零调用下游、不切换租户上下文。
        var gate = await AuthorizeGateAsync(tool, arguments, context, cancellationToken).ConfigureAwait(false);
        if (!gate.Allowed)
        {
            return await DenyAsync(
                tool, context, gate.Category, gate.Subtype, gate.Reason,
                arguments, executionStopwatch, activity, cancellationToken).ConfigureAwait(false);
        }

        activity?.SetTag(FeishuToolDiagnostics.TagDecision, FeishuToolDiagnostics.DecisionAllowed);

        // ④ 租户上下文切换（先于下游调用，作用域 finally 释放）。
        // ④' user 身份工具（WP5 §5.3）：把当前用户写入 IFeishuCurrentUserContext（AsyncLocal）——
        //     这是用户令牌缓存查找键的来源；执行后 finally 清理。**tenant 路径不得触碰该上下文**，
        //     AsyncLocal 泄漏会让同一异步流内后续请求误用上一个人的令牌（本方案最危险的一处）。
        var userContextRequired = string.Equals(tool.Identity, IdentityUser, StringComparison.Ordinal);
        if (userContextRequired)
        {
            if (_currentUserContext is null)
            {
                return await DenyAsync(
                    tool, context, ToolErrorCategory.Authorization, ToolErrorSubtype.AuthorizationDenied,
                    "authorization_denied: user 身份工具需要 IFeishuCurrentUserContext——宿主装配缺失（须注册 Abstractions 的当前用户上下文）",
                    arguments, executionStopwatch, activity, cancellationToken).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(context.UserId))
            {
                return await DenyAsync(
                    tool, context, ToolErrorCategory.Validation, ToolErrorSubtype.MissingRequired,
                    "invalid_args: user 身份工具要求执行上下文携带当前用户（FeishuToolContext.UserId，取 open_id）——宿主未提供",
                    arguments, executionStopwatch, activity, cancellationToken).ConfigureAwait(false);
            }
        }

        var userContextApplied = false;
        // R3-03：捕获宿主既有用户上下文前值——执行后恢复而非无条件 Clear，
        // 避免抹掉宿主经中间件设置的环境用户身份（后续依赖用户令牌的调用退化为未认证）。
        // 前值为空时仍走 Clear()（既有 "set-user → downstream → clear-user" 断言不变）。
        string? previousOpenId = null;
        string? previousUnionId = null;
        string? previousUserId = null;
        string? previousName = null;
        try
        {
            using var scope = _scopeFactory.BeginScope(context.AppKey);
            if (userContextRequired)
            {
                // 捕获前值（仅 user 身份路径需要；tenant 路径完全不触碰上下文）。
                previousOpenId = _currentUserContext!.OpenId;
                previousUnionId = _currentUserContext.UnionId;
                previousUserId = _currentUserContext.UserId;
                previousName = _currentUserContext.Name;

                // SetUser 的 openId 形参是令牌缓存查找键（未显式传 userId 时回退 openId）——
                // 此处把 context.UserId 同时作为两者，保证"查找键 = 宿主提供的身份"。
                _currentUserContext!.SetUser(context.UserId!, userId: context.UserId);
                userContextApplied = true;
            }

            // ④'' B3 限流退避：只读工具的 retryable 错误在此自动重放（唯一编排点，新增执行器零成本）。
            var result = await InvokeWithRetryAsync(tool, arguments, invokeDownstream, activity, cancellationToken)
                .ConfigureAwait(false);

            // ⑤ 内容安全（AT-F14）：在净化**之前**扫描原始文本（不变量 A9）——净化会剥离控制字符，
            //    而注入载荷常用不可见字符把关键词拆开，先净化就检测不到了。
            var safetyHits = ToolResultContentSafety.Scan(result.ToString(), _options.ContentSafetyMode);
            if (safetyHits.Count > 0 && _options.ContentSafetyMode == ContentSafetyModes.Block)
            {
                return await DenyAsync(
                    tool, context, ToolErrorCategory.ContentSafety, ToolErrorSubtype.InjectedContentBlocked,
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
                // R3-12：降级路径指标化——warn 模式只加标注不阻断，补计数使告警可量化。
                FeishuToolDiagnostics.RecordDegraded(context.AppKey, FeishuMetrics.DegradedReasons.ContentSafetyWarn);
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
            // R2-07：追踪号取本工具 Span 的 Id —— 模型侧提示的号与日志/Span 可关联
            //（"不要为了安全把可调试性一起削掉"，追踪号是白名单化的代价补偿）。
            var trace = activity?.Id ?? Guid.NewGuid().ToString("N").Substring(0, 8);
            _logger?.LogWarning(ex, "工具 {ToolName} 执行失败（appKey: {AppKey}, trace: {Trace}）",
                tool.Name, context.AppKey, trace);

            // WP2（S3 修复）：异常消息可能携带 URL/query/响应体片段，同属"外部数据"——
            // 与正常路径（SanitizeResult）同源处理，经出站净化后再回填模型（纵深防御，保持不变）。
            //
            // R2-07（SC-1 降级后的策略增强）：`ToolResultSanitizer` 是**黑名单**（掩码凭据/PII/控制字符），
            // 它不掩码内网 URL、主机名、SDK 类型名——而这些会随每轮对话进入第三方 LLM 并持久化到会话历史。
            // 故此处把模型出口改为**白名单**：错误分类 + 稳定文案 + 追踪号，原文只进日志与审计。
            // **审计出口不改**（下方 WriteAuditWithIsolationAsync 仍传 ex.Message）：
            // 审计是宿主内网出口，两条出口的判据不同（R1 §7 纪律 4）。
            var (category, subtype) = ToolErrorClassifier.Classify(ex);

            // R-1 / B-2：载荷构造收口到唯一工厂（此前是本类的第 3 份同构拷贝）。
            var errorPayload = ToolErrorFactory.Create(tool.Name, category, subtype, trace: trace);
            var humanReadable = StructuredError(
                tool.Name,
                category,
                $"工具执行异常（{category}，追踪号 {trace}）——请检查参数后重试；若反复失败请联系管理员并提供该追踪号");
            var bounded = ToolResultText.Truncate(ToolResultSanitizer.Sanitize(humanReadable), _options.MaxToolResultLength);

            FeishuToolDiagnostics.RecordExecution(tool.Name, context.AppKey, FeishuMetrics.ToolOutcomes.Error);
            FeishuToolDiagnostics.RecordDuration(tool.Name, context.AppKey, executionStopwatch.ElapsedMilliseconds);
            await WriteAuditWithIsolationAsync(
                tool, context, FeishuMetrics.ToolOutcomes.Error, ex.Message,
                ToolArgsDigester.Digest(arguments), executionStopwatch.ElapsedMilliseconds,
                CancellationToken.None).ConfigureAwait(false);
            return FeishuToolResult.FromError(errorPayload, bounded);
        }
        finally
        {
            // R3-03：user 身份上下文成对清理——恢复宿主既有上下文，而不是无条件抹掉。
            // 前值为空 ⇒ 仍走 Clear()（既有 "set-user → downstream → clear-user" 断言不变）。
            // 前值非空 ⇒ 先 Clear() 再 SetUser(前值)，规避 SetUser 的覆盖告警噪音。
            if (userContextApplied)
            {
                if (string.IsNullOrEmpty(previousOpenId))
                {
                    _currentUserContext?.Clear();
                }
                else
                {
                    _currentUserContext?.Clear();
                    // `!`：`netstandard2.0` 的 `string.IsNullOrEmpty` 无 [NotNullWhen(false)] 注解，
                    // 编译器无法从上方分支推断非空（net6.0+ 无此警告，语义一致）。
                    _currentUserContext?.SetUser(previousOpenId!, previousUnionId, previousUserId, previousName);
                }
            }
        }
    }

    /// <summary>
    /// B3 限流退避（429/5xx/超时的只读自动重试）：<b>唯一编排点</b>——
    /// 策略判定（<see cref="ToolRetryPolicy.ShouldRetry"/>）与延迟计算（<see cref="ToolRetryPolicy.ComputeDelay"/>）
    /// 都是纯函数，本方法只负责"判定 → 等待 → 重放"，使重试语义对所有执行器一次生效（新增工具零成本）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>安全语义</b>（方案 §3.B3）：只读工具遇 <c>retryable</c> 自动重试；写工具<b>默认零重试</b>
    /// （仅在 <c>AllowWriteRetry=true</c> <b>且</b>本调用携带 <c>idempotency_key</c> 时例外）；
    /// <c>dry_run</c> 与授权/策略拒绝不重试（确定性失败）。
    /// </para>
    /// <para>
    /// <b>异常路径</b>：下游抛出的可重试异常（<c>ApiException</c> 429/5xx、<c>HttpRequestException</c>、超时）
    /// 同样参与退避；不可重试的异常原样上抛，交外层 catch 归一（分类/审计语义完全不变）。
    /// </para>
    /// <para>
    /// <b>取消优先</b>：等待期间取消立即传播（<c>Task.Delay</c> 带取消令牌），不留"重试中"的中间态。
    /// </para>
    /// </remarks>
    private async Task<FeishuToolResult> InvokeWithRetryAsync(
        FeishuToolDefinition tool,
        IReadOnlyDictionary<string, object?> arguments,
        Func<CancellationToken, Task<FeishuToolResult>> invokeDownstream,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        if (!_retryPolicy.IsEnabled)
        {
            return await invokeDownstream(cancellationToken).ConfigureAwait(false);
        }

        var isDryRun = ToolDryRun.IsRequested(arguments);
        var hasIdempotencyKey = HasIdempotencyKey(arguments);
        var attempt = 0;

        while (true)
        {
            FeishuToolResult result;
            try
            {
                result = await invokeDownstream(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var (category, subtype) = ToolErrorClassifier.Classify(ex);

                // R-1 / B-2：第 4 份同构拷贝（本处此前内联 new ToolError；tests 的
                // TruncationVisibilityContractGuards.ToolError_ShouldBeConstructedOnlyAtTheSingleFactory
                // 机械拦截任何新的内联构造点）。载荷只用于策略判定，不进模型出口。
                var transient = ToolErrorFactory.Create(
                    tool.Name, category, subtype, attempts: attempt + 1);

                // 不可重试（或已耗尽预算）⇒ 原样上抛，交外层 catch 归一。
                if (!_retryPolicy.ShouldRetry(transient, tool.IsWrite, isDryRun, hasIdempotencyKey, attempt))
                {
                    throw;
                }

                await DelayBeforeRetryAsync(tool, transient, attempt, activity, cancellationToken).ConfigureAwait(false);
                attempt++;
                continue;
            }

            if (!_retryPolicy.ShouldRetry(result.Error, tool.IsWrite, isDryRun, hasIdempotencyKey, attempt))
            {
                // 尝试次数回填进载荷（B3.4）：宿主与模型都能看到"这条结果其实重试过几次"。
                activity?.SetTag(FeishuToolDiagnostics.TagAttempts, attempt + 1);
                return WithAttempts(result, attempt + 1);
            }

            await DelayBeforeRetryAsync(tool, result.Error!, attempt, activity, cancellationToken).ConfigureAwait(false);
            attempt++;
        }
    }

    /// <summary>退避等待（延迟来自策略；<c>retry_after_seconds</c> 存在时优先采用）。</summary>
    private async Task DelayBeforeRetryAsync(
        FeishuToolDefinition tool,
        ToolError error,
        int attempt,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        var delayMs = _retryPolicy.ComputeDelay(attempt, error.RetryAfterSeconds);
        activity?.SetTag(FeishuToolDiagnostics.TagAttempts, attempt + 1);

        _logger?.LogInformation(
            "工具 {ToolName} 命中可重试错误（{Category}/{Subtype}），第 {Attempt} 次重试将于 {DelayMs} ms 后发起",
            tool.Name, error.Category, error.Subtype, attempt + 1, delayMs);

        await _retryPolicy.DelayAsync(delayMs, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 调用方是否传了非空幂等键（写工具重试的唯一安全前提）。
    /// </summary>
    /// <remarks>
    /// 经 <see cref="ToolArgs.OptionalString"/> 读取（而非自写 switch）：参数值形态认知必须收敛到
    /// <c>ToolArgumentNormalizer</c>/<c>ToolArgs</c> 两个入口，各自 switch 是 S1 的根因
    /// （<c>ToolArgumentShapeContractGuards</c> 机械拦截）。
    /// </remarks>
    private static bool HasIdempotencyKey(IReadOnlyDictionary<string, object?> arguments)
        => ToolArgs.OptionalString(arguments, "idempotency_key") is { Length: > 0 };

    /// <summary>把实际尝试次数回填进错误载荷（首行 JSON 重建，人类可读正文原样保留）。</summary>
    private static FeishuToolResult WithAttempts(FeishuToolResult result, int attempts)
    {
        if (result.Error is null || attempts <= 1)
        {
            return result;
        }

        var text = result.ToString();
#if NETSTANDARD2_0
        var newlineIndex = text.IndexOf('\n');
#else
        var newlineIndex = text.IndexOf('\n', StringComparison.Ordinal);
#endif
        var body = newlineIndex >= 0 ? text.Substring(newlineIndex + 1) : string.Empty;
        var payload = result.Error with { Attempts = attempts };
        var jsonLine = ToolErrorPayloadSerializer.Serialize(payload);
        var rebuilt = FeishuToolResult.FromError(payload, body.Length > 0 ? ToolErrorFactory.Compose(payload, body) : jsonLine);

        // 出站净化：重试路径是"新拼出来的模型可见文本"，与成功路径同属出口——
        // 不得因为"原文已净化过"就跳过（纵深防御；ToolEgressPurificationContractGuards 断言本行存在）。
        return SanitizeResult(rebuilt);
    }

    /// <summary>
    /// 统一的拒绝出口（AT-B13 / R3 评审 C-4）：指标 + Span 判定 + 审计投递 + 结构化错误文本，
    /// <b>零调用下游、不切租户</b>。入站净化 / 策略轴 / 授权门禁 / 内容安全阻断四条路径共用，
    /// 确保"新增一个拒绝分支必然带上审计"（否则审计会成为可选步骤而被遗漏）。
    /// </summary>
    /// <remarks>
    /// R3-4：模型可见文案经 <see cref="EgressResult(string, ToolErrorCategory, string, string, int?)"/> 返回——
    /// 与成功路径同源净化 + 截断（审计侧仍记原始 <paramref name="reason"/>，两条出口判据不同）。
    /// </remarks>
    private async Task<FeishuToolResult> DenyAsync(
        FeishuToolDefinition tool,
        FeishuToolContext context,
        ToolErrorCategory category,
        string subtype,
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

        // R3-4：审计仍记**原始** reason（内部事实，非模型可见——R1 §7 纪律 4 的两条出口判据不同），
        // 回填模型的文本则必须经唯一出口净化 + 截断。
        return EgressResult(tool.Name, category, subtype, reason);
    }

    /// <summary>
    /// 模型可见出口的**唯一构造点**（R3-4）：一切回填模型的文本都必须经此净化 + 截断。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 根因（R3-4）：出站净化此前是「成功路径的一段代码」而不是「出口的唯一通道」，
    /// 于是新出口（拒绝、内容安全阻断、HITL 挂起、上下文缺失）天然漏掉净化。
    /// 收口到本方法后，净化成为出口的<b>性质</b>而非可遗忘的步骤。
    /// </para>
    /// <para>
    /// 拒绝文案同属「外部数据 → 模型上下文」的出口：<c>invalid_args: {argumentFailure}</c> 这类
    /// 文案含**模型可控**键名（由 <c>ToolArgumentNormalizer</c> 的 <c>prop.Name</c> 拼成），不得双标。
    /// </para>
    /// </remarks>
    private FeishuToolResult EgressResult(string message)
    {
        var safe = ToolResultText.Truncate(ToolResultSanitizer.Sanitize(message), _options.MaxToolResultLength);
        return FeishuToolResult.FromError(safe);
    }

    /// <summary>带错误语义分类的出口（B2 错误契约：首行 JSON 载荷 + 人类可读正文，经唯一闸门净化 + 截断）。</summary>
    /// <remarks>
    /// R-1 / B-2：载荷构造经 <see cref="ToolErrorFactory"/>(唯一构造点)、文本经
    /// <see cref="ToolResultPipeline.Error"/>（预算与标记单源）；术后净化仍在本方法内施加——
    /// 这是<b>"模型可见"边界</b>的要求（详见 <c>ToolResultPipeline</c> 的 remarks）。
    /// </remarks>
    private FeishuToolResult EgressResult(string toolName, ToolErrorCategory category, string subtype, string reason, int? apiCode = null)
    {
        var payload = ToolErrorFactory.Create(toolName, category, subtype, apiCode);
        var combined = ToolErrorFactory.Compose(payload, StructuredError(toolName, category, reason, apiCode));
        return ToolResultPipeline.FromErrorText(payload, ToolResultSanitizer.Sanitize(combined), _options.MaxToolResultLength);
    }

    /// <summary>无分类的出口（等价 <see cref="StructuredError(string, string)"/> 文案，用于上下文缺失等通用失败）。</summary>
    private FeishuToolResult EgressResult(string toolName, string reason)
        => EgressResult(StructuredError(toolName, reason));

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
            // 有意静默（守卫白名单）：取消是调用方的意图，不是审计故障——记日志只会把"正常取消"变成噪声。
        }
        catch (Exception ex)
        {
            // R3-12：降级路径指标化——审计出口失效对监控不可见，补计数使可告警。
            FeishuToolDiagnostics.RecordDegraded(context.AppKey, FeishuMetrics.DegradedReasons.AuditDeliveryFailed);
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
            // R3-12：降级路径指标化——整形失败回退默认结果，补计数使可观测。
            FeishuToolDiagnostics.RecordDegraded(string.Empty, FeishuMetrics.DegradedReasons.ResultShapingFailed);
            _logger?.LogWarning(ex, "工具结果整形钩子失败，回退默认结果（tool: {ToolName}）", toolName);
            return result;
        }
    }

    /// <summary>构造结构化错误文本（模型可读、带工具名前缀）。</summary>
    public static string StructuredError(string toolName, string reason)
        => $"[tool_error] {toolName}: {reason}";

    /// <summary>
    /// 构造带错误语义分类的结构化错误文本（B2 错误契约：分类 + 原因 + 建议——
    /// 授权拒绝与参数错误可区分，模型据此自我修正：重试 or 换参数 or 放弃）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// AT-B12（R3 评审 C-1）：<see cref="ToolErrorCategory.Confirmation"/> 是<b>独立</b>语义，
    /// 不得复用 <see cref="ToolErrorCategory.Validation"/> 的"请修正参数"后缀——待确认不是参数错，
    /// 让模型去改参数会让它陷入无意义的重试循环。
    /// </para>
    /// <para>
    /// <b>B2 错误契约</b>：此方法返回人类可读正文；结构化 JSON 载荷由调用方
    /// （<see cref="EgressResult(string, ToolErrorCategory, string, string, int?)"/>）
    /// 置于正文首行。老用例的 <c>Contain</c> 断言不受影响（正文形态保持不变）。
    /// </para>
    /// </remarks>
    internal static string StructuredError(string toolName, ToolErrorCategory category, string reason, int? apiCode = null)
    {
        // 无 code 时不产出空槽（保持既有文案形态不变，老用例的 Contain 断言不受影响）。
        var codeSegment = apiCode.HasValue ? $" code={apiCode.Value}" : string.Empty;

        return category switch
        {
            ToolErrorCategory.Retryable => $"[tool_error] {toolName} (retryable){codeSegment}: {reason}——服务端繁忙/网络异常，可稍后重试同一调用",
            ToolErrorCategory.Validation => $"[tool_error] {toolName} (invalid_args){codeSegment}: {reason}——{InvalidArgsNextStep}",
            ToolErrorCategory.Authorization => $"[tool_error] {toolName} (forbidden){codeSegment}: {reason}——授权被拒绝，请放弃或改用只读方案",
            // R2-1（P0）：删除「若结果中提供了确认令牌…重试即可继续」——该文案把批准所需的全部要素
            // 交给了模型，使模型可自行带令牌重试并放行写操作（HITL 退化为「取决于模型是否听话」）。
            // 确认令牌只经 IFeishuToolApprovalChannel 交给宿主，模型侧恒为中性语义。
            ToolErrorCategory.Confirmation => $"[tool_error] {toolName} (needs_confirmation){codeSegment}: {reason}——该操作需要用户确认后方可执行；"
                + "已交由宿主确认通道处理，未获得确认前不得重试同一调用，请告知用户确认进度",
            // Api（default 分支）：此前**完全没有下一步**（F-8 点名的两态之一）。
            _ => $"[tool_error] {toolName} (api_error){codeSegment}: {reason}——{ApiErrorNextStep}",
        };
    }

    /// <summary>
    /// R5 / F-8：<c>invalid_args</c> 的<b>可执行下一步</b>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 官方 <c>lark-cli</c> 的 <c>Suggestions []string</c> 注释写明其用途是
    /// "so an agent can retry <b>without parsing the human-facing hint</b>"。
    /// 本仓的对应形态是：<b>F-2 的闭集</b>让"非法取值"类错误天然带合法值清单
    /// （<c>ToolArgs.RequireNamedInt</c> 已输出"合法取值：…"），<b>B-6 的 anyOf</b>
    /// 则让"缺一"类错误天然带构造模板。本后缀只补<b>这两者都覆盖不到</b>的剩余情形
    /// （形状错、类型错、必填缺失），并明确指向上述机器可读来源。
    /// </para>
    /// <para>
    /// 刻意<b>不</b>在这里重复罗列取值：那会与闭集读取器输出的具体清单形成第二份真相源。
    /// </para>
    /// </remarks>
    private const string InvalidArgsNextStep =
        "请修正参数后重试：取值范围见工具参数描述中的 enum 闭集（非法取值时错误会直接列出全部合法值）；"
        + "多个参数'至少提供一个'的约束见 anyOf；仍是形状/类型错误请对照参数描述的类型重填";

    /// <summary>
    /// R5 / F-8：<c>api_error</c> 的<b>可执行下一步</b>。
    /// </summary>
    /// <remarks>
    /// 该态此前是纯 <c>{reason}</c>，模型只能盲试。补上后可形成确定的三步路径：
    /// 先核实只读事实 → 再重试 → 仍失败则带上 code 上报。
    /// </remarks>
    private const string ApiErrorNextStep =
        "该调用已被下游拒绝。建议顺序：① 先用同域只读工具核实目标是否存在/参数是否正确；"
        + "② 确认无误后重试同一调用；③ 仍失败请把上面的 code 一并提供给用户（code 是排查所需的唯一标识）";

    /// <summary>按飞书业务 code 分类构造错误回填（<c>FeishuApiOutcome</c> 解包路径共用；分类器 internal，宿主不可见）。</summary>
    internal static string StructuredError(string toolName, int? apiCode, string reason)
        => StructuredError(toolName, ToolErrorClassifier.ClassifyCode(apiCode).Category, reason, apiCode);

    /// <summary>
    /// 将 <see cref="ToolErrorCategory"/> 映射为 JSON 载荷中的 <c>category</c> 字面量。
    /// </summary>
    internal static string CategoryLiteral(ToolErrorCategory category) => category switch
    {
        ToolErrorCategory.Validation => "validation",
        ToolErrorCategory.Policy => "policy",
        ToolErrorCategory.Authorization => "authorization",
        ToolErrorCategory.Confirmation => "confirmation",
        ToolErrorCategory.Retryable => "retryable",
        ToolErrorCategory.Api => "api",
        ToolErrorCategory.Internal => "internal",
        ToolErrorCategory.ContentSafety => "content_safety",
        _ => "internal",
    };

    /// <summary>授权门禁判定结果（AT-B12：<see cref="ToolErrorCategory"/> 随判定一起返回，避免调用方猜测语义）。</summary>
    private readonly record struct GateDecision(bool Allowed, string Reason, ToolErrorCategory Category, string Subtype)
    {
        /// <summary>放行。</summary>
        public static GateDecision Pass() => new(true, string.Empty, ToolErrorCategory.Api, string.Empty);

        /// <summary>拒绝（带语义分类）。</summary>
        public static GateDecision Deny(string reason, ToolErrorCategory category, string subtype) => new(false, reason, category, subtype);
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
                    ToolErrorCategory.Authorization, ToolErrorSubtype.AuthorizationDenied);
            }

            // 只读工具默认 NotRequired（授权钩子预留；scopes 随 Schema 供宿主审计）。
            return GateDecision.Pass();
        }

        var result = await _authorizer
            .AuthorizeAsync(tool.Name, tool.RequiredScopes, tool.IsWrite, arguments, context, cancellationToken)
            .ConfigureAwait(false);
        if (result is null)
        {
            return GateDecision.Deny("authorization_denied: 授权器返回空结果——按拒绝处理（fail-closed）", ToolErrorCategory.Authorization, ToolErrorSubtype.AuthorizationDenied);
        }

        // R4-1（P0）：删除原「写类工具 NeedsUserConfirmation ⇒ Pass()」特例。
        //
        // 原依据是注释里的假设——「写工具经 ApplyApprovalGate 包装为 ApprovalRequiredAIFunction，
        // 框架不批准则本方法根本不会被调用 ⇒ 走到这里即意味着人已批准」。该假设在**运行期无任何校验**：
        // ApprovalRequiredAIFunction 是 MEAI **纯标记类型**，拦截只在 FunctionInvokingChatClient
        // 内部生效；宿主直接 InvokeAsync、或模型/调用方绕开该管线时，写工具会**静默放行**。
        //
        // 现在一律 fail-closed：批准状态的唯一所有者是宿主 IToolExecutionAuthorizer——
        // 未返回 Allowed 的待确认调用，无论读/写，都走无令牌版挂起解析（通知宿主通道 + 中性拒绝）。
        // 宿主在 MAF 批准回调中更新授权器状态后，下一次调用自然返回 Allowed。
        return result.Decision switch
        {
            AuthorizationDecision.Allowed => GateDecision.Pass(),
            AuthorizationDecision.Denied => GateDecision.Deny(
                $"authorization_denied: {result.Reason ?? "授权被拒绝"}", ToolErrorCategory.Authorization, ToolErrorSubtype.AuthorizationDenied),
            // WP3 后无令牌版：读写工具共用同一条挂起解析路径（SDK 不签发、不校验任何凭据）。
            AuthorizationDecision.NeedsUserConfirmation =>
                await ResolveNeedsConfirmationAsync(tool, arguments, context, result.Reason, cancellationToken)
                    .ConfigureAwait(false),
            _ => GateDecision.Deny(
                $"authorization_denied: 未知授权判定 {result.Decision}——按拒绝处理（fail-closed）", ToolErrorCategory.Authorization, ToolErrorSubtype.AuthorizationDenied),
        };
    }

    /// <summary>
    /// NeedsUserConfirmation 的挂起解析（WP3 后无令牌版，R4-1 起<b>读写工具共用</b>）：
    /// 批准状态的唯一所有者是宿主授权器（<see cref="IToolExecutionAuthorizer"/>）——
    /// SDK 不签发、不校验任何凭据。首次调用 → 通知宿主批准通道（携带参数摘要，供宿主建立"已批准"上下文）
    /// 并<b>中性拒绝</b>；宿主批准后由授权器在下一次调用时返回 <c>Allowed</c>——模型侧恒为中性语义，
    /// 不存在可自批复的凭据。
    /// </summary>
    /// <remarks>
    /// 写类工具同样走本路径：MAF 的 <c>ApprovalRequiredAIFunction</c> 只是「调用前提醒」，
    /// 不是执行链可验证的批准证据，故不得据此放行（R4-1 删除的 <c>Pass()</c> 特例）。
    /// </remarks>
    private async Task<GateDecision> ResolveNeedsConfirmationAsync(
        FeishuToolDefinition tool,
        IReadOnlyDictionary<string, object?> arguments,
        FeishuToolContext context,
        string? reason,
        CancellationToken cancellationToken)
    {
        var reasonText = reason ?? "未提供原因";

        // 未注册通道 = 纯提示（fail-closed），模型只收到中性拒绝文案。
        if (_approvalChannel is null)
        {
            return GateDecision.Deny(
                $"需要用户确认后才能执行（HITL）：{reasonText}", ToolErrorCategory.Confirmation, ToolErrorSubtype.NeedsUserConfirmation);
        }

        string? approvalId = null;
        try
        {
            approvalId = await _approvalChannel.RequestApprovalAsync(
                new ToolApprovalRequest(
                    tool.Name,
                    context.AppKey,
                    context.UserId,
                    context.ConversationKey,
                    ToolArgsDigester.Digest(arguments),
                    tool.RequiredScopes,
                    reason,
                    _utcClock() + ConfirmationLifetime),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // 通道失败必须降级为纯提示，绝不影响执行链安全。
            _logger?.LogWarning(ex, "工具批准通道提交失败，已降级为纯提示（tool: {ToolName}）", tool.Name);
        }

        return GateDecision.Deny(
            $"需要用户确认后才能执行（HITL）：{reasonText}"
            + (approvalId is null
                ? "；已在宿主侧发起确认，请等待用户批准"
                : $"；已在宿主侧发起确认（关联号 {approvalId}），请等待用户批准"),
            ToolErrorCategory.Confirmation, ToolErrorSubtype.NeedsUserConfirmation);
    }

    /// <summary>确认有效期（宿主据此判定多久未批准即视为放弃；原 T4-2 建议 10 分钟，WP3 内联为常量）。</summary>
    private static readonly TimeSpan ConfirmationLifetime = TimeSpan.FromMinutes(10);

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
            return $"{ToolErrorSubtype.ToolNotAllowed}: 宿主配置 {nameof(FeishuAgentOptions.MaxToolRisk)}='{_options.MaxToolRisk}' 非法，"
                + $"合法值为 {FeishuToolRiskNames.AllowedValuesText}——按 fail-closed 拒绝";
        }

        if (tool.Risk > maxRisk)
        {
            return $"{ToolErrorSubtype.RiskExceeded}: 工具 '{tool.Name}' 的风险为 {FeishuToolRiskNames.ToLiteral(tool.Risk)}，"
                + $"超过宿主配置上限 '{_options.MaxToolRisk}'";
        }

        if (!_allowedIdentities.Contains(tool.Identity))
        {
            return $"{ToolErrorSubtype.IdentityMismatch}: 工具 '{tool.Name}' 的身份为 '{tool.Identity}'，"
                + $"不在宿主允许集合 [{string.Join(",", _options.AllowedIdentities)}] 内"
                + $"（配置键 {FeishuAgentOptions.SectionName}:{nameof(FeishuAgentOptions.AllowedIdentities)}）";
        }

        return null;
    }
}
