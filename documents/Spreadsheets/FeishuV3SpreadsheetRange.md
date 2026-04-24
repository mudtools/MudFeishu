# 电子表格范围  
**行列范围操作 - IFeishuV3SpreadsheetRange**

## 功能描述  
范围（range）：在工作表中进行读取数据、写入数据、筛选数据等各类操作时，需要通过范围 range 参数指定操作数据的范围。

range 参数的格式为 `<sheetId>!<开始位置>:<结束位置>`。其中：
- `sheetId` 为工作表的唯一标识，通过获取工作表获取。
- `<开始位置>:<结束位置>` 为工作表中单元格的范围，使用数字表示行索引，字母表示列索引。如 `A2:B2` 表示该工作表第 2 行的 A 列到 B 列。

本接口提供行列的增加、插入、更新、移动和删除操作。

## 参考文档  
- [电子表格概述](https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview)

## 接口变体

| 接口名称 | 认证方式 | 说明 |
| :--- | :--- | :--- |
| `IFeishuTenantV3SpreadsheetRange` | 租户令牌（TenantAccessToken） | 应用身份访问 |
| `IFeishuUserV3SpreadsheetRange` | 用户令牌（UserAccessToken） | 用户身份访问 |

## 函数列表  
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateRangeAsync | 增加行列 | 租户令牌 / 用户令牌 | POST |
| InsertRangeAsync | 插入行列 | 租户令牌 / 用户令牌 | POST |
| UpdateRangeAsync | 更新行列 | 租户令牌 / 用户令牌 | PUT |
| MoveRangeAsync | 移动行列 | 租户令牌 / 用户令牌 | POST |
| DeleteRangeAsync | 删除行列 | 租户令牌 / 用户令牌 | POST |

## 函数详细内容  

### CreateRangeAsync  
增加行列。用于在电子表格工作表中增加空白行或列。

**函数签名**  
```csharp
Task<FeishuApiResult<CreateRangeResult>?> CreateRangeAsync(
    [Path] string spreadsheet_token,
    [Body] CreateRangeRequest createRangeRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| createRangeRequest | CreateRangeRequest | ✅ | 增加行列请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // CreateRangeResult
  }
}
```

**说明**  
在电子表格工作表末尾增加空白行或列。

**代码示例**  
```csharp
// 租户令牌方式
var tenantApi = feishuClient.TenantV3SpreadsheetRange;
var result = await tenantApi.CreateRangeAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new CreateRangeRequest { /* ... */ });

// 用户令牌方式
var userApi = feishuClient.UserV3SpreadsheetRange;
var result = await userApi.CreateRangeAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new CreateRangeRequest { /* ... */ });
```

---

### InsertRangeAsync  
插入行列。用于在电子表格的指定位置插入空白行或列。

**函数签名**  
```csharp
Task<FeishuNullDataApiResult?> InsertRangeAsync(
    [Path] string spreadsheet_token,
    [Body] InsertRangeRequest insertRangeRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| insertRangeRequest | InsertRangeRequest | ✅ | 插入行列请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**  
在指定位置插入空白行或列，原有数据会被推移。

**代码示例**  
```csharp
var result = await tenantApi.InsertRangeAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new InsertRangeRequest { /* ... */ });
```

---

### UpdateRangeAsync  
更新行列。用于更新设置电子表格中行列的属性，包括是否隐藏行列和设置行高列宽。

**函数签名**  
```csharp
Task<FeishuNullDataApiResult?> UpdateRangeAsync(
    [Path] string spreadsheet_token,
    [Body] UpdateRangeRequest insertRangeRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| insertRangeRequest | UpdateRangeRequest | ✅ | 更新行列请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**  
支持设置是否隐藏行列和设置行高列宽。

**代码示例**  
```csharp
var result = await tenantApi.UpdateRangeAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new UpdateRangeRequest { /* ... */ });
```

---

### MoveRangeAsync  
移动行列。用于移动行或列。行或列被移动到目标位置后，原本在目标位置的行列会对应右移或下移。

**函数签名**  
```csharp
Task<FeishuNullDataApiResult?> MoveRangeAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Body] MoveRangeRequest moveRangeRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| moveRangeRequest | MoveRangeRequest | ✅ | 移动行列请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**  
行列被移动到目标位置后，原本在目标位置的行列会对应右移或下移。

**代码示例**  
```csharp
var result = await tenantApi.MoveRangeAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    new MoveRangeRequest { /* ... */ });
```

---

### DeleteRangeAsync  
删除行列。用于删除电子表格中的指定行或列。

**函数签名**  
```csharp
Task<FeishuNullDataApiResult?> DeleteRangeAsync(
    [Path] string spreadsheet_token,
    [Body] DeleteRangeRequest deleteRangeRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| deleteRangeRequest | DeleteRangeRequest | ✅ | 删除行列请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**  
删除指定行或列，后续行列会自动前移。

**代码示例**  
```csharp
var result = await tenantApi.DeleteRangeAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new DeleteRangeRequest { /* ... */ });
```
