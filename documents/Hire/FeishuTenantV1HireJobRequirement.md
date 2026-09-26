# 招聘需求 - 租户令牌（FeishuTenantV1HireJobRequirement）

## 接口名称

**招聘需求（租户令牌）** -（`IFeishuTenantV1HireJobRequirement`）

## 功能描述

提供以租户身份管理飞书招聘需求的能力。飞书招聘（Hire）招聘需求 SDK 是一组服务端 OpenAPI 的封装，用于创建/更新/删除招聘需求、按 ID 或编号批量获取招聘需求、分页获取招聘需求列表以及获取招聘需求模板。本接口全部端点仅支持 tenant_access_token 调用。支持创建招聘需求、更新招聘需求、获取招聘需求信息、获取招聘需求列表、删除招聘需求、获取招聘需求模板等操作。

## 参考文档

- [招聘开发指南 - 飞书开放平台](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-development-guide)

## 函数列表

| 函数名称                          | 功能描述                       | 认证方式 | HTTP 方法 |
| --------------------------------- | ------------------------------ | -------- | --------- |
| CreateJobRequirementAsync         | 创建招聘需求                   | 租户令牌 | POST      |
| UpdateJobRequirementAsync         | 更新招聘需求                   | 租户令牌 | PUT       |
| SearchJobRequirementAsync         | 获取招聘需求信息（按 ID 批量查询） | 租户令牌 | POST   |
| GetJobRequirementListAsync        | 获取招聘需求列表               | 租户令牌 | GET       |
| DeleteJobRequirementAsync         | 删除招聘需求                   | 租户令牌 | DELETE    |
| GetJobRequirementSchemaListAsync  | 获取招聘需求模板               | 租户令牌 | GET       |

## 函数详细内容

### 创建招聘需求

