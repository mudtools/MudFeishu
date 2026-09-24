# 飞书 Aily 智能体 - 用户令牌（FeishuUserV1AilyAgent）

## 接口名称

**飞书 Aily 智能体（用户令牌）** -（`IFeishuUserV1AilyAgent`）

## 功能描述

提供以用户身份调用飞书 Aily 智能体的能力。飞书 Aily SDK 是一组服务端 OpenAPI 的封装，用于让用户以编程方式调用飞书 Aily（智能伙伴/智能体）的智能体对话、会话管理、附件与产物能力，把它集成到自己的业务系统里。除基础接口方法外，还提供仅支持用户身份的智能体可见性查询。支持上传附件、查询会话列表、获取指定会话信息、创建会话、发起智能体对话（含 SSE 流式输出）、获取对话结果、下载智能体产物和删除会话等操作。

## 参考文档

- [发起智能体对话 - 飞书开放平台](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent-agent_chat/create)

## 函数列表

| 函数名称                            | 功能描述               | 认证方式 | HTTP 方法 |
| ----------------------------------- | ---------------------- | -------- | --------- |
| CreateAgentAttachmentAsync          | 上传附件               | 用户令牌 | POST      |
| GetAgentChatSessionPageListAsync    | 查询会话列表           | 用户令牌 | GET       |
| GetAgentChatSessionAsync            | 获取指定会话信息       | 用户令牌 | GET       |
| DeleteAgentChatSessionAsync         | 删除会话               | 用户令牌 | DELETE    |
| CreateAgentChatSessionAsync         | 创建会话               | 用户令牌 | POST      |
| CreateAgentChatAsync                | 发起智能体对话         | 用户令牌 | POST      |
| CreateAgentChatStreamAsync          | 发起智能体对话（SSE 流式输出） | 用户令牌 | POST      |
| GetAgentChatAsync                   | 获取对话结果           | 用户令牌 | GET       |
| GetAgentArtifactAsync               | 下载智能体产物         | 用户令牌 | GET       |
| CheckAgentVisibilityAsync           | 获取智能体可见性       | 用户令牌 | POST      |

## 函数详细内容

### 上传附件

