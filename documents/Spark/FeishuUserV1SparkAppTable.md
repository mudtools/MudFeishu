# 飞书妙搭数据表 - 用户令牌（FeishuUserV1SparkAppTable）

## 接口名称

**飞书妙搭数据表（用户令牌）** -（`IFeishuUserV1SparkAppTable`）

## 功能描述

提供以用户身份管理飞书妙搭应用数据表与数据记录的能力。飞书妙搭（Spark）数据表 SDK 是一组服务端 OpenAPI 的封装，用于以编程方式管理妙搭应用下的数据表与数据记录。支持获取数据表列表、获取数据表详细信息、查询数据表数据记录、添加或更新记录、按条件更新记录、批量更新记录和删除记录等操作。

## 参考文档

- [飞书妙搭概述 - 飞书开放平台](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/overview)

## 函数列表

| 函数名称                        | 功能描述                 | 认证方式 | HTTP 方法 |
| ------------------------------- | ------------------------ | -------- | --------- |
| GetTableListAsync               | 获取数据表列表           | 用户令牌 | GET       |
| GetTableDetailAsync             | 获取数据表详细信息       | 用户令牌 | GET       |
| GetTableRecordListAsync         | 查询数据表数据记录       | 用户令牌 | GET       |
| PostTableRecordsAsync           | 向数据表中添加或更新记录 | 用户令牌 | POST      |
| PatchTableRecordsAsync          | 按条件更新数据表中的记录 | 用户令牌 | PATCH     |
| BatchUpdateTableRecordsAsync    | 批量更新数据表中的记录   | 用户令牌 | PATCH     |
| DeleteTableRecordsAsync         | 删除数据表中的记录       | 用户令牌 | DELETE    |

## 函数详细内容

### 获取数据表列表

获取应用下的数据表列表，包含数据表名称、描述，以及数据表列信息等字段。

**函数签名**：

```csharp
Task<FeishuApiResult<GetTableListResult>?> GetTableListAsync(
    [Path] string app_id,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("env")] string? env = "online",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名        | 类型      | 必填 | 说明                                                                 |
| ------------- | --------- | ---- | -------------------------------------------------------------------- |
| `app_id`      | `string`  | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取，如 `https://miaoda.feishu.cn/app/app_4jcn5n11bpf5v` 中的 `app_4jcn5n11bpf5v` 即为 app_id |
| `page_size`   | `int`     | ⚪   | 分页大小，用于限制一次请求所返回的数据条目数，默认 10，最大 500，示例值：`10` |
| `page_token`  | `string?` | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token |
| `env`         | `string?` | ⚪   | 访问的 database 环境，默认为 online（线上环境），示例值：`online`、`dev` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "name": "student_table",
        "description": "学生信息表",
        "columns": [
          {
            "name": "id",
            "description": "主键",
            "data_type": "int",
            "is_primary_key": true,
            "is_unique": true,
            "is_auto_increment": true,
            "is_array": false,
            "is_allow_null": false,
            "default_value": ""
          }
        ]
      }
    ],
    "has_more": false,
    "page_token": ""
  }
}
```

**说明**：返回数据表分页列表；`has_more` 为 true 时需携带返回的 `page_token` 继续拉取。列表中的 `name` 可作为后续数据表相关接口的 `table_name` 入参。

---

### 获取数据表详细信息

获取应用下的数据表详情，包含数据表名称、描述，以及数据表列信息等字段。

**函数签名**：

```csharp
Task<FeishuApiResult<AppTable>?> GetTableDetailAsync(
    [Path] string app_id,
    [Path] string table_name,
    [Query("env")] string? env = "online",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名        | 类型      | 必填 | 说明                                                             |
| ------------- | --------- | ---- | ---------------------------------------------------------------- |
| `app_id`      | `string`  | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取                             |
| `table_name`  | `string`  | ✅   | 妙搭数据表表名，必须属于 app_id 对应的妙搭应用，示例值：`student_table` |
| `env`         | `string?` | ⚪   | 访问的 database 环境，默认为 online（线上环境），示例值：`online`、`dev` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "name": "student_table",
    "description": "学生信息表",
    "columns": [
      {
        "name": "id",
        "description": "主键",
        "data_type": "int",
        "is_primary_key": true,
        "is_unique": true,
        "is_auto_increment": true,
        "is_array": false,
        "is_allow_null": false,
        "default_value": ""
      }
    ]
  }
}
```

**说明**：返回数据表详细信息，`columns` 描述各列的数据类型、主键/唯一/自增/数组/允许为空等约束，可用于构造写入记录的请求体。

---

### 查询数据表数据记录

查询应用下的数据表数据记录，包括指定列、字段值及分页信息，适用于需要获取应用下某数据表数据的记录、展示等场景。查询参数采用查询对象模式（`GetTableRecordListQuery`）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetTableRecordListResult>?> GetTableRecordListAsync(
    [Path] string app_id,
    [Path] string table_name,
    [Query] GetTableRecordListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名        | 类型                            | 必填 | 说明                                                             |
| ------------- | ------------------------------- | ---- | ---------------------------------------------------------------- |
| `app_id`      | `string`                        | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取                             |
| `table_name`  | `string`                        | ✅   | 妙搭数据表表名，必须属于 app_id 对应的妙搭应用，示例值：`student_table` |
| `query`       | `GetTableRecordListQuery?`      | ⚪   | 分页、列筛选、过滤、排序、环境与用户 ID 类型查询参数，该查询对象会整体展开为查询参数 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": "[{\"id\":1,\"name\":\"张三\"}]",
    "total": 100,
    "has_more": false,
    "page_token": ""
  }
}
```

