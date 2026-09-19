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
- ⚡ **Native AOT Support** - First-class Native AOT publishing on net8.0+, with source-generated JSON serialization and configuration binding across the entire pipeline (zero IL warnings in strict mode)

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
      "TimeoutSeconds": 30,
      "HttpRetry": {
        "MaxAttempts": 3,
        "DelayMs": 1000
      },
      "IsDefault": true
    }
  ],
  "FeishuWebSocket": {
    "Reconnect": {
      "Auto": true,
      "MaxAttempts": 5,
      "MaxAuthRetryAttempts": 5,
      "BaseDelayMs": 5000,
      "MaxDelayMs": 30000,
      "TotalBudget": "00:30:00",
      "Cooldown": "00:00:05"
    },
    "Certificate": {
      "Mode": "Strict",
      "AllowInsecureWebSocket": false,
      "ValidateServerCertificate": true,
      "AllowSelfSignedCertificates": false,
      "AllowCertificateNameMismatch": false
    },
    "HeartbeatIntervalMs": 25000,
    "ConnectionTimeoutMs": 10000,
    "HealthCheckIntervalMs": 60000,
    "MessageHandlerTimeoutMs": 30000,
    "AllowedHostSuffixes": "*.feishu.cn;*.larksuite.com",
    "EventDeduplication": {
      "Mode": "InMemory",
      "CacheExpiration": "2.00:00:00",
      "CleanupInterval": "00:05:00"
    }
  },
  "FeishuWebhook": {
    "GlobalRoutePrefix": "feishu",
    "MaxConcurrentEvents": 10,
    "EnforceHeaderSignatureValidation": true,
    "TimestampToleranceSeconds": 30,
    "Apps": {
      "default": {
        "AppKey": "cli_xxx",
        "VerificationToken": "your_verification_token",
        "EncryptKey": "your_encrypt_key_32_bytes_long"
      }
    }
  }
}
```

<details>
<summary>📋 WebSocket Advanced Configuration Reference</summary>

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Reconnect.MaxDelayMs` | int | 30000 | Maximum reconnection delay (ms) |
| `Reconnect.TotalBudget` | TimeSpan | 30min | Maximum total reconnection time |
| `Reconnect.Cooldown` | TimeSpan | 5s | Cooldown between reconnection attempts |
| `EnableReconnectMetrics` | bool | true | Enable reconnection metrics collection |
| `ConnectionTimeoutMs` | int | 10000 | Connection timeout (ms) |
| `InitialReceiveBufferSize` | int | 4096 | Initial receive buffer size (bytes) |
| `Certificate.Mode` | enum | Strict | Certificate validation mode: Strict / Dev / Custom |
| `Certificate.ValidateServerCertificate` | bool | true | Validate SSL certificates |
| `Certificate.AllowSelfSignedCertificates` | bool | false | Allow self-signed certificates |
| `Certificate.AllowCertificateNameMismatch` | bool | false | Allow certificate name mismatch |
| `Certificate.AllowInsecureWebSocket` | bool | false | Allow insecure ws:// connections (dev/test only) |
| `AllowedHostSuffixes` | string | `*.feishu.cn;*.larksuite.com` | Host allow-list (`*.` wildcard suffixes or exact hosts, `;`-separated); empty = unrestricted |
| `HealthCheckIntervalMs` | int | 60000 | Health check interval (ms) |
| `MessageHandlerTimeoutMs` | int | 30000 | Per-message processing timeout (ms), 0 disables the limit |
| `SequenceGapThreshold` | ulong | 0 | Message sequence gap threshold, 0 disables gap detection |
| `MessageSizeLimits` | object | 1MB / 10MB | Max text (chars) / binary (bytes) message size |
| `EventDeduplication` | object | InMemory | Event deduplication (`Mode`/`CacheExpiration`/`CleanupInterval`) |
| `RejectEmptyEventIds` | bool | true | Reject events with empty EventId (fail-closed, WHF-05 aligned) |
| `IgnoreUnknownEventTypes` | bool | false | Silently ignore unregistered event types (recommended true; default false for compatibility) |

> ℹ️ **Migration note**: the legacy `TokenRefreshInterval` / `TokenRefreshAhead` options have been removed. Token refresh is now controlled by `FeishuAppConfig.TokenRefreshThreshold` (HTTP layer); the WebSocket connection reuses the same app token manager and needs no extra configuration.

> 💡 For more details, see [Mud.Feishu.WebSocket Documentation](./Mud.Feishu.WebSocket/Readme.md)

</details>

### 3️⃣ Service Registration (Program.cs)

