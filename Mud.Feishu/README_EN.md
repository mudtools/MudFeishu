# MudFeishu

MudFeishu is a modern .NET library for simplified integration with the Feishu (Lark) API. Built on an attribute-driven HTTP client design, it provides a type-safe interface and strongly-typed data models, enabling developers to easily call Feishu APIs in .NET applications.

## Features

- **Attribute-driven HTTP Client**: Automatically generate HTTP clients using the `[HttpClientApi]` attribute, simplifying API calls
- **Strongly-typed Data Models**: Complete Feishu API data models with detailed XML documentation comments
- **Intelligent Token Management**: Built-in automatic token caching and refresh mechanism, supporting tenant tokens and user tokens
- **Unified Response Handling**: Response wrapping based on `FeishuApiResult<T>` for simplified error handling
- **Dependency Injection Friendly**: Provides `IServiceCollection` extension methods for easy integration with modern .NET applications
- **Multi-version .NET Support**: Supports .NET Framework 4.6+, .NET 6.0, .NET 7.0, .NET 8.0, .NET 9.0, .NET 10.0, using the latest C# 13.0 language features
- **Complete Feishu API Coverage**: Supports authentication, user management, department management, user group management, personnel type management, job level management, job family management, role management, unit management, position management, work city management
- **High-performance Caching Mechanism**: Resolves cache stampede and race conditions, supports automatic token refresh
- **Enterprise-grade Error Handling**: Unified exception handling and logging

## Supported .NET Versions

- **.NET 6.0** - LTS Long-term support version
- **.NET 7.0** - Stable version
- **.NET 8.0** - LTS Long-term support version
- **.NET 9.0** - Stable version
- **.NET 10.0** - LTS Long-term support version

## Comparison with Native Feishu SDK

The following table clearly demonstrates the advantages of Mud.Feishu components over the native SDK:

| Comparison Dimension | Native SDK Call | Mud.Feishu Components | Advantage Description |
|---------------------|-----------------|----------------------|---------------------|
| Development Efficiency | Requires manually constructing HTTP requests, handling responses, parsing JSON, and other boilerplate code | Just call concise interface methods, complete operations with one line of code | Significantly reduce code volume, improve development efficiency |
| Type Safety | Manual JSON serialization/deserialization, prone to type conversion errors | Provides complete strong-typing support, discover type errors at compile time | Improve code robustness, reduce runtime errors |
| Token Management | Need to manually obtain, refresh, and manage access tokens | Automatically handles token acquisition and refresh mechanism | Reduce developer burden, avoid token management errors |
| Exception Handling | Need to manually handle various network exceptions and business exceptions | Provides unified exception handling mechanism and clear exception types | Simplify exception handling logic, improve code readability |
| Retry Mechanism | Need to manually implement retry logic | Built-in intelligent retry mechanism, automatically handles network jitter and other issues | Improve system stability |
| Testability | Directly calling HTTP interfaces, difficult to unit test | Interface-based design, easy to mock test | Improve code quality and maintainability |
| Documentation Completeness | Need to look up detailed documentation of each interface in Feishu official documentation | Provides complete Chinese API documentation and example code | Reduce learning cost, quick start |
| Dependency Management | Need to manually introduce and manage various third-party libraries | Unified management of all dependencies, avoid version conflicts | Simplify project dependency management |

## Quick Start

### Installation

You can install MudFeishu via NuGet:

```bash
dotnet add package Mud.Feishu --version 1.0.2
```

### Configure Dependency Injection (ASP.NET Core)

Register services in `Program.cs`:

#### 🚀 One-click Complete Registration (Recommended for Beginners)

