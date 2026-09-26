# 词典词条 - 租户令牌（FeishuTenantV1LingoEntity）

## 接口名称

**词典词条（租户令牌）** -（`IFeishuTenantV1LingoEntity`）

## 功能描述

提供以租户身份管理飞书词典词条的能力。飞书词典（Lingo）词条入口域租户态 SDK 是一组服务端 OpenAPI 的封装，除继承自 `IFeishuV1LingoEntity` 的通用查询端点外，本接口还提供免审创建/更新/删除词条端点（仅 tenant_access_token 可调用）。支持获取词条详情、获取词条列表、模糊搜索词条、精准搜索词条、词条高亮、创建免审词条、更新免审词条、删除免审词条等操作。

## 参考文档

- [飞书词典概述 - 飞书开放平台](https://open.feishu.cn/document/lingo-v1/overview)

## 函数列表

| 函数名称                | 功能描述         | 认证方式 | HTTP 方法 |
| ----------------------- | ---------------- | -------- | --------- |
| GetEntityAsync          | 获取词条详情     | 租户令牌 | GET       |
| GetEntityListAsync      | 获取词条列表     | 租户令牌 | GET       |
| SearchEntityAsync       | 模糊搜索词条     | 租户令牌 | POST      |
| MatchEntityAsync        | 精准搜索词条     | 租户令牌 | POST      |
| HighlightEntityAsync    | 词条高亮         | 租户令牌 | POST      |
| CreateEntityAsync       | 创建免审词条     | 租户令牌 | POST      |
| UpdateEntityAsync       | 更新免审词条     | 租户令牌 | PUT       |
| DeleteEntityAsync       | 删除免审词条     | 租户令牌 | DELETE    |

## 函数详细内容

### 获取词条详情

按词条 ID 获取词条详情；也可通过 provider + outer_id 以外部系统关联方式查询。返回结果包含词条名、别名、释义、相关信息、反馈统计、外部关联、创建/更新信息等。限频：1000 次/分钟、50 次/秒。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。字段权限：contact:user.employee_id:readonly（返回的创建者/更新者字段）。支持的应用类型：自建应用。

**函数签名**：

```csharp
Task<FeishuApiResult<GetEntityResult>?> GetEntityAsync(
    [Path] string entity_id,
    [Query("provider")] string? provider = null,
    [Query("outer_id")] string? outer_id = null,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                                          |
| -------------- | --------- | ---- | ------------------------------------------------------------------------------------------------------------- |
| `entity_id`    | `string`  | ✅   | 词条 ID，示例值：`enterprise_515879`                                                                            |
| `provider`     | `string?` | ⚪   | 外部系统，长度 2 ～ 32 字符，示例值：`星云`                                                                     |
| `outer_id`     | `string?` | ⚪   | 词条在外部系统中对应的唯一 ID，长度 1 ～ 64 字符，示例值：`123aaa`                                             |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "entity": {
      "id": "enterprise_515879",
      "main_keys": [
        {
          "key": "飞书词典",
          "display_status": {
            "allow_highlight": true,
            "allow_search": true
          }
        }
      ],
      "aliases": [],
      "description": "飞书词典是飞书提供的一款词条释义工具",
      "rich_text": "",
      "creator": "ou_30b07b63089ea46518789914dac63d36",
      "create_time": "1627540853",
      "updater": "ou_30b07b63089ea46518789914dac63d36",
      "update_time": "1627541853",
      "source": 4
    }
  }
}
```

**说明**：除按词条 ID 查询外，也可通过 `provider` 与 `outer_id` 以外部系统关联方式定位词条。创建者与更新者字段需 contact:user.employee_id:readonly 字段权限才会返回。

---

### 获取词条列表

分页获取飞书词典的全部词条，支持按外部系统 provider 过滤、按词库 repo_id 拉取。限频：1000 次/分钟、50 次/秒。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。字段权限：contact:user.employee_id:readonly（返回的创建者/更新者字段）。支持的应用类型：自建应用。以应用身份拉取非全员词库的词条需在「词库设置」页面添加应用。

**函数签名**：

```csharp
Task<FeishuApiResult<GetEntityListResult>?> GetEntityListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("provider")] string? provider = null,
    [Query("repo_id")] string? repo_id = null,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型      | 必填 | 说明                                                                                                          |
| -------------- | --------- | ---- | ------------------------------------------------------------------------------------------------------------- |
| `page_size`    | `int?`    | ⚪   | 分页大小，默认 20，取值范围 1 ～ 100                                                                            |
| `page_token`   | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                                        |
| `provider`     | `string?` | ⚪   | 相关外部系统，可用来过滤词条数据，长度 2 ～ 32 字符，示例值：`星云`                                            |
| `repo_id`      | `string?` | ⚪   | 词库 ID，不传时默认返回全员词库数据，示例值：`7152790921053274113`                                             |
| `user_id_type` | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "entities": [
      {
        "id": "enterprise_515879",
        "main_keys": [
          {
            "key": "飞书词典",
            "display_status": {
              "allow_highlight": true,
              "allow_search": true
            }
          }
        ],
        "description": "飞书词典是飞书提供的一款词条释义工具",
        "source": 4
      }
    ],
    "page_token": "408ecac018b2e3518db37275e812aad7bb8ad3e755fc886f322ac6c430ba",
    "has_more": true
  }
}
```

**说明**：当 `has_more` 为 true 时，可使用返回的 `page_token` 继续拉取下一页数据。拉取非全员词库前需先在「词库设置」页面添加应用。

---

### 模糊搜索词条

传入关键词，与词条名、别名、释义等信息进行模糊匹配，返回搜到的词条信息；支持按分类、创建来源、创建者过滤。限频：1000 次/分钟、50 次/秒。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。字段权限：contact:user.employee_id:readonly（返回的创建者/更新者字段）。支持的应用类型：自建应用。

**函数签名**：

```csharp
Task<FeishuApiResult<SearchEntityResult>?> SearchEntityAsync(
    [Body] SearchEntityRequest request,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("repo_id")] string? repo_id = null,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                  | 必填 | 说明                                                                                                          |
| -------------- | --------------------- | ---- | ------------------------------------------------------------------------------------------------------------- |
| `request`      | `SearchEntityRequest` | ✅   | 搜索请求体（query 搜索关键词 1 ～ 100 字符、classification_filter 分类筛选、sources 创建来源、creators 创建者） |
| `page_size`    | `int?`                | ⚪   | 每页返回的词条量，默认 20，取值范围 1 ～ 100                                                                   |
| `page_token`   | `string?`             | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token                                                        |
| `repo_id`      | `string?`             | ⚪   | 词库 ID，不传时默认在全员词库内搜索，示例值：`7202510112396640276`                                             |
| `user_id_type` | `string?`             | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "entities": [
      {
        "id": "enterprise_515879",
        "main_keys": [
          {
            "key": "飞书词典",
            "display_status": {
              "allow_highlight": true,
              "allow_search": true
            }
          }
        ],
        "description": "飞书词典是飞书提供的一款词条释义工具",
        "source": 4
      }
    ],
    "page_token": "b152fa6e6f62a291019a04c3a93f365f8ac641910506ff15ff4cad6534e087cb4ed8fa2c",
    "has_more": true
  }
}
```

**说明**：模糊匹配覆盖词条名、别名与释义，可通过请求体的分类、创建来源与创建者条件进一步收窄结果范围。

---

### 精准搜索词条

将关键词与词条名、别名精准匹配，并返回对应的词条 ID，可在外部系统中快速定位词条。限频：1000 次/分钟、50 次/秒。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。支持的应用类型：自建应用。

**函数签名**：

```csharp
Task<FeishuApiResult<MatchEntityResult>?> MatchEntityAsync(
    [Body] MatchEntityRequest request,
    [Query("repo_id")] string? repo_id = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                 | 必填 | 说明                                                             |
| --------- | -------------------- | ---- | ---------------------------------------------------------------- |
| `request` | `MatchEntityRequest` | ✅   | 匹配请求体（word 搜索关键词必填，1 ～ 100 字符）                  |
| `repo_id` | `string?`            | ⚪   | 词库 ID，不传时默认在全员词库内搜索，示例值：`7202510112396640276` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "results": [
      {
        "entity_id": "enterprise_34***584",
        "type": 0
      }
    ]
  }
}
```

**说明**：与模糊搜索不同，精准搜索只与词条名、别名做完全匹配，适合在外部系统中按确定名称定位词条 ID。

---

### 词条高亮

传入一句话，智能识别句中对应的词条，并返回词条位置和 entity_id，可在外部系统中快速实现词条智能高亮。限频：1000 次/分钟、50 次/秒。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。支持的应用类型：自建应用。

**函数签名**：

```csharp
Task<FeishuApiResult<HighlightEntityResult>?> HighlightEntityAsync(
    [Body] HighlightEntityRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                     | 必填 | 说明                                                     |
| --------- | ------------------------ | ---- | -------------------------------------------------------- |
| `request` | `HighlightEntityRequest` | ✅   | 高亮请求体（text 需要识别的内容必填，1 ～ 1000 字符）     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "phrases": [
      {
        "name": "企业百科",
        "entity_ids": ["enterprise_51587960"],
        "span": {
          "start": 0,
          "end": 4
        }
      }
    ]
  }
}
```

**说明**：返回的 `span` 为词条在传入文本中的位置区间（从 0 开始计数，utf-8 编码），可直接用于前端高亮渲染。

---

### 创建免审词条

通过此接口创建的词条无需经过词典管理员审核，直接写入词库，调用时应当慎重操作。请求体为词条对象（`CreateOrUpdateEntityRequest`）。限频：100 次/分钟。所需权限：baike:entity:exempt_review（创建、更新词典免审词条）。字段权限：contact:user.employee_id:readonly（返回的创建者/更新者字段）。支持的应用类型：自建应用。仅支持 tenant_access_token 调用。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateEntityResult>?> CreateEntityAsync(
    [Body] CreateOrUpdateEntityRequest request,
    [Query("repo_id")] string? repo_id = null,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                          | 必填 | 说明                                                                                                          |
| -------------- | ----------------------------- | ---- | ------------------------------------------------------------------------------------------------------------- |
| `request`      | `CreateOrUpdateEntityRequest` | ✅   | 词条请求体（main_keys 词条名必填最多 1 个；description 与 rich_text 至少填一个，否则报错 1540001）              |
| `repo_id`      | `string?`                     | ⚪   | 词库 ID，需要在指定词库创建词条时传入，不传时默认创建至全员词库，示例值：`71527909****274113`                  |
| `user_id_type` | `string?`                     | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "entity": {
      "id": "enterprise_40217521",
      "main_keys": [
        {
          "key": "飞书词典",
          "display_status": {
            "allow_highlight": true,
            "allow_search": true
          }
        }
      ],
      "description": "飞书词典是飞书提供的一款词条释义工具",
      "source": 4
    }
  }
}
```

**说明**：免审词条跳过词典管理员审核直接写入词库，请确认内容合规后再调用。本端点仅支持 tenant_access_token 调用。

---

### 更新免审词条

通过此接口更新已有的词条，无需经过词典管理员审核，直接写入词库，调用时应当慎重操作。请求体为词条对象（`CreateOrUpdateEntityRequest`）。限频：100 次/分钟。所需权限：baike:entity:exempt_review（创建、更新词典免审词条）。字段权限：contact:user.employee_id:readonly（返回的创建者/更新者字段）。支持的应用类型：自建应用。仅支持 tenant_access_token 调用。

**函数签名**：

```csharp
Task<FeishuApiResult<UpdateEntityResult>?> UpdateEntityAsync(
    [Path] string entity_id,
    [Body] CreateOrUpdateEntityRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                          | 必填 | 说明                                                                                                          |
| -------------- | ----------------------------- | ---- | ------------------------------------------------------------------------------------------------------------- |
| `entity_id`    | `string`                      | ✅   | 词条 ID，示例值：`enterprise_40217521`                                                                          |
| `request`      | `CreateOrUpdateEntityRequest` | ✅   | 词条请求体（main_keys 词条名必填最多 1 个；description 与 rich_text 至少填一个，否则报错 1540001）              |
| `user_id_type` | `string?`                     | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "entity": {
      "id": "enterprise_40217521",
      "main_keys": [
        {
          "key": "飞书词典",
          "display_status": {
            "allow_highlight": true,
            "allow_search": true
          }
        }
      ],
      "description": "更新后的词条释义",
      "update_time": "1627541853",
      "source": 4
    }
  }
}
```

**说明**：更新为整体覆盖语义，请求体中未给出的字段将被清空。本端点仅支持 tenant_access_token 调用。

---

### 删除免审词条

通过此接口删除词条，无需经过词典管理员审核，直接从词库移除。使用外部系统关联方式删除时，需将路径中的词条 ID 固定为 enterprise_0，并同时提供 provider 与 outer_id。限频：1000 次/分钟、50 次/秒。所需权限：baike:entity:exempt_delete（免审删除词典词条）。支持的应用类型：自建应用。仅支持 tenant_access_token 调用。

**函数签名**：

```csharp
Task<FeishuApiResult<DeleteEntityResult>?> DeleteEntityAsync(
    [Path] string entity_id,
    [Query("provider")] string? provider = null,
    [Query("outer_id")] string? outer_id = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名      | 类型      | 必填 | 说明                                                                                                        |
| ----------- | --------- | ---- | ----------------------------------------------------------------------------------------------------------- |
| `entity_id` | `string`  | ✅   | 词条 ID，示例值：`enterprise_43742132363`；使用外部系统关联删除时固定为 `enterprise_0`                       |
| `provider`  | `string?` | ⚪   | 外部系统，使用外部系统关联删除时必填（不能包含中横线 "-"），长度 2 ～ 32 字符，示例值：`星云`                |
| `outer_id`  | `string?` | ⚪   | 词条在外部系统中对应的唯一 ID，使用外部系统关联删除时必填（不能包含中横线 "-"），长度 1 ～ 64 字符，示例值：`123aaa` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**说明**：删除成功时响应体无业务字段，仅返回 code 与 msg。删除后词条直接从词库移除且不可恢复，请谨慎操作。本端点仅支持 tenant_access_token 调用。
