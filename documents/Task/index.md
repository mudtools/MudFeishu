# 任务 SDK 接口文档

## 概述

任务 SDK 提供了飞书任务的完整 API 封装，支持任务创建与管理、清单管理、评论、附件、自定义字段等功能，帮助开发者构建企业级任务管理应用。

**主要功能：**

- 任务创建、更新、删除与查询
- 任务清单管理与成员管理
- 任务评论与沟通协作
- 任务附件上传与管理
- 自定义字段扩展
- 任务提醒与依赖关系
- 子任务管理
- 清单动态订阅通知

**适用场景：**

- 企业项目任务管理
- 团队协作与任务跟踪
- 工作流程自动化
- 任务数据统计分析

**文档使用指引：**

本索引文档提供了所有任务相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

## 快速开始

### 安装

```bash
dotnet add package Mud.Feishu
```

### 配置文件

在 `appsettings.json` 中添加飞书应用配置：

```json
{
  "FeishuApps": [
    {
      "AppKey": "default",
      "AppId": "cli_xxx",
      "AppSecret": "your_app_secret",
      "BaseUrl": "https://open.feishu.cn",
      "IsDefault": true
    }
  ]
}
```

### 注册服务

在 `Program.cs` 中注册飞书服务：

```csharp
// 添加飞书服务
builder.Services.AddFeishuApp(builder.Configuration, "FeishuApps");

// 注册 API 服务
builder.Services.CreateFeishuServicesBuilder()
    .AddModules(FeishuModule.All)
    .Build();
```

### 依赖注入使用

在 Controller 或服务中通过构造函数注入接口：

```csharp
using Mud.Feishu;

public class TaskController : ControllerBase
{
    private readonly IFeishuTenantV2Task _taskApi;

    public TaskController(IFeishuTenantV2Task taskApi)
    {
        _taskApi = taskApi;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var result = await _taskApi.CreateTaskAsync(request);
        return Ok(result);
    }
}
```

## API 接口导航

### 任务管理

- [飞书任务 V2（租户）](./FeishuTenantV2Task.md) — 创建、更新、删除任务，管理任务成员、清单、提醒、依赖关系及子任务
- [飞书任务 V2（用户）](./FeishuUserV2Task.md) — 用户令牌的任务管理，支持分页获取任务列表

### 任务清单

- [任务清单 V2（租户）](./FeishuTenantV2TaskList.md) — 创建、更新、删除清单，管理清单成员，获取清单任务列表
- [任务清单 V2（用户）](./FeishuUserV2TaskList.md) — 用户令牌的任务清单管理

### 任务评论

- [任务评论 V2（租户）](./FeishuTenantV2TaskComments.md) — 创建、更新、删除评论，列取评论列表
- [任务评论 V2（用户）](./FeishuUserV2TaskComments.md) — 用户令牌的任务评论管理

### 任务附件

- [任务附件 V2（租户）](./FeishuTenantV2TaskAttachments.md) — 上传、列取、获取、删除任务附件
- [任务附件 V2（用户）](./FeishuUserV2TaskAttachments.md) — 用户令牌的任务附件管理

### 自定义字段

- [任务自定义字段 V2（租户）](./FeishuTenantV2TaskCustomFields.md) — 创建、更新自定义字段，管理字段选项
- [任务自定义字段 V2（用户）](./FeishuUserV2TaskCustomFields.md) — 用户令牌的自定义字段管理

### 自定义分组

- [任务自定义分组 V2（租户）](./FeishuTenantV2TaskSections.md) — 创建、更新、删除自定义分组，获取分组任务列表
- [任务自定义分组 V2（用户）](./FeishuUserV2TaskSections.md) — 用户令牌的自定义分组管理

### 动态订阅

- [任务清单动态订阅 V2（租户）](./FeishuTenantV2TaskActivitySubscriptions.md) — 创建、查询、更新、删除清单动态订阅
- [任务清单动态订阅 V2（用户）](./FeishuUserV2TaskActivitySubscriptions.md) — 用户令牌的动态订阅管理

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.9
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
