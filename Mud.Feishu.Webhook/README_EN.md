# Mud.Feishu.Webhook

A webhook component for Feishu event subscription and handling, providing complete Feishu event receiving, validation, decryption, and distribution functionality.

**🚀 New Feature: Minimal API** - Complete service registration with one line of code, ready to use!

[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.Webhook.svg)](https://www.nuget.org/packages/Mud.Feishu.Webhook/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE-MIT)

## Features

- ✅ **Minimal API**: Complete service registration with one line of code, ready to use
- ✅ **Flexible Configuration**: Supports configuration files, code configuration, and builder pattern
- ✅ **Automatic Event Routing**: Automatically distributes events to corresponding handlers based on event type
- ✅ **Security Validation**: Supports event subscription validation, request signature validation, and timestamp validation
- ✅ **Encryption/Decryption**: Built-in AES-256-CBC decryption, automatically handles Feishu encrypted events
- ✅ **Middleware Mode**: Uses .NET standard middleware mode, simple integration
- ✅ **Dependency Injection**: Fully integrated with .NET dependency injection container
- ✅ **Exception Handling**: Comprehensive exception handling and logging
- ✅ **Performance Monitoring**: Optional performance metrics collection and monitoring
- ✅ **Health Checks**: Built-in health check endpoint
- ✅ **Async Processing**: Fully async event handling mechanism
- ✅ **Concurrency Control**: Configurable concurrent event processing limit with hot reload support
- ✅ **Distributed Support**: Provides distributed deduplication interface, supports Redis and other external storage
- ✅ **Configuration Hot Reload**: Supports runtime configuration changes without service restart
- ✅ **Rate Limiting**: Built-in sliding window rate limiting middleware to prevent malicious requests
- ✅ **Multi-App Support**: Supports multiple Feishu apps sharing the same Webhook endpoint
- ✅ **Background Processing**: Supports async background processing to avoid Feishu timeout retries
- ✅ **Security Hardening**: Enhanced IP validation, signature validation, and encryption key security checks
- ✅ **Cross-Platform**: Supports .NET Standard 2.0, .NET 6.0, .NET 8.0, .NET 10.0

## Quick Start

### 1. Install NuGet Package

```bash
dotnet add package Mud.Feishu.Webhook
```

### 2. Minimal Configuration (One Line)

In `Program.cs`:

```csharp
using Mud.Feishu.Webhook;
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// One line to register Webhook service (requires at least one event handler)
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();

// Add Feishu Webhook rate limit middleware (optional, recommended for production)
app.UseFeishuRateLimit();

// Add Feishu Webhook middleware
app.UseFeishuWebhook();

app.Run();
```

> 💡 **Note**: Webhook service uses middleware mode, automatically registering endpoints via `app.UseFeishuWebhook()`. Default route is `/feishu/{AppKey}`, where `{AppKey}` is the application key.
>
> ⚠️ **Important**: Rate limit middleware should be registered before Webhook middleware to ensure rate limiting policies are correctly applied.

### 3. Complete Configuration (Add Multiple Event Handlers)

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register multiple event handlers
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()      // Message receive event
    .AddHandler<DepartmentCreatedEventHandler>()   // Department created event
    .AddHandler<DepartmentUpdateEventHandler>()    // Department update event
    .AddHandler<DepartmentDeleteEventHandler>()    // Department delete event
    .Build();

var app = builder.Build();

// Add Feishu Webhook middleware
app.UseFeishuWebhook();

app.Run();
```

### 4. Configuration File (appsettings.json)

```json
{
  "FeishuWebhook": {
    "GlobalRoutePrefix": "feishu",
    "AutoRegisterEndpoint": true,
    "EnableRequestLogging": true,
    "EnableExceptionHandling": true,
    "EventHandlingTimeoutMs": 30000,
    "MaxConcurrentEvents": 10,
    "EnablePerformanceMonitoring": false,
    "AllowedHttpMethods": ["POST"],
    "MaxRequestBodySize": 10485760,
    "AllowedSourceIPs": [],
    "EnforceHeaderSignatureValidation": true,
    "TimestampToleranceSeconds": 30,
    "NonceValidationFailureMode": "Reject",
    "EnableBackgroundProcessing": false,
    "Retry": {
      "EnableRetry": false,
      "MaxRetryCount": 3,
      "InitialRetryDelaySeconds": 10,
      "RetryDelayMultiplier": 2.0,
      "MaxRetryDelaySeconds": 300,
      "RetryPollIntervalSeconds": 30,
      "MaxRetryPerPoll": 10
    },
    "RateLimit": {
      "EnableRateLimit": false,
      "WindowSizeSeconds": 60,
      "MaxRequestsPerWindow": 100,
      "EnableIpRateLimit": true,
      "TooManyRequestsStatusCode": 429,
      "TooManyRequestsMessage": "Too many requests, please try again later",
      "WhitelistIPs": ["127.0.0.1", "::1"]
    },
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

## 🏗️ Service Registration Methods

### 🚀 Method 1: Register from Configuration File (Recommended)

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Read configuration from appsettings.json
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook();
app.Run();
```

### ⚙️ Method 2: Code Configuration

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure via code
builder.Services.CreateFeishuWebhookServiceBuilder(options =>
{
    options.GlobalRoutePrefix = "feishu";
    options.EnableRequestLogging = true;
    options.EnableExceptionHandling = true;
    options.MaxConcurrentEvents = 10;
})
.AddHandler<MessageEventHandler>()
.Build();

var app = builder.Build();
app.UseFeishuWebhook();
app.Run();
```

**Note**: For multi-application scenarios, configure each application's `VerificationToken` and `EncryptKey` in the `Apps` dictionary of `FeishuWebhookOptions`, or set them via configuration file.

### 🔌 Method 3: Add Event Interceptors

```csharp
using Mud.Feishu.Webhook.Extensions;
using Mud.Feishu.Abstractions.Interceptors;

var builder = WebApplication.CreateBuilder(args);

// Add built-in and custom interceptors
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddInterceptor<LoggingEventInterceptor>()  // Built-in logging interceptor
    .AddInterceptor<TelemetryEventInterceptor>()  // Built-in telemetry interceptor
    .AddInterceptor<AuditLogInterceptor>()  // Custom audit interceptor
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook();
app.Run();
```

### 🔧 Method 4: Advanced Builder Pattern

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Use builder pattern for complex configuration
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .EnableHealthChecks()    // Enable health checks
    .AddHandler<MessageReceiveEventHandler>()
    .AddHandler<DepartmentCreatedEventHandler>()
    .Build();

var app = builder.Build();

// Add health check endpoint
app.MapHealthChecks("/health");

app.UseFeishuWebhook();
app.Run();
```

### 🔥 Method 5: Specify Configuration Section Name

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Read from custom configuration section
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration, "MyFeishuConfig")
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook();
app.Run();
```

## Usage Modes

### Middleware Mode

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook(); // Automatically handles requests under route prefix
app.Run();
```

> 💡 **Note**: Webhook service currently only supports middleware mode. Customize the global route prefix by configuring `GlobalRoutePrefix`.

## Creating Event Handlers

### Method 1: Implement IFeishuEventHandler Interface

```csharp
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;
using System.Text.Json;

public class MessageReceiveEventHandler : IFeishuEventHandler
{
    private readonly ILogger<MessageReceiveEventHandler> _logger;

    public MessageReceiveEventHandler(ILogger<MessageReceiveEventHandler> logger)
    {
        _logger = logger;
    }

    // Specify supported event type
    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received message event: EventId={EventId}, EventType={EventType}",
            eventData.EventId, eventData.EventType);

        // Handle message logic
        var messageData = JsonSerializer.Deserialize<MessageEventData>(
            eventData.Event?.ToString() ?? string.Empty);

        // Your business logic...
        _logger.LogInformation("Processing message: {MessageId}", messageData?.MessageId);

        await Task.CompletedTask;
    }
}

public class MessageEventData
{
    public string MessageId { get; set; }
    public string Content { get; set; }
    // ... other fields
}
```

### Method 2: Inherit Base Handler Class (Recommended)

Use base handler classes from `Mud.Feishu.Abstractions.EventHandlers` namespace for type safety and automatic deduplication:

```csharp
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.DataModels.Organization;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;

/// <summary>
/// Department created event handler
/// </summary>
public class DemoDepartmentEventHandler : DepartmentCreatedEventHandler
{
    private readonly DemoEventService _eventService;

    public DemoDepartmentEventHandler(
        IFeishuEventDeduplicator businessDeduplicator,
        ILogger<DemoDepartmentEventHandler> logger,
        DemoEventService eventService)
        : base(businessDeduplicator, logger)
    {
        _eventService = eventService;
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        DepartmentCreatedResult? eventEntity,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing department created event: DepartmentId={DepartmentId}, Name={DepartmentName}",
            eventEntity.DepartmentId, eventEntity.Name);

        // Your business logic
        await _eventService.RecordDepartmentEventAsync(eventEntity, cancellationToken);

        // Initialize department permissions
        _logger.LogInformation("Initializing department permissions: {DepartmentName}", eventEntity.Name);

        // Notify department leader
        if (!string.IsNullOrWhiteSpace(eventEntity.LeaderUserId))
        {
            _logger.LogInformation("Notifying department leader: {LeaderUserId}", eventEntity.LeaderUserId);
        }
    }
}
```

### Available Base Event Handlers

- `DepartmentCreatedEventHandler` - Department created event
- `DepartmentUpdateEventHandler` - Department update event
- `DepartmentDeleteEventHandler` - Department delete event
- More handlers available in `Mud.Feishu.Abstractions.EventHandlers` namespace

## Configuration Options

### Basic Configuration

| Option                 | Type   | Default  | Description                                        |
| ---------------------- | ------ | -------- | -------------------------------------------------- |
| `VerificationToken`    | string | -        | Feishu event subscription verification token       |
| `EncryptKey`           | string | -        | Feishu event encryption key (32 bytes)             |
| `GlobalRoutePrefix`    | string | "feishu" | Global route prefix (base path shared by all apps) |
| `AutoRegisterEndpoint` | bool   | true     | Whether to auto-register endpoint                  |

### Multi-App Configuration

| Option                                           | Type                                          | Default | Description                                                    |
| ------------------------------------------------ | --------------------------------------------- | ------- | -------------------------------------------------------------- |
| `Apps`                                           | Dictionary\<string, FeishuAppWebhookOptions\> | {}      | App configurations (AppKey -> AppConfig)                       |
| `Apps.{AppKey}.AppKey`                           | string                                        | -       | Application key (alphanumeric, underscore, hyphen, 1-64 chars) |
| `Apps.{AppKey}.VerificationToken`                | string                                        | -       | App verification token                                         |
| `Apps.{AppKey}.EncryptKey`                       | string                                        | -       | App encryption key (32 bytes)                                  |
| `Apps.{AppKey}.Description`                      | string?                                       | null    | App description (optional)                                     |
| `Apps.{AppKey}.TimestampToleranceSeconds`        | int                                           | -1      | Timestamp tolerance (-1 inherits global)                       |
| `Apps.{AppKey}.EventHandlingTimeoutMs`           | int                                           | -1      | Event handling timeout (-1 inherits global)                    |
| `Apps.{AppKey}.EnforceHeaderSignatureValidation` | bool?                                         | null    | Enforce header signature validation (null inherits global)     |
| `Apps.{AppKey}.EnableExceptionHandling`          | bool?                                         | null    | Enable exception handling (null inherits global)               |
| `Apps.{AppKey}.EnablePerformanceMonitoring`      | bool?                                         | null    | Enable performance monitoring (null inherits global)           |

### Security Configuration

| Option                             | Type              | Default  | Description                                                        |
| ---------------------------------- | ----------------- | -------- | ------------------------------------------------------------------ |
| `AllowedSourceIPs`                 | HashSet\<string\> | []       | Allowed source IP addresses (IP validation enabled when non-empty) |
| `AllowedHttpMethods`               | HashSet\<string\> | ["POST"] | Allowed HTTP methods                                               |
| `MaxRequestBodySize`               | long              | 10MB     | Max request body size                                              |
| `EnforceHeaderSignatureValidation` | bool              | true     | Whether to enforce header signature validation                     |
| `TimestampToleranceSeconds`        | int               | 30       | Timestamp validation tolerance (seconds)                           |
| `NonceValidationFailureMode`       | NonceFailureMode  | Reject  | Nonce dedup service unavailable fallback (Reject=security-first, Allow=availability-first) |

### Performance Configuration

| Option                        | Type | Default | Description                                                                                                 |
| ----------------------------- | ---- | ------- | ----------------------------------------------------------------------------------------------------------- |
| `MaxConcurrentEvents`         | int  | 10      | Max concurrent events, supports hot reload                                                                  |
| `EventHandlingTimeoutMs`      | int  | 30000   | Event handling timeout (milliseconds)                                                                       |
| `EnablePerformanceMonitoring` | bool | false   | Whether to enable performance monitoring                                                                    |
| `EnableBackgroundProcessing`  | bool | false   | Whether to enable background processing mode (activates token auto-refresh background service when enabled) |

### Logging Configuration

| Option                    | Type | Default | Description                          |
| ------------------------- | ---- | ------- | ------------------------------------ |
| `EnableRequestLogging`    | bool | true    | Whether to enable request logging    |
| `EnableExceptionHandling` | bool | true    | Whether to enable exception handling |

### Rate Limiting Configuration

| Option                                | Type              | Default                                     | Description                               |
| ------------------------------------- | ----------------- | ------------------------------------------- | ----------------------------------------- |
| `RateLimit.EnableRateLimit`           | bool              | false                                       | Whether to enable rate limiting           |
| `RateLimit.WindowSizeSeconds`         | int               | 60                                          | Time window size (seconds)                |
| `RateLimit.MaxRequestsPerWindow`      | int               | 100                                         | Max requests per time window              |
| `RateLimit.EnableIpRateLimit`         | bool              | true                                        | Whether to enable IP-based rate limiting  |
| `RateLimit.TooManyRequestsStatusCode` | int               | 429                                         | Status code when rate limit exceeded      |
| `RateLimit.TooManyRequestsMessage`    | string            | "Too many requests, please try again later" | Message when rate limit exceeded          |
| `RateLimit.WhitelistIPs`              | HashSet\<string\> | []                                          | Whitelist IPs (exempt from rate limiting) |

## Advanced Features

### Multi-App Support

Support multiple Feishu apps sharing the same Webhook endpoint:

```json
{
  "FeishuWebhook": {
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

Each app's route will be automatically registered as `/feishu/{AppKey}`.

### Rate Limiting

Built-in sliding window rate limiting middleware to prevent malicious requests:

```json
{
  "FeishuWebhook": {
    "RateLimit": {
      "EnableRateLimit": true,
      "WindowSizeSeconds": 60,
      "MaxRequestsPerWindow": 100,
      "EnableIpRateLimit": true,
      "WhitelistIPs": ["127.0.0.1", "::1"]
    }
  }
}
```

**Multi-App Support**: Rate limiting is based on `(AppKey, IP)` dimension, so requests from different apps won't affect each other.

**Usage**:

```csharp
var app = builder.Build();

// Add Feishu Webhook rate limit middleware (optional, recommended for production)
app.UseFeishuRateLimit();

// Add Feishu Webhook middleware
app.UseFeishuWebhook();

app.Run();
```

### Background Processing Mode

Enable background processing mode to avoid Feishu timeout retries:

```json
{
  "FeishuWebhook": {
    "EnableBackgroundProcessing": true
  }
}
```

```csharp
// After enabling background processing mode, middleware returns success response immediately
// Then processes events asynchronously in the background, suitable for long-running business logic
builder.Services.CreateFeishuWebhookServiceBuilder(options =>
{
    options.EnableBackgroundProcessing = true;
}).AddHandler<LongRunningEventHandler>()
    .Build();
```

### Concurrency Control Hot Reload

Support runtime dynamic adjustment of concurrency limits without service restart:

```csharp
// Configuration changes are automatically applied with hot reload support
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

// Modify MaxConcurrentEvents value in configuration file at runtime
// System will automatically detect and apply new concurrency limits
```

## Event Interceptors

Event interceptors allow executing custom logic before and after event handling, such as logging, metrics collection, permission verification, etc.

### Built-in Interceptors

**LoggingEventInterceptor** - Record event handling logs

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddInterceptor<LoggingEventInterceptor>()  // Record event handling start and end
    .AddHandler<MessageEventHandler>()
    .Build();
```

**TelemetryEventInterceptor** - Telemetry data collection

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddInterceptor<TelemetryEventInterceptor>(sp =>
        new TelemetryEventInterceptor("My.Application"))  // Specify application name
    .AddHandler<MessageEventHandler>()
    .Build();
```

### Custom Interceptors

Create custom interceptors by implementing the `IFeishuEventInterceptor` interface:

```csharp
using Mud.Feishu.Abstractions;

/// <summary>
/// Audit log interceptor example
/// </summary>
public class AuditLogInterceptor : IFeishuEventInterceptor
{
    private readonly ILogger<AuditLogInterceptor> _logger;

    public AuditLogInterceptor(ILogger<AuditLogInterceptor> logger)
        => _logger = logger;

    /// <summary>
    /// Before event handling interceptor
    /// </summary>
    /// <returns>Return false to interrupt event handling flow</returns>
    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Audit] Event started: {EventType}, EventId: {EventId}, TenantKey: {TenantKey}",
            eventType, eventData.EventId, eventData.TenantKey);
        return Task.FromResult(true); // Return true to continue, false to interrupt
    }

    /// <summary>
    /// After event handling interceptor
    /// </summary>
    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        if (exception == null)
        {
            _logger.LogInformation("[Audit] Event succeeded: {EventType}, EventId: {EventId}", eventType, eventData.EventId);
        }
        else
        {
            _logger.LogError(exception, "[Audit] Event failed: {EventType}, EventId: {EventId}", eventType, eventData.EventId);
        }
        return Task.CompletedTask;
    }
}
```

### Register Custom Interceptors

```csharp
// Type registration
.AddInterceptor<AuditLogInterceptor>()

// Factory registration
.AddInterceptor(sp => new AuditLogInterceptor(
    sp.GetRequiredService<ILogger<AuditLogInterceptor>>()))

// Instance registration
var interceptor = new AuditLogInterceptor(logger);
.AddInterceptor(interceptor)
```

### Interceptor Execution Order

Interceptors execute in registration order, complete flow:

```
Webhook Event Arrives
    ↓
Interceptor 1: BeforeHandleAsync
    ↓
Interceptor 2: BeforeHandleAsync
    ↓
...
    ↓
Interceptor N: BeforeHandleAsync
    ↓
[Event Handler Handles Event]
    ↓
Interceptor N: AfterHandleAsync
    ↓
...
    ↓
Interceptor 2: AfterHandleAsync
    ↓
Interceptor 1: AfterHandleAsync
    ↓
Handling Complete
```

### Use Cases

- **Logging**: Record event handling start, success, and failure
- **Metrics Collection**: Statistics on event processing time, success rate, etc.
- **Security Audit**: Record handling of sensitive events
- **Permission Control**: Decide whether to handle based on event type or content
- **Performance Monitoring**: Record processing time to identify performance bottlenecks
- **Business Tracing**: Write event information to audit logs or tracing systems

## Registering Event Handlers

### Register Multiple Handlers with Chain Calls

```csharp
using Mud.Feishu.Webhook.Extensions;

builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()      // Message receive
    .AddHandler<DepartmentCreatedEventHandler>()   // Department created
    .AddHandler<DepartmentUpdateEventHandler>()    // Department update
    .AddHandler<DepartmentDeleteEventHandler>()    // Department delete
    .Build();
```

### Register with Factory Method

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<MessageEventHandler>>();
        var myService = sp.GetRequiredService<MyCustomService>();
        return new MessageEventHandler(logger, myService);
    })
    .Build();
