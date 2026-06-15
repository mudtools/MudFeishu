# IFeishuUserV1MailPublicMailbox - 用户公共邮箱API

## 功能描述
飞书公共邮箱API接口实现公共邮箱管理、公共邮箱成员管理以及公共邮箱别名管理等管理功能。支持用户通过用户访问令牌访问和管理自己有权限的公共邮箱。

## 参考文档
- [分页查询所有公共邮箱](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox/list)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| GetPublicMailboxPageListAsync | 分页查询所有公共邮箱 | UserAccessToken | GET |

## 函数详细内容

### GetPublicMailboxPageListAsync
分页查询所有公共邮箱

**函数签名**
```csharp
Task<FeishuApiPageListResult<PublicMailboxInfo>?> GetPublicMailboxPageListAsync(
    [Query] int page_size = 20,
    [Query] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| page_size | int | ⚪ | 分页大小，默认值：20 | 20 |
| page_token | string? | ⚪ | 分页标记 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "public_mailbox_id": "xxxxxxxxxxxxxxx",
        "email": "test_public_mailbox@xxx.xx",
        "name": "公共邮箱名称"
      }
    ],
    "page_token": "evt_xxx",
    "has_more": true
  }
}
```

**说明**
- 分页查询所有公共邮箱
- 使用 user_access_token 时，只能查看当前用户有权限的公共邮箱

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuUserV1MailPublicMailbox>();
var result = await publicMailboxApi.GetPublicMailboxPageListAsync();
if (result?.Data?.Items != null)
{
    foreach (var mailbox in result.Data.Items)
    {
        Console.WriteLine($"公共邮箱: {mailbox.Name} ({mailbox.Email})");
    }
}
```
