# 原生审批管理接口 -（FeishuV4Approval_Tenant）

## 接口名称

原生审批管理接口 -（`IFeishuTenantV4Approval`）

## 功能描述

该接口用于管理飞书原生审批功能。根据企业业务需要在飞书审批中心创建审批定义，用来定义一类审批的表单与流程，后续员工发起审批时，需要填写定义的表单，审批的流转也会按照定义的流程进行。

## 参考文档

- [飞书官方文档 - 原生审批](https://open.feishu.cn/document/server-docs/approval-v4/approval/overview-of-approval-resources)

## 函数列表

| 函数名称                  | 功能描述                   | 认证方式 | HTTP 方法 |
| ------------------------- | -------------------------- | -------- | --------- |
| `CreateApprovalAsync`     | 创建审批定义               | 租户令牌 | POST      |
| `GetApprovalByCodeAsync`  | 获取审批定义详情           | 租户令牌 | GET       |
| `CreateInstanceAsync`     | 创建审批实例               | 租户令牌 | POST      |
| `CancelInstanceAsync`     | 撤回审批实例               | 租户令牌 | POST      |
| `CarbonCopyInstanceAsync` | 抄送审批实例               | 租户令牌 | POST      |
| `PreviewInstanceAsync`    | 预览审批流程（创建实例前） | 租户令牌 | POST      |
| `PreviewInstanceAsync`    | 预览审批流程（创建实例后） | 租户令牌 | POST      |
| `GetInstanceByIdAsync`    | 获取审批实例详情           | 租户令牌 | GET       |

## 函数详细内容

### 创建审批定义

#### 函数签名

```csharp
Task<FeishuApiResult<CreateApprovalResult>?> CreateApprovalAsync(
    [Body] CreateApprovalRequest createApprovalRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                  | 必填 | 类型                    | 描述                                     |
| ----------------------- | ---- | ----------------------- | ---------------------------------------- |
| `createApprovalRequest` | ✅   | `CreateApprovalRequest` | 创建审批定义请求体                       |
| `user_id_type`          | ⚪   | `string`                | 用户 ID 类型，默认：`open_id`            |
| `department_id_type`    | ⚪   | `string`                | 部门 ID 类型，默认：`open_department_id` |
| `cancellationToken`     | ⚪   | `CancellationToken`     | 取消操作令牌对象                         |

**请求体示例：**

```json
{
  "approval_name": "请假申请",
  "description": "用于员工请假的审批流程",
  "viewers": [
    {
      "viewer_type": "USER",
      "viewer_id": "ou_7dab8a3d3dfcd10xxx"
    }
  ],
  "form": {
    "form_content": "<input type='text' name='reason' placeholder='请假原因'/>"
  },
  "config": {
    "enable_again": true,
    "enable_cc": true
  }
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85"
  }
}
```

#### 说明

- 用于创建审批定义，可以灵活指定审批定义的基础信息、表单和流程等
- 创建成功后返回 `approval_code`，后续操作都需要使用此 Code

#### 代码示例

```csharp
// 使用租户权限创建审批定义
public class ApprovalService
{
    private readonly IFeishuTenantV4Approval _approvalClient;

    public ApprovalService(IFeishuTenantV4Approval approvalClient)
    {
        _approvalClient = approvalClient;
    }