```csharp
using Mud.Feishu;
using Mud.Feishu.WebSocket;
using Mud.Feishu.Webhook;

var builder = WebApplication.CreateBuilder(args);

// Register multi-application mode (Option 1: Load from configuration file)
builder.Services.AddFeishuApp(builder.Configuration);

// Register multi-application mode (Option 2: Code configuration)
builder.Services.AddFeishuApp(configure =>
{
    configure.AddDefaultApp("default", "cli_xxx", "dsk_xxx");
    configure.AddApp("hr-app", "cli_yyy", "dsk_yyy", opt =>
    {
        opt.TimeoutSeconds = 45;
        opt.HttpRetry.MaxAttempts = 5;
    });
});

// Register multi-application mode (Option 3: Use pre-built configuration list)
var configs = new List<FeishuAppConfig>
{
    new FeishuAppConfig { AppKey = "default", AppId = "cli_xxx", AppSecret = "dsk_xxx", IsDefault = true },
    new FeishuAppConfig { AppKey = "hr-app", AppId = "cli_yyy", AppSecret = "dsk_yyy" }
};
builder.Services.AddFeishuApp(configs);

// Register HTTP API services (lazy mode - register all APIs)
builder.Services.CreateFeishuServicesBuilder()
    .AddAllApis()
    .Build();

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
var tenantJobTitleApi = _feishuAppManager.GetWebApi<IFeishuTenantV3JobTitle>("hr-app");
var result = await tenantJobTitleApi.GetJobTitlesListAsync(10, null);

// Scope-based app switching with automatic context restoration (BeginScope)
var userApi = _feishuAppManager.GetDefaultWebApi<IFeishuTenantV3User>();
using (userApi.BeginScope("hr-app"))
{
    // All API calls within this scope use hr-app
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
        var messageEvent = JsonSerializer.Deserialize<MessageReceiveResult>(
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
// Department creation event handler (inherit the generated base handler)
public class DepartmentCreatedHandler : DepartmentCreatedEventHandler
{
    public DepartmentCreatedHandler(
        IFeishuEventDeduplicator businessDeduplicator,
        ILogger<DepartmentCreatedHandler> logger)
        : base(businessDeduplicator, logger)
    {
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        DepartmentCreatedResult? departmentData,
        FeishuEventHeader? header,
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
// The SDK publishes metrics on the "Mud.Feishu" meter (FeishuMetrics), e.g.:
// - feishu.websocket.connections  (observable gauge, per app_key) - real-time WebSocket connection count
// - feishu.event.handling / feishu.event.handling.duration - event processing count and latency
// - feishu.websocket.reconnect / feishu.websocket.message.duration - WebSocket health
// - feishu.webhook.request / feishu.webhook.request.duration - webhook traffic
// HTTP requests (mud.http.requests) and token refreshes (mud.token.refresh) are recorded
// automatically by the underlying Mud.HttpUtils component.
//
// Subscribe with a MeterListener or any OpenTelemetry exporter to consume them:
using var listener = new MeterListener();
listener.InstrumentPublished = (instrument, meterListener) =>
{
    var enable = instrument.Meter.Name == FeishuMetrics.MeterName;
    if (enable)
    {
        meterListener.EnableMeasurementEvents(instrument);
    }
    return enable;
};
listener.Start();
```

### URL Whitelist and SSRF Protection

```csharp
// Webhook security options (signature, replay window, IP whitelist)
var options = new FeishuWebhookOptions
{
    // Force validation of the X-Lark-Signature header (recommended in production)
    EnforceHeaderSignatureValidation = true,
    // Replay window tolerance (seconds), max 300
    TimestampToleranceSeconds = 30,
    // Optional source IP whitelist (supports CIDR, e.g. "192.168.1.0/24")
    AllowedSourceIPs = new HashSet<string> { "10.0.0.0/8" }
};

// Base URL whitelist validation (SSRF protection, provided by Mud.HttpUtils)
UrlValidator.ConfigureAllowedDomains(["open.feishu.cn", "open.larksuite.com"]);
UrlValidator.ValidateBaseUrl("https://open.feishu.cn/api", allowCustomBaseUrl: false);
```

### Event Interceptors

```csharp
// Register the built-in logging interceptor directly (constructor takes ILogger<LoggingEventInterceptor>)
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddInterceptor<LoggingEventInterceptor>()
    .AddHandler<DepartmentCreatedHandler>()
    .Build();

// Or implement IFeishuEventInterceptor for custom pre/post processing
public class AuditLogInterceptor : IFeishuEventInterceptor
{
    private readonly ILogger<AuditLogInterceptor> _logger;

    public AuditLogInterceptor(ILogger<AuditLogInterceptor> logger)
    {
        _logger = logger;
    }

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Start processing event: {EventType}, EventId: {EventId}", eventType, eventData.EventId);
        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Finished processing event: {EventType}, EventId: {EventId}", eventType, eventData.EventId);
        return Task.CompletedTask;
    }
}
```

---

## 🏢 Multi-Tenant Deployment Guide

> **Multi-tenant = Multi-app.** Each tenant maps to a `FeishuAppConfig` (independent AppId/AppSecret); switch the app context via `UseApp`/`BeginScope`.

**Minimal wiring snippet**:

