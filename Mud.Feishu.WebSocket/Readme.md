# 飞书WebSocket客户端服务

企业级飞书事件订阅WebSocket客户端，提供可靠的连接管理、自动重连和策略模式事件处理。

## ✨ 核心特性

- 🔄 **智能连接管理** - 自动重连、心跳检测、状态监控
- 🫀 **心跳消息处理** - 支持飞书 heartbeat 消息类型，实时连接状态监控
- 🚀 **高性能消息处理** - 异步处理、消息队列、并行执行
- 🎯 **策略模式事件处理** - 可扩展的事件处理器架构
- 🛡️ **企业级稳定性** - 完善的错误处理、资源管理、日志记录
- ⚙️ **灵活配置** - 丰富的配置选项、依赖注入支持
- 📊 **监控友好** - 详细的事件通知、性能指标、心跳统计

## 🚀 快速开始

### 1. 安装和注册服务

#### 方式一：建造者模式（推荐）

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 使用建造者模式注册飞书WebSocket服务
builder.Services.AddFeishuWebSocketBuilder()
    .ConfigureFrom(builder.Configuration)           // 从配置文件读取
    .UseMultiHandler()                              // 启用多处理器模式
    .AddHandler<ReceiveMessageEventHandler>()        // 添加消息处理器
    .AddHandler<UserCreatedEventHandler>()           // 添加用户事件处理器
    .Build();                                       // 构建服务注册

var app = builder.Build();
app.Run();
```

#### 方式二：简化注册

```csharp
// 单处理器模式
builder.Services.AddFeishuWebSocketServiceWithSingleHandler<ReceiveMessageEventHandler>(
    options => {
        options.AutoReconnect = true;
        options.MaxReconnectAttempts = 5;
        options.HeartbeatIntervalMs = 30000;
    });

// 或多处理器模式
builder.Services.AddFeishuWebSocketServiceWithMultiHandler<ReceiveMessageEventHandler, UserCreatedEventHandler>(
    options => {
        options.AutoReconnect = true;
        options.EnableLogging = true;
    });

// 或从配置文件注册
builder.Services.AddFeishuWebSocketService(builder.Configuration);
```

### 2. 配置文件

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

### 3. 基本使用

#### 方式一：使用WebSocket客户端（推荐）

```csharp
public class MessageService
{
    private readonly FeishuWebSocketClient _webSocketClient;

    public MessageService(
        ILogger<MessageService> logger,
        IFeishuEventHandlerFactory eventHandlerFactory,
        ILoggerFactory loggerFactory)
    {
        // 配置选项
        var options = new FeishuWebSocketOptions
        {
            EnableLogging = true,
            EnableBinaryMessageProcessing = true,
            EnableAutoAck = true,
            HeartbeatIntervalMs = 30000,
            EnableMessageQueue = true
        };

        // 创建WebSocket客户端
        _webSocketClient = new FeishuWebSocketClient(
            logger,
            eventHandlerFactory,
            loggerFactory,
            options);
        
        // 订阅连接事件
        _webSocketClient.Connected += OnConnected;
        _webSocketClient.Disconnected += OnDisconnected;
        _webSocketClient.Error += OnError;
        _webSocketClient.FeishuEventReceived += OnFeishuEventReceived;
    }

    // 连接到飞书WebSocket
    public async Task ConnectAsync(WsEndpointResult endpoint, string appAccessToken)
    {
        await _webSocketClient.ConnectAsync(endpoint, appAccessToken);
    }

    private void OnConnected(object? sender, EventArgs e)
        => Console.WriteLine("🚀 WebSocket连接已建立");

    private void OnDisconnected(object? sender, WebSocketCloseEventArgs e)
        => Console.WriteLine($"🔌 连接已断开: {e.CloseStatusDescription}");

    private void OnError(object? sender, WebSocketErrorEventArgs e)
        => Console.WriteLine($"❌ 错误: {e.ErrorMessage}");

