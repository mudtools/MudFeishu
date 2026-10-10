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
/// <param name="SubjectId">会话主体 ID（群聊 chat_id / 单聊 user_id）；语义收敛为「会话键维度」。</param>
/// <param name="SenderId">发送者用户 ID。</param>
/// <param name="MessageId">触发消息 ID（回复定位用）。</param>
/// <param name="MentionedText">@提及后提取的指令文本（可空）。</param>
/// <param name="ChatId">回复/流式目标会话 ID（可空；im 事件恒为 message.chat_id，p2p/group 均可用）。
/// 缺省时流式/工具上下文目标回退既有逻辑（群聊取 <see cref="SubjectId"/>）——单聊流式因此解锁
/// （AI-FD-D12 P2D-2b：会话键维度与回复目标解耦）。</param>
/// <param name="ParentId">引用（父）消息 ID（可空；<c>QuoteMessageContextAssembler</c> 消费，P2D-5b）。</param>
public sealed record ConversationRequest(
    string AppKey,
    ConversationScope Scope,
    string SubjectId,
    string SenderId,
    string MessageId,
    string? MentionedText,
    string? ChatId = null,
    string? ParentId = null,

    // R5 / F-4：话题（thread）维度。群话题场景下 Feishu 会把消息归入 thread；
    // 缺此字段则 thread 类工具（im.get_thread_messages / forward_thread）与
    // im.reply_message 的 reply_in_thread 在真实场景里拿不到必要入参。
    // 追加为**末尾可选参数** ⇒ 既有 8 个位置参数的调用点全部源码兼容。
    // 不变式：**流式目标恒为 chat_id**（thread 只作上下文与路由维度，不改流式目标）。
    string? ThreadId = null,

    // R7 / C2（T3-5）：事件维度。上下文装配器只拿到本记录（见 IContextAssembler），
    // 故"把事件载荷转成结构化 prompt 片段"必须先有载体的接缝：
    // EventKey = 事件类型键（如 approval_task / task_updated），EventFacts = 已归一化的事实键值对。
    // 追加为**末尾可选参数** ⇒ 既有调用点全部源码兼容；未填充时相关装配器返回空片段（降级为既有行为）。
    // ⚠️ EventFacts 的值可能含用户内容 ⇒ 消费方（装配器）必须带 untrusted 标注（ContextBudgets.UntrustedHeader）。
    string? EventKey = null,
    IReadOnlyDictionary<string, string?>? EventFacts = null);
