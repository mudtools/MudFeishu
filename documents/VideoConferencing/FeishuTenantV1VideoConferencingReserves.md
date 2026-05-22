# 会议预约
**IFeishuTenantV1VideoConferencingReserves**

## 功能描述
会议预约功能提供预约会议（创建会议预约）能力，可以提前设置参会成员和会议权限，并获取会议信息。支持预约最近 30 天内的会议，到期后可续期。

## 参考文档
- [会议预约概述](https://open.feishu.cn/document/server-docs/vc-v1/reserve/schedule-meeting-overview)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| ApplyReserveAsync | 预约会议 | 租户令牌 | POST |
| DeleteReserveAsync | 删除预约 | 租户令牌 | DELETE |
| UpdateReserveAsync | 更新预约 | 租户令牌 | PUT |
| GetReserveAsync | 获取预约详情 | 租户令牌 | GET |
| GetActiveMeetingReserveAsync | 获取活跃会议 | 租户令牌 | GET |

## 函数详细内容

### ApplyReserveAsync
创建一个会议预约。支持预约最近 30 天内的会议，预约到期后会议号将被释放，如需继续使用可通过"更新预约"接口进行续期。

**函数签名**
```csharp
Task<FeishuApiResult<ApplyReserveResult>?> ApplyReserveAsync(
    ApplyReserveRequest applyReserveRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| applyReserveRequest | ApplyReserveRequest | ✅ | 预约会议请求体，可配置参会人在会中的权限 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 预约到期时间距离当前时间不超过 30 天
- 预约到期后会议号将被释放，可通过更新预约续期
- 预约时可配置参会人在会中的权限

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "reserve": {
      "id": "6911188411932033028",
      "meeting_no": "123456789",
      "expire_time": "1655363258"
    }
  }
}
```

**代码示例**
```csharp
var request = new ApplyReserveRequest
{
    EndTime = "1655363258",
    Topic = "项目周会"
};
var result = await api.ApplyReserveAsync(request);
Console.WriteLine($"预约ID: {result?.Data?.Reserve?.Id}");
```

---

### DeleteReserveAsync
删除一个预约。只能删除归属于自己的预约；删除后数据不可恢复。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteReserveAsync(
    string reserve_id,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| reserve_id | string | ✅ | 预约ID（预约的唯一标识） | 6911188411932033028 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 只能删除归属于自己的预约
- 删除后数据不可恢复

**响应**
```json
{
  "code": 0,
  "msg": "success"
}
```

**代码示例**
```csharp
var result = await api.DeleteReserveAsync(
    reserve_id: "6911188411932033028"
);
```

---

### UpdateReserveAsync
更新一个预约。只能更新归属于自己的预约，不需要更新的字段不传（如果传空则会被更新为空）。可用于续期操作，到期时间距离当前时间不超过 30 天。

**函数签名**
```csharp
Task<FeishuApiResult<UpdateReserveResult>?> UpdateReserveAsync(
    string reserve_id,
    UpdateReserveRequest updateReserveRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| reserve_id | string | ✅ | 预约ID（预约的唯一标识） | 6911188411932033028 |
| updateReserveRequest | UpdateReserveRequest | ✅ | 更新预约请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 不需要更新的字段不要传入，传空则会被更新为空
- 续期时到期时间距离当前时间不超过 30 天

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {}
}
```

**代码示例**
```csharp
var request = new UpdateReserveRequest
{
    EndTime = "1655450000"
};
var result = await api.UpdateReserveAsync(
    reserve_id: "6911188411932033028",
    updateReserveRequest: request
);
```

---

### GetReserveAsync
获取一个预约的详情。只能获取归属于自己的预约。

**函数签名**
```csharp
Task<FeishuApiResult<GetReserveResult>?> GetReserveAsync(
    string reserve_id,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| reserve_id | string | ✅ | 预约ID（预约的唯一标识） | 6911188411932033028 |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 只能获取归属于自己的预约

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "reserve": {
      "id": "6911188411932033028",
      "meeting_no": "123456789",
      "topic": "项目周会"
    }
  }
}
```

**代码示例**
```csharp
var result = await api.GetReserveAsync(
    reserve_id: "6911188411932033028"
);
Console.WriteLine($"预约详情: {result?.Data?.Reserve?.Topic}");
```

---

### GetActiveMeetingReserveAsync
获取一个预约的当前活跃会议。一个预约最多有一个正在进行中的会议。

**函数签名**
```csharp
Task<FeishuApiResult<GetReserveResult>?> GetActiveMeetingReserveAsync(
    string reserve_id,
    bool? with_participants = null,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| reserve_id | string | ✅ | 预约ID（预约的唯一标识） | 6911188411932033028 |
| with_participants | bool? | ⚪ | 是否需要参会人列表，默认为 false | false |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 一个预约最多有一个正在进行中的会议

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "meeting": {
      "id": "6911188411932033028",
      "status": 1
    }
  }
}
```

**代码示例**
```csharp
var result = await api.GetActiveMeetingReserveAsync(
    reserve_id: "6911188411932033028",
    with_participants: true
);
Console.WriteLine($"活跃会议: {result?.Data}");
```