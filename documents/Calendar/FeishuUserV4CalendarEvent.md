# 日历日程 API（用户令牌）
**IFeishuUserV4CalendarEvent**

## 功能描述
以用户身份管理日历日程资源。包括日程的 CRUD 操作、搜索、回复、参与人管理、会议群/纪要管理、会议室忙闲查询，以及订阅/取消订阅日程变更事件、CalDAV 配置生成和 Exchange 账户绑定等用户专属功能。

## 参考文档
- [日历日程概述](https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/introduction)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateCalendarEventAsync | 创建日程 | 用户令牌 | POST |
| DeleteCalendarEventAsync | 删除日程 | 用户令牌 | DELETE |
| UpdateCalendarEventAsync | 更新日程 | 用户令牌 | PATCH |
| GetCalendarEventAsync | 获取日程 | 用户令牌 | GET |
| GetCalendarEventPageListAsync | 获取日程列表 | 用户令牌 | GET |
| SearchCalendarEventPageListAsync | 搜索日程 | 用户令牌 | POST |
| ReplyCalendarEventAsync | 回复日程 | 用户令牌 | POST |
| GetInstancesCalendarEventPageListAsync | 获取重复日程实例 | 用户令牌 | GET |
| GetInstanceViewCalendarEventAsync | 查询日程视图 | 用户令牌 | GET |
| CreateCalendarEventMeetingChatAsync | 创建会议群 | 用户令牌 | POST |
| DeleteCalendarEventMeetingChatAsync | 解绑会议群 | 用户令牌 | DELETE |
| CreateCalendarEventMeetingMinuteAsync | 创建会议纪要 | 用户令牌 | POST |
| QueryMeetingRoomFreebusyAsync | 查询会议室忙闲 | 用户令牌 | GET |
| CreateCalendarEventAttendeeAsync | 添加日程参与人 | 用户令牌 | POST |
| DeleteCalendarEventAttendeeAsync | 删除日程参与人 | 用户令牌 | DELETE |
| GetCalendarEventAttendeePageListAsync | 获取日程参与人列表 | 用户令牌 | GET |
| GetCalendarEventAttendeeChatMemberPageListAsync | 获取参与群成员列表 | 用户令牌 | GET |
| SubscriptionCalendarEventAsync | 订阅日程变更事件 | 用户令牌 | POST |
| UnSubscriptionCalendarEventAsync | 取消订阅日程变更事件 | 用户令牌 | POST |
| GenerateCaldavConfSettingAsync | 生成 CalDAV 配置 | 用户令牌 | POST |
| CreateExchangeBindingAsync | 绑定 Exchange 账户 | 用户令牌 | POST |
| DeleteExchangeBindingAsync | 删除 Exchange 绑定 | 用户令牌 | DELETE |
| GetExchangeBindingAsync | 查询 Exchange 绑定状态 | 用户令牌 | GET |

## 函数详细内容

### 基础日程操作

> 以下 17 个函数与租户版本共用基类接口，功能相同，区别在于使用用户令牌认证。

### CreateCalendarEventAsync
以用户身份在指定日历上创建一个日程。支持幂等 key 防重复。

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
用户令牌

| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| createCalendarEventRequest | CreateCalendarEventRequest | ✅ | 创建日程请求体 | - |
| idempotency_key | string | ⚪ | 幂等 key | 25fdf41b-8c80-2ce1-e94c-de8b5e7aa7e6 |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### DeleteCalendarEventAsync
以用户身份删除指定日历上的一个日程。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteCalendarEventAsync(
    string calendar_id,
    string event_id,
    bool? need_notification = true,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

---

### UpdateCalendarEventAsync
以用户身份更新指定日历上的一个日程。

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
用户令牌

---

### GetCalendarEventAsync
以用户身份获取指定日历内的某一日程信息。

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
用户令牌

---

### GetCalendarEventPageListAsync
以用户身份分页获取日程列表，支持增量同步、时间范围查询。

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
用户令牌

---

### SearchCalendarEventPageListAsync
以用户身份搜索日程。

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
用户令牌

---

### ReplyCalendarEventAsync
以用户身份回复日程（接受/拒绝/待定）。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> ReplyCalendarEventAsync(
    string calendar_id,
    string event_id,
    ReplyCalendarEventRequest replyCalendarEventRequest,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

---

### GetInstancesCalendarEventPageListAsync
以用户身份获取重复日程的实例信息。

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
用户令牌

**说明**
- start_time 与 end_time 之间不能超过 2 年

---

### GetInstanceViewCalendarEventAsync
以用户身份查询日程视图，展开重复日程实例。

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
用户令牌

---

### CreateCalendarEventMeetingChatAsync
以用户身份为指定日程创建会议群。

**函数签名**
```csharp
Task<FeishuApiResult<CreateCalendarEventMeetingChatResult>?> CreateCalendarEventMeetingChatAsync(
    string calendar_id,
    string event_id,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

---

### DeleteCalendarEventMeetingChatAsync
以用户身份解绑日程的会议群。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteCalendarEventMeetingChatAsync(
    string calendar_id,
    string event_id,
    string meeting_chat_id,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

---

### CreateCalendarEventMeetingMinuteAsync
以用户身份为日程创建会议纪要。

**函数签名**
```csharp
Task<FeishuApiResult<CreateCalendarEventMeetingMinuteResult>?> CreateCalendarEventMeetingMinuteAsync(
    string calendar_id,
    string event_id,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

---

### QueryMeetingRoomFreebusyAsync
以用户身份获取指定会议室的忙闲信息。

**函数签名**
```csharp
Task<FeishuApiResult<QueryMeetingRoomFreebusyResult>?> QueryMeetingRoomFreebusyAsync(
    string[] room_ids,
    string time_min,
    string time_max,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

---

### CreateCalendarEventAttendeeAsync
以用户身份为日程添加参与人。

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
用户令牌

---

### DeleteCalendarEventAttendeeAsync
以用户身份删除日程参与人。

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
用户令牌

---

### GetCalendarEventAttendeePageListAsync
以用户身份分页获取日程参与人列表。

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
用户令牌

---

### GetCalendarEventAttendeeChatMemberPageListAsync
以用户身份获取日程参与群成员列表。

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
用户令牌

---

### 用户专属功能

### SubscriptionCalendarEventAsync
以用户身份订阅指定日历下的日程变更事件。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> SubscriptionCalendarEventAsync(
    string calendar_id,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
await api.SubscriptionCalendarEventAsync("feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn");
```

---

### UnSubscriptionCalendarEventAsync
以用户身份取消订阅指定日历下的日程变更事件。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UnSubscriptionCalendarEventAsync(
    string calendar_id,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### GenerateCaldavConfSettingAsync
为当前用户生成 CalDAV 账号密码，用于将飞书日历同步到本地设备日历。

**函数签名**
```csharp
Task<FeishuApiResult<GenerateCaldavConfSettingResult>?> GenerateCaldavConfSettingAsync(
    GenerateCaldavConfSettingRequest generateCaldavConfSettingRequest,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| generateCaldavConfSettingRequest | GenerateCaldavConfSettingRequest | ✅ | 生成 CalDAV 配置请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var request = new GenerateCaldavConfSettingRequest
{
    Password = "your_password"
};
var result = await api.GenerateCaldavConfSettingAsync(request);
Console.WriteLine($"CalDAV 配置: {result?.Data}");
```

---

### CreateExchangeBindingAsync
将 Exchange 账户绑定到飞书账户，支持 Exchange 日历的导入。

**函数签名**
```csharp
Task<FeishuApiResult<ExchangeBindingOopsResult>?> CreateExchangeBindingAsync(
    CreateExchangeBindingRequest createExchangeBindingRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| createExchangeBindingRequest | CreateExchangeBindingRequest | ✅ | Exchange 绑定请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var request = new CreateExchangeBindingRequest
{
    AdminAccount = "admin@example.com",
    ExchangeAccount = "user@example.com"
};
var result = await api.CreateExchangeBindingAsync(request);
```

---

### DeleteExchangeBindingAsync
解除 Exchange 账户与飞书账户的绑定。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteExchangeBindingAsync(
    string exchange_binding_id,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| exchange_binding_id | string | ✅ | Exchange 绑定的唯一标识 ID | ZW1haWxfYWRtaW5fZXhhbXBsZUBvdXRsb29rLmNvbSBl... |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

---

### GetExchangeBindingAsync
获取 Exchange 账户的绑定状态和日历同步状态。

**函数签名**
```csharp
Task<FeishuApiResult<ExchangeBindingOopsResult>?> GetExchangeBindingAsync(
    string exchange_binding_id,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| exchange_binding_id | string | ✅ | Exchange 绑定的唯一标识 ID | ZW1haWxfYWRtaW5fZXhhbXBsZUBvdXRsb29rLmNvbSBl... |
| user_id_type | string | ⚪ | 用户 ID 类型 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var result = await api.GetExchangeBindingAsync(
    exchange_binding_id: "ZW1haWxfYWRtaW5fZXhhbXBsZUBvdXRsb29rLmNvbSBl..."
);
Console.WriteLine($"绑定状态: {result?.Data}");
```