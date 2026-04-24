# Mud.Feishu.Webhook

飞书事件订阅与处理的 Webhook 组件，提供完整的飞书事件接收、验证、解密和分发功能。

**🚀 新特性：极简API** - 一行代码完成服务注册，开箱即用！

[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.Webhook.svg)](https://www.nuget.org/packages/Mud.Feishu.Webhook/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE-MIT)

## 功能特性

### 🚀 核心能力

- ✅ **极简API**：一行代码完成服务注册，开箱即用
- ✅ **灵活配置**：支持配置文件、代码配置和建造者模式
- ✅ **自动事件路由**：根据事件类型自动分发到对应的处理器
- ✅ **中间件模式**：使用 .NET 标准中间件模式，集成简单
- ✅ **依赖注入**：完全集成 .NET 依赖注入容器

### 🔒 安全防护

- ✅ **安全验证**：支持事件订阅验证、请求签名验证和时间戳验证
- ✅ **加密解密**：内置 AES-256-CBC 解密功能，自动处理飞书加密事件
- ✅ **安全加固**：强化 IP 验证、签名验证和密钥安全检查
- ✅ **请求频率限制**：内置滑动窗口限流中间件，防止恶意请求
- ✅ **内容安全**：仅接受 `application/json` 请求，防止 DoS 攻击
- ✅ **日志脱敏**：自动脱敏敏感字段防止信息泄露

### ⚡ 性能与可靠性

- ✅ **异步处理**：完全异步的事件处理机制
- ✅ **并发控制**：可配置的并发事件处理数量限制，支持热更新
- ✅ **后台处理模式**：支持异步后台处理，避免飞书超时重试
- ✅ **容错机制**：失败事件指数退避重试
- ✅ **分布式支持**：提供分布式去重接口，支持 Redis 等外部存储

### 📊 监控与运维

- ✅ **异常处理**：完善的异常处理和日志记录
- ✅ **性能监控**：可选的性能指标收集和监控
- ✅ **健康检查**：内置健康检查端点
- ✅ **配置热更新**：支持运行时配置变更，无需重启服务
- ✅ **内存管理**：Nonce 过期清理，防止内存泄漏
- ✅ **配置锁定**：生产环境强制安全检查

### 🌐 兼容性与扩展

- ✅ **跨平台兼容**：支持 .NET Standard 2.0、.NET 6.0、.NET 8.0、.NET 10.0
- ✅ **多应用支持**：支持多个飞书应用共享同一个 Webhook 端点
- ✅ **事件处理拦截器**：前置/后置事件处理拦截器机制
- ✅ **流式处理**：流式请求体读取，优化内存使用

## 快速开始

### 1. 安装 NuGet 包

```bash
dotnet add package Mud.Feishu.Webhook
```

### 2. 最简配置（一行代码）

在 `Program.cs` 中：

```csharp
using Mud.Feishu.Webhook;
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 一行代码注册Webhook服务（需要至少一个事件处理器）
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();

// 添加飞书Webhook限流中间件（可选，推荐在生产环境启用）
app.UseFeishuRateLimit();

// 添加飞书Webhook中间件
app.UseFeishuWebhook();

app.Run();
```

> 💡 **说明**：Webhook 服务使用中间件模式，通过 `app.UseFeishuWebhook()` 自动注册端点。默认路由为 `/feishu/{AppKey}`，其中 `{AppKey}` 为应用键。
>
> ⚠️ **注意**：限流中间件应该在 Webhook 中间件之前注册，以确保限流策略能够正确应用。

### 3. 完整配置（添加多个事件处理器）

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 注册多个事件处理器
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()      // 消息接收事件
    .AddHandler<DepartmentCreatedEventHandler>()   // 部门创建事件
    .AddHandler<DepartmentUpdateEventHandler>()    // 部门更新事件
    .AddHandler<DepartmentDeleteEventHandler>()    // 部门删除事件
    .Build();

var app = builder.Build();

// 添加飞书Webhook中间件
app.UseFeishuWebhook();

app.Run();
```

### 4. 配置文件（appsettings.json）

```json
{
  "FeishuWebhook": {
    "VerificationToken": "your_verification_token",
    "EncryptKey": "your_encrypt_key_32_bytes_long",
    "GlobalRoutePrefix": "feishu",
    "AutoRegisterEndpoint": true,
    "EnableRequestLogging": true,
    "EnableExceptionHandling": true,
    "EventHandlingTimeoutMs": 30000,
    "MaxConcurrentEvents": 10,
    "EnablePerformanceMonitoring": false,
    "AllowedHttpMethods": ["POST"],
    "MaxRequestBodySize": 10485760,
    "AllowedSourceIPs": [],
    "EnforceHeaderSignatureValidation": true,
    "EnableBodySignatureValidation": true,
    "TimestampToleranceSeconds": 30,
    "EnableBackgroundProcessing": false,
    "Retry": {
      "EnableRetry": false,
      "MaxRetryCount": 3,
      "InitialRetryDelaySeconds": 10,
      "RetryDelayMultiplier": 2.0,
      "MaxRetryDelaySeconds": 300,
      "RetryPollIntervalSeconds": 30,
      "MaxRetryPerPoll": 10
    },
    "RateLimit": {
      "EnableRateLimit": false,
      "WindowSizeSeconds": 60,
      "MaxRequestsPerWindow": 100,
      "EnableIpRateLimit": true,
      "TooManyRequestsStatusCode": 429,
      "TooManyRequestsMessage": "请求过于频繁，请稍后再试",
      "WhitelistIPs": ["127.0.0.1", "::1"]
    },
    "Apps": {
      "app1": {
        "AppKey": "cli_a1b2c3d4e5f6g7h8",
        "VerificationToken": "your_app1_verification_token",
        "EncryptKey": "your_app1_encrypt_key_32_bytes_long"
      },
      "app2": {
        "AppKey": "cli_h8g7f6e5d4c3b2a1",
        "VerificationToken": "your_app2_verification_token",
        "EncryptKey": "your_app2_encrypt_key_32_bytes_long"
      }
    }
  }
}
```

## 🏗️ 服务注册方式

### 🚀 方式一：从配置文件注册（推荐）

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 从 appsettings.json 读取配置
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook();
app.Run();
```

### ⚙️ 方式二：代码配置

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 通过代码配置
builder.Services.CreateFeishuWebhookServiceBuilder(options =>
{
    options.GlobalRoutePrefix = "feishu";
    options.EnableRequestLogging = true;
    options.EnableExceptionHandling = true;
    options.MaxConcurrentEvents = 10;
})
.AddHandler<MessageEventHandler>()
.Build();

var app = builder.Build();
app.UseFeishuWebhook();
app.Run();
```

**注意**：多应用场景下，请在 `FeishuWebhookOptions` 的 `Apps` 字典中配置每个应用的 `VerificationToken` 和 `EncryptKey`，或通过配置文件设置。

### 🔌 方式三：添加事件拦截器

```csharp
using Mud.Feishu.Webhook.Extensions;
using Mud.Feishu.Abstractions.Interceptors;

var builder = WebApplication.CreateBuilder(args);

// 添加内置和自定义拦截器
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddInterceptor<LoggingEventInterceptor>()  // 内置日志拦截器
    .AddInterceptor<TelemetryEventInterceptor>()  // 内置遥测拦截器
    .AddInterceptor<AuditLogInterceptor>()  // 自定义审计拦截器
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook();
app.Run();
```

### 🔧 方式四：高级建造者模式

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 使用建造者模式进行复杂配置
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .EnableHealthChecks()    // 启用健康检查
    .AddHandler<MessageReceiveEventHandler>()
    .AddHandler<DepartmentCreatedEventHandler>()
    .Build();

var app = builder.Build();

// 添加健康检查端点
app.MapHealthChecks("/health");

app.UseFeishuWebhook();
app.Run();
```

### 🔥 方式五：指定配置节名称

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 从自定义配置节读取
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration, "MyFeishuConfig")
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook();
app.Run();
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

