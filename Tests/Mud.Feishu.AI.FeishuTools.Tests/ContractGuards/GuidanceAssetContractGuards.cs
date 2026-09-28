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
    private const int MaxBlockLength = 512;

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

    /// <summary>多域启用 → 按域名排序，保证同一启用集合产出同一指令文本（确定性）。</summary>
    [Fact]
    public void GetGuidance_WithMultipleDomains_ShouldBeOrderedByDomainName()
    {
        using var provider = GuardProviderFactory.CreateProvider(options =>
            options.Tools = [FeishuToolNames.WikiGetNode, FeishuToolNames.BitableListTables, FeishuToolNames.DocxGetRawContent]);

        var blocks = provider.GetRequiredService<FeishuAgentToolSource>().GetGuidance(provider);

        blocks.Select(static b => b.Domain).Should().Equal(["bitable", "docx", "wiki"]);
    }
}
