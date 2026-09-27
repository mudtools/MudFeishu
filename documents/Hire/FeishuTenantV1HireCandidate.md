# 候选人 - 租户令牌（FeishuTenantV1HireCandidate）

## 接口名称

**候选人（租户令牌）** -（`IFeishuTenantV1HireCandidate`）

## 功能描述

提供以租户身份管理飞书招聘候选人与投递流程的能力。飞书招聘（Hire）候选人入口域 SDK 是一组服务端 OpenAPI 的封装，覆盖飞书文档 candidate-management 分组的全部能力：内推信息与内推官网职位、招聘官网（列表/推广渠道/官网用户/官网职位）、官网投递创建与投递任务、官网申请表模板，人才管理（人才池、人才文件夹、标签、组合创建/更新、黑名单、入职状态、人才查询），投递流程管理（面试信息与评价记录 v1/v2、记录附件、速记明细、满意度问卷，Offer 创建/更新/查询/列表/状态变更，背调订单，三方协议，入职转正/取消与员工信息维护），以及人才备注增删改查、简历评估/笔试阅卷/面试任务列表与简历来源列表查询。本接口全部端点仅支持 tenant_access_token 调用（待办事项批量获取见用户态接口 `IFeishuUserV1HireCandidate`）。支持查询内推信息、获取内推官网职位列表、获取内推官网职位详情、按投递 ID 获取内推信息、获取官网申请表模板列表、创建官网推广渠道、更新官网推广渠道、删除官网推广渠道、获取官网推广渠道列表、创建官网用户、获取官网列表、获取官网职位详情、获取官网职位列表、搜索官网职位列表、按简历创建官网投递、按简历附件创建官网投递、获取官网投递任务结果、创建人才备注、更新人才备注、获取人才备注、获取人才备注列表、删除人才备注、获取简历评估任务列表、获取笔试阅卷任务列表、获取面试任务列表、获取简历来源列表、批量加入/移除人才库中人才、获取人才库列表、将人才加入人才库、操作人才标签、创建人才、更新人才信息、人才加入文件夹、人才移出文件夹、获取人才文件夹信息、根据手机号或邮箱获取人才 ID、获取人才列表、获取人才字段、获取人才信息、获取人才详细信息、更新人才在职状态、加入/移出人才黑名单、获取背调信息列表、查询背调信息列表、创建三方协议、获取三方协议、更新三方协议、删除三方协议、操作候选人入职、取消候选人入职、通过投递 ID 获取入职信息、通过员工 ID 获取入职信息、更新员工状态、更新 e-HR 导入任务结果、创建 Offer、更新 Offer 信息、获取 Offer 信息、获取 Offer 详情、获取 Offer 列表、更新 Offer 状态、更新实习 Offer 入/离职状态、获取面试信息、获取人才面试信息、获取面试评价详细信息、批量获取面试评价详细信息、获取面试记录附件、获取面试速记明细、获取面试满意度问卷列表等操作。

## 参考文档

- [招聘开发指南 - 飞书开放平台](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-development-guide)

## 函数列表

| 函数名称                                | 功能描述                        | 认证方式 | HTTP 方法 |
| --------------------------------------- | ------------------------------- | -------- | --------- |
| SearchReferralAsync                     | 查询内推信息                    | 租户令牌 | POST      |
| GetReferralWebsiteJobPostListAsync      | 获取内推官网职位列表            | 租户令牌 | GET       |
| GetReferralWebsiteJobPostAsync          | 获取内推官网职位详情            | 租户令牌 | GET       |
| GetReferralByApplicationAsync           | 按投递 ID 获取内推信息          | 租户令牌 | GET       |
| GetPortalApplySchemaListAsync           | 获取官网申请表模板列表          | 租户令牌 | GET       |
| CreateWebsiteChannelAsync               | 创建官网推广渠道                | 租户令牌 | POST      |
| UpdateWebsiteChannelAsync               | 更新官网推广渠道                | 租户令牌 | PUT       |
| DeleteWebsiteChannelAsync               | 删除官网推广渠道                | 租户令牌 | DELETE    |
| GetWebsiteChannelListAsync              | 获取官网推广渠道列表            | 租户令牌 | GET       |
| CreateWebsiteUserAsync                  | 创建官网用户                    | 租户令牌 | POST      |
| GetWebsiteListAsync                     | 获取官网列表                    | 租户令牌 | GET       |
| GetWebsiteJobPostAsync                  | 获取官网职位详情                | 租户令牌 | GET       |
| GetWebsiteJobPostListAsync              | 获取官网职位列表                | 租户令牌 | GET       |
| SearchWebsiteJobPostAsync               | 搜索官网职位列表                | 租户令牌 | POST      |
| CreateWebsiteDeliveryByResumeAsync      | 按简历创建官网投递              | 租户令牌 | POST      |
| CreateWebsiteDeliveryByAttachmentAsync  | 按简历附件创建官网投递          | 租户令牌 | POST      |
| GetWebsiteDeliveryTaskAsync             | 获取官网投递任务结果            | 租户令牌 | GET       |
| CreateNoteAsync                         | 创建人才备注                    | 租户令牌 | POST      |
| PatchNoteAsync                          | 更新人才备注                    | 租户令牌 | PATCH     |
| GetNoteAsync                            | 获取人才备注                    | 租户令牌 | GET       |
| GetNoteListAsync                        | 获取人才备注列表                | 租户令牌 | GET       |
| DeleteNoteAsync                         | 删除人才备注                    | 租户令牌 | DELETE    |
| GetEvaluationTaskListAsync              | 获取简历评估任务列表            | 租户令牌 | GET       |
| GetExamMarkingTaskListAsync             | 获取笔试阅卷任务列表            | 租户令牌 | GET       |
| GetInterviewTaskListAsync               | 获取面试任务列表                | 租户令牌 | GET       |
| GetResumeSourceListAsync                | 获取简历来源列表                | 租户令牌 | GET       |
| BatchChangeTalentPoolAsync              | 批量加入/移除人才库中人才       | 租户令牌 | POST      |
| GetTalentPoolListAsync                  | 获取人才库列表                  | 租户令牌 | GET       |
| AddTalentToTalentPoolAsync              | 将人才加入人才库                | 租户令牌 | POST      |
| OperateTalentTagAsync                   | 操作人才标签                    | 租户令牌 | POST      |
| CombinedCreateTalentAsync               | 创建人才                        | 租户令牌 | POST      |
| CombinedUpdateTalentAsync               | 更新人才信息                    | 租户令牌 | POST      |
| AddTalentToFolderAsync                  | 人才加入文件夹                  | 租户令牌 | POST      |
| RemoveTalentFromFolderAsync             | 人才移出文件夹                  | 租户令牌 | POST      |
| GetTalentFolderListAsync                | 获取人才文件夹信息              | 租户令牌 | GET       |
| BatchGetTalentIdAsync                   | 根据手机号或邮箱获取人才 ID     | 租户令牌 | POST      |
| GetTalentListAsync                      | 获取人才列表                    | 租户令牌 | GET       |
| QueryTalentObjectAsync                  | 获取人才字段                    | 租户令牌 | GET       |
| GetTalentAsync                          | 获取人才信息（v1）              | 租户令牌 | GET       |
| GetTalentV2Async                        | 获取人才详细信息（v2）          | 租户令牌 | GET       |
| UpdateTalentOnboardStatusAsync          | 更新人才在职状态                | 租户令牌 | POST      |
| ChangeTalentBlockAsync                  | 加入/移出人才黑名单             | 租户令牌 | POST      |
| GetBackgroundCheckOrderListAsync        | 获取背调信息列表                | 租户令牌 | GET       |
| BatchQueryBackgroundCheckOrderAsync     | 查询背调信息列表                | 租户令牌 | POST      |
| CreateTripartiteAgreementAsync          | 创建三方协议                    | 租户令牌 | POST      |
| GetTripartiteAgreementListAsync         | 获取三方协议                    | 租户令牌 | GET       |
| UpdateTripartiteAgreementAsync          | 更新三方协议                    | 租户令牌 | PUT       |
| DeleteTripartiteAgreementAsync          | 删除三方协议                    | 租户令牌 | DELETE    |
| TransferOnboardAsync                    | 操作候选人入职                  | 租户令牌 | POST      |
| CancelOnboardAsync                      | 取消候选人入职                  | 租户令牌 | POST      |
| GetEmployeeByApplicationAsync           | 通过投递 ID 获取入职信息        | 租户令牌 | GET       |
| GetEmployeeAsync                        | 通过员工 ID 获取入职信息        | 租户令牌 | GET       |
| PatchEmployeeAsync                      | 更新员工状态                    | 租户令牌 | PATCH     |
| PatchEhrImportTaskAsync                 | 更新 e-HR 导入任务结果          | 租户令牌 | PATCH     |
| CreateOfferAsync                        | 创建 Offer                      | 租户令牌 | POST      |
| UpdateOfferAsync                        | 更新 Offer 信息                 | 租户令牌 | PUT       |
| GetApplicationOfferAsync                | 获取 Offer 信息（按投递 ID）    | 租户令牌 | GET       |
| GetOfferAsync                           | 获取 Offer 详情                 | 租户令牌 | GET       |
| GetOfferListAsync                       | 获取 Offer 列表                 | 租户令牌 | GET       |
| ChangeOfferStatusAsync                  | 更新 Offer 状态                 | 租户令牌 | PATCH     |
| ChangeInternOfferStatusAsync            | 更新实习 Offer 入/离职状态      | 租户令牌 | POST      |
| GetInterviewListAsync                   | 获取面试信息                    | 租户令牌 | GET       |
| GetInterviewByTalentAsync               | 获取人才面试信息                | 租户令牌 | GET       |
| GetInterviewRecordAsync                 | 获取面试评价详细信息（v1）      | 租户令牌 | GET       |
| GetInterviewRecordV2Async               | 获取面试评价详细信息（新版 v2） | 租户令牌 | GET       |
| GetInterviewRecordListAsync             | 批量获取面试评价详细信息（v1）  | 租户令牌 | GET       |
| GetInterviewRecordListV2Async           | 批量获取面试评价详细信息（新版 v2） | 租户令牌 | GET   |
| GetInterviewRecordAttachmentAsync       | 获取面试记录附件                | 租户令牌 | GET       |
| GetInterviewMinutesAsync                | 获取面试速记明细                | 租户令牌 | GET       |
| GetInterviewQuestionnaireListAsync      | 获取面试满意度问卷列表          | 租户令牌 | GET       |

