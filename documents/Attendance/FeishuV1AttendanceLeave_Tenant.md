# 考勤休假管理接口 - FeishuV1AttendanceLeave_Tenant

## 接口名称
**考勤休假管理** - (FeishuV1AttendanceLeave_Tenant)

## 功能描述
考勤休假管理，包含休假过期时间获取发放记录、休假发放记录接口。支持通过过期时间获取发放记录，以及修改发放记录的发放数量和失效日期。

## 参考文档
- [飞书开放平台 - 考勤休假管理文档](https://open.feishu.cn/document/server-docs/attendance-v1/leave_employ_expire_record/get)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| GetLeaveEmployExpireRecordAsync | 通过过期时间获取发放记录 | 租户令牌 | GET |
| ModifyLeaveAccrualRecordAsync | 修改发放记录 | 租户令牌 | PATCH |

---

## 函数详细内容

### GetLeaveEmployExpireRecordAsync - 通过过期时间获取发放记录

通过过期时间获取发放记录，只能获取到对应时间段过期的发放记录。

#### 函数签名

```csharp
Task<FeishuApiResult<LeaveEmployExpireRecordsResult>?> GetLeaveEmployExpireRecordAsync(
    [Body] LeaveEmployExpireRecordsRequest leaveEmployExpireRecordsRequest,
    [Path] string leave_id,
    [Query("user_id_type")] string user_id_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| leaveEmployExpireRecordsRequest | LeaveEmployExpireRecordsRequest | ✅ | 通过过期时间获取发放记录请求体 | - |
| leave_id | string | ✅ | 假期类型ID | "7111688079785723436" |
| user_id_type | string | ⚪ | 员工ID类型：employee_id/employee_no/open_id | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "records": [
      {
        "user_id": "ou_abc123",
        "leave_id": "7111688079785723436",
        "grant_quantity": 10000,
        "expire_date": "2024-12-31",
        "created_at": 1704067200
      }
    ]
  }
}
```

#### 说明
- 只能查询到指定过期时间段内的发放记录
- 适用于统计即将过期或已过期的假期余额

#### 代码示例

```csharp
// 查询年假在2024年12月31日到期的发放记录
var request = new LeaveEmployExpireRecordsRequest
{
    ExpireDateFrom = "2024-12-01",
    ExpireDateTo = "2024-12-31"
};

var result = await feishuClient.GetLeaveEmployExpireRecordAsync(
    request, 
    "7111688079785723436"  // 年假类型ID
);

if (result?.Code == 0)
{
    foreach (var record in result.Data?.Records ?? [])
    {
        Console.WriteLine($"员工: {record.UserId}, 发放数量: {record.GrantQuantity}");
    }
}
```

---

### ModifyLeaveAccrualRecordAsync - 修改发放记录

更新发放记录的发放数量和失效日期，对应假勤管理-休假管理-发放记录。

#### 函数签名

```csharp
Task<FeishuApiResult<LeaveAccrualRecordResult>?> ModifyLeaveAccrualRecordAsync(
    [Body] LeaveAccrualRecordRequest leaveAccrualRecordRequest,
    [Path] string leave_id,
    [Query("user_id_type")] string user_id_type = "employee_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| leaveAccrualRecordRequest | LeaveAccrualRecordRequest | ✅ | 修改发放记录请求体 | - |
| leave_id | string | ✅ | 假期类型ID | "7111688079785723436" |
| user_id_type | string | ⚪ | 员工ID类型 | "employee_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "record_id": "record_xxx",
    "updated_at": 1704067200
  }
}
```

#### 说明
- 用于调整员工假期余额
- 可修改发放数量和失效日期

#### 代码示例

```csharp
// 修改员工年假发放记录
var request = new LeaveAccrualRecordRequest
{
    UserId = "ou_abc123",
    RecordId = "record_xxx",
    GrantQuantity = 50000,  // 修改为5天（单位为分钟或按企业配置）
    ExpireDate = "2025-12-31"
};

var result = await feishuClient.ModifyLeaveAccrualRecordAsync(
    request,
    "7111688079785723436"
);

if (result?.Code == 0)
{
    Console.WriteLine("发放记录修改成功");
}
```
