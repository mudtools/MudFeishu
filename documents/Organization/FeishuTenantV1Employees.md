# 租户V1员工管理 - FeishuTenantV1Employees

## 接口名称
**租户V1员工管理 - (IFeishuTenantV1Employees)**

## 功能描述
本接口提供飞书企业员工（Employee）的完整生命周期管理功能，适用于租户应用场景。支持员工的创建、信息更新、离职、恢复、状态变更以及批量查询和搜索等操作。

员工指飞书企业内身份为「Employee」的成员，等同于通讯录OpenAPI中的「User」。员工在飞书的身份标识包括 `employee_id`、`open_id` 和 `union_id`，其中 `employee_id` 的值等同于通讯录中的 `user_id`。

## 参考文档
- [飞书官方文档 - 员工管理](https://open.feishu.cn/document/directory-v1/employee/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateEmployeeAsync | 创建员工 | 租户令牌 | POST |
| UpdateEmployeeAsync | 更新员工信息 | 租户令牌 | PATCH |
| DeleteEmployeeByIdAsync | 离职员工 | 租户令牌 | DELETE |
| ResurrectEmployeeAsync | 恢复已离职员工 | 租户令牌 | POST |
| ResignedEmployeeAsync | 办理员工待离职 | 租户令牌 | PATCH |
| RegularEmployeeAsync | 取消员工离职 | 租户令牌 | PATCH |
| QueryEmployeesAsync | 批量查询员工 | 租户令牌 | POST |
| QueryEmployeePageListAsync | 分页查询员工列表 | 租户令牌 | POST |
| SearchEmployeePageListAsync | 搜索员工 | 租户令牌 | POST |

## 函数详细内容

### 创建员工

**函数名称**：创建员工

**函数签名**：
```csharp
Task<FeishuApiResult<EmployeeCreateResult>?> CreateEmployeeAsync(
    [Body] EmployeeCreateRequest userModel,
    [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| userModel | EmployeeCreateRequest | ✅ | 创建的员工请求体 |
| employee_id_type | string | ⚪ | 用户ID类型，默认 `open_id` |
| department_id_type | string | ⚪ | 部门ID类型，默认 `open_department_id` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "employee": {
      "employee_id": "ou_xxx",
      "name": "张三",
      "mobile": "+86-13800138000"
    }
  }
}
```

**说明**：
- 支持传入姓名、手机号等信息，生成在职状态的员工对象
- 需要在应用通讯录授权范围内操作

**代码示例**：
```csharp
public class EmployeeService
{
    private readonly IFeishuTenantV1Employees _employeeClient;

    public EmployeeService(IFeishuTenantV1Employees employeeClient)
    {
        _employeeClient = employeeClient;
    }

    public async Task CreateNewEmployeeAsync()
    {
        var request = new EmployeeCreateRequest
        {
            Employee = new EmployeeCreateInfo
            {
                Name = "张三",
                Mobile = "+86-13800138000",
                DepartmentIds = ["od-xxx"],
                EmployeeType = "正式"
            }
        };

        var result = await _employeeClient.CreateEmployeeAsync(request);
        if (result?.Code == 0)
        {
            Console.WriteLine($"员工创建成功，ID: {result.Data?.Employee?.EmployeeId}");
        }
    }
}
```

---

### 更新员工信息

**函数名称**：更新员工信息

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UpdateEmployeeAsync(
    [Path] string employee_id,
    [Body] EmployeeUpdateRequest userModel,
    [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| employee_id | string | ✅ | 员工ID |
| userModel | EmployeeUpdateRequest | ✅ | 更新的员工请求体 |
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

**说明**：用于更新在职/离职员工的信息、冻结/恢复员工。未传递的参数不会进行更新。

---

### 离职员工

**函数名称**：离职员工

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteEmployeeByIdAsync(
    [Path] string employee_id,
    [Body] DeleteEmployeeRequest deleteEmployeeRequest,
    [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| employee_id | string | ✅ | 员工ID |
| deleteEmployeeRequest | DeleteEmployeeRequest | ✅ | 离职员工请求体 |
| employee_id_type | string | ⚪ | 用户ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：
- 使用 `tenant_access_token` 时，只能在当前应用的通讯录授权范围内离职员工
- 若员工归属于多个部门，应用需要有员工所有所属部门的权限，才能离职成功

---

### 恢复已离职员工

**函数名称**：恢复已离职员工

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> ResurrectEmployeeAsync(
    [Path] string employee_id,
    [Body] ResurrectEmployeeRequest resurrectEmployeeRequest,
    [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| employee_id | string | ✅ | 员工ID |
| resurrectEmployeeRequest | ResurrectEmployeeRequest | ✅ | 恢复离职员工请求体 |
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

**说明**：用于恢复已离职的成员，恢复已离职成员至在职状态。

---

### 办理员工待离职

**函数名称**：办理员工待离职

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> ResignedEmployeeAsync(
    [Path] string employee_id,
    [Body] ResignEmployeeRequest resignEmployeeRequest,
    [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| employee_id | string | ✅ | 员工ID |
| resignEmployeeRequest | ResignEmployeeRequest | ✅ | 在职员工流转到待离职请求体 |
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

**说明**：
- 用于为在职员工办理离职，将其更新为「待离职」状态
- 「待离职」员工不会自动离职，需要使用「离职员工」API操作离职和资源转交

---

### 取消员工离职

**函数名称**：取消员工离职

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> RegularEmployeeAsync(
    [Path] string employee_id,
    [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| employee_id | string | ✅ | 员工ID |
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

**说明**：用于为待离职员工取消离职，将其更新为「在职」状态。取消离职时会清空离职信息。

---

### 批量查询员工

**函数名称**：批量查询员工

**函数签名**：
```csharp
Task<FeishuApiResult<EmployeeListResult>?> QueryEmployeesAsync(
    [Body] EmployeeQueryRequest employeeQueryRequest,
    [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| employeeQueryRequest | EmployeeQueryRequest | ✅ | 员工查询请求体 |
| employee_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "employees": [
      {
        "employee_id": "ou-xxx",
        "name": "张三",
        "mobile": "+86-13800138000"
      }
    ]
  }
}
```

**说明**：用于批量根据员工的ID查询员工的详情，比如员工姓名、手机号、邮箱、部门等信息。

---

### 分页查询员工列表

**函数名称**：分页查询员工列表

**函数签名**：
```csharp
Task<FeishuApiResult<EmployeePageListResult>?> QueryEmployeePageListAsync(
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

---

### 搜索员工

**函数名称**：搜索员工

**函数签名**：
```csharp
Task<FeishuApiResult<EmployeePageListResult>?> SearchEmployeePageListAsync(
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

**说明**：用于搜索员工信息，如通过关键词搜索员工的名称、手机号、邮箱等信息。
