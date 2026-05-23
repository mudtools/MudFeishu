# 任务自定义分组 V2 - 租户令牌

## 接口名称
**任务自定义分组 V2 -（IFeishuTenantV2TaskSections）**

## 功能描述
飞书自定义分组可以方便地在"我负责的"和清单中对任务进行自定义归类。通过自定义分组，可以：
- 按状态分组：待启动-进行中-已完成
- 按优先级分组：P0-重要且紧急，P1-重要但不紧急，...
- 按类别分组：市场相关、人事相关，...

本接口提供以租户身份管理任务自定义分组的能力。

## 参考文档
- [飞书开放平台 - 任务自定义分组概述](https://open.feishu.cn/document/task-v2/section/section-feature-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateTaskSectionsAsync | 创建自定义分组 | 租户令牌 | POST |
| UpdateSectionsAsync | 更新自定义分组 | 租户令牌 | PATCH |
| GetTaskSectionsByIdAsync | 获取自定义分组详情 | 租户令牌 | GET |
| DeleteTaskSectionsByIdAsync | 删除自定义分组 | 租户令牌 | DELETE |
| GetTaskSectionsPageListAsync | 列取自定义分组列表 | 租户令牌 | GET |
| GetTaskSectionsPageListByIdAsync | 获取自定义分组任务列表 | 租户令牌 | GET |

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

**认证**：租户令牌

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
- 创建时可以需要提供名称和可选的配置
- 如果不指定位置（insert_before/insert_after），新分组会放到指定resource的自定义分组列表的最后
- insert_before和insert_after不能同时设置

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "section": {
      "guid": "9842501a-9f47-4ff5-a622-d319eeecb97f",
      "name": "待审核任务",
      "resource_type": "tasklist",
      "is_default": false,
      "creator": {
        "id": "ou_xxx",
        "type": "user"
      },
      "tasklist": {
        "guid": "cc371766-6584-cf50-a222-c22cd9055004",
        "name": "年会工作任务清单"
      },
      "created_at": "1675742789470",
      "updated_at": "1675742789470"
    }
  }
}
```

**代码示例**：
```csharp
// 使用租户令牌创建自定义分组
public class TaskSectionsService
{
    private readonly IFeishuTenantV2TaskSections _sectionsClient;

    public TaskSectionsService(IFeishuTenantV2TaskSections sectionsClient)
    {
        _sectionsClient = sectionsClient;
    }

