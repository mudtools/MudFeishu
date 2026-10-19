// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Tools.Tests.Tools;
using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// WP6 / AT-F09 域 guidance 资产守卫：域集合与工具面<b>同源不漂移</b>，
/// 且装配口径与工具启用口径一致（未启用不注入）。
/// </summary>
/// <remarks>
/// <para>
/// 数据源是生成器从 <c>Guidance/{domain}.md</c>（AdditionalFiles）发射的编译期字典
/// <see cref="FeishuToolGuidance.ByDomain"/>——域与该域工具同 repo 同 pass，
/// 因此这里锁的是"资产与工具面的对应关系"，而不是"两份手写清单是否一致"。
/// </para>
/// <para>
/// <b>为什么不做成生成器诊断</b>：新增零容忍诊断必须同批补 driver 负例（WP1 元守卫）；
/// 而"多写了一个 md 文件"属于无害的文档噪声，不值得引入一条构建失败级诊断。
/// </para>
/// </remarks>
public class GuidanceAssetContractGuards
{
    /// <summary>单域 guidance 的体积上限（字符）——防止单个资产把 2 KB 预算吃光。</summary>
    // R5 / F-9（评审点③）：原值 512 与"每域 ≤ 2 KB"冲突——结构化改造后最大域（im）达 1375 字符，
    // 512 会让守卫先于内容变红。故按方案把上限提到 2 KB，并留出余量给后续补充避坑。
    private const int MaxBlockLength = 2048;

    /// <summary>guidance 的域必须对应契约工具面中真实存在的域（防僵尸资产：域改名/删工具后 md 成了孤儿）。</summary>
    [Fact]
    public void GuidanceDomains_ShouldAllExistInToolContracts()
    {
        var contractDomains = FeishuToolContracts.AllNames
            .Select(static name => name.Substring(0, name.IndexOf(".", StringComparison.Ordinal)))
            .ToHashSet(StringComparer.Ordinal);

        var orphans = FeishuToolGuidance.ByDomain.Keys
            .Where(domain => !contractDomains.Contains(domain))
            .ToArray();

        orphans.Should().BeEmpty(
            "以下 guidance 域在工具契约中没有任何工具（僵尸资产——工具改名/删除后 md 未同批处理）：{0}",
            string.Join(", ", orphans));
    }

    /// <summary>
    /// <b>正向</b>覆盖：工具面的每个域都必须有 L1 guidance 资产。
    /// </summary>
    /// <remarks>
    /// 与上一条用例<b>方向相反、缺一不可</b>：上一条防"僵尸资产"（有 md 无工具），本条防"静默失导"
    /// （有工具无 md）——后者<b>不会触发任何构建诊断</b>：工具装配正常、描述正常、模型却不知道
    /// "这类问题该走哪个域、先调哪个工具、失败怎么退"，表现为"能用但用不对"，从日志上完全看不出来。
    /// </remarks>
    [Fact]
    public void EveryContractDomain_ShouldHaveAL1GuidanceAsset()
    {
        // 自证（防"过滤器写坏 ⇒ 零缺失假绿"）：域数与工具条目数一起锁基线。
        // 新增域时必须同批补 Guidance/{domain}.md 并更新这里的 31；工具增减时更新 319。
        var contractDomains = FeishuToolContracts.AllNames
            .Select(DomainOf)
            .ToHashSet(StringComparer.Ordinal);
        contractDomains.Should().HaveCount(
            31, "工具面当前 31 个域（含元工具域 feishu）——新增域须同批补 L1 资产并更新本基线");
        FeishuToolContracts.AllNames.Should().HaveCount(
            319, "工具条目基线（与 FeishuToolSchemas.golden.txt 同源）");

        var missing = FindDomainsWithoutGuidance(FeishuToolContracts.AllNames, FeishuToolGuidance.ByDomain);

        missing.Should().BeEmpty(
            "以下域有工具却无 L1 guidance 资产（有工具无 md）——模型拿得到工具却不知何时该用，"
            + "且这类缺口不触发任何构建诊断：{0}",
            string.Join(", ", missing));
    }

