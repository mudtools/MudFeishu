# 审批实例（用户令牌） - FeishuV4ApprovalInstance_User

## 接口名称

审批实例接口（用户令牌） -（`IFeishuUserV4ApprovalInstance`）

## 功能描述

以**用户身份**操作审批实例，与飞书审批中心「已发起」「抄送我」等页面能力对应。支持：

- 抄送审批实例（add_cc）
- 获取单个审批实例详情（detail）
- 获取用户已发起审批列表（initiated）
- 撤回当前用户提交的审批实例（recall）
- 发送催办消息（remind）
- 订阅/退订审批实例状态变更事件（subscription / unsubscription）

## 参考文档

- [飞书官方文档 - 审批实例 API](https://open.feishu.cn/document/server-docs/approval-v4/instance/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| `AddCcInstanceAsync` | 抄送审批实例 | 用户令牌 | POST |
| `GetInstanceDetailAsync` | 获取单个审批实例详情 | 用户令牌 | GET |
| `GetInitiatedInstancePageListAsync` | 获取用户已发起审批列表 | 用户令牌 | GET |
| `RecallInstanceAsync` | 撤回审批实例 | 用户令牌 | POST |
| `RemindInstanceAsync` | 发送催办消息 | 用户令牌 | POST |
| `SubscribeInstanceStatusEventAsync` | 订阅审批实例状态变更事件 | 用户令牌 | POST |
| `UnsubscribeInstanceStatusEventAsync` | 退订审批实例状态变更事件 | 用户令牌 | DELETE |

## 函数详细内容

### AddCcInstanceAsync - 抄送审批实例

#### 函数签名

```csharp
[Post("/open-apis/approval/v4/instances/add_cc")]
Task<FeishuNullDataApiResult?> AddCcInstanceAsync(
    [Body] AddCcInstanceRequest addCcInstanceRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 参数

| 参数名 | 必填 | 类型 | 描述 |
| ------ | ---- | ---- | ---- |
| `addCcInstanceRequest` | ✅ | `AddCcInstanceRequest` | 抄送审批实例请求体 |
| `user_id_type` | ⚪ | `string` | 用户 ID 类型，默认 `open_id` |

**请求体（`AddCcInstanceRequest`）字段：**

| JSON 字段 | 类型 | 必填 | 描述 |
| --------- | ---- | ---- | ---- |
| `instance_code` | `string` | 是 | 审批实例 Code |
| `cc_user_ids` | `string[]` | 是 | 抄送人的用户 ID 列表 |
| `comment` | `string` | 否 | 抄送留言，不超过 500 字 |

#### 响应

无 data 数据（`FeishuNullDataApiResult`）。

### GetInstanceDetailAsync - 获取单个审批实例详情

#### 函数签名

```csharp
[Get("/open-apis/approval/v4/instances/detail")]
Task<FeishuApiResult<InstanceDetailResult>?> GetInstanceDetailAsync(
    [Query("instance_code")] string instance_code,
    [Query("locale")] string? locale = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 参数

| 参数名 | 必填 | 类型 | 描述 |
| ------ | ---- | ---- | ---- |
| `instance_code` | ✅ | `string` | 审批实例 Code |
| `locale` | ⚪ | `string` | 语言，默认为审批定义配置的默认语言 |
| `user_id_type` | ⚪ | `string` | 用户 ID 类型 |

#### 响应

`InstanceDetailResult` 主要字段：`definition_name`、`start_time`、`end_time`、`user_id`、`serial_number`、`department_id`、`status`、`form`、`tasks`（`InstanceDetailTaskInfo[]`）、`comments`（`InstanceDetailCommentInfo[]`）、`operation_records`（`InstanceDetailTimelineInfo[]`）、`definition_code`、`reverted`、`instance_code`、`current_nodes`（`InstanceCurrentNodeInfo[]`）。

**成功响应示例（节选）：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "definition_name": "请假",
    "start_time": "1564590532967",
    "end_time": "0",
    "user_id": "f3ta757q",
    "serial_number": "202102060002",
    "status": "PENDING",
    "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
    "tasks": [
      {
        "id": "6992353208872969234",
        "user_id": "f7cb567e",
        "status": "PENDING",
        "node_id": "46e6d96cfa756980907209209ec03b64",
        "node_name": "直属上级",
        "type": "AND",
        "start_time": "1564590532967",
        "end_time": "0"
      }
    ],
    "current_nodes": [
      {
        "node_id": "46e6d96cfa756980907209209ec03b64",
        "node_name": "直属上级",
        "type": "AND",
        "approvers": [{ "task_id": "6992353208872969234", "user_id": "f7cb567e" }]
      }
    ]
  }
}
```

#### 说明

- 该接口与租户令牌的「获取单个审批实例详情」（`GET /instances/{instance_id}`）返回结构不同：本接口（UAT 版）返回 `tasks/comments/operation_records/current_nodes` 字段，因此 SDK 使用独立的 `InstanceDetail*Info` 类型。

### GetInitiatedInstancePageListAsync - 获取用户已发起审批列表

#### 函数签名

```csharp
[Get("/open-apis/approval/v4/instances/initiated")]
Task<FeishuApiResult<GetInitiatedInstancePageListResult>?> GetInitiatedInstancePageListAsync(
    [Query("page_size")] int page_size = Consts.PageSize_10,
    [Query("page_token")] string? page_token = null,
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
| `page_size` | ⚪ | `int` | 分页大小，默认：10 |
| `page_token` | ⚪ | `string` | 分页标记 |
| `locale` | ⚪ | `string` | 语言 |
| `definition_code` | ⚪ | `string` | 审批定义 Code，用于筛选 |
| `start_timestamp` / `end_timestamp` | ⚪ | `string` | 毫秒时间戳区间 |
| `user_id_type` | ⚪ | `string` | 用户 ID 类型 |

#### 响应

`GetInitiatedInstancePageListResult`：继承 `ApiPageListResult`（`has_more`/`page_token`），附加 `instances`（`InitiatedInstanceInfo[]`）与 `count`（第一页返回，≥100 时返回 99）。

`InitiatedInstanceInfo` 字段：`instance_status`、`definition_code`、`initiator`、`initiator_name`、`instance_code`、`definition_group_id`、`definition_group_name`、`definition_name`、`summaries`（`ApprovalSummaryPairInfo[]`）、`instance_external_id`、`link`。

### RecallInstanceAsync - 撤回审批实例

#### 函数签名

```csharp
[Post("/open-apis/approval/v4/instances/recall")]
Task<FeishuNullDataApiResult?> RecallInstanceAsync(
    [Body] RecallInstanceRequest recallInstanceRequest,
    CancellationToken cancellationToken = default);
```

#### 说明

- 请求体 `RecallInstanceRequest` 仅含 `instance_code`。
- 只能撤回**当前用户身份提交**的审批实例；管理员撤回任意提交人的实例请用租户接口 `CancelInstanceAsync`（`POST /instances/cancel`）。

### RemindInstanceAsync - 发送催办消息

#### 函数签名

```csharp
[Post("/open-apis/approval/v4/instances/remind")]
Task<FeishuNullDataApiResult?> RemindInstanceAsync(
    [Body] RemindInstanceRequest remindInstanceRequest,
    CancellationToken cancellationToken = default);
```

**请求体（`RemindInstanceRequest`）字段：**

| JSON 字段 | 类型 | 必填 | 描述 |
| --------- | ---- | ---- | ---- |
| `instance_code` | `string` | 是 | 审批实例 Code |
| `task_ids` | `string[]` | 是 | 被催办的任务 ID |
| `comment` | `string` | 否 | 评论，500 字符内 |

### SubscribeInstanceStatusEventAsync / UnsubscribeInstanceStatusEventAsync

#### 函数签名

```csharp
[Post("/open-apis/approval/v4/instances/subscription")]
Task<FeishuNullDataApiResult?> SubscribeInstanceStatusEventAsync(
    [Body] InstanceSubscriptionRequest subscriptionRequest,
    CancellationToken cancellationToken = default);

[Delete("/open-apis/approval/v4/instances/subscription")]
Task<FeishuNullDataApiResult?> UnsubscribeInstanceStatusEventAsync(
    [Query("subscription_type")] string subscription_type,
    CancellationToken cancellationToken = default);
```

#### 说明

- `subscription_type` 可选值：`INVOLVED_APPROVAL`（参与审批订阅）、`MANAGED_APPROVAL`（管理审批订阅）。
- 退订接口为 DELETE 方法，`subscription_type` 通过查询参数传递（选填，不传表示取消所有类别的订阅）。
