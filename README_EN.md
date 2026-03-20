# MudFeishu

<div align="center">

![MudFeishu Logo](icon.png)

Enterprise-Grade .NET SDK for Feishu (Lark) API Integration

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.svg)](https://www.nuget.org/packages/Mud.Feishu/)
[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.WebSocket.svg)](https://www.nuget.org/packages/Mud.Feishu.WebSocket/)
[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.Webhook.svg)](https://www.nuget.org/packages/Mud.Feishu.Webhook/)
[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.Abstractions.svg)](https://www.nuget.org/packages/Mud.Feishu.Abstractions/)
[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.Authentication.svg)](https://www.nuget.org/packages/Mud.Feishu.Authentication/)
[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.Redis.svg)](https://www.nuget.org/packages/Mud.Feishu.Redis/)

**Complete HTTP API, WebSocket Real-time Event Subscription, and Webhook Event Processing Solution**

[Quick Start](#-quick-start) • [Architecture](#-project-architecture) • [Features](#-core-features) • [Examples](#-quick-start-examples) • [Docs](#-detailed-documentation)

</div>

---

## 📖 Project Introduction

MudFeishu is a modern enterprise-grade .NET SDK for Feishu (Lark) API integration, providing comprehensive HTTP API calls, WebSocket real-time event subscription, and Webhook event processing capabilities. The SDK is designed using Strategy and Factory patterns with built-in automatic token management, intelligent retry mechanisms, and high-performance caching, significantly simplifying Feishu application development.

### ✨ Core Advantages

- 🚀 **Minimal API** - One-line service registration, ready to use out of the box
- 🏗️ **Type Safety** - Strongly-typed data models with compile-time type checking
- 🔄 **Automatic Token Management** - Smart caching and refresh, no manual maintenance required
- 🛡️ **Enterprise Stability** - Unified exception handling, intelligent retry, detailed logging
- 🎯 **Event-Driven** - Strategy pattern event processing, flexible extension
- 📊 **Multi-Framework Support** - .NET Standard 2.0, .NET 6.0, .NET 8.0, .NET 10.0

---

## 🏗️ Project Architecture

### Overall Architecture Diagram

```mermaid
graph TB
    subgraph "MudFeishu SDK Architecture"
        direction TB

        subgraph HTTP["HTTP API Client"]
            H1["User Management"]
            H2["Department"]
            H3["Message"]
            H4["Approval"]
        end

        subgraph Event["Event Processing Layer"]
            E1["WebSocket Client"]
            E2["Webhook Handler"]
            E3["Event Routing"]
            E4["Strategy Pattern"]
        end

        subgraph Ext["Extensions"]
            X1["Redis Dedup"]
            X2["Performance"]
            X3["Health Check"]
        end

        subgraph Core["Shared Core Layer"]
            C1["Token Management"]
            C2["HTTP Client Factory"]
            C3["Exception Handling"]
            C4["Configuration"]
        end

        HTTP --> Core
        Event --> Core
        Ext --> Core
        Core --> Platform["Feishu Open Platform API"]
    end
```

### Module Comparison

| Module                   | Core Features                | Communication             | Real-time                | Use Cases                                       |
| ------------------------ | ---------------------------- | ------------------------- | ------------------------ | ----------------------------------------------- |
| **Mud.Feishu**           | HTTP API calls               | HTTP Request              | Low (active query)       | Data query, management operations               |
| **Mud.Feishu.WebSocket** | Real-time event subscription | WebSocket Long Connection | High (real-time push)    | Real-time notifications, instant response       |
| **Mud.Feishu.Webhook**   | HTTP callback processing     | HTTP Callback             | Medium (passive receive) | Event trigger, async processing                 |
| **Mud.Feishu.Redis**     | Distributed deduplication    | Redis                     | -                        | Multi-instance deployment, duplicate prevention |

---

## 📦 Project Overview

| Component                   | Description                                                                                                           | NuGet                                                                                                                           | Downloads                                                             |
| --------------------------- | --------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------- |
| **Mud.Feishu.Abstractions** | Event subscription abstraction layer with Strategy and Factory pattern event handling architecture                    | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Abstractions.svg)](https://www.nuget.org/packages/Mud.Feishu.Abstractions/) | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.Abstractions.svg) |
| **Mud.Feishu**              | Core HTTP API client library with full Feishu capabilities including organization, messaging, and group chat features | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.svg)](https://www.nuget.org/packages/Mud.Feishu/)                           | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.svg)              |
| **Mud.Feishu.Authentication** | Feishu user authentication middleware with thread-safe user context management based on AsyncLocal | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Authentication.svg)](https://www.nuget.org/packages/Mud.Feishu.Authentication/) | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.Authentication.svg) |
| **Mud.Feishu.WebSocket**    | Feishu WebSocket client supporting real-time event subscription and automatic reconnection                            | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.WebSocket.svg)](https://www.nuget.org/packages/Mud.Feishu.WebSocket/)       | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.WebSocket.svg)    |
| **Mud.Feishu.Webhook**      | Feishu Webhook event handling component for HTTP callback event reception and processing                              | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Webhook.svg)](https://www.nuget.org/packages/Mud.Feishu.Webhook/)           | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.Webhook.svg)      |
| **Mud.Feishu.Redis**        | Redis distributed deduplication extension supporting event deduplication in multi-instance deployment scenarios       | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Redis.svg)](https://www.nuget.org/packages/Mud.Feishu.Redis/)               | ![Nuget](https://img.shields.io/nuget/dt/Mud.Feishu.Redis.svg)        |

---

## 🚀 Quick Start

### 1️⃣ Install NuGet Packages

```bash
# HTTP API Client (Core Module)
dotnet add package Mud.Feishu

# Event Processing Abstraction Layer (Optional, WebSocket/Webhook dependency)
dotnet add package Mud.Feishu.Abstractions

# WebSocket Real-time Event Subscription (Optional)
dotnet add package Mud.Feishu.WebSocket

# Webhook HTTP Callback Event Processing (Optional)
dotnet add package Mud.Feishu.Webhook

# User Authentication Middleware (Optional)
dotnet add package Mud.Feishu.Authentication

# Redis Distributed Deduplication Extension (Optional)
dotnet add package Mud.Feishu.Redis
```

> 💡 **Tip**: Install packages based on your needs. `Mud.Feishu` is the core package, and `Mud.Feishu.Abstractions` is automatically installed as a dependency of WebSocket and Webhook.

### 2️⃣ Configuration File (appsettings.json)

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
      "EnableLogging": true,
      "IsDefault": true
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
    "VerificationToken": "your_verification_token",
    "EncryptKey": "your_encrypt_key_32_bytes_long",
    "RoutePrefix": "feishu/webhook",
    "EnableRequestLogging": true,
    "MaxConcurrentEvents": 10
  }
}
```

### 3️⃣ Service Registration (Program.cs)

```csharp
using Mud.Feishu;
using Mud.Feishu.WebSocket;
using Mud.Feishu.Webhook;

