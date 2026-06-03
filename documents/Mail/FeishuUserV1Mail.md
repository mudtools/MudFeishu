# IFeishuUserV1Mail - 用户邮箱事件API

## 功能描述
飞书邮箱事件API接口实现了事件订阅、获取订阅状态、取消订阅等功能。支持用户通过用户访问令牌管理邮箱事件通知。

## 参考文档
- [订阅事件](https://open.feishu.cn/document/mail-v1/user_mailbox-event/subscribe)
- [获取订阅状态](https://open.feishu.cn/document/mail-v1/user_mailbox-event/subscription)
- [取消订阅事件](https://open.feishu.cn/document/mail-v1/user_mailbox-event/unsubscribe)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| SubscribeUserMailboxEventAsync | 订阅邮箱事件 | UserAccessToken | POST |
| GetSubscribeUserMailboxEventAsync | 获取邮箱事件订阅状态 | UserAccessToken | GET |
| UnSubscribeUserMailboxEventAsync | 取消订阅邮箱事件 | UserAccessToken | POST |

## 函数详细内容

### SubscribeUserMailboxEventAsync
订阅用户邮箱事件通知

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> SubscribeUserMailboxEventAsync(
    [Path] string user_mailbox_id,
    [Body] SubscribeUserMailboxEventRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| request | SubscribeUserMailboxEventRequest | ✅ | 订阅事件请求对象 | - |
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
- 需要申请邮箱事件订阅权限
- 订阅后可通过WebSocket接收邮箱事件通知

**代码示例**
```csharp
var mailApi = feishuApp.GetApi<IFeishuUserV1Mail>();
var request = new SubscribeUserMailboxEventRequest
{
    // 填充订阅配置
};
var result = await mailApi.SubscribeUserMailboxEventAsync("me", request);
Console.WriteLine($"订阅结果: {result.Code == 0}");
```

---

### GetSubscribeUserMailboxEventAsync
获取用户邮箱事件订阅状态

**函数签名**
```csharp
Task<FeishuApiResult<GetSubscribeUserMailboxEventResult>?> GetSubscribeUserMailboxEventAsync(
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
    "is_subscribed": true,
    "subscribe_time": "2026-06-03T11:34:00+08:00"
  }
}
```

**说明**
- 返回当前邮箱的事件订阅状态
- 可用于检查订阅是否生效

**代码示例**
```csharp
var mailApi = feishuApp.GetApi<IFeishuUserV1Mail>();
var result = await mailApi.GetSubscribeUserMailboxEventAsync("me");
if (result?.Data?.IsSubscribed == true)
{
    Console.WriteLine("邮箱事件已订阅");
}
```

---

### UnSubscribeUserMailboxEventAsync
取消订阅用户邮箱事件

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UnSubscribeUserMailboxEventAsync(
    [Path] string user_mailbox_id,
    [Body] UnSubscribeUserMailboxEventRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_mailbox_id | string | ✅ | 用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。 | user@example.com |
| request | UnSubscribeUserMailboxEventRequest | ✅ | 取消订阅请求对象 | - |
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
- 取消订阅后将不再接收邮箱事件通知
- 如需再次接收需重新订阅

**代码示例**
```csharp
var mailApi = feishuApp.GetApi<IFeishuUserV1Mail>();
var request = new UnSubscribeUserMailboxEventRequest();
var result = await mailApi.UnSubscribeUserMailboxEventAsync("me", request);
Console.WriteLine($"取消订阅结果: {result.Code == 0}");
```
