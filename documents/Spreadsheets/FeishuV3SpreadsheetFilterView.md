# 电子表格筛选视图  
**筛选视图 - IFeishuV3SpreadsheetFilterView**

## 功能描述  
飞书的筛选视图是解决在线表格协作中"互相干扰"问题的关键功能，同时也是一个强大的数据组织和分发工具，帮助团队在共享一份数据源的同时，拥有各自独立的、高效的观察视角。

本接口提供筛选视图和筛选条件的完整 CRUD 操作能力。

## 参考文档  
- [电子表格概述](https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview)

## 接口变体

| 接口名称 | 认证方式 | 说明 |
| :--- | :--- | :--- |
| `IFeishuTenantV3SpreadsheetFilterView` | 租户令牌（TenantAccessToken） | 应用身份访问 |
| `IFeishuUserV3SpreadsheetFilterView` | 用户令牌（UserAccessToken） | 用户身份访问 |

## 函数列表  
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateFilterViewAsync | 创建筛选视图 | 租户令牌 / 用户令牌 | POST |
| UpdateFilterViewAsync | 更新筛选视图 | 租户令牌 / 用户令牌 | PATCH |
| GetFilterViewsAsync | 查询筛选视图列表 | 租户令牌 / 用户令牌 | GET |
| GetFilterViewByIdAsync | 获取筛选视图 | 租户令牌 / 用户令牌 | GET |
| DeleteFilterViewByIdAsync | 删除筛选视图 | 租户令牌 / 用户令牌 | DELETE |
| CreateFilterConditionsAsync | 创建筛选条件 | 租户令牌 / 用户令牌 | POST |
| UpdateFilterConditionsAsync | 更新筛选条件 | 租户令牌 / 用户令牌 | PUT |
| GetFilterConditionsAsync | 查询筛选条件列表 | 租户令牌 / 用户令牌 | GET |
| GetFilterConditionByIdAsync | 获取筛选条件 | 租户令牌 / 用户令牌 | GET |
| DeleteFilterConditionByIdAsync | 删除筛选条件 | 租户令牌 / 用户令牌 | DELETE |

## 函数详细内容  

### CreateFilterViewAsync  
创建筛选视图。指定电子表格工作表的筛选范围，创建一个筛选视图。

**函数签名**  
```csharp
Task<FeishuApiResult<FilterViewResult>?> CreateFilterViewAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Body] CreateFilterViewRequest createFilterViewRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| createFilterViewRequest | CreateFilterViewRequest | ✅ | 创建筛选视图请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // FilterViewResult
  }
}
```

**说明**  
指定筛选范围，创建筛选视图。

**代码示例**  
```csharp
// 租户令牌方式
var tenantApi = feishuClient.TenantV3SpreadsheetFilterView;
var result = await tenantApi.CreateFilterViewAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    new CreateFilterViewRequest { /* ... */ });

// 用户令牌方式
var userApi = feishuClient.UserV3SpreadsheetFilterView;
var result = await userApi.CreateFilterViewAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    new CreateFilterViewRequest { /* ... */ });
```

---

### UpdateFilterViewAsync  
更新筛选视图。更新筛选视图的名称或筛选范围。

**函数签名**  
```csharp
Task<FeishuApiResult<FilterViewResult>?> UpdateFilterViewAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Path] string filter_view_id,
    [Body] UpdateFilterViewRequest updateFilterViewRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| filter_view_id | string | ✅ | 筛选视图 ID | `"pH9hbVcCXA"` |
| updateFilterViewRequest | UpdateFilterViewRequest | ✅ | 更新筛选视图请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // FilterViewResult
  }
}
```

**说明**  
支持更新筛选视图的名称或筛选范围。

**代码示例**  
```csharp
var result = await tenantApi.UpdateFilterViewAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    "pH9hbVcCXA",
    new UpdateFilterViewRequest { /* ... */ });
```

---

### GetFilterViewsAsync  
查询筛选视图列表。查询电子表格指定工作表的所有筛选视图及其基本信息，包括视图 ID、视图名称和筛选范围。

**函数签名**  
```csharp
Task<FeishuApiResult<GetFilterViewsResult>?> GetFilterViewsAsync(
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
    // GetFilterViewsResult
  }
}
```

**说明**  
返回工作表中所有筛选视图的 ID、名称和筛选范围。

**代码示例**  
```csharp
var result = await tenantApi.GetFilterViewsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6");
```

---

### GetFilterViewByIdAsync  
获取筛选视图。获取指定筛选视图的信息，包括 ID、名称和筛选范围。

