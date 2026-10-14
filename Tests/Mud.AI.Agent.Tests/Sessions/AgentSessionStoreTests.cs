// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Mud.AI.Abstractions.Conversations;

namespace Mud.AI.Agent.Tests.Sessions;

/// <summary>
/// <see cref="AgentSessionStore"/> 的行为契约：往返 / 坏值自愈（四语义）/ 取消不被吞 / 无存储为空操作。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么这些用例在中性包里而不是 <c>Mud.Feishu.AI.Tests</c>（P1-H3）</b>：
/// 门面是可复用的中立机制（且 P3 会随 <c>Mud.AI.Agent</c> 上游化）——
/// 用例必须跟着实现走，否则上游化即覆盖丢失。产品侧（飞书）的集成用例
/// （经 <c>FeishuAgent</c> 的两个公开方法）仍在 <c>Mud.Feishu.AI.Tests</c> 保留。
/// </para>
/// <para>
/// <b>状态键是参数</b>：本包用例用测试自有键（<c>test.history</c>），
/// 不引用任何产品状态键——那正是 D4「字面量归产品、机制参数化」的可测形态。
/// </para>
/// <para>
/// <b>坏值自愈的四条语义</b>（迁移 / 重构时不得"顺手简化"，每条都有对应用例）：
/// ① 收口式 catch（非枚举白名单）；② 清库用 <see cref="CancellationToken.None"/>；
/// ③ 删除失败不掩盖重建主路径；④ 急切校验落在守护区之内。
/// </para>
/// </remarks>
public class AgentSessionStoreTests
{
    private const string HistoryStateKey = "test.history";

    private static readonly string CorruptedInnerStatePayload =
        "{\"stateBag\":{\"test.history\":{\"messages\":\"oops\"}}}";

    // ───────────────────── ① 键校验（fail-fast） ─────────────────────

    [Fact]
    public void Ctor_ShouldRejectNullCodec()
    {
        var act = () => new AgentSessionStore(null!, store: null, HistoryStateKey);

        act.Should().Throw<ArgumentNullException>().WithParameterName("codec");
    }

