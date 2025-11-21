# 职级管理

## 功能描述
职级是用户属性之一，可以根据企业组织架构的需要，添加职级，例如 P1、P2、P3、P4。后续在创建用户或者更新用户时，可以为用户设置指定的职级属性。该API提供完整的职级管理功能，支持创建、更新、查询和删除职级，满足企业职级体系管理的需求。

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP方法 |
|---------|---------|---------|---------|
| CreateJobLevelAsync | 创建一个职级 | tenant_access_token | POST |
| UpdateJobLevelAsync | 更新指定职级的信息 | tenant_access_token | PUT |
| GetJobLevelByIdAsync | 获取指定职级的信息 | tenant_access_token | GET |
| GetJobLevelListAsync | 获取当前租户下的职级信息列表 | tenant_access_token | GET |
| DeleteJobLevelByIdAsync | 删除指定的职级 | tenant_access_token | DELETE |

## CreateJobLevelAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- levelCreateRequest (必填) - 创建职级请求体

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "job_level_id": "6968045635650973636",
    "name": "高级专家",
    "description": "公司内部中高级职称，有一定专业技术能力的人员",
    "order": 200,
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
        "value": "公司内部中高级职称"
      },
      {
        "locale": "en_us",
        "value": "Senior professional title within the company"
      }
    ]
  }
}

错误响应：
{
  "code": 400,
  "msg": "职级名称已存在"
}
```

**说明**：
- 职级名称在租户内必须唯一
- description字符长度上限为5,000
- order值越小，排序越靠前
- status默认为true（启用状态）
- 支持多语言配置

**代码示例**：
```csharp
// 创建新的职级
var createRequest = new JobLevelCreateUpdateRequest
{
    Name = "高级专家",
    Description = "公司内部中高级职称，有一定专业技术能力的人员。负责核心技术攻关和团队指导工作。",
    Order = 200,
    Status = true,
    I18nName = new List<I18nContent>
    {
        new I18nContent
        {
            Locale = "zh_cn",
            Value = "高级专家"
        },
        new I18nContent
        {
            Locale = "en_us",
            Value = "Senior Expert"
        }
    },
    I18nDescription = new List<I18nContent>
    {
        new I18nContent
        {
            Locale = "zh_cn",
            Value = "公司内部中高级职称，专业技术能力突出"
        },
        new I18nContent
        {
            Locale = "en_us",
            Value = "Senior professional title with outstanding technical skills"
        }
    }
};

var result = await _feishuApi.CreateJobLevelAsync(
    tenantAccessToken,
    createRequest);

if (result.Success)
{
    var jobLevel = result.Data;
    Console.WriteLine($"职级创建成功：{jobLevel.Name} (ID: {jobLevel.JobLevelId})");
    Console.WriteLine($"排序：{jobLevel.Order}");
    Console.WriteLine($"状态：{(jobLevel.Status ? "启用" : "停用")");
    Console.WriteLine($"描述：{jobLevel.Description}");
    
    // 显示多语言配置
    if (jobLevel.I18nName?.Any() == true)
    {
        Console.WriteLine("多语言名称：");
        foreach (var i18n in jobLevel.I18nName)
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

## UpdateJobLevelAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- job_level_id (必填) - 职级ID
- levelCreateRequest (必填) - 更新职级请求体

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "job_level_id": "6968045635650973636",
    "name": "资深专家",
    "description": "公司内部高级职称，专业技术能力卓越的人员",
    "order": 200,
    "status": true
  }
}

错误响应：
{
  "code": 404,
  "msg": "职级不存在"
}
```

**说明**：
- 可以更新职级的所有字段
- 职级名称仍需保持租户内唯一
- 调整排序会影响职级在列表中的显示顺序

**代码示例**：
```csharp
// 更新职级信息
var updateRequest = new JobLevelCreateUpdateRequest
{
    Name = "资深专家",
    Description = "公司内部高级职称，专业技术能力卓越的人员。负责核心技术架构设计和重大技术决策。",
    Order = 150, // 调整排序，数值更小表示更高级别
    Status = true,
    I18nName = new List<I18nContent>
    {
        new I18nContent
        {
            Locale = "zh_cn",
            Value = "资深专家"
        },
        new I18nContent
        {
            Locale = "en_us",
            Value = "Principal Expert"
        }
    }
};

var result = await _feishuApi.UpdateJobLevelAsync(
    tenantAccessToken,
    "6968045635650973636",
    updateRequest);

