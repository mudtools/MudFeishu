# Mud.Feishu SDK 接口文档

## 概述

Mud.Feishu 是一个企业级 .NET SDK，为飞书开放平台 API 提供完整的封装。SDK 支持 HTTP API 客户端、WebSocket 实时事件和 Webhook 事件处理，帮助开发者快速构建企业级飞书应用。

**核心特性：**

- 完整的飞书 API 接口封装
- 支持租户令牌和用户令牌两种认证方式
- 异步编程模型，支持取消令牌
- 统一的响应模型和错误处理
- 依赖注入友好设计

**适用场景：**

- 企业内部系统集成
- 飞书机器人开发
- 企业办公自动化
- 数据同步与分析

**文档使用指引：**

本索引文档提供了所有 SDK 模块的导航入口。每个模块包含该领域相关 API 的详细文档。点击各模块链接可查看该模块的完整 API 列表。

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

public class MyController : ControllerBase
{
    private readonly IFeishuTenantV1Message _messageApi;
    private readonly IFeishuTenantV3User _userApi;

    public MyController(IFeishuTenantV1Message messageApi, IFeishuTenantV3User userApi)
    {
        _messageApi = messageApi;
        _userApi = userApi;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var result = await _messageApi.SendMessageAsync(request, "open_id");
        return Ok(result);
    }
}
```

## 模块导航

### 协作与沟通

| 模块 | 说明 |
|------|------|
| [消息（Message）](./Message/index.md) | 消息发送、回复、编辑、撤回、转发，获取历史消息与已读状态 |
| [群组（ChatGroup）](./ChatGroup/index.md) | 群聊创建、更新、解散，群成员管理，群公告，群菜单，会话标签页 |
| [卡片（Card）](./Card/index.md) | 卡片创建、更新，卡片组件管理，消息流卡片 |

### 文档与存储

| 模块 | 说明 |
|------|------|
| [文档（Docx）](./Docx/index.md) | 飞书云文档创建、获取，文档块管理 |
| [云空间（Drive）](./Drive/index.md) | 文件上传下载、文件夹管理、素材管理 |
| [知识库（Wiki）](./Wiki/index.md) | 知识空间管理，节点创建、移动、复制，Wiki 搜索 |

### 组织与人员

| 模块 | 说明 |
|------|------|
| [组织架构（Organization）](./Organization/index.md) | 部门管理、用户管理、角色权限、职务职级、用户群组 |

### 业务应用

| 模块 | 说明 |
|------|------|
| [审批（Approval）](./Approval/index.md) | 原生审批与三方审批管理，审批实例创建与查询 |
| [考勤（Attendance）](./Attendance/index.md) | 考勤组管理、班次管理、打卡记录、考勤统计、请假审批 |
| [任务（Task）](./Task/index.md) | 任务创建与管理，清单管理，评论、附件、自定义字段 |

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.5
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
