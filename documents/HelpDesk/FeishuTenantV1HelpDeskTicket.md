---
title: 服务台工单接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份操作飞书服务台工单，支持创建对话、查询工单详情与列表、获取工单图像与消息、回复用户提问以及管理工单自定义字段。
---

# IFeishuTenantV1HelpDeskTicket - 租户服务台工单API

## 功能描述
飞书服务台工单API是开放平台基于飞书服务台的工单功能模块开放的查看/创建/修改/删除等API，开发者可以基于这些API对服务台工单对应的功能模块进行操作。本接口使用租户访问令牌（TenantAccessToken）鉴权；同时继承自 `IFeishuV1HelpDeskTicket` 的 `HelpdeskTokenAndId` 属性用于在服务台请求 Header 中添加 `X-Lark-Helpdesk-Authorization` 参数（Value 为 `base64(helpdesk_id:helpdesk_token)`，即通过 base64 加密将 helpdesk_id 和 helpdesk_token 用 `:` 连接而成的字符串）。

## 参考文档
- [创建服务台对话](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/start_service)
- [查询指定工单详情](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/get)
- [查询全部工单详情](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/list)
- [查询全部工单详情（旧版文档路径）](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list)
- [获取工单内图像](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/ticket_image)
- [回复用户在工单里的提问](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/answer_user_query)
- [获取服务台自定义字段](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/customized_fields)
- [发送工单消息](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket-message/create)
- [获取工单消息详情](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket-message/list)
- [查询消息ID（旧版文档路径）](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket-message/list)
- [服务台机器人向工单绑定的群内发送消息](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket-message/create-2)
- [获取指定工单自定义字段](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket_customized_field/get-ticket-customized-field)
- [获取全部工单自定义字段](https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket_customized_field/list-ticket-customized-fields)
- [服务台接入指南](https://open.feishu.cn/document/server-docs/helpdesk-v1/access-guide)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| StartServiceTicketAsync | 创建服务台对话 | TenantAccessToken | POST |
| GetTicketAsync | 查询指定工单详情 | TenantAccessToken | GET |
| GetTicketListAsync | 查询全部工单详情 | TenantAccessToken | GET |
| GetTicketImageAsync | 获取工单内图像 | TenantAccessToken | GET |
| AnswerUserQueryTicketAsync | 回复用户在工单里的提问 | TenantAccessToken | POST |
| GetCustomizedFieldsListAsync | 获取服务台自定义字段 | TenantAccessToken | GET |
| CreateTicketMessageAsync | 发送工单消息 | TenantAccessToken | POST |
| GetTicketMessageListAsync | 获取工单消息详情 | TenantAccessToken | GET |
| CreateBotMessageAsync | 服务台机器人向工单绑定的群内发送消息 | TenantAccessToken | POST |
| GetCustomizedFieldAsync | 获取指定工单自定义字段 | TenantAccessToken | GET |
| GetCustomizedFieldPageListAsync | 获取全部工单自定义字段 | TenantAccessToken | GET |

## 函数详细内容

### StartServiceTicketAsync
用于创建服务台对话，创建后返回客服群 ID，人工工单还会返回工单 ID。

**函数签名**
```csharp
Task<FeishuApiResult<StartServiceTicketResult>?> StartServiceTicketAsync(
    [Body] StartServiceTicketRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | StartServiceTicketRequest | ✅ | 创建服务台对话请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "chat_id": "oc_7dab8a3d3cdcc9da365777c7ad535d62",
    "ticket_id": "7474857595946745884"
  }
}
```

**说明**
- `human_service` 为 true 表示直接进入人工；填写了 `appointed_agents` 时 `human_service` 必填
- `customized_info` 为工单来源自定义信息，长度限制 1024 字符，可通过获取工单详情接口返回
- `ticket_id` 仅人工工单返回

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskTicket>();
ticketApi.HelpdeskTokenAndId = Convert.ToBase64String(
    Encoding.UTF8.GetBytes($"{helpdeskId}:{helpdeskToken}"));

var request = new StartServiceTicketRequest
{
    OpenId = "ou_7dab8a3d3cdcc9da365777c7ad535d62",
    HumanService = true,
    AppointedAgents = new[] { "ou_7dab8a3d3cdcc9da365777c7ad535d62" }
};
var result = await ticketApi.StartServiceTicketAsync(request);
Console.WriteLine($"客服群: {result?.Data?.ChatId}，工单: {result?.Data?.TicketId}");
```

---

### GetTicketAsync
用于获取单个服务台工单详情。

**函数签名**
```csharp
Task<FeishuApiResult<GetTicketResult>?> GetTicketAsync(
    [Path] string ticket_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| ticket_id | string | ✅ | 工单 ID。可通过[查询全部工单详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list)获取 | 123456 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "ticket": {
      "ticket_id": "6626871355780366331",
      "helpdesk_id": "6626871355780366330",
      "guest": { "id": "ou_37019b7c830210acd88fdce886e25c71", "name": "abc" },
      "ticket_type": 1,
      "status": 1,
      "score": 1,
      "created_at": 1616920429000,
      "updated_at": 1616920429000,
      "channel": 0,
      "solve": 1,
      "tags": [{ "id": "7474857595946745884", "name": "标签名称" }]
    }
  }
}
```

**说明**
- 仅支持自建应用
- `status` 含义：1 已创建、2 处理中、3 排队中、4 待定、5 待用户响应、50 被机器人关闭、51 被客服关闭、52 用户自己关闭
- `score` 含义：1 不满意、2 一般、3 满意

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskTicket>();
var result = await ticketApi.GetTicketAsync("123456");
Console.WriteLine($"工单状态: {result?.Data?.Ticket?.Status}");
```

