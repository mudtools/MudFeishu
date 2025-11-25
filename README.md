# MudFeishu

MudFeishu 是一个用于简化与飞书（Feishu）API 集成的现代化 .NET 库。它基于特性驱动的 HTTP 客户端设计，提供了一套类型安全的接口和强类型化的数据模型，使开发人员能够轻松地在 .NET 应用程序中调用飞书 API。

## 功能特性

- **特性驱动的 HTTP 客户端**：使用 `[HttpClientApi]` 特性自动生成 HTTP 客户端，简化 API 调用
- **强类型数据模型**：完整的飞书 API 数据模型，包含详细的 XML 文档注释
- **智能令牌管理**：内置自动令牌缓存和刷新机制，支持租户令牌和用户令牌
- **统一的响应处理**：基于 `ApiResult<T>` 的响应包装，简化错误处理
- **依赖注入友好**：提供 `IServiceCollection` 扩展方法，易于集成到现代 .NET 应用
- **多版本 .NET 支持**：支持 .NET 6.0、.NET 7.0、.NET 8.0、.NET 9.0、.NET 10.0，使用最新的 C# 13.0 语言特性
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

#### 方式一：使用配置文件
```csharp
using Mud.Feishu;

var builder = WebApplication.CreateBuilder(args);

// 注册飞书 API 服务，从配置文件读取
builder.Services.AddFeishuApiService(builder.Configuration);

var app = builder.Build();
```

#### 方式二：使用配置节名称
```csharp
// 注册飞书 API 服务，指定配置节
builder.Services.AddFeishuApiService("Feishu");
```

##### 配置文件示例 (appsettings.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Feishu": {
    "AppId": "your_app_id",
    "AppSecret": "your_app_secret",
    "BaseUrl": "https://open.feishu.cn"
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
    private readonly IFeishuV3AuthenticationApi _authApi;
    private readonly IFeishuTenantV3User _userApi;
    private readonly IFeishuTenantV3Departments _departmentsApi;
    private readonly IFeishuTenantV3UserGroup _userGroupApi;
    private readonly IFeishuTenantV3EmployeeType _employeeTypeApi;
    private readonly IFeishuTenantV3JobLevel _jobLevelApi;
    private readonly IFeishuTenantV3JobFamilies _jobFamiliesApi;

    public FeishuController(
        IFeishuV3AuthenticationApi authApi, 
        IFeishuTenantV3User userApi,
        IFeishuTenantV3Departments departmentsApi,
        IFeishuTenantV3UserGroup userGroupApi,
        IFeishuTenantV3EmployeeType employeeTypeApi,
        IFeishuTenantV3JobLevel jobLevelApi,
        IFeishuTenantV3JobFamilies jobFamiliesApi)
    {
        _authApi = authApi;
        _userApi = userApi;
        _departmentsApi = departmentsApi;
        _userGroupApi = userGroupApi;
        _employeeTypeApi = employeeTypeApi;
        _jobLevelApi = jobLevelApi;
        _jobFamiliesApi = jobFamiliesApi;
    }
}
```

## API 接口概览

Mud.Feishu 提供了完整的飞书 API 覆盖，主要包含以下接口类别：

### 🔐 认证授权
- 自动令牌管理和刷新
- OAuth 授权流程支持
- 多种令牌类型（应用、租户、用户）

### 📱 消息服务  
- 丰富的消息类型支持（文本、图片、文件、卡片等）
- 批量消息发送和状态追踪
- 消息表情回复和互动功能

### 👥 组织架构管理
- **用户管理**：创建、更新、查询、删除用户
- **部门管理**：部门树形结构维护
- **用户组管理**：用户组成员管理
- **员工管理**：V1 版本员工相关功能

### 🏢 企业管理
- **人员类型**：员工分类管理
- **职级管理**：职级体系维护
- **职位序列**：职业发展路径
- **职务管理**：具体职位定义
- **角色管理**：权限角色体系
- **单位管理**：组织单位设置
- **工作城市**：办公地点管理

> 💡 **提示**：所有接口都基于特性驱动设计，具有强类型支持和完整的数据模型。详细的方法说明请参考项目源码中的 XML 文档注释。

## 使用示例

### 🚀 快速上手

 Mud.Feishu 提供了两种主要的使用方式：

#### 自动令牌管理（推荐）

使用带 `[HttpClientApi]` 特性的接口，令牌自动管理：

```csharp
// 在 Controller 中注入服务
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