用于上传需智能体分析的文件，上传成功后返回附件 ID；可在发起智能体对话时通过 agent_attachment_ids 引用。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateAgentAttachmentResult>?> CreateAgentAttachmentAsync(
    [Path] string agent_id,
    [FormContent] CreateAgentAttachmentRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名      | 类型                          | 必填 | 说明                                                             |
| ----------- | ----------------------------- | ---- | ---------------------------------------------------------------- |
| `agent_id`  | `string`                      | ✅   | 智能体 ID，通过智能体后台详情的地址栏中获取，示例值：`agent_4k4ue29hpwrx2` |
| `request`   | `CreateAgentAttachmentRequest` | ✅   | 上传附件请求体（multipart/form-data）                            |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "agent_attachment_id": "attachment_4k4ue29hpwrx2"
  }
}
```

**说明**：请求体按 `type` 区分附件来源：`type=image/file` 时 `file` 必传（png/jpg/pdf，文件最大 40M、图片最大 5M），此时 `doc_url` 不生效；`type=feishu_doc/bitable` 时 `doc_url` 必传，此时 `file` 不生效。以用户身份上传时，操作记录将关联到当前用户。

---

### 查询会话列表

用于查询智能体的会话列表，支持分页。

**函数签名**：

```csharp
Task<FeishuApiResult<AgentChatSessionPageListResult>?> GetAgentChatSessionPageListAsync(
    [Path] string agent_id,
    [Query("page_size")] int page_size = Consts.PageSize_20,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名        | 类型      | 必填 | 说明                                                                 |
| ------------- | --------- | ---- | -------------------------------------------------------------------- |
| `agent_id`    | `string`  | ✅   | 智能体 ID，通过智能体后台详情的地址栏中获取，示例值：`agent_4k4ue29hpwrx2` |
| `page_size`   | `int`     | ⚪   | 分页大小，默认取 `Consts.PageSize_20`，示例值：`10`                  |
| `page_token`  | `string?` | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "sessions": [
      {
        "session_id": "conversation_asdasda",
        "name": "我的会话",
        "status": "normal",
        "created_at": 1710000000000,
        "last_chat_at": 1710000000000
      }
    ],
    "has_more": false,
    "page_token": "",
    "next_page_token": ""
  }
}
```

**说明**：下一页标记取返回结果的 `Data.NextPageToken`（同时会序列化为 `page_token`）；`has_more` 为 false 时表示已到末页。以用户身份查询时，仅返回当前用户可见的会话。

---

### 获取指定会话信息

用于查询智能体某次指定会话的详细信息，包括状态、创建时间与对话轮次。

**函数签名**：

```csharp
Task<FeishuApiResult<AgentChatSessionOopsResult>?> GetAgentChatSessionAsync(
    [Path] string agent_id,
    [Path] string agent_chat_session_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                  | 类型     | 必填 | 说明                                                             |
| ----------------------- | -------- | ---- | ---------------------------------------------------------------- |
| `agent_id`              | `string` | ✅   | 智能体 ID，通过智能体后台详情的地址栏中获取，示例值：`agent_ashcascsa` |
| `agent_chat_session_id` | `string` | ✅   | 会话 ID，发起对话、创建会话或查询会话列表获取，示例值：`conversation_asdasda` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "session_id": "conversation_asdasda",
    "name": "我的会话",
    "status": "normal",
    "created_at": "1710000000000",
    "last_chat_at": "1710000000000",
    "turns": {
      "agent_chat_id": "7640186506971926032",
      "created_at": 1710000000000,
      "status": "normal"
    }
  }
}
```

**说明**：返回智能体会话详细信息，`turns` 描述该会话下的对话轮次。

---

### 删除会话

用于删除智能体的某次会话。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteAgentChatSessionAsync(
    [Path] string agent_id,
    [Path] string agent_chat_session_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                  | 类型     | 必填 | 说明                                                             |
| ----------------------- | -------- | ---- | ---------------------------------------------------------------- |
| `agent_id`              | `string` | ✅   | 智能体 ID，通过智能体后台详情的地址栏中获取，示例值：`agent_asdsad` |
| `agent_chat_session_id` | `string` | ✅   | 会话 ID，发起对话、创建会话或查询会话列表获取，示例值：`conversation_assadasd` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：删除成功时 `Data` 为空对象；删除后该会话下的历史对话不可恢复。以用户身份操作时需确保当前用户对该会话有删除权限。

---

### 创建会话

用于智能体创建空白会话。

**函数签名**：

```csharp
Task<FeishuApiResult<AgentChatSessionOopsResult>?> CreateAgentChatSessionAsync(
    [Path] string agent_id,
    [Body] CreateAgentChatSessionRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名     | 类型                            | 必填 | 说明                                                             |
| ---------- | ------------------------------- | ---- | ---------------------------------------------------------------- |
| `agent_id` | `string`                        | ✅   | 智能体 ID，通过智能体后台详情的地址栏中获取，示例值：`agent_4k4ue29hpwrx2` |
| `request`  | `CreateAgentChatSessionRequest` | ✅   | 创建会话请求体                                                   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "session_id": "conversation_asdasda",
    "name": "新建会话",
    "status": "normal"
  }
}
```

**说明**：创建成功后返回会话 ID 与会话名，可在后续发起对话时通过 `SessionId` 复用该会话进行多轮对话。以用户身份创建时，会话归属于当前用户。

---

### 发起智能体对话

异步发起一轮智能体对话，提交用户消息后立即返回对话 ID，触发智能体在后台运行；可通过获取对话结果接口轮询运行状态与回复。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateAgentChatResult>?> CreateAgentChatAsync(
    [Path] string agent_id,
    [Body] CreateAgentChatRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名     | 类型                    | 必填 | 说明                                                             |
| ---------- | ----------------------- | ---- | ---------------------------------------------------------------- |
| `agent_id` | `string`                | ✅   | 智能体 ID，通过智能体后台详情的地址栏中获取，示例值：`agent_4k4ue29hpwrx2` |
| `request`  | `CreateAgentChatRequest` | ✅   | 发起对话请求体（应保持 `Stream` 为 `false` 或 null）             |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "agent_chat_id": "7640186506971926032",
    "session_id": "conversation_asdasda"
  }
}
```

**说明**：请求体 `Stream=false`（或不传）时本接口返回 JSON；如需 SSE 流式输出请使用 `CreateAgentChatStreamAsync`。返回后智能体在后台运行，需轮询获取对话结果接口获取最终回复。

---

### 发起智能体对话（SSE 流式输出）

与 `CreateAgentChatAsync` 为同一端点，但请求体 `Stream=true`，响应为 Server-sent Events（SSE）流，超时时间 5 分钟。可在请求体传入 `SessionId` 复用既有会话进行多轮对话。

**函数签名**：

