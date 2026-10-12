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

    /// <summary>
    /// 通讯录权限范围变更事件
    /// </summary>
    public const string ContactScopeUpdated = "contact.scope.updated_v3";
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

    /// <summary>
    /// 用户进入与机器人的会话事件
    /// </summary>
    public const string ChatBotP2pChatEntered = "im.chat.access_event.bot_p2p_chat_entered_v1";
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

    /// <summary>
    /// 审批任务状态变更事件（新版 v4）
    /// </summary>
    public const string ApprovalTaskStatusChanged = "approval.task.status_changed_v4";
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

    /// <summary>
    /// 开始录制
    /// </summary>
    public const string MeetingRecordingStarted = "vc.meeting.recording_started_v1";

    /// <summary>
    /// 停止录制
    /// </summary>
    public const string MeetingRecordingEnded = "vc.meeting.recording_ended_v1";

    /// <summary>
    /// 录制完成
    /// </summary>
    public const string MeetingRecordingReady = "vc.meeting.recording_ready_v1";

    /// <summary>
    /// 会议共享开始
    /// </summary>
    public const string MeetingShareStarted = "vc.meeting.share_started_v1";

    /// <summary>
    /// 会议共享结束
    /// </summary>
    public const string MeetingShareEnded = "vc.meeting.share_ended_v1";

    /// <summary>
    /// 创建会议室层级
    /// </summary>
    public const string RoomLevelCreatedEvent = "vc.room_level.created_v1";

    /// <summary>
    /// 更新会议室层级
    /// </summary>
    public const string RoomLevelUpdatedEvent = "vc.room_level.updated_v1";

    /// <summary>
    /// 删除会议室层级
    /// </summary>
    public const string RoomLevelDeletedEvent = "vc.room_level.deleted_v1";

    /// <summary>
    /// 创建会议室
    /// </summary>
    public const string RoomCreatedEvent = "vc.room.created_v1";

    /// <summary>
    /// 更新会议室
    /// </summary>
    public const string RoomUpdatedEvent = "vc.room.updated_v1";

    /// <summary>
    /// 删除会议室
    /// </summary>
    public const string RoomDeletedEvent = "vc.room.deleted_v1";

    /// <summary>
    /// 预约配置更新
    /// </summary>
    public const string ReserveConfigUpdated = "vc.reserve_config.updated_v1";

    /// <summary>
    /// 会议开始事件（机器人维度）
    /// </summary>
    public const string VcBotMeetingStarted = "vc.bot.meeting_started_v1";
    #endregion

    #region Mail Events
    /// <summary>
    /// 邮件接收事件
    /// </summary>
    public const string MessageReceived = "mail.user_mailbox.event.message_received_v1";
    #endregion

    #region Hire Events
    /// <summary>
    /// 投递删除事件
    /// </summary>
    public const string HireApplicationDeleted = "hire.application.deleted_v1";

    /// <summary>
    /// 投递阶段变更事件
    /// </summary>
    public const string HireApplicationStageChanged = "hire.application.stage_changed_v1";

    /// <summary>
    /// Offer 状态变更事件
    /// </summary>
    public const string HireOfferStatusChanged = "hire.offer.status_changed_v1";

    /// <summary>
    /// 导入 e-HR 事件
    /// </summary>
    public const string HireEhrImportTaskImported = "hire.ehr_import_task.imported_v1";

    /// <summary>
    /// 实习 Offer 导入 e-HR 事件
    /// </summary>
    public const string HireEhrImportTaskForInternshipOfferImported = "hire.ehr_import_task_for_internship_offer.imported_v1";

    /// <summary>
    /// 人才标签订阅事件
    /// </summary>
    public const string HireTalentTagSubscription = "hire.talent.tag_subscription_v1";

    /// <summary>
    /// 人才删除事件
    /// </summary>
    public const string HireTalentDeleted = "hire.talent.deleted_v1";

    /// <summary>
    /// 账号创建事件
    /// </summary>
    public const string HireEcoAccountCreated = "hire.eco_account.created_v1";

    /// <summary>
    /// 背调创建事件
    /// </summary>
    public const string HireEcoBackgroundCheckCreated = "hire.eco_background_check.created_v1";

    /// <summary>
    /// 背调取消事件
    /// </summary>
    public const string HireEcoBackgroundCheckCanceled = "hire.eco_background_check.canceled_v1";

    /// <summary>
    /// 创建笔试事件
    /// </summary>
    public const string HireEcoExamCreated = "hire.eco_exam.created_v1";

    /// <summary>
    /// 内推账户余额变更事件
    /// </summary>
    public const string HireReferralAccountAssetsUpdate = "hire.referral_account.assets_update_v1";
    #endregion

    #region Performance Events
    /// <summary>
    /// 绩效结果开通事件
    /// </summary>
    public const string PerformanceStageTaskOpenResult = "performance.stage_task.open_result_v2";

    /// <summary>
    /// 绩效数据变更事件
    /// </summary>
    public const string PerformanceReviewDataChanged = "performance.review_data.changed_v2";
    #endregion

    #region Acs Events
    /// <summary>
    /// 门禁用户信息变更事件
    /// </summary>
    public const string AcsUserUpdated = "acs.user.updated_v1";

    /// <summary>
    /// 门禁通行记录创建事件
    /// </summary>
    public const string AcsAccessRecordCreated = "acs.access_record.created_v1";
    #endregion

    #region Helpdesk Events
    /// <summary>
    /// 服务台工单创建事件
    /// </summary>
    public const string HelpdeskTicketCreated = "helpdesk.ticket.created_v1";

    /// <summary>
    /// 服务台工单更新事件
    /// </summary>
    public const string HelpdeskTicketUpdated = "helpdesk.ticket.updated_v1";

    /// <summary>
    /// 服务台工单消息创建事件
    /// </summary>
    public const string HelpdeskTicketMessageCreated = "helpdesk.ticket_message.created_v1";

    /// <summary>
    /// 服务台推送审批通知事件
    /// </summary>
    public const string HelpdeskNotificationApprove = "helpdesk.notification.approve_v1";
    #endregion

    #region Application Events
    /// <summary>
    /// 机器人自定义菜单事件
    /// </summary>
    public const string ApplicationBotMenu = "application.bot.menu_v6";

    /// <summary>
    /// 应用创建事件
    /// </summary>
    public const string ApplicationCreated = "application.application.created_v6";

    /// <summary>
    /// 员工免审安装应用事件
    /// </summary>
    public const string ApplicationVisibilityAdded = "application.application.visibility.added_v6";

    /// <summary>
    /// 新增应用反馈事件
    /// </summary>
    public const string ApplicationFeedbackCreated = "application.application.feedback.created_v6";

    /// <summary>
    /// 应用反馈更新事件
    /// </summary>
    public const string ApplicationFeedbackUpdated = "application.application.feedback.updated_v6";

    /// <summary>
    /// 申请发布应用事件
    /// </summary>
    public const string ApplicationAppVersionPublishApply = "application.application.app_version.publish_apply_v6";

    /// <summary>
    /// 撤回应用发布申请事件
    /// </summary>
    public const string ApplicationAppVersionPublishRevoke = "application.application.app_version.publish_revoke_v6";

    /// <summary>
    /// 应用审核事件
    /// </summary>
    public const string ApplicationAppVersionAudit = "application.application.app_version.audit_v6";
    #endregion

    #region Auth Events
    /// <summary>
    /// 撤销用户授权事件
    /// </summary>
    public const string AuthUserAccessTokenRevoked = "auth.user_access_token.revoked_v4";
    #endregion

    #region MeetingRoom Events
    /// <summary>
    /// 会议室创建事件
    /// </summary>
    public const string MeetingRoomCreated = "meeting_room.meeting_room.created_v1";

    /// <summary>
    /// 会议室属性变更事件
    /// </summary>
    public const string MeetingRoomUpdated = "meeting_room.meeting_room.updated_v1";

    /// <summary>
    /// 会议室删除事件
    /// </summary>
    public const string MeetingRoomDeleted = "meeting_room.meeting_room.deleted_v1";
    #endregion

