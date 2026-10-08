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
using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.Messages;
using Mud.Feishu.DataModels.Tasks;
using Mud.Feishu.EventCallback.Task;

namespace Mud.Feishu.AI.Tools.Events;

/// <summary>
/// 任务更新会话事件处理器（WP7/R5 T7-2；回复链路 R2-01）：<c>task.task.updated_v1</c> 事件 →
/// 会话 → 模型 → 回复（单聊投递给任务负责人）。
/// </summary>
/// <remarks>
/// <para>
/// <b>事件规范化</b>：任务更新事件<b>不携带任何用户身份字段</b>（只有 <c>task_id</c> / <c>obj_type</c>），
/// 故投递目标必须由任务详情解析：<c>IFeishuTenantV2Task.GetTaskByIdAsync</c>
/// （<c>GetTaskByIdAsync</c> 声明于抽象基接口 <c>IFeishuV2Task</c>，见 <c>Mud.Feishu.Interfaces</c>）
/// → <c>TaskOperationResult.Task.Members</c> 中 <c>role=assignee</c> 的 <c>open_id</c>
/// （无负责人时回退创建者）。解析结果写入
/// <see cref="ConversationRequest.SenderId"/>/<see cref="ConversationRequest.SubjectId"/>，
/// 单聊维度（会话键按负责人分桶；同时使 user 身份工具拿到正确的当前用户）。
/// </para>
/// <para>
/// <b>不可投递时在调用模型之前短路（R2-01）</b>：未注册
/// <c>IFeishuTenantV1Message</c>、未注册 <c>IFeishuTenantV2Task</c>、任务已删除或解析不出负责人
/// 时，<see cref="TryFindDeliveryBlockAsync"/> 返回原因，基类按「已消费」短路——<b>零模型计费、零历史落库</b>。
/// 此前这些形态会先花一次完整模型调用、再把回答写进日志（用户永远收不到）。
/// </para>
/// <para>
/// <b>投递语义</b>：<see cref="ReplyAsync"/> 以 <c>receive_id_type=open_id</c> 发单聊文本
/// （与同包 <c>ApprovalTaskConversationalEventHandler</c> 同款范式：无 <c>chat_id</c> 的事件
/// 把解析出的用户 ID 当作接收方）。发送失败向上抛 ⇒ 基类幂等回滚 + 重投递，
/// 且重投递由 outbox 补发同一文本（不重跑模型）。多负责人场景只投递给<b>首位</b>负责人
/// （其余负责人需各自的会话轮次，属后续能力，不在本任务内）。
/// </para>
/// <para>
/// <b>幂等业务键</b>：自带命名空间 <c>feishu.agent.task_updated:{EventId}</c>（D 系契约：禁止裸 EventId）。
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
    Mud.Feishu.IFeishuTenantV2Task? taskClient = null,
    IFeishuAppContextScopeFactory? scopeFactory = null,
    ILogger? logger = null,
    IReadOnlyList<IContextAssembler>? contextAssemblers = null,
    IFeishuToolContextAccessor? toolContextAccessor = null,
    IMessageChannel? messageChannel = null,
    IConversationGate? conversationGate = null,
    IAppKeyAccessor? appKeyAccessor = null)
    : ConversationalFeishuEventHandler<TaskUpdatedResult>(
        agent, businessDeduplicator, logger, contextAssemblers, toolContextAccessor, messageChannel, conversationGate, appKeyAccessor,
        appContextScopeFactory: scopeFactory)
{
    /// <summary>负责人角色字面量（任务 v2 成员角色的闭集取值之一）。</summary>
    private const string AssigneeRole = "assignee";

    /// <summary>
    /// 未解析出投递目标时的 <see cref="ConversationRequest.SenderId"/> 占位值
    /// （与 <c>ConversationRequest.SenderId</c> 的缺省语义一致，见 BuildRequestAsync）。
    /// </summary>
    private const string SystemSenderId = "system";

    private readonly Mud.Feishu.IFeishuTenantV1Message? _messageClient = messageClient;
    private readonly Mud.Feishu.IFeishuTenantV2Task? _taskClient = taskClient;

    /// <inheritdoc />
    protected override async Task<ConversationRequest> BuildRequestAsync(TaskUpdatedResult eventData, CancellationToken cancellationToken)
    {
        // 投递目标解析（R2-01）：任务事件不带 open_id，须经任务详情取成员。
        // 解析失败时退回应用维度的默认主体（SubjectId 仍可用作会话键），
        // 「不可投递」由 TryFindDeliveryBlockAsync 判定并在调用模型前短路。
        var receiverOpenId = await ResolveReceiverAsync(eventData.TaskId, cancellationToken).ConfigureAwait(false);

        var subjectId = !string.IsNullOrEmpty(receiverOpenId)
            ? receiverOpenId!
            : (!string.IsNullOrEmpty(eventData.TaskId) ? $"task:{eventData.TaskId}" : "unknown_task");

        var request = new ConversationRequest(
            AppKey: CurrentAppKey ?? string.Empty,
            Scope: ConversationScope.P2P(),
            SubjectId: subjectId,
            SenderId: receiverOpenId ?? SystemSenderId,
            MessageId: string.Empty,
            MentionedText: BuildTaskNotification(eventData));

        return request;
    }

    /// <inheritdoc />
    protected override Task<string?> TryFindDeliveryBlockAsync(ConversationRequest request, CancellationToken cancellationToken)
    {
        // R2-01：三条"送不出去"的形态全部前移到模型调用之前（此前是花完模型再写日志）。
        _ = cancellationToken;

        if (_messageClient is null)
        {
            return Task.FromResult<string?>(
                "未注册 IFeishuTenantV1Message，任务事件回复通道不可用——已跳过本轮模型调用（注册消息客户端后重试）");
        }

        if (_taskClient is null)
        {
            return Task.FromResult<string?>(
                "未注册 IFeishuTenantV2Task，无法解析任务负责人（任务事件无 open_id 字段）——已跳过本轮模型调用");
        }

        return Task.FromResult<string?>(string.IsNullOrEmpty(request.SenderId) || request.SenderId == SystemSenderId
            ? $"任务无可投递负责人（task_id 解析不出 assignee/creator，会话主体: {request.SubjectId}）——已跳过本轮模型调用"
            : null);
    }

    /// <inheritdoc />
    protected override async Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken)
    {
        if (_messageClient is null)
        {
            // 正常路径已被 TryFindDeliveryBlockAsync 拦截；此处是"补发路径"（outbox replay）的兜底：
            // 仍旧不得静默丢弃——抛出让基类幂等回滚，避免"看起来已消费但用户没收到"。
            throw new InvalidOperationException(
                $"任务事件回复失败：IFeishuTenantV1Message 未注册（appKey: {request.AppKey}, subject: {request.SubjectId}）");
        }

        // 任务事件无 chat_id：经解析出的负责人以 open_id 发单聊（对齐 ApprovalTaskConversationalEventHandler 范式）。
        // content 用 JsonObject 构造（AOT 安全 + 正确转义），不手写 JSON 字符串。
        var receiveId = request.SenderId;
        // R3-13：使用 SystemSenderId 常量替代硬编码 "system" 字面量。
        if (string.IsNullOrEmpty(receiveId) || receiveId == SystemSenderId)
        {
            throw new InvalidOperationException(
                $"任务事件回复失败：无可投递接收方（subject: {request.SubjectId}）");
        }

        using var appScope = BeginAppScope(request.AppKey);
        var outcome = FeishuApiResultReader.Read(await _messageClient
            .SendMessageAsync(
                new SendMessageRequest
                {
                    ReceiveId = receiveId,
                    MsgType = "text",
                    Content = new JsonObject { ["text"] = responseText }.ToJsonString(),
                },
                "open_id",
                cancellationToken)
            .ConfigureAwait(false));
        if (!outcome.Ok)
        {
            // 失败向上抛：基类幂等回滚 + 重投递，重投递经 outbox 补发同一文本（不重跑模型）。
            throw new InvalidOperationException(
                $"任务事件回复失败（receiver: {receiveId}）: {outcome.ErrorText ?? "返回空数据"}");
        }
    }

    /// <summary>
    /// 解析任务负责人 open_id（单聊投递目标）。
    /// </summary>
    /// <returns>负责人（或创建者）open_id；无法解析时 <see langword="null"/>。</returns>
    /// <remarks>
    /// <para>
    /// 解析顺序：<c>members[role=assignee]</c> 的首个有效 id → <c>creator.id</c>。
    /// 两者皆无（或任务不存在/已删除）返回 <see langword="null"/>，由
    /// <see cref="TryFindDeliveryBlockAsync"/> 在模型调用前短路。
    /// </para>
    /// <para>
    /// <b>解析失败不抛异常</b>：网络/权限问题与"任务确实没有负责人"在投递语义上等价
    /// （本轮都送不出去），统一走"不可投递"路径；异常细节进日志（Warning）供排障。
    /// 但<b>取消</b>必须放行（否则取消事件处理变成"已消费"）。
    /// </para>
    /// </remarks>
    private async Task<string?> ResolveReceiverAsync(string? taskId, CancellationToken cancellationToken)
    {
        if (_taskClient is null || string.IsNullOrEmpty(taskId))
        {
            return null;
        }

        try
        {
            using var appScope = BeginAppScope(CurrentAppKey);
            var outcome = FeishuApiResultReader.Read(await _taskClient
                .GetTaskByIdAsync(taskId!, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                _logger.LogWarning(
                    "任务详情读取失败，本轮无法解析投递目标（task_id: {TaskId}）: {Error}",
                    taskId, outcome.ErrorText);
                return null;
            }

            var task = outcome.Data!.Task;
            if (task is null)
            {
                return null;
            }

            foreach (var member in task.Members ?? [])
            {
                if (member is not null
                    && string.Equals(member.Role, AssigneeRole, StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(member.Id))
                {
                    return member.Id;
                }
            }

            return string.IsNullOrEmpty(task.Creator?.Id) ? null : task.Creator!.Id;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "任务详情读取异常，本轮无法解析投递目标（task_id: {TaskId}）", taskId);
            return null;
        }
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
