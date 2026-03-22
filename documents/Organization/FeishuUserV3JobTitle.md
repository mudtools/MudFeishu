# 用户V3职务管理 - FeishuUserV3JobTitle

## 接口名称
**用户V3职务管理 - (IFeishuUserV3JobTitle)**

## 功能描述
本接口提供飞书职务（Job Title）的查询功能，适用于用户应用场景。支持职务列表查询和单个职务详情查询。使用用户令牌访问，适合代表用户查询职务信息的场景。

职务是用户属性之一，通过职务API仅支持查询职务信息。

## 参考文档
- [飞书官方文档 - 职务资源介绍](https://open.feishu.cn/document/contact-v3/job_title/job-title-resources-introduction)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| GetJobTitlesListAsync | 获取职务列表 | 用户令牌 | GET |
| GetJobTitleByIdAsync | 获取职务详情 | 用户令牌 | GET |

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

**认证**：用户令牌

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
      }
    ],
    "page_token": "xxx",
    "has_more": false
  }
}
```

**说明**：获取当前租户下的职务信息，包括职务的ID、名称、多语言名称以及启用状态。

**代码示例**：
```csharp
public class UserJobTitleService
{
    private readonly IFeishuUserV3JobTitle _jobTitleClient;

    public UserJobTitleService(IFeishuUserV3JobTitle jobTitleClient)
    {
        _jobTitleClient = jobTitleClient;
    }

    public async Task GetJobTitlesAsUserAsync()
    {
        var result = await _jobTitleClient.GetJobTitlesListAsync(page_size: 50);
        if (result?.Code == 0)
        {
            foreach (var title in result.Data?.Items ?? [])
            {
                Console.WriteLine($"职务: {title.Name}");
            }
        }
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

**认证**：用户令牌

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