### 方式一：实现 IFeishuEventHandler 接口

```csharp
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;
using System.Text.Json;

public class MessageReceiveEventHandler : IFeishuEventHandler
{
    private readonly ILogger<MessageReceiveEventHandler> _logger;

    public MessageReceiveEventHandler(ILogger<MessageReceiveEventHandler> logger)
    {
        _logger = logger;
    }

    // 指定支持的事件类型
    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("收到消息事件: EventId={EventId}, EventType={EventType}",
            eventData.EventId, eventData.EventType);

        // 处理消息逻辑
        var messageData = JsonSerializer.Deserialize<MessageEventData>(
            eventData.Event?.ToString() ?? string.Empty);

        // 你的业务逻辑...
        _logger.LogInformation("处理消息: {MessageId}", messageData?.MessageId);

        await Task.CompletedTask;
    }
}

public class MessageEventData
{
    public string MessageId { get; set; }
    public string Content { get; set; }
    // ... 其他字段
}
```

### 方式二：继承基类处理器（推荐）

使用 `Mud.Feishu.Abstractions.EventHandlers` 命名空间下的基类处理器，提供类型安全和自动去重：

```csharp
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.DataModels.Organization;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;

/// <summary>
/// 部门创建事件处理器
/// </summary>
public class DemoDepartmentEventHandler : DepartmentCreatedEventHandler
{
    private readonly DemoEventService _eventService;

    public DemoDepartmentEventHandler(
        IFeishuEventDeduplicator businessDeduplicator,
        ILogger<DemoDepartmentEventHandler> logger,
        DemoEventService eventService)
        : base(businessDeduplicator, logger)
    {
        _eventService = eventService;
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        DepartmentCreatedResult? eventEntity,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("处理部门创建事件: 部门ID={DepartmentId}, 部门名={DepartmentName}",
            eventEntity.DepartmentId, eventEntity.Name);

        // 你的业务逻辑
        await _eventService.RecordDepartmentEventAsync(eventEntity, cancellationToken);

        // 模拟权限初始化
        _logger.LogInformation("初始化部门权限: {DepartmentName}", eventEntity.Name);

        // 模拟通知部门主管
        if (!string.IsNullOrWhiteSpace(eventEntity.LeaderUserId))
        {
            _logger.LogInformation("通知部门主管: {LeaderUserId}", eventEntity.LeaderUserId);
        }
    }
}
```

