# 任务评论 V2 - 租户权限

## 接口名称
**任务评论 V2 -（IFeishuTenantV2TaskComments）**

## 功能描述
任务评论功能实现评论创建、回复、更新、删除、获取详情等功能。通过评论功能，团队成员可以在任务下进行沟通交流，记录任务相关的讨论和决策过程。

本接口提供以租户身份管理任务评论的能力。

## 参考文档
- [飞书开放平台 - 任务评论概述](https://open.feishu.cn/document/task-v2/comment/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateCommentAsync | 创建评论 | 租户令牌 | POST |
| GetCommentByIdAsync | 获取评论详情 | 租户令牌 | GET |
| UpdateCommentByIdAsync | 更新评论 | 租户令牌 | PATCH |
| DeleteCommentByIdAsync | 删除评论 | 租户令牌 | DELETE |
| GetCommentPageListAsync | 列取评论列表 | 租户令牌 | GET |

---

## 函数详细内容

### 创建评论

**函数名称**：创建评论

**函数签名**：
```csharp
Task<FeishuApiResult<CommentOpreationResult>?> CreateCommentAsync(
    [Body] CreateCommentRequest createCommentRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| createCommentRequest | ✅ | CreateCommentRequest | 创建评论请求体 |
| └ content | ✅ | string | 评论内容，最长3000个utf8字符 |
| └ reply_to_comment_id | ⚪ | string | 回复给评论的评论ID，不填表示创建非回复评论 |
| └ resource_type | ⚪ | string | 评论归属的资源类型，默认"task" |
| └ resource_id | ⚪ | string | 评论归属的资源ID（任务GUID） |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "comment": {
      "id": "7197020628442939411",
      "content": "这是一条评论",
      "creator": {
        "id": "ou_xxx",
        "type": "user"
      },
      "reply_to_comment_id": null,
      "created_at": "1675742789470",
      "updated_at": "1675742789470",
      "resource_type": "task",
      "resource_id": "ccb55625-95d2-2e80-655f-0e40bf67953f"
    }
  }
}
```

**说明**：
- 为一个任务创建评论，或者回复该任务的某个评论
- 若要创建一个回复评论，需要在创建时设置reply_to_comment_id字段
- 被回复的评论和新建的评论必须属于同一个任务

**代码示例**：
```csharp
// 使用租户权限创建评论
public class TaskCommentService
{
    private readonly IFeishuTenantV2TaskComments _commentClient;

    public TaskCommentService(IFeishuTenantV2TaskComments commentClient)
    {
        _commentClient = commentClient;
    }

    // 创建任务评论
    public async Task CreateTaskCommentAsync(string taskGuid)
    {
        var request = new CreateCommentRequest
        {
            Content = "这个任务需要尽快处理",
            ResourceType = "task",
            ResourceId = taskGuid
        };

        var result = await _commentClient.CreateCommentAsync(request);
        if (result?.Data?.Comment != null)
        {
            Console.WriteLine($"评论创建成功，ID: {result.Data.Comment.Id}");
        }
    }

    // 回复评论
    public async Task ReplyToCommentAsync(string taskGuid, string parentCommentId)
    {
        var request = new CreateCommentRequest
        {
            Content = "收到，我会优先处理",
            ResourceType = "task",
            ResourceId = taskGuid,
            ReplyToCommentId = parentCommentId
        };

        var result = await _commentClient.CreateCommentAsync(request);
        if (result?.Data?.Comment != null)
        {
            Console.WriteLine($"回复成功，ID: {result.Data.Comment.Id}");
        }
    }
}
```

---

### 获取评论详情

**函数名称**：获取评论详情

**函数签名**：
```csharp
Task<FeishuApiResult<CommentOpreationResult>?> GetCommentByIdAsync(
    [Path] string comment_id,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| comment_id | ✅ | string | 评论ID，示例：7198104824246747156 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "comment": {
      "id": "7198104824246747156",
      "content": "这是一条评论",
      "creator": {
        "id": "ou_xxx",
        "type": "user"
      },
      "reply_to_comment_id": null,
      "created_at": "1675742789470",
      "updated_at": "1675742789470",
      "resource_type": "task",
      "resource_id": "ccb55625-95d2-2e80-655f-0e40bf67953f"
    }
  }
}
```

**说明**：给定一个评论的ID，返回评论的详情，包括内容、创建人、创建时间和更新时间等信息。

**代码示例**：
```csharp
// 获取评论详情
public async Task GetCommentDetailAsync(string commentId)
{
    var result = await _commentClient.GetCommentByIdAsync(commentId);
    if (result?.Data?.Comment != null)
    {
        var comment = result.Data.Comment;
        Console.WriteLine($"评论内容: {comment.Content}");
        Console.WriteLine($"创建人: {comment.Creator?.Id}");
        Console.WriteLine($"创建时间: {comment.CreatedAt}");
    }
}
```

---

### 更新评论

**函数名称**：更新评论

**函数签名**：
```csharp
Task<FeishuApiResult<CommentOpreationResult>?> UpdateCommentByIdAsync(
    [Path] string comment_id,
    [Body] UpdateCommentRequest updateCommentRequest,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| comment_id | ✅ | string | 要更新评论的评论ID |
| updateCommentRequest | ✅ | UpdateCommentRequest | 更新评论请求体 |
| └ content | ✅ | string | 评论内容 |
| └ update_fields | ✅ | string[] | 需要更新的字段名列表 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 更新一条评论
- 更新时，将update_fields字段中填写所有要修改的评论的字段名，同时在comment字段中填写要修改的字段的新值

**代码示例**：
```csharp
// 更新评论
public async Task UpdateCommentAsync(string commentId)
{
    var request = new UpdateCommentRequest
    {
        Content = "更新后的评论内容",
        UpdateFields = new[] { "content" }
    };

    var result = await _commentClient.UpdateCommentByIdAsync(commentId, request);
    if (result?.Code == 0)
    {
        Console.WriteLine("评论更新成功");
    }
}
```

---

### 删除评论

**函数名称**：删除评论

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> DeleteCommentByIdAsync(
    [Path] string comment_id,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| comment_id | ✅ | string | 要删除评论的评论ID |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除一条评论。评论被删除后，将无法进行任何操作，也无法恢复。

**代码示例**：
```csharp
// 删除评论
public async Task DeleteCommentAsync(string commentId)
{
    var result = await _commentClient.DeleteCommentByIdAsync(commentId);
    if (result?.Code == 0)
    {
        Console.WriteLine("评论删除成功");
    }
}
```

---

### 列取评论列表

**函数名称**：列取评论列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<TaskCommentInfo>?> GetCommentPageListAsync(
    [Query("resource_id")] string? resource_id = null,
    [Query("resource_type")] string? resource_type = "task",
    [Query("direction")] string? direction = "asc",
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| resource_id | ⚪ | string | 资源ID（任务GUID），示例：d300a75f-c56a-4be9-80d1-e47653028ceb |
| resource_type | ⚪ | string | 资源类型，默认"task" |
| direction | ⚪ | string | 排序方式："asc"（最老到最新）或"desc"（最新到最老），默认"asc" |
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
        "id": "7197020628442939411",
        "content": "第一条评论",
        "creator": {
          "id": "ou_xxx",
          "type": "user"
        },
        "reply_to_comment_id": null,
        "created_at": "1675742789000",
        "updated_at": "1675742789000",
        "resource_type": "task",
        "resource_id": "ccb55625-95d2-2e80-655f-0e40bf67953f"
      },
      {
        "id": "7197020628442939412",
        "content": "回复第一条评论",
        "creator": {
          "id": "ou_yyy",
          "type": "user"
        },
        "reply_to_comment_id": "7197020628442939411",
        "created_at": "1675742789500",
        "updated_at": "1675742789500",
        "resource_type": "task",
        "resource_id": "ccb55625-95d2-2e80-655f-0e40bf67953f"
      }
    ],
    "page_token": "next_page_token",
    "has_more": true
  }
}
```

**说明**：
- 给定一个资源，返回该资源的评论列表
- 支持分页，评论可以按照创建时间的正序（asc）或逆序（desc）返回数据

**代码示例**：
```csharp
// 列取任务评论列表
public async Task ListTaskCommentsAsync(string taskGuid)
{
    var result = await _commentClient.GetCommentPageListAsync(
        resource_id: taskGuid,
        resource_type: "task",
        direction: "asc",
        page_size: 20
    );

    if (result?.Data?.Items != null)
    {
        foreach (var comment in result.Data.Items)
        {
            if (string.IsNullOrEmpty(comment.ReplyToCommentId))
            {
                Console.WriteLine($"[主评论] {comment.Content}");
            }
            else
            {
                Console.WriteLine($"  └ [回复] {comment.Content}");
            }
        }
    }
}
```

---

## 完整业务场景示例

```csharp
// 完整的评论管理流程示例
public async Task ManageTaskCommentsAsync(string taskGuid)
{
    // 1. 创建评论
    var createRequest = new CreateCommentRequest
    {
        Content = "请大家注意这个任务的截止日期",
        ResourceType = "task",
        ResourceId = taskGuid
    };

    var createResult = await _commentClient.CreateCommentAsync(createRequest);
    if (createResult?.Data?.Comment == null)
    {
        Console.WriteLine("评论创建失败");
        return;
    }

    var commentId = createResult.Data.Comment.Id;
    Console.WriteLine($"评论创建成功，ID: {commentId}");

    // 2. 回复评论
    var replyRequest = new CreateCommentRequest
    {
        Content = "收到，会按时完成",
        ResourceType = "task",
        ResourceId = taskGuid,
        ReplyToCommentId = commentId
    };

    var replyResult = await _commentClient.CreateCommentAsync(replyRequest);
    var replyId = replyResult?.Data?.Comment?.Id;

    // 3. 获取评论列表
    var listResult = await _commentClient.GetCommentPageListAsync(
        resource_id: taskGuid,
        resource_type: "task",
        direction: "asc",
        page_size: 50
    );

    if (listResult?.Data?.Items != null)
    {
        Console.WriteLine($"共有 {listResult.Data.Items.Count} 条评论");
    }

    // 4. 更新评论
    var updateRequest = new UpdateCommentRequest
    {
        Content = "请大家注意这个任务的截止日期，如有问题及时沟通",
        UpdateFields = new[] { "content" }
    };

    await _commentClient.UpdateCommentByIdAsync(commentId, updateRequest);

    // 5. 删除回复评论
    if (!string.IsNullOrEmpty(replyId))
    {
        await _commentClient.DeleteCommentByIdAsync(replyId);
        Console.WriteLine("回复评论已删除");
    }
}
```
