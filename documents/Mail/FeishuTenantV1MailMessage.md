# IFeishuTenantV1MailMessage - 租户邮箱邮件API

## 功能描述
飞书邮箱邮件API接口实现了修改、查询、删除等邮箱邮件管理功能。支持租户管理员通过租户访问令牌管理企业内所有用户的邮箱邮件。

## 参考文档
- [批量删除邮件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/batch_trash)
- [批量修改邮件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/batch_modify)
- [删除邮件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/trash)
- [修改邮件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/modify)
- [批量获取邮件详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/batch_get)
- [查询会话下邮件信息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/list_thread_message)
- [获取邮件卡片的邮件列表](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/list_thread_message)
- [分页列出邮件](https://open.feishu.cn/document/mail-v1/user_mailbox-message/list)
- [获取邮件详情](https://open.feishu.cn/document/mail-v1/user_mailbox-message/get)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| BatchTrashUserMailboxMessageAsync | 批量删除邮件 | TenantAccessToken | POST |
| BatchModifyUserMailboxMessageAsync | 批量修改邮件 | TenantAccessToken | POST |
| DeleteUserMailboxMessageAsync | 删除邮件 | TenantAccessToken | POST |
| ModifyUserMailboxMessageAsync | 修改邮件 | TenantAccessToken | PUT |
| BatchGetUserMailboxMessageAsync | 批量获取邮件详情 | TenantAccessToken | POST |
| GetThreadMessageUserMailboxMessageAsync | 查询会话下邮件信息 | TenantAccessToken | GET |
| GetByCardUserMailboxMessageAsync | 获取邮件卡片的邮件列表 | TenantAccessToken | GET |
| GetUserMailboxMessagePageListAsync | 分页列出邮件 | TenantAccessToken | GET |
| GetUserMailboxMessageAsync | 获取邮件详情 | TenantAccessToken | GET |

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
TenantAccessToken（租户访问令牌）

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
- 使用 tenant_access_token 时，需要申请邮件数据资源的数据权限

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuTenantV1MailMessage>();
var request = new BatchTrashUserMailboxMessageRequest
{
    MessageIds = new List<string> { "msg_id_1", "msg_id_2" }
};
var result = await messageApi.BatchTrashUserMailboxMessageAsync("user@example.com", request);
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
TenantAccessToken（租户访问令牌）

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
- 使用 tenant_access_token 时，需要申请邮件数据资源的数据权限

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuTenantV1MailMessage>();
var request = new BatchModifyUserMailboxMessageRequest
{
    MessageIds = new List<string> { "msg_id_1", "msg_id_2" },
    Labels = new List<string> { "FLAGGED" }
};
var result = await messageApi.BatchModifyUserMailboxMessageAsync("user@example.com", request);
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
TenantAccessToken（租户访问令牌）

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
- 使用 tenant_access_token 时，需要申请邮件数据资源的数据权限

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuTenantV1MailMessage>();
var result = await messageApi.DeleteUserMailboxMessageAsync("user@example.com", "msg_id_123");
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
TenantAccessToken（租户访问令牌）

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
- 使用 tenant_access_token 时，需要申请邮件数据资源的数据权限

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuTenantV1MailMessage>();
var request = new ModifyUserMailboxMessageRequest
{
    Labels = new List<string> { "FLAGGED" },
    IsRead = true
};
var result = await messageApi.ModifyUserMailboxMessageAsync("user@example.com", "msg_id_123", request);
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
TenantAccessToken（租户访问令牌）

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
    "items": [
      {
        "message_id": "NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ==",
        "subject": "邮件主题",
        "from": "sender@example.com",
        "to": ["recipient@example.com"],
        "create_time": "2026-06-03T11:41:00+08:00"
      }
    ]
  }
}
```

**说明**
- 通过指定邮件ID，获取对应邮件的标签、文件夹、摘要、正文、html、附件等信息
- 使用 tenant_access_token 时，需要申请邮件数据资源的数据权限

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuTenantV1MailMessage>();
var request = new BatchGetUserMailboxMessageRequest
{
    MessageIds = new List<string> { "msg_id_1", "msg_id_2" }
};
var result = await messageApi.BatchGetUserMailboxMessageAsync("user@example.com", request);
if (result?.Data?.Items != null)
{
    foreach (var msg in result.Data.Items)
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
TenantAccessToken（租户访问令牌）

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
        "subject": "邮件主题",
        "from": "sender@example.com",
        "create_time": "2026-06-03T11:41:00+08:00"
      }
    ]
  }
}
```

**说明**
- 通过用户邮箱地址和邮件会话ID，获取该会话下的所有邮件关键信息列表
- 使用 tenant_access_token 时，需要申请邮件数据资源的数据权限

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuTenantV1MailMessage>();
var result = await messageApi.GetThreadMessageUserMailboxMessageAsync("user@example.com", "thread_id_123", format: "full");
if (result?.Data?.Items != null)
{
    foreach (var msg in result.Data.Items)
    {
        Console.WriteLine($"会话邮件: {msg.Subject}");
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
    [Query] string? user_id_type = "user",
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

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
    "items": [
      {
        "message_id": "NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ==",
        "subject": "邮件主题"
      }
    ]
  }
}
```

**说明**
- 获取邮件卡片关联的邮件列表
- 可通过接收消息事件的推送获取卡片ID

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuTenantV1MailMessage>();
var result = await messageApi.GetByCardUserMailboxMessageAsync(
    "user@example.com",
    "512ca581-6059-4449-8150-5522e6641d32",
    "1234567890");
if (result?.Data?.Items != null)
{
    foreach (var msg in result.Data.Items)
    {
        Console.WriteLine($"卡片邮件: {msg.Subject}");
    }
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
    [Query] int page_size = 20,
    [Query] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| folder_id | string? | ⚪ | 文件夹id，获取方式见列出邮箱文件夹 | INBOX 或者用户文件夹id |
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
- 分页列出邮件，使用应用权限访问时，需要申请邮件数据资源的数据权限
- 返回邮件ID列表，可通过批量获取邮件详情接口获取详细信息

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuTenantV1MailMessage>();
var result = await messageApi.GetUserMailboxMessagePageListAsync("user@example.com", only_unread: true);
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
TenantAccessToken（租户访问令牌）

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
    "message_id": "NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ==",
    "subject": "邮件主题",
    "from": "sender@example.com",
    "to": ["recipient@example.com"],
    "body": "邮件正文内容",
    "create_time": "2026-06-03T11:41:00+08:00"
  }
}
```

**说明**
- 获取邮件详情，使用应用权限访问时，需要申请邮件数据资源的数据权限
- 可通过format参数控制返回内容的详细程度

**代码示例**
```csharp
var messageApi = feishuApp.GetApi<IFeishuTenantV1MailMessage>();
var result = await messageApi.GetUserMailboxMessageAsync("user@example.com", "msg_id_123", format: "full");
Console.WriteLine($"邮件主题: {result?.Data?.Subject}");
Console.WriteLine($"发件人: {result?.Data?.From}");
```
