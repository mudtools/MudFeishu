# 电子表格数据校验  
**数据校验 - IFeishuV2SpreadsheetDataValidation**

## 功能描述  
数据校验用于限制电子表格单元格中的数据类型或用户输入单元格的值。目前，电子表格支持下拉列表相关接口，用于验证数据。

本接口提供创建、更新、获取和删除数据校验（下拉列表）的能力。

## 参考文档  
- [数据校验指南](https://open.feishu.cn/document/server-docs/docs/sheets-v3/datavalidation/datavalidation-guide)

## 接口变体

| 接口名称 | 认证方式 | 说明 |
| :--- | :--- | :--- |
| `IFeishuTenantV2SpreadsheetDataValidation` | 租户令牌（TenantAccessToken） | 应用身份访问 |
| `IFeishuUserV2SpreadsheetDataValidation` | 用户令牌（UserAccessToken） | 用户身份访问 |

## 函数列表  
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateDataValidationAsync | 创建数据验证 | 租户令牌 / 用户令牌 | POST |
| UpdateDataValidationAsync | 更新下拉列表设置 | 租户令牌 / 用户令牌 | PUT |
| GetDataValidationsAsync | 获取数据验证 | 租户令牌 / 用户令牌 | GET |
| DeleteDataValidationAsync | 删除下拉列表设置 | 租户令牌 / 用户令牌 | DELETE |

## 函数详细内容  

### CreateDataValidationAsync  
创建数据验证。在电子表格工作表中设置数据验证。

**函数签名**  
```csharp
Task<FeishuApiResult?> CreateDataValidationAsync(
    [Path] string spreadsheet_token,
    [Body] CreateDataValidationRequest createDataValidationRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| createDataValidationRequest | CreateDataValidationRequest | ✅ | 创建数据验证请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**  
目前支持下拉列表类型的数据验证。

**代码示例**  
```csharp
// 租户令牌方式
var tenantApi = feishuClient.TenantV2SpreadsheetDataValidation;
var result = await tenantApi.CreateDataValidationAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new CreateDataValidationRequest { /* ... */ });

// 用户令牌方式
var userApi = feishuClient.UserV2SpreadsheetDataValidation;
var result = await userApi.CreateDataValidationAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new CreateDataValidationRequest { /* ... */ });
```

---

### UpdateDataValidationAsync  
更新下拉列表设置。更新电子表格工作表中单个下拉列表的设置，支持更新下拉列表的选项和属性，包括是否支持多选、下拉选项的样式等。

**函数签名**  
```csharp
Task<FeishuApiResult<UpdateDataValidationResult>?> UpdateDataValidationAsync(
    [Path] string spreadsheet_token,
    [Path] string sheetId,
    [Body] UpdateDataValidationRequest updateDataValidationRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheetId | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| updateDataValidationRequest | UpdateDataValidationRequest | ✅ | 更新数据验证请求 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // UpdateDataValidationResult
  }
}
```

**说明**  
支持更新下拉列表的选项和属性，包括是否支持多选、下拉选项的样式等。

**代码示例**  
```csharp
var result = await tenantApi.UpdateDataValidationAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    new UpdateDataValidationRequest { /* ... */ });
```

---

### GetDataValidationsAsync  
获取数据验证。查询指定范围中的下拉列表数据验证信息。

**函数签名**  
```csharp
Task<FeishuApiResult<GetDataValidationsResult>?> GetDataValidationsAsync(
    [Path] string spreadsheet_token,
    [Query("range")] string range,
    [Query("dataValidationType")] string dataValidationType,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| range | string | ✅ | 查询范围，格式为 `<sheetId>!<开始位置>:<结束位置>` | `"Q7PlXT!A1:B2"` |
| dataValidationType | string | ✅ | 数据验证类型，取固定值 `"list"` 表示下拉列表 | `"list"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // GetDataValidationsResult
  }
}
```

**说明**  
- range 格式为 `<sheetId>!<开始位置>:<结束位置>`，其中 sheetId 为工作表 ID，`<开始位置>:<结束位置>` 为单元格范围，如 `A2:B2` 表示第 2 行的 A 列到 B 列。
- dataValidationType 目前仅支持 `"list"`（下拉列表）。

**代码示例**  
```csharp
var result = await tenantApi.GetDataValidationsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "Q7PlXT!A1:B2",
    "list");
```

---

### DeleteDataValidationAsync  
删除下拉列表设置。删除电子表格工作表指定范围中下拉列表的设置，但仍保留选项文本。

**函数签名**  
```csharp
Task<FeishuApiResult<DeleteDataValidationResult>?> DeleteDataValidationAsync(
    [Path] string spreadsheet_token,
    [Body] DeleteDataValidationRequest deleteDataValidationRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| deleteDataValidationRequest | DeleteDataValidationRequest | ✅ | 删除数据验证请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // DeleteDataValidationResult
  }
}
```

**说明**  
删除下拉列表设置后，仍保留选项文本。

**代码示例**  
```csharp
var result = await tenantApi.DeleteDataValidationAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new DeleteDataValidationRequest { /* ... */ });
```