## 函数详细内容

### 查询内推信息

按人才 ID 与创建时间范围查询内推记录，按内推投递的创建时间倒序排列；不填时间范围时默认返回全部，最多 200 条。限频：10 次/秒。所需权限：hire:referral:readonly（获取内推信息）或 hire:referral（更新内推信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<SearchReferralResult>?> SearchReferralAsync(
    [Body] SearchReferralRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                   | 必填 | 说明                                                                                    |
| -------------- | ---------------------- | ---- | --------------------------------------------------------------------------------------- |
| `request`      | `SearchReferralRequest` | ✅   | 查询请求体（talent_id 必填：人才 ID；start_time/end_time 最早/最晚创建时间，毫秒时间戳） |
| `user_id_type` | `string?`              | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

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

**说明**：`data.items` 为 `ReferralInfo[]`；不传时间范围时默认返回全部，最多 200 条。

---

### 获取内推官网职位列表

分页获取内推官网的职位列表（不含自定义数据；含自定义数据的详情请用获取内推官网职位详情接口）（查询参数采用查询对象模式 `ReferralWebsiteJobPostListQuery`，见 AGENTS.md API-2）。限频：1000 次/分钟、50 次/秒。所需权限：hire:referral_website:readonly（获取内推官网信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetReferralWebsiteJobPostListResult>?> GetReferralWebsiteJobPostListAsync(
    [Query] ReferralWebsiteJobPostListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型                                | 必填 | 说明                                                               |
| ------- | ----------------------------------- | ---- | ------------------------------------------------------------------ |
| `query` | `ReferralWebsiteJobPostListQuery?`  | ⚪   | 流程类型、分页与各类 ID 类型查询参数，该对象会整体展开为查询参数   |

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

**说明**：`data.items` 为 `PortalJobPost[]`，不含自定义数据；需要自定义数据时改用「获取内推官网职位详情」。

---

### 获取内推官网职位详情

按职位广告 ID 获取内推官网职位详情（含自定义数据）。限频：1000 次/分钟、50 次/秒。所需权限：hire:referral_website:readonly（获取内推官网信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetReferralWebsiteJobPostResult>?> GetReferralWebsiteJobPostAsync(
    [Path] string job_post_id,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名               | 类型      | 必填 | 说明                                                                                    |
| -------------------- | --------- | ---- | --------------------------------------------------------------------------------------- |
| `job_post_id`        | `string`  | ✅   | 职位广告 ID，示例值：`6701528341100366094`                                              |
| `user_id_type`       | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |
| `department_id_type` | `string?` | ⚪   | 部门 ID 类型（open_department_id/department_id），默认 open_department_id               |
| `job_level_id_type`  | `string?` | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id  |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_post": {}
  }
}
```

**说明**：`data.job_post` 为 `PortalJobPost`，列表接口不含自定义数据时用本接口补齐。

---

### 按投递 ID 获取内推信息

按投递 ID 获取内推信息；仅适用于内推时选择了具体职位的场景，无具体职位的内推（未选职位投递）不存投递 ID，需改用内推 ID 或人才 ID 查询。限频：50 次/秒。所需权限：hire:referral:readonly（获取内推信息）或 hire:referral（更新内推信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetReferralByApplicationResult>?> GetReferralByApplicationAsync(
    [Query("application_id")] string application_id,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名           | 类型      | 必填 | 说明                                                                                                       |
| ---------------- | --------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `application_id` | `string`  | ✅   | 投递 ID，示例值：`6134134355464633`                                                                        |
| `user_id_type`   | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "referral": {}
  }
}
```

**说明**：`data.referral` 为 `Referral`；未选职位的内推无投递 ID，需改用「查询内推信息」。

---

### 获取官网申请表模板列表

分页获取「招聘官网申请表设置」页配置的申请表模板列表（官网/职位投递时按该模板填写申请表）。该能力处于灰度阶段。限频：20 次/秒。所需权限：hire:site:readonly（获取官网信息）或 hire:site（更新官网信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetPortalApplySchemaListResult>?> GetPortalApplySchemaListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                    |
| ------------ | --------- | ---- | ------------------------------------------------------- |
| `page_size`  | `int?`    | ⚪   | 每页数量，最大 50，默认 10                              |
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

**说明**：`data.items` 为 `RegistrationSchema[]`；该能力处于灰度阶段。

---

### 创建官网推广渠道

按招聘官网 ID 与渠道名称创建推广渠道，返回渠道链接与推广码。限频：1000 次/分钟、50 次/秒。所需权限：hire:site（更新官网信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<WebsiteChannelInfo>?> CreateWebsiteChannelAsync(
    [Path] string website_id,
    [Body] WebsiteChannelRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型                   | 必填 | 说明                                     |
| ------------ | ---------------------- | ---- | ---------------------------------------- |
| `website_id` | `string`               | ✅   | 官网 ID，示例值：`1618209327096`        |
| `request`    | `WebsiteChannelRequest` | ✅   | 创建请求体（channel_name 推广渠道名称，必填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "id": "",
    "name": "",
    "link": "",
    "code": ""
  }
}
```

**说明**：`data` 直接为 `WebsiteChannelInfo`，含渠道 ID、名称、链接与推广码。

---

### 更新官网推广渠道

按推广渠道 ID 修改招聘官网推广渠道名称。限频：1000 次/分钟、50 次/秒。所需权限：hire:site（更新官网信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<WebsiteChannelInfo>?> UpdateWebsiteChannelAsync(
    [Path] string website_id,
    [Path] string channel_id,
    [Body] WebsiteChannelRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型                   | 必填 | 说明                                     |
| ------------ | ---------------------- | ---- | ---------------------------------------- |
| `website_id` | `string`               | ✅   | 官网 ID，示例值：`1618209327096`        |
| `channel_id` | `string`               | ✅   | 推广渠道 ID，示例值：`7085989097067563300` |
| `request`    | `WebsiteChannelRequest` | ✅   | 更新请求体（channel_name 推广渠道名称，必填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "id": "",
    "name": "",
    "link": "",
    "code": ""
  }
}
```

**说明**：`data` 为更新后的 `WebsiteChannelInfo`；仅支持修改渠道名称。

---

### 删除官网推广渠道

按招聘官网 ID 与推广渠道 ID 删除推广渠道。限频：1000 次/分钟、50 次/秒。所需权限：hire:site（更新官网信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteWebsiteChannelAsync(
    [Path] string website_id,
    [Path] string channel_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型     | 必填 | 说明                                        |
| ------------ | -------- | ---- | ------------------------------------------- |
| `website_id` | `string` | ✅   | 官网 ID，示例值：`1618209327096`           |
| `channel_id` | `string` | ✅   | 推广渠道 ID，示例值：`7085989097067563300` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：成功时 `data` 为空对象；删除后该渠道的推广链接与推广码失效。

---

### 获取官网推广渠道列表

按招聘官网 ID 分页获取推广渠道列表。限频：1000 次/分钟、50 次/秒。所需权限：hire:site:readonly（获取官网信息）或 hire:site（更新官网信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetWebsiteChannelListResult>?> GetWebsiteChannelListAsync(
    [Path] string website_id,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                    |
| ------------ | --------- | ---- | ------------------------------------------------------- |
| `website_id` | `string`  | ✅   | 官网 ID，示例值：`1618209327096`                        |
| `page_size`  | `int?`    | ⚪   | 每页数量，最大 200，默认 10                              |
| `page_token` | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "has_more": false,
    "page_token": "",
    "website_channel_list": []
  }
}
```

**说明**：`data.website_channel_list` 为 `WebsiteChannelInfo[]`（注意字段名不是 `items`）。

---

### 创建官网用户

在招聘官网创建用户；external_id 为幂等字段，同一外部 ID 只会创建 1 个官网用户，已存在时返回已有用户信息。限频：10 次/秒。所需权限：hire:site（更新官网信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateWebsiteUserResult>?> CreateWebsiteUserAsync(
    [Path] string website_id,
    [Body] CreateWebsiteUserRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型                     | 必填 | 说明                                                                        |
| ------------ | ------------------------ | ---- | --------------------------------------------------------------------------- |
| `website_id` | `string`                 | ✅   | 官网 ID，可通过获取官网列表接口获取，示例值：`1618209327096`                |
| `request`    | `CreateWebsiteUserRequest` | ✅   | 创建请求体（external_id 必填；name/email/mobile/mobile_country_code 选填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "site_user": {}
  }
}
```

**说明**：`external_id` 为幂等字段，已存在时返回已有官网用户；官网投递前需先创建官网用户。

---

### 获取官网列表

分页获取招聘官网列表，返回官网名称、流程类型与招聘渠道 ID。限频：10 次/秒。所需权限：hire:site:readonly（获取官网信息）或 hire:site（更新官网信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetWebsiteListResult>?> GetWebsiteListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                    |
| ------------ | --------- | ---- | ------------------------------------------------------- |
| `page_size`  | `int?`    | ⚪   | 每页数量，最大 10                                       |
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

**说明**：`data.items` 为 `Website[]`，含官网名称、流程类型与招聘渠道 ID。

