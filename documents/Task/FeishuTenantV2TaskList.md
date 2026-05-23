# 任务清单 V2 - 租户令牌

## 接口名称
**任务清单 V2 -（IFeishuTenantV2TaskList）**

## 功能描述
飞书清单可以用于组织和管理属于同一个项目的多个任务。通过清单，团队可以将相关的任务归类到一起，方便统一管理和跟踪项目进度。

本接口提供以租户身份管理任务清单的能力，包括创建、更新、删除清单，管理清单成员，以及获取清单中的任务列表等功能。

## 参考文档
- [飞书开放平台 - 任务清单概述](https://open.feishu.cn/document/task-v2/tasklist/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateTaskListAsync | 创建清单 | 租户令牌 | POST |
| GetTaskListByIdAsync | 获取清单详情 | 租户令牌 | GET |
| UpdateTaskListByIdAsync | 更新清单 | 租户令牌 | PATCH |
| DeleteTaskListByIdAsync | 删除清单 | 租户令牌 | DELETE |
| AddTaskListMemberByIdAsync | 添加清单成员 | 租户令牌 | POST |
| RemoveTaskListMemberByIdAsync | 移除清单成员 | 租户令牌 | POST |
| GetTaskListPageListByIdAsync | 分页获取清单任务列表 | 租户令牌 | GET |

---

## 函数详细内容

### 创建清单

**函数名称**：创建清单

**函数签名**：
```csharp
Task<FeishuApiResult<TaskListOperationResult>?> CreateTaskListAsync(
    [Body] CreateTaskListRequest createTaskListRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| createTaskListRequest | ✅ | CreateTaskListRequest | 创建任务列表请求体 |
| └ name | ✅ | string | 清单名称，必填，最多100个字符 |
| └ members | ⚪ | TaskMember[] | 清单的成员列表 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "tasklist": {
      "guid": "cc371766-6584-cf50-a222-c22cd9055004",
      "name": "年会工作任务清单",
      "creator": {
        "id": "ou_xxx",
        "type": "user"
      },
      "owner": {
        "id": "ou_xxx",
        "type": "user"
      },
      "members": [
        {
          "id": "ou_yyy",
          "type": "user"
        }
      ],
      "url": "https://applink.feishu.cn/client/todo/task_list?guid=cc371766-6584-cf50-a222-c22cd9055004",
      "created_at": "1675742789470",
      "updated_at": "1675742789470"
    }
  }
}
```

**说明**：
- 创建一个清单，清单可以用于组织和管理属于同一个项目的多个任务
- 创建时，必须填写清单的名字
- 可以通过members字段设置清单的协作成员

**代码示例**：
```csharp
// 使用租户权限创建清单
public class TaskListService
{
    private readonly IFeishuTenantV2TaskList _taskListClient;

    public TaskListService(IFeishuTenantV2TaskList taskListClient)
    {
        _taskListClient = taskListClient;
    }

    public async Task CreateProjectTaskListAsync()
    {
        var request = new CreateTaskListRequest
        {
            Name = "Q1 产品迭代清单",
            Members = new[]
            {
                new TaskMember
                {
                    Id = "ou_yyy",
                    Type = "user"
                },
                new TaskMember
                {
                    Id = "ou_zzz",
                    Type = "user"
                }
            }
        };

        var result = await _taskListClient.CreateTaskListAsync(request);
        if (result?.Data?.Tasklist != null)
        {
            Console.WriteLine($"清单创建成功，GUID: {result.Data.Tasklist.Guid}");
            Console.WriteLine($"清单链接: {result.Data.Tasklist.Url}");
        }
    }
}
```

---

### 获取清单详情

**函数名称**：获取清单详情

**函数签名**：
```csharp
Task<FeishuApiResult<TaskListOperationResult>?> GetTaskListByIdAsync(
    [Path] string tasklist_guid,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID，示例：d300a75f-c56a-4be9-80d1-e47653028ceb |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "tasklist": {
      "guid": "d300a75f-c56a-4be9-80d1-e47653028ceb",
      "name": "年会工作任务清单",
      "creator": {
        "id": "ou_xxx",
        "type": "user"
      },
      "owner": {
        "id": "ou_xxx",
        "type": "user"
      },
      "members": [
        {
          "id": "ou_yyy",
          "type": "user"
        }
      ],
      "url": "https://applink.feishu.cn/client/todo/task_list?guid=d300a75f-c56a-4be9-80d1-e47653028ceb",
      "created_at": "1675742789470",
      "updated_at": "1675742789470"
    }
  }
}
```

**说明**：获取一个清单的详细信息，包括清单名、所有者、清单成员等。

**代码示例**：
```csharp
// 获取清单详情
public async Task GetTaskListDetailAsync(string tasklistGuid)
{
    var result = await _taskListClient.GetTaskListByIdAsync(tasklistGuid);
    if (result?.Data?.Tasklist != null)
    {
        var tasklist = result.Data.Tasklist;
        Console.WriteLine($"清单名称: {tasklist.Name}");
        Console.WriteLine($"创建者: {tasklist.Creator?.Id}");
        Console.WriteLine($"成员数: {tasklist.Members?.Length ?? 0}");
        Console.WriteLine($"清单链接: {tasklist.Url}");
    }
}
```

---

### 更新清单

**函数名称**：更新清单

**函数签名**：
```csharp
Task<FeishuApiResult<TaskListOperationResult>?> UpdateTaskListByIdAsync(
    [Path] string tasklist_guid,
    [Body] UpdateTaskListRequest updateTaskListRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |
| updateTaskListRequest | ✅ | UpdateTaskListRequest | 更新任务列表请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 更新清单，可以更新清单的名字和所有者
- 更新清单时，将update_fields字段中填写所有要修改的清单字段名，同时在tasklist字段中填写要修改的字段的新值

**代码示例**：
```csharp
// 更新清单
public async Task UpdateTaskListAsync(string tasklistGuid)
{
    var request = new UpdateTaskListRequest
    {
        Name = "Q1 产品迭代清单（已更新）",
        UpdateFields = new[] { "name" }
    };

    var result = await _taskListClient.UpdateTaskListByIdAsync(tasklistGuid, request);
    if (result?.Code == 0)
    {
        Console.WriteLine("清单更新成功");
    }
}
```

---

### 删除清单

**函数名称**：删除清单

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteTaskListByIdAsync(
    [Path] string tasklist_guid,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除一个清单。删除清单后，不可对该清单做任何操作，也无法再访问到清单。清单被删除后不可恢复。

**代码示例**：
```csharp
// 删除清单
public async Task DeleteTaskListAsync(string tasklistGuid)
{
    var result = await _taskListClient.DeleteTaskListByIdAsync(tasklistGuid);
    if (result?.Code == 0)
    {
        Console.WriteLine("清单删除成功");
    }
}
```

---

### 添加清单成员

**函数名称**：添加清单成员

**函数签名**：
```csharp
Task<FeishuApiResult<TaskListOperationResult>?> AddTaskListMemberByIdAsync(
    [Path] string tasklist_guid,
    [Body] AddTaskListMemberRequest addTaskListMemberRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |
| addTaskListMemberRequest | ✅ | AddTaskListMemberRequest | 添加清单成员请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：向一个清单添加1个或多个协作成员。成员信息通过设置members字段实现。

**代码示例**：
```csharp
// 添加清单成员
public async Task AddMembersToTaskListAsync(string tasklistGuid)
{
    var request = new AddTaskListMemberRequest
    {
        Members = new[]
        {
            new TaskMember
            {
                Id = "ou_new_member",
                Type = "user"
            }
        }
    };

    var result = await _taskListClient.AddTaskListMemberByIdAsync(tasklistGuid, request);
    if (result?.Code == 0)
    {
        Console.WriteLine("成员添加成功");
    }
}
```

---

### 移除清单成员

**函数名称**：移除清单成员

**函数签名**：
```csharp
Task<FeishuApiResult<TaskListOperationResult>?> RemoveTaskListMemberByIdAsync(
    [Path] string tasklist_guid,
    [Body] RemoveTaskListMemberRequest removeTaskListMemberRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |
| removeTaskListMemberRequest | ✅ | RemoveTaskListMemberRequest | 移除清单成员请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：移除清单的一个或多个协作成员。通过设置members字段表示要移除的成员信息。

---

### 分页获取清单任务列表

**函数名称**：分页获取清单任务列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<TaskSummary>?> GetTaskListPageListByIdAsync(
    [Path] string tasklist_guid,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("completed")] bool? completed = null,
    [Query("created_from")] string? created_from = null,
    [Query("created_to")] string? created_to = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |
| page_size | ⚪ | int | 分页大小，默认10 |
| page_token | ⚪ | string | 分页标记 |
| completed | ⚪ | bool | 特定完成状态的任务，true-已完成，false-未完成，不填表示不过滤 |
| created_from | ⚪ | string | 任务创建的起始时间戳（ms），闭区间 |
| created_to | ⚪ | string | 任务创建的结束时间戳（ms），闭区间 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "guid": "task-guid-1",
        "summary": "设计产品原型",
        "description": "完成Q1产品原型设计",
        "status": "todo",
        "completed_at": "0",
        "created_at": "1675742789000",
        "updated_at": "1675742789000"
      },
      {
        "guid": "task-guid-2",
        "summary": "开发前端页面",
        "description": "基于原型开发前端页面",
        "status": "done",
        "completed_at": "1675742789500",
        "created_at": "1675742789200",
        "updated_at": "1675742789500"
      }
    ],
    "page_token": "next_page_token",
    "has_more": true
  }
}
```

**说明**：分页获取一个清单的任务列表，返回任务的摘要信息。

**代码示例**：
```csharp
// 获取清单任务列表
public async Task GetTaskListTasksAsync(string tasklistGuid)
{
    // 获取未完成的任务
    var result = await _taskListClient.GetTaskListPageListByIdAsync(
        tasklistGuid,
        page_size: 20,
        completed: false
    );

    if (result?.Data?.Items != null)
    {
        Console.WriteLine($"未完成任务数: {result.Data.Items.Count}");
        foreach (var task in result.Data.Items)
        {
            Console.WriteLine($"- {task.Summary} ({task.Status})");
        }
    }
}
```

---

## 完整业务场景示例

```csharp
// 完整的清单管理流程示例
public async Task ManageTaskListAsync()
{
    // 1. 创建清单
    var createRequest = new CreateTaskListRequest
    {
        Name = "新产品发布项目",
        Members = new[]
        {
            new TaskMember { Id = "ou_member1", Type = "user" },
            new TaskMember { Id = "ou_member2", Type = "user" }
        }
    };

    var createResult = await _taskListClient.CreateTaskListAsync(createRequest);
    if (createResult?.Data?.Tasklist == null)
    {
        Console.WriteLine("清单创建失败");
        return;
    }

    var tasklistGuid = createResult.Data.Tasklist.Guid;
    Console.WriteLine($"清单创建成功: {tasklistGuid}");

    // 2. 获取清单详情
    var detailResult = await _taskListClient.GetTaskListByIdAsync(tasklistGuid);

    // 3. 添加更多成员
    var addMemberRequest = new AddTaskListMemberRequest
    {
        Members = new[]
        {
            new TaskMember { Id = "ou_member3", Type = "user" }
        }
    };

    await _taskListClient.AddTaskListMemberByIdAsync(tasklistGuid, addMemberRequest);

    // 4. 获取清单中的任务
    var tasksResult = await _taskListClient.GetTaskListPageListByIdAsync(
        tasklistGuid,
        page_size: 50
    );

    Console.WriteLine($"清单中共有 {tasksResult?.Data?.Items?.Count ?? 0} 个任务");

    // 5. 更新清单名称
    var updateRequest = new UpdateTaskListRequest
    {
        Name = "新产品发布项目（2024 Q1）",
        UpdateFields = new[] { "name" }
    };

    await _taskListClient.UpdateTaskListByIdAsync(tasklistGuid, updateRequest);

    // 6. 删除清单（清理）
    // await _taskListClient.DeleteTaskListByIdAsync(tasklistGuid);
}
```
