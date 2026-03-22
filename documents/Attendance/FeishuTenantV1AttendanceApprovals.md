# 考勤审批管理接口 - FeishuTenantV1AttendanceApprovals

## 接口名称
**三方系统假勤审批管理** - (FeishuTenantV1AttendanceApprovals)

## 功能描述
用于管理三方系统假勤审批的请假、加班、外出和出差四种审批数据。支持获取员工在某段时间内的审批数据、将三方审批结果回写到飞书考勤系统，以及更新审批状态。

## 参考文档
- [飞书开放平台 - 考勤审批文档](https://open.feishu.cn/document/server-docs/attendance-v1/user_approval/query)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| QueryUserApprovalAsync | 获取员工审批数据 | 租户令牌 | POST |
| CreateUserApprovalAsync | 回写三方审批结果 | 租户令牌 | POST |
| ProcessApprovalInfoAsync | 更新审批状态 | 租户令牌 | POST |

---

## 函数详细内容

### QueryUserApprovalAsync - 获取员工审批数据

获取员工在某段时间内的请假、加班、外出和出差四种审批数据。

#### 函数签名

```csharp
Task<FeishuApiResult<QueryAttendanceApprovalsResult>?> QueryUserApprovalAsync(
    [Body] QueryAttendanceApprovalsRequest queryAttendanceApprovalsRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| queryAttendanceApprovalsRequest | QueryAttendanceApprovalsRequest | ✅ | 获取审批数据请求体 | - |
| ├─ user_ids | string[] | ✅ | 员工ID列表 | ["ou_xxx"] |
| ├─ check_date_from | int | ✅ | 查询起始日期，格式yyyyMMdd | 20240101 |
| ├─ check_date_to | int | ✅ | 查询结束日期，与起始日期间隔不超过30天 | 20240131 |
| ├─ check_date_type | string | ⚪ | 查询时间类型：PeriodTime/CreateTime | "PeriodTime" |
| ├─ status | int? | ⚪ | 查询状态：0待审批/1未通过/2已通过/3已撤回/4已撤销 | 2 |
| ├─ check_time_from | string | ⚪ | 查询起始时间戳（灰度中） | "1566641088" |
| ├─ check_time_to | string | ⚪ | 查询结束时间戳（灰度中） | "1592561088" |
| employee_type | string | ⚪ | 员工ID类型：employee_id/employee_no | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "approval_list": [
      {
        "user_id": "ou_xxx",
        "approval_type": "leave",
        "status": 2,
        "start_time": 1704067200,
        "end_time": 1704153600,
        "duration": 28800
      }
    ]
  }
}
```

#### 说明
- 传入的日期不能超过当天+1天
- 请假、加班仅支持查询已通过和已撤回状态
- 外出、出差支持查询所有状态

#### 代码示例

```csharp
// 获取员工2024年1月的请假审批数据
var request = new QueryAttendanceApprovalsRequest
{
    UserIds = new[] { "ou_abc123" },
    CheckDateFrom = 20240101,
    CheckDateTo = 20240131,
    Status = 2  // 已通过
};

var result = await feishuClient.QueryUserApprovalAsync(request);
if (result?.Code == 0)
{
    foreach (var approval in result.Data?.ApprovalList ?? [])
    {
        Console.WriteLine($"审批类型: {approval.ApprovalType}, 状态: {approval.Status}");
    }
}
```

---

### CreateUserApprovalAsync - 回写三方审批结果

对于只使用飞书考勤系统而未使用飞书审批系统的企业，可以通过本接口将三方审批结果数据回写到飞书考勤系统中。

#### 函数签名

```csharp
Task<FeishuApiResult<CreateUserApprovalResult>?> CreateUserApprovalAsync(
    [Body] CreateUserApprovalRequest writeApprovalsDataRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| writeApprovalsDataRequest | CreateUserApprovalRequest | ✅ | 写入审批结果请求体 | - |
| ├─ user_approval | UserAttendanceApprovalData | ⚪ | 审批信息 | - |
| employee_type | string | ⚪ | 员工ID类型：employee_id/employee_no | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "approval_id": "6737202939523236113"
  }
}
```

#### 说明
- 适用于企业使用第三方审批系统的场景
- 回写后可在飞书考勤系统中查看审批数据

#### 代码示例

```csharp
// 将三方系统的请假审批结果回写到飞书
var request = new CreateUserApprovalRequest
{
    UserApproval = new UserAttendanceApprovalData
    {
        UserId = "ou_abc123",
        ApprovalType = "leave",
        StartTime = 1704067200,
        EndTime = 1704153600,
        Duration = 28800
    }
};

var result = await feishuClient.CreateUserApprovalAsync(request);
if (result?.Code == 0)
{
    Console.WriteLine($"审批回写成功，ID: {result.Data?.ApprovalId}");
}
```

---

### ProcessApprovalInfoAsync - 更新审批状态

通过该接口更新写入飞书考勤系统中的三方系统审批状态，例如请假、加班、外出、出差、补卡等审批，状态包括通过、不通过、撤销等。

#### 函数签名

```csharp
Task<FeishuApiResult<UpdateAttendanceApprovalInfoResult>?> ProcessApprovalInfoAsync(
    [Body] UpdateApprovalInfosRequest updateApprovalInfosRequest,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| updateApprovalInfosRequest | UpdateApprovalInfosRequest | ✅ | 通知审批状态更新请求体 | - |
| ├─ approval_id | string | ✅ | 审批实例ID | "6737202939523236113" |
| ├─ approval_type | string | ✅ | 审批类型：leave/out/overtime/trip/remedy | "remedy" |
| └─ status | int | ✅ | 审批状态：1不通过/2通过/4撤销 | 2 |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "success": true
  }
}
```

#### 说明
- 请假、外出、加班、出差只支持传**撤销**状态
- 补卡支持传**不通过、通过和撤销**状态
- 审批ID通过 `CreateUserApprovalAsync` 获取

#### 代码示例

```csharp
// 更新补卡审批状态为已通过
var request = new UpdateApprovalInfosRequest
{
    ApprovalId = "6737202939523236113",
    ApprovalType = "remedy",
    Status = 2  // 已通过
};

var result = await feishuClient.ProcessApprovalInfoAsync(request);
if (result?.Code == 0)
{
    Console.WriteLine("审批状态更新成功");
}
```
