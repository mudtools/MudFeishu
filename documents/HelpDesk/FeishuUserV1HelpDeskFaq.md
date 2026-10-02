---
title: 服务台常见问题接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份管理飞书服务台知识库 FAQ，支持创建、修改与删除知识库 FAQ。
---

# IFeishuUserV1HelpDeskFaq - 用户知识库FAQAPI

## 功能描述
飞书服务台知识库FAQ API是开放平台基于飞书服务台知识库的常见问题功能开放的查看/创建/修改/删除等API，开发者可以基于这些API对服务台知识库FAQ进行操作。本接口使用用户访问令牌（UserAccessToken）鉴权，并额外实现 `ICurrentUserId`；同时继承自 `IFeishuV1HelpDeskFaq` 的 `HelpdeskTokenAndId` 属性用于在服务台请求 Header 中添加 `X-Lark-Helpdesk-Authorization` 参数（Value 为 `base64(helpdesk_id:helpdesk_token)`，即通过 base64 加密将 helpdesk_id 和 helpdesk_token 用 `:` 连接而成的字符串）。

## 参考文档
- [创建知识库FAQ](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/create)
- [删除知识库FAQ](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/delete)
- [修改知识库FAQ](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/patch)
- [查询知识库FAQ列表](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/list)
- [服务台接入指南](https://open.feishu.cn/document/server-docs/helpdesk-v1/access-guide)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 | 接口文档 |
| :--- | :--- | :--- | :--- |----------|
| CreateFaqAsync | 创建知识库FAQ | UserAccessToken | POST | [CreateFaqAsync](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/create) |
| DeleteFaqAsync | 删除知识库FAQ | UserAccessToken | DELETE | [DeleteFaqAsync](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/delete) |
| UpdateFaqAsync | 修改知识库FAQ | UserAccessToken | PATCH | [UpdateFaqAsync](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/faq/patch) |

## 函数详细内容

### CreateFaqAsync
用于创建服务台知识库FAQ。

**函数签名**
```csharp
Task<FeishuApiResult<CreateFaqResult>?> CreateFaqAsync(
    [Body] CreateFaqRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | CreateFaqRequest | ✅ | 创建知识库FAQ请求体（`faq` 为 FAQ 详情对象） | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "faq": {
      "faq_id": "6936004780707807231",
      "id": "6936004780707807231",
      "helpdesk_id": "6936004780707807251",
      "question": "Question",
      "answer": "Answer",
      "categories": [{ "category_id": "6836004780707807251", "name": "分类名称" }],
      "tags": ["Similar questions"],
      "create_time": 1596379008,
      "update_time": 1596379008
    }
  }
}
```

**说明**
- user_access_token 访问时，需要操作者是当前服务台的客服、管理员或所有者
- `faq.question` 必填；`faq.answer` 与 `faq.answer_richtext` 二者填其一
- `faq.answer_richtext` 为 Json Array 形式的富文本，示例值未转义，使用时请注意转义

**代码示例**
```csharp
var faqApi = feishuApp.GetApi<IFeishuUserV1HelpDeskFaq>();
faqApi.HelpdeskTokenAndId = Convert.ToBase64String(
    Encoding.UTF8.GetBytes($"{helpdeskId}:{helpdeskToken}"));

var request = new CreateFaqRequest
{
    Faq = new FaqCreateInfo
    {
        CategoryId = "6836004780707807251",
        Question = "Question",
        Answer = "Answer",
        Tags = new[] { "Similar questions" }
    }
};
var result = await faqApi.CreateFaqAsync(request);
Console.WriteLine($"FAQ ID: {result?.Data?.Faq?.FaqId}");
```

---

### DeleteFaqAsync
用于删除服务台知识库FAQ。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteFaqAsync(
    [Path] string id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| id | string | ✅ | 知识库 FAQ ID | 12345 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- user_access_token 访问时，需要操作者是当前服务台的客服、管理员或所有者
- 该接口无请求体与查询参数

**代码示例**
```csharp
var faqApi = feishuApp.GetApi<IFeishuUserV1HelpDeskFaq>();
var result = await faqApi.DeleteFaqAsync("12345");
Console.WriteLine($"删除结果: {result?.Code == 0}");
```

---

### UpdateFaqAsync
用于修改服务台知识库FAQ。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateFaqAsync(
    [Path] string id,
    [Body] UpdateFaqRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| id | string | ✅ | 知识库 FAQ ID | 6856395634652479491 |
| request | UpdateFaqRequest | ✅ | 修改知识库FAQ请求体（`faq` 为修改的 FAQ 内容） | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- user_access_token 访问时，需要操作者是当前服务台的客服、管理员或所有者
- `faq.question` 必填；`faq.answer` 与 `faq.answer_richtext` 二者填其一
- `faq.type` 可选值：text、hyperlink、img、line break

**代码示例**
```csharp
var faqApi = feishuApp.GetApi<IFeishuUserV1HelpDeskFaq>();
var request = new UpdateFaqRequest
{
    Faq = new FaqUpdateInfo
    {
        CategoryId = "6836004780707807251",
        Question = "Question",
        Answer = "Answer",
        Type = "text",
        Tags = new[] { "Similar question" }
    }
};
var result = await faqApi.UpdateFaqAsync("6856395634652479491", request);
Console.WriteLine($"修改结果: {result?.Code == 0}");
```
