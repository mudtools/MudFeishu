# MudFeishu

<div align="center">

![MudFeishu Logo](icon.png)

企业级 .NET 飞书 API 集成 SDK

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Mud.Feishu.svg)](https://www.nuget.org/packages/Mud.Feishu/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Mud.Feishu.WebSocket.svg)](https://www.nuget.org/packages/Mud.Feishu.WebSocket/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Mud.Feishu.Webhook.svg)](https://www.nuget.org/packages/Mud.Feishu.Webhook/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Mud.Feishu.Abstractions.svg)](https://www.nuget.org/packages/Mud.Feishu.Abstractions/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Mud.Feishu.Authentication.svg)](https://www.nuget.org/packages/Mud.Feishu.Authentication/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Mud.Feishu.Redis.svg)](https://www.nuget.org/packages/Mud.Feishu.Redis/)

**完整的 HTTP API、WebSocket 实时事件订阅和 Webhook 事件处理解决方案**

[快速开始](#-快速开始) • [API 模块](#-api-模块) • [使用示例](#-使用示例) • [文档](#-详细文档)

</div>

---

## 📖 项目简介

MudFeishu 是一套现代化的企业级 .NET 飞书 API 集成 SDK，提供完整的 HTTP API 调用、WebSocket 实时事件订阅和 Webhook 事件处理能力。SDK 采用策略模式和工厂模式设计，内置自动令牌管理、智能重试、高性能缓存等企业级特性，大幅简化飞书应用的开发难度。

### ✨ 核心优势

- 🚀 **极简API** - 一行代码完成服务注册，开箱即用
- 🏗️ **类型安全** - 强类型数据模型，编译时类型检查
- 🔄 **自动令牌管理** - 智能缓存和刷新，无需手动维护
- 🛡️ **企业级稳定** - 统一异常处理、智能重试、详细日志
- 🎯 **事件驱动** - 策略模式事件处理，灵活扩展
- 📊 **多框架支持** - .NET Standard 2.0、.NET 6.0、.NET 8.0、.NET 10.0

---

## 📦 项目概览

| 组件                          | 描述                                                             | NuGet                                                                                                                               | 下载                                                                    |
| ----------------------------- | ---------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| **Mud.Feishu.Abstractions**   | 事件订阅抽象层，提供策略模式和工厂模式的事件处理架构             | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Abstractions.svg)](https://www.nuget.org/packages/Mud.Feishu.Abstractions/)     | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.Abstractions.svg)   |
| **Mud.Feishu**                | 核心 HTTP API 客户端库，支持组织架构、消息、群聊等完整飞书功能   | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.svg)](https://www.nuget.org/packages/Mud.Feishu/)                               | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.svg)                |
| **Mud.Feishu.Authentication** | 飞书用户认证中间件，基于 AsyncLocal 实现线程安全的用户上下文管理 | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Authentication.svg)](https://www.nuget.org/packages/Mud.Feishu.Authentication/) | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.Authentication.svg) |
| **Mud.Feishu.WebSocket**      | 飞书 WebSocket 客户端，支持实时事件订阅和自动重连                | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.WebSocket.svg)](https://www.nuget.org/packages/Mud.Feishu.WebSocket/)           | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.WebSocket.svg)      |
| **Mud.Feishu.Webhook**        | 飞书 Webhook 事件处理组件，支持 HTTP 回调事件接收和处理          | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Webhook.svg)](https://www.nuget.org/packages/Mud.Feishu.Webhook/)               | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.Webhook.svg)        |
| **Mud.Feishu.Redis**          | Redis 分布式去重扩展，支持多实例部署场景的事件去重               | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Redis.svg)](https://www.nuget.org/packages/Mud.Feishu.Redis/)                   | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.Redis.svg)          |

---

## 🚀 快速开始

### 1️⃣ 安装 NuGet 包

```bash
# HTTP API 客户端 (核心模块)
dotnet add package Mud.Feishu

# 事件处理抽象层 (核心模块，Mud.Feishu/WebSocket/Webhook 依赖)
dotnet add package Mud.Feishu.Abstractions

# WebSocket 实时事件订阅 (可选)
dotnet add package Mud.Feishu.WebSocket

# Webhook HTTP 回调事件处理 (可选)
dotnet add package Mud.Feishu.Webhook

# 用户认证中间件 (可选)
dotnet add package Mud.Feishu.Authentication

# Redis 分布式去重扩展 (可选)
dotnet add package Mud.Feishu.Redis
```

> 💡 **提示**：根据实际需求安装对应包，`Mud.Feishu` 是核心包，`Mud.Feishu.Abstractions` 已作为 Mud.Feishu\WebSocket\Webhook 的依赖自动安装。

### 2️⃣ 配置文件 (appsettings.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Mud.Feishu": "Debug"
    }
  },
  "FeishuApps": [
    {
      "AppKey": "default",
      "AppId": "your_feishu_app_id",
      "AppSecret": "your_feishu_app_secret",
      "BaseUrl": "https://open.feishu.cn",
      "TimeOut": 30,
      "RetryCount": 3,
      "RetryDelayMs": 1000,
      "EnableLogging": true
    }
  ],
  "WebSocket": {
    "AutoReconnect": true,
    "MaxReconnectAttempts": 5,
    "ReconnectDelayMs": 5000,
    "HeartbeatIntervalMs": 30000,
    "EnableLogging": true
  },
  "Webhook": {
    "GlobalRoutePrefix": "feishu",
    "EnableRequestLogging": true,
    "MaxConcurrentEvents": 10,
    "Apps": {
      "app1": {
        "AppKey": "cli_a1b2c3d4e5f6g7h8",
        "VerificationToken": "your_app1_verification_token",
        "EncryptKey": "your_app1_encrypt_key_32_bytes_long"
      },
      "app2": {
        "AppKey": "cli_h8g7f6e5d4c3b2a1",
        "VerificationToken": "your_app2_verification_token",
        "EncryptKey": "your_app2_encrypt_key_32_bytes_long"
      }
    }
  }
}
```

### 3️⃣ 服务注册 (Program.cs)

```csharp
using Mud.Feishu;
using Mud.Feishu.WebSocket;
using Mud.Feishu.Webhook;