    // 为清单创建状态分组
    public async Task CreateStatusSectionsAsync(string tasklistGuid)
    {
        // 创建"待启动"分组
        var todoRequest = new CreateTaskSectionsRequest
        {
            Name = "待启动",
            ResourceType = "tasklist",
            ResourceId = tasklistGuid
        };

        var todoResult = await _sectionsClient.CreateTaskSectionsAsync(todoRequest);
        Console.WriteLine($"待启动分组创建成功: {todoResult?.Data?.Section?.Guid}");

        // 创建"进行中"分组
        var doingRequest = new CreateTaskSectionsRequest
        {
            Name = "进行中",
            ResourceType = "tasklist",
            ResourceId = tasklistGuid
        };

        var doingResult = await _sectionsClient.CreateTaskSectionsAsync(doingRequest);
        Console.WriteLine($"进行中分组创建成功: {doingResult?.Data?.Section?.Guid}");

        // 创建"已完成"分组
        var doneRequest = new CreateTaskSectionsRequest
        {
            Name = "已完成",
            ResourceType = "tasklist",
            ResourceId = tasklistGuid
        };

        var doneResult = await _sectionsClient.CreateTaskSectionsAsync(doneRequest);
        Console.WriteLine($"已完成分组创建成功: {doneResult?.Data?.Section?.Guid}");
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

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| section_guid | ✅ | string | 要更新的自定义分组GUID，示例：9842501a-9f47-4ff5-a622-d319eeecb97f |
| updateTaskSectionsRequest | ✅ | UpdateTaskSectionsRequest | 更新自定义分组请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 更新自定义分组，可以更新自定义分组的名称和位置
- 更新时，将update_fields字段中填写所有要修改的字段名，同时在section字段中填写要修改的字段的新值

**代码示例**：
```csharp
// 更新自定义分组
public async Task UpdateSectionAsync(string sectionGuid)
{
    var request = new UpdateTaskSectionsRequest
    {
        Name = "已审核任务（已更新）",
        UpdateFields = new[] { "name" }
    };

    var result = await _sectionsClient.UpdateSectionsAsync(sectionGuid, request);
    if (result?.Code == 0)
    {
        Console.WriteLine("自定义分组更新成功");
    }
}
```

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

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| section_guid | ✅ | string | 要获取的自定义分组GUID |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "section": {
      "guid": "9842501a-9f47-4ff5-a622-d319eeecb97f",
      "name": "待审核任务",
      "resource_type": "tasklist",
      "is_default": false,
      "creator": {
        "id": "ou_xxx",
        "type": "user"
      },
      "tasklist": {
        "guid": "cc371766-6584-cf50-a222-c22cd9055004",
        "name": "年会工作任务清单"
      },
      "created_at": "1675742789470",
      "updated_at": "1675742789470"
    }
  }
}
```

**说明**：获取一个自定义分组详情，包括名称、创建人等信息。如果该自定义分组归属于一个清单，还会返回清单的摘要信息。

**代码示例**：
```csharp
// 获取自定义分组详情
public async Task GetSectionDetailAsync(string sectionGuid)
{
    var result = await _sectionsClient.GetTaskSectionsByIdAsync(sectionGuid);
    if (result?.Data?.Section != null)
    {
        var section = result.Data.Section;
        Console.WriteLine($"分组名称: {section.Name}");
        Console.WriteLine($"资源类型: {section.ResourceType}");
        Console.WriteLine($"是否默认: {section.IsDefault}");
        if (section.Tasklist != null)
        {
            Console.WriteLine($"所属清单: {section.Tasklist.Name}");
        }
    }
}
```

---

### 删除自定义分组

**函数名称**：删除自定义分组

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteTaskSectionsByIdAsync(
    [Path] string section_guid,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| section_guid | ✅ | string | 要删除的自定义分组GUID |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：
- 删除一个自定义分组
- 删除后该自定义分组中的任务会被移动到被删除自定义分组所属资源的默认自定义分组中
- 不能删除默认的自定义分组

**代码示例**：
```csharp
// 删除自定义分组
public async Task DeleteSectionAsync(string sectionGuid)
{
    var result = await _sectionsClient.DeleteTaskSectionsByIdAsync(sectionGuid);
    if (result?.Code == 0)
    {
        Console.WriteLine("自定义分组删除成功，其中的任务已移动到默认分组");
    }
}
```

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

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| resource_type | ✅ | string | 自定义分组所属的资源类型。支持my_tasks(我负责的)和tasklist(清单) |
| resource_id | ⚪ | string | 如resource_type为"tasklist"，这里需要填写要列取自定义分组的清单的GUID |
| page_size | ⚪ | int | 分页大小，默认10 |
| page_token | ⚪ | string | 分页标记 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "guid": "section-guid-1",
        "name": "待启动",
        "is_default": false
      },
      {
        "guid": "section-guid-2",
        "name": "进行中",
        "is_default": false
      },
      {
        "guid": "section-guid-3",
        "name": "已完成",
        "is_default": false
      },
      {
        "guid": "section-guid-default",
        "name": "未分组",
        "is_default": true
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**：分页获取自定义分组列表。

**代码示例**：
```csharp
// 列取清单的自定义分组
public async Task ListTasklistSectionsAsync(string tasklistGuid)
{
    var result = await _sectionsClient.GetTaskSectionsPageListAsync(
        resource_type: "tasklist",
        resource_id: tasklistGuid,
        page_size: 50
    );

    if (result?.Data?.Items != null)
    {
        foreach (var section in result.Data.Items)
        {
            var isDefault = section.IsDefault == true ? "[默认]" : "";
            Console.WriteLine($"分组: {section.Name} {isDefault}");
        }
    }
}
```

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

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| section_guid | ✅ | string | 要获取的自定义分组GUID |
| completed | ⚪ | bool | 按照任务状态过滤，true-已完成，false-未完成，不填表示不过滤 |
| created_from | ⚪ | string | 按照创建时间筛选的起始时间戳（ms) |
| created_to | ⚪ | string | 按照创建时间筛选的结束时间戳（ms) |
| page_size | ⚪ | int | 分页大小，默认10 |
| page_token | ⚪ | string | 分页标记 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 列取一个自定义分组里的所有任务
- 支持分页，任务按照自定义排序的顺序返回
- 本接口支持简单的过滤

**代码示例**：
```csharp
// 获取分组中的未完成任务
public async Task GetSectionPendingTasksAsync(string sectionGuid)
{
    var result = await _sectionsClient.GetTaskSectionsPageListByIdAsync(
        sectionGuid,
        completed: false,
        page_size: 20
    );

    if (result?.Data?.Items != null)
    {
        Console.WriteLine($"未完成任务数: {result.Data.Items.Count}");
        foreach (var task in result.Data.Items)
        {
            Console.WriteLine($"- {task.Summary}");
        }
    }
}
```

---

## 完整业务场景示例

```csharp
// 完整的自定义分组管理流程示例
public async Task ManageTaskSectionsAsync(string tasklistGuid)
{
    // 1. 创建多个自定义分组
    var sections = new[] { "待启动", "开发中", "测试中", "已发布" };
    var sectionGuids = new List<string>();

    foreach (var sectionName in sections)
    {
        var request = new CreateTaskSectionsRequest
        {
            Name = sectionName,
            ResourceType = "tasklist",
            ResourceId = tasklistGuid
        };

        var result = await _sectionsClient.CreateTaskSectionsAsync(request);
        if (result?.Data?.Section?.Guid != null)
        {
            sectionGuids.Add(result.Data.Section.Guid);
            Console.WriteLine($"创建分组: {sectionName}");
        }
    }

    // 2. 列取所有自定义分组
    var listResult = await _sectionsClient.GetTaskSectionsPageListAsync(
        resource_type: "tasklist",
        resource_id: tasklistGuid
    );

    Console.WriteLine($"共有 {listResult?.Data?.Items?.Count ?? 0} 个分组");

    // 3. 获取第一个分组的任务
    if (sectionGuids.Count > 0)
    {
        var tasksResult = await _sectionsClient.GetTaskSectionsPageListByIdAsync(
            sectionGuids[0],
            page_size: 50
        );

        Console.WriteLine($"第一个分组有 {tasksResult?.Data?.Items?.Count ?? 0} 个任务");
    }

    // 4. 更新最后一个分组的名称
    if (sectionGuids.Count > 0)
    {
        var updateRequest = new UpdateTaskSectionsRequest
        {
            Name = "已发布（已上线）",
            UpdateFields = new[] { "name" }
        };

        await _sectionsClient.UpdateSectionsAsync(sectionGuids[^1], updateRequest);
    }

    // 5. 清理：删除创建的分组（保留默认分组）
    foreach (var guid in sectionGuids)
    {
        await _sectionsClient.DeleteTaskSectionsByIdAsync(guid);
    }
}
```
