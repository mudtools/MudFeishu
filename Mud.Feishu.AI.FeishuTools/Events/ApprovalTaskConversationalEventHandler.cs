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
/// 但回复路径 fail-fast（会话上下文仍正常建立，模型可被调用）。
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
    IAppKeyAccessor? appKeyAccessor = null)
    : ConversationalFeishuEventHandler<ApprovalTaskResult>(
        agent, businessDeduplicator, logger, contextAssemblers, toolContextAccessor, messageChannel, conversationGate, appKeyAccessor)
{
    private readonly Mud.Feishu.IFeishuTenantV1Message? _messageClient = messageClient;

    /// <inheritdoc />
    protected override Task<ConversationRequest> BuildRequestAsync(ApprovalTaskResult eventData, CancellationToken cancellationToken)
    {
        // 审批事件面向操作人（open_id），单聊维度。
        var subjectId = !string.IsNullOrEmpty(eventData.OpenId)
            ? eventData.OpenId!
            : eventData.UserId ?? "unknown_user";

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
            _logger.LogWarning("审批事件回复失败：IFeishuTenantV1Message 未注册（appKey: {AppKey}, subject: {SubjectId}）",
                request.AppKey, request.SubjectId);
            return;
        }

        // 审批事件无 chat_id：回复需要宿主提供目标（如经 resolve_user 查到 open_id 后发消息）。
        // 此处用 receive_id_type=open_id 发送单聊消息。
        await _messageClient.SendMessageAsync(
            new Mud.Feishu.DataModels.Messages.SendMessageRequest
            {
                ReceiveId = request.SubjectId,
                MsgType = "text",
                Content = """{"text":""" + responseText.Replace("\\", "\\\\").Replace("\"", "\\\"") + """}""",
            },
            "open_id",
            cancellationToken).ConfigureAwait(false);
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