---

### 获取官网职位详情

按官网 ID 与职位广告 ID 获取官网职位详情（含自定义数据与目标专业）。官网职位以 job_post_id（职位广告 ID）标识，而非 job_id。限频：1000 次/分钟、50 次/秒。所需权限：hire:site_job_post:readonly（获取官网职位信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetWebsiteJobPostResult>?> GetWebsiteJobPostAsync(
    [Path] string website_id,
    [Path] string job_post_id,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名               | 类型      | 必填 | 说明                                                                                    |
| -------------------- | --------- | ---- | --------------------------------------------------------------------------------------- |
| `website_id`         | `string`  | ✅   | 官网 ID，示例值：`111`                                                                  |
| `job_post_id`        | `string`  | ✅   | 职位广告 ID，示例值：`111`                                                              |
| `user_id_type`       | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |
| `department_id_type` | `string?` | ⚪   | 部门 ID 类型（open_department_id/department_id），默认 open_department_id               |
| `job_level_id_type`  | `string?` | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id  |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "job_post": {}
  }
}
```

**说明**：`data.job_post` 为 `WebsiteJobPost`；官网职位用职位广告 ID 标识，不是 `job_id`。

---

### 获取官网职位列表

按官网 ID 分页获取职位列表（暂不支持获取自定义数据，请用详情接口）（查询参数采用查询对象模式 `WebsiteJobPostListQuery`，见 AGENTS.md API-2）。限频：1000 次/分钟、50 次/秒。所需权限：hire:site_job_post:readonly（获取官网职位信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetWebsiteJobPostListResult>?> GetWebsiteJobPostListAsync(
    [Path] string website_id,
    [Query] WebsiteJobPostListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型                      | 必填 | 说明                                                            |
| ------------ | ------------------------- | ---- | --------------------------------------------------------------- |
| `website_id` | `string`                  | ✅   | 官网 ID，示例值：`111`                                          |
| `query`      | `WebsiteJobPostListQuery?` | ⚪   | 分页、创建/更新时间范围与各类 ID 类型查询参数，该对象会整体展开为查询参数 |

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

**说明**：`data.items` 为 `WebsiteJobPost[]`；列表不含自定义数据，需自定义数据时用详情接口。

---

### 搜索官网职位列表

按职位类别、城市、职能、科目、关键词与创建/更新时间范围搜索官网职位。限频：50 次/秒。所需权限：hire:site_job_post:readonly（获取官网职位信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<SearchWebsiteJobPostResult>?> SearchWebsiteJobPostAsync(
    [Path] string website_id,
    [Body] SearchWebsiteJobPostRequest request,
    [Query("page_token")] string? page_token = null,
    [Query("page_size")] int? page_size = null,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名               | 类型                        | 必填 | 说明                                                                                    |
| -------------------- | --------------------------- | ---- | --------------------------------------------------------------------------------------- |
| `website_id`         | `string`                    | ✅   | 官网 ID，示例值：`111`                                                                  |
| `request`            | `SearchWebsiteJobPostRequest` | ✅   | 搜索请求体（各类 ID 列表最大 100 个；keyword、创建/更新时间范围选填）                   |
| `page_token`         | `string?`                   | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                 |
| `page_size`          | `int?`                      | ⚪   | 每页数量，最大 10，默认 10                                                              |
| `user_id_type`       | `string?`                   | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |
| `department_id_type` | `string?`                   | ⚪   | 部门 ID 类型（open_department_id/department_id），默认 open_department_id               |
| `job_level_id_type`  | `string?`                   | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id  |

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

**说明**：`data.items` 为 `WebsiteJobPost[]`；请求体中各类 ID 列表最大 100 个。

---

### 按简历创建官网投递

按结构化简历信息在招聘官网创建投递，需先创建官网用户；自定义字段取值格式见接口文档说明（单选传选项 ID、多选传选项 ID 数组字符串、时间段传毫秒时间戳数组等）。限频：1000 次/分钟、50 次/秒。所需权限：hire:site_application（更新官网投递信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateWebsiteDeliveryByResumeResult>?> CreateWebsiteDeliveryByResumeAsync(
    [Path] string website_id,
    [Body] CreateWebsiteDeliveryByResumeRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                                 | 必填 | 说明                                                                                            |
| -------------- | ------------------------------------ | ---- | ----------------------------------------------------------------------------------------------- |
| `website_id`   | `string`                             | ✅   | 官网 ID，示例值：`1618209327096`                                                                |
| `request`      | `CreateWebsiteDeliveryByResumeRequest` | ✅   | 投递请求体（job_post_id、resume、user_id 必填；resume 含基本信息、教育/工作/实习经历、自定义模块等） |
| `user_id_type` | `string?`                            | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "delivery": {}
  }
}
```

**说明**：`data.delivery` 为 `WebsiteDeliveryDto`，含投递 ID、职位、简历、官网用户与人才 ID；投递前需先创建官网用户。

---

### 按简历附件创建官网投递

上传简历附件解析后在招聘官网创建投递（异步任务），返回 task_id 后需轮询获取投递任务结果接口获取解析与投递结果。限频：1000 次/分钟、50 次/秒。所需权限：hire:site_application（更新官网投递信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateWebsiteDeliveryByAttachmentResult>?> CreateWebsiteDeliveryByAttachmentAsync(
    [Path] string website_id,
    [Body] CreateWebsiteDeliveryByAttachmentRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型                                     | 必填 | 说明                                                                                                                        |
| ------------ | ---------------------------------------- | ---- | --------------------------------------------------------------------------------------------------------------------------- |
| `website_id` | `string`                                 | ✅   | 官网 ID，示例值：`7047318856652261676`                                                                                      |
| `request`    | `CreateWebsiteDeliveryByAttachmentRequest` | ✅   | 投递请求体（job_post_id、user_id、resume_file_id 必填；channel_id、手机号/邮箱/证件信息选填，与附件不一致时以参数为准）     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "task_id": ""
  }
}
```

**说明**：为异步任务，返回 `task_id` 后需轮询「获取官网投递任务结果」接口获取解析与投递结果。

---

### 获取官网投递任务结果

查询简历附件投递任务状态；返回数据为空时请继续轮询，直到有数据后再解析 delivery。限频：1000 次/分钟、50 次/秒。所需权限：hire:site_application:readonly（获取官网投递信息）或 hire:site_application（更新官网投递信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetWebsiteDeliveryTaskResult>?> GetWebsiteDeliveryTaskAsync(
    [Path] string website_id,
    [Path] string delivery_task_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名             | 类型     | 必填 | 说明                                                                              |
| ------------------ | -------- | ---- | --------------------------------------------------------------------------------- |
| `website_id`       | `string` | ✅   | 官网 ID，示例值：`7047318856652261676`                                            |
| `delivery_task_id` | `string` | ✅   | 投递任务 ID，来自按简历附件创建官网投递的返回，示例值：`f1c2a0f138ec492d99d7ab73594158ad` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "status": 0,
    "delivery": {},
    "status_msg": "",
    "extra_info": ""
  }
}
```

**说明**：`status` 取 0 新建/1 处理中/2 成功/3 失败；返回 `delivery` 为空时需继续轮询。

---

### 创建人才备注

为人才创建备注信息，支持在备注中 @ 其他用户。限频：20 次/秒。所需权限：hire:note（更新招聘备注）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateNoteResult>?> CreateNoteAsync(
    [Body] CreateNoteRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型              | 必填 | 说明                                                                                                                             |
| -------------- | ----------------- | ---- | -------------------------------------------------------------------------------------------------------------------------------- |
| `request`      | `CreateNoteRequest` | ✅   | 创建请求体（talent_id、content 必填；application_id、creator_id、privacy 私密属性（1-私密/2-公开）、notify_mentioned_user、mention_entity_list 选填） |
| `user_id_type` | `string?`         | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限  |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "note": {}
  }
}
```

**说明**：`data.note` 为 `Note`，含备注 ID、人才/投递 ID、是否私密、创建/更新时间、创建人 ID 与内容。

---

### 更新人才备注

按备注 ID 更新备注内容，支持更新 @ 用户并选择是否通知被 @ 的用户。限频：20 次/秒。所需权限：hire:note（更新招聘备注）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<PatchNoteResult>?> PatchNoteAsync(
    [Path] string note_id,
    [Body] PatchNoteRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型             | 必填 | 说明                                                                                                    |
| -------------- | ---------------- | ---- | ------------------------------------------------------------------------------------------------------- |
| `note_id`      | `string`         | ✅   | 备注 ID，可通过获取人才备注列表接口获取，示例值：`6960663240925956401`                                  |
| `request`      | `PatchNoteRequest` | ✅   | 更新请求体（content 必填；operator_id、notify_mentioned_user、mention_entity_list 选填）               |
| `user_id_type` | `string?`        | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "note": {}
  }
}
```

**说明**：`data.note` 为更新后的 `Note`；可通过 `notify_mentioned_user` 控制是否通知被 @ 的用户。

---

### 获取人才备注

