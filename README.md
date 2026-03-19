# MudFeishu

<div align="center">

![MudFeishu Logo](icon.png)

企业级 .NET 飞书 API 集成 SDK

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.svg)](https://www.nuget.org/packages/Mud.Feishu/)
[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.WebSocket.svg)](https://www.nuget.org/packages/Mud.Feishu.WebSocket/)
[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.Webhook.svg)](https://www.nuget.org/packages/Mud.Feishu.Webhook/)
[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.Abstractions.svg)](https://www.nuget.org/packages/Mud.Feishu.Abstractions/)
[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.Redis.svg)](https://www.nuget.org/packages/Mud.Feishu.Redis/)

**完整的 HTTP API、WebSocket 实时事件订阅和 Webhook 事件处理解决方案**

[快速开始](#-快速开始) • [架构说明](#-项目架构) • [功能特性](#-核心功能) • [使用示例](#-使用示例) • [文档](#-详细文档)

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

## 🏗️ 项目架构

### 整体架构图

```mermaid
graph TB
    %% 简化的节点命名
    App["业务应用<br/>(Web/移动/微服务)"]
    Core["Mud.Feishu<br/>核心API客户端"]
    WebSocket["Mud.Feishu.WebSocket<br/>实时事件"]
    Webhook["Mud.Feishu.Webhook<br/>HTTP回调"]
    RedisExt["Mud.Feishu.Redis<br/>分布式去重"]
    Abstraction["Mud.Feishu.Abstractions<br/>事件抽象层"]
    Feishu[飞书开放平台]
    Config[配置]
    Logging[日志]

    %% 主要关系
    App --> Core
    App --> WebSocket
    App --> Webhook
    Webhook --> RedisExt

    %% SDK内部依赖
    Core --> Abstraction
    WebSocket --> Abstraction
    Webhook --> Abstraction
    RedisExt --> Abstraction

    %% 通信关系
    Core --> Feishu
    WebSocket --> Feishu
    Feishu --> Webhook

    %% 公共依赖
    Config -.-> Core
    Config -.-> WebSocket
    Config -.-> Webhook
    Config -.-> RedisExt

    Core -.-> Logging
    WebSocket -.-> Logging
    Webhook -.-> Logging
    RedisExt -.-> Logging

    %% 布局调整
    classDef app fill:#f9f,stroke:#333,stroke-width:2px;
    classDef sdk fill:#bbf,stroke:#333,stroke-width:2px;
    classDef infra fill:#bfb,stroke:#333,stroke-width:2px;

    class App app;
    class Core,WebSocket,Webhook,RedisExt,Abstraction sdk;
    class Feishu,Config,Logging infra;
```

### 模块功能对比

| 模块                     | 核心功能      | 通信方式         | 实时性        | 适用场景           |
| ------------------------ | ------------- | ---------------- | ------------- | ------------------ |
| **Mud.Feishu**           | HTTP API 调用 | HTTP 请求        | 低 (主动查询) | 数据查询、操作管理 |
| **Mud.Feishu.WebSocket** | 实时事件订阅  | WebSocket 长连接 | 高 (实时推送) | 实时通知、即时响应 |
| **Mud.Feishu.Webhook**   | HTTP 回调处理 | HTTP 回调        | 中 (被动接收) | 事件触发、异步处理 |
| **Mud.Feishu.Redis**     | 分布式去重    | Redis            | -             | 多实例部署、防重复 |

---

## 📦 项目概览

| 组件                        | 描述                                                           | NuGet                                                                                                                           | 下载                                                                  |
| --------------------------- | -------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------- |
| **Mud.Feishu.Abstractions** | 事件订阅抽象层，提供策略模式和工厂模式的事件处理架构           | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Abstractions.svg)](https://www.nuget.org/packages/Mud.Feishu.Abstractions/) | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.Abstractions.svg) |
| **Mud.Feishu**              | 核心 HTTP API 客户端库，支持组织架构、消息、群聊等完整飞书功能 | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.svg)](https://www.nuget.org/packages/Mud.Feishu/)                           | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.svg)              |
| **Mud.Feishu.WebSocket**    | 飞书 WebSocket 客户端，支持实时事件订阅和自动重连              | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.WebSocket.svg)](https://www.nuget.org/packages/Mud.Feishu.WebSocket/)       | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.WebSocket.svg)    |
| **Mud.Feishu.Webhook**      | 飞书 Webhook 事件处理组件，支持 HTTP 回调事件接收和处理        | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Webhook.svg)](https://www.nuget.org/packages/Mud.Feishu.Webhook/)           | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.Webhook.svg)      |
| **Mud.Feishu.Redis**        | Redis 分布式去重扩展，支持多实例部署场景的事件去重             | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Redis.svg)](https://www.nuget.org/packages/Mud.Feishu.Redis/)               | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.Redis.svg)        |

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

## 🎯 核心功能

### 🏛️ Mud.Feishu.Abstractions - 事件处理抽象层

**统一的事件处理架构，WebSocket 和 Webhook 共享相同的处理器接口**

```mermaid
graph LR
    A[事件源] --> B{事件类型}
    B -->|用户事件| C[UserEventHandler]
    B -->|部门事件| D[DepartmentEventHandler]
    B -->|消息事件| E[MessageEventHandler]
    B -->|未知事件| F[DefaultEventHandler]
```

| 功能特性       | 说明                       |
| -------------- | -------------------------- |
| **策略模式**   | 可扩展的事件处理器架构     |
| **工厂模式**   | 动态注册和发现处理器       |
| **类型安全**   | 强类型数据模型，编译时检查 |
| **自动去重**   | 内置事件 ID 去重机制       |
| **事件拦截器** | 支持事件处理前后的拦截逻辑 |
| **基类处理器** | 简化开发的专用基类         |

**支持的基类处理器**：

- `DepartmentCreatedEventHandler` - 部门创建
- `DepartmentDeleteEventHandler` - 部门删除
- `DefaultFeishuEventHandler<T>` - 通用处理器

**新增工具类**：

- `UrlValidator` - URL 白名单验证和 SSRF 防护
- `HttpRetryPolicyBuilder` - HTTP 重试策略构建器（支持指数退避和抖动）

### 🌐 Mud.Feishu - HTTP API 客户端

**完整的飞书 API 覆盖，自动令牌管理**

| 模块分类        | API版本 | 主要功能                                   |
| --------------- | ------- | ------------------------------------------ |
| **🔐 认证授权** | V3      | 应用令牌、租户令牌、用户令牌、OAuth 2.0、多应用管理 |
| **👥 组织架构** | V1/V3   | 用户、部门、员工、用户组、职级、职务、角色 |
| **💬 消息服务** | V1      | 文本/图片/卡片消息、批量发送、群聊管理     |
| **📋 审批流程** | V4      | 审批定义、审批实例、审批任务、审批消息、审批统计 |
| **📝 任务管理** | V2      | 任务创建、更新、分组、附件、评论、自定义字段 |
| **📅 日程会议** | V4      | 日程事件、会议管理                         |
| **📄 文档管理** | V1      | 飞书文档、文档块、内容转换、知识库        |
| **📚 知识库**   | V2      | 知识空间、节点管理、节点复制移动          |
| **☁️ 云盘管理** | V1      | 云空间、文件夹、文件上传、版本管理        |
| **⏰ 考勤管理** | V1      | 考勤组、打卡记录、请假审批、考勤统计      |

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

### 🔄 Mud.Feishu.WebSocket - 实时事件订阅

**基于 WebSocket 长连接的实时事件推送，支持智能连接管理和错误分类处理**

```mermaid
sequenceDiagram
    participant Client as 你的应用
    participant WS as Mud.Feishu.WebSocket
    participant Feishu as 飞书服务器

    Client->>WS: 1. 订阅事件
    WS->>Feishu: 2. 建立 WebSocket 连接
    Feishu-->>WS: 3. 认证成功
    loop 实时推送
        Feishu-->>WS: 4. 事件消息
        WS->>WS: 5. 路由到处理器
        WS->>Client: 6. 处理完成
    end
```

| 功能分类     | 特性                                       |
| ------------ | ------------------------------------------ |
| **连接管理** | 自动重连、心跳检测、连接监控、错误分类处理 |
| **事件处理** | 策略模式、多处理器并行、事件重放           |
| **消息类型** | ping/pong、heartbeat、event、auth          |
| **监控运维** | 连接状态、处理统计、健康检查、审计日志     |

**错误分类处理**：

- ✅ **可恢复错误** - 网络波动、临时故障等
- ✅ **不可恢复错误** - 认证失败、权限不足等
- ✅ **详细的错误日志和错误类型标识** - 帮助快速定位问题

**认证失败处理**：

- ✅ **按错误码分类认证失败原因**
- ✅ **统计总失败次数和失败时间**
- ✅ **提供针对性修复建议**

**性能指标监控**：

- ✅ **连接数统计** - 实时 WebSocket 连接数
- ✅ **事件处理指标** - 认证、事件处理计数和耗时
- ✅ **内置 Meter 支持** - 集成 .NET 性能计数器

### 🌐 Mud.Feishu.Webhook - HTTP 回调事件处理

**基于中间件模式的事件接收和分发，具备企业级安全防护和性能优化**

```mermaid
sequenceDiagram
    participant Feishu as 飞书服务器
    participant Webhook as Mud.Feishu.Webhook
    participant Middleware as 中间件
    participant Handler as 事件处理器

    Feishu->>Middleware: 1. POST /feishu/{AppKey}
    Middleware->>Middleware: 2. 验证签名
    Middleware->>Middleware: 3. 解密内容
    Middleware->>Webhook: 4. 路由事件
    Webhook->>Handler: 5. 调用处理器
    Handler-->>Middleware: 6. 处理完成
    Middleware-->>Feishu: 7. 返回响应
```

| 功能分类     | 特性                                                                 |
| ------------ | -------------------------------------------------------------------- |
| **安全验证** | 签名验证、时间戳验证、AES-256-CBC 解密、IP 白名单、Content-Type 验证、SSRF 防护、URL 白名单验证 |
| **事件处理** | 中间件模式、自动路由、策略模式、异步处理、事件拦截器、失败事件重试   |
| **高级功能** | 多机器人支持、后台处理、并发控制（支持热更新）、断路器模式           |
| **监控运维** | 性能监控、健康检查、请求日志、异常处理、安全审计日志               |
| **安全加固** | 滑动窗口限流、威胁检测、安全审计、密钥验证、JSON 深度限制、私网 IP 检测 |
| **性能优化** | 流式请求体读取、源生成器序列化、内存管理优化、信号量并发控制         |

**安全增强特性**：

- ✅ **Content-Type 验证** - 仅接受 `application/json` 请求
- ✅ **JSON 深度限制** - 防止深度嵌套 JSON 导致 DoS 攻击
- ✅ **流式请求体读取** - 防止伪造 Content-Length 的 DoS 攻击
- ✅ **Nonce 过期清理** - 防止内存泄漏
- ✅ **断路器模式** - 使用 Polly 实现熔断机制
- ✅ **失败事件重试** - 后台自动重试失败事件
- ✅ **SSRF 防护** - 自动检测和阻止内网 IP 访问请求
- ✅ **URL 白名单验证** - 支持配置允许的 URL 域名和路径
- ✅ **私网 IP 检测** - 自动识别 127.0.0.1、192.168.x.x 等内网地址
- ✅ **安全审计日志** - 记录所有安全相关事件（成功/失败）

**性能优化**：

- ✅ **源生成器序列化** - 提升序列化性能约 20-30%
- ✅ **限流内存管理** - LRU 淘汰机制，最大 10 万条目
- ✅ **日志脱敏** - 自动脱敏敏感字段防止信息泄露
- ✅ **信号量并发控制** - 使用 SemaphoreSlim 控制最大并发数，支持配置热更新
- ✅ **HTTP 重试策略** - 智能指数退避和抖动算法

**核心服务**：

- `FeishuWebhookConcurrencyService` - 并发控制服务，支持配置热更新
- `FailedEventRetryService` - 失败事件重试服务，后台自动重试
- `SecurityAuditService` - 安全审计服务，记录安全事件
- `ThreatDetectionService` - 威胁检测服务，识别异常请求模式
- `LoggingEventInterceptor` - 日志事件拦截器
- `TelemetryEventInterceptor` - 遥测事件拦截器

---

## 💡 快速开始示例

### HTTP API 调用

```csharp
// 创建用户
[HttpPost("users")]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    _userApi.UseApp("hr-app");// 多应用场景下切换到 hr-app 应用，单应用场景下可省略
    var result = await _userApi.CreateUserAsync(request);
    _userApi.UseDefaultApp();// 多应用场景下切换回默认应用, 单应用场景下可省略
    return result.Code == 0 ? Ok(result.Data) : BadRequest(result.Msg);
}

// 构造函数注入IFeishuAppManager接口。
private readonly IFeishuAppManager _feishuAppManager;

// 使用IFeishuAppManager获取API接口对象，灵活切换飞书应用。
var tenantJobTitleApi = _feishuAppManager.GetFeishuApi<IFeishuTenantV3JobTitle>("hr-app");
var result = await tenantJobTitleApi.GetJobTitlesListAsync(10, null);

// 使用应用上下文切换器
var contextSwitcher = _feishuAppManager.GetAppContextSwitcher();
using (contextSwitcher.UseApp("hr-app"))
{
    // 此范围内的所有 API 调用都使用 hr-app 应用
    var userApi = _feishuAppManager.GetFeishuApi<IFeishuTenantV3User>();
    var userResult = await userApi.GetUserInfoByIdAsync("user_123");
}

// 单应用模式下发送消息，无须应用。
var textContent = new MessageTextContent { Text = "Hello World!" };
var result = await messageApi.SendMessageAsync(new SendMessageRequest
{
    ReceiveId = "user_123",
    MsgType = "text",
    Content = JsonSerializer.Serialize(textContent)
}, receive_id_type: "user_id");
```

### WebSocket 事件处理

```csharp
// 实现事件处理器
public class MessageHandler : IFeishuEventHandler
{
    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        var messageEvent = JsonSerializer.Deserialize<MessageReceiveEvent>(
            eventData.Event?.ToString() ?? "{}");

        Console.WriteLine($"收到消息: {messageEvent.Message.Content}");
    }
}

// 注册处理器
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<MessageHandler>()
    .Build();
```

### Webhook 事件处理

```csharp
// 部门创建事件处理器（继承基类）
public class DepartmentCreatedHandler : DepartmentCreatedEventHandler
{
    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        DepartmentCreatedResult? departmentData,
        CancellationToken cancellationToken = default)
    {
        // 同步到本地数据库
        await SyncToDatabaseAsync(departmentData);
    }
}

// 注册处理器
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<DepartmentCreatedHandler>()
    .Build();

// 添加中间件
app.UseFeishuWebhook();
```

### 性能指标监控

```csharp
// 获取 WebSocket 实时连接数
var connectionCountProvider = app.Services.GetRequiredService<IWebSocketConnectionCountProvider>();
var connectionCount = await connectionCountProvider.GetConnectionCountAsync();

// 使用 FeishuMetrics 记录自定义指标
FeishuMetrics.RecordTokenRefresh("default", true);
FeishuMetrics.RecordHttpRequest("default", "user.get", 200, TimeSpan.FromMilliseconds(150));
```

### URL 白名单和 SSRF 防护

```csharp
// 配置 URL 白名单
var options = new FeishuWebhookOptions
{
    SsrfProtection = new SsrfProtectionOptions
    {
        Enabled = true,
        BlockPrivateIps = true,
        AllowList = new[]
        {
            "https://open.feishu.cn",
            "https://*.example.com"
        }
    }
};

// 验证 URL
UrlValidator.ValidateBaseUrl("https://open.feishu.cn/api", true);
```

### 事件拦截器

```csharp
// 创建日志拦截器
public class CustomLoggingInterceptor : LoggingEventInterceptor
{
    public CustomLoggingInterceptor(ILogger<CustomLoggingInterceptor> logger)
        : base(logger)
    {
    }

    protected override Task LogBeforeHandleAsync(
        string eventType,
        string? eventId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始处理事件: {EventType}, EventId: {EventId}", eventType, eventId);
        return Task.CompletedTask;
    }
}

// 注册拦截器
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddInterceptor<CustomLoggingInterceptor>()
    .AddHandler<DepartmentCreatedHandler>()
    .Build();
```

---

## 📸 演示界面展示

以下是 **FeishuWikiManager**（飞书知识库管理 Demo）的实际运行界面截图，展示了 SDK 在实际项目中的应用效果：

### 用户认证与登录

| 飞书 OAuth 授权 | 系统登录界面 |
|:--|:--|
| ![飞书认证授权](./Images/wiki飞书认证授权界面.png) | ![登录界面](./Images/飞书云文档管理登陆界面.png) |

### 知识库管理核心功能

| 主界面 | 知识空间 |
|:--|:--|
| ![知识库主界面](./Images/Wiki知识库主界面.png) | ![知识空间](./Images/Wiki知识空间界面.png) |

| 搜索功能 | 云空间同步 |
|:--|:--|
| ![搜索界面](./Images/Wiki知识库搜索界面.png) | ![云空间同步](./Images/飞书云文档管理云空间同步功能.png) |

### 云文档管理

| 文档管理主界面 | 文件上传 |
|:--|:--|
| ![文档管理主界面](./Images/飞书云文档管理主界面.png) | ![文件上传](./Images/飞书云文档管理文件上传界面.png) |

> 💡 **提示**：以上界面均基于 **Mud.Feishu** SDK 开发，完整展示了飞书 OAuth 认证、知识库管理、文档搜索、云空间同步等核心功能。[查看 Demo 源码](./Demos/FeishuWikiManager)

---

## 📖 详细文档

- [Mud.Feishu.Abstractions 详细文档](./Mud.Feishu.Abstractions/README.md) - 事件处理抽象层使用指南
- [Mud.Feishu 详细文档](./Mud.Feishu/README.md) - HTTP API 完整使用指南
- [Mud.Feishu.WebSocket 详细文档](./Mud.Feishu.WebSocket/Readme.md) - WebSocket 实时事件订阅指南
- [Mud.Feishu.Webhook 详细文档](./Mud.Feishu.Webhook/README.md) - Webhook HTTP 回调事件处理指南
- [Mud.Feishu.Redis 详细文档](./Mud.Feishu.Redis/README.md) - Redis 分布式去重扩展指南
- [安全增强文档](./docs/SECURITY_IMPROVEMENTS.md) - SSRF 防护、URL 验证等安全特性详解

---

## 🛠️ 技术栈

### 框架支持

- **.NET Standard 2.0** - 兼容 .NET Framework 4.6.1+
- **.NET 6.0** - LTS 长期支持版本
- **.NET 8.0** - LTS 长期支持版本（推荐）
- **.NET 10.0** - LTS 长期支持版本

### 核心依赖

| 包                                            | 版本             | 说明                  |
| --------------------------------------------- | ---------------- | --------------------- |
| **Mud.ServiceCodeGenerator**                  | v1.4.6           | HTTP 客户端代码生成器 |
| **System.Text.Json**                          | v10.0.1          | 高性能 JSON 序列化    |
| **Microsoft.Extensions.Http**                 | v8.0.1 / v10.0.1 | HTTP 客户端工厂       |
| **Microsoft.Extensions.Http.Polly**           | v8.0.2 / v10.0.1 | 弹性和瞬态故障处理    |
| **Microsoft.Extensions.DependencyInjection**  | v8.0.2 / v10.0.1 | 依赖注入              |
| **Microsoft.Extensions.Logging**              | v8.0.3 / v10.0.1 | 日志记录              |
| **Microsoft.Extensions.Configuration.Binder** | v8.0.2 / v10.0.1 | 配置绑定              |

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
