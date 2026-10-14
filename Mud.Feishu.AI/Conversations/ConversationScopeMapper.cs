// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Abstractions.Conversations;
using FeishuScope = Mud.Feishu.Abstractions.Conversations.ConversationScope;

namespace Mud.Feishu.AI.Conversations;

/// <summary>
/// 会话维度范围的<b>接缝适配器</b>：中立副本 ↔ 已发布 SDK 的冻结契约。
/// </summary>
/// <remarks>
/// <b>为什么需要这个 3 行映射</b>：会话请求（<see cref="ConversationRequest"/>）已中立化，其 <see cref="ConversationRequest.Scope"/>
/// 是中立的 <see cref="ConversationScope"/>；而飞书侧会话键构造器（<c>Mud.Feishu.Abstractions.Conversations.ConversationKeyBuilder</c>）
/// 是<b>已发布 SDK</b> 的冻结契约，只接受它自己的 <see cref="FeishuScope"/>。两者不可能合并（已发布 SDK 不得引用 <c>Mud.AI.*</c>；
/// <c>Mud.AI.*</c> 不得引用 <c>Mud.Feishu*</c>）⇒ 两个世界之间必须有一处显式转换。
/// <para>
/// <b>⚠️ 本文件是唯一允许同时引用两个 <c>ConversationScope</c> 的地方</b>：它用文件级别名 <c>FeishuScope</c> 消歧；其它文件请勿模仿——
/// 需要飞书侧冻结契约时应当反思"这个类型是不是本来就该留在产品侧"。
/// </para>
/// <para>
/// <b>为什么是 public（而不是 internal + IVT）</b>：宿主与 Demo 也会自建会话键（定制工作台、运维脚本、跨包联调）；若只给 IVT，
/// 外部消费者只能自己拼 <c>ConversationKeyBuilder.Build(appKey, FeishuScope.Group(), …)</c>——而这恰好要求他们同时认识两份
/// <c>ConversationScope</c> 并手工挑对的那份（挑错成"群聊写成 user 段"是<b>编译期无声</b>的会话串号）。把桥接点公开，是让接缝<b>可知可信</b>。
/// </para>
/// </remarks>
public static class ConversationScopeMapper
{
    /// <summary>把中立会话维度转换为飞书 SDK 的冻结契约形态。</summary>
    /// <param name="scope">中立会话维度。</param>
    /// <returns>飞书侧会话维度（键构造器入参）。</returns>
    public static FeishuScope ToFeishuScope(ConversationScope scope)
        => scope.IsGroup ? FeishuScope.Group() : FeishuScope.P2P();

    /// <summary>把飞书 SDK 的冻结契约形态转换为中立会话维度（反向映射，供解析回灌场景）。</summary>
    /// <param name="scope">飞书侧会话维度。</param>
    /// <returns>中立会话维度。</returns>
    public static ConversationScope ToNeutralScope(FeishuScope scope)
        => scope.IsGroup ? ConversationScope.Group() : ConversationScope.P2P();

    /// <summary>
    /// 用<b>中立会话维度</b>构造飞书会话键（接缝的唯一"出口"）。
    /// </summary>
    /// <remarks>
    /// 抽成一个方法而不是让每个调用点各写一次 <see cref="ToFeishuScope"/>：会话键的构造点有 5+
    /// 处（事件处理器 / 续跑闭环 / 诊断 / 用例），漏包一个就会退化成"键的维度段静默错误"
    /// ——群聊被当成单聊写进 `user:` 段，表现为会话历史串号且**编译期完全无声**。
    /// </remarks>
    /// <param name="appKey">租户/应用标识（空白视为 default）。</param>
    /// <param name="scope">中立会话维度。</param>
    /// <param name="subjectId">会话主体 ID。</param>
    /// <param name="keyPrefix">键前缀（可空 → 冻结契约的默认前缀）。</param>
    /// <returns>规范化后的会话键。</returns>
    public static string BuildKey(string? appKey, ConversationScope scope, string subjectId, string? keyPrefix = null)
        => ConversationKeyBuilder.Build(appKey, ToFeishuScope(scope), subjectId, keyPrefix);
}
