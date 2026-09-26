# Offer 设置 - 租户令牌（FeishuTenantV1HireOffer）

## 接口名称

**Offer 设置（租户令牌）** -（`IFeishuTenantV1HireOffer`）

## 功能描述

提供以租户身份管理飞书招聘 Offer 配置的能力。飞书招聘（Hire）Offer 域 SDK 是一组服务端 OpenAPI 的封装，用于招聘配置中的 Offer 申请表列表与模板 Schema 查询、申请表自定义字段更新、Offer 审批模板查询。本接口全部端点仅支持 tenant_access_token 调用（投递流程中的 Offer 创建/更新/查询/列表/状态变更见 `IFeishuTenantV1HireCandidate`）。支持获取 Offer 申请表列表、获取 Offer 申请表模板信息、更新 Offer 申请表自定义字段、获取 Offer 审批模板列表等操作。

## 参考文档

- [招聘开发指南 - 飞书开放平台](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-development-guide)

## 函数列表

| 函数名称                          | 功能描述                    | 认证方式 | HTTP 方法 |
| --------------------------------- | --------------------------- | -------- | --------- |
| GetOfferApplicationFormListAsync  | 获取 Offer 申请表列表       | 租户令牌 | GET       |
| GetOfferApplicationFormAsync      | 获取 Offer 申请表模板信息   | 租户令牌 | GET       |
| UpdateOfferCustomFieldAsync       | 更新 Offer 申请表自定义字段 | 租户令牌 | PUT       |
| GetOfferApprovalTemplateListAsync | 获取 Offer 审批模板列表     | 租户令牌 | GET       |

## 函数详细内容

### 获取 Offer 申请表列表

分页获取 Offer 申请表列表，返回申请表 ID、名称与创建时间。限频：10 次/秒。所需权限：hire:offer_schema:readonly（获取 Offer 申请表信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetOfferApplicationFormListResult>?> GetOfferApplicationFormListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                    |
| ------------ | --------- | ---- | ------------------------------------------------------- |
| `page_size`  | `int?`    | ⚪   | 每页数量，默认 1                                        |
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

**说明**：`data.items` 为 `OfferApplyForm[]`，含申请表 ID、名称与创建时间；翻页时传入上一次返回的 `page_token`。

---

### 获取 Offer 申请表模板信息

按申请表 ID 获取 Offer 申请表模板的完整 Schema 信息，包括模块、字段、选项、公式与联动显示配置。公式（formula）类型字段由其他字段计算得出，创建 Offer 时无需也不可传入。限频：1000 次/分钟、50 次/秒。所需权限：hire:offer_schema:readonly（获取 Offer 申请表信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetOfferApplicationFormResult>?> GetOfferApplicationFormAsync(
    [Path] string offer_application_form_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                       | 类型     | 必填 | 说明                                     |
| ---------------------------- | -------- | ---- | ---------------------------------------- |
| `offer_application_form_id`  | `string` | ✅   | Offer 申请表 ID，示例值：`237186812432` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "offer_apply_form": {}
  }
}
```

**说明**：`data.offer_apply_form` 为 `OfferApplyFormInfo` 完整 Schema；公式类型字段只读，创建 Offer 时不可传入。

---

### 更新 Offer 申请表自定义字段

更新 Offer 申请表自定义字段的名称与选项配置；不支持修改字段类型，公式类型字段不支持更新。每次更新后所有申请表的 schema_id 会升级为新版本。限频：1000 次/分钟、50 次/秒。所需权限：hire:offer_selection_object（更新 Offer 自定义字段）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UpdateOfferCustomFieldAsync(
    [Path] string offer_custom_field_id,
    [Body] UpdateOfferCustomFieldRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                   | 类型                            | 必填 | 说明                                                                                     |
| ------------------------ | ------------------------------- | ---- | ---------------------------------------------------------------------------------------- |
| `offer_custom_field_id`  | `string`                        | ✅   | Offer 申请表自定义字段 ID，可通过获取 Offer 申请表模板信息接口获取，示例值：`6906755946257615112` |
| `request`                | `UpdateOfferCustomFieldRequest` | ✅   | 更新请求体（name 必填，zh_cn/en_us 至少一个；仅单选/多选字段需传 config.options）        |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：成功时 `data` 为空对象；更新后所有申请表的 `schema_id` 升级为新版本，创建/更新 Offer 时需使用最新版 `schema_id`。

---

### 获取 Offer 审批模板列表

分页获取 Offer 审批模板列表，返回模板名称、创建时间、备注与适用部门。限频：10 次/秒。所需权限：hire:offer_approval_template:readonly（获取 Offer 审批流程配置）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetOfferApprovalTemplateListResult>?> GetOfferApprovalTemplateListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("department_id_type")] string? department_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名               | 类型      | 必填 | 说明                                                                             |
| -------------------- | --------- | ---- | -------------------------------------------------------------------------------- |
| `page_size`          | `int?`    | ⚪   | 每页数量，最大 200，默认 10                                                      |
| `page_token`         | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                          |
| `department_id_type` | `string?` | ⚪   | 部门 ID 类型（open_department_id/department_id/people_admin_department_id），默认 people_admin_department_id |

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

**说明**：`data.items` 为 `OfferApprovalTemplate[]`，含模板名称、创建时间、备注与适用部门；`department_id_type` 决定返回部门 ID 的类型。
