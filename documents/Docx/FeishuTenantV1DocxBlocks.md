# 飞书云文档块租户接口 - (IFeishuTenantV1DocxBlocks)

## 接口名称

**飞书云文档块租户接口** -（`IFeishuTenantV1DocxBlocks`）

## 功能描述

本接口提供飞书云文档块的租户级别操作能力，支持以应用身份对文档块进行增删改查操作。块（Block）是文档中的最小构建单元，是内容的结构化组成元素，包括文本、标题、表格、图片、代码块等多种形态。通过本接口，开发者可以：

- 创建和管理文档块（包括嵌套块）
- 更新块的内容
- 批量更新多个块
- 获取块的富文本内容
- 删除指定范围的子块
- 将 Markdown/HTML 内容转换为文档块

适用于需要自动化处理文档内容的场景，如文档生成器、报表系统、知识库同步等。

## 参考文档

- [飞书云文档概述](https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/docx-overview)
- [创建块](https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/document-block/create)
- [更新块](https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/document-block/patch)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|-----------|
| `CreateBlockAsync` | 创建子块 | 租户令牌 | POST |
| `CreateDescendantBlockAsync` | 创建嵌套子块 | 租户令牌 | POST |
| `UpdateBlockAsync` | 更新块内容 | 租户令牌 | PATCH |
| `GetBlockInfoAsync` | 获取块信息 | 租户令牌 | GET |
| `BatchUpdateBlocksAsync` | 批量更新块 | 租户令牌 | PATCH |
| `GetChildrenBlocksPageListAsync` | 分页获取子块列表 | 租户令牌 | GET |
| `BatchDeleteBlocksAsync` | 批量删除子块 | 租户令牌 | DELETE |
| `ContentConvertAsync` | 内容转换（Markdown/HTML 转块） | 租户令牌 | POST |

## 函数详细内容

---

### 创建子块

**函数名称**：创建子块

**函数签名**：

```csharp
Task<FeishuApiResult<BlockOpResult>?> CreateBlockAsync(
    [Path] string document_id,
    [Path] string block_id,
    [Body] CreateBlockRequest createBlockRequest,
    [Query("document_revision_id")] int? document_revision_id = -1,
    [Query("client_token")] string? client_token = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌（`TokenType.TenantAccessToken`）

**参数**：

| 参数名 | 类型 | 必填 | 描述 | 默认值 |
|-------|------|------|------|--------|
| `document_id` | `string` | ✅ | 文档的唯一标识 | - |
| `block_id` | `string` | ✅ | 父块的 block_id（文档根节点可用 document_id） | - |
| `createBlockRequest` | `CreateBlockRequest` | ✅ | 创建块请求体 | - |
| `document_revision_id` | `int?` | ⚪ | 文档版本号，-1 表示最新版本 | `-1` |
| `client_token` | `string?` | ⚪ | 幂等操作标识 | `null` |
| `user_id_type` | `string` | ⚪ | 用户 ID 类型 | `open_id` |
| `cancellationToken` | `CancellationToken` | ⚪ | 取消操作令牌 | - |

**CreateBlockRequest 字段说明**：

| 字段名 | 类型 | 必填 | 描述 |
|-------|------|------|------|
| `Childrens` | `Block[]?` | ⚪ | 添加的子块列表，最大 50 个 |
| `Index` | `int?` | ⚪ | 插入位置，起始值为 0 | `-1`（末尾） |

**响应**：

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
          "elements": [{"text_run": {"content": "新添加的文本块"}}]
        }
      }
    ],
    "document_revision_id": 5,
    "client_token": "fe599b60-450f-46ff-b2ef-9f6675625b97"
  }
}
```

**代码示例**：

```csharp
// 在文档末尾添加文本块
public async Task AddTextBlockAsync(string documentId, string text)
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
                        new TextElement { TextRun = new TextRun { Content = text } }
                    }
                }
            }
        },
        Index = -1 // 添加到末尾
    };

    var result = await _blocksClient.CreateBlockAsync(documentId, documentId, request);
    
    if (result?.IsSuccess() == true)
    {
        Console.WriteLine($"块创建成功，新版本号: {result.Data?.DocumentRevisionId}");
    }
}
```

---

### 创建嵌套子块

**函数名称**：创建嵌套子块

**函数签名**：

```csharp
Task<FeishuApiResult<CreateDescendantBlockResult>?> CreateDescendantBlockAsync(
    [Path] string document_id,
    [Path] string block_id,
    [Body] CreateDescendantBlockRequest createDescendantBlockRequest,
    [Query("document_revision_id")] int? document_revision_id = -1,
    [Query("client_token")] string? client_token = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌（`TokenType.TenantAccessToken`）

**参数**：

| 参数名 | 类型 | 必填 | 描述 | 默认值 |
|-------|------|------|------|--------|
| `document_id` | `string` | ✅ | 文档的唯一标识 | - |
| `block_id` | `string` | ✅ | 父块的 block_id | - |
| `createDescendantBlockRequest` | `CreateDescendantBlockRequest` | ✅ | 创建嵌套块请求体 | - |
| `document_revision_id` | `int?` | ⚪ | 文档版本号 | `-1` |
| `client_token` | `string?` | ⚪ | 幂等操作标识 | `null` |
| `user_id_type` | `string` | ⚪ | 用户 ID 类型 | `open_id` |
| `cancellationToken` | `CancellationToken` | ⚪ | 取消操作令牌 | - |

**说明**：

- 支持创建有父子关系的嵌套块结构
- GridColumn、TableCell、Callout 等块必须包含至少一个子块

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "children": [...],
    "document_revision_id": 6,
    "client_token": "80bf5b2a-4dea-4c02-8a84-a0e682de463d",
    "block_id_relations": [
      {"temp_block_id": "temp_001", "block_id": "doxcnxxx..."}
    ]
  }
}
```

