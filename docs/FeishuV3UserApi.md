# 用户管理

## 接口名称：IFeishuV3UserApi

## 功能描述：
飞书用户是飞书通讯录中的基础资源，对应企业组织架构中的成员实体。本 API 提供了完整的用户管理功能，包括用户创建、查询、更新、删除、恢复等操作，以及用户搜索、ID批量获取、登录状态管理等高级功能。支持用户信息维护、部门关联管理、数据转移等业务场景。

**接口详细文档请参见：**[飞书开放平台文档](https://open.feishu.cn/document/server-docs/contact-v3/user/field-overview)

## 函数列表：

| 序号 | 函数名称 | HTTP方法 | 功能描述 |
|------|----------|----------|----------|
| 1 | CreateUserAsync | POST | 创建用户（员工入职） |
| 2 | UpdateUserAsync | PATCH | 更新用户信息 |
| 3 | UpdateUserIdAsync | PATCH | 更新用户ID |
| 4 | GetUserInfoByIdAsync | GET | 获取单个用户信息 |
| 5 | GetUserByIdsAsync | GET | 批量获取用户信息 |
| 6 | GetUserByDepartmentIdAsync | GET | 获取部门直属用户列表 |
| 7 | GetBatchUsersAsync | POST | 通过手机号/邮箱批量获取用户ID |
| 8 | GetUsersByKeywordAsync | GET | 通过关键词搜索用户 |
| 9 | DeleteUserByIdAsync | DELETE | 删除用户（员工离职） |
| 10 | ResurrectUserByIdAsync | POST | 恢复已删除用户 |
| 11 | GetUserInfoAsync | GET | 通过用户访问令牌获取用户信息 |
| 12 | LogoutAsync | POST | 用户退出登录 |
| 13 | GetJsTicketAsync | POST | 获取JSAPI临时调用凭证 |

## 函数详细内容：

---

### 1. 创建用户

- **函数名称**：
```csharp
Task<FeishuApiResult<CreateOrUpdateUserResult>> CreateUserAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Body] CreateUserRequest userModel,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    [Query("client_token")] string? client_token = null,
    CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌，用于身份鉴权 |
  | userModel | CreateUserRequest | 是 | 创建用户请求体，包含姓名、邮箱、手机号、部门等信息 |
  | user_id_type | string? | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | department_id_type | string? | 否 | 部门 ID 类型，默认为 Consts.Department_Id_Type |
  | client_token | string? | 否 | 幂等性控制token，避免重复创建 |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user": {
      "user_id": "user_123456",
      "open_id": "open_123456",
      "union_id": "union_123456",
      "name": "张三",
      "email": "zhangsan@example.com",
      "mobile": "+86138****1234",
      "department_ids": ["dept_123"],
      "status": {
        "is_activated": false,
        "is_frozen": false,
        "is_resigned": false
      }
    }
  }
}
```

- **说明**：
  - 创建成功后系统会自动发送邀请短信或邮件
  - 用户需要接受邀请后方可访问企业
  - 支持幂等性控制，避免重复创建
  - 可以同时将用户分配到多个部门

- **代码示例**：
```csharp
// 创建用户示例
var createUserRequest = new CreateUserRequest
{
    Name = "张三",
    Email = "zhangsan@example.com",
    Mobile = "+8613812345678",
    DepartmentIds = new[] { "dept_001", "dept_002" },
    Avatar = "https://example.com/avatar.jpg",
    Title = "软件工程师",
    Description = "负责前端开发工作"
};

var result = await feishuApi.CreateUserAsync(
    "your_tenant_access_token",
    createUserRequest,
    user_id_type: "open_id",
    department_id_type: "department_id",
    client_token: Guid.NewGuid().ToString()
);

if (result.Code == 0)
{
    Console.WriteLine($"用户创建成功，用户ID: {result.Data.User.UserId}");
}
```

---

### 2. 更新用户信息

- **函数名称**：
```csharp
Task<FeishuApiResult<CreateOrUpdateUserResult>> UpdateUserAsync(
    [Token(TokenType.Both)][Header("Authorization")] string access_token,
    [Path] string user_id,
    [Body] UpdateUserRequest userModel,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default)
```

- **认证**：支持 tenant_access_token 或 user_access_token
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | access_token | string | 是 | 访问令牌（支持应用或用户令牌） |
  | user_id | string | 是 | 用户 ID，类型与 user_id_type 一致 |
  | userModel | UpdateUserRequest | 是 | 更新用户信息请求体 |
  | user_id_type | string? | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | department_id_type | string? | 否 | 部门 ID 类型，默认为 Consts.Department_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user": {
      "user_id": "user_123456",
      "name": "张三",
      "email": "zhangsan_new@example.com",
      "mobile": "+86138****5678",
      "department_ids": ["dept_003"],
      "title": "高级软件工程师",
      "avatar": "https://example.com/new_avatar.jpg"
    }
  }
}
```

- **说明**：
  - 可以更新用户的所有基础信息
  - 部门变更会影响用户的权限和可见范围
  - 邮箱和手机号变更可能需要重新验证
  - 支持部分更新，只传需要修改的字段

- **代码示例**：
```csharp
// 更新用户信息示例
var updateRequest = new UpdateUserRequest
{
    Name = "张三（更新）",
    Email = "zhangsan_new@example.com",
    Title = "高级软件工程师",
    DepartmentIds = new[] { "dept_003" }
};

