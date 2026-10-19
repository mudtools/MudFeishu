// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── ChatGroup · 群管理 ───────────────────────────

// ⚠️ 命名口径（与 HelpDesk 同款的坑）：工具名前缀 chatgroup 在生成器里被规范化为
//    **Chatgroup**（只首字母大写）——生成的常量是 FeishuToolNames.Chatgroup*、
//    参数类是 Chatgroup*Args、DI 入口是 AddFeishuChatgroupToolsCore；
//    而 SDK 接口名仍是 IFeishuTenantV1ChatGroup*（SDK 侧按专有名词大写）。
//
// ⚠️ 域边界（能力去重结论）：
//    ① 既有 im 域已有 im.get_chat / im.search_chats / im.list_chat_members，
//       它们分别映射 ChatGroup 家族的同名读方法 ⇒ **本域不重复策展**；
//    ② 群公告：ChatGroup 家族（IFeishuV1ChatGroupAnnouncement）与 Docx 家族
//       （IFeishuV1DocxAnnouncement）是**两套接口、同一批后端路径**，而
//       docx.get_chat_announcement / docx.set_chat_announcement 已实现 ⇒ **公告归 docx，本域不再策展**。

/// <summary>
/// 工具接口：chatgroup.list_chats（映射 <c>IFeishuTenantV1ChatGroup.GetChatGroupPageListAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>它补上了一个真实的缺口</b>：多处既有描述写"chat_id 来自 <c>im.list_chats</c>"，
/// 但仓内从来没有这个工具（只有按关键词搜索的 <c>im.search_chats</c>）。
/// 本工具正是"列出机器人所在的群"这条路径，是那些描述里缺失的那个入口。
/// </para>
/// <para>
/// <b>与 <c>im.search_chats</c> 的分工</b>：本工具<b>无需关键词</b>（浏览"我/机器人在哪些群"），
/// 按关键词找特定群用 <c>im.search_chats</c>。
/// </para>
/// </remarks>
[AgentTool("chatgroup.list_chats",
    Description = "分页列出**机器人所在**的群（返回 chat_id/群名/群描述/群主 ID/是否外部群/群状态）——「我们有哪些群」「这个 chat_id 对应哪个群」的第一跳；按关键词找群请用 im.search_chats。只读，需 im:chat:readonly。",
    RequiredScopes = ["im:chat:readonly"],
    Source = nameof(IFeishuTenantV1ChatGroup) + "." + nameof(IFeishuTenantV1ChatGroup.GetChatGroupPageListAsync))]
