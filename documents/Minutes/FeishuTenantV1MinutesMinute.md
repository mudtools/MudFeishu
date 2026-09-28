---
title: 飞书妙记接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份获取飞书妙记信息，支持妙记基础信息、音视频下载链接、文字记录、统计数据、AI 产物查询与搜索。
---

# 飞书妙记 - 租户令牌（FeishuTenantV1MinutesMinute）

## 接口名称

**飞书妙记（租户令牌）** -（`IFeishuTenantV1MinutesMinute`）

## 功能描述

提供以租户身份获取飞书妙记信息的能力。飞书妙记（Minutes）SDK 是一组服务端 OpenAPI 的封装，用于获取妙记基础信息、音视频下载链接、文字记录、统计数据、AI 产物与搜索妙记。本接口仅声明支持 tenant_access_token 与 user_access_token 双令牌调用的只读端点；剪辑、导入生成、事件订阅等 user-only 写端点见 `IFeishuUserV1MinutesMinute`。支持获取妙记信息、下载妙记音视频文件、导出妙记文字记录、获取妙记统计数据、获取妙记 AI 产物、搜索妙记等操作。

## 参考文档

- [获取妙记信息 - 飞书开放平台](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/get)

## 函数列表

| 函数名称                 | 功能描述               | 认证方式 | HTTP 方法 |
| ------------------------ | ---------------------- | -------- | --------- |
| GetMinuteAsync           | 获取妙记信息           | 租户令牌 | GET       |
| GetMinuteMediaAsync      | 下载妙记音视频文件     | 租户令牌 | GET       |
| GetMinuteTranscriptAsync | 导出妙记文字记录       | 租户令牌 | GET       |
| GetMinuteStatisticsAsync | 获取妙记统计数据       | 租户令牌 | GET       |
| GetMinuteArtifactsAsync  | 获取妙记 AI 产物       | 租户令牌 | GET       |
| SearchMinutesAsync       | 搜索妙记               | 租户令牌 | POST      |

## 函数详细内容

### 获取妙记信息

获取妙记基础概览，包括所有者、创建时间、标题、封面、时长与链接。限频：5 次/秒。所需权限：minutes:minutes.basic:read / minutes:minutes（至少其一）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetMinuteResult>?> GetMinuteAsync(
    [Path] string minute_token,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                                          |
| -------------- | --------- | ---- | ------------------------------------------------------------------------------------------------------------- |
| `minute_token` | `string`  | ✅   | 妙记唯一标识，取自妙记 URL 链接末段，长度 24 字符，示例值：`obcnq3b9jl72l83w4f14xxxx`                          |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "minute": {
      "token": "obcnq3b9jl72l83w4f14xxxx",
      "owner_id": "ou_30b07b63089ea46518789914dac63d36",
      "create_time": "1627540853",
      "title": "项目周会",
      "cover": "https://example.feishu.cn/cover/xxx",
      "duration": "3600",
      "url": "https://sample.feishu.cn/minutes/obcnq3b9jl72l83w4f14xxxx",
      "note_id": "7112223334445556667"
    }
  }
}
```

**说明**：`minute_token` 取自妙记 URL 链接末段（长度 24 字符）。返回的 `owner_id` 类型由 `user_id_type` 决定。

---

### 下载妙记音视频文件

获取妙记音视频文件的下载链接（有效期 1 天），用于批量下载。限频：5 次/秒。所需权限：minutes:minutes.media:export（下载妙记音视频文件）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetMinuteMediaResult>?> GetMinuteMediaAsync(
    [Path] string minute_token,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型     | 必填 | 说明                                                            |
| -------------- | -------- | ---- | --------------------------------------------------------------- |
| `minute_token` | `string` | ✅   | 妙记唯一标识，取自妙记 URL 链接末段，长度 24 字符，示例值：`obcnq3b9jl72l83w4f14xxxx` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "download_url": "https://example.feishu.cn/minutes/media/download/xxx"
  }
}
```

**说明**：返回的下载链接有效期为 1 天，过期后需重新获取。

---

### 导出妙记文字记录

获取妙记的文字记录（逐字稿），返回文件二进制流。限频：5 次/秒。所需权限：minutes:minutes.transcript:export（导出妙记文字转写）。

**函数签名**：

