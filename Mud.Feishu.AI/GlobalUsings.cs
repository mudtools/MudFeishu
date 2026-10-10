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

// R-9（阶段 5）：Channels / Events / Knowledge 的实现并入本工程后，这两条成为"跨子命名空间的
// 常规依赖"（`Mud.Feishu.AI.Channels` 等不是 `Mud.Feishu.AI.Agents` 的封闭命名空间，
// 且 Options 是本工程 4 个实现类型的构造依赖）——与其在 8 个文件里各写一遍，不如统一到全局。
global using Microsoft.Extensions.Options;
global using Mud.Feishu.AI.Agents;
global using Mud.Feishu.Abstractions.Observability;
global using Mud.Feishu.Abstractions;
global using Mud.Feishu.Abstractions.Configuration;
global using Mud.Feishu.Abstractions.Conversations;
global using Mud.Feishu.Abstractions.EventHandlers;
global using Mud.Feishu.Abstractions.Services;
global using Mud.Feishu.AI.Conversations;
global using Mud.Feishu.AI.Tools;