if (result.Success)
{
    var updatedLevel = result.Data;
    Console.WriteLine($"职级更新成功：{updatedLevel.Name}");
    Console.WriteLine($"新排序：{updatedLevel.Order}");
    
    // 如果要停用职级
    if (!updatedLevel.Status)
    {
        Console.WriteLine("注意：该职级已被停用，现有用户不受影响但新建用户无法选择此职级");
    }
}
else
{
    Console.WriteLine($"更新失败：{result.Message}");
}
```

---

## GetJobLevelByIdAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- job_level_id (必填) - 职级ID

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "job_level_id": "6968045635650973636",
    "name": "高级专家",
    "description": "公司内部中高级职称",
    "order": 200,
    "status": true,
    "i18n_name": [
      {
        "locale": "zh_cn",
        "value": "高级专家"
      }
    ]
  }
}

错误响应：
{
  "code": 404,
  "msg": "职级不存在"
}
```

**说明**：
- 获取单个职级的完整信息
- 包含多语言配置和排序信息

**代码示例**：
```csharp
// 获取指定职级详情
var result = await _feishuApi.GetJobLevelByIdAsync(
    tenantAccessToken,
    "6968045635650973636");

if (result.Success)
{
    var jobLevel = result.Data;
    Console.WriteLine($"职级名称：{jobLevel.Name}");
    Console.WriteLine($"职级ID：{jobLevel.JobLevelId}");
    Console.WriteLine($"排序：{jobLevel.Order}");
    Console.WriteLine($"描述：{jobLevel.Description ?? "无描述"}");
    Console.WriteLine($"状态：{(jobLevel.Status ? "启用" : "停用")");
    
    // 显示多语言配置
    if (jobLevel.I18nName?.Any() == true)
    {
        Console.WriteLine("多语言名称：");
        foreach (var i18n in jobLevel.I18nName)
        {
            Console.WriteLine($"  {i18n.Locale}: {i18n.Value}");
        }
    }
    
    if (jobLevel.I18nDescription?.Any() == true)
    {
        Console.WriteLine("多语言描述：");
        foreach (var i18n in jobLevel.I18nDescription)
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

## GetJobLevelListAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- name (必填) - 职级名称，用于搜索过滤
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
        "job_level_id": "6968045635650973635",
        "name": "初级工程师",
        "description": "入门级技术职位",
        "order": 400,
        "status": true
      },
      {
        "job_level_id": "6968045635650973636",
        "name": "高级专家",
        "description": "公司内部中高级职称",
        "order": 200,
        "status": true
      }
    ],
    "page_token": "next_page_token"
  }
}
```

**说明**：
- name参数用于搜索过滤，支持模糊匹配
- 返回按order字段排序的职级列表
- 支持分页获取大量数据

**代码示例**：
```csharp
// 获取职级列表（支持搜索和分页）
var pageSize = 50;
var pageToken = "";
var searchKeyword = "专家"; // 搜索包含"专家"的职级
var allJobLevels = new List<JobLevelResult>();

do
{
    var result = await _feishuApi.GetJobLevelListAsync(
        tenantAccessToken,
        name: searchKeyword,
        page_size: pageSize,
        page_token: string.IsNullOrEmpty(pageToken) ? null : pageToken);

    if (result.Success)
    {
        allJobLevels.AddRange(result.Data.Items);
        
        Console.WriteLine($"第 {allJobLevels.Count / pageSize + 1} 页结果：");
        
        // 按排序显示职级
        var sortedLevels = result.Data.Items.OrderBy(l => l.Order);
        
        Console.WriteLine("\n职级列表（按排序）：");
        foreach (var level in sortedLevels)
        {
            Console.WriteLine($"  {level.Name} (排序: {level.Order}) - {(level.Status ? "启用" : "停用")}");
            if (!string.IsNullOrEmpty(level.Description))
            {
                Console.WriteLine($"    描述：{level.Description}");
            }
        }

        pageToken = result.Data.PageToken;
    }
    else
    {
        Console.WriteLine($"获取失败：{result.Message}");
        break;
    }
} while (!string.IsNullOrEmpty(pageToken));

Console.WriteLine($"\n总共找到 {allJobLevels.Count} 个包含'{searchKeyword}'的职级");
```

---

## DeleteJobLevelByIdAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- job_level_id (必填) - 职级ID

**响应**：
```json
成功响应：
{
  "code": 0,
  "msg": "success"
}

错误响应：
{
  "code": 404,
  "msg": "职级不存在"
}

错误响应：
{
  "code": 400,
  "msg": "职级正在被使用，无法删除"
}
```

**说明**：
- 删除前请确认该职级没有被用户使用
- 删除操作不可恢复
- 建议删除前先停用职级

