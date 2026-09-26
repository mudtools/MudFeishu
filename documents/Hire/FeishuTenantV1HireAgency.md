# 猎头供应商 - 租户令牌（FeishuTenantV1HireAgency）

## 接口名称

**猎头供应商（租户令牌）** -（`IFeishuTenantV1HireAgency`）

## 功能描述

提供以租户身份管理飞书招聘猎头供应商的能力。飞书招聘（Hire）猎头供应商入口域 SDK 是一组服务端 OpenAPI 的封装，用于猎头供应商查询（按 ID/名称/条件搜索）、猎头供应商下猎头账号的查询与禁用/取消禁用，以及人才猎头保护期的设置与查询。本接口全部端点仅支持 tenant_access_token 调用。支持搜索猎头供应商列表、获取猎头供应商信息、按名称查询猎头供应商、查询猎头供应商下猎头列表、禁用/取消禁用猎头、设置猎头保护期、查询猎头保护期信息等操作。

## 参考文档

- [招聘开发指南 - 飞书开放平台](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-development-guide)

## 函数列表

| 函数名称                     | 功能描述                 | 认证方式 | HTTP 方法 |
| ---------------------------- | ------------------------ | -------- | --------- |
| BatchQueryAgencyAsync        | 搜索猎头供应商列表       | 租户令牌 | POST      |
| GetAgencyAsync               | 获取猎头供应商信息       | 租户令牌 | GET       |
| QueryAgencyAsync             | 按名称查询猎头供应商     | 租户令牌 | GET       |
| GetAgencyAccountAsync        | 查询猎头供应商下猎头列表 | 租户令牌 | POST      |
| OperateAgencyAccountAsync    | 禁用/取消禁用猎头        | 租户令牌 | POST      |
| ProtectAgencyAsync           | 设置猎头保护期           | 租户令牌 | POST      |
| SearchAgencyProtectionAsync  | 查询猎头保护期信息       | 租户令牌 | POST      |

## 函数详细内容

### 搜索猎头供应商列表

按猎头供应商 ID 列表或关键字、筛选项查询供应商信息，传 agency_supplier_id_list 时以其为准、其余查询字段失效；暂不支持查询「邀请中」的供应商。限频：10 次/秒。所需权限：hire:agency:readonly（获取猎头供应商信息）或 hire:agency（更新猎头供应商信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）、hire:agency.email:readonly（管理员邮箱）。

**函数签名**：

```csharp
Task<FeishuApiResult<BatchQueryAgencyResult>?> BatchQueryAgencyAsync(
    [Body] BatchQueryAgencyRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("page_token")] string? page_token = null,
    [Query("page_size")] int? page_size = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名          | 类型                      | 必填 | 说明                                                                                                                           |
| --------------- | ------------------------- | ---- | ------------------------------------------------------------------------------------------------------------------------------ |
| `request`       | `BatchQueryAgencyRequest` | ✅   | 搜索请求体（agency_supplier_id_list 最多 20 个；keyword 可传名称或邮箱；filter_list 支持 cooperation_create_time 范围及 cooperation_status/supplier_area/label_id_list 值筛选） |
| `user_id_type`  | `string?`                 | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限               |
| `page_token`    | `string?`                 | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                                                        |
| `page_size`     | `int?`                    | ⚪   | 每页数量，最大 20，默认 10                                                                                                     |

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

**说明**：`data.items` 为 `AgencySupplier[]` 猎头供应商分页列表，翻页时把上一次返回的 `page_token` 传入继续拉取。

---

### 获取猎头供应商信息

按猎头供应商 ID 获取猎头供应商信息，返回名称与供应商联系人。限频：10 次/秒。所需权限：hire:agency:readonly（获取猎头供应商信息）或 hire:agency（更新猎头供应商信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetAgencyResult>?> GetAgencyAsync(
    [Path] string agency_id,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                                    |
| -------------- | --------- | ---- | ------------------------------------------------------------------------------------------------------- |
| `agency_id`    | `string`  | ✅   | 猎头供应商 ID，示例值：`6898173495386147079`                                                            |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "agency": {}
  }
}
```

**说明**：`data.agency` 为 `Agency` 猎头供应商信息，含名称与供应商联系人。

---

### 按名称查询猎头供应商