    public async Task CreateApprovalAsync()
    {
        var request = new CreateApprovalRequest
        {
            ApprovalName = "请假申请",
            Description = "用于员工请假的审批流程",
            Viewers = new List<Viewer>
            {
                new Viewer
                {
                    ViewerType = "TENANT",
                    ViewerId = ""
                }
            },
            Form = new Form
            {
                FormContent = "表单配置内容"
            },
            Config = new ApprovalConfig
            {
                EnableAgain = true,
                EnableCc = true
            }
        };

        var result = await _approvalClient.CreateApprovalAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine($"审批定义创建成功，Code: {result.Data?.ApprovalCode}");
        }
    }
}
```

---

### 获取审批定义详情

#### 函数签名

```csharp
Task<FeishuApiResult<GetApprovalResult>?> GetApprovalByCodeAsync(
    [Path] string approval_code,
    [Query("locale")] string? locale = null,
    [Query("with_admin_id")] bool? with_admin_id = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名              | 必填 | 类型                | 描述                                                            |
| ------------------- | ---- | ------------------- | --------------------------------------------------------------- |
| `approval_code`     | ✅   | `string`            | 审批定义 Code，示例值：`"7C468A54-8745-2245-9675-08B7C63E7A85"` |
| `locale`            | ⚪   | `string`            | 语言可选值，默认为审批定义配置的默认语言，示例值：`"zh-CN"`     |
| `with_admin_id`     | ⚪   | `bool`              | 是否返回有数据管理权限的审批流程管理员 ID 列表                  |
| `user_id_type`      | ⚪   | `string`            | 用户 ID 类型，默认：`open_id`                                   |
| `cancellationToken` | ⚪   | `CancellationToken` | 取消操作令牌对象                                                |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
    "approval_name": "请假申请",
    "description": "用于员工请假的审批流程",
    "status": "ACTIVE",
    "form": {
      "form_content": "表单配置内容"
    },
    "nodes": [
      {
        "node_id": "START",
        "node_type": "START",
        "node_name": "发起人"
      }
    ]
  }
}
```

#### 说明

- 根据审批定义 Code 以及语言、用户 ID 等筛选条件获取指定审批定义的信息
- 包括审批定义名称、状态、表单控件以及节点等信息
- 获取审批定义信息后，可根据信息构造创建审批实例的请求

#### 代码示例

```csharp
// 使用租户权限获取审批定义详情
public class ApprovalService
{
    private readonly IFeishuTenantV4Approval _approvalClient;

    public ApprovalService(IFeishuTenantV4Approval approvalClient)
    {
        _approvalClient = approvalClient;
    }

