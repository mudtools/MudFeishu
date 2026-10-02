---
title: 数据驻留与用户迁移接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份管理飞书数据驻留与用户迁移，支持获取可用地理位置列表、迁移用户数据驻留位置及查询/取消迁移状态。
---

# 数据驻留与用户迁移 - 租户令牌（FeishuTenantV1SecurityUserMigration）

## 接口名称

**数据驻留与用户迁移（租户令牌）** -（`IFeishuTenantV1SecurityUserMigration`）

## 功能描述

提供以租户身份管理飞书数据驻留与用户迁移的能力。飞书安全与合规（Security）「数据驻留与用户迁移」SDK 是一组服务端 OpenAPI 的封装，用于获取租户可用的数据驻留地理位置列表、迁移用户数据驻留位置、查询单个/批量用户迁移状态以及取消用户迁移任务。本接口全部端点为 security_and_compliance/v1，仅支持 tenant_access_token 调用。支持获取数据驻留地理位置列表、迁移用户数据驻留位置、获取单个用户迁移状态、批量获取用户迁移状态、取消用户迁移任务等操作。

## 参考文档

- [迁移用户数据驻留位置 - 飞书开放平台](https://open.feishu.cn/document/server-docs/security_and_compliance-v1/user_migration/create)

## 函数列表

| 函数名称                      | 功能描述                 | 认证方式 | HTTP 方法 | 接口文档 |
| ----------------------------- | ------------------------ | -------- | --------- |----------|
| GetMultiGeoEntityTenantAsync  | 获取数据驻留地理位置列表 | 租户令牌 | GET       | [GetMultiGeoEntityTenantAsync](https://open.feishu.cn/document/server-docs/security_and_compliance-v1/user_migration/get-2) |
| CreateUserMigrationAsync      | 迁移用户数据驻留位置     | 租户令牌 | POST      | [CreateUserMigrationAsync](https://open.feishu.cn/document/server-docs/security_and_compliance-v1/user_migration/create) |
| GetUserMigrationAsync         | 获取单个用户迁移状态     | 租户令牌 | GET       | [GetUserMigrationAsync](https://open.feishu.cn/document/server-docs/security_and_compliance-v1/user_migration/get) |
| SearchUserMigrationsAsync     | 批量获取用户迁移状态     | 租户令牌 | POST      | [SearchUserMigrationsAsync](https://open.feishu.cn/document/server-docs/security_and_compliance-v1/user_migration/search) |
| CancelUserMigrationAsync      | 取消用户迁移任务         | 租户令牌 | POST      | [CancelUserMigrationAsync](https://open.feishu.cn/document/server-docs/security_and_compliance-v1/user_migration/cancel) |

## 函数详细内容

### 获取数据驻留地理位置列表

获取租户可用的数据驻留地理位置列表。限频：100 次/分钟。所需权限（开启任一即可）：security_and_compliance:multi_geo_entity.tenant:readonly（查看数据驻留租户信息）、security_and_compliance:user_migration:multi-geo（查询、更新员工的数据驻留地）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetMultiGeoEntityTenantResult>?> GetMultiGeoEntityTenantAsync(
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明                                         |
| ------ | ---- | ---- | -------------------------------------------- |
| 无     | —    | —    | 该接口无需业务参数，仅需 `CancellationToken` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "tenant": {
      "available_geo_locations": ["sg"]
    }
  }
}
```

**说明**：返回的 `available_geo_locations` 可作为迁移用户数据驻留位置时 `dest_geo` 的取值依据。

---

### 迁移用户数据驻留位置

将用户的数据驻留位置迁移到目标地理位置，一次最多迁移 100 个用户；用户已在目标地理位置或正在迁移中时会返回对应错误码。限频：10 次/分钟。所需权限（开启任一即可）：security_and_compliance:user_migration（创建、更新用户数据迁移）、security_and_compliance:user_migration:multi-geo（查询、更新员工的数据驻留地）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateUserMigrationResult>?> CreateUserMigrationAsync(
    [Body] CreateUserMigrationRequest request,
    [Query("user_id_type")] string user_id_type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                        | 必填 | 说明                                                                                      |
| -------------- | --------------------------- | ---- | ----------------------------------------------------------------------------------------- |
| `request`      | `CreateUserMigrationRequest` | ✅   | 请求体（user_ids 迁移用户 ID 列表必填 1~100 个；dest_geo 迁移目标地理位置区域必填，长度 2~10 字符） |
| `user_id_type` | `string`                    | ✅   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id（必填）                             |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user_migrations": [
      {
        "user_id": "ou_1234567890abcdef1234567890abcdef",
        "dest_geo": "us",
        "task_id": "7088763625288187923",
        "status": 1,
        "progress": 0
      }
    ]
  }
}
```

**说明**：一次最多迁移 100 个用户。用户已在目标地理位置或正在迁移中时会返回对应错误码；迁移进度可通过获取单个用户迁移状态接口查询。

---

### 获取单个用户迁移状态

通过 user_id 获取指定用户当前的迁移状态。限频：100 次/分钟。所需权限（开启任一即可）：security_and_compliance:user_migration、security_and_compliance:user_migration:multi-geo、security_and_compliance:user_migration:readonly（查看用户数据迁移）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetUserMigrationResult>?> GetUserMigrationAsync(
    [Path] string user_id,
    [Query("user_id_type")] string user_id_type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型     | 必填 | 说明                                                          |
| -------------- | -------- | ---- | ------------------------------------------------------------- |
| `user_id`      | `string` | ✅   | 用户 ID，ID 类型必须与 user_id_type 的取值一致                 |
| `user_id_type` | `string` | ✅   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id（必填）  |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user_migration": {
      "user_id": "ou_1234567890abcdef1234567890abcdef",
      "dest_geo": "us",
      "task_id": "7088763625288187923",
      "status": 2,
      "progress": 60
    }
  }
}
```

**说明**：`user_id` 的类型必须与 `user_id_type` 一致，否则将查询不到对应迁移记录。

---

### 批量获取用户迁移状态

传入用户 ID 列表，批量获取用户迁移状态，一次最多 500 个用户。限频：100 次/分钟。所需权限（开启任一即可）：security_and_compliance:user_migration、security_and_compliance:user_migration:multi-geo、security_and_compliance:user_migration:readonly（查看用户数据迁移）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<SearchUserMigrationsResult>?> SearchUserMigrationsAsync(
    [Body] SearchUserMigrationsRequest request,
    [Query("user_id_type")] string user_id_type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                          | 必填 | 说明                                                        |
| -------------- | ----------------------------- | ---- | ----------------------------------------------------------- |
| `request`      | `SearchUserMigrationsRequest` | ✅   | 请求体（user_ids 用户 ID 列表必填 1~500 个）                 |
| `user_id_type` | `string`                      | ✅   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id（必填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "user_id": "ou_1234567890abcdef1234567890abcdef",
        "dest_geo": "us",
        "task_id": "7088763625288187923",
        "status": 2,
        "progress": 60
      }
    ]
  }
}
```

**说明**：一次最多查询 500 个用户的迁移状态；超出请分批调用。

---

### 取消用户迁移任务

取消用户迁移任务，仅能对未启动迁移的用户做此操作；用户迁移状态可通过获取单个用户迁移状态接口查询。限频：10 次/分钟。所需权限（开启任一即可）：security_and_compliance:user_migration（创建、更新用户数据迁移）、security_and_compliance:user_migration:multi-geo（查询、更新员工的数据驻留地）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> CancelUserMigrationAsync(
    [Body] CancelUserMigrationRequest request,
    [Query("user_id_type")] string user_id_type,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                         | 必填 | 说明                                                        |
| -------------- | ---------------------------- | ---- | ----------------------------------------------------------- |
| `request`      | `CancelUserMigrationRequest` | ✅   | 请求体（user_ids 取消迁移用户 ID 列表必填 1~100 个）         |
| `user_id_type` | `string`                     | ✅   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id（必填） |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：仅未启动迁移的用户可被取消，已启动的迁移任务无法通过本接口终止；请先通过获取单个用户迁移状态接口确认状态。
