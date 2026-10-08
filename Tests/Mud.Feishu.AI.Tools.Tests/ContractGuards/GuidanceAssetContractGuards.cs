// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.FeishuTools.Tests.Tools;
using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

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
            var result = FeishuGuidanceComposer.Compose("宿主指令", [new FeishuGuidanceBlock(domain, content)]);

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

        blocks.Should().BeEmpty("默认不启用任何工具 → 不注入任何域资产（指令装配与 Phase 0 一致）");
    }

    /// <summary>启用某域工具 → 恰好得到该域 guidance（域去重、按名排序）。</summary>
    [Fact]
    public void GetGuidance_WithEnabledDomainTool_ShouldReturnThatDomainOnly()
    {
        using var provider = GuardProviderFactory.CreateProvider(options =>
            options.Tools = [FeishuToolNames.BitableListTables, FeishuToolNames.BitableListFields]);

        var blocks = provider.GetRequiredService<FeishuAgentToolSource>().GetGuidance(provider);

        blocks.Select(static b => b.Domain).Should().Equal(new[] { "bitable" }, "同域多工具只注入一份 guidance");
        blocks[0].Content.Should().Be(FeishuToolGuidance.ByDomain["bitable"]);
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

        var result = FeishuGuidanceComposer.Compose("宿主指令", blocks);

        result.Truncated.Should().BeFalse(
            "全域启用时不得因预算丢弃任何域——被丢弃域的避坑知识从未进入 prompt");
        result.OmittedDomains.Should().BeEmpty(
            "全域启用时的丢弃清单必须为空（预算 {0}，实测全域 {1} 字符）",
            FeishuGuidanceComposer.MaxGuidanceLength,
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
        var accepted = new List<FeishuGuidanceBlock>();
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
