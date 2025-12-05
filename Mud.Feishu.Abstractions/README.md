# Mud.Feishu.Abstractions

Mud.Feishu.Abstractions 是 MudFeishu 库的 WebSocket 事件订阅组件和 HTTP 事件订阅组件抽象层，专门用于处理飞书事件订阅。它提供了完整的事件订阅策略模式的事件处理机制，使开发人员能够轻松地在 .NET 应用程序中接收和处理飞书实时事件。

## 🚀 特性

- **📡 事件订阅抽象** - 提供完整的事件订阅和处理抽象层
- **🔧 策略模式** - 基于策略模式的事件处理器，支持多种事件类型
- **🏭 工厂模式** - 内置事件处理器工厂，支持动态注册和发现
- **⚡ 异步处理** - 完全异步的事件处理，支持并行处理
- **🎯 类型安全** - 强类型事件数据模型，避免运行时错误
- **📋 丰富事件类型** - 支持飞书所有主要事件类型
- **🔄 可扩展** - 易于扩展新的事件类型和处理器
- **📦 多框架支持** - 支持 .NET 6.0 - .NET 10.0

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
- **`DefaultFeishuEventHandler<T>`** - 事件处理器基类，提供默认实现
- **`IFeishuEventHandlerFactory`** - 事件处理器工厂，负责处理器的注册、发现和调用
- **`FeishuEventTypes`** - 事件类型常量，定义所有支持的飞书事件类型

## 🎯 支持的事件类型

### 组织管理事件
- `contact.user.created_v3` - 员工入职事件
- `contact.user.updated_v3` - 用户更新事件  
- `contact.user.deleted_v3` - 用户删除事件
- `contact.department.created_v3` - 部门创建事件
- `contact.department.updated_v3` - 部门更新事件
- `contact.department.deleted_v3` - 部门删除事件
- `contact.employee_type_enum.*` - 人员类型相关事件

### 消息事件
- `im.message.receive_v1` - 接收消息事件
- `im.message.recalled_v1` - 消息撤回事件
- `im.message.message_read_v1` - 消息已读事件
- `im.chat.member.user_added_v1` - 用户加入群聊事件
- `im.chat.member.user_deleted_v1` - 用户离开群聊事件
- `im.chat.updated_v1` - 群聊信息更新事件

### 审批事件
- `approval.approval.approved_v1` - 审批通过事件
- `approval.approval.rejected_v1` - 审批拒绝事件

### 日程和会议事件
- `calendar.event.updated_v4` - 日程事件
- `meeting.meeting.started_v1` - 会议开始事件
- `meeting.meeting.ended_v1` - 会议结束事件

## 📖 使用示例

### 1. 创建自定义事件处理器

```csharp
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;

public class UserCreatedEventHandler : IFeishuEventHandler
{
    public string SupportedEventType => FeishuEventTypes.UserCreated;

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"用户创建事件: {eventData.EventId}");
        
        // 获取具体的事件数据
        if (eventData.Event is UserCreatedEvent userEvent)
        {
            Console.WriteLine($"新用户: {userEvent.User.Name}");
        }
        
        await Task.CompletedTask;
    }
}
```

### 2. 使用事件处理器工厂

```csharp
public class EventService
{
    private readonly IFeishuEventHandlerFactory _factory;

    public EventService(IFeishuEventHandlerFactory factory)
    {
        _factory = factory;
    }

    public async Task ProcessEventAsync(EventData eventData)
    {
        // 注册处理器
        _factory.RegisterHandler(new UserCreatedEventHandler());
        _factory.RegisterHandler(new MessageReceiveEventHandler());

        // 获取并使用处理器
        var handler = _factory.GetHandler(eventData.EventType);
        await handler.HandleAsync(eventData);
    }

    public async Task ProcessEventParallelAsync(EventData eventData)
    {
        // 并行处理（使用所有匹配的处理器）
        await _factory.HandleEventParallelAsync(eventData.EventType, eventData);
    }
}
```

### 3. 配置依赖注入

```csharp
// 在 Startup.cs 或 Program.cs 中
builder.Services.AddSingleton<IFeishuEventHandlerFactory, FeishuEventHandlerFactory>();

// 注册具体的事件处理器
builder.Services.AddSingleton<IFeishuEventHandler, UserCreatedEventHandler>();
builder.Services.AddSingleton<IFeishuEventHandler, MessageReceiveEventHandler>();
```

### 4. 处理特定事件类型

```csharp
[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IFeishuEventHandlerFactory _factory;

    public WebhookController(IFeishuEventHandlerFactory factory)
    {
        _factory = factory;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook([FromBody] EventData eventData)
    {
        try
        {
            await _factory.HandleEventParallelAsync(
                eventData.EventType, 
                eventData);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                success = false, 
                error = ex.Message 
            });
        }
    }
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

## 📊 事件数据模型

### EventData 核心属性

```csharp
public class EventData
{
    public string EventId { get; set; }      // 事件ID
    public string EventType { get; set; }    // 事件类型
    public string AppId { get; set; }        // 应用ID
    public string TenantKey { get; set; }    // 租户ID
    public long CreateTime { get; set; }      // 事件创建时间戳
    public object? Event { get; set; }       // 具体事件内容
}
```

### 具体事件类型

每种事件类型都有对应的具体数据模型，例如：

- `UserCreatedEvent` - 用户创建事件数据
- `MessageReceiveEvent` - 消息接收事件数据
- `DepartmentCreatedEvent` - 部门创建事件数据

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
public class MyCustomEvent
{
    [JsonPropertyName("custom_data")]
    public string CustomData { get; set; } = string.Empty;
}
```

### 3. 实现事件处理器

```csharp
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
```

## 🚨 最佳实践

### 1. 错误处理
```csharp
public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
{
    try
    {
        // 事件处理逻辑
    }
    catch (Exception ex)
    {
        // 记录错误日志
        _logger.LogError(ex, "处理事件 {EventType} 时发生错误", eventData.EventType);
        
        // 根据业务需求决定是否重新抛出异常
    }
}
```

### 2. 性能优化
```csharp
public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
{
    // 使用 cancellation token
    cancellationToken.ThrowIfCancellationRequested();
    
    // 异步处理，避免阻塞
    await ProcessEventAsync(eventData);
    
    // 考虑使用 ValueTask 对高频事件进行优化
}
```

### 3. 资源管理
```csharp
public class ResourceAwareEventHandler : IFeishuEventHandler, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // 互斥处理事件
            await ProcessWithLockAsync(eventData);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}
```

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

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE-MIT](../../LICENSE-MIT) 文件了解详情。

## 🆘 支持

如果您遇到问题或有疑问，请：

1. 查看 [文档](https://open.feishu.cn/document/)
2. 搜索现有的 [Issues](https://gitee.com/mudtools/MudFeishu/issues)
3. 创建新的 [Issue](https://gitee.com/mudtools/MudFeishu/issues/new)

## 📊 版本历史

查看 [CHANGELOG.md](../../CHANGELOG.md) 了解版本更新详情。

---

**Mud.Feishu.Abstractions** - 让飞书事件处理变得简单而强大！ 🚀