# 审批地理库（租户令牌） - FeishuV4ApprovalDistrict_Tenant

## 接口名称

审批地理库接口（租户令牌） -（`IFeishuTenantV4ApprovalDistrict`）

## 功能描述

获取审批的地理库数据，用于在发起审批时填写地址控件的区域信息。支持从国家层级开始按层级遍历，也支持按关键字模糊搜索区域。对应 Go SDK 的 `district.List` / `district.Search`。

## 参考文档

- [飞书官方文档 - 查询地理库信息](https://open.feishu.cn/api-explorer?from=op_doc_tab&apiName=list&project=approval&resource=district&version=v4)
- [飞书官方文档 - 搜索地理库信息](https://open.feishu.cn/api-explorer?from=op_doc_tab&apiName=search&project=approval&resource=district&version=v4)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| `GetDistrictsPageListAsync` | 查询地理库信息 | 租户令牌/用户令牌 | GET |
| `SearchDistrictsAsync` | 搜索地理库信息 | 租户令牌/用户令牌 | POST |

## 函数详细内容

### GetDistrictsPageListAsync - 查询地理库信息

#### 函数签名

```csharp
[Get("/open-apis/approval/v4/districts")]
Task<FeishuApiResult<ListDistrictResult>?> GetDistrictsPageListAsync(
    [Query("page_size")] int page_size = Consts.PageSize_20,
    [Query("page_token")] string? page_token = null,
    [Query("root_district_id")] string? root_district_id = null,
    [Query("list_type")] string? list_type = null,
    [Query("locale")] string? locale = null,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）；同时支持用户令牌（见 `FeishuV4ApprovalDistrict_User`）。

#### 参数

| 参数名 | 必填 | 类型 | 描述 |
| ------ | ---- | ---- | ---- |
| `page_size` | ⚪ | `int` | 分页大小，最大：100，默认：20 |
| `page_token` | ⚪ | `string` | 分页标记，第一次请求不填 |
| `root_district_id` | ⚪ | `string` | 根区域 ID，不传默认从国家层级开始查询 |
| `list_type` | ⚪ | `string` | 遍历方式：`parent_level` 按父层级整层遍历；`leaf_level` 递归遍历到叶子层级 |
| `locale` | ⚪ | `string` | 语言，默认 `zh-CN` |
| `cancellationToken` | ⚪ | `CancellationToken` | 取消操作令牌对象 |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "version": "20230308",
    "has_more": true,
    "page_token": "next_page_token",
    "items": [
      {
        "id": "115618457",
        "name": "敏斯特",
        "level": "district",
        "has_sub_district": false,
        "parent_districts": [
          { "id": "115618456", "name": "柏林", "level": "city" }
        ]
      }
    ]
  }
}
```

#### 说明

- `items` 元素类型为 `District`：`id`、`name`、`level`（country/province/city/district）、`has_sub_district`、`parent_districts`。
- `parent_districts` 仅在 `list_type=leaf_level` 时返回。

### SearchDistrictsAsync - 搜索地理库信息

#### 函数签名

```csharp
[Post("/open-apis/approval/v4/districts/search")]
Task<FeishuApiResult<SearchDistrictResult>?> SearchDistrictsAsync(
    [Body] SearchDistrictRequest searchDistrictRequest,
    [Query("locale")] string? locale = null,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）；同时支持用户令牌（见 `FeishuV4ApprovalDistrict_User`）。

#### 参数

| 参数名 | 必填 | 类型 | 描述 |
| ------ | ---- | ---- | ---- |
| `searchDistrictRequest` | ✅ | `SearchDistrictRequest` | 搜索地理库信息请求体 |
| `locale` | ⚪ | `string` | 语言，默认 `zh-CN` |
| `cancellationToken` | ⚪ | `CancellationToken` | 取消操作令牌对象 |

**请求体（`SearchDistrictRequest`）字段：**

| JSON 字段 | 类型 | 描述 |
| --------- | ---- | ---- |
| `district_ids` | `string[]` | 根据 ID 查询指定区域，传了则以该参数为唯一筛选项 |
| `keyword` | `string` | 关键字，模糊查询。示例值：杭州市 |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      { "id": "156182582", "name": "杭州市", "level": "city", "has_sub_district": true }
    ]
  }
}
```

#### 说明

- 按官方文档，搜索接口响应 data 下仅有 `items`（不含 version/has_more/page_token），类型为 `ApiListResult<District>`。