    public async Task GetApprovalAsync()
    {
        var approvalCode = "7C468A54-8745-2245-9675-08B7C63E7A85";
        var result = await _approvalClient.GetApprovalByCodeAsync(
            approvalCode,
            locale: "zh-CN",
            with_admin_id: true);

        if (result?.Code == 0)
        {
            Console.WriteLine($"审批名称: {result.Data?.ApprovalName}");
            Console.WriteLine($"审批状态: {result.Data?.Status}");
        }
    }
}
```

---

### 创建审批实例

#### 函数签名

```csharp
Task<FeishuApiResult<CreateInstancesResult>?> CreateInstanceAsync(
    [Body] CreateInstanceRequest createInstanceRequest,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                  | 必填 | 类型                    | 描述               |
| ----------------------- | ---- | ----------------------- | ------------------ |
| `createInstanceRequest` | ✅   | `CreateInstanceRequest` | 创建审批实例请求体 |
| `cancellationToken`     | ⚪   | `CancellationToken`     | 取消操作令牌对象   |

**请求体示例：**

```json
{
  "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
  "open_id": "ou_7dab8a3d3dfcd10xxx",
  "form": [
    {
      "id": "reason",
      "type": "input",
      "value": "家中有事"
    },
    {
      "id": "date",
      "type": "date",
      "value": "2025-03-20"
    }
  ],
  "node_custome_mode": "SEQUENTIAL"
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0"
  }
}
```

#### 说明

- 使用指定审批定义 Code 创建一个审批实例
- 接口调用者需对审批定义的表单有详细了解
- 按照定义的表单结构，将表单 Value 通过本接口传入

#### 代码示例

```csharp
// 使用租户权限创建审批实例
public class ApprovalService
{
    private readonly IFeishuTenantV4Approval _approvalClient;

    public ApprovalService(IFeishuTenantV4Approval approvalClient)
    {
        _approvalClient = approvalClient;
    }

    public async Task CreateInstanceAsync()
    {
        var request = new CreateInstanceRequest
        {
            ApprovalCode = "7C468A54-8745-2245-9675-08B7C63E7A85",
            OpenId = "ou_7dab8a3d3dfcd10xxx",
            Form = new List<FormData>
            {
                new FormData { Id = "reason", Type = "input", Value = "家中有事" },
                new FormData { Id = "date", Type = "date", Value = "2025-03-20" },
                new FormData { Id = "days", Type = "number", Value = "1" }
            },
            NodeCustomeMode = "SEQUENTIAL"
        };

        var result = await _approvalClient.CreateInstanceAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine($"审批实例创建成功，Code: {result.Data?.InstanceCode}");
        }
    }
}
```

---

### 撤回审批实例

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> CancelInstanceAsync(
    [Body] CancelInstancesRequest cancelInstancesRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                   | 必填 | 类型                     | 描述                          |
| ------------------------ | ---- | ------------------------ | ----------------------------- |
| `cancelInstancesRequest` | ✅   | `CancelInstancesRequest` | 撤回审批实例请求体            |
| `user_id_type`           | ⚪   | `string`                 | 用户 ID 类型，默认：`open_id` |
| `cancellationToken`      | ⚪   | `CancellationToken`      | 取消操作令牌对象              |

**请求体示例：**

```json
{
  "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
  "user_id": "ou_7dab8a3d3dfcd10xxx",
  "reason": "信息填写错误，需要重新提交"
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

- 管理员在审批后台的某一审批定义的更多设置中，勾选了"允许撤销审批中的申请"或者"允许撤销 x 天内通过的审批"
- 在符合撤销规则的情况下，可以调用本接口将指定提交人的审批实例撤回

#### 代码示例

```csharp
// 使用租户权限撤回审批实例
public class ApprovalService
{
    private readonly IFeishuTenantV4Approval _approvalClient;

    public ApprovalService(IFeishuTenantV4Approval approvalClient)
    {
        _approvalClient = approvalClient;
    }

    public async Task CancelInstanceAsync()
    {
        var request = new CancelInstancesRequest
        {
            InstanceCode = "6A123516-FB88-470D-A428-9AF58B71B3C0",
            UserId = "ou_7dab8a3d3dfcd10xxx",
            Reason = "信息填写错误，需要重新提交"
        };

        var result = await _approvalClient.CancelInstanceAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine("审批实例撤回成功");
        }
    }
}
```

---

### 抄送审批实例

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> CarbonCopyInstanceAsync(
    [Body] CarbonCopyInstanceRequest ccInstanceRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名              | 必填 | 类型                        | 描述                          |
| ------------------- | ---- | --------------------------- | ----------------------------- |
| `ccInstanceRequest` | ✅   | `CarbonCopyInstanceRequest` | 抄送审批实例请求体            |
| `user_id_type`      | ⚪   | `string`                    | 用户 ID 类型，默认：`open_id` |
| `cancellationToken` | ⚪   | `CancellationToken`         | 取消操作令牌对象              |

**请求体示例：**

```json
{
  "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
  "cc_user_ids": ["ou_7dab8a3d3dfcd10xxx", "ou_8eab9b4e4egde21yyy"]
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

- 将当前审批实例抄送给指定用户
- 被抄送的用户可以查看审批实例详情
- 在飞书客户端的"工作台 > 审批 > 审批中心 > 抄送我"列表中可查看到审批实例

#### 代码示例

```csharp
// 使用租户权限抄送审批实例
public class ApprovalService
{
    private readonly IFeishuTenantV4Approval _approvalClient;

    public ApprovalService(IFeishuTenantV4Approval approvalClient)
    {
        _approvalClient = approvalClient;
    }

    public async Task CarbonCopyInstanceAsync()
    {
        var request = new CarbonCopyInstanceRequest
        {
            InstanceCode = "6A123516-FB88-470D-A428-9AF58B71B3C0",
            CcUserIds = new List<string>
            {
                "ou_7dab8a3d3dfcd10xxx",
                "ou_8eab9b4e4egde21yyy"
            }
        };

        var result = await _approvalClient.CarbonCopyInstanceAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine("抄送成功");
        }
    }
}
```

---

### 预览审批流程（创建实例前）

#### 函数签名

```csharp
Task<FeishuApiResult<PreviewNodeResult>?> PreviewInstanceAsync(
    [Body] PreviewInstanceRequest previewInstanceRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                   | 必填 | 类型                     | 描述                             |
| ------------------------ | ---- | ------------------------ | -------------------------------- |
| `previewInstanceRequest` | ✅   | `PreviewInstanceRequest` | 预览审批流程请求体（创建实例前） |
| `user_id_type`           | ⚪   | `string`                 | 用户 ID 类型，默认：`open_id`    |
| `cancellationToken`      | ⚪   | `CancellationToken`      | 取消操作令牌对象                 |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "nodes": [
      {
        "node_id": "START",
        "node_type": "START",
        "node_name": "发起人"
      },
      {
        "node_id": "APPROVAL_1",
        "node_type": "APPROVAL",
        "node_name": "部门主管审批",
        "approvers": ["ou_7dab8a3d3dfcd10xxx"]
      }
    ]
  }
}
```

#### 说明

- 在创建审批实例之前，可调用本接口预览审批流程数据
- 帮助用户了解审批流程走向

#### 代码示例

```csharp
// 使用租户权限预览审批流程
public class ApprovalService
{
    private readonly IFeishuTenantV4Approval _approvalClient;

    public ApprovalService(IFeishuTenantV4Approval approvalClient)
    {
        _approvalClient = approvalClient;
    }

    public async Task PreviewInstanceAsync()
    {
        var request = new PreviewInstanceRequest
        {
            ApprovalCode = "7C468A54-8745-2245-9675-08B7C63E7A85",
            OpenId = "ou_7dab8a3d3dfcd10xxx"
        };

        var result = await _approvalClient.PreviewInstanceAsync(request);

        if (result?.Code == 0)
        {
            foreach (var node in result.Data?.Nodes ?? new List<PreviewNode>())
            {
                Console.WriteLine($"节点: {node.NodeName}, 类型: {node.NodeType}");
            }
        }
    }
}
```

---

### 预览审批流程（创建实例后）

#### 函数签名

```csharp
Task<FeishuApiResult<PreviewNodeResult>?> PreviewInstanceAsync(
    [Body] PreviewInstanceAfterRequest previewInstanceRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                   | 必填 | 类型                          | 描述                             |
| ------------------------ | ---- | ----------------------------- | -------------------------------- |
| `previewInstanceRequest` | ✅   | `PreviewInstanceAfterRequest` | 预览审批流程请求体（创建实例后） |
| `user_id_type`           | ⚪   | `string`                      | 用户 ID 类型，默认：`open_id`    |
| `cancellationToken`      | ⚪   | `CancellationToken`           | 取消操作令牌对象                 |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "nodes": [
      {
        "node_id": "APPROVAL_2",
        "node_type": "APPROVAL",
        "node_name": "财务审批"
      }
    ]
  }
}
```

#### 说明

- 在创建审批实例之后，可调用本接口预览某一审批节点的后续流程数据
- 帮助用户了解从当前节点开始的后续审批流程走向

---

### 获取审批实例详情

#### 函数签名

```csharp
Task<FeishuApiResult<GetApprovalInstanceResult>?> GetInstanceByIdAsync(
    [Path] string instance_id,
    [Query("locale")] string? locale = null,
    [Query("user_id")] bool? user_id = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名              | 必填 | 类型                | 描述                                                            |
| ------------------- | ---- | ------------------- | --------------------------------------------------------------- |
| `instance_id`       | ✅   | `string`            | 审批实例 Code，示例值：`"6A123516-FB88-470D-A428-9AF58B71B3C0"` |
| `locale`            | ⚪   | `string`            | 语言可选值，示例值：`"zh-CN"`                                   |
| `user_id`           | ⚪   | `bool`              | 是否返回发起审批的用户 ID                                       |
| `user_id_type`      | ⚪   | `string`            | 用户 ID 类型，默认：`open_id`                                   |
| `cancellationToken` | ⚪   | `CancellationToken` | 取消操作令牌对象                                                |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
    "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
    "approval_name": "请假申请",
    "open_id": "ou_7dab8a3d3dfcd10xxx",
    "status": "PENDING",
    "create_time": "2025-03-20T10:30:00+08:00",
    "update_time": "2025-03-20T10:30:00+08:00",
    "form": [
      {
        "id": "reason",
        "name": "请假原因",
        "type": "input",
        "value": "家中有事"
      }
    ],
    "tasks": [
      {
        "task_id": "123456789",
        "open_id": "ou_8eab9b4e4egde21yyy",
        "status": "PENDING",
        "node_name": "部门主管审批"
      }
    ]
  }
}
```

#### 说明

- 通过审批实例 Code 获取指定审批实例的详细信息
- 包括审批实例的名称、创建时间、发起审批的用户、状态以及任务列表等信息

#### 代码示例

```csharp
// 使用租户权限获取审批实例详情
public class ApprovalService
{
    private readonly IFeishuTenantV4Approval _approvalClient;

    public ApprovalService(IFeishuTenantV4Approval approvalClient)
    {
        _approvalClient = approvalClient;
    }

    public async Task GetInstanceAsync()
    {
        var instanceId = "6A123516-FB88-470D-A428-9AF58B71B3C0";
        var result = await _approvalClient.GetInstanceByIdAsync(instanceId);

        if (result?.Code == 0)
        {
            var data = result.Data;
            Console.WriteLine($"审批名称: {data?.ApprovalName}");
            Console.WriteLine($"审批状态: {data?.Status}");
            Console.WriteLine($"创建时间: {data?.CreateTime}");

            foreach (var task in data?.Tasks ?? new List<TaskInfo>())
            {
                Console.WriteLine($"任务: {task.NodeName}, 状态: {task.Status}");
            }
        }
    }
}
```
