// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Catalog;

/// <summary>
/// <b>BUG-3 修复</b>：能力分组的「模块别名表」——把 SDK 接口名派生出的能力分组键
/// （<c>{Domain}{Resource}</c>，如 <c>AilyDataKnowledge</c>）映射到<b>该能力实际被策展成的工具模块</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>要解决的失真</b>：<c>feishu.capability_lookup</c> 用「分组键的首个 Pascal 段」与「工具名的模块前缀」
/// 做大小写不敏感比对来判定 <c>module_curated</c>。当分组键的命名与实际策展模块<b>跨命名</b>时，
/// 该启发式会给出<b>错误结论</b>——例如 Aily 的能力已被策展为 <c>knowledge.search</c>，
/// 分组键却是 <c>AilyDataKnowledge</c>（首段 <c>Aily</c> ≠ 模块 <c>knowledge</c>），
/// 模型会被告知「该能力未策展 / 不可用」，而事实上可用。
/// </para>
/// <para>
/// <b>为什么是编译期常量表</b>：本表与 <c>feishu.capability_lookup</c> 的其余数据源同纪律
/// （无网络、无租户、无 DI 循环），且别名本身是<b>人工事实</b>（"这个分组的工具落在哪个模块"），
/// 无法从命名机械推导——故显式登记并由 <c>CapabilityAliasContractGuards</c> 锁定
/// （每个目标模块必须真实存在 ≥1 工具；别名键必须真实出现在能力目录里；冗余别名即红）。
/// </para>
/// <para>
/// <b>保守性</b>：只登记"确有跨命名"且<b>目标模块确实有工具</b>的条目。没有对应工具的域
/// （<c>Bot*</c>、<c>HelpDesk*</c>、<c>TrustParty*</c>、<c>ChatGroup*</c> 等尚未策展的域）
/// <b>不</b>登记——让它们如实显示为「未策展」正是本工具的价值；
/// 目录里已有的别名目标由 <c>CapabilityAliasContractGuards</c> 断言"必须存在 ≥1 工具"
/// （先登记后落地会让守卫报红，这正是"别名不得虚构"的机械保证）。
/// </para>
/// </remarks>
internal static class CapabilityModuleAliases
{
    /// <summary>
    /// 能力分组首段（Pascal，大小写不敏感）→ 实际承载其能力的工具模块前缀集合。
    /// </summary>
    /// <remarks>
    /// 键是 <c>LeadingPascalSegment</c> 的输出（不是完整分组键，也不是接口名）。
    /// 值为<b>模块集合</b>：一个分组的能力可能分散在多个模块（如群公告归 docx、群设置归 chatgroup）。
    /// </remarks>
    public static IReadOnlyDictionary<string, string[]> ByGroupSegment { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // AI 域的接口族以 "AIDocument"/"AITranslation" 等命名，首段是单个字母 "A"；
            // 其能力（机器翻译等）被策展为 ai.* 工具。
            ["A"] = ["ai"],

            // 注：曾有的 ["Aily"] = ["knowledge"] 已删除——B2 补全域落地 aily.* 三个只读工具后，
            // "Aily" 分组经大小写不敏感比对**天然命中** aily 模块（AliasKeys_ShouldNotBeRealToolModules
            // 会把这种"键本身即模块"的条目点名为冗余）。knowledge.search（RAG-A 面）的模块归属
            // 来自它自己的工具名，不依赖本条。

            // 应用卡片流式消息（AppCardMessageStream）落 im.send_card 的流式通道。
            ["App"] = ["im"],

            // 批量消息（BatchMessage）与卡片（Card/CardElements）同为消息面能力，落 im 模块。
            ["Batch"] = ["im"],
            ["Card"] = ["im"],

            // 主数据（MDMCountryRegion 等）首段是字母 "M"，模块名是 mdm。
            ["M"] = ["mdm"],

            // 服务台（HelpDeskAgent / HelpDeskTicket / HelpDeskFaq …）**首段只截到 "Help"**
            // （LeadingPascalSegment 在第二个大写字母前截断：H-e-l-p 连续小写，遇 D 停），
            // 而工具模块名是 helpdesk ⇒ 不登记别名则 module_curated 恒为 false，
            // capability_lookup 会把"已策展的服务台域"说成"尚未策展"。这是 HelpDesk 域与
            // Video/Aily 等"首段即完整单词"的域的差别，必须显式登记。
            ["Help"] = ["helpdesk"],

            // 群管理（ChatGroup / ChatGroupMember / ChatGroupMenu / ChatGroupAnnouncement）同理：
            // 首段只截到 "Chat"（C-h-a-t 后遇 G 停），而工具模块名是 chatgroup ⇒ 必须登记，
            // 否则 capability_lookup 会把已策展的群管理域说成"尚未策展"。
            ["Chat"] = ["chatgroup"],

            // 跨组织互通（TrustPartyCollaborationTenant → 首段 "Trust"；Directory* 三个接口 → 首段 "Directory"）
            // 都落 trustparty 模块。两条键都必须登记：本域的工具**同时**来自这两族接口，
            // 少登记一条 ⇒ capability_lookup 会把该族能力误报为"尚未策展"。
            ["Trust"] = ["trustparty"],
            ["Directory"] = ["trustparty"],

            // 单条消息（Message）落 im 模块。
            ["Message"] = ["im"],

            // 电子表格族的两种前缀（Spreadsheet* / Spreadsheets）同落 sheets 模块。
            ["Spreadsheet"] = ["sheets"],
            ["Spreadsheets"] = ["sheets"],

            // 通讯录组织架构族：部门与人员落 contact 模块。
            ["Departments"] = ["contact"],
            ["Employees"] = ["contact"],
            ["User"] = ["contact"],

            // 组织元数据族（职务/职级/职务族/工作城市）落 org 模块。
            ["Employee"] = ["org"],
            ["Job"] = ["org"],

            // 视频会议族（VideoConferencing*）落 vc 模块。
            // 注：首段是 "Video"（不是 "VideoConferencing"）——首个 Pascal 段在第二个大写字母前截断，
            // 故方案原文写的 `VideoConferencing→vc` 在本仓的实际键形态是 `Video`。
            ["Video"] = ["vc"],
        };

    /// <summary>
    /// 把能力分组首段解析为"应当在其中查找已策展工具的模块集合"。
    /// </summary>
    /// <param name="groupSegment">分组键的首个 Pascal 段（见 <c>CapabilityLookupTools.LeadingPascalSegment</c>）。</param>
    /// <returns>模块前缀集合；无别名时返回仅含 <paramref name="groupSegment"/> 的单元素数组。</returns>
    /// <remarks>
    /// 返回<b>原段</b>作为兜底是有意的：没有别名的分组（如 <c>OkrObjective</c> 首段 <c>Okr</c>）
    /// 仍按"首段即模块"的既有启发式判定——本表只修正确有跨命名的那些。
    /// </remarks>
    public static string[] ResolveModules(string groupSegment)
        => ByGroupSegment.TryGetValue(groupSegment, out var modules) ? modules : [groupSegment];
}
