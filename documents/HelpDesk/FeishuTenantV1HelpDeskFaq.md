---
title: 服务台常见问题接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份查询飞书服务台知识库 FAQ，支持列表查询、关键词搜索、详情查询与 FAQ 图片获取。
---

# IFeishuTenantV1HelpDeskFaq - 租户知识库FAQAPI

## 功能描述
飞书服务台知识库FAQ API是开放平台基于飞书服务台知识库的常见问题功能开放的查看/创建/修改/删除等API，开发者可以基于这些API对服务台知识库FAQ进行操作。本接口使用租户访问令牌（TenantAccessToken）鉴权；同时继承自 `IFeishuV1HelpDeskFaq` 的 `HelpdeskTokenAndId` 属性用于在服务台请求 Header 中添加 `X-Lark-Helpdesk-Authorization` 参数（Value 为 `base64(helpdesk_id:helpdesk_token)`，即通过 base64 加密将 helpdesk_id 和 helpdesk_token 用 `:` 连接而成的字符串）。

## 参考文档
- [查询知识库FAQ列表](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/list)
- [搜索知识库FAQ](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/search)
- [获取知识库FAQ详情](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/get)
- [获取知识库FAQ图片](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/faq_image)
- [服务台接入指南](https://open.feishu.cn/document/server-docs/helpdesk-v1/access-guide)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| GetFaqListAsync | 查询知识库FAQ列表 | TenantAccessToken | GET |
| SearchFaqAsync | 搜索知识库FAQ | TenantAccessToken | GET |
| GetFaqAsync | 获取知识库FAQ详情 | TenantAccessToken | GET |
| GetFaqImageAsync | 获取知识库FAQ图片 | TenantAccessToken | GET |

## 函数详细内容

### GetFaqListAsync
用于获取服务台知识库详情，按分类、状态、关键词分页返回 FAQ 列表。

**函数签名**
```csharp
Task<FeishuApiResult<GetFaqListResult>?> GetFaqListAsync(
    [Query] string? category_id = null,
    [Query] string? status = null,
    [Query] string? search = null,
    [Query] string? page_token = null,
    [Query] int? page_size = Consts.PageSize_20,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| category_id | string? | ⚪ | 知识库分类 ID | 6856395522433908739 |
| status | string? | ⚪ | 搜索条件: 知识库状态 1 在线、0 已删除可恢复、2 已删除不可恢复 | 1 |
| search | string? | ⚪ | 搜索条件: 关键词，匹配问题标题、问题关键词、用户名 | Order |
| page_token | string? | ⚪ | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果 | 6856395634652479491 |
| page_size | int? | ⚪ | 分页大小，最大值为 100，默认 20 | 10 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "has_more": true,
    "page_token": "6856395634652479491",
    "page_size": 10,
    "total": 100,
    "items": [
      {
        "faq_id": "6936004780707807231",
        "id": "6936004780707807231",
        "helpdesk_id": "6936004780707807251",
        "question": "Question",
        "answer": "Answer",
        "content": "Answer",
        "type": "text",
        "create_time": 1596379008,
        "update_time": 1596379008,
        "categories": [],
        "tags": ["Similar questions"]
      }
    ]
  }
}
```

**说明**
- `page_size` 最大值为 100，默认为 20
- `has_more` 为 true 时会同时返回新的 `page_token`
- `faq_id` 为推荐使用的 FAQ ID，`id` 为旧版本字段

**代码示例**
```csharp
var faqApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskFaq>();
faqApi.HelpdeskTokenAndId = Convert.ToBase64String(
    Encoding.UTF8.GetBytes($"{helpdeskId}:{helpdeskToken}"));

var result = await faqApi.GetFaqListAsync(status: "1", page_size: 20);
foreach (var faq in result?.Data?.Items ?? Array.Empty<Faq>())
{
    Console.WriteLine($"[{faq.FaqId}] {faq.Question}");
}
```

---

### SearchFaqAsync
用于搜索服务台知识库。

**函数签名**
```csharp
Task<FeishuApiResult<SearchFaqResult>?> SearchFaqAsync(
    [Query] string query,
    [Query] string? base64 = null,
    [Query] string? page_token = null,
    [Query] int? page_size = Consts.PageSize_20,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| query | string | ✅ | 搜索词。如果搜索内容不是英文，有 2 种编码策略：1. URL 编码；2. base64 编码并传入 base64=true 参数 | wifi |
| base64 | string? | ⚪ | 是否转 base64。填 true 表示是。留空表示否。中文需要转 base64 | 5bel5Y2V |
| page_token | string? | ⚪ | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果 | 6936004780707807251 |
| page_size | int? | ⚪ | 分页大小，最大值为 100，默认 20 | 10 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "has_more": false,
    "page_token": "6936004780707807251",
    "page_size": 10,
    "total": 1,
    "items": [
      {
        "faq_id": "6936004780707807231",
        "question": "wifi 怎么连？",
        "answer": "请连接 Feishu-Guest 网络"
      }
    ]
  }
}
```

**说明**
- `query` 为必填搜索词
- 中文等非英文搜索内容需转 base64 并同时传入 `base64=true`，否则可仅做 URL 编码
- 响应中的 `page_size` 与 `total` 字段在官方「Response body」表格未列出，但接口文档的响应示例中包含

**代码示例**
```csharp
var faqApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskFaq>();
var result = await faqApi.SearchFaqAsync("wifi");
foreach (var faq in result?.Data?.Items ?? Array.Empty<Faq>())
{
    Console.WriteLine($"{faq.Question} => {faq.Answer}");
}
```

---

### GetFaqAsync
用于获取服务台知识库FAQ详情。

**函数签名**
```csharp
Task<FeishuApiResult<GetFaqResult>?> GetFaqAsync(
    [Path] string id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| id | string | ✅ | 知识库 FAQ ID | 6856395634652479491 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "faq": {
      "faq_id": "6856395634652479491",
      "id": "6856395634652479491",
      "helpdesk_id": "6936004780707807251",
      "question": "Question",
      "answer": "Answer",
      "answer_richtext": [{ "content": "Answer", "type": "text" }],
      "content": "Answer",
      "type": "text",
      "create_time": 1596379008,
      "update_time": 1596379008,
      "expire_time": 1596379008,
      "categories": [],
      "tags": ["Similar questions"],
      "update_user": { "id": "ou_37019b7c830210acd88fdce886e25c71", "name": "abc" },
      "create_user": { "id": "ou_37019b7c830210acd88fdce886e25c71", "name": "abc" }
    }
  }
}
```

**说明**
- `answer` 与 `answer_richtext` 二者填其一
- `expire_time` 为失效时间，`tags` 为相似问题列表

**代码示例**
```csharp
var faqApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskFaq>();
var result = await faqApi.GetFaqAsync("6856395634652479491");
Console.WriteLine($"问题: {result?.Data?.Faq?.Question}");
Console.WriteLine($"答案: {result?.Data?.Faq?.Answer}");
```

---

### GetFaqImageAsync
用于获取服务台知识库FAQ图片，返回文件二进制流。

**函数签名**
```csharp
Task<byte[]?> GetFaqImageAsync(
    [Path] string id,
    [Path] string image_key,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| id | string | ✅ | 知识库 FAQ ID | 12345 |
| image_key | string | ✅ | 图片 key | img_b07ffac0-19c1-48a3-afca-599f8ea825fj |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
(binary image content)
```

**说明**
- 成功时返回响应的二进制内容（取自 `HttpContent.ReadAsByteArrayAsync`，不会为 `null`；空响应体对应空数组）
- 服务端返回非 2xx 状态码时抛出 `ApiException`（携带 `StatusCode` 与响应内容）
- 飞书部分业务错误以 HTTP 200 + JSON 错误体（`{"code":...,"msg":...}`）返回，此时本方法会把错误 JSON 当作文件内容返回；落盘前应按 `Content-Type` 自检，详见 `documents/ErrorHandling.md`

**代码示例**
```csharp
var faqApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskFaq>();
var bytes = await faqApi.GetFaqImageAsync(
    "12345", "img_b07ffac0-19c1-48a3-afca-599f8ea825fj");
if (bytes is { Length: > 0 })
{
    await File.WriteAllBytesAsync("faq-image.png", bytes);
}
```
