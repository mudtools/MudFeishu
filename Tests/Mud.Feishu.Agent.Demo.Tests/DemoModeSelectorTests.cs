// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// 启动菜单的模式选择：进入哪个演示模块**由用户选择决定**，配置文件只提供参数与默认项。
/// </summary>
/// <remarks>
/// 输入经委托注入、输出经 <see cref="ConsoleRenderer"/> 接缝注入，故选择链可在无终端环境下断言；
/// 「非交互不挂起」与「EOF 不挂起」是两条必须成立的行为（否则管道/CI 里进程会卡住等待输入）。
/// </remarks>
public class DemoModeSelectorTests
{
    /// <summary>内存配置（键名即完整路径，如 <c>FeishuToolsDemo:Enabled</c>）。</summary>
    private static DemoConfigurationSources Sources(params (string Key, string Value)[] values)
        => new(
            new ConfigurationBuilder()
                .AddInMemoryCollection(values.ToDictionary(
                    static v => v.Key,
                    static v => (string?)v.Value,
                    StringComparer.Ordinal))
                .Build(),
            [DocAgentSettings.AppSettingsFile]);

    /// <summary>按序返回预设输入行（用尽后返回 <see langword="null"/> = 输入结束）。</summary>
    private static Func<string?> Input(params string?[] lines)
    {
        var index = 0;
        return () => index < lines.Length ? lines[index++] : null;
    }

    /// <summary>输入编号即进入对应菜单项（编号顺序 = 菜单顺序）。</summary>
    [Fact]
    public void Select_ShouldReturnModeByOrdinal()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();

        var mode = DemoModeSelector.Select(Sources(), renderer, interactive: true, Input("2"));

        mode.Should().NotBeNull();
        mode!.Id.Should().Be("tools");
        output.ToString().Should().Contain("请选择运行模式", "交互路径必须先打印菜单");
    }

    /// <summary>输入模式标识或配置节名同样命中（大小写不敏感、容忍首尾空白）。</summary>
    [Theory]
    [InlineData("doc", "doc")]
    [InlineData("DOC", "doc")]
    [InlineData("  events  ", "events")]
    [InlineData(DomainEventsDemoSettings.SectionName, "events")]
    public void Select_ShouldReturnModeByIdOrSectionName(string input, string expectedId)
    {
        var (renderer, _) = TestDoubles.CreateRenderer();

        var mode = DemoModeSelector.Select(Sources(), renderer, interactive: true, Input(input));

        mode.Should().NotBeNull();
        mode!.Id.Should().Be(expectedId);
    }

    /// <summary>回车（空白输入）采用配置默认项：启用项优先，无启用项时回落菜单首项（裸模型）。</summary>
    [Fact]
    public void Select_ShouldUseConfiguredDefault_WhenInputBlank()
    {
        var (renderer, _) = TestDoubles.CreateRenderer();
        var configured = Sources((DocAgentSettings.ConfigEnabledKey, "true"));

        DemoModeSelector.Select(configured, renderer, interactive: true, Input(string.Empty))!.Id
            .Should().Be("doc");
        DemoModeSelector.Select(configured, renderer, interactive: true, Input("   "))!.Id
            .Should().Be("doc");
        DemoModeSelector.Select(Sources(), renderer, interactive: true, Input(string.Empty))!.Id
            .Should().Be("bare", "无任何 Enabled=true 时默认项是菜单首项");
    }

    /// <summary>非交互输入（stdin 重定向 / 管道 / CI）跳过菜单且不读取输入——不挂起等待。</summary>
    [Fact]
    public void Select_ShouldNotReadInput_WhenNonInteractive()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();
        var readCount = 0;

        var mode = DemoModeSelector.Select(
            Sources((ToolsDemoSettings.EnabledKey, "true")),
            renderer,
            interactive: false,
            () =>
            {
                readCount++;
                return "9";
            });

        mode!.Id.Should().Be("tools");
        readCount.Should().Be(0);
        output.ToString().Should().Contain("跳过模式选择");
    }

    /// <summary>输入流耗尽（EOF）与"非交互"同处置：采用默认项而不是永久等待。</summary>
    [Fact]
    public void Select_ShouldFallBackToDefault_WhenInputEnds()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();

        var mode = DemoModeSelector.Select(
            Sources((SkillsDemoSettings.EnabledKey, "true")),
            renderer,
            interactive: true,
            Input());

        mode!.Id.Should().Be("skills");
        output.ToString().Should().Contain("输入已结束");
    }

    /// <summary><c>q</c> 取消选择：返回 <see langword="null"/> 且不再推进读取。</summary>
    [Fact]
    public void Select_ShouldReturnNull_WhenUserQuits()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();
        var readCount = 0;

        var mode = DemoModeSelector.Select(
            Sources(),
            renderer,
            interactive: true,
            () =>
            {
                readCount++;
                return "q";
            });

        mode.Should().BeNull();
        readCount.Should().Be(1);
        output.ToString().Should().Contain("已取消");
    }

    /// <summary>非法输入就地提示后重问（超范围编号与未知标识都不得被静默当成默认项）。</summary>
    [Fact]
    public void Select_ShouldReprompt_WhenInputInvalid()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();
        var readCount = 0;

        var mode = DemoModeSelector.Select(
            Sources(),
            renderer,
            interactive: true,
            () =>
            {
                readCount++;
                return readCount switch
                {
                    1 => "zzz",
                    2 => "99",
                    _ => "3",
                };
            });

        mode!.Id.Should().Be("im", "第三次输入 3 才命中");
        readCount.Should().Be(3);
        output.ToString().Should().Contain("无效选择").And.Contain("99");
    }
}

