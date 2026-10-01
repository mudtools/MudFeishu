---
title: 服务台 SDK 接口文档 | MudFeishu
description: 该文档为飞书服务台 OpenAPI 的接口导航总览，涵盖工单、客服坐席、知识库分类、常见问题、事件订阅与推送通知等能力的入口索引。
---

# 服务台 SDK 接口文档

## 概述

服务台 SDK 提供了飞书服务台（HelpDesk）的完整 API 封装，支持工单处理、客服坐席管理、知识库维护、事件订阅与推送通知等操作，帮助开发者构建企业级客户服务应用。

**主要功能：**

- 服务台工单的创建、查询、回复与消息发送
- 客服坐席状态、工作日程与技能管理
- 知识库分类与常见问题（FAQ）维护
- 服务台事件订阅与取消订阅
- 服务台推送任务的创建、审批、发送与取消

**适用场景：**

- 企业内部 IT 服务台自动化
- 客服工单系统与第三方系统集成
- 知识库内容同步与运营
- 员工服务通知的批量推送

**文档使用指引：**

本索引文档提供了所有服务台相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

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

public class HelpDeskController : ControllerBase
{
    private readonly IFeishuTenantV1HelpDeskTicket _ticketApi;

    public HelpDeskController(IFeishuTenantV1HelpDeskTicket ticketApi)
    {
        _ticketApi = ticketApi;
    }

    [HttpGet("{ticketId}")]
    public async Task<IActionResult> GetTicket(string ticketId)
    {
        // 服务台接口需设置 HelpdeskTokenAndId（X-Lark-Helpdesk-Authorization）
        _ticketApi.HelpdeskTokenAndId = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{helpdeskId}:{helpdeskToken}"));

        var result = await _ticketApi.GetTicketAsync(ticketId);
        return Ok(result);
    }
}
```

## API 接口导航

### 工单

- [飞书服务台工单租户接口](./FeishuTenantV1HelpDeskTicket.md) — 创建对话、查询工单详情/列表、获取工单图像与消息、回复用户提问、工单自定义字段查询
- [飞书服务台工单用户接口](./FeishuUserV1HelpDeskTicket.md) — 用户令牌的工单更新与工单自定义字段的创建/更新/删除

### 客服坐席

- [飞书服务台客服坐席租户接口](./FeishuTenantV1HelpDeskAgent.md) — 查询客服邮箱、客服工作日程、客服技能与技能规则
- [飞书服务台客服坐席用户接口](./FeishuUserV1HelpDeskAgent.md) — 更新客服信息、创建/更新/删除客服工作日程与客服技能

### 工单分类

- [飞书服务台知识库分类租户接口](./FeishuTenantV1HelpDeskCategory.md) — 获取全部知识库分类与查询单个知识库分类
- [飞书服务台知识库分类用户接口](./FeishuUserV1HelpDeskCategory.md) — 创建、更新与删除知识库分类

### 常见问题

- [飞书服务台知识库FAQ租户接口](./FeishuTenantV1HelpDeskFaq.md) — FAQ 列表查询、关键词搜索、FAQ 详情与 FAQ 图片获取
- [飞书服务台知识库FAQ用户接口](./FeishuUserV1HelpDeskFaq.md) — 创建、修改与删除知识库 FAQ

### 事件订阅

- [飞书服务台事件订阅租户接口](./FeishuTenantV1HelpDeskEvent.md) — 订阅与取消订阅服务台事件

### 通知消息

- [飞书服务台推送用户接口](./FeishuUserV1HelpDeskNotification.md) — 推送任务的创建、查询、更新、预览、提交审批、发送、取消发送与取消审批

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：3.0.0
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
