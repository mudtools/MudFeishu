# Feishu WebSocket Client Service

Enterprise-grade Feishu event subscription WebSocket client, providing reliable connection management, automatic reconnection, and strategy pattern event handling.

**🚀 New Feature: Minimal API** - Complete service registration with one line of code, ready to use!

## ✨ Core Features

- 🚀 **Minimal API** - Complete service registration with one line of code, ready to use
- 🔄 **Intelligent Connection Management** - Automatic reconnection, heartbeat detection, status monitoring
- 🫀 **Heartbeat Message Processing** - Supports Feishu heartbeat message type, real-time connection status monitoring
- 🚀 **High-Performance Message Processing** - Async processing, message queuing, parallel execution
- 🎯 **Strategy Pattern Event Handling** - Extensible event handler architecture
- 🔌 **Event Interceptors** - Support inserting custom logic before/after event handling (logging, telemetry, rate limiting, etc.)
- 🛡️ **Enterprise-Grade Stability** - Comprehensive error handling, resource management, logging
- ⚙️ **Flexible Configuration** - Supports configuration files, code configuration, and builder pattern
- 📊 **Monitoring-Friendly** - Detailed event notifications, performance metrics, heartbeat statistics, FeishuMetrics integration
- 🔁 **Exponential Backoff Reconnection** - Pluggable reconnection strategy, dual limits on attempts and time, debounce mechanism
- 🔐 **Message Sequence Validation** - Replay attack detection, message loss detection, sequence rollback detection
- 📦 **Message Queue Backpressure** - Three backpressure strategies (DropOldest/DropNewest/Block)
- 🔑 **Event Deduplication** - In-memory/Distributed deduplication (Redis), prevent duplicate processing
- 🔒 **SSL/TLS Certificate Validation** - Configurable certificate validation policy, custom validation callback
- 🎫 **Auto Token Refresh** - Access token caching and early refresh to avoid expiration
- ⚡ **Native AOT Support** - First-class Native AOT publishing on net8.0+, with compile-time protobuf models and source-generated JSON serialization

## 🚀 Quick Start

### 1. Install NuGet Package

```bash
dotnet add package Mud.Feishu.WebSocket
```

### 2. Minimal Configuration (One Line)

In `Program.cs`:

```csharp
using Mud.Feishu.WebSocket;

var builder = WebApplication.CreateBuilder(args);

// First register multi-application support
builder.Services.AddFeishuApp(builder.Configuration);

// One line to register WebSocket service (requires at least one event handler)
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration, "default")
    .AddHandler<MessageReceiveEventHandler>()
    .Build();

var app = builder.Build();
app.Run();
```

### 3. Complete Configuration (Add Event Handlers)

```csharp
// First register multi-application support
builder.Services.AddFeishuApp(builder.Configuration);

// Register from configuration file and add event handlers
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration, "default")
    .AddHandler<MessageReceiveEventHandler>()
    .AddHandler<UserCreateEventHandler>()
    .Build();

var app = builder.Build();
app.Run();
```

### 4. Configuration Options

```json
{
  "FeishuApps": [
    {
      "AppKey": "default",
      "AppId": "your_app_id",
      "AppSecret": "your_app_secret",
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
    "AllowedHostSuffixes": "*.feishu.cn;*.larksuite.com",
    "EventDeduplication": {
      "Mode": "InMemory",
      "CacheExpiration": "48:00:00",
      "CleanupInterval": "00:05:00"
    }
  }
}
```

## 🏗️ Architecture Design

### Modular Architecture

The Feishu WebSocket client adopts modular design, breaking down complex functionality into specialized components to improve code maintainability and extensibility.

### Architecture Design

#### Core Components

| Component                               | Responsibility           | Features                                                                                  |
| --------------------------------------- | ------------------------ | ----------------------------------------------------------------------------------------- |
| **WebSocketConnectionManager**          | Connection Manager       | Connection establishment, disconnection, state management, SSL/TLS certificate validation |
| **AuthenticationManager**               | Authentication Manager   | WebSocket authentication flow, state management, authentication events                    |
| **MessageRouter**                       | Message Router           | Message routing, version detection (v1.0/v2.0), handler management                        |
| **BinaryMessageProcessor**              | Binary Message Processor | Incremental receiving, ProtoBuf/JSON parsing, memory optimization                         |
| **HeartbeatManager**                    | Heartbeat Manager        | Heartbeat detection, timeout handling, consecutive timeout triggers reconnection          |
| **SessionManager**                      | Session Manager          | session_id management, session recovery, 24-hour validity                                 |
| **MessageSequenceValidator**            | Sequence Validator       | Replay detection, message loss detection, sequence rollback detection                     |
| **EventSubscriptionManager**            | Subscription Manager     | Event type subscription, subscription request sending                                     |
| **ReconnectionOrchestrator**            | Reconnection Coordinator | Unified reconnection management, debounce mechanism, cooldown time                        |
| **ExponentialBackoffReconnectStrategy** | Backoff Strategy         | Exponential backoff delay, dual limits on attempts and time                               |

