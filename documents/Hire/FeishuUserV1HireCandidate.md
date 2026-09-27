# 候选人 - 用户令牌（FeishuUserV1HireCandidate）

## 接口名称

**候选人（用户令牌）** -（`IFeishuUserV1HireCandidate`）

## 功能描述

提供以用户身份获取飞书招聘待办事项的能力。飞书招聘（Hire）候选人入口域用户态 SDK 是一组服务端 OpenAPI 的封装，用于以用户身份批量获取招聘待办事项（评估/offer/笔试/面试待办）。本接口全部端点仅支持 user_access_token 调用（租户态备注、任务列表等能力见 `IFeishuTenantV1HireCandidate`）。接口实现 `ICurrentUserId`，待办事项归属于当前登录用户。支持批量获取待办事项等操作。

## 参考文档

- [招聘开发指南 - 飞书开放平台](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-development-guide)

## 函数列表

| 函数名称         | 功能描述         | 认证方式 | HTTP 方法 |
| ---------------- | ---------------- | -------- | --------- |
| GetTodoListAsync | 批量获取待办事项 | 用户令牌 | GET       |

## 函数详细内容

### 批量获取待办事项

批量获取当前用户的招聘待办事项信息，包含评估待办、Offer 待办、笔试待办和面试待办；type 决定返回的待办类别。限频：10 次/秒。所需权限：hire:todo:readonly（访问待办事项）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。仅支持 user_access_token 调用。

**函数签名**：

```csharp
Task<FeishuApiResult<GetTodoListResult>?> GetTodoListAsync(
    [Query("type")] string type,
    [Query("page_token")] string? page_token = null,
    [Query("page_size")] int? page_size = null,
    [Query("user_id")] string? user_id = null,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                            |
| -------------- | --------- | ---- | ----------------------------------------------------------------------------------------------- |
| `type`         | `string`  | ✅   | 待办类型：evaluation-待评估，offer-待办 Offer，exam-待笔试，interview-待面试                    |
| `page_token`   | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                         |
| `page_size`    | `int?`    | ⚪   | 每页数量，默认 10，最大 100                                                                     |
| `user_id`      | `string?` | ⚪   | 用户 ID，用户令牌调用时无需传入                                                                 |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 people_admin_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [],
    "has_more": false,
    "page_token": ""
  }
}
```

**说明**：`data.items` 为 `Todo[]`；`type` 决定返回的待办类别，用户令牌调用时无需传入 `user_id`，待办归属于当前用户。
