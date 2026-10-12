# Mud.Feishu.EventCallback

飞书服务端 SDK 的 .NET 适配版，提供事件与回调功能强数据模型封装。

## 项目简介

`Mud.Feishu.EventCallback` 是 MudFeishu SDK 的核心组件之一，专门用于处理飞书平台的事件回调。本项目提供了完整的强类型数据模型，帮助开发者轻松处理飞书各种事件推送。

## 特性

- ✅ **强类型数据模型** - 所有事件数据均有完整的类型定义和 XML 文档注释
- ✅ **自动生成事件处理器** - 使用源代码生成器自动生成事件处理器基类
- ✅ **多框架支持** - 支持 .NET Standard 2.0、.NET 6.0、.NET 8.0、.NET 10.0
- ✅ **原生 AOT 支持** - net8.0+ 一等公民支持 Native AOT 发布，事件回调 JSON 全部走源生成上下文
- ✅ **完整的事件覆盖** - 覆盖飞书主要业务场景的事件回调
- ✅ **官方文档链接** - 每个事件模型都包含官方文档链接

## 支持的事件类型

> 共 206 个事件、25 个业务域。下表由源码中的 `[GenerateEventHandler]` 标注直接汇总。

### 👥 组织架构事件（Organization）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `contact.custom_attr_event.updated_v3` | `CustomAttrUpdateEventHandler` | 成员字段变更事件处理器 |
| `contact.department.created_v3` | `DepartmentCreatedEventHandler` | 部门创建事件结果 |
| `contact.department.deleted_v3` | `DepartmentDeleteEventHandler` | 部门被删除事件处理器 |
| `contact.department.updated_v3` | `DepartmentUpdateEventHandler` | 部门信息变化 |
| `contact.employee_type_enum.actived_v3` | `EmployeeTypeEnumActivedEventHandler` | 启用人员类型事件处理器 |
| `contact.employee_type_enum.created_v3` | `EmployeeTypeEnumCreatedEventHandler` | 新建人员类型事件处理器 |
| `contact.employee_type_enum.deactivated_v3` | `EmployeeTypeEnumDeActivedEventHandler` | 停用人员类型事件处理器 |
| `contact.employee_type_enum.deleted_v3` | `EmployeeTypeEnumDeleteEventHandler` | 修改人员类型名称事件处理器 |
| `contact.employee_type_enum.updated_v3` | `EmployeeTypeEnumUpdateEventHandler` | 修改人员类型名称 事件体 |
| `contact.scope.updated_v3` | `ContactScopeUpdatedEventHandler` | 通讯录权限范围变更 |
| `contact.user.created_v3` | `UserCreateEventHandler` | 员工入职事件处理器 |
| `contact.user.deleted_v3` | `UserDeleteEventHandler` | 员工离职事件处理器 |
| `contact.user.updated_v3` | `UserUpdateEventHandler` | 员工信息被修改事件处理器 |

### 💬 即时通讯事件（IM）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `im.chat.access_event.bot_p2p_chat_entered_v1` | `ChatBotP2pChatEnteredEventHandler` | 用户进入与机器人的会话 |
| `im.chat.disbanded_v1` | `ChatDisbandedEventHandler` | 群解散事件处理器 |
| `im.chat.member.bot.added_v1` | `ChatMemberBotAddedEventHandler` | 机器人进群事件处理器 |
| `im.chat.member.bot.deleted_v1` | `ChatMemberBotDeletedEventHandler` | 机器人被移出群事件处理器 |
| `im.chat.member.user.added_v1` | `ChatMemberUserAddedEventHandler` | 用户进群事件处理器 |
| `im.chat.member.user.deleted_v1` | `ChatMemberUserDeletedEventHandler` | 用户出群事件处理器 |
| `im.chat.member.user.withdrawn_v1` | `ChatMemberUserWithdrawnEventHandler` | 撤销拉用户进群事件处理器 |
| `im.chat.updated_v1` | `ChatUpdatedEventHandler` | 群配置修改事件处理器 |
| `im.message.message_read_v1` | `MessageReadEventHandler` | 消息已读事件处理器 |
| `im.message.reaction.created_v1` | `MessageReactionCreatedEventHandler` | 新增消息表情回复事件处理器 |
| `im.message.reaction.deleted_v1` | `MessageReactionDeletedEventHandler` | 删除消息表情回复事件处理器 |
| `im.message.recalled_v1` | `MessageRecalledEventHandler` | 撤回消息事件处理器 |
| `im.message.receive_v1` | `MessageReceiveEventHandler` | 接收消息事件处理器 |

### 📋 审批事件（Approval）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `approval_cc` | `ApprovalCcEventHandler` | 审批抄送状态变更事件体 |
| `approval_instance` | `ApprovalInstanceEventHandler` | 审批实例状态变更事件体 |
| `approval_task` | `ApprovalTaskEventHandler` | 审批任务状态变更事件体 |
| `approval.approval.updated_v4` | `ApprovalApprovalUpdatedEventHandler` | 审批定义更新事件体 |
| `approval.instance.remedy_group_update_v4` | `ApprovalInstanceRemedyGroupUpdateEventHandler` | 补卡审批事件体 |
| `approval.instance.trip_group_update_v4` | `ApprovalInstanceTripGroupUpdateEventHandler` | 出差审批事件体 |
| `approval.task.status_changed_v4` | `ApprovalTaskStatusChangedEventHandler` | 审批任务状态变更事件 |
| `leave_approval` | `LeaveApprovalEventHandler` | 请假审批事件体 |
| `leave_approval_revert` | `LeaveApprovalRevertEventHandler` | 请假审批通过并撤销事件体 |
| `leave_approvalV2` | `LeaveApprovalV2EventHandler` | 请假审批事件体 |
| `out_approval` | `OutApprovalEventHandler` | 外出审批事件体 |
| `shift_approval` | `ShiftApprovalEventHandler` | 换班审批事件体 |
| `work_approval` | `WorkApprovalEventHandler` | 加班审批事件体 |
| `work_approval_revert` | `WorkApprovalRevertEventHandler` | 加班审批通过并撤销事件体 |

