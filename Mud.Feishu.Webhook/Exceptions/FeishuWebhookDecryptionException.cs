// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Exceptions;

/// <summary>
/// 飞书Webhook解密异常（事件解密失败、密钥错误等）
/// </summary>
public class FeishuWebhookDecryptionException : FeishuWebhookException
{
    /// <summary>
    /// 应用ID
    /// </summary>
    public string? AppId { get; }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookDecryptionException"/> 的新实例
    /// </summary>
    public FeishuWebhookDecryptionException(string message, string? appId = null, string? requestId = null)
        : base("DecryptionError", message, requestId, isRetryable: false)
    {
        AppId = appId;
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookDecryptionException"/> 的新实例
    /// </summary>
    public FeishuWebhookDecryptionException(string message, Exception innerException, string? appId = null, string? requestId = null)
        : base("DecryptionError", message, innerException, requestId, isRetryable: false)
    {
        AppId = appId;
    }
}
