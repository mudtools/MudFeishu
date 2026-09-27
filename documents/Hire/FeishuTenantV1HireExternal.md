# 外部系统信息导入 - 租户令牌（FeishuTenantV1HireExternal）

## 接口名称

**外部系统信息导入（租户令牌）** -（`IFeishuTenantV1HireExternal`）

## 功能描述

提供以租户身份将外部招聘系统数据导入飞书招聘的能力。飞书招聘（Hire）外部系统信息导入入口域 SDK 是一组服务端 OpenAPI 的封装，用于将外部系统（ATS/RMS）中的人才外部创建时间、外部投递、外部面试与面评、外部 Offer、外部背调以及内推奖励导入或同步到飞书招聘。本接口全部端点仅支持 tenant_access_token 调用。支持创建人才外部信息、更新人才外部信息、创建外部投递、更新外部投递、获取外部投递列表、删除外部投递、创建外部面试、更新外部面试、查询外部面试列表、删除外部面试、创建外部面试评价、更新外部面试评价、创建外部 Offer、更新外部 Offer、查询外部 Offer 列表、删除外部 Offer、创建外部背调、更新外部背调、查询外部背调列表、删除外部背调、导入外部内推奖励、删除外部内推奖励等操作。

## 参考文档

- [创建外部投递 - 飞书开放平台](https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/import-external-system-information/create)

## 函数列表

| 函数名称                                | 功能描述               | 认证方式 | HTTP 方法 |
| --------------------------------------- | ---------------------- | -------- | --------- |
| CreateTalentExternalInfoAsync           | 创建人才外部信息       | 租户令牌 | POST      |
| UpdateTalentExternalInfoAsync           | 更新人才外部信息       | 租户令牌 | PUT       |
| CreateExternalApplicationAsync          | 创建外部投递           | 租户令牌 | POST      |
| UpdateExternalApplicationAsync          | 更新外部投递           | 租户令牌 | PUT       |
| GetExternalApplicationListAsync         | 获取外部投递列表       | 租户令牌 | GET       |
| DeleteExternalApplicationAsync          | 删除外部投递           | 租户令牌 | DELETE    |
| CreateExternalInterviewAsync            | 创建外部面试           | 租户令牌 | POST      |
| UpdateExternalInterviewAsync            | 更新外部面试           | 租户令牌 | PUT       |
| BatchQueryExternalInterviewAsync        | 查询外部面试列表       | 租户令牌 | POST      |
| DeleteExternalInterviewAsync            | 删除外部面试           | 租户令牌 | DELETE    |
| CreateExternalInterviewAssessmentAsync  | 创建外部面试评价       | 租户令牌 | POST      |
| PatchExternalInterviewAssessmentAsync   | 更新外部面试评价       | 租户令牌 | PATCH     |
| CreateExternalOfferAsync                | 创建外部 Offer         | 租户令牌 | POST      |
| UpdateExternalOfferAsync                | 更新外部 Offer         | 租户令牌 | PUT       |
| BatchQueryExternalOfferAsync            | 查询外部 Offer 列表    | 租户令牌 | POST      |
| DeleteExternalOfferAsync                | 删除外部 Offer         | 租户令牌 | DELETE    |
| CreateExternalBackgroundCheckAsync      | 创建外部背调           | 租户令牌 | POST      |
| UpdateExternalBackgroundCheckAsync      | 更新外部背调           | 租户令牌 | PUT       |
| BatchQueryExternalBackgroundCheckAsync  | 查询外部背调列表       | 租户令牌 | POST      |
| DeleteExternalBackgroundCheckAsync      | 删除外部背调           | 租户令牌 | DELETE    |
| CreateExternalReferralRewardAsync       | 导入外部内推奖励       | 租户令牌 | POST      |
| DeleteExternalReferralRewardAsync       | 删除外部内推奖励       | 租户令牌 | DELETE    |

## 函数详细内容

### 创建人才外部信息

