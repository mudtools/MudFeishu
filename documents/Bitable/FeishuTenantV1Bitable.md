# 多维表格 - 租户令牌（FeishuTenantV1Bitable）

## 接口名称

**多维表格（租户令牌）** -（`IFeishuTenantV1Bitable`）

## 功能描述

提供以租户身份管理飞书多维表格的能力。飞书多维表格（Base）是全新的业务管理工具，帮助用户重构工作应用和团队协同模式，高效在线协同数据，随心构建个性化应用，轻松掌控全盘业务数据，和团队一起创造效率的无限可能。多维表格可以是一个表格，也可以是无数个应用。它拥有强大的底层开放能力，你可以通过多维表格 API 轻松打通内部其他业务系统，让业务数据通畅流转，实时同步。支持创建、复制、获取和更新多维表格等操作。

## 参考文档

- [多维表格概述 - 飞书开放平台](https://open.feishu.cn/document/server-docs/docs/bitable-v1/bitable-overview)

## 函数列表

| 函数名称                | 功能描述         | 认证方式 | HTTP 方法 |
| ----------------------- | ---------------- | -------- | --------- |
| CreateBitableAppAsync   | 创建多维表格     | 租户令牌 | POST      |
| CopyBitableAppAsync     | 复制多维表格     | 租户令牌 | POST      |
| GetBitableAppInfoAsync  | 获取多维表格元数据 | 租户令牌 | GET       |
| UpdateBitableAppAsync   | 更新多维表格元数据 | 租户令牌 | PUT       |

## 函数详细内容

### 创建多维表格

在指定文件夹中创建一个多维表格，包含一个空白的数据表。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateBitableAppResult>?> CreateBitableAppAsync(
    [Body] CreateBitableAppRequest createAppRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名            | 类型                     | 必填 | 说明                     |
| ----------------- | ------------------------ | ---- | ------------------------ |
| `createAppRequest` | `CreateBitableAppRequest` | ✅   | 创建多维表格应用请求体   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "app": {
      "app_token": "AW3Qbtr2cakCnesXzXVbbsrIcVT",
      "name": "新建多维表格",
      "revision": 1
    }
  }
}
```

**说明**：创建多维表格时会在指定文件夹中生成一个包含空白数据表的多维表格应用。

---

### 复制多维表格

复制一个多维表格，可以指定复制到某个有权限的文件夹下。

**函数签名**：

```csharp
Task<FeishuApiResult<CopyBitableResult>?> CopyBitableAppAsync(
    [Path] string app_token,
    [Body] CopyBitableAppRequest copyBitableAppRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                  | 类型                     | 必填 | 说明                                                                 |
| ----------------------- | ------------------------ | ---- | -------------------------------------------------------------------- |
| `app_token`             | `string`                 | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`       |
| `copyBitableAppRequest` | `CopyBitableAppRequest`  | ✅   | 复制多维表格应用请求体                                               |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "app": {
      "app_token": "AW3Qbtr2cakCnesXzXVbbsrIcVT",
      "name": "复制的多维表格",
      "revision": 1
    }
  }
}
```

**说明**：复制多维表格时可以指定目标文件夹，需确保对目标文件夹有写入权限。

---

### 获取多维表格元数据

获取指定多维表格的元数据信息，包括多维表格名称、多维表格版本号、多维表格是否开启高级权限等。

**函数签名**：

```csharp
Task<FeishuApiResult<GetBitableAppResult>?> GetBitableAppInfoAsync(
    [Path] string app_token,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名      | 类型     | 必填 | 说明                                                           |
| ----------- | -------- | ---- | -------------------------------------------------------------- |
| `app_token` | `string` | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "app": {
      "app_token": "AW3Qbtr2cakCnesXzXVbbsrIcVT",
      "name": "多维表格名称",
      "revision": 1,
      "is_advanced_permission": false
    }
  }
}
```

**说明**：返回多维表格的基本元数据信息，包括名称、版本号和高级权限状态等。

---

### 更新多维表格元数据

更新多维表格元数据，包括多维表格的名称、是否开启高级权限。

**函数签名**：

```csharp
Task<FeishuApiResult<UpdateBitableAppResult>?> UpdateBitableAppAsync(
    [Path] string app_token,
    [Body] UpdateBitableAppRequest updateBitableAppRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                    | 类型                       | 必填 | 说明                                                           |
| ------------------------- | -------------------------- | ---- | -------------------------------------------------------------- |
| `app_token`               | `string`                   | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT` |
| `updateBitableAppRequest` | `UpdateBitableAppRequest`  | ✅   | 更新多维表格应用请求体                                         |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "app": {
      "app_token": "AW3Qbtr2cakCnesXzXVbbsrIcVT",
      "name": "更新后的名称",
      "revision": 2
    }
  }
}
```

**说明**：更新多维表格的元数据信息，支持修改名称和高级权限开关等属性。
