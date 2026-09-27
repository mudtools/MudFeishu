# 招聘设置与字典 - 租户令牌（FeishuTenantV1HireSetting）

## 接口名称

**招聘设置与字典（租户令牌）** -（`IFeishuTenantV1HireSetting`）

## 功能描述

提供以租户身份查询飞书招聘基础配置与字典数据的能力。飞书招聘（Hire）设置与字典域 SDK 是一组服务端 OpenAPI 的封装，用于招聘流程、科目、信息登记表模板、人才标签、地点、角色与用户角色等基础配置/字典资源的查询。本接口全部端点仅支持 tenant_access_token 调用。支持获取招聘流程列表、获取科目列表、获取信息登记表模板列表、获取人才标签列表、查询地点列表、获取地点列表、获取角色信息、获取角色列表、获取用户角色列表等操作。

## 参考文档

- [招聘开发指南 - 飞书开放平台](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-development-guide)

## 函数列表

| 函数名称                          | 功能描述                 | 认证方式 | HTTP 方法 |
| --------------------------------- | ------------------------ | -------- | --------- |
| GetJobProcessListAsync            | 获取招聘流程列表         | 租户令牌 | GET       |
| GetSubjectListAsync               | 获取科目列表             | 租户令牌 | GET       |
| GetRegistrationSchemaListAsync    | 获取信息登记表模板列表   | 租户令牌 | GET       |
| GetTalentTagListAsync             | 获取人才标签列表         | 租户令牌 | GET       |
| QueryLocationAsync                | 查询地点列表             | 租户令牌 | POST      |
| GetLocationListAsync              | 获取地点列表             | 租户令牌 | GET       |
| GetRoleAsync                      | 获取角色信息             | 租户令牌 | GET       |
| GetRoleListAsync                  | 获取角色列表             | 租户令牌 | GET       |
| GetUserRoleListAsync              | 获取用户角色列表         | 租户令牌 | GET       |

## 函数详细内容

### 获取招聘流程列表

