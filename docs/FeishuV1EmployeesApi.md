# 飞书员工管理 API 文档

## 概述

员工指飞书企业内身份为「Employee」的成员，等同于通讯录OpenAPI中的「User」。员工在飞书的身份标识包括 employee_id、open_id 和 union_id，其中 employee_id 的值等同于通讯录中的 user_id，其余两个也和通讯录的 User 的值相同。

**版本**: v1  
**基础URL**: `https://open.feishu.cn/open-apis/directory/v1`  
**完整文档**: [https://open.feishu.cn/document/directory-v1/employee/overview](https://open.feishu.cn/document/directory-v1/employee/overview)

---

## 创建员工

### 接口名称
用于在企业下创建员工。支持传入姓名、手机号等信息，生成在职状态的员工对象。

### 飞飞书接口URL
```
https://open.feishu.cn/open-apis/directory/v1/employees
```

### 方法
POST

### 认证
需要访问凭证（tenant_access_token 或 user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| employee_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |
| userModel | EmployeeCreateRequest | Body | 是 | 创建的员工请求体 | 见下方示例 |

#### EmployeeCreateRequest 结构

```json
{
  "name": {
    "zh_cn": "张三",
    "en_us": "Zhang San"
  },
  "mobile": "13800138000",
  "email": "zhangsan@example.com",
  "department_ids": ["od-12345678"],
  "employee_type": 1,
  "allow_duplicate_email": false
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "employee_id": "emp_123456789"
  }
}
```

#### 错误响应
```json
{
  "code": 99991401,
  "msg": "access_token invalid"
}
```

### 代码示例

```csharp
var createRequest = new EmployeeCreateRequest
{
    Name = new Dictionary<string, string>
    {
        ["zh_cn"] = "张三",
        ["en_us"] = "Zhang San"
    },
    Mobile = "13800138000",
    Email = "zhangsan@example.com",
    DepartmentIds = new List<string> { "od-12345678" },
    EmployeeType = 1,
    AllowDuplicateEmail = false
};

var result = await _employeesApi.CreateEmployeeAsync(
    access_token,
    createRequest
);

if (result.Code == 0)
{
    var employeeId = result.Data?.EmployeeId;
    Console.WriteLine($"员工创建成功，ID: {employeeId}");
}
```

### 说明
- 员工类型：1-正式员工，2-实习生，3-外包人员
- 邮箱不能重复，除非设置 `allow_duplicate_email` 为 true
- 支持多语言姓名，使用字典格式存储

---

## 更新员工信息

### 接口名称
用于更新在职/离职员工的信息、冻结/恢复员工。未传递的参数不会进行更新。

### 飞飞书接口URL
```
https://open.feishu.cn/open-apis/directory/v1/employees/{employee_id}
```

### 方法
PATCH

### 认证
需要访问凭证（tenant_access_token 或 user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| employee_id | string | Path | 是 | 员工ID | "emp_123456789" |
| employee_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |
| userModel | EmployeeUpdateRequest | Body | 是 | 更新的员工请求体 | 见下方示例 |

#### EmployeeUpdateRequest 结构

```json
{
  "name": {
    "zh_cn": "李四",
    "en_us": "Li Si"
  },
  "mobile": "13900139000",
  "email": "lisi@example.com",
  "department_ids": ["od-12345678"],
  "status": 2
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success"
}
```

#### 错误响应
```json
{
  "code": 400,
  "msg": "Invalid employee_id"
}
```

### 代码示例

```csharp
var updateRequest = new EmployeeUpdateRequest
{
    Name = new Dictionary<string, string>
    {
        ["zh_cn"] = "李四",
        ["en_us"] = "Li Si"
    },
    Mobile = "13900139000",
    Email = "lisi@example.com",
    DepartmentIds = new List<string> { "od-12345678" },
    Status = 2 // 冻结状态
};

var result = await _employeesApi.UpdateEmployeeAsync(
    access_token,
    "emp_123456789",
    updateRequest
);

if (result.Code == 0)
{
    Console.WriteLine("员工信息更新成功");
}
```

### 说明
- `status` 字段：1-正常，2-冻结，3-离职
- 未传递的参数不会进行更新
- 支持部分更新，只需传递需要修改的字段

---

## 离职员工

### 接口名称
用于离职员工

### 飞飞书接口URL
```
https://open.feishu.cn/open-apis/directory/v1/employees/{employee_id}
```

### 方法
DELETE

### 认证
需要访问凭证（tenant_access_token 或 user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| employee_id | string | Path | 是 | 员工ID | "emp_123456789" |
| employee_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| deleteEmployeeRequest | DeleteEmployeeRequest | Body | 是 | 离职员工请求体 | 见下方示例 |

#### DeleteEmployeeRequest 结构

```json
{
  "transfer_groups_to": "emp_987654321",
  "keep_groups_active": false,
  "transfer_approvals_to": "emp_987654321",
  "resend_welcome_email": false
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success"
}
```

#### 错误响应
```json
{
  "code": 403,
  "msg": "No permission to delete employee"
}
```

### 代码示例

```csharp
var deleteRequest = new DeleteEmployeeRequest
{
    TransferGroupsTo = "emp_987654321",
    KeepGroupsActive = false,
    TransferApprovalsTo = "emp_987654321",
    ResendWelcomeEmail = false
};

var result = await _employeesApi.DeleteEmployeeByIdAsync(
    access_token,
    "emp_123456789",
    deleteRequest
);

if (result.Code == 0)
{
    Console.WriteLine("员工离职成功");
}
```

### 说明
- 使用 tenant_access_token 时，只能在当前应用的通讯录授权范围内离职员工
- 若员工归属于多个部门，应用需要有员工所有所属部门的权限
- 支持资源转交，包括群组和审批权限

---

## 恢复离职员工

### 接口名称
用于恢复已离职的成员，恢复已离职成员至在职状态

### 飞飞书接口URL
```
https://open.feishu.cn/open-apis/directory/v1/employees/{employee_id}/resurrect
```

### 方法
POST

### 认证
需要访问凭证（tenant_access_token 或 user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| employee_id | string | Path | 是 | 员工ID | "emp_123456789" |
| employee_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |
| resurrectEmployeeRequest | ResurrectEmployeeRequest | Body | 是 | 恢复离职员工请求体 | 见下方示例 |

#### ResurrectEmployeeRequest 结构

```json
{
  "department_ids": ["od-12345678"],
  "keep_original_departments": true
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success"
}
```

#### 错误响应
```json
{
  "code": 404,
  "msg": "Employee not found"
}
```

### 代码示例

```csharp
var resurrectRequest = new ResurrectEmployeeRequest
{
    DepartmentIds = new List<string> { "od-12345678" },
    KeepOriginalDepartments = true
};

var result = await _employeesApi.ResurrectEmployeeAsync(
    access_token,
    "emp_123456789",
    resurrectRequest
);

if (result.Code == 0)
{
    Console.WriteLine("员工恢复成功");
}
```

### 说明
- 可以选择保留原有的部门归属或重新分配部门
- 恢复后的员工状态为在职

---

## 办理离职（待离职状态）

### 接口名称
用于为在职员工办理离职，将其更新为「待离职」状态

### 飞飞书接口URL
```
https://open.feishu.cn/open-apis/directory/v1/employees/{employee_id}/to_be_resigned
```

### 方法
PATCH

### 认证
需要访问凭证（tenant_access_token 或 user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| employee_id | string | Path | 是 | 员工ID | "emp_123456789" |
| employee_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |
| resignEmployeeRequest | ResignEmployeeRequest | Body | 是 | 在职员工流转到待离职请求体 | 见下方示例 |

#### ResignEmployeeRequest 结构

```json
{
  "resign_reason": "个人原因",
  "handover_to": "emp_987654321",
  "disable_account_immediately": false
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success"
}
```

#### 错误响应
```json
{
  "code": 403,
  "msg": "Only HR admin can perform this operation"
}
```

### 代码示例

```csharp
var resignRequest = new ResignEmployeeRequest
{
    ResignReason = "个人原因",
    HandoverTo = "emp_987654321",
    DisableAccountImmediately = false
};

var result = await _employeesApi.ResignedEmployeeAsync(
    access_token,
    "emp_123456789",
    resignRequest
);

if (result.Code == 0)
{
    Console.WriteLine("员工已设置为待离职状态");
}
```

### 说明
- 使用 user_access_token 时默认为管理员用户，仅「人事管理模式」的管理员可操作
- 「待离职」员工不会自动离职，需要使用「离职员工」API 操作离职和资源转交

---

## 取消离职

### 接口名称
用于为待离职员工取消离职，将其更新为「在职」状态

### 飞飞书接口URL
```
https://open.feishu.cn/open-apis/directory/v1/employees/{employee_id}/regular
```

### 方法
PATCH

### 认证
需要访问凭证（tenant_access_token 或 user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| employee_id | string | Path | 是 | 员工ID | "emp_123456789" |
| employee_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success"
}
```

#### 错误响应
```json
{
  "code": 400,
  "msg": "Employee is not in to_be_resigned status"
}
```

### 代码示例

```csharp
var result = await _employeesApi.RegularEmployeeAsync(
    access_token,
    "emp_123456789"
);

