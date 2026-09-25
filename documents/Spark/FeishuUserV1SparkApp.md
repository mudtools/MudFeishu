# 飞书妙搭应用管理 - 用户令牌（FeishuUserV1SparkApp）

## 接口名称

**飞书妙搭应用管理（用户令牌）** -（`IFeishuUserV1SparkApp`）

## 功能描述

提供以用户身份管理飞书妙搭（Spark）应用的能力，包括创建与更新应用、上传应用图标、上传 HTML 代码并发布、查询与更新应用可用范围；同时继承双令牌只读端点（批量获取应用、AI 额度消耗、运营数据总览与趋势），可统一以用户身份调用。

## 参考文档

- [批量获取妙搭应用](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/list)
- [创建妙搭应用](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/create)
- [更新妙搭应用信息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/patch)
- [上传妙搭应用图标](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/icon)
- [上传 HTML 代码并发布](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/upload_html_code_and_release)
- [获取妙搭应用可用范围](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/get_app_visibility)
- [更新妙搭应用可用范围](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/update_app_visibility)
- [获取妙搭应用消耗 AI 额度](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/open_api_credit_usage)
- [获取妙搭应用运营数据总览](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/open_api_analytics_overview)
- [获取妙搭应用运营数据趋势](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/query_analytics_data)

## 函数列表

| 函数名称                        | 功能描述             | 认证方式 | HTTP 方法 |
| ------------------------------- | -------------------- | -------- | --------- |
| CreateAppAsync                  | 创建妙搭应用         | 用户令牌 | POST      |
| PatchAppAsync                   | 更新妙搭应用信息     | 用户令牌 | PATCH     |
| UploadAppIconAsync              | 上传妙搭应用图标     | 用户令牌 | POST      |
| UploadHtmlCodeAndReleaseAsync   | 上传 HTML 代码并发布 | 用户令牌 | POST      |
| GetAppVisibilityAsync           | 获取妙搭应用可用范围 | 用户令牌 | GET       |
| UpdateAppVisibilityAsync        | 更新妙搭应用可用范围 | 用户令牌 | PUT       |
| GetAppListAsync                 | 批量获取妙搭应用     | 用户令牌 | GET       |
| GetAppCreditUsageAsync          | 获取应用消耗 AI 额度 | 用户令牌 | GET       |
| GetAppAnalyticsOverviewAsync    | 获取运营数据总览     | 用户令牌 | GET       |
| QueryAppAnalyticsDataAsync      | 获取运营数据趋势     | 用户令牌 | POST      |

> 后 4 个只读端点继承自 `IFeishuV1SparkApp`（双令牌基接口），也可通过 `IFeishuTenantV1SparkApp` 以租户令牌调用。

## 函数详细内容

### 创建妙搭应用

创建一个新的妙搭应用，返回应用详细信息。

**函数签名**：