---

### GetTicketListAsync
用于获取全部工单详情，支持按客服、状态、时间等条件筛选并分页返回。

**函数签名**
```csharp
Task<FeishuApiResult<GetTicketListResult>?> GetTicketListAsync(
    [Query] string? ticket_id = null,
    [Query] string? agent_id = null,
    [Query] string? closed_by_id = null,
    [Query] int? type = null,
    [Query] int? channel = null,
    [Query] int? solved = null,
    [Query] int? score = null,
    [Query] int[]? status_list = null,
    [Query] string? guest_name = null,
    [Query] string? guest_id = null,
    [Query] string[]? tags = null,
    [Query] int? page = null,
    [Query] int? page_size = 10,
    [Query] int? create_time_start = null,
    [Query] int? create_time_end = null,
    [Query] int? update_time_start = null,
    [Query] int? update_time_end = null,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| ticket_id | string? | ⚪ | 搜索条件: 工单 ID | 123456 |
| agent_id | string? | ⚪ | 搜索条件: 客服 id | ou_b5de90429xxx |
| closed_by_id | string? | ⚪ | 搜索条件: 关单客服 id | ou_b5de90429xxx |
| type | int? | ⚪ | 搜索条件: 工单类型 1 bot、2 人工 | 1 |
| channel | int? | ⚪ | 搜索条件: 工单渠道 | 0 |
| solved | int? | ⚪ | 搜索条件: 工单是否解决 1 没解决、2 已解决 | 1 |
| score | int? | ⚪ | 搜索条件: 工单评分 | 1 |
| status_list | int[]? | ⚪ | 搜索条件: 工单状态列表 | 1 |
| guest_name | string? | ⚪ | 搜索条件: 用户名称 | abc |
| guest_id | string? | ⚪ | 搜索条件: 用户 id | ou_b5de90429xxx |
| tags | string[]? | ⚪ | 搜索条件: 用户标签列表 | 备注 |
| page | int? | ⚪ | 页数，从 1 开始，默认为 1 | 1 |
| page_size | int? | ⚪ | 当前页大小，最大为 200，默认为 20 | 20 |
| create_time_start | int? | ⚪ | 搜索条件: 工单创建起始时间 ms（也需要填上 create_time_end），相当于 >= create_time_start | 1616920429000 |
| create_time_end | int? | ⚪ | 搜索条件: 工单创建结束时间 ms（也需要填上 create_time_start），相当于 <= create_time_end | 1616920429000 |
| update_time_start | int? | ⚪ | 搜索条件: 工单修改起始时间 ms（也需要填上 update_time_end） | 1616920429000 |
| update_time_end | int? | ⚪ | 搜索条件: 工单修改结束时间 ms（也需要填上 update_time_start） | 1616920429000 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "total": 100,
    "tickets": [
      {
        "ticket_id": "6626871355780366331",
        "helpdesk_id": "6626871355780366330",
        "status": 1,
        "ticket_type": 1,
        "channel": 0,
        "solve": 1,
        "created_at": 1616920429000
      }
    ]
  }
}
```