创建招聘需求；除招聘需求编号（short_code）必填外，其余字段是否必填以飞书招聘「招聘需求字段管理」设置为准。限频：5 次/秒。所需权限：hire:job_requirement（更新招聘需求）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateJobRequirementResult>?> CreateJobRequirementAsync(
    [Body] CreateJobRequirementRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    [Query("employee_type_id_type")] string? employee_type_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                  | 类型                           | 必填 | 说明                                                                                                       |
| ----------------------- | ------------------------------ | ---- | ---------------------------------------------------------------------------------------------------------- |
| `request`               | `CreateJobRequirementRequest`  | ✅   | 创建请求体（short_code、name、display_progress、head_count 必填；recruitment_type_id 与 employee_type_id 必填其一） |
| `user_id_type`          | `string?`                      | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |
| `department_id_type`    | `string?`                      | ⚪   | 部门 ID 类型（open_department_id/department_id），默认 open_department_id                                 |
| `job_level_id_type`     | `string?`                      | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id                    |
| `job_family_id_type`    | `string?`                      | ⚪   | 职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id          |
| `employee_type_id_type` | `string?`                      | ⚪   | 人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_requirement": {}
  }
}
```

**说明**：`data.job_requirement` 为创建后的 `JobRequirement`；除 `short_code` 外其余字段的必填性以「招聘需求字段管理」设置为准。

---

### 更新招聘需求

按招聘需求 ID 更新需求名称、需求状态、需求人数等信息（审批中的招聘需求不可更新）。除文档标注必填字段外，其余字段是否必填以飞书招聘「招聘需求字段管理」设置为准。限频：5 次/秒。所需权限：hire:job_requirement（更新招聘需求）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UpdateJobRequirementAsync(
    [Path] string job_requirement_id,
    [Body] UpdateJobRequirementRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    [Query("employee_type_id_type")] string? employee_type_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                  | 类型                          | 必填 | 说明                                                                                                       |
| ----------------------- | ----------------------------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `job_requirement_id`    | `string`                      | ✅   | 招聘需求 ID，示例值：`623455234`                                                                           |
| `request`               | `UpdateJobRequirementRequest` | ✅   | 更新请求体（name、display_progress、head_count 必填；update_option 控制是否同步修改关联职位）               |
| `user_id_type`          | `string?`                     | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |
| `department_id_type`    | `string?`                     | ⚪   | 部门 ID 类型（open_department_id/department_id），默认 open_department_id                                 |
| `job_level_id_type`     | `string?`                     | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id                    |
| `job_family_id_type`    | `string?`                     | ⚪   | 职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id          |
| `employee_type_id_type` | `string?`                     | ⚪   | 人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：成功时 `data` 为空对象；审批中的招聘需求不可更新，可通过 `update_option` 控制是否同步修改关联职位。

---

### 获取招聘需求信息（按 ID 批量查询）

按招聘需求 ID 列表或需求编号列表批量获取招聘需求信息，单次最多 100 条；两种列表不可同时传入。审批中的招聘需求不返回。限频：10 次/秒。所需权限：hire:job_requirement:readonly（获取招聘需求）或 hire:job_requirement（更新招聘需求）。

**函数签名**：

```csharp
Task<FeishuApiResult<SearchJobRequirementResult>?> SearchJobRequirementAsync(
    [Body] SearchJobRequirementRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    [Query("employee_type_id_type")] string? employee_type_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                  | 类型                          | 必填 | 说明                                                                                                          |
| ----------------------- | ----------------------------- | ---- | ------------------------------------------------------------------------------------------------------------- |
| `request`               | `SearchJobRequirementRequest` | ✅   | 查询请求体（id_list 招聘需求 ID 列表与 short_code_list 需求编号列表二选一，单次上限 100 条；均不传时返回空）  |
| `user_id_type`          | `string?`                     | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |
| `department_id_type`    | `string?`                     | ⚪   | 部门 ID 类型（open_department_id/department_id），默认 open_department_id                                    |
| `job_level_id_type`     | `string?`                     | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id                       |
| `job_family_id_type`    | `string?`                     | ⚪   | 职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id             |
| `employee_type_id_type` | `string?`                     | ⚪   | 人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id  |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": []
  }
}
```

**说明**：`data.items` 为 `JobRequirement[]`；`id_list` 与 `short_code_list` 不可同时传入，单次上限 100 条，审批中的需求不返回。

---

### 获取招聘需求列表

按职位 ID、创建/更新时间范围等条件分页查询招聘需求列表（查询参数采用查询对象模式 `JobRequirementListQuery`，见 AGENTS.md API-2）。限频：1000 次/分钟、50 次/秒。所需权限：hire:job_requirement:readonly（获取招聘需求）或 hire:job_requirement（更新招聘需求）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetJobRequirementListResult>?> GetJobRequirementListAsync(
    [Query] JobRequirementListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型                         | 必填 | 说明                                                               |
| ------- | ---------------------------- | ---- | ------------------------------------------------------------------ |
| `query` | `JobRequirementListQuery?`   | ⚪   | 分页、职位 ID、创建/更新时间范围与各类 ID 类型查询参数，该对象会整体展开为查询参数 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "has_more": false,
    "page_token": "",
    "items": []
  }
}
```

**说明**：`data.items` 为 `JobRequirement[]`；翻页时传入上一次返回的 `page_token`，`has_more` 为 true 表示还有下一页。

---

### 删除招聘需求

删除指定招聘需求；已关联职位的招聘需求不可删除。限频：10 次/秒。所需权限：hire:job_requirement（更新招聘需求）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteJobRequirementAsync(
    [Path] string job_requirement_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名               | 类型     | 必填 | 说明                              |
| -------------------- | -------- | ---- | --------------------------------- |
| `job_requirement_id` | `string` | ✅   | 招聘需求 ID，示例值：`1616161616` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：成功时 `data` 为空对象；已关联职位的招聘需求不可删除，需先解除关联。

---

### 获取招聘需求模板

分页获取招聘需求模板列表，返回模板内各模块与字段的配置（含选项、是否必填等）。限频：10 次/秒。所需权限：hire:job_requirement:readonly（获取招聘需求）或 hire:job_requirement（更新招聘需求）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetJobRequirementSchemaListResult>?> GetJobRequirementSchemaListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                    |
| ------------ | --------- | ---- | ------------------------------------------------------- |
| `page_size`  | `int?`    | ⚪   | 每页数量，默认 10                                       |
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

**说明**：`data.items` 为 `JobRequirementSchema[]`，含模板内各模块与字段配置（选项、是否必填等）。
