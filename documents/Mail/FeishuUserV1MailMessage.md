---
title: 邮箱邮件接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份管理自己的邮箱邮件，支持邮件的查询、修改、删除，以及邮件发送。
---

# IFeishuUserV1MailMessage - 用户邮箱邮件API

## 功能描述
飞书邮箱邮件API接口实现了修改、查询、删除等邮箱邮件管理功能。支持用户通过用户访问令牌管理自己的邮箱邮件，同时支持发送邮件功能。

## 参考文档
- [批量删除邮件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/batch_trash)
- [批量修改邮件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/batch_modify)
- [删除邮件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/trash)
- [修改邮件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/modify)
- [批量获取邮件详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/batch_get)
- [查询会话下邮件信息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/list_thread_message)
- [获取邮件卡片的邮件列表](https://open.feishu.cn/document/mail-v1/user_mailbox-message/get_by_card)
- [分页列出邮件](https://open.feishu.cn/document/mail-v1/user_mailbox-message/list)
- [获取邮件详情](https://open.feishu.cn/document/mail-v1/user_mailbox-message/get)
- [获取邮件内附件的下载链接](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message-attachment/download_url)
- [撤回已发送的邮件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-sent_message/recall)
- [查询已发送邮件的撤回详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-sent_message/get_recall_detail)
- [发送邮件](https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-message/send)
- [查询已发送邮件的投递状态](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/send_status)
- [搜索邮件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox/search)
- [取消定时发送](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/cancel_scheduled_send)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 | 接口文档 |
| :--- | :--- | :--- | :--- |----------|
| BatchTrashUserMailboxMessageAsync | 批量删除邮件 | UserAccessToken | POST | [BatchTrashUserMailboxMessageAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/batch_trash) |
| BatchModifyUserMailboxMessageAsync | 批量修改邮件 | UserAccessToken | POST | [BatchModifyUserMailboxMessageAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/batch_modify) |
| DeleteUserMailboxMessageAsync | 删除邮件 | UserAccessToken | POST | [DeleteUserMailboxMessageAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/trash) |
| ModifyUserMailboxMessageAsync | 修改邮件 | UserAccessToken | PUT | [ModifyUserMailboxMessageAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/modify) |
| BatchGetUserMailboxMessageAsync | 批量获取邮件详情 | UserAccessToken | POST | [BatchGetUserMailboxMessageAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/batch_get) |
| GetThreadMessageUserMailboxMessageAsync | 查询会话下邮件信息 | UserAccessToken | GET | [GetThreadMessageUserMailboxMessageAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/list_thread_message) |
| GetByCardUserMailboxMessageAsync | 获取邮件卡片的邮件列表 | UserAccessToken | GET | [GetByCardUserMailboxMessageAsync](https://open.feishu.cn/document/mail-v1/user_mailbox-message/get_by_card) |
| GetUserMailboxMessagePageListAsync | 分页列出邮件 | UserAccessToken | GET | [GetUserMailboxMessagePageListAsync](https://open.feishu.cn/document/mail-v1/user_mailbox-message/list) |
| GetUserMailboxMessageAsync | 获取邮件详情 | UserAccessToken | GET | [GetUserMailboxMessageAsync](https://open.feishu.cn/document/mail-v1/user_mailbox-message/get) |
| GetMessageAttachmentDownloadUrlAsync | 获取邮件内附件的下载链接 | UserAccessToken | GET | [GetMessageAttachmentDownloadUrlAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message-attachment/download_url) |
| RecallUserMailboxMessageAsync | 撤回已发送的邮件 | UserAccessToken | POST | [RecallUserMailboxMessageAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-sent_message/recall) |
| GetUserMailboxMessageRecallDetailAsync | 查询已发送邮件的撤回详情 | UserAccessToken | GET | [GetUserMailboxMessageRecallDetailAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-sent_message/get_recall_detail) |
| SendUserMailboxMessageAsync | 发送邮件 | UserAccessToken | POST | [SendUserMailboxMessageAsync](https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-message/send) |
| GetUserMailboxMessageSendStatusAsync | 查询已发送邮件的投递状态 | UserAccessToken | GET | [GetUserMailboxMessageSendStatusAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/send_status) |
| SearchUserMailboxMessageAsync | 搜索邮件 | UserAccessToken | POST | [SearchUserMailboxMessageAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox/search) |
| CancelScheduledSendUserMailboxMessageAsync | 取消定时发送 | UserAccessToken | POST | [CancelScheduledSendUserMailboxMessageAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/cancel_scheduled_send) |

## 函数详细内容

### BatchTrashUserMailboxMessageAsync
批量删除邮件

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> BatchTrashUserMailboxMessageAsync(
    [Path] string user_mailbox_id,
    [Body] BatchTrashUserMailboxMessageRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| request | BatchTrashUserMailboxMessageRequest | ✅ | 批量删除用户邮箱邮件请求对象 | - |
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
- 批量将邮件移动到已删除文件夹
- 使用 user_access_token 时，只能操作当前授权用户的邮箱邮件

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var request = new BatchTrashUserMailboxMessageRequest
{
    MessageIds = new[] { "msg_id_1", "msg_id_2" }
};
var result = await messageApi.BatchTrashUserMailboxMessageAsync("me", request);
Console.WriteLine($"批量删除结果: {result.Code == 0}");
```

---

### BatchModifyUserMailboxMessageAsync
批量修改邮件

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> BatchModifyUserMailboxMessageAsync(
    [Path] string user_mailbox_id,
    [Body] BatchModifyUserMailboxMessageRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| request | BatchModifyUserMailboxMessageRequest | ✅ | 批量修改用户邮箱邮件请求对象 | - |
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
- 批量修改邮件标签、所属文件夹、已读未读状态，可进行加旗标、归档、移至垃圾邮件等操作
- 不支持移入邮件进入已删除文件夹，如需，请使用批量删除邮件接口
- 使用 user_access_token 时，只能操作当前授权用户的邮箱邮件

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var request = new BatchModifyUserMailboxMessageRequest
{
    MessageIds = new[] { "msg_id_1", "msg_id_2" },
    AddLabelIds = new[] { "FLAGGED" }
};
var result = await messageApi.BatchModifyUserMailboxMessageAsync("me", request);
Console.WriteLine($"批量修改结果: {result.Code == 0}");
```

---

### DeleteUserMailboxMessageAsync
删除邮件

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteUserMailboxMessageAsync(
    [Path] string user_mailbox_id,
    [Path] string message_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| message_id | string | ✅ | 邮件ID，可通过列出邮件接口获得 | NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ== |
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
- 移动邮件到已删除文件夹
- 使用 user_access_token 时，只能操作当前授权用户的邮箱邮件

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var result = await messageApi.DeleteUserMailboxMessageAsync("me", "msg_id_123");
Console.WriteLine($"邮件删除结果: {result.Code == 0}");
```

---

### ModifyUserMailboxMessageAsync
修改邮件

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> ModifyUserMailboxMessageAsync(
    [Path] string user_mailbox_id,
    [Path] string message_id,
    [Body] ModifyUserMailboxMessageRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| message_id | string | ✅ | 邮件ID，可通过列出邮件接口获得 | NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ== |
| request | ModifyUserMailboxMessageRequest | ✅ | 修改用户邮箱邮件请求对象 | - |
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
- 修改邮件标签、所属文件夹、已读未读状态，可为邮件添加旗标、归档、移入垃圾邮件等操作
- 不支持移动邮件到已删除文件夹，如需，请使用删除邮件接口
- 使用 user_access_token 时，只能操作当前授权用户的邮箱邮件

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var request = new ModifyUserMailboxMessageRequest
{
    AddLabelIds = new[] { "FLAGGED" },
    AddFolder = "INBOX"
};
var result = await messageApi.ModifyUserMailboxMessageAsync("me", "msg_id_123", request);
Console.WriteLine($"邮件修改结果: {result.Code == 0}");
```

---

### BatchGetUserMailboxMessageAsync
批量获取邮件详情

**函数签名**
```csharp
Task<FeishuApiResult<BatchGetUserMailboxMessageResult>?> BatchGetUserMailboxMessageAsync(
    [Path] string user_mailbox_id,
    [Body] BatchGetUserMailboxMessageRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| request | BatchGetUserMailboxMessageRequest | ✅ | 批量获取用户邮箱邮件请求对象 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "messages": [
      {
        "message_id": "NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ==",
        "subject": "邮件主题",
        "head_from": { "mail_address": "sender@example.com", "name": "发件人" },
        "to": [{ "mail_address": "recipient@example.com", "name": "收件人" }],
        "internal_date": "1682377086000"
      }
    ]
  }
}
```

**说明**
- 通过指定邮件ID，获取对应邮件的标签、文件夹、摘要、正文、html、附件等信息
- 使用 user_access_token 时，只能获取当前授权用户的邮箱邮件详情

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var request = new BatchGetUserMailboxMessageRequest
{
    MessageIds = new[] { "msg_id_1", "msg_id_2" }
};
var result = await messageApi.BatchGetUserMailboxMessageAsync("me", request);
if (result?.Data?.Messages != null)
{
    foreach (var msg in result.Data.Messages)
    {
        Console.WriteLine($"邮件: {msg.Subject}");
    }
}
```

---

### GetThreadMessageUserMailboxMessageAsync
查询会话下邮件信息

**函数签名**
```csharp
Task<FeishuApiResult<GetThreadMessageUserMailboxMessageResult>?> GetThreadMessageUserMailboxMessageAsync(
    [Path] string user_mailbox_id,
    [Path] string thread_id,
    [Query] string? format = null,
    [Query] string? include_spam_trash = null,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| thread_id | string | ✅ | 邮件会话ID | xxxxxxxxxxxx |
| format | string? | ⚪ | 需要获取的邮件内容。支持full/plain_text_full/metadata | full |
| include_spam_trash | string? | ⚪ | 是否包含垃圾邮件和已删除邮件 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "message_id": "NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ==",
        "thread_id": "thread_id_123",
        "folder_id": "INBOX",
        "internal_date": "1682377086000",
        "message_state": 1
      }
    ]
  }
}
```

**说明**
- 通过用户邮箱地址和邮件会话ID，获取该会话下的所有邮件关键信息列表
- 使用 user_access_token 时，只能获取当前授权用户的邮箱会话邮件

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var result = await messageApi.GetThreadMessageUserMailboxMessageAsync("me", "thread_id_123", format: "full");
if (result?.Data?.Items != null)
{
    foreach (var msg in result.Data.Items)
    {
        Console.WriteLine($"会话邮件: {msg.Message?.Subject}");
    }
}
```

---

### GetByCardUserMailboxMessageAsync
获取邮件卡片的邮件列表

**函数签名**
```csharp
Task<FeishuApiResult<GetByCardUserMailboxMessageResult>?> GetByCardUserMailboxMessageAsync(
    [Path] string user_mailbox_id,
    [Query] string card_id,
    [Query] string owner_id,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| card_id | string | ✅ | 邮件卡片ID | 512ca581-6059-4449-8150-5522e6641d32 |
| owner_id | string | ✅ | 邮件卡片Owner ID | 1234567890 |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "owner_info": {
      "type": "user",
      "owner_user_id": "1234567890"
    },
    "message_ids": ["NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ=="],
    "card_id": "512ca581-6059-4449-8150-5522e6641d32"
  }
}
```

**说明**
- 获取邮件卡片关联的邮件列表
- 可通过接收消息事件的推送获取卡片ID
- 使用 user_access_token 时，只能获取当前授权用户的邮箱卡片邮件

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var result = await messageApi.GetByCardUserMailboxMessageAsync(
    "me",
    "512ca581-6059-4449-8150-5522e6641d32",
    "1234567890");
foreach (var msgId in result?.Data?.MessageIds ?? [])
{
    Console.WriteLine($"卡片邮件ID: {msgId}");
}
```

---

### GetUserMailboxMessagePageListAsync
分页列出邮件

**函数签名**
```csharp
Task<FeishuApiPageListResult<string>?> GetUserMailboxMessagePageListAsync(
    [Path] string user_mailbox_id,
    [Query] string? folder_id = null,
    [Query] bool? only_unread = null,
    [Query] string? label_id = null,
    [Query] int page_size = Consts.PageSize_20,
    [Query] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| folder_id | string? | ⚪ | 文件夹id | INBOX 或者用户文件夹id |
| only_unread | bool? | ⚪ | 是否只查询未读邮件 | true |
| label_id | string? | ⚪ | 标签id，支持IMPORTANT、OTHER、FLAGGED、SCHEDULED以及自定义文件夹标签 | FLAGGED |
| page_size | int | ⚪ | 分页大小，默认值：20 | 20 |
| page_token | string? | ⚪ | 分页标记 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": ["msg_id_1", "msg_id_2"],
    "page_token": "evt_xxx",
    "has_more": true
  }
}
```

**说明**
- 分页列出邮件
- 返回邮件ID列表，可通过批量获取邮件详情接口获取详细信息
- 使用 user_access_token 时，只能列出当前授权用户的邮箱邮件

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var result = await messageApi.GetUserMailboxMessagePageListAsync("me", only_unread: true);
if (result?.Data?.Items != null)
{
    foreach (var msgId in result.Data.Items)
    {
        Console.WriteLine($"邮件ID: {msgId}");
    }
}
```

---

### GetUserMailboxMessageAsync
获取邮件详情

**函数签名**
```csharp
Task<FeishuApiResult<GetUserMailboxMessageResult>?> GetUserMailboxMessageAsync(
    [Path] string user_mailbox_id,
    [Path] string message_id,
    [Query] string? format = null,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| message_id | string | ✅ | 邮件ID，可通过列出邮件接口获得 | NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ== |
| format | string? | ⚪ | 需要获取的邮件内容。支持full/plain_text_full/metadata | full |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "message": {
      "message_id": "NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ==",
      "subject": "邮件主题",
      "head_from": { "mail_address": "sender@example.com", "name": "发件人" },
      "to": [{ "mail_address": "recipient@example.com", "name": "收件人" }],
      "body_html": "5b+76K+H5paH5Lu25pON5L2c",
      "body_calendar": "QkVHSU46VkNBTEVOREFS",
      "internal_date": "1682377086000"
    }
  }
}
```

**说明**
- 获取邮件详情
- 可通过format参数控制返回内容的详细程度
- `body_calendar`（日历邀请正文）需具备字段权限：获取邮件正文(mail:user_mailbox.message.body:read)
- 使用 user_access_token 时，只能获取当前授权用户的邮箱邮件详情

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var result = await messageApi.GetUserMailboxMessageAsync("me", "msg_id_123", format: "full");
Console.WriteLine($"邮件主题: {result?.Data?.Message?.Subject}");
Console.WriteLine($"发件人: {result?.Data?.Message?.HeadFrom?.Name}");
```

---

### GetMessageAttachmentDownloadUrlAsync
获取邮件内附件的下载链接

**函数签名**
```csharp
Task<FeishuApiResult<AttachmentDownloadUrlResult>?> GetMessageAttachmentDownloadUrlAsync(
     [Path] string user_mailbox_id,
     [Path] string message_id,
     [Query("attachment_ids")] string? attachment_ids = null,
     CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| message_id | string | ✅ | 邮件 ID | NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ== |
| attachment_ids | string? | ⚪ | 待获取下载链接的附件 ID 列表，多个 ID 以逗号分隔 | att_001,att_002 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "download_urls": [
      {
        "attachment_id": "att_001",
        "download_url": "https://api-drive-stream.feishu.cn/space/api/box/stream/download/authcode/?code=xxxx"
      }
    ],
    "failed_ids": ["att_002"]
  }
}
```

**说明**
- 获取指定邮件内附件的有效下载链接，链接有时效性，过期后需重新获取
- 附件 ID 可通过获取邮件详情接口返回的 attachments 字段中的 id 获取
- `failed_ids` 返回获取失败的附件 ID 列表
- 使用 user_access_token 时，只能获取当前授权用户的邮箱邮件附件

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var result = await messageApi.GetMessageAttachmentDownloadUrlAsync(
    "me",
    "msg_id_123",
    "att_001,att_002");
if (result?.Data?.DownloadUrls != null)
{
    foreach (var item in result.Data.DownloadUrls)
    {
        Console.WriteLine($"附件 {item.AttachmentId} 下载链接: {item.DownloadUrl}");
    }
}
```

---

### RecallUserMailboxMessageAsync
撤回已发送的邮件

**函数签名**
```csharp
Task<FeishuApiResult<RecallMessageResult>?> RecallUserMailboxMessageAsync(
    [Path] string user_mailbox_id,
    [Path] string message_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| message_id | string | ✅ | 待撤回的邮件 ID | NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ== |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "recall_status": "unavailable",
    "recall_restriction_reason": "recall time exceeded"
  }
}
```

**说明**
- 仅可撤回当前授权用户已发送且在可撤回时间窗口内的邮件
- `recall_status` 表示撤回任务状态；不允许撤回时通过 `recall_restriction_reason` 返回原因
- 每个收件人的撤回结果可通过查询已发送邮件的撤回详情接口获取

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var result = await messageApi.RecallUserMailboxMessageAsync("me", "msg_id_123");
Console.WriteLine($"撤回状态: {result?.Data?.RecallStatus}");
```

---

### GetUserMailboxMessageRecallDetailAsync
查询已发送邮件的撤回详情

**函数签名**
```csharp
Task<FeishuApiResult<RecallMessageDetailResult>?> GetUserMailboxMessageRecallDetailAsync(
    [Path] string user_mailbox_id,
    [Path] string message_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| message_id | string | ✅ | 待查询撤回详情的邮件 ID | NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ== |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "recall_status": "success",
    "recall_result": "part_success",
    "success_count": 2,
    "failure_count": 1,
    "processing_count": 0,
    "items": [
      {
        "recipient_address": "recipient@example.com",
        "recipient_name": "收件人",
        "status": "success",
        "fail_reason": "",
        "is_mailing_list": false
      }
    ]
  }
}
```

**说明**
- 查询当前授权用户已发送邮件的每个收件人撤回结果，包括成功、失败与处理中的收件人数
- 收件人为邮件组地址时，`is_mailing_list` 为 true，并返回组内成功/失败人数

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var result = await messageApi.GetUserMailboxMessageRecallDetailAsync("me", "msg_id_123");
Console.WriteLine($"撤回状态: {result?.Data?.RecallStatus}");
Console.WriteLine($"成功: {result?.Data?.SuccessCount}, 失败: {result?.Data?.FailureCount}");
```

---

### SendUserMailboxMessageAsync
发送邮件

**函数签名**
```csharp
Task<FeishuApiResult<SendUserMailboxMessageResult>?> SendUserMailboxMessageAsync(
    [Path] string user_mailbox_id,
    [Body] SendUserMailboxMessageRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| request | SendUserMailboxMessageRequest | ✅ | 发送用户邮箱邮件请求对象 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "message_id": "NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ==",
    "thread_id": "thread_id_123"
  }
}
```

**说明**
- 发送邮件使用 base64url 编码。与普通 base64 的区别是将「+/」替换为「-_」
- 可通过 `raw` 传入完整 EML，或通过 subject/to/body_html 等结构化字段构造邮件
- 使用 user_access_token 时，只能从当前授权用户的主邮箱发送邮件

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var request = new SendUserMailboxMessageRequest
{
    Subject = "邮件主题",
    Tos = new MailAddress[] { new MailAddress { MailAddressSuffix = "recipient@example.com", Name = "收件人" } },
    BodyHtml = "<p>邮件正文内容</p>"
};
var result = await messageApi.SendUserMailboxMessageAsync("me", request);
Console.WriteLine($"邮件发送成功: {result?.Data?.MessageId}");
```

---

### GetUserMailboxMessageSendStatusAsync
查询已发送邮件的投递状态

**函数签名**
```csharp
Task<FeishuApiResult<MessageSendStatusResult>?> GetUserMailboxMessageSendStatusAsync(
     [Path] string user_mailbox_id,
     [Path] string message_id,
     CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| message_id | string | ✅ | 待查询的邮件 ID | NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ== |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "message_id": "NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ==",
    "details": [
      {
        "recipient": {
          "email_address": "recipient@example.com",
          "name": "收件人"
        },
        "status": 2,
        "last_updated_time": 1717382400
      }
    ]
  }
}
```

**说明**
- 返回已发送邮件中每个收件人的投递状态
- `last_updated_time` 为最后更新时间（Unix 时间戳，秒）

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var result = await messageApi.GetUserMailboxMessageSendStatusAsync("me", "msg_id_123");
if (result?.Data?.Details != null)
{
    foreach (var detail in result.Data.Details)
    {
        Console.WriteLine($"收件人 {detail.Recipient?.EmailAddress} 投递状态: {detail.Status}");
    }
}
```

---

### SearchUserMailboxMessageAsync
搜索邮件

**函数签名**
```csharp
Task<FeishuApiResult<SearchUserMailboxMessageResult>?> SearchUserMailboxMessageAsync(
    [Path] string user_mailbox_id,
    [Body] SearchUserMailboxMessageRequest request,
    [Query] int page_size = Consts.PageSize_15,
    [Query] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| request | SearchUserMailboxMessageRequest | ✅ | 搜索请求体：`query` 搜索关键词、`filter` 过滤条件（from/to/cc/bcc/subject/folder/label/has_attachment/is_unread/create_time） | - |
| page_size | int | ⚪ | 单次返回的搜索结果条数，默认值：15，取值范围 1 ～ 15 | 15 |
| page_token | string? | ⚪ | 分页标记，第一次请求不填，表示从头开始遍历 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "id": "msg_id_123",
        "display_info": "会议通知",
        "meta_data": {
          "title": "会议通知",
          "thread_id": "thread_id_123",
          "create_time": "2026-06-03T11:41:00+08:00",
          "message_biz_id": "NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ==",
          "from": { "mail_address": "sender@example.com", "name": "发件人" }
        }
      }
    ],
    "total": 99,
    "has_more": true,
    "page_token": "eVQrYzJBNDNONlk4VFZBZVlSdzlKdFJ4bVVHVExENDNKVHoxaVdiVnViQT0=",
    "notice": ""
  }
}
```

**说明**
- 支持关键词与发件人、收件人、文件夹、时间范围等多维过滤条件组合查询
- 返回 `page_token` 需在下次请求中回传以继续翻页
- `notice` 为服务端提示信息（如 query 被截断）

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var request = new SearchUserMailboxMessageRequest
{
    Query = "会议通知",
    Filter = new MailSearchFilter { IsUnread = true }
};
var result = await messageApi.SearchUserMailboxMessageAsync("me", request);
if (result?.Data?.Items != null)
{
    foreach (var item in result.Data.Items)
    {
        Console.WriteLine($"搜索结果: {item.MetaData?.Title}");
    }
}
```

---

### CancelScheduledSendUserMailboxMessageAsync
取消定时发送

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> CancelScheduledSendUserMailboxMessageAsync(
    [Path] string user_mailbox_id,
    [Path] string message_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | aba@aac.com |
| message_id | string | ✅ | 已设置定时发送的邮件 ID | 268dce11-85f7-427d-8756-6be3abc850fd |
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
- 取消定时发送的邮件，被取消的邮件将变成草稿
- 仅对已设置定时发送且尚未实际发出的邮件有效

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuUserV1MailMessage>();
var result = await messageApi.CancelScheduledSendUserMailboxMessageAsync("me", "msg_id_123");
Console.WriteLine($"取消定时发送结果: {result.Code == 0}");
```
