// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── ChatGroup · 群管理（B1 第四个补域） ───────────────────────────

// ⚠️ 命名口径（与 HelpDesk 同款的坑）：工具名前缀 chatgroup 在生成器里被规范化为
//    **Chatgroup**（只首字母大写）——生成的常量是 FeishuToolNames.Chatgroup*、
//    参数类是 Chatgroup*Args、DI 入口是 AddFeishuChatgroupToolsCore；
//    而 SDK 接口名仍是 IFeishuTenantV1ChatGroup*（SDK 侧按专有名词大写）。
//
// ⚠️ 域边界（方案 §2.4 要求的「能力去重评审」结论，证据见 §B.7）：
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
[FeishuTool("chatgroup.list_chats",
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
[FeishuTool("chatgroup.list_moderators",
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
[FeishuTool("chatgroup.is_in_chat",
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
[FeishuTool("chatgroup.get_menu",
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
