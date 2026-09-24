# 飞书 Aily SDK 接口文档

## 概述

飞书 Aily SDK 提供了飞书 Aily（智能伙伴/智能体）服务端 OpenAPI 的完整封装，支持智能体对话与会话管理、技能调用、数据知识问答与管理等能力，帮助开发者把 Aily 集成到自己的业务系统中。

**主要功能：**

- 智能体对话（普通 JSON 与 SSE 流式输出）、对话结果轮询
- 智能体会话管理与产物下载
- 技能调用、技能信息与技能列表查询
- 数据知识问答、文件上传与数据知识增删改查
- Aily 会话（Session）、消息（Message）与运行（Run）管理

**适用场景：**

- 企业智能助手与问答机器人集成
- 业务系统与 Aily 技能的编排调用
- 数据知识库构建与检索问答
- 多轮会话与运行状态的自定义编排

**文档使用指引：**

本索引文档提供了所有飞书 Aily 相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及响应示例。点击各 API 链接可查看详细文档。

## 快速开始

### 安装

```bash
dotnet add package Mud.Feishu
```

### 配置文件

在 `appsettings.json` 中添加飞书应用配置：

```json
{
  "FeishuApps": [
    {
      "AppKey": "default",
      "AppId": "cli_xxx",
      "AppSecret": "your_app_secret",
      "BaseUrl": "https://open.feishu.cn",
      "IsDefault": true
    }
  ]
}
```

### 注册服务

在 `Program.cs` 中注册飞书服务：

```csharp
// 添加飞书服务
builder.Services.AddFeishuApp(builder.Configuration, "FeishuApps");

// 注册 API 服务
builder.Services.CreateFeishuServicesBuilder()
    .AddModules(FeishuModule.All)
    .Build();
```

### 依赖注入使用

在 Controller 或服务中通过构造函数注入接口：

```csharp
using Mud.Feishu;

public class AilyController : ControllerBase
{
    private readonly IFeishuTenantV1AilyAgent _agentApi;

    public AilyController(IFeishuTenantV1AilyAgent agentApi)
    {
        _agentApi = agentApi;
    }

    [HttpPost("chats")]
    public async Task<IActionResult> CreateChat(string agentId, CreateAgentChatRequest request)
    {
        var result = await _agentApi.CreateAgentChatAsync(agentId, request);
        return Ok(result);
    }
}
```

## API 接口导航

### 智能体

- [智能体（租户）](./FeishuTenantV1AilyAgent.md) — 智能体对话（含 SSE 流式）、会话管理、附件上传与产物下载
- [智能体（用户）](./FeishuUserV1AilyAgent.md) — 用户令牌的智能体能力，额外提供智能体可见性查询

### 技能

- [技能（租户）](./FeishuTenantV1AilySkills.md) — 调用技能、获取技能信息、查询技能列表
- [技能（用户）](./FeishuUserV1AilySkills.md) — 用户令牌的技能调用与管理

### 数据知识

- [数据知识（租户）](./FeishuTenantV1AilyDataKnowledge.md) — 数据知识问答（SSE）、文件上传与数据知识增删改查
- [数据知识（用户）](./FeishuUserV1AilyDataKnowledge.md) — 用户令牌的数据知识问答与管理

### 会话与运行

- [会话与运行（租户）](./FeishuTenantV1AilySessions.md) — 会话增删改查、消息收发、运行创建/查询/取消
- [会话与运行（用户）](./FeishuUserV1AilySessions.md) — 用户令牌的会话、消息与运行管理

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **当前版本**：2.0.9
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
