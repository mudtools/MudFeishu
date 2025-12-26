# Mud.Feishu.Abstractions

[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.Abstractions.svg)](https://www.nuget.org/packages/Mud.Feishu.Abstractions/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE-MIT)

Mud.Feishu.Abstractions 是 MudFeishu 库的 WebSocket 事件订阅组件和 HTTP 事件订阅组件抽象层，专用于处理飞书事件订阅。它提供了完整的事件订阅策略模式的事件处理机制，使开发人员能够轻松地在 .NET 应用程序中接收和处理飞书实时事件。

## 🚀 特性

- **📡 事件订阅抽象** - 提供完整的事件订阅和处理抽象层
- **🔧 策略模式** - 基于策略模式的事件处理器，支持多种事件类型
- **🏭 工厂模式** - 内置事件处理器工厂，支持动态注册和发现
- **⚡ 异步处理** - 完全异步的事件处理，支持并行处理
- **🎯 类型安全** - 强类型事件数据模型，避免运行时错误
- **📋 丰富事件类型** - 支持飞书所有主要事件类型
- **🔄 可扩展** - 易于扩展新的事件类型和处理器
- **🛡️ 内置基类** - 提供默认事件处理器基类，简化开发
- **📦 多框架支持** - 支持.NET4.6+、 .NET 6.0 - .NET 10.0

## 📦 安装

```bash
dotnet add package Mud.Feishu.Abstractions
```

## 🏛️ 核心架构

### 事件处理流程

```
飞书事件 → EventData → EventHandlerFactory → IFeishuEventHandler → 业务逻辑
```

### 核心组件

- **`EventData`** - 事件数据模型，包含飞书事件的所有基本信息
- **`IFeishuEventHandler`** - 事件处理器接口，定义事件处理契约
- **`DefaultFeishuEventHandler<T>`** - 抽象事件处理器基类，提供默认的反序列化和错误处理
- **`DefaultFeishuObjectEventHandler<T>`** - 对象事件处理器基类，继承自DefaultFeishuEventHandler
- **`IFeishuEventHandlerFactory`** - 事件处理器工厂，负责处理器的注册、发现和调用
- **`IEventResult`** - 事件结果接口，用于标识不同类型事件的结果
- **`ObjectEventResult<T>`** - 对象事件结果类，包装事件处理后返回的对象
- **`FeishuEventTypes`** - 事件类型常量，定义所有支持的飞书事件类型

## 🎯 支持的事件类型

### 组织管理事件
- `contact.user.created_v3` - 员工入职事件
- `contact.user.updated_v3` - 用户更新事件  
- `contact.user.deleted_v3` - 用户删除事件
- `contact.custom_attr_event.updated_v3` - 成员字段变更事件
- `contact.department.created_v3` - 部门创建事件
- `contact.department.updated_v3` - 部门更新事件
- `contact.department.deleted_v3` - 部门删除事件
- `contact.employee_type_enum.created_v3` - 人员类型创建事件
- `contact.employee_type_enum.updated_v3` - 人员类型更新事件
- `contact.employee_type_enum.deleted_v3` - 人员类型删除事件
- `contact.employee_type_enum.actived_v3` - 人员类型启用事件
- `contact.employee_type_enum.deactivated_v3` - 人员类型禁用事件

### 消息事件
- `im.message.receive_v1` - 接收消息事件
- `im.message.recalled_v1` - 消息撤回事件
- `im.message.message_read_v1` - 消息已读事件
- `im.message.reaction.created_v1` - 新增消息表情回复事件
- `im.message.reaction.deleted_v1` - 删除消息表情回复事件

### 群聊事件
- `im.chat.disbanded_v1` - 群解散事件
- `im.chat.updated_v1` - 群配置修改事件
- `im.chat.member.user.added_v1` - 用户进群事件
- `im.chat.member.user.deleted_v1` - 用户出群事件
- `im.chat.member.user.withdrawn_v1` - 撤销拉用户进群事件
- `im.chat.member.bot.added_v1` - 机器人进群事件
- `im.chat.member.bot.deleted_v1` - 机器人被移出群事件

### 审批事件
- `approval.approval.approved_v1` - 审批通过事件
- `approval.approval.rejected_v1` - 审批拒绝事件

### 日程和会议事件
- `calendar.event.updated_v4` - 日程事件
- `meeting.meeting.started_v1` - 会议开始事件
- `meeting.meeting.ended_v1` - 会议结束事件

## 📖 使用示例

### 1. 创建基础事件处理器（实现 IFeishuEventHandler 接口）

```csharp
using Mud.Feishu.Abstractions;
using System.Text.Json;

namespace YourProject.Handlers;

/// <summary>
/// 演示用户事件处理器
/// </summary>
public class DemoUserEventHandler : IFeishuEventHandler
{
    private readonly ILogger<DemoUserEventHandler> _logger;
    private readonly YourEventService _eventService;

    public DemoUserEventHandler(ILogger<DemoUserEventHandler> logger, YourEventService eventService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    public string SupportedEventType => FeishuEventTypes.UserCreated;

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation("👤 [用户事件] 开始处理用户创建事件: {EventId}", eventData.EventId);

        try
        {
            // 解析用户数据
            var userData = ParseUserData(eventData);

            // 记录事件到服务
            await _eventService.RecordUserEventAsync(userData, cancellationToken);

            // 模拟业务处理
            await ProcessUserEventAsync(userData, cancellationToken);

            _logger.LogInformation("✅ [用户事件] 用户创建事件处理完成: 用户ID {UserId}, 用户名 {UserName}",
                userData.UserId, userData.UserName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [用户事件] 处理用户创建事件失败: {EventId}", eventData.EventId);
            throw;
        }
    }

    private UserData ParseUserData(EventData eventData)
    {
        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(eventData.Event?.ToString() ?? "{}");
            var userElement = jsonElement.GetProperty("user");

            return new UserData
            {
                UserId = userElement.GetProperty("user_id").GetString() ?? "",
                UserName = userElement.GetProperty("name").GetString() ?? "",
                Email = TryGetProperty(userElement, "email") ?? "",
                Department = TryGetProperty(userElement, "department") ?? "",
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析用户数据失败");
            throw new InvalidOperationException("无法解析用户数据", ex);
        }
    }

    private async Task ProcessUserEventAsync(UserData userData, CancellationToken cancellationToken)
    {
        // 模拟异步业务操作
        await Task.Delay(100, cancellationToken);

        // 验证必要字段
        if (string.IsNullOrWhiteSpace(userData.UserId))
        {
            throw new ArgumentException("用户ID不能为空");
        }

        // 模拟发送欢迎通知
        _logger.LogInformation("📧 [用户事件] 发送欢迎通知给用户: {UserName} ({Email})",
            userData.UserName, userData.Email);

        await Task.CompletedTask;
    }

    private static string? TryGetProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
    }
}
```

### 2. 继承预定义事件处理器（推荐方式）

```csharp
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.DataModels.Organization;
using Mud.Feishu.Abstractions.EventHandlers;

namespace YourProject.Handlers;

/// <summary>
/// 演示部门事件处理器 - 继承预定义的部门创建事件处理器
/// </summary>
public class DemoDepartmentEventHandler : DepartmentCreatedEventHandler
{
    private readonly YourEventService _eventService;

    public DemoDepartmentEventHandler(ILogger<DemoDepartmentEventHandler> logger, YourEventService eventService) : base(logger)
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

            _logger.LogInformation("[部门事件] 部门创建事件处理完成: 部门ID {DepartmentId}, 部门名 {DepartmentName}",
                departmentData.Object.DepartmentId, departmentData.Object.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[部门事件] 处理部门创建事件失败: {EventId}", eventData.EventId);
            throw;
        }
    }

    private async Task ProcessDepartmentEventAsync(DepartmentCreatedResult departmentData, CancellationToken cancellationToken)
    {
        // 模拟异步业务操作
        await Task.Delay(100, cancellationToken);

        // 验证逻辑
        if (string.IsNullOrWhiteSpace(departmentData.DepartmentId))
        {
            throw new ArgumentException("部门ID不能为空");
        }

        // 模拟权限初始化
        _logger.LogInformation("[部门事件] 初始化部门权限: {DepartmentName}", departmentData.Name);

        // 通知部门主管
        if (!string.IsNullOrWhiteSpace(departmentData.LeaderUserId))
        {
            _logger.LogInformation("[部门事件] 通知部门主管: {LeaderUserId}", departmentData.LeaderUserId);
        }

        // 处理层级关系
        if (!string.IsNullOrWhiteSpace(departmentData.ParentDepartmentId))
        {
            _logger.LogInformation("[部门事件] 建立层级关系: {DepartmentId} -> {ParentDepartmentId}",
                departmentData.DepartmentId, departmentData.ParentDepartmentId);
        }

        await Task.CompletedTask;
    }
}
```

### 3. 在 Program.cs 中配置服务和事件处理器

```csharp
using Mud.Feishu.WebSocket;
using Mud.Feishu.WebSocket.Demo.Handlers;
using Mud.Feishu.WebSocket.Demo.Services;
using Mud.Feishu.WebSocket.Services;

var builder = WebApplication.CreateBuilder(args);

// 配置基础服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "飞书WebSocket测试API",
        Version = "v1",
        Description = "用于测试飞书WebSocket长连接功能的演示API"
    });
});

// 配置飞书服务
builder.Services.AddFeishuServicesBuilder(builder.Configuration)
                .AddAuthenticationApi()
                .AddTokenManagers()
                .Build();

// 配置飞书WebSocket服务（推荐方式）
builder.Services.AddFeishuWebSocketBuilder()
    .ConfigureFrom(builder.Configuration)
    .UseMultiHandler()  // 使用多处理器模式
    .AddHandler<DemoDepartmentEventHandler>()      // 添加部门创建事件处理器
    .AddHandler<DemoDepartmentDeleteEventHandler>() // 添加部门删除事件处理器
    .Build();

// 配置自定义服务
builder.Services.AddSingleton<DemoEventService>();
builder.Services.AddHostedService<DemoEventBackgroundService>();

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 配置中间件
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();

app.Run();
```

### 4. 创建自定义事件服务来处理事件数据

```csharp
namespace YourProject.Services;

/// <summary>
/// 演示事件服务 - 用于记录和管理事件处理结果
/// </summary>
public class DemoEventService
{
    private readonly ILogger<DemoEventService> _logger;
    private readonly ConcurrentBag<UserData> _userEvents = new();
    private readonly ConcurrentBag<DepartmentData> _departmentEvents = new();
    private int _userCount = 0;
    private int _departmentCount = 0;

    public DemoEventService(ILogger<DemoEventService> logger)
    {
        _logger = logger;
    }

    public async Task RecordUserEventAsync(UserData userData, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("记录用户事件: {UserId}", userData.UserId);
        _userEvents.Add(userData);
        await Task.CompletedTask;
    }

    public async Task RecordDepartmentEventAsync(DepartmentData departmentData, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("记录部门事件: {DepartmentId}", departmentData.DepartmentId);
        _departmentEvents.Add(departmentData);
        await Task.CompletedTask;
    }

    public void IncrementUserCount()
    {
        Interlocked.Increment(ref _userCount);
        _logger.LogInformation("用户计数更新: {Count}", _userCount);
    }

    public void IncrementDepartmentCount()
    {
        Interlocked.Increment(ref _departmentCount);
        _logger.LogInformation("部门计数更新: {Count}", _departmentCount);
    }

    public IEnumerable<UserData> GetUserEvents() => _userEvents.ToList();
    public IEnumerable<DepartmentData> GetDepartmentEvents() => _departmentEvents.ToList();
    public int GetUserCount() => _userCount;
    public int GetDepartmentCount() => _departmentCount;
}
```

## 🏗️ 高级用法

### 多处理器策略

```csharp
public class MultiHandlerService
{
    private readonly IFeishuEventHandlerFactory _factory;

    public MultiHandlerService(IFeishuEventHandlerFactory factory)
    {
        _factory = factory;
    }

    public async Task HandleEventWithMultipleStrategies(EventData eventData)
    {
        // 获取所有匹配的处理器
        var handlers = _factory.GetHandlers(eventData.EventType);
        
        // 按优先级处理
        foreach (var handler in handlers.OrderBy(h => h.GetType().Name))
        {
            try
            {
                await handler.HandleAsync(eventData);
            }
            catch (Exception ex)
            {
                // 记录错误但继续处理其他处理器
                Console.WriteLine($"处理器 {handler.GetType().Name} 失败: {ex.Message}");
            }
        }
    }
}
```

### 条件事件处理

```csharp
public class ConditionalEventHandler : IFeishuEventHandler
{
    public string SupportedEventType => FeishuEventTypes.ReceiveMessage;

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        // 只处理特定类型的消息
        if (eventData.Event is MessageReceiveEvent msgEvent)
        {
            if (msgEvent.Message.MessageType == "text")
            {
                await HandleTextMessage(msgEvent);
            }
            else if (msgEvent.Message.MessageType == "image")
            {
                await HandleImageMessage(msgEvent);
            }
        }
    }

    private async Task HandleTextMessage(MessageReceiveEvent msgEvent)
    {
        // 处理文本消息逻辑
    }

    private async Task HandleImageMessage(MessageReceiveEvent msgEvent)
    {
        // 处理图片消息逻辑
    }
}
```

## 🔧 扩展新事件类型

### 1. 定义事件类型常量

```csharp
public static class CustomEventTypes
{
    public const string MyCustomEvent = "custom.my_event.v1";
}
```

### 2. 创建事件数据模型

```csharp
public class MyCustomEvent : IEventResult
{
    [JsonPropertyName("custom_data")]
    public string CustomData { get; set; } = string.Empty;
}
```

### 3. 实现事件处理器

```csharp
// 基础实现方式
public class MyCustomEventHandler : IFeishuEventHandler
{
    public string SupportedEventType => CustomEventTypes.MyCustomEvent;

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Event is MyCustomEvent customEvent)
        {
            // 处理自定义事件
        }
    }
}

// 推荐使用基类
public class MyCustomEventHandler : DefaultFeishuEventHandler<MyCustomEvent>
{
    public override string SupportedEventType => CustomEventTypes.MyCustomEvent;

    public MyCustomEventHandler(ILogger<MyCustomEventHandler> logger) : base(logger)
    {
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData, 
        MyCustomEvent? eventEntity, 
        CancellationToken cancellationToken = default)
    {
        if (eventEntity != null)
        {
            // 处理自定义事件，基类已自动反序列化
            Console.WriteLine($"自定义数据: {eventEntity.CustomData}");
        }
        
        await Task.CompletedTask;
    }
}
```

## 📊 选择对比

### 处理器选择策略

| 策略 | 优点 | 缺点 | 适用场景 |
|------|------|------|----------|
| `IEventHandler` 直接实现 | 最大灵活性 | 需要手动反序列化 | 简单事件或特殊需求 |
| `DefaultFeishuEventHandler<T>` | 自动反序列化、错误处理 | 继承层次增加 | 大多数标准事件 |
| `DefaultFeishuObjectEventHandler<T>` | 专为对象结果优化 | 功能相对固定 | 返回对象的事件 |

### 性能建议

- ✅ **推荐** 使用 `DefaultFeishuEventHandler<T>` 基类
- ⚡ **优化** 对高频事件使用 `ValueTask`
- 🔄 **并发** 使用 `HandleEventParallelAsync` 处理复杂事件
- 🛡️ **安全** 基类内置了异常处理和日志记录


## 🛠️ 开发和构建

### 要求

- .NET 6.0 或更高版本
- Visual Studio 2022 或 Visual Studio Code

### 构建项目

```bash
# 克隆仓库
git clone https://gitee.com/mudtools/MudFeishu.git
cd MudFeishu/Mud.Feishu.Abstractions

# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行测试
dotnet test
```

## 📚 相关项目

- [Mud.Feishu](../Mud.Feishu) - 主要的飞书SDK实现
- [Mud.Feishu.WebSocket](../Mud.Feishu.WebSocket) - WebSocket事件订阅实现
- [Mud.Feishu.Test](../Mud.Feishu.Test) - 测试项目和使用示例

## 🤝 贡献

欢迎贡献！请查看 [贡献指南](../../CONTRIBUTING.md) 了解详情。

### 贡献流程

1. Fork 项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

**Mud.Feishu.Abstractions** - 让飞书事件处理变得简单而强大！ 🚀