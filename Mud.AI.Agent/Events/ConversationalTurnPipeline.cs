// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using Mud.AI.Abstractions.Approvals;
using Mud.AI.Abstractions.Channels;
using Mud.AI.Abstractions.Configuration;
using Mud.AI.Abstractions.Context;
using Mud.AI.Abstractions.Conversations;
using Mud.AI.Abstractions.Observability;
using Mud.AI.Agent.Approvals;
using Mud.AI.Agent.Tools;

namespace Mud.AI.Agent.Events;

/// <summary>
/// 中立回合管线：外部事件 → 会话键 → 会话 → 上下文装配 → 模型轮次 → 落库 → 回复 的
/// <b>机制唯一归属</b>（N4 事件管线中立化；自产品适配层 <c>ConversationalFeishuEventHandler</c> 拆分）。
/// </summary>
/// <remarks>
/// <para>
/// <b>11 阶段次序（R2-8 / R5-1 / R5-12 三轮评审产物，一条不许"顺手优化"）</b>：
/// 请求准入校验 → 会话键 → 遥测作用域 → 会话闸门 → 取/建会话 → outbox 补发 →
/// 孤儿审批放弃 → 下发可行性前置判定 → 工具上下文/预算作用域 → 上下文装配 →
/// Run/RunStreaming（含流式补偿收尾）→ 落库 → 回复。
/// </para>
/// <para>
/// <b>产品策略零内建</b>（<c>TurnPipeline_ShouldNotHardcodeProductPolicy</c>）：多租户分级
/// （是否允许缺失租户键、工具链与租户键的组合约束）是<b>产品政策</b>，经
/// <see cref="ConversationalTurnOptions.ValidateRequestPolicies"/> 委托注入（③ 式）；
/// 本类不包含任何此类判断分支。闸门（§5.4.1 定稿）经 ③ 式委托消费——本类不命名任何闸门类型，
/// 闸门满额上抛的产品异常穿过管线原样传播（at-least-once + 幂等回滚承接）。
/// </para>
/// <para>
/// <b>四类补偿路径（不得合并或删减）</b>：
/// <list type="number">
/// <item><b>流式取消</b>：补偿收尾（Flush）必须用 <see cref="CancellationToken.None"/>——补偿本身
/// 不得被取消；取消语义原样保留（重抛）。</item>
/// <item><b>流式异常</b>：同上补偿收尾后异常继续向上传播（幂等键回滚 + 重投递）。</item>
/// <item><b>流式成功后的落库</b>：用 <see cref="CancellationToken.None"/>——「已送达但未落库」会与
/// 幂等回滚叠加成重复回复；失败路径不落库（那是 at-least-once 的重投递语义）。</item>
/// <item><b>补发成功后的落库</b>：用 <see cref="CancellationToken.None"/>——补发条目已在内存中摘除，
/// 这一步丢不得，否则下一轮重投递会再补发一次（用户收到两条）。</item>
/// </list>
/// </para>
/// <para>
/// <b>R2-8 落库/回复次序</b>：非流式路径<b>先持久化会话、再回复</b>（「用户可见的重复」比
/// 「历史内部多一轮」严重）；outbox（下发前写待补发条目）消除的是重投递时的「重跑模型」。
/// </para>
/// <para>
/// <b>R5-1 审批判定先于空回复守卫</b>：非流式单轮是「审批判定先于空回复守卫」纪律的<b>唯一点</b>
/// ——流式 <c>BeginAsync</c> 失败的回退路径必须共用同一回合实现，否则审批请求会被静默丢弃
/// （写工具永久挂起）。本类把该纪律收敛在 <c>RunNonStreamingTurnAsync</c>。
/// </para>
/// <para>
/// <b>R2-01b 触发文本不灭失</b>：内建装配把 <see cref="ConversationRequest.MentionedText"/>
/// 触发文本放在最前、装配器片段在后——每个派生事件处理器的事件触发文本<b>永远进得了模型</b>。
/// </para>
/// </remarks>
public sealed class ConversationalTurnPipeline
{
    private readonly AIAgent _agent;
    private readonly AgentRuntimeOptions _options;
    private readonly IAgentSessionFacade _sessions;
    private readonly Func<ConversationRequest, string> _keyBuilder;
    private readonly IAgentTelemetry _telemetry;
    private readonly ILogger? _logger;
    private readonly IReadOnlyList<IContextAssembler> _assemblers;
    private readonly Func<string, CancellationToken, Task<IDisposable?>>? _acquireGate;
    private readonly IMessageChannel? _channel;
    private readonly IToolApprovalChannel? _approvalChannel;
    private readonly IPendingApprovalStore? _approvalStore;
    private readonly ITurnScopeFactory? _scopeFactory;
    private readonly ConversationalTurnOptions _turnOptions;

