---
title: 绩效后台配置接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份读取与维护飞书绩效后台配置，涵盖周期与项目、评估配置（模板/评估项/标签）及指标配置（指标模板/指标库/字段/标签）。
---

# 绩效后台配置 - 租户令牌（FeishuTenantV2PerformanceSemesterActivity）

## 接口名称

**绩效后台配置（租户令牌）** -（`IFeishuTenantV2PerformanceSemesterActivity`）

## 功能描述

提供以租户身份读取与维护飞书绩效后台配置的能力。飞书绩效（Performance）后台配置 SDK 是一组服务端 OpenAPI 的封装，覆盖「周期与项目」（查询周期与项目配置、批量查询/导入/删除被评估人补充信息、更新人员组成员、查询被评估人与绩效周期人员快照信息）与「评估配置」（绩效模板、评估项、标签填写题）及「指标配置」（指标模板、指标库指标、指标字段、指标标签）。本接口全部端点仅支持 tenant_access_token 调用；除获取周期列表（performance/v1）外，其余端点为 performance/v2。支持获取周期列表、获取项目列表、批量查询补充信息、批量导入补充信息、批量删除补充信息、更新人员组成员、获取被评估人信息、获取绩效周期人员信息、获取绩效模板、获取标签填写题配置、获取评估项列表、获取指标模板列表、获取指标字段列表、获取指标列表、获取指标标签列表等操作。

## 参考文档

