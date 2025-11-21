# MudFeishu

MudFeishu 是一个现代化的 .NET 库，用于简化与飞书（Feishu）API 的集成。它基于特性驱动的 HTTP 客户端设计，提供了一套类型安全的接口和强类型化的数据模型，使开发人员能够轻松地在 .NET 应用程序中调用飞书 API。

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
dotnet add package Mud.Feishu --version 1.0.0
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
    private readonly IFeishuV3UserApi _userApi;
    private readonly IFeishuV3DepartmentsApi _departmentsApi;
    private readonly IFeishuV3UserGroupApi _userGroupApi;
    private readonly IFeishuV3EmployeeTypeApi _employeeTypeApi;
    private readonly IFeishuJobLevelApi _jobLevelApi;
    private readonly IFeishuJobFamiliesApi _jobFamiliesApi;

    public FeishuController(
        IFeishuV3AuthenticationApi authApi, 
        IFeishuV3UserApi userApi,
        IFeishuV3DepartmentsApi departmentsApi,
        IFeishuV3UserGroupApi userGroupApi,
        IFeishuV3EmployeeTypeApi employeeTypeApi,
        IFeishuJobLevelApi jobLevelApi,
        IFeishuJobFamiliesApi jobFamiliesApi)
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

## API 接口

### 认证授权 API (`IFeishuV3AuthenticationApi`)

- `GetUserInfoAsync()` - 通过 access_token 获取用户信息
- `LogoutAsync()` - 用户退出登录
- `GetJsTicketAsync()` - 获取 JS SDK 临时调用凭证
- `GetTenantAccessTokenAsync()` - 获取租户访问令牌
- `GetAppAccessTokenAsync()` - 获取应用访问令牌
- `GetOAuthenAccessTokenAsync()` - OAuth 授权获取用户访问令牌
- `GetOAuthenRefreshAccessTokenAsync()` - 刷新用户访问令牌
- `GetAuthorizeAsync()` - 发起用户授权

### 用户管理 API (`IFeishuV3UserApi`)

- `CreateUserAsync()` - 创建企业用户
- `UpdateUserAsync()` - 更新用户信息
- `UpdateUserIdAsync()` - 更新用户 ID
- `GetUserByIdAsync()` - 根据 ID 获取用户信息
- `GetUserByIdsAsync()` - 批量获取用户信息
- `GetUserByDepartmentIdAsync()` - 获取部门下的用户列表
- `GetBatchUsersAsync()` - 通过手机号或邮箱获取用户 ID
- `GetUsersByKeywordAsync()` - 通过关键词搜索用户
- `DeleteUserByIdAsync()` - 删除用户
- `ResurrectUserByIdAsync()` - 恢复已删除用户

### 部门管理 API (`IFeishuV3DepartmentsApi`)

- `CreateDepartmentAsync()` - 创建部门
- `UpdatePartDepartmentAsync()` - 部分更新部门信息
- `UpdateDepartmentAsync()` - 更新部门信息
- `UpdateDepartmentIdAsync()` - 更新部门 ID
- `GetDepartmentByIdAsync()` - 根据 ID 获取部门信息

### 用户组管理 API (`IFeishuV3UserGroupApi`)

- `CreateUserGroupAsync()` - 创建用户组
- `UpdateUserGroupAsync()` - 更新用户组
- `GetUserGroupByIdAsync()` - 根据 ID 获取用户组信息
- `GetUserGroupsAsync()` - 获取用户组列表
- `GetUserBelongGroupsAsync()` - 获取用户所属的用户组
- `DeleteUserGroupByIdAsync()` - 删除用户组

### 人员类型管理 API (`IFeishuV3EmployeeTypeApi`)

- `CreateEmployeeTypeAsync()` - 创建人员类型
- `UpdateEmployeeTypeAsync()` - 更新人员类型
- `GetEmployeeTypesAsync()` - 获取人员类型列表
- `DeleteEmployeeTypeByIdAsync()` - 删除人员类型

### 职级管理 API (`IFeishuV3JobLevelApi`)

