// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Abstractions.Context;

namespace Mud.AI.Abstractions.Conversations;

/// <summary>
/// 由外部事件规范化出的「会话请求」：Agent 管线（会话键 → 上下文装配 → Run → 回复）的统一输入。
/// </summary>
/// <remarks>
/// <para>
/// Prompt 统一由 <c>IContextAssembler</c> 装配，本记录只携带结构化事实（发送者、会话主体、
/// 触发文本），不冗余携带最终用户消息文本。
/// </para>
/// <para>
/// <b>中立化说明</b>：本类型自产品适配层迁入。
/// ① <see cref="Scope"/> 使用本包的<b>中立副本</b> <see cref="ConversationScope"/>
/// （与产品侧同名类型的"刻意接缝"理由见该类型的注释）；
/// ② <see cref="EventFacts"/> 的元素类型为中立 <see cref="EventFact"/>；
/// ③ 事件族判别键（<see cref="EventKey"/>）是<b>产品事实</b>——中立侧只承载字符串，
/// 取值单一源留在产品侧。
/// </para>
/// <para>
/// <b>为什么 <see cref="AppKey"/> 不改名 <c>TenantKey</c></b>：它是「租户/应用维度标识」这一
/// 通用概念的属性名（非产品商标词汇），且被下游 ~19 个文件（含 Derivation 与测试）消费；
/// 改名只增加改动面、不产生任何上游化收益。真正的产品词汇（<c>feishu.*</c> 命名面、
/// <c>x-feishu.*</c> 词汇、飞书实测预算值）由中立性守卫严格锁定。
/// 端口<b>参数</b>层仍按定式统一为 <c>tenantKey</c>（参数名不影响二进制兼容，改造成本为零）。
/// </para>
/// </remarks>
/// <param name="AppKey">租户/应用维度标识（进入会话键）。</param>
/// <param name="Scope">会话维度（群聊按 chat、单聊按 user）。</param>
/// <param name="SubjectId">会话主体 ID（群聊 chat_id / 单聊 user_id）；语义收敛为「会话键维度」。</param>
/// <param name="SenderId">发送者用户 ID。</param>
/// <param name="MessageId">触发消息 ID（回复定位用）。</param>
/// <param name="MentionedText">触发指令文本（可空）。</param>
/// <param name="ChatId">回复/流式目标会话 ID（可空）。缺省时流式目标回退既有逻辑
/// （群聊取 <see cref="SubjectId"/>）——单聊流式因此解锁（会话键维度与回复目标解耦）。</param>
/// <param name="ParentId">引用（父）消息 ID（可空；引用消息装配器消费）。</param>
/// <param name="ThreadId">话题（thread）维度（可空）。不变式：<b>流式目标恒为 chat_id</b>
/// （thread 只作上下文与路由维度，不改流式目标）。</param>
/// <param name="EventKey">事件族判别键（可空；取值单一源见产品侧常量）。</param>
/// <param name="EventFacts">有序事实列表（可空）。值可能含用户内容 ⇒ 消费方必须带 untrusted 标注。</param>
public sealed record ConversationRequest(
    string AppKey,
    ConversationScope Scope,
    string SubjectId,
    string SenderId,
    string MessageId,
    string? MentionedText,
    string? ChatId = null,
    string? ParentId = null,
    string? ThreadId = null,
    string? EventKey = null,
    IReadOnlyList<EventFact>? EventFacts = null);
