// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Configuration;


/// <summary>
/// 单个应用的配置
/// </summary>
public class FeishuAppWebhookOptions
{
    /// <summary>
    /// 应用键（用于标识应用）
    /// </summary>
    public string AppKey { get; set; } = string.Empty;

    /// <summary>
    /// 应用验证 Token
    /// </summary>
    public string VerificationToken { get; set; } = string.Empty;

    /// <summary>
    /// 事件加密 Key
    /// </summary>
    public string EncryptKey { get; set; } = string.Empty;

    /// <summary>
    /// 时间戳容错范围（秒），默认 -1 表示继承全局配置
    /// 设置为正整数时使用应用级配置，设置为 -1 或 0 时继承全局 TimestampToleranceSeconds
    /// </summary>
    public int TimestampToleranceSeconds { get; set; } = -1;

    /// <summary>
    /// 是否强制验证请求头签名，默认 false
    /// </summary>
    public bool EnforceHeaderSignatureValidation { get; set; } = false;

    /// <summary>
    /// 是否启用请求体签名验证，默认 true
    /// 如果 Middleware 中已验证 X-Lark-Signature 请求头，可禁用此选项以避免重复验证
    /// </summary>
    public bool EnableBodySignatureValidation { get; set; } = true;

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    /// <exception cref="InvalidOperationException">配置无效时抛出</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AppKey))
            throw new InvalidOperationException("AppKey 不能为空");

        if (string.IsNullOrWhiteSpace(VerificationToken))
            throw new InvalidOperationException("VerificationToken 不能为空");

        if (string.IsNullOrWhiteSpace(EncryptKey))
            throw new InvalidOperationException("EncryptKey 不能为空");

        if (EncryptKey.Length != 32)
            throw new InvalidOperationException("EncryptKey 长度必须为 32 字符");

        // TimestampToleranceSeconds: -1 表示继承全局配置，0 或正整数表示应用级配置
        // 不需要验证负数，因为 -1 是合法的特殊值
    }
}
