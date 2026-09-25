# 角色（FeishuTenantV1HireRole）

## 接口名称

**角色** -（`IFeishuTenantV1HireRole`）

## 功能描述

以租户身份查询飞书招聘的角色信息：获取单个角色详情（含社招/校招权限配置集合、业务管理范围），或分页获取角色列表。

## 参考文档

- [获取角色详情](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/auth/get)
- [获取角色列表](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/auth/list)

## 函数列表

| 函数名称       | 功能描述     | 限频                      | 所需权限                                  | HTTP 方法 |
| -------------- | ------------ | ------------------------- | ----------------------------------------- | --------- |
| GetRoleAsync   | 获取角色详情 | 10 次/秒                  | hire:auth:readonly 或 hire:auth           | GET       |
| GetRoleListAsync | 获取角色列表 | 1000 次/分、50 次/秒      | hire:auth:readonly 或 hire:auth           | GET       |

## 函数详细内容

### GetRoleAsync — 获取角色详情

`GET /open-apis/hire/v1/roles/{role_id}`

```csharp
Task<FeishuApiResult<GetRoleResult>?> GetRoleAsync(
    [Path] string role_id,
    CancellationToken cancellationToken = default);
```

响应 `data.role` 为 `RoleDetail`：角色名称/描述、适用范围、启用状态、是否配置业务管理范围，以及 `socail_permission_collection`（社招权限配置，字段名保留官方拼写）与 `campus_permission_collection`（校招权限配置），每个集合含功能权限、管理权限、数据权限与业务管理范围（实体 + 权限分组 + 管理范围规则）。

### GetRoleListAsync — 获取角色列表

`GET /open-apis/hire/v1/roles`

```csharp
Task<FeishuApiResult<GetRoleListResult>?> GetRoleListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

- `page_size`：可选，最大 200。
- 响应 `data.items` 为 `Role[]`，附 `has_more`/`page_token`。
