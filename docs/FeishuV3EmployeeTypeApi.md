# 飞书人员类型管理 API 文档

## 概述

飞书人员类型是通讯录中一种特殊的用户属性字段，用于标记用户的身份类型。使用通讯录 API，可以对人员类型资源进行增删改查操作。

**版本**: v3  
**基础URL**: `https://open.feishu.cn/open-apis/contact/v3/employee_type_enums`  
**完整文档**: [https://open.feishu.cn/document/server-docs/contact-v3/employee_type_enum/overview](https://open.feishu.cn/document/server-docs/contact-v3/employee_type_enum/overview)

---

## 创建人员类型

### 接口名称
新增一个自定义的人员类型。人员类型是用户属性之一，用于灵活标记用户的身份类型。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/employee_type_enums
```

### 方法
POST

### 认证
需要用户访问凭证（user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| user_access_token | string | Header | 是 | 用户访问凭证 | "u_xxxxxxxxx" |
| groupInfoRequest | EmployeeTypeEnumRequest | Body | 是 | 新增人员类型请求体 | 见下方示例 |

#### EmployeeTypeEnumRequest 结构

```json
{
  "content": "专家",
  "enum_type": 2,
  "enum_status": 1,
  "i18n_content": [
    {
      "locale": "zh_cn",
      "value": "专家"
    },
    {
      "locale": "en_us",
      "value": "Expert"
    },
    {
      "locale": "ja_jp",
      "value": "専門家"
    }
  ]
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "employee_type_enum": {
      "enum_id": "6989840515539468545",
      "enum_value": "6",
      "content": "专家",
      "enum_type": 2,
      "enum_status": 1,
      "i18n_content": [
        {
          "locale": "zh_cn",
          "value": "专家"
        },
        {
          "locale": "en_us",
          "value": "Expert"
        },
        {
          "locale": "ja_jp",
          "value": "専門家"
        }
      ]
    }
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
var createRequest = new EmployeeTypeEnumRequest
{
    Content = "专家",
    EnumType = 2, // 自定义类型
    EnumStatus = 1, // 激活状态
    I18nContent = new List<I18nContent>
    {
        new I18nContent { Locale = "zh_cn", Value = "专家" },
        new I18nContent { Locale = "en_us", Value = "Expert" },
        new I18nContent { Locale = "ja_jp", Value = "専門家" }
    }
};

var result = await _employeeTypeApi.CreateEmployeeTypeAsync(
    user_access_token,
    createRequest
);

if (result.Code == 0)
{
    var employeeType = result.Data?.EmployeeTypeEnum;
    Console.WriteLine($"人员类型创建成功，ID: {employeeType?.EnumId}");
    Console.WriteLine($"内容: {employeeType?.Content}");
}
```

### 说明
- `enum_type` 固定取值为 2（自定义类型），新增时不支持选择内置类型
- `enum_status`: 1-激活，2-未激活。只有已激活的选项可以用于配置用户属性
- 支持多语言配置，包含中文、英文、日文等
- 系统预定义了5种默认类型：正式、实习、外包、劳务、顾问

---

## 更新人员类型

### 接口名称
更新指定的自定义人员类型信息。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/employee_type_enums/{enum_id}
```

### 方法
PUT

### 认证
需要用户访问凭证（user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| user_access_token | string | Header | 是 | 用户访问凭证 | "u_xxxxxxxxx" |
| enum_id | string | Path | 是 | 自定义人员类型的选项ID | "6989840515539468545" |
| groupInfoRequest | EmployeeTypeEnumRequest | Body | 是 | 新增人员类型请求体 | 见下方示例 |

#### EmployeeTypeEnumRequest 结构

```json
{
  "content": "高级专家",
  "enum_type": 2,
  "enum_status": 1,
  "i18n_content": [
    {
      "locale": "zh_cn",
      "value": "高级专家"
    },
    {
      "locale": "en_us",
      "value": "Senior Expert"
    }
  ]
}
```

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "employee_type_enum": {
      "enum_id": "6989840515539468545",
      "enum_value": "6",
      "content": "高级专家",
      "enum_type": 2,
      "enum_status": 1,
      "i18n_content": [
        {
          "locale": "zh_cn",
          "value": "高级专家"
        },
        {
          "locale": "en_us",
          "value": "Senior Expert"
        }
      ]
    }
  }
}
```

#### 错误响应
```json
{
  "code": 404,
  "msg": "employee_type_enum not found"
}
```

### 代码示例

```csharp
var updateRequest = new EmployeeTypeEnumRequest
{
    Content = "高级专家",
    EnumType = 2,
    EnumStatus = 1,
    I18nContent = new List<I18nContent>
    {
        new I18nContent { Locale = "zh_cn", Value = "高级专家" },
        new I18nContent { Locale = "en_us", Value = "Senior Expert" }
    }
};

