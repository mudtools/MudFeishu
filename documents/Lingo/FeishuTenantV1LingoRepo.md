---
title: 词库接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份（应用身份）获取飞书词典的词库列表。
---

# 词库 - 租户令牌（FeishuTenantV1LingoRepo）

## 接口名称

**词库（租户令牌）** -（`IFeishuTenantV1LingoRepo`）

## 功能描述

提供以应用身份获取飞书词库列表的能力。飞书词典（Lingo）词库入口域租户态 SDK 是一组服务端 OpenAPI 的封装，用于以应用身份获取飞书词典的词库列表。本接口全部端点支持 tenant_access_token 调用。支持获取词库列表操作。

## 参考文档

- [飞书词典概述 - 飞书开放平台](https://open.feishu.cn/document/lingo-v1/overview)

## 函数列表

| 函数名称          | 功能描述     | 认证方式 | HTTP 方法 | 接口文档 |
| ----------------- | ------------ | -------- | --------- |----------|
| GetRepoListAsync  | 获取词库列表 | 租户令牌 | GET       | [GetRepoListAsync](https://open.feishu.cn/document/lingo-v1/repo/list) |

## 函数详细内容

### 获取词库列表

获取当前访问主体有权限查看的飞书词库列表，包含词库 ID（repo_id）、词库名（name）与词库类型（type，1 为全员词库、2 为个人词库、3 为自建词库）。注意：仅当应用或用户添加了对应词库后，才能查询到该词库，如需查询当前租户的全部词库，请使用飞书管理员账号创建的自建应用。限频：1000 次/分钟、50 次/秒。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。支持的应用类型：自建应用。

**函数签名**：

```csharp
Task<FeishuApiResult<GetRepoListResult>?> GetRepoListAsync(
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明                                             |
| ------ | ---- | ---- | ------------------------------------------------ |
| 无     | —    | —    | 该接口无需业务参数，仅需 `CancellationToken`     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "id": "71527909***274113",
        "name": "全员词库"
      }
    ]
  }
}
```

**说明**：仅返回当前应用已添加的词库，未添加的词库不会出现在列表中；如需查询租户的全部词库，请使用飞书管理员账号创建的自建应用。
