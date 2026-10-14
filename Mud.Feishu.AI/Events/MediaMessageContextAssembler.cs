// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Mud.AI.Agent.Context;
using Mud.Feishu.AI.Conversations;

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 媒体消息上下文装配器：把用户发送的图片/音频/文件转写为文本注入 prompt。
/// </summary>
/// <remarks>
/// <para>
/// <b>N4 拆分（E7）</b>：机制（事实提取 / text 短路 / 截断 / untrusted 头 / 失败隔离跳过）
/// 已上移中立 <see cref="MediaContextAssembler"/>；本类只提供<b>产品事实</b>——媒体类型判定
/// （image/audio/file/post…）、键提取、<see cref="IFeishuMediaTranscriber"/> 适配与各类占位降级文案。
/// 行为与拆分前逐字节一致。
/// </para>
/// <para>
/// <b>设计原则</b>（R7/C3）：转换在装配器内部发生，模型只见到文本结果——
/// 不把 OCR/STT 变成模型可调工具。
/// </para>
/// <para>
/// <b>Order = 15</b>：SenderInfo=10 → <b>MediaMessage=15</b> → QuoteMessage=20 → GroupHistory=30 → Knowledge=100。
/// 媒体先于引用（媒体转写是本轮消息自身的属性）。
/// </para>
/// <para>
/// <b>软缺席</b>：<see cref="IFeishuMediaTranscriber"/> 未注册时，装配器降级为占位文本。
/// </para>
/// <para>
/// <b>安全</b>：下载字节数上限 <see cref="MediaInputOptions.MaxBytes"/>；Content-Type 自检；
/// 全部输出走 <see cref="ContextBudgets.UntrustedHeader"/> + 长度闸。
/// </para>
/// </remarks>
public sealed class MediaMessageContextAssembler : MediaContextAssembler
{
    /// <summary>默认装配顺序（发送者信息之后、引用消息之前）。</summary>
    public const int DefaultOrder = 15;

    private readonly IFeishuMediaTranscriber? _transcriber;
    private readonly MediaInputOptions _options;

