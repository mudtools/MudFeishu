# 多维表格记录 - 租户令牌（FeishuTenantV1BitableRecord）

## 接口名称

**多维表格记录（租户令牌）** -（`IFeishuTenantV1BitableRecord`）

## 功能描述

提供以租户身份管理飞书多维表格记录的能力。记录 record 是多维表格的数据表中的每一行数据都是一条记录（record）。每条记录都有唯一标识 record_id，record_id 在一个多维表格中唯一，在全局不一定唯一。支持新增、更新、查询、删除记录，以及批量操作等。

## 参考文档

- [记录数据结构概述 - 飞书开放平台](https://open.feishu.cn/document/docs/bitable-v1/app-table-record/bitable-record-data-structure-overview)

## 函数列表

| 函数名称                    | 功能描述     | 认证方式 | HTTP 方法 |
| --------------------------- | ------------ | -------- | --------- |
| AddRecordAsync              | 新增记录     | 租户令牌 | POST      |
| UpdateRecordAsync           | 更新记录     | 租户令牌 | PUT       |
| QueryRecordsPageListAsync   | 查询记录     | 租户令牌 | POST      |
| DeleteRecordAsync           | 删除记录     | 租户令牌 | DELETE    |
| AddRecordsAsync             | 新增多条记录 | 租户令牌 | POST      |
| UpdateRecordsAsync          | 更新多条记录 | 租户令牌 | POST      |
| GetRecordsAsync             | 批量获取记录 | 租户令牌 | POST      |
| DeleteRecordsAsync          | 批量删除记录 | 租户令牌 | POST      |

## 函数详细内容

### 新增记录

在多维表格数据表中新增一条记录。

**函数签名**：

```csharp
Task<FeishuApiResult<RecordOpsResult>?> AddRecordAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Body] RecordOpsRequest addRecordRequest,
    [Query("client_token")] string? client_token = null,
    [Query("ignore_consistency_check")] bool? ignore_consistency_check = false,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                      | 类型               | 必填 | 说明                                                                                                     |
| --------------------------- | ------------------ | ---- | -------------------------------------------------------------------------------------------------------- |
| `app_token`                 | `string`           | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                                           |
| `table_id`                  | `string`           | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`                                                     |
| `addRecordRequest`          | `RecordOpsRequest` | ✅   | 新增记录请求体                                                                                           |
| `client_token`              | `string?`          | ⚪   | 操作的唯一标识（uuidv4），用于幂等更新操作，示例值：`fe599b60-450f-46ff-b2ef-9f6675625b97`               |
| `ignore_consistency_check`  | `bool?`            | ⚪   | 是否忽略一致性读写检查，默认 false，示例值：`true`                                                       |
| `user_id_type`              | `string`           | ⚪   | 用户 ID 类型，ID 类型与查询结果中的 user_id_type 类型保持一致                                            |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "record": {
      "record_id": "recqwIwhc6",
      "fields": {}
    }
  }
}
```

**说明**：新增记录时需传入字段值。通过 `client_token` 可实现幂等操作，避免重复创建。`ignore_consistency_check` 为 true 时可提高性能，但可能导致数据暂时不一致。

---

### 更新记录

更新多维表格数据表中的一条记录。

**函数签名**：

```csharp
Task<FeishuApiResult<RecordOpsResult>?> UpdateRecordAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Path] string record_id,
    [Body] RecordOpsRequest updateRecordRequest,
    [Query("client_token")] string? client_token = null,
    [Query("ignore_consistency_check")] bool? ignore_consistency_check = false,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                      | 类型               | 必填 | 说明                                                                                                     |
| --------------------------- | ------------------ | ---- | -------------------------------------------------------------------------------------------------------- |
| `app_token`                 | `string`           | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                                           |
| `table_id`                  | `string`           | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`                                                     |
| `record_id`                 | `string`           | ✅   | 数据表中一条记录的唯一标识，示例值：`recqwIwhc6`                                                         |
| `updateRecordRequest`       | `RecordOpsRequest` | ✅   | 更新记录请求体                                                                                           |
| `client_token`              | `string?`          | ⚪   | 操作的唯一标识（uuidv4），用于幂等更新操作，示例值：`fe599b60-450f-46ff-b2ef-9f6675625b97`               |
| `ignore_consistency_check`  | `bool?`            | ⚪   | 是否忽略一致性读写检查，默认 false，示例值：`true`                                                       |
| `user_id_type`              | `string`           | ⚪   | 用户 ID 类型，ID 类型与查询结果中的 user_id_type 类型保持一致                                            |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "record": {
      "record_id": "recqwIwhc6",
      "fields": {}
    }
  }
}
```

**说明**：更新记录时为全量更新，请求体中指定的字段值将覆盖原有值。

---

### 查询记录

用于查询数据表中的现有记录，单次最多查询 500 行记录，支持分页获取。

**函数签名**：

```csharp
Task<FeishuApiPageListTotalResult<AppTableRecord>?> QueryRecordsPageListAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Body] QueryRecordsRequest queryRecordsRequest,
    [Query("page_size")] int page_size = 20,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                | 类型                   | 必填 | 说明                                                                                     |
| --------------------- | ---------------------- | ---- | ---------------------------------------------------------------------------------------- |
| `app_token`           | `string`               | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                           |
| `table_id`            | `string`               | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`                                     |
| `queryRecordsRequest` | `QueryRecordsRequest`  | ✅   | 查询记录请求体                                                                           |
| `page_size`           | `int`                  | ⚪   | 分页大小，默认值：20                                                                     |
| `page_token`          | `string?`              | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token |
| `user_id_type`        | `string`               | ⚪   | 用户 ID 类型，ID 类型与查询结果中的 user_id_type 类型保持一致                            |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "record_id": "recqwIwhc6",
        "fields": {}
      }
    ],
    "page_token": "next_page_token",
    "has_more": false,
    "total": 100
  }
}
```

**说明**：查询记录支持通过过滤条件和排序规则筛选数据，单次最多返回 500 条记录。

---

### 删除记录

删除多维表格数据表中的一条记录。

**函数签名**：

```csharp
Task<FeishuApiResult<DeleteRecordResult>?> DeleteRecordAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Path] string record_id,
    [Query("ignore_consistency_check")] bool? ignore_consistency_check = false,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                      | 类型     | 必填 | 说明                                                           |
