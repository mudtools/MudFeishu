// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Curation;

// <summary>
// 通讯录域工具接口（P0：解决"模型无法把姓名/邮箱/手机号变成 ID"的必然失败场景）。
// </summary>
// <remarks>
// <para>
// <b>为什么这是 P0</b>：飞书全部 API 用 <c>open_id</c>/<c>user_id</c> 指代人，
// 而用户对模型说的是"发给张三"。在此之前没有任何工具能把"张三"变成 ID，
// 于是 <c>im.send_message</c> 这类写工具的入参根本凑不出来——不是能力缺失，是<b>链路缺首环</b>。
// </para>
// <para>
// 三步链（对齐官方 CLI 的 <c>lark-contact</c> 语义）：
// <c>contact.resolve_user</c>（邮箱/手机号 → ID）或 <c>contact.search_user</c>（姓名/关键字 → ID），
// 再 <c>contact.batch_get</c> 批量取详情；单点详情用 <c>contact.get_user</c>。
// </para>
// <para>
// <b>AT-F11 / R3 评审 C-7 的链路闭环</b>：<c>contact.search_user</c> 的返回值<b>必须</b>含 <c>open_id</c>，
// 因为"发给张三"的下一跳是 <c>im.send_message(receive_id_type="open_id", receive_id=&lt;open_id&gt;)</c>。
// 该两跳链路由 <c>DomainMappingTests.SearchUserThenSendMessage_*</c> 端到端锁定。
// </para>
// </remarks>

/// <summary>工具接口：contact.resolve_user（映射 <c>IFeishuTenantV3User.GetBatchUsersAsync</c>）。</summary>
[FeishuTool("contact.resolve_user",
    Description = "把邮箱或手机号批量换成用户 ID（open_id/user_id/union_id）——发送消息、加群、指派任务前的第一步。emails 与 mobiles 至少填一个，合计不超过 50 项。已知姓名/昵称而非邮箱手机号时请改用 contact.search_user。只读，需 contact:user.base:readonly。",
    RequiredScopes = ["contact:user.base:readonly"],
    Source = nameof(IFeishuTenantV3User) + "." + nameof(IFeishuTenantV3User.GetBatchUsersAsync),

    // R5 / B-6：emails 与 mobiles 至少提供一个——跨参数约束，required 表达不了。
    AnyOf = ["emails|mobiles"])]
public interface IFeishuTenantContactResolveUserTool
{
    /// <summary>按邮箱/手机号批量解析用户 ID。</summary>
    /// <returns>白名单投影后的 JSON 文本（items：email/mobile/user_id/open_id/status），超长截断并标记 truncated。</returns>
    Task<string> ResolveUsersAsync(
        [ToolParameter("emails", "邮箱数组（可选，与 mobiles 至少填一个；如 [\"zhangsan@example.com\"]）")] string[]? emails = null,
        [ToolParameter("mobiles", "手机号数组（可选，与 emails 至少填一个；如 [\"13800138000\"]）")] string[]? mobiles = null,
        [ToolParameter("include_resigned", "是否包含已离职成员（可选，默认 false）")] bool? include_resigned = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：contact.search_user（映射 <c>IFeishuTenantV3User.GetUsersByKeywordAsync</c>）。
/// </summary>
/// <remarks>
/// <b>AT-F11（P0，R3 新增）</b>：这是"发给张三"整条写链路的<b>首环</b>——用户口述的是姓名，
/// 而飞书全部写接口要的是 ID。SDK 方法早已存在（<c>GET /open-apis/search/v1/user</c>），
/// 此前只是没有把它接成工具，故本项成本极低而收益直接落在写链路上。
/// </remarks>
[FeishuTool("contact.search_user",
    Description = "按姓名/关键字搜索用户，返回 open_id/user_id/姓名/所属部门——用户说\"发给张三\"时的第一步（拿到 open_id 后交给 im.send_message，receive_id_type 传 open_id）。只读，需 contact:user.base:readonly。",
    RequiredScopes = ["contact:user.base:readonly"],
    Source = nameof(IFeishuTenantV3User) + "." + nameof(IFeishuTenantV3User.GetUsersByKeywordAsync))]
public interface IFeishuTenantContactSearchUserTool
{
    /// <summary>按关键字（姓名/昵称）搜索用户。</summary>
    /// <returns>白名单投影后的 JSON 文本（items：open_id/user_id/name/department_ids），超长截断并标记 truncated。</returns>
    Task<string> SearchUsersAsync(
        [ToolParameter("query", "搜索关键词（姓名/昵称，如 \"张三\"）", Required = true)] string query,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：contact.get_user（映射 <c>IFeishuTenantV3User.GetUserInfoByIdAsync</c>）。</summary>
[FeishuTool("contact.get_user",
    Description = "按 user_id 或 open_id 读取单个用户的通讯录详情（姓名/邮箱/手机号/部门/上级/状态）。只读，需 contact:user.base:readonly。",
    RequiredScopes = ["contact:user.base:readonly"],
    Source = nameof(IFeishuTenantV3User) + "." + nameof(IFeishuTenantV3User.GetUserInfoByIdAsync))]
public interface IFeishuTenantContactGetUserTool
{
    /// <summary>读取单个用户详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（user 对象），超长截断并标记 truncated。</returns>
    Task<string> GetUserAsync(
        [ToolParameter("user_id", "用户 ID（类型须与 user_id_type 一致，形如 ou_xxx / on_xxx）", Required = true)] string user_id,
        [ToolParameter("user_id_type", "ID 类型（可选：open_id / union_id / user_id，默认 open_id）")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：contact.batch_get（映射 <c>IFeishuTenantV3User.GetUserByIdsAsync</c>）。</summary>
[FeishuTool("contact.batch_get",
    Description = "按 ID 批量读取用户通讯录详情（最多 50 个）——contact.resolve_user 拿到 ID 后的详情补全链。只读，需 contact:user.base:readonly。",
    RequiredScopes = ["contact:user.base:readonly"],
    Source = nameof(IFeishuTenantV3User) + "." + nameof(IFeishuTenantV3User.GetUserByIdsAsync))]
public interface IFeishuTenantContactBatchGetTool
{
    /// <summary>批量读取用户详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（items：user 对象数组），超长截断并标记 truncated。</returns>
    Task<string> BatchGetUsersAsync(
        [ToolParameter("user_ids", "用户 ID 数组（最多 50 个；类型须与 user_id_type 一致）", Required = true)] string[] user_ids,
        [ToolParameter("user_id_type", "ID 类型（可选：open_id / union_id / user_id，默认 open_id）")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}