```

### Register with Instance

```csharp
var handler = new MessageEventHandler(loggerFactory.CreateLogger<MessageEventHandler>());

builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler(handler)
    .Build();
```

## Supported Event Types

This library supports all Feishu Open Platform event types. Common event types include:

### Message Events

- `im.message.receive_v1` - Receive message event
- `im.message.recalled_v1` - Message recalled event

### Group Chat Events

- `im.chat.member_user.added_v1` - User joins group chat
- `im.chat.member_user.withdrawn_v1` - User leaves group chat
- `im.chat.disbanded_v1` - Group chat disbanded
- `im.chat.updated_v1` - Group info updated

### Contact Events

- `contact.user.created_v3` - Employee onboarded
- `contact.user.updated_v3` - Employee info updated
- `contact.user.deleted_v3` - Employee offboarded
- `contact.department.created_v3` - Department created
- `contact.department.updated_v3` - Department info updated
- `contact.department.deleted_v3` - Department deleted

### Approval Events

- `approval.approval.approved_v1` - Approval approved
- `approval.approval.rejected_v1` - Approval rejected
- `approval.approval.updated_v1` - Approval updated

### Task Events

- `task.task.created_v1` - Task created
- `task.task.updated_v1` - Task updated

> 💡 **Tip**: For more event types, please refer to [Feishu Open Platform Event List](https://open.feishu.cn/document/ukTMukTMukTM/uUTNz4SN1MjL1UzM)

## Feishu Platform Configuration

### 1. Create Event Subscription

1. Log in to Feishu Open Platform
2. Go to your application details page
3. Click "Event Subscription"
4. Configure request URL: `https://your-domain.com/feishu/Webhook`
5. Set verification token and encryption key

