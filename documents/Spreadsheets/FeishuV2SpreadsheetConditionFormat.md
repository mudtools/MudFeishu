# 电子表格条件格式  
**条件格式 - IFeishuV2SpreadsheetConditionFormat**

## 功能描述  
电子表格条件格式用于根据指定的条件更改单元格的外观格式。目前，电子表格单个工作表中最多支持设置 20 个条件格式。

本接口提供批量创建、批量更新、批量获取和批量删除条件格式的能力。支持跨工作表操作多个条件格式。

## 参考文档  
- [条件格式指南](https://open.feishu.cn/document/server-docs/docs/sheets-v3/conditionformat/condition-format-guide)

## 接口变体

| 接口名称 | 认证方式 | 说明 |
| :--- | :--- | :--- |
| `IFeishuTenantV2SpreadsheetConditionFormat` | 租户令牌（TenantAccessToken） | 应用身份访问 |
| `IFeishuUserV2SpreadsheetConditionFormat` | 用户令牌（UserAccessToken） | 用户身份访问 |

## 函数列表  
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateConditionFormatsAsync | 批量创建条件格式 | 租户令牌 / 用户令牌 | POST |
| UpdateConditionFormatsAsync | 批量更新条件格式 | 租户令牌 / 用户令牌 | POST |
| GetConditionFormatsAsync | 批量获取条件格式 | 租户令牌 / 用户令牌 | GET |
| DeleteConditionFormatsAsync | 批量删除条件格式 | 租户令牌 / 用户令牌 | DELETE |

## 函数详细内容  

### CreateConditionFormatsAsync  
批量创建条件格式。在电子表格工作表的指定区域中，为满足指定条件的单元格和单元格中的数据设置样式。支持跨工作表创建多个条件格式。

**函数签名**  
```csharp
Task<FeishuApiResult<ConditionFormatOpsResult>?> CreateConditionFormatsAsync(
    [Path] string spreadsheet_token,
    [Body] CreateConditionFormatRequest createConditionFormatRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| createConditionFormatRequest | CreateConditionFormatRequest | ✅ | 批量创建条件格式请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // ConditionFormatOpsResult
  }
}
```

**说明**  
单个工作表中最多支持设置 20 个条件格式。

**代码示例**  
```csharp
// 租户令牌方式
var tenantApi = feishuClient.TenantV2SpreadsheetConditionFormat;
var result = await tenantApi.CreateConditionFormatsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new CreateConditionFormatRequest { /* ... */ });

// 用户令牌方式
var userApi = feishuClient.UserV2SpreadsheetConditionFormat;
var result = await userApi.CreateConditionFormatsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new CreateConditionFormatRequest { /* ... */ });
```

---

### UpdateConditionFormatsAsync  
批量更新条件格式。更新已有的条件格式。支持跨工作表更新多个条件格式。该接口为全量更新接口，若非必填参数不传值，将改变原有配置。

**函数签名**  
```csharp
Task<FeishuApiResult<ConditionFormatOpsResult>?> UpdateConditionFormatsAsync(
    [Path] string spreadsheet_token,
    [Body] UpdateConditionFormatRequest updateConditionFormatRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| updateConditionFormatRequest | UpdateConditionFormatRequest | ✅ | 批量更新条件格式请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // ConditionFormatOpsResult
  }
}
```

**说明**  
该接口为全量更新接口，若非必填参数不传值，将改变原有配置。

**代码示例**  
```csharp
var result = await tenantApi.UpdateConditionFormatsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new UpdateConditionFormatRequest { /* ... */ });
```

---

### GetConditionFormatsAsync  
批量获取条件格式。根据工作表 ID 获取详细的条件格式信息，最多支持同时查询 10 个工作表的条件格式。

**函数签名**  
```csharp
Task<FeishuApiResult<GetConditionFormatsResult>?> GetConditionFormatsAsync(
    [Path] string spreadsheet_token,
    [Query("sheet_ids")] string sheet_ids,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_ids | string | ✅ | 工作表 ID，多个 ID 使用逗号分隔 | `"xxxID1,xxxID2"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // GetConditionFormatsResult
  }
}
```

**说明**  
最多支持同时查询 10 个工作表的条件格式。

**代码示例**  
```csharp
var result = await tenantApi.GetConditionFormatsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "sheetId1,sheetId2");
```

---

### DeleteConditionFormatsAsync  
批量删除条件格式。删除已有的条件格式。支持跨工作表删除多个条件格式。

**函数签名**  
```csharp
Task<FeishuApiResult<ConditionFormatOpsResult>?> DeleteConditionFormatsAsync(
    [Path] string spreadsheet_token,
    [Body] DeleteConditionFormatsRequest deleteConditionFormatsRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| deleteConditionFormatsRequest | DeleteConditionFormatsRequest | ✅ | 删除条件格式请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // ConditionFormatOpsResult
  }
}
```

**说明**  
支持跨工作表删除多个条件格式。

**代码示例**  
```csharp
var result = await tenantApi.DeleteConditionFormatsAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new DeleteConditionFormatsRequest { /* ... */ });
```