### 📋 完整业务场景示例

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

### 🔧 配置文件示例

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

#### Program.cs 服务注册
```csharp
using Mud.Feishu;

var builder = WebApplication.CreateBuilder(args);

// 注册飞书 API 服务（推荐方式）
builder.Services.AddFeishuApiService(builder.Configuration);

// 或者指定配置节名称
// builder.Services.AddFeishuApiService("Feishu");

// 添加自定义令牌管理器（可选）
// builder.Services.AddSingleton<IUserTokenManager, CustomTokenManager>();

var app = builder.Build();

// 配置 Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();
```

## 🎯 常见使用场景快速参考

### 📧 消息通知
```csharp
// 发送文本消息
await messageApi.SendTextMessageAsync(new TextMessageRequest 
{
    ReceiveIdType = "user_id",
    ReceiveId = "user_123",
    Content = new TextContent { Text = "Hello World!" }
});

// 批量发送通知
await batchMessageApi.BatchSendTextMessageAsync(new BatchSenderTextMessageRequest
{
    DeptIds = new[] { "dept_1", "dept_2" },
    Content = new TextContent { Text = "系统通知：重要更新已发布" }
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

## 📦 示例项目

项目包含完整的测试演示项目 `Mud.Feishu.Test`，展示了所有 API 的实际使用方式：

- **Controllers/** - 各种使用场景的控制器示例
- **配置文件** - 完整的配置示例
- **错误处理** - 最佳实践演示
- **集成测试** - 端到端测试用例

运行示例项目：
```bash
cd Mud.Feishu.Test
dotnet run
```

访问 Swagger UI: `http://localhost:5000/swagger`

## 项目结构

