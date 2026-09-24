# 飞书 Aily 会话 - 用户令牌（FeishuUserV1AilySessions）

## 接口名称

**飞书 Aily 会话（用户令牌）** -（`IFeishuUserV1AilySessions`）

## 功能描述

提供以用户身份管理飞书 Aily 会话的能力。飞书 Aily SDK 是一组服务端 OpenAPI 的封装，用于让用户以编程方式调用飞书 Aily（智能伙伴/智能体）的能力，把它集成到自己的业务系统里。支持创建会话、更新会话、获取会话、删除会话、发送 Aily 消息、获取 Aily 消息、获取 Aily 消息列表、创建运行、获取运行、列出运行和取消运行等操作。

## 参考文档

- [飞书 Aily 使用指南 - 飞书开放平台](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent/agentuseguide)

## 函数列表

| 函数名称                                | 功能描述             | 认证方式 | HTTP 方法 |
| --------------------------------------- | -------------------- | -------- | --------- |
| CreateSessionAsync                      | 创建会话             | 用户令牌 | POST      |
| UpdateSessionAsync                      | 更新会话             | 用户令牌 | PUT       |
| GetSessionAsync                         | 获取会话             | 用户令牌 | GET       |
| DeleteSessionAsync                      | 删除会话             | 用户令牌 | DELETE    |
| CreateSessionAilyMessageAsync           | 发送 Aily 消息       | 用户令牌 | POST      |
| GetSessionAilyMessageAsync              | 获取 Aily 消息       | 用户令牌 | GET       |
| GetSessionAilyMessagePageListAsync      | 获取 Aily 消息（列表） | 用户令牌 | GET       |
| CreateSessionRunAsync                   | 创建运行             | 用户令牌 | POST      |
| GetSessionRunAsync                      | 获取运行             | 用户令牌 | GET       |
| GetSessionRunPageListAsync              | 列出运行             | 用户令牌 | GET       |
| CancelSessionRunAsync                   | 取消运行             | 用户令牌 | POST      |

> 说明：源码中 `GetSessionAilyMessagePageListAsync` 的 XML 摘要与 `GetSessionAilyMessageAsync` 同名（均为「获取 Aily 消息」），本文档在列表接口的摘要后补充「（列表）」以便区分。

## 函数详细内容

### 创建会话

用于创建与某个飞书 Aily 应用的一次会话（Session）；当创建会话成功后，可以发送消息、创建运行。

**函数签名**：

```csharp
Task<FeishuApiResult<SessionOopsResult>?> CreateSessionAsync(
     [Body] SessionOopsRequest request,
     CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名     | 类型                 | 必填 | 说明             |
| ---------- | -------------------- | ---- | ---------------- |
| `request`  | `SessionOopsRequest` | ✅   | 创建会话请求体   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "session": {
      "id": "session_4dfunz7sp1g8m",
      "created_at": "1710000000000",
      "modified_at": "1710000000000",
      "created_by": "ou_xxx",
      "channel_context": "",
      "metadata": ""
    }
  }
}
```

**说明**：创建成功后返回会话信息，后续发送消息、创建运行都以该会话 ID 为路径参数。

---

### 更新会话

用于更新与某个飞书 Aily 应用的一次会话（Session）；当更新会话成功后，可以发送消息、创建运行。

**函数签名**：

```csharp
Task<FeishuApiResult<SessionOopsResult>?> UpdateSessionAsync(
     [Path] string aily_session_id,
     [Body] SessionOopsRequest request,
     CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名             | 类型                 | 必填 | 说明                                     |
| ------------------ | -------------------- | ---- | ---------------------------------------- |
| `aily_session_id`  | `string`             | ✅   | 会话 ID，示例值：`session_4dfunz7sp1g8m` |
| `request`          | `SessionOopsRequest` | ✅   | 更新会话请求体                           |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "session": {
      "id": "session_4dfunz7sp1g8m",
      "created_at": "1710000000000",
      "modified_at": "1710000000001",
      "created_by": "ou_xxx",
      "channel_context": "",
      "metadata": "{\"key\":\"value\"}"
    }
  }
}
```

**说明**：请求体携带 `channel_context`、`metadata` 等可更新字段，未命名字段保持原值。

---

### 获取会话

用于获取与某个飞书 Aily 应用的一次会话（Session）的详细信息。

**函数签名**：

```csharp
Task<FeishuApiResult<SessionOopsResult>?> GetSessionAsync(
     [Path] string aily_session_id,
     CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名             | 类型     | 必填 | 说明                                     |
| ------------------ | -------- | ---- | ---------------------------------------- |
| `aily_session_id`  | `string` | ✅   | 会话 ID，示例值：`session_4dfunz7sp1g8m` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "session": {
      "id": "session_4dfunz7sp1g8m",
      "created_at": "1710000000000",
      "modified_at": "1710000000000",
      "created_by": "ou_xxx",
      "channel_context": "",
      "metadata": ""
    }
  }
}
```