分页获取全部招聘流程信息，包括流程名称、流程类型以及各阶段的名称与类型。限频：20 次/秒。所需权限：hire:job_process:readonly（获取招聘流程信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetJobProcessListResult>?> GetJobProcessListAsync(
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
    "has_more": false,
    "page_token": "",
    "items": []
  }
}
```

**说明**：`data.items` 为 `JobProcess[]`，含流程名称、流程类型及各阶段的名称与类型；翻页时传入上一次返回的 `page_token`。

---

### 获取科目列表

分页获取招聘科目列表，返回科目名称、启用状态与创建人等信息。限频：特殊限频。所需权限：hire:subject:readonly（获取招聘项目信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetSubjectListResult>?> GetSubjectListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                    |
| -------------- | --------- | ---- | --------------------------------------------------------------------------------------- |
| `page_size`    | `int?`    | ⚪   | 每页数量，最大 200，默认 1                                                              |
| `page_token`   | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                 |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

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

**说明**：`data.items` 为 `Subject[]`，含科目名称、启用状态与创建人信息。

---

### 获取信息登记表模板列表

分页获取信息登记表模板列表，可按适用场景（面试/入职/信息更新登记表）筛选，返回模板内模块与字段配置。限频：20 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetRegistrationSchemaListResult>?> GetRegistrationSchemaListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("scenario")] int? scenario = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                                       |
| ------------ | --------- | ---- | -------------------------------------------------------------------------- |
| `page_size`  | `int?`    | ⚪   | 每页数量，最大 50，默认 10                                                 |
| `page_token` | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                    |
| `scenario`   | `int?`    | ⚪   | 登记表适用场景：5 面试登记表 / 6 入职登记表 / 14 信息更新登记表；不传表示获取全部类型 |

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

**说明**：`data.items` 为 `RegistrationSchema[]`；`scenario` 不传时返回全部适用场景的模板。

---

### 获取人才标签列表

按关键词、ID 列表、标签类型与是否包含停用等条件分页查询人才标签，按创建时间倒序排列（查询参数采用查询对象模式 `TalentTagListQuery`，见 AGENTS.md API-2）。限频：20 次/秒。所需权限：hire:talent_tag（更新人才标签）或 hire:talent_tag:readonly（获取人才标签）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetTalentTagListResult>?> GetTalentTagListAsync(
    [Query] TalentTagListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型                  | 必填 | 说明                                                                     |
| ------- | --------------------- | ---- | ------------------------------------------------------------------------ |
| `query` | `TalentTagListQuery?` | ⚪   | 关键词、ID 列表、标签类型、启停状态与分页查询参数，该对象会整体展开为查询参数 |

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

**说明**：`data.items` 为 `TalentTag[]`，按创建时间倒序排列；可通过查询对象控制是否包含停用标签。

---

### 查询地点列表

根据地点类型（国家/省份/城市/区县）与地点码批量查询地点信息，获取地点名称（中文、英文、拼音）。限频：5 次/秒。所需权限：hire:location:readonly（获取地点信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryLocationResult>?> QueryLocationAsync(
    [Body] QueryLocationRequest request,
    [Query("page_size")] int page_size,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型                   | 必填 | 说明                                                                                                      |
| ------------ | ---------------------- | ---- | --------------------------------------------------------------------------------------------------------- |
| `request`    | `QueryLocationRequest` | ✅   | 查询请求体（location_type 必填：1 国家 / 2 省份 / 3 城市 / 4 区县；code_list 地点码列表，可选，最大 100 个，不填则查询全部） |
| `page_size`  | `int`                  | ✅   | 每页数量，必填，取值范围 1～100                                                                           |
| `page_token` | `string?`              | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                                   |

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

**说明**：`data.items` 为 `LocationDto[]`，含地点名称（中文、英文、拼音）；`page_size` 为必填查询参数，取值 1～100。

---

### 获取地点列表

获取飞书招聘内置的工作地/面试地地点列表，返回区县、城市、省份、国家的层级编码与名称。限频：特殊限频（详见接口文档）。所需权限：hire:location:readonly（获取地点信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetLocationListResult>?> GetLocationListAsync(
    [Query("usage")] string usage,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                    |
| ------------ | --------- | ---- | ------------------------------------------------------- |
| `usage`      | `string`  | ✅   | 地点用途，必填：position_location（工作地）/ interview_location（面试地） |
| `page_size`  | `int?`    | ⚪   | 每页数量，取值范围 1～100                               |
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

**说明**：`data.items` 为 `Location[]`，含区县、城市、省份、国家的层级编码与名称；`usage` 区分工作地与面试地。

---

### 获取角色信息

按角色 ID 获取角色详情，包括角色名称、描述、适用范围与社招/校招权限配置。限频：10 次/秒。所需权限：hire:auth:readonly（获取权限信息）或 hire:auth（更新权限信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetRoleResult>?> GetRoleAsync(
    [Path] string role_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型     | 必填 | 说明                                                      |
| --------- | -------- | ---- | --------------------------------------------------------- |
| `role_id` | `string` | ✅   | 角色 ID，可通过获取角色列表接口获取，示例值：`7350589232462807068` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "role": {}
  }
}
```

**说明**：`data.role` 为 `RoleDetail`，含角色名称、描述、适用范围与社招/校招权限配置。

---

### 获取角色列表

分页获取企业内飞书招聘角色列表，返回角色名称、描述与适用范围。限频：1000 次/分钟、50 次/秒。所需权限：hire:auth:readonly（获取权限信息）或 hire:auth（更新权限信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetRoleListResult>?> GetRoleListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                    |
| ------------ | --------- | ---- | ------------------------------------------------------- |
| `page_size`  | `int?`    | ⚪   | 每页数量，最大 200                                      |
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

**说明**：`data.items` 为 `Role[]`，含角色名称、描述与适用范围；角色 ID 可用于获取角色信息与用户角色列表。

---

### 获取用户角色列表

按用户、角色或更新时间分页查询用户角色分配关系，返回角色名称与业务管理范围（查询参数采用查询对象模式 `UserRoleListQuery`，见 AGENTS.md API-2）。限频：1000 次/分钟、50 次/秒。所需权限：hire:auth:readonly（获取权限信息）或 hire:auth（更新权限信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetUserRoleListResult>?> GetUserRoleListAsync(
    [Query] UserRoleListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型                 | 必填 | 说明                                                                  |
| ------- | -------------------- | ---- | --------------------------------------------------------------------- |
| `query` | `UserRoleListQuery?` | ⚪   | 分页、用户、角色、更新时间范围与用户 ID 类型查询参数，该对象会整体展开为查询参数 |

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

**说明**：`data.items` 为 `UserRole[]`，含角色名称与业务管理范围；可用于权限审计，按更新时间增量拉取。
