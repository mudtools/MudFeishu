# Mud.Feishu

[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.svg)](https://www.nuget.org/packages/Mud.Feishu/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Mud.Feishu 是飞书服务端 SDK 的 .NET 适配版，提供完整的 HTTP API 客户端能力，支持 .NET Standard 2.0、.NET 6.0、.NET 8.0 和 .NET 10.0 平台。通过类型安全的 API 封装，让开发者能够在 .NET 应用程序中便捷、高效地集成飞书服务端功能。

## 特性

- **类型安全** - 强类型接口定义，编译时类型检查
- **模块化设计** - 按需注册 API 模块，减少不必要的依赖
- **自动令牌管理** - 智能缓存和刷新，无需手动维护
- **智能重试机制** - 内置重试策略，提高调用成功率
- **多应用支持** - 统一管理多个飞书应用
- **完整 API 覆盖** - 支持组织架构、消息、群聊、审批、任务、云文档、画板、电子表格、多维表格、日历日程、视频会议、邮箱、AI、搜索、服务台等全部飞书核心功能

## 安装

```bash
dotnet add package Mud.Feishu
```

## 快速开始

### 1. 配置文件 (appsettings.json)

```json
{
  "FeishuApps": [
    {
      "AppKey": "default",
      "AppId": "your_feishu_app_id",
      "AppSecret": "your_feishu_app_secret",
      "BaseUrl": "https://open.feishu.cn",
      "TimeOut": 30,
      "RetryCount": 3,
      "RetryDelayMs": 1000,
      "EnableLogging": true
    }
  ]
}
```

### 2. 服务注册 (Program.cs)

```csharp
using Mud.Feishu;
using Mud.Feishu.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 注册飞书应用配置
builder.Services.AddFeishuApp(builder.Configuration);

// 方式一：懒人模式 - 注册所有 API 服务
builder.Services.CreateFeishuServicesBuilder()
    .AddAllApis()
    .Build();

// 方式二：建造者模式 - 按需注册
builder.Services.CreateFeishuServicesBuilder()
    .AddOrganizationApi()
    .AddMessageApi()
    .AddChatGroupApi()
    .AddApprovalApi()
    .AddTaskApi()
    .AddCardApi()
    .AddAttendanceApi()
    .AddDriveApi()
    .AddWikiApi()
    .AddDocxApi()
    .AddSpreadsheetsApi()
    .AddBiTableApi()
    .AddCalendarApi()
    .AddVideoConferencingApi()
    .AddMailApi()
    .AddAIApi()
    .AddSearchApi()
    .AddHelpDeskApi()
    .Build();

// 方式三：模块枚举模式
builder.Services.AddFeishuServices(
    FeishuModule.Organization,
    FeishuModule.Message,
    FeishuModule.ChatGroup,
    FeishuModule.Spreadsheets,
    FeishuModule.Bitable,
    FeishuModule.Calendar,
    FeishuModule.VideoConferencing,
    FeishuModule.Mail,
    FeishuModule.AI,
    FeishuModule.Search,
    FeishuModule.HelpDesk
);

var app = builder.Build();
app.Run();
```

### 3. 使用 API 服务

```csharp
using Mud.Feishu;

public class UserService
{
    private readonly IFeishuV3User_Tenant _userApi;

    public UserService(IFeishuV3User_Tenant userApi)
    {
        _userApi = userApi;
    }

    public async Task<FeishuApiResult<UserInfoResult>?> GetUserAsync(string userId)
    {
        return await _userApi.GetUserInfoByIdAsync(userId);
    }

    public async Task<FeishuApiResult<CreateOrUpdateUserResult>?> CreateUserAsync(CreateUserRequest request)
    {
        return await _userApi.CreateUserAsync(request);
    }
}
```

## API 模块

### 组织管理 (Organization)

#### 用户管理

**继承关系**：`IFeishuTenantV3User` / `IFeishuUserV3User` → `IFeishuV3User`

| 接口类型         | 接口名称                  | API 函数                       | 说明              |
| ------------ | --------------------- | ---------------------------- | --------------- |
| **父类接口（公共）** | `IFeishuV3User`       | `UpdateUserAsync`            | 更新用户信息          |
| <br />       | <br />                | `GetUserInfoByIdAsync`       | 获取单个用户信息        |
| <br />       | <br />                | `GetUserByIdsAsync`          | 批量获取用户信息        |
| <br />       | <br />                | `GetUserByDepartmentIdAsync` | 获取部门直属用户列表      |
| **租户令牌接口**   | `IFeishuTenantV3User` | `CreateUserAsync`            | 创建用户（员工入职）      |
| <br />       | 继承父类所有方法              | `UpdateUserIdAsync`          | 更新用户 ID         |
| <br />       | <br />                | `GetBatchUsersAsync`         | 通过手机号/邮箱获取用户 ID |
| <br />       | <br />                | `GetUsersByKeywordAsync`     | 通过关键词搜索用户       |
| <br />       | <br />                | `DeleteUserByIdAsync`        | 删除用户（员工离职）      |
| <br />       | <br />                | `ResurrectUserByIdAsync`     | 恢复已删除用户         |
| <br />       | <br />                | `LogoutAsync`                | 退出用户登录态         |
| <br />       | <br />                | `GetJsTicketAsync`           | 获取 JSAPI 临时调用凭证 |
| **用户令牌接口**   | `IFeishuUserV3User`   | `GetUsersByKeywordAsync`     | 通过关键词搜索用户       |
| <br />       | 继承父类所有方法              | `GetUserInfoAsync`           | 获取当前用户信息        |

#### 部门管理

**继承关系**：`IFeishuTenantV3Departments` / `IFeishuUserV3Departments` → `IFeishuV3Departments`

| 接口类型         | 接口名称                         | API 函数                          | 说明         |
| ------------ | ---------------------------- | ------------------------------- | ---------- |
| **父类接口（公共）** | `IFeishuV3Departments`       | `GetDepartmentInfoByIdAsync`    | 获取单个部门信息   |
| <br />       | <br />                       | `GetDepartmentsByIdsAsync`      | 批量获取部门信息   |
| <br />       | <br />                       | `GetDepartmentsByParentIdAsync` | 获取子部门列表    |
| <br />       | <br />                       | `GetParentDepartmentsByIdAsync` | 获取父部门信息    |
| **租户令牌接口**   | `IFeishuTenantV3Departments` | `CreateDepartmentAsync`         | 创建部门       |
| <br />       | 继承父类所有方法                     | `UpdatePartDepartmentAsync`     | 更新部门部分信息   |
| <br />       | <br />                       | `UpdateDepartmentAsync`         | 更新部门全部信息   |
| <br />       | <br />                       | `UpdateDepartmentIdAsync`       | 更新部门自定义 ID |
| <br />       | <br />                       | `UnbindDepartmentChatAsync`     | 解绑部门群      |
| <br />       | <br />                       | `DeleteDepartmentByIdAsync`     | 删除部门       |
| **用户令牌接口**   | `IFeishuUserV3Departments`   | -                               | 仅继承父类方法    |

#### 其他组织管理接口

| 接口名称                                                          | 继承关系 | 说明          |
| ------------------------------------------------------------- | ---- | ----------- |
| `IFeishuTenantV3JobLevel`                                     | -    | 职级管理（仅租户令牌） |
| `IFeishuTenantV3JobTitle` → `IFeishuV3JobTitle`               | 继承父类 | 职务管理        |
| `IFeishuUserV3JobTitle` → `IFeishuV3JobTitle`                 | 继承父类 | 职务管理        |
| `IFeishuTenantV3JobFamilies`                                  | -    | 职务族管理       |
| `IFeishuTenantV3UserGroup` → `IFeishuV3UserGroup`             | 继承父类 | 用户组管理       |
| `IFeishuTenantV3UserGroupMember` → `IFeishuV3UserGroupMember` | 继承父类 | 用户组成员管理     |
| `IFeishuTenantV3Role` → `IFeishuV3Role`                       | 继承父类 | 角色管理        |
| `IFeishuTenantV3RoleMember` → `IFeishuV3RoleMember`           | 继承父类 | 角色成员管理      |
| `IFeishuTenantV3Unit` → `IFeishuV3Unit`                       | 继承父类 | 单位管理        |
| `IFeishuTenantV3WorkCity` → `IFeishuV3WorkCity`               | 继承父类 | 工作城市管理      |
| `IFeishuUserV3WorkCity` → `IFeishuV3WorkCity`                 | 继承父类 | 工作城市管理      |
| `IFeishuTenantV3EmployeeType`                                 | -    | 人员类型管理      |

***

### 消息服务 (Messages)

**继承关系**：`IFeishuTenantV1Message` / `IFeishuUserV1Message` → `IFeishuV1Message`

| 接口类型         | 接口名称                           | API 函数                             | 说明             |
| ------------ | ------------------------------ | ---------------------------------- | -------------- |
| **父类接口（公共）** | `IFeishuV1Message`             | `RevokeMessageAsync`               | 撤回消息           |
| <br />       | <br />                         | `AddMessageReactionsAsync`         | 添加表情回复         |
| <br />       | <br />                         | `GetMessageReactionsPageListAsync` | 获取表情回复列表       |
| <br />       | <br />                         | `DeleteMessageReactionsAsync`      | 删除表情回复         |
| <br />       | <br />                         | `PinMessageAsync`                  | Pin 消息         |
| <br />       | <br />                         | `DeletePinMessageAsync`            | 移除 Pin         |
| <br />       | <br />                         | `GetPinMessagePageListAsync`       | 获取 Pin 消息列表    |
| **租户令牌接口**   | `IFeishuTenantV1Message`       | `SendMessageAsync`                 | 发送消息           |
| <br />       | 继承父类所有方法                       | `ReplyMessageAsync`                | 回复消息           |
| <br />       | <br />                         | `EditMessageAsync`                 | 编辑消息           |
| <br />       | <br />                         | `ReceiveMessageAsync`              | 转发消息           |
| <br />       | <br />                         | `MergeReceiveMessageAsync`         | 合并转发消息         |
| <br />       | <br />                         | `ReceiveThreadsAsync`              | 转发话题           |
| <br />       | <br />                         | `CreateMessageFollowUpAsync`       | 添加跟随气泡         |
| <br />       | <br />                         | `GetMessageReadUsesAsync`          | 查询消息已读状态       |
| <br />       | <br />                         | `GetHistoryMessageAsync`           | 获取历史消息         |
| <br />       | <br />                         | `GetMessageFile`                   | 获取消息内资源文件（小文件） |
| <br />       | <br />                         | `GetMessageLargeFile`              | 获取消息内资源文件（大文件） |
| <br />       | <br />                         | `GetContentListByMessageIdAsync`   | 获取消息内容         |
| <br />       | <br />                         | `UploadFileAsync`                  | 上传文件           |
| <br />       | <br />                         | `UploadImageAsync`                 | 上传图片           |
| <br />       | <br />                         | `DownFileAsync`                    | 下载文件（小文件）      |
| <br />       | <br />                         | `DownLargeFileAsync`               | 下载文件（大文件）      |
| <br />       | <br />                         | `DownImageAsync`                   | 下载图片（小文件）      |
| <br />       | <br />                         | `DownLargeImageAsync`              | 下载图片（大文件）      |
| <br />       | <br />                         | `MessageUrgentAppAsync`            | 应用内消息加急        |
| <br />       | <br />                         | `MessageUrgentSMSAsync`            | 短信消息加急         |
| <br />       | <br />                         | `MessageUrgentPhoneAsync`          | 电话消息加急         |
| <br />       | <br />                         | `UpdateUrlPreviewAsync`            | 更新 URL 预览      |
| **用户令牌接口**   | `IFeishuUserV1Message`         | -                                  | 仅继承父类方法        |
| **批量消息**     | `IFeishuV1BatchMessage_Tenant` | 批量消息相关                             | 批量消息管理         |

***

### 群聊管理 (ChatGroup)

**继承关系**：`IFeishuTenantV1ChatGroup` / `IFeishuUserV1ChatGroup` → `IFeishuV1ChatGroup`

| 接口类型         | 接口名称                       | API 函数                                   | 说明        |
| ------------ | -------------------------- | ---------------------------------------- | --------- |
| **父类接口（公共）** | `IFeishuV1ChatGroup`       | `UpdateChatGroupByIdAsync`               | 更新群信息     |
| <br />       | <br />                     | `DeleteChatGroupAsync`                   | 解散群聊      |
| <br />       | <br />                     | `GetChatGroupInoByIdAsync`               | 获取群信息     |
| <br />       | <br />                     | `GetChatGroupPageListAsync`              | 获取群列表     |
| <br />       | <br />                     | `GetChatGroupPageListByKeywordAsync`     | 搜索群列表     |
| <br />       | <br />                     | `UpdateChatModerationAsync`              | 更新群发言权限   |
| <br />       | <br />                     | `GetChatGroupModeratorPageListByIdAsync` | 获取群发言权限信息 |
| <br />       | <br />                     | `PutChatGroupTopNoticeAsync`             | 置顶消息/公告   |
| <br />       | <br />                     | `DeleteChatGroupTopNoticeAsync`          | 取消置顶      |
| <br />       | <br />                     | `GetChatGroupShareLinkByIdAsync`         | 获取群分享链接   |
| **租户令牌接口**   | `IFeishuTenantV1ChatGroup` | `CreateChatGroupAsync`                   | 创建群聊      |
| <br />       | 继承父类所有方法                   | <br />                                   | <br />    |

**其他群聊相关接口**：

| 接口名称                                                                      | 继承关系 | 说明           |
| ------------------------------------------------------------------------- | ---- | ------------ |
| `IFeishuTenantV1ChatGroupMember` → `IFeishuV1ChatGroupMember`             | 继承父类 | 群成员管理（租户令牌）  |
| `IFeishuUserV1ChatGroupMember` → `IFeishuV1ChatGroupMember`               | 继承父类 | 群成员管理（用户令牌）  |
| `IFeishuTenantV1ChatGroupAnnouncement` → `IFeishuV1ChatGroupAnnouncement` | 继承父类 | 群公告管理（租户令牌）  |
| `IFeishuUserV1ChatGroupAnnouncement` → `IFeishuV1ChatGroupAnnouncement`   | 继承父类 | 群公告管理（用户令牌）  |
| `IFeishuTenantV1ChatTabs` → `IFeishuV1ChatTabs`                           | 继承父类 | 群标签页管理（租户令牌） |
| `IFeishuUserV1ChatTabs` → `IFeishuV1ChatTabs`                             | 继承父类 | 群标签页管理（用户令牌） |
| `IFeishuTenantV1ChatGroupMenu`                                            | -    | 群菜单管理（仅租户令牌） |

***

### 审批流程 (Approval)

| 接口名称                                                      | 继承关系 | 说明              |
| --------------------------------------------------------- | ---- | --------------- |
| `IFeishuTenantV4Approval`                                 | -    | 审批定义和实例管理（租户令牌） |
| `IFeishuTenantV4ApprovalTask`                             | -    | 审批任务管理（租户令牌）    |
| `IFeishuTenantV4ApprovalComments`                         | -    | 审批评论管理（租户令牌）    |
| `IFeishuTenantV4ApprovalExternal`                         | -    | 第三方审批（租户令牌）     |
| `IFeishuTenantV4ApprovalFile`                             | -    | 审批文件管理（租户令牌）    |
| `IFeishuTenantV4ApprovalQuery` → `IFeishuV4ApprovalQuery` | 继承父类 | 审批查询（租户令牌） |
| `IFeishuUserV4ApprovalQuery` → `IFeishuV4ApprovalQuery` | 继承父类 | 审批查询（用户令牌） |
| `IFeishuTenantV4ApprovalSubscribe` | - | 审批订阅（租户令牌） |

**租户令牌接口** **`IFeishuTenantV4Approval`** **主要 API 函数**：

- `CreateApprovalAsync` - 创建审批定义
- `GetApprovalByCodeAsync` - 获取审批定义信息
- `CreateInstanceAsync` - 创建审批实例
- `CancelInstanceAsync` - 撤回审批实例
- `CarbonCopyInstanceAsync` - 抄送审批实例
- `PreviewInstanceAsync` - 预览审批流程
- `GetInstanceByIdAsync` - 获取审批实例详情

***

### 任务管理 (Task)

**继承关系**：`IFeishuTenantV2Task` / `IFeishuUserV2Task` → `IFeishuV2Task`

| 接口类型         | 接口名称                  | API 函数                            | 说明       |
| ------------ | --------------------- | --------------------------------- | -------- |
| **父类接口（公共）** | `IFeishuV2Task`       | `CreateTaskAsync`                 | 创建任务     |
| <br />       | <br />                | `UpdateTaskAsync`                 | 更新任务     |
| <br />       | <br />                | `GetTaskByIdAsync`                | 获取任务详情   |
| <br />       | <br />                | `DeleteTaskByIdAsync`             | 删除任务     |
| <br />       | <br />                | `AddMembersByIdAsync`             | 添加任务成员   |
| <br />       | <br />                | `RemoveMembersByIdAsync`          | 移除任务成员   |
| <br />       | <br />                | `GetTaskListsByIdAsync`           | 获取任务所在清单 |
| <br />       | <br />                | `AddTaskListsByIdAsync`           | 将任务加入清单  |
| <br />       | <br />                | `RemoveTaskListsByIdAsync`        | 将任务移出清单  |
| <br />       | <br />                | `AddTaskReminderByIdAsync`        | 添加任务提醒   |
| <br />       | <br />                | `RemoveTaskReminderByIdAsync`     | 移除任务提醒   |
| <br />       | <br />                | `AddTaskDependenciesByIdAsync`    | 添加任务依赖   |
| <br />       | <br />                | `RemoveTaskDependenciesByIdAsync` | 移除任务依赖   |
| <br />       | <br />                | `CreateSubTaskAsync`              | 创建子任务    |
| <br />       | <br />                | `GetSubTasksPageListByIdAsync`    | 获取子任务列表  |
| **租户令牌接口**   | `IFeishuTenantV2Task` | -                                 | 仅继承父类方法  |
| **用户令牌接口**   | `IFeishuUserV2Task`   | -                                 | 仅继承父类方法  |

**其他任务相关接口**：

| 接口名称                                                                              | 继承关系 | 说明      |
| --------------------------------------------------------------------------------- | ---- | ------- |
| `IFeishuTenantV2TaskCustomFields` → `IFeishuV2TaskCustomFields`                   | 继承父类 | 自定义字段管理 |
| `IFeishuUserV2TaskCustomFields` → `IFeishuV2TaskCustomFields`                     | 继承父类 | 自定义字段管理 |
| `IFeishuTenantV2TaskComments` → `IFeishuV2TaskComments`                           | 继承父类 | 任务评论管理  |
| `IFeishuUserV2TaskComments` → `IFeishuV2TaskComments`                             | 继承父类 | 任务评论管理  |
| `IFeishuTenantV2TaskAttachments` → `IFeishuV2TaskAttachments`                     | 继承父类 | 任务附件管理  |
| `IFeishuUserV2TaskAttachments` → `IFeishuV2TaskAttachments`                       | 继承父类 | 任务附件管理  |
| `IFeishuTenantV2TaskList` → `IFeishuV2TaskList`                                   | 继承父类 | 任务清单管理  |
| `IFeishuUserV2TaskList` → `IFeishuV2TaskList`                                     | 继承父类 | 任务清单管理  |
| `IFeishuTenantV2TaskSections` → `IFeishuV2TaskSections`                           | 继承父类 | 任务分组管理  |
| `IFeishuUserV2TaskSections` → `IFeishuV2TaskSections`                             | 继承父类 | 任务分组管理  |
| `IFeishuTenantV2TaskActivitySubscriptions` → `IFeishuV2TaskActivitySubscriptions` | 继承父类 | 任务活动订阅  |

***

### 卡片管理 (Card)

| 接口名称                          | API 函数                        | 说明     |
| ----------------------------- | ----------------------------- | ------ |
| `IFeishuTenantV1Card`         | `CreateCardAsync`             | 创建卡片实体 |
| <br />                        | `UpdateCardSettingsByIdAsync` | 更新卡片配置 |
| <br />                        | `PartialUpdateCardByIdAsync`  | 局部更新卡片 |
| <br />                        | `UpdateCardByIdAsync`         | 全量更新卡片 |
| `IFeishuTenantV1CardElements` | 卡片元素管理                        | 元素操作   |

***

### 考勤管理 (Attendance)

| 接口名称                                 | API 函数                     | 说明       |
| ------------------------------------ | -------------------------- | -------- |
| `IFeishuTenantV1AttendanceUserFlows` | `BatchCreateUserFlowAsync` | 导入打卡流水   |
| <br />                               | `GetUserFlowAsync`         | 获取打卡流水记录 |
| <br />                               | `QueryUserFlowAsync`       | 批量查询打卡流水 |
| <br />                               | `BatchDelUserFlowAsync`    | 删除打卡流水   |
| <br />                               | `QueryUserTaskAsync`       | 查询打卡结果   |
| `IFeishuTenantV1AttendanceGroups`    | 考勤组管理                      | 组配置管理    |
| `IFeishuTenantV1AttendanceStats`     | 考勤统计                       | 统计报表     |
| `IFeishuTenantV1AttendanceShifts`    | 班次管理                       | 班次配置管理   |
| `IFeishuTenantV1AttendanceArchives`  | 考勤档案管理                     | 档案数据管理   |

***

### 云盘管理 (Drive)

**继承关系**：`IFeishuTenantV1DriveFiles` / `IFeishuUserV1DriveFiles` → `IFeishuV1DriveFiles`

| 接口类型         | 接口名称                        | API 函数                                      | 说明        |
| ------------ | --------------------------- | ------------------------------------------- | --------- |
| **父类接口（公共）** | `IFeishuV1DriveFiles`       | `BatchQueryMetasAsync`                      | 批量获取文件元数据 |
| <br />       | <br />                      | `GetFileStatisticsByFileTokenAsync`         | 获取文件统计信息  |
| <br />       | <br />                      | `GetFileViewRecordPageListByFileTokenAsync` | 获取文件访问记录  |
| <br />       | <br />                      | `CopyFileByFileTokenAsync`                  | 复制文件      |
| <br />       | <br />                      | `MoveFileByFileTokenAsync`                  | 移动文件      |
| <br />       | <br />                      | `DeleteFileByFileTokenAsync`                | 删除文件      |
| <br />       | <br />                      | `CreateShortcutAsync`                       | 创建快捷方式    |
| <br />       | <br />                      | `UploadAllFileAsync`                        | 上传文件（小文件） |
| <br />       | <br />                      | `UploadPrepareFileAsync`                    | 预上传（分片上传） |
| <br />       | <br />                      | `UploadPartFileAsync`                       | 上传分片      |
| <br />       | <br />                      | `UploadFinishFileAsync`                     | 完成分片上传    |
| <br />       | <br />                      | `DownloadFileAsync`                         | 下载文件      |
| <br />       | <br />                      | `CreateImportTaskAsync`                     | 创建导入任务    |
| <br />       | <br />                      | `GetImportTaskAsync`                        | 获取导入任务结果  |
| <br />       | <br />                      | `CreateExportTaskAsync`                     | 创建导出任务    |
| <br />       | <br />                      | `GetExportTaskAsync`                        | 获取导出任务结果  |
| <br />       | <br />                      | `DownloadExportFileAsync`                   | 下载导出文件    |
| <br />       | <br />                      | `GetFileLikePageListByFileTokenAsync`       | 获取文件点赞列表  |
| **租户令牌接口**   | `IFeishuTenantV1DriveFiles` | -                                           | 仅继承父类方法   |
| **用户令牌接口**   | `IFeishuUserV1DriveFiles`   | -                                           | 仅继承父类方法   |

**其他云盘相关接口**：

| 接口名称                                                                | 继承关系 | 说明     |
| ------------------------------------------------------------------- | ---- | ------ |
| `IFeishuTenantV1DriveFolder` → `IFeishuV1DriveFolder`               | 继承父类 | 文件夹管理  |
| `IFeishuUserV1DriveFolder` → `IFeishuV1DriveFolder`                 | 继承父类 | 文件夹管理  |
| `IFeishuTenantV1DriveFilesVersions` → `IFeishuV1DriveFilesVersions` | 继承父类 | 文件版本管理 |
| `IFeishuUserV1DriveFilesVersions` → `IFeishuV1DriveFilesVersions`   | 继承父类 | 文件版本管理 |
| `IFeishuTenantV1DriveMedia` → `IFeishuV1DriveMedia`                 | 继承父类 | 媒体文件管理 |
| `IFeishuUserV1DriveMedia` → `IFeishuV1DriveMedia`                   | 继承父类 | 媒体文件管理 |
| `IFeishuTenantV1DrivePermissions` → `IFeishuV1DrivePermissions`     | 继承父类 | 云文档权限管理 |
| `IFeishuUserV1DrivePermissions` → `IFeishuV1DrivePermissions`       | 继承父类 | 云文档权限管理 |
| `IFeishuTenantV1DriveSubscribe` → `IFeishuV1DriveSubscribe`         | 继承父类 | 云文档事件订阅 |
| `IFeishuUserV1DriveSubscribe` → `IFeishuV1DriveSubscribe`           | 继承父类 | 云文档事件订阅 |
| `IFeishuTenantV1Comments` → `IFeishuV1Comments`                     | 继承父类 | 云文档评论管理 |
| `IFeishuUserV1Comments` → `IFeishuV1Comments`                       | 继承父类 | 云文档评论管理 |

#### 云文档权限管理 (Drive Permissions)

**继承关系**：`IFeishuTenantV1DrivePermissions` / `IFeishuUserV1DrivePermissions` → `IFeishuV1DrivePermissions`

| API 函数 | 说明 |
|---------|------|
| `CreatePermissionMemberAsync` | 增加协作者权限 |
| `BatchCreatePermissionMemberAsync` | 批量增加协作者权限 |
| `UpdatePermissionMemberAsync` | 更新协作者权限 |
| `GetPermissionMemberAsync` | 获取云文档协作者列表 |
| `DeletePermissionMemberAsync` | 移除云文档协作者权限 |
| `TransferOwnerPermissionMemberAsync` | 转移云文档所有者 |
| `GetAuthPermissionMemberAsync` | 判断用户云文档权限 |
| `UpdatePermissionPublicAsync` | 更新云文档权限设置 |
| `GetPermissionPublicAsync` | 获取云文档权限设置 |
| `CreatePermissionPublicPasswordAsync` | 启用云文档密码 |
| `UpdatePermissionPublicPasswordAsync` | 刷新云文档密码 |
| `DeletePermissionPublicPasswordAsync` | 停用云文档密码 |

#### 云文档评论 (Drive Comments)

**继承关系**：`IFeishuTenantV1Comments` / `IFeishuUserV1Comments` → `IFeishuV1Comments`

| API 函数 | 说明 |
|---------|------|
| `GetCommentsPageListAsync` | 获取云文档所有评论 |
| `BatchQueryFileCommentAsync` | 批量获取评论 |
| `PatchFileCommentAsync` | 解决/恢复评论 |
| `CreateFileCommentAsync` | 添加全文评论 |
| `GetFileCommentAsync` | 获取评论详情 |
| `CreateFileCommentReplyAsync` | 添加回复 |
| `GetFileCommentRepliesPageListAsync` | 分页获取回复信息 |
| `UpdateFileCommentReplyAsync` | 更新回复的内容 |
| `DeleteFileCommentReplyAsync` | 删除回复 |
| `UpdateReactionCommentReactionAsync` | 添加/取消表情回应 |

#### 云文档事件订阅 (Drive Subscribe)

**继承关系**：`IFeishuTenantV1DriveSubscribe` / `IFeishuUserV1DriveSubscribe` → `IFeishuV1Drive

***

### 知识库 (Wiki)

**继承关系**：`IFeishuTenantV2Wiki` / `IFeishuUserV2Wiki` → `IFeishuV2Wiki`

| 接口类型         | 接口名称                  | API 函数                        | 说明         |
| ------------ | --------------------- | ----------------------------- | ---------- |
| **父类接口（公共）** | `IFeishuV2Wiki`       | `GetSpacesPageListAsync`      | 获取知识空间列表   |
| <br />       | <br />                | `GetSpaceInfoAsync`           | 获取知识空间信息   |
| <br />       | <br />                | `GetSpaceMemberPageListAsync` | 获取知识空间成员列表 |
| <br />       | <br />                | `CreateSpaceMemberAsync`      | 添加知识空间成员   |
| <br />       | <br />                | `DeleteSpaceMemberAsync`      | 删除知识空间成员   |
| <br />       | <br />                | `UpdateSpaceSettingAsync`     | 更新知识空间设置   |
| **租户令牌接口**   | `IFeishuTenantV2Wiki` | -                             | 仅继承父类方法    |
| **用户令牌接口**   | `IFeishuUserV2Wiki`   | -                             | 仅继承父类方法    |

**知识节点接口**：

| 接口名称                                              | 继承关系 | 说明     |
| ------------------------------------------------- | ---- | ------ |
| `IFeishuTenantV2WikiNodes` → `IFeishuV2WikiNodes` | 继承父类 | 知识节点管理 |

***

### 文档管理 (Docx)

| 接口名称                                                | 继承关系 | 说明       |
| --------------------------------------------------- | ---- | -------- |
| `IFeishuTenantV1Docx` → `IFeishuV1Docx`             | 继承父类 | 飞书文档基础操作 |
| `IFeishuUserV1Docx` → `IFeishuV1Docx`               | 继承父类 | 飞书文档基础操作 |
| `IFeishuTenantV1DocxBlocks` → `IFeishuV1DocxBlocks` | 继承父类 | 文档块操作    |
| `IFeishuUserV1DocxBlocks` → `IFeishuV1DocxBlocks`   | 继承父类 | 文档块操作    |

```csharp
IFeishuV1Docx       // 文档基础操作
IFeishuV1DocxBlocks // 文档块操作
```

**功能列表**：

- 文档创建和获取
- 文档块读取和更新
- 批量操作文档块
- 内容转换

---

### 画板管理 (Board)

**继承关系**：`IFeishuTenantV1Board` / `IFeishuUserV1Board` → `IFeishuV1Board`

| API 函数 | 说明 |
---------|------
| `GetWhiteboardThemeAsync` | 获取画板主题 |
| `UpdateWhiteboardThemeAsync` | 更新画板主题 |
| `DownloadWhiteboardImageAsync` | 获取画板缩略图片 |
| `CreatePlantumlWhiteboardNodeAsync` | 解析画板语法（PlantUML/Mermaid） |
| `CreateWhiteboardNodeAsync` | 创建画板节点 |
| `GetWhiteboardNodesAsync` | 获取所有节点 |

---

### 电子表格 (Spreadsheets)

| 接口名称 | 继承关系 | 说明 |
---------|---------|------
| `IFeishuTenantV3Spreadsheets` → `IFeishuV3Spreadsheets` | 继承父类 | 电子表格管理 |
| `IFeishuUserV3Spreadsheets` → `IFeishuV3Spreadsheets` | 继承父类 | 电子表格管理 |
| `IFeishuTenantV3SpreadsheetRange` → `IFeishuV3SpreadsheetRange` | 继承父类 | 区域操作 |
| `IFeishuUserV3SpreadsheetRange` → `IFeishuV3SpreadsheetRange` | 继承父类 | 区域操作 |
| `IFeishuTenantV3SpreadsheetData` → `IFeishuV3SpreadsheetData` | 继承父类 | 数据操作 |
| `IFeishuUserV3SpreadsheetData` → `IFeishuV3SpreadsheetData` | 继承父类 | 数据操作 |
| `IFeishuTenantV3SpreadsheetCell` → `IFeishuV3SpreadsheetCell` | 继承父类 | 单元格操作 |
| `IFeishuUserV3SpreadsheetCell` → `IFeishuV3SpreadsheetCell` | 继承父类 | 单元格操作 |
| `IFeishuTenantV3SpreadsheetFilter` → `IFeishuV3SpreadsheetFilter` | 继承父类 | 筛选操作 |
| `IFeishuUserV3SpreadsheetFilter` → `IFeishuV3SpreadsheetFilter` | 继承父类 | 筛选操作 |
| `IFeishuTenantV3SpreadsheetFilterView` → `IFeishuV3SpreadsheetFilterView` | 继承父类 | 筛选视图 |
| `IFeishuUserV3SpreadsheetFilterView` → `IFeishuV3SpreadsheetFilterView` | 继承父类 | 筛选视图 |
| `IFeishuTenantV3SpreadsheetFloatImage` → `IFeishuV3SpreadsheetFloatImage` | 继承父类 | 浮动图片 |
| `IFeishuUserV3SpreadsheetFloatImage` → `IFeishuV3SpreadsheetFloatImage` | 继承父类 | 浮动图片 |
| `IFeishuTenantV2SpreadsheetProtected` → `IFeishuV2SpreadsheetProtected` | 继承父类 | 保护范围 |
| `IFeishuUserV2SpreadsheetProtected` → `IFeishuV2SpreadsheetProtected` | 继承父类 | 保护范围 |
| `IFeishuTenantV2SpreadsheetDataValidation` → `IFeishuV2SpreadsheetDataValidation` | 继承父类 | 数据验证 |
| `IFeishuUserV2SpreadsheetDataValidation` → `IFeishuV2SpreadsheetDataValidation` | 继承父类 | 数据验证 |
| `IFeishuTenantV2SpreadsheetConditionFormat` → `IFeishuV2SpreadsheetConditionFormat` | 继承父类 | 条件格式 |
| `IFeishuUserV2SpreadsheetConditionFormat` → `IFeishuV2SpreadsheetConditionFormat` | 继承父类 | 条件格式 |

---

### 多维表格 (Bitable)

| 接口名称 | 继承关系 | 说明 |
---------|---------|------
| `IFeishuTenantV1Bitable` → `IFeishuV1Bitable` | 继承父类 | 多维表格应用管理 |
| `IFeishuUserV1Bitable` → `IFeishuV1Bitable` | 继承父类 | 多维表格应用管理 |
| `IFeishuTenantV1BitableAppTable` → `IFeishuV1BitableAppTable` | 继承父类 | 数据表管理 |
| `IFeishuUserV1BitableAppTable` → `IFeishuV1BitableAppTable` | 继承父类 | 数据表管理 |
| `IFeishuTenantV1BitableRecord` → `IFeishuV1BitableRecord` | 继承父类 | 记录管理 |
| `IFeishuUserV1BitableRecord` → `IFeishuV1BitableRecord` | 继承父类 | 记录管理 |
| `IFeishuTenantV1BitableField` → `IFeishuV1BitableField` | 继承父类 | 字段管理 |
| `IFeishuUserV1BitableField` → `IFeishuV1BitableField` | 继承父类 | 字段管理 |
| `IFeishuTenantV1BitableView` → `IFeishuV1BitableView` | 继承父类 | 视图管理 |
| `IFeishuUserV1BitableView` → `IFeishuV1BitableView` | 继承父类 | 视图管理 |
| `IFeishuTenantV1BitableForm` → `IFeishuV1BitableForm` | 继承父类 | 表单管理 |
| `IFeishuUserV1BitableForm` → `IFeishuV1BitableForm` | 继承父类 | 表单管理 |
| `IFeishuTenantV1BitableDashboard` → `IFeishuV1BitableDashboard` | 继承父类 | 仪表盘管理 |
| `IFeishuUserV1BitableDashboard` → `IFeishuV1BitableDashboard` | 继承父类 | 仪表盘管理 |
| `IFeishuTenantV2BitableRole` → `IFeishuV2BitableRole` | 继承父类 | 角色管理 |
| `IFeishuUserV2BitableRole` → `IFeishuV2BitableRole` | 继承父类 | 角色管理 |
| `IFeishuV1BitableWorkflow` | - | 自动化流程管理 |

### 日历日程 (Calendar)

**继承关系**：`IFeishuTenantV4Calendar` / `IFeishuUserV4Calendar` → `IFeishuV4Calendar`

| 接口类型 | 接口名称 | 说明 |
|---------|---------|------|
| **父类接口（公共）** | `IFeishuV4Calendar` | 日历管理 |
| **租户令牌接口** | `IFeishuTenantV4Calendar` | 日历管理（租户令牌） |
| **用户令牌接口** | `IFeishuUserV4Calendar` | 日历管理（用户令牌） |
| `IFeishuTenantV4CalendarAcl` → `IFeishuV4CalendarAcl` | 继承父类 | 日历访问控制（ACL） |
| `IFeishuTenantV4CalendarEvent` → `IFeishuV4CalendarEvent` | 继承父类 | 日程事件管理 |

**功能列表**：

- 主日历和次要日历管理
- 日程事件创建、更新、查询
- 日历访问控制（ACL）
- 日历共享和权限管理

***

### 视频会议 (VideoConferencing)

| 接口名称 | 继承关系 | 说明 |
|---------|---------|------|
| `IFeishuTenantV1VideoConferencingMeeting` → `IFeishuV1VideoConferencingMeeting` | 继承父类 | 会议管理 |
| `IFeishuTenantV1VideoConferencingConfig` | - | 会议配置（仅租户令牌） |
| `IFeishuTenantV1VideoConferencingRoom` → `IFeishuV1VideoConferencingRoom` | 继承父类 | 会议室管理 |
| `IFeishuTenantV1VideoConferencingRoomLevel` | - | 会议室层级管理（仅租户令牌） |
| `IFeishuTenantV1VideoConferencingReserves` → `IFeishuV1VideoConferencingReserves` | 继承父类 | 会议室预定 |
| `IFeishuTenantV1VideoConferencingRecording` → `IFeishuV1VideoConferencingRecording` | 继承父类 | 录制管理 |
| `IFeishuTenantV1VideoConferencingNotes` | - | 会议纪要管理 |
| `IFeishuTenantV1VideoConferencingExports` → `IFeishuV1VideoConferencingExports` | 继承父类 | 会议导出 |
| `IFeishuTenantV1VideoConferencingMeetinData` → `IFeishuV1VideoConferencingMeetinData` | 继承父类 | 会议数据 |
| `IFeishuTenantV1VideoConferencingReport` | - | 会议报表（仅租户令牌） |

**功能列表**：

- 会议查询、邀请、踢出、设置主持人
- 会议室管理和会议室层级管理
- 会议配置和预定管理
- 会议数据导出和报表
- 会议录制和纪要管理

***

### AI 能力 (AI)

| 接口名称 | 继承关系 | 说明 |
|---------|---------|------|
| `IFeishuTenantV1AIDocument` → `IFeishuV1AIDocument` | 继承父类 | AI 文档解析 |
| `IFeishuTenantV1AIOpticalCharRecognition` | - | OCR 识别（仅租户令牌） |
| `IFeishuTenantV1AISpeechToText` | - | 语音转文字（仅租户令牌） |
| `IFeishuTenantV1AITranslation` | - | 文本翻译（仅租户令牌） |

**功能列表**：

- 文档解析（简历解析、合同字段提取）
- OCR 识别（身份证、银行卡、营业执照、驾驶证等 18 种证件票据）
- 语音转文字（文件识别、流式识别）
- 文本翻译和多语言检测

***

### 邮箱管理 (Mail)

**继承关系**：`IFeishuTenantV1MailMessage` / `IFeishuUserV1MailMessage` → `IFeishuV1MailMessage`

| 接口类型 | 接口名称 | 说明 |
|---------|---------|------|
| **父类接口（公共）** | `IFeishuV1MailMessage` | 邮件消息管理 |
| **租户令牌接口** | `IFeishuTenantV1MailMessage` | 邮件消息管理（租户令牌） |
| **用户令牌接口** | `IFeishuUserV1MailMessage` | 邮件消息管理（用户令牌） |

**其他邮箱相关接口**：

| 接口名称 | 继承关系 | 说明 |
|---------|---------|------|
| `IFeishuTenantV1MailAlias` | - | 邮件别名管理（仅租户令牌） |
| `IFeishuTenantV1MailContact` → `IFeishuV1MailContact` | 继承父类 | 联系人管理 |
| `IFeishuTenantV1MailDraft` | - | 草稿管理（仅用户令牌） |
| `IFeishuTenantV1MailFolder` → `IFeishuV1MailFolder` | 继承父类 | 文件夹管理 |
| `IFeishuTenantV1MailGroup` | - | 邮件组管理（仅租户令牌） |
| `IFeishuTenantV1MailLabel` → `IFeishuV1MailLabel` | 继承父类 | 标签管理 |
| `IFeishuTenantV1MailPublicMailbox` | - | 公共邮箱管理 |
| `IFeishuTenantV1MailRule` → `IFeishuV1MailRule` | 继承父类 | 邮件规则管理 |
| `IFeishuTenantV1MailTemplate` → `IFeishuV1MailTemplate` | 继承父类 | 邮件模板管理 |
| `IFeishuTenantV1MailThread` → `IFeishuV1MailThread` | 继承父类 | 邮件会话管理 |

**功能列表**：

- 邮件消息发送和管理
- 邮件别名和联系人管理
- 草稿和文件夹管理
- 邮件组、标签、规则管理
- 公共邮箱和邮件模板管理
- 邮件会话和事件订阅

***

### 搜索 (Search)

| 接口名称 | 继承关系 | 说明 |
|---------|---------|------|
| `IFeishuTenantV2SearchDataSource` | - | 数据源管理（仅租户令牌） |
| `IFeishuTenantV2SearchDocWiki` → `IFeishuV2SearchDocWiki` | 继承父类 | 文档/知识库搜索 |
| `IFeishuUserV2SearchSuite` | - | 套件搜索（仅用户令牌） |

**功能列表**：

- 搜索数据源管理
- 文档和知识库内容搜索
- 套件搜索

***

### 服务台 (HelpDesk)

**继承关系**：`IFeishuTenantV1HelpDeskAgent` / `IFeishuUserV1HelpDeskAgent` → `IFeishuV1HelpDeskAgent`

| 接口类型 | 接口名称 | 说明 |
|---------|---------|------|
| **父类接口（公共）** | `IFeishuV1HelpDeskAgent` | 客服管理 |
| **租户令牌接口** | `IFeishuTenantV1HelpDeskAgent` | 客服管理（租户令牌） |
| **用户令牌接口** | `IFeishuUserV1HelpDeskAgent` | 客服管理（用户令牌） |
| `IFeishuTenantV1HelpDeskTicket` → `IFeishuV1HelpDeskTicket` | 继承父类 | 工单管理 |

**功能列表**：

- 客服信息和技能管理
- 客服排班管理
- 工单创建、更新和查询
- 工单消息和自定义字段管理

## 多应用支持

Mud.Feishu 支持在同一个系统中管理多个飞书应用：

```csharp
// 配置多个应用
builder.Services.AddFeishuApp(configure =>
{
    configure.AddDefaultApp("default", "cli_xxx", "dsk_xxx");
    configure.AddApp("hr-app", "cli_yyy", "dsk_yyy", opt =>
    {
        opt.TimeOut = 45;
        opt.RetryCount = 5;
    });
});

// 使用指定应用
public class MultiAppService
{
    private readonly IFeishuAppManager _appManager;

    public async Task UseSpecificAppAsync()
    {
        // 获取指定应用的 API
        var userApi = _appManager.GetFeishuApi<IFeishuV3User_Tenant>("hr-app");
        var result = await userApi.GetUserInfoByIdAsync("user_123");
    }

    // 使用应用上下文切换器
    public async Task UseContextSwitcherAsync()
    {
        var contextSwitcher = _appManager.GetAppContextSwitcher();
        using (contextSwitcher.UseApp("hr-app"))
        {
            // 在此作用域内，所有 API 调用都使用 hr-app 的凭证
        }
    }
}
```

## 配置选项

| 配置项                     | 类型     | 默认值                      | 说明             |
| ----------------------- | ------ | ------------------------ | -------------- |
| `AppKey`                | string | -                        | 应用唯一标识（必需）     |
| `AppId`                 | string | -                        | 飞书应用 ID（必需）    |
| `AppSecret`             | string | -                        | 飞书应用密钥（必需）     |
| `BaseUrl`               | string | <https://open.feishu.cn> | API 基础地址       |
| `TimeOut`               | int    | 30                       | HTTP 请求超时时间（秒） |
| `RetryCount`            | int    | 3                        | 失败重试次数         |
| `RetryDelayMs`          | int    | 1000                     | 重试延迟时间（毫秒）     |
| `TokenRefreshThreshold` | int    | 300                      | 令牌刷新阈值（秒）      |
| `EnableLogging`         | bool   | true                     | 是否启用日志记录       |
| `IsDefault`             | bool   | false                    | 是否为默认应用        |

## 自定义模块注册

Mud.Feishu 支持自定义模块注册器，方便扩展：

```csharp
public class CustomModuleRegistrar : IFeishuModuleRegistrar
{
    public FeishuModule Module => FeishuModule.All + 1;

    public void Register(IServiceCollection services)
    {
        services.AddTransient<ICustomApi, CustomApi>();
    }
}

// 注册自定义模块
builder.Services.CreateFeishuServicesBuilder()
    .RegisterModule(new CustomModuleRegistrar())
    .Build();
```

## 核心特性

### 自动令牌管理

SDK 内置了智能的令牌管理机制：

- 自动获取和缓存 access\_token
- 令牌即将过期时自动刷新
- 支持应用令牌、租户令牌、用户令牌三种类型
- 高并发场景下的缓存击穿防护

### 智能重试机制

针对网络不稳定等场景，SDK 提供了可配置的重试策略：

- 可配置重试次数和延迟时间
- 支持指数退避策略
- 自动重试失败的 API 调用

### 类型安全

所有 API 都通过强类型接口定义：

- 编译时类型检查
- 完整的 XML 文档注释
- 智能提示支持

## 依赖项

| 包                           | 版本     | 说明                 |
| --------------------------- | ------ | ------------------ |
| **Mud.HttpUtils**           | v2.0.0 | HTTP 客户端工具类（含源代码生成器） |
| **Mud.HttpUtils.Generator** | v2.0.0 | HTTP 客户端代码生成器（编译时） |
| **Mud.Feishu.Abstractions** | \*     | 飞书 SDK 抽象层（同版本依赖）  |
| **Mud.Feishu.DataModels**   | \*     | 飞书 API 强类型数据模型（同版本依赖） |

## 框架支持

- .NET Standard 2.0
- .NET 6.0
- .NET 8.0
- .NET 10.0

## 相关项目

- [Mud.Feishu.Abstractions](../Mud.Feishu.Abstractions) - 事件订阅抽象层
- [Mud.Feishu.DataModels](../Mud.Feishu.DataModels) - HTTP API 强类型数据模型
- [Mud.Feishu.Authentication](../Mud.Feishu.Authentication) - 用户认证中间件
- [Mud.Feishu.EventCallback](../Mud.Feishu.EventCallback) - 事件回调强类型数据模型
- [Mud.Feishu.WebSocket](../Mud.Feishu.WebSocket) - WebSocket 实时事件订阅
- [Mud.Feishu.Webhook](../Mud.Feishu.Webhook) - Webhook HTTP 回调事件处理
- [Mud.Feishu.Redis](../Mud.Feishu.Redis) - Redis 分布式去重扩展
- [Mud.Feishu.OpenTelemetry](../Mud.Feishu.OpenTelemetry) - OpenTelemetry 可观测性扩展

## 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](../LICENSE) 文件

## 反馈与贡献

如有问题或建议，欢迎提交 Issue 或 Pull Request。

***

**Mud.Feishu** - 让飞书集成变得简单！
