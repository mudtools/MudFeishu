// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.AI.Knowledge;

namespace Mud.Feishu.AI.Tools.Tests.Knowledge;

/// <summary>
/// JSON 感知截断测试（AI-FD-D12 P1D-2a）与 RAG 引用回链投影测试（P2D-4c）。
/// </summary>
public class AilyKnowledgeSourceTests
{
    // ──────────────── P1D-2a TruncateJson ────────────────

    [Fact]
    public void TruncateJson_ShouldDropTrailingItems_AndKeepValidJson()
    {
        var items = Enumerable.Range(0, 50)
            .Select(i => new JsonObject { ["id"] = i.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        var envelope = new JsonObject { ["items"] = new JsonArray(items.Cast<JsonNode>().ToArray()) };
        var json = envelope.ToJsonString();

        var truncated = ToolResultText.TruncateJson(json, 400);

        var act = () => JsonDocument.Parse(truncated);
        act.Should().NotThrow("截断不落在 JSON 结构中间");
        using var document = JsonDocument.Parse(truncated);
        document.RootElement.GetProperty("items").GetArrayLength().Should()
            .BeGreaterThan(0).And.BeLessThan(50, "逐条删除尾部条目直至长度达标");
        document.RootElement.GetProperty("truncated").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("hint").GetString().Should().Contain("page_token");
    }

    /// <summary>
    /// <b>两个大数组的信封也必须被压到上限内</b>——只剪第一个数组会让整体照样超标，
    /// 而 <c>truncated</c> 标记还会谎报"已经处理过了"。
    /// </summary>
    /// <remarks>
    /// 复现源：<c>docx.import_markdown</c> 的投影同时返回 <c>first_level_block_ids</c> 与
    /// <c>blocks</c>，二者同长（都随 markdown 规模增长）。首版只处理首个数组，
    /// 于是"截断"后的结果体量仍与输入同量级。
    /// </remarks>
    [Fact]
    public void TruncateJson_ShouldBoundAllArrays_NotJustTheFirst()
    {
        var ids = new JsonArray([.. Enumerable.Range(0, 300)
            .Select(i => (JsonNode?)$"blk{i:0000}")]);
        var blocks = new JsonArray([.. Enumerable.Range(0, 300)
            .Select(i => (JsonNode?)new JsonObject { ["block_type"] = "文本段落", ["text"] = $"第 {i} 段" })]);
        var json = new JsonObject
        {
            ["first_level_block_ids"] = ids,
            ["blocks"] = blocks,
        }.ToJsonString();

        var truncated = ToolResultText.TruncateJson(json, 500);

        truncated.Length.Should().BeLessThan(2000,
            "两个大数组都必须被剪，否则'已截断'只是标记而没有效果");
        using var document = JsonDocument.Parse(truncated);
        document.RootElement.GetProperty("first_level_block_ids").GetArrayLength().Should()
            .BeGreaterThan(0).And.BeLessThan(300, "第一个数组同样要剪（保底留 1 条）");
        document.RootElement.GetProperty("blocks").GetArrayLength().Should()
            .BeGreaterThan(0).And.BeLessThan(300, "第二个数组不能被漏掉——这正是本用例的存在理由");
    }

    [Fact]
    public void TruncateJson_ShouldFallbackToCharTruncate_ForPlainText()
    {
        var text = new string('文', 5000);

        var truncated = ToolResultText.TruncateJson(text, 1000);

        truncated.Should().Contain(ToolResultText.TruncatedMarker, "纯文本退回字符截断（既有行为）");
    }

    [Fact]
    public void TruncateJson_ShouldReturnOriginal_WhenWithinLimit()
    {
        var json = new JsonObject { ["items"] = new JsonArray("a", "b") }.ToJsonString();

        ToolResultText.TruncateJson(json, 5000).Should().Be(json, "未超限原样返回");
    }

    // ──────────────── P2D-4c 引用回链投影 ────────────────

    [Fact]
    public void KnowledgeAnswer_Sources_ShouldProjectNumberedSources()
    {
        var answer = new KnowledgeAnswer(
            "问题",
            "答案",
            HasAnswer: true,
            Chunks: new[]
            {
                new RetrievedChunk("切片一", Source: "aily:data-knowledge:asset-1"),
                new RetrievedChunk("切片二"),
                new RetrievedChunk("切片三", Source: "aily:faq"),
            });

        answer.Sources.Should().Equal(
            ["[1] aily:data-knowledge:asset-1", "[3] aily:faq"],
            "编号尾注只含带来源的切片（格式 [n] 来源）");
    }

    [Fact]
    public void KnowledgeAnswer_Sources_ShouldBeEmpty_WhenNoSource()
    {
        var answer = new KnowledgeAnswer("问题", null, false, [new RetrievedChunk("无来源切片")]);
        answer.Sources.Should().BeEmpty("RAG-B 强回链为 Phase 3 契约位，本版无来源即空");
    }
}
