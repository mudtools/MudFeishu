# 三方审批管理接口 -（FeishuV4ApprovalExternal_Tenant）

## 接口名称

三方审批管理接口 -（`IFeishuTenantV4ApprovalExternal`）

## 功能描述

该接口用于将企业原有的审批系统与飞书审批系统连通。三方审批定义 API 用于连通企业原有的审批系统与飞书审批系统，连通仅为数据层的连通，即企业无需改造原有的审批系统，只需创建三方审批定义，设置三方审批系统的访问方式、回调 URL 以及加密密钥等数据，使三方审批系统的审批数据可以在飞书审批之间来回流转，从而实现企业员工在飞书内一站式查看和处理所有审批任务。

## 参考文档

- [飞书官方文档 - 三方审批](https://open.feishu.cn/document/server-docs/approval-v4/external_approval/overview)

## 函数列表

| 函数名称                         | 功能描述                     | 认证方式 | HTTP 方法 |
| -------------------------------- | ---------------------------- | -------- | --------- |
| `CreateApprovalAsync`            | 创建三方审批定义             | 租户令牌 | POST      |
| `GetApprovalByCodeAsync`         | 获取三方审批定义详情         | 租户令牌 | GET       |
| `SyncInstancesAsync`             | 同步三方审批实例             | 租户令牌 | POST      |
| `CheckInstancesAsync`            | 校验三方审批实例数据         | 租户令牌 | POST      |
| `GetInstancesStatePageListAsync` | 获取三方审批实例状态分页列表 | 租户令牌 | GET       |

## 函数详细内容

### 创建三方审批定义

#### 函数签名

```csharp
Task<FeishuApiResult<CreateApprovalExternalResult>?> CreateApprovalAsync(
    [Body] CreateApprovalExternalRequest createApprovalRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                  | 必填 | 类型                            | 描述                                     |
| ----------------------- | ---- | ------------------------------- | ---------------------------------------- |
| `createApprovalRequest` | ✅   | `CreateApprovalExternalRequest` | 创建三方审批定义请求体                   |
| `user_id_type`          | ⚪   | `string`                        | 用户 ID 类型，默认：`open_id`            |
| `department_id_type`    | ⚪   | `string`                        | 部门 ID 类型，默认：`open_department_id` |
| `cancellationToken`     | ⚪   | `CancellationToken`             | 取消操作令牌对象                         |

**请求体示例：**

```json
{
  "approval_name": "ERP采购审批",
  "description": "连接企业ERP系统的采购审批流程",
  "external": {
    "create_link_pc": "https://erp.company.com/approval/create",
    "create_link_mobile": "https://erp.company.com/mobile/approval/create",
    "callback_url": "https://erp.company.com/callback/feishu",
    "secret": "your_secret_key"
  },
  "viewers": [
    {
      "viewer_type": "TENANT"
    }
  ]
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

- 用于创建三方审批定义，设置审批的名称、描述等基本信息
- 设置三方审批系统的审批发起页、回调 URL 等信息
- 使企业员工在飞书审批内即可发起并操作三方审批

#### 代码示例

```csharp
// 使用租户权限创建三方审批定义
public class ExternalApprovalService
{
    private readonly IFeishuTenantV4ApprovalExternal _externalClient;

    public ExternalApprovalService(IFeishuTenantV4ApprovalExternal externalClient)
    {
        _externalClient = externalClient;
    }

    public async Task CreateApprovalAsync()
    {
        var request = new CreateApprovalExternalRequest
        {
            ApprovalName = "ERP采购审批",
            Description = "连接企业ERP系统的采购审批流程",
            External = new ExternalConfig
            {
                CreateLinkPc = "https://erp.company.com/approval/create",
                CreateLinkMobile = "https://erp.company.com/mobile/approval/create",
                CallbackUrl = "https://erp.company.com/callback/feishu",
                Secret = "your_secret_key"
            },
            Viewers = new List<Viewer>
            {
                new Viewer { ViewerType = "TENANT" }
            }
        };

        var result = await _externalClient.CreateApprovalAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine($"三方审批定义创建成功，Code: {result.Data?.ApprovalCode}");
        }
    }
}
```

---

### 获取三方审批定义详情

#### 函数签名

```csharp
Task<FeishuApiResult<GetApprovalExternalResult>?> GetApprovalByCodeAsync(
    [Path] string approval_code,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名              | 必填 | 类型                | 描述                                                                |
| ------------------- | ---- | ------------------- | ------------------------------------------------------------------- |
| `approval_code`     | ✅   | `string`            | 三方审批定义 Code，示例值：`"7C468A54-8745-2245-9675-08B7C63E7A85"` |
| `user_id_type`      | ⚪   | `string`            | 用户 ID 类型，默认：`open_id`                                       |
| `cancellationToken` | ⚪   | `CancellationToken` | 取消操作令牌对象                                                    |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
    "approval_name": "ERP采购审批",
    "description": "连接企业ERP系统的采购审批流程",
    "status": "ACTIVE",
    "external": {
      "create_link_pc": "https://erp.company.com/approval/create",
      "create_link_mobile": "https://erp.company.com/mobile/approval/create",
      "callback_url": "https://erp.company.com/callback/feishu"
    },
    "viewers": [
      {
        "viewer_type": "TENANT"
      }
    ]
  }
}
```

#### 说明

- 通过三方审批定义 Code 获取审批定义的详细数据
- 包括三方审批定义的名称、说明、三方审批发起链接、回调 URL 以及审批定义可见人列表等信息

#### 代码示例

```csharp
// 使用租户权限获取三方审批定义详情
public class ExternalApprovalService
{
    private readonly IFeishuTenantV4ApprovalExternal _externalClient;

    public ExternalApprovalService(IFeishuTenantV4ApprovalExternal externalClient)
    {
        _externalClient = externalClient;
    }

    public async Task GetApprovalAsync()
    {
        var approvalCode = "7C468A54-8745-2245-9675-08B7C63E7A85";
        var result = await _externalClient.GetApprovalByCodeAsync(approvalCode);

        if (result?.Code == 0)
        {
            var data = result.Data;
            Console.WriteLine($"审批名称: {data?.ApprovalName}");
            Console.WriteLine($"PC端链接: {data?.External?.CreateLinkPc}");
            Console.WriteLine($"回调地址: {data?.External?.CallbackUrl}");
        }
    }
}
```

---

### 同步三方审批实例

#### 函数签名

```csharp
Task<FeishuApiResult<SyncExternalInstancesResult>?> SyncInstancesAsync(
    [Body] SyncApprovalInstancesRequest syncApprovalInstancesRequest,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                         | 必填 | 类型                           | 描述                   |
| ------------------------------ | ---- | ------------------------------ | ---------------------- |
| `syncApprovalInstancesRequest` | ✅   | `SyncApprovalInstancesRequest` | 同步三方审批实例请求体 |
| `cancellationToken`            | ⚪   | `CancellationToken`            | 取消操作令牌对象       |

**请求体示例：**

```json
{
  "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
  "instances": [
    {
      "instance_code": "ERP20250320001",
      "open_id": "ou_7dab8a3d3dfcd10xxx",
      "status": "PENDING",
      "title": "采购申请-办公用品",
      "create_time": "2025-03-20T10:30:00+08:00",
      "update_time": "2025-03-20T10:30:00+08:00",
      "url": "https://erp.company.com/approval/detail/ERP20250320001",
      "tasks": [
        {
          "task_id": "TASK001",
          "open_id": "ou_8eab9b4e4egde21yyy",
          "status": "PENDING"
        }
      ]
    }
  ]
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "synced_count": 1,
    "failed_count": 0
  }
}
```

#### 说明

- 用于把三方系统在审批流转后生成的审批实例、审批任务、审批抄送数据同步到审批中心
- 审批中心不负责审批的流转，审批的流转在三方系统

#### 代码示例

```csharp
// 使用租户权限同步三方审批实例到飞书
public class ExternalApprovalService
{
    private readonly IFeishuTenantV4ApprovalExternal _externalClient;

    public ExternalApprovalService(IFeishuTenantV4ApprovalExternal externalClient)
    {
        _externalClient = externalClient;
    }

    public async Task SyncInstancesAsync()
    {
        var request = new SyncApprovalInstancesRequest
        {
            ApprovalCode = "7C468A54-8745-2245-9675-08B7C63E7A85",
            Instances = new List<ExternalInstance>
            {
                new ExternalInstance
                {
                    InstanceCode = "ERP20250320001",
                    OpenId = "ou_7dab8a3d3dfcd10xxx",
                    Status = "PENDING",
                    Title = "采购申请-办公用品",
                    CreateTime = DateTime.Now.AddHours(-2),
                    UpdateTime = DateTime.Now,
                    Url = "https://erp.company.com/approval/detail/ERP20250320001",
                    Tasks = new List<ExternalTask>
                    {
                        new ExternalTask
                        {
                            TaskId = "TASK001",
                            OpenId = "ou_8eab9b4e4egde21yyy",
                            Status = "PENDING"
                        }
                    }
                }
            }
        };

        var result = await _externalClient.SyncInstancesAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine($"同步成功 {result.Data?.SyncedCount} 条实例");
        }
    }
}
```

---

### 校验三方审批实例数据

#### 函数签名

```csharp
Task<FeishuApiResult<CheckExternalInstancesResult>?> CheckInstancesAsync(
    [Body] CheckExternalInstancesRequest checkExternalInstancesRequest,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                          | 必填 | 类型                            | 描述                   |
| ------------------------------- | ---- | ------------------------------- | ---------------------- |
| `checkExternalInstancesRequest` | ✅   | `CheckExternalInstancesRequest` | 校验三方审批实例请求体 |
| `cancellationToken`             | ⚪   | `CancellationToken`             | 取消操作令牌对象       |

**请求体示例：**

```json
{
  "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
  "instances": [
    {
      "instance_code": "ERP20250320001",
      "update_time": "2025-03-20T10:30:00+08:00"
    }
  ]
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "invalid_instances": ["ERP20250319001"],
    "not_found_instances": ["ERP20250318001"]
  }
}
```

#### 说明

- 校验三方审批实例数据，用于判断服务端数据是否为最新的
- 请求时提交实例最新更新时间，如果服务端不存在该实例，或者服务端实例更新时间不是最新的，则返回对应实例 ID

#### 代码示例

```csharp
// 使用租户权限校验三方审批实例数据状态
public class ExternalApprovalService
{
    private readonly IFeishuTenantV4ApprovalExternal _externalClient;

    public ExternalApprovalService(IFeishuTenantV4ApprovalExternal externalClient)
    {
        _externalClient = externalClient;
    }

    public async Task CheckInstancesAsync()
    {
        var request = new CheckExternalInstancesRequest
        {
            ApprovalCode = "7C468A54-8745-2245-9675-08B7C63E7A85",
            Instances = new List<InstanceCheckInfo>
            {
                new InstanceCheckInfo
                {
                    InstanceCode = "ERP20250320001",
                    UpdateTime = DateTime.Now
                }
            }
        };

        var result = await _externalClient.CheckInstancesAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine($"无效实例: {string.Join(",", result.Data?.InvalidInstances ?? new List<string>())}");
            Console.WriteLine($"不存在实例: {string.Join(",", result.Data?.NotFoundInstances ?? new List<string>())}");
        }
    }
}
```

---

### 获取三方审批实例状态分页列表

#### 函数签名

```csharp
Task<FeishuApiResult<GetInstancesStateResult>?> GetInstancesStatePageListAsync(
    [Body] GetExternalInstancesStateRequest getExternalInstancesStateRequest,
    [Query("page_size")] int page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名                             | 必填 | 类型                               | 描述                           |
| ---------------------------------- | ---- | ---------------------------------- | ------------------------------ |
| `getExternalInstancesStateRequest` | ✅   | `GetExternalInstancesStateRequest` | 获取三方审批实例状态列表请求体 |
| `page_size`                        | ⚪   | `int`                              | 分页大小，默认：10             |
| `page_token`                       | ⚪   | `string`                           | 分页标记，第一次请求不填       |
| `cancellationToken`                | ⚪   | `CancellationToken`                | 取消操作令牌对象               |

**请求体示例：**

```json
{
  "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
  "status": "PENDING",
  "start_time": "2025-03-01T00:00:00+08:00",
  "end_time": "2025-03-20T23:59:59+08:00"
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "items": [
      {
        "instance_code": "ERP20250320001",
        "status": "PENDING",
        "open_id": "ou_7dab8a3d3dfcd10xxx",
        "update_time": "2025-03-20T10:30:00+08:00"
      }
    ],
    "page_token": "",
    "has_more": false
  }
}
```

#### 说明

- 用于分批获取三方审批的状态
- 用户传入查询条件，接口返回满足条件的审批实例的状态

#### 代码示例

```csharp
// 使用租户权限获取三方审批实例状态列表
public class ExternalApprovalService
{
    private readonly IFeishuTenantV4ApprovalExternal _externalClient;

    public ExternalApprovalService(IFeishuTenantV4ApprovalExternal externalClient)
    {
        _externalClient = externalClient;
    }

    public async Task GetInstancesStateAsync()
    {
        var request = new GetExternalInstancesStateRequest
        {
            ApprovalCode = "7C468A54-8745-2245-9675-08B7C63E7A85",
            Status = "PENDING",
            StartTime = DateTime.Now.AddDays(-7),
            EndTime = DateTime.Now
        };

        var result = await _externalClient.GetInstancesStatePageListAsync(request, page_size: 50);

        if (result?.Code == 0)
        {
            foreach (var item in result.Data?.Items ?? new List<InstanceStateItem>())
            {
                Console.WriteLine($"实例: {item.InstanceCode}, 状态: {item.Status}");
            }
        }
    }
}
```