**说明**：返回数据记录分页列表，`items` 为记录数组序列化后的 JSONString，需调用方自行反序列化。`GetTableRecordListQuery` 支持 `PageSize`、`PageToken`、`Select`、`Filter`、`Order`、`Env`、`UserIdentifierType` 过滤与排序条件，整体展开为对应的查询参数，新增可选条件时无需修改接口签名。

---

### 向数据表中添加或更新记录

向应用下的数据表中添加或更新记录。

可选请求头 `Prefer` 控制行为：为空表示 INSERT；`resolution=merge-duplicates` 表示 UPSERT；`missing=default` 代表未传入的字段按默认值处理；可组合，如 `resolution=merge-duplicates, missing=default`。

**函数签名**：

```csharp
Task<FeishuApiResult<UpsertTableRecordsResult>?> PostTableRecordsAsync(
    [Path] string app_id,
    [Path] string table_name,
    [Body] PostTableRecordsRequest request,
    [Query("columns")] string? columns = null,
    [Query("on_conflict")] string? on_conflict = null,
    [Query("env")] string? env = "online",
    [Query("user_identifier_type")] string? user_identifier_type = "miaoda_user_id",
    [Header("Prefer")] string? prefer = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                 | 类型                         | 必填 | 说明                                                             |
| ---------------------- | ---------------------------- | ---- | ---------------------------------------------------------------- |
| `app_id`               | `string`                     | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取                             |
| `table_name`           | `string`                     | ✅   | 妙搭数据表表名，必须属于 app_id 对应的妙搭应用，示例值：`student_table` |
| `request`              | `PostTableRecordsRequest`    | ✅   | 添加或更新记录请求体（`records` 为 JSON 数组字符串，单次最大 500 条） |
| `columns`              | `string?`                    | ⚪   | UPSERT 时使用，指定列，多列英文逗号拼接，示例值：`name,age`      |
| `on_conflict`          | `string?`                    | ⚪   | UPSERT 时使用，指定使用哪一个或多个具有唯一约束的字段作为冲突判断依据，默认为表主键，示例值：`user_id,product_id` |
| `env`                  | `string?`                    | ⚪   | 访问的 database 环境，默认为 online（线上环境），示例值：`online`、`dev` |
| `user_identifier_type` | `string?`                    | ⚪   | 此次调用使用的用户 ID 类型，示例值：`miaoda_user_id`、`open_id`、`union_id`，默认值：`miaoda_user_id` |
| `prefer`               | `string?`                    | ⚪   | 可选请求头（`Prefer`），如 `resolution=merge-duplicates`（UPSERT）、`missing=default`，可逗号组合 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "record_ids": [
      "1",
      "2"
    ]
  }
}
```

**说明**：返回按记录顺序创建或更新的记录 ID 列表；默认行为（不传 `Prefer`）为 INSERT，如需 UPSERT 需同时设置 `Prefer` 请求头并根据需要指定 `columns`、`on_conflict`。

---

### 按条件更新数据表中的记录

