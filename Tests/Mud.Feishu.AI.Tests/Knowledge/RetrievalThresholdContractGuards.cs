// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Knowledge;

namespace Mud.Feishu.AI.Tests.Knowledge;

/// <summary>
/// T9 / D9 检索相关性门槛守卫。
/// </summary>
public class RetrievalThresholdContractGuards
{
    /// <summary>
    /// 守卫 ①：null 分数不过滤（无分值时行为与现状一致——A15 零差异）。
    /// </summary>
    /// <remarks>
    /// KnowledgeContextAssembler 的 minScore 默认 null（不过滤），
    /// Score 为 null 的切片保留（Aily 托管问答不返回得分）。
    /// </remarks>
    [Fact]
    public void KnowledgeContextAssembler_DefaultMinScore_ShouldBeNull_NoFiltering()
    {
        // 验证构造器默认 minScore = null（A15 零差异）
        var constructor = typeof(KnowledgeContextAssembler)
            .GetConstructors()
            .First();

        var minScoreParam = constructor.GetParameters()
            .FirstOrDefault(p => p.Name == "minScore");

        minScoreParam.Should().NotBeNull("KnowledgeContextAssembler 必须有 minScore 参数");
        minScoreParam!.DefaultValue.Should().BeNull(
            "minScore 默认为 null（不过滤），A15 零差异");
    }

    /// <summary>
    /// 守卫 ②：RetrievedChunk.Score 字段已存在（M1 修订：不新增字段）。
    /// </summary>
    [Fact]
    public void RetrievedChunk_Score_ShouldExist_AsNullableDouble()
    {
        var scoreProperty = typeof(RetrievedChunk)
            .GetProperty("Score");

        scoreProperty.Should().NotBeNull("RetrievedChunk.Score 字段必须存在（M1 修订：已存在，T9 只需过滤逻辑）");
        scoreProperty!.PropertyType.Should().Be(typeof(double?),
            "Score 是可空 double（Aily 托管问答不返回得分时为 null）");
    }

    /// <summary>
    /// 守卫 ③：minScore 过滤——低于阈值的切片被过滤，高于阈值的切片保留。
    /// </summary>
    [Fact]
    public async Task Assemble_ShouldFilterLowScoreChunks_WhenMinScoreIsSet()
    {
        var chunks = new[]
        {
            new RetrievedChunk("高分切片", Source: "doc-a", Score: 0.95),
            new RetrievedChunk("低分切片", Source: "doc-b", Score: 0.3),
            new RetrievedChunk("中分切片", Source: "doc-c", Score: 0.6)
        };

        var retriever = new Mock<IRetriever>();
        retriever
            .Setup(r => r.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);
        var assembler = new KnowledgeContextAssembler(retriever.Object, minScore: 0.5);

        var request = new ConversationRequest(
            "app-a", ConversationScope.Group(), "oc_1", "ou_1", "om_1", MentionedText: "问题");
        var fragment = await assembler.AssembleAsync(request);

        fragment.Should().NotBeNull();
        fragment!.Should().Contain("高分切片", "0.95 >= 0.5 应保留");
        fragment!.Should().Contain("中分切片", "0.6 >= 0.5 应保留");
        fragment!.Should().NotContain("低分切片", "0.3 < 0.5 应被过滤");
    }

    /// <summary>
    /// 守卫 ④：Score 为 null 的切片在 minScore 过滤下保留（A15 零差异：Aily 托管问答不返回得分）。
    /// </summary>
    [Fact]
    public async Task Assemble_ShouldKeepNullScoreChunks_WhenMinScoreIsSet()
    {
        var chunks = new[]
        {
            new RetrievedChunk("无分切片", Source: "doc-null", Score: null),
            new RetrievedChunk("高分切片", Source: "doc-high", Score: 0.9),
            new RetrievedChunk("低分切片", Source: "doc-low", Score: 0.1)
        };

        var retriever = new Mock<IRetriever>();
        retriever
            .Setup(r => r.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);
        var assembler = new KnowledgeContextAssembler(retriever.Object, minScore: 0.5);

        var request = new ConversationRequest(
            "app-a", ConversationScope.Group(), "oc_1", "ou_1", "om_1", MentionedText: "问题");
        var fragment = await assembler.AssembleAsync(request);

        fragment.Should().NotBeNull();
        fragment!.Should().Contain("无分切片", "Score=null 的切片应保留（Aily 托管问答不返回得分，A15 零差异）");
        fragment!.Should().Contain("高分切片", "0.9 >= 0.5 应保留");
        fragment!.Should().NotContain("低分切片", "0.1 < 0.5 应被过滤");
    }

    /// <summary>
    /// 守卫 ⑤：minScore=null（默认）时所有切片保留，行为与当前完全一致（A15 零差异）。
    /// </summary>
    [Fact]
    public async Task Assemble_ShouldKeepAllChunks_WhenMinScoreIsNull()
    {
        var chunks = new[]
        {
            new RetrievedChunk("低分", Source: "doc-1", Score: 0.01),
            new RetrievedChunk("高分", Source: "doc-2", Score: 0.99),
            new RetrievedChunk("无分", Source: "doc-3", Score: null)
        };

        var retriever = new Mock<IRetriever>();
        retriever
            .Setup(r => r.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);
        var assembler = new KnowledgeContextAssembler(retriever.Object); // minScore = null（默认）

        var request = new ConversationRequest(
            "app-a", ConversationScope.Group(), "oc_1", "ou_1", "om_1", MentionedText: "问题");
        var fragment = await assembler.AssembleAsync(request);

        fragment.Should().NotBeNull();
        fragment!.Should().Contain("低分", "minScore=null 时不过滤任何切片");
        fragment!.Should().Contain("高分", "minScore=null 时不过滤任何切片");
        fragment!.Should().Contain("无分", "minScore=null 时不过滤任何切片");
    }
}
