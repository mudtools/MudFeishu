# 飞书云文档用户接口 - (IFeishuUserV1Docx)

## 接口名称

**飞书云文档用户接口** -（`IFeishuUserV1Docx`）

## 功能描述

本接口提供飞书云文档的用户级别操作能力，支持以用户身份创建、查询和管理云文档。与租户接口相比，用户接口以当前登录用户的身份执行操作，可以访问用户有权限的个人文档和共享文档。适用于需要以用户身份进行文档操作的场景，如个人文档管理、用户侧文档编辑器等。

## 参考文档

- [飞书云文档概述](https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/docx-overview)
- [创建文档](https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/document/create)
- [获取文档信息](https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/document/get)
- [获取文档纯文本内容](https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/document/raw_content)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|-----------|
| `CreateDocumentAsync` | 创建 docx 文档 | 用户令牌 | POST |
| `GetDocumentInfoAsync` | 获取文档基本信息 | 用户令牌 | GET |
| `GetDocumentRawContentAsync` | 获取文档纯文本内容 | 用户令牌 | GET |
| `GetDocumentBlocksPageListAsync` | 分页获取文档所有块内容 | 用户令牌 | GET |

## 函数详细内容

---

### 创建文档

**函数名称**：创建 docx 文档

**函数签名**：

```csharp
Task<FeishuApiResult<DocumentInfoResult>?> CreateDocumentAsync(
    [Body] CreateDocumentRequest createDocumentRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（`TokenType.UserAccessToken`）

**参数**：

| 参数名 | 类型 | 必填 | 描述 |
|-------|------|------|------|
| `createDocumentRequest` | `CreateDocumentRequest` | ✅ | 创建文档请求体 |
| `cancellationToken` | `CancellationToken` | ⚪ | 取消操作令牌 |

**CreateDocumentRequest 字段说明**：

| 字段名 | 类型 | 必填 | 描述 | 示例值 |
|-------|------|------|------|--------|
| `FolderToken` | `string?` | ⚪ | 文档所在文件夹 Token，不传表示根目录 | `fldcnqquW1svRIYVT2Np6Iabcef` |
| `Title` | `string?` | ⚪ | 文档标题（纯文本），最大长度 800 | `我的笔记` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "document": {
      "document_id": "doxbcmEtbFrbbq10nPNu8gabcef",
      "revision_id": 1,
      "title": "我的笔记",
      "display_setting": {
        "enable_catalog": true
      },
      "cover": {
        "type": "default"
      }
    }
  }
}
```

**说明**：

- 以用户身份创建的文档，所有者为用户本人
- 文档 ID 长度为 27 个字符，可拼接为文档链接

**代码示例**：

```csharp
// 使用用户权限创建个人文档
public class UserDocumentService
{
    private readonly IFeishuUserV1Docx _userDocxClient;

    public UserDocumentService(IFeishuUserV1Docx userDocxClient)
    {
        _userDocxClient = userDocxClient;
    }

    public async Task<string> CreatePersonalNoteAsync(string title)
    {
        var request = new CreateDocumentRequest
        {
            Title = title
        };

        var result = await _userDocxClient.CreateDocumentAsync(request);
        
        if (result?.IsSuccess() == true)
        {
            var docId = result.Data?.Document?.DocumentId;
            Console.WriteLine($"个人文档创建成功，ID: {docId}");
            return docId ?? string.Empty;
        }
        
        throw new Exception($"创建文档失败: {result?.Msg}");
    }
}
```

---

### 获取文档信息

**函数名称**：获取文档基本信息

**函数签名**：

```csharp
Task<FeishuApiResult<DocumentInfoResult>?> GetDocumentInfoAsync(
    [Path] string document_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（`TokenType.UserAccessToken`）

**参数**：

| 参数名 | 类型 | 必填 | 描述 |
|-------|------|------|------|
| `document_id` | `string` | ✅ | 文档的唯一标识 | `doxcnePuYufKa49ISjhD8Iabcef` |
| `cancellationToken` | `CancellationToken` | ⚪ | 取消操作令牌 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "document": {
      "document_id": "doxbcmEtbFrbbq10nPNu8gabcef",
      "revision_id": 5,
      "title": "我的项目文档",
      "display_setting": {
        "enable_catalog": true,
        "show_title": true
      },
      "cover": {
        "type": "color",
        "color": 0
      }
    }
  }
}
```

**说明**：

