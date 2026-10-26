// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Conversations;

/// <summary>
/// 会话键产品政策：<b>"会话键怎么拼"的单一真相</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>定位</b>：会话键是<b>跨处理器共享的产品政策</b>（聊天会话与各类业务事件共用同一套键位），
/// 故落在 <c>Conversations/</c> 的非泛型类型上，而不是任何事件族基类的成员——后者会把
/// 「同一策略」按封闭泛型各存一份，并让调用方为拿一个纯函数而被迫写全类型实参。
/// </para>
/// <para>
/// <b>隔离语义</b>：业务事件（<see cref="ConversationRequest.EventKey"/> 非空）只在<b>前缀段</b>注入
/// 事件命名空间（<c>event-{EventKey}</c>），聊天会话沿用冻结契约的默认前缀。键位与段结构不变——
/// 五段布局与 <see cref="ConversationKeyBuilder.TryParse"/> 回灌逐字节兼容。
/// </para>
/// </remarks>
internal static class ConversationKeyPolicy
{
    /// <summary>事件命名空间前缀标记（拼成 <c>event-{EventKey}</c>）。</summary>
    internal const string EventNamespaceMarker = "event-";

    /// <summary>按会话请求构造会话键。</summary>
    /// <param name="request">已规范化的会话请求。</param>
    /// <returns>会话键。</returns>
    internal static string Build(ConversationRequest request)
        => ConversationScopeMapper.BuildKey(
            request.AppKey,
            request.Scope,
            request.SubjectId,
            PrefixFor(request.EventKey));

    /// <summary>事件键 → 键前缀段（空 ⇒ <see langword="null"/>，沿用冻结契约的默认前缀）。</summary>
    /// <param name="eventKey">事件键（聊天请求为 <see langword="null"/>）。</param>
    /// <returns>键前缀段。</returns>
    internal static string? PrefixFor(string? eventKey)
        => string.IsNullOrEmpty(eventKey) ? null : EventNamespaceMarker + eventKey;
}
