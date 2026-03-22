# 审批评论接口 -（FeishuV4ApprovalComments_Tenant）

## 接口名称

审批评论接口 -（`IFeishuTenantV4ApprovalComments`）

## 功能描述

该接口用于管理原生审批实例内的评论功能。原生审批实例内，支持员工进行评论、回复评论。评论内容支持文本、@用户以及添加附件。

## 参考文档

- [飞书官方文档 - 审批评论](https://open.feishu.cn/document/server-docs/approval-v4/instance-comment/overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| `CreateCommentAsync` | 创建/修改/回复评论 | 租户令牌 | POST |
| `DeleteCommentByIdAsync` | 删除指定评论 | 租户令牌 | DELETE |
| `RemoveCommentsAsync` | 清空全部评论 | 租户令牌 | POST |
| `GetCommentsPageListByIdAsync` | 获取评论分页列表 | 租户令牌 | GET |

## 函数详细内容

### 创建/修改/回复评论

#### 函数签名

```csharp
Task<FeishuApiResult<CommentOperationResult>?> CreateCommentAsync(
    [Path] string instance_id,
    [Query("user_id")] string user_id,
    [Body] CreateCommentRequest createApprovalRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名 | 必填 | 类型 | 描述 |
|-------|-----|------|-----|
| `instance_id` | ✅ | `string` | 审批实例 Code，支持传入自定义审批实例 ID，示例值：`"6A123516-FB88-470D-A428-9AF58B71B3C0"` |
| `user_id` | ✅ | `string` | 用户 ID，ID 类型与 user_id_type 取值一致，示例值：`"e5286g26"` |
| `createApprovalRequest` | ✅ | `CreateCommentRequest` | 创建评论请求体 |
| `user_id_type` | ⚪ | `string` | 用户 ID 类型，默认：`open_id` |
| `cancellationToken` | ⚪ | `CancellationToken` | 取消操作令牌对象 |

**请求体示例：**

```json
{
  "content": "这个审批需要尽快处理",
  "comment_id": "",
  "parent_comment_id": "",
  "mentions": [
    {
      "key": "@user",
      "id": "ou_7dab8a3d3dfcd10xxx"
    }
  ],
  "attachments": [
    {
      "file_token": "file_v2_xxx"
    }
  ]
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "comment_id": "7081516627711606803",
    "create_time": "2025-03-20T10:30:00+08:00"
  }
}
```

#### 说明

- 在指定审批实例下创建、修改评论或回复评论
- 不包含审批同意、拒绝、转交等附加的理由或意见
- 如需修改评论，传入已存在的 `comment_id`
- 如需回复评论，传入 `parent_comment_id` 指定父评论

#### 代码示例

```csharp
// 创建审批评论
var instanceId = "6A123516-FB88-470D-A428-9AF58B71B3C0";
var userId = "ou_7dab8a3d3dfcd10xxx";

var request = new CreateCommentRequest
{
    Content = "这个审批需要尽快处理，@主管请查看",
    Mentions = new List<Mention>
    {
        new Mention
        {
            Key = "@主管",
            Id = "ou_8eab9b4e4egde21yyy"
        }
    }
};

var result = await _feishuApi
    .UseApp(appId, appSecret)
    .ExecuteAsync(api => api.CreateCommentAsync(instanceId, userId, request));

if (result?.Code == 0)
{
    Console.WriteLine($"评论创建成功，ID: {result.Data?.CommentId}");
}
```

---

### 删除指定评论

#### 函数签名

```csharp
Task<FeishuApiResult<CommentOperationResult>?> DeleteCommentByIdAsync(
    [Path] string instance_id,
    [Path] string comment_id,
    [Query("user_id")] string user_id,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名 | 必填 | 类型 | 描述 |
|-------|-----|------|-----|
| `instance_id` | ✅ | `string` | 审批实例 Code，示例值：`"6A123516-FB88-470D-A428-9AF58B71B3C0"` |
| `comment_id` | ✅ | `string` | 评论 ID，示例值：`"7081516627711606803"` |
| `user_id` | ✅ | `string` | 用户 ID，ID 类型与 user_id_type 取值一致，示例值：`"e5286g26"` |
| `user_id_type` | ⚪ | `string` | 用户 ID 类型，默认：`open_id` |
| `cancellationToken` | ⚪ | `CancellationToken` | 取消操作令牌对象 |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "comment_id": "7081516627711606803",
    "delete_time": "2025-03-20T10:35:00+08:00"
  }
}
```

#### 说明

- 删除某审批实例下的一条评论或评论回复
- 不包含审批同意、拒绝、转交等附加的理由或意见
- 删除后在审批中心的审批实例内不再显示评论内容，而是显示"评论已删除"

#### 代码示例

```csharp
// 删除指定评论
var instanceId = "6A123516-FB88-470D-A428-9AF58B71B3C0";
var commentId = "7081516627711606803";
var userId = "ou_7dab8a3d3dfcd10xxx";

