// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// <summary>
// 写类工具接口集（发消息 / 新增记录 / 发起审批实例，全部 <c>IsWrite = true</c>）。
// </summary>
// <remarks>
// <para>
// 复用 <c>[AgentTool]</c> 源生成器（<c>IsWrite</c> 经 Schema 扩展 <c>x-feishu.is_write</c>
// 产出）；执行链（<c>FeishuToolBinding</c>）对写工具强制授权门禁——
// <c>EnforceToolAuthorization=true</c> 且未注册 <c>IToolExecutionAuthorizer</c> 即拒绝（安全默认），
// 白名单单独键控（<c>FeishuAgent:WriteAllowList</c>，默认空=不启用任何写工具）。
// </para>
// <para>
// 与只读工具同一约定：只声明模型可见 Schema（扁平参数）；请求体（<c>SendMessageRequest</c>/
// <c>RecordOpsRequest</c>/<c>CreateInstanceRequest</c>）由绑定层构造。
// </para>
// </remarks>

// ─────────────────────────── IM 写（1 个） ───────────────────────────

/// <summary>工具接口：im.send_message（映射 <c>IFeishuTenantV1Message.SendMessageAsync</c>）。</summary>
[AgentTool("im.send_message",
    Description = "发送文本消息到指定群聊或用户。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。首次面向陌生接收者时建议先以 dry_run=true 预演。",
    RequiredScopes = ["im:message:send_as_bot"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.SendMessageAsync))]
