# 审批任务操作接口 -（FeishuV4ApprovalTask_Tenant）

## 接口名称

审批任务操作接口 -（`IFeishuTenantV4ApprovalTask`）

## 功能描述

该接口用于操作原生审批实例中的审批任务。原生审批实例的流程中包含多个审批节点，审批节点内根据设置的审批人情况，会生成审批任务（一个审批人对应一个审批任务），使用原生审批任务 API，可以同意、拒绝、转交以及退回审批任务。

## 参考文档

- [飞书官方文档 - 审批任务](https://open.feishu.cn/document/server-docs/approval-v4/task/introduction)

## 函数列表

| 函数名称                | 功能描述         | 认证方式 | HTTP 方法 |
| ----------------------- | ---------------- | -------- | --------- |
| `AgreeApprovalAsync`    | 同意审批任务     | 租户令牌 | POST      |
| `RejectApprovalAsync`   | 拒绝审批任务     | 租户令牌 | POST      |
| `TransferApprovalAsync` | 转交审批任务     | 租户令牌 | POST      |
| `RollbackApprovalAsync` | 退回审批任务     | 租户令牌 | POST      |
| `InstancesAddSignAsync` | 审批任务加签     | 租户令牌 | POST      |
| `ResubmitApprovalAsync` | 重新提交审批任务 | 租户令牌 | POST      |

## 函数详细内容

### 同意审批任务

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> AgreeApprovalAsync(
    [Body] AgreeApprovalTasksRequest agreeApprovalTasksRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                      | 必填 | 类型                        | 描述                          |
| --------------------------- | ---- | --------------------------- | ----------------------------- |
| `agreeApprovalTasksRequest` | ✅   | `AgreeApprovalTasksRequest` | 同意审批任务请求体            |
| `user_id_type`              | ⚪   | `string`                    | 用户 ID 类型，默认：`open_id` |
| `cancellationToken`         | ⚪   | `CancellationToken`         | 取消操作令牌对象              |

**请求体示例：**

```json
{
  "task_id": "123456789",
  "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
  "user_id": "ou_7dab8a3d3dfcd10xxx",
  "comment": "同意，请尽快安排",
  "form": [
    {
      "id": "opinion",
      "type": "textarea",
      "value": "审批通过"
    }
  ]
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok"
}
```

#### 说明

- 对单个审批任务进行同意操作
- 同意后审批流程会流转到下一个审批人
- 如果是最后一个审批人，则审批流程结束并审批通过

#### 代码示例

```csharp
// 使用租户令牌同意审批任务
public class ApprovalTaskService
{
    private readonly IFeishuTenantV4ApprovalTask _taskClient;

    public ApprovalTaskService(IFeishuTenantV4ApprovalTask taskClient)
    {
        _taskClient = taskClient;
    }

    public async Task AgreeApprovalAsync()
    {
        var request = new AgreeApprovalTasksRequest
        {
            TaskId = "123456789",
            InstanceCode = "6A123516-FB88-470D-A428-9AF58B71B3C0",
            UserId = "ou_7dab8a3d3dfcd10xxx",
            Comment = "同意，请尽快安排"
        };

        var result = await _taskClient.AgreeApprovalAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine("审批已通过");
        }
    }
}
```

---

### 拒绝审批任务

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> RejectApprovalAsync(
    [Body] RejectApprovalTaskRequest rejectApprovalTaskRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                      | 必填 | 类型                        | 描述                          |
| --------------------------- | ---- | --------------------------- | ----------------------------- |
| `rejectApprovalTaskRequest` | ✅   | `RejectApprovalTaskRequest` | 拒绝审批任务请求体            |
| `user_id_type`              | ⚪   | `string`                    | 用户 ID 类型，默认：`open_id` |
| `cancellationToken`         | ⚪   | `CancellationToken`         | 取消操作令牌对象              |

**请求体示例：**

```json
{
  "task_id": "123456789",
  "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
  "user_id": "ou_7dab8a3d3dfcd10xxx",
  "comment": "信息不完整，请补充"
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok"
}
```

#### 说明

- 对单个审批任务进行拒绝操作
- 拒绝后审批流程结束，审批实例状态变为已拒绝

#### 代码示例

```csharp
// 使用租户令牌拒绝审批任务
public class ApprovalTaskService
{
    private readonly IFeishuTenantV4ApprovalTask _taskClient;

    public ApprovalTaskService(IFeishuTenantV4ApprovalTask taskClient)
    {
        _taskClient = taskClient;
    }

    public async Task RejectApprovalAsync()
    {
        var request = new RejectApprovalTaskRequest
        {
            TaskId = "123456789",
            InstanceCode = "6A123516-FB88-470D-A428-9AF58B71B3C0",
            UserId = "ou_7dab8a3d3dfcd10xxx",
            Comment = "信息不完整，请补充部门经理签字"
        };

        var result = await _taskClient.RejectApprovalAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine("审批已拒绝");
        }
    }
}
```

---

### 转交审批任务

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> TransferApprovalAsync(
    [Body] TransferApprovalTasksRequest transferApprovalTasksRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                         | 必填 | 类型                           | 描述                          |
| ------------------------------ | ---- | ------------------------------ | ----------------------------- |
| `transferApprovalTasksRequest` | ✅   | `TransferApprovalTasksRequest` | 转交审批任务请求体            |
| `user_id_type`                 | ⚪   | `string`                       | 用户 ID 类型，默认：`open_id` |
| `cancellationToken`            | ⚪   | `CancellationToken`            | 取消操作令牌对象              |

**请求体示例：**

```json
{
  "task_id": "123456789",
  "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
  "user_id": "ou_7dab8a3d3dfcd10xxx",
  "transfer_user_id": "ou_8eab9b4e4egde21yyy",
  "comment": "请协助审批"
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok"
}
```

#### 说明

- 对单个审批任务进行转交操作
- 转交后审批流程流转给被转交人
- 原审批人不再参与该审批任务的审批

#### 代码示例

```csharp
// 使用租户令牌转交审批任务
public class ApprovalTaskService
{
    private readonly IFeishuTenantV4ApprovalTask _taskClient;

    public ApprovalTaskService(IFeishuTenantV4ApprovalTask taskClient)
    {
        _taskClient = taskClient;
    }

    public async Task TransferApprovalAsync()
    {
        var request = new TransferApprovalTasksRequest
        {
            TaskId = "123456789",
            InstanceCode = "6A123516-FB88-470D-A428-9AF58B71B3C0",
            UserId = "ou_7dab8a3d3dfcd10xxx",
            TransferUserId = "ou_8eab9b4e4egde21yyy",
            Comment = "此申请涉及技术评估，请技术经理协助审批"
        };

        var result = await _taskClient.TransferApprovalAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine("审批已转交");
        }
    }
}
```

---

### 退回审批任务

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> RollbackApprovalAsync(
    [Body] RollbackApprovalInstancesRequest rollbackApprovalInstancesRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                             | 必填 | 类型                               | 描述                          |
| ---------------------------------- | ---- | ---------------------------------- | ----------------------------- |
| `rollbackApprovalInstancesRequest` | ✅   | `RollbackApprovalInstancesRequest` | 退回审批任务请求体            |
| `user_id_type`                     | ⚪   | `string`                           | 用户 ID 类型，默认：`open_id` |
| `cancellationToken`                | ⚪   | `CancellationToken`                | 取消操作令牌对象              |

**请求体示例：**

```json
{
  "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
  "user_id": "ou_7dab8a3d3dfcd10xxx",
  "node_id": "APPROVAL_1",
  "comment": "请修改金额后重新提交"
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok"
}
```

#### 说明

- 从当前审批任务，退回到已审批的一个或多个任务节点
- 退回后，已审批节点重新生成审批任务
- 适用于需要重新审批之前环节的场景

#### 代码示例

```csharp
// 使用租户令牌退回审批任务
public class ApprovalTaskService
{
    private readonly IFeishuTenantV4ApprovalTask _taskClient;

    public ApprovalTaskService(IFeishuTenantV4ApprovalTask taskClient)
    {
        _taskClient = taskClient;
    }

    public async Task RollbackApprovalAsync()
    {
        var request = new RollbackApprovalInstancesRequest
        {
            InstanceCode = "6A123516-FB88-470D-A428-9AF58B71B3C0",
            UserId = "ou_7dab8a3d3dfcd10xxx",
            NodeId = "START",
            Comment = "申请信息有误，请修改后重新提交"
        };

        var result = await _taskClient.RollbackApprovalAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine("审批已退回");
        }
    }
}
```

---

### 审批任务加签

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> InstancesAddSignAsync(
    [Body] InstancesAddSignRequest instancesAddSignRequest,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                    | 必填 | 类型                      | 描述               |
| ------------------------- | ---- | ------------------------- | ------------------ |
| `instancesAddSignRequest` | ✅   | `InstancesAddSignRequest` | 审批任务加签请求体 |
| `cancellationToken`       | ⚪   | `CancellationToken`       | 取消操作令牌对象   |

**请求体示例：**

```json
{
  "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
  "task_id": "123456789",
  "user_id": "ou_7dab8a3d3dfcd10xxx",
  "sign_user_ids": ["ou_9fab0c5f5fhif32zzz"],
  "sign_type": "BEFORE",
  "comment": "需要法务审核"
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok"
}
```

#### 说明

- 对单个审批任务进行加签操作
- 加签类型：
  - `BEFORE`：前加签，在当前审批人之前增加审批人
  - `AFTER`：后加签，在当前审批人之后增加审批人

#### 代码示例

```csharp
// 使用租户令牌审批任务加签
public class ApprovalTaskService
{
    private readonly IFeishuTenantV4ApprovalTask _taskClient;

    public ApprovalTaskService(IFeishuTenantV4ApprovalTask taskClient)
    {
        _taskClient = taskClient;
    }

    public async Task AddSignAsync()
    {
        var request = new InstancesAddSignRequest
        {
            InstanceCode = "6A123516-FB88-470D-A428-9AF58B71B3C0",
            TaskId = "123456789",
            UserId = "ou_7dab8a3d3dfcd10xxx",
            SignUserIds = new List<string> { "ou_9fab0c5f5fhif32zzz" },
            SignType = "BEFORE",
            Comment = "此合同涉及重要条款，需要法务部门先审核"
        };

        var result = await _taskClient.InstancesAddSignAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine("加签成功");
        }
    }
}
```

---

### 重新提交审批任务

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> ResubmitApprovalAsync(
    [Body] ResubmitApprovalRequest instancesAddSignRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                    | 必填 | 类型                      | 描述                          |
| ------------------------- | ---- | ------------------------- | ----------------------------- |
| `instancesAddSignRequest` | ✅   | `ResubmitApprovalRequest` | 重新提交审批任务请求体        |
| `user_id_type`            | ⚪   | `string`                  | 用户 ID 类型，默认：`open_id` |
| `cancellationToken`       | ⚪   | `CancellationToken`       | 取消操作令牌对象              |

**请求体示例：**

```json
{
  "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
  "user_id": "ou_7dab8a3d3dfcd10xxx",
  "form": [
    {
      "id": "amount",
      "type": "number",
      "value": "5000"
    }
  ],
  "comment": "已按意见修改"
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok"
}
```

#### 说明

- 对于退回到发起人的审批任务进行重新发起操作
- 发起后审批流程会流转到下一个审批人

#### 代码示例

```csharp
// 使用租户令牌重新提交审批任务
public class ApprovalTaskService
{
    private readonly IFeishuTenantV4ApprovalTask _taskClient;

    public ApprovalTaskService(IFeishuTenantV4ApprovalTask taskClient)
    {
        _taskClient = taskClient;
    }

    public async Task ResubmitApprovalAsync()
    {
        var request = new ResubmitApprovalRequest
        {
            InstanceCode = "6A123516-FB88-470D-A428-9AF58B71B3C0",
            UserId = "ou_7dab8a3d3dfcd10xxx",
            Form = new List<FormData>
            {
                new FormData { Id = "amount", Type = "number", Value = "5000" },
                new FormData { Id = "reason", Type = "textarea", Value = "费用明细已补充完整" }
            },
            Comment = "已按财务意见修改金额并补充说明"
        };

        var result = await _taskClient.ResubmitApprovalAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine("审批已重新提交");
        }
    }
}
```

**完整的审批任务处理示例：**

```csharp
// 使用租户令牌处理审批任务的完整流程
public class ApprovalTaskProcessor
{
    private readonly IFeishuTenantV4ApprovalTask _taskClient;

    public ApprovalTaskProcessor(IFeishuTenantV4ApprovalTask taskClient)
    {
        _taskClient = taskClient;
    }

    public async Task<bool> ProcessApprovalTaskAsync(
        string taskId,
        string instanceCode,
        string userId,
        ApprovalAction action,
        string? comment = null)
    {
        FeishuNullDataApiResult? result;

        switch (action)
        {
            case ApprovalAction.Agree:
                result = await _taskClient.AgreeApprovalAsync(
                    new AgreeApprovalTasksRequest
                    {
                        TaskId = taskId,
                        InstanceCode = instanceCode,
                        UserId = userId,
                        Comment = comment
                    });
                break;

            case ApprovalAction.Reject:
                result = await _taskClient.RejectApprovalAsync(
                    new RejectApprovalTaskRequest
                    {
                        TaskId = taskId,
                        InstanceCode = instanceCode,
                        UserId = userId,
                        Comment = comment
                    });
                break;

            case ApprovalAction.Transfer:
                result = await _taskClient.TransferApprovalAsync(
                    new TransferApprovalTasksRequest
                    {
                        TaskId = taskId,
                        InstanceCode = instanceCode,
                        UserId = userId,
                        TransferUserId = comment!,
                        Comment = "请协助审批"
                    });
                break;

            default:
                throw new ArgumentException("不支持的审批操作");
        }

        return result?.Code == 0;
    }
}

public enum ApprovalAction
{
    Agree,
    Reject,
    Transfer
}
```