### 可用的基类事件处理器

- `DepartmentCreatedEventHandler` - 部门创建事件
- `DepartmentUpdateEventHandler` - 部门更新事件
- `DepartmentDeleteEventHandler` - 部门删除事件
- 更多处理器请参考 `Mud.Feishu.Abstractions.EventHandlers` 命名空间

## 配置选项

### 基本配置

| 选项                   | 类型   | 默认值   | 说明                                   |
| ---------------------- | ------ | -------- | -------------------------------------- |
| `VerificationToken`    | string | -        | 飞书事件订阅验证 Token                 |
| `EncryptKey`           | string | -        | 飞书事件加密密钥（32字节）             |
| `GlobalRoutePrefix`    | string | "feishu" | 全局路由前缀（所有应用共享的基础路径） |
| `AutoRegisterEndpoint` | bool   | true     | 是否自动注册端点                       |

### 多应用配置

| 选项                              | 类型                                          | 默认值 | 说明                                               |
| --------------------------------- | --------------------------------------------- | ------ | -------------------------------------------------- |
| `Apps`                            | Dictionary\<string, FeishuAppWebhookOptions\> | {}     | 应用配置集合（AppKey -> 应用配置）                 |
| `Apps.{AppKey}.AppKey`            | string                                        | -      | 应用键（用于标识应用，仅允许字母、数字、下划线和连字符） |
| `Apps.{AppKey}.VerificationToken` | string                                        | -      | 应用验证 Token                                     |
| `Apps.{AppKey}.EncryptKey`        | string                                        | -      | 应用加密 Key（32字节）                             |
| `Apps.{AppKey}.Description`       | string?                                       | null   | 应用描述（可选）                                   |
| `Apps.{AppKey}.TimestampToleranceSeconds` | int                                    | -1     | 时间戳容差（-1 继承全局）                          |
| `Apps.{AppKey}.EventHandlingTimeoutMs` | int                                     | -1     | 事件处理超时（-1 继承全局）                        |
| `Apps.{AppKey}.EnforceHeaderSignatureValidation` | bool                           | false  | 是否强制签名验证（不继承全局）                     |
| `Apps.{AppKey}.EnableBodySignatureValidation` | bool                              | true   | 是否验证请求体签名（不继承全局）                   |
| `Apps.{AppKey}.EnableExceptionHandling` | bool?                                  | null   | 是否启用异常处理（null 继承全局）                  |
| `Apps.{AppKey}.EnablePerformanceMonitoring` | bool?                                 | null   | 是否启用性能监控（null 继承全局）                  |

### 安全配置

| 选项                               | 类型              | 默认值   | 说明                                           |
| ---------------------------------- | ----------------- | -------- | ---------------------------------------------- |
| `AllowedSourceIPs`                 | HashSet\<string\> | []       | 允许的源 IP 地址列表（非空时自动启用 IP 验证） |
| `AllowedHttpMethods`               | HashSet\<string\> | ["POST"] | 允许的 HTTP 方法                               |
| `MaxRequestBodySize`               | long              | 10MB     | 最大请求体大小                                 |
| `EnforceHeaderSignatureValidation` | bool              | true     | 是否强制验证请求头签名                         |
| `EnableBodySignatureValidation`    | bool              | true     | 是否在服务层再次验证请求体签名                 |
| `TimestampToleranceSeconds`        | int               | 60       | 时间戳验证容错范围（秒）                       |

### 性能配置

| 选项                          | 类型 | 默认值 | 说明                       |
| ----------------------------- | ---- | ------ | -------------------------- |
| `MaxConcurrentEvents`         | int  | 10     | 最大并发事件数，支持热更新 |
| `EventHandlingTimeoutMs`      | int  | 30000  | 事件处理超时时间（毫秒）   |
| `EnablePerformanceMonitoring` | bool | false  | 是否启用性能监控           |
| `EnableBackgroundProcessing`  | bool | false  | 是否启用异步后台处理模式   |

### 日志配置

| 选项                      | 类型 | 默认值 | 说明                 |
| ------------------------- | ---- | ------ | -------------------- |
| `EnableRequestLogging`    | bool | true   | 是否启用请求日志记录 |
| `EnableExceptionHandling` | bool | true   | 是否启用异常处理     |

### 失败事件重试配置

| 选项                             | 类型   | 默认值 | 说明                         |
| -------------------------------- | ------ | ------ | ---------------------------- |
| `Retry.EnableRetry`              | bool   | false  | 是否启用失败事件重试         |
| `Retry.MaxRetryCount`            | int    | 3      | 最大重试次数                 |
| `Retry.InitialRetryDelaySeconds` | int    | 10     | 初始重试延迟（秒）           |
| `Retry.RetryDelayMultiplier`     | double | 2.0    | 重试延迟倍数（指数退避）     |
| `Retry.MaxRetryDelaySeconds`     | int    | 300    | 最大重试延迟（秒）           |
| `Retry.RetryPollIntervalSeconds` | int    | 30     | 重试轮询间隔（秒）           |
| `Retry.MaxRetryPerPoll`          | int    | 10     | 每次轮询处理的最大失败事件数 |

### 请求频率限制配置