var builder = WebApplication.CreateBuilder(args);

// 注册多应用模式（方式一：从配置文件加载）
builder.Services.AddFeishuApp(builder.Configuration);

// 注册多应用模式（方式二：代码配置）
builder.Services.AddFeishuApp(configure =>
{
    config.AddDefaultApp("default", "cli_xxx", "dsk_xxx");
    config.AddApp("hr-app", "cli_yyy", "dsk_yyy", opt =>
    {
        opt.TimeOut = 45;
        opt.RetryCount = 5;
        opt.RetryDelayMs = 2000; // 自定义重试延迟
        opt.TokenRefreshThreshold = 300; // Token 刷新阈值（秒）
    });
});

// 注册多应用模式（方式三：使用预构建的配置列表）
var configs = new List<FeishuAppConfig>
{
    new FeishuAppConfig { AppKey = "default", AppId = "cli_xxx", AppSecret = "dsk_xxx" }, // IsDefault 自动推断
    new FeishuAppConfig { AppKey = "hr-app", AppId = "cli_yyy", AppSecret = "dsk_yyy" }
};
builder.Services.AddFeishuApp(configs);

// 注册 HTTP API 服务（懒人模式 - 注册所有服务）
builder.Services.AddFeishuHttpClient();

// 注册 HTTP API 服务（构造者模式 - 按需注册）
builder.Services.CreateFeishuServicesBuilder()
    .AddOrganizationApi()
    .AddMessageApi()
    .AddChatGroupApi()
    .AddApprovalApi()
    .AddTaskApi()
    .AddCardApi()
    .Build();

// 注册 HTTP API 服务（按模块注册）
builder.Services.AddFeishuHttpClient(new[] {
    FeishuModule.Organization,
    FeishuModule.Message,
    FeishuModule.ChatGroup,
    FeishuModule.Approval
});

// 注册 WebSocket 事件订阅服务
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

// 注册 Webhook HTTP 回调事件服务
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .AddHandler<DepartmentCreatedEventHandler>()
    .Build();

var app = builder.Build();

// 添加 Webhook 中间件
app.UseFeishuWebhook();

app.Run();
```

### 4️⃣ 验证配置

```csharp
// 获取用户信息测试
public class TestController : ControllerBase
{
    private readonly IFeishuTenantV3User _userApi;

