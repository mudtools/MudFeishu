# 飞书群组 API（租户级）

## 接口名称

**飞书群组 API -（IFeishuTenantV1ChatGroup）**

## 功能描述

飞书群组 OpenAPI 提供了群组管理能力，包括创建群、解散群、更新群信息、获取群信息、管理群置顶以及获取群分享链接等。
当前接口使用租户令牌访问，适应于租户应用场景。

## 参考文档

- [飞书官方文档 - 群组](https://open.feishu.cn/document/server-docs/group/chat/intro)

## 函数列表

| 函数名称                                   | 功能描述       | 认证方式 | HTTP 方法 |
| -------------------------------------- | ---------- | ---- | ------- |
| CreateChatGroupAsync                   | 创建群聊       | 租户令牌 | POST    |
| UpdateChatGroupByIdAsync               | 更新群信息      | 租户令牌 | PUT     |
| DeleteChatGroupAsync                   | 解散群组       | 租户令牌 | DELETE  |
| UpdateChatModerationAsync              | 更新群发言权限    | 租户令牌 | PUT     |
| GetChatGroupInoByIdAsync               | 获取群基本信息    | 租户令牌 | GET     |
| PutChatGroupTopNoticeAsync             | 设置群置顶      | 租户令牌 | POST    |
| DeleteChatGroupTopNoticeAsync          | 撤销群置顶      | 租户令牌 | POST    |
| GetChatGroupPageListAsync              | 分页获取群列表    | 租户令牌 | GET     |
| GetChatGroupPageListByKeywordAsync     | 关键词搜索群列表   | 租户令牌 | GET     |
| GetChatGroupModeratorPageListByIdAsync | 获取群发言模式及名单 | 租户令牌 | GET     |
| GetChatGroupShareLinkByIdAsync         | 获取群分享链接    | 租户令牌 | GET     |

***

## 函数详细内容

### 创建群聊

**函数名称**：创建群聊

**函数签名**：

```csharp
Task<FeishuApiResult<CreateUpdateChatResult>?> CreateChatGroupAsync(
    [Body] CreateChatRequest createChatRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    [Query("set_bot_manager")] bool? set_bot_manager = false,
    [Query("uuid")] string? uuid = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数                | 类型                | 必填 | 说明                                     |
| ----------------- | ----------------- | -- | -------------------------------------- |
| createChatRequest | CreateChatRequest | ✅  | 创建群聊请求体                                |
| user\_id\_type    | string            | ⚪  | 用户 ID 类型，默认值："open\_id"                |
| set\_bot\_manager | bool?             | ⚪  | 是否设置机器人为管理员，默认值：false                  |
| uuid              | string?           | ⚪  | 去重唯一标识，10小时内相同 uuid + owner\_id 只创建一个群 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "chat_id": "oc_a0553eda9014c201e6969b478895c230",
    "name": "测试群组",
    "avatar": "https://example.com/avatar.png",
    "description": "这是一个测试群组"
  }
}
```

**说明**：创建时支持设置群头像、群名称、群主以及群类型等配置，同时支持邀请群成员、群机器人入群。

**代码示例**：

```csharp
public class ChatGroupService
{
    private readonly IFeishuTenantV1ChatGroup _chatGroupClient;

    public ChatGroupService(IFeishuTenantV1ChatGroup chatGroupClient)
    {
        _chatGroupClient = chatGroupClient;
    }

    public async Task CreateProjectGroupAsync(string projectName, List<string> memberIds)
    {
        var request = new CreateChatRequest
        {
            name = $"{projectName}项目组",
            description = $"{projectName}项目协作群组",
            owner_id = "ou_xxxxxxxxxxxxxxxx",
            members = memberIds.Select(id => new Member { member_id = id, member_type = "user" }).ToList()
        };

        var result = await _chatGroupClient.CreateChatGroupAsync(request);
        if (result?.Data != null)
        {
            Console.WriteLine($"群创建成功，群ID: {result.Data.chat_id}");
        }
    }
}
```

***

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

**认证**：租户令牌

**参数**：

| 参数                | 类型                | 必填 | 说明                                             |
| ----------------- | ----------------- | -- | ---------------------------------------------- |
| chat\_id          | string            | ✅  | 群 ID，示例："oc\_a0553eda9014c201e6969b478895c230" |
| updateChatRequest | UpdateChatRequest | ✅  | 更新群聊请求体                                        |
| user\_id\_type    | string            | ⚪  | 用户 ID 类型，默认值："open\_id"                        |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "chat_id": "oc_a0553eda9014c201e6969b478895c230",
    "name": "更新后的群名称",
    "avatar": "https://example.com/new_avatar.png"
  }
}
```

**说明**：可更新群头像、群名称、群描述、群配置以及群主等信息。

**代码示例**：

```csharp
public async Task UpdateGroupNameAsync(string chatId, string newName)
{
    var request = new UpdateChatRequest
    {
        name = newName
    };

    var result = await _chatGroupClient.UpdateChatGroupByIdAsync(chatId, request);
    if (result?.Code == 0)
    {
        Console.WriteLine("群名称更新成功");
    }
}
```

***

### 解散群组

**函数名称**：解散群组

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteChatGroupAsync(
    [Path] string chat_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数       | 类型     | 必填 | 说明                                             |
| -------- | ------ | -- | ---------------------------------------------- |
| chat\_id | string | ✅  | 群 ID，示例："oc\_a0553eda9014c201e6969b478895c230" |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：通过 API 解散群组后，群聊天记录将不会保存。

**代码示例**：

```csharp
public async Task DisbandGroupAsync(string chatId)
{
    var result = await _chatGroupClient.DeleteChatGroupAsync(chatId);
    if (result?.Code == 0)
    {
        Console.WriteLine("群组已解散");
    }
}
```

***

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

**认证**：租户令牌

**参数**：

| 参数                          | 类型                          | 必填 | 说明                      |
| --------------------------- | --------------------------- | -- | ----------------------- |
| chat\_id                    | string                      | ✅  | 群 ID                    |
| updateChatModerationRequest | UpdateChatModerationRequest | ✅  | 更新群发言权限请求体              |
| user\_id\_type              | string                      | ⚪  | 用户 ID 类型，默认值："open\_id" |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：可设置为所有群成员可发言、仅群主或管理员可发言、指定群成员可发言。

**代码示例**：

```csharp
public async Task SetOnlyOwnerCanSpeakAsync(string chatId)
{
    var request = new UpdateChatModerationRequest
    {
        moderation_setting = "restricted_mode",
        restricted_mode_setting = new RestrictedModeSetting
        {
            restricted_mode_switch = true,
            speak_ids = new List<string>()
        }
    };

    await _chatGroupClient.UpdateChatModerationAsync(chatId, request);
}
```

***

### 获取群基本信息

**函数名称**：获取群基本信息

**函数签名**：

```csharp
Task<FeishuApiResult<GetChatGroupInfoResult>?> GetChatGroupInoByIdAsync(
    [Path] string chat_id,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数             | 类型     | 必填 | 说明                      |
| -------------- | ------ | -- | ----------------------- |
| chat\_id       | string | ✅  | 群 ID                    |
| user\_id\_type | string | ⚪  | 用户 ID 类型，默认值："open\_id" |

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
    "member_count": 25,
    "add_member_permission": "all_members",
    "share_card_permission": "allowed"
  }
}
```

**代码示例**：

```csharp
public async Task DisplayGroupInfoAsync(string chatId)
{
    var result = await _chatGroupClient.GetChatGroupInoByIdAsync(chatId);
    if (result?.Data != null)
    {
        Console.WriteLine($"群名称: {result.Data.name}");
        Console.WriteLine($"成员数: {result.Data.member_count}");
        Console.WriteLine($"群主ID: {result.Data.owner_id}");
    }
}
```

***

### 设置群置顶

**函数名称**：设置群置顶

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> PutChatGroupTopNoticeAsync(
    [Path] string chat_id,
    [Body] ChatTopNoticeRequest chatTopNoticeRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数                   | 类型                   | 必填 | 说明       |
| -------------------- | -------------------- | -- | -------- |
| chat\_id             | string               | ✅  | 群 ID     |
| chatTopNoticeRequest | ChatTopNoticeRequest | ✅  | 群置顶操作请求体 |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：可将群中的某一条消息，或群公告置顶展示。

***

### 撤销群置顶

**函数名称**：撤销群置顶

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteChatGroupTopNoticeAsync(
    [Path] string chat_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数       | 类型     | 必填 | 说明   |
| -------- | ------ | -- | ---- |
| chat\_id | string | ✅  | 群 ID |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：撤销指定群组中的置顶消息或群公告。

***

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

**认证**：租户令牌

**参数**：

| 参数             | 类型      | 必填 | 说明                                                        |
| -------------- | ------- | -- | --------------------------------------------------------- |
| user\_id\_type | string  | ⚪  | 用户 ID 类型，默认值："open\_id"                                   |
| sort\_type     | string  | ⚪  | 排序方式，ByCreateTimeAsc/ByActiveTimeDesc，默认值：ByCreateTimeAsc |
| page\_size     | int?    | ⚪  | 分页大小，默认值：10                                               |
| page\_token    | string? | ⚪  | 分页标记，第一次请求不填                                              |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "chat_id": "oc_a0553eda9014c201e6969b478895c230",
        "name": "测试群组",
        "avatar": "https://example.com/avatar.png",
        "description": "群组描述"
      }
    ],
    "page_token": "new_page_token",
    "has_more": true
  }
}
```

