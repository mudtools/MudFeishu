// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Exceptions;

/// <summary>
/// 飞书Webhook验证异常（请求格式验证、事件类型验证等）
/// </summary>
public class FeishuWebhookValidationException : FeishuWebhookException
{
    /// <summary>
    /// 验证字段名称
    /// </summary>
    public string? FieldName { get; }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookValidationException"/> 的新实例
    /// </summary>
    public FeishuWebhookValidationException(string message, string? fieldName = null, string? requestId = null)
        : base("ValidationError", message, requestId, isRetryable: false)
    {
        FieldName = fieldName;
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookValidationException"/> 的新实例
    /// </summary>
    public FeishuWebhookValidationException(string message, Exception innerException, string? fieldName = null, string? requestId = null)
        : base("ValidationError", message, innerException, requestId, isRetryable: false)
    {
        FieldName = fieldName;
    }

    /// <summary>
    /// 返回包含完整追踪信息的字符串表示
    /// </summary>
    public override string ToString()
    {
        var baseInfo = base.ToString();
        var fieldNameInfo = !string.IsNullOrEmpty(FieldName) ? $", FieldName: {FieldName}" : "";
        return baseInfo.Replace("}", $"}}{fieldNameInfo}");
    }
}