    /// <summary>
    /// 检查器自证：喂入"某域有工具但无资产"的合成输入，检查器<b>必须</b>报出该域。
    /// （没有这条，"过滤器永远返回空数组"这类坏法会让上面那条用例长期假绿。）
    /// </summary>
    [Fact]
    public void Scanner_ShouldFlagDomainWithoutGuidance_WhenToolHasNoAsset()
    {
        var guidance = new Dictionary<string, string>(StringComparer.Ordinal) { ["im"] = "im.md 的内容" };

        var missing = FindDomainsWithoutGuidance(["im.send_message", "brandnew.do_x"], guidance);

        missing.Should().Equal(["brandnew"], "有工具但无资产的域必须被点名；已有资产（im）不得误报");
    }

    /// <summary>从工具名取域前缀（`im.send_message` → `im`）。</summary>
    private static string DomainOf(string toolName)
        => toolName.Substring(0, toolName.IndexOf(".", StringComparison.Ordinal));

    /// <summary>正向覆盖检查器（纯函数，便于自证）：返回"有工具却无 L1 资产"的域（有序）。</summary>
    private static string[] FindDomainsWithoutGuidance(
        IEnumerable<string> toolNames,
        IReadOnlyDictionary<string, string> guidanceByDomain)
        => toolNames
            .Select(DomainOf)
            .Distinct(StringComparer.Ordinal)
            .Where(domain => !guidanceByDomain.ContainsKey(domain))
            .OrderBy(static domain => domain, StringComparer.Ordinal)
            .ToArray();

    /// <summary>每个域资产必须非空且足够精简（超过预算的域会在装配期被整体丢弃）。</summary>
    [Fact]
    public void GuidanceBlocks_ShouldBeNonEmptyAndConcise()
    {
        FeishuToolGuidance.ByDomain.Should().NotBeEmpty("至少应有已策展域的 guidance 资产");

        foreach (var (domain, content) in FeishuToolGuidance.ByDomain)
        {
            content.Should().NotBeNullOrWhiteSpace($"{domain}.md 不得为空（空资产会被静默跳过）");
            content.Length.Should().BeLessThanOrEqualTo(
                MaxBlockLength,
                $"{domain}.md 超过单域预算 {MaxBlockLength} 字符——多域同时启用时会挤掉其他域");
        }
    }

    /// <summary>单域启用时不得触发截断（最常见的用法必须完整注入）。</summary>
    [Fact]
    public void SingleEnabledDomain_ShouldNeverTruncate()
    {
        foreach (var (domain, content) in FeishuToolGuidance.ByDomain)
        {
            var result = GuidanceComposer.Compose(
                "宿主指令", [new GuidanceBlock(domain, content)], FeishuGuidanceBudget.MaxGuidanceLength);

            result.Truncated.Should().BeFalse($"{domain} 单域注入不应超限");
            result.Instructions.Should().Contain(content);
        }
    }

    /// <summary>装配口径 = 工具启用口径：未启用任何工具 → 无 guidance（不污染指令）。</summary>
    [Fact]
    public void GetGuidance_WithoutEnabledTools_ShouldBeEmpty()
    {
        using var provider = GuardProviderFactory.CreateProvider(_ => { });

        var blocks = provider.GetRequiredService<FeishuAgentToolSource>().GetGuidance(provider);

        blocks.Should().BeEmpty("默认不启用任何工具 → 不注入任何域资产（指令装配与 一致）");
    }

    /// <summary>启用某域工具 → 恰好得到该域 guidance（域去重、按名排序）。</summary>
    /// <remarks>
    /// BUG-4 起默认形态是 <c>routing</c>（一行摘要），故本用例断言"摘要形态"，并另有一条
    /// 用例（<see cref="GetGuidance_WithFullMode_ShouldReturnTheWholeL1Asset"/>）锁定 <c>full</c> 回退开关。
    /// </remarks>
    [Fact]
    public void GetGuidance_WithEnabledDomainTool_ShouldReturnThatDomainOnly()
    {
        using var provider = GuardProviderFactory.CreateProvider(options =>
            options.Tools = [FeishuToolNames.BitableListTables, FeishuToolNames.BitableListFields]);

        var blocks = provider.GetRequiredService<FeishuAgentToolSource>().GetGuidance(provider);

        blocks.Select(static b => b.Domain).Should().Equal(new[] { "bitable" }, "同域多工具只注入一份 guidance");
        blocks[0].Content.Should().Be(
            GuidanceRoutingSummary.Create("bitable", FeishuToolGuidance.ByDomain["bitable"]),
            "默认 routing 形态：内容必须与摘要推导函数的输出逐字节一致（同源同 pass）");
    }