按备注 ID 获取备注详情。限频：20 次/秒。所需权限：hire:note:readonly（获取招聘备注）或 hire:note（更新招聘备注）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetNoteResult>?> GetNoteAsync(
    [Path] string note_id,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                                    |
| -------------- | --------- | ---- | ------------------------------------------------------------------------------------------------------- |
| `note_id`      | `string`  | ✅   | 备注 ID，可通过获取人才备注列表接口获取，示例值：`6949805467799537964`                                  |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "note": {}
  }
}
```

**说明**：`data.note` 为 `Note` 备注详情。

---

### 获取人才备注列表

按人才 ID 分页获取备注列表。限频：20 次/秒。所需权限：hire:note:readonly（获取招聘备注）或 hire:note（更新招聘备注）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetNoteListResult>?> GetNoteListAsync(
    [Query("talent_id")] string talent_id,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                                    |
| -------------- | --------- | ---- | ------------------------------------------------------------------------------------------------------- |
| `talent_id`    | `string`  | ✅   | 人才 ID，示例值：`6916472453069883661`                                                                  |
| `page_size`    | `int?`    | ⚪   | 每页数量，默认 10，最大 200                                                                             |
| `page_token`   | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                                 |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

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

**说明**：`data.items` 为 `Note[]`；返回的备注 ID 可用于更新/删除备注。

---

### 删除人才备注

按备注 ID 删除指定备注。限频：20 次/秒。所需权限：hire:note（更新招聘备注）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteNoteAsync(
    [Path] string note_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型     | 必填 | 说明                                                          |
| --------- | -------- | ---- | ------------------------------------------------------------- |
| `note_id` | `string` | ✅   | 备注 ID，可通过获取人才备注列表接口获取，示例值：`6996605821056812588` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：删除成功时 `data` 为空对象。

---

### 获取简历评估任务列表

获取指定评估人的简历评估任务列表。限频：1000 次/分钟、50 次/秒。所需权限：hire:evaluation:readonly（获取简历评估信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetEvaluationTaskListResult>?> GetEvaluationTaskListAsync(
    [Query("user_id")] string user_id,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("activity_status")] int? activity_status = null,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名            | 类型      | 必填 | 说明                                                                                                       |
| ----------------- | --------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `user_id`         | `string`  | ✅   | 评估人 ID，需与 user_id_type 类型保持一致，示例值：`ou_e6139117c300506837def50545420c6a`                    |
| `page_size`       | `int?`    | ⚪   | 每页数量，最大 20，默认 10                                                                                 |
| `page_token`      | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                                    |
| `activity_status` | `int?`    | ⚪   | 任务状态：1-待评估，2-已评估，3-无需评估；不传查询全部                                                     |
| `user_id_type`    | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

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

**说明**：`data.items` 为 `EvaluationTask[]`；`user_id` 必须与 `user_id_type` 的类型一致。

---

### 获取笔试阅卷任务列表

获取指定员工的笔试阅卷任务列表。限频：1000 次/分钟、50 次/秒。所需权限：hire:exam:readonly（获取笔试信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetExamMarkingTaskListResult>?> GetExamMarkingTaskListAsync(
    [Query("user_id")] string user_id,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("activity_status")] int? activity_status = null,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名            | 类型      | 必填 | 说明                                                                                                       |
| ----------------- | --------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `user_id`         | `string`  | ✅   | 阅卷人 ID，需与 user_id_type 类型保持一致，示例值：`ou_e6139117c300506837def50545420c6a`                    |
| `page_size`       | `int?`    | ⚪   | 每页数量，最大 20，默认 10                                                                                 |
| `page_token`      | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                                    |
| `activity_status` | `int?`    | ⚪   | 任务状态：1-待阅卷，2-已阅卷；不传查询全部                                                                 |
| `user_id_type`    | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

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

**说明**：`data.items` 为 `ExamMarkingTask[]`；`user_id` 必须与 `user_id_type` 的类型一致。

---

### 获取面试任务列表

获取指定员工的面试任务列表。限频：1000 次/分钟、50 次/秒。所需权限：hire:interview:readonly（获取面试信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetInterviewTaskListResult>?> GetInterviewTaskListAsync(
    [Query("user_id")] string user_id,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("activity_status")] int? activity_status = null,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名            | 类型      | 必填 | 说明                                                                                                       |
| ----------------- | --------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `user_id`         | `string`  | ✅   | 面试官 ID，需与 user_id_type 类型保持一致，示例值：`ou_e6139117c300506837def50545420c6a`                    |
| `page_size`       | `int?`    | ⚪   | 每页数量，最大 20，默认 10                                                                                 |
| `page_token`      | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                                    |
| `activity_status` | `int?`    | ⚪   | 任务状态：1-未开始，2-未评估，3-已评估，5-已终止；不传查询全部                                             |
| `user_id_type`    | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

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

**说明**：`data.items` 为 `InterviewTask[]`；`user_id` 必须与 `user_id_type` 的类型一致。

---

### 获取简历来源列表

分页获取简历来源列表（内推、猎头、第三方招聘网站等来源配置）。限频：20 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetResumeSourceListResult>?> GetResumeSourceListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                    |
| ------------ | --------- | ---- | ------------------------------------------------------- |
| `page_size`  | `int?`    | ⚪   | 每页数量，最大 100                                      |
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

**说明**：`data.items` 为 `ResumeSource[]`，含内推、猎头、第三方招聘网站等来源配置。

---

### 批量加入/移除人才库中人才

对同一个人才库批量执行人才加入或移除操作；不存在的 ID 报错返回，已在/不在库中则静默处理。限频：10 次/秒。所需权限：hire:talent_folder（更新人才库信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> BatchChangeTalentPoolAsync(
    [Path] string talent_pool_id,
    [Body] BatchChangeTalentPoolRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名           | 类型                          | 必填 | 说明                                                                                  |
| ---------------- | ----------------------------- | ---- | ------------------------------------------------------------------------------------- |
| `talent_pool_id` | `string`                      | ✅   | 人才库 ID，可通过获取人才库列表接口获取，示例值：`6930815272790114324`                |
| `request`        | `BatchChangeTalentPoolRequest` | ✅   | 请求体（talent_id_list 必填：人才 ID 列表，1～50 个；option_type 必填：1 加入 / 2 移除） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：`talent_id_list` 最多 50 个；不存在的 ID 会报错，已在库/不在库的情况静默处理。

---

### 获取人才库列表

分页获取人才库列表，返回人才库 ID、中英文名称与描述、父子关系、可见性与创建/修改时间。限频：1000 次/分钟、50 次/秒。所需权限：hire:talent_folder:readonly（获取人才库信息）或 hire:talent_folder（更新人才库信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetTalentPoolListResult>?> GetTalentPoolListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("id_list")] string[]? id_list = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型        | 必填 | 说明                                                                 |
| ------------ | ----------- | ---- | -------------------------------------------------------------------- |
| `page_size`  | `int?`      | ⚪   | 每页数量，默认 10，最大 100                                          |
| `page_token` | `string?`   | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token               |
| `id_list`    | `string[]?` | ⚪   | 人才库 ID 列表，传入时按列表返回，最大 50 个；不传返回全部           |

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

**说明**：`data.items` 为 `TalentPool[]`，含中英文名称与描述、父子关系、可见性与创建/修改时间。

---

### 将人才加入人才库

将单个人才加入指定人才库，并可选择加入后是否从其他人才库移出。限频：1000 次/分钟、50 次/秒。所需权限：hire:talent_folder（更新人才库信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<AddTalentToTalentPoolResult>?> AddTalentToTalentPoolAsync(
    [Path] string talent_pool_id,
    [Body] AddTalentToTalentPoolRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名           | 类型                           | 必填 | 说明                                                                                      |
| ---------------- | ------------------------------ | ---- | ----------------------------------------------------------------------------------------- |
| `talent_pool_id` | `string`                       | ✅   | 人才库 ID，可通过获取人才库列表接口获取，示例值：`6930815272790114324`                    |
| `request`        | `AddTalentToTalentPoolRequest` | ✅   | 请求体（talent_id 必填：人才 ID；add_type 必填：1 不从其他库移出 / 2 从其他库移出）       |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "talent_pool_id": "",
    "talent_id": ""
  }
}
```

**说明**：`add_type` 为 2 时，人才加入目标库后会从其他人才库移出。

---

### 操作人才标签

为人才批量新增或删除标签。限频：20 次/秒。所需权限：hire:talent（更新人才信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> OperateTalentTagAsync(
    [Path] string talent_id,
    [Body] OperateTalentTagRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名      | 类型                     | 必填 | 说明                                                                     |
| ----------- | ------------------------ | ---- | ------------------------------------------------------------------------ |
| `talent_id` | `string`                 | ✅   | 人才 ID，可通过批量获取人才 ID 接口获取，示例值：`6930815272790114324`   |
| `request`   | `OperateTalentTagRequest` | ✅   | 请求体（operation 必填：1 新增 / 2 删除；tag_id_list 必填：标签 ID 列表） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：成功时 `data` 为空对象；标签 ID 可通过「获取人才标签列表」接口获取。

---

### 创建人才

在企业内创建一个人才（简历级聚合写入）；预置姓名必填，邮箱/手机是否必填取决于飞书招聘标准简历模板设置。限频：1000 次/分钟、50 次/秒。所需权限：hire:talent（更新人才信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<CombinedCreateTalentResult>?> CombinedCreateTalentAsync(
    [Body] CombinedCreateTalentRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                         | 必填 | 说明                                                                                                    |
| -------------- | ---------------------------- | ---- | ------------------------------------------------------------------------------------------------------- |
| `request`      | `CombinedCreateTalentRequest` | ✅   | 创建请求体（basic_info 必填；education_list 等各子经历数组最大 100 条）                                |
| `user_id_type` | `string?`                    | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "talent_id": "",
    "creator_id": "",
    "creator_account_type": 0
  }
}
```

**说明**：邮箱/手机是否必填取决于标准简历模板设置；各子经历数组最大 100 条。

---

### 更新人才信息

按人才 ID 全量聚合更新企业内人才信息；各子经历数组为全量覆盖，邮箱/手机必填性取决于标准简历模板设置。限频：1000 次/分钟、50 次/秒。所需权限：hire:talent（更新人才信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<CombinedUpdateTalentResult>?> CombinedUpdateTalentAsync(
    [Body] CombinedUpdateTalentRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                         | 必填 | 说明                                                                                                    |
| -------------- | ---------------------------- | ---- | ------------------------------------------------------------------------------------------------------- |
| `request`      | `CombinedUpdateTalentRequest` | ✅   | 更新请求体（talent_id 必填；basic_info 必填；operator_id/operator_account_type 为更新者信息）           |
| `user_id_type` | `string?`                    | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "talent_id": "",
    "operator_id": "",
    "operator_account_type": 0
  }
}
```

