# 飞书职级管理 API 文档

## 概述

职级是用户属性之一，可以根据企业组织架构的需要，添加职级，例如 P1、P2、P3、P4。后续在创建用户或者更新用户时，可以为用户设置指定的职级属性。使用职级 API，可以创建、更新、删除或查询职级。

**版本**: v3  
**基础URL**: `https://open.feishu.cn/open-apis/contact/v3/job_levels`  
**完整文档**: [https://open.feishu.cn/document/contact-v3/job_level/job-level-resources-introduction](https://open.feishu.cn/document/contact-v3/job_level/job-level-resources-introduction)

---

## 创建职级

### 接口名称
创建一个职级。职级是用户属性之一，用于标识用户的职位级别，例如 P1、P2、P3、P4。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/job_levels
```

### 方法
POST

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| levelCreateRequest | JobLevelCreateUpdateRequest | Body | 是 | 创建职级请求体 | 见下方示例 |

#### JobLevelCreateUpdateRequest 结构

```json
{
  "name": "高级专家",
  "description": "公司内部中高级职称，有一定专业技术能力的人员",
  "order": 300,
  "status": true,
  "i18n_name": [
    {
      "locale": "zh_cn",
      "value": "高级专家"
    },
    {
      "locale": "en_us",
      "value": "Senior Expert"
    }
  ],
  "i18n_description": [
    {
      "locale": "zh_cn",
      "value": "公司内部中高级职称，有一定专业技术能力的人员"
    },
    {
      "locale": "en_us",
      "value": "Intermediate to senior professional title within the company for personnel with certain professional technical capabilities"
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
    "job_level_id": "6989840515539468545",
    "name": "高级专家",
    "description": "公司内部中高级职称，有一定专业技术能力的人员",
    "order": 300,
    "status": true,
    "i18n_name": [
      {
        "locale": "zh_cn",
        "value": "高级专家"
      },
      {
        "locale": "en_us",
        "value": "Senior Expert"
      }
    ],
    "i18n_description": [
      {
        "locale": "zh_cn",
        "value": "公司内部中高级职称，有一定专业技术能力的人员"
      },
      {
        "locale": "en_us",
        "value": "Intermediate to senior professional title within the company for personnel with certain professional technical capabilities"
      }
    ]
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
var createRequest = new JobLevelCreateUpdateRequest
{
    Name = "高级专家",
    Description = "公司内部中高级职称，有一定专业技术能力的人员",
    Order = 300,
    Status = true, // 启用状态
    I18nName = new List<I18nContent>
    {
        new() { Locale = "zh_cn", Value = "高级专家" },
        new() { Locale = "en_us", Value = "Senior Expert" }
    },
    I18nDescription = new List<I18nContent>
    {
        new() { Locale = "zh_cn", Value = "公司内部中高级职称，有一定专业技术能力的人员" },
        new() { Locale = "en_us", Value = "Intermediate to senior professional title within the company for personnel with certain professional technical capabilities" }
    }
};

var result = await _jobLevelApi.CreateJobLevelAsync(
    tenant_access_token,
    createRequest
);

if (result.Code == 0)
{
    var jobLevel = result.Data;
    Console.WriteLine($"职级创建成功，ID: {jobLevel?.JobLevelId}");
    Console.WriteLine($"名称: {jobLevel?.Name}");
    Console.WriteLine($"排序: {jobLevel?.Order}");
    Console.WriteLine($"状态: {(jobLevel?.Status == true ? "启用" : "停用")}");
}
```

### 说明
- 职级名称在租户内应保持清晰，建议建立明确的职级体系
- 描述字符长度上限为 5,000 字符
- `order` 字段用于职级排序，数值越小，排序越靠前
- `status`: true-启用，false-停用。只有启用的职级可以用于配置用户属性
- 支持多语言配置，包含中文、英文等

---

## 更新职级

### 接口名称
更新指定职级的信息。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/job_levels/{job_level_id}
```

### 方法
PUT

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| job_level_id | string | Path | 是 | 职级ID | "6989840515539468545" |
| levelCreateRequest | JobLevelCreateUpdateRequest | Body | 是 | 更新职级请求体 | 见下方示例 |

#### JobLevelCreateUpdateRequest 结构

```json
{
  "name": "资深专家",
  "description": "公司内部高级职称，具有丰富专业技术能力和经验的核心人员",
  "order": 250,
  "status": true,
  "i18n_name": [
    {
      "locale": "zh_cn",
      "value": "资深专家"
    },
    {
      "locale": "en_us",
      "value": "Principal Expert"
    }
  ],
  "i18n_description": [
    {
      "locale": "zh_cn",
      "value": "公司内部高级职称，具有丰富专业技术能力和经验的核心人员"
    },
    {
      "locale": "en_us",
      "value": "Senior professional title within the company for core personnel with rich professional technical capabilities and experience"
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
    "job_level_id": "6989840515539468545",
    "name": "资深专家",
    "description": "公司内部高级职称，具有丰富专业技术能力和经验的核心人员",
    "order": 250,
    "status": true,
    "i18n_name": [
      {
        "locale": "zh_cn",
        "value": "资深专家"
      },
      {
        "locale": "en_us",
        "value": "Principal Expert"
      }
    ],
    "i18n_description": [
      {
        "locale": "zh_cn",
        "value": "公司内部高级职称，具有丰富专业技术能力和经验的核心人员"
      },
      {
        "locale": "en_us",
        "value": "Senior professional title within the company for core personnel with rich professional technical capabilities and experience"
      }
    ]
  }
}
```

#### 错误响应
```json
{
  "code": 404,
  "msg": "job_level not found"
}
```

### 代码示例

```csharp
var updateRequest = new JobLevelCreateUpdateRequest
{
    Name = "资深专家",
    Description = "公司内部高级职称，具有丰富专业技术能力和经验的核心人员",
    Order = 250,
    Status = true,
    I18nName = new List<I18nContent>
    {
        new() { Locale = "zh_cn", Value = "资深专家" },
        new() { Locale = "en_us", Value = "Principal Expert" }
    },
    I18nDescription = new List<I18nContent>
    {
        new() { Locale = "zh_cn", Value = "公司内部高级职称，具有丰富专业技术能力和经验的核心人员" },
        new() { Locale = "en_us", Value = "Senior professional title within the company for core personnel with rich professional technical capabilities and experience" }
    }
};

var result = await _jobLevelApi.UpdateJobLevelAsync(
    tenant_access_token,
    "6989840515539468545",
    updateRequest
);

if (result.Code == 0)
{
    Console.WriteLine("职级更新成功");
}
```

### 说明
- PUT 请求会完整替换职级信息，未传递的字段可能会被重置
- 建议在更新前先获取当前职级信息，确保数据的完整性
- `order` 字段的更新会影响职级在列表中的显示顺序

---

## 获取指定职级信息

### 接口名称
获取指定职级的信息，包括职级名称、描述、排序、状态以及多语言等。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/job_levels/{job_level_id}
```

### 方法
GET

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| job_level_id | string | Path | 是 | 职级ID | "6989840515539468545" |

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_level_id": "6989840515539468545",
    "name": "资深专家",
    "description": "公司内部高级职称，具有丰富专业技术能力和经验的核心人员",
    "order": 250,
    "status": true,
    "i18n_name": [
      {
        "locale": "zh_cn",
        "value": "资深专家"
      },
      {
        "locale": "en_us",
        "value": "Principal Expert"
      }
    ],
    "i18n_description": [
      {
        "locale": "zh_cn",
        "value": "公司内部高级职称，具有丰富专业技术能力和经验的核心人员"
      },
      {
        "locale": "en_us",
        "value": "Senior professional title within the company for core personnel with rich professional technical capabilities and experience"
      }
    ]
  }
}
```

#### 错误响应
```json
{
  "code": 404,
  "msg": "job_level not found"
}
```

### 代码示例

```csharp
var result = await _jobLevelApi.GetJobLevelByIdAsync(
    tenant_access_token,
    "6989840515539468545"
);

if (result.Code == 0)
{
    var jobLevel = result.Data;
    Console.WriteLine($"职级ID: {jobLevel?.JobLevelId}");
    Console.WriteLine($"名称: {jobLevel?.Name}");
    Console.WriteLine($"描述: {jobLevel?.Description}");
    Console.WriteLine($"排序: {jobLevel?.Order}");
    Console.WriteLine($"状态: {(jobLevel?.Status == true ? "启用" : "停用")}");
    
    if (jobLevel?.I18nName != null)
    {
        foreach (var name in jobLevel.I18nName)
        {
            Console.WriteLine($"多语言名称 ({name.Locale}): {name.Value}");
        }
    }
}
```

### 说明
- 返回职级的完整信息，包括所有配置的多语言内容
- `order` 字段可用于职级的排序显示
- `status` 为 true 表示启用状态，false 表示停用状态

---

## 获取职级列表

### 接口名称
获取当前租户下的职级信息，包括职级名称、描述、排序、状态以及多语言等。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/job_levels
```

### 方法
GET

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| name | string | Query | 是 | 职级名称 | "专家" |
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
        "job_level_id": "6989840515539468545",
        "name": "资深专家",
        "description": "公司内部高级职称，具有丰富专业技术能力和经验的核心人员",
        "order": 250,
        "status": true,
        "i18n_name": [
          {
            "locale": "zh_cn",
            "value": "资深专家"
          },
          {
            "locale": "en_us",
            "value": "Principal Expert"
          }
        ],
        "i18n_description": [
          {
            "locale": "zh_cn",
            "value": "公司内部高级职称，具有丰富专业技术能力和经验的核心人员"
          },
          {
            "locale": "en_us",
            "value": "Senior professional title within the company for core personnel with rich professional technical capabilities and experience"
          }
        ]
      },
      {
        "job_level_id": "6989840515539468546",
        "name": "高级专家",
        "description": "公司内部中高级职称，有一定专业技术能力的人员",
        "order": 300,
        "status": true,
        "i18n_name": [
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
var result = await _jobLevelApi.GetJobLevelListAsync(
    tenant_access_token,
    name: "", // 查询所有职级
    page_size: 20
);

if (result.Code == 0)
{
    Console.WriteLine($"找到 {result.Data?.Items?.Count} 个职级");
    
    // 按 order 字段排序显示
    var sortedLevels = result.Data?.Items?
        .OrderBy(l => l.Order)
        .ToList() ?? new List<JobLevelResult>();
    
    foreach (var jobLevel in sortedLevels)
    {
        Console.WriteLine($"职级：{jobLevel.Name}，ID：{jobLevel.JobLevelId}");
        Console.WriteLine($"排序：{jobLevel.Order}，状态：{(jobLevel.Status ? "启用" : "停用")}");
        Console.WriteLine($"描述：{jobLevel.Description}");
        Console.WriteLine("---");
    }
}
```

### 说明
- `name` 参数支持模糊搜索，留空表示查询所有职级
- 返回的数据通常需要按 `order` 字段排序来获得正确的职级顺序
- 支持分页查询，使用 `page_token` 获取下一页数据
- 只返回状态为启用和停用的所有职级

---

## 删除职级

### 接口名称
删除指定的职级。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/job_levels/{job_level_id}
```

### 方法
DELETE

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| job_level_id | string | Path | 是 | 职级ID | "6989840515539468545" |

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
  "msg": "Cannot delete job level that is in use"
}
```

### 代码示例

```csharp
var result = await _jobLevelApi.DeleteJobLevelByIdAsync(
    tenant_access_token,
    "6989840515539468545"
);

if (result.Code == 0)
{
    Console.WriteLine("职级删除成功");
}
else if (result.Code == 403)
{
    Console.WriteLine("无法删除正在使用的职级，请先移除使用该职级的用户");
}
```

### 说明
- 删除前请确认没有用户在使用该职级
- 如果职级正在被使用，则不能直接删除
- 删除操作不可恢复，请谨慎使用
- 建议在删除前先停用职级，确认无影响后再删除

---

## 数据模型说明

### JobLevelCreateUpdateRequest 职级创建/更新请求模型

| 字段名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| name | string | 是 | 职级名称。通用名称，如果未设置多语言名称，则默认展示该名称 |
| description | string | 否 | 职级描述。字符长度上限 5,000。通用描述，如果未设置多语言描述，则默认展示该描述 |
| order | int | 否 | 职级排序。数值越小，排序越靠前 |
| status | bool | 否 | 是否启用该职级。默认为 true |
| i18n_name | List<I18nContent> | 否 | 多语言职级名称 |
| i18n_description | List<I18nContent> | 否 | 多语言职级描述 |

### JobLevelResult 职级信息模型

| 字段名 | 类型 | 说明 |
|--------|------|------|
| job_level_id | string | 职级ID，唯一标识一个职级 |
| name | string | 职级名称。通用名称，如果未设置多语言名称，则默认展示该名称 |
| description | string | 职级描述。字符长度上限 5,000。通用描述，如果未设置多语言描述，则默认展示该描述 |
| order | int | 职级排序。数值越小，排序越靠前 |
| status | bool | 是否启用该职级 |
| i18n_name | List<I18nContent> | 多语言职级名称 |
| i18n_description | List<I18nContent> | 多语言职级描述 |

---

## 错误码说明

| 错误码 | 说明 | 解决方案 |
|--------|------|----------|
| 0 | 成功 | - |
| 99991401 | access_token 无效或已过期 | 重新获取 access_token |
| 99991400 | 参数错误 | 检查请求参数格式和必填项 |
| 403 | 无权限或操作被禁止 | 检查应用权限或职级状态（如是否正在使用） |
| 404 | 职级不存在 | 确认 job_level_id 是否正确 |
| 400 | 请求参数无效 | 检查参数值是否符合规范（如名称重复、描述长度超限） |
| 429 | 请求频率超限 | 降低请求频率，使用限流策略 |
| 500 | 服务器内部错误 | 稍后重试或联系技术支持 |

---

## 最佳实践

### 1. 职级体系设计
```csharp
public class JobLevelSystemManager
{
    private readonly IFeishuV3JobLevelApi _jobLevelApi;
    
    // 标准职级体系模板
    public class JobLevelTemplate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public List<I18nContent> I18nName { get; set; } = new();
        public List<I18nContent> I18nDescription { get; set; } = new();
    }
    
    public static readonly List<JobLevelTemplate> StandardLevels = new()
    {
        new JobLevelTemplate
        {
            Name = "实习生",
            Description = "公司实习人员，学习期员工",
            Order = 1000,
            I18nName = new List<I18nContent>
            {
                new() { Locale = "zh_cn", Value = "实习生" },
                new() { Locale = "en_us", Value = "Intern" }
            }
        },
        new JobLevelTemplate
        {
            Name = "初级工程师",
            Description = "初级技术人员，需要指导的员工",
            Order = 800,
            I18nName = new List<I18nContent>
            {
                new() { Locale = "zh_cn", Value = "初级工程师" },
                new() { Locale = "en_us", Value = "Junior Engineer" }
            }
        },
        new JobLevelTemplate
        {
            Name = "中级工程师",
            Description = "中级技术人员，能够独立工作的员工",
            Order = 600,
            I18nName = new List<I18nContent>
            {
                new() { Locale = "zh_cn", Value = "中级工程师" },
                new() { Locale = "en_us", Value = "Engineer" }
            }
        },
        new JobLevelTemplate
        {
            Name = "高级工程师",
            Description = "高级技术人员，技术骨干员工",
            Order = 400,
            I18nName = new List<I18nContent>
            {
                new() { Locale = "zh_cn", Value = "高级工程师" },
                new() { Locale = "en_us", Value = "Senior Engineer" }
            }
        },
        new JobLevelTemplate
        {
            Name = "专家",
            Description = "技术专家，具有深厚技术积累的员工",
            Order = 200,
            I18nName = new List<I18nContent>
            {
                new() { Locale = "zh_cn", Value = "专家" },
                new() { Locale = "en_us", Value = "Expert" }
            }
        },
        new JobLevelTemplate
        {
            Name = "资深专家",
            Description = "资深技术专家，行业内有影响力的员工",
            Order = 100,
            I18nName = new List<I18nContent>
            {
                new() { Locale = "zh_cn", Value = "资深专家" },
                new() { Locale = "en_us", Value = "Principal Expert" }
            }
        }
    };
    
    public async Task<bool> SetupStandardJobLevelsAsync()
    {
        try
        {
            var existingLevels = await GetAllJobLevelsAsync();
            var existingNames = existingLevels.Select(l => l.Name).ToHashSet();
            
            var successCount = 0;
            foreach (var template in StandardLevels)
            {
                if (!existingNames.Contains(template.Name))
                {
                    var request = new JobLevelCreateUpdateRequest
                    {
                        Name = template.Name,
                        Description = template.Description,
                        Order = template.Order,
                        Status = true,
                        I18nName = template.I18nName,
                        I18nDescription = template.I18nDescription
                    };
                    
                    var result = await _jobLevelApi.CreateJobLevelAsync(_token, request);
                    if (result.Code == 0)
                    {
                        successCount++;
                        Console.WriteLine($"创建职级 '{template.Name}' 成功");
                    }
                    else
                    {
                        Console.WriteLine($"创建职级 '{template.Name}' 失败: {result.Msg}");
                    }
                    
                    await Task.Delay(100); // 避免限流
                }
            }
            
            Console.WriteLine($"成功创建 {successCount} 个标准职级");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"设置标准职级失败: {ex.Message}");
            return false;
        }
    }
    
    private async Task<List<JobLevelResult>> GetAllJobLevelsAsync()
    {
        var allLevels = new List<JobLevelResult>();
        string? pageToken = null;
        
        do
        {
            var result = await _jobLevelApi.GetJobLevelListAsync(
                _token, 
                name: "", // 查询所有
                page_size: 50,
                page_token: pageToken
            );
            
            if (result.Code == 0 && result.Data?.Items != null)
            {
                allLevels.AddRange(result.Data.Items);
                pageToken = result.Data.HasMore ? result.Data.PageToken : null;
            }
            else
            {
                break;
            }
            
            await Task.Delay(50);
            
        } while (!string.IsNullOrEmpty(pageToken));
        
        return allLevels;
    }
}
```

### 2. 职级排序和层级管理
```csharp
public class JobLevelOrderManager
{
    private readonly IFeishuV3JobLevelApi _jobLevelApi;
    
    public async Task<bool> ReorderJobLevelsAsync(Dictionary<string, int> newOrders)
    {
        try
        {
            // 获取当前所有职级
            var allLevels = await GetAllJobLevelsAsync();
            
            foreach (var level in allLevels)
            {
                if (newOrders.TryGetValue(level.JobLevelId ?? "", out int newOrder))
                {
                    var updateRequest = new JobLevelCreateUpdateRequest
                    {
                        Name = level.Name ?? "",
                        Description = level.Description,
                        Order = newOrder,
                        Status = level.Status,
                        I18nName = level.I18nName,
                        I18nDescription = level.I18nDescription
                    };
                    
                    var result = await _jobLevelApi.UpdateJobLevelAsync(
                        _token, 
                        level.JobLevelId ?? "", 
                        updateRequest
                    );
                    
                    if (result.Code == 0)
                    {
                        Console.WriteLine($"职级 '{level.Name}' 排序更新为 {newOrder}");
                    }
                    
                    await Task.Delay(100);
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"职级排序失败: {ex.Message}");
            return false;
        }
    }
    
    public async Task<List<JobLevelResult>> GetSortedJobLevelsAsync()
    {
        var allLevels = await GetAllJobLevelsAsync();
        return allLevels
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Name)
            .ToList();
    }
    
    public int GetLevelIndex(List<JobLevelResult> sortedLevels, string jobLevelId)
    {
        var level = sortedLevels.FirstOrDefault(l => l.JobLevelId == jobLevelId);
        return level != null ? sortedLevels.IndexOf(level) : -1;
    }
    
    public bool IsHigherLevel(List<JobLevelResult> sortedLevels, string currentLevelId, string targetLevelId)
    {
        var currentIndex = GetLevelIndex(sortedLevels, currentLevelId);
        var targetIndex = GetLevelIndex(sortedLevels, targetLevelId);
        
        return currentIndex >= 0 && targetIndex >= 0 && targetIndex < currentIndex;
    }
}
```

### 3. 批量操作和状态管理
```csharp
public class JobLevelBatchManager
{
    private readonly IFeishuV3JobLevelApi _jobLevelApi;
    
    public async Task<BatchOperationResult> BatchCreateJobLevelsAsync(List<JobLevelDefinition> definitions)
    {
        var results = new List<OperationResult>();
        
        foreach (var definition in definitions)
        {
            try
            {
                var request = new JobLevelCreateUpdateRequest
                {
                    Name = definition.Name,
                    Description = definition.Description,
                    Order = definition.Order,
                    Status = definition.IsActive,
                    I18nName = definition.I18nName,
                    I18nDescription = definition.I18nDescription
                };
                
                var result = await _jobLevelApi.CreateJobLevelAsync(_token, request);
                
                if (result.Code == 0)
                {
                    var levelId = result.Data?.JobLevelId ?? string.Empty;
                    results.Add(OperationResult.Success(definition.Name, levelId));
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
            
            await Task.Delay(100); // 避免限流
        }
        
        return new BatchOperationResult
        {
            Success = results.Count(r => r.IsSuccess),
            Failure = results.Count(r => !r.IsSuccess),
            Results = results
        };
    }
    
    public async Task<bool> ToggleJobLevelStatusAsync(string jobLevelId, bool enable)
    {
        try
        {
            // 先获取当前职级信息
            var currentInfo = await _jobLevelApi.GetJobLevelByIdAsync(_token, jobLevelId);
            if (currentInfo.Code != 0)
            {
                Console.WriteLine("获取职级信息失败");
                return false;
            }
            
            var level = currentInfo.Data;
            if (level == null)
            {
                Console.WriteLine("职级不存在");
                return false;
            }
            
            var updateRequest = new JobLevelCreateUpdateRequest
            {
                Name = level.Name ?? "",
                Description = level.Description,
                Order = level.Order,
                Status = enable,
                I18nName = level.I18nName,
                I18nDescription = level.I18nDescription
            };
            
            var result = await _jobLevelApi.UpdateJobLevelAsync(_token, jobLevelId, updateRequest);
            
            if (result.Code == 0)
            {
                Console.WriteLine($"职级 {(enable ? "启用" : "停用")}成功");
                return true;
            }
            else
            {
                Console.WriteLine($"状态更新失败: {result.Msg}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"更新职级状态失败: {ex.Message}");
            return false;
        }
    }
    
    public async Task<bool> SafeDeleteJobLevelAsync(string jobLevelId)
    {
        try
        {
            // 先停用职级
            var disableSuccess = await ToggleJobLevelStatusAsync(jobLevelId, false);
            if (!disableSuccess)
            {
                Console.WriteLine("无法停用职级，删除终止");
                return false;
            }
            
            // 等待一段时间，确保停用生效
            await Task.Delay(2000);
            
            // 尝试删除
            var result = await _jobLevelApi.DeleteJobLevelByIdAsync(_token, jobLevelId);
            
            if (result.Code == 0)
            {
                Console.WriteLine("职级删除成功");
                return true;
            }
            else if (result.Code == 403)
            {
                Console.WriteLine("职级正在使用中，无法删除，已恢复启用状态");
                await ToggleJobLevelStatusAsync(jobLevelId, true);
                return false;
            }
            else
            {
                Console.WriteLine($"删除失败: {result.Msg}，已恢复启用状态");
                await ToggleJobLevelStatusAsync(jobLevelId, true);
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删除职级时发生异常: {ex.Message}");
            return false;
        }
    }
}
```

### 4. 缓存和性能优化
```csharp
public class JobLevelCacheManager
{
    private readonly IMemoryCache _cache;
    private readonly IFeishuV3JobLevelApi _jobLevelApi;
    private readonly ILogger<JobLevelCacheManager> _logger;
    
    public async Task<Dictionary<string, JobLevelResult>> GetJobLevelMapAsync()
    {
        const string cacheKey = "job_level_map";
        
        if (_cache.TryGetValue(cacheKey, out Dictionary<string, JobLevelResult> cachedMap))
        {
            return cachedMap;
        }
        
        var result = await _jobLevelApi.GetJobLevelListAsync(_token, name: "", page_size: 50);
        
        if (result.Code == 0 && result.Data?.Items != null)
        {
            var levelMap = result.Data.Items.ToDictionary(
                l => l.JobLevelId ?? string.Empty, 
                l => l
            );
            
            // 缓存30分钟
            _cache.Set(cacheKey, levelMap, TimeSpan.FromMinutes(30));
            
            _logger.LogInformation($"缓存了 {levelMap.Count} 个职级");
            return levelMap;
        }
        
        return new Dictionary<string, JobLevelResult>();
    }
    
    public async Task<List<JobLevelResult>> GetSortedJobLevelsAsync()
    {
        var levelMap = await GetJobLevelMapAsync();
        return levelMap.Values
            .Where(l => l.Status) // 只返回启用的职级
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Name)
            .ToList();
    }
    
    public async Task<JobLevelResult?> GetJobLevelByIdAsync(string jobLevelId)
    {
        var levelMap = await GetJobLevelMapAsync();
        return levelMap.GetValueOrDefault(jobLevelId);
    }
    
    public async Task<JobLevelResult?> GetJobLevelByNameAsync(string name)
    {
        var levelMap = await GetJobLevelMapAsync();
        return levelMap.Values.FirstOrDefault(l => l.Name == name);
    }
    
    public void InvalidateCache()
    {
        _cache.Remove("job_level_map");
        _logger.LogInformation("职级缓存已清除");
    }
    
    public async Task PreWarmCacheAsync()
    {
        try
        {
            await GetJobLevelMapAsync();
            _logger.LogInformation("职级缓存预热完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "职级缓存预热失败");
        }
    }
    
    public async Task<bool> ValidateJobLevelAsync(string jobLevelId)
    {
        var level = await GetJobLevelByIdAsync(jobLevelId);
        return level != null && level.Status;
    }
    
    public async Task<List<string>> GetJobLevelNamesAsync()
    {
        var sortedLevels = await GetSortedJobLevelsAsync();
        return sortedLevels.Select(l => l.Name ?? "").ToList();
    }
}
```

---

## 使用场景示例

### 1. 企业职级体系初始化
```csharp
// 创建企业标准职级体系
var manager = new JobLevelSystemManager(_jobLevelApi, _token);

Console.WriteLine("开始设置标准职级体系...");
var success = await manager.SetupStandardJobLevelsAsync();

if (success)
{
    Console.WriteLine("职级体系设置完成");
    
    // 显示设置的职级
    var levels = await manager.GetAllJobLevelsAsync();
    foreach (var level in levels.OrderBy(l => l.Order))
    {
        Console.WriteLine($"- {level.Name} (排序: {level.Order})");
    }
}
```

### 2. 多语言职级配置
```csharp
// 创建支持多语言的技术职级
var techLevels = new[]
{
    new JobLevelCreateUpdateRequest
    {
        Name = "T1 - 初级工程师",
        Description = "初级技术人员，需要指导完成工作任务",
        Order = 700,
        Status = true,
        I18nName = new List<I18nContent>
        {
            new() { Locale = "zh_cn", Value = "T1 - 初级工程师" },
            new() { Locale = "en_us", Value = "T1 - Junior Engineer" },
            new() { Locale = "ja_jp", Value = "T1 - 初級エンジニア" }
        },
        I18nDescription = new List<I18nContent>
        {
            new() { Locale = "zh_cn", Value = "初级技术人员，需要指导完成工作任务" },
            new() { Locale = "en_us", Value = "Junior technical staff who need guidance to complete work tasks" },
            new() { Locale = "ja_jp", Value = "初級技術者、指導を受けて作業タスクを完了する必要がある" }
        }
    },
    new JobLevelCreateUpdateRequest
    {
        Name = "T5 - 首席工程师",
        Description = "技术领军人物，负责技术路线规划和团队技术指导",
        Order = 100,
        Status = true,
        I18nName = new List<I18nContent>
        {
            new() { Locale = "zh_cn", Value = "T5 - 首席工程师" },
            new() { Locale = "en_us", Value = "T5 - Principal Engineer" },
            new() { Locale = "ja_jp", Value = "T5 - 主任エンジニア" }
        },
        I18nDescription = new List<I18nContent>
        {
            new() { Locale = "zh_cn", Value = "技术领军人物，负责技术路线规划和团队技术指导" },
            new() { Locale = "en_us", Value = "Technical leader responsible for technical roadmap planning and team technical guidance" },
            new() { Locale = "ja_jp", Value = "技術リーダー、技術ロードマップ計画とチーム技術指導を担当" }
        }
    }
};

foreach (var levelDef in techLevels)
{
    var result = await _jobLevelApi.CreateJobLevelAsync(_token, levelDef);
    
    if (result.Code == 0)
    {
        Console.WriteLine($"创建多语言职级 '{levelDef.Name}' 成功");
    }
    else
    {
        Console.WriteLine($"创建职级 '{levelDef.Name}' 失败: {result.Msg}");
    }
}
```

### 3. 职级晋升管理
```csharp
public class JobLevelPromotionManager
{
    private readonly JobLevelOrderManager _orderManager;
    
    public async Task<bool> ValidatePromotionAsync(string currentLevelId, string targetLevelId)
    {
        var sortedLevels = await _orderManager.GetSortedJobLevelsAsync();
        
        // 检查目标职级是否更高
        var isPromotion = _orderManager.IsHigherLevel(sortedLevels, currentLevelId, targetLevelId);
        if (!isPromotion)
        {
            Console.WriteLine("目标职级不是更高级别");
            return false;
        }
        
        // 检查是否跳级过多（最多跳2级）
        var currentIndex = _orderManager.GetLevelIndex(sortedLevels, currentLevelId);
        var targetIndex = _orderManager.GetLevelIndex(sortedLevels, targetLevelId);
        var levelGap = currentIndex - targetIndex;
        
        if (levelGap > 2)
        {
            Console.WriteLine($"跳级过多，建议逐级晋升（当前差距：{levelGap}级）");
            return false;
        }
        
        return true;
    }
    
    public async Task<List<string>> GetPromotionPathAsync(string currentLevelId, string targetLevelId)
    {
        var sortedLevels = await _orderManager.GetSortedJobLevelsAsync();
        var currentIndex = _orderManager.GetLevelIndex(sortedLevels, currentLevelId);
        var targetIndex = _orderManager.GetLevelIndex(sortedLevels, targetLevelId);
        
        var path = new List<string>();
        
        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= currentIndex)
        {
            return path;
        }
        
        // 从当前职级到目标职级的路径
        for (int i = currentIndex - 1; i >= targetIndex; i--)
        {
            if (i >= 0 && i < sortedLevels.Count)
            {
                path.Add(sortedLevels[i].Name ?? "");
            }
        }
        
        return path;
    }
}
```

---

## 版本更新记录

| 版本 | 日期 | 更新内容 |
|------|------|----------|
| v1.0 | 2024-01-01 | 初始版本，支持职级的基本CRUD操作 |

---

## 支持与反馈

如果您在使用过程中遇到问题，请通过以下方式获取帮助：

1. 查看 [飞书开放平台文档](https://open.feishu.cn/document/contact-v3/job_level/job-level-resources-introduction)
2. 提交问题到项目的 Issues
3. 联系技术支持团队

---

*最后更新时间: 2025-11-20*