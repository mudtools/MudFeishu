# 飞书 Aily 数据知识 - 用户令牌（FeishuUserV1AilyDataKnowledge）

## 接口名称

**飞书 Aily 数据知识（用户令牌）** -（`IFeishuUserV1AilyDataKnowledge`）

## 功能描述

提供以用户身份管理飞书 Aily 数据知识的能力。飞书 Aily SDK 是一组服务端 OpenAPI 的封装，用于让用户以编程方式调用飞书 Aily（智能伙伴/智能体）的数据知识能力，把它集成到自己的业务系统里。支持执行数据知识问答、上传文件用于数据知识管理、创建数据知识、获取数据知识、删除数据知识、获取数据知识列表和数据知识分类列表等操作。

## 参考文档

- [数据知识问答 - 飞书开放平台](https://open.feishu.cn/document/aily-v1/data-knowledge/ask)

## 函数列表

| 函数名称                        | 功能描述                     | 认证方式 | HTTP 方法 |
| ------------------------------- | ---------------------------- | -------- | --------- |
| AskDataKnowledgeAsync           | 执行数据知识问答             | 用户令牌 | POST      |
| UploadDataAssetFileAsync        | 上传文件用于数据知识管理     | 用户令牌 | POST      |
| CreateDataAssetAsync            | 创建数据知识                 | 用户令牌 | POST      |
| GetDataAssetAsync               | 获取数据知识                 | 用户令牌 | GET       |
| DeleteDataAssetAsync            | 删除数据知识                 | 用户令牌 | DELETE    |
| GetDataAssetPageListAsync       | 获取数据知识列表             | 用户令牌 | GET       |
| GetDataAssetTagPageListAsync    | 获取数据知识分类列表         | 用户令牌 | GET       |

## 函数详细内容

### 执行数据知识问答

用于对某个 Aily 应用配置的数据知识执行问答，基于指定数据知识范围生成回答。

此接口以 Server-sent Events（SSE）方式更新问答结果，响应体不是常规 JSON 包装，调用方需从 `HttpResponseMessage` 内容流中解析 `data:` 行并反序列化为 `KnowledgeAskEvent`。

**函数签名**：

```csharp
Task<HttpResponseMessage?> AskDataKnowledgeAsync(
    [Path] string app_id,
    [Body] AskDataKnowledgeRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名     | 类型                      | 必填 | 说明                                                       |
| ---------- | ------------------------- | ---- | ---------------------------------------------------------- |
| `app_id`   | `string`                  | ✅   | Aily 应用 ID（spring_xxx__c），示例值：`spring_5862e4fea8__c` |
| `request`  | `AskDataKnowledgeRequest` | ✅   | 执行数据知识问答请求体                                     |

**响应**：Server-sent Events（SSE）流，方法返回承载该响应流的 `HttpResponseMessage`（可空签名，实际成功路径不会为 `null`）

**说明**：调用方负责读取 `Content` 流并解析事件后释放响应，事件对象的类型为 `KnowledgeAskEvent`（包含 `status`、`finish_type`、`message`、`process_data`、`faq_result`、`has_answer` 等字段）。源码注释中的限制（`tenant_access_token` 需开启「支持使用应用身份调用 API 和 SDK」、无法对直连模式引入的飞书云文档执行问答）针对应用身份调用，以 user_access_token（用户身份）调用时不受该限制。服务端返回非 2xx 状态码时由 HTTP 执行器统一抛出 `ApiException`；飞书部分业务错误以 HTTP 200 + JSON 错误体返回（错误码 2700033 执行失败、2700034 问答被禁用、2700035 执行超时），此时本方法会把响应原样返回，调用方需按 Content-Type 自检，详见 `documents/ErrorHandling.md`。

---

### 上传文件用于数据知识管理

用于上传文件到 Aily 数据知识管理系统，上传成功后可获取 file token 用于创建数据知识。

**函数签名**：

```csharp
Task<FeishuApiResult<UploadDataAssetFileResult>?> UploadDataAssetFileAsync(
    [Path] string app_id,
    [FormContent] UploadDataAssetFileRequest request,
    [Query("tenant_type")] string? tenant_type = "dev",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名         | 类型                          | 必填 | 说明                                                       |
| -------------- | ----------------------------- | ---- | ---------------------------------------------------------- |
| `app_id`       | `string`                      | ✅   | Aily 应用 ID（spring_xxx__c），示例值：`spring_dsafdsaf__c` |
| `request`      | `UploadDataAssetFileRequest`  | ✅   | 上传文件请求体（本地文件路径）                             |
| `tenant_type`  | `string?`                     | ⚪   | 应用环境，枚举值：`online`（线上环境，默认值）、`dev`（开发环境），目前只支持 `dev` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "file_info": {
      "token": "boxcnxxxxxxxx",
      "mime_type": "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    }
  }
}
```

**说明**：仅支持开发环境（`tenant_type` 仅支持 `dev`）；仅支持上传 docx、txt、pdf、pptx 类型的文件；开发者需要 Aily 创建平台的应用协作者角色（管理员、开发者、运维人员）。

---

### 创建数据知识

用于在某个 Aily 应用中添加单个数据知识（Data Asset），支持文件导入、飞书云文档/知识空间/服务台直连等数据源。

**函数签名**：

```csharp
Task<FeishuApiResult<DataAssetOopsResult>?> CreateDataAssetAsync(
    [Path] string app_id,
    [Body] CreateDataAssetRequest request,
    [Query("tenant_type")] string? tenant_type = "dev",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名         | 类型                    | 必填 | 说明                                                       |
| -------------- | ----------------------- | ---- | ---------------------------------------------------------- |
| `app_id`       | `string`                | ✅   | Aily 应用 ID（spring_xxx__c），示例值：`spring_dfasdf__c`   |
| `request`      | `CreateDataAssetRequest` | ✅   | 创建数据知识请求体                                         |
| `tenant_type`  | `string?`               | ⚪   | 应用环境，枚举值：`online`（线上环境，默认值）、`dev`（开发环境），目前只支持 `dev` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "data_asset": {
      "data_asset_id": "data_asset_dafefadsaf1",
      "label": {
        "zh_cn": "产品手册"
      },
      "description": {
        "zh_cn": "产品使用说明"
      },
      "data_source_type": "file",
      "connect_status": "connected",
      "connect_type": "import",
      "tags": [
        {
          "data_asset_tag_id": "spring_5862e4fea8__c__asset_tag_aadg2b5ql4gbs",
          "name": "文档"
        }
      ],
      "created_time": "1710000000000",
      "updated_time": "1710000000000"
    }
  }
}
```

**说明**：仅支持开发环境；开发者需要 Aily 平台的应用协作者角色。以用户身份创建时，操作记录将关联到当前用户。

---

### 获取数据知识

用于获取某个 Aily 应用单个数据知识（Data Asset）的详细信息。

**函数签名**：

```csharp
Task<FeishuApiResult<DataAssetOopsResult>?> GetDataAssetAsync(
    [Path] string app_id,
    [Path] string data_asset_id,
    [Query("with_data_asset_item")] bool? with_data_asset_item = null,
    [Query("with_connect_status")] bool? with_connect_status = null,
    [Query("tenant_type")] string? tenant_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                  | 类型      | 必填 | 说明                                                       |
| ----------------------- | --------- | ---- | ---------------------------------------------------------- |
| `app_id`                | `string`  | ✅   | Aily 应用 ID（spring_xxx__c），示例值：`spring_feafdsaf__c` |
| `data_asset_id`         | `string`  | ✅   | 数据知识 ID，示例值：`data_asset_dafefadsaf1`              |
| `with_data_asset_item`  | `bool?`   | ⚪   | 结果是否包含数据与知识项                                   |
| `with_connect_status`   | `bool?`   | ⚪   | 结果是否包含数据知识连接状态                               |
| `tenant_type`           | `string?` | ⚪   | 应用环境，默认为线上环境，`dev` 代表开发环境               |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "data_asset": {
      "data_asset_id": "data_asset_dafefadsaf1",
      "label": {
        "zh_cn": "产品手册"
      },
      "data_source_type": "file",
      "connect_status": "connected",
      "connect_type": "import",
      "created_time": "1710000000000",
      "updated_time": "1710000000000"
    }
  }
}
```

**说明**：返回数据知识详细信息；需要获取条目（知识项）或连接状态时，分别传入 `with_data_asset_item`、`with_connect_status`。

---

### 删除数据知识

用于删除某个 Aily 应用的单个数据知识（Data Asset）。

**函数签名**：

```csharp
Task<FeishuApiResult<DataAssetOopsResult>?> DeleteDataAssetAsync(
    [Path] string app_id,
    [Path] string data_asset_id,
    [Query("tenant_type")] string? tenant_type = "dev",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                       |
| -------------- | --------- | ---- | ---------------------------------------------------------- |
| `app_id`       | `string`  | ✅   | Aily 应用 ID（spring_xxx__c），示例值：`spring_dfadsaf__c`  |
| `data_asset_id`| `string`  | ✅   | 数据知识 ID，示例值：`data_asset_dfadsafe`                 |
| `tenant_type`  | `string?` | ⚪   | 应用环境，枚举值：`online`（线上环境，默认值）、`dev`（开发环境），目前只支持 `dev` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "data_asset": {
      "data_asset_id": "data_asset_dfadsafe"
    }
  }
}
```

**说明**：返回被删除的数据知识信息；仅支持开发环境，删除后基于该数据知识的问答结果不再包含其内容。

---

### 获取数据知识列表

用于获取某个 Aily 应用的数据知识（Data Asset）列表，支持分页与多条件过滤。查询参数采用查询对象模式（`DataAssetListQuery`）。

**函数签名**：

```csharp
Task<FeishuApiResult<DataAssetPageListResult>?> GetDataAssetPageListAsync(
    [Path] string app_id,
    [Query] DataAssetListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名    | 类型                  | 必填 | 说明                                                       |
| --------- | --------------------- | ---- | ---------------------------------------------------------- |
| `app_id`  | `string`              | ✅   | Aily 应用 ID（spring_xxx__c），示例值：`spring_5862e4fea8__c` |
| `query`   | `DataAssetListQuery?` | ⚪   | 分页与过滤查询参数，该查询对象会整体展开为查询参数         |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "data_asset_id": "data_asset_dafefadsaf1",
        "label": {
          "zh_cn": "产品手册"
        },
        "data_source_type": "file",
        "connect_status": "connected"
      }
    ],
    "has_more": false,
    "page_token": ""
  }
}
```

**说明**：返回数据知识分页列表。`DataAssetListQuery` 支持 `PageSize`、`PageToken`、`Keyword`、`DataAssetIds`、`DataAssetTagIds`、`WithDataAssetItem`、`WithConnectStatus` 过滤条件，整体展开为对应的查询参数，新增可选过滤条件时无需修改接口签名。

---

### 获取数据知识分类列表

用于获取某个 Aily 应用的数据知识分类（Data Asset Tag）列表，支持分页与模糊匹配。

**函数签名**：

```csharp
Task<FeishuApiResult<DataAssetTagPageListResult>?> GetDataAssetTagPageListAsync(
    [Path] string app_id,
    [Query("page_size")] int page_size = Consts.PageSize_20,
    [Query("page_token")] string? page_token = null,
    [Query] string? keyword = null,
    [Query] string[]? data_asset_tag_ids = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名               | 类型        | 必填 | 说明                                                                 |
| -------------------- | ----------- | ---- | -------------------------------------------------------------------- |
| `app_id`             | `string`    | ✅   | Aily 应用 ID（spring_xxx__c），示例值：`spring_5862e4fea8__c`         |
| `page_size`          | `int`       | ⚪   | 分页大小，默认 20，最大 100，示例值：`10`                            |
| `page_token`         | `string?`   | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token |
| `keyword`            | `string?`   | ⚪   | 模糊匹配分类名称，示例值：`电影`                                     |
| `data_asset_tag_ids` | `string[]?` | ⚪   | 按数据知识分类 ID 过滤，示例值：`spring_5862e4fea8__c__asset_tag_aadg2b5ql4gbs` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "data_asset_tag_id": "spring_5862e4fea8__c__asset_tag_aadg2b5ql4gbs",
        "name": "电影"
      }
    ],
    "has_more": false,
    "page_token": ""
  }
}
```

**说明**：返回数据知识分类分页列表；`has_more` 为 true 时需携带返回的 `page_token` 继续拉取下一页。
