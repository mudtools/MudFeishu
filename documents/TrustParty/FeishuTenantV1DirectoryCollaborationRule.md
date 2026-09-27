# 可搜可见规则 - 租户令牌（FeishuTenantV1DirectoryCollaborationRule）

## 接口名称

**可搜可见规则（租户令牌）** -（`IFeishuTenantV1DirectoryCollaborationRule`）

## 功能描述

提供以租户身份管理飞书可搜可见规则的能力。飞书可搜可见规则（directory/v1/collaboration_rules）SDK 用于管理关联组织间的协作规则，控制双方组织内哪些主体（人员/部门/用户组）可以搜到并看见对方组织内的哪些客体，规则主客体实体数量之和需小于 100。本接口以 tenant_access_token 身份调用，仅支持自建应用，调用者需具备关联组织管理员权限。支持查询、新增、更新、删除规则等操作。

## 参考文档

- [可搜可见规则 - 飞书开放平台](https://open.feishu.cn/document/trust_party-v1/searchable-and-visible-rules/list)

## 函数列表

| 函数名称                      | 功能描述         | 认证方式 | HTTP 方法 |
| ----------------------------- | ---------------- | -------- | --------- |
| GetCollaborationRuleListAsync | 查询可搜可见规则 | 租户令牌 | GET       |
| CreateCollaborationRuleAsync  | 新增可搜可见规则 | 租户令牌 | POST      |
| UpdateCollaborationRuleAsync  | 更新可搜可见规则 | 租户令牌 | PUT       |
| DeleteCollaborationRuleAsync  | 删除可搜可见规则 | 租户令牌 | DELETE    |

## 函数详细内容

### 查询可搜可见规则

分页查询与对方组织之间的可搜可见规则列表，返回规则 ID、主体、客体及主客体是否在分享范围内（超出分享范围时为 false 且对应实体不返回）。所需权限：trust_party:collaboration_rule:read（读取关联组织协作规则）；同时需要关联组织管理员权限。

**函数签名**：

```csharp
Task<FeishuApiResult<GetCollaborationRuleListResult>?> GetCollaborationRuleListAsync(
    [Query("target_tenant_key")] string target_tenant_key,
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名              | 类型     | 必填 | 说明                                                                                                                                    |
| ------------------- | -------- | ---- | --------------------------------------------------------------------------------------------------------------------------------------- |
| `target_tenant_key` | `string` | ✅   | 对方组织的 tenant key，可通过管理员获取所有关联组织列表接口获取，示例值：`test_key`                                                     |
| `page_size`         | `int?`   | ⚪   | 分页大小，取值 0~100，默认 100                                                                                                          |
| `page_token`        | `string?` | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "rule_id": "12121",
        "subjects": {
          "open_user_ids": ["ou_7dab8a3d3cdcc9da365777c7ad53uew2"],
          "open_department_ids": ["0"],
          "open_group_ids": ["g-123456"]
        },
        "subject_is_valid": true,
        "objects": {
          "open_user_ids": ["ou_3d5b8a3d3cdcc9da365777c7ad53abcd"]
        },
        "object_is_valid": true
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**：主体（subjects）取自我方通讯录实体，客体（objects）为对方组织内实体。当主客体超出分享范围时，对应的 `subject_is_valid` / `object_is_valid` 为 false，且该实体不返回。当 `has_more` 为 true 时，可使用返回的 `page_token` 继续拉取下一页数据。

---

### 新增可搜可见规则

为本组织与对方组织之间新增一条可搜可见规则，主体取自我方通讯录实体，客体为对方组织内实体（可通过获取共享成员范围及关联组织部门/成员信息接口获取）。仅支持自建应用。限频：100 次/分钟。所需权限：trust_party:collaboration_rule:write（变更关联组织协作规则）；同时需要关联组织管理员权限。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateCollaborationRuleResult>?> CreateCollaborationRuleAsync(
    [Body] CreateCollaborationRuleRequest request,
    [Query("target_tenant_key")] string target_tenant_key,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名              | 类型                             | 必填 | 说明                                                                                     |
| ------------------- | -------------------------------- | ---- | ---------------------------------------------------------------------------------------- |
| `request`           | `CreateCollaborationRuleRequest` | ✅   | 新增规则请求体（subjects 主体与 objects 客体必填，主客体实体数量之和需小于100）           |
| `target_tenant_key` | `string`                         | ✅   | 对方组织的 tenant key，可通过管理员获取所有关联组织列表接口获取                           |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "add_rule_id": "12121"
  }
}
```

**说明**：新增规则时需保证主客体实体数量之和小于 100，返回的 `add_rule_id` 可用于后续的更新与删除操作。

---

### 更新可搜可见规则

按规则 ID 更新可搜可见规则的主客体实体。仅支持自建应用。限频：100 次/分钟。所需权限：trust_party:collaboration_rule:write（变更关联组织协作规则）；同时需要关联组织管理员权限。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UpdateCollaborationRuleAsync(
    [Path] string collaboration_rule_id,
    [Body] UpdateCollaborationRuleRequest request,
    [Query("target_tenant_key")] string target_tenant_key,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                   | 类型                             | 必填 | 说明                                                                           |
| ------------------------ | -------------------------------- | ---- | ------------------------------------------------------------------------------ |
| `collaboration_rule_id`  | `string`                         | ✅   | 规则 ID，通过查询可搜可见规则接口获取，示例值：`12121`                         |
| `request`                | `UpdateCollaborationRuleRequest` | ✅   | 更新规则请求体（subjects 主体与 objects 客体必填，主客体实体数量之和需小于100） |
| `target_tenant_key`      | `string`                         | ✅   | 对方组织的 tenant key，可通过管理员获取所有关联组织列表接口获取                 |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：更新为全量覆盖语义，请求体中给出的主客体实体集合将替换原有配置。

---

### 删除可搜可见规则

按规则 ID 删除与对方组织之间的一条可搜可见规则。仅支持自建应用。所需权限：trust_party:collaboration_rule:write（变更关联组织协作规则）；同时需要关联组织管理员权限。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteCollaborationRuleAsync(
    [Path] string collaboration_rule_id,
    [Query("target_tenant_key")] string target_tenant_key,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                  | 类型     | 必填 | 说明                                                           |
| ----------------------- | -------- | ---- | -------------------------------------------------------------- |
| `collaboration_rule_id` | `string` | ✅   | 规则 ID，通过查询可搜可见规则接口获取                           |
| `target_tenant_key`     | `string` | ✅   | 对方组织的 tenant key，可通过管理员获取所有关联组织列表接口获取 |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除规则后，双方组织内相关主体的可搜可见关系立即失效，请谨慎操作。