```csharp
using Mud.Feishu;

var builder = WebApplication.CreateBuilder(args);

// Register all Feishu API services with one line of code (Lazy mode)
builder.Services.AddFeishuServices(builder.Configuration);

// Register services flexibly as needed (Builder pattern)
builder.Services.CreateFeishuServicesBuilder(builder.Configuration)
    .AddTokenManagers()                   // Token management
    .AddOrganizationApi()                 // Organization structure
    .AddMessageApi()                      // Message service
    .AddChatGroupApi()                    // Group service
    .Build();

// Quick single module registration
builder.Services.AddFeishuTokenManagers(builder.Configuration);     // Token management
builder.Services.CreateFeishuServicesBuilder(builder.Configuration)
    .AddOrganizationApi()                 // Organization structure
    .AddMessageApi()                      // Message service
    .AddChatGroupApi()                    // Group service
    .AddApprovalApi()                     // Approval service
    .AddTaskApi()                         // Task service
    .AddCardApi()                         // Card service
    .Build();

// Modular registration
builder.Services.AddFeishuServices(builder.Configuration, new[]
{
    FeishuModule.TokenManagement,
    FeishuModule.Organization,
    FeishuModule.Message,
    FeishuModule.ChatGroup
});

var app = builder.Build();
```

#### 🔧 Builder Pattern (Recommended for Advanced Users)

```csharp
// Register services flexibly as needed (using configuration file)
builder.Services.CreateFeishuServicesBuilder(builder.Configuration)
    .AddTokenManagers()                   // Token management
    .AddOrganizationApi()                 // Organization structure
    .AddMessageApi()                      // Message service
    .Build();

// Register services flexibly as needed (using code configuration)
builder.Services.CreateFeishuServicesBuilder(options =>
{
    options.AppId = "your_app_id";
    options.AppSecret = "your_app_secret";
    options.BaseUrl = "https://open.feishu.cn";
})
    .AddTokenManagers()                   // Token management
    .AddOrganizationApi()                 // Organization structure
    .AddMessageApi()                      // Message service
    .Build();
    .AddTokenManagers()                   // Token management
    .AddOrganizationApi()                 // Organization structure
    .AddMessageApi()                      // Message service
    .Build();
```

#### ⚡ Quick Single Module Registration

```csharp
// Register only the services you need
builder.Services.CreateFeishuServicesBuilder(builder.Configuration)
    .AddOrganizationApi()                 // Organization structure
    .AddMessageApi()                      // Message service
    .AddTokenManagers()                   // Token management
    .Build();
```

#### 📦 Modular Registration

```csharp
builder.Services.AddFeishuModules(builder.Configuration, new[]
{
    FeishuModule.TokenManagement,
    FeishuModule.Organization,
    FeishuModule.Message
});
```

### Controller Injection Example

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

## Usage Examples

### 🚀 Quick Start

Mud.Feishu provides two main usage methods:

#### Automatic Token Management (Recommended)

Use interfaces with `[HttpClientApi]` attribute for automatic token management:

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
        // Token automatically handled, no need to manually obtain
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

### 📋 Business Scenario Examples

#### Scenario 1: User Lifecycle Management

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

    // Create new employee and add to specified department and user groups
    public async Task<string> OnboardNewEmployeeAsync(CreateUserRequest userRequest, string departmentId, string[] groupIds)
    {
        try
        {
            // 1. Create user
            var userResult = await _userApi.CreateUserAsync(userRequest);
            if (userResult.Code != 0)
                throw new Exception($"Failed to create user: {userResult.Msg}");

            var userId = userResult.Data!.User!.UserId;

            // 2. Get department information for verification
            var deptResult = await _deptApi.GetDepartmentInfoByIdAsync(departmentId);
            if (deptResult.Code != 0)
                throw new Exception($"Department does not exist: {deptResult.Msg}");

            // 3. Add user to user groups
            foreach (var groupId in groupIds)
            {
                var addMemberResult = await _groupApi.AddUserGroupMemberAsync(new AddUserGroupMemberRequest
                {
                    UserGroupId = groupId,
                    UserIds = new[] { userId }
                });
                
                if (addMemberResult.Code != 0)
                {
                    // Log warning but don't interrupt the process
                    Console.WriteLine($"Failed to add to user group {groupId}: {addMemberResult.Msg}");
                }
            }

            return userId;
        }
        catch (FeishuException ex)
        {
            // Log Feishu API error
            throw new Exception($"Feishu API call failed (Error code: {ex.ErrorCode}): {ex.Message}");
        }
    }
}
```

#### Scenario 2: Batch Message Sending

```csharp
public class NotificationService
{
    private readonly IFeishuTenantV1BatchMessage _batchMessageApi;

