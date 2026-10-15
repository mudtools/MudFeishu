// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text.Json;
global using System.Threading;
global using System.Threading.Tasks;
global using FluentAssertions;
global using Microsoft.Agents.AI;
global using Microsoft.Extensions.AI;
global using Microsoft.Extensions.DependencyInjection;

// R-9（阶段 5）：Channels / Events / Knowledge 的用例随集成面迁入本工程后，
// 下面三条成为常规依赖（Options.Create 造 Agent 选项、DataModels 的 FeishuApiResult<T> 造客户端返回、
// Knowledge 的 AilyKnowledgeOptions/IRetriever）——集中登记，避免 9 个文件各写一遍。
global using Microsoft.Extensions.Options;
global using Mud.Feishu.DataModels;
global using Mud.Feishu.AI.Knowledge;
global using Mud.Feishu.Tests.Docs;

global using Moq;
// H2 中立化：域 guidance 装配机制上移 Mud.AI.Agent.Guidance（预算常量留在 FeishuGuidanceBudget）、
// 会话 token 计数上移 Mud.AI.Agent.Conversations。
global using Mud.AI.Agent.Conversations;
global using Mud.AI.Agent.Guidance;
global using Mud.Feishu.AI;
global using Mud.Feishu.AI.Agents;
global using Mud.Feishu.AI.Conversations;
global using Mud.Feishu.AI.Extensions;
global using Mud.Feishu.Abstractions.Configuration;
global using Mud.Feishu.Abstractions.Services;
// ⚠️ N1 中立化：与生产工程同款纪律——**不得**全局导入
// `Mud.Feishu.Abstractions.Conversations`（其 `ConversationScope` 与中立副本同名 ⇒ CS0104）。
// 需要飞书侧冻结契约的类型用**全局别名**取用（与生产侧 GlobalUsings.cs 同口径）。
global using ConversationKeyBuilder = Mud.Feishu.Abstractions.Conversations.ConversationKeyBuilder;
global using IConversationGate = Mud.Feishu.Abstractions.Conversations.IConversationGate;
global using IConversationGateHandle = Mud.Feishu.Abstractions.Conversations.IConversationGateHandle;
global using IConversationStore = Mud.Feishu.Abstractions.Conversations.IConversationStore;
global using SessionStoreEncoding = Mud.Feishu.Abstractions.Conversations.SessionStoreEncoding;
global using Mud.AI.Abstractions.Approvals;
global using Mud.AI.Abstractions.Channels;
global using Mud.AI.Abstractions.Configuration;
global using Mud.AI.Abstractions.Context;
global using Mud.AI.Abstractions.Conversations;
global using Mud.AI.Abstractions.Knowledge;
global using Mud.AI.Abstractions.Media;
global using Mud.AI.Abstractions.Observability;
global using Mud.AI.Agent.Approvals;
// N2-b 中立化：缓冲通道基类与降级链上移 Mud.AI.Agent.Channels——产品子类用例
// （EditMessageChannelTests / CardStreamMessageChannelTests）与黄金快照经此取用中立类型。
global using Mud.AI.Agent.Channels;
// N2-c 中立化：审批态桥 / 框架审批投影上移 Mud.AI.Agent.Approvals（产品侧经此消费）。
global using Mud.AI.Agent.Approvals;
// N2-e 中立化：RAG 机制（DocumentChunker 等）上移 Mud.AI.Agent.Knowledge——黄金快照的
// 分块用例经此取用中立类型（机制自身用例已随类型迁入 Mud.AI.Agent.Tests）。
global using Mud.AI.Agent.Knowledge;
// BUG-1：集成面用例消费的接缝类型在 Mud.Feishu.AI.AgentTools；工具契约类型（FeishuToolRegistry 等）
// 已回迁工具包，其用例随迁至 Mud.Feishu.AI.Tools.Tests。
global using Mud.Feishu.AI.AgentTools;
// v2.3 P1-E：本工程的**样例策展接口**（Tools/FeishuToolInterfaces.cs）与生产侧同口径改用
// 中立声明契约（[AgentTool] / [ToolParameter]，随契约上移 Mud.AI.Abstractions）。
global using Mud.AI.Abstractions.Tools;
global using Xunit;
