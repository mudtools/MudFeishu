---
title: 邮箱别名接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份管理企业内所有用户的邮箱别名，支持邮箱别名的添加、查询与删除。
---

# IFeishuTenantV1MailAlias - 租户邮箱别名API

## 功能描述
飞书邮箱别名API接口实现了添加、查询、删除等邮箱别名管理功能。支持租户管理员通过租户访问令牌管理企业内所有用户的邮箱别名。

## 参考文档
- [从回收站删除用户邮箱地址](https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-alias/delete)
- [创建邮箱别名](https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-alias/create)
- [删除邮箱别名](https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-alias/delete-2)
- [获取邮箱别名列表](https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-alias/list)
- [查询邮箱地址状态](https://open.feishu.cn/document/server-docs/mail-v1/user/query)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| DeleteUserMailboxAsync | 从回收站删除用户邮箱地址 | TenantAccessToken | DELETE |
| CreateUserMailboxAliasAsync | 创建用户邮箱别名 | TenantAccessToken | POST |
| DeleteUserMailboxAliasAsync | 删除用户邮箱别名 | TenantAccessToken | DELETE |
| GetUserMailboxAliasPageListAsync | 获取用户邮箱所有别名 | TenantAccessToken | GET |
| QueryUserMailboxAddressAsync | 查询邮箱地址状态 | TenantAccessToken | POST |
| GetUserMailboxProfileAsync | 查询用户主邮箱地址 | TenantAccessToken | GET |

## 函数详细内容

### DeleteUserMailboxAsync
从回收站永久删除用户邮箱地址

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteUserMailboxAsync(
    [Path] string user_mailbox_id,
    [Query] string? transfer_mailbox = null,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识 | user@example.com |
| transfer_mailbox | string? | ⚪ | 用于接收转移的邮箱地址 | 888888@abc.com |
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
- 该接口会永久删除用户邮箱地址，一旦删除将无法恢复
- 支持邮件转移功能，可将被删除邮箱的邮件转移到其他邮箱
- 仅可删除位于邮箱回收站中的用户邮箱地址

**代码示例**
```csharp
var mailAliasApi = feishuApp.GetApi<IFeishuTenantV1MailAlias>();
var result = await mailAliasApi.DeleteUserMailboxAsync(
    "user@example.com",
    transfer_mailbox: "admin@company.com");
Console.WriteLine($"删除结果: {result.Code == 0}");
```

---

### CreateUserMailboxAliasAsync
创建用户邮箱别名

**函数签名**
```csharp
Task<FeishuApiResult<CreateUserMailboxAliasResult>?> CreateUserMailboxAliasAsync(
    [Path] string user_mailbox_id,
    [Body] CreateUserMailboxAliasRequest createUserMailboxAliasRequest,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识 | user@example.com |
| createUserMailboxAliasRequest | CreateUserMailboxAliasRequest | ✅ | 创建用户邮箱别名请求对象 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user_mailbox_alias": {
      "primary_email": "user@example.com",
      "email_alias": "user.alias@example.com"
    }
  }
}
```

**说明**
- 邮箱别名可用于邮件收发，作为主邮箱的补充地址
- 需要申请邮箱别名管理权限

**代码示例**
```csharp
var mailAliasApi = feishuApp.GetApi<IFeishuTenantV1MailAlias>();
var request = new CreateUserMailboxAliasRequest
{
    EmailAlias = "user.alias@example.com"
};
var result = await mailAliasApi.CreateUserMailboxAliasAsync("user@example.com", request);
Console.WriteLine($"别名创建成功: {result?.Data?.UserMailboxAlias?.EmailAliasSuffix}");
```

---

### DeleteUserMailboxAliasAsync
删除用户邮箱别名

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteUserMailboxAliasAsync(
    [Path] string user_mailbox_id,
    [Path] string alias_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识 | user@example.com |
| alias_id | string | ✅ | 别名邮箱地址ID | user_alias@xxx.xx |
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
- 删除后该别名将无法用于邮件收发
- 删除操作不可恢复

**代码示例**
```csharp
var mailAliasApi = feishuApp.GetApi<IFeishuTenantV1MailAlias>();
var result = await mailAliasApi.DeleteUserMailboxAliasAsync(
    "user@example.com",
    "user_alias@xxx.xx");
Console.WriteLine($"别名删除结果: {result.Code == 0}");
```

---

### GetUserMailboxAliasPageListAsync
获取用户邮箱所有别名

**函数签名**
```csharp
Task<FeishuApiPageListResult<EmailAlias>?> GetUserMailboxAliasPageListAsync(
    [Path] string user_mailbox_id,
    [Query] int page_size = Consts.PageSize_20,
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
        "primary_email": "user@example.com",
        "email_alias": "user.alias@example.com"
      }
    ],
    "page_token": "evt_xxx",
    "has_more": false
  }
}
```

**说明**
- 该接口一次性返回所有数据，分页参数无效（接口说明）
- 返回该用户邮箱下所有别名列表

**代码示例**
```csharp
var mailAliasApi = feishuApp.GetApi<IFeishuTenantV1MailAlias>();
var result = await mailAliasApi.GetUserMailboxAliasPageListAsync("user@example.com");
if (result?.Data?.Items != null)
{
    foreach (var alias in result.Data.Items)
    {
        Console.WriteLine($"别名: {alias.EmailAliasSuffix}");
    }
}
```

---

### QueryUserMailboxAddressAsync
查询邮箱地址状态

**函数签名**
```csharp
Task<FeishuApiResult<QueryUserMailboxAddressResult>?> QueryUserMailboxAddressAsync(
    [Body] QueryUserMailboxAddressRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | QueryUserMailboxAddressRequest | ✅ | 查询邮箱地址状态请求对象 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user_list": [
      {
        "email": "user@example.com",
        "status": 1,
        "type": 1
      },
      {
        "email": "admin@example.com",
        "status": 1,
        "type": 1
      }
    ]
  }
}
```

**说明**
- 可以输入邮箱地址，查询出该邮箱地址对应的类型以及状态
- 支持批量查询多个邮箱地址

**代码示例**
```csharp
var mailAliasApi = feishuApp.GetApi<IFeishuTenantV1MailAlias>();
var request = new QueryUserMailboxAddressRequest
{
    EmailList = new[] { "user@example.com", "admin@example.com" }
};
var result = await mailAliasApi.QueryUserMailboxAddressAsync(request);
if (result?.Data?.UserLists != null)
{
    foreach (var item in result.Data.UserLists)
    {
        Console.WriteLine($"邮箱: {item.Email}, 状态: {item.Status}");
    }
}
```

---

### GetUserMailboxProfileAsync
查询用户主邮箱地址

**函数签名**
```csharp
Task<FeishuApiResult<UserMailboxProfileResult>?> GetUserMailboxProfileAsync(
   [Path] string user_mailbox_id,
   CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "primary_email_address": "user@example.com",
    "not_found_reason": ""
  }
}
```

**说明**
- 根据用户邮箱 ID 查询其主邮箱地址
- 邮箱地址不存在时，可通过 `not_found_reason` 判断未命中原因

**接口文档**
[查询用户主邮箱地址](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox/profile)