#### Message Handlers

| Handler                   | Description                                                               |
| ------------------------- | ------------------------------------------------------------------------- |
| **IMessageHandler**           | Message handler interface (CanHandle/HandleAsync) |
| **FeishuEventMessageHandler** | Event message handler, supports v1.0 and v2.0     |
| **AuthMessageHandler**        | Auth message handler                              |
| **HeartbeatMessageHandler**   | Heartbeat message handler                         |
| **PingPongMessageHandler**    | Ping/Pong message handler                         |
| **FeishuWebSocketClient**     | Main client, composes all components              |

### Architecture Advantages

- **🎯 Single Responsibility** - Each component focuses on specific functionality, code is clear and easy to understand
- **🔧 Improved Code Reusability** - Modular design, each component can be used independently
- **🧪 Test-Friendly** - Each component can be tested independently, dependencies are clear
- **🚀 Enhanced Extensibility** - New features implemented by adding components, flexible configuration

### Custom Message Handler

```csharp
// Create custom message handler
public class CustomMessageHandler : JsonMessageHandler
{
    public CustomMessageHandler(ILogger<CustomMessageHandler> logger) : base(logger)
    {
    }

    public override bool CanHandle(string messageType)
        => messageType == "custom_type";

    public override async Task HandleAsync(string message, CancellationToken cancellationToken = default)
    {
        var data = SafeDeserialize<CustomMessage>(message);
        // Processing logic...
    }
}
```

> 💡 Built-in message handlers (Ping/Pong, auth, heartbeat, event) are automatically registered on the internal `MessageRouter` when `FeishuWebSocketClient` is constructed; `MessageRouter.RegisterHandler(IMessageHandler)` is a public method that can be used when building your own message router.

### File Structure

```
Mud.Feishu.WebSocket/
├── Configuration/                 # Configuration options
│   ├── FeishuWebSocketOptions.cs  # Core configuration options
│   ├── FeishuWebSocketOptionsValidator.cs # Configuration validator
│   ├── EventDeduplicationOptions.cs # Event deduplication config
│   ├── EventDeduplicationMode.cs  # Deduplication mode enum
│   └── MessageSizeLimits.cs       # Message size limits
├── Core/                          # Core components
│   ├── WebSocketConnectionManager.cs  # Connection management
│   ├── AuthenticationManager.cs      # Authentication management
│   ├── MessageRouter.cs              # Message routing
│   ├── BinaryMessageProcessor.cs     # Binary processing
│   ├── HeartbeatManager.cs           # Heartbeat management
│   ├── SessionManager.cs             # Session management
│   ├── MessageSequenceValidator.cs   # Message sequence validation
│   ├── EventSubscriptionManager.cs   # Event subscription management
│   ├── ReconnectionOrchestrator.cs   # Reconnection coordinator
│   ├── ExponentialBackoffReconnectStrategy.cs # Exponential backoff strategy
│   ├── IReconnectStrategy.cs         # Reconnect strategy interface
│   ├── IReconnectionOrchestrator.cs  # Reconnection orchestrator interface
│   ├── ErrorRecoveryStrategy.cs      # Error recovery strategy
│   ├── RetryHelper.cs                # Retry utility
│   └── JsonOptions.cs                # JSON serialization options
├── Handlers/                      # Message handlers
│   ├── FeishuEventMessageHandler.cs # Event message handling
│   ├── AuthMessageHandler.cs       # Auth message handling
│   ├── HeartbeatMessageHandler.cs  # Heartbeat message handling
│   ├── PingPongMessageHandler.cs   # Ping/Pong handling
│   ├── JsonMessageHandler.cs       # JSON message base class
│   └── ScopedFeishuEventHandlerFactory.cs # Event handler factory
├── Interfaces/                    # Public interfaces
│   ├── IFeishuWebSocketClient.cs   # Client interface
│   ├── IFeishuWebSocketManager.cs  # Manager interface
│   └── IMessageHandler.cs          # Message handler interface
├── SocketEventArgs/               # Event argument classes
├── DataModels/                    # Data models
├── Exceptions/                    # Exception definitions
├── Extensions/                    # Extension methods
│   ├── FeishuWebSocketServiceBuilder.cs # Service builder
│   └── ServiceCollectionExtensions.cs   # Registration extensions
├── FeishuWebSocketClient.cs       # Main client
├── FeishuWebSocketManager.cs      # Manager implementation
├── FeishuWebSocketHostedService.cs # Background service
└── WebSocketConnectionState.cs    # Connection state model
```

## 🏗️ Service Registration Methods

### 🚀 Minimal Registration (Recommended)

