---
title: 绩效关键指标数据接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份管理飞书绩效关键指标数据，支持批量获取被评估人的关键指标结果，以及批量录入关键指标数据。
---

# 关键指标数据 - 租户令牌（FeishuTenantV2PerformanceMetricDetail）

## 接口名称

**关键指标数据（租户令牌）** -（`IFeishuTenantV2PerformanceMetricDetail`）

## 功能描述

提供以租户身份管理飞书绩效关键指标数据的能力。飞书绩效（Performance）「关键指标数据」SDK 是一组服务端 OpenAPI 的封装，用于批量获取指定周期中被评估人的关键指标结果，以及批量录入被评估人的关键指标数据。本接口全部端点为 performance/v2，仅支持 tenant_access_token 调用。支持获取被评估人关键指标结果、录入被评估人关键指标数据等操作。

## 参考文档

- [获取被评估人关键指标结果 - 飞书开放平台](https://open.feishu.cn/document/performance-v1/metric_detail/query)

## 函数列表

| 函数名称                   | 功能描述                 | 认证方式 | HTTP 方法 |
| -------------------------- | ------------------------ | -------- | --------- |
| QueryMetricDetailListAsync | 获取被评估人关键指标结果 | 租户令牌 | POST      |
| ImportMetricDetailAsync    | 录入被评估人关键指标数据 | 租户令牌 | POST      |

## 函数详细内容

### 获取被评估人关键指标结果

批量获取指定周期中被评估人的关键指标结果，1 次只允许查询 1 个周期。限频：20 次/分钟。所需权限（开启任一即可）：performance:metric:write（管理关键指标数据）、performance:metric:read（获取关键指标数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryMetricDetailListResult>?> QueryMetricDetailListAsync(
    [Body] QueryMetricDetailListRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                           | 必填 | 说明                                                                                              |
| -------------- | ------------------------------ | ---- | ------------------------------------------------------------------------------------------------- |
| `request`      | `QueryMetricDetailListRequest` | ✅   | 请求体（semester_id 周期 ID 必填，1 次只允许查询 1 个周期；reviewee_user_ids 被评估人 ID 列表必填，1~50 个） |
| `user_id_type` | `string?`                      | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id                             |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "semester_id": "7291278856547794964",
    "reviewee_metrics": [
      {
        "reviewee_user_id": {
          "union_id": "on_8ddcc7c7c0f7c09f8d7c1a3b2c4d5e6f",
          "user_id": "bega29ca"
        },
        "metric_template_id": "7294570803306168339",
        "metric_details": [],
        "reviewee_stage_statuses": []
      }
    ]
  }
}
```

**说明**：一次调用只能查询 1 个周期，被评估人 ID 列表支持 1~50 个。指标明细的具体字段值需结合「获取指标字段列表」接口的字段定义解析。

---

### 录入被评估人关键指标数据

批量录入指定周期中被评估人的关键指标数据；同一个被评估人不允许传入重复的指标，指标的一个字段只允许传入唯一的字段值。限频：10 次/分钟。所需权限：performance:metric:write（管理关键指标数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。client_token 用于幂等去重，重复提交将被拦截（错误码 1580110）。

**函数签名**：

```csharp
Task<FeishuApiResult<ImportMetricDetailResult>?> ImportMetricDetailAsync(
    [Body] ImportMetricDetailRequest request,
    [Query("client_token")] string client_token,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名          | 类型                        | 必填 | 说明                                                                                          |
| --------------- | --------------------------- | ---- | --------------------------------------------------------------------------------------------- |
| `request`       | `ImportMetricDetailRequest` | ✅   | 请求体（semester_id 周期 ID 必填；import_record_name 录入记录名称，默认「API录入」；imported_metrics 指标明细列表必填，1~50 条） |
| `client_token`  | `string`                    | ✅   | 幂等请求标识，长度 0~64 字符（必填），示例值：`12454646`                                       |
| `user_id_type`  | `string?`                   | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id                         |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "import_record_id": "7241404194141224979"
  }
}
```

**说明**：`client_token` 为必填的幂等标识，重复的 `client_token` 提交会被拦截并返回错误码 1580110；如需重试同一批数据，请沿用同一 `client_token`。指标明细列表单次最多 50 条。
