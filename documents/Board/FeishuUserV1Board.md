# 画板 - 用户权限（FeishuUserV1Board）

## 接口名称

**画板（用户权限）** -（`IFeishuUserV1Board`）

## 功能描述

提供以用户身份管理飞书画板的能力。画板是全新的图形创作工具，使用门槛低、简洁高效且协作方便，能用画板轻松画出好看的流程图、规划图和方案图，并且可以和团队一起在画板上进行实时的图形化协作。通过画板 API，可以让画板接入内部业务系统，让画板成为业务流程的一部分。

## 参考文档

- [画板概述 - 飞书开放平台](https://open.feishu.cn/document/docs/board-v1/overview)

## 函数列表

| 函数名称                            | 功能描述       | 认证方式 | HTTP 方法 |
| ------------------------------------ | -------------- | -------- | --------- |
| GetWhiteboardThemeAsync              | 获取画板主题   | 用户令牌 | GET       |
| UpdateWhiteboardThemeAsync           | 更新画板主题   | 用户令牌 | POST      |
| DownloadWhiteboardImageAsync         | 获取画板缩略图 | 用户令牌 | GET       |
| CreatePlantumlWhiteboardNodeAsync    | 解析画板语法   | 用户令牌 | POST      |
| CreateWhiteboardNodeAsync            | 创建节点       | 用户令牌 | POST      |
| GetWhiteboardNodesAsync              | 获取所有节点   | 用户令牌 | GET       |

## 函数详细内容

### 获取画板主题

获取画板主题，不同主题下有不同的默认配色。

**函数签名**：

```csharp
Task<FeishuApiResult<GetWhiteboardsThemeResult>?> GetWhiteboardThemeAsync(
    [Path] string whiteboard_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名           | 类型     | 必填 | 说明                                                                                                                                                                    |
| ---------------- | -------- | ---- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `whiteboard_id`  | `string` | ✅   | 画板标识，可通过云文档下的文档接口[获取文档所有块](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/document-block/list)获取，`block_type` 为 43 的 block 即为画板，对应的 `block.token` 就是画板的 `whiteboard_id`，示例值：`Ud8xwWH01hO5mwbakqHbHeqmcCI` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "theme": "light"
  }
}
```

**说明**：返回画板当前的主题信息，不同主题下有不同的默认配色方案。以用户身份操作时，操作记录将关联到当前用户。

---

### 更新画板主题

更新画板的主题设置。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UpdateWhiteboardThemeAsync(
    [Path] string whiteboard_id,
    [Body] UpdateWhiteboardThemeRequest updateWhiteboardThemeRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                            | 类型                           | 必填 | 说明                                                                                                                                                                    |
| --------------------------------- | ------------------------------ | ---- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `whiteboard_id`                   | `string`                       | ✅   | 画板标识，可通过云文档下的文档接口[获取文档所有块](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/document-block/list)获取，`block_type` 为 43 的 block 即为画板，对应的 `block.token` 就是画板的 `whiteboard_id`，示例值：`Ud8xwWH01hO5mwbakqHbHeqmcCI` |
| `updateWhiteboardThemeRequest`    | `UpdateWhiteboardThemeRequest` | ✅   | 更新画板主题请求体                                                                                                                                                      |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：更新画板的主题设置，不同主题下有不同的默认配色方案。

---

### 获取画板缩略图

获取画板的缩略图片，响应数据为图片的二进制图片流。根据 Content-Type 值区分图片格式：image/png、image/jpeg、image/gif、image/svg+xml。

**函数签名**：

```csharp
Task<byte[]?> DownloadWhiteboardImageAsync(
    [Path] string whiteboard_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名           | 类型     | 必填 | 说明                                                                                                                                                                    |
| ---------------- | -------- | ---- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `whiteboard_id`  | `string` | ✅   | 画板标识，可通过云文档下的文档接口[获取文档所有块](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/document-block/list)获取，`block_type` 为 43 的 block 即为画板，对应的 `block.token` 就是画板的 `whiteboard_id`，示例值：`Ud8xwWH01hO5mwbakqHbHeqmcCI` |

**响应**：

图片的二进制数据流（`byte[]`），Content-Type 可能为 `image/png`、`image/jpeg`、`image/gif`、`image/svg+xml`。

**说明**：返回画板的缩略图片，以二进制流形式返回。根据响应的 Content-Type 判断图片格式。

---

### 解析画板语法

用户可以将 PlantUml/Mermaid 图表导入画板进行协同编辑。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> CreatePlantumlWhiteboardNodeAsync(
    [Path] string whiteboard_id,
    [Body] CreatePlantumlWhiteboardNodeRequest createPlantumlWhiteboardNodeRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                                  | 类型                                   | 必填 | 说明                                                                                                                                                                    |
| --------------------------------------- | -------------------------------------- | ---- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `whiteboard_id`                         | `string`                               | ✅   | 画板标识，可通过云文档下的文档接口[获取文档所有块](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/document-block/list)获取，`block_type` 为 43 的 block 即为画板，对应的 `block.token` 就是画板的 `whiteboard_id`，示例值：`Ud8xwWH01hO5mwbakqHbHeqmcCI` |
| `createPlantumlWhiteboardNodeRequest`   | `CreatePlantumlWhiteboardNodeRequest`  | ✅   | 解析画板语法请求体                                                                                                                                                      |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：支持将 PlantUml/Mermaid 语法解析并导入画板，导入后可在画板中进行协同编辑。

---

### 创建节点

创建画板节点，支持批量创建、创建含父子关系的节点等。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateWhiteboardNodeResult>?> CreateWhiteboardNodeAsync(
    [Path] string whiteboard_id,
    [Body] CreateWhiteboardNodeRequest createWhiteboardNodeRequest,
    [Query] string? client_token = null,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                         | 类型                          | 必填 | 说明                                                                                                                                                                    |
| ------------------------------ | ----------------------------- | ---- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `whiteboard_id`                | `string`                      | ✅   | 画板标识，可通过云文档下的文档接口[获取文档所有块](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/document-block/list)获取，`block_type` 为 43 的 block 即为画板，对应的 `block.token` 就是画板的 `whiteboard_id`，示例值：`Ud8xwWH01hO5mwbakqHbHeqmcCI` |
| `createWhiteboardNodeRequest`  | `CreateWhiteboardNodeRequest` | ✅   | 创建画板节点请求体                                                                                                                                                      |
| `client_token`                 | `string?`                     | ⚪   | 操作的唯一标识，用于幂等的进行更新操作。此值为空表示将发起一次新的请求，此值非空表示幂等的进行更新操作，示例值：`fe599b60-450f-46ff-b2ef-9f6675625b9`                     |
| `user_id_type`                 | `string?`                     | ⚪   | 用户 ID 类型，可选值：`open_id`、`union_id`、`user_id`，默认值：`open_id`                                                                                               |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "nodes": [
      {
        "node_id": "node_1",
        "parent_id": null,
        "children": []
      }
    ],
    "client_token": "fe599b60-450f-46ff-b2ef-9f6675625b9"
  }
}
```

**说明**：支持批量创建画板节点，可创建含父子关系的节点。通过 `client_token` 可实现幂等操作，避免重复创建。

---

### 获取所有节点

获取画板内所有的节点，节点以数组方式返回，可通过 parent_id（父节点）、children（子节点）关系组装成画板内容。

**函数签名**：

```csharp
Task<FeishuApiResult<GetWhiteboardNodesResult>?> GetWhiteboardNodesAsync(
    [Path] string whiteboard_id,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名           | 类型      | 必填 | 说明                                                                                                                                                                    |
| ---------------- | --------- | ---- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `whiteboard_id`  | `string`  | ✅   | 画板标识，可通过云文档下的文档接口[获取文档所有块](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/document-docx/docx-v1/document-block/list)获取，`block_type` 为 43 的 block 即为画板，对应的 `block.token` 就是画板的 `whiteboard_id`，示例值：`Ud8xwWH01hO5mwbakqHbHeqmcCI` |
| `user_id_type`   | `string?` | ⚪   | 用户 ID 类型，可选值：`open_id`、`union_id`、`user_id`，默认值：`open_id`                                                                                               |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "nodes": [
      {
        "node_id": "node_1",
        "parent_id": null,
        "children": ["node_2", "node_3"]
      },
      {
        "node_id": "node_2",
        "parent_id": "node_1",
        "children": []
      }
    ]
  }
}
```

**说明**：返回画板内所有节点的列表，节点以数组方式返回。可通过 `parent_id`（父节点）和 `children`（子节点）关系组装成画板的完整内容结构。
