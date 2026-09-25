# 飞书妙记（Minutes）SDK 接口文档

## 概述

飞书妙记（Minutes）SDK 提供了妙记服务端 OpenAPI 的完整封装，支持获取妙记基础信息、下载音视频文件、导出文字记录、获取访问统计数据、获取 AI 产物、搜索妙记、创建剪辑、导入云盘音视频生成妙记以及订阅妙记变更事件等能力。

> 说明：妙记的只读端点（基础信息、音视频下载、文字记录、统计数据、AI 产物、搜索）同时支持 tenant_access_token 与 user_access_token；写端点（创建剪辑、导入生成、订阅/取消订阅）仅支持 user_access_token。

**主要功能：**

- 获取妙记基础信息（标题、时长、封面、链接等）
- 下载妙记音视频文件（下载链接 1 天有效）
- 导出妙记文字记录（逐字稿，txt/srt 格式，二进制流）
- 获取妙记访问统计数据（PV、UV 与用户浏览列表）
- 获取妙记 AI 产物（总结、章节、待办、关键词、逐字稿）
- 按关键词/所有者/参与者/时间范围搜索妙记
- 创建妙记剪辑、导入云盘音视频生成妙记
- 订阅/取消订阅妙记变更事件

**适用场景：**

- 会议纪要的程序化归档与检索
- 妙记内容（逐字稿、总结、待办）集成到业务系统
- 音视频转写产物的批量下载与二次加工
- 妙记生成事件驱动的自动化流程

**文档使用指引：**

本索引文档提供了所有飞书妙记相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及响应示例。点击各 API 链接可查看详细文档。

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

public class MinutesController : ControllerBase
{
    private readonly IFeishuUserV1MinutesMinute _minuteApi;

    public MinutesController(IFeishuUserV1MinutesMinute minuteApi)
    {
        _minuteApi = minuteApi;
    }

    [HttpGet("minute")]
    public async Task<IActionResult> GetMinute(string minuteToken)
    {
        var result = await _minuteApi.GetMinuteAsync(minuteToken);
        return Ok(result);
    }
}
```

## API 接口导航

### 妙记管理

- [妙记（用户）](./FeishuUserV1MinutesMinute.md) — 基础信息、音视频下载、文字记录、统计、AI 产物、搜索、创建剪辑、导入生成、事件订阅
- [妙记（租户）](./FeishuTenantV1MinutesMinute.md) — 双令牌只读端点：基础信息、音视频下载、文字记录、统计、AI 产物、搜索

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **DTO 命名空间**：`Mud.Feishu.DataModels.Minutes`
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