### 2. Configure Event Types

Select the event types you want to subscribe to:

- Message events
- Group chat events
- User events
- Department events
- etc...

### 3. Publish Application

After configuration is complete, publish the application, and Feishu servers will start pushing events to your endpoint.

## Security Model and Validation Flow

### Signature Algorithm

This component uses **SHA-256** header signature validation (consistent with the official Feishu SDK Python/Go), not HMAC-SHA256.

Signature string format: `SHA-256(timestamp + nonce + encryptKey + body)`

The signature is passed via the `X-Lark-Signature` request header in lowercase hexadecimal format.

### Validation Order

`CompositeFeishuEventValidator` executes validation in the following order; any failed step rejects the request:

```
1. Timestamp Validation
   └─ Check if request timestamp is within tolerance window (replay attack time window)

2. Nonce Check (check only, do not mark)
   └─ Check if Nonce has already been used (intercept replay attacks)
   └─ Note: This step does NOT mark the Nonce, preventing premature consumption on signature failure

3. Signature Validation
   └─ Validate X-Lark-Signature header signature (SHA-256)
   └─ Use fixed-time comparison to prevent timing attacks

4. Nonce Mark (mark after signature passes)
   └─ After signature validation passes, mark Nonce as used
   └─ Safe to mark: no risk of premature consumption on signature failure
   └─ Concurrency: if already marked by another request during marking, reject
```

