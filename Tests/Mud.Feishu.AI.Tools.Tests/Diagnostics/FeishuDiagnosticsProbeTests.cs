// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

using Mud.AI.Abstractions.Observability;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Diagnostics;
using Mud.Feishu.AI.Tools.Catalog;
using Mud.Feishu.AI.Tools.Diagnostics;
using Mud.Feishu.AI.Tools.Hosting;

namespace Mud.Feishu.AI.Tools.Tests.Diagnostics;

/// <summary>
/// 诊断自检面（E-2）：<c>Fail</c> 只给"必然不可用"的装配错误，能力面缺席一律 <c>Warn</c>。
/// </summary>
public class FeishuDiagnosticsProbeTests
{
    private const string ReadTool = "im.list_chats";
    private const string WriteTool = "im.send_message";

    private static FeishuToolRegistry Registry(params (string Name, bool IsWrite)[] tools)
    {
        var registry = new FeishuToolRegistry();
        foreach (var (name, isWrite) in tools)
        {
            registry.Register(new FeishuToolDefinition(
                name,
                "测试工具",
                ["im:read"],
                isWrite,
                isWrite ? FeishuToolRisk.HighRiskWrite : FeishuToolRisk.Read,
                FeishuToolIdentityNames.Tenant,
                static (_, _, _) => Task.FromResult(ToolResult.FromText("ok"))));
            registry.MapTool(name);
        }

        return registry;
    }

    private static FeishuDiagnosticsProbe Probe(
        IServiceProvider services,
        Action<FeishuAgentOptions>? configure = null)
    {
        var options = new FeishuAgentOptions { Instructions = "test" };
        configure?.Invoke(options);
        return new FeishuDiagnosticsProbe(services, Options.Create(options));
    }

    private static FeishuDiagnosticsCheck Check(FeishuDiagnosticsReport report, string name)
        => report.Checks.Single(c => c.Name == name);

    [Fact]
    public void Probe_WithoutToolSurface_ShouldFail()
    {
        var report = Probe(new StubProvider()).Probe();

        report.Status.Should().Be(FeishuDiagnosticsStatus.Fail);
        Check(report, "toolSurface.registry").Status.Should().Be(FeishuDiagnosticsStatus.Fail);
    }

    [Fact]
    public void Probe_EmptyEnabledSet_ShouldFail()
    {
        var provider = new StubProvider().Add(new FeishuToolRegistry());

        var check = Check(Probe(provider).Probe(), "toolSurface.registry");

        check.Status.Should().Be(FeishuDiagnosticsStatus.Fail);
        check.Detail.Should().Contain("启用集为空");
    }

    [Fact]
    public void Probe_WhitelistDrift_ShouldFailAndNameTheTool()
    {
        var provider = new StubProvider().Add(Registry((ReadTool, false)));

        var check = Check(Probe(provider, o => o.Tools = [ReadTool, "im.typo_tool"]).Probe(), "toolSurface.whitelist");

        check.Status.Should().Be(FeishuDiagnosticsStatus.Fail, "白名单写了不存在的工具名 = 静默失效，必须响亮");
        check.Detail.Should().Contain("im.typo_tool");
    }

    [Fact]
    public void Probe_WriteToolWithoutApprovalChannel_ShouldWarn()
    {
        var provider = new StubProvider().Add(Registry((WriteTool, true)));

        var check = Check(Probe(provider).Probe(), "approval.channel");

        check.Status.Should().Be(FeishuDiagnosticsStatus.Warn);
        check.Detail.Should().Contain(nameof(IFeishuToolApprovalChannel));
    }

    [Fact]
    public void Probe_MissingCapabilityFaces_ShouldWarnNotFail()
    {
        var provider = new StubProvider()
            .Add(Registry((ReadTool, false)))
            .Add(Mock.Of<IToolExecutionAuthorizer>());

        var report = Probe(provider).Probe();

        report.Status.Should().Be(FeishuDiagnosticsStatus.Warn, "能跑但观测面缺失属 Warn");
        Check(report, "observability.auditSink").Status.Should().Be(FeishuDiagnosticsStatus.Warn);
        Check(report, "observability.usageMeter").Status.Should().Be(FeishuDiagnosticsStatus.Warn);
        Check(report, "observability.telemetry").Status.Should().Be(FeishuDiagnosticsStatus.Warn);
    }

    [Fact]
    public void Probe_MissingAuthorizer_ShouldFailWhenEnforcementOn_AndWarnWhenOff()
    {
        var provider = new StubProvider().Add(Registry((ReadTool, false)));

        Check(Probe(provider).Probe(), "security.authorizer").Status
            .Should().Be(FeishuDiagnosticsStatus.Fail, "强制授权开启而授权器缺席 ⇒ 全部调用被拒");

        Check(Probe(provider, o => o.EnforceToolAuthorization = false).Probe(), "security.authorizer").Status
            .Should().Be(FeishuDiagnosticsStatus.Warn);
    }

    [Fact]
    public void Probe_FullyWired_ShouldBeOk()
    {
        IFeishuUsageMeter meter = new InMemoryFeishuUsageMeter();
        var provider = new StubProvider()
            .Add(Registry((ReadTool, false)))
            .Add(Mock.Of<IToolExecutionAuthorizer>())
            .Add(Mock.Of<IToolExecutionAuditSink>())
            .Add(meter)
            .Add<IAgentTelemetry>(NoopAgentTelemetry.Instance)
            .Add<IAgentToolTelemetry>(NoopAgentTelemetry.Instance);

        var report = Probe(provider).Probe();

        report.Status.Should().Be(
            FeishuDiagnosticsStatus.Ok,
            "诊断明细：" + string.Join(" | ", report.Checks.Select(c => $"{c.Name}={c.Status}:{c.Detail}")));
    }

    /// <summary>最小服务提供器替身：只按类型取，不建作用域、不做生命周期管理。</summary>
    private sealed class StubProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = [];

        public StubProvider Add<T>(T service)
            where T : class
        {
            _services[typeof(T)] = service;
            return this;
        }

        public object? GetService(Type serviceType)
            => _services.TryGetValue(serviceType, out var service) ? service : null;
    }
}