var builder = WebApplication.CreateBuilder(args);

// Register multi-application mode (Option 1: Load from configuration file)
builder.Services.AddFeishuMultiApp(builder.Configuration);

// Register multi-application mode (Option 2: Code configuration)
builder.Services.AddFeishuMultiApp(configure =>
{
    config.AddDefaultApp("default", "cli_xxx", "dsk_xxx");
    config.AddApp("hr-app", "cli_yyy", "dsk_yyy", opt =>
    {
        opt.TimeOut = 45;
        opt.RetryCount = 5;
    });
});

// Register multi-application mode (Option 3: Use pre-built configuration list)
var configs = new List<FeishuAppConfig>
{
    new FeishuAppConfig { AppKey = "default", AppId = "cli_xxx", AppSecret = "dsk_xxx", IsDefault = true },
    new FeishuAppConfig { AppKey = "hr-app", AppId = "cli_yyy", AppSecret = "dsk_yyy" }
};
builder.Services.AddFeishuMultiApp(configs);

// Register HTTP API services (All services)
builder.Services.AddFeishuHttpClient();

// Or use builder pattern for selective registration
builder.Services.CreateFeishuServicesBuilder()
    .AddOrganizationApi()
    .AddMessageApi()
    .AddChatGroupApi()
    .Build();

