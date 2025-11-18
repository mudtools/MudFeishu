# MudFeishu

MudFeishu 是一个现代化的 .NET 库，用于简化与飞书（Feishu）API 的集成。它基于特性驱动的 HTTP 客户端设计，提供了一套类型安全的接口和强类型化的数据模型，使开发人员能够轻松地在 .NET 应用程序中调用飞书 API。

## 功能特性

- **特性驱动的 HTTP 客户端**：使用 `[HttpClientApi]` 特性自动生成 HTTP 客户端，简化 API 调用
- **强类型数据模型**：完整的飞书 API 数据模型，包含详细的 XML 文档注释
- **智能令牌管理**：内置自动令牌缓存和刷新机制，支持租户令牌和用户令牌
- **统一的响应处理**：基于 `ApiResult<T>` 的响应包装，简化错误处理
- **依赖注入友好**：提供 `IServiceCollection` 扩展方法，易于集成到现代 .NET 应用
- **多版本 .NET 支持**：支持 .NET 8.0、.NET 9.0、.NET 10.0，使用最新的 C# 13.0 语言特性
- **完整的飞书 API 覆盖**：支持认证、用户管理、部门管理、用户组管理、人员类型管理
- **高性能缓存机制**：解决缓存击穿和竞态条件，支持令牌自动刷新
- **企业级错误处理**：统一的异常处理和日志记录

## 支持的 .NET 版本

- **.NET 8.0** - LTS 长期支持版本
- **.NET 9.0** - 当前稳定版本  
- **.NET 10.0** - 最新LTS 长期支持版本

## 快速开始

### 安装

你可以通过 NuGet 安装 MudFeishu：

```bash
dotnet add package Mud.Feishu
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
    private readonly IFeishuAuthenticationApi _authApi;
    private readonly IFeishuUserApi _userApi;
    private readonly IFeishuDepartmentsApi _departmentsApi;
    private readonly IFeishuUserGroupApi _userGroupApi;
    private readonly IFeishuEmployeeTypeApi _employeeTypeApi;

    public FeishuController(
        IFeishuAuthenticationApi authApi, 
        IFeishuUserApi userApi,
        IFeishuDepartmentsApi departmentsApi,
        IFeishuUserGroupApi userGroupApi,
        IFeishuEmployeeTypeApi employeeTypeApi)
    {
        _authApi = authApi;
        _userApi = userApi;
        _departmentsApi = departmentsApi;
        _userGroupApi = userGroupApi;
        _employeeTypeApi = employeeTypeApi;
    }
}
```

## API 接口

### 认证授权 API (`IFeishuAuthenticationApi`)

- `GetUserInfoAsync()` - 通过 access_token 获取用户信息
- `LogoutAsync()` - 用户退出登录
- `GetJsTicketAsync()` - 获取 JS SDK 临时调用凭证
- `GetTenantAccessTokenAsync()` - 获取租户访问令牌
- `GetAppAccessTokenAsync()` - 获取应用访问令牌
- `GetOAuthenAccessTokenAsync()` - OAuth 授权获取用户访问令牌
- `GetOAuthenRefreshAccessTokenAsync()` - 刷新用户访问令牌
- `GetAuthorizeAsync()` - 发起用户授权

### 用户管理 API (`IFeishuUserApi`)

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

### 部门管理 API (`IFeishuDepartmentsApi`)

- `CreateDepartmentAsync()` - 创建部门
- `UpdatePartDepartmentAsync()` - 部分更新部门信息
- `UpdateDepartmentAsync()` - 更新部门信息
- `UpdateDepartmentIdAsync()` - 更新部门 ID
- `GetDepartmentByIdAsync()` - 根据 ID 获取部门信息

### 用户组管理 API (`IFeishuUserGroupApi`)

- `CreateUserGroupAsync()` - 创建用户组
- `UpdateUserGroupAsync()` - 更新用户组
- `GetUserGroupByIdAsync()` - 根据 ID 获取用户组信息
- `GetUserGroupsAsync()` - 获取用户组列表
- `GetUserBelongGroupsAsync()` - 获取用户所属的用户组
- `DeleteUserGroupByIdAsync()` - 删除用户组

### 人员类型管理 API (`IFeishuEmployeeTypeApi`)

- `CreateEmployeeTypeAsync()` - 创建人员类型
- `UpdateEmployeeTypeAsync()` - 更新人员类型
- `GetEmployeeTypesAsync()` - 获取人员类型列表
- `DeleteEmployeeTypeByIdAsync()` - 删除人员类型

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
    private readonly IFeishuUser _userApi;

    public UserController(IFeishuUser userApi)
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
    private readonly IFeishuAuthenticationApi _authApi;

    public AuthController(IFeishuAuthenticationApi authApi)
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
    private readonly IFeishuUserApi _userApi;
    private readonly IFeishuAuthenticationApi _authApi;

    public UserController(IFeishuUserApi userApi, IFeishuAuthenticationApi authApi)
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
├── IFeishuAuthenticationApi.cs           # 认证授权 API (5个方法)
├── IFeishuUserApi.cs                     # 用户管理 API (13个方法)
├── IFeishuDepartmentsApi.cs             # 部门管理 API (7个方法)
├── IFeishuUserGroupApi.cs               # 用户组管理 API (6个方法)
├── IFeishuEmployeeTypeApi.cs             # 人员类型管理 API (4个方法)
├── FeishuServiceCollectionExtensions.cs  # 依赖注入扩展
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

