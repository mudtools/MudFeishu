# 搜索文档 - 租户令牌（FeishuTenantV2SearchDocWiki）

## 接口名称

**搜索文档（租户令牌）** -（`IFeishuTenantV2SearchDocWiki`）

## 功能描述

提供以租户身份搜索飞书云文档的能力。飞书 AI 文档搜索根据搜索关键词（query）对当前用户可见的云文档进行搜索。支持搜索文档操作。

## 参考文档

- [文档搜索 - 飞书开放平台](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/search-v2/doc_wiki/search)

## 函数列表

| 函数名称             | 功能描述 | 认证方式 | HTTP 方法 |
| -------------------- | -------- | -------- | --------- |
| SearchDocWikiAsync   | 搜索文档 | 租户令牌 | POST      |

## 函数详细内容

### 搜索文档

用于根据搜索关键词（query）对当前用户可见的云文档进行搜索。

**函数签名**：

```csharp
Task<FeishuApiResult<SearchDocWikiResult>?> SearchDocWikiAsync(
    [Body] SearchDocWikiRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                   | 必填 | 说明             |
| --------- | ---------------------- | ---- | ---------------- |
| `request` | `SearchDocWikiRequest` | ✅   | 搜索文档请求体   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "total": 100,
    "has_more": false,
    "page_token": "next_page_token",
    "res_units": [
      {
        "title_highlighted": "<h>飞书文档</h>使用指南",
        "summary_highlighted": "本文介绍<h>飞书文档</h>的创建、编辑与分享功能",
        "entity_type": "DOC",
        "result_meta": {
          "doc_types": "SHORTCUT",
          "update_time": 1766567446,
          "url": "https://www.feishu.cn/docs/dox-1234567890abcdef",
          "owner_name": "张三",
          "owner_id": "ou-7890123456abcdef",
          "token": "dox_9876543210fedcba"
        }
      }
    ]
  }
}
```

**说明**：搜索结果按相关性返回，`res_units` 中的标题与摘要已包含 `<h>` 高亮标记。`entity_type` 为 `DOC` 表示 doc 实体，为 `WIKI` 表示 wiki 类型；当 `has_more` 为 true 时，可使用返回的 `page_token` 继续拉取下一页结果。