**说明**：分页获取当前 access\_token 所代表的用户或者机器人所在的群列表。

**代码示例**：

```csharp
public async Task ListAllGroupsAsync()
{
    string? pageToken = null;
    do
    {
        var result = await _chatGroupClient.GetChatGroupPageListAsync(
            page_token: pageToken,
            page_size: 50);
        
        if (result?.Data?.items != null)
        {
            foreach (var group in result.Data.items)
            {
                Console.WriteLine($"群: {group.name}");
            }
        }
        pageToken = result?.Data?.page_token;
    } while (!string.IsNullOrEmpty(pageToken));
}
```

***

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

**认证**：租户令牌

**参数**：

| 参数             | 类型      | 必填 | 说明                       |
| -------------- | ------- | -- | ------------------------ |
| query          | string? | ⚪  | 关键词，示例："abc"             |
| user\_id\_type | string  | ⚪  | 用户 ID 类型，默认值："open\_id"  |
| sort\_type     | string  | ⚪  | 排序方式，默认值：ByCreateTimeAsc |
| page\_size     | int?    | ⚪  | 分页大小，默认值：10              |
| page\_token    | string? | ⚪  | 分页标记                     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "chat_id": "oc_xxxxxxxx",
        "name": "包含关键词的群组"
      }
    ],
    "has_more": false
  }
}
```

**说明**：获取当前身份可见的群列表，包括所在的群、对当前身份公开的群。支持关键词搜索。

***

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

**认证**：租户令牌

**参数**：

| 参数             | 类型      | 必填 | 说明                      |
| -------------- | ------- | -- | ----------------------- |
| chat\_id       | string  | ✅  | 群 ID                    |
| user\_id\_type | string  | ⚪  | 用户 ID 类型，默认值："open\_id" |
| page\_size     | int?    | ⚪  | 分页大小，默认值：10             |
| page\_token    | string? | ⚪  | 分页标记                    |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "moderation_setting": "restricted_mode",
    "speak_ids": ["ou_xxx", "ou_yyy"],
    "has_more": false
  }
}
```

***

### 获取群分享链接

**函数名称**：获取群分享链接

**函数签名**：

```csharp
Task<FeishuApiResult<ShareLinkDataResult>?> GetChatGroupShareLinkByIdAsync(
    [Path] string chat_id,
    [Body] ShareLinkRequest shareLinkRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数               | 类型               | 必填 | 说明         |
| ---------------- | ---------------- | -- | ---------- |
| chat\_id         | string           | ✅  | 群 ID       |
| shareLinkRequest | ShareLinkRequest | ✅  | 获取群分享链接请求体 |

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

**说明**：他人点击分享链接后可加入群组。
