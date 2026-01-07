# Mud.Feishu.Webhook

A webhook component for Feishu event subscription and handling, providing complete Feishu event receiving, validation, decryption, and distribution functionality.

**🚀 New Feature: Minimal API** - Complete service registration with one line of code, ready to use!

## Features

- ✅ **Minimal API**: Complete service registration with one line of code, ready to use
- ✅ **Flexible Configuration**: Supports configuration files, code configuration, and builder pattern
- ✅ **Automatic Event Routing**: Automatically distributes events to corresponding handlers based on event type
- ✅ **Security Validation**: Supports event subscription validation, request signature validation, and timestamp validation
- ✅ **Encryption/Decryption**: Built-in AES-256-CBC decryption, automatically handles Feishu encrypted events
- ✅ **Multiple Usage Modes**: Supports middleware mode, controller mode, and hybrid mode
- ✅ **Dependency Injection**: Fully integrated with .NET dependency injection container
- ✅ **Exception Handling**: Comprehensive exception handling and logging
- ✅ **Performance Monitoring**: Optional performance metrics collection and monitoring
- ✅ **Health Checks**: Built-in health check endpoint
- ✅ **Async Processing**: Fully async event handling mechanism
- ✅ **Concurrency Control**: Configurable concurrent event processing limit

## Quick Start

### 1. Install NuGet Package

```bash
dotnet add package Mud.Feishu.Webhook
```

### 2. Minimal Configuration (One Line)

In `Program.cs`:

```csharp
using Mud.Feishu.Webhook;

var builder = WebApplication.CreateBuilder(args);

// One line to register Webhook service (requires at least one event handler)
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook(); // Add middleware
app.Run();
```

### 3. Complete Configuration (Add Event Handlers)

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .AddHandler<UserEventHandler>()
    .EnableControllers()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook();
app.MapControllers(); // Controller routing
app.Run();
```

### 4. Configuration File

```json
{
  "FeishuWebhook": {
    "VerificationToken": "your_verification_token",
    "EncryptKey": "your_encrypt_key",
    "RoutePrefix": "feishu/Webhook",
    "AutoRegisterEndpoint": true,
    "EnableRequestLogging": true,
    "EnableExceptionHandling": true,
    "EventHandlingTimeoutMs": 30000,
    "MaxConcurrentEvents": 10,
    "EnablePerformanceMonitoring": false,
    "AllowedHttpMethods": [ "POST" ],
    "MaxRequestBodySize": 10485760,
    "ValidateSourceIP": false,
    "AllowedSourceIPs": []
  }
}
```

## 🏗️ Service Registration Methods

### 🚀 Register from Configuration File (Recommended)

```csharp
// One line to complete basic configuration (requires at least one event handler)
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .Build();

// Add event handlers
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .AddHandler<UserCreatedEventHandler>()
    .EnableControllers()
    .Build();
```

### ⚙️ Code Configuration

```csharp
builder.Services.AddFeishuWebhookServiceBuilder(options =>
{
    options.VerificationToken = "your_verification_token";
    options.EncryptKey = "your_encrypt_key";
    options.RoutePrefix = "feishu/Webhook";
    options.EnableRequestLogging = true;
}).AddHandler<MessageEventHandler>()
    .Build();
```

### 🔧 Advanced Builder Pattern

```csharp
builder.Services.AddFeishuWebhookBuilder()
    .ConfigureFrom(configuration)
    .EnableControllers()
    .EnableHealthChecks()
    .EnableMetrics()
    .AddHandler<MessageReceiveEventHandler>()
    .Build();
```

## Usage Modes

### Middleware Mode (Recommended)

```csharp
builder.Services.AddFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook(); // Automatically handles requests under route prefix
app.Run();
```

### Controller Mode

```csharp
builder.Services.AddFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .EnableControllers() // Enable controller support
    .Build();

var app = builder.Build();
app.UseFeishuWebhook();  // Can use both middleware and controllers
app.MapControllers(); // Use controller routing
app.Run();
```

## Creating Event Handlers

```csharp
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;

public class MessageEventHandler : IFeishuEventHandler
{
    private readonly ILogger<MessageEventHandler> _logger;

    public MessageEventHandler(ILogger<MessageEventHandler> logger)
    {
        _logger = logger;
    }

    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received message event: {EventId}", eventData.EventId);
        
        // Handle message logic
        var messageData = JsonSerializer.Deserialize<object>(
            eventData.Event?.ToString() ?? string.Empty);
        
        // Your business logic...
        
