
# 飞书WebSocket客户端服务

该服务提供了与飞书事件订阅的WebSocket长连接功能，具有良好的可扩展性和企业级连接处理能力。

## 功能特点

- 自动连接管理和重连机制
- 心跳检测保持连接活跃
- 消息队列处理，支持高并发
- 企业级错误处理和日志记录
- 可配置的连接参数和行为
- 后台服务自动启动和管理
- 完整的事件通知机制
- 完整的实体类支持（PingMessage、PongMessage、EventMessage、AuthResponseMessage等）
- 智能重连策略和状态监控
- 分片消息处理和完整消息组装
- 带认证的连接和认证状态跟踪
- 灵活的消息处理机制和错误隔离

## 安装和配置

### 1. 注册服务

#### 使用配置文件

```csharp
// 在 Program.cs 或 Startup.cs 中
builder.Services.AddFeishuWebSocketService(builder.Configuration);
```

#### 使用代码配置

```csharp
// 在 Program.cs 或 Startup.cs 中
builder.Services.AddFeishuWebSocketService(
    feishuOptions =>
    {
        feishuOptions.AppId = "your_app_id";
        feishuOptions.AppSecret = "your_app_secret";
    },
    webSocketOptions =>
    {
        webSocketOptions.AutoReconnect = true;
        webSocketOptions.MaxReconnectAttempts = 5;
        webSocketOptions.ReconnectDelayMs = 5000;
        webSocketOptions.HeartbeatIntervalMs = 30000;
    });
```

### 2. 配置文件设置

在 `appsettings.json` 中添加以下配置：

```json
{
  "Feishu": {
    "AppId": "your_app_id",
    "AppSecret": "your_app_secret",
    "WebSocket": {
      "AutoReconnect": true,
      "MaxReconnectAttempts": 5,
      "ReconnectDelayMs": 5000,
      "ReceiveBufferSize": 4096,
      "HeartbeatIntervalMs": 30000,
      "ConnectionTimeoutMs": 10000,
      "EnableLogging": true,
      "EnableMessageQueue": true,
      "MessageQueueCapacity": 1000
    }
  }
}
```

## 使用方法

### 1. 注入WebSocket管理器

```csharp
public class MyService
{
    private readonly IFeishuWebSocketManager _webSocketManager;

    public MyService(IFeishuWebSocketManager webSocketManager)
    {
        _webSocketManager = webSocketManager;

        // 订阅事件
        _webSocketManager.Connected += OnConnected;
        _webSocketManager.Disconnected += OnDisconnected;
        _webSocketManager.MessageReceived += OnMessageReceived;
        _webSocketManager.Error += OnError;
    }

    private void OnConnected(object? sender, EventArgs e)
    {
        Console.WriteLine("WebSocket连接已建立");
    }

    private void OnDisconnected(object? sender, WebSocketCloseEventArgs e)
    {
        Console.WriteLine($"WebSocket连接已断开: {e.CloseStatus} - {e.CloseStatusDescription}");
    }

    private void OnMessageReceived(object? sender, WebSocketMessageEventArgs e)
    {
        Console.WriteLine($"接收到消息: {e.Message}");
        // 处理接收到的消息
    }

    private void OnError(object? sender, WebSocketErrorEventArgs e)
    {
        Console.WriteLine($"WebSocket错误: {e.ErrorMessage}");
        // 处理错误
    }

    public async Task SendMessageAsync(string message)
    {
        await _webSocketManager.SendMessageAsync(message);
    }
}
```

### 2. 手动控制连接

```csharp
// 启动连接
await _webSocketManager.StartAsync();

// 检查连接状态
if (_webSocketManager.IsConnected)
{
    // 发送消息
    await _webSocketManager.SendMessageAsync("your_message");
}

// 重新连接
await _webSocketManager.ReconnectAsync();

// 停止连接
await _webSocketManager.StopAsync();
```

## 配置选项说明

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| AutoReconnect | bool | true | 是否启用自动重连 |
| MaxReconnectAttempts | int | 5 | 最大重连次数 |
| ReconnectDelayMs | int | 5000 | 重连延迟时间（毫秒） |
| ReceiveBufferSize | int | 4096 | 接收缓冲区大小（字节） |
| HeartbeatIntervalMs | int | 30000 | 心跳间隔时间（毫秒） |
| ConnectionTimeoutMs | int | 10000 | 连接超时时间（毫秒） |
| EnableLogging | bool | true | 是否启用日志记录 |
| EnableMessageQueue | bool | true | 是否启用消息队列处理 |
| MessageQueueCapacity | int | 1000 | 消息队列最大容量 |

