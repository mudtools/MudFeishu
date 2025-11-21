# 用户组管理

## 功能描述：
用户组是飞书通讯录中基础实体之一，在用户组内可添加用户或部门资源。各类业务权限管控可以与用户组关联，从而实现高效便捷的成员权限管控。本 API 提供了完整的用户组管理功能，包括创建、查询、更新、删除用户组，以及查询用户所属的用户组等操作。

**接口详细文档请参见：**[飞书开放平台文档](https://open.feishu.cn/document/server-docs/contact-v3/group/overview)

## 函数列表

| 序号 | 函数名称 | HTTP方法 | 功能描述 |
|------|----------|----------|----------|
| 1 | CreateUserGroupAsync | POST | 创建用户组 |
| 2 | UpdateUserGroupAsync | PATCH | 更新用户组 |
| 3 | GetUserGroupInfoByIdAsync | GET | 通过用户组ID查询指定用户组的基本信息 |
| 4 | GetUserGroupsAsync | GET | 查询当前租户下的用户组列表 |
| 5 | GetUserBelongGroupsAsync | GET | 查询指定用户所属的用户组列表 |
| 6 | DeleteUserGroupByIdAsync | DELETE | 删除指定用户组 |

---

## 创建用户组

- **函数名称**：
```csharp
Task<FeishuApiResult<UserGroupCreateResult>> CreateUserGroupAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Body] UserGroupInfoRequest groupInfoRequest,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌，用于身份鉴权 |
  | groupInfoRequest | UserGroupInfoRequest | 是 | 创建用户组请求体，包含用户组名称、描述等信息 |
  | user_id_type | string? | 否 | 用户 ID 类型 |
  | department_id_type | string? | 否 | 部门 ID 类型，默认为 Consts.Department_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "group_id": "group_123456",
    "name": "测试用户组"
  }
}
```

- **说明**：
  - 用户组创建成功后会返回用户组 ID
  - 用户组名称在租户内必须唯一
  - 支持普通用户组和动态用户组两种类型

- **代码示例**：
```csharp
// 创建用户组示例
var request = new UserGroupInfoRequest
{
    Name = "研发部门用户组",
    Description = "负责研发项目的用户组",
    Type = 1 // 普通用户组
};

var result = await feishuApi.CreateUserGroupAsync(
    "your_tenant_access_token",
    request,
    user_id_type: "open_id",
    department_id_type: "department_id"
);

if (result.Code == 0)
{
    Console.WriteLine($"用户组创建成功，ID: {result.Data.GroupId}");
}
```

---

## 更新用户组

- **函数名称**：
```csharp
Task<FeishuNullDataApiResult> UpdateUserGroupAsync(
   [Token][Header("Authorization")] string tenant_access_token,
   [Path] string group_id,
   [Body] UserGroupUpdateRequest groupUpdateRequest,
   [Query("user_id_type")] string? user_id_type = null,
   [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
   CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌 |
  | group_id | string | 是 | 用户组 ID |
  | groupUpdateRequest | UserGroupUpdateRequest | 是 | 更新用户组请求体 |
  | user_id_type | string? | 否 | 用户 ID 类型 |
  | department_id_type | string? | 否 | 部门 ID 类型 |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

- **说明**：
  - 只能更新用户组的基本信息，如名称、描述等
  - 不能通过此接口修改用户组成员
  - 动态用户组的更新规则可能与普通用户组不同

- **代码示例**：
```csharp
// 更新用户组示例
var updateRequest = new UserGroupUpdateRequest
{
    Name = "更新后的用户组名称",
    Description = "更新后的描述信息"
};

var result = await feishuApi.UpdateUserGroupAsync(
    "your_tenant_access_token",
    "group_123456",
    updateRequest
);

if (result.Code == 0)
{
    Console.WriteLine("用户组更新成功");
}
```

---

## 查询指定用户组信息

- **函数名称**：
```csharp
Task<FeishuApiResult<UserGroupQueryResult>> GetUserGroupInfoByIdAsync(
  [Token][Header("Authorization")] string user_access_token,
  [Path] string group_id,
  [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
  [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
  CancellationToken cancellationToken = default)
```

- **认证**：需要 user_access_token（用户访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | user_access_token | string | 是 | 用户访问令牌 |
  | group_id | string | 是 | 用户组 ID |
  | user_id_type | string? | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | department_id_type | string? | 否 | 部门 ID 类型，默认为 Consts.Department_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "group": {
      "group_id": "group_123456",
      "name": "测试用户组",
      "description": "这是一个测试用户组",
      "type": 1,
      "member_count": 10,
      "member_type": "user",
      "create_time": "2023-01-01T00:00:00Z",
      "update_time": "2023-01-01T00:00:00Z"
    }
  }
}
```

- **说明**：
  - 返回用户组的详细信息，包括成员数量、创建时间等
  - 需要用户访问令牌，表示当前用户对该用户组有查看权限

- **代码示例**：
```csharp
// 查询用户组信息示例
var result = await feishuApi.GetUserGroupInfoByIdAsync(
    "your_user_access_token",
    "group_123456",
    user_id_type: "open_id"
);

if (result.Code == 0)
{
    var group = result.Data.Group;
    Console.WriteLine($"用户组名称: {group.Name}");
    Console.WriteLine($"成员数量: {group.MemberCount}");
    Console.WriteLine($"创建时间: {group.CreateTime}");
}
```

---

## 查询用户组列表

- **函数名称**：
```csharp
Task<FeishuApiResult<UserGroupListResult>> GetUserGroupsAsync(
 [Token][Header("Authorization")] string tenant_access_token,
 [Query("page_size")] int page_size = 10,
 [Query("page_token")] string? page_token = null,
 [Query("type")] int type = 1,
 CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌 |
  | page_size | int | 否 | 分页大小，默认 10 |
  | page_token | string? | 否 | 分页标记，首次请求不填 |
  | type | int | 否 | 用户组类型：1-普通用户组，2-动态用户组，默认 1 |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "groups": [
      {
        "group_id": "group_123456",
        "name": "测试用户组",
        "type": 1,
        "member_count": 10,
        "member_type": "user"
      }
    ],
    "page_token": "next_page_token",
    "has_more": true
  }
}
```

- **说明**：
  - 支持分页查询，通过 page_token 获取下一页数据
  - 可以按用户组类型过滤：普通用户组或动态用户组
  - 返回的列表包含用户组的基本信息

- **代码示例**：
```csharp
// 分页查询用户组列表示例
var pageSize = 20;
var pageToken = "";
var hasMore = true;

while (hasMore)
{
    var result = await feishuApi.GetUserGroupsAsync(
        "your_tenant_access_token",
        page_size: pageSize,
        page_token: pageToken,
        type: 1 // 查询普通用户组
    );

    if (result.Code == 0)
    {
        foreach (var group in result.Data.Groups)
        {
            Console.WriteLine($"用户组: {group.Name} (ID: {group.GroupId})");
        }

        hasMore = result.Data.HasMore;
        pageToken = result.Data.PageToken;
    }
    else
    {
        hasMore = false;
    }
}
```

---

## 查询用户所属的用户组

- **函数名称**：
```csharp
Task<FeishuApiResult<UserBelongGroupListResult>> GetUserBelongGroupsAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Query("member_id")] string member_id,
    [Query("member_id_type")] string? member_id_type = null,
    [Query("group_type")] int? group_type = null,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("type")] int type = 1,
    CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌 |
  | member_id | string | 是 | 成员 ID，ID 类型与 member_id_type 一致 |
  | member_id_type | string? | 否 | 成员 ID 类型 |
  | group_type | int? | 否 | 用户组类型：1-普通用户组，2-动态用户组 |
  | page_size | int | 否 | 分页大小，默认 10 |
  | page_token | string? | 否 | 分页标记，首次请求不填 |
  | type | int | 否 | 类型参数，默认 1 |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "groups": [
      {
        "group_id": "group_123456",
        "name": "研发部门",
        "type": 1,
        "member_count": 10
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

- **说明**：
  - 可以查询指定用户或部门所属的所有用户组
  - 支持按用户组类型过滤
  - 支持分页查询
  - member_id 可以是用户 ID 或部门 ID

- **代码示例**：
```csharp
// 查询用户所属用户组示例
var result = await feishuApi.GetUserBelongGroupsAsync(
    "your_tenant_access_token",
    member_id: "user_123456",
    member_id_type: "open_id",
    group_type: 1, // 普通用户组
    page_size: 50
);

if (result.Code == 0)
{
    Console.WriteLine($"用户所属的用户组数量: {result.Data.Groups.Count}");
    
    foreach (var group in result.Data.Groups)
    {
        Console.WriteLine($"- {group.Name} ({group.GroupId})");
    }
}
```

---

## 删除用户组

- **函数名称**：
```csharp
Task<FeishuNullDataApiResult> DeleteUserGroupByIdAsync(
  [Token][Header("Authorization")] string tenant_access_token,
  [Path] string group_id,
  CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌 |
  | group_id | string | 是 | 需删除的用户组 ID |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

- **说明**：
  - 删除操作不可逆，请谨慎操作
  - 删除用户组会同时解除该用户组与所有成员的关联
  - 如果该用户组正在被其他业务使用，可能会影响相关功能

- **代码示例**：
```csharp
// 删除用户组示例
var result = await feishuApi.DeleteUserGroupByIdAsync(
    "your_tenant_access_token",
    "group_123456"
);

if (result.Code == 0)
{
    Console.WriteLine("用户组删除成功");
}
else
{
    Console.WriteLine($"删除失败: {result.Msg}");
}
```

---

## 版本更新记录

| 版本 | 更新日期 | 更新内容 |
|------|----------|----------|
| 1.0.0 | 2025-11-20 | 初始版本，包含所有用户组管理功能接口 |
