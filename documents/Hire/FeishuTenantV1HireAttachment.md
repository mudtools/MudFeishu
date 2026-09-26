# 招聘附件 - 租户令牌（FeishuTenantV1HireAttachment）

## 接口名称

**招聘附件（租户令牌）** -（`IFeishuTenantV1HireAttachment`）

## 功能描述

提供以租户身份管理飞书招聘附件的能力。飞书招聘（Hire）附件入口域 SDK 是一组服务端 OpenAPI 的封装，用于招聘系统附件文件的上传（创建通用附件）、附件元信息（文件名、创建时间、下载地址）查询，以及人才简历附件的 PDF 格式下载链接获取。本接口全部端点仅支持 tenant_access_token 调用。支持创建附件、获取附件信息、获取人才简历附件 PDF 格式下载链接等操作。

## 参考文档

- [创建附件 - 飞书开放平台](https://open.feishu.cn/document/hire-v1/attachment/create)

## 函数列表

| 函数名称                | 功能描述                             | 认证方式 | HTTP 方法 |
| ----------------------- | ------------------------------------ | -------- | --------- |
| CreateAttachmentAsync   | 创建附件                             | 租户令牌 | POST      |
| GetAttachmentAsync      | 获取附件信息                         | 租户令牌 | GET       |
| PreviewAttachmentAsync  | 获取人才简历附件 PDF 格式下载链接    | 租户令牌 | GET       |

## 函数详细内容

### 创建附件

在招聘系统中上传附件文件，上传的附件为通用附件；文件大小不得超过 300 MB。请求体为 multipart/form-data，仅含一个文件字段 content。限频：1000 次/分钟、50 次/秒。所需权限：hire:attachment（上传招聘相关附件）。支持的应用类型：自建应用、商店应用。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateAttachmentResult>?> CreateAttachmentAsync(
    [FormContent] CreateAttachmentRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                      | 必填 | 说明                                                              |
| --------- | ------------------------- | ---- | ----------------------------------------------------------------- |
| `request` | `CreateAttachmentRequest` | ✅   | 上传请求体（content 为待上传文件的本地绝对路径，≤ 300 MB）        |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "id": "",
    "name": "",
    "url": ""
  }
}
```

**说明**：请求体以 multipart/form-data 提交，仅含一个文件字段 `content`（完整上传）；返回的 `url` 有效期 30 分钟，单个文件不得超过 300 MB。

---

### 获取附件信息

根据附件 ID 和附件类型获取招聘系统中附件的元信息，比如附件名称、附件创建时间、附件下载地址等。附件 ID 可通过「获取人才信息 V1」（简历/作品附件）或「创建附件」「获取 Offer 详情」「获取 Offer 信息」（通用附件）接口获取。限频：20 次/秒。所需权限：hire:attachment:readonly（获取附件信息）或 hire:attachment（上传招聘相关附件）。支持的应用类型：自建应用、商店应用。

**函数签名**：

```csharp
Task<FeishuApiResult<GetAttachmentResult>?> GetAttachmentAsync(
    [Path] string attachment_id,
    [Query("type")] int? type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名          | 类型      | 必填 | 说明                                                    |
| --------------- | --------- | ---- | ------------------------------------------------------- |
| `attachment_id` | `string`  | ✅   | 附件 ID，示例值：`6960663240925956555`                 |
| `type`          | `int?`    | ⚪   | 附件类型（1 简历附件 / 2 作品附件 / 3 通用附件），默认 1 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "attachment": {}
  }
}
```

**说明**：`data.attachment` 为 `HireAttachmentInfo`，含 id、url、name、mime、create_time；附件类型不传时按 1（简历附件）处理。

---

### 获取人才简历附件 PDF 格式下载链接

根据人才简历附件 ID 获取该简历附件对应的 PDF 文件的下载地址。仅支持转换人才简历类型附件，不支持作品附件和通用附件；.doc/.docx/.ppt/.pptx/.txt 可转换为 PDF，转换失败时返回附件原文件下载地址。限频：20 次/秒。所需权限：hire:attachment（上传招聘相关附件）。支持的应用类型：自建应用。

**函数签名**：

```csharp
Task<FeishuApiResult<PreviewAttachmentResult>?> PreviewAttachmentAsync(
    [Path] string attachment_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名          | 类型     | 必填 | 说明                                                                        |
| --------------- | -------- | ---- | --------------------------------------------------------------------------- |
| `attachment_id` | `string` | ✅   | 人才简历附件 ID，可通过「获取人才信息」接口返回数据获取，示例值：`64352523512563462` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "url": ""
  }
}
```

**说明**：返回的 `url` 为 PDF 文件下载链接，有效期 30 分钟；仅人才简历类型附件可转换，转换失败时返回原文件下载地址。