```
Mud.Feishu/
├── IFeishuV3AuthenticationApi.cs         # 认证授权 API
├── Organization/                         # 组织架构相关服务
│   ├── IFeishuV1Departments.cs           # V1部门管理基础接口
│   ├── IFeishuTenantV1Departments.cs     # V1租户部门管理接口
│   ├── IFeishuUserV1Departments.cs       # V1用户部门管理接口
│   ├── IFeishuV1Employees.cs             # V1员工管理基础接口
│   ├── IFeishuTenantV1Employees.cs      # V1租户员工管理接口
│   ├── IFeishuUserV1Employees.cs        # V1用户员工管理接口
│   ├── IFeishuV3Departments.cs           # V3部门管理基础接口
│   ├── IFeishuTenantV3Departments.cs     # V3租户部门管理接口
│   ├── IFeishuUserV3Departments.cs       # V3用户部门管理接口
│   ├── IFeishuTenantV3EmployeeType.cs   # V3租户人员类型管理接口
│   ├── IFeishuTenantV3JobFamilies.cs    # V3租户职位序列管理接口
│   ├── IFeishuTenantV3JobLevel.cs       # V3租户职级管理接口
│   ├── IFeishuV3JobTitle.cs              # V3职务管理基础接口
│   ├── IFeishuTenantV3JobTitle.cs       # V3租户职务管理接口
│   ├── IFeishuUserV3JobTitle.cs         # V3用户职务管理接口
│   ├── IFeishuTenantV3RoleMember.cs     # V3租户角色成员管理接口
│   ├── IFeishuTenantV3Role.cs           # V3租户角色管理接口
│   ├── IFeishuTenantV3Unit.cs           # V3租户单位管理接口
│   ├── IFeishuV3User.cs                  # V3用户管理基础接口
│   ├── IFeishuTenantV3UserGroupMember.cs # V3租户用户组成员管理接口
│   ├── IFeishuTenantV3UserGroup.cs      # V3租户用户组管理接口
│   ├── IFeishuTenantV3User.cs           # V3租户用户管理接口
│   ├── IFeishuUserV3User.cs             # V3用户管理接口
│   ├── IFeishuV3WorkCity.cs              # V3工作城市基础接口
│   ├── IFeishuTenantV3WorkCity.cs       # V3租户工作城市管理接口
│   └── IFeishuUserV3WorkCity.cs         # V3用户工作城市管理接口
├── Messages/                              # 消息相关服务
│   ├── IFeishuTenantV1BatchMessage.cs   # V1租户批量消息接口
│   ├── IFeishuV1Message.cs                # V1消息基础接口
│   ├── IFeishuTenantV1Message.cs        # V1租户消息接口
│   ├── IFeishuUserV1Message.cs          # V1用户消息接口
│   └── Imps/
│       └── FeishuV1MessageApi.cs         # V1消息API实现
├── TokenManager/                          # 令牌管理
│   ├── IAppTokenManager.cs               # 应用令牌管理器接口
│   ├── ITenantTokenManager.cs            # 租户令牌管理器接口
│   ├── ITokenManager.cs                  # 令牌管理器基础接口
│   ├── IUserTokenManager.cs              # 用户令牌管理器接口
│   ├── AppTokenManager.cs                # 应用令牌管理器实现
│   ├── TenantTokenManager.cs             # 租户令牌管理器实现
│   ├── UserTokenManager.cs               # 用户令牌管理器实现
│   └── TokenManagerWithCache.cs          # 带缓存的令牌管理器实现
├── Extensions/                            # 扩展组件
│   ├── FeishuOptions.cs                  # 配置选项
│   └── FeishuServiceCollectionExtensions.cs # 服务注册扩展
├── Exceptions/
│   └── FeishuException.cs               # 飞书异常类
├── GlobalUsings.cs                      # 全局引用
└── DataModels/                           # 数据模型
    ├── Common/                           # 通用数据模型
    ├── Departments_v1/                   # V1部门相关数据模型
    │   ├── RequestModel/                 # 请求模型
    │   └── ResponseModel/                # 响应模型
    ├── Departments_v3/                   # V3部门相关数据模型
    │   ├── RequestModel/                 # 请求模型
    │   ├── ResponseModel/                # 响应模型
    │   └── Common/                       # 通用模型
    ├── Employees/                        # 员工相关数据模型
    │   ├── RequestModel/                 # 请求模型
    │   ├── ResponseModel/                # 响应模型
    │   └── Common/                       # 通用模型
    ├── EmployeeType/                     # 人员类型相关数据模型
    ├── JobFamilies/                      # 职位序列相关数据模型
    ├── JobLevel/                         # 职级相关数据模型
    ├── JobTitles/                        # 职务相关数据模型
    ├── Messages/                         # 消息相关数据模型
    │   ├── RequestModel/                 # 请求模型
    │   └── ResponseModel/                # 响应模型
    ├── RoleMembers/                      # 角色成员相关数据模型
    ├── Roles/                            # 角色相关数据模型
    ├── Units/                            # 单位相关数据模型
    ├── UserGroup/                        # 用户组相关数据模型
    ├── UserGroupMember/                   # 用户组成员相关数据模型
    ├── Users/                            # 用户相关数据模型
    ├── WorkCites/                        # 工作城市相关数据模型
    ├── WsEndpoint/                       # WebSocket端点相关数据模型
    ├── AppCredentials.cs                 # 应用凭证
    ├── AppCredentialResult.cs            # 应用凭证结果
    ├── FeishuApiResult.cs                # 飞书API响应基础结果
    ├── OAuthCredentialsResult.cs        # OAuth 凭证结果
    └── TenantAppCredentialResult.cs      # 租户应用凭证结果
```

## 核心组件

### 令牌管理器体系

#### 基础令牌管理接口 (`ITokenManager`)
- **统一接口**：定义令牌获取的基础方法 `GetTokenAsync()`
- **异步操作**：所有方法均为异步，支持取消令牌
- **可扩展性**：支持自定义令牌管理实现
- **缓存管理**：提供 `CleanExpiredTokens()` 和 `GetCacheStatistics()` 方法

#### 应用令牌管理器 (`IAppTokenManager`)
- **应用级令牌**：管理应用访问令牌
- **自动刷新**：令牌即将过期时自动刷新
- **缓存机制**：内置缓存，减少API调用次数

#### 租户令牌管理器 (`ITenantTokenManager`)
- **租户级令牌**：管理租户访问令牌
- **多租户支持**：支持多租户场景下的令牌管理
- **隔离性**：不同租户的令牌完全隔离

#### 用户令牌管理器 (`IUserTokenManager`)
- **用户级令牌**：管理用户访问令牌
- **OAuth支持**：支持OAuth授权流程
- **刷新令牌**：支持通过刷新令牌获取新的访问令牌

