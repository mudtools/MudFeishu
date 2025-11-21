# 角色成员管理

## 接口名称：IFeishuV3RoleMemberApi

## 功能描述：
角色成员是指角色内添加的一个或多个用户。可以将角色设置为审批流程的审批人，这样该角色内的成员均可处理审批。同时，每一个角色成员都可以设置管理范围，以便指定不同成员管理不同的部门。本 API 提供了完整的角色成员管理功能，包括成员的添加、删除、管理范围设置以及查询等功能。

**接口详细文档请参见：**[飞书开放平台文档](https://open.feishu.cn/document/server-docs/contact-v3/functional_role-member/resource-introduction)

## 函数列表

| 序号 | 函数名称 | HTTP方法 | 功能描述 |
|------|----------|----------|----------|
| 1 | BatchAddMemberAsync | POST | 批量添加角色成员 |
| 2 | BatchAddMembersSopesAsync | POST | 批量设置成员管理范围 |
| 3 | GetMembersSopesAsync | GET | 查询指定成员管理范围 |
| 4 | GetMembersAsync | GET | 查询角色所有成员 |
| 5 | DeleteMembersByRoleIdAsync | DELETE | 批量删除角色成员 |

---

## 批量添加角色成员

- **函数名称**：
```csharp
Task<FeishuApiResult<RoleAssignmentResult>> BatchAddMemberAsync(
             [Token][Header("Authorization")] string tenant_access_token,
             [Path] string role_id,
             [Body] RoleMembersRequest roleMembersRequest,
             [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
             CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌，用于身份鉴权 |
  | role_id | string | 是 | 角色 ID |
  | roleMembersRequest | RoleMembersRequest | 是 | 角色成员的用户 ID 列表请求体 |
  | user_id_type | string? | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "role_assignments": [
      {
        "role_id": "role_123456",
        "user_id": "user_789012",
        "assignment_time": "2023-01-01T00:00:00Z"
      },
      {
        "role_id": "role_123456",
        "user_id": "user_345678",
        "assignment_time": "2023-01-01T00:00:00Z"
      }
    ],
    "failed_users": []
  }
}
```

- **说明**：
  - 支持批量添加多个成员到同一个角色
  - 已存在的成员不会重复添加
  - 返回成功和失败的成员列表
  - 添加后成员即可参与该角色的审批工作

- **代码示例**：
```csharp
// 批量添加角色成员示例
var roleMembersRequest = new RoleMembersRequest
{
    UserIds = new[] { "user_789012", "user_345678", "user_111111" }
};

var result = await feishuApi.BatchAddMemberAsync(
    "your_tenant_access_token",
    "role_123456",
    roleMembersRequest,
    user_id_type: "open_id"
);

if (result.Code == 0)
{
    Console.WriteLine($"成功添加 {result.Data.RoleAssignments.Count} 个成员");
    
    if (result.Data.FailedUsers?.Length > 0)
    {
        Console.WriteLine($"添加失败的成员: {string.Join(", ", result.Data.FailedUsers)}");
    }
}
```

---

## 批量设置成员管理范围

- **函数名称**：
```csharp
Task<FeishuApiResult<RoleAssignmentResult>> BatchAddMembersSopesAsync(
           [Token][Header("Authorization")] string tenant_access_token,
           [Path] string role_id,
           [Body] RoleMembersScopeRequest membersScopeRequest,
           [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
           [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
           CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌，用于身份鉴权 |
  | role_id | string | 是 | 角色 ID |
  | membersScopeRequest | RoleMembersScopeRequest | 是 | 角色成员管理范围请求体 |
  | user_id_type | string? | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | department_id_type | string? | 否 | 部门 ID 类型，默认为 Consts.Department_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "role_assignments": [
      {
        "role_id": "role_123456",
        "user_id": "user_789012",
        "department_ids": ["dept_001", "dept_002"],
        "assignment_time": "2023-01-01T00:00:00Z"
      }
    ],
    "failed_users": []
  }
}
```

- **说明**：
  - 管理范围指成员可以管理的部门范围
  - 未设置管理范围的成员可以管理所有部门
  - 支持为不同成员设置不同的管理范围
  - 管理范围设置后会立即生效

- **代码示例**：
```csharp
// 批量设置成员管理范围示例
var membersScopeRequest = new RoleMembersScopeRequest
{
    MemberScopes = new[]
    {
        new RoleMemberScope
        {
            UserId = "user_789012",
            DepartmentIds = new[] { "dept_001", "dept_002" } // 管理研发部和产品部
        },
        new RoleMemberScope
        {
            UserId = "user_345678",
            DepartmentIds = new[] { "dept_003" } // 管理市场部
        }
    }
};

var result = await feishuApi.BatchAddMembersSopesAsync(
    "your_tenant_access_token",
    "role_123456",
    membersScopeRequest,
    user_id_type: "open_id",
    department_id_type: "open_department_id"
);

if (result.Code == 0)
{
    Console.WriteLine($"成功设置 {result.Data.RoleAssignments.Count} 个成员的管理范围");
}
```

---

## 查询指定成员管理范围

- **函数名称**：
```csharp
Task<FeishuApiResult<RoleMemberScopeResult>> GetMembersSopesAsync(
          [Token][Header("Authorization")] string tenant_access_token,
          [Path] string role_id,
          [Path] string member_id,
          [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
          [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
          CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌，用于身份鉴权 |
  | role_id | string | 是 | 角色 ID |
  | member_id | string | 是 | 角色成员的用户 ID |
  | user_id_type | string? | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | department_id_type | string? | 否 | 部门 ID 类型，默认为 Consts.Department_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "member": {
      "role_id": "role_123456",
      "user_id": "user_789012",
      "department_ids": ["dept_001", "dept_002"],
      "assignment_time": "2023-01-01T00:00:00Z",
      "user_info": {
        "name": "张三",
        "email": "zhangsan@example.com"
      }
    }
  }
}
```

- **说明**：
  - 返回指定成员的详细管理范围信息
  - 包含成员的基本用户信息
  - 如果未设置管理范围，department_ids 可能为空或包含全部部门
  - 用于权限验证和范围控制

- **代码示例**：
```csharp
// 查询指定成员管理范围示例
var result = await feishuApi.GetMembersSopesAsync(
    "your_tenant_access_token",
    "role_123456",
    "user_789012",
    user_id_type: "open_id",
    department_id_type: "open_department_id"
);

if (result.Code == 0)
{
    var member = result.Data.Member;
    Console.WriteLine($"成员: {member.UserInfo?.Name} ({member.UserId})");
    
    if (member.DepartmentIds?.Length > 0)
    {
        Console.WriteLine($"管理范围: {string.Join(", ", member.DepartmentIds)}");
    }
    else
    {
        Console.WriteLine("管理范围: 所有部门");
    }
}
```

---

## 查询角色所有成员

- **函数名称**：
```csharp
Task<FeishuApiResult<RoleMemberScopeResult>> GetMembersAsync(
          [Token][Header("Authorization")] string tenant_access_token,
          [Path] string role_id,
          [Query("page_size")] int page_size = 10,
          [Query("page_token")] string? page_token = null,
          [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
          [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
          CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌，用于身份鉴权 |
  | role_id | string | 是 | 角色 ID |
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
    "members": [
      {
        "role_id": "role_123456",
        "user_id": "user_789012",
        "department_ids": ["dept_001", "dept_002"],
        "assignment_time": "2023-01-01T00:00:00Z",
        "user_info": {
          "name": "张三",
          "email": "zhangsan@example.com",
          "avatar": "https://example.com/avatar.jpg"
        }
      },
      {
        "role_id": "role_123456",
        "user_id": "user_345678",
        "department_ids": [],
        "assignment_time": "2023-01-01T00:00:00Z",
        "user_info": {
          "name": "李四",
          "email": "lisi@example.com"
        }
      }
    ],
    "page_token": "next_page_token",
    "has_more": true
  }
}
```

- **说明**：
  - 支持分页查询角色下的所有成员
  - 返回每个成员的详细信息和管理范围
  - 包含成员的用户基本信息
  - 权限不足时可能返回空列表

- **代码示例**：
```csharp
// 分页查询角色所有成员示例
var pageSize = 20;
var pageToken = "";
var hasMore = true;
var allMembers = new List<RoleMemberInfo>();

while (hasMore)
{
    var result = await feishuApi.GetMembersAsync(
        "your_tenant_access_token",
        "role_123456",
        page_size: pageSize,
        page_token: pageToken,
        user_id_type: "open_id",
        department_id_type: "open_department_id"
    );

    if (result.Code == 0)
    {
        allMembers.AddRange(result.Data.Members);
        
        Console.WriteLine($"本页成员数量: {result.Data.Members.Count}");
        
        foreach (var member in result.Data.Members)
        {
            var scope = member.DepartmentIds?.Length > 0 
                ? string.Join(", ", member.DepartmentIds) 
                : "所有部门";
            Console.WriteLine($"- {member.UserInfo?.Name}: {scope}");
        }

        hasMore = result.Data.HasMore;
        pageToken = result.Data.PageToken;
    }
    else
    {
        hasMore = false;
    }
}

Console.WriteLine($"总共获取到 {allMembers.Count} 个成员");
```

---

## 批量删除角色成员

- **函数名称**：
```csharp
Task<FeishuApiResult<RoleAssignmentResult>> DeleteMembersByRoleIdAsync(
         [Token][Header("Authorization")] string tenant_access_token,
         [Path] string role_id,
         [Body] RoleMembersRequest roleMembersRequest,
         [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌，用于身份鉴权 |
  | role_id | string | 是 | 角色 ID |
  | roleMembersRequest | RoleMembersRequest | 是 | 需删除的角色成员的用户 ID 列表请求体 |
  | user_id_type | string? | 否 | 用户 ID 类型，默认为 Consts.User_Id_Type |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "role_assignments": [
      {
        "role_id": "role_123456",
        "user_id": "user_789012",
        "assignment_time": "2023-01-01T00:00:00Z"
      }
    ],
    "failed_users": ["user_not_exist"]
  }
}
```

- **说明**：
  - 支持批量删除多个角色成员
  - 删除后成员将无法参与该角色的审批工作
  - 返回成功和失败的成员列表
  - 删除操作不可逆，请谨慎操作

- **代码示例**：
```csharp
// 批量删除角色成员示例
var deleteRequest = new RoleMembersRequest
{
    UserIds = new[] { "user_789012", "user_111111" }
};

var result = await feishuApi.DeleteMembersByRoleIdAsync(
    "your_tenant_access_token",
    "role_123456",
    deleteRequest,
    user_id_type: "open_id"
);

if (result.Code == 0)
{
    Console.WriteLine($"成功删除 {result.Data.RoleAssignments.Count} 个成员");
    
    if (result.Data.FailedUsers?.Length > 0)
    {
        Console.WriteLine($"删除失败的成员: {string.Join(", ", result.Data.FailedUsers)}");
    }
}
```

---

## 角色成员管理最佳实践

### 完整的角色成员管理流程

```csharp
public class RoleMemberManagementExample
{
    private readonly IFeishuV3RoleMemberApi _roleMemberApi;
    
    public RoleMemberManagementExample(IFeishuV3RoleMemberApi roleMemberApi)
    {
        _roleMemberApi = roleMemberApi;
    }
    
    // 设置财务角色成员及管理范围
    public async Task SetupFinanceRoleMembersAsync()
    {
        var roleId = "role_finance_123";
        var tenantToken = "your_tenant_access_token";
        
        // 步骤1: 添加财务团队成员到角色
        var financeMembers = new[] { "user_finance_1", "user_finance_2", "user_finance_3" };
        var addRequest = new RoleMembersRequest
        {
            UserIds = financeMembers
        };
        
        var addResult = await _roleMemberApi.BatchAddMemberAsync(
            tenantToken, roleId, addRequest, user_id_type: "open_id"
        );
        
        if (addResult.Code == 0)
        {
            Console.WriteLine($"成功添加 {addResult.Data.RoleAssignments.Count} 个财务成员");
        }
        
        // 步骤2: 设置不同成员的管理范围
        var scopeRequest = new RoleMembersScopeRequest
        {
            MemberScopes = new[]
            {
                new RoleMemberScope
                {
                    UserId = "user_finance_1",
                    DepartmentIds = new[] { "dept_rd", "dept_product" } // 研发部和产品部
                },
                new RoleMemberScope
                {
                    UserId = "user_finance_2",
                    DepartmentIds = new[] { "dept_sales", "dept_market" } // 销售部和市场部
                },
                new RoleMemberScope
                {
                    UserId = "user_finance_3",
                    DepartmentIds = new[] { "dept_hr", "dept_admin" } // 人事部和行政部
                }
            }
        };
        
        var scopeResult = await _roleMemberApi.BatchAddMembersSopesAsync(
            tenantToken, roleId, scopeRequest, 
            user_id_type: "open_id",
            department_id_type: "open_department_id"
        );
        
        if (scopeResult.Code == 0)
        {
            Console.WriteLine($"成功设置 {scopeResult.Data.RoleAssignments.Count} 个成员的管理范围");
        }
    }
    
    // 查询角色成员信息
    public async Task DisplayRoleMembersAsync(string roleId)
    {
        var result = await _roleMemberApi.GetMembersAsync(
            "your_tenant_access_token",
            roleId,
            page_size: 50,
            user_id_type: "open_id",
            department_id_type: "open_department_id"
        );
        
        if (result.Code == 0)
        {
            Console.WriteLine($"角色成员列表 (共 {result.Data.Members.Count} 人):");
            
            foreach (var member in result.Data.Members)
            {
                var scopeInfo = member.DepartmentIds?.Length > 0
                    ? $"管理范围: {string.Join(", ", member.DepartmentIds)}"
                    : "管理范围: 所有部门";
                    
                Console.WriteLine($"- {member.UserInfo?.Name} ({member.UserInfo?.Email}) - {scopeInfo}");
            }
        }
    }
    
    // 成员离职时的处理流程
    public async Task HandleMemberResignationAsync(string roleId, string userId)
    {
        // 查询成员当前的管理范围
        var memberInfo = await _roleMemberApi.GetMembersSopesAsync(
            "your_tenant_access_token",
            roleId,
            userId,
            user_id_type: "open_id"
        );
        
        if (memberInfo.Code == 0)
        {
            var member = memberInfo.Data.Member;
            Console.WriteLine($"即将移除成员: {member.UserInfo?.Name}");
            Console.WriteLine($"管理范围: {string.Join(", ", member.DepartmentIds ?? new string[0])}");
        }
        
        // 从角色中移除该成员
        var deleteRequest = new RoleMembersRequest
        {
            UserIds = new[] { userId }
        };
        
        var deleteResult = await _roleMemberApi.DeleteMembersByRoleIdAsync(
            "your_tenant_access_token",
            roleId,
            deleteRequest,
            user_id_type: "open_id"
        );
        
        if (deleteResult.Code == 0)
        {
            Console.WriteLine("成员已从角色中移除");
        }
    }
}
```

### 审批权限验证示例

```csharp
public class ApprovalPermissionChecker
{
    private readonly IFeishuV3RoleMemberApi _roleMemberApi;
    
    public ApprovalPermissionChecker(IFeishuV3RoleMemberApi roleMemberApi)
    {
        _roleMemberApi = roleMemberApi;
    }
    
    // 检查用户是否有权限审批指定部门的申请
    public async Task<bool> CheckApprovalPermissionAsync(string roleId, string userId, string departmentId)
    {
        try
        {
            var result = await _roleMemberApi.GetMembersSopesAsync(
                "your_tenant_access_token",
                roleId,
                userId,
                user_id_type: "open_id",
                department_id_type: "open_department_id"
            );
            
            if (result.Code != 0)
            {
                return false;
            }
            
            var member = result.Data.Member;
            
            // 如果没有设置管理范围，表示可以管理所有部门
            if (member.DepartmentIds == null || member.DepartmentIds.Length == 0)
            {
                return true;
            }
            
            // 检查是否在管理范围内
            return member.DepartmentIds.Contains(departmentId);
        }
        catch
        {
            return false;
        }
    }
    
    // 获取用户可以审批的所有部门
    public async Task<string[]> GetManageableDepartmentsAsync(string roleId, string userId)
    {
        var result = await _roleMemberApi.GetMembersSopesAsync(
            "your_tenant_access_token",
            roleId,
            userId,
            user_id_type: "open_id",
            department_id_type: "open_department_id"
        );
        
        if (result.Code == 0)
        {
            var member = result.Data.Member;
            
            // 如果没有设置管理范围，返回所有部门
            if (member.DepartmentIds == null || member.DepartmentIds.Length == 0)
            {
                // 这里可以调用部门API获取所有部门ID
                return new[] { "all_departments" };
            }
            
            return member.DepartmentIds;
        }
        
        return new string[0];
    }
}
```

## 版本更新记录

| 版本 | 更新日期 | 更新内容 |
|------|----------|----------|
| 1.0.0 | 2025-11-20 | 初始版本，包含完整的角色成员管理功能接口 |
