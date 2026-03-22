# 飞书会话标签页 API（用户级）

## 接口名称
**飞书会话标签页 API -（IFeishuUserV1ChatTabs）**

## 功能描述
会话标签页是指飞书客户端某一会话顶部的标签页，通过 OpenAPI 支持添加、删除、更新以及获取会话标签页等操作。
当前接口使用用户令牌访问，适应于用户应用场景。

## 参考文档
- [飞书官方文档 - 会话标签页](https://open.feishu.cn/document/group/chat-tab/chat-tab-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateChatTabsByIdAsync | 创建会话标签页 | 用户令牌 | POST |
| UpdateChatTabsByIdAsync | 更新会话标签页 | 用户令牌 | POST |
| DeleteChatTabsByIdAsync | 删除会话标签页 | 用户令牌 | DELETE |
| ChatTabsSortByIdAsync | 排序会话标签页 | 用户令牌 | POST |
| GetChatTabsListByIdAsync | 获取会话标签页列表 | 用户令牌 | POST |

---

## 函数详细内容

### 创建会话标签页

**函数名称**：创建会话标签页

**函数签名**：
```csharp
Task<FeishuApiResult<ChatTabsCreateResult>?> CreateChatTabsByIdAsync(
    [Path] string chat_id,
    [Body] CreateChatTabsRequest createChatTabsRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    [Query("set_bot_manager")] bool? set_bot_manager = false,
    [Query("uuid")] string? uuid = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| createChatTabsRequest | CreateChatTabsRequest | ✅ | 添加会话标签页请求体 |
| user_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |
| set_bot_manager | bool? | ⚪ | 是否设置机器人为管理员，默认值：false |
| uuid | string? | ⚪ | 去重唯一标识 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "chat_id": "oc_a0553eda9014c201e6969b478895c230",
    "tabs": [
      {
        "tab_id": "tab_001",
        "name": "项目文档",
        "type": "doc"
      }
    ]
  }
}
```

**说明**：创建会话标签页，支持添加文档类型（doc）或 URL 类型（url）的标签页。

**代码示例**：
```csharp
public class UserChatTabsService
{
    private readonly IFeishuUserV1ChatTabs _tabsClient;

    public UserChatTabsService(IFeishuUserV1ChatTabs tabsClient)
    {
        _tabsClient = tabsClient;
    }

    public async Task AddUrlTabAsync(string chatId, string url, string tabName)
    {
        var request = new CreateChatTabsRequest
        {
            tabs = new List<ChatTab>
            {
                new ChatTab
                {
                    name = tabName,
                    type = "url",
                    content = url
                }
            }
        };

        var result = await _tabsClient.CreateChatTabsByIdAsync(chatId, request);
        if (result?.Code == 0)
        {
            Console.WriteLine("标签页创建成功");
        }
    }
}
```

---

### 更新会话标签页

**函数名称**：更新会话标签页

**函数签名**：
```csharp
Task<FeishuApiResult<ChatTabsUpdateResult>?> UpdateChatTabsByIdAsync(
    [Path] string chat_id,
    [Body] UpdateChatTabsRequest updateChatTabsRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| updateChatTabsRequest | UpdateChatTabsRequest | ✅ | 更新会话标签页请求体 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "tabs": [
      {
        "tab_id": "tab_001",
        "name": "更新后的名称"
      }
    ]
  }
}
```

**说明**：更新指定的会话标签页信息，包括名称、类型以及内容等。仅支持更新文档类型（doc）或 URL（url）类型的标签页。

---

### 删除会话标签页

**函数名称**：删除会话标签页

**函数签名**：
```csharp
Task<FeishuApiResult<DeleteTabsResult>?> DeleteChatTabsByIdAsync(
    [Path] string chat_id,
    [Body] ChatTabsIdsRequest deleteChatTabsRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| deleteChatTabsRequest | ChatTabsIdsRequest | ✅ | 删除会话标签页请求体 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "deleted_tab_ids": ["tab_001"]
  }
}
```

---

### 排序会话标签页

**函数名称**：排序会话标签页

**函数签名**：
```csharp
Task<FeishuApiResult<ChatTabsSortResult>?> ChatTabsSortByIdAsync(
    [Path] string chat_id,
    [Body] ChatTabsIdsRequest chatTabsSortRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| chatTabsSortRequest | ChatTabsIdsRequest | ✅ | 排序会话标签页请求体 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "tabs": [
      {
        "tab_id": "tab_001",
        "sort_index": 0
      }
    ]
  }
}
```

---

### 获取会话标签页列表

**函数名称**：获取会话标签页列表

**函数签名**：
```csharp
Task<FeishuApiResult<GetChatTabsResult>?> GetChatTabsListByIdAsync(
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
  "msg": "success",
  "data": {
    "chat_id": "oc_a0553eda9014c201e6969b478895c230",
    "tabs": [
      {
        "tab_id": "tab_001",
        "name": "项目文档",
        "type": "doc",
        "sort_index": 0
      }
    ]
  }
}
```

**说明**：获取指定会话内的会话标签页信息，包括 ID、名称、类型以及内容等。