| 选项                                  | 类型              | 默认值                     | 说明                           |
| ------------------------------------- | ----------------- | -------------------------- | ------------------------------ |
| `RateLimit.EnableRateLimit`           | bool              | false                      | 是否启用请求频率限制           |
| `RateLimit.WindowSizeSeconds`         | int               | 60                         | 时间窗口大小（秒）             |
| `RateLimit.MaxRequestsPerWindow`      | int               | 100                        | 每个时间窗口内允许的最大请求数 |
| `RateLimit.EnableIpRateLimit`         | bool              | true                       | 是否基于 IP 限流               |
| `RateLimit.TooManyRequestsStatusCode` | int               | 429                        | 超出限制时的响应状态码         |
| `RateLimit.TooManyRequestsMessage`    | string            | "请求过于频繁，请稍后再试" | 超出限制时的响应消息           |
| `RateLimit.WhitelistIPs`              | HashSet\<string\> | []                         | 白名单 IP 列表（不参与限流）   |

## 高级功能

### 多应用支持

支持多个飞书应用共享同一个 Webhook 端点：

```json
{
  "FeishuWebhook": {
    "Apps": {
      "app1": {
        "AppKey": "cli_a1b2c3d4e5f6g7h8",
        "VerificationToken": "your_app1_verification_token",
        "EncryptKey": "your_app1_encrypt_key_32_bytes_long"
      },
      "app2": {
        "AppKey": "cli_h8g7f6e5d4c3b2a1",
        "VerificationToken": "your_app2_verification_token",
        "EncryptKey": "your_app2_encrypt_key_32_bytes_long"
      }
    }
  }
}
```

每个应用的路由将自动注册为 `/feishu/{AppKey}`。

### 请求频率限制

内置滑动窗口限流中间件，防止恶意请求：

```json
{
  "FeishuWebhook": {
    "RateLimit": {
      "EnableRateLimit": true,
      "WindowSizeSeconds": 60,
      "MaxRequestsPerWindow": 100,
      "EnableIpRateLimit": true,
      "WhitelistIPs": ["127.0.0.1", "::1"]
    }
  }
}
```

**多应用支持**：限流策略基于 `(AppKey, IP)` 维度，不同应用的请求不会相互影响。

**使用方式**：

```csharp
var app = builder.Build();

// 添加飞书Webhook限流中间件（可选，推荐在生产环境启用）
app.UseFeishuRateLimit();

// 添加飞书Webhook中间件
app.UseFeishuWebhook();

app.Run();
```

### 后台处理模式

启用后台处理模式，避免飞书超时重试：

```json
{
  "FeishuWebhook": {
    "EnableBackgroundProcessing": true
  }
}
```

```csharp
// 启用后台处理模式后，中间件会立即返回成功响应
// 然后在后台异步处理事件，适用于耗时较长的业务逻辑
builder.Services.CreateFeishuWebhookServiceBuilder(options =>
{
    options.EnableBackgroundProcessing = true;
}).AddHandler<LongRunningEventHandler>()
    .Build();
```

## 事件拦截器（Interceptors）

事件拦截器允许在事件处理前后执行自定义逻辑，如日志记录、指标收集、权限验证等。

### 内置拦截器

**LoggingEventInterceptor** - 记录事件处理日志

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddInterceptor<LoggingEventInterceptor>()  // 记录事件处理开始和结束
    .AddHandler<MessageEventHandler>()
    .Build();
```

**TelemetryEventInterceptor** - 遥测数据收集

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddInterceptor<TelemetryEventInterceptor>(sp =>
        new TelemetryEventInterceptor("My.Application"))  // 指定应用名称
    .AddHandler<MessageEventHandler>()
    .Build();
```

### 自定义拦截器

创建自定义拦截器需要实现 `IFeishuEventInterceptor` 接口：

```csharp
using Mud.Feishu.Abstractions;

/// <summary>
/// 审计日志拦截器示例
/// </summary>
public class AuditLogInterceptor : IFeishuEventInterceptor
{
    private readonly ILogger<AuditLogInterceptor> _logger;

    public AuditLogInterceptor(ILogger<AuditLogInterceptor> logger)
        => _logger = logger;

    /// <summary>
    /// 事件处理前拦截
    /// </summary>
    /// <returns>返回 false 将中断事件处理流程</returns>
    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[审计] 事件开始: {EventType}, EventId: {EventId}, TenantKey: {TenantKey}",
            eventType, eventData.EventId, eventData.TenantKey);
        return Task.FromResult(true); // 返回 true 继续处理，false 中断
    }

    /// <summary>
    /// 事件处理后拦截
    /// </summary>
    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        if (exception == null)
        {
            _logger.LogInformation("[审计] 事件成功: {EventType}, EventId: {EventId}", eventType, eventData.EventId);
        }
        else
        {
            _logger.LogError(exception, "[审计] 事件失败: {EventType}, EventId: {EventId}", eventType, eventData.EventId);
        }
        return Task.CompletedTask;
    }
}
```

### 注册自定义拦截器

```csharp
// 类型注册
.AddInterceptor<AuditLogInterceptor>()

// 工厂注册
.AddInterceptor(sp => new AuditLogInterceptor(
    sp.GetRequiredService<ILogger<AuditLogInterceptor>>()))

// 实例注册
var interceptor = new AuditLogInterceptor(logger);
.AddInterceptor(interceptor)
```

