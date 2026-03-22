# 考勤补卡管理接口 - FeishuTenantV1AttendanceRemedys

## 接口名称
**考勤补卡管理** - (FeishuTenantV1AttendanceRemedys)

## 功能描述
对于只使用飞书考勤系统而未使用飞书审批系统的企业，可以通过该接口，将在三方审批系统中补卡审批数据，同步到飞书考勤系统中。支持创建补卡审批、查询可补卡时间以及查询补卡记录。

## 参考文档
- [飞书开放平台 - 考勤补卡文档](https://open.feishu.cn/document/server-docs/attendance-v1/user_task_remedy/create)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateUserTaskRemedyAsync | 创建补卡审批 | 租户令牌 | POST |
| QueryUserAllowedRemedysUserTaskRemedyAsync | 获取可补卡时间 | 租户令牌 | POST |
| QueryUserTaskRemedyAsync | 查询补卡记录 | 租户令牌 | POST |

---

## 函数详细内容

### CreateUserTaskRemedyAsync - 创建补卡审批

将在三方审批系统中发起的补卡审批数据，写入到飞书考勤系统中，状态为审批中。写入后可通过 `ProcessApprovalInfoAsync` 进行状态更新。

#### 函数签名

```csharp
Task<FeishuApiResult<AttendanceRemedysResult>?> CreateUserTaskRemedyAsync(
    [Body] AttendanceRemedysRequest attendanceRemedysRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| attendanceRemedysRequest | AttendanceRemedysRequest | ✅ | 通知补卡审批发起请求体 | - |
| ├─ user_id | string | ✅ | 用户ID | "ou_abc123" |
| ├─ remedy_date | int | ✅ | 补卡日期，格式yyyyMMdd | 20240115 |
| ├─ remedy_time | string | ✅ | 补卡时间，格式HH:mm | "09:00" |
| ├─ punch_type | int | ✅ | 打卡类型：1上班/2下班 | 1 |
| ├─ reason | string | ⚪ | 补卡原因 | "忘记打卡" |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "remedy_id": "6737202939523236113"
  }
}
```

#### 代码示例

```csharp
// 创建补卡审批
var request = new AttendanceRemedysRequest
{
    UserId = "ou_abc123",
    RemedyDate = 20240115,
    RemedyTime = "09:00",
    PunchType = 1,  // 上班卡
    Reason = "忘记打卡"
};

var result = await feishuClient.CreateUserTaskRemedyAsync(request);
if (result?.Code == 0)
{
    Console.WriteLine($"补卡审批创建成功，ID: {result.Data?.RemedyId}");
}
```

---

### QueryUserAllowedRemedysUserTaskRemedyAsync - 获取可补卡时间

获取用户某天可以补的第几次上/下班卡的时间。

#### 函数签名

```csharp
Task<FeishuApiResult<QueryUserAllowedRemedysResult>?> QueryUserAllowedRemedysUserTaskRemedyAsync(
    [Body] AllowedRemedysRequest allowedRemedysRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| allowedRemedysRequest | AllowedRemedysRequest | ✅ | 获取可补卡时间请求体 | - |
| ├─ user_id | string | ✅ | 用户ID | "ou_abc123" |
| ├─ check_date | int | ✅ | 查询日期，格式yyyyMMdd | 20240115 |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "allowed_remedys": [
      {
        "punch_type": 1,
        "punch_time": "09:00",
        "can_remedy": true
      },
      {
        "punch_type": 2,
        "punch_time": "18:00",
        "can_remedy": true
      }
    ]
  }
}
```

#### 代码示例

```csharp
// 查询用户某天的可补卡时间
var request = new AllowedRemedysRequest
{
    UserId = "ou_abc123",
    CheckDate = 20240115
};

var result = await feishuClient.QueryUserAllowedRemedysUserTaskRemedyAsync(request);
if (result?.Code == 0)
{
    foreach (var remedy in result.Data?.AllowedRemedys ?? [])
    {
        var type = remedy.PunchType == 1 ? "上班" : "下班";
        Console.WriteLine($"{type}卡时间: {remedy.PunchTime}, 是否可补: {remedy.CanRemedy}");
    }
}
```

---

### QueryUserTaskRemedyAsync - 查询补卡记录

获取用户补卡记录，补卡记录是用户通过审批的方式，在某一次上/下班的打卡时间范围内，补充一条打卡记录，用以修正用户的考勤结果。

#### 函数签名

```csharp
Task<FeishuApiResult<QueryUserRemedysResult>?> QueryUserTaskRemedyAsync(
    [Body] QueryUserRemedysRequest queryUserRemedysRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| queryUserRemedysRequest | QueryUserRemedysRequest | ✅ | 获取补卡记录请求体 | - |
| ├─ user_ids | string[] | ✅ | 用户ID列表 | ["ou_abc123"] |
| ├─ check_date_from | int | ✅ | 查询起始日期 | 20240101 |
| ├─ check_date_to | int | ✅ | 查询结束日期 | 20240131 |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "remedy_list": [
      {
        "remedy_id": "6737202939523236113",
        "user_id": "ou_abc123",
        "remedy_date": 20240115,
        "remedy_time": "09:00",
        "punch_type": 1,
        "status": 2,
        "reason": "忘记打卡"
      }
    ]
  }
}
```

#### 代码示例

```csharp
// 查询用户的补卡记录
var request = new QueryUserRemedysRequest
{
    UserIds = new[] { "ou_abc123" },
    CheckDateFrom = 20240101,
    CheckDateTo = 20240131
};

var result = await feishuClient.QueryUserTaskRemedyAsync(request);
if (result?.Code == 0)
{
    foreach (var remedy in result.Data?.RemedyList ?? [])
    {
        Console.WriteLine($"补卡日期: {remedy.RemedyDate}, 时间: {remedy.RemedyTime}");
    }
}
```
