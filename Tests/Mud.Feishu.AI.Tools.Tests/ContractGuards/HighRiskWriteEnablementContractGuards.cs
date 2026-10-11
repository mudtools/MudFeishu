// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// R-4 / F-13：<b>删除（高危写）能力受控启用</b>的装配期告警与审计可分辨性守卫。
/// </summary>
/// <remarks>
/// <para>
/// <b>锁定什么</b>：删除工具（<c>high-risk-write</c>）本题材下**不是"不提供"**，而是"默认对模型不可见"。
/// 宿主把它写进 <c>FeishuAgent:WriteAllowList</c> 即启用。本守卫锁定三件事：
/// ① 该启用动作在装配期<b>必须产生 Warning</b>（把"静默加白名单"升级为"显式告知"）；
/// ② 审计记录<b>必须带 Risk 字面量</b>（让删除在审计里可分辨、可聚合）；
/// ③ 启用删除但缺授权器时<b>仍然 fail-closed</b>（安全默认不因"能启用"而削弱）。
/// </para>
/// <para>
/// <b>为什么不引入"删除开关"</b>：启用/禁用的真相源唯一（<c>WriteAllowList</c> + <c>MaxToolRisk</c>）。
/// 再加一个开关会形成第二启用源（R5：同一语义只能有一个配置节），故本项只补可观测性。
/// </para>
/// <para>
/// <b>自证</b>：<see cref="HighRiskClassifier_ShouldFlagOnlyHighRiskWrite_OtherwiseGuardIsFalseGreen"/>
/// 用合成数据驱动同一判据，断言它真的能区分"高危写"与"普通写"——否则正例那条是假绿。
/// </para>
/// </remarks>
public class HighRiskWriteEnablementContractGuards
{
    private const string AppKey = "cli_test";

    [Fact]
    public void EnablingHighRiskWriteTool_ShouldEmitAssemblyWarning()
    {
        var (provider, logs) = BuildProvider(o => o.WriteAllowList = [FeishuToolNames.BitableDeleteRecord]);

        using (provider)
        {
            // 前置：判据不是空的——该工具在契约表里确实是 high-risk-write。
            FeishuToolContracts.ByToolName[FeishuToolNames.BitableDeleteRecord].Risk
                .Should().Be(FeishuToolRisk.HighRiskWrite,
                    "前置失效：本用例选定的工具不再是高危写（换一个真实的高危写工具，别让断言空转）");

            // 装配告警发生在注册表解析时（BuildRegistry 为懒解析）。
            var registry = provider.GetRequiredService<FeishuToolRegistry>();
            registry.EnabledTools.Select(static t => t.Name)
                .Should().Contain(FeishuToolNames.BitableDeleteRecord, "前置：白名单必须真的启用了该工具");

            var warnings = logs.Where(static e => e.Level == LogLevel.Warning).Select(static e => e.Message).ToArray();

            warnings.Should().NotBeEmpty("启用高危写工具必须产生装配期 Warning（F-13：把静默启用升级为显式告知）");

            var notice = string.Join("\n", warnings);
            notice.Should().Contain(FeishuToolNames.BitableDeleteRecord, "告警必须点名工具（宿主才知道自己启用了什么）");
            notice.Should().Contain("不可撤销", "告警必须说明后果（删除不可回滚）");
            notice.Should().Contain("IToolExecutionAuthorizer", "告警必须指向强制力来源（授权器）");
            notice.Should().Contain("MaxToolRisk", "告警必须给出收回手段");
        }
    }

    [Fact]
    public void EnablingOrdinaryWriteTool_ShouldNotEmitWarning()
    {
        var (provider, logs) = BuildProvider(o => o.WriteAllowList = [FeishuToolNames.BitableAddRecord]);

        using (provider)
        {
            var registry = provider.GetRequiredService<FeishuToolRegistry>();
            registry.EnabledTools.Select(static t => t.Name)
                .Should().Contain(FeishuToolNames.BitableAddRecord, "前置：白名单必须真的启用了普通写工具");

            logs.Where(static e => e.Level == LogLevel.Warning)
                .Should().BeEmpty("普通写（write）不得触发高危告警——否则告警会淹没在噪声里（负例自证）");
        }
    }

