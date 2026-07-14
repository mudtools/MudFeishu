# Mud.Feishu.DataModels

飞书 HTTP API 强类型数据模型库，为 `Mud.Feishu` 核心 HTTP API 客户端提供完整的请求/响应数据模型定义。

## 项目简介

`Mud.Feishu.DataModels` 是 MudFeishu SDK 的数据模型层，专门用于封装飞书开放平台所有 HTTP API 的请求参数和响应结果。项目采用强类型设计，所有模型均带有完整的 XML 文档注释和 JSON 序列化特性标注，帮助开发者在编译时获得类型安全保障，并通过智能提示快速了解字段含义。

> 💡 本项目是 `Mud.Feishu` 的隐式依赖，安装 `Mud.Feishu` 后将自动引入，通常无需单独安装。

## 特性

- ✅ **强类型数据模型** - 所有 API 的请求参数和响应结果均有完整类型定义
- ✅ **完整 JSON 标注** - 使用 `[JsonPropertyName]` 精确映射飞书 API 字段
- ✅ **XML 文档注释** - 每个公共属性均有中文文档注释，支持智能提示
- ✅ **按模块组织** - 数据模型按飞书 API 模块分目录组织，结构清晰
- ✅ **请求/响应分离** - 每个模块下 `RequestModel` 和 `ResponseModel` 独立管理
- ✅ **公共模型复用** - `Common` 命名空间提供分页、通用筛选等共享模型
- ✅ **多框架支持** - 支持 .NET Standard 2.0，兼容 .NET Framework 4.6.1+

## 数据模型模块总览

| 模块 | 命名空间 | 说明 |
| --- | --- | --- |
| **🤖 AI 能力** | `Mud.Feishu.DataModels.AI` | 文档解析、OCR 识别、语音转文字、文本翻译 |
| **📋 审批流程** | `Mud.Feishu.DataModels.Approval` | 审批定义、实例、任务、评论、表单、外部审批、文件、消息、查询 |
| **⏰ 考勤管理** | `Mud.Feishu.DataModels.Attendance` | 考勤组、打卡流水、班次、请假、补卡、统计、档案、用户设置、每日排班 |
| **📋 多维表格** | `Mud.Feishu.DataModels.Bitable` | 应用、数据表、记录、字段、视图、表单、仪表盘、角色、自动化流程 |
| **🎨 画板管理** | `Mud.Feishu.DataModels.Board` | 画板主题、节点请求/响应模型 |
| **📅 日历日程** | `Mud.Feishu.DataModels.Calendar` | 日历管理、日程事件、ACL 访问控制 |
| **🎴 卡片管理** | `Mud.Feishu.DataModels.Card` | 卡片实体、卡片元素、消息流卡片 |
| **🏢 群聊管理** | `Mud.Feishu.DataModels.ChatGroup` | 群组、群成员、群公告、群菜单、群标签页 |
| **🔧 公共模型** | `Mud.Feishu.DataModels.Common` | 分页请求/结果、通用筛选、头像、自定义字段等共享模型 |
| **📄 文档管理** | `Mud.Feishu.DataModels.Docx` | 文档操作、文档块、通用文档模型 |
| **☁️ 云盘管理** | `Mud.Feishu.DataModels.Drive` | 文件、文件夹、版本、媒体、权限、评论、订阅 |
| **🎧 服务台** | `Mud.Feishu.DataModels.HelpDesk` | 客服管理、技能、排班、工单、工单消息 |
| **📧 邮箱管理** | `Mud.Feishu.DataModels.Mail` | 别名、联系人、草稿、事件、文件夹、邮件组、标签、模板、消息、公共邮箱、规则、会话 |
| **💬 消息服务** | `Mud.Feishu.DataModels.Messages` | 消息请求/响应模型，批量消息 |
| **👥 组织架构** | `Mud.Feishu.DataModels.Organization` | 用户、部门（V1/V3）、员工、人员类型、职务族、职级、职务、角色、单位、用户组、工作城市 |
| **🔍 搜索** | `Mud.Feishu.DataModels.Search` | 数据源、文档/知识库搜索、套件搜索 |
| **📊 电子表格** | `Mud.Feishu.DataModels.Spreadsheets` | 单元格、条件格式、数据、数据验证、筛选、筛选视图、浮动图片、保护范围 |
| **📝 任务管理** | `Mud.Feishu.DataModels.Task` | 任务、任务清单、任务分组、自定义字段、评论、附件、活动订阅 |
| **📹 视频会议** | `Mud.Feishu.DataModels.VideoConferencing` | 会议、配置、导出、会议数据、纪要、录制、报告、会议室、会议室层级 |
| **📚 知识库** | `Mud.Feishu.DataModels.Wiki` | 知识空间、空间成员、知识节点 |

## 命名规范

数据模型按以下约定组织：

```
Mud.Feishu.DataModels/
├── {Module}/                    # API 模块（如 Approval、Organization）
│   ├── {SubModule}/             # 子模块（如 ApprovalTask、ApprovalQuery）
│   │   ├── Common/              # 子模块公共模型
│   │   ├── RequestModel/        # 请求参数模型
│   │   └── ResponseModel/       # 响应结果模型
│   └── {SharedModel}.cs         # 模块级共享模型
└── Common/                      # 全局共享模型
    ├── PageRequest.cs           # 分页请求基类
    ├── PageListResult.cs        # 分页结果基类
    └── ...
```

