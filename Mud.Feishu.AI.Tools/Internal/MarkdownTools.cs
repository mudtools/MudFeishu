// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

using Mud.Feishu.DataModels.Drive;
using Mud.Feishu.DataModels.Drive.Files;
using Mud.Feishu.DataModels.Drive.Folder;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Markdown 生命周期执行器（<c>markdown.create</c> / <c>fetch</c> / <c>patch</c> /
/// <c>overwrite</c> / <c>diff</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么生命周期落在 docx 上</b>：云空间资源文件没有原位更新接口，覆盖只能删旧建新（token 变化、
/// 引用失效）。故可改写的 Markdown 载体是新版文档：写入走 Markdown→块转换，读取走
/// <c>docs/v1/content</c> 的 markdown 出口；云空间现成的 <c>.md</c> 资源文件仍可按文本读取，但只读。
/// </para>
/// <para>
/// <b>依赖可空、在调用点判定</b>：与 <c>SlidesReadTools</c> 同款——缺少某个客户端时只有用到它的工具
/// 给出可执行错误，而不是整域工具从注册表静默消失。
/// </para>
/// </remarks>
internal sealed class MarkdownTools(
    Mud.Feishu.IFeishuTenantV1DriveFiles? filesClient = null,
    Mud.Feishu.IFeishuTenantV1DocxBlocks? blocksClient = null,
    Mud.Feishu.IFeishuTenantV1DriveFolder? folderClient = null,
    IFeishuAttachmentStager? stager = null,
    ILogger<MarkdownTools>? logger = null,
    IOptions<FeishuAgentOptions> options = null!)
{
    /// <summary>token_type 取值：新版文档。</summary>
    private const string TokenTypeDocx = "docx";

    /// <summary>token_type 取值：云空间资源文件。</summary>
    private const string TokenTypeFile = "file";

    /// <summary>导入任务状态：成功。</summary>
    private const int ImportJobSucceeded = 0;

    /// <summary>导入任务状态：初始化 / 处理中（继续轮询）。</summary>
    private const int ImportJobInitializing = 1;

    /// <summary>导入任务状态：处理中（继续轮询）。</summary>
    private const int ImportJobProcessing = 2;

    /// <summary>导入源文件扩展名（必须与上传文件名后缀逐字一致，否则平台返回 1069910）。</summary>
    private const string ImportFileExtension = "md";

    /// <summary>平台单文件上传上限（20 MB）。</summary>
    private const int MaxUploadBytes = 20 * 1024 * 1024;

    /// <summary>差异文本留给 envelope 其余字段的余量（字符）。</summary>
    private const int DiffEnvelopeReserve = 512;

    /// <summary>严格 UTF-8 解码器（非法字节抛错，而不是替换成 U+FFFD 静默损坏内容）。</summary>
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly Mud.Feishu.IFeishuTenantV1DriveFiles? _filesClient = filesClient;
    private readonly Mud.Feishu.IFeishuTenantV1DocxBlocks? _blocksClient = blocksClient;
    private readonly Mud.Feishu.IFeishuTenantV1DriveFolder? _folderClient = folderClient;
    private readonly IFeishuAttachmentStager? _stager = stager;
    private readonly ILogger<MarkdownTools>? _logger = logger;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>导入任务轮询间隔（毫秒；测试覆写以缩短等待窗）。</summary>
    internal int PollIntervalMs { get; set; } = 500;

    /// <summary>导入任务轮询次数上限（约 30s 等待窗；测试覆写以缩短等待窗）。</summary>
    internal int MaxPollAttempts { get; set; } = 60;

    /// <summary>markdown.create：Markdown → 上传 .md → 导入为 docx（<c>dry_run</c> 只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantMarkdownCreateTool))]
    public Task<ToolResult> CreateAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MarkdownCreate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = MarkdownCreateArgs.Unpack(arguments);

            if (string.IsNullOrWhiteSpace(args.Title))
            {
                throw new ArgumentException("title 不能为空");
            }

            if (string.IsNullOrWhiteSpace(args.Markdown))
            {
                throw new ArgumentException("markdown 不能为空");
            }

            var bytes = Encoding.UTF8.GetBytes(args.Markdown);
            if (bytes.Length > MaxUploadBytes)
            {
                throw new ArgumentException(
                    $"markdown 的 UTF-8 体积为 {bytes.Length.ToString(CultureInfo.InvariantCulture)} 字节，"
                    + $"超过平台单文件上限 {MaxUploadBytes.ToString(CultureInfo.InvariantCulture)} 字节（20 MB）；请拆分内容后分批创建");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST",
                    "/open-apis/drive/v1/files/upload_all → /open-apis/drive/v1/import_tasks → /open-apis/drive/v1/import_tasks/{ticket}",
                    ToolDryRun.IdempotencyNote(null),
                    ("title", args.Title.Length),
                    ("markdown_bytes", bytes.Length),
                    ("folder_token", args.FolderToken?.Length ?? 0)));
            }

            var filesClient = Require(executor.ToolName, _filesClient, "IFeishuTenantV1DriveFiles", "宿主须启用云空间（Drive）API");
            var stager = Require(executor.ToolName, _stager, "IFeishuAttachmentStager", "宿主须注册附件落盘器");
            var folderToken = await ResolveFolderTokenAsync(executor.ToolName, args.FolderToken, cancellationToken).ConfigureAwait(false);
            var uploadName = BuildUploadFileName(args.Title);

            var staged = await stager
                .StageAsync(new AttachmentSource(null, bytes, uploadName), cancellationToken)
                .ConfigureAwait(false)
                ?? throw new ArgumentException("附件落盘被宿主拒绝（宿主安全域：大小上限 / 落盘策略）——文档未创建");

            try
            {
                var upload = FeishuApiResultReader.Read(await filesClient
                    .UploadAllFileAsync(
                        new UploadAllFileRequest
                        {
                            FileName = uploadName,
                            ParentType = "explorer",
                            ParentNode = folderToken,
                            Size = bytes.Length,
                            FilePath = staged.LocalPath,
                        },
                        cancellationToken)
                    .ConfigureAwait(false));
                if (executor.FailIfError(upload) is { } uploadFailure)
                {
                    return uploadFailure;
                }

                var sourceToken = upload.Data?.FileToken;
                if (string.IsNullOrEmpty(sourceToken))
                {
                    throw new ArgumentException("上传成功但平台未返回 file_token——无法继续导入");
                }

                var created = FeishuApiResultReader.Read(await filesClient
                    .CreateImportTaskAsync(
                        new ImportTasksRequest
                        {
                            FileExtension = ImportFileExtension,
                            FileToken = sourceToken!,
                            Type = TokenTypeDocx,
                            FileName = args.Title.Trim(),
                            Point = new ImportTaskMountPoint { MountType = 1, MountKey = folderToken },
                        },
                        cancellationToken)
                    .ConfigureAwait(false));
                if (executor.FailIfError(created) is { } createFailure)
                {
                    await TryDeleteSourceAsync(filesClient, sourceToken!, cancellationToken).ConfigureAwait(false);
                    return createFailure;
                }

                var ticket = created.Data?.Ticket;
                if (string.IsNullOrEmpty(ticket))
                {
                    await TryDeleteSourceAsync(filesClient, sourceToken!, cancellationToken).ConfigureAwait(false);
                    throw new ArgumentException("导入任务创建成功但平台未返回 ticket——无法轮询结果");
                }

                var job = await PollImportAsync(executor, filesClient, ticket!, sourceToken!, cancellationToken).ConfigureAwait(false);

                // 导入终态后才删源文件：任务完成前删除会让平台拿不到待导入内容。
                var cleaned = await TryDeleteSourceAsync(filesClient, sourceToken!, cancellationToken).ConfigureAwait(false);

                var envelope = new JsonObject
                {
                    ["created"] = true,
                    ["document_token"] = job.Token,
                    ["url"] = job.Url,
                    ["title"] = args.Title.Trim(),
                    ["document_type"] = job.Type,
                    ["source_file_token"] = sourceToken,
                    ["source_file_cleaned"] = cleaned,
                };

                if (!cleaned)
                {
                    envelope["note"] = "源 .md 临时文件未能自动清理（不影响新建文档）；可用 drive 工具按 source_file_token 手动删除。";
                }

                if (job.Extra is { Length: > 0 })
                {
                    // extra 是平台"内容被截断"的额外提示，必须转告：模型据此决定是否拆分重导。
                    envelope["truncated_by_platform"] = true;
                    envelope["extra"] = new JsonArray([.. job.Extra.Select(static item => (JsonNode?)item)]);
                }

                return ToolResult.FromText(envelope.ToJsonString());
            }
            finally
            {
                try
                {
                    await staged.Cleanup().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "临时 Markdown 文件清理失败，文件可能残留（path: {Path}）", staged.LocalPath);
                }
            }
        });
    }

    /// <summary>markdown.fetch：按窗口读取 docx 的 Markdown 正文或云空间 .md 文本。</summary>
    [ToolHandler(typeof(IFeishuTenantMarkdownFetchTool))]
    public Task<ToolResult> FetchAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MarkdownFetch, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = MarkdownFetchArgs.Unpack(arguments);
            var tokenType = ResolveTokenType(args.TokenType, "token_type");

            // 窗口越界在本地拒绝：不消耗一次下游调用，模型看得到修复指引。
            ToolContentWindow.Validate(args.Offset, args.Limit, "offset", "limit");

            if (tokenType == TokenTypeFile)
            {
                var (text, failure) = await TryReadFileMarkdownAsync(executor, args.Token, cancellationToken).ConfigureAwait(false);
                if (failure is not null)
                {
                    return failure;
                }

                return ToolResult.FromText(ProjectWindow(text, args.Offset, args.Limit, tokenType).ToJsonString());
            }

            var outcome = FeishuApiResultReader.Read(await Require(
                    executor.ToolName, _filesClient, "IFeishuTenantV1DriveFiles", "宿主须启用云空间（Drive）API")
                .GetFileContentByFileTokenAsync(args.Token, TokenTypeDocx, "markdown", cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            // 窗口已限定输出体积，故不走预算截断（截断会让窗口后半段永远不可达）。
            return executor.FromApiUntruncated(
                outcome,
                data => ProjectWindow(data.Content, args.Offset, args.Limit, tokenType));
        });
    }

    /// <summary>markdown.patch：精确文本替换后整篇重建（要求唯一命中或显式 replace_all）。</summary>
    [ToolHandler(typeof(IFeishuTenantMarkdownPatchTool))]
    public Task<ToolResult> PatchAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MarkdownPatch, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = MarkdownPatchArgs.Unpack(arguments);

            if (string.IsNullOrWhiteSpace(args.Find))
            {
                throw new ArgumentException("find 不能为空（它是要被替换的原文）");
            }

            RequireIdempotencyKey(executor.ToolName, args.IdempotencyKey);

            var (before, readFailure) = await TryReadMarkdownAsync(
                executor, args.DocumentToken, TokenTypeDocx, cancellationToken).ConfigureAwait(false);
            if (readFailure is not null)
            {
                return readFailure;
            }

            var replacements = MarkdownTextOps.CountOccurrences(before!, args.Find);
            var replacement = args.ReplaceWith ?? string.Empty;

            if (replacements > 1 && args.ReplaceAll != true)
            {
                throw new ArgumentException(
                    $"find 在文档中命中 {replacements.ToString(CultureInfo.InvariantCulture)} 处，无法确定替换目标："
                    + "请给出更长的上下文使命中唯一，或显式传 replace_all=true 替换全部");
            }

            var after = replacements == 0 ? before! : MarkdownTextOps.ReplaceAllOrdinal(before!, args.Find, replacement);

            if (replacements == 0)
            {
                return ToolResult.FromText(new JsonObject
                {
                    ["changed"] = false,
                    ["replacements"] = 0,
                    ["before_length"] = before!.Length,
                    ["after_length"] = before!.Length,
                    ["message"] = "未找到与 find 逐字一致的文本（含空白与换行）；文档未被修改。可先 markdown.fetch 核对原文。",
                }.ToJsonString());
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(new JsonObject
                {
                    ["changed"] = true,
                    ["replacements"] = replacements,
                    ["before_length"] = before!.Length,
                    ["after_length"] = after.Length,
                    ["dry_run"] = true,
                    ["note"] = "仅预演：未调用任何写接口。",
                }.ToJsonString());
            }

            var blocksClient = Require(executor.ToolName, _blocksClient, "IFeishuTenantV1DocxBlocks", "宿主须启用文档（Docx）API");
            var envelope = await DocxMarkdownRewrite.ReplaceAsync(
                blocksClient,
                args.DocumentToken,
                args.DocumentToken,
                after,
                args.IdempotencyKey!,
                executor.ToolName,
                cancellationToken).ConfigureAwait(false);

            // 转换/追加失败时文档未被修改（信封带 step）；删除失败时新旧并存（新内容已写入）。
            envelope["changed"] = !envelope.ContainsKey("step");
            envelope["replacements"] = replacements;
            envelope["before_length"] = before!.Length;
            envelope["after_length"] = after.Length;
            return FromEnvelope(envelope);
        });
    }

    /// <summary>markdown.overwrite：用 Markdown 整体重建正文（与 docx.replace_document 同语义）。</summary>
    [ToolHandler(typeof(IFeishuTenantMarkdownOverwriteTool))]
    public Task<ToolResult> OverwriteAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MarkdownOverwrite, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = MarkdownOverwriteArgs.Unpack(arguments);

            if (string.IsNullOrWhiteSpace(args.Markdown))
            {
                throw new ArgumentException("markdown 不能为空");
            }

            RequireIdempotencyKey(executor.ToolName, args.IdempotencyKey);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST+DELETE",
                    $"/open-apis/docx/v1/documents/{args.DocumentToken}/blocks/convert → children → batch_delete",
                    "步骤：① 转换 Markdown ② 追加新块 ③ 删除旧块 [0, 原块数)。原块数在**执行时**读取。"
                    + ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("document_token", args.DocumentToken.Length), ("markdown", args.Markdown.Length)));
            }

            var blocksClient = Require(executor.ToolName, _blocksClient, "IFeishuTenantV1DocxBlocks", "宿主须启用文档（Docx）API");
            var envelope = await DocxMarkdownRewrite.ReplaceAsync(
                blocksClient,
                args.DocumentToken,
                args.DocumentToken,
                args.Markdown,
                args.IdempotencyKey!,
                executor.ToolName,
                cancellationToken).ConfigureAwait(false);
            return FromEnvelope(envelope);
        });
    }

    /// <summary>markdown.diff：两份 Markdown 的行级 unified diff（有界输出）。</summary>
    [ToolHandler(typeof(IFeishuTenantMarkdownDiffTool))]
    public Task<ToolResult> DiffAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MarkdownDiff, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = MarkdownDiffArgs.Unpack(arguments);

            if (args.RightToken is not null && args.RightText is not null)
            {
                throw new ArgumentException("right_token 与 right_text 须二选一（同传无法确定比较哪一侧）");
            }

            if (args.RightToken is null && args.RightText is null)
            {
                throw new ArgumentException("须提供右侧内容：right_token（另一份文档/文件）或 right_text（手头草稿）");
            }

            var leftType = ResolveTokenType(args.LeftTokenType, "left_token_type");
            var rightType = ResolveTokenType(args.RightTokenType, "right_token_type");

            var (left, leftFailure) = await TryReadMarkdownAsync(executor, args.LeftToken, leftType, cancellationToken).ConfigureAwait(false);
            if (leftFailure is not null)
            {
                return leftFailure;
            }

            string right;
            if (args.RightText is not null)
            {
                right = args.RightText;
            }
            else
            {
                var (text, rightFailure) = await TryReadMarkdownAsync(executor, args.RightToken!, rightType, cancellationToken).ConfigureAwait(false);
                if (rightFailure is not null)
                {
                    return rightFailure;
                }

                right = text!;
            }

            var diff = MarkdownLineDiff.Compute(left!, right, Math.Max(256, _maxResultLength - DiffEnvelopeReserve));

            var envelope = new JsonObject
            {
                ["changed"] = diff.Changed,
                ["added_lines"] = diff.AddedLines,
                ["removed_lines"] = diff.RemovedLines,
                ["coarse"] = diff.Coarse,
                ["diff"] = diff.Text,
                ["truncated"] = diff.Truncated,
                ["diff_total_length"] = diff.TotalLength,
                ["left_length"] = left!.Length,
                ["right_length"] = right.Length,
                ["left_token_type"] = leftType,
                ["right_source"] = args.RightText is not null ? "text" : rightType,
            };

            if (!diff.Changed)
            {
                envelope["note"] = "两侧内容一致（比较忽略 CRLF 差异与末尾单个空行）。";
            }

            return FromEnvelope(envelope);
        });
    }

    // ────────── 读取与解码 ──────────

    /// <summary>按 token 类型读取 Markdown；API 失败原样冒泡为执行器结果。</summary>
    private async Task<(string? Text, ToolResult? Failure)> TryReadMarkdownAsync(
        ToolExecutor executor,
        string token,
        string tokenType,
        CancellationToken cancellationToken)
    {
        if (tokenType == TokenTypeFile)
        {
            return await TryReadFileMarkdownAsync(executor, token, cancellationToken).ConfigureAwait(false);
        }

        var outcome = FeishuApiResultReader.Read(await Require(
                executor.ToolName, _filesClient, "IFeishuTenantV1DriveFiles", "宿主须启用云空间（Drive）API")
            .GetFileContentByFileTokenAsync(token, TokenTypeDocx, "markdown", cancellationToken: cancellationToken)
            .ConfigureAwait(false));

        var failure = executor.FailIfError(outcome);
        return failure is null
            ? (outcome.Data?.Content ?? string.Empty, null)
            : (null, failure);
    }

    /// <summary>下载云空间 .md 资源文件并严格按 UTF-8 解码（错误信封不得进入正文）。</summary>
    private async Task<(string? Text, ToolResult? Failure)> TryReadFileMarkdownAsync(
        ToolExecutor executor,
        string token,
        CancellationToken cancellationToken)
    {
        var content = await Require(
                executor.ToolName, _filesClient, "IFeishuTenantV1DriveFiles", "宿主须启用云空间（Drive）API")
            .DownloadFileAsync(token, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (content is null || content.Length == 0)
        {
            throw new ArgumentException("平台未返回文件内容——请确认 token 是云空间里的 .md 资源文件，且当前身份有下载权限");
        }

        if (DownloadedContentGuard.ShouldRejectForJsonErrorBody(content, expectedContentType: null, out var reason))
        {
            throw new ArgumentException($"下载内容被错误体防线拒绝：{reason}");
        }

        try
        {
            var text = StrictUtf8.GetString(content);

            // GetString 不会剥离 BOM；带 BOM 的 UTF-8 文件会多一个 U+FEFF。
            var body = text.Length > 0 && text[0] == '\uFEFF' ? text.Substring(1) : text;
            return (body, null);
        }
        catch (DecoderFallbackException)
        {
            throw new ArgumentException(
                "该资源文件不是合法的 UTF-8 文本（可能是二进制文件或其它编码）——markdown.fetch 只读 UTF-8 编码的 .md 文件；"
                + "其它格式请改用 drive.download_file");
        }
    }

    /// <summary>窗口投影（只读工具共用同口径；输出键用 markdown 以免与消息正文截断口径混淆）。</summary>
    private static JsonObject ProjectWindow(string? content, int? offset, int? limit, string tokenType)
    {
        var window = ToolContentWindow.Compute(content, offset, limit, "offset", "limit");

        var result = new JsonObject
        {
            ["markdown"] = window.Text,
            ["total_length"] = window.TotalLength,
            ["offset"] = window.Offset,
            ["limit"] = window.Limit,
            ["has_more"] = window.HasMore,
            ["next_offset"] = window.NextOffset,
            ["token_type"] = tokenType,
        };

        if (window.OffsetOutOfRange)
        {
            result["offset_out_of_range"] = true;
        }

        return result;
    }

    // ────────── 导入链路 ──────────

    /// <summary>解析上传目录：显式 folder_token 优先，缺省读根文件夹元数据。</summary>
    private async Task<string> ResolveFolderTokenAsync(string toolName, string? folderToken, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(folderToken))
        {
            return folderToken.Trim();
        }

        var folder = Require(toolName, _folderClient, "IFeishuTenantV1DriveFolder", "宿主须启用云空间文件夹 API");
        var outcome = FeishuApiResultReader.Read(await folder
            .GetDriveRootFolderMetaAsync(cancellationToken)
            .ConfigureAwait(false));

        var error = outcome.Ok ? null : outcome.ErrorText;
        if (error is not null || string.IsNullOrEmpty(outcome.Data?.Token))
        {
            throw new ArgumentException(
                $"未能解析云空间根目录（{error ?? "平台未返回根目录 token"}）——请显式传 folder_token，"
                + "或先用 drive.list_folder_files 确认根目录可用");
        }

        return outcome.Data!.Token!;
    }

    /// <summary>有界轮询导入任务至终态；失败/超时消息里保留源文件 token 供排查。</summary>
    private async Task<ImportTaskInfo> PollImportAsync(
        ToolExecutor executor,
        Mud.Feishu.IFeishuTenantV1DriveFiles filesClient,
        string ticket,
        string sourceToken,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            var status = FeishuApiResultReader.Read(await filesClient
                .GetImportTaskAsync(ticket, cancellationToken)
                .ConfigureAwait(false));

            if (executor.FailIfError(status) is not null)
            {
                throw new ArgumentException(
                    $"查询导入任务失败（ticket={ticket}，源文件 token={sourceToken}）：{status.ErrorText}。源文件已保留，可据此排查");
            }

            var job = status.Data?.Result
                ?? throw new ArgumentException($"平台返回空的导入结果（ticket={ticket}，源文件 token={sourceToken}）——请稍后重试");

            if (job.JobStatus == ImportJobSucceeded)
            {
                return string.IsNullOrEmpty(job.Token)
                    ? throw new ArgumentException($"导入任务成功但未返回文档 token（ticket={ticket}）——请在云空间按标题查找")
                    : job;
            }

            if (job.JobStatus is ImportJobInitializing or ImportJobProcessing)
            {
                if (attempt >= MaxPollAttempts - 1)
                {
                    throw new ArgumentException(
                        $"导入任务在等待窗口内未完成（ticket={ticket}，源文件 token={sourceToken}）——"
                        + "文档可能过大，请稍后重新导入或改用 docx.append_blocks 分段写入");
                }

                await Task.Delay(PollIntervalMs, cancellationToken).ConfigureAwait(false);
                continue;
            }

            throw new ArgumentException(
                $"导入任务失败（job_status={job.JobStatus?.ToString(CultureInfo.InvariantCulture) ?? "未知"}，"
                + $"原因：{job.JobErrorMsg ?? "(无错误信息)"}）。{ImportFailureHint(job.JobStatus)}"
                + $"源文件 token={sourceToken} 已保留，可据此排查");
        }
    }

    /// <summary>导入失败状态的常见处置提示（平台状态码闭集见 SDK 的 job_status 文档）。</summary>
    private static string ImportFailureHint(int? jobStatus) => jobStatus switch
    {
        100 => "源文档已加密，无法导入。",
        104 => "租户容量不足，请清理空间后重试。",
        108 => "处理超时，可稍后重试。",
        110 or 116 => "当前身份无导入权限（检查目标文件夹授权）。",
        112 or 113 or 118 or 120 => "格式不受支持或后缀不匹配：源文件必须是 .md，且 file_extension 必须为 md。",
        115 => "导入文件超过平台上限（20 MB），请拆分内容。",
        117 or 119 => "目标文件夹不存在或已删除，请换一个 folder_token。",
        121 => "导入文件已过期，请重新上传。",
        129 => "文件格式损坏，请另存为 UTF-8 .md 后重试。",
        7000 or 7001 or 7002 => "文档块数量/层级/大小超过平台上限，请拆分内容后分批导入。",
        _ => "请根据 job_error_msg 排查；内容较大时可改用 docx 工具分段写入。",
    };

    /// <summary>尽力删除导入源文件；失败不覆盖创建结果（源文件是临时物，文档已建成）。</summary>
    private async Task<bool> TryDeleteSourceAsync(
        Mud.Feishu.IFeishuTenantV1DriveFiles filesClient,
        string sourceToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = FeishuApiResultReader.Read(await filesClient
                .DeleteFileByFileTokenAsync(sourceToken, TokenTypeFile, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            if (deleted.Ok)
            {
                return true;
            }

            _logger?.LogWarning(
                "导入源文件清理失败（file_token: {Token}）：{Error}",
                sourceToken,
                deleted.ErrorText);
            return false;
        }
        catch (Exception ex)
        {
            // OperationCanceledException 也在此吞掉：文档已创建，清理失败不应改写成功事实。
            _logger?.LogWarning(ex, "导入源文件清理异常（file_token: {Token}）", sourceToken);
            return false;
        }
    }

    // ────────── 校验与文本工具 ──────────

    /// <summary>幂等键必填：整篇重建是两步写，重试无稳定 client_token 会重复写入。</summary>
    private static void RequireIdempotencyKey(string toolName, string? idempotencyKey)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return;
        }

        throw new ArgumentException(
            $"{toolName} 必须提供 idempotency_key：写入是先追加后删除的两步操作，"
            + "重试若无稳定 client_token 会重复插入。请传入稳定值（如业务单号），不要用随机数。");
    }

    /// <summary>token_type 闭集校验（缺省 docx）。</summary>
    private static string ResolveTokenType(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TokenTypeDocx;
        }

        var normalized = value.Trim();
        if (normalized.Equals(TokenTypeDocx, StringComparison.OrdinalIgnoreCase))
        {
            return TokenTypeDocx;
        }

        if (normalized.Equals(TokenTypeFile, StringComparison.OrdinalIgnoreCase))
        {
            return TokenTypeFile;
        }

        throw new ArgumentException($"{parameterName} 仅支持 docx（新版文档）或 file（云空间资源文件），实际: {value}");
    }

    /// <summary>上传文件名：标题净化后加 .md（后缀必须与 file_extension 逐字一致）。</summary>
    private static string BuildUploadFileName(string title)
    {
        var sanitized = new string([.. title.Trim().Select(static ch =>
            ch is '/' or '\\' || Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch)]);

        if (sanitized.Length == 0)
        {
            sanitized = "markdown";
        }

        if (sanitized.Length > 200)
        {
            sanitized = sanitized.Substring(0, 200);
        }

        return sanitized + "." + ImportFileExtension;
    }

    /// <summary>取客户端；缺席时给出<b>可执行</b>提示（宿主该启用什么），而不是空引用。</summary>
    private static T Require<T>(string toolName, T? client, string dependency, string hint) where T : class
        => client
            ?? throw new ArgumentException(
                $"{toolName} 需要 {dependency}——{hint}；未启用时该能力不可用（不影响同名域的其它工具）");

    /// <summary>按预算返回手工信封（新块 ID 等数组可能随文档规模增长）。</summary>
    private ToolResult FromEnvelope(JsonObject envelope)
        => ToolResultPipeline.OkJson(envelope, _maxResultLength);
}
