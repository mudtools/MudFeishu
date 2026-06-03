# IFeshuUserV1MailRule - 用户邮箱收信规则API

## 功能描述
飞书邮箱收信规则API接口实现了修改、查询、删除等邮箱收信规则管理功能。
支持用户通过用户访问令牌管理自己的邮箱收信规则。

## 参考文档
- [创建收信规则](https://open.feishu.cn/document/mail-v1/user_mailbox-rule/create)
- [删除收信规则](https://open.feishu.cn/document/mail-v1/user_mailbox-rule/delete)
- [更新收信规则](https://open.feishu.cn/document/mail-v1/user_mailbox-rule/update)
- [列出收信规则](https://open.feishu.cn/document/mail-v1/user_mailbox-rule/list)
- [对收信规则进行排序](https://open.feishu.cn/document/mail-v1/user_mailbox-rule/reorder)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateUserMailboxRuleAsync | 创建收信规则 | UserAccessToken | POST |
| DeleteUserMailboxRuleAsync | 删除收信规则 | UserAccessToken | DELETE |
| UpdateUserMailboxRuleAsync | 更新收信规则 | UserAccessToken | PUT |
| GetMailboxRuleListAsync | 列出收信规则 | UserAccessToken | GET |
| ReorderUserMailboxRuleAsync | 对收信规则进行排序 | UserAccessToken | POST |

## 函数详细内容

### CreateUserMailboxRuleAsync
创建收信规则

**函数签名**
```csharp
Task<FeishuApiResult<CreateUserMailboxRuleResult>?> CreateUserMailboxRuleAsync(
    [Path] string user_mailbox_id,
    [Body] CreateUserMailboxRuleRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| request | CreateUserMailboxRuleRequest | ✅ | 创建用户邮箱收信规则请求对象，包含待创建的收信规则信息。 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象。 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "rule_id": "123123123",
    "name": "规则名称",
    "create_time": "2026-06-03T11:41:00+08:00"
  }
}
```

**说明**
- 创建收信规则。使用 tenant_access_token 时，需要申请收信规则资源的数据权限。
- 创建后可设置条件（发件人、主题、收件人等）和操作（移动到文件夹、标记、删除等）。

**代码示例**
```csharp
var ruleApi = feishuApp.GetApi<IFeshuUserV1MailRule>();
var request = new CreateUserMailboxRuleRequest
{
    Name = "重要邮件规则",
    Condition = new RuleCondition { Sender = "boss@example.com" },
    Action = new RuleAction { MoveToFolder = "重要邮件" }
};
var result = await ruleApi.CreateUserMailboxRuleAsync("me", request);
Console.WriteLine($"规则创建成功: {result?.Data?.RuleId}");
```

---

### DeleteUserMailboxRuleAsync
删除收信规则

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteUserMailboxRuleAsync(
    [Path] string user_mailbox_id,
    [Path] string rule_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| rule_id | string | ✅ | 规则 id，获取方式见 [列出收信规则](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-rule/list) | 123123123 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象。 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- 删除收信规则。使用 tenant_access_token 时，需要申请收信规则资源的数据权限。
- 删除操作不可恢复。

**代码示例**
```csharp
var ruleApi = feishuApp.GetApi<IFeshuUserV1MailRule>();
var result = await ruleApi.DeleteUserMailboxRuleAsync("me", "123123123");
Console.WriteLine($"规则删除结果: {result.Code == 0}");
```

---

### UpdateUserMailboxRuleAsync
更新收信规则

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateUserMailboxRuleAsync(
    [Path] string user_mailbox_id,
    [Path] string rule_id,
    [Body] UpdateUserMailboxRuleRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| rule_id | string | ✅ | 规则 id，获取方式见 [列出收信规则](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-rule/list) | 123123123 |
| request | UpdateUserMailboxRuleRequest | ✅ | 更新用户邮箱收信规则请求对象，包含待更新的收信规则信息。 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象。 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- 更新收信规则。使用 tenant_access_token 时，需要申请收信规则资源的数据权限。
- 可更新规则名称、条件、操作等。

**代码示例**
```csharp
var ruleApi = feishuApp.GetApi<IFeshuUserV1MailRule>();
var request = new UpdateUserMailboxRuleRequest
{
    Name = "更新后的规则名称",
    Condition = new RuleCondition { Sender = "updated@example.com" }
};
var result = await ruleApi.UpdateUserMailboxRuleAsync("me", "123123123", request);
Console.WriteLine($"规则更新结果: {result.Code == 0}");
```

---

### GetMailboxRuleListAsync
列出收信规则

**函数签名**
```csharp
Task<FeishuApiResult<GetMailboxRuleListResult>?> GetMailboxRuleListAsync(
    [Path] string user_mailbox_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象。 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "rule_id": "123123123",
        "name": "规则名称",
        "create_time": "2026-06-03T11:41:00+08:00"
      }
    ]
  }
}
```

**说明**
- 列出收信规则。使用 tenant_access_token 时，需要申请收信规则资源的数据权限。
- 返回所有收信规则列表，按排序顺序排列。

**代码示例**
```csharp
var ruleApi = feishuApp.GetApi<IFeshuUserV1MailRule>();
var result = await ruleApi.GetMailboxRuleListAsync("me");
if (result?.Data?.Items != null)
{
    foreach (var rule in result.Data.Items)
    {
        Console.WriteLine($"规则: {rule.Name} ({rule.RuleId})");
    }
}
```

---

### ReorderUserMailboxRuleAsync
对收信规则进行排序

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> ReorderUserMailboxRuleAsync(
    [Path] string user_mailbox_id,
    [Body] ReorderUserMailboxRuleRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| request | ReorderUserMailboxRuleRequest | ✅ | 重新排序用户邮箱收信规则请求对象，包含新的收信规则顺序信息。 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象。 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- 对收信规则进行排序。使用 tenant_access_token 时，需要申请收信规则资源的数据权限。
- 当使用该接口时，需要传递所有规则 id。
- 排序影响规则的匹配顺序，排在前面的规则优先匹配。

**代码示例**
```csharp
var ruleApi = feishuApp.GetApi<IFeshuUserV1MailRule>();
var request = new ReorderUserMailboxRuleRequest
{
    RuleIds = new List<string> { "rule_id_3", "rule_id_1", "rule_id_2" }
};
var result = await ruleApi.ReorderUserMailboxRuleAsync("me", request);
Console.WriteLine($"规则排序结果: {result.Code == 0}");
```
