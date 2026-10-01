---
title: 服务台工单分类接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份管理飞书服务台知识库分类，支持创建、更新与删除知识库分类。
---

# IFeishuUserV1HelpDeskCategory - 用户知识库分类API

## 功能描述
飞书服务台知识库分类API是开放平台基于飞书服务台知识库的分类功能开放的查看/创建/修改/删除等API，开发者可以基于这些API对服务台知识库分类进行操作。本接口使用用户访问令牌（UserAccessToken）鉴权，并额外实现 `ICurrentUserId`；同时继承自 `IFeishuV1HelpDeskCategory` 的 `HelpdeskTokenAndId` 属性用于在服务台请求 Header 中添加 `X-Lark-Helpdesk-Authorization` 参数（Value 为 `base64(helpdesk_id:helpdesk_token)`，即通过 base64 加密将 helpdesk_id 和 helpdesk_token 用 `:` 连接而成的字符串）。

## 参考文档
- [创建知识库分类](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/create)
- [删除知识库分类](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/delete)
- [更新知识库分类](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/patch)
- [获取全部知识库分类](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/list-categories)
- [服务台接入指南](https://open.feishu.cn/document/server-docs/helpdesk-v1/access-guide)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 | 接口文档 |
| :--- | :--- | :--- | :--- |----------|
| CreateCategoryAsync | 创建知识库分类 | UserAccessToken | POST | [CreateCategoryAsync](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/create) |
| DeleteCategoryAsync | 删除知识库分类 | UserAccessToken | DELETE | [DeleteCategoryAsync](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/delete) |
| UpdateCategoryAsync | 更新知识库分类 | UserAccessToken | PATCH | [UpdateCategoryAsync](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/patch) |

## 函数详细内容

### CreateCategoryAsync
用于创建服务台知识库分类。

**函数签名**
```csharp
Task<FeishuApiResult<CreateCategoryResult>?> CreateCategoryAsync(
    [Body] CreateCategoryRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | CreateCategoryRequest | ✅ | 创建知识库分类请求体（`name` 与 `parent_id` 必填，`language` 选填） | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "category": {
      "category_id": "6948728206392295444",
      "id": "6948728206392295444",
      "name": "Create a team and invite members",
      "parent_id": "0",
      "helpdesk_id": "6939771743531696147",
      "language": "zh_cn",
      "children": []
    }
  }
}
```

**说明**
- user_access_token 访问时，需要操作者是当前服务台的客服、管理员或所有者
- `parent_id` 为父知识库分类 ID，创建顶级分类时填 `"0"`

**代码示例**
```csharp
var categoryApi = feishuApp.GetApi<IFeishuUserV1HelpDeskCategory>();
categoryApi.HelpdeskTokenAndId = Convert.ToBase64String(
    Encoding.UTF8.GetBytes($"{helpdeskId}:{helpdeskToken}"));

var request = new CreateCategoryRequest
{
    Name = "Create a team and invite members",
    ParentId = "0",
    Language = "zh_cn"
};
var result = await categoryApi.CreateCategoryAsync(request);
Console.WriteLine($"分类 ID: {result?.Data?.Category?.CategoryId}");
```

---

### DeleteCategoryAsync
用于删除单个服务台知识库分类。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteCategoryAsync(
    [Path] string id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| id | string | ✅ | 知识库分类 ID | 6948728206392295444 |
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
var categoryApi = feishuApp.GetApi<IFeishuUserV1HelpDeskCategory>();
var result = await categoryApi.DeleteCategoryAsync("6948728206392295444");
Console.WriteLine($"删除结果: {result?.Code == 0}");
```

---

### UpdateCategoryAsync
用于更新单个服务台知识库分类。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateCategoryAsync(
    [Path] string id,
    [Body] UpdateCategoryRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| id | string | ✅ | 知识库分类 ID | 6948728206392295444 |
| request | UpdateCategoryRequest | ✅ | 更新知识库分类请求体（`name` 与 `parent_id` 均选填） | - |
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
- `name` 为新名称，`parent_id` 为新的父知识库分类 ID，二者均为选填

**代码示例**
```csharp
var categoryApi = feishuApp.GetApi<IFeishuUserV1HelpDeskCategory>();
var request = new UpdateCategoryRequest
{
    Name = "Create a team and invite members",
    ParentId = "0"
};
var result = await categoryApi.UpdateCategoryAsync("6948728206392295444", request);
Console.WriteLine($"更新结果: {result?.Code == 0}");
```