```csharp
// One line to complete basic configuration
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .Build();
```

### 📋 Register Multiple Event Handlers

```csharp
// Support chaining, register multiple handlers
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .AddHandler<UserCreateEventHandler>()
    .AddHandler<MessageReadEventHandler>()
    .Build();
```

### ⚙️ Code Configuration

```csharp
// Use delegate to configure options
builder.Services.CreateFeishuWebSocketServiceBuilder(options =>
{
    options.Reconnect.Auto = true;
    options.Reconnect.MaxAttempts = 5;
    options.Reconnect.TotalBudget = TimeSpan.FromMinutes(30);
    options.HeartbeatIntervalMs = 25000;
    options.EventDeduplication.Mode = EventDeduplicationMode.InMemory;
})
.AddHandler<MessageReceiveEventHandler>()
.Build();
```

### 🎯 Apply Method

```csharp
// Use Apply method for conditional configuration
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .Apply(b =>
    {
        if (builder.Environment.IsDevelopment())
            b.AddInterceptor<LoggingEventInterceptor>();

        if (builder.Configuration.GetValue<bool>("Features:EnableAudit"))
            b.AddHandler<AuditEventHandler>();
    })
    .AddHandler<MessageReceiveEventHandler>()
    .Build();
```

### 🔌 Add Event Interceptors

```csharp
// Add built-in logging interceptor and custom interceptor
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddInterceptor<LoggingEventInterceptor>()  // Built-in logging interceptor
    .AddInterceptor<CustomTelemetryInterceptor>()  // Custom telemetry interceptor
    .AddHandler<MessageReceiveEventHandler>()
    .Build();
```

### 🎯 Three Handler Registration Methods

```csharp
// Method 1: Type registration (recommended)
.AddHandler<MessageReceiveEventHandler>()

// Method 2: Factory registration
.AddHandler(sp => new FactoryEventHandler(
    sp.GetRequiredService<ILogger<FactoryEventHandler>>()))

// Method 3: Instance registration
.AddHandler(new InstanceEventHandler())
```

---

## 🎯 Event Handlers (Strategy Pattern)

### Built-in Event Handlers

| Handler                            | Event Type                       | Description                        |
| ---------------------------------- | -------------------------------- | ---------------------------------- |
| `MessageReceiveEventHandler`       | `im.message.receive_v1`          | Receive message event              |
| `UserCreateEventHandler`           | `contact.user.created_v3`        | User created event                 |
| `MessageReadEventHandler`          | `im.message.message_read_v1`     | Message read event                 |
| `ChatMemberUserAddedEventHandler`  | `im.chat.member.user.added_v1`   | User joins group chat              |
| `ChatMemberUserDeletedEventHandler`| `im.chat.member.user.deleted_v1` | User leaves group chat             |
| `DefaultFeishuEventHandler<T>`     | -                                | Unknown event handling (abstract)  |
| `DepartmentCreatedEventHandler`    | `contact.department.created_v3`  | Department created event           |
| `DepartmentDeleteEventHandler`     | `contact.department.deleted_v3`  | Department deleted event           |

### Using Built-in Event Handler Base Classes

Mud.Feishu.Abstractions provides multiple built-in event handler base classes. Inheriting from these base classes can simplify development:

#### User Event Handler (Generic Base Class)

```csharp
using Mud.Feishu.Abstractions;
using Mud.Feishu.WebSocket.Services;
using System.Text.Json;

namespace YourProject.Handlers;

/// <summary>
/// Demo user event handler - implements generic interface
/// </summary>
public class DemoUserEventHandler : IFeishuEventHandler
{
    private readonly ILogger<DemoUserEventHandler> _logger;
    private readonly DemoEventService _eventService;

    public DemoUserEventHandler(ILogger<DemoUserEventHandler> logger, DemoEventService eventService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    public string SupportedEventType => "contact.user.created_v3";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        try
        {
            // Parse user data
            var userData = ParseUserData(eventData);

            // Record event to service
            await _eventService.RecordUserEventAsync(userData, cancellationToken);

            // Simulate business processing
            await ProcessUserEventAsync(userData, cancellationToken);

            _logger.LogInformation("✅ [User Event] User creation event processing completed: {UserId}", userData.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [User Event] Failed to process user creation event");
            throw;
        }
    }

    private UserData ParseUserData(EventData eventData)
    {
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(eventData.Event?.ToString() ?? "{}");
        var userElement = jsonElement.GetProperty("user");

        return new UserData
        {
            UserId = userElement.GetProperty("user_id").GetString() ?? "",
            UserName = userElement.GetProperty("name").GetString() ?? "",
            Email = TryGetProperty(userElement, "email") ?? "",
            Department = TryGetProperty(userElement, "department") ?? "",
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow
        };
    }

    private static string? TryGetProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
    }
}

/// <summary>
/// User data model
/// </summary>
public class UserData
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ProcessedAt { get; init; }
}
```