- **.NET 8.0/9.0/10.0** - 目标框架，使用 C# 13.0
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

## 测试项目

Mud.Feishu.Test 是一个完整的 ASP.NET Core Web API 项目，用于演示和测试库的所有功能。

### 测试项目特性

- **多框架支持**：同时支持 .NET 8.0、.NET 9.0、.NET 10.0
- **Swagger 集成**：自动生成 API 文档，支持交互式测试
- **完整示例**：包含所有 API 接口的实际使用示例
- **配置完整**：包含真实的飞书应用配置示例

### 测试项目结构

```
Mud.Feishu.Test/
├── Controllers/
│   ├── AuthController.cs          # 认证相关 API 测试
│   ├── UserController.cs          # 用户管理 API 测试
│   ├── DepartmentController.cs    # 部门管理 API 测试
│   └── EmployeeTypeController.cs  # 人员类型 API 测试
├── Properties/
│   └── launchSettings.json       # 开发环境启动配置
├── Program.cs                     # 应用程序入口
├── appsettings.json              # 配置文件
└── Mud.Feishu.Test.csproj        # 项目文件
```

### 运行测试项目

```bash
# 进入测试项目目录
cd Mud.Feishu.Test

# 运行测试项目（默认端口 60360/60361）
dotnet run

# 或者指定特定框架运行
dotnet run --framework net8.0
dotnet run --framework net9.0
dotnet run --framework net10.0
```

### 测试 API 端点

启动测试项目后，可以通过以下端点测试各项功能：

#### 认证相关
- `GET /api/auth` - 获取访问令牌（测试令牌管理器）

#### 用户管理
- `GET /api/user/{userId}` - 获取指定用户信息
- `GET /api/user?departmentId=xxx` - 获取部门用户列表

#### 部门管理
- `GET /api/department/{departmentId}` - 获取指定部门信息
- `GET /api/department?parentDepartmentId=xxx` - 获取子部门列表

#### 人员类型
- `GET /api/employeetype` - 获取人员类型列表

#### 角色管理
- `POST /api/role` - 创建角色
- `PUT /api/role/{roleId}` - 更新角色
- `DELETE /api/role/{roleId}` - 删除角色

#### 单位管理
- `POST /api/unit` - 创建单位
- `PATCH /api/unit/{unitId}` - 更新单位名称
- `GET /api/unit/{unitId}` - 获取单位信息
- `GET /api/unit` - 获取单位列表
- `GET /api/unit/{unitId}/departments` - 获取单位绑定的部门列表
- `POST /api/unit/bind-department` - 绑定部门到单位
- `POST /api/unit/unbind-department` - 解除部门与单位绑定
- `DELETE /api/unit/{unitId}` - 删除单位

#### 用户组管理
- `POST /api/usergroup` - 创建用户组
- `PATCH /api/usergroup/{groupId}` - 更新用户组
- `GET /api/usergroup/{groupId}` - 获取用户组信息
- `GET /api/usergroup` - 获取用户组列表
- `GET /api/usergroup/member-belong/{memberId}` - 获取用户所属的用户组列表
- `DELETE /api/usergroup/{groupId}` - 删除用户组

#### 用户组成员管理
- `POST /api/usergroupmember/{groupId}/add-member` - 添加用户组成员
- `POST /api/usergroupmember/{groupId}/batch-add-member` - 批量添加用户组成员
- `GET /api/usergroupmember/{groupId}/members` - 获取用户组成员列表
- `POST /api/usergroupmember/{groupId}/remove-member` - 移除用户组成员
- `POST /api/usergroupmember/{groupId}/batch-remove-member` - 批量移除用户组成员

### Swagger 文档访问

启动测试项目后，访问 Swagger UI 进行交互式测试：

```
https://localhost:60360/swagger
```

### 开发工作流

### 克隆项目
```bash
git clone https://github.com/your-repo/Mud.Feishu.git
cd Mud.Feishu
```

### 构建项目
```bash
# 构建解决方案
dotnet build

# 构建特定项目
dotnet build Mud.Feishu/Mud.Feishu.csproj
```

### 运行测试
```bash
# 运行单元测试（如果有）
dotnet test

# 运行集成测试项目
cd Mud.Feishu.Test
dotnet run

# 指定框架运行测试
dotnet run --framework net8.0
```

### 打包发布
```bash
# 创建 NuGet 包
dotnet pack -c Release

# 发布到本地 NuGet 源
dotnet nuget push Mud.Feishu.*.nupkg --source Local
```

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
A: 支持 .NET 8.0、9.0、10.0，推荐使用 LTS 版本 8.0。

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
- [Mud.ServiceCodeGenerator](https://www.nuget.org/packages/Mud.ServiceCodeGenerator/)

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

### 测试要求
- 新功能必须在 `Mud.Feishu.Test` 项目中添加演示代码
- 确保 Controller 示例能够正常工作
- 添加相应的 Swagger 文档注释

## 更新日志

### v1.0.0 (2025-11-14)
- 初始版本发布
- 支持飞书认证、用户、部门、用户组、人员类型管理 API
- 内置智能令牌管理和缓存机制
- 支持 .NET 8.0/9.0/10.0
- 包含完整的测试演示项目
- 集成 Swagger 文档支持