## 事件处理

WebSocket客户端提供了以下事件：

- `Connected`: 连接建立时触发
- `Disconnected`: 连接断开时触发
- `MessageReceived`: 接收到消息时触发
- `Error`: 发生错误时触发

## 高级用法

### 自定义消息处理

可以通过实现自定义的消息处理器来处理特定类型的消息：

```csharp
public class CustomMessageHandler
{
    public async Task HandleMessageAsync(string message)
    {
        // 解析消息
        var messageObject = JsonSerializer.Deserialize<YourMessageType>(message);

        // 根据消息类型处理
        switch (messageObject?.Type)
        {
            case "event_type_1":
                await HandleEventType1Async(messageObject);
                break;
            case "event_type_2":
                await HandleEventType2Async(messageObject);
                break;
            // 其他类型处理
        }
    }

    private async Task HandleEventType1Async(YourMessageType message)
    {
        // 处理类型1的消息
    }

    private async Task HandleEventType2Async(YourMessageType message)
    {
        // 处理类型2的消息
    }
}
```

### 多实例部署

在多实例部署环境中，每个实例都会创建自己的WebSocket连接。为了确保消息不重复处理，可以使用分布式锁或消息去重机制：

```csharp
public class DistributedMessageHandler
{
    private readonly IDistributedLockProvider _lockProvider;
    private readonly IMessageProcessor _messageProcessor;

    public DistributedMessageHandler(
        IDistributedLockProvider lockProvider,
        IMessageProcessor messageProcessor)
    {
        _lockProvider = lockProvider;
        _messageProcessor = messageProcessor;
    }

    public async Task HandleMessageAsync(string messageId, string message)
    {
        // 使用消息ID作为锁键
        var lockKey = $"feishu_message_{messageId}";

        // 尝试获取分布式锁
        await using (await _lockProvider.AcquireLockAsync(lockKey, TimeSpan.FromMinutes(1)))
        {
            // 获取锁成功，处理消息
            await _messageProcessor.ProcessAsync(message);
        }
        // 如果获取锁失败，说明其他实例正在处理该消息，直接返回
    }
}
```

## 性能优化建议

### 1. 消息处理优化

- **异步处理**: 确保所有消息处理逻辑都是异步的，避免阻塞WebSocket接收线程
- **并行处理**: 利用消息处理器并行执行特性，对不同类型的消息使用不同的处理器
- **批量处理**: 对于大量相似消息，考虑使用批量处理机制
- **错误隔离**: 确保单个消息处理器的异常不会影响其他处理器

```csharp
// 示例：注册多个专用消息处理器
webSocketManager.Client.RegisterMessageProcessor(HandleMessageEventAsync);
webSocketManager.Client.RegisterMessageProcessor(HandleUserEventAsync);
webSocketManager.Client.RegisterMessageProcessor(HandleApprovalEventAsync);
```

### 2. 连接管理优化

- **心跳间隔**: 根据网络环境调整心跳间隔，通常建议15-30秒
- **重连策略**: 合理设置重连延迟和最大重连次数，避免频繁重连
- **连接池**: 在高并发场景下，考虑使用连接池管理多个WebSocket连接

### 3. 内存管理

- **消息队列**: 根据业务需求调整消息队列容量，避免内存溢出
- **资源释放**: 确保在应用关闭时正确释放所有WebSocket资源
- **定期清理**: 定期清理过期的消息和缓存数据

### 4. 监控和日志

- **性能监控**: 实现关键性能指标的监控，如消息处理延迟、队列大小等
- **结构化日志**: 使用结构化日志记录关键事件和错误信息
- **告警机制**: 设置关键错误的告警机制，及时发现和解决问题

## 常见问题

### 1. 连接频繁断开

可能原因：
- 网络不稳定
- 心跳间隔设置过长
- 服务器端主动断开连接

