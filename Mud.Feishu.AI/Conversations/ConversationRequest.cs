// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Conversations;

/// <summary>
/// 由飞书事件规范化出的「会话请求」：Agent 管线（会话键 → 上下文装配 → Run → 回复）的统一输入。
/// </summary>
/// <remarks>
/// <para>
/// Prompt 统一由 <c>IContextAssembler</c> 装配，本记录只携带结构化事实（发送者、会话主体、
/// @提及文本），不冗余携带最终用户消息文本。
/// </para>
/// </remarks>
/// <param name="AppKey">应用唯一标识（租户/应用维度，进入会话键）。</param>
/// <param name="Scope">会话维度（群聊按 chat、单聊按 user）。</param>
/// <param name="SubjectId">会话主体 ID（群聊 chat_id / 单聊 user_id）。</param>
/// <param name="SenderId">发送者用户 ID。</param>
/// <param name="MessageId">触发消息 ID（回复定位用）。</param>
/// <param name="MentionedText">@提及后提取的指令文本（可空）。</param>
public sealed record ConversationRequest(
    string AppKey,
    ConversationScope Scope,
    string SubjectId,
    string SenderId,
    string MessageId,
    string? MentionedText);
