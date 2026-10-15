// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Mud.AI.Agent.Events;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 会话式事件处理器基类：飞书事件 → 会话 → 模型 → 回复 的入口。
/// </summary>
/// <remarks>
/// 本类是<b>薄壳</b>：11 阶段回合状态机与四类补偿路径在中立 <see cref="ConversationalTurnPipeline"/>，
/// 这里只留产品事实与产品策略——事件规范化、回复下发（<see cref="IConversationReplySink"/> 端口，
/// 首次回复与 outbox 补发共用）、多租户分级（<see cref="ConversationalTurnOptions.ValidateRequestPolicies"/>
/// 委托注入，管线零产品政策）、可投递性前置判定与会话键治理。
/// <para>
/// 次序约束（勿改）：①非流式<b>先落库、再回复</b>——反序会让用户收到两条；②流式 Begin 失败回退非流式，
/// 流中失败须先用<b>不可取消令牌</b>补一次 <see cref="IMessageChannel.FlushAsync"/>，把占位消息落到终结态
/// 再向上传播。
/// </para>
/// <para>
/// 幂等沿用 <see cref="IdempotentFeishuEventHandler{T}"/>（同一事件不二次调模型、不重复扣费）；
/// 业务去重键自带命名空间，禁止裸 EventId。
/// </para>
/// </remarks>
/// <typeparam name="T">强类型事件 DTO（须实现 <see cref="IEventResult"/>）。</typeparam>
public abstract class ConversationalFeishuEventHandler<T> : IdempotentFeishuEventHandler<T>
    where T : class, IEventResult, new()
{
    private readonly FeishuAppContextScopeHolder _scopeHolder;
    private readonly ConversationalTurnPipeline _pipeline;
    private readonly IConversationReplySink _replySink;

    /// <summary>
    /// 宿主是否装配了应用键上下文（多应用宿主标志；构造期确定）。
    /// </summary>
    /// <remarks>
    /// Webhook 侧由 <c>FeishuWebhookServiceBuilder</c> 注册 <see cref="IAppKeyAccessor"/>；
    /// WebSocket 侧当前不注册（单应用语义），此时 <c>CurrentAppKey</c> 恒为空——本标志用于区分
    /// 「多应用宿主取不到键（缺陷）」与「单应用宿主本就无键（正常）」，见 <see cref="AllowMissingAppKey"/>。
    /// </remarks>
    protected bool HasAppKeyContext { get; }

    /// <summary>已注册的上下文装配器（按 <see cref="IContextAssembler.Order"/> 排序）。</summary>
    protected IReadOnlyList<IContextAssembler> ContextAssemblers { get; }

    /// <summary>工具执行上下文访问器（可空；未注入时工具链在执行期拿不到上下文会结构化拒绝）。</summary>
    protected IFeishuToolContextAccessor? ToolContextAccessor { get; }

    /// <summary>流式回复通道（可空；未注入时保持非流式回复路径）。</summary>
    protected IMessageChannel? MessageChannel { get; }

    /// <summary>
    /// P4-1：宿主人工批准通道（可空；未注册时写工具停在「等待确认」——fail-closed，绝不自动放行）。
    /// </summary>
    protected IFeishuToolApprovalChannel? ApprovalChannel { get; }

    /// <summary>
    /// R7 / C4a：待确认快照存储（可空；未注册时只通知不落库——宿主重进进程后列不出待办，
    /// 但写工具仍保持未执行，fail-closed 不变）。
    /// </summary>
    protected IPendingApprovalStore? PendingApprovalStore { get; }

    /// <summary>
    /// 会话闸门（可空；P2D-1 会话串行化——<c>AddFeishuAgent</c> 默认注册
    /// <c>KeyedConversationGate</c>，多实例部署可替换 Redis 实现。未注入时不串行化）。
    /// </summary>
    protected IConversationGate? ConversationGate { get; }

    /// <summary>
    /// 初始化会话式事件处理器（构造签名与参数名保持源码兼容——派生处理器与测试派生类零改动）。
    /// </summary>
    /// <param name="agent">飞书 Agent（同时是中立 <see cref="IAgentSessionFacade"/> 会话端口实现）。</param>
    /// <param name="businessDeduplicator">业务层去重服务。</param>
    /// <param name="logger">日志（可空）。</param>
    /// <param name="contextAssemblers">上下文装配器（可空）。</param>
    /// <param name="toolContextAccessor">工具执行上下文访问器（可空）。</param>
    /// <param name="messageChannel">流式回复通道（可空）。</param>
    /// <param name="conversationGate">会话闸门（可空）。</param>
    /// <param name="appKeyAccessor">应用键上下文访问器（可空）。</param>
    /// <param name="approvalChannel">宿主批准通道（可空）。</param>
    /// <param name="appContextScopeFactory">租户作用域工厂（可空；下发动作前切租户的 fail-closed 防线）。</param>
    /// <param name="pendingApprovalStore">待确认快照存储（可空）。</param>
    protected ConversationalFeishuEventHandler(
        FeishuAgent agent,
        IFeishuEventDeduplicator businessDeduplicator,
        ILogger? logger = null,
        IReadOnlyList<IContextAssembler>? contextAssemblers = null,
        IFeishuToolContextAccessor? toolContextAccessor = null,
        IMessageChannel? messageChannel = null,
        IConversationGate? conversationGate = null,
        IAppKeyAccessor? appKeyAccessor = null,
        IFeishuToolApprovalChannel? approvalChannel = null,
        IFeishuAppContextScopeFactory? appContextScopeFactory = null,

        // R7 / C4a：待确认快照存储（可空 ⇒ 退化为「只通知不落库」）。
        // 追加为**末尾可选参数**：既有派生处理器的位置参数与命名参数调用点全部源码兼容；
        // DI 侧由容器按「可选参数默认值」语义处理（未注册 ⇒ null，不抛解析失败）。
        IPendingApprovalStore? pendingApprovalStore = null)
        : base(businessDeduplicator, logger ?? NullLogger.Instance, appKeyAccessor)
    {
        _scopeHolder = new FeishuAppContextScopeHolder(appContextScopeFactory);
        HasAppKeyContext = appKeyAccessor is not null;
        ContextAssemblers = contextAssemblers ?? [];
        ToolContextAccessor = toolContextAccessor;
        MessageChannel = messageChannel;
        ApprovalChannel = approvalChannel;
        PendingApprovalStore = pendingApprovalStore;
        ConversationGate = conversationGate;

        // N4：回合机制整体交由中立管线（闸门经 ③ 式委托消费——ConversationGate 为 null 时
        // 委托返回 null = 不串行化，与既有语义逐字节一致）；产品事实（outbox 状态键字面量）与
        // 产品策略（多租户分级 / 投递判定 / 目标解析 / 装配 / 审批文案）以委托形态注入，
        // 方法组绑定到虚方法 ⇒ 派生类覆写自动生效。
        _pipeline = new ConversationalTurnPipeline(
            agent,
            agent.RuntimeOptions,
            agent,
            keyBuilder: request => ConversationScopeMapper.BuildKey(request.AppKey, request.Scope, request.SubjectId),
            telemetry: FeishuAgent.AgentTelemetry,
            logger,
            contextAssemblers,
            acquireGate: conversationGate is null
                ? null
                : async (key, ct) => await conversationGate.AcquireAsync(key, ct).ConfigureAwait(false),
            channel: messageChannel,
            approvalChannel: approvalChannel,
            approvalStore: pendingApprovalStore,
            scopeFactory: toolContextAccessor is null
                ? null
                : new FeishuToolContextScopeFactory(toolContextAccessor),
            turnOptions: new ConversationalTurnOptions
            {
                PendingReplyStateKey = FeishuAgent.PendingReplyStateKey,
                PendingReplyTurnStateKey = FeishuAgent.PendingReplyTurnStateKey,
                ValidateRequestPolicies = ValidateRequestPolicies,
                FindDeliveryBlockAsync = TryFindDeliveryBlockAsync,
                ResolveStreamTarget = ResolveStreamTarget,
                ResolveStreamTargetChatId = ResolveStreamTargetChatId,
                AssembleUserMessageAsync = AssembleUserMessageAsync,
                BuildApprovalPendingReply = BuildApprovalPendingReply,
            });

        _replySink = new ConversationReplySink(this);
    }

    /// <summary>
    /// 把强类型事件规范化为会话请求（群聊/单聊维度选择、会话主体提取）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>租户作用域义务（R5-9）</b>：本方法在管线建立<b>任何</b>租户作用域<b>之前</b>执行
    /// （<see cref="BeginAppScope"/> 的 fail-closed 防线只覆盖「下发动作」）。实现内如需调用
    /// 飞书 API（如 <c>task.task.updated_v1</c> 按平台解析回投目标），必须先以
    /// <c>BeginAppScope(从事件中提取的 appKey)</c> 建立租户作用域——否则多应用宿主下该解析请求
    /// 会以<b>默认应用身份</b>发出（TMA2-20 跨租户错发的「读」方向）。
    /// </para>
    /// <para>
    /// 需要平台解析时，把结果写入 <see cref="ConversationRequest.SenderId"/> /
    /// <see cref="ConversationRequest.SubjectId"/>，<see cref="TryFindDeliveryBlockAsync"/> 只做纯判定。
    /// </para>
    /// </remarks>
    /// <param name="eventData">强类型事件 DTO。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>会话请求。</returns>
    protected abstract Task<ConversationRequest> BuildRequestAsync(T eventData, CancellationToken cancellationToken);

    /// <summary>
    /// 把模型回复送达飞书（消息回复/卡片，由派生类选择通道）。
    /// </summary>
    /// <remarks>
    /// 经 <see cref="IConversationReplySink"/> 端口交由管线回调——首次回复与 outbox 补发共用本钩子。
    /// 实现内<b>必须</b>先 <see cref="BeginAppScope"/>（R3-1：回复前切到事件的租户上下文）。
    /// </remarks>
    /// <param name="request">会话请求。</param>
    /// <param name="responseText">模型最终回答文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    protected abstract Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken);

    /// <summary>
    /// 为「调用飞书客户端产生对外副作用」的动作建立 SDK 租户作用域（R3-1/R3-2）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>必须</b>在任何下发动作<b>之前</b>调用（回复消息、上传附件、更新卡片等）：生成的客户端在
    /// 无环境上下文时会退化为默认应用身份，多应用宿主下即以<b>默认应用身份</b>把回复发给别的租户
    /// （TMA2-20 跨租户错发）。
    /// </para>
    /// <para>
    /// <b>fail-closed</b>：有 appKey 却未注入 <see cref="IFeishuAppContextScopeFactory"/> 属装配缺陷，
    /// 显式抛错而**不**回退默认应用——宁可失败，也不以其他租户身份发出。
    /// appKey 为空时返回 <see langword="null"/>（单应用宿主语义，由 <see cref="AllowMissingAppKey"/>
    /// 分级决定是否允许），此时行为与既有实现<b>零差异</b>。
    /// </para>
    /// </remarks>
    /// <param name="appKey">会话请求携带的应用键（可空）。</param>
    /// <returns>作用域；appKey 为空时为 <see langword="null"/>（调用方以 <c>using</c> 承接即可）。</returns>
    /// <exception cref="InvalidOperationException">appKey 非空但未注入作用域工厂。</exception>
    protected IDisposable? BeginAppScope(string? appKey) => _scopeHolder.BeginAppScope(appKey);

    /// <summary>
    /// 下发可行性前置判定：在<b>装配上下文与调用模型之前</b>执行，返回非 <see langword="null"/> 表示本轮
    /// 不可投递，管线按「<b>已消费</b>」短路（不调模型、不落历史、不重投递），原因进 Warning 日志。
    /// </summary>
    /// <remarks>
    /// 部分事件形态（如 <c>task.task.updated_v1</c>）的回投目标须经平台 API 解析，若到<b>回复阶段</b>才发现
    /// 解析不出收件人，本轮已付过一次完整模型调用；本钩子的唯一目的就是把「能不能送出去」前移到<b>花钱之前</b>。
    /// <para>
    /// 用钩子而不是「抛终态异常」：基类异常语义是幂等回滚 + 重投递（at-least-once，被多个用例锁定），
    /// 加一类「不重投递的异常」等于改全局契约；钩子表达力等价（可携带原因）且<b>零异常语义变更</b>。
    /// </para>
    /// <para>
    /// <b>实现方约定</b>：需要平台 API 解析投递目标时，在 <see cref="BuildRequestAsync"/> 内解析并写入
    /// <see cref="ConversationRequest.SenderId"/>（或 <see cref="ConversationRequest.SubjectId"/>），本钩子只做纯判定。
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

    /// <summary>
    /// 覆写业务去重键：自带命名空间（<c>feishu.agent.conversation:{EventId}</c>），禁止裸 EventId。
    /// </summary>
    /// <param name="eventData">事件原始数据。</param>
    /// <returns>业务去重键。</returns>
    protected override string? GetBusinessKey(EventData eventData)
        => string.IsNullOrEmpty(eventData.EventId)
            ? null
            : $"feishu.agent.conversation:{eventData.EventId}";

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
        await _pipeline.RunTurnAsync(request, _replySink, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 请求准入校验（产品政策，经 <see cref="ConversationalTurnOptions.ValidateRequestPolicies"/> 注入管线）：
    /// 多租户分级（TMA2-20）+ 工具链与租户键的组合约束（R2-3）。appKey 分级发生在管线建立
    /// 会话作用域之前（与既有行为逐字节一致）。
    /// </summary>
    private void ValidateRequestPolicies(ConversationRequest request)
    {
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
    }

    /// <summary>
    /// 覆写流式回复目标（R5-3 两段式解析）。内建默认 = 通道解析器优先、回退目标会话 ID；
    /// 派生类可覆写以改变通道目标语义。
    /// </summary>
    protected virtual string? ResolveStreamTarget(ConversationRequest request)
        => _pipeline.ResolveDefaultStreamTarget(request);

    /// <summary>
    /// 解析流式回复的目标 chat_id：<see cref="ConversationRequest.ChatId"/> 优先（im 事件恒可用，
    /// 单聊流式因此解锁，P2D-2b）；缺省群聊回退会话主体。派生类可覆写（同时对「流式目标」与
    /// 「工具上下文目标」生效——管线经策略委托回读本虚方法）。
    /// </summary>
    protected virtual string? ResolveStreamTargetChatId(ConversationRequest request)
        => _pipeline.ResolveDefaultStreamTargetChatId(request);

    /// <summary>
    /// 拼接用户消息：<b>触发文本在前</b>（<see cref="ConversationRequest.MentionedText"/>），
    /// 装配器片段在后；派生类可覆写以改变次序或追加固定前缀。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 触发文本（每个派生类的 <c>MentionedText</c>）<b>必须进入模型</b>——内建装配把它放在最前。
    /// 派生类（如 IM 处理器）<b>不得</b>为同一语义再写一遍覆写（「每个派生类都得记得覆写同样三行」是机制缺失；
    /// 基类默认 + 管线内建装配已保证正确）。
    /// </para>
    /// <para>
    /// 空消息守卫由管线承载：它拦的是"装配器与触发文本都空"的真实异常形态
    /// （会被 OpenAI 兼容端点 400 拒绝且已计费）。
    /// </para>
    /// </remarks>
    /// <param name="request">会话请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户消息文本。</returns>
    protected virtual Task<string> AssembleUserMessageAsync(ConversationRequest request, CancellationToken cancellationToken)
        => _pipeline.AssembleDefaultUserMessageAsync(request, cancellationToken);

    /// <summary>
    /// P4-1：构造「等待人工确认」的用户可见答复（经策略委托注入管线）。
    /// </summary>
    /// <remarks>
    /// 这是会被下发给用户/发回模型的文本，故必须<b>不含任何批准要素</b>
    /// （不含 requestId、不含入参原文、不含任何形式的令牌）——否则「批准只能来自宿主」的前提被破坏。
    /// </remarks>
    protected virtual string BuildApprovalPendingReply(IReadOnlyList<FrameworkToolApprovalRequest> pending)
        => $"以下操作需要人工确认后才能执行：{string.Join("、", pending.Select(p => p.ToolName).Distinct(StringComparer.Ordinal))}。"
           + "已通知审批人，确认后会自动继续。";

    /// <summary>
    /// 租户作用域建立的持有者（把 <see cref="IFeishuAppContextScopeFactory"/> 的 fail-closed 语义
    /// 收在一处，供 <see cref="BeginAppScope"/> 与派生类共用）。
    /// </summary>
    private sealed class FeishuAppContextScopeHolder(IFeishuAppContextScopeFactory? factory)
    {
        public IDisposable? BeginAppScope(string? appKey)
        {
            if (string.IsNullOrWhiteSpace(appKey))
            {
                return null;
            }

            if (factory is null)
            {
                throw new InvalidOperationException(
                    $"事件包含 appKey('{appKey}') 但未注入 {nameof(IFeishuAppContextScopeFactory)}，拒绝以默认应用身份回复。");
            }

            return factory.BeginScope(appKey!);
        }
    }

    /// <summary>
    /// 回复下发端口（<see cref="IConversationReplySink"/>）的产品实现：桥接管线与本类的
    /// <see cref="ReplyAsync"/> 钩子（首次回复与 outbox 补发共用同一实现）。
    /// </summary>
    private sealed class ConversationReplySink(ConversationalFeishuEventHandler<T> owner) : IConversationReplySink
    {
        public Task DeliverAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken = default)
            => owner.ReplyAsync(request, responseText, cancellationToken);
    }
}
