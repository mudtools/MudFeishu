---
title: 邮箱草稿接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份管理自己的邮件草稿，支持草稿的创建、修改、查询、删除与发送。
---

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
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 | 接口文档 |
| :--- | :--- | :--- | :--- |----------|
| UpdateUserMailboxDraftAsync | 更新草稿 | UserAccessToken | PUT | [UpdateUserMailboxDraftAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/update) |
| SendUserMailboxDraftAsync | 发送草稿 | UserAccessToken | POST | [SendUserMailboxDraftAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/send) |
| GetUserMailboxDraftPageListAsync | 分页列出草稿列表 | UserAccessToken | GET | [GetUserMailboxDraftPageListAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/list) |
| GetUserMailboxDraftAsync | 获取草稿内容 | UserAccessToken | GET | [GetUserMailboxDraftAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/get) |
| DeleteUserMailboxDraftAsync | 删除草稿 | UserAccessToken | DELETE | [DeleteUserMailboxDraftAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/delete) |
| CreateUserMailboxDraftAsync | 创建草稿 | UserAccessToken | POST | [CreateUserMailboxDraftAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/create) |

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
    "draft": {
      "id": "268dce11-85f7-427d-8756-6be3abc850fd",
      "message": {
        "raw": "Q29udGVudC1UeXBlOiB0ZXh0L3BsYWluOyBjaGFyc2V0PSJ1cy1hc2NpaSIK",
        "subject": "更新后的邮件主题",
        "message_state": 3
      }
    },
    "reference": "https://{domain}/mail?draftId=268dce11-85f7-427d-8756-6be3abc850fd&scene=send-preview&mailbox=user%40company.com"
  }
}
```

**说明**
- 以 base64url 编码的完整 RFC 5822（EML）邮件内容整体替换草稿，未携带的字段将被清空
- `reference` 为草稿 Web 预览链接
- 更新后草稿状态保持不变

**代码示例**
```csharp
var draftApi = feishuApp.GetApi<IFeishuUserV1MailDraft>();
var request = new UserMailboxDraftRequest
{
    Raw = Convert.ToBase64String(Encoding.UTF8.GetBytes(eml)).Replace('+', '-').Replace('/', '_')
};
var result = await draftApi.UpdateUserMailboxDraftAsync("me", "draft_id_123", request);
Console.WriteLine($"草稿更新成功: {result?.Data?.Draft?.Id}");
```

---

### SendUserMailboxDraftAsync
发送草稿

**函数签名**
```csharp
Task<FeishuApiResult<SendUserMailboxDraftResult>?> SendUserMailboxDraftAsync(
    [Path] string user_mailbox_id,
    [Path] string draft_id,
    [Body] SendUserMailboxDraftRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| draft_id | string | ✅ | 草稿ID，可通过创建草稿、更新草稿或列出草稿列表接口获得 | 268dce11-85f7-427d-8756-6be3abc850fd |
| request | SendUserMailboxDraftRequest | ⚪ | 发送草稿请求体，可选 `send_time`（定时发送的 Unix 时间戳，秒），需至少为当前时间 + 5 分钟；不传则立即发送，日历邀请邮件不支持该字段 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "message_id": "197c5d72e22e1d79",
    "thread_id": "197c5d72e22e1d78",
    "recall_status": "available",
    "automation_send_disable": {
      "reason": "Automation send is disabled by your mailbox setting",
      "reference": "https://open.larksuite.com/mail/settings/automation"
    }
  }
}
```

**说明**
- 传入 `send_time` 时为定时发送，不传则立即发送
- `recall_status` 为撤回状态：available（可撤回）/ unavailable（不可撤回）
- `automation_send_disable` 非空表示自动化发信被禁用，其中 `reason` 为禁用原因、`reference` 为参考链接
- 发送成功后草稿将被删除

**代码示例**
```csharp
var draftApi = feishuApp.GetApi<IFeishuUserV1MailDraft>();
var request = new SendUserMailboxDraftRequest { SendTime = "1720000000" };
var result = await draftApi.SendUserMailboxDraftAsync("me", "draft_id_123", request);
Console.WriteLine($"草稿发送成功: {result?.Data?.MessageId}，会话: {result?.Data?.ThreadId}");
```

---

### GetUserMailboxDraftPageListAsync
分页列出草稿列表

**函数签名**
```csharp
Task<FeishuApiPageListResult<DraftId>?> GetUserMailboxDraftPageListAsync(
    [Path] string user_mailbox_id,
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
        "id": "268dce11-85f7-427d-8756-6be3abc850fd"
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
        Console.WriteLine($"草稿ID: {draft.Id}");
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
    "draft": {
      "id": "268dce11-85f7-427d-8756-6be3abc850fd",
      "message": {
        "subject": "邮件主题",
        "to": [{ "mail_address": "recipient@example.com", "name": "收件人" }],
        "head_from": { "mail_address": "user@example.com", "name": "发件人" },
        "body_html": "5b+76K+H5paH5Lu25pON5L2c",
        "message_state": 3,
        "folder_id": "DRAFT"
      }
    }
  }
}
```

**说明**
- 根据草稿ID获取草稿详细信息，返回结构为 `draft`（含 `id` 与 `message`）
- 可通过format参数控制返回内容的详细程度

**代码示例**
```csharp
var draftApi = feishuApp.GetApi<IFeishuUserV1MailDraft>();
var result = await draftApi.GetUserMailboxDraftAsync("me", "draft_id_123", format: "full");
Console.WriteLine($"草稿主题: {result?.Data?.Draft?.Message?.Subject}");
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
    "draft": {
      "id": "268dce11-85f7-427d-8756-6be3abc850fd",
      "message": {
        "subject": "新邮件主题",
        "message_state": 3
      }
    },
    "reference": "https://{domain}/mail?draftId=268dce11-85f7-427d-8756-6be3abc850fd&scene=send-preview&mailbox=user%40company.com"
  }
}
```

**说明**
- 根据 base64url 编码的完整 RFC 5822（EML）邮件内容创建草稿
- 创建成功后通过 `draft.id` 返回草稿ID，可用于后续更新、发送等操作
- `reference` 为草稿 Web 预览链接

**代码示例**
```csharp
var draftApi = feishuApp.GetApi<IFeishuUserV1MailDraft>();
var request = new UserMailboxDraftRequest
{
    Raw = Convert.ToBase64String(Encoding.UTF8.GetBytes(eml)).Replace('+', '-').Replace('/', '_')
};
var result = await draftApi.CreateUserMailboxDraftAsync("me", request);
Console.WriteLine($"草稿创建成功: {result?.Data?.Draft?.Id}");
```
