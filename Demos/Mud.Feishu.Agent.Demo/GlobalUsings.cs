// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

// N1 中立化：Demo 作为"宿主视角"的消费方，同样需要中立的契约命名空间才能零改动地
// 继续按简单名引用会话请求 / 流式通道 / 宿主批准通道（Demo 是**产品语义之外**的消费者，
// 正好是"中立层确实被复用"的活证据）。
global using Mud.AI.Abstractions.Approvals;
global using Mud.AI.Abstractions.Channels;
global using Mud.AI.Abstractions.Configuration;
global using Mud.AI.Abstractions.Context;
global using Mud.AI.Abstractions.Conversations;
global using Mud.AI.Abstractions.Knowledge;
global using Mud.AI.Abstractions.Media;
// 飞书侧冻结契约（会话存储 / 会话闸门）——Demo 直接驱动它们做宿主视角装配，
// 用全局别名取用（与生产侧 GlobalUsings.cs 同一纪律：不得整体导入该命名空间）。
global using IConversationGate = Mud.Feishu.Abstractions.Conversations.IConversationGate;
global using IConversationStore = Mud.Feishu.Abstractions.Conversations.IConversationStore;
global using ConversationKeyBuilder = Mud.Feishu.Abstractions.Conversations.ConversationKeyBuilder;
// 会话维度接缝适配器（中立 ConversationScope ↔ 飞书冻结契约 ConversationScope）：
// 中立化后**推荐**的键构造入口是 ConversationScopeMapper.BuildKey(...)。
global using Mud.Feishu.AI.Conversations;
