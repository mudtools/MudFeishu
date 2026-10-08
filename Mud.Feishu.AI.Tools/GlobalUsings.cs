// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Mud.Feishu.Abstractions.Observability;
global using Mud.Feishu.AI.Agents;
global using Mud.Feishu.AI.Tools;
global using Mud.Feishu.AI.Tools.Generated;
// [FeishuToolHandler] 独占 Handlers/ 命名空间，**刻意不等于** [FeishuTool] 所在的 Mud.Feishu.AI.Tools：
// 剖面用 ToolAttributeNamespace / ToolHandlerAttributeNamespace 两个槽区分两类特性，合并即失去可寻址性。
global using Mud.Feishu.AI.Tools.Handlers;

// R5 / F-1（载体 C）：工具声明面（87 个 [FeishuTool] 契约接口）迁到独立目录 Curation/ 与
// 独立命名空间，使其与 Tools/ 下的运行时基础设施（参数净化、结果裁剪、错误分类、DI…）在
// **物理与逻辑上都可区分**——"人工策展什么"与"框架提供什么"不再混在同一目录。
// 之所以用全局 using 而非逐文件添加：这批接口是本程序集的核心词汇（每个执行器都实现其中几个），
// 逐文件添加会产生 24+ 处无信息量的 using 行；GlobalUsings.cs 已是本仓既有机制。
global using Mud.Feishu.AI.Tools.Curation;
global using Mud.Feishu.DataModels;
// R-1+2c：Curation/ 的 [FeishuTool(Source = …)] 由字面量改为「nameof(接口) + "." + nameof(接口.方法)」
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
