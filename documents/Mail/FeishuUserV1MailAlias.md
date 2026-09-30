# 邮箱地址 API（用户令牌）

## 接口名称
**飞书邮箱地址 API -（IFeishuUserV1MailAlias）**

## 功能描述
飞书邮箱地址 API 接口实现了查询用户主邮箱地址功能。
当前接口使用用户令牌（UserAccessToken）访问，`user_mailbox_id` 可使用占位符 `me` 表示当前授权用户的主邮箱。

## 参考文档
- [查询用户主邮箱地址](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox/profile)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| GetUserMailboxProfileAsync | 查询用户主邮箱地址 | UserAccessToken | GET |

## 函数详细内容

### 查询用户主邮箱地址

**函数签名**：
```csharp
Task<FeishuApiResult<UserMailboxProfileResult>?> GetUserMailboxProfileAsync(
   [Path] string user_mailbox_id,
   CancellationToken cancellationToken = default);
```

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时可使用占位符 `me` 表示当前授权用户的主邮箱，示例值：`user@example.com` |

**说明**：根据用户邮箱 ID 查询其主邮箱地址；邮箱地址不存在时可通过响应中的 `not_found_reason` 判断未命中原因。
