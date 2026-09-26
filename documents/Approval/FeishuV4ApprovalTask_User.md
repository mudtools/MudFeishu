# 审批任务（用户令牌） - FeishuV4ApprovalTask_User

## 接口名称

审批任务接口（用户令牌） -（`IFeishuUserV4ApprovalTask`）

## 功能描述

以**用户身份**操作审批任务，对应飞书审批中心「待办/已办」页面的操作能力。支持：

- 加签审批任务（tasks/add_sign）
- 转交审批任务（tasks/forward）
- 获取审批任务列表（GET /tasks）
- 同意审批任务（tasks/pass）
- 拒绝审批任务（tasks/refuse）
- 退回审批任务（tasks/rollback）
- 订阅/退订审批任务状态变更事件（tasks/subscription）

> 注意：飞书同时存在租户令牌版本的同语义接口（approve/reject/transfer/resubmit），见 `IFeishuTenantV4ApprovalTask` 文档。两套接口路径不同、参数略有差异，均为官方正式 API。

## 参考文档

- [飞书官方文档 - 审批任务 API](https://open.feishu.cn/document/server-docs/approval-v4/task/introduction)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| `AddSignApprovalTaskAsync` | 加签审批任务 | 用户令牌 | POST |
| `ForwardApprovalTaskAsync` | 转交审批任务 | 用户令牌 | POST |
| `GetApprovalTaskPageListAsync` | 获取审批任务列表 | 用户令牌 | GET |
| `PassApprovalTaskAsync` | 同意审批任务 | 用户令牌 | POST |
| `RefuseApprovalTaskAsync` | 拒绝审批任务 | 用户令牌 | POST |
| `RollbackApprovalTaskAsync` | 退回审批任务 | 用户令牌 | POST |
| `SubscribeTaskStatusEventAsync` | 订阅审批任务状态变更事件 | 用户令牌 | POST |
| `UnsubscribeTaskStatusEventAsync` | 退订审批任务状态变更事件 | 用户令牌 | DELETE |

## 函数详细内容

### AddSignApprovalTaskAsync - 加签审批任务

#### 函数签名

```csharp
[Post("/open-apis/approval/v4/tasks/add_sign")]
Task<FeishuNullDataApiResult?> AddSignApprovalTaskAsync(
    [Body] AddSignTaskRequest addSignTaskRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**请求体（`AddSignTaskRequest`）字段：**

| JSON 字段 | 类型 | 必填 | 描述 |
| --------- | ---- | ---- | ---- |
| `instance_code` | `string` | 是 | 审批实例 code |
| `task_id` | `string` | 是 | 任务 id |
| `comment` | `string` | 否 | 审批意见，500 字符内 |
| `add_sign_user_ids` | `string[]` | 是 | 被加签人 id，与 user_id_type 一致 |
| `add_sign_type` | `int` | 是 | 1：前加签；2：后加签；3：并加签 |
| `approval_method` | `int` | 否 | 仅前/后加签填写：1：或签；2：会签 |

### ForwardApprovalTaskAsync - 转交审批任务

#### 函数签名

```csharp
[Post("/open-apis/approval/v4/tasks/forward")]
Task<FeishuNullDataApiResult?> ForwardApprovalTaskAsync(
    [Body] ForwardTaskRequest forwardTaskRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**请求体（`ForwardTaskRequest`）字段：** `instance_code`（审批实例 Code）、`task_id`（审批任务 ID）、`transfer_user_id`（被转交人的用户 ID）、`comment`（审批意见，可选）。

### GetApprovalTaskPageListAsync - 获取审批任务列表

#### 函数签名

```csharp
[Get("/open-apis/approval/v4/tasks")]
Task<FeishuApiResult<GetApprovalTaskPageListResult>?> GetApprovalTaskPageListAsync(
    [Query("page_size")] int page_size = Consts.PageSize_100,
    [Query("page_token")] string? page_token = null,
    [Query("topic")] string? topic = null,
    [Query("locale")] string? locale = null,
    [Query("definition_code")] string? definition_code = null,
    [Query("start_timestamp")] string? start_timestamp = null,
    [Query("end_timestamp")] string? end_timestamp = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 参数

| 参数名 | 必填 | 类型 | 描述 |
| ------ | ---- | ---- | ---- |
| `page_size` | ⚪ | `int` | 分页大小，默认：100 |
| `page_token` | ⚪ | `string` | 分页标记 |
| `topic` | ⚪ | `string` | 任务分组：1：待办审批；2：已办审批 |
| `locale` | ⚪ | `string` | 语言 |
| `definition_code` | ⚪ | `string` | 审批定义 Code，用于筛选 |
| `start_timestamp` / `end_timestamp` | ⚪ | `string` | 毫秒时间戳区间 |
| `user_id_type` | ⚪ | `string` | 用户 ID 类型 |

#### 响应

`GetApprovalTaskPageListResult`：继承 `ApiPageListResult`（`has_more`/`page_token`），附加 `tasks`（`TaskItemInfo[]`）与 `count`（第一页返回，≥100 时返回 99）。

`TaskItemInfo` 字段：`topic`、`user_id`、`title`、`status`、`instance_status`、`definition_code`、`initiator`、`initiator_name`、`task_id`、`instance_code`、`definition_group_id`、`definition_group_name`、`definition_name`、`summaries`（`ApprovalSummaryPairInfo[]`）、`instance_external_id`、`task_external_id`、`support_api_operate`、`link`。

### PassApprovalTaskAsync - 同意审批任务

#### 函数签名

```csharp
[Post("/open-apis/approval/v4/tasks/pass")]
Task<FeishuNullDataApiResult?> PassApprovalTaskAsync(
    [Body] PassTaskRequest passTaskRequest,
    CancellationToken cancellationToken = default);
```

**请求体（`PassTaskRequest`）字段：** `instance_code`（必填）、`task_id`（必填）、`form`（表单数据，仅该节点需录入的字段，可选）、`comment`（审批意见，可选）。注意该接口无查询参数。

### RefuseApprovalTaskAsync - 拒绝审批任务

#### 函数签名

```csharp
[Post("/open-apis/approval/v4/tasks/refuse")]
Task<FeishuNullDataApiResult?> RefuseApprovalTaskAsync(
    [Body] RefuseTaskRequest refuseTaskRequest,
    CancellationToken cancellationToken = default);
```

**请求体（`RefuseTaskRequest`）字段：** `instance_code`（必填）、`task_id`（必填）、`comment`（审批意见，500 字符内，可选）。注意该接口无查询参数。

### RollbackApprovalTaskAsync - 退回审批任务

#### 函数签名

```csharp
[Post("/open-apis/approval/v4/tasks/rollback")]
Task<FeishuNullDataApiResult?> RollbackApprovalTaskAsync(
    [Body] RollbackTaskRequest rollbackTaskRequest,
    CancellationToken cancellationToken = default);
```

**请求体（`RollbackTaskRequest`）字段：** `instance_code`（必填）、`task_id`（必填）、`comment`（可选）、`node_ids`（退回到的节点 id 列表，发起节点为 `START`，可选）。注意该接口无查询参数。

### SubscribeTaskStatusEventAsync / UnsubscribeTaskStatusEventAsync

#### 函数签名

```csharp
[Post("/open-apis/approval/v4/tasks/subscription")]
Task<FeishuNullDataApiResult?> SubscribeTaskStatusEventAsync(
    [Body] TaskSubscriptionRequest subscriptionRequest,
    CancellationToken cancellationToken = default);

[Delete("/open-apis/approval/v4/tasks/subscription")]
Task<FeishuNullDataApiResult?> UnsubscribeTaskStatusEventAsync(
    [Query("subscription_type")] string subscription_type,
    CancellationToken cancellationToken = default);
```

#### 说明

- `subscription_type` 可选值：`INVOLVED_APPROVAL`（参与审批订阅）、`MANAGED_APPROVAL`（管理审批订阅）。
- 退订接口为 DELETE 方法，`subscription_type` 通过查询参数传递，需与订阅时一致。
