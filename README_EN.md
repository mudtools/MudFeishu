# MudFeishu

An enterprise-grade .NET SDK for Feishu (Lark) API integration, providing comprehensive solutions for HTTP API, WebSocket real-time event subscription, and Webhook event handling. Supports Strategy Pattern, Factory Pattern, and automatic token management, designed for enterprise application development.

## 📦 Project Overview

| Component | Description | NuGet |
|-----------|-------------|-------|
| **Mud.Feishu.Abstractions** | Event subscription abstraction layer with Strategy and Factory pattern event handling architecture | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Abstractions.svg)](https://www.nuget.org/packages/Mud.Feishu.Abstractions/) |
| **Mud.Feishu** | Core HTTP API client library with full Feishu capabilities including organization, messaging, and group chat features | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.svg)](https://www.nuget.org/packages/Mud.Feishu/) |
| **Mud.Feishu.WebSocket** | Feishu WebSocket client supporting real-time event subscription and automatic reconnection | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.WebSocket.svg)](https://www.nuget.org/packages/Mud.Feishu.WebSocket/) |
| **Mud.Feishu.Webhook** | Feishu Webhook event handling component for HTTP callback event reception and processing | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Webhook.svg)](https://www.nuget.org/packages/Mud.Feishu.Webhook/) |

## 🚀 Quick Start

### Installation

```bash
# Event handling abstraction layer
dotnet add package Mud.Feishu.Abstractions

# HTTP API client
dotnet add package Mud.Feishu

# WebSocket real-time event subscription
dotnet add package Mud.Feishu.WebSocket

# Webhook HTTP callback event handling
dotnet add package Mud.Feishu.Webhook
```

### Basic Configuration

```csharp
using Mud.Feishu;
using Mud.Feishu.WebSocket;
using Mud.Feishu.Webhook;

var builder = WebApplication.CreateBuilder(args);

// Register HTTP API service (Method 1: Lazy mode - register all services)
builder.Services.AddFeishuServices(builder.Configuration);

// Register HTTP API service (Method 2: Builder pattern - on-demand registration)
builder.Services.CreateFeishuServicesBuilder(builder.Configuration)
    .AddOrganizationApi()
    .AddMessageApi()
    .AddChatGroupApi()
    .Build();

// Register HTTP API service (Method 3: Module-based registration)
builder.Services.AddFeishuServices(builder.Configuration, new[] {
    FeishuModule.Organization,
    FeishuModule.Message,
    FeishuModule.ChatGroup
});

// Register HTTP API service (Method 4: Token management services only)
builder.Services.AddFeishuTokenManagers(builder.Configuration);

// Register HTTP API service (Method 5: Code-based configuration)
builder.Services.CreateFeishuServicesBuilder(options =>
{
    options.AppId = "your_app_id";
    options.AppSecret = "your_app_secret";
    options.BaseUrl = "https://open.feishu.cn";
})
.AddAllApis()
.Build();

// Register WebSocket event subscription service
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

// Register Webhook HTTP callback event service
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<UserCreatedEventHandler>()
    .AddHandler<MessageReceiveEventHandler>()
    .Build();

var app = builder.Build();

// Add Webhook middleware
app.UseFeishuWebhook();

app.Run();
```

### Configuration File

```json
{
    "Feishu": {
        "AppId": "demo_app_id",
        "AppSecret": "demo_app_secret",
        "BaseUrl": "https://open.feishu.cn",
        "WebSocket": {
            "AutoReconnect": true,
            "MaxReconnectAttempts": 5,
            "ReconnectDelayMs": 5000,
            "HeartbeatIntervalMs": 30000,
            "ConnectionTimeoutMs": 10000,
            "ReceiveBufferSize": 4096,
            "EnableLogging": true,
            "EnableMessageQueue": true,
            "MessageQueueCapacity": 1000,
            "ParallelMultiHandlers": true
        },
        "Webhook": {
            "RoutePrefix": "feishu/Webhook",
            "AutoRegisterEndpoint": true,
            "VerificationToken": "your_verification_token",
            "EncryptKey": "your_encrypt_key",
            "EnableRequestLogging": true,
            "EnableExceptionHandling": true,
            "EventHandlingTimeoutMs": 30000,
            "MaxConcurrentEvents": 10,
            "EnablePerformanceMonitoring": false,
            "AllowedHttpMethods": ["POST"],
            "MaxRequestBodySize": 10485760,
            "ValidateSourceIP": false,
            "AllowedSourceIPs": []
        }
    }
}
```

## 📊 Key Features

### 🏛️ Mud.Feishu.Abstractions - Event Handling Abstraction Layer

#### 🎯 Event Handling Architecture

| Feature | Description | Event Types |
|---------|-------------|-------------|
| **Strategy Pattern** | Extensible event handler architecture supporting multiple event type processing | - |
| **Factory Pattern** | Built-in event handler factory with dynamic registration and discovery | - |
| **Abstract Base Classes** | Provides base classes like `DefaultFeishuEventHandler<T>` to simplify development | - |
| **Type Safety** | Strong-typed event data models with compile-time type checking | - |
| **Async Processing** | Fully asynchronous event processing with parallel execution support | - |
| **Extensibility** | Easy to extend with new event types and handlers | - |
| **Organization Events** | User change events, department organizational structure changes | User create/update/delete, department change |
| **Message Events** | Receive new messages, send status notifications, read status changes | Message receive, send status, read status |
| **Application Events** | App authorization events, permission level adjustment events | App authorization, permission change |
| **Custom Events** | Supports enterprise custom event types | Enterprise custom |

### 🌐 Mud.Feishu - HTTP API Client Features

| Module Category | Main Features | API Version | Description |
|----------------|---------------|-------------|-------------|
| **🔐 Authentication & Token Management** | Multi-type token support | - | Supports app token, tenant token, and user token types |
|  | Auto token caching | - | Built-in token caching mechanism to reduce API calls |
|  | Smart token refresh | - | Automatically refreshes tokens before expiration to ensure service continuity |
|  | Multi-tenant support | - | Supports token isolation and management in multi-tenant scenarios |
|  | OAuth flow | - | Complete OAuth authorization flow support for secure user token acquisition |
| **🏢 Organization Management** | User management | V1/V3 | Create, update, query, delete, and batch operate users |
|  | Department management | V1/V3 | Department tree structure maintenance, multi-level department management |
|  | Employee management | V1 | Employee profile and detailed information management |
|  | Job level management | - | Enterprise job hierarchy maintenance, CRUD operations |
|  | Job family management | - | Career path management, job family definition |
|  | Role permissions | - | Enterprise permission role system, role member management |
|  | User group management | - | User group member management, flexible user grouping |
|  | Work city management | - | Multi-city work location maintenance |
| **📱 Message Service** | Message sending | V1 | Supports rich message types: text, image, file, card, etc. |
|  | Batch messaging | V1 | Send messages to multiple users/departments in batch |
|  | Group chat management | - | Group chat creation, member management, group info maintenance |
|  | Message interactions | - | Message emoji reactions, quote replies, and other interactive features |
|  | Task management | - | Task creation, updates, status management, and collaboration features |
| **🛠️ Enterprise Features** | Unified exception handling | - | Comprehensive exception handling mechanism with unified error response format |
|  | Smart retry mechanism | - | Automatic retry for network failures and temporary errors, improving stability |
|  | High-performance caching | - | Resolves cache stampede and race conditions, supports automatic token refresh |
|  | Connection pool management | - | HTTP connection pool reuse, improving API call efficiency |
|  | Async programming support | - | Full async/await support with non-blocking I/O operations |
|  | Detailed logging | - | Structured logging for monitoring and troubleshooting |

> 💡 **Note**: The above only shows feature module examples. For more details, please refer to [Mud.Feishu Detailed Documentation](./Mud.Feishu/README.md)

### 🔄 Mud.Feishu.WebSocket - Real-time Event Subscription Features

| Feature Category | Main Features | Description |
|------------------|----------------|-------------|
| **🤖 Event Handling Architecture** | Strategy pattern design | Extensible event handler architecture supporting custom business logic |
|  | Multi-handler support | Register multiple event handlers to process different event types in parallel |
|  | Single handler mode | Suitable for simple event handling scenarios with single functionality |
|  | Custom handlers | Fully customizable for business requirements, supporting complex scenarios |
|  | Event replay | Supports event replay and recovery mechanisms to ensure data consistency |
| **🫀 Connection Management** | WebSocket connection management | Persistent connection maintenance and monitoring |
|  | Auto reconnection mechanism | Automatically reconnects on disconnection to ensure service continuity |
|  | Heartbeat monitoring | Periodic heartbeat detection to ensure connection is active |
|  | Connection load balancing | Load distribution across multiple connection instances for improved processing capacity |
|  | Graceful shutdown | Supports graceful connection shutdown and resource cleanup |
| **📈 Monitoring & Operations** | Connection status monitoring | Real-time connection count and status monitoring |
|  | Event processing statistics | Event reception count and processing time statistics |
|  | Performance metrics collection | Message processing throughput and latency monitoring |
|  | Health checks | Real-time service health status checking |
|  | Alert support | Automatic alert notifications for abnormal situations |
|  | Detailed audit logs | Complete event processing audit records |

### 🌐 Mud.Feishu.Webhook - HTTP Callback Event Handling Features

| Feature Category | Main Features | Description |
|------------------|----------------|-------------|
| **🔒 Security Verification & Decryption** | Event subscription verification | Supports Feishu URL verification flow |
|  | Request signature verification | Verifies authenticity of Feishu event request signatures |
|  | Timestamp verification | Timestamp check to prevent replay attacks |
|  | AES-256-CBC decryption | Built-in decryption functionality for automatic encrypted event handling |
|  | Source IP verification | Configurable IP whitelist verification |
| **🚀 Event Handling Architecture** | Middleware mode | Seamless integration with ASP.NET Core pipeline |
|  | Auto event routing | Automatically distributes events to corresponding handlers based on event type |
|  | Multiple usage modes | Supports middleware mode, controller mode, and hybrid mode |
|  | Async processing | Fully asynchronous event handling mechanism |
|  | Concurrency control | Configurable concurrent event processing limit |
| **📊 Monitoring & Operations** | Performance monitoring | Optional performance metrics collection and monitoring |
|  | Health checks | Built-in health check endpoints |
|  | Exception handling | Comprehensive exception handling and logging |
|  | Request logging | Detailed request processing logging |


## 🎯 Usage Scenarios

### 🚀 Quick Start Examples

#### HTTP API Call Example

```csharp
// User management Controller example
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IFeishuTenantV3User _userApi;
    private readonly IFeishuTenantV3Departments _deptApi;
    
    public UserController(
        IFeishuTenantV3User userApi,
        IFeishuTenantV3Departments deptApi)
    {
        _userApi = userApi;
        _deptApi = deptApi;
    }
    
    // Create new user
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await _userApi.CreateUserAsync(request);
        return result.Code == 0 ? Ok(result.Data) : BadRequest(result.Msg);
    }
    
    // Get all users under a department
    [HttpGet("departments/{departmentId}/users")]
    public async Task<IActionResult> GetDepartmentUsers(string departmentId)
    {
        var result = await _deptApi.GetUserByDepartmentIdAsync(departmentId);
        return Ok(result.Data);
    }
    
    // Batch get user information
    [HttpPost("users/batch")]
    public async Task<IActionResult> GetUsersBatch([FromBody] string[] userIds)
    {
        var result = await _userApi.GetUserByIdsAsync(userIds);
        return Ok(result.Data);
    }
}
```

#### Message Sending Example

```csharp
public class NotificationService
{
    private readonly IFeishuTenantV1Message _messageApi;
    private readonly IFeishuTenantV1BatchMessage _batchMessageApi;
    
    public NotificationService(
        IFeishuTenantV1Message messageApi,
        IFeishuTenantV1BatchMessage batchMessageApi)
    {
        _messageApi = messageApi;
        _batchMessageApi = batchMessageApi;
    }

    // Send text message to user
    public async Task<string> SendTextMessageAsync(string userId, string content)
    {
        var textContent = new MessageTextContent { Text = content };
        var request = new SendMessageRequest
        {
            ReceiveId = userId,
            MsgType = "text",
            Content = JsonSerializer.Serialize(textContent)
        };

        var result = await _messageApi.SendMessageAsync(request, receive_id_type: "user_id");
        return result.Code == 0 ? result.Data?.MessageId : null;
    }

    // Send system notification in batch
    public async Task<string> SendSystemNotificationAsync(string[] departmentIds, string title, string content)
    {
        var request = new BatchSenderTextMessageRequest
        {
            DeptIds = departmentIds,
            Content = new MessageTextContent
            {
                Text = $"📢 {title}\n\n{content}"
            }
        };

        var result = await _batchMessageApi.BatchSendTextMessageAsync(request);
        return result.Code == 0 ? result.Data?.MessageId : null;
    }
}
```

#### WebSocket Event Handling Example

```csharp
using Mud.Feishu.Abstractions;
using System.Text.Json;

/// <summary>
/// User event handler - implements IFeishuEventHandler interface
/// </summary>
public class DemoUserEventHandler : IFeishuEventHandler
{
    private readonly ILogger<DemoUserEventHandler> _logger;
    private readonly IUserSyncService _syncService;

    public DemoUserEventHandler(ILogger<DemoUserEventHandler> logger, IUserSyncService syncService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
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
            await _syncService.RecordUserEventAsync(userData, cancellationToken);

            // Simulate business processing
            await ProcessUserEventAsync(userData, cancellationToken);

            _logger.LogInformation("User creation event processed successfully: UserID {UserId}, UserName {UserName}",
                userData.UserId, userData.UserName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process user creation event: {EventId}", eventData.EventId);
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
            Phone = TryGetProperty(userElement, "phone") ?? "",
            Avatar = TryGetProperty(userElement, "avatar") ?? "",
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow
        };
    }

    private async Task ProcessUserEventAsync(UserData userData, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting to process user data: {UserId}", userData.UserId);

        // Simulate async business operation
        await Task.Delay(100, cancellationToken);

        // Simulate user data processing: database storage, cache update, notification sending, etc.
        if (string.IsNullOrWhiteSpace(userData.UserId))
        {
            throw new ArgumentException("User ID cannot be empty");
        }

        // Simulate sending welcome notification
        _logger.LogInformation("Sending welcome notification to user: {UserName} ({Email})",
            userData.UserName, userData.Email);

        // Simulate updating statistics
        _syncService.IncrementUserCount();

        await Task.CompletedTask;
    }

    private static string? TryGetProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
    }
}

/// <summary>
/// Department event handler - inherits from DepartmentCreatedEventHandler base class
/// </summary>
public class DemoDepartmentEventHandler : DepartmentCreatedEventHandler
{
    private readonly IDepartmentSyncService _syncService;

    public DemoDepartmentEventHandler(ILogger<DemoDepartmentEventHandler> logger, IDepartmentSyncService syncService)
        : base(logger)
    {
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        ObjectEventResult<DepartmentCreatedResult>? departmentData,
        CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation("Starting to process department creation event: {EventId}", eventData.EventId);

        try
        {
            // Record event to service
            await _syncService.RecordDepartmentEventAsync(departmentData.Object, cancellationToken);

            // Simulate business processing
            await ProcessDepartmentEventAsync(departmentData.Object, cancellationToken);

            _logger.LogInformation("Department creation event processed successfully: DepartmentID {DepartmentId}, DepartmentName {DepartmentName}",
                departmentData.Object.DepartmentId, departmentData.Object.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process department creation event: {EventId}", eventData.EventId);
            throw;
        }
    }

    private async Task ProcessDepartmentEventAsync(DepartmentCreatedResult departmentData, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting to process department data: {DepartmentId}", departmentData.DepartmentId);

        // Simulate async business operation
        await Task.Delay(100, cancellationToken);

        // Simulate validation logic
        if (string.IsNullOrWhiteSpace(departmentData.DepartmentId))
        {
            throw new ArgumentException("Department ID cannot be empty");
        }

        // Simulate permission initialization
        _logger.LogInformation("Initializing department permissions: {DepartmentName}", departmentData.Name);

        // Simulate notifying department manager
        if (!string.IsNullOrWhiteSpace(departmentData.LeaderUserId))
        {
            _logger.LogInformation("Notifying department manager: {LeaderUserId}", departmentData.LeaderUserId);
        }

        // Simulate updating statistics
        _syncService.IncrementDepartmentCount();

        // Simulate hierarchy relationship processing
        if (!string.IsNullOrWhiteSpace(departmentData.ParentDepartmentId))
        {
            _logger.LogInformation("Establishing hierarchy relationship: {DepartmentId} -> {ParentDepartmentId}",
                departmentData.DepartmentId, departmentData.ParentDepartmentId);
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Department delete event handler - inherits from DepartmentDeleteEventHandler base class
/// </summary>
public class DemoDepartmentDeleteEventHandler : DepartmentDeleteEventHandler
{
    public DemoDepartmentDeleteEventHandler(ILogger<DepartmentDeleteEventHandler> logger)
        : base(logger)
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
            _logger.LogWarning("Department delete event entity is null, skipping processing");
            return;
        }

        _logger.LogInformation("Starting to process department delete event: EventId={EventId}, AppId={AppId}, TenantKey={TenantKey}",
            eventData.EventId, eventData.AppId, eventData.TenantKey);

        _logger.LogDebug("Department delete event details: {@EventEntity}", eventEntity);

        // Execute department deletion related business logic
        // For example: clean department cache, update statistics, notify relevant personnel, etc.

        await Task.CompletedTask;
    }
}
```

#### Webhook Event Handling Example

Webhook event handlers use the same `IFeishuEventHandler` interface as WebSocket event handlers, so code can be reused.

```csharp
// User creation event handler - can be used by both Webhook and WebSocket
public class UserCreatedEventHandler : IFeishuEventHandler
{
    private readonly ILogger<UserCreatedEventHandler> _logger;
    private readonly IUserSyncService _syncService;

    public UserCreatedEventHandler(
        ILogger<UserCreatedEventHandler> logger,
        IUserSyncService syncService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
    }

    public string SupportedEventType => "contact.user.created_v3";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse user event data
            var userEvent = JsonSerializer.Deserialize<UserCreatedEvent>(eventData.Event?.ToString() ?? "{}");

            _logger.LogInformation("New user created: {UserName} ({UserId})",
                userEvent.User.Name, userEvent.User.UserId);

            // Sync user to local database
            await _syncService.SyncUserToDatabaseAsync(userEvent.User, cancellationToken);

            // Send welcome message
            await SendWelcomeMessageAsync(userEvent.User.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process user creation event");
            throw;
        }
    }
}

// Message receive event handler
public class MessageReceiveEventHandler : IFeishuEventHandler
{
    private readonly ILogger<MessageReceiveEventHandler> _logger;
    private readonly IFeishuTenantV1Message _messageApi;

    public MessageReceiveEventHandler(
        ILogger<MessageReceiveEventHandler> logger,
        IFeishuTenantV1Message messageApi)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageApi = messageApi ?? throw new ArgumentNullException(nameof(messageApi));
    }

    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            var messageEvent = JsonSerializer.Deserialize<MessageReceiveEvent>(eventData.Event?.ToString() ?? "{}");

            _logger.LogInformation("Message received - Sender: {SenderId}, Content: {Content}",
                messageEvent.Sender.Id, messageEvent.Message.Content);

            // Smart reply logic
            if (messageEvent.Message.Content.Contains("help"))
            {
                await SendHelpMessageAsync(messageEvent.Sender.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message receive event");
            throw;
        }
    }
}
```

## 📖 Detailed Documentation

- [Mud.Feishu.Abstractions Documentation](./Mud.Feishu.Abstractions/README.md) - Event handling abstraction layer guide
- [Mud.Feishu Documentation](./Mud.Feishu/README.md) - HTTP API complete usage guide
- [Mud.Feishu.WebSocket Documentation](./Mud.Feishu.WebSocket/Readme.md) - WebSocket real-time event subscription guide
- [Mud.Feishu.Webhook Documentation](./Mud.Feishu.Webhook/README.md) - Webhook HTTP callback event handling guide

## 🛠️ Technology Stack

### 📚 Framework Support
- **.NET Standard 2.0** - Compatible with .NET Framework 4.6.1+
- **.NET 6.0** - LTS long-term support version
- **.NET 8.0** - LTS long-term support version (recommended)
- **.NET 10.0** - LTS long-term support version

### 🔧 Core Dependencies
- **Mud.ServiceCodeGenerator v1.4.5.3** - HTTP client code generator
- **System.Text.Json v10.0.1** - High-performance JSON serialization (.NET Standard 2.0)
- **Microsoft.Extensions.Http** - HTTP client factory
  - .NET 6.0 / .NET Standard 2.0: v8.0.1
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Http.Polly** - Resilience and transient fault handling
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.DependencyInjection** - Dependency injection
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Logging** - Logging
  - .NET 6.0 / .NET Standard 2.0: v8.0.3
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Configuration.Binder** - Configuration binding
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1

## 📄 License

This project is licensed under the [MIT License](./LICENSE), allowing both commercial and non-commercial use.

## 🔗 Related Links

### 📖 Official Documentation
- [Feishu Open Platform Documentation](https://open.feishu.cn/document/) - Feishu API official documentation and best practices
- [NuGet Package Manager](https://www.nuget.org/) - Official .NET package management platform

### 📦 NuGet Packages
- [Mud.Feishu.Abstractions](https://www.nuget.org/packages/Mud.Feishu.Abstractions/) - Event handling abstraction layer
- [Mud.Feishu](https://www.nuget.org/packages/Mud.Feishu/) - Core HTTP API client library
- [Mud.Feishu.WebSocket](https://www.nuget.org/packages/Mud.Feishu.WebSocket/) - WebSocket real-time event subscription library
- [Mud.Feishu.Webhook](https://www.nuget.org/packages/Mud.Feishu.Webhook/) - Webhook HTTP callback event handling library

### 🛠️ Development Resources
- [Project Repository](https://gitee.com/mudtools/MudFeishu) - Source code and development documentation
- [Mud.ServiceCodeGenerator](https://gitee.com/mudtools/mud-code-generator) - HTTP client code generator
- [Example Projects](./Mud.Feishu.Test) - Complete usage examples and demo code

### 🤝 Community Support
- [Issue Tracker](https://gitee.com/mudtools/MudFeishu/issues) - Bug reports and feature requests
- [Contributing Guide](./CONTRIBUTING.md) - How to contribute to the project
- [Changelog](./CHANGELOG.md) - Version updates and change notes