    [Theory]
    [InlineData("")]
    public void Ctor_ShouldRejectEmptyHistoryStateKey(string stateKey)
    {
        var act = () => new AgentSessionStore(CreateCodec(), store: null, stateKey);

        act.Should().Throw<ArgumentException>().WithParameterName("historyStateKey");
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldRejectEmptyKey()
    {
        var store = new AgentSessionStore(CreateCodec(), new FakeSessionStore(), HistoryStateKey);

        var act = () => store.GetOrCreateAsync("");

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("key");
    }

    [Fact]
    public async Task SaveAsync_ShouldRejectNullSession()
    {
        var store = new AgentSessionStore(CreateCodec(), new FakeSessionStore(), HistoryStateKey);

        var act = () => store.SaveAsync("k", null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("session");
    }

    // ───────────────────── ② 往返 + 无存储为空操作 ─────────────────────

    [Fact]
    public async Task GetOrCreateAsync_ShouldRoundTripSession_ThroughStore()
    {
        var fake = new FakeSessionStore();
        var codec = CreateCodec();
        var store = new AgentSessionStore(codec, fake, HistoryStateKey);

        var first = await store.GetOrCreateAsync("k1");
        first.SetInMemoryChatHistory(
            [new ChatMessage(ChatRole.User, "第一轮"), new ChatMessage(ChatRole.Assistant, "回复一")],
            HistoryStateKey,
            null);
        await store.SaveAsync("k1", first);

        var second = await store.GetOrCreateAsync("k1");

        second.Should().NotBeSameAs(first, "持久化后读取的是反序列化的新实例");
        second.TryGetInMemoryChatHistory(out var history, HistoryStateKey, null)
            .Should().BeTrue("健康会话的历史必须原样恢复");
        history!.Should().HaveCount(2);
        fake.Peek("k1").Should().NotBeNull("健康载荷不得被误删");
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldNotTouchStore_WhenStoreIsNull()
    {
        var store = new AgentSessionStore(CreateCodec(), store: null, HistoryStateKey);

        var session = await store.GetOrCreateAsync("k1");

        session.Should().NotBeNull("无存储 ⇒ 纯内存会话（不抛、不静默失败）");
    }

    [Fact]
    public async Task SaveAsync_ShouldBeNoOp_WhenStoreIsNull()
    {
        var store = new AgentSessionStore(CreateCodec(), store: null, HistoryStateKey);
        var session = await store.GetOrCreateAsync("k1");

        var act = () => store.SaveAsync("k1", session);

        await act.Should().NotThrowAsync("无存储时持久化为空操作");
    }

    // ───────────────────── ③ 坏值自愈 ─────────────────────

    [Fact]
    public async Task GetOrCreateAsync_ShouldRebuild_WhenStoredPayloadIsNotJson()
    {
        var fake = new FakeSessionStore();
        await fake.SaveAsync("k1", "{not-valid-json");
        var store = new AgentSessionStore(CreateCodec(), fake, HistoryStateKey);

        var session = await store.GetOrCreateAsync("k1");

        session.Should().NotBeNull("损坏载荷按 miss 处理并重建，绝不把异常抛给事件循环");
        fake.Peek("k1").Should().BeNull("坏键必须被删除——否则旧格式 / 坏载荷永远不会自愈");
    }

    /// <summary>
    /// 合法 JSON 但根不是对象（<c>"[]"</c>）：MAF 反序列化抛 <see cref="ArgumentException"/>——
    /// 若 catch 面只覆盖 <see cref="System.Text.Json.JsonException"/>，坏载荷会在每次重投递时再次失败。
    /// </summary>
    [Fact]
    public async Task GetOrCreateAsync_ShouldRebuild_WhenStoredPayloadIsJsonArray()
    {
        var fake = new FakeSessionStore();
        await fake.SaveAsync("k1", "[]");
        var store = new AgentSessionStore(CreateCodec(), fake, HistoryStateKey);

        (await store.GetOrCreateAsync("k1")).Should().NotBeNull();
        fake.Peek("k1").Should().BeNull();
    }

    /// <summary>
    /// <b>收口式 catch 的红转绿回归锁（语义 ①）</b>：载荷 <c>{"stateBag":{"test.history":null}}</c>
    /// 在急切校验中实测抛 <see cref="NullReferenceException"/>（MAF 对 null 状态值未做防御）——
    /// 该类型<b>不在</b>旧白名单（JsonException / ArgumentException / InvalidOperationException /
    /// NotSupportedException）内。故本用例在枚举白名单实现下必然 FAIL（异常逃出守护区），
    /// 在收口实现下 PASS。
    /// </summary>
    [Fact]
    public async Task GetOrCreateAsync_ShouldRebuild_WhenInnerStateIsNull_EvenForNonWhitelistedExceptionType()
    {
        var fake = new FakeSessionStore();
        await fake.SaveAsync("k1", "{\"stateBag\":{\"test.history\":null}}");
        var store = new AgentSessionStore(CreateCodec(), fake, HistoryStateKey);

        (await store.GetOrCreateAsync("k1")).Should().NotBeNull(
            "白名单之外的异常类型同样必须按 miss 处理并重建（收口式 catch）");
        fake.Peek("k1").Should().BeNull("坏值必须被删除，否则会话在其 TTL 内永久毒化");
    }

    /// <summary>外层合法 + 内层损坏（跨版本残留 / 库中被破坏）：同样必须自愈。</summary>
    [Fact]
    public async Task GetOrCreateAsync_ShouldRebuild_WhenInnerStateCorrupted()
    {
        var fake = new FakeSessionStore();
        await fake.SaveAsync("k1", CorruptedInnerStatePayload);
        var store = new AgentSessionStore(CreateCodec(), fake, HistoryStateKey);

        (await store.GetOrCreateAsync("k1")).Should().NotBeNull();
        fake.Peek("k1").Should().BeNull();
    }

    // ───────────────────── ④ 取消与删除失败（语义 ②③） ─────────────────────

    /// <summary>
    /// <b>取消不得被吞（语义 ①后半）</b>：<see cref="OperationCanceledException"/> 必须原样传播，
    /// 不能被 catch 面吞成"重建"——否则调用方取消会被伪装成"会话损坏"。
    /// </summary>
    /// <remarks>
    /// 用 <see cref="OperationCanceledCodecAgent"/> 强制让<b>守护区之内</b>抛出 OCE
    /// （从存储侧造 OCE 只会命中守护区之外的 <c>GetAsync</c>，证明不了 catch 面的过滤形态）。
    /// </remarks>
    [Fact]
    public async Task GetOrCreateAsync_ShouldPropagateCancellation_FromInsideTheGuard()
    {
        var fake = new FakeSessionStore();
        await fake.SaveAsync("k1", "{\"stateBag\":{}}");
        var store = new AgentSessionStore(new OperationCanceledCodecAgent(), fake, HistoryStateKey);

        var act = () => store.GetOrCreateAsync("k1");

        await act.Should().ThrowAsync<OperationCanceledException>(
            "取消必须原样传播；被吞成「重建」等于把调用方取消伪装成会话损坏");
        fake.Peek("k1").Should().NotBeNull("取消路径不得顺手删除载荷（那不是自愈，是数据丢失）");
    }

    /// <summary>
    /// <b>清库用 <see cref="CancellationToken.None"/>（语义 ②）</b>：删除坏值是自愈的必要动作，
    /// 若用调用方 token，调用方一取消就留下坏值（会话在其 TTL 内继续毒化）。
    /// </summary>
    [Fact]
    public async Task GetOrCreateAsync_ShouldDeleteCorruptPayload_WithCancellationTokenNone()
    {
        var fake = new FakeSessionStore();
        await fake.SaveAsync("k1", "[]");
        var store = new AgentSessionStore(CreateCodec(), fake, HistoryStateKey);
        using var cts = new CancellationTokenSource();

        await store.GetOrCreateAsync("k1", cts.Token);

        fake.DeleteCalls.Should().Be(1);
        fake.LastDeleteToken.Should().Be(
            CancellationToken.None,
            "自愈动作不得被调用方取消中断（否则坏值残留）");
    }

    /// <summary>
    /// <b>删除失败不掩盖重建主路径（语义 ③）</b>：删除是尽力而为（坏值还有 TTL 兜底），
    /// 但绝不能因为删除失败就把异常抛给调用方——那会让事件循环被一个"已经能重建"的会话打死。
    /// </summary>
    [Fact]
    public async Task GetOrCreateAsync_ShouldStillRebuild_WhenDeleteFails()
    {
        var fake = new FakeSessionStore { DeleteException = new InvalidOperationException("后端不可用") };
        await fake.SaveAsync("k1", "[]");
        var store = new AgentSessionStore(CreateCodec(), fake, HistoryStateKey);

        var session = await store.GetOrCreateAsync("k1");

        session.Should().NotBeNull("删除失败不得掩盖重建主路径（坏值由 TTL 兜底）");
        fake.DeleteCalls.Should().Be(1, "删除确实被尝试过（不是被静默跳过）");
    }

    // ───────────────────── 测试替身 ─────────────────────

    /// <summary>真实 MAF 编解码器（含 <c>InMemoryChatHistoryProvider</c> ⇒ 状态袋具备惰性反序列化语义）。</summary>
    private static ChatClientAgent CreateCodec()
    {
        var chatClient = new Mock<IChatClient>();
        chatClient.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));

        return new ChatClientAgent(chatClient.Object, new ChatClientAgentOptions
        {
            Name = "codec",
            ChatHistoryProvider = new InMemoryChatHistoryProvider(
                new InMemoryChatHistoryProviderOptions { StateKey = HistoryStateKey }),
        });
    }

    /// <summary>可控的 <see cref="ISessionStore"/> 替身（记录调用与最近一次删除用的取消令牌）。</summary>
    private sealed class FakeSessionStore : ISessionStore
    {
        private readonly Dictionary<string, string> _items = new(StringComparer.Ordinal);

        public int GetCalls { get; private set; }

        public int SaveCalls { get; private set; }

        public int DeleteCalls { get; private set; }

        public CancellationToken LastDeleteToken { get; private set; }

        public Exception? DeleteException { get; set; }

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            GetCalls++;
            return Task.FromResult(_items.TryGetValue(key, out var value) ? value : null);
        }

        public Task SaveAsync(string key, string serializedSession, CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            _items[key] = serializedSession;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            LastDeleteToken = cancellationToken;

            if (DeleteException is not null)
            {
                throw DeleteException;
            }

            _items.Remove(key);
            return Task.CompletedTask;
        }

        /// <summary>直接窥视后端值（断言"坏值是否真的被删除"）。</summary>
        public string? Peek(string key) => _items.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// 反序列化时抛 <see cref="OperationCanceledException"/> 的编码器：
    /// 把 OCE 精确地送进<b>守护区之内</b>，用于验证 catch 面的过滤形态。
    /// </summary>
    private sealed class OperationCanceledCodecAgent : AIAgent
    {
        protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
            => new(new TestSession());

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
            => new(JsonDocument.Parse("{\"stateBag\":{}}").RootElement.Clone());

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedState, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
            => throw new OperationCanceledException("模拟守护区内的取消");

        protected override Task<AgentResponse> RunCoreAsync(
            IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        private sealed class TestSession : AgentSession
        {
        }
    }
}
