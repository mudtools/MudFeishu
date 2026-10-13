// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
// v2.3 P1-D：执行器骨架中立化上移 Mud.AI.Agent（决策⑮）——工具执行骨架（ToolExecutor / ToolResult /
// ToolPagination / ToolDryRun / ToolResultPipeline / ToolErrorFactory / ToolErrorText / ToolApiOutcome …）
// 与错误契约词汇经全局 using 保持"简单名即可引用"（与生成产物同口径）。
global using Mud.AI.Agent.Tools;
global using Mud.AI.Agent.Tools.Errors;
// v2.3 P1-E：声明契约（`[AgentTool]` / `[ToolParameter]` / `[ToolHandler]`）中立化上移
// Mud.AI.Abstractions——策展接口与执行器绑定面经全局 using 直接引用新名（308 + 244 处调用点零改动）。
global using Mud.AI.Abstractions.Tools;
global using Mud.AI.Abstractions.Tools.Handlers;
global using Mud.Feishu.Abstractions.Observability;
global using Mud.Feishu.AI.Agents;
// BUG-1 / 方案 C：AI 包侧"共享接缝"命名空间（FeishuToolContext / IFeishuToolContextAccessor /
// IFeishuAppContextScopeFactory / IFeishuToolApprovalChannel / FeishuToolRiskNames /
// FeishuToolDiagnostics / FeishuApiResultReader …）。工具面契约仍在 Mud.Feishu.AI.Tools。
global using Mud.Feishu.AI.AgentTools;
global using Mud.Feishu.AI.Tools;
global using Mud.Feishu.AI.Tools.Generated;
// v2.3 P1-E：[ToolHandler] 随声明契约上移 `Mud.AI.Abstractions.Tools.Handlers`（原
// `Mud.Feishu.AI.Tools.Handlers` 命名空间随该特性文件一并删除）——它**刻意不等于**
// [AgentTool] 所在的 `Mud.AI.Abstractions.Tools`：剖面用 ToolAttributeNamespace /
// ToolHandlerAttributeNamespace 两个槽区分两类特性，合并即失去可寻址性。
// （该全局 using 由上方 `Mud.AI.Abstractions.Tools.Handlers` 覆盖，此处不再重复声明。）

// R5 / F-1（载体 C）：工具声明面（87 个 [AgentTool] 契约接口）迁到独立目录 Curation/ 与
// 独立命名空间，使其与 Tools/ 下的运行时基础设施（参数净化、结果裁剪、错误分类、DI…）在
// **物理与逻辑上都可区分**——"人工策展什么"与"框架提供什么"不再混在同一目录。
// 之所以用全局 using 而非逐文件添加：这批接口是本程序集的核心词汇（每个执行器都实现其中几个），
// 逐文件添加会产生 24+ 处无信息量的 using 行；GlobalUsings.cs 已是本仓既有机制。
global using Mud.Feishu.AI.Tools.Curation;
// R-1+2c 后续重组（Tools/ 按关注面解散为 9 目录）：Args/Results/Errors/Execution/Catalog/
// Hosting/Security/Parsers/Constants 各自声明子命名空间，经全局 using 保持"简单名即可引用"——
// 源生成器按简单名引用 ToolResult/FeishuToolBinding（剖面 ResultTypeName/BindingTypeName 槽），
// 本批 using 是生成代码与全包词汇表不掉线的保证。
global using Mud.Feishu.AI.Tools.Args;
global using Mud.Feishu.AI.Tools.Catalog;
global using Mud.Feishu.AI.Tools.Constants;
global using Mud.Feishu.AI.Tools.Errors;
global using Mud.Feishu.AI.Tools.Execution;
global using Mud.Feishu.AI.Tools.Hosting;
global using Mud.Feishu.AI.Tools.Parsers;
global using Mud.Feishu.AI.Tools.Results;
global using Mud.Feishu.AI.Tools.Security;
global using Mud.Feishu.DataModels;
// R-1+2c：Curation/ 的 [AgentTool(Source = …)] 由字面量改为「nameof(接口) + "." + nameof(接口.方法)」
// 常量拼接形态（符号锚定 → IDE 重命名自动联动）。SDK 接口全部落在 Mud.Feishu 命名空间，故引入本 using
// 让 84 处声明免写全限定名（本工程内部/执行器仍保留 Mud.Feishu.IFeishuX 的全限定风格）。
global using Mud.Feishu;
global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Globalization;
global using System.Linq;
global using System.Text.Json.Nodes;
global using System.Threading;
global using System.Threading.Tasks;
