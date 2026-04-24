# 电子表格浮动图片  
**浮动图片 - IFeishuV3SpreadsheetFloatImage**

## 功能描述  
电子表格浮动图片指悬浮在表格单元格上方的图片。图片大小可自行调整，不会随单元格大小而变化。

单个电子表格最多支持放置 4,000 张不同 token 的图片，即表格内不重复的图片（包括浮动图片和单元格图片）总数不超过 4,000 张。将相同 token 的图片多次放置在表格的不同位置，数量上仅算一张图片。

## 参考文档  
- [浮动图片用户指南](https://open.feishu.cn/document/server-docs/docs/sheets-v3/spreadsheet-sheet-float_image/float-image-user-guide)

## 接口变体

| 接口名称 | 认证方式 | 说明 |
| :--- | :--- | :--- |
| `IFeishuTenantV3SpreadsheetFloatImage` | 租户令牌（TenantAccessToken） | 应用身份访问 |
| `IFeishuUserV3SpreadsheetFloatImage` | 用户令牌（UserAccessToken） | 用户身份访问 |

## 函数列表  
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateFloatImageAsync | 创建浮动图片 | 租户令牌 / 用户令牌 | POST |
| UpdateFloatImageAsync | 更新浮动图片 | 租户令牌 / 用户令牌 | PATCH |
| GetFloatImageAsync | 获取浮动图片 | 租户令牌 / 用户令牌 | GET |
| GetFloatImagesAsync | 查询浮动图片列表 | 租户令牌 / 用户令牌 | GET |
| DeleteFloatImageAsync | 删除浮动图片 | 租户令牌 / 用户令牌 | DELETE |

## 函数详细内容  

### CreateFloatImageAsync  
创建浮动图片。在电子表格工作表的指定位置创建一张浮动图片。

**函数签名**  
```csharp
Task<FeishuApiResult<FloatImageOpsResult>?> CreateFloatImageAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Body] CreateFloatImageRequest createFloatImageRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| createFloatImageRequest | CreateFloatImageRequest | ✅ | 创建浮动图片请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // FloatImageOpsResult
  }
}
```

**说明**  
在指定位置创建浮动图片。

**代码示例**  
```csharp
// 租户令牌方式
var tenantApi = feishuClient.TenantV3SpreadsheetFloatImage;
var result = await tenantApi.CreateFloatImageAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    new CreateFloatImageRequest { /* ... */ });

// 用户令牌方式
var userApi = feishuClient.UserV3SpreadsheetFloatImage;
var result = await userApi.CreateFloatImageAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    new CreateFloatImageRequest { /* ... */ });
```

---

### UpdateFloatImageAsync  
更新浮动图片。更新已有的浮动图片位置和宽高。

**函数签名**  
```csharp
Task<FeishuApiResult<FloatImageOpsResult>?> UpdateFloatImageAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Path] string float_image_id,
    [Body] UpdateFloatImageRequest updateFloatImageRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| float_image_id | string | ✅ | 工作表内浮动图片的唯一标识 | `"ye06SS14ph"` |
| updateFloatImageRequest | UpdateFloatImageRequest | ✅ | 更新浮动图片请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // FloatImageOpsResult
  }
}
```

**说明**  
更新浮动图片的位置和宽高。

**代码示例**  
```csharp
var result = await tenantApi.UpdateFloatImageAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    "ye06SS14ph",
    new UpdateFloatImageRequest { /* ... */ });
```

---

### GetFloatImageAsync  
获取浮动图片。获取电子表格工作表内指定浮动图片的参数信息。

**函数签名**  
```csharp
Task<FeishuApiResult<FloatImageOpsResult>?> GetFloatImageAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Path] string float_image_id,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| float_image_id | string | ✅ | 工作表内浮动图片的唯一标识 | `"ye06SS14ph"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // FloatImageOpsResult
  }
}
```

**说明**  
获取指定浮动图片的参数信息。

**代码示例**  
```csharp
var result = await tenantApi.GetFloatImageAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    "ye06SS14ph");
```

---

### GetFloatImagesAsync  
查询浮动图片列表。获取电子表格工作表内所有的浮动图片的参数信息。

**函数签名**  
```csharp
Task<FeishuApiResult<GetFloatImagesResult>?> GetFloatImagesAsync(
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
    // GetFloatImagesResult
  }
}
```

**说明**  
获取工作表内所有浮动图片的参数信息。

**代码示例**  
```csharp
var result = await tenantApi.GetFloatImagesAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6");
```

---

### DeleteFloatImageAsync  
删除浮动图片。删除电子表格工作表内指定的浮动图片。

**函数签名**  
```csharp
Task<FeishuNullDataApiResult?> DeleteFloatImageAsync(
    [Path] string spreadsheet_token,
    [Path] string sheet_id,
    [Path] string float_image_id,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| sheet_id | string | ✅ | 工作表的 ID | `"2jm6f6"` |
| float_image_id | string | ✅ | 工作表内浮动图片的唯一标识 | `"ye06SS14ph"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**  
删除指定的浮动图片。

**代码示例**  
```csharp
var result = await tenantApi.DeleteFloatImageAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "2jm6f6",
    "ye06SS14ph");
```