**说明**：为全量聚合更新，各子经历数组全量覆盖，未提交的经历会被清除。

---

### 人才加入文件夹

把一批人才加入到指定文件夹。限频：1000 次/分钟、50 次/秒。所需权限：hire:talent（更新人才信息）或 hire:talent_folder_association（更新人才文件夹关联信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<TalentToFolderResult>?> AddTalentToFolderAsync(
    [Body] TalentToFolderRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                  | 必填 | 说明                                                                       |
| --------- | --------------------- | ---- | -------------------------------------------------------------------------- |
| `request` | `TalentToFolderRequest` | ✅   | 请求体（talent_id_list 必填：人才 ID 列表，1～200 个；folder_id 必填：文件夹 ID） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "talent_id_list": [],
    "folder_id": ""
  }
}
```

**说明**：单次最多 200 个 `talent_id`；返回回显的人才 ID 列表与文件夹 ID。

---

### 人才移出文件夹

按人才 ID 列表把人才从指定文件夹移出。限频：10 次/秒。所需权限：hire:talent_folder_association（更新人才文件夹关联信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<TalentToFolderResult>?> RemoveTalentFromFolderAsync(
    [Body] TalentToFolderRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                  | 必填 | 说明                                                                       |
| --------- | --------------------- | ---- | -------------------------------------------------------------------------- |
| `request` | `TalentToFolderRequest` | ✅   | 请求体（talent_id_list 必填：人才 ID 列表，1～200 个；folder_id 必填：文件夹 ID） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "talent_id_list": [],
    "folder_id": ""
  }
}
```

**说明**：单次最多 200 个 `talent_id`；只解除文件夹关联，不删除人才。

---

### 获取人才文件夹信息

分页获取招聘系统中的人才文件夹列表（文件夹维度的人才库）。限频：1000 次/分钟、50 次/秒。所需权限：hire:talent_folder:readonly（获取人才库信息）或 hire:talent_folder（更新人才库信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetTalentFolderListResult>?> GetTalentFolderListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                                       |
| -------------- | --------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `page_size`    | `int?`    | ⚪   | 每页数量，默认 10，最大 100                                                                                |
| `page_token`   | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                                    |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

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

**说明**：`data.items` 为 `TalentFolder[]`；返回的 `folder_id` 用于人才加入/移出文件夹。

---

### 根据手机号或邮箱获取人才 ID

通过手机号、邮箱或证件号批量反查人才 ID 及基础信息；三类条件为且（AND）关系，至少传入一种，最多各 100 条。限频：1000 次/分钟、50 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。字段权限：hire:talent_onboard_status:readonly（返回的入职状态字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<BatchGetTalentIdResult>?> BatchGetTalentIdAsync(
    [Body] BatchGetTalentIdRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                    | 必填 | 说明                                                                                    |
| --------- | ----------------------- | ---- | --------------------------------------------------------------------------------------- |
| `request` | `BatchGetTalentIdRequest` | ✅   | 请求体（mobile_code、mobile_number_list、email_list、identification_type、identification_number_list 选填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "talent_list": []
  }
}
```

**说明**：`data.talent_list` 为 `TalentBatchInfo[]`；三类查询条件为「且」关系，至少传入一种，每类最多 100 条。

---

### 获取人才列表

按关键词、更新时间等条件分页获取候选人（人才）全量档案列表（查询参数采用查询对象模式 `TalentListQuery`，见 AGENTS.md API-2）。限频：20 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetTalentListResult>?> GetTalentListAsync(
    [Query] TalentListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型              | 必填 | 说明                                                               |
| ------- | ----------------- | ---- | ------------------------------------------------------------------ |
| `query` | `TalentListQuery?` | ⚪   | 关键词、更新时间范围、分页、排序与 ID 类型查询参数，该对象会整体展开为查询参数 |

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

**说明**：`data.items` 为 `Talent[]`；可按更新时间增量拉取人才全量档案。

---

### 获取人才字段

获取人才档案的字段（模块）配置清单，含字段类型、选项与自定义字段。限频：10 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryTalentObjectResult>?> QueryTalentObjectAsync(
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明               |
| ------ | ---- | ---- | ------------------ |
| （无） | —    | —    | 该接口无需业务参数 |

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

**说明**：`data.items` 为 `CommonSchema[]`，含字段类型、选项与自定义字段配置。

---

### 获取人才信息（v1）

按人才 ID 获取候选人（人才）完整信息，返回 v1 版 talent 结构。限频：20 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetTalentResult>?> GetTalentAsync(
    [Path] string talent_id,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                                       |
| -------------- | --------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `talent_id`    | `string`  | ✅   | 人才 ID，可通过获取人才列表接口获取，示例值：`6930815272790114324`                                         |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 people_admin_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "talent": {}
  }
}
```

**说明**：`data.talent` 为 v1 版 `Talent`；需要人才库/标签/黑名单等扩展数据时改用 v2 接口。

---

### 获取人才详细信息（v2）

按人才 ID 获取人才 v2 版详细信息（data 直接为 composite_talent，含人才库、文件夹、标签、黑名单、备注等扩展数据）；不支持查询已删除人才。限频：20 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）；备注/人才库/标签/黑名单等字段另需对应字段权限。

**函数签名**：

```csharp
Task<FeishuApiResult<GetTalentV2Result>?> GetTalentV2Async(
    [Path] string talent_id,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                                       |
| -------------- | --------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `talent_id`    | `string`  | ✅   | 人才 ID，示例值：`6930815272790114324`                                                                     |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 people_admin_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "talent_id": "",
    "basic_info": {},
    "education_list": [],
    "career_list": [],
    "project_list": [],
    "works_list": [],
    "award_list": [],
    "language_list": [],
    "sns_list": [],
    "resume_source_list": [],
    "internship_list": [],
    "customized_data_list": [],
    "resume_attachment_id_list": [],
    "resume_attachment_list": [],
    "interview_registration_list": [],
    "registration_list": [],
    "is_onboarded": false,
    "is_in_agency_period": false,
    "top_degree": 0,
    "talent_pool_id_list": [],
    "talent_folder_ref_list_v2": [],
    "tag_list": [],
    "similar_info_v2": {},
    "block_info": {},
    "talent_pool_ref_list_v2": [],
    "note_list_v2": []
  }
}
```

**说明**：`data` 直接为 v2 版聚合人才信息；不支持查询已删除人才，扩展字段需对应字段权限。

---

### 更新人才在职状态

