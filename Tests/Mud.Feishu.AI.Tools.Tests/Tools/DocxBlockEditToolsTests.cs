// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Moq;

using Mud.Feishu.AI.FeishuTools.Internal;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Docx;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// R5 / F-5（续）：docx.update_blocks / delete_blocks / import_markdown 的行为断言。
/// </summary>
/// <remarks>
/// 断言的落点都是"容易静默出错"的地方：删除区间校验、更新项的 JSON 形状、
/// 以及"只转换不写入"这条语义边界（import_markdown 不得真的改文档）。
/// </remarks>
public class DocxBlockEditToolsTests
{
    private static Dictionary<string, object?> Args(params (string Key, object? Value)[] pairs)
        => pairs.ToDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal);

    private sealed class Capture
    {
        public BatchUpdateBlocksRequest? UpdateRequest { get; set; }

        public string? UpdateDocumentId { get; set; }

        public BatchDeleteBlocksRequest? DeleteRequest { get; set; }

        public string? DeleteParentBlockId { get; set; }

        public ConvertContentRequest? ConvertRequest { get; set; }

        /// <summary>转换返回的块数（用于验证结果体量真的被预算约束）。</summary>
        public int ConvertedBlockCount { get; set; }

        /// <summary>额外补一个**无文本块**（分隔线 / 图片 / 表格在转换结果里就没有 Text）。</summary>
        public bool IncludeTextlessBlock { get; set; }
    }

    private static (Mock<IFeishuTenantV1DocxBlocks> Client, Capture Captured) CreateClient()
    {
        var captured = new Capture();
        var client = new Mock<IFeishuTenantV1DocxBlocks>();

        client
            .Setup(c => c.BatchUpdateBlocksAsync(
                It.IsAny<string>(), It.IsAny<BatchUpdateBlocksRequest>(), It.IsAny<int?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, BatchUpdateBlocksRequest, int?, string?, string, CancellationToken>(
                (docId, request, _, _, _, _) => { captured.UpdateDocumentId = docId; captured.UpdateRequest = request; })
            .ReturnsAsync(new FeishuApiResult<BatchUpdateBlocksResult> { Code = 0, Data = new BatchUpdateBlocksResult() });

        client
            .Setup(c => c.BatchDeleteBlocksAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BatchDeleteBlocksRequest>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, BatchDeleteBlocksRequest, int?, string?, CancellationToken>(
                (_, blockId, request, _, _, _) => { captured.DeleteParentBlockId = blockId; captured.DeleteRequest = request; })
            .ReturnsAsync(new FeishuApiResult<BatchDeleteBlocksResult> { Code = 0, Data = new BatchDeleteBlocksResult() });

        client
            .Setup(c => c.ContentConvertAsync(
                It.IsAny<ConvertContentRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<ConvertContentRequest, string, CancellationToken>((request, _, _) => captured.ConvertRequest = request)
            .ReturnsAsync(() => new FeishuApiResult<ContentConvertResult>
            {
                Code = 0,
                Data = new ContentConvertResult
                {
                    Blocks = captured.IncludeTextlessBlock
                        ?
                        [
                            .. Enumerable.Range(0, captured.ConvertedBlockCount)
                                .Select(i => new Block { BlockType = BlockTypes.Text }),
                            // Text 为 null：分隔线 / 图片 / 表格在转换结果里的真实形态。
                            new Block { BlockType = BlockTypes.Divider },
                        ]
                        : [.. Enumerable.Range(0, captured.ConvertedBlockCount)
                            .Select(i => new Block { BlockType = BlockTypes.Text })],
                    FirstLevelBlockIds = [.. Enumerable.Range(0, captured.ConvertedBlockCount)
                        .Select(i => $"blk{i}")],
                },
            });

        return (client, captured);
    }

    /// <summary>构造执行器；<paramref name="maxResultLength"/> 可调，用于验证截断预算被真正遵守。</summary>
    private static DocxWriteTools CreateTools(Mock<IFeishuTenantV1DocxBlocks> client, int maxResultLength = 4000)
        => new(
            new Mock<IFeishuTenantV1Docx>().Object,
            client.Object,
            Options.Create(new FeishuAgentOptions { MaxToolResultLength = maxResultLength }));

    /// <summary>update_blocks：把 {block_id,text} 正确映射为 UpdateTextElements。</summary>
    [Fact]
    public async Task UpdateBlocks_ShouldMapTextToUpdateTextElements()
    {
        var (client, captured) = CreateClient();
        var tools = CreateTools(client);

        await tools.UpdateBlocksAsync(
            Args(("document_id", "doc1"), ("blocks", "[{\"block_id\":\"b1\",\"text\":\"新文本\"}]")),
            CancellationToken.None);

        captured.UpdateDocumentId.Should().Be("doc1");
        captured.UpdateRequest.Should().NotBeNull("BatchUpdateBlocksAsync 未被调用");
        var request = captured.UpdateRequest!.Requests.Should().ContainSingle().Subject;
        request.BlockId.Should().Be("b1");
        request.UpdateTextElements!.Elements.Should().ContainSingle();
        request.UpdateTextElements.Elements[0].TextRun!.Content.Should().Be("新文本");
    }

    /// <summary>update_blocks：缺 block_id / text / 形状错误必须报错并指明原因（不静默跳过该项）。</summary>
    [Theory]
    [InlineData("[{\"text\":\"只有文本\"}]", "block_id")]
    [InlineData("[{\"block_id\":\"b1\"}]", "text")]
    [InlineData("{\"block_id\":\"b1\"}", "JSON 数组")]
    public async Task UpdateBlocks_ShouldRejectMalformedItems(string blocks, string expectedFragment)
    {
        var (client, _) = CreateClient();
        var tools = CreateTools(client);

        var result = await tools.UpdateBlocksAsync(
            Args(("document_id", "doc1"), ("blocks", blocks)),
            CancellationToken.None);

        result.Text.Should().Contain(expectedFragment,
            "形状错误的更新项必须被提前拒绝并指明原因，否则会静默漏改");
    }

    /// <summary>delete_blocks：合法区间下发给 SDK 的区间必须原样一致，父块缺省为文档根块。</summary>
    [Fact]
    public async Task DeleteBlocks_ShouldForwardExactRange_AndDefaultParentToDocumentId()
    {
        var (client, captured) = CreateClient();
        var tools = CreateTools(client);

        await tools.DeleteBlocksAsync(
            Args(("document_id", "doc1"), ("start_index", 2), ("end_index", 5)),
            CancellationToken.None);

        captured.DeleteRequest.Should().NotBeNull("BatchDeleteBlocksAsync 未被调用");
        captured.DeleteRequest!.StartIndex.Should().Be(2);
        captured.DeleteRequest.EndIndex.Should().Be(5);
        captured.DeleteParentBlockId.Should().Be("doc1", "缺省父块应为文档根块（block_id == document_id）");
    }

    /// <summary>
    /// delete_blocks：<b>非法区间必须提前拒绝</b> —— 删除不可撤销，绝不能把意外区间下发给平台。
    /// </summary>
    [Theory]
    [InlineData(-1, 3)]
    [InlineData(5, 5)]
    [InlineData(5, 2)]
    public async Task DeleteBlocks_ShouldRejectInvalidRange(int start, int end)
    {
        var (client, captured) = CreateClient();
        var tools = CreateTools(client);

        var result = await tools.DeleteBlocksAsync(
            Args(("document_id", "doc1"), ("start_index", start), ("end_index", end)),
            CancellationToken.None);

        result.Text.Should().Contain("index");
        captured.DeleteRequest.Should().BeNull("非法区间不得下发到 SDK（删除不可撤销）");
    }

    /// <summary>
    /// import_markdown：<b>只转换、不写入</b> —— 只调 ContentConvertAsync，
    /// 且不得出现任何写入调用（这是该工具的语义边界）。
    /// </summary>
    [Fact]
    public async Task ImportMarkdown_ShouldOnlyConvert_NotWrite()
    {
        var (client, captured) = CreateClient();
        var tools = CreateTools(client);

        await tools.ImportMarkdownAsync(Args(("markdown", "# 标题")), CancellationToken.None);

        captured.ConvertRequest.Should().NotBeNull("ContentConvertAsync 未被调用");
        captured.ConvertRequest!.ContentType.Should().Be("markdown", "content_type 必须显式声明为 markdown");
        captured.ConvertRequest.Content.Should().Be("# 标题");

        captured.UpdateRequest.Should().BeNull("import_markdown 不得触发写入（更新）");
        captured.DeleteRequest.Should().BeNull("import_markdown 不得触发写入（删除）");
        client.Verify(
            c => c.CreateBlockAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateBlockRequest>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "import_markdown 是只读转换，不得调用 CreateBlockAsync");
    }

    // ────────── docx.replace_document（组合工具 · 安全性不变量） ──────────

    /// <summary>replace_document 的调用记录（顺序是核心不变量）。</summary>
    private sealed class ReplaceCapture
    {
        public List<string> Order { get; } = [];

        public BatchDeleteBlocksRequest? DeleteRequest { get; set; }

        public Block[]? AppendedBlocks { get; set; }

        public string? ClientToken { get; set; }

        /// <summary>让"删除"失败，用于验证部分失败的上报。</summary>
        public bool FailDelete { get; set; }

        /// <summary>让"追加"失败，用于验证"文档未被修改"的上报。</summary>
        public bool FailAppend { get; set; }

        /// <summary>现有子块数（决定删除区间上界）。</summary>
        public int ExistingChildren { get; set; }

        /// <summary>markdown 转换出的块数。</summary>
        public int ConvertedBlocks { get; set; } = 2;
    }

    private static (Mock<IFeishuTenantV1DocxBlocks> Client, ReplaceCapture Captured) CreateReplaceClient()
    {
        var captured = new ReplaceCapture();
        var client = new Mock<IFeishuTenantV1DocxBlocks>();

        client
            .Setup(c => c.ContentConvertAsync(
                It.IsAny<ConvertContentRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<ConvertContentRequest, string, CancellationToken>((_, _, _) => captured.Order.Add("convert"))
            .ReturnsAsync(() =>
            {
                var data = new ContentConvertResult
                {
                    Blocks = [.. Enumerable.Range(0, captured.ConvertedBlocks)
                        .Select(i => new Block { BlockType = BlockTypes.Text })],
                };
                return new FeishuApiResult<ContentConvertResult> { Code = 0, Data = data };
            });

        client
            .Setup(c => c.GetChildrenBlocksPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(),
                It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, int?, int, string?, bool?, string, CancellationToken>(
                (_, _, _, _, _, _, _, _) => captured.Order.Add("count"))
            .ReturnsAsync(() => new FeishuApiPageListResult<Block>
            {
                Code = 0,
                Data = new ApiPageListResult<Block>
                {
                    HasMore = false,
                    Items = [.. Enumerable.Range(0, captured.ExistingChildren)
                        .Select(i => new Block { BlockType = BlockTypes.Text })],
                },
            });

        client
            .Setup(c => c.CreateBlockAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateBlockRequest>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CreateBlockRequest, int?, string?, string, CancellationToken>(
                (_, _, request, _, token, _, _) =>
                {
                    captured.Order.Add("append");
                    captured.AppendedBlocks = request.Childrens;
                    captured.ClientToken = token;
                })
            .ReturnsAsync(() => captured.FailAppend
                ? new FeishuApiResult<BlockOpResult> { Code = 1770001, Msg = "append failed" }
                : new FeishuApiResult<BlockOpResult>
                {
                    Code = 0,
                    Data = new BlockOpResult { Childrens = [new Block { BlockId = "new1" }] },
                });

        client
            .Setup(c => c.BatchDeleteBlocksAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BatchDeleteBlocksRequest>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, BatchDeleteBlocksRequest, int?, string?, CancellationToken>(
                (_, _, request, _, _, _) => { captured.Order.Add("delete"); captured.DeleteRequest = request; })
            .ReturnsAsync(() => captured.FailDelete
                ? new FeishuApiResult<BatchDeleteBlocksResult> { Code = 1770002, Msg = "delete failed" }
                : new FeishuApiResult<BatchDeleteBlocksResult> { Code = 0, Data = new BatchDeleteBlocksResult() });

        return (client, captured);
    }

    /// <summary>
    /// <b>核心安全不变量</b>：必须"<b>先追加、后删除</b>"。
    /// </summary>
    /// <remarks>
    /// 反序（先删后建）会在删除成功而创建失败时留下<b>被清空的文档</b>。
    /// 本用例把顺序本身当作断言对象——它是本工具唯一真正的安全性保证。
    /// </remarks>
    [Fact]
    public async Task ReplaceDocument_ShouldAppendBeforeDeleting()
    {
        var (client, captured) = CreateReplaceClient();
        captured.ExistingChildren = 3;
        var tools = CreateTools(client);

        var result = await tools.ReplaceDocumentAsync(
            Args(("document_id", "doc1"), ("markdown", "# 新内容"), ("idempotency_key", "k1")),
            CancellationToken.None);

        captured.Order.Should().Equal(
            ["convert", "count", "append", "delete"],
            "必须先追加新块再删除旧块：反序会在删除成功而创建失败时清空文档");

        captured.DeleteRequest.Should().NotBeNull();
        captured.DeleteRequest!.StartIndex.Should().Be(0);
        captured.DeleteRequest.EndIndex.Should().Be(
            3, "删除区间上界 = 原有子块数（新块从索引 3 开始，不会被误删）");

        result.Text.Should().Contain("\"deleted\":3");
    }

    /// <summary>
    /// <b>追加失败</b>：必须如实告知"文档未被修改"，让模型可安全重试，且<b>不得</b>触发删除。
    /// </summary>
    [Fact]
    public async Task ReplaceDocument_ShouldReportDocumentUntouched_WhenAppendFails()
    {
        var (client, captured) = CreateReplaceClient();
        captured.ExistingChildren = 3;
        captured.FailAppend = true;
        var tools = CreateTools(client);

        var result = await tools.ReplaceDocumentAsync(
            Args(("document_id", "doc1"), ("markdown", "# 新内容"), ("idempotency_key", "k1")),
            CancellationToken.None);

        captured.Order.Should().NotContain("delete", "追加失败时绝不能继续删除旧内容（那会清空文档）");

        // ⚠️ 断言用**解析后的 JSON 值**而非原始文本：仓内 `JsonNode.ToJsonString()` 走默认
        //    JavaScriptEncoder，会把 CJK 转义成 \uXXXX（全仓既有行为，非本工具特有）。
        //    按语义断言既不依赖转义形态，也更贴近"模型读到什么"。
        using var document = JsonDocument.Parse(result.Text);
        var root = document.RootElement;
        root.GetProperty("partial_failure").GetBoolean().Should().BeTrue();
        root.GetProperty("message").GetString().Should()
            .Contain("未被修改", "必须明确告知文档原封未动，否则模型可能采取更激进的补救动作")
            .And.Contain("可安全重试");
    }

    /// <summary>
    /// <b>删除失败</b>（补偿失败）：必须如实上报"新旧并存且内容未丢失"，并给出精确修复指引。
    /// </summary>
    [Fact]
    public async Task ReplaceDocument_ShouldReportPartialFailureWithRepairHint_WhenDeleteFails()
    {
        var (client, captured) = CreateReplaceClient();
        captured.ExistingChildren = 4;
        captured.FailDelete = true;
        var tools = CreateTools(client);

        var result = await tools.ReplaceDocumentAsync(
            Args(("document_id", "doc1"), ("markdown", "# 新内容"), ("idempotency_key", "k1")),
            CancellationToken.None);

        using var document = JsonDocument.Parse(result.Text);
        var root = document.RootElement;
        root.GetProperty("partial_failure").GetBoolean().Should().BeTrue();
        root.GetProperty("deleted").GetInt32().Should().Be(0, "删除失败时 deleted 必须为 0（不得谎报成功）");

        var message = root.GetProperty("message").GetString()!;
        message.Should().Contain("内容未丢失", "必须明确告知内容未丢失，避免模型误以为已损坏而采取破坏性补救");
        message.Should().Contain("docx.delete_blocks", "必须给出可执行的修复指引");
        message.Should().Contain("end_index=4", "修复指引必须带精确区间");
    }

    /// <summary>markdown 转换不出任何块时必须拒绝执行 —— 否则会删光旧内容而无新内容。</summary>
    [Fact]
    public async Task ReplaceDocument_ShouldRefuse_WhenMarkdownYieldsNoBlocks()
    {
        var (client, captured) = CreateReplaceClient();
        captured.ConvertedBlocks = 0;
        captured.ExistingChildren = 3;
        var tools = CreateTools(client);

        // 注意：走的是"转换成功但零块"这条路径（不是空输入那条），
        // 因为只有这条才验证"拒绝删光旧内容"。
        var result = await tools.ReplaceDocumentAsync(
            Args(("document_id", "doc1"), ("markdown", "（有效但无块的 Markdown）"), ("idempotency_key", "k1")),
            CancellationToken.None);

        captured.Order.Should().Contain("convert").And.NotContain("append").And.NotContain("delete");
        result.Text.Should().Contain("未产生任何块", "必须在转换出零块时拒绝执行，否则会清空文档而无新内容");
    }

    /// <summary><c>idempotency_key</c> 必填：缺失时拒绝，避免重试语义不确定。</summary>
    [Fact]
    public async Task ReplaceDocument_ShouldRequireIdempotencyKey()
    {
        var (client, captured) = CreateReplaceClient();
        var tools = CreateTools(client);

        var result = await tools.ReplaceDocumentAsync(
            Args(("document_id", "doc1"), ("markdown", "# x")),
            CancellationToken.None);

        captured.Order.Should().BeEmpty("缺幂等键时不得发起任何下游调用");
        result.Text.Should().Contain("idempotency_key");
    }

    /// <summary>空文档（无旧块）时不得调用删除（区间 [0,0) 无意义且会被平台拒绝）。</summary>
    [Fact]
    public async Task ReplaceDocument_ShouldSkipDelete_WhenDocumentHasNoChildren()
    {
        var (client, captured) = CreateReplaceClient();
        captured.ExistingChildren = 0;
        var tools = CreateTools(client);

        var result = await tools.ReplaceDocumentAsync(
            Args(("document_id", "doc1"), ("markdown", "# x"), ("idempotency_key", "k1")),
            CancellationToken.None);

        captured.Order.Should().NotContain("delete", "空文档没有旧块可删，不应发起删除");
        result.Text.Should().Contain("\"deleted\":0");
    }

    /// <summary>dry_run 不得发起任何下游调用。</summary>
    [Fact]
    public async Task ReplaceDocument_DryRun_ShouldNotCallDownstream()
    {
        var (client, captured) = CreateReplaceClient();
        var tools = CreateTools(client);

        var result = await tools.ReplaceDocumentAsync(
            Args(("document_id", "doc1"), ("markdown", "# x"), ("idempotency_key", "k1"), ("dry_run", true)),
            CancellationToken.None);

        captured.Order.Should().BeEmpty("dry_run 必须完全不触达下游");
        result.Text.Should().Contain("dry_run");
    }

    /// <summary>
    /// import_markdown：<b>无文本块（分隔线 / 图片 / 表格）不得让工具崩溃</b>。
    /// </summary>
    /// <remarks>
    /// 这不是假想边角：Markdown 里的 <c>---</c>、图片、表格转换后**都没有 Text**，
    /// 属常见路径。首版投影把 <c>null</c> 直接交给 <c>Truncate</c> ⇒
    /// <see cref="NullReferenceException"/> 冒到工具层，**含分隔线或图片的 Markdown 全数转换失败**。
    /// 该缺陷无编译错误、无 golden 漂移，只能由断言锁住。
    /// </remarks>
    [Fact]
    public async Task ImportMarkdown_ShouldNotCrash_WhenBlockHasNoText()
    {
        var (client, captured) = CreateClient();
        captured.ConvertedBlockCount = 2;
        captured.IncludeTextlessBlock = true;
        var tools = CreateTools(client);

        var result = await tools.ImportMarkdownAsync(
            Args(("markdown", "文本\n\n---\n\n更多")), CancellationToken.None);

        using var document = JsonDocument.Parse(result.Text);
        document.RootElement.GetProperty("blocks").EnumerateArray()
            .Should().Contain(
                block => block.GetProperty("text").ValueKind == JsonValueKind.Null,
                "无文本块的 text 必须回 null（而不是抛异常、也不是空字符串）");
    }

    /// <summary>
    /// import_markdown：<b>超长结果必须被预算截断</b>（S-16）。
    /// </summary>
    /// <remarks>
    /// 这条断言的对象是"<b>预算是否被真正遵守</b>"，而不是某个具体投影：
    /// 本类原先因拿不到 <c>MaxToolResultLength</c> 而改用非截断出口，
    /// 于是"结果体积由输入 markdown 决定"⇒ 调用方上下文不受保护。
    /// 该缺陷**没有任何编译错误**，只能靠断言结果长度来锁住。
    /// </remarks>
    [Fact]
    public async Task ImportMarkdown_ShouldTruncateResult_WhenExceedingBudget()
    {
        var (client, captured) = CreateClient();
        captured.ConvertedBlockCount = 500;
        var tools = CreateTools(client, maxResultLength: 200);

        var result = await tools.ImportMarkdownAsync(Args(("markdown", "# 大文档")), CancellationToken.None);

        ToolResultText.IsTruncated(result.Text).Should().BeTrue(
            "超长转换结果必须被截断——否则 docx.import_markdown 会把不受限的上下文交给调用方");
        result.Text.Length.Should().BeLessThan(1000,
            "截断后体量应与预算同量级，而不是与块数同量级");
    }

    /// <summary>预算宽裕时不得出现截断标记（证明截断是"按预算生效"而非无条件发生）。</summary>
    [Fact]
    public async Task ImportMarkdown_ShouldNotTruncate_WhenWithinBudget()
    {
        var (client, captured) = CreateClient();
        captured.ConvertedBlockCount = 3;
        var tools = CreateTools(client, maxResultLength: 4000);

        var result = await tools.ImportMarkdownAsync(Args(("markdown", "# 小文档")), CancellationToken.None);

        ToolResultText.IsTruncated(result.Text).Should().BeFalse("预算内的结果必须原样返回");
        result.Text.Should().Contain("\"blocks\"");
    }

    /// <summary>import_markdown：空输入必须被拒（转换空内容会返回空块集，浪费一次调用）。</summary>
    [Fact]
    public async Task ImportMarkdown_ShouldRejectEmptyInput()
    {
        var (client, captured) = CreateClient();
        var tools = CreateTools(client);

        var result = await tools.ImportMarkdownAsync(Args(("markdown", "   ")), CancellationToken.None);

        result.Text.Should().Contain("markdown");
        captured.ConvertRequest.Should().BeNull("空输入不应发起下游调用");
    }
}
