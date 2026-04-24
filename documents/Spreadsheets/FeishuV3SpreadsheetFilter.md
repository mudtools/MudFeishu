# 电子表格筛选  
**筛选 - IFeishuV3SpreadsheetFilter**

## 功能描述  
筛选指在电子表格工作表指定范围中，为指定列（col）设置筛选条件。本接口提供飞书开放平台电子表格中筛选能力相关方法列表，包括创建、更新、获取和删除筛选。

## 参考文档  
- [电子表格概述](https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview)

## 接口变体

| 接口名称 | 认证方式 | 说明 |
| :--- | :--- | :--- |
| `IFeishuTenantV3SpreadsheetFilter` | 租户令牌（TenantAccessToken） | 应用身份访问 |
| `IFeishuUserV3SpreadsheetFilter` | 用户令牌（UserAccessToken） | 用户身份访问 |

## 函数列表  
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateFilterAsync | 创建筛选 | 租户令牌 / 用户令牌 | POST |
| UpdateFilterAsync | 更新筛选 | 租户令牌 / 用户令牌 | PUT |
| GetFilterAsync | 获取筛选 | 租户令牌 / 用户令牌 | GET |
| DeleteFilterAsync | 删除筛选 | 租户令牌 / 用户令牌 | DELETE |

## 函数详细内容  

### CreateFilterAsync  
创建筛选。在电子表格工作表的指定范围内，设置筛选条件，创建筛选。

**函数签名**  
```csharp
Task<FeishuNullDataApiResult?> CreateFilterAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Body] CreateFilterRequest createFilterRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| createFilterRequest | CreateFilterRequest | ✅ | 创建筛选请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**  
在指定范围内设置筛选条件，创建筛选。

**代码示例**  
```csharp
// 租户令牌方式
var tenantApi = feishuClient.TenantV3SpreadsheetFilter;
var result = await tenantApi.CreateFilterAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    new CreateFilterRequest { /* ... */ });

// 用户令牌方式
var userApi = feishuClient.UserV3SpreadsheetFilter;
var result = await userApi.CreateFilterAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    new CreateFilterRequest { /* ... */ });
```

---

### UpdateFilterAsync  
更新筛选。在电子表格工作表筛选范围中，更新指定列的筛选条件。

**函数签名**  
```csharp
Task<FeishuNullDataApiResult?> UpdateFilterAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Body] UpdateFilterRequest updateFilterRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| updateFilterRequest | UpdateFilterRequest | ✅ | 更新筛选请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**  
在筛选范围中更新指定列的筛选条件。

**代码示例**  
```csharp
var result = await tenantApi.UpdateFilterAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    new UpdateFilterRequest { /* ... */ });
```

---

### GetFilterAsync  
获取筛选。获取电子表格中工作表的详细筛选信息，包括筛选的应用范围、筛选条件、被筛选条件过滤掉的行。

**函数签名**  
```csharp
Task<FeishuApiResult<GetFilterResult>?> GetFilterAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // GetFilterResult
  }
}
```

**说明**  
返回筛选的应用范围、筛选条件、被筛选条件过滤掉的行等详细信息。

**代码示例**  
```csharp
var result = await tenantApi.GetFilterAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6");
```

---

### DeleteFilterAsync  
删除筛选。删除电子表格中指定工作表的所有筛选。

**函数签名**  
```csharp
Task<FeishuNullDataApiResult?> DeleteFilterAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**  
删除指定工作表的所有筛选。

**代码示例**  
```csharp
var result = await tenantApi.DeleteFilterAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6");
```
