---
title: 邮件模板接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份管理自己的邮件模板，支持邮件模板的创建、查询、更新与删除，以及签名的查询。
---

# IFeishuUserV1MailTemplate - 用户邮件模板API

## 功能描述
飞书邮件模板API接口实现了更新、查询等邮件模板功能。支持用户通过用户访问令牌管理自己的邮件模板，`user_mailbox_id` 可使用占位符 `me` 表示当前授权用户的主邮箱；同时提供用户邮箱签名的查询能力。

## 参考文档
- [获取模板附件下载链接](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-template/download_url)
- [更新邮件模板](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-template/update)
- [列出邮件模板](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-template/list)
- [获取邮件模板](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-template/get)
- [创建邮件模板](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-template/create)
- [删除邮件模板](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-template/delete)
- [列出可发信邮箱](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-setting/send_as)
- [查询用户邮箱签名](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-setting/get_signatures)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| GetAttachmentsDownloadUrlAsync | 获取模板附件下载链接 | UserAccessToken | GET |
| UpdateMailTemplateAsync | 更新邮件模板 | UserAccessToken | PUT |
| GetMailTemplateListAsync | 列出邮件模板 | UserAccessToken | GET |
| GetMailTemplateAsync | 获取邮件模板 | UserAccessToken | GET |
| CreateMailTemplateAsync | 创建邮件模板 | UserAccessToken | POST |
| DeleteMailTemplateAsync | 删除邮件模板 | UserAccessToken | DELETE |
| GetSendAsUserMailboxSettingAsync | 列出可发信邮箱 | UserAccessToken | GET |
| GetUserMailboxSignaturesAsync | 查询用户邮箱签名 | UserAccessToken | GET |

## 函数详细内容

### GetAttachmentsDownloadUrlAsync
获取模板附件下载链接

**函数签名**
```csharp
Task<FeishuApiResult<GetAttachmentsDownloadUrlResult>?> GetAttachmentsDownloadUrlAsync(
    [Path] string user_mailbox_id,
    [Path] string template_id,
    [Query] string[] attachment_ids,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | me |
| template_id | string | ✅ | 邮件模板 ID。可通过列出个人邮件模板接口或创建个人邮件模板接口的返回值获取。 | 7281187859195772947 |
| attachment_ids | string[] | ✅ | 待获取下载链接的附件 ID 列表。可通过获取个人邮件模板详情接口返回的 attachments 字段中的 id 获取。 | - |
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
        "download_url": "https://example.com/download/att_001"
      },
      {
        "attachment_id": "att_002",
        "download_url": "https://example.com/download/att_002"
      }
    ],
    "failed_reasons": []
  }
}
```

**说明**
- 获取指定邮件模板下的附件下载链接
- 用于在已知模板 ID 与附件 ID 的场景下，二次获取附件的有效访问 URL
- 便于在用户端预览或下载邮件模板中的附件资源

**代码示例**
```csharp
var templateApi = feishuApp.GetApi<IFeishuUserV1MailTemplate>();
var attachmentIds = new string[] { "att_001", "att_002" };
var result = await templateApi.GetAttachmentsDownloadUrlAsync(
    "me",
    "7281187859195772947",
    attachmentIds);
if (result?.Data?.DownloadUrls != null)
{
    foreach (var att in result.Data.DownloadUrls)
    {
        Console.WriteLine($"附件 {att.AttachmentId} 下载链接: {att.DownloadUrl}");
    }
}
```

---

### UpdateMailTemplateAsync
更新邮件模板

**函数签名**
```csharp
Task<FeishuApiResult<UpdateMailTemplateResult>?> UpdateMailTemplateAsync(
    [Path] string user_mailbox_id,
    [Path] string template_id,
    [Body] UpdateMailTemplateRequest updateMailTemplateRequest,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | me |
| template_id | string | ✅ | 邮件模板 ID。可通过列出个人邮件模板接口或创建个人邮件模板接口的返回值获取。 | 7281187859195772947 |
| updateMailTemplateRequest | UpdateMailTemplateRequest | ✅ | 更新邮件模板请求对象，包含待更新的邮件模板信息。 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "template": {
      "name": "更新后的模板名称",
      "subject": "更新后的邮件主题",
      "template_content": "更新后的邮件正文"
    }
  }
}
```

