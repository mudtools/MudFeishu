// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Tools;
using ToolApprovalRequest = Mud.Feishu.AI.Tools.ToolApprovalRequest;
using IFeishuToolApprovalChannel = Mud.Feishu.AI.Tools.IFeishuToolApprovalChannel;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// T4-2（WP4 / D-3）执行链集成：NeedsUserConfirmation 的「签发 → 宿主批准 → 带令牌重试」闭环，
/// 以及降级（未配置密钥/未注册通道）与防滥用（换参数重试、令牌不豁免 Denied）语义。
/// </summary>
/// <remarks>
/// <b>R2-1（P0）改写</b>：确认令牌<b>不再</b>经工具结果回填模型——模型可据此自行带令牌重试并放行写操作。
/// 令牌只经 <see cref="IFeishuToolApprovalChannel"/> 交给宿主；本类既有用例中
/// 「从结果文本正则提取令牌」的写法（等价于把泄漏固化为契约）已全部改为
/// 「从宿主通道捕获令牌」，并新增「模型可见文本不得含令牌」的反向断言。
/// </remarks>
public class ToolConfirmationTokenFlowTests : IDisposable
{
    private const string Secret = "flow-test-host-secret";
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IFeishuAppContextScopeFactory> _scopeFactory = new();

    public void Dispose() => GC.SuppressFinalize(this);

