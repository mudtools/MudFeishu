# 职位类别（FeishuTenantV1HireJobType）

## 接口名称

**职位类别** -（`IFeishuTenantV1HireJobType`）

## 功能描述

以租户身份分页获取职位类别列表（树形结构，通过 `parent_id` 表达层级），用于职位创建时的类别选择。

## 参考文档

- [获取职位类别列表](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/list-4)

## 函数列表

| 函数名称            | 功能描述         | 限频     | 所需权限          | HTTP 方法 |
| ------------------- | ---------------- | -------- | ----------------- | --------- |
| GetJobTypeListAsync | 获取职位类别列表 | 20 次/秒 | hire:job:readonly | GET       |

## 函数详细内容

### GetJobTypeListAsync — 获取职位类别列表

`GET /open-apis/hire/v1/job_types`

```csharp
Task<FeishuApiResult<GetJobTypeListResult>?> GetJobTypeListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

- `page_size`：可选，默认 10。
- 响应 `data.items` 为 `JobTypeInfo[]`（职位类别 ID、名称、父级职位类别 ID），附 `has_more`/`page_token`。
