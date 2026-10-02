---
title: 关联组织（TrustParty）SDK 接口文档 | MudFeishu
description: 该文档为飞书关联组织服务端 OpenAPI 的接口导航总览，涵盖可见关联组织列表与详情查询、组织内可见实体查询、共享成员范围查询以及可搜可见规则管理等能力的入口索引。
---

# 关联组织（TrustParty）SDK 接口文档

## 概述

关联组织 SDK 提供了飞书 trust_party-v1 与 directory-v1 相关 API 的完整封装，支持可见关联组织列表与详情查询、组织内可见部门/成员/用户组信息查询、共享成员范围查询以及可搜可见规则管理，帮助开发者构建跨组织协同应用。

**主要功能：**

- 可见关联组织列表与关联组织详情查询
- 关联组织内可见实体（部门/用户/用户组）查询
- 关联组织部门详情与成员详情查询
- 本组织与关联组织双方共享成员范围查询
- 可搜可见规则的查询、新增、更新与删除
- 管理员视角获取所有已建联关联组织

**适用场景：**

- 跨组织通讯录打通与检索
- 关联组织成员信息同步
- 可搜可见规则的自动化配置
- 供应链、渠道伙伴协同管理

**文档使用指引：**

本索引文档提供了所有关联组织相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

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

public class TrustPartyController : ControllerBase
{
    private readonly IFeishuTenantV1TrustPartyCollaborationTenant _collaborationApi;
    private readonly IFeishuTenantV1DirectoryCollaborationRule _ruleApi;

    public TrustPartyController(IFeishuTenantV1TrustPartyCollaborationTenant collaborationApi, IFeishuTenantV1DirectoryCollaborationRule ruleApi)
    {
        _collaborationApi = collaborationApi;
        _ruleApi = ruleApi;
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants()
    {
        var result = await _collaborationApi.GetCollaborationTenantListAsync();
        return Ok(result);
    }

    [HttpGet("rules")]
    public async Task<IActionResult> GetRules(string targetTenantKey)
    {
        var result = await _ruleApi.GetCollaborationRuleListAsync(targetTenantKey);
        return Ok(result);
    }
}
```

## API 接口导航

### 关联组织

- [关联组织（租户）](./FeishuTenantV1TrustPartyCollaborationTenant.md) — 可见关联组织列表、组织详情、可见成员信息、部门详情与成员详情
- [关联组织（用户）](./FeishuUserV1TrustPartyCollaborationTenant.md) — 用户令牌的关联组织查询，可见性按用户维度校验

### 关联组织管理端

- [关联组织管理端（租户）](./FeishuTenantV1DirectoryCollaborationTenant.md) — 管理员视角获取所有已建联关联组织列表
- [关联组织管理端（用户）](./FeishuUserV1DirectoryCollaborationTenant.md) — 用户令牌的管理员视角关联组织列表

### 共享成员范围

- [共享成员范围（租户）](./FeishuTenantV1DirectoryShareEntity.md) — 查询双方共享的部门、用户组与成员范围
- [共享成员范围（用户）](./FeishuUserV1DirectoryShareEntity.md) — 用户令牌的共享成员范围查询

### 可搜可见规则

- [可搜可见规则（租户）](./FeishuTenantV1DirectoryCollaborationRule.md) — 可搜可见规则的查询、新增、更新与删除
- [可搜可见规则（用户）](./FeishuUserV1DirectoryCollaborationRule.md) — 用户令牌的可搜可见规则管理

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：3.0.0
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
