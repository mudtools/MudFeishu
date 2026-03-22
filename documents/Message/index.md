# 消息 SDK 接口文档

## 概述

消息 SDK 提供了飞书即时消息的完整 API 封装，支持消息发送、回复、编辑、撤回、转发以及批量消息管理等功能，帮助开发者构建企业级消息应用。

**主要功能：**

- 发送多种类型消息（文本、富文本、卡片、图片等）
- 消息回复、编辑、撤回
- 消息转发与合并转发
- 批量消息发送与管理
- 消息已读状态查询
- 表情回复与 Pin 消息

**适用场景：**

- 企业通知推送系统
- 机器人消息服务
- 消息自动化处理
- 批量营销消息

**文档使用指引：**

本索引文档提供了所有消息相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

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
using Mud.Feishu.DataModels.Messages;

public class MessageController : ControllerBase
{
    private readonly IFeishuTenantV1Message _messageApi;

    public MessageController(IFeishuTenantV1Message messageApi)
    {
        _messageApi = messageApi;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendMessageRequest request,
        [FromQuery] string receiveIdType = "open_id")
    {
        var result = await _messageApi.SendMessageAsync(request, receiveIdType);
        return Ok(result);
    }
}
```

## API 接口导航

### 消息管理

- [消息管理（租户）](./FeishuTenantV1Message.md) — 发送、回复、编辑、撤回、转发消息，获取历史消息与已读状态
- [消息管理（用户）](./FeishuUserV1Message.md) — 用户权限的消息管理，支持撤回、表情回复、Pin 消息等

### 批量消息

- [批量消息管理](./FeishuTenantV1BatchMessage.md) — 批量发送文本、富文本、图片等消息，查询发送进度与已读状态

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.5
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
