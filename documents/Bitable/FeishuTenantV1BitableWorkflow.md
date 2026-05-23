# 多维表格自动化流程 - 租户令牌（FeishuTenantV1BitableWorkflow）

## 接口名称

**多维表格自动化流程（租户令牌）** -（`IFeishuTenantV1BitableWorkflow`）

## 功能描述

提供以租户身份管理飞书多维表格自动化流程的能力。自动化流程 workflows 是用户给多维表格设定的自动运行规则。设定"触发条件"和"执行操作"以后，多维表格会根据数据变更，自动执行下一步操作。支持列出自动化流程、更新自动化流程状态，以及列出工作流等操作。

## 参考文档

- [自动化流程 - 飞书开放平台](https://open.feishu.cn/document/docs/bitable-v1/app-workflow/list)

## 函数列表

| 函数名称                      | 功能描述           | 认证方式 | HTTP 方法 |
| ----------------------------- | ------------------ | -------- | --------- |
| GetAppWorkflowListAsync       | 列出自动化流程     | 租户令牌 | GET       |
| UpdateAppWorkflowAsync        | 更新自动化流程状态 | 租户令牌 | PUT       |
| GetAppBlockWorkflowListAsync  | 列出工作流         | 租户令牌 | GET       |

## 函数详细内容

### 列出自动化流程

用于列出多维表格的自动化流程。

**函数签名**：

```csharp
Task<FeishuApiResult<GetAppWorkflowListResult>?> GetAppWorkflowListAsync(
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
    "workflows": [
      {
        "workflow_id": "730887xxxx552638996",
        "workflow_name": "自动化流程1",
        "status": "enabled"
      }
    ]
  }
}
```

**说明**：返回多维表格中所有自动化流程的列表，包括流程 ID、名称和状态等信息。

---

### 更新自动化流程状态

开启或关闭自动化流程。

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UpdateAppWorkflowAsync(
    [Path] string app_token,
    [Path] string workflow_id,
    [Body] UpdateAppWorkflowRequest updateAppWorkflowRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                      | 类型                        | 必填 | 说明                                                                                     |
| --------------------------- | --------------------------- | ---- | ---------------------------------------------------------------------------------------- |
| `app_token`                 | `string`                    | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                           |
| `workflow_id`               | `string`                    | ✅   | 自动化工作流 ID，通过列出自动化流程接口获取，示例值：`730887xxxx552638996`                |
| `updateAppWorkflowRequest`  | `UpdateAppWorkflowRequest`  | ✅   | 更新自动化流程状态请求体                                                                 |

**响应**：

```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：通过该接口可以开启或关闭指定的自动化流程。`workflow_id` 可通过列出自动化流程接口获取。

---

### 列出工作流

用于返回多维表格中所有工作流，多维表格管理员可通过此接口来管理表中的工作流。

**函数签名**：

```csharp
Task<FeishuApiResult<GetAppWorkflowListResult>?> GetAppBlockWorkflowListAsync(
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
    "workflows": [
      {
        "workflow_id": "730887xxxx552638996",
        "workflow_name": "工作流1",
        "status": "enabled"
      }
    ]
  }
}
```

**说明**：返回多维表格中所有工作流的列表，多维表格管理员可通过此接口来管理表中的工作流。
