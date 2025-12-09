# Mud.Feishu.Webbook

飞书事件订阅与处理的 Webbook 组件，提供完整的飞书事件接收、验证、解密和分发功能。

## 功能特性

- ✅ **极简API**：一行代码完成服务注册，开箱即用
- ✅ **灵活配置**：支持配置文件、代码配置和建造者模式
- ✅ **自动事件路由**：根据事件类型自动分发到对应的处理器
- ✅ **安全验证**：支持事件订阅验证、请求签名验证和时间戳验证
- ✅ **加密解密**：内置 AES-256-CBC 解密功能，自动处理飞书加密事件
- ✅ **多种使用模式**：支持中间件模式、控制器模式和混合模式
- ✅ **依赖注入**：完全集成 .NET 依赖注入容器
- ✅ **异常处理**：完善的异常处理和日志记录
- ✅ **性能监控**：可选的性能指标收集和监控
- ✅ **健康检查**：内置健康检查端点
- ✅ **异步处理**：完全异步的事件处理机制
- ✅ **并发控制**：可配置的并发事件处理数量限制

## 快速开始

### 1. 安装 NuGet 包

```bash
dotnet add package Mud.Feishu.Webbook
```

### 2. 最简配置（一行代码）

在 `Program.cs` 中：

```csharp
using Mud.Feishu.Webbook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 一行代码注册Webbook服务
builder.Services.AddFeishuWebbook(builder.Configuration);

var app = builder.Build();
app.UseFeishuWebbook(); // 添加中间件
app.Run();
```

### 3. 完整配置（添加事件处理器）

```csharp
builder.Services.AddFeishuWebbook(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .AddHandler<UserEventHandler>()
    .EnableControllers();

var app = builder.Build();
app.UseFeishuWebbook();
app.MapControllers(); // 控制器路由
app.Run();
```

### 4. 详细配置选项

#### 方式一：从配置文件注册（推荐）

```csharp
using Mud.Feishu.Webbook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 从配置文件注册飞书 Webbook 服务
builder.Services.AddFeishuWebbook(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()               // 添加消息处理器
    .AddHandler<UserCreatedEventHandler>()                // 添加用户事件处理器
    .EnableControllers();                                  // 启用控制器支持

var app = builder.Build();

// 添加飞书 Webbook 中间件
app.UseFeishuWebbook();

app.Run();
```

#### 方式二：代码配置

```csharp
// 使用代码配置飞书 Webbook 服务
builder.Services.AddFeishuWebbook(options =>
{
    options.VerificationToken = "your_verification_token";
    options.EncryptKey = "your_encrypt_key";
    options.RoutePrefix = "feishu/webbook";
})
.AddHandler<MessageReceiveEventHandler>()
.AddHandler<UserCreatedEventHandler>()
.EnableControllers();
```

#### 方式三：建造者模式（高级用法）

```csharp
// 使用建造者模式进行复杂配置
builder.Services.AddFeishuWebbookBuilder()
    .ConfigureFrom(builder.Configuration)                    // 从配置文件读取
    .EnableControllers()                                   // 启用控制器支持
    .EnableHealthChecks()                                  // 启用健康检查
    .EnableMetrics()                                       // 启用性能指标
    .AddHandler<MessageReceiveEventHandler>()                 // 添加消息处理器
    .AddHandler<UserCreatedEventHandler>()                  // 添加用户事件处理器
    .Build();                                           // 构建服务注册
```

### 3. 配置文件

```json
{
  "FeishuWebbook": {
    "VerificationToken": "your_verification_token",
    "EncryptKey": "your_encrypt_key",
    "RoutePrefix": "feishu/webbook",
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

## 🏗️ 服务注册方式详解

### 从配置文件注册

最简单的方式，直接从 `appsettings.json` 读取配置：

```csharp
// 使用默认配置节 "FeishuWebbook"
builder.Services.AddFeishuWebbook(builder.Configuration);

// 使用自定义配置节
builder.Services.AddFeishuWebbook(builder.Configuration, "CustomSection");

