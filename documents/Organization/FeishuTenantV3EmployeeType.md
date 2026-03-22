# 租户V3人员类型管理 - FeishuTenantV3EmployeeType

## 接口名称
**租户V3人员类型管理 - (IFeishuTenantV3EmployeeType)**

## 功能描述
本接口提供飞书人员类型的管理功能，适用于租户应用场景。支持人员类型的创建、更新、查询和删除等操作。

飞书人员类型是通讯录中一种特殊的用户属性字段，用于标记用户的身份类型。使用通讯录API，可以对人员类型资源进行增删改查操作。系统默认包含正式、实习、外包、劳务、顾问五个选项。

## 参考文档
- [飞书官方文档 - 人员类型概述](https://open.feishu.cn/document/server-docs/contact-v3/employee_type_enum/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateEmployeeTypeAsync | 创建人员类型 | 租户令牌 | POST |
| UpdateEmployeeTypeAsync | 更新人员类型 | 租户令牌 | PUT |
| GetEmployeeTypesAsync | 查询人员类型列表 | 租户令牌 | GET |
| DeleteEmployeeTypeByIdAsync | 删除人员类型 | 租户令牌 | DELETE |

## 函数详细内容

### 创建人员类型

**函数名称**：创建人员类型

**函数签名**：
```csharp
Task<FeishuApiResult<EmployeeTypeEnumResult>?> CreateEmployeeTypeAsync(
    [Body] EmployeeTypeEnumRequest groupInfoRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| groupInfoRequest | EmployeeTypeEnumRequest | ✅ | 新增人员类型请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "employee_type_enum": {
      "enum_id": "xxx",
      "enum_type": 2,
      "enum_value": "contractor",
      "content": "合同工",
      "is_default": false
    }
  }
}
```

**说明**：新增一个自定义的人员类型。人员类型是用户属性之一，用于灵活标记用户的身份类型。

**代码示例**：
```csharp
public class EmployeeTypeService
{
    private readonly IFeishuTenantV3EmployeeType _employeeTypeClient;

    public EmployeeTypeService(IFeishuTenantV3EmployeeType employeeTypeClient)
    {
        _employeeTypeClient = employeeTypeClient;
    }

    public async Task CreateCustomEmployeeTypeAsync()
    {
        var request = new EmployeeTypeEnumRequest
        {
            EnumValue = "contractor",
            Content = "合同工",
            EnumType = 2
        };

        var result = await _employeeTypeClient.CreateEmployeeTypeAsync(request);
        if (result?.Code == 0)
        {
            Console.WriteLine($"人员类型创建成功，ID: {result.Data?.EmployeeTypeEnum?.EnumId}");
        }
    }
}
```

---

### 更新人员类型

**函数名称**：更新人员类型

**函数签名**：
```csharp
Task<FeishuApiResult<EmployeeTypeEnumResult>?> UpdateEmployeeTypeAsync(
    [Path] string enum_id,
    [Body] EmployeeTypeEnumRequest groupInfoRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| enum_id | string | ✅ | 自定义人员类型的选项ID |
| groupInfoRequest | EmployeeTypeEnumRequest | ✅ | 更新人员类型请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "employee_type_enum": {
      "enum_id": "xxx",
      "content": "新名称"
    }
  }
}
```

**说明**：更新指定的自定义人员类型信息。`enum_id` 可以在新建人员类型时从返回值中获取，也可以调用查询人员类型接口获取。

---

### 查询人员类型列表

**函数名称**：查询人员类型列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<EmployeeTypeEnum>?> GetEmployeeTypesAsync(
    [Query("page_size")] int? page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "enum_id": "xxx",
        "enum_type": 1,
        "enum_value": "formal",
        "content": "正式",
        "is_default": true
      },
      {
        "enum_id": "yyy",
        "enum_type": 2,
        "enum_value": "contractor",
        "content": "合同工",
        "is_default": false
      }
    ],
    "page_token": "xxx",
    "has_more": false
  }
}
```

**说明**：查询当前租户下所有的人员类型信息，包括选项ID、类型、编号以及内容等。

**代码示例**：
```csharp
public async Task ListAllEmployeeTypesAsync()
{
    var result = await _employeeTypeClient.GetEmployeeTypesAsync(page_size: 50);
    if (result?.Code == 0)
    {
        foreach (var type in result.Data?.Items ?? [])
        {
            Console.WriteLine($"类型: {type.Content}, 值: {type.EnumValue}, 默认: {type.IsDefault}");
        }
    }
}
```

---

### 删除人员类型

**函数名称**：删除人员类型

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteEmployeeTypeByIdAsync(
    [Path] string enum_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| enum_id | string | ✅ | 自定义人员类型的选项ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：
- 仅支持删除自定义的人员类型
- 默认包含的正式、实习、外包、劳务、顾问五个选项不支持删除
