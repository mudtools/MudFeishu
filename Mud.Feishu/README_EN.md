# MudFeishu

<div align="center">

![NuGet](https://img.shields.io/nuget/v/Mud.Feishu)
![NuGet](https://img.shields.io/nuget/dt/Mud.Feishu)
![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%207.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-purple)
![License](https://img.shields.io/badge/license-MIT-blue)

**Modern .NET SDK for Feishu (Lark) API Integration**

Minimal API · Type Safety · Enterprise Features · Event-Driven

[Quick Start](#quick-start) · [Documentation](https://www.mudtools.cn/documents/guides/feishu/) · [Examples](#example-projects) · [Contributing](#contributing-guidelines)

</div>

---

## 📖 Introduction

MudFeishu is a modern enterprise-grade .NET SDK for Feishu (Lark) API integration, providing complete HTTP API calls, WebSocket real-time event subscription, and Webhook event processing capabilities. The SDK is designed using Strategy and Factory patterns with built-in automatic token management, intelligent retry mechanisms, and high-performance caching.

### ✨ Core Features

- **Minimal API** - One-line service registration
- **Type Safety** - Complete strongly-typed data models with compile-time checking
- **Automatic Token Management** - Smart caching and auto-refresh mechanism
- **Enterprise Stability** - Unified exception handling, intelligent retry, high-performance caching
- **Event-Driven Architecture** - WebSocket + Webhook dual-mode support
- **Multi-Framework Support** - .NET Standard 2.0 / .NET 6.0 / .NET 8.0 / .NET 10.0
- **Distributed Support** - Redis distributed deduplication, multi-instance deployment
- **Security Enhancement** - Signature verification, timestamp validation, IP whitelist

---

---

## 🚀 Quick Start

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

var app = builder.Build();
```

#### 🔧 Builder Pattern (Recommended for Advanced Users)

```csharp
// Register services flexibly as needed (using configuration file)
builder.Services.CreateFeishuServicesBuilder(builder.Configuration)
    .AddOrganizationApi()                 // Organization structure
    .AddMessageApi()                      // Message service
    .AddChatGroupApi()                    // Group service
    .AddApprovalApi()                     // Approval service
    .AddTaskApi()                         // Task service
    .AddCardApi()                         // Card service
    .Build();

// Register services flexibly as needed (using code configuration)
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

#### ⚡ Quick Single Module Registration

```csharp
// Register only the services you need
builder.Services.CreateFeishuServicesBuilder(builder.Configuration)
    .AddOrganizationApi()                 // Organization structure
    .AddMessageApi()                      // Message service
    .Build();
```

#### 📦 Modular Registration

```csharp
// Register only services you need
builder.Services.AddFeishuServices(builder.Configuration, new[]
{
    FeishuModule.Organization,      // Organization structure
    FeishuModule.Message,          // Message service
    FeishuModule.ChatGroup         // Group service
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
    "BaseUrl": "https://open.feishu.cn",
    "TimeOut": 30,
    "RetryCount": 3,
    "EnableLogging": true
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

## ⚙️ Configuration Options

### FeishuAppConfig Configuration Items

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `AppKey` | string | - | Application unique identifier (required), used to reference this app in code |
| `AppId` | string | - | Feishu application unique identifier (required) |
| `AppSecret` | string | - | Feishu application secret (required) |
| `BaseUrl` | string | "https://open.feishu.cn" | Feishu API base URL |
| `TimeOut` | int | 30 | HTTP request timeout (seconds), range: 1-300 |
| `RetryCount` | int | 3 | Retry count on failure, range: 0-10 |
| `RetryDelayMs` | int | 1000 | Retry delay (milliseconds), range: 100-60000 |
| `TokenRefreshThreshold` | int | 300 | Token refresh threshold (seconds), range: 60-3600 |
| `EnableLogging` | bool | true | Enable logging |
| `IsDefault` | bool | false | Whether it's the default app (supports auto-inference) |

### Configuration Validation

`FeishuAppConfig` provides a `Validate()` method for validating configuration items:

- `TimeOut` must be between 1-300 seconds
- `RetryCount` must be between 0-10 times
- `RetryDelayMs` must be between 100-60000 milliseconds
- `TokenRefreshThreshold` must be between 60-3600 seconds
- `BaseUrl` must be a valid HTTP/HTTPS URL format
- `AppId` must start with `cli_` or `app_` and be at least 20 characters
- `AppSecret` must be at least 16 characters

### Security Recommendations

- `AppId` and `AppSecret` are the credentials for your Feishu application, please keep them safe
- Recommend using environment variables or secure configuration management systems to store sensitive information
- Do not hardcode sensitive information in your code
- In production environments, recommend using HTTPS protocol to ensure communication security

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

---

## 📋 API Module Details

> This section details the interfaces and their supported methods in each module of the `Mud.Feishu/Interfaces` directory.

### 📄 Document Management (Docx)

Feishu document API interfaces supporting document creation, editing, and block operations. Feishu Open Platform cloud documents are divided into documents and blocks:
- **Document**: An online document created by users in the cloud, each with a unique `document_id`
- **Block**: The smallest building unit in a document, a structured composition element of content, which can be text, spreadsheet, image, or bitable, etc.

#### Document Basic Operations (`IFeishuV1Docx`)

| Method | Description |
|--------|-------------|
| `CreateDocumentAsync` | Create a document of type docx, optionally passing document title and folder |
| `GetDocumentInfoAsync` | Get document basic information including title, owner, creation time, etc. |
| `GetDocumentRawContentAsync` | Get document plain text content, supports specifying @user language |
| `GetDocumentBlocksPageListAsync` | Get rich text content of all blocks in the document with pagination |

#### Document Block Operations (`IFeishuV1DocxBlocks`)

| Method | Description |
|--------|-------------|
| `CreateBlockAsync` | Create a batch of child blocks for a specified block and insert at specified position |
| `CreateDescendantBlockAsync` | Create a batch of child blocks with parent-child relationships in the specified block's child list |
| `UpdateBlockAsync` | Update the content of a specified block |
| `GetBlockInfoAsync` | Get rich text content data of a specified block by block_id |
| `BatchUpdateBlocksAsync` | Batch update rich text content of blocks |
| `GetChildrenBlocksPageListAsync` | Get rich text content of all child blocks of a specified block in the document with pagination |
| `BatchDeleteBlocksAsync` | Delete a specified range of child blocks from a specified block |
| `ContentConvertAsync` | Convert Markdown/HTML format content to document blocks |

---

### 📚 Wiki

Feishu Wiki is a knowledge management system for organizations, structuring high-value information to form a complete knowledge system. Clear content classification and hierarchical page trees can easily improve knowledge circulation and dissemination efficiency.

#### Knowledge Space Management (`IFeishuV2Wiki`)

| Method | Description |
|--------|-------------|
| `GetSpacesPageListAsync` | Get list of knowledge spaces with access permission (does not return "My Documents") |
| `GetSpaceInfoAsync` | Query knowledge space information by space ID, including type, visibility, sharing status, etc. |
| `GetSpaceMemberPageListAsync` | Get knowledge space member list |
| `CreateSpaceMemberAsync` | Add knowledge space members (requires admin permission) |
| `DeleteSpaceMemberAsync` | Delete knowledge space member |
| `UpdateSpaceSettingAsync` | Update knowledge space settings |

---

### ☁️ Drive Management

A collective term for various types of files in cloud storage, including online documents, spreadsheets, bitables, mind notes, wiki documents created in cloud storage, as well as various files uploaded from local environment.

#### File Operations (`IFeishuV1DriveFiles`)

| Method | Description |
|--------|-------------|
| `BatchQueryMetasAsync` | Get file metadata by file token, including title, owner, creation time, classification, access link, etc. |
| `GetFileStatisticsByFileTokenAsync` | Get traffic statistics and interaction information for various files, including readers, reads, and likes |
| `GetFileViewRecordPageListByFileTokenAsync` | Get historical access records for documents, spreadsheets, bitables, etc. |
| `CopyFileByFileTokenAsync` | Copy files from user's cloud storage to other folders (async interface) |
| `MoveFileByFileTokenAsync` | Move files or folders to other locations in user's cloud storage (async interface) |
| `DeleteFileByFileTokenAsync` | Delete files or folders from user's cloud storage (goes to recycle bin) |
| `CreateShortcutAsync` | Create a shortcut to a specified file in other folders in cloud storage |
| `UploadAllFileAsync` | Upload specified file to specified directory in cloud storage (≤20MB) |
| `UploadPrepareFileAsync` | Send initialization request to get upload transaction ID and sharding strategy |
| `UploadPartFileAsync` | Upload corresponding file shards based on upload transaction ID and sharding strategy |
| `UploadFinishFileAsync` | Trigger upload completion after all shards are uploaded |
| `DownloadFileAsync` | Download files from cloud storage (like PDF files), supports shard download |
| `CreateImportTaskAsync` | Create an import file task to import local files as Feishu online cloud documents |
| `GetImportTaskAsync` | Poll import results by import task ID |
| `CreateExportTaskAsync` | Create an export file task to export Feishu documents as local files |
| `GetExportTaskAsync` | Poll export task results by export task ID |
| `DownloadExportFileAsync` | Download exported files to local by export file token |
| `GetFileLikePageListByFileTokenAsync` | Get list of likers for a specified cloud document |

#### Folder Operations (`IFeishuV1DriveFolder`)

| Method | Description |
|--------|-------------|
| `GetDriveRootFolderMetaAsync` | Get metadata of user's "My Space" (root folder) |
| `GetFilesPageListAsync` | Get file list in a folder |
| `GetFolderMetaByTokenAsync` | Get folder metadata by folder token |
| `CreateFolderAsync` | Create an empty folder in a specified folder in user's cloud storage |
| `GetTaskCheckFileAsync` | Query async task status (delete folder and move folder) |

#### Media File Operations (`IFeishuV1DriveMedia`)

| Method | Description |
|--------|-------------|
| `UploadAllMediaAsync` | Upload files, images, videos and other media to specified cloud documents (≤20MB) |
| `UploadPrepareMediaAsync` | Send initialization request to get upload transaction ID and sharding strategy |
| `UploadPartMediaAsync` | Upload corresponding media shards based on upload transaction ID and sharding strategy |
| `UploadFinishMediaAsync` | Trigger upload completion after all media shards are uploaded |
| `DownloadFileAsync` | Download media from various cloud documents (like images in spreadsheets) |
| `BatchGetTmpDownloadUrlAsync` | Get temporary download links for media in cloud documents (valid for 24 hours) |

---

### ⏰ Attendance Management

Complete enterprise attendance management API interfaces, supporting attendance group management, check-in records, attendance statistics, shift management, and more.

#### Attendance Group Management (`IFeishuV1AttendanceGroups`)

Attendance groups are rule settings for the attendance status of departments or employees in specific places and time periods, including arrival/departure, late/early leave, sick leave, marriage leave, bereavement leave, public holidays, working hours, overtime, etc. You can set attendance methods, times, and locations from department and employee dimensions.

| Method | Description |
|--------|-------------|
| `CreateGroupAsync` | Create or modify attendance group |
| `DeleteGroupByIdAsync` | Delete attendance group by group ID |
| `GetGroupByIdAsync` | Get attendance group details by group ID (basic info, shifts, attendance methods, settings) |
| `GetGroupByNameAsync` | Query attendance group summary info by group name (supports exact and fuzzy matching) |
| `GetGroupPageListAsync` | Get list of all attendance groups with pagination |

#### Attendance Statistics (`IFeishuV1AttendanceStats`)

Attendance statistics interface allows developers to customize returned data, so developers can get only the data content they care about.

| Method | Description |
|--------|-------------|
| `UpdateUserStatsViewAsync` | Update customized daily or monthly statistics report header settings |
| `QueryUserStatsFieldAsync` | Query daily or monthly statistics report headers supported by attendance statistics |
| `QueryUserStatsViewAsync` | Query daily or monthly statistics report headers supported by attendance statistics |
| `QueryUserStatsDataAsync` | Query daily or monthly statistics data |

#### Check-in Records Management (`IFeishuV1AttendanceUserFlows`)

Check-in information management, can import, query, and delete employee check-in records.

| Method | Description |
|--------|-------------|
| `BatchCreateUserFlowAsync` | Import employee check-in records (will calculate check-in status and results based on shift rules after import) |
| `GetUserFlowAsync` | Get user's check-in record by check-in record ID |
| `QueryUserFlowAsync` | Batch query check-in records by check-in record IDs |
| `BatchDelUserFlowAsync` | Delete employee check-in records imported from open platform |
| `QueryUserTaskAsync` | Get actual check-in results of employees in the enterprise |

---

### 📋 Approval Workflow

Complete enterprise approval workflow API interfaces, supporting approval definitions, instances, tasks, comments, and more.

#### Approval Definitions and Instances (`IFeishuV4Approval`)

Native approval is used to create approval definitions in Feishu Approval Center based on enterprise business needs, defining the form and process of a type of approval.

| Method | Description |
|--------|-------------|
| `CreateApprovalAsync` | Create approval definition (can flexibly specify basic info, form, and process) |
| `GetApprovalByCodeAsync` | Get approval definition info by approval definition Code (name, status, form controls, nodes, etc.) |
| `CreateInstanceAsync` | Create an approval instance using specified approval definition Code |
| `CancelInstanceAsync` | Withdraw approval instance |
| `CarbonCopyInstanceAsync` | CC current approval instance to specified users |
| `PreviewInstanceAsync` | Preview approval process data before or after creating approval instance |
| `GetInstanceByIdAsync` | Get detailed info of approval instance by approval instance Code |

#### Approval Queries (`IFeishuV4ApprovalQuery`)

Query approval instances, approval CC, and approval task lists that meet conditions through different criteria.

| Method | Description |
|--------|-------------|
| `GetInstancesPageListAsync` | Query approval instances list in the approval system that meet conditions |
| `GetCarbonCopyPageListAsync` | Query approval CC list in the approval system that meet conditions |
| `GetTasksPageListAsync` | Query approval task list in the approval system that meet conditions |

#### Approval Task Management (`IFeishuV4ApprovalTask`)

Approval instance process contains multiple approval nodes, and approval tasks are generated within approval nodes. You can agree, reject, transfer, and rollback approval tasks.

| Method | Description |
|--------|-------------|
| `AgreeApprovalAsync` | Agree to an approval task (process flows to next approver after agreement) |
| `RejectApprovalAsync` | Reject an approval task (approval process ends after rejection) |
| `TransferApprovalAsync` | Transfer an approval task (process flows to transferee after transfer) |
| `RollbackApprovalAsync` | Rollback from current approval task to one or more approved task nodes |
| `InstancesAddSignAsync` | Add sign to an approval task |
| `ResubmitApprovalAsync` | Resubmit an approval task returned to the initiator |

#### Approval Comments (`IFeishuV4ApprovalComments`)

Employees can comment and reply to comments in approval instances, supporting text, @users, and attachments.

| Method | Description |
|--------|-------------|
| `CreateCommentAsync` | Create, modify comments or reply to comments in a specified approval instance |
| `DeleteCommentByIdAsync` | Delete a comment or comment reply in an approval instance |
| `RemoveCommentsAsync` | Clear all comments and comment replies in an approval instance |
| `GetCommentsPageListByIdAsync` | Get all comments and comment replies in an approval instance by approval instance Code |

---

### 📝 Task Management

Feishu Task is a universal task/project management tool built into Feishu with powerful collaboration capabilities. You can easily create tasks in Feishu App's task center, groups, documents, and other scenarios.

#### Task Management (`IFeishuV2Task`)

| Method | Description |
|--------|-------------|
| `CreateTaskAsync` | Create a task (supports title, description, owner, time, reminders, etc.) |
| `UpdateTaskAsync` | Modify task title, description, due time, etc. |
| `GetTaskByIdAsync` | Get task details (title, description, time, members, etc.) |
| `DeleteTaskByIdAsync` | Delete a task |
| `AddMembersByIdAsync` | Add task owners or followers |
| `RemoveMembersByIdAsync` | Remove task owners or followers |
| `GetTaskListsByIdAsync` | List all tasklists a task belongs to |
| `AddTaskListsByIdAsync` | Add a task to a tasklist |
| `RemoveTaskListsByIdAsync` | Remove a task from a tasklist |
| `AddTaskReminderByIdAsync` | Add a reminder to a task (calculated based on due time) |
| `RemoveTaskReminderByIdAsync` | Remove a reminder from a task |
| `AddTaskDependenciesByIdAsync` | Add dependencies to a task (predecessor and successor dependencies) |
| `RemoveTaskDependenciesByIdAsync` | Remove dependencies from a task |
| `CreateSubTaskAsync` | Create a subtask for a task |
| `GetSubTasksPageListByIdAsync` | Get subtask list of a task with pagination |

#### Custom Fields (`IFeishuV2TaskCustomFields`)

Task functionality supports extending custom fields in tasks to add key task information more clearly, you can define fields like "priority", "project release date", "price", etc.

| Method | Description |
|--------|-------------|
| `CreateCustomFieldsAsync` | Create a custom field and add it to a resource (tasklist) |
| `UpdateCustomFieldsAsync` | Update a custom field's name and settings |
| `GetCustomFieldsByIdAsync` | Get custom field details |
| `GetCustomFieldsPageListAsync` | List custom fields accessible to the user with pagination |
| `AddCustomFieldsByIdAsync` | Add a custom field to a resource (tasklist) |
| `RemoveCustomFieldsByIdAsync` | Remove a custom field from a resource |
| `CreateCustomFieldsOptionsAsync` | Add a custom option to a single-select or multi-select field |
| `UpdateCustomFieldsOptionsAsync` | Update custom field option data |

---

### 👥 Organization

Complete organization management API interfaces, supporting users, departments, employees, user groups, positions, job levels, and more.

#### User Management (`IFeishuV3User`)

Feishu users are basic resources in Feishu contacts, corresponding to member entities in the enterprise organization.

| Method | Description |
|--------|-------------|
| `CreateUserAsync` | Create a user in contacts (employee onboarding) |
| `UpdateUserIdAsync` | Update user ID |
| `GetBatchUsersAsync` | Get IDs and status information of one or more users by phone number or email |
| `GetUsersByKeywordAsync` | Search other users' information by username keyword |
| `DeleteUserByIdAsync` | Delete a specified user from contacts (employee departure) |
| `ResurrectUserByIdAsync` | Restore deleted user (departed member) |
| `LogoutAsync` | Log out user's session |
| `GetJsTicketAsync` | Get temporary credential for calling JSAPI |

#### Department Management (`IFeishuV1Departments`)

Department is a basic entity in Feishu organization structure, each employee belongs to one or more departments.

| Method | Description |
|--------|-------------|
| `CreateDepartmentAsync` | Create a new department in the enterprise organization |
| `UpdateDepartmentAsync` | Update enterprise organization department information |
| `DeleteDepartmentByIdAsync` | Delete a specified department from the enterprise organization |
| `QueryDepartmentsAsync` | Support passing multiple department IDs, return detailed info for each department |
| `QueryDepartmentsPageListAsync` | Batch get list of department details that meet specified conditions |
| `SearchEmployeePageListAsync` | Search department information (by department name keyword, etc.) |

#### Employee Management (`IFeishuV1Employees`)

Employees refer to members with "Employee" identity in Feishu enterprise, equivalent to "User" in Contacts OpenAPI.

| Method | Description |
|--------|-------------|
| `CreateEmployeeAsync` | Create an employee in the enterprise |
| `UpdateEmployeeAsync` | Update active/departed employee information, freeze/restore employees |
| `DeleteEmployeeByIdAsync` | Depart an employee (app needs permission for all departments the employee belongs to) |
| `ResurrectEmployeeAsync` | Restore departed member to active status |
| `ResignedEmployeeAsync` | Process departure for active employee, update to "pending departure" status |
| `RegularEmployeeAsync` | Cancel departure for pending departure employee, update to "active" status |
| `QueryEmployeesAsync` | Batch query employee details by employee IDs |
| `QueryEmployeePageListAsync` | Batch get list of employee details that meet specified conditions with pagination |
| `SearchEmployeePageListAsync` | Search employee information (by keyword search name, phone, email, etc.) |

#### User Group Management (`IFeishuV3UserGroup`)

User groups are one of the basic entities in Feishu contacts, you can add users or department resources within user groups. Various business permission controls can be associated with user groups.

| Method | Description |
|--------|-------------|
| `CreateUserGroupAsync` | Create a user group |
| `UpdateUserGroupAsync` | Update a user group |
| `GetUserGroupInfoByIdAsync` | Query specified user group basic info by user group ID |
| `GetUserGroupsAsync` | Query user group list under current tenant |
| `GetUserBelongGroupsAsync` | Query user group list that a specified user belongs to |
| `DeleteUserGroupByIdAsync` | Delete a specified user group |

#### Role Management (`IFeishuV3Role`)

Feishu roles refer to professional division categories of team members, such as HR, administration, finance, etc. A role can consist of one or more members. Currently mainly used for application approval scenarios.

| Method | Description |
|--------|-------------|
| `CreateRoleAsync` | Create a role |
| `UpdateRoleAsync` | Modify specified role's name |
| `DeleteRoleByIdAsync` | Delete a specified role |

---

### 💬 Message Service (Messages)

A message is a single message in Feishu chat. You can use message management API to send, reply, edit, recall, forward, and query messages.

#### Message Management (`IFeishuV1Message`)

| Method | Description |
|--------|-------------|
| `RevokeMessageAsync` | Revoke a specified message (bot can revoke its own messages, group owner can revoke messages in the group) |
| `AddMessageReactionsAsync` | Add a specified type of emoji reaction to a specified message |
| `GetMessageReactionsPageListAsync` | Get emoji reaction list in a specified message |
| `DeleteMessageReactionsAsync` | Delete a specific emoji reaction from a message |
| `PinMessageAsync` | Pin a specified message |
| `DeletePinMessageAsync` | Remove a Pin from a specified message |
| `GetPinMessagePageListAsync` | Get all Pin messages in a specified group within a specified time range |

#### Tenant-level Message Operations (`IFeishuTenantV1Message`)

| Method | Description |
|--------|-------------|
| `SendMessageAsync` | Send a message to a specified user or group (supports text, rich text, card, image, video, audio, file, etc.) |
| `ReplyMessageAsync` | Reply to a specified message |
| `EditMessageAsync` | Edit sent message content (supports text, rich text messages) |
| `ReceiveMessageAsync` | Forward a specified message to a user, group, or topic |
| `MergeReceiveMessageAsync` | Merge and forward multiple messages from the same conversation to specified users, groups, or topics |
| `ReceiveThreadsAsync` | Forward a topic to specified users, groups, or topics |
| `CreateMessageFollowUpAsync` | Add bubble-style content below the latest message |
| `GetMessageReadUsesAsync` | Query if a specified message has been read |
| `GetHistoryMessageAsync` | Get history messages (chat records) in a specified conversation |
| `GetMessageFile` | Get resource files contained in a specified message (small files) |
| `GetMessageLargeFile` | Get resource files contained in a specified message (large files) |
| `GetContentListByMessageIdAsync` | Query specified message content by message_id |
| `DownFileAsync` | Download file by uploaded file Key (small files) |
| `DownLargeFileAsync` | Download file by uploaded file Key (large files) |
| `DownImageAsync` | Download image by uploaded image Key (small files) |
| `DownLargeImageAsync` | Download image by uploaded image Key (large files) |
| `UploadFileAsync` | Upload local file to open platform (supports audio, video, documents, etc.) |
| `UploadImageAsync` | Upload image to Feishu open platform |
| `MessageUrgentAppAsync` | Urgent a specified message to target users (notification in Feishu client) |
| `MessageUrgentSMSAsync` | Urgent a specified message to target users (Feishu client and SMS) |
| `MessageUrgentPhoneAsync` | Urgent a specified message to target users (Feishu client and phone call) |
| `UpdateUrlPreviewAsync` | Update URL preview |

#### Batch Messages (`IFeishuTenantV1BatchMessage`)

For managing sending messages to multiple users or multiple departments.

| Method | Description |
|--------|-------------|
| `BatchSendTextMessageAsync` | Send text messages to members of multiple users or multiple departments |
| `BatchSendRichTextMessageAsync` | Send rich text messages to members of multiple users or multiple departments |
| `BatchSendImageMessageAsync` | Send image messages to members of multiple users or multiple departments |
| `BatchSendGroupShareMessageAsync` | Send group share messages to members of multiple users or multiple departments |
| `RevokeMessageAsync` | Revoke messages sent via batch message API |
| `GetUserReadMessageInfosAsync` | Query total number of batch message recipients and number of read users |
| `GetBatchMessageProgressAsync` | Query message sending progress and recall progress |

---

### 🃏 Card Service (Cards)

Feishu cards are an application capability, including components needed to build card content and capabilities needed to send cards, with a visual building tool provided.

#### Card Management (`IFeishuV1Card`)

| Method | Description |
|--------|-------------|
| `CreateCardAsync` | Create a card entity based on card JSON code or card built with visual builder |
| `UpdateCardSettingsByIdAsync` | Update specified card entity's configuration (supports config field and card_link field) |
| `PartialUpdateCardByIdAsync` | Update card entity local content (including configuration and components, supports multi-component add/delete/modify) |
| `UpdateCardByIdAsync` | Pass new card JSON code to overwrite and update all content of specified card entity |

#### Card Element Operations (`IFeishuV1CardElements`)

| Method | Description |
|--------|-------------|
| `CreateCardElementAsync` | Add new components to specified card entity to extend card content |
| `UpdateCardElementByIdAsync` | Update specified component in card entity to new component |
| `UpdateCardElementAttributeByIdAsync` | Update corresponding component's attributes in card entity |
| `StreamUpdateCardTextByIdAsync` | Pass full text content to text element to achieve "typewriter" style text output effect |
| `DeleteCardElementByIdAsync` | Delete component in specified card entity |

#### Message Stream Cards (`IFeishuV2AppCardMessageStream`)

App message stream cards are message delivery capabilities provided by Feishu for apps, allowing apps to send messages directly in the message stream.

| Method | Description |
|--------|-------------|
| `CreateCardMessageStreamAsync` | Create app message stream card |
| `UpdateCardMessageStreamAsync` | Update app message stream card |
| `DeleteCardMessageStreamAsync` | Delete app message stream card |
| `BotTimeSentiveAsync` | Pin bot conversation at top of message list |
| `UpdateCardMessageStreamButtonAsync` | Add, update, delete quick action buttons for message stream cards |
| `FeedCardsByFeedCardIdAsync` | Instant reminder capability, pin group or bot conversation at top of message list |

---

### 💬 Chat Group Management (ChatGroup)

Feishu group OpenAPI provides group management capabilities, including creating groups, dissolving groups, updating group information, getting group information, managing group pins, and getting group share links.

#### Group Management (`IFeishuV1ChatGroup`)

| Method | Description |
|--------|-------------|
| `UpdateChatGroupByIdAsync` | Update specified group's information (group avatar, name, description, configuration, owner, etc.) |
| `DeleteChatGroupAsync` | Dissolve specified group by chat_id |
| `UpdateChatModerationAsync` | Update specified group's speaking permissions (all members can speak, only admins can speak, specified members can speak) |
| `GetChatGroupInoByIdAsync` | Get specified group's basic information (name, description, avatar, owner ID, permission configuration, etc.) |
| `PutChatGroupTopNoticeAsync` | Update group's pinned info (can pin messages or group announcement) |
| `DeleteChatGroupTopNoticeAsync` | Remove pinned message or group announcement from specified group |
| `GetChatGroupPageListAsync` | Get list of groups current user or bot is in with pagination |
| `GetChatGroupPageListByKeywordAsync` | Get list of groups visible to current identity with pagination (supports keyword search) |
| `GetChatGroupModeratorPageListByIdAsync` | Get specified group's speaking mode, speakable user list, etc. with pagination |
| `GetChatGroupShareLinkByIdAsync` | Get share link for specified group |

#### Group Announcement Management (`IFeishuV1ChatGroupAnnouncement`)

Group announcement is an announcement document in the group, carried by Feishu cloud document, each group has only one group announcement.

| Method | Description |
|--------|-------------|
| `GetNoticeInfoByIdAsync` | Get group announcement basic information in specified group |
| `GetNoticeBlocksListByIdAsync` | Get rich text content of all blocks in group announcement with pagination |
| `CreateNoticeBlockAsync` | Create a batch of child blocks in specified block's child list |
| `UpdateNoticeBlockAsync` | Batch update rich text content of blocks |
| `GetBlockContentByIdAsync` | Get rich text content of group announcement block |
| `GetBlockContentPageListByIdAsync` | Get rich text content of all blocks in group announcement with pagination |
| `DeleteBlockByIdAsync` | Delete specified range of child blocks from specified block |

#### Group Member Management (`IFeishuV1ChatGroupMember`)

Feishu group members include users and bots, supporting adding users or bots as group members, and also supporting setting users or bots as group admins.

| Method | Description |
|--------|-------------|
| `AddManagersAsync` | Set specified users or bots in a specified group as group admins |
| `DeleteManagersAsync` | Delete specified admins from a specified group |
| `AddMemberAsync` | Pull specified users or bots into a specified group |
| `MeJoinChatGroupAsync` | Add current interface caller to specified group |
| `RemoveMemberAsync` | Remove specified users or bots from a group |
| `GetMemberPageListByIdAsync` | Get member information of specified group with pagination |
| `GetMemberInChatByIdAsync` | Determine if corresponding user or bot is in specified group based on access_token used |

---

## 📁 Example Projects

### Mud.Feishu.Test

Complete HTTP API functional testing with demo code for all modules:

- **Organization**: Users, departments, employees, user groups, positions, job levels
- **Messaging**: Message sending, batch messages
- **Chat Groups**: Groups, members, menus, session tabs
- **Approvals**: Approval instances, tasks, comments
- **Tasks**: Tasks, task lists, comments, custom fields
- **Cards**: Card management, elements, message stream cards
- **Documents**: Feishu docs, block operations, content conversion
- **Wiki**: Knowledge space management, node operations, document moves
- **Drive**: File upload/download, folder management, version control
- **Attendance**: Attendance groups, check-in records, leave approvals, statistics

### FeishuWikiManager

Feishu Wiki Management Demo (Vue3 + .NET), demonstrating:

- Feishu OAuth 2.0 login integration
- Knowledge space browsing and management
- Document search and favorites
- User information and permission management

---

## ⚙️ Configuration Options

### FeishuAppConfig Configuration Items

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `AppKey` | string | - | Application unique identifier (required), used to reference this app in code |
| `AppId` | string | - | Feishu application unique identifier (required) |
| `AppSecret` | string | - | Feishu application secret (required) |
| `BaseUrl` | string | "https://open.feishu.cn" | Feishu API base URL |
| `TimeOut` | int | 30 | HTTP request timeout (seconds), range: 1-300 |
| `RetryCount` | int | 3 | Retry count on failure, range: 0-10 |
| `RetryDelayMs` | int | 1000 | Retry delay (milliseconds), range: 100-60000 |
| `TokenRefreshThreshold` | int | 300 | Token refresh threshold (seconds), range: 60-3600 |
| `EnableLogging` | bool | true | Enable logging |
| `IsDefault` | bool | false | Whether it's the default app (supports auto-inference) |

### Configuration Validation

`FeishuAppConfig` provides a `Validate()` method for validating configuration items:

- `TimeOut` must be between 1-300 seconds
- `RetryCount` must be between 0-10 times
- `RetryDelayMs` must be between 100-60000 milliseconds
- `TokenRefreshThreshold` must be between 60-3600 seconds
- `BaseUrl` must be a valid HTTP/HTTPS URL format
- `AppId` must start with `cli_` or `app_` and be at least 20 characters
- `AppSecret` must be at least 16 characters

### Security Recommendations

- `AppId` and `AppSecret` are the credentials for your Feishu application, please keep them safe
- Recommend using environment variables or secure configuration management systems to store sensitive information
- Do not hardcode sensitive information in your code
- In production environments, recommend using HTTPS protocol to ensure communication security
- Production environment must enable signature verification and timestamp validation

---

## 📚 Supported .NET Versions

| .NET Version | Status | Description |
|--------------|--------|-------------|
| .NET Standard 2.0 | ✅ | Compatibility version |
| .NET 6.0 | ✅ LTS | Long-term support version |
| .NET 7.0 | ✅ | Stable version |
| .NET 8.0 | ✅ LTS | Long-term support version |
| .NET 9.0 | ✅ | Stable version |
| .NET 10.0 | ✅ LTS | Long-term support version |

---

## 🤝 Contributing Guidelines

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

---

<div align="center">

**Made with ❤️ by MudTools**

If you find this project helpful, please give us a ⭐️ Star!

</div>
