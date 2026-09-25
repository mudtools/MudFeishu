# 飞书妙搭（Spark）SDK 接口文档

## 概述

飞书妙搭（Spark）SDK 提供了妙搭应用服务端 OpenAPI 的完整封装，支持自定义枚举查询、SQL 执行、数据表与数据记录管理、视图数据查询以及应用文件资源上传下载等能力，帮助开发者把妙搭应用的数据与文件集成到自己的业务系统中。

> 说明：飞书妙搭 OpenAPI 中，数据表、记录、视图、枚举、SQL、文件存储等端点仅提供 user_access_token（用户身份）调用方式，本模块 SDK 相应只提供用户令牌（`IFeishuUserV1Spark*`）接口；应用管理（Spark App）模块的只读端点（批量查询应用、AI 额度、运营数据）同时支持 tenant_access_token 与 user_access_token，写端点（创建/更新应用、图标上传、HTML 发布、可用范围）为 user-only。

**主要功能：**

- 自定义枚举列表与详情查询
- 在妙搭应用下执行 SQL 语句
- 数据表列表与详情查询、数据记录增删改查与批量更新
- 视图数据记录查询（支持列筛选、过滤、排序）
- 文件上传下载（含 20MB 以内直传与分片上传）
- 应用管理（创建、更新、图标上传、HTML 发布、可用范围）与运营数据查询（AI 额度、总览、趋势）

**适用场景：**

- 低代码应用数据同步与集成
- 妙搭表单/数据表的数据展示与报表
- 应用附件、图片等资源的程序化管理
- 基于 SQL 的自定义数据加工

**文档使用指引：**

本索引文档提供了所有飞书妙搭相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及响应示例。点击各 API 链接可查看详细文档。

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

public class SparkController : ControllerBase
{
    private readonly IFeishuUserV1SparkAppTable _tableApi;

    public SparkController(IFeishuUserV1SparkAppTable tableApi)
    {
        _tableApi = tableApi;
    }

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables(string appId)
    {
        var result = await _tableApi.GetTableListAsync(appId);
        return Ok(result);
    }
}
```

## API 接口导航

### 应用管理

- [应用管理（用户）](./FeishuUserV1SparkApp.md) — 创建/更新应用、图标上传、HTML 发布、可用范围，及双令牌只读端点（应用列表、AI 额度、运营数据）
- [应用管理（租户）](./FeishuTenantV1SparkApp.md) — 双令牌只读端点：应用列表、AI 额度、运营数据总览与趋势

### 自定义枚举

- [自定义枚举（用户）](./FeishuUserV1SparkAppEnum.md) — 获取自定义枚举列表与枚举详情

### SQL

- [执行 SQL（用户）](./FeishuUserV1SparkAppSql.md) — 在妙搭应用下执行 SQL 语句并获取结果

### 数据表与记录

- [数据表（用户）](./FeishuUserV1SparkAppTable.md) — 数据表列表与详情、记录查询、添加与更新、批量更新、按条件删除

### 视图

- [视图（用户）](./FeishuUserV1SparkAppView.md) — 视图数据记录查询

### 文件存储

- [文件存储（用户）](./FeishuUserV1SparkAppStorage.md) — 文件上传下载、分片上传（初始化/上传分片/完成上传）

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.9
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
