---
title: OpenAPI 审计日志接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份获取飞书 OpenAPI 审计日志数据，包括调用方、时间、请求与响应摘要等信息。
---

# OpenAPI 审计日志 - 租户令牌（FeishuTenantV1SecurityOpenApiLog）

## 接口名称

**OpenAPI 审计日志（租户令牌）** -（`IFeishuTenantV1SecurityOpenApiLog`）

## 功能描述

提供以租户身份获取飞书 OpenAPI 审计日志的能力。飞书安全与合规（Security）「OpenAPI 审计日志」SDK 用于获取 OpenAPI 审计日志数据（调用方、时间、请求与响应摘要等）。本接口为 security_and_compliance/v1 端点，仅支持 tenant_access_token 调用。支持获取 OpenAPI 审计日志数据操作。

## 参考文档

- [获取 OpenAPI 审计日志数据 - 飞书开放平台](https://open.feishu.cn/document/security_and_compliance-v1/openapi_log/list_data)

## 函数列表

| 函数名称                 | 功能描述                   | 认证方式 | HTTP 方法 |
| ------------------------ | -------------------------- | -------- | --------- |
| ListOpenApiLogDataAsync  | 获取 OpenAPI 审计日志数据  | 租户令牌 | POST      |

## 函数详细内容

### 获取 OpenAPI 审计日志数据

获取 OpenAPI 审计日志数据，可按 API 列表、时间范围、应用 ID 筛选并分页返回。限频：100 次/分钟。所需权限：security_and_compliance:audit_log.openapi_log:readonly（查看 OpenAPI 审计日志）。

**函数签名**：

```csharp
Task<FeishuApiResult<ListOpenApiLogDataResult>?> ListOpenApiLogDataAsync(
    [Body] ListOpenApiLogDataRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                          | 必填 | 说明                                                                                                                |
| --------- | ----------------------------- | ---- | ------------------------------------------------------------------------------------------------------------------- |
| `request` | `ListOpenApiLogDataRequest`   | ✅   | 请求体（api_keys API 列表最大 100 个；start_time/end_time 秒级时间戳；app_id 应用唯一标识；page_size 分页大小 1~100；page_token 分页标记） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "id": "10000",
        "api_key": "POST/open-apis/demo/v1/example",
        "event_time": "1704038400",
        "app_id": "cli_a98ea7d1a0ba100b",
        "ip": "10.0.0.1",
        "log_detail": "{\"request\":{},\"response\":{}}"
      }
    ],
    "page_token": "next_page_token",
    "has_more": true
  }
}
```

**说明**：单次最多按 100 个 API 过滤，`page_size` 取值范围 1~100。当 `has_more` 为 true 时，可使用返回的 `page_token` 继续拉取下一页数据。