### 拦截器执行顺序

拦截器按注册顺序依次执行，完整流程：

```
Webhook 事件到达
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

### 应用场景

- **日志记录**：记录事件处理的开始、成功、失败
- **指标收集**：统计事件处理时间、成功率等
- **安全审计**：记录敏感事件的处理情况
- **权限控制**：根据事件类型或内容决定是否处理
- **性能监控**：记录处理耗时，识别性能瓶颈
- **业务追踪**：将事件信息写入审计日志或追踪系统

## 注册事件处理器

### 链式调用注册多个处理器

```csharp
using Mud.Feishu.Webhook.Extensions;

builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()      // 消息接收
    .AddHandler<DepartmentCreatedEventHandler>()   // 部门创建
    .AddHandler<DepartmentUpdateEventHandler>()    // 部门更新
    .AddHandler<DepartmentDeleteEventHandler>()    // 部门删除
    .Build();
```

### 使用工厂方法注册

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<MessageEventHandler>>();
        var myService = sp.GetRequiredService<MyCustomService>();
        return new MessageEventHandler(logger, myService);
    })
    .Build();
```

### 使用实例注册

```csharp
var handler = new MessageEventHandler(loggerFactory.CreateLogger<MessageEventHandler>());

builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler(handler)
    .Build();
```

## 支持的事件类型

本库支持所有飞书开放平台的事件类型。常见事件类型包括：

### 消息事件

- `im.message.receive_v1` - 接收消息事件
- `im.message.recalled_v1` - 消息撤回事件

### 群聊事件

- `im.chat.member_user.added_v1` - 用户加入群聊
- `im.chat.member_user.withdrawn_v1` - 用户离开群聊
- `im.chat.disbanded_v1` - 群聊解散
- `im.chat.updated_v1` - 群信息变更

### 通讯录事件

- `contact.user.created_v3` - 员工入职
- `contact.user.updated_v3` - 员工信息变更
- `contact.user.deleted_v3` - 员工离职
- `contact.department.created_v3` - 部门创建
- `contact.department.updated_v3` - 部门信息变更
- `contact.department.deleted_v3` - 部门删除

### 审批事件

- `approval.approval.approved_v1` - 审批通过
- `approval.approval.rejected_v1` - 审批拒绝
- `approval.approval.updated_v1` - 审批更新

### 任务事件

- `task.task.created_v1` - 任务创建
- `task.task.updated_v1` - 任务更新

