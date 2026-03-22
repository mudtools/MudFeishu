# 用户V1部门管理 - FeishuUserV1Departments

## 接口名称
**用户V1部门管理 - (IFeishuUserV1Departments)**

## 功能描述
本接口提供飞书企业组织架构中部门的管理功能，适用于用户应用场景。支持部门的创建、更新、删除、批量查询、条件筛选和搜索等操作。使用用户令牌访问，适合代表用户执行部门相关操作的场景。

部门是飞书组织架构里的基础实体，每个员工都归属于一个或多个部门。部门在飞书的身份标识包括 `department_id` 和 `open_department_id`。

## 参考文档
- [飞书官方文档 - 部门管理](https://open.feishu.cn/document/directory-v1/department/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateDepartmentAsync | 创建部门 | 用户令牌 | POST |
| UpdateDepartmentAsync | 更新部门 | 用户令牌 | PATCH |
| DeleteDepartmentByIdAsync | 删除部门 | 用户令牌 | DELETE |
| QueryDepartmentsAsync | 批量查询部门 | 用户令牌 | POST |
| QueryDepartmentsPageListAsync | 分页查询部门列表 | 用户令牌 | POST |
| SearchEmployeePageListAsync | 搜索部门 | 用户令牌 | POST |

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

**认证**：用户令牌

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
      "name": "产品研发部"
    }
  }
}
```

**说明**：
- 使用用户令牌时，默认为管理员用户，仅「人事管理模式」的管理员可操作
- 用户需要有相应的通讯录权限

**代码示例**：
```csharp
public class UserDepartmentService
{
    private readonly IFeishuUserV1Departments _departmentClient;

    public UserDepartmentService(IFeishuUserV1Departments departmentClient)
    {
        _departmentClient = departmentClient;
    }

    public async Task CreateDepartmentAsUserAsync()
    {
        var request = new DepartmentCreateUpdateRequest
        {
            Department = new DepartmentInfo
            {
                Name = "市场部",
                ParentDepartmentId = "0"
            }
        };

        var result = await _departmentClient.CreateDepartmentAsync(request);
        if (result?.Code == 0)
        {
            Console.WriteLine($"部门创建成功: {result.Data?.Department?.Name}");
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

**认证**：用户令牌

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

**认证**：用户令牌

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

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| departmentQueryRequest | DepartmentQueryRequest | ✅ | 部门查询参数请求体 |
| employee_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

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

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| filterSearchRequest | FilterSearchRequest | ✅ | 字段过滤查询条件请求体 |
| employee_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

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

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| pageSearchRequest | PageSearchRequest | ✅ | 分页查询参数请求体 |
| employee_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |
