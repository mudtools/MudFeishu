# 电子表格 SDK 接口文档

## 概述

电子表格 SDK 提供了飞书电子表格的完整 API 封装，支持表格与工作表管理、数据读写、单元格操作、条件格式、数据校验、数据保护、筛选与筛选视图、浮动图片及行列范围管理等功能，帮助开发者构建企业级电子表格应用。

**主要功能：**

- 电子表格与工作表的创建、查询及属性管理
- 数据读取、写入、插入与追加
- 单元格合并、拆分、查找、替换及样式设置
- 条件格式的批量创建、更新、获取与删除
- 数据校验（下拉列表）管理
- 数据保护范围设置
- 筛选与筛选视图管理
- 浮动图片的创建、更新、查询与删除
- 行列范围的增加、插入、更新、移动与删除

**适用场景：**

- 企业报表自动化生成与更新
- 数据采集与批量写入
- 表格协作与权限保护
- 条件格式与数据校验规则管理
- 多维度数据筛选与视图管理
- 文档数据同步与备份

**文档使用指引：**

本索引文档提供了所有电子表格相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。所有接口均提供租户令牌（TenantAccessToken）和用户令牌（UserAccessToken）两种认证方式。

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

public class SpreadsheetController : ControllerBase
{
    private readonly IFeishuTenantV3Spreadsheets _spreadsheetsApi;
    private readonly IFeishuTenantV3SpreadsheetData _dataApi;

    public SpreadsheetController(
        IFeishuTenantV3Spreadsheets spreadsheetsApi,
        IFeishuTenantV3SpreadsheetData dataApi)
    {
        _spreadsheetsApi = spreadsheetsApi;
        _dataApi = dataApi;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateSpreadsheet()
    {
        var result = await _spreadsheetsApi.CreateSpreadsheetAsync(
            new CreateSpreadsheetRequest { Title = "新建报表" });
        return Ok(result);
    }

    [HttpGet("data")]
    public async Task<IActionResult> GetRangeData(string token, string range)
    {
        var result = await _dataApi.GetRangeDataAsync(token, range);
        return Ok(result);
    }
}
```

## API 接口导航

### 电子表格与工作表

- [电子表格（IFeishuV3Spreadsheets）](./FeishuV3Spreadsheets.md) — 创建电子表格、修改属性、获取信息，以及工作表的增加/复制/删除/属性更新/查询

### 数据读写

- [数据读写（IFeishuV3SpreadsheetData）](./FeishuV3SpreadsheetData.md) — 插入数据、追加数据、写入图片、读取单个/多个范围数据、向单个/多个范围写入数据

### 单元格操作

- [单元格操作（IFeishuV3SpreadsheetCell）](./FeishuV3SpreadsheetCell.md) — 合并/拆分单元格、查找/替换单元格、设置/批量设置单元格样式

### 条件格式

- [条件格式（IFeishuV2SpreadsheetConditionFormat）](./FeishuV2SpreadsheetConditionFormat.md) — 批量创建/更新/获取/删除条件格式

### 数据校验

- [数据校验（IFeishuV2SpreadsheetDataValidation）](./FeishuV2SpreadsheetDataValidation.md) — 创建/更新/获取/删除下拉列表数据校验

### 数据保护

- [数据保护（IFeishuV2SpreadsheetProtected）](./FeishuV2SpreadsheetProtected.md) — 增加/修改/获取/删除保护范围

### 筛选

- [筛选（IFeishuV3SpreadsheetFilter）](./FeishuV3SpreadsheetFilter.md) — 创建/更新/获取/删除筛选

### 筛选视图

- [筛选视图（IFeishuV3SpreadsheetFilterView）](./FeishuV3SpreadsheetFilterView.md) — 筛选视图的创建/更新/查询/获取/删除，以及筛选条件的创建/更新/查询/获取/删除

### 浮动图片

- [浮动图片（IFeishuV3SpreadsheetFloatImage）](./FeishuV3SpreadsheetFloatImage.md) — 创建/更新/获取/查询/删除浮动图片

### 行列范围

- [行列范围（IFeishuV3SpreadsheetRange）](./FeishuV3SpreadsheetRange.md) — 增加/插入/更新/移动/删除行列

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.9
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