    private void OnFeishuEventReceived(object? sender, WebSocketFeishuEventArgs e)
        => Console.WriteLine($"📨 收到飞书事件: {e.EventType}");
}
```

#### 方式二：使用管理器

```csharp
public class MessageService
{
    private readonly IFeishuWebSocketManager _webSocketManager;

    public MessageService(IFeishuWebSocketManager webSocketManager)
    {
        _webSocketManager = webSocketManager;
        
        // 订阅连接事件
        _webSocketManager.Connected += OnConnected;
        _webSocketManager.Disconnected += OnDisconnected;
        _webSocketManager.Error += OnError;
        _webSocketManager.HeartbeatReceived += OnHeartbeatReceived;
    }

    private void OnConnected(object? sender, EventArgs e)
        => Console.WriteLine("🚀 WebSocket连接已建立");

    private void OnDisconnected(object? sender, WebSocketCloseEventArgs e)
        => Console.WriteLine($"🔌 连接已断开: {e.CloseStatusDescription}");

    private void OnError(object? sender, WebSocketErrorEventArgs e)
        => Console.WriteLine($"❌ 错误: {e.ErrorMessage}");

    private void OnHeartbeatReceived(object? sender, WebSocketHeartbeatEventArgs e)
        => Console.WriteLine($"💗 心跳消息: {e.Timestamp}, 间隔: {e.Interval}s, 状态: {e.Status}");
}
```

### 4. 心跳处理功能

#### 心跳事件订阅

```csharp
public class HeartbeatMonitorService : IHostedService
{
    private readonly IFeishuWebSocketManager _webSocketManager;
    private readonly List<DateTime> _heartbeatTimestamps = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 订阅心跳事件
        _webSocketManager.HeartbeatReceived += OnHeartbeatReceived;
        
        _logger.LogInformation("心跳监控服务已启动");
    }

    private void OnHeartbeatReceived(object? sender, WebSocketHeartbeatEventArgs e)
    {
        _heartbeatTimestamps.Add(DateTime.UtcNow);
        
        _logger.LogInformation("💗 心跳消息 - 时间戳: {Timestamp}, 间隔: {Interval}s, 状态: {Status}",
            e.Timestamp, e.Interval, e.Status);
    }
}
```

#### 心跳数据模型

```csharp
// 心跳消息结构
public class HeartbeatMessage : FeishuWebSocketMessage
{
    public HeartbeatData? Data { get; set; }
}

public class HeartbeatData
{
    public long Timestamp { get; set; }           // 心跳时间戳
    public int? Interval { get; set; }           // 心跳间隔（秒）
    public string? Status { get; set; }           // 心跳状态
}

