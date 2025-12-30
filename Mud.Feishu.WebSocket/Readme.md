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

// 一行代码注册WebSocket服务（需要至少一个事件处理器）
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .Build();

var app = builder.Build();
app.Run();
```

### 3. 完整配置（添加事件处理器）

```csharp
// 从配置文件注册并添加事件处理器
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .AddHandler<UserCreatedEventHandler>()
    .Build();

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

## 🏗️ 服务注册方式

### 🚀 最简注册（推荐）

```csharp
// 一行代码完成基础配置
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration);

// 添加事件处理器
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .AddHandler<UserCreatedEventHandler>()
    .UseMultiHandler();
```

### ⚙️ 代码配置

```csharp
builder.Services.AddFeishuWebSocketServiceBuilder(options =>
{
    options.AppId = "your_app_id";
    options.AppSecret = "your_app_secret";
    options.AutoReconnect = true;
    options.HeartbeatIntervalMs = 30000;
});
```

### 🔧 高级配置（建造者模式）

```csharp
builder.Services.AddFeishuWebSocketServiceBuilder(configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .Build();
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
| `DepartmentCreatedEventHandler` | `contact.department.created_v3` | 部门创建事件 |
| `DepartmentDeleteEventHandler` | `contact.department.deleted_v3` | 部门删除事件 |

### 使用内置事件处理器基类

Mud.Feishu.Abstractions 提供了多个内置事件处理器基类，继承这些基类可以简化开发：

#### 用户事件处理器（通用基类）

```csharp
using Mud.Feishu.Abstractions;
using Mud.Feishu.WebSocket.Services;
using System.Text.Json;

namespace YourProject.Handlers;

/// <summary>
/// 演示用户事件处理器 - 实现通用接口
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
            // 解析用户数据
            var userData = ParseUserData(eventData);

            // 记录事件到服务
            await _eventService.RecordUserEventAsync(userData, cancellationToken);

            // 模拟业务处理
            await ProcessUserEventAsync(userData, cancellationToken);

            _logger.LogInformation("✅ [用户事件] 用户创建事件处理完成: {UserId}", userData.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [用户事件] 处理用户创建事件失败");
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
/// 用户数据模型
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

#### 部门事件处理器（继承专用基类）

```csharp
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.DataModels.Organization;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.WebSocket.Services;

namespace YourProject.Handlers;

/// <summary>
/// 演示部门创建事件处理器 - 继承 DepartmentCreatedEventHandler 基类
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

        _logger.LogInformation("[部门事件] 开始处理部门创建事件: {EventId}", eventData.EventId);

        try
        {
            // 记录事件到服务
            await _eventService.RecordDepartmentEventAsync(departmentData.Object, cancellationToken);

            // 模拟业务处理
            await ProcessDepartmentEventAsync(departmentData.Object, cancellationToken);

            _logger.LogInformation("[部门事件] 部门创建事件处理完成: {DepartmentId}", departmentData.Object.DepartmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[部门事件] 处理部门创建事件失败");
            throw;
        }
    }

    private async Task ProcessDepartmentEventAsync(DepartmentCreatedResult departmentData, CancellationToken cancellationToken)
    {
        _logger.LogDebug("🔄 [部门事件] 开始处理部门数据: {DepartmentId}", departmentData.DepartmentId);

        // 模拟异步业务操作
        await Task.Delay(100, cancellationToken);

        // 模拟验证逻辑
        if (string.IsNullOrWhiteSpace(departmentData.DepartmentId))
        {
            throw new ArgumentException("部门ID不能为空");
        }

        // 模拟权限初始化
        _logger.LogInformation("[部门事件] 初始化部门权限: {DepartmentName}", departmentData.Name);

        // 模拟更新统计信息
        _eventService.IncrementDepartmentCount();

        await Task.CompletedTask;
    }
}

/// <summary>
/// 演示部门删除事件处理器 - 继承 DepartmentDeleteEventHandler 基类
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
            _logger.LogWarning("部门删除事件实体为空，跳过处理");
            return;
        }

        _logger.LogInformation("🗑️ [部门删除事件] 开始处理部门删除事件");
        _logger.LogDebug("部门删除事件详情: {@EventEntity}", eventEntity);

        await Task.CompletedTask;
    }
}
```

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

```csharp
// 注册处理器（多种方式）
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<CustomEventHandler>()                    // 类型注册
    .AddHandler(sp => new FactoryEventHandler(           // 工厂注册
        sp.GetRequiredService<ILogger<FactoryEventHandler>>()))
    .AddHandler(new InstanceEventHandler());               // 实例注册

// 或使用建造者模式
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<CustomEventHandler>()
    .Build();
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

## ⚙️ 配置选项

### 主要配置项

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `AutoReconnect` | bool | true | 自动重连 |
| `MaxReconnectAttempts` | int | 5 | 最大重连次数 |
| `ReconnectDelayMs` | int | 5000 | 重连延迟(ms) |
| `HeartbeatIntervalMs` | int | 30000 | 心跳间隔(ms) |
| `EnableLogging` | bool | true | 启用日志 |
| `EnableMessageQueue` | bool | true | 启用消息队列 |

## 🎯 高级用法

### 多环境配置

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

### 条件性处理器注册

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

## 🔧 高级功能

### 手动连接控制

```csharp
public class ConnectionService
{
    private readonly IFeishuWebSocketManager _manager;

    public ConnectionService(IFeishuWebSocketManager manager)
        => _manager = manager;

    // 连接管理
    public async Task StartAsync() => await _manager.StartAsync();
    public async Task StopAsync() => await _manager.StopAsync();
    public async Task ReconnectAsync() => await _manager.ReconnectAsync();
    
    // 消息操作
    public async Task SendMessageAsync(string message) 
        => await _manager.SendMessageAsync(message);
    
    // 事件订阅
    public void SubscribeEvents()
    {
        _manager.Connected += OnConnected;
        _manager.Disconnected += OnDisconnected;
        _manager.HeartbeatReceived += OnHeartbeat;
    }
}
```

## 📋 支持的事件类型

### WebSocket 消息类型
- `ping` / `pong` - 连接保活
- `heartbeat` - 心跳消息
- `event` - 业务事件
- `auth` - 认证响应

### 主要业务事件
- **消息**: `im.message.receive_v1`, `im.message.message_read_v1`
- **群聊**: `im.chat.member.user_added_v1`, `im.chat.member.user_deleted_v1`
- **用户**: `contact.user.created_v3`, `contact.user.updated_v3`, `contact.user.deleted_v3`
- **部门**: `contact.department.*_v3`
- **审批**: `approval.approval.*_v1`
- **日程**: `calendar.event.updated_v4`
- **会议**: `meeting.meeting.*_v1`

## 📄 许可证

本项目遵循 MIT 许可证进行分发和使用。

---

**🚀 立即开始使用飞书WebSocket客户端，构建稳定可靠的事件处理系统！**