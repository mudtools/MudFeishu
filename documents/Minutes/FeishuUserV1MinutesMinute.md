# 飞书妙记管理 - 用户令牌（FeishuUserV1MinutesMinute）

## 接口名称

**飞书妙记管理（用户令牌）** -（`IFeishuUserV1MinutesMinute`）

## 功能描述

以用户身份使用飞书妙记（Minutes）的全部能力：获取基础信息、下载音视频文件、导出文字记录、获取统计数据、获取 AI 产物、搜索妙记，以及创建剪辑、导入云盘音视频生成妙记、订阅/取消订阅妙记变更事件。只读端点继承自双令牌基接口 `IFeishuV1MinutesMinute`，租户身份调用见 `IFeishuTenantV1MinutesMinute`。

## 参考文档

- [获取妙记信息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/get)
- [下载妙记音视频文件](https://open.feishu.cn/document/minutes-v1/minute-media/get)
- [导出妙记文字记录](https://open.feishu.cn/document/minutes-v1/minute-transcript/get)
- [获取妙记统计数据](https://open.feishu.cn/document/server-docs/minutes-v1/minute-statistics/get)
- [获取妙记 AI 产物](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/artifacts)
- [搜索妙记](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/search)
- [创建妙记剪辑](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/clip)
- [导入云盘文件生成妙记](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/upload)
- [订阅妙记变更事件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/subscription)
- [取消订阅妙记变更事件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/unsubscription)

## 函数列表

| 函数名称               | 功能描述                       | 认证方式 | HTTP 方法 |
| ---------------------- | ------------------------------ | -------- | --------- |
| GetMinuteAsync         | 获取妙记信息                   | 用户令牌 | GET       |
| GetMinuteMediaAsync    | 下载妙记音视频文件（获取链接） | 用户令牌 | GET       |
| GetMinuteTranscriptAsync | 导出妙记文字记录（二进制流）  | 用户令牌 | GET       |
| GetMinuteStatisticsAsync | 获取妙记统计数据             | 用户令牌 | GET       |
| GetMinuteArtifactsAsync | 获取妙记 AI 产物              | 用户令牌 | GET       |
| SearchMinutesAsync     | 搜索妙记                       | 用户令牌 | POST      |
| ClipMinuteAsync        | 创建妙记剪辑                   | 用户令牌 | POST      |
| UploadMinuteAsync      | 导入云盘文件生成妙记           | 用户令牌 | POST      |
| SubscribeMinuteAsync   | 订阅妙记变更事件               | 用户令牌 | POST      |
| UnsubscribeMinuteAsync | 取消订阅妙记变更事件           | 用户令牌 | POST      |

> 前 6 个只读端点继承自 `IFeishuV1MinutesMinute`（双令牌基接口）。

## 函数详细内容

### 获取妙记信息

获取妙记基础概览，包括所有者、创建时间、标题、封面、时长与链接。限频：5 次/秒。

**函数签名**：

```csharp
Task<FeishuApiResult<GetMinuteResult>?> GetMinuteAsync(
    [Path] string minute_token,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（双令牌基接口）

**参数**：

| 参数名          | 类型     | 必填 | 说明                                                                     |
| --------------- | -------- | ---- | ------------------------------------------------------------------------ |
| `minute_token`  | `string` | ✅   | 妙记唯一标识（URL 末段，长度 24 字符），示例值：`obcnq3b9jl72l83w4f14xxxx` |
| `user_id_type`  | `string` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id                    |

**响应**：`data.minute` 含 `token`、`owner_id`、`create_time`、`title`、`cover`、`duration`、`url`、`note_id`。

### 下载妙记音视频文件

获取妙记音视频文件的下载链接（有效期 1 天）。限频：5 次/秒；所需权限：`minutes:minutes.media:export`。

**函数签名**：

```csharp
Task<FeishuApiResult<GetMinuteMediaResult>?> GetMinuteMediaAsync(
    [Path] string minute_token,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（双令牌基接口）

**参数**：`minute_token`（✅，妙记唯一标识）。

**响应**：`data.download_url` 为音视频文件下载链接（1 天有效）。

### 导出妙记文字记录

获取妙记的文字记录（逐字稿），返回文件二进制流。限频：5 次/秒；所需权限：`minutes:minutes.transcript:export`。

**函数签名**：

```csharp
Task<byte[]?> GetMinuteTranscriptAsync(
    [Path] string minute_token,
    [Query("need_speaker")] bool? need_speaker = null,
    [Query("need_timestamp")] bool? need_timestamp = null,
    [Query("file_format")] string? file_format = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（双令牌基接口）

**参数**：

| 参数名          | 类型      | 必填 | 说明                                     |
| --------------- | --------- | ---- | ---------------------------------------- |
| `minute_token`  | `string`  | ✅   | 妙记唯一标识                             |
| `need_speaker`  | `bool?`   | ⚪   | 是否包含说话人                           |
| `need_timestamp`| `bool?`   | ⚪   | 是否包含时间戳                           |
| `file_format`   | `string?` | ⚪   | 导出文件格式，示例值：txt、srt           |

**响应**：文字记录文件二进制内容（`byte[]`）。

**异常说明（`ApiException`）**：服务端返回非 2xx 状态码时抛出。注意：飞书部分业务错误以 HTTP 200 + JSON 错误体返回，此时会把错误 JSON 当作文件内容返回，落盘前应按 `Content-Type` 自检（详见 `documents/ErrorHandling.md`）。

### 获取妙记统计数据

获取妙记的访问统计数据，包括 PV、UV、访问用户 ID 与访问时间。限频：5 次/秒；所需权限：`minutes:minutes.statistics:read`。

**函数签名**：

```csharp
Task<FeishuApiResult<GetMinuteStatisticsResult>?> GetMinuteStatisticsAsync(
    [Path] string minute_token,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（双令牌基接口）

**响应**：`data.statistics` 含 `user_view_count`（UV）、`page_view_count`（PV）与 `user_view_list`（`user_id` + `view_time`）。

### 获取妙记 AI 产物

获取妙记的 AI 产物，包括总结、章节、待办、推荐关键词与逐字稿。限频：5 次/秒；所需权限：`minutes:minutes.artifacts:read`。

**函数签名**：

```csharp
Task<FeishuApiResult<GetMinuteArtifactsResult>?> GetMinuteArtifactsAsync(
    [Path] string minute_token,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（双令牌基接口）

**响应**：`data` 含 `summary`、`minute_chapters`（章节）、`minute_todos`（待办）、`keywords`、`transcript`。

### 搜索妙记

按关键词、所有者、参与者与创建时间等多条件搜索妙记列表，支持分页。限频：5 次/秒；所需权限：`minutes:minutes.search:read`；搜索时间范围最大 1 个月。查询参数采用查询参数模式（body + page_size/page_token）。

**函数签名**：

```csharp
Task<FeishuApiResult<SearchMinutesResult>?> SearchMinutesAsync(
    [Body] SearchMinutesRequest request,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（双令牌基接口）

**参数**：

| 参数名        | 类型                   | 必填 | 说明                                                                     |
| ------------- | ---------------------- | ---- | ------------------------------------------------------------------------ |
| `request`     | `SearchMinutesRequest` | ✅   | `query` 关键词 10～50 字符；`filter`（owner_ids/participant_ids/create_time）；`sorter`。query 与 filter 至少提供一个 |
| `page_size`   | `int?`                 | ⚪   | 分页大小，范围 1～30，默认 15                                            |
| `page_token`  | `string?`              | ⚪   | 分页标记，翻页时取上一次返回的 `page_token`                              |

**响应**：`data` 含 `items`（`MinutesSearchItem`：token、display_info、meta_data）、`total`、`has_more`、`page_token`、`notice`。

### 创建妙记剪辑

基于已完成的妙记与指定时间段创建妙记剪辑；响应成功表示已提交，转写与音视频后台异步生成。限频：5 次/秒；所需权限：`minutes:minutes.clip:write`。每个时间段须大于 1000 毫秒，重叠或相邻区间自动合并。

**函数签名**：

```csharp
Task<FeishuApiResult<MinuteUrlResult>?> ClipMinuteAsync(
    [Path] string minute_token,
    [Body] ClipMinuteRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名         | 类型                | 必填 | 说明                                                     |
| -------------- | ------------------- | ---- | -------------------------------------------------------- |
| `minute_token` | `string`            | ✅   | 妙记唯一标识，示例值：`mt_123456789abcdef0123456789abcdef0` |
| `request`      | `ClipMinuteRequest` | ✅   | `time_ranges` 必填 1～50 个时间段；`title` 可选           |

**响应**：`data.minute_url` 为剪辑妙记链接。

### 导入云盘文件生成妙记

基于云盘音视频文件生成妙记。限频：5 次/秒；所需权限：`minutes:minutes.upload:write`。支持音频 wav/mp3/m4a/aac/ogg/wma/amr、视频 avi/wmv/mov/mp4/m4v/mpeg/ogg/flv；音频不超过 6 小时，文件最大 6GB。

**函数签名**：

```csharp
Task<FeishuApiResult<MinuteUrlResult>?> UploadMinuteAsync(
    [Body] UploadMinuteRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：`request`（✅，`file_token` 必填：音视频云盘文件 token）。

**响应**：`data.minute_url` 为生成的妙记链接。

### 订阅妙记变更事件

订阅当前用户身份相关的妙记变更事件。限频：1000 次/分钟且 50 次/秒；所需权限：`minutes:minutes.basic:read`。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> SubscribeMinuteAsync(
    [Body] MinuteSubscriptionRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：`request`（`event_type` 可选：`minutes.minute.generated_v1` 妙记生成事件）。

**响应**：成功时 `data` 为空对象。

### 取消订阅妙记变更事件

取消订阅当前用户身份相关的妙记变更事件。限频与权限同订阅。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UnsubscribeMinuteAsync(
    [Body] MinuteSubscriptionRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：`request`（`event_type` 可选，同订阅）。

**响应**：成功时 `data` 为空对象。
