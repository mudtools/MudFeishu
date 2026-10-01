---
title: 多实体搜索接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份进行多实体搜索，适用于写信联系人等场景。
---

# IFeishuUserV1MailMultiEntity - 用户多实体搜索API

## 功能描述
飞书邮箱多实体搜索API接口实现了写信联系人等多实体的搜索功能。支持用户通过用户访问令牌在写信场景下检索联系人、群聊、部门等实体。

## 参考文档
- [多实体搜索](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/multi_entity/search)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| SearchMultiEntityAsync | 多实体搜索 | UserAccessToken | POST |

## 函数详细内容

### SearchMultiEntityAsync
多实体搜索

**函数签名**
```csharp
Task<FeishuApiResult<SearchMultiEntityResult>?> SearchMultiEntityAsync(
    [Body] SearchMultiEntityRequest request,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | SearchMultiEntityRequest | ✅ | 多实体搜索请求对象，包含搜索关键词与返回条数。 | - |
| user_id_type | string? | ⚪ | 用户 ID 类型，可选值：open_id、union_id、user_id。默认值：open_id。值为 user_id 时需具备字段权限：获取用户 user ID(contact:user.employee_id:readonly)。 | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "type": "user",
        "id": "6911188411932033028",
        "name": "张三",
        "email": "zhangsan@bytedance.com",
        "display_name": "备注名",
        "user_id": "ou_2d131f4c3a28b0",
        "department": "飞书研发团队",
        "chat_id": "oc_40ed357053e34b9",
        "member_count": 128,
        "tag": "邮箱联系人"
      }
    ],
    "notice": ""
  }
}
```

**说明**
- 多实体搜索，适用于写信联系人搜索
- 请求体 `SearchMultiEntityRequest` 字段：

| 字段 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| query | string | ✅ | 搜索关键词，最大长度 50、最小长度 1 | 周会 |
| size | int? | ⚪ | 获取的数据条数，取值范围 1~20，默认值 20 | 20 |

- 响应 `items[]` 中的实体类型由 `type` 标识（如 user、chat 等），字段按实体类型可选返回
- `notice` 返回本次搜索的额外说明（例如 query 被截断、搜索结果不全）

**代码示例**
```csharp
var searchApi = feishuApp.GetApi<IFeishuUserV1MailMultiEntity>();
var request = new SearchMultiEntityRequest
{
    Query = "张三",
    Size = 20
};
var result = await searchApi.SearchMultiEntityAsync(request);
if (result?.Data?.Items != null)
{
    foreach (var item in result.Data.Items)
    {
        Console.WriteLine($"[{item.Type}] {item.Name} ({item.Email})");
    }
}
```
