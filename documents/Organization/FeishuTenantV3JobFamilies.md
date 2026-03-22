# 租户V3序列管理 - FeishuTenantV3JobFamilies

## 接口名称
**租户V3序列管理 - (IFeishuTenantV3JobFamilies)**

## 功能描述
本接口提供飞书序列（Job Family）的管理功能，适用于租户应用场景。支持序列的创建、更新、查询和删除等操作。

序列是用户属性之一，用来为不同的用户定义不同的工作类型，例如产品、研发、测试、运营。可以根据企业实际需要添加序列，后续在创建或更新用户时，为用户设置相匹配的序列。

## 参考文档
- [飞书官方文档 - 序列资源介绍](https://open.feishu.cn/document/contact-v3/job_family/job-family-resource-introduction)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateJobFamilyAsync | 创建序列 | 租户令牌 | POST |
| UpdateJobFamilyAsync | 更新序列 | 租户令牌 | PUT |
| GetJobFamilyByIdAsync | 获取序列详情 | 租户令牌 | GET |
| GetJobFamilesListAsync | 获取序列列表 | 租户令牌 | GET |
| DeleteJobFamilyByIdAsync | 删除序列 | 租户令牌 | DELETE |

## 函数详细内容

### 创建序列

**函数名称**：创建序列

**函数签名**：
```csharp
Task<FeishuApiResult<JobFamilyResult>?> CreateJobFamilyAsync(
    [Body] JobFamilyCreateUpdateRequest familyCreateRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| familyCreateRequest | JobFamilyCreateUpdateRequest | ✅ | 职位序列创建请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_family": {
      "job_family_id": "xxx",
      "name": "研发",
      "description": "技术研发序列",
      "is_active": true
    }
  }
}
```

**说明**：创建一个序列。序列是用户属性之一，用来定义用户的工作类型，例如产品、研发、运营等。

**代码示例**：
```csharp
public class JobFamilyService
{
    private readonly IFeishuTenantV3JobFamilies _jobFamilyClient;

    public JobFamilyService(IFeishuTenantV3JobFamilies jobFamilyClient)
    {
        _jobFamilyClient = jobFamilyClient;
    }

    public async Task CreateNewJobFamilyAsync()
    {
        var request = new JobFamilyCreateUpdateRequest
        {
            Name = "产品研发",
            Description = "负责产品设计与研发",
            IsActive = true
        };

        var result = await _jobFamilyClient.CreateJobFamilyAsync(request);
        if (result?.Code == 0)
        {
            Console.WriteLine($"序列创建成功，ID: {result.Data?.JobFamily?.JobFamilyId}");
        }
    }
}
```

---

### 更新序列

**函数名称**：更新序列

**函数签名**：
```csharp
Task<FeishuApiResult<JobFamilyResult>?> UpdateJobFamilyAsync(
    [Path] string job_family_id,
    [Body] JobFamilyCreateUpdateRequest familyCreateRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| job_family_id | string | ✅ | 序列ID |
| familyCreateRequest | JobFamilyCreateUpdateRequest | ✅ | 职位序列更新请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_family": {
      "job_family_id": "xxx",
      "name": "新名称",
      "is_active": true
    }
  }
}
```

**说明**：更新指定序列的信息。

---

### 获取序列详情

**函数名称**：获取序列详情

**函数签名**：
```csharp
Task<FeishuApiResult<JobFamilyResult>?> GetJobFamilyByIdAsync(
    [Path] string job_family_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| job_family_id | string | ✅ | 序列ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_family": {
      "job_family_id": "xxx",
      "name": "研发",
      "description": "技术研发序列",
      "is_active": true,
      "i18n_name": {
        "zh_cn": "研发",
        "en_us": "R&D"
      }
    }
  }
}
```

**说明**：获取指定序列的信息，包括序列的名称、描述、启用状态以及ID等。

**代码示例**：
```csharp
public async Task GetJobFamilyDetailsAsync(string jobFamilyId)
{
    var result = await _jobFamilyClient.GetJobFamilyByIdAsync(jobFamilyId);
    if (result?.Code == 0)
    {
        var family = result.Data?.JobFamily;
        Console.WriteLine($"序列: {family?.Name}, 状态: {(family?.IsActive == true ? "启用" : "禁用")}");
    }
}
```

---

### 获取序列列表

**函数名称**：获取序列列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<JobFamilyInfo>?> GetJobFamilesListAsync(
    [Query("name")] string? name,
    [Query("page_size")] int? page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| name | string | ⚪ | 序列名称，用于筛选 |
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
        "job_family_id": "xxx",
        "name": "研发",
        "description": "技术研发",
        "is_active": true
      }
    ],
    "page_token": "xxx",
    "has_more": false
  }
}
```

**说明**：获取当前租户下的序列信息，包含序列的名称、描述、启用状态以及ID等。

---

### 删除序列

**函数名称**：删除序列

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteJobFamilyByIdAsync(
    [Path] string job_family_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| job_family_id | string | ✅ | 序列ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：
- 仅支持删除没有子序列的序列
- 如果序列内存在子序列，则不能直接删除
