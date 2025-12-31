# MudFeishu

MudFeishu 是一个用于简化与飞书（Feishu）API 集成的现代化 .NET 库。它基于特性驱动的 HTTP 客户端设计，提供了一套类型安全的接口和强类型化的数据模型，使开发人员能够轻松地在 .NET 应用程序中调用飞书 API。

## 功能特性

- **特性驱动的 HTTP 客户端**：使用 `[HttpClientApi]` 特性自动生成 HTTP 客户端，简化 API 调用
- **强类型数据模型**：完整的飞书 API 数据模型，包含详细的 XML 文档注释
- **智能令牌管理**：内置自动令牌缓存和刷新机制，支持租户令牌和用户令牌
- **统一的响应处理**：基于 `FeishuApiResult<T>` 的响应包装，简化错误处理
- **依赖注入友好**：提供 `IServiceCollection` 扩展方法，易于集成到现代 .NET 应用
- **多版本 .NET 支持**：支持.NET4.6+、.NET 6.0、.NET 7.0、.NET 8.0、.NET 9.0、.NET 10.0，使用最新的 C# 13.0 语言特性
- **完整的飞书 API 覆盖**：支持认证、用户管理、部门管理、用户组管理、人员类型管理、职级管理、职位序列管理、角色管理、单位管理、职务管理、工作城市管理
- **高性能缓存机制**：解决缓存击穿和竞态条件，支持令牌自动刷新
- **企业级错误处理**：统一的异常处理和日志记录

## 支持的 .NET 版本

- **.NET 6.0** - LTS 长期支持版本
- **.NET 7.0** - 稳定版本
- **.NET 8.0** - LTS 长期支持版本
- **.NET 9.0** - 稳定版本  
- **.NET 10.0** - LTS 长期支持版本

## 与原生飞书SDK的对比分析

以下表格清晰地展示Mud.Feishu组件相对于原生SDK的优势：

| 对比维度 | 原生SDK调用 | Mud.Feishu组件 | 优势说明 |
|---------|------------|---------------|----------|
| 开发效率 | 需要手动构造HTTP请求、处理响应、解析JSON等大量样板代码 | 只需调用简洁的接口方法，一行代码完成操作 | 大幅减少代码量，提高开发效率 |
| 类型安全 | 手动处理JSON序列化/反序列化，容易出现类型转换错误 | 提供完整的强类型支持，编译时就能发现类型错误 | 提高代码健壮性，减少运行时错误 |
| 令牌管理 | 需要手动获取、刷新和管理访问令牌 | 自动处理令牌获取和刷新机制 | 减少开发者负担，避免令牌管理错误 |
| 异常处理 | 需要手动处理各种网络异常和业务异常 | 提供统一的异常处理机制和明确的异常类型 | 简化异常处理逻辑，提高代码可读性 |
| 重试机制 | 需要手动实现重试逻辑 | 内置智能重试机制，自动处理网络抖动等问题 | 提高系统稳定性 |
| 可测试性 | 直接调用HTTP接口，难以进行单元测试 | 基于接口设计，易于进行Mock测试 | 提高代码质量和可维护性 |
| 文档完善度 | 需要在飞书官方文档中查找各个接口的详细说明 | 提供完整的中文API文档和示例代码 | 降低学习成本，快速上手 |
| 依赖管理 | 需要自行引入和管理各种第三方库 | 统一管理所有依赖，避免版本冲突 | 简化项目依赖管理 |

## 快速开始

### 安装

你可以通过 NuGet 安装 MudFeishu：

```bash
dotnet add package Mud.Feishu --version 1.0.2
```

### 配置依赖注入（ASP.NET Core）

在 `Program.cs` 中注册服务：

#### 🚀 一键完整注册（推荐新手）

```csharp
using Mud.Feishu;

var builder = WebApplication.CreateBuilder(args);

// 一行代码注册所有飞书 API 服务
builder.Services.AddFeishuApiService(builder.Configuration);

var app = builder.Build();
```

#### 🔧 构造者模式（推荐高级用户）

