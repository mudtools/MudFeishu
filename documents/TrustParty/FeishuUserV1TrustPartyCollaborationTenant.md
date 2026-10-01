---
title: 关联组织接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份查询飞书关联组织（trust_party/v1）的协作关系，包括可见关联组织列表与详情、组织内可见的部门/成员/用户组信息及部门、成员详情。
---

# 关联组织 - 用户令牌（FeishuUserV1TrustPartyCollaborationTenant）

## 接口名称

**关联组织（用户令牌）** -（`IFeishuUserV1TrustPartyCollaborationTenant`）

## 功能描述

提供以用户身份查询飞书关联组织的能力。飞书关联组织（trust_party/v1）SDK 是一组服务端 OpenAPI 的封装，用于查询本组织与对方关联组织（协作组织）之间的协作关系，包括可见关联组织列表、关联组织详情、组织内可见的部门/成员/用户组信息以及部门、成员详情。本接口以 user_access_token 身份调用，可见性按 admin 后台对用户设置的可见性规则校验。支持获取可见关联组织的列表、获取关联组织详情、获取关联组织的成员信息、获取关联组织部门详情、获取关联组织成员详情等操作。

## 参考文档

- [关联组织 - 飞书开放平台](https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/list)

## 函数列表

| 函数名称                        | 功能描述                 | 认证方式 | HTTP 方法 | 接口文档 |
| ------------------------------- | ------------------------ | -------- | --------- |----------|
| GetCollaborationTenantListAsync | 获取可见关联组织的列表   | 用户令牌 | GET       | [GetCollaborationTenantListAsync](https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/list) |
| GetCollaborationTenantAsync     | 获取关联组织详情         | 用户令牌 | GET       | [GetCollaborationTenantAsync](https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/get) |
| GetVisibleOrganizationAsync     | 获取关联组织的成员信息   | 用户令牌 | GET       | [GetVisibleOrganizationAsync](https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/visible_organization) |
| GetCollaborationDepartmentAsync | 获取关联组织部门详情     | 用户令牌 | GET       | [GetCollaborationDepartmentAsync](https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/get-2) |
| GetCollaborationUserAsync       | 获取关联组织成员详情     | 用户令牌 | GET       | [GetCollaborationUserAsync](https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/get-3) |

## 函数详细内容

### 获取可见关联组织的列表

分页获取当前用户/应用可见的关联组织（协作组织）列表，返回组织名称、简称、标签、头像、品牌与关联时间等信息。限频：1000 次/分钟、50 次/秒。所需权限：trust_party:collaboration.tenant:readonly（以应用身份读取关联组织）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetCollaborationTenantListResult>?> GetCollaborationTenantListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                                                                                                    |
| ------------ | --------- | ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| `page_size`  | `int?`    | ⚪   | 单次请求的关联组织数量，取值 1~100，默认 10                                                                                             |
| `page_token` | `string?` | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "target_tenant_list": [
      {
        "tenant_key": "4e6ac4d14bcd5071a37a39de902c7141",
        "tenant_name": "关联组织 A",
        "i18n_tenant_name": {
          "zh_cn": "关联组织 A",
          "en_us": "Partner A",
          "ja_jp": "パートナー A"
        },
        "tenant_short_name": "A",
        "connect_time": 1627540853,
        "tenant_tag": "供应商",
        "brand": "feishu",
        "avatar": {
          "avatar_72": "https://example.feishu.cn/avatar/72",
          "avatar_240": "https://example.feishu.cn/avatar/240",
          "avatar_640": "https://example.feishu.cn/avatar/640",
          "avatar_origin": "https://example.feishu.cn/avatar/origin"
        }
      }
    ],
    "has_more": false,
    "page_token": "next_page_token"
  }
}
```

**说明**：返回的 `tenant_key` 可作为后续获取关联组织详情、成员信息的入参。以用户令牌调用时，可见性按 admin 后台对该用户设置的可见性规则校验。

---

### 获取关联组织详情

按对方关联组织的 tenant key 获取组织名称、简称、标签、头像、品牌与关联时间等详情。限频：5 次/秒。所需权限：trust_party:collaboration.tenant:readonly（以应用身份读取关联组织）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetCollaborationTenantResult>?> GetCollaborationTenantAsync(
    [Path] string target_tenant_key,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名              | 类型     | 必填 | 说明                                                                                          |
| ------------------- | -------- | ---- | --------------------------------------------------------------------------------------------- |
| `target_tenant_key` | `string` | ✅   | 对方关联组织的 tenant key，可通过获取可见关联组织的列表接口获取，示例值：`4e6ac4d14bcd5071a37a39de902c7141` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "target_tenant": {
      "tenant_key": "4e6ac4d14bcd5071a37a39de902c7141",
      "tenant_name": "关联组织 A",
      "i18n_tenant_name": {
        "zh_cn": "关联组织 A",
        "en_us": "Partner A",
        "ja_jp": "パートナー A"
      },
      "tenant_short_name": "A",
      "connect_time": 1627540853,
      "tenant_tag": "供应商",
      "brand": "feishu",
      "avatar": {
        "avatar_72": "https://example.feishu.cn/avatar/72",
        "avatar_240": "https://example.feishu.cn/avatar/240",
        "avatar_640": "https://example.feishu.cn/avatar/640",
        "avatar_origin": "https://example.feishu.cn/avatar/origin"
      }
    }
  }
}
```

