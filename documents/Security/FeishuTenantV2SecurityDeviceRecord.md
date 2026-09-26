# 设备管理 - 租户令牌（FeishuTenantV2SecurityDeviceRecord）

## 接口名称

**设备管理（租户令牌）** -（`IFeishuTenantV2SecurityDeviceRecord`）

## 功能描述

提供以租户身份管理飞书设备记录的能力。飞书安全与合规（Security）「设备管理」SDK 是一组服务端 OpenAPI 的封装，用于在设备管理中新增、查询（分页/单个）、更新、删除设备记录。本接口全部端点为 security_and_compliance/v2，仅支持 tenant_access_token 调用。支持新增设备、查询设备信息、获取设备信息、更新设备、删除设备等操作。

## 参考文档

- [新增设备 - 飞书开放平台](https://open.feishu.cn/document/security_and_compliance-v1/security_and_compliance-v2/device_record/create)

## 函数列表

| 函数名称                 | 功能描述     | 认证方式 | HTTP 方法 |
| ------------------------ | ------------ | -------- | --------- |
| CreateDeviceRecordAsync  | 新增设备     | 租户令牌 | POST      |
| ListDeviceRecordsAsync   | 查询设备信息 | 租户令牌 | GET       |
| GetDeviceRecordAsync     | 获取设备信息 | 租户令牌 | GET       |
| UpdateDeviceRecordAsync  | 更新设备     | 租户令牌 | PUT       |
| DeleteDeviceRecordAsync  | 删除设备     | 租户令牌 | DELETE    |

## 函数详细内容

### 新增设备

在设备管理中新增一台设备，新增设备的类型为管理员导入；设备特征需与操作系统匹配（如 Android 传 android_id、iOS 传 idfv、OpenHarmony 传 aaid）。限频：10 次/秒。所需权限：security_and_compliance:device_record:write（新增、更新、删除设备）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateDeviceRecordResult>?> CreateDeviceRecordAsync(
    [Body] CreateDeviceRecordRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                         | 必填 | 说明                                                                                                                                                                                    |
| --------- | ---------------------------- | ---- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `request` | `CreateDeviceRecordRequest`  | ✅   | 请求体（device_system 操作系统必填 1~6；device_ownership 设备归属必填 0~2；device_status 可信状态必填 0~2；serial_number/disk_serial_number/uuid/mac_address 适用于 Windows/macOS/Linux；android_id 适用于 Android；idfv 适用于 iOS；aaid 适用于 OpenHarmony） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "device_record_id": "7089353870308032531"
  }
}
```

**说明**：新增设备的类型固定为「管理员导入」。设备特征字段必须与 `device_system` 匹配，否则将新增失败。

---

### 查询设备信息

分页查询设备列表信息，支持按设备认证编码、设备名称、各类设备特征、归属、可信状态、MDM 信息等筛选；查询参数采用查询对象模式 `ListDeviceRecordsQuery`，见 AGENTS.md API-2。限频：10 次/秒。所需权限：security_and_compliance:device_record:read（获取设备信息）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<ListDeviceRecordsResult>?> ListDeviceRecordsAsync(
    [Query] ListDeviceRecordsQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型                          | 必填 | 说明                                                                           |
| ------- | ----------------------------- | ---- | ------------------------------------------------------------------------------ |
| `query` | `ListDeviceRecordsQuery?`     | ⚪   | 分页大小（page_size 必填，1~100，默认 100）、分页标记与设备特征等可选查询参数，该对象会整体展开为查询参数 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "device_record_id": "7089353870308032531",
        "version": "0",
        "device_name": "张三的笔记本",
        "device_ownership": 1,
        "device_status": 1
      }
    ],
    "page_token": "next_page_token",
    "has_more": true
  }
}
```

**说明**：`page_size` 为必填项，取值范围 1~100、默认 100。当 `has_more` 为 true 时，可使用返回的 `page_token` 继续拉取下一页数据。

---

### 获取设备信息

在设备管理中获取设备的设备参数、设备归属、设备状态等信息。限频：50 次/秒。所需权限：security_and_compliance:device_record:read（获取设备信息）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetDeviceRecordResult>?> GetDeviceRecordAsync(
    [Path] string device_record_id,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名             | 类型      | 必填 | 说明                                                       |
| ------------------ | --------- | ---- | ---------------------------------------------------------- |
| `device_record_id` | `string`  | ✅   | 设备认证编码，通过查询设备信息接口获取                       |
| `user_id_type`     | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id       |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "device_record": {
      "device_record_id": "7089353870308032531",
      "version": "0",
      "device_name": "张三的笔记本",
      "device_model": "MacBook Pro",
      "device_ownership": 1,
      "device_status": 1,
      "device_system": 2
    }
  }
}
```

**说明**：返回设备认证编码、版本号、设备名称与型号、各类设备特征、归属、可信状态、认证方式、设备指纹与 MDM 信息等完整设备信息。

---

### 更新设备

在设备管理中修改一台设备的设备归属、设备状态等信息。限频：10 次/秒。所需权限：security_and_compliance:device_record:write（新增、更新、删除设备）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UpdateDeviceRecordAsync(
    [Path] string device_record_id,
    [Body] UpdateDeviceRecordRequest request,
    [Query("version")] string version,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名             | 类型                         | 必填 | 说明                                       |
| ------------------ | ---------------------------- | ---- | ------------------------------------------ |
| `device_record_id` | `string`                     | ✅   | 设备认证编码                                |
| `request`          | `UpdateDeviceRecordRequest`  | ✅   | 请求体（device_ownership 设备归属必填 0~2；device_status 可信状态必填 0~2） |
| `version`          | `string`                     | ✅   | 版本号（必填），需与当前设备记录的版本一致   |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：`version` 需与当前设备记录的版本一致，起到乐观锁作用；如版本不一致请先调用获取设备信息接口获取最新版本号。

---

### 删除设备

在设备管理中删除一台设备。限频：10 次/秒。所需权限：security_and_compliance:device_record:write（新增、更新、删除设备）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> DeleteDeviceRecordAsync(
    [Path] string device_record_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名             | 类型     | 必填 | 说明           |
| ------------------ | -------- | ---- | -------------- |
| `device_record_id` | `string` | ✅   | 设备认证编码   |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：删除后该设备将不再受设备管理策略约束，请谨慎操作。
