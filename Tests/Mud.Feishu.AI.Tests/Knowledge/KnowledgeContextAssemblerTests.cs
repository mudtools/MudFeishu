// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using Mud.Feishu.AI.Knowledge;

namespace Mud.Feishu.AI.Tests.Knowledge;

/// <summary>
/// 知识注入装配器的预算闸与引用回链映射（R3-8）。
/// </summary>
/// <remarks>
/// <b>为什么这三条必须都在</b>：R3-8 引入的是三级闸（条数 8 / 切片总长 3000 / 单条 500）+
/// 编号↔来源映射。三级闸若只有一级被用例覆盖，另两级就是"写了但从未验证是否能触发"的死闸——
/// 而最初方案里 8×500 = 4000 &lt; 6000 正是这种情况（总预算闸永不触发）。
/// </remarks>
public class KnowledgeContextAssemblerTests
{
    private static ConversationRequest Request(string? mentionedText = "问题")
        => new("app-a", ConversationScope.Group(), "oc_1", "ou_1", "om_1", MentionedText: mentionedText);

    private static KnowledgeContextAssembler Assembler(params RetrievedChunk[] chunks)
    {
        var retriever = new Mock<IRetriever>();
        retriever
            .Setup(r => r.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);
        return new KnowledgeContextAssembler(retriever.Object);
    }

    /// <summary>
    /// 条数闸 + 编号↔来源映射：20 条短切片只注入前 8 条，且 <c>[n]</c> 与来源成对。
    /// </summary>
    [Fact]
    public async Task Assemble_ShouldBoundInjectedChunks_AndKeepCitationMapping()
    {
        var chunks = Enumerable.Range(0, 20)
            .Select(i => new RetrievedChunk($"切片{i}", Source: $"doc-{i}"))
            .ToArray();

        var fragment = await Assembler(chunks).AssembleAsync(Request());

        fragment.Should().NotBeNull();
        var citations = Regex.Matches(fragment!, @"^\[(\d+)\]", RegexOptions.Multiline);
        citations.Count.Should().Be(8, "条数闸：单次注入 ≤ 8 条（R3-8）");
        citations.Select(m => m.Groups[1].Value).Should().Equal(["1", "2", "3", "4", "5", "6", "7", "8"],
            "编号必须连续——模型按 [n] 引用时宿主可按下标回链");
        fragment!.Should().Contain("[1] source=doc-0 切片0", "[1] 必须对应第一条召回的来源（R3-8 引用回链）");
        fragment!.Should().Contain("[8] source=doc-7 切片7");
    }

    /// <summary>
    /// 总长度闸：长切片（每条 500 字）× 8 条时，切片文本总量必须 ≤ 3000 字符
    /// （条数闸不再是约束项，撞的是总预算闸）。
    /// </summary>
    [Fact]
    public async Task Assemble_ShouldBoundInjectedTotalLength_WhenChunksAreLong()
    {
        var chunks = Enumerable.Range(0, 8)
            .Select(i => new RetrievedChunk(new string('长', 500), Source: $"doc-{i}"))
            .ToArray();

        var fragment = await Assembler(chunks).AssembleAsync(Request());

        fragment.Should().NotBeNull();

        // 抽取每条已注入的切片正文（`[n] source=<src> <text>` 的行尾部分）并求和。
        var injectedTextLength = 0;
        foreach (var line in fragment!.Split('\n'))
        {
            var match = Regex.Match(line, @"^\[\d+\] source=\S+ (?<text>.*)$");
            if (match.Success)
            {
                injectedTextLength += match.Groups["text"].Value.TrimEnd('\r').Length;
            }
        }

        injectedTextLength.Should().BeLessThanOrEqualTo(3000 + 8,
            "切片文本总量必须落在总预算内（+8 为单条截断追加的省略号；R3-8 总长度闸必须真正可触发）");
        Regex.Matches(fragment!, @"^\[\d+\] source=doc-", RegexOptions.Multiline).Count.Should().BeLessThan(
            8, "总预算先于条数闸耗尽 ⇒ 第 8 条不应被注入（证明总预算闸可达，而非 8×500=4000 的死闸）");
    }

    /// <summary>
    /// 单条闸：一条超长切片被截断到 500 字以内，且不吞掉同批的后续合法切片。
    /// </summary>
    [Fact]
    public async Task Assemble_ShouldTruncateEachChunk_WithoutSwallowingOthers()
    {
        var fragment = await Assembler(
            new RetrievedChunk(new string('长', 2000), Source: "doc-big"),
            new RetrievedChunk("短切片", Source: "doc-small"))
            .AssembleAsync(Request());

        fragment.Should().NotBeNull();
        fragment!.Should().Contain("[1] source=doc-big ");
        fragment!.Should().Contain("…", "超长单条按 500 字截断（单条闸）");
        fragment!.Should().Contain("[2] source=doc-small 短切片",
            "超长切片不得吞掉同批的后续切片");
    }

    /// <summary>空白切片被跳过（不占用编号），全空白时返回 null（对本次事件无贡献）。</summary>
    [Fact]
    public async Task Assemble_ShouldSkipBlankChunks_AndReturnNullWhenNothingUsable()
    {
        var fragment = await Assembler(
            new RetrievedChunk("   ", Source: "doc-blank"),
            new RetrievedChunk("有效切片", Source: "doc-1"))
            .AssembleAsync(Request());

        fragment.Should().NotBeNull();
        fragment!.Should().Contain("[1] source=doc-1 有效切片", "空白切片不得占用编号（编号必须指向可回链的内容）");
        fragment!.Should().NotContain("doc-blank");

        (await Assembler(new RetrievedChunk("  ", Source: "doc-x")).AssembleAsync(Request()))
            .Should().BeNull("召回切片全不可用时不得注入只含页眉页脚的空节");
    }
}