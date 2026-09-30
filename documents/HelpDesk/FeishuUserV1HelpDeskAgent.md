---
title: 客服坐席接口（用户令牌）| MudFeishu
description: 该接口用于以用户身份管理飞书服务台客服坐席，支持更新客服信息、创建/更新/删除客服工作日程以及创建/更新/删除客服技能。
---

# IFeishuUserV1HelpDeskAgent - 用户客服坐席API

## 功能描述
飞书服务台API是开放平台基于飞书服务台的知识库/工单/客服等功能模块开放的查看/创建/修改/删除等API，开发者可以基于这些API对服务台对应的功能模块进行操作。本接口聚焦客服坐席的管理能力，使用用户访问令牌（UserAccessToken）鉴权，并额外实现 `ICurrentUserId`；同时继承自 `IFeishuV1HelpDeskAgent` 的 `HelpdeskTokenAndId` 属性用于在服务台请求 Header 中添加 `X-Lark-Helpdesk-Authorization` 参数（Value 为 `base64(helpdesk_id:helpdesk_token)`，即通过 base64 加密将 helpdesk_id 和 helpdesk_token 用 `:` 连接而成的字符串）。

## 参考文档
- [飞书服务台 API 概览](https://open.feishu.cn/document/server-docs/helpdesk-v1/overview)
- [更新客服信息](https://open.feishu.cn/document/server-docs/helpdesk-v1/agent-function/agent/patch)
- [创建客服工作日程](https://open.feishu.cn/document/server-docs/helpdesk-v1/agent-function/agent-schedules/create)
- [删除客服工作日程](https://open.feishu.cn/document/server-docs/helpdesk-v1/agent-function/agent-schedules/delete)
- [更新客服工作日程](https://open.feishu.cn/document/server-docs/helpdesk-v1/agent-function/agent-schedules/patch)
- [创建客服技能](https://open.feishu.cn/document/server-docs/helpdesk-v1/agent-function/agent_skill/create)
- [删除客服技能](https://open.feishu.cn/document/server-docs/helpdesk-v1/agent-function/agent_skill/delete)
- [更新客服技能](https://open.feishu.cn/document/server-docs/helpdesk-v1/agent-function/agent_skill/patch)
- [服务台接入指南](https://open.feishu.cn/document/server-docs/helpdesk-v1/access-guide)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| UpdateAgentInfoAsync | 更新客服信息 | UserAccessToken | PATCH |
| CreateAgentScheduleAsync | 创建客服工作日程 | UserAccessToken | POST |
| DeleteAgentScheduleAsync | 删除客服工作日程 | UserAccessToken | DELETE |
| UpdateAgentScheduleAsync | 更新客服工作日程 | UserAccessToken | PATCH |
| CreateAgentSkillAsync | 创建客服技能 | UserAccessToken | POST |
| DeleteAgentSkillAsync | 删除客服技能 | UserAccessToken | DELETE |
| UpdateAgentSkillAsync | 更新客服技能 | UserAccessToken | PATCH |

## 函数详细内容

### UpdateAgentInfoAsync
更新客服状态等信息。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateAgentInfoAsync(
    [Path] string agent_id,
    [Body] UpdateAgentInfoRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| agent_id | string | ✅ | 客服 id | ou_14777d82ffef0f707de5a8c7ff2c5ebe |
| request | UpdateAgentInfoRequest | ✅ | 更新客服信息请求体（`status` 选填） | - |
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
- `status` 含义：1 在线、2 离线

**代码示例**
```csharp
var agentApi = feishuApp.GetApi<IFeishuUserV1HelpDeskAgent>();
agentApi.HelpdeskTokenAndId = Convert.ToBase64String(
    Encoding.UTF8.GetBytes($"{helpdeskId}:{helpdeskToken}"));

var request = new UpdateAgentInfoRequest { Status = 1 };
var result = await agentApi.UpdateAgentInfoAsync("ou_14777d82ffef0f707de5a8c7ff2c5ebe", request);
Console.WriteLine($"更新结果: {result?.Code == 0}");
```

---

### CreateAgentScheduleAsync
用于创建客服日程。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> CreateAgentScheduleAsync(
    [Body] CreateAgentScheduleRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | CreateAgentScheduleRequest | ✅ | 创建客服工作日程请求体（`agent_schedules` 新客服日程列表） | - |
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
- 每条日程包含 `agent_id`（客服 id）、`schedule`（工作日程列表）与 `agent_skill_ids`（客服技能 ids）
- `weekday` 取值：1 Monday 至 7 Sunday，9 Everyday、10 Weekday、11 Weekend；时间格式 `00:00` - `23:59`

**代码示例**
```csharp
var agentApi = feishuApp.GetApi<IFeishuUserV1HelpDeskAgent>();
var request = new CreateAgentScheduleRequest
{
    AgentSchedules = new[]
    {
        new AgentScheduleUpdateInfo
        {
            AgentId = "ou_14777d82ffef0f707de5a8c7ff2c5ebe",
            Schedules = new[]
            {
                new WeekdaySchedule { StartTime = "09:00", EndTime = "18:00", Weekday = 10 }
            },
            AgentSkillIds = new[] { "test-skill-id" }
        }
    }
};
var result = await agentApi.CreateAgentScheduleAsync(request);
Console.WriteLine($"创建结果: {result?.Code == 0}");
```

---

### DeleteAgentScheduleAsync
用于删除客服日程。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteAgentScheduleAsync(
    [Path] string agent_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| agent_id | string | ✅ | 客服 id | ou_14777d82ffef0f707de5a8c7ff2c5ebe |
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
- 该接口无请求体与查询参数，按路径中的 `agent_id` 删除对应客服日程

**代码示例**
```csharp
var agentApi = feishuApp.GetApi<IFeishuUserV1HelpDeskAgent>();
var result = await agentApi.DeleteAgentScheduleAsync("ou_14777d82ffef0f707de5a8c7ff2c5ebe");
Console.WriteLine($"删除结果: {result?.Code == 0}");
```

---

### UpdateAgentScheduleAsync
用于更新客服日程。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateAgentScheduleAsync(
    [Path] string agent_id,
    [Body] UpdateAgentScheduleRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| agent_id | string | ✅ | 客服 id | ou_14777d82ffef0f707de5a8c7ff2c5ebe |
| request | UpdateAgentScheduleRequest | ✅ | 更新客服工作日程请求体（`agent_schedule` 工作日程列表） | - |
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
- `agent_schedule` 字段类型为 `AgentScheduleUpdate`，包含 `schedule`（工作日程列表）与 `agent_skill_ids`（客服技能 ids）

**代码示例**
```csharp
var agentApi = feishuApp.GetApi<IFeishuUserV1HelpDeskAgent>();
var request = new UpdateAgentScheduleRequest
{
    AgentSchedule = new AgentScheduleUpdate
    {
        Schedules = new[]
        {
            new WeekdaySchedule { StartTime = "09:00", EndTime = "18:00", Weekday = 10 }
        },
        AgentSkillIds = new[] { "test-skill-id" }
    }
};
var result = await agentApi.UpdateAgentScheduleAsync("ou_14777d82ffef0f707de5a8c7ff2c5ebe", request);
Console.WriteLine($"更新结果: {result?.Code == 0}");
```

---

### CreateAgentSkillAsync
用于创建客服技能，返回新创建的客服技能 id。

**函数签名**
```csharp
Task<FeishuApiResult<CreateAgentSkillResult>?> CreateAgentSkillAsync(
    [Body] CreateAgentSkillRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | CreateAgentSkillRequest | ✅ | 创建客服技能请求体（`name`、`rules`、`agent_ids` 均选填） | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "agent_skill_id": "test-skill-id"
  }
}
```

**说明**
- user_access_token 访问时，需要操作者是当前服务台的管理员或所有者
- `rules` 每项包含 `id`（rule id，可通过获取客服技能规则列表接口获取 options）、`selected_operator`（运算符比较）、`operand`（rule 操作数的值）、`category`（1 知识库、2 工单信息、3 用户飞书信息）

**代码示例**
```csharp
var agentApi = feishuApp.GetApi<IFeishuUserV1HelpDeskAgent>();
var request = new CreateAgentSkillRequest
{
    Name = "test-skill",
    AgentIds = new[] { "ou_ea21d7f018e1155d960e40d33191f966" },
    Rules = new[]
    {
        new AgentSkillRule
        {
            Id = "test-skill-id",
            SelectedOperator = 8,
            Category = 3
        }
    }
};
var result = await agentApi.CreateAgentSkillAsync(request);
Console.WriteLine($"技能 ID: {result?.Data?.AgentSkillId}");
```

---

### DeleteAgentSkillAsync
用于删除客服技能。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteAgentSkillAsync(
    [Path] string agent_skill_id,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| agent_skill_id | string | ✅ | agent group id | test-skill-id |
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
- 该接口无请求体与查询参数

**代码示例**
```csharp
var agentApi = feishuApp.GetApi<IFeishuUserV1HelpDeskAgent>();
var result = await agentApi.DeleteAgentSkillAsync("test-skill-id");
Console.WriteLine($"删除结果: {result?.Code == 0}");
```

---

### UpdateAgentSkillAsync
用于更新客服技能。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> UpdateAgentSkillAsync(
    [Path] string agent_skill_id,
    [Body] UpdateAgentSkillRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
UserAccessToken（用户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| agent_skill_id | string | ✅ | agent group id | test-skill-id |
| request | UpdateAgentSkillRequest | ✅ | 更新客服技能请求体（`agent_skill` 更新技能对象） | - |
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
- `agent_skill` 字段类型为 `AgentSkillData`，包含 `name`（技能名）、`rules`（技能 rules）与 `agent_ids`（具有此技能的客服 ids）

**代码示例**
```csharp
var agentApi = feishuApp.GetApi<IFeishuUserV1HelpDeskAgent>();
var request = new UpdateAgentSkillRequest
{
    AgentSkill = new AgentSkillData
    {
        Name = "skill-name",
        AgentIds = new[] { "ou_ea21d7f018e1155d960e40d33191f966" }
    }
};
var result = await agentApi.UpdateAgentSkillAsync("test-skill-id", request);
Console.WriteLine($"更新结果: {result?.Code == 0}");
```