### Two-Step Nonce Validation Design

Nonce validation is split into "check" (`CheckNonceAsync`) and "mark" (`TryMarkNonceAsUsedAsync`) steps:

- **Check step** (before signature validation): Only checks if Nonce exists, does not mark. Intercepts used Nonces (replay attack prevention)
- **Mark step** (after signature validation): Marks Nonce as used only after signature passes. Ensures Nonce is not consumed on signature failure, allowing Feishu to safely retry

| Scenario | Before (old logic) | After (new logic) |
|----------|-------------------|-------------------|
| Signature failure | ❌ Nonce consumed, Feishu retry rejected (event permanently lost) | ✅ Nonce not consumed, Feishu can safely retry |
| Signature success | ✅ Normal marking | ✅ Normal marking |
| Replay attack | ✅ Intercepted | ✅ Intercepted (check step still intercepts used Nonces) |

### Security Hardening Measures

| Measure | Description |
|---------|-------------|
| Fixed-time comparison | Token and signature comparison uses `FixedTimeEquals` to prevent timing attacks |
| Production enforcement | Production environment auto-detects and rejects disabled signature validation |
| Sensitive data masking | Token, EncryptKey and other sensitive fields are automatically masked in logs |
| Request body size limit | Prevents oversized request body DoS attacks |
| IP whitelist | Supports configurable source IP address list (CIDR format) |
| Rate limiting | Sliding window rate limiting based on `(AppKey, IP)` dimension |

