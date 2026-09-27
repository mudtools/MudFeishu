// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Conversations;

/// <summary>
/// 会话维度范围：标识会话键归属的聊天形态。
/// </summary>
/// <remarks>
/// <para>
/// 群聊按 <c>chat_id</c>、单聊按 <c>user_id</c> 隔离（路线图主线一 §4.3）；
/// <see cref="IsGroup"/> 为 <see langword="true"/> 时主体 ID 的语义是 chat_id，
/// 否则是发送者的 user_id。维度选择由 <see cref="ConversationKeyBuilder"/> 依据
/// <see cref="IsGroup"/> 统一施加（<c>chat</c>/<c>user</c> 键段），禁止调用方自行拼接。
/// </para>
/// </remarks>
/// <param name="ChatType">飞书原始聊天类型（<see cref="ChatTypeP2P"/> / <see cref="ChatTypeGroup"/>）。</param>
/// <param name="IsGroup">是否群聊。</param>
public readonly record struct ConversationScope(string ChatType, bool IsGroup)
{
    /// <summary>单聊（p2p）聊天类型。</summary>
    public const string ChatTypeP2P = "p2p";

    /// <summary>群聊（group）聊天类型。</summary>
    public const string ChatTypeGroup = "group";

    /// <summary>
    /// 单聊会话维度：以发送者 user_id 为会话主体。
    /// </summary>
    public static ConversationScope P2P() => new(ChatTypeP2P, IsGroup: false);

    /// <summary>
    /// 群聊会话维度：以 chat_id 为会话主体。
    /// </summary>
    public static ConversationScope Group() => new(ChatTypeGroup, IsGroup: true);
}
