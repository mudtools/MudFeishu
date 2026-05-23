# 会议室管理 - 租户令牌
**IFeishuTenantV1VideoConferencingRoom**

## 功能描述
提供会议室的完整 CRUD 操作，包括创建会议室、删除会议室、更新会议室、查询会议室详情、批量查询会议室以及分页查询会议室列表。

## 参考文档
- [会议室概述](https://open.feishu.cn/document/server-docs/vc-v1/room/room-overview)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateMeetingRoomAsync | 创建会议室 | 租户令牌 | POST |
| DeleteMeetingRoomAsync | 删除会议室 | 租户令牌 | DELETE |
| UpdateMeetingRoomAsync | 更新会议室 | 租户令牌 | PATCH |
| GetMeetingRoomAsync | 查询会议室详情 | 租户令牌 | GET |
| GetMeetingRoomsAsync | 批量查询会议室详情 | 租户令牌 | POST |
| GetMeetingRoomsPageListAsync | 分页查询会议室列表 | 租户令牌 | GET |

## 函数详细内容

### CreateMeetingRoomAsync
创建一个新的会议室。

**函数签名**
```csharp
Task<FeishuApiResult<CreateMeetingRoomResult>?> CreateMeetingRoomAsync(
    CreateMeetingRoomRequest createMeetingRoomRequest,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| createMeetingRoomRequest | CreateMeetingRoomRequest | ✅ | 创建会议室请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "room": {
      "room_id": "omm_4de32cf10a4358788ff4e09e37ebbf9b",
      "name": "会议室A"
    }
  }
}
```

**代码示例**
```csharp
var request = new CreateMeetingRoomRequest
{
    Name = "会议室A",
    RoomLevelId = "omb_xxx"
};
var result = await api.CreateMeetingRoomAsync(request);
Console.WriteLine($"会议室ID: {result?.Data?.Room?.RoomId}");
```

---

### DeleteMeetingRoomAsync
删除指定会议室。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteMeetingRoomAsync(
    string room_id,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| room_id | string | ✅ | 会议室ID | omm_4de32cf10a4358788ff4e09e37ebbf9b |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success"
}
```

**代码示例**
```csharp
var result = await api.DeleteMeetingRoomAsync(
    room_id: "omm_4de32cf10a4358788ff4e09e37ebbf9b"
);
```

---

### UpdateMeetingRoomAsync
更新指定会议室的信息。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateMeetingRoomAsync(
    string room_id,
    UpdateMeetingRoomRequest updateMeetingRoomRequest,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| room_id | string | ✅ | 会议室ID | omm_4de32cf10a4358788ff4e09e37ebbf9b |
| updateMeetingRoomRequest | UpdateMeetingRoomRequest | ✅ | 更新会议室请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success"
}
```

**代码示例**
```csharp
var request = new UpdateMeetingRoomRequest
{
    Name = "新会议室名称"
};
var result = await api.UpdateMeetingRoomAsync(
    room_id: "omm_4de32cf10a4358788ff4e09e37ebbf9b",
    updateMeetingRoomRequest: request
);
```

---

### GetMeetingRoomAsync
查询指定会议室的详细信息。

**函数签名**
```csharp
Task<FeishuApiResult<GetMeetingRoomResult>?> GetMeetingRoomAsync(
    string room_id,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| room_id | string | ✅ | 会议室ID | omm_4de32cf10a4358788ff4e09e37ebbf9b |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "room": {
      "room_id": "omm_4de32cf10a4358788ff4e09e37ebbf9b",
      "name": "会议室A",
      "capacity": 10
    }
  }
}
```

**代码示例**
```csharp
var result = await api.GetMeetingRoomAsync(
    room_id: "omm_4de32cf10a4358788ff4e09e37ebbf9b"
);
Console.WriteLine($"会议室名称: {result?.Data?.Room?.Name}");
```

---

### GetMeetingRoomsAsync
使用会议室 ID 批量查询会议室详情。

**函数签名**
```csharp
Task<FeishuApiResult<GetMeetingRoomsResult>?> GetMeetingRoomsAsync(
    GetMeetingRoomsRequest getMeetingRoomsRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| getMeetingRoomsRequest | GetMeetingRoomsRequest | ✅ | 批量查询会议室请求体，包含会议室ID列表 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "rooms": [
      {
        "room_id": "omm_xxx",
        "name": "会议室A"
      }
    ]
  }
}
```

**代码示例**
```csharp
var request = new GetMeetingRoomsRequest
{
    RoomIds = new[] { "omm_xxx", "omm_yyy" }
};
var result = await api.GetMeetingRoomsAsync(request);
Console.WriteLine($"会议室数量: {result?.Data?.Rooms?.Count}");
```

---

### GetMeetingRoomsPageListAsync
分页查询某个会议室层级下的会议室列表。当 room_level_id 传空时可获取租户下全部会议室列表。

**函数签名**
```csharp
Task<FeishuApiPageListResult<MeetingRoomInfo>?> GetMeetingRoomsPageListAsync(
    string? room_level_id = null,
    int page_size = 10,
    string? page_token = null,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| room_level_id | string | ⚪ | 层级ID，传空时获取租户下所有会议室列表 | omb_4ad1a2c7a2fbc5fc9570f38456931293 |
| page_size | int | ⚪ | 分页大小，最大条目数 | 10 |
| page_token | string | ⚪ | 分页标记，首次查询不填 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [],
    "page_token": "",
    "has_more": false
  }
}
```

**代码示例**
```csharp
var result = await api.GetMeetingRoomsPageListAsync(
    room_level_id: "omb_4ad1a2c7a2fbc5fc9570f38456931293",
    page_size: 10
);
Console.WriteLine($"会议室数量: {result?.Data?.Items?.Count}");
```