    public TestController(IFeishuTenantV3User userApi)
    {
        _userApi = userApi;
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestConnection()
    {
        var result = await _userApi.GetUserInfoByIdAsync("test_user_id");
        return Ok(new { code = result.Code, message = result.Msg });
    }
}
```

---

## 🎯 API 模块

Mud.Feishu 提供完整的飞书 HTTP API 覆盖，支持以下模块：

### 📋 API 模块总览

| 模块分类        | API版本 | 主要功能                                            |
| --------------- | ------- | --------------------------------------------------- |
| **🔐 认证授权** | V3      | 应用令牌、租户令牌、用户令牌、OAuth 2.0、多应用管理 |
| **👥 组织架构** | V1/V3   | 用户、部门、员工、用户组、职级、职务、角色          |
| **💬 消息服务** | V1      | 文本/图片/卡片消息、批量发送、群聊管理              |
| **📋 审批流程** | V4      | 审批定义、审批实例、审批任务、审批消息、审批统计    |
| **📝 任务管理** | V2      | 任务创建、更新、分组、附件、评论、自定义字段        |
| **📅 日程会议** | V4      | 日程事件、会议管理                                  |
| **📄 文档管理** | V1      | 飞书文档、文档块、内容转换、知识库                  |
| **📚 知识库**   | V2      | 知识空间、节点管理、节点复制移动                    |
| **☁️ 云盘管理** | V1      | 云空间、文件夹、文件上传、版本管理                  |
| **⏰ 考勤管理** | V1      | 考勤组、打卡记录、请假审批、考勤统计                |
| **🎴 卡片管理** | V1/V2   | 卡片管理、卡片元素、消息流卡片                      |

### 📄 文档管理 (Docx)

飞书文档 API，支持文档创建、编辑、块操作等。

```csharp
public interface IFeishuV1Docx
{
    // 文档基础操作
}

public interface IFeishuV1DocxBlocks
{
    // 文档块操作
}
```

**功能列表**：

- 文档创建和获取
- 文档块读取和更新
- 批量操作文档块
- 内容转换

### 📚 知识库 (Wiki)

知识空间和节点管理 API。

```csharp
public interface IFeishuV2Wiki
{
    // 知识空间管理
}

public interface IFeishuV2WikiNodes
{
    // 知识节点管理
}
```

**功能列表**：

- 知识空间创建和查询
- 节点树结构管理
- 节点复制和移动
- 文档导入知识库

### ☁️ 云盘管理 (Drive)

云空间文件和文件夹管理 API。

```csharp
public interface IFeishuV1DriveFiles
{
    // 文件操作
}

public interface IFeishuV1DriveFolder
{
    // 文件夹操作
}

public interface IFeishuV1DriveFilesVersions
{
    // 文件版本管理
}
```

**功能列表**：

- 文件上传和下载
- 文件夹创建和管理
- 文件版本控制
- 文件权限管理
- 媒体文件处理

### ⏰ 考勤管理 (Attendance)

企业考勤全流程管理 API。

```csharp
public interface IFeishuV1AttendanceGroups
{
    // 考勤组管理
}

public interface IFeishuV1AttendanceUserFlows
{
    // 打卡流水
}

public interface IFeishuV1AttendanceStats
{
    // 考勤统计
}
```

**功能列表**：

- 考勤组配置管理
- 打卡记录查询
- 班次管理
- 请假审批
- 补卡申请
- 考勤统计报表

### 📋 审批流程 (Approval)

企业审批全流程管理 API。

```csharp
public interface IFeishuV4Approval
{
    // 审批定义和实例
}

public interface IFeishuV4ApprovalTask
{
    // 审批任务管理
}
```

**功能列表**：

- 审批定义创建
- 审批实例发起
- 审批任务处理
- 审批评论
- 第三方审批集成

### 📝 任务管理 (Task)

飞书任务全功能 API。

```csharp
public interface IFeishuV2Task
{
    // 任务管理
}

public interface IFeishuV2TaskCustomFields
{
    // 自定义字段
}
```

**功能列表**：

- 任务创建和更新
- 任务列表管理
- 任务分组
- 任务评论和附件
- 自定义字段

### 👥 组织架构 (Organization)

完整的组织架构管理 API。

```csharp
public interface IFeishuTenantV3User
{
    // 用户管理
}

public interface IFeishuTenantV3Departments
{
    // 部门管理
}
```

**功能列表**：

- 用户 CRUD 操作
- 部门树管理
- 用户组管理
- 职级职务管理
- 角色权限管理

### 💬 消息服务 (Messages)

消息发送和批量消息 API。

```csharp
public interface IFeishuV1Message
{
    // 消息发送
}

public interface IFeishuV1BatchMessage
{
    // 批量消息
}
```

**功能列表**：

- 文本/图片/卡片消息
- 批量发送
- 消息撤回
- 已读状态查询

### 🎴 卡片管理 (Cards)

卡片和消息流卡片 API。

```csharp
public interface IFeishuV1Card
{
    // 卡片管理
}

public interface IFeishuV1CardElements
{
    // 卡片元素
}
```

**功能列表**：

- 卡片创建和更新
- 卡片元素管理
- 消息流卡片

### 🏢 群聊管理 (ChatGroup)

群组和会话管理 API。

```csharp
public interface IFeishuTenantV3ChatGroup
{
    // 群组管理
}
```

**功能列表**：

- 群组创建和管理
- 群成员管理
- 群公告
- 会话标签

---

## 🎯 核心功能

### 🏛️ Mud.Feishu.Abstractions - 事件处理抽象层

**统一的事件处理架构，WebSocket 和 Webhook 共享相同的处理器接口**

| 功能特性       | 说明                       |
| -------------- | -------------------------- |
| **策略模式**   | 可扩展的事件处理器架构     |
| **工厂模式**   | 动态注册和发现处理器       |
| **类型安全**   | 强类型数据模型，编译时检查 |
| **自动去重**   | 内置事件 ID 去重机制       |
| **事件拦截器** | 支持事件处理前后的拦截逻辑 |
| **基类处理器** | 简化开发的专用基类         |

**新增工具类**：

- `UrlValidator` - URL 白名单验证和 SSRF 防护
- `HttpRetryPolicyBuilder` - HTTP 重试策略构建器（支持指数退避和抖动）

### 🌐 Mud.Feishu - HTTP API 客户端

**企业级特性**：

- ✅ 自动令牌缓存和刷新
- ✅ 智能重试机制（可配置重试次数和延迟）
- ✅ 高性能缓存（解决缓存击穿）
- ✅ 统一异常处理
- ✅ 连接池管理
- ✅ 详细日志记录
- ✅ 多应用上下文切换支持
- ✅ 性能指标监控（内置 Meter 指标收集）

> 💡 **提示**：[查看完整 API 文档](./Mud.Feishu/README.md)

---

## 💡 快速开始示例

### HTTP API 调用

```csharp
// 创建用户
[HttpPost("users")]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    _userApi.UseApp("hr-app");// 多应用场景下切换应用
    var result = await _userApi.CreateUserAsync(request);
    _userApi.UseDefaultApp();
    return result.Code == 0 ? Ok(result.Data) : BadRequest(result.Msg);
}