### 模型命名约定

| 模型类型 | 命名约定 | 示例 |
| --- | --- | --- |
| 请求模型 | `{Operation}Request` | `CreateUserRequest`、`SendMessageRequest` |
| 响应模型 | `{Operation}Result` | `CreateUserResult`、`GetUserInfoResult` |
| 列表结果 | `{Entity}ListResult` | `GetDepartmentListResult` |
| 公共模型 | `{Entity}Info` / `{Entity}Data` | `UserInfo`、`TaskData` |

## 安装

```bash
dotnet add package Mud.Feishu.DataModels
```

> 💡 通常无需单独安装，`Mud.Feishu` 已包含本项目作为依赖。

## 使用示例

### 1. 配合 Mud.Feishu API 接口使用

```csharp
using Mud.Feishu.DataModels.Organization;
using Mud.Feishu.DataModels.Messages;

public class UserService
{
    private readonly IFeishuTenantV3User _userApi;

    public UserService(IFeishuTenantV3User userApi)
    {
        _userApi = userApi;
    }

    // 使用请求模型创建用户
    public async Task<CreateOrUpdateUserResult?> CreateUserAsync()
    {
        var request = new CreateUserRequest
        {
            UserId = "test_user_id",
            Name = "张三",
            Email = "zhangsan@example.com",
            Mobile = "13800138000",
            DepartmentIds = new List<string> { "dept_001" }
        };

        var result = await _userApi.CreateUserAsync(request);
        return result?.Data;
    }
}
```

### 2. 使用分页请求模型

```csharp
using Mud.Feishu.DataModels.Common;

public class DepartmentService
{
    private readonly IFeishuTenantV3Departments _deptApi;

    // 使用分页请求获取子部门列表
    public async Task GetSubDepartmentsAsync(string parentId)
    {
        var result = await _deptApi.GetDepartmentsByParentIdAsync(
            parentId,
            pageSize: 50,
            pageToken: null);

        // PageListResult<T> 包含分页信息和数据
        if (result?.Code == 0)
        {
            var departments = result.Data.Items;
            var nextPageToken = result.Data.PageToken;
        }
    }
}
```

### 3. 使用消息请求模型发送卡片消息

```csharp
using Mud.Feishu.DataModels.Messages;

public class MessageService
{
    private readonly IFeishuTenantV1Message _messageApi;

    public async Task SendTextMessageAsync(string receiveId)
    {
        var request = new SendMessageRequest
        {
            ReceiveId = receiveId,
            ReceiveIdType = "open_id",
            MsgType = "text",
            Content = """{"text":"Hello from Mud.Feishu!"}"""
        };

        var result = await _messageApi.SendMessageAsync(request);
    }
}
```

## 公共模型

`Mud.Feishu.DataModels.Common` 命名空间提供以下共享模型：

| 模型 | 说明 |
| --- | --- |
| `PageRequest` | 分页请求基类（PageSize、PageToken） |
| `PageListResult<T>` | 分页列表结果基类（Items、PageToken、HasMore） |
| `PageSearchRequest` | 分页搜索请求（继承 PageRequest，增加搜索关键词） |
| `SearchRequest` | 通用搜索请求 |
| `FilterSearchRequest` | 带筛选条件的搜索请求 |
| `FieldFilter` | 字段筛选器 |
| `FieldCondition` | 字段条件 |
| `AvatarInfo` | 头像信息 |
| `CustomFieldValue` | 自定义字段值 |
| `PhoneValue` | 电话号码值 |
| `UrlValue` | URL 值 |
| `UserValue` | 用户值 |
| `EnumValue` | 枚举值 |
| `AbnormalInfo` | 异常信息 |

## 框架支持

- .NET Standard 2.0（兼容 .NET Framework 4.6.1+、.NET Core 2.0+）

## 依赖项

| 包 | 说明 |
| --- | --- |
| **Mud.Feishu.Abstractions** | SDK 抽象层（提供 `FeishuApiResult<T>` 等基础模型） |
| **Mud.HttpUtils.Generator** | HTTP 客户端代码生成器（编译时分析器，不产出运行时依赖） |

## 相关项目

- [Mud.Feishu](../Mud.Feishu) - 核心 HTTP API 客户端库
- [Mud.Feishu.Abstractions](../Mud.Feishu.Abstractions) - 事件处理抽象层
- [Mud.Feishu.EventCallback](../Mud.Feishu.EventCallback) - 事件回调强类型数据模型
- [Mud.Feishu.WebSocket](../Mud.Feishu.WebSocket) - WebSocket 实时事件订阅
- [Mud.Feishu.Webhook](../Mud.Feishu.Webhook) - Webhook HTTP 回调事件处理

## 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](../LICENSE) 文件

---

**Mud.Feishu.DataModels** - 强类型数据模型，让飞书 API 调用更安全！
