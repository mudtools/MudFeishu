# 文件版本管理 - 用户令牌（FeishuUserV1DriveFilesVersions）

## 接口名称
**文件版本管理（用户令牌）** -（`IFeishuUserV1DriveFilesVersions`）

## 功能描述
提供以用户身份管理飞书云空间文件版本的能力。文件版本是基于文件生成的新版本，版本依附于文件而存在。支持基于在线文档和电子表格创建、获取、删除版本信息。适用于需要以具体用户身份管理文档版本的业务场景，如文档版本回溯、版本对比等。

## 参考文档
- [文件版本概述 - 飞书开放平台](https://open.feishu.cn/document/server-docs/docs/drive-v1/file-version/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateFileVersionAsync | 创建文档版本 | 用户令牌 | POST |
| GetFileVersionPageListByFileTokenAsync | 获取文档版本列表 | 用户令牌 | GET |
| GetFileVersionByFileTokenAsync | 获取指定版本信息 | 用户令牌 | GET |
| DeleteFileVersionByFileTokenAsync | 删除文档版本 | 用户令牌 | DELETE |

## 函数详细内容

### 创建文档版本

**函数签名**：
```csharp
Task<FeishuApiResult<CreateFileVersionResult>?> CreateFileVersionAsync(
    [Path] string file_token,
    [Body] CreateFileVersionRequest createFileVersionRequest,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string` | ✅ | 文件 token，示例值：`doccnfYZzTlvXqZIGTdAHKabcef` |
| `createFileVersionRequest` | `CreateFileVersionRequest` | ✅ | 创建版本请求体 |
| ├─ `Name` | `string` | ✅ | 版本标题，最大 1024 个 Unicode 码点 |
| ├─ `ObjType` | `string` | ✅ | 源文档类型：`docx`（新版文档）/`sheet`（电子表格） |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型，默认 `open_id` |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "version_id": "fnJfyX",
    "create_time": "2024-01-20T14:22:00",
    "creator": "ou_xxxxxxxx"
  }
}
```

**说明**：该接口为异步接口，用于为在线文档或电子表格创建新版本。创建后的版本可用于后续回溯或对比。

---

### 获取文档版本列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<FileVersionInfo>?> GetFileVersionPageListByFileTokenAsync(
    [Path] string? file_token,
    [Query("obj_type")] string obj_type,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string?` | ✅ | 文件 token |
| `obj_type` | `string` | ✅ | 源文档类型：`docx`/`sheet` |
| `page_size` | `int` | ⚪ | 分页大小，默认 10 |
| `page_token` | `string?` | ⚪ | 分页标记，首次请求不填 |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型，默认 `open_id` |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "version_id": "fnJfyX",
        "name": "项目文档 第 1 版",
        "creator": "ou_xxxxxxxx",
        "create_time": "2024-01-20T14:22:00"
      }
    ],
    "page_token": "xxx",
    "has_more": false
  }
}
```

**说明**：获取文档的所有历史版本列表，按创建时间倒序排列。

---

### 获取指定版本信息

**函数签名**：
```csharp
Task<FeishuApiResult<FileVersionInfo>?> GetFileVersionByFileTokenAsync(
    [Path] string? file_token,
    [Path] string version_id,
    [Query("obj_type")] string obj_type,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string?` | ✅ | 文件 token |
| `version_id` | `string` | ✅ | 版本标识，示例值：`fnJfyX` |
| `obj_type` | `string` | ✅ | 源文档类型：`docx`/`sheet` |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型，默认 `open_id` |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "version_id": "fnJfyX",
    "name": "项目文档 第 1 版",
    "creator": "ou_xxxxxxxx",
    "create_time": "2024-01-20T14:22:00"
  }
}
```

**说明**：用于获取文档或电子表格指定版本的详细信息，包括标题、标识、创建者、创建时间等。

---

### 删除文档版本

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteFileVersionByFileTokenAsync(
    [Path] string? file_token,
    [Path] string version_id,
    [Query("obj_type")] string obj_type,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `file_token` | `string?` | ✅ | 文件 token |
| `version_id` | `string` | ✅ | 版本标识 |
| `obj_type` | `string` | ✅ | 源文档类型：`docx`/`sheet` |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型，默认 `open_id` |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除基于在线文档或电子表格创建的版本。删除后无法恢复，请谨慎操作。

---

**代码示例**：
```csharp
// 使用用户令牌管理文档版本
public class DocumentVersionService
{
    private readonly IFeishuUserV1DriveFilesVersions _versionClient;

    public DocumentVersionService(IFeishuUserV1DriveFilesVersions versionClient)
    {
        _versionClient = versionClient;
    }

    public async Task CreateNewVersionAsync(string fileToken, string versionName)
    {
        // 创建新版本
        var request = new CreateFileVersionRequest
        {
            Name = versionName,
            ObjType = "docx"
        };

        var result = await _versionClient.CreateFileVersionAsync(fileToken, request);
        if (result?.Data != null)
        {
            Console.WriteLine($"版本创建成功，版本 ID: {result.Data.VersionId}");
        }
    }

    public async Task ListAllVersionsAsync(string fileToken)
    {
        // 获取所有版本
        var result = await _versionClient.GetFileVersionPageListByFileTokenAsync(
            fileToken, 
            obj_type: "docx",
            page_size: 20);

        if (result?.Data?.Items != null)
        {
            foreach (var version in result.Data.Items)
            {
                Console.WriteLine($"版本: {version.Name}, 创建者: {version.Creator}, 时间: {version.CreateTime}");
            }
        }
    }
}
```
