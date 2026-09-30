---
title: 客服坐席接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份查询飞书服务台客服信息，包括客服邮箱、客服工作日程、客服技能与技能规则。
---

# IFeishuTenantV1HelpDeskAgent - 租户客服坐席API

## 功能描述
飞书服务台API是开放平台基于飞书服务台的知识库/工单/客服等功能模块开放的查看/创建/修改/删除等API，开发者可以基于这些API对服务台对应的功能模块进行操作。本接口聚焦客服坐席的查询能力，使用租户访问令牌（TenantAccessToken）鉴权；同时继承自 `IFeishuV1HelpDeskAgent` 的 `HelpdeskTokenAndId` 属性用于在服务台请求 Header 中添加 `X-Lark-Helpdesk-Authorization` 参数（Value 为 `base64(helpdesk_id:helpdesk_token)`，即通过 base64 加密将 helpdesk_id 和 helpdesk_token 用 `:` 连接而成的字符串）。

## 参考文档
- [飞书服务台 API 概览](https://open.feishu.cn/document/server-docs/helpdesk-v1/overview)
- [获取客服邮箱](https://open.feishu.cn/document/server-docs/helpdesk-v1/agent-function/agent/agent_email)
- [查询指定客服工作日程](https://open.feishu.cn/document/server-docs/helpdesk-v1/agent-function/agent-schedules/get)
- [查询全部客服工作日程](https://open.feishu.cn/document/server-docs/helpdesk-v1/agent-function/agent-schedules/list)
- [查询全部客服技能](https://open.feishu.cn/document/server-docs/helpdesk-v1/agent-function/agent_skill/list)
- [获取客服技能列表](https://open.feishu.cn/document/server-docs/helpdesk-v1/agent-function/agent_skill_rule/list)
- [服务台接入指南](https://open.feishu.cn/document/server-docs/helpdesk-v1/access-guide)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| GetAgentEmailAsync | 获取客服邮箱 | TenantAccessToken | GET |
| GetAgentScheduleAsync | 查询指定客服工作日程 | TenantAccessToken | GET |
| GetAgentScheduleListAsync | 查询全部客服工作日程 | TenantAccessToken | GET |
| GetAgentSkillListAsync | 查询全部客服技能 | TenantAccessToken | GET |
| GetAgentSkillRuleListAsync | 获取客服技能列表 | TenantAccessToken | GET |

## 函数详细内容

### GetAgentEmailAsync
用于获取客服邮箱地址，返回客服 open id 与邮箱地址的映射关系。

**函数签名**
```csharp
Task<FeishuApiResult<GetAgentEmailResult>?> GetAgentEmailAsync(
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "agents": {
      "ou_xxx": "xxx@feishu.cn",
      "ou_yyy": "yyy@feishu.cn"
    }
  }
}
```

**说明**
- 返回值 `agents` 为键值对映射：key 为客服 open id，value 为客服邮箱地址

**代码示例**
```csharp
var agentApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskAgent>();
agentApi.HelpdeskTokenAndId = Convert.ToBase64String(
    Encoding.UTF8.GetBytes($"{helpdeskId}:{helpdeskToken}"));

var result = await agentApi.GetAgentEmailAsync();
foreach (var (agentId, email) in result?.Data?.Agents ?? new Dictionary<string, string>())
{
    Console.WriteLine($"{agentId}: {email}");
}
```

---

### GetAgentScheduleAsync
用于获取单个客服的工作日程信息（源码 `<summary>` 中的说明为「用于获取客服信息」）。

**函数签名**
```csharp
Task<FeishuApiResult<GetAgentScheduleResult>?> GetAgentScheduleAsync(
    [Path] string agent_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

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
  "data": {
    "agent_schedule": {
      "status": 1,
      "agent": {
        "id": "ou_14777d82ffef0f707de5a8c7ff2c5ebe",
        "name": "test-user",
        "email": "test@bytedance.com",
        "avatar_url": "https://avatar-url.com/test.png",
        "department": "测试部门",
        "company_name": "test-company"
      },
      "schedule": [
        { "start_time": "00:00", "end_time": "24:00", "weekday": 9 }
      ],
      "agent_skills": [
        { "id": "agent-skill-id", "name": "agent-skill", "is_default": false }
      ]
    }
  }
}
```

**说明**
- `status` 含义：1 online 客服、2 offline（手动）客服、3 off duty（下班）客服
- `schedule` 中 `weekday` 取值：1 Monday 至 7 Sunday，9 Everyday、10 Weekday、11 Weekend

**代码示例**
```csharp
var agentApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskAgent>();
var result = await agentApi.GetAgentScheduleAsync("ou_14777d82ffef0f707de5a8c7ff2c5ebe");
Console.WriteLine($"客服状态: {result?.Data?.AgentSchedule?.Status}");
```

---

### GetAgentScheduleListAsync
用于获取所有客服的工作日程信息，按客服状态筛选返回。

**函数签名**
```csharp
Task<FeishuApiResult<GetAgentScheduleListResult>?> GetAgentScheduleListAsync(
    [Query] int[] status,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| status | int[] | ✅ | 筛选条件：1 online 客服、2 offline（手动）客服、3 off duty（下班）客服、4 移除客服。在 GET 请求中传入多个值的格式为 `status=1&status=2` | status=1&status=2 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "agent_schedules": [
      {
        "status": 1,
        "agent": {
          "id": "ou_14777d82ffef0f707de5a8c7ff2c5ebe",
          "name": "test-user",
          "email": "test@bytedance.com"
        },
        "schedule": [
          { "start_time": "00:00", "end_time": "24:00", "weekday": 9 }
        ],
        "agent_skills": [
          { "id": "agent-skill-id", "name": "agent-skill", "is_default": false }
        ]
      }
    ]
  }
}
```

**说明**
- `status` 为必填数组参数，多个值在 GET 请求中以 `status=1&status=2` 形式重复传递
- `status = 4` 表示移除客服

**代码示例**
```csharp
var agentApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskAgent>();
var result = await agentApi.GetAgentScheduleListAsync(new[] { 1, 2 });
foreach (var schedule in result?.Data?.AgentSchedules ?? Array.Empty<AgentScheduleInfo>())
{
    Console.WriteLine($"{schedule.Agent?.Name}: status={schedule.Status}");
}
```

---

### GetAgentSkillListAsync
获取全部客服技能列表。

**函数签名**
```csharp
Task<FeishuApiResult<GetAgentSkillListResult>?> GetAgentSkillListAsync(CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "agent_skills": [
      {
        "id": "test-skill-id",
        "name": "skill-name",
        "agent_ids": ["ou_ea21d7f018e1155d960e40d33191f966"],
        "is_default": false
      }
    ]
  }
}
```

**说明**
- 返回 `agent_skills` 列表，每项包含技能 id、技能名、具有此技能的客服 ids 与是否默认技能

**代码示例**
```csharp
var agentApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskAgent>();
var result = await agentApi.GetAgentSkillListAsync();
foreach (var skill in result?.Data?.AgentSkills ?? Array.Empty<AgentSkillInfo>())
{
    Console.WriteLine($"技能: {skill.Name}（{skill.Id}），客服数: {skill.AgentIds?.Length ?? 0}");
}
```

---

### GetAgentSkillRuleListAsync
用于获取全部客服技能的规则（rules）列表，仅支持自建应用。

**函数签名**
```csharp
Task<FeishuApiResult<GetAgentSkillRuleListResult>?> GetAgentSkillRuleListAsync(CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "rules": [
      {
        "display_name": "中文知识库分类",
        "operator_options": []
      }
    ]
  }
}
```

**说明**
- 仅支持自建应用
- 返回的 rules 可用于创建/更新客服技能时的 `AgentSkillRule.Id`（rule id）、`selected_operator`（运算符比较）与 `category`（1 知识库、2 工单信息、3 用户飞书信息）

**代码示例**
```csharp
var agentApi = feishuApp.GetApi<IFeishuTenantV1HelpDeskAgent>();
var result = await agentApi.GetAgentSkillRuleListAsync();
foreach (var rule in result?.Data?.Rules ?? Array.Empty<AgentSkillRuleInfo>())
{
    Console.WriteLine($"规则: {rule.DisplayName}");
}
```
