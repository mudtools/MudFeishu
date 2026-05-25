# 会议管理 - 用户令牌
**IFeishuUserV1VideoConferencingMeeting**

## 功能描述
会议管理功能为用户提供会议操作能力，包括获取会议详情、获取与会议号关联的会议列表、搜索会议、设置主持人、邀请参会人、结束会议等操作。使用用户令牌认证，适用于用户个人场景。

## 参考文档
- [会议管理概述](https://open.feishu.cn/document/server-docs/vc-v1/meeting/meeting-overview)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| GetMeetingAsync | 获取会议详情 | 用户令牌 | GET |
| GetMeetingPageListAsync | 获取与会议号关联的会议列表 | 用户令牌 | GET |
| SearchMeetingPageListAsync | 搜索会议记录 | 用户令牌 | GET |
| SetHostMeetingAsync | 设置主持人 | 用户令牌 | PATCH |
| InviteMeetingAsync | 邀请参会人 | 用户令牌 | PATCH |
| EndMeetingAsync | 结束会议 | 用户令牌 | PATCH |

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
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| meeting_id | string | ✅ | 会议ID（视频会议的唯一标识，视频会议开始后才会产生） | 6911188411932033028 |
| with_participants | bool? | ⚪ | 是否返回参会人列表，默认 false | false |
| with_meeting_ability | bool? | ⚪ | 是否返回会中使用能力统计，默认 false | false |
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
用户令牌

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

### SearchMeetingPageListAsync
根据关键词、时间范围等条件搜索会议记录，返回符合条件的会议列表，包含会议 ID、主题、开始时间及参与者等信息。

**函数签名**
```csharp
Task<FeishuApiPageListResult<MeetingSearchResult>?> SearchMeetingPageListAsync(
    SearchMeetingRequest searchMeetingRequest,
    int page_size = 15,
    string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| searchMeetingRequest | SearchMeetingRequest | ✅ | 会议搜索请求模型，包含关键词、时间范围等搜索条件 | - |
| page_size | int | ⚪ | 分页大小，最大条目数 | 15 |
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
var request = new SearchMeetingRequest
{
    Keyword = "项目周会",
    StartTime = "1655276858",
    EndTime = "1655363258"
};
var result = await api.SearchMeetingPageListAsync(
    searchMeetingRequest: request,
    page_size: 15
);
Console.WriteLine($"搜索结果: {result?.Data?.Items?.Count}");
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
用户令牌

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

### InviteMeetingAsync
邀请参会人进入会议。发起邀请的操作者必须具有相应的权限（如果操作者为用户，则必须在会中），如果会议被锁定、或参会人数如果达到上限，则会邀请失败。

**函数签名**
```csharp
Task<FeishuApiResult<InviteMeetingResult>?> InviteMeetingAsync(
    string meeting_id,
    InviteMeetingRequest inviteMeetingRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| meeting_id | string | ✅ | 会议ID（视频会议的唯一标识，视频会议开始后才会产生） | 6911188411932033028 |
| inviteMeetingRequest | InviteMeetingRequest | ✅ | 邀请参会人请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 操作者必须具有相应权限，用户操作时必须在会中
- 会议被锁定或参会人数达到上限时会邀请失败

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
var request = new InviteMeetingRequest
{
    Invitees = new[] { "ou_xxx", "ou_yyy" }
};
var result = await api.InviteMeetingAsync(
    meeting_id: "6911188411932033028",
    inviteMeetingRequest: request
);
Console.WriteLine(result?.Data);
```

---

### EndMeetingAsync
结束一个进行中的会议。操作者须具有相应的权限（如果操作者为用户，必须是会中当前主持人）。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> EndMeetingAsync(
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
- 会议必须处于进行中状态
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
var result = await api.EndMeetingAsync(
    meeting_id: "6911188411932033028"
);
Console.WriteLine("会议已结束");
```