**说明**
- 以全量替换的方式更新指定邮件模板的所有字段（包括名称、主题、正文、附件、收件信息等）
- 本接口为「全量更新」语义：请求时需传入完整的模板对象，未携带的字段将被清空

**代码示例**
```csharp
var templateApi = feishuApp.GetApi<IFeishuUserV1MailTemplate>();
var request = new UpdateMailTemplateRequest
{
    Template = new MailTemplate
    {
        Name = "更新后的模板名称",
        Subject = "更新后的邮件主题",
        TemplateContent = "更新后的邮件正文"
    }
};
var result = await templateApi.UpdateMailTemplateAsync(
    "me",
    "7281187859195772947",
    request);
Console.WriteLine($"模板更新成功: {result?.Data?.Template?.Name}");
```

---

### GetMailTemplateListAsync
列出邮件模板

**函数签名**
```csharp
Task<FeishuApiResult<GetMailTemplateListResult>?> GetMailTemplateListAsync(
    [Path] string user_mailbox_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | me |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "template_id": "7281187859195772947",
        "name": "模板1",
        "create_time": "2026-06-03T11:50:00+08:00"
      }
    ]
  }
}
```

**说明**
- 列出指定用户邮箱下的全部个人邮件模板基本信息（一次性返回，不分页）
- 常用于在编辑或发送邮件场景下展示可选模板列表
- 如需获取模板正文与附件等完整字段，请通过获取个人邮件模板详情接口按 template_id 查询

**代码示例**
```csharp
var templateApi = feishuApp.GetApi<IFeishuUserV1MailTemplate>();
var result = await templateApi.GetMailTemplateListAsync("me");
if (result?.Data?.Items != null)
{
    foreach (var template in result.Data.Items)
    {
        Console.WriteLine($"模板: {template.Name} (ID: {template.TemplateId})");
    }
}
```

---

### GetMailTemplateAsync
获取邮件模板

**函数签名**
```csharp
Task<FeishuApiResult<GetMailTemplateResult>?> GetMailTemplateAsync(
    [Path] string user_mailbox_id,
    [Path] string template_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | me |
| template_id | string | ✅ | 邮件模板 ID。可通过列出个人邮件模板接口或创建个人邮件模板接口的返回值获取。 | 7281187859195772947 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "template": {
      "template_id": "7281187859195772947",
      "name": "模板名称",
      "subject": "邮件主题",
      "template_content": "邮件正文",
      "tos": [
        {
          "mail_address": "recipient@example.com",
          "name": "收件人"
        }
      ],
      "create_time": "2026-06-03T11:50:00+08:00"
    }
  }
}
```

**说明**
- 获取指定邮件模板的完整详情，包括模板名称、主题、正文（HTML 或纯文本）、收件人/抄送/密送地址、附件信息等所有字段
- 常用于编辑模板前回填表单，或在发送邮件场景下读取模板内容做二次填充

**代码示例**
```csharp
var templateApi = feishuApp.GetApi<IFeishuUserV1MailTemplate>();
var result = await templateApi.GetMailTemplateAsync(
    "me",
    "7281187859195772947");
Console.WriteLine($"模板名称: {result?.Data?.Template?.Name}");
Console.WriteLine($"主题: {result?.Data?.Template?.Subject}");
Console.WriteLine($"正文: {result?.Data?.Template?.TemplateContent}");
```

---

### CreateMailTemplateAsync
创建邮件模板

**函数签名**
```csharp
Task<FeishuApiResult<CreateMailTemplateResult>?> CreateMailTemplateAsync(
    [Path] string user_mailbox_id,
    [Body] CreateMailTemplateRequest createMailTemplateRequest,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | me |
| createMailTemplateRequest | CreateMailTemplateRequest | ✅ | 创建邮件模板请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "template": {
      "template_id": "7281187859195772947",
      "name": "新模板",
      "subject": "邮件主题",
      "template_content": "邮件正文",
      "create_time": "2026-06-03T11:50:00+08:00"
    }
  }
}
```

**说明**
- 在指定用户邮箱下创建一份可复用的个人邮件模板
- 请求时需传入完整的模板对象（含名称、主题、正文、收件信息、附件等）
- 创建成功后返回完整模板内容（含系统生成的 template_id）
- 适用于将常用邮件内容沉淀为模板以便后续快速发送同类型邮件

