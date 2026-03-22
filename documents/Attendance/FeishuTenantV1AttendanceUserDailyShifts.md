# 考勤排班管理接口 - FeishuTenantV1AttendanceUserDailyShifts

## 接口名称
**考勤排班管理** - (FeishuTenantV1AttendanceUserDailyShifts)

## 功能描述
排班表是用来描述考勤组内人员每天按哪个班次进行上班。目前排班表支持按x月y日对一位或多位人员进行排班。当用户的排班数据不存在时会进行创建，当用户的排班数据存在时会按照入参信息进行修改。

## 参考文档
- [飞书开放平台 - 考勤排班文档](https://open.feishu.cn/document/server-docs/attendance-v1/user_daily_shift/batch_create)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| BatchCreateUserDailyShiftAsync | 创建或修改排班表 | 租户令牌 | POST |
| QueryUserDailyShiftAsync | 查询排班表 | 租户令牌 | POST |
| BatchCreateTempUserDailyShiftAsync | 创建或修改临时排班 | 租户令牌 | POST |

---

## 函数详细内容

### BatchCreateUserDailyShiftAsync - 创建或修改排班表

创建或修改排班表，为指定人员在指定日期安排班次。

#### 函数签名

```csharp
Task<FeishuApiResult<UserDailyShiftResult>?> BatchCreateUserDailyShiftAsync(
    [Body] UserDailyShiftsRequest userDailyShiftRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| userDailyShiftRequest | UserDailyShiftsRequest | ✅ | 创建或修改排班表请求体 | - |
| ├─ user_daily_shifts | UserDailyShiftItem[] | ✅ | 排班数据列表 | - |
| ├─ ├─ user_id | string | ✅ | 用户ID | "ou_abc123" |
| ├─ ├─ date | int | ✅ | 排班日期，格式yyyyMMdd | 20240115 |
| ├─ ├─ shift_id | string | ✅ | 班次ID | "6919358778597097404" |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "success_count": 2,
    "fail_count": 0
  }
}
```

#### 说明
- 每人每天只能在一个考勤组中
- 如果排班数据已存在，会进行更新
- 如果排班数据不存在，会进行创建

#### 代码示例

```csharp
// 为员工批量排班
var request = new UserDailyShiftsRequest
{
    UserDailyShifts = new[]
    {
        new UserDailyShiftItem
        {
            UserId = "ou_abc123",
            Date = 20240115,
            ShiftId = "6919358778597097404"  // 早班
        },
        new UserDailyShiftItem
        {
            UserId = "ou_abc123",
            Date = 20240116,
            ShiftId = "6919358778597097404"  // 早班
        },
        new UserDailyShiftItem
        {
            UserId = "ou_def456",
            Date = 20240115,
            ShiftId = "6919358778597097405"  // 晚班
        }
    }
};

var result = await feishuClient.BatchCreateUserDailyShiftAsync(request);
if (result?.Code == 0)
{
    Console.WriteLine($"排班成功: {result.Data?.SuccessCount}人");
}
```

---

### QueryUserDailyShiftAsync - 查询排班表

支持查询多个用户的排班情况。可以通过返回结果中的group_id查询考勤组，shift_id查询班次。

#### 函数签名

```csharp
Task<FeishuApiResult<UserDailyShiftsQueryResult>?> QueryUserDailyShiftAsync(
    [Body] QueryUserDailyShiftsRequest userDailyShiftRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| userDailyShiftRequest | QueryUserDailyShiftsRequest | ✅ | 查询排班表请求体 | - |
| ├─ user_ids | string[] | ✅ | 用户ID列表 | ["ou_xxx"] |
| ├─ date_from | int | ✅ | 查询起始日期 | 20240101 |
| ├─ date_to | int | ✅ | 查询结束日期 | 20240131 |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user_daily_shifts": [
      {
        "user_id": "ou_abc123",
        "date": 20240115,
        "shift_id": "6919358778597097404",
        "group_id": "6919358128597097404"
      },
      {
        "user_id": "ou_abc123",
        "date": 20240116,
        "shift_id": "6919358778597097404",
        "group_id": "6919358128597097404"
      }
    ]
  }
}
```

#### 说明
- 查询的时间跨度不能超过30天
- 返回的是用户维度的排班结果

#### 代码示例

```csharp
// 查询用户的排班情况
var request = new QueryUserDailyShiftsRequest
{
    UserIds = new[] { "ou_abc123" },
    DateFrom = 20240101,
    DateTo = 20240131
};

var result = await feishuClient.QueryUserDailyShiftAsync(request);
if (result?.Code == 0)
{
    foreach (var shift in result.Data?.UserDailyShifts ?? [])
    {
        Console.WriteLine($"日期: {shift.Date}, 班次ID: {shift.ShiftId}");
    }
}
```

---

### BatchCreateTempUserDailyShiftAsync - 创建或修改临时排班

可在排班表上创建或修改临时班次，并用于排班。目前支持按日期对一位或多位人员进行排临时班次。

#### 函数签名

```csharp
Task<FeishuApiResult<UserTmpDailyShiftResult>?> BatchCreateTempUserDailyShiftAsync(
    [Body] UserTmpDailyShiftRequest userTmpDailyShiftRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| userTmpDailyShiftRequest | UserTmpDailyShiftRequest | ✅ | 创建或修改临时排班请求体 | - |
| ├─ user_tmp_shifts | UserTmpShiftItem[] | ✅ | 临时排班数据列表 | - |
| ├─ ├─ user_id | string | ✅ | 用户ID | "ou_abc123" |
| ├─ ├─ date | int | ✅ | 排班日期 | 20240115 |
| ├─ ├─ shift_name | string | ✅ | 临时班次名称 | "临时加班班" |
| ├─ ├─ punch_time_rules | PunchTimeRule[] | ✅ | 打卡时间规则 | - |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "success_count": 1,
    "fail_count": 0
  }
}
```

#### 说明
- 临时排班为付费功能，如需使用请联系飞书的客户经理
- 临时班次不需要预先创建，直接指定时间规则即可

#### 代码示例

```csharp
// 为员工安排临时排班（如加班）
var request = new UserTmpDailyShiftRequest
{
    UserTmpShifts = new[]
    {
        new UserTmpShiftItem
        {
            UserId = "ou_abc123",
            Date = 20240115,
            ShiftName = "周末加班班",
            PunchTimeRules = new[]
            {
                new PunchTimeRule
                {
                    PunchType = 1,
                    PunchTime = "09:00"
                },
                new PunchTimeRule
                {
                    PunchType = 2,
                    PunchTime = "18:00"
                }
            }
        }
    }
};

var result = await feishuClient.BatchCreateTempUserDailyShiftAsync(request);
if (result?.Code == 0)
{
    Console.WriteLine("临时排班创建成功");
}
```