// 添加事件处理器
builder.Services.AddFeishuWebbook(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .AddHandler<UserCreatedEventHandler>();
```

### 代码配置

直接在代码中配置选项：

```csharp
builder.Services.AddFeishuWebbook(options =>
{
    options.VerificationToken = "your_verification_token";
    options.EncryptKey = "your_encrypt_key";
    options.RoutePrefix = "webhook";
    options.EnableRequestLogging = true;
    options.MaxConcurrentEvents = 20;
});
```

### 高级建造者模式

对于复杂的配置需求，可以使用建造者模式：

```csharp
var webbookBuilder = builder.Services.AddFeishuWebbookBuilder()
    .ConfigureFrom(configuration, "CustomSection")          // 指定配置节
    .ConfigureOptions(options => {                           // 代码配置
        options.VerificationToken = "token";
        options.EncryptKey = "key";
        options.RoutePrefix = "webhook";
    });

// 添加不同类型的处理器
webbookBuilder
    .AddHandler<MessageEventHandler>()                      // 类型注册
    .AddHandler<UserEventHandler>()                         // 类型注册
    .AddHandler(new CustomEventHandler())                   // 实例注册
    .AddHandler(sp => new FactoryEventHandler(               // 工厂注册
        sp.GetService<ILogger<FactoryEventHandler>>(),
        sp.GetService<IConfiguration>()));

// 启用可选功能
webbookBuilder
    .EnableControllers()          // 启用控制器支持
    .EnableHealthChecks()         // 启用健康检查
    .EnableMetrics()              // 启用性能监控
    .EnableAutoEndpoint();        // 自动注册端点

// 构建服务注册
webbookBuilder.Build();
```
app.UseFeishuWebbook();

app.Run();
```

### 3. 创建事件处理器

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
        _logger.LogInformation("收到消息事件: {EventId}", eventData.EventId);
        
        // 处理消息逻辑
        var messageData = JsonSerializer.Deserialize<object>(
            eventData.Event?.ToString() ?? string.Empty);
        
        // 你的业务逻辑...
        
        await Task.CompletedTask;
    }
}
```

## 配置选项

### 基本配置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `VerificationToken` | string | - | 飞书事件订阅验证 Token |
| `EncryptKey` | string | - | 飞书事件加密密钥 |
| `RoutePrefix` | string | "feishu/webbook" | Webbook 路由前缀 |
| `AutoRegisterEndpoint` | bool | true | 是否自动注册端点 |

### 安全配置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ValidateSourceIP` | bool | false | 是否验证来源 IP |
| `AllowedSourceIPs` | HashSet\<string\> | - | 允许的源 IP 地址列表 |
| `AllowedHttpMethods` | HashSet\<string\> | ["POST"] | 允许的 HTTP 方法 |
| `MaxRequestBodySize` | long | 10MB | 最大请求体大小 |

### 性能配置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `MaxConcurrentEvents` | int | 10 | 最大并发事件数 |
| `EventHandlingTimeoutMs` | int | 30000 | 事件处理超时时间（毫秒） |
| `EnablePerformanceMonitoring` | bool | false | 是否启用性能监控 |

### 日志配置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `EnableRequestLogging` | bool | true | 是否启用请求日志记录 |
| `EnableExceptionHandling` | bool | true | 是否启用异常处理 |

## 使用模式

### 中间件模式（推荐）

```csharp
// Program.cs
builder.Services.AddFeishuWebbook(builder.Configuration);

var app = builder.Build();
app.UseFeishuWebbook(); // 自动处理路由前缀下的请求
app.Run();
```

### 控制器模式

```csharp
// Program.cs
builder.Services.AddFeishuWebbook(builder.Configuration)
    .EnableControllers(); // 启用控制器支持

var app = builder.Build();
app.UseFeishuWebbook();  // 可以同时使用中间件和控制器
app.MapControllers();     // 使用控制器路由
app.Run();
```

### 配置文件模式

在 `appsettings.json` 中：

```json
{
  "FeishuWebbook": {
    "VerificationToken": "your_verification_token_here",
    "EncryptKey": "your_encrypt_key_here",
    "RoutePrefix": "feishu/webbook",
    "EnableRequestLogging": true,
    "EnablePerformanceMonitoring": true,
    "MaxConcurrentEvents": 20
  }
}
```

在代码中：

```csharp
// 简单注册，使用默认配置节
builder.Services.AddFeishuWebbook(builder.Configuration);

// 使用自定义配置节
builder.Services.AddFeishuWebbook(builder.Configuration, "CustomSection");

// 添加事件处理器
builder.Services.AddFeishuWebbook(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .AddHandler<UserEventHandler>();
```

## 事件处理

### 支持的事件类型

库支持所有飞书事件类型，包括但不限于：

- `im.message.receive_v1` - 接收消息
- `im.chat.member_user_added_v1` - 用户加入群聊
- `im.chat.member_user_deleted_v1` - 用户离开群聊
- `contact.user.created_v3` - 用户创建
- `contact.user.updated_v3` - 用户更新
- `contact.user.deleted_v3` - 用户删除

