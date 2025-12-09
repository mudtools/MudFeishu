# MudFeishu

现代化的 .NET 飞书 API 集成库，提供完整的 HTTP API、WebSocket 实时事件订阅和 Webbook 事件处理解决方案。

## 📦 项目概览

| 组件 | 描述 | NuGet |
|-----|------|-------|
| **Mud.Feishu.Abstractions** | 事件订阅抽象层，提供策略模式和工厂模式的事件处理架构 | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Abstractions.svg)](https://www.nuget.org/packages/Mud.Feishu.Abstractions/) |
| **Mud.Feishu** | 核心 HTTP API 客户端库，支持组织架构、消息、群聊等完整飞书功能 | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.svg)](https://www.nuget.org/packages/Mud.Feishu/) |
| **Mud.Feishu.WebSocket** | 飞书 WebSocket 客户端，支持实时事件订阅和自动重连 | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.WebSocket.svg)](https://www.nuget.org/packages/Mud.Feishu.WebSocket/) |
| **Mud.Feishu.Webbook** | 飞书 Webbook 事件处理组件，支持 HTTP 回调事件接收和处理 | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Webbook.svg)](https://www.nuget.org/packages/Mud.Feishu.Webbook/) |

## ✨ 核心特性

| 特性类别 | Mud.Feishu.Abstractions | Mud.Feishu (HTTP API) | Mud.Feishu.WebSocket | Mud.Feishu.Webbook |
|---------|----------------------|----------------------|-------------------|-------------------|
| **🎯 核心功能** | 事件订阅抽象层和策略模式 | HTTP API 客户端，RESTful 调用 | WebSocket 客户端，实时事件订阅 | Webbook HTTP 回调事件处理 |
| **🔧 设计模式** | 策略模式、工厂模式、抽象基类 | 特性驱动设计，自动生成 HTTP 客户端 | 建造者模式，可扩展事件处理器 | 中间件模式，自动事件路由 |
| **🛡️ 类型安全** | 强类型事件数据模型，默认基类 | 完整的数据模型，编译时类型检查 | 强类型事件消息，智能反序列化 | 类型安全的事件解密和验证 |
| **🔐 令牌管理** | - | 自动缓存刷新，多类型令牌支持 | 继承 HTTP API 的令牌管理能力 | 继承 HTTP API 的令牌管理能力 |
| **📦 服务注册** | - | 模块化注册，支持构造者模式 | 建造者模式配置，灵活的处理器管理 | 一行代码注册，自动中间件配置 |
| **🚀 性能优化** | 异步事件处理，并行执行 | 连接池管理，智能重试机制 | 异步消息处理，内置消息队列 | 异步事件处理，并发控制 |
| **🛠️ 企业级特性** | 可扩展架构，异常处理基类 | 统一异常处理，性能监控 | 自动重连，心跳检测，状态监控 | 安全验证，加密解密，健康检查 |

## 📊 主要功能

### 🏛️ Mud.Feishu.Abstractions - 事件处理抽象层

#### 🎯 事件处理架构
- **策略模式** - 可扩展的事件处理器架构，支持多种事件类型处理
- **工厂模式** - 内置事件处理器工厂，支持动态注册和发现
- **抽象基类** - 提供 `DefaultFeishuEventHandler<T>` 等基类简化开发
- **类型安全** - 强类型事件数据模型，编译时类型检查
- **异步处理** - 完全异步的事件处理，支持并行执行
- **可扩展性** - 易于扩展新的事件类型和处理器

#### 📋 丰富事件类型支持
- **组织管理事件** - 用户创建/更新/删除、部门变更等
- **消息事件** - 消息接收、发送状态、阅读状态等
- **应用事件** - 应用授权、权限变更等应用级事件
- **自定义事件** - 支持企业自定义事件类型

### 🌐 Mud.Feishu - HTTP API 客户端功能

#### 🔐 认证与令牌管理
- **多类型令牌支持** - 支持应用令牌、租户令牌、用户令牌三种类型
- **自动令牌缓存** - 内置令牌缓存机制，减少API调用次数
- **智能令牌刷新** - 令牌即将过期时自动刷新，确保服务连续性
- **多租户支持** - 支持多租户场景下的令牌隔离和管理
- **OAuth流程** - 完整的OAuth授权流程支持，安全获取用户令牌

#### 🏢 组织架构管理
- **用户管理** - V1/V3 双版本用户API，创建、更新、查询、删除、批量操作
- **部门管理** - V1/V3 部门树形结构维护，多层级部门管理
- **员工管理** - V1 员工档案和详细信息管理
- **职级管理** - 企业职级体系维护，职级增删改查
- **职位序列** - 职业发展路径管理，职位序列定义
- **角色权限** - 企业权限角色体系，角色成员管理
- **用户组管理** - 用户组成员管理，灵活的用户分组
- **工作城市管理** - 多城市工作地点维护

#### 📱 消息服务
- **消息发送** - V1 消息API，支持文本、图片、文件、卡片等丰富类型
- **批量消息** - V1 批量消息API，向多用户/部门批量发送
- **群聊管理** - 群聊创建、成员管理、群聊信息维护
- **消息互动** - 消息表情回复、引用回复等互动功能
- **任务管理** - 任务创建、更新、状态管理等协作功能

#### 🛠️ 企业级特性
- **统一异常处理** - 完善的异常处理机制，统一错误响应格式
- **智能重试机制** - 网络故障和临时错误的自动重试，提高稳定性
- **高性能缓存** - 解决缓存击穿和竞态条件，支持令牌自动刷新
- **连接池管理** - HTTP连接池复用，提升API调用效率
- **异步编程支持** - 全面的async/await支持，非阻塞I/O操作
- **详细日志记录** - 结构化日志，便于监控和问题排查

### 🔄 Mud.Feishu.WebSocket - 实时事件订阅功能

#### 🤖 事件处理架构
- **策略模式设计** - 可扩展的事件处理器架构，支持自定义业务逻辑
- **多处理器支持** - 可注册多个事件处理器并行处理不同类型事件
- **单处理器模式** - 适合单一功能的简单事件处理场景
- **自定义处理器** - 完全可扩展的业务定制，支持复杂场景
- **事件重放** - 支持事件的重放和恢复机制，确保数据一致性

#### 🫀 连接管理
- **WebSocket连接管理** - 持久连接维护和监控
- **自动重连机制** - 连接断开时自动重新连接，确保服务连续性
- **心跳监控** - 定期心跳检测，确保连接活跃状态
- **连接负载均衡** - 多连接实例的负载分发，提升处理能力
- **优雅关闭** - 支持连接的优雅关闭和资源清理

#### 📈 监控与运维
- **连接状态监控** - 实时连接数量、状态监控
- **事件处理统计** - 事件接收数量、处理时间统计
- **性能指标收集** - 消息处理吞吐量、延迟监控
- **健康检查** - 服务健康状态实时检查
- **告警支持** - 异常情况自动告警通知
- **详细审计日志** - 完整的事件处理审计记录

### 🌐 Mud.Feishu.Webbook - HTTP 回调事件处理功能

#### 🔒 安全验证与解密
- **事件订阅验证** - 支持飞书 URL 验证流程
- **请求签名验证** - 验证飞书事件请求的签名真实性
- **时间戳验证** - 防止重放攻击的时间戳检查
- **AES-256-CBC解密** - 内置解密功能，自动处理加密事件
- **来源IP验证** - 可配置的IP白名单验证

#### 🚀 事件处理架构
- **中间件模式** - 无缝集成到 ASP.NET Core 管道
- **自动事件路由** - 根据事件类型自动分发到对应处理器
- **多种使用模式** - 支持中间件模式、控制器模式和混合模式
- **异步处理** - 完全异步的事件处理机制
- **并发控制** - 可配置的并发事件处理数量限制

#### 📊 监控与运维
- **性能监控** - 可选的性能指标收集和监控
- **健康检查** - 内置健康检查端点
- **异常处理** - 完善的异常处理和日志记录
- **请求日志** - 详细的请求处理日志记录


## 🚀 快速开始

### 安装

```bash
# 事件处理抽象层
dotnet add package Mud.Feishu.Abstractions

# HTTP API 客户端
dotnet add package Mud.Feishu

# WebSocket 实时事件订阅
dotnet add package Mud.Feishu.WebSocket

# Webbook HTTP 回调事件处理
dotnet add package Mud.Feishu.Webbook
```

### 基础配置

```csharp
using Mud.Feishu;
using Mud.Feishu.WebSocket;
using Mud.Feishu.Webbook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 注册 HTTP API 服务（方式一：传统方法）
builder.Services.AddFeishuApiService(builder.Configuration);

// 注册 HTTP API 服务（方式二：构造者模式）
// builder.Services.AddFeishuServices()
//     .ConfigureFrom(builder.Configuration)
//     .AddOrganizationApi()
//     .AddMessageApi()
//     .Build();

// 注册 WebSocket 事件订阅服务
builder.Services.AddFeishuWebSocketBuilder()
    .ConfigureFrom(builder.Configuration)
    .UseMultiHandler()
    .AddHandler<MessageEventHandler>()
    .Build();

// 注册 Webbook HTTP 回调事件服务
builder.Services.AddFeishuWebbook(builder.Configuration)
    .AddHandler<UserCreatedEventHandler>()
    .AddHandler<MessageReceiveEventHandler>()
    .Build();

var app = builder.Build();

// 添加 Webbook 中间件
app.UseFeishuWebbook();

app.Run();
```

### 配置文件

```json
{
    "Feishu": {
        "AppId": "demo_app_id",
        "AppSecret": "demo_app_secret",
        "BaseUrl": "https://open.feishu.cn",
        "WebSocket": {
            "AutoReconnect": true,
            "MaxReconnectAttempts": 5,
            "ReconnectDelayMs": 5000,
            "HeartbeatIntervalMs": 30000,
            "ConnectionTimeoutMs": 10000,
            "ReceiveBufferSize": 4096,
            "EnableLogging": true,
            "EnableMessageQueue": true,
            "MessageQueueCapacity": 1000,
            "ParallelMultiHandlers": true
        },
        "Webbook": {
            "RoutePrefix": "feishu/webbook",
            "AutoRegisterEndpoint": true,
            "VerificationToken": "your_verification_token",
            "EncryptKey": "your_encrypt_key",
            "EnableRequestLogging": true,
            "EnableExceptionHandling": true,
            "EventHandlingTimeoutMs": 30000,
            "MaxConcurrentEvents": 10,
            "EnablePerformanceMonitoring": false,
            "AllowedHttpMethods": ["POST"],
            "MaxRequestBodySize": 10485760,
            "ValidateSourceIP": false,
            "AllowedSourceIPs": []
        }
    }
}
```

## 🎯 使用场景

### 🚀 快速上手示例

#### HTTP API 调用示例

```csharp
// 用户管理 Controller 示例
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IFeishuTenantV3User _userApi;
    private readonly IFeishuTenantV3Departments _deptApi;
    
    public UserController(
        IFeishuTenantV3User userApi,
        IFeishuTenantV3Departments deptApi)
    {
        _userApi = userApi;
        _deptApi = deptApi;
    }
    
    // 创建新用户
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await _userApi.CreateUserAsync(request);
        return result.Code == 0 ? Ok(result.Data) : BadRequest(result.Msg);
    }
    
    // 获取部门下的所有用户
    [HttpGet("departments/{departmentId}/users")]
    public async Task<IActionResult> GetDepartmentUsers(string departmentId)
    {
        var result = await _deptApi.GetUserByDepartmentIdAsync(departmentId);
        return Ok(result.Data);
    }
    
    // 批量获取用户信息
    [HttpPost("users/batch")]
    public async Task<IActionResult> GetUsersBatch([FromBody] string[] userIds)
    {
        var result = await _userApi.GetUserByIdsAsync(userIds);
        return Ok(result.Data);
    }
}
```

#### 消息发送示例

```csharp
public class NotificationService
{
    private readonly IFeishuTenantV1Message _messageApi;
    private readonly IFeishuTenantV1BatchMessage _batchMessageApi;
    
    public NotificationService(
        IFeishuTenantV1Message messageApi,
        IFeishuTenantV1BatchMessage batchMessageApi)
    {
        _messageApi = messageApi;
        _batchMessageApi = batchMessageApi;
    }
    
    // 发送文本消息给用户
    public async Task<string> SendTextMessageAsync(string userId, string content)
    {
        var request = new TextMessageRequest
        {
            ReceiveIdType = "user_id",
            ReceiveId = userId,
            Content = new TextContent { Text = content }
        };
        
        var result = await _messageApi.SendTextMessageAsync(request);
        return result.Code == 0 ? result.Data?.MessageId : null;
    }
    
    // 批量发送系统通知
    public async Task<string> SendSystemNotificationAsync(string[] departmentIds, string title, string content)
    {
        var request = new BatchSenderTextMessageRequest
        {
            DeptIds = departmentIds,
            Content = new TextContent 
            { 
                Text = $"📢 {title}\n\n{content}"
            }
        };
        
        var result = await _batchMessageApi.BatchSendTextMessageAsync(request);
        return result.Code == 0 ? result.Data?.MessageId : null;
    }
}
```

#### WebSocket 事件处理示例

```csharp
// 消息事件处理器
public class MessageEventHandler : IFeishuWebSocketEventHandler
{
    private readonly ILogger<MessageEventHandler> _logger;
    private readonly IFeishuTenantV1Message _messageApi;
    
    public MessageEventHandler(
        ILogger<MessageEventHandler> logger,
        IFeishuTenantV1Message messageApi)
    {
        _logger = logger;
        _messageApi = messageApi;
    }
    
    public async Task HandleAsync(FeishuWebSocketMessage message)
    {
        try
        {
            switch (message.Type)
            {
                case "message.receive_v1":
                    await HandleMessageReceivedAsync(message);
                    break;
                    
                case "im.message.message_read_v1":
                    await HandleMessageReadAsync(message);
                    break;
                    
                default:
                    _logger.LogInformation($"收到未处理的消息类型: {message.Type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"处理消息事件失败: {message.Type}");
        }
    }
    
    private async Task HandleMessageReceivedAsync(FeishuWebSocketMessage message)
    {
        var data = JsonSerializer.Deserialize<MessageReceiveEvent>(message.Data.ToString());
        _logger.LogInformation($"收到消息 - 发送者: {data.Sender.Id}, 内容: {data.Message.Content}");
        
        // 可以在这里添加业务逻辑，比如自动回复、消息转发等
        if (data.Message.Content.Contains("帮助"))
        {
            await SendHelpMessageAsync(data.Sender.Id);
        }
    }
    
    private async Task HandleMessageReadAsync(FeishuWebSocketMessage message)
    {
        var data = JsonSerializer.Deserialize<MessageReadEvent>(message.Data.ToString());
        _logger.LogInformation($"消息已读 - 用户: {data.Reader.Id}, 消息ID: {data.MessageId}");
        
        // 更新数据库中的消息阅读状态等
        await UpdateMessageReadStatusAsync(data.MessageId, data.Reader.Id);
    }
}

// 用户事件处理器
public class UserEventHandler : IFeishuWebSocketEventHandler
{
    private readonly ILogger<UserEventHandler> _logger;
    private readonly IUserSyncService _syncService;
    
    public UserEventHandler(
        ILogger<UserEventHandler> logger,
        IUserSyncService syncService)
    {
        _logger = logger;
        _syncService = syncService;
    }
    
    public async Task HandleAsync(FeishuWebSocketMessage message)
    {
        switch (message.Type)
        {
            case "contact.user.created_v3":
                await HandleUserCreatedAsync(message);
                break;
                
            case "contact.user.updated_v3":
                await HandleUserUpdatedAsync(message);
                break;
                
            case "contact.user.deleted_v3":
                await HandleUserDeletedAsync(message);
                break;
        }
    }
    
    private async Task HandleUserCreatedAsync(FeishuWebSocketMessage message)
    {
        var userEvent = JsonSerializer.Deserialize<UserCreatedEvent>(message.Data.ToString());
        _logger.LogInformation($"新用户创建: {userEvent.User.Name} ({userEvent.User.UserId})");
        
        // 同步用户到本地数据库
        await _syncService.SyncUserToDatabaseAsync(userEvent.User);
        
        // 发送欢迎消息
        await SendWelcomeMessageAsync(userEvent.User.UserId);
    }
    
    private async Task HandleUserUpdatedAsync(FeishuWebSocketMessage message)
    {
        var userEvent = JsonSerializer.Deserialize<UserUpdatedEvent>(message.Data.ToString());
        _logger.LogInformation($"用户信息更新: {userEvent.User.Name}");
        
        // 更新本地数据库中的用户信息
        await _syncService.UpdateUserInDatabaseAsync(userEvent.User);
    }
    
    private async Task HandleUserDeletedAsync(FeishuWebSocketMessage message)
    {
        var userEvent = JsonSerializer.Deserialize<UserDeletedEvent>(message.Data.ToString());
        _logger.LogInformation($"用户已删除: {userEvent.UserId}");
        
        // 从本地数据库中删除用户
        await _syncService.DeleteUserFromDatabaseAsync(userEvent.UserId);
    }
}
```

#### Webbook 事件处理示例

```csharp
// 用户创建事件处理器
public class UserCreatedEventHandler : IFeishuWebbookEventHandler
{
    private readonly ILogger<UserCreatedEventHandler> _logger;
    private readonly IUserSyncService _syncService;
    
    public UserCreatedEventHandler(
        ILogger<UserCreatedEventHandler> logger,
        IUserSyncService syncService)
    {
        _logger = logger;
        _syncService = syncService;
    }
    
    public async Task<bool> CanHandleAsync(string eventType)
    {
        return eventType == "contact.user.created_v3";
    }
    
    public async Task HandleAsync(FeishuWebbookRequest request)
    {
        try
        {
            var eventData = await DecryptEventAsync(request);
            var userEvent = JsonSerializer.Deserialize<UserCreatedEvent>(eventData);
            
            _logger.LogInformation($"新用户创建: {userEvent.User.Name} ({userEvent.User.UserId})");
            
            // 同步用户到本地数据库
            await _syncService.SyncUserToDatabaseAsync(userEvent.User);
            
            // 发送欢迎消息
            await SendWelcomeMessageAsync(userEvent.User.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理用户创建事件失败");
            throw;
        }
    }
}

// 消息接收事件处理器
public class MessageReceiveEventHandler : IFeishuWebbookEventHandler
{
    private readonly ILogger<MessageReceiveEventHandler> _logger;
    private readonly IFeishuTenantV1Message _messageApi;
    
    public MessageReceiveEventHandler(
        ILogger<MessageReceiveEventHandler> logger,
        IFeishuTenantV1Message messageApi)
    {
        _logger = logger;
        _messageApi = messageApi;
    }
    
    public async Task<bool> CanHandleAsync(string eventType)
    {
        return eventType == "im.message.receive_v1";
    }
    
    public async Task HandleAsync(FeishuWebbookRequest request)
    {
        try
        {
            var eventData = await DecryptEventAsync(request);
            var messageEvent = JsonSerializer.Deserialize<MessageReceiveEvent>(eventData);
            
            _logger.LogInformation($"收到消息 - 发送者: {messageEvent.Sender.Id}, 内容: {messageEvent.Message.Content}");
            
            // 智能回复逻辑
            if (messageEvent.Message.Content.Contains("帮助"))
            {
                await SendHelpMessageAsync(messageEvent.Sender.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理消息接收事件失败");
            throw;
        }
    }
}
```

## 📖 详细文档

- [Mud.Feishu.Abstractions 详细文档](./Mud.Feishu.Abstractions/README.md) - 事件处理抽象层使用指南
- [Mud.Feishu 详细文档](./Mud.Feishu/README.md) - HTTP API 完整使用指南
- [Mud.Feishu.WebSocket 详细文档](./Mud.Feishu.WebSocket/Readme.md) - WebSocket 实时事件订阅指南
- [Mud.Feishu.Webbook 详细文档](./Mud.Feishu.Webbook/README.md) - Webbook HTTP 回调事件处理指南

## 🛠️ 技术栈

### 📚 核心技术依赖
- **.NET 6.0+** - 全面支持 .NET 6.0/7.0/8.0/9.0/10.0，使用最新的 C# 语言特性
- **ASP.NET Core** - 原生依赖注入、配置系统、日志框架，完美集成现代 .NET 应用
- **Mud.ServiceCodeGenerator v1.3.3** - 特性驱动的 HTTP 客户端代码生成器，自动生成类型安全的 API 客户端

### 🔧 系统组件
- **System.Text.Json** - 高性能 JSON 序列化/反序列化，内置优化和流式处理
- **Microsoft.Extensions.Http** - HTTP 客户端工厂，自动管理连接池和生命周期
- **Microsoft.Extensions.Hosting.Abstractions** - 托管服务抽象，支持后台服务和生命周期管理
- **Microsoft.Extensions.Configuration.Binder** - 强类型配置绑定，简化配置管理

### 🏗️ 架构设计模式
- **依赖注入模式** - 基于接口的设计，支持单元测试和模拟
- **建造者模式** - 灵活的服务配置和注册方式
- **策略模式** - WebSocket 事件处理的可扩展架构
- **适配器模式** - 统一的 API 响应处理和错误管理

### 📊 性能特性
- **异步编程** - 全面的 async/await 支持，非阻塞 I/O 操作
- **连接池管理** - HTTP 连接复用，减少连接建立开销
- **智能缓存** - 令牌缓存机制，减少 API 调用频率
- **并发安全** - 线程安全的消息处理和状态管理

### 🔒 企业级特性
- **自动重试机制** - 网络故障和临时错误的智能重试
- **熔断器模式** - 防止级联故障，保护系统稳定性
- **详细的日志记录** - 结构化日志，便于监控和问题排查
- **健康检查** - 服务状态监控和自动恢复机制

## 📄 许可证

本项目遵循 [MIT 许可证](./LICENSE)，允许商业和非商业用途。

## 🔗 相关链接

### 📖 官方文档
- [飞书开放平台文档](https://open.feishu.cn/document/) - 飞书 API 官方文档和最佳实践
- [NuGet 包管理器](https://www.nuget.org/) - .NET 包管理官方平台

### 📦 NuGet 包
- [Mud.Feishu.Abstractions](https://www.nuget.org/packages/Mud.Feishu.Abstractions/) - 事件处理抽象层
- [Mud.Feishu](https://www.nuget.org/packages/Mud.Feishu/) - 核心 HTTP API 客户端库
- [Mud.Feishu.WebSocket](https://www.nuget.org/packages/Mud.Feishu.WebSocket/) - WebSocket 实时事件订阅库
- [Mud.Feishu.Webbook](https://www.nuget.org/packages/Mud.Feishu.Webbook/) - Webbook HTTP 回调事件处理库

### 🛠️ 开发资源
- [项目仓库](https://gitee.com/mudtools/MudFeishu) - 源代码和开发文档
- [Mud.ServiceCodeGenerator](https://gitee.com/mudtools/mud-code-generator) - HTTP 客户端代码生成器
- [示例项目](./Mud.Feishu.Test) - 完整的使用示例和演示代码

### 🤝 社区支持
- [问题反馈](https://gitee.com/mudtools/MudFeishu/issues) - Bug 报告和功能请求
- [贡献指南](./CONTRIBUTING.md) - 如何参与项目贡献
- [更新日志](./CHANGELOG.md) - 版本更新记录和变更说明