- `CreateJobLevelAsync()` - 创建职级
- `UpdateJobLevelAsync()` - 更新职级
- `GetJobLevelByIdAsync()` - 根据 ID 获取职级信息
- `GetJobLevelsAsync()` - 获取职级列表
- `DeleteJobLevelByIdAsync()` - 删除职级

### 职位序列管理 API (`IFeishuV3JobFamiliesApi`)

- `CreateJobFamilyAsync()` - 创建职位序列
- `UpdateJobFamilyAsync()` - 更新职位序列
- `GetJobFamilyByIdAsync()` - 根据 ID 获取职位序列信息
- `GetJobFamilesListAsync()` - 获取职位序列列表
- `DeleteJobFamilyByIdAsync()` - 删除职位序列

### 角色管理 API (`IFeishuV3RoleApi`)

- `CreateRoleAsync()` - 创建角色
- `UpdateRoleAsync()` - 更新角色
- `GetRoleByIdAsync()` - 根据 ID 获取角色信息
- `GetRolesAsync()` - 获取角色列表
- `DeleteRoleByIdAsync()` - 删除角色

### 角色成员管理 API (`IFeishuV3RoleMemberApi`)

- `AddRoleMemberAsync()` - 添加角色成员
- `BatchAddRoleMemberAsync()` - 批量添加角色成员
- `GetRoleMembersAsync()` - 获取角色成员列表
- `GetRoleMemberScopesAsync()` - 获取角色成员管理范围
- `BatchDeleteRoleMemberAsync()` - 批量删除角色成员

### 单位管理 API (`IFeishuV3UnitApi`)

- `CreateUnitAsync()` - 创建单位
- `UpdateUnitNameAsync()` - 更新单位名称
- `GetUnitByIdAsync()` - 根据 ID 获取单位信息
- `GetUnitsAsync()` - 获取单位列表
- `GetUnitDepartmentsAsync()` - 获取单位绑定的部门列表
- `BindDepartmentToUnitAsync()` - 绑定部门到单位
- `UnbindDepartmentFromUnitAsync()` - 解除部门与单位绑定
- `DeleteUnitByIdAsync()` - 删除单位

### 用户组成员管理 API (`IFeishuV3UserGroupMemberApi`)

- `AddUserGroupMemberAsync()` - 添加用户组成员
- `BatchAddUserGroupMemberAsync()` - 批量添加用户组成员
- `GetUserGroupMembersAsync()` - 获取用户组成员列表
- `RemoveUserGroupMemberAsync()` - 移除用户组成员
- `BatchRemoveUserGroupMemberAsync()` - 批量移除用户组成员

### 职务管理 API (`IFeishuV3JobTitleApi`)

- `GetJobTitlesAsync()` - 获取职务信息列表

### 工作城市管理 API (`IFeishuV3WorkCityApi`)

- `GetWorkCitiesAsync()` - 获取工作城市列表
- `GetWorkCityByIdAsync()` - 根据 ID 获取工作城市信息

## 使用示例

### 示例项目 (Mud.Feishu.Test)

项目包含一个完整的测试演示项目 `Mud.Feishu.Test`，展示了所有 API 的实际使用方式：

#### 配置文件示例 (appsettings.json)
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
    "AppId": "Feishu AppId",
    "AppSecret": "Feishu AppSecret",
    "BaseUrl": "https://open.feishu.cn"
  }
}
```

#### 控制器注入示例
```csharp
// 使用 TokenManager 直接获取令牌
public class AuthController : ControllerBase
{
    private readonly ITokenManager _tokenManager;

    public AuthController(ITokenManager tokenManager)
    {
        _tokenManager = tokenManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetToken()
    {
        var token = await _tokenManager.GetTokenAsync();
        return Ok(token);
    }
}

// 使用包装后的 API（自动处理令牌）
public class UserController : ControllerBase
{
    private readonly IFeishuV3User _userApi;

