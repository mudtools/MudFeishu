// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// <summary>
// Phase 2 写类工具接口集（§3.3：发消息 / 新增记录 / 发起审批实例，全部 <c>IsWrite = true</c>）。
// </summary>
// <remarks>
// <para>
// 复用 Phase 1 <c>[FeishuTool]</c> 源生成器（<c>IsWrite</c> 经 Schema 扩展 <c>x-feishu.is_write</c>
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
[FeishuTool("im.send_message",
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
/// <b>R5 / F-6：消除模型手造 JSON。</b> <c>im.reply_message</c> 的 <c>content</c>
/// 要求模型自造卡片 JSON，官方明确禁止该做法。本工具改为接受<b>结构化描述</b>，
/// 由 <c>CardDsl</c> 在工具实现内编译为卡片 JSON —— <b>模型全程零 JSON 字符串</b>。
/// </para>
/// <para>
/// <b>发送路径（回答 A-12：不造第三套）</b>：复用 <c>im.send_message</c> 的
/// <c>SendMessageAsync</c> 单条发送路径。流式卡片通道 <c>CardStreamMessageChannel</c>
/// 用于"边生成边刷新"的流式更新（占位卡 + sequence 递增），与本工具的"发送一张静态卡片"
/// 是<b>不同场景</b>，故不复用。
/// </para>
/// <para>
/// <b>不做（D-6 / U-7）</b>：不做通用卡片 JSON 透传（会退回"模型自造 JSON"），
/// 也不做用户可写表达式 DSL（官方 27KB 规模）。
/// </para>
/// </remarks>
[FeishuTool("im.send_card",
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
