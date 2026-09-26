# 搜索 SDK 接口文档

## 概述

搜索 SDK 提供了飞书搜索（search-v2）相关 API 的完整封装，支持搜索连接器的数据源与数据范式管理、数据项索引维护，以及文档搜索与套件搜索，帮助开发者构建企业级统一检索应用。

**主要功能：**

- 搜索连接器数据源的创建、删除、更新与查询
- 数据项索引的创建、批量创建、删除与查询
- 数据范式的创建、删除、修改与查询
- 按关键词搜索用户可见的云文档
- 套件内消息与应用的关键词搜索

**适用场景：**

- 企业内外部信息的一站式检索
- 业务系统数据接入飞书搜索
- 云文档搜索能力集成
- 套件内消息与应用检索

**文档使用指引：**

本索引文档提供了所有搜索相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

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

public class SearchController : ControllerBase
{
    private readonly IFeishuTenantV2SearchDataSource _dataSourceApi;
    private readonly IFeishuUserV2SearchDocWiki _docWikiApi;

    public SearchController(IFeishuTenantV2SearchDataSource dataSourceApi, IFeishuUserV2SearchDocWiki docWikiApi)
    {
        _dataSourceApi = dataSourceApi;
        _docWikiApi = docWikiApi;
    }

    [HttpGet("data_sources")]
    public async Task<IActionResult> GetDataSources()
    {
        var result = await _dataSourceApi.GetDataSourcePageListAsync();
        return Ok(result);
    }

    [HttpPost("doc_wiki")]
    public async Task<IActionResult> SearchDocWiki([FromBody] SearchDocWikiRequest request)
    {
        var result = await _docWikiApi.SearchDocWikiAsync(request);
        return Ok(result);
    }
}
```

## API 接口导航

### 搜索连接器

- [搜索数据源（租户）](./FeishuTenantV2SearchDataSource.md) — 数据源与数据范式的创建、删除、更新、查询，数据项索引维护

### 文档搜索

- [搜索文档（租户）](./FeishuTenantV2SearchDocWiki.md) — 以租户身份按关键词搜索可见云文档
- [搜索文档（用户）](./FeishuUserV2SearchDocWiki.md) — 以用户身份按关键词搜索可见云文档

### 套件搜索

- [搜索套件（用户）](./FeishuUserV2SearchSuite.md) — 以用户身份搜索套件内可见的消息与应用

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.9
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
