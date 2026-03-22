# 飞书群公告 API（用户级）

## 接口名称
**飞书群公告 API -（IFeishuUserV1ChatGroupAnnouncement）**

## 功能描述
群公告是群组中的公告文档，采用飞书云文档承载，每个群组只有一个群公告，每篇群公告都有唯一的 chat_id 作为标识。
当前接口使用用户令牌访问，适应于用户应用场景。

## 参考文档
- [飞书官方文档 - 群公告](https://open.feishu.cn/document/group/upgraded-group-announcement/group-announcement-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| GetNoticeInfoByIdAsync | 获取群公告基本信息 | 用户令牌 | GET |
| GetNoticeBlocksListByIdAsync | 分页获取群公告块列表 | 用户令牌 | GET |
| CreateNoticeBlockAsync | 创建群公告块 | 用户令牌 | POST |
| UpdateNoticeBlockAsync | 更新群公告块 | 用户令牌 | PATCH |
| GetBlockContentByIdAsync | 获取群公告块内容 | 用户令牌 | GET |
| GetBlockContentPageListByIdAsync | 分页获取块子内容 | 用户令牌 | GET |
| DeleteBlockByIdAsync | 删除群公告块 | 用户令牌 | DELETE |

---

## 函数详细内容

### 获取群公告基本信息

**函数名称**：获取群公告基本信息

**函数签名**：
```csharp
Task<FeishuApiResult<GetAnnouncementResult>?> GetNoticeInfoByIdAsync(
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
    "title": "群公告标题",
    "create_time": "1690000000",
    "edit_time": "1690100000",
    "owner_id": "ou_xxxxxxxx",
    "revision": 5
  }
}
```

**代码示例**：
```csharp
public class UserAnnouncementService
{
    private readonly IFeishuUserV1ChatGroupAnnouncement _announcementClient;

    public UserAnnouncementService(IFeishuUserV1ChatGroupAnnouncement announcementClient)
    {
        _announcementClient = announcementClient;
    }

    public async Task ShowAnnouncementAsync(string chatId)
    {
        var result = await _announcementClient.GetNoticeInfoByIdAsync(chatId);
        if (result?.Data != null)
        {
            Console.WriteLine($"公告标题: {result.Data.title}");
        }
    }
}
```

---

### 分页获取群公告块列表

**函数名称**：分页获取群公告块列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<AnnouncementBlock>?> GetNoticeBlocksListByIdAsync(
    [Path] string chat_id,
    [Query("page_size")] int? page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("revision_id")] int? revision_id = -1,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| page_size | int? | ⚪ | 分页大小，默认值：10 |
| page_token | string? | ⚪ | 分页标记 |
| revision_id | int? | ⚪ | 群公告版本，-1 表示最新版本 |
| user_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |

---

### 创建群公告块

**函数名称**：创建群公告块

**函数签名**：
```csharp
Task<FeishuApiResult<CreateBlockResult>?> CreateNoticeBlockAsync(
    [Path] string chat_id,
    [Path] string block_id,
    [Body] CreateBlockRequest createBlockRequest,
    [Query("revision_id")] int revision_id = -1,
    [Query("client_token")] string? client_token = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| block_id | string | ✅ | 父块 ID |
| createBlockRequest | CreateBlockRequest | ✅ | 创建块请求体 |
| revision_id | int | ⚪ | 群公告版本，默认值：-1 |
| client_token | string? | ⚪ | 幂等操作标识 |
| user_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |

---

### 更新群公告块

**函数名称**：更新群公告块

**函数签名**：
```csharp
Task<FeishuApiResult<BatchUpdateResult>?> UpdateNoticeBlockAsync(
    [Path] string chat_id,
    [Body] BlocksBatchUpdateRequest batchUpdateRequest,
    [Query("revision_id")] int revision_id = -1,
    [Query("client_token")] string? client_token = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| batchUpdateRequest | BlocksBatchUpdateRequest | ✅ | 批量更新块内容请求体 |
| revision_id | int | ⚪ | 群公告版本，默认值：-1 |
| client_token | string? | ⚪ | 幂等操作标识 |
| user_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |

---

### 获取群公告块内容

**函数名称**：获取群公告块内容

**函数签名**：
```csharp
Task<FeishuApiResult<GetBlockContentListResult>?> GetBlockContentByIdAsync(
    [Path] string chat_id,
    [Path] string block_id,
    [Query("revision_id")] int? revision_id = -1,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| block_id | string | ✅ | Block 唯一标识 |
| revision_id | int? | ⚪ | 群公告版本，默认值：-1 |
| user_id_type | string? | ⚪ | 用户 ID 类型，默认值："open_id" |

---

### 分页获取块子内容

**函数名称**：分页获取块子内容

**函数签名**：
```csharp
Task<FeishuApiPageListResult<AnnouncementBlock>?> GetBlockContentPageListByIdAsync(
    [Path] string chat_id,
    [Path] string block_id,
    [Query("page_size")] int? page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("revision_id")] int? revision_id = -1,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| block_id | string | ✅ | Block 唯一标识 |
| page_size | int? | ⚪ | 分页大小，默认值：10 |
| page_token | string? | ⚪ | 分页标记 |
| revision_id | int? | ⚪ | 群公告版本，默认值：-1 |
| user_id_type | string? | ⚪ | 用户 ID 类型，默认值："open_id" |

---

### 删除群公告块

**函数名称**：删除群公告块

**函数签名**：
```csharp
Task<FeishuApiResult<DeleteAnnouncementBlockResult>?> DeleteBlockByIdAsync(
    [Path] string chat_id,
    [Path] string block_id,
    [Body] DeleteAnnouncementBlockRequest deleteRequest,
    [Query("user_id_type")] string? user_id_type = "open_id",
    [Query("client_token")] string? client_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| block_id | string | ✅ | Block 唯一标识 |
| deleteRequest | DeleteAnnouncementBlockRequest | ✅ | 删除请求体 |
| user_id_type | string? | ⚪ | 用户 ID 类型，默认值："open_id" |
| client_token | string? | ⚪ | 幂等操作标识 |
