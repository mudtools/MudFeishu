---
title: 词典文件接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份（应用身份）管理飞书词典图片文件，支持词条图片的上传与下载。
---

# 词典文件 - 租户令牌（FeishuTenantV1LingoFile）

## 接口名称

**词典文件（租户令牌）** -（`IFeishuTenantV1LingoFile`）

## 功能描述

提供以应用身份管理飞书词典图片文件的能力。飞书词典（Lingo）文件入口域租户态 SDK 是一组服务端 OpenAPI 的封装，用于以应用身份上传词条图片与下载词条图片。本接口全部端点支持 tenant_access_token 调用。支持上传词条图片、下载词条图片等操作。

## 参考文档

- [飞书词典概述 - 飞书开放平台](https://open.feishu.cn/document/lingo-v1/overview)

## 函数列表

| 函数名称          | 功能描述     | 认证方式 | HTTP 方法 | 接口文档 |
| ----------------- | ------------ | -------- | --------- |----------|
| UploadFileAsync   | 上传词条图片 | 租户令牌 | POST      | [UploadFileAsync](https://open.feishu.cn/document/lingo-v1/file/upload) |
| DownloadFileAsync | 下载词条图片 | 租户令牌 | GET       | [DownloadFileAsync](https://open.feishu.cn/document/server-docs/docs/drive-v1/download/download) |

## 函数详细内容

### 上传词条图片

上传词条关联的图片文件（multipart/form-data），返回 file_token，可用于关联至词条 images[].token 或下载。仅支持 icon、bmp、gif、png、jpeg、webp 六种格式，高宽像素在 320 - 4096 像素之间，大小在 3KB - 10MB。限频：100 次/分钟。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）。支持的应用类型：自建应用。

**函数签名**：

```csharp
Task<FeishuApiResult<UploadFileResult>?> UploadFileAsync(
    [FormContent] UploadFileRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                | 必填 | 说明                                                                     |
| --------- | ------------------- | ---- | ------------------------------------------------------------------------ |
| `request` | `UploadFileRequest` | ✅   | 上传请求体（name 文件名称必填 1 ～ 100 字符；file 为图片文件本地绝对路径） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "file_token": "boxbcEcmKiD****vgqWTpvdc7jc"
  }
}
```

**说明**：请求体以 multipart/form-data 提交，其中 `FilePath` 对应文件字段 `file`，必须为本地绝对路径，上传前会检查文件是否存在，不存在则抛出异常。返回的 `file_token` 可用于词条 images[].token 关联或后续下载。

---

### 下载词条图片

根据文件 token 下载词条关联的图片，响应体为图片二进制流。限频：100 次/分钟。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。支持的应用类型：自建应用。

**函数签名**：

```csharp
Task<byte[]?> DownloadFileAsync(
    [Path] string file_token,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型     | 必填 | 说明                                              |
| ------------ | -------- | ---- | ------------------------------------------------- |
| `file_token` | `string` | ✅   | 文件 token，示例值：`boxbcEcmKiD****vgqWTpvdc7jc` |

**响应**：文件二进制数据

**说明**：成功时返回图片二进制内容。HTTP 200 + JSON 错误体场景（如 99991400 系统繁忙、99991672 无权限等业务错误）仍可能以 200 返回，此时反序列化将失败并抛出 `ApiException`，调用方需针对该残余风险做异常兜底（参见 `documents/ErrorHandling.md`）。
