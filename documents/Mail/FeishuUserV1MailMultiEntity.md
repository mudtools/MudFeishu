# 邮箱多实体搜索 API（用户令牌）

## 接口名称
**飞书邮箱多实体搜索 API -（IFeishuUserV1MailMultiEntity）**

## 功能描述
飞书邮箱多实体搜索 API 接口实现了写信联系人等多实体的搜索功能。
当前接口使用用户令牌（UserAccessToken）访问。

## 参考文档
- [多实体搜索](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/multi_entity/search)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| SearchMultiEntityAsync | 多实体搜索 | UserAccessToken | POST |

## 函数详细内容

### 多实体搜索

**函数签名**：
```csharp
Task<FeishuApiResult<SearchMultiEntityResult>?> SearchMultiEntityAsync(
    [Body] SearchMultiEntityRequest request,
    [Query] string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| request | SearchMultiEntityRequest | ✅ | 多实体搜索请求对象，包含搜索关键词与返回条数 |
| user_id_type | string? | ⚪ | 用户 ID 类型，可选值：`open_id`、`union_id`、`user_id`，默认值：`open_id`。当值为 `user_id` 时，需具备字段权限 `contact:user.employee_id:readonly`（获取用户 user ID） |

**说明**：多实体搜索，适用于写信联系人搜索。