// Register WebSocket Event Subscription
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

// Register Webhook HTTP Callback Event Service
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .AddHandler<DepartmentCreatedEventHandler>()
    .Build();

var app = builder.Build();

// Add Webhook Middleware
app.UseFeishuWebhook();

app.Run();
```

### 4️⃣ Verify Configuration

```csharp
// Test user information retrieval
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

## 🎯 Core Features

### 🏛️ Mud.Feishu.Abstractions - Event Processing Abstraction Layer

**Unified event processing architecture, WebSocket and Webhook share the same handler interface**

```mermaid
graph LR
    A[Event Source] --> B{Event Type}
    B -->|User Event| C[UserEventHandler]
    B -->|Department Event| D[DepartmentEventHandler]
    B -->|Message Event| E[MessageEventHandler]
    B -->|Unknown Event| F[DefaultEventHandler]
```

| Feature                | Description                                           |
| ---------------------- | ----------------------------------------------------- |
| **Strategy Pattern**   | Extensible event handler architecture                 |
| **Factory Pattern**    | Dynamic registration and discovery of handlers        |
| **Type Safety**        | Strongly-typed data models with compile-time checking |
| **Auto Deduplication** | Built-in event ID deduplication mechanism             |
| **Event Interceptors** | Support for pre/post event processing interception    |
| **Base Handlers**      | Specialized base classes to simplify development      |

**Supported Base Handlers**:

- `DepartmentCreatedEventHandler` - Department creation
- `DepartmentDeleteEventHandler` - Department deletion
- `DefaultFeishuEventHandler<T>` - Generic handler

**New Utility Classes**:

- `UrlValidator` - URL whitelist validation and SSRF protection
- `HttpRetryPolicyBuilder` - HTTP retry policy builder (supports exponential backoff and jitter)

### 🌐 Mud.Feishu - HTTP API Client

- `DepartmentCreatedEventHandler` - Department creation
- `DepartmentDeleteEventHandler` - Department deletion
- `DefaultFeishuEventHandler<T>` - Generic handler

### 🌐 Mud.Feishu - HTTP API Client

**Complete Feishu API coverage with automatic token management**

| Module Category       | API Version | Main Features                                                            |
| --------------------- | ----------- | ------------------------------------------------------------------------ |
| **🔐 Authentication** | V3          | App token, tenant token, user token, OAuth 2.0, multi-app management |
| **👥 Organization**   | V1/V3       | Users, departments, employees, user groups, job levels, positions, roles |
| **💬 Messaging**      | V1          | Text/image/card messages, batch sending, group chat management |
| **📋 Approvals**      | V4          | Approval definitions, instances, tasks, messages, statistics |
| **📝 Tasks**          | V2          | Task creation, updates, groups, attachments, comments, custom fields |
| **📅 Calendar**       | V4          | Calendar events, meeting management |
| **📄 Documents**      | V1          | Feishu docs, document blocks, content conversion, wiki |
| **📚 Wiki**           | V2          | Knowledge spaces, node management, node copy and move |
| **☁️ Drive**          | V1          | Cloud space, folders, file upload, version management |
| **⏰ Attendance**     | V1          | Attendance groups, check-in records, leave approval, statistics |

**Enterprise Features**:

- ✅ Automatic token caching and refresh
- ✅ Intelligent retry mechanism (configurable retry count and delay)
- ✅ High-performance caching (resolves cache stampede)
- ✅ Unified exception handling
- ✅ Connection pool management
- ✅ Detailed logging
- ✅ Multi-app context switching support
- ✅ Performance monitoring (built-in Meter metrics collection)

> 💡 **Tip**: [View complete API documentation](./Mud.Feishu/README.md)

### 🔄 Mud.Feishu.WebSocket - Real-time Event Subscription

