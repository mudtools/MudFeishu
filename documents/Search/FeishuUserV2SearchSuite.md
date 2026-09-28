---
title: 搜索套件接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份在飞书套件内检索消息与应用，根据关键词对当前用户可见的应用、消息进行搜索。
---

# 搜索套件 - 用户令牌（FeishuUserV2SearchSuite）

## 接口名称

**搜索套件（用户令牌）** -（`IFeishuUserV2SearchSuite`）

## 功能描述

提供以用户身份在飞书套件内检索消息与应用的能力。飞书 AI 套件搜索根据搜索关键词（query）对当前用户可见的应用、消息进行搜索，可见性与套件内搜索保持一致。支持搜索消息、搜索应用等操作。

## 参考文档

- [套件搜索 - 飞书开放平台](https://open.feishu.cn/document/server-docs/search-v2/suite-search/create)

## 函数列表

| 函数名称                 | 功能描述 | 认证方式 | HTTP 方法 |
| ------------------------ | -------- | -------- | --------- |
| SearchMessagePageListAsync | 搜索消息 | 用户令牌 | POST      |
| SearchAppPageListAsync   | 搜索应用 | 用户令牌 | POST      |

## 函数详细内容

### 搜索消息

用户可以通过关键字搜索可见消息，可见性和套件内搜索一致。

**函数签名**：

```csharp
Task<FeishuApiPageListResult<string>?> SearchMessagePageListAsync(
    [Body] SearchMessageRequest request,
    [Query] int? page_size = 20,
    [Query] string? page_token = null,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名         | 类型                   | 必填 | 说明                                                                                                                                                        |
| -------------- | ---------------------- | ---- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `request`      | `SearchMessageRequest` | ✅   | 搜索消息请求体                                                                                                                                              |
| `page_size`    | `int?`                 | ⚪   | 分页大小，即本次请求所返回的用户信息列表内的最大条目数，默认值：20                                                                                          |
| `page_token`   | `string?`              | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果                       |
| `user_id_type` | `string?`              | ⚪   | 用户 ID 类型，可选值：`open_id`、`union_id`、`user_id`，示例值：`open_id`，默认值：`open_id`                                                                 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      "om_dc9d2ee6b1d24f8a9b0c1e2f3a4b5c6d"
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**：以用户身份搜索可见消息，搜索结果的可见性与套件内搜索一致，返回命中的消息 ID 列表。当 `has_more` 为 true 时，可使用返回的 `page_token` 继续拉取下一页结果。

---

### 搜索应用

用户可以通过关键字搜索可见应用，可见性和套件内搜索一致。

**函数签名**：

```csharp
Task<FeishuApiPageListResult<string>?> SearchAppPageListAsync(
    [Body] SearchAppRequest request,
    [Query] int? page_size = 20,
    [Query] string? page_token = null,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名         | 类型               | 必填 | 说明                                                                                                                                    |
| -------------- | ------------------ | ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| `request`      | `SearchAppRequest` | ✅   | 搜索应用请求体                                                                                                                          |
| `page_size`    | `int?`             | ⚪   | 分页大小，即本次请求所返回的用户信息列表内的最大条目数，默认值：20                                                                       |
| `page_token`   | `string?`          | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果  |
| `user_id_type` | `string?`          | ⚪   | 用户 ID 类型，可选值：`open_id`、`union_id`、`user_id`，示例值：`open_id`，默认值：`open_id`                                             |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      "cli_a98ea7d1a0ba100b"
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**：以用户身份搜索可见应用，搜索结果的可见性与套件内搜索一致，返回命中的应用 ID 列表。当 `has_more` 为 true 时，可使用返回的 `page_token` 继续拉取下一页结果。
