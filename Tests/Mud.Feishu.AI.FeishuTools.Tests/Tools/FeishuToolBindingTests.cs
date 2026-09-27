// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// 工具执行链测试（<see cref="FeishuToolBinding"/>），含护城河硬验收三项（Phase 1 §5/§7）：
/// ① 双 appKey <c>BeginScope</c> 切换先于调用且作用域释放；② 授权拒绝时下游零调用；
/// ③ scopes/decision 进 OTel Span 属性。
/// </summary>
public class FeishuToolBindingTests : IDisposable
{
    private readonly Mock<IFeishuAppContextScopeFactory> _scopeFactory = new();
    private readonly List<string> _callLog = [];
    private readonly List<Activity> _activities = [];
    private readonly ActivityListener _listener;

    public FeishuToolBindingTests()
    {
        _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Callback((string appKey) => _callLog.Add($"scope:{appKey}"))
            .Returns(() =>
            {
                var disposed = false;
                return new DisposableAction(() =>
                {
                    if (!disposed)
                    {
                        disposed = true;
                        _callLog.Add("scope-release");
                    }
                });
            });

        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == FeishuActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => _activities.Add(activity),
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    private FeishuToolBinding CreateBinding(IToolExecutionAuthorizer? authorizer = null, FeishuAgentOptions? options = null)
        => new(
            _scopeFactory.Object,
            Options.Create(options ?? new FeishuAgentOptions { Instructions = "test" }),
            authorizer);

    private static FeishuToolDefinition Definition(
        string name = "test.tool",
        string[]? scopes = null,
        bool isWrite = false,
        FeishuToolHandler? handler = null)
        => new(
            name,
            "测试工具",
            scopes ?? ["test:read"],
            isWrite,
            handler ?? ((_, _, _) => Task.FromResult("downstream-result")));

    private static IReadOnlyDictionary<string, object?> Args() => new Dictionary<string, object?> { ["app_token"] = "bascnXxx" };

    [Fact]
    public async Task Execute_ShouldBeginScopeBeforeDownstream_AndReleaseAfter()
    {
        var binding = CreateBinding();

        var result = await binding.ExecuteAsync(
            Definition(),
            Args(),
            new FeishuToolContext("appA"),
            _ => { _callLog.Add("downstream"); return Task.FromResult("ok"); });

        result.Should().Be("ok");
        _callLog.Should().Equal(
            ["scope:appA", "downstream", "scope-release"],
            "BeginScope 必须先于下游调用（租户=应用上下文，TMA2-20），作用域在完成后释放");
    }

    [Fact]
    public async Task Execute_DualAppKeySequence_ShouldSwitchTenantContextPerCall()
    {
        // 护城河硬验收①：同进程内顺序切换双 appKey，作用域各按各自租户上下文进入并释放。
        var binding = CreateBinding();

        await binding.ExecuteAsync(Definition(), Args(), new FeishuToolContext("appA"),
            _ => Task.FromResult("from-A"));
        await binding.ExecuteAsync(Definition(), Args(), new FeishuToolContext("appB"),
            _ => Task.FromResult("from-B"));

        _callLog.Where(c => c.StartsWith("scope:", StringComparison.Ordinal)).Should().Equal(
            ["scope:appA", "scope:appB"],
            "两次调用分别切换到各自租户上下文——多租户隔离可演示");
    }

    [Fact]
    public async Task Execute_AuthorizerDenies_ShouldNotCallDownstream()
    {
        // 护城河硬验收②：拒绝时不切换租户上下文、零调用下游接口，错误结构化回填。
        var denied = new Mock<IToolExecutionAuthorizer>();
        denied
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<FeishuToolContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationResult.Deny("租户未开通该工具"));

        var binding = CreateBinding(denied.Object);
        var downstreamCalled = false;

        var result = await binding.ExecuteAsync(
            Definition(),
            Args(),
            new FeishuToolContext("appA"),
            _ => { downstreamCalled = true; return Task.FromResult("ok"); });

        downstreamCalled.Should().BeFalse("拒绝路径不得触碰下游接口");
        _callLog.Should().NotContain(c => c.StartsWith("scope:", StringComparison.Ordinal),
            "拒绝时不切入租户上下文");
        result.Should().StartWith("[tool_error] test.tool");
        result.Should().Contain("租户未开通该工具", "拒绝原因结构化回填模型");
    }