var result = await feishuApi.UpdateUserAsync(
    "your_access_token",
    "user_123456",
    updateRequest,
    user_id_type: "user_id"
);

if (result.Code == 0)
{
    Console.WriteLine($"用户信息更新成功，新邮箱: {result.Data.User.Email}");
}
```

---

### 3. 更新用户ID

- **函数名称**：
```csharp
Task<FeishuNullDataApiResult> UpdateUserIdAsync(
    [Token][Header("Authorization")] string user_access_token,
    [Path] string user_id,
    [Body] UpdateUserIdRequest updateUserId,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default)
```

- **认证**：需要 user_access_token（用户访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | user_access_token | string | 是 | 用户访问令牌 |
  | user_id | string | 是 | 当前用户 ID |
  | updateUserId | UpdateUserIdRequest | 是 | 更新用户ID请求体，包含新的user_id |
  | user_id_type | string? | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

- **说明**：
  - 新的用户ID长度不能超过64字符
  - 用户ID必须保证唯一性
  - 更新后原用户ID将失效
  - 此操作可能影响用户的历史数据访问

- **代码示例**：
```csharp
// 更新用户ID示例
var updateUserIdRequest = new UpdateUserIdRequest
{
    UserId = "new_user_id_12345"
};

var result = await feishuApi.UpdateUserIdAsync(
    "your_user_access_token",
    "old_user_id_12345",
    updateUserIdRequest
);

if (result.Code == 0)
{
    Console.WriteLine("用户ID更新成功");
}
```

---

### 4. 获取单个用户信息

- **函数名称**：
```csharp
Task<FeishuApiResult<GetUserInfoResult>> GetUserInfoByIdAsync(
    [Token(TokenType.Both)][Header("Authorization")] string access_token,
    [Path] string user_id,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default)
```

- **认证**：支持 tenant_access_token 或 user_access_token
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | access_token | string | 是 | 访问令牌（支持应用或用户令牌） |
  | user_id | string | 是 | 用户 ID |
  | user_id_type | string? | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | department_id_type | string? | 否 | 部门 ID 类型，默认为 Consts.Department_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user": {
      "user_id": "user_123456",
      "open_id": "open_123456",
      "union_id": "union_123456",
      "name": "张三",
      "en_name": "Zhang San",
      "email": "zhangsan@example.com",
      "mobile": "+8613812345678",
      "avatar": {
        "avatar_72": "https://example.com/avatar_72.png",
        "avatar_240": "https://example.com/avatar_240.png",
        "avatar_640": "https://example.com/avatar_640.png",
        "avatar_origin": "https://example.com/avatar.png"
      },
      "department_ids": ["dept_001"],
      "leader_user_id": "leader_123",
      "position": "软件工程师",
      "employee_type": 1,
      "status": {
        "is_activated": true,
        "is_frozen": false,
        "is_resigned": false,
        "is_exited": false
      },
      "orders": [
        {
          "department_id": "dept_001",
          "order": 1
        }
      ]
    }
  }
}
```

- **说明**：
  - 返回用户的完整详细信息
  - 包含用户状态、部门关系、职位等信息
  - 返回多种尺寸的头像链接
  - 权限不足时可能只返回部分信息

- **代码示例**：
```csharp
// 获取用户信息示例
var result = await feishuApi.GetUserInfoByIdAsync(
    "your_access_token",
    "user_123456",
    user_id_type: "open_id",
    department_id_type: "department_id"
);

if (result.Code == 0)
{
    var user = result.Data.User;
    Console.WriteLine($"用户姓名: {user.Name}");
    Console.WriteLine($"邮箱: {user.Email}");
    Console.WriteLine($"部门数量: {user.DepartmentIds.Length}");
    Console.WriteLine($"是否已激活: {user.Status.IsActivated}");
}
```