解决方案：
- 调整心跳间隔时间，建议设置为30秒以内
- 启用自动重连功能
- 检查网络环境

### 2. 消息处理不及时

可能原因：
- 消息处理逻辑耗时过长
- 消息队列容量不足

解决方案：
- 优化消息处理逻辑，使用异步处理
- 增加消息队列容量
- 使用多个消费者处理消息

### 3. 内存占用过高

可能原因：
- 消息队列中积压过多消息
- 未正确释放资源

解决方案：
- 及时处理消息，避免积压
- 确保在应用关闭时正确释放WebSocket资源
- 监控内存使用情况

## 实体功能完善

### 心跳消息实体使用

系统现在使用 `PingMessage` 类替代之前的匿名对象，提供完整的心跳消息结构：

```csharp
var pingMessage = new PingMessage
{
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
};
var heartbeatMessage = JsonSerializer.Serialize(pingMessage);
```

### Pong 响应处理

系统现在能够处理服务器的 Pong 消息，并提供延迟计算：

```csharp
// 新增事件定义
public event EventHandler<WebSocketPongEventArgs>? PongReceived;

// 延迟计算
long? latencyMs = null;
if (pongMessage?.Timestamp > 0)
{
    var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    latencyMs = (currentTime - pongMessage.Timestamp) * 1000;
}
```

### 事件消息解析和处理

系统现在能够解析和处理飞书事件消息，支持多种事件类型：

```csharp
// 事件类型常量
public static class FeishuEventTypes
{
    // 消息相关事件
    public const string ReceiveMessage = "im.message.receive_v1";
    public const string MessageRead = "im.message.message_read_v1";

    // 群聊相关事件
    public const string UserAddedToGroup = "im.chat.member.user_added_v1";
    public const string UserRemovedFromGroup = "im.chat.member.user_deleted_v1";
    public const string GroupUpdated = "im.chat.updated_v1";

    // 用户相关事件
    public const string UserCreated = "contact.user.created_v3";
    public const string UserUpdated = "contact.user.updated_v3";
    public const string UserDeleted = "contact.user.deleted_v3";

    // 部门相关事件
    public const string DepartmentCreated = "contact.department.created_v3";
    public const string DepartmentUpdated = "contact.department.updated_v3";
    public const string DepartmentDeleted = "contact.department.deleted_v3";

    // 审批相关事件
    public const string ApprovalApproved = "approval.approval.approved_v1";
    public const string ApprovalRejected = "approval.approval.rejected_v1";

    // 日程相关事件
    public const string CalendarEvent = "calendar.event.updated_v4";

    // 会议相关事件
    public const string MeetingStart = "meeting.meeting.started_v1";
    public const string MeetingEnd = "meeting.meeting.ended_v1";
}
```

### 认证响应处理

系统现在能够处理认证响应消息，并根据响应码设置认证状态：

```csharp
if (authResponse?.Code == 0)
{
    _isAuthenticated = true;
    Authenticated?.Invoke(this, EventArgs.Empty);
}
else
{
    _isAuthenticated = false;
    Error?.Invoke(this, new WebSocketErrorEventArgs
    {
        ErrorMessage = $"WebSocket认证失败: {authResponse?.Code} - {authResponse?.Message}",
        IsAuthError = true
    });
}
```

### 消息类型识别和路由机制

系统现在有统一的消息类型识别和路由机制：

```csharp
private async Task ProcessMessageByTypeAsync(string message)
{
    using var jsonDoc = JsonDocument.Parse(message);
    if (!jsonDoc.RootElement.TryGetProperty("type", out var typeElement)) return;

    var messageType = typeElement.GetString()?.ToLowerInvariant();

    switch (messageType)
    {
        case "ping": await HandlePingMessageAsync(message); break;
        case "pong": await HandlePongMessageAsync(message); break;
        case "event": await HandleEventMessageAsync(message); break;
        case "auth": await HandleAuthResponseMessageAsync(message); break;
    }
}
```

## 增强功能

### 完善的自动重连机制

- **智能重连策略**: 在 FeishuWebSocketHostedService 中实现了完整的重连逻辑
- **重连计数**: 支持配置最大重连次数和重连延迟
- **状态监控**: 定期检查连接状态，自动触发重连

### 后台服务自动管理

