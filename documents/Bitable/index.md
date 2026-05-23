# 多维表格 SDK 接口文档

## 概述

多维表格 SDK 提供了飞书多维表格的完整 API 封装，支持多维表格应用管理、数据表管理、字段管理、记录管理、视图管理、表单管理、仪表盘管理、高级权限管理和自动化流程管理等功能，帮助开发者构建企业级多维表格应用。

**主要功能：**

- 多维表格应用创建与管理
- 数据表增删改查
- 字段定义与管理
- 记录数据增删改查与批量操作
- 视图创建与管理
- 表单元数据与问题管理
- 仪表盘管理
- 高级权限（自定义角色与协作者管理）
- 自动化流程管理

**适用场景：**

- 企业数据管理平台
- 项目管理与任务跟踪
- 数据采集与表单管理
- 业务流程自动化
- 团队协作与权限管控
- 数据报表与可视化

**文档使用指引：**

本索引文档提供了所有多维表格相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

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

public class BitableController : ControllerBase
{
    private readonly IFeishuTenantV1BitableRecord _recordApi;

    public BitableController(IFeishuTenantV1BitableRecord recordApi)
    {
        _recordApi = recordApi;
    }

    [HttpGet("records")]
    public async Task<IActionResult> QueryRecords(string appToken, string tableId)
    {
        var result = await _recordApi.QueryRecordsPageListAsync(appToken, tableId, new QueryRecordsRequest());
        return Ok(result);
    }
}
```

## API 接口导航

### 多维表格应用

- [多维表格（租户）](./FeishuTenantV1Bitable.md) — 创建、获取、更新多维表格应用
- [多维表格（用户）](./FeishuUserV1Bitable.md) — 用户令牌的多维表格应用管理

### 数据表

- [数据表（租户）](./FeishuTenantV1BitableAppTable.md) — 数据表增删改查、批量操作
- [数据表（用户）](./FeishuUserV1BitableAppTable.md) — 用户令牌的数据表管理

### 仪表盘

- [仪表盘（租户）](./FeishuTenantV1BitableDashboard.md) — 仪表盘增删改查
- [仪表盘（用户）](./FeishuUserV1BitableDashboard.md) — 用户令牌的仪表盘管理

### 字段

- [字段（租户）](./FeishuTenantV1BitableField.md) — 字段增删改查、字段编组管理
- [字段（用户）](./FeishuUserV1BitableField.md) — 用户令牌的字段管理

### 表单

- [表单（租户）](./FeishuTenantV1BitableForm.md) — 表单升级、元数据管理、问题管理
- [表单（用户）](./FeishuUserV1BitableForm.md) — 用户令牌的表单管理

### 记录

- [记录（租户）](./FeishuTenantV1BitableRecord.md) — 记录增删改查、批量操作
- [记录（用户）](./FeishuUserV1BitableRecord.md) — 用户令牌的记录管理

### 视图

- [视图（租户）](./FeishuTenantV1BitableView.md) — 视图增删改查
- [视图（用户）](./FeishuUserV1BitableView.md) — 用户令牌的视图管理

### 高级权限

- [高级权限（租户）](./FeishuTenantV2BitableRole.md) — 自定义角色管理、协作者管理
- [高级权限（用户）](./FeishuUserV2BitableRole.md) — 用户令牌的高级权限管理

### 自动化流程

- [自动化流程（租户）](./FeishuTenantV1BitableWorkflow.md) — 列出自动化流程、更新流程状态、列出工作流
- [自动化流程（用户）](./FeishuUserV1BitableWorkflow.md) — 用户令牌的自动化流程管理

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.9
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