**代码示例**：
```csharp
// 安全删除职级（先检查使用情况）
var jobLevelIdToDelete = "6968045635650973636";

// 先获取职级详情
var getResult = await _feishuApi.GetJobLevelByIdAsync(
    tenantAccessToken,
    jobLevelIdToDelete);

if (!getResult.Success)
{
    Console.WriteLine("职级不存在");
    return;
}

var jobLevel = getResult.Data;
Console.WriteLine($"准备删除职级：{jobLevel.Name}");

// 检查是否有用户使用该职级（这里需要调用用户API检查）
// 在实际应用中，应该先检查是否有用户使用该职级
Console.WriteLine("注意：删除前请确认没有用户使用该职级");

// 确认删除
Console.WriteLine("确认要删除这个职级吗？删除后不可恢复。");
// 这里可以添加用户确认逻辑

var deleteResult = await _feishuApi.DeleteJobLevelByIdAsync(
    tenantAccessToken,
    jobLevelIdToDelete);

if (deleteResult.Success)
{
    Console.WriteLine("职级删除成功");
}
else
{
    Console.WriteLine($"删除失败：{deleteResult.Message}");
    
    if (deleteResult.Message.Contains("正在被使用"))
    {
        Console.WriteLine("建议：先停用该职级，重新分配用户职级后再删除");
    }
}
```

---

## 高级应用场景示例

### 批量创建完整职级体系
```csharp
// 创建完整的职级体系
async Task CreateJobLevelHierarchy()
{
    // 定义职级体系
    var jobLevels = new[]
    {
        new { Name = "实习生", Description = "实习岗位", Order = 500 },
        new { Name = "初级工程师", Description = "入门级技术职位", Order = 400 },
        new { Name = "工程师", Description = "中级技术职位", Order = 300 },
        new { Name = "高级工程师", Description = "高级技术职位", Order = 250 },
        new { Name = "技术专家", Description = "专业技术专家", Order = 200 },
        new { Name = "资深专家", Description = "高级技术专家", Order = 150 },
        new { Name = "首席专家", Description = "最高技术专家", Order = 100 }
    };
    
    var createdLevels = new List<JobLevelResult>();
    
    foreach (var level in jobLevels)
    {
        var createRequest = new JobLevelCreateUpdateRequest
        {
            Name = level.Name,
            Description = level.Description,
            Order = level.Order,
            Status = true,
            I18nName = new List<I18nContent>
            {
                new I18nContent { Locale = "zh_cn", Value = level.Name },
                new I18nContent { Locale = "en_us", Value = $"{level.Name}_EN" }
            }
        };
        
        var result = await _feishuApi.CreateJobLevelAsync(tenantAccessToken, createRequest);
        if (result.Success)
        {
            createdLevels.Add(result.Data);
            Console.WriteLine($"创建职级：{level.Name} (ID: {result.Data.JobLevelId})");
        }
        else
        {
            Console.WriteLine($"创建失败 {level.Name}: {result.Message}");
        }
    }
    
    Console.WriteLine($"\n职级体系创建完成，共创建 {createdLevels.Count} 个职级");
    
    // 显示创建的职级体系
    var sortedLevels = createdLevels.OrderBy(l => l.Order);
    Console.WriteLine("\n职级体系（从高到低）：");
    foreach (var level in sortedLevels)
    {
        Console.WriteLine($"  {level.Name} (排序: {level.Order})");
    }
}
```

### 职级同步管理
```csharp
// 职级同步管理示例
async Task SyncJobLevels()
{
    // 获取现有职级
    var existingLevels = new List<JobLevelResult>();
    var pageToken = "";
    
    do
    {
        var result = await _feishuApi.GetJobLevelListAsync(
            tenantAccessToken,
            name: "",
            page_size: 100,
            page_token: string.IsNullOrEmpty(pageToken) ? null : pageToken);
        
        if (result.Success)
        {
            existingLevels.AddRange(result.Data.Items);
            pageToken = result.Data.PageToken;
        }
    } while (!string.IsNullOrEmpty(pageToken));
    
    Console.WriteLine($"当前共有 {existingLevels.Count} 个职级");
    
    // 检查并更新职级状态
    foreach (var level in existingLevels)
    {
        // 这里可以根据业务规则检查是否需要更新职级
        if (string.IsNullOrEmpty(level.Description))
        {
            var updateRequest = new JobLevelCreateUpdateRequest
            {
                Name = level.Name,
                Description = $"{level.Name}的详细描述信息",
                Order = level.Order,
                Status = level.Status
            };
            
            await _feishuApi.UpdateJobLevelAsync(tenantAccessToken, level.JobLevelId, updateRequest);
            Console.WriteLine($"更新职级描述：{level.Name}");
        }
    }
    
    Console.WriteLine("职级同步完成");
}
```

---

## 版本记录

| 版本 | 日期 | 说明 | 作者 |
|-----|-----|-----|-----|
| v1.0.0 | 2025-11-20 | 初始版本，包含所有职级管理API | Mud Studio |
