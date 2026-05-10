// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Exceptions;

namespace Mud.Feishu.Webhook.Exceptions;

/// <summary>
/// 飞书Webhook异常基类
/// </summary>
public class FeishuWebhookException : FeishuException
{
    /// <summary>
    /// 错误类型
    /// </summary>
    public string ErrorType { get; }

    /// <summary>
    /// 请求ID（用于追踪）
    /// </summary>
    public string? RequestId { get; }

    /// <summary>
    /// 是否可重试
    /// </summary>
    public bool IsRetryable { get; }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookException"/> 的新实例
    /// </summary>
    public FeishuWebhookException(string errorType, string message, string? requestId = null, bool isRetryable = false)
        : base(message)
    {
        ErrorType = errorType;
        RequestId = requestId;
        IsRetryable = isRetryable;
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookException"/> 的新实例
    /// </summary>
    public FeishuWebhookException(string errorType, string message, Exception innerException, string? requestId = null, bool isRetryable = false)
        : base(message, innerException)
    {
        ErrorType = errorType;
        RequestId = requestId;
        IsRetryable = isRetryable;
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookException"/> 的新实例（带错误代码）
    /// </summary>
    public FeishuWebhookException(int errorCode, string errorType, string message, string? requestId = null, bool isRetryable = false)
        : base(errorCode, message)
    {
        ErrorType = errorType;
        RequestId = requestId;
        IsRetryable = isRetryable;
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookException"/> 的新实例（带错误代码和内部异常）
    /// </summary>
    public FeishuWebhookException(int errorCode, string errorType, string message, Exception innerException, string? requestId = null, bool isRetryable = false)
        : base(errorCode, message, innerException)
    {
        ErrorType = errorType;
        RequestId = requestId;
        IsRetryable = isRetryable;
    }

    /// <summary>
    /// 返回包含完整追踪信息的字符串表示
    /// </summary>
    public override string ToString()
    {
        var requestIdInfo = !string.IsNullOrEmpty(RequestId) ? $", RequestId: {RequestId}" : "";
        return $"{GetType().Name}: {Message}{requestIdInfo}, ErrorType: {ErrorType}, IsRetryable: {IsRetryable}{(InnerException != null ? $" -> {InnerException}" : "")}";
    }
}
