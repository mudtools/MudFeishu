# 职位管理 - 租户令牌（FeishuTenantV1HireJob）

## 接口名称

**职位管理（租户令牌）** -（`IFeishuTenantV1HireJob`）

## 功能描述

提供以租户身份管理飞书招聘职位的能力。飞书招聘（Hire）职位域 SDK 是一组服务端 OpenAPI 的封装，用于职位的组合创建/更新与设置维护、职位管理人员批量维护、职位详情与列表查询、职位开放，以及职位类别、职能分类、职位模板、职位发布记录与职位广告发布。本接口全部端点仅支持 tenant_access_token 调用。支持组合创建职位、组合更新职位、更新职位设置、批量更新职位管理人员、获取职位信息、获取职位详情、获取职位设置、获取职位列表、开放职位、获取职位发布人、获取职位类别列表、获取职位职能分类列表、获取职位模板列表、查询职位发布记录、发布职位广告等操作。

## 参考文档

- [招聘开发指南 - 飞书开放平台](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-development-guide)

## 函数列表

| 函数名称                        | 功能描述                 | 认证方式 | HTTP 方法 |
| ------------------------------- | ------------------------ | -------- | --------- |
| CombinedCreateJobAsync          | 组合创建职位             | 租户令牌 | POST      |
| CombinedUpdateJobAsync          | 组合更新职位             | 租户令牌 | POST      |
| UpdateJobConfigAsync            | 更新职位设置             | 租户令牌 | POST      |
| BatchUpdateJobManagerAsync      | 批量更新职位管理人员     | 租户令牌 | POST      |
| GetJobAsync                     | 获取职位信息             | 租户令牌 | GET       |
| GetJobDetailAsync               | 获取职位详情             | 租户令牌 | GET       |
| GetJobConfigAsync               | 获取职位设置             | 租户令牌 | GET       |
| GetJobListAsync                 | 获取职位列表             | 租户令牌 | GET       |
| OpenJobAsync                    | 开放职位                 | 租户令牌 | POST      |
| GetJobRecruiterAsync            | 获取职位发布人           | 租户令牌 | GET       |
| GetJobTypeListAsync             | 获取职位类别列表         | 租户令牌 | GET       |
| GetJobFunctionListAsync         | 获取职位职能分类列表     | 租户令牌 | GET       |
| GetJobSchemaListAsync           | 获取职位模板列表         | 租户令牌 | GET       |
| SearchJobPublishRecordAsync     | 查询职位发布记录         | 租户令牌 | POST      |
| PublishAdvertisementAsync       | 发布职位广告             | 租户令牌 | POST      |

## 函数详细内容

### 组合创建职位

一次性提交职位基础信息、职位管理人员与自定义字段等完整信息创建职位，返回职位、职位管理人员、默认职位广告与登记表信息。限频：20 次/秒。所需权限：hire:job（更新职位）。

**函数签名**：

```csharp
Task<FeishuApiResult<CombinedJobResult>?> CombinedCreateJobAsync(
    [Body] CombinedJobRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名               | 类型                | 必填 | 说明                                                                                                       |
| -------------------- | ------------------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `request`            | `CombinedJobRequest` | ✅   | 组合创建请求体（title 职位名称、department_id、job_process_id、job_type_id 等按需填写；job_managers 含 recruiter_id/hiring_manager_id_list） |
| `user_id_type`       | `string?`           | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |
| `department_id_type` | `string?`           | ⚪   | 部门 ID 类型（open_department_id/department_id），默认 open_department_id                                 |
| `job_level_id_type`  | `string?`           | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id                    |
| `job_family_id_type` | `string?`           | ⚪   | 职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id          |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "default_job_post": {},
    "job": {},
    "job_manager": {},
    "interview_registration_schema_info": {},
    "onboard_registration_schema_info": {},
    "target_major_list": [],
    "portal_website_apply_form_schema_info": {}
  }
}
```

**说明**：一步完成职位主体、职位管理人员与登记表配置；返回的 `default_job_post.id` 可作为发布职位广告的职位广告 ID。

---

### 组合更新职位

