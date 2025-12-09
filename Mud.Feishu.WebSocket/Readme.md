# 飞书WebSocket客户端服务

企业级飞书事件订阅WebSocket客户端，提供可靠的连接管理、自动重连和策略模式事件处理。

**🚀 新特性：极简API** - 一行代码完成服务注册，开箱即用！

## ✨ 核心特性

- 🚀 **极简API** - 一行代码完成服务注册，开箱即用
- 🔄 **智能连接管理** - 自动重连、心跳检测、状态监控
- 🫀 **心跳消息处理** - 支持飞书 heartbeat 消息类型，实时连接状态监控
- 🚀 **高性能消息处理** - 异步处理、消息队列、并行执行
- 🎯 **策略模式事件处理** - 可扩展的事件处理器架构
- 🛡️ **企业级稳定性** - 完善的错误处理、资源管理、日志记录
- ⚙️ **灵活配置** - 支持配置文件、代码配置和建造者模式
- 📊 **监控友好** - 详细的事件通知、性能指标、心跳统计

## 🚀 快速开始

### 1. 安装NuGet包

```bash
dotnet add package Mud.Feishu.WebSocket
```

### 2. 最简配置（一行代码）

在 `Program.cs` 中：

```csharp
using Mud.Feishu.WebSocket;

var builder = WebApplication.CreateBuilder(args);

// 一行代码注册WebSocket服务
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration);

var app = builder.Build();
app.Run();
```

### 3. 完整配置（添加事件处理器）

```csharp
// 从配置文件注册并添加事件处理器
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .AddHandler<UserCreatedEventHandler>()
    .UseMultiHandler();

var app = builder.Build();
app.Run();
```

### 4. 配置选项

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

## 🏗️ 服务注册方式详解

### 从配置文件注册（最简单）

最简单的方式，直接从配置文件读取：

```csharp
// 使用默认配置节 "Feishu:WebSocket"
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration);

// 使用自定义配置节
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration, "CustomSection");

// 添加事件处理器
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .AddHandler<UserCreatedEventHandler>()
    .UseMultiHandler();
```

### 代码配置

直接在代码中配置选项：

```csharp
builder.Services.AddFeishuWebSocketServiceBuilder(options =>
{
    options.AppId = "your_app_id";
    options.AppSecret = "your_app_secret";
    options.AutoReconnect = true;
    options.MaxReconnectAttempts = 5;
    options.HeartbeatIntervalMs = 30000;
});
```

### 高级建造者模式

对于复杂的配置需求：

```csharp
var webSocketBuilder = builder.Services.AddFeishuWebSocketServiceBuilder()
    .ConfigureFrom(configuration, "Feishu:WebSocket")
    .ConfigureOptions(options => {
        options.AppId = "your_app_id";
        options.AppSecret = "your_app_secret";
    });

// 添加不同类型的处理器
webSocketBuilder
    .AddHandler<ReceiveMessageEventHandler>()
    .AddHandler<UserCreatedEventHandler>()
    .AddHandler(sp => new FactoryEventHandler(
        sp.GetService<ILogger<FactoryEventHandler>>(),
        sp.GetService<IConfiguration>()));

// 启用功能
webSocketBuilder
    .UseMultiHandler()         // 启用多处理器模式
    .EnableMetrics()           // 启用性能监控
    .EnableHealthChecks();     // 启用健康检查

// 构建服务注册
webSocketBuilder.Build();
```

### 简化的处理器注册

```csharp
// 快速注册多个处理器
builder.Services.AddFeishuWebSocketServiceBuilder(configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .AddHandler<UserCreatedEventHandler>()
    .AddHandler<MessageReadEventHandler>()
    .UseMultiHandler();

// 链式调用配置
builder.Services.AddFeishuWebSocketServiceBuilder(configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .ConfigureOptions(options => {
        options.HeartbeatIntervalMs = 25000;
        options.EnableLogging = true;
    });
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

#### 使用简化方式注册（推荐）

```csharp
// 注册多个自定义处理器
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .UseMultiHandler()
    .AddHandler<CustomEventHandler>()                    // 类型注册
    .AddHandler<AnotherEventHandler>()                    // 第二个处理器
    .AddHandler(sp => new FactoryEventHandler(           // 工厂方法注册
        sp.GetService<ILogger<FactoryEventHandler>>(),
        sp.GetService<IConfiguration>()))
    .AddHandler(new InstanceEventHandler());               // 实例注册