FeishuWebSocketHostedService 是一个后台服务，用于自动启动和管理WebSocket连接：

- **自动启动**: 应用启动时自动建立WebSocket连接
- **生命周期管理**: 随应用启动而启动，随应用停止而停止
- **状态监控**: 定期检查连接状态，自动处理断线重连
- **事件通知**: 提供连接状态变化的事件通知

```csharp
// 后台服务会自动注册，无需手动启动
// 在应用启动时，后台服务会自动执行以下操作：
// 1. 获取应用访问令牌
// 2. 获取WebSocket端点
// 3. 建立WebSocket连接并进行认证
// 4. 启动心跳检测
// 5. 开始监听消息
```

### 连接状态管理

- **连接状态跟踪**: 实时跟踪WebSocket连接状态
- **认证状态管理**: 管理WebSocket认证状态和令牌刷新
- **错误恢复**: 自动处理连接错误并尝试恢复
- **资源释放**: 应用关闭时正确释放WebSocket资源

### 消息缓冲区处理优化

- **分片消息处理**: 正确处理 WebSocket 分片消息，避免消息截断
- **完整消息组装**: 使用 StringBuilder 正确组装完整的文本消息
- **二进制消息支持**: 支持二进制消息类型的基础处理

### 完整的认证流程

- **带认证的连接**: 新增 `ConnectAsync(endpoint, appAccessToken)` 方法
- **认证消息**: 实现标准的飞书 WebSocket 认证流程
- **认证状态跟踪**: 提供 `IsAuthenticated` 属性跟踪认证状态
- **认证事件**: 新增 `Authenticated` 事件通知认证成功

### 灵活的消息处理机制

- **消息处理器注册**: 支持注册多个自定义消息处理器
- **并行处理**: 多个消息处理器并行执行，提高处理效率
- **错误隔离**: 单个处理器异常不影响其他处理器运行
- **队列管理**: 完善的消息队列管理，防止内存溢出

### 增强的错误处理和日志

- **详细错误信息**: WebSocketErrorEventArgs 提供详细的错误上下文
- **错误分类**: 区分网络错误、认证错误等不同类型的错误
- **时间戳记录**: 所有事件都包含精确的时间戳
- **连接状态**: 错误事件中包含当前连接状态信息

## 新增的API

### IFeishuWebSocketClient 接口新增方法

```csharp
// 带认证的连接方法
Task ConnectAsync(WsEndpointResult endpoint, string appAccessToken, CancellationToken cancellationToken = default);

// 消息处理器管理
void RegisterMessageProcessor(Func<string, Task> processor);
bool UnregisterMessageProcessor(Func<string, Task> processor);

// 认证状态属性
bool IsAuthenticated { get; }

// 认证成功事件
event EventHandler<EventArgs>? Authenticated;

// WebSocket状态
WebSocketState State { get; }

// 事件
event EventHandler<EventArgs>? Connected;
event EventHandler<WebSocketCloseEventArgs>? Disconnected;
event EventHandler<WebSocketMessageEventArgs>? MessageReceived;
event EventHandler<WebSocketErrorEventArgs>? Error;
event EventHandler<WebSocketPingEventArgs>? PingReceived;
event EventHandler<WebSocketPongEventArgs>? PongReceived;
event EventHandler<WebSocketFeishuEventArgs>? FeishuEventReceived;

// 方法
Task ConnectAsync(WsEndpointResult endpoint, CancellationToken cancellationToken = default);
Task DisconnectAsync(CancellationToken cancellationToken = default);
Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
```

### IFeishuWebSocketManager 接口说明

```csharp
// 属性
IFeishuWebSocketClient Client { get; }
bool IsConnected { get; }

// 事件
event EventHandler<EventArgs>? Connected;
event EventHandler<WebSocketCloseEventArgs>? Disconnected;
event EventHandler<WebSocketMessageEventArgs>? MessageReceived;
event EventHandler<WebSocketErrorEventArgs>? Error;

// 方法
Task StartAsync(CancellationToken cancellationToken = default);
Task StopAsync(CancellationToken cancellationToken = default);
Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
Task ReconnectAsync(CancellationToken cancellationToken = default);
```

### 增强的事件参数