为人才创建外部系统信息（人才在外部系统的创建时间）。限频：1000 次/分钟、50 次/秒。所需权限：hire:talent（更新人才信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateTalentExternalInfoResult>?> CreateTalentExternalInfoAsync(
    [Path] string talent_id,
    [Body] TalentExternalInfoRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名      | 类型                       | 必填 | 说明                                                                           |
| ----------- | -------------------------- | ---- | ------------------------------------------------------------------------------ |
| `talent_id` | `string`                   | ✅   | 人才 ID，示例值：`7043758982146345223`                                         |
| `request`   | `TalentExternalInfoRequest` | ✅   | 创建请求体（external_create_time 必填：人才在外部系统的创建时间，毫秒时间戳） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_info": {}
  }
}
```

**说明**：`data.external_info` 为 `TalentExternalInfo`；`external_create_time` 为毫秒时间戳。

---

### 更新人才外部信息

更新人才的外部系统信息（人才在外部系统的创建时间）。限频：1000 次/分钟、50 次/秒。所需权限：hire:talent（更新人才信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<UpdateTalentExternalInfoResult>?> UpdateTalentExternalInfoAsync(
    [Path] string talent_id,
    [Body] TalentExternalInfoRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名      | 类型                       | 必填 | 说明                                                                           |
| ----------- | -------------------------- | ---- | ------------------------------------------------------------------------------ |
| `talent_id` | `string`                   | ✅   | 人才 ID，示例值：`7043758982146345223`                                         |
| `request`   | `TalentExternalInfoRequest` | ✅   | 更新请求体（external_create_time 必填：人才在外部系统的创建时间，毫秒时间戳） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_info": {}
  }
}
```

**说明**：`data.external_info` 为更新后的 `TalentExternalInfo`。

---

### 创建外部投递

创建来自外部系统的投递；external_id 为幂等字段，同一 external_id 24 小时内仅可创建一次。限频：20 次/秒。所需权限：hire:external_application（更新外部投递信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateExternalApplicationResult>?> CreateExternalApplicationAsync(
    [Body] CreateExternalApplicationRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                             | 必填 | 说明                                                                                    |
| --------- | -------------------------------- | ---- | --------------------------------------------------------------------------------------- |
| `request` | `CreateExternalApplicationRequest` | ✅   | 创建请求体（talent_id 必填；external_id、职位/简历来源/阶段/终止原因/投递类型/时间字段选填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_application": {}
  }
}
```

**说明**：`external_id` 为幂等字段，同一值 24 小时内仅可创建一次投递。

---

### 更新外部投递

按外部投递 ID 覆盖更新外部投递的字段。限频：20 次/秒。所需权限：hire:external_application（更新外部投递信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<UpdateExternalApplicationResult>?> UpdateExternalApplicationAsync(
    [Path] string external_application_id,
    [Body] UpdateExternalApplicationRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                    | 类型                             | 必填 | 说明                                                                                                                       |
| ------------------------- | -------------------------------- | ---- | -------------------------------------------------------------------------------------------------------------------------- |
| `external_application_id` | `string`                         | ✅   | 外部投递 ID，示例值：`6960663240925956660`                                                                                 |
| `request`                 | `UpdateExternalApplicationRequest` | ✅   | 更新请求体（job_recruitment_type、job_title、resume_source、stage、termination_reason、delivery_type、modify_time、create_time、termination_type 选填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_application": {}
  }
}
```

**说明**：为覆盖更新，未传入的字段按接口语义处理，修改后请同步 `modify_time`。

---

### 获取外部投递列表

按人才 ID 分页获取外部投递信息列表。限频：20 次/秒。所需权限：hire:external_application（更新外部投递信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetExternalApplicationListResult>?> GetExternalApplicationListAsync(
    [Query("talent_id")] string? talent_id = null,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                    |
| ------------ | --------- | ---- | ------------------------------------------------------- |
| `talent_id`  | `string?` | ⚪   | 人才 ID，示例值：`6960663240925956660`                 |
| `page_size`  | `int?`    | ⚪   | 每页数量，最大 20                                       |
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

**说明**：`data.items` 为 `ExternalApplication[]`；`talent_id` 为空时返回全量外部投递。

---

### 删除外部投递

按外部投递 ID 删除外部投递。限频：20 次/分钟。所需权限：hire:external_application（更新外部投递信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<DeleteExternalApplicationResult>?> DeleteExternalApplicationAsync(
    [Path] string external_application_id,
    [Query("talent_id")] string? talent_id = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                    | 类型      | 必填 | 说明                                    |
| ------------------------- | --------- | ---- | --------------------------------------- |
| `external_application_id` | `string`  | ✅   | 外部投递 ID，示例值：`6960663240925956660` |
| `talent_id`               | `string?` | ⚪   | 人才 ID，示例值：`6960663240925956660`     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_application": {}
  }
}
```

**说明**：`data.external_application` 为被删除的外部投递信息。

---

### 创建外部面试

创建来自外部系统的面试；external_id 为幂等字段，可携带面试评价列表。限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateExternalInterviewResult>?> CreateExternalInterviewAsync(
    [Body] CreateExternalInterviewRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                          | 必填 | 说明                                                                                                          |
| --------- | ----------------------------- | ---- | ------------------------------------------------------------------------------------------------------------- |
| `request` | `CreateExternalInterviewRequest` | ✅   | 创建请求体（external_application_id 必填；external_id、participate_status、begin_time、end_time、interview_assessments 选填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_interview": {}
  }
}
```

