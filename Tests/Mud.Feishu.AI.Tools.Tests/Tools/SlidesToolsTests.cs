// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.DataModels.Slides;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// 幻灯片域（<c>slides.*</c>，12 枚：7 只读 + 5 写类）的链路用例。
/// </summary>
/// <remarks>
/// <para>
/// <b>本域值得锁住的非显然事实</b>：① 整副 XML 走窗口、单页 XML 绝不截断——截断页回填编辑会静默丢内容；
/// ② 平台没有「列页面」端点，清单由本地解析带默认命名空间的 SML 得到（按 <c>LocalName</c> 匹配，
/// 全名匹配会一页都找不到）；③ 页图是 Base64 二进制通道：解码、错误体防线与落盘都在执行器里，
/// 结果只回路径（no-bytes-in-context）；④ <c>parts</c> 是 JSON 字符串且 action 为闭集——
/// 交给平台只回数字错误码，模型无从修复。
/// </para>
/// </remarks>
public class SlidesToolsTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1SlidesPresentation> _presentationClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1SlidesSlide> _slideClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1SlidesHistory> _historyClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1SlidesSlideImage> _imageClient = new();
    private readonly RecordingStager _stager = new();

    private SlidesReadTools CreateReadTools()
        => new(
            _presentationClient.Object,
            _slideClient.Object,
            _historyClient.Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }));

    private SlidesWriteTools CreateWriteTools()
        => new(_presentationClient.Object, _slideClient.Object, _historyClient.Object);

    private SlidesImageTools CreateImageTools() => new(_imageClient.Object, _stager);

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    /// <summary>把工具结果文本解析回 JSON 对象（断言字段值而不是字符串包含）。</summary>
    private static JsonObject Payload(ToolResult result)
        => JsonNode.Parse(result.ToString())!.AsObject();

    private static string Body(int length)
    {
        var buffer = new char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = (char)('A' + (i % 26));
        }

        return new string(buffer);
    }

    private static FeishuToolRisk Risk(string risk) => risk switch
    {
        "read" => FeishuToolRisk.Read,
        "write" => FeishuToolRisk.Write,
        _ => FeishuToolRisk.HighRiskWrite,
    };

    // ───────────────────── 契约事实（method / path / 身份 / 风险 / scope） ─────────────────────

    [Theory]
    [InlineData("slides.get_presentation", "GET", "/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}", "read", "slides:presentation:read")]
    [InlineData("slides.list_slides", "GET", "/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}", "read", "slides:presentation:read")]
    [InlineData("slides.get_slide", "GET", "/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/slide", "read", "slides:presentation:read")]
    [InlineData("slides.list_histories", "GET", "/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/histories", "read", "slides:presentation:read")]
    [InlineData("slides.get_revert_status", "GET", "/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/history/revert_status", "read", "slides:presentation:read")]
    [InlineData("slides.list_slide_images", "POST", "/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/slide_images", "read", "slides:presentation:screenshot")]
    [InlineData("slides.render_slide_image", "POST", "/open-apis/slides_ai/v1/slide_image/render", "read", "slides:presentation:screenshot")]
    [InlineData("slides.create_presentation", "POST", "/open-apis/slides_ai/v1/xml_presentations", "write", "slides:presentation:create")]
    [InlineData("slides.add_slide", "POST", "/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/slide", "write", "slides:presentation:update")]
    [InlineData("slides.replace_slide", "POST", "/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/slide/replace", "write", "slides:presentation:update")]
    [InlineData("slides.delete_slide", "DELETE", "/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/slide", "high-risk-write", "slides:presentation:update")]
    [InlineData("slides.revert_history", "POST", "/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/history/revert", "write", "slides:presentation:update")]
    public void Contracts_ShouldCarrySdkMethodRouteRiskAndScopes(
        string toolName, string httpMethod, string route, string risk, string scope)
    {
        var contract = FeishuToolContracts.ByToolName[toolName];

        contract.HttpMethod.Should().Be(httpMethod);
        contract.Route.Should().Be(route);
        contract.Identity.Should().Be("tenant");
        contract.Risk.Should().Be(Risk(risk), "删页命中危险词表，须宿主授权与 affordance 资产");
        contract.IsWrite.Should().Be(risk != "read");
        contract.RequiredScopes.Should().BeEquivalentTo(
            risk == "read" ? new[] { scope } : new[] { scope, "slides:presentation:write_only" },
            "读面只持读权限；写面必须同时持 update/create 与 write_only");
    }

    // ───────────────────── 整副 XML 分窗口 ─────────────────────

    [Fact]
    public async Task GetPresentation_ShouldWindowContent_WithOffset()
    {
        var content = Body(25000);
        _presentationClient
            .Setup(c => c.GetXmlPresentationAsync("S7Y1", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetXmlPresentationResult>
            {
                Code = 0,
                Data = new GetXmlPresentationResult
                {
                    XmlPresentation = new XmlPresentation { PresentationId = "S7Y1", RevisionId = 7, Content = content },
                },
            });

        var payload = Payload(await CreateReadTools().GetPresentationAsync(
            Args(("xml_presentation_id", "S7Y1"), ("content_offset", 10000), ("content_limit", 10000)),
            CancellationToken.None));

        payload["xml"]!.GetValue<string>().Should().Be(content.Substring(10000, 10000),
            "第 2 页内容必须等于总长切片（参数被忽略时这里会错位）");
        payload["xml_total_length"]!.GetValue<int>().Should().Be(25000);
        payload["has_more"]!.GetValue<bool>().Should().BeTrue();
        payload["next_offset"]!.GetValue<int>().Should().Be(20000, "续读游标必须指向下一段起点");
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(null, 0)]
    [InlineData(null, 50001)]
    public async Task GetPresentation_ShouldRejectOutOfRangeWindow_BeforeCallingDownstream(int? offset, int? limit)
    {
        var args = Args(("xml_presentation_id", "S7Y1"));
        if (offset is not null)
        {
            args = Args(("xml_presentation_id", "S7Y1"), ("content_offset", offset));
        }

        if (limit is not null)
        {
            args = Args(("xml_presentation_id", "S7Y1"), ("content_limit", limit));
        }

        var result = await CreateReadTools().GetPresentationAsync(args, CancellationToken.None);

        result.Error!.Subtype.Should().Be("invalid_args");
        _presentationClient.Invocations.Should().BeEmpty("窗口越界在本地拒绝，不消耗一次下游调用");
    }

    [Fact]
    public async Task GetPresentation_ShouldReturnNotFound_WhenContentMissing()
    {
        _presentationClient
            .Setup(c => c.GetXmlPresentationAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetXmlPresentationResult>
            {
                Code = 0,
                Data = new GetXmlPresentationResult { XmlPresentation = new XmlPresentation() },
            });

        var payload = Payload(await CreateReadTools().GetPresentationAsync(
            Args(("xml_presentation_id", "S7Y1")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse("空内容须显式「没找到」，而不是空对象让模型猜");
    }

    // ───────────────────── 页面清单（本地解析 SML） ─────────────────────

    [Fact]
    public async Task ListSlides_ShouldParseNamespacedSml_IntoInventory()
    {
        const string xml =
            "<presentation xmlns=\"http://www.larkoffice.com/sml/2.0\" id=\"p1\">"
            + "<slide id=\"s1\"><shape><content>封面 标题</content></shape></slide>"
            + "<slide id=\"s2\"><content>  第二页\n 内容 </content></slide>"
            + "</presentation>";
        _presentationClient
            .Setup(c => c.GetXmlPresentationAsync("S7Y1", null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetXmlPresentationResult>
            {
                Code = 0,
                Data = new GetXmlPresentationResult
                {
                    XmlPresentation = new XmlPresentation { PresentationId = "S7Y1", RevisionId = 7, Content = xml },
                },
            });

        var payload = Payload(await CreateReadTools().ListSlidesAsync(
            Args(("xml_presentation_id", "S7Y1")), CancellationToken.None));

        payload["slide_count"]!.GetValue<int>().Should().Be(2);
        var slides = payload["slides"]!.AsArray();
        slides[0]!["slide_id"]!.GetValue<string>().Should().Be("s1");
        slides[1]!["text_preview"]!.GetValue<string>().Should().Be("第二页 内容", "空白折叠后才不会让 120 字全被缩进占满");
    }

    [Fact]
    public async Task ListSlides_ShouldReturnNotFound_WhenXmlUnreadable()
    {
        _presentationClient
            .Setup(c => c.GetXmlPresentationAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetXmlPresentationResult>
            {
                Code = 0,
                Data = new GetXmlPresentationResult { XmlPresentation = new XmlPresentation { Content = "<presentation" } },
            });

        var result = await CreateReadTools().ListSlidesAsync(
            Args(("xml_presentation_id", "S7Y1")), CancellationToken.None);

        result.Error!.Subtype.Should().Be("invalid_args");
        result.Text.Should().Contain("无法解析", "坏 XML 须指向 get_presentation 核对原文，而不是回空清单");
    }

    // ───────────────────── 单页 XML ─────────────────────

    [Fact]
    public async Task GetSlide_ShouldSendSlideNumber_WhenGiven()
    {
        string? capturedSlideId = "unset";
        int? capturedSlideNumber = null;
        _slideClient
            .Setup(c => c.GetSlideAsync("S7Y1", It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string? slideId, int? _, int? slideNumber, CancellationToken _) =>
            {
                capturedSlideId = slideId;
                capturedSlideNumber = slideNumber;
            })
            .ReturnsAsync(new FeishuApiResult<GetSlideResult>
            {
                Code = 0,
                Data = new GetSlideResult
                {
                    Slide = new Slide { SlideId = "s2", Content = "<slide id=\"s2\"><content>x</content></slide>" },
                    RevisionId = 7,
                },
            });

        var payload = Payload(await CreateReadTools().GetSlideAsync(
            Args(("xml_presentation_id", "S7Y1"), ("slide_number", 2)), CancellationToken.None));

        capturedSlideId.Should().BeNull("按页码定位时不得同时下发 slide_id");
        capturedSlideNumber.Should().Be(2);
        payload["found"]!.GetValue<bool>().Should().BeTrue();
        payload["xml"]!.GetValue<string>().Should().Contain("<slide", "单页 XML 不截断——它是回填编辑的输入");
    }

    [Theory]
    [InlineData("s1", 1)]
    [InlineData(null, null)]
    public async Task GetSlide_ShouldRejectSelectorCombinations_BeforeCallingDownstream(string? slideId, int? slideNumber)
    {
        var args = Args(("xml_presentation_id", "S7Y1"), ("slide_id", slideId), ("slide_number", slideNumber));

        var result = await CreateReadTools().GetSlideAsync(args, CancellationToken.None);

        result.Error!.Subtype.Should().Be("invalid_args", "slide_id 与 slide_number 必须二选一，同传/都不传都无法定位页面");
        _slideClient.Invocations.Should().BeEmpty();
    }

    // ───────────────────── 历史版本与回滚 ─────────────────────

    [Fact]
    public void ListHistories_ShouldNotExposePageSize()
    {
        FeishuToolSchemas.SchemaByToolName[FeishuToolNames.SlidesListHistories]
            .Should().NotContain("page_size", "页大小是运维参数（DP-B1-0：页不进、预算进），由执行器固定在平台上限");
    }

    [Fact]
    public async Task ListHistories_ShouldProjectRollbackInputs()
    {
        _historyClient
            .Setup(c => c.ListPresentationHistoriesAsync("S7Y1", 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ListPresentationHistoriesResult>
            {
                Code = 0,
                Data = new ListPresentationHistoriesResult
                {
                    Entries =
                    [
                        new PresentationHistoryEntry
                        {
                            HistoryVersionId = "3072",
                            RevisionId = 7,
                            Type = 1,
                            Name = "编辑",
                            EditorIds = ["ou_a"],
                        },
                    ],
                },
            });

        var payload = Payload(await CreateReadTools().ListHistoriesAsync(
            Args(("xml_presentation_id", "S7Y1")), CancellationToken.None));

        var entry = payload["entries"]!.AsArray()[0]!;
        entry["history_version_id"]!.GetValue<string>().Should().Be("3072", "回滚只认 history_version_id，不是 revision_id");
        entry["editor_ids"]!.AsArray().Should().HaveCount(1);
        payload["has_more"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task GetRevertStatus_ShouldSurfacePartialFailure()
    {
        _historyClient
            .Setup(c => c.GetHistoryRevertStatusAsync("S7Y1", "task1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetHistoryRevertStatusResult>
            {
                Code = 0,
                Data = new GetHistoryRevertStatusResult
                {
                    Status = "partial_failed",
                    FailedBlockTokens = ["blk1"],
                    HistoryVersionId = "3072",
                },
            });

        var payload = Payload(await CreateReadTools().GetRevertStatusAsync(
            Args(("xml_presentation_id", "S7Y1"), ("task_id", "task1")), CancellationToken.None));

        payload["status"]!.GetValue<string>().Should().Be("partial_failed");
        payload["failed_block_tokens"]!.AsArray()[0]!.GetValue<string>().Should().Be("blk1");
        payload["note"]!.GetValue<string>().Should().Contain("部分块回滚失败", "部分失败必须如实转达，不能当成功");
    }

    // ───────────────────── 页图：选择器、Base64 与落盘 ─────────────────────

    [Theory]
    [InlineData("s1,s2", 1, null, "二选一")]
    [InlineData("", null, null, "须给出页面选择器")]
    [InlineData("", 0, 1, "从 1 起")]
    [InlineData("", 3, 2, "page_end 须 ≥")]
    [InlineData("", 1, 11, "单次最多导出 10 页")]
    [InlineData("s1,s2,s3,s4,s5,s6,s7,s8,s9,s10,s11", null, null, "最多 10")]
    public void BuildImageRequest_ShouldEnforceSelectorRules(string idsCsv, int? pageStart, int? pageEnd, string message)
    {
        var slideIds = idsCsv.Length == 0 ? null : idsCsv.Split(',');

        var act = () => SlidesImageTools.BuildImageRequest(slideIds, pageStart, pageEnd);

        act.Should().Throw<ArgumentException>().WithMessage($"*{message}*");
    }

    [Fact]
    public async Task ListSlideImages_ShouldRejectBothSelectors_BeforeCallingDownstream()
    {
        var result = await CreateImageTools().ListSlideImagesAsync(
            Args(("xml_presentation_id", "S7Y1"), ("slide_ids", new[] { "s1" }), ("page_start", 1)),
            CancellationToken.None);

        result.Error!.Subtype.Should().Be("invalid_args");
        _imageClient.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task ListSlideImages_ShouldDecodeAndStage_WithoutBytesInPayload()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x01, 0x02, 0x03 };
        var base64 = Convert.ToBase64String(bytes);
        _imageClient
            .Setup(c => c.ListSlideImagesAsync("S7Y1", It.IsAny<ListSlideImagesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ListSlideImagesResult>
            {
                Code = 0,
                Data = new ListSlideImagesResult
                {
                    SlideImages = [new SlideImage { SlideId = "s1", SlideNumber = 1, Format = 2, Data = base64 }],
                    TotalCount = "1",
                    Revision = "7",
                },
            });

        var text = (await CreateImageTools().ListSlideImagesAsync(
            Args(("xml_presentation_id", "S7Y1"), ("slide_ids", new[] { "s1" })), CancellationToken.None)).ToString();
        var payload = JsonNode.Parse(text)!.AsObject();

        payload["count"]!.GetValue<int>().Should().Be(1);
        var file = payload["files"]!.AsArray()[0]!;
        file["local_path"]!.GetValue<string>().Should().EndWith("slide-S7Y1-1.jpg");
        file["content_type"]!.GetValue<string>().Should().Be("image/jpeg");
        file["size"]!.GetValue<int>().Should().Be(bytes.Length);
        text.Should().NotContain(base64, "图片字节不进上下文（no-bytes-in-context）");
        _stager.Sources.Single().Content!.Value.ToArray().Should().Equal(bytes, "落盘的必须是与平台一致的解码字节");
    }

    [Fact]
    public async Task ListSlideImages_ShouldRejectJsonErrorBody_BeforeStaging()
    {
        var errorBody = System.Text.Encoding.UTF8.GetBytes("{\"code\":99991663,\"msg\":\"permission denied\"}");
        _imageClient
            .Setup(c => c.ListSlideImagesAsync(It.IsAny<string>(), It.IsAny<ListSlideImagesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ListSlideImagesResult>
            {
                Code = 0,
                Data = new ListSlideImagesResult
                {
                    SlideImages = [new SlideImage { SlideId = "s1", Format = 1, Data = Convert.ToBase64String(errorBody) }],
                },
            });

        var result = await CreateImageTools().ListSlideImagesAsync(
            Args(("xml_presentation_id", "S7Y1"), ("slide_ids", new[] { "s1" })), CancellationToken.None);

        result.Error!.Subtype.Should().Be("invalid_args");
        _stager.Sources.Should().BeEmpty("错误信封不得落成假 .png");
    }

    [Fact]
    public async Task ListSlideImages_ShouldReturnNotFound_WhenPlatformReturnsNoImages()
    {
        _imageClient
            .Setup(c => c.ListSlideImagesAsync(It.IsAny<string>(), It.IsAny<ListSlideImagesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ListSlideImagesResult>
            {
                Code = 0,
                Data = new ListSlideImagesResult { SlideImages = [] },
            });

        var payload = Payload(await CreateImageTools().ListSlideImagesAsync(
            Args(("xml_presentation_id", "S7Y1"), ("page_start", 1)), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse();
        payload["count"]!.GetValue<int>().Should().Be(0);
    }

    [Fact]
    public async Task RenderSlideImage_ShouldStage_AndStateNoVersionWasCreated()
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        _imageClient
            .Setup(c => c.RenderSlideImageAsync(It.IsAny<RenderSlideImageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<RenderSlideImageResult>
            {
                Code = 0,
                Data = new RenderSlideImageResult
                {
                    SlideImage = new SlideImage { Format = 1, Data = Convert.ToBase64String(bytes) },
                },
            });

        var payload = Payload(await CreateImageTools().RenderSlideImageAsync(
            Args(("content", "<slide><shape><content>x</content></shape></slide>")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeTrue();
        payload["note"]!.GetValue<string>().Should().Contain("未写入任何演示文稿", "渲染不落库、不产生历史版本，须明示");
        payload["file"]!["content_type"]!.GetValue<string>().Should().Be("image/png");
    }

    [Fact]
    public async Task RenderSlideImage_ShouldReturnNotFound_WhenPlatformReturnsNoImage()
    {
        _imageClient
            .Setup(c => c.RenderSlideImageAsync(It.IsAny<RenderSlideImageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<RenderSlideImageResult>
            {
                Code = 0,
                Data = new RenderSlideImageResult { SlideImage = null },
            });

        var payload = Payload(await CreateImageTools().RenderSlideImageAsync(
            Args(("content", "<slide/>")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse();
        payload["message"]!.GetValue<string>().Should().Contain("单页");
    }

    // ───────────────────── 写面：整副新建 / 加页 / 替换 / 删页 / 回滚 ─────────────────────

    [Fact]
    public async Task CreatePresentation_ShouldRejectNonPresentationRoot_BeforeCallingDownstream()
    {
        var result = await CreateWriteTools().CreatePresentationAsync(
            Args(("content", "<slide><content>x</content></slide>")), CancellationToken.None);

        result.Error!.Subtype.Should().Be("invalid_args");
        result.Text.Should().Contain("根元素须是 <presentation>");
        _presentationClient.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePresentation_ShouldSendWholeXml_AndProjectIds()
    {
        const string xml = "<presentation xmlns=\"http://www.larkoffice.com/sml/2.0\"><slide id=\"s1\"/></presentation>";
        CreateXmlPresentationRequest? captured = null;
        _presentationClient
            .Setup(c => c.CreateXmlPresentationAsync(It.IsAny<CreateXmlPresentationRequest>(), It.IsAny<CancellationToken>()))
            .Callback((CreateXmlPresentationRequest request, CancellationToken _) => captured = request)
            .ReturnsAsync(new FeishuApiResult<CreateXmlPresentationResult>
            {
                Code = 0,
                Data = new CreateXmlPresentationResult
                {
                    XmlPresentationId = "S7Y1",
                    SlideIds = ["s1"],
                    RevisionId = 3,
                    Url = "https://example.feishu.cn/slides/S7Y1",
                },
            });

        var payload = Payload(await CreateWriteTools().CreatePresentationAsync(
            Args(("content", xml)), CancellationToken.None));

        captured!.XmlPresentation!.Content.Should().Be(xml);
        payload["created"]!.GetValue<bool>().Should().BeTrue();
        payload["xml_presentation_id"]!.GetValue<string>().Should().Be("S7Y1");
        payload["slide_ids"]!.AsArray().Should().HaveCount(1);
        payload["note"].Should().BeNull("issues 为空时不得凭空造告警");
    }

    [Fact]
    public async Task AddSlide_ShouldNormalizeEmptyBeforeSlideId_ToNull()
    {
        CreateSlideRequest? captured = null;
        _slideClient
            .Setup(c => c.CreateSlideAsync("S7Y1", It.IsAny<CreateSlideRequest>(), null, null, It.IsAny<CancellationToken>()))
            .Callback((string _, CreateSlideRequest request, int? _, string? _, CancellationToken _) => captured = request)
            .ReturnsAsync(new FeishuApiResult<CreateSlideResult>
            {
                Code = 0,
                Data = new CreateSlideResult { SlideId = "s9", RevisionId = 8 },
            });

        var payload = Payload(await CreateWriteTools().AddSlideAsync(
            Args(("xml_presentation_id", "S7Y1"), ("content", "<slide><content>x</content></slide>"), ("before_slide_id", "  ")),
            CancellationToken.None));

        captured!.BeforeSlideId.Should().BeNull("空串与「未给出」平台语义不同：空串=插到未知页面之前");
        payload["added"]!.GetValue<bool>().Should().BeTrue();
        payload["slide_id"]!.GetValue<string>().Should().Be("s9");
        payload["inserted_before"].Should().BeNull();
    }

    [Fact]
    public async Task ReplaceSlide_ShouldSendTrimmedPartsAndComment_AndReportSuccess()
    {
        ReplaceSlideRequest? captured = null;
        _slideClient
            .Setup(c => c.ReplaceSlideAsync("S7Y1", "s1", It.IsAny<ReplaceSlideRequest>(), null, It.IsAny<CancellationToken>()))
            .Callback((string _, string _, ReplaceSlideRequest request, int? _, CancellationToken _) => captured = request)
            .ReturnsAsync(new FeishuApiResult<ReplaceSlideResult>
            {
                Code = 0,
                Data = new ReplaceSlideResult { RevisionId = 9 },
            });

        var payload = Payload(await CreateWriteTools().ReplaceSlideAsync(
            Args(
                ("xml_presentation_id", "S7Y1"),
                ("slide_id", "s1"),
                ("parts", "[{\"action\":\"block_replace\",\"block_id\":\"bab\",\"replacement\":\"<shape/>\"}]"),
                ("comment", " 调整标题 "),
                ("idempotency_key", " idem-1 ")),
            CancellationToken.None));

        captured!.Parts.Should().HaveCount(1);
        captured.Comment.Should().Be("调整标题");
        captured.IdempotencyKey.Should().Be("idem-1");
        payload["replaced"]!.GetValue<bool>().Should().BeTrue();
        payload["part_count"]!.GetValue<int>().Should().Be(1);
        payload["note"].Should().BeNull();
    }

    [Fact]
    public async Task ReplaceSlide_ShouldSurfaceFailedPartIndex()
    {
        _slideClient
            .Setup(c => c.ReplaceSlideAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ReplaceSlideRequest>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ReplaceSlideResult>
            {
                Code = 0,
                Data = new ReplaceSlideResult { RevisionId = 9, FailedPartIndex = 1, FailedReason = "block not found" },
            });

        var payload = Payload(await CreateWriteTools().ReplaceSlideAsync(
            Args(
                ("xml_presentation_id", "S7Y1"),
                ("slide_id", "s1"),
                ("parts", "[{\"action\":\"block_insert\",\"insertion\":\"<shape/>\"}]")),
            CancellationToken.None));

        payload["replaced"]!.GetValue<bool>().Should().BeFalse();
        payload["failed_part_index"]!.GetValue<int>().Should().Be(1);
        payload["note"]!.GetValue<string>().Should().Contain("第 1 项动作失败");
    }

    [Theory]
    [InlineData("[]", "非空")]
    [InlineData("not json", "合法 JSON 数组")]
    [InlineData("[{\"action\":\"replace\",\"block_id\":\"bab\",\"replacement\":\"<shape/>\"}]", "仅支持")]
    [InlineData("[{\"action\":\"block_replace\",\"replacement\":\"<shape/>\"}]", "缺 block_id")]
    [InlineData("[{\"action\":\"block_replace\",\"block_id\":\"bab\"}]", "缺 replacement")]
    [InlineData("[{\"action\":\"block_replace\",\"block_id\":\"bab\",\"replacement\":\"<shape>\"}]", "不是良构 XML")]
    [InlineData("[{\"action\":\"block_insert\"}]", "缺 insertion")]
    [InlineData("[{\"action\":\"block_insert\",\"insertion\":\"<shape/>\",\"block_id\":\"bab\"}]", "却填了 block_id")]
    public async Task ReplaceSlide_ShouldRejectInvalidParts_BeforeCallingDownstream(string parts, string message)
    {
        var result = await CreateWriteTools().ReplaceSlideAsync(
            Args(("xml_presentation_id", "S7Y1"), ("slide_id", "s1"), ("parts", parts)),
            CancellationToken.None);

        result.Error!.Subtype.Should().Be("invalid_args", "本地逐项校验（带项号）才有修复价值");
        result.Text.Should().Contain(message);
        _slideClient.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task ReplaceSlide_ShouldRejectMoreThan200Parts_BeforeCallingDownstream()
    {
        var parts = "[" + string.Join(",", Enumerable.Repeat(
            "{\"action\":\"block_insert\",\"insertion\":\"<shape/>\"}", 201)) + "]";

        var result = await CreateWriteTools().ReplaceSlideAsync(
            Args(("xml_presentation_id", "S7Y1"), ("slide_id", "s1"), ("parts", parts)),
            CancellationToken.None);

        result.Error!.Subtype.Should().Be("invalid_args");
        result.Text.Should().Contain("最多 200");
        _slideClient.Invocations.Should().BeEmpty();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("00")]
    [InlineData("-1")]
    [InlineData("r3072")]
    [InlineData("")]
    public async Task RevertHistory_ShouldRejectNonPositiveVersionId_BeforeCallingDownstream(string versionId)
    {
        var result = await CreateWriteTools().RevertHistoryAsync(
            Args(("xml_presentation_id", "S7Y1"), ("history_version_id", versionId)),
            CancellationToken.None);

        result.Error!.Subtype.Should().Be("invalid_args", "最常见的误用是填 revision_id，必须在本地拦下");
        _historyClient.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task RevertHistory_ShouldSubmitAsyncTask_AndPointToStatusTool()
    {
        _historyClient
            .Setup(c => c.RevertPresentationHistoryAsync("S7Y1", It.IsAny<RevertPresentationHistoryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<RevertPresentationHistoryResult>
            {
                Code = 0,
                Data = new RevertPresentationHistoryResult
                {
                    TaskId = "task1",
                    Status = "running",
                    PollAfterMs = 500,
                    HistoryVersionId = "3072",
                },
            });

        var payload = Payload(await CreateWriteTools().RevertHistoryAsync(
            Args(("xml_presentation_id", "S7Y1"), ("history_version_id", "3072")),
            CancellationToken.None));

        payload["submitted"]!.GetValue<bool>().Should().BeTrue();
        payload["task_id"]!.GetValue<string>().Should().Be("task1");
        payload["poll_after_ms"]!.GetValue<int>().Should().Be(500);
        payload["note"]!.GetValue<string>().Should().Contain("slides.get_revert_status",
            "提交成功不等于已落地，必须指向状态工具");
    }

    // ───────────────────── dry_run：写工具零调用 ─────────────────────

    [Theory]
    [InlineData(FeishuToolNames.SlidesCreatePresentation)]
    [InlineData(FeishuToolNames.SlidesAddSlide)]
    [InlineData(FeishuToolNames.SlidesReplaceSlide)]
    [InlineData(FeishuToolNames.SlidesDeleteSlide)]
    [InlineData(FeishuToolNames.SlidesRevertHistory)]
    public async Task WriteTools_DryRun_ShouldNotCallDownstream(string toolName)
    {
        var tools = CreateWriteTools();
        var result = toolName switch
        {
            FeishuToolNames.SlidesCreatePresentation => await tools.CreatePresentationAsync(
                Args(("content", "<presentation xmlns=\"http://www.larkoffice.com/sml/2.0\"/>"), ("dry_run", true)),
                CancellationToken.None),
            FeishuToolNames.SlidesAddSlide => await tools.AddSlideAsync(
                Args(("xml_presentation_id", "S7Y1"), ("content", "<slide/>"), ("dry_run", true)),
                CancellationToken.None),
            FeishuToolNames.SlidesReplaceSlide => await tools.ReplaceSlideAsync(
                Args(
                    ("xml_presentation_id", "S7Y1"),
                    ("slide_id", "s1"),
                    ("parts", "[{\"action\":\"block_insert\",\"insertion\":\"<shape/>\"}]"),
                    ("dry_run", true)),
                CancellationToken.None),
            FeishuToolNames.SlidesDeleteSlide => await tools.DeleteSlideAsync(
                Args(("xml_presentation_id", "S7Y1"), ("slide_id", "s1"), ("dry_run", true)),
                CancellationToken.None),
            _ => await tools.RevertHistoryAsync(
                Args(("xml_presentation_id", "S7Y1"), ("history_version_id", "3072"), ("dry_run", true)),
                CancellationToken.None),
        };

        result.ToString().Should().StartWith("[dry_run]");
        _presentationClient.Invocations.Should().BeEmpty();
        _slideClient.Invocations.Should().BeEmpty();
        _historyClient.Invocations.Should().BeEmpty();
    }

    // ───────────────────── 测试替身 ─────────────────────

    /// <summary>记录落盘来源并把文件名回显为路径（不碰磁盘）。</summary>
    private sealed class RecordingStager : IFeishuAttachmentStager
    {
        public List<AttachmentSource> Sources { get; } = [];

        public Task<StagedAttachment?> StageAsync(AttachmentSource source, CancellationToken cancellationToken = default)
        {
            Sources.Add(source);
            var staged = new StagedAttachment(
                $"C:\\staged\\{source.FileName}",
                source.Content?.Length ?? 0,
                ContentType: null,
                Cleanup: static () => ValueTask.CompletedTask);
            return Task.FromResult<StagedAttachment?>(staged);
        }
    }
}
