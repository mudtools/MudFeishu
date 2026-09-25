# 职位管理（FeishuTenantV1HireJob）

## 接口名称

**职位管理** -（`IFeishuTenantV1HireJob`）

## 功能描述

以租户身份调用飞书招聘（Hire）职位相关的服务端 API：职位组合创建/组合更新（一步完成职位主体、职位管理人员、登记表与官网申请表配置）、职位设置更新与查询、职位管理人员批量更新、职位信息/聚合详情/列表查询、职位上架、职位发布人查询。全部端点仅支持 tenant_access_token。

## 参考文档

- [组合创建职位](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/combined_create)
- [组合更新职位](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/combined_update)
- [更新职位设置](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/update_config)
- [批量更新职位管理人员](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/batch_update)
- [获取职位详情](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/get_detail)
- [获取职位信息](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/get)
- [获取职位发布人](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/recruiter)
- [获取职位设置](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/config)
- [获取职位列表](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/list-2)
- [更新职位状态（上架）](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/open)

## 函数列表

| 函数名称                    | 功能描述                       | 限频       | 所需权限           | HTTP 方法 |
| --------------------------- | ------------------------------ | ---------- | ------------------ | --------- |
| CombinedCreateJobAsync      | 组合创建职位                   | 20 次/秒   | hire:job           | POST      |
| CombinedUpdateJobAsync      | 组合更新职位                   | 10 次/秒   | hire:job           | POST      |
| UpdateJobConfigAsync        | 更新职位设置                   | 10 次/秒   | hire:job           | POST      |
| BatchUpdateJobManagerAsync  | 批量更新职位管理人员           | 1000 次/分、50 次/秒 | hire:job | POST      |
| GetJobAsync                 | 获取职位信息                   | 50 次/秒   | hire:job:readonly  | GET       |
| GetJobDetailAsync           | 获取职位详情（聚合）           | 20 次/秒   | hire:job.composite_info:readonly | GET |
| GetJobConfigAsync           | 获取职位设置                   | 20 次/秒   | hire:job:readonly（字段权限 contact:user.employee_id:readonly） | GET |
| GetJobListAsync             | 获取职位列表                   | 50 次/秒   | hire:job:readonly（字段权限 contact:user.employee_id:readonly） | GET |
| OpenJobAsync                | 上架职位（更新职位状态）       | 10 次/秒   | hire:job           | POST      |
| GetJobRecruiterAsync        | 获取职位发布人                 | 50 次/秒   | hire:job:readonly  | GET       |

## 函数详细内容

### CombinedCreateJobAsync — 组合创建职位

`POST /open-apis/hire/v1/jobs/combined_create`

```csharp
Task<FeishuApiResult<CombinedJobResult>?> CombinedCreateJobAsync(
    [Body] CombinedJobRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    CancellationToken cancellationToken = default);
```

请求体 `CombinedJobRequest` 必填：`title`（职位名称）；其余按需填写（招聘流程 `job_process_id`/`process_type`、部门 `department_id`、职位管理人员 `job_managers`、自定义字段 `customized_data_list` 等）。响应 `CombinedJobResult` 含职位、职位管理人员、默认职位广告 `default_job_post`、面试/入职登记表信息与目标专业列表。

### CombinedUpdateJobAsync — 组合更新职位

`POST /open-apis/hire/v1/jobs/{job_id}/combined_update`

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

请求体与响应同组合创建，`job_id` 走路径参数。

### UpdateJobConfigAsync — 更新职位设置

`POST /open-apis/hire/v1/jobs/{job_id}/update_config`