```csharp
// 1. Register an app config per tenant
builder.Services.AddFeishuApp(configure =>
{
    configure.AddDefaultApp("tenant-a", "cli_aaa", "dsk_aaa");
    configure.AddApp("tenant-b", "cli_bbb", "dsk_bbb");
});

// 2. Register HTTP API services
builder.Services.CreateFeishuServicesBuilder()
    .AddAllApis()
    .Build();

// 3. Switch the app context per tenant in a request
public class TenantController : ControllerBase
{
    private readonly IFeishuAppManager _appManager;

    public TenantController(IFeishuAppManager appManager)
    {
        _appManager = appManager;
    }

    [HttpGet("tenant/{tenantKey}/users/{userId}")]
    public async Task<IActionResult> GetUser(string tenantKey, string userId)
    {
        var userApi = _appManager.GetDefaultWebApi<IFeishuTenantV3User>();
        // using ensures the default app is restored when the scope ends
        using var scope = userApi.BeginScope(tenantKey);
        var result = await userApi.GetUserInfoByIdAsync(userId);
        return Ok(result);
    }
}
```

> ⚠️ **Security note**: `UseApp`/`BeginScope` only switches the app context (token/endpoint); it does **NOT enforce tenant isolation authorization**.
> To restrict a caller to its own tenant's data, implement custom authorization in the business layer (e.g., a Claim-based tenant validation middleware).
> The component-side `IAppAccessAuthorizer` reports an error when it is missing, but this SDK does not bundle an authorization implementation.

> 🔒 **Multi-tenant Redis isolation**: when using Redis distributed deduplication, set distinct key prefixes
> (`EventKeyPrefix`/`NonceKeyPrefix`/`SeqIdKeyPrefix`) per tenant to prevent cross-tenant event conflicts.

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
- [Mud.Feishu Documentation](./Mud.Feishu/README.md) - HTTP API complete usage guide
- [Mud.Feishu.WebSocket Documentation](./Mud.Feishu.WebSocket/Readme_EN.md) - WebSocket real-time event subscription guide
- [Mud.Feishu.Webhook Documentation](./Mud.Feishu.Webhook/README_EN.md) - Webhook HTTP callback event processing guide
- [Mud.Feishu.Authentication Documentation](./Mud.Feishu.Authentication/README.md) - Feishu user authentication middleware guide
- [Mud.Feishu.Redis Documentation](./Mud.Feishu.Redis/README.md) - Redis distributed deduplication extension guide

## 🛠️ Technology Stack

### Framework Support

- **.NET Standard 2.0** - Compatible with .NET Framework 4.6.1+
- **.NET 6.0** - LTS long-term support version
- **.NET 8.0** - LTS long-term support version (recommended, Native AOT publishing supported)
- **.NET 10.0** - LTS long-term support version (recommended, Native AOT publishing supported)

## ⚡ Native AOT Support

Since 3.0.0, MudFeishu fully supports .NET **Native AOT** (first-class support on net8.0+ target frameworks) and can be published as self-contained single-file native binaries:

- **End-to-end source-generated JSON serialization** - every package ships built-in `JsonSerializerContext` types (`FeishuJsonContext`, `WebSocketJsonContext`, `EventCallbackJsonContext`, etc.), reflection-free serialization/deserialization at runtime
- **Source-generated configuration binding** - `EnableConfigurationBindingGenerator` is enabled so config DTOs (e.g. `FeishuAppConfig`) get compile-time binding code; config DTOs avoid the `required` modifier in favor of `Validate()` methods
- **Global AOT analyzer governance** - all projects on net8.0+ enable `IsAotCompatible` / `EnableAotAnalyzer` / `EnableTrimAnalyzer` / `TrimMode=full`, with the `FeishuJsonAot` AOT-safe serialization helper and rd.xml trim roots
- **Quality gate enforcement** - `verify-build.ps1` includes an AOT strict-mode smoke build (per-project `AotStrictMode` + `--no-incremental`) asserting a successful build and **0** `AOT00x` / `IL2026` / `IL3050` diagnostics
- **Dedicated verification project** - `Demos/Mud.Feishu.AotVerification` covers JSON serialization, HTTP clients, event handling, and WebSocket protocol messages under AOT (win-x64 / linux-x64 dual RID)

Publishing an AOT application:

```bash
dotnet publish -r win-x64 -c Release /p:PublishAot=true
```

> ⚠️ **Notes**:
> - Native AOT is only supported on net8.0+ target frameworks; AOT analyzers are not enabled for netstandard2.0 / net6.0
> - Register custom `JsonSerializerContext` types via entry points such as `FeishuJsonDefaults.ConfigureUserResolver` to avoid runtime reflection-based serialization
> - Do not mark extended config DTO properties with `required` (the source-generated binder constructs via `new T()`); perform validation in a `Validate()` method instead

### Core Dependencies

| Package                                       | Version          | Description                                           |
| --------------------------------------------- | ---------------- | ----------------------------------------------------- |
| **Mud.HttpUtils**                             | v2.0.7           | HTTP client utilities with source generator (incl. resilience policies) |
| **Mud.HttpUtils.Generator**                   | v2.0.7           | HTTP client code generator (compile-time)             |
| **System.Text.Json**                          | v10.0.9          | High-performance JSON serialization (netstandard2.0 target) |
| **Microsoft.Extensions.***                    | v8.0.2 / v10.0.9 | Dependency injection, logging, configuration binding, options |

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
