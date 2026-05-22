# 会议导出
**IFeishuTenantV1VideoConferencingExports**

## 功能描述
用于导出一段时间内租户的会议数据，包括导出会议明细、参会人明细、参会人会议质量数据以及会议室预定数据。支持异步导出任务，可查询任务结果并下载导出文件。

## 参考文档
- [导出概述](https://open.feishu.cn/document/server-docs/vc-v1/export/export-overview)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| MeetingListExportAsync | 导出会议明细 | 租户令牌 | POST |
| ParticipantListExportAsync | 导出参会人明细 | 租户令牌 | POST |
| ParticipantQualityListExportAsync | 导出参会人会议质量数据 | 租户令牌 | POST |
| ResourceReservationListExportAsync | 导出会议室预定数据 | 租户令牌 | POST |
| GetExportAsync | 查询导出任务结果 | 租户令牌 | GET |
| DownloadExportAsync | 下载导出文件 | 租户令牌 | GET |

## 函数详细内容

### MeetingListExportAsync
导出会议明细，返回异步任务ID，可通过查询任务结果接口获取下载链接。

**函数签名**
```csharp
Task<FeishuApiResult<MeetingExportResult>?> MeetingListExportAsync(
    MeetingListExportRequest meetingListExportRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| meetingListExportRequest | MeetingListExportRequest | ✅ | 导出会议明细请求体，包含时间范围等筛选条件 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "task_id": "7108646852144136212"
  }
}
```

**代码示例**
```csharp
var request = new MeetingListExportRequest
{
    StartTime = "1655276858",
    EndTime = "1655363258"
};
var result = await api.MeetingListExportAsync(request);
Console.WriteLine($"任务ID: {result?.Data?.TaskId}");
```

---

### ParticipantListExportAsync
导出某个会议的参会人详情列表。

**函数签名**
```csharp
Task<FeishuApiResult<MeetingExportResult>?> ParticipantListExportAsync(
    ParticipantListExportRequest participantListExportRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| participantListExportRequest | ParticipantListExportRequest | ✅ | 导出会议参与人明细请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "task_id": "7108646852144136212"
  }
}
```

**代码示例**
```csharp
var request = new ParticipantListExportRequest
{
    MeetingNo = "123456789",
    StartTime = "1655276858",
    EndTime = "1655363258"
};
var result = await api.ParticipantListExportAsync(request);
Console.WriteLine($"任务ID: {result?.Data?.TaskId}");
```

---

### ParticipantQualityListExportAsync
导出某场会议某个参会人的音视频/共享质量数据（仅支持已结束会议）。

**函数签名**
```csharp
Task<FeishuApiResult<MeetingExportResult>?> ParticipantQualityListExportAsync(
    ParticipantQualityListExportRequest participantQualityListExportRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| participantQualityListExportRequest | ParticipantQualityListExportRequest | ✅ | 导出参会人会议质量数据请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "task_id": "7108646852144136212"
  }
}
```

**代码示例**
```csharp
var request = new ParticipantQualityListExportRequest
{
    MeetingNo = "123456789",
    StartTime = "1655276858",
    EndTime = "1655363258"
};
var result = await api.ParticipantQualityListExportAsync(request);
Console.WriteLine($"任务ID: {result?.Data?.TaskId}");
```

---

### ResourceReservationListExportAsync
导出会议室预定数据。

**函数签名**
```csharp
Task<FeishuApiResult<MeetingExportResult>?> ResourceReservationListExportAsync(
    ResourceReservationListExportRequest resourceReservationListExportRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| resourceReservationListExportRequest | ResourceReservationListExportRequest | ✅ | 导出会议室预定数据请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "task_id": "7108646852144136212"
  }
}
```

**代码示例**
```csharp
var request = new ResourceReservationListExportRequest
{
    RoomLevelId = "omb_57c9cc7d9a81e27e54c8fabfd02759e7",
    StartTime = "1655276858",
    EndTime = "1655363258"
};
var result = await api.ResourceReservationListExportAsync(request);
Console.WriteLine($"任务ID: {result?.Data?.TaskId}");
```

---

### GetExportAsync
根据任务ID查询导出任务的结果，获取导出文件信息。

**函数签名**
```csharp
Task<FeishuApiResult<ExportResult>?> GetExportAsync(
    string? task_id,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| task_id | string | ✅ | 导出任务ID | 7108646852144136212 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "status": 1,
    "file_token": "6yHu7Igp7Igy62Ez6fLr6IJz7j9i5WMe6fHq5yZeY2Jz6yLqYAMAY46fZfEz64Lr5fYyYQ==",
    "url": "https://..."
  }
}
```

**说明**
- 导出为异步任务，需先调用对应的导出接口获取 task_id，再通过此接口查询任务状态
- 任务完成后可通过返回的 file_token 调用下载接口获取文件

**代码示例**
```csharp
var result = await api.GetExportAsync(task_id: "7108646852144136212");
Console.WriteLine($"任务状态: {result?.Data?.Status}");
```

---

### DownloadExportAsync
下载导出文件，返回文件字节数组。

**函数签名**
```csharp
Task<byte[]?> DownloadExportAsync(
    string? file_token,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| file_token | string | ✅ | 导出文件的token，从查询导出任务结果接口获取 | 6yHu7Igp7Igy62Ez6fLr6IJz7j9i5WMe6fHq5yZeY2Jz6yLqYAMAY46fZfEz64Lr5fYyYQ== |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
返回文件的字节数组（`byte[]`）。

**代码示例**
```csharp
var fileBytes = await api.DownloadExportAsync(
    file_token: "6yHu7Igp7Igy62Ez6fLr6IJz7j9i5WMe6fHq5yZeY2Jz6yLqYAMAY46fZfEz64Lr5fYyYQ=="
);
if (fileBytes != null)
{
    await System.IO.File.WriteAllBytesAsync("export.csv", fileBytes);
}
```