var result = await _employeeTypeApi.UpdateEmployeeTypeAsync(
    user_access_token,
    "6989840515539468545",
    updateRequest
);

if (result.Code == 0)
{
    Console.WriteLine("人员类型更新成功");
}
```

### 说明
- 只能更新自定义的人员类型，不能更新系统预定义的类型
- `enum_id` 可以在新建人员类型时从返回值中获取，也可以调用查询接口获取
- PUT 请求会完整替换人员类型信息，未传递的字段可能会被重置

---

## 查询人员类型列表

### 接口名称
查询当前租户下所有的人员类型信息，包括选项 ID、类型、编号以及内容等。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/employee_type_enums
```

### 方法
GET

### 认证
需要用户访问凭证（user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| user_access_token | string | Header | 是 | 用户访问凭证 | "u_xxxxxxxxx" |
| page_size | int | Query | 否 | 分页大小 | 10 |
| page_token | string | Query | 否 | 分页标记 | "" |

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "enum_id": "6989840515539468545",
        "enum_value": "6",
        "content": "专家",
        "enum_type": 2,
        "enum_status": 1,
        "i18n_content": [
          {
            "locale": "zh_cn",
            "value": "专家"
          },
          {
            "locale": "en_us",
            "value": "Expert"
          }
        ]
      },
      {
        "enum_id": "6989840515539468540",
        "enum_value": "1",
        "content": "正式",
        "enum_type": 0,
        "enum_status": 1,
        "i18n_content": [
          {
            "locale": "zh_cn",
            "value": "正式"
          }
        ]
      }
    ],
    "page_token": "",
    "has_more": false
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
var result = await _employeeTypeApi.GetEmployeeTypesAsync(
    user_access_token,
    page_size: 20
);

if (result.Code == 0)
{
    Console.WriteLine($"找到 {result.Data?.Items?.Count} 种人员类型");
    
    foreach (var employeeType in result.Data?.Items ?? [])
    {
        Console.WriteLine($"类型：{employeeType.Content}，ID：{employeeType.EnumId}");
        Console.WriteLine($"类型：{(employeeType.EnumType == 0 ? "系统预定义" : "自定义")}");
        Console.WriteLine($"状态：{(employeeType.EnumStatus == 1 ? "启用" : "停用")}");
        Console.WriteLine("---");
    }
}
```

### 说明
- 返回列表包含系统预定义的5种类型和所有自定义类型
- `enum_type`: 0-系统预定义枚举，1-用户自定义枚举
- `enum_status`: 0-已停用，1-启用
- 支持分页查询，使用 `page_token` 获取下一页数据
- 默认包含的5种系统类型：正式、实习、外包、劳务、顾问

---

## 删除人员类型

### 接口名称
删除指定的自定义人员类型。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/employee_type_enums/{enum_id}
```

### 方法
DELETE

### 认证
需要用户访问凭证（user_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| user_access_token | string | Header | 是 | 用户访问凭证 | "u_xxxxxxxxx" |
| enum_id | string | Path | 是 | 自定义人员类型的选项ID | "6989840515539468545" |

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
  "msg": "employee_type_enum not found"
}
```

### 代码示例

```csharp
var result = await _employeeTypeApi.DeleteEmployeeTypeByIdAsync(
    user_access_token,
    "6989840515539468545"
);

