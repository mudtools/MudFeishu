# 人员类型管理

## 功能描述
飞书人员类型是通讯录中一种特殊的用户属性字段，用于标记用户的身份类型。系统默认包含正式、实习、外包、劳务、顾问五个预定义类型，同时支持创建自定义人员类型。该API提供完整的人员类型管理功能，包括创建、更新、查询和删除自定义人员类型，满足企业多样化的人员分类管理需求。

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP方法 |
|---------|---------|---------|---------|
| CreateEmployeeTypeAsync | 新增一个自定义的人员类型 | user_access_token | POST |
| UpdateEmployeeTypeAsync | 更新指定的自定义人员类型信息 | user_access_token | PUT |
| GetEmployeeTypesAsync | 查询当前租户下所有的人员类型信息 | user_access_token | GET |
| DeleteEmployeeTypeByIdAsync | 删除指定的自定义人员类型 | user_access_token | DELETE |

## CreateEmployeeTypeAsync
**认证**：user_access_token  
**参数**：
- user_access_token (必填) - 用户访问令牌
- groupInfoRequest (必填) - 新增人员类型请求体

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "employee_type_enum": {
      "enum_id": "67890",
      "enum_value": "6",
      "content": "专家顾问",
      "enum_type": 2,
      "enum_status": 1,
      "i18n_content": [
        {
          "locale": "zh_cn",
          "value": "专家顾问（中文）"
        },
        {
          "locale": "en_us", 
          "value": "Expert Advisor"
        }
      ]
    }
  }
}

错误响应：
{
  "code": 400,
  "msg": "人员类型内容已存在"
}
```

**说明**：
- enum_type 固定取值为 2（自定义类型）
- enum_status 为 1 表示激活状态，2 表示未激活状态
- 支持配置国际化内容，满足多语言需求
- 创建成功后会自动分配 enum_id 和 enum_value

**代码示例**：
```csharp
// 创建新的人员类型
var createRequest = new EmployeeTypeEnumRequest
{
    Content = "专家顾问",
    EnumType = 2, // 自定义类型
    EnumStatus = 1, // 激活状态
    I18nContent = new List<I18nContent>
    {
        new I18nContent
        {
            Locale = "zh_cn",
            Value = "专家顾问（中文）"
        },
        new I18nContent
        {
            Locale = "en_us",
            Value = "Expert Advisor"
        }
    }
};

var result = await _feishuApi.CreateEmployeeTypeAsync(
    userAccessToken,
    createRequest);

if (result.Success)
{
    var employeeType = result.Data.EmployeeTypeEnum;
    Console.WriteLine($"人员类型创建成功：{employeeType.Content} (ID: {employeeType.EnumId})");
    
    if (employeeType.I18nContent != null)
    {
        foreach (var i18n in employeeType.I18nContent)
        {
            Console.WriteLine($"  {i18n.Locale}: {i18n.Value}");
        }
    }
}
else
{
    Console.WriteLine($"创建失败：{result.Message}");
}
```

---

## UpdateEmployeeTypeAsync
**认证**：user_access_token  
**参数**：
- user_access_token (必填) - 用户访问令牌
- enum_id (必填) - 自定义人员类型的选项ID
- groupInfoRequest (必填) - 更新人员类型请求体

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "employee_type_enum": {
      "enum_id": "67890",
      "enum_value": "6",
      "content": "高级专家顾问",
      "enum_type": 2,
      "enum_status": 1,
      "i18n_content": [
        {
          "locale": "zh_cn",
          "value": "高级专家顾问"
        }
      ]
    }
  }
}

错误响应：
{
  "code": 404,
  "msg": "人员类型不存在"
}
```

**说明**：
- 只能更新自定义类型（enum_type = 2）
- 不能更新系统内置的5个默认类型
- 可以修改内容、状态和国际化配置

**代码示例**：
```csharp
// 更新人员类型
var updateRequest = new EmployeeTypeEnumRequest
{
    Content = "高级专家顾问",
    EnumType = 2,
    EnumStatus = 1,
    I18nContent = new List<I18nContent>
    {
        new I18nContent
        {
            Locale = "zh_cn",
            Value = "高级专家顾问（更新版）"
        },
        new I18nContent
        {
            Locale = "en_us",
            Value = "Senior Expert Advisor"
        }
    }
};

var result = await _feishuApi.UpdateEmployeeTypeAsync(
    userAccessToken,
    "67890",
    updateRequest);

if (result.Success)
{
    var updatedType = result.Data.EmployeeTypeEnum;
    Console.WriteLine($"人员类型更新成功：{updatedType.Content}");
    
    // 检查是否需要停用某些类型
    if (updatedType.EnumStatus == 0)
    {
        Console.WriteLine("注意：该人员类型已被停用");
    }
}
else
{
    Console.WriteLine($"更新失败：{result.Message}");
}
```

