// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Abstractions.Conversations;

/// <summary>
/// 会话维度范围（<b>中立副本</b>）：标识会话键归属的聊天形态。
/// </summary>
/// <remarks>
/// <para>
/// <b>⚠️ 与 <c>Mud.Feishu.Abstractions.Conversations.ConversationScope</c> 同名同形是刻意为之，
/// 不是可合并的重复类型 —— 下一位维护者请勿"顺手删掉一份"：</b>
/// <list type="bullet">
/// <item>飞书侧那一份是<b>已发布 SDK</b> 的公开契约（`Mud.Feishu.Abstractions` 是字节冻结包，
/// 由 <c>CoreSdkProjects_ShouldNotDependOnMudAiFamily</c> 机械锁定"不得引用 Mud.AI.*"）；</item>
/// <item>本中立包被 <c>Mud.AI.*</c> 独占，<b>不得</b>反向引用任何 <c>Mud.Feishu*</c>
/// （由 <c>MudAiFamily_ShouldNotDependOnFeishuProjects</c> 锁定）。</item>
/// </list>
/// 两条方向性不变量同时成立 ⇒ <b>两者不可能合并为一个类型</b>。形状相同、生命周期与归属不同 ⇒
/// 这是<b>有意的接缝</b>（与 <c>ISessionStore</c>/<c>IConversationStore</c> 同款定式，
/// 见 P1-H3 的防腐层设计）。飞书侧的桥接点为
/// <c>Mud.Feishu.AI.Conversations.ConversationScopeMapper</c>。
/// </para>
/// <para>
/// 群聊按 <c>chat_id</c>、单聊按 <c>user_id</c> 隔离；<see cref="IsGroup"/> 为
/// <see langword="true"/> 时主体 ID 的语义是 chat_id，否则是发送者的 user_id。
/// </para>
/// </remarks>
/// <param name="ChatType">原始聊天类型（<see cref="ChatTypeP2P"/> / <see cref="ChatTypeGroup"/>）。</param>
/// <param name="IsGroup">是否群聊。</param>
public readonly record struct ConversationScope(string ChatType, bool IsGroup)
{
    /// <summary>单聊（p2p）聊天类型。</summary>
    public const string ChatTypeP2P = "p2p";

    /// <summary>群聊（group）聊天类型。</summary>
    public const string ChatTypeGroup = "group";

    /// <summary>单聊会话维度：以发送者 user_id 为会话主体。</summary>
    public static ConversationScope P2P() => new(ChatTypeP2P, IsGroup: false);

    /// <summary>群聊会话维度：以 chat_id 为会话主体。</summary>
    public static ConversationScope Group() => new(ChatTypeGroup, IsGroup: true);
}
