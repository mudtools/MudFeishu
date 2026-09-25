# 飞书妙搭自定义枚举 - 用户令牌（FeishuUserV1SparkAppEnum）

## 接口名称

**飞书妙搭自定义枚举（用户令牌）** -（`IFeishuUserV1SparkAppEnum`）

## 功能描述

提供以用户身份获取飞书妙搭应用自定义枚举的能力。飞书妙搭（Spark）自定义枚举 SDK 是一组服务端 OpenAPI 的封装，用于获取妙搭应用下的自定义枚举列表与详情。支持获取自定义枚举列表和获取自定义枚举详细信息等操作。

## 参考文档

- [飞书妙搭概述 - 飞书开放平台](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/overview)

## 函数列表

| 函数名称              | 功能描述                 | 认证方式 | HTTP 方法 |
| --------------------- | ------------------------ | -------- | --------- |
| GetEnumListAsync      | 获取自定义枚举列表       | 用户令牌 | GET       |
| GetEnumDetailAsync    | 获取自定义枚举详细信息   | 用户令牌 | GET       |

## 函数详细内容

### 获取自定义枚举列表

获取应用下的自定义枚举列表，包括枚举名称、描述、枚举值列表等字段信息。

**函数签名**：

```csharp
Task<FeishuApiResult<GetEnumListResult>?> GetEnumListAsync(
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
        "name": "enum_demo_1",
        "description": "示例枚举",
        "options": [
          "option_a",
          "option_b"
        ],
        "created_at": 1710000000000
      }
    ],
    "has_more": false,
    "page_token": ""
  }
}
```

**说明**：返回自定义枚举分页列表；`has_more` 为 true 时需携带返回的 `page_token` 继续拉取。列表中的 `name` 可作为获取自定义枚举详细信息接口的 `enum_name` 入参。

---

### 获取自定义枚举详细信息

获取应用下的自定义枚举详细信息，包括枚举名称、描述、枚举值列表等字段信息。

**函数签名**：

```csharp
Task<FeishuApiResult<GetEnumDetailResult>?> GetEnumDetailAsync(
    [Path] string app_id,
    [Path] string enum_name,
    [Query("env")] string? env = "online",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名      | 类型      | 必填 | 说明                                                                 |
| ----------- | --------- | ---- | -------------------------------------------------------------------- |
| `app_id`    | `string`  | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取                                 |
| `enum_name` | `string`  | ✅   | 枚举名称，可以从「获取自定义枚举列表」接口返回列表中获取，示例值：`enum_demo_1` |
| `env`       | `string?` | ⚪   | 访问的 database 环境，默认为 online（线上环境），示例值：`online`、`dev` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "name": "enum_demo_1",
    "description": "示例枚举",
    "options": [
      "option_a",
      "option_b"
    ],
    "created_at": "1710000000000"
  }
}
```

**说明**：返回自定义枚举详细信息，包含全部枚举值；以用户身份调用时需具备该妙搭应用的访问权限。