## Monitoring and Diagnostics

### Performance Monitoring

Performance monitoring is implemented through the interceptor mechanism, supporting OpenTelemetry and custom metrics collection:

```csharp
// Add telemetry interceptor (OpenTelemetry integration)
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddInterceptor<TelemetryEventInterceptor>()
    .AddHandler<MessageEventHandler>()
    .Build();

// Or use custom performance monitoring interceptor
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddInterceptor<PerformanceMonitoringInterceptor>()
    .AddHandler<MessageEventHandler>()
    .Build();
```

### Health Checks

Built-in health check support to monitor Webhook service status:

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Enable health checks
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .EnableHealthChecks()    // Enable health checks
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();

// Add health check endpoint
app.MapHealthChecks("/health");

app.UseFeishuWebhook();
app.Run();
```

Health check configuration options:

```json
{
  "FeishuWebhook": {
    "HealthCheckUnhealthyFailureRateThreshold": 0.1, // Unhealthy threshold (10%)
    "HealthCheckDegradedFailureRateThreshold": 0.05, // Degraded threshold (5%)
    "HealthCheckMinEventsThreshold": 10 // Minimum events count
  }
}
```

### Logging

This library uses the standard .NET logging framework with flexible log level configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Mud.Feishu.Webhook": "Debug",
      "Mud.Feishu.Webhook.Services": "Debug",
      "Mud.Feishu.Webhook.Middleware": "Information",
      "Mud.Feishu.Abstractions": "Information"
    }
  }
}
```

