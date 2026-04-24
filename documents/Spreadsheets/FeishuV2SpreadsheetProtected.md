# 电子表格数据保护  
**数据保护 - IFeishuV2SpreadsheetProtected**

## 功能描述  
电子表格数据保护用于设置电子表格保护范围，指对工作表中的任意行或列进行保护，并可设置其他协作者是否有权限编辑该数据，有效保障数据信息安全。

本接口提供增加、修改、获取和删除保护范围的能力。

## 参考文档  
- [电子表格概述](https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview)

## 接口变体

| 接口名称 | 认证方式 | 说明 |
| :--- | :--- | :--- |
| `IFeishuTenantV2SpreadsheetProtected` | 租户令牌（TenantAccessToken） | 应用身份访问 |
| `IFeishuUserV2SpreadsheetProtected` | 用户令牌（UserAccessToken） | 用户身份访问 |

## 函数列表  
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateProtectedAsync | 增加保护范围 | 租户令牌 / 用户令牌 | POST |
| UpdateProtectedAsync | 修改保护范围 | 租户令牌 / 用户令牌 | POST |
| GetProtectedAsync | 获取保护范围 | 租户令牌 / 用户令牌 | GET |
| DeleteProtectedAsync | 删除保护范围 | 租户令牌 / 用户令牌 | DELETE |

## 函数详细内容  

### CreateProtectedAsync  
增加保护范围。在电子表格工作表中设置多个保护范围，支持对行或列设置保护范围。

**函数签名**  
```csharp
Task<FeishuApiResult<CreateProtectedResult>?> CreateProtectedAsync(
    [Path] string spreadsheet_token,
    [Body] CreateProtectedRequest createProtectedRequest,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| createProtectedRequest | CreateProtectedRequest | ✅ | 创建保护范围的请求体 | — |
| user_id_type | string? | ⚪ | 请求体中 users 字段对应的用户 ID 类型 | `"open_id"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // CreateProtectedResult
  }
}
```

**说明**  
支持对行或列设置保护范围，并可设置其他协作者是否有权限编辑该数据。

**代码示例**  
```csharp
// 租户令牌方式
var tenantApi = feishuClient.TenantV2SpreadsheetProtected;
var result = await tenantApi.CreateProtectedAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new CreateProtectedRequest { /* ... */ });

// 用户令牌方式
var userApi = feishuClient.UserV2SpreadsheetProtected;
var result = await userApi.CreateProtectedAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new CreateProtectedRequest { /* ... */ });
```

---

### UpdateProtectedAsync  
修改保护范围。修改电子表格工作表中指定的保护范围。

**函数签名**  
```csharp
Task<FeishuApiResult<UpdateProtectedResult>?> UpdateProtectedAsync(
    [Path] string spreadsheet_token,
    [Body] UpdateProtectedRequest updateProtectedRequest,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| updateProtectedRequest | UpdateProtectedRequest | ✅ | 修改保护范围的请求体 | — |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // UpdateProtectedResult
  }
}
```

**说明**  
可修改保护范围的行列范围、可编辑用户等信息。

**代码示例**  
```csharp
var result = await tenantApi.UpdateProtectedAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    new UpdateProtectedRequest { /* ... */ });
```

---

### GetProtectedAsync  
获取保护范围。获取电子表格工作表中指定保护范围的信息，包括保护的行列索引、支持编辑的用户 ID、保护范围的备注等。

**函数签名**  
```csharp
Task<FeishuApiResult<GetProtectedResult>?> GetProtectedAsync(
    [Path] string spreadsheet_token,
    [Query("protectIds")] string protectIds,
    [Query("memberType")] string? memberType = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| protectIds | string | ✅ | 要获取的保护范围的 ID 列表，多个 ID 之间逗号分隔 | `"7379738014546812456,7379738014546812456"` |
| memberType | string? | ⚪ | 返回的用户 ID 的类型，默认为 `userId`，建议选择 `openId` | `"open_id"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // GetProtectedResult
  }
}
```

**说明**  
返回保护范围的行列索引、支持编辑的用户 ID、保护范围的备注等信息。

**代码示例**  
```csharp
var result = await tenantApi.GetProtectedAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "7379738014546812456,7379738014546812457");
```

---

### DeleteProtectedAsync  
删除保护范围。根据保护范围 ID 删除保护范围。

**函数签名**  
```csharp
Task<FeishuApiResult<DeleteProtectedResult>?> DeleteProtectedAsync(
    [Path] string spreadsheet_token,
    [Query("protectIds")] string protectIds,
    CancellationToken cancellationToken = default);
```

**认证**  
租户令牌 / 用户令牌

**参数**  
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| spreadsheet_token | string | ✅ | 电子表格的 token | `"Iow7sNNEphp3WbtnbCscPqabcef"` |
| protectIds | string | ✅ | 要删除的保护范围的 ID 列表，多个 ID 之间逗号分隔 | `"7379738014546812456,7379738014546812456"` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | — |

**响应**  
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    // DeleteProtectedResult
  }
}
```

**说明**  
根据保护范围 ID 删除保护范围，支持一次删除多个保护范围。

**代码示例**  
```csharp
var result = await tenantApi.DeleteProtectedAsync(
    "Iow7sNNEphp3WbtnbCscPqabcef",
    "7379738014546812456,7379738014546812457");
```