```csharp
// 按需灵活注册服务
builder.Services.AddFeishuServices()
    .ConfigureFrom(builder.Configuration)
    .AddTokenManagers()                   // 令牌管理
    .AddOrganizationApi()                 // 组织架构
    .AddMessageApi()                      // 消息服务
    .Build();
```

#### ⚡ 快速单模块注册

```csharp
// 只注册需要的服务
builder.Services.AddFeishuOrganizationApi(builder.Configuration);  // 组织架构
builder.Services.AddFeishuMessageApi(builder.Configuration);        // 消息服务
builder.Services.AddFeishuTokenManagers(builder.Configuration);     // 令牌管理
```

#### 📦 模块化注册

```csharp
builder.Services.AddFeishuModules(builder.Configuration, new[]
{
    FeishuModule.TokenManagement,
    FeishuModule.Organization,
    FeishuModule.Message
});
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
    private readonly IFeishuTenantV3UserGroup _userGroupApi;
    private readonly IFeishuTenantV3EmployeeType _employeeTypeApi;
    private readonly IFeishuTenantV3JobLevel _jobLevelApi;
    private readonly IFeishuTenantV3JobFamilies _jobFamiliesApi;
    private readonly IFeishuTenantV1Message _messageApi;

    public FeishuController(
        IFeishuTenantV3User userApi,
        IFeishuTenantV3Departments departmentsApi,
        IFeishuTenantV3UserGroup userGroupApi,
        IFeishuTenantV3EmployeeType employeeTypeApi,
        IFeishuTenantV3JobLevel jobLevelApi,
        IFeishuTenantV3JobFamilies jobFamiliesApi,
        IFeishuTenantV1Message messageApi)
    {
        _userApi = userApi;
        _departmentsApi = departmentsApi;
        _userGroupApi = userGroupApi;
        _employeeTypeApi = employeeTypeApi;
        _jobLevelApi = jobLevelApi;
        _jobFamiliesApi = jobFamiliesApi;
        _messageApi = messageApi;
    }
}
```

## 使用示例

### 🚀 快速上手

 Mud.Feishu 提供了两种主要的使用方式：

#### 自动令牌管理（推荐）

使用带 `[HttpClientApi]` 特性的接口，令牌自动管理：

```csharp
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

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // 令牌自动处理，无需手动获取
        var result = await _userApi.CreateUserAsync(request);

        if (result.Code == 0)
        {
            return Ok(new { success = true, userId = result.Data?.User?.UserId });
        }
        return BadRequest(new { error = result.Msg });
    }

    [HttpGet("departments/{departmentId}/users")]
    public async Task<IActionResult> GetDepartmentUsers(string departmentId)
    {
        var result = await _deptApi.GetUserByDepartmentIdAsync(departmentId);
        return Ok(result.Data);
    }
}
```

### 📋 业务场景实战

#### 场景1：用户全生命周期管理

```csharp
public class UserManagementService
{
    private readonly IFeishuTenantV3User _userApi;
    private readonly IFeishuTenantV3Departments _deptApi;
    private readonly IFeishuTenantV3UserGroup _groupApi;

    public UserManagementService(
        IFeishuTenantV3User userApi,
        IFeishuTenantV3Departments deptApi,
        IFeishuTenantV3UserGroup groupApi)
    {
        _userApi = userApi;
        _deptApi = deptApi;
        _groupApi = groupApi;
    }

    // 创建新员工并加入指定部门和用户组
    public async Task<string> OnboardNewEmployeeAsync(CreateUserRequest userRequest, string departmentId, string[] groupIds)
    {
        try
        {
            // 1. 创建用户
            var userResult = await _userApi.CreateUserAsync(userRequest);
            if (userResult.Code != 0)
                throw new Exception($"创建用户失败: {userResult.Msg}");

            var userId = userResult.Data!.User!.UserId;

            // 2. 获取部门信息用于验证
            var deptResult = await _deptApi.GetDepartmentInfoByIdAsync(departmentId);
            if (deptResult.Code != 0)
                throw new Exception($"部门不存在: {deptResult.Msg}");

            // 3. 将用户加入用户组
            foreach (var groupId in groupIds)
            {
                var addMemberResult = await _groupApi.AddUserGroupMemberAsync(new AddUserGroupMemberRequest
                {
                    UserGroupId = groupId,
                    UserIds = new[] { userId }
                });

                if (addMemberResult.Code != 0)
                {
                    // 记录警告但不中断流程
                    Console.WriteLine($"加入用户组 {groupId} 失败: {addMemberResult.Msg}");
                }
            }

            return userId;
        }
        catch (FeishuException ex)
        {
            // 记录飞书 API 错误
            throw new Exception($"飞书 API 调用失败 (错误码: {ex.ErrorCode}): {ex.Message}");
        }
    }
}
```

