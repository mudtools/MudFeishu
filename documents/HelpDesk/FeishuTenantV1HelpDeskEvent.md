---
title: 服务台事件订阅接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份管理飞书服务台事件的订阅状态，支持订阅与取消订阅服务台事件。
---

# IFeishuTenantV1HelpDeskEvent - 租户服务台事件订阅API

## 功能描述
飞书服务台事件订阅API是开放平台基于飞书服务台的事件功能开放的订阅/取消订阅API，开发者可以基于这些API管理服务台事件的订阅状态。本接口使用租户访问令牌（TenantAccessToken）鉴权；接口自身声明了 `HelpdeskTokenAndId` 属性，用于在服务台请求 Header 中添加 `X-Lark-Helpdesk-Authorization` 参数（Value 为 `base64(helpdesk_id:helpdesk_token)`，即通过 base64 加密将 helpdesk_id 和 helpdesk_token 用 `:` 连接而成的字符串）。

## 参考文档
- [订阅服务台事件](https://open.feishu.cn/document/server-docs/helpdesk-v1/event/subscribe)
- [取消订阅服务台事件](https://open.feishu.cn/document/server-docs/helpdesk-v1/event/unsubscribe)
- [服务台接入指南](https://open.feishu.cn/document/server-docs/helpdesk-v1/access-guide)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 | 接口文档 |
| :--- | :--- | :--- | :--- |----------|
| SubscribeEventAsync | 订阅服务台事件 | TenantAccessToken | POST | [SubscribeEventAsync](https://open.feishu.cn/document/server-docs/helpdesk-v1/event/subscribe) |
| UnsubscribeEventAsync | 取消订阅服务台事件 | TenantAccessToken | POST | [UnsubscribeEventAsync](https://open.feishu.cn/document/server-docs/helpdesk-v1/event/unsubscribe) |

## 函数详细内容

### SubscribeEventAsync
用于订阅服务台事件。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> SubscribeEventAsync(
    [Body] SubscribeEventRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | SubscribeEventRequest | ✅ | 订阅服务台事件请求体（`events` 必填） | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- `events` 为可订阅事件列表，每项包含 `type`（事件类型）与 `subtype`（事件子类型），二者均为必填
- 订阅成功后事件将按订阅关系推送

**代码示例**
```csharp
var eventApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskEvent>();
eventApi.HelpdeskTokenAndId = Convert.ToBase64String(
    Encoding.UTF8.GetBytes($"{helpdeskId}:{helpdeskToken}"));

var request = new SubscribeEventRequest
{
    Events = new[]
    {
        new HelpDeskEvent
        {
            Type = "helpdesk.ticket_message",
            Subtype = "ticket_message.created_v1"
        }
    }
};
var result = await eventApi.SubscribeEventAsync(request);
Console.WriteLine($"订阅结果: {result?.Code == 0}");
```

---

### UnsubscribeEventAsync
用于取消订阅服务台事件。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UnsubscribeEventAsync(
    [Body] UnsubscribeEventRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | UnsubscribeEventRequest | ✅ | 取消订阅服务台事件请求体（`events` 必填） | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- `events` 为取消订阅的事件列表，每项包含 `type`（事件类型）与 `subtype`（事件子类型），二者均为必填
- 请求体结构与订阅接口一致，仅语义为取消订阅

**代码示例**
```csharp
var eventApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskEvent>();
var request = new UnsubscribeEventRequest
{
    Events = new[]
    {
        new HelpDeskEvent
        {
            Type = "helpdesk.ticket_message",
            Subtype = "ticket_message.created_v1"
        }
    }
};
var result = await eventApi.UnsubscribeEventAsync(request);
Console.WriteLine($"取消订阅结果: {result?.Code == 0}");
```
