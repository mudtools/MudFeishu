# 云文档 SDK 接口文档

## 概述

云文档 SDK 提供了飞书云文档的完整 API 封装，支持文档创建、查询、块管理等操作，帮助开发者构建企业级文档管理应用。

**主要功能：**

- 创建 docx 格式云文档
- 获取文档基本信息与纯文本内容
- 文档块增删改查
- 批量更新与嵌套块创建
- Markdown/HTML 内容转换

**适用场景：**

- 自动化文档生成
- 文档内容批量处理
- 知识库同步与管理
- 报表系统文档输出

**文档使用指引：**

本索引文档提供了所有云文档相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

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

public class DocxController : ControllerBase
{
    private readonly IFeishuTenantV1Docx _docxApi;

    public DocxController(IFeishuTenantV1Docx docxApi)
    {
        _docxApi = docxApi;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentRequest request)
    {
        var result = await _docxApi.CreateDocumentAsync(request);
        return Ok(result);
    }
}
```

## API 接口导航

### 文档管理

- [飞书云文档租户接口](./FeishuTenantV1Docx.md) — 创建文档、获取文档信息、获取文档纯文本内容
- [飞书云文档用户接口](./FeishuUserV1Docx.md) — 用户权限的文档管理，以用户身份操作文档

### 文档块管理

- [飞书云文档块租户接口](./FeishuTenantV1DocxBlocks.md) — 创建/更新/删除文档块、批量更新、内容转换
- [飞书云文档块用户接口](./FeishuUserV1DocxBlocks.md) — 用户权限的文档块管理，以用户身份编辑文档内容

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：待补充
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