#### Department Event Handler (Inherit Specialized Base Class)

```csharp
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.EventCallback.Organization;
using Mud.Feishu.WebSocket.Services;

namespace YourProject.Handlers;

/// <summary>
/// Demo department creation event handler - inherits DepartmentCreatedEventHandler base class
/// </summary>
public class DemoDepartmentEventHandler : DepartmentCreatedEventHandler
{
    private readonly DemoEventService _eventService;

    public DemoDepartmentEventHandler(IFeishuEventDeduplicator businessDeduplicator, ILogger<DemoDepartmentEventHandler> logger, DemoEventService eventService) : base(businessDeduplicator, logger)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        DepartmentCreatedResult? departmentData,
        FeishuEventHeader? header,
        CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation("[Department Event] Starting to process department creation event: {EventId}", eventData.EventId);

        if (departmentData == null)
        {
            _logger.LogWarning("[Department Event] Department creation event data is empty, skip processing: {EventId}", eventData.EventId);
            return;
        }

        try
        {
            // Record event to service
            await _eventService.RecordDepartmentEventAsync(departmentData, cancellationToken);

            // Simulate business processing
            if (departmentData?.Object != null)
            {
                await ProcessDepartmentEventAsync(departmentData.Object, cancellationToken);
            }

            _logger.LogInformation("[Department Event] Department creation event processing completed: {DepartmentId}", departmentData?.Object?.DepartmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Department Event] Failed to process department creation event");
            throw;
        }
    }

    private async Task ProcessDepartmentEventAsync(DepartmentResultInfo departmentData, CancellationToken cancellationToken)
    {
        _logger.LogDebug("🔄 [Department Event] Starting to process department data: {DepartmentId}", departmentData.DepartmentId);

        // Simulate async business operation
        await Task.Delay(100, cancellationToken);

        // Simulate validation logic
        if (string.IsNullOrWhiteSpace(departmentData.DepartmentId))
        {
            throw new ArgumentException("Department ID cannot be empty");
        }

        // Simulate permission initialization
        _logger.LogInformation("[Department Event] Initialize department permissions: {DepartmentName}", departmentData.Name);

        // Simulate updating statistics
        _eventService.IncrementDepartmentCount();

        await Task.CompletedTask;
    }
}

/// <summary>
/// Demo department deletion event handler - inherits DepartmentDeleteEventHandler base class
/// </summary>
public class DemoDepartmentDeleteEventHandler : DepartmentDeleteEventHandler
{
    public DemoDepartmentDeleteEventHandler(IFeishuEventDeduplicator businessDeduplicator, ILogger<DepartmentDeleteEventHandler> logger) : base(businessDeduplicator, logger)
    {
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        DepartmentDeleteResult? eventEntity,
        FeishuEventHeader? header,
        CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        if (eventEntity == null)
        {
            _logger.LogWarning("Department deletion event entity is empty, skip processing");
            return;
        }

        _logger.LogInformation("🗑️ [Department Deletion Event] Starting to process department deletion event");
        _logger.LogDebug("Department deletion event details: {@EventEntity}", eventEntity);

        await Task.CompletedTask;
    }
}
```

### Creating Custom Event Handler

```csharp
public class CustomEventHandler : IFeishuEventHandler
{
    private readonly ILogger<CustomEventHandler> _logger;

    public CustomEventHandler(ILogger<CustomEventHandler> logger)
        => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public string SupportedEventType => "custom.event.example_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData == null) throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation("🎯 Processing custom event: {EventType}", eventData.EventType);

        // Implement your business logic
        await ProcessBusinessLogicAsync(eventData);
    }

    private async Task ProcessBusinessLogicAsync(EventData eventData)
    {
        // Database operations, external API calls, etc.
        await Task.CompletedTask;
    }
}
```

### Registering Custom Handler

```csharp
// Register handlers (multiple ways)
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<CustomEventHandler>()                    // Type registration
    .AddHandler(sp => new FactoryEventHandler(           // Factory registration
        sp.GetRequiredService<ILogger<FactoryEventHandler>>()))
    .AddHandler(new InstanceEventHandler())               // Instance registration
    .Build();
```

### Event Interceptors

Event interceptors allow executing custom logic before and after event handling, such as logging, metrics collection, permission verification, etc.

#### Built-in Interceptor

**LoggingEventInterceptor** - Record event handling logs

```csharp
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddInterceptor<LoggingEventInterceptor>()  // Record event handling start and end
    .AddHandler<MessageReceiveEventHandler>()
    .Build();
```

#### Custom Interceptors

Create custom interceptors by implementing the `IFeishuEventInterceptor` interface:

```csharp
using Mud.Feishu.Abstractions;

/// <summary>
/// Custom telemetry interceptor example
/// </summary>
public class CustomTelemetryInterceptor : IFeishuEventInterceptor
{
    private readonly ILogger<CustomTelemetryInterceptor> _logger;

    public CustomTelemetryInterceptor(ILogger<CustomTelemetryInterceptor> logger)
        => _logger = logger;

    /// <summary>
    /// Before event handling interceptor
    /// </summary>
    /// <returns>Return false to interrupt event handling flow</returns>
    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Telemetry] Event started: {EventType}, EventId: {EventId}", eventType, eventData.EventId);
        return Task.FromResult(true); // Return true to continue, false to interrupt
    }

    /// <summary>
    /// After event handling interceptor
    /// </summary>
    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        if (exception == null)
        {
            _logger.LogInformation("[Telemetry] Event succeeded: {EventType}", eventType);
        }
        else
        {
            _logger.LogError(exception, "[Telemetry] Event failed: {EventType}", eventType);
        }
        return Task.CompletedTask;
    }
}
```

#### Register Custom Interceptors

```csharp
// Type registration
.AddInterceptor<CustomTelemetryInterceptor>()

// Factory registration
.AddInterceptor(sp => new CustomTelemetryInterceptor(
    sp.GetRequiredService<ILogger<CustomTelemetryInterceptor>>()))

// Instance registration
var interceptor = new CustomTelemetryInterceptor(logger);
.AddInterceptor(interceptor)
```

#### Interceptor Execution Order

Interceptors execute in registration order, complete flow:

```
WebSocket Event Arrives
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

#### Runtime Dynamic Registration

```csharp
public class ServiceManager
{
    private readonly IFeishuEventHandlerFactory _factory;
    private readonly ILogger<ServiceManager> _logger;

