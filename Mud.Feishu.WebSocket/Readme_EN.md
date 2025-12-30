# Feishu WebSocket Client Service

Enterprise-grade Feishu event subscription WebSocket client, providing reliable connection management, automatic reconnection, and strategy pattern event handling.

**🚀 New Feature: Minimal API** - Complete service registration with one line of code, ready to use!

## ✨ Core Features

- 🚀 **Minimal API** - Complete service registration with one line of code, ready to use
- 🔄 **Intelligent Connection Management** - Automatic reconnection, heartbeat detection, status monitoring
- 🫀 **Heartbeat Message Processing** - Supports Feishu heartbeat message type, real-time connection status monitoring
- 🚀 **High-Performance Message Processing** - Async processing, message queuing, parallel execution
- 🎯 **Strategy Pattern Event Handling** - Extensible event handler architecture
- 🛡️ **Enterprise-Grade Stability** - Comprehensive error handling, resource management, logging
- ⚙️ **Flexible Configuration** - Supports configuration files, code configuration, and builder pattern
- 📊 **Monitoring-Friendly** - Detailed event notifications, performance metrics, heartbeat statistics

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

// One line to register WebSocket service (requires at least one event handler)
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .Build();

var app = builder.Build();
app.Run();
```

### 3. Complete Configuration (Add Event Handlers)

```csharp
// Register from configuration file and add event handlers
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .AddHandler<UserCreatedEventHandler>()
    .Build();

var app = builder.Build();
app.Run();
```

### 4. Configuration Options

```json
{
  "Feishu": {
    "AppId": "your_app_id",
    "AppSecret": "your_app_secret",
    "WebSocket": {
      "AutoReconnect": true,
      "MaxReconnectAttempts": 5,
      "ReconnectDelayMs": 5000,
      "HeartbeatIntervalMs": 30000,
      "EnableLogging": true
    }
  }
}
```

## 🏗️ Architecture Design

### Modular Architecture

The Feishu WebSocket client adopts modular design, breaking down complex functionality into specialized components to improve code maintainability and extensibility.

### Architecture Design

#### Core Components

| Component | Responsibility | Features |
|-----------|---------------|----------|
| **WebSocketConnectionManager** | Connection Manager | Connection establishment, disconnection, state management, reconnection mechanism |
| **AuthenticationManager** | Authentication Manager | WebSocket authentication flow, state management, authentication events |
| **MessageRouter** | Message Router | Message routing, version detection (v1.0/v2.0), handler management |
| **BinaryMessageProcessor** | Binary Message Processor | Incremental receiving, ProtoBuf/JSON parsing, memory optimization |

#### Message Handlers

| Handler | Description |
|---------|-------------|
| **IMessageHandler** | Message handler interface, provides generic deserialization functionality |
| **EventMessageHandler** | Event message handler, supports v1.0 and v2.0 versions |
| **BasicMessageHandler** | Basic message handler (Ping/Pong, authentication, heartbeat) |
| **FeishuWebSocketClient** | Main client, composes all components |

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
    public override bool CanHandle(string messageType)
        => messageType == "custom_type";

    public override async Task HandleAsync(string message, CancellationToken cancellationToken = default)
    {
        var data = SafeDeserialize<CustomMessage>(message);
        // Processing logic...
    }
}

// Register to message router
client.RegisterMessageProcessor(customMessageHandler);
```

### File Structure

```
Mud.Feishu.WebSocket/
├── Core/                           # Core components
│   ├── WebSocketConnectionManager.cs  # Connection management
│   ├── AuthenticationManager.cs      # Authentication management
│   ├── MessageRouter.cs             # Message routing
│   └── BinaryMessageProcessor.cs    # Binary processing
├── Handlers/                       # Message handlers
│   ├── IMessageHandler.cs          # Handler interface
│   ├── EventMessageHandler.cs       # Event message handling
│   └── BasicMessageHandler.cs     # Basic message handling
├── SocketEventArgs/                # Event argument classes
├── DataModels/                    # Data models
├── FeishuWebSocketClient.cs       # Main client
└── Examples/                      # Usage examples
```

## 🏗️ Service Registration Methods

### 🚀 Minimal Registration (Recommended)

```csharp
// One line to complete basic configuration
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration);

// Add event handlers
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .AddHandler<UserCreatedEventHandler>()
    .UseMultiHandler();
```

### ⚙️ Code Configuration

```csharp
builder.Services.AddFeishuWebSocketServiceBuilder(options =>
{
    options.AppId = "your_app_id";
    options.AppSecret = "your_app_secret";
    options.AutoReconnect = true;
    options.HeartbeatIntervalMs = 30000;
});
```

### 🔧 Advanced Configuration (Builder Pattern)

```csharp
builder.Services.AddFeishuWebSocketServiceBuilder(configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .Build();
```

---

## 🎯 Event Handlers (Strategy Pattern)

### Built-in Event Handlers