/// <summary>
/// 模式目录契约：菜单项 ↔ 设置记录（节名 / 默认项开关键）机械对齐，防「加了菜单项但没接真实设置」。
/// </summary>
public class DemoModeCatalogTests
{
    /// <summary>菜单顺序与各设置记录的节名逐项一致（顺序即「能力由弱到强」的阅读顺序）。</summary>
    [Fact]
    public void Catalog_ShouldCoverEveryModeSection_InMenuOrder()
    {
        DemoModeCatalog.All.Select(static mode => mode.SectionName).Should().Equal(
            FeishuDemoSettings.SectionName,
            ToolsDemoSettings.SectionName,
            ImHandlerDemoSettings.SectionName,
            DomainEventsDemoSettings.SectionName,
            DocAgentSettings.SectionName,
            SkillsDemoSettings.SectionName);

        DemoModeCatalog.All.Select(static mode => mode.Id).Should().OnlyHaveUniqueItems();
        DemoModeCatalog.All.Should().OnlyContain(static mode => mode.Run != null, "每个菜单项都必须接到真实入口");
    }

    /// <summary>默认项开关键必须取自设置记录的常量（写错字面量会让菜单默认项永远失效）。</summary>
    [Fact]
    public void Catalog_EnabledKeys_ShouldMatchSettingsConstants()
    {
        DemoModeCatalog.All.Single(static mode => mode.Id == "tools").EnabledKey.Should().Be(ToolsDemoSettings.EnabledKey);
        DemoModeCatalog.All.Single(static mode => mode.Id == "im").EnabledKey.Should().Be(ImHandlerDemoSettings.EnabledKey);
        DemoModeCatalog.All.Single(static mode => mode.Id == "events").EnabledKey.Should().Be(DomainEventsDemoSettings.EnabledKey);
        DemoModeCatalog.All.Single(static mode => mode.Id == "doc").EnabledKey.Should().Be(DocAgentSettings.ConfigEnabledKey);
        DemoModeCatalog.All.Single(static mode => mode.Id == "skills").EnabledKey.Should().Be(SkillsDemoSettings.EnabledKey);
        DemoModeCatalog.All.Single(static mode => mode.Id == "bare").EnabledKey.Should().BeNull("裸模型无开关");

        DemoModeCatalog.All.Select(static mode => mode.EnabledKey ?? "(none)").Should().OnlyHaveUniqueItems(
            "开关键撞名会让「默认项」判定串线");
    }

    /// <summary>随仓库提交的模板必须让文档业务智能体成为菜单默认项（"clone 即可体验主线"的默认体验）。</summary>
    [Fact]
    public void Default_ShouldBeDocAgent_ForShippedTemplate()
    {
        var template = new ConfigurationBuilder()
            .AddJsonFile(
                Path.Combine(
                    TestDoubles.RepositoryRoot(),
                    "Demos",
                    "Mud.Feishu.Agent.Demo",
                    DocAgentSettings.AppSettingsFile),
                optional: false,
                reloadOnChange: false)
            .Build();

        DemoModeCatalog.Default(template).Id.Should().Be("doc");
    }
}
