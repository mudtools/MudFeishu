// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>BUG-3 守卫</b>：能力分组「模块别名表」（<see cref="CapabilityModuleAliases"/>）的
/// 正确性与必要性锁定。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要机械门禁</b>：别名表是"跨命名分组"的人工事实，它有三个静默失败形态：
/// <list type="number">
/// <item><b>僵尸别名</b>——别名键在能力目录里已不存在（SDK 接口改名后表未同步），
/// 该条永远不会命中，而派生出的"未策展"误报会原样回归；</item>
/// <item><b>悬空目标</b>——别名指向的模块没有任何工具（拼错模块名），
/// 结果是"别名生效了但指向空集"，误报依旧；</item>
/// <item><b>冗余别名</b>——别名键本身就是一个真实模块名（此时不写别名也会命中），
/// 会让读者以为"这里有跨命名"，掩盖表的真实语义。</item>
/// </list>
/// </para>
/// <para>
/// 三条判据都无法靠编译发现（<c>feishu.capability_lookup</c> 只回 JSON），故必须由本守卫锁死。
/// </para>
/// </remarks>
public class CapabilityAliasContractGuards
{
    /// <summary>别名表必须非空（空表 = BUG-3 回归，跨命名分组会重新被误报"未策展"）。</summary>
    [Fact]
    public void AliasTable_ShouldNotBeEmpty()
    {
        CapabilityModuleAliases.ByGroupSegment.Should().NotBeEmpty(
            "别名表为空说明 BUG-3 的修复被回退——Aily/ChatGroup/Spreadsheets 等跨命名分组会被误报为「未策展」");
    }

    /// <summary>
    /// 判据 ①：每个别名目标域必须存在 ≥1 工具（悬空目标 = 别名生效但指向空集，误报照旧）。
    /// </summary>
    [Fact]
    public void AliasTargets_ShouldEachOwnAtLeastOneTool()
    {
        var modulesWithTools = FeishuToolNames.All
            .Select(ModuleOf)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        modulesWithTools.Should().NotBeEmpty("工具模块面不得为空（否则本守卫恒绿）");

        var dangling = CapabilityModuleAliases.ByGroupSegment
            .SelectMany(pair => pair.Value.Select(module => (Segment: pair.Key, Module: module)))
            .Where(pair => !modulesWithTools.Contains(pair.Module))
            .Select(pair => $"'{pair.Segment}' → '{pair.Module}'")
            .ToArray();

        dangling.Should().BeEmpty(
            "以下别名指向的模块没有任何已策展工具（悬空目标）——这会让 module_curated 依旧误报「未策展」：{0}",
            string.Join(", ", dangling));
    }

    /// <summary>
    /// 判据 ②：别名键必须真实出现在能力目录中（僵尸别名 = SDK 改名后表未同步）。
    /// </summary>
    [Fact]
    public void AliasKeys_ShouldExistInTheCapabilityCatalog()
    {
        var segments = FeishuToolCapabilityCatalog.MethodsByDomain.Keys
            .Select(LeadingPascalSegment)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        segments.Should().NotBeEmpty("能力目录不得为空（否则本守卫恒绿）");

        var zombies = CapabilityModuleAliases.ByGroupSegment.Keys
            .Where(segment => !segments.Contains(segment))
            .ToArray();

        zombies.Should().BeEmpty(
            "以下别名键在能力目录里不存在任何分组（僵尸别名）——SDK 接口改名后必须同批同步本表，"
            + "否则「别名看起来还在，实际一条都命不中」：{0}",
            string.Join(", ", zombies));
    }

    /// <summary>
    /// 判据 ③：别名键<b>不得</b>本身就是一个真实工具模块（冗余别名会掩盖表的真实语义）。
    /// </summary>
    [Fact]
    public void AliasKeys_ShouldNotBeRealToolModules()
    {
        var modulesWithTools = FeishuToolNames.All
            .Select(ModuleOf)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var redundant = CapabilityModuleAliases.ByGroupSegment.Keys
            .Where(modulesWithTools.Contains)
            .ToArray();

        redundant.Should().BeEmpty(
            "以下别名键本身就是真实工具模块（冗余条目）：不写别名也会命中，留着会让读者以为这里存在跨命名：{0}",
            string.Join(", ", redundant));
    }

