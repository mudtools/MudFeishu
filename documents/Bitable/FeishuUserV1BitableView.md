# 多维表格视图 - 用户令牌（FeishuUserV1BitableView）

## 接口名称

**多维表格视图（用户令牌）** -（`IFeishuUserV1BitableView`）

## 功能描述

提供以用户身份管理飞书多维表格视图的能力。视图 view 是多维表格数据的汇总和展现形式。视图有多种类型，包括表格视图、看板视图、画册视图、甘特视图和表单视图等。一个数据表至少有一个视图，可能有多个视图。每个视图都有唯一标识 view_id，view_id 在一个多维表格中唯一，在全局不一定唯一。支持新增、更新、列出、获取和删除视图等操作。

## 参考文档

- [多维表格概述 - 飞书开放平台](https://open.feishu.cn/document/server-docs/docs/bitable-v1/bitable-overview)

## 函数列表

| 函数名称                   | 功能描述     | 认证方式 | HTTP 方法 |
| -------------------------- | ------------ | -------- | --------- |
| CreateViewAsync            | 新增视图     | 用户令牌 | POST      |
| UpdateViewAsync            | 更新视图     | 用户令牌 | PATCH     |
| GetViewsPageListAsync      | 分页列出视图 | 用户令牌 | GET       |
| GetViewAsync               | 获取视图     | 用户令牌 | GET       |
| DeleteViewAsync            | 删除视图     | 用户令牌 | DELETE    |

## 函数详细内容

### 新增视图

在多维表格数据表中新增一个视图，可指定视图类型，包括表格视图、看板视图、画册视图、甘特视图和表单视图。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateViewResult>?> CreateViewAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Body] CreateViewRequest createViewRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名               | 类型                  | 必填 | 说明                                                           |
| -------------------- | --------------------- | ---- | -------------------------------------------------------------- |
| `app_token`          | `string`              | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`           | `string`              | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `createViewRequest`  | `CreateViewRequest`   | ✅   | 创建多维表格应用视图请求体                                     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "view": {
      "view_id": "vewTpR1urY",
      "view_name": "新建视图",
      "view_type": "grid"
    }
  }
}
```

**说明**：新增视图时需指定视图类型和名称，支持多种视图类型。以用户身份操作时，操作记录将关联到当前用户。

---

### 更新视图

更新多维表格数据表中的视图信息。

**函数签名**：

```csharp
Task<FeishuApiResult<UpdateViewResult>?> UpdateViewAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Path] string view_id,
    [Body] UpdateViewRequest updateViewRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名               | 类型                  | 必填 | 说明                                                           |
| -------------------- | --------------------- | ---- | -------------------------------------------------------------- |
| `app_token`          | `string`              | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`           | `string`              | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `view_id`            | `string`              | ✅   | 多维表格中视图的唯一标识，示例值：`vewTpR1urY`                 |
| `updateViewRequest`  | `UpdateViewRequest`   | ✅   | 更新多维表格应用视图请求体                                     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "view": {
      "view_id": "vewTpR1urY",
      "view_name": "更新后的视图",
      "view_type": "grid"
    }
  }
}
```

**说明**：更新视图时为部分更新，仅更新请求体中指定的字段。

---

### 分页列出视图

分页获取多维表格数据表中的所有视图。

**函数签名**：

```csharp
Task<FeishuApiPageListTotalResult<AppViewDetailInfo>?> GetViewsPageListAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Query("page_size")] int page_size = 20,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名        | 类型      | 必填 | 说明                                                                                     |
| ------------- | --------- | ---- | ---------------------------------------------------------------------------------------- |
| `app_token`   | `string`  | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                           |
| `table_id`    | `string`  | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`                                     |
| `page_size`   | `int`     | ⚪   | 分页大小，默认值：20                                                                     |
| `page_token`  | `string?` | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token |
| `user_id_type` | `string` | ⚪   | 用户 ID 类型，ID 类型与查询结果中的 user_id_type 类型保持一致                            |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "view_id": "vewTpR1urY",
        "view_name": "视图1",
        "view_type": "grid"
      }
    ],
    "page_token": "next_page_token",
    "has_more": false,
    "total": 3
  }
}
```

**说明**：返回数据表中所有视图的详细信息，支持分页获取。

---

### 获取视图

根据视图 ID 获取现有视图信息，包括视图名称、类型、属性等。

**函数签名**：

```csharp
Task<FeishuApiResult<GetViewResult>?> GetViewAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Path] string view_id,
    [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名        | 类型     | 必填 | 说明                                                           |
| ------------- | -------- | ---- | -------------------------------------------------------------- |
| `app_token`   | `string` | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`    | `string` | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `view_id`     | `string` | ✅   | 多维表格中视图的唯一标识，示例值：`vewTpR1urY`                 |
| `user_id_type` | `string` | ⚪   | 用户 ID 类型，ID 类型与查询结果中的 user_id_type 类型保持一致  |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "view": {
      "view_id": "vewTpR1urY",
      "view_name": "视图名称",
      "view_type": "grid",
      "property": {}
    }
  }
}
```

**说明**：返回指定视图的完整信息，包括名称、类型和属性等。

---

### 删除视图

通过 app_token、table_id 和 view_id，删除多维表格数据表中的指定视图。

**函数签名**：

```csharp
Task<FeishuApiResult<GetViewResult>?> DeleteViewAsync(
    [Path] string app_token,
    [Path] string table_id,
    [Path] string view_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名      | 类型     | 必填 | 说明                                                           |
| ----------- | -------- | ---- | -------------------------------------------------------------- |
| `app_token` | `string` | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `table_id`  | `string` | ✅   | 多维表格数据表的唯一标识，示例值：`tbl1TkhyTWDkSoZ3`           |
| `view_id`   | `string` | ✅   | 多维表格中视图的唯一标识，示例值：`vewTpR1urY`                 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "view": {
      "view_id": "vewTpR1urY",
      "deleted": true
    }
  }
}
```

**说明**：删除视图后，该视图的配置将无法恢复，请谨慎操作。注意：数据表至少需要保留一个视图，无法删除最后一个视图。