---

## GetEmployeeTypesAsync
**认证**：user_access_token  
**参数**：
- user_access_token (必填) - 用户访问令牌
- page_size (可选) - 分页大小，默认10
- page_token (可选) - 分页标记

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "items": [
      {
        "enum_id": "1",
        "enum_value": "1",
        "content": "正式员工",
        "enum_type": 1,
        "enum_status": 1
      },
      {
        "enum_id": "2", 
        "enum_value": "2",
        "content": "实习生",
        "enum_type": 1,
        "enum_status": 1
      },
      {
        "enum_id": "67890",
        "enum_value": "6", 
        "content": "专家顾问",
        "enum_type": 2,
        "enum_status": 1,
        "i18n_content": [
          {
            "locale": "zh_cn",
            "value": "专家顾问"
          }
        ]
      }
    ],
    "page_token": "next_page_token"
  }
}
```

**说明**：
- 返回所有人员类型，包括系统内置和自定义的
- enum_type = 1 表示系统内置类型，enum_type = 2 表示自定义类型
- 支持分页获取大量数据

**代码示例**：
```csharp
// 获取所有人员类型
var pageSize = 50;
var pageToken = "";
var allEmployeeTypes = new List<EmployeeTypeEnum>();

do
{
    var result = await _feishuApi.GetEmployeeTypesAsync(
        userAccessToken,
        page_size: pageSize,
        page_token: string.IsNullOrEmpty(pageToken) ? null : pageToken);

    if (result.Success)
    {
        allEmployeeTypes.AddRange(result.Data.Items);
        
        // 分类显示人员类型
        Console.WriteLine("系统内置类型：");
        foreach (var type in result.Data.Items.Where(t => t.EnumType == 1))
        {
            Console.WriteLine($"  {type.Content} ({type.EnumStatus == 1 ? "启用" : "停用"})");
        }
        
        Console.WriteLine("\n自定义类型：");
        foreach (var type in result.Data.Items.Where(t => t.EnumType == 2))
        {
            Console.WriteLine($"  {type.Content} (ID: {type.EnumId}, {type.EnumStatus == 1 ? "启用" : "停用"})");
        }

        pageToken = result.Data.PageToken;
    }
    else
    {
        Console.WriteLine($"获取失败：{result.Message}");
        break;
    }
} while (!string.IsNullOrEmpty(pageToken));

Console.WriteLine($"\n总共获取到 {allEmployeeTypes.Count} 种人员类型");
```

---

## DeleteEmployeeTypeByIdAsync
**认证**：user_access_token  
**参数**：
- user_access_token (必填) - 用户访问令牌
- enum_id (必填) - 自定义人员类型的选项ID

**响应**：
```json
成功响应：
{
  "code": 0,
  "msg": "success"
}

错误响应：
{
  "code": 400,
  "msg": "不能删除系统内置人员类型"
}

错误响应：
{
  "code": 404,
  "msg": "人员类型不存在"
}
```

**说明**：
- 仅支持删除自定义人员类型（enum_type = 2）
- 系统内置的5个默认类型（正式、实习、外包、劳务、顾问）不支持删除
- 删除前请确认该类型没有被用户使用

**代码示例**：
```csharp
// 安全删除人员类型（先检查是否可删除）
var enumIdToDelete = "67890";

// 先获取人员类型详情
var getTypesResult = await _feishuApi.GetEmployeeTypesAsync(
    userAccessToken,
    page_size: 100);

if (getTypesResult.Success)
{
    var targetType = getTypesResult.Data.Items.FirstOrDefault(t => t.EnumId == enumIdToDelete);
    
    if (targetType == null)
    {
        Console.WriteLine("人员类型不存在");
        return;
    }
    
    if (targetType.EnumType == 1)
    {
        Console.WriteLine("不能删除系统内置人员类型");
        return;
    }
    
    Console.WriteLine($"准备删除人员类型：{targetType.Content}");
    
    // 确认删除
    var deleteResult = await _feishuApi.DeleteEmployeeTypeByIdAsync(
        userAccessToken,
        enumIdToDelete);
    
    if (deleteResult.Success)
    {
        Console.WriteLine("人员类型删除成功");
    }
    else
    {
        Console.WriteLine($"删除失败：{deleteResult.Message}");
    }
}
```

---

## 版本记录

| 版本 | 日期 | 说明 | 作者 |
|-----|-----|-----|-----|
| v1.0.0 | 2025-11-20 | 初始版本，包含所有人员类型管理API | Mud Studio |
