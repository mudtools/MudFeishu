// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tests.Agents;

/// <summary>
/// 中立装配工厂的三件套契约：<c>Name</c> / <c>ChatOptions{Instructions, Tools}</c> /
/// <c>ChatHistoryProvider</c>（状态键 + 裁剪窗）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么在这里断言而不是只靠产品用例</b>：装配三件套的构造已从产品类搬进中立工厂，
/// 产品用例只能证明"最终送给模型的东西对"，证明不了"工厂本身按参数装配"——
/// 工厂若忽略某个形参（例如把裁剪窗写死），产品侧只要传入相同取值就照样绿。
/// </para>
/// <para>
/// <b>形态</b>：一律<b>行为断言</b>（驱动 mock <see cref="IChatClient"/> 观察实际入参），
/// 不读源码文本——装配是运行期构造，源码断言在格式调整下会假红。
/// </para>
/// </remarks>
public class AgentAssemblyFactoryTests
{
    private const string HistoryStateKey = "test.history";

    /// <summary>Agent 名与已组装指令必须原样进入 <see cref="ChatClientAgentOptions"/>。</summary>
    [Fact]
    public async Task Create_ShouldApplyNameAndInstructions()
    {
        var mock = NewClient();
        var agent = AgentAssemblyFactory.Create(
            mock.Object, "装配名", "宿主指令", tools: null, HistoryStateKey, maxHistoryMessages: 8);

        agent.Name.Should().Be("装配名", "Agent 名是装配三件套之一（宿主可见的身份）");

        var session = await agent.CreateSessionAsync();
        await agent.RunAsync("查询", session);

        mock.Verify(
            c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.Is<ChatOptions?>(o => o != null && o.Instructions == "宿主指令"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 工具集合原样进入 <see cref="ChatOptions.Tools"/>；<b>空集与 <see langword="null"/> 同义</b>
    /// （不设置 Tools ⇒ 保持裸模型行为，而不是给模型一个空的工具列表）。
    /// </summary>
    [Fact]
    public async Task Create_ShouldApplyToolsAndTreatEmptyListAsAbsent()
    {
        var withTools = await CaptureOptionsAsync([AIFunctionFactory.Create(() => "pong", "ping")]);
        withTools!.Tools.Should().NotBeNull().And.HaveCount(1, "传入的工具必须进入 ChatOptions.Tools");

        var emptyList = await CaptureOptionsAsync([]);
        emptyList!.Tools.Should().BeNull("空集与 null 同义：不设 Tools 而非给出空列表（裸模型行为）");

        var nullTools = await CaptureOptionsAsync(null);
        nullTools!.Tools.Should().BeNull();
    }

    /// <summary>
    /// 历史写入必须落在<b>传入的状态键</b>下，且裁剪窗上限由参数真实消费——
    /// 小窗（2）与大窗（20）各跑 5 轮，小窗历史必须显著更短（窗口写死即失败）。
    /// </summary>
    /// <remarks>
    /// 断言用<b>相对比较</b>而非绝对条数：绝对条数取决于上游 reducer 的剔除细节
    /// （工具消息剔除、首条 system 保留），钉死数字会把用例变成"上游实现的复述"。
    /// </remarks>
    [Fact]
    public async Task Create_ShouldWriteHistoryUnderProvidedStateKey_AndHonorWindow()
    {
        var small = await RunTurnsAndCountHistoryAsync(maxHistoryMessages: 2);
        var large = await RunTurnsAndCountHistoryAsync(maxHistoryMessages: 20);

        small.Should().BeLessThan(large,
            "裁剪窗上限必须由参数真实消费——写死窗口会让小窗与大窗的历史一样长（长会话无界增长）");
        small.Should().BeLessThan(10, "5 轮共写入 10 条；小窗下必须真的被裁掉");

        // 键的归属：换一个键必须仍能读到该键下的历史（若工厂忽略形参而用固定键，这里读不到 ⇒ 报红）。
        var otherKey = await RunTurnsAndCountHistoryAsync(maxHistoryMessages: 8, stateKey: "other.key");
        otherKey.Should().BePositive("状态键形参必须被真实消费：显式键下应能读到历史");
    }

    // ───────────────────── 辅助 ─────────────────────

    private static Mock<IChatClient> NewClient()
    {
        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        return mock;
    }

    /// <summary>驱动一次运行并返回上游模型客户端实际收到的 <see cref="ChatOptions"/>。</summary>
    private static async Task<ChatOptions?> CaptureOptionsAsync(IReadOnlyList<AIFunction>? tools)
    {
        ChatOptions? captured = null;
        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((_, options, _) => captured = options)
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));

        var agent = AgentAssemblyFactory.Create(
            mock.Object, "装配名", "指令", tools, HistoryStateKey, maxHistoryMessages: 8);
        var session = await agent.CreateSessionAsync();
        await agent.RunAsync("查询", session);

        return captured;
    }

    /// <summary>连续 5 轮对话后，读取指定状态键下的历史条数。</summary>
    private static async Task<int> RunTurnsAndCountHistoryAsync(int maxHistoryMessages, string stateKey = HistoryStateKey)
    {
        var mock = NewClient();
        var agent = AgentAssemblyFactory.Create(
            mock.Object, "装配名", "指令", tools: null, stateKey, maxHistoryMessages);
        var session = await agent.CreateSessionAsync();

        for (var i = 0; i < 5; i++)
        {
            await agent.RunAsync($"第 {i} 轮", session);
        }

        session.TryGetInMemoryChatHistory(out var history, stateKey, null)
            .Should().BeTrue("历史必须写在该状态键下（由工厂注入的键决定）");

        return history!.Count;
    }
}
