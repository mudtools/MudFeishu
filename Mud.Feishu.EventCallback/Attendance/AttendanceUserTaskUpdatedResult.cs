// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.DataModels.Attendance;

/// <summary>
/// 当用户任务变更后，推送该用户的任务状态变更消息。
/// <para>事件类型:attendance.user_task.updated_v1</para>
/// <para>使用时请继承：<see cref="AttendanceUserTaskUpdatedEventHandler"/></para>
/// <para>文档地址：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/event/user-task-status-change-event"/> </para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.AttendanceUserTaskUpdate, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class AttendanceUserTaskUpdatedResult : IEventResult
{
    /// <summary>
    /// 日期，格式为yyyyMMdd
    /// </summary>
    [JsonPropertyName("date")]
    public int Date { get; set; }

    /// <summary>
    /// 飞书管理后台 > 组织架构 > 成员与部门 > 成员详情中的用户 ID
    /// </summary>
    [JsonPropertyName("employee_id")]
    public string? EmployeeId { get; set; }

    /// <summary>
    /// 飞书管理后台 > 组织架构 > 成员与部门 > 成员详情中的工号
    /// </summary>
    [JsonPropertyName("employee_no")]
    public string? EmployeeNo { get; set; }

    /// <summary>
    /// 考勤组 ID，可用于按 ID 查询考勤组
    /// </summary>
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }

    /// <summary>
    /// 班次 ID，可用于按 ID 查询班次
    /// </summary>
    [JsonPropertyName("shift_id")]
    public string? ShiftId { get; set; }

    /// <summary>
    /// 状态变更数组
    /// </summary>
    [JsonPropertyName("status_changes")]
    public StatusItem[] StatusChanges { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("time_zone")]
    public string? TimeZone { get; set; }
}


/// <summary>
/// 状态
/// </summary>
public class StatusItem
{
    /// <summary>
    /// 变更前打卡结果，值为：【NoNeedCheck（无需打卡），SystemCheck（系统打卡），Normal（正常），Early（早退），Late（迟到），Lack（缺卡）】
    /// </summary>
    [JsonPropertyName("before_status")]
    public string? BeforeStatus { get; set; }

    /// <summary>
    /// 变更前结果补充，值为：【None（无），ManagerModification（管理员修改），CardReplacement（补卡通过），ShiftChange（换班），Travel（出差），Leave（请假），GoOut（外出），CardReplacementApplication（补卡申请中），FieldPunch（外勤打卡）】
    /// </summary>
    [JsonPropertyName("before_supplement")]
    public string? BeforeSupplement { get; set; }

    /// <summary>
    /// 变更后打卡结果，值为：【NoNeedCheck（无需打卡），SystemCheck（系统打卡），Normal（正常），Early（早退），Late（迟到），Lack（缺卡）】
    /// </summary>
    [JsonPropertyName("current_status")]
    public string? CurrentStatus { get; set; }

    /// <summary>
    /// 变更后打卡结果补充，值为：【None（无），ManagerModification（管理员修改），CardReplacement（补卡通过），ShiftChange（换班），Travel（出差），Leave（请假），GoOut（外出），CardReplacementApplication（补卡申请中），FieldPunch（外勤打卡）】
    /// </summary>
    [JsonPropertyName("current_supplement")]
    public string? CurrentSupplement { get; set; }

    /// <summary>
    /// 任务中的第几次上下班
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// 上下班状态变更，值为：【on（上班），off（下班）】
    /// </summary>
    [JsonPropertyName("work_type")]
    public string? WorkType { get; set; }
}