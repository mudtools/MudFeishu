// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Agent.Context;

namespace Mud.AI.Agent.Tests.Context;

/// <summary>
/// <see cref="ContextAssemblerAIContextProvider"/>（N4 · MAF 桥接产出物）的自有测试：
/// 装配器族 → MAF AIContextProvider 的桥接语义与失败隔离。
/// </summary>
public class ContextAssemblerAIContextProviderTests
{
    /// <summary>装配器替身：记录收到的请求并返回片段。</summary>
    private sealed class StubAssembler(string? fragment, List<string>? trace = null) : IContextAssembler
    {
        public ConversationRequest? LastRequest { get; private set; }

        public int Order => 0;

        public Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            trace?.Add("assemble");
            return Task.FromResult(fragment);
        }
    }

    /// <summary>抛错替身：验证隔离。</summary>
    private sealed class ThrowingAssembler : IContextAssembler
    {
        public int Order => 0;

        public Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("装配失败");
    }

    /// <summary>最小 AIAgent 替身（InvokingContext 构造需要非空 agent）。</summary>
    private sealed class TestAgent : AIAgent
    {
        protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
            => new(new EmptySession());

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
            => new(JsonDocument.Parse("{}").RootElement.Clone());

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedState, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
            => new(new EmptySession());

        protected override Task<AgentResponse> RunCoreAsync(
            IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("桥接测试不调用模型");

        protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("桥接测试不调用模型");
    }

    private sealed class EmptySession : AgentSession
    {
    }

    private static AIContextProvider.InvokingContext NewContext(string userText)
    {
        // MAAI001：InvokingContext 公开构造在 MAF 1.20.0 标注实验面——测试替身构造上下文是
        // 该构造的正当用途（产品面不使用；NeutralPackages_ShouldNotReferenceMafExperimentalApis
        // 禁的是 Compaction 族，非此处）。
#pragma warning disable MAAI001
        return new(
            new TestAgent(),
            new EmptySession(),
            new AIContext { Messages = [new ChatMessage(ChatRole.User, userText)] });
#pragma warning restore MAAI001
    }

    [Fact]
    public async Task InvokingAsync_ShouldProjectLastUserMessage_AndJoinFragments()
    {
        ConversationRequest? seen = null;
        var assembler = new StubAssembler("片段A");
        var provider = new ContextAssemblerAIContextProvider(
            [assembler],
            requestFactory: message =>
            {
                seen = new ConversationRequest(
                    AppKey: "t1",
                    Scope: ConversationScope.Group(),
                    SubjectId: "chat-1",
                    SenderId: "user-1",
                    MessageId: "m1",
                    MentionedText: message?.Text);
                return seen;
            });

        var context = await provider.InvokingAsync(NewContext("用户提问"));

        context.Should().NotBeNull("有片段产出 ⇒ 附加上下文并入本轮");
        // 基类 InvokingCoreAsync 把调用方消息与附加上下文合并：末条 = 我们的片段。
        context!.Messages.Should().NotBeNull();
        context.Messages!.Last().Text.Should().Contain("片段A");
        seen.Should().NotBeNull();
        seen!.MentionedText.Should().Be("用户提问", "缺省/自定义工厂从本轮输入投影请求");
        assembler.LastRequest.Should().BeSameAs(seen);
    }

    [Fact]
    public async Task InvokingAsync_ShouldIsolateFailingAssembler_AndReturnNull_WhenAllFail()
    {
        var provider = new ContextAssemblerAIContextProvider([new ThrowingAssembler(), new StubAssembler(null)]);

        var context = await provider.InvokingAsync(NewContext("用户提问"));

        context.Should().NotBeNull("基类契约：返回空上下文（零消息 = 无并入）");
        // 合并语义：无附加消息时结果仅含调用方原始消息。
        context!.Messages!.Should().ContainSingle().Which.Text.Should().Be("用户提问");
    }
}