> 💡 **提示**：更多事件类型请参考[飞书开放平台事件列表](https://open.feishu.cn/document/ukTMukTMukTM/uUTNz4SN1MjL1UzM)

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

## 自定义验证器和密钥提供程序

### 自定义签名验证器

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .UseSignatureValidator<MyCustomSignatureValidator>()
    .AddHandler<MessageEventHandler>()
    .Build();
```

### 自定义时间戳验证器

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .UseTimestampValidator<MyCustomTimestampValidator>()
    .AddHandler<MessageEventHandler>()
    .Build();
```

### 自定义 Nonce 验证器

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .UseNonceValidator<MyCustomNonceValidator>()
    .AddHandler<MessageEventHandler>()
    .Build();
```

### 自定义订阅验证器

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .UseSubscriptionValidator<MyCustomSubscriptionValidator>()
    .AddHandler<MessageEventHandler>()
    .Build();
```

### 自定义加密密钥提供程序

支持从外部源（如 Azure KeyVault、AWS Secrets Manager、环境变量等）获取加密密钥：

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .UseEncryptKeyProvider<AzureKeyVaultEncryptKeyProvider>()
    .AddHandler<MessageEventHandler>()
    .Build();
```

实现 `IEncryptKeyProvider` 接口：

```csharp
using Mud.Feishu.Webhook;

public class AzureKeyVaultEncryptKeyProvider : IEncryptKeyProvider
{
    private readonly KeyVaultClient _keyVaultClient;

    public AzureKeyVaultEncryptKeyProvider(KeyVaultClient keyVaultClient)
    {
        _keyVaultClient = keyVaultClient;
    }

    public async Task<string?> GetEncryptKeyAsync(string appKey, CancellationToken cancellationToken = default)
    {
        return await _keyVaultClient.GetSecretAsync($"feishu-{appKey}-encrypt-key", cancellationToken);
    }

    public async Task<string?> GetVerificationTokenAsync(string appKey, CancellationToken cancellationToken = default)
    {
        return await _keyVaultClient.GetSecretAsync($"feishu-{appKey}-verification-token", cancellationToken);
    }
}
```

### 自定义组合验证器

完全替换默认的验证逻辑：

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .UseCompositeValidator<MyCustomCompositeValidator>()
    .AddHandler<MessageEventHandler>()
    .Build();
```

## 应用级配置继承

多应用模式下，`FeishuAppWebhookOptions` 支持继承全局配置：

| 配置项                        | 继承规则                                       |
| ----------------------------- | ---------------------------------------------- |
| `TimestampToleranceSeconds`   | 设置为 -1 或 0 时继承全局配置，正整数使用应用级配置 |
| `EventHandlingTimeoutMs`      | 设置为 -1 或 0 时继承全局配置，正整数使用应用级配置 |
| `EnableExceptionHandling`     | 设置为 null 时继承全局配置，否则使用应用级配置     |
| `EnablePerformanceMonitoring` | 设置为 null 时继承全局配置，否则使用应用级配置     |
| `EnforceHeaderSignatureValidation` | 不继承，直接使用应用级配置（默认 false）     |
| `EnableBodySignatureValidation`    | 不继承，直接使用应用级配置（默认 true）      |

示例：

```json
{
  "FeishuWebhook": {
    "EventHandlingTimeoutMs": 30000,
    "TimestampToleranceSeconds": 30,
    "Apps": {
      "app1": {
        "EventHandlingTimeoutMs": 60000,
        "TimestampToleranceSeconds": -1
      },
      "app2": {
        "EventHandlingTimeoutMs": -1,
        "TimestampToleranceSeconds": 60
      }
    }
  }
}
```

上述配置中：
- `app1`：事件处理超时 60 秒（应用级），时间戳容差 30 秒（继承全局）
- `app2`：事件处理超时 30 秒（继承全局），时间戳容差 60 秒（应用级）

## 安全审计

内置 `ISecurityAuditService` 安全审计服务，记录安全验证事件：

```csharp
public class MySecurityService
{
    private readonly ISecurityAuditService _auditService;

    public MySecurityService(ISecurityAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task OnSuspiciousRequestAsync(string clientIp, string requestPath)
    {
        await _auditService.LogSecurityFailureAsync(
            SecurityEventType.SignatureValidation,
            clientIp,
            requestPath,
            "签名验证失败，疑似伪造请求");
    }
}
```

支持的安全事件类型：`SignatureValidation`、`TimestampValidation`、`IpValidation`、`SubscriptionValidation`、`InvalidContentType`、`RateLimitExceeded`、`RequestSizeLimit`、`ThreatDetection`、`Other`。

## 监控和诊断

### 性能监控

性能监控通过拦截器机制实现,支持 OpenTelemetry 和自定义指标收集：

```csharp
// 添加遥测拦截器（OpenTelemetry 集成）
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddInterceptor<TelemetryEventInterceptor>()
    .AddHandler<MessageEventHandler>()
    .Build();

// 或使用自定义性能监控拦截器
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddInterceptor<PerformanceMonitoringInterceptor>()
    .AddHandler<MessageEventHandler>()
    .Build();
```

### 健康检查

内置健康检查支持，默认启用，可监控 Webhook 服务的运行状态：

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 健康检查默认启用，也可以显式调用
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .EnableHealthChecks()    // 启用健康检查（默认已启用）
    .AddHandler<MessageEventHandler>()
    .Build();

// 如需禁用健康检查
// builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
//     .DisableHealthChecks()
//     .AddHandler<MessageEventHandler>()
//     .Build();

var app = builder.Build();

// 添加健康检查端点
app.MapHealthChecks("/health");

app.UseFeishuWebhook();
app.Run();
```

健康检查返回的数据包括：配置有效性、最大并发数、超时时间、当前可用并发槽位等。

### 日志记录

本库使用标准的 .NET 日志记录框架，可以灵活配置日志级别：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Mud.Feishu.Webhook": "Debug",
      "Mud.Feishu.Webhook.Services": "Debug",
      "Mud.Feishu.Webhook.Middleware": "Information",
      "Mud.Feishu.Abstractions": "Information"
    }
  }
}
```

### 诊断端点（Demo 示例）

Demo 项目提供了诊断端点，可以查看已注册的事件处理器：

```csharp
// 在 Demo 项目中使用
app.MapDiagnostics();  // 注册诊断端点

// 访问 /diagnostics/handlers 查看所有已注册的处理器
```

## 最佳实践

### 1. 错误处理

在事件处理器中妥善处理异常，避免影响其他事件的处理：

```csharp
public class RobustEventHandler : IFeishuEventHandler
{
    private readonly ILogger<RobustEventHandler> _logger;

    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始处理事件: {EventId}", eventData.EventId);

            // 你的业务逻辑
            await ProcessBusinessLogicAsync(eventData, cancellationToken);

            _logger.LogInformation("事件处理完成: {EventId}", eventData.EventId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("事件处理被取消: {EventId}", eventData.EventId);
            throw; // 超时取消应该抛出
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理事件时发生错误: {EventId}", eventData.EventId);
            // 不要重新抛出异常，避免影响其他处理器
            // 可以选择记录到失败队列或告警系统
        }
    }

    private async Task ProcessBusinessLogicAsync(EventData eventData, CancellationToken cancellationToken)
    {
        // 实际业务逻辑
        await Task.CompletedTask;
    }
}
```

### 2. 异步处理和取消支持

正确使用异步编程和取消令牌：

```csharp
public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
{
    // ✅ 正确：使用异步 API 并传递取消令牌
    await ProcessMessageAsync(eventData, cancellationToken);
    await SaveToDatabaseAsync(eventData, cancellationToken);

    // ❌ 错误：不要使用阻塞调用
    // var result = ProcessMessageAsync(eventData).Result;
    // ProcessMessageAsync(eventData).Wait();

    // ✅ 正确：尊重取消令牌
    cancellationToken.ThrowIfCancellationRequested();
}
```

### 3. 依赖注入

合理使用依赖注入，确保服务生命周期正确：

```csharp
public class MessageEventHandler : IFeishuEventHandler
{
    private readonly ILogger<MessageEventHandler> _logger;
    private readonly IMessageService _messageService;  // Scoped 服务
    private readonly IConfiguration _configuration;    // Singleton 服务

    public MessageEventHandler(
        ILogger<MessageEventHandler> logger,
        IMessageService messageService,
        IConfiguration configuration)
    {
        _logger = logger;
        _messageService = messageService;
        _configuration = configuration;
    }

    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        // 使用注入的服务
        await _messageService.ProcessAsync(eventData, cancellationToken);
    }
}
```

### 4. 使用基类处理器（推荐）

继承基类处理器可以获得自动去重和类型安全：

```csharp
using Mud.Feishu.Abstractions.EventHandlers;

// 继承基类处理器，自动处理去重和类型转换
public class MyDepartmentHandler : DepartmentCreatedEventHandler
{
    public MyDepartmentHandler(
        IFeishuEventDeduplicator deduplicator,
        ILogger<MyDepartmentHandler> logger)
        : base(deduplicator, logger)
    {
    }

    // 只需要实现业务逻辑
    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        DepartmentCreatedResult eventEntity,
        CancellationToken cancellationToken = default)
    {
        // eventEntity 已经是强类型的实体对象
        _logger.LogInformation("处理部门: {Name}", eventEntity.Name);
    }
}
```

### 5. 配置验证

启动时验证配置，尽早发现问题：

```csharp
// 配置会在 Build() 时自动验证
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();  // 这里会验证配置

// 如果配置无效，会抛出 InvalidOperationException
```

### 6. 长时间运行的任务

对于耗时较长的任务，启用后台处理模式：

```csharp
// appsettings.json
{
  "FeishuWebhook": {
    "EnableBackgroundProcessing": true,  // 立即返回成功，后台处理
    "EventHandlingTimeoutMs": 60000      // 增加超时时间
  }
}
```

### 7. 测试和调试

Demo 项目提供了测试端点，可用于调试：

```csharp
// 在 Demo 项目中
app.MapTestEndpoints();        // 测试端点
app.MapDiagnostics();          // 诊断端点

// 可以使用以下端点：
// POST /test/capture - 捕获原始请求
// GET /test/captured - 查看捕获的请求
// GET /diagnostics/handlers - 查看已注册的处理器
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
   - 参考 `IFeishuNonceDistributedDeduplicator` 接口自定义 Redis 实现

6. **超时处理**
   - 检查 `EventHandlingTimeoutMs` 配置是否合理
   - 确保事件处理逻辑支持取消令牌

7. **请求频率限制问题**
   - 检查 `RateLimit.EnableRateLimit` 配置
   - 确认客户端 IP 是否在白名单中
   - 调整 `MaxRequestsPerWindow` 和 `WindowSizeSeconds` 参数

8. **多应用配置问题**
   - 检查 `Apps` 配置是否正确
   - 确认各应用的 AppKey、VerificationToken 和 EncryptKey 配置
   - 验证应用路由是否正确（`/feishu/{AppKey}`）

### 调试技巧

```csharp
// 启用详细日志
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// 启用请求日志记录和性能监控
builder.Services.CreateFeishuWebhookServiceBuilder(options =>
{
    options.EnableRequestLogging = true;
    options.EnablePerformanceMonitoring = true;
    options.RateLimit.EnableRateLimit = true; // 启用限流调试
}).AddHandler<MessageEventHandler>()
    .Build();
```

## 完整示例

### 基础示例

完整的 Program.cs 示例：

```csharp
using Mud.Feishu.Webhook.Extensions;
using Mud.Feishu.Webhook.Demo.Handlers;
using Mud.Feishu.Webhook.Demo.Services;

var builder = WebApplication.CreateBuilder(args);

// 注册自定义服务
builder.Services.AddSingleton<DemoEventService>();

// 注册飞书Webhook服务
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration, "FeishuWebhook")
    .AddHandler<DemoDepartmentEventHandler>()
    .AddHandler<DemoDepartmentDeleteEventHandler>()
    .AddHandler<DemoDepartmentUpdateEventHandler>()
    .Build();