// 心跳事件参数
public class WebSocketHeartbeatEventArgs : EventArgs
{
    public HeartbeatMessage? HeartbeatMessage { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public int? Interval { get; set; }
    public string? Status { get; set; }
}
```

#### 心跳统计和分析

```csharp
public class HeartbeatStatistics
{
    public int TotalHeartbeats { get; set; }          // 总心跳次数
    public List<HeartbeatInfo> RecentHeartbeats { get; set; }  // 最近心跳记录
    public DateTime? LastHeartbeatTime { get; set; }   // 最后心跳时间
    public double? AverageInterval { get; set; }       // 平均心跳间隔
}

// 获取心跳统计
var statistics = heartbeatService.GetStatistics();
Console.WriteLine($"总心跳: {statistics.TotalHeartbeats}, 平均间隔: {statistics.AverageInterval:F2}s");
```

## 🏗️ 架构设计

### 组件化架构

飞书WebSocket客户端采用组件化设计，将复杂功能拆分为专门的组件，提高代码的可维护性和扩展性。

### 架构设计

#### 核心组件

| 组件 | 职责 | 特性 |
|------|------|------|
| **WebSocketConnectionManager** | 连接管理器 | 连接建立、断开、状态管理、重连机制 |
| **AuthenticationManager** | 认证管理器 | WebSocket认证流程、状态管理、认证事件 |
| **MessageRouter** | 消息路由器 | 消息路由、版本检测(v1.0/v2.0)、处理器管理 |
| **BinaryMessageProcessor** | 二进制消息处理器 | 增量接收、ProtoBuf/JSON解析、内存优化 |

#### 消息处理器

| 处理器 | 说明 |
|---------|------|
| **IMessageHandler** | 消息处理器接口，提供通用反序列化功能 |
| **EventMessageHandler** | 事件消息处理器，支持v1.0和v2.0版本 |
| **BasicMessageHandler** | 基础消息处理器(Ping/Pong、认证、心跳) |
| **FeishuWebSocketClient** | 主客户端，组合所有组件 |

### 架构优势

- **🎯 单一职责** - 每个组件专注特定功能，代码清晰易懂
- **🔧 代码复用性提升** - 组件化设计，各组件可独立使用
- **🧪 测试友好** - 每个组件可独立测试，依赖清晰
- **🚀 扩展性提升** - 新功能通过添加组件实现，配置灵活

### 自定义消息处理器

```csharp
// 创建自定义消息处理器
public class CustomMessageHandler : JsonMessageHandler
{
    public override bool CanHandle(string messageType)
        => messageType == "custom_type";

    public override async Task HandleAsync(string message, CancellationToken cancellationToken = default)
    {
        var data = SafeDeserialize<CustomMessage>(message);
        // 处理逻辑...
    }
}

// 注册到消息路由器
client.RegisterMessageProcessor(customMessageHandler);
```

### 文件结构

```
Mud.Feishu.WebSocket/
├── Core/                           # 核心组件
│   ├── WebSocketConnectionManager.cs  # 连接管理
│   ├── AuthenticationManager.cs      # 认证管理  
│   ├── MessageRouter.cs             # 消息路由
│   └── BinaryMessageProcessor.cs    # 二进制处理
├── Handlers/                       # 消息处理器
│   ├── IMessageHandler.cs          # 处理器接口
│   ├── EventMessageHandler.cs       # 事件消息处理
│   └── BasicMessageHandler.cs     # 基础消息处理
├── SocketEventArgs/                # 事件参数类
├── DataModels/                    # 数据模型
├── FeishuWebSocketClient.cs       # 主客户端
└── Examples/                      # 使用示例
```

---

## 🎯 事件处理器（策略模式）

### 内置事件处理器

| 处理器 | 事件类型 | 说明 |
|--------|----------|------|
| `ReceiveMessageEventHandler` | `im.message.receive_v1` | 接收消息事件 |
| `UserCreatedEventHandler` | `contact.user.created_v3` | 用户创建事件 |
| `MessageReadEventHandler` | `im.message.message_read_v1` | 消息已读事件 |
| `UserAddedToGroupEventHandler` | `im.chat.member.user_added_v1` | 用户加入群聊 |
| `UserRemovedFromGroupEventHandler` | `im.chat.member.user_deleted_v1` | 用户离开群聊 |
| `DefaultFeishuEventHandler` | - | 未知事件类型处理 |

### 创建自定义事件处理器

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

        _logger.LogInformation("🎯 处理自定义事件: {EventType}", eventData.EventType);
        
        // 实现你的业务逻辑
        await ProcessBusinessLogicAsync(eventData);
    }

    private async Task ProcessBusinessLogicAsync(EventData eventData)
    {
        // 数据库操作、外部API调用等
        await Task.CompletedTask;
    }
}
```

### 注册自定义处理器

#### 使用建造者模式注册（推荐）

```csharp
// 注册多个自定义处理器
builder.Services.AddFeishuWebSocketBuilder()
    .ConfigureFrom(builder.Configuration)
    .UseMultiHandler()
    .AddHandler<CustomEventHandler>()                    // 类型注册
    .AddHandler<AnotherEventHandler>()                    // 第二个处理器
    .AddHandler(sp => new FactoryEventHandler(           // 工厂方法注册
        sp.GetService<ILogger<FactoryEventHandler>>(),
        sp.GetService<IConfiguration>()))
    .AddHandler(new InstanceEventHandler())               // 实例注册
    .Build();
```

#### 依赖注入注册

```csharp
// 注册处理器到 DI 容器
builder.Services.AddSingleton<CustomEventHandler>();
builder.Services.AddFeishuWebSocketService(builder.Configuration);
```

#### 运行时动态注册

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
        _logger.LogInformation("已注册自定义处理器: {HandlerType}", typeof(CustomEventHandler).Name);
    }
}
```

## ⚙️ 配置选项

### WebSocket配置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `AutoReconnect` | bool | true | 自动重连 |
| `MaxReconnectAttempts` | int | 5 | 最大重连次数 |
| `ReconnectDelayMs` | int | 5000 | 重连延迟(ms) |
| `HeartbeatIntervalMs` | int | 30000 | 心跳间隔(ms) |
| `ConnectionTimeoutMs` | int | 10000 | 连接超时(ms) |
| `ReceiveBufferSize` | int | 4096 | 接收缓冲区大小 |
| `EnableLogging` | bool | true | 启用日志 |
| `EnableMessageQueue` | bool | true | 启用消息队列 |
| `MessageQueueCapacity` | int | 1000 | 消息队列容量 |
| `ParallelMultiHandlers` | bool | true | 多处理器并行执行 |

### 代码配置

#### 建造者模式配置（推荐）

```csharp
builder.Services.AddFeishuWebSocketBuilder()
    .ConfigureOptions(options => {
        options.AppId = "your_app_id";
        options.AppSecret = "your_app_secret";
        options.AutoReconnect = true;
        options.MaxReconnectAttempts = 10;
        options.ReconnectDelayMs = 3000;
        options.HeartbeatIntervalMs = 25000;
    })
    .UseMultiHandler()
    .AddHandler<CustomHandler1>()
    .AddHandler<CustomHandler2>()
    .Build();