**Real-time event push based on WebSocket long connection**

```mermaid
sequenceDiagram
    participant Client as Your App
    participant WS as Mud.Feishu.WebSocket
    participant Feishu as Feishu Server

    Client->>WS: 1. Subscribe to events
    WS->>Feishu: 2. Establish WebSocket connection
    Feishu-->>WS: 3. Auth successful
    loop Real-time push
        Feishu-->>WS: 4. Event message
        WS->>WS: 5. Route to handler
        WS->>Client: 6. Processing complete
    end
```

| Category                  | Features                                                            |
| ------------------------- | ------------------------------------------------------------------- |
| **Connection Management** | Auto reconnect, heartbeat detection, connection monitoring, error classification |
| **Event Processing**      | Strategy pattern, multi-handler parallel, event replay              |
| **Message Types**         | ping/pong, heartbeat, event, auth                                   |
| **Monitoring**            | Connection status, processing statistics, health checks, audit logs |

**Error Classification Handling**:

- ✅ **Recoverable Errors** - Network fluctuations, temporary failures, etc.
- ✅ **Non-recoverable Errors** - Authentication failure, insufficient permissions, etc.
- ✅ **Detailed Error Logs and Error Type Identification** - Helps quickly locate issues

**Authentication Failure Handling**:

- ✅ **Classify authentication failure reasons by error code**
- ✅ **Track total failure count and failure time**
- ✅ **Provide targeted repair suggestions**

**Performance Monitoring**:

- ✅ **Connection Statistics** - Real-time WebSocket connection count
- ✅ **Event Processing Metrics** - Authentication, event processing count and latency
- ✅ **Built-in Meter Support** - Integrated .NET performance counter

**Supported Event Types**:

- Message events: `im.message.receive_v1`
- User events: `contact.user.*_v3`
- Department events: `contact.department.*_v3`
- Approval events: `approval.approval.*_v1`

### 🌐 Mud.Feishu.Webhook - HTTP Callback Event Processing

**Event reception and distribution based on middleware mode**

```mermaid
sequenceDiagram
    participant Feishu as Feishu Server
    participant Webhook as Mud.Feishu.Webhook
    participant Middleware as Middleware
    participant Handler as Event Handler

    Feishu->>Middleware: 1. POST /feishu/webhook
    Middleware->>Middleware: 2. Verify signature
    Middleware->>Middleware: 3. Decrypt content
    Middleware->>Webhook: 4. Route event
    Webhook->>Handler: 5. Call handler
    Handler-->>Middleware: 6. Processing complete
    Middleware-->>Feishu: 7. Return response
```

| Category               | Features                                                                                   |
| ---------------------- | ------------------------------------------------------------------------------------------ |
| **Security**           | Signature verification, timestamp verification, AES-256-CBC decryption, IP whitelist, Content-Type validation, SSRF protection, URL whitelist |
| **Event Processing**   | Middleware mode, auto routing, strategy pattern, async processing, event interceptors, failed event retry |
| **Advanced**           | Multi-bot support, background processing, concurrency control (hot reload supported), circuit breaker pattern |
| **Monitoring**         | Performance monitoring, health checks, request logs, exception handling, security audit logs |
| **Security Hardening** | Sliding window rate limiting, threat detection, security audit, key validation, JSON depth limit, private IP detection |
| **Performance**        | Streaming request body reading, source generator serialization, memory optimization, semaphore concurrency control |

**Security Enhancement Features**:

- ✅ **Content-Type Validation** - Only accepts `application/json` requests
- ✅ **JSON Depth Limit** - Prevents DoS attacks from deeply nested JSON
- ✅ **Streaming Request Body Reading** - Prevents DoS attacks with forged Content-Length
- ✅ **Nonce Expiration Cleanup** - Prevents memory leaks
- ✅ **Circuit Breaker Pattern** - Implemented with Polly for circuit breaking
- ✅ **Failed Event Retry** - Background automatic retry of failed events
- ✅ **SSRF Protection** - Automatically detects and blocks private IP access requests
- ✅ **URL Whitelist Validation** - Supports configuring allowed URL domains and paths
- ✅ **Private IP Detection** - Automatically identifies 127.0.0.1, 192.168.x.x and other private addresses
- ✅ **Security Audit Logs** - Records all security-related events (success/failure)

