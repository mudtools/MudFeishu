# 飞书 WebSocket 客户端服务

企业级飞书事件订阅 WebSocket 客户端，提供可靠的连接管理、自动重连和策略模式事件处理。

**🚀 新特性：极简 API** - 一行代码完成服务注册，开箱即用！

## ✨ 核心特性

- 🚀 **极简 API** - 一行代码完成服务注册，开箱即用
- 🔄 **智能连接管理** - 自动重连、心跳检测、状态监控
- 🫀 **心跳消息处理** - 支持飞书 heartbeat 消息类型，实时连接状态监控
- 🚀 **高性能消息处理** - 异步处理、消息队列、并行执行
- 🎯 **策略模式事件处理** - 可扩展的事件处理器架构
- 🔌 **事件拦截器** - 支持在事件处理前后插入自定义逻辑（日志、遥测、限流等）
- 🛡️ **企业级稳定性** - 完善的错误处理、资源管理、日志记录
- ⚙️ **灵活配置** - 支持配置文件、代码配置和建造者模式
- 📊 **监控友好** - 详细的事件通知、性能指标、心跳统计、FeishuMetrics 集成
- 🛡️ **错误分类处理** - 区分可恢复和不可恢复错误
- 🛡️ **认证失败详细追踪** - 按错误码分类认证失败原因，统计失败次数和时间
- 🔧 **资源管理优化** - 实现 IHostedService 生命周期管理，避免资源泄漏
- 🔁 **指数退避重连** - 可插拔重连策略，次数和时间双重限制，防抖机制
- 🔐 **消息序号验证** - 重放攻击检测、消息丢失检测、序号回退检测
- 📦 **消息队列背压** - 三种背压策略（DropOldest/DropNewest/Block）
- 🔑 **事件去重** - 内存去重/分布式去重（Redis），防止重复处理
- 🔒 **SSL/TLS 证书验证** - 可配置证书验证策略，支持自定义验证回调
- 🎫 **令牌自动刷新** - 访问令牌缓存和提前刷新，避免过期

## 🚀 快速开始

### 1. 安装 NuGet 包

```bash
dotnet add package Mud.Feishu.WebSocket
```

### 2. 最简配置（一行代码）

在 `Program.cs` 中：

```csharp
using Mud.Feishu.WebSocket;

var builder = WebApplication.CreateBuilder(args);

// 先注册多应用支持
builder.Services.AddFeishuApp(builder.Configuration);

// 一行代码注册WebSocket服务（需要至少一个事件处理器）
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration, "default")
    .AddHandler<ReceiveMessageEventHandler>()
    .Build();

var app = builder.Build();
app.Run();
```

### 3. 完整配置（添加事件处理器）

```csharp
// 先注册多应用支持
builder.Services.AddFeishuApp(builder.Configuration);

// 从配置文件注册并添加事件处理器
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration, "default")
    .AddHandler<ReceiveMessageEventHandler>()
    .AddHandler<UserCreatedEventHandler>()
    .Build();

var app = builder.Build();
app.Run();
```

### 4. 配置选项

```json
{
  "FeishuApps": [
    {
      "AppKey": "default",
      "AppId": "your_app_id",
      "AppSecret": "your_app_secret",
      "BaseUrl": "https://open.feishu.cn",
      "TimeOut": 30,
      "RetryCount": 3,
      "EnableLogging": true,
      "IsDefault": true
    }
  ],
  "FeishuWebSocket": {
    "AutoReconnect": true,
    "MaxReconnectAttempts": 5,
    "ReconnectDelayMs": 5000,
    "HeartbeatIntervalMs": 25000,
    "EnableLogging": true,
    "EventDeduplication": {
      "Mode": "InMemory",
      "CacheExpirationMs": 172800000,
      "CleanupIntervalMs": 300000
    }
  }
}
```

## 🏗️ 架构设计

### 组件化架构

飞书 WebSocket 客户端采用组件化设计，将复杂功能拆分为专门的组件，提高代码的可维护性和扩展性。

### 架构设计

#### 核心组件