### ✅ 任务事件（Task）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `task.task.comment.updated_v1` | `TaskCommentUpdatedEventHandler` | 任务评论信息变更事件体 |
| `task.task.update_tenant_v1` | `TaskUpdateTenantEventHandler` | 任务信息变更（租户维度） 事件体 |
| `task.task.updated_v1` | `TaskUpdatedEventHandler` | 任务信息变更（应用维度） 事件体 |

### ☁️ 云盘事件（Drive）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `drive.file.created_in_folder_v1` | `DriveFileCreatedEventHandler` | 文件夹下文件创建 |
| `drive.file.deleted_v1` | `DriveFileDeletedEventHandler` | 文件彻底删除 |
| `drive.file.edit_v1` | `DriveFileEditEventHandler` | 文件编辑 |
| `drive.file.permission_member_added_v1` | `DriveFilePermissionMemberAddedEventHandler` | 文件协作者添加 |
| `drive.file.permission_member_applied_v1` | `DriveFilePermissionMemberAppliedEventHandler` | 文件协作者权限申请 |
| `drive.file.permission_member_removed_v1` | `DriveFilePermissionMemberRemovedEventHandler` | 文件协作者移除 |
| `drive.file.read_v1` | `DriveFileReadEventHandler` | 文件已读 |
| `drive.file.title_updated_v1` | `DriveFileTitleUpdatedEventHandler` | 文件标题变更 |
| `drive.file.trashed_v1` | `DriveFileTrashedEventHandler` | 文件删除到回收站 |
| `drive.notice.comment_add_v1` | `DriveNoticeCommentAddEventHandler` | 添加评论、回复通知事件 |

### 📊 多维表格事件（Bitable）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `drive.file.bitable_field_changed_v1` | `BitableFieldChangedEventHandler` | 多维表格字段变更 |
| `drive.file.bitable_record_changed_v1` | `BitableRecordChangedEventHandler` | 多维表格记录变更 |

### 📅 日历事件（Calendar）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `calendar.calendar.acl.created_v4` | `CalendarAclCreatedEventHandler` | 创建 ACL |
| `calendar.calendar.acl.deleted_v4` | `CalendarAclDeleteEventHandler` | 删除 ACL |
| `calendar.calendar.changed_v4` | `CalendarChangedEventHandler` | 日历变更 |
| `calendar.calendar.event.changed_v4` | `CalendarEventChangedEventHandler` | 日程变更 |
| `meeting_room.meeting_room.status_changed_v1` | `RoomStatusChangedEventHandler` | 会议室状态信息变更 |
| `third_party_meeting_room_event_created` | `ThirdPartyMeetingRoomEventCreatedEventHandler` | 第三方会议室日程变动 |

### ⏰ 考勤事件（Attendance）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `attendance.user_flow.created_v1` | `AttendanceUserFlowCreatedEventHandler` | 用户打卡成功后，推送该用户的打卡流水消息。 |
| `attendance.user_task.updated_v1` | `AttendanceUserTaskUpdatedEventHandler` | 当用户任务变更后，推送该用户的任务状态变更消息。 |

### 🎥 视频会议事件（VideoConferencing）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `vc.bot.meeting_started_v1` | `VcBotMeetingStartedEventHandler` | 会议开始 |
| `vc.meeting.all_meeting_ended_v1` | `MeetingAllMeetingEndedEventHandler` | 企业会议结束 |
| `vc.meeting.all_meeting_started_v1` | `MeetingAllMeetingStartedEventHandler` | 企业会议开始 |
| `vc.meeting.join_meeting_v1` | `MeetingJoinMeetingEventHandler` | 加入会议 |
| `vc.meeting.leave_meeting_v1` | `MeetingLeaveMeetingEventHandler` | 离开会议 |
| `vc.meeting.meeting_ended_v1` | `MeetingMeetingEndedEventHandler` | 会议结束 |
| `vc.meeting.meeting_started_v1` | `MeetingMeetingStartedEventHandler` | 会议开始 |
| `vc.meeting.participant_meeting_ended_v1` | `MeetingParticipantMeetingEndedEventHandler` | 参与的会议结束 |
| `vc.meeting.recording_ended_v1` | `MeetingRecordingEndedEventHandler` | 停止录制 |
| `vc.meeting.recording_ready_v1` | `MeetingRecordingReadyEventHandler` | 录制完成 |
| `vc.meeting.recording_started_v1` | `MeetingRecordingStartedEventHandler` | 开始录制 |
| `vc.meeting.share_ended_v1` | `MeetingShareEndedEventHandler` | 会议共享结束 |
| `vc.meeting.share_started_v1` | `MeetingShareStartedEventHandler` | 会议共享开始 |
| `vc.note.generated_v1` | `VcNoteGeneratedEventHandler` | 纪要生成 |
| `vc.reserve_config.updated_v1` | `ReserveConfigUpdatedEventHandler` | 更新会议室预定限制 |
| `vc.room_level.created_v1` | `RoomLevelCreatedEventHandler` | 会议室层级创建 |
| `vc.room_level.deleted_v1` | `RoomLevelDeletedEventHandler` | 会议室层级删除 |
| `vc.room_level.updated_v1` | `RoomLevelUpdatedEventHandler` | 会议室层级更新 |
| `vc.room.created_v1` | `RoomCreatedEventHandler` | 会议室创建 |
| `vc.room.deleted_v1` | `RoomDeletedEventHandler` | 会议室删除 |
| `vc.room.updated_v1` | `RoomUpdatedEventHandler` | 会议室更新 |

