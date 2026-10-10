// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.AI.Knowledge;

namespace Mud.Feishu.AI.Tests.Knowledge;

/// <summary>
/// R7 / C5 RAG-B：结构优先切片器（<see cref="DocumentChunker"/>）的行为断言。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么这些用例是"行为"而不是"实现细节"</b>：切片结果会进向量库，缺陷形态是
/// "答案正好落在切口上/章节层级丢了/回链标识丢了"——都只能从<b>输出</b>看出来。
/// </para>
/// <para>
/// <b>确定性要求</b>：同一输入必须产出逐块相同的切片（否则"重建索引"会制造无谓的向量写入抖动）。
/// </remarks>
/// </remarks>
public class DocumentChunkerTests
{
    private static CorpusDocument Document(string text, string? url = "https://wiki/x", string? scope = "co_1")
        => new("wiki_node_1", text, "手册", url, scope);

    private static ChunkingOptions Options(
        int maxChunkLength = 200,
        int overlap = 4,
        int maxTokens = 4000,
        int minChunkLength = 1)
        => new()
        {
            MaxChunkLength = maxChunkLength,
            OverlapLength = overlap,
            MaxTokens = maxTokens,
            MinChunkLength = minChunkLength,
        };

    [Fact]
    public void Chunk_Should_SplitOnBlankLines_AndKeepWholeBlocks()
    {
        // 窗口 12 < 两段之和（10 + 1 + 10）⇒ 必须在空行处断开，且每块保持完整（不被拦腰截断）。
        var text = "第一段内容，长度足够。\n\n第二段内容，长度足够。\n\n第三段内容，长度足够。";

        var chunks = DocumentChunker.Chunk(Document(text), Options(maxChunkLength: 12));

        chunks.Should().HaveCount(3, "空行是块边界，块必须按段落分开而不是糊在一起");
        chunks.Select(c => c.Text).Should().Contain("第一段内容，长度足够。").And.Contain("第三段内容，长度足够。");
        chunks.Should().OnlyContain(c => c.Text.StartsWith("第", StringComparison.Ordinal),
            "结构优先切片下不应出现被拦腰截断的半句");
    }

    [Fact]
    public void Chunk_Should_PreserveHeadingHierarchy_AsTitlePath()
    {
        var text = "# 总览\n\n总述段落。\n\n## 部署\n\n部署段落。\n\n### 环境变量\n\n变量段落。";

        var chunks = DocumentChunker.Chunk(Document(text), Options(maxChunkLength: 8));

        chunks.Should().NotBeEmpty();
        chunks.Select(c => c.TitlePath).Should().Contain("总览 / 部署 / 环境变量");
        chunks.Select(c => c.TitlePath).Should().Contain("总览 / 部署");
        chunks.Should().NotContain(c => !string.IsNullOrEmpty(c.TitlePath) && c.TitlePath!.Contains("总述", StringComparison.Ordinal),
            "标题路径不得混入正文（那是编造的层级）");
    }

    /// <summary>
    /// 进入更浅层级时必须清空更深层——否则 <c>#A / ###B</c> 之后的 <c>##C</c> 会拼出
    /// <c>A / B / C</c> 这种不存在的层级路径（Markdown 语义上是错的）。
    /// </summary>
    [Fact]
    public void Chunk_Should_DropDeeperHeadings_WhenLevelGoesUp()
    {
        var text = "# A\n\n段落甲。\n\n### B\n\n段落乙。\n\n## C\n\n段落丙。";

        var chunks = DocumentChunker.Chunk(Document(text), Options(maxChunkLength: 8));

        chunks.Select(c => c.TitlePath).Should().Contain("A / C").And.NotContain("A / B / C");
    }

    [Fact]
    public void Chunk_Should_SplitOversizedBlock_WithOverlap()
    {
        var text = new string('甲', 500);

        var chunks = DocumentChunker.Chunk(Document(text), Options(maxChunkLength: 100, overlap: 20));

        chunks.Should().HaveCountGreaterThan(1, "超长单块必须按定长窗口切分");
        chunks.Should().OnlyContain(c => c.Text.Length <= 101, "每块长度不得超过窗口（+1 是省略号位）");
        chunks.Select(c => c.Text).Should().NotContain(text, "切分后不应存在原样整块");

        // 重叠语义：相邻块首尾应共享一段文本（避免答案正好落在切口上）。
        chunks.Count.Should().BeGreaterThan(1);
        chunks[0].Text[^20..].Should().Be(chunks[1].Text[..20], "相邻块必须保留重叠区");
    }

