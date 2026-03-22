# 审批查询接口（用户） -（FeishuV4ApprovalQuery_User）

## 接口名称

审批查询接口（用户） -（`IFeishuUserV4ApprovalQuery`）

## 功能描述

该接口用于通过不同条件查询审批系统中符合条件的审批实例、审批抄送、审批任务列表（适用于原生审批及三方审批）。支持用户级别的审批数据查询，使用用户身份令牌进行认证，适用于用户自助查询自己的审批数据场景。

## 参考文档

- [飞书官方文档 - 审批查询](https://open.feishu.cn/document/server-docs/approval-v4/approval-search/query-2)

## 函数列表

| 函数名称                     | 功能描述             | 认证方式 | HTTP 方法 |
| ---------------------------- | -------------------- | -------- | --------- |
| `GetInstancesPageListAsync`  | 查询审批实例分页列表 | 用户令牌 | POST      |
| `GetCarbonCopyPageListAsync` | 查询审批抄送分页列表 | 用户令牌 | POST      |
| `GetTasksPageListAsync`      | 查询审批任务分页列表 | 用户令牌 | POST      |

## 函数详细内容

### 查询审批实例分页列表

#### 函数签名

```csharp
Task<FeishuApiResult<ApprovalInstancesQueryResult>?> GetInstancesPageListAsync(
    [Body] ApprovalInstancesQueryRequest approvalInstancesQueryRequest,
    [Query("page_size")] int page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**用户令牌**（`UserAccessToken`）

#### 参数

| 参数名                          | 必填 | 类型                            | 描述                          |
| ------------------------------- | ---- | ------------------------------- | ----------------------------- |
| `approvalInstancesQueryRequest` | ✅   | `ApprovalInstancesQueryRequest` | 查询实例列表请求体            |
| `page_size`                     | ⚪   | `int`                           | 分页大小，默认：10            |
| `page_token`                    | ⚪   | `string`                        | 分页标记，第一次请求不填      |
| `user_id_type`                  | ⚪   | `string`                        | 用户 ID 类型，默认：`open_id` |
| `cancellationToken`             | ⚪   | `CancellationToken`             | 取消操作令牌对象              |

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
        "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
        "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
        "approval_name": "请假申请",
        "open_id": "ou_7dab8a3d3dfcd10xxx",
        "status": "PENDING",
        "create_time": "2025-03-20T10:30:00+08:00",
        "update_time": "2025-03-20T10:30:00+08:00"
      }
    ],
    "page_token": "next_page_token",
    "has_more": true
  }
}
```

#### 说明

- 使用用户令牌查询，只能查询当前用户有权限查看的审批实例
- 适合在用户端界面展示"我的审批"列表
- 支持按审批定义、状态、时间范围等条件筛选

#### 代码示例

```csharp
// 使用用户权限查询审批实例分页列表
public class UserApprovalQueryService
{
    private readonly IFeishuUserV4ApprovalQuery _queryClient;

    public UserApprovalQueryService(IFeishuUserV4ApprovalQuery queryClient)
    {
        _queryClient = queryClient;
    }

    public async Task GetMyInstancesAsync()
    {
        var request = new ApprovalInstancesQueryRequest
        {
            Status = "PENDING",
            StartTime = DateTime.Now.AddDays(-30),
            EndTime = DateTime.Now
        };

        var result = await _queryClient.GetInstancesPageListAsync(request, page_size: 20);

        if (result?.Code == 0)
        {
            Console.WriteLine($"您有 {result.Data?.Items?.Count} 条审批记录");
            foreach (var instance in result.Data?.Items ?? new List<ApprovalInstance>())
            {
                Console.WriteLine($"- {instance.ApprovalName} ({instance.Status})");
            }
        }
    }
}
```

---

### 查询审批抄送分页列表

#### 函数签名

```csharp
Task<FeishuApiResult<ApprovalInstancesCcQueryResult>?> GetCarbonCopyPageListAsync(
    [Body] ApprovalInstancesCcQueryRequest approvalInstancesCcQueryReques,
    [Query("page_size")] int page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**用户令牌**（`UserAccessToken`）

#### 参数

| 参数名                           | 必填 | 类型                              | 描述                          |
| -------------------------------- | ---- | --------------------------------- | ----------------------------- |
| `approvalInstancesCcQueryReques` | ✅   | `ApprovalInstancesCcQueryRequest` | 查询抄送列表请求体            |
| `page_size`                      | ⚪   | `int`                             | 分页大小，默认：10            |
| `page_token`                     | ⚪   | `string`                          | 分页标记，第一次请求不填      |
| `user_id_type`                   | ⚪   | `string`                          | 用户 ID 类型，默认：`open_id` |
| `cancellationToken`              | ⚪   | `CancellationToken`               | 取消操作令牌对象              |

**请求体示例：**

```json
{
  "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
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
        "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
        "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
        "approval_name": "请假申请",
        "cc_open_id": "ou_7dab8a3d3dfcd10xxx",
        "status": "APPROVED",
        "cc_time": "2025-03-20T10:30:00+08:00"
      }
    ],
    "page_token": "",
    "has_more": false
  }
}
```

#### 说明

- 使用用户令牌查询，返回当前用户被抄送的审批实例列表
- 适合在用户端界面展示"抄送我"列表

#### 代码示例

```csharp
// 使用用户权限查询审批抄送分页列表
public class UserApprovalQueryService
{
    private readonly IFeishuUserV4ApprovalQuery _queryClient;

    public UserApprovalQueryService(IFeishuUserV4ApprovalQuery queryClient)
    {
        _queryClient = queryClient;
    }

