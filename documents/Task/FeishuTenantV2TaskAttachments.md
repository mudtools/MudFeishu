# 任务附件 V2 - 租户权限

## 接口名称
**任务附件 V2 -（IFeishuTenantV2TaskAttachments）**

## 功能描述
任务可以拥有附件。一个附件可以是任意类型的文件，如图片、PDF文档、zip文件等。附件不可以单独存在，必须与某种资源产生关联关系。关联附件的资源类型只有任务。

因为附件不可单独存在，因此为新任务添加附件时，必须先调用创建任务接口完成任务创建，再调用上传附件接口上传文件，并关联到新建的任务上。

本接口提供以租户身份管理任务附件的能力，包括上传、列取、获取详情和删除附件等功能。

## 参考文档
- [飞书开放平台 - 任务附件功能概述](https://open.feishu.cn/document/task-v2/attachment/attachment-feature-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| UploadAttachmentAsync | 上传附件 | 租户令牌 | POST |
| GetAttachmentPageListAsync | 列取附件列表 | 租户令牌 | GET |
| GetAttachmentByIdAsync | 获取附件详情 | 租户令牌 | GET |
| DeleteAttachmentByIdAsync | 删除附件 | 租户令牌 | DELETE |

---

## 函数详细内容

### 上传附件

**函数名称**：上传附件

**函数签名**：
```csharp
Task<FeishuApiResult<TaskAttachmentsUploadResult>?> UploadAttachmentAsync(
    [FormContent] UploadTaskAttachmentsRequest uploadFileRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| uploadFileRequest | ✅ | UploadTaskAttachmentsRequest | 上传附件请求体 |
| └ resource_type | ⚪ | string | 附件归属资源的类型，默认"task" |
| └ resource_id | ✅ | string | 附件要归属资源的id（任务GUID） |
| └ file | ✅ | string | 要上传的文件路径，单请求支持最多5个文件 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
        "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
        "name": "project_document.pdf",
        "size": 62232,
        "resource": {
          "type": "task",
          "id": "fe96108d-b004-4a47-b2f8-6886e758b3a5"
        },
        "uploader": {
          "id": "ou_xxx",
          "type": "user"
        },
        "is_cover": false,
        "uploaded_at": "1675742789470"
      }
    ]
  }
}
```

**说明**：
- 本接口可以支持一次上传多个附件，最多5个
- 每个附件尺寸不超过50MB，格式不限
- 上传结果的顺序将和请求中文件的顺序保持一致

**代码示例**：
```csharp
// 使用租户权限上传附件
public class TaskAttachmentService
{
    private readonly IFeishuTenantV2TaskAttachments _attachmentClient;

    public TaskAttachmentService(IFeishuTenantV2TaskAttachments attachmentClient)
    {
        _attachmentClient = attachmentClient;
    }

    public async Task UploadTaskAttachmentAsync(string taskGuid, string filePath)
    {
        var request = new UploadTaskAttachmentsRequest
        {
            ResourceType = "task",
            ResourceId = taskGuid,
            File = filePath
        };

        var result = await _attachmentClient.UploadAttachmentAsync(request);
        if (result?.Data?.Items != null)
        {
            foreach (var attachment in result.Data.Items)
            {
                Console.WriteLine($"附件上传成功: {attachment.Name}, GUID: {attachment.Guid}");
            }
        }
    }
}
```

---

### 列取附件列表

**函数名称**：列取附件列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<AttachmentResultInfo>?> GetAttachmentPageListAsync(
    [Query("resource_id")] string? resource_id = null,
    [Query("resource_type")] string? resource_type = "task",
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| resource_id | ⚪ | string | 资源ID（任务GUID），示例：d300a75f-c56a-4be9-80d1-e47653028ceb |
| resource_type | ⚪ | string | 资源类型，默认"task" |
| page_size | ⚪ | int | 分页大小，默认10 |
| page_token | ⚪ | string | 分页标记 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
        "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
        "name": "project_document.pdf",
        "size": 62232,
        "url": "https://example.com/download/authcode/?code=OWMzNDlmMjJmZThkYzZkZGJlMjYwZTI0OTUxZTE2MDJfMDZmZmMwOWVj",
        "resource": {
          "type": "task",
          "id": "fe96108d-b004-4a47-b2f8-6886e758b3a5"
        },
        "uploader": {
          "id": "ou_xxx",
          "type": "user"
        },
        "is_cover": false,
        "uploaded_at": "1675742789470"
      }
    ],
    "page_token": "next_page_token",
    "has_more": true
  }
}
```

**说明**：
- 列取一个资源的所有附件
- 返回的附件列表支持分页，按照附件上传时间排序
- 每个附件会返回一个可供下载的临时url，有效期为3分钟，最多可以支持3次下载
- 如果超过使用限制，需要通过本接口获取新的临时url

**代码示例**：
```csharp
// 列取任务附件列表
public async Task ListTaskAttachmentsAsync(string taskGuid)
{
    var result = await _attachmentClient.GetAttachmentPageListAsync(
        resource_id: taskGuid,
        resource_type: "task",
        page_size: 20
    );

    if (result?.Data?.Items != null)
    {
        foreach (var attachment in result.Data.Items)
        {
            Console.WriteLine($"附件: {attachment.Name}");
            Console.WriteLine($"大小: {attachment.Size} bytes");
            Console.WriteLine($"下载链接: {attachment.Url}");
        }
    }
}
```

---

### 获取附件详情

**函数名称**：获取附件详情

**函数签名**：
```csharp
Task<FeishuApiResult<GetAttachmentsInfoResult>?> GetAttachmentByIdAsync(
    [Path] string attachment_guid,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| attachment_guid | ✅ | string | 附件GUID，示例：b59aa7a3-e98c-4830-8273-cbb29f89b837 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "attachment": {
      "guid": "b59aa7a3-e98c-4830-8273-cbb29f89b837",
      "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
      "name": "design_mockup.png",
      "size": 124580,
      "url": "https://example.com/download/authcode/?code=OWMzNDlmMjJmZThkYzZkZGJlMjYwZTI0OTUxZTE2MDJfMDZmZmMwOWVj",
      "resource": {
        "type": "task",
        "id": "fe96108d-b004-4a47-b2f8-6886e758b3a5"
      },
      "uploader": {
        "id": "ou_xxx",
        "type": "user"
      },
      "is_cover": true,
      "uploaded_at": "1675742789470"
    }
  }
}
```

**说明**：提供一个附件GUID，返回附件的详细信息，包括GUID、名称、大小、上传时间、临时可下载链接等。

**代码示例**：
```csharp
// 获取附件详情
public async Task GetAttachmentDetailAsync(string attachmentGuid)
{
    var result = await _attachmentClient.GetAttachmentByIdAsync(attachmentGuid);
    
    if (result?.Data?.Attachment != null)
    {
        var attachment = result.Data.Attachment;
        Console.WriteLine($"附件名称: {attachment.Name}");
        Console.WriteLine($"文件大小: {attachment.Size} bytes");
        Console.WriteLine($"上传时间: {attachment.UploadedAt}");
        Console.WriteLine($"下载链接: {attachment.Url}");
        Console.WriteLine($"是否封面: {attachment.IsCover}");
    }
}
```

---

### 删除附件

**函数名称**：删除附件

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteAttachmentByIdAsync(
    [Path] string attachment_guid,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| attachment_guid | ✅ | string | 要删除的附件GUID，示例：b59aa7a3-e98c-4830-8273-cbb29f89b837 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：提供一个附件GUID，删除该附件。删除后该附件不可再恢复。

**代码示例**：
```csharp
// 删除附件
public async Task DeleteAttachmentAsync(string attachmentGuid)
{
    var result = await _attachmentClient.DeleteAttachmentByIdAsync(attachmentGuid);
    
    if (result?.Code == 0)
    {
        Console.WriteLine("附件删除成功");
    }
}
```

---

## 完整业务场景示例

```csharp
// 完整的附件管理流程示例
public async Task ManageTaskAttachmentsAsync(string taskGuid)
{
    // 1. 上传附件到任务
    var uploadRequest = new UploadTaskAttachmentsRequest
    {
        ResourceType = "task",
        ResourceId = taskGuid,
        File = "/path/to/document.pdf"
    };

    var uploadResult = await _attachmentClient.UploadAttachmentAsync(uploadRequest);
    if (uploadResult?.Data?.Items == null || uploadResult.Data.Items.Length == 0)
    {
        Console.WriteLine("附件上传失败");
        return;
    }

    var attachmentGuid = uploadResult.Data.Items[0].Guid;
    Console.WriteLine($"附件上传成功: {attachmentGuid}");

    // 2. 列取任务的所有附件
    var listResult = await _attachmentClient.GetAttachmentPageListAsync(
        resource_id: taskGuid,
        resource_type: "task",
        page_size: 50
    );

    if (listResult?.Data?.Items != null)
    {
        Console.WriteLine($"任务共有 {listResult.Data.Items.Count} 个附件");
        foreach (var att in listResult.Data.Items)
        {
            Console.WriteLine($"  - {att.Name} ({att.Size} bytes)");
        }
    }

    // 3. 获取附件详情（获取下载链接）
    var detailResult = await _attachmentClient.GetAttachmentByIdAsync(attachmentGuid);
    if (detailResult?.Data?.Attachment?.Url != null)
    {
        var downloadUrl = detailResult.Data.Attachment.Url;
        Console.WriteLine($"下载链接: {downloadUrl}");
        Console.WriteLine("注意：链接有效期3分钟，最多可下载3次");
    }

    // 4. 删除附件
    await _attachmentClient.DeleteAttachmentByIdAsync(attachmentGuid);
    Console.WriteLine("附件已删除");
}
```