    public NotificationService(IFeishuTenantV1BatchMessage batchMessageApi)
    {
        _batchMessageApi = batchMessageApi;
    }

    // Send system notification to multiple departments
    public async Task<string> SendSystemNotificationAsync(string[] departmentIds, string title, string content)
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
            Console.WriteLine($"Batch message sent successfully, task ID: {messageId}");
            
            // Can asynchronously query sending progress
            _ = Task.Run(async () => await MonitorProgressAsync(messageId));
            
            return messageId;
        }
        
        throw new Exception($"Failed to send: {result.Msg}");
    }

    private async Task MonitorProgressAsync(string messageId)
    {
        var delay = TimeSpan.FromSeconds(5);
        var maxAttempts = 20; // Maximum wait 100 seconds
        
        for (int i = 0; i < maxAttempts; i++)
        {
            var progress = await _batchMessageApi.GetBatchMessageProgressAsync(messageId);
            
            if (progress.Code == 0)
            {
                var progressData = progress.Data!;
                Console.WriteLine($"Sending progress: {progressData.SentCount}/{progressData.TotalCount}");
                
                if (progressData.IsFinished)
                {
                    Console.WriteLine($"Sending completed! Success: {progressData.SentCount}, Failed: {progressData.FailedCount}");
                    break;
                }
            }
            
            await Task.Delay(delay);
        }
    }
}
```

#### Scenario 3: Organization Structure Synchronization

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

    // Synchronize organization structure data to local system
    public async Task SyncOrganizationAsync()
    {
        try
        {
            // 1. Get root department
            var rootDeptResult = await _deptApi.GetDepartmentsByParentIdAsync("0");
            if (rootDeptResult.Code != 0)
                throw new Exception($"Failed to get root department: {rootDeptResult.Msg}");

            var allDepartments = new List<DepartmentInfo>();
            var allUsers = new List<UserInfo>();

            // 2. Recursively get all departments
            foreach (var rootDept in rootDeptResult.Data!.Items!)
            {
                await LoadDepartmentTreeAsync(rootDept.DepartmentId!, allDepartments);
            }

            // 3. Get all users
            foreach (var dept in allDepartments)
            {
                var usersResult = await _userApi.GetUserByDepartmentIdAsync(dept.DepartmentId!);
                if (usersResult.Code == 0 && usersResult.Data?.Items != null)
                {
                    allUsers.AddRange(usersResult.Data.Items);
                }
            }

            Console.WriteLine($"Synchronization completed: {allDepartments.Count} departments, {allUsers.Count} users");
            
            // TODO: Save to database
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Organization structure synchronization failed: {ex.Message}");
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

## 🎯 Quick Reference for Common Operations

### 📧 Message Notifications

```csharp
// Send text message
var textContent = new MessageTextContent { Text = "Hello World!" };
await messageApi.SendMessageAsync(new SendMessageRequest
{
    ReceiveId = "user_123",
    MsgType = "text",
    Content = JsonSerializer.Serialize(textContent)
}, receive_id_type: "user_id");

// Send batch notifications
var batchContent = new MessageTextContent { Text = "System notification: Important update released" };
await batchMessageApi.BatchSendTextMessageAsync(new BatchSenderTextMessageRequest
{
    DeptIds = new[] { "dept_1", "dept_2" },
    Content = batchContent
});
```

### 👤 User Management

```csharp
// Create user
var userResult = await userApi.CreateUserAsync(new CreateUserRequest
{
    Name = "Zhang San",
    Mobile = "13800138000",
    DepartmentIds = new[] { "dept_1" },
    Emails = new[] { new EmailValue { Email = "zhangsan@company.com" } }
});