if (result.Code == 0)
{
    Console.WriteLine("人员类型删除成功");
}
```

### 说明
- 仅支持删除自定义的人员类型
- 默认包含的正式、实习、外包、劳务、顾问五个系统预定义选项不支持删除
- 删除前请确认没有用户在使用该人员类型
- 删除操作不可恢复，请谨慎使用

---

## 数据模型说明

### EmployeeTypeEnum 人员类型模型

| 字段名 | 类型 | 说明 |
|--------|------|------|
| enum_id | string | 枚举ID，唯一标识一个人员类型 |
| enum_value | string | 枚举值，用于系统内部标识 |
| content | string | 枚举内容，显示给用户看的名称 |
| enum_type | int | 枚举类型：0-系统预定义，1-用户自定义 |
| enum_status | int | 枚举状态：0-已停用，1-启用 |
| i18n_content | List<I18nContent> | 国际化内容配置 |

### I18nContent 国际化内容模型

| 字段名 | 类型 | 说明 | 示例值 |
|--------|------|------|--------|
| locale | string | 语言版本 | "zh_cn", "en_us", "ja_jp" |
| value | string | 语言版本对应的内容 | "专家", "Expert", "専門家" |

### EmployeeTypeEnumRequest 请求模型

| 字段名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| content | string | 是 | 人员类型的选项内容 |
| enum_type | int | 是 | 人员类型选项类型：2-自定义 |
| enum_status | int | 否 | 选项激活状态：1-激活，2-未激活 |
| i18n_content | List<I18nContent> | 否 | 选项内容的国际化配置 |

---

## 错误码说明

| 错误码 | 说明 | 解决方案 |
|--------|------|----------|
| 0 | 成功 | - |
| 99991401 | access_token 无效或已过期 | 重新获取 access_token |
| 99991400 | 参数错误 | 检查请求参数格式和必填项 |
| 403 | 无权限 | 检查用户权限和应用权限 |
| 404 | 人员类型不存在 | 确认 enum_id 是否正确 |
| 400 | 请求参数无效 | 检查参数值是否符合规范 |
| 429 | 请求频率超限 | 降低请求频率，使用限流策略 |
| 500 | 服务器内部错误 | 稍后重试或联系技术支持 |

---

## 最佳实践

### 1. 人员类型管理策略
```csharp
public class EmployeeTypeManager
{
    private readonly IFeishuV3EmployeeTypeApi _employeeTypeApi;
    private readonly IMemoryCache _cache;
    
    public async Task<string> CreateCustomEmployeeTypeAsync(string content, Dictionary<string, string>? i18nContent = null)
    {
        // 检查是否已存在相同名称的人员类型
        var existingTypes = await GetAllEmployeeTypesAsync();
        if (existingTypes.Any(t => t.Content == content))
        {
            throw new InvalidOperationException($"人员类型 '{content}' 已存在");
        }
        
        var request = new EmployeeTypeEnumRequest
        {
            Content = content,
            EnumType = 2, // 自定义类型
            EnumStatus = 1, // 激活状态
            I18nContent = i18nContent?.Select(kv => new I18nContent 
            { 
                Locale = kv.Key, 
                Value = kv.Value 
            }).ToList()
        };
        
        var result = await _employeeTypeApi.CreateEmployeeTypeAsync(_token, request);
        if (result.Code == 0)
        {
            // 清除缓存
            _cache.Remove("employee_types");
            return result.Data?.EmployeeTypeEnum?.EnumId ?? string.Empty;
        }
        
        throw new Exception($"创建人员类型失败: {result.Msg}");
    }
    
    public async Task<List<EmployeeTypeEnum>> GetAllEmployeeTypesAsync()
    {
        const string cacheKey = "employee_types";
        
        if (_cache.TryGetValue(cacheKey, out List<EmployeeTypeEnum> cachedTypes))
        {
            return cachedTypes;
        }
        
        var allTypes = new List<EmployeeTypeEnum>();
        string? pageToken = null;
        
        do
        {
            var result = await _employeeTypeApi.GetEmployeeTypesAsync(
                _token, 
                page_size: 50, 
                page_token: pageToken
            );
            
            if (result.Code == 0 && result.Data?.Items != null)
            {
                allTypes.AddRange(result.Data.Items);
                pageToken = result.Data.HasMore ? result.Data.PageToken : null;
            }
            else
            {
                break;
            }
            
            // 避免请求过于频繁
            await Task.Delay(50);
            
        } while (!string.IsNullOrEmpty(pageToken));
        
        // 缓存30分钟
        _cache.Set(cacheKey, allTypes, TimeSpan.FromMinutes(30));
        return allTypes;
    }
}
```

### 2. 国际化配置管理
```csharp
public class I18nEmployeeTypeManager
{
    private readonly Dictionary<string, string> _supportedLocales = new()
    {
        { "zh_cn", "中文" },
        { "en_us", "英文" },
        { "ja_jp", "日文" }
    };
    