#### WebSocketCloseEventArgs
```csharp
public class WebSocketCloseEventArgs : EventArgs
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public bool IsServerInitiated { get; set; }
    public TimeSpan? ConnectionDuration { get; set; }
    public WebSocketCloseStatus? CloseStatus { get; set; }
    public string? CloseStatusDescription { get; set; }
}
```

#### WebSocketMessageEventArgs
```csharp
public class WebSocketMessageEventArgs : EventArgs
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public int MessageSize { get; set; }
    public int QueueCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public WebSocketMessageType MessageType { get; set; }
    public bool EndOfMessage { get; set; }
}
```

#### WebSocketErrorEventArgs
```csharp
public class WebSocketErrorEventArgs : EventArgs
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? ErrorType { get; set; }
    public WebSocketState ConnectionState { get; set; }
    public bool IsNetworkError { get; set; }
    public bool IsAuthError { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}
```

#### WebSocketPingEventArgs
```csharp
public class WebSocketPingEventArgs : EventArgs
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public PingMessage? PingMessage { get; set; }
}
```

#### WebSocketPongEventArgs
```csharp
public class WebSocketPongEventArgs : EventArgs
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public PongMessage? PongMessage { get; set; }
    public long? LatencyMs { get; set; }
}
```

#### WebSocketFeishuEventArgs
```csharp
public class WebSocketFeishuEventArgs : EventArgs
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public EventMessage? EventMessage { get; set; }
    public EventData? EventData => EventMessage?.Data;
    public string? EventType => EventData?.EventType;
    public string? AppId => EventData?.AppId;
    public string? TenantKey => EventData?.TenantKey;
    public string? RawEvent { get; set; }
}
```

## 使用示例

### 基本使用（带自动认证）

```csharp
// 注册服务
builder.Services.AddFeishuWebSocketService(builder.Configuration);

// 在服务中使用
public class MyWebSocketService
{
    private readonly IFeishuWebSocketManager _webSocketManager;

    public MyWebSocketService(IFeishuWebSocketManager webSocketManager)
    {
        _webSocketManager = webSocketManager;

        // 订阅事件
        _webSocketManager.Connected += OnConnected;
        _webSocketManager.Disconnected += OnDisconnected;
        _webSocketManager.MessageReceived += OnMessageReceived;
        _webSocketManager.Error += OnError;
    }

    private void OnConnected(object? sender, EventArgs e)
    {
        Console.WriteLine("WebSocket 连接已建立");
    }

    private void OnDisconnected(object? sender, WebSocketCloseEventArgs e)
    {
        Console.WriteLine($"WebSocket 连接已断开: {e.CloseStatus} - " +
                        $"{e.CloseStatusDescription} (服务器端: {e.IsServerInitiated})");
    }

    private void OnMessageReceived(object? sender, WebSocketMessageEventArgs e)
    {
        Console.WriteLine($"接收消息: {e.Message} (大小: {e.MessageSize}字节, " +
                        $"队列: {e.QueueCount}条)");
    }

    private void OnError(object? sender, WebSocketErrorEventArgs e)
    {
        Console.WriteLine($"WebSocket 错误: {e.ErrorMessage} " +
                        $"(类型: {e.ErrorType}, 网络: {e.IsNetworkError}, " +
                        $"认证: {e.IsAuthError})");
    }
}
```

### 注册自定义消息处理器

```csharp
public class CustomMessageProcessor
{
    public async Task ProcessMessageAsync(string message)
    {
        // 解析消息
        var messageData = JsonSerializer.Deserialize<YourMessageType>(message);

        // 根据消息类型处理
        switch (messageData?.Type)
        {
            case "message":
                await HandleMessageEventAsync(messageData);
                break;
            case "user":
                await HandleUserEventAsync(messageData);
                break;
        }
    }

    private async Task HandleMessageEventAsync(YourMessageType message)
    {
        // 处理消息事件
        Console.WriteLine($"处理消息事件: {message}");
        await Task.CompletedTask;
    }

    private async Task HandleUserEventAsync(YourMessageType message)
    {
        // 处理用户事件
        Console.WriteLine($"处理用户事件: {message}");
        await Task.CompletedTask;
    }
}

// 注册处理器
var processor = new CustomMessageProcessor();
_webSocketManager.Client.RegisterMessageProcessor(processor.ProcessMessageAsync);
```

### 高级配置