if (result.Code == 0)
{
    Console.WriteLine("员工取消离职成功");
}
```

### 说明
- 取消离职时会清空离职信息
- 仅对「待离职」状态的员工有效

---

## 批量查询员工

### 接口名称
用于批量根据员工的ID查询员工的详情，比如员工姓名，手机号，邮箱，部门等信息。

### 飞飞书接口URL
```
https://open.feishu.cn/open-apis/directory/v1/employees/mget
```

### 方法
POST

### 认证
需要访问凭证（tenant_access_token 或 user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| employee_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |
| employeeQueryRequest | EmployeeQueryRequest | Body | 是 | 员工查询请求体 | 见下方示例 |

#### EmployeeQueryRequest 结构

```json
{
  "employee_ids": ["emp_123456789", "emp_987654321"],
  "required_fields": ["name", "email", "mobile", "department_ids"]
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "employees": [
      {
        "employee_id": "emp_123456789",
        "name": {
          "zh_cn": "张三",
          "en_us": "Zhang San"
        },
        "email": "zhangsan@example.com",
        "mobile": "13800138000",
        "department_ids": ["od-12345678"]
      }
    ],
    "abnormals": []
  }
}
```

#### 错误响应
```json
{
  "code": 400,
  "msg": "Invalid employee_ids"
}
```

### 代码示例

```csharp
var queryRequest = new EmployeeQueryRequest
{
    EmployeeIds = new List<string> { "emp_123456789", "emp_987654321" },
    RequiredFields = new List<string> 
    { 
        "name", 
        "email", 
        "mobile", 
        "department_ids" 
    }
};

