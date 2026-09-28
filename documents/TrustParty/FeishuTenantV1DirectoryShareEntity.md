---
title: 共享成员范围接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份查询本组织与对方关联组织之间双向共享的部门、用户组与成员范围，为配置可搜可见规则选取主客体实体提供依据。
---

# 共享成员范围 - 租户令牌（FeishuTenantV1DirectoryShareEntity）

## 接口名称

**共享成员范围（租户令牌）** -（`IFeishuTenantV1DirectoryShareEntity`）

## 功能描述

提供以租户身份查询飞书共享成员范围的能力。飞书共享成员范围（directory/v1/share_entities）SDK 用于查询本组织与对方关联组织之间双向共享的部门、用户组与成员范围，为配置可搜可见规则时选取主客体实体提供依据。本接口以 tenant_access_token 身份调用，仅支持自建应用，调用者需具备关联组织管理员权限。支持获取关联组织双方共享成员范围操作。

## 参考文档

- [共享成员范围 - 飞书开放平台](https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/list-3)

## 函数列表

| 函数名称                 | 功能描述                     | 认证方式 | HTTP 方法 |
| ------------------------ | ---------------------------- | -------- | --------- |
| GetShareEntityListAsync  | 获取关联组织双方共享成员范围 | 租户令牌 | GET       |

## 函数详细内容

### 获取关联组织双方共享成员范围

分页查询与对方关联组织之间共享的部门、用户组与成员列表（查询参数采用查询对象模式 `ShareEntityListQuery`，见 AGENTS.md API-2）。仅支持自建应用。限频：100 次/分钟。所需权限：trust_party:collaboration_rule:read（读取关联组织协作规则）；同时需要关联组织管理员权限。

**函数签名**：

```csharp
Task<FeishuApiResult<GetShareEntityListResult>?> GetShareEntityListAsync(
    [Query] ShareEntityListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型                    | 必填 | 说明                                                                                          |
| ------- | ----------------------- | ---- | --------------------------------------------------------------------------------------------- |
| `query` | `ShareEntityListQuery?` | ⚪   | 对方组织 tenant key（必填）、部门/用户组 ID、是否查主体侧及分页等查询参数，该对象会整体展开为查询参数 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "page_token": "next_page_token",
    "has_more": false,
    "share_departments": [
      {
        "open_department_id": "od-4e6ac4d14bcd5071a37a39de902c7141",
        "name": {
          "locale": "zh_cn",
          "value": "技术部"
        }
      }
    ],
    "share_groups": [
      {
        "open_group_id": "og-4e6ac4d14bcd5071a37a39de902c7141",
        "name": {
          "locale": "zh_cn",
          "value": "项目协同组"
        }
      }
    ],
    "share_users": [
      {
        "open_user_id": "ou-4e6ac4d14bcd5071a37a39de902c7141",
        "name": {
          "locale": "zh_cn",
          "value": "张三"
        },
        "avatar": {
          "avatar_72": "https://example.feishu.cn/avatar/72",
          "avatar_240": "https://example.feishu.cn/avatar/240",
          "avatar_640": "https://example.feishu.cn/avatar/640",
          "avatar_origin": "https://example.feishu.cn/avatar/origin"
        }
      }
    ]
  }
}
```

**说明**：`query` 对象会整体展开为查询参数，其中 `target_tenant_key` 为必填项，`target_department_id` 不填时查询整个组织分享范围，填 0 时若为全员分享则展示一级部门、否则展示分享的部门与成员；`target_group_id` 填写后忽略 `target_department_id`；`is_select_subject` 用于切换查询主体组织分享范围（默认查客体组织）。当 `has_more` 为 true 时，可使用返回的 `page_token` 继续拉取下一页数据。
