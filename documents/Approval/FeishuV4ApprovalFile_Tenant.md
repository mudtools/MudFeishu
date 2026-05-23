# 审批文件管理接口 -（FeishuV4ApprovalFile_Tenant）

## 接口名称

审批文件管理接口 -（`IFeishuTenantV2ApprovalFile`）

## 功能描述

该接口用于上传审批表单控件内的文件。原生审批文件提供的 API 主要用于上传审批表单控件内的文件。当审批表单中有图片或者附件控件时，开发者需要在调用创建审批实例前，将传入图片或附件控件的文件上传到审批系统，系统会返回文件的 code，该 code 用于创建审批实例时为图片或附件控件赋值。

## 参考文档

- [飞书官方文档 - 审批文件](https://open.feishu.cn/document/server-docs/approval-v4/file/overview)

## 函数列表

| 函数名称          | 功能描述     | 认证方式 | HTTP 方法 |
| ----------------- | ------------ | -------- | --------- |
| `UploadFileAsync` | 上传审批文件 | 租户令牌 | POST      |

## 函数详细内容

### 上传审批文件

#### 函数签名

```csharp
Task<FeishuApiResult<FileUploadResult>?> UploadFileAsync(
    [FormContent] UploadApprovalRequest uploadFileRequest,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名              | 必填 | 类型                    | 描述             |
| ------------------- | ---- | ----------------------- | ---------------- |
| `uploadFileRequest` | ✅   | `UploadApprovalRequest` | 文件上传请求体   |
| `cancellationToken` | ⚪   | `CancellationToken`     | 取消操作令牌对象 |

**请求体示例：**

```json
{
  "file": "[二进制文件数据]",
  "file_name": "请假证明.pdf",
  "file_type": "attachment"
}
```

**参数说明：**

| 字段名      | 必填 | 类型                    | 描述                              |
| ----------- | ---- | ----------------------- | --------------------------------- |
| `file`      | ✅   | `FileStream` / `byte[]` | 文件二进制数据                    |
| `file_name` | ✅   | `string`                | 文件名，包含扩展名                |
| `file_type` | ✅   | `string`                | 文件类型：`image` 或 `attachment` |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "file_token": "file_v2_xxx",
    "file_name": "请假证明.pdf",
    "file_type": "attachment",
    "file_size": 102456,
    "url": "https://internal-api-drive-stream.feishu.cn/xxx"
  }
}
```

**响应字段说明：**

| 字段名       | 类型     | 描述                                              |
| ------------ | -------- | ------------------------------------------------- |
| `file_token` | `string` | 文件 code，用于创建审批实例时为图片或附件控件赋值 |
| `file_name`  | `string` | 文件名                                            |
| `file_type`  | `string` | 文件类型                                          |
| `file_size`  | `long`   | 文件大小（字节）                                  |
| `url`        | `string` | 文件访问链接（有效期有限）                        |

#### 说明

- 当审批表单中有图片或者附件控件时，需要先将文件通过本接口上传到审批系统
- 接口会返回文件的 `file_token`，该 token 用于创建审批实例时为图片或附件控件赋值
- 支持的文件类型：
  - `image`：图片文件（JPG、PNG、GIF 等）
  - `attachment`：附件文件（PDF、DOC、XLS 等）
- 文件大小限制请参考飞书官方文档

#### 代码示例

```csharp
// 使用租户令牌上传审批文件
public class ApprovalFileService
{
    private readonly IFeishuTenantV2ApprovalFile _fileClient;

    public ApprovalFileService(IFeishuTenantV2ApprovalFile fileClient)
    {
        _fileClient = fileClient;
    }

    public async Task UploadAttachmentAsync()
    {
        using var fileStream = File.OpenRead("C:\\Documents\\请假证明.pdf");
        var request = new UploadApprovalRequest
        {
            File = fileStream,
            FileName = "请假证明.pdf",
            FileType = "attachment"
        };

        var result = await _fileClient.UploadFileAsync(request);

        if (result?.Code == 0)
        {
            var fileToken = result.Data?.FileToken;
            Console.WriteLine($"文件上传成功，Token: {fileToken}");
        }
    }
}
```

**上传图片文件示例：**

```csharp
// 使用租户令牌上传审批图片
public class ApprovalFileService
{
    private readonly IFeishuTenantV2ApprovalFile _fileClient;

    public ApprovalFileService(IFeishuTenantV2ApprovalFile fileClient)
    {
        _fileClient = fileClient;
    }

    public async Task UploadImageAsync()
    {
        using var imageStream = File.OpenRead("C:\\Documents\\请假截图.png");
        var request = new UploadApprovalRequest
        {
            File = imageStream,
            FileName = "请假截图.png",
            FileType = "image"
        };

        var result = await _fileClient.UploadFileAsync(request);

        if (result?.Code == 0)
        {
            var imageToken = result.Data?.FileToken;
            Console.WriteLine($"图片上传成功，Token: {imageToken}");
        }
    }
}
```

**完整业务场景示例：**

```csharp
// 使用租户令牌完成文件上传并创建审批实例的完整流程
public class ApprovalWithFileService
{
    private readonly IFeishuTenantV2ApprovalFile _fileClient;
    private readonly IFeishuTenantV4Approval _approvalClient;

    public ApprovalWithFileService(
        IFeishuTenantV2ApprovalFile fileClient,
        IFeishuTenantV4Approval approvalClient)
    {
        _fileClient = fileClient;
        _approvalClient = approvalClient;
    }

    public async Task<string> CreateApprovalWithAttachmentAsync(
        string attachmentPath,
        string userOpenId)
    {
        string fileToken;
        using (var fileStream = File.OpenRead(attachmentPath))
        {
            var fileName = Path.GetFileName(attachmentPath);
            var fileType = IsImageFile(fileName) ? "image" : "attachment";

            var uploadRequest = new UploadApprovalRequest
            {
                File = fileStream,
                FileName = fileName,
                FileType = fileType
            };

            var uploadResult = await _fileClient.UploadFileAsync(uploadRequest);

            if (uploadResult?.Code != 0)
            {
                throw new Exception($"文件上传失败: {uploadResult?.Msg}");
            }

            fileToken = uploadResult.Data.FileToken;
        }

        var instanceRequest = new CreateInstanceRequest
        {
            ApprovalCode = "7C468A54-8745-2245-9675-08B7C63E7A85",
            OpenId = userOpenId,
            Form = new List<FormData>
            {
                new FormData { Id = "reason", Type = "input", Value = "病假" },
                new FormData { Id = "attachment", Type = "attachment", Value = fileToken }
            }
        };

        var instanceResult = await _approvalClient.CreateInstanceAsync(instanceRequest);

        if (instanceResult?.Code != 0)
        {
            throw new Exception($"创建审批实例失败: {instanceResult?.Msg}");
        }

        return instanceResult.Data.InstanceCode;
    }

    private bool IsImageFile(string fileName)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var ext = Path.GetExtension(fileName).ToLower();
        return imageExtensions.Contains(ext);
    }
}
```