#region Moments Events
    /// <summary>
    /// 发布帖子
    /// </summary>
    public const string MomentsPostCreated = "moments.post.created_v1";

    /// <summary>
    /// 删除帖子
    /// </summary>
    public const string MomentsPostDeleted = "moments.post.deleted_v1";

    /// <summary>
    /// 发布评论
    /// </summary>
    public const string MomentsCommentCreated = "moments.comment.created_v1";

    /// <summary>
    /// 删除评论
    /// </summary>
    public const string MomentsCommentDeleted = "moments.comment.deleted_v1";

    /// <summary>
    /// 表情互动
    /// </summary>
    public const string MomentsReactionCreated = "moments.reaction.created_v1";

    /// <summary>
    /// 取消表情互动
    /// </summary>
    public const string MomentsReactionDeleted = "moments.reaction.deleted_v1";

    /// <summary>
    /// 帖子统计数据变更
    /// </summary>
    public const string MomentsPostStatisticsUpdated = "moments.post_statistics.updated_v1";

    #endregion
    #region ELearning Events
    /// <summary>
    /// 课程学习进度新增事件
    /// </summary>
    public const string ELearningCourseRegistrationCreated = "elearning.course_registration.created_v2";

    /// <summary>
    /// 课程学习进度更新事件
    /// </summary>
    public const string ELearningCourseRegistrationUpdated = "elearning.course_registration.updated_v2";

    /// <summary>
    /// 课程学习进度删除事件
    /// </summary>
    public const string ELearningCourseRegistrationDeleted = "elearning.course_registration.deleted_v2";

    #endregion
    #region SecurityCompliance Events
    /// <summary>
    /// 设备信息变更事件
    /// </summary>
    public const string SecurityComplianceDeviceChanged = "security_and_compliance.device_record.device_change_event_v2";

    /// <summary>
    /// 设备申报事件
    /// </summary>
    public const string SecurityComplianceDeviceApplied = "security_and_compliance.device_apply_record.device_apply_event_v2";

    #endregion
    #region Compensation Events
    /// <summary>
    /// 薪资档案变更
    /// </summary>
    public const string CompensationArchiveChanged = "compensation.archive.changed_v1";

    #endregion
    #region Payroll Events
    /// <summary>
    /// 发薪活动变更事件
    /// </summary>
    public const string PayrollPaymentActivityStatusChanged = "payroll.payment_activity.status_changed_v1";

    /// <summary>
    /// 发薪活动封存
    /// </summary>
    public const string PayrollPaymentActivityApproved = "payroll.payment_activity.approved_v1";

    #endregion
