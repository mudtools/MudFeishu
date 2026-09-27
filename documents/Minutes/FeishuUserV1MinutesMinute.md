# 飞书妙记 - 用户令牌（FeishuUserV1MinutesMinute）

## 接口名称

**飞书妙记（用户令牌）** -（`IFeishuUserV1MinutesMinute`）

## 功能描述

提供以用户身份使用飞书妙记的能力。飞书妙记（Minutes）SDK 是一组服务端 OpenAPI 的封装，用于以用户身份创建妙记剪辑、导入云盘音视频生成妙记、订阅/取消订阅妙记变更事件，并继承双令牌只读端点（基础信息、音视频下载、文字记录、统计数据、AI 产物、搜索）。支持获取妙记信息、下载妙记音视频文件、导出妙记文字记录、获取妙记统计数据、获取妙记 AI 产物、搜索妙记、创建妙记剪辑、导入云盘文件生成妙记、订阅妙记变更事件、取消订阅妙记变更事件等操作。

## 参考文档

- [创建妙记剪辑 - 飞书开放平台](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/clip)

## 函数列表

| 函数名称                 | 功能描述               | 认证方式 | HTTP 方法 |
| ------------------------ | ---------------------- | -------- | --------- |
| GetMinuteAsync           | 获取妙记信息           | 用户令牌 | GET       |
| GetMinuteMediaAsync      | 下载妙记音视频文件     | 用户令牌 | GET       |
| GetMinuteTranscriptAsync | 导出妙记文字记录       | 用户令牌 | GET       |
| GetMinuteStatisticsAsync | 获取妙记统计数据       | 用户令牌 | GET       |
| GetMinuteArtifactsAsync  | 获取妙记 AI 产物       | 用户令牌 | GET       |
| SearchMinutesAsync       | 搜索妙记               | 用户令牌 | POST      |
| ClipMinuteAsync          | 创建妙记剪辑           | 用户令牌 | POST      |
| UploadMinuteAsync        | 导入云盘文件生成妙记   | 用户令牌 | POST      |
| SubscribeMinuteAsync     | 订阅妙记变更事件       | 用户令牌 | POST      |
| UnsubscribeMinuteAsync   | 取消订阅妙记变更事件   | 用户令牌 | POST      |

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

**认证**：用户令牌

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

**说明**：以用户令牌调用时，仅能获取当前用户有权限访问的妙记。返回的 `owner_id` 类型由 `user_id_type` 决定。

---

### 下载妙记音视频文件

获取妙记音视频文件的下载链接（有效期 1 天），用于批量下载。限频：5 次/秒。所需权限：minutes:minutes.media:export（下载妙记音视频文件）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetMinuteMediaResult>?> GetMinuteMediaAsync(
    [Path] string minute_token,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

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

**认证**：用户令牌

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

**认证**：用户令牌

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

**认证**：用户令牌

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

**认证**：用户令牌

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

**说明**：以用户令牌调用时，搜索范围仅限当前用户可见的妙记。搜索时间范围最大为 1 个月；当 `has_more` 为 true 时，可使用返回的 `page_token` 继续拉取下一页数据。

---

### 创建妙记剪辑

基于已完成的妙记与指定时间段创建妙记剪辑，响应成功表示已提交创建，转写与音视频文件在后台异步生成。限频：5 次/秒。所需权限：minutes:minutes.clip:write（创建妙记剪辑）。每个时间段须大于 1000 毫秒，重叠或相邻区间会自动合并。

**函数签名**：

```csharp
Task<FeishuApiResult<MinuteUrlResult>?> ClipMinuteAsync(
    [Path] string minute_token,
    [Body] ClipMinuteRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名         | 类型                | 必填 | 说明                                                            |
| -------------- | ------------------- | ---- | --------------------------------------------------------------- |
| `minute_token` | `string`            | ✅   | 妙记唯一标识，示例值：`mt_123456789abcdef0123456789abcdef0`     |
| `request`      | `ClipMinuteRequest` | ✅   | 剪辑请求体（time_ranges 必填，1～50 个时间段；可选 title 剪辑标题） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "minute_url": "https://sample.feishu.cn/minutes/obcnq3b9jl72l83w4f14xxxx"
  }
}
```

**说明**：响应成功仅表示剪辑已提交，转写与音视频文件在后台异步生成，需稍后访问返回的妙记链接。本端点仅支持 user_access_token 调用。

---

### 导入云盘文件生成妙记

基于云盘音视频文件生成妙记。支持音频 wav/mp3/m4a/aac/ogg/wma/amr，视频 avi/wmv/mov/mp4/m4v/mpeg/ogg/flv，音频时长不超过 6 小时，文件最大 6GB。限频：5 次/秒。所需权限：minutes:minutes.upload:write（从音视频文件生成妙记）。用户身份调用时用户须具备云盘文件下载权限。

**函数签名**：

```csharp
Task<FeishuApiResult<MinuteUrlResult>?> UploadMinuteAsync(
    [Body] UploadMinuteRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名    | 类型                  | 必填 | 说明                                                 |
| --------- | --------------------- | ---- | ---------------------------------------------------- |
| `request` | `UploadMinuteRequest` | ✅   | 导入请求体（file_token 必填：音视频云盘文件 token）   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "minute_url": "https://sample.feishu.cn/minutes/obcnq3b9jl72l83w4f14xxxx"
  }
}
```

**说明**：妙记为后台异步生成，响应返回的链接在生成完成后才可访问。本端点仅支持 user_access_token 调用，且当前用户需具备该云盘文件的下载权限。

---

### 订阅妙记变更事件

订阅当前用户身份相关的妙记变更事件，通过指定事件类型订阅不同的变更。限频：1000 次/分钟且 50 次/秒。所需权限：minutes:minutes.basic:read（读取妙记基本信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> SubscribeMinuteAsync(
    [Body] MinuteSubscriptionRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名    | 类型                          | 必填 | 说明                                                                |
| --------- | ----------------------------- | ---- | ------------------------------------------------------------------- |
| `request` | `MinuteSubscriptionRequest`   | ✅   | 订阅请求体（event_type 可选：minutes.minute.generated_v1 表示妙记生成事件） |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：订阅范围为当前用户身份相关的妙记变更，重复订阅同一事件类型不会产生副作用。本端点仅支持 user_access_token 调用。

---

### 取消订阅妙记变更事件

取消订阅当前用户身份相关的妙记变更事件，通过指定事件类型取消对应的订阅。限频：1000 次/分钟且 50 次/秒。所需权限：minutes:minutes.basic:read（读取妙记基本信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UnsubscribeMinuteAsync(
    [Body] MinuteSubscriptionRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名    | 类型                        | 必填 | 说明                                                                |
| --------- | --------------------------- | ---- | ------------------------------------------------------------------- |
| `request` | `MinuteSubscriptionRequest` | ✅   | 取消订阅请求体（event_type 可选：minutes.minute.generated_v1 表示妙记生成事件） |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：取消订阅后，对应事件类型的妙记变更将不再推送。本端点仅支持 user_access_token 调用。