```csharp
Task<FeishuApiResult<AppResult>?> CreateAppAsync(
    [Body] CreateAppRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名    | 类型                | 必填 | 说明                                                             |
| --------- | ------------------- | ---- | ---------------------------------------------------------------- |
| `request` | `CreateAppRequest`  | ✅   | 创建请求体：`name` 必填；可选 `app_type`、`description`、`icon_url` |

**响应**：`data.app` 为应用详细信息（`app_id`、`app_type`、`name`、`description`、`icon_url` 等）。

### 更新妙搭应用信息

更新应用名称、描述或图标地址，未传字段保持不变。

**函数签名**：

```csharp
Task<FeishuApiResult<AppResult>?> PatchAppAsync(
    [Path] string app_id,
    [Body] PatchAppRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名     | 类型               | 必填 | 说明                                                     |
| ---------- | ------------------ | ---- | -------------------------------------------------------- |
| `app_id`   | `string`           | ✅   | 妙搭应用唯一标识，示例值：`app_4k6af8utt2s0n`             |
| `request`  | `PatchAppRequest`  | ✅   | 更新请求体：`name`、`description`、`icon_url` 均可选      |

**响应**：`data.app` 为更新后的应用详细信息。

### 上传妙搭应用图标

上传应用图标文件（multipart/form-data），返回图标访问 URL，可用于创建或更新应用的 `icon_url`。

**函数签名**：

```csharp
Task<FeishuApiResult<UploadAppIconResult>?> UploadAppIconAsync(
    [FormContent] UploadAppIconRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名    | 类型                   | 必填 | 说明                                                     |
| --------- | ---------------------- | ---- | -------------------------------------------------------- |
| `request` | `UploadAppIconRequest` | ✅   | `file`：图标文件本地路径，建议 PNG/JPG、128×128 像素      |

**响应**：`data.icon_url` 为上传成功后的图标访问 URL。

### 上传 HTML 代码并发布

上传 tar 格式的 HTML 代码文件并直接发布应用，返回发布成功后的在线访问地址。

**函数签名**：

```csharp
Task<FeishuApiResult<UploadHtmlCodeResult>?> UploadHtmlCodeAndReleaseAsync(
    [Path] string app_id,
    [FormContent] UploadHtmlCodeRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名    | 类型                    | 必填 | 说明                                                     |
| --------- | ----------------------- | ---- | -------------------------------------------------------- |
| `app_id`  | `string`                | ✅   | 妙搭应用唯一标识，示例值：`app_4k6af8utt2s0n`             |
| `request` | `UploadHtmlCodeRequest` | ✅   | `file`：tar 格式的 HTML 文件本地路径                      |

**响应**：`data.online_url` 为发布成功后的在线访问地址。

### 获取妙搭应用可用范围

查询应用的可见范围类型（All/Tenant/Range）、授权用户/部门/群聊列表与申请访问配置。

**函数签名**：

```csharp
Task<FeishuApiResult<GetAppVisibilityResult>?> GetAppVisibilityAsync(
    [Path] string app_id,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名         | 类型     | 必填 | 说明                                                                       |
| -------------- | -------- | ---- | -------------------------------------------------------------------------- |
| `app_id`       | `string` | ✅   | 妙搭应用唯一标识，示例值：`app_4k6af8utt2s0n`                               |
| `user_id_type` | `string` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 需相应字段权限 |

**响应**：`data` 含可见范围类型、授权对象列表与申请访问配置。

### 更新妙搭应用可用范围

设置应用的可见范围类型（Public/Tenant/Range）及授权对象、申请访问配置与登录要求。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UpdateAppVisibilityAsync(
    [Path] string app_id,
    [Body] UpdateAppVisibilityRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名         | 类型                      | 必填 | 说明                                                                       |
| -------------- | ------------------------- | ---- | -------------------------------------------------------------------------- |
| `app_id`       | `string`                  | ✅   | 妙搭应用唯一标识，示例值：`app_4k6af8utt2s0n`                               |
| `request`      | `UpdateAppVisibilityRequest` | ✅  | `scope` 必填：Public/Tenant/Range；`users`、`departments`、`chats` 仅 Scope=Range 时生效；可选 `apply_config`、`require_login` |
| `user_id_type` | `string`                  | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 需相应字段权限 |

**响应**：更新成功时 `data` 为空对象（`FeishuNullDataApiResult`）。

### 批量获取妙搭应用

查询当前令牌有权限查看的妙搭应用列表，支持按类型、关键词、归属范围筛选与分页。查询参数采用查询对象模式（`GetAppListQuery`，见 AGENTS.md API-2）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetAppListResult>?> GetAppListAsync(
    [Query] GetAppListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（双令牌基接口）

**参数**：

| 参数名   | 类型                | 必填 | 说明                                                     |
| -------- | ------------------- | ---- | -------------------------------------------------------- |
| `query`  | `GetAppListQuery?`  | ⚪   | 分页（`page_size`/`page_token`）、应用类型、关键词与归属范围查询参数，整体展开为查询参数 |

**响应**：`data.items` 为应用详细信息数组，含分页字段。

### 获取妙搭应用消耗 AI 额度

查询指定应用在时间区间内按天的 AI 额度消耗数据点与汇总。

**函数签名**：

```csharp
Task<FeishuApiResult<GetCreditUsageResult>?> GetAppCreditUsageAsync(
    [Path] string app_id,
    [Query("start_time")] string start_time,
    [Query("end_time")] string end_time,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（双令牌基接口）

**参数**：

| 参数名       | 类型     | 必填 | 说明                                                                                   |
| ------------ | -------- | ---- | -------------------------------------------------------------------------------------- |
| `app_id`     | `string` | ✅   | 妙搭应用 ID，示例值：`app_4jbp6bx8fwjgm`                                                |
| `start_time` | `string` | ✅   | 时间范围起始时间戳，单位：秒（Unix 时间戳），示例值：`1717286400`                        |
| `end_time`   | `string` | ✅   | 时间范围结束时间戳，单位：秒（Unix 时间戳），须满足 `end_time >= start_time`             |

**响应**：`data` 含总额度消耗、按天数据点列表（`CreditUsagePoint`）与企业/个人额度汇总。

### 获取妙搭应用运营数据总览

查询指定应用在时间区间内的活跃用户、新增用户与页面访问数总览，含上一等长区间环比。

**函数签名**：

```csharp
Task<FeishuApiResult<GetAnalyticsOverviewResult>?> GetAppAnalyticsOverviewAsync(
    [Path] string app_id,
    [Query("start_time")] string start_time,
    [Query("end_time")] string end_time,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（双令牌基接口）

**参数**：

| 参数名       | 类型     | 必填 | 说明                                                                                   |
| ------------ | -------- | ---- | -------------------------------------------------------------------------------------- |
| `app_id`     | `string` | ✅   | 妙搭应用 ID，示例值：`app_4jbp6bx8fwjgm`                                                |
| `start_time` | `string` | ✅   | 运营分析区间起始时间戳，单位：秒（Unix 时间戳）                                         |
| `end_time`   | `string` | ✅   | 运营分析区间结束时间戳，单位：秒（Unix 时间戳），须满足 `end_time >= start_time`         |

**响应**：`data` 含 `active_users`、`signups`、`page_views` 三项指标对象（值、环比基准、环比变化）。

### 获取妙搭应用运营数据趋势

按指标、时间聚合单元与过滤条件查询应用运营数据趋势序列。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryAnalyticsDataResult>?> QueryAppAnalyticsDataAsync(
    [Path] string app_id,
    [Body] QueryAnalyticsDataRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（双令牌基接口）

**参数**：

| 参数名     | 类型                     | 必填 | 说明                                                                                                   |
| ---------- | ------------------------ | ---- | ------------------------------------------------------------------------------------------------------ |
| `app_id`   | `string`                 | ✅   | 妙搭应用 ID，长度 1～1000 字符，示例值：`app_4jbp6bx8fwjgm`                                              |
| `request`  | `QueryAnalyticsDataRequest` | ✅  | `metric_types`、`start_timestamp_ns`、`end_timestamp_ns`、`time_aggregation_unit` 必填；可选 `filter`、`page`、`device_types`、`need_pack_lack_point`、`group_by` |

**响应**：`data.series` 为按指标划分的趋势序列（`metric_type` + `points` 数据点数组）。
