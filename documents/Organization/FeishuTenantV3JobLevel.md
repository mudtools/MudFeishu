# 租户V3职级管理 - FeishuTenantV3JobLevel

## 接口名称
**租户V3职级管理 - (IFeishuTenantV3JobLevel)**

## 功能描述
本接口提供飞书职级（Job Level）的管理功能，适用于租户应用场景。支持职级的创建、更新、查询和删除等操作。

职级是用户属性之一，可以根据企业组织架构的需要，添加职级，例如 P1、P2、P3、P4。后续在创建用户或者更新用户时，可以为用户设置指定的职级属性。

## 参考文档
- [飞书官方文档 - 职级资源介绍](https://open.feishu.cn/document/contact-v3/job_level/job-level-resources-introduction)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateJobLevelAsync | 创建职级 | 租户令牌 | POST |
| UpdateJobLevelAsync | 更新职级 | 租户令牌 | PUT |
| GetJobLevelByIdAsync | 获取职级详情 | 租户令牌 | GET |
| GetJobLevelListAsync | 获取职级列表 | 租户令牌 | GET |
| DeleteJobLevelByIdAsync | 删除职级 | 租户令牌 | DELETE |

## 函数详细内容

### 创建职级

**函数名称**：创建职级

**函数签名**：
```csharp
Task<FeishuApiResult<JobLevelResult>?> CreateJobLevelAsync(
    [Body] JobLevelCreateUpdateRequest levelCreateRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| levelCreateRequest | JobLevelCreateUpdateRequest | ✅ | 创建职级请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_level": {
      "job_level_id": "xxx",
      "name": "P5",
      "description": "高级工程师",
      "order": 5,
      "is_active": true
    }
  }
}
```

**说明**：创建一个职级。职级是用户属性之一，用于标识用户的职位级别，例如 P1、P2、P3、P4。

**代码示例**：
```csharp
public class JobLevelService
{
    private readonly IFeishuTenantV3JobLevel _jobLevelClient;

    public JobLevelService(IFeishuTenantV3JobLevel jobLevelClient)
    {
        _jobLevelClient = jobLevelClient;
    }

    public async Task CreateNewJobLevelAsync()
    {
        var request = new JobLevelCreateUpdateRequest
        {
            Name = "P6",
            Description = "资深工程师",
            Order = 6,
            IsActive = true
        };

        var result = await _jobLevelClient.CreateJobLevelAsync(request);
        if (result?.Code == 0)
        {
            Console.WriteLine($"职级创建成功，ID: {result.Data?.JobLevel?.JobLevelId}");
        }
    }
}
```

---

### 更新职级

**函数名称**：更新职级

**函数签名**：
```csharp
Task<FeishuApiResult<JobLevelResult>?> UpdateJobLevelAsync(
    [Path] string job_level_id,
    [Body] JobLevelCreateUpdateRequest levelCreateRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| job_level_id | string | ✅ | 职级ID |
| levelCreateRequest | JobLevelCreateUpdateRequest | ✅ | 更新职级请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_level": {
      "job_level_id": "xxx",
      "name": "P7",
      "description": "专家",
      "is_active": true
    }
  }
}
```

**说明**：更新指定职级的信息。

---

### 获取职级详情

**函数名称**：获取职级详情

**函数签名**：
```csharp
Task<FeishuApiResult<JobLevelResult>?> GetJobLevelByIdAsync(
    [Path] string job_level_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| job_level_id | string | ✅ | 职级ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_level": {
      "job_level_id": "xxx",
      "name": "P5",
      "description": "高级工程师",
      "order": 5,
      "is_active": true,
      "i18n_name": {
        "zh_cn": "高级工程师",
        "en_us": "Senior Engineer"
      }
    }
  }
}
```

**说明**：获取指定职级的信息，包括职级名称、描述、排序、状态以及多语言等。

**代码示例**：
```csharp
public async Task GetJobLevelDetailsAsync(string jobLevelId)
{
    var result = await _jobLevelClient.GetJobLevelByIdAsync(jobLevelId);
    if (result?.Code == 0)
    {
        var level = result.Data?.JobLevel;
        Console.WriteLine($"职级: {level?.Name}, 描述: {level?.Description}");
    }
}
```

---

### 获取职级列表

**函数名称**：获取职级列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<JobLevelInfo>?> GetJobLevelListAsync(
    [Query("name")] string? name,
    [Query("page_size")] int? page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| name | string | ⚪ | 职级名称，用于筛选 |
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
        "job_level_id": "xxx",
        "name": "P5",
        "description": "高级工程师",
        "order": 5,
        "is_active": true
      },
      {
        "job_level_id": "yyy",
        "name": "P6",
        "description": "资深工程师",
        "order": 6,
        "is_active": true
      }
    ],
    "page_token": "xxx",
    "has_more": false
  }
}
```

**说明**：获取当前租户下的职级信息，包括职级名称、描述、排序、状态以及多语言等。

---

### 删除职级

**函数名称**：删除职级

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteJobLevelByIdAsync(
    [Path] string job_level_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| job_level_id | string | ✅ | 职级ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除指定的职级。