按职位 ID 全量更新职位信息；未填写的字段会被清空，请提交完整职位数据。限频：10 次/秒。所需权限：hire:job（更新职位）。

**函数签名**：

```csharp
Task<FeishuApiResult<CombinedJobResult>?> CombinedUpdateJobAsync(
    [Path] string job_id,
    [Body] CombinedJobRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名               | 类型                | 必填 | 说明                                                                                           |
| -------------------- | ------------------- | ---- | ---------------------------------------------------------------------------------------------- |
| `job_id`             | `string`            | ✅   | 职位 ID，示例值：`6960663240925956660`                                                         |
| `request`            | `CombinedJobRequest` | ✅   | 组合更新请求体（title、job_managers 必填；其余字段未传即清空）                                |
| `user_id_type`       | `string?`           | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |
| `department_id_type` | `string?`           | ⚪   | 部门 ID 类型（open_department_id/department_id），默认 open_department_id                     |
| `job_level_id_type`  | `string?`           | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id         |
| `job_family_id_type` | `string?`           | ⚪   | 职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "default_job_post": {},
    "job": {},
    "job_manager": {},
    "interview_registration_schema_info": {},
    "onboard_registration_schema_info": {},
    "target_major_list": [],
    "portal_website_apply_form_schema_info": {}
  }
}
```

**说明**：为全量更新，未提交的字段会被清空，调用前请先读取现有职位数据并完整回填。

---

### 更新职位设置

更新职位的面试官建议、Offer 申请表、面试/入职登记表、面试轮次类型与自助约面等设置；须按 update_option_list 声明的更新项填写对应必填字段。限频：10 次/秒。所需权限：hire:job（更新职位）。需先在飞书招聘「设置-基础设置」中开启 API 同步职位开关。

**函数签名**：

```csharp
Task<FeishuApiResult<UpdateJobConfigResult>?> UpdateJobConfigAsync(
    [Path] string job_id,
    [Body] UpdateJobConfigRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                    | 必填 | 说明                                                                          |
| -------------- | ----------------------- | ---- | ----------------------------------------------------------------------------- |
| `job_id`       | `string`                | ✅   | 职位 ID，示例值：`6960663240925956660`                                        |
| `request`      | `UpdateJobConfigRequest` | ✅   | 职位设置请求体（update_option_list 必填：要更新的配置项编号；各项按需填写）  |
| `user_id_type` | `string?`               | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id        |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_config": {}
  }
}
```

**说明**：需先开启「API 同步职位开关」；`data.job_config` 为更新后的 `JobConfigResult`，仅 `update_option_list` 声明的更新项生效。

---

### 批量更新职位管理人员

按 update_option_list 选择的成员项批量更新职位的招聘负责人、招聘助理与用人经理。限频：1000 次/分钟、50 次/秒。所需权限：hire:job（更新职位）。

**函数签名**：

```csharp
Task<FeishuApiResult<BatchUpdateJobManagerResult>?> BatchUpdateJobManagerAsync(
    [Path] string job_id,
    [Body] BatchUpdateJobManagerRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                          | 必填 | 说明                                                                                                  |
| -------------- | ----------------------------- | ---- | ----------------------------------------------------------------------------------------------------- |
| `job_id`       | `string`                      | ✅   | 职位 ID，示例值：`6960663240925956660`                                                                |
| `request`      | `BatchUpdateJobManagerRequest` | ✅   | 管理人员更新请求体（update_option_list 必填：1 招聘负责人 / 2 招聘助理 / 3 用人经理；对应成员列表随之生效） |
| `user_id_type` | `string?`                     | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_manager": {}
  }
}
```

**说明**：`data.job_manager` 为更新后的 `JobManager`（招聘负责人/招聘助理/用人经理）。

---

### 获取职位信息

