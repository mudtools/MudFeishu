# 任务清单动态订阅 V2 - 租户令牌

## 接口名称
**任务清单动态订阅 V2 -（IFeishuTenantV2TaskActivitySubscriptions）**

## 功能描述
任务清单动态订阅功能允许为任务清单创建订阅，当清单中发生特定事件时，系统会向订阅中的订阅者发送通知消息。

本接口提供以租户身份管理任务清单动态订阅的能力，包括创建、查询、更新和删除订阅等功能。

## 参考文档
- [飞书开放平台 - 任务清单动态订阅](https://open.feishu.cn/document/task-v2/tasklist-activity_subscription/create)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateActivitySubscriptionsAsync | 创建动态订阅 | 租户令牌 | POST |
| GetActivitySubscriptionsByIdAsync | 获取动态订阅详情 | 租户令牌 | GET |
| GetActivitySubscriptionsListByIdAsync | 列取动态订阅列表 | 租户令牌 | GET |
| UpdateActivitySubscriptionsByIdAsync | 更新动态订阅 | 租户令牌 | PATCH |
| DeleteActivitySubscriptionsByIdAsync | 删除动态订阅 | 租户令牌 | DELETE |

---

## 函数详细内容

### 创建动态订阅

**函数名称**：创建动态订阅

**函数签名**：
```csharp
Task<FeishuApiResult<TasklistActivitySubscriptionResult>?> CreateActivitySubscriptionsAsync(
    [Path] string tasklist_guid,
    [Body] CreateActivitySubscriptionsRequest createActivitySubscriptionsRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID，示例：d300a75f-c56a-4be9-80d1-e47653028ceb |
| createActivitySubscriptionsRequest | ✅ | CreateActivitySubscriptionsRequest | 创建动态订阅请求体 |
| └ name | ✅ | string | 订阅名称，不能为空，最大50个字符 |
| └ subscribers | ✅ | TaskListMember[] | 订阅者列表，目前只支持"chat"类型（普通群组） |
| └ include_keys | ✅ | int[] | 订阅的event key列表，详见下方说明 |
| └ disabled | ⚪ | bool | 该订阅是否为停用，默认false |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**include_keys 可选值**：

| Event Key | 事件描述 |
|----------|---------|
| 100 | 任务添加入清单 |
| 101 | 任务从清单被移除 |
| 103 | 任务被完成 |
| 104 | 任务恢复为未完成 |
| 109 | 任务添加了负责人 |
| 110 | 任务更新了负责人 |
| 111 | 任务移除了负责人 |
| 119 | 任务添加了附件 |
| 121 | 任务中添加了新评论 |
| 122 | 任务中对评论进行回复 |
| 129 | 任务设置了新的开始时间 |
| 130 | 任务设置了新的截止时间 |
| 131 | 任务同时设置了新的开始/截止时间 |
| 132 | 任务同时移除了开始/截止时间 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "activity_subscription": {
      "guid": "33991879-704f-444f-81d7-55a6aa7be80c",
      "name": "销售清单动态订阅",
      "subscribers": [
        {
          "id": "oc_xxx",
          "type": "chat"
        }
      ],
      "include_keys": [100, 103, 121],
      "disabled": false,
      "created_at": "1675742789470",
      "updated_at": "1675742789470"
    }
  }
}
```

**说明**：
- 每个订阅可以包含1个或多个订阅者（目前只支持普通群组）
- 订阅创建后，如清单发生相应的事件，则会向订阅里的订阅者发送通知消息
- 一个清单最多可以创建50个订阅
- 每个订阅最大支持50个订阅者

**代码示例**：
```csharp
// 使用租户令牌创建动态订阅
public class TaskActivitySubscriptionService
{
    private readonly IFeishuTenantV2TaskActivitySubscriptions _subscriptionClient;

    public TaskActivitySubscriptionService(IFeishuTenantV2TaskActivitySubscriptions subscriptionClient)
    {
        _subscriptionClient = subscriptionClient;
    }

    public async Task CreateSubscriptionAsync(string tasklistGuid)
    {
        var request = new CreateActivitySubscriptionsRequest
        {
            Name = "销售清单动态订阅",
            Subscribers = new[]
            {
                new TaskListMember
                {
                    Id = "oc_xxx",
                    Type = "chat"
                }
            },
            IncludeKeys = new[] { 100, 103, 121 }, // 任务添加、完成、新评论
            Disabled = false
        };

        var result = await _subscriptionClient.CreateActivitySubscriptionsAsync(
            tasklistGuid, 
            request
        );

        if (result?.Data?.ActivitySubscription != null)
        {
            Console.WriteLine($"订阅创建成功，GUID: {result.Data.ActivitySubscription.Guid}");
        }
    }
}
```

---

### 获取动态订阅详情

**函数名称**：获取动态订阅详情

**函数签名**：
```csharp
Task<FeishuApiResult<TasklistActivitySubscriptionResult>?> GetActivitySubscriptionsByIdAsync(
    [Path] string tasklist_guid,
    [Path] string activity_subscription_guid,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |
| activity_subscription_guid | ✅ | string | 订阅GUID，示例：33991879-704f-444f-81d7-55a6aa7be80c |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "activity_subscription": {
      "guid": "33991879-704f-444f-81d7-55a6aa7be80c",
      "name": "销售清单动态订阅",
      "subscribers": [
        {
          "id": "oc_xxx",
          "type": "chat"
        }
      ],
      "include_keys": [100, 103, 121],
      "disabled": false,
      "created_at": "1675742789470",
      "updated_at": "1675742789470"
    }
  }
}
```

**说明**：提供一个清单的GUID和一个订阅的GUID，获取该订阅的详细信息，包括名称、订阅者、可通知的event key列表等。

**代码示例**：
```csharp
// 获取动态订阅详情
public async Task GetSubscriptionDetailAsync(string tasklistGuid, string subscriptionGuid)
{
    var result = await _subscriptionClient.GetActivitySubscriptionsByIdAsync(
        tasklistGuid, 
        subscriptionGuid
    );

    if (result?.Data?.ActivitySubscription != null)
    {
        var sub = result.Data.ActivitySubscription;
        Console.WriteLine($"订阅名称: {sub.Name}");
        Console.WriteLine($"订阅者数: {sub.Subscribers?.Length ?? 0}");
        Console.WriteLine($"是否停用: {sub.Disabled}");
    }
}
```

---

### 列取动态订阅列表

**函数名称**：列取动态订阅列表

**函数签名**：
```csharp
Task<FeishuApiListResult<TasklistActivitySubscriptionInfo>?> GetActivitySubscriptionsListByIdAsync(
    [Path] string tasklist_guid,
    [Query("limit")] int limit = 50,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |
| limit | ⚪ | int | 返回结果的最大数量，默认50 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "guid": "33991879-704f-444f-81d7-55a6aa7be80c",
        "name": "销售清单动态订阅",
        "subscribers": [
          {
            "id": "oc_xxx",
            "type": "chat"
          }
        ],
        "include_keys": [100, 103, 121],
        "disabled": false,
        "created_at": "1675742789470",
        "updated_at": "1675742789470"
      },
      {
        "guid": "44291879-704f-444f-81d7-55a6aa7be90d",
        "name": "任务完成提醒",
        "subscribers": [
          {
            "id": "oc_yyy",
            "type": "chat"
          }
        ],
        "include_keys": [103],
        "disabled": false,
        "created_at": "1675742789500",
        "updated_at": "1675742789500"
      }
    ]
  }
}
```

**说明**：给定一个清单的GUID，获取其所有的订阅信息。结果按照订阅的创建时间排序。

**代码示例**：
```csharp
// 列取动态订阅列表
public async Task ListSubscriptionsAsync(string tasklistGuid)
{
    var result = await _subscriptionClient.GetActivitySubscriptionsListByIdAsync(
        tasklistGuid, 
        limit: 50
    );

    if (result?.Data?.Items != null)
    {
        foreach (var sub in result.Data.Items)
        {
            Console.WriteLine($"订阅: {sub.Name}, 事件数: {sub.IncludeKeys?.Length ?? 0}");
        }
    }
}
```

---

### 更新动态订阅

**函数名称**：更新动态订阅

**函数签名**：
```csharp
Task<FeishuApiResult<TasklistActivitySubscriptionResult>?> UpdateActivitySubscriptionsByIdAsync(
    [Path] string tasklist_guid,
    [Path] string activity_subscription_guid,
    [Body] UpdateActivitySubscriptionsRequest updateActivitySubscriptionsRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |
| activity_subscription_guid | ✅ | string | 订阅GUID |
| updateActivitySubscriptionsRequest | ✅ | UpdateActivitySubscriptionsRequest | 更新动态订阅请求体 |
| └ name | ⚪ | string | 订阅名称 |
| └ subscribers | ⚪ | TaskListMember[] | 订阅者列表 |
| └ include_keys | ⚪ | int[] | 订阅的event key列表 |
| └ disabled | ⚪ | bool | 是否停用 |
| └ update_fields | ✅ | string[] | 需要更新的字段名列表 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 提供一个清单的GUID和一个动态订阅的GUID，对其进行更新
- 更新时，将update_fields字段中填写所有要修改的字段名，同时在activity_subscription字段中填写要修改的字段的新值

**代码示例**：
```csharp
// 更新动态订阅
public async Task UpdateSubscriptionAsync(string tasklistGuid, string subscriptionGuid)
{
    var request = new UpdateActivitySubscriptionsRequest
    {
        Name = "更新后的订阅名称",
        IncludeKeys = new[] { 100, 103, 104, 121 },
        Disabled = false,
        UpdateFields = new[] { "name", "include_keys", "disabled" }
    };

    var result = await _subscriptionClient.UpdateActivitySubscriptionsByIdAsync(
        tasklistGuid, 
        subscriptionGuid, 
        request
    );

    if (result?.Code == 0)
    {
        Console.WriteLine("订阅更新成功");
    }
}
```

---

### 删除动态订阅

**函数名称**：删除动态订阅

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteActivitySubscriptionsByIdAsync(
    [Path] string tasklist_guid,
    [Path] string activity_subscription_guid,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| tasklist_guid | ✅ | string | 任务清单全局唯一GUID |
| activity_subscription_guid | ✅ | string | 订阅GUID |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：给定一个清单的GUID和一个订阅的GUID，将其删除。删除后的数据不可恢复。

**代码示例**：
```csharp
// 删除动态订阅
public async Task DeleteSubscriptionAsync(string tasklistGuid, string subscriptionGuid)
{
    var result = await _subscriptionClient.DeleteActivitySubscriptionsByIdAsync(
        tasklistGuid, 
        subscriptionGuid
    );

    if (result?.Code == 0)
    {
        Console.WriteLine("订阅删除成功");
    }
}
```
