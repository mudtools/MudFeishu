# 飞书云文档租户接口 - (IFeishuTenantV1Docx)

## 接口名称

**飞书云文档租户接口** -（`IFeishuTenantV1Docx`）

## 功能描述

本接口提供飞书云文档的租户级别操作能力，支持以应用身份创建、查询和管理云文档。云文档是飞书提供的在线协作文档服务，每篇文档都有唯一的 `document_id` 作为标识。通过本接口，开发者可以：

- 创建 docx 格式的云文档
- 获取文档基本信息
- 获取文档纯文本内容
- 分页获取文档所有块的富文本内容

适用于需要以应用身份统一管理文档的企业级场景，如自动化文档生成、文档归档系统等。

## 参考文档

- [飞书云文档概述](https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/docx-overview)
- [创建文档](https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/document/create)
- [获取文档信息](https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/document/get)
- [获取文档纯文本内容](https://open.feishu.cn/document/server-docs/docs/docs/docx-v1/document/raw_content)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|-----------|
| `CreateDocumentAsync` | 创建 docx 文档 | 租户令牌 | POST |
| `GetDocumentInfoAsync` | 获取文档基本信息 | 租户令牌 | GET |
| `GetDocumentRawContentAsync` | 获取文档纯文本内容 | 租户令牌 | GET |
| `GetDocumentBlocksPageListAsync` | 分页获取文档所有块内容 | 租户令牌 | GET |

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

**认证**：租户令牌（`TokenType.TenantAccessToken`）

**参数**：

| 参数名 | 类型 | 必填 | 描述 |
|-------|------|------|------|
| `createDocumentRequest` | `CreateDocumentRequest` | ✅ | 创建文档请求体 |
| `cancellationToken` | `CancellationToken` | ⚪ | 取消操作令牌 |

**CreateDocumentRequest 字段说明**：

| 字段名 | 类型 | 必填 | 描述 | 示例值 |
|-------|------|------|------|--------|
| `FolderToken` | `string?` | ⚪ | 文档所在文件夹 Token，不传表示根目录 | `fldcnqquW1svRIYVT2Np6Iabcef` |
| `Title` | `string?` | ⚪ | 文档标题（纯文本），最大长度 800 | `一篇新的文档` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "document": {
      "document_id": "doxbcmEtbFrbbq10nPNu8gabcef",
      "revision_id": 1,
      "title": "一篇新的文档",
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

- 若应用使用 `tenant_access_token` 权限，`FolderToken` 仅可指定应用创建的文件夹
- 文档 ID 长度为 27 个字符，可拼接为文档链接：`https://{domain}.feishu.cn/docx/{document_id}`

**代码示例**：

```csharp
// 使用租户权限创建云文档
public class DocumentService
{
    private readonly IFeishuTenantV1Docx _docxClient;

    public DocumentService(IFeishuTenantV1Docx docxClient)
    {
        _docxClient = docxClient;
    }

    public async Task<string> CreateMeetingNotesAsync(string title, string? folderToken = null)
    {
        var request = new CreateDocumentRequest
        {
            Title = title,
            FolderToken = folderToken
        };

        var result = await _docxClient.CreateDocumentAsync(request);
        
        if (result?.IsSuccess() == true)
        {
            var docId = result.Data?.Document?.DocumentId;
            Console.WriteLine($"文档创建成功，ID: {docId}");
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

**认证**：租户令牌（`TokenType.TenantAccessToken`）

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
      "title": "项目需求文档",
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

- 对于知识库（wiki）中的文档，其 URL 中的 token **不是** `document_id`，请注意区分
- 需要持有文档的阅读权限

**代码示例**：

```csharp
// 获取文档元数据
public async Task<DocumentInfo?> GetDocumentMetadataAsync(string documentId)
{
    var result = await _docxClient.GetDocumentInfoAsync(documentId);
    
    if (result?.IsSuccess() == true)
    {
        return result.Data?.Document;
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

**认证**：租户令牌（`TokenType.TenantAccessToken`）

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
    "content": "云文档\n多人实时协同，插入一切元素。不仅是在线文档，更是强大的创作和互动工具\n云文档：专为协作而生\n"
  }
}
```

**说明**：

- 纯文本内容不包含格式信息，适用于文档内容检索、全文索引等场景
- 返回内容为文档中所有文本块的纯文本拼接

**代码示例**：

```csharp
// 文档内容全文检索
public async Task<bool> DocumentContainsKeywordAsync(string documentId, string keyword)
{
    var result = await _docxClient.GetDocumentRawContentAsync(documentId);
    
    if (result?.IsSuccess() == true)
    {
        var content = result.Data?.Content ?? string.Empty;
        return content.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }
    
    return false;
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

**认证**：租户令牌（`TokenType.TenantAccessToken`）

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
      }
    ],
    "page_token": "next_page_token",
    "has_more": true
  }
}
```

**说明**：

- 块（Block）是文档的最小构建单元，包括文本、标题、表格、图片等
- 若查询最新版本需要阅读权限，查询历史版本需要编辑权限
- 当 `has_more` 为 `true` 时，使用返回的 `page_token` 继续获取下一页

**代码示例**：

```csharp
// 遍历获取文档所有块内容
public async Task<List<Block>> GetAllDocumentBlocksAsync(string documentId)
{
    var allBlocks = new List<Block>();
    string? pageToken = null;

    do
    {
        var result = await _docxClient.GetDocumentBlocksPageListAsync(
            documentId,
            page_token: pageToken,
            page_size: 500);

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
