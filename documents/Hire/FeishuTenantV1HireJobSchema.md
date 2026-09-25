# 职位模板（FeishuTenantV1HireJobSchema）

## 接口名称

**职位模板** -（`IFeishuTenantV1HireJobSchema`）

## 功能描述

以租户身份按场景（社招/校招）分页获取职位模板列表，返回模板下的模块与字段结构（含必填/可见/启用状态与选项配置）。

## 参考文档

- [获取职位模板列表](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/list)

## 函数列表

| 函数名称               | 功能描述         | 限频     | 所需权限          | HTTP 方法 |
| ---------------------- | ---------------- | -------- | ----------------- | --------- |
| GetJobSchemaListAsync  | 获取职位模板列表 | 10 次/秒 | hire:job:readonly | GET       |

## 函数详细内容

### GetJobSchemaListAsync — 获取职位模板列表

`GET /open-apis/hire/v1/job_schemas`

```csharp
Task<FeishuApiResult<GetJobSchemaListResult>?> GetJobSchemaListAsync(
    [Query("scenario")] int? scenario = null,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

- `scenario`：场景，1 社招 / 2 校招。
- `page_size`：可选，最大 100。
- 响应 `data.items` 为 `JobSchema[]`（模板 ID、名称、类型、`object_list` 模块列表；模块含 `children_list` 字段列表与 `setting` 配置），附 `has_more`/`page_token`。
