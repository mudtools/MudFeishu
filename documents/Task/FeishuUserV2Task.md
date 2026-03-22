# 飞书任务 V2 - 用户权限

## 接口名称
**飞书任务 V2 -（IFeishuUserV2Task）**

## 功能描述
飞书任务是一款飞书自带的通用任务/项目管理工具，拥有强大的协作能力。可以轻松地在飞书App的任务中心、群组、文档等场景中快捷创建任务，同时也可以将任务分享给感兴趣的成员，或者关注和跟进一些感兴趣的任务。

本接口提供以当前登录用户身份操作飞书任务的完整能力，与租户权限接口功能一致，但使用用户令牌进行认证。额外支持基于调用身份分页列出特定类型的所有任务。

## 参考文档
- [飞书开放平台 - 任务 V2 概述](https://open.feishu.cn/document/task-v2/task/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| GetTasksPageListByIdAsync | 分页获取任务列表 | 用户令牌 | GET |
| CreateTaskAsync | 创建任务 | 用户令牌 | POST |
| UpdateTaskAsync | 更新任务 | 用户令牌 | PATCH |
| GetTaskByIdAsync | 获取任务详情 | 用户令牌 | GET |
| DeleteTaskByIdAsync | 删除任务 | 用户令牌 | DELETE |
| AddMembersByIdAsync | 添加任务成员 | 用户令牌 | POST |
| RemoveMembersByIdAsync | 移除任务成员 | 用户令牌 | POST |
| GetTaskListsByIdAsync | 列取任务所在清单 | 用户令牌 | GET |
| AddTaskListsByIdAsync | 将任务加入清单 | 用户令牌 | POST |
| RemoveTaskListsByIdAsync | 将任务从清单移出 | 用户令牌 | POST |
| AddTaskReminderByIdAsync | 添加任务提醒 | 用户令牌 | POST |
| RemoveTaskReminderByIdAsync | 移除任务提醒 | 用户令牌 | POST |
| AddTaskDependenciesByIdAsync | 添加任务依赖 | 用户令牌 | POST |
| RemoveTaskDependenciesByIdAsync | 移除任务依赖 | 用户令牌 | POST |
| CreateSubTaskAsync | 创建子任务 | 用户令牌 | POST |
| GetSubTasksPageListByIdAsync | 分页获取子任务列表 | 用户令牌 | GET |

---

## 函数详细内容

### 分页获取任务列表

**函数名称**：分页获取任务列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<ListTaskInfo>?> GetTasksPageListByIdAsync(
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("completed")] bool? completed = null,
    [Query("type")] string? type = "my_tasks",
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| page_size | ⚪ | int | 分页大小，默认10 |
| page_token | ⚪ | string | 分页标记，第一次请求不填 |
| completed | ⚪ | bool | 是否包含已完成任务，默认不包含 |
| type | ⚪ | string | 列取任务的类型，目前只支持"my_tasks"（我负责的） |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "guid": "83912691-2e43-47fc-94a4-d512e03984fa",
        "summary": "进行销售年中总结",
        "description": "需要整理上半年的销售数据",
        "due": {
          "timestamp": "1675742789470",
          "is_all_day": false
        },
        "status": "todo",
        "members": [
          {
            "id": "ou_xxx",
            "type": "user",
            "role": "assignee"
          }
        ],
        "tasklists": [
          {
            "guid": "d300a75f-c56a-4be9-80d1-e47653028ceb",
            "name": "销售部门任务"
          }
        ],
        "created_at": "1675742789470",
        "updated_at": "1675742789470",
        "completed_at": "0",
        "subtask_count": 3,
        "url": "https://applink.feishu.cn/client/todo/detail?guid=83912691-2e43-47fc-94a4-d512e03984fa"
      }
    ],
    "page_token": "next_page_token",
    "has_more": true
  }
}
```

**说明**：
- 基于调用身份，分页列出特定类型的所有任务
- 目前只支持列取任务界面上"我负责的"任务
- 返回的任务数据按照任务在"我负责的"界面中"自定义拖拽"的顺序排序

**代码示例**：
```csharp
// 使用用户权限获取任务列表
public class UserTaskService
{
    private readonly IFeishuUserV2Task _taskClient;

