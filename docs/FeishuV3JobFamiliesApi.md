# 职位序列管理

## 功能描述
序列是用户属性之一，用来为不同的用户定义不同的工作类型，例如产品、研发、测试、运营等。可以根据企业实际需要添加序列，后续在创建或更新用户时，为用户设置相匹配的序列。该API提供完整的职位序列管理功能，支持创建层级化的序列结构、序列信息的查询更新和删除，满足企业精细化的人员分类和岗位管理需求。

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP方法 |
|---------|---------|---------|---------|
| CreateJobFamilyAsync | 创建一个序列 | tenant_access_token | POST |
| UpdateJobFamilyAsync | 更新指定序列的信息 | tenant_access_token | PUT |
| GetJobFamilyByIdAsync | 获取指定序列的信息 | tenant_access_token | GET |
| GetJobFamilesListAsync | 获取当前租户下的序列信息列表 | tenant_access_token | GET |
| DeleteJobFamilyByIdAsync | 删除指定序列 | tenant_access_token | DELETE |

## CreateJobFamilyAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- familyCreateRequest (必填) - 职位序列创建请求体

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "job_family": {
      "job_family_id": "6968045635650973636",
      "name": "技术研发",
      "description": "负责公司核心技术研发和产品开发",
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
          "value": "负责公司核心技术研发"
        },
        {
          "locale": "en_us",
          "value": "Core technology R&D"
        }
      ]
    }
  }
}

错误响应：
{
  "code": 400,
  "msg": "序列名称已存在"
}
```

**说明**：
- 序列名称在租户内必须唯一
- 支持创建层级化的序列结构，通过parent_job_family_id指定父序列
- description字符长度上限为5,000
- 支持多语言配置
- status默认为true（启用状态）

**代码示例**：
```csharp
// 创建新的职位序列
var createRequest = new JobFamilyCreateUpdateRequest
{
    Name = "技术研发",
    Description = "负责公司核心技术研发和产品开发，包括前端、后端、移动端等技术方向",
    Status = true,
    ParentJobFamilyId = null, // 顶级序列
    I18nName = new List<I18nContent>
    {
        new I18nContent
        {
            Locale = "zh_cn",
            Value = "技术研发"
        },
        new I18nContent
        {
            Locale = "en_us",
            Value = "R&D Department"
        }
    },
    I18nDescription = new List<I18nContent>
    {
        new I18nContent
        {
            Locale = "zh_cn",
            Value = "负责公司核心技术研发和产品开发"
        },
        new I18nContent
        {
            Locale = "en_us",
            Value = "Responsible for core technology R&D and product development"
        }
    }
};

var result = await _feishuApi.CreateJobFamilyAsync(
    tenantAccessToken,
    createRequest);

if (result.Success)
{
    var jobFamily = result.Data.JobFamily;
    Console.WriteLine($"序列创建成功：{jobFamily.Name} (ID: {jobFamily.JobFamilyId})");
    Console.WriteLine($"描述：{jobFamily.Description}");
    Console.WriteLine($"状态：{(jobFamily.Status ? "启用" : "停用")");
    
    // 显示多语言配置
    if (jobFamily.I18nName != null)
    {
        foreach (var i18n in jobFamily.I18nName)
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

## UpdateJobFamilyAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- job_family_id (必填) - 序列ID
- familyCreateRequest (必填) - 职位序列更新请求体

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "job_family": {
      "job_family_id": "6968045635650973636",
      "name": "技术研发部",
      "description": "负责公司核心技术研发、产品开发和技术创新",
      "parent_job_family_id": "",
      "status": true
    }
  }
}

错误响应：
{
  "code": 404,
  "msg": "序列不存在"
}
```

**说明**：
- 可以更新序列的所有字段
- 序列名称仍需保持租户内唯一
- 更新父序列ID时要注意层级关系

**代码示例**：
```csharp
// 更新职位序列信息
var updateRequest = new JobFamilyCreateUpdateRequest
{
    Name = "技术研发部",
    Description = "负责公司核心技术研发、产品开发和技术创新工作",
    Status = true,
    ParentJobFamilyId = null,
    I18nName = new List<I18nContent>
    {
        new I18nContent
        {
            Locale = "zh_cn",
            Value = "技术研发部"
        },
        new I18nContent
        {
            Locale = "en_us",
            Value = "R&D Department"
        }
    }
};

