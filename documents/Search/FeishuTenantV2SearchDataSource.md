---
title: 搜索数据源接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份管理飞书搜索连接器数据源，支持数据源与数据范式的创建/更新/删除/查询，以及数据项的索引创建、批量创建与删除。
---

# 搜索数据源 - 租户令牌（FeishuTenantV2SearchDataSource）

## 接口名称

**搜索数据源（租户令牌）** -（`IFeishuTenantV2SearchDataSource`）

## 功能描述

提供以租户身份管理飞书搜索连接器数据源的能力。飞书搜索连接器提供了一组 RESTful API，帮助企业快速实现飞书之外的各类信息的搜索能力，在飞书内即可实现一站式的信息检索和获取，有效提升工作和协同效率。开发者使用数据源 API 建立数据源，再通过数据 API 将数据推送到该数据源，就完成了飞书搜索能力的构建。支持创建、删除、更新、获取、批量获取数据源，为指定数据项创建索引、批量为数据项创建索引、删除数据项、查询指定数据项，以及创建、删除、修改、获取数据范式等操作。

## 参考文档

- [搜索连接器概述 - 飞书开放平台](https://open.feishu.cn/document/server-docs/search-v2/open-search/search-connector-overview--)

## 函数列表

| 函数名称                      | 功能描述             | 认证方式 | HTTP 方法 | 接口文档 |
| ----------------------------- | -------------------- | -------- | --------- |----------|
| CreateDataSourceAsync         | 创建数据源           | 租户令牌 | POST      | [CreateDataSourceAsync](https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source/create) |
| DeleteDataSourceAsync         | 删除数据源           | 租户令牌 | DELETE    | [DeleteDataSourceAsync](https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source/delete) |
| UpdateDataSourceAsync         | 更新数据源           | 租户令牌 | PATCH     | [UpdateDataSourceAsync](https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source/patch) |
| GetDataSourceAsync            | 获取数据源           | 租户令牌 | GET       | [GetDataSourceAsync](https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source/get) |
| GetDataSourcePageListAsync    | 批量获取数据源       | 租户令牌 | GET       | [GetDataSourcePageListAsync](https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source/list) |
| CreateDataItemIndexAsync      | 为指定数据项创建索引 | 租户令牌 | POST      | [CreateDataItemIndexAsync](https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source-item/create) |
| BatchCreateDataItemIndexAsync | 批量为数据项创建索引 | 租户令牌 | POST      | [BatchCreateDataItemIndexAsync](https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source-item/batch_create) |
| DeleteDataItemIndexAsync      | 删除数据项           | 租户令牌 | DELETE    | [DeleteDataItemIndexAsync](https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source/delete) |
| GetDataItemIndexAsync         | 查询指定数据项       | 租户令牌 | GET       | [GetDataItemIndexAsync](https://open.feishu.cn/document/server-docs/search-v2/open-search/data_source-item/get?appId=cli_a98ea7d1a0ba100b) |
| CreateSchemaAsync             | 创建数据范式         | 租户令牌 | POST      | [CreateSchemaAsync](https://open.feishu.cn/document/server-docs/search-v2/open-search/schema/create) |
| DeleteSchemaAsync             | 删除数据范式         | 租户令牌 | DELETE    | [DeleteSchemaAsync](https://open.feishu.cn/document/server-docs/search-v2/open-search/schema/delete) |
| UpdateSchemaAsync             | 修改数据范式         | 租户令牌 | PATCH     | [UpdateSchemaAsync](https://open.feishu.cn/document/server-docs/search-v2/open-search/schema/patch) |
| GetSchemaAsync                | 获取数据范式         | 租户令牌 | GET       | [GetSchemaAsync](https://open.feishu.cn/document/server-docs/search-v2/open-search/schema/get) |

## 函数详细内容

### 创建数据源

创建搜索数据源。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateDataSourceResult>?> CreateDataSourceAsync(
    [Body] CreateDataSourceRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                     | 必填 | 说明               |
| --------- | ------------------------ | ---- | ------------------ |
| `request` | `CreateDataSourceRequest` | ✅   | 创建数据源请求体   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "data_source": {
      "id": 6953903108179099667,
      "name": "客服工单",
      "state": 0,
      "description": "搜索客服工单数据",
      "icon_url": "https://www.xxx.com/open.jpg",
      "template": "search_common_card",
      "schema_id": "custom_schema",
      "create_time": "1618831236",
      "update_time": "1618831236",
      "is_exceed_quota": false
    }
  }
}
```

**说明**：创建数据源后即可通过数据项索引接口向该数据源推送数据。数据源状态 `state` 为 0 表示已上线，1 表示未上线，未填写时默认为未上线。

---

### 删除数据源

删除一个已存在的数据源。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteDataSourceAsync(
    [Path] string data_source_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名           | 类型     | 必填 | 说明                                             |
| ---------------- | -------- | ---- | ------------------------------------------------ |
| `data_source_id` | `string` | ✅   | 数据源的唯一标识，示例值：`6953903108179099667` |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除数据源会同时移除该数据源下的全部数据项索引，且不可恢复，请谨慎操作。

---

### 更新数据源

更新一个已存在的数据源。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UpdateDataSourceAsync(
    [Path] string data_source_id,
    [Body] UpdateDataSourceRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名           | 类型                     | 必填 | 说明                                             |
| ---------------- | ------------------------ | ---- | ------------------------------------------------ |
| `data_source_id` | `string`                 | ✅   | 数据源的唯一标识，示例值：`6953903108179099667` |
| `request`        | `UpdateDataSourceRequest` | ✅   | 更新数据源请求体                                 |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：更新数据源时按请求体中已赋值的字段做局部更新，未赋值的字段保持不变。

---

### 获取数据源

获取已经创建的数据源。

**函数签名**：

```csharp
Task<FeishuApiResult<GetDataSourceResult>?> GetDataSourceAsync(
    [Path] string data_source_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名           | 类型     | 必填 | 说明                                             |
| ---------------- | -------- | ---- | ------------------------------------------------ |
| `data_source_id` | `string` | ✅   | 数据源的唯一标识，示例值：`6953903108179099667` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "data_source": {
      "id": 6953903108179099667,
      "name": "客服工单",
      "state": 0,
      "app_id": "cli_a98ea7d1a0ba100b",
      "create_time": "1618831236",
      "update_time": "1618831236",
      "is_exceed_quota": false
    }
  }
}
```

**说明**：返回数据源的全量信息，并额外带上该数据源对应的开放平台应用 ID（`app_id`）。

---

### 批量获取数据源

批量获取创建的数据源信息。

**函数签名**：

```csharp
Task<FeishuApiPageListResult<AppDataSourceInfo>?> GetDataSourcePageListAsync(
    [Query] int? view = null,
    [Query] int? page_size = 20,
    [Query] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                                                                             |
| ------------ | --------- | ---- | ---------------------------------------------------------------------------------------------------------------- |
| `view`       | `int?`    | ⚪   | 回包数据格式，0-全量数据；1-摘要数据（摘要数据仅包含 `id`、`name`、`state`），可选值：0、1，默认值：null         |
| `page_size`  | `int?`    | ⚪   | 分页大小，即本次请求所返回的信息列表内的最大条目数，默认值：20                                                   |
| `page_token` | `string?` | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "id": 6953903108179099667,
        "name": "客服工单",
        "state": 0,
        "app_id": "cli_a98ea7d1a0ba100b"
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**：当 `has_more` 为 true 时，可使用返回的 `page_token` 继续拉取下一页数据。取摘要数据（`view` = 1）时回包仅包含 `id`、`name`、`state` 三个字段。

---

### 为指定数据项创建索引

索引一条数据记录。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> CreateDataItemIndexAsync(
    [Path] string data_source_id,
    [Body] CreateDataItemIndexRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名           | 类型                         | 必填 | 说明                                             |
| ---------------- | ---------------------------- | ---- | ------------------------------------------------ |
| `data_source_id` | `string`                     | ✅   | 数据源的唯一标识，示例值：`6953903108179099667` |
| `request`        | `CreateDataItemIndexRequest` | ✅   | 创建数据项索引请求体                             |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：同一数据源下重复提交同一 `item_id` 时为覆盖语义。数据项的 `acl` 为空数组时默认数据不可见，如需全员可见需设置 `access` = `allow`、`type` = `user`、`value` = `everyone`。

---

### 批量为数据项创建索引

为提高索引数据记录的速度，特提供批量索引数据记录的接口。

**函数签名**：

```csharp
Task<FeishuApiResult<BatchCreateDataItemIndexResult>?> BatchCreateDataItemIndexAsync(
    [Path] string data_source_id,
    [Body] BatchCreateDataItemIndexRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名           | 类型                              | 必填 | 说明                                             |
| ---------------- | --------------------------------- | ---- | ------------------------------------------------ |
| `data_source_id` | `string`                          | ✅   | 数据源的唯一标识，示例值：`6953903108179099667` |
| `request`        | `BatchCreateDataItemIndexRequest` | ✅   | 创建数据项索引请求体                             |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "result": [
      {
        "err": "",
        "is_success": true,
        "item_id": "my_item_01010111"
      }
    ]
  }
}
```

**说明**：批量索引为逐条处理，返回 `result` 列表按提交顺序给出每条数据项的处理结果，需逐项检查 `is_success`。注意：一个 batch 中所有数据项的 data source ID 需要一致，tenant ID 也需要一致。

---

### 删除数据项

删除数据项。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteDataItemIndexAsync(
    [Path] string data_source_id,
    [Path] string item_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名           | 类型     | 必填 | 说明                                             |
| ---------------- | -------- | ---- | ------------------------------------------------ |
| `data_source_id` | `string` | ✅   | 数据源的唯一标识，示例值：`6953903108179099667` |
| `item_id`        | `string` | ✅   | 数据记录的 ID，示例值：`01010111`               |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：按数据源 ID 与数据项 ID 定位并删除单条数据项索引，删除后该数据项不再被飞书搜索召回。

---

### 查询指定数据项

获取单个数据记录。

**函数签名**：

```csharp
Task<FeishuApiResult<DataItemIndex>?> GetDataItemIndexAsync(
    [Path] string data_source_id,
    [Path] string item_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名           | 类型     | 必填 | 说明                                             |
| ---------------- | -------- | ---- | ------------------------------------------------ |
| `data_source_id` | `string` | ✅   | 数据源的唯一标识，示例值：`6953903108179099667` |
| `item_id`        | `string` | ✅   | 数据记录的 ID，示例值：`01010111`               |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "id": "my_item_01010111",
    "acl": [
      {
        "access": "allow",
        "value": "everyone",
        "type": "user"
      }
    ],
    "metadata": {
      "title": "工单：无法创建文章",
      "source_url": "http://www.abc.com.cn",
      "create_time": 1618831236,
      "update_time": 1618831236
    },
    "structured_data": "{\"description\":\"问题出现的环境和复现方法描述\",\"priority\":\"HIGH\"}",
    "content": {
      "format": "html",
      "content_data": "这是一个很长的文本"
    }
  }
}
```

**说明**：返回数据项的完整索引内容，包含访问权限控制（`acl`）、元信息（`metadata`）、结构化数据（`structured_data`）与非结构化数据（`content`）。

---

### 创建数据范式

创建一个数据范式。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateSchemaResult>?> CreateSchemaAsync(
    [Body] CreateSchemaRequest request,
    [Query] bool? validate_only = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名          | 类型                  | 必填 | 说明                                       |
| --------------- | --------------------- | ---- | ------------------------------------------ |
| `request`       | `CreateSchemaRequest` | ✅   | 创建数据范式请求体                         |
| `validate_only` | `bool?`               | ⚪   | 是否只用来校验合法性，示例值：`true`，默认 false |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "schema": {
      "schema_id": "custom_schema_id",
      "properties": [
        {
          "name": "summary",
          "type": "text",
          "is_searchable": true,
          "is_returnable": true,
          "is_sortable": false
        }
      ],
      "display": {
        "card_key": "search_common_card"
      }
    }
  }
}
```

**说明**：`validate_only` 为 true 时只校验数据范式定义是否合法，不会真正创建。`schema_id` 最大长度 40 字符，需匹配 `^[a-zA-Z][a-zA-Z0-9-_].*$`。

---

### 删除数据范式

删除已存在的数据范式。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteSchemaAsync(
    [Path] string schema_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名      | 类型     | 必填 | 说明                                                                                                        |
| ----------- | -------- | ---- | ----------------------------------------------------------------------------------------------------------- |
| `schema_id` | `string` | ✅   | 用户自定义数据范式的唯一标识，示例值：`custom_schema_id`，最大长度 `40` 字符，正则校验 `^[a-zA-Z][a-zA-Z0-9-_].*$` |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除数据范式前请确认其未被数据源引用，否则可能导致对应数据源的数据展示异常。

---

### 修改数据范式

修改数据范式。

**函数签名**：

```csharp
Task<FeishuApiResult<UpdateSchemaResult>?> UpdateSchemaAsync(
    [Path] string schema_id,
    [Body] UpdateSchemaRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名      | 类型                  | 必填 | 说明                                                                                                        |
| ----------- | --------------------- | ---- | ----------------------------------------------------------------------------------------------------------- |
| `schema_id` | `string`              | ✅   | 用户自定义数据范式的唯一标识，示例值：`custom_schema_id`，最大长度 `40` 字符，正则校验 `^[a-zA-Z][a-zA-Z0-9-_].*$` |
| `request`   | `UpdateSchemaRequest` | ✅   | 修改数据范式请求体                                                                                          |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "schema": {
      "schema_id": "custom_schema_id",
      "properties": [
        {
          "name": "summary",
          "type": "text",
          "is_searchable": true,
          "is_returnable": true,
          "is_sortable": false
        }
      ],
      "display": {
        "card_key": "search_common_card"
      }
    }
  }
}
```

**说明**：修改数据范式为整体覆盖语义，请求体需给出完整的 `properties` 与 `display` 定义。

---

### 获取数据范式

获取单个数据范式。

**函数签名**：

```csharp
Task<FeishuApiResult<GetSchemaResult>?> GetSchemaAsync(
    [Path] string schema_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名      | 类型     | 必填 | 说明                                                                                                        |
| ----------- | -------- | ---- | ----------------------------------------------------------------------------------------------------------- |
| `schema_id` | `string` | ✅   | 用户自定义数据范式的唯一标识，示例值：`custom_schema_id`，最大长度 `40` 字符，正则校验 `^[a-zA-Z][a-zA-Z0-9-_].*$` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "schema": {
      "schema_id": "custom_schema_id",
      "properties": [
        {
          "name": "summary",
          "type": "text",
          "is_searchable": true,
          "is_returnable": true,
          "is_sortable": false
        }
      ],
      "display": {
        "card_key": "search_common_card"
      }
    }
  }
}
```

**说明**：通过数据范式 ID 获取其属性定义与展示配置，可用于校验数据源推送的数据结构是否符合约定。
