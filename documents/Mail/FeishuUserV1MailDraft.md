# IFeishuUserV1MailDraft - 用户邮箱草稿API

## 功能描述
飞书邮箱草稿API接口实现了修改、查询、删除等邮件草稿功能。支持用户通过用户访问令牌管理自己的邮件草稿。

## 参考文档
- [更新草稿](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/update)
- [发送草稿](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/send)
- [列出草稿列表](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/list)
- [获取草稿内容](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/get)
- [删除草稿](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/delete)
- [创建草稿](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/create)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| UpdateUserMailboxDraftAsync | 更新草稿 | UserAccessToken | PUT |
| SendUserMailboxDraftAsync | 发送草稿 | UserAccessToken | POST |
| GetUserMailboxDraftPageListAsync | 分页列出草稿列表 | UserAccessToken | GET |
| GetUserMailboxDraftAsync | 获取草稿内容 | UserAccessToken | GET |
| DeleteUserMailboxDraftAsync | 删除草稿 | UserAccessToken | DELETE |
| CreateUserMailboxDraftAsync | 创建草稿 | UserAccessToken | POST |

## 函数详细内容

### UpdateUserMailboxDraftAsync
更新草稿内容

**函数签名**
```csharp
Task<FeishuApiResult<UserMailboxDraftOopsResult>?> UpdateUserMailboxDraftAsync(
    [Path] string user_mailbox_id,
    [Path] string draft_id,
    [Body] UserMailboxDraftRequest updateUserMailboxDraftRequest,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| draft_id | string | ✅ | 草稿ID，可通过创建草稿或列出草稿接口获得 | 268dce11-85f7-427d-8756-6be3abc850fd |
| updateUserMailboxDraftRequest | UserMailboxDraftRequest | ✅ | 更新草稿请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "draft_id": "268dce11-85f7-427d-8756-6be3abc850fd",
    "update_time": "2026-06-03T11:34:00+08:00"
  }
}
```

**说明**
- 支持更新草稿的主题、收件人、正文等内容
- 更新后草稿状态保持不变

**代码示例**
```csharp
var draftApi = feishuApp.GetApi<IFeishuUserV1MailDraft>();
var request = new UserMailboxDraftRequest
{
    Subject = "更新后的邮件主题",
    ToRecipients = new List<string> { "recipient@example.com" }
};
var result = await draftApi.UpdateUserMailboxDraftAsync("me", "draft_id_123", request);
Console.WriteLine($"草稿更新成功: {result?.Data?.DraftId}");
```

---

### SendUserMailboxDraftAsync
发送草稿

**函数签名**
```csharp
Task<FeishuApiResult<SendUserMailboxDraftResult>?> SendUserMailboxDraftAsync(
    [Path] string user_mailbox_id,
    [Path] string draft_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| draft_id | string | ✅ | 草稿ID，可通过创建草稿或列出草稿接口获得 | 268dce11-85f7-427d-8756-6be3abc850fd |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "message_id": "msg_xxxxxxxx",
    "send_time": "2026-06-03T11:34:00+08:00"
  }
}
```

**说明**
- 将指定的草稿发送出去
- 发送成功后草稿将被删除

**代码示例**
```csharp
var draftApi = feishuApp.GetApi<IFeishuUserV1MailDraft>();
var result = await draftApi.SendUserMailboxDraftAsync("me", "draft_id_123");
Console.WriteLine($"草稿发送成功: {result?.Data?.MessageId}");
```

---

### GetUserMailboxDraftPageListAsync
分页列出草稿列表

**函数签名**
```csharp
Task<FeishuApiPageListResult<DraftId>?> GetUserMailboxDraftPageListAsync(
    [Path] string user_mailbox_id,
    [Query] int page_size = 20,
    [Query] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| page_size | int | ⚪ | 分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：20 | 20 |
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
        "draft_id": "268dce11-85f7-427d-8756-6be3abc850fd"
      }
    ],
    "page_token": "evt_xxx",
    "has_more": true
  }
}
```