        await Task.CompletedTask;
    }
}
```

## Configuration Options

### Basic Configuration

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `VerificationToken` | string | - | Feishu event subscription verification token |
| `EncryptKey` | string | - | Feishu event encryption key |
| `RoutePrefix` | string | "feishu/Webhook" | Webhook route prefix |
| `AutoRegisterEndpoint` | bool | true | Whether to auto-register endpoint |

### Security Configuration

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ValidateSourceIP` | bool | false | Whether to validate source IP |
| `AllowedSourceIPs` | HashSet\<string\> | - | Allowed source IP addresses |
| `AllowedHttpMethods` | HashSet\<string\> | ["POST"] | Allowed HTTP methods |
| `MaxRequestBodySize` | long | 10MB | Max request body size |

### Performance Configuration

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `MaxConcurrentEvents` | int | 10 | Max concurrent events |
| `EventHandlingTimeoutMs` | int | 30000 | Event handling timeout (milliseconds) |
| `EnablePerformanceMonitoring` | bool | false | Whether to enable performance monitoring |

### Logging Configuration

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnableRequestLogging` | bool | true | Whether to enable request logging |
| `EnableExceptionHandling` | bool | true | Whether to enable exception handling |

## Registering Handlers

```csharp
// Add handlers using chain calls
builder.Services.AddFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .AddHandler<UserEventHandler>()
    .AddHandler<DepartmentEventHandler>()
    .Build();

// Use builder pattern for complex configuration
builder.Services.AddFeishuWebhookBuilder()
    .ConfigureFrom(configuration)
    .AddHandler<MessageEventHandler>()
    .AddHandler<UserEventHandler>()
    .EnableControllers()
    .Build();
```

## Supported Event Types

The library supports all Feishu event types, including but not limited to:

- `im.message.receive_v1` - Receive message
- `im.chat.member_user_added_v1` - User joins group chat
- `im.chat.member_user_deleted_v1` - User leaves group chat
- `contact.user.created_v3` - User created
- `contact.user.updated_v3` - User updated
- `contact.user.deleted_v3` - User deleted

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

## Monitoring and Diagnostics

### Performance Monitoring

```csharp
// Method 1: Enable via builder pattern
builder.Services.AddFeishuWebhookBuilder()
    .ConfigureFrom(configuration)
    .EnableMetrics()
    .Build();

// Method 2: Enable via configuration options
builder.Services.CreateFeishuWebhookServiceBuilder(options =>
{
    options.EnablePerformanceMonitoring = true; // Enable performance monitoring
}).AddHandler<MessageEventHandler>()
    .Build();
```

### Health Checks

```csharp
// Enable health checks using builder pattern
builder.Services.CreateFeishuWebhookBuilder()
    .ConfigureFrom(configuration)
    .EnableHealthChecks()
    .Build();

// Add health check endpoint
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/health"); // Health check endpoint
```

### Logging

The library uses the standard .NET logging framework, and you can configure different log levels:

```json
{
  "Logging": {
    "LogLevel": {
      "Mud.Feishu.Webhook": "Information",
      "Mud.Feishu.Webhook.Services": "Debug"
    }
  }
}
```

## Best Practices

### 1. Error Handling

```csharp
public class RobustEventHandler : IFeishuEventHandler
{
    private readonly ILogger<RobustEventHandler> _logger;

    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            // Business logic
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling event: {EventId}", eventData.EventId);
            // Do not rethrow exceptions to avoid affecting other handlers
        }
    }
}
```

### 2. Async Processing

```csharp
public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
{
    // Use async APIs
    await ProcessMessageAsync(eventData, cancellationToken);
    
    // Avoid blocking calls
    // Do not use .Result or .Wait()
}
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

4. **Event Handling Failed**
   - Check if event handlers are correctly registered
   - View detailed error information in logs

### Debugging Tips

```csharp
// Enable verbose logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Enable request logging
builder.Services.CreateFeishuWebhookServiceBuilder(options =>
{
    options.EnableRequestLogging = true;
    options.EnablePerformanceMonitoring = true;
}).AddHandler<MessageEventHandler>()
    .Build();
```

## Quick Reference

### Most Common Registration Methods

```csharp
// Method 1: Minimal (requires at least one event handler)
builder.Services.CreateFeishuWebhookServiceBuilder(configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .Build();

// Method 2: Minimal + Handlers
builder.Services.CreateFeishuWebhookServiceBuilder(configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .EnableControllers()
    .Build();

// Method 3: Code Configuration
builder.Services.CreateFeishuWebhookServiceBuilder(options => {
    options.VerificationToken = "your_token";
    options.EncryptKey = "your_key";
}).AddHandler<MessageEventHandler>()
    .Build();

// Method 4: Builder Pattern (complex configuration)
builder.Services.CreateFeishuWebhookBuilder()
    .ConfigureFrom(configuration)
    .EnableMetrics()
    .AddHandler<Handler>()
    .Build();
```

---

**🚀 Get started with Feishu Webhook now and build a stable, reliable event handling system!**