```csharp
Task<byte[]?> GetMinuteTranscriptAsync(
    [Path] string minute_token,
    [Query("need_speaker")] bool? need_speaker = null,
    [Query("need_timestamp")] bool? need_timestamp = null,
    [Query("file_format")] string? file_format = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名           | 类型      | 必填 | 说明                             |
| ---------------- | --------- | ---- | -------------------------------- |
| `minute_token`   | `string`  | ✅   | 妙记唯一标识，取自妙记 URL 链接末段，长度 24 字符，示例值：`obcnq3b9jl72l83w4f14xxxx` |
| `need_speaker`   | `bool?`   | ⚪   | 是否包含说话人                   |
| `need_timestamp` | `bool?`   | ⚪   | 是否包含时间戳                   |
| `file_format`    | `string?` | ⚪   | 导出文件格式，示例值：txt、srt   |

**响应**：文件二进制数据

**说明**：成功时返回文字记录文件的二进制内容（取自 `HttpContent.ReadAsByteArrayAsync`，不会为 null；空响应体对应空数组）。服务端返回非 2xx 状态码时抛出 `ApiException`；飞书部分业务错误以 HTTP 200 + JSON 错误体（`{"code":...,"msg":...}`）返回，此时本方法会把错误 JSON 当作文件内容返回，落盘前应按 `Content-Type` 自检，详见 `documents/ErrorHandling.md`。

---

### 获取妙记统计数据

获取妙记的访问统计数据，包括 PV、UV、访问用户 ID 与访问时间。限频：5 次/秒。所需权限：minutes:minutes.statistics:read（读取妙记统计信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetMinuteStatisticsResult>?> GetMinuteStatisticsAsync(
    [Path] string minute_token,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                                          |
| -------------- | --------- | ---- | ------------------------------------------------------------------------------------------------------------- |
| `minute_token` | `string`  | ✅   | 妙记唯一标识，取自妙记 URL 链接末段，长度 24 字符，示例值：`obcnq3b9jl72l83w4f14xxxx`                          |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "statistics": {
      "user_view_count": 12,
      "page_view_count": 30,
      "user_view_list": [
        {
          "user_id": "ou_30b07b63089ea46518789914dac63d36",
          "view_time": "1627540853"
        }
      ]
    }
  }
}
```

**说明**：`user_view_count` 为 UV（访问用户数），`page_view_count` 为 PV（访问次数），`user_view_list` 为每位访问用户及其访问时间。

---

### 获取妙记 AI 产物

获取妙记的 AI 产物，包括总结、章节、待办、推荐关键词与逐字稿。限频：5 次/秒。所需权限：minutes:minutes.artifacts:read（获取妙记 AI 产物）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetMinuteArtifactsResult>?> GetMinuteArtifactsAsync(
    [Path] string minute_token,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型     | 必填 | 说明                                                            |
| -------------- | -------- | ---- | --------------------------------------------------------------- |
| `minute_token` | `string` | ✅   | 妙记唯一标识，取自妙记 URL 链接末段，长度 24 字符，示例值：`obcnq3b9jl72l83w4f149w9c` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "summary": "本次会议确认了迭代范围与排期",
    "minute_chapters": [],
    "minute_todos": [],
    "keywords": ["迭代", "排期"],
    "transcript": "逐字稿内容"
  }
}
```

**说明**：AI 产物由妙记后台生成，妙记刚生成时可能尚未产出，需稍后重试获取。

---

### 搜索妙记

按关键词、所有者、参与者与创建时间等多条件搜索妙记列表，支持分页。限频：5 次/秒。所需权限：minutes:minutes.search:read（搜索妙记）。搜索时间范围最大为 1 个月。

**函数签名**：

```csharp
Task<FeishuApiResult<SearchMinutesResult>?> SearchMinutesAsync(
    [Body] SearchMinutesRequest request,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型                   | 必填 | 说明                                                                                                                              |
| ------------ | ---------------------- | ---- | --------------------------------------------------------------------------------------------------------------------------------- |
| `request`    | `SearchMinutesRequest` | ✅   | 搜索请求体（query 关键词 10～50 字符；filter 过滤条件（owner_ids/participant_ids/create_time）；sorter 排序方式；至少提供一个过滤条件） |
| `page_size`  | `int?`                 | ⚪   | 分页大小，范围 1～30，默认 15                                                                                                     |
| `page_token` | `string?`              | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                                                            |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "token": "obcnq3b9jl72l83w4f14xxxx",
        "display_info": {
          "title": "项目周会"
        },
        "meta_data": {
          "create_time": "1627540853",
          "duration": "3600"
        }
      }
    ],
    "total": 100,
    "has_more": true,
    "page_token": "next_page_token",
    "notice": ""
  }
}
```

**说明**：`notice` 为搜索结果提示信息（如结果过多时的降级提示）。搜索时间范围最大为 1 个月；当 `has_more` 为 true 时，可使用返回的 `page_token` 继续拉取下一页数据。
