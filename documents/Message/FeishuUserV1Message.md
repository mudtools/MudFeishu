# 消息管理（用户权限）-（FeishuUserV1Message）

## 功能描述

消息即飞书聊天中的一条消息。使用消息管理 API 对消息进行发送、回复、编辑、撤回、转发以及查询等操作。支持发送文本、富文本、卡片、群名片、个人名片、图片、视频、音频、文件、表情包等多种消息类型。

本接口使用用户权限（UserAccessToken），适合以用户身份进行操作，例如发送消息时显示为指定用户而非应用机器人。

## 参考文档

- [消息概述 - 飞书开放平台](https://open.feishu.cn/document/server-docs/im-v1/message/intro)

## 函数列表

### 从父接口（IFeishuV1Message）继承的方法

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| RevokeMessageAsync | 撤回消息 | 用户令牌 | DELETE |
| AddMessageReactionsAsync | 添加表情回复 | 用户令牌 | POST |
| GetMessageReactionsPageListAsync | 获取表情回复列表 | 用户令牌 | GET |
| DeleteMessageReactionsAsync | 删除表情回复 | 用户令牌 | DELETE |
| PinMessageAsync | Pin 消息 | 用户令牌 | POST |
| DeletePinMessageAsync | 移除 Pin | 用户令牌 | DELETE |
| GetPinMessagePageListAsync | 获取 Pin 消息列表 | 用户令牌 | GET |

## 函数详细内容

### 撤回消息

**函数名称**：撤回指定消息

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> RevokeMessageAsync(
    [Path] string message_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌 (UserAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| message_id | string | ✅ | 待撤回的消息 ID。示例值："om_dc13264520392913993dd051dba21dcf" |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**
- 用户只能撤回自己发送的消息
- 消息撤回后，飞书客户端会显示"消息已撤回"提示
- 超过一定时间的消息可能无法撤回（具体时限参考飞书官方文档）

**代码示例**
```csharp
// 以用户身份撤回消息
public class UserMessageService
{
    private readonly IFeishuUserV1Message _userMessageClient;

    public UserMessageService(IFeishuUserV1Message userMessageClient)
    {
        _userMessageClient = userMessageClient;
    }

    public async Task RevokeUserMessageAsync(string messageId)
    {
        try
        {
            var result = await _userMessageClient.RevokeMessageAsync(messageId);
            
            if (result?.Code == 0)
            {
                Console.WriteLine("消息撤回成功");
            }
            else
            {
                Console.WriteLine($"撤回失败: {result?.Msg}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"操作异常: {ex.Message}");
        }
    }
}
```

---

### 添加表情回复

**函数名称**：给消息添加表情回复

**函数签名**
```csharp
Task<FeishuApiResult<EmojiReactionResult>?> AddMessageReactionsAsync(
    [Path] string message_id,
    [Body] EmojiReactionRequest sendMessageRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌 (UserAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| message_id | string | ✅ | 待添加表情回复的消息 ID |
| sendMessageRequest | EmojiReactionRequest | ✅ | 表情回复请求体 |
| ├─ ReactionType | string | ✅ | 表情类型，如 "OK"、"THUMBSUP"、"HEART" 等 |
| └─ UserId | string? | ⚪ | 用户ID（可选，默认当前用户） |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "reaction_id": "ZCaCIjUBVVWSrm5L-3ZTw*************sNa8dHVplEzzSfJVUVLMLcS_",
    "message_id": "om_8964d1b4*********2b31383276113",
    "reaction_type": "OK",
    "operator_id": {
      "open_id": "ou_xxxxxxxxxxxxxxxx",
      "union_id": "on_xxxxxxxxxxxxxxxx",
      "user_id": "xxxxxxxx"
    },
    "create_time": "1609094735000"
  }
}
```

**代码示例**
```csharp
// 为消息添加点赞表情
public async Task AddLikeReactionAsync(string messageId)
{
    var request = new EmojiReactionRequest
    {
        ReactionType = "THUMBSUP"  // 点赞表情
    };

    var result = await _userMessageClient.AddMessageReactionsAsync(messageId, request);
    
    if (result?.Data != null)
    {
        Console.WriteLine($"表情添加成功，ID: {result.Data.ReactionId}");
    }
}
```

---

### 获取表情回复列表

**函数名称**：获取指定消息的表情回复列表

**函数签名**
```csharp
Task<FeishuApiListResult<EmojiReactionResult>?> GetMessageReactionsPageListAsync(
    [Path] string message_id,
    [Query("reaction_type")] string reaction_type,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌 (UserAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| message_id | string | ✅ | 待查询的消息 ID |
| reaction_type | string | ✅ | 表情类型，用于筛选特定类型的表情 |
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string? | ⚪ | 分页标记 |
| user_id_type | string? | ⚪ | 用户ID类型，默认 open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**代码示例**
```csharp
// 获取消息的所有表情回复
public async Task GetMessageReactionsAsync(string messageId)
{
    // 查询点赞表情
    var result = await _userMessageClient.GetMessageReactionsPageListAsync(
        message_id: messageId,
        reaction_type: "THUMBSUP",
        page_size: 50
    );

    if (result?.Data?.Items != null)
    {
        Console.WriteLine($"该消息共有 {result.Data.Items.Count} 个点赞");
        foreach (var reaction in result.Data.Items)
        {
            Console.WriteLine($"- 用户 {reaction.OperatorId?.OpenId} 于 {reaction.CreateTime} 点赞");
        }
    }
}
```

---

### 删除表情回复

**函数名称**：删除指定消息的某一表情回复

**函数签名**
```csharp
Task<FeishuApiResult<EmojiReactionResult>?> DeleteMessageReactionsAsync(
    [Path] string message_id,
    [Path] string reaction_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌 (UserAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| message_id | string | ✅ | 待删除表情回复的消息 ID |
| reaction_id | string | ✅ | 待删除的表情回复 ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**代码示例**
```csharp
// 删除自己的表情回复
public async Task RemoveReactionAsync(string messageId, string reactionId)
{
    var result = await _userMessageClient.DeleteMessageReactionsAsync(messageId, reactionId);
    
    if (result?.Code == 0)
    {
        Console.WriteLine("表情回复已删除");
    }
}
```

---

### Pin 消息

**函数名称**：Pin 一条指定的消息

**函数签名**
```csharp
Task<FeishuApiResult<PinDataResult>?> PinMessageAsync(
    [Body] MessageRequest messageRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌 (UserAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| messageRequest | MessageRequest | ✅ | Pin 消息请求体 |
| └─ MessageId | string | ✅ | 要 Pin 的消息 ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "pin_id": "pin_xxxxxxxxxxxxxxxx",
    "message_id": "om_xxxxxxxxxxxxxxxx",
    "chat_id": "oc_xxxxxxxxxxxxxxxx",
    "operator_id": {
      "open_id": "ou_xxxxxxxxxxxxxxxx",
      "union_id": "on_xxxxxxxxxxxxxxxx",
      "user_id": "xxxxxxxx"
    },
    "create_time": "1609094735000"
  }
}
```

**代码示例**
```csharp
// Pin 重要消息
public async Task PinImportantMessageAsync(string messageId)
{
    var request = new MessageRequest
    {
        MessageId = messageId
    };

    var result = await _userMessageClient.PinMessageAsync(request);
    
    if (result?.Data != null)
    {
        Console.WriteLine($"消息已 Pin，Pin ID: {result.Data.PinId}");
    }
}
```

---

### 移除 Pin

**函数名称**：移除一条指定消息的 Pin

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeletePinMessageAsync(
    [Path] string message_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌 (UserAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| message_id | string | ✅ | 待移除 Pin 的消息 ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**代码示例**
```csharp
// 取消 Pin 消息
public async Task UnpinMessageAsync(string messageId)
{
    var result = await _userMessageClient.DeletePinMessageAsync(messageId);
    
    if (result?.Code == 0)
    {
        Console.WriteLine("Pin 已移除");
    }
}
```

---

### 获取 Pin 消息列表

**函数名称**：获取指定群、指定时间范围内的所有 Pin 消息

**函数签名**
```csharp
Task<FeishuApiPageListResult<PinInfo>?> GetPinMessagePageListAsync(
    [Query("chat_id")] string chat_id,
    [Query("start_time")] string? start_time,
    [Query("end_time")] string? end_time,
    [Query("page_size")] int? page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌 (UserAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| chat_id | string | ✅ | 群组 ID |
| start_time | string? | ⚪ | 起始时间，毫秒级时间戳 |
| end_time | string? | ⚪ | 结束时间，毫秒级时间戳 |
| page_size | int? | ⚪ | 分页大小，默认10 |
| page_token | string? | ⚪ | 分页标记 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**代码示例**
```csharp
// 获取群组内的所有 Pin 消息
public async Task GetChatPinnedMessagesAsync(string chatId)
{
    var startTime = DateTimeOffset.Now.AddDays(-30).ToUnixTimeMilliseconds().ToString();
    var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
    
    var result = await _userMessageClient.GetPinMessagePageListAsync(
        chat_id: chatId,
        start_time: startTime,
        end_time: endTime,
        page_size: 50
    );

    if (result?.Data?.Items != null)
    {
        Console.WriteLine($"群内共有 {result.Data.Items.Count} 条 Pin 消息");
        foreach (var pin in result.Data.Items)
        {
            Console.WriteLine($"- 消息ID: {pin.MessageId}, Pin时间: {pin.CreateTime}");
        }
    }
}
```

## 说明

### 用户权限与租户权限的区别

| 特性 | 用户权限 (UserAccessToken) | 租户权限 (TenantAccessToken) |
|-----|---------------------------|----------------------------|
| 身份标识 | 以指定用户身份操作 | 以应用/机器人身份操作 |
| 适用场景 | 需要模拟用户行为的场景 | 服务端系统通知、群发 |
| 消息发送者 | 显示为对应用户 | 显示为应用机器人 |
| 权限范围 | 受用户自身权限限制 | 受应用权限范围限制 |

### 获取用户令牌

使用本接口前，需要通过 OAuth2 流程获取用户授权令牌：

```csharp
// 用户授权流程通常在客户端完成
// 获取到用户的 access_token 后，可以通过以下方式使用：

// 方式1：通过依赖注入获取（推荐）
public class MyService
{
    private readonly IFeishuUserV1Message _userMessageClient;
    
    public MyService(IFeishuUserV1Message userMessageClient)
    {
        _userMessageClient = userMessageClient;
    }
}

// 方式2：通过 ICurrentUserId 获取当前用户上下文
// 接口继承自 ICurrentUserId，会自动使用当前用户的 token
```