    /// <summary>
    /// 初始化 <see cref="MediaMessageContextAssembler"/>。
    /// </summary>
    /// <param name="messageClient">消息客户端（Tenant 身份，用于下载文件）。</param>
    /// <param name="scopeFactory">租户上下文作用域工厂。</param>
    /// <param name="transcriber">媒体转写器（可空——软缺席，未注册时降级为占位文本）。</param>
    /// <param name="options">媒体输入配置。</param>
    /// <param name="logger">日志（可空）。</param>
    public MediaMessageContextAssembler(
        Mud.Feishu.IFeishuTenantV1Message messageClient,
        IFeishuAppContextScopeFactory scopeFactory,
        IFeishuMediaTranscriber? transcriber,
        MediaInputOptions options,
        ILogger<MediaMessageContextAssembler>? logger = null)
        : base(
            order: DefaultOrder,
            untrustedHeader: ContextBudgets.UntrustedHeader,
            truncationMarker: ContextBudgets.TruncationMarker,
            perItemLength: options.PerItemLength,
            logger: logger)
    {
        _ = messageClient ?? throw new ArgumentNullException(nameof(messageClient));
        _ = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _transcriber = transcriber;
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    protected override async Task<string?> TranscribeAsync(
        ConversationRequest request, string messageType, string content, CancellationToken cancellationToken)
    {
        return messageType switch
        {
            "image" => await HandleImageAsync(request, content, cancellationToken).ConfigureAwait(false),
            "audio" => await HandleAudioAsync(request, content, cancellationToken).ConfigureAwait(false),
            "file" or "media" => HandleFileMetadata(content),
            "post" => HandlePostContent(content),
            "sticker" => "[用户发送了一个表情包]",
            "share_chat" => "[用户分享了一个群聊]",
            "share_user" => "[用户分享了一个联系人]",
            _ => $"[用户发送了一条 {messageType} 类型消息]",
        };
    }

    private async Task<string?> HandleImageAsync(ConversationRequest request, string content, CancellationToken cancellationToken)
    {
        if (!_options.EnableImageOcr)
        {
            return "[用户发送了一张图片（OCR 未启用）]";
        }

        var imageKey = ExtractKeyFromContent(content, "image_key");
        if (string.IsNullOrEmpty(imageKey))
        {
            return "[用户发送了一张图片（无法解析 image_key）]";
        }

        if (_transcriber is null)
        {
            return "[用户发送了一张图片（转写器未注册，暂无法识别内容）]";
        }

        var transcription = await _transcriber.TranscribeAsync(
            new MediaTranscriptionRequest(request.AppKey, request.MessageId, imageKey, "image", null),
            cancellationToken).ConfigureAwait(false);

        if (transcription is null || string.IsNullOrWhiteSpace(transcription.Text))
        {
            return "[用户发送了一张图片（暂无法识别内容）]";
        }

        return $"[图片内容｜OCR] {transcription.Text}";
    }

    private async Task<string?> HandleAudioAsync(ConversationRequest request, string content, CancellationToken cancellationToken)
    {
        if (!_options.EnableAudioStt)
        {
            return "[用户发送了一段语音（STT 未启用）]";
        }

        var fileKey = ExtractKeyFromContent(content, "file_key");
        if (string.IsNullOrEmpty(fileKey))
        {
            return "[用户发送了一段语音（无法解析 file_key）]";
        }

        if (_transcriber is null)
        {
            return "[用户发送了一段语音（转写器未注册，暂无法转写）]";
        }

        var transcription = await _transcriber.TranscribeAsync(
            new MediaTranscriptionRequest(request.AppKey, request.MessageId, fileKey, "audio", null),
            cancellationToken).ConfigureAwait(false);

        if (transcription is null || string.IsNullOrWhiteSpace(transcription.Text))
        {
            return "[用户发送了一段语音（暂无法转写内容）]";
        }

        return $"[语音转写] {transcription.Text}";
    }

    private static string? HandleFileMetadata(string content)
    {
        var fileName = ExtractKeyFromContent(content, "file_name");
        if (string.IsNullOrEmpty(fileName))
        {
            return "[用户发送了一个文件]";
        }

        return $"[用户发送了文件 {fileName}]";
    }

    private static string? HandlePostContent(string content)
    {
        try
        {
            if (JsonNode.Parse(content) is not JsonObject obj)
            {
                return content.Length > 200 ? content.Substring(0, 200) + ContextBudgets.TruncationMarker : content;
            }

            var sb = new StringBuilder();
            var title = obj["title"]?["element"]?.ToString();
            if (!string.IsNullOrEmpty(title))
            {
                _ = sb.Append($"标题: {title} ");
            }

            // 尽力抽取文本节点。
            if (obj["content"] is JsonArray contentArray)
            {
                foreach (var block in contentArray)
                {
                    if (block?["tag"]?.ToString() == "text" && block["text"] is not null)
                    {
                        _ = sb.Append(block["text"]!.ToString());
                    }
                }
            }

            return sb.Length > 0 ? sb.ToString() : "[用户发送了一条富文本消息]";
        }
        catch (JsonException)
        {
            // 有意静默（守卫白名单）：富文本 post 载荷非 JSON 属正常形态（纯函数，无 logger 面），
            // 降级文案确定；记日志只会把"用户发了非 JSON 卡片"变成告警噪声。
            return "[用户发送了一条富文本消息]";
        }
    }

    private static string? ExtractKeyFromContent(string content, string keyName)
    {
        try
        {
            if (JsonNode.Parse(content) is JsonObject obj && obj[keyName] is not null)
            {
                return obj[keyName]!.ToString();
            }
        }
        catch (JsonException)
        {
            // 有意静默（守卫白名单）：纯函数（无 logger 面），返回 null 即"该键不存在"的确定降级语义，
            // 调用方按"未取到"处理；非法 JSON 无法解析。
        }

        return null;
    }
}
