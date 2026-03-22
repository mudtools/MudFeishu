# 云空间 SDK 接口文档

## 概述

云空间 SDK 提供了飞书云空间的完整 API 封装，支持文件管理、文件夹管理、版本管理、素材管理等功能，帮助开发者构建企业级云存储应用。

**主要功能：**

- 文件上传下载与导入导出
- 文件夹创建与管理
- 文件版本控制
- 素材上传与下载
- 文件元数据查询
- 文件访问记录统计

**适用场景：**

- 企业文档管理系统
- 文件自动化备份与同步
- 文档协作与版本管理
- 素材资源管理

**文档使用指引：**

本索引文档提供了所有云空间相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

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

public class DriveController : ControllerBase
{
    private readonly IFeishuTenantV1DriveFolder _folderApi;

    public DriveController(IFeishuTenantV1DriveFolder folderApi)
    {
        _folderApi = folderApi;
    }

    [HttpGet("root")]
    public async Task<IActionResult> GetRootFolder()
    {
        var result = await _folderApi.GetDriveRootFolderMetaAsync();
        return Ok(result);
    }
}
```

## API 接口导航

### 文件管理

- [云空间文件管理（租户）](./FeishuTenantV1DriveFiles.md) — 文件上传下载、复制移动删除、导入导出、元数据查询
- [云空间文件管理（用户）](./FeishuUserV1DriveFiles.md) — 用户权限的文件管理，以用户身份操作云文档

### 文件版本管理

- [文件版本管理（租户）](./FeishuTenantV1DriveFilesVersions.md) — 创建、查询、删除文档版本
- [文件版本管理（用户）](./FeishuUserV1DriveFilesVersions.md) — 用户权限的版本管理

### 文件夹管理

- [文件夹管理（租户）](./FeishuTenantV1DriveFolder.md) — 获取文件夹元数据、创建文件夹、获取文件清单

### 素材管理

- [素材管理（租户）](./FeishuTenantV1DriveMedia.md) — 素材上传下载、获取临时下载链接
- [素材管理（用户）](./FeishuUserV1DriveMedia.md) — 用户权限的素材管理

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.5
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