var result = await _employeesApi.QueryEmployeesAsync(
    access_token,
    queryRequest
);

if (result.Code == 0)
{
    foreach (var employee in result.Data?.Employees ?? [])
    {
        Console.WriteLine($"员工：{employee.Name}");
        Console.WriteLine($"邮箱：{employee.Email}");
    }
}
```

### 说明
- 最多支持查询50个员工
- `abnormals` 字段包含查询失败的员工信息

---

## 分页查询员工

### 接口名称
用于依据指定条件，分页批量获取符合条件的员工详情列表

### 飞飞书接口URL
```
https://open.feishu.cn/open-apis/directory/v1/employees/filter
```

### 方法
POST

### 认证
需要访问凭证（tenant_access_token 或 user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| employee_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |
| employeeQueryRequest | EmployeeSearchRequest | Body | 是 | 员工查询请求体 | 见下方示例 |

#### EmployeeSearchRequest 结构

```json
{
  "rules": [
    {
      "field": "employment_type",
      "operator": "is",
      "value": "formal"
    },
    {
      "field": "department_ids",
      "operator": "contains",
      "value": ["od-12345678"]
    }
  ],
  "page_request": {
    "page_size": 20,
    "page_token": ""
  }
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "employees": [
      {
        "employee_id": "emp_123456789",
        "name": {
          "zh_cn": "张三",
          "en_us": "Zhang San"
        },
        "employment_type": "formal",
        "department_ids": ["od-12345678"]
      }
    ],
    "page": {
      "page_size": 20,
      "page_token": "",
      "has_more": false,
      "total": "1"
    }
  }
}
```

#### 错误响应
```json
{
  "code": 400,
  "msg": "Invalid filter rules"
}
```

### 代码示例

```csharp
var searchRequest = new EmployeeSearchRequest
{
    Rules = new List<FilterRule>
    {
        new FilterRule
        {
            Field = "employment_type",
            Operator = "is",
            Value = "formal"
        }
    },
    PageRequest = new PageRequest
    {
        PageSize = 20,
        PageToken = ""
    }
};

var result = await _employeesApi.QueryEmployeePageListAsync(
    access_token,
    searchRequest
);

if (result.Code == 0)
{
    Console.WriteLine($"找到 {result.Data?.Employees?.Count} 个员工");
    Console.WriteLine($"总计 {result.Data?.Page?.Total} 个员工");
}
```

### 说明
- 支持复杂的过滤条件组合
- 支持分页查询，使用 `page_token` 获取下一页
- 支持多种操作符：is, contains, in, not_in 等

---

## 搜索员工

### 接口名称
用于搜索员工信息，如通过关键词搜索员工的名称、手机号、邮箱等信息

### 飞飞书接口URL
```
https://open.feishu.cn/open-apis/directory/v1/employees/search
```

### 方法
POST

### 认证
需要访问凭证（tenant_access_token 或 user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| employee_id_type | string | Query | 否 | 用户 ID 类型 | "open_id" |
| department_id_type | string | Query | 否 | 部门 ID 类型 | "department_id" |
| employeeQueryRequest | EmployeePageQueryRequest | Body | 是 | 员工查询请求体 | 见下方示例 |

#### EmployeePageQueryRequest 结构

```json
{
  "query": "张三",
  "page_size": 20,
  "page_token": ""
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "employees": [
      {
        "employee_id": "emp_123456789",
        "name": {
          "zh_cn": "张三",
          "en_us": "Zhang San"
        },
        "email": "zhangsan@example.com",
        "mobile": "13800138000"
      }
    ],
    "page": {
      "page_size": 20,
      "page_token": "",
      "has_more": false,
      "total": "1"
    }
  }
}
```

#### 错误响应
```json
{
  "code": 400,
  "msg": "Invalid query parameter"
}
```

### 代码示例

```csharp
var pageQueryRequest = new EmployeePageQueryRequest
{
    Query = "张三",
    PageSize = 20,
    PageToken = ""
};

