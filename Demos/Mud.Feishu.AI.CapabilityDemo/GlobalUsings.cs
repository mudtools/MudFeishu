// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

global using System.Collections.Concurrent;
global using System.Globalization;
global using System.Text;
global using System.Text.Json;

global using Microsoft.Agents.AI;
global using Microsoft.Extensions.AI;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using Mud.AI.Abstractions.Approvals;
global using Mud.AI.Abstractions.Conversations;
global using Mud.Feishu.AI.Agents;
global using Mud.Feishu.AI.AgentTools;
global using Mud.Feishu.AI.Conversations;
global using Mud.Feishu.AI.Extensions;
global using Mud.Feishu.AI.Tools;
global using Mud.Feishu.AI.Tools.Catalog;
global using Mud.Feishu.AI.Tools.Hosting;

// 飞书侧冻结契约（会话存储/闸门）只按别名取用，不整体导入命名空间
// （与生产侧 GlobalUsings 同一纪律：避免与中立层同名类型混淆）。
global using IConversationGate = Mud.Feishu.Abstractions.Conversations.IConversationGate;
global using IConversationStore = Mud.Feishu.Abstractions.Conversations.IConversationStore;