    public UserTaskService(IFeishuUserV2Task taskClient)
    {
        _taskClient = taskClient;
    }

    public async Task GetMyTasksAsync()
    {
        var result = await _taskClient.GetTasksPageListByIdAsync(
            page_size: 20,
            completed: false,
            type: "my_tasks"
        );

        if (result?.Data?.Items != null)
        {
            foreach (var task in result.Data.Items)
            {
                Console.WriteLine($"任务: {task.Summary}, 状态: {task.Status}");
            }
        }
    }

    // 分页遍历所有任务
    public async Task GetAllTasksAsync()
    {
        string? pageToken = null;
        do
        {
            var result = await _taskClient.GetTasksPageListByIdAsync(
                page_size: 50,
                page_token: pageToken,
                completed: null
            );

            if (result?.Data?.Items != null)
            {
                foreach (var task in result.Data.Items)
                {
                    Console.WriteLine($"任务: {task.Summary}");
                }
            }

            pageToken = result?.Data?.PageToken;
        } while (!string.IsNullOrEmpty(pageToken));
    }
}
```

---

### 创建任务

**函数名称**：创建任务

**函数签名**：
```csharp
Task<FeishuApiResult<TaskOperationResult>?> CreateTaskAsync(
    [Body] CreateTaskRequest createTaskRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| createTaskRequest | ✅ | CreateTaskRequest | 创建任务请求体 |
| └ summary | ✅ | string | 任务标题，最大3000个utf8字符 |
| └ description | ⚪ | string | 任务摘要，最大3000个utf8字符 |
| └ due | ⚪ | TaskTime | 任务截止时间 |
| └ members | ⚪ | TaskMemberInfo[] | 任务成员列表（负责人和关注人） |
| └ tasklists | ⚪ | TaskInTaskListInfo[] | 任务所在清单信息 |
| └ reminders | ⚪ | TaskReminder[] | 任务提醒设置 |
| └ origin | ⚪ | TaskOriginSrcData | 第三方平台来源信息 |
| └ extra | ⚪ | string | 附带的自定义数据 |
| └ completed_at | ⚪ | string | 完成时刻时间戳(ms)，0表示未完成 |
| └ repeat_rule | ⚪ | string | 重复任务规则 |
| └ custom_complete | ⚪ | TaskPlatformCompleteData | 自定义完成配置 |
| └ client_token | ⚪ | string | 幂等token |
| └ start | ⚪ | TasksStartTime | 任务开始时间 |
| └ mode | ⚪ | int | 完成模式：1-会签任务，2-或签任务 |
| └ is_milestone | ⚪ | bool | 是否是里程碑任务 |
| └ custom_fields | ⚪ | InputCustomFieldValue[] | 自定义字段值 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "task": {
      "guid": "e297ddff-06ca-4166-b917-4ce57cd3a7a0",
      "summary": "针对全年销售进行一次复盘",
      "description": "需要事先阅读复盘总结文档",
      "due": {
        "timestamp": "1675742789470",
        "is_all_day": false
      },
      "members": [
        {
          "id": "ou_xxx",
          "type": "user",
          "role": "assignee"
        }
      ],
      "status": "todo",
      "created_at": "1675742789470",
      "updated_at": "1675742789470",
      "source": 7,
      "url": "https://applink.feishu.cn/client/todo/detail?guid=e297ddff-06ca-4166-b917-4ce57cd3a7a0"
    }
  }
}
```

**说明**：
- 创建任务时支持设置任务的基本信息（如标题、描述、负责人等）
- 可以通过传入 tasklists 字段将新任务加到多个清单中
- 可以通过设置 members 字段来设置任务的负责人和关注人
- 以用户令牌创建的任务，source字段值为7

**代码示例**：
```csharp
// 使用用户权限创建任务
public async Task CreatePersonalTaskAsync()
{
    var request = new CreateTaskRequest
    {
        Summary = "个人待办事项",
        Description = "这是一个个人任务",
        Due = new TaskTime
        {
            Timestamp = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeMilliseconds().ToString(),
            IsAllDay = false
        },
        Members = new[]
        {
            new TaskMemberInfo
            {
                Id = "ou_xxx",
                Type = "user",
                Role = "assignee"
            }
        }
    };

    var result = await _taskClient.CreateTaskAsync(request);
    if (result?.Data?.Task != null)
    {
        Console.WriteLine($"任务创建成功，GUID: {result.Data.Task.Guid}");
    }
}
```

---

### 更新任务

**函数名称**：更新任务

**函数签名**：
```csharp
Task<FeishuApiResult<TaskOperationResult>?> UpdateTaskAsync(
    [Path] string task_guid,
    [Body] UpdateTaskRequest updateTaskRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| task_guid | ✅ | string | 任务全局唯一ID |
| updateTaskRequest | ✅ | UpdateTaskRequest | 更新任务请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：用于修改任务的标题、描述、截止时间等信息。

---

### 获取任务详情

**函数名称**：获取任务详情

**函数签名**：
```csharp
Task<FeishuApiResult<TaskOperationResult>?> GetTaskByIdAsync(
    [Path] string task_guid,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| task_guid | ✅ | string | 任务全局唯一ID |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：用于获取任务详情，包括任务标题、描述、时间、成员等信息。

---

### 删除任务

**函数名称**：删除任务

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteTaskByIdAsync(
    [Path] string task_guid,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| task_guid | ✅ | string | 要删除的任务全局唯一ID |

**说明**：删除一个任务。删除后任务无法再被获取到。

---

### 添加任务成员

**函数名称**：添加任务成员

**函数签名**：
```csharp
Task<FeishuApiResult<TaskOperationResult>?> AddMembersByIdAsync(
    [Path] string task_guid,
    [Body] AddMembersRequest addMembersRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| task_guid | ✅ | string | 任务全局唯一ID |
| addMembersRequest | ✅ | AddMembersRequest | 添加任务成员请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：添加任务的负责人或者关注人。一次性可以添加多个成员。

---

### 移除任务成员

**函数名称**：移除任务成员

**函数签名**：
```csharp
Task<FeishuApiResult<TaskOperationResult>?> RemoveMembersByIdAsync(
    [Path] string task_guid,
    [Body] RemoveMembersRequest removeMembersRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| task_guid | ✅ | string | 任务全局唯一ID |
| removeMembersRequest | ✅ | RemoveMembersRequest | 移除任务成员请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：移除任务成员。如果要移除的成员不是任务成员，会被自动忽略。

---

### 列取任务所在清单

**函数名称**：列取任务所在清单

**函数签名**：
```csharp
Task<FeishuApiResult<TaskGuidTaskListsResult>?> GetTaskListsByIdAsync(
    [Path] string task_guid,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| task_guid | ✅ | string | 任务全局唯一ID |

**说明**：列取一个任务所在的所有清单的信息。

---

### 将任务加入清单

**函数名称**：将任务加入清单

**函数签名**：
```csharp
Task<FeishuApiResult<AddTaskListResult>?> AddTaskListsByIdAsync(
    [Path] string task_guid,
    [Body] AddTasklistRequest addTasklistRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| task_guid | ✅ | string | 任务全局唯一ID |
| addTasklistRequest | ✅ | AddTasklistRequest | 任务加入清单请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：将一个任务加入清单。如果任务已经在该清单，接口将返回成功。

---

### 将任务从清单移出

**函数名称**：将任务从清单移出

**函数签名**：
```csharp
Task<FeishuApiResult<RemoveTaskListResult>?> RemoveTaskListsByIdAsync(
    [Path] string task_guid,
    [Body] RemoveTasklistRequest removeTasklistRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| task_guid | ✅ | string | 任务全局唯一ID |
| removeTasklistRequest | ✅ | RemoveTasklistRequest | 移除任务清单请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：将任务从一个清单中移出。如果任务不在清单中，接口将返回成功。

---

### 添加任务提醒

**函数名称**：添加任务提醒

**函数签名**：
```csharp
Task<FeishuApiResult<AddTaskReminderResult>?> AddTaskReminderByIdAsync(
    [Path] string task_guid,
    [Body] AddTaskReminderRequest addTaskReminderRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| task_guid | ✅ | string | 任务全局唯一ID |
| addTaskReminderRequest | ✅ | AddTaskReminderRequest | 添加任务提醒请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：为一个任务添加提醒。为了设置提醒，任务必须首先拥有截止时间。

---

### 移除任务提醒

**函数名称**：移除任务提醒

**函数签名**：
```csharp
Task<FeishuApiResult<RemoveTaskReminderResult>?> RemoveTaskReminderByIdAsync(
    [Path] string task_guid,
    [Body] RemoveReminderRequest removeReminderRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| task_guid | ✅ | string | 任务全局唯一ID |
| removeReminderRequest | ✅ | RemoveReminderRequest | 移除任务提醒请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：将一个提醒从任务中移除。

---

### 添加任务依赖

**函数名称**：添加任务依赖

**函数签名**：
```csharp
Task<FeishuApiResult<TaskDependenciesOpreationResult>?> AddTaskDependenciesByIdAsync(
    [Path] string task_guid,
    [Body] AddTaskDependenciesRequest addTaskReminderRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| task_guid | ✅ | string | 任务全局唯一ID |
| addTaskReminderRequest | ✅ | AddTaskDependenciesRequest | 添加任务依赖请求体 |

**说明**：为一个任务添加一个或多个依赖。可以添加任务的前置依赖和后置依赖。

---

### 移除任务依赖

**函数名称**：移除任务依赖

**函数签名**：
```csharp
Task<FeishuApiResult<TaskDependenciesOpreationResult>?> RemoveTaskDependenciesByIdAsync(
    [Path] string task_guid,
    [Body] RemoveTaskDependenciesRequest removeTaskDependenciesRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| task_guid | ✅ | string | 任务全局唯一ID |
| removeTaskDependenciesRequest | ✅ | RemoveTaskDependenciesRequest | 移除任务依赖请求体 |

**说明**：从一个任务移除一个或者多个依赖。

---

### 创建子任务

**函数名称**：创建子任务

**函数签名**：
```csharp
Task<FeishuApiResult<CreateSubTaskResult>?> CreateSubTaskAsync(
    [Path] string task_guid,
    [Body] CreateSubTaskRequest createSubTaskRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| task_guid | ✅ | string | 父任务全局唯一ID |
| createSubTaskRequest | ✅ | CreateSubTaskRequest | 创建子任务请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：给一个任务创建一个子任务。

---

### 分页获取子任务列表

**函数名称**：分页获取子任务列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<SubTaskInfo>?> GetSubTasksPageListByIdAsync(
    [Path] string task_guid,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| task_guid | ✅ | string | 任务全局唯一ID |
| page_size | ⚪ | int | 分页大小，默认10 |
| page_token | ⚪ | string | 分页标记 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：分页获取一个任务的子任务列表。

**代码示例**：
```csharp
// 完整的任务管理示例
public async Task ManageTasksAsync()
{
    // 1. 获取我的任务列表
    var tasks = await _taskClient.GetTasksPageListByIdAsync(
        page_size: 10,
        completed: false,
        type: "my_tasks"
    );

    if (tasks?.Data?.Items == null || tasks.Data.Items.Count == 0)
    {
        Console.WriteLine("暂无我负责的任务");
        return;
    }

    var firstTask = tasks.Data.Items[0];
    Console.WriteLine($"选中任务: {firstTask.Summary}");

    // 2. 获取任务详情
    var taskDetail = await _taskClient.GetTaskByIdAsync(firstTask.Guid!);
    if (taskDetail?.Data?.Task != null)
    {
        Console.WriteLine($"任务描述: {taskDetail.Data.Task.Description}");
        Console.WriteLine($"子任务数: {taskDetail.Data.Task.SubtaskCount}");
    }

    // 3. 获取子任务列表
    if (firstTask.SubtaskCount > 0)
    {
        var subTasks = await _taskClient.GetSubTasksPageListByIdAsync(
            firstTask.Guid!,
            page_size: 20
        );

        if (subTasks?.Data?.Items != null)
        {
            foreach (var subTask in subTasks.Data.Items)
            {
                Console.WriteLine($"  - 子任务: {subTask.Summary}");
            }
        }
    }
}
```
