---
title: 词典草稿接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份管理飞书词典草稿，支持发起创建新词条或更新现有词条的草稿申请，以及按草稿 ID 更新草稿内容。
---

# 词典草稿 - 用户令牌（FeishuUserV1LingoDraft）

## 接口名称

**词典草稿（用户令牌）** -（`IFeishuUserV1LingoDraft`）

## 功能描述

提供以用户身份管理飞书词典草稿的能力。飞书词典（Lingo）草稿入口域用户态 SDK 是一组服务端 OpenAPI 的封装，用于以用户身份发起创建新词条或更新现有词条的草稿申请，以及按草稿 ID 更新草稿内容。本接口全部端点支持 user_access_token 调用。支持创建草稿、更新草稿等操作。

## 参考文档

- [飞书词典概述 - 飞书开放平台](https://open.feishu.cn/document/lingo-v1/overview)

## 函数列表

| 函数名称         | 功能描述 | 认证方式 | HTTP 方法 |
| ---------------- | -------- | -------- | --------- |
| CreateDraftAsync | 创建草稿 | 用户令牌 | POST      |
| UpdateDraftAsync | 更新草稿 | 用户令牌 | PUT       |

## 函数详细内容

### 创建草稿

通过此接口发起创建新词条或更新现有词条的申请，草稿需经词典管理员审核通过后才会写入词库。请求体为词条对象（`CreateOrUpdateEntityRequest`），创建新词条时不填 id，更新已有词条时填入词条 ID。限频：100 次/分钟。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）。字段权限：contact:user.employee_id:readonly（返回的创建者/更新者字段）。支持的应用类型：自建应用。以用户身份创建草稿需该用户拥有对应词库的可见权限。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateDraftResult>?> CreateDraftAsync(
    [Body] CreateOrUpdateEntityRequest request,
    [Query("repo_id")] string? repo_id = null,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名         | 类型                          | 必填 | 说明                                                                                                          |
| -------------- | ----------------------------- | ---- | ------------------------------------------------------------------------------------------------------------- |
| `request`      | `CreateOrUpdateEntityRequest` | ✅   | 词条请求体（main_keys 词条名必填最多 1 个；description 与 rich_text 至少填一个，否则报错 1540001）              |
| `repo_id`      | `string?`                     | ⚪   | 词库 ID，需要在指定词库创建草稿时填写，不填写默认创建至全员词库，示例值：`7202510112396640276`                 |
| `user_id_type` | `string?`                     | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "draft": {
      "draft_id": "7241543272228814852",
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
        "description": "词典词条释义",
        "source": 4
      }
    }
  }
}
```

**说明**：草稿并非词条，需词典管理员审核通过后才会写入词库。创建新词条时请求体中不填 `id`，更新已有词条时填入词条 ID；草稿将以当前用户身份提交。

---

### 更新草稿

根据 draft_id 更新草稿内容，已审批的草稿无法编辑。请求体为词条对象（`CreateOrUpdateEntityRequest`）。限频：100 次/分钟。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）。字段权限：contact:user.employee_id:readonly（返回的创建者/更新者字段）。支持的应用类型：自建应用。

**函数签名**：

```csharp
Task<FeishuApiResult<UpdateDraftResult>?> UpdateDraftAsync(
    [Path] string draft_id,
    [Body] CreateOrUpdateEntityRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名         | 类型                          | 必填 | 说明                                                                                                          |
| -------------- | ----------------------------- | ---- | ------------------------------------------------------------------------------------------------------------- |
| `draft_id`     | `string`                      | ✅   | 草稿 ID，示例值：`7241543272228814852`                                                                         |
| `request`      | `CreateOrUpdateEntityRequest` | ✅   | 词条请求体（main_keys 词条名必填最多 1 个；description 与 rich_text 至少填一个，否则报错 1540001）              |
| `user_id_type` | `string?`                     | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "draft": {
      "draft_id": "7241543272228814852",
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
        "description": "更新后的词典词条释义",
        "source": 4
      }
    }
  }
}
```

**说明**：更新草稿为整体覆盖语义。已审批通过的草稿无法再编辑，需新建草稿提交变更。
