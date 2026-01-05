// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Approval;


/// <summary>
/// 请假审批事件体
/// <para>审批定义的表单包含 请假控件组 时，该定义下的审批实例在 通过 或者 通过并撤销 时，会触发该事件。</para>
/// <para>事件类型:leave_approval</para>
/// <para>使用时请继承：<see cref="LeaveApprovalEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/approval-v4/event/special-event/leave</para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.LeaveApproval, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class LeaveApprovalResult : IEventResult
{
    /// <summary>
    /// 应用的 App ID。
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// 审批定义 Code。
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string? ApprovalCode { get; set; }

    /// <summary>
    /// 审批实例 Code。
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string? InstanceCode { get; set; }

    /// <summary>
    /// 审批提交人的 user_id。
    /// </summary>
    [JsonPropertyName("employee_id")]
    public string? EmployeeId { get; set; }

    /// <summary>
    /// 审批提交人的 open_id。
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// 审批开始时间，秒级时间戳。
    /// </summary>
    [JsonPropertyName("start_time")]
    public long StartTime { get; set; }

    /// <summary>
    /// 审批结束时间，秒级时间戳。
    /// </summary>
    [JsonPropertyName("end_time")]
    public long EndTime { get; set; }

    /// <summary>
    /// 请假类型。
    /// </summary>
    [JsonPropertyName("leave_type")]
    public string? LeaveType { get; set; }

    /// <summary>
    /// 请假开始时间。示例格式：2025-01-14 00:00:00
    /// </summary>
    [JsonPropertyName("leave_start_time")]
    public string? LeaveStartTime { get; set; }

    /// <summary>
    /// 请假结束时间。示例格式：2025-01-14 00:00:00
    /// </summary>
    [JsonPropertyName("leave_end_time")]
    public string? LeaveEndTime { get; set; }

    /// <summary>
    /// 请假时长。单位：秒
    /// </summary>
    [JsonPropertyName("leave_interval")]
    public int LeaveInterval { get; set; }

    /// <summary>
    /// 请假单位。
    /// </summary>
    [JsonPropertyName("leave_unit")]
    public int LeaveUnit { get; set; }

    /// <summary>
    /// 请假事由。
    /// </summary>
    [JsonPropertyName("leave_reason")]
    public string? LeaveReason { get; set; }

    /// <summary>
    /// 租户 Key，是企业的唯一标识。
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }

    /// <summary>
    /// 事件类型。固定值 leave_approval
    /// </summary>
    [JsonPropertyName("type")]
    public string? EventType { get; set; }

}
