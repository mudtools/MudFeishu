// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using Mud.Feishu.Abstractions;

namespace Mud.Feishu.AI.Tools.Tests.Hosting;

/// <summary>
/// GAP-11 装配自检面：逐条结论码的<b>可达性</b>与严重度分档。
/// </summary>
/// <remarks>
/// 自检面的价值在于「宿主还没调用就把装配缺陷读出来」。因此每条结论都要被证明确实在
/// 对应的错误装配下出现（否则是一条永不命中的死码），且 <c>error</c> 只留给
/// 「运行期必然失败 / 必然静默丢弃」的形态。
/// </remarks>
public class FeishuDiagnosticsProbeTests
{
    private static FeishuDiagnosticsReport ProbeOf(
        Action<FeishuAgentOptions> configureOptions,
        Action<IServiceCollection>? configureServices = null)
    {
        using var provider = GuardProviderFactory.CreateProvider(configureOptions, configureServices: configureServices);
        return provider.GetRequiredService<IFeishuDiagnosticsProbe>().Probe();
    }

    private static string[] CodesOf(FeishuDiagnosticsReport report)
        => report.Findings.Select(static f => f.Code).ToArray();

    private static string SeverityOf(FeishuDiagnosticsReport report, string code)
        => report.Findings.Single(f => f.Code == code).Severity;

    private static string FactOf(FeishuDiagnosticsReport report, string name)
        => report.Facts.Single(f => f.Name == name).Value;

    private static int FactCountOf(FeishuDiagnosticsReport report, string name)
        => int.Parse(FactOf(report, name), CultureInfo.InvariantCulture);

    [Fact]
    public void Probe_ShouldReportConfigInvalid_WhenOptionsFailValidation()
    {
        var report = ProbeOf(o => o.Instructions = string.Empty);

        CodesOf(report).Should().Contain("config_invalid");
        SeverityOf(report, "config_invalid").Should().Be(FeishuDiagnosticsSeverities.Error);
        report.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void Probe_ShouldReportOptionsMissing_WhenProviderHasNoAssembly()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();

        var report = new FeishuDiagnosticsProbe(provider).Probe();

        CodesOf(report).Should().Contain("options_missing");
        SeverityOf(report, "options_missing").Should().Be(FeishuDiagnosticsSeverities.Info);
        CodesOf(report).Should().Contain("no_tools_enabled");
    }

    [Fact]
    public void Probe_ShouldReportReadAndWriteSplit_WhenOnlyReadToolsEnabled()
    {
        var report = ProbeOf(o => o.Tools = [FeishuToolNames.BitableListTables, FeishuToolNames.BitableListFields]);

        FactOf(report, "tools.enabled").Should().Be("2");
        FactOf(report, "tools.enabled_read").Should().Be("2");
        FactOf(report, "tools.enabled_write").Should().Be("0");
        FactOf(report, "tools.domains_enabled").Should().Be("1");
        CodesOf(report).Should().NotContain("no_tools_enabled");
    }

    [Fact]
    public void Probe_ShouldReportNoToolsEnabled_WhenWhitelistEmpty()
    {
        var report = ProbeOf(_ => { });

        CodesOf(report).Should().Contain("no_tools_enabled");
        FactOf(report, "tools.enabled").Should().Be("0");
    }

    /// <summary>
    /// 注册表构建失败时，后续判据必须<b>复用同一次解析结果</b>：自行再解析会把同一条装配异常
    /// 抛第二次，报告退化成无信息量的 <c>probe_fault</c>（宿主看到「自检坏了」而不是「白名单配错了」）。
    /// </summary>
    [Fact]
    public void Probe_ShouldReportRegistryBuildFailed_WhenWhitelistNamesUnknownTool()
    {
        var report = ProbeOf(o => o.Tools = ["not.a.tool"]);

        CodesOf(report).Should().Contain("registry_build_failed");
        CodesOf(report).Should().NotContain("probe_fault");
        SeverityOf(report, "registry_build_failed").Should().Be(FeishuDiagnosticsSeverities.Error);
        FactOf(report, "guidance.domains").Should().Be("0");
    }

