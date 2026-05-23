# 画板 SDK 接口文档

## 概述

画板 SDK 提供了飞书画板的完整 API 封装，支持画板主题管理、缩略图获取、语法解析导入、节点创建与查询等功能，帮助开发者构建企业级图形协作应用。

**主要功能：**

- 画板主题获取与更新
- 画板缩略图导出
- PlantUml/Mermaid 语法解析导入
- 画板节点创建（支持批量与父子关系）
- 画板节点查询与组装

**适用场景：**

- 图形化协作工具集成
- 流程图与规划图自动化生成
- 文档内画板内容管理
- PlantUml/Mermaid 图表协同编辑
- 企业知识库图形化管理

**文档使用指引：**

本索引文档提供了所有画板相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

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

public class BoardController : ControllerBase
{
    private readonly IFeishuTenantV1Board _boardApi;

    public BoardController(IFeishuTenantV1Board boardApi)
    {
        _boardApi = boardApi;
    }

    [HttpGet("theme")]
    public async Task<IActionResult> GetTheme(string whiteboardId)
    {
        var result = await _boardApi.GetWhiteboardThemeAsync(whiteboardId);
        return Ok(result);
    }
}
```

## API 接口导航

### 画板管理

- [画板（租户）](./FeishuTenantV1Board.md) — 画板主题管理、缩略图导出、语法解析、节点创建与查询
- [画板（用户）](./FeishuUserV1Board.md) — 用户令牌的画板管理，以用户身份操作画板

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.9
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
