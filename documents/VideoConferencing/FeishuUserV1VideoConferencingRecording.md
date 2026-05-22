# 会议录制（用户令牌）
**IFeishuUserV1VideoConferencingRecording**

## 功能描述
用于管理会议的录制操作，包括开始录制、停止录制、获取录制文件以及授权录制文件。使用用户令牌认证，操作者需具有相应权限（如为会中主持人）。

## 参考文档
- [会议录制概述](https://open.feishu.cn/document/server-docs/vc-v1/meeting-recording/recording-overview)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| StartMeetingRecordingAsync | 开始录制 | 用户令牌 | PATCH |
| StopMeetingRecordingAsync | 停止录制 | 用户令牌 | PATCH |
| GetMeetingRecordingAsync | 获取录制文件 | 用户令牌 | GET |
| SetPermissionMeetingRecordingAsync | 授权录制文件 | 用户令牌 | PATCH |

## 函数详细内容

### StartMeetingRecordingAsync
在会议中开始录制。会议正在进行中，且操作者具有相应权限（如果操作者为用户，必须是会中当前主持人）。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> StartMeetingRecordingAsync(
    string meeting_id,
    StartMeetingRecordingRequest startMeetingRecordingRequest,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| meeting_id | string | ✅ | 会议ID（视频会议的唯一标识，视频会议开始后才会产生） | 6911188411932033028 |
| startMeetingRecordingRequest | StartMeetingRecordingRequest | ✅ | 开始会议录制请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 会议必须正在进行中
- 操作者必须是会中当前主持人

**响应**
```json
{
  "code": 0,
  "msg": "success"
}
```

**代码示例**
```csharp
var request = new StartMeetingRecordingRequest();
var result = await api.StartMeetingRecordingAsync(
    meeting_id: "6911188411932033028",
    startMeetingRecordingRequest: request
);
Console.WriteLine("录制已开始");
```

---

### StopMeetingRecordingAsync
在会议中停止录制。会议正在录制中，且操作者具有相应权限（如果操作者为用户，必须是会中当前主持人）。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> StopMeetingRecordingAsync(
    string meeting_id,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| meeting_id | string | ✅ | 会议ID（视频会议的唯一标识，视频会议开始后才会产生） | 6911188411932033028 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 会议必须正在录制中
- 操作者必须是会中当前主持人

**响应**
```json
{
  "code": 0,
  "msg": "success"
}
```

**代码示例**
```csharp
var result = await api.StopMeetingRecordingAsync(
    meeting_id: "6911188411932033028"
);
Console.WriteLine("录制已停止");
```

---

### GetMeetingRecordingAsync
获取一个会议的录制文件。会议结束后并且收到了录制完成的事件方可获取录制文件。

**函数签名**
```csharp
Task<FeishuApiResult<GetMeetingRecordingResult>?> GetMeetingRecordingAsync(
    string meeting_id,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

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

---

### SetPermissionMeetingRecordingAsync
将一个会议的录制文件授权给组织、用户或公开到公网。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> SetPermissionMeetingRecordingAsync(
    string meeting_id,
    SetPermissionMeetingRecordingRequest setPermissionMeetingRecordingRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| meeting_id | string | ✅ | 会议ID（视频会议的唯一标识，视频会议开始后才会产生） | 6911188411932033028 |
| setPermissionMeetingRecordingRequest | SetPermissionMeetingRecordingRequest | ✅ | 授权录制文件请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 会议结束后并且收到了"录制完成"的事件方可进行授权
- 会议所有者（通过开放平台预约的会议即为预约人）才有权限操作

**响应**
```json
{
  "code": 0,
  "msg": "success"
}
```

**代码示例**
```csharp
var request = new SetPermissionMeetingRecordingRequest
{
    PermissionType = "public"
};
var result = await api.SetPermissionMeetingRecordingAsync(
    meeting_id: "6911188411932033028",
    setPermissionMeetingRecordingRequest: request
);
```