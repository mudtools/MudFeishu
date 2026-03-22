# 租户V3用户管理 - FeishuTenantV3User

## 接口名称
**租户V3用户管理 - (IFeishuTenantV3User)**

## 功能描述
本接口提供飞书通讯录V3版本用户的管理功能，适用于租户应用场景。支持用户的创建、更新、ID变更、查询、搜索、删除、恢复、退出登录以及JSAPI票据获取等操作。

飞书用户是飞书通讯录中的基础资源，对应企业组织架构中的成员实体。

## 参考文档
- [飞书官方文档 - 用户字段概述](https://open.feishu.cn/document/server-docs/contact-v3/user/field-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| GetUserInfoByIdAsync | 获取用户信息 | 租户令牌 | GET |
| GetUserByIdsAsync | 批量获取用户 | 租户令牌 | GET |
| GetUserByDepartmentIdAsync | 获取部门直属用户 | 租户令牌 | GET |
| UpdateUserAsync | 更新用户 | 租户令牌 | PATCH |
| CreateUserAsync | 创建用户 | 租户令牌 | POST |
| UpdateUserIdAsync | 更新用户ID | 租户令牌 | PATCH |
| GetBatchUsersAsync | 通过手机号/邮箱获取用户 | 租户令牌 | POST |
| GetUsersByKeywordAsync | 搜索用户 | 租户令牌 | GET |
| DeleteUserByIdAsync | 删除用户 | 租户令牌 | DELETE |
| ResurrectUserByIdAsync | 恢复用户 | 租户令牌 | POST |
| LogoutAsync | 退出登录 | 租户令牌 | POST |
| GetJsTicketAsync | 获取JSAPI票据 | 租户令牌 | POST |

## 函数详细内容

### 获取用户信息

**函数名称**：获取用户信息

**函数签名**：
```csharp
Task<FeishuApiResult<GetUserInfoResult>?> GetUserInfoByIdAsync(
    [Path] string user_id,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| user_id | string | ✅ | 用户ID |
| user_id_type | string | ⚪ | 用户ID类型，默认 `open_id` |
| department_id_type | string | ⚪ | 部门ID类型，默认 `open_department_id` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user": {
      "user_id": "ou_xxx",
      "name": "张三",
      "mobile": "+86-13800138000",
      "email": "zhangsan@example.com",
      "department_ids": ["od-xxx"],
      "status": {"is_frozen": false, "is_resigned": false}
    }
  }
}
```

**说明**：获取通讯录中某一用户的信息，包括用户ID、名称、邮箱、手机号、状态以及所属部门等信息。

**代码示例**：
```csharp
public class UserService
{
    private readonly IFeishuTenantV3User _userClient;

    public UserService(IFeishuTenantV3User userClient)
    {
        _userClient = userClient;
    }

    public async Task GetUserDetailsAsync(string userId)
    {
        var result = await _userClient.GetUserInfoByIdAsync(userId);
        if (result?.Code == 0)
        {
            var user = result.Data?.User;
            Console.WriteLine($"用户: {user?.Name}, 邮箱: {user?.Email}");
        }
    }
}
```

---

### 批量获取用户

**函数名称**：批量获取用户

**函数签名**：
```csharp
Task<FeishuApiResult<GetUserInfosResult>?> GetUserByIdsAsync(
    [Query("user_ids")] string[] user_ids,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| user_ids | string[] | ✅ | 用户ID数组 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "users": [
      {
        "user_id": "ou_xxx",
        "name": "张三",
        "email": "zhangsan@example.com"
      }
    ]
  }
}
```

**说明**：批量获取通讯录中用户的信息。

---

### 获取部门直属用户

**函数名称**：获取部门直属用户

**函数签名**：
```csharp
Task<FeishuApiResult<GetUserInfosResult>?> GetUserByDepartmentIdAsync(
    [Query("department_id")] string department_id,
    [Query("page_size")] int page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_id | string | ✅ | 部门ID |
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**：获取指定部门直属的用户信息列表。

---

### 更新用户

**函数名称**：更新用户

**函数签名**：
```csharp
Task<FeishuApiResult<CreateOrUpdateUserResult>?> UpdateUserAsync(
    [Path] string user_id,
    [Body] UpdateUserRequest userModel,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| user_id | string | ✅ | 用户ID |
| userModel | UpdateUserRequest | ✅ | 用于更新的用户请求体 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user": {
      "user_id": "ou_xxx",
      "name": "张三（已更新）"
    }
  }
}
```

**说明**：更新通讯录中指定用户的信息，包括名称、邮箱、手机号、所属部门以及自定义字段等信息。

---

### 创建用户

**函数名称**：创建用户

**函数签名**：
```csharp
Task<FeishuApiResult<CreateOrUpdateUserResult>?> CreateUserAsync(
    [Body] CreateUserRequest userModel,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    [Query("client_token")] string? client_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| userModel | CreateUserRequest | ✅ | 创建的用户请求体 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| client_token | string | ⚪ | 幂等判断令牌 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user": {
      "user_id": "ou_xxx",
      "name": "李四",
      "mobile": "+86-13900139000"
    }
  }
}
```

**说明**：向通讯录创建一个用户（该动作可以理解为员工入职）。成功创建用户后，系统会以短信或邮件的形式向用户发送邀请，用户在同意邀请后方可访问企业或团队。

**代码示例**：
```csharp
public async Task CreateNewUserAsync()
{
    var request = new CreateUserRequest
    {
        User = new UserCreateInfo
        {
            Name = "李四",
            Mobile = "+86-13900139000",
            DepartmentIds = ["od-xxx"]
        }
    };

    var result = await _userClient.CreateUserAsync(request);
    if (result?.Code == 0)
    {
        Console.WriteLine($"用户创建成功，ID: {result.Data?.User?.UserId}");
    }
}
```

---

### 更新用户ID

**函数名称**：更新用户ID

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UpdateUserIdAsync(
    [Path] string user_id,
    [Body] UpdateUserIdRequest updateUserId,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| user_id | string | ✅ | 用户ID |
| updateUserId | UpdateUserIdRequest | ✅ | 自定义新的用户user_id |
| user_id_type | string | ⚪ | 用户ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**：更新用户ID。自定义新的用户user_id，长度不能超过64字符。

---

### 通过手机号/邮箱获取用户

**函数名称**：通过手机号/邮箱获取用户

**函数签名**：
```csharp
Task<FeishuApiResult<UserQueryListResult>?> GetBatchUsersAsync(
    [Body] UserQueryRequest queryRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| queryRequest | UserQueryRequest | ✅ | 查询参数请求体 |
| user_id_type | string | ⚪ | 用户ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user_list": [
      {
        "user_id": "ou_xxx",
        "status": {"is_frozen": false}
      }
    ]
  }
}
```

**说明**：通过手机号或邮箱获取一个或多个用户的ID（包括user_id、open_id、union_id）与状态信息。

---

### 搜索用户

**函数名称**：搜索用户

**函数签名**：
```csharp
Task<FeishuApiResult<UserSearchListResult>?> GetUsersByKeywordAsync(
    [Query("query")] string query,
    [Query("page_size")] int page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| query | string | ✅ | 搜索关键词 |
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**：通过用户名关键词搜索其他用户的信息，包括用户头像、用户名、用户所在部门、用户user_id以及open_id。

---

### 删除用户

**函数名称**：删除用户

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteUserByIdAsync(
    [Path] string user_id,
    [Body] DeleteSettingsRequest deleteSettingsRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| user_id | string | ✅ | 用户ID |
| deleteSettingsRequest | DeleteSettingsRequest | ✅ | 用户删除参数请求体 |
| user_id_type | string | ⚪ | 用户ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**：从通讯录内删除一个指定用户（该动作可以理解为员工离职），删除时可通过请求参数将用户所有的群组、文档、日程和应用等数据转让至他人。

---

### 恢复用户

**函数名称**：恢复用户

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> ResurrectUserByIdAsync(
    [Path] string user_id,
    [Body] ResurrectUserRequest resurrectUserRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| user_id | string | ✅ | 用户ID |
| resurrectUserRequest | ResurrectUserRequest | ✅ | 恢复已删除用户操作的请求体 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**：用于恢复已删除用户（已离职的成员）。

---

### 退出登录

**函数名称**：退出登录

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> LogoutAsync(
    [Body] LogoutRequest logoutRequest,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| logoutRequest | LogoutRequest | ✅ | 退出登录请求体 |
| user_id_type | string | ⚪ | 用户ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**：该接口用于退出用户的登录态。

---

### 获取JSAPI票据

**函数名称**：获取JSAPI票据

**函数签名**：
```csharp
Task<FeishuApiResult<TicketData>?> GetJsTicketAsync(CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "ticket": "xxx",
    "expire_in": 7200
  }
}
```

**说明**：用于返回调用JSAPI临时调用凭证，使用该凭证调用JSAPI时，请求不会被拦截。
