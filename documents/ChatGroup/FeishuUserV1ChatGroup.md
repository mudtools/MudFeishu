# 飞书群组 API（用户级）

## 接口名称
**飞书群组 API -（IFeishuUserV1ChatGroup）**

## 功能描述
飞书群组 OpenAPI 提供了群组管理能力，包括解散群、更新群信息、获取群信息、管理群置顶以及获取群分享链接等。
当前接口使用用户令牌访问，适应于用户应用场景。

## 参考文档
- [飞书官方文档 - 群组](https://open.feishu.cn/document/server-docs/group/chat/intro)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| UpdateChatGroupByIdAsync | 更新群信息 | 用户令牌 | PUT |
| DeleteChatGroupAsync | 解散群组 | 用户令牌 | DELETE |
| UpdateChatModerationAsync | 更新群发言权限 | 用户令牌 | PUT |
| GetChatGroupInoByIdAsync | 获取群基本信息 | 用户令牌 | GET |
| PutChatGroupTopNoticeAsync | 设置群置顶 | 用户令牌 | POST |
| DeleteChatGroupTopNoticeAsync | 撤销群置顶 | 用户令牌 | POST |
| GetChatGroupPageListAsync | 分页获取群列表 | 用户令牌 | GET |
| GetChatGroupPageListByKeywordAsync | 关键词搜索群列表 | 用户令牌 | GET |
| GetChatGroupModeratorPageListByIdAsync | 获取群发言模式及名单 | 用户令牌 | GET |
| GetChatGroupShareLinkByIdAsync | 获取群分享链接 | 用户令牌 | GET |

---

## 函数详细内容

### 更新群信息

**函数名称**：更新群信息

**函数签名**：
```csharp
Task<FeishuApiResult<CreateUpdateChatResult>?> UpdateChatGroupByIdAsync(
    [Path] string chat_id,
    [Body] UpdateChatRequest updateChatRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID，示例："oc_a0553eda9014c201e6969b478895c230" |
| updateChatRequest | UpdateChatRequest | ✅ | 更新群聊请求体 |
| user_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "chat_id": "oc_a0553eda9014c201e6969b478895c230",
    "name": "更新后的群名称"
  }
}
```

**说明**：可更新群头像、群名称、群描述、群配置以及群主等信息。当前用户必须是群主才能执行此操作。

**代码示例**：
```csharp
public class UserChatGroupService
{
    private readonly IFeishuUserV1ChatGroup _chatGroupClient;

    public UserChatGroupService(IFeishuUserV1ChatGroup chatGroupClient)
    {
        _chatGroupClient = chatGroupClient;
    }

    public async Task UpdateGroupAvatarAsync(string chatId, string avatarUrl)
    {
        var request = new UpdateChatRequest
        {
            avatar = avatarUrl
        };

        var result = await _chatGroupClient.UpdateChatGroupByIdAsync(chatId, request);
        if (result?.Code == 0)
        {
            Console.WriteLine("群头像更新成功");
        }
    }
}
```

---

### 解散群组

**函数名称**：解散群组

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteChatGroupAsync(
    [Path] string chat_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：通过 API 解散群组后，群聊天记录将不会保存。当前用户必须是群主才能解散群组。

---

### 更新群发言权限

**函数名称**：更新群发言权限

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UpdateChatModerationAsync(
    [Path] string chat_id,
    [Body] UpdateChatModerationRequest updateChatModerationRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| updateChatModerationRequest | UpdateChatModerationRequest | ✅ | 更新群发言权限请求体 |
| user_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：可设置为所有群成员可发言、仅群主或管理员可发言、指定群成员可发言。

---

### 获取群基本信息

**函数名称**：获取群基本信息

**函数签名**：
```csharp
Task<FeishuApiResult<GetChatGroupInfoResult>?> GetChatGroupInoByIdAsync(
    [Path] string chat_id,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| user_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "chat_id": "oc_a0553eda9014c201e6969b478895c230",
    "name": "测试群组",
    "avatar": "https://example.com/avatar.png",
    "description": "群组描述",
    "owner_id": "ou_xxxxxxxxxxxxxxxx",
    "member_count": 25
  }
}
```

**代码示例**：
```csharp
public async Task DisplayGroupDetailsAsync(string chatId)
{
    var result = await _chatGroupClient.GetChatGroupInoByIdAsync(chatId);
    if (result?.Data != null)
    {
        Console.WriteLine($"群名称: {result.Data.name}");
        Console.WriteLine($"群描述: {result.Data.description}");
        Console.WriteLine($"成员数: {result.Data.member_count}");
    }
}
```

---

### 设置群置顶

**函数名称**：设置群置顶

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> PutChatGroupTopNoticeAsync(
    [Path] string chat_id,
    [Body] ChatTopNoticeRequest chatTopNoticeRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| chatTopNoticeRequest | ChatTopNoticeRequest | ✅ | 群置顶操作请求体 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：可将群中的某一条消息，或群公告置顶展示。

---

### 撤销群置顶

**函数名称**：撤销群置顶

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteChatGroupTopNoticeAsync(
    [Path] string chat_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

---

### 分页获取群列表

**函数名称**：分页获取群列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<ChatItemInfo>?> GetChatGroupPageListAsync(
    [Query("user_id_type")] string user_id_type = "open_id",
    [Query("sort_type")] string sort_type = "ByCreateTimeAsc",
    [Query("page_size")] int? page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| user_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |
| sort_type | string | ⚪ | 排序方式，默认值：ByCreateTimeAsc |
| page_size | int? | ⚪ | 分页大小，默认值：10 |
| page_token | string? | ⚪ | 分页标记 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "chat_id": "oc_a0553eda9014c201e6969b478895c230",
        "name": "测试群组"
      }
    ],
    "has_more": true
  }
}
```

---

### 关键词搜索群列表

**函数名称**：关键词搜索群列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<ChatItemInfo>?> GetChatGroupPageListByKeywordAsync(
    [Query("query")] string? query = "",
    [Query("user_id_type")] string user_id_type = "open_id",
    [Query("sort_type")] string sort_type = "ByCreateTimeAsc",
    [Query("page_size")] int? page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| query | string? | ⚪ | 关键词 |
| user_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |
| sort_type | string | ⚪ | 排序方式，默认值：ByCreateTimeAsc |
| page_size | int? | ⚪ | 分页大小，默认值：10 |
| page_token | string? | ⚪ | 分页标记 |

---

### 获取群发言模式及名单

**函数名称**：获取群发言模式及名单

**函数签名**：
```csharp
Task<FeishuApiResult<ChatGroupModeratorPageListResult>?> GetChatGroupModeratorPageListByIdAsync(
    [Path] string chat_id,
    [Query("user_id_type")] string user_id_type = "open_id",
    [Query("page_size")] int? page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| user_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |
| page_size | int? | ⚪ | 分页大小，默认值：10 |
| page_token | string? | ⚪ | 分页标记 |

---

### 获取群分享链接

**函数名称**：获取群分享链接

**函数签名**：
```csharp
Task<FeishuApiResult<ShareLinkDataResult>?> GetChatGroupShareLinkByIdAsync(
    [Path] string chat_id,
    [Body] ShareLinkRequest shareLinkRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| shareLinkRequest | ShareLinkRequest | ✅ | 获取群分享链接请求体 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "link": "https://applink.feishu.cn/client/chat/chatter/add?token=xxx"
  }
}
```
