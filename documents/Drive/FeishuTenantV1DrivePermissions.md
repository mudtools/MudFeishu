# 云文档权限 - 租户令牌（FeishuTenantV1DrivePermissions）

## 接口名称

**云文档权限（租户权限）** -（`IFeishuTenantV1DrivePermissions`）

## 功能描述

提供以租户身份管理飞书云文档权限的能力。权限是指在云文档相关资源中，应用或用户对各类云文档资源，如文件夹、文档、电子表格、多维表格、知识库等的可阅读、可编辑、可管理等权限。支持增加/批量增加/更新/移除协作者权限、获取协作者列表、转移所有者、判断用户权限、更新/获取权限设置、启用/刷新/停用文档密码等操作。适用于需要以应用身份管理云文档权限的业务场景，如自动化权限分配、权限审计等。

## 参考文档

- [权限概述 - 飞书开放平台](https://open.feishu.cn/document/server-docs/docs/permission/overview)

## 函数列表

| 函数名称                            | 功能描述             | 认证方式 | HTTP 方法 |
| ----------------------------------- | -------------------- | -------- | --------- |
| CreatePermissionMemberAsync         | 增加协作者权限       | 租户令牌 | POST      |
| BatchCreatePermissionMemberAsync    | 批量增加协作者权限   | 租户令牌 | POST      |
| UpdatePermissionMemberAsync         | 更新协作者权限       | 租户令牌 | PUT       |
| GetPermissionMemberAsync            | 获取云文档协作者     | 租户令牌 | GET       |
| DeletePermissionMemberAsync         | 移除云文档协作者权限 | 租户令牌 | DELETE    |
| TransferOwnerPermissionMemberAsync  | 转移云文档所有者     | 租户令牌 | POST      |
| GetAuthPermissionMemberAsync        | 判断用户云文档权限   | 租户令牌 | GET       |
| UpdatePermissionPublicAsync         | 更新云文档权限设置   | 租户令牌 | PATCH     |
| GetPermissionPublicAsync            | 获取云文档权限设置   | 租户令牌 | GET       |
| CreatePermissionPublicPasswordAsync | 启用云文档密码       | 租户令牌 | POST      |
| UpdatePermissionPublicPasswordAsync | 刷新云文档密码       | 租户令牌 | PUT       |
| DeletePermissionPublicPasswordAsync | 停用云文档密码       | 租户令牌 | DELETE    |

## 函数详细内容

### 增加协作者权限

**函数签名**：

```csharp
Task<FeishuApiResult<PermissionMemberOopsResult>?> CreatePermissionMemberAsync(
    [Path] string token,
    [Query("type")] string type,
    [Body] CreatePermissionMemberRequest createPermissionMemberRequest,
    [Query("need_notification")] bool? need_notification = false,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                          | 类型                            | 必填 | 说明                                                                                                                             |
| ------------------------------- | ------------------------------- | ---- | -------------------------------------------------------------------------------------------------------------------------------- |
| `token`                         | `string`                        | ✅   | 云文档的 token，示例值：`doccnBKgoMyY5OMbUG6FioTXuBe`                                                                            |
| `type`                          | `string`                        | ✅   | 云文档类型，可选值：`doc`、`sheet`、`file`、`wiki`、`bitable`、`docx`、`folder`、`mindnote`、`minutes`、`slides`，示例值：`docx` |
| `createPermissionMemberRequest` | `CreatePermissionMemberRequest` | ✅   | 增加协作者权限请求体                                                                                                             |
| `need_notification`             | `bool?`                         | ⚪   | 添加权限后是否通知对方，默认值：`false`。仅当使用用户令牌调用时有效                                                              |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "member_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
    "perm": "full_access"
  }
}
```

**说明**：为指定云文档添加协作者，协作者可以是用户、群组、部门、用户组等。

---

### 批量增加协作者权限

**函数签名**：

```csharp
Task<FeishuApiResult<BatchCreatePermissionMemberResult>?> BatchCreatePermissionMemberAsync(
    [Path] string token,
    [Query("type")] string type,
    [Body] BatchCreatePermissionMemberRequest batchCreatePermissionMemberRequest,
    [Query("need_notification")] bool? need_notification = false,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                               | 类型                                 | 必填 | 说明                                                                                                                             |
| ------------------------------------ | ------------------------------------ | ---- | -------------------------------------------------------------------------------------------------------------------------------- |
| `token`                              | `string`                             | ✅   | 云文档的 token，示例值：`doccnBKgoMyY5OMbUG6FioTXuBe`                                                                            |
| `type`                               | `string`                             | ✅   | 云文档类型，可选值：`doc`、`sheet`、`file`、`wiki`、`bitable`、`docx`、`folder`、`mindnote`、`minutes`、`slides`，示例值：`docx` |
| `batchCreatePermissionMemberRequest` | `BatchCreatePermissionMemberRequest` | ✅   | 批量增加协作者权限请求体                                                                                                         |
| `need_notification`                  | `bool?`                              | ⚪   | 添加权限后是否通知对方，默认值：`false`。仅当使用用户令牌调用时有效                                                              |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "members": [
      {
        "member_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
        "perm": "full_access"
      }
    ]
  }
}
```

**说明**：为指定云文档批量添加多个协作者，协作者可以是用户、群组、部门、用户组等。

---

### 更新协作者权限

**函数签名**：

```csharp
Task<FeishuApiResult<PermissionMemberOopsResult>?> UpdatePermissionMemberAsync(
    [Path] string token,
    [Path] string member_id,
    [Query("type")] string type,
    [Body] UpdatePermissionMemberRequest updatePermissionMemberRequest,
    [Query("need_notification")] bool? need_notification = false,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                          | 类型                            | 必填 | 说明                                                                                                                             |
| ------------------------------- | ------------------------------- | ---- | -------------------------------------------------------------------------------------------------------------------------------- |
| `token`                         | `string`                        | ✅   | 云文档的 token，示例值：`doccnBKgoMyY5OMbUG6FioTXuBe`                                                                            |
| `member_id`                     | `string`                        | ✅   | 协作者 ID，示例值：`ou_7dab8a3d3cdcc9da365777c7ad535d62`                                                                         |
| `type`                          | `string`                        | ✅   | 云文档类型，可选值：`doc`、`sheet`、`file`、`wiki`、`bitable`、`docx`、`folder`、`mindnote`、`minutes`、`slides`，示例值：`docx` |
| `updatePermissionMemberRequest` | `UpdatePermissionMemberRequest` | ✅   | 更新协作者权限成员请求模型                                                                                                       |
| `need_notification`             | `bool?`                         | ⚪   | 添加权限后是否通知对方，默认值：`false`。仅当使用用户令牌调用时有效                                                              |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "member_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
    "perm": "edit"
  }
}
```

**说明**：更新指定云文档协作者的权限。

---

### 获取云文档协作者

**函数签名**：

```csharp
Task<FeishuApiResult<GetPermissionMemberResult>?> GetPermissionMemberAsync(
    [Path] string token,
    [Query("type")] string type,
    [Query("fields")] string? fields = null,
    [Query("perm_type")] string? perm_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名      | 类型      | 必填 | 说明                                                                                                                                   |
| ----------- | --------- | ---- | -------------------------------------------------------------------------------------------------------------------------------------- |
| `token`     | `string`  | ✅   | 云文档的 token，示例值：`doccnBKgoMyY5OMbUG6FioTXuBe`                                                                                  |
| `type`      | `string`  | ✅   | 云文档类型，可选值：`doc`、`sheet`、`file`、`wiki`、`bitable`、`docx`、`folder`、`mindnote`、`minutes`、`slides`，示例值：`docx`       |
| `fields`    | `string?` | ⚪   | 指定返回的协作者字段信息，支持 `name`、`type`、`avatar`、`external_label`，可使用 `*` 返回所有字段，使用 `,` 分隔多个字段，示例值：`*` |
| `perm_type` | `string?` | ⚪   | 协作者的权限角色类型，仅知识库文档有效。可选值：`container`（当前页面及子页面）、`single_page`（仅当前页面），示例值：`container`      |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "members": [
      {
        "member_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
        "name": "张三",
        "perm": "full_access",
        "type": "user"
      }
    ]
  }
}
```

**说明**：获取指定云文档的协作者，支持查询人、群、组织架构、用户组、知识库成员五种类型的协作者。

---

### 移除云文档协作者权限

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeletePermissionMemberAsync(
    [Path] string token,
    [Path] string member_id,
    [Query("type")] string type,
    [Body] DeletePermissionMemberRequest deletePermissionMemberRequest,
    [Query("member_type")] string member_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                          | 类型                            | 必填 | 说明                                                                                                                                       |
| ------------------------------- | ------------------------------- | ---- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| `token`                         | `string`                        | ✅   | 云文档的 token，示例值：`doccnBKgoMyY5OMbUG6FioTXuBe`                                                                                      |
| `member_id`                     | `string`                        | ✅   | 协作者 ID，示例值：`ou_7dab8a3d3cdcc9da365777c7ad535d62`                                                                                   |
| `type`                          | `string`                        | ✅   | 云文档类型，可选值：`doc`、`sheet`、`file`、`wiki`、`bitable`、`docx`、`folder`、`mindnote`、`minutes`、`slides`，示例值：`docx`           |
| `deletePermissionMemberRequest` | `DeletePermissionMemberRequest` | ✅   | 删除云文档协作者请求体                                                                                                                     |
| `member_type`                   | `string`                        | ⚪   | 协作者 ID 类型，可选值：`email`、`openid`、`openchat`、`opendepartmentid`、`userid`、`unionid`、`groupid`、`wikispaceid`，示例值：`openid` |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：通过云文档 token 和协作者 ID 移除指定云文档协作者的权限。

---

### 转移云文档所有者

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> TransferOwnerPermissionMemberAsync(
    [Path] string token,
    [Query("type")] string type,
    [Body] TransferOwnerPermissionMemberRequest transferOwnerPermissionMemberRequest,
    [Query("need_notification")] bool? need_notification = false,
    [Query("remove_old_owner")] bool? remove_old_owner = false,
    [Query("stay_put")] bool? stay_put = false,
    [Query("old_owner_perm")] string? old_owner_perm = "full_access",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                                 | 类型                                   | 必填 | 说明                                                                                                                             |
| -------------------------------------- | -------------------------------------- | ---- | -------------------------------------------------------------------------------------------------------------------------------- |
| `token`                                | `string`                               | ✅   | 云文档的 token，示例值：`doccnBKgoMyY5OMbUG6FioTXuBe`                                                                            |
| `type`                                 | `string`                               | ✅   | 云文档类型，可选值：`doc`、`sheet`、`file`、`wiki`、`bitable`、`docx`、`folder`、`mindnote`、`minutes`、`slides`，示例值：`docx` |
| `transferOwnerPermissionMemberRequest` | `TransferOwnerPermissionMemberRequest` | ✅   | 转移云文档所有者请求体                                                                                                           |
| `need_notification`                    | `bool?`                                | ⚪   | 转移后是否通知对方，默认值：`false`                                                                                              |
| `remove_old_owner`                     | `bool?`                                | ⚪   | 转移后是否移除原所有者权限，默认值：`false`                                                                                      |
| `stay_put`                             | `bool?`                                | ⚪   | 个人文件夹下的云文档是否留在原位置，默认值：`false`。仅当云文档在个人文件夹下时参数生效                                          |
| `old_owner_perm`                       | `string?`                              | ⚪   | 为原所有者保留的权限，可选值：`view`、`edit`、`full_access`，默认值：`full_access`。仅当 `remove_old_owner` 为 `false` 时生效    |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：转移指定云文档的所有者。

---

### 判断用户云文档权限

**函数签名**：

```csharp
Task<FeishuApiResult<GetAuthPermissionMemberResult>?> GetAuthPermissionMemberAsync(
    [Path] string token,
    [Query("type")] string type,
    [Query("action")] string action,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名   | 类型     | 必填 | 说明                                                                                                                                                                                           |
| -------- | -------- | ---- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `token`  | `string` | ✅   | 云文档的 token，示例值：`doccnBKgoMyY5OMbUG6FioTXuBe`                                                                                                                                          |
| `type`   | `string` | ✅   | 云文档类型，可选值：`doc`、`sheet`、`file`、`wiki`、`bitable`、`docx`、`folder`、`mindnote`、`minutes`、`slides`，示例值：`docx`                                                               |
| `action` | `string` | ✅   | 需要判断的权限，可选值：`view`（阅读）、`edit`（编辑）、`share`（分享）、`comment`（评论）、`export`（导出）、`copy`（拷贝）、`print`（打印）、`manage_public`（管理权限设置），示例值：`view` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "is_allowed": true
  }
}
```

**说明**：判断当前请求的应用或用户是否具有指定云文档的指定权限。

---

### 更新云文档权限设置

**函数签名**：

```csharp
Task<FeishuApiResult<PermissionPublicResult>?> UpdatePermissionPublicAsync(
    [Path] string token,
    [Query("type")] string type,
    [Body] UpdateDrivePermissionsRequest updateDrivePermissionsRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                          | 类型                            | 必填 | 说明                                                                                                                             |
| ------------------------------- | ------------------------------- | ---- | -------------------------------------------------------------------------------------------------------------------------------- |
| `token`                         | `string`                        | ✅   | 云文档的 token，示例值：`doccnBKgoMyY5OMbUG6FioTXuBe`                                                                            |
| `type`                          | `string`                        | ✅   | 云文档类型，可选值：`doc`、`sheet`、`file`、`wiki`、`bitable`、`docx`、`folder`、`mindnote`、`minutes`、`slides`，示例值：`docx` |
| `updateDrivePermissionsRequest` | `UpdateDrivePermissionsRequest` | ✅   | 更新云文档权限设置的请求体                                                                                                       |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_access_entity": "open",
    "security_entity": "anyone_can_view",
    "comment_entity": "anyone_can_view",
    "share_entity": "anyone",
    "link_share_entity": "anyone_readable",
    "invite_external": false,
    "manage_collaborator_entity": "collaborator_full_access"
  }
}
```

**说明**：更新指定云文档的权限设置，包括是否允许内容被分享到组织外、谁可以查看、添加、移除协作者、谁可以复制内容等设置。

---

### 获取云文档权限设置

**函数签名**：

```csharp
Task<FeishuApiResult<PermissionPublicResult>?> GetPermissionPublicAsync(
    [Path] string token,
    [Query("type")] string type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型     | 必填 | 说明                                                                                                                             |
| ------- | -------- | ---- | -------------------------------------------------------------------------------------------------------------------------------- |
| `token` | `string` | ✅   | 云文档的 token，示例值：`doccnBKgoMyY5OMbUG6FioTXuBe`                                                                            |
| `type`  | `string` | ✅   | 云文档类型，可选值：`doc`、`sheet`、`file`、`wiki`、`bitable`、`docx`、`folder`、`mindnote`、`minutes`、`slides`，示例值：`docx` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_access_entity": "open",
    "security_entity": "anyone_can_view",
    "comment_entity": "anyone_can_view",
    "share_entity": "anyone",
    "link_share_entity": "anyone_readable",
    "invite_external": false,
    "manage_collaborator_entity": "collaborator_full_access"
  }
}
```

**说明**：获取指定云文档的权限设置，包括是否允许内容被分享到组织外、谁可以查看、添加、移除协作者、谁可以复制内容等设置。

---

### 启用云文档密码

**函数签名**：

```csharp
Task<FeishuApiResult<PermissionsPasswordResult>?> CreatePermissionPublicPasswordAsync(
    [Path] string token,
    [Query("type")] string type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型     | 必填 | 说明                                                                                                                             |
| ------- | -------- | ---- | -------------------------------------------------------------------------------------------------------------------------------- |
| `token` | `string` | ✅   | 云文档的 token，示例值：`doccnBKgoMyY5OMbUG6FioTXuBe`                                                                            |
| `type`  | `string` | ✅   | 云文档类型，可选值：`doc`、`sheet`、`file`、`wiki`、`bitable`、`docx`、`folder`、`mindnote`、`minutes`、`slides`，示例值：`docx` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "password": "abcd1234"
  }
}
```

**说明**：启用指定云文档的密码。密码启用后，组织外用户需要密码访问，组织内用户无需密码可直接访问。

---

### 刷新云文档密码

**函数签名**：

```csharp
Task<FeishuApiResult<PermissionsPasswordResult>?> UpdatePermissionPublicPasswordAsync(
    [Path] string token,
    [Query("type")] string type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型     | 必填 | 说明                                                                                                                             |
| ------- | -------- | ---- | -------------------------------------------------------------------------------------------------------------------------------- |
| `token` | `string` | ✅   | 云文档的 token，示例值：`doccnBKgoMyY5OMbUG6FioTXuBe`                                                                            |
| `type`  | `string` | ✅   | 云文档类型，可选值：`doc`、`sheet`、`file`、`wiki`、`bitable`、`docx`、`folder`、`mindnote`、`minutes`、`slides`，示例值：`docx` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "password": "efgh5678"
  }
}
```

**说明**：刷新指定云文档的密码。密码刷新后，旧密码将失效，并生成新密码。

---

### 停用云文档密码

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeletePermissionPublicPasswordAsync(
    [Path] string token,
    [Query("type")] string type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型     | 必填 | 说明                                                                                                                             |
| ------- | -------- | ---- | -------------------------------------------------------------------------------------------------------------------------------- |
| `token` | `string` | ✅   | 云文档的 token，示例值：`doccnBKgoMyY5OMbUG6FioTXuBe`                                                                            |
| `type`  | `string` | ✅   | 云文档类型，可选值：`doc`、`sheet`、`file`、`wiki`、`bitable`、`docx`、`folder`、`mindnote`、`minutes`、`slides`，示例值：`docx` |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：停用指定云文档的密码。密码停用后，组织外用户访问文档将无需输入密码。
