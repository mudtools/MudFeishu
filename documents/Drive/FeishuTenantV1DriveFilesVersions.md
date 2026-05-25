# 文件版本管理（租户令牌）- FeishuTenantV1DriveFilesVersions

## 接口名称
**文件版本管理（租户令牌）** -（`FeishuTenantV1DriveFilesVersions`）

## 功能描述
文件版本是基于文件生成的新版本，版本依附于文件而存在。本接口提供基于租户令牌的在线文档和电子表格版本管理功能，支持创建、删除和获取版本信息。适用于企业级应用场景，如文档版本控制、历史记录管理等。

## 参考文档
- [飞书官方文档 - 文件版本概述](https://open.feishu.cn/document/server-docs/docs/drive-v1/file-version/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateFileVersionAsync | 创建文档版本 | 租户令牌 | POST |
| GetFileVersionPageListByFileTokenAsync | 获取文档版本列表 | 租户令牌 | GET |
| GetFileVersionByFileTokenAsync | 获取指定版本信息 | 租户令牌 | GET |
| DeleteFileVersionByFileTokenAsync | 删除文档版本 | 租户令牌 | DELETE |

## 函数详细内容

### 创建文档版本

**函数名称**：创建文档版本

**函数签名**：
```csharp
Task<FeishuApiResult<CreateFileVersionResult>?> CreateFileVersionAsync(
    [Path] string file_token,
    [Body] CreateFileVersionRequest createFileVersionRequest,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| file_token | string | ✅ | 文件 token | doccnfYZzTlvXqZIGTdAHKabcef |
| createFileVersionRequest | CreateFileVersionRequest | ✅ | 创建版本请求体 | - |
| ├─ name | string | ✅ | 版本文档标题（最大1024个Unicode码点） | 项目文档 第 1 版 |
| ├─ obj_type | string | ✅ | 源文档类型：docx/sheet | docx |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 | - |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "name": "项目文档 第 1 版",
    "version": "fnJfyX",
    "parent_token": "doxbcyvqZlSc9WlHvQMlSJabcdf",
    "owner_id": "694699009591869450",
    "creator_id": "694699009591869451",
    "create_time": "1660708537",
    "update_time": "1660708537",
    "status": "0",
    "obj_type": "docx",
    "parent_type": "docx"
  }
}
```

**说明**：
- 该接口为异步接口
- 支持在线文档（docx）和电子表格（sheet）
- 版本创建后可在飞书文档中查看历史版本

**代码示例**：
```csharp
// 使用租户令牌创建文档版本
public class DocumentVersionService
{
    private readonly IFeishuTenantV1DriveFilesVersions _versionClient;

    public DocumentVersionService(IFeishuTenantV1DriveFilesVersions versionClient)
    {
        _versionClient = versionClient;
    }

    public async Task<CreateFileVersionResult?> CreateDocumentVersionAsync(string fileToken)
    {
        var request = new CreateFileVersionRequest
        {
            Name = "项目文档 V2.0",
            ObjType = "docx"
        };

        var result = await _versionClient.CreateFileVersionAsync(fileToken, request);
        return result?.Data;
    }
}
```

---

### 获取文档版本列表

**函数名称**：获取文档版本列表

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

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| file_token | string | ✅ | 文件 token | XIHSdYSI7oMEU1xrsnxc8fabcef |
| obj_type | string | ✅ | 源文档类型：docx/sheet | docx |
| page_size | int | ⚪ | 分页大小（默认10） | 10 |
| page_token | string | ⚪ | 分页标记 | - |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 | - |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "name": "项目文档 第1版",
        "version": "fnJfyX",
        "parent_token": "doxbcyvqZlSc9WlHvQMlSJabcef",
        "owner_id": "694699009591869450",
        "creator_id": "694699009591869451",
        "create_time": "1660708537",
        "update_time": "1660708537",
        "status": "0",
        "obj_type": "docx",
        "parent_type": "docx"
      }
    ],
    "page_token": "MTY1NTA3MTA1OXw3MTA4NDc2MDc1NzkyOTI0Nabcef",
    "has_more": true
  }
}
```

**说明**：
- 返回结果按创建时间倒序排列
- 支持分页查询，has_more 为 true 时表示还有更多数据

**代码示例**：
```csharp
// 分页获取文档版本列表
public class DocumentVersionService
{
    private readonly IFeishuTenantV1DriveFilesVersions _versionClient;

