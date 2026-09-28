// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// T4-2（WP4 / D-3）执行链集成：NeedsUserConfirmation 的「签发 → 用户同意 → 带令牌重试」闭环，
/// 以及降级（未配置密钥）与防滥用（换参数重试、令牌不豁免 Denied）语义。
/// </summary>
public class ToolConfirmationTokenFlowTests : IDisposable
{
    private const string Secret = "flow-test-host-secret";
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IFeishuAppContextScopeFactory> _scopeFactory = new();

    public void Dispose() => GC.SuppressFinalize(this);

    private FeishuToolBinding CreateBinding(
        IToolExecutionAuthorizer authorizer,
        IToolConfirmationTokenSecretProvider? secretProvider,
        Func<DateTimeOffset>? clock = null)
    {
        _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Returns(new DisposableAction(() => { }));

        return new FeishuToolBinding(
            _scopeFactory.Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            authorizer,
            confirmationTokenSecretProvider: secretProvider,
            utcClock: clock ?? (() => Now));
    }

    private static Mock<IToolExecutionAuthorizer> ConfirmingAuthorizer()
    {
        var authorizer = new Mock<IToolExecutionAuthorizer>();
        authorizer
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<FeishuToolContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationResult.Confirm("删除操作须用户批准"));
        return authorizer;
    }

    private static IToolConfirmationTokenSecretProvider FixedSecret() =>
        new FixedSecretProvider(Secret);

    private sealed class FixedSecretProvider(string secret) : IToolConfirmationTokenSecretProvider
    {
        public string? GetSecret() => secret;
    }

    private static FeishuToolDefinition Definition(string name = "test.tool", FeishuToolHandler? handler = null)
        => new(name, "测试工具", ["test:write"], true, FeishuToolRisk.Write, "tenant",
            handler ?? ((_, _, _) => Task.FromResult(FeishuToolResult.FromText("downstream-result"))));

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    [Fact]
    public async Task Execute_NeedsConfirmation_ShouldIssueToken_WhenSecretConfigured()
    {
        var binding = CreateBinding(ConfirmingAuthorizer().Object, FixedSecret());
        var result = await binding.ExecuteAsync(
            Definition(), Args(("text", "hello")), new FeishuToolContext("appA", UserId: "user1"), CancellationTokenOnly);

        var text = result.ToString()!;
        text.Should().Contain("(needs_confirmation)");
        text.Should().Contain("确认令牌: v1.", "密钥已配置时拒绝文案必须携带签发的确认令牌（T4-2 真挂起）");
        text.Should().Contain("confirm_token", "必须告知模型重试方式");
    }

    [Fact]
    public async Task Execute_WithValidConfirmToken_ShouldBypassConfirmBranch_AndExecuteDownstream()
    {
        var binding = CreateBinding(ConfirmingAuthorizer().Object, FixedSecret());
        var first = await binding.ExecuteAsync(
            Definition(), Args(("text", "hello")), new FeishuToolContext("appA", UserId: "user1"), CancellationTokenOnly);
        var token = Regex.Match(first.ToString()!, @"确认令牌: (v1\.[0-9]+\.[A-Za-z0-9_-]+)").Groups[1].Value;

        token.Should().NotBeNullOrEmpty("第一次调用必须拿到令牌");

        var downstreamCalls = 0;
        var binding2 = CreateBinding(ConfirmingAuthorizer().Object, FixedSecret());
        var retry = await binding2.ExecuteAsync(
            Definition(),
            Args(("text", "hello"), (ToolConfirmationToken.ArgumentName, token)),
            new FeishuToolContext("appA", UserId: "user1"),
            _ =>
            {
                downstreamCalls++;
                return Task.FromResult(FeishuToolResult.FromText("executed"));
            });

        retry.ToString().Should().Be("executed", "有效令牌 = 用户已批准，跳过 Confirm 分支执行下游");
        downstreamCalls.Should().Be(1);
    }

    [Fact]
    public async Task Execute_WithTokenButChangedArguments_ShouldDenyAgain()
    {
        var binding = CreateBinding(ConfirmingAuthorizer().Object, FixedSecret());
        var first = await binding.ExecuteAsync(
            Definition(), Args(("text", "hello")), new FeishuToolContext("appA", UserId: "user1"), CancellationTokenOnly);
        var token = Regex.Match(first.ToString()!, @"确认令牌: (v1\.[0-9]+\.[A-Za-z0-9_-]+)").Groups[1].Value;

        var retry = await binding.ExecuteAsync(
            Definition(), Args(("text", "changed-value"), (ToolConfirmationToken.ArgumentName, token)),
            new FeishuToolContext("appA", UserId: "user1"), CancellationTokenOnly);

        retry.ToString().Should().Contain("(needs_confirmation)", "换参数即失效——批准不得搬运到不同调用");
        retry.ToString().Should().Contain("无效或已过期");
    }

    [Fact]
    public async Task Execute_NeedsConfirmation_ShouldStayPlain_WhenSecretNotConfigured()
    {
        var binding = CreateBinding(ConfirmingAuthorizer().Object, secretProvider: null);
        var result = await binding.ExecuteAsync(
            Definition(), Args(("text", "hello")), new FeishuToolContext("appA"), CancellationTokenOnly);

        var text = result.ToString()!;
        text.Should().Contain("(needs_confirmation)");
        text.Should().NotContain("确认令牌:", "密钥未配置 = 能力降级，不得签发令牌（与既有行为一致）");
    }

    private static Task<FeishuToolResult> CancellationTokenOnly(CancellationToken _)
        => Task.FromResult(FeishuToolResult.FromText("ok"));

    private sealed class DisposableAction(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
