// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Abstractions.Context;
using Mud.AI.Abstractions.Conversations;

namespace Mud.AI.Agent.Context;

/// <summary>
/// 媒体消息上下文装配器（T3 / D3）的<b>中立基类</b>（N4 · E7 拆分）：
/// 把用户发送的媒体内容转写为文本注入 prompt——「转换在装配器内发生，模型只见文本结果」
/// （不把 OCR/STT 变成模型可调工具）。
/// </summary>
/// <remarks>
/// <para>
/// <b>拆分说明（方案 v2 §5.5 E7）</b>：机制（事实提取 → 文本类型短路 → 转写调用 → 截断 →
/// untrusted 头 → 失败隔离跳过）在此一份实现；<b>产品事实</b>（媒体类型判定、键提取、
/// 转写器适配、各类占位降级文案）由产品子类经 <see cref="TranscribeAsync"/> 钩子提供（③ 式）。
/// </para>
/// <para>
/// <b>软缺席</b>：转写器未注册是<b>产品侧</b>事实——子类以占位文本降级（绝不抛"缺能力"异常中断对话）。
/// </para>
/// <para>
/// <b>失败隔离</b>：转写抛错（除取消外）记 Information 后返回 <see langword="null"/>（跳过装配，
/// 不阻断对话）；取消原样传播。
/// </para>
/// <para>
/// <b>安全</b>：输出走 untrusted 头 + 单条长度闸（<c>perItemLength</c>，产品实测值经构造参数注入）。
/// </para>
/// </remarks>
public abstract class MediaContextAssembler : IContextAssembler
{
    private readonly int _order;
    private readonly string _untrustedHeader;
    private readonly string _truncationMarker;
    private readonly int _perItemLength;
    private readonly ILogger? _logger;

    /// <summary>
    /// 初始化媒体装配器基类。
    /// </summary>
    /// <param name="order">装配顺序（小者在前）。</param>
    /// <param name="untrustedHeader">untrusted 标注文案（<b>产品事实</b>，⑤ 式）。</param>
    /// <param name="truncationMarker">截断标记（<see cref="AssemblerBudgets.TruncationMarker"/>）。</param>
    /// <param name="perItemLength">单条媒体转写文本的长度闸（<b>产品事实</b>：与平台限制对齐）。</param>
    /// <param name="logger">日志（可空）。</param>
    protected MediaContextAssembler(
        int order,
        string untrustedHeader,
        string truncationMarker,
        int perItemLength,
        ILogger? logger = null)
    {
        _order = order;
        _untrustedHeader = untrustedHeader;
        _truncationMarker = truncationMarker;
        _perItemLength = perItemLength;
        _logger = logger;
    }

    /// <inheritdoc />
    public int Order => _order;

    /// <inheritdoc />
    public async Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrEmpty(request.MessageId))
        {
            return null;
        }

        // 从 EventFacts 中提取 message_type 和 content（由事件处理器写入）。
        var msgType = GetEventFact(request, "message_type");
        var content = GetEventFact(request, "content");

        if (string.IsNullOrEmpty(msgType) || string.IsNullOrEmpty(content))
        {
            return null;
        }

        // text 类型不处理（触发文本已由回合管线装配，R2-01b）。
        if (string.Equals(msgType, "text", StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            var text = await TranscribeAsync(request, msgType!, content!, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            // 长度截断（单条闸）。
            if (text.Length > _perItemLength)
            {
                text = text.Substring(0, _perItemLength) + _truncationMarker;
            }

            return $"{_untrustedHeader}\n[媒体内容] {text}";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogInformation(ex, "媒体消息装配失败（messageId: {MessageId}, type: {MessageType}）——跳过",
                request.MessageId, msgType);
            return null;
        }
    }

    /// <summary>
    /// 媒体转写钩子（<b>产品事实</b>）：按消息类型判定媒体形态、提取键、调转写器并产出文本
    /// （含全部占位降级文案；返回空/null = 本装配器对本次事件无贡献）。
    /// </summary>
    /// <param name="request">规范化会话请求（租户键 / 消息 ID 等）。</param>
    /// <param name="messageType">平台消息类型（已排除 text）。</param>
    /// <param name="content">类型化 JSON 载荷。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>转写文本（可空）。</returns>
    protected abstract Task<string?> TranscribeAsync(
        ConversationRequest request, string messageType, string content, CancellationToken cancellationToken);

    /// <summary>按键读取事件事实（机制侧公用：null 缺载荷语义）。</summary>
    protected static string? GetEventFact(ConversationRequest request, string key)
    {
        if (request.EventFacts is null)
        {
            return null;
        }

        foreach (var fact in request.EventFacts)
        {
            if (string.Equals(fact.Key, key, StringComparison.Ordinal))
            {
                return fact.Value;
            }
        }

        return null;
    }
}
