// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using Mud.Feishu.DataModels.Drive.Files;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// T-1 <b>受控下载出口</b>（<c>drive.download_file</c> / <c>drive.export_document</c>）的链路用例：
/// 断言 SDK 实参（token / 导出请求体 / 轮询 ticket）、错误体防线、宿主落盘边界与
/// <b>交付物生命周期</b>（执行器不清理——与上传类"临时附件"相反）。
/// </summary>
/// <remarks>
/// <para>
/// 验收标准沿用 R3：<b>"工具存在"不算通过</b>——每条工具都必须有抓取实参的链路用例。
/// </para>
/// <para>
/// 落盘器是宿主实现（U-5：SDK 只定契约），故此处用替身：它把字节写成真实文件并记录
/// 清理调用——"执行器是否调用 Cleanup"因此成为可断言事实（下载产物是交付物，不得清理）。
/// </para>
/// </remarks>
public class DriveDownloadToolsChainTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1DriveFiles> _filesClient = new();

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private DriveDownloadTools CreateTools(IFeishuAttachmentStager stager, int maxPollAttempts = 3)
        => new(_filesClient.Object, stager) { PollIntervalMs = 1, MaxPollAttempts = maxPollAttempts };

    /// <summary>宿主落盘器替身：写出真实临时文件并记录清理调用（下载产物不应触发清理）。</summary>
    private sealed class FakeStager : IFeishuAttachmentStager
    {
        private readonly bool _allow;
        private readonly List<string> _stagedPaths = [];

        public FakeStager(bool allow = true) => _allow = allow;

        public int StageCalls { get; private set; }

        public int CleanupCalls { get; private set; }

        public string? LastUrl { get; private set; }

        public string? LastFileName { get; private set; }

        public byte[]? LastContent { get; private set; }

        public async Task<StagedAttachment?> StageAsync(AttachmentSource source, CancellationToken cancellationToken)
        {
            StageCalls++;
            LastUrl = source.Url;
            LastFileName = source.FileName;
            LastContent = source.Content?.ToArray();
            if (!_allow)
            {
                return null;
            }

            var path = Path.Combine(Path.GetTempPath(), $"mud-feishu-download-test-{Guid.NewGuid():N}.bin");
            await File.WriteAllBytesAsync(path, LastContent ?? [], cancellationToken).ConfigureAwait(false);
            _stagedPaths.Add(path);

            return new StagedAttachment(
                path,
                LastContent?.LongLength ?? 0,
                "application/octet-stream",
                () =>
                {
                    CleanupCalls++;
                    File.Delete(path);
                    return ValueTask.CompletedTask;
                });
        }

        /// <summary>测试自身清场：交付物不进执行器清理链，由用例事后删除。</summary>
        public void DeleteStagedFiles()
        {
            foreach (var path in _stagedPaths)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }

    // ───────────────────── 契约事实（method / path / 只读风险） ─────────────────────

    /// <summary>method/path 的真相源是编译期契约（R4 §2.1）：执行器与契约同源，此处把二者锁在一起。</summary>
    [Theory]
    [InlineData("drive.download_file", "/open-apis/drive/v1/files/{file_token}/download", "drive:drive:readonly")]
    [InlineData("drive.export_document", "/open-apis/drive/v1/export_tasks/file/{file_token}/download", "drive:export:readonly")]
    public void Contracts_ShouldCarrySdkMethodAndRoute(string toolName, string route, string scope)
    {
        FeishuToolContracts.ByToolName.Should().ContainKey(toolName);
        var contract = FeishuToolContracts.ByToolName[toolName];

        contract.HttpMethod.Should().Be("GET", "契约的 http 与 SDK 符号派生一致");
        contract.Route.Should().Be(route);
        contract.Identity.Should().Be("tenant");
        contract.Risk.Should().Be(FeishuToolRisk.Read, "下载/导出是只读面（GET 派生读）");
        contract.IsWrite.Should().BeFalse();
        contract.RequiredScopes.Should().Contain(scope);
    }

    // ───────────────────── drive.download_file ─────────────────────

    [Fact]
    public async Task DownloadFile_ShouldStageBytes_AndReturnPathOnly()
    {
        var stager = new FakeStager();
        string? capturedToken = null;
        var payload = Bytes("PDF-BYTES-MARKER");

        _filesClient
            .Setup(c => c.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string fileToken, string? _, string? __, CancellationToken ___) => capturedToken = fileToken)
            .ReturnsAsync(payload);

        try
        {
            var result = await CreateTools(stager).DownloadFileAsync(
                Args(("file_token", "boxcn_doc1")), CancellationToken.None);

            var text = result.ToString()!;
            capturedToken.Should().Be("boxcn_doc1", "file_token 经 [Path] 下发");
            stager.StageCalls.Should().Be(1, "字节必须经宿主落盘（no-bytes-in-context）");
            stager.LastUrl.Should().BeNull("下载来源是内存字节，不是 URL");
            stager.LastContent.Should().Equal(payload);

            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            var localPath = root.GetProperty("local_path").GetString();
            localPath.Should().NotBeNullOrEmpty();
            root.GetProperty("size").GetInt64().Should().Be(payload.LongLength);
            File.Exists(localPath).Should().BeTrue("下载产物是交付物：执行器不得清理，文件留在宿主落盘路径");
            stager.CleanupCalls.Should().Be(0, "与上传类临时文件相反——交付物不进清理链");
            text.Should().NotContain("PDF-BYTES-MARKER", "工具结果只回路径/大小/类型，字节不进上下文");
        }
        finally
        {
            stager.DeleteStagedFiles();
        }
    }

    [Fact]
    public async Task DownloadFile_WhenJsonErrorBody_ShouldRejectWithoutStaging()
    {
        var stager = new FakeStager();
        _filesClient
            .Setup(c => c.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Bytes("""{"code":1770001,"msg":"file not found"}"""));

        var result = await CreateTools(stager).DownloadFileAsync(
            Args(("file_token", "missing")), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[tool_error] drive.download_file");
        text.Should().Contain("错误体防线");
        text.Should().Contain("1770001", "原因必须带平台错误码（F-10/B-2：HTTP 200 + JSON 错误体伪装成文件）");
        stager.StageCalls.Should().Be(0, "错误体不得落盘（否则磁盘上会多一个内容是错误 JSON 的假文件）");
    }

    [Fact]
    public async Task DownloadFile_WhenHostRejectsStaging_ShouldReturnStructuredError()
    {
        var stager = new FakeStager(allow: false);
        _filesClient
            .Setup(c => c.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Bytes("PDF-OK"));

        var result = await CreateTools(stager).DownloadFileAsync(
            Args(("file_token", "boxcn_doc1")), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[tool_error] drive.download_file");
        text.Should().Contain("宿主安全域", "安全域由宿主实现，SDK 只如实回填（不假装能校验）");
        text.Should().Contain("文件未交付", "落盘被拒 = 交付失败，不得降级为『跳过落盘』");
        stager.StageCalls.Should().Be(1, "下载已发生（字节到了进程内），被拒的是落盘策略");
        stager.CleanupCalls.Should().Be(0);
    }

    [Fact]
    public async Task DownloadFile_WhenEmptyContent_ShouldReturnStructuredError()
    {
        var stager = new FakeStager();
        _filesClient
            .Setup(c => c.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<byte>());

        var result = await CreateTools(stager).DownloadFileAsync(
            Args(("file_token", "boxcn_doc1")), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[tool_error] drive.download_file");
        text.Should().Contain("平台未返回文件内容");
        stager.StageCalls.Should().Be(0);
    }

    // ───────────────────── drive.export_document ─────────────────────

    [Fact]
    public async Task ExportDocument_ShouldCreatePollThenDownload_AndStageWithFileName()
    {
        var stager = new FakeStager();
        ExportTasksRequest? capturedRequest = null;
        string? capturedTicket = null;
        string? capturedToken = null;
        string? capturedDownloadToken = null;
        var pollCalls = 0;

        _filesClient
            .Setup(c => c.CreateExportTaskAsync(It.IsAny<ExportTasksRequest>(), It.IsAny<CancellationToken>()))
            .Callback((ExportTasksRequest request, CancellationToken _) => capturedRequest = request)
            .ReturnsAsync(new FeishuApiResult<TasksResult>
            {
                Code = 0,
                Data = new TasksResult { Ticket = "ticket_1" },
            });

        // 处理中 → 成功：Moq 的 ISetupSequentialResult 不支持 Callback，故用队列式 ReturnsAsync。
        var pollResults = new Queue<FeishuApiResult<ExportTasksResult>>(
        [
            new FeishuApiResult<ExportTasksResult>
            {
                Code = 0,
                Data = new ExportTasksResult { Result = new ExportTaskInfo { JobStatus = 2 } },
            },
            new FeishuApiResult<ExportTasksResult>
            {
                Code = 0,
                Data = new ExportTasksResult { Result = new ExportTaskInfo { JobStatus = 0, FileName = "report.pdf" } },
            },
        ]);

        _filesClient
            .Setup(c => c.GetExportTaskAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string ticket, string? token, CancellationToken _) =>
            {
                capturedTicket = ticket;
                capturedToken = token;
                pollCalls++;
            })
            .ReturnsAsync(() => pollResults.Dequeue());

        _filesClient
            .Setup(c => c.DownloadExportFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string token, CancellationToken _) => capturedDownloadToken = token)
            .ReturnsAsync(Bytes("PDF-EXPORT-MARKER"));

        try
        {
            var result = await CreateTools(stager).ExportDocumentAsync(
                Args(("file_token", "docx_token1"), ("file_extension", "pdf"), ("type", "docx")),
                CancellationToken.None);

            capturedRequest!.FileExtension.Should().Be("pdf");
            capturedRequest.Token.Should().Be("docx_token1");
            capturedRequest.Type.Should().Be("docx");
            capturedTicket.Should().Be("ticket_1", "轮询必须带创建返回的 ticket");
            capturedToken.Should().Be("docx_token1", "查询任务必须回带源文档 token（平台据此校验归属）");
            pollCalls.Should().Be(2, "处理中 → 成功：轮询一次后命中终态");
            capturedDownloadToken.Should().Be("docx_token1", "下载导出产物用源文档 token（路径参数）");
            stager.LastFileName.Should().Be("report.pdf", "任务返回的 file_name 作为落盘建议名");

            var text = result.ToString()!;
            using var document = JsonDocument.Parse(text);
            var localPath = document.RootElement.GetProperty("local_path").GetString();
            File.Exists(localPath).Should().BeTrue("导出产物同属交付物：落盘后不清理");
            stager.CleanupCalls.Should().Be(0);
            text.Should().NotContain("PDF-EXPORT-MARKER");
        }
        finally
        {
            stager.DeleteStagedFiles();
        }
    }

    [Fact]
    public async Task ExportDocument_DryRun_ShouldOnlyPreviewCreateStep()
    {
        var stager = new FakeStager();

        var result = await CreateTools(stager).ExportDocumentAsync(
            Args(("file_token", "docx_token1"), ("file_extension", "pdf"), ("type", "docx"), ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain("POST /open-apis/drive/v1/export_tasks", "预演只描述创建步骤");
        text.Should().NotContain("docx_token1", "预演摘要不回显 token 原文");
        stager.StageCalls.Should().Be(0);
        _filesClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExportDocument_WhenTypeFormatMismatch_ShouldReturnStructuredError_WithoutCreatingTask()
    {
        var result = await CreateTools(new FakeStager()).ExportDocumentAsync(
            Args(("file_token", "docx_token1"), ("file_extension", "pdf"), ("type", "sheet")),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[tool_error] drive.export_document");
        text.Should().Contain("导出格式不兼容");
        _filesClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExportDocument_WhenExtensionNotInClosedSet_ShouldReject()
    {
        var result = await CreateTools(new FakeStager()).ExportDocumentAsync(
            Args(("file_token", "docx_token1"), ("file_extension", "doc"), ("type", "doc")),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[tool_error] drive.export_document");
        text.Should().Contain("file_extension 仅支持 docx/pdf/xlsx/csv");
        _filesClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExportDocument_WhenJobFails_ShouldReturnStructuredError_WithoutDownloading()
    {
        var stager = new FakeStager();
        _filesClient
            .Setup(c => c.CreateExportTaskAsync(It.IsAny<ExportTasksRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<TasksResult>
            {
                Code = 0,
                Data = new TasksResult { Ticket = "ticket_x" },
            });

        _filesClient
            .Setup(c => c.GetExportTaskAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ExportTasksResult>
            {
                Code = 0,
                Data = new ExportTasksResult
                {
                    Result = new ExportTaskInfo { JobStatus = 110, JobErrorMsg = "无权限" },
                },
            });

        var result = await CreateTools(stager).ExportDocumentAsync(
            Args(("file_token", "docx_token1"), ("file_extension", "pdf"), ("type", "docx")),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[tool_error] drive.export_document");
        text.Should().Contain("导出任务失败");
        text.Should().Contain("110", "失败分支必须回填 job_status 与原因");
        text.Should().Contain("无权限");
        stager.StageCalls.Should().Be(0);
        _filesClient.Verify(
            c => c.DownloadExportFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "任务失败不得下载（平台未产出文件）");
    }

    [Fact]
    public async Task ExportDocument_WhenPollTimesOut_ShouldReturnStructuredError()
    {
        var stager = new FakeStager();
        var pollCalls = 0;
        _filesClient
            .Setup(c => c.CreateExportTaskAsync(It.IsAny<ExportTasksRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<TasksResult>
            {
                Code = 0,
                Data = new TasksResult { Ticket = "ticket_slow" },
            });

        _filesClient
            .Setup(c => c.GetExportTaskAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string? __, CancellationToken ___) => pollCalls++)
            .ReturnsAsync(new FeishuApiResult<ExportTasksResult>
            {
                Code = 0,
                Data = new ExportTasksResult { Result = new ExportTaskInfo { JobStatus = 2 } },
            });

        var result = await CreateTools(stager, maxPollAttempts: 3).ExportDocumentAsync(
            Args(("file_token", "docx_token1"), ("file_extension", "pdf"), ("type", "docx")),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[tool_error] drive.export_document");
        text.Should().Contain("等待窗口内未完成");
        pollCalls.Should().Be(3, "有界轮询：到上限必须停（不得无限等待）");
        stager.StageCalls.Should().Be(0);
        _filesClient.Verify(
            c => c.DownloadExportFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