标记人才「入职/离职」；仅适用于未通过飞书招聘投递入职的候选人，且通过本接口标记入职的只能用本接口标记离职。仅自建应用可用。限频：20 次/分钟。所需权限：hire:talent（更新人才信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UpdateTalentOnboardStatusAsync(
    [Path] string talent_id,
    [Body] UpdateTalentOnboardStatusRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名      | 类型                               | 必填 | 说明                                                                                                    |
| ----------- | ---------------------------------- | ---- | ------------------------------------------------------------------------------------------------------- |
| `talent_id` | `string`                           | ✅   | 人才 ID，示例值：`6930815272790114324`                                                                  |
| `request`   | `UpdateTalentOnboardStatusRequest` | ✅   | 请求体（operation 必填：1 入职 / 2 离职；onboard_time/overboard_time 按操作类型必填，毫秒时间戳）       |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：仅自建应用可用；通过本接口标记入职的人才只能通过本接口标记离职。

---

### 加入/移出人才黑名单

按人才 ID 将人才加入或移出招聘黑名单。仅自建应用可用。限频：10 次/秒。所需权限：hire:talent_blocklist（更新黑名单信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> ChangeTalentBlockAsync(
    [Body] ChangeTalentBlockRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                       | 必填 | 说明                                                                          |
| --------- | -------------------------- | ---- | ----------------------------------------------------------------------------- |
| `request` | `ChangeTalentBlockRequest` | ✅   | 请求体（talent_id 必填：人才 ID；option 必填：1 加入 / 2 移出；reason 加入黑名单时必填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：仅自建应用可用；`option` 为 1（加入黑名单）时 `reason` 必填。

---

### 获取背调信息列表

根据投递 ID 或背调更新时间，分页批量获取背调订单信息（含报告、进度、自定义字段等）（查询参数采用查询对象模式 `BackgroundCheckOrderListQuery`，见 AGENTS.md API-2）。限频：1000 次/分钟、50 次/秒。所需权限：hire:background_check_order（更新招聘背调信息）或 hire:background_check_order:readonly（获取招聘背调信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的发起人 ID）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetBackgroundCheckOrderListResult>?> GetBackgroundCheckOrderListAsync(
    [Query] BackgroundCheckOrderListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型                              | 必填 | 说明                                                             |
| ------- | --------------------------------- | ---- | ---------------------------------------------------------------- |
| `query` | `BackgroundCheckOrderListQuery?`   | ⚪   | 分页、投递 ID、更新时间范围与用户 ID 类型查询参数，该对象会整体展开为查询参数 |

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

**说明**：`data.items` 为 `BackgroundCheckOrder[]`，含报告、进度与自定义字段；可按更新时间增量拉取。

---

### 查询背调信息列表

按背调 ID 列表、投递 ID、时间范围、订单状态等条件组合分页查询背调订单；传入 background_check_order_id_list 时其余查询字段全部失效。限频：10 次/秒。所需权限：hire:background_check_order（更新招聘背调信息）或 hire:background_check_order:readonly（获取招聘背调信息）。字段权限：contact:user.employee_id:readonly、hire:employee.email:readonly、hire:employee.mobile:readonly、hire:talent.email:readonly、hire:talent.mobile:readonly。

**函数签名**：

```csharp
Task<FeishuApiResult<GetBackgroundCheckOrderListResult>?> BatchQueryBackgroundCheckOrderAsync(
    [Body] BatchQueryBackgroundCheckOrderRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("page_token")] string? page_token = null,
    [Query("page_size")] int? page_size = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                                   | 必填 | 说明                                                                                            |
| -------------- | -------------------------------------- | ---- | ----------------------------------------------------------------------------------------------- |
| `request`      | `BatchQueryBackgroundCheckOrderRequest` | ✅   | 查询请求体（background_check_order_id_list 最大 20 个；application_id、order_status、创建/更新时间范围选填） |
| `user_id_type` | `string?`                              | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |
| `page_token`   | `string?`                              | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                         |
| `page_size`    | `int?`                                 | ⚪   | 每页数量，默认 10，最大 100                                                                     |

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

**说明**：传入 `background_check_order_id_list`（最多 20 个）时其余查询字段全部失效。

---

### 创建三方协议

在校招投递上创建一条三方协议记录；需在招聘后台「设置-候选人流程管理-三方协议设置」勾选「通过 API 维护三方协议」，且投递为校招投递、Offer 办公地点在中国大陆。限频：10 次/秒。所需权限：hire:tripartite_agreement（更新三方协议）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateTripartiteAgreementResult>?> CreateTripartiteAgreementAsync(
    [Body] CreateTripartiteAgreementRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                               | 必填 | 说明                                                                                            |
| --------- | ---------------------------------- | ---- | ----------------------------------------------------------------------------------------------- |
| `request` | `CreateTripartiteAgreementRequest` | ✅   | 创建请求体（application_id 必填：投递 ID；state 必填：协议状态；create_time 必填：创建时间毫秒时间戳） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "id": ""
  }
}
```

**说明**：须先勾选「通过 API 维护三方协议」；仅限校招投递且 Offer 办公地点在中国大陆。

---

### 获取三方协议

按三方协议 ID 或投递 ID 查询三方协议信息（状态、创建/修改时间）；application_id 与 tripartite_agreement_id 至少填一个，都填时以协议 ID 为准（当前接口只返回一条数据，page_size/page_token 实际无效）。限频：10 次/秒。所需权限：hire:tripartite_agreement（更新三方协议）或 hire:tripartite_agreement:readonly（查看三方协议信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetTripartiteAgreementListResult>?> GetTripartiteAgreementListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("application_id")] string? application_id = null,
    [Query("tripartite_agreement_id")] string? tripartite_agreement_id = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                     | 类型      | 必填 | 说明                                              |
| -------------------------- | --------- | ---- | ------------------------------------------------- |
| `page_size`                | `int?`    | ⚪   | 分页大小（当前接口实际无效）                      |
| `page_token`               | `string?` | ⚪   | 分页标记（当前接口实际无效）                      |
| `application_id`           | `string?` | ⚪   | 投递 ID，示例值：`6930815272790114324`            |
| `tripartite_agreement_id`  | `string?` | ⚪   | 三方协议 ID，由创建接口返回，示例值：`6930815272790114325` |

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

**说明**：`application_id` 与 `tripartite_agreement_id` 至少填一个，都填时以协议 ID 为准；当前只返回一条数据，分页参数无效。

---

### 更新三方协议

更新已有三方协议的状态与修改时间；需在招聘后台勾选「通过 API 维护三方协议」。限频：10 次/秒。所需权限：hire:tripartite_agreement（更新三方协议）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UpdateTripartiteAgreementAsync(
    [Path] string tripartite_agreement_id,
    [Body] UpdateTripartiteAgreementRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                     | 类型                              | 必填 | 说明                                                                                           |
| -------------------------- | --------------------------------- | ---- | ---------------------------------------------------------------------------------------------- |
| `tripartite_agreement_id`  | `string`                          | ✅   | 三方协议 ID，示例值：`7084008015948283905`                                                     |
| `request`                  | `UpdateTripartiteAgreementRequest` | ✅   | 更新请求体（state 必填：协议状态；modify_time 必填：修改时间毫秒时间戳，不可小于创建或上次修改时间） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：`modify_time` 不可小于创建时间或上次修改时间。

---

### 删除三方协议

删除某投递下的三方协议记录；需在招聘后台勾选「通过 API 维护三方协议」。限频：10 次/秒。所需权限：hire:tripartite_agreement（更新三方协议）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteTripartiteAgreementAsync(
    [Path] string tripartite_agreement_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                    | 类型     | 必填 | 说明                                    |
| ------------------------- | -------- | ---- | --------------------------------------- |
| `tripartite_agreement_id` | `string` | ✅   | 三方协议 ID，示例值：`6930815272790114324` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：成功时 `data` 为空对象；须先勾选「通过 API 维护三方协议」。

---

### 操作候选人入职

按投递 ID 操作候选人入职并创建员工；需在招聘后台开启「通过 e-HR / OA 办公系统同步候选人入职、转正、离职事件」，且投递须处于「待入职」阶段；仅自建应用可用。限频：20 次/秒。所需权限：hire:application（更新投递信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<TransferOnboardResult>?> TransferOnboardAsync(
    [Path] string application_id,
    [Body] TransferOnboardRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    [Query("employee_type_id_type")] string? employee_type_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                  | 类型                    | 必填 | 说明                                                                                          |
| ----------------------- | ----------------------- | ---- | --------------------------------------------------------------------------------------------- |
| `application_id`        | `string`                | ✅   | 投递 ID，示例值：`7073372582620416300`                                                        |
| `request`               | `TransferOnboardRequest` | ✅   | 请求体（actual_onboard_time 实际入职时间毫秒时间戳，不传默认当前时间；部门/上级/序列/职级等选填） |
| `user_id_type`          | `string?`               | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id                                        |
| `department_id_type`    | `string?`               | ⚪   | 部门 ID 类型（open_department_id/department_id/people_admin_department_id），默认 people_admin_department_id |
| `job_level_id_type`     | `string?`               | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id         |
| `job_family_id_type`    | `string?`               | ⚪   | 职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id |
| `employee_type_id_type` | `string?`               | ⚪   | 人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "employee": {}
  }
}
```

**说明**：`data.employee` 为 `Employee`，含员工 ID、入职/转正状态与时间、部门/上级等；投递须处于「待入职」阶段。

---

### 取消候选人入职

取消待入职阶段候选人的入职（已入职者请改用更新员工状态接口做离职）；集成了飞书人事且已在人事创建待入职记录的候选人只能在飞书人事取消；仅自建应用可用。限频：10 次/秒。所需权限：hire:application（更新投递信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> CancelOnboardAsync(
    [Path] string application_id,
    [Body] CancelOnboardRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名           | 类型                 | 必填 | 说明                                                                                                       |
| ---------------- | -------------------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `application_id` | `string`             | ✅   | 投递 ID，示例值：`1111111111`                                                                              |
| `request`        | `CancelOnboardRequest` | ✅   | 请求体（termination_type 必填：1 我们拒绝了候选人 / 22 候选人拒绝了我们 / 27 其他；具体原因 ID 列表与备注选填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：仅自建应用可用；已入职者请改用「更新员工状态」接口办理离职。

---

### 通过投递 ID 获取入职信息

通过投递 ID 查询员工入职信息（查询参数采用查询对象模式 `GetEmployeeByApplicationQuery`，见 AGENTS.md API-2）。限频：1000 次/分钟、50 次/秒。所需权限：hire:employee（更新招聘员工信息）或 hire:employee:readonly（获取招聘员工信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetEmployeeByApplicationResult>?> GetEmployeeByApplicationAsync(
    [Query] GetEmployeeByApplicationQuery query,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型                                | 必填 | 说明                                                       |
| ------- | ----------------------------------- | ---- | ---------------------------------------------------------- |
| `query` | `GetEmployeeByApplicationQuery`     | ✅   | 投递 ID（必填）与各类 ID 类型查询参数，该对象会整体展开为查询参数 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "employee": {}
  }
}
```

**说明**：`data.employee` 为 `Employee`，含飞书人事雇佣 ID `external_employment_id`。

---

### 通过员工 ID 获取入职信息

通过员工 ID 查询入职信息。限频：1000 次/分钟、50 次/秒。所需权限：hire:employee:readonly（获取招聘员工信息）或 hire:employee（更新招聘员工信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetEmployeeResult>?> GetEmployeeAsync(
    [Path] string employee_id,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    [Query("employee_type_id_type")] string? employee_type_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                  | 类型      | 必填 | 说明                                                                                                    |
| ----------------------- | --------- | ---- | ------------------------------------------------------------------------------------------------------- |
| `employee_id`           | `string`  | ✅   | 员工 ID，示例值：`7379910335417927975`                                                                  |
| `user_id_type`          | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id                                                  |
| `department_id_type`    | `string?` | ⚪   | 部门 ID 类型（open_department_id/department_id/people_admin_department_id），默认 people_admin_department_id |
| `job_level_id_type`     | `string?` | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id                  |
| `job_family_id_type`    | `string?` | ⚪   | 职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id        |
| `employee_type_id_type` | `string?` | ⚪   | 人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "employee": {}
  }
}
```

**说明**：`data.employee` 为 `Employee` 入职信息。

---

### 更新员工状态

根据员工 ID 更新招聘系统内的转正、离职、恢复待入职、撤销离职、撤销转正状态；仅自建应用可用。限频：1000 次/分钟、50 次/秒。所需权限：hire:employee（更新招聘员工信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<PatchEmployeeResult>?> PatchEmployeeAsync(
    [Path] string employee_id,
    [Body] PatchEmployeeRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    [Query("employee_type_id_type")] string? employee_type_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                  | 类型                 | 必填 | 说明                                                                                                                       |
| ----------------------- | -------------------- | ---- | -------------------------------------------------------------------------------------------------------------------------- |
| `employee_id`           | `string`             | ✅   | 员工 ID，示例值：`6891613503971461384`                                                                                     |
| `request`               | `PatchEmployeeRequest` | ✅   | 请求体（operation 必填：1 转正 / 2 离职 / 3 恢复至待入职 / 4 撤销离职 / 5 撤销转正；conversion_info 转正时必填、overboard_info 离职时必填） |
| `user_id_type`          | `string?`            | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id                                                                     |
| `department_id_type`    | `string?`            | ⚪   | 部门 ID 类型（open_department_id/department_id/people_admin_department_id），默认 people_admin_department_id                |
| `job_level_id_type`     | `string?`            | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id                                     |
| `job_family_id_type`    | `string?`            | ⚪   | 职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id                           |
| `employee_type_id_type` | `string?`            | ⚪   | 人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id                 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "employee": {}
  }
}
```

**说明**：`operation` 为 1 时 `conversion_info` 必填，为 2 时 `overboard_info` 必填；仅自建应用可用。

---

### 更新 e-HR 导入任务结果

处理完「导入 e-HR」事件后，向飞书回写该导入任务的成功/失败结果及跳转链接；仅自建应用可用。限频：50 次/秒（英文文档标注特殊限频）。所需权限：hire:ehr_import（更新导入 e-HR 任务）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> PatchEhrImportTaskAsync(
    [Path] string ehr_import_task_id,
    [Body] PatchEhrImportTaskRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                | 类型                        | 必填 | 说明                                                                                        |
| --------------------- | --------------------------- | ---- | ------------------------------------------------------------------------------------------- |
| `ehr_import_task_id`  | `string`                    | ✅   | 导入任务 ID，来源于「导入 e-HR」事件中的 task_id，示例值：`6914551145542568199`              |
| `request`             | `PatchEhrImportTaskRequest` | ✅   | 请求体（state 必填：1 导入成功 / 2 导入失败；fail_reason 失败原因、redirect_url 跳转链接选填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：仅自建应用可用；`ehr_import_task_id` 取自「导入 e-HR」事件的 `task_id`。

---

### 创建 Offer

传入 Offer 基本信息，为指定投递创建 Offer（正式或实习）；仅自建应用可用。限频：10 次/秒。所需权限：hire:offer（更新 offer 信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的敏感字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateOfferResult>?> CreateOfferAsync(
    [Body] CreateOfferRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    [Query("employee_type_id_type")] string? employee_type_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                  | 类型                | 必填 | 说明                                                                                                    |
| ----------------------- | ------------------- | ---- | ------------------------------------------------------------------------------------------------------- |
| `request`               | `CreateOfferRequest` | ✅   | 创建请求体（application_id 必填；basic_info 必填，含部门/直属上级/负责人/操作人；salary_info、customized_info_list 选填） |
| `user_id_type`          | `string?`           | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id                                   |
| `department_id_type`    | `string?`           | ⚪   | 部门 ID 类型（open_department_id/department_id），默认 open_department_id                               |
| `job_level_id_type`     | `string?`           | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id                  |
| `job_family_id_type`    | `string?`           | ⚪   | 职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id        |
| `employee_type_id_type` | `string?`           | ⚪   | 人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "offer_id": "",
    "application_id": "",
    "schema_id": "",
    "offer_type": 0,
    "basic_info": {},
    "salary_info": {},
    "customized_info_list": []
  }
}
```

**说明**：仅自建应用可用；`schema_id` 需使用「获取 Offer 申请表模板信息」返回的最新版本。

---

### 更新 Offer 信息

全量覆盖更新 Offer 的基本信息、薪资信息与自定义信息；状态为「Offer 已发送(6)」「候选人已接受(7)」时不可更新；仅自建应用可用。限频：10 次/秒。所需权限：hire:offer（更新 offer 信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的敏感字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<UpdateOfferResult>?> UpdateOfferAsync(
    [Path] string offer_id,
    [Body] UpdateOfferRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    [Query("employee_type_id_type")] string? employee_type_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                  | 类型                | 必填 | 说明                                                                                    |
| ----------------------- | ------------------- | ---- | --------------------------------------------------------------------------------------- |
| `offer_id`              | `string`            | ✅   | Offer ID，可通过获取 Offer 列表接口获取，示例值：`7085989097067563300`                  |
| `request`               | `UpdateOfferRequest` | ✅   | 更新请求体（schema_id 必填且须最新版；basic_info 必填；旧自定义字段不传=删除）         |
| `user_id_type`          | `string?`           | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id                   |
| `department_id_type`    | `string?`           | ⚪   | 部门 ID 类型（open_department_id/department_id），默认 open_department_id               |
| `job_level_id_type`     | `string?`           | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id  |
| `job_family_id_type`    | `string?`           | ⚪   | 职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id |
| `employee_type_id_type` | `string?`           | ⚪   | 人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "offer_id": "",
    "schema_id": "",
    "basic_info": {},
    "salary_info": {},
    "customized_info_list": []
  }
}
```

**说明**：为全量覆盖更新，旧自定义字段不传视为删除；状态为「已发送/已接受」时不可更新。

---

### 获取 Offer 信息（按投递 ID）

根据投递 ID 获取该投递下的 Offer 数据；暂不支持查询实习 Offer。限频：20 次/秒。所需权限：hire:application（更新投递信息）或 hire:application:readonly（获取投递信息）。字段权限：contact:user.employee_id:readonly（返回的敏感字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetApplicationOfferResult>?> GetApplicationOfferAsync(
    [Path] string application_id,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    [Query("employee_type_id_type")] string? employee_type_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                  | 类型      | 必填 | 说明                                                                                    |
| ----------------------- | --------- | ---- | --------------------------------------------------------------------------------------- |
| `application_id`        | `string`  | ✅   | 投递 ID，示例值：`6701528341100366094`                                                  |
| `user_id_type`          | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id                                   |
| `department_id_type`    | `string?` | ⚪   | 部门 ID 类型（open_department_id/department_id），默认 open_department_id               |
| `job_level_id_type`     | `string?` | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id  |
| `job_family_id_type`    | `string?` | ⚪   | 职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id |
| `employee_type_id_type` | `string?` | ⚪   | 人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "offer": {}
  }
}
```

**说明**：`data.offer` 为 `Offer`，含 basic_info、salary_plan、offer_status 与发送记录；暂不支持查询实习 Offer。

---

### 获取 Offer 详情

根据 Offer ID 获取 Offer 详细信息（含薪资计划、发送记录与签署信息）。限频：20 次/秒。所需权限：hire:offer:low_sensitive_info:readonly（查看 offer 的基础信息）/ hire:offer:readonly（获取 offer 信息）/ hire:offer（更新 offer 信息）。字段权限：hire:offer:readonly、hire:offer、contact:user.employee_id:readonly（remark、level、salary_plan 等敏感字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetOfferResult>?> GetOfferAsync(
    [Path] string offer_id,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("department_id_type")] string? department_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    [Query("job_family_id_type")] string? job_family_id_type = null,
    [Query("employee_type_id_type")] string? employee_type_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                  | 类型      | 必填 | 说明                                                                                                       |
| ----------------------- | --------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `offer_id`              | `string`  | ✅   | Offer ID，示例值：`7085989097067563300`                                                                    |
| `user_id_type`          | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id                                      |
| `department_id_type`    | `string?` | ⚪   | 部门 ID 类型（open_department_id/department_id），默认 open_department_id                                  |
| `job_level_id_type`     | `string?` | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id                     |
| `job_family_id_type`    | `string?` | ⚪   | 职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id           |
| `employee_type_id_type` | `string?` | ⚪   | 人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "offer": {}
  }
}
```

**说明**：`data.offer` 为 `Offer` 详情，含 offer_type、offer_status、basic_info、salary_plan 等；敏感字段需对应字段权限。

---

### 获取 Offer 列表

根据人才 ID 分页获取 Offer 列表。限频：1000 次/分钟、50 次/秒。所需权限：hire:offer:readonly（获取 offer 信息）或 hire:offer（更新 offer 信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetOfferListResult>?> GetOfferListAsync(
    [Query("talent_id")] string talent_id,
    [Query("page_token")] string? page_token = null,
    [Query("page_size")] int? page_size = null,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("employee_type_id_type")] string? employee_type_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                  | 类型      | 必填 | 说明                                                                                                       |
| ----------------------- | --------- | ---- | ---------------------------------------------------------------------------------------------------------- |
| `talent_id`             | `string`  | ✅   | 人才 ID（必填），示例值：`6930815272790114324`                                                             |
| `page_token`            | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                                    |
| `page_size`             | `int?`    | ⚪   | 每页数量，最大 200，默认 10                                                                                |
| `user_id_type`          | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id（将下线，不建议使用）                |
| `employee_type_id_type` | `string?` | ⚪   | 人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id |

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

**说明**：`data.items` 为 `OfferListInfo[]`；`talent_id` 必填，`user_id_type` 默认 open_id（将下线）。

---

### 更新 Offer 状态

通过 Offer ID 更新 Offer 的审批状态或发送和接受状态（对接 OA 场景）；需在飞书招聘 Offer 规则设置中开启对应 OA 开关，否则返回错误；仅自建应用可用。限频：100 次/分钟。所需权限：hire:offer（更新 offer 信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> ChangeOfferStatusAsync(
    [Path] string offer_id,
    [Body] ChangeOfferStatusRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名     | 类型                      | 必填 | 说明                                                                                                                                                       |
| ---------- | ------------------------- | ---- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `offer_id` | `string`                  | ✅   | Offer ID，示例值：`7085989097067563300`                                                                                                                    |
| `request`  | `ChangeOfferStatusRequest` | ✅   | 请求体（offer_status 必填：2 审批中/3 审批已撤回/4 审批通过/5 审批不通过/6 已发送/7 被接受/8 被拒绝/9 已失效/10 已创建；6 时需 expiration_date，8 时需 termination_reason_id_list） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：须先在 Offer 规则设置中开启对应 OA 开关；状态 6 需 `expiration_date`，状态 8 需 `termination_reason_id_list`。

---

### 更新实习 Offer 入/离职状态

对「实习待入职」的实习 Offer 确认入职/放弃入职，或对「实习已入职」的实习 Offer 操作离职；仅自建应用可用。限频：1000 次/分钟、50 次/秒。所需权限：hire:offer（更新 offer 信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<ChangeInternOfferStatusResult>?> ChangeInternOfferStatusAsync(
    [Path] string offer_id,
    [Body] ChangeInternOfferStatusRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名     | 类型                           | 必填 | 说明                                                                                                                              |
| ---------- | ------------------------------ | ---- | --------------------------------------------------------------------------------------------------------------------------------- |
| `offer_id` | `string`                       | ✅   | 实习 Offer ID，示例值：`7085989097067563300`                                                                                       |
| `request`  | `ChangeInternOfferStatusRequest` | ✅   | 请求体（operation 必填：confirm_onboarding/cancel_onboarding/offboard；onboarding_info 在确认入职时必填，offboarding_info 在离职时必填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "offer_id": "",
    "operation": "",
    "onboarding_info": {},
    "offboarding_info": {}
  }
}
```

**说明**：仅自建应用可用；确认入职时 `onboarding_info` 必填，离职时 `offboarding_info` 必填。

---

### 获取面试信息

按投递 ID、面试 ID 或面试开始时间区间筛选，分页获取面试列表；application_id、interview_id、start_time、end_time 不允许同时为空（查询参数采用查询对象模式 `InterviewListQuery`，见 AGENTS.md API-2）。限频：50 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetInterviewListResult>?> GetInterviewListAsync(
    [Query] InterviewListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型                  | 必填 | 说明                                                                     |
| ------- | --------------------- | ---- | ------------------------------------------------------------------------ |
| `query` | `InterviewListQuery?` | ⚪   | 分页、投递/面试 ID、面试开始时间范围与各类 ID 类型查询参数，该对象会整体展开为查询参数 |

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

**说明**：`application_id`、`interview_id`、`start_time`、`end_time` 不允许同时为空；`data.items` 为 `InterviewExtend[]`。

---

### 获取人才面试信息

按人才 ID 获取该人才下所有投递的全部面试信息（不分页）。限频：1000 次/分钟、50 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetInterviewByTalentResult>?> GetInterviewByTalentAsync(
    [Query("talent_id")] string talent_id,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("job_level_id_type")] string? job_level_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名              | 类型      | 必填 | 说明                                                                                    |
| ------------------- | --------- | ---- | --------------------------------------------------------------------------------------- |
| `talent_id`         | `string`  | ✅   | 人才 ID（必填），示例值：`6930815272790114324`                                          |
| `user_id_type`      | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |
| `job_level_id_type` | `string?` | ⚪   | 职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id  |

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

**说明**：`data.items` 为 `TalentInterview[]`，按投递分组（含 `application_id`、`interview_list`），结果不分页。

---

### 获取面试评价详细信息（v1）

按面试评价 ID 获取单条面试评价详情（结论、得分、面试官、题目与维度评价）。限频：20 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。字段权限：contact:user.employee_id:readonly。

**函数签名**：

```csharp
Task<FeishuApiResult<GetInterviewRecordResult>?> GetInterviewRecordAsync(
    [Path] string interview_record_id,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                | 类型      | 必填 | 说明                                             |
| --------------------- | --------- | ---- | ------------------------------------------------ |
| `interview_record_id` | `string`  | ✅   | 面试评价 ID，示例值：`7047318856652261676`      |
| `user_id_type`        | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "interview_record": {}
  }
}
```

**说明**：`data.interview_record` 为 v1 版 `InterviewRecord`，含结论、得分、面试官、题目与维度评价。

---

### 获取面试评价详细信息（新版 v2）

按面试评价 ID 获取新版结构详情（record_score 总得分与 module_assessments 模块评价）。限频：20 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。字段权限：contact:user.employee_id:readonly。

**函数签名**：

```csharp
Task<FeishuApiResult<GetInterviewRecordV2Result>?> GetInterviewRecordV2Async(
    [Path] string interview_record_id,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                | 类型      | 必填 | 说明                                             |
| --------------------- | --------- | ---- | ------------------------------------------------ |
| `interview_record_id` | `string`  | ✅   | 面试评价 ID，示例值：`7047318856652261676`      |
| `user_id_type`        | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "interview_record": {}
  }
}
```

**说明**：`data.interview_record` 为 `InterviewRecordV2`，含 `record_score` 总得分与 `module_assessments` 模块评价。

---

### 批量获取面试评价详细信息（v1）

按评价 ID 列表或分页批量获取面试评价（v1 旧结构）；传入 ids 时不分页。限频：10 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。字段权限：contact:user.employee_id:readonly。

**函数签名**：

```csharp
Task<FeishuApiResult<GetInterviewRecordListResult>?> GetInterviewRecordListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("ids")] string[]? ids = null,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型        | 必填 | 说明                                                    |
| -------------- | ----------- | ---- | ------------------------------------------------------- |
| `page_size`    | `int?`      | ⚪   | 每页数量，默认 10，最大 100                              |
| `page_token`   | `string?`   | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token |
| `ids`          | `string[]?` | ⚪   | 面试评价 ID 列表，最大 100 个；使用该筛选项时不分页      |
| `user_id_type` | `string?`   | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id   |

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

