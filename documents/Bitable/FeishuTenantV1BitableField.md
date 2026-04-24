# 多维表格字段 - 租户权限（FeishuTenantV1BitableField）

## 接口名称

**多维表格字段（租户权限）** -（`IFeishuTenantV1BitableField`）

## 功能描述

提供以租户身份管理飞书多维表格字段的能力。字段 field 即多维表格的"列"，多维表格提供丰富的字段类型。每个字段都有唯一标识 field_id，field_id 在一个多维表格内唯一，在全局不一定唯一。支持新增、更新、列出和删除字段，以及创建字段编组等操作。

## 参考文档

- [字段概述 - 飞书开放平台](https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-table-field/guide)

## 函数列表

| 函数名称                    | 功能描述     | 认证方式 | HTTP 方法 |
| --------------------------- | ------------ | -------- | --------- |
| AddFieldAsync               | 新增字段     | 租户令牌 | POST      |
| UpdateFieldAsync            | 更新字段     | 租户令牌 | PUT       |
| QueryRecordsPageListAsync   | 列出字段     | 租户令牌 | GET       |
| DeleteFieldAsync            | 删除字段     | 租户令牌 | DELETE    |
| CreateFieldGroupAsync       | 创建字段编组 | 租户令牌 | POST      |

## 函数详细内容

### 新增字段

在多维表格数据表中新增一个字段。

**函数签名**：

```csharp
Task<FeishuApiResult<FieldOpsResult>?> AddFieldAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Body] AddFieldRequest addFieldRequest,
    [Query("client_token")] string? client_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名           | 类型               | 必填 | 说明                                                                                                     |
| ---------------- | ------------------ | ---- | -------------------------------------------------------------------------------------------------------- |
| `app_token`      | `string`           | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                                           |
| `table_id`       | `string`           | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`                                                     |
| `addFieldRequest` | `AddFieldRequest`  | ✅   | 新增多维表格字段操作请求体                                                                               |
| `client_token`   | `string?`          | ⚪   | 操作的唯一标识（uuidv4），用于幂等更新操作，示例值：`fe599b60-450f-46ff-b2ef-9f6675625b97`               |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "field": {
      "field_id": "fldPTb0U2y",
      "field_name": "新字段",
      "type": 1
    }
  }
}
```

**说明**：新增字段时需指定字段名称和字段类型。通过 `client_token` 可实现幂等操作，避免重复创建。

---

### 更新字段

在多维表格数据表中更新一个字段。更新字段时为全量更新，property 等字段会被完全覆盖。

**函数签名**：

```csharp
Task<FeishuApiResult<FieldOpsResult>?> UpdateFieldAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Path] string field_id,
    [Body] UpdateFieldRequest updateFieldRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名               | 类型                  | 必填 | 说明                                                           |
| -------------------- | --------------------- | ---- | -------------------------------------------------------------- |
| `app_token`          | `string`              | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`           | `string`              | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `field_id`           | `string`              | ✅   | 数据表中一个字段的唯一标识，示例值：`fldPTb0U2y`               |
| `updateFieldRequest` | `UpdateFieldRequest`  | ✅   | 更新多维表格字段操作请求体                                     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "field": {
      "field_id": "fldPTb0U2y",
      "field_name": "更新后的字段",
      "type": 1
    }
  }
}
```

**说明**：更新字段时为全量更新，`property` 等字段会被完全覆盖，请确保传入完整的字段属性。

---

### 列出字段

获取多维表格数据表中的所有字段。

**函数签名**：

```csharp
Task<FeishuApiPageListTotalResult<AppTableFieldInfo>?> QueryRecordsPageListAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Query("view_id")] string? view_id = null,
    [Query("text_field_as_array")] bool? text_field_as_array = null,
    [Query("page_size")] int page_size = 20,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                | 类型      | 必填 | 说明                                                                                     |
| --------------------- | --------- | ---- | ---------------------------------------------------------------------------------------- |
| `app_token`           | `string`  | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                           |
| `table_id`            | `string`  | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`                                     |
| `view_id`             | `string?` | ⚪   | 多维表格中视图的唯一标识，示例值：`vewOVMEXPF`                                           |
| `text_field_as_array` | `bool?`   | ⚪   | 控制字段描述 description 数据的返回格式，默认 false，示例值：`true`                      |
| `page_size`           | `int`     | ⚪   | 分页大小，默认值：20                                                                     |
| `page_token`          | `string?` | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "field_id": "fldPTb0U2y",
        "field_name": "字段1",
        "type": 1
      }
    ],
    "page_token": "next_page_token",
    "has_more": false,
    "total": 10
  }
}
```

**说明**：返回数据表中所有字段的信息。当指定 `view_id` 时，仅返回该视图中的字段。`text_field_as_array` 为 true 时，字段描述将以数组形式返回。

---

### 删除字段

删除多维表格数据表中的一个字段。

**函数签名**：

```csharp
Task<FeishuApiResult<DeleteFieldResult>?> DeleteFieldAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Path] string field_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名      | 类型     | 必填 | 说明                                                           |
| ----------- | -------- | ---- | -------------------------------------------------------------- |
| `app_token` | `string` | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`  | `string` | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `field_id`  | `string` | ✅   | 数据表中一个字段的唯一标识，示例值：`fldPTb0U2y`               |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "field_id": "fldPTb0U2y",
    "deleted": true
  }
}
```

**说明**：删除字段后，该字段对应的所有数据将无法恢复，请谨慎操作。

---

### 创建字段编组

用于为多维表格数据表的字段创建编组。创建字段编组后，字段将被组织到该编组中，便于多维表格的数据管理。适用于多维表格字段较多，需要分类管理字段的场景。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateFieldGroupResult>?> CreateFieldGroupAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Body] CreateFieldGroupRequest createFieldGroupRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                     | 类型                        | 必填 | 说明                                                           |
| -------------------------- | --------------------------- | ---- | -------------------------------------------------------------- |
| `app_token`                | `string`                    | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`                 | `string`                    | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `createFieldGroupRequest`  | `CreateFieldGroupRequest`   | ✅   | 新增字段编组请求体                                             |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "field_group": {
      "field_group_id": "fgp1AbcDef",
      "field_group_name": "基本信息"
    }
  }
}
```

**说明**：字段编组用于对数据表中的字段进行分组管理，适用于字段较多的场景。
