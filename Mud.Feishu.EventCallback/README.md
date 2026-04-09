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

### 📋 审批事件（Approval）

| 事件类型                                | 说明         | 数据模型                                  |
| --------------------------------------- | ------------ | ----------------------------------------- |
| `leave_approval`                        | 请假审批     | `LeaveApprovalResult`                     |
| `leave_approval_revert`                 | 请假撤销     | `LeaveApprovalRevertResult`               |
| `leave_approval_v2`                     | 请假审批 V2  | `LeaveApprovalV2Result`                   |
| `work_approval`                         | 加班审批     | `WorkApprovalResult`                      |
| `work_approval_revert`                  | 加班撤销     | `WorkApprovalRevertResult`                |
| `out_approval`                          | 外出审批     | `OutApprovalResult`                       |
| `shift_approval`                        | 换班审批     | `ShiftApprovalResult`                     |
| `approval_instance`                     | 审批实例     | `ApprovalInstanceResult`                  |
| `approval_task`                         | 审批任务     | `ApprovalResult`                          |
| `approval_cc`                           | 审批抄送     | `ApprovalCcResult`                        |
| `approval_approval_updated`             | 审批定义更新 | `ApprovalApprovalUpdatedResult`           |
| `approval_instance_trip_group_update`   | 出差审批更新 | `ApprovalInstanceTripGroupUpdateResult`   |
| `approval_instance_remedy_group_update` | 补卡审批更新 | `ApprovalInstanceRemedyGroupUpdateResult` |

### 👥 组织架构事件（Organization）

| 事件类型                                    | 说明           | 数据模型                          |
| ------------------------------------------- | -------------- | --------------------------------- |
| `contact.user.created_v3`                   | 员工入职       | `UserCreateResult`                |
| `contact.user.deleted_v3`                   | 员工离职       | `UserDeleteResult`                |
| `contact.user.updated_v3`                   | 员工信息更新   | `UserUpdateResult`                |
| `contact.department.created_v3`             | 部门创建       | `DepartmentCreatedEventResult`    |
| `contact.department.deleted_v3`             | 部门删除       | `DepartmentDeleteResult`          |
| `contact.department.updated_v3`             | 部门更新       | `DepartmentDeleteResult`          |
| `contact.custom_attr.updated_v3`            | 自定义属性更新 | `CustomAttrUpdateResult`          |
| `contact.employee_type_enum.created_v3`     | 人员类型创建   | `EmployeeTypeEnumCreatedResult`   |
| `contact.employee_type_enum.deleted_v3`     | 人员类型删除   | `EmployeeTypeEnumDeleteResult`    |
| `contact.employee_type_enum.updated_v3`     | 人员类型更新   | `EmployeeTypeEnumUpdateResult`    |
| `contact.employee_type_enum.activated_v3`   | 人员类型激活   | `EmployeeTypeEnumActivedResult`   |
| `contact.employee_type_enum.deactivated_v3` | 人员类型停用   | `EmployeeTypeEnumDeActivedResult` |

### 💬 即时通讯事件（IM）

| 事件类型                           | 说明         | 数据模型                       |
| ---------------------------------- | ------------ | ------------------------------ |
| `im.message.receive_v1`            | 接收消息     | `MessageReceiveResult`         |
| `im.message.read_v1`               | 消息已读     | `MessageReadResult`            |
| `im.message.recalled_v1`           | 消息撤回     | `MessageRecalledResult`        |
| `im.message_reaction.created_v1`   | 表情回应创建 | `MessageReactionCreatedResult` |
| `im.message_reaction.deleted_v1`   | 表情回应删除 | `MessageReactionDeletedResult` |
| `im.chat.updated_v1`               | 群聊更新     | `ChatUpdatedResult`            |
| `im.chat.disbanded_v1`             | 群聊解散     | `ChatDisbandedResult`          |
| `im.chat.member.user.added_v1`     | 用户进群     | `ChatMemberUserAddedResult`    |
| `im.chat.member.user.deleted_v1`   | 用户退群     | `ChatMemberUserAddedResult`    |
| `im.chat.member.user.withdrawn_v1` | 用户主动退群 | `ChatMemberUserAddedResult`    |
| `im.chat.member.bot.added_v1`      | 机器人进群   | `ChatMemberBotAddedResult`     |
| `im.chat.member.bot.deleted_v1`    | 机器人退群   | `ChatMemberBotDeletedResult`   |

### ✅ 任务事件（Task）

| 事件类型               | 说明         | 数据模型                   |
| ---------------------- | ------------ | -------------------------- |
| `task.updated`         | 任务更新     | `TaskUpdatedResult`        |
| `task.comment.updated` | 任务评论更新 | `TaskCommentUpdatedResult` |
| `task.tenant.updated`  | 租户任务更新 | `TaskUpdateTenantResult`   |

### ⏰ 考勤事件（Attendance）

| 事件类型                          | 说明         | 数据模型                          |
| --------------------------------- | ------------ | --------------------------------- |
| `attendance.user_flow.created_v1` | 考勤流水创建 | `AttendanceUserFlowCreatedResult` |
| `attendance.user_task.updated_v1` | 考勤任务更新 | `AttendanceUserTaskUpdatedResult` |

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
using Mud.Feishu.EventCallback.DataModels.IM;

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
using Mud.Feishu.EventCallback.DataModels.Approval;

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
using Mud.Feishu.EventCallback.DataModels.Organization;

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
│   ├── LeaveApprovalEvent/           # 请假审批
│   ├── WorkApprovalEvent/            # 加班审批
│   ├── OutApprovalEvent/             # 外出审批
│   ├── ShiftApprovalEvent/           # 换班审批
│   ├── ApprovalInstanceEvent/        # 审批实例
│   ├── ApprovalTaskEvent/            # 审批任务
│   ├── ApprovalCcEvent/              # 审批抄送
│   └── ...
├── Organization/                      # 组织架构事件
│   ├── UserCreateEvent/              # 用户创建
│   ├── UserDeleteEvent/              # 用户删除
│   ├── UserUpdateEvent/              # 用户更新
│   ├── DepartmentCreatedEvent/       # 部门创建
│   ├── DepartmentDeletedEvent/       # 部门删除
│   ├── DepartmentUpdateEvent/        # 部门更新
│   └── ...
├── IM/                               # 即时通讯事件
│   ├── MessageReceiveEvent/          # 消息接收
│   ├── MessageReadEvent/             # 消息已读
│   ├── MessageRecalledEvent/         # 消息撤回
│   ├── MessageReactionCreatedEvent/  # 表情回应创建
│   ├── MessageReactionDeletedEvent/  # 表情回应删除
│   ├── ChatUpdatedEvent/             # 群聊更新
│   ├── ChatDisbandedEvent/           # 群聊解散
│   └── ...
├── Task/                             # 任务事件
│   ├── TaskUpdatedResult.cs          # 任务更新
│   ├── TaskCommentUpdatedResult.cs   # 任务评论更新
│   └── TaskUpdateTenantResult.cs     # 租户任务更新
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