---

### 5. 批量获取用户信息

- **函数名称**：
```csharp
Task<FeishuApiResult<GetUserInfosResult>> GetUserByIdsAsync(
   [Token(TokenType.Both)][Header("Authorization")] string access_token,
   [Query("user_ids")] string[] user_ids,
   [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
   [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
   CancellationToken cancellationToken = default)
```

- **认证**：支持 tenant_access_token 或 user_access_token
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | access_token | string | 是 | 访问令牌（支持应用或用户令牌） |
  | user_ids | string[] | 是 | 用户ID数组，最多50个 |
  | user_id_type | string? | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | department_id_type | string? | 否 | 部门 ID 类型，默认为 Consts.Department_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "users": [
      {
        "user_id": "user_123456",
        "name": "张三",
        "email": "zhangsan@example.com",
        "department_ids": ["dept_001"]
      },
      {
        "user_id": "user_789012",
        "name": "李四",
        "email": "lisi@example.com",
        "department_ids": ["dept_002"]
      }
    ]
  }
}
```

- **说明**：
  - 一次最多可查询50个用户
  - 部分用户不存在或无权限访问时，仍会返回其他用户信息
  - 建议分批处理大量用户查询
  - 权限不足的用户信息可能不完整

- **代码示例**：
```csharp
// 批量获取用户信息示例
var userIds = new[] { "user_123456", "user_789012", "user_345678" };

var result = await feishuApi.GetUserByIdsAsync(
    "your_access_token",
    userIds,
    user_id_type: "open_id"
);