    [Fact]
    public async Task EnabledHighRiskWriteTool_ShouldStillFailClosed_WithoutAuthorizer()
    {
        var sink = new RecordingAuditSink();
        var (provider, _) = BuildProvider(
            o => o.WriteAllowList = [FeishuToolNames.BitableDeleteRecord],
            auditSink: sink);

        using (provider)
        {
            var registry = provider.GetRequiredService<FeishuToolRegistry>();
            var definition = registry.AllTools.Single(t => t.Name == FeishuToolNames.BitableDeleteRecord);

            var downstreamCalled = false;
            var result = await provider.GetRequiredService<FeishuToolBinding>().ExecuteAsync(
                definition,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["app_token"] = "bascn" },
                new FeishuToolContext(AppKey),
                _ =>
                {
                    downstreamCalled = true;
                    return Task.FromResult(FeishuToolResult.FromText("不应到达"));
                });

            downstreamCalled.Should().BeFalse("无授权器时写工具必须 fail-closed（零调用下游）");
            result.ToString().Should().Contain("authorization_denied", "拒绝原因必须指向授权（可修）");

            // F-13 验收②：审计可分辨——删除操作在审计记录里带高危风险字面量。
            var record = sink.Records.Should().ContainSingle().Subject;
            record.Decision.Should().Be("denied");
            record.Risk.Should().Be(FeishuToolRiskNames.HighRiskWrite,
                "审计必须能分辨高危写（否则'删了一条记录'与'发了一条消息'在告警里同形）");
        }
    }

    [Fact]
    public async Task MaxToolRiskWrite_ShouldDenyHighRiskWriteTool_WithRiskExceeded()
    {
        var (provider, _) = BuildProvider(o =>
        {
            o.WriteAllowList = [FeishuToolNames.BitableDeleteRecord];
            o.MaxToolRisk = FeishuToolRiskNames.Write;
        });

        using (provider)
        {
            var registry = provider.GetRequiredService<FeishuToolRegistry>();
            var definition = registry.AllTools.Single(t => t.Name == FeishuToolNames.BitableDeleteRecord);

            var result = await provider.GetRequiredService<FeishuToolBinding>().ExecuteAsync(
                definition,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["app_token"] = "bascn" },
                new FeishuToolContext(AppKey),
                _ => Task.FromResult(FeishuToolResult.FromText("不应到达")));

            result.ToString().Should().Contain(
                ToolErrorSubtype.RiskExceeded,
                "MaxToolRisk=write 是一键收回高危写的既有手段（F-13：不新增第二个开关）");
        }
    }

    /// <summary>负例自证：判据必须能区分高危写与普通写（否则正例恒绿）。</summary>
    [Fact]
    public void HighRiskClassifier_ShouldFlagOnlyHighRiskWrite_OtherwiseGuardIsFalseGreen()
    {
        ((int)FeishuToolRisk.HighRiskWrite).Should().BeGreaterThan((int)FeishuToolRisk.Write,
            "风险轴必须单调可比（MaxToolRisk 的'一键拒掉高危写'依赖该序）");
        ((int)FeishuToolRisk.Write).Should().BeGreaterThan((int)FeishuToolRisk.Read);

        SelectHighRisk([FeishuToolRisk.Read, FeishuToolRisk.Write, FeishuToolRisk.HighRiskWrite])
            .Should().Equal([FeishuToolRisk.HighRiskWrite], "判据必须只挑出高危写（挑不出即为假绿）");

        SelectHighRisk([FeishuToolRisk.Read, FeishuToolRisk.Write])
            .Should().BeEmpty("没有高危写时不得误报（否则告警会淹没在噪声里）");

        // 反向自证：真实契约表里必须确实存在高危写工具，否则"正例"只是因为找不到工具才过。
        FeishuToolContracts.ByToolName.Values
            .Select(static c => c.Risk)
            .Distinct()
            .Should().Contain(FeishuToolRisk.HighRiskWrite, "工具面必须存在高危写工具（删除/撤回），正例才有对象");
    }

    private static FeishuToolRisk[] SelectHighRisk(IEnumerable<FeishuToolRisk> risks)
        => [.. risks.Where(static r => r == FeishuToolRisk.HighRiskWrite)];

    private static (ServiceProvider Provider, List<(LogLevel Level, string Message)> Logs) BuildProvider(
        Action<FeishuAgentOptions> configure,
        IToolExecutionAuditSink? auditSink = null)
    {
        var options = new FeishuAgentOptions { Instructions = "test" };
        configure(options);

        var logs = new List<(LogLevel, string)>();

        var services = new ServiceCollection()
            .AddSingleton(Options.Create(options))
            // 作用域工厂的核心依赖（与生成的 HTTP 客户端同构，与工具域无关）。
            .AddSingleton(new Mock<Mud.HttpUtils.IAppContextHolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object)
            .AddSingleton(Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>())))
            // 高危写工具所在域（Bitable 写执行器）的客户端——缺席时该域写工具不进注册表。
            .AddSingleton(new Mock<Mud.Feishu.IFeishuTenantV1BitableRecord>().Object);

        if (auditSink is not null)
        {
            services.AddSingleton(auditSink);
        }

        services.AddLogging(builder => builder.AddProvider(new CapturingLoggerProvider(logs)));
        services.AddFeishuWriteTools();

        return (services.BuildServiceProvider(), logs);
    }

    /// <summary>捕获日志的提供程序（装配告警断言用；零第三方依赖）。</summary>
    private sealed class CapturingLoggerProvider(List<(LogLevel Level, string Message)> sink) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(sink);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(List<(LogLevel Level, string Message)> sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
                => sink.Add((logLevel, formatter(state, exception)));
        }
    }

    /// <summary>审计出口替身（F-13 验收② 读 Risk 字段）。</summary>
    private sealed class RecordingAuditSink : IToolExecutionAuditSink
    {
        public List<ToolExecutionAuditRecord> Records { get; } = [];

        public Task WriteAsync(ToolExecutionAuditRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }
    }
}