| 组件                                    | 职责             | 特性                                       |
| --------------------------------------- | ---------------- | ------------------------------------------ |
| **WebSocketConnectionManager**          | 连接管理器       | 连接建立、断开、状态管理、SSL/TLS 证书验证 |
| **AuthenticationManager**               | 认证管理器       | WebSocket 认证流程、状态管理、认证事件     |
| **MessageRouter**                       | 消息路由器       | 消息路由、版本检测(v1.0/v2.0)、处理器管理  |
| **BinaryMessageProcessor**              | 二进制消息处理器 | 增量接收、ProtoBuf/JSON 解析、内存优化     |
| **HeartbeatManager**                    | 心跳管理器       | 心跳检测、超时处理、连续超时触发重连       |
| **SessionManager**                      | 会话管理器       | session_id 管理、会话恢复、24 小时有效期   |
| **MessageSequenceValidator**            | 消息序号验证器   | 重放检测、消息丢失检测、序号回退检测       |
| **MessageQueueManager**                 | 消息队列管理器   | 消息队列、背压策略、并发控制               |
| **EventSubscriptionManager**            | 事件订阅管理器   | 事件类型订阅、订阅请求发送                 |
| **ConnectionMetrics**                   | 连接指标管理器   | 消息统计、性能指标、FeishuMetrics 集成     |
| **ReconnectionOrchestrator**            | 重连协调器       | 统一重连管理、防抖机制、冷却时间           |
| **ExponentialBackoffReconnectStrategy** | 指数退避重连策略 | 指数退避延迟、次数和时间双重限制           |

#### 消息处理器

| 处理器                    | 说明                                   |
| ------------------------- | -------------------------------------- |
| **IMessageHandler**       | 消息处理器接口，提供通用反序列化功能   |
| **EventMessageHandler**   | 事件消息处理器，支持 v1.0 和 v2.0 版本 |
| **BasicMessageHandler**   | 基础消息处理器(Ping/Pong、认证、心跳)  |
| **FeishuWebSocketClient** | 主客户端，组合所有组件                 |

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
├── Configuration/                 # 配置选项
│   ├── FeishuWebSocketOptions.cs  # 核心配置选项
│   ├── FeishuWebSocketOptionsValidator.cs # 配置验证器
│   ├── EventDeduplicationOptions.cs # 事件去重配置
│   ├── EventDeduplicationMode.cs  # 去重模式枚举
│   └── MessageSizeLimits.cs       # 消息大小限制
├── Core/                          # 核心组件
│   ├── WebSocketConnectionManager.cs  # 连接管理
│   ├── AuthenticationManager.cs      # 认证管理
│   ├── MessageRouter.cs              # 消息路由
│   ├── BinaryMessageProcessor.cs     # 二进制处理
│   ├── HeartbeatManager.cs           # 心跳管理
│   ├── SessionManager.cs             # 会话管理
│   ├── MessageSequenceValidator.cs   # 消息序号验证
│   ├── MessageQueueManager.cs        # 消息队列管理
│   ├── EventSubscriptionManager.cs   # 事件订阅管理
│   ├── ConnectionMetrics.cs          # 连接指标
│   ├── ReconnectionOrchestrator.cs   # 重连协调器
│   ├── ExponentialBackoffReconnectStrategy.cs # 指数退避策略
│   ├── IReconnectStrategy.cs         # 重连策略接口
│   ├── IReconnectionOrchestrator.cs  # 重连协调器接口
│   ├── ErrorRecoveryStrategy.cs      # 错误恢复策略
│   ├── RetryHelper.cs                # 重试工具
│   └── JsonOptions.cs                # JSON序列化选项
├── Handlers/                      # 消息处理器
│   ├── IMessageHandler.cs          # 处理器接口
│   ├── FeishuEventMessageHandler.cs # 事件消息处理
│   ├── AuthMessageHandler.cs       # 认证消息处理
│   ├── HeartbeatMessageHandler.cs  # 心跳消息处理
│   ├── PingPongMessageHandler.cs   # Ping/Pong处理
│   ├── JsonMessageHandler.cs       # JSON消息基类
│   └── FeishuWebSocketEventHandlerFactory.cs # 事件处理器工厂
├── Interfaces/                    # 公共接口
│   ├── IFeishuWebSocketClient.cs   # 客户端接口
│   ├── IFeishuWebSocketManager.cs  # 管理器接口
│   └── IMessageHandler.cs          # 消息处理器接口
├── SocketEventArgs/               # 事件参数类
├── DataModels/                    # 数据模型
├── Exceptions/                    # 异常定义
├── Extensions/                    # 扩展方法
│   ├── FeishuWebSocketServiceBuilder.cs # 服务建造者
│   └── ServiceCollectionExtensions.cs   # 注册扩展
├── FeishuWebSocketClient.cs       # 主客户端
├── FeishuWebSocketManager.cs      # 管理器实现
├── FeishuWebSocketHostedService.cs # 后台服务
└── WebSocketConnectionState.cs    # 连接状态模型
```

## 🏗️ 服务注册方式

### 🚀 最简注册（推荐）

```csharp
// 一行代码完成基础配置
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .Build();
```

### 📋 注册多个事件处理器

```csharp
// 支持链式调用，注册多个处理器
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<ReceiveMessageEventHandler>()
    .AddHandler<UserCreatedEventHandler>()
    .AddHandler<MessageReadEventHandler>()
    .Build();
