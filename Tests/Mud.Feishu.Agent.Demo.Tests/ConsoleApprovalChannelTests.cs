// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// <see cref="ConsoleApprovalChannel"/> 的 HITL 契约：**登记 + 立即返回**（不阻塞等待人类），
/// 以及挂起登记表的增删查语义。
/// </summary>
public class ConsoleApprovalChannelTests
{
    /// <summary>
    /// 通道必须<b>立即返回</b>并完成登记（ADR-04）。
    /// </summary>
    /// <remarks>
    /// <b>断言方式刻意不用延时上界</b>：仓库既有教训明确禁止"固定 <c>Task.Delay</c> 上界当耗时断言"
    /// （线程池注入延迟会让用例偶发红）。此处用 <c>Task.IsCompleted</c> —— 若实现真的阻塞等待人类，
    /// 该方法返回的任务必然是未完成态，判定确定、零抖动。
    /// </remarks>
    [Fact]
    public async Task RequestFrameworkApprovalAsync_Should_RegisterAnd_ReturnImmediately()
    {
        var (renderer, _) = TestDoubles.CreateRenderer();
        var state = new ConsoleApprovalChannelState();
        var channel = new ConsoleApprovalChannel(state, renderer);
        var request = TestDoubles.CreateApprovalRequest();

        var task = channel.RequestFrameworkApprovalAsync(request);

        task.IsCompleted.Should().BeTrue(
            "通道实现不得阻塞等待人工点按钮（不得 Task.Delay 轮询），必须登记后立即返回");

        var correlationId = await task;

        correlationId.Should().NotBeNullOrWhiteSpace();
        correlationId.Should().Be("demo-approval-001", "关联号面向用户，须短且可复现（便于 /approve 抄写）");
        state.HasPending.Should().BeTrue();
        state.Count.Should().Be(1);
    }

    /// <summary>卡片必须打印关键要素：关联号、工具名、权限点、有效期。</summary>
    [Fact]
    public async Task RequestFrameworkApprovalAsync_Should_PrintApprovalCard()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();
        var channel = new ConsoleApprovalChannel(new ConsoleApprovalChannelState(), renderer);

        _ = await channel.RequestFrameworkApprovalAsync(TestDoubles.CreateApprovalRequest());

        var text = output.ToString();
        text.Should().Contain("demo-approval-001");
        text.Should().Contain("docx.delete_blocks");
        text.Should().Contain("docx:document");
        text.Should().Contain("/approve demo-approval-001");
    }

    /// <summary>同一 <c>RequestId</c> 重复登记只保留首条（框架请求是幂等键）。</summary>
    [Fact]
    public async Task RequestFrameworkApprovalAsync_Should_DeduplicateByRequestId()
    {
        var (renderer, _) = TestDoubles.CreateRenderer();
        var state = new ConsoleApprovalChannelState();
        var channel = new ConsoleApprovalChannel(state, renderer);

        var first = await channel.RequestFrameworkApprovalAsync(TestDoubles.CreateApprovalRequest());
        var second = await channel.RequestFrameworkApprovalAsync(TestDoubles.CreateApprovalRequest());

        first.Should().Be("demo-approval-001");
        second.Should().Be("demo-approval-002");
        state.Count.Should().Be(1, "同一 RequestId 只应登记一条（否则 /approve 后残留孤儿记录）");
    }

    /// <summary>非框架路径（P4-1 后预期不可达）同样登记 + 打印，但必须 Warn 提示预期外路径。</summary>
    [Fact]
    public async Task RequestApprovalAsync_Should_RegisterAndWarn()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();
        var state = new ConsoleApprovalChannelState();
        var channel = new ConsoleApprovalChannel(state, renderer);

        var request = new Mud.Feishu.AI.Tools.ToolApprovalRequest(
            ToolName: "docx.delete_blocks",
            AppKey: DocAgentSettings.DefaultAppKey,
            UserId: DocAgentSettings.DefaultConsoleUserId,
            ConversationKey: "feishu:demo-app:conversation:user:ou_console_demo_user",
            ArgumentsDigest: "document_id=<27 字符>",
            RequiredScopes: ["docx:document"],
            Reason: "测试",
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(10));

        var correlationId = await channel.RequestApprovalAsync(request);

        correlationId.Should().NotBeNullOrWhiteSpace();
        state.Count.Should().Be(1);
        output.ToString().Should().Contain("非框架审批请求");
    }

    /// <summary>挂起登记表：查找 / 摘除 / 清空语义（<c>/abandon</c> 依赖 <c>Clear</c> 的返回值）。</summary>
    [Fact]
    public async Task State_Should_FindRemoveAndClear()
    {
        var (renderer, _) = TestDoubles.CreateRenderer();
        var state = new ConsoleApprovalChannelState();
        var channel = new ConsoleApprovalChannel(state, renderer);

        var correlationId = await channel.RequestFrameworkApprovalAsync(TestDoubles.CreateApprovalRequest());

        state.FindByCorrelationId(correlationId!).Should().NotBeNull();
        state.FindByCorrelationId("demo-approval-999").Should().BeNull();

        state.Snapshot().Should().HaveCount(1);
        state.Snapshot()[0].ExpiresAt.Should().BeAfter(state.Snapshot()[0].CreatedAt);
        state.Snapshot()[0].ExpiresAt.Should().BeCloseTo(
            state.Snapshot()[0].CreatedAt + ConsoleApprovalChannelState.DefaultLifetime,
            TimeSpan.FromSeconds(1));

        state.Remove("ficc_call_1").Should().BeTrue();
        state.HasPending.Should().BeFalse();

        await channel.RequestFrameworkApprovalAsync(TestDoubles.CreateApprovalRequest(requestId: "ficc_call_2"));
        state.Clear().Should().Be(1);
        state.HasPending.Should().BeFalse();
    }

    /// <summary>关联号查找必须精确匹配（前缀相同但不同的关联号不得误命中）。</summary>
    [Fact]
    public async Task State_FindByCorrelationId_Should_BeExactMatch()
    {
        var (renderer, _) = TestDoubles.CreateRenderer();
        var state = new ConsoleApprovalChannelState();
        var channel = new ConsoleApprovalChannel(state, renderer);

        await channel.RequestFrameworkApprovalAsync(TestDoubles.CreateApprovalRequest());

        state.FindByCorrelationId("demo-approval-00").Should().BeNull();
        state.FindByCorrelationId("demo-approval-001").Should().NotBeNull();
    }
}
