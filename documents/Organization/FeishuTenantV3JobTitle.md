# 租户V3职务管理 - FeishuTenantV3JobTitle

## 接口名称
**租户V3职务管理 - (IFeishuTenantV3JobTitle)**

## 功能描述
本接口提供飞书职务（Job Title）的查询功能，适用于租户应用场景。支持职务列表查询和单个职务详情查询。

职务是用户属性之一，通过职务API仅支持查询职务信息。职务定义了用户在组织中的职位角色，如"产品经理"、"软件工程师"等。

## 参考文档
- [飞书官方文档 - 职务资源介绍](https://open.feishu.cn/document/contact-v3/job_title/job-title-resources-introduction)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| GetJobTitlesListAsync | 获取职务列表 | 租户令牌 | GET |
| GetJobTitleByIdAsync | 获取职务详情 | 租户令牌 | GET |

## 函数详细内容

### 获取职务列表

**函数名称**：获取职务列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<JobTitle>?> GetJobTitlesListAsync(
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
        "job_title_id": "xxx",
        "name": "产品经理",
        "is_active": true,
        "i18n_name": {
          "zh_cn": "产品经理",
          "en_us": "Product Manager"
        }
      },
      {
        "job_title_id": "yyy",
        "name": "软件工程师",
        "is_active": true,
        "i18n_name": {
          "zh_cn": "软件工程师",
          "en_us": "Software Engineer"
        }
      }
    ],
    "page_token": "xxx",
    "has_more": true
  }
}
```

**说明**：获取当前租户下的职务信息，包括职务的ID、名称、多语言名称以及启用状态。

**代码示例**：
```csharp
public class JobTitleService
{
    private readonly IFeishuTenantV3JobTitle _jobTitleClient;

    public JobTitleService(IFeishuTenantV3JobTitle jobTitleClient)
    {
        _jobTitleClient = jobTitleClient;
    }

    public async Task ListAllJobTitlesAsync()
    {
        string? pageToken = null;
        do
        {
            var result = await _jobTitleClient.GetJobTitlesListAsync(
                page_size: 50, 
                page_token: pageToken);
            
            if (result?.Code == 0)
            {
                foreach (var title in result.Data?.Items ?? [])
                {
                    Console.WriteLine($"职务: {title.Name}, 状态: {(title.IsActive ? "启用" : "禁用")}");
                }
                pageToken = result.Data?.PageToken;
            }
        } while (!string.IsNullOrEmpty(pageToken));
    }
}
```

---

### 获取职务详情

**函数名称**：获取职务详情

**函数签名**：
```csharp
Task<FeishuApiResult<JobTitleResult>?> GetJobTitleByIdAsync(
    [Path] string job_title_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| job_title_id | string | ✅ | 职务ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_title": {
      "job_title_id": "xxx",
      "name": "高级产品经理",
      "is_active": true,
      "i18n_name": {
        "zh_cn": "高级产品经理",
        "en_us": "Senior Product Manager"
      }
    }
  }
}
```

**说明**：获取指定职务的信息，包括职务的ID、名称、多语言名称以及启用状态。

**代码示例**：
```csharp
public async Task GetJobTitleDetailsAsync(string jobTitleId)
{
    var result = await _jobTitleClient.GetJobTitleByIdAsync(jobTitleId);
    if (result?.Code == 0)
    {
        var title = result.Data?.JobTitle;
        Console.WriteLine($"职务名称: {title?.Name}");
        Console.WriteLine($"英文名称: {title?.I18nName?.EnUs}");
    }
}
```
