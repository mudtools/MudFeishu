---
title: 云空间文件管理接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份管理飞书云空间文件，继承租户文件接口的全部能力，同时支持用户视角的云文档搜索，适用于个人文件管理与用户级文档搜索场景。
---

# 云空间文件管理 - 用户令牌（FeishuUserV1DriveFiles）

## 接口名称
**云空间文件管理（用户令牌）** -（`IFeishuUserV1DriveFiles`）

## 功能描述
提供以用户身份管理飞书云空间文件的能力，继承自租户令牌文件接口的所有功能，同时支持用户视角下的云文档搜索功能。适用于需要以具体用户身份操作云文档的业务场景，如个人文件管理、用户级文档搜索等。

## 参考文档
- [文件概述 - 飞书开放平台](https://open.feishu.cn/document/docs/drive-v1/file/file-overview)
- [搜索云文档 - 飞书开放平台](https://open.feishu.cn/document/server-docs/docs/drive-v1/search/document-search)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 | 接口文档 |
|---------|---------|---------|----------|----------|
| BatchQueryMetasAsync | 批量获取文件元数据 | 用户令牌 | POST | [BatchQueryMetasAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/file/batch_query) |
| GetFileStatisticsByFileTokenAsync | 获取文件统计信息 | 用户令牌 | GET | [GetFileStatisticsByFileTokenAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/file/get) |
| GetFileViewRecordPageListByFileTokenAsync | 获取文件访问记录 | 用户令牌 | GET | [GetFileViewRecordPageListByFileTokenAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/file-view_record/list) |
| CopyFileByFileTokenAsync | 复制文件 | 用户令牌 | POST | [CopyFileByFileTokenAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/file/copy) |
| MoveFileByFileTokenAsync | 移动文件 | 用户令牌 | POST | [MoveFileByFileTokenAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/file/move) |
| DeleteFileByFileTokenAsync | 删除文件 | 用户令牌 | DELETE | [DeleteFileByFileTokenAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/file/delete) |
| CreateShortcutAsync | 创建文件快捷方式 | 用户令牌 | POST | [CreateShortcutAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/file/create_shortcut) |
| UploadAllFileAsync | 上传文件（完整上传） | 用户令牌 | POST | [UploadAllFileAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/upload/upload_all) |
| UploadPrepareFileAsync | 预上传（分片上传初始化） | 用户令牌 | POST | [UploadPrepareFileAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/upload/multipart-upload-file-/upload_prepare) |
| UploadPartFileAsync | 上传分片 | 用户令牌 | POST | [UploadPartFileAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/upload/multipart-upload-file-/upload_part) |
| UploadFinishFileAsync | 完成分片上传 | 用户令牌 | POST | [UploadFinishFileAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/upload/multipart-upload-file-/upload_finish) |
| DownloadFileAsync | 下载文件 | 用户令牌 | GET | [DownloadFileAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/download/download) |
| CreateImportTaskAsync | 创建导入任务 | 用户令牌 | POST | [CreateImportTaskAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/import_task/create) |
| GetImportTaskAsync | 查询导入任务结果 | 用户令牌 | GET | [GetImportTaskAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/import_task/get) |
| CreateExportTaskAsync | 创建导出任务 | 用户令牌 | POST | [CreateExportTaskAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/export_task/create) |
| GetExportTaskAsync | 查询导出任务结果 | 用户令牌 | GET | [GetExportTaskAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/export_task/get) |
| DownloadExportFileAsync | 下载导出文件 | 用户令牌 | GET | [DownloadExportFileAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/export_task/download) |
| DownloadExportLargeFileAsync | 下载导出大文件 | 用户令牌 | GET | [DownloadExportLargeFileAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/export_task/download) |
| GetFileLikePageListByFileTokenAsync | 获取文件点赞列表 | 用户令牌 | GET | [GetFileLikePageListByFileTokenAsync](https://open.feishu.cn/document/docs/drive-v1/like/list) |
| GetFileContentByFileTokenAsync | 获取云文档内容 | 用户令牌 | GET | [GetFileContentByFileTokenAsync](https://open.feishu.cn/document/docs/docs-v1/get) |
| SearchFilesAsync | 搜索云文档 | 用户令牌 | POST | [SearchFilesAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/search/document-search) |

## 函数详细内容

### 批量获取文件元数据

**函数签名**：
```csharp
Task<FeishuApiResult<MetasBatchQueryResult>?> BatchQueryMetasAsync(
    [Body] MetasBatchQueryRequest metasBatchQueryRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `metasBatchQueryRequest` | `MetasBatchQueryRequest` | ✅ | 请求体 |
| ├─ `RequestDocs` | `RequestDoc[]` | ✅ | 请求的文件的 token 和类型，一次请求中不可超过 200 个 |
| ├─ `WithUrl` | `bool?` | ⚪ | 是否获取文件的访问链接 |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型，默认 `open_id` |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "metas": [
      {
        "doc_token": "doccnfYZzTlvXqZIGTdAHKabcef",
        "doc_type": "docx",
        "title": "项目文档",
        "owner_id": "ou_xxxxxxxx",
        "create_time": "2024-01-15T08:30:00",
        "edit_time": "2024-01-20T14:22:00",
        "url": "https://example.feishu.cn/docx/xxx"
      }
    ],
    "failed_list": []
  }
}
```

**说明**：用于根据文件 token 获取其元数据，包括标题、所有者、创建时间、密级、访问链接等数据。

---

### 获取文件统计信息

**函数签名**：
```csharp
Task<FeishuApiResult<FileStatisticsReuslt>?> GetFileStatisticsByFileTokenAsync(
    [Path] string file_token,
    [Query("file_type")] string file_type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件 token，示例值：`doccnfYZzTlvXqZIGTdAHKabcef` |
| `file_type` | `string` | ✅ | 文件类型：`doc`/`docx`/`sheet`/`bitable`/`mindnote`/`wiki`/`file` |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "file_token": "doccnfYZzTlvXqZIGTdAHKabcef",
    "uv": 150,
    "pv": 320,
    "like_count": 25
  }
}
```

**说明**：获取各类文件的流量统计信息和互动信息，包括阅读人数（UV）、阅读次数（PV）和点赞数。

---

### 获取文件访问记录

**函数签名**：
```csharp
Task<FeishuApiPageListResult<FileViewRecord>?> GetFileViewRecordPageListByFileTokenAsync(
    [Path] string file_token,
    [Query("file_type")] string file_type,
    [Query("page_size")] int page_size = Consts.PageSize_10,
    [Query("page_token")] string? page_token = null,
    [Query("viewer_id_type")] string? viewer_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**说明**：获取文档、电子表格、多维表格等文件的历史访问记录，包括访问者的 ID、姓名、头像和最近访问时间。

---

### 复制文件

**函数签名**：
```csharp
Task<FeishuApiResult<CopyFileResult>?> CopyFileByFileTokenAsync(
    [Body] CopyFileRequest copyFileRequest,
    [Path] string file_token,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 源文件 token |
| `copyFileRequest` | `CopyFileRequest` | ✅ | 复制请求体 |
| ├─ `Name` | `string` | ✅ | 新文件名称，最大 256 字节 |
| ├─ `Type` | `string?` | ⚪ | 源文件类型 |
| ├─ `FolderToken` | `string` | ✅ | 目标文件夹 token |
| ├─ `Extras` | `Property[]?` | ⚪ | 自定义附加参数 |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "file_token": "doccnNewTokenxxxx",
    "task_id": "task_xxxxxxxx"
  }
}
```

**说明**：将用户云空间中的文件复制至其它文件夹下。该接口为异步接口。

---

### 移动文件

**函数签名**：
```csharp
Task<FeishuApiResult<FileTaskResult>?> MoveFileByFileTokenAsync(
    [Body] MoveFileRequest moveFileRequest,
    [Path] string file_token,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**说明**：将文件或者文件夹移动到用户云空间的其他位置。该接口为异步接口。

---

### 删除文件

**函数签名**：
```csharp
Task<FeishuApiResult<FileTaskResult>?> DeleteFileByFileTokenAsync(
    [Path] string file_token,
    [Query("type")] string file_type,
    [Query("async")] bool? async = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件 token |
| `file_type` | `string` | ✅ | 被删除文件的类型：`file`/`docx`/`bitable`/`folder`/`doc`/`sheet`/`mindnote`/`shortcut`/`slides` |
| `async` | `bool?` | ⚪ | 是否为异步删除文件，默认 `false`。填 `true` 时删除文件夹会返回 `task_id`，可继续调用查询异步任务状态接口查询任务执行状态 |

**说明**：删除用户在云空间内的文件或者文件夹。文件或文件夹被删除后，会进入回收站中。

---

### 创建文件快捷方式

**函数签名**：
```csharp
Task<FeishuApiResult<CreateShortcutResult>?> CreateShortcutAsync(
    [Body] CreateShortcutRequest createShortcutRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**说明**：创建指定文件的快捷方式到云空间的其它文件夹中。

---

### 上传文件（完整上传）

**函数签名**：
```csharp
Task<FeishuApiResult<FilesUploadAllResult>?> UploadAllFileAsync(
    [FormContent] UploadAllFileRequest uploadAllFileRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `uploadAllFileRequest` | `UploadAllFileRequest` | ✅ | 上传请求体 |
| ├─ `FileName` | `string` | ✅ | 文件名称，最大 250 字符 |
| ├─ `ParentType` | `string` | ✅ | 上传点类型，固定值 `explorer` |
| ├─ `ParentNode` | `string` | ✅ | 文件夹 token |
| ├─ `Size` | `int` | ✅ | 文件大小（字节），最大 20971520（20MB） |
| ├─ `Checksum` | `string?` | ⚪ | Adler-32 校验和 |
| ├─ `FilePath` | `string?` | ✅ | 本地文件绝对路径 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "file_token": "boxcnxxxxxxxx",
    "name": "demo.pdf",
    "size": 1024000,
    "url": "https://example.feishu.cn/file/xxx"
  }
}
```

**说明**：将指定文件上传至云空间指定目录中。文件大小不得超过 20 MB。

---

### 分片上传文件

包含三个步骤：

#### 1. 预上传（初始化）

**函数签名**：
```csharp
Task<FeishuApiResult<FilesUploadPrepareResult>?> UploadPrepareFileAsync(
    [Body] FilesUploadPrepareRequest filesUploadPartRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：请求包含文件名、父节点类型、父节点 token、文件大小等信息。

**说明**：发送初始化请求，以获取上传事务 ID 和分片策略，平台固定以 4MB 的大小对文件进行分片。上传事务 ID 和上传进度在 24 小时内有效。

#### 2. 上传分片

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UploadPartFileAsync(
    [FormContent] FilesUploadPartRequest filesUploadPartRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**说明**：根据预上传接口返回的上传事务 ID 和分片策略上传对应的文件分片。

#### 3. 完成上传

**函数签名**：
```csharp
Task<FeishuApiResult<FilesUploadFinishResult>?> UploadFinishFileAsync(
    [Body] FilesUploadFinishRequest filesUploadPartRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**说明**：将分片全部上传完毕后，需调用本接口触发完成上传，否则将上传失败。

---

### 下载文件

**函数签名**：
```csharp
Task<byte[]?> DownloadFileAsync(
    [Path] string file_token,
    [Header("Range")] string? range = null,
    [Query("version")] string? version = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件的 token |
| `range` | `string?` | ⚪ | 分片下载范围，格式：`bytes=start-end` |
| `version` | `string?` | ⚪ | 文件版本标识，指定下载的文件版本 |

**说明**：下载云空间中的文件，支持通过 Range 参数分片下载。

---

### 创建导入任务

**函数签名**：
```csharp
Task<FeishuApiResult<TasksResult>?> CreateImportTaskAsync(
    [Body] ImportTasksRequest importTasksRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**说明**：用于创建导入文件的任务，将本地文件如 Word、TXT、Markdown、Excel 等导入为飞书在线云文档。

---

### 查询导入任务结果

**函数签名**：
```csharp
Task<FeishuApiResult<ImportTaskResult>?> GetImportTaskAsync(
    [Path] string ticket,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**说明**：根据导入任务 ID 轮询导入结果。

---

### 创建导出任务

**函数签名**：
```csharp
Task<FeishuApiResult<TasksResult>?> CreateExportTaskAsync(
    [Body] ExportTasksRequest exportTasksRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**说明**：用于创建导出文件的任务，将飞书文档导出为 Word、Excel、PDF、CSV 格式。

---

### 查询导出任务结果

**函数签名**：
```csharp
Task<FeishuApiResult<ExportTasksResult>?> GetExportTaskAsync(
    [Path] string ticket,
    [Query] string? token,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**说明**：轮询导出任务结果，并返回导出文件的 token。

---

### 下载导出文件

**函数签名**：
```csharp
Task<byte[]?> DownloadExportFileAsync(
    [Path] string file_token, 
    CancellationToken cancellationToken = default);

Task DownloadExportLargeFileAsync(
    [Path] string file_token, 
    [FilePath] string localFile, 
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 导出的文件的 token |
| `localFile` | `string` | ✅ | 需要保存的本地文件路径（大文件下载） |

**说明**：下载导出产物到本地。需在导出任务结束 10 分钟内下载。

---

### 获取文件点赞列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<FileLikeInfo>?> GetFileLikePageListByFileTokenAsync(
    [Path] string file_token,
    [Query("file_type")] string file_type,
    [Query("page_size")] int page_size = Consts.PageSize_10,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**说明**：获取指定云文档的点赞者列表并按点赞时间由近到远分页返回。

---

### 获取云文档内容

**函数签名**：
```csharp
Task<FeishuApiResult<FileContentResult>?> GetFileContentByFileTokenAsync(
       [Query] string doc_token,
       [Query] string doc_type,
       [Query] string content_type,
       [Query] string? lang = "zh",
       CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `doc_token` | `string` | ✅ | 文件的 token，示例值：`XIHSdYSI7oMEU1xrsnxc8fabcef` |
| `doc_type` | `string` | ✅ | 云文档类型，可选值：`docx`（新版文档），示例值：`docx` |
| `content_type` | `string` | ✅ | 内容类型，可选值：`markdown`（Markdown 格式），示例值：`markdown` |
| `lang` | `string?` | ⚪ | 云文档中存在 @用户 元素时，指定该用户名称的语言，可选值：`zh`（中文，默认）/`en`（英文）/`ja`（日文） |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "content": "# 项目文档\n\n这是文档正文内容。"
  }
}
```

**说明**：获取指定云文档内容，当前只支持获取新版文档（`docx`）Markdown 格式的内容，返回 `FileContentResult`，`data.content` 即为 Markdown 文本。接口路径为 `GET /open-apis/docs/v1/content`，参数全部位于查询串（非路径参数）。

---

### 搜索云文档

**函数签名**：
```csharp
Task<FeishuApiResult<SearchFileObjectResult>?> SearchFilesAsync(
    [Body] SearchFileObjectRequest searchFileObjectRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `searchFileObjectRequest` | `SearchFileObjectRequest` | ✅ | 搜索请求体 |
| ├─ `SearchKey` | `string` | ✅ | 搜索关键词 |
| ├─ `Count` | `int?` | ⚪ | 返回文件数量，取值范围 [0,50] |
| ├─ `Offset` | `int?` | ⚪ | 偏移量，offset + count < 200 |
| ├─ `OwnerIds` | `string[]?` | ⚪ | 文件所有者的 Open ID 列表 |
| ├─ `ChatIds` | `string[]?` | ⚪ | 文件所在群的 ID 列表 |
| ├─ `DocsTypes` | `string[]?` | ⚪ | 文件类型过滤：`doc`/`sheet`/`slides`/`bitable`/`mindnote`/`file` |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "docs_entities": [
      {
        "docs_token": "doccnfYZzTlvXqZIGTdAHKabcef",
        "docs_type": "docx",
        "title": "产品需求文档",
        "owner_id": "ou_xxxxxxxx",
        "owner_name": "张三",
        "edit_time": "2024-01-20T14:22:00",
        "url": "https://example.feishu.cn/docx/xxx"
      }
    ],
    "has_more": false,
    "total": 15
  }
}
```

**说明**：用于根据搜索关键词对当前用户可见的云文档进行搜索。支持按所有者、群聊、文件类型等条件过滤。

**代码示例**：
```csharp
public class DocumentSearchService
{
    private readonly IFeishuUserV1DriveFiles _driveFilesClient;

    public DocumentSearchService(IFeishuUserV1DriveFiles driveFilesClient)
    {
        _driveFilesClient = driveFilesClient;
    }

    public async Task SearchUserDocumentsAsync(string keyword)
    {
        var request = new SearchFileObjectRequest
        {
            SearchKey = keyword,
            Count = 20,
            Offset = 0,
            DocsTypes = new[] { "docx", "sheet" }  // 只搜索文档和表格
        };

        var result = await _driveFilesClient.SearchFilesAsync(request);
        if (result?.Data?.DocsEntities != null)
        {
            foreach (var doc in result.Data.DocsEntities)
            {
                Console.WriteLine($"找到文档: {doc.Title}, 类型: {doc.DocsType}, 作者: {doc.OwnerName}");
            }
            Console.WriteLine($"总计: {result.Data.Total} 个结果");
        }
    }

    public async Task SearchByOwnerAsync(string keyword, string ownerId)
    {
        var request = new SearchFileObjectRequest
        {
            SearchKey = keyword,
            OwnerIds = new[] { ownerId },
            Count = 10
        };

        var result = await _driveFilesClient.SearchFilesAsync(request);
        // 处理搜索结果...
    }
}
```
