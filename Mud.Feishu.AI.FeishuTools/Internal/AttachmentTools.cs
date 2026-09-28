// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Channels;
using Mud.Feishu.AI.FeishuTools.Tools;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// 上传类工具执行器（<c>im.send_image</c> / <c>im.send_file</c>，WP7 / AT-F07 上传部分 / AT-F08）。
/// </summary>
/// <remarks>
/// <para>
/// <b>三步链路（落盘 → 上传 → 发送）与生命周期</b>：URL 经 <see cref="IFeishuAttachmentStager"/>
/// 落成 SDK <c>[FilePath]</c> 参数所需的本地绝对路径 → 上传取 <c>image_key</c>/<c>file_key</c> →
/// 以该 key 发送消息；<b>无论成功失败，<c>Cleanup</c> 都在 <c>finally</c> 中调用</b>
/// （临时文件不进 GC/finalizer，否则磁盘会在进程生命周期内持续增长）。
/// </para>
/// <para>
/// <b>不接受本地路径</b>：模型给本地路径是危险信号（无从得知宿主磁盘布局）——
/// 参数校验在落盘<b>之前</b>拦截，宿主因此不会被诱导去读任意本地文件。
/// </para>
/// <para>
/// <b>安全域由宿主实现</b>：域名白名单（防 SSRF）、大小上限、扩展名/MIME 校验都在
/// <see cref="IFeishuAttachmentStager"/> 实现里；SDK 侧只把"来源不被允许"（返回 <c>null</c>）
/// 转成结构化 <c>invalid_args</c>，不假装自己能做安全域。
/// </para>
/// </remarks>
internal sealed class AttachmentTools(
    Mud.Feishu.IFeishuTenantV1Message messageClient,
    IFeishuAttachmentStager stager)
{
    /// <summary>IM 上传接口的文件类型：非枚举内格式统一用 <c>stream</c>（官方文档口径）。</summary>
    private const string StreamFileType = "stream";

    /// <summary>图片消息的图片用途：<c>message</c>（用于发送消息）。</summary>
    private const string MessageImageType = "message";

    private readonly Mud.Feishu.IFeishuTenantV1Message _messageClient = messageClient
        ?? throw new ArgumentNullException(nameof(messageClient));
    private readonly IFeishuAttachmentStager _stager = stager
        ?? throw new ArgumentNullException(nameof(stager));

    /// <summary>im.send_image：落盘 → 上传图片 → 发送图片消息（<c>dry_run=true</c> 时只预演）。</summary>
    public Task<FeishuToolResult> SendImageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImSendImage);
        return executor.RunAsync(async () =>
        {
            var args = ImSendImageArgs.Unpack(arguments);
            var imageUrl = RequireHttpUrl(args.ImageUrl, "image_url");
            var receiveIdType = ResolveReceiveIdType(args.ReceiveIdType);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/im/v1/images → /open-apis/im/v1/messages",
                    ToolDryRun.IdempotencyNote(null),
                    ("receive_id", args.ReceiveId.Length), ("image_url", imageUrl.Length)));
            }

            var staged = await StageAsync(imageUrl, args.FileName, cancellationToken).ConfigureAwait(false);
            try
            {
                var upload = FeishuApiResultReader.Read(await _messageClient
                    .UploadImageAsync(
                        new UploadImageRequest { ImageType = MessageImageType, FilePath = staged.LocalPath },
                        cancellationToken)
                    .ConfigureAwait(false));
                if (executor.FailIfError(upload) is { } uploadFailure)
                {
                    return uploadFailure;
                }

                return await SendWithKeyAsync(
                    executor, args.ReceiveId, receiveIdType, "image", "image_key", upload.Data!.ImageKey, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                // 生命周期显式：临时文件必须消失（不进 GC/finalizer）。
                // Cleanup 的失败不得覆盖业务结果（它是宿主实现，异常隔离与审计出口同款处理）。
                try
                {
                    await staged.Cleanup().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // 有意吞掉：清理失败不应把一次成功的发送变成失败（磁盘残留由宿主监控负责）。
                }
            }
        });
    }

    /// <summary>im.send_file：落盘 → 上传文件 → 发送文件消息（<c>dry_run=true</c> 时只预演）。</summary>
    public Task<FeishuToolResult> SendFileAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImSendFile);
        return executor.RunAsync(async () =>
        {
            var args = ImSendFileArgs.Unpack(arguments);
            var fileUrl = RequireHttpUrl(args.FileUrl, "file_url");
            var receiveIdType = ResolveReceiveIdType(args.ReceiveIdType);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/im/v1/files → /open-apis/im/v1/messages",
                    ToolDryRun.IdempotencyNote(null),
                    ("receive_id", args.ReceiveId.Length), ("file_url", fileUrl.Length), ("file_name", args.FileName.Length)));
            }

            var staged = await StageAsync(fileUrl, args.FileName, cancellationToken).ConfigureAwait(false);
            try
            {
                var upload = FeishuApiResultReader.Read(await _messageClient
                    .UploadFileAsync(
                        new UploadMessageFileRequest
                        {
                            FileType = StreamFileType,
                            FileName = args.FileName,
                            FilePath = staged.LocalPath,
                        },
                        cancellationToken)
                    .ConfigureAwait(false));
                if (executor.FailIfError(upload) is { } uploadFailure)
                {
                    return uploadFailure;
                }

                return await SendWithKeyAsync(
                    executor, args.ReceiveId, receiveIdType, "file", "file_key", upload.Data!.FileKey, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                try
                {
                    await staged.Cleanup().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // 同上：清理失败不覆盖业务结果。
                }
            }
        });
    }

    /// <summary>以资源 key 发送消息（<c>msg_type</c> 为 image/file，content 为 <c>{"&lt;key 名&gt;": "..."}</c>）。</summary>
    private async Task<FeishuToolResult> SendWithKeyAsync(
        ToolExecutor executor,
        string receiveId,
        string receiveIdType,
        string msgType,
        string keyName,
        string? key,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException($"上传成功但平台未返回 {keyName}——无法继续发送");
        }

        var outcome = FeishuApiResultReader.Read(await _messageClient
            .SendMessageAsync(
                new SendMessageRequest
                {
                    ReceiveId = receiveId,
                    MsgType = msgType,
                    Content = new JsonObject { [keyName] = key }.ToJsonString(),
                },
                receiveIdType,
                cancellationToken)
            .ConfigureAwait(false));
        return executor.FromApiUntruncated(outcome, static data => new JsonObject
        {
            ["message_id"] = data.MessageId,
        });
    }

    /// <summary>经宿主落盘器把 URL 落成 SDK 上传接口所需的本地绝对路径。</summary>
    private async Task<StagedAttachment> StageAsync(
        string url,
        string? fileName,
        CancellationToken cancellationToken)
    {
        var staged = await _stager
            .StageAsync(new AttachmentSource(url, null, fileName), cancellationToken)
            .ConfigureAwait(false);

        // 宿主返回 null = 该来源不被允许（域名白名单/大小/MIME 等宿主策略）——不得降级为"跳过校验"。
        return staged ?? throw new ArgumentException(
            $"附件来源不被宿主允许（宿主安全域拒绝）：{url}——请检查宿主的域名白名单/大小上限/MIME 校验策略");
    }

    /// <summary>校验 URL 参数（<b>只接受 http/https 绝对地址</b>；拒绝本地路径）。</summary>
    /// <remarks>取值由生成的 <c>Args.Unpack</c> 承担（必填校验同源），此处只保留协议白名单——它是业务规则而非解包。</remarks>
    private static string RequireHttpUrl(string value, string parameterName)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                $"{parameterName} 须为 http/https 绝对地址（不接受本地路径与其它协议），实际: {value}");
        }

        return value;
    }

    /// <summary>接收方 ID 类型（缺省 <c>chat_id</c>；闭集校验与发送文本消息共用同一常量）。</summary>
    private static string ResolveReceiveIdType(string? receiveIdTypeRaw)
    {
        var receiveIdType = receiveIdTypeRaw ?? "chat_id";
        if (!EditMessageChannel.AllowedReceiveIdTypes.Contains(receiveIdType, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"receive_id_type 仅支持 {string.Join("/", EditMessageChannel.AllowedReceiveIdTypes)}，实际: {receiveIdType}");
        }

        return receiveIdType;
    }
}
