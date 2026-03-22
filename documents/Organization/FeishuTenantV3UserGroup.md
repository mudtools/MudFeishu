# 租户V3用户组管理 - FeishuTenantV3UserGroup

## 接口名称
**租户V3用户组管理 - (IFeishuTenantV3UserGroup)**

## 功能描述
本接口提供飞书用户组（User Group）的管理功能，适用于租户应用场景。支持用户组的创建、更新、查询和删除等操作。

用户组是飞书通讯录中基础实体之一，在用户组内可添加用户或部门资源。各类业务权限管控可以与用户组关联，从而实现高效便捷的成员权限管控。

## 参考文档
- [飞书官方文档 - 用户组概述](https://open.feishu.cn/document/server-docs/contact-v3/group/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateUserGroupAsync | 创建用户组 | 租户令牌 | POST |
| UpdateUserGroupAsync | 更新用户组 | 租户令牌 | PATCH |
| GetUserGroupInfoByIdAsync | 获取用户组详情 | 租户令牌 | GET |
| GetUserGroupsAsync | 获取用户组列表 | 租户令牌 | GET |
| GetUserBelongGroupsAsync | 获取成员所属用户组 | 租户令牌 | GET |
| DeleteUserGroupByIdAsync | 删除用户组 | 租户令牌 | DELETE |

## 函数详细内容

### 创建用户组

**函数名称**：创建用户组

**函数签名**：
```csharp
Task<FeishuApiResult<UserGroupCreateResult>?> CreateUserGroupAsync(
    [Body] UserGroupInfoRequest groupInfoRequest,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| groupInfoRequest | UserGroupInfoRequest | ✅ | 创建用户组请求体 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型，默认 `open_department_id` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "group": {
      "group_id": "xxx",
      "name": "项目A成员",
      "member_count": 0,
      "type": 1
    }
  }
}
```

**说明**：创建用户组。用户组是飞书通讯录中基础实体之一，在用户组内可添加用户或部门资源。各类业务权限管控可以与用户组关联，从而实现高效便捷的成员权限管控。

**代码示例**：
```csharp
public class UserGroupService
{
    private readonly IFeishuTenantV3UserGroup _userGroupClient;

    public UserGroupService(IFeishuTenantV3UserGroup userGroupClient)
    {
        _userGroupClient = userGroupClient;
    }

    public async Task CreateProjectGroupAsync()
    {
        var request = new UserGroupInfoRequest
        {
            Name = "项目A成员",
            Description = "负责项目A的所有成员"
        };

        var result = await _userGroupClient.CreateUserGroupAsync(request);
        if (result?.Code == 0)
        {
            Console.WriteLine($"用户组创建成功，ID: {result.Data?.Group?.GroupId}");
        }
    }
}
```

---

### 更新用户组

**函数名称**：更新用户组

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UpdateUserGroupAsync(
    [Path] string group_id,
    [Body] UserGroupUpdateRequest groupUpdateRequest,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| group_id | string | ✅ | 用户组ID |
| groupUpdateRequest | UserGroupUpdateRequest | ✅ | 更新用户组请求体 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：更新用户组。

---

### 获取用户组详情

**函数名称**：获取用户组详情

**函数签名**：
```csharp
Task<FeishuApiResult<UserGroupQueryResult>?> GetUserGroupInfoByIdAsync(
    [Path] string group_id,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| group_id | string | ✅ | 用户组ID |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "group": {
      "group_id": "xxx",
      "name": "项目A成员",
      "member_count": 10,
      "type": 1
    }
  }
}
```

**说明**：通过用户组ID查询指定用户组的基本信息，包括用户组名称、成员数量和类型等。

---

### 获取用户组列表

**函数名称**：获取用户组列表

**函数签名**：
```csharp
Task<FeishuApiResult<UserGroupListResult>?> GetUserGroupsAsync(
    [Query("page_size")] int? page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    [Query("type")] int? type = 1,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
| type | int | ⚪ | 用户组类型，1：普通用户组，2：动态用户组，默认1 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "grouplist": [
      {
        "group_id": "xxx",
        "name": "项目A成员",
        "member_count": 10,
        "type": 1
      }
    ],
    "page_token": "xxx",
    "has_more": false
  }
}
```

**说明**：查询当前租户下的用户组列表，列表内包含用户组的ID、名字、成员数量和类型等信息。

---

### 获取成员所属用户组

**函数名称**：获取成员所属用户组

**函数签名**：
```csharp
Task<FeishuApiResult<UserBelongGroupListResult>?> GetUserBelongGroupsAsync(
    [Query("member_id")] string member_id,
    [Query("member_id_type")] string? member_id_type = null,
    [Query("group_type")] int? group_type = null,
    [Query("page_size")] int? page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    [Query("type")] int? type = 1,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| member_id | string | ✅ | 成员ID |
| member_id_type | string | ⚪ | 成员ID类型 |
| group_type | int | ⚪ | 用户组类型，1：普通用户组，2：动态用户组 |
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
| type | int | ⚪ | 类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "grouplist": [
      {
        "group_id": "xxx",
        "name": "项目A成员"
      }
    ]
  }
}
```

**说明**：查询指定用户所属的用户组列表。

---

### 删除用户组

**函数名称**：删除用户组

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteUserGroupByIdAsync(
    [Path] string group_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| group_id | string | ✅ | 需删除的用户组ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除指定用户组。
