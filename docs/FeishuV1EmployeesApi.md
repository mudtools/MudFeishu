# 员工管理

## 功能描述
该接口提供了飞书员工管理的完整功能，包括员工的创建、更新、查询、离职、恢复等操作。员工指飞书企业内身份为「Employee」的成员，等同于通讯录OpenAPI中的「User」。员工在飞书的身份标识包括employee_id、open_id和union_id，其中employee_id的值等同于通讯录中的user_id，其余两个也和通讯录的User的值相同。

接口详细文档请参见：[飞书官方文档](https://open.feishu.cn/document/directory-v1/employee/overview)

## 函数列表

| 函数名称 | HTTP方法 | 功能描述 |
|---------|---------|---------|
| CreateEmployeeAsync | POST | 创建员工 |
| UpdateEmployeeAsync | PATCH | 更新员工信息 |
| DeleteEmployeeByIdAsync | DELETE | 离职员工 |
| ResurrectEmployeeAsync | POST | 恢复已离职员工 |
| ResignedEmployeeAsync | PATCH | 在职员工流转到待离职状态 |
| RegularEmployeeAsync | PATCH | 待离职员工取消离职 |
| QueryEmployeesAsync | POST | 批量查询员工详情 |
| QueryEmployeePageListAsync | POST | 分页查询员工列表 |
| SearchEmployeePageListAsync | POST | 搜索员工信息 |


## CreateEmployeeAsync
```csharp
Task<FeishuApiResult<EmployeeCreateResult>> CreateEmployeeAsync(
    [Token(TokenType.Both)][Header("Authorization")] string access_token,
    [Body] EmployeeCreateRequest userModel,
    [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：需要 tenant_access_token 或 user_access_token

**参数**：
- `access_token` (必填): 应用调用API时的访问凭证，用于身份鉴权
- `userModel` (必填): 创建员工的请求体
  - `employee`: 待创建员工对象
    - `name`: 姓名信息（可选）
    - `mobile`: 员工手机号（可选）
    - `custom_employee_id`: 自定义员工ID（可选）
    - `avatar_key`: 员工头像key（可选）
    - `email`: 工作邮箱（可选）
    - `enterprise_email`: 企业邮箱（可选）
    - `gender`: 性别（可选：0未知，1男，2女，3其他）
    - `leader_id`: 直属上级ID（可选）
    - `dotted_line_leader_ids`: 虚线上级ID列表（可选）
    - `work_country_or_region`: 工作地国家/地区码（可选）
    - `work_place_id`: 工作地点ID（可选）
    - `work_station`: 工位（可选）
    - `job_number`: 工号（可选）
    - `extension_number`: 分机号（可选）
    - `join_date`: 入职日期（可选）
    - `employment_type`: 员工类型（可选：1全职，2实习，3外包，4劳务，5顾问）
    - `job_title_id`: 职务ID（可选）
    - `custom_field_values`: 自定义字段（可选）
  - `options`: 接口拓展选项（可选）
- `employee_id_type` (可选): 用户ID类型
- `department_id_type` (可选): 部门ID类型
- `cancellationToken` (可选): 取消操作令牌

**响应**：
```json
// 成功响应示例
{
  "code": 0,
  "msg": "success",
  "data": {
    "employee_id": "7123456789012345678"
  }
}

// 错误响应示例
{
  "code": 400,
  "msg": "参数错误：手机号已存在"
}
```

**说明**：用于在企业下创建员工，支持传入姓名、手机号等信息，生成在职状态的员工对象。创建成功后返回新员工的employee_id。

**代码示例**：
```csharp
// 创建新员工示例
var createRequest = new EmployeeCreateRequest
{
    Employee = new EmployeeCreateInfo
    {
        Name = new EmployeeName { I18n = new Dictionary<string, string> { ["zh_cn"] = "张三" } },
        Mobile = "13800138000",
        Email = "zhangsan@example.com",
        EnterpriseEmail = "zhangsan@company.com",
        Gender = 1,
        JobNumber = "EMP001",
        JoinDate = "2024-01-15",
        EmploymentType = "1",
        CustomEmployeeId = "custom_emp_001"
    },
    Options = new EmployeeCreateOptions
    {
        // 可选的创建选项
    }
};

var result = await _employeeApi.CreateEmployeeAsync(
    access_token: "your_access_token",
    userModel: createRequest);

if (result.Code == 0)
{
    Console.WriteLine($"员工创建成功，员工ID: {result.Data?.EmployeeId}");
}
else
{
    Console.WriteLine($"创建失败: {result.Msg}");
}
```

---

## UpdateEmployeeAsync
```csharp
Task<FeishuNullDataApiResult> UpdateEmployeeAsync(
   [Token(TokenType.Both)][Header("Authorization")] string access_token,
   [Path] string employee_id,
   [Body] EmployeeUpdateRequest userModel,
   [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
   [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
   CancellationToken cancellationToken = default);
```

**认证**：需要 tenant_access_token 或 user_access_token

**参数**：
- `access_token` (必填): 应用调用API时的访问凭证
- `employee_id` (必填): 员工ID，用于指定要更新的员工
- `userModel` (必填): 更新员工的请求体
- `employee_id_type` (可选): 用户ID类型
- `department_id_type` (可选): 部门ID类型
- `cancellationToken` (可选): 取消操作令牌

**响应**：
```json
// 成功响应示例
{
  "code": 0,
  "msg": "success"
}
```

**说明**：用于更新在职/离职员工的信息、冻结/恢复员工。未传递的参数不会进行更新。

**代码示例**：
```csharp
// 更新员工信息示例
var updateRequest = new EmployeeUpdateRequest
{
    Employee = new EmployeeUpdateInfo
    {
        Name = new EmployeeName { I18n = new Dictionary<string, string> { ["zh_cn"] = "张三丰" } },
        Mobile = "13900139000",
        Email = "zhangsanfeng@example.com"
    }
};

var result = await _employeeApi.UpdateEmployeeAsync(
    access_token: "your_access_token",
    employee_id: "7123456789012345678",
    userModel: updateRequest);

if (result.Code == 0)
{
    Console.WriteLine("员工信息更新成功");
}
else
{
    Console.WriteLine($"更新失败: {result.Msg}");
}
```

---

## DeleteEmployeeByIdAsync
```csharp
Task<FeishuNullDataApiResult> DeleteEmployeeByIdAsync(
  [Token(TokenType.Both)][Header("Authorization")] string access_token,
  [Path] string employee_id,
  [Body] DeleteEmployeeRequest deleteEmployeeRequest,
  [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
  CancellationToken cancellationToken = default);
```

**认证**：支持 tenant_access_token 和 user_access_token

**参数**：
- `access_token` (必填): 应用调用API时的访问凭证
- `employee_id` (必填): 员工ID
- `deleteEmployeeRequest` (必填): 离职员工请求体
  - `options`: 离职员工接口拓展选项（可选）
- `employee_id_type` (可选): 用户ID类型
- `cancellationToken` (可选): 取消操作令牌

**响应**：
```json
// 成功响应示例
{
  "code": 0,
  "msg": "success"
}
```

**说明**：
- 本接口支持tenant_access_token和user_access_token
- 使用tenant_access_token时，只能在当前应用的通讯录授权范围内离职员工
- 若员工归属于多个部门，应用需要有员工所有所属部门的权限，才能离职成功
- 使用user_access_token时，默认为管理员用户，将校验管理员管理范围

**代码示例**：
```csharp
// 员工离职示例
var deleteRequest = new DeleteEmployeeRequest
{
    Options = new DeleteEmployeeOptions
    {
        // 离职选项配置
    }
};

var result = await _employeeApi.DeleteEmployeeByIdAsync(
    access_token: "your_access_token",
    employee_id: "7123456789012345678",
    deleteEmployeeRequest: deleteRequest);

if (result.Code == 0)
{
    Console.WriteLine("员工离职成功");
}
else
{
    Console.WriteLine($"离职失败: {result.Msg}");
}
```

---

## ResurrectEmployeeAsync
```csharp
Task<FeishuNullDataApiResult> ResurrectEmployeeAsync(
  [Token(TokenType.Both)][Header("Authorization")] string access_token,
  [Path] string employee_id,
  [Body] ResurrectEmployeeRequest resurrectEmployeeRequest,
  [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
  [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
  CancellationToken cancellationToken = default);
```

**认证**：需要 tenant_access_token 或 user_access_token

**参数**：
- `access_token` (必填): 应用调用API时的访问凭证
- `employee_id` (必填): 员工ID
- `resurrectEmployeeRequest` (必填): 恢复离职员工请求体
  - `employee`: 恢复员工选项参数
    - `department_ids`: 恢复后所属部门ID列表
    - `transfer_to_user_id`: 数据转移接收人ID（可选）
- `employee_id_type` (可选): 用户ID类型
- `department_id_type` (可选): 部门ID类型
- `cancellationToken` (可选): 取消操作令牌

**响应**：
```json
// 成功响应示例
{
  "code": 0,
  "msg": "success"
}
```

**说明**：用于恢复已离职的成员，恢复已离职成员至在职状态。

**代码示例**：
```csharp
// 恢复离职员工示例
var resurrectRequest = new ResurrectEmployeeRequest
{
    Employee = new ResurrectEmployeeOptions
    {
        DepartmentIds = new List<string> { "od-dept-001" },
        TransferToUserId = "ou_manager_001"
    }
};

var result = await _employeeApi.ResurrectEmployeeAsync(
    access_token: "your_access_token",
    employee_id: "7123456789012345678",
    resurrectEmployeeRequest: resurrectRequest);

if (result.Code == 0)
{
    Console.WriteLine("员工恢复成功");
}
else
{
    Console.WriteLine($"恢复失败: {result.Msg}");
}
```

---

## ResignedEmployeeAsync
```csharp
Task<FeishuNullDataApiResult> ResignedEmployeeAsync(
 [Token(TokenType.Both)][Header("Authorization")] string access_token,
 [Path] string employee_id,
 [Body] ResignEmployeeRequest resignEmployeeRequest,
 [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
 [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
 CancellationToken cancellationToken = default);
```

**认证**：需要 tenant_access_token 或 user_access_token

**参数**：
- `access_token` (必填): 应用调用API时的访问凭证
- `employee_id` (必填): 员工ID
- `resignEmployeeRequest` (必填): 在职员工流转到待离职请求体
  - `employee`: 在职员工流转到待离职选项参数
- `employee_id_type` (可选): 用户ID类型
- `department_id_type` (可选): 部门ID类型
- `cancellationToken` (可选): 取消操作令牌

**响应**：
```json
// 成功响应示例
{
  "code": 0,
  "msg": "success"
}
```

**说明**：
- 用于为在职员工办理离职，将其更新为「待离职」状态
- 「待离职」员工不会自动离职，需要使用「离职员工」API操作离职和资源转交
- 使用user_access_token时默认为管理员用户，仅「人事管理模式」的管理员可操作

**代码示例**：
```csharp
// 在职员工流转到待离职示例
var resignRequest = new ResignEmployeeRequest
{
    Employee = new ResignEmployeeOption
    {
        // 待离职选项配置
        ResignDate = "2024-12-31",
        Reason = "个人原因"
    }
};

var result = await _employeeApi.ResignedEmployeeAsync(
    access_token: "your_access_token",
    employee_id: "7123456789012345678",
    resignEmployeeRequest: resignRequest);

if (result.Code == 0)
{
    Console.WriteLine("员工已转为待离职状态");
}
else
{
    Console.WriteLine($"操作失败: {result.Msg}");
}
```

---

## RegularEmployeeAsync
```csharp
Task<FeishuNullDataApiResult> RegularEmployeeAsync(
         [Token(TokenType.Both)][Header("Authorization")] string access_token,
         [Path] string employee_id,
         [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
         [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
         CancellationToken cancellationToken = default);
```

**认证**：需要 tenant_access_token 或 user_access_token

**参数**：
- `access_token` (必填): 应用调用API时的访问凭证
- `employee_id` (必填): 员工ID
- `employee_id_type` (可选): 用户ID类型
- `department_id_type` (可选): 部门ID类型
- `cancellationToken` (可选): 取消操作令牌

**响应**：
```json
// 成功响应示例
{
  "code": 0,
  "msg": "success"
}
```

**说明**：
- 用于为待离职员工取消离职，将其更新为「在职」状态
- 取消离职时会清空离职信息
- 使用user_access_token时默认为管理员用户，仅「人事管理模式」的管理员可操作

**代码示例**：
```csharp
// 取消员工待离职状态示例
var result = await _employeeApi.RegularEmployeeAsync(
    access_token: "your_access_token",
    employee_id: "7123456789012345678");

if (result.Code == 0)
{
    Console.WriteLine("员工已恢复为在职状态");
}
else
{
    Console.WriteLine($"操作失败: {result.Msg}");
}
```

---

## QueryEmployeesAsync
```csharp
Task<FeishuApiResult<EmployeeListResult>> QueryEmployeesAsync(
    [Token(TokenType.Both)][Header("Authorization")] string access_token,
    [Body] EmployeeQueryRequest employeeQueryRequest,
    [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：需要 tenant_access_token 或 user_access_token

**参数**：
- `access_token` (必填): 应用调用API时的访问凭证
- `employeeQueryRequest` (必填): 员工查询请求体
  - `employee_ids`: 员工ID列表，与employee_id_type类型保持一致
  - `required_fields`: 需要查询的字段列表，将按照传递的字段列表返回有权限的行、列数据
- `employee_id_type` (可选): 用户ID类型
- `department_id_type` (可选): 部门ID类型
- `cancellationToken` (可选): 取消操作令牌

**响应**：
```json
// 成功响应示例
{
  "code": 0,
  "msg": "success",
  "data": {
    "employees": [
      {
        "employee_id": "7123456789012345678",
        "name": "张三",
        "mobile": "13800138000",
        "email": "zhangsan@example.com"
      }
    ],
    "abnormals": []
  }
}
```

**说明**：用于批量根据员工的ID查询员工的详情，比如员工姓名、手机号、邮箱、部门等信息。

**代码示例**：
```csharp
// 批量查询员工详情示例
var queryRequest = new EmployeeQueryRequest
{
    EmployeeIds = new List<string> 
    { 
        "7123456789012345678", 
        "7123456789012345679" 
    },
    RequiredFields = new List<string> 
    { 
        "employee_id", 
        "name", 
        "mobile", 
        "email", 
        "department_id" 
    }
};

var result = await _employeeApi.QueryEmployeesAsync(
    access_token: "your_access_token",
    employeeQueryRequest: queryRequest);

if (result.Code == 0)
{
    foreach (var employee in result.Data?.Employees ?? new List<EmployeeDetail>())
    {
        Console.WriteLine($"员工姓名: {employee.Name}, 手机号: {employee.Mobile}");
    }
}
else
{
    Console.WriteLine($"查询失败: {result.Msg}");
}
```

---

## QueryEmployeePageListAsync
```csharp
Task<FeishuApiResult<EmployeeListPageResult>> QueryEmployeePageListAsync(
   [Token(TokenType.Both)][Header("Authorization")] string access_token,
   [Body] EmployeeSearchRequest employeeQueryRequest,
   [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
   [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
   CancellationToken cancellationToken = default);
```

**认证**：需要 tenant_access_token 或 user_access_token

**参数**：
- `access_token` (必填): 应用调用API时的访问凭证
- `employeeQueryRequest` (必填): 员工查询请求体
  - `filter`: 查询条件，支持复杂的过滤条件
  - `required_fields`: 需要查询的字段列表（可选）
  - `page_request`: 分页参数
    - `page_token`: 分页标记
    - `page_size`: 页面大小
- `employee_id_type` (可选): 用户ID类型
- `department_id_type` (可选): 部门ID类型
- `cancellationToken` (可选): 取消操作令牌

**响应**：
```json
// 成功响应示例
{
  "code": 0,
  "msg": "success",
  "data": {
    "employees": [
      {
        "employee_id": "7123456789012345678",
        "name": "张三",
        "mobile": "13800138000"
      }
    ],
    "page_response": {
      "page_token": "next_page_token",
      "has_more": true,
      "total": 100
    },
    "abnormals": []
  }
}
```

**说明**：用于依据指定条件，分页批量获取符合条件的员工详情列表。

**代码示例**：
```csharp
// 分页查询员工列表示例
var searchRequest = new EmployeeSearchRequest
{
    Filter = new FieldFilter
    {
        // 构建查询条件，例如：查询技术部门的员工
        Conditions = new List<FilterCondition>
        {
            new FilterCondition
            {
                Field = "department_id",
                Operator = "eq",
                Value = "od-tech-dept"
            }
        }
    },
    RequiredFields = new List<string> 
    { 
        "employee_id", 
        "name", 
        "mobile", 
        "department_id" 
    },
    PageRequest = new PageRequest
    {
        PageSize = 20,
        PageToken = "" // 第一页不传token
    }
};

var result = await _employeeApi.QueryEmployeePageListAsync(
    access_token: "your_access_token",
    employeeQueryRequest: searchRequest);

if (result.Code == 0)
{
    var pageResult = result.Data;
    Console.WriteLine($"查询到 {pageResult?.Employees.Count} 条记录");
    
    if (pageResult?.Page?.HasMore == true)
    {
        Console.WriteLine($"还有更多数据，下一页token: {pageResult.Page.PageToken}");
    }
}
else
{
    Console.WriteLine($"查询失败: {result.Msg}");
}
```

---

## SearchEmployeePageListAsync
```csharp
Task<FeishuApiResult<EmployeeListPageResult>> SearchEmployeePageListAsync(
  [Token(TokenType.Both)][Header("Authorization")] string access_token,
  [Body] EmployeePageQueryRequest employeeQueryRequest,
  [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
  [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
  CancellationToken cancellationToken = default);
```

**认证**：需要 tenant_access_token 或 user_access_token

**参数**：
- `access_token` (必填): 应用调用API时的访问凭证
- `employeeQueryRequest` (必填): 员工查询请求体
  - `query`: 搜索关键词
  - `required_fields`: 需要查询的字段列表（可选）
  - `page_request`: 分页参数
- `employee_id_type` (可选): 用户ID类型
- `department_id_type` (可选): 部门ID类型
- `cancellationToken` (可选): 取消操作令牌

**响应**：
```json
// 成功响应示例
{
  "code": 0,
  "msg": "success",
  "data": {
    "employees": [
      {
        "employee_id": "7123456789012345678",
        "name": "张三",
        "mobile": "13800138000"
      }
    ],
    "page_response": {
      "page_token": "next_page_token",
      "has_more": false,
      "total": 1
    },
    "abnormals": []
  }
}
```

**说明**：用于搜索员工信息，如通过关键词搜索员工的名称、手机号、邮箱等信息。

**代码示例**：
```csharp
// 搜索员工信息示例
var pageQueryRequest = new EmployeePageQueryRequest
{
    Query = "张三", // 搜索关键词
    RequiredFields = new List<string> 
    { 
        "employee_id", 
        "name", 
        "mobile", 
        "email" 
    },
    PageRequest = new PageRequest
    {
        PageSize = 10,
        PageToken = ""
    }
};

var result = await _employeeApi.SearchEmployeePageListAsync(
    access_token: "your_access_token",
    employeeQueryRequest: pageQueryRequest);

if (result.Code == 0)
{
    var searchResult = result.Data;
    Console.WriteLine($"搜索到 {searchResult?.Employees.Count} 条匹配记录");
    
    foreach (var employee in searchResult?.Employees ?? new List<EmployeeDetail>())
    {
        Console.WriteLine($"找到员工: {employee.Name}, 手机: {employee.Mobile}");
    }
}
else
{
    Console.WriteLine($"搜索失败: {result.Msg}");
}
```

## 版本更新记录

| 版本 | 日期 | 更新内容 |
|------|------|----------|
| v1.0 | 2025-11-20 | 初始版本，包含完整的员工管理功能 |

