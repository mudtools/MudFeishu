// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Exceptions;

/// <summary>
/// 飞书Webhook序列化异常（JSON反序列化失败等）
/// </summary>
public class FeishuWebhookSerializationException : FeishuWebhookException
{
    /// <summary>
    /// 序列化格式（如：JSON）
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookSerializationException"/> 的新实例
    /// </summary>
    public FeishuWebhookSerializationException(string message, string format = "JSON", string? requestId = null)
        : base("SerializationError", message, requestId, isRetryable: false)
    {
        Format = format;
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookSerializationException"/> 的新实例
    /// </summary>
    public FeishuWebhookSerializationException(string message, Exception innerException, string format = "JSON", string? requestId = null)
        : base("SerializationError", message, innerException, requestId, isRetryable: false)
    {
        Format = format;
    }
}