var result = await _feishuApi.UpdateJobFamilyAsync(
    tenantAccessToken,
    "6968045635650973636",
    updateRequest);

if (result.Success)
{
    var updatedFamily = result.Data.JobFamily;
    Console.WriteLine($"序列更新成功：{updatedFamily.Name}");
    
    // 如果要停用序列
    if (!updatedFamily.Status)
    {
        Console.WriteLine("注意：该序列已被停用，现有用户不受影响但新建用户无法选择此序列");
    }
}
else
{
    Console.WriteLine($"更新失败：{result.Message}");
}
```

---

## GetJobFamilyByIdAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- job_family_id (必填) - 序列ID

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "job_family": {
      "job_family_id": "6968045635650973636",
      "name": "技术研发",
      "description": "负责公司核心技术研发",
      "parent_job_family_id": "",
      "status": true,
      "i18n_name": [
        {
          "locale": "zh_cn",
          "value": "技术研发"
        }
      ]
    }
  }
}

错误响应：
{
  "code": 404,
  "msg": "序列不存在"
}
```

**说明**：
- 获取单个序列的完整信息
- 包含多语言配置和层级关系

**代码示例**：
```csharp
// 获取指定序列详情
var result = await _feishuApi.GetJobFamilyByIdAsync(
    tenantAccessToken,
    "6968045635650973636");

if (result.Success)
{
    var jobFamily = result.Data.JobFamily;
    Console.WriteLine($"序列名称：{jobFamily.Name}");
    Console.WriteLine($"序列ID：{jobFamily.JobFamilyId}");
    Console.WriteLine($"描述：{jobFamily.Description ?? "无描述"}");
    Console.WriteLine($"状态：{(jobFamily.Status ? "启用" : "停用")}");
    
    if (!string.IsNullOrEmpty(jobFamily.ParentJobFamilyId))
    {
        Console.WriteLine($"父序列ID：{jobFamily.ParentJobFamilyId}");
        
        // 可以进一步查询父序列信息
        var parentResult = await _feishuApi.GetJobFamilyByIdAsync(
            tenantAccessToken,
            jobFamily.ParentJobFamilyId);
        
        if (parentResult.Success)
        {
            Console.WriteLine($"父序列名称：{parentResult.Data.JobFamily.Name}");
        }
    }
    
    // 显示多语言配置
    if (jobFamily.I18nName?.Any() == true)
    {
        Console.WriteLine("多语言名称：");
        foreach (var i18n in jobFamily.I18nName)
        {
            Console.WriteLine($"  {i18n.Locale}: {i18n.Value}");
        }
    }
}
else
{
    Console.WriteLine($"获取失败：{result.Message}");
}
```

---

## GetJobFamilesListAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- name (必填) - 序列名称，用于搜索过滤
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
        "job_family_id": "6968045635650973636",
        "name": "技术研发",
        "description": "负责公司核心技术研发",
        "parent_job_family_id": "",
        "status": true
      },
      {
        "job_family_id": "6968045635650973637",
        "name": "产品设计",
        "description": "负责产品规划和设计",
        "parent_job_family_id": "",
        "status": true
      }
    ],
    "page_token": "next_page_token"
  }
}
```

**说明**：
- name参数用于搜索过滤，支持模糊匹配
- 支持分页获取大量数据
- 返回按创建时间排序的序列列表

**代码示例**：
```csharp
// 获取序列列表（支持搜索和分页）
var pageSize = 20;
var pageToken = "";
var searchKeyword = "技术"; // 搜索包含"技术"的序列
var allJobFamilies = new List<JobFamilyInfo>();

do
{
    var result = await _feishuApi.GetJobFamilesListAsync(
        tenantAccessToken,
        name: searchKeyword,
        page_size: pageSize,
        page_token: string.IsNullOrEmpty(pageToken) ? null : pageToken);

    if (result.Success)
    {
        allJobFamilies.AddRange(result.Data.Items);
        
        Console.WriteLine($"第 {allJobFamilies.Count / pageSize + 1} 页结果：");
        
        // 构建层级关系显示
        var topLevelFamilies = result.Data.Items.Where(f => string.IsNullOrEmpty(f.ParentJobFamilyId));
        var childFamilies = result.Data.Items.Where(f => !string.IsNullOrEmpty(f.ParentJobFamilyId));
        
        Console.WriteLine("\n顶级序列：");
        foreach (var family in topLevelFamilies)
        {
            Console.WriteLine($"  {family.Name} ({family.JobFamilyId}) - {(family.Status ? "启用" : "停用")}");
        }
        
        Console.WriteLine("\n子序列：");
        foreach (var family in childFamilies)
        {
            Console.WriteLine($"  {family.Name} (父序列: {family.ParentJobFamilyId}) - {(family.Status ? "启用" : "停用")}");
        }

        pageToken = result.Data.PageToken;
    }
    else
    {
        Console.WriteLine($"获取失败：{result.Message}");
        break;
    }
} while (!string.IsNullOrEmpty(pageToken));