**函数签名**  
```csharp
Task<FeishuApiResult<FilterViewResult>?> GetFilterViewByIdAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Path] string filter_view_id,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| filter_view_id | string | ✅ | 筛选视图 ID | `"pH9hbVcCXA"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // FilterViewResult
  }
}
```

**说明**  
返回指定筛选视图的 ID、名称和筛选范围。

**代码示例**  
```csharp
var result = await tenantApi.GetFilterViewByIdAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    "pH9hbVcCXA");
```

---

### DeleteFilterViewByIdAsync  
删除筛选视图。删除指定筛选视图。

**函数签名**  
```csharp
Task<FeishuNullDataApiResult?> DeleteFilterViewByIdAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Path] string filter_view_id,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| filter_view_id | string | ✅ | 筛选视图 ID | `"pH9hbVcCXA"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**  
删除指定筛选视图及其所有筛选条件。

**代码示例**  
```csharp
var result = await tenantApi.DeleteFilterViewByIdAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    "pH9hbVcCXA");
```

---

### CreateFilterConditionsAsync  
创建筛选条件。在筛选视图的指定列创建筛选条件，包括筛选的类型、比较类型、筛选参数等。

**函数签名**  
```csharp
Task<FeishuApiResult<FilterConditionsOpsResult>?> CreateFilterConditionsAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Path] string filter_view_id,
    [Body] CreateFilterConditionsRequest createFilterConditionsRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| filter_view_id | string | ✅ | 筛选视图 ID | `"pH9hbVcCXA"` |
| createFilterConditionsRequest | CreateFilterConditionsRequest | ✅ | 创建筛选条件请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // FilterConditionsOpsResult
  }
}
```

**说明**  
包括筛选的类型、比较类型、筛选参数等。

**代码示例**  
```csharp
var result = await tenantApi.CreateFilterConditionsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    "pH9hbVcCXA",
    new CreateFilterConditionsRequest { /* ... */ });
```

---

### UpdateFilterConditionsAsync  
更新筛选条件。在筛选视图的指定列更新筛选条件。

**函数签名**  
```csharp
Task<FeishuApiResult<FilterConditionsOpsResult>?> UpdateFilterConditionsAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Path] string filter_view_id,
    [Path] string condition_id,
    [Body] UpdateFilterConditionsRequest updateFilterConditionsRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| filter_view_id | string | ✅ | 筛选视图 ID | `"pH9hbVcCXA"` |
| condition_id | string | ✅ | 筛选条件 ID | `"pH9hbVcCXA"` |
| updateFilterConditionsRequest | UpdateFilterConditionsRequest | ✅ | 更新筛选条件请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // FilterConditionsOpsResult
  }
}
```

**说明**  
更新筛选视图指定列的筛选条件。

**代码示例**  
```csharp
var result = await tenantApi.UpdateFilterConditionsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    "pH9hbVcCXA",
    "conditionId",
    new UpdateFilterConditionsRequest { /* ... */ });
```

---

### GetFilterConditionsAsync  
查询筛选条件列表。查询指定筛选视图的所有筛选条件，包括筛选的类型、比较类型、筛选参数等。

**函数签名**  
```csharp
Task<FeishuApiResult<GetFilterConditionsResult>?> GetFilterConditionsAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Path] string filter_view_id,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| filter_view_id | string | ✅ | 筛选视图 ID | `"pH9hbVcCXA"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // GetFilterConditionsResult
  }
}
```

**说明**  
返回筛选视图的所有筛选条件。

**代码示例**  
```csharp
var result = await tenantApi.GetFilterConditionsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    "pH9hbVcCXA");
```

---

### GetFilterConditionByIdAsync  
获取筛选条件。获取筛选视图指定筛选条件的详细信息。

**函数签名**  
```csharp
Task<FeishuApiResult<GetFilterConditionResult>?> GetFilterConditionByIdAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Path] string filter_view_id,
    [Path] string condition_id,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| filter_view_id | string | ✅ | 筛选视图 ID | `"pH9hbVcCXA"` |
| condition_id | string | ✅ | 筛选条件 ID | `"pH9hbVcCXA"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // GetFilterConditionResult
  }
}
```

**说明**  
返回指定筛选条件的详细信息。

**代码示例**  
```csharp
var result = await tenantApi.GetFilterConditionByIdAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    "pH9hbVcCXA",
    "conditionId");
```

---

### DeleteFilterConditionByIdAsync  
删除筛选条件。删除筛选视图指定列的所有筛选条件。

**函数签名**  
```csharp
Task<FeishuNullDataApiResult?> DeleteFilterConditionByIdAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Path] string filter_view_id,
    [Path] string condition_id,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| filter_view_id | string | ✅ | 筛选视图 ID | `"pH9hbVcCXA"` |
| condition_id | string | ✅ | 筛选条件 ID | `"pH9hbVcCXA"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**  
删除筛选视图指定列的所有筛选条件。

**代码示例**  
```csharp
var result = await tenantApi.DeleteFilterConditionByIdAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    "pH9hbVcCXA",
    "conditionId");
```
