# 日历日程 API
**IFeishuTenantV4CalendarEvent**

## 功能描述
日程是存在于日历内的实例资源，开发人员可以通过关联特定日期或时间段、参与人、地点等规则，构建指定主题内容的工作安排。包括日程的 CRUD 操作、搜索、回复、参与人管理、会议群管理、会议纪要以及请假日程等功能。

## 参考文档
- [日历日程概述](https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/introduction)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateCalendarEventAsync | 创建日程 | 租户令牌 | POST |
| DeleteCalendarEventAsync | 删除日程 | 租户令牌 | DELETE |
| UpdateCalendarEventAsync | 更新日程 | 租户令牌 | PATCH |
| GetCalendarEventAsync | 获取日程 | 租户令牌 | GET |
| GetCalendarEventPageListAsync | 获取日程列表 | 租户令牌 | GET |
| SearchCalendarEventPageListAsync | 搜索日程 | 租户令牌 | POST |
| ReplyCalendarEventAsync | 回复日程 | 租户令牌 | POST |
| GetInstancesCalendarEventPageListAsync | 获取重复日程实例 | 租户令牌 | GET |
| GetInstanceViewCalendarEventAsync | 查询日程视图 | 租户令牌 | GET |
| CreateCalendarEventMeetingChatAsync | 创建会议群 | 租户令牌 | POST |
| DeleteCalendarEventMeetingChatAsync | 解绑会议群 | 租户令牌 | DELETE |
| CreateCalendarEventMeetingMinuteAsync | 创建会议纪要 | 租户令牌 | POST |
| QueryMeetingRoomFreebusyAsync | 查询会议室忙闲 | 租户令牌 | GET |
| CreateCalendarEventAttendeeAsync | 添加日程参与人 | 租户令牌 | POST |
| DeleteCalendarEventAttendeeAsync | 删除日程参与人 | 租户令牌 | DELETE |
| GetCalendarEventAttendeePageListAsync | 分页获取日程参与人列表 | 租户令牌 | GET |
| GetCalendarEventAttendeeChatMemberPageListAsync | 获取日程参与群成员列表 | 租户令牌 | GET |
| CreateTimeoffEventAsync | 创建请假日程 | 租户令牌 | POST |
| DeleteTimeoffEventAsync | 删除请假日程 | 租户令牌 | DELETE |
| GetMeetingRoomSummaryAsync | 查询会议室日程主题和详情 | 租户令牌 | POST |
| ReplyMeetingRoomEventInstanceAsync | 回复会议室日程实例 | 租户令牌 | POST |

## 函数详细内容

### CreateCalendarEventAsync
在指定日历上创建一个日程。

**函数签名**
```csharp
Task<FeishuApiResult<CalendarEventOopsResult>?> CreateCalendarEventAsync(
    string calendar_id,
    CreateCalendarEventRequest createCalendarEventRequest,
    string? idempotency_key = null,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| createCalendarEventRequest | CreateCalendarEventRequest | ✅ | 创建日程请求体 | - |
| idempotency_key | string | ⚪ | 幂等 key，避免重复创建 | 25fdf41b-8c80-2ce1-e94c-de8b5e7aa7e6 |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var request = new CreateCalendarEventRequest
{
    Summary = "项目周会",
    StartTime = new TimeInfo { Timestamp = "1704067200" },
    EndTime = new TimeInfo { Timestamp = "1704070800" }
};
var result = await api.CreateCalendarEventAsync(
    calendar_id: "feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn",
    createCalendarEventRequest: request
);
```

---