if (result.Code == 0)
{
    Console.WriteLine($"成功获取 {result.Data.Users.Count} 个用户信息");
    
    foreach (var user in result.Data.Users)
    {
        Console.WriteLine($"- {user.Name} ({user.UserId}): {user.Email}");
    }
}
```

---

### 6. 获取部门直属用户列表

- **函数名称**：
```csharp
Task<FeishuApiResult<GetUserInfosResult>> GetUserByDepartmentIdAsync(
 [Token(TokenType.Both)][Header("Authorization")] string access_token,
 [Query("department_id")] string department_id,
 [Query("page_size")] int page_size = 10,
 [Query("page_token")] string? page_token = null,
 [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
 [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
 CancellationToken cancellationToken = default)
```

- **认证**：支持 tenant_access_token 或 user_access_token
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | access_token | string | 是 | 访问令牌（支持应用或用户令牌） |
  | department_id | string | 是 | 部门 ID |
  | page_size | int | 否 | 分页大小，默认10，最大50 |
  | page_token | string? | 否 | 分页标记，首次请求不填 |
  | user_id_type | string? | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | department_id_type | string? | 否 | 部门 ID 类型，默认为 Consts.Department_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "users": [
      {
        "user_id": "user_123456",
        "name": "张三",
        "email": "zhangsan@example.com",
        "position": "部门经理"
      }
    ],
    "page_token": "next_page_token",
    "has_more": true
  }
}
```

- **说明**：
  - 只返回指定部门的直属用户，不包含子部门用户
  - 支持分页查询，使用page_token获取下一页
  - 返回用户在部门中的排序信息
  - 权限不足时可能返回空列表

- **代码示例**：
```csharp
// 获取部门用户列表示例
var pageSize = 20;
var pageToken = "";
var hasMore = true;

while (hasMore)
{
    var result = await feishuApi.GetUserByDepartmentIdAsync(
        "your_access_token",
        "dept_001",
        page_size: pageSize,
        page_token: pageToken
    );

    if (result.Code == 0)
    {
        Console.WriteLine($"部门 'dept_001' 的用户:");
        
        foreach (var user in result.Data.Users)
        {
            Console.WriteLine($"- {user.Name} ({user.Position})");
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

### 7. 通过手机号/邮箱批量获取用户ID

- **函数名称**：
```csharp
Task<FeishuApiResult<UserQueryListResult>> GetBatchUsersAsync(
  [Token][Header("Authorization")] string tenant_access_token,
  [Body] UserQueryRequest queryRequest,
  [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
  CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌 |
  | queryRequest | UserQueryRequest | 是 | 查询请求体，包含手机号和邮箱列表 |
  | user_id_type | string? | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "users": [
      {
        "user_id": "user_123456",
        "open_id": "open_123456",
        "union_id": "union_123456",
        "email": "zhangsan@example.com",
        "mobile": "+8613812345678",
        "status": {
          "is_activated": true,
          "is_frozen": false,
          "is_resigned": false
        }
      }
    ]
  }
}
```

- **说明**：
  - 一次最多可查询50个手机号或邮箱
  - 返回匹配用户的所有ID类型和状态信息
  - 对于已离职或冻结的用户也会返回相关信息
  - 不存在的联系方式不会在结果中出现

- **代码示例**：
```csharp
// 通过联系方式批量获取用户ID示例
var queryRequest = new UserQueryRequest
{
    Emails = new[] { "zhangsan@example.com", "lisi@example.com" },
    Mobiles = new[] { "+8613812345678", "+8613898765432" }
};

var result = await feishuApi.GetBatchUsersAsync(
    "your_tenant_access_token",
    queryRequest,
    user_id_type: "open_id"
);

if (result.Code == 0)
{
    Console.WriteLine($"找到 {result.Data.Users.Count} 个匹配用户:");
    
    foreach (var user in result.Data.Users)
    {
        Console.WriteLine($"- {user.Email}/{user.Mobile} -> OpenID: {user.OpenId}");
    }
}
```

---

### 8. 通过关键词搜索用户

- **函数名称**：
```csharp
Task<FeishuApiResult<UserSearchListResult>> GetUsersByKeywordAsync(
 [Token(TokenType.UserAccessToken)][Header("Authorization")] string user_access_token,
 [Query("query")] string query,
 [Query("page_size")] int page_size = 10,
 [Query("page_token")] string? page_token = null,
 CancellationToken cancellationToken = default)
```

- **认证**：需要 user_access_token（用户访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | user_access_token | string | 是 | 用户访问令牌 |
  | query | string | 是 | 搜索关键词，匹配用户名 |
  | page_size | int | 否 | 分页大小，默认10，最大50 |
  | page_token | string? | 否 | 分页标记，首次请求不填 |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "users": [
      {
        "user_id": "user_123456",
        "open_id": "open_123456",
        "name": "张三",
        "en_name": "Zhang San",
        "avatar": {
          "avatar_72": "https://example.com/avatar_72.png"
        },
        "department": {
          "department_id": "dept_001",
          "name": "技术部"
        }
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

- **说明**：
  - 搜索基于用户名进行模糊匹配
  - 需要用户令牌，表示以当前用户身份搜索
  - 只返回有权限查看的用户信息
  - 搜索范围受用户权限限制

- **代码示例**：
```csharp
// 用户搜索示例
var keyword = "张";

var result = await feishuApi.GetUsersByKeywordAsync(
    "your_user_access_token",
    keyword,
    page_size: 20
);

if (result.Code == 0)
{
    Console.WriteLine($"搜索 '{keyword}' 找到 {result.Data.Users.Count} 个用户:");
    
    foreach (var user in result.Data.Users)
    {
        Console.WriteLine($"- {user.Name} ({user.Department?.Name})");
    }
}
```

---

### 9. 删除用户

- **函数名称**：
```csharp
Task<FeishuNullDataApiResult> DeleteUserByIdAsync(
   [Token][Header("Authorization")] string user_access_token,
   [Path] string user_id,
   [Body] DeleteSettingsRequest deleteSettingsRequest,
   [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
   CancellationToken cancellationToken = default)
```

- **认证**：需要 user_access_token（用户访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | user_access_token | string | 是 | 用户访问令牌 |
  | user_id | string | 是 | 要删除的用户ID |
  | deleteSettingsRequest | DeleteSettingsRequest | 是 | 删除设置请求体，包含数据转移配置 |
  | user_id_type | string? | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

- **说明**：
  - 删除操作对应员工离职流程
  - 可以配置将用户的群组、文档、日程等数据转移给其他用户
  - 删除后用户无法再访问企业资源
  - 删除操作不可逆，请谨慎操作

- **代码示例**：
```csharp
// 删除用户示例
var deleteRequest = new DeleteSettingsRequest
{
    // 将用户的数据转移给指定用户
    TransferUserId = "manager_123456",
    // 转移的类型：群聊、文档、日程等
    TransferType = new[] { "chat", "doc", "calendar" }
};

var result = await feishuApi.DeleteUserByIdAsync(
    "your_user_access_token",
    "user_123456",
    deleteRequest
);

if (result.Code == 0)
{
    Console.WriteLine("用户删除成功，相关数据已转移");
}
```

---

### 10. 恢复已删除用户

- **函数名称**：
```csharp
Task<FeishuNullDataApiResult> ResurrectUserByIdAsync(
  [Token][Header("Authorization")] string tenant_access_token,
  [Path] string user_id,
  [Body] ResurrectUserRequest resurrectUserRequest,
  [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
  [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
  CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌 |
  | user_id | string | 是 | 要恢复的用户ID |
  | resurrectUserRequest | ResurrectUserRequest | 是 | 恢复用户请求体，包含部门和职位信息 |
  | user_id_type | string? | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | department_id_type | string? | 否 | 部门 ID 类型，默认为 Consts.Department_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

- **说明**：
  - 只能恢复已删除但未完全清理的用户
  - 恢复时需要重新分配部门和职位
  - 恢复后用户需要重新激活才能使用
  - 部分数据可能无法完全恢复

- **代码示例**：
```csharp
// 恢复用户示例
var resurrectRequest = new ResurrectUserRequest
{
    DepartmentIds = new[] { "dept_001" },
    Position = "软件工程师",
    Email = "zhangsan@example.com",
    Mobile = "+8613812345678"
};

var result = await feishuApi.ResurrectUserByIdAsync(
    "your_tenant_access_token",
    "user_123456",
    resurrectRequest,
    user_id_type: "user_id",
    department_id_type: "department_id"
);

if (result.Code == 0)
{
    Console.WriteLine("用户恢复成功，等待用户重新激活");
}
```

---

### 11. 获取当前用户信息

- **函数名称**：
```csharp
Task<FeishuApiResult<GetUserDataResult>> GetUserInfoAsync(
    [Token(TokenType.UserAccessToken)][Header("Authorization")] string user_access_token,
    CancellationToken cancellationToken = default)
```

- **认证**：需要 user_access_token（用户访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | user_access_token | string | 是 | 用户访问令牌 |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user": {
      "user_id": "user_123456",
      "open_id": "open_123456",
      "union_id": "union_123456",
      "name": "张三",
      "en_name": "Zhang San",
      "avatar_url": "https://example.com/avatar.png",
      "email": "zhangsan@example.com",
      "mobile": "+8613812345678",
      "tenant_key": "your_tenant_key"
    }
  }
}
```

- **说明**：
  - 获取当前令牌对应用户的基本信息
  - 包含租户信息和用户身份信息
  - 用于用户身份验证和个性化展示
  - 返回的信息相对基础，不包含部门等详细信息

- **代码示例**：
```csharp
// 获取当前用户信息示例
var result = await feishuApi.GetUserInfoAsync(
    "your_user_access_token"
);

if (result.Code == 0)
{
    var user = result.Data.User;
    Console.WriteLine($"当前用户: {user.Name}");
    Console.WriteLine($"邮箱: {user.Email}");
    Console.WriteLine($"租户: {user.TenantKey}");
}
```

---

### 12. 用户退出登录

- **函数名称**：
```csharp
Task<FeishuNullDataApiResult> LogoutAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Body] LogoutRequest logoutRequest,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌 |
  | logoutRequest | LogoutRequest | 是 | 退出登录请求体，包含用户ID |
  | user_id_type | string | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

- **说明**：
  - 退出指定用户的登录态
  - 用户需要重新登录才能访问应用
  - 不会影响其他设备的登录状态
  - 可用于强制用户重新登录

- **代码示例**：
```csharp
// 用户退出登录示例
var logoutRequest = new LogoutRequest
{
    UserId = "user_123456"
};

var result = await feishuApi.LogoutAsync(
    "your_tenant_access_token",
    logoutRequest
);

if (result.Code == 0)
{
    Console.WriteLine("用户已退出登录");
}
```

---

### 13. 获取JSAPI临时调用凭证

- **函数名称**：
```csharp
Task<FeishuApiResult<TicketData>> GetJsTicketAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌，格式为 "Bearer access_token" |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "ticket": "ticket_1234567890abcdef",
    "expires_in": 7200
  }
}
```

- **说明**：
  - 调用JSAPI时需要使用此临时凭证
  - ticket有有效期，一般为2小时
  - 过期后需要重新获取
  - 用于前端页面调用飞书JSAPI

- **代码示例**：
```csharp
// 获取JSAPI凭证示例
var result = await feishuApi.GetJsTicketAsync(
    "Bearer your_tenant_access_token"
);

if (result.Code == 0)
{
    var ticket = result.Data.Ticket;
    var expiresIn = result.Data.ExpiresIn;
    
    Console.WriteLine($"JSAPI凭证: {ticket}");
    Console.WriteLine($"有效期: {expiresIn} 秒");
    
    // 将ticket传递给前端使用
    // frontendJsApi.init(ticket);
}
```

---

## 版本更新记录

| 版本 | 更新日期 | 更新内容 |
|------|----------|----------|
| 1.0.0 | 2025-11-20 | 初始版本，包含完整的用户管理功能接口 |
