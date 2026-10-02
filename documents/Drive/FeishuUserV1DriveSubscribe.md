---
title: 云文档事件订阅接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份订阅飞书云文档事件，支持订阅/取消订阅、查询订阅状态、用户级订阅管理以及文件订阅状态配置，当文档发生变更时推送通知。
---

# 云文档事件订阅 - 用户令牌（FeishuUserV1DriveSubscribe）

## 接口名称
**云文档事件订阅（用户令牌）** -（`IFeishuUserV1DriveSubscribe`）

## 功能描述
提供以用户身份订阅飞书云文档事件的能力。云文档事件订阅用于订阅云文档的事件，如文件创建、更新、删除等，当云文档发生指定事件时，系统会向配置的地址发送事件通知。支持订阅云文档事件、查询订阅状态、取消订阅、订阅用户云文档事件、取消用户订阅、查询用户订阅状态，以及获取/创建/更新文件订阅状态等操作。适用于需要以用户身份监听云文档变更事件的业务场景，如用户个人文档变更通知、评论订阅等。

## 参考文档
- [云文档事件订阅 - 飞书开放平台](https://open.feishu.cn/document/server-docs/docs/drive-v1/media/introduction)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 | 接口文档 |
|---------|---------|---------|----------|----------|
| SubscribeFileEventAsync | 订阅云文档事件 | 用户令牌 | POST | [SubscribeFileEventAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/event/subscribe) |
| GetFileSubscribeAsync | 查询云文档事件订阅状态 | 用户令牌 | GET | [GetFileSubscribeAsync](https://open.feishu.cn/document/docs/drive-v1/event/get_subscribe) |
| UnsubscribeFileEventAsync | 取消云文档事件订阅 | 用户令牌 | DELETE | [UnsubscribeFileEventAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/event/delete_subscribe) |
| SubscribeUserFileEventAsync | 订阅用户云文档事件 | 用户令牌 | POST | [SubscribeUserFileEventAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/user/subscription) |
| UnsubscribeUserFileEventAsync | 取消用户云文档事件订阅 | 用户令牌 | DELETE | [UnsubscribeUserFileEventAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/user/remove_subscription) |
| GetUserFileSubscribeAsync | 查询用户云文档事件订阅状态 | 用户令牌 | GET | [GetUserFileSubscribeAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/user/subscription_status) |
| GetFileSubscriptionAsync | 获取订阅状态 | 用户令牌 | GET | [GetFileSubscriptionAsync](https://open.feishu.cn/document/server-docs/docs/docs-assistant/file-subscription/get) |
| CreateFileSubscriptionAsync | 创建订阅 | 用户令牌 | POST | [CreateFileSubscriptionAsync](https://open.feishu.cn/document/server-docs/docs/docs-assistant/file-subscription/create) |
| UpdateFileSubscriptionAsync | 更新订阅状态 | 用户令牌 | PATCH | [UpdateFileSubscriptionAsync](https://open.feishu.cn/document/server-docs/docs/docs-assistant/file-subscription/patch) |

## 函数详细内容

### 订阅云文档事件

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> SubscribeFileEventAsync(
    [Path] string file_token,
    [Query("file_type")] string file_type,
    [Query("event_type")] string? event_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 云文档的 token，示例值：`doccnfYZzTlvXqZIGTdAHKabcef` |
| `file_type` | `string` | ✅ | 云文档类型，可选值：`doc`（旧版文档，已不推荐）、`docx`（新版文档）、`sheet`（电子表格）、`bitable`（多维表格）、`file`（文件）、`folder`（文件夹）、`slides`（幻灯片），示例值：`docx` |
| `event_type` | `string?` | ⚪ | 事件类型。若 `file_type` 为 `folder`，需填写 `file.created_in_folder_v1`；若 `file_type` 不为 `folder`，请勿填写，示例值：`file.created_in_folder_v1` |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：订阅云文档的各类通知事件。调用该接口并在开发者后台添加事件后，当云文档发生指定事件时，系统会向配置的地址发送事件。

---

### 查询云文档事件订阅状态

**函数签名**：
```csharp
Task<FeishuApiResult<GetFileSubscribeResult>?> GetFileSubscribeAsync(
    [Path] string file_token,
    [Query("file_type")] string file_type,
    [Query("event_type")] string? event_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 云文档的 token，示例值：`doccnfYZzTlvXqZIGTdAHKabcef` |
| `file_type` | `string` | ✅ | 云文档类型，可选值：`doc`、`docx`、`sheet`、`bitable`、`file`、`folder`、`slides`，示例值：`docx` |
| `event_type` | `string?` | ⚪ | 事件类型。若 `file_type` 为 `folder`，需填写 `file.created_in_folder_v1`；若 `file_type` 不为 `folder`，请勿填写，示例值：`file.created_in_folder_v1` |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "is_subscribe": true
  }
}
```

**说明**：用于查询云文档事件的订阅状态。

---

### 取消云文档事件订阅

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UnsubscribeFileEventAsync(
    [Path] string file_token,
    [Query("file_type")] string file_type,
    [Query("event_type")] string? event_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 云文档的 token，示例值：`doccnfYZzTlvXqZIGTdAHKabcef` |
| `file_type` | `string` | ✅ | 云文档类型，可选值：`doc`、`docx`、`sheet`、`bitable`、`file`、`folder`、`slides`，示例值：`docx` |
| `event_type` | `string?` | ⚪ | 事件类型。若 `file_type` 为 `folder`，需填写 `file.created_in_folder_v1`；若 `file_type` 不为 `folder`，请勿填写，示例值：`file.created_in_folder_v1` |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：用于取消订阅云文档的通知事件。

---

### 订阅用户云文档事件

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> SubscribeUserFileEventAsync(
    [Body] SubscribeUserFileEventRequest subscribeUserFileEventRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `subscribeUserFileEventRequest` | `SubscribeUserFileEventRequest` | ✅ | 订阅用户云文档事件请求体 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：订阅用户云文档的各类通知事件，调用后目前可获取接收者视角的云文档评论、回复添加事件，未来还会陆续扩充其它通知事件。

---

### 取消用户云文档事件订阅

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UnsubscribeUserFileEventAsync(
    [Query("event_type")] string? event_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `event_type` | `string?` | ⚪ | 事件类型，示例值：`file.created_in_folder_v1` |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：用于取消订阅用户云文档的通知事件。取消订阅后，用户将不再收到云文档评论、回复添加事件。

---

### 查询用户云文档事件订阅状态

**函数签名**：
```csharp
Task<FeishuApiResult<GetUserFileSubscribeResult>?> GetUserFileSubscribeAsync(
    [Query("event_type")] string? event_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `event_type` | `string?` | ⚪ | 事件类型，示例值：`file.created_in_folder_v1` |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "is_subscribe": true
  }
}
```

**说明**：用于查询用户云文档事件的订阅状态。仅当 `is_subscribe` 为 `true`，应用才可收到"用户云文档事件"下的各类通知事件。

---

### 获取订阅状态

**函数签名**：
```csharp
Task<FeishuApiResult<FileSubscriptionOOpsResult>?> GetFileSubscriptionAsync(
    [Path] string file_token,
    [Path] string subscription_id,
    [Body] GetFileSubscriptionRequest getFileSubscriptionRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 云文档的 token，示例值：`doccnfYZzTlvXqZIGTdAHKabcef` |
| `subscription_id` | `string` | ✅ | 订阅关系 ID，示例值：`1234567890987654321` |
| `getFileSubscriptionRequest` | `GetFileSubscriptionRequest` | ✅ | 获取订阅状态请求体 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "subscription_id": "1234567890987654321",
    "is_subscribed": true,
    "event_type": "comment"
  }
}
```

**说明**：根据订阅 ID 获取该订阅的状态。

---

### 创建订阅

**函数签名**：
```csharp
Task<FeishuApiResult<FileSubscriptionOOpsResult>?> CreateFileSubscriptionAsync(
    [Path] string file_token,
    [Body] CreateFileSubscriptionRequest createFileSubscriptionRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 云文档的 token，示例值：`doccnfYZzTlvXqZIGTdAHKabcef` |
| `createFileSubscriptionRequest` | `CreateFileSubscriptionRequest` | ✅ | 创建订阅请求体 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "subscription_id": "1234567890987654321",
    "is_subscribed": true,
    "event_type": "comment"
  }
}
```

**说明**：订阅文档中的变更事件，当前支持文档评论订阅，订阅后文档评论更新会有"云文档助手"推送给订阅的用户。

---

### 更新订阅状态

**函数签名**：
```csharp
Task<FeishuApiResult<FileSubscriptionOOpsResult>?> UpdateFileSubscriptionAsync(
    [Path] string file_token,
    [Path] string subscription_id,
    [Body] UpdateFileSubscriptionRequest updateFileSubscriptionRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 云文档的 token，示例值：`doccnfYZzTlvXqZIGTdAHKabcef` |
| `subscription_id` | `string` | ✅ | 订阅关系 ID，示例值：`1234567890987654321` |
| `updateFileSubscriptionRequest` | `UpdateFileSubscriptionRequest` | ✅ | 更新订阅请求体 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "subscription_id": "1234567890987654321",
    "is_subscribed": true,
    "event_type": "comment"
  }
}
```

**说明**：根据订阅 ID 更新订阅状态。
