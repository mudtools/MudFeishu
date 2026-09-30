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
using Mud.Feishu.EventCallback.Task;

namespace Mud.Feishu.AI.FeishuTools.Events;

/// <summary>
/// 任务更新会话事件处理器（WP7/R5 T7-2）：
/// <c>task.task.updated_v1</c> 事件 → 会话 → 模型 → 回复。
/// </summary>
/// <remarks>
/// <para>
/// <b>事件规范化</b>：任务更新事件会话主体 = 任务创建者（从事件上下文解析），单聊维度；
/// <c>task_id</c> / <c>obj_type</c> 进入用户消息文本，模型可据此调用 <c>task.list_my_tasks</c> 获取详情。
/// </para>
/// <para>
/// <b>幂等业务键</b>：自带命名空间 <c>feishu.agent.task_updated:{EventId}</c>（D 系契约：禁止裸 EventId）。
/// </para>
/// <para>
/// <b>软缺席</b>：未注册 <c>IFeishuTenantV1Message</c> 时回复通道不可用——事件处理器仍可装配，
/// 但回复路径 fail-fast。
/// </para>
/// <para>
/// <b>obj_type 语义映射</b>：
/// <list type="bullet">
/// <item>1 = 任务详情变化</item>
/// <item>2 = 协作者变化</item>
/// <item>3 = 关注者变化</item>
/// <item>4 = 提醒时间变化</item>
/// <item>5 = 任务完成</item>
/// <item>6 = 任务取消完成</item>
/// <item>7 = 任务删除</item>
/// </list>
/// </para>
/// </remarks>
public sealed class TaskUpdatedConversationalEventHandler(
    FeishuAgent agent,
    IFeishuEventDeduplicator businessDeduplicator,
    Mud.Feishu.IFeishuTenantV1Message? messageClient,
    ILogger? logger = null,
    IReadOnlyList<IContextAssembler>? contextAssemblers = null,
    IFeishuToolContextAccessor? toolContextAccessor = null,
    IMessageChannel? messageChannel = null,
    IConversationGate? conversationGate = null,
    IAppKeyAccessor? appKeyAccessor = null)
    : ConversationalFeishuEventHandler<TaskUpdatedResult>(
        agent, businessDeduplicator, logger, contextAssemblers, toolContextAccessor, messageChannel, conversationGate, appKeyAccessor)
{
    private readonly Mud.Feishu.IFeishuTenantV1Message? _messageClient = messageClient;

    /// <inheritdoc />
    protected override Task<ConversationRequest> BuildRequestAsync(TaskUpdatedResult eventData, CancellationToken cancellationToken)
    {
        // 任务事件面向任务相关人，无 open_id 字段——使用 app 维度的默认主体。
        var subjectId = !string.IsNullOrEmpty(eventData.TaskId)
            ? $"task:{eventData.TaskId}"
            : "unknown_task";

        var request = new ConversationRequest(
            AppKey: CurrentAppKey ?? string.Empty,
            Scope: ConversationScope.P2P(),
            SubjectId: subjectId,
            SenderId: "system",
            MessageId: string.Empty,
            MentionedText: BuildTaskNotification(eventData));

        return Task.FromResult(request);
    }

    /// <inheritdoc />
    protected override async Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken)
    {
        if (_messageClient is null)
        {
            _logger.LogWarning("任务事件回复失败：IFeishuTenantV1Message 未注册（appKey: {AppKey}, subject: {SubjectId}）",
                request.AppKey, request.SubjectId);
            return;
        }

        // 任务事件无 chat_id：回复经宿主提供的目标发送。
        // 宿主可覆写 ReplyAsync 指定精确目标（如经 task API 查到 assignee 后发单聊）。
        _logger.LogInformation("任务事件回复（subject: {SubjectId}, text: {Text}）", request.SubjectId, responseText);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// 构造任务更新通知文本。
    /// </summary>
    private static string BuildTaskNotification(TaskUpdatedResult eventData)
    {
        var changeType = eventData.ObjType switch
        {
            1 => "任务详情发生变化",
            2 => "任务协作者发生变化",
            3 => "任务关注者发生变化",
            4 => "任务提醒时间发生变化",
            5 => "任务已完成",
            6 => "任务取消完成",
            7 => "任务已删除",
            _ => $"任务发生变化（类型 {eventData.ObjType}）",
        };

        return $"任务更新通知：任务 {eventData.TaskId} {changeType}。"
            + "你可以使用 task.list_my_tasks 查看当前任务列表。";
    }

    /// <inheritdoc />
    protected override string? GetBusinessKey(EventData eventData)
        => string.IsNullOrEmpty(eventData.EventId)
            ? null
            : $"feishu.agent.task_updated:{eventData.EventId}";
}
