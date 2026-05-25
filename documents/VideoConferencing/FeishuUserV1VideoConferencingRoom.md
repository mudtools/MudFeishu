# 会议室搜索 - 用户令牌
**IFeishuUserV1VideoConferencingRoom**

## 功能描述
提供会议室搜索能力，支持使用关键词或自定义会议室 ID 进行查询。该接口只会返回用户有预定权限的会议室列表。

## 参考文档
- [会议室概述](https://open.feishu.cn/document/server-docs/vc-v1/room/room-overview)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| SearchMeetingRoomsAsync | 搜索会议室 | 用户令牌 | POST |

## 函数详细内容

### SearchMeetingRoomsAsync
搜索会议室。可以使用关键词进行搜索，也支持使用自定义会议室 ID 进行查询。该接口只会返回用户有预定权限的会议室列表。

**函数签名**
```csharp
Task<FeishuApiResult<SearchMeetingRoomsResult>?> SearchMeetingRoomsAsync(
    SearchMeetingRoomsRequest searchMeetingRoomsRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
用户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| searchMeetingRoomsRequest | SearchMeetingRoomsRequest | ✅ | 搜索会议室请求对象，支持关键词和自定义ID | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 只会返回当前用户有预定权限的会议室列表

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "rooms": [
      {
        "room_id": "omm_xxx",
        "name": "会议室A",
        "capacity": 10
      }
    ]
  }
}
```

**代码示例**
```csharp
var request = new SearchMeetingRoomsRequest
{
    Keyword = "会议室A"
};
var result = await api.SearchMeetingRoomsAsync(request);
Console.WriteLine($"搜索结果: {result?.Data?.Rooms?.Count}");
```