var app = builder.Build();

// 添加飞书Webhook中间件
app.UseFeishuWebhook();

app.Run();
```

### 高级示例

包含健康检查、性能监控和自定义端点：

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 配置日志
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// 注册飞书Webhook服务（高级配置）
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .EnableHealthChecks()    // 启用健康检查
    .AddHandler<MessageReceiveEventHandler>()
    .AddHandler<DepartmentCreatedEventHandler>()
    .Build();

var app = builder.Build();

// 健康检查端点
app.MapHealthChecks("/health");

// 飞书Webhook中间件
app.UseFeishuWebhook();

app.Run();
```

### Demo 项目完整示例

Demo 项目提供了完整的测试和诊断功能：

```csharp
using Mud.Feishu.Webhook.Demo.Handlers;
using Mud.Feishu.Webhook.Demo.Services;
using Mud.Feishu.Webhook.Extensions;
using Mud.Feishu.Webhook.Demo;

var builder = WebApplication.CreateBuilder(args);

// 注册演示服务
builder.Services.AddSingleton<DemoEventService>();

// 注册飞书Webhook服务
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration, "FeishuWebhook")
    .AddHandler<DemoDepartmentEventHandler>()
    .AddHandler<DemoDepartmentDeleteEventHandler>()
    .AddHandler<DemoDepartmentUpdateEventHandler>()
    .Build();

var app = builder.Build();

// 添加诊断端点（仅开发环境）
if (app.Environment.IsDevelopment())
{
    app.MapDiagnostics();      // GET /diagnostics/handlers
    app.MapTestEndpoints();    // POST /test/capture 等
}

// 添加飞书Webhook中间件
app.UseFeishuWebhook();

app.Run();
```