    /// <summary>
    /// 初始化回合管线。
    /// </summary>
    /// <param name="agent">模型轮次执行器（MAF <see cref="AIAgent"/>；Run/RunStreaming 的唯一入口）。</param>
    /// <param name="options">② 式中立配置面（预算/流式抑制等运行期旋钮；产品侧经投影构造）。</param>
    /// <param name="sessions">② 式会话取/建/存端口（产品 <c>FeishuAgent</c> 直接实现）。</param>
    /// <param name="keyBuilder">③ 式会话键治理委托（键命名空间是产品策略）。</param>
    /// <param name="telemetry">② 式遥测端口（默认空实现——零开销、零副作用）。</param>
    /// <param name="logger">日志（可空）。</param>
    /// <param name="assemblers">上下文装配器（可空；管线按 <see cref="IContextAssembler.Order"/> 排序）。</param>
    /// <param name="acquireGate">
    /// 会话闸门获取委托（③ 式，§5.4.1 定稿；<see langword="null"/> ⇒ 不串行化）。
    /// 闸门满额上抛的产品异常穿过管线原样传播。
    /// </param>
    /// <param name="channel">流式回复通道（可空；未注入时保持非流式回复路径）。</param>
    /// <param name="approvalChannel">宿主批准通道（可空；未注册时写工具停在「等待确认」——fail-closed）。</param>
    /// <param name="approvalStore">待确认快照存储（可空；未注册时只通知不落库）。</param>
    /// <param name="scopeFactory">产品作用域端口（可空；未注入时不建立任何工具执行上下文）。</param>
    /// <param name="turnOptions">产品事实与产品策略注入面（可空 = 全默认）。</param>
    public ConversationalTurnPipeline(
        AIAgent agent,
        AgentRuntimeOptions options,
        IAgentSessionFacade sessions,
        Func<ConversationRequest, string> keyBuilder,
        IAgentTelemetry? telemetry = null,
        ILogger? logger = null,
        IReadOnlyList<IContextAssembler>? assemblers = null,
        Func<string, CancellationToken, Task<IDisposable?>>? acquireGate = null,
        IMessageChannel? channel = null,
        IToolApprovalChannel? approvalChannel = null,
        IPendingApprovalStore? approvalStore = null,
        ITurnScopeFactory? scopeFactory = null,
        ConversationalTurnOptions? turnOptions = null)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _keyBuilder = keyBuilder ?? throw new ArgumentNullException(nameof(keyBuilder));
        _telemetry = telemetry ?? NoopAgentTelemetry.Instance;
        _logger = logger;
        _assemblers = assemblers ?? [];
        _acquireGate = acquireGate;
        _channel = channel;
        _approvalChannel = approvalChannel;
        _approvalStore = approvalStore;
        _scopeFactory = scopeFactory;
        _turnOptions = turnOptions ?? new ConversationalTurnOptions();
    }

    /// <summary>
    /// 运行一轮会话回合（11 阶段状态机）。
    /// </summary>
    /// <param name="request">已规范化的会话请求（由宿主的事件处理器 <c>BuildRequestAsync</c> 产出）。</param>
    /// <param name="reply">回复下发端口（产品实现；首次回复与 outbox 补发共用）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>回合结果（观测面）。</returns>
    public async Task<TurnOutcome> RunTurnAsync(
        ConversationRequest request,
        IConversationReplySink reply,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (reply is null)
        {
            throw new ArgumentNullException(nameof(reply));
        }

        // ① 请求准入校验（产品策略；可抛异常 fail-fast——多租户分级发生在建立会话作用域之前）。
        _turnOptions.ValidateRequestPolicies?.Invoke(request);

        // ② 会话键（③ 式委托：键命名空间治理是产品策略）。
        var conversationKey = _keyBuilder(request);

        // ③ 遥测作用域（② 式端口：命名面归产品实现；键只进 Span，不入 Metrics tag）。
        using var turn = _telemetry.StartTurn(conversationKey, request.AppKey);

        // ④ 会话闸门（③ 式委托消费；同键串行、跨键并行。等待语义由闸门实现决定，本层不做超时；
        // 等待耗时只进 Span 属性——原则 8：键不进 Metrics tag）。
        IDisposable? gateHandle = null;
        try
        {
            if (_acquireGate is not null)
            {
                var gateStopwatch = Stopwatch.StartNew();
                gateHandle = await _acquireGate(conversationKey, cancellationToken).ConfigureAwait(false);
                _telemetry.GateWaited(turn, gateStopwatch.Elapsed.TotalMilliseconds);
            }

            // ⑤ 取/建会话。
            var session = await _sessions.GetOrCreateSessionAsync(conversationKey, cancellationToken).ConfigureAwait(false);

            // ⑥ P4-4（outbox）：上一轮「已生成但未确认送达」的回复——本轮重投递命中同一轮次时
            // **补发**而不是重跑模型。省下一次模型计费，且不往历史里多加一轮。
            if (TryTakePendingReply(session, request, out var pendingReply))
            {
                _telemetry.Replayed(turn);
                await reply.DeliverAsync(request, pendingReply!, cancellationToken).ConfigureAwait(false);

                // 补发成功后落库（条目已在内存中摘除）：失败则异常 ⇒ 幂等回滚 ⇒ 再重投递仍会补发，
                // 语义与既有 at-least-once 一致（不追求 exactly-once）。
                await _sessions.SaveSessionAsync(conversationKey, session, cancellationToken).ConfigureAwait(false);
                return new TurnOutcome(pendingReply, false, true);
            }

            // ⑦ R5-12：放弃「上一轮发起、至今无人应答」的待确认写操作（位置：补发之后、模型之前——
            // 补发不调模型无需修史；修史必须早于历史进入管线）。不处理的后果：残留的审批请求内容会让
            // 框架的整段入站配对校验在**每一轮**抛异常（事件永久毒化循环），且迟到的批准会执行
            // 一轮已被用户放弃的写操作（越权方向）。
            var pendingState = new PendingApprovalState(_sessions.HistoryStateKey);
            if (pendingState.HasPending(session))
            {
                var abandoned = pendingState.Abandon(session);
                _logger?.LogWarning(
                    "上一轮的人工确认未在续跑轮中回灌，已放弃该待确认操作（摘除 {Removed} 条孤儿审批内容；subject: {SubjectId}）"
                    + "——如需执行该操作请重新发起；迟到的批准将被框架绑定层丢弃（fail-closed）",
                    abandoned, request.SubjectId);

                // 立即落库：修史结果必须持久化，否则下一次重投递会读到仍含孤儿的旧载荷而反复修补。
                await _sessions.SaveSessionAsync(conversationKey, session, cancellationToken).ConfigureAwait(false);
            }

            // ⑧ 下发可行性前置判定（R2-01）：置于补发之后（补发不消耗模型，不该被本判定拦截）、
            // 模型调用（含上下文装配）之前。判定不通过即按「已消费」返回：绝不重投递。
            if (_turnOptions.FindDeliveryBlockAsync is not null)
            {
                var deliveryBlock = await _turnOptions.FindDeliveryBlockAsync(request, cancellationToken).ConfigureAwait(false);
                if (deliveryBlock is not null)
                {
                    _logger?.LogWarning(
                        "本轮不可投递，已在调用模型前短路（未产生模型计费与历史落库；subject: {SubjectId}）：{Reason}",
                        request.SubjectId, deliveryBlock);
                    return TurnOutcome.Empty;
                }
            }

            // ⑨ 工具执行上下文/预算作用域：必须在上下文装配**之前**生效——
            // 知识/引用类装配器经租户上下文切租户（顺序颠倒会让装配器抛错并被隔离吞为 Warning，
            // 表现为「注入模式每轮零知识注入」且仅日志可见）。
            // 回复目标优先 request.ChatId（P2D-2b），缺省群聊回退 SubjectId。
            using var _toolScope = _scopeFactory?.BeginToolScope(new TurnScopeContext(
                request.AppKey,
                conversationKey,
                ChatIdTarget(request),
                request.SenderId,
                request.ThreadId));

            // T4 / D4：单轮工具调用预算——在模型调用前开启、随 using 释放（0 = 不限制，A15 零差异）。
            using var _budgetScope = _options.MaxToolCallsPerTurn > 0
                ? new ToolCallBudget(_options.MaxToolCallsPerTurn).Begin()
                : null;

            // ⑩ 上下文装配（R2-01b：触发文本在前、装配器片段在后）。
            var userMessage = _turnOptions.AssembleUserMessageAsync is not null
                ? await _turnOptions.AssembleUserMessageAsync(request, cancellationToken).ConfigureAwait(false)
                : await AssembleUserMessageDefaultAsync(request, cancellationToken).ConfigureAwait(false);

            // 空用户消息守卫（P2-7）：空 user 消息会被 OpenAI 兼容端点拒绝（400）且已计费。
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                _logger?.LogWarning(
                    "会话用户消息为空（无任何装配器产出），跳过本轮模型调用（subject: {SubjectId}）", request.SubjectId);
                return TurnOutcome.Empty;
            }

            // ⑪ Run / RunStreaming（含流式补偿收尾）。
            var (responseText, streamed) = await RunConversationAsync(
                request, session, userMessage, turn, cancellationToken).ConfigureAwait(false);

            // 空模型回复守卫（R2-9）：内容过滤/推理中断时 responseText 为空——下发空文本会让
            // 飞书类平台 API 报错 → 异常 → 幂等回滚 → 重投递重跑模型（重复计费）。此处按「已消费」
            // 处理：历史已落库，仅跳过下发。
            var shouldDeliver = !streamed && !string.IsNullOrWhiteSpace(responseText);

            // P4-4（outbox）：**下发之前**把待补发文本写进会话一并落盘。于是「下发失败 → 幂等回滚 →
            // 重投递」这条既有路径，下一轮会命中补发而不是重跑模型。仅非流式：流式已由通道分片段送达，
            // 补发整段会与已送达的半截内容重复。
            if (shouldDeliver)
            {
                SetPendingReply(session, request, responseText);
            }

            // 流式路径回复已部分送达：落库不得使用会被取消的令牌（补偿路径 ③），否则「已送达但未落库」
            // 会与幂等回滚叠加成重复回复。失败路径**不**落库——那是既有 at-least-once 语义
            // （重投递），改成「失败也落库」等于把「可能重复」换成「确定丢失」。
            await _sessions.SaveSessionAsync(
                conversationKey, session, streamed ? CancellationToken.None : cancellationToken).ConfigureAwait(false);

            if (!streamed && string.IsNullOrWhiteSpace(responseText))
            {
                _logger?.LogWarning("模型返回空回复，跳过下发（subject: {SubjectId}）", request.SubjectId);
                return new TurnOutcome(responseText, false, false);
            }

            // 流式路径已由通道送达（Begin/Write/Flush），不再走回复端口。
            if (!streamed)
            {
                await reply.DeliverAsync(request, responseText!, cancellationToken).ConfigureAwait(false);

                // 送达成功 ⇒ 摘除待补发条目并落盘（补偿路径 ④：用不可取消令牌——这一步丢不得，
                // 否则下一轮重投递会再补发一次，用户收到两条）。
                ClearPendingReply(session);
                await _sessions.SaveSessionAsync(
                    conversationKey, session, CancellationToken.None).ConfigureAwait(false);
            }

            return new TurnOutcome(responseText, streamed, false);
        }
        finally
        {
            // 保存完成后（含失败路径）放行；同键下一事件方可进入。
            gateHandle?.Dispose();
        }
    }

    /// <summary>
    /// 运行一轮对话：注入流式通道且可解析目标时走流式（Begin → 增量 → Flush），
    /// 否则非流式单次 Run。流式 Begin 失败回退非流式；流中失败向上传播（幂等回滚重投递）。
    /// </summary>
    /// <returns>（模型最终回答文本, 是否已经流式送达）。</returns>
    private async Task<(string ResponseText, bool Streamed)> RunConversationAsync(
        ConversationRequest request,
        AgentSession session,
        string userMessage,
        IDisposable? turn,
        CancellationToken cancellationToken)
    {
        var streamTarget = _channel is not null ? StreamTarget(request) : null;
        if (_channel is null || string.IsNullOrEmpty(streamTarget))
        {
            return await RunNonStreamingTurnAsync(request, session, userMessage, cancellationToken).ConfigureAwait(false);
        }

        // 环境量携带会话请求：通道降级链内各子通道按自身语义重解析目标（卡片流=open_id、编辑通道=chat_id），
        // 管线零感知（P2D-2a）。仅流式路径建立，结束即清空。
        using var _streamContext = StreamingRequestContext.Begin(request);

        string messageId;
        try
        {
            messageId = await _channel.BeginAsync(request.AppKey, streamTarget!, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Begin 失败：模型尚未调用，回退非流式（零重复成本）。
            // R5-1：回退路径必须与主非流式路径共用同一回合实现——否则审批请求在此被静默丢弃
            // （写工具永久挂起、宿主收不到回调、用户收到空回复而被空回复守卫吞掉）。
            _logger?.LogWarning(ex, "流式占位消息创建失败，回退非流式回复（tenantKey: {TenantKey}, target: {Target}）",
                request.AppKey, streamTarget);
            return await RunNonStreamingTurnAsync(request, session, userMessage, cancellationToken).ConfigureAwait(false);
        }

        var fullText = new StringBuilder();
        var pendingApprovals = new List<FrameworkToolApprovalRequest>();
        // T10 / D10：流式文本抑制——DeferTextUntilTurnComplete=true 时缓存全部文本增量，
        // 仅在本轮无工具调用时一次性 Flush；出现 FunctionCallContent 则清空缓冲（并入终态）。
        var deferText = _options.DeferTextUntilTurnComplete;
        var hadToolCall = false;
        try
        {
            await foreach (var update in _agent
                .RunStreamingAsync(userMessage, session, options: null, cancellationToken)
                .ConfigureAwait(false))
            {
                // 审批请求同样经流式更新下发（内容是 ToolApprovalRequestContent，无文本增量）。
                CollectApprovalRequests(update.Contents, request, pendingApprovals);

                // T10：检测工具调用——出现 FunctionCallContent 标记并清空已缓冲文本。
                // 「并入终态」语义：工具调用前的文本不是最终答案，丢弃后等最终文本。
                if (deferText && update.Contents is not null &&
                    update.Contents.Any(static c => c is FunctionCallContent))
                {
                    hadToolCall = true;
                    fullText.Clear();
                    continue;
                }

                var delta = update.Text;
                if (string.IsNullOrEmpty(delta))
                {
                    continue;
                }

                fullText.Append(delta);

                // T10：deferText=true 时延迟到循环结束后一次性推送。
                if (deferText)
                {
                    continue;
                }

                // 调用方兜底（R2-7）：契约要求通道实现自带失败隔离，但第三方通道未必遵守——
                // 单次写入失败只跳过该分片，绝不升级为"补偿 Flush + 幂等回滚 + 重投递（模型重复计费）"。
                try
                {
                    await _channel.WriteStreamAsync(request.AppKey, streamTarget!, messageId, delta, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "流式增量写入失败，已跳过该分片（通道未按契约隔离；messageId: {MessageId}）", messageId);
                }
            }

            // T10：deferText=true 且本轮无工具调用时，一次性推送全部缓存的文本。
            if (deferText && !hadToolCall && fullText.Length > 0)
            {
                try
                {
                    await _channel.WriteStreamAsync(
                        request.AppKey, streamTarget!, messageId, fullText.ToString(), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "流式延迟推送失败（messageId: {MessageId}）", messageId);
                }
            }

            await _channel.FlushAsync(request.AppKey, streamTarget!, messageId, cancellationToken).ConfigureAwait(false);

            // P4-1：流中出现审批请求时，本轮的下沉内容是「等待确认」而不是模型文本
            // （占位消息已 Flush 到终结态，故这里返回的文本不再经通道送达，走回复端口下发）。
            if (pendingApprovals.Count > 0)
            {
                await NotifyApprovalRequestsAsync(pendingApprovals, cancellationToken).ConfigureAwait(false);
                return (BuildApprovalPendingReply(pendingApprovals), false);
            }
        }
        catch (OperationCanceledException)
        {
            // R3-14：取消路径补偿收尾（补偿路径 ①）——与非取消异常路径一致，必须把占位消息落到终结态，
            // 否则单例字典残留该 messageId 的完整文本（缓冲通道的清理只在 FlushAsync 内）。
            // 补偿路径不得使用会被取消的令牌（D15 精神）。
            await CompensatingFlushAsync(request, streamTarget!, messageId).ConfigureAwait(false);

            // 取消语义不变（不得吞掉）。
            throw;
        }
        catch
        {
            // 补偿收尾（P1-2 / 补偿路径 ②）：模型已计费、回复已部分送达——必须把占位消息落到终结态，
            // 否则半截内容残留在会话里（通道的既定语义是「停留上一次成功内容」）。
            // 补偿路径不得使用会被取消的令牌，否则补偿本身被取消、问题依旧。
            await CompensatingFlushAsync(request, streamTarget!, messageId).ConfigureAwait(false);

            // 幂等键回滚语义不变（at-least-once）：异常必须继续向上传播。
            throw;
        }

        _telemetry.Streamed(turn);
        return (fullText.ToString(), true);
    }

    /// <summary>
    /// 流式取消/异常路径的<b>补偿收尾</b>：用不可取消令牌补一次 Flush，把占位消息落到终结态。
    /// 补偿失败只记 Warning——不得掩盖原始异常（回滚与重投递语义依赖它）。
    /// </summary>
    private async Task CompensatingFlushAsync(ConversationRequest request, string streamTarget, string messageId)
    {
        try
        {
            await _channel!.FlushAsync(request.AppKey, streamTarget, messageId, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception flushEx)
        {
            _logger?.LogWarning(flushEx, "流式占位消息补偿收尾失败（messageId: {MessageId}）", messageId);
        }
    }

    /// <summary>
    /// 非流式单轮 Run：「审批判定先于空回复守卫」纪律的<b>唯一点</b>（R5-1）。
    /// </summary>
    /// <remarks>
    /// <b>为什么必须是单一实现</b>：流式 <c>BeginAsync</c> 失败的回退路径共用本方法——
    /// 「每条路径都得记得补审批三行」正是机制缺失。缺陷后果：模型已产出审批请求（工具未执行）、
    /// 回退路径却把空文本当普通回复 ⇒ 命中空回复守卫后按「已消费」收尾——<b>用户收不到确认提示、
    /// 宿主批准通道收不到回调、写工具静默挂起</b>。
    /// </remarks>
    /// <returns>（模型最终回答文本, 是否已经流式送达）——本路径恒为 <see langword="false"/>。</returns>
    private async Task<(string ResponseText, bool Streamed)> RunNonStreamingTurnAsync(
        ConversationRequest request,
        AgentSession session,
        string userMessage,
        CancellationToken cancellationToken)
    {
        var response = await _agent.RunAsync(userMessage, session, options: null, cancellationToken).ConfigureAwait(false);

        // P4-1：写工具经 ApprovalRequiredAIFunction 包装后，框架会在**调用之前**把该次调用
        // 改写成 ToolApprovalRequestContent——此刻工具**未执行**，须由宿主批准后继续。
        var pending = ExtractApprovalRequests(response.Messages, request);
        if (pending.Count > 0)
        {
            await NotifyApprovalRequestsAsync(pending, cancellationToken).ConfigureAwait(false);
            return (BuildApprovalPendingReply(pending), false);
        }

        return (response.Text, false);
    }

    /// <summary>
    /// P4-4（outbox）：本轮「轮次标识」——用于判定重投递事件与待补发条目是否同一轮。
    /// </summary>
    /// <remarks>
    /// 消息 ID 是重投递下最稳定的标识（同一事件重投递携带同一 MessageId）；
    /// 无消息 ID 的事件（如某些回调）退化为「发送人 + 提及文本」，避免把所有无 ID 事件
    /// 折叠进同一个补发桶。
    /// </remarks>
    private static string TurnKeyOf(ConversationRequest request)
        => !string.IsNullOrEmpty(request.MessageId)
            ? request.MessageId
            : (request.SenderId ?? string.Empty) + "|" + (request.MentionedText ?? string.Empty);

    /// <summary>
    /// P4-4（outbox）：取出并<b>摘除</b>本轮待补发条目（状态键未注入 ⇒ outbox 禁用，恒 miss）。
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><b>轮次不匹配 ⇒ 陈旧条目</b>：说明用户已发了新消息，补发上一轮的回复属于答非所问——丢弃，
    /// 由调用方正常走模型（回复丢失的后果与修复前一致，不放大）。</item>
    /// <item><b>无论命中与否都先摘除</b>：调用方在补发成功 / 正常跑完一轮后都会落库，
    /// 于是陈旧条目不会在会话里无限驻留。</item>
    /// <item><b>轮次标识必须先读再摘除</b>——摘除后再读恒为 null，会让同一轮次被误判为「陈旧」而丢弃。</item>
    /// <item><b>读取必须防御</b>：状态袋值是惰性反序列化的，损坏载荷只在首次类型化读取时抛，
    /// 不得让本路径成为新的逃逸点（catch 面**收口**，取消原样传播）。</item>
    /// </list>
    /// </remarks>
    private bool TryTakePendingReply(AgentSession session, ConversationRequest request, out string? pendingReply)
    {
        pendingReply = null;
        var stateKey = _turnOptions.PendingReplyStateKey;
        if (string.IsNullOrEmpty(stateKey))
        {
            return false; // outbox 未配置（中立独立装配形态）——与产品侧未注入状态键时一致。
        }

        try
        {
            if (!session.StateBag.TryGetValue<string>(stateKey, out var stored, null)
                || string.IsNullOrEmpty(stored))
            {
                return false;
            }

            // 先读轮次、再摘除（次序不可颠倒，见 remarks）。
            var turnKey = _turnOptions.PendingReplyTurnStateKey;
            session.StateBag.TryGetValue<string>(turnKey, out var storedTurn, null);

            session.StateBag.TryRemoveValue(stateKey);
            session.StateBag.TryRemoveValue(turnKey!);

            if (!string.Equals(storedTurn, TurnKeyOf(request), StringComparison.Ordinal))
            {
                _logger?.LogWarning(
                    "发现陈旧的待补发条目（轮次不匹配），已丢弃——不补发上一轮回复（subject: {SubjectId}）",
                    request.SubjectId);
                return false;
            }

            pendingReply = stored;
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 损坏条目：按 miss 处理（坏值不该毒化会话）。catch 面**收口**为
            // `when (ex is not OperationCanceledException)`——枚举白名单会漏掉残余异常类型，
            // 使坏载荷每次重投递都在守护区外再抛 ⇒ 事件永久毒化循环。取消必须原样传播。
            _logger?.LogWarning(ex, "待补发条目不可用（已按无待补发处理）");
            return false;
        }
    }

    /// <summary>P4-4（outbox）：把本轮回复写入待补发条目（随下一次落库一起持久化）。</summary>
    private void SetPendingReply(AgentSession session, ConversationRequest request, string responseText)
    {
        var stateKey = _turnOptions.PendingReplyStateKey;
        if (string.IsNullOrEmpty(stateKey))
        {
            return;
        }

        session.StateBag.SetValue(stateKey, responseText, null);
        session.StateBag.SetValue(_turnOptions.PendingReplyTurnStateKey!, TurnKeyOf(request), null);
    }

    /// <summary>P4-4（outbox）：摘除待补发条目（送达成功后调用）。</summary>
    private void ClearPendingReply(AgentSession session)
    {
        var stateKey = _turnOptions.PendingReplyStateKey;
        if (string.IsNullOrEmpty(stateKey))
        {
            return;
        }

        session.StateBag.TryRemoveValue(stateKey);
        session.StateBag.TryRemoveValue(_turnOptions.PendingReplyTurnStateKey!);
    }

    /// <summary>
    /// P4-1：把一轮模型回应里的审批内容投影为宿主可见的待确认请求（投影唯一归属：
    /// <see cref="ApprovalRequestProjector"/>——事件层与 HITL 续跑闭环共用一份）。
    /// </summary>
    private List<FrameworkToolApprovalRequest> ExtractApprovalRequests(
        IEnumerable<ChatMessage> messages, ConversationRequest request)
        => ApprovalRequestProjector.FromMessages(
            messages,
            request.AppKey,
            _keyBuilder(request),
            ChatIdTarget(request),
            request.SenderId);

    private void CollectApprovalRequests(
        IList<AIContent>? contents, ConversationRequest request, List<FrameworkToolApprovalRequest> sink)
        => ApprovalRequestProjector.Collect(
            contents,
            request.AppKey,
            _keyBuilder(request),
            ChatIdTarget(request),
            request.SenderId,
            sink);

    /// <summary>
    /// P4-1：把待确认项交给宿主批准通道，并记一条 Warning（便于观测「写工具停在等待确认」）。
    /// </summary>
    /// <remarks>
    /// <b>fail-closed</b>：通道未注册 ⇒ 写工具保持未执行，只提示用户「无法发起确认」——
    /// 绝不自动批准。通道抛异常同理。
    /// </remarks>
    private Task NotifyApprovalRequestsAsync(
        IReadOnlyList<FrameworkToolApprovalRequest> pending,
        CancellationToken cancellationToken)
        => ApprovalRequestProjector.NotifyAsync(
            _approvalChannel, _approvalStore, _logger, pending, cancellationToken);

    /// <summary>
    /// P4-1：构造「等待人工确认」的用户可见答复（文案委托缺省 ⇒ 内建默认）。
    /// </summary>
    /// <remarks>
    /// 这是会被下发给用户/发回模型的文本，故必须<b>不含任何批准要素</b>
    /// （不含 requestId、不含入参原文、不含任何形式的令牌）——否则「批准只能来自宿主」的前提被破坏。
    /// </remarks>
    private string BuildApprovalPendingReply(IReadOnlyList<FrameworkToolApprovalRequest> pending)
        => _turnOptions.BuildApprovalPendingReply is not null
            ? _turnOptions.BuildApprovalPendingReply(pending)
            : $"以下操作需要人工确认后才能执行：{string.Join("、", pending.Select(p => p.ToolName).Distinct(StringComparer.Ordinal))}。"
              + "已通知审批人，确认后会自动继续。";

    /// <summary>
    /// 解析流式回复目标：通道实现 <see cref="IChannelTargetResolver"/> 时按其语义解析
    /// （卡片流 = 接收用户 open_id）；否则回退目标会话 ID 解析。
    /// 策略委托（<see cref="ConversationalTurnOptions.ResolveStreamTarget"/>）优先。
    /// </summary>
    /// <remarks>
    /// <b>R5-3 语义（两段式解析，P2D-2a）</b>：解析返回 <see langword="null"/> <b>不是</b>
    /// 「放弃流式」，而是回退目标会话 ID 的默认解析值，作为 <c>BeginAsync</c> 的<b>入参兜底目标</b>。
    /// 「本通道对本次事件不适用」这一信号由<b>第二段</b>承接：链内各子通道在 <c>BeginAsync</c> 阶段
    /// 按 <see cref="StreamingRequestContext"/> 环境量二次重解析自身目标，全部子通道不适用时
    /// <c>BeginAsync</c> 抛错 ⇒ 回退非流式。
    /// </remarks>
    private string? StreamTarget(ConversationRequest request)
        => _turnOptions.ResolveStreamTarget is not null
            ? _turnOptions.ResolveStreamTarget(request) ?? ChatIdTarget(request)
            : _channel is IChannelTargetResolver resolver
                ? resolver.ResolveStreamTarget(request) ?? ChatIdTarget(request)
                : ChatIdTarget(request);

    /// <summary>流式回复的目标会话 ID 解析（策略委托优先；缺省 = 内建默认）。</summary>
    private string? ChatIdTarget(ConversationRequest request)
        => _turnOptions.ResolveStreamTargetChatId is not null
            ? _turnOptions.ResolveStreamTargetChatId(request)
            : ResolveDefaultStreamTargetChatId(request);

    /// <summary>
    /// 目标会话 ID 的<b>内建默认</b>解析：<see cref="ConversationRequest.ChatId"/> 优先（im 事件恒可用，
    /// 单聊流式因此解锁）；缺省群聊回退会话主体。公开供产品薄壳的虚方法委托（保持派生类覆写点）。
    /// </summary>
    public string? ResolveDefaultStreamTargetChatId(ConversationRequest request)
        => string.IsNullOrWhiteSpace(request.ChatId) ? (request.Scope.IsGroup ? request.SubjectId : null) : request.ChatId;

    /// <summary>
    /// 流式目标的<b>内建默认</b>解析（通道解析器优先，回退目标会话 ID）。公开供产品薄壳的虚方法委托。
    /// </summary>
    public string? ResolveDefaultStreamTarget(ConversationRequest request)
        => _channel is IChannelTargetResolver resolver
            ? resolver.ResolveStreamTarget(request) ?? ResolveDefaultStreamTargetChatId(request)
            : ResolveDefaultStreamTargetChatId(request);

    /// <summary>
    /// 用户消息的<b>内建默认</b>装配（公开供产品薄壳的虚方法委托）：装配器片段按
    /// <see cref="IContextAssembler.Order"/> 排序，单个装配器失败被隔离（记 Warning 后跳过）；
    /// <b>触发文本在前</b>（R2-01b——事件触发文本永远进得了模型）。
    /// </summary>
    public async Task<string> AssembleDefaultUserMessageAsync(
        ConversationRequest request, CancellationToken cancellationToken)
    {
        var fragments = new List<string>();
        foreach (var assembler in _assemblers.OrderBy(a => a.Order))
        {
            try
            {
                var fragment = await assembler.AssembleAsync(request, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(fragment))
                {
                    // netstandard2.0 的 BCL 不带 IsNullOrWhiteSpace 的 NotNullWhen 注解，需显式断言。
                    fragments.Add(fragment!);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // 单个装配器失败不中断事件处理（异常隔离，对齐事件派发隔离精神）。
                _logger?.LogWarning(ex, "上下文装配器 {Assembler} 失败，已跳过", assembler.GetType().Name);
            }
        }

        var assembled = string.Join("\n", fragments);
        var triggerText = request.MentionedText;
        if (string.IsNullOrWhiteSpace(triggerText))
        {
            return assembled;
        }

        return string.IsNullOrWhiteSpace(assembled)
            ? triggerText!
            : $"{triggerText}\n{assembled}";
    }

    private Task<string> AssembleUserMessageDefaultAsync(ConversationRequest request, CancellationToken cancellationToken)
        => AssembleDefaultUserMessageAsync(request, cancellationToken);
}
