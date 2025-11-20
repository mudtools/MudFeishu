# 飞书职位序列管理 API 文档

## 概述

序列是用户属性之一，用来为不同的用户定义不同的工作类型，例如产品、研发、测试、运营。可以根据企业实际需要添加序列，后续在创建或更新用户时，为用户设置相匹配的序列。通过序列 API，可以创建、更新、查询、删除序列信息。

**版本**: v3  
**基础URL**: `https://open.feishu.cn/open-apis/contact/v3/job_families`  
**完整文档**: [https://open.feishu.cn/document/contact-v3/job_family/job-family-resource-introduction](https://open.feishu.cn/document/contact-v3/job_family/job-family-resource-introduction)

---

## 创建职位序列

### 接口名称
创建一个序列。序列是用户属性之一，用来定义用户的工作类型，例如产品、研发、运营等。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/job_families
```

### 方法
POST

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| familyCreateRequest | JobFamilyCreateUpdateRequest | Body | 是 | 职位序列创建请求体 | 见下方示例 |

#### JobFamilyCreateUpdateRequest 结构

```json
{
  "name": "技术研发",
  "description": "负责公司产品技术研发相关工作",
  "parent_job_family_id": null,
  "status": true,
  "i18n_name": [
    {
      "locale": "zh_cn",
      "value": "技术研发"
    },
    {
      "locale": "en_us",
      "value": "R&D"
    }
  ],
  "i18n_description": [
    {
      "locale": "zh_cn",
      "value": "负责公司产品技术研发相关工作"
    },
    {
      "locale": "en_us",
      "value": "Responsible for product technology R&D"
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
    "job_family": {
      "job_family_id": "6989840515539468545",
      "name": "技术研发",
      "description": "负责公司产品技术研发相关工作",
      "parent_job_family_id": "",
      "status": true,
      "i18n_name": [
        {
          "locale": "zh_cn",
          "value": "技术研发"
        },
        {
          "locale": "en_us",
          "value": "R&D"
        }
      ],
      "i18n_description": [
        {
          "locale": "zh_cn",
          "value": "负责公司产品技术研发相关工作"
        },
        {
          "locale": "en_us",
          "value": "Responsible for product technology R&D"
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
var createRequest = new JobFamilyCreateUpdateRequest
{
    Name = "技术研发",
    Description = "负责公司产品技术研发相关工作",
    ParentJobFamilyId = null, // 顶级序列
    Status = true, // 启用状态
    I18nName = new List<I18nContent>
    {
        new() { Locale = "zh_cn", Value = "技术研发" },
        new() { Locale = "en_us", Value = "R&D" }
    },
    I18nDescription = new List<I18nContent>
    {
        new() { Locale = "zh_cn", Value = "负责公司产品技术研发相关工作" },
        new() { Locale = "en_us", Value = "Responsible for product technology R&D" }
    }
};

var result = await _jobFamiliesApi.CreateJobFamilyAsync(
    tenant_access_token,
    createRequest
);

if (result.Code == 0)
{
    var jobFamily = result.Data?.JobFamily;
    Console.WriteLine($"职位序列创建成功，ID: {jobFamily?.JobFamilyId}");
    Console.WriteLine($"名称: {jobFamily?.Name}");
    Console.WriteLine($"描述: {jobFamily?.Description}");
}
```

### 说明
- 序列名称在租户内必须唯一，支持中、英文及符号
- 描述字符长度上限为 5,000 字符
- `parent_job_family_id` 用于创建子序列，不填或为空时创建顶级序列
- `status`: true-启用，false-停用。只有启用的序列可以用于配置用户属性
- 支持多语言配置，包含中文、英文等

---

## 更新职位序列

### 接口名称
更新指定序列的信息。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/job_families/{job_family_id}
```

### 方法
PUT

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| job_family_id | string | Path | 是 | 序列ID | "6989840515539468545" |
| familyCreateRequest | JobFamilyCreateUpdateRequest | Body | 是 | 职位序列更新请求体 | 见下方示例 |

#### JobFamilyCreateUpdateRequest 结构

```json
{
  "name": "技术研发部",
  "description": "负责公司产品技术研发和创新工作，包括前后端开发、测试等全流程",
  "parent_job_family_id": null,
  "status": true,
  "i18n_name": [
    {
      "locale": "zh_cn",
      "value": "技术研发部"
    },
    {
      "locale": "en_us",
      "value": "R&D Department"
    }
  ],
  "i18n_description": [
    {
      "locale": "zh_cn",
      "value": "负责公司产品技术研发和创新工作，包括前后端开发、测试等全流程"
    },
    {
      "locale": "en_us",
      "value": "Responsible for product technology R&D and innovation, including full-stack development and testing"
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
    "job_family": {
      "job_family_id": "6989840515539468545",
      "name": "技术研发部",
      "description": "负责公司产品技术研发和创新工作，包括前后端开发、测试等全流程",
      "parent_job_family_id": "",
      "status": true,
      "i18n_name": [
        {
          "locale": "zh_cn",
          "value": "技术研发部"
        },
        {
          "locale": "en_us",
          "value": "R&D Department"
        }
      ],
      "i18n_description": [
        {
          "locale": "zh_cn",
          "value": "负责公司产品技术研发和创新工作，包括前后端开发、测试等全流程"
        },
        {
          "locale": "en_us",
          "value": "Responsible for product technology R&D and innovation, including full-stack development and testing"
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
  "msg": "job_family not found"
}
```

### 代码示例

```csharp
var updateRequest = new JobFamilyCreateUpdateRequest
{
    Name = "技术研发部",
    Description = "负责公司产品技术研发和创新工作，包括前后端开发、测试等全流程",
    ParentJobFamilyId = null,
    Status = true,
    I18nName = new List<I18nContent>
    {
        new() { Locale = "zh_cn", Value = "技术研发部" },
        new() { Locale = "en_us", Value = "R&D Department" }
    },
    I18nDescription = new List<I18nContent>
    {
        new() { Locale = "zh_cn", Value = "负责公司产品技术研发和创新工作，包括前后端开发、测试等全流程" },
        new() { Locale = "en_us", Value = "Responsible for product technology R&D and innovation, including full-stack development and testing" }
    }
};

var result = await _jobFamiliesApi.UpdateJobFamilyAsync(
    tenant_access_token,
    "6989840515539468545",
    updateRequest
);

if (result.Code == 0)
{
    Console.WriteLine("职位序列更新成功");
}
```

### 说明
- PUT 请求会完整替换序列信息，未传递的字段可能会被重置
- 序列名称在租户内仍然需要保持唯一
- 不能通过此接口将一个序列的父序列改为其子序列（会形成循环引用）

---

## 获取指定序列信息

### 接口名称
获取指定序列的信息，包括序列的名称、描述、启用状态以及 ID 等。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/job_families/{job_family_id}
```

### 方法
GET

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| job_family_id | string | Path | 是 | 序列ID | "6989840515539468545" |

### 响应

#### 成功响应
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_family": {
      "job_family_id": "6989840515539468545",
      "name": "技术研发部",
      "description": "负责公司产品技术研发和创新工作，包括前后端开发、测试等全流程",
      "parent_job_family_id": "",
      "status": true,
      "i18n_name": [
        {
          "locale": "zh_cn",
          "value": "技术研发部"
        },
        {
          "locale": "en_us",
          "value": "R&D Department"
        }
      ],
      "i18n_description": [
        {
          "locale": "zh_cn",
          "value": "负责公司产品技术研发和创新工作，包括前后端开发、测试等全流程"
        },
        {
          "locale": "en_us",
          "value": "Responsible for product technology R&D and innovation, including full-stack development and testing"
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
  "msg": "job_family not found"
}
```

### 代码示例

```csharp
var result = await _jobFamiliesApi.GetJobFamilyByIdAsync(
    tenant_access_token,
    "6989840515539468545"
);

if (result.Code == 0)
{
    var jobFamily = result.Data?.JobFamily;
    Console.WriteLine($"序列ID: {jobFamily?.JobFamilyId}");
    Console.WriteLine($"名称: {jobFamily?.Name}");
    Console.WriteLine($"描述: {jobFamily?.Description}");
    Console.WriteLine($"状态: {(jobFamily?.Status == true ? "启用" : "停用")}");
    Console.WriteLine($"父序列ID: {jobFamily?.ParentJobFamilyId ?? "无"}");
    
    if (jobFamily?.I18nName != null)
    {
        foreach (var name in jobFamily.I18nName)
        {
            Console.WriteLine($"多语言名称 ({name.Locale}): {name.Value}");
        }
    }
}
```

### 说明
- `parent_job_family_id` 为空字符串时表示顶级序列
- `status` 为 true 表示启用状态，false 表示停用状态
- 支持多语言内容显示

---

## 获取序列列表

### 接口名称
获取当前租户下的序列信息，包含序列的名称、描述、启用状态以及 ID 等。

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
| name | string | Query | 是 | 序列名称 | "技术" |
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
        "job_family_id": "6989840515539468545",
        "name": "技术研发部",
        "description": "负责公司产品技术研发和创新工作",
        "parent_job_family_id": "",
        "status": true,
        "i18n_name": [
          {
            "locale": "zh_cn",
            "value": "技术研发部"
          },
          {
            "locale": "en_us",
            "value": "R&D Department"
          }
        ],
        "i18n_description": [
          {
            "locale": "zh_cn",
            "value": "负责公司产品技术研发和创新工作"
          },
          {
            "locale": "en_us",
            "value": "Responsible for product technology R&D and innovation"
          }
        ]
      },
      {
        "job_family_id": "6989840515539468546",
        "name": "产品设计",
        "description": "负责产品规划和设计工作",
        "parent_job_family_id": "",
        "status": true,
        "i18n_name": [
          {
            "locale": "zh_cn",
            "value": "产品设计"
          },
          {
            "locale": "en_us",
            "value": "Product Design"
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
var result = await _jobFamiliesApi.GetJobFamilesListAsync(
    tenant_access_token,
    name: "", // 查询所有序列
    page_size: 20
);

if (result.Code == 0)
{
    Console.WriteLine($"找到 {result.Data?.Items?.Count} 个职位序列");
    
    foreach (var jobFamily in result.Data?.Items ?? [])
    {
        Console.WriteLine($"序列：{jobFamily.Name}，ID：{jobFamily.JobFamilyId}");
        Console.WriteLine($"描述：{jobFamily.Description}");
        Console.WriteLine($"状态：{(jobFamily.Status ? "启用" : "停用")}");
        Console.WriteLine($"父序列：{(string.IsNullOrEmpty(jobFamily.ParentJobFamilyId) ? "顶级序列" : jobFamily.ParentJobFamilyId)}");
        Console.WriteLine("---");
    }
}
```

### 说明
- `name` 参数支持模糊搜索，留空表示查询所有序列
- 支持分页查询，使用 `page_token` 获取下一页数据
- 返回的列表包含所有匹配的序列信息

---

## 删除职位序列

### 接口名称
删除指定序列。

### 飞书接口URL
```
https://open.feishu.cn/open-apis/contact/v3/job_families/{job_family_id}
```

### 方法
DELETE

### 认证
需要租户访问凭证（tenant_access_token）

### 参数

| 参数名 | 类型 | 位置 | 必填 | 说明 | 示例值 |
|--------|------|------|------|------|--------|
| tenant_access_token | string | Header | 是 | 应用访问凭证 | "cli_xxxxxxxxx" |
| job_family_id | string | Path | 是 | 序列ID | "6989840515539468545" |

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
  "msg": "Cannot delete job family with children"
}
```

### 代码示例

```csharp
var result = await _jobFamiliesApi.DeleteJobFamilyByIdAsync(
    tenant_access_token,
    "6989840515539468545"
);

if (result.Code == 0)
{
    Console.WriteLine("职位序列删除成功");
}
else if (result.Code == 403)
{
    Console.WriteLine("无法删除包含子序列的序列，请先删除所有子序列");
}
```

### 说明
- 仅支持删除没有子序列的序列
- 如果序列内存在子序列，则不能直接删除
- 删除前请确认没有用户在使用该序列
- 删除操作不可恢复，请谨慎使用

---

## 数据模型说明

### JobFamilyCreateUpdateRequest 序列创建/更新请求模型

| 字段名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| name | string | 是 | 序列名称，租户内唯一。支持中、英文及符号 |
| description | string | 否 | 序列描述，描述序列详情信息。字符长度上限为 5,000 |
| parent_job_family_id | string | 否 | 上级序列ID。如果需要为某一序列添加子序列，则需要传入该参数值 |
| status | bool | 否 | 是否启用序列。默认为 true |
| i18n_name | List<I18nContent> | 否 | 多语言序列名称 |
| i18n_description | List<I18nContent> | 否 | 多语言序列描述 |

### JobFamilyInfo 序列信息模型

| 字段名 | 类型 | 说明 |
|--------|------|------|
| job_family_id | string | 序列ID，唯一标识一个职位序列 |
| name | string | 序列名称 |
| description | string | 序列描述 |
| parent_job_family_id | string | 上级序列ID，空字符串表示顶级序列 |
| status | bool | 序列状态，true-启用，false-停用 |
| i18n_name | List<I18nContent> | 多语言序列名称 |
| i18n_description | List<I18nContent> | 多语言序列描述 |

---

## 错误码说明

| 错误码 | 说明 | 解决方案 |
|--------|------|----------|
| 0 | 成功 | - |
| 99991401 | access_token 无效或已过期 | 重新获取 access_token |
| 99991400 | 参数错误 | 检查请求参数格式和必填项 |
| 403 | 无权限或操作被禁止 | 检查应用权限或序列状态（如是否有子序列） |
| 404 | 序列不存在 | 确认 job_family_id 是否正确 |
| 400 | 请求参数无效 | 检查参数值是否符合规范（如名称重复、描述长度超限） |
| 429 | 请求频率超限 | 降低请求频率，使用限流策略 |
| 500 | 服务器内部错误 | 稍后重试或联系技术支持 |

---

## 最佳实践

### 1. 职位序列层级管理
```csharp
public class JobFamilyHierarchyManager
{
    private readonly IFeishuV3JobFamiliesApi _jobFamiliesApi;
    
    public async Task<JobFamilyTree> BuildJobFamilyTreeAsync()
    {
        // 获取所有序列
        var allFamilies = await GetAllJobFamiliesAsync();
        
        // 构建树形结构
        var tree = new JobFamilyTree();
        var familyMap = allFamilies.ToDictionary(f => f.JobFamilyId, f => f);
        
        foreach (var family in allFamilies.Where(f => string.IsNullOrEmpty(f.ParentJobFamilyId)))
        {
            var rootNode = BuildTreeNode(family, familyMap);
            tree.RootNodes.Add(rootNode);
        }
        
        return tree;
    }
    
    private JobFamilyTreeNode BuildTreeNode(JobFamilyInfo family, Dictionary<string, JobFamilyInfo> familyMap)
    {
        var node = new JobFamilyTreeNode
        {
            Id = family.JobFamilyId,
            Name = family.Name,
            Description = family.Description,
            Status = family.Status
        };
        
        // 递归构建子节点
        var children = familyMap.Values.Where(f => f.ParentJobFamilyId == family.JobFamilyId);
        foreach (var child in children)
        {
            node.Children.Add(BuildTreeNode(child, familyMap));
        }
        
        return node;
    }
    
    public async Task<List<JobFamilyInfo>> GetAllJobFamiliesAsync()
    {
        var allFamilies = new List<JobFamilyInfo>();
        string? pageToken = null;
        
        do
        {
            var result = await _jobFamiliesApi.GetJobFamilesListAsync(
                _token, 
                name: "", // 查询所有
                page_size: 50,
                page_token: pageToken
            );
            
            if (result.Code == 0 && result.Data?.Items != null)
            {
                allFamilies.AddRange(result.Data.Items);
                pageToken = result.Data.HasMore ? result.Data.PageToken : null;
            }
            else
            {
                break;
            }
            
        } while (!string.IsNullOrEmpty(pageToken));
        
        return allFamilies;
    }
}
```

### 2. 批量创建和管理
```csharp
public class JobFamilyBatchManager
{
    public async Task<BatchCreateResult> CreateJobFamiliesAsync(List<JobFamilyDefinition> definitions)
    {
        var results = new List<CreateResult>();
        
        foreach (var definition in definitions)
        {
            try
            {
                var request = new JobFamilyCreateUpdateRequest
                {
                    Name = definition.Name,
                    Description = definition.Description,
                    ParentJobFamilyId = definition.ParentJobFamilyId,
                    Status = definition.Status,
                    I18nName = definition.I18nName,
                    I18nDescription = definition.I18nDescription
                };
                
                var result = await _jobFamiliesApi.CreateJobFamilyAsync(_token, request);
                
                if (result.Code == 0)
                {
                    var jobFamilyId = result.Data?.JobFamily?.JobFamilyId ?? string.Empty;
                    results.Add(CreateResult.Success(definition.Name, jobFamilyId));
                }
                else
                {
                    results.Add(CreateResult.Failure(definition.Name, result.Msg ?? "Unknown error"));
                }
            }
            catch (Exception ex)
            {
                results.Add(CreateResult.Failure(definition.Name, ex.Message));
            }
            
            // 避免请求频率限制
            await Task.Delay(100);
        }
        
        return new BatchCreateResult
        {
            Success = results.Count(r => r.IsSuccess),
            Failure = results.Count(r => !r.IsSuccess),
            Results = results
        };
    }
    
    public async Task<bool> SafeDeleteJobFamilyAsync(string jobFamilyId)
    {
        try
        {
            // 先检查是否有子序列
            var allFamilies = await GetAllJobFamiliesAsync();
            var hasChildren = allFamilies.Any(f => f.ParentJobFamilyId == jobFamilyId);
            
            if (hasChildren)
            {
                Console.WriteLine($"序列 {jobFamilyId} 包含子序列，无法直接删除");
                return false;
            }
            
            var result = await _jobFamiliesApi.DeleteJobFamilyByIdAsync(_token, jobFamilyId);
            return result.Code == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删除序列失败: {ex.Message}");
            return false;
        }
    }
}
```

### 3. 国际化配置管理
```csharp
public class JobFamilyI18nManager
{
    public List<I18nContent> BuildI18nContent(
        string zhCnName, 
        string? enUsName = null,
        string? jaJpName = null,
        string zhCnDescription = "",
        string? enUsDescription = null,
        string? jaJpDescription = null)
    {
        var nameContent = new List<I18nContent>
        {
            new() { Locale = "zh_cn", Value = zhCnName }
        };
        
        var descContent = new List<I18nContent>();
        
        if (!string.IsNullOrEmpty(zhCnDescription))
        {
            descContent.Add(new() { Locale = "zh_cn", Value = zhCnDescription });
        }
        
        if (!string.IsNullOrEmpty(enUsName))
        {
            nameContent.Add(new() { Locale = "en_us", Value = enUsName });
        }
        
        if (!string.IsNullOrEmpty(enUsDescription))
        {
            descContent.Add(new() { Locale = "en_us", Value = enUsDescription });
        }
        
        if (!string.IsNullOrEmpty(jaJpName))
        {
            nameContent.Add(new() { Locale = "ja_jp", Value = jaJpName });
        }
        
        if (!string.IsNullOrEmpty(jaJpDescription))
        {
            descContent.Add(new() { Locale = "ja_jp", Value = jaJpDescription });
        }
        
        return nameContent; // 可以根据需要返回名称或描述内容
    }
    
    public string GetLocalizedContent(List<I18nContent> i18nContents, string locale = "zh_cn")
    {
        // 优先使用指定语言的内容
        var localized = i18nContents?.FirstOrDefault(c => c.Locale == locale);
        if (localized?.Value != null)
        {
            return localized.Value;
        }
        
        // 回退到中文
        var chinese = i18nContents?.FirstOrDefault(c => c.Locale == "zh_cn");
        if (chinese?.Value != null)
        {
            return chinese.Value;
        }
        
        return string.Empty;
    }
}
```

### 4. 缓存和性能优化
```csharp
public class JobFamilyCacheManager
{
    private readonly IMemoryCache _cache;
    private readonly IFeishuV3JobFamiliesApi _jobFamiliesApi;
    
    public async Task<Dictionary<string, JobFamilyInfo>> GetJobFamilyMapAsync()
    {
        const string cacheKey = "job_family_map";
        
        if (_cache.TryGetValue(cacheKey, out Dictionary<string, JobFamilyInfo> cachedMap))
        {
            return cachedMap;
        }
        
        // 获取所有序列
        var allFamilies = await GetAllJobFamiliesFromApiAsync();
        var familyMap = allFamilies.ToDictionary(f => f.JobFamilyId, f => f);
        
        // 缓存30分钟
        _cache.Set(cacheKey, familyMap, TimeSpan.FromMinutes(30));
        
        return familyMap;
    }
    
    public async Task<List<JobFamilyInfo>> GetEnabledJobFamiliesAsync()
    {
        var allFamilies = await GetJobFamilyMapAsync();
        return allFamilies.Values.Where(f => f.Status).ToList();
    }
    
    public async Task<JobFamilyInfo?> GetJobFamilyByIdAsync(string jobFamilyId)
    {
        var familyMap = await GetJobFamilyMapAsync();
        return familyMap.GetValueOrDefault(jobFamilyId);
    }
    
    private async Task<List<JobFamilyInfo>> GetAllJobFamiliesFromApiAsync()
    {
        var allFamilies = new List<JobFamilyInfo>();
        string? pageToken = null;
        
        do
        {
            var result = await _jobFamiliesApi.GetJobFamilesListAsync(
                _token, 
                name: "",
                page_size: 50,
                page_token: pageToken
            );
            
            if (result.Code == 0 && result.Data?.Items != null)
            {
                allFamilies.AddRange(result.Data.Items);
                pageToken = result.Data.HasMore ? result.Data.PageToken : null;
            }
            else
            {
                break;
            }
            
            await Task.Delay(50);
            
        } while (!string.IsNullOrEmpty(pageToken));
        
        return allFamilies;
    }
    
    public void InvalidateCache()
    {
        _cache.Remove("job_family_map");
        Console.WriteLine("职位序列缓存已清除");
    }
}
```

---

## 使用场景示例

### 1. 企业组织架构设计
```csharp
// 创建典型的技术部门职位序列
var techFamilies = new[]
{
    new JobFamilyDefinition 
    { 
        Name = "研发管理", 
        Description = "负责研发团队管理和项目规划",
        I18nName = new List<I18nContent>
        {
            new() { Locale = "zh_cn", Value = "研发管理" },
            new() { Locale = "en_us", Value = "R&D Management" }
        }
    },
    new JobFamilyDefinition 
    { 
        Name = "前端开发", 
        Description = "负责前端页面开发和用户交互实现",
        ParentJobFamilyId = "研发管理ID",
        I18nName = new List<I18nContent>
        {
            new() { Locale = "zh_cn", Value = "前端开发" },
            new() { Locale = "en_us", Value = "Frontend Development" }
        }
    },
    new JobFamilyDefinition 
    { 
        Name = "后端开发", 
        Description = "负责服务端开发和系统架构设计",
        ParentJobFamilyId = "研发管理ID",
        I18nName = new List<I18nContent>
        {
            new() { Locale = "zh_cn", Value = "后端开发" },
            new() { Locale = "en_us", Value = "Backend Development" }
        }
    }
};

// 批量创建
var result = await _batchManager.CreateJobFamiliesAsync(techFamilies.ToList());
Console.WriteLine($"成功创建 {result.Success} 个序列，失败 {result.Failure} 个");
```

### 2. 多语言国际化配置
```csharp
// 创建支持多语言的产品序列
var productFamily = new JobFamilyCreateUpdateRequest
{
    Name = "产品设计",
    Description = "负责产品规划、设计和用户体验优化",
    Status = true,
    I18nName = new List<I18nContent>
    {
        new() { Locale = "zh_cn", Value = "产品设计" },
        new() { Locale = "en_us", Value = "Product Design" },
        new() { Locale = "ja_jp", Value = "プロダクト設計" }
    },
    I18nDescription = new List<I18nContent>
    {
        new() { Locale = "zh_cn", Value = "负责产品规划、设计和用户体验优化" },
        new() { Locale = "en_us", Value = "Responsible for product planning, design and user experience optimization" },
        new() { Locale = "ja_jp", Value = "製品企画、設計、ユーザーエクスペリエンスの最適化を担当" }
    }
};

var result = await _jobFamiliesApi.CreateJobFamilyAsync(_token, productFamily);
```

### 3. 序列状态管理
```csharp
public async Task<bool> ToggleJobFamilyStatusAsync(string jobFamilyId, bool enable)
{
    // 先获取当前序列信息
    var currentInfo = await _jobFamiliesApi.GetJobFamilyByIdAsync(_token, jobFamilyId);
    if (currentInfo.Code != 0)
    {
        Console.WriteLine("获取序列信息失败");
        return false;
    }
    
    var family = currentInfo.Data?.JobFamily;
    if (family == null)
    {
        Console.WriteLine("序列不存在");
        return false;
    }
    
    var updateRequest = new JobFamilyCreateUpdateRequest
    {
        Name = family.Name,
        Description = family.Description,
        ParentJobFamilyId = family.ParentJobFamilyId,
        Status = enable,
        I18nName = family.I18nName,
        I18nDescription = family.I18nDescription
    };
    
    var result = await _jobFamiliesApi.UpdateJobFamilyAsync(_token, jobFamilyId, updateRequest);
    
    if (result.Code == 0)
    {
        Console.WriteLine($"序列 {(enable ? "启用" : "停用")}成功");
        return true;
    }
    else
    {
        Console.WriteLine($"状态更新失败: {result.Msg}");
        return false;
    }
}
```

---

## 版本更新记录

| 版本 | 日期 | 更新内容 |
|------|------|----------|
| v1.0 | 2025-11-01 | 初始版本，支持职位序列的基本CRUD操作 |

---

## 支持与反馈

如果您在使用过程中遇到问题，请通过以下方式获取帮助：

1. 查看 [飞书开放平台文档](https://open.feishu.cn/document/contact-v3/job_family/job-family-resource-introduction)
2. 提交问题到项目的 Issues
3. 联系技术支持团队

---

*最后更新时间: 2025-11-20*