## 快速参考

### 最常用的代码模式

```csharp
// ✅ 推荐：从配置文件读取（多应用配置）
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<YourEventHandler>()
    .Build();

// ✅ 代码配置（需在 Apps 中配置每个应用的 Token 和 Key）
builder.Services.CreateFeishuWebhookServiceBuilder(options => {
    options.GlobalRoutePrefix = "feishu";
})
.AddHandler<YourEventHandler>()
.Build();

// ✅ 高级配置
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .EnableHealthChecks()
    .AddHandler<Handler1>()
    .AddHandler<Handler2>()
    .Build();

// ✅ 多应用独立处理器和拦截器
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<App1Handler>("app1")
    .AddInterceptor<App1LoggingInterceptor>("app1")
    .AddHandler<App2Handler>("app2")
    .Build();

// ✅ 自定义验证器和密钥提供程序
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .UseSignatureValidator<MySignatureValidator>()
    .UseEncryptKeyProvider<AzureKeyVaultEncryptKeyProvider>()
    .AddHandler<YourEventHandler>()
    .Build();
```

### 常用配置项速查

| 配置项                        | 默认值           | 说明                     |
| ----------------------------- | ---------------- | ------------------------ |
| `GlobalRoutePrefix`           | `"feishu"`       | 全局路由前缀             |
| `VerificationToken`           | -                | 验证令牌（必填）         |
| `EncryptKey`                  | -                | 加密密钥（32字节）       |
| `MaxConcurrentEvents`         | `10`             | 最大并发事件数，支持热更新 |
| `EventHandlingTimeoutMs`      | `30000`          | 事件处理超时（毫秒）     |
| `EnableBackgroundProcessing`  | `false`          | 后台处理模式             |
| `EnablePerformanceMonitoring` | `false`          | 性能监控                 |
| `EnforceHeaderSignatureValidation` | `true`     | 强制签名验证（生产环境必须启用） |
| `TimestampToleranceSeconds`   | `30`             | 时间戳容错范围（秒）     |

---

## 多应用模式

Mud.Feishu.Webhook 支持多应用模式，允许你为不同的飞书应用配置独立的路由、处理器和配置。

### 配置文件

```json
{
  "FeishuWebhook": {
    "GlobalRoutePrefix": "feishu",
    "Apps": {
      "app1": {
        "VerificationToken": "app1_verification_token",
        "EncryptKey": "app1_encrypt_key_32_bytes_long"
      },
      "app2": {
        "VerificationToken": "app2_verification_token",
        "EncryptKey": "app2_encrypt_key_32_bytes_long"
      }
    }
  }
}
```

### 注册代码

```csharp
// 为不同应用注册独立的处理器和拦截器
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    // App1 处理器和拦截器
    .AddHandler<App1DepartmentEventHandler>("app1")
    .AddHandler<App1MessageEventHandler>("app1")
    .AddInterceptor<App1LoggingInterceptor>("app1")

    // App2 处理器和拦截器
    .AddHandler<App2DepartmentEventHandler>("app2")
    .AddHandler<App2MessageEventHandler>("app2")
    .AddInterceptor<App2AuditInterceptor>("app2")

    .Build();

var app = builder.Build();

// 使用多应用中间件（自动处理路由）
app.UseFeishuWebhook();

app.Run();
```

> 💡 **说明**：通过 `AddHandler<T>(appKey)` 和 `AddInterceptor<T>(appKey)` 注册的处理器和拦截器仅对指定应用生效，不会注册到全局集合，避免跨应用泄漏。

### 路由映射

系统会自动将路由映射到对应的应用：

- `/feishu/app1` → App1 的处理器
- `/feishu/app2` → App2 的处理器

### 应用隔离

每个应用完全隔离：

- ✅ **配置隔离**：每个应用独立的 `EncryptKey` 和 `VerificationToken`
- ✅ **处理器隔离**：每个应用只能调用自己的处理器
- ✅ **拦截器隔离**：每个应用只能调用自己的拦截器
- ✅ **路由隔离**：不同的路由前缀，互不干扰
- ✅ **安全隔离**：每个应用独立的安全验证
- ✅ **限流隔离**：基于 `(AppKey, IP)` 维度的独立限流

详细的多应用文档请参阅：[Readme.MultiApp.md](Readme.MultiApp.md)

---

**🚀 立即开始使用飞书Webhook，构建稳定可靠的事件处理系统！**
