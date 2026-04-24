# 群组 SDK 接口文档

## 概述

群组 SDK 提供了飞书群组的完整 API 封装，支持群组管理、成员管理、公告管理、菜单管理、会话标签页等功能，帮助开发者构建企业级群组管理应用。

**主要功能：**

- 群组创建、更新、解散
- 群成员管理与权限设置
- 群公告创建与编辑
- 群菜单自定义配置
- 会话标签页管理
- 群置顶与分享链接

**适用场景：**

- 企业群组自动化管理
- 群消息推送与通知
- 群组权限控制
- 群组数据分析与统计

**文档使用指引：**

本索引文档提供了所有群组相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及请求示例。点击各 API 链接可查看详细文档。

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

public class ChatGroupController : ControllerBase
{
    private readonly IFeishuTenantV1ChatGroup _chatGroupApi;

    public ChatGroupController(IFeishuTenantV1ChatGroup chatGroupApi)
    {
        _chatGroupApi = chatGroupApi;
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateChatGroupRequest request)
    {
        var result = await _chatGroupApi.CreateChatGroupAsync(request);
        return Ok(result);
    }
}
```

## API 接口导航

### 群组管理

- [飞书群组 API（租户）](./FeishuTenantV1ChatGroup.md) — 创建群聊、更新群信息、解散群组、获取群列表、群置顶管理
- [飞书群组 API（用户）](./FeishuUserV1ChatGroup.md) — 用户权限的群组管理，适用于用户身份操作群组

### 群成员管理

- [飞书群成员 API（租户）](./FeishuTenantV1ChatGroupMember.md) — 添加/移除群成员、添加/删除群管理员、分页获取群成员列表
- [飞书群成员 API（用户）](./FeishuUserV1ChatGroupMember.md) — 用户权限的群成员管理，适用于用户身份操作

### 群公告管理

- [飞书群公告 API（租户）](./FeishuTenantV1ChatGroupAnnouncement.md) — 获取群公告、创建/更新/删除群公告块
- [飞书群公告 API（用户）](./FeishuUserV1ChatGroupAnnouncement.md) — 用户权限的群公告管理

### 群菜单与标签页

- [飞书群菜单 API（租户）](./FeishuTenantV1ChatGroupMenu.md) — 添加、更新、删除、排序群菜单
- [飞书会话标签页 API（租户）](./FeishuTenantV1ChatTabs.md) — 创建、更新、删除、排序会话标签页
- [飞书会话标签页 API（用户）](./FeishuUserV1ChatTabs.md) — 用户权限的会话标签页管理

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.9
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