### Diagnostics Endpoint (Demo Example)

The Demo project provides diagnostics endpoints to view registered event handlers:

```csharp
// Use in Demo project
app.MapDiagnostics();  // Register diagnostics endpoint

// Visit /diagnostics/handlers to view all registered handlers
```

## Best Practices

### 1. Error Handling

Handle exceptions properly in event handlers to avoid affecting other events:

```csharp
public class RobustEventHandler : IFeishuEventHandler
{
    private readonly ILogger<RobustEventHandler> _logger;

    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting to process event: {EventId}", eventData.EventId);

            // Your business logic
            await ProcessBusinessLogicAsync(eventData, cancellationToken);

            _logger.LogInformation("Event processing completed: {EventId}", eventData.EventId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Event processing was cancelled: {EventId}", eventData.EventId);
            throw; // Timeout cancellation should be thrown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling event: {EventId}", eventData.EventId);
            // Do not rethrow exceptions to avoid affecting other handlers
            // Optionally log to failure queue or alert system
        }
    }

    private async Task ProcessBusinessLogicAsync(EventData eventData, CancellationToken cancellationToken)
    {
        // Actual business logic
        await Task.CompletedTask;
    }
}
```

### 2. Async Processing and Cancellation Support

Correctly use async programming and cancellation tokens:

```csharp
public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
{
    // ✅ Correct: Use async APIs and pass cancellation token
    await ProcessMessageAsync(eventData, cancellationToken);
    await SaveToDatabaseAsync(eventData, cancellationToken);

    // ❌ Wrong: Don't use blocking calls
    // var result = ProcessMessageAsync(eventData).Result;
    // ProcessMessageAsync(eventData).Wait();

    // ✅ Correct: Respect cancellation token
    cancellationToken.ThrowIfCancellationRequested();
}
```

### 3. Dependency Injection

Use dependency injection properly, ensuring correct service lifetimes:

```csharp
public class MessageEventHandler : IFeishuEventHandler
{
    private readonly ILogger<MessageEventHandler> _logger;
    private readonly IMessageService _messageService;  // Scoped service
    private readonly IConfiguration _configuration;    // Singleton service

    public MessageEventHandler(
        ILogger<MessageEventHandler> logger,
        IMessageService messageService,
        IConfiguration configuration)
    {
        _logger = logger;
        _messageService = messageService;
        _configuration = configuration;
    }

    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        // Use injected services
        await _messageService.ProcessAsync(eventData, cancellationToken);
    }
}
```

### 4. Use Base Handler Classes (Recommended)

Inherit base handler classes for automatic deduplication and type safety:

```csharp
using Mud.Feishu.Abstractions.EventHandlers;

// Inherit base handler class for automatic deduplication and type conversion
public class MyDepartmentHandler : DepartmentCreatedEventHandler
{
    public MyDepartmentHandler(
        IFeishuEventDeduplicator deduplicator,
        ILogger<MyDepartmentHandler> logger)
        : base(deduplicator, logger)
    {
    }

    // Only need to implement business logic
    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        DepartmentCreatedResult eventEntity,
        CancellationToken cancellationToken = default)
    {
        // eventEntity is already a strongly-typed entity object
        _logger.LogInformation("Processing department: {Name}", eventEntity.Name);
    }
}
```

### 5. Configuration Validation

Validate configuration at startup to catch issues early:

```csharp
// Configuration is automatically validated during Build()
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();  // Configuration is validated here

// Will throw InvalidOperationException if configuration is invalid
```

### 6. Long-Running Tasks

For time-consuming tasks, enable background processing mode:

```csharp
// appsettings.json
{
  "FeishuWebhook": {
    "EnableBackgroundProcessing": true,  // Return success immediately, process in background
    "EventHandlingTimeoutMs": 60000      // Increase timeout duration
  }
}
```

### 7. Testing and Debugging

The Demo project provides test endpoints for debugging:

```csharp
// In Demo project
app.MapTestEndpoints();        // Test endpoints
app.MapDiagnostics();          // Diagnostics endpoints

// Available endpoints:
// POST /test/capture - Capture raw requests
// GET /test/captured - View captured requests
// GET /diagnostics/handlers - View registered handlers
```

## Troubleshooting

### Common Issues

1. **Validation Failed**
   - Check if `VerificationToken` is correct
   - Confirm request URL is configured correctly

2. **Decryption Failed**
   - Check if `EncryptKey` is correct
   - Confirm encryption is enabled on Feishu platform

3. **Signature Validation Failed**
   - Check time synchronization
   - Confirm request hasn't been modified by proxy server
   - Ensure `EnforceHeaderSignatureValidation` is set to true in production

4. **Event Handling Failed**
   - Check if event handlers are correctly registered
   - View detailed error information in logs

5. **Distributed Deployment Event Duplication**
   - Default uses in-memory deduplication, multi-instance deployment requires distributed deduplication
   - Refer to `IFeishuNonceDistributedDeduplicator` interface for custom Redis implementation

6. **Timeout Handling**
   - Check if `EventHandlingTimeoutMs` configuration is reasonable
   - Ensure event handling logic supports cancellation tokens

7. **Rate Limiting Issues**
   - Check `RateLimit.EnableRateLimit` configuration
   - Confirm client IP is in whitelist
   - Adjust `MaxRequestsPerWindow` and `WindowSizeSeconds` parameters

