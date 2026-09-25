# 飞书妙搭产品使用权限 - 用户令牌（FeishuUserV1SparkAvailableScope）

## 接口名称

**飞书妙搭产品使用权限（用户令牌）** -（`IFeishuUserV1SparkAvailableScope`）

## 功能描述

提供获取与修改妙搭产品企业级使用权限配置的能力（可用范围模式 + 允许/禁止的部门/成员名单）。

## 参考文档

- [获取妙搭产品使用权限](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-available_scope/open_api_get_miaoda_available_scope)
- [修改妙搭产品使用权限](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-available_scope/open_api_update_miaoda_available_scope)

## 函数列表

| 函数名称                | 功能描述             | 认证方式 | HTTP 方法 |
| ----------------------- | -------------------- | -------- | --------- |
| GetAvailableScopeAsync  | 获取妙搭产品使用权限 | 用户令牌 | GET       |
| UpdateAvailableScopeAsync | 修改妙搭产品使用权限 | 用户令牌 | PUT       |

## 函数详细内容

### 获取妙搭产品使用权限

获取当前生效的产品使用权限配置（可用范围模式 + 允许/禁止名单）。限频：20 次/秒；所需权限：`spark:admin:read`（获取妙搭企业级管理配置）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetAvailableScopeResult>?> GetAvailableScopeAsync(
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：无业务参数。

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "available_scope": "ALLOW_PART",
    "department_ids": ["dpt_12345"],
    "user_ids": ["usr_67890"]
  }
}
```

**说明**：`data.available_scope` 可选值为 `ALLOW_ALL`（全部允许）、`DENY_ALL`（全部拒绝）、`ALLOW_PART`（部分允许）、`DENY_PART`（部分拒绝）；`department_ids`、`user_ids` 仅 `ALLOW_PART`/`DENY_PART` 时有效。

### 修改妙搭产品使用权限

配置允许（或禁止）使用产品的部门/成员范围，设置立即生效。限频：20 次/秒；所需权限：`spark:admin:write`（更新妙搭企业级管理配置）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UpdateAvailableScopeAsync(
    [Body] UpdateAvailableScopeRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名    | 类型                       | 必填 | 说明                                                                                           |
| --------- | -------------------------- | ---- | ---------------------------------------------------------------------------------------------- |
| `request` | `UpdateAvailableScopeRequest` | ✅  | `available_scope` 必填：ALLOW_ALL/DENY_ALL/ALLOW_PART/DENY_PART；`department_ids`、`user_ids` 仅 ALLOW_PART/DENY_PART 时生效，各 0～1000 |

**响应**：修改成功时 `data` 为空对象（`FeishuNullDataApiResult`）。
