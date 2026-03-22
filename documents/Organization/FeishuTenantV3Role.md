# 租户V3角色管理 - FeishuTenantV3Role

## 接口名称
**租户V3角色管理 - (IFeishuTenantV3Role)**

## 功能描述
本接口提供飞书角色（Functional Role）的管理功能，适用于租户应用场景。支持角色的创建、修改和删除等操作。

飞书角色指的是团队成员的专业分工类别，如人事、行政、财务等，一个角色可由一名或多名成员组成。目前，角色主要用于应用审批场景。在审批管理后台，管理员可以选择某一角色作为审批人。例如，选择财务角色作为报销流程的审批人。这样做可以避免因成员离职变动导致的审批流失效的情况，角色内的其他成员可以继续完成审批，提高审批效率。

## 参考文档
- [飞书官方文档 - 角色资源介绍](https://open.feishu.cn/document/server-docs/contact-v3/functional_role/resource-introduction)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateRoleAsync | 创建角色 | 租户令牌 | POST |
| UpdateRoleAsync | 更新角色 | 租户令牌 | PUT |
| DeleteRoleByIdAsync | 删除角色 | 租户令牌 | DELETE |

## 函数详细内容

### 创建角色

**函数名称**：创建角色

**函数签名**：
```csharp
Task<FeishuApiResult<RoleCreateResult>?> CreateRoleAsync(
    [Body] RoleRequest roleRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| roleRequest | RoleRequest | ✅ | 创建角色请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "role": {
      "role_id": "xxx",
      "role_name": "财务审批人"
    }
  }
}
```

**说明**：创建一个角色，用于在审批流程中作为审批人。

**代码示例**：
```csharp
public class RoleService
{
    private readonly IFeishuTenantV3Role _roleClient;

    public RoleService(IFeishuTenantV3Role roleClient)
    {
        _roleClient = roleClient;
    }

    public async Task CreateFinanceRoleAsync()
    {
        var request = new RoleRequest
        {
            RoleName = "财务审批人"
        };

        var result = await _roleClient.CreateRoleAsync(request);
        if (result?.Code == 0)
        {
            Console.WriteLine($"角色创建成功，ID: {result.Data?.Role?.RoleId}");
        }
    }
}
```

---

### 更新角色

**函数名称**：更新角色

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UpdateRoleAsync(
    [Path] string role_id,
    [Body] RoleRequest roleRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| role_id | string | ✅ | 角色ID |
| roleRequest | RoleRequest | ✅ | 更新角色请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：修改指定角色的角色名称。

**代码示例**：
```csharp
public async Task UpdateRoleNameAsync(string roleId, string newName)
{
    var request = new RoleRequest
    {
        RoleName = newName
    };

    var result = await _roleClient.UpdateRoleAsync(roleId, request);
    if (result?.Code == 0)
    {
        Console.WriteLine("角色名称更新成功");
    }
}
```

---

### 删除角色

**函数名称**：删除角色

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteRoleByIdAsync(
    [Path] string role_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| role_id | string | ✅ | 角色ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除指定角色。

**代码示例**：
```csharp
public async Task DeleteRoleAsync(string roleId)
{
    var result = await _roleClient.DeleteRoleByIdAsync(roleId);
    if (result?.Code == 0)
    {
        Console.WriteLine("角色删除成功");
    }
}
```