#### 带缓存的令牌管理器 (`TokenManagerWithCache`)
- **智能缓存**：自动管理令牌缓存，支持多租户场景
- **自动刷新**：令牌即将过期时自动刷新，提前5分钟触发
- **并发安全**：使用 `ConcurrentDictionary` 和 `Lazy<Task>` 解决缓存击穿和竞态条件
- **重试机制**：内置重试逻辑，最多重试2次，提高系统稳定性
- **性能监控**：提供缓存统计信息，便于监控和调试
- **异常处理**：统一的异常处理和日志记录
- **资源管理**：实现 `IDisposable` 接口，确保资源正确释放

### 配置选项 (`FeishuOptions`)
```csharp
public class FeishuOptions
{
    public required string AppId { get; set; }     // 飞书应用ID
    public required string AppSecret { get; set; } // 飞书应用秘钥
}
```

### 异常处理 (`FeishuException`)
```csharp
public class FeishuException : Exception
{
    public int ErrorCode { get; set; }  // 飞书错误码
    // 支持多种构造函数，便于异常处理
}
```

## 技术栈

- **.NET 6.0/7.0/8.0/9.0/10.0** - 目标框架，使用 C# 13.0
- **Mud.ServiceCodeGenerator v1.2.5** - HTTP 客户端代码生成器
- **System.Text.Json** - 高性能 JSON 序列化
- **Microsoft.Extensions.DependencyInjection** - 依赖注入支持
- **Microsoft.Extensions.Http** - HTTP 客户端工厂
- **Microsoft.Extensions.Logging** - 日志记录支持

## 开发环境要求

- **Visual Studio Code 1.106** 或更高版本
- **.NET 8.0 SDK** 或更高版本
- **飞书开发者账号**和应用凭证
- **Git** 版本控制

## 高级用法

### 自定义令牌管理
```csharp
// 自定义令牌管理器实现
public class CustomTokenManager : ITokenManager
{
    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        // 自定义令牌获取逻辑
        return await GetCustomTokenAsync(cancellationToken);
    }
}

// 注册自定义令牌管理器
services.AddSingleton<ITokenManager, CustomTokenManager>();
```

### 手动令牌刷新
```csharp
// 注入令牌管理器
public class MyService
{
    private readonly ITokenManager _tokenManager;
    
    public MyService(ITokenManager tokenManager)
    {
        _tokenManager = tokenManager;
    }
    
    public async Task<string> GetValidToken()
    {
        return await _tokenManager.GetTokenAsync();
    }
}
```

### 监控和调试
```csharp
// 获取令牌缓存统计（TokenManagerWithCache）
var (total, expired) = _tokenManager.GetCacheStatistics();
 _logger.LogInformation("Token cache: {Total} total, {Expired} expired", total, expired);
    
// 清理过期令牌
cachedManager.CleanExpiredTokens();
```

## 性能优化建议

1. **令牌缓存**：内置的令牌缓存机制自动处理，无需额外配置
2. **HTTP 连接池**：使用 `HttpClientFactory` 自动管理连接池
3. **异步编程**：所有 API 都是异步的，确保高并发性能
4. **配置验证**：启动时自动验证配置，避免运行时错误

## 常见问题

### Q: 如何处理令牌过期？
A: 库内置了自动令牌刷新机制，会在令牌过期前自动获取新令牌，无需手动处理。

### Q: 支持哪些 .NET 版本？
A: 支持 .NET  6.0、7.0、8.0、9.0、10.0，推荐使用 LTS 8.0及以上版本。

### Q: 如何配置多个飞书应用？
A: 可以注册多个服务实例，每个实例使用不同的配置节名称。

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

## 许可证

MudFeishu 遵循 [MIT 许可证](LICENSE)。

## 相关链接

- [飞书开放平台文档](https://open.feishu.cn/document/)
- [.NET 官方文档](https://docs.microsoft.com/dotnet/)
- [NuGet 包管理器](https://www.nuget.org/)
- [Mud.ServiceCodeGenerator](https://gitee.com/mudtools/mud-code-generator)

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
- 所亦接口必须指定飞书API原始文档URL

### 测试要求
- 新功能必须在 `Mud.Feishu.Test` 项目中添加演示代码
- 确保 Controller 示例能够正常工作
- 添加相应的 Swagger 文档注释