    /// <summary>
    /// <b>BUG-4 回退开关</b>：<c>GuidanceMode=full</c> 时必须恢复"每域 L1 全文"的既有形态。
    /// </summary>
    /// <remarks>
    /// 没有这条，"routing 化"会变成不可回退的行为变更——宿主遇到模型侧观感退化时无退路。
    /// </remarks>
    [Fact]
    public void GetGuidance_WithFullMode_ShouldReturnTheWholeL1Asset()
    {
        using var provider = GuardProviderFactory.CreateProvider(options =>
        {
            options.Tools = [FeishuToolNames.BitableListTables];
            options.GuidanceMode = GuidanceModes.Full;
        });

        var blocks = provider.GetRequiredService<FeishuAgentToolSource>().GetGuidance(provider);

        blocks.Should().ContainSingle();
        blocks[0].Content.Should().Be(
            FeishuToolGuidance.ByDomain["bitable"],
            "full 形态必须逐字节等于 L1 资产全文（回退开关生效）");
    }

    /// <summary>
    /// <b>BUG-4 核心守卫</b>：<c>routing</c> 形态下<b>全域启用也不得触发截断</b>
    /// （<c>Truncated=false</c> 且丢弃清单为空），且每域摘要 ≤ 256 字符。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么是"零丢弃"而非"≤ 预算"</b>：超限策略是整域丢弃且失败方式为静默——
    /// 域扩容（专题一新增 5~10 域）后若仍走全文形态，25→32 域的总量会越过 32,768 而静默丢尾。
    /// 本用例把"任何域数下零丢弃"钉死为路由摘要形态的性质。
    /// </para>
    /// <para>
    /// 与 <see cref="GetGuidance_WithAllDomainsEnabled_ShouldNeverDropAnyDomain"/> 的分工：那条断言的是
    /// <b>装配结果</b>（走默认形态），本条额外锁<b>单域预算</b>与"摘要非空/含指引"这些
    /// 让摘要真正可用的性质（一条空摘要在装配层面同样是"零丢弃"）。
    /// </para>
    /// </remarks>
    [Fact]
    public void RoutingSummaries_ShouldBeConciseUsable_AndNeverTruncate()
    {
        using var provider = GuardProviderFactory.CreateProvider(EnableAllTools);

        var blocks = provider.GetRequiredService<FeishuAgentToolSource>().GetGuidance(provider);
        blocks.Should().HaveCount(FeishuToolGuidance.ByDomain.Count, "全域启用应覆盖全部 guidance 域");

        foreach (var block in blocks)
        {
            block.Content.Should().NotBeNullOrWhiteSpace($"{block.Domain} 的路由摘要不得为空");
            block.Content.Length.Should().BeLessThanOrEqualTo(
                GuidanceRoutingSummary.MaxSummaryLength,
                $"{block.Domain} 的路由摘要超过单域硬预算——28 域 × 256 ≈ 7 KB 是「恒定可控」的前提");
            block.Content.Should().Contain($"feishu-guidance://{block.Domain}",
                $"{block.Domain} 的摘要必须给出全文获取指引（否则模型只能猜）");
            block.Content.Should().Contain($"【{block.Domain}】",
                "摘要必须以域名开头（模型按域路由时的锚点）");
        }

        var result = GuidanceComposer.Compose("宿主指令", blocks, FeishuGuidanceBudget.MaxGuidanceLength);

        result.Truncated.Should().BeFalse(
            "routing 形态下全域装配不得触发任何截断（这就是 BUG-4 的修复判据）");
        result.OmittedDomains.Should().BeEmpty("routing 形态下丢弃清单必须为空");

        // 预算对照自证：摘要形态总量必须显著小于全文形态（否则"改造"是名义性的）。
        var fullTotal = FeishuToolGuidance.ByDomain.Values.Sum(static v => v.Length);
        var summaryTotal = blocks.Sum(static b => b.Content.Length);
        summaryTotal.Should().BeLessThan(
            fullTotal,
            "摘要形态的总量必须小于全文形态（否则本用例证不了 BUG-4 被修）");
    }