### 📧 邮箱事件（Mail）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `mail.user_mailbox.event.message_received_v1` | `MessageReceivedEventHandler` | 收信通知 |

### 🧑‍💼 招聘事件（Hire）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `hire.application.deleted_v1` | `HireApplicationDeletedEventHandler` | 投递删除 |
| `hire.application.stage_changed_v1` | `HireApplicationStageChangedEventHandler` | 投递阶段变更 |
| `hire.eco_account.created_v1` | `HireEcoAccountCreatedEventHandler` | 账号创建 |
| `hire.eco_background_check.canceled_v1` | `HireEcoBackgroundCheckCanceledEventHandler` | 背调取消 |
| `hire.eco_background_check.created_v1` | `HireEcoBackgroundCheckCreatedEventHandler` | 背调创建 |
| `hire.eco_exam.created_v1` | `HireEcoExamCreatedEventHandler` | 创建笔试 |
| `hire.ehr_import_task_for_internship_offer.imported_v1` | `HireEhrImportTaskForInternshipOfferImportedEventHandler` | 实习 Offer 导入 e-HR |
| `hire.ehr_import_task.imported_v1` | `HireEhrImportTaskImportedEventHandler` | 导入 e-HR |
| `hire.offer.status_changed_v1` | `HireOfferStatusChangedEventHandler` | Offer 状态变更 |
| `hire.referral_account.assets_update_v1` | `HireReferralAccountAssetsUpdateEventHandler` | 内推账户余额变更 |
| `hire.talent.deleted_v1` | `HireTalentDeletedEventHandler` | 人才删除 |
| `hire.talent.tag_subscription_v1` | `HireTalentTagSubscriptionEventHandler` | 人才标签订阅 |

### 📈 绩效事件（Performance）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `performance.review_data.changed_v2` | `PerformanceReviewDataChangedEventHandler` | 绩效数据变更 |
| `performance.stage_task.open_result_v2` | `PerformanceStageTaskOpenEventHandler` | 绩效结果开通 |

### 🚪 智能门禁事件（Acs）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `acs.access_record.created_v1` | `AcsAccessRecordCreatedEventHandler` | 门禁通行记录创建 |
| `acs.user.updated_v1` | `AcsUserUpdatedEventHandler` | 门禁用户信息变更 |

### 🎧 服务台事件（Helpdesk）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `helpdesk.notification.approve_v1` | `HelpdeskNotificationApproveEventHandler` | 服务台推送审批通知 |
| `helpdesk.ticket_message.created_v1` | `HelpdeskTicketMessageCreatedEventHandler` | 服务台工单消息创建 |
| `helpdesk.ticket.created_v1` | `HelpdeskTicketCreatedEventHandler` | 服务台工单创建 |
| `helpdesk.ticket.updated_v1` | `HelpdeskTicketUpdatedEventHandler` | 服务台工单更新 |

### 🧩 应用信息事件（Application）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `application.application.app_version.audit_v6` | `ApplicationAppVersionAuditEventHandler` | 应用审核 |
| `application.application.app_version.publish_apply_v6` | `ApplicationAppVersionPublishApplyEventHandler` | 申请发布应用 |
| `application.application.app_version.publish_revoke_v6` | `ApplicationAppVersionPublishRevokeEventHandler` | 撤回应用发布申请 |
| `application.application.created_v6` | `ApplicationCreatedEventHandler` | 应用创建 |
| `application.application.feedback.created_v6` | `ApplicationFeedbackCreatedEventHandler` | 新增应用反馈 |
| `application.application.feedback.updated_v6` | `ApplicationFeedbackUpdatedEventHandler` | 反馈更新 |
| `application.application.visibility.added_v6` | `ApplicationVisibilityAddedEventHandler` | 员工免审安装应用 |
| `application.bot.menu_v6` | `ApplicationBotMenuEventHandler` | 机器人自定义菜单事件 |

### 🔐 认证授权事件（Auth）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `auth.user_access_token.revoked_v4` | `AuthUserAccessTokenRevokedEventHandler` | 撤销用户授权事件 |

### 🏢 会议室事件（MeetingRoom）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `meeting_room.meeting_room.created_v1` | `MeetingRoomCreatedEventHandler` | 会议室创建 |
| `meeting_room.meeting_room.deleted_v1` | `MeetingRoomDeletedEventHandler` | 会议室删除 |
| `meeting_room.meeting_room.updated_v1` | `MeetingRoomUpdatedEventHandler` | 会议室属性变更 |