```

### ⚙️ 代码配置

```csharp
// 使用委托配置选项
builder.Services.CreateFeishuWebSocketServiceBuilder(options =>
{
    options.AutoReconnect = true;
    options.HeartbeatIntervalMs = 25000;
    options.MaxReconnectAttempts = 5;
    options.MaxTotalReconnectTime = TimeSpan.FromMinutes(30);
    options.EventDeduplication.Mode = EventDeduplicationMode.InMemory;
})
.AddHandler<ReceiveMessageEventHandler>()
.Build();
```

### 🎯 Apply 方法

```csharp
// 使用 Apply 方法进行条件性配置
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .Apply(b =>
    {
        if (builder.Environment.IsDevelopment())
            b.AddInterceptor<LoggingEventInterceptor>();

        if (builder.Configuration.GetValue<bool>("Features:EnableAudit"))
            b.AddHandler<AuditEventHandler>();
    })
    .AddHandler<ReceiveMessageEventHandler>()
    .Build();
```

### 🔌 添加事件拦截器

```csharp
// 添加内置日志拦截器和自定义拦截器
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddInterceptor<LoggingEventInterceptor>()  // 内置日志拦截器
    .AddInterceptor<CustomTelemetryInterceptor>()  // 自定义遥测拦截器
    .AddHandler<ReceiveMessageEventHandler>()
    .Build();
```

### 🎯 三种处理器注册方式

```csharp
// 方式1：类型注册（推荐）
.AddHandler<ReceiveMessageEventHandler>()

// 方式2：工厂注册
.AddHandler(sp => new FactoryEventHandler(
    sp.GetRequiredService<ILogger<FactoryEventHandler>>()))

// 方式3：实例注册
.AddHandler(new InstanceEventHandler())
```

---

## 🎯 事件处理器（策略模式）

### 内置事件处理器

| 处理器                             | 事件类型                         | 说明             |
| ---------------------------------- | -------------------------------- | ---------------- |
| `ReceiveMessageEventHandler`       | `im.message.receive_v1`          | 接收消息事件     |
| `UserCreatedEventHandler`          | `contact.user.created_v3`        | 用户创建事件     |
| `MessageReadEventHandler`          | `im.message.message_read_v1`     | 消息已读事件     |
| `UserAddedToGroupEventHandler`     | `im.chat.member.user_added_v1`   | 用户加入群聊     |
| `UserRemovedFromGroupEventHandler` | `im.chat.member.user_deleted_v1` | 用户离开群聊     |
| `DefaultFeishuEventHandler`        | -                                | 未知事件类型处理 |
| `DepartmentCreatedEventHandler`    | `contact.department.created_v3`  | 部门创建事件     |
| `DepartmentDeleteEventHandler`     | `contact.department.deleted_v3`  | 部门删除事件     |

> 💡 **提示**：以上处理器来自 `Mud.Feishu.Abstractions` 项目，通过代码生成器自动生成。你也可以实现 `IFeishuEventHandler` 接口创建自定义处理器。

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
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<CustomEventHandler>()                    // 类型注册
    .AddHandler(sp => new FactoryEventHandler(           // 工厂注册
        sp.GetRequiredService<ILogger<FactoryEventHandler>>()))
    .AddHandler(new InstanceEventHandler())               // 实例注册
    .Build();
```

