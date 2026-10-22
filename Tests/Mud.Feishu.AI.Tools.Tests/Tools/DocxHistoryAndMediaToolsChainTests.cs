// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.DataModels.Docx;
using Mud.Feishu.DataModels.Drive;
using Mud.Feishu.DataModels.Drive.FileVersions;
using Mud.Feishu.DataModels.Drive.Media;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// <c>docx.history_list</c>（跨域 Drive 版本列表）与 <c>docx.media_insert</c>
/// （Drive 素材上传 → Docx 图片块）的链路用例。
/// </summary>
/// <remarks>
/// <para>
/// <b>本批值得锁住的非显然事实</b>：① 版本端点归 Drive 而非 Docx，<c>obj_type</c> 是闭集
/// （docx / sheet），非法值必须在本地拒绝而不是交给平台；② 媒体上传不带 <c>file_path</c>
/// 就等于上传空字节——落盘器交出的本地路径必须进入表单；③ 素材上传不具幂等键，
/// 块创建失败时留下的是「已上传但未被引用」的素材，必须回 <c>partial_failure</c> +
/// <c>image_token</c>，否则模型把「未显示」误判为「未上传」而重复上传；④ 临时文件无论
/// 成功、失败还是拒绝都要清理（用户数据驻留面）。
/// </para>
/// </remarks>
public class DocxHistoryAndMediaToolsChainTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1Docx> _docxClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1DocxBlocks> _blocksClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1DriveFilesVersions> _versionsClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1DriveMedia> _mediaClient = new();
    private readonly RecordingStager _stager = new();

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static JsonObject Payload(ToolResult result)
        => JsonNode.Parse(result.ToString())!.AsObject();

    private static FeishuAgentOptions AgentOptions() => new() { MaxToolResultLength = 20000 };

    private DocxDeepTools CreateHistoryTools()
        => new(
            _docxClient.Object,
            _blocksClient.Object,
            new Mock<Mud.Feishu.IFeishuTenantV1DocxAnnouncement>().Object,
            _versionsClient.Object,
            Options.Create(AgentOptions()));

    private DocxWriteTools CreateMediaTools(IFeishuAttachmentStager? stager = null)
        => new(
            _docxClient.Object,
            _blocksClient.Object,
            Options.Create(AgentOptions()),
            mediaClient: _mediaClient.Object,
            stager: stager ?? _stager);

    /// <summary>配置「素材上传成功 → 建块成功」的两步链路。</summary>
    private void SetupUploadThenBlock(string imageToken = "imgtk1", string blockId = "blk1")
    {
        _mediaClient
            .Setup(c => c.UploadAllMediaAsync(It.IsAny<MediasUploadAllRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<FilesUploadAllResult>
            {
                Code = 0,
                Data = new FilesUploadAllResult { FileToken = imageToken },
            });

        _blocksClient
            .Setup(c => c.CreateBlockAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateBlockRequest>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<BlockOpResult>
            {
                Code = 0,
                Data = new BlockOpResult { Childrens = [new Block { BlockId = blockId }] },
            });
    }

    // ───────────────────── docx.history_list ─────────────────────

    [Fact]
    public async Task HistoryList_ShouldProjectVersionFieldsAndPagingContract()
    {
        _versionsClient
            .Setup(c => c.GetFileVersionPageListByFileTokenAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<FileVersionInfo>
            {
                Code = 0,
                Data = new ApiPageListResult<FileVersionInfo>
                {
                    HasMore = true,
                    PageToken = "next",
                    Items =
                    [
                        new FileVersionInfo
                        {
                            VersionSuffix = "vA1",
                            Name = "项目文档 第1版",
                            ParentToken = "doxcparenttok",
                            ObjType = "docx",
                            CreatorId = "694699009591869451",
                            CreateTime = "1660708537",
                            UpdateTime = "1660708999",
                            Status = "completed",
                        },
                    ],
                },
            });

        var result = await CreateHistoryTools().ListHistoryAsync(
            Args(("document_id", "doxcn1"), ("page_token", "prev")), CancellationToken.None);

        var payload = Payload(result);
        payload["has_more"]!.GetValue<bool>().Should().BeTrue();
        payload["page_token"]!.GetValue<string>().Should().Be("next");
        var first = payload["items"]!.AsArray()[0]!.AsObject();
        first["version"]!.GetValue<string>().Should().Be("vA1");
        first["name"]!.GetValue<string>().Should().Be("项目文档 第1版");
        first["creator_id"]!.GetValue<string>().Should().Be("694699009591869451");
        result.ToString().Should().NotContain("doxcparenttok", "parent_token 等未投影字段不得出现在结果里");
    }

    [Fact]
    public async Task HistoryList_ShouldDefaultObjTypeToDocxAndUsePageSizesConstant()
    {
        string? seenObjType = null;
        int seenPageSize = 0;

        _versionsClient
            .Setup(c => c.GetFileVersionPageListByFileTokenAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string objType, int pageSize, string? ___, string? ____, CancellationToken _____) =>
            {
                seenObjType = objType;
                seenPageSize = pageSize;
            })
            .ReturnsAsync(new FeishuApiPageListResult<FileVersionInfo>
            {
                Code = 0,
                Data = new ApiPageListResult<FileVersionInfo>(),
            });

        await CreateHistoryTools().ListHistoryAsync(
            Args(("document_id", "doxcn1")), CancellationToken.None);

        seenObjType.Should().Be("docx", "缺省即当前文档形态");
        seenPageSize.Should().Be(PageSizes.FileVersions, "页大小来自 PageSizes（运维参数不进 Schema）");
    }

    [Fact]
    public async Task HistoryList_WhenObjTypeUnsupported_ShouldRejectBeforeCallingDownstream()
    {
        var result = await CreateHistoryTools().ListHistoryAsync(
            Args(("document_id", "doxcn1"), ("obj_type", "bitable")), CancellationToken.None);

        result.ToString().Should().Contain("obj_type").And.Contain("docx / sheet");
        _versionsClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HistoryList_WhenFetchAll_ShouldAggregatePagesUntilNoMore()
    {
        var calls = 0;

        _versionsClient
            .Setup(c => c.GetFileVersionPageListByFileTokenAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                calls++;
                var last = calls == 2;

                return new FeishuApiPageListResult<FileVersionInfo>
                {
                    Code = 0,
                    Data = new ApiPageListResult<FileVersionInfo>
                    {
                        HasMore = !last,
                        PageToken = last ? null : "p2",
                        Items =
                        [
                            new FileVersionInfo { VersionSuffix = $"v{calls}" },
                            new FileVersionInfo { VersionSuffix = $"v{calls}b" },
                        ],
                    },
                };
            });

        var result = await CreateHistoryTools().ListHistoryAsync(
            Args(("document_id", "doxcn1"), ("obj_type", "sheet"), ("fetch_all", true)),
            CancellationToken.None);

        var payload = Payload(result);
        calls.Should().Be(2, "聚合路径必须翻到 has_more=false");
        payload["items"]!.AsArray().Should().HaveCount(4);
        payload["has_more"]!.GetValue<bool>().Should().BeFalse();
    }

    // ───────────────────── docx.media_insert ─────────────────────

    [Fact]
    public async Task MediaInsert_ShouldUploadThenCreateImageBlockAndReportBothTokens()
    {
        SetupUploadThenBlock(imageToken: "imgtk1", blockId: "blk9");

        var result = await CreateMediaTools().MediaInsertAsync(
            Args(
                ("document_id", "doxcn1"),
                ("image_url", "https://example.com/a/b/pic.png"),
                ("idempotency_key", "op-1")),
            CancellationToken.None);

        var payload = Payload(result);
        payload["image_token"]!.GetValue<string>().Should().Be("imgtk1");
        payload["block_ids"]!.AsArray()[0]!.GetValue<string>().Should().Be("blk9");

        _mediaClient.Verify(
            c => c.UploadAllMediaAsync(
                It.Is<MediasUploadAllRequest>(r =>
                    r.ParentType == "docx_image"
                    && r.ParentNode == "doxcn1"
                    && r.FileName == "pic.png"
                    && r.FilePath == "C:\\staged\\pic.png"),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "落盘器交出的本地路径必须进入表单——缺 file_path 等于上传空字节");

        _blocksClient.Verify(
            c => c.CreateBlockAsync(
                "doxcn1", "doxcn1", It.IsAny<CreateBlockRequest>(),
                It.IsAny<int?>(), "op-1", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "父块缺省为文档根块（= document_id），幂等键透传 client_token");

        _stager.CleanupCalls.Should().Be(1);
    }

    [Fact]
    public async Task MediaInsert_ShouldMapAlignAndCarryCaptionAndSize()
    {
        SetupUploadThenBlock();
        CreateBlockRequest? captured = null;

        _blocksClient
            .Setup(c => c.CreateBlockAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateBlockRequest>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, CreateBlockRequest request, int? ___, string? ____, string _____, CancellationToken ______) =>
                captured = request)
            .ReturnsAsync(new FeishuApiResult<BlockOpResult>
            {
                Code = 0,
                Data = new BlockOpResult { Childrens = [new Block { BlockId = "blk1" }] },
            });

        await CreateMediaTools().MediaInsertAsync(
            Args(
                ("document_id", "doxcn1"),
                ("image_url", "https://example.com/pic.png"),
                ("align", "right"),
                ("width", 640),
                ("height", 480),
                ("caption", "季度营收图"),
                ("index", 0)),
            CancellationToken.None);

        captured.Should().NotBeNull();
        var image = captured!.Childrens![0]!.Image!;
        image.Align.Should().Be(3, "模型侧 left/center/right 映射平台闭集 1/2/3");
        image.Width.Should().Be(640);
        image.Height.Should().Be(480);
        image.Caption!.Content.Should().Be("季度营收图");
        captured.Index.Should().Be(0);
    }

    [Fact]
    public async Task MediaInsert_WhenAlignUnknown_ShouldRejectWithoutUploading()
    {
        var result = await CreateMediaTools().MediaInsertAsync(
            Args(("document_id", "doxcn1"), ("image_url", "https://example.com/pic.png"), ("align", "middle")),
            CancellationToken.None);

        result.ToString().Should().Contain("align");
        _stager.StageCalls.Should().Be(0, "闭集校验先于任何 IO（对齐写错不该白传一张图）");
        _mediaClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MediaInsert_WhenUrlIsNotHttp_ShouldRejectBeforeStaging()
    {
        var result = await CreateMediaTools().MediaInsertAsync(
            Args(("document_id", "doxcn1"), ("image_url", "file:///etc/passwd")),
            CancellationToken.None);

        result.ToString().Should().Contain("http/https");
        _stager.StageCalls.Should().Be(0);
    }

    [Fact]
    public async Task MediaInsert_DryRun_ShouldNotTouchDownstream()
    {
        var result = await CreateMediaTools().MediaInsertAsync(
            Args(
                ("document_id", "doxcn1"),
                ("image_url", "https://example.com/pic.png"),
                ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain("medias/upload_all").And.Contain("blocks/{block_id}/children");
        _stager.StageCalls.Should().Be(0);
        _mediaClient.VerifyNoOtherCalls();
        _blocksClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MediaInsert_WithoutStager_ShouldReturnActionableError()
    {
        var tools = new DocxWriteTools(
            _docxClient.Object,
            _blocksClient.Object,
            Options.Create(AgentOptions()),
            mediaClient: _mediaClient.Object);

        var result = await tools.MediaInsertAsync(
            Args(("document_id", "doxcn1"), ("image_url", "https://example.com/pic.png")),
            CancellationToken.None);

        result.ToString().Should().Contain("IFeishuAttachmentStager");
        _mediaClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MediaInsert_WithoutMediaClient_ShouldReturnActionableError()
    {
        var tools = new DocxWriteTools(
            _docxClient.Object,
            _blocksClient.Object,
            Options.Create(AgentOptions()),
            stager: _stager);

        var result = await tools.MediaInsertAsync(
            Args(("document_id", "doxcn1"), ("image_url", "https://example.com/pic.png")),
            CancellationToken.None);

        result.ToString().Should().Contain("AddDriveApi");
        _stager.StageCalls.Should().Be(0, "客户端缺席时不该先落盘（白占宿主磁盘）");
    }

    [Fact]
    public async Task MediaInsert_WhenStagerRejectsSource_ShouldReportSecurityRejection()
    {
        var result = await CreateMediaTools(stager: new RejectingStager()).MediaInsertAsync(
            Args(("document_id", "doxcn1"), ("image_url", "https://not-allowed.example.com/pic.png")),
            CancellationToken.None);

        result.ToString().Should().Contain("来源不被宿主允许");
        _mediaClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MediaInsert_WhenBlockCreationFails_ShouldReportPartialFailureWithImageToken()
    {
        _mediaClient
            .Setup(c => c.UploadAllMediaAsync(It.IsAny<MediasUploadAllRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<FilesUploadAllResult>
            {
                Code = 0,
                Data = new FilesUploadAllResult { FileToken = "imgtk1" },
            });

        _blocksClient
            .Setup(c => c.CreateBlockAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateBlockRequest>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<BlockOpResult> { Code = 1770001, Msg = "block create failed" });

        var result = await CreateMediaTools().MediaInsertAsync(
            Args(("document_id", "doxcn1"), ("image_url", "https://example.com/pic.png")),
            CancellationToken.None);

        var payload = Payload(result);
        payload["partial_failure"]!.GetValue<bool>().Should().BeTrue();
        payload["image_token"]!.GetValue<string>().Should().Be("imgtk1",
            "素材已上传，必须把 token 交回模型，否则它只能重传");
        payload["message"]!.GetValue<string>().Should().Contain("不支持幂等");
        _stager.CleanupCalls.Should().Be(1, "失败路径同样要清理临时文件");
    }

    [Fact]
    public async Task MediaInsert_WhenUploadReturnsNoToken_ShouldFailWithoutCreatingBlock()
    {
        _mediaClient
            .Setup(c => c.UploadAllMediaAsync(It.IsAny<MediasUploadAllRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<FilesUploadAllResult> { Code = 0, Data = new FilesUploadAllResult() });

        var result = await CreateMediaTools().MediaInsertAsync(
            Args(("document_id", "doxcn1"), ("image_url", "https://example.com/pic.png")),
            CancellationToken.None);

        result.ToString().Should().Contain("file_token");
        _blocksClient.VerifyNoOtherCalls();
        _stager.CleanupCalls.Should().Be(1);
    }

    [Theory]
    [InlineData("https://cdn.example.com/dir/photo.jpeg", "photo.jpeg")]
    [InlineData("https://cdn.example.com/photo%20name.png", "photo name.png")]
    [InlineData("https://cdn.example.com", "image.png")]
    public async Task MediaInsert_ShouldDeriveFileNameFromUrlPath(string url, string expected)
    {
        SetupUploadThenBlock();
        MediasUploadAllRequest? captured = null;

        _mediaClient
            .Setup(c => c.UploadAllMediaAsync(It.IsAny<MediasUploadAllRequest>(), It.IsAny<CancellationToken>()))
            .Callback((MediasUploadAllRequest request, CancellationToken _) => captured = request)
            .ReturnsAsync(new FeishuApiResult<FilesUploadAllResult>
            {
                Code = 0,
                Data = new FilesUploadAllResult { FileToken = "imgtk1" },
            });

        await CreateMediaTools().MediaInsertAsync(
            Args(("document_id", "doxcn1"), ("image_url", url)), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.FileName.Should().Be(expected);
    }

    [Fact]
    public async Task MediaInsert_WhenSuppliedFileNameExceedsLimit_ShouldRejectBeforeUploading()
    {
        SetupUploadThenBlock();

        var result = await CreateMediaTools().MediaInsertAsync(
            Args(
                ("document_id", "doxcn1"),
                ("image_url", "https://example.com/pic.png"),
                ("file_name", new string('a', 300) + ".png")),
            CancellationToken.None);

        result.ToString().Should().Contain("250");
        _mediaClient.VerifyNoOtherCalls();
        _blocksClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MediaInsert_WhenDerivedFileNameTooLong_ShouldTruncateKeepingExtension()
    {
        SetupUploadThenBlock();
        MediasUploadAllRequest? captured = null;

        _mediaClient
            .Setup(c => c.UploadAllMediaAsync(It.IsAny<MediasUploadAllRequest>(), It.IsAny<CancellationToken>()))
            .Callback((MediasUploadAllRequest request, CancellationToken _) => captured = request)
            .ReturnsAsync(new FeishuApiResult<FilesUploadAllResult>
            {
                Code = 0,
                Data = new FilesUploadAllResult { FileToken = "imgtk1" },
            });

        await CreateMediaTools().MediaInsertAsync(
            Args(("document_id", "doxcn1"), ("image_url", $"https://example.com/{new string('a', 300)}.png")),
            CancellationToken.None);

        captured!.FileName.Length.Should().Be(250, "平台素材名上限 250，超限即被拒绝");
        captured.FileName.EndsWith(".png").Should().BeTrue("截断必须保住扩展名");
    }

    /// <summary>记录式落盘器（同时统计清理调用——驻留面是安全事实）。</summary>
    private sealed class RecordingStager : IFeishuAttachmentStager
    {
        public int StageCalls { get; private set; }

        public int CleanupCalls { get; private set; }

        public Task<StagedAttachment?> StageAsync(AttachmentSource source, CancellationToken cancellationToken = default)
        {
            StageCalls++;

            var staged = new StagedAttachment(
                $"C:\\staged\\{source.FileName}",
                1024,
                ContentType: null,
                Cleanup: () =>
                {
                    CleanupCalls++;
                    return ValueTask.CompletedTask;
                });

            return Task.FromResult<StagedAttachment?>(staged);
        }
    }

    /// <summary>模拟宿主安全域拒绝（返回 null 表示不允许落盘）。</summary>
    private sealed class RejectingStager : IFeishuAttachmentStager
    {
        public Task<StagedAttachment?> StageAsync(AttachmentSource source, CancellationToken cancellationToken = default)
            => Task.FromResult<StagedAttachment?>(null);
    }
}
