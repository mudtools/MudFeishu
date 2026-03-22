# 考勤打卡管理接口 - FeishuTenantV1AttendanceUserFlows

## 接口名称
**考勤打卡管理** - (FeishuTenantV1AttendanceUserFlows)

## 功能描述
打卡信息管理，可以导入、查询、删除员工的打卡流水记录。导入后会根据员工所在的考勤组班次规则，计算最终的打卡状态与结果。

## 参考文档
- [飞书开放平台 - 考勤打卡文档](https://open.feishu.cn/document/server-docs/attendance-v1/user_task/batch_create)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| BatchCreateUserFlowAsync | 导入打卡流水 | 租户令牌 | POST |
| GetUserFlowAsync | 获取打卡流水记录 | 租户令牌 | GET |
| QueryUserFlowAsync | 批量查询打卡流水 | 租户令牌 | POST |
| BatchDelUserFlowAsync | 删除打卡流水 | 租户令牌 | POST |
| QueryUserTaskAsync | 查询打卡结果 | 租户令牌 | POST |

---

## 函数详细内容

### BatchCreateUserFlowAsync - 导入打卡流水

导入员工的打卡流水记录。导入后，会根据员工所在的考勤组班次规则，计算最终的打卡状态与结果。

#### 函数签名

```csharp
Task<FeishuApiResult<UserFlowsBatchCreateResult>?> BatchCreateUserFlowAsync(
    [Body] UserFlowsBatchCreateRequest userFlowsBatchCreateRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| userFlowsBatchCreateRequest | UserFlowsBatchCreateRequest | ✅ | 导入打卡流水请求体 | - |
| ├─ user_flows | UserFlowItem[] | ✅ | 打卡流水列表 | - |
| ├─ ├─ user_id | string | ✅ | 用户ID | "ou_abc123" |
| ├─ ├─ punch_time | int | ✅ | 打卡时间，Unix时间戳 | 1705312800 |
| ├─ ├─ punch_type | int | ✅ | 打卡类型：1上班/2下班 | 1 |
| ├─ ├─ location_name | string | ⚪ | 打卡地点名称 | "公司正门" |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "success_count": 2,
    "fail_count": 0,
    "user_flow_ids": ["6708236686834352397", "6708236686834352398"]
  }
}
```

#### 说明
- 导入的打卡记录可在打卡管理-打卡记录中查询
- 导入后会自动计算考勤状态（正常、迟到、早退等）

#### 代码示例

```csharp
// 批量导入员工打卡记录
var request = new UserFlowsBatchCreateRequest
{
    UserFlows = new[]
    {
        new UserFlowItem
        {
            UserId = "ou_abc123",
            PunchTime = 1705312800,  // 2024-01-15 09:00:00
            PunchType = 1,  // 上班
            LocationName = "公司正门"
        },
        new UserFlowItem
        {
            UserId = "ou_abc123",
            PunchTime = 1705345200,  // 2024-01-15 18:00:00
            PunchType = 2,  // 下班
            LocationName = "公司正门"
        }
    }
};

var result = await feishuClient.BatchCreateUserFlowAsync(request);
if (result?.Code == 0)
{
    Console.WriteLine($"成功导入: {result.Data?.SuccessCount}条记录");
}
```

---

### GetUserFlowAsync - 获取打卡流水记录

通过打卡记录ID获取用户的打卡流水记录。

#### 函数签名

```csharp
Task<FeishuApiResult<UserFlowInfo>?> GetUserFlowAsync(
    [Path] string user_flow_id,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| user_flow_id | string | ✅ | 打卡流水记录ID | "6708236686834352397" |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user_flow_id": "6708236686834352397",
    "user_id": "ou_abc123",
    "user_name": "张三",
    "punch_time": 1705312800,
    "punch_type": 1,
    "location_name": "公司正门",
    "punch_result": "normal"
  }
}
```

#### 代码示例

```csharp
// 获取单条打卡记录详情
var result = await feishuClient.GetUserFlowAsync("6708236686834352397");
if (result?.Code == 0)
{
    Console.WriteLine($"员工: {result.Data?.UserName}");
    Console.WriteLine($"打卡时间: {DateTimeOffset.FromUnixTimeSeconds(result.Data?.PunchTime ?? 0):yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"打卡结果: {result.Data?.PunchResult}");
}
```

---

### QueryUserFlowAsync - 批量查询打卡流水

通过打卡记录ID批量查询打卡流水记录。

#### 函数签名

```csharp
Task<FeishuApiResult<UserFlowsQueryResult>?> QueryUserFlowAsync(
    [Body] UserFlowsQueryRequest userFlowsQueryRequest,
    [Query("include_terminated_user")] bool? include_terminated_user = null,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| userFlowsQueryRequest | UserFlowsQueryRequest | ✅ | 批量查询打卡流水请求体 | - |
| ├─ user_flow_ids | string[] | ✅ | 打卡流水ID列表 | ["6708236686834352397"] |
| include_terminated_user | bool? | ⚪ | 是否包含已离职用户 | true |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user_flows": [
      {
        "user_flow_id": "6708236686834352397",
        "user_id": "ou_abc123",
        "punch_time": 1705312800,
        "punch_type": 1
      }
    ]
  }
}
```

#### 代码示例

```csharp
// 批量查询打卡记录
var request = new UserFlowsQueryRequest
{
    UserFlowIds = new[] { "6708236686834352397", "6708236686834352398" }
};

var result = await feishuClient.QueryUserFlowAsync(request, includeTerminatedUser: true);
if (result?.Code == 0)
{
    foreach (var flow in result.Data?.UserFlows ?? [])
    {
        Console.WriteLine($"记录ID: {flow.UserFlowId}, 用户: {flow.UserId}");
    }
}
```

---

### BatchDelUserFlowAsync - 删除打卡流水

删除员工从开放平台导入的打卡记录。删除后会重新计算打卡记录对应考勤任务结果。

#### 函数签名

```csharp
Task<FeishuApiResult<UserFlowsBatchDelResult>?> BatchDelUserFlowAsync(
    [Body] UserFlowsBatchDelRequest userFlowsBatchDelRequest,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| userFlowsBatchDelRequest | UserFlowsBatchDelRequest | ✅ | 删除打卡流水请求体 | - |
| ├─ user_flow_ids | string[] | ✅ | 要删除的打卡流水ID列表 | ["6708236686834352397"] |

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
- 只能删除通过开放平台导入的打卡记录
- 删除后会重新计算考勤结果

#### 代码示例

```csharp
// 批量删除导入的打卡记录
var request = new UserFlowsBatchDelRequest
{
    UserFlowIds = new[] { "6708236686834352397", "6708236686834352398" }
};

var result = await feishuClient.BatchDelUserFlowAsync(request);
if (result?.Code == 0)
{
    Console.WriteLine($"成功删除: {result.Data?.SuccessCount}条记录");
}
```

---

### QueryUserTaskAsync - 查询打卡结果

获取企业内员工的实际打卡结果。

#### 函数签名

```csharp
Task<FeishuApiResult<UserTasksQueryResult>?> QueryUserTaskAsync(
    [Body] UserTasksQueryRequest userTasksQueryRequest,
    [Query("ignore_invalid_users")] bool? ignore_invalid_users = null,
    [Query("include_terminated_user")] bool? include_terminated_user = null,
    [Query("employee_type")] string employee_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| userTasksQueryRequest | UserTasksQueryRequest | ✅ | 查询打卡结果请求体 | - |
| ├─ user_ids | string[] | ✅ | 用户ID列表 | ["ou_xxx"] |
| ├─ check_date_from | int | ✅ | 查询起始日期 | 20240101 |
| ├─ check_date_to | int | ✅ | 查询结束日期 | 20240131 |
| ignore_invalid_users | bool? | ⚪ | 是否忽略无效用户 | true |
| include_terminated_user | bool? | ⚪ | 是否包含已离职用户 | false |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user_tasks": [
      {
        "user_id": "ou_abc123",
        "date": 20240115,
        "shift_name": "标准班",
        "punch_records": [
          {
            "punch_type": 1,
            "punch_time": 1705312800,
            "punch_result": "normal"
          }
        ]
      }
    ]
  }
}
```

#### 代码示例

```csharp
// 查询员工的打卡结果
var request = new UserTasksQueryRequest
{
    UserIds = new[] { "ou_abc123" },
    CheckDateFrom = 20240101,
    CheckDateTo = 20240131
};

var result = await feishuClient.QueryUserTaskAsync(
    request,
    ignoreInvalidUsers: true,
    includeTerminatedUser: false
);

if (result?.Code == 0)
{
    foreach (var task in result.Data?.UserTasks ?? [])
    {
        Console.WriteLine($"日期: {task.Date}, 班次: {task.ShiftName}");
        foreach (var record in task.PunchRecords ?? [])
        {
            Console.WriteLine($"  打卡类型: {record.PunchType}, 结果: {record.PunchResult}");
        }
    }
}
```