### 事件拦截器（Interceptors）

事件拦截器允许在事件处理前后执行自定义逻辑，如日志记录、指标收集、权限验证等。

#### 内置拦截器

**LoggingEventInterceptor** - 记录事件处理日志

```csharp
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddInterceptor<LoggingEventInterceptor>()  // 记录事件处理开始和结束
    .AddHandler<ReceiveMessageEventHandler>()
    .Build();
```

#### 自定义拦截器

创建自定义拦截器需要实现 `IFeishuEventInterceptor` 接口：

```csharp
using Mud.Feishu.Abstractions;

/// <summary>
/// 自定义遥测拦截器示例
/// </summary>
public class CustomTelemetryInterceptor : IFeishuEventInterceptor
{
    private readonly ILogger<CustomTelemetryInterceptor> _logger;

    public CustomTelemetryInterceptor(ILogger<CustomTelemetryInterceptor> logger)
        => _logger = logger;

    /// <summary>
    /// 事件处理前拦截
    /// </summary>
    /// <returns>返回 false 将中断事件处理流程</returns>
    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[遥测] 事件开始: {EventType}, EventId: {EventId}", eventType, eventData.EventId);
        return Task.FromResult(true); // 返回 true 继续处理，false 中断
    }

    /// <summary>
    /// 事件处理后拦截
    /// </summary>
    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        if (exception == null)
        {
            _logger.LogInformation("[遥测] 事件成功: {EventType}", eventType);
        }
        else
        {
            _logger.LogError(exception, "[遥测] 事件失败: {EventType}", eventType);
        }
        return Task.CompletedTask;
    }
}
```

#### 注册自定义拦截器

```csharp
// 类型注册
.AddInterceptor<CustomTelemetryInterceptor>()

// 工厂注册
.AddInterceptor(sp => new CustomTelemetryInterceptor(
    sp.GetRequiredService<ILogger<CustomTelemetryInterceptor>>()))

// 实例注册
var interceptor = new CustomTelemetryInterceptor(logger);
.AddInterceptor(interceptor)
```

#### 拦截器执行顺序

拦截器按注册顺序依次执行，完整流程：