按职位 ID 获取职位基础信息、部门、职级、序列、工作城市等完整信息。限频：50 次/秒。所需权限：hire:job:readonly（获取职位信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetJobResult>?> GetJobAsync(
    [Path] string job_id,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名               | 类型      | 必填 | 说明                                                                                                       |
| -------------------- | --------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `job_id`             | `string`  | ✅   | 职位 ID，示例值：`6960663240925956660`                                                                     |
| `user_id_type`       | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |
| `department_id_type` | `string?` | ⚪   | 部门 ID 类型（open_department_id/department_id），默认 open_department_id                                  |
| `job_level_id_type`  | `string?` | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id                     |
| `job_family_id_type` | `string?` | ⚪   | 职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id           |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job": {}
  }
}
```

**说明**：`data.job` 为 `Job`，含职位基础信息、部门、职级、序列、工作城市等。

---

### 获取职位详情

获取职位聚合详情，包括基本信息、职位管理人员、招聘需求、职位地址、职位设置、门店、标签与投递阶段统计数据。限频：20 次/秒。所需权限：hire:job.composite_info:readonly（获取职位聚合信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetJobDetailResult>?> GetJobDetailAsync(
    [Path] string job_id,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名               | 类型      | 必填 | 说明                                                                                                       |
| -------------------- | --------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `job_id`             | `string`  | ✅   | 职位 ID，示例值：`6960663240925956660`                                                                     |
| `user_id_type`       | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |
| `department_id_type` | `string?` | ⚪   | 部门 ID 类型（open_department_id/department_id），默认 open_department_id                                  |
| `job_level_id_type`  | `string?` | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id                     |
| `job_family_id_type` | `string?` | ⚪   | 职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id           |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_detail": {}
  }
}
```

**说明**：`data.job_detail` 为 `JobDetail` 聚合详情，含职位管理人员、关联招聘需求、地址列表、职位设置、门店、标签与阶段统计。

---

### 获取职位设置

获取职位的 Offer 申请表、审批流程、面试官建议、登记表、面试轮次类型与自助约面等设置。限频：20 次/秒。所需权限：hire:job:readonly（获取职位信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetJobConfigResult>?> GetJobConfigAsync(
    [Path] string job_id,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                                       |
| -------------- | --------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `job_id`       | `string`  | ✅   | 职位 ID，示例值：`6960663240925956660`                                                                     |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_config": {}
  }
}
```

**说明**：`data.job_config` 为 `JobConfigResult`，含 Offer 申请表/审批流、面试评价表、登记表、面试轮次类型与自助约面配置。

---

### 获取职位列表

按更新时间、招聘负责人、用人经理、部门等条件分页查询职位列表（查询参数采用查询对象模式 `JobListQuery`，见 AGENTS.md API-2）。限频：50 次/秒。所需权限：hire:job:readonly（获取职位信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetJobListResult>?> GetJobListAsync(
    [Query] JobListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型            | 必填 | 说明                                                             |
| ------- | --------------- | ---- | ---------------------------------------------------------------- |
| `query` | `JobListQuery?` | ⚪   | 更新时间范围、分页与各类 ID 类型查询参数，该对象会整体展开为查询参数 |

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

**说明**：`data.items` 为 `Job[]`；可按更新时间增量拉取，翻页时传入上一次返回的 `page_token`。

---

### 开放职位

开放指定职位用于投递，可指定到期日期或长期有效。限频：10 次/秒。所需权限：hire:job（更新职位）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> OpenJobAsync(
    [Path] string job_id,
    [Body] OpenJobRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型            | 必填 | 说明                                                                                                    |
| --------- | --------------- | ---- | ------------------------------------------------------------------------------------------------------- |
| `job_id`  | `string`        | ✅   | 职位 ID，示例值：`6960663240925956660`                                                                  |
| `request` | `OpenJobRequest` | ✅   | 开放请求体（is_never_expired 必填：true 长期有效；false 时 expiry_time 必填且须晚于当前时间）          |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：成功时 `data` 为空对象；`is_never_expired=false` 时必须填写 `expiry_time` 且须晚于当前时间。

---

### 获取职位发布人

获取指定职位的招聘负责人、用人经理与招聘助理列表。限频：50 次/秒。所需权限：hire:job:readonly（获取职位信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetJobRecruiterResult>?> GetJobRecruiterAsync(
    [Path] string job_id,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                    |
| -------------- | --------- | ---- | --------------------------------------------------------------------------------------- |
| `job_id`       | `string`  | ✅   | 职位 ID，示例值：`6960663240925956660`                                                  |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "info": {}
  }
}
```