- 用户只能获取自己有阅读权限的文档信息
- 对于知识库（wiki）中的文档，其 URL 中的 token **不是** `document_id`

**代码示例**：

```csharp
// 获取用户有权限的文档信息
public async Task<string?> GetDocumentTitleAsync(string documentId)
{
    var result = await _userDocxClient.GetDocumentInfoAsync(documentId);
    
    if (result?.IsSuccess() == true)
    {
        return result.Data?.Document?.Title;
    }
    
    return null;
}
```

---

### 获取文档纯文本内容

**函数名称**：获取文档纯文本内容

**函数签名**：

```csharp
Task<FeishuApiResult<DocumentRawContentResult>?> GetDocumentRawContentAsync(
    [Path] string document_id,
    [Query("lang")] int? lang = 0,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（`TokenType.UserAccessToken`）

**参数**：

| 参数名 | 类型 | 必填 | 描述 |
|-------|------|------|------|
| `document_id` | `string` | ✅ | 文档的唯一标识 |
| `lang` | `int?` | ⚪ | @用户 的语言类型，默认 0 |
| `cancellationToken` | `CancellationToken` | ⚪ | 取消操作令牌 |

**lang 可选值**：

| 值 | 含义 |
|---|------|
| 0 | 用户默认名称（如：@张敏） |
| 1 | 用户英文名称（如：@Min Zhang） |
| 2 | 暂不支持，返回默认名称 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "content": "我的笔记\n今天的工作内容...\n\n待办事项：\n- 完成任务 A\n- 开会讨论\n"
  }
}
```

**说明**：

- 纯文本内容不包含格式信息
- 适用于文档内容检索、全文索引等场景

**代码示例**：

```csharp
// 提取文档文本用于搜索索引
public async Task<string> ExtractDocumentTextAsync(string documentId)
{
    var result = await _userDocxClient.GetDocumentRawContentAsync(documentId);
    
    if (result?.IsSuccess() == true)
    {
        return result.Data?.Content ?? string.Empty;
    }
    
    return string.Empty;
}
```

---

### 分页获取文档块列表

**函数名称**：分页获取文档所有块内容

**函数签名**：

```csharp
Task<FeishuApiPageListResult<Block>?> GetDocumentBlocksPageListAsync(
    [Path] string document_id,
    [Query("document_revision_id")] int? document_revision_id = -1,
    [Query("page_size")] int page_size = 500,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（`TokenType.UserAccessToken`）

**参数**：

| 参数名 | 类型 | 必填 | 描述 | 默认值 |
|-------|------|------|------|--------|
| `document_id` | `string` | ✅ | 文档的唯一标识 | - |
| `document_revision_id` | `int?` | ⚪ | 文档版本号，-1 表示最新版本 | `-1` |
| `page_size` | `int` | ⚪ | 分页大小，最大 500 | `500` |
| `page_token` | `string?` | ⚪ | 分页标记，首次请求不填 | `null` |
| `user_id_type` | `string` | ⚪ | 用户 ID 类型 | `open_id` |
| `cancellationToken` | `CancellationToken` | ⚪ | 取消操作令牌 | - |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "block_id": "doxcnO6UW6wAw2qIcYf4hZpFIth",
        "block_type": 1,
        "parent_id": "",
        "children": ["doxcn123...", "doxcn456..."],
        "page": {
          "title": "文档标题"
        }
      },
      {
        "block_id": "doxcn123...",
        "block_type": 3,
        "parent_id": "doxcnO6UW6wAw2qIcYf4hZpFIth",
        "heading1": {
          "elements": [{"text_run": {"content": "第一章"}}]
        }
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**：

- 块（Block）是文档的最小构建单元，包括文本、标题、表格、图片等
- 不同 `block_type` 对应不同类型的块（1=页面, 3=一级标题等）

**代码示例**：

```csharp
// 分析文档结构并提取所有标题
public async Task<List<string>> ExtractAllHeadingsAsync(string documentId)
{
    var headings = new List<string>();
    var result = await _userDocxClient.GetDocumentBlocksPageListAsync(documentId);

    if (result?.IsSuccess() == true && result.Data?.Items != null)
    {
        foreach (var block in result.Data.Items)
        {
            // 标题块类型：3-11 对应一级到九级标题
            if (block.BlockType >= 3 && block.BlockType <= 11)
            {
                // 提取标题文本...
            }
        }
    }

    return headings;
}
```