**说明**：`data.items` 为 `InterviewRecord[]`；传入 `ids`（最大 100 个）时不分页。

---

### 批量获取面试评价详细信息（新版 v2）

按评价 ID 列表或分页批量获取面试评价（新版结构，含模块评价）；传入 ids 时不分页。限频：10 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。字段权限：contact:user.employee_id:readonly。

**函数签名**：

```csharp
Task<FeishuApiResult<GetInterviewRecordListV2Result>?> GetInterviewRecordListV2Async(
    [Query("ids")] string[]? ids = null,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型        | 必填 | 说明                                                            |
| -------------- | ----------- | ---- | --------------------------------------------------------------- |
| `ids`          | `string[]?` | ⚪   | 面试评价 ID 列表，长度 0～100；使用该筛选项时不分页              |
| `page_size`    | `int?`      | ⚪   | 每页数量，0～100；不传时默认按 ids 参数获取数据                  |
| `page_token`   | `string?`   | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token          |
| `user_id_type` | `string?`   | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id           |

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

**说明**：`data.items` 为 `InterviewRecordV2[]`；不传 `page_size` 时默认按 `ids` 获取数据。

---

### 获取面试记录附件

获取面试记录 PDF 附件下载链接（含投递基本信息与面试评价信息）；不传 interview_record_id 时返回该投递下所有面试评价的附件。限频：20 次/分钟。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetInterviewRecordAttachmentResult>?> GetInterviewRecordAttachmentAsync(
    [Query("application_id")] string application_id,
    [Query("interview_record_id")] string? interview_record_id = null,
    [Query("language")] int? language = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                | 类型      | 必填 | 说明                                                        |
| --------------------- | --------- | ---- | ----------------------------------------------------------- |
| `application_id`      | `string`  | ✅   | 投递 ID（必填），示例值：`6701528341100366094`              |
| `interview_record_id` | `string?` | ⚪   | 面试评价 ID；不填则获取该投递下所有面试评价的附件           |
| `language`            | `int?`    | ⚪   | 附件语言：1 中文（默认）/ 2 英文                            |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "attachment": {}
  }
}
```