    [Fact]
    public void Chunk_Should_CarryBacklinkFields_EveryChunk()
    {
        var chunks = DocumentChunker.Chunk(
            Document("段落甲。\n\n段落乙。", url: "https://wiki/node/1", scope: "co_7"),
            Options(maxChunkLength: 12));

        chunks.Should().NotBeEmpty();
        chunks.Should().OnlyContain(c => c.SourceToken == "wiki_node_1");
        chunks.Should().OnlyContain(c => c.Url == "https://wiki/node/1");
        chunks.Should().OnlyContain(c => c.ScopeKey == "co_7", "范围键必须逐块携带（向量库据此隔离租户）");
        chunks.Select(c => c.Index).Should().BeInAscendingOrder();
    }

    [Fact]
    public void Chunk_Should_RespectTokenBudget_AfterFirstChunk()
    {
        var text = string.Join("\n\n", Enumerable.Range(0, 40).Select(i => $"第{i}段内容，长度足够占位。"));

        var chunks = DocumentChunker.Chunk(Document(text), Options(maxChunkLength: 40, maxTokens: 60));

        chunks.Should().NotBeEmpty("预算必须留至少一块（否则任何文档都索引不出内容）");
        TokenEstimator.Estimate(string.Concat(chunks.Select(c => c.Text)))
            .Should().BeLessThan(200, "超出预算的文档必须在预算处停止（而不是全量索引）");
    }

    [Fact]
    public void Chunk_Should_DropTooShortBlocks()
    {
        var text = "# 标题\n\n短。\n\n这是一段足够长的正文内容，应当被保留下来。";

        var chunks = DocumentChunker.Chunk(Document(text), Options(maxChunkLength: 60, minChunkLength: 10));

        chunks.Should().OnlyContain(c => c.Text.Length >= 10, "短于阈值的块是噪声（标题残留/空行），应丢弃");
    }

    [Fact]
    public void Chunk_Should_ReturnEmpty_ForBlankOrWhitespaceDocument()
    {
        DocumentChunker.Chunk(Document("   \n\t\n")).Should().BeEmpty();
        DocumentChunker.Chunk(Document(string.Empty)).Should().BeEmpty();
    }

    [Fact]
    public void Chunk_Should_Reject_DocumentWithoutSourceToken()
    {
        var act = () => DocumentChunker.Chunk(new CorpusDocument(" ", "正文内容足够长以通过最短块阈值。"), Options());

        act.Should().Throw<ArgumentException>("缺来源标识的切片无法回链 ⇒ 必须拒绝入库");
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-5, 10)]
    [InlineData(100, 100)]
    [InlineData(100, 200)]
    public void Chunk_Should_Reject_InvalidOptions(int maxChunkLength, int overlap)
    {
        var act = () => DocumentChunker.Chunk(Document("正文内容足够长。"), Options(maxChunkLength, overlap));

        act.Should().Throw<ArgumentOutOfRangeException>(
            "非法参数（尤其重叠 ≥ 窗口这类「无法前进」的配置）必须显式抛错，不得静默回落默认值");
    }

    [Fact]
    public void Chunk_Should_BeDeterministic()
    {
        var text = "# 标题\n\n段落甲内容。\n\n## 次级\n\n" + new string('乙', 400) + "\n\n段落丙内容。";
        var options = Options(maxChunkLength: 90, overlap: 15);

        var first = DocumentChunker.Chunk(Document(text), options);
        var second = DocumentChunker.Chunk(Document(text), options);

        second.Select(c => (c.Text, c.TitlePath, c.Index))
            .Should().Equal(first.Select(c => (c.Text, c.TitlePath, c.Index)),
                "同一输入必须产出逐块相同的结果（否则重建索引会造成无谓的向量写入抖动）");
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("abcd", 1)]
    [InlineData("中文字", 3)]
    [InlineData("a中", 2)]
    public void TokenEstimator_Should_CountWideCharsAsOne(string? value, int expected)
        => TokenEstimator.Estimate(value).Should().Be(expected);
}