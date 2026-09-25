# 飞书妙搭应用管理 - 租户令牌（FeishuTenantV1SparkApp）

## 接口名称

**飞书妙搭应用管理（租户令牌）** -（`IFeishuTenantV1SparkApp`）

## 功能描述

以租户身份调用支持 tenant_access_token 的飞书妙搭（Spark）应用只读端点：批量获取妙搭应用、查询应用消耗 AI 额度、获取运营数据总览与运营数据趋势。本接口为 `IFeishuV1SparkApp`（双令牌基接口）的租户令牌派生，创建、更新、图标上传、HTML 发布与可用范围等 user-only 写端点见 `IFeishuUserV1SparkApp`。

## 参考文档

- [批量获取妙搭应用](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/list)
- [获取妙搭应用消耗 AI 额度](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/open_api_credit_usage)
- [获取妙搭应用运营数据总览](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/open_api_analytics_overview)
- [获取妙搭应用运营数据趋势](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/query_analytics_data)

## 函数列表

| 函数名称                     | 功能描述             | 认证方式 | HTTP 方法 |
| ---------------------------- | -------------------- | -------- | --------- |
| GetAppListAsync              | 批量获取妙搭应用     | 租户令牌 | GET       |
| GetAppCreditUsageAsync       | 获取应用消耗 AI 额度 | 租户令牌 | GET       |
| GetAppAnalyticsOverviewAsync | 获取运营数据总览     | 租户令牌 | GET       |
| QueryAppAnalyticsDataAsync   | 获取运营数据趋势     | 租户令牌 | POST      |

## 函数详细内容

4 个只读端点均继承自 `IFeishuV1SparkApp`，签名与参数同 [FeishuUserV1SparkApp](./FeishuUserV1SparkApp.md) 中对应章节，仅认证方式为租户令牌。

### 批量获取妙搭应用

**函数签名**：

```csharp
Task<FeishuApiResult<GetAppListResult>?> GetAppListAsync(
    [Query] GetAppListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌。`query` 为查询对象 `GetAppListQuery?`（分页、应用类型、关键词与归属范围），整体展开为查询参数。

### 获取妙搭应用消耗 AI 额度

**函数签名**：

```csharp
Task<FeishuApiResult<GetCreditUsageResult>?> GetAppCreditUsageAsync(
    [Path] string app_id,
    [Query("start_time")] string start_time,
    [Query("end_time")] string end_time,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌。时间参数为秒级 Unix 时间戳字符串，须满足 `end_time >= start_time`。

### 获取妙搭应用运营数据总览

**函数签名**：

```csharp
Task<FeishuApiResult<GetAnalyticsOverviewResult>?> GetAppAnalyticsOverviewAsync(
    [Path] string app_id,
    [Query("start_time")] string start_time,
    [Query("end_time")] string end_time,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌。返回 `active_users`、`signups`、`page_views` 三项指标对象（值、环比基准、环比变化）。

### 获取妙搭应用运营数据趋势

**函数签名**：

```csharp
Task<FeishuApiResult<QueryAnalyticsDataResult>?> QueryAppAnalyticsDataAsync(
    [Path] string app_id,
    [Body] QueryAnalyticsDataRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌。请求体 `QueryAnalyticsDataRequest` 中 `metric_types`、`start_timestamp_ns`、`end_timestamp_ns`、`time_aggregation_unit` 必填；可选 `filter`、`page`、`device_types`、`need_pack_lack_point`、`group_by`。
