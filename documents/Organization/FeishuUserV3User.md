# 用户V3用户管理 - FeishuUserV3User

## 接口名称
**用户V3用户管理 - (IFeishuUserV3User)**

## 功能描述
本接口提供飞书通讯录V3版本用户的管理功能，适用于用户应用场景。支持用户的查询、搜索以及获取当前登录用户信息等操作。使用用户令牌访问，适合代表用户执行用户相关操作的场景。

飞书用户是飞书通讯录中的基础资源，对应企业组织架构中的成员实体。

## 参考文档
- [飞书官方文档 - 用户字段概述](https://open.feishu.cn/document/server-docs/contact-v3/user/field-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| GetUserInfoByIdAsync | 获取用户信息 | 用户令牌 | GET |
| GetUserByIdsAsync | 批量获取用户 | 用户令牌 | GET |
| GetUserByDepartmentIdAsync | 获取部门直属用户 | 用户令牌 | GET |
| UpdateUserAsync | 更新用户 | 用户令牌 | PATCH |
| GetUsersByKeywordAsync | 搜索用户 | 用户令牌 | GET |
| GetUserInfoAsync | 获取当前登录用户信息 | 用户令牌 | GET |

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

**认证**：用户令牌

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
      "department_ids": ["od-xxx"]
    }
  }
}
```

**说明**：获取通讯录中某一用户的信息，包括用户ID、名称、邮箱、手机号、状态以及所属部门等信息。

**代码示例**：
```csharp
public class UserV3Service
{
    private readonly IFeishuUserV3User _userClient;

    public UserV3Service(IFeishuUserV3User userClient)
    {
        _userClient = userClient;
    }

    public async Task GetUserInfoAsync(string userId)
    {
        var result = await _userClient.GetUserInfoByIdAsync(userId);
        if (result?.Code == 0)
        {
            Console.WriteLine($"用户名: {result.Data?.User?.Name}");
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

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| user_ids | string[] | ✅ | 用户ID数组 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

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

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_id | string | ✅ | 部门ID |
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

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

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| user_id | string | ✅ | 用户ID |
| userModel | UpdateUserRequest | ✅ | 用于更新的用户请求体 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

---

### 搜索用户

**函数名称**：搜索用户

**函数签名**：
```csharp
Task<FeishuApiResult<UserSearchListResult>?> GetUsersByKeywordAsync(
    [Query("query")] string query,
    [Query("page_size")] int? page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| query | string | ✅ | 搜索关键词 |
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
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
        "avatar": "https://xxx.com/avatar.png"
      }
    ]
  }
}
```

**说明**：通过用户名关键词搜索其他用户的信息，包括用户头像、用户名、用户所在部门、用户user_id以及open_id。

---

### 获取当前登录用户信息

**函数名称**：获取当前登录用户信息

**函数签名**：
```csharp
Task<FeishuApiResult<GetUserDataResult>?> GetUserInfoAsync(CancellationToken cancellationToken = default);
```

**认证**：用户令牌

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
    "user_id": "ou_xxx",
    "name": "张三",
    "email": "zhangsan@example.com"
  }
}
```

**说明**：通过 `user_access_token` 获取相关用户信息。

**代码示例**：
```csharp
public async Task GetCurrentUserInfoAsync()
{
    var result = await _userClient.GetUserInfoAsync();
    if (result?.Code == 0)
    {
        Console.WriteLine($"当前用户: {result.Data?.Name}");
    }
}
```
