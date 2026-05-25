# 多维表格高级权限 - 用户令牌（FeishuUserV2BitableRole）

## 接口名称

**多维表格高级权限（用户令牌）** -（`IFeishuUserV2BitableRole`）

## 功能描述

提供以用户身份管理飞书多维表格高级权限的能力。飞书多维表格高级权限允许用户针对单一数据表设置哪些用户可以查看、编辑指定的行，或是设置针对某用户可以编辑的列。高级权限接口分为自定义角色和协作者两部分，多维表格的所有者或者有可管理权限的用户可通过接口设置高级权限，管理高级权限的协作者。

## 参考文档

- [高级权限指南 - 飞书开放平台](https://open.feishu.cn/document/server-docs/docs/bitable-v1/advanced-permission/advanced-permission-guide)

## 函数列表

### 自定义角色管理

| 函数名称               | 功能描述       | 认证方式 | HTTP 方法 |
| ---------------------- | -------------- | -------- | --------- |
| CreateRoleAsync        | 新增自定义角色 | 用户令牌 | POST      |
| UpdateRoleAsync        | 更新自定义角色 | 用户令牌 | PUT       |
| GetRolesPageListAsync  | 列出自定义角色 | 用户令牌 | GET       |
| DeleteRoleAsync        | 删除自定义角色 | 用户令牌 | DELETE    |

### 协作者管理

| 函数名称                    | 功能描述       | 认证方式 | HTTP 方法 |
| --------------------------- | -------------- | -------- | --------- |
| AddRoleMemberAsync          | 新增协作者     | 用户令牌 | POST      |
| AddRoleMembersAsync         | 批量新增协作者 | 用户令牌 | POST      |
| GetRoleMembersPageListAsync | 分页列出协作者 | 用户令牌 | GET       |
| DeleteRoleMemberAsync       | 删除协作者     | 用户令牌 | DELETE    |
| DeleteRoleMembersAsync      | 批量删除协作者 | 用户令牌 | DELETE    |

## 函数详细内容

### 新增自定义角色

新增多维表格高级权限中自定义的角色。

**函数签名**：

```csharp
Task<FeishuApiResult<RoleOpsResult>?> CreateRoleAsync(
    [Path] string app_token,
    [Body] CreateRoleRequest createRoleRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名               | 类型                 | 必填 | 说明                                                           |
| -------------------- | -------------------- | ---- | -------------------------------------------------------------- |
| `app_token`          | `string`             | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `createRoleRequest`  | `CreateRoleRequest`  | ✅   | 新增自定义角色请求体                                           |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "role": {
      "role_id": "roljRpwIUt",
      "role_name": "自定义角色"
    }
  }
}
```

**说明**：新增自定义角色时需指定角色名称和角色规则。以用户身份操作时，操作记录将关联到当前用户。

---

### 更新自定义角色

更新多维表格高级权限中自定义的角色。

**函数签名**：

```csharp
Task<FeishuApiResult<RoleOpsResult>?> UpdateRoleAsync(
    [Path] string app_token,
    [Path] string role_id,
    [Body] UpdateRoleRequest updateRoleRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名               | 类型                 | 必填 | 说明                                                                                     |
| -------------------- | -------------------- | ---- | ---------------------------------------------------------------------------------------- |
| `app_token`          | `string`             | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                           |
| `role_id`            | `string`             | ✅   | 自定义角色的唯一标识，以 rol 开头，示例值：`roljRpwIUt`                                  |
| `updateRoleRequest`  | `UpdateRoleRequest`  | ✅   | 更新自定义角色请求体                                                                     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "role": {
      "role_id": "roljRpwIUt",
      "role_name": "更新后的角色"
    }
  }
}
```

**说明**：更新自定义角色时为全量更新，请求体中指定的字段值将覆盖原有值。

---

### 列出自定义角色

列出多维表格高级权限中用户自定义的角色。

**函数签名**：

```csharp
Task<FeishuApiPageListTotalResult<AppRoleInfo>?> GetRolesPageListAsync(
    [Path] string app_token,
    [Query("page_size")] int page_size = 20,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名        | 类型      | 必填 | 说明                                                                                     |
| ------------- | --------- | ---- | ---------------------------------------------------------------------------------------- |
| `app_token`   | `string`  | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                           |
| `page_size`   | `int`     | ⚪   | 分页大小，默认值：20                                                                     |
| `page_token`  | `string?` | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "role_id": "roljRpwIUt",
        "role_name": "自定义角色"
      }
    ],
    "page_token": "next_page_token",
    "has_more": false,
    "total": 5
  }
}
```

**说明**：返回多维表格中所有自定义角色的列表，支持分页获取。

---

### 删除自定义角色

删除多维表格高级权限中自定义的角色。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteRoleAsync(
    [Path] string app_token,
    [Path] string role_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名      | 类型     | 必填 | 说明                                                           |
| ----------- | -------- | ---- | -------------------------------------------------------------- |
| `app_token` | `string` | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `role_id`   | `string` | ✅   | 自定义角色的唯一标识，以 rol 开头，示例值：`roljRpwIUt`        |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除自定义角色后，该角色下的所有协作者权限将被移除，请谨慎操作。

---

### 新增协作者

新增多维表格高级权限中自定义角色的协作者。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> AddRoleMemberAsync(
    [Path] string app_token,
    [Path] string role_id,
    [Body] AddRoleMemberRequest addRoleMemberRequest,
    [Query("member_id_type")] string member_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                | 类型                   | 必填 | 说明                                                                                                                                                                    |
| --------------------- | ---------------------- | ---- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `app_token`           | `string`               | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                                                                                                          |
| `role_id`             | `string`               | ✅   | 自定义角色的唯一标识，以 rol 开头，示例值：`roljRpwIUt`                                                                                                                  |
| `addRoleMemberRequest`| `AddRoleMemberRequest` | ✅   | 新增协作者请求体                                                                                                                                                        |
| `member_id_type`      | `string`               | ⚪   | 协作者 ID 的类型，可选值：`open_id`、`union_id`、`user_id`、`chat_id`、`department_id`、`open_department_id`，默认值：`open_id`                                           |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：新增协作者时需指定协作者的 ID 和类型。`member_id_type` 决定了如何识别协作者，支持用户、群组和部门等多种类型。

---

### 批量新增协作者

批量新增多维表格高级权限中自定义角色的协作者。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> AddRoleMembersAsync(
    [Path] string app_token,
    [Path] string role_id,
    [Body] AddRoleMembersRequest addRoleMemberRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                 | 类型                    | 必填 | 说明                                                           |
| ---------------------- | ----------------------- | ---- | -------------------------------------------------------------- |
| `app_token`            | `string`                | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `role_id`              | `string`                | ✅   | 自定义角色的唯一标识，以 rol 开头，示例值：`roljRpwIUt`        |
| `addRoleMemberRequest` | `AddRoleMembersRequest` | ✅   | 批量新增协作者请求体                                           |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：批量新增协作者时，请求体中可包含多个协作者信息。

---

### 分页列出协作者

分页列出多维表格高级权限中自定义角色的协作者。

**函数签名**：

```csharp
Task<FeishuApiPageListResult<AppRoleMember>?> GetRoleMembersPageListAsync(
    [Path] string app_token,
    [Path] string role_id,
    [Query("page_size")] int page_size = 20,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名        | 类型      | 必填 | 说明                                                                                     |
| ------------- | --------- | ---- | ---------------------------------------------------------------------------------------- |
| `app_token`   | `string`  | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                           |
| `role_id`     | `string`  | ✅   | 自定义角色的唯一标识，以 rol 开头，示例值：`roljRpwIUt`                                  |
| `page_size`   | `int`     | ⚪   | 分页大小，默认值：20                                                                     |
| `page_token`  | `string?` | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "member_id": "ou_7dab8a3d3cdcc9da365777c7ad53uew2",
        "member_name": "张三",
        "member_type": "user"
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**：返回指定角色下的所有协作者列表，支持分页获取。

---

### 删除协作者

删除多维表格高级权限中自定义角色的协作者。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteRoleMemberAsync(
    [Path] string app_token,
    [Path] string role_id,
    [Path] string member_id,
    [Query("member_id_type")] string member_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名           | 类型     | 必填 | 说明                                                                                                                                                                    |
| ---------------- | -------- | ---- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `app_token`      | `string` | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                                                                                                          |
| `role_id`        | `string` | ✅   | 自定义角色的唯一标识，以 rol 开头，示例值：`roljRpwIUt`                                                                                                                  |
| `member_id`      | `string` | ✅   | 协作者的 ID，需与 member_id_type 的类型一致，示例值：`ou_7dab8a3d3cdcc9da365777c7ad53uew2`                                                                               |
| `member_id_type` | `string` | ⚪   | 协作者 ID 的类型，可选值：`open_id`、`union_id`、`user_id`、`chat_id`、`department_id`、`open_department_id`，默认值：`open_id`                                           |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除协作者后，该协作者将失去该角色对应的权限。

---

### 批量删除协作者

批量删除多维表格高级权限中自定义角色的协作者。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteRoleMembersAsync(
    [Path] string app_token,
    [Path] string role_id,
    [Body] DeleteRoleMembersRequest deleteRoleMembersRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                     | 类型                       | 必填 | 说明                                                           |
| -------------------------- | -------------------------- | ---- | -------------------------------------------------------------- |
| `app_token`                | `string`                   | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `role_id`                  | `string`                   | ✅   | 自定义角色的唯一标识，以 rol 开头，示例值：`roljRpwIUt`        |
| `deleteRoleMembersRequest` | `DeleteRoleMembersRequest` | ✅   | 批量删除协作者请求体                                           |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：批量删除协作者后，这些协作者将失去该角色对应的权限，请谨慎操作。
