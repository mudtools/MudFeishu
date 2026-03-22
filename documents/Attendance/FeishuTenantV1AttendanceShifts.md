# 考勤班次管理接口 - FeishuTenantV1AttendanceShifts

## 接口名称
**考勤班次管理** - (FeishuTenantV1AttendanceShifts)

## 功能描述
考勤班次是描述一次考勤任务时间规则的统称，比如一天打多少次卡，每次卡的上下班时间，晚到多长时间算迟到，晚到多长时间算缺卡等。支持创建、删除、查询班次信息。

## 参考文档
- [飞书开放平台 - 考勤班次文档](https://open.feishu.cn/document/server-docs/attendance-v1/shift/create)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateShiftAsync | 创建考勤班次 | 租户令牌 | POST |
| DeleteShiftByIdAsync | 删除考勤班次 | 租户令牌 | DELETE |
| GetShiftByIdAsync | 获取班次详情 | 租户令牌 | GET |
| GetShiftByNameAsync | 按名称查询班次 | 租户令牌 | POST |
| GetShiftsPageListAsync | 分页查询所有班次 | 租户令牌 | GET |

---

## 函数详细内容

### CreateShiftAsync - 创建考勤班次

创建考勤班次，设置上下班时间、迟到规则、休息规则等。

#### 函数签名

```csharp
Task<FeishuApiResult<CreateAttendanceShiftsResult>?> CreateShiftAsync(
    [Body] CreateAttendanceShiftsRequest createAttendanceShiftsRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| createAttendanceShiftsRequest | CreateAttendanceShiftsRequest | ✅ | 创建班次请求体 | - |
| ├─ shift_name | string | ✅ | 班次名称 | "早班" |
| ├─ punch_times | int | ✅ | 一天内打卡次数 | 2 |
| ├─ punch_time_rules | PunchTimeRule[] | ✅ | 打卡时间规则 | - |
| ├─ rest_time_rules | RestTimeRule[] | ⚪ | 休息规则 | - |
| ├─ late_rules | LateRule[] | ⚪ | 迟到规则 | - |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "shift_id": "6919358778597097404",
    "shift_name": "早班"
  }
}
```

#### 代码示例

```csharp
// 创建一个朝九晚六的班次
var request = new CreateAttendanceShiftsRequest
{
    ShiftName = "标准班",
    PunchTimes = 2,
    PunchTimeRules = new[]
    {
        new PunchTimeRule
        {
            PunchType = 1,  // 上班
            PunchTime = "09:00",
            AllowLateMinutes = 15
        },
        new PunchTimeRule
        {
            PunchType = 2,  // 下班
            PunchTime = "18:00",
            AllowEarlyMinutes = 15
        }
    }
};

var result = await feishuClient.CreateShiftAsync(request);
if (result?.Code == 0)
{
    Console.WriteLine($"班次创建成功，ID: {result.Data?.ShiftId}");
}
```

---

### DeleteShiftByIdAsync - 删除考勤班次

通过班次ID删除班次，对应功能为假勤设置-班次设置班次列表中操作栏的删除按钮。

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> DeleteShiftByIdAsync(
    [Path] string shift_id,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| shift_id | string | ✅ | 班次ID | "6919358778597097404" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success"
}
```

#### 说明
- 如果班次已被考勤组使用，则无法删除
- 删除后不可恢复

#### 代码示例

```csharp
// 删除指定ID的班次
var result = await feishuClient.DeleteShiftByIdAsync("6919358778597097404");
if (result?.Code == 0)
{
    Console.WriteLine("班次删除成功");
}
```

---

### GetShiftByIdAsync - 获取班次详情

通过班次ID获取班次详情，对应功能为假勤设置-班次设置班次列表中的具体班次。

#### 函数签名

```csharp
Task<FeishuApiResult<GetAttendanceShiftsResult>?> GetShiftByIdAsync(
    [Path] string shift_id,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| shift_id | string | ✅ | 班次ID | "6919358778597097404" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "shift_id": "6919358778597097404",
    "shift_name": "早班",
    "punch_times": 2,
    "punch_time_rules": [
      {
        "punch_type": 1,
        "punch_time": "09:00",
        "allow_late_minutes": 15
      },
      {
        "punch_type": 2,
        "punch_time": "18:00",
        "allow_early_minutes": 15
      }
    ]
  }
}
```

#### 代码示例

```csharp
// 获取班次详情
var result = await feishuClient.GetShiftByIdAsync("6919358778597097404");
if (result?.Code == 0)
{
    Console.WriteLine($"班次名称: {result.Data?.ShiftName}");
    Console.WriteLine($"打卡次数: {result.Data?.PunchTimes}");
}
```

---

### GetShiftByNameAsync - 按名称查询班次

按名称查询班次，对应功能为飞书人事管理后台中假勤设置-班次配置中的搜索班次名称功能。

#### 函数签名

```csharp
Task<FeishuApiResult<GetAttendanceShiftsResult>?> GetShiftByNameAsync(
    [Query("shift_name")] string shift_name,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| shift_name | string | ✅ | 班次名称 | "早班" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "shift_id": "6919358778597097404",
    "shift_name": "早班",
    "punch_times": 2
  }
}
```

#### 代码示例

```csharp
// 搜索名为"早班"的班次
var result = await feishuClient.GetShiftByNameAsync("早班");
if (result?.Code == 0)
{
    Console.WriteLine($"找到班次: {result.Data?.ShiftName}, ID: {result.Data?.ShiftId}");
}
```

---

### GetShiftsPageListAsync - 分页查询所有班次

分页查询所有班次，对应功能为飞书人事管理后台中假勤设置-班次配置中的翻页查询所有班次功能。

#### 函数签名

```csharp
Task<FeishuApiResult<GetAttendanceShiftsPageListResult>?> GetShiftsPageListAsync(
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| page_size | int | ⚪ | 分页大小，默认10 | 10 |
| page_token | string | ⚪ | 分页标记 | "xxx" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "shifts": [
      {
        "shift_id": "6919358778597097404",
        "shift_name": "早班",
        "punch_times": 2
      }
    ],
    "page_token": "next_page_token",
    "has_more": true
  }
}
```

#### 代码示例

```csharp
// 分页获取所有班次
string? pageToken = null;
do
{
    var result = await feishuClient.GetShiftsPageListAsync(10, pageToken);
    if (result?.Code == 0)
    {
        foreach (var shift in result.Data?.Shifts ?? [])
        {
            Console.WriteLine($"班次: {shift.ShiftName}");
        }
        pageToken = result.Data?.PageToken;
    }
} while (!string.IsNullOrEmpty(pageToken));
```
