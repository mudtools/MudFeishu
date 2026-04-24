# 电子表格单元格  
**单元格操作 - IFeishuV3SpreadsheetCell**

## 功能描述  
飞书开放平台电子表格工作表中的单元格处理功能。在工作表单元格中进行合并、拆分、查找、替换以及设置样式等操作。

## 参考文档  
- [电子表格概述](https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview)

## 接口变体

| 接口名称 | 认证方式 | 说明 |
| :--- | :--- | :--- |
| `IFeishuTenantV3SpreadsheetCell` | 租户令牌（TenantAccessToken） | 应用身份访问 |
| `IFeishuUserV3SpreadsheetCell` | 用户令牌（UserAccessToken） | 用户身份访问 |

## 函数列表  
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| MergeCellsAsync | 合并单元格 | 租户令牌 / 用户令牌 | POST |
| UnMergeCellsAsync | 拆分单元格 | 租户令牌 / 用户令牌 | POST |
| FindCellsAsync | 查找单元格 | 租户令牌 / 用户令牌 | POST |
| ReplaceCellsAsync | 替换单元格 | 租户令牌 / 用户令牌 | POST |
| SetCellsStyleAsync | 设置单元格样式 | 租户令牌 / 用户令牌 | PUT |
| BatchSetCellsStyleAsync | 批量设置单元格样式 | 租户令牌 / 用户令牌 | PUT |

## 函数详细内容  

### MergeCellsAsync  
合并单元格。合并电子表格工作表中的单元格。

**函数签名**  
```csharp
Task<FeishuApiResult<CellsOpsResult>?> MergeCellsAsync(
    [Path] string spreadsheet_token,
    [Body] MergeCellsRequest mergeCellsRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| mergeCellsRequest | MergeCellsRequest | ✅ | 合并单元格请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // CellsOpsResult
  }
}
```

**说明**  
无特殊限制。

**代码示例**  
```csharp
// 租户令牌方式
var tenantApi = feishuClient.TenantV3SpreadsheetCell;
var result = await tenantApi.MergeCellsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new MergeCellsRequest { /* ... */ });

// 用户令牌方式
var userApi = feishuClient.UserV3SpreadsheetCell;
var result = await userApi.MergeCellsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new MergeCellsRequest { /* ... */ });
```

---

### UnMergeCellsAsync  
拆分单元格。拆分电子表格工作表中的单元格。

**函数签名**  
```csharp
Task<FeishuApiResult<CellsOpsResult>?> UnMergeCellsAsync(
    [Path] string spreadsheet_token,
    [Body] UnMergeCellsRequest unMergeCellsRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| unMergeCellsRequest | UnMergeCellsRequest | ✅ | 拆分单元格请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // CellsOpsResult
  }
}
```

**说明**  
无特殊限制。

**代码示例**  
```csharp
var result = await tenantApi.UnMergeCellsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new UnMergeCellsRequest { /* ... */ });
```

---

### FindCellsAsync  
查找单元格。在指定范围内查找符合查找条件的单元格。

**函数签名**  
```csharp
Task<FeishuApiResult<FindCellsResult>?> FindCellsAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Body] FindCellsRequest findCellsRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| findCellsRequest | FindCellsRequest | ✅ | 查找单元格请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // FindCellsResult
  }
}
```

**说明**  
在指定范围内查找符合查找条件的单元格。

**代码示例**  
```csharp
var result = await tenantApi.FindCellsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    new FindCellsRequest { /* ... */ });
```

---

### ReplaceCellsAsync  
替换单元格。在指定范围内，查找并替换符合查找条件的单元格。

**函数签名**  
```csharp
Task<FeishuApiResult<ReplaceCellsResult>?> ReplaceCellsAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Body] ReplaceCellsRequest replaceCellsRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| replaceCellsRequest | ReplaceCellsRequest | ✅ | 替换单元格请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // ReplaceCellsResult
  }
}
```

**说明**  
在指定范围内，查找并替换符合查找条件的单元格。

**代码示例**  
```csharp
var result = await tenantApi.ReplaceCellsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    new ReplaceCellsRequest { /* ... */ });
```

---

### SetCellsStyleAsync  
设置单元格样式。设置单元格中数据的样式。支持设置字体、背景、边框等样式。

**函数签名**  
```csharp
Task<FeishuApiResult<SetCellsStyleResult>?> SetCellsStyleAsync(
    [Path] string spreadsheet_token,
    [Body] SetCellsStyleRequest setCellsStyleRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| setCellsStyleRequest | SetCellsStyleRequest | ✅ | 设置单元格样式请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // SetCellsStyleResult
  }
}
```

**说明**  
支持设置字体、背景、边框等样式。

**代码示例**  
```csharp
var result = await tenantApi.SetCellsStyleAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new SetCellsStyleRequest { /* ... */ });
```

---

### BatchSetCellsStyleAsync  
批量设置单元格样式。批量设置单元格中数据的样式。支持设置字体、背景、边框等样式。

**函数签名**  
```csharp
Task<FeishuApiResult<BatchSetCellsStyleResult>?> BatchSetCellsStyleAsync(
    [Path] string spreadsheet_token,
    [Body] BatchSetCellsStyleRequest batchSetCellsStyleRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| batchSetCellsStyleRequest | BatchSetCellsStyleRequest | ✅ | 批量设置单元格样式请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // BatchSetCellsStyleResult
  }
}
```

**说明**  
支持批量设置多个单元格的字体、背景、边框等样式。

**代码示例**  
```csharp
var result = await tenantApi.BatchSetCellsStyleAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new BatchSetCellsStyleRequest { /* ... */ });
```
