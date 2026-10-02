---
title: 邮箱 SDK 接口文档 | MudFeishu
description: 该文档为飞书邮箱服务端 OpenAPI 的接口导航总览，涵盖邮件收发、会话、草稿、文件夹、标签、规则、邮件组、公共邮箱与模板等能力的入口索引。
---

# 邮箱 SDK 接口文档

## 概述

邮箱 SDK 提供了飞书邮箱的完整 API 封装，支持邮件收发、会话与草稿管理、邮箱组织（文件夹/标签/收信规则）、联系人、邮件组与公共邮箱、模板与签名等操作，帮助开发者构建企业级邮件集成应用。

**主要功能：**

- 邮件发送、读取、修改、撤回、投递状态查询
- 邮件会话与草稿的增删改查、定时发送
- 邮箱文件夹、标签、收信规则与联系人管理
- 邮件组、公共邮箱及其成员/权限管理
- 邮件模板与邮箱签名、可发信地址查询
- 写信场景的多实体搜索与邮箱事件订阅

**适用场景：**

- 企业邮件自动化与通知推送
- 客服/工单系统邮件接入
- 邮件归档、审计与合规处理
- 邮箱组织结构与权限的集中治理

**文档使用指引：**

本索引文档提供了所有邮箱相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明、响应示例及请求示例。点击各 API 链接可查看详细文档。

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

public class MailController : ControllerBase
{
    private readonly IFeishuUserV1MailMessage _mailApi;

    public MailController(IFeishuUserV1MailMessage mailApi)
    {
        _mailApi = mailApi;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendUserMailboxMessageRequest request)
    {
        var result = await _mailApi.SendUserMailboxMessageAsync("me", request);
        return Ok(result);
    }
}
```

> 说明：`Tenant`（`IFeishuTenantV1*`）接口使用租户访问令牌，适用于代企业内用户执行操作；`User`（`IFeishuUserV1*`）接口使用用户访问令牌，适用于以当前授权用户身份操作，`user_mailbox_id` 可使用占位符 `me` 表示当前授权用户的主邮箱。

## API 接口导航

### 邮件与会话

- [邮件（租户令牌）](./FeishuTenantV1MailMessage.md) — 批量/单个邮件的发送、读取、修改、删除与撤回
- [邮件（用户令牌）](./FeishuUserV1MailMessage.md) — 用户身份的邮件收发，含发送状态、搜索与取消定时发送
- [邮件会话（租户令牌）](./FeishuTenantV1MailThread.md) — 会话的批量/单个修改、删除、详情与分页列出
- [邮件会话（用户令牌）](./FeishuUserV1MailThread.md) — 用户身份的邮件会话管理
- [邮件草稿（用户令牌）](./FeishuUserV1MailDraft.md) — 草稿的创建、更新、读取、发送与定时发送

### 邮箱组织与联系人

- [邮箱文件夹（租户令牌）](./FeishuTenantV1MailFolder.md) — 文件夹的创建、更新、查询与删除
- [邮箱文件夹（用户令牌）](./FeishuUserV1MailFolder.md) — 用户身份的文件夹管理，含可访问邮箱列出
- [邮箱标签（租户令牌）](./FeishuTenantV1MailLabel.md) — 标签的创建、更新、查询与删除
- [邮箱标签（用户令牌）](./FeishuUserV1MailLabel.md) — 用户身份的标签管理
- [收信规则（租户令牌）](./FeishuTenantV1MailRule.md) — 收信规则的增删改查与排序
- [收信规则（用户令牌）](./FeishuUserV1MailRule.md) — 用户身份的收信规则管理
- [邮箱联系人（租户令牌）](./FeishuTenantV1MailContact.md) — 联系人的创建、更新、查询与删除
- [邮箱联系人（用户令牌）](./FeishuUserV1MailContact.md) — 用户身份的联系人管理
- [邮箱地址（租户令牌）](./FeishuTenantV1MailAlias.md) — 别名的创建、删除、列出与邮箱地址状态查询
- [邮箱地址（用户令牌）](./FeishuUserV1MailAlias.md) — 查询用户主邮箱地址

### 邮件组与公共邮箱

- [邮件组（租户令牌）](./FeishuTenantV1MailGroup.md) — 邮件组、成员、别名、管理者与权限成员的完整管理
- [公共邮箱（租户令牌）](./FeishuTenantV1MailPublicMailbox.md) — 公共邮箱、成员、别名与回收站管理
- [公共邮箱（用户令牌）](./FeishuUserV1MailPublicMailbox.md) — 用户可访问的公共邮箱查询

### 模板与签名

- [邮件模板（租户令牌）](./FeishuTenantV1MailTemplate.md) — 模板的创建、查询、更新、删除与可发信邮箱列出
- [邮件模板（用户令牌）](./FeishuUserV1MailTemplate.md) — 用户身份的模板管理，含邮箱签名查询

### 搜索与事件

- [多实体搜索（用户令牌）](./FeishuUserV1MailMultiEntity.md) — 写信联系人等多实体搜索
- [邮箱事件（用户令牌）](./FeishuUserV1Mail.md) — 用户邮箱事件的订阅、查询与取消订阅

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：3.0.0
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
