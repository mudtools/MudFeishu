# Mud.Feishu.Webhook

飞书事件订阅与处理的 Webhook 组件，提供完整的飞书事件接收、验证、解密和分发功能。

**🚀 新特性：极简API** - 一行代码完成服务注册，开箱即用！

## 功能特性

- ✅ **极简API**：一行代码完成服务注册，开箱即用
- ✅ **灵活配置**：支持配置文件、代码配置和建造者模式
- ✅ **自动事件路由**：根据事件类型自动分发到对应的处理器
- ✅ **安全验证**：支持事件订阅验证、请求签名验证和时间戳验证
- ✅ **加密解密**：内置 AES-256-CBC 解密功能，自动处理飞书加密事件
- ✅ **使用模式**：支持中间件模式
- ✅ **依赖注入**：完全集成 .NET 依赖注入容器
- ✅ **异常处理**：完善的异常处理和日志记录
- ✅ **性能监控**：可选的性能指标收集和监控
- ✅ **健康检查**：内置健康检查端点
- ✅ **异步处理**：完全异步的事件处理机制
- ✅ **并发控制**：可配置的并发事件处理数量限制
- ✅ **分布式支持**：提供分布式去重接口，支持 Redis 等外部存储
- ✅ **配置热更新**：支持运行时配置变更，无需重启服务

## 快速开始

### 1. 安装 NuGet 包

```bash
dotnet add package Mud.Feishu.Webhook
```

### 2. 最简配置（一行代码）

在 `Program.cs` 中：

```csharp
using Mud.Feishu.Webhook;

var builder = WebApplication.CreateBuilder(args);

// 一行代码注册Webhook服务（需要至少一个事件处理器）
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook(); // 添加中间件
app.Run();
```

> 💡 **说明**：Webhook 服务使用中间件模式，通过 `app.UseFeishuWebhook()` 自动注册端点。

### 3. 完整配置（添加事件处理器）

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .AddHandler<UserEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook();
app.Run();
```

### 4. 配置文件

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

## 🏗️ 服务注册方式

### 🚀 从配置文件注册（推荐）

```csharp
// 一行代码完成基础配置（需要至少一个事件处理器）
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .Build();
```

### ⚙️ 代码配置

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

### 🔧 高级建造者模式

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .ConfigureFrom(configuration)
    .EnableHealthChecks()
    .EnableMetrics()
    .AddHandler<MessageReceiveEventHandler>()
    .Build();
```

## 使用模式

### 中间件模式

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook(); // 自动处理路由前缀下的请求
app.Run();
```

> 💡 **说明**：Webhook 服务目前仅支持中间件模式，通过配置 `RoutePrefix` 来自定义路由路径。

## 创建事件处理器

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
| `RoutePrefix` | string | "feishu/Webhook" | Webhook 路由前缀 |
| `AutoRegisterEndpoint` | bool | true | 是否自动注册端点 |

### 安全配置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ValidateSourceIP` | bool | false | 是否验证来源 IP |
| `AllowedSourceIPs` | HashSet\<string\> | - | 允许的源 IP 地址列表 |
| `AllowedHttpMethods` | HashSet\<string\> | ["POST"] | 允许的 HTTP 方法 |
| `MaxRequestBodySize` | long | 10MB | 最大请求体大小 |
| `EnforceHeaderSignatureValidation` | bool | true | 是否强制验证请求头签名 |
| `EnableBodySignatureValidation` | bool | true | 是否在服务层再次验证请求体签名 |

### 性能配置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `MaxConcurrentEvents` | int | 10 | 最大并发事件数，支持热更新 |
| `EventHandlingTimeoutMs` | int | 30000 | 事件处理超时时间（毫秒） |
| `EnablePerformanceMonitoring` | bool | false | 是否启用性能监控 |

### 日志配置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `EnableRequestLogging` | bool | true | 是否启用请求日志记录 |
| `EnableExceptionHandling` | bool | true | 是否启用异常处理 |

## 注册处理器

```csharp
// 使用链式调用添加处理器
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .AddHandler<UserEventHandler>()
    .AddHandler<DepartmentEventHandler>()
    .Build();

// 使用建造者模式进行复杂配置
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .ConfigureFrom(configuration)
    .AddHandler<MessageEventHandler>()
    .AddHandler<UserEventHandler>()
    .Build();
```

## 支持的事件类型

库支持所有飞书事件类型，包括但不限于：

- `im.message.receive_v1` - 接收消息
- `im.chat.member_user_added_v1` - 用户加入群聊
- `im.chat.member_user_deleted_v1` - 用户离开群聊
- `contact.user.created_v3` - 用户创建
- `contact.user.updated_v3` - 用户更新
- `contact.user.deleted_v3` - 用户删除

## 飞书平台配置

### 1. 创建事件订阅

1. 登录飞书开放平台
2. 进入你的应用详情页
3. 点击"事件订阅"
4. 配置请求网址：`https://your-domain.com/feishu/Webhook`
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
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .ConfigureFrom(configuration)
    .EnableMetrics()
    .Build();

// 方式二：通过配置选项启用
builder.Services.CreateFeishuWebhookServiceBuilder(options =>
{
    options.EnablePerformanceMonitoring = true; // 启用性能监控
}).AddHandler<MessageEventHandler>()
    .Build();
```

### 健康检查

```csharp
// 使用建造者模式启用健康检查
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
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
      "Mud.Feishu.Webhook": "Information",
      "Mud.Feishu.Webhook.Services": "Debug"
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

    public string SupportedEventType => "im.message.receive_v1";

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
   - 生产环境确保 `EnforceHeaderSignatureValidation` 设置为 true

4. **事件处理失败**
   - 检查事件处理器是否正确注册
   - 查看日志中的详细错误信息

5. **分布式部署事件重复**
   - 默认使用内存去重，多实例部署需要实现分布式去重
   - 参考 `IFeishuWebhookDistributedDeduplicator` 接口自定义 Redis 实现

6. **超时处理**
   - 检查 `EventHandlingTimeoutMs` 配置是否合理
   - 确保事件处理逻辑支持取消令牌

### 调试技巧

```csharp
// 启用详细日志
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// 启用请求日志记录
builder.Services.CreateFeishuWebhookServiceBuilder(options =>
{
    options.EnableRequestLogging = true;
    options.EnablePerformanceMonitoring = true;
}).AddHandler<MessageEventHandler>()
    .Build();
```

## 快速参考

### 最常用的注册方式

```csharp
// 方式一：最简化（需要至少一个事件处理器）
builder.Services.CreateFeishuWebhookServiceBuilder(configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .Build();

// 方式二：简化 + 处理器
builder.Services.CreateFeishuWebhookServiceBuilder(configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .Build();

// 方式三：代码配置
builder.Services.CreateFeishuWebhookServiceBuilder(options => {
    options.VerificationToken = "your_token";
    options.EncryptKey = "your_key";
}).AddHandler<MessageEventHandler>()
    .Build();

// 方式四：建造者模式（复杂配置）
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .ConfigureFrom(configuration)
    .EnableMetrics()
    .AddHandler<Handler>()
    .Build();
```

---

**🚀 立即开始使用飞书Webhook，构建稳定可靠的事件处理系统！**