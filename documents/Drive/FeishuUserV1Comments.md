# 云文档评论 - 用户权限（FeishuUserV1Comments）

## 接口名称
**云文档评论（用户权限）** -（`IFeishuUserV1Comments`）

## 功能描述
提供以用户身份管理飞书云文档评论的能力。飞书在线文档中的评论是文件、文件夹、文档、电子表格、多维表格、思维笔记、知识库中的文档的评论。支持获取评论列表、批量查询评论、添加评论、获取评论详情、解决/恢复评论、添加回复、获取回复列表、更新回复、删除回复以及添加/取消表情回应等操作。适用于需要以用户身份管理云文档评论的业务场景，如用户个人评论管理、评论互动等。

## 参考文档
- [评论概述 - 飞书开放平台](https://open.feishu.cn/document/server-docs/docs/CommentAPI/list)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| GetCommentsPageListAsync | 获取云文档所有评论 | 用户令牌 | GET |
| BatchQueryFileCommentAsync | 批量获取评论 | 用户令牌 | POST |
| PatchFileCommentAsync | 解决/恢复评论 | 用户令牌 | PATCH |
| CreateFileCommentAsync | 添加全文评论 | 用户令牌 | POST |
| GetFileCommentAsync | 获取评论详情 | 用户令牌 | GET |
| CreateFileCommentReplyAsync | 添加回复 | 用户令牌 | POST |
| GetFileCommentRepliesPageListAsync | 分页获取回复信息 | 用户令牌 | GET |
| UpdateFileCommentReplyAsync | 更新回复的内容 | 用户令牌 | PUT |
| DeleteFileCommentReplyAsync | 删除回复 | 用户令牌 | DELETE |
| UpdateReactionCommentReactionAsync | 添加/取消表情回应 | 用户令牌 | POST |

## 函数详细内容

### 获取云文档所有评论

**函数签名**：
```csharp
Task<FeishuApiPageListResult<FileComment>?> GetCommentsPageListAsync(
    [Path] string file_token,
    [Query] string file_type,
    [Query] bool? is_whole = false,
    [Query] bool? is_solved = false,
    [Query] bool? need_reaction = false,
    [Query] int page_size = Consts.PageSize_50,
    [Query] string? page_token = null,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件的 token，示例值：`XIHSdYSI7oMEU1xrsnxc8fabcef` |
| `file_type` | `string` | ✅ | 云文档类型，可选值：`doc`（旧版文档）、`docx`（新版文档）、`sheet`（电子表格）、`file`（文件）、`slides`（幻灯片），示例值：`doc` |
| `is_whole` | `bool?` | ⚪ | 是否全文评论，默认值：`false` |
| `is_solved` | `bool?` | ⚪ | 是否已解决，默认值：`false` |
| `need_reaction` | `bool?` | ⚪ | 是否需要获取评论卡片上挂载的 Reaction 数据，默认值：`false` |
| `page_size` | `int` | ⚪ | 分页大小，默认值：50 |
| `page_token` | `string?` | ⚪ | 分页标记，第一次请求不填 |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "comment_id": "6916106822734578184",
        "user_id": "ou_xxxxxx",
        "is_whole": true,
        "is_solved": false,
        "reply_list": []
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**：用于根据云文档 Token 分页获取文档所有评论信息，包括评论和回复 ID、回复的内容、评论人和回复人的用户 ID 等。该接口支持返回全局评论以及局部评论，可通过 `is_whole` 字段区分。默认每页返回 50 个评论。

---

### 批量获取评论

**函数签名**：
```csharp
Task<FeishuApiResult<BatchQueryFileCommentResponse>?> BatchQueryFileCommentAsync(
    [Path] string file_token,
    [Query] string file_type,
    [Body] BatchQueryFileCommentRequest queryFileCommentRequest,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件的 token，示例值：`XIHSdYSI7oMEU1xrsnxc8fabcef` |
| `file_type` | `string` | ✅ | 云文档类型，可选值：`doc`、`docx`、`sheet`、`file`、`slides`，示例值：`doc` |
| `queryFileCommentRequest` | `BatchQueryFileCommentRequest` | ✅ | 批量查询评论请求对象 |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "comments": [
      {
        "comment_id": "6916106822734578184",
        "user_id": "ou_xxxxxx",
        "is_whole": true,
        "is_solved": false
      }
    ]
  }
}
```

**说明**：用于根据评论 ID 列表批量获取云文档评论信息，包括评论和回复 ID、回复的内容、评论人和回复人的用户 ID 等。支持返回全局评论以及局部评论，可通过 `is_whole` 字段区分。

---

### 解决/恢复评论

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> PatchFileCommentAsync(
    [Path] string file_token,
    [Path] string comment_id,
    [Query] string file_type,
    [Body] PatchFileCommentRequest patchFileCommentRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件的 token，示例值：`XIHSdYSI7oMEU1xrsnxc8fabcef` |
| `comment_id` | `string` | ✅ | 评论 ID，示例值：`6916106822734578184` |
| `file_type` | `string` | ✅ | 云文档类型，可选值：`doc`、`docx`、`sheet`、`file`、`slides`，示例值：`doc` |
| `patchFileCommentRequest` | `PatchFileCommentRequest` | ✅ | 解决/恢复评论请求体 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：用于解决或恢复云文档中的评论。

---

### 添加全文评论

**函数签名**：
```csharp
Task<FeishuApiResult<CreateFileCommentResult>?> CreateFileCommentAsync(
    [Path] string file_token,
    [Query] string file_type,
    [Body] CreateFileCommentRequest createFileCommentRequest,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件的 token，示例值：`XIHSdYSI7oMEU1xrsnxc8fabcef` |
| `file_type` | `string` | ✅ | 云文档类型，可选值：`doc`、`docx`、`sheet`、`file`、`slides`，示例值：`doc` |
| `createFileCommentRequest` | `CreateFileCommentRequest` | ✅ | 添加全文评论请求体 |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "comment_id": "6916106822734578184"
  }
}
```

**说明**：在文档中添加一条全局评论，不支持局部评论。

---

### 获取评论详情

**函数签名**：
```csharp
Task<FeishuApiResult<FileComment>?> GetFileCommentAsync(
    [Path] string file_token,
    [Path] string comment_id,
    [Query] string file_type,
    [Query] bool? need_reaction = false,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件的 token，示例值：`XIHSdYSI7oMEU1xrsnxc8fabcef` |
| `comment_id` | `string` | ✅ | 评论 ID，示例值：`6916106822734578184` |
| `file_type` | `string` | ✅ | 云文档类型，可选值：`doc`、`docx`、`sheet`、`file`、`slides`，示例值：`doc` |
| `need_reaction` | `bool?` | ⚪ | 是否需要获取评论卡片上挂载的 Reaction 数据，默认值：`false` |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "comment_id": "6916106822734578184",
    "user_id": "ou_xxxxxx",
    "is_whole": true,
    "is_solved": false,
    "reply_list": []
  }
}
```

**说明**：根据评论 ID 获取云文档中某条评论的详细信息。

---

### 添加回复

**函数签名**：
```csharp
Task<FeishuApiResult<FileCommentReply>?> CreateFileCommentReplyAsync(
    [Path] string file_token,
    [Path] string comment_id,
    [Query] string file_type,
    [Body] CreateFileCommentReplyRequest createFileCommentReplyRequest,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件的 token，示例值：`XIHSdYSI7oMEU1xrsnxc8fabcef` |
| `comment_id` | `string` | ✅ | 评论 ID，示例值：`6916106822734578184` |
| `file_type` | `string` | ✅ | 云文档类型，可选值：`doc`、`docx`、`sheet`、`file`、`slides`，示例值：`doc` |
| `createFileCommentReplyRequest` | `CreateFileCommentReplyRequest` | ✅ | 添加评论回复请求体 |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "reply_id": "6916106822734594568",
    "user_id": "ou_xxxxxx",
    "content": "回复内容"
  }
}
```

**说明**：对云文档中的某条评论进行回复，回复内容支持普通文本、云文档链接等。

---

### 分页获取回复信息

**函数签名**：
```csharp
Task<FeishuApiPageListResult<FileCommentReply>?> GetFileCommentRepliesPageListAsync(
    [Path] string file_token,
    [Path] string comment_id,
    [Query] string file_type,
    [Query] bool? need_reaction = false,
    [Query] int page_size = Consts.PageSize_50,
    [Query] string? page_token = null,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件的 token，示例值：`XIHSdYSI7oMEU1xrsnxc8fabcef` |
| `comment_id` | `string` | ✅ | 评论 ID，示例值：`6916106822734578184` |
| `file_type` | `string` | ✅ | 云文档类型，可选值：`doc`、`docx`、`sheet`、`file`、`slides`，示例值：`doc` |
| `need_reaction` | `bool?` | ⚪ | 是否需要获取评论卡片上挂载的 Reaction 数据，默认值：`false` |
| `page_size` | `int` | ⚪ | 分页大小，默认值：50 |
| `page_token` | `string?` | ⚪ | 分页标记，第一次请求不填 |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "reply_id": "6916106822734594568",
        "user_id": "ou_xxxxxx",
        "content": "回复内容"
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**：用于根据评论 ID，获取该条评论对应的回复信息列表，包括回复 ID、回复内容、回复人的用户 ID 等。

---

### 更新回复的内容

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UpdateFileCommentReplyAsync(
    [Path] string file_token,
    [Path] string comment_id,
    [Path] string reply_id,
    [Query] string file_type,
    [Body] UpdateFileCommentReplyRequest updateFileCommentReplyRequest,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件的 token，示例值：`XIHSdYSI7oMEU1xrsnxc8fabcef` |
| `comment_id` | `string` | ✅ | 评论 ID，示例值：`6916106822734578184` |
| `reply_id` | `string` | ✅ | 回复 ID，示例值：`6916106822734594568` |
| `file_type` | `string` | ✅ | 云文档类型，可选值：`doc`、`docx`、`sheet`、`file`、`slides`，示例值：`doc` |
| `updateFileCommentReplyRequest` | `UpdateFileCommentReplyRequest` | ✅ | 更新文件评论回复请求 |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：更新云文档中的某条回复的内容。

---

### 删除回复

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteFileCommentReplyAsync(
    [Path] string file_token,
    [Path] string comment_id,
    [Path] string reply_id,
    [Query] string file_type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件的 token，示例值：`XIHSdYSI7oMEU1xrsnxc8fabcef` |
| `comment_id` | `string` | ✅ | 评论 ID，示例值：`6916106822734578184` |
| `reply_id` | `string` | ✅ | 回复 ID，示例值：`6916106822734594568` |
| `file_type` | `string` | ✅ | 云文档类型，可选值：`doc`、`docx`、`sheet`、`file`、`slides`，示例值：`doc` |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除云文档中的某条回复。

---

### 添加/取消表情回应

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UpdateReactionCommentReactionAsync(
    [Path] string file_token,
    [Query] string file_type,
    [Body] UpdateReactionCommentReactionRequest updateReactionCommentReactionRequest,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件的 token，示例值：`XIHSdYSI7oMEU1xrsnxc8fabcef` |
| `file_type` | `string` | ✅ | 云文档类型，可选值：`doc`、`docx`、`sheet`、`file`、`slides`，示例值：`doc` |
| `updateReactionCommentReactionRequest` | `UpdateReactionCommentReactionRequest` | ✅ | 添加/取消表情回应请求体 |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：对云文档中的某条评论进行 emoji 表情回应或取消 emoji 表情回应。适用于用户需要对云文档评论进行 emoji 表情互动的场景。
