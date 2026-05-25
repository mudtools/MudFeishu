# 审批查询接口（租户） -（FeishuV4ApprovalQuery_Tenant）

## 接口名称

审批查询接口（租户） -（`IFeishuTenantV4ApprovalQuery`）

## 功能描述

该接口用于通过不同条件查询审批系统中符合条件的审批实例、审批抄送、审批任务列表（适用于原生审批及三方审批）。支持租户级别的审批数据查询，适用于后台管理系统或需要查询企业全部审批数据的场景。

## 参考文档

- [飞书官方文档 - 审批查询](https://open.feishu.cn/document/server-docs/approval-v4/approval-search/query-2)

## 函数列表

| 函数名称                     | 功能描述             | 认证方式 | HTTP 方法 |
| ---------------------------- | -------------------- | -------- | --------- |
| `GetInstancesPageListAsync`  | 查询审批实例分页列表 | 租户令牌 | POST      |
| `GetCarbonCopyPageListAsync` | 查询审批抄送分页列表 | 租户令牌 | POST      |
| `GetTasksPageListAsync`      | 查询审批任务分页列表 | 租户令牌 | POST      |

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

**租户令牌**（`TenantAccessToken`）

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
  "open_id": "ou_7dab8a3d3dfcd10xxx",
  "status": "PENDING",
  "start_time": "2025-03-01T00:00:00+08:00",
  "end_time": "2025-03-20T23:59:59+08:00"
}
```

**查询参数说明：**

| 字段名          | 必填 | 类型     | 描述                                                        |
| --------------- | ---- | -------- | ----------------------------------------------------------- |
| `approval_code` | ⚪   | `string` | 审批定义 Code                                               |
| `open_id`       | ⚪   | `string` | 发起审批的用户 ID                                           |
| `status`        | ⚪   | `string` | 审批实例状态：`PENDING`、`APPROVED`、`REJECTED`、`CANCELED` |
| `start_time`    | ⚪   | `string` | 开始时间（ISO 8601 格式）                                   |
| `end_time`      | ⚪   | `string` | 结束时间（ISO 8601 格式）                                   |
| `instance_code` | ⚪   | `string` | 审批实例 Code                                               |

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

- 通过不同条件查询审批系统中符合条件的审批实例列表
- 支持按审批定义、用户、状态、时间范围等条件筛选
- 适用于后台管理系统查询企业全部审批数据

#### 代码示例

```csharp
// 使用租户令牌查询审批实例分页列表
public class ApprovalQueryService
{
    private readonly IFeishuTenantV4ApprovalQuery _queryClient;

    public ApprovalQueryService(IFeishuTenantV4ApprovalQuery queryClient)
    {
        _queryClient = queryClient;
    }