#### 场景2：批量消息发送

```csharp
public class NotificationService
{
    private readonly IFeishuTenantV1BatchMessage _batchMessageApi;

    public NotificationService(IFeishuTenantV1BatchMessage batchMessageApi)
    {
        _batchMessageApi = batchMessageApi;
    }

    // 发送系统通知给多个部门
    public async Task<string> SendSystemNotificationAsync(string[] departmentIds, string title, string content)
    {
        var request = new BatchSenderTextMessageRequest
        {
            DeptIds = departmentIds,
            Content = new TextContent
            {
                Text = $"📢 {title}

{content}"
            }
        };

        var result = await _batchMessageApi.BatchSendTextMessageAsync(request);

        if (result.Code == 0)
        {
            var messageId = result.Data!.MessageId;
            Console.WriteLine($"批量消息发送成功，任务ID: {messageId}");

            // 可以异步查询发送进度
            _ = Task.Run(async () => await MonitorProgressAsync(messageId));

            return messageId;
        }

        throw new Exception($"发送失败: {result.Msg}");
    }

    private async Task MonitorProgressAsync(string messageId)
    {
        var delay = TimeSpan.FromSeconds(5);
        var maxAttempts = 20; // 最多等待100秒
        
        for (int i = 0; i < maxAttempts; i++)
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
            
            await Task.Delay(delay);
        }
    }
}
```

#### 场景3：组织架构同步

```csharp
public class OrganizationSyncService
{
    private readonly IFeishuTenantV3Departments _deptApi;
    private readonly IFeishuTenantV3User _userApi;

    public OrganizationSyncService(
        IFeishuTenantV3Departments deptApi,
        IFeishuTenantV3User userApi)
    {
        _deptApi = deptApi;
        _userApi = userApi;
    }

    // 同步组织架构数据到本地系统
    public async Task SyncOrganizationAsync()
    {
        try
        {
            // 1. 获取根部门
            var rootDeptResult = await _deptApi.GetDepartmentsByParentIdAsync("0");
            if (rootDeptResult.Code != 0)
                throw new Exception($"获取根部门失败: {rootDeptResult.Msg}");

            var allDepartments = new List<DepartmentInfo>();
            var allUsers = new List<UserInfo>();

            // 2. 递归获取所有部门
            foreach (var rootDept in rootDeptResult.Data!.Items!)
            {
                await LoadDepartmentTreeAsync(rootDept.DepartmentId!, allDepartments);
            }

            // 3. 获取所有用户
            foreach (var dept in allDepartments)
            {
                var usersResult = await _userApi.GetUserByDepartmentIdAsync(dept.DepartmentId!);
                if (usersResult.Code == 0 && usersResult.Data?.Items != null)
                {
                    allUsers.AddRange(usersResult.Data.Items);
                }
            }

            Console.WriteLine($"同步完成: {allDepartments.Count} 个部门, {allUsers.Count} 个用户");

            // TODO: 保存到数据库
        }
        catch (Exception ex)
        {
            Console.WriteLine($"组织架构同步失败: {ex.Message}");
            throw;
        }
    }

    private async Task LoadDepartmentTreeAsync(string departmentId, List<DepartmentInfo> departments)
    {
        var result = await _deptApi.GetDepartmentsByParentIdAsync(departmentId, fetch_child: true);

        if (result.Code == 0 && result.Data?.Items != null)
        {
            foreach (var dept in result.Data.Items)
            {
                departments.Add(dept);
                await LoadDepartmentTreeAsync(dept.DepartmentId!, departments);
            }
        }
    }
}
```

## 🎯 常见操作快速参考