**Performance Optimization**:

- ✅ **Source Generator Serialization** - Improves serialization performance by ~20-30%
- ✅ **Rate Limiter Memory Management** - LRU eviction mechanism, max 100k entries
- ✅ **Log Sanitization** - Automatically sanitizes sensitive fields to prevent information leakage
- ✅ **Semaphore Concurrency Control** - Uses SemaphoreSlim to control max concurrency, supports hot config reload
- ✅ **HTTP Retry Policy** - Intelligent exponential backoff and jitter algorithm

**Core Services**:

- `FeishuWebhookConcurrencyService` - Concurrency control service with hot config reload
- `FailedEventRetryService` - Failed event retry service, automatic background retry
- `SecurityAuditService` - Security audit service, records security events
- `ThreatDetectionService` - Threat detection service, identifies abnormal request patterns
- `LoggingEventInterceptor` - Logging event interceptor
- `TelemetryEventInterceptor` - Telemetry event interceptor

### 💾 Mud.Feishu.Redis - Distributed Deduplication Extension

**Distributed event deduplication based on Redis, suitable for multi-instance deployment**

| Category                    | Features                                             |
| --------------------------- | ---------------------------------------------------- |
| **Deduplication Mechanism** | EventId, Nonce, SeqID three deduplication dimensions |
| **Atomic Operations**       | SETNX + EXPIRE ensures atomicity                     |
| **Auto Expiration**         | Auto cleanup of expired data                         |
| **Distributed Support**     | Cluster mode, sentinel mode, TLS/SSL                 |
| **Flexible Config**         | Configurable expiration time, key prefix, timeout    |
| **Monitoring**              | Logging, cache statistics, health checks             |

---

## 📚 Usage Scenarios

| Scenario                  | Recommended Solution | Latency | Code Example |
| ------------------------- | -------------------- | ------- | ------------ |
| User Info Query           | Mud.Feishu           | Low     | HTTP API     |
| System Notification       | Mud.Feishu           | Low     | HTTP API     |
| Real-time Chatbot         | Mud.Feishu.WebSocket | High    | WebSocket    |
| Organization Sync         | Mud.Feishu.Webhook   | Medium  | Webhook      |
| Multi-instance Deployment | Mud.Feishu.Redis     | -       | Redis        |

---

## 💡 Quick Start Examples

### HTTP API Calls

```csharp
// Create user
[HttpPost("users")]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    _userApi.UseApp("hr-app");// Switch to hr-app in multi-app scenario, can be omitted in single-app scenario
    var result = await _userApi.CreateUserAsync(request);
    _userApi.UseDefaultApp();// Switch back to default app in multi-app scenario, can be omitted in single-app scenario
    return result.Code == 0 ? Ok(result.Data) : BadRequest(result.Msg);
}

// Constructor injection of IFeishuAppManager interface
private readonly IFeishuAppManager _feishuAppManager;

// Use IFeishuAppManager to get API interface objects and flexibly switch between Feishu apps
var tenantJobTitleApi = _feishuAppManager.GetFeishuApi<IFeishuTenantV3JobTitle>("hr-app");
var result = await tenantJobTitleApi.GetJobTitlesListAsync(10, null);

// Use app context switcher
var contextSwitcher = _feishuAppManager.GetAppContextSwitcher();
using (contextSwitcher.UseApp("hr-app"))
{
    // All API calls within this scope use hr-app
    var userApi = _feishuAppManager.GetFeishuApi<IFeishuTenantV3User>();
    var userResult = await userApi.GetUserInfoByIdAsync("user_123");
}

// Send message in single-app mode, no app switching needed
var textContent = new MessageTextContent { Text = "Hello World!" };
var result = await messageApi.SendMessageAsync(new SendMessageRequest
{
    ReceiveId = "user_123",
    MsgType = "text",
    Content = JsonSerializer.Serialize(textContent)
}, receive_id_type: "user_id");
```