| --------------------------- | -------- | ---- | -------------------------------------------------------------- |
| `app_token`                 | `string` | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`                  | `string` | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `record_id`                 | `string` | ✅   | 数据表中一条记录的唯一标识，示例值：`recqwIwhc6`               |
| `ignore_consistency_check`  | `bool?`  | ⚪   | 是否忽略一致性读写检查，默认 false，示例值：`true`             |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "deleted": true
  }
}
```

**说明**：删除记录后数据将无法恢复，请谨慎操作。

---

### 新增多条记录

在多维表格数据表中新增多条记录，单次调用最多新增 1,000 条记录。

**函数签名**：

```csharp
Task<FeishuApiResult<RecordsOpsResult>?> AddRecordsAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Body] AddRecordsRequest addRecordsRequest,
    [Query("client_token")] string? client_token = null,
    [Query("ignore_consistency_check")] bool? ignore_consistency_check = false,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                      | 类型                 | 必填 | 说明                                                                                                     |
| --------------------------- | -------------------- | ---- | -------------------------------------------------------------------------------------------------------- |
| `app_token`                 | `string`             | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                                           |
| `table_id`                  | `string`             | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`                                                     |
| `addRecordsRequest`         | `AddRecordsRequest`  | ✅   | 新增多条记录请求体                                                                                       |
| `client_token`              | `string?`            | ⚪   | 操作的唯一标识（uuidv4），用于幂等更新操作，示例值：`fe599b60-450f-46ff-b2ef-9f6675625b97`               |
| `ignore_consistency_check`  | `bool?`              | ⚪   | 是否忽略一致性读写检查，默认 false，示例值：`true`                                                       |
| `user_id_type`              | `string`             | ⚪   | 用户 ID 类型，ID 类型与查询结果中的 user_id_type 类型保持一致                                            |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "records": [
      {
        "record_id": "recqwIwhc6",
        "fields": {}
      }
    ]
  }
}
```

**说明**：批量新增记录时单次最多新增 1,000 条记录。通过 `client_token` 可实现幂等操作。

---

### 更新多条记录

更新数据表中的多条记录，单次调用最多更新 1,000 条记录。

**函数签名**：

```csharp
Task<FeishuApiResult<RecordsOpsResult>?> UpdateRecordsAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Body] UpdateRecordsRequest updateRecordsRequest,
    [Query("ignore_consistency_check")] bool? ignore_consistency_check = false,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                      | 类型                     | 必填 | 说明                                                           |
| --------------------------- | ------------------------ | ---- | -------------------------------------------------------------- |
| `app_token`                 | `string`                 | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`                  | `string`                 | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `updateRecordsRequest`      | `UpdateRecordsRequest`   | ✅   | 更新记录请求体                                                 |
| `ignore_consistency_check`  | `bool?`                  | ⚪   | 是否忽略一致性读写检查，默认 false，示例值：`true`             |
| `user_id_type`              | `string`                 | ⚪   | 用户 ID 类型，ID 类型与查询结果中的 user_id_type 类型保持一致  |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "records": [
      {
        "record_id": "recqwIwhc6",
        "fields": {}
      }
    ]
  }
}
```

**说明**：批量更新记录时单次最多更新 1,000 条记录。

---

### 批量获取记录

通过多个记录 ID 查询记录信息。该接口最多支持查询 100 条记录。

**函数签名**：

```csharp
Task<FeishuApiResult<GetRecordsResult>?> GetRecordsAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Body] GetRecordsRequest queryRecordsRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                | 类型                  | 必填 | 说明                                                           |
| --------------------- | --------------------- | ---- | -------------------------------------------------------------- |
| `app_token`           | `string`              | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`            | `string`              | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `queryRecordsRequest` | `GetRecordsRequest`   | ✅   | 批量获取记录请求体                                             |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "records": [
      {
        "record_id": "recqwIwhc6",
        "fields": {}
      }
    ]
  }
}
```

**说明**：通过记录 ID 列表批量获取记录详情，最多支持 100 条记录。

---

### 批量删除记录

删除多维表格数据表中现有的多条记录。

**函数签名**：

```csharp
Task<FeishuApiResult<DeleteRecordsResult>?> DeleteRecordsAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Body] DeleteRecordsRequest queryRecordsRequest,
    [Query("ignore_consistency_check")] bool? ignore_consistency_check = false,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                      | 类型                     | 必填 | 说明                                                           |
| --------------------------- | ------------------------ | ---- | -------------------------------------------------------------- |
| `app_token`                 | `string`                 | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`                  | `string`                 | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `queryRecordsRequest`       | `DeleteRecordsRequest`   | ✅   | 批量删除记录请求体                                             |
| `ignore_consistency_check`  | `bool?`                  | ⚪   | 是否忽略一致性读写检查，默认 false，示例值：`true`             |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "deleted_count": 5
  }
}
```

**说明**：批量删除记录后数据将无法恢复，请谨慎操作。
