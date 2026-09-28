---
title: 设备申报接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份审批飞书设备申报，支持通过或驳回成员自主申报的设备申请。
---

# 设备申报 - 租户令牌（FeishuTenantV2SecurityDeviceApplyRecord）

## 接口名称

**设备申报（租户令牌）** -（`IFeishuTenantV2SecurityDeviceApplyRecord`）

## 功能描述

提供以租户身份审批飞书设备申报的能力。飞书安全与合规（Security）「设备申报」SDK 用于在设备管理中审批成员自主申报的设备申请（通过或驳回）。本接口为 security_and_compliance/v2 端点，仅支持 tenant_access_token 调用。支持审批设备申报操作。

## 参考文档

- [审批设备申报 - 飞书开放平台](https://open.feishu.cn/document/security_and_compliance-v1/security_and_compliance-v2/device_apply_record/update)

## 函数列表

| 函数名称                      | 功能描述     | 认证方式 | HTTP 方法 |
| ----------------------------- | ------------ | -------- | --------- |
| UpdateDeviceApplyRecordAsync  | 审批设备申报 | 租户令牌 | PUT       |

## 函数详细内容

### 审批设备申报

在设备管理中通过或驳回一条成员自主申报申请。限频：10 次/秒。所需权限：security_and_compliance:device_apply_record:write（审核自主申报申请）。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UpdateDeviceApplyRecordAsync(
    [Path] string device_apply_record_id,
    [Body] UpdateDeviceApplyRecordRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                    | 类型                              | 必填 | 说明                                            |
| ------------------------- | --------------------------------- | ---- | ----------------------------------------------- |
| `device_apply_record_id`  | `string`                          | ✅   | 设备申报记录 ID，示例值：`7088763625288187923`   |
| `request`                 | `UpdateDeviceApplyRecordRequest`  | ✅   | 请求体（is_approved 是否审批通过，必填）         |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：`is_approved` 为 true 表示通过申报，false 表示驳回；审批通过后设备记录将写入设备管理列表。