```

#### 简化配置

```csharp
// 单处理器模式
builder.Services.AddFeishuWebSocketServiceWithSingleHandler<CustomHandler>(
    options => {
        options.AppId = "your_app_id";
        options.AppSecret = "your_app_secret";
        options.AutoReconnect = true;
        options.MaxReconnectAttempts = 10;
        options.ReconnectDelayMs = 3000;
        options.HeartbeatIntervalMs = 25000;
    });

// 多处理器模式
builder.Services.AddFeishuWebSocketServiceWithMultiHandler<Handler1, Handler2>(
    options => {
        options.AppId = "your_app_id";
        options.AppSecret = "your_app_secret";
        options.AutoReconnect = true;
        options.EnableLogging = true;
    });
```

## 🎯 建造者模式高级用法

### 灵活的配置组合

```csharp
// 场景1：多环境配置
var builder = services.AddFeishuWebSocketBuilder();

if (builder.Environment.IsDevelopment())
{
    builder.ConfigureOptions(options => {
        options.EnableLogging = true;
        options.HeartbeatIntervalMs = 15000;
    });
}
else if (builder.Environment.IsProduction())
{
    builder.ConfigureFrom(configuration, "Production:WebSocket");
}

builder.UseMultiHandler()
       .AddHandler<DevEventHandler>()
       .AddHandler<ProdEventHandler>()
       .Build();
```

### 条件性处理器注册

```csharp
services.AddFeishuWebSocketBuilder()
    .ConfigureFrom(configuration)
    .UseMultiHandler()
    .AddHandler<BaseEventHandler>()
    .Apply(builder => {
        // 根据功能开关注册处理器
        if (configuration.GetValue<bool>("Features:EnableAudit"))
            builder.AddHandler<AuditEventHandler>();
        
        if (configuration.GetValue<bool>("Features:EnableAnalytics"))
            builder.AddHandler<AnalyticsEventHandler>();
    })
    .Build();
