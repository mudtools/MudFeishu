# 考勤排班（用户令牌） - FeishuV1AttendanceUserDailyShifts_User

## 接口名称

考勤排班接口（用户令牌） -（`IFeishuUserV1AttendanceUserDailyShifts`）

## 功能描述

创建或修改临时排班：可在排班表上创建或修改临时班次，并用于排班。目前支持按日期对一位或多位人员进行排临时班次。

> 飞书考勤 `POST /user_daily_shifts/batch_create_temp` 同时支持租户令牌与用户令牌。租户令牌版本见 `IFeishuTenantV1AttendanceUserDailyShifts.BatchCreateTempUserDailyShiftAsync`（文档：[FeishuTenantV1AttendanceUserDailyShifts](./FeishuTenantV1AttendanceUserDailyShifts.md)），本接口为用户令牌版本。

## 参考文档

- [飞书官方文档 - 创建或更改临时排班](https://open.feishu.cn/api-explorer?from=op_doc_tab&apiName=batch_create_temp&project=attendance&resource=user_daily_shift&version=v1)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| `BatchCreateTempUserDailyShiftAsync` | 创建或修改临时排班 | 用户令牌 | POST |

## 函数详细内容

### BatchCreateTempUserDailyShiftAsync - 创建或修改临时排班

#### 函数签名

```csharp
[Post("/open-apis/attendance/v1/user_daily_shifts/batch_create_temp")]
Task<FeishuApiResult<UserTmpDailyShiftResult>?> BatchCreateTempUserDailyShiftAsync(
   [Body] UserTmpDailyShiftRequest userTmpDailyShiftRequest,
   [Query("employee_type")] string employee_type = Consts.User_Id_Type,
   CancellationToken cancellationToken = default);
```

#### 认证

**用户令牌**（`UserAccessToken`）

#### 参数

| 参数名 | 必填 | 类型 | 描述 |
| ------ | ---- | ---- | ---- |
| `userTmpDailyShiftRequest` | ✅ | `UserTmpDailyShiftRequest` | 创建或修改临时排班请求体（`user_tmp_daily_shifts`：group_id/user_id/date/shift_name/punch_time_simple_rules、`operator_id`） |
| `employee_type` | ⚪ | `string` | 员工 ID 类型，默认 `open_id` |
| `cancellationToken` | ⚪ | `CancellationToken` | 取消操作令牌对象 |

#### 响应

`UserTmpDailyShiftResult`：`user_tmp_daily_shifts`（`UserTmpDailyShift[]`）。

#### 说明

- 临时排班为付费功能，如需使用请联系飞书的客户经理。
- 注意：如果返回 code=0 且 msg 不为空，表示临时排班部分成功。msg 返回 {人员：[日期，日期]} 格式，代表人员在排班日期下临时排班未成功，一般是考勤组 id 与人员不匹配造成的。
