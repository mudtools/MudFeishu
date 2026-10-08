// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// <see cref="ScenarioBook"/> 的注册与查询：6 部剧本全部可按名或别名解析。
/// </summary>
public class ScenarioBookTests
{
    private static ScenarioBook CreateBook(DocAgentSettings? settings = null)
        => ScenarioBook.CreateDefault(settings ?? TestDoubles.CreateSettings());

    /// <summary>默认装配必须恰好是 6 部剧本，且名称、别名唯一。</summary>
    [Fact]
    public void CreateDefault_Should_RegisterSixScenarios()
    {
        var book = CreateBook();

        book.All.Should().HaveCount(6);
        book.All.Select(static s => s.Name).Should().OnlyHaveUniqueItems();
        book.All.SelectMany(static s => s.Aliases).Should().OnlyHaveUniqueItems();
        book.All.Should().OnlyContain(static s => !string.IsNullOrWhiteSpace(s.Prompt));
        book.All.Should().OnlyContain(static s => !string.IsNullOrWhiteSpace(s.Description));
    }

    /// <summary>名称与别名都必须能解析到同一部剧本（<c>/scenario kb</c> 与 <c>/scenario kb-digest</c> 等价）。</summary>
    /// <param name="name">剧本名。</param>
    /// <param name="alias">别名。</param>
    [Theory]
    [InlineData("kb-digest", "kb")]
    [InlineData("doc-authoring", "author")]
    [InlineData("md-import", "md")]
    [InlineData("sheet-sync", "sheet")]
    [InlineData("safe-delete", "del")]
    [InlineData("self-heal", "heal")]
    public void Find_Should_Resolve_ByNameOrAlias(string name, string alias)
    {
        var book = CreateBook();

        book.Find(name).Should().NotBeNull();
        book.Find(alias).Should().NotBeNull();
        book.Find(alias)!.Name.Should().Be(name, "别名必须解析到同一部剧本");
        book.Find(name)!.Aliases.Should().Contain(alias);
    }

    /// <summary>大小写不敏感（终端输入容错）。</summary>
    [Fact]
    public void Find_Should_BeCaseInsensitive()
    {
        var book = CreateBook();

        book.Find("KB-DIGEST").Should().NotBeNull();
        book.Find("Heal").Should().NotBeNull();
    }

    /// <summary>未命中与空输入返回 <see langword="null"/>（调用方据此提示清单）。</summary>
    [Fact]
    public void Find_Should_ReturnNull_When_Unknown()
    {
        var book = CreateBook();

        book.Find("not-a-scenario").Should().BeNull();
        book.Find(null).Should().BeNull();
        book.Find("   ").Should().BeNull();
    }

    /// <summary>配置了知识库空间 ID 时，S1 引导语必须带上它（否则模型要多花一次调用去列空间）。</summary>
    [Fact]
    public void KnowledgeBaseDigest_Should_IncludeWikiSpaceId_WhenConfigured()
    {
        var settings = TestDoubles.CreateSettings() with { WikiSpaceId = "wikcn_space_123" };
        var book = CreateBook(settings);

        book.Find("kb")!.Prompt.Should().Contain("wikcn_space_123");
    }

    /// <summary>
    /// 未配置知识库空间 ID 时必须产出可读告警（而不是静默少一次调用）；
    /// 配置后告警消失。告警由宿主在启动横幅<b>之后</b>输出（时序正确）。
    /// </summary>
    [Fact]
    public void Warnings_ShouldMentionMissingWikiSpaceId()
    {
        CreateBook().Warnings.Should().ContainSingle(
            w => w.Contains(DocAgentSettings.EnvWikiSpaceId, StringComparison.Ordinal));

        CreateBook(TestDoubles.CreateSettings() with { WikiSpaceId = "wikcn_space_123" })
            .Warnings.Should().BeEmpty();
    }

    /// <summary>只有只读链路才带自动应答；写类剧本必须为空（写动作一律要人确认，ADR-04）。</summary>
    [Fact]
    public void AutoAnswers_Should_OnlyExistForReadOnlyScenarios()
    {
        var book = CreateBook();

        book.Find("kb")!.AutoAnswers.Should().NotBeEmpty("只读链路可一键跑通多轮");
        book.Find("heal")!.AutoAnswers.Should().NotBeEmpty("只读链路可一键跑通多轮");

        book.Find("author")!.AutoAnswers.Should().BeEmpty("写类剧本不得自动应答（写动作一律要人工确认）");
        book.Find("md")!.AutoAnswers.Should().BeEmpty();
        book.Find("sheet")!.AutoAnswers.Should().BeEmpty();
        book.Find("del")!.AutoAnswers.Should().BeEmpty();
    }
}
