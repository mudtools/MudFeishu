// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback;

/// <summary>
/// 飞书WebSocket事件类型常量
/// </summary>
public static class FeishuEventTypes
{
    #region Organization Events
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
    #endregion

    #region IM Events
    /// <summary>
    /// 接收消息事件
    /// </summary>
    public const string ReceiveMessage = "im.message.receive_v1";

    /// <summary>
    /// 消息撤回事件
    /// </summary>
    public const string MessageRecalled = "im.message.recalled_v1";

    /// <summary>
    /// 消息已读事件
    /// </summary>
    public const string MessageRead = "im.message.message_read_v1";

    /// <summary>
    /// 新增消息表情回复事件
    /// </summary>
    public const string MessageReactionCreated = "im.message.reaction.created_v1";

    /// <summary>
    /// 删除消息表情回复事件
    /// </summary>
    public const string MessageReactionDeleted = "im.message.reaction.deleted_v1";

    /// <summary>
    /// 群解散事件
    /// </summary>
    public const string ChatDisbanded = "im.chat.disbanded_v1";

    /// <summary>
    /// 群配置修改事件
    /// </summary>
    public const string ChatUpdated = "im.chat.updated_v1";

    /// <summary>
    /// 用户进群事件
    /// </summary>
    public const string ChatMemberUserAdd = "im.chat.member.user.added_v1";

    /// <summary>
    /// 用户出群事件
    /// </summary>
    public const string ChatMemberUserDelete = "im.chat.member.user.deleted_v1";

    /// <summary>
    /// 撤销拉用户进群事件
    /// </summary>
    public const string ChatMemberUserWithdrawn = "im.chat.member.user.withdrawn_v1";

    /// <summary>
    /// 机器人进群事件
    /// </summary>
    public const string ChatMemberBotAdded = "im.chat.member.bot.added_v1";

    /// <summary>
    /// 机器人被移出群事件
    /// </summary>
    public const string ChatMemberBotDeleted = "im.chat.member.bot.deleted_v1";
    #endregion

    #region Task Events

    /// <summary>
    /// 任务信息变更（租户维度）事件
    /// </summary>
    public const string TaskUpdateTenant = "task.task.update_tenant_v1";

    /// <summary>
    /// 任务信息变更事件
    /// </summary>
    public const string TaskUpdated = "task.task.updated_v1";

    /// <summary>
    /// 任务评论信息变更事件
    /// </summary>
    public const string TaskCommentUpdated = "task.task.comment.updated_v1";
    #endregion

    #region Approval Events
    /// <summary>
    /// 审批定义更新事件
    /// </summary>
    public const string ApprovalApprovalUpdated = "approval.approval.updated_v4";

    /// <summary>
    /// 审批抄送状态变更事件
    /// </summary>
    public const string ApprovalCc = "approval_cc";

    /// <summary>
    /// 审批任务状态变更事件
    /// </summary>
    public const string ApprovalTask = "approval_task";

    /// <summary>
    /// 审批实例状态变更事件
    /// </summary>
    public const string ApprovalInstance = "approval_instance";

    /// <summary>
    /// 外出审批事件
    /// </summary>
    public const string OutApproval = "out_approval";

    /// <summary>
    /// 出差审批事件
    /// </summary>
    public const string ApprovalInstanceTripGroupUpdate = "approval.instance.trip_group_update_v4";

    /// <summary>
    /// 补卡审批事件
    /// </summary>
    public const string ApprovalInstanceRemedyGroupUpdate = "approval.instance.remedy_group_update_v4";

    /// <summary>
    /// 换班审批事件
    /// </summary>
    public const string ShiftApproval = "shift_approval";

    /// <summary>
    /// 加班审批事件
    /// </summary>
    public const string WorkApproval = "work_approval";

    /// <summary>
    /// 加班审批通过并撤销事件
    /// </summary>
    public const string WorkApprovalRevert = "work_approval_revert";

    /// <summary>
    /// 请假审批事件
    /// </summary>
    public const string LeaveApproval = "leave_approval";

    /// <summary>
    /// 请假审批事件
    /// </summary>
    public const string LeaveApprovalV2 = "leave_approvalV2";

    /// <summary>
    /// 请假审批通过并撤销事件
    /// </summary>
    public const string LeaveApprovalRevert = "leave_approval_revert";
    #endregion

    #region Attendance Events
    /// <summary>
    /// 考勤用户打卡流水事件
    /// </summary>
    public const string AttendanceUserFlowCreated = "attendance.user_flow.created_v1";