public interface IFeishuTenantChatgroupListChatsTool
{
    /// <summary>列出机器人所在的群。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token：chat_id/name/description/owner_id 等），超长截断并标记 truncated。</returns>
    Task<string> ListChatsAsync(
        [ToolParameter("sort_type", "排序方式（可选：ByCreateTimeAsc=按创建时间升序 / ByActiveTimeDesc=按活跃时间降序；默认 ByCreateTimeAsc）")] string? sort_type = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：chatgroup.list_moderators（映射 <c>IFeishuTenantV1ChatGroup.GetChatGroupModeratorPageListByIdAsync</c>）。
/// </summary>
/// <remarks>
/// 返回群的<b>发言权限设置</b>与<b>可发言成员名单</b>——回答"这个群的发言权限是怎么配的、
/// 谁被允许发言"。它是 <c>im.get_chat</c> 的补充：后者给群主/管理员，本工具给"发言白名单"。
/// </remarks>
[AgentTool("chatgroup.list_moderators",
    Description = "查询群的发言权限设置与可发言成员名单（返回 moderation_setting + user_id 列表，分页）——回答「这个群谁能发言」。与 im.get_chat（群主/管理员）互补。只读，需 im:chat:readonly。",
    RequiredScopes = ["im:chat:readonly"],
    Source = nameof(IFeishuTenantV1ChatGroup) + "." + nameof(IFeishuTenantV1ChatGroup.GetChatGroupModeratorPageListByIdAsync))]
public interface IFeishuTenantChatgroupListModeratorsTool
{
    /// <summary>查询发言权限名单。</summary>
    /// <returns>白名单投影后的 JSON 文本（moderation_setting + items/has_more/page_token：user_id），超长截断并标记 truncated。</returns>
    Task<string> ListModeratorsAsync(
        [ToolParameter("chat_id", "群 ID（必填，形如 oc_xxx；来自 chatgroup.list_chats / im.search_chats）", Required = true)] string chat_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：chatgroup.is_in_chat（映射 <c>IFeishuTenantV1ChatGroupMember.GetMemberInChatByIdAsync</c>）。
/// </summary>
/// <remarks>
/// <b>发消息前的预检</b>：<c>im.send_message</c> 到一个机器人不在的群会失败。
/// 先用本工具确认"当前身份在不在这个群"，能把一次必然失败的调用省掉。
/// </remarks>
[AgentTool("chatgroup.is_in_chat",
    Description = "判断当前身份（机器人/用户）是否在指定群内（返回 is_in_chat）——向某个群发消息前的预检：不在群里就别发，先让用户把应用拉进群。只读，需 im:chat:readonly。",
    RequiredScopes = ["im:chat:readonly"],
    Source = nameof(IFeishuTenantV1ChatGroupMember) + "." + nameof(IFeishuTenantV1ChatGroupMember.GetMemberInChatByIdAsync))]
public interface IFeishuTenantChatgroupIsInChatTool
{
    /// <summary>判断当前身份是否在群内。</summary>
    /// <returns>白名单投影后的 JSON 文本（chat_id/is_in_chat + 下一步提示），超长截断并标记 truncated。</returns>
    Task<string> IsInChatAsync(
        [ToolParameter("chat_id", "群 ID（必填，形如 oc_xxx）", Required = true)] string chat_id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：chatgroup.get_menu（映射 <c>IFeishuTenantV1ChatGroupMenu.GetMenuByIdAsync</c>）。
/// </summary>
/// <remarks>
/// 群菜单是群会话里的**快捷入口**（点击跳到机器人或网页）。纯配置数据，不含任何个人信息——
/// 用于回答"这个群有哪些快捷入口"或核对菜单配置是否生效。
/// </remarks>
[AgentTool("chatgroup.get_menu",
    Description = "查询群的快捷菜单配置（一级/二级菜单的名称、类型与跳转链接）——回答「这个群的快捷入口有哪些」或核对菜单是否配置生效。只读，需 im:chat:readonly。",
    RequiredScopes = ["im:chat:readonly"],
    Source = nameof(IFeishuTenantV1ChatGroupMenu) + "." + nameof(IFeishuTenantV1ChatGroupMenu.GetMenuByIdAsync))]
public interface IFeishuTenantChatgroupGetMenuTool
{
    /// <summary>查询群菜单。</summary>
    /// <returns>白名单投影后的 JSON 文本（items：一级菜单 id/name/action_type/url + children 二级菜单），超长截断并标记 truncated。</returns>
    Task<string> GetMenuAsync(
        [ToolParameter("chat_id", "群 ID（必填，形如 oc_xxx）", Required = true)] string chat_id,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── 写面（群设置 / 成员 / 管理员 / 置顶 / 菜单删除） ───────────────────────────
//
// 三条纪律（与 TrustParty 写面同源）：
// ① **scope 全部标 ⚠️ 待核对**：本域 SDK XML 注释里**没有任何 scope 字面量**
//    （与 TrustParty 形成决定性对比），故按官方 API 文档取群管理写权限 im:chat 并标注待核对；
// ② **写工具默认不启用**：授权门禁（IToolExecutionAuthorizer）与 MaxToolRisk 在执行链，
//    执行器只负责「参数校验早于 dry_run」+ dry_run 短路 + 白名单回执；
// ③ **本域不碰群公告**（归 docx 域，见上方域边界）——建群/改群工具也不要写公告块。
//
// **不做的三个写能力**（理由写进 Guidance，不做静默缺口）：
//    · add_menu / update_menu：请求体是**嵌套树**（menu_tree → top_levels → children），
//      扁平参数无法表达，需先引入 JSON 参数形态并配套 dry_run 形状校验；
//    · get_chat_group_share_link_by_id：POST 但语义是**只读**（取群分享链接），
//      混进写面会让「POST ⇒ 写」的推断失效，故不混入。

/// <summary>
/// 工具接口：chatgroup.create_chat（映射 <c>IFeishuTenantV1ChatGroup.CreateChatGroupAsync</c>）。
/// </summary>
/// <remarks>
/// 群主默认是**当前应用/机器人身份**。群主转让（<c>owner_id</c>）不暴露——那是高危语义，
/// 应走独立工具而不是混在建群参数里。
/// </remarks>
[AgentTool("chatgroup.create_chat",
    Description = "创建一个群（返回 chat_id 与无效成员 ID 列表）——「拉个群沟通」的第一落点。写操作：默认不启用，需宿主授权（IToolExecutionAuthorizer），需 im:chat（⚠️ scope 名待控制台核对）。",
    RequiredScopes = ["im:chat"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1ChatGroup) + "." + nameof(IFeishuTenantV1ChatGroup.CreateChatGroupAsync))]
public interface IFeishuTenantChatgroupCreateChatTool
{
    /// <summary>创建群。</summary>
    /// <returns>白名单投影后的 JSON 回执（chat_id + invalid_user_id_list），超长截断并标记 truncated。</returns>
    Task<string> CreateChatAsync(
        [ToolParameter("name", "群名称（必填）", Required = true)] string name,
        [ToolParameter("description", "群描述（可选）")] string? description = null,
        [ToolParameter("user_id_list", "建群时邀请的成员 ID 列表（可选；类型由 user_id_type 指定，不填则只拉机器人自己）")] string[]? user_id_list = null,
        [ToolParameter("chat_type", "群类型（可选：private=私有群 / public=公开群；默认 private）")] string? chat_type = null,
        [ToolParameter("group_message_type", "群消息模式（可选：chat=对话消息 / thread=话题消息；默认 chat）")] string? group_message_type = null,
        [ToolParameter("edit_permission", "群编辑权限（可选：all_members=所有成员 / only_owner=仅群主与管理员；默认 all_members）")] string? edit_permission = null,
        [ToolParameter("membership_approval", "入群审批（可选：no_approval_required=无需审批 / approval_required=需要审批）")] string? membership_approval = null,
        [ToolParameter("user_id_type", "user_id_list 的 ID 类型（可选：open_id / user_id / union_id；默认 open_id）")] string? user_id_type = null,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回将要创建的群名与邀请人数，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：chatgroup.update_chat（映射 <c>UpdateChatGroupByIdAsync</c>）。
/// </summary>
/// <remarks>
/// <b>只改传入的字段</b>（未传的保持原值）——与互通规则的覆盖式语义不同，这里是**增量更新**。
/// </remarks>
[AgentTool("chatgroup.update_chat",
    Description = "更新群设置（群名/描述/编辑权限/拉人权限/分享名片权限/@所有人权限/消息模式）——未传的字段保持原值。写操作：默认不启用，需宿主授权，需 im:chat（⚠️ scope 名待控制台核对）。",
    RequiredScopes = ["im:chat"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1ChatGroup) + "." + nameof(IFeishuTenantV1ChatGroup.UpdateChatGroupByIdAsync))]
public interface IFeishuTenantChatgroupUpdateChatTool
{
    /// <summary>更新群设置。</summary>
    /// <returns>白名单投影后的 JSON 回执（chat_id + updated + 本次改动的字段数），超长截断并标记 truncated。</returns>
    Task<string> UpdateChatAsync(
        [ToolParameter("chat_id", "群 ID（必填，形如 oc_xxx）", Required = true)] string chat_id,
        [ToolParameter("name", "新群名称（可选；不传则保持原值）")] string? name = null,
        [ToolParameter("description", "新群描述（可选）")] string? description = null,
        [ToolParameter("edit_permission", "群编辑权限（可选：all_members / only_owner）")] string? edit_permission = null,
        [ToolParameter("add_member_permission", "拉人权限（可选：all_members / only_owner）")] string? add_member_permission = null,
        [ToolParameter("share_card_permission", "分享名片权限（可选：all_members / not_anyone）")] string? share_card_permission = null,
        [ToolParameter("at_all_permission", "@所有人权限（可选：all_members / only_owner）")] string? at_all_permission = null,
        [ToolParameter("group_message_type", "群消息模式（可选：chat / thread）")] string? group_message_type = null,
        [ToolParameter("user_id_type", "user_id_list 的 ID 类型（可选：open_id / user_id / union_id）")] string? user_id_type = null,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回 chat_id 与将要修改的字段数，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：chatgroup.delete_chat（映射 <c>DeleteChatGroupAsync</c>）。
/// </summary>
/// <remarks>
/// <b>high-risk-write</b>（命中危险词 <c>delete</c>）：解散群是**不可逆**操作——
/// 群内历史消息随群一起消失，且只能重建（新 chat_id）。
/// </remarks>
[AgentTool("chatgroup.delete_chat",
    Description = "解散一个群（⚠️ 不可逆：群内历史消息随群一起消失，且只能重建、会拿到新 chat_id——务必先 dry_run 预演并确认 chat_id 无误）。写操作：默认不启用，需宿主授权，需 im:chat（⚠️ scope 名待控制台核对）。",
    RequiredScopes = ["im:chat"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1ChatGroup) + "." + nameof(IFeishuTenantV1ChatGroup.DeleteChatGroupAsync))]
public interface IFeishuTenantChatgroupDeleteChatTool
{
    /// <summary>解散群。</summary>
    /// <returns>白名单投影后的 JSON 回执（chat_id + deleted），超长截断并标记 truncated。</returns>
    Task<string> DeleteChatAsync(
        [ToolParameter("chat_id", "要解散的群 ID（必填，形如 oc_xxx）", Required = true)] string chat_id,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回将要解散的群 ID，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：chatgroup.update_moderation（映射 <c>UpdateChatModerationAsync</c>）。
/// </summary>
/// <remarks>
/// ⚠️ 方法名里没有危险词（不命中 <c>permission</c>），故生成器**不会**把它判为 high-risk-write；
/// 但"改群发言权限"实质是高危语义（可一句话禁言全群），故在描述里自行标注后果。
/// </remarks>
[AgentTool("chatgroup.update_moderation",
    Description = "更新群的发言权限（⚠️ 高危语义：可一句话把全群改成「仅群主与管理员可发言」——moderation_setting=all_members / only_owner / moderator_list；选 moderator_list 时用 moderator_added_list / moderator_removed_list 增删可发言成员）。写操作：默认不启用，需宿主授权，需 im:chat（⚠️ scope 名待控制台核对）。建议先 dry_run 预演。",
    RequiredScopes = ["im:chat"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1ChatGroup) + "." + nameof(IFeishuTenantV1ChatGroup.UpdateChatModerationAsync))]
public interface IFeishuTenantChatgroupUpdateModerationTool
{
    /// <summary>更新发言权限。</summary>
    /// <returns>白名单投影后的 JSON 回执（chat_id + updated + 生效的发言模式），超长截断并标记 truncated。</returns>
    Task<string> UpdateModerationAsync(
        [ToolParameter("chat_id", "群 ID（必填，形如 oc_xxx）", Required = true)] string chat_id,
        [ToolParameter("moderation_setting", "发言模式（必填：all_members=所有成员可发言 / only_owner=仅群主与管理员 / moderator_list=指定成员可发言）", Required = true)] string moderation_setting,
        [ToolParameter("moderator_added_list", "指定发言成员时**新增**的成员 ID 列表（可选；类型由 member_id_type 指定）")] string[]? moderator_added_list = null,
        [ToolParameter("moderator_removed_list", "指定发言成员时**移除**的成员 ID 列表（可选）")] string[]? moderator_removed_list = null,
        [ToolParameter("member_id_type", "成员 ID 类型（可选：open_id / user_id / union_id；默认 open_id）")] string? member_id_type = null,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回 chat_id 与将要设置的发言模式，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：chatgroup.add_member（映射 <c>IFeishuTenantV1ChatGroupMember.AddMemberAsync</c>）。
/// </summary>
/// <remarks>
/// 入群失败的成员会进 <c>invalid_id_list</c>（用户不存在 / 已退群 / 类型不匹配）——
/// 回执必须把它带回，否则模型会以为"全员拉进去了"。
/// </remarks>
[AgentTool("chatgroup.add_member",
    Description = "把成员拉进群（返回无效成员 ID 列表）——「把人加进群沟通」。写操作：默认不启用，需宿主授权，需 im:chat（⚠️ scope 名待控制台核对）。",
    RequiredScopes = ["im:chat"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1ChatGroupMember) + "." + nameof(IFeishuTenantV1ChatGroupMember.AddMemberAsync))]
public interface IFeishuTenantChatgroupAddMemberTool
{
    /// <summary>拉成员入群。</summary>
    /// <returns>白名单投影后的 JSON 回执（chat_id + invalid_id_list），超长截断并标记 truncated。</returns>
    Task<string> AddMemberAsync(
        [ToolParameter("chat_id", "群 ID（必填，形如 oc_xxx）", Required = true)] string chat_id,
        [ToolParameter("id_list", "要拉入群的成员 ID 列表（必填；类型由 member_id_type 指定）", Required = true)] string[] id_list,
        [ToolParameter("member_id_type", "成员 ID 类型（可选：open_id / user_id / union_id；默认 open_id）")] string? member_id_type = null,
        [ToolParameter("succeed_type", "部分失败时的策略（可选：0=有无效 ID 就整体失败 / 1=拉入可用的并返回剩余 / 2=任一无效即失败；默认 0）")] int? succeed_type = null,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回 chat_id 与将要拉入的成员数，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：chatgroup.remove_member（映射 <c>RemoveMemberAsync</c>）。
/// </summary>
/// <remarks>
/// <b>high-risk-write</b>（命中危险词 <c>remove</c>）：把成员移出群——对方会立即失去群内可见性，
/// 且**不会**收到"你被移出了"的单独通知（只有群成员数变化）。
/// </remarks>
[AgentTool("chatgroup.remove_member",
    Description = "把成员移出群（⚠️ 对方立即失去群内可见性，且不会收到单独通知——务必先 dry_run 预演并确认 id_list 无误）。写操作：默认不启用，需宿主授权，需 im:chat（⚠️ scope 名待控制台核对）。",
    RequiredScopes = ["im:chat"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1ChatGroupMember) + "." + nameof(IFeishuTenantV1ChatGroupMember.RemoveMemberAsync))]
public interface IFeishuTenantChatgroupRemoveMemberTool
{
    /// <summary>移成员出群。</summary>
    /// <returns>白名单投影后的 JSON 回执（chat_id + invalid_id_list），超长截断并标记 truncated。</returns>
    Task<string> RemoveMemberAsync(
        [ToolParameter("chat_id", "群 ID（必填，形如 oc_xxx）", Required = true)] string chat_id,
        [ToolParameter("id_list", "要移出群的成员 ID 列表（必填；类型由 member_id_type 指定）", Required = true)] string[] id_list,
        [ToolParameter("member_id_type", "成员 ID 类型（可选：open_id / user_id / union_id；默认 open_id）")] string? member_id_type = null,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回 chat_id 与将要移出的成员数，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：chatgroup.me_join_chat（映射 <c>MeJoinChatGroupAsync</c>）。
/// </summary>
[AgentTool("chatgroup.me_join_chat",
    Description = "让当前应用/机器人身份加入一个群（公开群可直接加入；私密群需已在群内或被邀请）——用于「机器人进群后先自证在群内」的前置步骤。写操作：默认不启用，需宿主授权，需 im:chat（⚠️ scope 名待控制台核对）。",
    RequiredScopes = ["im:chat"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1ChatGroupMember) + "." + nameof(IFeishuTenantV1ChatGroupMember.MeJoinChatGroupAsync))]
public interface IFeishuTenantChatgroupMeJoinChatTool
{
    /// <summary>当前身份加入群。</summary>
    /// <returns>白名单投影后的 JSON 回执（chat_id + joined），超长截断并标记 truncated。</returns>
    Task<string> MeJoinChatAsync(
        [ToolParameter("chat_id", "要加入的群 ID（必填，形如 oc_xxx）", Required = true)] string chat_id,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回将要加入的群 ID，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：chatgroup.add_manager（映射 <c>AddManagersAsync</c>）。
/// </summary>
[AgentTool("chatgroup.add_manager",
    Description = "把成员设为群管理员（可多人；返回无效 ID 列表）——「给群加管理员」。写操作：默认不启用，需宿主授权，需 im:chat（⚠️ scope 名待控制台核对）。",
    RequiredScopes = ["im:chat"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1ChatGroupMember) + "." + nameof(IFeishuTenantV1ChatGroupMember.AddManagersAsync))]
public interface IFeishuTenantChatgroupAddManagerTool
{
    /// <summary>设置群管理员。</summary>
    /// <returns>白名单投影后的 JSON 回执（chat_id + invalid_manager_id_list），超长截断并标记 truncated。</returns>
    Task<string> AddManagerAsync(
        [ToolParameter("chat_id", "群 ID（必填，形如 oc_xxx）", Required = true)] string chat_id,
        [ToolParameter("manager_ids", "要设为管理员的成员 ID 列表（必填；类型由 member_id_type 指定）", Required = true)] string[] manager_ids,
        [ToolParameter("member_id_type", "成员 ID 类型（可选：open_id / user_id / union_id；默认 open_id）")] string? member_id_type = null,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回 chat_id 与将要设为管理员的成员数，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：chatgroup.delete_manager（映射 <c>DeleteManagersAsync</c>）。
/// </summary>
/// <remarks>
/// <b>high-risk-write</b>（命中危险词 <c>delete</c>）：撤销管理员身份——对方失去群管理能力
/// （改设置、踢人、置顶等），且**不会**收到通知。
/// </remarks>
[AgentTool("chatgroup.delete_manager",
    Description = "撤销成员的群管理员身份（⚠️ 对方立即失去群管理能力且不会收到通知——务必先 dry_run 预演并确认 manager_ids 无误）。写操作：默认不启用，需宿主授权，需 im:chat（⚠️ scope 名待控制台核对）。",
    RequiredScopes = ["im:chat"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1ChatGroupMember) + "." + nameof(IFeishuTenantV1ChatGroupMember.DeleteManagersAsync))]
public interface IFeishuTenantChatgroupDeleteManagerTool
{
    /// <summary>撤销群管理员。</summary>
    /// <returns>白名单投影后的 JSON 回执（chat_id + invalid_manager_id_list），超长截断并标记 truncated。</returns>
    Task<string> DeleteManagerAsync(
        [ToolParameter("chat_id", "群 ID（必填，形如 oc_xxx）", Required = true)] string chat_id,
        [ToolParameter("manager_ids", "要撤销管理员身份的成员 ID 列表（必填；类型由 member_id_type 指定）", Required = true)] string[] manager_ids,
        [ToolParameter("member_id_type", "成员 ID 类型（可选：open_id / user_id / union_id；默认 open_id）")] string? member_id_type = null,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回 chat_id 与将要撤销的成员数，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：chatgroup.put_top_notice（映射 <c>PutChatGroupTopNoticeAsync</c>）。
/// </summary>
/// <remarks>
/// <c>action_type=1</c> 必须配 <c>message_id</c>（置顶某条消息）；<c>action_type=2</c> 是置顶群公告
/// （**不需要** message_id，但要求群已开通公告——公告内容本身仍归 docx 域工具写）。
/// </remarks>
[AgentTool("chatgroup.put_top_notice",
    Description = "设置群置顶（action_type=1 置顶某条消息，需配 message_id；action_type=2 置顶群公告，不需 message_id）——「把重要内容钉在群顶部」。写操作：默认不启用，需宿主授权，需 im:chat（⚠️ scope 名待控制台核对）。",
    RequiredScopes = ["im:chat"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1ChatGroup) + "." + nameof(IFeishuTenantV1ChatGroup.PutChatGroupTopNoticeAsync))]
public interface IFeishuTenantChatgroupPutTopNoticeTool
{
    /// <summary>设置群置顶。</summary>
    /// <returns>白名单投影后的 JSON 回执（chat_id + action_type），超长截断并标记 truncated。</returns>
    Task<string> PutTopNoticeAsync(
        [ToolParameter("chat_id", "群 ID（必填，形如 oc_xxx）", Required = true)] string chat_id,
        [ToolParameter("action_type", "置顶类型（必填：1=消息类型，需配 message_id / 2=群公告类型）", Required = true)] string action_type,
        [ToolParameter("message_id", "要置顶的消息 ID（action_type=1 时必填）")] string? message_id = null,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回 chat_id 与置顶类型，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：chatgroup.delete_top_notice（映射 <c>DeleteChatGroupTopNoticeAsync</c>）。
/// </summary>
/// <remarks>
/// <b>high-risk-write</b>（命中危险词 <c>delete</c>）：取消群置顶——影响面是全群可见性
/// （所有人看到的"钉在顶部"消失），但不丢消息本身。
/// </remarks>
[AgentTool("chatgroup.delete_top_notice",
    Description = "取消群置顶（⚠️ 全群可见的置顶会消失，消息本身不丢失——务必先 dry_run 预演并确认 chat_id 无误）。写操作：默认不启用，需宿主授权，需 im:chat（⚠️ scope 名待控制台核对）。",
    RequiredScopes = ["im:chat"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1ChatGroup) + "." + nameof(IFeishuTenantV1ChatGroup.DeleteChatGroupTopNoticeAsync))]
public interface IFeishuTenantChatgroupDeleteTopNoticeTool
{
    /// <summary>取消群置顶。</summary>
    /// <returns>白名单投影后的 JSON 回执（chat_id + deleted），超长截断并标记 truncated。</returns>
    Task<string> DeleteTopNoticeAsync(
        [ToolParameter("chat_id", "群 ID（必填，形如 oc_xxx）", Required = true)] string chat_id,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回将要取消置顶的群 ID，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：chatgroup.delete_menu（映射 <c>IFeishuTenantV1ChatGroupMenu.DeleteMenuByIdAsync</c>）。
/// </summary>
/// <remarks>
/// 只删**一级菜单**（传 <c>chat_menu_top_level_ids</c>），其下二级菜单随父级一起消失。
/// 新增/修改菜单（<c>add_menu</c> / <c>update_menu</c>）的请求体是**嵌套树结构**，
/// 扁平参数无法表达，故不策展（见 Guidance）。
/// </remarks>
[AgentTool("chatgroup.delete_menu",
    Description = "删除群的快捷菜单一级入口（⚠️ 二级菜单会随父级一起消失——务必先 dry_run 预演并确认 chat_menu_top_level_ids 无误）。写操作：默认不启用，需宿主授权，需 im:chat（⚠️ scope 名待控制台核对）。",
    RequiredScopes = ["im:chat"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1ChatGroupMenu) + "." + nameof(IFeishuTenantV1ChatGroupMenu.DeleteMenuByIdAsync))]
public interface IFeishuTenantChatgroupDeleteMenuTool
{
    /// <summary>删除群菜单一级入口。</summary>
    /// <returns>白名单投影后的 JSON 回执（chat_id + deleted_menu_count），超长截断并标记 truncated。</returns>
    Task<string> DeleteMenuAsync(
        [ToolParameter("chat_id", "群 ID（必填，形如 oc_xxx）", Required = true)] string chat_id,
        [ToolParameter("chat_menu_top_level_ids", "要删除的一级菜单 ID 列表（必填，来自 chatgroup.get_menu）", Required = true)] string[] chat_menu_top_level_ids,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回 chat_id 与将要删除的菜单数，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
