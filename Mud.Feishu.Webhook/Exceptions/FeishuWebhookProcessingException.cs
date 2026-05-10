// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Exceptions;

/// <summary>
/// 飞书Webhook处理异常（事件处理失败、处理器异常等）
/// </summary>
public class FeishuWebhookProcessingException : FeishuWebhookException
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public string? EventType { get; }

    /// <summary>
    /// 事件ID
    /// </summary>
    public string? EventId { get; }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookProcessingException"/> 的新实例
    /// </summary>
    public FeishuWebhookProcessingException(string message, string? eventType = null, string? eventId = null, string? requestId = null)
        : base("ProcessingError", message, requestId, isRetryable: true)
    {
        EventType = eventType;
        EventId = eventId;
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookProcessingException"/> 的新实例
    /// </summary>
    public FeishuWebhookProcessingException(string message, Exception innerException, string? eventType = null, string? eventId = null, string? requestId = null)
        : base("ProcessingError", message, innerException, requestId, isRetryable: true)
    {
        EventType = eventType;
        EventId = eventId;
    }

    /// <summary>
    /// 返回包含完整追踪信息的字符串表示
    /// </summary>
    public override string ToString()
    {
        var baseInfo = base.ToString();
        var eventTypeInfo = !string.IsNullOrEmpty(EventType) ? $", EventType: {EventType}" : "";
        var eventIdInfo = !string.IsNullOrEmpty(EventId) ? $", EventId: {EventId}" : "";
        return baseInfo.Replace("}", $"}}{eventTypeInfo}{eventIdInfo}");
    }
}