**代码示例**
```csharp
var templateApi = feishuApp.GetApi<IFeishuUserV1MailTemplate>();
var request = new CreateMailTemplateRequest
{
    Template = new MailTemplate
    {
        Name = "新模板",
        Subject = "邮件主题",
        TemplateContent = "邮件正文",
        Tos = new[] { new MailAddress { MailAddressSuffix = "recipient@example.com", Name = "收件人" } }
    }
};
var result = await templateApi.CreateMailTemplateAsync("me", request);
Console.WriteLine($"模板创建成功: {result?.Data?.Template?.TemplateId}");
```

---

### DeleteMailTemplateAsync
删除邮件模板

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteMailTemplateAsync(
    [Path] string user_mailbox_id,
    [Path] string template_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | me |
| template_id | string | ✅ | 邮件模板 ID。可通过列出个人邮件模板接口或创建个人邮件模板接口的返回值获取。 | 7281187859195772947 |
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
- 永久删除指定用户邮箱下的某个个人邮件模板
- 删除操作不可恢复
- 删除后该模板将无法在「列出邮件模板」「获取邮件模板」等接口中再返回
- 常用于清理已废弃或不再使用的模板

**代码示例**
```csharp
var templateApi = feishuApp.GetApi<IFeishuUserV1MailTemplate>();
var result = await templateApi.DeleteMailTemplateAsync(
    "me",
    "7281187859195772947");
Console.WriteLine($"模板删除结果: {result.Code == 0}");
```

---

### GetSendAsUserMailboxSettingAsync
列出可发信邮箱

**函数签名**
```csharp
Task<FeishuApiResult<GetSendAsUserMailboxSettingResult>?> GetSendAsUserMailboxSettingAsync(
    [Path] string user_mailbox_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | me |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "sendable_addresses": [
      {
        "email_address": "user@example.com",
        "name": "用户姓名",
        "email_type": "USER_PRIMARY"
      },
      {
        "email_address": "alias@example.com",
        "name": "别名邮箱",
        "email_type": "USER_ALIAS"
      }
    ]
  }
}
```

**说明**
- 获取当前地址的可用于发信的邮箱地址列表
- 包括主邮箱、别名邮箱、公共邮箱等可发信地址

**代码示例**
```csharp
var templateApi = feishuApp.GetApi<IFeishuUserV1MailTemplate>();
var result = await templateApi.GetSendAsUserMailboxSettingAsync("me");
if (result?.Data?.SendableAddresses != null)
{
    foreach (var addr in result.Data.SendableAddresses)
    {
        Console.WriteLine($"可发信邮箱: {addr.EmailAddress} ({addr.EmailType})");
    }
}
```

---

### GetUserMailboxSignaturesAsync
查询用户邮箱签名

**函数签名**
```csharp
Task<FeishuApiResult<MailboxSignaturesResult>?> GetUserMailboxSignaturesAsync(
   [Path] string user_mailbox_id,
   CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | me |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "signatures": [
      {
        "id": "7281187859195772947",
        "name": "默认签名",
        "content": "<p>张三</p>",
        "signature_type": "USER",
        "signature_device": "PC"
      }
    ],
    "usages": [
      {
        "email_address": "user@example.com",
        "send_mail_signature_id": "7281187859195772947",
        "reply_signature_id": "7281187859195772947"
      }
    ]
  }
}
```

**说明**
- 查询用户邮箱签名，返回签名列表与各邮箱地址的签名使用情况
- `signature_type` 可选值：`USER`（用户签名）、`TENANT`（租户签名）
- `signature_device` 可选值：`PC`、`MOBILE`
- `usages[]` 中 `send_mail_signature_id` / `reply_signature_id` 分别为该邮箱地址发送邮件、回复邮件时使用的签名 ID

**代码示例**
```csharp
var templateApi = feishuApp.GetApi<IFeishuUserV1MailTemplate>();
var result = await templateApi.GetUserMailboxSignaturesAsync("me");
if (result?.Data?.Signatures != null)
{
    foreach (var signature in result.Data.Signatures)
    {
        Console.WriteLine($"签名: {signature.Name} ({signature.SignatureType})");
    }
}
```
