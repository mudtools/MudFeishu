// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.DataModels.Okr;
using Mud.Feishu.DataModels.OkrV2;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// OKR 域工具（R6 / S2）：<c>okr.*</c> 15 个工具的只读投影、写面 dry-run 与失败边界。
/// </summary>
/// <remarks>
/// <para>
/// <b>本域真正的策展点</b>：<c>content</c> / <c>notes</c> 是飞书 v2 的**富文本块树**
/// （paragraph → elements → text_run / docs_link / mention），而工具把它投影为**纯文本**。
/// 因此这里必须显式锁住三件事：① 文本被正确抽取；② 单条正文按预览长度截断；
/// ③ guide/notes 之外的字段（score/weight/deadline 等）原样透传。
/// </para>
/// <para>
/// <b>写面</b>锁三件事：dry-run 不触下游且不回正文原文；非法比例（score 越界）在**下发前**被拒；
/// 客户端缺席时 fail-fast 且**不抛裸异常**（走结构化错误）。
/// </para>
/// </remarks>
public class OkrToolsTests
{
    private static FeishuAgentOptions AgentOptions() => new() { Instructions = "test" };

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static OkrTools CreateReadTools(
        Mock<Mud.Feishu.IFeishuTenantV2OkrCycle>? cycle = null,
        Mock<Mud.Feishu.IFeishuTenantV2OkrObjective>? objective = null,
        Mock<Mud.Feishu.IFeishuTenantV2OkrKeyResult>? keyResult = null,
        Mock<Mud.Feishu.IFeishuTenantV2OkrProgress>? progress = null,
        Mock<Mud.Feishu.IFeishuTenantV1OkrPeriod>? period = null,
        Mock<Mud.Feishu.IFeishuTenantV2OkrCategory>? category = null)
        => new(
            cycle?.Object, objective?.Object, keyResult?.Object, progress?.Object, period?.Object, category?.Object,
            Options.Create(AgentOptions()));

    private static OkrWriteTools CreateWriteTools(
        Mock<Mud.Feishu.IFeishuTenantV2OkrCycle>? cycle = null,
        Mock<Mud.Feishu.IFeishuTenantV2OkrObjective>? objective = null,
        Mock<Mud.Feishu.IFeishuTenantV2OkrKeyResult>? keyResult = null)
        => new(cycle?.Object, objective?.Object, keyResult?.Object, Options.Create(AgentOptions()));

    // ───────────────────── 只读：下游调用形状 ─────────────────────

    [Fact]
    public async Task ListCycles_ShouldCallDownstream_WithUserIdAndPaging()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV2OkrCycle>();
        client.Setup(c => c.ListCyclesAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ListCyclesResult>
            {
                Code = 0,
                Data = new ListCyclesResult
                {
                    Items = [new Cycle { Id = "cyc_1", CycleStatus = 1, Score = 0.5 }],
                    HasMore = false,
                },
            });

