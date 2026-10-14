// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
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
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
global using Moq;
global using Xunit;

// Mud.AI.Agent 自身命名空间：中性类型经全局 using 保持"简单名即可引用"
// （与 Mud.Feishu.AI.Tests 对 Mud.Feishu.AI.* 的既有做法同口径）。
global using Mud.AI.Abstractions.Channels;
global using Mud.AI.Abstractions.Configuration;
global using Mud.AI.Abstractions.Context;
global using Mud.AI.Abstractions.Conversations;
global using Mud.AI.Abstractions.Observability;
global using Mud.AI.Agent.Approvals;
// N2-e：RAG 契约与预算常量（CorpusDocument / RetrievedChunk / IVectorStore / RetrievalBudgets）。
global using Mud.AI.Abstractions.Knowledge;
global using Mud.AI.Abstractions.Tools.Errors;
global using Mud.AI.Agent.Channels;
global using Mud.AI.Agent.Conversations;
// N2-e：RAG 机制（DocumentChunker / TokenEstimator / VectorRetriever / CorpusIndexer）随类型迁入。
global using Mud.AI.Agent.Knowledge;
global using Mud.AI.Agent.Guidance;
global using Mud.AI.Agent.Sessions;
// N4：装配器族与 MAF 桥接适配器（Context/）。
global using Mud.AI.Agent.Context;
global using Mud.AI.Agent.Tools;
global using Mud.AI.Agent.Tools.Errors;
