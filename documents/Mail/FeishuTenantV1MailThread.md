---
title: 邮件会话接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份管理邮件会话，支持会话的批量/单个修改、删除、详情查询与分页列出。
---

# IFeishuTenantV1MailThread - 租户邮件会话API

## 功能描述
飞书邮件会话API接口实现了修改、查询、删除等邮件会话管理功能。支持租户管理员通过租户访问令牌管理企业内用户的邮件会话。

## 参考文档
- [批量删除邮件会话](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-thread/batch_trash)
- [批量修改邮件会话](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-thread/batch_modify)
- [删除邮件会话](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-thread/trash)
- [修改邮件会话](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-thread/modify)
- [获取邮件会话详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-thread/get)
- [分页列出邮件会话](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-thread/list)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| BatchTrashUserMailboxThreadAsync | 批量删除邮件会话 | TenantAccessToken | POST |
| BatchModifyUserMailboxThreadAsync | 批量修改邮件会话 | TenantAccessToken | POST |
| TrashUserMailboxThreadAsync | 删除邮件会话 | TenantAccessToken | POST |
| ModifyUserMailboxThreadAsync | 修改邮件会话 | TenantAccessToken | POST |
| GetUserMailboxThreadAsync | 获取邮件会话详情 | TenantAccessToken | GET |
| GetUserMailboxThreadPageListAsync | 分页列出邮件会话 | TenantAccessToken | GET |

## 函数详细内容

