# 电子表格数据  
**数据读写 - IFeishuV3SpreadsheetData**

## 功能描述  
飞书开放平台电子表格工作表中的数据处理相关功能。在工作表中进行读取数据、写入数据、追加数据、写入图片等各类操作。

## 参考文档  
- [电子表格概述](https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview)

## 接口变体

| 接口名称 | 认证方式 | 说明 |
| :--- | :--- | :--- |
| `IFeishuTenantV3SpreadsheetData` | 租户令牌（TenantAccessToken） | 应用身份访问 |
| `IFeishuUserV3SpreadsheetData` | 用户令牌（UserAccessToken） | 用户身份访问 |

## 函数列表  
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| InsertDataAsync | 插入数据 | 租户令牌 / 用户令牌 | POST |
| AppendDataAsync | 追加数据 | 租户令牌 / 用户令牌 | POST |
| ImageDataAsync | 写入图片 | 租户令牌 / 用户令牌 | POST |
| GetRangeDataAsync | 读取单个范围 | 租户令牌 / 用户令牌 | GET |
| GetRangesDataAsync | 读取多个范围 | 租户令牌 / 用户令牌 | GET |
| RangeWriteDataAsync | 向单个范围写入数据 | 租户令牌 / 用户令牌 | PUT |
| RangesWriteDataAsync | 向多个范围写入数据 | 租户令牌 / 用户令牌 | POST |

## 函数详细内容  

### InsertDataAsync  
插入数据。在电子表格工作表的指定范围的起始位置上方增加若干行，并在该范围中填充数据。

**函数签名**  
```csharp
Task<FeishuApiResult<RangeDataOpsResult>?> InsertDataAsync(
    [Path] string spreadsheet_token,
    [Body] RangeDataOpsRequest insertDataRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| insertDataRequest | RangeDataOpsRequest | ✅ | 插入数据请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // RangeDataOpsResult
  }
}
```

**说明**  
在指定范围的起始位置上方增加若干行，并在该范围中填充数据。

**代码示例**  
```csharp
// 租户令牌方式
var tenantApi = feishuClient.TenantV3SpreadsheetData;
var result = await tenantApi.InsertDataAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new RangeDataOpsRequest { /* ... */ });

// 用户令牌方式
var userApi = feishuClient.UserV3SpreadsheetData;
var result = await userApi.InsertDataAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new RangeDataOpsRequest { /* ... */ });
```

---

### AppendDataAsync  
追加数据。在电子表格工作表的指定范围中，在空白位置中追加数据。

**函数签名**  
```csharp
Task<FeishuApiResult<RangeDataOpsResult>?> AppendDataAsync(
    [Path] string spreadsheet_token,
    [Body] RangeDataOpsRequest appendDataRequest,
    [Query("insertDataOption")] string? insertDataOption = "OVERWRITE",
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| appendDataRequest | RangeDataOpsRequest | ✅ | 追加数据请求体 | — |
| insertDataOption | string? | ⚪ | 追加数据的方式，默认 `OVERWRITE` | `"OVERWRITE"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // RangeDataOpsResult
  }
}
```

**说明**  
- insertDataOption 可选值：
  - `OVERWRITE`：若空行数量小于追加数据的行数，则会覆盖已有数据
  - `INSERT_ROWS`：插入足够数量的行后再进行数据追加

**代码示例**  
```csharp
var result = await tenantApi.AppendDataAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new RangeDataOpsRequest { /* ... */ },
    insertDataOption: "INSERT_ROWS");