- [获取周期列表 - 飞书开放平台](https://open.feishu.cn/document/performance-v1/review_config/semester_activity/semester/list)

## 函数列表

| 函数名称                             | 功能描述               | 认证方式 | HTTP 方法 | 接口文档 |
| ------------------------------------ | ---------------------- | -------- | --------- |----------|
| GetSemesterListAsync                 | 获取周期列表           | 租户令牌 | GET       | [GetSemesterListAsync](https://open.feishu.cn/document/performance-v1/review_config/semester_activity/semester/list) |
| QueryActivityListAsync               | 获取项目列表           | 租户令牌 | POST      | [QueryActivityListAsync](https://open.feishu.cn/document/performance-v1/review_config/semester_activity/activity/query) |
| QueryAdditionalInformationListAsync  | 批量查询补充信息       | 租户令牌 | POST      | [QueryAdditionalInformationListAsync](https://open.feishu.cn/document/performance-v1/review_config/semester_activity/additional_information/query) |
| ImportAdditionalInformationAsync     | 批量导入补充信息       | 租户令牌 | POST      | [ImportAdditionalInformationAsync](https://open.feishu.cn/document/performance-v1/review_config/semester_activity/additional_information/import) |
| BatchDeleteAdditionalInformationAsync | 批量删除补充信息      | 租户令牌 | DELETE    | [BatchDeleteAdditionalInformationAsync](https://open.feishu.cn/document/performance-v1/review_config/semester_activity/additional_information/delete) |
| WriteUserGroupUserRelAsync           | 更新人员组成员         | 租户令牌 | POST      | [WriteUserGroupUserRelAsync](https://open.feishu.cn/document/performance-v1/review_config/semester_activity/user_group_user_rel/write) |
| QueryRevieweeListAsync               | 获取被评估人信息       | 租户令牌 | POST      | [QueryRevieweeListAsync](https://open.feishu.cn/document/performance-v1/review_config/semester_activity/reviewee/query) |
| QueryUserInfoListAsync               | 获取绩效周期人员信息   | 租户令牌 | POST      | [QueryUserInfoListAsync](https://open.feishu.cn/document/performance-v1/review_config/semester_activity/reviewee/query-2) |
| QueryReviewTemplateListAsync         | 获取绩效模板           | 租户令牌 | POST      | [QueryReviewTemplateListAsync](https://open.feishu.cn/document/performance-v1/review_config/review_template/query) |
| QueryTagBasedQuestionListAsync       | 获取标签填写题配置     | 租户令牌 | POST      | [QueryTagBasedQuestionListAsync](https://open.feishu.cn/document/performance-v1/review_config/review_template/query-2) |
| QueryIndicatorListAsync              | 获取评估项列表         | 租户令牌 | POST      | [QueryIndicatorListAsync](https://open.feishu.cn/document/performance-v1/review_config/review_template/query-3) |
| QueryMetricTemplateListAsync         | 获取指标模板列表       | 租户令牌 | POST      | [QueryMetricTemplateListAsync](https://open.feishu.cn/document/performance-v1/review_config/metric_template/query) |
| QueryMetricFieldListAsync            | 获取指标字段列表       | 租户令牌 | POST      | [QueryMetricFieldListAsync](https://open.feishu.cn/document/performance-v1/review_config/metric_template/query-2) |
| QueryMetricListAsync                 | 获取指标列表           | 租户令牌 | POST      | [QueryMetricListAsync](https://open.feishu.cn/document/performance-v1/review_config/metric_template/query-3) |
| GetMetricTagListAsync                | 获取指标标签列表       | 租户令牌 | GET       | [GetMetricTagListAsync](https://open.feishu.cn/document/performance-v1/review_config/metric_template/list) |

## 函数详细内容

### 获取周期列表

批量获取周期的基本信息（名称、类型、时间范围等），支持按时间段、周期年份、周期类型筛选；各查询参数之间为「与」关系，全部不传时返回所有周期。查询参数采用查询对象模式 `GetSemesterListQuery`，见 AGENTS.md API-2。限频：10 次/分钟。所需权限（开启任一即可）：performance:performance（管理绩效数据）、performance:performance:readonly（查看绩效数据）、performance:semester:read（查看周期数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetSemesterListResult>?> GetSemesterListAsync(
    [Query] GetSemesterListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型                      | 必填 | 说明                                                                                    |
| ------- | ------------------------- | ---- | --------------------------------------------------------------------------------------- |
| `query` | `GetSemesterListQuery?`   | ⚪   | 周期起止时间、年份（0~9999）、类型分组、类型与用户 ID 类型等可选查询参数，该对象会整体展开为查询参数 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "id": "6992035450862224940",
        "year": 2024,
        "type_group": 1,
        "type": 1,
        "name": "2024 上半年绩效",
        "progress": 2,
        "start_time": "1704038400000",
        "end_time": "1719763200000",
        "create_time": "1704038400000",
        "modify_time": "1704038400000",
        "create_user_id": "ou_30b07b63089ea46518789914dac63d36",
        "modify_user_id": "ou_30b07b63089ea46518789914dac63d36"
      }
    ]
  }
}
```

**说明**：各查询条件之间为「与」关系，全部不传时返回所有周期。该端点为 performance/v1。

---

### 获取项目列表

批量获取项目的配置信息（项目名称、项目模式、项目状态等）。semester_ids 与 activity_ids 均未填写时返回空数据；填写 activity_ids 时 semester_ids 无效。限频：10 次/分钟。所需权限（开启任一即可）：performance:performance、performance:performance:readonly、performance:semester_activity:read（获取周期与项目配置信息）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryActivityListResult>?> QueryActivityListAsync(
    [Body] QueryActivityListRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                      | 必填 | 说明                                                                    |
| -------------- | ------------------------- | ---- | ----------------------------------------------------------------------- |
| `request`      | `QueryActivityListRequest` | ✅   | 请求体（semester_ids 评估周期 ID 列表 0~10 个；activity_ids 项目 ID 列表 0~50 个） |
| `user_id_type` | `string?`                 | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id                   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "activities": [
      {
        "id": "7343513161666707459",
        "name": "上半年绩效评估",
        "description": "2024 上半年绩效评估项目",
        "semester_id": "6992035450862224940",
        "mode": 1,
        "progress": 2,
        "create_time": "1704038400000",
        "modify_time": "1704038400000",
        "create_user_id": "ou_30b07b63089ea46518789914dac63d36",
        "modify_user_id": "ou_30b07b63089ea46518789914dac63d36"
      }
    ]
  }
}
```

**说明**：`semester_ids` 与 `activity_ids` 至少填写一项，否则返回空数据；同时填写时以 `activity_ids` 为准。

---

### 批量查询补充信息

批量查询被评估人的补充信息（事项、时间、具体描述）。item_ids、external_ids、reviewee_user_ids 均为空时返回 semester_id 指定周期的全部补充信息；多筛选参数按 item_ids > external_ids > reviewee_user_ids 的优先级取第一个有值者。限频：10 次/分钟。所需权限（开启任一即可）：performance:performance、performance:performance:readonly、performance:semester_activity:read、performance:semester_activity:write；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryAdditionalInformationListResult>?> QueryAdditionalInformationListAsync(
    [Body] QueryAdditionalInformationListRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("page_token")] string? page_token = null,
    [Query("page_size")] int? page_size = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                                     | 必填 | 说明                                                              |
| -------------- | ---------------------------------------- | ---- | ----------------------------------------------------------------- |
| `request`      | `QueryAdditionalInformationListRequest`  | ✅   | 请求体（semester_id 必填 1~100 字符；item_ids/external_ids/reviewee_user_ids 各 0~50 个） |
| `user_id_type` | `string?`                                | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id |
| `page_token`   | `string?`                                | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token            |
| `page_size`    | `int?`                                   | ⚪   | 分页大小，默认 20，取值范围 0~50                                  |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "additional_informations": [
      {
        "item_id": "7350194523185610771",
        "external_id": "external_001",
        "reviewee_user_id": {
          "union_id": "on_8ddcc7c7c0f7c09f8d7c1a3b2c4d5e6f",
          "user_id": "bega29ca"
        },
        "item": "季度突出贡献",
        "time": "1704038400000",
        "detailed_description": "主导完成核心系统重构"
      }
    ],
    "has_more": true,
    "page_token": "next_page_token"
  }
}
```

**说明**：三个筛选参数同时填写时，按 item_ids > external_ids > reviewee_user_ids 的优先级取第一个有值者；全部为空时返回该周期的全部补充信息。

---

### 批量导入补充信息

批量导入被评估人的补充信息作为绩效评估参考，同时支持创建与更新：先按已有 item_id 更新，再按已有 external_id 更新，否则按 reviewee_user_id + item + time + detailed_description 的组合匹配更新，均不匹配则新建。限频：10 次/分钟。所需权限（开启任一即可）：performance:performance、performance:semester_activity:write；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。client_token 用于幂等去重，重复提交将被拦截（错误码 1580110）。

**函数签名**：

```csharp
Task<FeishuApiResult<ImportAdditionalInformationResult>?> ImportAdditionalInformationAsync(
    [Body] ImportAdditionalInformationRequest request,
    [Query("client_token")] string client_token,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                                | 必填 | 说明                                                                              |
| -------------- | ----------------------------------- | ---- | --------------------------------------------------------------------------------- |
| `request`      | `ImportAdditionalInformationRequest` | ✅   | 请求体（semester_id 必填；additional_informations 1~1000 条；import_record_name 导入记录名称，默认「API导入」） |
| `client_token` | `string`                            | ✅   | 幂等请求标识，长度 0~64 字符（必填），示例值：`12454646`                           |
| `user_id_type` | `string?`                           | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id             |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "import_record_id": "7350194523185610771",
    "additional_informations": [
      {
        "item_id": "7350194523185610771",
        "external_id": "external_001",
        "item": "季度突出贡献",
        "time": "1704038400000",
        "detailed_description": "主导完成核心系统重构"
      }
    ]
  }
}
```

**说明**：单次最多导入 1000 条补充信息。`client_token` 为必填的幂等标识，重复提交会被拦截并返回错误码 1580110。

---

### 批量删除补充信息

按周期 ID 批量删除被评估人的补充信息。限频：10 次/分钟。所需权限（开启任一即可）：performance:performance、performance:semester_activity:write；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<DeleteAdditionalInformationResult>?> BatchDeleteAdditionalInformationAsync(
    [Body] DeleteAdditionalInformationRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                                | 必填 | 说明                                                                 |
| -------------- | ----------------------------------- | ---- | -------------------------------------------------------------------- |
| `request`      | `DeleteAdditionalInformationRequest` | ✅   | 请求体（semester_id 必填；additional_informations 补充信息 ID 列表 1~100 个） |
| `user_id_type` | `string?`                           | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id                |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "additional_informations": ["7350194523185610771"]
  }
}
```

**说明**：单次最多删除 100 条补充信息；删除后不可恢复，请谨慎操作。

---

### 更新人员组成员

更新指定人员组的成员。该接口为覆盖式更新，更新操作会清除人员组原有成员；人员组需在后台「人员范围设置」中勾选「API 自动写入人员名单」，否则返回错误码 1580401。限频：20 次/分钟。所需权限：performance:semester_activity:write（管理周期与项目配置信息）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。client_token 用于幂等去重，重复提交将被拦截（错误码 1580110）。

**函数签名**：

```csharp
Task<FeishuApiResult<WriteUserGroupUserRelResult>?> WriteUserGroupUserRelAsync(
    [Body] WriteUserGroupUserRelRequest request,
    [Query("client_token")] string client_token,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                          | 必填 | 说明                                                                                                                     |
| -------------- | ----------------------------- | ---- | ------------------------------------------------------------------------------------------------------------------------ |
| `request`      | `WriteUserGroupUserRelRequest` | ✅   | 请求体（group_id 人员组 ID 必填 0~128 字符；scope_visible_setting 可见性 0 无限制/1 后台管理员不可见，默认 1，取值范围 0~10；user_ids 人员 ID 列表 0~10000 个） |
| `client_token` | `string`                      | ✅   | 幂等请求标识，长度 0~64 字符（必填），示例值：`123456`                                                                     |
| `user_id_type` | `string?`                     | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id                                                     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "data": {
      "success_user_ids": ["bega29ca"],
      "fail_user_datas": []
    }
  }
}
```

**说明**：覆盖式更新会清除人员组原有成员，请提交完整的成员列表。该端点响应体为两层 `data` 结构（`data.data`）；人员组未勾选「API 自动写入人员名单」时返回错误码 1580401。

---

### 获取被评估人信息

获取绩效周期中被圈定到项目中的被评估人信息（含未启动的项目），可按用户或按项目过滤。限频：20 次/分钟。所需权限（开启任一即可）：performance:performance、performance:performance:readonly、performance:semester_user:read（获取周期人员信息）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryRevieweeListResult>?> QueryRevieweeListAsync(
    [Body] QueryRevieweeListRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("page_token")] string? page_token = null,
    [Query("page_size")] int? page_size = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                      | 必填 | 说明                                                                 |
| -------------- | ------------------------- | ---- | -------------------------------------------------------------------- |
| `request`      | `QueryRevieweeListRequest` | ✅   | 请求体（semester_id 必填；user_ids 0~50 个；activity_ids 项目 ID 列表） |
| `user_id_type` | `string?`                 | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id |
| `page_token`   | `string?`                 | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token               |
| `page_size`    | `int?`                    | ⚪   | 分页大小，默认 20，取值范围 1~50                                     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "semester_id": "6992035450862224940",
    "reviewees": [
      {
        "reviewee_user_id": {
          "union_id": "on_8ddcc7c7c0f7c09f8d7c1a3b2c4d5e6f",
          "user_id": "bega29ca"
        },
        "activity_ids": ["7343513161666707459"]
      }
    ],
    "has_more": true,
    "page_token": "next_page_token"
  }
}
```

**说明**：按 `user_ids` 或 `activity_ids` 过滤时二者可同时传入；不传过滤条件时分页返回该周期全部被评估人。

---

### 获取绩效周期人员信息

获取指定绩效周期下，被评估人在评估时的部门、序列、职级等人员快照信息。限频：100 次/分钟。所需权限（开启任一即可）：performance:performance、performance:performance:readonly；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）、performance:user_snapshot.department:read（部门）、performance:user_snapshot.direct_leader:read（直属上级）、performance:user_snapshot.job_family:read（序列）、performance:user_snapshot.job_level:read（职级）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryUserInfoListResult>?> QueryUserInfoListAsync(
    [Body] QueryUserInfoListRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名               | 类型                       | 必填 | 说明                                                                    |
| -------------------- | -------------------------- | ---- | ----------------------------------------------------------------------- |
| `request`            | `QueryUserInfoListRequest` | ✅   | 请求体（semester_id 必填；user_ids 人员 ID 列表 0~10 个）                |
| `user_id_type`       | `string?`                  | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id                   |
| `department_id_type` | `string?`                  | ⚪   | 部门 ID 类型：department_id/open_department_id，默认 open_department_id  |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "semester_id": "6992035450862224940",
    "user_infos": [
      {
        "user_id": {
          "union_id": "on_8ddcc7c7c0f7c09f8d7c1a3b2c4d5e6f",
          "user_id": "bega29ca"
        },
        "direct_leader_user_id": {
          "union_id": "on_1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d",
          "user_id": "leader001"
        },
        "department": {},
        "job_family": {},
        "job_level": {}
      }
    ]
  }
}
```

**说明**：部门、直属上级、序列与职级分别需要对应的 `performance:user_snapshot.*:read` 字段权限才会返回。单次最多传入 10 个人员 ID。

---

### 获取绩效模板

获取绩效模板信息，包括模版名称、执行角色、填写项类型等；可按模板 ID 列表筛选，不传时分页返回全部。限频：10 次/分钟。所需权限（开启任一即可）：performance:performance、performance:performance:readonly、performance:review_template:read（获取评估配置信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryReviewTemplateListResult>?> QueryReviewTemplateListAsync(
    [Body] QueryReviewTemplateListRequest request,
    [Query("page_token")] string? page_token = null,
    [Query("page_size")] int? page_size = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型                             | 必填 | 说明                                              |
| ------------ | -------------------------------- | ---- | ------------------------------------------------- |
| `request`    | `QueryReviewTemplateListRequest` | ✅   | 请求体（review_template_ids 绩效模板 ID 列表 0~50 个，不传返回所有） |
| `page_token` | `string?`                        | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token |
| `page_size`  | `int?`                           | ⚪   | 分页大小，默认 20，取值范围 0~50                   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "review_templates": [
      {
        "review_template_id": "7343513161666723843",
        "name": "通用绩效模板",
        "description": "适用于全员",
        "status": 1,
        "templates": [],
        "units": []
      }
    ],
    "has_more": true,
    "page_token": "next_page_token"
  }
}
```

**说明**：`templates` 为各环节模板，`units` 为评估内容；`review_template_ids` 不传时分页返回全部模板。

---

### 获取标签填写题配置

获取标签填写题配置信息，包括标签填写题名称、标签列表等；可按标签填写题 ID 列表筛选，不传时分页返回全部。限频：10 次/分钟。所需权限（开启任一即可）：performance:performance、performance:performance:readonly、performance:review_template:read（获取评估配置信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryTagBasedQuestionListResult>?> QueryTagBasedQuestionListAsync(
    [Body] QueryTagBasedQuestionListRequest request,
    [Query("page_token")] string? page_token = null,
    [Query("page_size")] int? page_size = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型                               | 必填 | 说明                                              |
| ------------ | ---------------------------------- | ---- | ------------------------------------------------- |
| `request`    | `QueryTagBasedQuestionListRequest` | ✅   | 请求体（tag_based_question_ids 标签填写题 ID 列表 0~50 个，不传返回所有） |
| `page_token` | `string?`                          | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token |
| `page_size`  | `int?`                             | ⚪   | 分页大小，默认 20，取值范围 0~50                   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "tag_based_questions": [
      {
        "question_id": "7343513161666707459",
        "name": "协作能力标签",
        "tag_items": []
      }
    ],
    "has_more": true,
    "page_token": "next_page_token"
  }
}
```

**说明**：标签填写题常用于绩效评估中的标签化打分项，`tag_items` 为该题目下的可选标签列表。

---

### 获取评估项列表

批量获取评估项信息，如评估项名称、评估项类型、评估项等级配置等；可按评估项 ID 列表筛选，不传时分页返回全部评估项。限频：10 次/分钟。所需权限（开启任一即可）：performance:performance、performance:performance:readonly、performance:review_template:read（获取评估配置信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryIndicatorListResult>?> QueryIndicatorListAsync(
    [Body] QueryIndicatorListRequest request,
    [Query("page_token")] string? page_token = null,
    [Query("page_size")] int? page_size = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型                       | 必填 | 说明                                              |
| ------------ | -------------------------- | ---- | ------------------------------------------------- |
| `request`    | `QueryIndicatorListRequest` | ✅   | 请求体（indicator_ids 评估项 ID 列表 0~50 个，不传返回所有） |
| `page_token` | `string?`                  | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token |
| `page_size`  | `int?`                     | ⚪   | 分页大小，默认 20，取值范围 0~50                   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "indicators": [
      {
        "id": "7343513161666707459",
        "name": "价值观",
        "type": 1,
        "options": [
          {
            "lable": "A"
          }
        ]
      }
    ],
    "has_more": true,
    "page_token": "next_page_token"
  }
}
```

**说明**：`options` 为评估项的等级配置，`lable` 为等级代号（飞书官方字段拼写）。

---

### 获取指标模板列表

批量获取指标模板的信息，可按模板 ID 列表与状态筛选，参数之间为「与」关系，均不传时分页返回所有指标模版信息。限频：20 次/分钟。所需权限（开启任一即可）：performance:metric:write（管理关键指标数据）、performance:metric_lib:read（获取指标配置信息）、performance:metric:read（获取关键指标数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryMetricTemplateListResult>?> QueryMetricTemplateListAsync(
    [Body] QueryMetricTemplateListRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("page_token")] string? page_token = null,
    [Query("page_size")] int? page_size = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                             | 必填 | 说明                                                                                  |
| -------------- | -------------------------------- | ---- | ------------------------------------------------------------------------------------- |
| `request`      | `QueryMetricTemplateListRequest` | ✅   | 请求体（metrics_template_ids 指标模板 ID 列表 0~50 个；status 模版状态 to_be_configured/to_be_activated/enabled/disabled） |
| `user_id_type` | `string?`                        | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id                 |
| `page_token`   | `string?`                        | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                |
| `page_size`    | `int?`                           | ⚪   | 分页大小，默认 20，取值范围 1~50                                                      |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "id": "7296488199415660563",
        "name": "销售指标模板",
        "description": "销售团队关键指标",
        "status": "enabled",
        "metric_dimensions": [],
        "metrics": [],
        "groups": []
      }
    ],
    "has_more": true,
    "page_token": "next_page_token"
  }
}
```

**说明**：`metrics_template_ids` 与 `status` 之间为「与」关系；均不传时分页返回所有指标模板。

---

### 获取指标字段列表

批量获取指标的字段基础信息，如指标字段名称、指标字段类型等；可按字段 ID 列表筛选，不填时获取全部指标字段（该接口无分页）。限频：20 次/分钟。所需权限（开启任一即可）：performance:metric:write（管理关键指标数据）、performance:metric_lib:read（获取指标配置信息）、performance:metric:read（获取关键指标数据）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryMetricFieldListResult>?> QueryMetricFieldListAsync(
    [Body] QueryMetricFieldListRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                          | 必填 | 说明                                                 |
| --------- | ----------------------------- | ---- | ---------------------------------------------------- |
| `request` | `QueryMetricFieldListRequest` | ✅   | 请求体（field_ids 指标字段 ID 列表 0~50 个，不传返回所有） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "field_id": "7272581996315099155",
        "name": "销售额",
        "type": "number"
      }
    ]
  }
}
```

**说明**：该接口无分页字段，一次返回全部指标字段；解析关键指标明细时以此为字段字典。

---

### 获取指标列表

获取指标库中的指标信息，如指标名称、指标类型、指标标签和指标字段等，可按指标启用状态、指标类型、指标可用范围等筛选条件获取指定范围的指标信息。限频：20 次/分钟。所需权限（开启任一即可）：performance:metric:write（管理关键指标数据）、performance:metric_lib:read（获取指标配置信息）、performance:metric:read（获取关键指标数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryMetricListResult>?> QueryMetricListAsync(
    [Body] QueryMetricListRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("page_token")] string? page_token = null,
    [Query("page_size")] int? page_size = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                   | 必填 | 说明                                                                                                                                                                        |
| -------------- | ---------------------- | ---- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `request`      | `QueryMetricListRequest` | ✅   | 请求体（is_active 启用状态；tag_ids 标签 ID 列表 0~99 个；type_ids 指标类型 ID 列表 0~99 个；range_of_availability 可用范围 admins_and_reviewees/only_admins；scoring_setting_type 评分类型 score_manually/score_by_formula） |
| `user_id_type` | `string?`              | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id                                                                                                     |
| `page_token`   | `string?`              | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                                                                                                    |
| `page_size`    | `int?`                 | ⚪   | 分页大小，默认 20，取值范围 1~50                                                                                                                                          |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "metric_id": "7272581996315099155",
        "name": "销售额",
        "type_id": "7272581996315099156",
        "tags": [],
        "fields": [],
        "range_of_availability": "admins_and_reviewees",
        "is_active": true
      }
    ],
    "has_more": true,
    "page_token": "next_page_token"
  }
}
```

**说明**：返回指标库指标及其标签、字段、评分设置与公式、可用范围与启用状态；`page_size` 取值范围 1~50。

---

### 获取指标标签列表

批量获取指标的标签信息，如标签名称、创建时间等；传 tag_ids 时不进行分页，不传时分页返回所有数据。限频：10 次/秒。所需权限（开启任一即可）：performance:metric:write（管理关键指标数据）、performance:metric_lib:read（获取指标配置信息）、performance:metric:read（获取关键指标数据）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetMetricTagListResult>?> GetMetricTagListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("tag_ids")] string[]? tag_ids = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型        | 必填 | 说明                                                   |
| ------------ | ----------- | ---- | ------------------------------------------------------ |
| `page_size`  | `int?`      | ⚪   | 分页大小，默认 20，最大 50                              |
| `page_token` | `string?`   | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token |
| `tag_ids`    | `string[]?` | ⚪   | 指标标签 ID 列表（0~9999 个），传此参数时不进行分页     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "tag_id": "7302271694582841364",
        "tag_name": "核心指标",
        "index": 1,
        "create_time": "1704038400000",
        "update_time": "1704038400000"
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**：传入 `tag_ids` 时不分页，一次返回指定标签；不传时按 `page_size`（默认 20、最大 50）分页返回全部标签。
