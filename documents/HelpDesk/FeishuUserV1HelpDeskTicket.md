---
title: 服务台工单接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份更新服务台工单详情，并创建、更新与删除工单自定义字段。
---

# IFeishuUserV1HelpDeskTicket - 用户服务台工单API

## 功能描述
飞书服务台工单API是开放平台基于飞书服务台的工单功能模块开放的查看/创建/修改/删除等API，开发者可以基于这些API对服务台工单对应的功能模块进行操作。本接口使用用户访问令牌（UserAccessToken）鉴权，并额外实现 `ICurrentUserId`；同时继承自 `IFeishuV1HelpDeskTicket` 的 `HelpdeskTokenAndId` 属性用于在服务台请求 Header 中添加 `X-Lark-Helpdesk-Authorization` 参数（Value 为 `base64(helpdesk_id:helpdesk_token)`，即通过 base64 加密将 helpdesk_id 和 helpdesk_token 用 `:` 连接而成的字符串）。

## 参考文档
- [更新工单详情](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/update)
- [创建工单自定义字段](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket_customized_field/create-ticket-customized-field)
- [删除工单自定义字段](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket_customized_field/delete)
- [更新工单自定义字段](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket_customized_field/update-ticket-customized-field)
- [查询全部工单详情（旧版文档路径）](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list)
- [服务台接入指南](https://open.feishu.cn/document/server-docs/helpdesk-v1/access-guide)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 | 接口文档 |
| :--- | :--- | :--- | :--- |----------|
| UpdateTicketAsync | 更新工单详情 | UserAccessToken | PUT | [UpdateTicketAsync](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/update) |
| CreateCustomizedFieldAsync | 创建工单自定义字段 | UserAccessToken | POST | [CreateCustomizedFieldAsync](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket_customized_field/create-ticket-customized-field) |
| DeleteCustomizedFieldAsync | 删除工单自定义字段 | UserAccessToken | DELETE | [DeleteCustomizedFieldAsync](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket_customized_field/delete) |
| UpdateCustomizedFieldAsync | 更新工单自定义字段 | UserAccessToken | PATCH | [UpdateCustomizedFieldAsync](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket_customized_field/update-ticket-customized-field) |

## 函数详细内容

### UpdateTicketAsync
用于更新服务台工单详情。只会更新数据，不会触发相关操作（如修改工单状态到关单不会关闭聊天页面），要更新的工单字段必须至少输入一项。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateTicketAsync(
    [Path] string ticket_id,
    [Body] UpdateTicketRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| ticket_id | string | ✅ | 工单 ID。可通过[查询全部工单详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list)获取 | 123456 |
| request | UpdateTicketRequest | ✅ | 更新工单请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- 仅支持自建应用
- 只会更新数据，不会触发相关操作；如修改工单状态到关单，不会关闭聊天页面
- 要更新的工单字段必须至少输入一项
- `status` 含义：1 待响应、2 处理中、3 排队中、4 待定、5 待用户响应、50 机器人关闭工单、51 人工关闭工单
- `solved` 含义：1 未解决、2 已解决

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuUserV1HelpDeskTicket>();
ticketApi.HelpdeskTokenAndId = Convert.ToBase64String(
    Encoding.UTF8.GetBytes($"{helpdeskId}:{helpdeskToken}"));

var request = new UpdateTicketRequest
{
    Status = 2,
    Solved = 2,
    TagNames = new[] { "abc" },
    Comment = "good"
};
var result = await ticketApi.UpdateTicketAsync("123456", request);
Console.WriteLine($"更新结果: {result?.Code == 0}");
```

---

### CreateCustomizedFieldAsync
用于创建自定义字段。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> CreateCustomizedFieldAsync(
    [Body] CreateCustomizedFieldRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | CreateCustomizedFieldRequest | ✅ | 创建工单自定义字段请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- user_access_token 访问时，需要操作者是当前服务台的管理员或所有者
- `field_type` 可选值：string 单行文本、multiline 多行文本、dropdown 下拉列表、dropdown_nested 级联下拉
- `helpdesk_id` 需要和请求 Header 中的服务台 ID 保持一致，可以省略
- `position` 为字段在列表后台管理列表中的位置

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuUserV1HelpDeskTicket>();
var request = new CreateCustomizedFieldRequest
{
    KeyName = "test dropdown",
    DisplayName = "test dropdown",
    Position = "3",
    FieldType = "dropdown",
    Description = "下拉示例",
    Visible = true,
    Required = false,
    DropdownAllowMultiple = true
};
var result = await ticketApi.CreateCustomizedFieldAsync(request);
Console.WriteLine($"创建结果: {result?.Code == 0}");
```

---

### DeleteCustomizedFieldAsync
用于删除工单自定义字段。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteCustomizedFieldAsync(
    [Path] string ticket_customized_field_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| ticket_customized_field_id | string | ✅ | 工单自定义字段 ID | 6948728206392295444 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- user_access_token 访问时，需要操作者是当前服务台的管理员或所有者
- 字段 ID 可通过获取全部工单自定义字段接口获取

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuUserV1HelpDeskTicket>();
var result = await ticketApi.DeleteCustomizedFieldAsync("6948728206392295444");
Console.WriteLine($"删除结果: {result?.Code == 0}");
```

---

### UpdateCustomizedFieldAsync
用于更新自定义字段。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateCustomizedFieldAsync(
    [Path] string ticket_customized_field_id,
    [Body] UpdateCustomizedFieldRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| ticket_customized_field_id | string | ✅ | 工单自定义字段 ID | 6948728206392295444 |
| request | UpdateCustomizedFieldRequest | ✅ | 更新工单自定义字段请求体（所有字段均为选填） | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- user_access_token 访问时，需要操作者是当前服务台的管理员或所有者
- 请求体字段 `display_name` / `position` / `description` / `visible` / `required` / `dropdown_options` 均为选填，按需更新

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuUserV1HelpDeskTicket>();
var request = new UpdateCustomizedFieldRequest
{
    DisplayName = "test dropdown",
    Position = "3",
    Description = "下拉示例",
    Visible = true,
    Required = false
};
var result = await ticketApi.UpdateCustomizedFieldAsync("6948728206392295444", request);
Console.WriteLine($"更新结果: {result?.Code == 0}");
```