```
WebSocket 事件到达
    ↓
拦截器1: BeforeHandleAsync
    ↓
拦截器2: BeforeHandleAsync
    ↓
...
    ↓
拦截器N: BeforeHandleAsync
    ↓
[事件处理器处理事件]
    ↓
拦截器N: AfterHandleAsync
    ↓
...
    ↓
拦截器2: AfterHandleAsync
    ↓
拦截器1: AfterHandleAsync
    ↓
处理完成
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

### WebSocket 核心配置

| 选项                                  | 类型                                 | 默认值     | 说明                                        |
| ------------------------------------- | ------------------------------------ | ---------- | ------------------------------------------- |
| `AutoReconnect`                       | bool                                 | true       | 自动重连                                    |
| `MaxReconnectAttempts`                | int                                  | 5          | 最大重连次数                                |
| `ReconnectDelayMs`                    | int                                  | 5000       | 重连基础延迟(ms)，最小 1000                 |
| `MaxReconnectDelayMs`                 | int                                  | 30000      | 最大重连延迟(ms)，≥ReconnectDelayMs         |
| `MaxTotalReconnectTime`               | TimeSpan                             | 30 分钟    | 最大重连总时间，超时后停止重连              |
| `ReconnectCooldownTime`               | TimeSpan                             | 5 秒       | 两次重连尝试间的最小间隔                    |
| `EnableReconnectMetrics`              | bool                                 | true       | 是否启用重连指标收集                        |
| `HeartbeatIntervalMs`                 | int                                  | 25000      | 心跳间隔(ms)，最小 5000（飞书建议 25 秒内） |
| `ConnectionTimeoutMs`                 | int                                  | 10000      | 连接超时(ms)                                |
| `InitialReceiveBufferSize`            | int                                  | 4096       | 初始接收缓冲区大小(字节)                    |
| `EnableLogging`                       | bool                                 | true       | 启用日志                                    |
| `EnableMessageQueue`                  | bool                                 | true       | 启用消息队列                                |
| `MessageQueueCapacity`                | int                                  | 1000       | 消息队列容量                                |
| `BackpressureStrategy`                | QueueBackpressureStrategy            | DropOldest | 背压策略（DropOldest/DropNewest/Block）     |
| `BackpressureBlockTimeoutMs`          | int                                  | 5000       | 背压阻塞等待超时(ms)，仅 Block 模式         |
| `EmptyQueueCheckIntervalMs`           | int                                  | 100        | 空队列检查间隔(ms)，最小 10                 |
| `HealthCheckIntervalMs`               | int                                  | 60000      | 健康检查间隔(ms)，最小 1000                 |
| `MaxConcurrentMessageProcessing`      | int                                  | 10         | 最大并发消息处理数，最小 1                  |
| `ValidateServerCertificate`           | bool                                 | true       | 是否验证 SSL 证书（生产环境建议 true）      |
| `AllowSelfSignedCertificates`         | bool                                 | false      | 是否允许自签名证书（生产环境建议 false）    |
| `CustomCertificateValidationCallback` | RemoteCertificateValidationCallback? | null       | 自定义证书验证回调                          |
| `TokenRefreshInterval`                | TimeSpan?                            | 2 小时     | 访问令牌有效期                              |
| `TokenRefreshAhead`                   | TimeSpan?                            | 5 分钟     | 提前刷新令牌的时间                          |
| `EventDeduplication`                  | EventDeduplicationOptions            | 见下       | 事件去重配置                                |

### 消息大小限制配置 (`MessageSizeLimits`)

| 选项                   | 类型 | 默认值   | 说明                     |
| ---------------------- | ---- | -------- | ------------------------ |
| `MaxTextMessageSize`   | int  | 1048576  | 最大文本消息大小(字符)   |
| `MaxBinaryMessageSize` | long | 10485760 | 最大二进制消息大小(字节) |

**配置示例：**

```json
{
  "FeishuWebSocket": {
    "MessageSizeLimits": {
      "MaxTextMessageSize": 1048576,
      "MaxBinaryMessageSize": 10485760
    }
  }
}
```

### 事件去重配置 (`EventDeduplication`)

| 选项                | 类型                     | 默认值     | 说明                                  |
| ------------------- | ------------------------ | ---------- | ------------------------------------- |
| `Mode`              | `EventDeduplicationMode` | `InMemory` | 去重模式（None/InMemory/Distributed） |
| `CacheExpirationMs` | int                      | 172800000  | 缓存过期时间(ms)，默认 48 小时        |
| `CleanupIntervalMs` | int                      | 300000     | 缓存清理间隔(ms)，默认 5 分钟         |

**去重模式说明：**

- `None` - 禁用去重（不推荐，仅用于特殊场景）
- `InMemory` - 内存去重（单实例，默认）
- `Distributed` - 分布式去重（需配置 `IFeishuEventDistributedDeduplicator`）

**配置示例：**

```json
{
  "FeishuWebSocket": {
    "EventDeduplication": {
      "Mode": "InMemory",
      "CacheExpirationMs": 172800000,
      "CleanupIntervalMs": 300000
    }
  }
}
```

## 🎯 高级用法

### 多环境配置

```csharp
var webSocketBuilder = builder.Services.CreateFeishuWebSocketServiceBuilder(configuration);

