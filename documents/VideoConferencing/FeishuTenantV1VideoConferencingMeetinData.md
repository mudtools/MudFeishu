# 会议数据查询 - 租户令牌
**IFeishuTenantV1VideoConferencingMeetinData**

## 功能描述
用于分页查询一段时间内租户的会议数据，包括查询会议明细、参会人明细、参会人会议质量数据、会议室预定数据以及设备告警记录。使用租户令牌认证，可查询租户下全部会议数据。

## 参考文档
- [会议数据资源介绍](https://open.feishu.cn/document/server-docs/vc-v1/meeting-room-data/resource-introduction)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| GetMeetingPageListAsync | 分页查询会议明细 | 租户令牌 | GET |
| GetParticipantPageListAsync | 分页查询参会人明细 | 租户令牌 | GET |
| GetParticipantQualityPageListAsync | 分页查询参会人会议质量数据 | 租户令牌 | GET |
| GetResourceReservationPageListAsync | 分页查询会议室预定数据 | 租户令牌 | GET |
| GetAlertPageListAsync | 分页查询设备告警记录 | 租户令牌 | GET |

## 函数详细内容

### GetMeetingPageListAsync
根据时间范围分页查询会议明细，支持按用户、会议室、会议类型等条件筛选。

**函数签名**
```csharp
Task<FeishuApiResult<GetMeetingListResult>?> GetMeetingPageListAsync(
    string start_time,
    string end_time,
    int? meeting_status = null,
    string? meeting_no = null,
    string? user_id = null,
    string? room_id = null,
    int? meeting_type = null,
    bool? include_external_meetings = null,
    bool? include_webinar = null,
    int? page_size = 20,
    string? page_token = null,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| start_time | string | ✅ | 查询开始时间（unix时间，单位sec） | 1608888867 |
| end_time | string | ✅ | 查询结束时间（unix时间，单位sec） | 1608888867 |
| meeting_status | int? | ⚪ | 会议状态：1=进行中，2=已结束，3=待召开（只读） | 2 |
| meeting_no | string | ⚪ | 9位会议号（会议链接最后9位数） | 123456789 |
| user_id | string | ⚪ | 按参会飞书用户筛选 | ou_3ec3f6a28a0d08c45d895276e8e5e19b |
| room_id | string | ⚪ | 按参会Rooms筛选 | omm_eada1d61a550955240c28757e7dec3af |
| meeting_type | int? | ⚪ | 按会议类型筛选：1=全部类型，2=视频会议，3=本地投屏 | 2 |
| include_external_meetings | bool? | ⚪ | 是否查询外部会议 | false |
| include_webinar | bool? | ⚪ | 是否查询网络研讨会 | false |
| page_size | int? | ⚪ | 分页大小，最大条目数 | 20 |
| page_token | string | ⚪ | 分页标记，首次查询不填 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- user_id、room_id 和 meeting_type 最多只能设置一个筛选条件，设置多个会导致参数校验失败

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "meeting_list": [],
    "page_token": "",
    "has_more": false
  }
}
```

**代码示例**
```csharp
var result = await api.GetMeetingPageListAsync(
    start_time: "1608888867",
    end_time: "1608889000",
    meeting_status: 2,
    page_size: 20
);
Console.WriteLine($"会议数量: {result?.Data?.MeetingList?.Count}");
```

---

### GetParticipantPageListAsync
根据会议号和时间范围分页查询参会人明细。

**函数签名**
```csharp
Task<FeishuApiResult<GetParticipantListResult>?> GetParticipantPageListAsync(
    string meeting_start_time,
    string meeting_end_time,
    string meeting_no,
    int? meeting_status = null,
    string? user_id = null,
    string? room_id = null,
    string? webinar_user_role = null,
    int? page_size = 20,
    string? page_token = null,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| meeting_start_time | string | ✅ | 会议开始时间（unix时间，单位sec） | 1655276858 |
| meeting_end_time | string | ✅ | 会议结束时间（unix时间，单位sec，进行中会议填当前时间） | 1655276858 |
| meeting_no | string | ✅ | 9位会议号（会议链接最后9位数） | 123456789 |
| meeting_status | int? | ⚪ | 会议状态：1=进行中，2=已结束，3=待召开 | 2 |
| user_id | string | ⚪ | 按参会飞书用户筛选 | ou_3ec3f6a28a0d08c45d895276e8e5e19b |
| room_id | string | ⚪ | 按参会Rooms筛选 | omm_eada1d61a550955240c28757e7dec3af |
| webinar_user_role | string | ⚪ | 网络研讨会观众类型：0=嘉宾，3=观众 | 0 |
| page_size | int? | ⚪ | 分页大小，最大条目数 | 20 |
| page_token | string | ⚪ | 分页标记，首次查询不填 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- user_id 和 room_id 最多只能设置一个筛选条件

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "participant_list": [],
    "page_token": "",
    "has_more": false
  }
}
```

**代码示例**
```csharp
var result = await api.GetParticipantPageListAsync(
    meeting_start_time: "1655276858",
    meeting_end_time: "1655276900",
    meeting_no: "123456789",
    page_size: 20
);
Console.WriteLine($"参会人数: {result?.Data?.ParticipantList?.Count}");
```

---

### GetParticipantQualityPageListAsync
查询参会人会议质量数据（仅支持已结束会议），返回音视频及共享质量数据。

**函数签名**
```csharp
Task<FeishuApiResult<GetParticipantQualityListResult>?> GetParticipantQualityPageListAsync(
    string meeting_start_time,
    string meeting_end_time,
    string meeting_no,
    string join_time,
    string? user_id = null,
    string? room_id = null,
    int? page_size = 20,
    string? page_token = null,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| meeting_start_time | string | ✅ | 会议开始时间（unix时间，单位sec） | 1655276858 |
| meeting_end_time | string | ✅ | 会议结束时间（unix时间，单位sec） | 1655276858 |
| meeting_no | string | ✅ | 9位会议号 | 123456789 |
| join_time | string | ✅ | 参会人入会时间（unix时间，单位sec），可从查询参会人明细接口获取 | 1655276858 |
| user_id | string | ⚪ | 按参会飞书用户筛选 | ou_3ec3f6a28a0d08c45d895276e8e5e19b |
| room_id | string | ⚪ | 按参会Rooms筛选 | omm_eada1d61a550955240c28757e7dec3af |
| page_size | int? | ⚪ | 分页大小，最大条目数 | 20 |
| page_token | string | ⚪ | 分页标记，首次查询不填 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 仅支持查询已结束会议的参会人质量数据

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "participant_quality_list": [],
    "page_token": "",
    "has_more": false
  }
}
```

**代码示例**
```csharp
var result = await api.GetParticipantQualityPageListAsync(
    meeting_start_time: "1655276858",
    meeting_end_time: "1655276900",
    meeting_no: "123456789",
    join_time: "1655276860",
    page_size: 20
);
Console.WriteLine($"质量数据条数: {result?.Data?.ParticipantQualityList?.Count}");
```

---

### GetResourceReservationPageListAsync
分页查询会议室预定数据，支持按层级和会议室ID筛选。

**函数签名**
```csharp
Task<FeishuApiResult<GetResourceReservationListResult>?> GetResourceReservationPageListAsync(
    string room_level_id,
    string start_time,
    string end_time,
    string[] room_ids,
    bool? need_topic = null,
    bool? is_exclude = null,
    int? page_size = 20,
    string? page_token = null,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| room_level_id | string | ✅ | 层级ID，非omb前缀的异常ID时默认使用租户层级兜底 | omb_57c9cc7d9a81e27e54c8fabfd02759e7 |
| start_time | string | ✅ | 查询开始时间（unix时间，单位sec） | 1655276858 |
| end_time | string | ✅ | 查询结束时间（unix时间，单位sec） | 1655276858 |
| room_ids | string[] | ✅ | 待筛选的会议室ID列表 | ["omm_12443435556"] |
| need_topic | bool? | ⚪ | 是否展示会议主题 | true |
| is_exclude | bool? | ⚪ | true时排除room_ids列表中的会议室，获取剩余会议室预定数据 | false |
| page_size | int? | ⚪ | 分页大小，最大条目数 | 20 |
| page_token | string | ⚪ | 分页标记，首次查询不填 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "resource_reservation_list": [],
    "page_token": "",
    "has_more": false
  }
}
```

**代码示例**
```csharp
var result = await api.GetResourceReservationPageListAsync(
    room_level_id: "omb_57c9cc7d9a81e27e54c8fabfd02759e7",
    start_time: "1655276858",
    end_time: "1655363258",
    room_ids: new[] { "omm_12443435556" },
    page_size: 20
);
Console.WriteLine($"预定数据条数: {result?.Data?.ResourceReservationList?.Count}");
```

---

### GetAlertPageListAsync
获取特定条件下租户的设备告警记录。

**函数签名**
```csharp
Task<FeishuApiPageListResult<AlertInfo>?> GetAlertPageListAsync(
    string start_time,
    string end_time,
    int? query_type = null,
    string? query_value = null,
    int? page_size = 20,
    string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| start_time | string | ✅ | 查询开始时间（unix时间，单位sec） | 1655276858 |
| end_time | string | ✅ | 查询结束时间（unix时间，单位sec） | 1655276858 |
| query_type | int? | ⚪ | 查询对象类型：1=会议室，2=企业会议室连接器，3=SIP会议室系统 | 1 |
| query_value | string | ⚪ | 查询对象ID（会议室ID或企业会议室连接器ID） | omm_4de32cf10a4358788ff4e09e37ebbf9b |
| page_size | int? | ⚪ | 分页大小，最大条目数 | 20 |
| page_token | string | ⚪ | 分页标记，首次查询不填 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [],
    "page_token": "",
    "has_more": false
  }
}
```

**代码示例**
```csharp
var result = await api.GetAlertPageListAsync(
    start_time: "1655276858",
    end_time: "1655363258",
    query_type: 1,
    page_size: 20
);
Console.WriteLine($"告警数量: {result?.Data?.Items?.Count}");
```