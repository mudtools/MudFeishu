# 飞书妙搭文件存储 - 用户令牌（FeishuUserV1SparkAppStorage）

## 接口名称

**飞书妙搭文件存储（用户令牌）** -（`IFeishuUserV1SparkAppStorage`）

## 功能描述

提供以用户身份管理飞书妙搭应用文件资源的能力。飞书妙搭（Spark）文件存储 SDK 是一组服务端 OpenAPI 的封装，用于上传、下载及分片上传妙搭应用下的文件资源。支持上传文件、下载文件、分片上传（创建上传请求、上传分片、完成上传）等操作。

## 参考文档

- [飞书妙搭概述 - 飞书开放平台](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/overview)

## 函数列表

| 函数名称                        | 功能描述                       | 认证方式 | HTTP 方法 |
| ------------------------------- | ------------------------------ | -------- | --------- |
| UploadStorageAsync              | 上传文件                       | 用户令牌 | POST      |
| DownloadStorageAsync            | 下载文件                       | 用户令牌 | GET       |
| UploadStorageInitializeAsync    | 分片上传文件 - 创建上传请求    | 用户令牌 | POST      |
| UploadStoragePartAsync          | 分片上传文件 - 上传分片        | 用户令牌 | POST      |
| UploadStorageCompleteAsync      | 分片上传文件 - 完成上传        | 用户令牌 | POST      |

## 函数详细内容

### 上传文件

用于上传 20MB（含）以内的文件。接口频率限制 5 次/秒。

**函数签名**：

```csharp
Task<FeishuApiResult<UploadStorageResult>?> UploadStorageAsync(
    [Path] string app_id,
    [FormContent] UploadStorageFileRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名     | 类型                        | 必填 | 说明                                                                 |
| ---------- | --------------------------- | ---- | -------------------------------------------------------------------- |
| `app_id`   | `string`                    | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取，如 `https://miaoda.feishu.cn/app/app_4jcn5n11bpf5v` 中的 `app_4jcn5n11bpf5v` 即为 app_id |
| `request`  | `UploadStorageFileRequest`  | ✅   | 上传文件请求体（multipart：`file_name`、可选 `check_sum`、`file` 文件本地路径） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "file_key": "1859988692091946",
    "file_url": "/storage/xxx/xxx.png",
    "file_name": "示例文件.png",
    "file_size": 102400,
    "mime_type": "image/png"
  }
}
```

**说明**：返回上传后的文件信息（`file_key`、相对路径 `file_url` 等）；单次仅支持 20MB（含）以内的文件，更大的文件请使用分片上传流程，接口频率限制 5 次/秒。

---

### 下载文件

用于下载 20MB（含）以内的文件。支持通过请求头 Range 分片下载。

**函数签名**：

```csharp
Task<byte[]?> DownloadStorageAsync(
    [Path] string app_id,
    [Query("file_key")] string? file_key = null,
    [Query("file_url")] string? file_url = null,
    [Header("Range")] string? range = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名      | 类型      | 必填 | 说明                                                                 |
| ----------- | --------- | ---- | -------------------------------------------------------------------- |
| `app_id`    | `string`  | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取                                 |
| `file_key`  | `string?` | ⚪   | 文件 ID，ID 和 URL 不能同时为空；都提供时优先使用 `file_key`，示例值：`1859988692091946` |
| `file_url`  | `string?` | ⚪   | 文件 URL（相对路径），ID 和 URL 不能同时为空                         |
| `range`     | `string?` | ⚪   | 可选请求头（`Range`），在 HTTP 请求头中指定下载文件的部分内容，单位为字节，格式为 `Range: bytes=start-end`，示例值：`bytes=0-1024` |

**响应**：文件二进制数据

**说明**：成功时返回响应的二进制内容（取自 `HttpContent.ReadAsByteArrayAsync`，不会为 `null`；空响应体对应空数组）。`file_key` 与 `file_url` 不能同时为空，同时提供时以 `file_key` 为准。服务端返回非 2xx 状态码时由 HTTP 执行器统一抛出 `ApiException`；飞书部分业务错误以 HTTP 200 + JSON 错误体（`{"code":...,"msg":...}`）返回，此时本方法会把错误 JSON 当作文件内容返回，落盘前应按 `Content-Type` 自检，详见 `documents/ErrorHandling.md`。

---

### 分片上传文件 - 创建上传请求

发送初始化请求，以获取上传请求 ID 和分片策略，为上传分片做准备。

**函数签名**：

```csharp
Task<FeishuApiResult<UploadStorageInitializeResult>?> UploadStorageInitializeAsync(
    [Path] string app_id,
    [Body] UploadStorageInitializeRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名     | 类型                              | 必填 | 说明                                                   |
| ---------- | --------------------------------- | ---- | ------------------------------------------------------ |
| `app_id`   | `string`                          | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取                   |
| `request`  | `UploadStorageInitializeRequest`  | ✅   | 创建上传请求体（`file_name`、`file_size`；可选 `mime_type`） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "upload_id": "upload_xxx",
    "chunk_size": 4194304,
    "chunk_numbers": 10
  }
}
```

**说明**：返回上传请求 ID（有效期 24h）与建议分片策略（`chunk_size` 分片大小、`chunk_numbers` 分片数量），后续上传分片与完成上传都依赖该 `upload_id`。

---

### 分片上传文件 - 上传分片

根据创建上传请求返回的上传请求 ID 和分片策略上传对应的文件分片。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UploadStoragePartAsync(
    [Path] string app_id,
    [FormContent] UploadStoragePartRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名     | 类型                         | 必填 | 说明                                                                 |
| ---------- | ---------------------------- | ---- | -------------------------------------------------------------------- |
| `app_id`   | `string`                     | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取                                 |
| `request`  | `UploadStoragePartRequest`   | ✅   | 上传分片请求体（multipart：`upload_id`、`chunk_index`、分片文件本地路径、可选 `chunk_check_sum`） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：上传成功时 `data` 为空对象；需按服务端返回的分片大小与分片序号逐个上传，全部分片上传完成后调用完成上传接口。

---

### 分片上传文件 - 完成上传

调用「上传分片」将分片全部上传完毕后，调用本接口触发完成上传。

**函数签名**：

```csharp
Task<FeishuApiResult<UploadStorageResult>?> UploadStorageCompleteAsync(
    [Path] string app_id,
    [Body] UploadStorageCompleteRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名     | 类型                            | 必填 | 说明                                   |
| ---------- | ------------------------------- | ---- | -------------------------------------- |
| `app_id`   | `string`                        | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取   |
| `request`  | `UploadStorageCompleteRequest`  | ✅   | 完成上传请求体（`upload_id`）          |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "file_key": "1859988692091946",
    "file_url": "/storage/xxx/xxx.png",
    "file_name": "示例文件.png",
    "file_size": 41943040,
    "mime_type": "image/png"
  }
}
```

**说明**：返回上传完成后的文件信息；上传请求 ID 有效期为 24 小时，过期后需重新执行「创建上传请求」。