### WebSocket Event Processing

```csharp
// Implement event handler
public class MessageHandler : IFeishuEventHandler
{
    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        var messageEvent = JsonSerializer.Deserialize<MessageReceiveEvent>(
            eventData.Event?.ToString() ?? "{}");

        Console.WriteLine($"Message received: {messageEvent.Message.Content}");
    }
}

// Register handler
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<MessageHandler>()
    .Build();
```

### Webhook Event Processing

```csharp
// Department creation event handler (inherit base class)
public class DepartmentCreatedHandler : DepartmentCreatedEventHandler
{
    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        DepartmentCreatedResult? departmentData,
        CancellationToken cancellationToken = default)
    {
        // Sync to local database
        await SyncToDatabaseAsync(departmentData);
    }
}

// Register handler
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<DepartmentCreatedHandler>()
    .Build();

// Add middleware
app.UseFeishuWebhook();
```

### Performance Monitoring

```csharp
// Get real-time WebSocket connection count
var connectionCountProvider = app.Services.GetRequiredService<IWebSocketConnectionCountProvider>();
var connectionCount = await connectionCountProvider.GetConnectionCountAsync();

// Use FeishuMetrics to record custom metrics
FeishuMetrics.RecordTokenRefresh("default", true);
FeishuMetrics.RecordHttpRequest("default", "user.get", 200, TimeSpan.FromMilliseconds(150));
```

### URL Whitelist and SSRF Protection

```csharp
// Configure URL whitelist
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

// Validate URL
UrlValidator.ValidateBaseUrl("https://open.feishu.cn/api", true);
```

### Event Interceptors

```csharp
// Create logging interceptor
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
        _logger.LogInformation("Start processing event: {EventType}, EventId: {EventId}", eventType, eventId);
        return Task.CompletedTask;
    }
}

// Register interceptor
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddInterceptor<CustomLoggingInterceptor>()
    .AddHandler<DepartmentCreatedHandler>()
    .Build();
```

---

## 📸 Demo Screenshots

Below are actual screenshots of **FeishuWikiManager** (Feishu Wiki Management Demo), demonstrating the SDK in a real project:

### User Authentication & Login

| Feishu OAuth | System Login |
|:--|:--|
| ![Feishu OAuth](./Images/wiki飞书认证授权界面.png) | ![Login](./Images/飞书云文档管理登陆界面.png) |

### Wiki Management Core Features

| Main Interface | Knowledge Space |
|:--|:--|
| ![Wiki Main](./Images/Wiki知识库主界面.png) | ![Knowledge Space](./Images/Wiki知识空间界面.png) |

| Search | Cloud Sync |
|:--|:--|
| ![Search](./Images/Wiki知识库搜索界面.png) | ![Cloud Sync](./Images/飞书云文档管理云空间同步功能.png) |

### Document Management

| Document Manager | File Upload |
|:--|:--|
| ![Doc Manager](./Images/飞书云文档管理主界面.png) | ![Upload](./Images/飞书云文档管理文件上传界面.png) |

> 💡 **Tip**: All interfaces are built with **Mud.Feishu** SDK, demonstrating Feishu OAuth, wiki management, document search, cloud sync and other core features. [View Demo Source](./Demos/FeishuWikiManager)

---

## 📖 Detailed Documentation

- [Mud.Feishu.Abstractions Documentation](./Mud.Feishu.Abstractions/README_EN.md) - Event processing abstraction layer guide
- [Mud.Feishu Documentation](./Mud.Feishu/README_EN.md) - HTTP API complete usage guide
- [Mud.Feishu.WebSocket Documentation](./Mud.Feishu.WebSocket/Readme_EN.md) - WebSocket real-time event subscription guide
- [Mud.Feishu.Webhook Documentation](./Mud.Feishu.Webhook/README_EN.md) - Webhook HTTP callback event processing guide
- [Mud.Feishu.Authentication Documentation](./Mud.Feishu.Authentication/README.md) - Feishu user authentication middleware guide
- [Mud.Feishu.Redis Documentation](./Mud.Feishu.Redis/README.md) - Redis distributed deduplication extension guide
- [Security Enhancements](./docs/SECURITY_IMPROVEMENTS.md) - SSRF protection, URL validation and other security features

