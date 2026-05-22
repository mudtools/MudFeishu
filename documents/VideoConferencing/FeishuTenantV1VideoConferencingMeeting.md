# 会议管理
**IFeishuTenantV1VideoConferencingMeeting**

## 功能描述
会议管理功能提供会议操作能力，包括获取会议详情、获取与会议号关联的会议列表、设置主持人以及移除参会人等操作。使用租户令牌认证，适用于企业管理场景。

## 参考文档
- [会议管理概述](https://open.feishu.cn/document/server-docs/vc-v1/meeting/meeting-overview)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| GetMeetingAsync | 获取会议详情 | 租户令牌 | GET |
| GetMeetingPageListAsync | 获取与会议号关联的会议列表 | 租户令牌 | GET |
| SetHostMeetingAsync | 设置主持人 | 租户令牌 | PATCH |
| KickoutMeetingAsync | 移除参会人 | 租户令牌 | POST |

## 函数详细内容

### GetMeetingAsync
根据会议 ID 获取指定会议的详细信息，包括会议主题、链接、主持人、参会人员、状态、时间信息及关联纪要 ID。

**函数签名**
```csharp
Task<FeishuApiResult<MeetingResult>?> GetMeetingAsync(
    string meeting_id,
    bool? with_participants = null,
    bool? with_meeting_ability = null,
    int? query_mode = null,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| meeting_id | string | ✅ | 会议ID（视频会议的唯一标识，视频会议开始后才会产生） | 6911188411932033028 |
| with_participants | bool? | ⚪ | 是否返回参会人列表，默认 false | false |
| with_meeting_ability | bool? | ⚪ | 是否返回会中使用能力统计，默认 false；仅限 tenant_access_token | false |
| query_mode | int? | ⚪ | 查询模式：0=只查询会议信息（默认），1=只查询会议产物（纪要、逐字稿） | 0 |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 只能获取归属于自己的会议，支持查询最近90天内的会议
- 当 user_id_type 为 user_id 时，参会人列表仅能获取 Lark 用户

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "meeting": {
      "id": "6911188411932033028",
      "topic": "项目周会",
      "status": 2,
      "start_time": "1655276858",
      "end_time": "1655277858"
    }
  }
}
```

**代码示例**
```csharp
var result = await api.GetMeetingAsync(
    meeting_id: "6911188411932033028",
    with_participants: true
);
Console.WriteLine($"会议主题: {result?.Data?.Meeting?.Topic}");
```

---

### GetMeetingPageListAsync
获取指定时间范围内与会议号关联的会议简要信息列表。仅支持查询 90 天内的数据。

**函数签名**
```csharp
Task<FeishuApiResult<MeetingPageListResult>?> GetMeetingPageListAsync(
    string meeting_no,
    string start_time,
    string end_time,
    int page_size = 20,
    string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| meeting_no | string | ✅ | 9位会议号（会议链接最后9位数） | 123456789 |
| start_time | string | ✅ | 查询开始时间（unix时间，单位sec），需小于 end_time | 1608888867 |
| end_time | string | ✅ | 查询结束时间（unix时间，单位sec） | 1608888867 |
| page_size | int | ⚪ | 分页大小，最大条目数 | 20 |
| page_token | string | ⚪ | 分页标记，首次查询不填 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

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
    meeting_no: "123456789",
    start_time: "1608888867",
    end_time: "1608889000",
    page_size: 20
);
Console.WriteLine($"会议数量: {result?.Data?.MeetingList?.Count}");
```

---

### SetHostMeetingAsync
设置会议的主持人。发起设置主持人的操作者必须具有相应的权限（如果操作者为用户，必须是会中当前主持人）。

**函数签名**
```csharp
Task<FeishuApiResult<SetHostMeetingResult>?> SetHostMeetingAsync(
    string meeting_id,
    SetHostMeetingRequest setHostMeetingRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| meeting_id | string | ✅ | 会议ID（视频会议的唯一标识，视频会议开始后才会产生） | 6911188411932033028 |
| setHostMeetingRequest | SetHostMeetingRequest | ✅ | 设置主持人请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 该操作使用CAS并发安全机制，需传入会中当前主持人，如果操作失败可使用返回的最新数据重试

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "host_user": {}
  }
}
```

**代码示例**
```csharp
var request = new SetHostMeetingRequest
{
    HostUserId = "ou_xxx",
    OldHostUserId = "ou_yyy"
};
var result = await api.SetHostMeetingAsync(
    meeting_id: "6911188411932033028",
    setHostMeetingRequest: request
);
Console.WriteLine(result?.Data);
```

---

### KickoutMeetingAsync
将参会人从会议中移除。

**函数签名**
```csharp
Task<FeishuApiResult<KickoutMeetingResult>?> KickoutMeetingAsync(
    string meeting_id,
    KickoutMeetingRequest kickoutMeetingRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| meeting_id | string | ✅ | 会议ID（视频会议的唯一标识，视频会议开始后才会产生） | 6911188411932033028 |
| kickoutMeetingRequest | KickoutMeetingRequest | ✅ | 移除会议用户请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**代码示例**
```csharp
var request = new KickoutMeetingRequest
{
    KickoutUsers = new[] { "ou_xxx" }
};
var result = await api.KickoutMeetingAsync(
    meeting_id: "6911188411932033028",
    kickoutMeetingRequest: request
);
```