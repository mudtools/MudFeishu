// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Exceptions;

/// <summary>
/// 飞书 Webhook 安全异常
/// </summary>
public class FeishuWebhookSecurityException : FeishuWebhookException
{
    /// <summary>
    /// 客户端 IP
    /// </summary>
    public string? ClientIp { get; }

    /// <summary>
    /// 安全事件类型
    /// </summary>
    public SecurityEventType? SecurityEventType { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public FeishuWebhookSecurityException(
        string message,
        string? clientIp = null,
        SecurityEventType? securityEventType = null,
        string? requestId = null)
        : base("SecurityException", message, requestId: requestId)
    {
        ClientIp = clientIp;
        SecurityEventType = securityEventType;
    }

    /// <summary>
    /// 构造函数（带内部异常）
    /// </summary>
    public FeishuWebhookSecurityException(
        string message,
        Exception innerException,
        string? clientIp = null,
        SecurityEventType? securityEventType = null,
        string? requestId = null)
        : base("SecurityException", message, innerException, requestId: requestId)
    {
        ClientIp = clientIp;
        SecurityEventType = securityEventType;
    }
}
