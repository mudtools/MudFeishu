# 批量消息管理 -（FeishuTenantV1BatchMessage）

## 功能描述

用于管理给多个用户或者多个部门发送消息，支持发送文本、富文本、卡片、群名片、个人名片、图片、视频、音频、文件、表情包等类型。提供批量发送、撤回、查询已读状态、查询发送进度等功能。

适用场景：
- 企业向多个员工批量发送通知公告
- 向多个部门群发系统消息
- 批量消息的发送状态追踪和撤回管理

## 参考文档

- [批量发送消息 - 飞书开放平台](https://open.feishu.cn/document/server-docs/im-v1/batch_message/send-messages-in-batches)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| BatchSendTextMessageAsync | 批量发送文本消息 | 租户令牌 | POST |
| BatchSendRichTextMessageAsync | 批量发送富文本消息 | 租户令牌 | POST |
| BatchSendImageMessageAsync | 批量发送图片消息 | 租户令牌 | POST |
| BatchSendGroupShareMessageAsync | 批量发送群分享消息 | 租户令牌 | POST |
| RevokeMessageAsync | 撤回批量消息 | 租户令牌 | DELETE |
| GetUserReadMessageInfosAsync | 获取消息已读用户信息 | 租户令牌 | GET |
| GetBatchMessageProgressAsync | 获取批量消息发送/撤回进度 | 租户令牌 | GET |

## 函数详细内容

### 批量发送文本消息

**函数签名**
```csharp
Task<FeishuApiResult<BatchMessageResult>?> BatchSendTextMessageAsync(
    [Body] BatchSenderTextMessageRequest sendMessageRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| sendMessageRequest | BatchSenderTextMessageRequest | ✅ | 批量发送文本消息请求体 |
| ├─ UserIds | List<string> | ⚪ | 用户 user_id 列表，示例：["7cdcc7c2","ca51d83b"] |
| ├─ UnionIds | List<string> | ⚪ | 用户 union_id 列表 |
| ├─ OpenIds | List<string> | ⚪ | 用户 open_id 列表 |
| ├─ DepartmentIds | List<string> | ⚪ | 部门 ID 列表 |
| ├─ MsgType | string | ✅ | 消息类型，固定为 "text" |
| └─ Content | MessageTextContent | ✅ | 文本内容，包含 text 字段 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

> **注意**：至少需要填写一种用户标识（UserIds/UnionIds/OpenIds/DepartmentIds 之一）

**响应**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "message_id": "om_dc13264520392913993dd051dba21dcf",
    "invalid_user_ids": ["invalid_id_1"],
    "invalid_open_ids": [],
    "invalid_union_ids": [],
    "invalid_department_ids": []
  }
}
```

**说明**
- 消息将异步发送，返回的 message_id 可用于后续查询和撤回
- 响应中 invalid_* 字段包含无效的用户/部门标识
- 消息内容长度限制参考飞书官方文档

**代码示例**
```csharp
// 批量发送文本通知给多个员工
public class NotificationService
{
    private readonly IFeishuTenantV1BatchMessage _batchMessageClient;

    public NotificationService(IFeishuTenantV1BatchMessage batchMessageClient)
    {
        _batchMessageClient = batchMessageClient;
    }

    public async Task SendHolidayNoticeAsync(List<string> employeeOpenIds)
    {
        var request = new BatchSenderTextMessageRequest
        {
            OpenIds = employeeOpenIds,
            Content = new MessageTextContent
            {
                Text = "【放假通知】全体员工请注意，国庆节放假时间为10月1日至10月7日，共7天。祝大家节日愉快！"
            }
        };

        var result = await _batchMessageClient.BatchSendTextMessageAsync(request);
        
        if (result?.Data != null)
        {
            Console.WriteLine($"消息ID: {result.Data.MessageId}");
            if (result.Data.InvalidOpenIds?.Any() == true)
            {
                Console.WriteLine($"无效用户: {string.Join(",", result.Data.InvalidOpenIds)}");
            }
        }
    }
}
```

---

### 批量发送富文本消息

**函数签名**
```csharp
Task<FeishuApiResult<BatchMessageResult>?> BatchSendRichTextMessageAsync(
    [Body] BatchSenderRichTextMessageRequest sendMessageRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| sendMessageRequest | BatchSenderRichTextMessageRequest | ✅ | 批量发送富文本消息请求体 |
| ├─ UserIds | List<string> | ⚪ | 用户 user_id 列表 |
| ├─ UnionIds | List<string> | ⚪ | 用户 union_id 列表 |
| ├─ OpenIds | List<string> | ⚪ | 用户 open_id 列表 |
| ├─ DepartmentIds | List<string> | ⚪ | 部门 ID 列表 |
| ├─ MsgType | string | ✅ | 消息类型，固定为 "post" |
| └─ Content | MessagePostContent | ✅ | 富文本内容，支持图文混排 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "message_id": "om_dc13264520392913993dd051dba21dcf",
    "invalid_user_ids": [],
    "invalid_open_ids": [],
    "invalid_union_ids": [],
    "invalid_department_ids": []
  }
}
```

**代码示例**
```csharp
// 批量发送富文本通知
public async Task SendRichTextNoticeAsync(List<string> departmentIds)
{
    var request = new BatchSenderRichTextMessageRequest
    {
        DepartmentIds = departmentIds,
        Content = new MessagePostContent
        {
            Post = new PostContent
            {
                Title = "重要通知",
                Content = new List<List<PostItem>>
                {
                    new List<PostItem>
                    {
                        new PostItem { Tag = "text", Text = "全体员工请注意：" }
                    },
                    new List<PostItem>
                    {
                        new PostItem { Tag = "a", Text = "点击查看详情", Href = "https://example.com" }
                    }
                }
            }
        }
    };

    var result = await _batchMessageClient.BatchSendRichTextMessageAsync(request);
    Console.WriteLine($"发送结果: {result?.Data?.MessageId}");
}
```

---

### 批量发送图片消息

**函数签名**
```csharp
Task<FeishuApiResult<BatchMessageResult>?> BatchSendImageMessageAsync(
    [Body] BatchSenderMessageImageRequest sendMessageRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| sendMessageRequest | BatchSenderMessageImageRequest | ✅ | 批量发送图片消息请求体 |
| ├─ UserIds | List<string> | ⚪ | 用户 user_id 列表 |
| ├─ UnionIds | List<string> | ⚪ | 用户 union_id 列表 |
| ├─ OpenIds | List<string> | ⚪ | 用户 open_id 列表 |
| ├─ DepartmentIds | List<string> | ⚪ | 部门 ID 列表 |
| ├─ MsgType | string | ✅ | 消息类型，固定为 "image" |
| └─ Content | MessageImageContent | ✅ | 图片内容，包含 image_key 字段 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**
- image_key 需先通过上传图片接口获取

---

### 批量发送群分享消息

**函数签名**
```csharp
Task<FeishuApiResult<BatchMessageResult>?> BatchSendGroupShareMessageAsync(
    [Body] BatchSenderMessageGroupShareRequest sendMessageRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| sendMessageRequest | BatchSenderMessageGroupShareRequest | ✅ | 批量发送群分享消息请求体 |
| ├─ UserIds | List<string> | ⚪ | 用户 user_id 列表 |
| ├─ UnionIds | List<string> | ⚪ | 用户 union_id 列表 |
| ├─ OpenIds | List<string> | ⚪ | 用户 open_id 列表 |
| ├─ DepartmentIds | List<string> | ⚪ | 部门 ID 列表 |
| ├─ MsgType | string | ✅ | 消息类型，固定为 "share_chat" |
| └─ Content | MessageGroupShareContent | ✅ | 群分享内容，包含 chat_id 字段 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

---

### 撤回批量消息

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> RevokeMessageAsync(
    [Path] string batch_message_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| batch_message_id | string | ✅ | 待撤回的批量消息任务 ID，即批量发送消息接口返回值中的 message_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**
- 只能撤回通过批量发送接口发送的消息
- 撤回操作是异步的，需要通过 GetBatchMessageProgressAsync 查询撤回进度

**代码示例**
```csharp
// 撤回已发送的批量消息
public async Task RevokeBatchMessageAsync(string messageId)
{
    var result = await _batchMessageClient.RevokeMessageAsync(messageId);
    if (result?.Code == 0)
    {
        Console.WriteLine("撤回请求已提交，正在异步处理");
        
        // 查询撤回进度
        var progress = await _batchMessageClient.GetBatchMessageProgressAsync(messageId);
        Console.WriteLine($"撤回进度: {progress?.Data?.BatchMessageRecallProgress?.SuccessCount}");
    }
}
```

---

### 获取消息已读用户信息

**函数签名**
```csharp
Task<FeishuApiResult<BatchMessageReadStatusResult>?> GetUserReadMessageInfosAsync(
    [Path] string batch_message_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| batch_message_id | string | ✅ | 批量消息任务 ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "read_user": {
      "total_count": 100,
      "read_count": 85
    }
  }
}
```

**代码示例**
```csharp
// 查询消息阅读统计
public async Task CheckReadStatusAsync(string messageId)
{
    var result = await _batchMessageClient.GetUserReadMessageInfosAsync(messageId);
    if (result?.Data?.ReadUser != null)
    {
        var readUser = result.Data.ReadUser;
        double readRate = (double)readUser.ReadCount / readUser.TotalCount * 100;
        Console.WriteLine($"已读人数: {readUser.ReadCount}/{readUser.TotalCount} ({readRate:F1}%)");
    }
}
```

---

### 获取批量消息进度

**函数签名**
```csharp
Task<FeishuApiResult<BatchMessageProgressResult>?> GetBatchMessageProgressAsync(
    [Path] string batch_message_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌 (TenantAccessToken)

**参数**

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| batch_message_id | string | ✅ | 批量消息任务 ID |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "batch_message_send_progress": {
      "total_count": 100,
      "success_count": 95,
      "fail_count": 5
    },
    "batch_message_recall_progress": {
      "total_count": 100,
      "success_count": 90,
      "fail_count": 10
    }
  }
}
```

**说明**
- 可用于查询批量发送消息的发送进度
- 也可用于查询批量撤回消息的撤回进度

**代码示例**
```csharp
// 轮询查询批量消息发送进度
public async Task MonitorSendProgressAsync(string messageId)
{
    while (true)
    {
        var result = await _batchMessageClient.GetBatchMessageProgressAsync(messageId);
        var progress = result?.Data?.BatchMessageSendProgress;
        
        if (progress != null)
        {
            Console.WriteLine($"进度: {progress.SuccessCount}/{progress.TotalCount}");
            
            if (progress.SuccessCount + progress.FailCount >= progress.TotalCount)
            {
                Console.WriteLine("发送完成");
                break;
            }
        }
        
        await Task.Delay(2000); // 等待2秒后再次查询
    }
}
```
