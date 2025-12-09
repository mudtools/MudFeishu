# Mud.Feishu.Webbook

飞书事件订阅与处理的 Webbook 组件，提供完整的飞书事件接收、验证、解密和分发功能。

## 功能特性

- ✅ **自动事件路由**：根据事件类型自动分发到对应的处理器
- ✅ **安全验证**：支持事件订阅验证、请求签名验证和时间戳验证
- ✅ **加密解密**：内置 AES-256-CBC 解密功能，自动处理飞书加密事件
- ✅ **中间件支持**：提供中间件和控制器两种使用模式
- ✅ **依赖注入**：完全集成 .NET 依赖注入容器
- ✅ **异常处理**：完善的异常处理和日志记录
- ✅ **性能监控**：可选的性能指标收集和监控
- ✅ **健康检查**：内置健康检查端点
- ✅ **可配置性**：丰富的配置选项，支持代码和配置文件配置
- ✅ **异步处理**：完全异步的事件处理机制
- ✅ **并发控制**：可配置的并发事件处理数量限制

## 快速开始

### 1. 安装 NuGet 包

```bash
dotnet add package Mud.Feishu.Webbook
```

### 2. 配置服务

在 `Program.cs` 中添加服务配置：

```csharp
using Mud.Feishu.Webbook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 添加飞书 Webbook 服务
builder.Services.AddFeishuWebbook(options =>
{
    options.VerificationToken = "your_verification_token_here";
    options.EncryptKey = "your_encrypt_key_here";
    options.RoutePrefix = "feishu/webbook";
    options.EnableRequestLogging = true;
});

// 添加自定义事件处理器
builder.Services.AddFeishuEventHandler<YourCustomEventHandler>();

var app = builder.Build();

// 添加飞书 Webbook 中间件
app.UseFeishuWebbook();

app.Run();
```

### 3. 创建事件处理器

```csharp
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;

public class MessageEventHandler : IFeishuEventHandler
{
    private readonly ILogger<MessageEventHandler> _logger;

    public MessageEventHandler(ILogger<MessageEventHandler> logger)
    {
        _logger = logger;
    }

    public string SupportedEventType => FeishuEventTypes.IMMessageReceiveV1;

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("收到消息事件: {EventId}", eventData.EventId);
        
        // 处理消息逻辑
        var messageData = JsonSerializer.Deserialize<MessageReceiveResult>(
            eventData.Event?.ToString() ?? string.Empty);
        
        // 你的业务逻辑...
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
builder.Services.AddFeishuWebbook(options =>
{
    options.VerificationToken = "your_token";
    options.EncryptKey = "your_key";
});

var app = builder.Build();
app.UseFeishuWebbook(); // 自动处理路由前缀下的请求
app.Run();
```

### 控制器模式

```csharp
// Program.cs
builder.Services.AddFeishuWebbook(options =>
{
    options.VerificationToken = "your_token";
    options.EncryptKey = "your_key";
    options.AutoRegisterEndpoint = false; // 禁用中间件
});

builder.Services.AddControllers(); // 启用控制器

var app = builder.Build();
// app.UseFeishuWebbook(); // 不使用中间件
app.MapControllers(); // 使用控制器路由
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
builder.Services.AddFeishuWebbook("FeishuWebbook");
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
// 注册单个处理器
builder.Services.AddFeishuEventHandler<MessageEventHandler>();

// 批量注册多个处理器
builder.Services.AddFeishuEventHandlers(
    typeof(MessageEventHandler),
    typeof(UserAddedEventHandler),
    typeof(UserDeletedEventHandler));
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
builder.Services.AddFeishuWebbook(options =>
{
    options.EnablePerformanceMonitoring = true; // 启用性能监控
});
```

### 健康检查

```csharp
// 添加健康检查
builder.Services.AddHealthChecks()
    .AddCheck<FeishuWebbookHealthCheck>("feishu-webbook");

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