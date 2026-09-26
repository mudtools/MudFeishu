# 飞书词典（Lingo）SDK 接口文档

## 概述

飞书词典 SDK 提供了飞书词典（lingo-v1）相关 API 的完整封装，支持词库与分类查询、词条详情与列表、模糊/精准搜索与高亮识别、草稿申请，以及词条图片上传下载，帮助开发者构建企业知识词条应用。

**主要功能：**

- 词库列表与词典分类查询
- 词条详情、列表查询与模糊/精准搜索
- 词条智能高亮识别
- 词条创建/更新草稿申请与草稿更新
- 免审词条的创建、更新与删除（仅租户令牌）
- 词条图片上传与下载

**适用场景：**

- 企业百科词条建设与维护
- 业务术语统一管理与检索
- 外部系统词条数据同步
- 文档与 IM 场景的词条智能高亮

**文档使用指引：**

本索引文档提供了所有飞书词典相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

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

public class LingoController : ControllerBase
{
    private readonly IFeishuTenantV1LingoRepo _repoApi;
    private readonly IFeishuTenantV1LingoEntity _entityApi;

    public LingoController(IFeishuTenantV1LingoRepo repoApi, IFeishuTenantV1LingoEntity entityApi)
    {
        _repoApi = repoApi;
        _entityApi = entityApi;
    }

    [HttpGet("repos")]
    public async Task<IActionResult> GetRepos()
    {
        var result = await _repoApi.GetRepoListAsync();
        return Ok(result);
    }

    [HttpGet("entities/{entityId}")]
    public async Task<IActionResult> GetEntity(string entityId)
    {
        var result = await _entityApi.GetEntityAsync(entityId);
        return Ok(result);
    }
}
```

## API 接口导航

### 词库

- [词库（租户）](./FeishuTenantV1LingoRepo.md) — 以应用身份获取有权限查看的词库列表
- [词库（用户）](./FeishuUserV1LingoRepo.md) — 用户令牌的词库列表查询

### 词典分类

- [词典分类（租户）](./FeishuTenantV1LingoClassification.md) — 分页获取词典的一级/二级分类与国际化分类名
- [词典分类（用户）](./FeishuUserV1LingoClassification.md) — 用户令牌的词典分类查询

### 词条

- [词典词条（租户）](./FeishuTenantV1LingoEntity.md) — 词条详情、列表、搜索、高亮，以及免审创建/更新/删除
- [词典词条（用户）](./FeishuUserV1LingoEntity.md) — 用户令牌的词条查询、搜索与高亮

### 草稿

- [词典草稿（租户）](./FeishuTenantV1LingoDraft.md) — 以应用身份发起词条创建/更新草稿并更新草稿内容
- [词典草稿（用户）](./FeishuUserV1LingoDraft.md) — 用户令牌的词条草稿申请与更新

### 词条图片

- [词典文件（租户）](./FeishuTenantV1LingoFile.md) — 以应用身份上传与下载词条图片
- [词典文件（用户）](./FeishuUserV1LingoFile.md) — 用户令牌的词条图片上传与下载

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.9
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
