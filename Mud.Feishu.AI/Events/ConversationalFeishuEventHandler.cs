// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Diagnostics;

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 会话式事件处理器基类：飞书事件 → 会话 → 模型 → 回复 的入口（路线图主线① M1）。
/// </summary>
/// <remarks>
/// <para>
/// 继承 <see cref="IdempotentFeishuEventHandler{T}"/> 复用 EventId 幂等（同一事件不二次调模型、
/// 不重复扣费）；业务去重键自带命名空间（D 系契约：禁止裸 EventId）。
/// </para>
/// <para>
/// 流程（总体设计 §4）：BuildRequest（事件规范化）→ <see cref="ConversationKeyBuilder"/>
/// → 取/建会话 → 上下文装配 → <c>FeishuAgent.RunAsync</c> → 持久化会话 →
/// <see cref="ReplyAsync"/>。回复机制由派生类实现（SDK 不预设消息通道，
/// 典型实现经 <c>IFeishuTenantV1Message.ReplyMessageAsync</c> 回复）。
/// </para>
/// <para>
/// 流式回复（Phase 2 §3.1）：注入 <see cref="IMessageChannel"/> 且会话主体可解析出 chat_id
/// （群聊默认取 <see cref="ConversationRequest.SubjectId"/>；单聊经
/// <see cref="ResolveStreamTargetChatId"/> 覆写解析，缺省回退非流式）时，改走
/// <c>RunStreamingAsync</c>——通道 Begin 占位 → 增量写入 → Flush 收尾，不再调
/// <see cref="ReplyAsync"/>。通道 Begin 失败回退非流式（模型尚未调用，零重复成本）；
/// 流中失败**先补偿收尾**（用不可取消令牌补一次 <see cref="IMessageChannel.FlushAsync"/>，
/// 把占位消息落到终结态）再向上传播（幂等键回滚，事件重投递）。
/// </para>
/// <para>
/// 多租户（TMA2-20）：<see cref="ConversationRequest.AppKey"/> 进入会话键命名空间，
/// 缺失时的处置由 <see cref="AllowMissingAppKey"/> 分级决定（已装配
/// <see cref="IAppKeyAccessor"/> 的宿主 fail-fast，未装配的单应用宿主降级并告警）；
/// 若此时还装配了工具执行链，则由 <see cref="AllowToolsWithoutAppKey"/> 决定是否 fail-fast（R2-3）。
/// </para>
/// <para>
/// <b>落库 / 回复次序的取舍（R2-8，已决策，勿改次序）</b>：非流式路径<b>先持久化会话、再回复</b>。
/// 两种次序各有失败面，本次序的代价更小——
/// <list type="bullet">
/// <item>本次序：回复下发失败 ⇒ 幂等回滚 ⇒ 重投递<b>重跑模型</b>（多一次计费、历史内部多一轮），
/// 但<b>用户只收到一条</b>回复；</item>
/// <item>反序（先回复后落库）：落库失败 ⇒ 回滚重投递 ⇒ <b>用户收到两条回复</b>。</item>
/// </list>
/// 「用户可见的重复」比「历史内部多一轮（用户不可见）」严重得多，故维持现状。
/// 根治已由 <b>P4-4（outbox）</b>完成：回复在下发<b>之前</b>随会话落盘，重投递时补发同一份文本
/// 而非重跑模型（见 R2 方案 §4.1「P4-4 实施记录」）。次序仍维持「先落库、后回复」——
/// outbox 消除的是「重跑模型」这一环，不是重投递本身（exactly-once 做不到，也不该假装有）。
/// </para>
/// </remarks>
/// <typeparam name="T">强类型事件 DTO（须实现 <see cref="IEventResult"/>）。</typeparam>
public abstract class ConversationalFeishuEventHandler<T>(
    FeishuAgent agent,
    IFeishuEventDeduplicator businessDeduplicator,
    ILogger? logger = null,
    IReadOnlyList<IContextAssembler>? contextAssemblers = null,
    IFeishuToolContextAccessor? toolContextAccessor = null,
    IMessageChannel? messageChannel = null,
    IConversationGate? conversationGate = null,
    IAppKeyAccessor? appKeyAccessor = null,
    IFeishuToolApprovalChannel? approvalChannel = null) : IdempotentFeishuEventHandler<T>(
        businessDeduplicator, logger ?? NullLogger.Instance, appKeyAccessor)
    where T : class, IEventResult, new()
{
    private readonly FeishuAgent _agent = agent ?? throw new ArgumentNullException(nameof(agent));

    /// <summary>
    /// 宿主是否装配了应用键上下文（多应用宿主标志；构造期确定）。
    /// </summary>
    /// <remarks>
    /// Webhook 侧由 <c>FeishuWebhookServiceBuilder</c> 注册 <see cref="IAppKeyAccessor"/>；
    /// WebSocket 侧当前不注册（单应用语义），此时基类
    /// <c>CurrentAppKey</c> 恒为空——本标志用于区分「多应用宿主取不到键（缺陷）」与
    /// 「单应用宿主本就无键（正常）」，见 <see cref="AllowMissingAppKey"/>。
    /// </remarks>
    protected bool HasAppKeyContext { get; } = appKeyAccessor is not null;

    /// <summary>已注册的上下文装配器（按 <see cref="IContextAssembler.Order"/> 排序）。</summary>
    protected IReadOnlyList<IContextAssembler> ContextAssemblers { get; } = contextAssemblers ?? [];

    /// <summary>工具执行上下文访问器（可空；未注入时工具链在执行期拿不到上下文会结构化拒绝）。</summary>
    protected IFeishuToolContextAccessor? ToolContextAccessor { get; } = toolContextAccessor;

    /// <summary>流式回复通道（可空；未注入时保持非流式回复路径，Phase 2 §3.1）。</summary>
    protected IMessageChannel? MessageChannel { get; } = messageChannel;

    /// <summary>
    /// P4-1：宿主人工批准通道（可空；未注册时写工具停在「等待确认」——fail-closed，绝不自动放行）。
    /// </summary>
    protected IFeishuToolApprovalChannel? ApprovalChannel { get; } = approvalChannel;

    /// <summary>
    /// 会话闸门（可空；P2D-1 会话串行化——<c>AddFeishuAgent</c> 默认注册
    /// <c>KeyedConversationGate</c>，多实例部署可替换 Redis 实现。未注入时不串行化，
    /// 保持既有行为）。
    /// </summary>
    protected IConversationGate? ConversationGate { get; } = conversationGate;

    /// <summary>
    /// 把强类型事件规范化为会话请求（群聊/单聊维度选择、会话主体提取）。
    /// </summary>
    /// <param name="eventData">强类型事件 DTO。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>会话请求。</returns>
    protected abstract Task<ConversationRequest> BuildRequestAsync(T eventData, CancellationToken cancellationToken);

    /// <summary>
    /// 把模型回复送达飞书（消息回复/卡片，由派生类选择通道）。
    /// </summary>
    /// <param name="request">会话请求。</param>
    /// <param name="responseText">模型最终回答文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    protected abstract Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken);

    /// <summary>
    /// 下发可行性前置判定（R2-01）：在<b>装配上下文与调用模型之前</b>执行，返回非 <see langword="null"/>
    /// 表示本轮不可投递，基类按「<b>已消费</b>」短路（不调模型、不落历史、不重投递），原因进 Warning 日志。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>要解决的问题</b>：部分事件形态（如 <c>task.task.updated_v1</c>）的回投目标必须经平台 API 解析，
    /// 而"解析不出收件人"若在<b>回复阶段</b>才发现，则本轮已经付过一次完整模型调用（计费 + 延迟 + 一轮历史落库）。
    /// 该判定把"能不能送出去"前移到<b>花钱之前</b>——这是本钩子的唯一目的。
    /// </para>
    /// <para>
    /// <b>为什么用钩子而不是"抛终态异常 + 改基类异常语义"</b>（与方案初稿的偏离，理由如下）：
    /// 基类既有的异常语义是<b>幂等回滚 + 重投递</b>（<see cref="ProcessBusinessLogicAsync"/> 的
    /// at-least-once 契约，被多个用例锁定）；引入"某类异常不重投递"需要改这条全局契约，
    /// 影响面覆盖全部派生处理器。钩子返回值的表达力等价（可携带原因），却<b>零异常语义变更</b>。
    /// </para>
    /// <para>
    /// <b>实现方约定</b>：需要平台 API 解析投递目标时，在
    /// <see cref="BuildRequestAsync"/> 内解析并把目标写入 <see cref="ConversationRequest.SenderId"/>
    /// （或 <see cref="ConversationRequest.SubjectId"/>），本钩子只做纯判定（不再重复 I/O）。
    /// </para>
    /// </remarks>
    /// <param name="request">已规范化的会话请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>不可投递的原因；可投递时为 <see langword="null"/>（默认，即保持既有行为）。</returns>
    protected virtual Task<string?> TryFindDeliveryBlockAsync(ConversationRequest request, CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);

    /// <summary>
    /// 缺少 <see cref="ConversationRequest.AppKey"/> 时是否允许降级到 <c>default</c> 命名空间。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 默认 = <see langword="!HasAppKeyContext"/>：<b>已装配</b> <see cref="IAppKeyAccessor"/> 的宿主
    /// （多应用形态）在事件期取不到 appKey 属配置缺陷，必须 fail-fast——否则不同应用共用同一会话键，
    /// 会话历史跨租户混用（TMA2-20）。
    /// </para>
    /// <para>
    /// <b>完全未装配</b>访问器的宿主（单应用形态，如 WebSocket 既有部署）按既有语义降级并记 Warning，
    /// 避免把「未启用多应用」误判为故障。宿主可覆写本属性显式选择任一侧：
    /// 返回 <see langword="false"/> 即在任何形态下强制 fail-fast。
    /// </para>
    /// </remarks>
    protected virtual bool AllowMissingAppKey => !HasAppKeyContext;

    /// <summary>
    /// 缺少 <see cref="ConversationRequest.AppKey"/> 时是否允许在<b>已装配工具执行链</b>的情况下继续。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 默认 <see langword="false"/>（fail-closed）：工具执行链依赖<b>非空</b> appKey——
    /// <c>IFeishuAppContextScopeFactory.BeginScope</c> 显式拒绝空值，多租户隔离禁止默认应用兜底（TMA2-20）。
    /// 故该形态下工具调用与知识注入<b>必然 100% 失败</b>，若继续则每轮白跑一次模型调用（计费 + 延迟）：
    /// 这是装配缺陷，不得伪装成「正常降级」。
    /// </para>
    /// <para>
    /// <b>判据与误伤面</b>：<see cref="ToolContextAccessor"/> 非空是「工具链已装配」的代理判据，
    /// 无法区分「装配但启用 0 个工具」——该类宿主会被误判。仅当宿主明确接受
    /// 「工具与知识面不可用」时才覆写为 <see langword="true"/>（逃生门）。
    /// </para>
    /// <para>
    /// 另可注册 <c>IAppKeyAccessor</c>（多应用/单应用均可显式提供应用键）从根本上解决（推荐）。
    /// </para>
    /// </remarks>
    protected virtual bool AllowToolsWithoutAppKey => false;

    /// <inheritdoc />
    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        T? eventEntity,
        CancellationToken cancellationToken = default)
    {
        if (eventEntity is null)
        {
            _logger.LogWarning("事件 {EventId} 缺少强类型实体，跳过会话处理", eventData.EventId);
            return;
        }

        var request = await BuildRequestAsync(eventEntity, cancellationToken).ConfigureAwait(false);

        // 多租户隔离（TMA2-20）：appKey 直接进入会话键命名空间，缺失即会让不同应用共用会话历史。
        if (string.IsNullOrWhiteSpace(request.AppKey))
        {
            if (!AllowMissingAppKey)
            {
                throw new InvalidOperationException(
                    "会话请求缺少 AppKey——已装配 IAppKeyAccessor 的宿主禁止默认应用兜底（TMA2-20）；"
                    + "请确认事件通道在派发前设置了应用上下文，或覆写 AllowMissingAppKey 显式承担跨租户风险");
            }

            // 工具执行链已装配 + appKey 缺失 ⇒ fail-fast（R2-3）：工具与知识注入必然 100% 失败，
            // 继续只会每轮白跑一次模型调用，是装配缺陷而非可接受的降级形态。
            if (ToolContextAccessor is not null && !AllowToolsWithoutAppKey)
            {
                throw new InvalidOperationException(
                    "会话请求缺少 AppKey，而工具执行链已装配（ToolContextAccessor 非空）——"
                    + "工具与知识注入依赖非空 appKey（IFeishuAppContextScopeFactory.BeginScope 拒绝空值，TMA2-20），"
                    + "该形态下所有工具调用与知识注入必然失败。"
                    + "请注册 IAppKeyAccessor（多应用/单应用均可显式提供应用键），"
                    + "或覆写 AllowToolsWithoutAppKey => true 显式接受「工具面不可用」的降级语义");
            }

            _logger.LogWarning(
                "会话请求缺少 AppKey，已降级到 default 命名空间（未装配 IAppKeyAccessor，单应用宿主语义；subject: {SubjectId}）",
                request.SubjectId);
        }

        var conversationKey = ConversationKeyBuilder.Build(request.AppKey, request.Scope, request.SubjectId);

        using var activity = FeishuAgentDiagnostics.StartConversationActivity(conversationKey, request.AppKey);

        // 会话闸门（P2D-1）：同键串行、跨键并行；闸门忙时快速失败——
        // 事件层既有幂等回滚 + 重投递机制承接，不静默排队。等待耗时只进 Span 属性（原则 8：键不进 Metrics tag）。
        IConversationGateHandle? gateHandle = null;
        try
        {
            if (ConversationGate is not null)
            {
                var gateStopwatch = System.Diagnostics.Stopwatch.StartNew();
                gateHandle = await ConversationGate.AcquireAsync(conversationKey, cancellationToken).ConfigureAwait(false);
                activity?.AddTag(FeishuAgentDiagnostics.TagGateWaitMs, gateStopwatch.Elapsed.TotalMilliseconds.ToString("0.#", CultureInfo.InvariantCulture));
            }

            var session = await _agent.GetOrCreateSessionAsync(conversationKey, cancellationToken).ConfigureAwait(false);

            // P4-4（outbox）：上一轮「已生成但未确认送达」的回复——本轮重投递命中同一轮次时
            // **补发**而不是重跑模型。省下一次模型计费，且不往历史里多加一轮（根治 R2-8 的取舍代价）。
            if (TryTakePendingReply(session, request, out var pendingReply))
            {
                activity?.AddTag(FeishuAgentDiagnostics.TagReplayed, true);
                await ReplyAsync(request, pendingReply!, cancellationToken).ConfigureAwait(false);

                // 补发成功后落库（条目已在内存中摘除）：失败则异常 ⇒ 幂等回滚 ⇒ 再重投递仍会补发，
                // 语义与既有 at-least-once 一致（不追求 exactly-once，见类注释）。
                await _agent.SaveSessionAsync(conversationKey, session, cancellationToken).ConfigureAwait(false);
                return;
            }

            // 下发可行性前置判定（R2-01）：置于补发判定**之后**（补发不消耗模型，不该被本判定拦截），
            // 模型调用（含上下文装配）**之前**。判定不通过即按「已消费」返回：幂等终态落 Completed，
            // 绝不重投递——重投递只会把同一轮白判一次，对"投递目标不存在"这一事实毫无改善。
            var deliveryBlock = await TryFindDeliveryBlockAsync(request, cancellationToken).ConfigureAwait(false);
            if (deliveryBlock is not null)
            {
                _logger.LogWarning(
                    "本轮不可投递，已在调用模型前短路（未产生模型计费与历史落库；subject: {SubjectId}）：{Reason}",
                    request.SubjectId, deliveryBlock);
                return;
            }

            // 工具执行上下文沿异步流注入：必须在上下文装配**之前**生效——
            // 知识/引用类装配器（如 KnowledgeContextAssembler → AilyKnowledgeProvider）经
            // IFeishuToolContextAccessor 读取 appKey 切租户（多租户隔离禁止默认兜底，TMA2-20）；
            // 顺序颠倒会让装配器抛 InvalidOperationException 并被装配器隔离 catch 吞为 Warning，
            // 表现为「注入模式每轮零知识注入」且仅日志可见。
            // 回复目标优先 request.ChatId（P2D-2b），缺省群聊回退 SubjectId——统一走
            // ResolveStreamTargetChatId 钩子，使派生类覆写一次即对「流式目标」与「工具上下文目标」同时生效。
            using var _toolScope = ToolContextAccessor?.Begin(new FeishuToolContext(
                request.AppKey,
                conversationKey,
                ChatId: ResolveStreamTargetChatId(request),
                UserId: request.SenderId));

            var userMessage = await AssembleUserMessageAsync(request, cancellationToken).ConfigureAwait(false);

            // 空用户消息守卫（P2-7）：空 user 消息会被 OpenAI 兼容端点拒绝（400）且已计费。
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                _logger.LogWarning(
                    "会话用户消息为空（无任何装配器产出），跳过本轮模型调用（subject: {SubjectId}）", request.SubjectId);
                return;
            }

            var (responseText, streamed) = await RunConversationAsync(
                request, session, userMessage, activity, cancellationToken).ConfigureAwait(false);

            // 空模型回复守卫（R2-9）：内容过滤/推理中断时 responseText 为空，
            // 下发空文本会让飞书 API 报错 → 异常 → 幂等回滚 → 重投递重跑模型（重复计费），
            // 且可能反复得到空回复。此处按「已消费」处理：历史已落库，仅跳过下发。
            var shouldDeliver = !streamed && !string.IsNullOrWhiteSpace(responseText);

            // P4-4（outbox）：**下发之前**把待补发文本写进会话一并落盘。
            // 于是「下发失败 → 幂等回滚 → 重投递」这条既有路径，下一轮会命中补发而不是重跑模型。
            // 仅非流式：流式已由通道分片段送达，补发整段会与已送达的半截内容重复（其失败语义由
            // 「补偿 Flush + 回滚」承载，见流式路径注释）。
            if (shouldDeliver)
            {
                SetPendingReply(session, request, responseText);
            }

            // 流式路径回复已部分送达：落库不得使用会被取消的令牌，否则「已送达但未落库」
            // 会与幂等回滚叠加成重复回复。失败路径**不**落库——那是既有 at-least-once 语义
            // （RollbackProcessingAsync → 重投递），改成「失败也落库」等于把「可能重复」换成「确定丢失」。
            await _agent.SaveSessionAsync(
                conversationKey, session, streamed ? CancellationToken.None : cancellationToken).ConfigureAwait(false);

            if (!streamed && string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogWarning("模型返回空回复，跳过下发（subject: {SubjectId}）", request.SubjectId);
                return;
            }

            // 流式路径已由通道送达（Begin/Write/Flush），不再走派生类回复。
            if (!streamed)
            {
                await ReplyAsync(request, responseText, cancellationToken).ConfigureAwait(false);

                // 送达成功 ⇒ 摘除待补发条目并落盘（用不可取消令牌：这一步丢不得，
                // 否则下一轮重投递会再补发一次，用户收到两条）。
                ClearPendingReply(session);
                await _agent.SaveSessionAsync(
                    conversationKey, session, CancellationToken.None).ConfigureAwait(false);
            }
        }
        finally
        {
            // 保存完成后（含失败路径）放行；同键下一事件方可进入。
            gateHandle?.Dispose();
        }
    }

    /// <summary>
    /// 运行一轮对话：注入流式通道且可解析 chat_id 时走流式（Begin → 增量 → Flush），
    /// 否则非流式单次 Run。流式 Begin 失败回退非流式；流中失败向上传播（幂等回滚重投递）。
    /// </summary>
    /// <returns>（模型最终回答文本, 是否已经流式送达）。</returns>
    private async Task<(string ResponseText, bool Streamed)> RunConversationAsync(
        ConversationRequest request,
        AgentSession session,
        string userMessage,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        var streamTarget = MessageChannel is not null ? ResolveStreamTarget(request) : null;
        if (MessageChannel is null || string.IsNullOrEmpty(streamTarget))
        {
            var response = await _agent.RunAsync(userMessage, session, options: null, cancellationToken).ConfigureAwait(false);

            // P4-1：写工具经 ApprovalRequiredAIFunction 包装后，MAF 会在**调用之前**把该次调用
            // 改写成 ToolApprovalRequestContent——此刻工具**未执行**，须由宿主批准后继续。
            // 必须先于 R2-9 空回复守卫判定：这里的语义是「已发起确认」，不是「模型没说话」。
            var pending = ExtractApprovalRequests(response.Messages, request);
            if (pending.Count > 0)
            {
                await NotifyApprovalRequestsAsync(request, pending, cancellationToken).ConfigureAwait(false);
                return (BuildApprovalPendingReply(pending), false);
            }

            return (response.Text, false);
        }

        // 环境量携带会话请求：通道降级链内各子通道按自身语义重解析目标（卡片流=open_id、编辑通道=chat_id），
        // 事件处理器零感知（P2D-2a）。仅流式路径建立，结束即清空。
        using var _streamContext = StreamingRequestContext.Begin(request);

        string messageId;
        try
        {
            messageId = await MessageChannel.BeginAsync(request.AppKey, streamTarget!, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Begin 失败：模型尚未调用，回退非流式（零重复成本，Phase 2 §3.1）。
            _logger.LogWarning(ex, "流式占位消息创建失败，回退非流式回复（appKey: {AppKey}, target: {Target}）",
                request.AppKey, streamTarget);
            var response = await _agent.RunAsync(userMessage, session, options: null, cancellationToken).ConfigureAwait(false);
            return (response.Text, false);
        }

        var fullText = new StringBuilder();
        var pendingApprovals = new List<FrameworkToolApprovalRequest>();
        try
        {
            await foreach (var update in _agent
                .RunStreamingAsync(userMessage, session, options: null, cancellationToken)
                .ConfigureAwait(false))
            {
                // 审批请求同样经流式更新下发（内容是 ToolApprovalRequestContent，无文本增量）。
                CollectApprovalRequests(update.Contents, request, pendingApprovals);

                var delta = update.Text;
                if (string.IsNullOrEmpty(delta))
                {
                    continue;
                }

                fullText.Append(delta);

                // 调用方兜底（R2-7）：契约要求通道实现自带失败隔离，但第三方通道未必遵守——
                // 单次写入失败只跳过该分片，绝不升级为"补偿 Flush + 幂等回滚 + 重投递（模型重复计费）"。
                // 与 BeginAsync 失败「零成本回退非流式」同侧的降级哲学：通道问题不应让模型白跑。
                try
                {
                    await MessageChannel.WriteStreamAsync(request.AppKey, streamTarget!, messageId, delta, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "流式增量写入失败，已跳过该分片（通道未按契约隔离；messageId: {MessageId}）", messageId);
                }
            }

            await MessageChannel.FlushAsync(request.AppKey, streamTarget!, messageId, cancellationToken).ConfigureAwait(false);

            // P4-1：流中出现审批请求时，本轮的下沉内容是「等待确认」而不是模型文本
            // （占位消息已 Flush 到终结态，故这里返回的文本不再经通道送达，由 HandleAsync 走非流式回复）。
            if (pendingApprovals.Count > 0)
            {
                await NotifyApprovalRequestsAsync(request, pendingApprovals, cancellationToken).ConfigureAwait(false);
                return (BuildApprovalPendingReply(pendingApprovals), false);
            }
        }
        catch (OperationCanceledException)
        {
            // R3-14：取消路径补偿收尾——与非取消异常路径一致，必须把占位消息落到终结态，
            // 否则单例字典残留该 messageId 的完整文本（BufferedMessageChannel 的清理只在 FlushAsync 内）。
            // 补偿路径不得使用会被取消的令牌（D15 精神）。
            try
            {
                await MessageChannel.FlushAsync(request.AppKey, streamTarget!, messageId, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception flushEx)
            {
                _logger.LogWarning(flushEx, "流式取消路径补偿收尾失败（messageId: {MessageId}）", messageId);
            }

            // 取消语义不变（不得吞掉）。
            throw;
        }
        catch
        {
            // 补偿收尾（P1-2）：模型已计费、回复已部分送达——必须把占位消息落到终结态，
            // 否则半截内容残留在会话里（通道的既定语义是「停留上一次成功内容」）。
            // 补偿路径不得使用会被取消的令牌，否则补偿本身被取消、问题依旧。
            try
            {
                await MessageChannel.FlushAsync(request.AppKey, streamTarget!, messageId, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception flushEx)
            {
                _logger.LogWarning(flushEx, "流式占位消息收尾失败（messageId: {MessageId}）", messageId);
            }

            // 幂等键回滚语义不变（at-least-once，既有用例锁定）：异常必须继续向上传播。
            throw;
        }

        activity?.AddTag(FeishuAgentDiagnostics.TagStreamed, true);
        return (fullText.ToString(), true);
    }

    /// <summary>
    /// P4-4（outbox）：本轮「轮次标识」——用于判定重投递事件与待补发条目是否同一轮。
    /// </summary>
    /// <remarks>
    /// 消息 ID 是重投递下最稳定的标识（同一事件重投递携带同一 <c>MessageId</c>）；
    /// 无消息 ID 的事件（如某些回调）退化为「发送人 + 提及文本」，避免把所有无 ID 事件
    /// 折叠进同一个补发桶。
    /// </remarks>
    private static string TurnKeyOf(ConversationRequest request)
        => !string.IsNullOrEmpty(request.MessageId)
            ? request.MessageId
            : (request.SenderId ?? string.Empty) + "|" + (request.MentionedText ?? string.Empty);

    /// <summary>
    /// P4-4（outbox）：取出并<b>摘除</b>本轮待补发条目。
    /// </summary>
    /// <returns>命中同一轮次且应补发时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><b>轮次不匹配 ⇒ 陈旧条目</b>：说明用户已发了新消息，补发上一轮的回复属于答非所问 —— 丢弃，
    /// 由调用方正常走模型（回复丢失的后果与修复前一致，不放大）。</item>
    /// <item><b>无论命中与否都先摘除</b>：调用方在补发成功 / 正常跑完一轮后都会落库，
    /// 于是陈旧条目不会在会话里无限驻留。</item>
    /// <item><b>读取必须防御</b>：状态袋值是惰性反序列化的（R2-2），损坏载荷只在首次类型化读取时抛，
    /// 不得让本路径成为新的逃逸点。</item>
    /// </list>
    /// </remarks>
    private bool TryTakePendingReply(AgentSession session, ConversationRequest request, out string? pendingReply)
    {
        pendingReply = null;

        try
        {
            if (!session.StateBag.TryGetValue<string>(
                    FeishuAgent.PendingReplyStateKey, out var stored, null)
                || string.IsNullOrEmpty(stored))
            {
                return false;
            }

            // 轮次标识必须**先读再摘除**——摘除后再读恒为 null，会让同一轮次被误判为「陈旧」而丢弃
            // （本机用例实测：表现为重投递仍然重跑模型，outbox 完全失效且无任何异常）。
            session.StateBag.TryGetValue<string>(
                FeishuAgent.PendingReplyTurnStateKey, out var storedTurn, null);

            session.StateBag.TryRemoveValue(FeishuAgent.PendingReplyStateKey);
            session.StateBag.TryRemoveValue(FeishuAgent.PendingReplyTurnStateKey);

            if (!string.Equals(storedTurn, TurnKeyOf(request), StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "发现陈旧的待补发条目（轮次不匹配），已丢弃——不补发上一轮回复（subject: {SubjectId}）",
                    request.SubjectId);
                return false;
            }

            pendingReply = stored;
            return true;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException
                                     or InvalidOperationException or NotSupportedException)
        {
            // 损坏条目：按 miss 处理（与 R2-2 同一哲学——坏值不该毒化会话）。
            _logger.LogWarning(ex, "待补发条目不可用（已按无待补发处理）");
            return false;
        }
    }

    /// <summary>P4-4（outbox）：把本轮回复写入待补发条目（随下一次落库一起持久化）。</summary>
    private static void SetPendingReply(AgentSession session, ConversationRequest request, string responseText)
    {
        session.StateBag.SetValue(FeishuAgent.PendingReplyStateKey, responseText, null);
        session.StateBag.SetValue(FeishuAgent.PendingReplyTurnStateKey, TurnKeyOf(request), null);
    }

    /// <summary>P4-4（outbox）：摘除待补发条目（送达成功后调用）。</summary>
    private static void ClearPendingReply(AgentSession session)
    {
        session.StateBag.TryRemoveValue(FeishuAgent.PendingReplyStateKey);
        session.StateBag.TryRemoveValue(FeishuAgent.PendingReplyTurnStateKey);
    }

    /// <summary>
    /// P4-1：把一轮模型回应里的 <see cref="ToolApprovalRequestContent"/> 投影为宿主可见的
    /// <see cref="FrameworkToolApprovalRequest"/>（写工具经 <c>ApprovalRequiredAIFunction</c>
    /// 包装后，MAF 在<b>调用之前</b>产出该内容，此时工具<b>尚未执行</b>）。
    /// </summary>
    /// <param name="messages">模型回应消息集合。</param>
    /// <param name="request">规范化会话请求（提供 appKey / user / 会话键）。</param>
    /// <returns>待提交宿主的审批请求（无则空）。</returns>
    private List<FrameworkToolApprovalRequest> ExtractApprovalRequests(
        IEnumerable<ChatMessage> messages, ConversationRequest request)
    {
        var collected = new List<FrameworkToolApprovalRequest>();
        if (messages is null)
        {
            return collected;
        }

        foreach (var message in messages)
        {
            CollectApprovalRequests(message.Contents, request, collected);
        }

        return collected;
    }

    private void CollectApprovalRequests(
        IList<AIContent>? contents, ConversationRequest request, List<FrameworkToolApprovalRequest> sink)
    {
        if (contents is null)
        {
            return;
        }

        foreach (var content in contents)
        {
            if (content is not ToolApprovalRequestContent approval)
            {
                continue;
            }

            // ToolCallContent 是基类；具体形态是 FunctionCallContent（含 Name/Arguments）。
            var toolName = approval.ToolCall is FunctionCallContent call ? call.Name : null;

            sink.Add(new FrameworkToolApprovalRequest(
                RequestId: approval.RequestId,
                ToolName: toolName ?? "(unknown)",
                ToolCallId: approval.ToolCall?.CallId,
                AppKey: request.AppKey,
                UserId: request.SenderId,
                ConversationKey: ConversationKeyBuilder.Build(request.AppKey, request.Scope, request.SubjectId),
                ArgumentsDigest: null,
                RequiredScopes: []));
        }
    }

    /// <summary>
    /// P4-1：把待确认项交给宿主批准通道，并记一条 Warning（便于观测「写工具停在等待确认」）。
    /// </summary>
    /// <remarks>
    /// <b>fail-closed</b>：通道未注册 ⇒ 写工具保持未执行，只提示用户「无法发起确认」——
    /// 绝不自动批准。通道抛异常同理。
    /// </remarks>
    private async Task NotifyApprovalRequestsAsync(
        ConversationRequest request,
        IReadOnlyList<FrameworkToolApprovalRequest> pending,
        CancellationToken cancellationToken)
    {
        foreach (var item in pending)
        {
            _logger.LogWarning(
                "写工具待人工确认（工具: {ToolName}, requestId: {RequestId}, appKey: {AppKey}）——工具尚未执行，宿主批准后回灌批准响应方可继续",
                item.ToolName, item.RequestId, item.AppKey);
        }

        if (ApprovalChannel is null)
        {
            return;
        }

        foreach (var item in pending)
        {
            try
            {
                await ApprovalChannel
                    .RequestFrameworkApprovalAsync(item, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // 与 FeishuToolBinding 同侧哲学：通道故障只降级为「无法发起确认」，不自动放行。
                _logger.LogWarning(ex,
                    "宿主批准通道提交失败（工具: {ToolName}, requestId: {RequestId}）——写工具保持未执行",
                    item.ToolName, item.RequestId);
            }
        }
    }

    /// <summary>
    /// P4-1：构造「等待人工确认」的用户可见答复。
    /// </summary>
    /// <remarks>
    /// 这是会被下发给用户/发回模型的文本，故必须<b>不含任何批准要素</b>
    /// （不含 requestId、不含入参原文、不含任何形式的令牌）——否则「批准只能来自宿主」的前提被破坏。
    /// </remarks>
    protected virtual string BuildApprovalPendingReply(IReadOnlyList<FrameworkToolApprovalRequest> pending)
        => $"以下操作需要人工确认后才能执行：{string.Join("、", pending.Select(p => p.ToolName).Distinct(StringComparer.Ordinal))}。"
           + "已通知审批人，确认后会自动继续。";

    /// <summary>
    /// 解析流式回复目标：通道实现 <see cref="IMessageChannelTargetResolver"/> 时按其语义解析
    /// （卡片流 = 接收用户 open_id）；否则缺省 <see cref="ResolveStreamTargetChatId"/>
    /// （<see cref="ConversationRequest.ChatId"/> 优先，群聊回退会话主体）。
    /// 返回 <see langword="null"/> 表示本次事件不适用流式，回退非流式路径。
    /// </summary>
    /// <param name="request">规范化会话请求。</param>
    /// <returns>流式目标（通道自解释；可空）。</returns>
    protected virtual string? ResolveStreamTarget(ConversationRequest request)
        => MessageChannel is IMessageChannelTargetResolver resolver
            ? resolver.ResolveStreamTarget(request) ?? ResolveStreamTargetChatId(request)
            : ResolveStreamTargetChatId(request);

    /// <summary>
    /// 解析流式回复的目标 chat_id：<see cref="ConversationRequest.ChatId"/> 优先（im 事件恒可用，
    /// 单聊流式因此解锁，P2D-2b）；缺省群聊回退会话主体（chat_id）。返回 <see langword="null"/>
    /// 表示本次事件不适用流式，回退非流式路径。
    /// </summary>
    /// <param name="request">规范化会话请求。</param>
    /// <returns>目标 chat_id（可空）。</returns>
    protected virtual string? ResolveStreamTargetChatId(ConversationRequest request)
        => string.IsNullOrWhiteSpace(request.ChatId) ? (request.Scope.IsGroup ? request.SubjectId : null) : request.ChatId;

    /// <summary>
    /// 覆写业务去重键：自带命名空间（<c>feishu.agent.conversation:{EventId}</c>），禁止裸 EventId。
    /// </summary>
    /// <param name="eventData">事件原始数据。</param>
    /// <returns>业务去重键。</returns>
    protected override string? GetBusinessKey(EventData eventData)
        => string.IsNullOrEmpty(eventData.EventId)
            ? null
            : $"feishu.agent.conversation:{eventData.EventId}";

    /// <summary>
    /// 拼接用户消息：<b>触发文本在前</b>（<see cref="ConversationRequest.MentionedText"/>），
    /// 装配器片段在后；派生类可覆写以改变次序或追加固定前缀。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>R2-01b（本轮审查新增发现，方案初稿未识别）</b>：本方法此前<b>只</b>返回装配器产物——
    /// 于是"事件触发器文本"（每个派生类的 <c>MentionedText</c>：IM 的用户提问、审批/任务变更通知）
    /// <b>永远进不了模型</b>，且在没有装配器的宿主上用户消息恒为空 ⇒ 基类的空消息守卫直接短路，
    /// 模型<b>根本不会被调用</b>。真实缺陷因此比"白跑一次模型"更严重：
    /// <c>TaskUpdatedConversationalEventHandler</c> 与 <c>ApprovalTaskConversationalEventHandler</c>
    /// 是<b>端到端完全 no-op</b>（既不调模型，也不投递），只有
    /// <c>ImMessageConversationalEventHandler</c> 因自行覆写本方法而幸免。
    /// </para>
    /// <para>
    /// <b>为什么修在基类而不是各派生类各补一遍</b>：这正是根因 R-C 的形态——"每个派生类都必须记得
    /// 覆写一次同样的三行"是机制缺失，不是实现疏忽。修在基类后<b>新增派生类自动正确</b>，
    /// 且 <c>ImMessage…</c> 的重复覆写随之删除（同一语义只允许有一处实现）。
    /// </para>
    /// <para>
    /// 空消息守卫（下方 <c>ProcessBusinessLogicAsync</c>）保持不变：它拦的是"装配器与触发文本都空"
    /// 的真实异常形态（会被 OpenAI 兼容端点 400 拒绝且已计费）。
    /// </para>
    /// </remarks>
    /// <param name="request">会话请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户消息文本。</returns>
    protected virtual async Task<string> AssembleUserMessageAsync(ConversationRequest request, CancellationToken cancellationToken)
    {
        var fragments = new List<string>();
        foreach (var assembler in ContextAssemblers.OrderBy(a => a.Order))
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
                _logger.LogWarning(ex, "上下文装配器 {Assembler} 失败，已跳过", assembler.GetType().Name);
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
}