    public List<I18nContent> BuildI18nContent(string baseContent, Dictionary<string, string>? translations = null)
    {
        var i18nContent = new List<I18nContent>
        {
            new() { Locale = "zh_cn", Value = baseContent }
        };
        
        if (translations != null)
        {
            foreach (var translation in translations)
            {
                if (_supportedLocales.ContainsKey(translation.Key))
                {
                    i18nContent.Add(new I18nContent 
                    { 
                        Locale = translation.Key, 
                        Value = translation.Value 
                    });
                }
            }
        }
        
        return i18nContent;
    }
    
    public string GetLocalizedContent(EmployeeTypeEnum employeeType, string locale = "zh_cn")
    {
        // 优先使用指定语言的内容
        var localized = employeeType.I18nContent?.FirstOrDefault(c => c.Locale == locale);
        if (localized?.Value != null)
        {
            return localized.Value;
        }
        
        // 回退到中文
        var chinese = employeeType.I18nContent?.FirstOrDefault(c => c.Locale == "zh_cn");
        if (chinese?.Value != null)
        {
            return chinese.Value;
        }
        
        // 最后回退到原始内容
        return employeeType.Content ?? string.Empty;
    }
}
```

### 3. 批量操作和错误处理
```csharp
public class BatchEmployeeTypeOperation
{
    public async Task<BatchOperationResult> BatchCreateEmployeeTypesAsync(
        List<EmployeeTypeDefinition> definitions)
    {
        var results = new List<OperationResult>();
        var createdIds = new List<string>();
        
        foreach (var definition in definitions)
        {
            try
            {
                var request = new EmployeeTypeEnumRequest
                {
                    Content = definition.Name,
                    EnumType = 2,
                    EnumStatus = definition.IsActive ? 1 : 2,
                    I18nContent = definition.I18nTranslations
                        .Select(kv => new I18nContent { Locale = kv.Key, Value = kv.Value })
                        .ToList()
                };
                
                var result = await _employeeTypeApi.CreateEmployeeTypeAsync(_token, request);
                
                if (result.Code == 0)
                {
                    var enumId = result.Data?.EmployeeTypeEnum?.EnumId ?? string.Empty;
                    results.Add(OperationResult.Success(definition.Name, enumId));
                    createdIds.Add(enumId);
                }
                else
                {
                    results.Add(OperationResult.Failure(definition.Name, result.Msg ?? "Unknown error"));
                }
            }
            catch (Exception ex)
            {
                results.Add(OperationResult.Failure(definition.Name, ex.Message));
            }
            
            // 避免请求频率限制
            await Task.Delay(100);
        }
        
        return new BatchOperationResult
        {
            Success = createdIds.Count,
            Failure = definitions.Count - createdIds.Count,
            Results = results
        };
    }
    
    public async Task<bool> SafeDeleteEmployeeTypeAsync(string enumId)
    {
        try
        {
            // 先检查是否为系统预定义类型
            var allTypes = await _employeeTypeApi.GetEmployeeTypesAsync(_token);
            var targetType = allTypes.Data?.Items?.FirstOrDefault(t => t.EnumId == enumId);
            
            if (targetType?.EnumType == 0)
            {
                Console.WriteLine("不能删除系统预定义的人员类型");
                return false;
            }
            
            var result = await _employeeTypeApi.DeleteEmployeeTypeByIdAsync(_token, enumId);
            return result.Code == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删除人员类型失败: {ex.Message}");
            return false;
        }
    }
}
```

### 4. 缓存和性能优化
```csharp
public class EmployeeTypeCacheManager
{
    private readonly IMemoryCache _cache;
    private readonly IFeishuV3EmployeeTypeApi _employeeTypeApi;
    private readonly ILogger<EmployeeTypeCacheManager> _logger;
    
    public async Task<Dictionary<string, EmployeeTypeEnum>> GetEmployeeTypeMapAsync()
    {
        const string cacheKey = "employee_type_map";
        
        if (_cache.TryGetValue(cacheKey, out Dictionary<string, EmployeeTypeEnum> cachedMap))
        {
            return cachedMap;
        }
        
        var result = await _employeeTypeApi.GetEmployeeTypesAsync(_token, page_size: 50);
        
        if (result.Code == 0 && result.Data?.Items != null)
        {
            var typeMap = result.Data.Items.ToDictionary(t => t.EnumId ?? string.Empty, t => t);
            
            // 缓存30分钟
            _cache.Set(cacheKey, typeMap, TimeSpan.FromMinutes(30));
            
            _logger.LogInformation($"缓存了 {typeMap.Count} 种人员类型");
            return typeMap;
        }
        
        return new Dictionary<string, EmployeeTypeEnum>();
    }
    
