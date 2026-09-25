# 地址（FeishuTenantV1HireLocation）

## 接口名称

**地址** -（`IFeishuTenantV1HireLocation`）

## 功能描述

以租户身份查询飞书招聘的地址主数据：按地点类型（国家/省/市/区）批量查询地址码，或按用途（职位地址/面试地点）分页查询已维护的地址列表。

## 参考文档

- [查询地址](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/location/query)
- [获取地址列表](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/location/list)

## 函数列表

| 函数名称           | 功能描述             | 限频         | 所需权限             | HTTP 方法 |
| ------------------ | -------------------- | ------------ | -------------------- | --------- |
| QueryLocationAsync | 查询地址码           | 5 次/秒      | hire:location:readonly | POST    |
| GetLocationListAsync | 获取地址列表（按用途） | Special Rate Limit | hire:location:readonly | GET   |

## 函数详细内容

### QueryLocationAsync — 查询地址码

`POST /open-apis/hire/v1/locations/query`

```csharp
Task<FeishuApiResult<QueryLocationResult>?> QueryLocationAsync(
    [Body] QueryLocationRequest request,
    [Query("page_size")] int page_size,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

- `page_size`：**必填**，1–100。
- 请求体 `QueryLocationRequest`：`location_type`（**必填**：1 国家 / 2 省 / 3 市 / 4 区）、`code_list`（地点码列表，最多 100 条；不传则按地点类型分页查询全量）。
- 响应 `data.items` 为 `LocationDto[]`（按 `location_type` 返回 `country`/`state`/`city`/`district` 之一，含各级 code 与 `LocationNameInfo` 中英文/拼音名），附 `has_more`/`page_token`。

### GetLocationListAsync — 获取地址列表

`GET /open-apis/hire/v1/locations`

```csharp
Task<FeishuApiResult<GetLocationListResult>?> GetLocationListAsync(
    [Query("usage")] string usage,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

- `usage`：**必填**，地点用途（`position_location` 职位地址 / `interview_location` 面试地点）。
- `page_size`：可选，最大 100。
- 响应 `data.items` 为 `Location[]`（地址 ID、名称、区/市/省/国家 `CodeNameObject`、启用状态）。
