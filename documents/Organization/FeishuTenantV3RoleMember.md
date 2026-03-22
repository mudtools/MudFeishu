# 租户V3角色成员管理 - FeishuTenantV3RoleMember

## 接口名称
**租户V3角色成员管理 - (IFeishuTenantV3RoleMember)**

## 功能描述
本接口提供飞书角色成员的管理功能，适用于租户应用场景。支持角色成员的添加、管理范围设置、查询和删除等操作。

角色成员是指角色内添加的一个或多个用户。可以将角色设置为审批流程的审批人，这样该角色内的成员均可处理审批。同时，每一个角色成员都可以设置管理范围，以便指定不同成员管理不同的部门。

## 参考文档
- [飞书官方文档 - 角色成员资源介绍](https://open.feishu.cn/document/server-docs/contact-v3/functional_role-member/resource-introduction)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| BatchAddMemberAsync | 批量添加角色成员 | 租户令牌 | POST |
| BatchAddMembersSopesAsync | 批量设置成员管理范围 | 租户令牌 | POST |
| GetMembersSopesAsync | 获取成员管理范围 | 租户令牌 | GET |
| GetMembersAsync | 获取角色成员列表 | 租户令牌 | GET |
| DeleteMembersByRoleIdAsync | 批量删除角色成员 | 租户令牌 | DELETE |

## 函数详细内容

### 批量添加角色成员

**函数名称**：批量添加角色成员

**函数签名**：
```csharp
Task<FeishuApiResult<RoleAssignmentResult>?> BatchAddMemberAsync(
    [Path] string role_id,
    [Body] RoleMembersRequest roleMembersRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| role_id | string | ✅ | 角色ID |
| roleMembersRequest | RoleMembersRequest | ✅ | 角色成员的用户ID列表请求体 |
| user_id_type | string | ⚪ | 用户ID类型，默认 `open_id` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "assignment_result": [
      {
        "user_id": "ou_xxx",
        "code": 0
      }
    ]
  }
}
```

**说明**：在指定角色内添加一个或多个成员。

**代码示例**：
```csharp
public class RoleMemberService
{
    private readonly IFeishuTenantV3RoleMember _roleMemberClient;

    public RoleMemberService(IFeishuTenantV3RoleMember roleMemberClient)
    {
        _roleMemberClient = roleMemberClient;
    }

    public async Task AddMembersToRoleAsync(string roleId, List<string> userIds)
    {
        var request = new RoleMembersRequest
        {
            Members = userIds.Select(id => new RoleMember { UserId = id }).ToList()
        };

        var result = await _roleMemberClient.BatchAddMemberAsync(roleId, request);
        if (result?.Code == 0)
        {
            Console.WriteLine($"成功添加 {result.Data?.AssignmentResult?.Count} 个成员");
        }
    }
}
```

---

### 批量设置成员管理范围

**函数名称**：批量设置成员管理范围

**函数签名**：
```csharp
Task<FeishuApiResult<RoleAssignmentResult>?> BatchAddMembersSopesAsync(
    [Path] string role_id,
    [Body] RoleMembersScopeRequest membersScopeRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| role_id | string | ✅ | 角色ID |
| membersScopeRequest | RoleMembersScopeRequest | ✅ | 角色成员管理范围请求体 |
| user_id_type | string | ⚪ | 用户ID类型，默认 `open_id` |
| department_id_type | string | ⚪ | 部门ID类型，默认 `open_department_id` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "assignment_result": [
      {
        "user_id": "ou_xxx",
        "code": 0
      }
    ]
  }
}
```

**说明**：为指定角色内的一个或多个角色成员设置管理范围。管理范围是指角色成员可以管理的部门范围。

---

### 获取成员管理范围

**函数名称**：获取成员管理范围

**函数签名**：
```csharp
Task<FeishuApiResult<RoleMemberScopeResult>?> GetMembersSopesAsync(
    [Path] string role_id,
    [Path] string member_id,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| role_id | string | ✅ | 角色ID |
| member_id | string | ✅ | 角色成员的用户ID |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "role_member": {
      "user_id": "ou_xxx",
      "scope": {
        "type": "part",
        "department_ids": ["od-xxx"]
      }
    }
  }
}
```

**说明**：查询指定角色内的指定成员的管理范围。

---

### 获取角色成员列表

**函数名称**：获取角色成员列表

**函数签名**：
```csharp
Task<FeishuApiResult<RoleMemberScopeInfo>?> GetMembersAsync(
    [Path] string role_id,
    [Query("page_size")] int? page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| role_id | string | ✅ | 角色ID |
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "user_id": "ou_xxx",
        "scope": {
          "type": "all"
        }
      }
    ],
    "page_token": "xxx",
    "has_more": false
  }
}
```

**说明**：查询指定角色内的所有成员信息，包括成员的用户ID、管理范围。

---

### 批量删除角色成员

**函数名称**：批量删除角色成员

**函数签名**：
```csharp
Task<FeishuApiResult<RoleAssignmentResult>?> DeleteMembersByRoleIdAsync(
    [Path] string role_id,
    [Body] RoleMembersRequest roleMembersRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| role_id | string | ✅ | 角色ID |
| roleMembersRequest | RoleMembersRequest | ✅ | 需删除的角色成员的用户ID列表请求体 |
| user_id_type | string | ⚪ | 用户ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "assignment_result": [
      {
        "user_id": "ou_xxx",
        "code": 0
      }
    ]
  }
}
```

**说明**：在指定角色内删除一个或多个成员。
