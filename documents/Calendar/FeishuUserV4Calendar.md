# 日历管理 - 用户令牌
**IFeishuUserV4Calendar**

## 功能描述
以用户身份调用日历 API，对日历资源进行管理。包括创建、删除、查询、更新、搜索日历，以及订阅/取消订阅日历和日历变更事件等功能。

## 参考文档
- [日历管理概述](https://open.feishu.cn/document/server-docs/calendar-v4/calendar/introduction)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateCalendarAsync | 创建共享日历 | 用户令牌 | POST |
| DeleteCalendarAsync | 删除共享日历 | 用户令牌 | DELETE |
| GetPrimaryCalendarAsync | 查询主日历信息 | 用户令牌 | POST |
| GetPrimarysCalendarAsync | 批量获取主日历信息 | 用户令牌 | POST |
| GetCalendarAsync | 查询日历信息 | 用户令牌 | GET |
| GetCalendarsAsync | 批量查询日历信息 | 用户令牌 | POST |
| GetFreebusyCalendarAsync | 查询主日历日程忙闲信息 | 用户令牌 | POST |
| GetFreebusyCalendarsAsync | 批量查询主日历日程忙闲信息 | 用户令牌 | POST |
| QueryCalendarsPageListAsync | 查询日历列表 | 用户令牌 | GET |
| UpdateCalendarAsync | 更新日历信息 | 用户令牌 | PATCH |
| SearchCalendarsPageListAsync | 搜索日历 | 用户令牌 | POST |
| SubscribeCalendarAsync | 订阅日历 | 用户令牌 | POST |
| UnSubscribeCalendarAsync | 取消订阅日历 | 用户令牌 | POST |
| SubscribeCalendarEventAsync | 订阅日历变更事件 | 用户令牌 | POST |
| UnSubscribeCalendarEventAsync | 取消订阅日历变更事件 | 用户令牌 | POST |

## 函数详细内容

### CreateCalendarAsync
以用户身份创建一个共享日历。

**函数签名**
```csharp
Task<FeishuApiResult<CalendarOopsResult>?> CreateCalendarAsync(
    CreateCalendarRequest createCalendarRequest,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| createCalendarRequest | CreateCalendarRequest | ✅ | 创建共享日历请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "calendar": {
      "calendar_id": "feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn",
      "summary": "测试日历"
    }
  }
}
```

**代码示例**
```csharp
var request = new CreateCalendarRequest
{
    Summary = "个人工作日历"
};
var result = await api.CreateCalendarAsync(request);
```

---

### DeleteCalendarAsync
以用户身份删除指定共享日历。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteCalendarAsync(
    string calendar_id,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var result = await api.DeleteCalendarAsync(
    calendar_id: "feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn"
);
```

---

### GetPrimaryCalendarAsync
获取当前用户的主日历信息。

**函数签名**
```csharp
Task<FeishuApiResult<GetPrimaryCalendarResult>?> GetPrimaryCalendarAsync(
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var result = await api.GetPrimaryCalendarAsync();
Console.WriteLine($"主日历: {result?.Data}");
```

---

### GetPrimarysCalendarAsync
根据 user id 列表批量查询指定用户的主日历信息。

**函数签名**
```csharp
Task<FeishuApiResult<GetPrimaryCalendarResult>?> GetPrimarysCalendarAsync(
    GetPrimarysCalendarRequest getPrimarysCalendarRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| getPrimarysCalendarRequest | GetPrimarysCalendarRequest | ✅ | 批量获取主日历信息请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var request = new GetPrimarysCalendarRequest
{
    UserIds = new[] { "ou_xxx", "ou_yyy" }
};
var result = await api.GetPrimarysCalendarAsync(request);
```

---

### GetCalendarAsync
以用户身份查询指定日历的信息。

**函数签名**
```csharp
Task<FeishuApiResult<CalendarInfo>?> GetCalendarAsync(
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
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var result = await api.GetCalendarAsync(
    calendar_id: "feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn"
);
```

---

### GetCalendarsAsync
批量查询指定日历的标题、描述、公开范围等信息。

**函数签名**
```csharp
Task<FeishuApiResult<GetCalendarsResult>?> GetCalendarsAsync(
    GetCalendarsRequest getCalendarsRequest,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| getCalendarsRequest | GetCalendarsRequest | ✅ | 批量获取日历信息请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var request = new GetCalendarsRequest
{
    CalendarIds = new[] { "feishu.cn_xxx1@group.calendar.feishu.cn" }
};
var result = await api.GetCalendarsAsync(request);
```

---

### GetFreebusyCalendarAsync
查询指定用户的主日历忙闲信息。

**函数签名**
```csharp
Task<FeishuApiResult<GetFreebusyCalendarResult>?> GetFreebusyCalendarAsync(
    GetFreebusyCalendarRequest getFreebusyCalendarRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| getFreebusyCalendarRequest | GetFreebusyCalendarRequest | ✅ | 获取空闲/忙碌日历信息的请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var request = new GetFreebusyCalendarRequest
{
    UserId = "ou_xxx",
    TimeMin = "2024-01-01T00:00:00+08:00",
    TimeMax = "2024-01-02T00:00:00+08:00"
};
var result = await api.GetFreebusyCalendarAsync(request);
```

---

### GetFreebusyCalendarsAsync
批量查询指定用户的主日历在指定时间段内的忙碌时间段信息。

**函数签名**
```csharp
Task<FeishuApiResult<GetFreebusyCalendarsResult>?> GetFreebusyCalendarsAsync(
    GetFreebusyCalendarsRequest getFreebusyCalendarsRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| getFreebusyCalendarsRequest | GetFreebusyCalendarsRequest | ✅ | 批量查询主日历日程忙闲信息请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var request = new GetFreebusyCalendarsRequest
{
    UserIds = new[] { "ou_xxx", "ou_yyy" },
    TimeMin = "2024-01-01T00:00:00+08:00",
    TimeMax = "2024-01-02T00:00:00+08:00"
};
var result = await api.GetFreebusyCalendarsAsync(request);
```

---

### QueryCalendarsPageListAsync
分页查询当前用户的日历列表。支持增量同步。

**函数签名**
```csharp
Task<FeishuApiResult<QueryCalendarsResult>?> QueryCalendarsPageListAsync(
    int page_size = 500,
    string? page_token = null,
    string? sync_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| page_size | int | ⚪ | 分页大小 | 500 |
| page_token | string | ⚪ | 分页标记，首次查询不填 | - |
| sync_token | string | ⚪ | 增量同步标记 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var result = await api.QueryCalendarsPageListAsync(page_size: 500);
Console.WriteLine($"日历数量: {result?.Data?.Items?.Count}");
```

---

### UpdateCalendarAsync
以用户身份修改指定日历的标题、描述、公开范围等信息。

**函数签名**
```csharp
Task<FeishuApiResult<CalendarOopsResult>?> UpdateCalendarAsync(
    string calendar_id,
    UpdateCalendarRequest updateCalendarRequest,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| updateCalendarRequest | UpdateCalendarRequest | ✅ | 更新日历请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var request = new UpdateCalendarRequest
{
    Summary = "更新后的日历名称"
};
var result = await api.UpdateCalendarAsync(
    calendar_id: "feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn",
    updateCalendarRequest: request
);
```

---

### SearchCalendarsPageListAsync
通过关键字搜索日历。

**函数签名**
```csharp
Task<FeishuApiPageListResult<CalendarInfo>?> SearchCalendarsPageListAsync(
    SearchCalendarsRequest searchCalendarsRequest,
    int page_size = 500,
    string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| searchCalendarsRequest | SearchCalendarsRequest | ✅ | 搜索日历请求体 | - |
| page_size | int | ⚪ | 分页大小 | 500 |
| page_token | string | ⚪ | 分页标记 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var request = new SearchCalendarsRequest
{
    Query = "团队"
};
var result = await api.SearchCalendarsPageListAsync(request);
```

---

### SubscribeCalendarAsync
以用户身份订阅指定的日历。

**函数签名**
```csharp
Task<FeishuApiResult<CalendarOopsResult>?> SubscribeCalendarAsync(
    string calendar_id,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var result = await api.SubscribeCalendarAsync(
    calendar_id: "feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn"
);
```

---

### UnSubscribeCalendarAsync
以用户身份取消指定日历的订阅状态。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UnSubscribeCalendarAsync(
    string calendar_id,
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var result = await api.UnSubscribeCalendarAsync(
    calendar_id: "feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn"
);
```

---

### SubscribeCalendarEventAsync
为当前用户身份订阅日历变更事件。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> SubscribeCalendarEventAsync(
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success"
}
```

**代码示例**
```csharp
var result = await api.SubscribeCalendarEventAsync();
Console.WriteLine("日历变更事件订阅成功");
```

---

### UnSubscribeCalendarEventAsync
为当前用户身份取消订阅日历变更事件。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UnSubscribeCalendarEventAsync(
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**代码示例**
```csharp
var result = await api.UnSubscribeCalendarEventAsync();
Console.WriteLine("日历变更事件取消订阅成功");
```