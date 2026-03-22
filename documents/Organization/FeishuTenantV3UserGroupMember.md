# 租户V3用户组成员管理 - FeishuTenantV3UserGroupMember

## 接口名称
**租户V3用户组成员管理 - (IFeishuTenantV3UserGroupMember)**

## 功能描述
本接口提供飞书用户组成员的管理功能，适用于租户应用场景。支持用户组成员的添加、批量添加、查询、移除和批量移除等操作。

用户组内可以添加部门或用户，部门和用户均属于用户组成员。使用用户组成员API，可以在用户组内添加、移除、查询成员。

## 参考文档
- [飞书官方文档 - 用户组成员概述](https://open.feishu.cn/document/server-docs/contact-v3/group-member/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| AddMemberAsync | 添加成员 | 租户令牌 | POST |
| BatchAddMemberAsync | 批量添加成员 | 租户令牌 | POST |
| GetMemberListByGroupIdAsync | 获取成员列表 | 租户令牌 | GET |
| RemoveMemberAsync | 移除成员 | 租户令牌 | POST |
| BatchRemoveMemberAsync | 批量移除成员 | 租户令牌 | POST |

## 函数详细内容

### 添加成员

**函数名称**：添加成员

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> AddMemberAsync(
    [Path] string group_id,
    [Body] UserGroupMemberRequest groupMemberRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| group_id | string | ✅ | 用户组ID |
| groupMemberRequest | UserGroupMemberRequest | ✅ | 添加用户组成员请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：向指定的普通用户组内添加成员。

**代码示例**：
```csharp
public class UserGroupMemberService
{
    private readonly IFeishuTenantV3UserGroupMember _memberClient;

    public UserGroupMemberService(IFeishuTenantV3UserGroupMember memberClient)
    {
        _memberClient = memberClient;
    }

    public async Task AddUserToGroupAsync(string groupId, string userId)
    {
        var request = new UserGroupMemberRequest
        {
            MemberId = userId,
            MemberType = "user"
        };

        var result = await _memberClient.AddMemberAsync(groupId, request);
        if (result?.Code == 0)
        {
            Console.WriteLine("成员添加成功");
        }
    }
}
```

---

### 批量添加成员

**函数名称**：批量添加成员

**函数签名**：
```csharp
Task<FeishuApiResult<BatchAddMemberResult>?> BatchAddMemberAsync(
    [Path] string group_id,
    [Body] BatchMembersRequest groupMemberRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| group_id | string | ✅ | 用户组ID |
| groupMemberRequest | BatchMembersRequest | ✅ | 批量添加用户组成员请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "results": [
      {
        "member_id": "ou_xxx",
        "code": 0,
        "msg": "success"
      }
    ]
  }
}
```

**说明**：向指定的普通用户组内添加一个或多个成员。

**代码示例**：
```csharp
public async Task BatchAddUsersToGroupAsync(string groupId, List<string> userIds)
{
    var request = new BatchMembersRequest
    {
        Members = userIds.Select(id => new MemberItem 
        { 
            MemberId = id, 
            MemberType = "user" 
        }).ToList()
    };

    var result = await _memberClient.BatchAddMemberAsync(groupId, request);
    if (result?.Code == 0)
    {
        var successCount = result.Data?.Results?.Count(r => r.Code == 0) ?? 0;
        Console.WriteLine($"成功添加 {successCount} 个成员");
    }
}
```

---

### 获取成员列表

**函数名称**：获取成员列表

**函数签名**：
```csharp
Task<FeishuApiResult<MemberListRequest>?> GetMemberListByGroupIdAsync(
    [Path] string group_id,
    [Query("page_size")] int? page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    [Query("member_id_type")] string? member_id_type = Consts.User_Id_Type,
    [Query("member_type")] string? member_type = "user",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| group_id | string | ✅ | 用户组ID |
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
| member_id_type | string | ⚪ | 用户组成员ID类型，默认 `open_id` |
| member_type | string | ⚪ | 用户组成员类型，默认 `user` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "memberlist": [
      {
        "member_id": "ou_xxx",
        "member_type": "user"
      },
      {
        "member_id": "od_xxx",
        "member_type": "department"
      }
    ],
    "page_token": "xxx",
    "has_more": false
  }
}
```

**说明**：查询指定用户组内的成员列表，列表内主要包括成员ID信息。

---

### 移除成员

**函数名称**：移除成员

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> RemoveMemberAsync(
    [Path] string group_id,
    [Body] UserGroupMemberRequest groupMemberRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| group_id | string | ✅ | 用户组ID |
| groupMemberRequest | UserGroupMemberRequest | ✅ | 移除用户组成员请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：移除指定普通用户组内的某一成员。

---

### 批量移除成员

**函数名称**：批量移除成员

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> BatchRemoveMemberAsync(
    [Path] string group_id,
    [Body] BatchMembersRequest groupMemberRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| group_id | string | ✅ | 用户组ID |
| groupMemberRequest | BatchMembersRequest | ✅ | 批量移除用户组成员请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：从指定普通用户组内移除一个或多个成员。
