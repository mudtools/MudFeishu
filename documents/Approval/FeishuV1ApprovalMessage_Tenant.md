# 审批 Bot 消息接口 -（FeishuV1ApprovalMessage_Tenant）

## 接口名称

审批 Bot 消息接口 -（`IFeishuTenantV1ApprovalMessage`）

## 功能描述

该接口用于通过飞书审批的 Bot 推送消息给用户或更新审批 Bot 消息。当有新的审批待办，或者审批待办的状态有更新时，可以通过飞书审批的 Bot 告知用户。

> **注意**：如果出现推送成功，但是没有收到消息，可能是因为开通了审批机器人的聚合推送。

## 参考文档

- [飞书官方文档 - 审批 Bot 消息](https://open.feishu.cn/document/server-docs/approval-v4/approval-search/query-2)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| `SendBotMessageAsync` | 发送审批 Bot 消息（通用模板） | 租户令牌 | POST |
| `SendBotMessageAsync` | 发送审批 Bot 消息（自定义模板） | 租户令牌 | POST |
| `UpdateBotMessageAsync` | 更新审批 Bot 消息 | 租户令牌 | POST |

## 函数详细内容

### 发送审批 Bot 消息（通用模板）

#### 函数签名

```csharp
Task<FeishuApiResult<ApprovalBotMessageResult>?> SendBotMessageAsync(
    [Body] ApprovalBotMessageRequest approvalBotMessageRequest,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名 | 必填 | 类型 | 描述 |
|-------|-----|------|-----|
| `approvalBotMessageRequest` | ✅ | `ApprovalBotMessageRequest` | 发送审批 Bot 消息通用模板请求体 |
| `cancellationToken` | ⚪ | `CancellationToken` | 取消操作令牌对象 |

**请求体示例：**

```json
{
  "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
  "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
  "open_id": "ou_7dab8a3d3dfcd10xxx",
  "task_id": "123456789",
  "status": "pending"
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "message_id": "om_7dxxxxxxxxxx"
  }
}
```

#### 说明

- 当有新的审批待办时调用此接口推送消息
- 审批待办状态更新时也可调用此接口推送消息

#### 代码示例

```csharp
// 发送审批待办通知
var request = new ApprovalBotMessageRequest
{
    ApprovalCode = "7C468A54-8745-2245-9675-08B7C63E7A85",
    InstanceCode = "6A123516-FB88-470D-A428-9AF58B71B3C0",
    OpenId = "ou_7dab8a3d3dfcd10xxx",
    TaskId = "123456789",
    Status = "pending"
};

var result = await _feishuApi
    .UseApp(appId, appSecret)
    .ExecuteAsync(api => api.SendBotMessageAsync(request));

if (result?.Code == 0)
{
    Console.WriteLine($"消息发送成功，消息ID: {result.Data?.MessageId}");
}
```

---

### 发送审批 Bot 消息（自定义模板）

#### 函数签名

```csharp
Task<FeishuApiResult<ApprovalBotMessageResult>?> SendBotMessageAsync(
    [Body] CustomApprovalBotMessageRequest customApprovalBotMessageRequest,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名 | 必填 | 类型 | 描述 |
|-------|-----|------|-----|
| `customApprovalBotMessageRequest` | ✅ | `CustomApprovalBotMessageRequest` | 发送审批 Bot 消息自定义模板请求体 |
| `cancellationToken` | ⚪ | `CancellationToken` | 取消操作令牌对象 |

**请求体示例：**

```json
{
  "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
  "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
  "open_id": "ou_7dab8a3d3dfcd10xxx",
  "task_id": "123456789",
  "status": "pending",
  "custom_message": {
    "title": "您有新的审批待办",
    "content": "请假申请需要您的审批"
  }
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "message_id": "om_7dxxxxxxxxxx"
  }
}
```

#### 说明

- 支持自定义消息内容格式
- 适用于需要个性化消息展示的场景

#### 代码示例

```csharp
// 发送自定义审批通知
var request = new CustomApprovalBotMessageRequest
{
    ApprovalCode = "7C468A54-8745-2245-9675-08B7C63E7A85",
    InstanceCode = "6A123516-FB88-470D-A428-9AF58B71B3C0",
    OpenId = "ou_7dab8a3d3dfcd10xxx",
    TaskId = "123456789",
    Status = "pending",
    CustomMessage = new CustomMessage
    {
        Title = "您有新的审批待办",
        Content = "张三的请假申请需要您的审批"
    }
};

var result = await _feishuApi
    .UseApp(appId, appSecret)
    .ExecuteAsync(api => api.SendBotMessageAsync(request));
```

---

### 更新审批 Bot 消息

#### 函数签名

```csharp
Task<FeishuApiResult<ApprovalBotMessageResult>?> UpdateBotMessageAsync(
    [Body] ApprovalBotMessageUpdateRequest approvalBotMessageUpdateRequest,
    CancellationToken cancellationToken = default);
```

#### 认证

**租户令牌**（`TenantAccessToken`）

#### 参数

| 参数名 | 必填 | 类型 | 描述 |
|-------|-----|------|-----|
| `approvalBotMessageUpdateRequest` | ✅ | `ApprovalBotMessageUpdateRequest` | 更新审批 Bot 消息请求体 |
| `cancellationToken` | ⚪ | `CancellationToken` | 取消操作令牌对象 |

**请求体示例：**

```json
{
  "message_id": "om_7dxxxxxxxxxx",
  "status": "approved",
  "update_content": {
    "title": "审批已完成",
    "content": "该请假申请已通过"
  }
}
```

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "message_id": "om_7dxxxxxxxxxx"
  }
}
```

#### 说明

- 调用发送审批 Bot 消息接口后，可根据审批 Bot 消息 ID 及审批相应的状态，更新审批 Bot 消息
- 例如，给审批人推送了审批待办消息，当审批人通过审批后，可以将之前推送的 Bot 消息更新为已审批

#### 代码示例

```csharp
// 更新已发送的审批消息为已通过状态
var request = new ApprovalBotMessageUpdateRequest
{
    MessageId = "om_7dxxxxxxxxxx",
    Status = "approved",
    UpdateContent = new UpdateContent
    {
        Title = "审批已完成",
        Content = "该请假申请已通过"
    }
};

var result = await _feishuApi
    .UseApp(appId, appSecret)
    .ExecuteAsync(api => api.UpdateBotMessageAsync(request));

if (result?.Code == 0)
{
    Console.WriteLine("消息更新成功");
}
```
