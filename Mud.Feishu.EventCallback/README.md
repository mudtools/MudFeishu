# Mud.Feishu.EventCallback

飞书服务端 SDK 的 .NET 适配版，提供事件与回调功能强数据模型封装。

## 项目简介

`Mud.Feishu.EventCallback` 是 MudFeishu SDK 的核心组件之一，专门用于处理飞书平台的事件回调。本项目提供了完整的强类型数据模型，帮助开发者轻松处理飞书各种事件推送。

## 特性

- ✅ **强类型数据模型** - 所有事件数据均有完整的类型定义和 XML 文档注释
- ✅ **自动生成事件处理器** - 使用源代码生成器自动生成事件处理器基类
- ✅ **多框架支持** - 支持 .NET Standard 2.0、.NET 6.0、.NET 8.0、.NET 10.0
- ✅ **完整的事件覆盖** - 覆盖飞书主要业务场景的事件回调
- ✅ **官方文档链接** - 每个事件模型都包含官方文档链接

## 支持的事件类型

### 👥 组织架构事件（Organization）

| 事件类型                                    | 事件委托类                              | 事件说明         |
| ------------------------------------------- | --------------------------------------- | ---------------- |
| `contact.user.created_v3`                   | `UserCreateEventHandler`                | 员工入职事件     |
| `contact.user.updated_v3`                   | `UserUpdateEventHandler`                | 员工信息更新事件 |
| `contact.user.deleted_v3`                   | `UserDeleteEventHandler`                | 员工离职事件     |
| `contact.department.created_v3`             | `DepartmentCreatedEventHandler`         | 部门创建事件     |
| `contact.department.updated_v3`             | `DepartmentUpdateEventHandler`          | 部门更新事件     |
| `contact.department.deleted_v3`             | `DepartmentDeleteEventHandler`          | 部门删除事件     |
| `contact.custom_attr_event.updated_v3`      | `CustomAttrUpdateEventHandler`          | 成员字段变更事件 |
| `contact.employee_type_enum.created_v3`     | `EmployeeTypeEnumCreatedEventHandler`   | 人员类型创建事件 |
| `contact.employee_type_enum.updated_v3`     | `EmployeeTypeEnumUpdateEventHandler`    | 人员类型更新事件 |
| `contact.employee_type_enum.deleted_v3`     | `EmployeeTypeEnumDeleteEventHandler`    | 人员类型删除事件 |
| `contact.employee_type_enum.actived_v3`     | `EmployeeTypeEnumActivedEventHandler`   | 人员类型启用事件 |
| `contact.employee_type_enum.deactivated_v3` | `EmployeeTypeEnumDeActivedEventHandler` | 人员类型禁用事件 |

### 💬 即时通讯事件（IM）

| 事件类型                           | 事件委托类                            | 事件说明             |
| ---------------------------------- | ------------------------------------- | -------------------- |
| `im.message.receive_v1`            | `MessageReceiveEventHandler`          | 接收消息事件         |
| `im.message.recalled_v1`           | `MessageRecalledEventHandler`         | 消息撤回事件         |
| `im.message.message_read_v1`       | `MessageReadEventHandler`             | 消息已读事件         |
| `im.message.reaction.created_v1`   | `MessageReactionCreatedEventHandler`  | 新增消息表情回复事件 |
| `im.message.reaction.deleted_v1`   | `MessageReactionDeletedEventHandler`  | 删除消息表情回复事件 |
| `im.chat.disbanded_v1`             | `ChatDisbandedEventHandler`           | 群解散事件           |
| `im.chat.updated_v1`               | `ChatUpdatedEventHandler`             | 群配置修改事件       |
| `im.chat.member.user.added_v1`     | `ChatMemberUserAddedEventHandler`     | 用户进群事件         |
| `im.chat.member.user.deleted_v1`   | `ChatMemberUserDeletedEventHandler`   | 用户出群事件         |
| `im.chat.member.user.withdrawn_v1` | `ChatMemberUserWithdrawnEventHandler` | 撤销拉用户进群事件   |
| `im.chat.member.bot.added_v1`      | `ChatMemberBotAddedEventHandler`      | 机器人进群事件       |
| `im.chat.member.bot.deleted_v1`    | `ChatMemberBotDeletedEventHandler`    | 机器人被移出群事件   |

### 📋 审批事件（Approval）

| 事件类型                                   | 事件委托类                                      | 事件说明               |
| ------------------------------------------ | ----------------------------------------------- | ---------------------- |
| `approval.approval.updated_v4`             | `ApprovalApprovalUpdatedEventHandler`           | 审批定义更新事件       |
| `approval_instance`                        | `ApprovalInstanceEventHandler`                  | 审批实例状态变更事件   |
| `approval_task`                            | `ApprovalTaskEventHandler`                      | 审批任务状态变更事件   |
| `approval_cc`                              | `ApprovalCcEventHandler`                        | 审批抄送状态变更事件   |
| `approval.instance.trip_group_update_v4`   | `ApprovalInstanceTripGroupUpdateEventHandler`   | 出差审批事件           |
| `approval.instance.remedy_group_update_v4` | `ApprovalInstanceRemedyGroupUpdateEventHandler` | 补卡审批事件           |
| `leave_approval`                           | `LeaveApprovalEventHandler`                     | 请假审批事件           |
| `leave_approvalV2`                         | `LeaveApprovalV2EventHandler`                   | 请假审批事件 V2        |
| `leave_approval_revert`                    | `LeaveApprovalRevertEventHandler`               | 请假审批通过并撤销事件 |
| `work_approval`                            | `WorkApprovalEventHandler`                      | 加班审批事件           |
| `work_approval_revert`                     | `WorkApprovalRevertEventHandler`                | 加班审批通过并撤销事件 |
| `out_approval`                             | `OutApprovalEventHandler`                       | 外出审批事件           |
| `shift_approval`                           | `ShiftApprovalEventHandler`                     | 换班审批事件           |

