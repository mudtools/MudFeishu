// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.EventHandlers;

/// <summary>
/// 飞书WebSocket事件类型常量
/// </summary>
public static class FeishuEventTypes
{
    /// <summary>
    /// 接收消息事件
    /// </summary>
    public const string ReceiveMessage = "im.message.receive_v1";

    /// <summary>
    /// 消息已读事件
    /// </summary>
    public const string MessageRead = "im.message.message_read_v1";

    /// <summary>
    /// 用户加入群聊事件
    /// </summary>
    public const string UserAddedToGroup = "im.chat.member.user_added_v1";

    /// <summary>
    /// 用户离开群聊事件
    /// </summary>
    public const string UserRemovedFromGroup = "im.chat.member.user_deleted_v1";

    /// <summary>
    /// 群聊信息更新事件
    /// </summary>
    public const string GroupUpdated = "im.chat.updated_v1";

    /// <summary>
    /// 员工入职事件
    /// </summary>
    public const string UserCreated = "contact.user.created_v3";

    /// <summary>
    /// 用户更新事件
    /// </summary>
    public const string UserUpdated = "contact.user.updated_v3";

    /// <summary>
    /// 用户删除事件
    /// </summary>
    public const string UserDeleted = "contact.user.deleted_v3";

    /// <summary>
    /// 成员字段变更事件
    /// </summary>
    public const string CustomAttrUpdated = "contact.custom_attr_event.updated_v3";

    /// <summary>
    /// 人员类型创建事件
    /// </summary>
    public const string EmployeeTypeEnumCreated = "contact.employee_type_enum.created_v3";

    /// <summary>
    /// 人员类型更新事件
    /// </summary>
    public const string EmployeeTypeEnumUpdated = "contact.employee_type_enum.updated_v3";

    /// <summary>
    /// 人员类型删除事件
    /// </summary>
    public const string EmployeeTypeEnumDelete = "contact.employee_type_enum.deleted_v3";

    /// <summary>
    /// 人员类型启用事件
    /// </summary>
    public const string EmployeeTypeEnumActived = "contact.employee_type_enum.actived_v3";

    /// <summary>
    /// 人员类型禁用事件
    /// </summary>
    public const string EmployeeTypeEnumDeActived = "contact.employee_type_enum.deactivated_v3";


    /// <summary>
    /// 部门创建事件
    /// </summary>
    public const string DepartmentCreated = "contact.department.created_v3";

    /// <summary>
    /// 部门更新事件
    /// </summary>
    public const string DepartmentUpdated = "contact.department.updated_v3";

    /// <summary>
    /// 部门删除事件
    /// </summary>
    public const string DepartmentDeleted = "contact.department.deleted_v3";

    /// <summary>
    /// 审批通过事件
    /// </summary>
    public const string ApprovalApproved = "approval.approval.approved_v1";

    /// <summary>
    /// 审批拒绝事件
    /// </summary>
    public const string ApprovalRejected = "approval.approval.rejected_v1";

    /// <summary>
    /// 日程事件
    /// </summary>
    public const string CalendarEvent = "calendar.event.updated_v4";

    /// <summary>
    /// 会议开始事件
    /// </summary>
    public const string MeetingStart = "meeting.meeting.started_v1";

    /// <summary>
    /// 会议结束事件
    /// </summary>
    public const string MeetingEnd = "meeting.meeting.ended_v1";
}