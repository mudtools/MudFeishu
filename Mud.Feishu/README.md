# MudFeishu

<div align="center">

![NuGet](https://img.shields.io/nuget/v/Mud.Feishu)
![NuGet](https://img.shields.io/nuget/dt/Mud.Feishu)
![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%207.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-purple)
![License](https://img.shields.io/badge/license-MIT-blue)

**现代化 .NET 飞书 API 集成 SDK**

极简 API · 类型安全 · 企业级特性 · 事件驱动

[快速开始](#快速开始) · [文档](https://www.mudtools.cn/documents/guides/feishu/) · [示例代码](#示例项目) · [贡献指南](#贡献指南)

</div>

---

## 📖 简介

MudFeishu 是一套现代化的企业级 .NET 飞书 API 集成 SDK，提供完整的 HTTP API 调用、WebSocket 实时事件订阅和 Webhook 事件处理能力。SDK 采用策略模式和工厂模式设计，内置自动令牌管理、智能重试、高性能缓存等企业级特性。

### ✨ 核心特性

- **极简 API** - 一行代码完成服务注册
- **类型安全** - 完整的强类型数据模型，编译时检查
- **自动令牌管理** - 智能缓存和自动刷新机制
- **企业级稳定** - 统一异常处理、智能重试、高性能缓存
- **事件驱动架构** - WebSocket + Webhook 双模式支持
- **多框架支持** - .NET Standard 2.0 / .NET 6.0 / .NET 8.0 / .NET 10.0
- **分布式支持** - Redis 分布式去重，多实例部署
- **安全增强** - 签名验证、时间戳验证、IP 白名单

---

---

## 📦 模块组成

| 模块 | NuGet 包 | 功能定位 |
|------|---------|---------|
| **Mud.Feishu.Abstractions** | `Mud.Feishu.Abstractions` | 事件处理抽象层，提供统一的处理器接口和数据模型 |
| **Mud.Feishu** | `Mud.Feishu` | HTTP API 客户端核心，支持组织架构、消息、审批、任务等 |
| **Mud.Feishu.WebSocket** | `Mud.Feishu.WebSocket` | 实时事件订阅，支持自动重连、心跳检测、消息队列 |
| **Mud.Feishu.Webhook** | `Mud.Feishu.Webhook` | HTTP 回调事件处理，支持签名验证、加密解密 |
| **Mud.Feishu.Redis** | `Mud.Feishu.Redis` | 分布式去重扩展，支持多实例部署 |

---

## 🚀 快速开始

### 安装

```bash
# 核心包
dotnet add package Mud.Feishu --version 2.0.5

# 可选模块
dotnet add package Mud.Feishu.WebSocket --version 2.0.5
dotnet add package Mud.Feishu.Webhook --version 2.0.5
dotnet add package Mud.Feishu.Redis --version 2.0.5
```

### 配置依赖注入

#### 🎯 一键完整注册（推荐新手）

```csharp
using Mud.Feishu;

var builder = WebApplication.CreateBuilder(args);

// 一行代码注册所有飞书 API 服务（懒人模式）
builder.Services.AddFeishuServices(builder.Configuration);

var app = builder.Build();
```

#### 🔧 构造者模式（推荐高级用户）

```csharp
// 按需灵活注册服务（使用配置文件）
builder.Services.CreateFeishuServicesBuilder(builder.Configuration)
    .AddOrganizationApi()                  // 组织架构
    .AddMessageApi()                       // 消息服务
    .AddChatGroupApi()                    // 群组服务
    .AddApprovalApi()                     // 流程审批
    .AddTaskApi()                         // 任务管理
    .AddCardApi()                         // 卡片管理
    .Build();

// 按需灵活注册服务（使用代码配置）
builder.Services.CreateFeishuServicesBuilder(options =>
{
    options.AppId = "your_app_id";
    options.AppSecret = "your_app_secret";
    options.BaseUrl = "https://open.feishu.cn";
    options.TimeOut = 30;
    options.RetryCount = 3;
})
    .AddOrganizationApi()
    .AddMessageApi()
    .Build();
```

#### 📦 模块化注册

```csharp
// 仅注册需要的模块
builder.Services.AddFeishuServices(builder.Configuration, new[]
{
    FeishuModule.Organization,      // 组织架构
    FeishuModule.Message,          // 消息服务
    FeishuModule.ChatGroup         // 群组服务
});
```

### 配置文件

#### appsettings.json

```json
{
  "Feishu": {
    "AppId": "your_feishu_app_id",
    "AppSecret": "your_feishu_app_secret",
    "BaseUrl": "https://open.feishu.cn",
    "TimeOut": 30,
    "RetryCount": 3,
    "EnableLogging": true
  },
  "Feishu:Redis": {
    "ServerAddress": "localhost:6379",
    "Password": "",
    "DefaultDatabase": 0,
    "EventCacheExpiration": "08:00:00"
  }
}
```

### Controller 注入示例

```csharp
using Microsoft.AspNetCore.Mvc;
using Mud.Feishu;

[ApiController]
[Route("api/[controller]")]
public class FeishuController : ControllerBase
{
    private readonly IFeishuTenantV3User _userApi;
    private readonly IFeishuTenantV3Departments _departmentsApi;
    private readonly IFeishuTenantV1Message _messageApi;

    public FeishuController(
        IFeishuTenantV3User userApi,
        IFeishuTenantV3Departments departmentsApi,
        IFeishuTenantV1Message messageApi)
    {
        _userApi = userApi;
        _departmentsApi = departmentsApi;
        _messageApi = messageApi;
    }
}
```

---

## 💡 使用示例

### 📧 消息通知

#### 发送文本消息

```csharp
public class MessageService
{
    private readonly IFeishuTenantV1Message _messageApi;

    public MessageService(IFeishuTenantV1Message messageApi)
    {
        _messageApi = messageApi;
    }

    public async Task SendMessageAsync(string userId, string text)
    {
        var content = new MessageTextContent { Text = text };
        var result = await _messageApi.SendMessageAsync(new SendMessageRequest
        {
            ReceiveId = userId,
            MsgType = "text",
            Content = JsonSerializer.Serialize(content)
        }, receive_id_type: "user_id");

        if (result.Code != 0)
        {
            throw new Exception($"发送失败: {result.Msg}");
        }

        Console.WriteLine($"消息发送成功，消息ID: {result.Data?.MessageId}");
    }
}
```

#### 批量发送通知

```csharp
public class NotificationService
{
    private readonly IFeishuTenantV1BatchMessage _batchMessageApi;

    public async Task<string> SendSystemNotificationAsync(
        string[] departmentIds,
        string title,
        string content)
    {
        var request = new BatchSenderTextMessageRequest
        {
            DeptIds = departmentIds,
            Content = new TextContent
            {
                Text = $"📢 {title}-{content}"
            }
        };

        var result = await _batchMessageApi.BatchSendTextMessageAsync(request);

        if (result.Code == 0)
        {
            var messageId = result.Data!.MessageId;
            Console.WriteLine($"批量消息发送成功，任务ID: {messageId}");

            // 异步查询发送进度
            _ = Task.Run(async () => await MonitorProgressAsync(messageId));

            return messageId;
        }

        throw new Exception($"发送失败: {result.Msg}");
    }

    private async Task MonitorProgressAsync(string messageId)
    {
        for (int i = 0; i < 20; i++)
        {
            var progress = await _batchMessageApi.GetBatchMessageProgressAsync(messageId);

            if (progress.Code == 0)
            {
                var progressData = progress.Data!;
                Console.WriteLine($"发送进度: {progressData.SentCount}/{progressData.TotalCount}");

                if (progressData.IsFinished)
                {
                    Console.WriteLine($"发送完成！成功: {progressData.SentCount}, 失败: {progressData.FailedCount}");
                    break;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
```

### 👤 用户管理

#### 创建用户

```csharp
public async Task<string> CreateUserAsync()
{
    var userResult = await _userApi.CreateUserAsync(new CreateUserRequest
    {
        Name = "张三",
        Mobile = "13800138000",
        DepartmentIds = new[] { "dept_1" },
        Emails = new[] { new EmailValue { Email = "zhangsan@company.com" } }
    });

    if (userResult.Code != 0)
    {
        throw new Exception($"创建用户失败: {userResult.Msg}");
    }

    return userResult.Data!.User!.UserId;
}
```

#### 批量获取用户信息

```csharp
public async Task<List<UserInfo>> GetUsersAsync(string[] userIds)
{
    var result = await _userApi.GetUserByIdsAsync(userIds);

    if (result.Code == 0)
    {
        return result.Data!.Users!;
    }

    throw new Exception($"获取用户失败: {result.Msg}");
}
```

### 🏢 组织架构

#### 获取部门树

```csharp
public async Task<List<DepartmentInfo>> GetDepartmentTreeAsync()
{
    var result = await _deptApi.GetDepartmentsByParentIdAsync("0", fetch_child: true);

    if (result.Code != 0)
    {
        throw new Exception($"获取部门树失败: {result.Msg}");
    }

    return result.Data!.Items!;
}
```

#### 获取部门下的用户

```csharp
public async Task<List<UserInfo>> GetDepartmentUsersAsync(string departmentId)
{
    var result = await _deptApi.GetUserByDepartmentIdAsync(departmentId);

    if (result.Code == 0)
    {
        return result.Data!.Items!;
    }

    throw new Exception($"获取部门用户失败: {result.Msg}");
}
```

---

## 🌐 事件处理

### WebSocket 实时事件订阅

#### 服务注册

```csharp
// 配置 WebSocket 服务（自动包含令牌管理）
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()        // 消息事件
    .AddHandler<UserEventHandler>()           // 用户事件
    .AddHandler<DepartmentCreatedEventHandler>()  // 部门创建事件
    .AddHandler<DepartmentDeletedEventHandler>()  // 部门删除事件
    .Build();

// 配置 Redis 分布式去重（可选）
builder.Services.AddFeishuRedisDeduplicators(builder.Configuration);
```

#### 配置选项

```json
{
  "FeishuWebSocket": {
    "AutoReconnect": true,
    "MaxReconnectAttempts": 5,
    "ReconnectDelayMs": 5000,
    "HeartbeatIntervalMs": 30000,
    "InitialReceiveBufferSize": 4096,
    "EnableLogging": true,
    "EnableMessageQueue": true,
    "MessageQueueCapacity": 1000,
    "EmptyQueueCheckIntervalMs": 100,
    "HealthCheckIntervalMs": 60000,
    "MaxConcurrentMessageProcessing": 10,
    "MessageSizeLimits": {
      "MaxTextMessageSize": 1048576,
      "MaxBinaryMessageSize": 10485760
    },
    "EventDeduplication": {
      "Mode": "Distributed",
      "CacheExpirationMs": 86400000,
      "CleanupIntervalMs": 300000
    }
  }
}
```

#### 事件处理器示例

```csharp
using Mud.Feishu.Abstractions.EventHandlers;

public class MessageEventHandler : MessageReceiveEventBaseHandler
{
    private readonly ILogger<MessageEventHandler> _logger;

    public MessageEventHandler(ILogger<MessageEventHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        MessageReceiveResult? messageData,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "收到消息: {MessageId}, 发送者: {SenderId}, 内容: {Content}",
            messageData?.Message?.MessageId,
            messageData?.Sender?.SenderId,
            messageData?.Message?.Content);

        // 处理业务逻辑
        await ProcessMessageAsync(messageData, cancellationToken);
    }
}
```

### Webhook 事件处理

#### 服务注册

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .AddHandler<DepartmentCreatedEventHandler>()
    .AddHandler<DepartmentDeleteEventHandler>()
    .EnableHealthChecks()    // 启用健康检查
    .EnableMetrics()         // 启用性能监控
    .Build();

var app = builder.Build();
app.UseFeishuWebhook();
```

#### 配置选项

```json
{
  "FeishuWebhook": {
    "VerificationToken": "your_verification_token",
    "EncryptKey": "your_encrypt_key_32_bytes",
    "DefaultAppId": "your_app_id",
    "RoutePrefix": "feishu/Webhook",
    "EnforceHeaderSignatureValidation": true,
    "EnableBodySignatureValidation": true,
    "EventHandlingTimeoutMs": 30000,
    "MaxConcurrentEvents": 10
  }
}
```

#### 事件处理器示例

```csharp
using Mud.Feishu.Abstractions.EventHandlers;

public class DepartmentCreatedEventHandler :
    DefaultFeishuEventHandler<DepartmentCreatedResult>
{
    private readonly IDepartmentService _departmentService;

    public DepartmentCreatedEventHandler(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        DepartmentCreatedResult? departmentData,
        CancellationToken cancellationToken = default)
    {
        if (departmentData == null)
        {
            return;
        }

        // 1. 记录事件到数据库
        await _departmentService.RecordDepartmentEventAsync(
            departmentData, cancellationToken);

        // 2. 处理业务逻辑
        await _departmentService.InitializeDepartmentPermissionsAsync(
            departmentData.Department!.DepartmentId!, cancellationToken);

        // 3. 通知部门主管
        await _departmentService.NotifyDepartmentLeaderAsync(
            departmentData.Department!, cancellationToken);
    }
}
```

---

## ⚙️ 配置选项

### FeishuAppConfig 配置项

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `AppKey` | string | - | 应用唯一标识（必填），用于在代码中引用此应用 |
| `AppId` | string | - | 飞书应用唯一标识（必填） |
| `AppSecret` | string | - | 飞书应用秘钥（必填） |
| `BaseUrl` | string | "https://open.feishu.cn" | 飞书 API 基础地址 |
| `TimeOut` | int | 30 | HTTP 请求超时时间（秒），范围：1-300 |
| `RetryCount` | int | 3 | 失败重试次数，范围：0-10 |
| `RetryDelayMs` | int | 1000 | 重试延迟（毫秒），范围：100-60000 |
| `TokenRefreshThreshold` | int | 300 | 令牌刷新阈值（秒），范围：60-3600 |
| `EnableLogging` | bool | true | 是否启用日志记录 |
| `IsDefault` | bool | false | 是否为默认应用（支持自动推断） |

### FeishuWebSocketOptions 配置项

| 配置项 | 类型 | 默认值 | 说明 |
|-------|------|--------|------|
| **连接管理** | | | |
| `AutoReconnect` | bool | true | 是否自动重连 |
| `MaxReconnectAttempts` | int | 5 | 最大重连次数 |
| `ReconnectDelayMs` | int | 5000 | 重连延迟（毫秒） |
| `ConnectionTimeoutMs` | int | 10000 | 连接超时（毫秒） |
| **心跳检测** | | | |
| `HeartbeatIntervalMs` | int | 30000 | 心跳间隔（毫秒） |
| `HealthCheckIntervalMs` | int | 60000 | 健康检查间隔（毫秒） |
| **消息处理** | | | |
| `MaxConcurrentMessageProcessing` | int | 10 | 最大并发消息处理数 |
| `EnableMessageQueue` | bool | true | 启用消息队列 |
| `MessageQueueCapacity` | int | 1000 | 队列容量 |
| **事件去重** | | | |
| `EventDeduplication.Mode` | `EventDeduplicationMode` | `InMemory` | 去重模式（None/InMemory/Distributed） |
| `EventDeduplication.CacheExpirationMs` | int | 86400000 | 缓存过期时间（毫秒） |
| `EventDeduplication.CleanupIntervalMs` | int | 300000 | 缓存清理间隔（毫秒） |

### FeishuWebhookOptions 配置项

| 配置项 | 类型 | 默认值 | 说明 |
|-------|------|--------|------|
| **安全配置** | | | |
| `VerificationToken` | string | - | 验证 Token |
| `EncryptKey` | string | - | 事件加密 Key（32字节） |
| `DefaultAppId` | string | - | 默认应用ID |
| `EnforceHeaderSignatureValidation` | bool | true | 强制签名验证 |
| `ValidateSourceIP` | bool | false | 验证来源IP |
| **路由配置** | | | |
| `RoutePrefix` | string | "feishu/Webhook" | 路由前缀 |
| `AutoRegisterEndpoint` | bool | true | 自动注册端点 |
| **事件处理** | | | |
| `EventHandlingTimeoutMs` | int | 30000 | 事件处理超时（毫秒） |
| `MaxConcurrentEvents` | int | 10 | 最大并发事件数 |

### 配置验证

`FeishuAppConfig` 提供了 `Validate()` 方法用于验证配置项的有效性：

- `TimeOut` 必须在 1-300 秒之间
- `RetryCount` 必须在 0-10 次之间
- `RetryDelayMs` 必须在 100-60000 毫秒之间
- `TokenRefreshThreshold` 必须在 60-3600 秒之间
- `AppId` 必须以 `cli_` 或 `app_` 开头，且长度至少为 20 字符
- `AppSecret` 长度至少为 16 字符
- `RetryCount` 必须在 0-10 次之间
- `BaseUrl` 必须是有效的 HTTP/HTTPS URL 格式

### 安全建议

- `AppId` 和 `AppSecret` 是飞书应用的身份凭证，请妥善保管
- 建议使用环境变量或安全的配置管理系统来存储敏感信息
- 不要在代码中硬编码敏感信息
- 在生产环境中，建议使用 HTTPS 协议以确保通信安全
- 生产环境必须启用签名验证和时间戳验证

---

## 📋 API 模块详情

> 本章节详细介绍 `Mud.Feishu/Interfaces` 目录中各个模块的接口定义及其支持的方法。

### 📄 文档管理 (Docx)

飞书文档 API 接口，支持文档创建、编辑、块操作等。飞书开放平台云文档分为文档和块：
- **文档**：用户在云文档中创建的一篇在线文档，每篇文档都有唯一的 `document_id` 作为标识
- **块**：文档中的最小构建单元，是内容的结构化组成元素，可以是一段文字、一张电子表格、一张图片或一个多维表格等

#### 文档基础操作 (`IFeishuV1Docx`)

| 方法 | 说明 |
|------|------|
| `CreateDocumentAsync` | 创建文档类型为 docx 的文档，可选择传入文档标题和文件夹 |
| `GetDocumentInfoAsync` | 获取文档基本信息，包括标题、所有者、创建时间等 |
| `GetDocumentRawContentAsync` | 获取文档的纯文本内容，支持指定 @用户 的语言 |
| `GetDocumentBlocksPageListAsync` | 获取文档所有块的富文本内容并分页返回 |

#### 文档块操作 (`IFeishuV1DocxBlocks`)

| 方法 | 说明 |
|------|------|
| `CreateBlockAsync` | 指定需要操作的块，为其创建一批子块，并插入到指定位置 |
| `CreateDescendantBlockAsync` | 在指定块的子块列表中，新创建一批有父子关系的子块 |
| `UpdateBlockAsync` | 更新指定块的内容 |
| `GetBlockInfoAsync` | 指定块的 block_id 获取指定块的富文本内容数据 |
| `BatchUpdateBlocksAsync` | 批量更新块的富文本内容 |
| `GetChildrenBlocksPageListAsync` | 获取文档中指定块的所有子块的富文本内容并分页返回 |
| `BatchDeleteBlocksAsync` | 指定需要操作的块，删除其指定范围的子块 |
| `ContentConvertAsync` | 将 Markdown/HTML 格式的内容转换为文档块 |

---

### 📚 知识库 (Wiki)

飞书知识库是一个面向组织的知识管理系统，通过结构化沉淀高价值信息，形成完整的知识体系。明确的内容分类，层级式的页面树，能够轻松提升知识的流转和传播效率。

#### 知识空间管理 (`IFeishuV2Wiki`)

| 方法 | 说明 |
|------|------|
| `GetSpacesPageListAsync` | 获取有权限访问的知识空间列表（不返回"我的文档库"） |
| `GetSpaceInfoAsync` | 根据知识空间 ID 查询知识空间信息，包括类型、可见性、分享状态等 |
| `GetSpaceMemberPageListAsync` | 获取知识空间成员列表 |
| `CreateSpaceMemberAsync` | 添加知识空间成员（需管理员权限） |
| `DeleteSpaceMemberAsync` | 删除知识空间成员 |
| `UpdateSpaceSettingAsync` | 更新知识空间设置 |

---

### ☁️ 云盘管理 (Drive)

云空间内各种类型的文件的统称，泛指云空间内所有的文件，包括在云空间创建的在线文档、电子表格、多维表格、思维笔记、知识库中的文档等，也包括从本地环境上传的各类文件。

#### 文件操作 (`IFeishuV1DriveFiles`)

| 方法 | 说明 |
|------|------|
| `BatchQueryMetasAsync` | 根据文件 token 获取其元数据，包括标题、所有者、创建时间、密级、访问链接等 |
| `GetFileStatisticsByFileTokenAsync` | 获取各类文件的流量统计信息和互动信息，包括阅读人数、阅读次数和点赞数 |
| `GetFileViewRecordPageListByFileTokenAsync` | 获取文档、电子表格、多维表格等文件的历史访问记录 |
| `CopyFileByFileTokenAsync` | 将用户云空间中的文件复制至其它文件夹下（异步接口） |
| `MoveFileByFileTokenAsync` | 将文件或者文件夹移动到用户云空间的其他位置（异步接口） |
| `DeleteFileByFileTokenAsync` | 删除用户在云空间内的文件或者文件夹（进入回收站） |
| `CreateShortcutAsync` | 创建指定文件的快捷方式到云空间的其它文件夹中 |
| `UploadAllFileAsync` | 将指定文件上传至云空间指定目录中（≤20MB） |
| `UploadPrepareFileAsync` | 发送初始化请求，以获取上传事务 ID 和分片策略 |
| `UploadPartFileAsync` | 根据预上传接口返回的上传事务 ID 和分片策略上传对应的文件分片 |
| `UploadFinishFileAsync` | 将分片全部上传完毕后，触发完成上传 |
| `DownloadFileAsync` | 下载云空间中的文件（如 PDF 文件），支持分片下载 |
| `CreateImportTaskAsync` | 创建导入文件的任务，将本地文件导入为飞书在线云文档 |
| `GetImportTaskAsync` | 根据导入任务 ID 轮询导入结果 |
| `CreateExportTaskAsync` | 创建导出文件的任务，将飞书文档导出为本地文件 |
| `GetExportTaskAsync` | 根据导出任务 ID 轮询导出任务结果 |
| `DownloadExportFileAsync` | 根据导出文件的 token 下载导出产物到本地 |
| `GetFileLikePageListByFileTokenAsync` | 获取指定云文档的点赞者列表 |

#### 文件夹操作 (`IFeishuV1DriveFolder`)

| 方法 | 说明 |
|------|------|
| `GetDriveRootFolderMetaAsync` | 获取用户"我的空间"（根文件夹）的元数据 |
| `GetFilesPageListAsync` | 获取文件夹中的文件清单 |
| `GetFolderMetaByTokenAsync` | 根据文件夹 token 获取该文件夹的元数据 |
| `CreateFolderAsync` | 在用户云空间指定文件夹中创建一个空文件夹 |
| `GetTaskCheckFileAsync` | 查询异步任务的状态信息（删除文件夹和移动文件夹） |

#### 媒体文件操作 (`IFeishuV1DriveMedia`)

| 方法 | 说明 |
|------|------|
| `UploadAllMediaAsync` | 将文件、图片、视频等素材上传到指定云文档中（≤20MB） |
| `UploadPrepareMediaAsync` | 发送初始化请求，以获取上传事务 ID 和分片策略 |
| `UploadPartMediaAsync` | 根据预上传接口返回的上传事务 ID 和分片策略上传对应的素材分片 |
| `UploadFinishMediaAsync` | 将素材分片全部上传完毕后，触发完成上传 |
| `DownloadFileAsync` | 下载各类云文档中的素材（如电子表格中的图片） |
| `BatchGetTmpDownloadUrlAsync` | 获取云文档中素材的临时下载链接（有效期 24 小时） |

---

### ⏰ 考勤管理 (Attendance)

企业考勤全流程管理 API 接口，支持考勤组管理、打卡流水、考勤统计、班次管理等功能。

#### 考勤组管理 (`IFeishuV1AttendanceGroups`)

考勤组是对部门或者员工在某个特定场所及特定时间段内的出勤情况的规则设定，可以从部门、员工两个维度来设定考勤方式、考勤时间、考勤地点等考勤规则。

| 方法 | 说明 |
|------|------|
| `CreateGroupAsync` | 创建或修改考勤组 |
| `DeleteGroupByIdAsync` | 通过考勤组 ID 删除考勤组 |
| `GetGroupByIdAsync` | 通过考勤组 ID 获取考勤组详情（基本信息、考勤班次、考勤方式、考勤设置） |
| `GetGroupByNameAsync` | 按考勤组名称查询考勤组摘要信息（支持精确匹配和模糊匹配） |
| `GetGroupPageListAsync` | 分页获取所有考勤组列表 |

#### 考勤统计 (`IFeishuV1AttendanceStats`)

考勤统计接口支持开发者定制接口返回数据，让开发者可以只获取自己所关注的数据内容。

| 方法 | 说明 |
|------|------|
| `UpdateUserStatsViewAsync` | 更新开发者定制的日度统计或月度统计的统计报表表头设置信息 |
| `QueryUserStatsFieldAsync` | 查询考勤统计支持的日度统计或月度统计的统计表头 |
| `QueryUserStatsViewAsync` | 查询考勤统计支持的日度统计或月度统计的统计表头 |
| `QueryUserStatsDataAsync` | 查询日度统计或月度统计的统计数据 |

#### 打卡流水管理 (`IFeishuV1AttendanceUserFlows`)

打卡信息管理，可以导入、查询、删除员工的打卡流水记录。

| 方法 | 说明 |
|------|------|
| `BatchCreateUserFlowAsync` | 导入员工的打卡流水记录（导入后会根据班次规则计算打卡状态与结果） |
| `GetUserFlowAsync` | 通过打卡记录 ID 获取用户的打卡流水记录 |
| `QueryUserFlowAsync` | 通过打卡记录 ID 批量查询打卡流水记录 |
| `BatchDelUserFlowAsync` | 删除员工从开放平台导入的打卡记录 |
| `QueryUserTaskAsync` | 获取企业内员工的实际打卡结果 |

---

### 📋 审批流程 (Approval)

企业审批全流程管理 API 接口，支持审批定义、审批实例、审批任务、审批评论等功能。

#### 审批定义和实例 (`IFeishuV4Approval`)

原生审批用于根据企业业务需要在飞书审批中心创建审批定义，用来定义一类审批的表单与流程。

| 方法 | 说明 |
|------|------|
| `CreateApprovalAsync` | 创建审批定义（可灵活指定基础信息、表单和流程等） |
| `GetApprovalByCodeAsync` | 根据审批定义 Code 获取审批定义信息（名称、状态、表单控件、节点等） |
| `CreateInstanceAsync` | 使用指定审批定义 Code 创建一个审批实例 |
| `CancelInstanceAsync` | 撤回审批实例 |
| `CarbonCopyInstanceAsync` | 将当前审批实例抄送给指定用户 |
| `PreviewInstanceAsync` | 在创建审批实例之前或之后预览审批流程数据 |
| `GetInstanceByIdAsync` | 通过审批实例 Code 获取审批实例的详细信息 |

#### 审批查询 (`IFeishuV4ApprovalQuery`)

通过不同条件查询审批系统中符合条件的审批实例、审批抄送、审批任务列表。

| 方法 | 说明 |
|------|------|
| `GetInstancesPageListAsync` | 通过不同条件查询审批系统中符合条件的审批实例列表 |
| `GetCarbonCopyPageListAsync` | 通过不同条件查询审批系统中符合条件的审批抄送列表 |
| `GetTasksPageListAsync` | 通过不同条件查询审批系统中符合条件的审批任务列表 |

#### 审批任务管理 (`IFeishuV4ApprovalTask`)

审批实例的流程中包含多个审批节点，审批节点内会生成审批任务，可以同意、拒绝、转交以及退回审批任务。

| 方法 | 说明 |
|------|------|
| `AgreeApprovalAsync` | 对单个审批任务进行同意操作（同意后流程流转到下一个审批人） |
| `RejectApprovalAsync` | 对单个审批任务进行拒绝操作（拒绝后审批流程结束） |
| `TransferApprovalAsync` | 对单个审批任务进行转交操作（转交后流程流转给被转交人） |
| `RollbackApprovalAsync` | 从当前审批任务，退回到已审批的一个或多个任务节点 |
| `InstancesAddSignAsync` | 对单个审批任务进行加签操作 |
| `ResubmitApprovalAsync` | 对于退回到发起人的审批任务进行重新发起操作 |

#### 审批评论 (`IFeishuV4ApprovalComments`)

审批实例内支持员工进行评论、回复评论，评论内容支持文本、@用户以及添加附件。

| 方法 | 说明 |
|------|------|
| `CreateCommentAsync` | 在指定审批实例下创建、修改评论或回复评论 |
| `DeleteCommentByIdAsync` | 删除某审批实例下的一条评论或评论回复 |
| `RemoveCommentsAsync` | 清空某审批实例下的全部评论与评论回复 |
| `GetCommentsPageListByIdAsync` | 根据审批实例 Code 获取某个审批实例下全部评论与评论回复 |

---

### 📝 任务管理 (Task)

飞书任务是一款飞书自带的通用任务/项目管理工具，拥有强大的协作能力。可以轻松地在飞书 App 的任务中心、群组、文档等场景中快捷创建任务。

#### 任务管理 (`IFeishuV2Task`)

| 方法 | 说明 |
|------|------|
| `CreateTaskAsync` | 创建一个任务（支持填写标题、描述、负责人、时间、提醒等） |
| `UpdateTaskAsync` | 修改任务的标题、描述、截止时间等信息 |
| `GetTaskByIdAsync` | 获取任务详情（标题、描述、时间、成员等） |
| `DeleteTaskByIdAsync` | 删除一个任务 |
| `AddMembersByIdAsync` | 添加任务的负责人或关注人 |
| `RemoveMembersByIdAsync` | 移除任务的负责人或关注人 |
| `GetTaskListsByIdAsync` | 列取一个任务所在的所有清单的信息 |
| `AddTaskListsByIdAsync` | 将一个任务加入清单 |
| `RemoveTaskListsByIdAsync` | 将任务从一个清单中移出 |
| `AddTaskReminderByIdAsync` | 为一个任务添加提醒（基于截止时间计算） |
| `RemoveTaskReminderByIdAsync` | 将一个提醒从任务中移除 |
| `AddTaskDependenciesByIdAsync` | 为一个任务添加依赖（前置依赖和后置依赖） |
| `RemoveTaskDependenciesByIdAsync` | 从一个任务移除依赖 |
| `CreateSubTaskAsync` | 给一个任务创建一个子任务 |
| `GetSubTasksPageListByIdAsync` | 分页获取一个任务的子任务列表 |

#### 自定义字段 (`IFeishuV2TaskCustomFields`)

任务功能支持在任务中扩充自定义字段，更清晰地添加任务关键信息，可以自行定义如"优先级"、"项目发布日期"、"价格"等字段。

| 方法 | 说明 |
|------|------|
| `CreateCustomFieldsAsync` | 创建一个自定义字段，并将其加入一个资源（清单） |
| `UpdateCustomFieldsAsync` | 更新一个自定义字段的名称和设定 |
| `GetCustomFieldsByIdAsync` | 获取自定义字段详情 |
| `GetCustomFieldsPageListAsync` | 分页列取用户可访问的自定义字段列表 |
| `AddCustomFieldsByIdAsync` | 将自定义字段加入一个资源（清单） |
| `RemoveCustomFieldsByIdAsync` | 将自定义字段从资源中移出 |
| `CreateCustomFieldsOptionsAsync` | 为单选或多选字段添加一个自定义选项 |
| `UpdateCustomFieldsOptionsAsync` | 更新自定义字段选项的数据 |

---

### 👥 组织架构 (Organization)

完整的组织架构管理 API 接口，支持用户、部门、员工、用户组、职务、职级等管理。

#### 用户管理 (`IFeishuV3User`)

飞书用户是飞书通讯录中的基础资源，对应企业组织架构中的成员实体。

| 方法 | 说明 |
|------|------|
| `CreateUserAsync` | 向通讯录创建一个用户（员工入职） |
| `UpdateUserIdAsync` | 更新用户 ID |
| `GetBatchUsersAsync` | 通过手机号或邮箱获取一个或多个用户的 ID 与状态信息 |
| `GetUsersByKeywordAsync` | 通过用户名关键词搜索其他用户的信息 |
| `DeleteUserByIdAsync` | 从通讯录内删除一个指定用户（员工离职） |
| `ResurrectUserByIdAsync` | 恢复已删除用户（已离职的成员） |
| `LogoutAsync` | 退出用户的登录态 |
| `GetJsTicketAsync` | 获取调用 JSAPI 临时调用凭证 |

#### 部门管理 (`IFeishuV1Departments`)

部门是飞书组织架构里的一个基础实体，每个员工都归属于一个或多个部门。

| 方法 | 说明 |
|------|------|
| `CreateDepartmentAsync` | 在企业组织机构中创建新部门 |
| `UpdateDepartmentAsync` | 更新企业组织机构部门信息 |
| `DeleteDepartmentByIdAsync` | 从企业组织机构中删除指定的部门 |
| `QueryDepartmentsAsync` | 支持传入多个部门 ID，返回每个部门的详细信息 |
| `QueryDepartmentsPageListAsync` | 依据指定条件，批量获取符合条件的部门详情列表 |
| `SearchEmployeePageListAsync` | 搜索部门信息（通过部门名称等关键词） |

#### 员工管理 (`IFeishuV1Employees`)

员工指飞书企业内身份为「Employee」的成员，等同于通讯录 OpenAPI 中的「User」。

| 方法 | 说明 |
|------|------|
| `CreateEmployeeAsync` | 在企业下创建员工 |
| `UpdateEmployeeAsync` | 更新在职/离职员工的信息、冻结/恢复员工 |
| `DeleteEmployeeByIdAsync` | 离员工（需应用有员工所有所属部门的权限） |
| `ResurrectEmployeeAsync` | 恢复已离职的成员至在职状态 |
| `ResignedEmployeeAsync` | 为在职员工办理离职，将其更新为「待离职」状态 |
| `RegularEmployeeAsync` | 为待离职员工取消离职，将其更新为「在职」状态 |
| `QueryEmployeesAsync` | 批量根据员工的 ID 查询员工的详情 |
| `QueryEmployeePageListAsync` | 依据指定条件，分页批量获取符合条件的员工详情列表 |
| `SearchEmployeePageListAsync` | 搜索员工信息（通过关键词搜索名称、手机号、邮箱等） |

#### 用户组管理 (`IFeishuV3UserGroup`)

用户组是飞书通讯录中基础实体之一，在用户组内可添加用户或部门资源。各类业务权限管控可以与用户组关联。

| 方法 | 说明 |
|------|------|
| `CreateUserGroupAsync` | 创建用户组 |
| `UpdateUserGroupAsync` | 更新用户组 |
| `GetUserGroupInfoByIdAsync` | 通过用户组 ID 查询指定用户组的基本信息 |
| `GetUserGroupsAsync` | 查询当前租户下的用户组列表 |
| `GetUserBelongGroupsAsync` | 查询指定用户所属的用户组列表 |
| `DeleteUserGroupByIdAsync` | 删除指定用户组 |

#### 角色管理 (`IFeishuV3Role`)

飞书角色指的是团队成员的专业分工类别，如人事、行政、财务等，一个角色可由一名或多名成员组成。目前主要用于应用审批场景。

| 方法 | 说明 |
|------|------|
| `CreateRoleAsync` | 创建一个角色 |
| `UpdateRoleAsync` | 修改指定角色的角色名称 |
| `DeleteRoleByIdAsync` | 删除指定角色 |

---

### 💬 消息服务 (Messages)

消息即飞书聊天中的一条消息，可以使用消息管理 API 对消息进行发送、回复、编辑、撤回、转发以及查询等操作。

#### 消息管理 (`IFeishuV1Message`)

| 方法 | 说明 |
|------|------|
| `RevokeMessageAsync` | 撤回指定消息（机器人可撤回自己发送的消息，群主可撤回群内消息） |
| `AddMessageReactionsAsync` | 给指定消息添加指定类型的表情回复 |
| `GetMessageReactionsPageListAsync` | 获取指定消息内的表情回复列表 |
| `DeleteMessageReactionsAsync` | 删除指定消息的某一表情回复 |
| `PinMessageAsync` | Pin 一条指定的消息 |
| `DeletePinMessageAsync` | 移除一条指定消息的 Pin |
| `GetPinMessagePageListAsync` | 获取指定群、指定时间范围内的所有 Pin 消息 |

#### 租户级消息操作 (`IFeishuTenantV1Message`)

| 方法 | 说明 |
|------|------|
| `SendMessageAsync` | 向指定用户或者群聊发送消息（支持文本、富文本、卡片、图片、视频、音频、文件等） |
| `ReplyMessageAsync` | 回复指定消息 |
| `EditMessageAsync` | 编辑已发送的消息内容（支持文本、富文本消息） |
| `ReceiveMessageAsync` | 将一条指定的消息转发给用户、群聊或话题 |
| `MergeReceiveMessageAsync` | 将来自同一个会话内的多条消息，合并转发给指定的用户、群聊或话题 |
| `ReceiveThreadsAsync` | 将话题转发至指定的用户、群聊或话题 |
| `CreateMessageFollowUpAsync` | 在最新一条消息下方添加气泡样式的内容 |
| `GetMessageReadUsesAsync` | 查询指定消息是否已读 |
| `GetHistoryMessageAsync` | 获取指定会话内的历史消息（聊天记录） |
| `GetMessageFile` | 获取指定消息内包含的资源文件（小文件） |
| `GetMessageLargeFile` | 获取指定消息内包含的资源文件（大文件） |
| `GetContentListByMessageIdAsync` | 通过消息的 message_id 查询指定消息的内容 |
| `DownFileAsync` | 通过已上传文件的 Key 下载文件（小文件） |
| `DownLargeFileAsync` | 通过已上传文件的 Key 下载文件（大文件） |
| `DownImageAsync` | 通过已上传图片的 Key 下载图片（小文件） |
| `DownLargeImageAsync` | 通过已上传图片的 Key 下载图片（大文件） |
| `UploadFileAsync` | 将本地文件上传至开放平台（支持音频、视频、文档等） |
| `UploadImageAsync` | 将图片上传至飞书开放平台 |
| `MessageUrgentAppAsync` | 把指定消息加急给目标用户（飞书客户端内通知） |
| `MessageUrgentSMSAsync` | 把指定消息加急给目标用户（飞书客户端和短信） |
| `MessageUrgentPhoneAsync` | 把指定消息加急给目标用户（飞书客户端和电话） |
| `UpdateUrlPreviewAsync` | 更新 URL 预览 |

#### 批量消息 (`IFeishuTenantV1BatchMessage`)

用于管理给多个用户或者多个部门发送消息。

| 方法 | 说明 |
|------|------|
| `BatchSendTextMessageAsync` | 给多个用户或者多个部门中的成员发送文本消息 |
| `BatchSendRichTextMessageAsync` | 给多个用户或者多个部门中的成员发送富文本消息 |
| `BatchSendImageMessageAsync` | 给多个用户或者多个部门中的成员发送图片消息 |
| `BatchSendGroupShareMessageAsync` | 给多个用户或者多个部门中的成员发群分享消息 |
| `RevokeMessageAsync` | 撤回通过批量发送消息接口发送的消息 |
| `GetUserReadMessageInfosAsync` | 查询批量消息推送的总人数以及消息已读人数 |
| `GetBatchMessageProgressAsync` | 查询消息的发送进度和撤回进度 |

---

### 🃏 卡片服务 (Cards)

飞书卡片是应用的一种能力，包括构建卡片内容所需的组件和发送卡片所需的能力，并提供了可视化搭建工具。

#### 卡片管理 (`IFeishuV1Card`)

| 方法 | 说明 |
|------|------|
| `CreateCardAsync` | 基于卡片 JSON 代码或卡片搭建工具搭建的卡片，创建卡片实体 |
| `UpdateCardSettingsByIdAsync` | 更新指定卡片实体的配置（支持 config 字段和 card_link 字段） |
| `PartialUpdateCardByIdAsync` | 更新卡片实体局部内容（包括配置和组件，支持多组件增删改） |
| `UpdateCardByIdAsync` | 传入新的卡片 JSON 代码，覆盖更新指定的卡片实体的所有内容 |

#### 卡片元素操作 (`IFeishuV1CardElements`)

| 方法 | 说明 |
|------|------|
| `CreateCardElementAsync` | 为指定卡片实体新增组件，以扩展卡片内容 |
| `UpdateCardElementByIdAsync` | 更新卡片实体中的指定组件为新组件 |
| `UpdateCardElementAttributeByIdAsync` | 更新卡片实体中对应组件的属性 |
| `StreamUpdateCardTextByIdAsync` | 对文本元素传入全量文本内容，实现"打字机"式的文字输出效果 |
| `DeleteCardElementByIdAsync` | 删除指定卡片实体中的组件 |

#### 消息流卡片 (`IFeishuV2AppCardMessageStream`)

应用消息流卡片是飞书为应用提供的消息触达能力，让应用可以直接在消息流发送消息。

| 方法 | 说明 |
|------|------|
| `CreateCardMessageStreamAsync` | 创建应用消息流卡片 |
| `UpdateCardMessageStreamAsync` | 更新应用消息流卡片 |
| `DeleteCardMessageStreamAsync` | 删除应用消息流卡片 |
| `BotTimeSentiveAsync` | 将机器人对话在消息列表中置顶展示 |
| `UpdateCardMessageStreamButtonAsync` | 为消息流卡片添加、更新、删除快捷操作按钮 |
| `FeedCardsByFeedCardIdAsync` | 即时提醒能力，将群组或机器人对话在消息列表中置顶展示 |

---

### 💬 群组管理 (ChatGroup)

飞书群组 OpenAPI 提供了群组管理能力，包括创建群、解散群、更新群信息、获取群信息、管理群置顶以及获取群分享链接等。

#### 群组管理 (`IFeishuV1ChatGroup`)

| 方法 | 说明 |
|------|------|
| `UpdateChatGroupByIdAsync` | 更新指定群的信息（群头像、群名称、群描述、群配置、群主等） |
| `DeleteChatGroupAsync` | 通过 chat_id 解散指定群组 |
| `UpdateChatModerationAsync` | 更新指定群组的发言权限（所有成员可发言、仅管理员可发言、指定成员可发言） |
| `GetChatGroupInoByIdAsync` | 获取指定群的基本信息（名称、描述、头像、群主 ID、权限配置等） |
| `PutChatGroupTopNoticeAsync` | 更新群组中的群置顶信息（可将消息或群公告置顶展示） |
| `DeleteChatGroupTopNoticeAsync` | 撤销指定群组中的置顶消息或群公告 |
| `GetChatGroupPageListAsync` | 分页获取当前用户或机器人所在的群列表 |
| `GetChatGroupPageListByKeywordAsync` | 分页获取当前身份可见的群列表（支持关键词搜索） |
| `GetChatGroupModeratorPageListByIdAsync` | 分页获取指定群组的发言模式、可发言用户名单等信息 |
| `GetChatGroupShareLinkByIdAsync` | 获取指定群的分享链接 |

#### 群公告管理 (`IFeishuV1ChatGroupAnnouncement`)

群公告是群组中的公告文档，采用飞书云文档承载，每个群组只有一个群公告。

| 方法 | 说明 |
|------|------|
| `GetNoticeInfoByIdAsync` | 获取指定群组中的群公告基本信息 |
| `GetNoticeBlocksListByIdAsync` | 获取群公告所有块的富文本内容并分页返回 |
| `CreateNoticeBlockAsync` | 在指定块的子块列表中，新创建一批子块 |
| `UpdateNoticeBlockAsync` | 批量更新块的富文本内容 |
| `GetBlockContentByIdAsync` | 获取群公告块的富文本内容 |
| `GetBlockContentPageListByIdAsync` | 获取群公告所有块的富文本内容并分页返回 |
| `DeleteBlockByIdAsync` | 指定需要操作的块，删除其指定范围的子块 |

#### 群成员管理 (`IFeishuV1ChatGroupMember`)

飞书群成员包括用户和机器人，支持添加用户或机器人作为群成员，同时支持将用户或机器人设置为群管理员。

| 方法 | 说明 |
|------|------|
| `AddManagersAsync` | 指定群组，将群内指定的用户或者机器人设置为群管理员 |
| `DeleteManagersAsync` | 指定群组，删除群组内指定的管理员 |
| `AddMemberAsync` | 把指定的用户或机器人拉入指定群聊内 |
| `MeJoinChatGroupAsync` | 将当前调用接口的操作者加入指定群聊 |
| `RemoveMemberAsync` | 将指定的用户或机器人从群聊中移出 |
| `GetMemberPageListByIdAsync` | 分页获取指定群组的成员信息 |
| `GetMemberInChatByIdAsync` | 根据使用的 access_token 判断对应的用户或机器人是否在指定的群里 |

---

## 🎯 常见操作快速参考

### 令牌管理

```csharp
// 直接获取有效令牌（自动处理刷新）
var token = await tokenManager.GetTokenAsync();

// 监控令牌缓存状态
var (total, expired) = tokenManager.GetCacheStatistics();
logger.LogInformation("令牌缓存状态: 总数 {Total}, 过期 {Expired}", total, expired);

// 清理过期令牌
tokenManager.CleanExpiredTokens();
```

### 分页处理

```csharp
public async Task<List<T>> GetAllItemsAsync<T>(
    Func<string?, Task<FeishuApiPageListResult<T>>> pageFetcher)
{
    var allItems = new List<T>();
    string? pageToken = null;

    do
    {
        var result = await pageFetcher(pageToken);

        if (result.Code == 0 && result.Data?.Items != null)
        {
            allItems.AddRange(result.Data.Items);
            pageToken = result.Data.PageToken;
        }
        else
        {
            break;
        }

    } while (!string.IsNullOrEmpty(pageToken));

    return allItems;
}

// 使用示例
var allUsers = await GetAllItemsAsync(pageToken =>
    userApi.GetUserByDepartmentIdAsync("dept_123", page_size: 50, page_token: pageToken));
```

---

## 🔒 错误处理最佳实践

### 统一错误处理

```csharp
public class FeishuServiceBase
{
    protected async Task<T> ExecuteWithErrorHandling<T>(
        Func<Task<T>> operation,
        string operationName)
    {
        try
        {
            var result = await operation();

            if (result.Code != 0)
            {
                throw new FeishuServiceException(
                    $"飞书 API 调用失败: {operationName}",
                    result.Code,
                    result.Msg);
            }

            return result.Data!;
        }
        catch (FeishuException ex)
        {
            // 飞书 API 错误
            logger.LogError(ex,
                "飞书 API 错误 (代码: {ErrorCode}): {Message}",
                ex.ErrorCode, ex.Message);
            throw;
        }
        catch (HttpRequestException ex)
        {
            // 网络错误
            logger.LogError(ex, "网络请求失败: {Message}", ex.Message);
            throw new FeishuServiceException(
                $"网络连接失败: {operationName}", -1, ex.Message);
        }
    }
}

// 使用示例
public async Task<UserInfo> GetUserSafelyAsync(string userId)
{
    return await ExecuteWithErrorHandling(
        () => userApi.GetUserInfoByIdAsync(userId),
        "获取用户信息");
}
```

---

## 📊 与原生飞书 SDK 的对比

| 对比维度 | 原生 SDK 调用 | Mud.Feishu 组件 | 优势说明 |
|---------|--------------|----------------|----------|
| **开发效率** | 需要手动构造 HTTP 请求、处理响应、解析 JSON 等大量样板代码 | 只需调用简洁的接口方法，一行代码完成操作 | 大幅减少代码量，提高开发效率 |
| **类型安全** | 手动处理 JSON 序列化/反序列化，容易出现类型转换错误 | 提供完整的强类型支持，编译时就能发现类型错误 | 提高代码健壮性，减少运行时错误 |
| **令牌管理** | 需要手动获取、刷新和管理访问令牌 | 自动处理令牌获取和刷新机制 | 减少开发者负担，避免令牌管理错误 |
| **异常处理** | 需要手动处理各种网络异常和业务异常 | 提供统一的异常处理机制和明确的异常类型 | 简化异常处理逻辑，提高代码可读性 |
| **重试机制** | 需要手动实现重试逻辑 | 内置智能重试机制，自动处理网络抖动等问题 | 提高系统稳定性 |
| **可测试性** | 直接调用 HTTP 接口，难以进行单元测试 | 基于接口设计，易于进行 Mock 测试 | 提高代码质量和可维护性 |
| **文档完善度** | 需要在飞书官方文档中查找各个接口的详细说明 | 提供完整的中文 API 文档和示例代码 | 降低学习成本，快速上手 |
| **事件处理** | 需要自行实现 WebSocket 和 Webhook 处理逻辑 | 提供完整的 WebSocket 和 Webhook 事件处理框架 | 简化事件驱动架构的实现 |
| **分布式支持** | 需要自行实现分布式锁和去重机制 | 内置 Redis 分布式去重，支持多实例部署 | 快速构建高可用系统 |

---

## 📁 示例项目

### Mud.Feishu.Test

完整的 HTTP API 功能测试，包含所有模块的演示代码：

- **组织架构**：用户、部门、员工、用户组、职务、职级等
- **消息服务**：消息发送、批量消息
- **群聊管理**：群组、成员、菜单、会话标签
- **审批流程**：审批实例、审批任务、审批评论
- **任务管理**：任务、任务列表、任务评论、自定义字段
- **卡片服务**：卡片管理、卡片元素、消息流卡片
- **文档管理**：飞书文档、文档块操作、内容转换
- **知识库**：知识空间管理、节点操作、文档移动
- **云盘管理**：文件上传下载、文件夹管理、版本控制
- **考勤管理**：考勤组、打卡记录、请假审批、统计报表

### FeishuWikiManager

飞书知识库管理 Demo（Vue3 + .NET），展示：

- 飞书 OAuth 2.0 登录集成
- 知识空间浏览和管理
- 文档搜索和收藏
- 用户信息和权限管理

### Mud.Feishu.Webhook.Demo

Webhook 事件处理演示，展示如何：

- 注册和配置 Webhook 服务
- 实现自定义事件处理器
- 处理部门创建、更新、删除事件
- 实现事件去重和安全验证

### Mud.Feishu.WebSocket.Demo

WebSocket 实时事件订阅演示，展示如何：

- 注册和配置 WebSocket 服务
- 实现自定义事件处理器
- 处理实时用户和部门事件
- 使用 Redis 实现分布式去重

---

## 🛠️ 技术架构

### 设计模式

- **策略模式** - 事件处理器接口和实现
- **工厂模式** - 处理器工厂和表单组件工厂
- **建造者模式** - 服务注册构造者
- **中间件模式** - Webhook 中间件和限流中间件

### 企业级特性

- **自动令牌管理** - 智能缓存（提前 5 分钟刷新），解决缓存击穿和竞态条件
- **智能重试** - 基于 Polly 策略，指数退避算法
- **统一异常处理** - `FeishuException` 和详细日志记录
- **高性能缓存** - `ConcurrentDictionary` + `Lazy<Task>`，并发安全
- **分布式支持** - Redis 去重，支持集群和哨兵模式

---

## 📚 支持的 .NET 版本

| .NET 版本 | 支持状态 | 说明 |
|----------|---------|------|
| .NET Standard 2.0 | ✅ | 兼容性版本 |
| .NET 6.0 | ✅ LTS | 长期支持版本 |
| .NET 7.0 | ✅ | 稳定版本 |
| .NET 8.0 | ✅ LTS | 长期支持版本 |
| .NET 9.0 | ✅ | 稳定版本 |
| .NET 10.0 | ✅ LTS | 长期支持版本 |

---

## 🤝 贡献指南

我们欢迎社区贡献！请遵循以下指南：

1. **Fork 项目**并创建特性分支
2. **编写代码**并添加相应的单元测试
3. **确保代码质量**：遵循项目编码规范，代码覆盖率不低于 80%
4. **提交 Pull Request**：详细描述更改内容和测试结果

### 代码规范

- 使用 C# 13.0 语言特性
- 遵循 Microsoft 编码规范
- 所有公共 API 必须包含 XML 文档注释
- 异步方法命名以 `Async` 结尾
- 所有接口必须指定飞书 API 原始文档 URL

### 测试要求

- 新功能必须在 `Mud.Feishu.Test` 项目中添加演示代码
- 确保 Controller 示例能够正常工作
- 添加相应的 Swagger 文档注释

---

## 📄 许可证

MudFeishu 遵循 [MIT 许可证](LICENSE)。

---

## 🔗 相关链接

- [项目 Gitee 主页](https://gitee.com/mudtools/MudFeishu)
- [项目 Github 主页](https://github.com/mudtools/MudFeishu)
- [NuGet 包](https://www.nuget.org/packages/Mud.Feishu/)
- [文档网站](https://www.mudtools.cn/documents/guides/feishu/)
- [飞书开放平台](https://open.feishu.cn/document/)
- [问题反馈](https://gitee.com/mudtools/MudFeishu/issues)

---

<div align="center">

**Made with ❤️ by MudTools**

如果你觉得这个项目对你有帮助，请给我们一个 ⭐️ Star！

</div>
