// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

global using System;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Globalization;
global using System.Linq;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Nodes;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Agents.AI;
global using Microsoft.Extensions.AI;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;

// Channels / Events / Knowledge 的实现并入本工程后，这两条成为"跨子命名空间的常规依赖"
// （`Mud.Feishu.AI.Channels` 等不是 `Mud.Feishu.AI.Agents` 的封闭命名空间，且 Options 是本工程 4 个实现类型的构造依赖）。
global using Microsoft.Extensions.Options;
// H2 中立化：域 guidance 装配机制（GuidanceBlock / GuidanceResult / GuidanceComposer）上移
// Mud.AI.Agent.Guidance——预算常量（FeishuGuidanceBudget）留在本产品的 Agents 命名空间。
global using Mud.AI.Agent.Guidance;
// H2 中立化：会话 token 计数（ChatTokenCounter）上移 Mud.AI.Agent.Conversations 并转 public。
global using Mud.AI.Agent.Conversations;
// P1-H3：会话门面（AgentSessionStore：按键存取 + 坏值自愈）上移 Mud.AI.Agent.Sessions。
global using Mud.AI.Agent.Sessions;
// N2 中立化：待确认快照的进程内默认实现（InMemoryPendingApprovalStore）上移 Mud.AI.Agent.Approvals。
global using Mud.AI.Agent.Approvals;
// N2-b 中立化：分片缓冲流式通道基类（BufferedMessageChannel）与降级链（StreamingChannelChain）
// 上移 Mud.AI.Agent.Channels——产品子类（EditMessageChannel / CardStreamMessageChannel）经此派生。
global using Mud.AI.Agent.Channels;
// N4（E5/E6/E8）中立化：事实列表装配器基类与发送者信息装配器上移 Mud.AI.Agent.Context
// ——产品子类与 DI 注册经全局 using 保持简单名引用零改动。
global using Mud.AI.Agent.Context;
// N2-c 中立化：审批态桥 / 投影 / 续跑闭环上移 Mud.AI.Agent.Approvals（产品事实经构造参数注入）。
global using Mud.AI.Agent.Approvals;
global using Mud.Feishu.AI.Agents;
global using Mud.Feishu.Abstractions.Observability;
global using Mud.Feishu.Abstractions;
global using Mud.Feishu.Abstractions.Configuration;
// ⚠️ N1 中立化（**易踩坑，勿"顺手补回来"**）：此处**刻意不**写
//    `global using Mud.Feishu.Abstractions.Conversations;`
// 原因：该命名空间含 `ConversationScope`，而中立包 `Mud.AI.Abstractions.Conversations`
// 含**同名同形**的类型（刻意的接缝，见该类型注释）。两个命名空间同时全局可见 ⇒
// `ConversationScope` 的**每个**使用点报 CS0104，且无法靠"只全局导入中立版"绕过
// （`ConversationKeyBuilder` / `IConversationStore` 仍在飞书侧且被多文件消费）。
// 处置：全局只导入**中立**命名空间；需要飞书侧冻结契约的少量文件（会话存储防腐层、
// 进程内闸门、DI 注册扩展、FeishuAgent）在文件头**显式** `using Mud.Feishu.Abstractions.Conversations;`
// ——这些文件都不使用 `ConversationScope`，故不会产生歧义。
global using Mud.Feishu.Abstractions.EventHandlers;
global using Mud.Feishu.Abstractions.Services;
global using Mud.Feishu.AI.Conversations;
// 飞书侧**冻结契约**中"无中立同名孪生"的两个类型，用**全局别名**取用：`ConversationKeyBuilder` 与
// `IConversationStore` 被 13 个文件消费，逐文件加 using 会让「哪些文件还能看见冻结命名空间」变成散落的隐式事实。
// ⚠️ 反过来说：除这两个（以及下述文件显式 using 的闸门类型）之外，**不得**在本工程再引用
// `Mud.Feishu.Abstractions.Conversations` 的其它成员——那会立刻把 `ConversationScope` 的 CS0104 歧义带回来。
global using ConversationKeyBuilder = Mud.Feishu.Abstractions.Conversations.ConversationKeyBuilder;
// N1 决策：会话闸门**不**中立化（见方案 §4.4 D3/D4 的评审订正）——它的
// `IConversationGate` / `IConversationGateHandle` 是**已发布 SDK 的宿主可替换契约**
// （Redis 包实现它），搬进中立包后既无法再实现该冻结接口、又会引入第二个"可替换槽位"。
// 故用同名全局别名把"这个词指飞书契约"钉成一处声明；中立管线改经 ③式委托消费闸门。
global using IConversationGate = Mud.Feishu.Abstractions.Conversations.IConversationGate;
global using IConversationGateHandle = Mud.Feishu.Abstractions.Conversations.IConversationGateHandle;
global using IConversationStore = Mud.Feishu.Abstractions.Conversations.IConversationStore;
// ─── N1 中立化：中立契约命名空间的全局导入（"④ 门面保留"的落地手段）──────────────────
// 契约与值类型搬到中立包后，产品侧调用点经全局 using 保持零改动；
// 不搬的（产品语义类型）仍留在 Mud.Feishu.AI.* / Mud.Feishu.Abstractions.*。
global using Mud.AI.Abstractions.Approvals;
global using Mud.AI.Abstractions.Channels;
global using Mud.AI.Abstractions.Configuration;
global using Mud.AI.Abstractions.Context;
global using Mud.AI.Abstractions.Conversations;
global using Mud.AI.Abstractions.Knowledge;
global using Mud.AI.Abstractions.Media;
global using Mud.AI.Abstractions.Observability;
// BUG-1 / 方案 C：本包保留的"AI 运行时 ↔ 工具面共享接缝"（工具源、租户作用域工厂、
// 上下文访问器、审批通道、风险/身份/内容安全词汇、诊断、API 结果读取）落在本包自有命名空间
// Mud.Feishu.AI.AgentTools；工具契约与实现（注册表/结果/属性/目录/授权/审计/错误/附件暂存）
// 已回迁 Mud.Feishu.AI.Tools 并保持命名空间 Mud.Feishu.AI.Tools（归属唯一，由
// PackageOwnershipContractGuards 锁定）。
global using Mud.Feishu.AI.AgentTools;