**说明**：`data.attachment` 为 `InterviewAttachment`，含 id、url（30 分钟有效）、name、mime、create_time。

---

### 获取面试速记明细

获取指定面试的速记（逐句转写）明细记录；仅自建应用可用。限频：10 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetInterviewMinutesResult>?> GetInterviewMinutesAsync(
    [Query("interview_id")] string interview_id,
    [Query("page_token")] string? page_token = null,
    [Query("page_size")] int? page_size = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                    |
| -------------- | --------- | ---- | ------------------------------------------------------- |
| `interview_id` | `string`  | ✅   | 面试 ID（必填），示例值：`7047318856652261676`          |
| `page_token`   | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token |
| `page_size`    | `int?`    | ⚪   | 本次获取的语句最大数量，默认 20，范围 1～100            |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "minutes": {},
    "page_token": "",
    "has_more": false
  }
}
```

**说明**：`data.minutes` 为 `InterviewMinutes`，其 `sentences` 为速记句子列表；仅自建应用可用。

---

### 获取面试满意度问卷列表

批量获取面试满意度问卷（完成情况、题目及作答内容）；application_id 与 interview_id 不可同时填写（查询参数采用查询对象模式 `InterviewQuestionnaireListQuery`，见 AGENTS.md API-2）。限频：1000 次/分钟、50 次/秒。所需权限：hire:questionnaire:readonly（获取面试满意度问卷信息）或 hire:questionnaire（更新面试满意度问卷信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetInterviewQuestionnaireListResult>?> GetInterviewQuestionnaireListAsync(
    [Query] InterviewQuestionnaireListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型                                | 必填 | 说明                                                               |
| ------- | ----------------------------------- | ---- | ------------------------------------------------------------------ |
| `query` | `InterviewQuestionnaireListQuery?`  | ⚪   | 分页、投递/面试 ID 与更新时间范围查询参数，该对象会整体展开为查询参数 |

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

**说明**：`data.items` 为 `InterviewQuestionnaire[]`；`application_id` 与 `interview_id` 不可同时填写。
