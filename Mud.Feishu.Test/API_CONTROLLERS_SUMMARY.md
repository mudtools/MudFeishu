# API测试控制器汇总

## 已完善的控制器

### 1. UserController (飞书用户管理)
覆盖了 `IFeishuUserApi` 的所有接口：

| 方法 | 路径 | HTTP方法 | 功能 |
|------|------|----------|------|
| CreateUser | `/api/user` | POST | 创建用户（员工入职） |
| UpdateUser | `/api/user/{userId}` | PUT | 更新用户信息 |
| UpdateUserId | `/api/user/{userId}/update-user-id` | PATCH | 更新用户ID |
| GetUser | `/api/user/{userId}` | GET | 获取用户信息 |
| GetUsersByIds | `/api/user/batch` | POST | 批量获取用户信息 |
| GetUsersByDepartment | `/api/user/by-department/{departmentId}` | GET | 根据部门ID获取用户列表 |
| GetBatchUsers | `/api/user/batch-get-id` | POST | 通过手机号或邮箱获取用户ID |
| SearchUsers | `/api/user/search` | GET | 通过关键词搜索用户 |
| DeleteUser | `/api/user/{userId}` | DELETE | 删除用户（员工离职） |
| ResurrectUser | `/api/user/{userId}/resurrect` | POST | 恢复已删除用户 |
| GetCurrentUser | `/api/user/current` | GET | 获取当前用户信息 |

### 2. DepartmentController (飞书部门管理)
覆盖了 `IFeishuDepartmentsApi` 的所有接口：

| 方法 | 路径 | HTTP方法 | 功能 |
|------|------|----------|------|
| CreateDepartment | `/api/department` | POST | 创建部门 |
| UpdatePartDepartment | `/api/department/{departmentId}` | PATCH | 部分更新部门信息 |
| UpdateDepartment | `/api/department/{departmentId}` | PUT | 完全更新部门信息 |
| UpdateDepartmentId | `/api/department/{departmentId}/update-department-id` | PATCH | 更新部门ID |
| UnbindDepartmentChat | `/api/department/unbind-department-chat` | POST | 解绑部门群聊 |
| GetDepartment | `/api/department/{departmentId}` | GET | 获取部门信息 |
| GetDepartmentsByIds | `/api/department/batch` | GET | 批量获取部门信息 |
| GetSubDepartments | `/api/department/{departmentId}/children` | GET | 获取子部门列表 |
| GetParentDepartments | `/api/department/{departmentId}/parents` | GET | 获取父部门列表 |
| SearchDepartments | `/api/department/search` | POST | 搜索部门 |

### 3. RoleMemberController (飞书角色成员管理)
覆盖了 `IFeishuRoleMemberApi` 的所有接口：

| 方法 | 路径 | HTTP方法 | 功能 |
|------|------|----------|------|
| BatchAddMember | `/api/rolemember/{roleId}/batch-add-member` | POST | 批量添加角色成员 |
| BatchAddMembersSopes | `/api/rolemember/{roleId}/set-member-scopes` | POST | 批量设置角色成员管理范围 |
| GetMemberSopes | `/api/rolemember/{roleId}/members/{memberId}/scopes` | GET | 获取角色成员的管理范围 |
| GetMembers | `/api/rolemember/{roleId}/members` | GET | 获取角色成员列表 |
| DeleteMembers | `/api/rolemember/{roleId}/batch-delete-member` | DELETE | 批量删除角色成员 |

### 4. JobTitleController (飞书职务管理)
覆盖了 `IFeishuJobTitleApi` 的所有接口：

| 方法 | 路径 | HTTP方法 | 功能 |
|------|------|----------|------|
| GetTenantJobTitlesList | `/api/jobtitle/tenant/list` | GET | 获取当前租户下的职务列表 |
| GetUserJobTitlesList | `/api/jobtitle/user/list` | GET | 获取当前登录用户下的职务列表 |
| GetTenantJobTitleById | `/api/jobtitle/tenant/{jobTitleId}` | GET | 获取指定职务的信息（租户级别） |
| GetUserJobTitleById | `/api/jobtitle/user/{jobTitleId}` | GET | 获取指定职务的信息（用户级别） |

### 5. WorkCityController (飞书工作城市管理)
覆盖了 `IFeishuWorkCityApi` 的所有接口：

| 方法 | 路径 | HTTP方法 | 功能 |
|------|------|----------|------|
| GetTenantWorkCitiesList | `/api/workcity/tenant/list` | GET | 获取当前租户下所有工作城市列表 |
| GetUserWorkCitiesList | `/api/workcity/user/list` | GET | 获取当前登录用户下所有工作城市列表 |
| GetTenantWorkCityById | `/api/workcity/tenant/{workCityId}` | GET | 获取指定工作城市的信息（租户级别） |
| GetUserWorkCityById | `/api/workcity/user/{workCityId}` | GET | 获取指定工作城市的信息（用户级别） |

## 特性

### 统一的错误处理
所有控制器都包含 try-catch 错误处理，返回统一的错误格式：
```json
{
  "error": "错误消息"
}
```

### 参数验证
- 使用 `[Required]` 特性标记必需参数
- 使用 `[FromBody]` 和 `[FromQuery]` 特性明确参数来源
- 提供合理的默认值

### RESTful 设计
- 遵循 RESTful API 设计原则
- 使用适当的 HTTP 方法（GET、POST、PUT、PATCH、DELETE）
- 清晰的路由结构

### 文档注释
- 每个控制器和方法都有详细的 XML 文档注释
- 参数说明清晰
- 返回值说明明确

## 使用方式

这些控制器可以直接在 ASP.NET Core Web API 项目中使用：

1. 注册服务：
```csharp
builder.Services.AddControllers();
builder.Services.AddScoped<IFeishuUser, FeishuUser>();
builder.Services.AddScoped<IFeishuDepartments, FeishuDepartments>();
builder.Services.AddScoped<IFeishuRoleMember, FeishuRoleMember>();
```

2. 启动应用后，可以通过 Swagger UI 查看和测试所有 API 接口。

3. 所有接口都支持标准的 HTTP 状态码和错误处理。