### 🧑‍🏫 飞书人事（标准版）事件（CoreHr）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `corehr.common_data.id.user_mapping_changed_v1` | `CoreHrUserIdMappingChangedEventHandler` | 用户ID映射变更 |
| `corehr.common_data.meta_data.updated_v1` | `CoreHrMetaDataUpdatedEventHandler` | 元数据信息变更 |
| `corehr.contract.created_v1` | `CoreHrContractCreatedEventHandler` | 合同创建 |
| `corehr.contract.deleted_v1` | `CoreHrContractDeletedEventHandler` | 合同删除 |
| `corehr.contract.updated_v1` | `CoreHrContractUpdatedEventHandler` | 合同更新 |
| `corehr.department.created_v1` | `CoreHrDepartmentCreatedEventHandler` | 【事件】创建部门 |
| `corehr.department.deleted_v1` | `CoreHrDepartmentDeletedEventHandler` | 【事件】删除部门 |
| `corehr.department.updated_v1` | `CoreHrDepartmentUpdatedEventHandler` | 【事件】更新部门 |
| `corehr.employment.converted_v1` | `CoreHrEmploymentConvertedEventHandler` | 员工完成转正 |
| `corehr.employment.created_v1` | `CoreHrEmploymentCreatedEventHandler` | 【事件】创建雇佣信息 |
| `corehr.employment.deleted_v1` | `CoreHrEmploymentDeletedEventHandler` | 【事件】删除雇佣信息 |
| `corehr.employment.resigned_v1` | `CoreHrEmploymentResignedEventHandler` | 员工完成离职 |
| `corehr.employment.updated_v1` | `CoreHrEmploymentUpdatedEventHandler` | 【事件】更新雇佣信息 |
| `corehr.job_change.updated_v1` | `CoreHrJobChangeUpdatedEventHandler` | 异动状态变更 |
| `corehr.job_data.changed_v1` | `CoreHrJobDataChangedEventHandler` | 员工完成异动 |
| `corehr.job_data.created_v1` | `CoreHrJobDataCreatedEventHandler` | 任职信息创建 |
| `corehr.job_data.deleted_v1` | `CoreHrJobDataDeletedEventHandler` | 任职信息删除 |
| `corehr.job_data.employed_v1` | `CoreHrJobDataEmployedEventHandler` | 员工完成入职 |
| `corehr.job_data.updated_v1` | `CoreHrJobDataUpdatedEventHandler` | 任职信息更新 |
| `corehr.job.created_v1` | `CoreHrJobCreatedEventHandler` | 【事件】创建职务 |
| `corehr.job.deleted_v1` | `CoreHrJobDeletedEventHandler` | 【事件】删除职务 |
| `corehr.job.updated_v1` | `CoreHrJobUpdatedEventHandler` | 【事件】更新职务 |
| `corehr.offboarding.updated_v1` | `CoreHrOffboardingUpdatedEventHandler` | 离职申请状态变更 |
| `corehr.org_role_authorization.updated_v1` | `CoreHrOrgRoleAuthorizationUpdatedEventHandler` | 组织角色授权变更 |
| `corehr.person.created_v1` | `CoreHrPersonCreatedEventHandler` | 【事件】个人信息创建 |
| `corehr.person.deleted_v1` | `CoreHrPersonDeletedEventHandler` | 【事件】个人信息删除 |
| `corehr.person.updated_v1` | `CoreHrPersonUpdatedEventHandler` | 【事件】更新个人信息 |
| `corehr.pre_hire.updated_v1` | `CoreHrPreHireUpdatedEventHandler` | 入职信息变更 |

