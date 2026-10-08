// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Events;
using Mud.Feishu.EventCallback.Approval;

namespace Mud.Feishu.AI.FeishuTools.Events;

/// <summary>
/// 审批任务状态变更会话事件处理器（WP7/R5 T7-1）：
/// <c>approval_task</c> 事件 → 会话 → 模型 → 回复（"有新的待办"→ Agent 主动提示/动作）。
/// </summary>
/// <remarks>
/// <para>
/// <b>事件规范化</b>：审批任务事件的会话主体 = 操作人（<c>open_id</c>），会话维度 = 单聊
/// （审批通知天然面向个人）；<c>instance_code</c> / <c>task_id</c> / <c>status</c> 进入用户消息文本，
/// 模型可据此调用 <c>approval.get_instance</c> 或 <c>approval.list_pending_tasks</c> 获取详情。
/// </para>
/// <para>
/// <b>幂等业务键</b>：自带命名空间 <c>feishu.agent.approval_task:{EventId}</c>（D 系契约：禁止裸 EventId）。
/// </para>
/// <para>
/// <b>软缺席</b>：未注册 <c>IFeishuTenantV1Message</c> 时回复通道不可用——事件处理器仍可装配，
/// 但回复路径 fail-fast。
/// </para>
/// <para>
/// <b>R2-01b（本轮审查新增发现）</b>：本类与任务事件处理器同批修正了三处缺陷——
/// ① 触发文本（<c>MentionedText</c>）此前进不了模型，且无装配器时用户消息恒空 ⇒ 模型从未被调用
/// （真实形态是端到端 no-op，比"白跑模型"更严重；已在基类 <c>AssembleUserMessageAsync</c> 统一修正）；
/// ② 回复的 content 用 <c>JsonObject</c> 构造，替代手写 <c>Replace</c> 转义链；
/// ③ 平台返回 <c>code != 0</c> 时不再静默成功——改为上抛触发幂等回滚 + 重投递。
/// </para>
/// <para>
/// <b>与 IM 会话处理器的差异</b>：审批事件没有 <c>chat_id</c>——回复目标必须由宿主覆写
/// <see cref="ConversationalFeishuEventHandler{T}.ResolveStreamTargetChatId"/> 指定（如操作人的单聊 chat_id），
/// 否则流式路径不启用（回退非流式回复经 <see cref="ReplyAsync"/>）。
/// </para>
/// </remarks>
public sealed class ApprovalTaskConversationalEventHandler(
    FeishuAgent agent,
    IFeishuEventDeduplicator businessDeduplicator,
    Mud.Feishu.IFeishuTenantV1Message? messageClient,
    ILogger? logger = null,
    IReadOnlyList<IContextAssembler>? contextAssemblers = null,
    IFeishuToolContextAccessor? toolContextAccessor = null,
    IMessageChannel? messageChannel = null,
    IConversationGate? conversationGate = null,
    IAppKeyAccessor? appKeyAccessor = null,
    IFeishuAppContextScopeFactory? appContextScopeFactory = null)
    : ConversationalFeishuEventHandler<ApprovalTaskResult>(
        agent, businessDeduplicator, logger, contextAssemblers, toolContextAccessor, messageChannel, conversationGate, appKeyAccessor,
        appContextScopeFactory: appContextScopeFactory)
{
    /// <summary>
    /// 解析不出操作人时的会话主体占位值（<c>open_id</c> 与 <c>user_id</c> 都为空，
    /// 如自动通过类型的审批任务）——它只用于会话分桶，<b>不是</b>可投递接收方。
    /// </summary>
    private const string UnknownSubject = "unknown_user";

    private readonly Mud.Feishu.IFeishuTenantV1Message? _messageClient = messageClient;

    /// <inheritdoc />
    protected override Task<ConversationRequest> BuildRequestAsync(ApprovalTaskResult eventData, CancellationToken cancellationToken)
    {
        // 审批事件面向操作人（open_id），单聊维度。
        var subjectId = !string.IsNullOrEmpty(eventData.OpenId)
            ? eventData.OpenId!
            : eventData.UserId ?? UnknownSubject;

        var request = new ConversationRequest(
            AppKey: CurrentAppKey ?? string.Empty,
            Scope: ConversationScope.P2P(),
            SubjectId: subjectId,
            SenderId: eventData.OpenId ?? eventData.UserId ?? string.Empty,
            MessageId: string.Empty,
            MentionedText: BuildApprovalNotification(eventData));

        return Task.FromResult(request);
    }

    /// <inheritdoc />
    protected override async Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken)
    {
        if (_messageClient is null)
        {
            // R2-01b：与任务事件处理器对齐——不得静默 return（"看起来已消费但用户没收到"）。
            // 正常路径已由 TryFindDeliveryBlockAsync 在调用模型前拦截；此处是补发路径的兜底。
            throw new InvalidOperationException(
                $"审批事件回复失败：IFeishuTenantV1Message 未注册（appKey: {request.AppKey}, subject: {request.SubjectId}）");
        }

        if (string.IsNullOrEmpty(request.SubjectId))
        {
            throw new InvalidOperationException("审批事件回复失败：会话主体为空，无可投递接收方");
        }

        // R3-1：回复前必须切到事件的租户上下文，否则生成的客户端会退回默认应用身份（TMA2-20 跨租户错发）。
        using var appScope = BeginAppScope(request.AppKey);

        // 审批事件无 chat_id：以审批人 open_id 发单聊（receive_id_type = open_id）。
        // content 用 JsonObject 构造（AOT 安全 + 正确转义），取代此前手写的 Replace 转义链
        //（手写转义漏掉 \b/\f/\n/\r/\t 与单个 \u 序列，且与 SDK 其它出口两套写法——R2-16 的收敛点之一）。
        var outcome = FeishuApiResultReader.Read(await _messageClient
            .SendMessageAsync(
                new Mud.Feishu.DataModels.Messages.SendMessageRequest
                {
                    ReceiveId = request.SubjectId,
                    MsgType = "text",
                    Content = new JsonObject { ["text"] = responseText }.ToJsonString(),
                },
                "open_id",
                cancellationToken)
            .ConfigureAwait(false));

        // R2-01b：此前**不检查** outcome —— 平台拒绝（限流/权限/目标不存在）时静默成功，
        // 幂等标记落 Completed，用户永远收不到答复且无任何信号。现改为上抛 → 基类幂等回滚 + 重投递
        //（重投递经 outbox 补发同一文本，不重跑模型）。
        if (!outcome.Ok)
        {
            throw new InvalidOperationException(
                $"审批事件回复失败（receiver: {request.SubjectId}）: {outcome.ErrorText ?? "返回空数据"}");
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// R2-01：审批事件的投递目标<b>来自事件本身</b>（<c>open_id</c>），因此"能不能送出去"在调用模型前
    /// 即可判定——不可投递时短路可省下一次完整模型调用（与任务事件处理器同一处置）。
    /// <b>自动通过类型的审批任务 <c>open_id</c> 为空</b>（官方文档明示），此时同样无收件人。
    /// </remarks>
    protected override Task<string?> TryFindDeliveryBlockAsync(ConversationRequest request, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (_messageClient is null)
        {
            return Task.FromResult<string?>(
                "未注册 IFeishuTenantV1Message，审批事件回复通道不可用——已跳过本轮模型调用（注册消息客户端后重试）");
        }

        var subject = request.SubjectId;
        return Task.FromResult<string?>(string.IsNullOrEmpty(subject) || string.Equals(subject, UnknownSubject, StringComparison.Ordinal)
            ? "审批事件无可投递操作人（open_id/user_id 均为空，如自动通过类型的审批任务）——已跳过本轮模型调用"
            : null);
    }

    /// <summary>
    /// 构造审批任务通知文本（注入为 MentionedText，模型可据此决定是否调用工具获取详情）。
    /// </summary>
    private static string BuildApprovalNotification(ApprovalTaskResult eventData)
    {
        var status = eventData.Status switch
        {
            "PENDING" => "进行中",
            "APPROVED" => "已通过",
            "REJECTED" => "已拒绝",
            "TRANSFERRED" => "已转交",
            "ROLLBACK" => "已退回",
            "REVERTED" => "已还原",
            "DONE" => "已完成",
            _ => eventData.Status ?? "未知状态",
        };

        return $"审批任务状态变更通知：审批实例 {eventData.InstanceCode} 的任务 {eventData.TaskId} 状态变更为「{status}」"
            + (string.IsNullOrEmpty(eventData.ApprovalCode) ? "" : $"（审批定义：{eventData.ApprovalCode}）")
            + "。你可以使用 approval.get_instance 获取详情，或使用 approval.list_pending_tasks 查看待办。";
    }

    /// <inheritdoc />
    protected override string? GetBusinessKey(EventData eventData)
        => string.IsNullOrEmpty(eventData.EventId)
            ? null
            : $"feishu.agent.approval_task:{eventData.EventId}";
}
