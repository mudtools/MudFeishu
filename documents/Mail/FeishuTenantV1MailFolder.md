# IFeishuTenantV1MailFolder - 租户邮箱文件夹API

## 功能描述
飞书邮箱文件夹API接口实现了修改、查询、删除等邮箱文件夹管理功能。支持租户管理员通过租户访问令牌管理企业内所有用户的邮箱文件夹。

## 参考文档
- [获取邮箱文件夹信息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-folder/get)
- [创建邮箱文件夹](https://open.feishu.cn/document/mail-v1/user_mailbox-folder/create)
- [删除邮箱文件夹](https://open.feishu.cn/document/mail-v1/user_mailbox-folder/delete)
- [更新邮箱文件夹](https://open.feishu.cn/document/mail-v1/user_mailbox-folder/patch)
- [列出邮箱文件夹](https://open.feishu.cn/document/mail-v1/user_mailbox-folder/list)
- [列出可访问的邮箱](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox/accessible_mailboxes)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| GetUserMailboxFoldeAsync | 获取邮箱文件夹信息 | TenantAccessToken | GET |
| CreateUserMailboxFoldeAsync | 创建邮箱文件夹 | TenantAccessToken | POST |
| DeleteUserMailboxFoldeAsync | 删除邮箱文件夹 | TenantAccessToken | DELETE |
| UpdateUserMailboxFoldeAsync | 更新邮箱文件夹 | TenantAccessToken | PATCH |
| GetUserMailboxFoldeListAsync | 列出邮箱文件夹 | TenantAccessToken | GET |
| GetAccessibleMailboxesUserMailboxAsync | 列出可访问的邮箱 | TenantAccessToken | GET |

## 函数详细内容

### GetUserMailboxFoldeAsync
获取邮箱文件夹信息

**函数签名**
```csharp
Task<FeishuApiResult<UserMailboxFoldeOopsResult>?> GetUserMailboxFoldeAsync(
    [Path] string user_mailbox_id,
    [Path] string folder_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识 | user@example.com |
| folder_id | string | ✅ | 邮件文件夹唯一标识。可通过「获取邮箱文件夹列表」接口获取目标文件夹的 ID；若未传入该参数，默认返回根文件夹（收件箱）详情。 | 7620095646711680541 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "folder_id": "7620095646711680541",
    "name": "工作邮件",
    "type": 2,
    "parent_id": "0",
    "create_time": "2026-06-03T11:34:00+08:00",
    "update_time": "2026-06-03T11:34:00+08:00"
  }
}
```

**说明**
- 通过指定文件夹ID，获取文件夹信息，包括名称、类型等
- 若未传入folder_id，默认返回根文件夹（收件箱）详情

**代码示例**
```csharp
var folderApi = feishuApp.GetApi<IFeishuTenantV1MailFolder>();
var result = await folderApi.GetUserMailboxFoldeAsync("user@example.com", "7620095646711680541");
Console.WriteLine($"文件夹名称: {result?.Data?.Name}");
```

---

### CreateUserMailboxFoldeAsync
创建邮箱文件夹

**函数签名**
```csharp
Task<FeishuApiResult<UserMailboxFoldeOopsResult>?> CreateUserMailboxFoldeAsync(
    [Path] string user_mailbox_id,
    [Body] CreateUserMailboxFoldeRequest createUserMailboxFoldeRequest,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识 | user@example.com |
| createUserMailboxFoldeRequest | CreateUserMailboxFoldeRequest | ✅ | 创建用户邮箱文件夹请求对象 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "folder_id": "7620095646711680542",
    "name": "新建文件夹",
    "type": 2,
    "parent_id": "0",
    "create_time": "2026-06-03T11:34:00+08:00"
  }
}
```

**说明**
- 创建用户自定义的邮箱文件夹
- 可用于分类管理邮件

**代码示例**
```csharp
var folderApi = feishuApp.GetApi<IFeishuTenantV1MailFolder>();
var request = new CreateUserMailboxFoldeRequest
{
    Name = "工作邮件",
    ParentId = "0"
};
var result = await folderApi.CreateUserMailboxFoldeAsync("user@example.com", request);
Console.WriteLine($"文件夹创建成功: {result?.Data?.FolderId}");
```

---

### DeleteUserMailboxFoldeAsync
删除邮箱文件夹

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteUserMailboxFoldeAsync(
    [Path] string user_mailbox_id,
    [Path] string folder_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识 | user@example.com |
| folder_id | string | ✅ | 邮件文件夹唯一标识 | 7620095646711680541 |
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
- 通过指定文件夹ID，删除对应的邮箱文件夹
- 删除操作不可恢复
- 系统文件夹（收件箱、已发送等）不可删除

**代码示例**
```csharp
var folderApi = feishuApp.GetApi<IFeishuTenantV1MailFolder>();
var result = await folderApi.DeleteUserMailboxFoldeAsync("user@example.com", "7620095646711680541");
Console.WriteLine($"文件夹删除结果: {result.Code == 0}");
```

---

### UpdateUserMailboxFoldeAsync
更新邮箱文件夹

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateUserMailboxFoldeAsync(
    [Path] string user_mailbox_id,
    [Path] string folder_id,
    [Body] UpdateUserMailboxFoldeRequest updateUserMailboxFoldeRequest,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识 | user@example.com |
| folder_id | string | ✅ | 邮件文件夹唯一标识 | 7620095646711680541 |
| updateUserMailboxFoldeRequest | UpdateUserMailboxFoldeRequest | ✅ | 更新用户邮箱文件夹请求对象 | - |
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
- 通过指定文件夹ID，更新对应的邮箱文件夹
- 支持修改文件夹名称等属性

**代码示例**
```csharp
var folderApi = feishuApp.GetApi<IFeishuTenantV1MailFolder>();
var request = new UpdateUserMailboxFoldeRequest
{
    Name = "更新后的文件夹名称"
};
var result = await folderApi.UpdateUserMailboxFoldeAsync("user@example.com", "7620095646711680541", request);
Console.WriteLine($"文件夹更新结果: {result.Code == 0}");
```

---

### GetUserMailboxFoldeListAsync
列出邮箱文件夹

**函数签名**
```csharp
Task<FeishuApiResult<GetUserMailboxFoldeListResult>?> GetUserMailboxFoldeListAsync(
    [Path] string user_mailbox_id,
    [Query] int? folder_type = null,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识 | user@example.com |
| folder_type | int? | ⚪ | 文件夹类型<br/>- 1：系统文件夹<br/>- 2：用户文件夹 | 2 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "folder_id": "7620095646711680541",
        "name": "工作邮件",
        "type": 2,
        "unread_count": 5,
        "total_count": 100
      }
    ]
  }
}
```

**说明**
- 列出用户文件夹，可获取文件夹名称、文件夹ID、文件夹下的未读邮件和未读会话数量
- 可通过folder_type参数筛选文件夹类型

**代码示例**
```csharp
var folderApi = feishuApp.GetApi<IFeishuTenantV1MailFolder>();
var result = await folderApi.GetUserMailboxFoldeListAsync("user@example.com", folder_type: 2);
if (result?.Data?.Items != null)
{
    foreach (var folder in result.Data.Items)
    {
        Console.WriteLine($"文件夹: {folder.Name}, 未读: {folder.UnreadCount}");
    }
}
```

---

### GetAccessibleMailboxesUserMailboxAsync
列出可访问的邮箱

**函数签名**
```csharp
Task<FeishuApiResult<GetAccessibleMailboxesUserMailboxResult>?> GetAccessibleMailboxesUserMailboxAsync(
    [Path] string user_mailbox_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识 | user@example.com |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "mailbox_id": "user@example.com",
        "mailbox_type": "user_mailbox",
        "permission": "read_write"
      },
      {
        "mailbox_id": "public@company.com",
        "mailbox_type": "public_mailbox",
        "permission": "read_only"
      }
    ]
  }
}
```

**说明**
- 列出可访问的邮箱，包括拥有读信和发信权限的主账号、公共邮箱
- 可用于切换操作的邮箱账户

**代码示例**
```csharp
var folderApi = feishuApp.GetApi<IFeishuTenantV1MailFolder>();
var result = await folderApi.GetAccessibleMailboxesUserMailboxAsync("user@example.com");
if (result?.Data?.Items != null)
{
    foreach (var mailbox in result.Data.Items)
    {
        Console.WriteLine($"可访问邮箱: {mailbox.MailboxId}, 权限: {mailbox.Permission}");
    }
}
```