    public ServiceManager(IFeishuEventHandlerFactory factory, ILogger<ServiceManager> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public void RegisterHandler()
    {
        var customHandler = new CustomEventHandler(_logger);
        _factory.RegisterHandler(customHandler);
        _logger.LogInformation("Registered custom handler: {HandlerType}", typeof(CustomEventHandler).Name);
    }
}
```

## ⚙️ Configuration Options

### WebSocket Configuration

| Option                                | Type                                 | Default    | Description                                                      |
| ------------------------------------- | ------------------------------------ | ---------- | ---------------------------------------------------------------- |
| `AppKey`                              | string                               | "default"  | Feishu app key used as metric dimension (**hot-updatable**)      |
| `Reconnect.Auto`                      | bool                                 | true       | Auto reconnect                                                   |
| `Reconnect.MaxAttempts`               | int                                  | 5          | Max reconnect attempts; 0 = unlimited (bounded by `Reconnect.TotalBudget`) |
| `Reconnect.MaxAuthRetryAttempts`      | int                                  | 5          | Max authentication retries; 0 = unlimited (independent of reconnect) |
| `Reconnect.BaseDelayMs`               | int                                  | 5000       | Base reconnect delay (ms), min 1000                              |
| `Reconnect.MaxDelayMs`                | int                                  | 30000      | Max reconnect delay (ms), must be ≥ `Reconnect.BaseDelayMs`       |
| `Reconnect.TotalBudget`               | TimeSpan                             | 30min      | Max total reconnection time, stops retrying after                |
| `Reconnect.Cooldown`                  | TimeSpan                             | 5s         | Minimum interval between reconnection attempts                   |
| `EnableReconnectMetrics`              | bool                                 | true       | Enable reconnection metrics collection                           |
| `HeartbeatIntervalMs`                 | int                                  | 25000      | Heartbeat interval (ms), min 5000 (Feishu recommends ≤25s)       |
| `ConnectionTimeoutMs`                 | int                                  | 10000      | Connection timeout (ms)                                          |
| `InitialReceiveBufferSize`            | int                                  | 4096       | Initial receive buffer size (bytes)                              |
| `HealthCheckIntervalMs`               | int                                  | 60000      | Health check interval (ms), min 1000                             |
| `MessageHandlerTimeoutMs`             | int                                  | 30000      | Per-message handling timeout (ms); 0 = unlimited                 |
| `MaxConcurrentHandlers`               | int                                  | 32         | Concurrency gate; 0/negative = unlimited. **Backpressure is applied on the receive path** |
| `AuthTimeoutMs`                       | int                                  | 30000      | Authentication response timeout (ms); 0 falls back to 30000      |
| `AuthGateTimeoutMs`                   | int                                  | 0          | Auth gate wait limit (ms); 0 = disabled (**hot-updatable**)      |
| `ProtocolKeepAliveInterval`           | TimeSpan                             | 20s        | Protocol-level Ping/Pong keep-alive; 0 = disabled (5–300s)       |
| `SequenceGapThreshold`                | ulong                                | 0          | Sequence gap threshold; 0 = gap detection disabled               |
| `Certificate.Mode`                    | `CertificateValidationMode`          | `Strict`   | Certificate validation mode: `Strict` / `Dev` / `Custom` (`Custom` requires `Certificate.CustomCallback`) |
| `Certificate.ValidateServerCertificate` | bool                               | true       | Validate SSL certificate (recommended true in production)        |
| `Certificate.AllowSelfSignedCertificates` | bool                             | false      | Allow self-signed certificates (only honored when `Mode=Dev`)    |
| `Certificate.AllowCertificateNameMismatch` | bool                           | false      | Allow certificate name mismatch (only honored when `Mode=Dev`)   |
| `Certificate.AllowInsecureWebSocket`  | bool                                 | false      | Allow insecure ws:// connections (dev/test only)                 |
| `Certificate.CustomCallback`          | RemoteCertificateValidationCallback? | null       | Custom certificate validation callback (`Mode=Custom` requires it) |
| `AllowedHostSuffixes`                 | string                               | `*.feishu.cn;*.larksuite.com` | Host allow-list: `*.` wildcard suffixes or exact hosts, `;`-separated, case-insensitive; **empty = unrestricted** (use for custom gateways/local test endpoints) |
| `EventDeduplication`                  | EventDeduplicationOptions            | See below  | Event deduplication configuration                                |

> ℹ️ **Legacy flat keys**: the old JSON keys (`AutoReconnect`, `MaxReconnectAttempts`, `ReconnectDelayMs`, `ValidateServerCertificate`, `AllowSelfSignedCertificates`, …) are **still bindable** (filled into `Reconnect.*` / `Certificate.*` at startup), but **C# code must use the nested API**. See the [configuration migration guide](../documents/Configuration/ConfigMigration-R2.md).

### Message Size Limits (`MessageSizeLimits`)

| Option                 | Type | Default  | Description                        |
| ---------------------- | ---- | -------- | ---------------------------------- |
| `MaxTextMessageSize`   | int  | 1048576  | Max text message size (characters) |
| `MaxTextMessageBytes`  | int  | 0        | Max text message size (UTF-8 bytes); 0 = derive as 3 × `MaxTextMessageSize` (shared by send & receive) |
| `MaxBinaryMessageSize` | long | 10485760 | Max binary message size (bytes); enforced on send and receive |

**Configuration Example:**

```json
{
  "FeishuWebSocket": {
    "MessageSizeLimits": {
      "MaxTextMessageSize": 1048576,
      "MaxTextMessageBytes": 0,
      "MaxBinaryMessageSize": 10485760
    }
  }
}
```

### Event Deduplication (`EventDeduplication`)

| Option              | Type                     | Default    | Description                                    |
| ------------------- | ------------------------ | ---------- | ---------------------------------------------- |
| `Mode`              | `EventDeduplicationMode` | `InMemory` | Deduplication mode (None/InMemory/Distributed) |
| `CacheExpiration`   | TimeSpan                 | 48:00:00   | Cache expiration time, default 48 hours        |
| `CleanupInterval`   | TimeSpan                 | 00:05:00   | Cache cleanup interval, default 5 minutes      |

**Deduplication Modes:**

- `None` - Disable deduplication (not recommended, for special scenarios only)
- `InMemory` - In-memory deduplication (single instance, default)
- `Distributed` - Distributed deduplication (requires `IFeishuEventDistributedDeduplicator`)

**Configuration Example:**

```json
{
  "FeishuWebSocket": {
    "EventDeduplication": {
      "Mode": "InMemory",
      "CacheExpiration": "48:00:00",
      "CleanupInterval": "00:05:00"
    }
  }
}
```

## 🎯 Advanced Usage

### Multi-Environment Configuration

```csharp
var webSocketBuilder = builder.Services.CreateFeishuWebSocketServiceBuilder(configuration);

if (builder.Environment.IsDevelopment())
{
    webSocketBuilder.ConfigureOptions(options => {
        options.HeartbeatIntervalMs = 15000;
    });
}
else
{
    webSocketBuilder.ConfigureFrom(configuration, "Production:Feishu:WebSocket");
}

webSocketBuilder.AddHandler<DevEventHandler>()
    .AddHandler<ProdEventHandler>()
    .Build();
```

### Conditional Handler Registration

```csharp
builder.Services.CreateFeishuWebSocketServiceBuilder(configuration)
    .AddHandler<BaseEventHandler>()
    .Apply(webSocketBuilder => {
        if (configuration.GetValue<bool>("Features:EnableAudit"))
            webSocketBuilder.AddHandler<AuditEventHandler>();

        if (configuration.GetValue<bool>("Features:EnableAnalytics"))
            webSocketBuilder.AddHandler<AnalyticsEventHandler>();

        if (configuration.GetValue<bool>("Features:EnableTelemetry"))
            webSocketBuilder.AddInterceptor<TelemetryInterceptor>();
    })
    .Build();
```

### Configure Redis Distributed Deduplication

```csharp
// Register Redis distributed deduplication service
builder.Services.AddFeishuRedisDeduplicators(builder.Configuration);

// Configure Feishu WebSocket service
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddInterceptor<LoggingEventInterceptor>()
    .AddHandler<MessageReceiveEventHandler>()
    .Build();
```

### Specify Configuration Section Name

```csharp
// Use non-default configuration section
builder.Services.CreateFeishuWebSocketServiceBuilder(
        configuration,
        sectionName: "CustomFeishu")  // Configuration section name
    .AddHandler<MessageReceiveEventHandler>()
    .Build();
```

## 🔧 Advanced Features

### Manual Connection Control

```csharp
public class ConnectionService
{
    private readonly IFeishuWebSocketManager _manager;