**说明**：`target_tenant_key` 必须是当前用户可见的关联组织，否则将返回无权限或不存在错误。

---

### 获取关联组织的成员信息

分页获取对方关联组织内指定部门或用户组下可见的部门、用户、用户组实体列表（查询参数采用查询对象模式 `VisibleOrganizationQuery`，见 AGENTS.md API-2）。限频：1000 次/分钟、50 次/秒。所需权限：trust_party:collaboration.tenant:readonly（以应用身份读取关联组织）。`target_department_id` 与 `target_group_id` 二选一。

**函数签名**：

```csharp
Task<FeishuApiResult<GetVisibleOrganizationResult>?> GetVisibleOrganizationAsync(
    [Path] string target_tenant_key,
    [Query] VisibleOrganizationQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名              | 类型                        | 必填 | 说明                                                                                          |
| ------------------- | --------------------------- | ---- | --------------------------------------------------------------------------------------------- |
| `target_tenant_key` | `string`                    | ✅   | 对方关联组织的 tenant key，可通过获取可见关联组织的列表接口获取                                |
| `query`             | `VisibleOrganizationQuery?` | ⚪   | 部门/用户组 ID 类型、目标 ID、分页等查询参数（`target_department_id` 与 `target_group_id` 二选一），该对象会整体展开为查询参数 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "collaboration_entity_list": [
      {
        "collaboration_entity_type": "user",
        "user_id": "4e6ac4d1",
        "open_user_id": "ou-4e6ac4d14bcd5071a37a39de902c7141",
        "union_user_id": "on-4e6ac4d14bcd5071a37a39de902c7141",
        "user_name": "张三",
        "i18n_user_name": {
          "zh_cn": "张三",
          "en_us": "Zhang San",
          "ja_jp": "張三"
        },
        "user_avatar": {
          "avatar_72": "https://example.feishu.cn/avatar/72",
          "avatar_240": "https://example.feishu.cn/avatar/240",
          "avatar_640": "https://example.feishu.cn/avatar/640",
          "avatar_origin": "https://example.feishu.cn/avatar/origin"
        }
      }
    ],
    "has_more": false,
    "page_token": "next_page_token"
  }
}
```

**说明**：`query` 对象会整体展开为查询参数，`target_department_id` 填 0 表示根部门，与 `target_group_id` 二选一；`department_id_type` 与 `group_id_type` 决定所填 ID 的类型。返回列表中通过 `collaboration_entity_type` 区分 `user`、`department`、`group` 三类实体，同一实体仅对应字段有值。

---

### 获取关联组织部门详情