## 🛠️ Technology Stack

### Framework Support

- **.NET Standard 2.0** - Compatible with .NET Framework 4.6.1+
- **.NET 6.0** - LTS long-term support version
- **.NET 8.0** - LTS long-term support version (recommended)
- **.NET 10.0** - LTS long-term support version

### Core Dependencies

| Package                                       | Version          | Description                             |
| --------------------------------------------- | ---------------- | --------------------------------------- |
| **Mud.ServiceCodeGenerator**                  | v1.4.6           | HTTP client code generator              |
| **System.Text.Json**                          | v10.0.1          | High-performance JSON serialization     |
| **Microsoft.Extensions.Http**                 | v8.0.1 / v10.0.1 | HTTP client factory                     |
| **Microsoft.Extensions.Http.Polly**           | v8.0.2 / v10.0.1 | Resilience and transient fault handling |
| **Microsoft.Extensions.DependencyInjection**  | v8.0.2 / v10.0.1 | Dependency injection                    |
| **Microsoft.Extensions.Logging**              | v8.0.3 / v10.0.1 | Logging                                 |
| **Microsoft.Extensions.Configuration.Binder** | v8.0.2 / v10.0.1 | Configuration binding                   |

---

## 📄 License

This project is licensed under the [MIT License](./LICENSE), allowing both commercial and non-commercial use.

---

## 🔗 Related Links

### 📖 Official Documentation

- [Feishu Open Platform Documentation](https://open.feishu.cn/document/) - Official Feishu API documentation and best practices
- [NuGet Package Manager](https://www.nuget.org/) - Official .NET package management platform

### 📦 NuGet Packages

- [Mud.Feishu.Abstractions](https://www.nuget.org/packages/Mud.Feishu.Abstractions/) - Event processing abstraction layer
- [Mud.Feishu](https://www.nuget.org/packages/Mud.Feishu/) - Core HTTP API client library
- [Mud.Feishu.WebSocket](https://www.nuget.org/packages/Mud.Feishu.WebSocket/) - WebSocket real-time event subscription library
- [Mud.Feishu.Webhook](https://www.nuget.org/packages/Mud.Feishu.Webhook/) - Webhook HTTP callback event processing library
- [Mud.Feishu.Authentication](https://www.nuget.org/packages/Mud.Feishu.Authentication/) - Feishu user authentication middleware library
- [Mud.Feishu.Redis](https://www.nuget.org/packages/Mud.Feishu.Redis/) - Redis distributed deduplication extension library

### 🛠️ Development Resources

- [Project Repository](https://gitee.com/mudtools/MudFeishu) - Source code and development documentation
- [Mud.ServiceCodeGenerator](https://gitee.com/mudtools/mud-code-generator) - HTTP client code generator
- [Example Projects](./Demos) - Complete usage examples and demo code
  - [FeishuWikiManager](./Demos/FeishuWikiManager) - Feishu Wiki Management Demo (Vue3 + .NET)
  - [Webhook Demo](./Demos/Mud.Feishu.Webhook.Demo) - Webhook event processing demo
  - [WebSocket Demo](./Demos/Mud.Feishu.WebSocket.Demo) - WebSocket real-time event demo
- [Test Projects](./Tests) - Complete unit tests and integration tests

### 🤝 Community Support

- [Issue Tracker](https://gitee.com/mudtools/MudFeishu/issues) - Bug reports and feature requests
- [Contributing Guide](./CONTRIBUTING.md) - How to contribute to the project
- [Changelog](./CHANGELOG.md) - Version updates and change notes

---

<div align="center">

**If MudFeishu helps you, please give us a ⭐Star to support us!**

Made with ❤️ by MudTools

</div>