    /// <summary>
    /// 考勤用户任务更新事件
    /// </summary>
    public const string AttendanceUserTaskUpdate = "attendance.user_task.updated_v1";
    #endregion

    #region Drive Events
    /// <summary>
    /// 文件夹下文件创建
    /// </summary>
    public const string DriveFileCreated = "drive.file.created_in_folder_v1";

    /// <summary>
    /// 文件标题更新事件
    /// </summary>
    public const string DriveFileTitleUpdated = "drive.file.title_updated_v1";

    /// <summary>
    /// 文件已读事件
    /// </summary>
    public const string DriveFileRead = "drive.file.read_v1";

    /// <summary>
    /// 文件编辑事件
    /// </summary>
    public const string DriveFileEdit = "drive.file.edit_v1";

    /// <summary>
    /// 文件协作者权限申请
    /// </summary>
    public const string DriveFilePermissionMemberApplied = "drive.file.permission_member_applied_v1";

    /// <summary>
    /// 文件协作者添加
    /// </summary>
    public const string DriveFilePermissionMemberAdded = "drive.file.permission_member_added_v1";

    /// <summary>
    /// 文档协作者移除
    /// </summary>
    public const string DriveFilePermissionMemberRemoved = "drive.file.permission_member_removed_v1";

    /// <summary>
    /// 文件删除到回收站事件
    /// </summary>
    public const string DriveFileTrashed = "drive.file.trashed_v1";

    /// <summary>
    /// 文件删除事件
    /// </summary>
    public const string DriveFileDeleted = "drive.file.deleted_v1";

    /// <summary>
    /// 文件评论新增事件
    /// </summary>
    public const string DriveNoticeCommentAdd = "drive.notice.comment_add_v1";

    /// <summary>
    /// 多维表格字段变更事件
    /// </summary>
    public const string BitableFieldChanged = "drive.file.bitable_field_changed_v1";

    /// <summary>
    /// 多维表格记录变更事件
    /// </summary>
    public const string BitableRecordChanged = "drive.file.bitable_record_changed_v1";
    #endregion

    #region Calendar Events
    /// <summary>
    /// 日历变更事件
    /// </summary>
    public const string CalendarChanged = "calendar.calendar.changed_v4";

    /// <summary>
    /// 创建 ACL 事件
    /// </summary>
    public const string CalendarAclCreated = "calendar.calendar.acl.created_v4";

    /// <summary>
    /// 删除 ACL
    /// </summary>
    public const string CalendarAclDeleted = "calendar.calendar.acl.deleted_v4";

    /// <summary>
    /// 日程变更
    /// </summary>
    public const string CalendarEventChanged = "calendar.calendar.event.changed_v4";

    /// <summary>
    /// 会议室状态信息变更
    /// </summary>
    public const string RoomStatusChanged = "meeting_room.meeting_room.status_changed_v1";

    /// <summary>
    /// 第三方会议室日程变动
    /// </summary>
    public const string ThirdPartyMeetingRoomEventCreated = "third_party_meeting_room_event_created";
    #endregion

    #region VideoConferencing Events
    /// <summary>
    /// 会议纪要生成
    /// </summary>
    public const string VcNoteGenerated = "vc.note.generated_v1";

    /// <summary>
    /// 参与的会议结束
    /// </summary>
    public const string MeetingParticipantMeetingEnded = "vc.meeting.participant_meeting_ended_v1";

    /// <summary>
    /// 企业会议开始
    /// </summary>
    public const string MeetingAllMeetingStarted = "vc.meeting.all_meeting_started_v1";

    /// <summary>
    /// 企业会议结束
    /// </summary>
    public const string MeetingAllMeetingEnded = "vc.meeting.all_meeting_ended_v1";

    /// <summary>
    /// 会议开始
    /// </summary>
    public const string MeetingMeetingStarted = "vc.meeting.meeting_started_v1";

    /// <summary>
    /// 会议结束
    /// </summary>
    public const string MeetingMeetingEnded = "vc.meeting.meeting_ended_v1";

    /// <summary>
    /// 加入会议
    /// </summary>
    public const string MeetingJoinMeeting = "vc.meeting.join_meeting_v1";

    /// <summary>
    /// 离开会议
    /// </summary>
    public const string MeetingLeaveMeeting = "vc.meeting.leave_meeting_v1";

    #endregion

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