    public UserController(IFeishuV3User userApi)
    {
        _userApi = userApi;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        try
        {
            var result = await _userApi.GetUserByIdAsync(userId);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
```

## 使用示例（ASP.NET Core Controller）

以下示例展示了如何在 ASP.NET Core Controller 中使用飞书 API。

### 获取租户访问令牌

```csharp
using Microsoft.AspNetCore.Mvc;
using Mud.Feishu;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IFeishuV3AuthenticationApi _authApi;

    public AuthController(IFeishuV3AuthenticationApi authApi)
    {
        _authApi = authApi;
    }

    [HttpPost("tenant-token")]
    public async Task<IActionResult> GetTenantAccessTokenAsync()
    {
        var credentials = new AppCredentials
        {
            AppId = "your_app_id",
            AppSecret = "your_app_secret"
        };

        var tokenResult = await _authApi.GetTenantAccessTokenAsync(credentials);

        if (tokenResult.Code == 0)
        {
            return Ok(new 
            { 
                token = tokenResult.TenantAccessToken,
                expiresIn = tokenResult.Expire
            });
        }
        else
        {
            return BadRequest(new { error = tokenResult.Msg, code = tokenResult.Code });
        }
    }
}
```

### 创建企业用户

```csharp
using Microsoft.AspNetCore.Mvc;
using Mud.Feishu;
using Mud.Feishu.DataModels.Users;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IFeishuV3UserApi _userApi;
    private readonly IFeishuV3AuthenticationApi _authApi;

    public UserController(IFeishuV3UserApi userApi, IFeishuV3AuthenticationApi authApi)
    {
        _userApi = userApi;
        _authApi = authApi;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest request)
    {
        // 首先获取访问令牌
        var tokenResult = await _authApi.GetTenantAccessTokenAsync(new AppCredentials
        {
            AppId = "your_app_id",
            AppSecret = "your_app_secret"
        });

        if (tokenResult.Code != 0)
        {
            return BadRequest(new { error = "获取访问令牌失败", details = tokenResult.Msg });
        }

        var result = await _userApi.CreateUserAsync(
            $"Bearer {tokenResult.TenantAccessToken}",
            request
        );

        if (result.Code == 0)
        {
            return Ok(new { 
                success = true, 
                userId = result.Data?.User?.UserId,
                message = "用户创建成功" 
            });
        }
        else
        {
            return BadRequest(new { error = result.Msg, code = result.Code });
        }
    }
}
```

### 查询部门信息

```csharp
using Microsoft.AspNetCore.Mvc;
using Mud.Feishu;

[ApiController]
[Route("api/[controller]")]
public class DepartmentController : ControllerBase
{
    private readonly IFeishuDepartmentsApi _departmentsApi;
    private readonly IFeishuAuthenticationApi _authApi;

    public DepartmentController(IFeishuDepartmentsApi departmentsApi, IFeishuAuthenticationApi authApi)
    {
        _departmentsApi = departmentsApi;
        _authApi = authApi;
    }

