# 会议录制 - 租户令牌
**IFeishuTenantV1VideoConferencingRecording**

## 功能描述
用于获取会议的录制文件。使用租户令牌认证，可获取租户下会议的录制文件信息。

## 参考文档
- [会议录制概述](https://open.feishu.cn/document/server-docs/vc-v1/meeting-recording/recording-overview)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| GetMeetingRecordingAsync | 获取录制文件 | 租户令牌 | GET |

## 函数详细内容

### GetMeetingRecordingAsync
获取一个会议的录制文件。会议结束后并且收到了录制完成的事件方可获取录制文件。

**函数签名**
```csharp
Task<FeishuApiResult<GetMeetingRecordingResult>?> GetMeetingRecordingAsync(
    string meeting_id,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| meeting_id | string | ✅ | 会议ID（视频会议的唯一标识，视频会议开始后才会产生） | 6911188411932033028 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 会议结束后并且收到了录制完成的事件方可获取录制文件

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "recording_file": {
      "url": "https://...",
      "duration": 3600
    }
  }
}
```

**代码示例**
```csharp
var result = await api.GetMeetingRecordingAsync(
    meeting_id: "6911188411932033028"
);
Console.WriteLine($"录制文件: {result?.Data}");
```