```csharp
// 配置 WebSocket 选项
builder.Services.AddFeishuWebSocketService(
    feishuOptions =>
    {
        feishuOptions.AppId = "your_app_id";
        feishuOptions.AppSecret = "your_app_secret";
    },
    webSocketOptions =>
    {
        webSocketOptions.AutoReconnect = true;
        webSocketOptions.MaxReconnectAttempts = 10;
        webSocketOptions.ReconnectDelayMs = 3000;
        webSocketOptions.HeartbeatIntervalMs = 25000;
        webSocketOptions.ReceiveBufferSize = 8192;
        webSocketOptions.ConnectionTimeoutMs = 15000;
        webSocketOptions.EnableLogging = true;
        webSocketOptions.EnableMessageQueue = true;
        webSocketOptions.MessageQueueCapacity = 2000;
    });
```

## 示例项目

### 完整示例项目

以下是一个完整的示例项目，展示如何使用Mud.Feishu.WebSocket库处理飞书事件：

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 注册飞书WebSocket服务
builder.Services.AddFeishuWebSocketService(builder.Configuration);

// 添加自定义消息处理器
builder.Services.AddSingleton<CustomMessageHandler>();

var app = builder.Build();

// 启动应用
app.Run();
```

```csharp
// CustomMessageHandler.cs
public class CustomMessageHandler
{
    private readonly ILogger<CustomMessageHandler> _logger;

    public CustomMessageHandler(ILogger<CustomMessageHandler> logger, IFeishuWebSocketManager webSocketManager)
    {
        _logger = logger;

        // 订阅WebSocket事件
        webSocketManager.Connected += OnConnected;
        webSocketManager.Disconnected += OnDisconnected;
        webSocketManager.MessageReceived += OnMessageReceived;
        webSocketManager.Error += OnError;
        webSocketManager.Client.FeishuEventReceived += OnFeishuEventReceived;

        // 注册消息处理器
        webSocketManager.Client.RegisterMessageProcessor(ProcessMessageAsync);
    }

    private void OnConnected(object? sender, EventArgs e)
    {
        _logger.LogInformation("WebSocket连接已建立");
    }

    private void OnDisconnected(object? sender, WebSocketCloseEventArgs e)
    {
        _logger.LogInformation("WebSocket连接已断开: {Status} - {Description}", 
            e.CloseStatus, e.CloseStatusDescription);
    }

    private void OnMessageReceived(object? sender, WebSocketMessageEventArgs e)
    {
        _logger.LogInformation("接收到消息: {Message}", e.Message);
    }

    private void OnError(object? sender, WebSocketErrorEventArgs e)
    {
        _logger.LogError(e.Exception, "WebSocket错误: {Message}", e.ErrorMessage);
    }

    private void OnFeishuEventReceived(object? sender, WebSocketFeishuEventArgs e)
    {
        _logger.LogInformation("接收到飞书事件: {EventType}, 应用ID: {AppId}", 
            e.EventType, e.AppId);

        // 根据事件类型处理
        switch (e.EventType)
        {
            case FeishuEventTypes.ReceiveMessage:
                HandleReceiveMessage(e);
                break;
            case FeishuEventTypes.UserCreated:
                HandleUserCreated(e);
                break;
            // 其他事件处理...
        }
    }

    private void HandleReceiveMessage(WebSocketFeishuEventArgs e)
    {
        // 处理接收到的消息
        _logger.LogInformation("处理接收消息事件: {Event}", e.RawEvent);
    }

    private void HandleUserCreated(WebSocketFeishuEventArgs e)
    {
        // 处理用户创建事件
        _logger.LogInformation("处理用户创建事件: {Event}", e.RawEvent);
    }

    private async Task ProcessMessageAsync(string message)
    {
        // 自定义消息处理逻辑
        _logger.LogInformation("处理消息: {Message}", message);
        await Task.CompletedTask;
    }
}
```

## 版本历史

### v1.0.0
- 初始版本发布
- 实现基本的WebSocket连接和消息处理
- 支持自动重连和心跳检测
- 提供完整的事件订阅机制

## 许可证

本项目遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。

完整的示例项目可以在 `Mud.Feishu.Test` 中找到，其中包含了WebSocket客户端的使用示例和最佳实践。
