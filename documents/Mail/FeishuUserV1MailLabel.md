# IFeishuUserV1MailLabel - 用户邮箱标签API

## 功能描述
飞书邮箱标签API接口实现了修改、查询、删除等邮件标签功能。支持用户通过用户访问令牌管理自己的邮箱标签。

## 参考文档
- [更新标签](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-label/patch)
- [列出标签](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-label/list)
- [获取标签信息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-label/get)
- [删除标签](https://open.feishhu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-label/delete)
- [创建标签](https://open.feishhu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-label/create)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| UpdateUserMailboxLabelAsync | 更新标签 | UserAccessToken | PATCH |
| GetUserMailboxLabelListAsync | 列出标签 | UserAccessToken | GET |
| GetUserMailboxLabelAsync | 获取标签信息 | UserAccessToken | GET |
| DeleteUserMailboxLabelAsync | 删除标签 | UserAccessToken | DELETE |
| CreateUserMailboxLabelAsync | 创建标签 | UserAccessToken | POST |

## 函数详细内容

### UpdateUserMailboxLabelAsync
更新标签

**函数签名**
```csharp
Task<FeishuApiResult<UserMailboxLabelOopsResult>?> UpdateUserMailboxLabelAsync(
    [Path] string user_mailbox_id,
    [Path] string label_id,
    [Body] UpdateUserMailboxLabelRequest updateUserMailboxLabelRequest,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| label_id | string | ✅ | 标签ID，创建标签成功后返回的标签ID，或可通过列出标签、获取邮件详情等接口获得 | 7620003644728938013 |
| updateUserMailboxLabelRequest | UpdateUserMailboxLabelRequest | ✅ | 更新用户邮箱标签请求对象 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "label_id": "7620003644728938013",
    "name": "更新后的标签名称",
    "color": "blue",
    "update_time": "2026-06-03T11:41:00+08:00"
  }
}
```

**说明**
- 更新用户指定标签的名字、颜色等信息
- 使用 user_access_token 时，只能更新当前授权用户的邮箱标签

**代码示例**
```csharp
var labelApi = feishuApp.GetApi<IFeishuUserV1MailLabel>();
var request = new UpdateUserMailboxLabelRequest
{
    Name = "更新后的标签名称",
    Color = "blue"
};
var result = await labelApi.UpdateUserMailboxLabelAsync("me", "7620003644728938013", request);
Console.WriteLine($"标签更新成功: {result?.Data?.Name}");
```

---

### GetUserMailboxLabelListAsync
列出标签

**函数签名**
```csharp
Task<FeishuApiResult<GetUserMailboxLabelListResult>?> GetUserMailboxLabelListAsync(
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
    "items": [
      {
        "label_id": "7620003644728938013",
        "name": "重要邮件",
        "color": "red",
        "unread_count": 5
      }
    ]
  }
}
```

**说明**
- 列出邮件标签，包括ID、名称、颜色、未读信息等内容
- 使用 user_access_token 时，只能列出当前授权用户的邮箱标签

**代码示例**
```csharp
var labelApi = feishuApp.GetApi<IFeishuUserV1MailLabel>();
var result = await labelApi.GetUserMailboxLabelListAsync("me");
if (result?.Data?.Items != null)
{
    foreach (var label in result.Data.Items)
    {
        Console.WriteLine($"标签: {label.Name}, 未读: {label.UnreadCount}");
    }
}
```

---

### GetUserMailboxLabelAsync
获取标签信息

**函数签名**
```csharp
Task<FeishuApiResult<GetUserMailboxLabelResult>?> GetUserMailboxLabelAsync(
    [Path] string user_mailbox_id,
    [Path] string label_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| label_id | string | ✅ | 标签ID，创建标签成功后返回的标签ID，或可通过列出标签、获取邮件详情等接口获得 | 7620003644728938013 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "label_id": "7620003644728938013",
    "name": "重要邮件",
    "color": "red",
    "unread_count": 5,
    "create_time": "2026-06-03T11:41:00+08:00"
  }
}
```

**说明**
- 根据指定ID，获取邮件标签信息，包括名称、未读数据、颜色等信息
- 使用 user_access_token 时，只能获取当前授权用户的邮箱标签信息

**代码示例**
```csharp
var labelApi = feishhuApp.GetApi<IFeishuUserV1MailLabel>();
var result = await labelApi.GetUserMailboxLabelAsync("me", "7620003644728938013");
Console.WriteLine($"标签名称: {result?.Data?.Name}, 颜色: {result?.Data?.Color}");
```

---

### DeleteUserMailboxLabelAsync
删除标签

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteUserMailboxLabelAsync(
    [Path] string user_mailbox_id,
    [Path] string label_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| label_id | string | ✅ | 标签ID，创建标签成功后返回的标签ID，或可通过列出标签、获取邮件详情等接口获得 | 7620003644728938013 |
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
- 删除用户指定的标签，注意，删除的标签无法恢复
- 使用 user_access_token 时，只能删除当前授权用户的邮箱标签

**代码示例**
```csharp
var labelApi = feishhuApp.GetApi<IFeishuUserV1MailLabel>();
var result = await labelApi.DeleteUserMailboxLabelAsync("me", "7620003644728938013");
Console.WriteLine($"标签删除结果: {result.Code == 0}");
```

---

### CreateUserMailboxLabelAsync
创建标签

**函数签名**
```csharp
Task<FeishhuApiResult<UserMailboxLabelOopsResult>?> CreateUserMailboxLabelAsync(
    [Path] string user_mailbox_id,
    [Body] CreateUserMailboxLabelRequest createUserMailboxLabelRequest,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| createUserMailboxLabelRequest | CreateUserMailboxLabelRequest | ✅ | 创建用户邮箱标签请求对象 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "label_id": "7620003644728938013",
    "name": "新标签",
    "color": "green",
    "create_time": "2026-06-03T11:41:00+08:00"
  }
}
```

**说明**
- 根据用户指定的名称、颜色等信息，创建邮件标签
- 使用 user_access_token 时，只能为当前授权用户创建邮箱标签

**代码示例**
```csharp
var labelApi = feishhuApp.GetApi<IFeishuUserV1MailLabel>();
var request = new CreateUserMailboxLabelRequest
{
    Name = "新标签",
    Color = "green"
};
var result = await labelApi.CreateUserMailboxLabelAsync("me", request);
Console.WriteLine($"标签创建成功: {result?.Data?.LabelId}");
```
