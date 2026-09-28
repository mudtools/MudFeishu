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
/// 根治方案（交付态补发 / outbox：持久化"已生成但未确认送达"的回复文本，重投递优先补发而非重跑模型）
/// 见 R2 方案 §4.1 P4-4，本轮不实现。
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
    IAppKeyAccessor? appKeyAccessor = null) : IdempotentFeishuEventHandler<T>(
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

            // 流式路径回复已部分送达：落库不得使用会被取消的令牌，否则「已送达但未落库」
            // 会与幂等回滚叠加成重复回复。失败路径**不**落库——那是既有 at-least-once 语义
            // （RollbackProcessingAsync → 重投递），改成「失败也落库」等于把「可能重复」换成「确定丢失」。
            await _agent.SaveSessionAsync(
                conversationKey, session, streamed ? CancellationToken.None : cancellationToken).ConfigureAwait(false);

            // 空模型回复守卫（R2-9）：内容过滤/推理中断时 responseText 为空，
            // 下发空文本会让飞书 API 报错 → 异常 → 幂等回滚 → 重投递重跑模型（重复计费），
            // 且可能反复得到空回复。此处按「已消费」处理：历史已落库，仅跳过下发。
            if (!streamed && string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogWarning("模型返回空回复，跳过下发（subject: {SubjectId}）", request.SubjectId);
                return;
            }

            // 流式路径已由通道送达（Begin/Write/Flush），不再走派生类回复。
            if (!streamed)
            {
                await ReplyAsync(request, responseText, cancellationToken).ConfigureAwait(false);
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
        try
        {
            await foreach (var update in _agent
                .RunStreamingAsync(userMessage, session, options: null, cancellationToken)
                .ConfigureAwait(false))
            {
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
        }
        catch (OperationCanceledException)
        {
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
    /// 拼接用户消息：默认仅装配器产物；派生类可覆写追加固定前缀/额外上下文。
    /// </summary>
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

        return string.Join("\n", fragments);
    }
}