#region CoreHr Events
    /// <summary>
    /// 元数据信息变更
    /// </summary>
    public const string CoreHrMetaDataUpdated = "corehr.common_data.meta_data.updated_v1";

    /// <summary>
    /// 用户ID映射变更
    /// </summary>
    public const string CoreHrUserIdMappingChanged = "corehr.common_data.id.user_mapping_changed_v1";

    /// <summary>
    /// 【事件】个人信息创建
    /// </summary>
    public const string CoreHrPersonCreated = "corehr.person.created_v1";

    /// <summary>
    /// 【事件】更新个人信息
    /// </summary>
    public const string CoreHrPersonUpdated = "corehr.person.updated_v1";

    /// <summary>
    /// 【事件】个人信息删除
    /// </summary>
    public const string CoreHrPersonDeleted = "corehr.person.deleted_v1";

    /// <summary>
    /// 【事件】创建雇佣信息
    /// </summary>
    public const string CoreHrEmploymentCreated = "corehr.employment.created_v1";

    /// <summary>
    /// 【事件】更新雇佣信息
    /// </summary>
    public const string CoreHrEmploymentUpdated = "corehr.employment.updated_v1";

    /// <summary>
    /// 【事件】删除雇佣信息
    /// </summary>
    public const string CoreHrEmploymentDeleted = "corehr.employment.deleted_v1";

    /// <summary>
    /// 员工完成转正
    /// </summary>
    public const string CoreHrEmploymentConverted = "corehr.employment.converted_v1";

    /// <summary>
    /// 员工完成离职
    /// </summary>
    public const string CoreHrEmploymentResigned = "corehr.employment.resigned_v1";

    /// <summary>
    /// 任职信息创建
    /// </summary>
    public const string CoreHrJobDataCreated = "corehr.job_data.created_v1";

    /// <summary>
    /// 任职信息更新
    /// </summary>
    public const string CoreHrJobDataUpdated = "corehr.job_data.updated_v1";

    /// <summary>
    /// 任职信息删除
    /// </summary>
    public const string CoreHrJobDataDeleted = "corehr.job_data.deleted_v1";

    /// <summary>
    /// 员工完成入职
    /// </summary>
    public const string CoreHrJobDataEmployed = "corehr.job_data.employed_v1";

    /// <summary>
    /// 员工完成异动
    /// </summary>
    public const string CoreHrJobDataChanged = "corehr.job_data.changed_v1";

    /// <summary>
    /// 【事件】创建部门
    /// </summary>
    public const string CoreHrDepartmentCreated = "corehr.department.created_v1";

    /// <summary>
    /// 【事件】更新部门
    /// </summary>
    public const string CoreHrDepartmentUpdated = "corehr.department.updated_v1";

    /// <summary>
    /// 【事件】删除部门
    /// </summary>
    public const string CoreHrDepartmentDeleted = "corehr.department.deleted_v1";

    /// <summary>
    /// 【事件】创建职务
    /// </summary>
    public const string CoreHrJobCreated = "corehr.job.created_v1";

    /// <summary>
    /// 【事件】更新职务
    /// </summary>
    public const string CoreHrJobUpdated = "corehr.job.updated_v1";

    /// <summary>
    /// 【事件】删除职务
    /// </summary>
    public const string CoreHrJobDeleted = "corehr.job.deleted_v1";

    /// <summary>
    /// 入职信息变更
    /// </summary>
    public const string CoreHrPreHireUpdated = "corehr.pre_hire.updated_v1";

    /// <summary>
    /// 异动状态变更
    /// </summary>
    public const string CoreHrJobChangeUpdated = "corehr.job_change.updated_v1";

    /// <summary>
    /// 合同创建
    /// </summary>
    public const string CoreHrContractCreated = "corehr.contract.created_v1";

    /// <summary>
    /// 合同更新
    /// </summary>
    public const string CoreHrContractUpdated = "corehr.contract.updated_v1";

    /// <summary>
    /// 合同删除
    /// </summary>
    public const string CoreHrContractDeleted = "corehr.contract.deleted_v1";

    /// <summary>
    /// 组织角色授权变更
    /// </summary>
    public const string CoreHrOrgRoleAuthorizationUpdated = "corehr.org_role_authorization.updated_v1";

    /// <summary>
    /// 离职申请状态变更
    /// </summary>
    public const string CoreHrOffboardingUpdated = "corehr.offboarding.updated_v1";

    #endregion
