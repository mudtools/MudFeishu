# 邮件模板 API（用户令牌）

## 接口名称
**飞书邮件模板 API -（IFeishuUserV1MailTemplate）**

## 功能描述
飞书邮件模板 API 接口实现了更新、查询等邮件模板功能。与租户令牌版本相比，用户令牌版本用于管理当前授权用户自己的邮件模板，`user_mailbox_id` 可使用占位符 `me` 表示当前授权用户的主邮箱。

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
| user_mailbox_id | string | ✅ | 用户邮箱地址，可使用占位符 `me` 表示当前授权用户的主邮箱。 | me |
| template_id | string | ✅ | 邮件模板 ID。 | 7281187859195772947 |
| attachment_ids | string[] | ✅ | 待获取下载链接的附件 ID 列表。 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
获取指定邮件模板下的附件下载链接，用于在已知模板 ID 与附件 ID 的场景下二次获取附件的有效访问 URL。

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
| 参数名 | 类型 | 必填 | 描述 |
| :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，可使用占位符 `me` |
| template_id | string | ✅ | 邮件模板 ID |
| updateMailTemplateRequest | UpdateMailTemplateRequest | ✅ | 更新邮件模板请求体（全量替换语义） |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 |

**说明**：以全量替换的方式更新指定邮件模板的所有字段，未携带的字段将被清空。

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
| 参数名 | 类型 | 必填 | 描述 |
| :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，可使用占位符 `me` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 |

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
| 参数名 | 类型 | 必填 | 描述 |
| :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，可使用占位符 `me` |
| template_id | string | ✅ | 邮件模板 ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 |

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
| 参数名 | 类型 | 必填 | 描述 |
| :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，可使用占位符 `me` |
| createMailTemplateRequest | CreateMailTemplateRequest | ✅ | 创建邮件模板请求体 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 |

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
| 参数名 | 类型 | 必填 | 描述 |
| :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，可使用占位符 `me` |
| template_id | string | ✅ | 邮件模板 ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 |

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
| 参数名 | 类型 | 必填 | 描述 |
| :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，可使用占位符 `me` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 |

**说明**：获取当前地址可用于发信的邮箱地址列表，包括主邮箱、别名邮箱、公共邮箱等可发信地址。

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
| 参数名 | 类型 | 必填 | 描述 |
| :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，可使用占位符 `me` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 |

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
