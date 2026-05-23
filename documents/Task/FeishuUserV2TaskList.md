# 任务清单 V2 - 用户令牌

## 接口名称
**任务清单 V2 -（IFeishuUserV2TaskList）**

## 功能描述
飞书清单可以用于组织和管理属于同一个项目的多个任务。通过清单，团队可以将相关的任务归类到一起，方便统一管理和跟踪项目进度。

本接口提供以当前登录用户身份管理任务清单的能力，与租户权限接口功能一致，但使用用户令牌进行认证。

## 参考文档
- [飞书开放平台 - 任务清单概述](https://open.feishu.cn/document/task-v2/tasklist/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateTaskListAsync | 创建清单 | 用户令牌 | POST |
| GetTaskListByIdAsync | 获取清单详情 | 用户令牌 | GET |
| UpdateTaskListByIdAsync | 更新清单 | 用户令牌 | PATCH |
| DeleteTaskListByIdAsync | 删除清单 | 用户令牌 | DELETE |
| AddTaskListMemberByIdAsync | 添加清单成员 | 用户令牌 | POST |
| RemoveTaskListMemberByIdAsync | 移除清单成员 | 用户令牌 | POST |
| GetTaskListPageListByIdAsync | 分页获取清单任务列表 | 用户令牌 | GET |

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

**认证**：用户令牌

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
      "name": "个人工作清单",
      "creator": {
        "id": "ou_xxx",
        "type": "user"
      },
      "owner": {
        "id": "ou_xxx",
        "type": "user"
      },
      "members": [],
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
// 使用用户权限创建清单
public class UserTaskListService
{
    private readonly IFeishuUserV2TaskList _taskListClient;

    public UserTaskListService(IFeishuUserV2TaskList taskListClient)
    {
        _taskListClient = taskListClient;
    }

    public async Task CreatePersonalTaskListAsync()
    {
        var request = new CreateTaskListRequest
        {
            Name = "我的个人待办",
            Members = new[]
            {
                new TaskMember
                {
                    Id = "ou_colleague",
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

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：获取一个清单的详细信息，包括清单名、所有者、清单成员等。

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

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |
| updateTaskListRequest | ✅ | UpdateTaskListRequest | 更新任务列表请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：更新清单，可以更新清单的名字和所有者。

---

### 删除清单

**函数名称**：删除清单

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteTaskListByIdAsync(
    [Path] string tasklist_guid,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |

**说明**：删除一个清单。删除清单后，不可对该清单做任何操作，也无法再访问到清单。清单被删除后不可恢复。

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

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |
| addTaskListMemberRequest | ✅ | AddTaskListMemberRequest | 添加清单成员请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：向一个清单添加1个或多个协作成员。

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

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |
| removeTaskListMemberRequest | ✅ | RemoveTaskListMemberRequest | 移除清单成员请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：移除清单的一个或多个协作成员。

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

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |
| page_size | ⚪ | int | 分页大小，默认10 |
| page_token | ⚪ | string | 分页标记 |
| completed | ⚪ | bool | 特定完成状态的任务 |
| created_from | ⚪ | string | 任务创建的起始时间戳（ms） |
| created_to | ⚪ | string | 任务创建的结束时间戳（ms） |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：分页获取一个清单的任务列表，返回任务的摘要信息。

**代码示例**：
```csharp
// 完整的个人清单管理示例
public async Task ManagePersonalTaskListAsync()
{
    // 1. 创建个人清单
    var createRequest = new CreateTaskListRequest
    {
        Name = "本周工作计划",
        Members = new[]
        {
            new TaskMember { Id = "ou_teammate", Type = "user" }
        }
    };

    var createResult = await _taskListClient.CreateTaskListAsync(createRequest);
    if (createResult?.Data?.Tasklist == null)
    {
        Console.WriteLine("清单创建失败");
        return;
    }

    var tasklistGuid = createResult.Data.Tasklist.Guid;
    Console.WriteLine($"个人清单创建成功: {tasklistGuid}");

    // 2. 查看清单详情
    var detailResult = await _taskListClient.GetTaskListByIdAsync(tasklistGuid);
    if (detailResult?.Data?.Tasklist != null)
    {
        Console.WriteLine($"清单名称: {detailResult.Data.Tasklist.Name}");
        Console.WriteLine($"我是所有者: {detailResult.Data.Tasklist.Owner?.Id}");
    }

    // 3. 获取清单中的任务
    var tasksResult = await _taskListClient.GetTaskListPageListByIdAsync(
        tasklistGuid,
        page_size: 50,
        completed: null  // 获取所有任务
    );

    var tasks = tasksResult?.Data?.Items;
    var completedCount = tasks?.Count(t => t.Status == "done") ?? 0;
    var pendingCount = (tasks?.Count ?? 0) - completedCount;

    Console.WriteLine($"清单任务统计: {pendingCount} 个待办, {completedCount} 个已完成");

    // 4. 更新清单名称
    var updateRequest = new UpdateTaskListRequest
    {
        Name = "本周工作计划（已更新）",
        UpdateFields = new[] { "name" }
    };

    await _taskListClient.UpdateTaskListByIdAsync(tasklistGuid, updateRequest);

    // 5. 移除成员
    var removeRequest = new RemoveTaskListMemberRequest
    {
        Members = new[]
        {
            new TaskMember { Id = "ou_teammate", Type = "user" }
        }
    };

    await _taskListClient.RemoveTaskListMemberByIdAsync(tasklistGuid, removeRequest);
}
```
