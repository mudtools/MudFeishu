// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Mud.Feishu.AI.Tests.Agents;

/// <summary>
/// <see cref="FeishuAgent"/> 单轮闭环（Moq <see cref="IChatClient"/>，不真实调模型——Phase 0 §7）。
/// </summary>
public class FeishuAgentTests
{
    private static Mock<IChatClient> CreateMockClient(string replyText)
    {
        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, replyText)));
        return mock;
    }

    private static FeishuAgentOptions ValidOptions() => new()
    {
        Instructions = "你是飞书测试助手",
        MaxHistoryMessages = 10,
    };

    [Fact]
    public async Task RunAsync_ShouldReturnModelText_WhenBareModelCall()
    {
        var mock = CreateMockClient("你好，我是飞书助手");
        var agent = new FeishuAgent(mock.Object, ValidOptions());

        var session = await agent.CreateSessionAsync();
        var response = await agent.RunAsync("你好", session);

        response.Text.Should().Be("你好，我是飞书助手");
        mock.Verify(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldPassInstructionsViaChatOptions()
    {
        var mock = CreateMockClient("ok");
        var agent = new FeishuAgent(mock.Object, ValidOptions());

        var session = await agent.CreateSessionAsync();
        await agent.RunAsync("查询", session);

        mock.Verify(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.Is<ChatOptions?>(o => o != null && o.Instructions == ValidOptions().Instructions),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Ctor_ShouldRejectInvalidOptions_FailFast()
    {
        var mock = CreateMockClient("ok");

        var act = () => new FeishuAgent(mock.Object, new FeishuAgentOptions { Instructions = "" });

        act.Should().Throw<InvalidOperationException>("Phase 0 §8：工厂启动即校验，不透传非法状态");
    }

    [Fact]
    public async Task SessionPersistence_ShouldRoundTripThroughStore()
    {
        var mock = CreateMockClient("ok");
        var store = new MemoryConversationStore();
        var agent = new FeishuAgent(mock.Object, ValidOptions(), conversationStore: store);
        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.P2P(), "ou_1");

        var first = await agent.GetOrCreateSessionAsync(key);
        await agent.RunAsync("第一轮", first);
        await agent.SaveSessionAsync(key, first);

        var second = await agent.GetOrCreateSessionAsync(key);
        second.Should().NotBeSameAs(first, "持久化后读取的是反序列化的新实例");
        second.GetType().Should().Be(first.GetType());
    }

    [Fact]
    public async Task GetOrCreateSession_ShouldRebuild_WhenStoredPayloadCorrupted()
    {
        var mock = CreateMockClient("ok");
        var store = new MemoryConversationStore();
        var agent = new FeishuAgent(mock.Object, ValidOptions(), conversationStore: store);
        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_9");

        await store.SaveAsync(key, "{not-valid-json");

        var session = await agent.GetOrCreateSessionAsync(key);
        session.Should().NotBeNull("损坏载荷按 miss 处理并重建会话（Phase 0 §8）");
    }

    [Fact]
    public async Task SaveSession_ShouldBeNoOp_WithoutStore()
    {
        var mock = CreateMockClient("ok");
        var agent = new FeishuAgent(mock.Object, ValidOptions());
        var session = await agent.CreateSessionAsync();

        var act = () => agent.SaveSessionAsync("feishu:app-a:conversation:chat:oc_1", session);

        await act.Should().NotThrowAsync("无存储时持久化为空操作");
    }

    [Fact]
    public async Task RunStreaming_ShouldYieldUpdates()
    {
        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(StreamChunks("你", "好"));

        var agent = new FeishuAgent(mock.Object, ValidOptions());
        var session = await agent.CreateSessionAsync();

        var collected = new List<string>();
        await foreach (var update in agent.RunStreamingAsync("流式", session))
        {
            collected.Add(update.Text ?? string.Empty);
        }

        string.Concat(collected).Should().Be("你好");
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamChunks(
        params string[] chunks)
    {
        foreach (var chunk in chunks)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
            await Task.Yield();
        }
    }

    [Fact]
    public async Task RunStreaming_ShouldStopActivity_WhenConsumerBreaksEarly()
    {
        // 消费方提前断开时迭代器被 Dispose，using Activity 不得泄漏或抛异常。
        var mock = new Mock<IChatClient>();
        static async IAsyncEnumerable<ChatResponseUpdate> InfiniteStream(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                yield return new ChatResponseUpdate(ChatRole.Assistant, "x");
                await Task.Yield();
            }
        }

        mock.Setup(c => c.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(InfiniteStream());

        var agent = new FeishuAgent(mock.Object, ValidOptions());
        var session = await agent.CreateSessionAsync();

        var act = async () =>
        {
            await foreach (var _ in agent.RunStreamingAsync("流式", session))
            {
                break;
            }
        };

        await act.Should().NotThrowAsync();
    }
}