var result = await _employeesApi.SearchEmployeePageListAsync(
    access_token,
    pageQueryRequest
);

if (result.Code == 0)
{
    foreach (var employee in result.Data?.Employees ?? [])
    {
        Console.WriteLine($"找到员工：{employee.Name}");
    }
}
```

### 说明
- 支持模糊搜索，会搜索员工姓名、手机号、邮箱等字段
- 支持分页查询
- 搜索结果按相关性排序

---

## 错误码说明

| 错误码 | 说明 | 解决方案 |
|--------|------|----------|
| 0 | 成功 | - |
| 99991401 | access_token 无效或已过期 | 重新获取 access_token |
| 99991400 | 参数错误 | 检查请求参数格式和必填项 |
| 403 | 无权限 | 检查应用权限和管理员范围 |
| 404 | 员工不存在 | 确认 employee_id 是否正确 |
| 400 | 请求参数无效 | 检查参数值是否符合规范 |
| 429 | 请求频率超限 | 降低请求频率，使用限流策略 |

---

## 最佳实践

### 1. 错误处理
```csharp
public async Task<bool> HandleApiResult<T>(FeishuApiResult<T> result)
{
    switch (result.Code)
    {
        case 0:
            return true;
        case 99991401:
            // Token 过期，重新获取
            await RefreshToken();
            return false;
        case 429:
            // 限流，延迟重试
            await Task.Delay(1000);
            return false;
        default:
            // 其他错误，记录日志
            _logger.LogError($"API调用失败: {result.Code} - {result.Msg}");
            return false;
    }
}
```

### 2. 批量操作优化
```csharp
// 批量查询时使用分批处理
public async Task<List<EmployeeDetail>> GetAllEmployeesAsync(List<string> employeeIds)
{
    var results = new List<EmployeeDetail>();
    const int batchSize = 50;
    
    for (int i = 0; i < employeeIds.Count; i += batchSize)
    {
        var batch = employeeIds.Skip(i).Take(batchSize).ToList();
        var query = new EmployeeQueryRequest
        {
            EmployeeIds = batch,
            RequiredFields = new List<string> { "name", "email", "mobile" }
        };
        
        var result = await _employeesApi.QueryEmployeesAsync(_token, query);
        if (result.Code == 0 && result.Data?.Employees != null)
        {
            results.AddRange(result.Data.Employees);
        }
        
        // 避免限流
        await Task.Delay(100);
    }
    
    return results;
}
```

### 3. 缓存策略
```csharp
// 员工信息缓存
public async Task<EmployeeDetail> GetEmployeeWithCacheAsync(string employeeId)
{
    var cacheKey = $"employee:{employeeId}";
    
    if (_cache.TryGetValue(cacheKey, out EmployeeDetail cachedEmployee))
    {
        return cachedEmployee;
    }
    
    var query = new EmployeeQueryRequest
    {
        EmployeeIds = new List<string> { employeeId }
    };
    
    var result = await _employeesApi.QueryEmployeesAsync(_token, query);
    if (result.Code == 0 && result.Data?.Employees?.Count > 0)
    {
        var employee = result.Data.Employees[0];
        _cache.Set(cacheKey, employee, TimeSpan.FromMinutes(30));
        return employee;
    }
    
    throw new Exception($"获取员工信息失败: {employeeId}");
}
```

---

## 版本更新记录

| 版本 | 日期 | 更新内容 |
|------|------|----------|
| v1.0 | 2025-11-01 | 初始版本，支持员工基本CRUD操作 |

---

## 支持与反馈

如果您在使用过程中遇到问题，请通过以下方式获取帮助：

1. 查看 [飞书开放平台文档](https://open.feishu.cn/document/directory-v1/employee/overview)
2. 提交问题到项目的 Issues
3. 联系技术支持团队

---

*最后更新时间: 2025-11-20*