```

### 服务注册最佳实践

```csharp
// 推荐：使用扩展方法封装复杂配置
public static class FeishuWebSocketExtensions
{
    public static IServiceCollection AddFeishuWebSocketWithDefaultHandlers(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        return services.AddFeishuWebSocketBuilder()
            .ConfigureFrom(configuration)
            .UseMultiHandler()
            .AddHandler<ReceiveMessageEventHandler>()
            .AddHandler<UserCreatedEventHandler>()
            .AddHandler<MessageReadEventHandler>()
            .Build();
    }
}

// 使用时更简洁
builder.Services.AddFeishuWebSocketWithDefaultHandlers(builder.Configuration);
```

## 🔧 高级功能

### 心跳监控服务

```csharp
public class HeartbeatMonitorService : IHostedService
{
    private readonly IFeishuWebSocketManager _webSocketManager;
    private readonly List<DateTime> _heartbeatTimestamps = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 订阅心跳事件
        _webSocketManager.HeartbeatReceived += OnHeartbeatReceived;
        
        // 启动定时检查
        _heartbeatCheckTimer = new Timer(CheckHeartbeatStatus, null, 
            TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    private void OnHeartbeatReceived(object? sender, WebSocketHeartbeatEventArgs e)
    {
        _heartbeatTimestamps.Add(DateTime.UtcNow);
        
        // 分析心跳模式
        AnalyzeHeartbeatPattern();
        
        _logger.LogInformation("💗 收到心跳消息 - 时间戳: {Timestamp}, 间隔: {Interval}s, 状态: {Status}",
            e.Timestamp, e.Interval, e.Status);
    }

    private void AnalyzeHeartbeatPattern()
    {
        var recentTimestamps = _heartbeatTimestamps.TakeLast(10).ToList();
        var intervals = new List<double>();

        for (int i = 1; i < recentTimestamps.Count; i++)
        {
            var interval = (recentTimestamps[i] - recentTimestamps[i - 1]).TotalSeconds;
            intervals.Add(interval);
        }

        if (intervals.Any())
        {
            var averageInterval = intervals.Average();
            var variance = intervals.Select(x => Math.Pow(x - averageInterval, 2)).Average();
            var standardDeviation = Math.Sqrt(variance);

            // 如果标准差过大，可能表示心跳不稳定
            if (standardDeviation > 5.0)
            {
                _logger.LogWarning("检测到心跳间隔不稳定，可能存在连接问题");
            }
        }
    }

    public HeartbeatStatistics GetStatistics()
    {
        return new HeartbeatStatistics
        {
            TotalHeartbeats = _heartbeatTimestamps.Count,
            RecentHeartbeats = _heartbeatTimestamps.TakeLast(20)
                .Select((timestamp, index) => new HeartbeatInfo { Timestamp = timestamp })
                .ToList(),
            LastHeartbeatTime = _heartbeatTimestamps.LastOrDefault(),
            AverageInterval = CalculateAverageInterval(_heartbeatTimestamps.TakeLast(20).ToList())
        };
    }
}
```

### 事件处理器工厂

```csharp
public class EventHandlerManager
{
    private readonly IFeishuEventHandlerFactory _factory;

    public EventHandlerManager(IFeishuEventHandlerFactory factory)
        => _factory = factory;

    // 获取处理器
    public IFeishuEventHandler GetHandler(string eventType)
        => _factory.GetHandler(eventType);

    // 注册处理器
    public void RegisterHandler(IFeishuEventHandler handler)
        => _factory.RegisterHandler(handler);

    // 检查注册状态
    public bool IsRegistered(string eventType)
        => _factory.IsHandlerRegistered(eventType);

    // 获取所有事件类型
    public IReadOnlyList<string> GetAllEventTypes()
        => _factory.GetRegisteredEventTypes();
}
```

### 手动连接控制

```csharp
public class ConnectionController
{
    private readonly IFeishuWebSocketManager _manager;

