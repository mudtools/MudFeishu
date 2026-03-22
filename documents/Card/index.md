# 卡片 SDK 接口文档

## 概述

卡片 SDK 提供了飞书卡片消息的完整 API 封装，支持卡片创建、更新、组件管理以及应用消息流卡片等功能，帮助开发者构建丰富的交互式消息体验。

**主要功能：**

- 飞书卡片实体创建与管理
- 卡片组件精细化管理
- 应用消息流卡片管理
- 流式更新文本内容
- 即时提醒与快捷操作

**适用场景：**

- 构建交互式消息卡片
- 自动化消息推送与更新
- 企业通知与告警系统
- 审批流程消息通知

**文档使用指引：**

本索引文档提供了所有卡片相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

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
using Mud.Feishu.DataModels.Cards;

public class CardController : ControllerBase
{
    private readonly IFeishuTenantV1Card _cardApi;

    public CardController(IFeishuTenantV1Card cardApi)
    {
        _cardApi = cardApi;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCard([FromBody] CreateCardRequest request)
    {
        var result = await _cardApi.CreateCardAsync(request);
        return Ok(result);
    }
}
```

## API 接口导航

### 卡片管理

- [飞书卡片管理接口](./FeishuTenantV1Card.md) — 创建卡片实体、更新卡片配置、局部/全量更新卡片内容
- [飞书卡片组件管理接口](./FeishuTenantV1CardElements.md) — 新增、更新、删除卡片组件，流式更新文本内容

### 消息流卡片

- [应用消息流卡片接口](./FeishuTenantV2AppCardMessageStream.md) — 创建、更新、删除消息流卡片，设置即时提醒与快捷操作按钮

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.5
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
