# 知识库 SDK 接口文档

## 概述

知识库 SDK 提供了飞书 Wiki 知识库的完整 API 封装，支持知识空间管理、节点操作、成员权限管理等功能，帮助开发者构建企业级知识管理应用。

**主要功能：**

- 知识空间创建与管理
- 知识空间成员权限管理
- 知识空间节点创建、移动、复制
- 云文档移动至知识空间
- Wiki 节点搜索
- 异步任务状态查询

**适用场景：**

- 企业知识库建设与管理
- 团队文档协作与共享
- 知识内容结构化组织
- 文档权限精细化管理

**文档使用指引：**

本索引文档提供了所有知识库相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

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

public class WikiController : ControllerBase
{
    private readonly IFeishuTenantV2Wiki _wikiApi;
    private readonly IFeishuTenantV2WikiNodes _wikiNodesApi;

    public WikiController(IFeishuTenantV2Wiki wikiApi, IFeishuTenantV2WikiNodes wikiNodesApi)
    {
        _wikiApi = wikiApi;
        _wikiNodesApi = wikiNodesApi;
    }

    [HttpGet("spaces")]
    public async Task<IActionResult> GetSpaces()
    {
        var result = await _wikiApi.GetSpacesPageListAsync();
        return Ok(result);
    }

    [HttpPost("nodes")]
    public async Task<IActionResult> CreateNode([FromBody] CreateSpaceNodeRequest request)
    {
        var result = await _wikiNodesApi.CreateSpaceNodeAsync(request.SpaceId, request);
        return Ok(result);
    }
}
```

## API 接口导航

### 知识空间管理

- [Wiki 知识库（租户）](./FeishuV2Wiki_Tenant.md) — 获取知识空间列表、空间详情、成员管理、更新空间设置
- [Wiki 知识库（用户）](./FeishuV2Wiki_User.md) — 用户权限的知识空间管理，支持创建知识空间

### 知识空间节点

- [Wiki 知识库节点（租户）](./FeishuV2WikiNodes_Tenant.md) — 创建、移动、复制节点，移动云文档至知识空间
- [Wiki 知识库节点（用户）](./FeishuV2WikiNodes_User.md) — 用户权限的节点管理，支持 Wiki 搜索功能

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.5
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
