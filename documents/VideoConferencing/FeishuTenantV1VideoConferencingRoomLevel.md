# 会议室层级
**IFeishuTenantV1VideoConferencingRoomLevel**

## 功能描述
提供会议室层级的完整管理功能，包括创建、删除、更新会议室层级，查询层级详情（单个/批量），分页查询层级列表以及搜索层级。

## 参考文档
- [会议室层级概述](https://open.feishu.cn/document/server-docs/vc-v1/room_level/room-level-overview)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateRoomLevelAsync | 创建会议室层级 | 租户令牌 | POST |
| DeleteRoomLevelAsync | 删除会议室层级 | 租户令牌 | POST |
| UpdateRoomLevelAsync | 更新会议室层级 | 租户令牌 | PATCH |
| GetRoomLevelAsync | 查询会议室层级详情 | 租户令牌 | GET |
| GetRoomLevelsAsync | 批量查询会议室层级详情 | 租户令牌 | POST |
| GetRoomLevelsPageListAsync | 分页查询会议室层级列表 | 租户令牌 | GET |
| SearchRoomLevelAsync | 搜索会议室层级 | 租户令牌 | GET |

## 函数详细内容

### CreateRoomLevelAsync
创建一个新的会议室层级。

**函数签名**
```csharp
Task<FeishuApiResult<CreateRoomLevelResult>?> CreateRoomLevelAsync(
    CreateRoomLevelRequest createRoomLevelRequest,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| createRoomLevelRequest | CreateRoomLevelRequest | ✅ | 创建会议室层级请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "room_level": {
      "room_level_id": "omb_xxx",
      "name": "总部"
    }
  }
}
```

**代码示例**
```csharp
var request = new CreateRoomLevelRequest
{
    Name = "总部",
    ParentRoomLevelId = "omb_parent"
};
var result = await api.CreateRoomLevelAsync(request);
Console.WriteLine($"层级ID: {result?.Data?.RoomLevel?.RoomLevelId}");
```

---

### DeleteRoomLevelAsync
删除指定会议室层级。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteRoomLevelAsync(
    DeleteRoomLevelRequest deleteRoomLevelRequest,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| deleteRoomLevelRequest | DeleteRoomLevelRequest | ✅ | 删除会议室层级请求体 | - |
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
var request = new DeleteRoomLevelRequest
{
    RoomLevelId = "omb_xxx"
};
var result = await api.DeleteRoomLevelAsync(request);
```

---

### UpdateRoomLevelAsync
更新指定会议室层级的信息。

**函数签名**
```csharp
Task<FeishuApiResult<CreateRoomLevelResult>?> UpdateRoomLevelAsync(
    string room_level_id,
    UpdateRoomLevelRequest updateRoomLevelRequest,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| room_level_id | string | ✅ | 层级ID | omb_4ad1a2c7a2fbc5fc9570f38456931293 |
| updateRoomLevelRequest | UpdateRoomLevelRequest | ✅ | 更新会议室层级请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "room_level": {}
  }
}
```

**代码示例**
```csharp
var request = new UpdateRoomLevelRequest
{
    Name = "新层级名称"
};
var result = await api.UpdateRoomLevelAsync(
    room_level_id: "omb_4ad1a2c7a2fbc5fc9570f38456931293",
    updateRoomLevelRequest: request
);
```

---

### GetRoomLevelAsync
使用会议室层级 ID 查询会议室层级详情。

**函数签名**
```csharp
Task<FeishuApiResult<GetRoomLevelResult>?> GetRoomLevelAsync(
    string room_level_id,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| room_level_id | string | ✅ | 层级ID | omb_4ad1a2c7a2fbc5fc9570f38456931293 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "room_level": {
      "room_level_id": "omb_4ad1a2c7a2fbc5fc9570f38456931293",
      "name": "总部"
    }
  }
}
```

**代码示例**
```csharp
var result = await api.GetRoomLevelAsync(
    room_level_id: "omb_4ad1a2c7a2fbc5fc9570f38456931293"
);
Console.WriteLine($"层级名称: {result?.Data?.RoomLevel?.Name}");
```

---

### GetRoomLevelsAsync
使用会议室层级 ID 批量查询会议室层级详情。

**函数签名**
```csharp
Task<FeishuApiResult<GetRoomLevelsResult>?> GetRoomLevelsAsync(
    GetRoomLevelsRequest getRoomLevelsRequest,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| getRoomLevelsRequest | GetRoomLevelsRequest | ✅ | 批量查询会议室层级详情请求体，包含层级ID列表 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "room_levels": [
      {
        "room_level_id": "omb_xxx",
        "name": "总部"
      }
    ]
  }
}
```

**代码示例**
```csharp
var request = new GetRoomLevelsRequest
{
    RoomLevelIds = new[] { "omb_xxx", "omb_yyy" }
};
var result = await api.GetRoomLevelsAsync(request);
Console.WriteLine($"层级数量: {result?.Data?.RoomLevels?.Count}");
```

---

### GetRoomLevelsPageListAsync
分页查询某个会议室层级下的子层级列表。

**函数签名**
```csharp
Task<FeishuApiPageListResult<RoomLevelInfo>?> GetRoomLevelsPageListAsync(
    string room_level_id,
    int page_size = 10,
    string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| room_level_id | string | ✅ | 层级ID | omb_4ad1a2c7a2fbc5fc9570f38456931293 |
| page_size | int | ⚪ | 分页大小，最大条目数 | 10 |
| page_token | string | ⚪ | 分页标记，首次查询不填 | - |
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
var result = await api.GetRoomLevelsPageListAsync(
    room_level_id: "omb_4ad1a2c7a2fbc5fc9570f38456931293",
    page_size: 10
);
Console.WriteLine($"子层级数量: {result?.Data?.Items?.Count}");
```

---

### SearchRoomLevelAsync
使用自定义会议室层级 ID 搜索会议室层级。

**函数签名**
```csharp
Task<FeishuApiResult<SearchRoomLevelResult>?> SearchRoomLevelAsync(
    string custom_level_ids,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| custom_level_ids | string | ✅ | 用于查询指定会议室层级的自定义会议室层级ID，多个以逗号分隔 | 1000,1001 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "room_levels": []
  }
}
```

**代码示例**
```csharp
var result = await api.SearchRoomLevelAsync(
    custom_level_ids: "1000,1001"
);
Console.WriteLine($"搜索结果: {result?.Data?.RoomLevels?.Count}");
```