    public async Task GetInstancesAsync()
    {
        var request = new ApprovalInstancesQueryRequest
        {
            ApprovalCode = "7C468A54-8745-2245-9675-08B7C63E7A85",
            Status = "PENDING",
            StartTime = DateTime.Now.AddDays(-30),
            EndTime = DateTime.Now
        };

        var result = await _queryClient.GetInstancesPageListAsync(request, page_size: 50);

        if (result?.Code == 0)
        {
            Console.WriteLine($"共找到 {result.Data?.Items?.Count} 条记录");
            foreach (var instance in result.Data?.Items ?? new List<ApprovalInstance>())
            {
                Console.WriteLine($"实例: {instance.InstanceCode}, 状态: {instance.Status}");
            }

            if (result.Data?.HasMore == true)
            {
                var nextPage = await _queryClient.GetInstancesPageListAsync(
                    request,
                    page_size: 50,
                    page_token: result.Data.PageToken);
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

**租户令牌**（`TenantAccessToken`）

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
  "cc_open_id": "ou_7dab8a3d3dfcd10xxx",
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

- 通过不同条件查询审批系统中符合条件的审批抄送列表
- 支持查询某个用户被抄送的全部审批实例

#### 代码示例

```csharp
// 使用租户令牌查询审批抄送分页列表
public class ApprovalQueryService
{
    private readonly IFeishuTenantV4ApprovalQuery _queryClient;

    public ApprovalQueryService(IFeishuTenantV4ApprovalQuery queryClient)
    {
        _queryClient = queryClient;
    }

    public async Task GetCarbonCopyListAsync()
    {
        var request = new ApprovalInstancesCcQueryRequest
        {
            CcOpenId = "ou_7dab8a3d3dfcd10xxx",
            StartTime = DateTime.Now.AddDays(-30),
            EndTime = DateTime.Now
        };

        var result = await _queryClient.GetCarbonCopyPageListAsync(request);

        if (result?.Code == 0)
        {
            Console.WriteLine($"共找到 {result.Data?.Items?.Count} 条抄送记录");
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

**租户令牌**（`TenantAccessToken`）

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
  "open_id": "ou_7dab8a3d3dfcd10xxx",
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

- 通过不同条件查询审批系统中符合条件的审批任务列表
- 支持查询某个用户的待办任务、已办任务等
- 适用于构建待办事项列表或任务管理功能

#### 代码示例

```csharp
// 使用租户令牌查询审批任务分页列表
public class ApprovalQueryService
{
    private readonly IFeishuTenantV4ApprovalQuery _queryClient;

    public ApprovalQueryService(IFeishuTenantV4ApprovalQuery queryClient)
    {
        _queryClient = queryClient;
    }

    public async Task GetTasksAsync()
    {
        var request = new ApprovalInstancesTaskQueryRequest
        {
            OpenId = "ou_7dab8a3d3dfcd10xxx",
            TaskStatus = "PENDING",
            StartTime = DateTime.Now.AddDays(-30),
            EndTime = DateTime.Now
        };

        var result = await _queryClient.GetTasksPageListAsync(request, page_size: 100);

        if (result?.Code == 0)
        {
            var pendingTasks = result.Data?.Items ?? new List<ApprovalTask>();
            Console.WriteLine($"您有 {pendingTasks.Count} 个待办审批任务");

            foreach (var task in pendingTasks)
            {
                Console.WriteLine($"- [{task.ApprovalName}] {task.NodeName}");
            }
        }
    }
}
```

**综合查询示例：**

```csharp
// 使用租户令牌构建完整的审批数据报表
public class ApprovalReportService
{
    private readonly IFeishuTenantV4ApprovalQuery _queryClient;

    public ApprovalReportService(IFeishuTenantV4ApprovalQuery queryClient)
    {
        _queryClient = queryClient;
    }

    public async Task<ApprovalReport> GenerateApprovalReportAsync(
        DateTime startDate,
        DateTime endDate)
    {
        var report = new ApprovalReport
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalInstances = new List<ApprovalInstance>(),
            TotalTasks = new List<ApprovalTask>()
        };

        var instanceRequest = new ApprovalInstancesQueryRequest
        {
            StartTime = startDate,
            EndTime = endDate
        };

        string? pageToken = null;
        do
        {
            var instanceResult = await _queryClient.GetInstancesPageListAsync(
                instanceRequest,
                page_size: 100,
                page_token: pageToken);

            if (instanceResult?.Code == 0)
            {
                report.TotalInstances.AddRange(instanceResult.Data?.Items ?? new List<ApprovalInstance>());
                pageToken = instanceResult.Data?.PageToken;
            }
        } while (!string.IsNullOrEmpty(pageToken));

        report.PendingCount = report.TotalInstances.Count(i => i.Status == "PENDING");
        report.ApprovedCount = report.TotalInstances.Count(i => i.Status == "APPROVED");
        report.RejectedCount = report.TotalInstances.Count(i => i.Status == "REJECTED");

        return report;
    }
}

public class ApprovalReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<ApprovalInstance> TotalInstances { get; set; }
    public List<ApprovalTask> TotalTasks { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
}
```
