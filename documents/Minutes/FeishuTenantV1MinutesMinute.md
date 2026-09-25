# 飞书妙记管理 - 租户令牌（FeishuTenantV1MinutesMinute）

## 接口名称

**飞书妙记管理（租户令牌）** -（`IFeishuTenantV1MinutesMinute`）

## 功能描述

以租户身份调用支持 tenant_access_token 的飞书妙记（Minutes）只读端点：获取基础信息、下载音视频文件、导出文字记录、获取统计数据、获取 AI 产物、搜索妙记。本接口为双令牌基接口 `IFeishuV1MinutesMinute` 的租户令牌派生；创建剪辑、导入生成、事件订阅等 user-only 写端点见 `IFeishuUserV1MinutesMinute`。

## 参考文档

- [获取妙记信息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/get)
- [下载妙记音视频文件](https://open.feishu.cn/document/minutes-v1/minute-media/get)
- [导出妙记文字记录](https://open.feishu.cn/document/minutes-v1/minute-transcript/get)
- [获取妙记统计数据](https://open.feishu.cn/document/server-docs/minutes-v1/minute-statistics/get)
- [获取妙记 AI 产物](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/artifacts)
- [搜索妙记](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/minutes-v1/minute/search)

## 函数列表

| 函数名称                 | 功能描述                       | 认证方式 | HTTP 方法 |
| ------------------------ | ------------------------------ | -------- | --------- |
| GetMinuteAsync           | 获取妙记信息                   | 租户令牌 | GET       |
| GetMinuteMediaAsync      | 下载妙记音视频文件（获取链接） | 租户令牌 | GET       |
| GetMinuteTranscriptAsync | 导出妙记文字记录（二进制流）   | 租户令牌 | GET       |
| GetMinuteStatisticsAsync | 获取妙记统计数据               | 租户令牌 | GET       |
| GetMinuteArtifactsAsync  | 获取妙记 AI 产物               | 租户令牌 | GET       |
| SearchMinutesAsync       | 搜索妙记                       | 租户令牌 | POST      |

## 函数详细内容

6 个只读端点均继承自 `IFeishuV1MinutesMinute`，签名、参数与响应同 [FeishuUserV1MinutesMinute](./FeishuUserV1MinutesMinute.md) 中对应章节，仅认证方式为租户令牌：

- `GetMinuteAsync([Path] string minute_token, [Query("user_id_type")] string? user_id_type = null, CancellationToken cancellationToken = default)`
- `GetMinuteMediaAsync([Path] string minute_token, CancellationToken cancellationToken = default)`
- `GetMinuteTranscriptAsync([Path] string minute_token, [Query("need_speaker")] bool? need_speaker = null, [Query("need_timestamp")] bool? need_timestamp = null, [Query("file_format")] string? file_format = null, CancellationToken cancellationToken = default)` — 返回二进制流（`byte[]`，`ApiException` 残余风险见用户文档说明）
- `GetMinuteStatisticsAsync([Path] string minute_token, [Query("user_id_type")] string? user_id_type = null, CancellationToken cancellationToken = default)`
- `GetMinuteArtifactsAsync([Path] string minute_token, CancellationToken cancellationToken = default)`
- `SearchMinutesAsync([Body] SearchMinutesRequest request, [Query("page_size")] int? page_size = null, [Query("page_token")] string? page_token = null, CancellationToken cancellationToken = default)`