var result = await _feishuApi
    .UseApp(appId, appSecret)
    .ExecuteAsync(api => api.DeleteCommentByIdAsync(instanceId, commentId, userId));

if (result?.Code == 0)
{
    Console.WriteLine("评论删除成功");
}
```

---

### 清空全部评论

#### 函数签名

```csharp
Task<FeishuApiResult<CommentsRemoveResult>?> RemoveCommentsAsync(
    [Path] string instance_id,
    [Query("user_id")] string user_id,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名 | 必填 | 类型 | 描述 |
|-------|-----|------|-----|
| `instance_id` | ✅ | `string` | 审批实例 Code，示例值：`"6A123516-FB88-470D-A428-9AF58B71B3C0"` |
| `user_id` | ✅ | `string` | 用户 ID，ID 类型与 user_id_type 取值一致，示例值：`"e5286g26"` |
| `user_id_type` | ⚪ | `string` | 用户 ID 类型，默认：`open_id` |
| `cancellationToken` | ⚪ | `CancellationToken` | 取消操作令牌对象 |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "removed_count": 5
  }
}
```

#### 说明

- 清空某审批实例下的全部评论与评论回复
- 包括显示为已删除的评论

#### 代码示例

```csharp
// 清空审批实例的所有评论
var instanceId = "6A123516-FB88-470D-A428-9AF58B71B3C0";
var userId = "ou_7dab8a3d3dfcd10xxx";

var result = await _feishuApi
    .UseApp(appId, appSecret)
    .ExecuteAsync(api => api.RemoveCommentsAsync(instanceId, userId));

if (result?.Code == 0)
{
    Console.WriteLine($"已清空 {result.Data?.RemovedCount} 条评论");
}
```

---

### 获取评论分页列表

#### 函数签名

```csharp
Task<FeishuApiResult<CommentsPageListResult>?> GetCommentsPageListByIdAsync(
    [Path] string instance_id,
    [Query("user_id")] string user_id,
    [Query("page_size")] int page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名 | 必填 | 类型 | 描述 |
|-------|-----|------|-----|
| `instance_id` | ✅ | `string` | 审批实例 Code，示例值：`"6A123516-FB88-470D-A428-9AF58B71B3C0"` |
| `user_id` | ✅ | `string` | 用户 ID，ID 类型与 user_id_type 取值一致，示例值：`"e5286g26"` |
| `page_size` | ⚪ | `int` | 分页大小，默认：10 |
| `page_token` | ⚪ | `string` | 分页标记，第一次请求不填 |
| `user_id_type` | ⚪ | `string` | 用户 ID 类型，默认：`open_id` |
| `cancellationToken` | ⚪ | `CancellationToken` | 取消操作令牌对象 |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "items": [
      {
        "comment_id": "7081516627711606803",
        "open_id": "ou_7dab8a3d3dfcd10xxx",
        "content": "这个审批需要尽快处理",
        "create_time": "2025-03-20T10:30:00+08:00",
        "parent_comment_id": "",
        "is_deleted": false
      },
      {
        "comment_id": "7081516627711606804",
        "open_id": "ou_8eab9b4e4egde21yyy",
        "content": "已处理",
        "create_time": "2025-03-20T10:35:00+08:00",
        "parent_comment_id": "7081516627711606803",
        "is_deleted": false
      }
    ],
    "page_token": "",
    "has_more": false
  }
}
```

#### 说明

- 根据审批实例 Code 获取某个审批实例下全部评论与评论回复
- 不包含审批同意、拒绝、转交等附加的理由或意见

#### 代码示例

```csharp
// 获取审批实例的评论列表
var instanceId = "6A123516-FB88-470D-A428-9AF58B71B3C0";
var userId = "ou_7dab8a3d3dfcd10xxx";

var result = await _feishuApi
    .UseApp(appId, appSecret)
    .ExecuteAsync(api => api.GetCommentsPageListByIdAsync(
        instanceId, 
        userId, 
        page_size: 20));

if (result?.Code == 0)
{
    foreach (var comment in result.Data?.Items ?? new List<CommentItem>())
    {
        Console.WriteLine($"[{comment.CreateTime}] {comment.Content}");
    }
}
```