    /// <summary>
    /// 摘要里的工具名引用必须真实存在（防"摘要指向已改名工具"——摘要由全文推导，
    /// 但推导只取片段，工具名可能落在片段里，故仍需独立断言）。
    /// </summary>
    [Fact]
    public void RoutingSummaries_ToolReferences_ShouldAllExistInContracts()
    {
        var validNames = FeishuToolNames.All.ToHashSet(StringComparer.Ordinal);
        var violations = new List<string>();

        foreach (var (domain, content) in FeishuToolGuidance.ByDomain)
        {
            var summary = GuidanceRoutingSummary.Create(domain, content);
            foreach (var reference in GuidanceReferenceScanner.FindToolReferences(summary))
            {
                if (!validNames.Contains(reference))
                {
                    violations.Add($"{domain} 的路由摘要引用了不存在的工具 '{reference}'");
                }
            }
        }

        violations.Should().BeEmpty(
            "路由摘要中的工具引用必须全部存在（防推导片段截出半个工具名）：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>推导函数必须对"无 H1 标题"的输入响亮回落（绝不产出空摘要）。</summary>
    [Fact]
    public void RoutingSummary_ShouldFallBackToDomainName_WhenTitleMissing()
    {
        var summary = GuidanceRoutingSummary.Create("brandnew", "没有标题也没有路由小节的资产");

        summary.Should().StartWith("【brandnew】");
        summary.Should().Contain("feishu-guidance://brandnew");
        summary.Length.Should().BeLessThanOrEqualTo(GuidanceRoutingSummary.MaxSummaryLength);
    }

    /// <summary>
    /// 多域启用 → 按"该域已启用工具数降序 + 域名序"排序（<b>丢弃优先级</b>，R5 / B-12），
    /// 保证同一启用集合产出同一指令文本（确定性），且预算被调小时丢的是<b>长尾域</b>而非主力域。
    /// </summary>
    /// <remarks>
    /// 原实现按域名字母序：一旦超限就确定性丢掉字母序靠后的域——实测在原 2048 预算下
    /// 从第 7 个域<code>feishu</code> 起共 8 个域的 guidance 从未进入过 prompt。
    /// 本用例用"工具数不等"的组合锁定新语义：<c>bitable</c> 启用 2 个工具 → 排在最前。
    /// </remarks>
    [Fact]
    public void GetGuidance_WithMultipleDomains_ShouldOrderByEnabledToolCountThenName()
    {
        using var provider = GuardProviderFactory.CreateProvider(options =>
            options.Tools =
            [
                FeishuToolNames.WikiGetNode,
                FeishuToolNames.BitableListTables,
                FeishuToolNames.BitableListFields,
                FeishuToolNames.DocxGetRawContent,
            ]);

        var blocks = provider.GetRequiredService<FeishuAgentToolSource>().GetGuidance(provider);

        // bitable=2 工具 → 第 1；docx/wiki 各 1 工具 → 按域名序 docx 先于 wiki。
        blocks.Select(static b => b.Domain).Should().Equal(["bitable", "docx", "wiki"]);
    }

    /// <summary>
    /// <b>R5 / B-12 核心守卫</b>：<b>全域启用</b>时 guidance 拼装结果必须
    /// <b>零丢弃</b>（<c>Truncated == false</c> 且 <c>OmittedDomains</c> 为空）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么是"零丢弃"而非"≤ 预算"</b>：超限策略是<b>整域丢弃</b>且失败方式为静默
    /// （仅返回值上的 <c>Truncated</c>/<c>OmittedDomains</c> 可查，无日志无告警）。
    /// 只断言"≤ 预算"会让"丢弃 1 个域"继续合法——而那正是本项修复前的实际状态
    /// （实测 ≈3,650 字符 vs 原 2048 预算，8 个域从未进入 prompt）。
    /// </para>
    /// <para>
    /// 本用例同时是"未来有人在不知情下加长 guidance md"的防线：加长 → 本用例红。
    /// </para>
    /// </remarks>
    [Fact]
    public void GetGuidance_WithAllDomainsEnabled_ShouldNeverDropAnyDomain()
    {
        using var provider = GuardProviderFactory.CreateProvider(EnableAllTools);

        var source = provider.GetRequiredService<FeishuAgentToolSource>();
        var blocks = source.GetGuidance(provider);

        blocks.Should().HaveCount(
            FeishuToolGuidance.ByDomain.Count,
            "全域启用时每个 guidance 域都应被注入（启用集合覆盖全部工具 ⇒ 覆盖全部域）");

        var result = GuidanceComposer.Compose("宿主指令", blocks, FeishuGuidanceBudget.MaxGuidanceLength);

        result.Truncated.Should().BeFalse(
            "全域启用时不得因预算丢弃任何域——被丢弃域的避坑知识从未进入 prompt");
        result.OmittedDomains.Should().BeEmpty(
            "全域启用时的丢弃清单必须为空（预算 {0}，实测全域 {1} 字符）",
            FeishuGuidanceBudget.MaxGuidanceLength,
            blocks.Sum(static b => b.Content.Length));
    }

    /// <summary>
    /// 反向验证：把预算调回原值（2048）时，被丢弃的必须是<b>长尾域</b>（工具数少的），
    /// 而<b>不是</b> <c>im</c>/<c>task</c> 等主力域 —— 证明排序（丢弃优先级）真的生效。
    /// </summary>
    [Fact]
    public void GetGuidance_WhenBudgetIsUndersized_ShouldDropTailDomainsNotMajorDomains()
    {
        using var provider = GuardProviderFactory.CreateProvider(EnableAllTools);

        var blocks = provider.GetRequiredService<FeishuAgentToolSource>().GetGuidance(provider);

        // 复现"预算被调小"的场景：用一个必然超限的额度装配（不改生产常量——额度只计guidance 本体，
        // 故直接按字符预算语义构造：按排序后的顺序累加，超出即丢弃）。
        var budget = blocks.Sum(static b => b.Content.Length) - 1;
        var accepted = new List<GuidanceBlock>();
        var used = 0;
        foreach (var block in blocks)
        {
            var extra = (accepted.Count > 0 ? 2 : 0) + block.Content.Length;
            if (used + extra > budget)
            {
                continue;
            }

            accepted.Add(block);
            used += extra;
        }

        accepted.Select(static b => b.Domain).Should().Contain("im",
            "im 有 8 个启用工具（全域最多）⇒ 排序最前 ⇒ 任何预算下都不应先于长尾域被丢");
        accepted.Select(static b => b.Domain).Should().Contain("task");
        accepted.Count.Should().BeLessThan(blocks.Count,
            "预算小于全域总量 ⇒ 必然发生丢弃（否则本用例是假绿）");
    }

    /// <summary>
    /// 启用全部工具：读写分列（写类工具必须经 <c>WriteAllowList</c> 单独键控）+ 放行 <c>user</c> 身份
    /// （<c>approval.get_instance</c>/<c>task.list_my_tasks</c>/<c>mail.send_message</c> 是 user 身份工具，
    /// 不加入 <c>AllowedIdentities</c> 会在<b>装配期</b> fail-fast）。
    /// </summary>
    private static void EnableAllTools(FeishuAgentOptions options)
    {
        options.Tools = [.. FeishuToolNames.ReadonlyAll];
        options.WriteAllowList = [.. FeishuToolNames.WriteAll];
        options.AllowedIdentities = ["tenant", "user"];
    }

    // ────────── R7/WP1 守卫 A：工具名引用一致性 ──────────

    /// <summary>
    /// guidance 中所有 <c>域.工具</c> 形式引用必须存在于 <see cref="FeishuToolNames.All"/>——
    /// 防"引用了已删/改名的工具"（R7/WP1-T1-4，根因 R-G 修复）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>正则</b>：匹配 guidance 文本中出现的 <c>word.word</c> 模式（如 <c>sheets.update_range</c>），
    /// 然后断言其全集 ⊆ <see cref="FeishuToolNames.All"/>。
    /// </para>
    /// <para>
    /// <b>负例</b>：<see cref="Scanner_ShouldFlagNonExistentToolReference_WhenGuidanceMentionsRemovedTool"/>
    /// 用合成文本驱动同一扫描器并断言报红。
    /// </para>
    /// </remarks>
    [Fact]
    public void GuidanceToolReferences_ShouldAllExistInToolContracts()
    {
        var validNames = FeishuToolNames.All.ToHashSet(StringComparer.Ordinal);
        var violations = new List<string>();

        foreach (var (domain, content) in FeishuToolGuidance.ByDomain)
        {
            var referenced = GuidanceReferenceScanner.FindToolReferences(content);
            foreach (var name in referenced)
            {
                if (!validNames.Contains(name))
                {
                    violations.Add($"{domain}.md 引用了不存在的工具 '{name}'");
                }
            }
        }

        violations.Should().BeEmpty(
            "guidance 中引用的工具名必须全部存在于 FeishuToolNames.All（防引用已删/改名的工具）：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>负例：合成文本包含一个不存在的工具名 → 扫描器必须检出。</summary>
    [Fact]
    public void Scanner_ShouldFlagNonExistentToolReference_WhenGuidanceMentionsRemovedTool()
    {
        var validNames = new HashSet<string>(["bitable.list_tables", "bitable.add_record"], StringComparer.Ordinal);
        var referenced = GuidanceReferenceScanner.FindToolReferences("写入走 bitable.ghost_tool（已删除）");

        var ghosts = referenced.Where(n => !validNames.Contains(n)).ToArray();
        ghosts.Should().Contain("bitable.ghost_tool",
            "扫描器必须能定位 guidance 中的工具名引用——找不到说明正则坏了（假绿）");
    }

    // ────────── R7/WP1 守卫 B：反义短语一致性 ──────────

    /// <summary>
    /// 若某域在契约中存在写工具，则该域 guidance <b>不得</b>包含
    /// 「未开放」「不支持写」「仅只读」「未提供写」等否定短语——
    /// 防"自带资产否定自身能力"（R7/WP1-T1-5，根因 R-G 修复）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>负例</b>：<see cref="Scanner_ShouldFlagDenialPhrase_WhenDomainHasWriteTools"/>
    /// 用合成文本驱动同一扫描器并断言报红。
    /// </para>
    /// </remarks>
    [Fact]
    public void Guidance_ShouldNotDenyWriteCapability_WhenDomainHasWriteTools()
    {
        var writeDomains = FeishuToolContracts.ByToolName
            .Where(static kv => kv.Value.IsWrite)
            .Select(static kv => kv.Key.Substring(0, kv.Key.IndexOf('.', StringComparison.Ordinal)))
            .ToHashSet(StringComparer.Ordinal);

        var violations = new List<string>();

        foreach (var (domain, content) in FeishuToolGuidance.ByDomain)
        {
            if (!writeDomains.Contains(domain))
                continue;

            var denied = GuidanceReferenceScanner.FindWriteDenialPhrases(content);
            if (denied.Count > 0)
            {
                violations.Add($"{domain}.md 存在否定写能力的短语（但该域已有写工具）：{string.Join(", ", denied)}");
            }
        }

        violations.Should().BeEmpty(
            "已有写工具的域，其 guidance 不得否定写能力（会误导模型避免调用已启用的写工具）：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>负例：合成文本包含否定短语 → 扫描器必须检出。</summary>
    [Fact]
    public void Scanner_ShouldFlagDenialPhrase_WhenDomainHasWriteTools()
    {
        var denied = GuidanceReferenceScanner.FindWriteDenialPhrases("写类接口未开放");
        denied.Should().Contain("未开放",
            "扫描器必须能检出「未开放」否定短语——找不到说明正则坏了（假绿）");
    }
}

/// <summary>
/// guidance 文本扫描器（可被合成文本驱动 ⇒ 支持检查器自证）。
/// </summary>
internal static class GuidanceReferenceScanner
{
    private static readonly System.Text.RegularExpressions.Regex ToolReferencePattern =
        new(@"\b([a-z][a-z0-9]*)\.([a-z][a-z0-9_]*)\b",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly string[] DenialPhrases =
        ["未开放", "不支持写", "仅只读", "未提供写", "写类未开放", "不支持写入"];

    /// <summary>提取 guidance 文本中所有 <c>域.工具</c> 形式引用。</summary>
    public static IReadOnlyList<string> FindToolReferences(string text)
    {
        var names = new List<string>();
        foreach (System.Text.RegularExpressions.Match match in ToolReferencePattern.Matches(text))
        {
            names.Add(match.Value);
        }
        return names.Distinct(StringComparer.Ordinal).ToArray();
    }

    /// <summary>检测 guidance 文本中是否存在否定写能力的短语。</summary>
    public static IReadOnlyList<string> FindWriteDenialPhrases(string text)
    {
        return DenialPhrases.Where(p => text.Contains(p, StringComparison.Ordinal)).ToArray();
    }
}
