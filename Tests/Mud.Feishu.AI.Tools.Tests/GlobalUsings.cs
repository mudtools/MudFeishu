// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

global using System.Text.Json;
global using FluentAssertions;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Options;
global using Moq;
// v2.3 P1-D：执行器骨架中立化上移 Mud.AI.Agent.Tools / .Errors（ToolResult / ToolApiOutcome /
// ToolErrorCategory / IToolResultShaper …）——测试与被测工程同口径经全局 using 引用。
global using Mud.AI.Agent.Tools;
global using Mud.AI.Agent.Tools.Errors;
// v2.3 P1-E：声明契约（[AgentTool] / [ToolParameter] / [ToolHandler]）上移 Mud.AI.Abstractions。
global using Mud.AI.Abstractions.Tools;
global using Mud.AI.Abstractions.Tools.Handlers;
global using Mud.Feishu.AI.Agents;
global using Mud.Feishu.AI.Knowledge;
global using Mud.Feishu.AI.AgentTools;
global using Mud.Feishu.AI.Tools;
global using Mud.Feishu.AI.Tools.Args;
global using Mud.Feishu.AI.Tools.Catalog;
global using Mud.Feishu.Tests.Docs;
global using Mud.Feishu.AI.Tools.Constants;
global using Mud.Feishu.AI.Tools.Errors;
global using Mud.Feishu.AI.Tools.Execution;
global using Mud.Feishu.AI.Tools.Hosting;
global using Mud.Feishu.AI.Tools.Parsers;
global using Mud.Feishu.AI.Tools.Results;
global using Mud.Feishu.AI.Tools.Security;
global using Mud.Feishu.AI.Tools.Generated;
// v2.3 P1-E：执行器绑定特性 [ToolHandler] 随声明契约上移 Mud.AI.Abstractions.Tools.Handlers
// （原 Mud.Feishu.AI.Tools.Handlers 命名空间随特性文件删除）——与 [AgentTool] 所在的
// Mud.AI.Abstractions.Tools 刻意不同：剖面靠这两个命名空间差异区分「工具声明面」与
// 「执行器绑定面」，塌陷即失去可寻址性。
global using Mud.Feishu.AI.Tools.Internal;
global using Mud.Feishu.Abstractions.Observability;
global using Mud.Feishu.DataModels;
global using Xunit;
