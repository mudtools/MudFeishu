---
title: 关联组织管理端接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份查询本租户所有已建联的关联组织（返回名称与简称），为创建可搜可见规则提供有效的 tenant key。
---

# 关联组织管理端 - 用户令牌（FeishuUserV1DirectoryCollaborationTenant）

## 接口名称

**关联组织管理端（用户令牌）** -（`IFeishuUserV1DirectoryCollaborationTenant`）

## 功能描述

提供以用户身份查询飞书关联组织的能力。飞书关联组织管理端（directory/v1）SDK 用于管理员视角查询本租户所有已建联的关联组织（返回 i18n_text 结构的名称与简称），为创建可搜可见规则等管理操作提供有效的 tenant key。本接口以 user_access_token 身份调用，仅支持自建应用，调用者需具备关联组织管理员权限。支持管理员获取所有关联组织列表操作。

## 参考文档

- [关联组织管理端 - 飞书开放平台](https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/list-2)

## 函数列表

| 函数名称                           | 功能描述                 | 认证方式 | HTTP 方法 | 接口文档 |
| ---------------------------------- | ------------------------ | -------- | --------- |----------|
| GetAllCollaborationTenantListAsync | 管理员获取所有关联组织列表 | 用户令牌 | GET       | [GetAllCollaborationTenantListAsync](https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/list-2) |

## 函数详细内容

### 管理员获取所有关联组织列表

分页获取本租户管理员视角下所有已建联的关联组织列表（tenant key、建联时间、头像、品牌、名称与简称），用于创建规则时获取对方组织的有效 tenant key。仅支持自建应用。限频：100 次/分钟。所需权限：trust_party:collaboration_rule:read（读取关联组织协作规则）；同时需要关联组织管理员权限。

**函数签名**：

```csharp
Task<FeishuApiResult<GetAllCollaborationTenantListResult>?> GetAllCollaborationTenantListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                                                                                                    |
| ------------ | --------- | ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| `page_size`  | `int?`    | ⚪   | 分页大小，取值 0~100，默认 100                                                                                                          |
| `page_token` | `string?` | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "tenant_key": "test_key",
        "connect_time": 1627540853,
        "avatar": {
          "avatar_72": "https://example.feishu.cn/avatar/72",
          "avatar_240": "https://example.feishu.cn/avatar/240",
          "avatar_640": "https://example.feishu.cn/avatar/640",
          "avatar_origin": "https://example.feishu.cn/avatar/origin"
        },
        "brand": "feishu",
        "name": {
          "locale": "zh_cn",
          "value": "关联组织名称"
        },
        "short_name": {
          "locale": "zh_cn",
          "value": "关联组织简称"
        }
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**：返回的 `tenant_key` 是创建可搜可见规则时 `target_tenant_key` 的取值来源。以用户令牌调用时，需确保当前用户具备关联组织管理员权限。
