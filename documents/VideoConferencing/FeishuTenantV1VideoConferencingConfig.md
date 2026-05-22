# 会议室配置
**IFeishuTenantV1VideoConferencingConfig**

## 功能描述
会议室配置用于对飞书会议室的背景设置、资源管理等进行配置。支持查询和设置会议室配置、管理会议室预定限制、预定表单、预定管理员以及禁用状态变更通知。

## 参考文档
- [会议室配置概述](https://open.feishu.cn/document/server-docs/vc-v1/scope_config/room-configuration-overview)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| GetScopeConfigAsync | 查询会议室配置 | 租户令牌 | GET |
| CreateScopeConfigAsync | 设置会议室配置 | 租户令牌 | POST |
| GetReserveScopeReserveConfigAsync | 查询会议室预定限制 | 租户令牌 | GET |
| UpdateReserveConfigAsync | 更新会议室预定限制 | 租户令牌 | PATCH |
| GetReserveConfigFormAsync | 查询会议室预定表单 | 租户令牌 | GET |
| UpdateReserveConfigFormAsync | 更新会议室预定表单 | 租户令牌 | PATCH |
| GetReserveConfigAdminAsync | 查询会议室预定管理员 | 租户令牌 | GET |
| UpdateReserveConfigAdminAsync | 更新会议室预定管理员 | 租户令牌 | PATCH |
| GetReserveConfigDisableInformAsync | 查询禁用状态变更通知 | 租户令牌 | GET |
| UpdateReserveConfigDisableInformAsync | 更新禁用状态变更通知 | 租户令牌 | PATCH |

## 函数详细内容

### GetScopeConfigAsync
查询某个会议层级范围下或者某个会议室的配置。

**函数签名**
```csharp
Task<FeishuApiResult<GetScopeConfigResult>?> GetScopeConfigAsync(
    string scope_type,
    string scope_id,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| scope_type | string | ✅ | 查询节点范围：1=会议室层级，2=会议室 | 1 |
| scope_id | string | ✅ | 查询节点ID，scope_type=1时为层级ID，scope_type=2时为会议室ID | omm_608d34d82d531b27fa993902d350a307 |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "scope_config": {
      "scope_type": 1,
      "scope_id": "omm_608d34d82d531b27fa993902d350a307",
      "config": {}
    }
  }
}
```

**代码示例**
```csharp
var result = await api.GetScopeConfigAsync(
    scope_type: "1",
    scope_id: "omm_608d34d82d531b27fa993902d350a307"
);
Console.WriteLine(result?.Data);
```

---

### CreateScopeConfigAsync
设置某个会议层级范围下或者某个会议室的配置。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> CreateScopeConfigAsync(
    CreateScopeConfigRequest createScopeConfigRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| createScopeConfigRequest | CreateScopeConfigRequest | ✅ | 设置会议室配置请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
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
var request = new CreateScopeConfigRequest { /* ... */ };
var result = await api.CreateScopeConfigAsync(request);
```

---

### GetReserveScopeReserveConfigAsync
查询会议室预定限制。

**函数签名**
```csharp
Task<FeishuApiResult<GetReserveScopeReserveConfigResult>?> GetReserveScopeReserveConfigAsync(
    string scope_id,
    string scope_type,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| scope_id | string | ✅ | 查询节点ID，scope_type=1时为层级ID，scope_type=2时为会议室ID | omm_608d34d82d531b27fa993902d350a307 |
| scope_type | string | ✅ | 查询节点范围：1=会议室层级，2=会议室 | 1 |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "reserve_config": {}
  }
}
```

**代码示例**
```csharp
var result = await api.GetReserveScopeReserveConfigAsync(
    scope_id: "omm_608d34d82d531b27fa993902d350a307",
    scope_type: "1"
);
Console.WriteLine(result?.Data);
```

---

### UpdateReserveConfigAsync
更新会议室预定限制。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateReserveConfigAsync(
    string reserve_config_id,
    UpdateReserveConfigRequest updateReserveConfigRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| reserve_config_id | string | ✅ | 会议室或层级ID | omm_3c5dd7e09bac0c1758fcf9511bd1a771 |
| updateReserveConfigRequest | UpdateReserveConfigRequest | ✅ | 更新会议室预定限制请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
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
var request = new UpdateReserveConfigRequest { /* ... */ };
var result = await api.UpdateReserveConfigAsync(
    reserve_config_id: "omm_3c5dd7e09bac0c1758fcf9511bd1a771",
    updateReserveConfigRequest: request
);
```

---

### GetReserveConfigFormAsync
查询会议室预定表单。

**函数签名**
```csharp
Task<FeishuApiResult<GetReserveConfigFormResult>?> GetReserveConfigFormAsync(
    string reserve_config_id,
    string scope_type,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| reserve_config_id | string | ✅ | 会议室或层级ID | omm_3c5dd7e09bac0c1758fcf9511bd1a771 |
| scope_type | string | ✅ | 查询节点范围：1=会议室层级，2=会议室 | 1 |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "form_config": {}
  }
}
```

**代码示例**
```csharp
var result = await api.GetReserveConfigFormAsync(
    reserve_config_id: "omm_3c5dd7e09bac0c1758fcf9511bd1a771",
    scope_type: "1"
);
Console.WriteLine(result?.Data);
```

---

### UpdateReserveConfigFormAsync
更新会议室预定表单。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateReserveConfigFormAsync(
    string reserve_config_id,
    UpdateReserveConfigFormRequest updateReserveConfigFormRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| reserve_config_id | string | ✅ | 会议室或层级ID | omm_3c5dd7e09bac0c1758fcf9511bd1a771 |
| updateReserveConfigFormRequest | UpdateReserveConfigFormRequest | ✅ | 更新会议室预定表单请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
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
var request = new UpdateReserveConfigFormRequest { /* ... */ };
var result = await api.UpdateReserveConfigFormAsync(
    reserve_config_id: "omm_3c5dd7e09bac0c1758fcf9511bd1a771",
    updateReserveConfigFormRequest: request
);
```

---

### GetReserveConfigAdminAsync
查询会议室预定管理员。

**函数签名**
```csharp
Task<FeishuApiResult<GetReserveConfigAdminResult>?> GetReserveConfigAdminAsync(
    string reserve_config_id,
    string scope_type,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| reserve_config_id | string | ✅ | 会议室或层级ID | omm_3c5dd7e09bac0c1758fcf9511bd1a771 |
| scope_type | string | ✅ | 查询节点范围：1=会议室层级，2=会议室 | 1 |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "admins": []
  }
}
```

**代码示例**
```csharp
var result = await api.GetReserveConfigAdminAsync(
    reserve_config_id: "omm_3c5dd7e09bac0c1758fcf9511bd1a771",
    scope_type: "1"
);
Console.WriteLine(result?.Data);
```

---

### UpdateReserveConfigAdminAsync
更新会议室预定管理员。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateReserveConfigAdminAsync(
    string reserve_config_id,
    UpdateReserveConfigAdminRequest updateReserveConfigAdminRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| reserve_config_id | string | ✅ | 会议室或层级ID | omm_3c5dd7e09bac0c1758fcf9511bd1a771 |
| updateReserveConfigAdminRequest | UpdateReserveConfigAdminRequest | ✅ | 更新会议室预定管理员请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
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
var request = new UpdateReserveConfigAdminRequest { /* ... */ };
var result = await api.UpdateReserveConfigAdminAsync(
    reserve_config_id: "omm_3c5dd7e09bac0c1758fcf9511bd1a771",
    updateReserveConfigAdminRequest: request
);
```

---

### GetReserveConfigDisableInformAsync
查询禁用状态变更通知。

**函数签名**
```csharp
Task<FeishuApiResult<GetReserveConfigDisableInformResult>?> GetReserveConfigDisableInformAsync(
    string reserve_config_id,
    string scope_type,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| reserve_config_id | string | ✅ | 会议室或层级ID | omm_3c5dd7e09bac0c1758fcf9511bd1a771 |
| scope_type | string | ✅ | 查询节点范围：1=会议室层级，2=会议室 | 1 |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "disable_inform": {}
  }
}
```

**代码示例**
```csharp
var result = await api.GetReserveConfigDisableInformAsync(
    reserve_config_id: "omm_3c5dd7e09bac0c1758fcf9511bd1a771",
    scope_type: "1"
);
Console.WriteLine(result?.Data);
```

---

### UpdateReserveConfigDisableInformAsync
更新禁用状态变更通知。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateReserveConfigDisableInformAsync(
    string reserve_config_id,
    UpdateReserveConfigDisableInformRequest updateReserveConfigDisableInformRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| reserve_config_id | string | ✅ | 会议室或层级ID | omm_3c5dd7e09bac0c1758fcf9511bd1a771 |
| updateReserveConfigDisableInformRequest | UpdateReserveConfigDisableInformRequest | ✅ | 更新禁用状态变更通知请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
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
var request = new UpdateReserveConfigDisableInformRequest { /* ... */ };
var result = await api.UpdateReserveConfigDisableInformAsync(
    reserve_config_id: "omm_3c5dd7e09bac0c1758fcf9511bd1a771",
    updateReserveConfigDisableInformRequest: request
);
```