将数据表中符合 filter 条件的记录更新为 record 参数指定的内容。

**函数签名**：

```csharp
Task<FeishuApiResult<UpsertTableRecordsResult>?> PatchTableRecordsAsync(
    [Path] string app_id,
    [Path] string table_name,
    [Body] PatchTableRecordsRequest request,
    [Query("filter")] string filter,
    [Query("env")] string? env = "online",
    [Query("user_identifier_type")] string? user_identifier_type = "miaoda_user_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                 | 类型                        | 必填 | 说明                                                             |
| ---------------------- | --------------------------- | ---- | ---------------------------------------------------------------- |
| `app_id`               | `string`                    | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取                             |
| `table_name`           | `string`                    | ✅   | 妙搭数据表表名，必须属于 app_id 对应的妙搭应用，示例值：`student_table` |
| `request`              | `PatchTableRecordsRequest`  | ✅   | 更新记录请求体（`record` 为 JSON 对象字符串）                    |
| `filter`               | `string`                    | ✅   | 筛选条件，遵循 PostgREST 语法（horizontal filtering），示例值：`age=gt.10` |
| `env`                  | `string?`                   | ⚪   | 访问的 database 环境，默认为 online（线上环境），示例值：`online`、`dev` |
| `user_identifier_type` | `string?`                   | ⚪   | 此次调用使用的用户 ID 类型，示例值：`miaoda_user_id`、`open_id`、`union_id`，默认值：`miaoda_user_id` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "record_ids": [
      "1",
      "2"
    ]
  }
}
```

**说明**：返回更新的记录唯一 ID 列表；`filter` 为必填参数，未命中任何记录时返回空列表。

---

### 批量更新数据表中的记录

批量更新应用下的数据表中的记录，每条记录需包含主键如 _id，单次最多 500 条，且不同行要更新的字段需保持一致。

**函数签名**：

```csharp
Task<FeishuApiResult<UpsertTableRecordsResult>?> BatchUpdateTableRecordsAsync(
    [Path] string app_id,
    [Path] string table_name,
    [Body] BatchUpdateTableRecordsRequest request,
    [Query("env")] string? env = "online",
    [Query("user_identifier_type")] string? user_identifier_type = "miaoda_user_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                 | 类型                             | 必填 | 说明                                                             |
| ---------------------- | -------------------------------- | ---- | ---------------------------------------------------------------- |
| `app_id`               | `string`                         | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取                             |
| `table_name`           | `string`                         | ✅   | 妙搭数据表表名，必须属于 app_id 对应的妙搭应用，示例值：`student_table` |
| `request`              | `BatchUpdateTableRecordsRequest` | ✅   | 批量更新记录请求体（`records` 为 JSON 数组字符串，单次最大 500 条） |
| `env`                  | `string?`                        | ⚪   | 访问的 database 环境，默认为 online（线上环境），示例值：`online`、`dev` |
| `user_identifier_type` | `string?`                        | ⚪   | 此次调用使用的用户 ID 类型，示例值：`miaoda_user_id`、`open_id`、`union_id`，默认值：`miaoda_user_id` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "record_ids": [
      "1",
      "2"
    ]
  }
}
```

**说明**：返回更新的记录唯一 ID 列表；单次最多 500 条，每条记录需包含主键（如 `_id`），且不同行要更新的字段需保持一致。

---

### 删除数据表中的记录

删除指定应用下数据表中符合 filter 筛选条件的记录，删除后记录不可恢复。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteTableRecordsAsync(
    [Path] string app_id,
    [Path] string table_name,
    [Query("filter")] string filter,
    [Query("env")] string? env = "online",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名        | 类型      | 必填 | 说明                                                             |
| ------------- | --------- | ---- | ---------------------------------------------------------------- |
| `app_id`      | `string`  | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取                             |
| `table_name`  | `string`  | ✅   | 妙搭数据表表名，必须属于 app_id 对应的妙搭应用，示例值：`student_table` |
| `filter`      | `string`  | ✅   | 筛选条件，遵循 PostgREST 语法（horizontal filtering），此处用法和查询数据记录一致，示例值：`age=gt.10` |
| `env`         | `string?` | ⚪   | 访问的 database 环境，默认为 online（线上环境），示例值：`online`、`dev` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：删除成功时 `data` 为空对象；删除操作不可恢复，建议先用查询接口确认 `filter` 命中范围。
