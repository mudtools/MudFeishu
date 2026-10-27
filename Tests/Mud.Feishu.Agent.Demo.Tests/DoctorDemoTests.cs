// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Mud.Feishu.AI.Tools.Hosting;

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// 装配自检模式（doctor）：配置面（凭证可选 / 半配置 fail-fast）与自检报告面（结论码 + 确定性文本）。
/// </summary>
/// <remarks>
/// 断言对象是<b>占位凭证形态</b>的完整容器——CI 里没有飞书凭证，而自检面本来就是零网络：
/// 占位凭证不会被使用，报出的接线事实与结论码与真实凭证装配同构（补齐凭证只影响令牌维度读数）。
/// </remarks>
public class DoctorDemoTests
{
    /// <summary>内存配置（键名即完整路径）。</summary>
    private static IConfigurationRoot Config(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(
                static kv => kv.Key,
                static kv => (string?)kv.Value,
                StringComparer.Ordinal))
            .Build();

    private static FeishuDiagnosticsReport ProbePlaceholderCredential()
    {
        using var provider = DoctorDemo.CreateProvider(new DoctorDemoSettings());
        return provider.GetRequiredService<IFeishuDiagnosticsProbe>().Probe();
    }

    private static string Fact(FeishuDiagnosticsReport report, string name)
        => report.Facts.Single(fact => string.Equals(fact.Name, name, StringComparison.Ordinal)).Value;

    // ──────────────────────────── 配置面 ────────────────────────────

    /// <summary>零凭证即可解析：不读模型三项，凭证缺席不报错（本模式的可用性前提）。</summary>
    [Fact]
    public void DoctorDemoSettings_ShouldParse_WithoutModelOrCredentials()
    {
        var settings = DoctorDemoSettings.FromConfiguration(Config((DoctorDemoSettings.EnabledKey, "true")));

        settings.Enabled.Should().BeTrue();
        settings.AppId.Should().BeNull();
        settings.HasCredentials.Should().BeFalse();

        var act = () => settings.Validate();
        act.Should().NotThrow("零凭证是受支持的自检形态");
    }

    /// <summary>凭证从统一节读取（齐备才算有凭证）。</summary>
    [Fact]
    public void DoctorDemoSettings_ShouldReadOptionalCredentials_FromSharedSection()
    {
        var settings = DoctorDemoSettings.FromConfiguration(Config(
            ("FeishuDemo:AppId", "cli_test"),
            ("FeishuDemo:AppSecret", "secret-test")));

        settings.AppId.Should().Be("cli_test");
        settings.AppSecret.Should().Be("secret-test");
        settings.HasCredentials.Should().BeTrue();
    }

    /// <summary>只配一半凭证必须 fail-fast——静默退化成零凭证形态会让报告结论失真。</summary>
    [Theory]
    [InlineData("AppId")]
    [InlineData("AppSecret")]
    public void DoctorDemoSettings_ShouldFailFast_WhenCredentialsHalfConfigured(string missing)
    {
        var pairs = new List<(string Key, string Value)>
        {
            ("FeishuDemo:AppId", "cli_test"),
            ("FeishuDemo:AppSecret", "secret-test"),
        };

        var config = Config([.. pairs.Where(pair => pair.Key != $"FeishuDemo:{missing}")]);

        var settings = DoctorDemoSettings.FromConfiguration(config);

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{missing}*");
        settings.HasCredentials.Should().BeFalse("半配置按无凭证处理，故必须先被 Validate 拦下");
    }

    // ──────────────────────────── 自检报告面 ────────────────────────────

    /// <summary>占位凭证照样装配出完整容器：白名单不悬空、应用面在位、guidance 读数非零。</summary>
    [Fact]
    public void Probe_ShouldReportSurfaceAndGuidanceFacts_WhenPlaceholderCredential()
    {
        var report = ProbePlaceholderCredential();
        var codes = report.Findings.Select(static finding => finding.Code).ToArray();

        int.Parse(Fact(report, "tools.enabled"), CultureInfo.InvariantCulture)
            .Should().BeGreaterThan(0,
                "全域只读白名单必须真的启用工具——否则自检只剩「白名单含未注册工具」一条噪声");
        Fact(report, "tools.contract_surface").Should().NotBeNullOrWhiteSpace();
        int.Parse(Fact(report, "guidance.composed_chars"), CultureInfo.InvariantCulture)
            .Should().BeGreaterThan(0);
        Fact(report, "host.IFeishuAppManager").Should().Be("true", "占位凭证把应用面装配完整，自检才读得到接线事实");
        int.Parse(Fact(report, "host.apps_configured"), CultureInfo.InvariantCulture).Should().Be(1);

        codes.Should().NotContain("no_apps_configured");
        codes.Should().NotContain("registry_build_failed");
    }

    /// <summary>只读白名单不触发写类门禁结论（结论码必须由判据真实成立才产出）。</summary>
    [Fact]
    public void Probe_ShouldNotFlagWriteGates_WhenReadonlyWhitelist()
    {
        var report = ProbePlaceholderCredential();
        var codes = report.Findings.Select(static finding => finding.Code).ToArray();

        Fact(report, "tools.enabled_write").Should().Be("0");

        codes.Should().NotContain("write_tools_without_authorizer");
        codes.Should().NotContain("authorization_not_enforced");
        codes.Should().NotContain("high_risk_write_enabled");
        codes.Should().NotContain("risk_policy_mismatch");
    }

    /// <summary>同一装配两次自检逐字节相同（报告可作冒烟比对与差异断言面）。</summary>
    [Fact]
    public void Probe_ShouldBeDeterministic()
    {
        using var provider = DoctorDemo.CreateProvider(new DoctorDemoSettings());
        var probe = provider.GetRequiredService<IFeishuDiagnosticsProbe>();

        probe.Probe().ToText().Should().Be(probe.Probe().ToText());
    }

    /// <summary>报告正文含分段结论计数与末行判定（判定行跟着结论走）。</summary>
    [Fact]
    public void BuildReport_ShouldIncludeCountsAndVerdict()
    {
        var report = ProbePlaceholderCredential();

        var text = string.Join("\n", DoctorDemo.BuildReport(report));

        text.Should().Contain("=== 装配自检（doctor）===");
        text.Should().Contain("诊断结论（");
        text.Should().Contain("事实读数（");
        text.Should().Contain(report.HasErrors ? "判定：存在" : "判定：无 error",
            "判定行必须与报告自身的 error 读数一致");
    }

    /// <summary>有 error 级结论时判定必须指向处置（不得写成「自洽」）。</summary>
    [Fact]
    public void BuildReport_ShouldPointToRemediation_WhenErrorPresent()
    {
        var report = new FeishuDiagnosticsReport(
            [new FeishuDiagnosticsFinding(FeishuDiagnosticsSeverities.Error, "no_tools_enabled", "已启用工具为 0")],
            []);

        var text = string.Join("\n", DoctorDemo.BuildReport(report));

        text.Should().Contain("判定：存在 1 条 error 级结论");
        text.Should().Contain("[error] no_tools_enabled");
    }

    [Fact]
    public void BuildReport_ShouldRejectNullReport()
    {
        var act = () => DoctorDemo.BuildReport(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>无 error / 无 warning 时判定为「自洽」——判定行必须跟着结论走，不是固定文案。</summary>
    [Fact]
    public void BuildReport_ShouldStateCleanVerdict_WhenNoFindings()
    {
        var text = string.Join("\n", DoctorDemo.BuildReport(new FeishuDiagnosticsReport([], [])));

        text.Should().Contain("判定：无 error / warning 级结论");
    }
}
