# 角色管理

## 接口名称：IFeishuV3RoleApi

## 功能描述：
飞书角色指的是团队成员的专业分工类别，如人事、行政、财务等，一个角色可由一名或多名成员组成。目前，角色主要用于应用审批场景。在审批管理后台，管理员可以选择某一角色作为审批人。例如，选择财务角色作为报销流程的审批人。这样做可以避免因成员离职变动导致的审批流失效的情况，角色内的其他成员可以继续完成审批，提高审批效率。

**接口详细文档请参见：**[飞书开放平台文档](https://open.feishu.cn/document/server-docs/contact-v3/functional_role/resource-introduction)

## 函数列表：

| 序号 | 函数名称 | HTTP方法 | 功能描述 |
|------|----------|----------|----------|
| 1 | CreateRoleAsync | POST | 创建角色 |
| 2 | UpdateRoleAsync | PUT | 修改角色名称 |
| 3 | DeleteRoleByIdAsync | DELETE | 删除指定角色 |

## 函数详细内容：

---

### 1. 创建角色

- **函数名称**：
```csharp
Task<FeishuApiResult<RoleCreateResult>> CreateRoleAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Body] RoleRequest roleRequest,
    CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌，用于身份鉴权 |
  | roleRequest | RoleRequest | 是 | 创建角色请求体，包含角色名称等信息 |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "role": {
      "role_id": "role_123456",
      "role_name": "财务审批",
      "description": "负责财务相关审批流程",
      "member_count": 0,
      "create_time": "2023-01-01T00:00:00Z",
      "update_time": "2023-01-01T00:00:00Z"
    }
  }
}
```

- **说明**：
  - 角色名称在租户内必须唯一
  - 创建时角色成员数量为0，需要后续添加成员
  - 角色主要用于审批流程的审批人设置
  - 创建后可以立即用于审批配置

- **代码示例**：
```csharp
// 创建角色示例
var roleRequest = new RoleRequest
{
    RoleName = "财务审批",
    Description = "负责财务相关审批流程，包括报销、付款等"
};

var result = await feishuApi.CreateRoleAsync(
    "your_tenant_access_token",
    roleRequest
);

if (result.Code == 0)
{
    var role = result.Data.Role;
    Console.WriteLine($"角色创建成功，ID: {role.RoleId}");
    Console.WriteLine($"角色名称: {role.RoleName}");
    Console.WriteLine($"描述: {role.Description}");
}
```

---

### 2. 修改角色

- **函数名称**：
```csharp
Task<FeishuNullDataApiResult> UpdateRoleAsync(
  [Token][Header("Authorization")] string tenant_access_token,
  [Path] string role_id,
  [Body] RoleRequest roleRequest,
  CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌，用于身份鉴权 |
  | role_id | string | 是 | 要修改的角色 ID |
  | roleRequest | RoleRequest | 是 | 修改角色请求体，包含新的角色名称等信息 |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

- **说明**：
  - 只能修改角色名称和描述等基本信息
  - 不能通过此接口修改角色成员
  - 角色名称修改后，所有使用该角色的审批流程会同步更新
  - 修改操作会记录更新时间

- **代码示例**：
```csharp
// 修改角色示例
var updateRequest = new RoleRequest
{
    RoleName = "高级财务审批",
    Description = "负责大额财务相关审批流程"
};

var result = await feishuApi.UpdateRoleAsync(
    "your_tenant_access_token",
    "role_123456",
    updateRequest
);

if (result.Code == 0)
{
    Console.WriteLine("角色修改成功");
}
else
{
    Console.WriteLine($"修改失败: {result.Msg}");
}
```

---

### 3. 删除角色

- **函数名称**：
```csharp
Task<FeishuNullDataApiResult> DeleteRoleByIdAsync(
  [Token][Header("Authorization")] string tenant_access_token,
  [Path] string role_id,
  CancellationToken cancellationToken = default)
```

- **认证**：需要 tenant_access_token（应用访问令牌）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用访问令牌，用于身份鉴权 |
  | role_id | string | 是 | 要删除的角色 ID |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

- **说明**：
  - 删除角色会自动移除所有角色成员
  - 如果该角色正在被审批流程使用，需要先更新审批配置
  - 删除操作不可逆，请谨慎操作
  - 建议在删除前确认没有业务流程依赖该角色

- **代码示例**：
```csharp
// 删除角色示例
var result = await feishuApi.DeleteRoleByIdAsync(
    "your_tenant_access_token",
    "role_123456"
);

if (result.Code == 0)
{
    Console.WriteLine("角色删除成功");
}
else
{
    Console.WriteLine($"删除失败: {result.Msg}");
}
```

---

## 角色管理最佳实践

### 角色创建流程建议

```csharp
// 完整的角色管理示例流程
public class RoleManagementExample
{
    private readonly IFeishuV3RoleApi _roleApi;
    
    public RoleManagementExample(IFeishuV3RoleApi roleApi)
    {
        _roleApi = roleApi;
    }
    
    // 创建常用业务角色
    public async Task CreateStandardRolesAsync()
    {
        var roles = new[]
        {
            new { Name = "财务审批", Desc = "负责财务相关审批流程" },
            new { Name = "人事审批", Desc = "负责人事相关审批流程" },
            new { Name = "行政审批", Desc = "负责行政相关审批流程" },
            new { Name = "技术审批", Desc = "负责技术相关审批流程" }
        };
        
        foreach (var role in roles)
        {
            var request = new RoleRequest
            {
                RoleName = role.Name,
                Description = role.Desc
            };
            
            var result = await _roleApi.CreateRoleAsync(
                "your_tenant_access_token", 
                request
            );
            
            if (result.Code == 0)
            {
                Console.WriteLine($"创建角色成功: {role.Name} (ID: {result.Data.Role.RoleId})");
            }
            else
            {
                Console.WriteLine($"创建角色失败: {role.Name} - {result.Msg}");
            }
        }
    }
    
    // 更新角色信息
    public async Task UpdateRoleAsync(string roleId, string newName, string newDescription)
    {
        var request = new RoleRequest
        {
            RoleName = newName,
            Description = newDescription
        };
        
        var result = await _roleApi.UpdateRoleAsync(
            "your_tenant_access_token",
            roleId,
            request
        );
        
        Console.WriteLine(result.Code == 0 ? "角色更新成功" : $"角色更新失败: {result.Msg}");
    }
    
    // 删除角色前检查依赖
    public async Task<bool> CanDeleteRoleAsync(string roleId)
    {
        // 在实际应用中，这里应该检查是否有审批流程在使用该角色
        // 这里只是示例逻辑
        Console.WriteLine("检查角色依赖关系...");
        
        // 假设我们检查后发现可以删除
        return true;
    }
    
    // 安全删除角色
    public async Task SafeDeleteRoleAsync(string roleId)
    {
        // 检查是否可以删除
        if (!await CanDeleteRoleAsync(roleId))
        {
            Console.WriteLine("角色正在被使用，无法删除");
            return;
        }
        
        var result = await _roleApi.DeleteRoleByIdAsync(
            "your_tenant_access_token",
            roleId
        );
        
        if (result.Code == 0)
        {
            Console.WriteLine("角色删除成功");
        }
        else
        {
            Console.WriteLine($"角色删除失败: {result.Msg}");
        }
    }
}
```

### 角色使用场景示例

```csharp
// 审批流程中的角色使用示例
public class ApprovalWorkflowExample
{
    public async Task ConfigureApprovalWithRoleAsync()
    {
        // 步骤1: 创建财务审批角色
        var createRoleRequest = new RoleRequest
        {
            RoleName = "报销审批",
            Description = "负责员工报销申请的审批"
        };
        
        var roleResult = await _roleApi.CreateRoleAsync(
            "your_tenant_access_token",
            createRoleRequest
        );
        
        if (roleResult.Code != 0)
        {
            Console.WriteLine($"创建角色失败: {roleResult.Msg}");
            return;
        }
        
        var roleId = roleResult.Data.Role.RoleId;
        
        // 步骤2: 配置审批流程使用该角色
        // (这里需要调用审批相关的API，此处仅作示例)
        Console.WriteLine($"已创建报销审批角色，ID: {roleId}");
        Console.WriteLine("可以将此角色配置到报销审批流程中");
        
        // 步骤3: 当需要修改时，可以更新角色信息
        var updateRequest = new RoleRequest
        {
            RoleName = "财务报销审批",
            Description = "负责所有财务相关报销申请的审批，支持大额报销"
        };
        
        await _roleApi.UpdateRoleAsync("your_tenant_access_token", roleId, updateRequest);
    }
}
```

## 版本更新记录

| 版本 | 更新日期 | 更新内容 |
|------|----------|----------|
| 1.0.0 | 2025-11-20 | 初始版本，包含基本的角色管理功能接口 |