| Handler | Event Type | Description |
|---------|-----------|-------------|
| `ReceiveMessageEventHandler` | `im.message.receive_v1` | Receive message event |
| `UserCreatedEventHandler` | `contact.user.created_v3` | User created event |
| `MessageReadEventHandler` | `im.message.message_read_v1` | Message read event |
| `UserAddedToGroupEventHandler` | `im.chat.member.user_added_v1` | User joins group chat |
| `UserRemovedFromGroupEventHandler` | `im.chat.member.user_deleted_v1` | User leaves group chat |
| `DefaultFeishuEventHandler` | - | Unknown event type handling |
| `DepartmentCreatedEventHandler` | `contact.department.created_v3` | Department created event |
| `DepartmentDeleteEventHandler` | `contact.department.deleted_v3` | Department deleted event |

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
using Mud.Feishu.Abstractions.DataModels.Organization;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.WebSocket.Services;

namespace YourProject.Handlers;

/// <summary>
/// Demo department creation event handler - inherits DepartmentCreatedEventHandler base class
/// </summary>
public class DemoDepartmentEventHandler : DepartmentCreatedEventHandler
{
    private readonly DemoEventService _eventService;

    public DemoDepartmentEventHandler(ILogger<DemoDepartmentEventHandler> logger, DemoEventService eventService) : base(logger)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        ObjectEventResult<DepartmentCreatedResult>? departmentData,
        CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation("[Department Event] Starting to process department creation event: {EventId}", eventData.EventId);

        try
        {
            // Record event to service
            await _eventService.RecordDepartmentEventAsync(departmentData.Object, cancellationToken);

            // Simulate business processing
            await ProcessDepartmentEventAsync(departmentData.Object, cancellationToken);

            _logger.LogInformation("[Department Event] Department creation event processing completed: {DepartmentId}", departmentData.Object.DepartmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Department Event] Failed to process department creation event");
            throw;
        }
    }

    private async Task ProcessDepartmentEventAsync(DepartmentCreatedResult departmentData, CancellationToken cancellationToken)
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
    public DemoDepartmentDeleteEventHandler(ILogger<DepartmentDeleteEventHandler> logger) : base(logger)
    {
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        DepartmentDeleteResult? eventEntity,
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
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<CustomEventHandler>()                    // Type registration
    .AddHandler(sp => new FactoryEventHandler(           // Factory registration
        sp.GetRequiredService<ILogger<FactoryEventHandler>>()))
    .AddHandler(new InstanceEventHandler());               // Instance registration

// Or use builder pattern
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<CustomEventHandler>()
    .Build();
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

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `AutoReconnect` | bool | true | Auto reconnect |
| `MaxReconnectAttempts` | int | 5 | Max reconnect attempts |
| `ReconnectDelayMs` | int | 5000 | Reconnect delay (ms) |
| `HeartbeatIntervalMs` | int | 30000 | Heartbeat interval (ms) |
| `ConnectionTimeoutMs` | int | 10000 | Connection timeout (ms) |
| `ReceiveBufferSize` | int | 4096 | Receive buffer size |
| `EnableLogging` | bool | true | Enable logging |
| `EnableMessageQueue` | bool | true | Enable message queue |
| `MessageQueueCapacity` | int | 1000 | Message queue capacity |
| `ParallelMultiHandlers` | bool | true | Parallel execution for multiple handlers |

## ⚙️ Configuration Options

### Main Configuration Items

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `AutoReconnect` | bool | true | Auto reconnect |
| `MaxReconnectAttempts` | int | 5 | Max reconnect attempts |
| `ReconnectDelayMs` | int | 5000 | Reconnect delay (ms) |
| `HeartbeatIntervalMs` | int | 30000 | Heartbeat interval (ms) |
| `EnableLogging` | bool | true | Enable logging |
| `EnableMessageQueue` | bool | true | Enable message queue |

## 🎯 Advanced Usage

### Multi-Environment Configuration

```csharp
var webSocketBuilder = builder.Services.AddFeishuWebSocketServiceBuilder(configuration);

if (builder.Environment.IsDevelopment())
{
    webSocketBuilder.ConfigureOptions(options => {
        options.EnableLogging = true;
        options.HeartbeatIntervalMs = 15000;
    });
}
else
{
    webSocketBuilder.ConfigureFrom(configuration, "Production:WebSocket");
}

webSocketBuilder.AddHandler<DevEventHandler>()
    .AddHandler<ProdEventHandler>()
    .Build();
```

### Conditional Handler Registration

```csharp
builder.Services.AddFeishuWebSocketServiceBuilder(configuration)
    .AddHandler<BaseEventHandler>()
    .Apply(webSocketBuilder => {
        if (configuration.GetValue<bool>("Features:EnableAudit"))
            webSocketBuilder.AddHandler<AuditEventHandler>();
        
        if (configuration.GetValue<bool>("Features:EnableAnalytics"))
            webSocketBuilder.AddHandler<AnalyticsEventHandler>();
    })
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

    // Connection management
    public async Task StartAsync() => await _manager.StartAsync();
    public async Task StopAsync() => await _manager.StopAsync();
    public async Task ReconnectAsync() => await _manager.ReconnectAsync();
    
    // Message operations
    public async Task SendMessageAsync(string message) 
        => await _manager.SendMessageAsync(message);
    
    // Event subscription
    public void SubscribeEvents()
    {
        _manager.Connected += OnConnected;
        _manager.Disconnected += OnDisconnected;
        _manager.HeartbeatReceived += OnHeartbeat;
    }
}
```

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
