---
title: 群公告接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份获取群公告基本信息、读取与批量更新群公告中的块，并支持在群公告中创建或删除块，适用于用户侧群公告编辑场景。
---

# IFeishuUserV1DocxAnnouncement - 用户群公告API

## 功能描述
飞书群公告是群聊中的公告消息，新版群公告以文档（docx）形式存储，其内容由多个块（Block）组成。群公告接口实现了获取群公告基本信息、读取群公告中的块、批量更新块的内容，以及在群公告中创建或删除块等功能。支持用户通过用户访问令牌操作所在群的群公告。

## 参考文档
- [获取群公告基本信息](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement/get)
- [批量更新群公告块](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement-block/batch_update)
- [获取群公告指定块](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement-block/get)
- [获取群公告所有块](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement-block/list)
- [在群公告中创建块](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement-block-children/create)
- [获取群公告指定块的子块](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement-block-children/get)
- [批量删除群公告中的块](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/chat-announcement-block-children/batch_delete)
- [创建群](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| GetChatAnnouncementAsync | 获取群公告基本信息 | UserAccessToken | GET |
| BatchUpdateChatAnnouncementBlocksAsync | 批量更新群公告块内容 | UserAccessToken | PATCH |
| GetChatAnnouncementBlockInfoAsync | 获取群公告指定块信息 | UserAccessToken | GET |
| GetChatAnnouncementBlocksPageListAsync | 分页获取群公告所有块 | UserAccessToken | GET |
| BatchDeleteChatAnnouncementBlockChildrenAsync | 批量删除群公告子块 | UserAccessToken | DELETE |
| CreateChatAnnouncementBlockChildrenAsync | 在群公告中创建子块 | UserAccessToken | POST |
| GetChatAnnouncementBlockChildrenPageListAsync | 分页获取群公告指定块的子块 | UserAccessToken | GET |

## 函数详细内容

### GetChatAnnouncementAsync
获取群公告基本信息，包含群公告类型、群公告版本号、群公告所有者与最新修改者等信息

**函数签名**
```csharp
Task<FeishuApiResult<GetChatAnnouncementResult>?> GetChatAnnouncementAsync(
    [Path] string chat_id,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| chat_id | string | ✅ | 群 ID。获取方式：[创建群](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create)接口返回的 chat_id | - |
| user_id_type | string | ⚪ | 用户 ID 类型，默认 open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "revision_id": 1,
    "create_time": 1609296809,
    "update_time": 1609296809,
    "owner_id": "ou_7d8a6e6df7621556ce0d21922b676706ccs",
    "owner_id_type": "open_id",
    "modifier_id": "ou_7d8a6e6df7621556ce0d21922b676706ccs",
    "modifier_id_type": "open_id",
    "announcement_type": "docx"
  }
}
```

**说明**
- 返回群公告的版本号（revision_id）、所有者（owner_id）、最新修改者（modifier_id）与群公告类型（announcement_type）等信息
- announcement_type 为 docx 表示新版群公告，为 doc 表示旧版群公告
- 群公告创建后版本为 1，查询最新版本需要持有群公告的阅读权限

**代码示例**
```csharp
public class AnnouncementService
{
    private readonly IFeishuUserV1DocxAnnouncement _announcementClient;

    public AnnouncementService(IFeishuUserV1DocxAnnouncement announcementClient)
        => _announcementClient = announcementClient;

    public async Task<GetChatAnnouncementResult?> GetAnnouncementAsync(string chatId)
    {
        var result = await _announcementClient.GetChatAnnouncementAsync(chatId);

        if (result?.Code == 0)
        {
            Console.WriteLine($"群公告版本: {result.Data?.RevisionId}，类型: {result.Data?.AnnouncementType}");
            return result.Data;
        }

        return null;
    }
}
```

---

### BatchUpdateChatAnnouncementBlocksAsync
批量更新群公告中块的富文本内容

**函数签名**
```csharp
Task<FeishuApiResult<BatchUpdateChatAnnouncementBlocksResult>?> BatchUpdateChatAnnouncementBlocksAsync(
    [Path] string chat_id,
    [Body] BatchUpdateBlocksRequest updateBlockRequest,
    [Query("revision_id")] int? revision_id = -1,
    [Query("client_token")] string? client_token = null,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| chat_id | string | ✅ | 群 ID。获取方式：[创建群](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create)接口返回的 chat_id | - |
| updateBlockRequest | BatchUpdateBlocksRequest | ✅ | 批量更新块的内容请求体 | - |
| revision_id | int? | ⚪ | 要操作的群公告版本，-1 表示群公告最新版本；群公告创建后版本为 1，需确保已拥有群公告的编辑权限，默认 -1 | -1 |
| client_token | string? | ⚪ | 操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等进行更新操作；此值为空表示发起一次新的请求，默认 null | fe599b60-450f-46ff-b2ef-9f6675625b97 |
| user_id_type | string | ⚪ | 用户 ID 类型，默认 open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "blocks": [
      {
        "block_id": "doxcnO6UW6wAw2qIcYf4hZpFIth",
        "block_type": 2,
        "text": {
          "elements": [{"text_run": {"content": "更新后的群公告文本"}}]
        }
      }
    ],
    "revision_id": 2,
    "client_token": "fe599b60-450f-46ff-b2ef-9f6675625b97"
  }
}
```

**说明**
- 一次可批量更新多个块的富文本内容，需要持有群公告的编辑权限
- client_token 与接口返回值的 client_token 相对应，用于幂等进行更新操作
- revision_id 默认 -1 表示群公告最新版本，群公告创建后版本为 1

**代码示例**
```csharp
public async Task<int?> UpdateAnnouncementBlockTextAsync(string chatId, string blockId, string text)
{
    var request = new BatchUpdateBlocksRequest
    {
        Requests = new[]
        {
            new BatchUpdateBlockRequest
            {
                BlockId = blockId,
                UpdateTextElements = new UpdateTextElementsRequest
                {
                    Elements = new[]
                    {
                        new TextElement { TextRun = new TextElementTextRun { Content = text } }
                    }
                }
            }
        }
    };

    var result = await _announcementClient.BatchUpdateChatAnnouncementBlocksAsync(chatId, request);

    return result?.Code == 0 ? result.Data?.RevisionId : null;
}
```

---

### GetChatAnnouncementBlockInfoAsync
指定块的 block_id 获取群公告指定块的富文本内容数据

**函数签名**
```csharp
Task<FeishuApiResult<GetBlockInfoResult>?> GetChatAnnouncementBlockInfoAsync(
    [Path] string chat_id,
    [Path] string block_id,
    [Query("revision_id")] int? revision_id = -1,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| chat_id | string | ✅ | 群 ID。获取方式：[创建群](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create)接口返回的 chat_id | - |
| block_id | string | ✅ | 块的唯一标识 | doxcnO6UW6wAw2qIcYf4hZpFIth |
| revision_id | int? | ⚪ | 查询的群公告版本，-1 表示最新版本；查询最新版本需要群公告阅读权限，查询历史版本需要群公告更新权限，默认 -1 | -1 |
| user_id_type | string | ⚪ | 用户 ID 类型，默认 open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "block": {
      "block_id": "doxcnO6UW6wAw2qIcYf4hZpFIth",
      "block_type": 2,
      "parent_id": "doxcnwQhtolBSG0dTaaIuvFqteg",
      "children": [],
      "text": {
        "elements": [{"text_run": {"content": "这是一段群公告文本"}}]
      }
    }
  }
}
```

**说明**
- 响应体 data.block 返回指定块的富文本内容
- 查询最新版本需要持有群公告的阅读权限，查询历史版本需要持有群公告的更新权限

**代码示例**
```csharp
public async Task<Block?> GetAnnouncementBlockAsync(string chatId, string blockId)
{
    var result = await _announcementClient.GetChatAnnouncementBlockInfoAsync(chatId, blockId);

    return result?.Code == 0 ? result.Data?.Block : null;
}
```

---

### GetChatAnnouncementBlocksPageListAsync
获取群公告所有块的富文本内容并分页返回

**函数签名**
```csharp
Task<FeishuApiPageListResult<Block>?> GetChatAnnouncementBlocksPageListAsync(
    [Path] string chat_id,
    [Query("revision_id")] int? revision_id = -1,
    [Query("page_size")] int page_size = 500,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| chat_id | string | ✅ | 群 ID。获取方式：[创建群](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create)接口返回的 chat_id | - |
| revision_id | int? | ⚪ | 查询的群公告版本，-1 表示最新版本；查询最新版本需要群公告阅读权限，查询历史版本需要群公告编辑权限，默认 -1 | -1 |
| page_size | int | ⚪ | 分页大小，即本次请求返回的最大条目数，默认 500 | 500 |
| page_token | string? | ⚪ | 分页标记，第一次请求不填，表示从头开始遍历；返回结果还有更多项时会同时返回新的 page_token | - |
| user_id_type | string | ⚪ | 用户 ID 类型，默认 open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "block_id": "doxcnwQhtolBSG0dTaaIuvFqteg",
        "block_type": 1,
        "parent_id": "",
        "children": ["doxcnO6UW6wAw2qIcYf4hZpFIth"],
        "page": {
          "title": "群公告"
        }
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**
- 群公告以 docx 文档形式存储，其根块为页面 Block，可通过 children 字段遍历子块
- 当 has_more 为 true 时，使用返回的 page_token 继续获取下一页

**代码示例**
```csharp
public async Task<List<Block>> GetAllAnnouncementBlocksAsync(string chatId)
{
    var allBlocks = new List<Block>();
    string? pageToken = null;

    do
    {
        var result = await _announcementClient.GetChatAnnouncementBlocksPageListAsync(
            chatId, page_token: pageToken);

        if (result?.Code == 0 && result.Data?.Items != null)
        {
            allBlocks.AddRange(result.Data.Items);
            pageToken = result.Data.PageToken;
        }
        else
        {
            break;
        }
    } while (!string.IsNullOrEmpty(pageToken));

    return allBlocks;
}
```

---

### BatchDeleteChatAnnouncementBlockChildrenAsync
指定需要操作的块，删除其指定范围的子块。如果操作成功，接口将返回应用删除操作后群公告的版本号

**函数签名**
```csharp
Task<FeishuApiResult<DeleteChatAnnouncementBlockChildrenResult>?> BatchDeleteChatAnnouncementBlockChildrenAsync(
    [Path] string chat_id,
    [Path] string block_id,
    [Body] BatchDeleteBlocksRequest batchDeleteBlocksRequest,
    [Query("revision_id")] int? revision_id = -1,
    [Query("client_token")] string? client_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| chat_id | string | ✅ | 群 ID。获取方式：[创建群](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create)接口返回的 chat_id | - |
| block_id | string | ✅ | 父 Block 的唯一标识 | doxcnwQhtolBSG0dTaaIuvFqteg |
| batchDeleteBlocksRequest | BatchDeleteBlocksRequest | ✅ | 删除块请求体，包含 start_index 与 end_index | - |
| revision_id | int? | ⚪ | 要操作的群公告版本，-1 表示最新版本；需确保已拥有群公告的编辑权限，默认 -1 | -1 |
| client_token | string? | ⚪ | 操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等进行更新操作；此值为空表示发起一次新的请求，默认 null | fe599b60-450f-46ff-b2ef-9f6675625b97 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "revision_id": 3,
    "client_token": "fe599b60-450f-46ff-b2ef-9f6675625b97"
  }
}
```

**说明**
- 删除范围为 [start_index, end_index)，左闭右开区间
- 需要持有群公告的编辑权限，删除成功后返回群公告的新版本号（revision_id）
- 该接口不包含 user_id_type 查询参数

**代码示例**
```csharp
public async Task<int?> DeleteAnnouncementChildrenAsync(string chatId, string blockId, int startIndex, int endIndex)
{
    var request = new BatchDeleteBlocksRequest
    {
        StartIndex = startIndex,
        EndIndex = endIndex
    };

    var result = await _announcementClient.BatchDeleteChatAnnouncementBlockChildrenAsync(chatId, blockId, request);

    return result?.Code == 0 ? result.Data?.RevisionId : null;
}
```

---

### CreateChatAnnouncementBlockChildrenAsync
指定需要操作的块，为其创建一批子块，并插入到指定位置。如果操作成功，接口将返回新创建子块的富文本内容

**函数签名**
```csharp
Task<FeishuApiResult<CreateChatAnnouncementBlockChildrenResult>?> CreateChatAnnouncementBlockChildrenAsync(
    [Path] string chat_id,
    [Path] string block_id,
    [Body] CreateBlockRequest createBlockRequest,
    [Query("revision_id")] int? revision_id = -1,
    [Query("client_token")] string? client_token = null,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| chat_id | string | ✅ | 群 ID。获取方式：[创建群](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create)接口返回的 chat_id | - |
| block_id | string | ✅ | 父块的 block_id，表示为其创建一批子块；如需对群公告根节点创建子块，可将群公告的根块 ID 填入此处 | doxcnwQhtolBSG0dTaaIuvFqteg |
| createBlockRequest | CreateBlockRequest | ✅ | 创建块请求体，包含 children 与 index | - |
| revision_id | int? | ⚪ | 要操作的群公告版本，-1 表示最新版本；需确保已拥有群公告的编辑权限，默认 -1 | -1 |
| client_token | string? | ⚪ | 操作的唯一标识，与接口返回值的 client_token 相对应，用于幂等进行更新操作；此值为空表示发起一次新的请求，默认 null | fe599b60-450f-46ff-b2ef-9f6675625b97 |
| user_id_type | string | ⚪ | 用户 ID 类型，默认 open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "children": [
      {
        "block_id": "doxcnO6UW6wAw2qIcYf4hZpFIth",
        "block_type": 2,
        "text": {
          "elements": [{"text_run": {"content": "新添加的群公告文本块"}}]
        }
      }
    ],
    "revision_id": 2,
    "client_token": "fe599b60-450f-46ff-b2ef-9f6675625b97"
  }
}
```

**说明**
- 如果需要对群公告根节点创建子块，可将群公告的根块 ID 填入 block_id
- 接口返回新创建子块的富文本内容（children）、群公告新版本号与 client_token

**代码示例**
```csharp
public async Task<string?> AddAnnouncementTextAsync(string chatId, string rootBlockId, string text)
{
    var request = new CreateBlockRequest
    {
        Childrens = new[]
        {
            new Block
            {
                BlockType = 2, // 文本块
                Text = new BlockText
                {
                    Elements = new[]
                    {
                        new TextElement { TextRun = new TextElementTextRun { Content = text } }
                    }
                }
            }
        },
        Index = -1 // 添加到末尾
    };

    var result = await _announcementClient.CreateChatAnnouncementBlockChildrenAsync(chatId, rootBlockId, request);

    return result?.Code == 0 ? result.Data?.Childrens?.FirstOrDefault()?.BlockId : null;
}
```

---

### GetChatAnnouncementBlockChildrenPageListAsync
获取群公告中指定块的所有子块的富文本内容并分页返回

**函数签名**
```csharp
Task<FeishuApiPageListResult<Block>?> GetChatAnnouncementBlockChildrenPageListAsync(
    [Path] string chat_id,
    [Path] string block_id,
    [Query("revision_id")] int? revision_id = -1,
    [Query("page_size")] int page_size = 500,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| chat_id | string | ✅ | 群 ID。获取方式：[创建群](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat/create)接口返回的 chat_id | - |
| block_id | string | ✅ | 父块的 block_id，表示获取其子块列表 | doxcnwQhtolBSG0dTaaIuvFqteg |
| revision_id | int? | ⚪ | 查询的群公告版本，-1 表示最新版本；查询最新版本需要群公告阅读权限，查询历史版本需要群公告更新权限，默认 -1 | -1 |
| page_size | int | ⚪ | 分页大小，即本次请求返回的最大条目数，默认 500 | 500 |
| page_token | string? | ⚪ | 分页标记，第一次请求不填，表示从头开始遍历；返回结果还有更多项时会同时返回新的 page_token | - |
| user_id_type | string | ⚪ | 用户 ID 类型，默认 open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "block_id": "doxcnO6UW6wAw2qIcYf4hZpFIth",
        "block_type": 2,
        "parent_id": "doxcnwQhtolBSG0dTaaIuvFqteg",
        "children": [],
        "text": {
          "elements": [{"text_run": {"content": "这是一段群公告文本"}}]
        }
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**
- 仅返回指定块的直接子块，不包含更深层的子孙块
- 当 has_more 为 true 时，使用返回的 page_token 继续获取下一页

**代码示例**
```csharp
public async Task<List<Block>> GetAnnouncementChildrenAsync(string chatId, string blockId)
{
    var allBlocks = new List<Block>();
    string? pageToken = null;

    do
    {
        var result = await _announcementClient.GetChatAnnouncementBlockChildrenPageListAsync(
            chatId, blockId, page_token: pageToken);

        if (result?.Code == 0 && result.Data?.Items != null)
        {
            allBlocks.AddRange(result.Data.Items);
            pageToken = result.Data.PageToken;
        }
        else
        {
            break;
        }
    } while (!string.IsNullOrEmpty(pageToken));

    return allBlocks;
}
```