        var result = await CreateReadTools(cycle: client).ListCyclesAsync(
            Args(("user_id", "ou_1")), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        var items = document.RootElement.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("cycle_id").GetString().Should().Be("cyc_1");
        items[0].GetProperty("cycle_status").GetInt32().Should().Be(1);

        client.Verify(
            c => c.ListCyclesAsync("ou_1", 20, null, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "默认每页 20 条（官方上限 100），user_id 必须原样透传");
    }

    [Fact]
    public async Task ListObjectives_ShouldProjectRichTextToPlainText()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV2OkrCycle>();
        client.Setup(c => c.ListCycleObjectivesAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ListCycleObjectivesResult>
            {
                Code = 0,
                Data = new ListCycleObjectivesResult
                {
                    Items =
                    [
                        new Objective
                        {
                            Id = "obj_1",
                            Score = 0.6,
                            Weight = 0.4,
                            Deadline = "1760604634563",
                            Owner = new Owner { UserId = "ou_owner" },
                            // 富文本块树：text_run + mention + docs_link
                            Content = new ContentBlockV2
                            {
                                Blocks =
                                [
                                    new ContentBlockElementV2
                                    {
                                        BlockElementType = "paragraph",
                                        Paragraph = new ContentParagraphV2
                                        {
                                            Elements =
                                            [
                                                new ContentParagraphElementV2
                                                {
                                                    ParagraphElementType = "text_run",
                                                    TextRun = new ContentTextRunV2 { Text = "提升留存率" },
                                                },
                                                new ContentParagraphElementV2
                                                {
                                                    ParagraphElementType = "mention",
                                                    Mention = new ContentMention { UserId = "ou_x" },
                                                },
                                                new ContentParagraphElementV2
                                                {
                                                    ParagraphElementType = "docs_link",
                                                    DocsLink = new ContentDocsLinkV2 { Title = "看板", Url = "open.feishu.cn/x" },
                                                },
                                            ],
                                        },
                                    },
                                ],
                            },
                        },
                    ],
                    HasMore = true,
                    PageToken = "pt_1",
                },
            });

        var result = await CreateReadTools(cycle: client).ListObjectivesAsync(
            Args(("cycle_id", "cyc_1")), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        var root = document.RootElement;
        var objective = root.GetProperty("items")[0];

        objective.GetProperty("content").GetString()
            .Should().Be("提升留存率@ou_x看板(open.feishu.cn/x)",
                "富文本必须投影为纯文本：text_run 取原文、mention 记 @user、docs_link 记 title(url)");
        objective.GetProperty("owner_id").GetString().Should().Be("ou_owner");
        objective.GetProperty("score").GetDouble().Should().Be(0.6);
        objective.GetProperty("weight").GetDouble().Should().Be(0.4);
        objective.GetProperty("deadline").GetString().Should().Be("1760604634563");

        root.GetProperty("has_more").GetBoolean().Should().BeTrue();
        root.GetProperty("page_token").GetString().Should().Be("pt_1");
    }

    [Fact]
    public async Task ListObjectives_ShouldTruncateLongContent_ToPreviewLength()
    {
        var longText = new string('长', PageSizes.MessagePreviewLength + 50);
        var client = new Mock<Mud.Feishu.IFeishuTenantV2OkrCycle>();
        client.Setup(c => c.ListCycleObjectivesAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ListCycleObjectivesResult>
            {
                Code = 0,
                Data = new ListCycleObjectivesResult
                {
                    Items = [new Objective { Id = "obj_1", Content = Text(longText) }],
                },
            });

        var result = await CreateReadTools(cycle: client).ListObjectivesAsync(
            Args(("cycle_id", "cyc_1")), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain(ToolResultText.TruncatedMarker,
            "正文类字段必须预截断（R5/B-3 口径），否则超长目标会挤占上下文");
    }

    [Fact]
    public async Task GetObjective_MissingId_ShouldReturnStructuredError()
    {
        var result = await CreateReadTools(objective: new Mock<Mud.Feishu.IFeishuTenantV2OkrObjective>())
            .GetObjectiveAsync(Args(), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] okr.get_objective");
    }

    [Fact]
    public async Task ReadTool_ClientAbsent_ShouldFailFast_WithActionableError()
    {
        var result = await CreateReadTools().ListPeriodsAsync(Args(), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[tool_error] okr.list_periods");
        text.Should().Contain("IFeishuTenantV1OkrPeriod",
            "客户端缺席必须给出可操作的提示（哪个客户端、宿主该启用什么），而不是裸 NullReferenceException");
    }

    [Fact]
    public async Task ListCategories_InvalidOwnerType_ShouldBeRejectedLocally()
    {
        var result = await CreateReadTools(category: new Mock<Mud.Feishu.IFeishuTenantV2OkrCategory>())
            .ListCategoriesAsync(Args(("owner_type", "team")), CancellationToken.None);

        result.ToString().Should().Contain("owner_type");
    }

    [Fact]
    public async Task ListPeriods_ShouldCallDownstream_WithPageSize()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1OkrPeriod>();
        client.Setup(c => c.ListPeriodsAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ListPeriodsResult>
            {
                Code = 0,
                Data = new ListPeriodsResult
                {
                    Items = [new Period { Id = "p1", ZhName = "2026 上半年", Status = 1 }],
                },
            });

        // R7/B1（S3）：`page_size` 不再是模型可见参数（"页不进、预算进"）——分页尺寸是绑定层常量
        // （OkrTools.DefaultPageSize = 20），模型侧只保留 page_token 与 fetch_all/max_items。
        var result = await CreateReadTools(period: client).ListPeriodsAsync(
            Args(), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        document.RootElement.GetProperty("items")[0].GetProperty("zh_name").GetString().Should().Be("2026 上半年");
        client.Verify(c => c.ListPeriodsAsync(20, null, It.IsAny<CancellationToken>()), Times.Once,
            "下游调用必须使用绑定层固定页大小（page_size=20），而不是模型传值");
    }

    // ───────────────────── 写面：dry-run 与校验 ─────────────────────

    [Fact]
    public async Task DeleteObjective_DryRun_ShouldNotCallDownstream_AndReturnRoute()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV2OkrObjective>();
        var tools = CreateWriteTools(objective: client);

        var result = await tools.DeleteObjectiveAsync(
            Args(("objective_id", "obj_secret"), ("dry_run", true)), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain("DELETE /open-apis/okr/v2/objectives/{objective_id}",
            "预演必须给出将要下发的 method + path 模板");
        text.Should().NotContain("obj_secret", "干跑摘要不回显标识原文（只回长度/模板）");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateObjective_DryRun_ShouldNotCallDownstream_AndNotEchoContent()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV2OkrCycle>();
        var tools = CreateWriteTools(cycle: client);

        var result = await tools.CreateObjectiveAsync(
            Args(("cycle_id", "cyc_1"), ("content", "绝密目标正文"), ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain("POST /open-apis/okr/v2/cycles/{cycle_id}/objectives");
        text.Should().NotContain("绝密目标正文", "dry_run 不得成为回显通道（它与出站净化是两条路）");
        client.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("5")]
    [InlineData("-0.1")]
    [InlineData("abc")]
    public async Task CreateObjective_InvalidScore_ShouldFailBeforeDownstream(string score)
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV2OkrCycle>();
        var tools = CreateWriteTools(cycle: client);

        var result = await tools.CreateObjectiveAsync(
            Args(("cycle_id", "cyc_1"), ("content", "目标"), ("score", score)),
            CancellationToken.None);

        result.ToString().Should().Contain("score",
            "得分是 0~1 的比例：越界/非数值必须在**下发前**拒绝（平台会静默接受或报语焉不详的错）");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateKeyResult_NoUpdatableField_ShouldBeRejected()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV2OkrKeyResult>();
        var tools = CreateWriteTools(keyResult: client);

        var result = await tools.UpdateKeyResultAsync(
            Args(("key_result_id", "kr_1")), CancellationToken.None);

        result.ToString().Should().Contain("至少");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateKeyResult_ShouldWrapPlainTextIntoRichTextBlock()
    {
        CreateKeyResultRequest? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV2OkrKeyResult>();
        client.Setup(c => c.CreateObjectiveKeyResultAsync(
                It.IsAny<string>(), It.IsAny<CreateKeyResultRequest>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CreateKeyResultRequest, string, string, CancellationToken>(
                (_, request, _, _, _) => captured = request)
            .ReturnsAsync(new FeishuApiResult<CreateKeyResultResult>
            {
                Code = 0,
                Data = new CreateKeyResultResult { KeyResultId = "kr_new" },
            });

        var result = await CreateWriteTools(keyResult: client).CreateKeyResultAsync(
            Args(("objective_id", "obj_1"), ("content", "把留存做到 40%")), CancellationToken.None);

        var block = captured!.Content;
        block.Should().NotBeNull("纯文本入参必须由工具层包装为富文本块树");
        block!.Blocks.Should().HaveCount(1);
        block.Blocks![0].BlockElementType.Should().Be("paragraph");
        block.Blocks[0].Paragraph!.Elements![0].TextRun!.Text.Should().Be("把留存做到 40%");

        result.ToString().Should().Contain("kr_new");
    }

    [Fact]
    public async Task WriteTool_ClientAbsent_ShouldFailFast()
    {
        var result = await CreateWriteTools().DeleteKeyResultAsync(
            Args(("key_result_id", "kr_1")), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] okr.delete_key_result");
    }

    // ───────────────────── 投影辅助 ─────────────────────

    /// <summary>构造单段单 run 的富文本块树（与执行器写面同构）。</summary>
    private static ContentBlockV2 Text(string value)
        => new()
        {
            Blocks =
            [
                new ContentBlockElementV2
                {
                    BlockElementType = "paragraph",
                    Paragraph = new ContentParagraphV2
                    {
                        Elements =
                        [
                            new ContentParagraphElementV2
                            {
                                ParagraphElementType = "text_run",
                                TextRun = new ContentTextRunV2 { Text = value },
                            },
                        ],
                    },
                },
            ],
        };
}
