# 职能分类（FeishuTenantV1HireJobFunction）

## 接口名称

**职能分类** -（`IFeishuTenantV1HireJobFunction`）

## 功能描述

以租户身份分页获取职能分类列表（树形结构，通过 `parent_id` 表达层级），用于职位创建时的职能选择。

## 参考文档

- [获取职能分类列表](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/list-3)

## 函数列表

| 函数名称                 | 功能描述         | 限频     | 所需权限          | HTTP 方法 |
| ------------------------ | ---------------- | -------- | ----------------- | --------- |
| GetJobFunctionListAsync  | 获取职能分类列表 | 20 次/秒 | hire:job:readonly | GET       |

## 函数详细内容

### GetJobFunctionListAsync — 获取职能分类列表

`GET /open-apis/hire/v1/job_functions`

```csharp
Task<FeishuApiResult<GetJobFunctionListResult>?> GetJobFunctionListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

- `page_size`：可选，最大 50。
- 响应 `data.items` 为 `JobFunction[]`（职能分类 ID、名称、启用状态、父级职能分类 ID），附 `has_more`/`page_token`。
