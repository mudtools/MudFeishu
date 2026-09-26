# 面试设置 - 租户令牌（FeishuTenantV1HireInterview）

## 接口名称

**面试设置（租户令牌）** -（`IFeishuTenantV1HireInterview`）

## 功能描述

提供以租户身份管理飞书招聘面试配置的能力。飞书招聘（Hire）面试域 SDK 是一组服务端 OpenAPI 的封装，用于招聘配置中的面试轮次类型、面试反馈表、面试登记表模板的查询与面试官认证信息的维护。本接口全部端点仅支持 tenant_access_token 调用（投递流程中的面试信息与评价记录等见 `IFeishuTenantV1HireCandidate`）。支持获取面试轮次类型列表、获取面试反馈表、获取面试登记表模板列表、获取面试官信息列表、更新面试官信息等操作。

## 参考文档

- [招聘开发指南 - 飞书开放平台](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-development-guide)

## 函数列表

| 函数名称                              | 功能描述                   | 认证方式 | HTTP 方法 |
| ------------------------------------- | -------------------------- | -------- | --------- |
| GetInterviewRoundTypeListAsync        | 获取面试轮次类型列表       | 租户令牌 | GET       |
| GetInterviewFeedbackFormListAsync     | 获取面试反馈表             | 租户令牌 | GET       |
| GetInterviewRegistrationSchemaListAsync | 获取面试登记表模板列表   | 租户令牌 | GET       |
| GetInterviewerListAsync               | 获取面试官信息列表         | 租户令牌 | GET       |
| PatchInterviewerAsync                 | 更新面试官信息             | 租户令牌 | PATCH     |

## 函数详细内容

### 获取面试轮次类型列表

获取面试轮次类型列表，返回轮次类型名称、流程类型、启用状态及关联的面试评价表。限频：1000 次/分钟、50 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetInterviewRoundTypeListResult>?> GetInterviewRoundTypeListAsync(
    [Query("process_type")] int? process_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名          | 类型   | 必填 | 说明                                                  |
| --------------- | ------ | ---- | ----------------------------------------------------- |
| `process_type`  | `int?` | ⚪   | 职位流程类型：1 社会招聘流程 / 2 校园招聘流程         |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "active_status": 0,
    "items": []
  }
}
```

**说明**：`data.items` 为 `InterviewRoundType[]` 面试轮次类型列表，`active_status` 为启用状态。

---

### 获取面试反馈表

分页获取面试反馈表详情，包括问题描述、问题选项、打分配置等；传入 interview_feedback_form_ids 时按 ID 精确查询并忽略其他参数。限频：10 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetInterviewFeedbackFormListResult>?> GetInterviewFeedbackFormListAsync(
    [Query("interview_feedback_form_ids")] string[]? interview_feedback_form_ids = null,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                         | 类型        | 必填 | 说明                                                    |
| ------------------------------ | ----------- | ---- | ------------------------------------------------------- |
| `interview_feedback_form_ids`  | `string[]?` | ⚪   | 面试反馈表 ID 列表，传入该参数时其他查询参数被忽略，最多 100 个 |
| `page_size`                    | `int?`      | ⚪   | 每页数量                                                |
| `page_token`                   | `string?`   | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [],
    "page_token": "",
    "has_more": false
  }
}
```

**说明**：`data.items` 为 `InterviewFeedbackForm[]`；传入 `interview_feedback_form_ids` 时不再分页，最多 100 个 ID。

---

### 获取面试登记表模板列表

分页获取面试登记表模板列表，返回模板名称、是否作为全局面试登记表及模块字段配置。限频：1000 次/分钟、50 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetInterviewRegistrationSchemaListResult>?> GetInterviewRegistrationSchemaListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                    |
| ------------ | --------- | ---- | ------------------------------------------------------- |
| `page_size`  | `int?`    | ⚪   | 每页数量，最大 10，默认 10                              |
| `page_token` | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token |

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

**说明**：`data.items` 为 `InterviewRegistrationSchema[]`，含模板名称、是否作为全局面试登记表及模块字段配置。

---

### 获取面试官信息列表

分页查询面试官认证信息；仅返回已通过「更新面试官信息」接口写入的数据，未写入的用户默认不返回（查询参数采用查询对象模式 `InterviewerListQuery`，见 AGENTS.md API-2）。默认按更新时间、user_id 排序。限频：20 次/秒。所需权限：hire:interviewer（管理面试官信息）或 hire:interviewer:readonly（获取面试官信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetInterviewerListResult>?> GetInterviewerListAsync(
    [Query] InterviewerListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型                    | 必填 | 说明                                                                                     |
| ------- | ----------------------- | ---- | ---------------------------------------------------------------------------------------- |
| `query` | `InterviewerListQuery?` | ⚪   | 分页、面试官 user_id 列表、认证状态、更新时间范围与用户 ID 类型查询参数，该对象会整体展开为查询参数 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [],
    "page_token": "",
    "has_more": false
  }
}
```

**说明**：`data.items` 为 `Interviewer[]`；仅返回已通过「更新面试官信息」写入认证状态的面试官，未写入的用户不返回。

---

### 更新面试官信息

更新指定面试官的认证状态（1 未认证 / 2 已认证）。限频：20 次/秒。所需权限：hire:interviewer（管理面试官信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<PatchInterviewerResult>?> PatchInterviewerAsync(
    [Path] string interviewer_id,
    [Body] PatchInterviewerRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名            | 类型                    | 必填 | 说明                                                                                    |
| ----------------- | ----------------------- | ---- | --------------------------------------------------------------------------------------- |
| `interviewer_id`  | `string`                | ✅   | 面试官 userID，示例值：`ou_7dab8a3d3cdcc9da365777c7ad535d62`                            |
| `request`         | `PatchInterviewerRequest` | ✅   | 更新请求体（interviewer 必填，含 verify_status 认证状态）                              |
| `user_id_type`    | `string?`               | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "interviewer": {}
  }
}
```

**说明**：`data.interviewer` 为更新后的 `Interviewer`；`verify_status` 取 1 未认证、2 已认证。
