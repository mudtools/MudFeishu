# 内推账户 - 租户令牌（FeishuTenantV1HireReferralAccount）

## 接口名称

**内推账户（租户令牌）** -（`IFeishuTenantV1HireReferralAccount`）

## 功能描述

提供以租户身份管理飞书招聘内推奖励账户的能力。飞书招聘（Hire）内推账户入口域 SDK 是一组服务端 OpenAPI 的封装，用于内推奖励账户的注册、启用/停用、余额查询、全额提现以及按时间段的提现数据对账。本接口全部端点仅支持 tenant_access_token 调用。支持注册内推账户、启用内推账户、查询内推账户、停用内推账户、全额提取内推账户余额、内推账户提现数据对账等操作。

## 参考文档

- [注册内推账户 - 飞书开放平台](https://open.feishu.cn/document/hire-v1/referral_account/create)

## 函数列表

| 函数名称                            | 功能描述               | 认证方式 | HTTP 方法 |
| ----------------------------------- | ---------------------- | -------- | --------- |
| CreateReferralAccountAsync          | 注册内推账户           | 租户令牌 | POST      |
| EnableReferralAccountAsync          | 启用内推账户           | 租户令牌 | POST      |
| GetReferralAccountAssetsAsync       | 查询内推账户           | 租户令牌 | GET       |
| DeactivateReferralAccountAsync      | 停用内推账户           | 租户令牌 | POST      |
| WithdrawReferralAccountAsync        | 全额提取内推账户余额   | 租户令牌 | POST      |
| ReconciliationReferralAccountAsync  | 内推账户提现数据对账   | 租户令牌 | POST      |

## 函数详细内容

### 注册内推账户

通过内推人的手机号或邮箱注册「内推奖励账户」，返回账户 ID 与账户余额；mobile 与 email 二选一必传。限频：100 次/分钟。所需权限：hire:referral_account（更新内推账号信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<CreateReferralAccountResult>?> CreateReferralAccountAsync(
    [Body] CreateReferralAccountRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                          | 必填 | 说明                                                                                    |
| --------- | ----------------------------- | ---- | --------------------------------------------------------------------------------------- |
| `request` | `CreateReferralAccountRequest` | ✅   | 注册请求体（mobile 含 code 国际区号与 number 手机号；email 邮箱；二者传其一）          |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "account": {}
  }
}
```

**说明**：`data.account` 为 `ReferralAccount`，含 `account_id`、`assets`、`status`；`mobile` 与 `email` 必须传入其中之一。

---

### 启用内推账户

根据账户 ID 启用账户，启用后可通过「内推账户余额变更事件」监听余额变更、通过「全额提取内推账户余额」提取余额。限频：10 次/秒。所需权限：hire:referral_account（更新内推账号信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<EnableReferralAccountResult>?> EnableReferralAccountAsync(
    [Body] EnableReferralAccountRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                          | 必填 | 说明                                                            |
| --------- | ----------------------------- | ---- | --------------------------------------------------------------- |
| `request` | `EnableReferralAccountRequest` | ✅   | 启用请求体（referral_account_id 选填：注册账户后获取的账户 ID） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "account": {}
  }
}
```

**说明**：`data.account` 为启用后的 `ReferralAccount`；停用状态下不会推送余额变更事件，启用后恢复。

---

### 查询内推账户

根据账户 ID 查询内推账户信息，返回账户余额、账户状态与账户绑定的内推人信息。限频：10 次/秒。所需权限：hire:referral_account:readonly（获取内推账户信息）或 hire:referral_account（更新内推账号信息）。字段权限：hire:employee.email:readonly（内推人邮箱）、hire:employee.mobile:readonly（内推人手机号）、contact:user.employee_id:readonly（取 user_id 时必填）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetReferralAccountAssetsResult>?> GetReferralAccountAssetsAsync(
    [Query("referral_account_id")] string referral_account_id,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                 | 类型      | 必填 | 说明                                                                                    |
| ---------------------- | --------- | ---- | --------------------------------------------------------------------------------------- |
| `referral_account_id`  | `string`  | ✅   | 账户 ID，注册账户后获取，示例值：`6942778198054125570`                                  |
| `user_id_type`         | `string?` | ⚪   | 用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "account": {}
  }
}
```

**说明**：`data.account` 为 `ReferralAccount`，含 `account_id`、`assets`、`status` 与 `referrer` 内推人信息。

---

### 停用内推账户

根据账户 ID 停用账户，停用后将不再发送「内推账户余额变更事件」，也无法通过「全额提取内推账户余额」提取余额。限频：100 次/分钟。所需权限：hire:referral_account（更新内推账号信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<DeactivateReferralAccountResult>?> DeactivateReferralAccountAsync(
    [Path] string referral_account_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                | 类型     | 必填 | 说明                                    |
| --------------------- | -------- | ---- | --------------------------------------- |
| `referral_account_id` | `string` | ✅   | 账户 ID，示例值：`6942778198054125570` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "account": {}
  }
}
```

**说明**：`data.account` 为停用后的 `ReferralAccount`；停用后无法提现，需重新启用。

---

### 全额提取内推账户余额

通过账户 ID 全额提取内推账户下的积分/现金；提现后账户余额清零，对应奖励在招聘系统中标记为「已发放」。external_order_id 为幂等键，重复传入返回原单据的提取详情。限频：100 次/分钟。所需权限：hire:referral_account（更新内推账号信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<WithdrawReferralAccountResult>?> WithdrawReferralAccountAsync(
    [Path] string referral_account_id,
    [Body] WithdrawReferralAccountRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                | 类型                             | 必填 | 说明                                                                                                                  |
| --------------------- | -------------------------------- | ---- | --------------------------------------------------------------------------------------------------------------------- |
| `referral_account_id` | `string`                         | ✅   | 账户 ID，示例值：`6942778198054125570`                                                                                |
| `request`             | `WithdrawReferralAccountRequest` | ✅   | 提现请求体（withdraw_bonus_type 必填：1 积分 / 2 现金；external_order_id 必填：请求方提供的唯一单据 ID，保证幂等）   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "external_order_id": "",
    "trans_time": "",
    "withdrawal_details": {}
  }
}
```

**说明**：`external_order_id` 为幂等键，重复传入同一值返回原单据的提取详情；提现后账户余额清零，奖励标记为「已发放」。

---

### 内推账户提现数据对账

对一段时间内的内推账户积分提现数据进行对账，调用方需传入调用方系统的内推账户积分变动信息，返回核对失败的账户列表。限频：100 次/分钟。所需权限：hire:referral_account（更新内推账号信息）。

**函数签名**：

```csharp
Task<FeishuApiResult<ReconciliationReferralAccountResult>?> ReconciliationReferralAccountAsync(
    [Body] ReconciliationReferralAccountRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名    | 类型                                | 必填 | 说明                                                                                                                       |
| --------- | ----------------------------------- | ---- | -------------------------------------------------------------------------------------------------------------------------- |
| `request` | `ReconciliationReferralAccountRequest` | ✅   | 对账请求体（start_trans_time/end_trans_time 必填：对账时段的起止交易时间（毫秒时间戳）；trade_details 选填：账户积分变动信息列表） |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "check_failed_list": []
  }
}
```

**说明**：`data.check_failed_list` 为 `ReferralAccountCheckFailedInfo[]`，含 `account_id`、提取金额与充值金额；仅返回核对失败的账户。
