# 消息管理（租户权限）-（FeishuTenantV1Message）

## 功能描述

消息即飞书聊天中的一条消息。使用消息管理 API 对消息进行发送、回复、编辑、撤回、转发以及查询等操作。支持发送文本、富文本、卡片、群名片、个人名片、图片、视频、音频、文件、表情包等多种消息类型。

本接口使用租户权限（TenantAccessToken），适合服务端应用代表企业/组织进行操作。

## 参考文档

- [消息概述 - 飞书开放平台](https://open.feishu.cn/document/server-docs/im-v1/message/intro)

## 函数列表

### 消息管理

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| SendMessageAsync | 发送消息 | 租户令牌 | POST |
| ReplyMessageAsync | 回复消息 | 租户令牌 | POST |
| EditMessageAsync | 编辑消息 | 租户令牌 | PUT |
| ReceiveMessageAsync | 转发消息 | 租户令牌 | POST |
| MergeReceiveMessageAsync | 合并转发消息 | 租户令牌 | POST |
| ReceiveThreadsAsync | 转发话题 | 租户令牌 | POST |
| CreateMessageFollowUpAsync | 添加消息跟随气泡 | 租户令牌 | POST |
| GetMessageReadUsesAsync | 获取消息已读用户 | 租户令牌 | GET |
| GetHistoryMessageAsync | 获取历史消息 | 租户令牌 | GET |
| GetContentListByMessageIdAsync | 根据ID获取消息内容 | 租户令牌 | GET |
| GetMessageFile | 获取消息资源文件（小文件） | 租户令牌 | GET |
| GetMessageLargeFile | 获取消息资源文件（大文件） | 租户令牌 | GET |

### 文件管理

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| DownFileAsync | 下载文件（小文件） | 租户令牌 | GET |
| DownLargeFileAsync | 下载文件（大文件） | 租户令牌 | GET |
| DownImageAsync | 下载图片（小文件） | 租户令牌 | GET |
| DownLargeImageAsync | 下载图片（大文件） | 租户令牌 | GET |
| UploadFileAsync | 上传文件 | 租户令牌 | POST |
| UploadImageAsync | 上传图片 | 租户令牌 | POST |

### 消息加急

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| MessageUrgentAppAsync | 应用内加急 | 租户令牌 | PATCH |
| MessageUrgentSMSAsync | 短信加急 | 租户令牌 | PATCH |
| MessageUrgentPhoneAsync | 电话加急 | 租户令牌 | PATCH |

### URL 预览

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| UpdateUrlPreviewAsync | 更新 URL 预览 | 租户令牌 | POST |

### 继承自父接口（IFeishuV1Message）

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| RevokeMessageAsync | 撤回消息 | 租户令牌 | DELETE |
| AddMessageReactionsAsync | 添加表情回复 | 租户令牌 | POST |
| GetMessageReactionsPageListAsync | 获取表情回复列表 | 租户令牌 | GET |
| DeleteMessageReactionsAsync | 删除表情回复 | 租户令牌 | DELETE |
| PinMessageAsync | Pin 消息 | 租户令牌 | POST |
| DeletePinMessageAsync | 移除 Pin | 租户令牌 | DELETE |
| GetPinMessagePageListAsync | 获取 Pin 消息列表 | 租户令牌 | GET |

## 函数详细内容

### 发送消息

**函数签名**
```csharp
Task<FeishuApiResult<MessageDataResult>?> SendMessageAsync(
    [Body] SendMessageRequest sendMessageRequest,
    [Query("receive_id_type")] string receive_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| sendMessageRequest | SendMessageRequest | ✅ | 发送消息请求体 |
| ├─ ReceiveId | string | ✅ | 消息接收者ID，根据 receive_id_type 类型填写 |
| ├─ MsgType | string | ✅ | 消息类型：text/post/image/file/audio/media/share_chat/share_user/interactive/system |
| └─ Content | object | ✅ | 消息内容，根据 MsgType 变化 |
| receive_id_type | string | ⚪ | 用户ID类型：open_id/union_id/user_id/email/chat_id，默认 open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "message_id": "om_dc13264520392913993dd051dba21dcf",
    "root_id": "om_xxxxxxxxxxxxxxxx",
    "parent_id": "om_xxxxxxxxxxxxxxxx",
    "thread_id": "omt_xxxxxxxxxxxxxxxx",
    "msg_type": "text",
    "create_time": "1609094735000",
    "update_time": "1609094735000",
    "deleted": false,
    "updated": false,
    "chat_id": "oc_xxxxxxxxxxxxxxxx",
    "sender": {
      "sender_id": {
        "open_id": "ou_xxxxxxxxxxxxxxxx",
        "union_id": "on_xxxxxxxxxxxxxxxx",
        "user_id": "xxxxxxxx"
      },
      "sender_type": "user",
      "tenant_key": "xxxxxxxx"
    },
    "body": {
      "content": "{\"text\":\"Hello World\"}"
    }
  }
}
```

**代码示例**
```csharp
// 发送文本消息给用户
public class MessageService
{
    private readonly IFeishuTenantV1Message _messageClient;

    public MessageService(IFeishuTenantV1Message messageClient)
    {
        _messageClient = messageClient;
    }

    public async Task SendTextMessageAsync(string userOpenId, string content)
    {
        var request = new SendMessageRequest
        {
            ReceiveId = userOpenId,
            MsgType = "text",
            Content = new MessageTextContent
            {
                Text = content
            }
        };

        var result = await _messageClient.SendMessageAsync(request);
        
        if (result?.Data != null)
        {
            Console.WriteLine($"消息发送成功，ID: {result.Data.MessageId}");
        }
    }

    // 发送卡片消息
    public async Task SendCardMessageAsync(string chatId)
    {
        var request = new SendMessageRequest
        {
            ReceiveId = chatId,
            MsgType = "interactive",
            Content = new MessageCardContent
            {
                Card = new Card
                {
                    Header = new Header
                    {
                        Title = new Title { Content = "审批通知" }
                    },
                    Elements = new List<Element>
                    {
                        new MarkdownElement { Content = "您的请假申请已通过审批" },
                        new ActionElement 
                        { 
                            Actions = new List<Action>
                            {
                                new ButtonAction { Label = "查看详情", Url = "https://example.com" }
                            }
                        }
                    }
                }
            }
        };

        var result = await _messageClient.SendMessageAsync(request, receive_id_type: "chat_id");
        Console.WriteLine($"卡片消息发送: {result?.Data?.MessageId}");
    }
}
```

---

### 回复消息

**函数签名**
```csharp
Task<FeishuApiResult<MessageDataResult>?> ReplyMessageAsync(
    [Path] string message_id,
    [Body] ReplyMessageRequest replyMessageRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| message_id | string | ✅ | 待回复的消息 ID |
| replyMessageRequest | ReplyMessageRequest | ✅ | 回复消息请求体 |
| ├─ Content | object | ✅ | 回复内容 |
| └─ MsgType | string | ✅ | 消息类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**代码示例**
```csharp
// 回复用户消息
public async Task ReplyToUserAsync(string originalMessageId, string replyContent)
{
    var request = new ReplyMessageRequest
    {
        MsgType = "text",
        Content = new MessageTextContent
        {
            Text = replyContent
        }
    };

    var result = await _messageClient.ReplyMessageAsync(originalMessageId, request);
    Console.WriteLine($"回复成功: {result?.Data?.MessageId}");
}
```

---

### 编辑消息

**函数签名**
```csharp
Task<FeishuApiResult<MessageDataResult>?> EditMessageAsync(
    [Path] string message_id,
    [Body] EditMessageRequest editMessageRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**说明**
- 仅支持编辑文本和富文本消息
- 卡片消息请使用更新卡片接口

---

### 转发消息

**函数签名**
```csharp
Task<FeishuApiResult<ReceiveMessageResult>?> ReceiveMessageAsync(
    [Path] string message_id,
    [Body] ReceiveMessageRequest receiveMessageRequest,
    [Query("receive_id_type")] string receive_id_type = "open_id",
    [Query("uuid")] string? uuid = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| message_id | string | ✅ | 待转发的消息 ID |
| receiveMessageRequest | ReceiveMessageRequest | ✅ | 转发请求体 |
| └─ ReceiveId | string | ✅ | 接收者 ID |
| receive_id_type | string | ⚪ | 接收者ID类型，默认 open_id |
| uuid | string? | ⚪ | 去重标识，相同 uuid 1小时内只能转发成功一次 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

---

### 获取历史消息

**函数签名**
```csharp
Task<FeishuApiPageListResult<HistoryMessageData>?> GetHistoryMessageAsync(
    [Query("container_id_type")] string container_id_type,
    [Query("container_id")] string container_id,
    [Query("start_time")] string? start_time = null,
    [Query("end_time")] string? end_time = null,
    [Query("sort_type")] string? sort_type = "ByCreateTimeAsc",
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| container_id_type | string | ✅ | 容器类型：chat（单聊/群聊）/ thread（话题） |
| container_id | string | ✅ | 容器 ID |
| start_time | string? | ⚪ | 起始时间，秒级时间戳 |
| end_time | string? | ⚪ | 结束时间，秒级时间戳 |
| sort_type | string? | ⚪ | 排序方式：ByCreateTimeAsc/ByCreateTimeDesc |
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string? | ⚪ | 分页标记 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**代码示例**
```csharp
// 获取群聊历史消息
public async Task GetChatHistoryAsync(string chatId)
{
    var result = await _messageClient.GetHistoryMessageAsync(
        container_id_type: "chat",
        container_id: chatId,
        start_time: "1608594809",
        end_time: "1609296809",
        sort_type: "ByCreateTimeDesc",
        page_size: 50
    );

    if (result?.Data?.Items != null)
    {
        foreach (var message in result.Data.Items)
        {
            Console.WriteLine($"[{message.CreateTime}] {message.Sender?.SenderId?.OpenId}: {message.Body?.Content}");
        }
    }
}
```

---

### 撤回消息

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> RevokeMessageAsync(
    [Path] string message_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| message_id | string | ✅ | 待撤回的消息 ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**
- 机器人可以撤回自己发送的消息
- 群主可以撤回群内的指定消息

---

### 上传文件

**函数签名**
```csharp
Task<FeishuApiResult<FileUploadResult>?> UploadFileAsync(
    [FormContent] UploadMessageFileRequest uploadFileRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| uploadFileRequest | UploadMessageFileRequest | ✅ | 文件上传请求体 |
| ├─ FileName | string | ✅ | 文件名 |
| ├─ FileType | string | ✅ | 文件类型：mp4/pdf/doc/xls... |
| └─ File | Stream/FileInfo | ✅ | 文件流或文件信息 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "file_key": "file_456a92d6-c6ea-4de4-ac3f-7afcf44ac78g"
  }
}
```

**代码示例**
```csharp
// 上传并发送文件
public async Task SendFileMessageAsync(string userOpenId, string filePath)
{
    // 上传文件
    var uploadRequest = new UploadMessageFileRequest
    {
        FileName = Path.GetFileName(filePath),
        FileType = "pdf",
        File = new FileInfo(filePath)
    };

    var uploadResult = await _messageClient.UploadFileAsync(uploadRequest);
    
    if (uploadResult?.Data?.FileKey != null)
    {
        // 发送文件消息
        var sendRequest = new SendMessageRequest
        {
            ReceiveId = userOpenId,
            MsgType = "file",
            Content = new MessageFileContent
            {
                FileKey = uploadResult.Data.FileKey
            }
        };

        await _messageClient.SendMessageAsync(sendRequest);
    }
}
```

---

### 消息加急（应用内）

**函数签名**
```csharp
Task<FeishuApiResult<MessageUrgentResult>?> MessageUrgentAppAsync(
    [Path] string message_id,
    [Body] MessageUrgentRequest sendMessageRequest,
    [Query("receive_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| message_id | string | ✅ | 待加急的消息 ID |
| sendMessageRequest | MessageUrgentRequest | ✅ | 加急请求体 |
| └─ UserIdList | List<string> | ✅ | 需要加急的用户ID列表 |
| user_id_type | string | ⚪ | 用户ID类型，默认 open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**
- 加急仅在飞书客户端内通知
- 每个用户每天接收加急的次数有限制