### BatchTrashUserMailboxThreadAsync
批量删除邮件会话

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> BatchTrashUserMailboxThreadAsync(
  [Path] string user_mailbox_id,
  [Body] BatchTrashUserMailboxThreadRequest batchTrashUserMailboxThreadRequest,
  CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| batchTrashUserMailboxThreadRequest | BatchTrashUserMailboxThreadRequest | ✅ | 批量删除邮件会话请求对象，包含待删除的邮件会话 ID 列表。 | - |
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
- 批量将指定的邮件会话移入已删除文件夹
- 请求体 `BatchTrashUserMailboxThreadRequest` 字段：

| 字段 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| thread_ids | string[]? | ⚪ | 邮箱会话 ID 列表，可通过列出邮件会话接口获取。最大长度 20 | - |

- 使用 tenant_access_token 时，需要申请邮箱资源的数据权限

**代码示例**
```csharp
var threadApi = feishuApp.GetApi<IFeishuTenantV1MailThread>();
var request = new BatchTrashUserMailboxThreadRequest
{
    ThreadIds = new[] { "th_001", "th_002" }
};
var result = await threadApi.BatchTrashUserMailboxThreadAsync("user@example.com", request);
Console.WriteLine($"批量删除结果: {result?.Code == 0}");
```

---

### BatchModifyUserMailboxThreadAsync
批量修改邮件会话

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> BatchModifyUserMailboxThreadAsync(
 [Path] string user_mailbox_id,
 [Body] BatchModifyUserMailboxThreadRequest batchModifyUserMailboxThreadRequest,
 CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| batchModifyUserMailboxThreadRequest | BatchModifyUserMailboxThreadRequest | ✅ | 批量修改邮件会话请求对象，包含待修改的邮件会话 ID 列表。 | - |
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
- 批量修改邮件会话的标签、所属文件夹和已读未读状态，支持为邮件会话添加旗标、归档、移入垃圾邮件文件夹
- 注意：接口**不支持**将邮件会话移入已删除文件夹，如需请使用批量删除邮件会话接口
- 请求体 `BatchModifyUserMailboxThreadRequest` 字段：

| 字段 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| add_label_ids | string[]? | ⚪ | 待添加的标签，可选值：UNREAD、IMPORTANT、OTHER、FLAGGED，以及自定义标签 ID。最大长度 20 | - |
| remove_label_ids | string[]? | ⚪ | 待移除的标签，可选值同上。最大长度 20 | - |
| add_folder | string? | ⚪ | 需要移入的文件夹，支持 INBOX、SENT、SPAM、ARCHIVED 以及自定义文件夹 ID | INBOX |
| thread_ids | string[]? | ⚪ | 需要操作的邮件会话 ID，可通过列出邮件会话获取。最大长度 20 | - |

- 使用 tenant_access_token 时，需要申请邮箱资源的数据权限

**代码示例**
```csharp
var threadApi = feishuApp.GetApi<IFeishuTenantV1MailThread>();
var request = new BatchModifyUserMailboxThreadRequest
{
    ThreadIds = new[] { "th_001", "th_002" },
    AddLabelIds = new[] { "IMPORTANT" },
    RemoveLabelIds = new[] { "UNREAD" }
};
var result = await threadApi.BatchModifyUserMailboxThreadAsync("user@example.com", request);
Console.WriteLine($"批量修改结果: {result?.Code == 0}");
```

---

### TrashUserMailboxThreadAsync
删除邮件会话

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> TrashUserMailboxThreadAsync(
    [Path] string user_mailbox_id,
    [Path] string thread_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| thread_id | string | ✅ | 邮件会话 ID。可通过发送邮件、回复邮件的接口返回值或获取邮件详情接口查询获得。 | th_xxxxxxxxxxxx |
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
- 将指定的邮件会话移入已删除文件夹
- 使用 tenant_access_token 时，需要申请邮箱资源的数据权限

**代码示例**
```csharp
var threadApi = feishuApp.GetApi<IFeishuTenantV1MailThread>();
var result = await threadApi.TrashUserMailboxThreadAsync("user@example.com", "th_xxxxxxxxxxxx");
Console.WriteLine($"删除结果: {result?.Code == 0}");
```

---

### ModifyUserMailboxThreadAsync
修改邮件会话

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> ModifyUserMailboxThreadAsync(
         [Path] string user_mailbox_id,
         [Path] string thread_id,
         [Body] ModifyUserMailboxThreadRequest modifyUserMailboxThreadRequest,
         CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| thread_id | string | ✅ | 邮件会话 ID。可通过发送邮件、回复邮件的接口返回值或获取邮件详情接口查询获得。 | th_xxxxxxxxxxxx |
| modifyUserMailboxThreadRequest | ModifyUserMailboxThreadRequest | ✅ | 修改邮件会话请求对象，包含待修改的邮件会话信息。 | - |
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
- 修改邮件会话的标签、所属文件夹和已读未读状态，支持为邮件会话添加旗标、归档、移入垃圾邮件文件夹
- 注意：接口**不支持**将邮件会话移入已删除文件夹，如需请使用删除邮件会话接口
- 请求体 `ModifyUserMailboxThreadRequest` 字段：

| 字段 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| add_label_ids | string[]? | ⚪ | 待添加的标签，可选值：UNREAD、IMPORTANT、OTHER、FLAGGED，以及自定义标签 ID。最大长度 20 | - |
| remove_label_ids | string[]? | ⚪ | 待移除的标签，可选值同上。最大长度 20 | - |
| add_folder | string? | ⚪ | 需要移入的文件夹，支持 INBOX、SENT、SPAM、ARCHIVED 以及自定义文件夹 ID | INBOX |

- 使用 tenant_access_token 时，需要申请邮箱资源的数据权限

**代码示例**
```csharp
var threadApi = feishuApp.GetApi<IFeishuTenantV1MailThread>();
var request = new ModifyUserMailboxThreadRequest
{
    AddLabelIds = new[] { "FLAGGED" }
};
var result = await threadApi.ModifyUserMailboxThreadAsync("user@example.com", "th_xxxxxxxxxxxx", request);
Console.WriteLine($"修改结果: {result?.Code == 0}");
```

---

### GetUserMailboxThreadAsync
获取邮件会话详情

**函数签名**
```csharp
Task<FeishuApiResult<GetUserMailboxThreadResult>?> GetUserMailboxThreadAsync(
        [Path] string user_mailbox_id,
        [Path] string thread_id,
        [Query] string? format = null,
        [Query] bool? include_spam_trash = null,
        CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| thread_id | string | ✅ | 邮件会话 ID。可通过发送邮件、回复邮件的接口返回值或获取邮件详情接口查询获得。 | th_xxxxxxxxxxxx |
| format | string? | ⚪ | 需要获取的邮件内容，支持选择 full / plain_text_full / metadata。默认值：null | full |
| include_spam_trash | bool? | ⚪ | 是否获取包含来自 SPAM 和 TRASH 的邮件。默认值：null | true |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "thread": {
      "id": "th_xxxxxxxxxxxx",
      "body_preview": "hello world",
      "messages": [
        {
          "message_id": "msg_001",
          "subject": "会议纪要"
        }
      ]
    }
  }
}
```

**说明**
- 获取指定邮件会话下的邮件列表，包含邮件元数据及主题、正文等内容
- 支持获取会话中位于垃圾邮件文件夹和已删除文件夹的邮件
- `format` 可选值：
  - `full`：全文，包括标签、文件夹、主题、收发件人、纯文本、HTML 等信息
  - `plain_text_full`：全文，只返回纯文本正文内容，不返回 HTML。返回内容包括标签、文件夹、主题、收发件人、纯文本等信息
  - `metadata`：邮件元数据信息，包括标签、文件夹、主题、收发件人、摘要等信息，不返回正文内容
- 使用 tenant_access_token 时，需要申请邮箱资源的数据权限

**代码示例**
```csharp
var threadApi = feishuApp.GetApi<IFeishuTenantV1MailThread>();
var result = await threadApi.GetUserMailboxThreadAsync(
    "user@example.com",
    "th_xxxxxxxxxxxx",
    format: "metadata",
    include_spam_trash: true);
Console.WriteLine($"会话摘要: {result?.Data?.Thread?.BodyPreview}");
```

---

### GetUserMailboxThreadPageListAsync
分页列出邮件会话

**函数签名**
```csharp
Task<FeishuApiPageListResult<MailThreadInfo>?> GetUserMailboxThreadPageListAsync(
        [Path] string user_mailbox_id,
        [Query] string? folder_id = null,
        [Query] bool? only_unread = null,
        [Query] string? label_id = null,
        [Query] int page_size = Consts.PageSize_20,
        [Query] string? page_token = null,
        CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| folder_id | string? | ⚪ | 文件夹 id，支持 INBOX、SENT、SPAM、ARCHIVED、SCHEDULED、TRASH、DRAFT 以及自定义文件夹 ID。默认值：null | INBOX |
| only_unread | bool? | ⚪ | 是否只查询未读会话。默认值：null | true |
| label_id | string? | ⚪ | 标签 id，支持 IMPORTANT、OTHER、FLAGGED 以及自定义标签 ID。默认值：null | FLAGGED |
| page_size | int | ⚪ | 分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：20 | 20 |
| page_token | string? | ⚪ | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "id": "th_xxxxxxxxxxxx",
        "body_preview": "hello world"
      }
    ],
    "page_token": "uY2gj1RUFhM2vpu1xJf9asLkl2sMkmbudq1",
    "has_more": true
  }
}
```

**说明**
- 分页列出用户指定文件夹或标签下的邮件会话，按时间倒序分页获取
- 使用 tenant_access_token 时，需要申请邮箱资源的数据权限

**代码示例**
```csharp
var threadApi = feishuApp.GetApi<IFeishuTenantV1MailThread>();
string? pageToken = null;
do
{
    var result = await threadApi.GetUserMailboxThreadPageListAsync(
        "user@example.com",
        folder_id: "INBOX",
        page_size: 20,
        page_token: pageToken);
    if (result?.Data?.Items != null)
    {
        foreach (var thread in result.Data.Items)
        {
            Console.WriteLine($"会话 {thread.Id}: {thread.BodyPreview}");
        }
    }
    pageToken = result?.Data?.PageToken;
} while (pageToken is not null);
```