### 🏫 飞书人事（企业版）事件（CoreHrV2）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `corehr.approval_groups.updated_v2` | `CoreHrV2ApprovalGroupsUpdatedEventHandler` | 组织架构调整状态变更事件 |
| `corehr.company.created_v2` | `CoreHrV2CompanyCreatedEventHandler` | 创建公司 |
| `corehr.company.deleted_v2` | `CoreHrV2CompanyDeletedEventHandler` | 【事件】删除公司 |
| `corehr.company.updated_v2` | `CoreHrV2CompanyUpdatedEventHandler` | 【事件】更新公司 |
| `corehr.cost_center.created_v2` | `CoreHrV2CostCenterCreatedEventHandler` | 【事件】创建成本中心 |
| `corehr.cost_center.deleted_v2` | `CoreHrV2CostCenterDeletedEventHandler` | 【事件】删除成本中心 |
| `corehr.cost_center.updated_v2` | `CoreHrV2CostCenterUpdatedEventHandler` | 【事件】更新成本中心 |
| `corehr.custom_org.created_v2` | `CoreHrV2CustomOrgCreatedEventHandler` | 【事件】创建自定义组织 |
| `corehr.custom_org.deleted_v2` | `CoreHrV2CustomOrgDeletedEventHandler` | 【事件】删除自定义组织 |
| `corehr.custom_org.updated_v2` | `CoreHrV2CustomOrgUpdatedEventHandler` | 【事件】更新自定义组织 |
| `corehr.department.created_v2` | `CoreHrV2DepartmentCreatedEventHandler` | 【事件】创建部门 |
| `corehr.department.updated_v2` | `CoreHrV2DepartmentUpdatedEventHandler` | 【事件】更新部门 |
| `corehr.employee.domain_event_v2` | `CoreHrV2EmployeeDomainEventEventHandler` | 人员信息变更 |
| `corehr.job_change.status_updated_v2` | `CoreHrV2JobChangeStatusUpdatedEventHandler` | 异动状态变更 |
| `corehr.job_change.updated_v2` | `CoreHrV2JobChangeUpdatedEventHandler` | 异动信息变更 |
| `corehr.job_family.created_v2` | `CoreHrV2JobFamilyCreatedEventHandler` | 【事件】创建序列 |
| `corehr.job_family.deleted_v2` | `CoreHrV2JobFamilyDeletedEventHandler` | 【事件】删除序列 |
| `corehr.job_family.updated_v2` | `CoreHrV2JobFamilyUpdatedEventHandler` | 【事件】更新序列 |
| `corehr.job_grade.created_v2` | `CoreHrV2JobGradeCreatedEventHandler` | 【事件】创建职等 |
| `corehr.job_grade.deleted_v2` | `CoreHrV2JobGradeDeletedEventHandler` | 【事件】删除职等 |
| `corehr.job_grade.updated_v2` | `CoreHrV2JobGradeUpdatedEventHandler` | 【事件】更新职等 |
| `corehr.job_level.created_v2` | `CoreHrV2JobLevelCreatedEventHandler` | 【事件】创建职级 |
| `corehr.job_level.deleted_v2` | `CoreHrV2JobLevelDeletedEventHandler` | 【事件】删除职级 |
| `corehr.job_level.updated_v2` | `CoreHrV2JobLevelUpdatedEventHandler` | 【事件】更新职级 |
| `corehr.location.created_v2` | `CoreHrV2LocationCreatedEventHandler` | 【事件】创建地点 |
| `corehr.location.deleted_v2` | `CoreHrV2LocationDeletedEventHandler` | 【事件】删除地点 |
| `corehr.location.updated_v2` | `CoreHrV2LocationUpdatedEventHandler` | 【事件】更新地点 |
| `corehr.offboarding.checklist_updated_v2` | `CoreHrV2OffboardingChecklistUpdatedEventHandler` | 离职流转状态变更 |
| `corehr.offboarding.status_updated_v2` | `CoreHrV2OffboardingStatusUpdatedEventHandler` | 离职申请状态变更 |
| `corehr.offboarding.updated_v2` | `CoreHrV2OffboardingUpdatedEventHandler` | 离职信息变更 |
| `corehr.pathway.created_v2` | `CoreHrV2PathwayCreatedEventHandler` | 通道创建事件 |
| `corehr.pathway.deleted_v2` | `CoreHrV2PathwayDeletedEventHandler` | 通道删除事件 |
| `corehr.pathway.updated_v2` | `CoreHrV2PathwayUpdatedEventHandler` | 通道更新事件 |
| `corehr.position.created_v2` | `CoreHrV2PositionCreatedEventHandler` | 创建岗位事件 |
| `corehr.position.deleted_v2` | `CoreHrV2PositionDeletedEventHandler` | 删除岗位事件 |
| `corehr.position.updated_v2` | `CoreHrV2PositionUpdatedEventHandler` | 更新岗位事件 |
| `corehr.pre_hire.onboarding_task_changed_v2` | `CoreHrV2OnboardingTaskChangedEventHandler` | 入职流程状态变更 |
| `corehr.probation.updated_v2` | `CoreHrV2ProbationUpdatedEventHandler` | 试用期状态变更 |
| `corehr.process_comment_info.updated_v2` | `CoreHrV2ProcessCommentInfoUpdatedEventHandler` | 流程评论事件 |
| `corehr.process.approver.updated_v2` | `CoreHrV2ProcessApproverUpdatedEventHandler` | 审批任务状态变更 |
| `corehr.process.cc.updated_v2` | `CoreHrV2ProcessCcUpdatedEventHandler` | 抄送单据状态变更 |
| `corehr.process.node.updated_v2` | `CoreHrV2ProcessNodeUpdatedEventHandler` | 流程节点状态变更 |
| `corehr.process.status.update_v2` | `CoreHrV2ProcessStatusUpdatedEventHandler` | 流程实例状态变化 |
| `corehr.process.updated_v2` | `CoreHrV2ProcessUpdatedEventHandler` | 流程实例信息变更 |
| `corehr.signature_file.status_updated_v2` | `CoreHrV2SignatureFileStatusUpdatedEventHandler` | 电子签文件状态变更事件 |

### 🌈 公司圈事件（Moments）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `moments.comment.created_v1` | `MomentsCommentCreatedEventHandler` | 发布评论 |
| `moments.comment.deleted_v1` | `MomentsCommentDeletedEventHandler` | 删除评论 |
| `moments.post_statistics.updated_v1` | `MomentsPostStatisticsUpdatedEventHandler` | 帖子统计数据变更 |
| `moments.post.created_v1` | `MomentsPostCreatedEventHandler` | 发布帖子 |
| `moments.post.deleted_v1` | `MomentsPostDeletedEventHandler` | 删除帖子 |
| `moments.reaction.created_v1` | `MomentsReactionCreatedEventHandler` | 表情互动 |
| `moments.reaction.deleted_v1` | `MomentsReactionDeletedEventHandler` | 取消表情互动 |

### 🎓 eLearning 事件（ELearning）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `elearning.course_registration.created_v2` | `ELearningCourseRegistrationCreatedEventHandler` | 课程学习进度新增事件 |
| `elearning.course_registration.deleted_v2` | `ELearningCourseRegistrationDeletedEventHandler` | 课程学习进度删除事件 |
| `elearning.course_registration.updated_v2` | `ELearningCourseRegistrationUpdatedEventHandler` | 课程学习进度更新事件 |

### 🛡️ 安全合规事件（SecurityCompliance）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `security_and_compliance.device_apply_record.device_apply_event_v2` | `SecurityComplianceDeviceAppliedEventHandler` | 设备申报事件 |
| `security_and_compliance.device_record.device_change_event_v2` | `SecurityComplianceDeviceChangedEventHandler` | 设备信息变更事件 |

### 💵 Payroll 事件（Payroll）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `payroll.payment_activity.approved_v1` | `PayrollPaymentActivityApprovedEventHandler` | 发薪活动封存 |
| `payroll.payment_activity.status_changed_v1` | `PayrollPaymentActivityStatusChangedEventHandler` | 发薪活动变更事件 |