### 创建处理器

```csharp
// 简单处理器
public class SimpleEventHandler : IFeishuEventHandler
{
    public string SupportedEventType => FeishuEventTypes.IMMessageReceiveV1;
    
    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        // 处理逻辑
        await Task.CompletedTask;
    }
}

// 继承基类处理器
public abstract class BaseFeishuEventHandler : IFeishuEventHandler
{
    public abstract string SupportedEventType { get; }
    
    public virtual async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        // 基础处理逻辑
        await HandleEventInternalAsync(eventData, cancellationToken);
    }
    
    protected abstract Task HandleEventInternalAsync(EventData eventData, CancellationToken cancellationToken);
}
```

### 注册处理器

```csharp
// 使用链式调用添加处理器
builder.Services.AddFeishuWebbook(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .AddHandler<UserEventHandler>()
    .AddHandler<DepartmentEventHandler>();

// 使用建造者模式进行复杂配置
builder.Services.AddFeishuWebbookBuilder()
    .ConfigureFrom(configuration)
    .AddHandler<MessageEventHandler>()
    .AddHandler<UserEventHandler>()
    .EnableControllers()
    .Build();
```

## 飞书平台配置

### 1. 创建事件订阅

1. 登录飞书开放平台
2. 进入你的应用详情页
3. 点击"事件订阅"
4. 配置请求网址：`https://your-domain.com/feishu/webbook`
5. 设置验证 Token 和加密 Key

### 2. 配置事件类型

选择你需要订阅的事件类型：

- 消息事件
- 群聊事件
- 用户事件
- 部门事件
- 等...

### 3. 发布应用

配置完成后发布应用，飞书服务器将开始向你的端点推送事件。

## 监控和诊断

### 性能监控

```csharp
// 方式一：通过建造者模式启用
builder.Services.AddFeishuWebbookBuilder()
    .ConfigureFrom(configuration)
    .EnableMetrics()
    .Build();

// 方式二：通过配置选项启用
builder.Services.AddFeishuWebbook(options =>
{
    options.EnablePerformanceMonitoring = true; // 启用性能监控
});
```

### 健康检查

```csharp
// 使用建造者模式启用健康检查
builder.Services.AddFeishuWebbookBuilder()
    .ConfigureFrom(configuration)
    .EnableHealthChecks()
    .Build();

// 添加健康检查端点
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/health"); // 健康检查端点
```

### 日志记录

库使用标准的 .NET 日志记录框架，可以配置不同的日志级别：

```json
{
  "Logging": {
    "LogLevel": {
      "Mud.Feishu.Webbook": "Information",
      "Mud.Feishu.Webbook.Services": "Debug"
    }
  }
}
```

## 最佳实践

### 1. 错误处理

```csharp
public class RobustEventHandler : IFeishuEventHandler
{
    private readonly ILogger<RobustEventHandler> _logger;

    public string SupportedEventType => FeishuEventTypes.IMMessageReceiveV1;

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            // 业务逻辑
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理事件时发生错误: {EventId}", eventData.EventId);
            // 不要重新抛出异常，避免影响其他处理器
        }
    }
}
```

### 2. 异步处理

```csharp
public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
{
    // 使用异步 API
    await ProcessMessageAsync(eventData, cancellationToken);
    
    // 避免阻塞调用
    // 不要使用 .Result 或 .Wait()
}
```

### 3. 资源管理

```csharp
public class ResourceAwareHandler : IFeishuEventHandler, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(5, 5); // 限制并发数

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // 处理逻辑
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
```

## 故障排除

### 常见问题

1. **验证失败**
   - 检查 `VerificationToken` 是否正确
   - 确认请求 URL 配置正确

2. **解密失败**
   - 检查 `EncryptKey` 是否正确
   - 确认飞书平台已启用加密

3. **签名验证失败**
   - 检查时间同步
   - 确认请求没有被代理服务器修改

4. **事件处理失败**
   - 检查事件处理器是否正确注册
   - 查看日志中的详细错误信息

### 调试技巧

```csharp
// 启用详细日志
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// 启用请求日志记录
builder.Services.AddFeishuWebbook(options =>
{
    options.EnableRequestLogging = true;
    options.EnablePerformanceMonitoring = true;
});
```

## 许可证

本项目采用 MIT 许可证。详见 [LICENSE](../../../LICENSE-MIT) 文件。