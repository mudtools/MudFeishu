# 飞书妙搭用户 ID 转换 - 用户令牌（FeishuUserV1SparkDirectoryUser）

## 接口名称

**飞书妙搭用户 ID 转换（用户令牌）** -（`IFeishuUserV1SparkDirectoryUser`）

## 功能描述

提供以用户身份在飞书妙搭与飞书开放平台之间转换用户 ID 的能力。指定转换类型（id_convert_type）与待转换的 ID 列表（ids），支持妙搭用户 ID 与开放平台 OpenID/UnionID、飞书用户 ID 之间的双向转换。本接口为双令牌基接口 `IFeishuV1SparkDirectoryUser` 的用户令牌派生，租户身份调用见 `IFeishuTenantV1SparkDirectoryUser`。

## 参考文档

- [转换飞书妙搭与飞书开放平台之间的用户 ID](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/directory-user/id_convert)

## 函数列表

| 函数名称        | 功能描述                       | 认证方式 | HTTP 方法 |
| --------------- | ------------------------------ | -------- | --------- |
| IdConvertAsync  | 转换妙搭与开放平台之间的用户 ID | 用户令牌 | POST      |

## 函数详细内容

### 转换飞书妙搭与飞书开放平台之间的用户 ID

指定转换类型（id_convert_type）与待转换的 ID 列表（ids），实现妙搭用户 ID 与飞书开放平台 OpenID/UnionID、飞书用户 ID 之间的双向转换。限频：50 次/秒；所需权限：`spark:directory.user.id_convert:read`（至少具备其一）。

**函数签名**：

```csharp
Task<FeishuApiResult<IdConvertResult>?> IdConvertAsync(
    [Body] IdConvertRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌（双令牌基接口）

**参数**：

| 参数名    | 类型             | 必填 | 说明                                                                   |
| --------- | ---------------- | ---- | ---------------------------------------------------------------------- |
| `request` | `IdConvertRequest` | ✅  | `id_convert_type` 必填（10/11/20/21/40）；`ids` 可选，长度 1～100       |

**`id_convert_type` 可选值**：

| 值  | 含义                                 | `ids` 须为        |
| --- | ------------------------------------ | ----------------- |
| 10  | 妙搭用户 ID → 开放平台 OpenID        | 妙搭用户 ID       |
| 11  | 妙搭用户 ID → 开放平台 UnionID       | 妙搭用户 ID       |
| 20  | 开放平台 OpenID → 妙搭用户 ID        | 开放平台 OpenID   |
| 21  | 开放平台 UnionID → 妙搭用户 ID       | 开放平台 UnionID  |
| 40  | 妙搭用户 ID → 飞书用户 ID            | 妙搭用户 ID       |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "source_id": "123445678933432",
        "target_id": "ou_1234cdjhjfedgfhgdhy3884"
      }
    ]
  }
}
```

**说明**：`data.items` 为 ID 映射列表（`IdMapItem`：`source_id` 源 ID、`target_id` 目标 ID）；无结果或查询出错的 ID 不会返回。