**说明**：`data.info` 为 `JobManager`，含 `recruiter_id`、`hiring_manager_id_list` 与 `assistant_id_list`。

---

### 获取职位类别列表

分页获取招聘系统预置的职位类别列表，按创建时间升序返回，并包含节点的父子层级关系（parent_id），可用于构建职位类别树。限频：20 次/秒。所需权限：hire:job:readonly（获取职位信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetJobTypeListResult>?> GetJobTypeListAsync(
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
    "page_token": "",
    "has_more": false
  }
}
```

**说明**：`data.items` 为 `JobTypeInfo[]`，按创建时间升序返回，可据 `parent_id` 构建职位类别树。

---

### 获取职位职能分类列表

分页获取招聘系统内置的职位职能分类列表（含父级职能分类 ID，可据此构建职能分类树）。限频：20 次/秒。所需权限：hire:job:readonly（获取职位信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetJobFunctionListResult>?> GetJobFunctionListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                    |
| ------------ | --------- | ---- | ------------------------------------------------------- |
| `page_size`  | `int?`    | ⚪   | 每页数量，最大 50                                       |
| `page_token` | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token |

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

**说明**：`data.items` 为 `JobFunction[]`，含父级职能分类 ID，可据此构建职能分类树。

---

### 获取职位模板列表

按招聘场景（社招/校招）分页获取职位模板列表，返回模板内各模块的字段与选项配置。限频：10 次/秒。所需权限：hire:job:readonly（获取职位信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetJobSchemaListResult>?> GetJobSchemaListAsync(
    [Query("scenario")] int? scenario = null,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                    |
| ------------ | --------- | ---- | ------------------------------------------------------- |
| `scenario`   | `int?`    | ⚪   | 招聘场景：1 社招 / 2 校招                               |
| `page_size`  | `int?`    | ⚪   | 每页数量，最大 100                                      |
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

**说明**：`data.items` 为 `JobSchema[]`，含模板内各模块的字段与选项配置。

---

### 查询职位发布记录

按招聘渠道分页查询已发布到官网/拉勾等渠道的职位广告记录（查询参数采用查询对象模式 `JobPublishRecordSearchQuery`，见 AGENTS.md API-2）。限频：1000 次/分钟、50 次/秒。所需权限：hire:job:readonly（获取职位信息）或 hire:job（更新职位）。

**函数签名**：

```csharp
Task<FeishuApiResult<SearchJobPublishRecordResult>?> SearchJobPublishRecordAsync(
    [Body] SearchJobPublishRecordRequest request,
    [Query] JobPublishRecordSearchQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                            | 必填 | 说明                                                                                    |
| --------- | ------------------------------- | ---- | --------------------------------------------------------------------------------------- |
| `request` | `SearchJobPublishRecordRequest` | ✅   | 查询请求体（job_channel_id 渠道 ID，如官网渠道取 "2"、拉勾渠道取 "3"，可通过获取招聘渠道列表接口获取） |
| `query`   | `JobPublishRecordSearchQuery?`  | ⚪   | 分页与各类 ID 类型查询参数，该对象会整体展开为查询参数                                  |

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

**说明**：`data.items` 为 `WebsiteJobPost[]`；渠道 ID 可通过获取招聘渠道列表接口获取。

---

### 发布职位广告

将指定职位广告发布至所选招聘渠道（如官网招聘渠道）。限频：10 次/秒。所需权限：hire:advertisement（获取或更新招聘广告信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> PublishAdvertisementAsync(
    [Path] string advertisement_id,
    [Body] PublishAdvertisementRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名             | 类型                         | 必填 | 说明                                                                              |
| ------------------ | ---------------------------- | ---- | --------------------------------------------------------------------------------- |
| `advertisement_id` | `string`                     | ✅   | 职位广告 ID，来自职位创建响应中的 default_job_post.id                             |
| `request`          | `PublishAdvertisementRequest` | ✅   | 发布请求体（job_channel_id 渠道 ID，可通过获取招聘渠道列表接口获取，如官网渠道为 "3"） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：成功时 `data` 为空对象；职位广告 ID 取自组合创建/更新职位响应中的 `default_job_post.id`。