**说明**
- 仅支持自建应用
- 分页查询最多累计返回一万条数据，超过一万条请更改查询条件，推荐通过时间查询
- `total` 为工单总数，单次请求最大为 10000 条

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskTicket>();
var result = await ticketApi.GetTicketListAsync(page: 1, page_size: 20, status_list: new[] { 1 });
Console.WriteLine($"工单总数: {result?.Data?.Total}");
foreach (var ticket in result?.Data?.Tickets ?? Array.Empty<Ticket>())
{
    Console.WriteLine($"工单 {ticket.TicketId}: 状态 {ticket.Status}");
}
```

---

### GetTicketImageAsync
用于获取服务台工单消息图象，返回图片的二进制内容。

**函数签名**
```csharp
Task<byte[]?> GetTicketImageAsync(
    [Query] string ticket_id,
    [Query] string msg_id,
    [Query] int? index = null,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| ticket_id | string | ✅ | 工单 ID。可通过[查询全部工单详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list)获取 | 123456 |
| msg_id | string | ✅ | 消息 ID，可通过[查询消息ID](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket-message/list)获取 | 12345 |
| index | int? | ⚪ | index，当消息类型为 post 时，需指定图片 index，index 从 0 开始；当消息类型为 img 时无需 index | 0 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
(binary image content)
```

**说明**
- 仅支持自建应用
- 成功时返回响应的二进制内容（取自 `HttpContent.ReadAsByteArrayAsync`，不会为 `null`；空响应体对应空数组）
- 服务端返回非 2xx 状态码时抛出 `ApiException`（携带 `StatusCode` 与响应内容）
- 飞书部分业务错误以 HTTP 200 + JSON 错误体（`{"code":...,"msg":...}`）返回，此时本方法会把错误 JSON 当作文件内容返回；落盘前应按 `Content-Type` 自检，详见 `documents/ErrorHandling.md`

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskTicket>();
var bytes = await ticketApi.GetTicketImageAsync("123456", "12345", index: 0);
if (bytes is { Length: > 0 })
{
    await File.WriteAllBytesAsync("ticket-image.png", bytes);
}
```

---

### AnswerUserQueryTicketAsync
用于回复用户提问结果至工单，需要工单仍处于进行中且未接入人工状态。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> AnswerUserQueryTicketAsync(
    [Path] string ticket_id,
    [Body] AnswerUserQueryTicketRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| ticket_id | string | ✅ | 工单 ID。可通过[查询全部工单详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list)获取 | 123456 |
| request | AnswerUserQueryTicketRequest | ✅ | 回复用户在工单里的提问请求体（`event_id` 必填，`faqs` 选填） | - |
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
- 调用前提：工单仍处于进行中且未接入人工状态
- `event_id` 为事件 ID，可从订阅事件中提取；`faqs` 为 faq 结果列表，每项包含 `id`（faq 服务台内唯一标识）与 `score`（faq 匹配得分）

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskTicket>();
var request = new AnswerUserQueryTicketRequest
{
    EventId = "abcd",
    Faqs = new[] { new UserQueryFaqInfo { Id = "12345", Score = 0.9f } }
};
var result = await ticketApi.AnswerUserQueryTicketAsync("123456", request);
Console.WriteLine($"回复结果: {result?.Code == 0}");
```

---

### GetCustomizedFieldsListAsync
用于获取服务台自定义字段详情，返回用户自定义字段与自定义工单字段两类列表。

**函数签名**
```csharp
Task<FeishuApiResult<GetCustomizedFieldsListResult>?> GetCustomizedFieldsListAsync(
    [Query] bool? visible_only = null,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| visible_only | bool? | ⚪ | visible only | true |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "user_customized_fields": [
      {
        "user_customized_field_id": "6746384425543548981",
        "id": "6746384425543548981",
        "helpdesk_id": "1542164574896126",
        "key_name": "company_id3",
        "display_name": "CompanyID",
        "position": "1",
        "field_type": "string",
        "description": "租户ID",
        "visible": false,
        "editable": false,
        "required": false
      }
    ],
    "ticket_customized_fields": [
      {
        "ticket_customized_field_id": "6834320707288072194",
        "helpdesk_id": "1542164574896126",
        "key_name": "testdropdown",
        "display_name": "testdropdown",
        "position": "3",
        "field_type": "dropdown",
        "description": "下拉示例",
        "visible": true,
        "editable": true,
        "required": false,
        "dropdown_allow_multiple": true
      }
    ]
  }
}
```

**说明**
- `user_customized_fields` 为用户自定义字段，`ticket_customized_fields` 为自定义工单字段
- 工单字段类型 `field_type` 可选值：string 单行文本、multiline 多行文本、dropdown 下拉列表、dropdown_nested 级联下拉

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskTicket>();
var result = await ticketApi.GetCustomizedFieldsListAsync(visible_only: true);
Console.WriteLine($"工单自定义字段数: {result?.Data?.TicketCustomizedFields?.Length ?? 0}");
```

---

### CreateTicketMessageAsync
用于发送工单消息，支持文本与富文本两种消息类型。

**函数签名**
```csharp
Task<FeishuApiResult<CreateTicketMessageResult>?> CreateTicketMessageAsync(
    [Path] string ticket_id,
    [Body] CreateTicketMessageRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| ticket_id | string | ✅ | 工单 ID。可通过[查询全部工单详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list)获取 | 123456 |
| request | CreateTicketMessageRequest | ✅ | 发送工单消息请求体（`msg_type` 与 `content` 必填） | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "message_id": "om_8baa3656c7b41900d29bf9104bf5310b"
  }
}
```

**说明**
- `msg_type` 取值：text 纯文本、post 富文本
- `content` 为消息内容字符串：纯文本参考[发送文本消息](https://open.feishu.cn/document/ukTMukTMukTM/uUjNz4SN2MjL1YzM)中的 content，富文本参考[发送富文本消息](https://open.feishu.cn/document/ukTMukTMukTM/uMDMxEjLzATMx4yMwETM)中的 content
- 返回 `message_id` 为 chat 消息 open ID

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskTicket>();
var request = new CreateTicketMessageRequest
{
    MsgType = "text",
    Content = "{\"text\":\"您好，请问还有什么可以帮您？\"}"
};
var result = await ticketApi.CreateTicketMessageAsync("123456", request);
Console.WriteLine($"消息 ID: {result?.Data?.MessageId}");
```

---

### GetTicketMessageListAsync
用于获取服务台工单消息详情，按时间范围分页返回工单消息列表。

**函数签名**
```csharp
Task<FeishuApiResult<GetTicketMessageListResult>?> GetTicketMessageListAsync(
    [Path] string ticket_id,
    [Query] int? time_start = null,
    [Query] int? time_end = null,
    [Query] int? page = null,
    [Query] int? page_size = 10,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| ticket_id | string | ✅ | 工单 ID。可通过[查询全部工单详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list)获取 | 123456 |
| time_start | int? | ⚪ | 起始时间 | 1617960686 |
| time_end | int? | ⚪ | 结束时间 | 1617960687 |
| page | int? | ⚪ | 页数 ID | 1 |
| page_size | int? | ⚪ | 消息数量，最大 200，默认 20 | 10 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "messages": [
      {
        "id": "6948728206392295444",
        "message_id": "6949088236610273307",
        "message_type": "text",
        "created_at": 1617960686000,
        "content": "{\"content\":\"进入人工服务。 @李宁 为你提供服务，开始聊起来吧~\",\"msg_type\":\"text\"}",
        "user_name": "李宁",
        "user_id": "ou_37019b7c830210acd88fdce886e25c71"
      }
    ],
    "total": 100
  }
}
```

**说明**
- `message_type` 取值：text 纯文本、post 富文本、image 图像、file 文件、media 视频
- 返回的 `message_id` 可用于获取工单内图像接口的 `msg_id` 参数

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskTicket>();
var result = await ticketApi.GetTicketMessageListAsync("123456", page: 1, page_size: 10);
foreach (var msg in result?.Data?.Messages ?? Array.Empty<TicketMessage>())
{
    Console.WriteLine($"[{msg.MessageType}] {msg.UserName}: {msg.Content}");
}
```

---

### CreateBotMessageAsync
通过服务台机器人给指定用户的服务台专属群或私聊发送消息，支持文本、富文本、卡片、图片。

**函数签名**
```csharp
Task<FeishuApiResult<CreateTicketMessageResult>?> CreateBotMessageAsync(
    [Body] CreateBotMessageRequest request,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | CreateBotMessageRequest | ✅ | 服务台机器人向工单绑定的群内发送消息请求体 | - |
| user_id_type | string? | ⚪ | 用户 ID 类型，默认值：open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "message_id": "om_8baa3656c7b41900d29bf9104bf5310b"
  }
}
```

**说明**
- `msg_type` 可选值：text 普通文本、post 富文本、image 图片、interactive 卡片消息
- `receive_type` 可选值：chat 通过服务台专属群发送、user 通过服务台机器人私聊发送；若选择专属服务群，用户有正在处理的工单将会发送失败，默认以 chat 方式发送
- `user_id_type` 可选值：open_id、union_id、user_id，默认 open_id

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskTicket>();
var request = new CreateBotMessageRequest
{
    MsgType = "text",
    Content = "{\"text\":\"您好，工单已收到，我们会尽快处理。\"}",
    ReceiverId = "ou_7346484524",
    ReceiveType = "chat"
};
var result = await ticketApi.CreateBotMessageAsync(request);
Console.WriteLine($"消息 ID: {result?.Data?.MessageId}");
```

---

### GetCustomizedFieldAsync
用于获取工单自定义字段详情。

**函数签名**
```csharp
Task<FeishuApiResult<GetCustomizedFieldResult>?> GetCustomizedFieldAsync(
    [Path] string ticket_customized_field_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

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
  "data": {
    "ticket_customized_field_id": "6948728206392295444",
    "helpdesk_id": "1542164574896126",
    "key_name": "testdropdown",
    "display_name": "test dropdown",
    "position": "3",
    "field_type": "dropdown",
    "description": "下拉示例",
    "visible": true,
    "editable": true,
    "required": false,
    "created_at": "1591239289000",
    "updated_at": "1591239289000",
    "dropdown_allow_multiple": true
  }
}
```

**说明**
- `dropdown_options` 为下拉列表选项，`dropdown_allow_multiple` 仅在字段类型是 dropdown 时有效
- `created_by` / `updated_by` 返回创建用户与更新用户信息

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskTicket>();
var result = await ticketApi.GetCustomizedFieldAsync("6948728206392295444");
Console.WriteLine($"字段名称: {result?.Data?.DisplayName}，类型: {result?.Data?.FieldType}");
```

---

### GetCustomizedFieldPageListAsync
用于获取全部工单自定义字段，按分页标记返回工单自定义字段列表。

**函数签名**
```csharp
Task<FeishuApiResult<GetCustomizedFieldListResult>?> GetCustomizedFieldPageListAsync(
    [Body] GetCustomizedFieldRequest request,
    [Query] string? page_token = null,
    [Query] int? page_size = 10,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | GetCustomizedFieldRequest | ✅ | 获取工单自定义字段请求体（`visible` 是否可见） | - |
| page_token | string? | ⚪ | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果 | 6948728206392295444 |
| page_size | int? | ⚪ | 分页大小，最大值 100，默认为 20 | 10 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "has_more": true,
    "next_page_token": "6948728206392295444",
    "page_token": "6948728206392295444",
    "items": [
      {
        "ticket_customized_field_id": "6834320707288072194",
        "helpdesk_id": "1542164574896126",
        "key_name": "testdropdown",
        "display_name": "testdropdown",
        "position": "3",
        "field_type": "dropdown",
        "description": "下拉示例",
        "visible": true,
        "editable": true,
        "required": false
      }
    ]
  }
}
```

**说明**
- 该接口为 GET 方法但携带请求体（`[Body] GetCustomizedFieldRequest`），用于按 `visible` 条件筛选
- `page_size` 最大值为 100，默认为 20
- 响应同时暴露 `next_page_token` 与兼容别名 `page_token`，二者指向同一值

**代码示例**
```csharp
var ticketApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskTicket>();
var request = new GetCustomizedFieldRequest { Visible = true };
var result = await ticketApi.GetCustomizedFieldPageListAsync(request, page_size: 20);
foreach (var field in result?.Data?.Items ?? Array.Empty<TicketCustomizedField>())
{
    Console.WriteLine($"字段: {field.DisplayName}（{field.FieldType}）");
}
```