---

### 更新块内容

**函数名称**：更新块内容

**函数签名**：

```csharp
Task<FeishuApiResult<BlockOpResult>?> UpdateBlockAsync(
    [Path] string document_id,
    [Path] string block_id,
    [Body] UpdateBlockRequest updateBlockRequest,
    [Query("document_revision_id")] int? document_revision_id = -1,
    [Query("client_token")] string? client_token = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌（`TokenType.TenantAccessToken`）

**参数**：

| 参数名 | 类型 | 必填 | 描述 | 默认值 |
|-------|------|------|------|--------|
| `document_id` | `string` | ✅ | 文档的唯一标识 | - |
| `block_id` | `string` | ✅ | 要更新的块 ID | - |
| `updateBlockRequest` | `UpdateBlockRequest` | ✅ | 更新块请求体 | - |
| `document_revision_id` | `int?` | ⚪ | 文档版本号 | `-1` |
| `client_token` | `string?` | ⚪ | 幂等操作标识 | `null` |
| `user_id_type` | `string` | ⚪ | 用户 ID 类型 | `open_id` |
| `cancellationToken` | `CancellationToken` | ⚪ | 取消操作令牌 | - |

**UpdateBlockRequest 字段说明**：

| 字段名 | 类型 | 必填 | 描述 |
|-------|------|------|------|
| `UpdateTextElements` | `UpdateTextElementsRequest?` | ⚪ | 更新文本元素 |
| `UpdateTextStyle` | `UpdateTextStyleRequest?` | ⚪ | 更新文本样式 |
| `UpdateTableProperty` | `UpdateTablePropertyRequest?` | ⚪ | 更新表格属性 |
| `InsertTableRow` | `InsertTableRowRequest?` | ⚪ | 表格插入新行 |
| `InsertTableColumn` | `InsertTableColumnRequest?` | ⚪ | 表格插入新列 |
| `DeleteTableRows` | `DeleteTableRowsRequest?` | ⚪ | 表格删除行 |

**代码示例**：

```csharp
// 更新文本块内容
public async Task UpdateTextBlockAsync(string documentId, string blockId, string newText)
{
    var request = new UpdateBlockRequest
    {
        UpdateTextElements = new UpdateTextElementsRequest
        {
            // 设置新的文本元素...
        }
    };

    var result = await _blocksClient.UpdateBlockAsync(documentId, blockId, request);
}
```

---

### 获取块信息

**函数名称**：获取块信息

**函数签名**：

```csharp
Task<FeishuApiResult<GetBlockInfoResult>?> GetBlockInfoAsync(
    [Path] string document_id,
    [Path] string block_id,
    [Query("document_revision_id")] int? document_revision_id = -1,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌（`TokenType.TenantAccessToken`）

**参数**：

| 参数名 | 类型 | 必填 | 描述 | 默认值 |
|-------|------|------|------|--------|
| `document_id` | `string` | ✅ | 文档的唯一标识 | - |
| `block_id` | `string` | ✅ | 块的唯一标识 | - |
| `document_revision_id` | `int?` | ⚪ | 文档版本号 | `-1` |
| `user_id_type` | `string` | ⚪ | 用户 ID 类型 | `open_id` |
| `cancellationToken` | `CancellationToken` | ⚪ | 取消操作令牌 | - |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "block": {
      "block_id": "doxcnO6UW6wAw2qIcYf4hZpFIth",
      "block_type": 2,
      "parent_id": "doxcnePuYufKa49ISjhD8Ih0ikh",
      "children": [],
      "text": {
        "elements": [{"text_run": {"content": "这是一段文本"}}]
      }
    }
  }
}
```

---

### 批量更新块

**函数名称**：批量更新块

**函数签名**：

```csharp
Task<FeishuApiResult<BatchUpdateBlocksResult>?> BatchUpdateBlocksAsync(
    [Path] string document_id,
    [Body] BatchUpdateBlocksRequest updateBlockRequest,
    [Query("document_revision_id")] int? document_revision_id = -1,
    [Query("client_token")] string? client_token = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌（`TokenType.TenantAccessToken`）

**说明**：

- 一次可批量更新多个块的富文本内容
- 适用于批量替换、批量格式化等场景

---

### 分页获取子块列表

**函数名称**：分页获取子块列表

**函数签名**：

```csharp
Task<FeishuApiPageListResult<Block>?> GetChildrenBlocksPageListAsync(
    [Path] string document_id,
    [Path] string block_id,
    [Query("document_revision_id")] int? document_revision_id = -1,
    [Query("client_token")] string? client_token = null,
    [Query("page_size")] int page_size = 500,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌（`TokenType.TenantAccessToken`）

**参数**：

| 参数名 | 类型 | 必填 | 描述 | 默认值 |
|-------|------|------|------|--------|
| `document_id` | `string` | ✅ | 文档的唯一标识 | - |
| `block_id` | `string` | ✅ | 父块的 block_id | - |
| `document_revision_id` | `int?` | ⚪ | 文档版本号 | `-1` |
| `client_token` | `string?` | ⚪ | 幂等操作标识 | `null` |
| `page_size` | `int` | ⚪ | 分页大小 | `500` |
| `page_token` | `string?` | ⚪ | 分页标记 | `null` |
| `user_id_type` | `string` | ⚪ | 用户 ID 类型 | `open_id` |
| `cancellationToken` | `CancellationToken` | ⚪ | 取消操作令牌 | - |

**代码示例**：

```csharp
// 获取指定块的所有子块
public async Task<List<Block>> GetAllChildrenBlocksAsync(string documentId, string blockId)
{
    var allBlocks = new List<Block>();
    string? pageToken = null;

    do
    {
        var result = await _blocksClient.GetChildrenBlocksPageListAsync(
            documentId, blockId, page_token: pageToken);

        if (result?.IsSuccess() == true && result.Data?.Items != null)
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

### 批量删除子块

**函数名称**：批量删除子块

**函数签名**：

```csharp
Task<FeishuApiResult<BatchDeleteBlocksResult>?> BatchDeleteBlocksAsync(
    [Path] string document_id,
    [Path] string block_id,
    [Body] BatchDeleteBlocksRequest batchDeleteBlocksRequest,
    [Query("document_revision_id")] int? document_revision_id = -1,
    [Query("client_token")] string? client_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌（`TokenType.TenantAccessToken`）

**参数**：

| 参数名 | 类型 | 必填 | 描述 | 默认值 |
|-------|------|------|------|--------|
| `document_id` | `string` | ✅ | 文档的唯一标识 | - |
| `block_id` | `string` | ✅ | 父块的 block_id | - |
| `batchDeleteBlocksRequest` | `BatchDeleteBlocksRequest` | ✅ | 删除块请求体 | - |
| `document_revision_id` | `int?` | ⚪ | 文档版本号 | `-1` |
| `client_token` | `string?` | ⚪ | 幂等操作标识 | `null` |
| `cancellationToken` | `CancellationToken` | ⚪ | 取消操作令牌 | - |

**BatchDeleteBlocksRequest 字段说明**：

| 字段名 | 类型 | 必填 | 描述 |
|-------|------|------|------|
| `StartIndex` | `int` | ✅ | 删除的起始索引（左闭右开） |
| `EndIndex` | `int` | ✅ | 删除的末尾索引（左闭右开） |

**说明**：

- 删除范围为 `[start_index, end_index)`，左闭右开区间
- 删除后返回应用删除操作后的文档版本号

---

### 内容转换

**函数名称**：内容转换（Markdown/HTML 转块）

**函数签名**：

```csharp
Task<FeishuApiResult<ContentConvertResult>?> ContentConvertAsync(
    [Body] ConvertContentRequest convertContentRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌（`TokenType.TenantAccessToken`）

**参数**：

| 参数名 | 类型 | 必填 | 描述 | 默认值 |
|-------|------|------|------|--------|
| `convertContentRequest` | `ConvertContentRequest` | ✅ | 内容转换请求体 | - |
| `user_id_type` | `string` | ⚪ | 用户 ID 类型 | `open_id` |
| `cancellationToken` | `CancellationToken` | ⚪ | 取消操作令牌 | - |

**ConvertContentRequest 字段说明**：

| 字段名 | 类型 | 必填 | 描述 |
|-------|------|------|------|
| `ContentType` | `string` | ✅ | 内容类型：`markdown` 或 `html` | `markdown` |
| `Content` | `string` | ✅ | 文本内容，长度 1-10485760 字符 | - |

**支持的转换类型**：

- 文本块、1-9 级标题
- 无序列表、有序列表
- 代码块、引用块
- 待办事项、图片
- 表格及表格单元格

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "first_level_block_ids": ["doxcn1...", "doxcn2..."],
    "blocks": [...],
    "block_id_to_image_urls": [
      {"block_id": "doxcnimg...", "image_url": "https://..."}
    ]
  }
}
```

**代码示例**：

```csharp
// 将 Markdown 内容转换为文档块
public async Task ConvertMarkdownToBlocksAsync(string markdownContent)
{
    var request = new ConvertContentRequest
    {
        ContentType = "markdown",
        Content = markdownContent
    };

    var result = await _blocksClient.ContentConvertAsync(request);
    
    if (result?.IsSuccess() == true)
    {
        var blocks = result.Data?.Blocks;
        var blockIds = result.Data?.FirstLevelBlockIds;
        Console.WriteLine($"转换成功，生成 {blocks?.Length} 个块");
    }
}
```