    /// <summary>
    /// 方案 §1 BUG-3 点名的跨命名分组必须被覆盖（防"表被清空成只剩一两条"的静默退化）。
    /// </summary>
    /// <remarks>
    /// 逐条对应方案原文：<c>Aily→knowledge</c>、<c>Messages→im</c>、<c>VideoConferencing→vc</c>、
    /// <c>Spreadsheets→sheets</c>、<c>Organization→contact/org</c>、<c>ChatGroup→im</c>。
    /// 其中三条按<b>代码事实</b>落地为更精确的形态（首段 = 首个 Pascal 段，在第二个大写字母前截断）：
    /// <list type="bullet">
    /// <item><c>Messages→im</c>：能力目录里的分组首段是 <c>Message</c>（单数），故键为 <c>Message</c>；</item>
    /// <item><c>VideoConferencing→vc</c>：首段实际是 <c>Video</c>（<c>VideoConferencingBot</c> → <c>Video</c>）；</item>
    /// <item><c>ChatGroup→im</c>：分组首段是 <c>Chat</c>，且群生命周期工具落在 <c>chatgroup</c> 模块、
    /// 群公告按去重决策落 <c>docx</c>——故目标为 <c>[chatgroup, docx]</c>（比单一 <c>im</c> 更精确）。</item>
    /// </list>
    /// </remarks>
    /// <remarks>
    /// <b>Aily→knowledge 已于 B2 补全域后删除</b>：aily.* 三个只读工具落地后，"Aily" 分组经
    /// 大小写不敏感比对天然命中 aily 模块，该条目被 <c>AliasKeys_ShouldNotBeRealToolModules</c>
    /// 点名为冗余（"键本身即模块"）。这是别名表的生命周期正例：<b>跨命名是暂态，域落地后即转正</b>。
    /// </remarks>
    [Fact]
    public void PlanMandatedAliases_ShouldBePresent()
    {
        var table = CapabilityModuleAliases.ByGroupSegment;

        // Aily 别名已按冗余纪律删除（见类级 remarks）——这里反向断言它**不再存在**，
        // 防止有人照着旧方案原文把它加回来（加了就会被 AliasKeys_ShouldNotBeRealToolModules 拦）。
        table.Should().NotContainKey("Aily", "aily.* 工具落地后该分组天然命中 aily 模块，别名已按冗余纪律删除");

        table.Should().ContainKey("Message");
        table["Message"].Should().Contain("im", "消息面能力落 im 模块");

        table.Should().ContainKey("Video");
        table["Video"].Should().Contain("vc", "VideoConferencing* 分组的能力落 vc 模块");

        table.Should().ContainKey("Spreadsheet");
        table["Spreadsheet"].Should().Contain("sheets");
        table.Should().ContainKey("Spreadsheets");
        table["Spreadsheets"].Should().Contain("sheets");

        // Organization 域：部门/人员落 contact，职务/职级/族/城市落 org。
        table["Departments"].Should().Contain("contact");
        table["User"].Should().Contain("contact");
        table["Job"].Should().Contain("org");
        table["Employee"].Should().Contain("org");

        // 专题一 B1 第四域（chatgroup）落地后，按本用例原先的备注把 Chat 别名断言**改回正向**：
        // 群管理（ChatGroup*）的分组首段是 "Chat"，工具模块名是 chatgroup ⇒ 必须登记别名，
        // 否则 capability_lookup 会把已策展的群管理域说成"尚未策展"。
        // 注：方案原文写的是 `ChatGroup→im`（因为当时只有 im 域下三个群读工具）；
        // 本仓现状是**独立 chatgroup 模块**（群列表/发言名单/在群预检/群菜单），
        // 而群详情/搜群/成员列表仍在 im 域 ⇒ 别名指向 chatgroup，im 侧的既有映射不受影响。
        table.Should().ContainKey("Chat");
        table["Chat"].Should().Contain("chatgroup", "ChatGroup* 分组的能力落 chatgroup 模块");
    }

    /// <summary>
    /// 行为金丝雀：解析函数必须真的把跨命名分组映射到目标模块，且未登记段原样返回。
    /// </summary>
    /// <remarks>
    /// 这条把"表存在"与"表被用上"分开断言：前者可被"表建好了但执行器没接"绕过。
    /// </remarks>
    [Fact]
    public void ResolveModules_ShouldMapCrossNamedSegments_AndPassThroughOthers()
    {
        // Aily：别名删除后原样返回，靠大小写不敏感比对天然命中 aily 模块（B2 补全域后的事实）。
        CapabilityModuleAliases.ResolveModules("Aily").Should().Equal(["Aily"]);
        CapabilityModuleAliases.ResolveModules("Video").Should().Equal(["vc"]);

        // 未登记段原样返回（既有启发式不变）——否则会改变已正确的分组判定。
        CapabilityModuleAliases.ResolveModules("Okr").Should().Equal(["Okr"]);
        CapabilityModuleAliases.ResolveModules("Bitable").Should().Equal(["Bitable"]);

        // 大小写不敏感（分组首段是 Pascal，工具模块是小写，比对口径必须一致）。
        CapabilityModuleAliases.ResolveModules("aily").Should().Equal(["aily"]);
    }

    /// <summary>
    /// 执行器必须真的消费别名表（源码结构断言）：<c>CapabilityLookupTools</c> 里不得再
    /// 直接用首段做 <c>module_curated</c> 判定。
    /// </summary>
    [Fact]
    public void CapabilityLookupExecutor_ShouldConsumeTheAliasTable()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Internal", "CapabilityLookupTools.cs"));

        source.Should().Contain("CapabilityModuleAliases.ResolveModules(",
            "执行器必须经别名表解析分组首段——直接比对首段就是 BUG-3 的原始形态");
        source.Should().NotContain("curatedByModule.ContainsKey(leadingSegment)",
            "直接的\"首段 vs 模块\"比对必须已被别名解析取代（否则 Aily 等跨命名分组会被误报未策展）");
    }

    private static string ModuleOf(string toolName)
    {
        var index = toolName.IndexOf(".", StringComparison.Ordinal);
        return index > 0 ? toolName.Substring(0, index) : toolName;
    }

    /// <summary>取分组键的首个 Pascal 段（与生产实现同口径）。</summary>
    private static string LeadingPascalSegment(string groupName)
    {
        var end = 1;
        while (end < groupName.Length && char.IsLower(groupName[end]))
        {
            end++;
        }

        return groupName.Substring(0, end);
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}