```

#### 使用建造者模式注册（高级用法）

```csharp
// 复杂配置场景
builder.Services.AddFeishuWebSocketBuilder()
    .ConfigureFrom(builder.Configuration)
    .UseMultiHandler()
    .AddHandler<CustomEventHandler>()
    .AddHandler<AnotherEventHandler>()
    .EnableMetrics()
    .Build();
```

#### 依赖注入注册

```csharp
// 注册处理器到 DI 容器
builder.Services.AddSingleton<CustomEventHandler>();
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration);
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

#### 简化配置（推荐）

```csharp
// 从代码配置选项
builder.Services.AddFeishuWebSocketServiceBuilder(options =>
{
    options.AppId = "your_app_id";
    options.AppSecret = "your_app_secret";
    options.AutoReconnect = true;
    options.MaxReconnectAttempts = 10;
    options.ReconnectDelayMs = 3000;
    options.HeartbeatIntervalMs = 25000;
})
.AddHandler<ReceiveMessageEventHandler>()
.UseMultiHandler();
```

#### 建造者模式配置（高级用法）

```csharp
// 复杂配置使用建造者模式
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
    .EnableMetrics()
    .AddHandler<CustomHandler1>()
    .AddHandler<CustomHandler2>()
    .Build();
```

#### 多种注册方式对比

```csharp
// 方式一：最简化
builder.Services.AddFeishuWebSocketServiceBuilder(configuration);

// 方式二：简化 + 处理器
builder.Services.AddFeishuWebSocketServiceBuilder(configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .UseMultiHandler();

// 方式三：代码配置
builder.Services.AddFeishuWebSocketServiceBuilder(options =>
{
    options.AppId = "your_app_id";
    options.AppSecret = "your_app_secret";
});

// 方式四：建造者模式（复杂配置）
builder.Services.AddFeishuWebSocketBuilder()
    .ConfigureFrom(configuration)
    .UseMultiHandler()
    .EnableMetrics()
    .AddHandler<Handler>()
    .Build();
```

## 🎯 高级配置用法

### 多环境配置

```csharp
// 场景1：多环境配置
var webSocketBuilder = builder.Services.AddFeishuWebSocketServiceBuilder(configuration);

if (builder.Environment.IsDevelopment())
{
    webSocketBuilder.ConfigureOptions(options => {
        options.EnableLogging = true;
        options.HeartbeatIntervalMs = 15000;
    });
}
else if (builder.Environment.IsProduction())
{
    webSocketBuilder.ConfigureFrom(configuration, "Production:WebSocket");
}

webSocketBuilder
    .UseMultiHandler()
    .AddHandler<DevEventHandler>()
    .AddHandler<ProdEventHandler>()
    .Build();
```

### 条件性处理器注册

```csharp
builder.Services.AddFeishuWebSocketServiceBuilder(configuration)
    .UseMultiHandler()
    .AddHandler<BaseEventHandler>()
    .Apply(webSocketBuilder => {
        // 根据功能开关注册处理器
        if (configuration.GetValue<bool>("Features:EnableAudit"))
            webSocketBuilder.AddHandler<AuditEventHandler>();
        
        if (configuration.GetValue<bool>("Features:EnableAnalytics"))
            webSocketBuilder.AddHandler<AnalyticsEventHandler>();
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
        return services.AddFeishuWebSocketServiceBuilder(configuration)
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

### 使用建造者模式的高级功能

```csharp
// 当需要复杂配置时使用建造者模式
builder.Services.AddFeishuWebSocketBuilder()
    .ConfigureFrom(configuration, "Feishu:WebSocket")
    .ConfigureOptions(options => {
        options.AppId = "your_app_id";
        options.AppSecret = "your_app_secret";
    })
    .UseMultiHandler()
    .EnableMetrics()
    .EnableHealthChecks()
    .AddHandler<ReceiveMessageEventHandler>()
    .AddHandler<UserCreatedEventHandler>()
    .AddHandler(sp => new CustomEventHandler(
        sp.GetService<ILogger<CustomEventHandler>>()))
    .Build();
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

## 📄 许可证

本项目遵循 MIT 许可证进行分发和使用。

---

**🚀 立即开始使用飞书WebSocket客户端，构建稳定可靠的事件处理系统！**