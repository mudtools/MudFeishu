// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using System.Text.Json.Nodes;

using Mud.Feishu.DataModels.Docx;
using Mud.Feishu.DataModels.Drive;
using Mud.Feishu.DataModels.Drive.Files;
using Mud.Feishu.DataModels.Drive.Folder;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// Markdown 生命周期域（<c>markdown.*</c>，5 枚：2 只读 + 3 写）的链路用例。
/// </summary>
/// <remarks>
/// <para>
/// <b>本域值得锁住的非显然事实</b>：① 创建是「上传 .md → 导入任务 → 有界轮询」三步，
/// 源文件在终态后才清理（提前删会让平台取不到待导入内容）；② 补丁按精确文本命中，
/// 多处命中必须显式 <c>replace_all</c>，否则拒绝写入；③ 整篇重建是「先追加后删除」，
/// 删除失败时新旧并存且 <c>changed=true</c>（新内容确实写入了）；④ <c>.md</c> 资源文件
/// 走严格 UTF-8 解码 + 错误体防线（平台 200 + JSON 错误体不得进入正文）。
/// </para>
/// </remarks>
public class MarkdownToolsTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1DriveFiles> _filesClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1DocxBlocks> _blocksClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1DriveFolder> _folderClient = new();
    private readonly RecordingStager _stager = new();

    private MarkdownTools CreateTools(int maxResultLength = 20000)
        => new(
            filesClient: _filesClient.Object,
            blocksClient: _blocksClient.Object,
            folderClient: _folderClient.Object,
            stager: _stager,
            logger: null,
            options: Options.Create(new FeishuAgentOptions { MaxToolResultLength = maxResultLength }))
        {
            PollIntervalMs = 1,
            MaxPollAttempts = 5,
        };

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static JsonObject Payload(ToolResult result)
        => JsonNode.Parse(result.ToString())!.AsObject();

    /// <summary>配置 docx 侧「转换 → 读数 → 追加 → 删除」四步的成功链路。</summary>
    private void SetupRewrite(int oldBlockCount, params string[] newBlockIds)
    {
        _blocksClient
            .Setup(c => c.ContentConvertAsync(
                It.IsAny<ConvertContentRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ContentConvertResult>
            {
                Code = 0,
                Data = new ContentConvertResult
                {
                    Blocks = [.. newBlockIds.Select(id => new Block { BlockId = id })],
                },
            });

        _blocksClient
            .Setup(c => c.GetChildrenBlocksPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<Block>
            {
                Code = 0,
                Data = new ApiPageListResult<Block>
                {
                    Items = [.. Enumerable.Range(0, oldBlockCount).Select(_ => new Block())],
                    HasMore = false,
                },
            });

        _blocksClient
            .Setup(c => c.CreateBlockAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateBlockRequest>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<BlockOpResult>
            {
                Code = 0,
                Data = new BlockOpResult
                {
                    Childrens = [.. newBlockIds.Select(id => new Block { BlockId = id })],
                },
            });

        _blocksClient
            .Setup(c => c.BatchDeleteBlocksAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BatchDeleteBlocksRequest>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<BatchDeleteBlocksResult>
            {
                Code = 0,
                Data = new BatchDeleteBlocksResult(),
            });
    }

    private void SetupDocxContent(string content)
        => _filesClient
            .Setup(c => c.GetFileContentByFileTokenAsync(
                It.IsAny<string>(), "docx", "markdown", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<FileContentResult>
            {
                Code = 0,
                Data = new FileContentResult { Content = content },
            });

    // ───────────────────── 契约事实（method / path / 风险 / 写标记） ─────────────────────

    [Theory]
    [InlineData("markdown.create", "write", "POST", "/open-apis/drive/v1/import_tasks")]
    [InlineData("markdown.fetch", "read", "GET", "/open-apis/docs/v1/content")]
    [InlineData("markdown.patch", "write", "POST", "/open-apis/docx/v1/documents/{document_id}/blocks/{block_id}/children")]
    [InlineData("markdown.overwrite", "write", "POST", "/open-apis/docx/v1/documents/{document_id}/blocks/{block_id}/children")]
    [InlineData("markdown.diff", "read", "GET", "/open-apis/docs/v1/content")]
    public void Contracts_ShouldCarrySdkRouteRiskAndWriteFlag(string toolName, string risk, string httpMethod, string route)
    {
        var contract = FeishuToolContracts.ByToolName[toolName];

        contract.HttpMethod.Should().Be(httpMethod);
        contract.Route.Should().Be(route);
        contract.Identity.Should().Be("tenant");
        contract.IsWrite.Should().Be(risk == "write");
        contract.Risk.Should().Be(risk == "write" ? FeishuToolRisk.Write : FeishuToolRisk.Read);
    }

    // ───────────────────── markdown.create ─────────────────────

    [Fact]
    public async Task Create_ShouldUploadThenImportThenPollThenCleanSource()
    {
        _folderClient
            .Setup(c => c.GetDriveRootFolderMetaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetDriveRootFolderMetaReuslt>
            {
                Code = 0,
                Data = new GetDriveRootFolderMetaReuslt { Token = "root_tok" },
            });

        UploadAllFileRequest? upload = null;
        ImportTasksRequest? import = null;
        var deleted = new List<(string Token, string Type)>();

        _filesClient
            .Setup(c => c.UploadAllFileAsync(It.IsAny<UploadAllFileRequest>(), It.IsAny<CancellationToken>()))
            .Callback((UploadAllFileRequest request, CancellationToken _) => upload = request)
            .ReturnsAsync(new FeishuApiResult<FilesUploadAllResult>
            {
                Code = 0,
                Data = new FilesUploadAllResult { FileToken = "src_tok" },
            });

        _filesClient
            .Setup(c => c.CreateImportTaskAsync(It.IsAny<ImportTasksRequest>(), It.IsAny<CancellationToken>()))
            .Callback((ImportTasksRequest request, CancellationToken _) => import = request)
            .ReturnsAsync(new FeishuApiResult<TasksResult>
            {
                Code = 0,
                Data = new TasksResult { Ticket = "ticket_1" },
            });

        var polls = new Queue<FeishuApiResult<ImportTaskResult>>(
        [
            new()
            {
                Code = 0,
                Data = new ImportTaskResult { Result = new ImportTaskInfo { JobStatus = 2 } },
            },
            new()
            {
                Code = 0,
                Data = new ImportTaskResult
                {
                    Result = new ImportTaskInfo
                    {
                        JobStatus = 0,
                        Token = "doxcn1",
                        Url = "https://example.feishu.cn/docx/doxcn1",
                        Type = "docx",
                    },
                },
            },
        ]);

        _filesClient
            .Setup(c => c.GetImportTaskAsync("ticket_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => polls.Dequeue());

        _filesClient
            .Setup(c => c.DeleteFileByFileTokenAsync("src_tok", "file", It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .Callback((string token, string type, bool? _, CancellationToken _) => deleted.Add((token, type)))
            .ReturnsAsync(new FeishuApiResult<FileTaskResult> { Code = 0, Data = new FileTaskResult() });

        var markdown = "# 标题\n\n正文";
        var result = await CreateTools().CreateAsync(
            Args(("title", "周报/2026"), ("markdown", markdown)), CancellationToken.None);

        upload.Should().NotBeNull("上传步骤不可省");
        upload!.ParentType.Should().Be("explorer");
        upload.ParentNode.Should().Be("root_tok", "未指定 folder_token 时落根目录");
        upload.FileName.Should().Be("周报_2026.md", "标题中的路径分隔符必须净化，后缀固定 md");
        upload.Size.Should().Be(Encoding.UTF8.GetByteCount(markdown));

        import.Should().NotBeNull();
        import!.FileExtension.Should().Be("md", "扩展名必须与源文件后缀逐字一致（平台 1069910）");
        import.FileToken.Should().Be("src_tok");
        import.Type.Should().Be("docx");
        import.Point!.MountType.Should().Be(1);
        import.Point.MountKey.Should().Be("root_tok");

        deleted.Should().ContainSingle().Which.Should().Be(("src_tok", "file"), "导入终态后清理源文件");
        _stager.CleanupCalls.Should().Be(1, "临时落盘文件必须清理");

        var payload = Payload(result);
        payload["document_token"]!.GetValue<string>().Should().Be("doxcn1");
        payload["source_file_cleaned"]!.GetValue<bool>().Should().BeTrue();
        payload["title"]!.GetValue<string>().Should().Be("周报/2026");
    }

    [Fact]
    public async Task Create_WhenImportTaskCreationFails_ShouldCleanSourceAndReturnError()
    {
        _filesClient
            .Setup(c => c.UploadAllFileAsync(It.IsAny<UploadAllFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<FilesUploadAllResult>
            {
                Code = 0,
                Data = new FilesUploadAllResult { FileToken = "src_tok" },
            });

        _filesClient
            .Setup(c => c.CreateImportTaskAsync(It.IsAny<ImportTasksRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<TasksResult> { Code = 1061004, Msg = "forbidden" });

        _filesClient
            .Setup(c => c.DeleteFileByFileTokenAsync("src_tok", "file", It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<FileTaskResult> { Code = 0, Data = new FileTaskResult() });

        var result = await CreateTools().CreateAsync(
            Args(("title", "T"), ("markdown", "x"), ("folder_token", "fld1")), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[tool_error] markdown.create");
        text.Should().Contain("forbidden");
        _filesClient.Verify(
            c => c.DeleteFileByFileTokenAsync("src_tok", "file", It.IsAny<bool?>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "导入任务创建失败后源文件是孤儿，必须立即清理");
        _stager.CleanupCalls.Should().Be(1);
    }

    [Fact]
    public async Task Create_WhenImportJobFails_ShouldKeepSourceTokenAndExplainStatus()
    {
        _filesClient
            .Setup(c => c.UploadAllFileAsync(It.IsAny<UploadAllFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<FilesUploadAllResult>
            {
                Code = 0,
                Data = new FilesUploadAllResult { FileToken = "src_tok" },
            });

        _filesClient
            .Setup(c => c.CreateImportTaskAsync(It.IsAny<ImportTasksRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<TasksResult> { Code = 0, Data = new TasksResult { Ticket = "ticket_1" } });

        _filesClient
            .Setup(c => c.GetImportTaskAsync("ticket_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ImportTaskResult>
            {
                Code = 0,
                Data = new ImportTaskResult
                {
                    Result = new ImportTaskInfo
                    {
                        JobStatus = 112,
                        JobErrorMsg = "unsupported extension",
                    },
                },
            });

        var result = await CreateTools().CreateAsync(
            Args(("title", "T"), ("markdown", "x"), ("folder_token", "fld1")), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("格式不受支持", "失败状态必须转成可执行提示");
        text.Should().Contain("src_tok", "失败时保留源文件 token 供排查");
        _filesClient.Verify(
            c => c.DeleteFileByFileTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "导入失败时源文件是唯一排查线索，不得删除");
        _stager.CleanupCalls.Should().Be(1, "本地临时文件与平台源文件是两回事，本地仍须清理");
    }

    [Fact]
    public async Task Create_WithoutStager_ShouldReturnActionableError()
    {
        var tools = new MarkdownTools(
            filesClient: _filesClient.Object,
            folderClient: _folderClient.Object,
            options: Options.Create(new FeishuAgentOptions { MaxToolResultLength = 20000 }));

        var result = await tools.CreateAsync(
            Args(("title", "T"), ("markdown", "x"), ("folder_token", "fld1")), CancellationToken.None);

        result.ToString().Should().Contain("IFeishuAttachmentStager");
        _filesClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_DryRun_ShouldNotTouchDownstream()
    {
        var result = await CreateTools().CreateAsync(
            Args(("title", "T"), ("markdown", "x"), ("folder_token", "fld1"), ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain("import_tasks");
        _filesClient.VerifyNoOtherCalls();
        _stager.StageCalls.Should().Be(0);
    }

    // ───────────────────── markdown.fetch ─────────────────────

    [Fact]
    public async Task Fetch_Docx_ShouldWindowContentWithNextOffset()
    {
        SetupDocxContent("0123456789");

        var result = await CreateTools().FetchAsync(
            Args(("token", "doxcn1"), ("offset", 2), ("limit", 4)), CancellationToken.None);

        var payload = Payload(result);
        payload["markdown"]!.GetValue<string>().Should().Be("2345");
        payload["total_length"]!.GetValue<int>().Should().Be(10);
        payload["has_more"]!.GetValue<bool>().Should().BeTrue();
        payload["next_offset"]!.GetValue<int>().Should().Be(6);
        payload["token_type"]!.GetValue<string>().Should().Be("docx");
    }

    [Fact]
    public async Task Fetch_File_ShouldStripBomAndDecodeUtf8()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("# 标题")).ToArray();
        _filesClient
            .Setup(c => c.DownloadFileAsync("filetok", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        var result = await CreateTools().FetchAsync(
            Args(("token", "filetok"), ("token_type", "file")), CancellationToken.None);

        Payload(result)["markdown"]!.GetValue<string>().Should().Be("# 标题", "BOM 不得混入正文");
    }

    [Fact]
    public async Task Fetch_File_WhenBodyIsJsonError_ShouldReject()
    {
        _filesClient
            .Setup(c => c.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("""{"code":1770001,"msg":"file not found"}"""));

        var result = await CreateTools().FetchAsync(
            Args(("token", "filetok"), ("token_type", "file")), CancellationToken.None);

        result.ToString().Should().Contain("错误体防线");
    }

    [Theory]
    [InlineData("sheet")]
    [InlineData("DOC")]
    public async Task Fetch_WithUnknownTokenType_ShouldReject(string tokenType)
    {
        var result = await CreateTools().FetchAsync(
            Args(("token", "t"), ("token_type", tokenType)), CancellationToken.None);

        result.ToString().Should().Contain("仅支持 docx");
        _filesClient.VerifyNoOtherCalls();
    }

    // ───────────────────── markdown.patch ─────────────────────

    [Fact]
    public async Task Patch_ShouldReplaceUniqueTextThenRebuildDocument()
    {
        SetupDocxContent("hello world");
        SetupRewrite(3, "new1");
        string? capturedToken = null;
        BatchDeleteBlocksRequest? deleteRequest = null;

        _blocksClient
            .Setup(c => c.CreateBlockAsync(
                "doxcn1", "doxcn1", It.IsAny<CreateBlockRequest>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string _, CreateBlockRequest _, int? _, string? clientToken, string _, CancellationToken _) =>
                capturedToken = clientToken)
            .ReturnsAsync(new FeishuApiResult<BlockOpResult>
            {
                Code = 0,
                Data = new BlockOpResult { Childrens = [new Block { BlockId = "new1" }] },
            });

        _blocksClient
            .Setup(c => c.BatchDeleteBlocksAsync(
                "doxcn1", "doxcn1", It.IsAny<BatchDeleteBlocksRequest>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string _, BatchDeleteBlocksRequest request, int? _, string? _, CancellationToken _) =>
                deleteRequest = request)
            .ReturnsAsync(new FeishuApiResult<BatchDeleteBlocksResult>
            {
                Code = 0,
                Data = new BatchDeleteBlocksResult(),
            });

        var result = await CreateTools().PatchAsync(
            Args(("document_token", "doxcn1"), ("find", "world"), ("replace_with", "there"), ("idempotency_key", "op-1")),
            CancellationToken.None);

        capturedToken.Should().Be("op-1", "幂等键必须作为 client_token 下发");
        deleteRequest!.StartIndex.Should().Be(0);
        deleteRequest.EndIndex.Should().Be(3, "删除区间是执行时读取的原块数");

        var payload = Payload(result);
        payload["changed"]!.GetValue<bool>().Should().BeTrue();
        payload["replacements"]!.GetValue<int>().Should().Be(1);
        payload["before_length"]!.GetValue<int>().Should().Be(11);
        payload["after_length"]!.GetValue<int>().Should().Be(11);
    }

    [Fact]
    public async Task Patch_WhenFindMatchesMultipleTimes_ShouldRejectWithoutWriting()
    {
        SetupDocxContent("a a a");

        var result = await CreateTools().PatchAsync(
            Args(("document_token", "doxcn1"), ("find", "a"), ("idempotency_key", "op-1")),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("命中 3 处");
        text.Should().Contain("replace_all=true");
        _blocksClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Patch_WhenFindMatchesNothing_ShouldReportUnchanged()
    {
        SetupDocxContent("hello");

        var result = await CreateTools().PatchAsync(
            Args(("document_token", "doxcn1"), ("find", "absent"), ("idempotency_key", "op-1")),
            CancellationToken.None);

        var payload = Payload(result);
        payload["changed"]!.GetValue<bool>().Should().BeFalse();
        payload["replacements"]!.GetValue<int>().Should().Be(0);
        _blocksClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Patch_DryRun_ShouldCountWithoutWriting()
    {
        SetupDocxContent("hello world");

        var result = await CreateTools().PatchAsync(
            Args(
                ("document_token", "doxcn1"), ("find", "world"), ("replace_with", "there"),
                ("idempotency_key", "op-1"), ("dry_run", true)),
            CancellationToken.None);

        var payload = Payload(result);
        payload["changed"]!.GetValue<bool>().Should().BeTrue();
        payload["replacements"]!.GetValue<int>().Should().Be(1);
        payload["dry_run"]!.GetValue<bool>().Should().BeTrue();
        _blocksClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Patch_WithoutIdempotencyKey_ShouldReject()
    {
        var result = await CreateTools().PatchAsync(
            Args(("document_token", "doxcn1"), ("find", "a")), CancellationToken.None);

        result.ToString().Should().Contain("idempotency_key");
        _filesClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Patch_WhenDeleteFails_ShouldReportPartialFailureButChanged()
    {
        SetupDocxContent("old");
        SetupRewrite(2, "new1");
        _blocksClient
            .Setup(c => c.BatchDeleteBlocksAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BatchDeleteBlocksRequest>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<BatchDeleteBlocksResult> { Code = 1770001, Msg = "internal error" });

        var result = await CreateTools().PatchAsync(
            Args(("document_token", "doxcn1"), ("find", "old"), ("idempotency_key", "op-1")),
            CancellationToken.None);

        var payload = Payload(result);
        payload["changed"]!.GetValue<bool>().Should().BeTrue("新内容已写入，changed 不得因删除失败而报 false");
        payload["partial_failure"]!.GetValue<bool>().Should().BeTrue();
        payload["deleted"]!.GetValue<int>().Should().Be(0);
        payload["message"]!.GetValue<string>().Should().Contain("docx.delete_blocks", "必须给出精确修复指引");
    }

    // ───────────────────── markdown.overwrite ─────────────────────

    [Fact]
    public async Task Overwrite_ShouldRebuildWholeDocument()
    {
        SetupRewrite(2, "new1", "new2");

        var result = await CreateTools().OverwriteAsync(
            Args(("document_token", "doxcn1"), ("markdown", "# new"), ("idempotency_key", "op-2")),
            CancellationToken.None);

        var payload = Payload(result);
        payload["appended"]!.GetValue<int>().Should().Be(2);
        payload["deleted"]!.GetValue<int>().Should().Be(2);
        payload["new_block_ids"]!.AsArray().Count.Should().Be(2);
    }

    [Fact]
    public async Task Overwrite_WithoutBlocksClient_ShouldReturnActionableError()
    {
        var tools = new MarkdownTools(
            filesClient: _filesClient.Object,
            blocksClient: null,
            folderClient: _folderClient.Object,
            stager: _stager,
            options: Options.Create(new FeishuAgentOptions { MaxToolResultLength = 20000 }));

        var result = await tools.OverwriteAsync(
            Args(("document_token", "doxcn1"), ("markdown", "# new"), ("idempotency_key", "op-2")),
            CancellationToken.None);

        result.ToString().Should().Contain("IFeishuTenantV1DocxBlocks");
        _blocksClient.VerifyNoOtherCalls();
        _filesClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Overwrite_DryRun_ShouldNotTouchDownstream()
    {
        var result = await CreateTools().OverwriteAsync(
            Args(("document_token", "doxcn1"), ("markdown", "# new"), ("idempotency_key", "op-2"), ("dry_run", true)),
            CancellationToken.None);

        result.ToString().Should().StartWith("[dry_run]");
        _blocksClient.VerifyNoOtherCalls();
        _filesClient.VerifyNoOtherCalls();
    }

    // ───────────────────── markdown.diff ─────────────────────

    [Fact]
    public async Task Diff_WithRightText_ShouldReturnUnifiedDiff()
    {
        SetupDocxContent("a\nb\nc");

        var result = await CreateTools().DiffAsync(
            Args(("left_token", "doxcn1"), ("right_text", "a\nB\nc")), CancellationToken.None);

        var payload = Payload(result);
        payload["changed"]!.GetValue<bool>().Should().BeTrue();
        payload["added_lines"]!.GetValue<int>().Should().Be(1);
        payload["removed_lines"]!.GetValue<int>().Should().Be(1);
        payload["right_source"]!.GetValue<string>().Should().Be("text");
        payload["diff"]!.GetValue<string>().Should().Contain("-b").And.Contain("+B");
    }

    [Fact]
    public async Task Diff_WithBothRightSources_ShouldReject()
    {
        var result = await CreateTools().DiffAsync(
            Args(("left_token", "doxcn1"), ("right_token", "doxcn2"), ("right_text", "x")),
            CancellationToken.None);

        result.ToString().Should().Contain("二选一");
        _filesClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Diff_WithoutRightSource_ShouldReject()
    {
        var result = await CreateTools().DiffAsync(
            Args(("left_token", "doxcn1")), CancellationToken.None);

        result.ToString().Should().Contain("right_token");
        _filesClient.VerifyNoOtherCalls();
    }

    /// <summary>宿主落盘器替身：记录入参、模拟清理，不触碰真实磁盘。</summary>
    private sealed class RecordingStager : IFeishuAttachmentStager
    {
        public int StageCalls { get; private set; }

        public int CleanupCalls { get; private set; }

        public string? LastFileName { get; private set; }

        public byte[]? LastContent { get; private set; }

        public Task<StagedAttachment?> StageAsync(AttachmentSource source, CancellationToken cancellationToken = default)
        {
            StageCalls++;
            LastFileName = source.FileName;
            LastContent = source.Content?.ToArray();

            var staged = new StagedAttachment(
                $"C:\\staged\\{source.FileName}",
                source.Content?.Length ?? 0,
                ContentType: null,
                Cleanup: () =>
                {
                    CleanupCalls++;
                    return ValueTask.CompletedTask;
                });

            return Task.FromResult<StagedAttachment?>(staged);
        }
    }
}
