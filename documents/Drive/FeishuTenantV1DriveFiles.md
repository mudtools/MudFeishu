# 云空间文件管理 - 租户令牌（FeishuTenantV1DriveFiles）

## 接口名称
**云空间文件管理（租户令牌）** -（`IFeishuTenantV1DriveFiles`）

## 功能描述
提供飞书云空间文件的全生命周期管理能力，包括文件元数据查询、文件统计信息获取、访问记录查询、文件复制/移动/删除、快捷方式创建、文件上传下载、以及文件导入导出等功能。适用于需要以应用身份管理企业云文档资源的业务场景。

## 参考文档
- [文件概述 - 飞书开放平台](https://open.feishu.cn/document/docs/drive-v1/file/file-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| BatchQueryMetasAsync | 批量获取文件元数据 | 租户令牌 | POST |
| GetFileStatisticsByFileTokenAsync | 获取文件统计信息 | 租户令牌 | GET |
| GetFileViewRecordPageListByFileTokenAsync | 获取文件访问记录 | 租户令牌 | GET |
| CopyFileByFileTokenAsync | 复制文件 | 租户令牌 | POST |
| MoveFileByFileTokenAsync | 移动文件 | 租户令牌 | POST |
| DeleteFileByFileTokenAsync | 删除文件 | 租户令牌 | DELETE |
| CreateShortcutAsync | 创建文件快捷方式 | 租户令牌 | POST |
| UploadAllFileAsync | 上传文件（完整上传） | 租户令牌 | POST |
| UploadPrepareFileAsync | 预上传（分片上传初始化） | 租户令牌 | POST |
| UploadPartFileAsync | 上传分片 | 租户令牌 | POST |
| UploadFinishFileAsync | 完成分片上传 | 租户令牌 | POST |
| DownloadFileAsync | 下载文件 | 租户令牌 | GET |
| CreateImportTaskAsync | 创建导入任务 | 租户令牌 | POST |
| GetImportTaskAsync | 查询导入任务结果 | 租户令牌 | GET |
| CreateExportTaskAsync | 创建导出任务 | 租户令牌 | POST |
| GetExportTaskAsync | 查询导出任务结果 | 租户令牌 | GET |
| DownloadExportFileAsync | 下载导出文件 | 租户令牌 | GET |
| DownloadExportLargeFileAsync | 下载导出大文件 | 租户令牌 | GET |
| GetFileLikePageListByFileTokenAsync | 获取文件点赞列表 | 租户令牌 | GET |

## 函数详细内容

### 批量获取文件元数据

**函数签名**：
```csharp
Task<FeishuApiResult<MetasBatchQueryResult>?> BatchQueryMetasAsync(
    [Body] MetasBatchQueryRequest metasBatchQueryRequest,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

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

**代码示例**：
```csharp
public class FileMetaService
{
    private readonly IFeishuTenantV1DriveFiles _driveFilesClient;

    public FileMetaService(IFeishuTenantV1DriveFiles driveFilesClient)
    {
        _driveFilesClient = driveFilesClient;
    }

    public async Task GetFilesMetaAsync()
    {
        var request = new MetasBatchQueryRequest
        {
            RequestDocs = new[]
            {
                new RequestDoc { DocToken = "doccnfYZzTlvXqZIGTdAHKabcef", DocType = "docx" },
                new RequestDoc { DocToken = "sheetabc123", DocType = "sheet" }
            },
            WithUrl = true
        };

        var result = await _driveFilesClient.BatchQueryMetasAsync(request);
        if (result?.Data?.Metas != null)
        {
            foreach (var meta in result.Data.Metas)
            {
                Console.WriteLine($"文件: {meta.Title}, 所有者: {meta.OwnerId}");
            }
        }
    }
}
```

---

### 获取文件统计信息

**函数签名**：
```csharp
Task<FeishuApiResult<FileStatisticsReuslt>?> GetFileStatisticsByFileTokenAsync(
    [Path] string? file_token,
    [Query("file_type")] string file_type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string?` | ✅ | 文件 token，示例值：`doccnfYZzTlvXqZIGTdAHKabcef` |
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

**代码示例**：
```csharp
public async Task GetFileStatsAsync(string fileToken, string fileType)
{
    var result = await _driveFilesClient.GetFileStatisticsByFileTokenAsync(
        file_token: fileToken,
        file_type: fileType);

    if (result?.Data != null)
    {
        Console.WriteLine($"阅读人数: {result.Data.Uv}, 阅读次数: {result.Data.Pv}, 点赞: {result.Data.LikeCount}");
    }
}
```

---

### 获取文件访问记录

**函数签名**：
```csharp
Task<FeishuApiPageListResult<FileViewRecord>?> GetFileViewRecordPageListByFileTokenAsync(
    [Path] string? file_token,
    [Query("file_type")] string file_type,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("viewer_id_type")] string? viewer_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string?` | ✅ | 文件的 token |
| `file_type` | `string` | ✅ | 文件类型 |
| `page_size` | `int` | ⚪ | 分页大小，默认 10 |
| `page_token` | `string?` | ⚪ | 分页标记，首次请求为空 |
| `viewer_id_type` | `string?` | ⚪ | 查看者 ID 类型 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "viewer_id": "ou_xxxxxxxx",
        "viewer_name": "张三",
        "viewer_avatar": "https://example.com/avatar.jpg",
        "view_time": "2024-01-20T14:22:00"
      }
    ],
    "page_token": "next_page_token",
    "has_more": true
  }
}
```

**说明**：获取文档、电子表格、多维表格等文件的历史访问记录，包括访问者的 ID、姓名、头像和最近访问时间。

---

### 复制文件

**函数签名**：
```csharp
Task<FeishuApiResult<CopyFileResult>?> CopyFileByFileTokenAsync(
    [Body] CopyFileRequest copyFileRequest,
    [Path] string? file_token,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string?` | ✅ | 源文件 token |
| `copyFileRequest` | `CopyFileRequest` | ✅ | 复制请求体 |
| ├─ `Name` | `string` | ✅ | 新文件名称，最大 256 字节 |
| ├─ `Type` | `string?` | ⚪ | 源文件类型（必须与实际类型一致） |
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

**代码示例**：
```csharp
public async Task<string> CopyDocumentAsync(string sourceToken, string newName, string targetFolder)
{
    var request = new CopyFileRequest
    {
        Name = newName,
        Type = "docx",
        FolderToken = targetFolder
    };

    var result = await _driveFilesClient.CopyFileByFileTokenAsync(
        copyFileRequest: request,
        file_token: sourceToken);

    return result?.Data?.FileToken ?? string.Empty;
}
```

---

### 移动文件

**函数签名**：
```csharp
Task<FeishuApiResult<FileTaskResult>?> MoveFileByFileTokenAsync(
    [Body] MoveFileRequest moveFileRequest,
    [Path] string? file_token,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string?` | ✅ | 文件 token |
| `moveFileRequest` | `MoveFileRequest` | ✅ | 移动请求体 |
| ├─ `FolderToken` | `string` | ✅ | 目标文件夹 token |
| ├─ `Type` | `string?` | ⚪ | 文件类型 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "task_id": "task_xxxxxxxx"
  }
}
```

**说明**：将文件或者文件夹移动到用户云空间的其他位置。该接口为异步接口。

---

### 删除文件

**函数签名**：
```csharp
Task<FeishuApiResult<FileTaskResult>?> DeleteFileByFileTokenAsync(
    [Path] string? file_token,
    [Query("type")] string file_type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string?` | ✅ | 文件 token |
| `file_type` | `string` | ✅ | 被删除文件的类型：`file`/`docx`/`bitable`/`folder`/`doc`/`sheet`/`mindnote`/`shortcut`/`slides` |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "task_id": "task_xxxxxxxx"
  }
}
```

**说明**：删除用户在云空间内的文件或者文件夹。文件或文件夹被删除后，会进入回收站中。

---

### 创建文件快捷方式

**函数签名**：
```csharp
Task<FeishuApiResult<CreateShortcutResult>?> CreateShortcutAsync(
    [Body] CreateShortcutRequest createShortcutRequest,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `createShortcutRequest` | `CreateShortcutRequest` | ✅ | 请求体 |
| ├─ `ParentToken` | `string` | ✅ | 目标父文件夹的 token |
| ├─ `ReferEntity` | `CreateShortcutReferEntity` | ✅ | 源文件的信息 |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "shortcut_token": "shtcutxxxxxxxx"
  }
}
```

**说明**：创建指定文件的快捷方式到云空间的其它文件夹中。

---

### 上传文件（完整上传）

**函数签名**：
```csharp
Task<FeishuApiResult<FilesUploadAllResult>?> UploadAllFileAsync(
    [FormContent] UploadAllFileRequest uploadAllFileRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

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

**说明**：将指定文件上传至云空间指定目录中。文件大小不得超过 20 MB。调用频率上限为 5 QPS，10000 次/天。

**代码示例**：
```csharp
public async Task UploadDocumentAsync(string localFilePath, string targetFolder)
{
    var fileInfo = new FileInfo(localFilePath);
    var request = new UploadAllFileRequest
    {
        FileName = fileInfo.Name,
        ParentType = "explorer",
        ParentNode = targetFolder,
        Size = (int)fileInfo.Length,
        FilePath = localFilePath
    };

    var result = await _driveFilesClient.UploadAllFileAsync(request);
    Console.WriteLine($"上传成功，文件 token: {result?.Data?.FileToken}");
}
```

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

**参数**：请求包含文件名、父节点类型、父节点 token、文件大小等信息。

**说明**：发送初始化请求，以获取上传事务 ID 和分片策略，平台固定以 4MB 的大小对文件进行分片。上传事务 ID 和上传进度在 24 小时内有效。

#### 2. 上传分片

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UploadPartFileAsync(
    [FormContent] FilesUploadPartRequest filesUploadPartRequest,
    CancellationToken cancellationToken = default);
```

**说明**：根据预上传接口返回的上传事务 ID 和分片策略上传对应的文件分片。

#### 3. 完成上传

**函数签名**：
```csharp
Task<FeishuApiResult<FilesUploadFinishResult>?> UploadFinishFileAsync(
    [Body] FilesUploadFinishRequest filesUploadPartRequest,
    CancellationToken cancellationToken = default);
```

**说明**：将分片全部上传完毕后，需调用本接口触发完成上传，否则将上传失败。

---

### 下载文件

**函数签名**：
```csharp
Task<byte[]?> DownloadFileAsync(
    [Path] string file_token,
    [Header("Range")] string? range = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件的 token |
| `range` | `string?` | ⚪ | 分片下载范围，格式：`bytes=start-end` |

**响应**：文件字节流

**说明**：下载云空间中的文件，如 PDF 文件。不包含飞书文档、电子表格以及多维表格等在线文档。支持通过 Range 参数分片下载。

**代码示例**：
```csharp
public async Task DownloadDocumentAsync(string fileToken, string savePath)
{
    var fileBytes = await _driveFilesClient.DownloadFileAsync(fileToken);
    if (fileBytes != null)
    {
        await File.WriteAllBytesAsync(savePath, fileBytes);
        Console.WriteLine($"文件已下载到: {savePath}");
    }
}
```

---

### 创建导入任务

**函数签名**：
```csharp
Task<FeishuApiResult<TasksResult>?> CreateImportTaskAsync(
    [Body] ImportTasksRequest importTasksRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**说明**：用于创建导入文件的任务，并返回导入任务 ID。导入文件指将本地文件如 Word、TXT、Markdown、Excel 等格式的文件导入为某种格式的飞书在线云文档。该接口为异步接口。

---

### 查询导入任务结果

**函数签名**：
```csharp
Task<FeishuApiResult<ImportTaskResult>?> GetImportTaskAsync(
    [Path] string ticket,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `ticket` | `string` | ✅ | 导入任务 ID |

**说明**：根据创建导入任务返回的导入任务 ID（ticket）轮询导入结果。

---

### 创建导出任务

**函数签名**：
```csharp
Task<FeishuApiResult<TasksResult>?> CreateExportTaskAsync(
    [Body] ExportTasksRequest exportTasksRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**说明**：用于创建导出文件的任务，并返回导出任务 ID。导出文件指将飞书文档、电子表格、多维表格导出为本地文件，包括 Word、Excel、PDF、CSV 格式。该接口为异步接口。

---

### 查询导出任务结果

**函数签名**：
```csharp
Task<FeishuApiResult<ExportTasksResult>?> GetExportTaskAsync(
    [Path] string ticket,
    [Query] string? token,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `ticket` | `string` | ✅ | 导出任务 ID |
| `token` | `string?` | ⚪ | 要导出的云文档的 token |

**说明**：根据创建导出任务返回的导出任务 ID（ticket）轮询导出任务结果，并返回导出文件的 token。

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

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 导出的文件的 token |
| `localFile` | `string` | ✅ | 需要保存的本地文件路径（大文件下载） |

**说明**：根据查询导出任务结果返回的导出文件的 token，下载导出产物到本地。需在导出任务结束 10 分钟内下载，否则文件将被删除。

---

### 获取文件点赞列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<FileLikeInfo>?> GetFileLikePageListByFileTokenAsync(
    [Path] string? file_token,
    [Query("file_type")] string file_type,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string?` | ✅ | 文件的 token |
| `file_type` | `string` | ✅ | 云文档类型：`doc`/`docx`/`file` |
| `page_size` | `int` | ⚪ | 分页大小，默认 10 |
| `page_token` | `string?` | ⚪ | 分页标记 |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型 |

**说明**：获取指定云文档的点赞者列表并按点赞时间由近到远分页返回。