    [HttpGet("{departmentId}")]
    public async Task<IActionResult> GetDepartmentAsync(string departmentId)
    {
        var tokenResult = await _authApi.GetTenantAccessTokenAsync(new AppCredentials
        {
            AppId = "your_app_id",
            AppSecret = "your_app_secret"
        });

        if (tokenResult.Code != 0)
        {
            return BadRequest(new { error = "获取访问令牌失败" });
        }

        var result = await _departmentsApi.GetDepartmentByIdAsync(
            $"Bearer {tokenResult.TenantAccessToken}",
            departmentId
        );

        if (result.Code == 0)
        {
            return Ok(result.Data);
        }
        else
        {
            return BadRequest(new { error = result.Msg });
        }
    }
}
```

## 项目结构

```
Mud.Feishu/
├── IFeishuV3AuthenticationApi.cs         # 认证授权 API (5个方法)
├── Organization/                         # 组织架构相关API
│   ├── IFeishuV3UserApi.cs             # 用户管理 API (13个方法)
│   ├── IFeishuV3DepartmentsApi.cs       # 部门管理 API (7个方法)
│   ├── IFeishuV3UserGroupApi.cs         # 用户组管理 API (6个方法)
│   ├── IFeishuV3UserGroupMemberApi.cs   # 用户组成员管理 API (5个方法)
│   ├── IFeishuV3EmployeeTypeApi.cs     # 人员类型管理 API (4个方法)
│   ├── IFeishuV3JobLevelApi.cs         # 职级管理 API (5个方法)
│   ├── IFeishuV3JobFamiliesApi.cs      # 职位序列管理 API (5个方法)
│   ├── IFeishuV3RoleApi.cs              # 角色管理 API (5个方法)
│   ├── IFeishuV3RoleMemberApi.cs        # 角色成员管理 API (5个方法)
│   ├── IFeishuV3UnitApi.cs             # 单位管理 API (8个方法)
│   ├── IFeishuV3JobTitleApi.cs         # 职务管理 API (1个方法)
│   └── IFeishuV3WorkCityApi.cs         # 工作城市管理 API (2个方法)
├── Extensions/                            # 扩展组件
│   ├── FeishuOptions.cs                  # 配置选项
│   └── FeishuServiceCollectionExtensions.cs # 服务注册扩展
├── Extensions/
│   ├── FeishuOptions.cs                 # 配置选项
│   └── FeishuServiceCollectionExtensions.cs # 服务注册扩展
├── TokenManager/
│   ├── ITokenManager.cs                 # 令牌管理接口
│   └── TokenManagerWithCache.cs          # 带缓存的令牌管理器
├── Exceptions/
│   └── FeishuException.cs               # 飞书异常类
├── GlobalUsings.cs                      # 全局引用
└── DataModels/
    ├── Departments/                      # 部门相关数据模型
    │   ├── RequestModel/                 # 请求模型
    │   ├── ResponseModel/                # 响应模型
    │   └── Common/                       # 通用模型
    ├── Users/                           # 用户相关数据模型
    │   ├── RequestModel/                 # 请求模型
    │   ├── ResponseModel/                # 响应模型
    │   └── Common/                       # 通用模型
    ├── UserGroup/                       # 用户组相关数据模型
    │   ├── RequestModel/                 # 请求模型
    │   └── ResponseModel/                # 响应模型
    ├── EmployeeType/                    # 人员类型相关数据模型
    ├── JobLevel/                        # 职级相关数据模型
    ├── JobFamilies/                     # 职位序列相关数据模型
    ├── AppCredentials.cs                # 应用凭证
    ├── AppCredentialResult.cs           # 应用凭证结果
    ├── I18nName.cs                      # 国际化名称
    └── OAuthCredentialsResult.cs        # OAuth 凭证结果
```

## 核心组件

### 令牌管理器 (`TokenManagerWithCache`)
- **智能缓存**：自动管理令牌缓存，支持多租户场景
- **自动刷新**：令牌即将过期时自动刷新，提前5分钟触发
- **并发安全**：使用 `ConcurrentDictionary` 和 `Lazy<Task>` 解决缓存击穿
- **重试机制**：内置重试逻辑，提高系统稳定性
- **性能监控**：提供缓存统计信息，便于监控和调试

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

- **Visual Studio 2022** 或更高版本
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
A: 支持 .NET  6.0、7.0、8.0、9.0、10.0，推荐使用 LTS 8.0及以上子。

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

## 更新日志

### v1.1.0 (2025-11-14)
- 新增角色管理 API (`IFeishuV3RoleApi`)
- 新增角色成员管理 API (`IFeishuV3RoleMemberApi`)
- 新增单位管理 API (`IFeishuV3UnitApi`)
- 新增职务管理 API (`IFeishuV3JobTitleApi`)
- 新增工作城市管理 API (`IFeishuV3WorkCityApi`)
- 新增职级管理 API (`IFeishuV3JobLevelApi`)
- 新增职位序列管理 API (`IFeishuV3JobFamiliesApi`)
- 完善数据模型 XML 注释
- 优化测试项目，新增多个测试控制器
- 统一部门相关数据模型，消除重复代码
- 更新 README 文档，增加 API 完整覆盖说明
- 支持 .NET 6.0/7.0/8.0/9.0/10.0

### v1.0.0 (2025-11-14)
- 初始版本发布
- 支持飞书认证、用户、部门、用户组、人员类型管理 API
- 内置智能令牌管理和缓存机制
- 支持 .NET 8.0/9.0/10.0
- 包含完整的测试演示项目
- 集成 Swagger 文档支持