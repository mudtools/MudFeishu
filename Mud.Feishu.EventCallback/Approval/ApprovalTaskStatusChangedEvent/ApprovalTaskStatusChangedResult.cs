// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Approval;

/// <summary>
/// 审批任务状态变更事件
/// <para>审批任务状态发生变更时会触发该事件。状态变更包括： 用户创建审批实例后，推送第一个审批节点的审批任务 PENDING 状态。 如果当前审批节点是会签（AND）节点： 任一审批任务被同意，推送该任务的 APPROVED（已通过）状态，并推送当前节点剩余任务的 PENDING 状态。</para>
/// <para>事件类型:approval.task.status_changed_v4</para>
/// <para>使用时请继承：<see cref="ApprovalTaskStatusChangedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/approval-v4/task/events/status_changed</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.ApprovalTaskStatusChanged, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Approval")]
public class ApprovalTaskStatusChangedResult : IEventResult
{
    /// <summary>
    /// <para>审批定义code</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `64` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string? ApprovalCode { get; set; }

    /// <summary>
    /// <para>审批实例code</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `64` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string? InstanceCode { get; set; }

    /// <summary>
    /// <para>审批任务id</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `64` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// <para>三方审批任务id（仅为三方审批时有值）</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `128` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("task_external_id")]
    public string? TaskExternalId { get; set; }

    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("assigned_user")]
    public UserIdInfo? AssignedUser { get; set; }

    /// <summary>
    /// <para>任务状态</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `64` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>事件触发时间</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `64` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operate_time")]
    public string? OperateTime { get; set; }
}