按猎头供应商名称精准匹配查询（区分大小写）。限频：10 次/秒。所需权限：hire:agency:readonly（获取猎头供应商信息）或 hire:agency（更新猎头供应商信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryAgencyResult>?> QueryAgencyAsync(
    [Query("name")] string name,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                         |
| -------------- | --------- | ---- | -------------------------------------------------------------------------------------------- |
| `name`         | `string`  | ✅   | 猎头供应商名称，精准匹配（区分大小写），示例值：`超越猎头公司`                                |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

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

**说明**：`data.items` 为 `Agency[]` 猎头供应商列表，名称需完全匹配且区分大小写。

---

### 查询猎头供应商下猎头列表

按猎头供应商 ID 分页查询其下的猎头账号，可按猎头状态与角色过滤。限频：10 次/秒。所需权限：hire:agency_account:readonly（查询猎头供应商下猎头信息）或 hire:agency_account（更新猎头供应商下猎头信息）。字段权限：hire:agency.email:readonly（用户邮箱）、hire:agency.mobile:readonly（用户手机号）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetAgencyAccountResult>?> GetAgencyAccountAsync(
    [Body] GetAgencyAccountRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    [Query("page_token")] string? page_token = null,
    [Query("page_size")] int? page_size = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                     | 必填 | 说明                                                                                           |
| -------------- | ------------------------ | ---- | ---------------------------------------------------------------------------------------------- |
| `request`      | `GetAgencyAccountRequest` | ✅   | 查询请求体（supplier_id 必填；status 猎头状态 0 正常/1 已禁用/2 自助停用；role 角色 0 管理员/1 顾问） |
| `user_id_type` | `string?`                | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id                                         |
| `page_token`   | `string?`                | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                        |
| `page_size`    | `int?`                   | ⚪   | 每页数量，最大 20，默认 10                                                                     |

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

**说明**：`data.items` 为 `AgencyAccount[]` 猎头账号分页列表，可按 `status` 与 `role` 过滤。

---

### 禁用/取消禁用猎头

按猎头 ID 对猎头执行禁用或取消禁用；被禁用的猎头不能推荐候选人与被分配职位。限频：10 次/秒。所需权限：hire:agency_account（更新猎头供应商下猎头信息）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> OperateAgencyAccountAsync(
    [Body] OperateAgencyAccountRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                          | 必填 | 说明                                                                                    |
| --------- | ----------------------------- | ---- | --------------------------------------------------------------------------------------- |
| `request` | `OperateAgencyAccountRequest` | ✅   | 操作请求体（option 必填：1 禁用/2 取消禁用；id 必填：猎头 ID；reason 禁用原因，option 为 1 时必填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：成功时 `data` 为空对象；`option` 为 1（禁用）时必须填写 `reason`。

---

### 设置猎头保护期

设置指定人才的猎头保护期；当「飞书招聘」内置的保护期功能不满足需求时，可通过此接口自定义人才的保护期。限频：1000 次/分钟、50 次/秒。所需权限：hire:agency（更新猎头供应商信息）。字段权限：contact:user.employee_id:readonly（取 user_id 时必填）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> ProtectAgencyAsync(
    [Body] ProtectAgencyRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                  | 必填 | 说明                                                                                                             |
| -------------- | --------------------- | ---- | ---------------------------------------------------------------------------------------------------------------- |
| `request`      | `ProtectAgencyRequest` | ✅   | 设置请求体（talent_id、supplier_id、consultant_id、protect_create_time、protect_expire_time 必填；comment、current_salary、expected_salary 选填） |
| `user_id_type` | `string?`             | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：成功时 `data` 为空对象；用于自定义人才在猎头供应商下的保护期起止时间。

---

### 查询猎头保护期信息

查询指定人才的猎头保护期信息列表，包含保护期起止时间、猎头供应商与猎头顾问信息；若人才已入职，还会返回入职时所在的保护期信息。限频：10 次/秒。所需权限：hire:agency:readonly（获取猎头供应商信息）或 hire:agency（更新猎头供应商信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<SearchAgencyProtectionResult>?> SearchAgencyProtectionAsync(
    [Body] SearchAgencyProtectionRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                            | 必填 | 说明                                        |
| --------- | ------------------------------- | ---- | ------------------------------------------- |
| `request` | `SearchAgencyProtectionRequest` | ✅   | 查询请求体（talent_id 必填：人才 ID）       |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "is_onboarded": false,
    "onboarded_in_protection": false,
    "onboarded_protection": {},
    "protection_list": []
  }
}
```

**说明**：`data.protection_list` 为 `AgencyProtection[]` 保护期列表；`onboarded_protection` 仅在人才已入职且处于保护期时返回。
