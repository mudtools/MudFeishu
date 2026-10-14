// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Agent.Context;

namespace Mud.AI.Agent.Tests.Context;

/// <summary>
/// <see cref="RetrievalContextAssembler"/>（N4 · F5 机制）的自有测试：
/// minScore 过滤 / 编号与来源成对回链 / 三级预算（条数闸）。
/// </summary>
/// <remarks>
/// 产品子类（飞书知识库文案注入）的行为覆盖在 AI.Tests 的
/// <c>RetrievalThresholdContractGuards</c> 与 <c>NeutralizationGoldenTests</c>。
/// </remarks>
public class RetrievalContextAssemblerTests
{
    /// <summary>检索替身：固定返回切片，记录查询文本。</summary>
    private sealed class StubRetriever(params RetrievedChunk[] chunks) : IRetriever
    {
        public string? LastQuery { get; private set; }

        public Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string query, CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            return Task.FromResult<IReadOnlyList<RetrievedChunk>>(chunks);
        }
    }

    private static ConversationRequest NewRequest(string query = "怎么配置 Webhook？")
        => new(
            AppKey: "t1",
            Scope: ConversationScope.Group(),
            SubjectId: "chat-1",
            SenderId: "user-1",
            MessageId: "m1",
            MentionedText: query);

    private static RetrievalContextAssembler NewAssembler(
        IRetriever retriever, double? minScore = null)
        => new(retriever, header: "[参考知识]", footer: "（完）", order: 100, minScore: minScore);

    [Fact]
    public async Task AssembleAsync_ShouldRetrieveWithQuery_AndRenderNumberedChunksWithSource()
    {
        var retriever = new StubRetriever(
            new RetrievedChunk("切片一", Source: "doc/a.md"),
            new RetrievedChunk("切片二", Source: "doc/b.md", TitlePath: "指南 > 安装"));

        var fragment = await NewAssembler(retriever).AssembleAsync(NewRequest());

        retriever.LastQuery.Should().Be("怎么配置 Webhook？", "检索查询 = 提及文本");
        fragment.Should().StartWith("[参考知识]", "untrusted 头是硬约束");
        fragment.Should().Contain("[1] source=doc/a.md 切片一", "编号与来源成对输出（模型引用 [n] 可回链）");
        fragment.Should().Contain("[2] source=doc/b.md title=指南 > 安装 切片二", "标题层级路径同属回链信息");
        fragment.Should().EndWith("（完）");
    }

    [Fact]
    public async Task AssembleAsync_ShouldFilterLowScoreChunks_ButKeepUnscored()
    {
        var retriever = new StubRetriever(
            new RetrievedChunk("低分切片", Score: 0.1),
            new RetrievedChunk("无分切片"),
            new RetrievedChunk("高分切片", Score: 0.9));

        var fragment = await NewAssembler(retriever, minScore: 0.5).AssembleAsync(NewRequest());

        fragment.Should().Contain("[1] 无分切片", "Score 为 null 的切片保留（A15 零差异）");
        fragment.Should().Contain("高分切片");
        fragment.Should().NotContain("低分切片", "低于 minScore 的切片被过滤（T9/D9）");
    }

    [Fact]
    public async Task AssembleAsync_ShouldRespectMaxChunksGate_AndReturnNull_WhenNoQuery()
    {
        var chunks = Enumerable.Range(1, RetrievalBudgets.MaxChunks + 3)
            .Select(i => new RetrievedChunk($"切片{i}"))
            .ToArray();
        var retriever = new StubRetriever(chunks);

        var fragment = await NewAssembler(retriever).AssembleAsync(NewRequest());

        fragment.Should().Contain($"切片{RetrievalBudgets.MaxChunks}", "条数闸：恰好收满 MaxChunks 条");
        fragment.Should().NotContain($"切片{RetrievalBudgets.MaxChunks + 1}", "超出条数闸的切片不注入");

        (await NewAssembler(retriever).AssembleAsync(NewRequest(query: "  "))).Should().BeNull(
            "无问题即无检索语义");
    }
}