    private FeishuToolBinding CreateBinding(
        IToolExecutionAuthorizer authorizer,
        IToolConfirmationTokenSecretProvider? secretProvider,
        Func<DateTimeOffset>? clock = null,
        IFeishuToolApprovalChannel? approvalChannel = null)
    {
        _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Returns(new DisposableAction(() => { }));

        return new FeishuToolBinding(
            _scopeFactory.Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            authorizer,
            confirmationTokenSecretProvider: secretProvider,
            approvalChannel: approvalChannel,
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

    /// <summary>宿主批准通道桩：捕获提交给宿主的 <see cref="ToolApprovalRequest"/>（令牌的唯一出口）。</summary>
    private sealed class CapturingApprovalChannel(string? approvalId = "approval-1", Exception? failure = null)
        : IFeishuToolApprovalChannel
    {
        internal List<ToolApprovalRequest> Requests { get; } = [];

        internal List<FrameworkToolApprovalRequest> FrameworkRequests { get; } = [];

        public Task<string?> RequestApprovalAsync(ToolApprovalRequest request, CancellationToken cancellationToken = default)
        {
            if (failure is not null)
            {
                throw failure;
            }

            Requests.Add(request);
            return Task.FromResult<string?>(approvalId);
        }

        public Task<string?> RequestFrameworkApprovalAsync(
            FrameworkToolApprovalRequest request, CancellationToken cancellationToken = default)
        {
            if (failure is not null)
            {
                throw failure;
            }

            FrameworkRequests.Add(request);
            return Task.FromResult<string?>(approvalId);
        }
    }

    private static FeishuToolDefinition Definition(string name = "test.tool", FeishuToolHandler? handler = null)
        => new(name, "测试工具", ["test:write"], true, FeishuToolRisk.Write, "tenant",
            handler ?? ((_, _, _) => Task.FromResult(FeishuToolResult.FromText("downstream-result"))));

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    // ===== R2-1：令牌退出模型可见面 =====

    [Fact]
    public async Task Execute_NeedsConfirmation_ShouldNotLeakToken_IntoModelVisibleText()
    {
        var channel = new CapturingApprovalChannel();
        var binding = CreateBinding(ConfirmingAuthorizer().Object, FixedSecret(), approvalChannel: channel);

        var result = await binding.ExecuteAsync(
            Definition(), Args(("text", "hello")), new FeishuToolContext("appA", UserId: "user1"), CancellationTokenOnly);

        var text = result.ToString()!;
        text.Should().Contain("(needs_confirmation)");

        // 反向断言：令牌、令牌前缀、重试参数名均不得出现在模型可见文本中。
        text.Should().NotContain("确认令牌", "令牌不得进入模型上下文（模型可据此自批复写操作）");
        text.Should().NotContain("v1.", "令牌明文（v1.<expiry>.<hmac>）不得出现在模型可见文本中");
        text.Should().NotContain(ToolConfirmationToken.ArgumentName,
            "不得再指示模型以 confirm_token 重试——那等于把批准要素交给模型");

        // 但令牌必须确实被投递给了宿主。
        channel.Requests.Should().HaveCount(1);
        channel.Requests[0].ConfirmationToken.Should().StartWith("v1.");
    }

    [Fact]
    public async Task Execute_NeedsConfirmation_ShouldRequestHostApproval_WhenChannelRegistered()
    {
        var channel = new CapturingApprovalChannel("APPROVAL-42");
        var binding = CreateBinding(ConfirmingAuthorizer().Object, FixedSecret(), approvalChannel: channel);
        var context = new FeishuToolContext("appA", "conv-1", UserId: "user1");

        var result = await binding.ExecuteAsync(Definition(), Args(("text", "hello")), context, CancellationTokenOnly);

        channel.Requests.Should().HaveCount(1, "待确认事件必须提交宿主批准通道");
        var request = channel.Requests[0];
        request.ToolName.Should().Be("test.tool");
        request.AppKey.Should().Be("appA");
        request.UserId.Should().Be("user1");
        request.ConversationKey.Should().Be("conv-1");
        request.Reason.Should().Be("删除操作须用户批准");
        request.RequiredScopes.Should().Contain("test:write");
        request.ArgumentsDigest.Should().NotBeNullOrEmpty("参数摘要与令牌绑定，宿主不得修改参数");
        request.ExpiresAt.Should().Be(Now + ToolConfirmationToken.DefaultLifetime);
        request.ConfirmationToken.Should().StartWith("v1.");

        // 模型可见侧只拿到关联号，拿不到令牌。
        var text = result.ToString()!;
        text.Should().Contain("APPROVAL-42");
        text.Should().NotContain(request.ConfirmationToken);
    }

    [Fact]
    public async Task Execute_ShouldDegradeToPlain_WhenApprovalChannelMissing()
    {
        var downstreamCalls = 0;
        var binding = CreateBinding(ConfirmingAuthorizer().Object, FixedSecret()); // 不注册通道

        var result = await binding.ExecuteAsync(
            Definition(), Args(("text", "hello")), new FeishuToolContext("appA", UserId: "user1"),
            _ =>
            {
                downstreamCalls++;
                return Task.FromResult(FeishuToolResult.FromText("executed"));
            });

        var text = result.ToString()!;
        text.Should().Contain("(needs_confirmation)", "未注册通道 = HITL 降级为纯提示（fail-closed）");
        text.Should().NotContain("确认令牌");
        text.Should().NotContain("v1.");
        downstreamCalls.Should().Be(0, "未获人工批准前绝不触达下游");
    }

    [Fact]
    public async Task Execute_ShouldDegradeToPlain_WhenApprovalChannelThrows()
    {
        var channel = new CapturingApprovalChannel(failure: new InvalidOperationException("审批系统不可用"));
        var binding = CreateBinding(ConfirmingAuthorizer().Object, FixedSecret(), approvalChannel: channel);

        var result = await binding.ExecuteAsync(
            Definition(), Args(("text", "hello")), new FeishuToolContext("appA", UserId: "user1"), CancellationTokenOnly);

        var text = result.ToString()!;
        text.Should().Contain("(needs_confirmation)", "通道失败必须降级为纯提示，不得中断执行链");
        text.Should().NotContain("v1.", "通道失败时更不得把令牌回填模型");
    }

    // ===== 令牌回灌路径（宿主侧闭环） =====

    [Fact]
    public async Task Execute_WithValidConfirmToken_ShouldBypassConfirmBranch_AndExecuteDownstream()
    {
        var channel = new CapturingApprovalChannel();
        var binding = CreateBinding(ConfirmingAuthorizer().Object, FixedSecret(), approvalChannel: channel);

        // 宿主侧取得令牌（模型侧拿不到）。
        await binding.ExecuteAsync(
            Definition(), Args(("text", "hello")), new FeishuToolContext("appA", UserId: "user1"), CancellationTokenOnly);
        var token = channel.Requests[0].ConfirmationToken;
        token.Should().NotBeNullOrEmpty("宿主通道必须收到签发的令牌");

        var downstreamCalls = 0;
        var binding2 = CreateBinding(ConfirmingAuthorizer().Object, FixedSecret(), approvalChannel: channel);
        var retry = await binding2.ExecuteAsync(
            Definition(),
            Args(("text", "hello"), (ToolConfirmationToken.ArgumentName, token)),
            new FeishuToolContext("appA", UserId: "user1"),
            _ =>
            {
                downstreamCalls++;
                return Task.FromResult(FeishuToolResult.FromText("executed"));
            });

        retry.ToString().Should().Be("executed", "宿主回灌的有效令牌 = 用户已批准，跳过 Confirm 分支执行下游");
        downstreamCalls.Should().Be(1);
    }

    [Fact]
    public async Task Execute_WithTokenButChangedArguments_ShouldDenyAgain()
    {
        var channel = new CapturingApprovalChannel();
        var binding = CreateBinding(ConfirmingAuthorizer().Object, FixedSecret(), approvalChannel: channel);

        await binding.ExecuteAsync(
            Definition(), Args(("text", "hello")), new FeishuToolContext("appA", UserId: "user1"), CancellationTokenOnly);
        var token = channel.Requests[0].ConfirmationToken;

        var retry = await binding.ExecuteAsync(
            Definition(), Args(("text", "changed-value"), (ToolConfirmationToken.ArgumentName, token)),
            new FeishuToolContext("appA", UserId: "user1"), CancellationTokenOnly);

        retry.ToString().Should().Contain("(needs_confirmation)", "换参数即失效——批准不得搬运到不同调用");
        retry.ToString().Should().Contain("无效或已过期");
    }

    [Fact]
    public async Task Execute_WithTokenFromOtherApp_ShouldDenyAgain()
    {
        var channel = new CapturingApprovalChannel();
        var binding = CreateBinding(ConfirmingAuthorizer().Object, FixedSecret(), approvalChannel: channel);

        await binding.ExecuteAsync(
            Definition(), Args(("text", "hello")), new FeishuToolContext("appA", UserId: "user1"), CancellationTokenOnly);
        var token = channel.Requests[0].ConfirmationToken;

        // 换应用重试：令牌与 appKey 绑定（TMA2-20 多租户隔离）。
        var retry = await binding.ExecuteAsync(
            Definition(), Args(("text", "hello"), (ToolConfirmationToken.ArgumentName, token)),
            new FeishuToolContext("appB", UserId: "user1"), CancellationTokenOnly);

        retry.ToString().Should().Contain("无效或已过期", "令牌绑定 appKey，跨应用不得复用");
    }

    [Fact]
    public async Task Execute_NeedsConfirmation_ShouldStayPlain_WhenSecretNotConfigured()
    {
        var channel = new CapturingApprovalChannel();
        var binding = CreateBinding(ConfirmingAuthorizer().Object, secretProvider: null, approvalChannel: channel);

        var result = await binding.ExecuteAsync(
            Definition(), Args(("text", "hello")), new FeishuToolContext("appA"), CancellationTokenOnly);

        var text = result.ToString()!;
        text.Should().Contain("(needs_confirmation)");
        text.Should().NotContain("确认令牌:", "密钥未配置 = 能力降级，不得签发令牌（与既有行为一致）");
        channel.Requests.Should().BeEmpty("无密钥时不得向宿主通道提交任何待确认事件");
    }

    private static Task<FeishuToolResult> CancellationTokenOnly(CancellationToken _)
        => Task.FromResult(FeishuToolResult.FromText("ok"));

    private sealed class DisposableAction(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