#region CoreHrV2 Events
    /// <summary>
    /// 人员信息变更
    /// </summary>
    public const string CoreHrV2EmployeeDomainEvent = "corehr.employee.domain_event_v2";

    /// <summary>
    /// 【事件】创建部门
    /// </summary>
    public const string CoreHrV2DepartmentCreated = "corehr.department.created_v2";

    /// <summary>
    /// 【事件】更新部门
    /// </summary>
    public const string CoreHrV2DepartmentUpdated = "corehr.department.updated_v2";

    /// <summary>
    /// 【事件】创建地点
    /// </summary>
    public const string CoreHrV2LocationCreated = "corehr.location.created_v2";

    /// <summary>
    /// 【事件】更新地点
    /// </summary>
    public const string CoreHrV2LocationUpdated = "corehr.location.updated_v2";

    /// <summary>
    /// 【事件】删除地点
    /// </summary>
    public const string CoreHrV2LocationDeleted = "corehr.location.deleted_v2";

    /// <summary>
    /// 创建公司
    /// </summary>
    public const string CoreHrV2CompanyCreated = "corehr.company.created_v2";

    /// <summary>
    /// 【事件】更新公司
    /// </summary>
    public const string CoreHrV2CompanyUpdated = "corehr.company.updated_v2";

    /// <summary>
    /// 【事件】删除公司
    /// </summary>
    public const string CoreHrV2CompanyDeleted = "corehr.company.deleted_v2";

    /// <summary>
    /// 【事件】创建成本中心
    /// </summary>
    public const string CoreHrV2CostCenterCreated = "corehr.cost_center.created_v2";

    /// <summary>
    /// 【事件】更新成本中心
    /// </summary>
    public const string CoreHrV2CostCenterUpdated = "corehr.cost_center.updated_v2";

    /// <summary>
    /// 【事件】删除成本中心
    /// </summary>
    public const string CoreHrV2CostCenterDeleted = "corehr.cost_center.deleted_v2";

    /// <summary>
    /// 【事件】创建自定义组织
    /// </summary>
    public const string CoreHrV2CustomOrgCreated = "corehr.custom_org.created_v2";

    /// <summary>
    /// 【事件】更新自定义组织
    /// </summary>
    public const string CoreHrV2CustomOrgUpdated = "corehr.custom_org.updated_v2";

    /// <summary>
    /// 【事件】删除自定义组织
    /// </summary>
    public const string CoreHrV2CustomOrgDeleted = "corehr.custom_org.deleted_v2";

    /// <summary>
    /// 组织架构调整状态变更事件
    /// </summary>
    public const string CoreHrV2ApprovalGroupsUpdated = "corehr.approval_groups.updated_v2";

    /// <summary>
    /// 【事件】创建序列
    /// </summary>
    public const string CoreHrV2JobFamilyCreated = "corehr.job_family.created_v2";

    /// <summary>
    /// 【事件】更新序列
    /// </summary>
    public const string CoreHrV2JobFamilyUpdated = "corehr.job_family.updated_v2";

    /// <summary>
    /// 【事件】删除序列
    /// </summary>
    public const string CoreHrV2JobFamilyDeleted = "corehr.job_family.deleted_v2";

    /// <summary>
    /// 【事件】创建职级
    /// </summary>
    public const string CoreHrV2JobLevelCreated = "corehr.job_level.created_v2";

    /// <summary>
    /// 【事件】更新职级
    /// </summary>
    public const string CoreHrV2JobLevelUpdated = "corehr.job_level.updated_v2";

    /// <summary>
    /// 【事件】删除职级
    /// </summary>
    public const string CoreHrV2JobLevelDeleted = "corehr.job_level.deleted_v2";

    /// <summary>
    /// 【事件】创建职等
    /// </summary>
    public const string CoreHrV2JobGradeCreated = "corehr.job_grade.created_v2";

    /// <summary>
    /// 【事件】更新职等
    /// </summary>
    public const string CoreHrV2JobGradeUpdated = "corehr.job_grade.updated_v2";

    /// <summary>
    /// 【事件】删除职等
    /// </summary>
    public const string CoreHrV2JobGradeDeleted = "corehr.job_grade.deleted_v2";

    /// <summary>
    /// 通道创建事件
    /// </summary>
    public const string CoreHrV2PathwayCreated = "corehr.pathway.created_v2";

    /// <summary>
    /// 通道更新事件
    /// </summary>
    public const string CoreHrV2PathwayUpdated = "corehr.pathway.updated_v2";

    /// <summary>
    /// 通道删除事件
    /// </summary>
    public const string CoreHrV2PathwayDeleted = "corehr.pathway.deleted_v2";

    /// <summary>
    /// 创建岗位事件
    /// </summary>
    public const string CoreHrV2PositionCreated = "corehr.position.created_v2";

    /// <summary>
    /// 更新岗位事件
    /// </summary>
    public const string CoreHrV2PositionUpdated = "corehr.position.updated_v2";

    /// <summary>
    /// 删除岗位事件
    /// </summary>
    public const string CoreHrV2PositionDeleted = "corehr.position.deleted_v2";

    /// <summary>
    /// 入职流程状态变更
    /// </summary>
    public const string CoreHrV2OnboardingTaskChanged = "corehr.pre_hire.onboarding_task_changed_v2";

    /// <summary>
    /// 试用期状态变更
    /// </summary>
    public const string CoreHrV2ProbationUpdated = "corehr.probation.updated_v2";

    /// <summary>
    /// 异动状态变更
    /// </summary>
    public const string CoreHrV2JobChangeStatusUpdated = "corehr.job_change.status_updated_v2";

    /// <summary>
    /// 异动信息变更
    /// </summary>
    public const string CoreHrV2JobChangeUpdated = "corehr.job_change.updated_v2";

    /// <summary>
    /// 离职信息变更
    /// </summary>
    public const string CoreHrV2OffboardingUpdated = "corehr.offboarding.updated_v2";

    /// <summary>
    /// 离职申请状态变更
    /// </summary>
    public const string CoreHrV2OffboardingStatusUpdated = "corehr.offboarding.status_updated_v2";

    /// <summary>
    /// 离职流转状态变更
    /// </summary>
    public const string CoreHrV2OffboardingChecklistUpdated = "corehr.offboarding.checklist_updated_v2";

    /// <summary>
    /// 电子签文件状态变更事件
    /// </summary>
    public const string CoreHrV2SignatureFileStatusUpdated = "corehr.signature_file.status_updated_v2";

    /// <summary>
    /// 流程实例状态变化
    /// </summary>
    public const string CoreHrV2ProcessStatusUpdate = "corehr.process.status.update_v2";

    /// <summary>
    /// 流程实例信息变更
    /// </summary>
    public const string CoreHrV2ProcessUpdated = "corehr.process.updated_v2";

    /// <summary>
    /// 流程节点状态变更
    /// </summary>
    public const string CoreHrV2ProcessNodeUpdated = "corehr.process.node.updated_v2";

    /// <summary>
    /// 审批任务状态变更
    /// </summary>
    public const string CoreHrV2ProcessApproverUpdated = "corehr.process.approver.updated_v2";

    /// <summary>
    /// 抄送单据状态变更
    /// </summary>
    public const string CoreHrV2ProcessCcUpdated = "corehr.process.cc.updated_v2";

    /// <summary>
    /// 流程评论事件
    /// </summary>
    public const string CoreHrV2ProcessCommentInfoUpdated = "corehr.process_comment_info.updated_v2";

    #endregion
    /// <summary>
    /// 用户加入群聊事件
    /// </summary>
    [Obsolete("已由 " + nameof(ChatMemberUserAdd) + " 取代，将在 4.0 移除")]
    public const string UserAddedToGroup = "im.chat.member.user_added_v1";

    /// <summary>
    /// 用户离开群聊事件
    /// </summary>
    [Obsolete("已由 " + nameof(ChatMemberUserDelete) + " 取代，将在 4.0 移除")]
    public const string UserRemovedFromGroup = "im.chat.member.user_deleted_v1";

    /// <summary>
    /// 群聊信息更新事件
    /// </summary>
    [Obsolete("已由 " + nameof(ChatUpdated) + " 取代，将在 4.0 移除")]
    public const string GroupUpdated = "im.chat.updated_v1";

    /// <summary>
    /// 日程事件
    /// </summary>
    [Obsolete("已由 " + nameof(CalendarEventChanged) + " 取代，将在 4.0 移除")]
    public const string CalendarEvent = "calendar.event.updated_v4";

    /// <summary>
    /// 会议开始事件
    /// </summary>
    [Obsolete("已由 " + nameof(MeetingMeetingStarted) + " 取代，将在 4.0 移除")]
    public const string MeetingStart = "meeting.meeting.started_v1";

    /// <summary>
    /// 会议结束事件
    /// </summary>
    [Obsolete("已由 " + nameof(MeetingMeetingEnded) + " 取代，将在 4.0 移除")]
    public const string MeetingEnd = "meeting.meeting.ended_v1";
}