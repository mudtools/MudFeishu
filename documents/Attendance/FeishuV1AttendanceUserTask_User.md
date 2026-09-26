# 考勤打卡结果（用户令牌） - FeishuV1AttendanceUserTask_User

## 接口名称

考勤打卡结果接口（用户令牌） -（`IFeishuUserV1AttendanceUserTask`）

## 功能描述

获取企业内员工的实际打卡结果，包括打卡任务列表、打卡记录 id、用户信息、考勤组 ID、班次 ID、考勤记录、上/下班打卡结果、上/下班打卡时间、无效用户 ID 列表、没有权限用户 ID 列表。

> 飞书考勤 `POST /user_tasks/query` 同时支持租户令牌与用户令牌。租户令牌版本见 `IFeishuTenantV1AttendanceUserFlows.QueryUserTaskAsync`（文档：[FeishuTenantV1AttendanceUserFlows](./FeishuTenantV1AttendanceUserFlows.md)），本接口为用户令牌版本。

## 参考文档

- [飞书官方文档 - 查询打卡结果](https://open.feishu.cn/api-explorer?from=op_doc_tab&apiName=query&project=attendance&resource=user_task&version=v1)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| `QueryUserTaskAsync` | 查询打卡结果 | 用户令牌 | POST |

## 函数详细内容

### QueryUserTaskAsync - 查询打卡结果

#### 函数签名

```csharp
[Post("/open-apis/attendance/v1/user_tasks/query")]
Task<FeishuApiResult<UserTasksQueryResult>?> QueryUserTaskAsync(
   [Body] UserTasksQueryRequest userTasksQueryRequest,
   [Query("ignore_invalid_users")] bool? ignore_invalid_users = null,
   [Query("include_terminated_user")] bool? include_terminated_user = null,
   [Query("employee_type")] string employee_type = Consts.User_Id_Type,
   CancellationToken cancellationToken = default);
```

#### 认证

**用户令牌**（`UserAccessToken`）

#### 参数

| 参数名 | 必填 | 类型 | 描述 |
| ------ | ---- | ---- | ---- |
| `userTasksQueryRequest` | ✅ | `UserTasksQueryRequest` | 查询打卡结果请求体（`user_ids`、`check_date_from`、`check_date_to`、`need_overtime_result`） |
| `ignore_invalid_users` | ⚪ | `bool` | true：返回有效用户信息并告知无效用户；false：存在无效用户时报错 |
| `include_terminated_user` | ⚪ | `bool` | true：返回在职+离职用户数据；false：只返回在职或最近一个离职用户数据 |
| `employee_type` | ⚪ | `string` | 员工 ID 类型，默认 `open_id` |
| `cancellationToken` | ⚪ | `CancellationToken` | 取消操作令牌对象 |

#### 响应

`UserTasksQueryResult`：`user_task_results`（`UserTask[]`，含 `result_id`、`user_id`、`day`、`group_id`、`shift_id`、`records` 打卡明细）、`invalid_user_ids`、`unauthorized_user_ids`。

#### 说明

- 如果企业给员工设定的班次是上午 9 点和下午 6 点各打一次上下班卡，即使员工在这期间打了多次卡，该接口也只会返回 1 条记录。
- 如需获取打卡详细数据（打卡位置等），请使用查询打卡流水接口。
