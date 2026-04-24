# 电子表格  
**电子表格与工作表 - IFeishuV3Spreadsheets**

## 功能描述  
飞书开放平台电子表格分为表格（spreadsheet）、工作表（sheet）和范围（range）。

- **表格**是承载数据的容器，提供数据处理、展示、分析的功能。一个表格包含一个或多个工作表。每个表格都有一个 spreadsheetToken 作为唯一标识。
- **工作表（sheet）**是表格中的单独页面。每个工作表都有自己的行和列，形成一个网格，用于组织和存储数据。每一个工作表都有唯一的 sheetId 作为标识。
- 在工作表中进行读取数据、写入数据、筛选数据等各类操作时，需要通过范围 range 参数指定操作数据的范围。

本接口提供电子表格和工作表的创建、修改、查询以及工作表属性更新等操作。

## 参考文档  
- [电子表格概述](https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview)

## 接口变体

| 接口名称 | 认证方式 | 说明 |
| :--- | :--- | :--- |
| `IFeishuTenantV3Spreadsheets` | 租户令牌（TenantAccessToken） | 应用身份访问 |
| `IFeishuUserV3Spreadsheets` | 用户令牌（UserAccessToken） | 用户身份访问 |

## 函数列表  
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateSpreadsheetAsync | 创建电子表格 | 租户令牌 / 用户令牌 | POST |
| PatchSpreadsheetAsync | 修改电子表格属性 | 租户令牌 / 用户令牌 | PATCH |
| GetSpreadsheetByTokenAsync | 获取电子表格信息 | 租户令牌 / 用户令牌 | GET |
| BatchUpdateSheetAsync | 操作工作表 | 租户令牌 / 用户令牌 | POST |
| BatchUpdateSheetPropertiesAsync | 更新工作表属性 | 租户令牌 / 用户令牌 | POST |
| GetSpreadsheetSheetsByTokenAsync | 获取所有工作表 | 租户令牌 / 用户令牌 | GET |
| GetSpreadsheetSheetBySheetIdAsync | 查询工作表 | 租户令牌 / 用户令牌 | GET |

## 函数详细内容  

### CreateSpreadsheetAsync  
创建电子表格。在云空间指定目录下创建电子表格。可自定义表格标题。不支持带内容创建表格。

**函数签名**  
```csharp
Task<FeishuApiResult<CreateSpreadsheetResult>?> CreateSpreadsheetAsync(
    [Body] CreateSpreadsheetRequest createSpreadsheetRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| createSpreadsheetRequest | CreateSpreadsheetRequest | ✅ | 创建电子表格请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // CreateSpreadsheetResult
  }
}
```

**说明**  
不支持带内容创建表格，仅可自定义表格标题和在指定目录下创建。

**代码示例**  
```csharp
// 租户令牌方式
var tenantApi = feishuClient.TenantV3Spreadsheets;
var result = await tenantApi.CreateSpreadsheetAsync(
    new CreateSpreadsheetRequest { Title = "新建表格" });

// 用户令牌方式
var userApi = feishuClient.UserV3Spreadsheets;
var result = await userApi.CreateSpreadsheetAsync(
    new CreateSpreadsheetRequest { Title = "新建表格" });
```

---

### PatchSpreadsheetAsync  
修改电子表格属性。用于修改电子表格的属性。目前支持修改电子表格标题。

**函数签名**  
```csharp
Task<FeishuNullDataApiResult?> PatchSpreadsheetAsync(
    [Path] string spreadsheet_token,
    [Body] PatchSpreadsheetRequest patchSpreadsheetRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| patchSpreadsheetRequest | PatchSpreadsheetRequest | ✅ | 修改电子表格属性请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**  
目前支持修改电子表格标题。

**代码示例**  
```csharp
var result = await tenantApi.PatchSpreadsheetAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new PatchSpreadsheetRequest { /* ... */ });
```

---

### GetSpreadsheetByTokenAsync  
根据电子表格 token 获取电子表格的基础信息，包括电子表格的所有者、URL 链接等。

**函数签名**  
```csharp
Task<FeishuApiResult<GetSpreadsheetResult>?> GetSpreadsheetByTokenAsync(
    [Path] string spreadsheet_token,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| user_id_type | string? | ⚪ | 用户 ID 类型 | `"open_id"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // GetSpreadsheetResult
  }
}
```

**说明**  
返回电子表格的所有者、URL 链接等基础信息。

**代码示例**  
```csharp
var result = await tenantApi.GetSpreadsheetByTokenAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef");
```

---

### BatchUpdateSheetAsync  
操作工作表。根据电子表格的 token 对工作表进行操作，包括增加工作表、复制工作表、删除工作表。

**函数签名**  
```csharp
Task<FeishuApiResult<BatchUpdateSheetResult>?> BatchUpdateSheetAsync(
    [Path] string spreadsheet_token,
    [Body] BatchUpdateSheetRequest batchUpdateSheetRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| batchUpdateSheetRequest | BatchUpdateSheetRequest | ✅ | 操作工作表请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // BatchUpdateSheetResult
  }
}
```

**说明**  
支持增加工作表、复制工作表、删除工作表。

**代码示例**  
```csharp
var result = await tenantApi.BatchUpdateSheetAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new BatchUpdateSheetRequest { /* ... */ });
```

---

### BatchUpdateSheetPropertiesAsync  
更新工作表属性。更新电子表格中的工作表。支持更新工作表的标题、位置，和隐藏、冻结、保护等属性。

**函数签名**  
```csharp
Task<FeishuApiResult<BatchUpdateSheetPropertiesResult>?> BatchUpdateSheetPropertiesAsync(
    [Path] string spreadsheet_token,
    [Body] BatchUpdateSheetPropertiesRequest batchUpdateSheetPropertiesRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| batchUpdateSheetPropertiesRequest | BatchUpdateSheetPropertiesRequest | ✅ | 更新工作表属性请求体 | — |
| user_id_type | string? | ⚪ | 用户 ID 类型 | `"open_id"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // BatchUpdateSheetPropertiesResult
  }
}
```

**说明**  
支持更新工作表的标题、位置，和隐藏、冻结、保护等属性。

**代码示例**  
```csharp
var result = await tenantApi.BatchUpdateSheetPropertiesAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new BatchUpdateSheetPropertiesRequest { /* ... */ });
```

---

### GetSpreadsheetSheetsByTokenAsync  
获取电子表格的所有工作表。根据电子表格 token 获取电子表格的基础信息，包括电子表格的所有者、URL 链接等。

**函数签名**  
```csharp
Task<FeishuApiResult<GetSpreadsheetSheetsResult>?> GetSpreadsheetSheetsByTokenAsync(
    [Path] string spreadsheet_token,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // GetSpreadsheetSheetsResult
  }
}
```

**说明**  
返回电子表格中所有工作表的信息。

**代码示例**  
```csharp
var result = await tenantApi.GetSpreadsheetSheetsByTokenAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef");
```

---

### GetSpreadsheetSheetBySheetIdAsync  
查询电子表格中的工作表。根据工作表 ID 查询工作表属性信息，包括工作表的标题、索引位置、是否被隐藏等。

**函数签名**  
```csharp
Task<FeishuApiResult<GetSpreadsheetSheetResult>?> GetSpreadsheetSheetBySheetIdAsync(
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
    // GetSpreadsheetSheetResult
  }
}
```

**说明**  
返回工作表的标题、索引位置、是否被隐藏等属性信息。

**代码示例**  
```csharp
var result = await tenantApi.GetSpreadsheetSheetBySheetIdAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6");
```
