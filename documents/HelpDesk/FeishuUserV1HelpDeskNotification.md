---
title: 服务台推送接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份管理飞书服务台推送任务，支持推送的创建、查询、更新、预览、提交审批、发送、取消发送与取消审批。
---

# IFeishuUserV1HelpDeskNotification - 用户服务台推送API

## 功能描述
飞书服务台推送API是开放平台基于飞书服务台的推送功能开放的创建/查询/更新/预览/审批/发送等API，开发者可以基于这些API管理服务台推送任务。本接口使用用户访问令牌（UserAccessToken）鉴权，并额外实现 `ICurrentUserId`；接口自身声明了 `HelpdeskTokenAndId` 属性，用于在服务台请求 Header 中添加 `X-Lark-Helpdesk-Authorization` 参数（Value 为 `base64(helpdesk_id:helpdesk_token)`，即通过 base64 加密将 helpdesk_id 和 helpdesk_token 用 `:` 连接而成的字符串）。

## 参考文档
- [创建推送](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/create)
- [查询推送详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/get)
- [更新推送](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/patch)
- [预览推送](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/preview)
- [提交审批](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/submit_approve)
- [发送推送](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/execute_send)
- [取消推送](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/cancel_send)
- [取消审批](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/cancel_approve)
- [服务台接入指南](https://open.feishu.cn/document/server-docs/helpdesk-v1/access-guide)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 | 接口文档 |
| :--- | :--- | :--- | :--- |----------|
| CreateNotificationAsync | 创建推送 | UserAccessToken | POST | [CreateNotificationAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/create) |
| GetNotificationAsync | 查询推送详情 | UserAccessToken | GET | [GetNotificationAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/get) |
| UpdateNotificationAsync | 更新推送 | UserAccessToken | PATCH | [UpdateNotificationAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/patch) |
| PreviewNotificationAsync | 预览推送 | UserAccessToken | POST | [PreviewNotificationAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/preview) |
| SubmitApproveNotificationAsync | 提交审批 | UserAccessToken | POST | [SubmitApproveNotificationAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/submit_approve) |
| ExecuteSendNotificationAsync | 发送推送 | UserAccessToken | POST | [ExecuteSendNotificationAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/execute_send) |
| CancelSendNotificationAsync | 取消推送 | UserAccessToken | POST | [CancelSendNotificationAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/cancel_send) |
| CancelApproveNotificationAsync | 取消审批 | UserAccessToken | POST | [CancelApproveNotificationAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/cancel_approve) |

## 函数详细内容

### CreateNotificationAsync
创建一个推送，创建后处于草稿状态。

**函数签名**
```csharp
Task<FeishuApiResult<CreateNotificationResult>?> CreateNotificationAsync(
    [Body] CreateNotificationRequest request,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | CreateNotificationRequest | ✅ | 创建推送请求体，字段结构与推送任务（`Notification`）一致 | - |
| user_id_type | string? | ⚪ | 用户 ID 类型，默认值：open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "notification_id": "6985032626234982420",
    "status": 0
  }
}
```

**说明**
- 限频：10 次/分钟
- 字段权限（响应中含敏感字段时才返回）：`contact:user.employee_id:readonly`
- `push_type`：0 定时推送（push_scope 不能等于 3）、1 新员工入职推送（push_scope 必须等于 1 或 3，且 `new_staff_scope_type` 不能为空）
- `push_scope_type`：0 组织内全部成员（`user_list` 和 `department_list` 必须为空）、1 无任何成员（`chat_list` 不能为空）、2 指定成员（`user_list` 或 `department_list` 不能为空）、3 新员工
- `job_name` 任务名称与 `push_content` 推送内容必填

**代码示例**
```csharp
var notificationApi = feishuApp.GetApi<IFeishuUserV1HelpDeskNotification>();
notificationApi.HelpdeskTokenAndId = Convert.ToBase64String(
    Encoding.UTF8.GetBytes($"{helpdeskId}:{helpdeskToken}"));

var request = new CreateNotificationRequest
{
    JobName = "Test the push task",
    PushContent = "{\"header\":{\"title\":{\"tag\":\"plain_text\",\"content\":\"服务提醒\"}}}",
    PushType = 0,
    PushScopeType = 2,
    UserList = new[]
    {
        new NotificationUser { UserId = "ou_7277fd1262bfafc363d5b2a1f9c2ac90", Name = "test" }
    }
};
var result = await notificationApi.CreateNotificationAsync(request);
Console.WriteLine($"推送 ID: {result?.Data?.NotificationId}，状态: {result?.Data?.Status}");
```

---

### GetNotificationAsync
查询推送详情。

**函数签名**
```csharp
Task<FeishuApiResult<GetNotificationResult>?> GetNotificationAsync(
    [Path] string notification_id,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| notification_id | string | ✅ | 唯一 ID | 1624326025000 |
| user_id_type | string? | ⚪ | 用户 ID 类型，默认值：open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "notification": {
      "id": "6981801914270744596",
      "job_name": "Test the push task",
      "status": 0,
      "create_user": { "user_id": "ou_7277fd1262bfafc363d5b2a1f9c2ac90", "name": "test" },
      "created_at": "1626332244719",
      "update_user": { "user_id": "ou_7277fd1262bfafc363d5b2a1f9c2ac90", "name": "test" },
      "updated_at": "1626332244719",
      "target_user_count": 1,
      "sent_user_count": 1,
      "read_user_count": 1,
      "send_at": "1626332244719",
      "push_content": "{\"header\":{\"title\":{\"tag\":\"plain_text\",\"content\":\"服务提醒\"}}}",
      "push_type": 0,
      "push_scope_type": 2,
      "user_list": [{ "user_id": "ou_7277fd1262bfafc363d5b2a1f9c2ac90", "name": "test" }],
      "department_list": [],
      "chat_list": [],
      "ext": "{}"
    },
    "approval_app_link": "http://applink.feishu.cn/*xx"
  }
}
```

**说明**
- 限频：100 次/分钟
- 字段权限（响应中含敏感字段时才返回）：`contact:user.employee_id:readonly`
- 响应额外返回 `approval_app_link` 审批链接

**代码示例**
```csharp
var notificationApi = feishuApp.GetApi<IFeishuUserV1HelpDeskNotification>();
var result = await notificationApi.GetNotificationAsync("6981801914270744596");
Console.WriteLine($"任务名称: {result?.Data?.Notification?.JobName}，状态: {result?.Data?.Notification?.Status}");
Console.WriteLine($"审批链接: {result?.Data?.ApprovalAppLink}");
```

---

### UpdateNotificationAsync
更新推送消息，仅可在消息处于草稿状态时调用本接口。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateNotificationAsync(
    [Path] string notification_id,
    [Body] UpdateNotificationRequest request,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| notification_id | string | ✅ | 推送任务唯一 ID | 6985032626234982420 |
| request | UpdateNotificationRequest | ✅ | 更新推送请求体，字段结构与推送任务（`Notification`）一致 | - |
| user_id_type | string? | ⚪ | 用户 ID 类型，默认值：open_id | open_id |
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
- 限频：20 次/分钟
- 字段权限（响应中含敏感字段时才返回）：`contact:user.employee_id:readonly`
- 仅可在消息处于草稿状态时调用本接口

**代码示例**
```csharp
var notificationApi = feishuApp.GetApi<IFeishuUserV1HelpDeskNotification>();
var request = new UpdateNotificationRequest
{
    JobName = "Test the push task",
    PushContent = "{\"header\":{\"title\":{\"tag\":\"plain_text\",\"content\":\"服务提醒（已更新）\"}}}",
    PushType = 0,
    PushScopeType = 2
};
var result = await notificationApi.UpdateNotificationAsync("6985032626234982420", request);
Console.WriteLine($"更新结果: {result?.Code == 0}");
```

---

### PreviewNotificationAsync
推送前预览已设置的推送内容。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> PreviewNotificationAsync(
    [Path] string notification_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| notification_id | string | ✅ | 推送创建 API 成功后返回的唯一 ID | 6985032626234982420 |
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
- 限频：20 次/分钟
- 本接口无请求体、无查询参数

**代码示例**
```csharp
var notificationApi = feishuApp.GetApi<IFeishuUserV1HelpDeskNotification>();
var result = await notificationApi.PreviewNotificationAsync("6985032626234982420");
Console.WriteLine($"预览结果: {result?.Code == 0}");
```

---

### SubmitApproveNotificationAsync
通常在调用「创建推送」API 后调用本接口，提交推送审批。

**函数签名**
```csharp
Task<FeishuApiResult<SubmitApproveNotificationResult>?> SubmitApproveNotificationAsync(
    [Path] string notification_id,
    [Body] SubmitApproveNotificationRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| notification_id | string | ✅ | 「创建推送」API 返回的唯一 ID | 6985032626234982420 |
| request | SubmitApproveNotificationRequest | ✅ | 提交审批请求体（`reason` 提交审批的原因，必填） | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "has_access": true
  }
}
```

**说明**
- 限频：10 次/分钟
- 如果创建者是服务台所有者，推送消息将自动审批通过；否则会通知服务台所有者审批该推送消息
- `has_access` 表示是否有创建或管理审批流程的权限范围；以下两种情况下用户没有该权限范围：1. 用户未安装服务台小组件；2. 用户安装的服务台小组件版本过低

**代码示例**
```csharp
var notificationApi = feishuApp.GetApi<IFeishuUserV1HelpDeskNotification>();
var request = new SubmitApproveNotificationRequest { Reason = "Submit for approval" };
var result = await notificationApi.SubmitApproveNotificationAsync("6985032626234982420", request);
Console.WriteLine($"提交审批: {result?.Code == 0}，有权限范围: {result?.Data?.HasAccess}");
```

---

### ExecuteSendNotificationAsync
审批通过后，调用本接口设置推送时间，等待调度系统发送消息。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> ExecuteSendNotificationAsync(
    [Path] string notification_id,
    [Body] ExecuteSendNotificationRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| notification_id | string | ✅ | 「创建推送」API 返回的唯一 ID | 6985032626234982420 |
| request | ExecuteSendNotificationRequest | ✅ | 发送推送请求体（`send_at` 发送的时间戳，单位毫秒，必填） | - |
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
- 限频：10 次/分钟
- 调用前提：审批已通过
- `send_at` 为发送的时间戳（单位毫秒）

**代码示例**
```csharp
var notificationApi = feishuApp.GetApi<IFeishuUserV1HelpDeskNotification>();
var request = new ExecuteSendNotificationRequest { SendAt = "1624326025000" };
var result = await notificationApi.ExecuteSendNotificationAsync("6985032626234982420", request);
Console.WriteLine($"发送结果: {result?.Code == 0}");
```

---

### CancelSendNotificationAsync
取消推送，可在审批通过后等待定时发送期间、消息发送中以及发送完成后调用。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> CancelSendNotificationAsync(
    [Path] string notification_id,
    [Body] CancelSendNotificationRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| notification_id | string | ✅ | 唯一 ID | 6981801914270744596 |
| request | CancelSendNotificationRequest | ✅ | 取消推送请求体（`is_recall` 是否撤回已发送的消息，必填） | - |
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
- 可在审批通过后等待定时发送期间、消息发送中（已发送消息将被撤回）、以及发送完成后（所有已发送消息将被撤回）调用
- `is_recall` 表示是否撤回已发送的消息，同样适用于新员工的消息

**代码示例**
```csharp
var notificationApi = feishuApp.GetApi<IFeishuUserV1HelpDeskNotification>();
var request = new CancelSendNotificationRequest { IsRecall = true };
var result = await notificationApi.CancelSendNotificationAsync("6981801914270744596", request);
Console.WriteLine($"取消发送结果: {result?.Code == 0}");
```

---

### CancelApproveNotificationAsync
提交审批后调用本接口取消审批。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> CancelApproveNotificationAsync(
    [Path] string notification_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| notification_id | string | ✅ | 唯一 ID | 6981801914270744596 |
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
- 限频：10 次/分钟
- 本接口无请求体、无查询参数

**代码示例**
```csharp
var notificationApi = feishuApp.GetApi<IFeishuUserV1HelpDeskNotification>();
var result = await notificationApi.CancelApproveNotificationAsync("6981801914270744596");
Console.WriteLine($"取消审批结果: {result?.Code == 0}");
```
