---
title: 邮箱地址接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份查询自己的主邮箱地址，可判断邮箱地址是否命中。
---

# IFeishuUserV1MailAlias - 用户邮箱地址API

## 功能描述
飞书邮箱地址API接口实现了查询用户主邮箱地址功能。支持用户通过用户访问令牌查询自己所授权邮箱的主邮箱地址。

## 参考文档
- [查询用户主邮箱地址](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox/profile)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| GetUserMailboxProfileAsync | 查询用户主邮箱地址 | UserAccessToken | GET |

## 函数详细内容

### GetUserMailboxProfileAsync
查询用户主邮箱地址

**函数签名**
```csharp
Task<FeishuApiResult<UserMailboxProfileResult>?> GetUserMailboxProfileAsync(
   [Path] string user_mailbox_id,
   CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

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
- 邮箱地址不存在时可通过 `not_found_reason` 判断未命中原因
- 使用 user_access_token 调用时，`user_mailbox_id` 可传占位符 `me`，表示当前授权用户的主邮箱

**代码示例**
```csharp
var aliasApi = feishuApp.GetApi<IFeishuUserV1MailAlias>();
var result = await aliasApi.GetUserMailboxProfileAsync("me");
if (result?.Code == 0)
{
    Console.WriteLine($"主邮箱地址: {result.Data?.PrimaryEmailAddress}");
}
```