// 多应用场景下使用 IFeishuAppManager
var tenantJobTitleApi = _feishuAppManager.GetFeishuApi<IFeishuTenantV3JobTitle>("hr-app");
var result = await tenantJobTitleApi.GetJobTitlesListAsync(10, null);

// 使用应用上下文切换器
var contextSwitcher = _feishuAppManager.GetAppContextSwitcher();
using (contextSwitcher.UseApp("hr-app"))
{
    var userApi = _feishuAppManager.GetFeishuApi<IFeishuTenantV3User>();
    var userResult = await userApi.GetUserInfoByIdAsync("user_123");
}
```

### 事件处理

```csharp
// WebSocket 实时事件订阅
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

// Webhook HTTP 回调事件处理
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<DepartmentCreatedEventHandler>()
    .Build();

app.UseFeishuWebhook();
```

---

## 📸 演示界面展示

以下是 **FeishuWikiManager**（飞书知识库管理 Demo）的实际运行界面截图，展示了 SDK 在实际项目中的应用效果：

### 用户认证与登录

| 飞书 OAuth 授权                                    | 系统登录界面                                     |
| :------------------------------------------------- | :----------------------------------------------- |
| ![飞书认证授权](./Images/wiki飞书认证授权界面.png) | ![登录界面](./Images/飞书云文档管理登陆界面.png) |

### 知识库管理核心功能

| 主界面                                         | 知识空间                                   |
| :--------------------------------------------- | :----------------------------------------- |
| ![知识库主界面](./Images/Wiki知识库主界面.png) | ![知识空间](./Images/Wiki知识空间界面.png) |

| 搜索功能                                     | 云空间同步                                               |
| :------------------------------------------- | :------------------------------------------------------- |
| ![搜索界面](./Images/Wiki知识库搜索界面.png) | ![云空间同步](./Images/飞书云文档管理云空间同步功能.png) |

### 云文档管理

| 文档管理主界面                                       | 文件上传                                             |
| :--------------------------------------------------- | :--------------------------------------------------- |
| ![文档管理主界面](./Images/飞书云文档管理主界面.png) | ![文件上传](./Images/飞书云文档管理文件上传界面.png) |

> 💡 **提示**：以上界面均基于 **Mud.Feishu** SDK 开发，完整展示了飞书 OAuth 认证、知识库管理、文档搜索、云空间同步等核心功能。[查看 Demo 源码](./Demos/FeishuWikiManager)

---

## 📖 详细文档

- [Mud.Feishu.Abstractions 详细文档](./Mud.Feishu.Abstractions/README.md) - 事件处理抽象层使用指南
- [Mud.Feishu 详细文档](./Mud.Feishu/README.md) - HTTP API 完整使用指南
- [Mud.Feishu.WebSocket 详细文档](./Mud.Feishu.WebSocket/Readme.md) - WebSocket 实时事件订阅指南
- [Mud.Feishu.Webhook 详细文档](./Mud.Feishu.Webhook/README.md) - Webhook HTTP 回调事件处理指南
- [Mud.Feishu.Authentication 详细文档](./Mud.Feishu.Authentication/README.md) - 飞书用户认证中间件使用指南
- [Mud.Feishu.Redis 详细文档](./Mud.Feishu.Redis/README.md) - Redis 分布式去重扩展指南
- [安全增强文档](./docs/SECURITY_IMPROVEMENTS.md) - SSRF 防护、URL 验证等安全特性详解

---

## 🛠️ 技术栈

### 框架支持

- **.NET Standard 2.0** - 兼容 .NET Framework 4.6.1+
- **.NET 6.0** - LTS 长期支持版本
- **.NET 8.0** - LTS 长期支持版本（推荐）
- **.NET 10.0** - LTS 长期支持版本（推荐）

### 核心依赖

| 包                                  | 版本             | 说明                            |
| ----------------------------------- | ---------------- | ------------------------------- |
| **Mud.HttpUtils**                   | v1.6.2           | HTTP 客户端工具类               |
| **Mud.HttpUtils.Generator**         | v1.6.2           | HTTP 客户端代码生成器（编译时） |
| **Microsoft.Extensions.Http**       | v8.0.1 / v10.0.4 | HTTP 客户端工厂                 |
| **Microsoft.Extensions.Http.Polly** | v8.0.2 / v10.0.4 | 弹性和瞬态故障处理              |

---

## 📄 许可证

本项目遵循 [MIT 许可证](./LICENSE)，允许商业和非商业用途。

---

## 🔗 相关链接

### 📖 官方文档

- [飞书开放平台文档](https://open.feishu.cn/document/) - 飞书 API 官方文档和最佳实践
- [NuGet 包管理器](https://www.nuget.org/) - .NET 包管理官方平台

### 📦 NuGet 包

- [Mud.Feishu.Abstractions](https://www.nuget.org/packages/Mud.Feishu.Abstractions/) - 事件处理抽象层
- [Mud.Feishu](https://www.nuget.org/packages/Mud.Feishu/) - 核心 HTTP API 客户端库
- [Mud.Feishu.WebSocket](https://www.nuget.org/packages/Mud.Feishu.WebSocket/) - WebSocket 实时事件订阅库
- [Mud.Feishu.Webhook](https://www.nuget.org/packages/Mud.Feishu.Webhook/) - Webhook HTTP 回调事件处理库
- [Mud.Feishu.Authentication](https://www.nuget.org/packages/Mud.Feishu.Authentication/) - 飞书用户认证中间件库
- [Mud.Feishu.Redis](https://www.nuget.org/packages/Mud.Feishu.Redis/) - Redis 分布式去重扩展库

### 🛠️ 开发资源

- [项目仓库](https://gitee.com/mudtools/MudFeishu) - 源代码和开发文档
- [Mud.ServiceCodeGenerator](https://gitee.com/mudtools/mud-code-generator) - HTTP 客户端代码生成器
- [示例项目](./Demos) - 完整的使用示例和演示代码
  - [FeishuWikiManager](./Demos/FeishuWikiManager) - 飞书知识库管理 Demo（Vue3 + .NET）
  - [Webhook Demo](./Demos/Mud.Feishu.Webhook.Demo) - Webhook 事件处理演示
  - [WebSocket Demo](./Demos/Mud.Feishu.WebSocket.Demo) - WebSocket 实时事件演示
- [测试项目](./Tests) - 完整的单元测试和集成测试

### 🤝 社区支持

- [问题反馈](https://gitee.com/mudtools/MudFeishu/issues) - Bug 报告和功能请求
- [贡献指南](./CONTRIBUTING.md) - 如何参与项目贡献
- [更新日志](./CHANGELOG.md) - 版本更新记录和变更说明

---

<div align="center">

**如果觉得 MudFeishu 对你有帮助，请给个 ⭐Star 支持一下！**

Made with ❤️ by MudTools

</div>
