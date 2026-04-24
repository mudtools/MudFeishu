# 多维表格数据表 - 用户权限（FeishuUserV1BitableAppTable）

## 接口名称

**多维表格数据表（用户权限）** -（`IFeishuUserV1BitableAppTable`）

## 功能描述

提供以用户身份管理飞书多维表格数据表的能力。数据表 table 是多维表格的数据容器，一个多维表格中至少有一个数据表（table），也可能有多个数据表。每个数据表都有唯一标识 table_id。table_id 在一个多维表格 App 中唯一，在全局不一定唯一。支持新增、更新、列出和删除数据表等操作。

## 参考文档

- [多维表格概述 - 飞书开放平台](https://open.feishu.cn/document/server-docs/docs/bitable-v1/bitable-overview)

## 函数列表

| 函数名称                   | 功能描述       | 认证方式 | HTTP 方法 |
| -------------------------- | -------------- | -------- | --------- |
| CreateAppTableAsync        | 新增一个数据表 | 用户令牌 | POST      |
| CreateAppTablesAsync       | 新增多个数据表 | 用户令牌 | POST      |
| UpdateAppTableAsync        | 更新数据表     | 用户令牌 | PATCH     |
| GetAppTablePageListAsync   | 列出数据表     | 用户令牌 | GET       |
| DeleteAppTableAsync        | 删除一个数据表 | 用户令牌 | DELETE    |
| DeleteAppTablesAsync       | 删除多个数据表 | 用户令牌 | DELETE    |

## 函数详细内容

### 新增一个数据表

新增一个数据表，支持传入数据表名称、视图名称和字段。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateTableResult>?> CreateAppTableAsync(
    [Path] string app_token,
    [Body] CreateTableRequest createTableRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名               | 类型                  | 必填 | 说明                                                           |
| -------------------- | --------------------- | ---- | -------------------------------------------------------------- |
| `app_token`          | `string`              | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `createTableRequest` | `CreateTableRequest`  | ✅   | 创建多维表格请求体                                             |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "table_id": "tbl1TkhyTWDkSoZ3",
    "name": "新建数据表",
    "revision": 1
  }
}
```

**说明**：支持在创建时指定数据表名称、默认视图名称和字段定义。以用户身份创建时，操作记录将关联到当前用户。

---

### 新增多个数据表

新增多个数据表，仅可指定数据表名称。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateTablesResult>?> CreateAppTablesAsync(
    [Path] string app_token,
    [Body] CreateTablesRequest createTableRequest,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名               | 类型                   | 必填 | 说明                                                           |
| -------------------- | ---------------------- | ---- | -------------------------------------------------------------- |
| `app_token`          | `string`               | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `createTableRequest` | `CreateTablesRequest`  | ✅   | 创建多维表格请求体                                             |
| `user_id_type`       | `string`               | ⚪   | 用户 ID 类型，ID 类型与查询结果中的 user_id_type 类型保持一致  |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "tables": [
      {
        "table_id": "tbl1TkhyTWDkSoZ3",
        "name": "数据表1"
      },
      {
        "table_id": "tbl2XkhyTWDkSoZ4",
        "name": "数据表2"
      }
    ]
  }
}
```

**说明**：批量新增数据表时仅支持指定数据表名称，不支持自定义字段和视图。

---

### 更新数据表

更新多维表格应用中的数据表信息。

**函数签名**：

```csharp
Task<FeishuApiResult<UpdateAppTableResult>?> UpdateAppTableAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Body] UpdateAppTableRequest updateAppTableRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                   | 类型                      | 必填 | 说明                                                           |
| ------------------------ | ------------------------- | ---- | -------------------------------------------------------------- |
| `app_token`              | `string`                  | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`               | `string`                  | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `updateAppTableRequest`  | `UpdateAppTableRequest`   | ✅   | 更新多维表格应用数据表请求体                                   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "table_id": "tbl1TkhyTWDkSoZ3",
    "name": "更新后的名称",
    "revision": 2
  }
}
```

**说明**：更新数据表时为部分更新，仅更新请求体中指定的字段。

---

### 列出数据表

列出多维表格中的所有数据表，包括其 ID、版本号和名称。

**函数签名**：

```csharp
Task<FeishuApiPageListResult<AppTableBaseInfo>?> GetAppTablePageListAsync(
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
        "table_id": "tbl1TkhyTWDkSoZ3",
        "name": "数据表1",
        "revision": 1
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**：返回多维表格中所有数据表的基本信息，支持分页获取。

---

### 删除一个数据表

通过 app_token 和 table_id 删除指定的多维表格数据表。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteAppTableAsync(
    [Path] string app_token,
    [Path] string table_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名      | 类型     | 必填 | 说明                                                           |
| ----------- | -------- | ---- | -------------------------------------------------------------- |
| `app_token` | `string` | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`  | `string` | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除数据表后，数据表中的所有数据将无法恢复，请谨慎操作。

---

### 删除多个数据表

通过 app_token 和 table_id 删除多个数据表。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteAppTablesAsync(
    [Path] string app_token,
    [Body] BatchDeleteRequest batchDeleteRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名               | 类型                   | 必填 | 说明                                                           |
| -------------------- | ---------------------- | ---- | -------------------------------------------------------------- |
| `app_token`          | `string`               | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `batchDeleteRequest` | `BatchDeleteRequest`   | ✅   | 批量删除多维表格应用数据表请求体                               |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：批量删除数据表后，数据表中的所有数据将无法恢复，请谨慎操作。
