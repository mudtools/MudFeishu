# 任务评论 V2 - 用户令牌

## 接口名称
**任务评论 V2 -（IFeishuUserV2TaskComments）**

## 功能描述
任务评论功能实现评论创建、回复、更新、删除、获取详情等功能。通过评论功能，团队成员可以在任务下进行沟通交流，记录任务相关的讨论和决策过程。

本接口提供以当前登录用户身份管理任务评论的能力，与租户权限接口功能一致，但使用用户令牌进行认证。

## 参考文档
- [飞书开放平台 - 任务评论概述](https://open.feishu.cn/document/task-v2/comment/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateCommentAsync | 创建评论 | 用户令牌 | POST |
| GetCommentByIdAsync | 获取评论详情 | 用户令牌 | GET |
| UpdateCommentByIdAsync | 更新评论 | 用户令牌 | PATCH |
| DeleteCommentByIdAsync | 删除评论 | 用户令牌 | DELETE |
| GetCommentPageListAsync | 列取评论列表 | 用户令牌 | GET |

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

**认证**：用户令牌

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
// 使用用户权限创建评论
public class UserCommentService
{
    private readonly IFeishuUserV2TaskComments _commentClient;

    public UserCommentService(IFeishuUserV2TaskComments commentClient)
    {
        _commentClient = commentClient;
    }

    // 创建任务评论
    public async Task CreateTaskCommentAsync(string taskGuid)
    {
        var request = new CreateCommentRequest
        {
            Content = "我负责的部分已经完成，请大家 review",
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
            Content = "已 review，没有问题",
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

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| comment_id | ✅ | string | 评论ID |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：给定一个评论的ID，返回评论的详情，包括内容、创建人、创建时间和更新时间等信息。

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

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| comment_id | ✅ | string | 要更新评论的评论ID |
| updateCommentRequest | ✅ | UpdateCommentRequest | 更新评论请求体 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：更新一条评论。更新时，将update_fields字段中填写所有要修改的评论的字段名。

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

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| comment_id | ✅ | string | 要删除评论的评论ID |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：删除一条评论。评论被删除后，将无法进行任何操作，也无法恢复。

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

**认证**：用户令牌

**参数**：

| 参数名 | 必填 | 类型 | 说明 |
|-------|------|------|------|
| resource_id | ⚪ | string | 资源ID（任务GUID） |
| resource_type | ⚪ | string | 资源类型，默认"task" |
| direction | ⚪ | string | 排序方式："asc"或"desc"，默认"asc" |
| page_size | ⚪ | int | 分页大小，默认10 |
| page_token | ⚪ | string | 分页标记 |
| user_id_type | ⚪ | string | 用户ID类型，默认open_id |

**说明**：
- 给定一个资源，返回该资源的评论列表
- 支持分页，评论可以按照创建时间的正序或逆序返回数据

**代码示例**：
```csharp
// 列取任务评论列表
public async Task ListTaskCommentsAsync(string taskGuid)
{
    var result = await _commentClient.GetCommentPageListAsync(
        resource_id: taskGuid,
        resource_type: "task",
        direction: "desc",  // 最新评论在前
        page_size: 20
    );

    if (result?.Data?.Items != null)
    {
        foreach (var comment in result.Data.Items)
        {
            if (string.IsNullOrEmpty(comment.ReplyToCommentId))
            {
                Console.WriteLine($"[{comment.Creator?.Id}] {comment.Content}");
            }
            else
            {
                Console.WriteLine($"  └ [回复] [{comment.Creator?.Id}] {comment.Content}");
            }
        }
    }
}

// 完整的个人评论管理示例
public async Task PersonalCommentWorkflowAsync(string taskGuid)
{
    // 1. 查看任务现有评论
    var existingComments = await _commentClient.GetCommentPageListAsync(
        resource_id: taskGuid,
        resource_type: "task",
        page_size: 50
    );

    Console.WriteLine($"任务已有 {existingComments?.Data?.Items?.Count ?? 0} 条评论");

    // 2. 添加自己的评论
    var myComment = new CreateCommentRequest
    {
        Content = "我开始处理这个任务了，预计今天完成",
        ResourceType = "task",
        ResourceId = taskGuid
    };

    var createResult = await _commentClient.CreateCommentAsync(myComment);
    var myCommentId = createResult?.Data?.Comment?.Id;

    // 3. 如果需要修改评论
    if (!string.IsNullOrEmpty(myCommentId))
    {
        var updateRequest = new UpdateCommentRequest
        {
            Content = "我开始处理这个任务了，预计今天下午完成",
            UpdateFields = new[] { "content" }
        };

        await _commentClient.UpdateCommentByIdAsync(myCommentId, updateRequest);
    }

    // 4. 最后删除自己的评论（如果需要）
    if (!string.IsNullOrEmpty(myCommentId))
    {
        await _commentClient.DeleteCommentByIdAsync(myCommentId);
    }
}
```