    public DocumentVersionService(IFeishuTenantV1DriveFilesVersions versionClient)
    {
        _versionClient = versionClient;
    }

    public async Task<List<FileVersionInfo>> GetAllVersionsAsync(string fileToken, string objType)
    {
        var versions = new List<FileVersionInfo>();
        string? pageToken = null;

        do
        {
            var result = await _versionClient.GetFileVersionPageListByFileTokenAsync(
                fileToken, objType, page_token: pageToken);

            if (result?.Data?.Items != null)
            {
                versions.AddRange(result.Data.Items);
                pageToken = result.Data.PageToken;
            }
        } while (!string.IsNullOrEmpty(pageToken));

        return versions;
    }
}
```

---

### 获取指定版本信息

**函数名称**：获取指定版本信息

**函数签名**：
```csharp
Task<FeishuApiResult<FileVersionInfo>?> GetFileVersionByFileTokenAsync(
    [Path] string? file_token,
    [Path] string version_id,
    [Query("obj_type")] string obj_type,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| file_token | string | ✅ | 文件 token | XIHSdYSI7oMEU1xrsnxc8fabcef |
| version_id | string | ✅ | 版本标识 | fnJfyX |
| obj_type | string | ✅ | 源文档类型：docx/sheet | docx |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 | - |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "name": "项目文档 第1版",
    "version": "fnJfyX",
    "parent_token": "doxbcyvqZlSc9WlHvQMlSJabcef",
    "owner_id": "694699009591869450",
    "creator_id": "694699009591869451",
    "create_time": "1660708537",
    "update_time": "1660708537",
    "status": "0",
    "obj_type": "docx",
    "parent_type": "docx"
  }
}
```

**说明**：
- 可获取指定版本的完整信息，包括标题、标识、创建者、创建时间等
- 状态值：0-正常，1-已删除，2-回收站

**代码示例**：
```csharp
// 获取指定版本详情
public class DocumentVersionService
{
    private readonly IFeishuTenantV1DriveFilesVersions _versionClient;

    public DocumentVersionService(IFeishuTenantV1DriveFilesVersions versionClient)
    {
        _versionClient = versionClient;
    }

    public async Task<FileVersionInfo?> GetVersionDetailAsync(
        string fileToken, string versionId, string objType)
    {
        var result = await _versionClient.GetFileVersionByFileTokenAsync(
            fileToken, versionId, objType);
        return result?.Data;
    }
}
```

---

### 删除文档版本

**函数名称**：删除文档版本

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteFileVersionByFileTokenAsync(
    [Path] string? file_token,
    [Path] string version_id,
    [Query("obj_type")] string obj_type,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| file_token | string | ✅ | 文件 token | XIHSdYSI7oMEU1xrsnxc8fabcef |
| version_id | string | ✅ | 版本标识 | fnJfyX |
| obj_type | string | ✅ | 源文档类型：docx/sheet | docx |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 | - |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：
- 删除的版本将进入回收站状态（status=2）
- 删除操作不可逆，请谨慎操作

**代码示例**：
```csharp
// 删除指定文档版本
public class DocumentVersionService
{
    private readonly IFeishuTenantV1DriveFilesVersions _versionClient;
    private readonly ILogger<DocumentVersionService> _logger;

    public DocumentVersionService(
        IFeishuTenantV1DriveFilesVersions versionClient,
        ILogger<DocumentVersionService> logger)
    {
        _versionClient = versionClient;
        _logger = logger;
    }

    public async Task<bool> DeleteVersionAsync(string fileToken, string versionId, string objType)
    {
        var result = await _versionClient.DeleteFileVersionByFileTokenAsync(
            fileToken, versionId, objType);

        if (result?.Code == 0)
        {
            _logger.LogInformation("版本 {VersionId} 删除成功", versionId);
            return true;
        }

        _logger.LogWarning("版本删除失败: {Msg}", result?.Msg);
        return false;
    }
}
```
