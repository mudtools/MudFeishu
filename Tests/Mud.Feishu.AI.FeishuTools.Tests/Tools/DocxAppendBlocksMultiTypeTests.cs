// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Moq;

using Mud.Feishu.AI.FeishuTools.Internal;
using Mud.Feishu.DataModels.Docx;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// R5 / F-5：docx.append_blocks 多块 / 多块型升级的行为断言（F-5 DoD）。
/// </summary>
/// <remarks>
/// 缺陷背景：升级前只追加单个 text 块（参数仅 document_id + block_type + text），
/// 加"标题 + 若干段落"必须调 N 次——慢，且半途失败会留下结构不一致的文档。
/// <para>
/// 为什么断言 [Body] 实参而不只看返回值：本项核心是"多块 + 多类型是否真的下发"。
/// 只断言返回的 block_ids 无法区分"发了 5 个正确的块"与"只发了第 1 个、其余被静默丢弃"
/// ——后者恰是这类批量组装代码最常见的缺陷形态。
/// </para>
/// </remarks>
public class DocxAppendBlocksMultiTypeTests
{
    private static Dictionary<string, object?> Args(params (string Key, object? Value)[] pairs)
        => pairs.ToDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal);

    /// <summary>捕获 CreateBlockAsync 的 [Body] 实参（引用类型，避开闭包按值返回陷阱）。</summary>
    private sealed class BodyCapture
    {
        public CreateBlockRequest? Request { get; set; }
    }

    private static (Mock<IFeishuTenantV1DocxBlocks> Client, BodyCapture Capture) CreateClient()
    {
        var capture = new BodyCapture();
        var client = new Mock<IFeishuTenantV1DocxBlocks>();

        client
            .Setup(c => c.CreateBlockAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateBlockRequest>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CreateBlockRequest, int?, string?, string, CancellationToken>(
                (_, _, request, _, _, _, _) => capture.Request = request)
            .ReturnsAsync(new Mud.Feishu.DataModels.FeishuApiResult<BlockOpResult>
            {
                Code = 0,
                Data = new BlockOpResult(),
            });

        return (client, capture);
    }

    private static DocxWriteTools CreateTools(Mock<IFeishuTenantV1DocxBlocks> client)
        => new(
            new Mock<IFeishuTenantV1Docx>().Object,
            client.Object);

    /// <summary>DoD 核心：一次调用下发 5 种块型，且每种都挂在与其 block_type 匹配的元素属性上。</summary>
    [Fact]
    public async Task AppendBlocks_ShouldSendMultipleBlockTypes_InOneRequest()
    {
        var (client, capture) = CreateClient();
        var tools = CreateTools(client);

        const string blocks = """
            [{"block_type":"heading1","text":"周报"},
             {"block_type":"text","text":"本周进展"},
             {"block_type":"bullet","text":"完成 A"},
             {"block_type":"todo","text":"待办 B"},
             {"block_type":"divider"}]
            """;

        await tools.AppendBlocksAsync(
            Args(("document_id", "doxcn_1"), ("blocks", blocks)),
            CancellationToken.None);

        capture.Request.Should().NotBeNull("CreateBlockAsync 未被调用");
        var sent = capture.Request!.Childrens ?? [];
        sent.Should().HaveCount(5, "5 个块必须一次性下发（不能只取第一个）");

        sent[0].BlockType.Should().Be(BlockTypes.Heading1);
        sent[0].Heading1.Should().NotBeNull("heading1 块的内容必须挂在 heading1 属性");
        sent[0].Text.Should().BeNull("heading1 块不应把内容挂到 text 属性");

        sent[1].BlockType.Should().Be(BlockTypes.Text);
        sent[1].Text.Should().NotBeNull();

        sent[2].BlockType.Should().Be(BlockTypes.Bullet);
        sent[2].Bullet.Should().NotBeNull("bullet 块的内容必须挂在 bullet 属性");

        sent[3].BlockType.Should().Be(BlockTypes.Todo);
        sent[3].Todo.Should().NotBeNull("todo 块的内容必须挂在 todo 属性");

        sent[4].BlockType.Should().Be(BlockTypes.Divider);
        sent[4].Divider.Should().NotBeNull("divider 无内容属性，但 Divider 标记必须置位");
    }

    /// <summary>单块简写仍可用；与 blocks 同时给出时以 blocks 为准。</summary>
    [Fact]
    public async Task SingleBlockShorthand_ShouldStillWork_AndBlocksShouldWin()
    {
        var (client, capture) = CreateClient();
        var tools = CreateTools(client);

        var first = await tools.AppendBlocksAsync(
            Args(("document_id", "doxcn_1"), ("block_type", "Text"), ("text", "单块")),
            CancellationToken.None);

        first.Text.Should().NotContain("[tool_error]", "单块简写必须可用");
        var single = capture.Request!.Childrens ?? [];
        single.Should().ContainSingle();
        single[0].BlockType.Should().Be(BlockTypes.Text);
        single[0].Text.Should().NotBeNull();

        await tools.AppendBlocksAsync(
            Args(
                ("document_id", "doxcn_1"),
                ("block_type", "Text"),
                ("text", "被忽略"),
                ("blocks", "[{\"block_type\":\"heading2\",\"text\":\"生效\"}]")),
            CancellationToken.None);

        var both = capture.Request!.Childrens ?? [];
        both.Should().ContainSingle();
        both[0].BlockType.Should().Be(BlockTypes.Heading2, "同时给出时必须以 blocks 为准");
    }

    /// <summary>非法块型名必须抛错，且文案附合法值清单（F-8 suggestions 来源）。</summary>
    [Fact]
    public async Task UnknownBlockType_ShouldThrowWithLegalValueList()
    {
        var (client, _) = CreateClient();
        var tools = CreateTools(client);

        // ⚠️ ToolExecutor 会把 ArgumentException 转成 FromError 结果（生产行为），
        //   故此处断言**结果文本**而不是"抛异常"。
        var result = await tools.AppendBlocksAsync(
            Args(("document_id", "doxcn_1"), ("blocks", "[{\"block_type\":\"nope\",\"text\":\"x\"}]")),
            CancellationToken.None);

        result.Text.Should().Contain("nope", "错误文案应回显模型给错的名字");
        // 清单给的是**规范小写名**（与平台/模型写法一致）。
        result.Text.Should().Contain("text")
            .And.Contain("heading1")
            .And.Contain("todo")
            .And.Contain("divider");
    }

    /// <summary>缺输入（既无 blocks 也无单块简写）必须抛错并说明两种写法。</summary>
    [Fact]
    public async Task MissingBothForms_ShouldThrowWithBothOptions()
    {
        var (client, _) = CreateClient();
        var tools = CreateTools(client);

        var result = await tools.AppendBlocksAsync(
            Args(("document_id", "doxcn_1")),
            CancellationToken.None);

        result.Text.Should().Contain("block_type").And.Contain("blocks");
    }

    /// <summary>超过 50 个子块必须提前拒绝（平台上限），而不是让平台返回难以理解的错误。</summary>
    [Fact]
    public async Task TooManyBlocks_ShouldBeRejectedEarly()
    {
        var (client, _) = CreateClient();
        var tools = CreateTools(client);

        var many = string.Join(",", Enumerable.Repeat("{\"block_type\":\"text\",\"text\":\"x\"}", 51));
        var result = await tools.AppendBlocksAsync(
            Args(("document_id", "doxcn_1"), ("blocks", $"[{many}]")),
            CancellationToken.None);

        result.Text.Should().Contain("50", "超过平台上限要提前拒绝并说明上限");
    }
}