    public ConnectionService(IFeishuWebSocketManager manager)
        => _manager = manager;

    public async Task StartAsync() => await _manager.StartAsync();
    public async Task StopAsync() => await _manager.StopAsync();
    public async Task ReconnectAsync() => await _manager.ReconnectAsync();
    public async Task SendMessageAsync(string message)
        => await _manager.SendMessageAsync(message);

    public void SubscribeEvents()
    {
        _manager.Connected += OnConnected;
        _manager.Disconnected += OnDisconnected;
        _manager.Error += OnError;
        _manager.MessageReceived += OnMessageReceived;
    }

    public WebSocketConnectionState GetConnectionState()
        => _manager.GetConnectionState();

    public (TimeSpan Uptime, int ReconnectCount, Exception? LastError) GetStats()
        => _manager.GetConnectionStats();
}
```

### Connection Token Contract (Breaking behavior change)

The `cancellationToken` passed to `ConnectAsync(endpoint, [appAccessToken,] cancellationToken)`
**only governs the connection-establishment phase** (handshake + authentication).
It does **not** represent the connection lifetime:

- Cancelling it after a successful connect will **not** stop the receive loop or the heartbeat
  (previously it was linked as the lifetime token, so cancelling it silently stopped frame reading);
- To terminate a connection use `DisconnectAsync()` / `DisposeAsync()`; to recover use
  `IFeishuWebSocketManager.ReconnectAsync()`.

> Background: the old behavior could produce a **zombie connection** (`WebSocketState.Open` but no
> frames are read: no events, no disconnect notification, recovery only via health-check polling).
> Now every termination path raises a disconnect claim, and a liveness probe covers externally silent failures.

### `MessageReceived` Threading Contract (Breaking behavior change)

`MessageReceived` is an **observation hook** (logging/metrics), not a business entry point:

| Aspect | Current contract |
| --- | --- |
| Dispatch thread | **Not** the receive loop thread — it is dispatched inside the leased processing task |
| Concurrency | **May run concurrently** (bounded by `MaxConcurrentHandlers`) |
| Ordering | Order relative to frame arrival is **not guaranteed** |
| Blocking | A blocking subscriber only occupies one concurrency slot (backpressure); it does **not** block the receive pipeline |

> For ordered processing protected by `MessageHandlerTimeoutMs`, use `IMessageHandler`
> (dispatched via `MessageRouter`) instead.

### Liveness Probe & Health Check

`FeishuWebSocketHealthCheck` now exposes liveness fields in its `data` payload:

| Field | Meaning |
| --- | --- |
| `receive_loop_alive` | Whether the receive loop task is still running |
| `last_receive_utc` | Timestamp of the last received frame (`never` if none) |
| `idle_ms` | Milliseconds since the last received frame (`-1` = no sample yet) |
| `is_zombie` | `true` = zombie state (`State == Open` while the receive loop has ended) ⇒ `Unhealthy` |

> Only the deterministic contradiction "`State == Open` **and** receive loop ended" is treated as a zombie.
> A merely idle connection is **not** reported unhealthy (an idle long connection legitimately receives no frames).
> Zombie state also triggers an automatic reconnect from the hosted service's periodic check.

Liveness is also exported as OTel metrics (dimension `feishu.app_key`), so it can be alerted on as a **trend**:

| Metric | Meaning |
| --- | --- |
| `feishu.websocket.receive.idle_ms` | Milliseconds since the last received frame (`-1` = no sample yet) |
| `feishu.websocket.receive.loop_alive` | `1` = receive loop running, `0` = ended |
| `feishu.websocket.zombie` | `1` = zombie connection (`State == Open` while the receive loop ended) |
| `feishu.websocket.frames.discarded` | Controlled frame/message discards, split by `reason` |

> `frames.discarded` reasons: `fragment_size_exceeded`, `drain_bound_exceeded`, `auth_gate_timeout`,
> `concurrency_rejected`. Discards are a **precursor to event loss** — alert on any sustained increase.
> Note that `receive.idle_ms` growing on its own is normal for an idle long connection
> (the heartbeat is client→server only); it is only actionable together with `zombie`/`loop_alive`.

### Configuration Upper Bounds (fail-fast at startup)

`FeishuWebSocketOptions.Validate()` now enforces **both** lower and upper bounds:

| Option | Upper bound |
| --- | --- |
| `Reconnect.TotalBudget` | 7 days |
| `Reconnect.BaseDelayMs` / `Reconnect.MaxDelayMs` | 1 hour |
| `MessageSizeLimits.MaxTextMessageSize` | 10 MB |
| `ConnectionTimeoutMs` / `AuthTimeoutMs` / `AuthGateTimeoutMs` | 5 minutes |

> Migration: if an existing deployment uses larger values, reduce them to fit.
> The bounds prevent configuration values from exceeding implementation-level representable ranges
> (e.g. an over-long duration handed to `CancellationTokenSource` throws inside the asynchronous
> reconnect path, which surfaces as a **silently disabled automatic reconnect**).

### `StartReceivingAsync` Is Deprecated

The receive loop is managed by `ConnectAsync` — do **not** call
`IFeishuWebSocketClient.StartReceivingAsync` explicitly:

- Throws `InvalidOperationException` when not connected;
- Is an idempotent no-op (with a warning log) when a loop is already running.

> Migration: simply remove the call; use `IFeishuWebSocketManager.ReconnectAsync()` to recover receiving.

### Message Sequence Validation

Built-in `MessageSequenceValidator` detects message replay and loss:

- **Duplicate detection**: Sliding window deduplication (last 1000 messages)
- **Sequence rollback detection**: Detects potential attack behavior
- **Message loss detection**: Warning when sequence gap exceeds threshold

```csharp
var validator = serviceProvider.GetRequiredService<MessageSequenceValidator>();
validator.ValidationFailed += (sender, args) =>
{
    if (args.MessageType == SequenceValidationType.SequenceRollback)
        logger.LogWarning("Sequence rollback detected: {Message}", args.Message);
    else if (args.MessageType == SequenceValidationType.MessageLoss)
        logger.LogWarning("Possible message loss: {Message}", args.Message);
};
```

### Session Management

`SessionManager` manages WebSocket session state, supporting disconnection recovery:

- **Session ID management**: Automatically tracks current session_id
- **Session validity**: 24-hour validity check
- **Reconnection recovery**: Get valid session ID via `GetSessionIdForReconnect()`
- **Session events**: `SessionUpdated` event for session changes

### SSL/TLS Certificate Configuration

```csharp
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .ConfigureOptions(options =>
    {
        options.Certificate.ValidateServerCertificate = true;
        options.Certificate.AllowSelfSignedCertificates = false;
    })
    .AddHandler<MessageReceiveEventHandler>()
    .Build();
```

Custom certificate validation:

```csharp
options.Certificate.Mode = CertificateValidationMode.Custom;
options.Certificate.CustomCallback = (sender, certificate, chain, sslPolicyErrors) =>
{
    return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
};
```

### Custom Reconnection Strategy

Implement `IReconnectStrategy` to replace the default exponential backoff:

```csharp
public class FixedIntervalReconnectStrategy : IReconnectStrategy
{
    private readonly TimeSpan _interval;