```csharp
Task<FeishuApiResult<UpdateJobConfigResult>?> UpdateJobConfigAsync(
    [Path] string job_id,
    [Body] UpdateJobConfigRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

请求体按 `JobConfig` 形状定义（`update_option_list` 指定本次更新的配置项，对应配置项的必填字段必须填写）。**注意**：需开启「API 同步职位开关」方可调用。响应 `data.job_config` 为 `JobConfigResult`。

### BatchUpdateJobManagerAsync — 批量更新职位管理人员

`POST /open-apis/hire/v1/jobs/{job_id}/managers/batch_update`

```csharp
Task<FeishuApiResult<BatchUpdateJobManagerResult>?> BatchUpdateJobManagerAsync(
    [Path] string job_id,
    [Body] BatchUpdateJobManagerRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

请求体字段：`update_option_list`（必填，1 招聘负责人 / 2 招聘助理 / 3 用人经理）、`recruiter_id`、`assistant_id_list`、`hiring_manager_id_list`（按更新项必填）、`creator_id`。响应 `data.job_manager` 为更新后的 `JobManager`。

### GetJobAsync — 获取职位信息

`GET /open-apis/hire/v1/jobs/{job_id}`

```csharp
Task<FeishuApiResult<GetJobResult>?> GetJobAsync(
    [Path] string job_id,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    CancellationToken cancellationToken = default);
```

响应 `data.job` 为 `Job`。

### GetJobDetailAsync — 获取职位详情（聚合）

`GET /open-apis/hire/v1/jobs/{job_id}/get_detail`

```csharp
Task<FeishuApiResult<GetJobDetailResult>?> GetJobDetailAsync(
    [Path] string job_id,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    CancellationToken cancellationToken = default);
```

响应 `data.job_detail` 为 `JobDetail`：基本/聚合信息、职位负责人与助理/用人经理、关联招聘需求、地址列表、职位设置 `job_config`、门店列表、标签列表、招聘进展阶段统计 `stage_count_list`。

### GetJobConfigAsync — 获取职位设置

`GET /open-apis/hire/v1/jobs/{job_id}/config`

```csharp
Task<FeishuApiResult<GetJobConfigResult>?> GetJobConfigAsync(
    [Path] string job_id,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

响应 `data.job_config` 为 `JobConfigResult`（Offer 申请表/审批流、建议评估人、面试评价表、建议面试官轮次、登记表、面试轮次类型、关联职位、自助约面配置、官网申请表）。

### GetJobListAsync — 获取职位列表（查询对象模式）

`GET /open-apis/hire/v1/jobs`

```csharp
Task<FeishuApiResult<GetJobListResult>?> GetJobListAsync(
    [Query] JobListQuery? query = null,
    CancellationToken cancellationToken = default);
```

`JobListQuery`（实现 `IQueryParameter`，null/空值自动跳过）：

| 字段              | 查询参数名         | 类型     | 必填 | 说明                                    |
| ----------------- | ------------------ | -------- | ---- | --------------------------------------- |
| UpdateStartTime   | update_start_time  | string   | 否   | 按更新时间范围过滤（起）                |
| UpdateEndTime     | update_end_time    | string   | 否   | 按更新时间范围过滤（止）                |
| PageSize          | page_size          | int      | 否   | 分页大小，最大 20                       |
| PageToken         | page_token         | string   | 否   | 分页标记                                |
| UserIdType        | user_id_type       | string   | 否   | 用户 ID 类型                            |
| DepartmentIdType  | department_id_type | string   | 否   | 部门 ID 类型                            |
| JobLevelIdType    | job_level_id_type  | string   | 否   | 职级 ID 类型                            |
| JobFamilyIdType   | job_family_id_type | string   | 否   | 序列 ID 类型                            |

响应 `data.items` 为 `Job[]`，附 `has_more`/`page_token`。

### OpenJobAsync — 上架职位

`POST /open-apis/hire/v1/jobs/{job_id}/open`

```csharp
Task<FeishuNullDataApiResult?> OpenJobAsync(
    [Path] string job_id,
    [Body] OpenJobRequest request,
    CancellationToken cancellationToken = default);
```

请求体：`is_never_expired`（是否长期有效）、`expiry_time`（毫秒时间戳，`is_never_expired=false` 时必填且大于当前时间）。响应体为空，返回 `FeishuNullDataApiResult`。

### GetJobRecruiterAsync — 获取职位发布人

`GET /open-apis/hire/v1/jobs/{job_id}/recruiter`

```csharp
Task<FeishuApiResult<GetJobRecruiterResult>?> GetJobRecruiterAsync(
    [Path] string job_id,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

响应 `data.info` 为 `JobManager`（`recruiter_id`/`hiring_manager_id_list`/`assistant_id_list`）。
