# 租户V1部门管理 - FeishuTenantV1Departments

## 接口名称
**租户V1部门管理 - (IFeishuTenantV1Departments)**

## 功能描述
本接口提供飞书企业组织架构中部门的管理功能，适用于租户应用场景。支持部门的创建、更新、删除、批量查询、条件筛选和搜索等操作。

部门是飞书组织架构里的基础实体，每个员工都归属于一个或多个部门。部门在飞书的身份标识包括 `department_id` 和 `open_department_id`。

## 参考文档
- [飞书官方文档 - 部门管理](https://open.feishu.cn/document/directory-v1/department/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateDepartmentAsync | 创建部门 | 租户令牌 | POST |
| UpdateDepartmentAsync | 更新部门 | 租户令牌 | PATCH |
| DeleteDepartmentByIdAsync | 删除部门 | 租户令牌 | DELETE |
| QueryDepartmentsAsync | 批量查询部门 | 租户令牌 | POST |
| QueryDepartmentsPageListAsync | 分页查询部门列表 | 租户令牌 | POST |
| SearchEmployeePageListAsync | 搜索部门 | 租户令牌 | POST |

## 函数详细内容

### 创建部门

**函数名称**：创建部门

**函数签名**：
```csharp
Task<FeishuApiResult<DepartmentCreateUpdateRequest>?> CreateDepartmentAsync(
    [Body] DepartmentCreateUpdateRequest departmentCreateRequest,
    [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| departmentCreateRequest | DepartmentCreateUpdateRequest | ✅ | 创建部门的请求体 |
| employee_id_type | string | ⚪ | 用户ID类型，默认 `open_id` |
| department_id_type | string | ⚪ | 部门ID类型，默认 `open_department_id` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "department": {
      "department_id": "od-5b1a1b1b1b1b1b1b",
      "name": "产品研发部",
      "parent_department_id": "0"
    }
  }
}
```

**说明**：
- 需要在应用通讯录权限范围内操作
- 部门名称在同一父部门下不能重复

**代码示例**：
```csharp
public class DepartmentService
{
    private readonly IFeishuTenantV1Departments _departmentClient;

    public DepartmentService(IFeishuTenantV1Departments departmentClient)
    {
        _departmentClient = departmentClient;
    }

    public async Task CreateNewDepartmentAsync()
    {
        var request = new DepartmentCreateUpdateRequest
        {
            Department = new DepartmentInfo
            {
                Name = "产品研发部",
                ParentDepartmentId = "0",
                LeaderEmployeeId = "ou_xxx"
            }
        };

        var result = await _departmentClient.CreateDepartmentAsync(request);
        if (result?.Code == 0)
        {
            Console.WriteLine($"部门创建成功，ID: {result.Data?.Department?.DepartmentId}");
        }
    }
}
```

---

### 更新部门

**函数名称**：更新部门

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UpdateDepartmentAsync(
    [Path] string department_id,
    [Body] DepartmentCreateUpdateRequest departmentUpdateRequest,
    [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_id | string | ✅ | 部门ID |
| departmentUpdateRequest | DepartmentCreateUpdateRequest | ✅ | 更新部门的请求体 |
| employee_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：仅更新显式传参的部分，未传递的参数保持原值。

**代码示例**：
```csharp
public async Task UpdateDepartmentNameAsync(string departmentId, string newName)
{
    var request = new DepartmentCreateUpdateRequest
    {
        Department = new DepartmentInfo
        {
            Name = newName
        }
    };

    var result = await _departmentClient.UpdateDepartmentAsync(departmentId, request);
    if (result?.Code == 0)
    {
        Console.WriteLine("部门名称更新成功");
    }
}
```

---

### 删除部门

**函数名称**：删除部门

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteDepartmentByIdAsync(
    [Path] string department_id,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_id | string | ✅ | 部门ID |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除部门前需要确保该部门下没有子部门和成员。

**代码示例**：
```csharp
public async Task DeleteDepartmentAsync(string departmentId)
{
    var result = await _departmentClient.DeleteDepartmentByIdAsync(departmentId);
    if (result?.Code == 0)
    {
        Console.WriteLine("部门删除成功");
    }
}
```

---

### 批量查询部门

**函数名称**：批量查询部门

**函数签名**：
```csharp
Task<FeishuApiResult<DepartmentListResult>?> QueryDepartmentsAsync(
    [Body] DepartmentQueryRequest departmentQueryRequest,
    [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| departmentQueryRequest | DepartmentQueryRequest | ✅ | 部门查询参数请求体 |
| employee_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "departments": [
      {
        "department_id": "od-xxx",
        "name": "产品研发部",
        "parent_department_id": "0",
        "leader_employee_id": "ou-xxx"
      }
    ]
  }
}
```

**代码示例**：
```csharp
public async Task QueryDepartmentsByIdsAsync(string[] departmentIds)
{
    var request = new DepartmentQueryRequest
    {
        DepartmentIds = departmentIds.ToList()
    };

    var result = await _departmentClient.QueryDepartmentsAsync(request);
    if (result?.Code == 0)
    {
        foreach (var dept in result.Data?.Departments ?? [])
        {
            Console.WriteLine($"部门: {dept.Name}, ID: {dept.DepartmentId}");
        }
    }
}
```

---

### 分页查询部门列表

**函数名称**：分页查询部门列表

**函数签名**：
```csharp
Task<FeishuApiResult<DepartmentPageListResult>?> QueryDepartmentsPageListAsync(
    [Body] FilterSearchRequest filterSearchRequest,
    [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| filterSearchRequest | FilterSearchRequest | ✅ | 字段过滤查询条件请求体 |
| employee_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [...],
    "page_token": "xxx",
    "has_more": true
  }
}
```

---

### 搜索部门

**函数名称**：搜索部门

**函数签名**：
```csharp
Task<FeishuApiResult<DepartmentPageListResult>?> SearchEmployeePageListAsync(
    [Body] PageSearchRequest pageSearchRequest,
    [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| pageSearchRequest | PageSearchRequest | ✅ | 分页查询参数请求体 |
| employee_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**：通过部门名称等关键词搜索部门信息。