    [Fact]
    public void Probe_ShouldReportContractGap_WhenOnlyOneDomainAssembled()
    {
        using var provider = new ServiceCollection()
            .AddSingleton(Options.Create(new FeishuAgentOptions
            {
                Instructions = "test",
                Tools = [FeishuToolNames.BitableListTables],
            }))
            .AddSingleton(new Mock<Mud.HttpUtils.IAppContextHolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object)
            .AddSingleton(Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>())))
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableAppTable>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableField>().Object)
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableRecord>().Object)
            .AddFeishuBitableTools()
            .BuildServiceProvider();

        var report = provider.GetRequiredService<IFeishuDiagnosticsProbe>().Probe();

        CodesOf(report).Should().Contain("contract_registry_gap");
        SeverityOf(report, "contract_registry_gap").Should().Be(FeishuDiagnosticsSeverities.Warning);
        FactCountOf(report, "tools.contract_surface").Should()
            .BeGreaterThan(FactCountOf(report, "tools.registered"));
    }

    [Fact]
    public void Probe_ShouldReportWriteToolsWithoutAuthorizer_WhenEnforcementOnAndHookAbsent()
    {
        var report = ProbeOf(o => o.WriteAllowList = [FeishuToolNames.ImSendMessage]);

        CodesOf(report).Should().Contain("write_tools_without_authorizer");
        SeverityOf(report, "write_tools_without_authorizer").Should().Be(FeishuDiagnosticsSeverities.Error);
        CodesOf(report).Should().NotContain("authorization_not_enforced");
    }

    [Fact]
    public void Probe_ShouldReportAuthorizationNotEnforced_WhenEnforcementOff()
    {
        var report = ProbeOf(o =>
        {
            o.WriteAllowList = [FeishuToolNames.ImSendMessage];
            o.EnforceToolAuthorization = false;
        });

        CodesOf(report).Should().Contain("authorization_not_enforced");
        CodesOf(report).Should().NotContain("write_tools_without_authorizer");
    }

    [Fact]
    public void Probe_ShouldReportApprovalChannelMissing_WhenAuthorizerRegisteredWithoutChannel()
    {
        var report = ProbeOf(
            _ => { },
            services => services.AddSingleton(Mock.Of<IToolExecutionAuthorizer>()));

        CodesOf(report).Should().Contain("approval_channel_missing");
        SeverityOf(report, "approval_channel_missing").Should().Be(FeishuDiagnosticsSeverities.Warning);
        FactOf(report, $"host.{nameof(IToolExecutionAuthorizer)}").Should().Be("true");
    }

    [Fact]
    public void Probe_ShouldReportRiskPolicyMismatch_WhenEnabledToolExceedsCeiling()
    {
        var report = ProbeOf(o =>
        {
            o.WriteAllowList = [FeishuToolNames.ImSendMessage];
            o.MaxToolRisk = FeishuToolRiskNames.Read;
        });

        CodesOf(report).Should().Contain("risk_policy_mismatch");
        SeverityOf(report, "risk_policy_mismatch").Should().Be(FeishuDiagnosticsSeverities.Error);
        report.Findings.Single(f => f.Code == "risk_policy_mismatch").Message
            .Should().Contain(FeishuToolNames.ImSendMessage);
    }

    [Fact]
    public void Probe_ShouldReportHighRiskWriteEnabled_WhenDeleteToolEnabled()
    {
        var report = ProbeOf(o => o.WriteAllowList = [FeishuToolNames.BitableDeleteRecord]);

        CodesOf(report).Should().Contain("high_risk_write_enabled");
        FactOf(report, "tools.enabled_high_risk_write").Should().Be("1");
    }

    [Fact]
    public void Probe_ShouldReportContentSafetyOff_WhenModeOff()
    {
        var report = ProbeOf(o => o.ContentSafetyMode = ContentSafetyModes.Off);

        CodesOf(report).Should().Contain("content_safety_off");
        FactOf(report, $"policy.{nameof(FeishuAgentOptions.ContentSafetyMode)}").Should().Be("off");
    }

    [Fact]
    public void Probe_ShouldReportUserToolWiring_WhenUserIdentityToolEnabledWithoutContext()
    {
        var report = ProbeOf(o =>
        {
            o.Tools = [FeishuToolNames.TaskListMyTasks];
            o.AllowedIdentities = ["tenant", "user"];
        });

        FactOf(report, "tools.enabled_user_identity").Should().Be("1");
        CodesOf(report).Should().Contain("user_tool_wiring_missing");
        CodesOf(report).Should().Contain("user_token_resolver_missing");
        SeverityOf(report, "user_tool_wiring_missing").Should().Be(FeishuDiagnosticsSeverities.Error);
    }

    [Fact]
    public void Probe_ShouldReportUserTokenPrecheckOff_WhenResolverRegisteredAndPrecheckOff()
    {
        var report = ProbeOf(
            o =>
            {
                o.Tools = [FeishuToolNames.TaskListMyTasks];
                o.AllowedIdentities = ["tenant", "user"];
                o.UserTokenPrecheck = UserTokenPrecheckModes.Off;
            },
            services => services.AddSingleton(Mock.Of<IFeishuTokenManagerResolver>()));

        CodesOf(report).Should().Contain("user_token_precheck_off");
        CodesOf(report).Should().NotContain("user_token_resolver_missing");
    }

    [Fact]
    public void Probe_ShouldReportNoAppsConfigured_WhenEnabledToolsExistButNoAppRegistered()
    {
        var report = ProbeOf(o => o.Tools = [FeishuToolNames.BitableListTables]);

        CodesOf(report).Should().Contain("no_apps_configured");
        FactOf(report, "host.apps_configured").Should().Be("0");
    }

    [Fact]
    public void Probe_ShouldReportAuditSinkAsInfo_WhenEnabledToolsExist()
    {
        var report = ProbeOf(o => o.Tools = [FeishuToolNames.BitableListTables]);

        CodesOf(report).Should().Contain("audit_sink_missing");
        SeverityOf(report, "audit_sink_missing").Should().Be(FeishuDiagnosticsSeverities.Info);
    }

    [Fact]
    public void Probe_ShouldReportHeadroomConsistentWithComposedLength_WhenGuidanceAssembled()
    {
        var report = ProbeOf(o =>
        {
            o.Tools = [.. FeishuToolNames.ReadonlyAll];
            o.AllowedIdentities = ["tenant", "user"];
            o.GuidanceMode = GuidanceModes.Full;
        });

        var budget = FactCountOf(report, "guidance.budget_chars");
        var composed = FactCountOf(report, "guidance.composed_chars");

        budget.Should().Be(FeishuGuidanceBudget.MaxGuidanceLength);
        composed.Should().BeLessThanOrEqualTo(budget);
        FactCountOf(report, "guidance.headroom_chars").Should().Be(budget - composed);
        FactCountOf(report, "guidance.domains").Should().BeGreaterThan(1);
    }

    /// <summary>
    /// 宿主指令吃满预算时，guidance 轴<b>必须有一条结论落地</b>——整域丢弃是 <c>error</c>，
    /// 未丢弃但余量见底是 <c>warning</c>；两条都不发即「预算已尽而报告沉默」。
    /// </summary>
    [Fact]
    public void Probe_ShouldReportGuidancePressure_WhenHostInstructionLeavesNoHeadroom()
    {
        var report = ProbeOf(o =>
        {
            o.Tools = [.. FeishuToolNames.ReadonlyAll];
            o.AllowedIdentities = ["tenant", "user"];
            o.GuidanceMode = GuidanceModes.Full;
            o.Instructions = new string('x', FeishuGuidanceBudget.MaxGuidanceLength);
        });

        CodesOf(report).Should().Contain(
            static code => code == "guidance_domains_omitted" || code == "guidance_budget_low",
            "预算见底必须告知（否则宿主会把「模型失去避坑知识」当成随机答错）");
    }

    [Fact]
    public void Probe_ShouldNotThrow_WhenHostHookFactoryThrows()
    {
        var report = ProbeOf(
            o => o.Tools = [FeishuToolNames.BitableListTables],
            services => services.AddSingleton<IToolExecutionAuthorizer>(
                _ => throw new NotSupportedException("宿主钩子激活失败")));

        CodesOf(report).Should().Contain("probe_fault");
        SeverityOf(report, "probe_fault").Should().Be(FeishuDiagnosticsSeverities.Error);
    }

    [Fact]
    public void Probe_ShouldBeDeterministic_WhenCalledTwiceOnSameAssembly()
    {
        using var provider = GuardProviderFactory.CreateProvider(o =>
        {
            o.Tools = [FeishuToolNames.BitableListTables];
            o.WriteAllowList = [FeishuToolNames.BitableDeleteRecord];
        });

        var probe = provider.GetRequiredService<IFeishuDiagnosticsProbe>();

        probe.Probe().ToText().Should().Be(probe.Probe().ToText());
    }

    [Fact]
    public void ToText_ShouldOrderFindingsBySeverityThenCode_AndFactsByName()
    {
        var report = new FeishuDiagnosticsReport(
            [
                new FeishuDiagnosticsFinding(FeishuDiagnosticsSeverities.Info, "zzz_info", "m"),
                new FeishuDiagnosticsFinding(FeishuDiagnosticsSeverities.Warning, "aaa_warn", "m"),
                new FeishuDiagnosticsFinding(FeishuDiagnosticsSeverities.Error, "mmm_err", "m"),
                new FeishuDiagnosticsFinding(FeishuDiagnosticsSeverities.Error, "aaa_err", "m"),
            ],
            [
                new FeishuDiagnosticsFact("tools.enabled", "1"),
                new FeishuDiagnosticsFact("guidance.domains", "2"),
            ]);

        var text = report.ToText();

        var expected = string.Join(
            Environment.NewLine,
            new[]
            {
                "诊断结论（4 条）",
                "  [error] aaa_err — m",
                "  [error] mmm_err — m",
                "  [warning] aaa_warn — m",
                "  [info] zzz_info — m",
                "事实读数（2 条）",
                "  guidance.domains = 2",
                "  tools.enabled = 1",
            });

        text.Should().Be(expected);
        report.HasErrors.Should().BeTrue();
    }
}