### 📧 消息通知
```csharp
// 发送文本消息
var textContent = new MessageTextContent { Text = "Hello World!" };
await messageApi.SendMessageAsync(new SendMessageRequest
{
    ReceiveId = "user_123",
    MsgType = "text",
    Content = JsonSerializer.Serialize(textContent)
}, receive_id_type: "user_id");

// 批量发送通知
var batchContent = new MessageTextContent { Text = "系统通知：重要更新已发布" };
await batchMessageApi.BatchSendTextMessageAsync(new BatchSenderTextMessageRequest
{
    DeptIds = new[] { "dept_1", "dept_2" },
    Content = batchContent
});
```

### 👤 用户管理
```csharp
// 创建用户
var userResult = await userApi.CreateUserAsync(new CreateUserRequest
{
    Name = "张三",
    Mobile = "13800138000",
    DepartmentIds = new[] { "dept_1" },
    Emails = new[] { new EmailValue { Email = "zhangsan@company.com" } }
});

// 批量获取用户信息
var users = await userApi.GetUserByIdsAsync(new[] { "user_1", "user_2", "user_3" });
```

### 🏢 组织架构
```csharp
// 获取部门树
var departments = await deptApi.GetDepartmentsByParentIdAsync("0", fetch_child: true);

// 获取部门下的用户
var users = await deptApi.GetUserByDepartmentIdAsync("dept_123");

// 创建子部门
var newDept = await deptApi.CreateDepartmentAsync(new DepartmentCreateRequest
{
    Name = "新部门",
    ParentDepartmentId = "parent_dept_123"
});
```

### 🛠️ 令牌管理
```csharp
// 直接获取有效令牌（自动处理刷新）
var token = await tokenManager.GetTokenAsync();

// 监控令牌缓存状态
var (total, expired) = tokenManager.GetCacheStatistics();
logger.LogInformation("令牌缓存状态: 总数 {Total}, 过期 {Expired}", total, expired);

// 清理过期令牌
tokenManager.CleanExpiredTokens();
```

### 🔧 完整配置示例

#### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Mud.Feishu": "Debug"
    }
  },
  "AllowedHosts": "*",
  "Feishu": {
    "AppId": "your_feishu_app_id",
    "AppSecret": "your_feishu_app_secret",
    "BaseUrl": "https://open.feishu.cn"
  }
}
```

#### Program.cs 完整配置

```csharp
using Mud.Feishu;

var builder = WebApplication.CreateBuilder(args);

// 选择注册方式
builder.Services.AddFeishuApiService(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
```

## 🔄 错误处理最佳实践

### 统一错误处理

```csharp
public class FeishuServiceBase
{
    protected async Task<T> ExecuteWithErrorHandling<T>(Func<Task<T>> operation, string operationName)
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
            logger.LogError(ex, "飞书 API 错误 (代码: {ErrorCode}): {Message}", ex.ErrorCode, ex.Message);
            throw;
        }
        catch (HttpRequestException ex)
        {
            // 网络错误
            logger.LogError(ex, "网络请求失败: {Message}", ex.Message);
            throw new FeishuServiceException($"网络连接失败: {operationName}", -1, ex.Message);
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

### 分页处理

```csharp
public async Task<List<T>> GetAllItemsAsync<T>(Func<string?, Task<FeishuApiPageListResult<T>>> pageFetcher)
{
    var allItems = new List<T>();
    string? pageToken = null;
    const int pageSize = 50;

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

## 贡献指南

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
- 所有接口必须指定飞书API原始文档URL

### 测试要求

- 新功能必须在 `Mud.Feishu.Test` 项目中添加演示代码
- 确保 Controller 示例能够正常工作
- 添加相应的 Swagger 文档注释

## 许可证

MudFeishu 遵循 [MIT 许可证](LICENSE)。

## 相关链接

- [项目Gitee主页](https://gitee.com/mudtools/MudFeishu)
- [项目Github主页](https://github.com/mudtools/MudFeishu)
- [NuGet 包](https://www.nuget.org/packages/Mud.Feishu/)
- [文档网站](https://www.mudtools.cn/documents/guides/feishu/)
- [飞书开放平台](https://open.feishu.cn/document/)
- [问题反馈](https://gitee.com/mudtools/MudFeishu/issues)