```

---

### ImageDataAsync  
写入图片。向电子表格某个工作表的单个指定单元格写入图片，支持传入图片的二进制流，支持多种图片格式。

**函数签名**  
```csharp
Task<FeishuApiResult<ImageDataOpsResult>?> ImageDataAsync(
    [Path] string spreadsheet_token,
    [Body] ImageDataOpsRequest imageDataOpsRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| imageDataOpsRequest | ImageDataOpsRequest | ✅ | 写入图片请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // ImageDataOpsResult
  }
}
```

**说明**  
支持传入图片的二进制流，支持多种图片格式。

**代码示例**  
```csharp
var result = await tenantApi.ImageDataAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new ImageDataOpsRequest { /* ... */ });
```

---

### GetRangeDataAsync  
读取单个范围。读取电子表格中单个指定范围的数据。

**函数签名**  
```csharp
Task<FeishuApiResult<GetRangeDataResult>?> GetRangeDataAsync(
    [Path] string spreadsheet_token,
    [Path] string range,
    [Query("valueRenderOption")] string? valueRenderOption = null,
    [Query("dateTimeRenderOption")] string? dateTimeRenderOption = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| range | string | ✅ | 查询范围，格式为 `<sheetId>!<开始位置>:<结束位置>` | `"Q7PlXT!A1:B2"` |
| valueRenderOption | string? | ⚪ | 单元格数据格式 | `"FormattedValue"` |
| dateTimeRenderOption | string? | ⚪ | 日期时间数据格式 | `"FormattedString"` |
| user_id_type | string? | ⚪ | 用户 ID 类型 | `"open_id"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // GetRangeDataResult
  }
}
```

**说明**  
- range 格式为 `<sheetId>!<开始位置>:<结束位置>`，如 `A2:B2` 表示第 2 行的 A 列到 B 列。
- valueRenderOption 可选值：`ToString`（纯文本）、`Formula`（公式本身）、`FormattedValue`（计算并格式化）、`UnformattedValue`（计算不格式化）
- dateTimeRenderOption 可选值：`FormattedString`（格式化后的字符串），不传则返回浮点数值
- 若使用 `<sheetId>!<开始单元格>:<结束列>` 和 `<sheetId>!<开始列>:<结束列>` 的写法时，仅支持获取 100 列数据。

**代码示例**  
```csharp
var result = await tenantApi.GetRangeDataAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "Q7PlXT!A1:B2",
    valueRenderOption: "FormattedValue",
    dateTimeRenderOption: "FormattedString");
```

---

### GetRangesDataAsync  
读取多个范围。读取电子表格中多个指定范围的数据。

**函数签名**  
```csharp
Task<FeishuApiResult<GetRangesDataResult>?> GetRangesDataAsync(
    [Path] string spreadsheet_token,
    [Query("ranges")] string ranges,
    [Query("valueRenderOption")] string? valueRenderOption = null,
    [Query("dateTimeRenderOption")] string? dateTimeRenderOption = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| ranges | string | ✅ | 多个查询范围，逗号分隔 | `"Q7PlXT!A2:B6,0b6377!B1:C8"` |
| valueRenderOption | string? | ⚪ | 单元格数据格式 | `"FormattedValue"` |
| dateTimeRenderOption | string? | ⚪ | 日期时间数据格式 | `"FormattedString"` |
| user_id_type | string? | ⚪ | 用户 ID 类型 | `"open_id"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // GetRangesDataResult
  }
}
```

**说明**  
ranges 参数为多个范围，使用逗号分隔，如 `Q7PlXT!A2:B6,0b6377!B1:C8`。

**代码示例**  
```csharp
var result = await tenantApi.GetRangesDataAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "Q7PlXT!A2:B6,0b6377!B1:C8");
```

---

### RangeWriteDataAsync  
向单个范围写入数据。向电子表格某个工作表的单个指定范围中写入数据。若指定范围内已有数据，将被新写入的数据覆盖。

**函数签名**  
```csharp
Task<FeishuApiResult<CellsUpdate>?> RangeWriteDataAsync(
    [Path] string spreadsheet_token,
    [Body] RangeDataOpsRequest rangeDataRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| rangeDataRequest | RangeDataOpsRequest | ✅ | 指定工作表的范围和写入的数据请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // CellsUpdate
  }
}
```

**说明**  
若指定范围内已有数据，将被新写入的数据覆盖。

**代码示例**  
```csharp
var result = await tenantApi.RangeWriteDataAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new RangeDataOpsRequest { /* ... */ });
```

---

### RangesWriteDataAsync  
向多个范围写入数据。向电子表格某个工作表的多个指定范围中写入数据。若指定范围已有数据，将被新写入的数据覆盖。

**函数签名**  
```csharp
Task<FeishuApiResult<RangesWriteDataResult>?> RangesWriteDataAsync(
    [Path] string spreadsheet_token,
    [Body] RangesDataOpsRequest rangesDataRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| rangesDataRequest | RangesDataOpsRequest | ✅ | 写入多个范围数据请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // RangesWriteDataResult
  }
}
```

**说明**  
若指定范围已有数据，将被新写入的数据覆盖。

**代码示例**  
```csharp
var result = await tenantApi.RangesWriteDataAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new RangesDataOpsRequest { /* ... */ });
```