    public ConnectionController(IFeishuWebSocketManager manager)
        => _manager = manager;

    // 启动连接
    public async Task StartAsync()
        => await _manager.StartAsync();

    // 发送消息
    public async Task SendMessageAsync(string message)
        => await _manager.SendMessageAsync(message);

    // 重新连接
    public async Task ReconnectAsync()
        => await _manager.ReconnectAsync();

    // 停止连接
    public async Task StopAsync()
        => await _manager.StopAsync();
}
```

### 分布式部署支持

```csharp
public class DistributedEventProcessor
{
    private readonly IDistributedLockProvider _lockProvider;
    private readonly ILogger<DistributedEventProcessor> _logger;

    public async Task ProcessEventAsync(string eventId, EventData eventData)
    {
        var lockKey = $"feishu_event_{eventId}";
        
        await using (await _lockProvider.AcquireLockAsync(lockKey, TimeSpan.FromMinutes(1)))
        {
            // 获取锁成功，处理事件
            _logger.LogInformation("🔄 处理事件: {EventId}", eventId);
            await ProcessEventInternalAsync(eventData);
        }
        // 获取锁失败，说明其他实例正在处理
    }
}
```

## 📈 性能优化

### 消息处理优化

- ✅ **异步处理** - 所有事件处理器使用异步方法
- ✅ **并行执行** - 多个事件处理器可并行运行
- ✅ **错误隔离** - 单个处理器异常不影响其他处理器
- ✅ **批量处理** - 支持批量处理相似事件

### 连接管理优化

- ✅ **智能心跳** - 根据网络环境自动调整心跳间隔
- ✅ **连接池** - 高并发场景下支持连接池管理
- ✅ **资源管理** - 自动释放连接资源，防止内存泄漏

### 监控和告警

- ✅ **性能指标** - 消息处理延迟、队列大小等监控
- ✅ **结构化日志** - 详细的日志记录和错误追踪
- ✅ **健康检查** - 连接状态和健康状态监控

## 🐛 常见问题



### 连接问题

**Q: 连接频繁断开？**
- 检查网络稳定性
- 调整心跳间隔至30秒以内
- 启用自动重连功能
- 监控 heartbeat 消息的接收情况

**Q: 认证失败？**
- 验证AppId和AppSecret是否正确
- 检查应用权限配置
- 确认网络访问权限

**Q: 没有收到心跳消息？**
- 确认 WebSocket 连接已建立
- 检查是否订阅了 HeartbeatReceived 事件
- 验证飞书服务器是否支持 heartbeat 消息类型
- 查看应用日志中的心跳相关错误

### 心跳问题

**Q: 心跳间隔不稳定？**
- 检查网络延迟和稳定性
- 使用心跳统计功能分析间隔模式
- 考虑在网络波动时增加重试机制

**Q: 如何使用心跳进行连接健康检查？**
- 监控 HeartbeatReceived 事件触发频率
- 设置心跳超时检测（如2分钟无心跳视为异常）
- 结合连接状态进行综合判断

### 性能问题

**Q: 消息处理延迟？**
- 优化事件处理器逻辑，使用异步操作
- 增加消息队列容量
- 使用多个消费者处理事件
- 监控心跳统计以评估连接质量

**Q: 内存占用过高？**
- 及时处理积压的消息
- 调整消息队列容量
- 监控内存使用情况
- 定期清理心跳统计数据

## 📋 支持的事件类型

### WebSocket 消息类型
- `ping` - 连接保活消息（自动响应 pong）
- `pong` - 连接保活响应
- `heartbeat` - 心跳消息（包含状态和间隔信息）
- `event` - 业务事件消息
- `auth` - 认证响应消息

### 消息事件
- `im.message.receive_v1` - 接收消息
- `im.message.message_read_v1` - 消息已读

### 群聊事件
- `im.chat.member.user_added_v1` - 用户加入群聊
- `im.chat.member.user_deleted_v1` - 用户离开群聊
- `im.chat.updated_v1` - 群聊信息更新

### 用户事件
- `contact.user.created_v3` - 用户创建
- `contact.user.updated_v3` - 用户更新
- `contact.user.deleted_v3` - 用户删除

### 部门事件
- `contact.department.created_v3` - 部门创建
- `contact.department.updated_v3` - 部门更新
- `contact.department.deleted_v3` - 部门删除

### 审批事件
- `approval.approval.approved_v1` - 审批通过
- `approval.approval.rejected_v1` - 审批拒绝

### 日程事件
- `calendar.event.updated_v4` - 日程事件

### 会议事件
- `meeting.meeting.started_v1` - 会议开始
- `meeting.meeting.ended_v1` - 会议结束

```csharp
public interface IFeishuWebSocketManager
{
    // 属性
    IFeishuWebSocketClient Client { get; }
    bool IsConnected { get; }
    