Console.WriteLine($"\n总共找到 {allJobFamilies.Count} 个包含'{searchKeyword}'的序列");
```

---

## DeleteJobFamilyByIdAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- job_family_id (必填) - 序列ID

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
  "msg": "序列存在子序列，无法删除"
}

错误响应：
{
  "code": 404,
  "msg": "序列不存在"
}
```

**说明**：
- 仅支持删除没有子序列的序列
- 删除前建议先停用序列
- 删除操作不可恢复

**代码示例**：
```csharp
// 安全删除序列（先检查是否存在子序列）
var jobFamilyIdToDelete = "6968045635650973636";

// 先获取序列详情
var getResult = await _feishuApi.GetJobFamilyByIdAsync(
    tenantAccessToken,
    jobFamilyIdToDelete);

if (!getResult.Success)
{
    Console.WriteLine("序列不存在");
    return;
}

var jobFamily = getResult.Data.JobFamily;
Console.WriteLine($"准备删除序列：{jobFamily.Name}");

// 检查是否有子序列
var listResult = await _feishuApi.GetJobFamilesListAsync(
    tenantAccessToken,
    name: "", // 搜索所有序列
    page_size: 100);

if (listResult.Success)
{
    var childFamilies = listResult.Data.Items
        .Where(f => f.ParentJobFamilyId == jobFamilyIdToDelete)
        .ToList();

    if (childFamilies.Any())
    {
        Console.WriteLine("无法删除：该序列存在以下子序列：");
        foreach (var child in childFamilies)
        {
            Console.WriteLine($"  - {child.Name} ({child.JobFamilyId})");
        }
        Console.WriteLine("请先删除或重新分配所有子序列");
        return;
    }
}

// 确认删除
Console.WriteLine("确认要删除这个序列吗？删除后不可恢复。");
// 这里可以添加用户确认逻辑

var deleteResult = await _feishuApi.DeleteJobFamilyByIdAsync(
    tenantAccessToken,
    jobFamilyIdToDelete);

if (deleteResult.Success)
{
    Console.WriteLine("序列删除成功");
}
else
{
    Console.WriteLine($"删除失败：{deleteResult.Message}");
}
```

---

## 高级应用场景示例

### 批量创建序列层级结构
```csharp
// 创建完整的序列层级结构
async Task CreateJobFamilyHierarchy()
{
    // 1. 创建顶级序列
    var techFamilyRequest = new JobFamilyCreateUpdateRequest
    {
        Name = "技术类",
        Description = "技术相关岗位序列",
        Status = true
    };
    
    var techResult = await _feishuApi.CreateJobFamilyAsync(tenantAccessToken, techFamilyRequest);
    if (!techResult.Success) return;
    
    var techFamilyId = techResult.Data.JobFamily.JobFamilyId;
    
    // 2. 创建子序列
    var subFamilies = new[]
    {
        new { Name = "研发", Description = "产品研发岗位" },
        new { Name = "测试", Description = "质量测试岗位" },
        new { Name = "运维", Description = "系统运维岗位" }
    };
    
    foreach (var subFamily in subFamilies)
    {
        var subRequest = new JobFamilyCreateUpdateRequest
        {
            Name = subFamily.Name,
            Description = subFamily.Description,
            ParentJobFamilyId = techFamilyId,
            Status = true
        };
        
        await _feishuApi.CreateJobFamilyAsync(tenantAccessToken, subRequest);
    }
    
    Console.WriteLine("序列层级结构创建完成");
}
```

---

## 版本记录

| 版本 | 日期 | 说明 | 作者 |
|-----|-----|-----|-----|
| v1.0.0 | 2025-11-20 | 初始版本，包含所有职位序列管理API | Mud Studio |
