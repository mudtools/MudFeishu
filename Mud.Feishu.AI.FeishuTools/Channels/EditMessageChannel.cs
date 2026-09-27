// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Channels;
using Mud.Feishu.DataModels.Messages;
using System.Collections.Concurrent;
using System.Text;

namespace Mud.Feishu.AI.FeishuTools.Channels;

/// <summary>
/// 分片编辑流式通道（Phase 2 §3.1 首版降级实现）：发送占位文本消息 → 模型增量缓冲达到
/// <see cref="FeishuAgentOptions.MaxStreamChunkLength"/> 时用 <c>EditMessageAsync</c> 以
/// <b>累计全文</b> 编辑一次 → 结束时落地最终文本。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么不逐 token 编辑</b>：飞书编辑消息接口有频率限制，逐增量编辑必然触发限流；
/// 分片缓冲（默认 200 字符）把编辑次数压到「每 200 字符一次」，代价是出字粒度变粗。
/// 消息流卡片（<c>IFeishuTenantV2AppCardMessageStream</c>）为后续通道实现切换，不改变本抽象。
/// </para>
/// <para>
/// <b>失败语义</b>（接口契约）：<see cref="BeginAsync"/> 失败向上抛（调用方回退非流式路径，
/// 模型尚未调用、零重复成本）；<see cref="WriteStreamAsync"/>/<see cref="FlushAsync"/> 的单次
/// 编辑失败只记日志（对齐 I1 异常隔离精神）——占位消息停留在上一次成功内容，不中断模型流。
/// </para>
/// <para>
/// 每次下游调用前经 <see cref="IFeishuAppContextScopeFactory.BeginScope"/> 切换租户上下文
/// （与工具执行链同一事实来源；TMA2-20），finally 释放。
/// </para>
/// </remarks>
public sealed class EditMessageChannel : IMessageChannel
{
    /// <summary>占位消息初始文本（可见的「正在输入」占位；空文本会被飞书拒绝）。</summary>
    private const string PlaceholderText = "…";

    /// <summary>允许的 receive_id_type 白名单（防注入任意查询参数）。</summary>
    internal static readonly string[] AllowedReceiveIdTypes = ["chat_id", "open_id", "user_id", "union_id", "email"];

    private readonly IFeishuTenantV1Message _messageClient;
    private readonly IFeishuAppContextScopeFactory _scopeFactory;
    private readonly int _chunkLength;
    private readonly ConcurrentDictionary<string, StringBuilder> _buffers = new(StringComparer.Ordinal);
    private readonly ILogger? _logger;

    /// <summary>
    /// 初始化 <see cref="EditMessageChannel"/>。
    /// </summary>
    /// <param name="messageClient">消息客户端（Tenant 身份）。</param>
    /// <param name="scopeFactory">租户上下文作用域工厂。</param>
    /// <param name="options">Agent 配置（<see cref="FeishuAgentOptions.MaxStreamChunkLength"/> 消费点）。</param>
    /// <param name="logger">日志（可空）。</param>
    public EditMessageChannel(
        Mud.Feishu.IFeishuTenantV1Message messageClient,
        IFeishuAppContextScopeFactory scopeFactory,
        IOptions<FeishuAgentOptions> options,
        ILogger<EditMessageChannel>? logger = null)
    {
        _messageClient = messageClient ?? throw new ArgumentNullException(nameof(messageClient));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _chunkLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxStreamChunkLength;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> BeginAsync(string appKey, string chatId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("appKey 不能为空", nameof(appKey));
        if (string.IsNullOrWhiteSpace(chatId))
            throw new ArgumentException("chatId 不能为空", nameof(chatId));

        using var scope = _scopeFactory.BeginScope(appKey);
        var outcome = FeishuApiResultReader.Read(await _messageClient
            .SendMessageAsync(BuildTextRequest(chatId, PlaceholderText), "chat_id", cancellationToken)
            .ConfigureAwait(false));
        if (!outcome.Ok || string.IsNullOrEmpty(outcome.Data!.MessageId))
        {
            // Begin 失败向上抛：事件处理器回退非流式路径（模型尚未调用，零重复成本）。
            throw new InvalidOperationException(
                $"流式占位消息创建失败（chatId: {chatId}）: {outcome.ErrorText ?? "返回空 message_id"}");
        }

        return outcome.Data.MessageId!;
    }

    /// <inheritdoc />
    public async Task WriteStreamAsync(string appKey, string chatId, string messageId, string delta, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(delta))
        {
            return;
        }

        var buffer = _buffers.GetOrAdd(messageId, static _ => new StringBuilder());
        lock (buffer)
        {
            buffer.Append(delta);
            if (buffer.Length < _chunkLength)
            {
                return;
            }
        }

        await EditWithIsolationAsync(appKey, messageId, buffer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task FlushAsync(string appKey, string chatId, string messageId, CancellationToken cancellationToken = default)
    {
        if (_buffers.TryRemove(messageId, out var buffer) && buffer.Length > 0)
        {
            await EditWithIsolationAsync(appKey, messageId, buffer, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>按累计全文编辑一次消息；失败只记日志（失败隔离，不中断模型流）。</summary>
    private async Task EditWithIsolationAsync(string appKey, string messageId, StringBuilder buffer, CancellationToken cancellationToken)
    {
        string fullText;
        lock (buffer)
        {
            fullText = buffer.ToString();
        }

        try
        {
            using var scope = _scopeFactory.BeginScope(appKey);
            var outcome = FeishuApiResultReader.Read(await _messageClient
                .EditMessageAsync(messageId, BuildEditRequest(fullText), cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                _logger?.LogWarning(
                    "流式分片编辑失败（messageId: {MessageId}）: {Error}——停留上一次成功内容",
                    messageId, outcome.ErrorText);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "流式分片编辑异常（messageId: {MessageId}）——停留上一次成功内容", messageId);
        }
    }

    private static SendMessageRequest BuildTextRequest(string chatId, string text)
        => new()
        {
            ReceiveId = chatId,
            MsgType = "text",
            Content = BuildTextContent(text),
        };

    private static EditMessageRequest BuildEditRequest(string text)
        => new()
        {
            MsgType = "text",
            Content = BuildTextContent(text),
        };

    /// <summary>构造飞书 text 消息 content JSON（<c>{"text":"…"}</c>；JsonNode 转义，AOT 安全）。</summary>
    private static string BuildTextContent(string text)
        => new JsonObject { ["text"] = text }.ToJsonString();
}
