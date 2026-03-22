# 考勤统计接口 - FeishuTenantV1AttendanceStats

## 接口名称
**考勤统计** - (FeishuTenantV1AttendanceStats)

## 功能描述
考勤统计接口支持开发者定制接口返回数据，让开发者可以只获取自己所关注的数据内容。支持更新统计设置、查询统计表头、查询统计数据等功能。

## 参考文档
- [飞书开放平台 - 考勤统计文档](https://open.feishu.cn/document/server-docs/attendance-v1/user_stats_data/attendance-statistic-reference)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| UpdateUserStatsViewAsync | 更新统计设置 | 租户令牌 | PUT |
| QueryUserStatsFieldAsync | 查询统计表头 | 租户令牌 | POST |
| QueryUserStatsViewAsync | 查询统计设置 | 租户令牌 | POST |
| QueryUserStatsDataAsync | 查询统计数据 | 租户令牌 | POST |

---

## 函数详细内容

### UpdateUserStatsViewAsync - 更新统计设置

更新开发者定制的日度统计或月度统计的统计报表表头设置信息。

#### 函数签名

```csharp
Task<FeishuApiResult<UserStatsViewsResult>?> UpdateUserStatsViewAsync(
    [Body] UserStatsViewsRequest userStatsViewsRequest,
    [Path] string user_stats_view_id,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| userStatsViewsRequest | UserStatsViewsRequest | ✅ | 更新统计设置请求体 | - |
| user_stats_view_id | string | ✅ | 用户视图ID | "TmpZNU5qTTJORFF6T1RnNU5UTTNOakV6TWl0dGIyNTBhQT09" |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "view_id": "TmpZNU5qTTJORFF6T1RnNU5UTTNOakV6TWl0dGIyNTBhQT09",
    "view_name": "月度考勤报表",
    "updated_at": 1704067200
  }
}
```

#### 代码示例

```csharp
// 更新统计视图设置
var request = new UserStatsViewsRequest
{
    ViewName = "月度考勤报表",
    Fields = new[] { "user_id", "user_name", "attendance_days", "late_times" }
};

var result = await feishuClient.UpdateUserStatsViewAsync(
    request,
    "TmpZNU5qTTJORFF6T1RnNU5UTTNOakV6TWl0dGIyNTBhQT09"
);

if (result?.Code == 0)
{
    Console.WriteLine("统计设置更新成功");
}
```

---

### QueryUserStatsFieldAsync - 查询统计表头

查询考勤统计支持的日度统计或月度统计的统计表头。

#### 函数签名

```csharp
Task<FeishuApiResult<QueryStatsFieldsResult>?> QueryUserStatsFieldAsync(
    [Body] QueryStatsFieldsRequest queryStatsFieldsRequest,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| queryStatsFieldsRequest | QueryStatsFieldsRequest | ✅ | 查询统计表头请求体 | - |
| ├─ stats_type | string | ✅ | 统计类型：day/month | "month" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "fields": [
      {
        "field_code": "user_id",
        "field_name": "用户ID",
        "field_type": "string"
      },
      {
        "field_code": "attendance_days",
        "field_name": "出勤天数",
        "field_type": "number"
      },
      {
        "field_code": "late_times",
        "field_name": "迟到次数",
        "field_type": "number"
      }
    ]
  }
}
```

#### 代码示例

```csharp
// 查询月度统计支持的表头字段
var request = new QueryStatsFieldsRequest
{
    StatsType = "month"
};

var result = await feishuClient.QueryUserStatsFieldAsync(request);
if (result?.Code == 0)
{
    foreach (var field in result.Data?.Fields ?? [])
    {
        Console.WriteLine($"字段: {field.FieldName} ({field.FieldCode})");
    }
}
```

---

### QueryUserStatsViewAsync - 查询统计设置

查询考勤统计支持的日度统计或月度统计的统计表头。报表的表头信息可以在考勤统计-报表中查询到具体的报表信息。

#### 函数签名

```csharp
Task<FeishuApiResult<UserStatsViewsResult>?> QueryUserStatsViewAsync(
    [Body] QueryStatsViewsRequest queryStatsFieldsRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| queryStatsFieldsRequest | QueryStatsViewsRequest | ✅ | 查询统计设置请求体 | - |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "views": [
      {
        "view_id": "TmpZNU5qTTJORFF6T1RnNU5UTTNOakV6TWl0dGIyNTBhQT09",
        "view_name": "月度考勤报表",
        "stats_type": "month",
        "fields": ["user_id", "user_name", "attendance_days"]
      }
    ]
  }
}
```

#### 代码示例

```csharp
// 查询已配置的统计视图
var request = new QueryStatsViewsRequest
{
    StatsType = "month"
};

var result = await feishuClient.QueryUserStatsViewAsync(request);
if (result?.Code == 0)
{
    foreach (var view in result.Data?.Views ?? [])
    {
        Console.WriteLine($"视图: {view.ViewName}, 类型: {view.StatsType}");
    }
}
```

---

### QueryUserStatsDataAsync - 查询统计数据

查询日度统计或月度统计的统计数据。字段包含基本信息、考勤组信息、出勤统计、异常统计、请假统计、加班统计、打卡时间、考勤结果和自定义字段。

#### 函数签名

```csharp
Task<FeishuApiResult<QueryStatsDatasResult>?> QueryUserStatsDataAsync(
    [Body] QueryStatsDatasRequest queryStatsDatasRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| queryStatsDatasRequest | QueryStatsDatasRequest | ✅ | 查询统计数据请求体 | - |
| ├─ user_ids | string[] | ✅ | 用户ID列表 | ["ou_xxx"] |
| ├─ start_date | int | ✅ | 起始日期，格式yyyyMMdd | 20240101 |
| ├─ end_date | int | ✅ | 结束日期，格式yyyyMMdd | 20240131 |
| ├─ stats_type | string | ✅ | 统计类型：day/month | "month" |
| ├─ view_id | string | ⚪ | 自定义视图ID | "xxx" |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "stats_datas": [
      {
        "user_id": "ou_abc123",
        "user_name": "张三",
        "attendance_days": 22,
        "late_times": 2,
        "leave_days": 1,
        "overtime_hours": 10
      }
    ]
  }
}
```

#### 代码示例

```csharp
// 查询员工月度考勤统计数据
var request = new QueryStatsDatasRequest
{
    UserIds = new[] { "ou_abc123", "ou_def456" },
    StartDate = 20240101,
    EndDate = 20240131,
    StatsType = "month"
};

var result = await feishuClient.QueryUserStatsDataAsync(request);
if (result?.Code == 0)
{
    foreach (var data in result.Data?.StatsDatas ?? [])
    {
        Console.WriteLine($"员工: {data.UserName}");
        Console.WriteLine($"出勤天数: {data.AttendanceDays}");
        Console.WriteLine($"迟到次数: {data.LateTimes}");
    }
}
```