    public FixedIntervalReconnectStrategy(TimeSpan interval)
        => _interval = interval;

    public TimeSpan CalculateDelay(int attemptCount) => _interval;

    public bool ShouldContinueReconnect(int attemptCount, TimeSpan totalElapsedTime)
        => attemptCount <= 10;
}

// Register custom strategy (before CreateFeishuWebSocketServiceBuilder)
builder.Services.AddSingleton<IReconnectStrategy>(
    new FixedIntervalReconnectStrategy(TimeSpan.FromSeconds(10)));
```

### Access Token Management

`FeishuWebSocketManager`'s access token caching and auto-refresh are managed by `IAppTokenManager`, no manual refresh interval configuration needed.

## 📋 Supported Event Types

### WebSocket Message Types

- `ping` / `pong` - Connection keep-alive
- `heartbeat` - Heartbeat message
- `event` - Business event
- `auth` - Authentication response

### Main Business Events

- **Messages**: `im.message.receive_v1`, `im.message.message_read_v1`
- **Group Chats**: `im.chat.member.user_added_v1`, `im.chat.member.user_deleted_v1`
- **Users**: `contact.user.created_v3`, `contact.user.updated_v3`, `contact.user.deleted_v3`
- **Departments**: `contact.department.*_v3`
- **Approvals**: `approval.approval.*_v1`
- **Calendar**: `calendar.event.updated_v4`
- **Meetings**: `meeting.meeting.*_v1`

## 📄 License

This project is distributed and used under the MIT License.

---

**🚀 Get started with Feishu WebSocket Client now and build a stable, reliable event handling system!**
