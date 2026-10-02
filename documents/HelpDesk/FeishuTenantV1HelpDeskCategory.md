---
title: 服务台工单分类接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份查询飞书服务台知识库分类，支持获取全部知识库分类与查询单个知识库分类。
---

# IFeishuTenantV1HelpDeskCategory - 租户知识库分类API

## 功能描述
飞书服务台知识库分类API是开放平台基于飞书服务台知识库的分类功能开放的查看/创建/修改/删除等API，开发者可以基于这些API对服务台知识库分类进行操作。本接口使用租户访问令牌（TenantAccessToken）鉴权；同时继承自 `IFeishuV1HelpDeskCategory` 的 `HelpdeskTokenAndId` 属性用于在服务台请求 Header 中添加 `X-Lark-Helpdesk-Authorization` 参数（Value 为 `base64(helpdesk_id:helpdesk_token)`，即通过 base64 加密将 helpdesk_id 和 helpdesk_token 用 `:` 连接而成的字符串）。

## 参考文档
- [获取全部知识库分类](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/list-categories)
- [获取知识库分类](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/get)
- [服务台接入指南](https://open.feishu.cn/document/server-docs/helpdesk-v1/access-guide)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 | 接口文档 |
| :--- | :--- | :--- | :--- |----------|
| GetCategoryListAsync | 获取全部知识库分类 | TenantAccessToken | GET | [GetCategoryListAsync](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/list-categories) |
| GetCategoryAsync | 获取知识库分类 | TenantAccessToken | GET | [GetCategoryAsync](https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/get) |

## 函数详细内容

### GetCategoryListAsync
用于获取服务台知识库所有分类。

**函数签名**
```csharp
Task<FeishuApiResult<GetCategoryListResult>?> GetCategoryListAsync(
    [Query] string? lang = null,
    [Query] int? order_by = null,
    [Query] bool? asc = null,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| lang | string? | ⚪ | 知识库分类语言 | zh_cn |
| order_by | int? | ⚪ | 排序 key。1：按知识库分类修改时间排序 | 1 |
| asc | bool? | ⚪ | 顺序。true：升序；false：降序 | true |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "categories": [
      {
        "category_id": "6948728206392295444",
        "id": "6948728206392295444",
        "name": "Create a team and invite members",
        "parent_id": "0",
        "helpdesk_id": "6939771743531696147",
        "language": "zh_cn",
        "children": []
      }
    ]
  }
}
```

**说明**
- 返回的分类为树形结构，`children` 为子分类详情
- `category_id` 为推荐使用的分类 ID，`id` 为旧版本字段

**代码示例**
```csharp
var categoryApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskCategory>();
categoryApi.HelpdeskTokenAndId = Convert.ToBase64String(
    Encoding.UTF8.GetBytes($"{helpdeskId}:{helpdeskToken}"));

var result = await categoryApi.GetCategoryListAsync(lang: "zh_cn", order_by: 1, asc: true);
foreach (var category in result?.Data?.Categories ?? Array.Empty<Category>())
{
    Console.WriteLine($"分类: {category.Name}（{category.CategoryId}）");
}
```

---

### GetCategoryAsync
用于获取单个服务台知识库分类。

**函数签名**
```csharp
Task<FeishuApiResult<GetCategoryResult>?> GetCategoryAsync(
    [Path] string id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

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
  "data": {
    "category_id": "6948728206392295444",
    "id": "6948728206392295444",
    "name": "Create a team and invite members",
    "helpdesk_id": "6939771743531696147",
    "language": "zh_cn"
  }
}
```

**说明**
- 单个分类响应不包含 `parent_id` 与 `children`，仅返回分类基本信息
- `id` 为旧版本字段，建议使用 `category_id`

**代码示例**
```csharp
var categoryApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskCategory>();
var result = await categoryApi.GetCategoryAsync("6948728206392295444");
Console.WriteLine($"分类名称: {result?.Data?.Name}，语言: {result?.Data?.Language}");
```