**说明**：`external_id` 为幂等字段；可在创建时通过 `interview_assessments` 一并携带面试评价。

---

### 更新外部面试

按外部面试 ID 覆盖更新外部面试的字段。限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<UpdateExternalInterviewResult>?> UpdateExternalInterviewAsync(
    [Path] string external_interview_id,
    [Body] UpdateExternalInterviewRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                 | 类型                          | 必填 | 说明                                                                                                  |
| ---------------------- | ----------------------------- | ---- | ----------------------------------------------------------------------------------------------------- |
| `external_interview_id` | `string`                     | ✅   | 外部面试 ID，可通过查询外部面试列表接口获取，示例值：`6960663240925956660`                            |
| `request`              | `UpdateExternalInterviewRequest` | ✅   | 更新请求体（external_application_id 必填；participate_status、begin_time、end_time、interview_assessments 选填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_interview": {}
  }
}
```

**说明**：为覆盖更新；外部面试 ID 可通过查询外部面试列表接口获取。

---

### 查询外部面试列表

按外部投递 ID 或外部面试 ID 列表分页查询外部面试信息；传 external_interview_id_list 时以其为准。限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）或 hire:external_application:readonly（查看外部投递）。

**函数签名**：

```csharp
Task<FeishuApiResult<BatchQueryExternalInterviewResult>?> BatchQueryExternalInterviewAsync(
    [Body] BatchQueryExternalInterviewRequest request,
    [Query("external_application_id")] string? external_application_id = null,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                    | 类型                               | 必填 | 说明                                                    |
| ------------------------- | ---------------------------------- | ---- | ------------------------------------------------------- |
| `request`                 | `BatchQueryExternalInterviewRequest` | ✅   | 查询请求体（external_interview_id_list 最多 20 个，传入时以其为准） |
| `external_application_id` | `string?`                          | ⚪   | 外部投递 ID，示例值：`6960663240925956660`              |
| `page_size`               | `int?`                             | ⚪   | 每页数量，范围 1~20，默认 10                            |
| `page_token`              | `string?`                          | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token |

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

**说明**：`data.items` 为 `ExternalInterview[]`；传入 `external_interview_id_list` 时其他查询条件失效。

---

### 删除外部面试

按外部面试 ID 删除外部面试。限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteExternalInterviewAsync(
    [Path] string external_interview_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                  | 类型     | 必填 | 说明                                    |
| ----------------------- | -------- | ---- | --------------------------------------- |
| `external_interview_id` | `string` | ✅   | 外部面试 ID，示例值：`6960663240925956660` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：成功时 `data` 为空对象。

---

### 创建外部面试评价

导入来自其他系统的面评信息，创建为外部面评；external_id 为幂等字段。限频：20 次/秒。所需权限：hire:external_application（更新外部投递信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateExternalInterviewAssessmentResult>?> CreateExternalInterviewAssessmentAsync(
    [Body] CreateExternalInterviewAssessmentRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                                     | 必填 | 说明                                                                                                                 |
| --------- | ---------------------------------------- | ---- | -------------------------------------------------------------------------------------------------------------------- |
| `request` | `CreateExternalInterviewAssessmentRequest` | ✅   | 创建请求体（external_interview_id 必填；external_id、username、conclusion、assessment_dimension_list、content 选填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_interview_assessment": {}
  }
}
```

**说明**：`external_id` 为幂等字段；面评须挂在已创建的外部面试下。

---

### 更新外部面试评价

按外部面评 ID 局部更新外部面评字段，留空的字段不更新。限频：20 次/秒。所需权限：hire:external_application（更新外部投递信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<PatchExternalInterviewAssessmentResult>?> PatchExternalInterviewAssessmentAsync(
    [Path] string external_interview_assessment_id,
    [Body] PatchExternalInterviewAssessmentRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                             | 类型                                    | 必填 | 说明                                                                                |
| ---------------------------------- | --------------------------------------- | ---- | ----------------------------------------------------------------------------------- |
| `external_interview_assessment_id` | `string`                               | ✅   | 外部面评 ID，示例值：`6930815272790114324`                                          |
| `request`                          | `PatchExternalInterviewAssessmentRequest` | ✅   | 更新请求体（username、conclusion、assessment_dimension_list、content 选填）        |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_interview_assessment": {}
  }
}
```

**说明**：为局部更新（PATCH），留空的字段保持原值不变。

---

### 创建外部 Offer

从其他系统导入 Offer 信息并创建为外部 Offer；external_id 为幂等字段。限频：10 次/秒。所需权限：hire:external_offer（更新外部 Offer）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateExternalOfferResult>?> CreateExternalOfferAsync(
    [Body] CreateExternalOfferRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                       | 必填 | 说明                                                                                                        |
| --------- | -------------------------- | ---- | ----------------------------------------------------------------------------------------------------------- |
| `request` | `CreateExternalOfferRequest` | ✅   | 创建请求体（external_application_id 必填；external_id、biz_create_time、owner、offer_status、attachment_id_list 选填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_offer": {}
  }
}
```

**说明**：`external_id` 为幂等字段；Offer 须挂在已创建的外部投递下。

---

### 更新外部 Offer

按外部 Offer ID 覆盖更新外部 Offer 的字段。限频：10 次/秒。所需权限：hire:external_offer（更新外部 Offer）。

**函数签名**：

```csharp
Task<FeishuApiResult<UpdateExternalOfferResult>?> UpdateExternalOfferAsync(
    [Path] string external_offer_id,
    [Body] UpdateExternalOfferRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名             | 类型                       | 必填 | 说明                                                                                                        |
| ------------------ | -------------------------- | ---- | ----------------------------------------------------------------------------------------------------------- |
| `external_offer_id` | `string`                  | ✅   | 外部 Offer ID，可通过查询外部 Offer 列表接口获取，示例值：`6960663240925956660`                              |
| `request`          | `UpdateExternalOfferRequest` | ✅   | 更新请求体（external_application_id 必填；biz_create_time、owner、offer_status、attachment_id_list 选填）   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_offer": {}
  }
}
```

**说明**：为覆盖更新；外部 Offer ID 可通过查询外部 Offer 列表接口获取。

---

### 查询外部 Offer 列表

按外部投递 ID 或外部 Offer ID 列表分页查询外部 Offer 信息；传 external_offer_id_list 时以其为准。限频：10 次/秒。所需权限：hire:external_offer（更新外部 Offer）或 hire:external_offer:readonly（查看外部 Offer）。

**函数签名**：

```csharp
Task<FeishuApiResult<BatchQueryExternalOfferResult>?> BatchQueryExternalOfferAsync(
    [Body] BatchQueryExternalOfferRequest request,
    [Query("external_application_id")] string? external_application_id = null,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                    | 类型                          | 必填 | 说明                                                    |
| ------------------------- | ----------------------------- | ---- | ------------------------------------------------------- |
| `request`                 | `BatchQueryExternalOfferRequest` | ✅   | 查询请求体（external_offer_id_list 最多 20 个，传入时以其为准） |
| `external_application_id` | `string?`                     | ⚪   | 外部投递 ID，示例值：`6960663240925956660`              |
| `page_size`               | `int?`                        | ⚪   | 每页数量，最大 20，默认 10                              |
| `page_token`              | `string?`                     | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token |

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

**说明**：`data.items` 为 `ExternalOffer[]`；传入 `external_offer_id_list` 时其他查询条件失效。

---

### 删除外部 Offer

按外部 Offer ID 删除外部 Offer。限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteExternalOfferAsync(
    [Path] string external_offer_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名             | 类型     | 必填 | 说明                                    |
| ------------------ | -------- | ---- | --------------------------------------- |
| `external_offer_id` | `string` | ✅   | 外部 Offer ID，示例值：`6960663240925956660` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：成功时 `data` 为空对象。

---

### 创建外部背调

导入来自外部系统的背调信息；external_id 为幂等字段，同一 external_id 24 小时内仅可创建一次。限频：20 次/秒。所需权限：hire:external_application（更新外部投递信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateExternalBackgroundCheckResult>?> CreateExternalBackgroundCheckAsync(
    [Body] CreateExternalBackgroundCheckRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                                | 必填 | 说明                                                                                                  |
| --------- | ----------------------------------- | ---- | ----------------------------------------------------------------------------------------------------- |
| `request` | `CreateExternalBackgroundCheckRequest` | ✅   | 创建请求体（external_application_id 必填；external_id、date、name、result、attachment_id_list 选填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_background_check": {}
  }
}
```

**说明**：`external_id` 为幂等字段，同一值 24 小时内仅可创建一次背调。

---

### 更新外部背调

按外部背调 ID 覆盖更新外部背调的字段。限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<UpdateExternalBackgroundCheckResult>?> UpdateExternalBackgroundCheckAsync(
    [Path] string external_background_check_id,
    [Body] UpdateExternalBackgroundCheckRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                         | 类型                                | 必填 | 说明                                                                                       |
| ------------------------------ | ----------------------------------- | ---- | ------------------------------------------------------------------------------------------ |
| `external_background_check_id` | `string`                           | ✅   | 外部背调 ID，可通过查询外部背调列表接口获取，示例值：`6960663240925956660`                  |
| `request`                      | `UpdateExternalBackgroundCheckRequest` | ✅   | 更新请求体（external_application_id 必填；date、name、result、attachment_id_list 选填）    |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_background_check": {}
  }
}
```

**说明**：为覆盖更新；外部背调 ID 可通过查询外部背调列表接口获取。

---

### 查询外部背调列表

按外部投递 ID 或外部背调 ID 列表分页查询外部背调信息；传 external_background_check_id_list 时以其为准。限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）或 hire:external_application:readonly（查看外部投递）。

**函数签名**：

```csharp
Task<FeishuApiResult<BatchQueryExternalBackgroundCheckResult>?> BatchQueryExternalBackgroundCheckAsync(
    [Body] BatchQueryExternalBackgroundCheckRequest request,
    [Query("external_application_id")] string? external_application_id = null,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                         | 类型                                      | 必填 | 说明                                                          |
| ------------------------------ | ----------------------------------------- | ---- | ------------------------------------------------------------- |
| `request`                      | `BatchQueryExternalBackgroundCheckRequest` | ✅   | 查询请求体（external_background_check_id_list 最多 20 个，传入时以其为准） |
| `external_application_id`      | `string?`                                | ⚪   | 外部投递 ID，示例值：`6960663240925956660`                    |
| `page_size`                    | `int?`                                   | ⚪   | 每页数量，范围 1~20，默认 10                                  |
| `page_token`                   | `string?`                                | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token       |

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

**说明**：`data.items` 为 `ExternalBackgroundCheck[]`；传入 `external_background_check_id_list` 时其他查询条件失效。

---

### 删除外部背调

按外部背调 ID 删除外部背调。限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteExternalBackgroundCheckAsync(
    [Path] string external_background_check_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                         | 类型     | 必填 | 说明                                    |
| ------------------------------ | -------- | ---- | --------------------------------------- |
| `external_background_check_id` | `string` | ✅   | 外部背调 ID，示例值：`6960663240925956660` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：成功时 `data` 为空对象。

---

### 导入外部内推奖励

将外部系统的内推奖励（积分/现金）导入到招聘的「内推账号」；external_id 为幂等字段。限频：10 次/秒。所需权限：hire:external_referral_reward（导入内推奖励信息）。字段权限：contact:user.employee_id:readonly（取 user_id 时必填）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateExternalReferralRewardResult>?> CreateExternalReferralRewardAsync(
    [Body] CreateExternalReferralRewardRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                                | 必填 | 说明                                                                                                              |
| -------------- | ----------------------------------- | ---- | ----------------------------------------------------------------------------------------------------------------- |
| `request`      | `CreateExternalReferralRewardRequest` | ✅   | 导入请求体（referral_user_id、external_id、rule_type、bonus、stage 必填；application_id 与 talent_id 二选一，其余选填） |
| `user_id_type` | `string?`                          | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限  |

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

**说明**：`data.id` 为创建的内推奖励 ID；`external_id` 为幂等字段，`application_id` 与 `talent_id` 二选一。

---

### 删除外部内推奖励

按 ID 删除导入的外部内推奖励，删除后招聘系统「内推奖励管理」中的对应明细会消失；删除「已确认/已发放」奖励前请先与相关内推人沟通。限频：10 次/秒。所需权限：hire:external_referral_reward（导入内推奖励信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteExternalReferralRewardAsync(
    [Path] string external_referral_reward_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                         | 类型     | 必填 | 说明                                      |
| ------------------------------ | -------- | ---- | ----------------------------------------- |
| `external_referral_reward_id`  | `string` | ✅   | 内推奖励 ID，示例值：`6930815272790114324` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：删除后「内推奖励管理」中的对应明细消失；删除「已确认/已发放」奖励前请先与内推人沟通。