**说明**：返回会话详细信息，包含创建时间、修改时间与创建人等。

---

### 删除会话

用于删除与某个飞书 Aily 应用的一次会话（Session）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteSessionAsync(
     [Path] string aily_session_id,
     CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名             | 类型     | 必填 | 说明                                     |
| ------------------ | -------- | ---- | ---------------------------------------- |
| `aily_session_id`  | `string` | ✅   | 会话 ID，示例值：`session_4dfunz7sp1g8m` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：删除成功时 `Data` 为空对象；删除后该会话下的消息与运行不可再访问。

---

### 发送 Aily 消息

用于向某个飞书 Aily 应用发送一条消息（Message）；每个消息从属于一个活跃的会话（Session）。

**函数签名**：

```csharp
Task<FeishuApiResult<SessionAilyMessageOopsResult>?> CreateSessionAilyMessageAsync(
    [Path] string aily_session_id,
    [Body] CreateSessionAilyMessageRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名             | 类型                                | 必填 | 说明                                     |
| ------------------ | ----------------------------------- | ---- | ---------------------------------------- |
| `aily_session_id`  | `string`                            | ✅   | 会话 ID，示例值：`session_4dfunz7sp1g8m` |
| `request`          | `CreateSessionAilyMessageRequest`   | ✅   | 创建会话消息请求体                       |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "message": {
      "id": "message_4dfunz7sp1g8m",
      "session_id": "session_4dfunz7sp1g8m",
      "run_id": "run_4dfrxvctjqzzj",
      "content_type": "text",
      "content": "你好",
      "plain_text": "你好",
      "created_at": "1710000000000"
    }
  }
}
```

**说明**：请求体支持 `idempotent_id` 幂等标识、`content_type`、`content`、`file_ids`、`quote_message_id` 与 `mentions`；仅能向活跃会话发送消息。

---

### 获取 Aily 消息

用于获取与某个飞书 Aily 应用的一条消息（Message）的详细信息。

**函数签名**：

```csharp
Task<FeishuApiResult<SessionAilyMessageOopsResult>?> GetSessionAilyMessageAsync(
    [Path] string aily_session_id,
    [Path] string aily_message_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名             | 类型     | 必填 | 说明                                     |
| ------------------ | -------- | ---- | ---------------------------------------- |
| `aily_session_id`  | `string` | ✅   | 会话 ID，示例值：`session_4dfunz7sp1g8m` |
| `aily_message_id`  | `string` | ✅   | 消息 ID，示例值：`message_4dfunz7sp1g8m` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "message": {
      "id": "message_4dfunz7sp1g8m",
      "session_id": "session_4dfunz7sp1g8m",
      "run_id": "run_4dfrxvctjqzzj",
      "content_type": "text",
      "content": "你好",
      "quote_message_id": "",
      "plain_text": "你好",
      "created_at": "1710000000000"
    }
  }
}
```

**说明**：返回单条消息的详细信息，包含正文、附件、引用消息与发送者等。

---

### 获取 Aily 消息（列表）

用于分页列出某个会话（Session）下的消息（Message）。

**函数签名**：

```csharp
Task<FeishuApiResult<SessionAilyMessagePageListResult>?> GetSessionAilyMessagePageListAsync(
  [Path] string aily_session_id,
  [Query("page_size")] int page_size = Consts.PageSize_20,
  [Query("page_token")] string? page_token = null,
  [Query] string? run_id = null,
  [Query] bool? with_partial_message = null,
  CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                  | 类型      | 必填 | 说明                                                                 |
| ----------------------- | --------- | ---- | -------------------------------------------------------------------- |
| `aily_session_id`       | `string`  | ✅   | 会话 ID，示例值：`session_4dfunz7sp1g8m`                             |
| `page_size`             | `int`     | ⚪   | 分页大小，默认取 `Consts.PageSize_20`                                |
| `page_token`            | `string?` | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token |
| `run_id`                | `string?` | ⚪   | 过滤条件，按执行的唯一 ID 筛选，示例值：`run_4dfrxvctjqzzj`          |
| `with_partial_message`  | `bool?`   | ⚪   | 是否返回正在进行中（即流式输出中）的消息内容                         |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "messages": [
      {
        "id": "message_4dfunz7sp1g8m",
        "session_id": "session_4dfunz7sp1g8m",
        "run_id": "run_4dfrxvctjqzzj",
        "content_type": "text",
        "content": "你好",
        "plain_text": "你好",
        "created_at": "1710000000000"
      }
    ],
    "has_more": false,
    "page_token": ""
  }
}
```

**说明**：返回消息分页列表；传入 `run_id` 可只返回某次运行产生的消息，传入 `with_partial_message` 可包含仍在流式输出中的消息内容。

---

### 创建运行

用于在某个飞书 Aily 应用的会话（Session）上创建一次运行（Run）。

**函数签名**：

```csharp
Task<FeishuApiResult<SessionRunOopsResult>?> CreateSessionRunAsync(
    [Path] string aily_session_id,
    [Body] CreateSessionRunRequest request,
    [Header("X-Aily-BizUserID")] string? x_aily_biz_user_id = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                | 类型                       | 必填 | 说明                                                             |
| --------------------- | -------------------------- | ---- | ---------------------------------------------------------------- |
| `aily_session_id`     | `string`                   | ✅   | 会话 ID，示例值：`session_4dfunz7sp1g8m`                         |
| `request`             | `CreateSessionRunRequest`  | ✅   | 创建运行请求体                                                   |
| `x_aily_biz_user_id`  | `string?`                  | ⚪   | 可选请求头（`X-Aily-BizUserID`），唯一的用户身份标识（建议使用内部唯一 ID 或其他唯一字段），最大长度 64 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "run": {
      "id": "run_4dfrxvctjqzzj",
      "created_at": "1710000000000",
      "app_id": "spring_5862e4fea8__c",
      "session_id": "session_4dfunz7sp1g8m",
      "status": "running",
      "started_at": "1710000000000",
      "ended_at": "",
      "error": null,
      "metadata": ""
    }
  }
}
```

**说明**：运行在后台异步执行，可通过获取运行接口轮询状态，或在取消前调用取消运行接口终止。

---

### 获取运行

用于获取某个飞书 Aily 应用会话（Session）上一次运行（Run）的详细信息，包括运行状态、结束时间等。

**函数签名**：

```csharp
Task<FeishuApiResult<SessionRunOopsResult>?> GetSessionRunAsync(
    [Path] string aily_session_id,
    [Path] string run_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名             | 类型     | 必填 | 说明                                     |
| ------------------ | -------- | ---- | ---------------------------------------- |
| `aily_session_id`  | `string` | ✅   | 会话 ID，示例值：`session_4dfunz7sp1g8m` |
| `run_id`           | `string` | ✅   | 运行 ID，示例值：`run_4dfrxvctjqzzj`     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "run": {
      "id": "run_4dfrxvctjqzzj",
      "created_at": "1710000000000",
      "app_id": "spring_5862e4fea8__c",
      "session_id": "session_4dfunz7sp1g8m",
      "status": "success",
      "started_at": "1710000000000",
      "ended_at": "1710000000010",
      "error": null,
      "metadata": ""
    }
  }
}
```

**说明**：返回运行详细信息；执行失败时 `error` 字段携带 `code` 与 `message`。

---

### 列出运行

用于列出某个飞书 Aily 应用会话（Session）上的运行（Run）详情，包括状态、结束时间等。

**函数签名**：

```csharp
Task<FeishuApiResult<SessionRunPageListResult>?> GetSessionRunPageListAsync(
    [Path] string aily_session_id,
    [Query("page_size")] int page_size = Consts.PageSize_20,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名             | 类型      | 必填 | 说明                                                                 |
| ------------------ | --------- | ---- | -------------------------------------------------------------------- |
| `aily_session_id`  | `string`  | ✅   | 会话 ID，示例值：`session_4dfunz7sp1g8m`                             |
| `page_size`        | `int`     | ⚪   | 分页大小，默认取 `Consts.PageSize_20`                                |
| `page_token`       | `string?` | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "runs": [
      {
        "id": "run_4dfrxvctjqzzj",
        "created_at": "1710000000000",
        "app_id": "spring_5862e4fea8__c",
        "session_id": "session_4dfunz7sp1g8m",
        "status": "success",
        "started_at": "1710000000000",
        "ended_at": "1710000000010",
        "error": null,
        "metadata": ""
      }
    ],
    "has_more": false,
    "page_token": ""
  }
}
```

**说明**：返回运行分页列表；`has_more` 为 true 时需携带返回的 `page_token` 继续拉取。

---

### 取消运行

用于取消某个飞书 Aily 应用会话（Session）上的一次运行（Run）。

**函数签名**：

```csharp
Task<FeishuApiResult<SessionRunOopsResult>?> CancelSessionRunAsync(
    [Path] string aily_session_id,
    [Path] string run_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名             | 类型     | 必填 | 说明                                     |
| ------------------ | -------- | ---- | ---------------------------------------- |
| `aily_session_id`  | `string` | ✅   | 会话 ID，示例值：`session_4dfunz7sp1g8m` |
| `run_id`           | `string` | ✅   | 运行 ID，示例值：`run_4dfrxvctjqzzj`     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "run": {
      "id": "run_4dfrxvctjqzzj",
      "session_id": "session_4dfunz7sp1g8m",
      "status": "cancelled"
    }
  }
}
```

**说明**：返回取消后的运行信息；已结束（成功或失败）的运行无法取消。