### 💰 薪酬事件（Compensation）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `compensation.archive.changed_v1` | `CompensationArchiveChangedEventHandler` | 薪资档案变更 |

### 🎴 卡片事件回调（Card）

| 事件类型 | 事件委托类 | 事件说明 |
| --- | --- | --- |
| `card.action.trigger` | `CardActionTriggerEventHandler` | 卡片请求回调（用户点击回调交互组件） |

> 卡片事件回调与常规事件订阅不同：它由**卡片交互**触发、经 **Webhook** 投递，且业务服务器**必须在 3 秒内响应**。
> 请求体模型见 `CardActionTriggerResult`（`operator` / `token` / `action` / `host` / `delivery_type` / `context`），
> 响应体模型见 `CardCallbackResponse`（`toast` + `card`，`card.data` 支持 `raw` 与 `template` 两种形态）。
> 详见[飞书卡片事件回调文档](https://open.feishu.cn/document/feishu-cards/card-callback-communication)。

### 📌 说明

- `meeting_room.meeting_room.status_changed_v1`（`RoomStatusChangedEventHandler`）按存量实现归属 **Calendar** 域，即会议室事件分布在 Calendar 与 MeetingRoom 两个域。
- `approval_instance` / `approval_task` / `approval_cc` / `leave_approval*` / `work_approval*` / `out_approval` / `shift_approval` 等为飞书旧版审批事件，官方已标注历史版本；SDK 保留以兼容存量用户代码，新接入推荐使用 v4 形态（如 `approval.task.status_changed_v4`）。
- `attendance.*` 与部分旧版审批事件不在官方「事件列表」页内，属其他文档章节来源。
- **敏感数据域**（Payroll 薪酬、Compensation 薪资档案、Moments 公司圈 UGC、SecurityCompliance 设备信息）的事件体含薪资与个人信息：处理器实现中**禁止**输出事件体原文，日志只能记结构化字段并脱敏截断（AGENTS.md D5 日志最小暴露）。

## 安装

### 通过 NuGet 安装

```bash
dotnet add package Mud.Feishu.EventCallback
```

### 支持的目标框架

- .NET Standard 2.0
- .NET 6.0
- .NET 8.0
- .NET 10.0

## 快速开始

### 1. 定义事件处理器

继承自动生成的事件处理器基类，并重写 `ProcessBusinessLogicAsync` 实现业务逻辑：

> **注意**：
> - 生成的处理器基类 `HandleAsync(EventData, CancellationToken)` 已声明为 `sealed`（内置事件反序列化与业务层幂等保护），**不可重写**；业务逻辑的唯一扩展点是 `ProcessBusinessLogicAsync`。
> - 生成的处理器构造函数签名为 `(IFeishuEventDeduplicator, ILogger, IAppKeyAccessor?)`，派生类必须显式声明构造函数并调用 `base(businessDeduplicator, logger)`（`IAppKeyAccessor` 为可选参数，用于多应用去重隔离）。
> - 事件模型显式声明了 `HeaderType = nameof(FeishuEventHeader)` 时，生成的处理器继承双泛型参数基类，应重写带 `header` 参数的 `ProcessBusinessLogicAsync` 重载；未声明时继承单泛型参数基类，应重写不带 `header` 参数的重载。

```csharp
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.EventCallback;
using Mud.Feishu.EventCallback.IM;

public class MyMessageHandler : MessageReceiveEventHandler
{
    public MyMessageHandler(IFeishuEventDeduplicator businessDeduplicator, ILogger<MyMessageHandler> logger)
        : base(businessDeduplicator, logger)
    {
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        MessageReceiveResult? eventEntity,
        FeishuEventHeader? header,
        CancellationToken cancellationToken = default)
    {
        if (eventEntity?.Message is null)
        {
            return;
        }

        _logger.LogInformation("收到消息：{MessageId}", eventEntity.Message.MessageId);

        if (eventEntity.Message.Content is not null)
        {
            _logger.LogInformation("消息内容：{Content}", eventEntity.Message.Content);
        }

        await Task.CompletedTask;
    }
}
```

### 2. 处理审批事件

```csharp
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.EventCallback;
using Mud.Feishu.EventCallback.Approval;

public class LeaveApprovalHandler : LeaveApprovalEventHandler
{
    public LeaveApprovalHandler(IFeishuEventDeduplicator businessDeduplicator, ILogger<LeaveApprovalHandler> logger)
        : base(businessDeduplicator, logger)
    {
    }

    // leave_approval 未声明 HeaderType，生成的处理器继承 IdempotentFeishuEventHandler<LeaveApprovalResult>，
    // 因此重写不带 header 参数的重载
    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        LeaveApprovalResult? eventEntity,
        CancellationToken cancellationToken = default)
    {
        if (eventEntity is null)
        {
            return;
        }

        _logger.LogInformation(
            "员工 {EmployeeId} 请假申请：{StartTime} 至 {EndTime}，时长 {Duration} 秒",
            eventEntity.EmployeeId,
            eventEntity.LeaveStartTime,
            eventEntity.LeaveEndTime,
            eventEntity.LeaveInterval);

        await Task.CompletedTask;
    }
}
```

### 3. 处理组织架构事件

```csharp
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.EventCallback;
using Mud.Feishu.EventCallback.Organization;

public class UserCreatedHandler : UserCreateEventHandler
{
    public UserCreatedHandler(IFeishuEventDeduplicator businessDeduplicator, ILogger<UserCreatedHandler> logger)
        : base(businessDeduplicator, logger)
    {
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        UserCreateResult? eventEntity,
        FeishuEventHeader? header,
        CancellationToken cancellationToken = default)
    {
        if (eventEntity is null)
        {
            return;
        }

        _logger.LogInformation(
            "新员工入职：{Name} ({UserId})",
            eventEntity.Name,
            eventEntity.UserId);

        await Task.CompletedTask;
    }
}
```

### 4. 处理卡片事件回调

卡片事件回调（`card.action.trigger`）由**卡片交互**触发、经 **Webhook** 投递，业务服务器**必须在 3 秒内响应**。
处理器内可读取交互数据，响应体用 `CardCallbackResponse` 构造后写回 HTTP 响应（不得返回 3xx 重定向）：

```csharp
using System.Text.Json;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.EventCallback;
using Mud.Feishu.EventCallback.Card;

public class MyCardActionHandler : CardActionTriggerEventHandler
{
    public MyCardActionHandler(IFeishuEventDeduplicator businessDeduplicator, ILogger<MyCardActionHandler> logger)
        : base(businessDeduplicator, logger)
    {
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        CardActionTriggerResult? eventEntity,
        FeishuEventHeader? header,
        CancellationToken cancellationToken = default)
    {
        var action = eventEntity?.Action;
        _logger.LogInformation("卡片交互：tag={Tag}, operator={OpenId}", action?.Tag, eventEntity?.Operator?.OpenId);

        // 构造响应：Toast 提示 + 更新卡片（raw 或 template）
        var response = new CardCallbackResponse
        {
            Toast = new CardCallbackToast
            {
                Type = "success",
                Content = "处理成功",
                I18n = new CardCallbackToastI18n { ZhCn = "处理成功", EnUs = "Success" }
            },
            Card = CardCallbackCard.Template("AAqi6xJ8rabcd", "1.0.0",
                new Dictionary<string, object?> { ["open_id"] = eventEntity?.Operator?.OpenId })
        };

        // 由宿主把响应写回 HTTP 响应体（AOT 安全入口）
        var json = FeishuJsonAot.Serialize(response, FeishuJsonDefaults.SerializerOptions);

        await Task.CompletedTask;
    }
}
```

## 数据模型结构

每个事件数据模型都实现了 `IEventResult` 接口，并包含：

- **完整的属性定义** - 所有字段均有强类型定义
- **JSON 序列化特性** - 使用 `[JsonPropertyName]` 标注字段映射
- **XML 文档注释** - 详细的中文注释说明
- **官方文档链接** - 通过 `<see href>` 链接到官方文档

示例：

```csharp
/// <summary>
/// 接收消息事件处理器
/// <para>机器人接收到用户发送的消息后触发此事件。</para>
/// <para>事件类型:im.message.receive_v1</para>
/// <para>使用时请继承：<see cref="MessageReceiveEventHandler"/></para>
/// <para>文档地址：<see href="https://open.feishu.cn/document/server-docs/im-v1/message/events/receive"/></para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.ReceiveMessage,
                      HandlerNamespace = Consts.HandlerNamespace,
                      InheritedFrom = Consts.InheritedFrom,
                      HeaderType = nameof(FeishuEventHeader))]
public class MessageReceiveResult : IEventResult
{
    [JsonPropertyName("sender")]
    public MessageSender? Sender { get; set; }

    [JsonPropertyName("message")]
    public MessageContent? Message { get; set; }
}
```

## 项目结构

```
Mud.Feishu.EventCallback/
├── Approval/                          # 审批事件
│   ├── ApprovalApprovalUpdatedEvent/  # 审批定义更新
│   ├── ApprovalCcEvent/              # 审批抄送
│   ├── ApprovalInstanceEvent/        # 审批实例
│   ├── ApprovalInstanceTripGroupUpdateEvent/ # 出差审批
│   ├── ApprovalInstanceRemedyGroupUpdateEvent/ # 补卡审批
│   ├── ApprovalTaskEvent/            # 审批任务
│   ├── LeaveApprovalEvent/           # 请假审批
│   ├── OutApprovalEvent/             # 外出审批
│   ├── ShiftApprovalEvent/           # 换班审批
│   └── WorkApprovalEvent/            # 加班审批
├── Organization/                      # 组织架构事件
│   ├── UserCreateEvent/              # 用户创建
│   ├── UserDeleteEvent/              # 用户删除
│   ├── UserUpdateEvent/              # 用户更新
│   ├── DepartmentCreatedEvent/       # 部门创建
│   ├── DepartmentDeletedEvent/       # 部门删除
│   ├── DepartmentUpdateEvent/        # 部门更新
│   ├── CustomAttrUpdateEvent/        # 自定义属性更新
│   ├── EmployeeTypeEnumCreatedEvent/ # 人员类型创建
│   ├── EmployeeTypeEnumDeleteEvent/  # 人员类型删除
│   ├── EmployeeTypeEnumUpdateEvent/  # 人员类型更新
│   ├── EmployeeTypeEnumActivedEvent/ # 人员类型启用
│   └── EmployeeTypeEnumDeActivedEvent/ # 人员类型禁用
├── IM/                               # 即时通讯事件
│   ├── MessageReceiveEvent/          # 消息接收
│   ├── MessageReadEvent/             # 消息已读
│   ├── MessageRecalledEvent/         # 消息撤回
│   ├── MessageReactionCreatedEvent/  # 表情回应创建
│   ├── MessageReactionDeletedEvent/  # 表情回应删除
│   ├── ChatUpdatedEvent/             # 群聊更新
│   ├── ChatDisbandedEvent/           # 群聊解散
│   ├── ChatMemberUserAddedEvent/     # 用户进群
│   ├── ChatMemberUserDeletedEvent/   # 用户出群
│   ├── ChatMemberUserWithdrawnEvent/ # 撤销拉用户进群
│   ├── ChatMemberBotAddedEvent/      # 机器人进群
│   └── ChatMemberBotDeletedEvent/    # 机器人退群
├── Task/                             # 任务事件
│   ├── TaskUpdatedResult.cs          # 任务更新
│   ├── TaskCommentUpdatedResult.cs   # 任务评论更新
│   └── TaskUpdateTenantResult.cs     # 租户任务更新
├── Drive/                            # 云盘事件
│   ├── DriveFileCreatedEvent/        # 文件创建
│   ├── DriveFileDeletedEvent/        # 文件删除
│   ├── DriveFileEditEvent/           # 文件编辑
│   ├── DriveFileReadEvent/           # 文件已读
│   ├── DriveFileTitleUpdatedEvent/   # 文件标题更新
│   ├── DriveFileTrashedEvent/        # 文件回收站
│   ├── DriveFilePermissionMemberAddedEvent/     # 协作者添加
│   ├── DriveFilePermissionMemberAppliedEvent/   # 协作者权限申请
│   ├── DriveFilePermissionMemberRemovedEvent/   # 协作者移除
│   └── DriveNoticeCommentAddEvent/   # 评论新增
├── Bitable/                          # 多维表格事件
│   ├── BitableFieldChangedEvent/     # 字段变更
│   └── BitableRecordChanged/         # 记录变更
├── Attendance/                       # 考勤事件
│   ├── AttendanceUserFlowCreatedResult.cs   # 考勤流水创建
│   └── AttendanceUserTaskUpdatedResult.cs   # 考勤任务更新
├── Hire/                             # 招聘事件
│   ├── ApplicationDeletedEvent/                      # 投递删除
│   ├── ApplicationStageChangedEvent/                # 投递阶段变更
│   ├── OfferStatusChangedEvent/                     # Offer 状态变更
│   ├── EhrImportTaskImportedEvent/                  # 导入 e-HR
│   ├── EhrImportTaskForInternshipOfferImportedEvent/# 实习 Offer 导入 e-HR
│   ├── TalentTagSubscriptionEvent/                  # 人才标签订阅
│   ├── TalentDeletedEvent/                          # 人才删除
│   ├── EcoAccountCreatedEvent/                      # 账号创建
│   ├── EcoBackgroundCheckCreatedEvent/              # 背调创建
│   ├── EcoBackgroundCheckCanceledEvent/             # 背调取消
│   ├── EcoExamCreatedEvent/                         # 创建笔试
│   └── ReferralAccountAssetsUpdateEvent/            # 内推账户余额变更
├── Performance/                      # 绩效事件
│   ├── StageTaskOpenResultEvent/     # 绩效结果开通
│   └── ReviewDataChangedEvent/       # 绩效数据变更
├── Acs/                              # 智能门禁事件
│   ├── UserUpdatedEvent/             # 用户信息变更
│   └── AccessRecordCreatedEvent/     # 通行记录创建
├── Helpdesk/                         # 服务台事件
│   ├── Common/                       # 工单等共用模型
│   ├── TicketCreatedEvent/           # 工单创建
│   ├── TicketUpdatedEvent/           # 工单更新
│   ├── TicketMessageCreatedEvent/    # 工单消息创建
│   └── NotificationApproveEvent/     # 推送审批通知
├── Card/                             # 卡片事件回调（请求回调）
│   ├── CardActionTriggerEvent/       # 卡片交互回调（operator/action/context 等）
│   ├── CardCallbackResponse.cs       # 回调响应体（toast + card）
│   ├── CardCallbackToast.cs          # Toast 弹窗
│   ├── CardCallbackToastI18n.cs      # Toast 多语言
│   ├── CardCallbackCard.cs           # 响应卡片（raw / template）
│   └── CardCallbackTemplateData.cs   # 模板卡片数据
└── GlobalUsings.cs                   # 全局引用
```

## 依赖项

- `Mud.Feishu.Abstractions` - 核心抽象和基础类型
- `Mud.HttpUtils.Generator` - 源代码生成器（用于自动生成事件处理器）

## 相关项目

- **Mud.Feishu** - 核心 HTTP API 客户端
- **Mud.Feishu.DataModels** - HTTP API 强类型数据模型
- **Mud.Feishu.Abstractions** - 事件处理抽象
- **Mud.Feishu.Authentication** - 用户认证中间件
- **Mud.Feishu.Webhook** - Webhook 事件处理
- **Mud.Feishu.WebSocket** - WebSocket 实时事件
- **Mud.Feishu.Redis** - Redis 分布式去重
- **Mud.Feishu.OpenTelemetry** - OpenTelemetry 可观测性扩展

## 文档与资源

- [飞书开放平台文档](https://open.feishu.cn/document/home/introduction-to-feishu-open-platform/)
- [事件订阅概述](https://open.feishu.cn/document/ukTMukTMukTM/uUTNz4SN1MjL1UzM)
- [MudFeishu GitHub 仓库](https://github.com/mudtools/MudFeishu)

## 许可证

本项目采用 MIT 许可证。详见 [LICENSE-MIT](../LICENSE-MIT) 文件。

## 版权声明

版权所有 © Mud Studio 2026。保留所有权利。

Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。

本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。

不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