### ✅ 任务事件（Task）

| 事件类型                       | 事件委托类                       | 事件说明                     |
| ------------------------------ | -------------------------------- | ---------------------------- |
| `task.task.updated_v1`         | `TaskUpdatedEventHandler`        | 任务信息变更（应用维度）事件 |
| `task.task.update_tenant_v1`   | `TaskUpdateTenantEventHandler`   | 任务信息变更（租户维度）事件 |
| `task.task.comment.updated_v1` | `TaskCommentUpdatedEventHandler` | 任务评论信息变更事件         |

### ☁️ 云盘事件（Drive）

| 事件类型                                  | 事件委托类                                     | 事件说明               |
| ----------------------------------------- | ---------------------------------------------- | ---------------------- |
| `drive.file.created_in_folder_v1`         | `DriveFileCreatedEventHandler`                 | 文件夹下文件创建事件   |
| `drive.file.title_updated_v1`             | `DriveFileTitleUpdatedEventHandler`            | 文件标题更新事件       |
| `drive.file.read_v1`                      | `DriveFileReadEventHandler`                    | 文件已读事件           |
| `drive.file.edit_v1`                      | `DriveFileEditEventHandler`                    | 文件编辑事件           |
| `drive.file.permission_member_applied_v1` | `DriveFilePermissionMemberAppliedEventHandler` | 文件协作者权限申请事件 |
| `drive.file.permission_member_added_v1`   | `DriveFilePermissionMemberAddedEventHandler`   | 文件协作者添加事件     |
| `drive.file.permission_member_removed_v1` | `DriveFilePermissionMemberRemovedEventHandler` | 文档协作者移除事件     |
| `drive.file.trashed_v1`                   | `DriveFileTrashedEventHandler`                 | 文件删除到回收站事件   |
| `drive.file.deleted_v1`                   | `DriveFileDeletedEventHandler`                 | 文件删除事件           |
| `drive.notice.comment_add_v1`             | `DriveNoticeCommentAddEventHandler`            | 文件评论新增事件       |

### 📋 多维表格事件（Bitable）

| 事件类型                               | 事件委托类                         | 事件说明             |
| -------------------------------------- | ---------------------------------- | -------------------- |
| `drive.file.bitable_field_changed_v1`  | `BitableFieldChangedEventHandler`  | 多维表格字段变更事件 |
| `drive.file.bitable_record_changed_v1` | `BitableRecordChangedEventHandler` | 多维表格记录变更事件 |

### ⏰ 考勤事件（Attendance）

| 事件类型                          | 事件委托类                              | 事件说明             |
| --------------------------------- | --------------------------------------- | -------------------- |
| `attendance.user_flow.created_v1` | `AttendanceUserFlowCreatedEventHandler` | 考勤用户打卡流水事件 |
| `attendance.user_task.updated_v1` | `AttendanceUserTaskUpdatedEventHandler` | 考勤用户任务更新事件 |

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

继承自动生成的事件处理器基类，实现业务逻辑：

```csharp
using Mud.Feishu.EventCallback.IM;

public class MyMessageHandler : MessageReceiveEventHandler
{
    private readonly ILogger<MyMessageHandler> _logger;

    public MyMessageHandler(ILogger<MyMessageHandler> logger)
    {
        _logger = logger;
    }

    public override async Task HandleAsync(MessageReceiveResult result, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("收到消息：{MessageId}", result.Message?.MessageId);

        if (result.Message?.Content != null)
        {
            var content = result.Message.Content;
            _logger.LogInformation("消息内容：{Content}", content);
        }

        await Task.CompletedTask;
    }
}
```

### 2. 处理审批事件

```csharp
using Mud.Feishu.EventCallback.Approval;

public class LeaveApprovalHandler : LeaveApprovalEventHandler
{
    private readonly ILogger<LeaveApprovalHandler> _logger;

    public LeaveApprovalHandler(ILogger<LeaveApprovalHandler> logger)
    {
        _logger = logger;
    }

    public override async Task HandleAsync(LeaveApprovalResult result, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "员工 {EmployeeId} 请假申请：{StartTime} 至 {EndTime}，时长 {Duration} 秒",
            result.EmployeeId,
            result.LeaveStartTime,
            result.LeaveEndTime,
            result.LeaveInterval
        );

        await Task.CompletedTask;
    }
}
```

### 3. 处理组织架构事件

```csharp
using Mud.Feishu.EventCallback.Organization;

public class UserCreatedHandler : UserCreateEventHandler
{
    private readonly ILogger<UserCreatedHandler> _logger;

    public UserCreatedHandler(ILogger<UserCreatedHandler> logger)
    {
        _logger = logger;
    }

    public override async Task HandleAsync(UserCreateResult result, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "新员工入职：{Name} ({UserId})",
            result.Name,
            result.UserId
        );

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
                      InheritedFrom = Consts.InheritedFrom)]
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
└── GlobalUsings.cs                   # 全局引用
```

## 依赖项

- `Mud.Feishu.Abstractions` - 核心抽象和基础类型
- `Mud.HttpUtils.Generator` - 源代码生成器（用于自动生成事件处理器）

## 相关项目

- **Mud.Feishu** - 核心 HTTP API 客户端
- **Mud.Feishu.Abstractions** - 事件处理抽象
- **Mud.Feishu.Webhook** - Webhook 事件处理
- **Mud.Feishu.WebSocket** - WebSocket 实时事件
- **Mud.Feishu.Redis** - Redis 分布式去重

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
