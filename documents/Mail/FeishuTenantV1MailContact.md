# IFeishuTenantV1MailContact - 租户邮箱联系人API

## 功能描述
飞书邮箱联系人API接口实现了修改、查询、删除等邮箱联系人管理功能。支持租户管理员通过租户访问令牌管理企业内所有用户的邮箱联系人。

## 参考文档
- [创建邮箱联系人](https://open.feishu.cn/document/mail-v1/user_mailbox-mail_contact/create)
- [删除邮箱联系人](https://open.feishu.cn/document/mail-v1/user_mailbox-mail_contact/delete)
- [修改邮箱联系人](https://open.feishu.cn/document/mail-v1/user_mailbox-mail_contact/patch)
- [列出邮箱联系人](https://open.feishu.cn/document/mail-v1/user_mailbox-mail_contact/list)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateUserMailboxContactAsync | 创建邮箱联系人 | TenantAccessToken | POST |
| DeleteUserMailboxContactAsync | 删除邮箱联系人 | TenantAccessToken | DELETE |
| UpdateUserMailboxContactAsync | 修改邮箱联系人信息 | TenantAccessToken | PATCH |
| GetUserMailboxContactPageListAsync | 分页列出邮箱联系人 | TenantAccessToken | GET |

## 函数详细内容

### CreateUserMailboxContactAsync
创建用户邮箱联系人

**函数签名**
```csharp
Task<FeishuApiResult<CreateUserMailboxContactResult>?> CreateUserMailboxContactAsync(
    [Path] string user_mailbox_id,
    [Body] CreateUserMailboxContactRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识 | user@example.com |
| request | CreateUserMailboxContactRequest | ✅ | 创建用户邮箱联系人请求对象 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "mail_contact_id": "123",
    "email": "contact@example.com",
    "name": "联系人名称",
    "create_time": "2026-06-03T11:34:00+08:00"
  }
}
```

**说明**
- 使用 tenant_access_token 时，需要申请邮箱联系人资源的数据权限
- 创建的联系人可用于邮件收发时的快速选择

**代码示例**
```csharp
var contactApi = feishuApp.GetApi<IFeishuTenantV1MailContact>();
var request = new CreateUserMailboxContactRequest
{
    Email = "contact@example.com",
    Name = "联系人名称"
};
var result = await contactApi.CreateUserMailboxContactAsync("user@example.com", request);
Console.WriteLine($"联系人创建成功: {result?.Data?.MailContactId}");
```

---

### DeleteUserMailboxContactAsync
删除用户邮箱联系人

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteUserMailboxContactAsync(
    [Path] string user_mailbox_id,
    [Path] string mail_contact_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识 | user@example.com |
| mail_contact_id | string | ✅ | 邮箱联系人 id | 123 |
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
- 删除操作不可恢复
- 需要获取邮箱联系人ID，可通过列出邮箱联系人接口获取

**代码示例**
```csharp
var contactApi = feishuApp.GetApi<IFeishuTenantV1MailContact>();
var result = await contactApi.DeleteUserMailboxContactAsync("user@example.com", "123");
Console.WriteLine($"联系人删除结果: {result.Code == 0}");
```

---

### UpdateUserMailboxContactAsync
修改用户邮箱联系人信息

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateUserMailboxContactAsync(
    [Path] string user_mailbox_id,
    [Path] string mail_contact_id,
    [Body] UpdateUserMailboxContactRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识 | user@example.com |
| mail_contact_id | string | ✅ | 邮箱联系人 id | 123 |
| request | UpdateUserMailboxContactRequest | ✅ | 更新用户邮箱联系人请求对象 | - |
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
- 使用 tenant_access_token 时，需要申请邮箱联系人资源的数据权限
- 支持部分更新，仅传递需要修改的字段

**代码示例**
```csharp
var contactApi = feishuApp.GetApi<IFeishuTenantV1MailContact>();
var request = new UpdateUserMailboxContactRequest
{
    Name = "更新后的联系人名称"
};
var result = await contactApi.UpdateUserMailboxContactAsync("user@example.com", "123", request);
Console.WriteLine($"联系人更新结果: {result.Code == 0}");
```

---

### GetUserMailboxContactPageListAsync
分页列出用户邮箱联系人

**函数签名**
```csharp
Task<FeishuApiPageListResult<MailboxContactInfo>?> GetUserMailboxContactPageListAsync(
    [Path] string user_mailbox_id,
    [Query] int page_size = 20,
    [Query] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识 | user@example.com |
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
        "mail_contact_id": "123",
        "email": "contact@example.com",
        "name": "联系人名称",
        "create_time": "2026-06-03T11:34:00+08:00"
      }
    ],
    "page_token": "evt_xxx",
    "has_more": true
  }
}
```

**说明**
- 使用 tenant_access_token 时，需要申请邮箱联系人资源的数据权限
- 支持分页查询，可通过 page_token 获取下一页数据

**代码示例**
```csharp
var contactApi = feishuApp.GetApi<IFeishuTenantV1MailContact>();
var result = await contactApi.GetUserMailboxContactPageListAsync("user@example.com");
if (result?.Data?.Items != null)
{
    foreach (var contact in result.Data.Items)
    {
        Console.WriteLine($"联系人: {contact.Name} ({contact.Email})");
    }
}
```