8. **Multi-App Configuration Issues**
   - Check if `Apps` configuration is correct
   - Confirm AppKey, VerificationToken, and EncryptKey for each app
   - Verify app routes are correct (`/feishu/{AppKey}`)

### Debugging Tips

```csharp
// Enable verbose logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Enable request logging and performance monitoring
builder.Services.CreateFeishuWebhookServiceBuilder(options =>
{
    options.EnableRequestLogging = true;
    options.EnablePerformanceMonitoring = true;
    options.RateLimit.EnableRateLimit = true; // Enable rate limiting debugging
}).AddHandler<MessageEventHandler>()
    .Build();
```

## Complete Examples

### Basic Example

Complete Program.cs example:

```csharp
using Mud.Feishu.Webhook.Extensions;
using Mud.Feishu.Webhook.Demo.Handlers;
using Mud.Feishu.Webhook.Demo.Services;

var builder = WebApplication.CreateBuilder(args);

// Register custom services
builder.Services.AddSingleton<DemoEventService>();

// Register Feishu Webhook service
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration, "FeishuWebhook")
    .AddHandler<DemoDepartmentEventHandler>()
    .AddHandler<DemoDepartmentDeleteEventHandler>()
    .AddHandler<DemoDepartmentUpdateEventHandler>()
    .Build();

var app = builder.Build();

// Add Feishu Webhook middleware
app.UseFeishuWebhook();

app.Run();
```

### Advanced Example

Including health checks, performance monitoring, and custom endpoints:

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Register Feishu Webhook service (advanced configuration)
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .EnableHealthChecks()    // Enable health checks
    .AddHandler<MessageReceiveEventHandler>()
    .AddHandler<DepartmentCreatedEventHandler>()
    .Build();

var app = builder.Build();

// Health check endpoint
app.MapHealthChecks("/health");

// Feishu Webhook middleware
app.UseFeishuWebhook();

app.Run();
```

### Complete Demo Project Example

The Demo project provides complete testing and diagnostics functionality:

```csharp
using Mud.Feishu.Webhook.Demo.Handlers;
using Mud.Feishu.Webhook.Demo.Services;
using Mud.Feishu.Webhook.Extensions;
using Mud.Feishu.Webhook.Demo;

var builder = WebApplication.CreateBuilder(args);

// Register demo services
builder.Services.AddSingleton<DemoEventService>();

// Register Feishu Webhook service
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration, "FeishuWebhook")
    .AddHandler<DemoDepartmentEventHandler>()
    .AddHandler<DemoDepartmentDeleteEventHandler>()
    .AddHandler<DemoDepartmentUpdateEventHandler>()
    .Build();

var app = builder.Build();

// Add diagnostics endpoints (development environment only)
if (app.Environment.IsDevelopment())
{
    app.MapDiagnostics();      // GET /diagnostics/handlers
    app.MapTestEndpoints();    // POST /test/capture etc.
}

// Add Feishu Webhook middleware
app.UseFeishuWebhook();

app.Run();
```

## Quick Reference

### Most Common Code Patterns

```csharp
// ✅ Recommended: Read from configuration file (multi-app config)
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<YourEventHandler>()
    .Build();

// ✅ Code configuration (configure each app's Token and Key in Apps)
builder.Services.CreateFeishuWebhookServiceBuilder(options => {
    options.GlobalRoutePrefix = "feishu";
})
.AddHandler<YourEventHandler>()
.Build();

// ✅ Advanced configuration
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .EnableHealthChecks()
    .AddHandler<Handler1>()
    .AddHandler<Handler2>()
    .Build();
```

### Common Configuration Quick Reference

| Configuration                 | Default            | Description                                               |
| ----------------------------- | ------------------ | --------------------------------------------------------- |
| `RoutePrefix`                 | `"feishu/Webhook"` | Webhook route prefix                                      |
| `VerificationToken`           | -                  | Verification token (required)                             |
| `EncryptKey`                  | -                  | Encryption key (32 bytes)                                 |
| `MaxConcurrentEvents`         | `10`               | Max concurrent events                                     |
| `EventHandlingTimeoutMs`      | `30000`            | Event handling timeout (ms)                               |
| `EnableBackgroundProcessing`  | `false`            | Background processing mode (activates token auto-refresh) |
| `EnablePerformanceMonitoring` | `false`            | Performance monitoring                                    |

---

**🚀 Get started with Feishu Webhook now and build a stable, reliable event handling system!**