public interface IFeishuTenantImSendMessageTool
{
    /// <summary>发送文本消息。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> SendMessageAsync(
        [ToolParameter("receive_id", "接收者 ID（群聊 ocXxx 或用户 open_id；open_id 可由 contact.search_user 获取）", Required = true)] string receive_id,
        [ToolParameter("text", "消息文本内容（纯文本）", Required = true)] string text,
        [ToolParameter("receive_id_type", "接收者 ID 类型（可选：chat_id=群聊 / open_id=用户 / user_id / union_id / email，默认 chat_id）")] string? receive_id_type = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同键在 1 小时内至多成功发送一条消息（平台侧去重）；省略时不保证幂等。建议由调用方给出稳定值（如「单据号+动作」），不要用随机数。幂等键不跨工具共享（不同工具的同名键互不影响）。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不发送（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：im.send_card（映射 <c>IFeishuTenantV1Message.SendMessageAsync</c>，<c>msg_type=interactive</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>消除模型手造 JSON。</b> <c>im.reply_message</c> 的 <c>content</c>
/// 要求模型自造卡片 JSON，官方明确禁止该做法。本工具改为接受<b>结构化描述</b>，
/// 由 <c>CardDsl</c> 在工具实现内编译为卡片 JSON —— <b>模型全程零 JSON 字符串</b>。
/// </para>
/// <para>
/// <b>发送路径（不造第三套）</b>：复用 <c>im.send_message</c> 的
/// <c>SendMessageAsync</c> 单条发送路径。流式卡片通道 <c>CardStreamMessageChannel</c>
/// 用于"边生成边刷新"的流式更新（占位卡 + sequence 递增），与本工具的"发送一张静态卡片"
/// 是<b>不同场景</b>，故不复用。
/// </para>
/// <para>
/// <b>不做</b>：不做通用卡片 JSON 透传（会退回"模型自造 JSON"），
/// 也不做用户可写表达式 DSL（官方 27KB 规模）。
/// </para>
/// </remarks>
[AgentTool("im.send_card",
    Description = "发送一张交互式卡片（标题＋正文＋按钮），用于让对方一眼看到要点并能直接点按钮——比纯文本更适合通知、待办、审批提醒。⚠️ 用结构化语法描述内容，**不要写 JSON**：body 每行一个元素（text:内容 / quote:内容 / code:内容 / divider），buttons 每行一个按钮（按钮文本|url|链接 或 按钮文本|value|回调值）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。",
    RequiredScopes = ["im:message:send_as_bot"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.SendMessageAsync))]
public interface IFeishuTenantImSendCardTool
{
    /// <summary>发送交互式卡片。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id）；<c>dry_run=true</c> 时返回请求摘要与编译出的卡片结构，不调用下游。</returns>
    Task<string> SendCardAsync(
        [ToolParameter("receive_id", "接收者 ID（群聊 ocXxx 或用户 open_id；open_id 可由 contact.search_user 获取）", Required = true)] string receive_id,
        [ToolParameter("body", "卡片正文（每行一个元素，按顺序渲染）：text:内容=段落 / quote:内容=引用 / code:内容=代码块 / divider=分隔线（不带内容）。示例：text:任务已分配\\nquote:请今日内处理", Required = true)] string body,
        [ToolParameter("title", "卡片标题（可选，显示在卡片顶部）")] string? title = null,
        [ToolParameter("buttons", "按钮列表（可选，每行一个）：形如 '查看任务|url|https://…'（跳转链接）或 '已处理|value|done'（回调给机器人）。第一个按钮为主色。")] string? buttons = null,
        [ToolParameter("receive_id_type", "接收者 ID 类型（可选：chat_id=群聊 / open_id=用户 / user_id / union_id / email，默认 chat_id）")] string? receive_id_type = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同键在 1 小时内至多成功发送一条消息（平台侧去重）；省略时不保证幂等。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不发送（可选，默认 false）：返回将要下发的 method/path、**编译出的卡片结构**与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── IM 写面成环（4 个） ───────────────────────────

/// <summary>工具接口：im.edit_message（映射 <c>IFeishuTenantV1Message.EditMessageAsync</c>）。</summary>
/// <remarks>只能编辑本 Bot 发送的文本消息（平台限制）；PUT 整段覆盖天然幂等——重复提交相同内容结果一致。</remarks>
[AgentTool("im.edit_message",
    Description = "编辑本 Bot 已发送的文本消息内容（发错字时纠正，对方看到的消息被更新）。message_id 来自 im.send_message 的返回。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。",
    RequiredScopes = ["im:message"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.EditMessageAsync))]
public interface IFeishuTenantImEditMessageTool
{
    /// <summary>编辑文本消息。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> EditMessageAsync(
        [ToolParameter("message_id", "待编辑的消息 ID（om_ 开头；来自 im.send_message 的返回）", Required = true)] string message_id,
        [ToolParameter("text", "新的消息文本内容（纯文本）", Required = true)] string text,
        [ToolParameter("dry_run", "仅预演不编辑（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：im.patch_card_message（映射 <c>IFeishuTenantV1Message.PatchMessageAsync</c>）。</summary>
/// <remarks>
/// <b>模型全程零 JSON。</b> 与 <c>im.send_card</c> 同一套 <c>CardDsl</c> 结构化语法——
/// 整卡覆盖语义（每次提交完整新卡片，不做局部 patch）。
/// </remarks>
[AgentTool("im.patch_card_message",
    Description = "更新本 Bot 已发送的卡片消息内容（整卡覆盖）。⚠️ 用结构化语法描述内容，**不要写 JSON**：body 每行一个元素（text:内容 / quote:内容 / code:内容 / divider），buttons 每行一个按钮（按钮文本|url|链接 或 按钮文本|value|回调值）。message_id 来自 im.send_card 的返回。写操作：默认空名单不启用，需 im:message。",
    RequiredScopes = ["im:message"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.PatchMessageAsync))]
public interface IFeishuTenantImPatchCardMessageTool
{
    /// <summary>更新卡片消息。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id/updated=true）；<c>dry_run=true</c> 时返回编译出的卡片结构，不调用下游。</returns>
    Task<string> PatchCardMessageAsync(
        [ToolParameter("message_id", "待更新的卡片消息 ID（om_ 开头；来自 im.send_card 的返回）", Required = true)] string message_id,
        [ToolParameter("body", "卡片正文（每行一个元素，按顺序渲染）：text:内容=段落 / quote:内容=引用 / code:内容=代码块 / divider=分隔线（不带内容）", Required = true)] string body,
        [ToolParameter("title", "卡片标题（可选，显示在卡片顶部）")] string? title = null,
        [ToolParameter("buttons", "按钮列表（可选，每行一个）：形如 '查看任务|url|https://…'（跳转链接）或 '已处理|value|done'（回调给机器人）。第一个按钮为主色。")] string? buttons = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path、编译出的卡片结构与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：im.urgent_app（映射 <c>IFeishuTenantV1Message.MessageUrgentAppAsync</c>）。</summary>
/// <remarks>加急是<b>高打扰</b>动作（对方客户端强提醒）——描述中保留"确有需要时才用"的使用纪律；工具名不含 profile 危险词，故风险级别为普通写（非 high-risk-write）。</remarks>
[AgentTool("im.urgent_app",
    Description = "把一条已发送消息加急给指定用户（飞书客户端来弹窗强提醒）——适用于对方可能漏看的紧急场景。⚠️ 会打扰对方，仅在确有需要时使用。message_id 来自 im.send_message；user_ids 为对方 open_id 数组。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。",
    RequiredScopes = ["im:message"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.MessageUrgentAppAsync))]
public interface IFeishuTenantImUrgentAppTool
{
    /// <summary>应用内加急。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id/urgent=true/invalid_user_id_list）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UrgentAppAsync(
        [ToolParameter("message_id", "待加急的消息 ID（om_ 开头；来自 im.send_message 的返回）", Required = true)] string message_id,
        [ToolParameter("user_ids", "加急目标用户 open_id 数组（如 [\"ou_xxx\"]；对方须在该消息的接收范围内，否则出现在 invalid_user_id_list）", Required = true)] string[] user_ids,
        [ToolParameter("user_id_type", "目标用户 ID 类型（可选：open_id / user_id / union_id，默认 open_id）")] string? user_id_type = null,
        [ToolParameter("dry_run", "仅预演不加急（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：im.urgent_sms（映射 <c>IFeishuTenantV1Message.MessageUrgentSMSAsync</c>）。</summary>
/// <remarks>短信加急 = 飞书客户端通知 + 手机短信双通道，打扰强度高于 im.urgent_app。</remarks>
[AgentTool("im.urgent_sms",
    Description = "把一条已发送消息以短信加急给指定用户（飞书客户端通知＋手机短信双通道）——比 im.urgent_app 打扰更强，仅在对方可能长时间离线的紧急场景使用。message_id 来自 im.send_message；user_ids 为对方 open_id 数组。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。",
    RequiredScopes = ["im:message"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.MessageUrgentSMSAsync))]
public interface IFeishuTenantImUrgentSmsTool
{
    /// <summary>短信加急。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id/urgent=true/invalid_user_id_list）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UrgentSmsAsync(
        [ToolParameter("message_id", "待加急的消息 ID（om_ 开头；来自 im.send_message 的返回）", Required = true)] string message_id,
        [ToolParameter("user_ids", "加急目标用户 open_id 数组（如 [\"ou_xxx\"]；对方须在该消息的接收范围内，否则出现在 invalid_user_id_list）", Required = true)] string[] user_ids,
        [ToolParameter("user_id_type", "目标用户 ID 类型（可选：open_id / user_id / union_id，默认 open_id）")] string? user_id_type = null,
        [ToolParameter("dry_run", "仅预演不加急（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── 表情回复写面（2 个） ───────────────────────────

/// <summary>工具接口：im.add_reaction（映射 <c>IFeishuTenantV1Message.AddMessageReactionsAsync</c>）。</summary>
/// <remarks>
/// <b>平台不去重</b>：表情回复没有幂等语义——同一 emoji_type 调用两次就是两条回复（界面上计数 +2），
/// 故<b>不提供</b> <c>idempotency_key</c>（提供了也是假的），改为在描述里写明"同一条消息同一表情只调用一次"。
/// 撤回错发的表情用 <c>im.delete_reaction</c>（需 reaction_id，即本工具的回执）。
/// </remarks>
[AgentTool("im.add_reaction",
    Description = "给一条消息加一个表情回复（比发一条'收到👌'消息更轻的确认方式）。emoji_type 是表情名（如 SMILE / THUMBSUP / OK / HEART / DONE，完整清单见飞书「表情文案说明」）。⚠️ 平台**不去重**：同一表情调用两次会叠成两条——一次只调一次，撤回用 im.delete_reaction。reaction_id 在本工具回执里。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。",
    RequiredScopes = ["im:message"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.AddMessageReactionsAsync))]
public interface IFeishuTenantImAddReactionTool
{
    /// <summary>添加表情回复。</summary>
    /// <returns>白名单投影后的 JSON 文本（reaction_id/emoji_type）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> AddReactionAsync(
        [ToolParameter("message_id", "目标消息 ID（om_ 开头）", Required = true)] string message_id,
        [ToolParameter("emoji_type", "表情类型名（如 SMILE / THUMBSUP / OK / HEART / DONE）", Required = true)] string emoji_type,
        [ToolParameter("dry_run", "仅预演不加表情（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：im.delete_reaction（映射 <c>IFeishuTenantV1Message.DeleteMessageReactionsAsync</c>）。</summary>
/// <remarks>
/// 删除的是**单条回复实例**（一个 reaction_id = 一个人的一次表情），不影响同一表情上的其他人。
/// reaction_id 来自 <c>im.add_reaction</c> 的回执或 <c>im.list_reactions</c> / <c>im.batch_query_reactions</c> 的明细。
/// 动词命中 profile 危险词表 ⇒ 风险级别 high-risk-write（须配 affordance 资产）。
/// </remarks>
[AgentTool("im.delete_reaction",
    Description = "撤回**一个**表情回复实例（按 reaction_id 精确删除，只影响这一次回复，不影响同一表情上其他人的回复）。reaction_id 来自 im.add_reaction 的回执，或 im.list_reactions / im.batch_query_reactions（with_details=true）的明细。⚠️ 撤回后无法恢复，只能重新 im.add_reaction。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。建议先 dry_run 预演确认。",
    RequiredScopes = ["im:message"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.DeleteMessageReactionsAsync))]
public interface IFeishuTenantImDeleteReactionTool
{
    /// <summary>删除表情回复。</summary>
    /// <returns>结构化文本（deleted=true + message_id/reaction_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteReactionAsync(
        [ToolParameter("message_id", "表情回复所在的消息 ID（om_ 开头）", Required = true)] string message_id,
        [ToolParameter("reaction_id", "要删除的表情回复 ID（单次回复的实例 ID，不是表情名）", Required = true)] string reaction_id,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path 与字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
