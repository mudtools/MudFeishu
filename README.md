# MudFeishu

MudFeishu 是一个现代化的 .NET 库，用于简化与飞书（Feishu）API 的集成。它基于特性驱动的 HTTP 客户端设计，提供了一套类型安全的接口和强类型化的数据模型，使开发人员能够轻松地在 .NET 应用程序中调用飞书 API。

## 功能特性

- **特性驱动的 HTTP 客户端**：使用 `[HttpClientApi]` 特性自动生成 HTTP 客户端，简化 API 调用
- **强类型数据模型**：完整的飞书 API 数据模型，包含详细的 XML 文档注释
- **自动令牌管理**：内置认证接口，支持多种访问令牌的获取和刷新
- **统一的响应处理**：基于 `ApiResult<T>` 的响应包装，简化错误处理
- **依赖注入友好**：提供 `IServiceCollection` 扩展方法，易于集成到现代 .NET 应用
- **现代化 .NET 支持**：支持 .NET 7.0+，使用最新的 C# 语言特性
- **完整的飞书 API 覆盖**：支持认证、用户管理、部门管理、用户组管理、人员类型管理

## 快速开始

### 安装

你可以通过 NuGet 安装 MudFeishu：

```bash
dotnet add package Mud.Feishu
```

### 配置依赖注入（ASP.NET Core）

在 `Program.cs` 或 `Startup.cs` 中注册服务：

```csharp
using Mud.Feishu;

var builder = WebApplication.CreateBuilder(args);

// 注册飞书 API 服务
builder.Services.AddFeishuApiService();

var app = builder.Build();
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
├── IFeishuAuthenticationApi.cs     # 认证授权 API
├── IFeishuUserApi.cs               # 用户管理 API
├── IFeishuDepartmentsApi.cs       # 部门管理 API
├── IFeishuUserGroupApi.cs         # 用户组管理 API
├── IFeishuEmployeeTypeApi.cs       # 人员类型管理 API
├── FeishuServiceCollectionExtensions.cs # 依赖注入扩展
└── DataModels/
    ├── Departments/                # 部门相关数据模型
    ├── Users/                     # 用户相关数据模型
    ├── UserGroup/                 # 用户组相关数据模型
    └── EmployeeType/              # 人员类型相关数据模型
```

## 技术栈

- **.NET 7.0+** - 目标框架
- **HttpClient** - 基于特性的 HTTP 客户端
- **System.Text.Json** - JSON 序列化
- **Microsoft.Extensions.DependencyInjection** - 依赖注入支持

## 开发要求

- Visual Studio 2022 或更高版本
- .NET 7.0 SDK 或更高版本
- 飞书开发者账号和应用凭证

## 项目结构

```
Mud.Feishu/
├── DataModels/           # 数据模型定义
│   ├── Users/           # 用户相关数据模型
│   └── *.cs            # 其他数据模型
├── IFeishuAuthenticationApi.cs    # 认证授权接口
├── IFeishuUserApi.cs              # 用户管理接口
├── FeishuServiceCollectionExtensions.cs  # 依赖注入扩展
└── Mud.Feishu.csproj              # 项目文件
```

## 依赖项

- `Microsoft.Extensions.Http` - HTTP 客户端支持
- `Mud.ServiceCodeGenerator` - 服务代码生成器

## 开发

### 构建项目

```bash
dotnet build
```

### 运行测试

```bash
dotnet test
```

## 贡献指南

欢迎贡献代码和建议！请遵循以下指南：

1. Fork 项目并创建特性分支
2. 提交更改并添加测试
3. 确保所有测试通过
4. 提交 Pull Request

详细的贡献指南请参阅 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 许可证

MudFeishu 遵循 [MIT 许可证](LICENSE)。

## 相关链接

- [飞书开放平台文档](https://open.feishu.cn/document/)
- [.NET 官方文档](https://docs.microsoft.com/dotnet/)
- [NuGet 包管理器](https://www.nuget.org/)