// Batch get user information
var users = await userApi.GetUserByIdsAsync(new[] { "user_1", "user_2", "user_3" });
```

### 🏢 Organization Structure

```csharp
// Get department tree
var departments = await deptApi.GetDepartmentsByParentIdAsync("0", fetch_child: true);

// Get users under department
var users = await deptApi.GetUserByDepartmentIdAsync("dept_123");

// Create sub-department
var newDept = await deptApi.CreateDepartmentAsync(new DepartmentCreateRequest
{
    Name = "New Department",
    ParentDepartmentId = "parent_dept_123"
});
```

### 🛠️ Token Management

```csharp
// Get valid token directly (automatically handles refresh)
var token = await tokenManager.GetTokenAsync();

// Monitor token cache status
var (total, expired) = tokenManager.GetCacheStatistics();
logger.LogInformation("Token cache status: Total {Total}, Expired {Expired}", total, expired);

// Clean expired tokens
tokenManager.CleanExpiredTokens();
```

### 🔧 Complete Configuration Example

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

#### Program.cs Complete Configuration

```csharp
using Mud.Feishu;

var builder = WebApplication.CreateBuilder(args);

// Choose registration method
builder.Services.AddFeishuServices(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
```

## 🔄 Error Handling Best Practices

### Unified Error Handling

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
                    $"Feishu API call failed: {operationName}",
                    result.Code,
                    result.Msg);
            }
            
            return result.Data!;
        }
        catch (FeishuException ex)
        {
            // Feishu API error
            logger.LogError(ex, "Feishu API error (code: {ErrorCode}): {Message}", ex.ErrorCode, ex.Message);
            throw;
        }
        catch (HttpRequestException ex)
        {
            // Network error
            logger.LogError(ex, "Network request failed: {Message}", ex.Message);
            throw new FeishuServiceException($"Network connection failed: {operationName}", -1, ex.Message);
        }
    }
}

// Usage example
public async Task<UserInfo> GetUserSafelyAsync(string userId)
{
    return await ExecuteWithErrorHandling(
        () => userApi.GetUserInfoByIdAsync(userId),
        "Get user information");
}
```

### Pagination Handling

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

// Usage example
var allUsers = await GetAllItemsAsync(pageToken => 
    userApi.GetUserByDepartmentIdAsync("dept_123", page_size: 50, page_token: pageToken));
```

## Contributing Guidelines

We welcome community contributions! Please follow these guidelines:

1. **Fork the project** and create a feature branch
2. **Write code** and add corresponding unit tests
3. **Ensure code quality**: Follow project coding standards, code coverage not less than 80%
4. **Submit Pull Request**: Describe changes and test results in detail

### Code Standards

- Use C# 13.0 language features
- Follow Microsoft coding standards
- All public APIs must include XML documentation comments
- Async method naming should end with `Async`
- All interfaces must specify Feishu API original documentation URL

### Testing Requirements

- New features must add demo code in the `Mud.Feishu.Test` project
- Ensure Controller examples work properly
- Add corresponding Swagger documentation comments

## License

MudFeishu follows the [MIT License](LICENSE).

## Related Links

- [Project Gitee Homepage](https://gitee.com/mudtools/MudFeishu)
- [Project GitHub Homepage](https://github.com/mudtools/MudFeishu)
- [NuGet Package](https://www.nuget.org/packages/Mud.Feishu/)
- [Documentation Site](https://www.mudtools.cn/documents/guides/feishu/)
- [Feishu Open Platform](https://open.feishu.cn/document/)
- [Issue Tracker](https://gitee.com/mudtools/MudFeishu/issues)