    public async Task<EmployeeTypeEnum?> GetEmployeeTypeByIdAsync(string enumId)
    {
        var typeMap = await GetEmployeeTypeMapAsync();
        return typeMap.GetValueOrDefault(enumId);
    }
    
    public void InvalidateCache()
    {
        _cache.Remove("employee_type_map");
        _cache.Remove("employee_types");
        _logger.LogInformation("人员类型缓存已清除");
    }
    
    public async Task PreWarmCacheAsync()
    {
        try
        {
            await GetEmployeeTypeMapAsync();
            _logger.LogInformation("人员类型缓存预热完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "人员类型缓存预热失败");
        }
    }
}
```

---

## 使用场景示例

### 1. 企业人员类型管理
```csharp
// 为特定企业创建自定义人员类型
var enterpriseTypes = new[]
{
    new { Name = "技术专家", Translations = new Dictionary<string, string> { ["en_us"] = "Technical Expert" } },
    new { Name = "产品经理", Translations = new Dictionary<string, string> { ["en_us"] = "Product Manager" } },
    new { Name = "设计师", Translations = new Dictionary<string, string> { ["en_us"] = "Designer" } },
    new { Name = "项目经理", Translations = new Dictionary<string, string> { ["en_us"] = "Project Manager" } }
};

foreach (var typeInfo in enterpriseTypes)
{
    var request = new EmployeeTypeEnumRequest
    {
        Content = typeInfo.Name,
        EnumType = 2,
        EnumStatus = 1,
        I18nContent = new List<I18nContent>
        {
            new() { Locale = "zh_cn", Value = typeInfo.Name },
            new() { Locale = "en_us", Value = typeInfo.Translations["en_us"] }
        }
    };
    
    var result = await _employeeTypeApi.CreateEmployeeTypeAsync(_token, request);
    
    if (result.Code == 0)
    {
        Console.WriteLine($"创建人员类型 '{typeInfo.Name}' 成功，ID: {result.Data?.EmployeeTypeEnum?.EnumId}");
    }
    else
    {
        Console.WriteLine($"创建人员类型 '{typeInfo.Name}' 失败: {result.Msg}");
    }
}
```

### 2. 多语言国际化配置
```csharp
// 创建支持多语言的人员类型
var multilingualRequest = new EmployeeTypeEnumRequest
{
    Content = "顾问",
    EnumType = 2,
    EnumStatus = 1,
    I18nContent = new List<I18nContent>
    {
        new() { Locale = "zh_cn", Value = "顾问" },
        new() { Locale = "en_us", Value = "Consultant" },
        new() { Locale = "ja_jp", Value = "コンサルタント" },
        new() { Locale = "ko_kr", Value = "컨설턴트" } // 韩语
    }
};

var result = await _employeeTypeApi.CreateEmployeeTypeAsync(_token, multilingualRequest);
```

### 3. 人员类型状态管理
```csharp
// 批量激活/停用人员类型
public async Task<bool> UpdateEmployeeTypeStatusAsync(string enumId, bool activate)
{
    // 先获取当前人员类型信息
    var allTypes = await _employeeTypeApi.GetEmployeeTypesAsync(_token);
    var targetType = allTypes.Data?.Items?.FirstOrDefault(t => t.EnumId == enumId);
    
    if (targetType == null)
    {
        Console.WriteLine("人员类型不存在");
        return false;
    }
    
    if (targetType.EnumType == 0)
    {
        Console.WriteLine("不能修改系统预定义人员类型的状态");
        return false;
    }
    
    var updateRequest = new EmployeeTypeEnumRequest
    {
        Content = targetType.Content,
        EnumType = targetType.EnumType,
        EnumStatus = activate ? 1 : 2, // 1-激活，2-未激活
        I18nContent = targetType.I18nContent
    };
    
    var result = await _employeeTypeApi.UpdateEmployeeTypeAsync(_token, enumId, updateRequest);
    return result.Code == 0;
}
```

---

## 版本更新记录

| 版本 | 日期 | 更新内容 |
|------|------|----------|
| v1.0 | 2025-11-01 | 初始版本，支持人员类型的基本CRUD操作 |

---

## 支持与反馈

如果您在使用过程中遇到问题，请通过以下方式获取帮助：

1. 查看 [飞书开放平台文档](https://open.feishu.cn/document/server-docs/contact-v3/employee_type_enum/overview)
2. 提交问题到项目的 Issues
3. 联系技术支持团队

---

*最后更新时间: 2025-11-20*