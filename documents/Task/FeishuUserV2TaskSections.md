# 任务自定义分组 V2 - 用户权限

## 接口名称
**任务自定义分组 V2 -（IFeishuUserV2TaskSections）**

## 功能描述
飞书自定义分组可以方便地在"我负责的"和清单中对任务进行自定义归类。通过自定义分组，可以：
- 按状态分组：待启动-进行中-已完成
- 按优先级分组：P0-重要且紧急，P1-重要但不紧急，...
- 按类别分组：市场相关、人事相关，...

本接口提供以当前登录用户身份管理任务自定义分组的能力，与租户权限接口功能一致，但使用用户令牌进行认证。

## 参考文档
- [飞书开放平台 - 任务自定义分组概述](https://open.feishu.cn/document/task-v2/section/section-feature-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateTaskSectionsAsync | 创建自定义分组 | 用户令牌 | POST |
| UpdateSectionsAsync | 更新自定义分组 | 用户令牌 | PATCH |
| GetTaskSectionsByIdAsync | 获取自定义分组详情 | 用户令牌 | GET |
| DeleteTaskSectionsByIdAsync | 删除自定义分组 | 用户令牌 | DELETE |
| GetTaskSectionsPageListAsync | 列取自定义分组列表 | 用户令牌 | GET |
| GetTaskSectionsPageListByIdAsync | 获取自定义分组任务列表 | 用户令牌 | GET |

---

## 函数详细内容

### 创建自定义分组

**函数名称**：创建自定义分组

**函数签名**：
```csharp
Task<FeishuApiResult<TaskSectionsOperationResult>?> CreateTaskSectionsAsync(
    [Body] CreateTaskSectionsRequest createTaskSectionsRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| createTaskSectionsRequest | ✅ | CreateTaskSectionsRequest | 创建自定义分组请求体 |
| └ name | ✅ | string | 自定义分组名，不允许为空，最大100个utf8字符 |
| └ resource_type | ✅ | string | 资源类型，支持"tasklist"（清单）或"my_tasks"（我负责的） |
| └ resource_id | ⚪ | string | 资源ID，当resource_type为"tasklist"时必填清单GUID |
| └ insert_before | ⚪ | string | 将新分组插入到指定分组前面 |
| └ insert_after | ⚪ | string | 将新分组插入到指定分组后面 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 为清单或我负责的任务列表创建一个自定义分组
- 如果不指定位置，新分组会放到指定resource的自定义分组列表的最后

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "section": {
      "guid": "9842501a-9f47-4ff5-a622-d319eeecb97f",
      "name": "高优先级",
      "resource_type": "my_tasks",
      "is_default": false,
      "creator": {
        "id": "ou_xxx",
        "type": "user"
      },
      "created_at": "1675742789470",
      "updated_at": "1675742789470"
    }
  }
}
```

**代码示例**：
```csharp
// 使用用户权限创建自定义分组
public class UserTaskSectionsService
{
    private readonly IFeishuUserV2TaskSections _sectionsClient;

    public UserTaskSectionsService(IFeishuUserV2TaskSections sectionsClient)
    {
        _sectionsClient = sectionsClient;
    }

    // 为"我负责的"创建优先级分组
    public async Task CreateMyTaskSectionsAsync()
    {
        // 创建"高优先级"分组
        var highRequest = new CreateTaskSectionsRequest
        {
            Name = "高优先级",
            ResourceType = "my_tasks"
        };

        var highResult = await _sectionsClient.CreateTaskSectionsAsync(highRequest);
        Console.WriteLine($"高优先级分组创建成功: {highResult?.Data?.Section?.Guid}");

        // 创建"普通"分组
        var normalRequest = new CreateTaskSectionsRequest
        {
            Name = "普通",
            ResourceType = "my_tasks"
        };

        var normalResult = await _sectionsClient.CreateTaskSectionsAsync(normalRequest);
        Console.WriteLine($"普通分组创建成功: {normalResult?.Data?.Section?.Guid}");
    }

    // 为清单创建分组
    public async Task CreateTasklistSectionsAsync(string tasklistGuid)
    {
        var request = new CreateTaskSectionsRequest
        {
            Name = "我的关注",
            ResourceType = "tasklist",
            ResourceId = tasklistGuid
        };

        var result = await _sectionsClient.CreateTaskSectionsAsync(request);
        if (result?.Data?.Section != null)
        {
            Console.WriteLine($"分组创建成功: {result.Data.Section.Guid}");
        }
    }
}
```

---

### 更新自定义分组

**函数名称**：更新自定义分组

**函数签名**：
```csharp
Task<FeishuApiResult<UpdateTaskSectionsResult>?> UpdateSectionsAsync(
    [Path] string section_guid,
    [Body] UpdateTaskSectionsRequest updateTaskSectionsRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| section_guid | ✅ | string | 要更新的自定义分组GUID |
| updateTaskSectionsRequest | ✅ | UpdateTaskSectionsRequest | 更新自定义分组请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：更新自定义分组，可以更新自定义分组的名称和位置。

---

### 获取自定义分组详情

**函数名称**：获取自定义分组详情

**函数签名**：
```csharp
Task<FeishuApiResult<TaskSectionsOperationResult>?> GetTaskSectionsByIdAsync(
    [Path] string section_guid,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| section_guid | ✅ | string | 要获取的自定义分组GUID |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：获取一个自定义分组详情，包括名称、创建人等信息。

---

### 删除自定义分组

**函数名称**：删除自定义分组

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteTaskSectionsByIdAsync(
    [Path] string section_guid,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| section_guid | ✅ | string | 要删除的自定义分组GUID |

**说明**：
- 删除一个自定义分组
- 删除后该自定义分组中的任务会被移动到默认自定义分组中
- 不能删除默认的自定义分组

---

### 列取自定义分组列表

**函数名称**：列取自定义分组列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<SectionSummaryInfo>?> GetTaskSectionsPageListAsync(
    [Query("resource_type")] string resource_type,
    [Query("resource_id")] string? resource_id = null,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| resource_type | ✅ | string | 自定义分组所属的资源类型。支持my_tasks和tasklist |
| resource_id | ⚪ | string | 如resource_type为"tasklist"，需要填写清单的GUID |
| page_size | ⚪ | int | 分页大小，默认10 |
| page_token | ⚪ | string | 分页标记 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：分页获取自定义分组列表。

---

### 获取自定义分组任务列表

**函数名称**：获取自定义分组任务列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<TaskSummary>?> GetTaskSectionsPageListByIdAsync(
    [Path] string section_guid,
    [Query("completed")] bool? completed = null,
    [Query("created_from")] string? created_from = null,
    [Query("created_to")] string? created_to = null,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| section_guid | ✅ | string | 要获取的自定义分组GUID |
| completed | ⚪ | bool | 按照任务状态过滤 |
| created_from | ⚪ | string | 按照创建时间筛选的起始时间戳（ms) |
| created_to | ⚪ | string | 按照创建时间筛选的结束时间戳（ms) |
| page_size | ⚪ | int | 分页大小，默认10 |
| page_token | ⚪ | string | 分页标记 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 列取一个自定义分组里的所有任务
- 支持分页，任务按照自定义排序的顺序返回

**代码示例**：
```csharp
// 完整的个人自定义分组管理示例
public async Task ManagePersonalSectionsAsync()
{
    // 1. 为"我负责的"创建自定义分组
    var sections = new[] { "今日待办", "本周计划", "稍后处理" };
    var sectionGuids = new List<string>();

    foreach (var sectionName in sections)
    {
        var request = new CreateTaskSectionsRequest
        {
            Name = sectionName,
            ResourceType = "my_tasks"
        };

        var result = await _sectionsClient.CreateTaskSectionsAsync(request);
        if (result?.Data?.Section?.Guid != null)
        {
            sectionGuids.Add(result.Data.Section.Guid);
            Console.WriteLine($"创建个人分组: {sectionName}");
        }
    }

    // 2. 列取"我负责的"所有自定义分组
    var mySectionsResult = await _sectionsClient.GetTaskSectionsPageListAsync(
        resource_type: "my_tasks",
        page_size: 50
    );

    Console.WriteLine($"我的任务共有 {mySectionsResult?.Data?.Items?.Count ?? 0} 个分组");

    // 3. 获取"今日待办"分组的任务
    if (sectionGuids.Count > 0)
    {
        var todayTasks = await _sectionsClient.GetTaskSectionsPageListByIdAsync(
            sectionGuids[0],
            completed: false,
            page_size: 20
        );

        Console.WriteLine($"今日待办有 {todayTasks?.Data?.Items?.Count ?? 0} 个未完成任务");
    }

    // 4. 更新"本周计划"分组名称
    if (sectionGuids.Count > 1)
    {
        var updateRequest = new UpdateTaskSectionsRequest
        {
            Name = "本周重要计划",
            UpdateFields = new[] { "name" }
        };

        await _sectionsClient.UpdateSectionsAsync(sectionGuids[1], updateRequest);
    }

    // 5. 清理：删除创建的分组
    foreach (var guid in sectionGuids)
    {
        try
        {
            await _sectionsClient.DeleteTaskSectionsByIdAsync(guid);
            Console.WriteLine($"删除分组: {guid}");
        }
        catch
        {
            // 默认分组不能删除，忽略错误
        }
    }
}
```