if (builder.Environment.IsDevelopment())
{
    webSocketBuilder.ConfigureOptions(options => {
        options.EnableLogging = true;
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

### 条件性处理器注册

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

### 配置 Redis 分布式去重

```csharp
// 注册Redis分布式去重服务
builder.Services.AddFeishuRedisDeduplicators(builder.Configuration);

// 配置飞书WebSocket服务
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddInterceptor<LoggingEventInterceptor>()
    .AddHandler<ReceiveMessageEventHandler>()
    .Build();
```

### 指定配置节名称

```csharp
// 使用非默认配置节
builder.Services.CreateFeishuWebSocketServiceBuilder(
        configuration,
        sectionName: "CustomFeishu")  // 配置节名称
    .AddHandler<ReceiveMessageEventHandler>()
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

### 消息序号验证

内置 `MessageSequenceValidator` 可检测消息重放和丢失：

- **重复消息检测**：滑动窗口去重（最近 1000 条消息）
- **序号回退检测**：检测可能的攻击行为
- **消息丢失检测**：序号跳跃超过阈值时发出警告

```csharp
// 序号验证器在服务注册时自动启用
// 可通过事件订阅验证失败通知
var validator = serviceProvider.GetRequiredService<MessageSequenceValidator>();
validator.ValidationFailed += (sender, args) =>
{
    if (args.MessageType == SequenceValidationType.SequenceRollback)
        logger.LogWarning("检测到序号回退攻击: {Message}", args.Message);
    else if (args.MessageType == SequenceValidationType.MessageLoss)
        logger.LogWarning("可能丢失消息: {Message}", args.Message);
};
```

### 会话管理

`SessionManager` 管理 WebSocket 会话状态，支持断线恢复：

- **会话 ID 管理**：自动跟踪当前 session_id
- **会话有效期**：24 小时有效期检查
- **重连恢复**：通过 `GetSessionIdForReconnect()` 获取有效会话 ID 用于断线恢复
- **会话事件**：`SessionUpdated` 事件通知会话变更

### 连接监控指标

`ConnectionMetrics` 提供实时连接统计，集成 `FeishuMetrics` 全局指标体系：

```csharp
var metrics = serviceProvider.GetRequiredService<ConnectionMetrics>();
var stats = metrics.GetCurrentStats();

// 可用指标
stats.MessagesSent;           // 发送消息数
stats.MessagesReceived;       // 接收消息数（有效）
stats.MessagesReceivedTotal;  // 总接收数（含重复）
stats.BytesSent;              // 发送字节数
stats.BytesReceived;          // 接收字节数
stats.ConnectionErrors;       // 连接错误数
stats.AuthenticationErrors;   // 认证错误数
stats.AverageProcessingTimeMs;// 平均处理时间
stats.Uptime;                 // 连接时长
stats.MessagesPerSecond;      // 每秒消息数
stats.BytesPerSecond;         // 每秒字节数
```

### 背压策略

消息队列满时支持三种背压策略：

| 策略         | 说明                   | 适用场景                 |
| ------------ | ---------------------- | ------------------------ |
| `DropOldest` | 丢弃最旧的消息（默认） | 实时性优先，允许丢消息   |
| `DropNewest` | 丢弃新消息             | 数据完整性优先           |
| `Block`      | 阻塞等待直到队列有空间 | 不允许丢消息，可接受延迟 |

```json
{
  "FeishuWebSocket": {
    "MessageQueueCapacity": 1000,
    "BackpressureStrategy": "DropOldest",
    "BackpressureBlockTimeoutMs": 5000
  }
}
```

### SSL/TLS 证书配置

```csharp
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .ConfigureOptions(options =>
    {
        options.ValidateServerCertificate = true;
        options.AllowSelfSignedCertificates = false;
    })
    .AddHandler<ReceiveMessageEventHandler>()
    .Build();
```

自定义证书验证：

```csharp
options.CustomCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
{
    return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
};
```

### 自定义重连策略

实现 `IReconnectStrategy` 接口可替换默认的指数退避策略：

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

// 注册自定义策略（在 CreateFeishuWebSocketServiceBuilder 之前）
builder.Services.AddSingleton<IReconnectStrategy>(
    new FixedIntervalReconnectStrategy(TimeSpan.FromSeconds(10)));
```

### 访问令牌管理

`FeishuWebSocketManager` 内置访问令牌缓存和自动刷新：

```json
{
  "FeishuWebSocket": {
    "TokenRefreshInterval": "02:00:00",
    "TokenRefreshAhead": "00:05:00"
  }
}
```

- `TokenRefreshInterval`：令牌有效期，默认 2 小时（与飞书一致）
- `TokenRefreshAhead`：提前刷新时间，默认 5 分钟，避免使用即将过期的令牌

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

**🚀 立即开始使用飞书 WebSocket 客户端，构建稳定可靠的事件处理系统！**
