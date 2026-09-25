# 用户角色（FeishuTenantV1HireUserRole）

## 接口名称

**用户角色** -（`IFeishuTenantV1HireUserRole`）

## 功能描述

以租户身份分页查询用户在飞书招聘中的角色绑定关系，返回用户 ID、角色信息、修改时间及业务管理范围（实体 + 管理范围规则）。

## 参考文档

- [获取用户角色列表](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/auth/list-2)

## 函数列表

| 函数名称            | 功能描述         | 限频                   | 所需权限                                                     | HTTP 方法 |
| ------------------- | ---------------- | ---------------------- | ------------------------------------------------------------ | --------- |
| GetUserRoleListAsync | 获取用户角色列表 | 1000 次/分、50 次/秒   | hire:auth:readonly 或 hire:auth（字段权限 contact:user.employee_id:readonly） | GET |

## 函数详细内容

### GetUserRoleListAsync — 获取用户角色列表（查询对象模式）

`GET /open-apis/hire/v1/user_roles`

```csharp
Task<FeishuApiResult<GetUserRoleListResult>?> GetUserRoleListAsync(
    [Query] UserRoleListQuery? query = null,
    CancellationToken cancellationToken = default);
```

`UserRoleListQuery`（实现 `IQueryParameter`，null/空值自动跳过）：

| 字段             | 查询参数名         | 类型   | 必填 | 说明                       |
| ---------------- | ------------------ | ------ | ---- | -------------------------- |
| PageToken        | page_token         | string | 否   | 分页标记                   |
| PageSize         | page_size          | int    | 否   | 分页大小                   |
| UserId           | user_id            | string | 否   | 用户 ID，与 user_id_type 一致 |
| RoleId           | role_id            | string | 否   | 角色 ID                    |
| UpdateStartTime  | update_start_time  | string | 否   | 按修改时间范围过滤（起）   |
| UpdateEndTime    | update_end_time    | string | 否   | 按修改时间范围过滤（止）   |
| UserIdType       | user_id_type       | string | 否   | 用户 ID 类型               |

响应 `data.items` 为 `UserRole[]`（`user_id`、`role_id`、`modify_time`、`role_name`、`role_description`、`business_management_scopes`），附 `has_more`/`page_token`。