按部门 ID 获取对方关联组织的部门详情（名称、i18n 名称、排序、负责人、父部门）。负责人与父部门需对其有可见性权限才会返回。限频：5 次/秒。所需权限：trust_party:collaboration.tenant:readonly（以应用身份读取关联组织）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetCollaborationDepartmentResult>?> GetCollaborationDepartmentAsync(
    [Path] string target_tenant_key,
    [Path] string target_department_id,
    [Query("target_department_id_type")] string? target_department_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                      | 类型      | 必填 | 说明                                                                                    |
| --------------------------- | --------- | ---- | --------------------------------------------------------------------------------------- |
| `target_tenant_key`         | `string`  | ✅   | 对方关联组织的 tenant key，可通过获取可见关联组织的列表接口获取                          |
| `target_department_id`      | `string`  | ✅   | 对方关联组织的部门 ID，需要与 `target_department_id_type` 中填写的值保持一致             |
| `target_department_id_type` | `string?` | ⚪   | 对方关联组织的入参部门类型：`department_id`（默认，部门 ID）/ `open_department_id`（部门 open ID） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "target_department": {
      "open_department_id": "od-4e6ac4d14bcd5071a37a39de902c7141",
      "department_id": "4e6ac4d1",
      "name": "技术部",
      "i18n_name": {
        "zh_cn": "技术部",
        "en_us": "Technology",
        "ja_jp": "技術部"
      },
      "order": "1",
      "leaders": [],
      "parent_department_id": {
        "department_id": "0",
        "open_department_id": "od-0"
      }
    }
  }
}
```

**说明**：`target_department_id` 的类型必须与 `target_department_id_type` 一致，否则将查询失败。部门负责人（`leaders`）与父部门（`parent_department_id`）仅在对其有可见性权限时返回。

---

### 获取关联组织成员详情

按用户 ID 获取对方关联组织的成员详情（名称、头像、手机号、职务、工号、自定义属性、部门与主管等）。手机号、职务、工号、自定义属性需要对方租户授权展示。限频：5 次/秒。所需权限：trust_party:collaboration.tenant:readonly（以应用身份读取关联组织）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetCollaborationUserResult>?> GetCollaborationUserAsync(
    [Path] string target_tenant_key,
    [Path] string target_user_id,
    [Query("target_user_id_type")] string? target_user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                | 类型      | 必填 | 说明                                                                                                    |
| --------------------- | --------- | ---- | ------------------------------------------------------------------------------------------------------- |
| `target_tenant_key`   | `string`  | ✅   | 对方关联组织的 tenant key，可通过获取可见关联组织的列表接口获取                                          |
| `target_user_id`      | `string`  | ✅   | 请求的关联组织用户 ID，需要与 `target_user_id_type` 中填写的类型保持一致                                 |
| `target_user_id_type` | `string?` | ⚪   | 用户 ID 类型：`user_id`（默认）/ `union_id` / `open_id`，可从获取关联组织的成员信息接口中获取对应的用户 ID |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "target_user": {
      "open_id": "ou-4e6ac4d14bcd5071a37a39de902c7141",
      "user_id": "4e6ac4d1",
      "union_id": "on-4e6ac4d14bcd5071a37a39de902c7141",
      "name": "张三",
      "i18n_name": {
        "zh_cn": "张三",
        "en_us": "Zhang San",
        "ja_jp": "張三"
      },
      "avatar": {
        "avatar_72": "https://example.feishu.cn/avatar/72",
        "avatar_240": "https://example.feishu.cn/avatar/240",
        "avatar_640": "https://example.feishu.cn/avatar/640",
        "avatar_origin": "https://example.feishu.cn/avatar/origin"
      },
      "mobile": "130****1234",
      "job_title": "技术专家",
      "employee_no": "E12345",
      "status": {
        "is_frozen": false,
        "is_resigned": false,
        "is_activated": true
      }
    }
  }
}
```

**说明**：`target_user_id` 的类型必须与 `target_user_id_type` 一致，用户 ID 可从获取关联组织的成员信息接口获得。手机号、职务、工号与自定义属性需对方租户授权后才会展示。