    // 事件
    event EventHandler<EventArgs>? Connected;
    event EventHandler<WebSocketCloseEventArgs>? Disconnected;
    event EventHandler<WebSocketMessageEventArgs>? MessageReceived;
    event EventHandler<WebSocketErrorEventArgs>? Error;
    event EventHandler<WebSocketHeartbeatEventArgs>? HeartbeatReceived;  // 心跳事件
    
    // 方法
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
    Task ReconnectAsync(CancellationToken cancellationToken = default);
}
```

### IFeishuEventHandler

```csharp
public interface IFeishuEventHandler
{
    string SupportedEventType { get; }
    Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default);
}
```

### FeishuWebSocketClient（主客户端）

```csharp
public class FeishuWebSocketClient : IFeishuWebSocketClient, IDisposable
{
    // 构造函数
    public FeishuWebSocketClient(
        ILogger<FeishuWebSocketClient> logger,
        IFeishuEventHandlerFactory eventHandlerFactory,
        ILoggerFactory loggerFactory,
        FeishuWebSocketOptions? options = null);

    // 属性
    public WebSocketState State { get; }
    public bool IsAuthenticated { get; }

    // 事件
    public event EventHandler<EventArgs>? Connected;
    public event EventHandler<WebSocketCloseEventArgs>? Disconnected;
    public event EventHandler<WebSocketMessageEventArgs>? MessageReceived;
    public event EventHandler<WebSocketErrorEventArgs>? Error;
    public event EventHandler<EventArgs>? Authenticated;
    public event EventHandler<WebSocketPingEventArgs>? PingReceived;
    public event EventHandler<WebSocketPongEventArgs>? PongReceived;
    public event EventHandler<WebSocketHeartbeatEventArgs>? HeartbeatReceived;
    public event EventHandler<WebSocketFeishuEventArgs>? FeishuEventReceived;
    public event EventHandler<WebSocketBinaryMessageEventArgs>? BinaryMessageReceived;

    // 主要方法
    public Task ConnectAsync(WsEndpointResult endpoint, CancellationToken cancellationToken = default);
    public Task ConnectAsync(WsEndpointResult endpoint, string appAccessToken, CancellationToken cancellationToken = default);
    public Task DisconnectAsync(CancellationToken cancellationToken = default);
    public Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
    public Task StartReceivingAsync(CancellationToken cancellationToken);
    
    // 自定义消息处理器管理
    public void RegisterMessageProcessor(Func<string, Task> processor);
    public bool UnregisterMessageProcessor(Func<string, Task> processor);
}
```

### IMessageHandler（消息处理器接口）

```csharp
public interface IMessageHandler
{
    bool CanHandle(string messageType);
    Task HandleAsync(string message, CancellationToken cancellationToken = default);
}
```

## 📄 许可证

本项目遵循 MIT 许可证进行分发和使用。

---

**🚀 立即开始使用飞书WebSocket客户端，构建稳定可靠的事件处理系统！**