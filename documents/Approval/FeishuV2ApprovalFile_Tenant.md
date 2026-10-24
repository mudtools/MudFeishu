---
title: 审批文件管理接口（租户令牌）| MudFeishu
description: 该接口用于上传飞书原生审批表单控件内的文件。当审批表单包含图片或附件控件时，开发者需在创建审批实例前先将文件上传到审批系统，获取返回的 file code 用于为控件赋值。
---

# 审批文件管理接口 -（IFeishuTenantV2ApprovalFile）

## 接口名称

审批文件管理接口 -（`IFeishuTenantV2ApprovalFile`）

## 功能描述

该接口用于上传审批表单控件内的文件。当审批表单中有图片或者附件控件时，开发者需要在调用创建审批实例前，将传入图片或附件控件的文件上传到审批系统，系统会返回文件的 code，该 code 用于创建审批实例时为图片或附件控件赋值。

## 参考文档

- [飞书官方文档 - 审批文件](https://open.feishu.cn/document/server-docs/approval-v4/file/overview)
- [飞书官方文档 - 上传文件](https://open.feishu.cn/document/server-docs/approval-v4/file/upload-files)

## 函数列表

| 函数名称          | 功能描述     | 认证方式 | HTTP 方法 | 接口文档 |
| ----------------- | ------------ | -------- | --------- |----------|
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

**请求体（multipart/form-data）：**

| 字段名    | 必填 | 类型     | 描述                                                       |
| --------- | ---- | -------- | ---------------------------------------------------------- |
| `name`    | ✅   | `string` | 文件名，需包含文件扩展名，例如 `文件.doc`                  |
| `type`    | ✅   | `string` | 文件类型：`image`（图片）或 `attachment`（附件）           |
| `content` | ✅   | 文件     | 文件二进制内容；SDK 中通过 `FilePath` 指定本地文件绝对路径 |

**请求示例：**

```csharp
var request = new UploadApprovalRequest
{
    Name = "请假证明.pdf",
    Type = "attachment",
    FilePath = @"C:\Documents\请假证明.pdf"
};

var result = await _fileClient.UploadFileAsync(request);
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "code": "D93653C3-2609-4EE0-8041-61DC1D84F0B5",
    "url": "https://example.com/lark-approval-attachment/image/20210819/a8c1a1f1-47ae-4147-9deb-a8bf2c1234.jpg~image.image?x-expires=1634941234&x-signature=1234Tfv50ryUesNwKTUTnBlJivY%3D#.jpg"
  }
}
```

**响应字段说明：**

| 字段名 | 类型     | 描述                                                       |
| ------ | -------- | ---------------------------------------------------------- |
| `code` | `string` | 文件标识码，用于创建审批实例时传入图片或附件控件的 `value` |
| `url`  | `string` | 文件访问链接（有效期有限）                                 |

#### 说明

- 每次请求只能上传一个文件；如需上传多个文件，请逐个调用。
- 接口返回的 `code` 需要写入创建审批实例时图片或附件控件的 `value`（图片控件使用数组，例如 `["D93653C3-..."]`）。
- 附件上传大小上限为 50 MB，图片上传大小上限为 10 MB。
- 支持的文件类型：
  - `image`：图片文件（JPG、PNG、GIF 等）
  - `attachment`：附件文件（PDF、DOC、XLS 等）

#### 代码示例

```csharp
public class ApprovalFileService
{
    private readonly IFeishuTenantV2ApprovalFile _fileClient;

    public ApprovalFileService(IFeishuTenantV2ApprovalFile fileClient)
    {
        _fileClient = fileClient;
    }

    public async Task<string?> UploadAttachmentAsync(string filePath)
    {
        var request = new UploadApprovalRequest
        {
            Name = Path.GetFileName(filePath),
            Type = "attachment",
            FilePath = filePath
        };

        var result = await _fileClient.UploadFileAsync(request);
        return result?.Data?.Code;
    }
}
```
