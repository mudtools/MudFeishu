# 飞书妙搭用户 ID 转换 - 租户令牌（FeishuTenantV1SparkDirectoryUser）

## 接口名称

**飞书妙搭用户 ID 转换（租户令牌）** -（`IFeishuTenantV1SparkDirectoryUser`）

## 功能描述

以租户身份在飞书妙搭与飞书开放平台之间转换用户 ID。本接口为双令牌基接口 `IFeishuV1SparkDirectoryUser` 的租户令牌派生，用户身份调用见 `IFeishuUserV1SparkDirectoryUser`。

## 参考文档

- [转换飞书妙搭与飞书开放平台之间的用户 ID](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/directory-user/id_convert)

## 函数列表

| 函数名称       | 功能描述                        | 认证方式 | HTTP 方法 |
| -------------- | ------------------------------- | -------- | --------- |
| IdConvertAsync | 转换妙搭与开放平台之间的用户 ID | 租户令牌 | POST      |

## 函数详细内容

### 转换飞书妙搭与飞书开放平台之间的用户 ID

签名与参数同 [FeishuUserV1SparkDirectoryUser](./FeishuUserV1SparkDirectoryUser.md) 中对应章节，仅认证方式为租户令牌。

**函数签名**：

```csharp
Task<FeishuApiResult<IdConvertResult>?> IdConvertAsync(
    [Body] IdConvertRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌。请求体 `IdConvertRequest` 中 `id_convert_type` 必填（10/11/20/21/40），`ids` 可选（长度 1～100）；响应 `data.items` 为 ID 映射列表。