**说明**
- 只会返回草稿ID信息，不会返回草稿内容
- 如需获取草稿详细内容，请使用GetUserMailboxDraftAsync接口

**代码示例**
```csharp
var draftApi = feishuApp.GetApi<IFeishuUserV1MailDraft>();
var result = await draftApi.GetUserMailboxDraftPageListAsync("me");
if (result?.Data?.Items != null)
{
    foreach (var draft in result.Data.Items)
    {
        Console.WriteLine($"草稿ID: {draft.DraftId}");
    }
}
```

---

### GetUserMailboxDraftAsync
获取草稿内容

**函数签名**
```csharp
Task<FeishuApiResult<GetUserMailboxDraftResult>?> GetUserMailboxDraftAsync(
    [Path] string user_mailbox_id,
    [Path] string draft_id,
    [Query] string? format = null,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| draft_id | string | ✅ | 草稿ID，可通过创建草稿或列出草稿接口获得 | 268dce11-85f7-427d-8756-6be3abc850fd |
| format | string? | ⚪ | 需要获取的草稿内容样式，取值：metadata / full（默认）/ raw<br/>- metadata：草稿元数据信息，包括邮件摘要、主题、收发件人等信息<br/>- raw：获取草稿EML<br/>- full：邮件全文，获取包括纯文本、HTML等在内的邮件全文信息 | full |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "draft_id": "268dce11-85f7-427d-8756-6be3abc850fd",
    "subject": "邮件主题",
    "to_recipients": ["recipient@example.com"],
    "create_time": "2026-06-03T11:34:00+08:00",
    "update_time": "2026-06-03T11:34:00+08:00"
  }
}
```

**说明**
- 根据草稿ID获取草稿详细信息
- 可通过format参数控制返回内容的详细程度

**代码示例**
```csharp
var draftApi = feishuApp.GetApi<IFeishuUserV1MailDraft>();
var result = await draftApi.GetUserMailboxDraftAsync("me", "draft_id_123", format: "full");
Console.WriteLine($"草稿主题: {result?.Data?.Subject}");
```

---

### DeleteUserMailboxDraftAsync
删除草稿

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteUserMailboxDraftAsync(
    [Path] string user_mailbox_id,
    [Path] string draft_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| draft_id | string | ✅ | 草稿ID，可通过创建草稿或列出草稿接口获得 | 268dce11-85f7-427d-8756-6be3abc850fd |
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
- 删除指定邮箱账户下的单份邮件草稿
- 删除操作不可恢复

**代码示例**
```csharp
var draftApi = feishuApp.GetApi<IFeishuUserV1MailDraft>();
var result = await draftApi.DeleteUserMailboxDraftAsync("me", "draft_id_123");
Console.WriteLine($"草稿删除结果: {result.Code == 0}");
```

---

### CreateUserMailboxDraftAsync
创建草稿

**函数签名**
```csharp
Task<FeishuApiResult<UserMailboxDraftOopsResult>?> CreateUserMailboxDraftAsync(
    [Path] string user_mailbox_id,
    [Body] UserMailboxDraftRequest createUserMailboxDraftRequest,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| createUserMailboxDraftRequest | UserMailboxDraftRequest | ✅ | 创建草稿请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "draft_id": "268dce11-85f7-427d-8756-6be3abc850fd",
    "create_time": "2026-06-03T11:34:00+08:00"
  }
}
```

**说明**
- 根据指定的内容创建草稿
- 创建成功后返回草稿ID，可用于后续更新、发送等操作

**代码示例**
```csharp
var draftApi = feishuApp.GetApi<IFeishuUserV1MailDraft>();
var request = new UserMailboxDraftRequest
{
    Subject = "新邮件主题",
    ToRecipients = new List<string> { "recipient@example.com" },
    Body = "邮件正文内容"
};
var result = await draftApi.CreateUserMailboxDraftAsync("me", request);
Console.WriteLine($"草稿创建成功: {result?.Data?.DraftId}");
```