    public async Task GetMyCarbonCopyListAsync()
    {
        var request = new ApprovalInstancesCcQueryRequest
        {
            StartTime = DateTime.Now.AddDays(-30),
            EndTime = DateTime.Now
        };

        var result = await _queryClient.GetCarbonCopyPageListAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine($"您有 {result.Data?.Items?.Count} 条抄送记录");
        }
    }
}
```

---

### 查询审批任务分页列表

#### 函数签名

```csharp
Task<FeishuApiResult<ApprovalInstancesTaskQueryResult>?> GetTasksPageListAsync(
    [Body] ApprovalInstancesTaskQueryRequest approvalInstancesTaskQueryRequest,
    [Query("page_size")] int page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**用户令牌**（`UserAccessToken`）

#### 参数

| 参数名                              | 必填 | 类型                                | 描述                          |
| ----------------------------------- | ---- | ----------------------------------- | ----------------------------- |
| `approvalInstancesTaskQueryRequest` | ✅   | `ApprovalInstancesTaskQueryRequest` | 查询任务列表请求体            |
| `page_size`                         | ⚪   | `int`                               | 分页大小，默认：10            |
| `page_token`                        | ⚪   | `string`                            | 分页标记，第一次请求不填      |
| `user_id_type`                      | ⚪   | `string`                            | 用户 ID 类型，默认：`open_id` |
| `cancellationToken`                 | ⚪   | `CancellationToken`                 | 取消操作令牌对象              |

**请求体示例：**

```json
{
  "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
  "task_status": "PENDING",
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
        "task_id": "123456789",
        "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
        "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
        "approval_name": "请假申请",
        "open_id": "ou_7dab8a3d3dfcd10xxx",
        "task_status": "PENDING",
        "node_name": "部门主管审批",
        "create_time": "2025-03-20T10:30:00+08:00"
      }
    ],
    "page_token": "",
    "has_more": false
  }
}
```

#### 说明

- 使用用户令牌查询，返回当前用户的审批任务列表
- 适合在用户端界面展示"待办事项"或"我的任务"列表
- 用户可以查询自己的待办任务、已办任务等

#### 代码示例

```csharp
// 使用用户权限查询审批任务分页列表
public class UserApprovalQueryService
{
    private readonly IFeishuUserV4ApprovalQuery _queryClient;

    public UserApprovalQueryService(IFeishuUserV4ApprovalQuery queryClient)
    {
        _queryClient = queryClient;
    }

    public async Task GetMyPendingTasksAsync()
    {
        var request = new ApprovalInstancesTaskQueryRequest
        {
            TaskStatus = "PENDING",
            StartTime = DateTime.Now.AddDays(-30),
            EndTime = DateTime.Now
        };

        var result = await _queryClient.GetTasksPageListAsync(request, page_size: 50);

        if (result?.Code == 0)
        {
            var pendingTasks = result.Data?.Items ?? new List<ApprovalTask>();
            Console.WriteLine($"您有 {pendingTasks.Count} 个待办审批任务");

            foreach (var task in pendingTasks)
            {
                Console.WriteLine($"- [{task.ApprovalName}] {task.NodeName} ({task.CreateTime:MM-dd})");
            }
        }
    }
}
```

**完整的个人审批中心示例：**

```csharp
// 使用用户权限构建个人审批中心数据
public class PersonalApprovalCenterService
{
    private readonly IFeishuUserV4ApprovalQuery _queryClient;

    public PersonalApprovalCenterService(IFeishuUserV4ApprovalQuery queryClient)
    {
        _queryClient = queryClient;
    }

    public async Task<PersonalApprovalCenter> GetPersonalApprovalCenterAsync()
    {
        var center = new PersonalApprovalCenter();

        var taskRequest = new ApprovalInstancesTaskQueryRequest
        {
            TaskStatus = "PENDING"
        };

        var taskResult = await _queryClient.GetTasksPageListAsync(taskRequest);

        if (taskResult?.Code == 0)
        {
            center.PendingTasks = taskResult.Data?.Items ?? new List<ApprovalTask>();
            center.PendingCount = center.PendingTasks.Count;
        }

        var ccRequest = new ApprovalInstancesCcQueryRequest
        {
            StartTime = DateTime.Now.AddDays(-30)
        };

        var ccResult = await _queryClient.GetCarbonCopyPageListAsync(ccRequest, page_size: 10);

        if (ccResult?.Code == 0)
        {
            center.CcList = ccResult.Data?.Items ?? new List<ApprovalCcItem>();
        }

        var instanceRequest = new ApprovalInstancesQueryRequest
        {
            StartTime = DateTime.Now.AddDays(-30)
        };

        var instanceResult = await _queryClient.GetInstancesPageListAsync(instanceRequest, page_size: 10);

        if (instanceResult?.Code == 0)
        {
            center.MyInstances = instanceResult.Data?.Items ?? new List<ApprovalInstance>();
        }

        return center;
    }
}

public class PersonalApprovalCenter
{
    public int PendingCount { get; set; }
    public List<ApprovalTask> PendingTasks { get; set; } = new();
    public List<ApprovalCcItem> CcList { get; set; } = new();
    public List<ApprovalInstance> MyInstances { get; set; } = new();
}
```

## 租户接口与用户接口的区别

| 特性     | 租户接口 (`IFeishuTenantV4ApprovalQuery`) | 用户接口 (`IFeishuUserV4ApprovalQuery`) |
| -------- | ----------------------------------------- | --------------------------------------- |
| 认证方式 | 租户令牌                                  | 用户令牌                                |
| 数据范围 | 可查询企业全部审批数据                    | 只能查询当前用户有权限的数据            |
| 适用场景 | 后台管理系统、数据分析                    | 用户端界面、个人中心                    |
| 权限要求 | 需要应用具备相应权限                      | 需要用户授权相应权限                    |
