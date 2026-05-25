# 应用消息流卡片接口 - FeishuTenantV2AppCardMessageStream

## 接口名称
**应用消息流卡片管理（租户令牌）** - (FeishuTenantV2AppCardMessageStream)

## 功能描述
应用消息流卡片是飞书为应用提供的消息触达能力，让应用可以直接在消息流发送消息，重要消息能更快触达用户。支持创建、更新、删除消息流卡片，以及设置即时提醒、更新快捷操作按钮等功能。

## 参考文档
- [飞书开放平台 - IM接口概述](https://open.feishu.cn/document/im-v2/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateCardMessageStreamAsync | 创建应用消息流卡片 | 租户令牌 | POST |
| UpdateCardMessageStreamAsync | 更新应用消息流卡片 | 租户令牌 | PUT |
| DeleteCardMessageStreamAsync | 删除应用消息流卡片 | 租户令牌 | DELETE |
| BotTimeSentiveAsync | 机器人单聊即时提醒 | 租户令牌 | PATCH |
| UpdateCardMessageStreamButtonAsync | 更新消息流卡片按钮 | 租户令牌 | PUT |
| FeedCardsByFeedCardIdAsync | 群组/机器人即时提醒 | 租户令牌 | PATCH |

---

## 函数详细内容

### CreateCardMessageStreamAsync - 创建应用消息流卡片

创建应用消息流卡片，向指定用户发送卡片消息。

#### 函数签名

```csharp
Task<FeishuApiResult<CreateAppCardMessageStreamResult>?> CreateCardMessageStreamAsync(
    [Body] CreateAppCardMessageStreamRequest appCardMessageStreamRequest,
    [Query("user_id_type")] string user_id_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| appCardMessageStreamRequest | CreateAppCardMessageStreamRequest | ✅ | 创建应用消息流卡片请求体 | - |
| ├─ user_id | string | ✅ | 用户 ID | "ou_abc123" |
| ├─ card_id | string | ✅ | 卡片实体 ID | "7355372766134157313" |
| ├─ scene | object | ✅ | 场景配置 | - |
| ├─ ├─ scene_type | int | ✅ | 场景类型：1单聊/2群聊 | 1 |
| user_id_type | string | ⚪ | 用户 ID 类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "feed_card_id": "feed_xxx",
    "create_time": 1705312800
  }
}
```

#### 说明
- 创建的卡片会显示在用户的消息流中
- 需要先通过卡片接口创建卡片实体获取 card_id

#### 代码示例

```csharp
public class AppCardMessageService
{
    private readonly IFeishuTenantV2AppCardMessageStream _appCardClient;

    public AppCardMessageService(IFeishuTenantV2AppCardMessageStream appCardClient)
    {
        _appCardClient = appCardClient;
    }

    public async Task<string> SendCardToUserAsync(string userId, string cardId)
    {
        var request = new CreateAppCardMessageStreamRequest
        {
            UserId = userId,
            CardId = cardId,
            Scene = new SceneConfig
            {
                SceneType = 1  // 单聊场景
            }
        };

        var result = await _appCardClient.CreateCardMessageStreamAsync(request);
        if (result?.Code == 0)
        {
            Console.WriteLine($"卡片发送成功，Feed ID: {result.Data?.FeedCardId}");
            return result.Data?.FeedCardId;
        }
        return string.Empty;
    }
}
```

---

### UpdateCardMessageStreamAsync - 更新应用消息流卡片

更新已发送的应用消息流卡片内容。

#### 函数签名

```csharp
Task<FeishuApiResult<UpdateAppCardMessageStreamResult>?> UpdateCardMessageStreamAsync(
    [Body] UpdateAppCardMessageStreamRequest appCardMessageStreamRequest,
    [Query("user_id_type")] string user_id_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| appCardMessageStreamRequest | UpdateAppCardMessageStreamRequest | ✅ | 更新应用消息流卡片请求体 | - |
| ├─ feed_card_ids | string[] | ✅ | 消息流卡片 ID 列表 | ["feed_xxx"] |
| ├─ card_id | string | ✅ | 新的卡片实体 ID | "7355372766134157314" |
| user_id_type | string | ⚪ | 用户 ID 类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "updated_count": 2
  }
}
```

#### 说明
- 支持批量更新多个消息流卡片
- 更新后用户消息流中的卡片内容会同步刷新

#### 代码示例

```csharp
public async Task UpdateCardMessageAsync(string[] feedCardIds, string newCardId)
{
    var request = new UpdateAppCardMessageStreamRequest
    {
        FeedCardIds = feedCardIds,
        CardId = newCardId
    };

    var result = await _appCardClient.UpdateCardMessageStreamAsync(request);
    if (result?.Code == 0)
    {
        Console.WriteLine($"成功更新 {result.Data?.UpdatedCount} 条消息流卡片");
    }
}
```

---

### DeleteCardMessageStreamAsync - 删除应用消息流卡片

用于删除应用消息流卡片。

#### 函数签名

```csharp
Task<FeishuApiResult<DeleteAppCardMessageStreamResult>?> DeleteCardMessageStreamAsync(
    [Body] DeleteAppCardMessageStreamRequest appCardMessageStreamRequest,
    [Query("user_id_type")] string user_id_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| appCardMessageStreamRequest | DeleteAppCardMessageStreamRequest | ✅ | 删除应用消息流卡片请求体 | - |
| ├─ feed_card_ids | string[] | ✅ | 消息流卡片 ID 列表 | ["feed_xxx"] |
| user_id_type | string | ⚪ | 用户 ID 类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "deleted_count": 1
  }
}
```

#### 说明
- 删除后卡片将从用户消息流中消失
- 支持批量删除多个卡片

#### 代码示例

```csharp
public async Task DeleteCardMessagesAsync(string[] feedCardIds)
{
    var request = new DeleteAppCardMessageStreamRequest
    {
        FeedCardIds = feedCardIds
    };

    var result = await _appCardClient.DeleteCardMessageStreamAsync(request);
    if (result?.Code == 0)
    {
        Console.WriteLine($"成功删除 {result.Data?.DeletedCount} 条消息流卡片");
    }
}
```

---

### BotTimeSentiveAsync - 机器人单聊即时提醒

可将机器人对话在消息列表中置顶展示，打开飞书首页即可处理重要任务。

#### 函数签名

```csharp
Task<FeishuApiResult<BotTimeSentiveResult>?> BotTimeSentiveAsync(
    [Body] BotTimeSentiveRequest timeSentiveRequest,
    [Query("user_id_type")] string user_id_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| timeSentiveRequest | BotTimeSentiveRequest | ✅ | 机器人单聊即时提醒请求体 | - |
| ├─ user_id | string | ✅ | 用户 ID | "ou_abc123" |
| ├─ urgent | bool | ✅ | 是否紧急提醒 | true |
| user_id_type | string | ⚪ | 用户 ID 类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "success": true
  }
}
```

#### 说明
- 紧急提醒会在消息列表中置顶显示
- 适用于重要通知、待办提醒等场景

#### 代码示例

```csharp
public async Task SetUrgentReminderAsync(string userId)
{
    var request = new BotTimeSentiveRequest
    {
        UserId = userId,
        Urgent = true
    };

    var result = await _appCardClient.BotTimeSentiveAsync(request);
    if (result?.Code == 0)
    {
        Console.WriteLine("即时提醒设置成功");
    }
}
```

---

### UpdateCardMessageStreamButtonAsync - 更新消息流卡片按钮

为群组消息、机器人消息的消息流卡片添加、更新、删除快捷操作按钮。

#### 函数签名

```csharp
Task<FeishuApiResult<BotTimeSentiveResult>?> UpdateCardMessageStreamButtonAsync(
    [Body] UpdateCardMessageStreamButtonRequest updateCardMessageStreamButtonRequest,
    [Query("user_id_type")] string user_id_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| updateCardMessageStreamButtonRequest | UpdateCardMessageStreamButtonRequest | ✅ | 更新消息流卡片按钮请求体 | - |
| ├─ feed_card_id | string | ✅ | 消息流卡片 ID | "feed_xxx" |
| ├─ buttons | object[] | ✅ | 按钮列表 | - |
| ├─ ├─ text | string | ✅ | 按钮文本 | "查看详情" |
| ├─ ├─ action | string | ✅ | 按钮动作 | "open_url" |
| user_id_type | string | ⚪ | 用户 ID 类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "success": true
  }
}
```

#### 说明
- 支持添加多个快捷操作按钮
- 按钮可以设置跳转链接、回调等动作

#### 代码示例

```csharp
public async Task UpdateCardButtonsAsync(string feedCardId)
{
    var request = new UpdateCardMessageStreamButtonRequest
    {
        FeedCardId = feedCardId,
        Buttons = new[]
        {
            new CardButton
            {
                Text = "审批通过",
                Action = "callback",
                Value = new { action = "approve", status = 1 }
            },
            new CardButton
            {
                Text = "查看详情",
                Action = "open_url",
                Url = "https://example.com/detail"
            }
        }
    };

    var result = await _appCardClient.UpdateCardMessageStreamButtonAsync(request);
    if (result?.Code == 0)
    {
        Console.WriteLine("卡片按钮更新成功");
    }
}
```

---

### FeedCardsByFeedCardIdAsync - 群组/机器人即时提醒

即时提醒能力是飞书在消息列表中提供的强提醒能力，当有重要通知或任务需要及时触达用户，可将群组或机器人对话在消息列表中置顶展示，打开飞书首页即可处理重要任务。

#### 函数签名

```csharp
Task<FeishuApiResult<BotTimeSentiveResult>?> FeedCardsByFeedCardIdAsync(
    [Path] string feed_card_id,
    [Body] FeedCardsByFeedCardIdRequest feedCardsByFeedCardIdRequest,
    [Query("user_id_type")] string user_id_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| feed_card_id | string | ✅ | 消息流卡片 ID | "feed_xxx" |
| feedCardsByFeedCardIdRequest | FeedCardsByFeedCardIdRequest | ✅ | 即时提醒请求体 | - |
| ├─ urgent | bool | ✅ | 是否紧急提醒 | true |
| ├─ expire_time | int | ⚪ | 提醒过期时间戳 | 1705399200 |
| user_id_type | string | ⚪ | 用户 ID 类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "success": true
  }
}
```

#### 说明
- 可将特定消息流卡片置顶显示
- 支持设置过期时间，过期后自动取消置顶
- 适用于审批提醒、任务通知等紧急消息

#### 代码示例

```csharp
public async Task SetFeedCardUrgentAsync(string feedCardId)
{
    var request = new FeedCardsByFeedCardIdRequest
    {
        Urgent = true,
        ExpireTime = (int)DateTimeOffset.Now.AddHours(24).ToUnixTimeSeconds()
    };

    var result = await _appCardClient.FeedCardsByFeedCardIdAsync(feedCardId, request);
    if (result?.Code == 0)
    {
        Console.WriteLine("即时提醒设置成功，24小时后自动过期");
    }
}
```
