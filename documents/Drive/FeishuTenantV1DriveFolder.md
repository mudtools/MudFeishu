# 文件夹管理 - 租户令牌（FeishuTenantV1DriveFolder）

## 接口名称
**文件夹管理（租户令牌）** -（`IFeishuTenantV1DriveFolder`）

## 功能描述
提供以租户身份管理飞书云空间文件夹的能力。文件夹是飞书云空间中用于管理文件和其它文件夹的容器，每个文件夹都有唯一的 token 作为标识。支持获取文件夹元数据、创建文件夹、获取文件清单以及查询异步任务状态。适用于需要以应用身份管理云空间文件夹的业务场景，如企业级文档管理、自动化文件夹创建等。

## 参考文档
- [文件夹概述 - 飞书开放平台](https://open.feishu.cn/document/docs/drive-v1/folder/folder-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| GetDriveRootFolderMetaAsync | 获取根文件夹元数据 | 租户令牌 | GET |
| GetFilesPageListAsync | 获取文件夹中的文件清单 | 租户令牌 | GET |
| GetFolderMetaByTokenAsync | 获取文件夹元数据 | 租户令牌 | GET |
| CreateFolderAsync | 创建文件夹 | 租户令牌 | POST |
| GetTaskCheckFileAsync | 查询异步任务状态 | 租户令牌 | GET |

## 函数详细内容

### 获取根文件夹元数据

**函数签名**：
```csharp
Task<FeishuApiResult<GetDriveRootFolderMetaReuslt>?> GetDriveRootFolderMetaAsync(
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：无

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "token": "fldbcO1UuPz8VwnpPx5a92abcef",
    "id": "7363598283275341826",
    "user_id": "ou_xxxxxxxx"
  }
}
```

**说明**：获取用户"我的空间"（根文件夹）的元数据，包括根文件夹的 token、ID 和文件夹所有者的 ID。

---

### 获取文件夹中的文件清单

**函数签名**：
```csharp
Task<FeishuApiResult<GetDriveFilesResult>?> GetFilesPageListAsync(
    [Query("folder_token")] string? folder_token,
    [Query("order_by")] string? order_by = "EditedTime",
    [Query("direction")] string? direction = "DESC",
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `folder_token` | `string?` | ⚪ | 文件夹 token，不填则获取根目录清单 |
| `order_by` | `string?` | ⚪ | 排序字段：`CreatedTime`/`EditedTime`，默认 `EditedTime` |
| `direction` | `string?` | ⚪ | 排序方向：`ASC`/`DESC`，默认 `DESC` |
| `page_size` | `int` | ⚪ | 分页大小，默认 10 |
| `page_token` | `string?` | ⚪ | 分页标记，首次请求不填 |
| `user_id_type` | `string?` | ⚪ | 用户 ID 类型，默认 `open_id` |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "files": [
      {
        "token": "doccnfYZzTlvXqZIGTdAHKabcef",
        "name": "项目文档",
        "type": "docx",
        "owner_id": "ou_xxxxxxxx",
        "create_time": "2024-01-15T08:30:00",
        "edit_time": "2024-01-20T14:22:00"
      }
    ],
    "next_page_token": "xxx",
    "has_more": true
  }
}
```

**说明**：获取指定文件夹内的文件和子文件夹清单。不填写 folder_token 时将获取根目录清单，但不支持分页和返回快捷方式。

---

### 获取文件夹元数据

**函数签名**：
```csharp
Task<FeishuApiResult<GetFolderMetaResult>?> GetFolderMetaByTokenAsync(
    [Path] string? folderToken,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `folderToken` | `string?` | ✅ | 文件夹 token |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "token": "fldbcO1UuPz8VwnpPx5a92abcef",
    "id": "7363598283275341826",
    "name": "产品文档",
    "owner_id": "ou_xxxxxxxx",
    "create_time": "2024-01-15T08:30:00"
  }
}
```

**说明**：用于根据文件夹 token 获取该文件夹的元数据，包括文件夹的 ID、名称、创建者 ID 等。

---

### 创建文件夹

**函数签名**：
```csharp
Task<FeishuApiResult<CreateFolderResult>?> CreateFolderAsync(
    [Body] CreateFolderRequest createFolderRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `createFolderRequest` | `CreateFolderRequest` | ✅ | 创建文件夹请求体 |
| ├─ `Name` | `string` | ✅ | 文件夹名称，1~256 字节 |
| ├─ `FolderToken` | `string` | ✅ | 父文件夹 token，空字符串表示根目录 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "token": "fldbcNewFolderxxx",
    "name": "产品优化项目",
    "url": "https://example.feishu.cn/drive/folder/xxx"
  }
}
```

**说明**：用于在云空间指定文件夹中创建一个空文件夹。创建成功后返回新文件夹的 token 和访问链接。

---

### 查询异步任务状态

**函数签名**：
```csharp
Task<FeishuApiResult<FilesTaskCheckResult>?> GetTaskCheckFileAsync(
    [Query("task_id")] string task_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|-----|------|
| `task_id` | `string` | ✅ | 异步任务 ID |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "task_id": "task_xxxxxxxx",
    "status": "success",
    "progress": 100
  }
}
```

**说明**：查询异步任务的状态信息。目前支持查询删除文件夹和移动文件夹的异步任务。

---

**代码示例**：
```csharp
// 使用租户令牌管理文件夹
public class FolderManagementService
{
    private readonly IFeishuTenantV1DriveFolder _folderClient;

    public FolderManagementService(IFeishuTenantV1DriveFolder folderClient)
    {
        _folderClient = folderClient;
    }

    public async Task CreateProjectFolderAsync(string projectName, string parentFolderToken)
    {
        // 创建项目文件夹
        var request = new CreateFolderRequest
        {
            Name = projectName,
            FolderToken = parentFolderToken
        };

        var result = await _folderClient.CreateFolderAsync(request);
        if (result?.Data != null)
        {
            Console.WriteLine($"文件夹创建成功，Token: {result.Data.Token}");
        }
    }

    public async Task ListFolderContentsAsync(string folderToken)
    {
        // 获取文件夹内容
        var result = await _folderClient.GetFilesPageListAsync(
            folder_token: folderToken,
            order_by: "EditedTime",
            direction: "DESC",
            page_size: 50);

        if (result?.Data?.Files != null)
        {
            foreach (var file in result.Data.Files)
            {
                Console.WriteLine($"文件: {file.Name}, 类型: {file.Type}");
            }
        }
    }
}
```