### DeleteCalendarEventAsync
删除指定日历上的一个日程。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteCalendarEventAsync(
    string calendar_id,
    string event_id,
    bool? need_notification = true,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| event_id | string | ✅ | 日程 ID | xxxxxxxxx_0 |
| need_notification | bool? | ⚪ | 是否发送 Bot 通知，默认 true | false |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### UpdateCalendarEventAsync
更新指定日历上的一个日程。

**函数签名**
```csharp
Task<FeishuApiResult<CalendarEventOopsResult>?> UpdateCalendarEventAsync(
    string calendar_id,
    string event_id,
    UpdateCalendarEventRequest updateCalendarEventRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| event_id | string | ✅ | 日程 ID | xxxxxxxxx_0 |
| updateCalendarEventRequest | UpdateCalendarEventRequest | ✅ | 更新日程请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### GetCalendarEventAsync
获取指定日历内的某一日程信息。

**函数签名**
```csharp
Task<FeishuApiResult<GetCalendarEventResult>?> GetCalendarEventAsync(
    string calendar_id,
    string event_id,
    bool? need_meeting_settings = false,
    bool? need_attendee = null,
    int? max_attendee_num = 10,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| event_id | string | ✅ | 日程 ID | xxxxxxxxx_0 |
| need_meeting_settings | bool? | ⚪ | 是否返回 VC 会前设置 | false |
| need_attendee | bool? | ⚪ | 是否返回参与人信息 | false |
| max_attendee_num | int? | ⚪ | 最大参与人数量 | 10 |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### GetCalendarEventPageListAsync
获取指定日历内的日程列表，支持分页、增量同步、时间范围查询。

**函数签名**
```csharp
Task<FeishuApiResult<GetCalendarEventPageListResult>?> GetCalendarEventPageListAsync(
    string calendar_id,
    int page_size = 20,
    string? page_token = null,
    string? anchor_time = null,
    string? sync_token = null,
    string? start_time = null,
    string? end_time = null,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| page_size | int | ⚪ | 分页大小 | 20 |
| page_token | string | ⚪ | 分页标记 | - |
| anchor_time | string | ⚪ | 时间锚点，Unix 时间戳（秒） | 1609430400 |
| sync_token | string | ⚪ | 增量同步标记 | - |
| start_time | string | ⚪ | 开始时间，Unix 时间戳（秒） | 1631777271 |
| end_time | string | ⚪ | 结束时间，Unix 时间戳（秒） | 1631777271 |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### SearchCalendarEventPageListAsync
搜索指定日历下的相关日程。

**函数签名**
```csharp
Task<FeishuApiPageListResult<SearchCalendarEventResult>?> SearchCalendarEventPageListAsync(
    string calendar_id,
    SearchCalendarEventRequest searchCalendarEventRequest,
    int page_size = 20,
    string? page_token = null,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| searchCalendarEventRequest | SearchCalendarEventRequest | ✅ | 搜索日程请求体 | - |
| page_size | int | ⚪ | 分页大小 | 20 |
| page_token | string | ⚪ | 分页标记 | - |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### ReplyCalendarEventAsync
回复日程（接受/拒绝/待定）。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> ReplyCalendarEventAsync(
    string calendar_id,
    string event_id,
    ReplyCalendarEventRequest replyCalendarEventRequest,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| event_id | string | ✅ | 日程 ID | xxxxxxxxx_0 |
| replyCalendarEventRequest | ReplyCalendarEventRequest | ✅ | 回复日程请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### GetInstancesCalendarEventPageListAsync
获取重复日程的实例信息。

**函数签名**
```csharp
Task<FeishuApiPageListResult<CalendarEventInstanceResult>?> GetInstancesCalendarEventPageListAsync(
    string calendar_id,
    string event_id,
    string start_time,
    string end_time,
    int page_size = 50,
    string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| event_id | string | ✅ | 日程 ID | xxxxxxxxx_0 |
| start_time | string | ✅ | 开始时间，Unix 时间戳（秒） | 1631777271 |
| end_time | string | ✅ | 结束时间，Unix 时间戳（秒） | 1631777271 |
| page_size | int | ⚪ | 分页大小 | 50 |
| page_token | string | ⚪ | 分页标记 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- start_time 与 end_time 之间的时间区间不能超过 2 年

---

### GetInstanceViewCalendarEventAsync
查询日程视图，将重复日程按规则展开为多个实例。

**函数签名**
```csharp
Task<FeishuApiResult<GetInstanceViewCalendarEventResult>?> GetInstanceViewCalendarEventAsync(
    string calendar_id,
    string start_time,
    string end_time,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| start_time | string | ✅ | 开始时间，Unix 时间戳（秒） | 1631777271 |
| end_time | string | ✅ | 结束时间，Unix 时间戳（秒） | 1631777271 |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### CreateCalendarEventMeetingChatAsync
为指定日程创建一个会议群。

**函数签名**
```csharp
Task<FeishuApiResult<CreateCalendarEventMeetingChatResult>?> CreateCalendarEventMeetingChatAsync(
    string calendar_id,
    string event_id,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| event_id | string | ✅ | 日程 ID | xxxxxxxxx_0 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### DeleteCalendarEventMeetingChatAsync
为日程解绑已创建的会议群。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteCalendarEventMeetingChatAsync(
    string calendar_id,
    string event_id,
    string meeting_chat_id,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| event_id | string | ✅ | 日程 ID | xxxxxxxxx_0 |
| meeting_chat_id | string | ✅ | 会议群 ID | oc_xxx |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### CreateCalendarEventMeetingMinuteAsync
为指定日程创建会议纪要，返回纪要文档 URL。

**函数签名**
```csharp
Task<FeishuApiResult<CreateCalendarEventMeetingMinuteResult>?> CreateCalendarEventMeetingMinuteAsync(
    string calendar_id,
    string event_id,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| event_id | string | ✅ | 日程 ID | xxxxxxxxx_0 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### QueryMeetingRoomFreebusyAsync
获取指定会议室的忙碌/空闲日程信息。

**函数签名**
```csharp
Task<FeishuApiResult<QueryMeetingRoomFreebusyResult>?> QueryMeetingRoomFreebusyAsync(
    string[] room_ids,
    string time_min,
    string time_max,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| room_ids | string[] | ✅ | 会议室 ID 列表（不超过 20 个） | ["omm_xxxx"] |
| time_min | string | ✅ | 查询起始时间，RFC3339 格式 | 2019-09-04T08:45:00+08:00 |
| time_max | string | ✅ | 查询结束时间，RFC3339 格式 | 2019-09-04T09:45:00+08:00 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### CreateCalendarEventAttendeeAsync
为指定日程添加一个或多个参与人。

**函数签名**
```csharp
Task<FeishuApiResult<CreateCalendarEventAttendeeResult>?> CreateCalendarEventAttendeeAsync(
    string calendar_id,
    string event_id,
    CreateCalendarEventAttendeeRequest createCalendarEventAttendeeRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| event_id | string | ✅ | 日程 ID | xxxxxxxxx_0 |
| createCalendarEventAttendeeRequest | CreateCalendarEventAttendeeRequest | ✅ | 创建参与人请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### DeleteCalendarEventAttendeeAsync
删除指定日程的一个或多个参与人。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteCalendarEventAttendeeAsync(
    string calendar_id,
    string event_id,
    DeleteCalendarEventAttendeeRequest deleteCalendarEventAttendeeRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| event_id | string | ✅ | 日程 ID | xxxxxxxxx_0 |
| deleteCalendarEventAttendeeRequest | DeleteCalendarEventAttendeeRequest | ✅ | 删除参与人请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### GetCalendarEventAttendeePageListAsync
分页获取日程的参与人列表。

**函数签名**
```csharp
Task<FeishuApiPageListResult<CalendarEventAttendeeInfoResult>?> GetCalendarEventAttendeePageListAsync(
    string calendar_id,
    string event_id,
    bool? need_resource_customization = null,
    string? page_token = null,
    int? page_size = 20,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| event_id | string | ✅ | 日程 ID | xxxxxxxxx_0 |
| need_resource_customization | bool? | ⚪ | 是否需要会议室表单信息 | true |
| page_token | string | ⚪ | 分页标记 | - |
| page_size | int? | ⚪ | 分页大小 | 20 |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### GetCalendarEventAttendeeChatMemberPageListAsync
获取日程的群组类型参与人的群成员列表。

**函数签名**
```csharp
Task<FeishuApiPageListResult<CalendarEventAttendeeChatMember>?> GetCalendarEventAttendeeChatMemberPageListAsync(
    string calendar_id,
    string event_id,
    string attendee_id,
    string? page_token = null,
    int? page_size = 20,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| event_id | string | ✅ | 日程 ID | xxxxxxxxx_0 |
| attendee_id | string | ✅ | 群组类型参与人 ID | chat_xxxxxx |
| page_token | string | ⚪ | 分页标记 | - |
| page_size | int? | ⚪ | 分页大小 | 20 |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### CreateTimeoffEventAsync
为指定用户创建一个请假日程。请假期间用户个人签名页会展示请假信息。

**函数签名**
```csharp
Task<FeishuApiResult<CreateTimeoffEventResult>?> CreateTimeoffEventAsync(
    CreateTimeoffEventRequest createTimeoffEventRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| createTimeoffEventRequest | CreateTimeoffEventRequest | ✅ | 创建请假日程请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### DeleteTimeoffEventAsync
删除一个指定的请假日程，用户签名页的请假信息也会消失。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteTimeoffEventAsync(
    string timeoff_event_id,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| timeoff_event_id | string | ✅ | 请假日程 ID | timeoff:XXXXXX-XXXX-0917-1623-aa493d591a39 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### GetMeetingRoomSummaryAsync
使用日程的 Uid 和 Original time 查询会议室日程主题与详情。

**函数签名**
```csharp
Task<FeishuApiResult<GetMeetingRoomSummaryResult>?> GetMeetingRoomSummaryAsync(
    GetMeetingRoomSummaryRequest getMeetingRoomSummaryRequest,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| getMeetingRoomSummaryRequest | GetMeetingRoomSummaryRequest | ✅ | 查询会议室日程请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### ReplyMeetingRoomEventInstanceAsync
回复会议室日程实例，支持回复未签到释放、提前结束释放、被管理员置为接受/拒绝。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> ReplyMeetingRoomEventInstanceAsync(
    ReplyMeetingRoomEventInstanceRequest replyMeetingRoomInstanceRequest,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| replyMeetingRoomInstanceRequest | ReplyMeetingRoomEventInstanceRequest | ✅ | 回复会议室日程实例请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |