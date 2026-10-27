// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// 妙记逐字稿导出执行器（<c>minutes.get_transcript</c>）：受控下载出口，字节不进上下文。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么单独成类而不进 <c>MinutesReadTools</c></b>：本工具需要<b>非可空</b>的落盘器
/// （<see cref="IFeishuAttachmentStager"/>）——缺席时"能读元数据但不能落盘"的中间态
/// 会让字节既不进上下文也不进磁盘，即静默丢数据。把它与只读投影类合流会迫使落盘器降级为可空。
/// </para>
/// <para>
/// <b>落盘器是硬依赖、SDK 客户端是软依赖</b>：客户端缺席只让本工具给可执行错误（整域软缺席语义），
/// 与 <c>drive.download_file</c> 的出口纪律一致。逐字稿是<b>交付物</b>，故不调用 <c>Cleanup</c>。
/// </para>
/// </remarks>
internal sealed class MinutesTranscriptTools(
    Mud.Feishu.IFeishuTenantV1MinutesMinute? minutesClient,
    IFeishuAttachmentStager stager)
{
    /// <summary>file_format 闭集（平台取值）。</summary>
    private static readonly string[] TranscriptFormats = ["txt", "srt"];

    private readonly Mud.Feishu.IFeishuTenantV1MinutesMinute? _minutesClient = minutesClient;
    private readonly IFeishuAttachmentStager _stager = stager
        ?? throw new ArgumentNullException(nameof(stager));

    /// <summary>minutes.get_transcript：导出逐字稿文件 → 错误体防线 → 宿主落盘 → 只回路径。</summary>
    [ToolHandler(typeof(IFeishuTenantMinutesGetTranscriptTool))]
    public Task<ToolResult> GetMinuteTranscriptAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MinutesGetTranscript);
        return executor.RunAsync(async () =>
        {
            var args = MinutesGetTranscriptArgs.Unpack(arguments);

            var client = _minutesClient
                ?? throw new ArgumentException(
                    "minutes.get_transcript 需要 IFeishuTenantV1MinutesMinute——宿主须启用妙记（Minutes）API");

            var format = string.IsNullOrWhiteSpace(args.FileFormat) ? null : args.FileFormat.Trim();
            if (format is not null && !TranscriptFormats.Contains(format, StringComparer.Ordinal))
            {
                throw new ArgumentException(
                    $"file_format 仅支持 {string.Join("/", TranscriptFormats)}，实际: {args.FileFormat}");
            }

            var content = await client
                .GetMinuteTranscriptAsync(
                    args.MinuteToken,
                    need_speaker: args.NeedSpeaker,
                    need_timestamp: args.NeedTimestamp,
                    file_format: format,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (content is null || content.Length == 0)
            {
                throw new ArgumentException("平台未返回逐字稿内容——请确认 minute_token 有效、妙记转写已完成且当前身份有导出权限");
            }

            // HTTP 200 + JSON 错误体（平台把业务错误当文件内容返回）必须由防线拦下，否则错误 JSON 会被交付成 .txt。
            if (DownloadedContentGuard.ShouldRejectForJsonErrorBody(content, expectedContentType: null, out var reason))
            {
                throw new ArgumentException($"下载内容被错误体防线拒绝：{reason}");
            }

            var staged = await _stager
                .StageAsync(new AttachmentSource(null, content, $"minute-{args.MinuteToken}.{format ?? "txt"}"), cancellationToken)
                .ConfigureAwait(false)
                ?? throw new ArgumentException("附件落盘被宿主拒绝（宿主安全域：大小上限 / 落盘策略）——文件未交付");

            return ToolResult.FromText(new JsonObject
            {
                ["local_path"] = staged.LocalPath,
                ["size"] = staged.Size,
                ["content_type"] = staged.ContentType,
                ["minute_token"] = args.MinuteToken,
            }.ToJsonString());
        });
    }
}
