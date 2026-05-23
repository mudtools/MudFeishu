# 多维表格表单 - 用户令牌（FeishuUserV1BitableForm）

## 接口名称

**多维表格表单（用户令牌）** -（`IFeishuUserV1BitableForm`）

## 功能描述

提供以用户身份管理飞书多维表格表单的能力。表单视图 form，表单视图是多维表格的一种视图类型，形式类似于问卷，可以用来收集信息和数据。每个表单都有唯一标识 form_id，即当前视图的 view_id。支持升级表单、更新表单元数据、获取表单元数据、更新表单问题和列出表单问题等操作。

## 参考文档

- [表单视图 - 飞书开放平台](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-table-form/upgrade)

## 函数列表

| 函数名称                     | 功能描述       | 认证方式 | HTTP 方法 |
| ---------------------------- | -------------- | -------- | --------- |
| UpgradeFormAsync             | 升级表单       | 用户令牌 | POST      |
| UpdateFormAsync              | 更新表单元数据 | 用户令牌 | PATCH     |
| GetFormAsync                 | 获取表单元数据 | 用户令牌 | GET       |
| UpdateFormFieldAsync         | 更新表单问题   | 用户令牌 | PATCH     |
| GetFormFieldsPageListAsync   | 列出表单问题   | 用户令牌 | GET       |

## 函数详细内容

### 升级表单

升级旧版表单至收集表。

**函数签名**：

```csharp
Task<FeishuApiResult<UpgradeFormResult>?> UpgradeFormAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Path] string form_id,
    [Body] UpgradeFormRequest upgradeFormRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名               | 类型                  | 必填 | 说明                                                           |
| -------------------- | --------------------- | ---- | -------------------------------------------------------------- |
| `app_token`          | `string`              | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`           | `string`              | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `form_id`            | `string`              | ✅   | 多维表格中表单的唯一标识，示例值：`vew6oMbAa4`                 |
| `upgradeFormRequest` | `UpgradeFormRequest`  | ✅   | 升级表单请求体                                                 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "form": {
      "form_id": "vew6oMbAa4",
      "upgraded": true
    }
  }
}
```

**说明**：升级后的表单将变为收集表形式，功能更丰富。以用户身份操作时，操作记录将关联到当前用户。

---

### 更新表单元数据

更新表单视图中的元数据，包括表单名称、描述、是否共享等。

**函数签名**：

```csharp
Task<FeishuApiResult<FormResult>?> UpdateFormAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Path] string form_id,
    [Body] UpdateFormRequest updateFormRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名               | 类型                  | 必填 | 说明                                                           |
| -------------------- | --------------------- | ---- | -------------------------------------------------------------- |
| `app_token`          | `string`              | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`           | `string`              | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `form_id`            | `string`              | ✅   | 多维表格中表单的唯一标识，示例值：`vew6oMbAa4`                 |
| `updateFormRequest`  | `UpdateFormRequest`   | ✅   | 更新表单请求体                                                 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "form": {
      "form_id": "vew6oMbAa4",
      "name": "更新后的表单",
      "description": "表单描述",
      "shared": true
    }
  }
}
```

**说明**：更新表单的元数据信息，支持修改名称、描述和共享状态等。

---

### 获取表单元数据

获取表单的所有元数据，包括表单名称、描述、是否共享等。

**函数签名**：

```csharp
Task<FeishuApiResult<FormResult>?> GetFormAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Path] string form_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名      | 类型     | 必填 | 说明                                                           |
| ----------- | -------- | ---- | -------------------------------------------------------------- |
| `app_token` | `string` | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`  | `string` | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `form_id`   | `string` | ✅   | 多维表格中表单的唯一标识，示例值：`vew6oMbAa4`                 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "form": {
      "form_id": "vew6oMbAa4",
      "name": "表单名称",
      "description": "表单描述",
      "shared": true
    }
  }
}
```

**说明**：返回表单的完整元数据信息。

---

### 更新表单问题

更新表单中的问题项。

**函数签名**：

```csharp
Task<FeishuApiResult<UpdateFormFieldResult>?> UpdateFormFieldAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Path] string form_id,
    [Path] string field_id,
    [Body] UpdateFormFieldRequest updateFormFieldRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                     | 类型                       | 必填 | 说明                                                                                                     |
| -------------------------- | -------------------------- | ---- | -------------------------------------------------------------------------------------------------------- |
| `app_token`                | `string`                   | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                                           |
| `table_id`                 | `string`                   | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`                                                     |
| `form_id`                  | `string`                   | ✅   | 多维表格中表单的唯一标识，示例值：`vew6oMbAa4`                                                           |
| `field_id`                 | `string`                   | ✅   | 表单问题的唯一标识，示例值：`fldjX7dUj5`                                                                 |
| `updateFormFieldRequest`   | `UpdateFormFieldRequest`   | ✅   | 更新表单字段请求                                                                                         |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "field": {
      "field_id": "fldjX7dUj5",
      "field_name": "更新后的问题",
      "required": true
    }
  }
}
```

**说明**：表单中的问题本质上是表单视图中的字段，可通过列出字段接口获取问题标识。

---

### 列出表单问题

分页列出表单中的所有问题项。

**函数签名**：

```csharp
Task<FeishuApiPageListTotalResult<AppTableFormFieldInfo>?> GetFormFieldsPageListAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Path] string form_id,
    [Query("page_size")] int page_size = 20,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名        | 类型      | 必填 | 说明                                                                                     |
| ------------- | --------- | ---- | ---------------------------------------------------------------------------------------- |
| `app_token`   | `string`  | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                           |
| `table_id`    | `string`  | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`                                     |
| `form_id`     | `string`  | ✅   | 多维表格中表单的唯一标识，示例值：`vew6oMbAa4`                                           |
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
        "field_id": "fldjX7dUj5",
        "field_name": "问题1",
        "type": 1,
        "required": true
      }
    ],
    "page_token": "next_page_token",
    "has_more": false,
    "total": 5
  }
}
```

**说明**：返回表单中所有问题项的信息，支持分页获取。
