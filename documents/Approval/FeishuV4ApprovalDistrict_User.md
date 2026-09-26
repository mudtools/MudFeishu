# 审批地理库（用户令牌） - FeishuV4ApprovalDistrict_User

## 接口名称

审批地理库接口（用户令牌） -（`IFeishuUserV4ApprovalDistrict`）

## 功能描述

与租户令牌版本的 [FeishuV4ApprovalDistrict_Tenant](./FeishuV4ApprovalDistrict_Tenant.md) 完全一致，区别仅在于使用**用户身份令牌**（`UserAccessToken`）调用，实现 `ICurrentUserId` 以携带当前用户身份。

## 参考文档

- [飞书官方文档 - 查询地理库信息](https://open.feishu.cn/api-explorer?from=op_doc_tab&apiName=list&project=approval&resource=district&version=v4)
- [飞书官方文档 - 搜索地理库信息](https://open.feishu.cn/api-explorer?from=op_doc_tab&apiName=search&project=approval&resource=district&version=v4)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| `GetDistrictsPageListAsync` | 查询地理库信息 | 用户令牌 | GET |
| `SearchDistrictsAsync` | 搜索地理库信息 | 用户令牌 | POST |

## 函数详细内容

函数签名、参数、请求/响应示例与 [FeishuV4ApprovalDistrict_Tenant](./FeishuV4ApprovalDistrict_Tenant.md) 一致（列表接口分页大小默认 20，搜索接口仅支持 locale 查询参数）。

#### 认证

**用户令牌**（`UserAccessToken`）

#### 说明

- 地理库查询为只读接口，飞书同时支持租户令牌与用户令牌。
- 用户令牌调用时 SDK 会自动附加当前用户身份（`ICurrentUserId`）。