```csharp
Task<HttpResponseMessage?> CreateAgentChatStreamAsync(
    [Path] string agent_id,
    [Body] CreateAgentChatRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名     | 类型                     | 必填 | 说明                                                             |
| ---------- | ------------------------ | ---- | ---------------------------------------------------------------- |
| `agent_id` | `string`                 | ✅   | 智能体 ID，通过智能体后台详情的地址栏中获取，示例值：`agent_4k4ue29hpwrx2` |
| `request`  | `CreateAgentChatRequest` | ✅   | 发起对话请求体；调用前必须将 `Stream` 设为 `true`，否则响应不是 SSE 流 |

**响应**：Server-sent Events（SSE）流，方法返回承载该响应流的 `HttpResponseMessage`（可空签名，实际成功路径不会为 `null`）

**说明**：调用方负责读取 `Content` 流、解析 SSE 事件后释放响应。服务端返回非 2xx 状态码时由 HTTP 执行器统一抛出 `ApiException`（异常携带 `StatusCode` 与响应内容）；飞书部分业务错误以 HTTP 200 + JSON 错误体返回（如错误码 2700001 param is invalid），此时本方法会把响应原样返回，调用方需按 Content-Type 自检，详见 `documents/ErrorHandling.md`。

---

### 获取对话结果

用于获取智能体的对话回复，内容包括文字和产物等信息。

**函数签名**：

```csharp
Task<FeishuApiResult<AgentChatResult>?> GetAgentChatAsync(
    [Path] string agent_id,
    [Path] string agent_chat_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名          | 类型     | 必填 | 说明                                                             |
| --------------- | -------- | ---- | ---------------------------------------------------------------- |
| `agent_id`      | `string` | ✅   | 智能体 ID，通过智能体后台详情的地址栏中获取，示例值：`agent_4k4ue29hpwrx2` |
| `agent_chat_id` | `string` | ✅   | 智能体对话 ID，通过发起智能体对话接口获取，示例值：`7640186506971926032` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "content": [
      {
        "type": "text",
        "text": "你好，有什么可以帮你？",
        "agent_artifact_id": "",
        "artifact_type": ""
      }
    ],
    "finish_reason": "stop",
    "status": "success"
  }
}
```

**说明**：返回对话回复内容、结束原因与状态；智能体尚未结束时需轮询本接口。`content` 中的 `agent_artifact_id` 可用于调用下载智能体产物接口获取产物下载地址。

---

### 下载智能体产物

根据产物 ID（agent_artifact_id）获取该产物的下载地址及基础信息（名称、URL），用于拉取智能体会话中生成的图片、文件、云文档等产物。

**函数签名**：

```csharp
Task<FeishuApiResult<AgentArtifactOopsResult>?> GetAgentArtifactAsync(
    [Path] string agent_id,
    [Path] string agent_artifact_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名              | 类型     | 必填 | 说明                                                             |
| ------------------- | -------- | ---- | ---------------------------------------------------------------- |
| `agent_id`          | `string` | ✅   | 智能体 ID，通过智能体后台详情的地址栏中获取，示例值：`agent_4k6jukr5skfax` |
| `agent_artifact_id` | `string` | ✅   | 智能体产物 ID，调用获取对话结果接口获取，示例值：`artifact_4k6m2dbmrjeqf` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "agent_artifact": {
      "artifact_id": "artifact_4k6m2dbmrjeqf",
      "name": "分析报告.png",
      "url": "https://example.feishu.cn/artifact/xxx"
    }
  }
}
```

**说明**：返回的下载 URL 24 小时内有效，过期后需重新调用本接口获取新地址。

---

### 获取智能体可见性

查询当前调用用户对指定智能体的可见性。接口根据 UserAccessToken（用户身份凭证）解析出当前用户，结合传入的 channel_type（渠道类型），返回可见性。

**函数签名**：

```csharp
Task<FeishuApiResult<CheckAgentVisibilityResult>?> CheckAgentVisibilityAsync(
    [Path] string agent_id,
    [Body] CheckAgentVisibilityRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名     | 类型                          | 必填 | 说明                                                             |
| ---------- | ----------------------------- | ---- | ---------------------------------------------------------------- |
| `agent_id` | `string`                      | ✅   | 智能体 ID，通过智能体后台详情的地址栏中获取，示例值：`agent_4k4ue29hpwrx2` |
| `request`  | `CheckAgentVisibilityRequest` | ✅   | 获取可见性请求体（含渠道类型 `channel_type`）                    |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "visibility": true
  }
}
```

**说明**：仅支持 user_access_token（用户身份凭证），不支持 tenant_access_token，因此该方法只存在于用户令牌接口；典型用于 WebSDK 场景，前端据此决定是否向当前用户展示该智能体。
