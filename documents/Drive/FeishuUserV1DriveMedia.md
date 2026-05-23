# 素材管理 - 用户令牌（FeishuUserV1DriveMedia）

## 接口名称
**素材管理（用户权限）** -（`IFeishuUserV1DriveMedia`）

## 功能描述
提供以用户身份管理飞书云文档素材的能力。素材指在文档、电子表格、多维表格等中用到的资源素材，如文档中的图片、视频或文件等，每个素材都有唯一的 token 作为标识。支持素材的上传（包括完整上传和分片上传）、下载和获取临时下载链接。适用于需要以具体用户身份操作云文档素材的业务场景，如个人文档编辑、用户自主上传素材等。

## 参考文档
- [素材概述 - 飞书开放平台](https://open.feishu.cn/document/server-docs/docs/drive-v1/media/introduction)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| UploadAllMediaAsync | 上传素材（完整上传） | 用户令牌 | POST |
| UploadPrepareMediaAsync | 预上传（分片上传初始化） | 用户令牌 | POST |
| UploadPartMediaAsync | 上传分片 | 用户令牌 | POST |
| UploadFinishMediaAsync | 完成分片上传 | 用户令牌 | POST |
| DownloadFileAsync | 下载素材 | 用户令牌 | GET |
| BatchGetTmpDownloadUrlAsync | 获取素材临时下载链接 | 用户令牌 | GET |

## 函数详细内容

### 上传素材（完整上传）

**函数签名**：
```csharp
Task<FeishuApiResult<FilesUploadAllResult>?> UploadAllMediaAsync(
    [FormContent] MediasUploadAllRequest mediasUploadAllRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `mediasUploadAllRequest` | `MediasUploadAllRequest` | ✅ | 上传素材请求体 |
| ├─ `FileName` | `string` | ✅ | 素材名称，最大 250 字符 |
| ├─ `ParentType` | `string` | ✅ | 上传点类型，如 `docx_image`、`sheet_file` 等 |
| ├─ `ParentNode` | `string` | ✅ | 上传点 token（目标云文档 token） |
| ├─ `Size` | `int` | ✅ | 文件大小（字节），最大 20971520（20MB） |
| ├─ `Checksum` | `string?` | ⚪ | Adler-32 校验和 |
| ├─ `Extra` | `string?` | ⚪ | 额外参数，JSON 格式 |
| ├─ `FilePath` | `string?` | ✅ | 本地文件绝对路径 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "file_token": "boxcnxxxxxxxx",
    "name": "demo.jpeg",
    "size": 1024000,
    "url": "https://example.feishu.cn/drive/media/xxx"
  }
}
```

**说明**：将文件、图片、视频等素材上传到指定云文档中。素材大小不得超过 20 MB，超过请使用分片上传。调用频率上限为 5 QPS，10000 次/天。

---

### 预上传（分片上传初始化）

**函数签名**：
```csharp
Task<FeishuApiResult<FilesUploadPrepareResult>?> UploadPrepareMediaAsync(
    [Body] MediasUploadPrepareRequest mediasUploadPrepareRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `mediasUploadPrepareRequest` | `MediasUploadPrepareRequest` | ✅ | 预上传请求体 |
| ├─ `FileName` | `string` | ✅ | 素材名称 |
| ├─ `ParentType` | `string` | ✅ | 上传点类型 |
| ├─ `ParentNode` | `string` | ✅ | 上传点 token |
| ├─ `Size` | `int` | ✅ | 文件大小（字节） |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "upload_id": "upload_xxxxxxxx",
    "block_size": 4194304,
    "block_num": 3
  }
}
```

**说明**：发送初始化请求获取上传事务 ID 和分片策略。平台固定以 4MB 大小对素材进行分片。上传事务 ID 在 24 小时内有效。

---

### 上传分片

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UploadPartMediaAsync(
    [FormContent] FilesUploadPartRequest filesUploadPartRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `filesUploadPartRequest` | `FilesUploadPartRequest` | ✅ | 上传分片请求体 |
| ├─ `UploadId` | `string` | ✅ | 上传事务 ID |
| ├─ `Seq` | `int` | ✅ | 分片序号，从 0 开始 |
| ├─ `FilePath` | `string?` | ✅ | 分片文件路径 |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：根据预上传接口返回的上传事务 ID 和分片策略上传对应的文件分片。

---

### 完成分片上传

**函数签名**：
```csharp
Task<FeishuApiResult<FilesUploadFinishResult>?> UploadFinishMediaAsync(
    [Body] FilesUploadFinishRequest filesUploadPartRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `filesUploadPartRequest` | `FilesUploadFinishRequest` | ✅ | 完成上传请求体 |
| ├─ `UploadId` | `string` | ✅ | 上传事务 ID |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "file_token": "boxcnxxxxxxxx",
    "name": "demo.jpeg",
    "size": 15728640
  }
}
```

**说明**：将分片全部上传完毕后，需调用本接口触发完成上传。否则将上传失败。

---

### 下载素材

**函数签名**：
```csharp
Task<byte[]?> DownloadFileAsync(
    [Path] string file_token,
    [Query("extra")] string extra,
    [Header("Range")] string? range = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件 token |
| `extra` | `string` | ✅ | 额外参数，用于多维表格鉴权 |
| `range` | `string?` | ⚪ | 分片下载范围，如 `bytes=0-1024` |

**响应**：文件二进制数据

**说明**：下载各类云文档中的素材，例如电子表格中的图片。支持通过 Range 参数分片下载。

---

### 获取素材临时下载链接

**函数签名**：
```csharp
Task<FeishuApiResult<BatchGetTmpDownloadUrlResult>?> BatchGetTmpDownloadUrlAsync(
    [Query("file_tokens")] string file_token,
    [Query("extra")] string extra,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件 token |
| `extra` | `string` | ✅ | 额外参数，用于多维表格鉴权 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "tmp_download_urls": [
      {
        "file_token": "boxcnxxxxxxxx",
        "tmp_download_url": "https://example.feishu.cn/download/xxx",
        "expire_time": "2024-01-21T14:22:00"
      }
    ]
  }
}
```

**说明**：获取云文档中素材的临时下载链接，链接有效期为 24 小时。调用前需确保用户拥有素材的下载权限。

---

**代码示例**：
```csharp
// 使用用户权限管理素材
public class UserMediaService
{
    private readonly IFeishuUserV1DriveMedia _mediaClient;

    public UserMediaService(IFeishuUserV1DriveMedia mediaClient)
    {
        _mediaClient = mediaClient;
    }

    public async Task UploadAttachmentToSheetAsync(string sheetToken, string filePath)
    {
        // 上传附件到电子表格
        var request = new MediasUploadAllRequest
        {
            FileName = Path.GetFileName(filePath),
            ParentType = "sheet_file",
            ParentNode = sheetToken,
            Size = (int)new FileInfo(filePath).Length,
            FilePath = filePath
        };

        var result = await _mediaClient.UploadAllMediaAsync(request);
        if (result?.Data != null)
        {
            Console.WriteLine($"附件上传成功，Token: {result.Data.FileToken}");
        }
    }

    public async Task<byte[]> DownloadMediaAsync(string fileToken)
    {
        // 下载素材
        var data = await _mediaClient.DownloadFileAsync(fileToken, extra: "{}");
        if (data != null)
        {
            Console.WriteLine($"素材下载成功，大小: {data.Length} 字节");
        }
        return data ?? Array.Empty<byte>();
    }
}
```
