# 邮件会话 API（用户令牌）

## 接口名称
**飞书邮件会话 API -（IFeishuUserV1MailThread）**

## 功能描述
飞书邮件会话 API 接口实现了修改、查询、删除等邮件会话管理功能。
当前接口使用用户令牌（UserAccessToken）访问，调用时 `user_mailbox_id` 可使用占位符 `me` 表示当前授权用户的主邮箱。

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
| BatchTrashUserMailboxThreadAsync | 批量删除邮件会话 | UserAccessToken | POST |
| BatchModifyUserMailboxThreadAsync | 批量修改邮件会话 | UserAccessToken | POST |
| TrashUserMailboxThreadAsync | 删除邮件会话 | UserAccessToken | POST |
| ModifyUserMailboxThreadAsync | 修改邮件会话 | UserAccessToken | POST |
| GetUserMailboxThreadAsync | 获取邮件会话详情 | UserAccessToken | GET |
| GetUserMailboxThreadPageListAsync | 分页列出邮件会话 | UserAccessToken | GET |

## 函数详细内容

### 批量删除邮件会话

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> BatchTrashUserMailboxThreadAsync(
  [Path] string user_mailbox_id,
  [Body] BatchTrashUserMailboxThreadRequest batchTrashUserMailboxThreadRequest,
  CancellationToken cancellationToken = default);
```

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| user_mailbox_id | string | ✅ | 用户邮箱地址，可使用占位符 `me` 表示当前授权用户的主邮箱 |
| batchTrashUserMailboxThreadRequest | BatchTrashUserMailboxThreadRequest | ✅ | 批量删除邮件会话请求对象，包含待删除的邮件会话 ID 列表 |

**说明**：批量将指定的邮件会话移入已删除文件夹。

---

### 批量修改邮件会话

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> BatchModifyUserMailboxThreadAsync(
 [Path] string user_mailbox_id,
 [Body] BatchModifyUserMailboxThreadRequest batchModifyUserMailboxThreadRequest,
 CancellationToken cancellationToken = default);
```

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| user_mailbox_id | string | ✅ | 用户邮箱地址，可使用占位符 `me` |
| batchModifyUserMailboxThreadRequest | BatchModifyUserMailboxThreadRequest | ✅ | 批量修改邮件会话请求对象，包含待修改的邮件会话 ID 列表 |

**说明**：批量修改邮件会话的标签、所属文件夹和已读未读状态。不支持移入已删除文件夹。

---

### 删除邮件会话

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> TrashUserMailboxThreadAsync(
    [Path] string user_mailbox_id,
    [Path] string thread_id,
    CancellationToken cancellationToken = default);
```

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| user_mailbox_id | string | ✅ | 用户邮箱地址，可使用占位符 `me` |
| thread_id | string | ✅ | 邮件会话 ID，示例值：`th_xxxxxxxxxxxx` |

**说明**：将指定的邮件会话移入已删除文件夹。

---

### 修改邮件会话

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> ModifyUserMailboxThreadAsync(
     [Path] string user_mailbox_id,
     [Path] string thread_id,
     [Body] ModifyUserMailboxThreadRequest modifyUserMailboxThreadRequest,
     CancellationToken cancellationToken = default);
```

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| user_mailbox_id | string | ✅ | 用户邮箱地址，可使用占位符 `me` |
| thread_id | string | ✅ | 邮件会话 ID，示例值：`th_xxxxxxxxxxxx` |
| modifyUserMailboxThreadRequest | ModifyUserMailboxThreadRequest | ✅ | 修改邮件会话请求对象 |

**说明**：修改邮件会话的标签、所属文件夹和已读未读状态。

---

### 获取邮件会话详情

**函数签名**：
```csharp
Task<FeishuApiResult<GetUserMailboxThreadResult>?> GetUserMailboxThreadAsync(
    [Path] string user_mailbox_id,
    [Path] string thread_id,
    [Query] string? format = null,
    [Query] bool? include_spam_trash = null,
    CancellationToken cancellationToken = default);
```

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| user_mailbox_id | string | ✅ | 用户邮箱地址，可使用占位符 `me` |
| thread_id | string | ✅ | 邮件会话 ID，示例值：`th_xxxxxxxxxxxx` |
| format | string? | ⚪ | 邮件内容格式：`full`、`plain_text_full`、`metadata`，默认值：`null` |
| include_spam_trash | bool? | ⚪ | 是否包含来自 SPAM 和 TRASH 的邮件，默认值：`null` |

**说明**：获取指定邮件会话下的邮件列表。

---

### 分页列出邮件会话

**函数签名**：
```csharp
Task<FeishuApiPageListResult<MailThreadInfo>?> GetUserMailboxThreadPageListAsync(
    [Path] string user_mailbox_id,
    [Query] string? folder_id = null,
    [Query] bool? only_unread = null,
    [Query] string? label_id = null,
    [Query] int page_size = 20,
    [Query] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| user_mailbox_id | string | ✅ | 用户邮箱地址，可使用占位符 `me` |
| folder_id | string? | ⚪ | 文件夹 ID，支持 `INBOX`、`SENT`、`SPAM`、`ARCHIVED`、`SCHEDULED`、`TRASH`、`DRAFT` 及自定义文件夹 ID |
| only_unread | bool? | ⚪ | 是否只查询未读会话，默认值：`null` |
| label_id | string? | ⚪ | 标签 ID，支持 `IMPORTANT`、`OTHER`、`FLAGGED` 及自定义标签 ID |
| page_size | int | ⚪ | 分页大小，默认值：20 |
| page_token | string? | ⚪ | 分页标记，第一次请求不填 |

**说明**：分页列出用户指定文件夹或标签下的邮件会话，按时间倒序分页获取。
