// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools;
using Mud.Feishu.DataModels.Drive.Files;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// 受控下载出口执行器（<c>drive.download_file</c> / <c>drive.export_document</c>，T-1）。
/// </summary>
/// <remarks>
/// <para>
/// <b>字节不进上下文（no-bytes-in-context）</b>：字节经 <see cref="DownloadedContentGuard"/> 拦截
/// 平台错误体后交宿主 <see cref="IFeishuAttachmentStager"/> 落盘，工具结果只回
/// <c>local_path</c>/<c>size</c>/<c>content_type</c>。
/// </para>
/// <para>
/// <b>交付物生命周期</b>：与上传类工具的"临时附件"不同，下载产物<b>是交付物</b>——执行器<b>不</b>调用
/// <c>Cleanup</c>，文件生存期由宿主决定。
/// </para>
/// <para>
/// <b>导出链路</b>：创建导出任务 → 有界轮询任务状态 → 下载导出产物；<c>dry_run</c> 只预演创建步骤
/// （不消耗任何平台资源）。导出产物在任务完成后约 10 分钟内有效（平台会清理），故任务一完成立即下载。
/// </para>
/// </remarks>
internal sealed class DriveDownloadTools(
    Mud.Feishu.IFeishuTenantV1DriveFiles filesClient,
    IFeishuAttachmentStager stager)
{
    /// <summary>导出任务状态：成功。</summary>
    private const int ExportJobSucceeded = 0;

    /// <summary>导出任务状态：初始化 / 处理中（继续轮询）。</summary>
    private const int ExportJobInitializing = 1;

    /// <summary>导出任务状态：处理中（继续轮询）。</summary>
    private const int ExportJobProcessing = 2;

    /// <summary>file_extension 闭集（平台取值）。</summary>
    private static readonly string[] ExportExtensions = ["docx", "pdf", "xlsx", "csv"];

    /// <summary>type 闭集（平台取值）。</summary>
    private static readonly string[] ExportTypes = ["doc", "sheet", "bitable", "docx"];

    private readonly Mud.Feishu.IFeishuTenantV1DriveFiles _filesClient = filesClient
        ?? throw new ArgumentNullException(nameof(filesClient));
    private readonly IFeishuAttachmentStager _stager = stager
        ?? throw new ArgumentNullException(nameof(stager));

    /// <summary>轮询间隔（毫秒；测试覆写以缩短等待窗）。</summary>
    internal int PollIntervalMs { get; set; } = 500;

    /// <summary>轮询次数上限（约 30s 等待窗；测试覆写以缩短等待窗）。</summary>
    internal int MaxPollAttempts { get; set; } = 60;

    /// <summary>drive.download_file：下载资源文件到宿主落盘边界。</summary>
    [ToolHandler(typeof(IFeishuTenantDriveDownloadFileTool))]
    public Task<ToolResult> DownloadFileAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveDownloadFile);
        return executor.RunAsync(async () =>
        {
            var args = DriveDownloadFileArgs.Unpack(arguments);

            var content = await _filesClient
                .DownloadFileAsync(args.FileToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return await StageAsync(content, null, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>drive.export_document：创建导出任务 → 有界轮询 → 下载导出产物。</summary>
    [ToolHandler(typeof(IFeishuTenantDriveExportDocumentTool))]
    public Task<ToolResult> ExportDocumentAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveExportDocument);
        return executor.RunAsync(async () =>
        {
            var args = DriveExportDocumentArgs.Unpack(arguments);

            var extension = RequireOneOf(args.FileExtension, ExportExtensions, "file_extension");
            var docType = RequireOneOf(args.Type, ExportTypes, "type");
            RequireCompatibleFormat(docType, extension);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/drive/v1/export_tasks",
                    null,
                    ("file_token", args.FileToken.Length), ("file_extension", extension.Length), ("type", docType.Length)));
            }

            // ① 创建导出任务（平台侧副作用；dry_run 已在上方拦截，预演不占用导出配额）。
            var created = FeishuApiResultReader.Read(await _filesClient
                .CreateExportTaskAsync(
                    new ExportTasksRequest { FileExtension = extension, Token = args.FileToken, Type = docType },
                    cancellationToken)
                .ConfigureAwait(false));
            if (executor.FailIfError(created) is { } createFailure)
            {
                return createFailure;
            }

            var ticket = created.Data?.Ticket;
            if (string.IsNullOrEmpty(ticket))
            {
                throw new ArgumentException("导出任务创建成功但平台未返回 ticket——无法继续");
            }

            // ② 有界轮询至终态（0=成功；1/2=未完成；其余=失败）。
            // 完成标志独立于 FileName：平台可不返回文件名（file_name 为可空字段），
            // 不能用 "fileName is null" 当循环条件——否则任务已成功仍会继续轮询。
            string? fileName = null;
            var completed = false;
            for (var attempt = 0; !completed; attempt++)
            {
                var status = FeishuApiResultReader.Read(await _filesClient
                    .GetExportTaskAsync(ticket!, token: args.FileToken, cancellationToken: cancellationToken)
                    .ConfigureAwait(false));
                if (executor.FailIfError(status) is { } statusFailure)
                {
                    return statusFailure;
                }

                var job = status.Data?.Result;
                if (job is { JobStatus: ExportJobSucceeded })
                {
                    fileName = job.FileName;
                    completed = true;
                }
                else if (job?.JobStatus is not (ExportJobInitializing or ExportJobProcessing))
                {
                    throw new ArgumentException(
                        $"导出任务失败（job_status={job?.JobStatus?.ToString(CultureInfo.InvariantCulture) ?? "未知"}，"
                        + $"原因：{job?.JobErrorMsg ?? "(无错误信息)"}）");
                }
                else if (attempt >= MaxPollAttempts - 1)
                {
                    throw new ArgumentException(
                        $"导出任务在等待窗口内未完成（ticket={ticket}）——文档可能过大，请稍后重试");
                }
                else
                {
                    await Task.Delay(PollIntervalMs, cancellationToken).ConfigureAwait(false);
                }
            }

            // ③ 下载导出产物（token 参数即源文档 token）。
            var content = await _filesClient
                .DownloadExportFileAsync(args.FileToken, cancellationToken)
                .ConfigureAwait(false);
            return await StageAsync(content, fileName, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>字节 → 错误体防线 → 宿主落盘 → 结果信封（两条出口的唯一共同路径）。</summary>
    private async Task<ToolResult> StageAsync(byte[]? content, string? suggestedFileName, CancellationToken cancellationToken)
    {
        if (content is null || content.Length == 0)
        {
            throw new ArgumentException("平台未返回文件内容——请确认 token 有效且当前应用有下载权限");
        }

        if (DownloadedContentGuard.ShouldRejectForJsonErrorBody(content, expectedContentType: null, out var reason))
        {
            throw new ArgumentException($"下载内容被错误体防线拒绝：{reason}");
        }

        var staged = await _stager
            .StageAsync(new AttachmentSource(null, content, suggestedFileName), cancellationToken)
            .ConfigureAwait(false)
            // 宿主返回 null = 该来源不被允许（大小上限/落盘策略等宿主安全域）——不得降级为"跳过落盘"。
            ?? throw new ArgumentException("附件落盘被宿主拒绝（宿主安全域：大小上限 / 落盘策略）——文件未交付");

        return ToolResult.FromText(new JsonObject
        {
            ["local_path"] = staged.LocalPath,
            ["size"] = staged.Size,
            ["content_type"] = staged.ContentType,
        }.ToJsonString());
    }

    /// <summary>闭集校验（平台取值均为小写，逐字符比对）。</summary>
    private static string RequireOneOf(string value, string[] allowed, string parameterName)
    {
        var trimmed = value.Trim();
        if (!allowed.Contains(trimmed, StringComparer.Ordinal))
        {
            throw new ArgumentException($"{parameterName} 仅支持 {string.Join("/", allowed)}，实际: {value}");
        }

        return trimmed;
    }

    /// <summary>导出格式兼容矩阵（docx 文档→docx/pdf；doc/sheet/bitable→其他按平台表）。</summary>
    private static void RequireCompatibleFormat(string docType, string extension)
    {
        var compatible = (docType, extension) switch
        {
            ("docx", "docx") or ("docx", "pdf") => true,
            ("doc", "docx") or ("doc", "pdf") => true,
            ("sheet", "xlsx") or ("sheet", "csv") => true,
            ("bitable", "xlsx") or ("bitable", "csv") => true,
            _ => false,
        };

        if (!compatible)
        {
            throw new ArgumentException(
                $"导出格式不兼容：type={docType} 不支持 file_extension={extension}"
                + "（docx→docx/pdf；sheet/bitable→xlsx/csv）");
        }
    }
}