    [Fact]
    public async Task Execute_ShouldRecordToolNameAppKeyScopesAndDecisionInActivity()
    {
        // 护城河硬验收③：工具名/appKey/scopes/判定结果进结构化审计（OTel Span 属性）。
        var binding = CreateBinding();

        await binding.ExecuteAsync(
            Definition(name: "bitable.list_tables", scopes: ["bitable:app:readonly"]),
            Args(),
            new FeishuToolContext("appAudit"),
            _ => Task.FromResult("ok"));

        var activity = _activities.Should().ContainSingle().Subject;
        activity.GetTagItem(FeishuToolDiagnostics.TagToolName).Should().Be("bitable.list_tables");
        activity.GetTagItem(FeishuActivitySource.Tags.AppKey).Should().Be("appAudit");
        activity.GetTagItem(FeishuToolDiagnostics.TagScopes).Should().Be("bitable:app:readonly");
        activity.GetTagItem(FeishuToolDiagnostics.TagDecision).Should().Be(FeishuToolDiagnostics.DecisionAllowed);
    }

    [Fact]
    public async Task Execute_Denied_ShouldRecordDeniedDecision()
    {
        var denied = new Mock<IToolExecutionAuthorizer>();
        denied
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<FeishuToolContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationResult.Deny("no"));

        var binding = CreateBinding(denied.Object);
        await binding.ExecuteAsync(Definition(), Args(), new FeishuToolContext("appA"),
            _ => Task.FromResult("ok"));

        var activity = _activities.Should().ContainSingle().Subject;
        activity.GetTagItem(FeishuToolDiagnostics.TagDecision).Should().Be(FeishuToolDiagnostics.DecisionDenied);
    }

    [Fact]
    public async Task Execute_MissingAppKey_ShouldFailStructured_WithoutScope()
    {
        var binding = CreateBinding();

        var result = await binding.ExecuteAsync(Definition(), Args(), new FeishuToolContext(string.Empty),
            _ => Task.FromResult("ok"));

        result.Should().Contain("缺少 appKey", "多租户隔离禁止默认应用兜底（TMA2-20）");
        _callLog.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_WriteToolWithoutAuthorizer_WithEnforcement_ShouldDeny()
    {
        var binding = CreateBinding(options: new FeishuAgentOptions { Instructions = "test", EnforceToolAuthorization = true });

        var result = await binding.ExecuteAsync(
            Definition(name: "im.send", isWrite: true),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult("ok"));

        result.Should().Contain("未注册 IToolExecutionAuthorizer");
        _callLog.Should().NotContain(c => c.StartsWith("scope:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Execute_WriteToolWithoutAuthorizer_WithoutEnforcement_ShouldPass()
    {
        var binding = CreateBinding(options: new FeishuAgentOptions { Instructions = "test", EnforceToolAuthorization = false });

        var result = await binding.ExecuteAsync(
            Definition(name: "im.send", isWrite: true),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult("ok"));

        result.Should().Be("ok", "EnforceToolAuthorization=false 时写工具不强制（宿主自担风险）");
    }

    [Fact]
    public async Task Execute_NeedsUserConfirmation_ShouldNotExecute_AndBackfillReason()
    {
        var confirming = new Mock<IToolExecutionAuthorizer>();
        confirming
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<FeishuToolContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationResult.Confirm("等待用户批准"));

        var binding = CreateBinding(confirming.Object);
        var downstreamCalled = false;

        var result = await binding.ExecuteAsync(Definition(), Args(), new FeishuToolContext("appA"),
            _ => { downstreamCalled = true; return Task.FromResult("ok"); });

        downstreamCalled.Should().BeFalse("Phase 1 无 HITL 恢复机制，待确认不执行（Phase 3 交付）");
        result.Should().Contain("需要用户确认");
    }

    [Fact]
    public async Task Execute_DownstreamThrows_ShouldReturnStructuredError_NotRethrow()
    {
        var binding = CreateBinding();

        var result = await binding.ExecuteAsync(Definition(), Args(), new FeishuToolContext("appA"),
            _ => throw new InvalidOperationException("boom"));

        result.Should().StartWith("[tool_error]").And.Contain("boom");
        _callLog.Should().Contain("scope-release", "异常路径作用域仍释放");
    }

    [Fact]
    public async Task Execute_Cancelled_ShouldPropagateCancellation()
    {
        var binding = CreateBinding();

        var act = async () => await binding.ExecuteAsync(Definition(), Args(), new FeishuToolContext("appA"),
            (token => throw new OperationCanceledException(token)));

        await act.Should().ThrowAsync<OperationCanceledException>("取消即时传播（D15）");
    }

    private sealed class DisposableAction(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
