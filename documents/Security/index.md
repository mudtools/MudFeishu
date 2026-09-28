---
title: 安全与合规（Security）SDK 接口文档 | MudFeishu
description: 该文档为飞书安全与合规服务端 OpenAPI 的接口导航总览，涵盖行为审计日志、OpenAPI 审计日志、数据驻留与用户迁移，以及设备管理与设备申报等能力的入口索引。
---

# 安全与合规（Security）SDK 接口文档

## 概述

安全与合规 SDK 提供了飞书 security_and_compliance-v1 / v2 相关 API 的完整封装，支持行为审计日志与 OpenAPI 审计日志查询、数据驻留与用户迁移管理，以及设备管理中设备记录与设备申报的维护，帮助管理员发现违规操作、保护企业数据安全。

**主要功能：**

- 查询成员操作行为审计日志
- 获取 OpenAPI 审计日志数据
- 获取数据驻留地理位置、迁移用户数据驻留位置与查询/取消迁移任务
- 设备记录的新增、分页查询、查询单个、更新与删除
- 审批成员自主申报的设备申请

**适用场景：**

- 违规操作审计与安全事件追溯
- OpenAPI 调用留痕与合规审查
- 跨国企业数据驻留合规迁移
- 可信设备的批量纳管与申报审批

**文档使用指引：**

本索引文档提供了所有安全与合规相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及响应示例。点击各 API 链接可查看详细文档。

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

public class SecurityController : ControllerBase
{
    private readonly IFeishuTenantV1SecurityAuditLog _auditLogApi;
    private readonly IFeishuTenantV2SecurityDeviceRecord _deviceRecordApi;

    public SecurityController(IFeishuTenantV1SecurityAuditLog auditLogApi, IFeishuTenantV2SecurityDeviceRecord deviceRecordApi)
    {
        _auditLogApi = auditLogApi;
        _deviceRecordApi = deviceRecordApi;
    }

    [HttpGet("audit_logs")]
    public async Task<IActionResult> GetAuditLogs()
    {
        var result = await _auditLogApi.GetAuditLogDataAsync();
        return Ok(result);
    }

    [HttpGet("devices/{deviceRecordId}")]
    public async Task<IActionResult> GetDevice(string deviceRecordId)
    {
        var result = await _deviceRecordApi.GetDeviceRecordAsync(deviceRecordId);
        return Ok(result);
    }
}
```

## API 接口导航

### 审计日志

- [行为审计日志（租户）](./FeishuTenantV1SecurityAuditLog.md) — 查询成员操作行为日志（时间、地点、操作对象等）
- [OpenAPI 审计日志（租户）](./FeishuTenantV1SecurityOpenApiLog.md) — 获取 OpenAPI 调用审计日志数据

### 数据驻留与用户迁移

- [数据驻留与用户迁移（租户）](./FeishuTenantV1SecurityUserMigration.md) — 可用地理位置查询、用户迁移、迁移状态查询与取消

### 设备管理

- [设备管理（租户）](./FeishuTenantV2SecurityDeviceRecord.md) — 设备记录的新增、分页查询、获取、更新与删除
- [设备申报（租户）](./FeishuTenantV2SecurityDeviceApplyRecord.md) — 审批成员自主申报的设备申请

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.9
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
