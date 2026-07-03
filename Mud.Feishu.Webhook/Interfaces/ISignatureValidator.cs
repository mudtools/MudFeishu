// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书事件签名验证器接口
/// 负责验证请求体签名和请求头签名，支持 HMAC-SHA256 和 SHA-256 算法
/// </summary>
public interface ISignatureValidator
{
    /// <summary>
    /// 设置当前应用键（多应用场景）
    /// </summary>
    /// <param name="appKey">应用键，用于多应用场景下的上下文标识</param>
    void SetCurrentAppKey(string appKey);

    /// <summary>
    /// 验证请求头中的签名（使用 SHA-256 算法）
    /// </summary>
    /// <param name="timestamp">请求时间戳</param>
    /// <param name="nonce">随机数</param>
    /// <param name="body">请求体内容</param>
    /// <param name="headerSignature">请求头 X-Lark-Signature 中的签名</param>
    /// <param name="encryptKey">加密密钥</param>
    /// <returns>如果签名验证通过返回 true，否则返回 false</returns>
    /// <remarks>
    /// 签名计算方式：SHA-256(timestamp + nonce + encryptKey + body)
    /// 如果 headerSignature 为空且配置允许，可能跳过验证
    /// </remarks>
    Task<bool> ValidateHeaderSignatureAsync(long timestamp, string nonce, string body, string? headerSignature, string encryptKey);

    /// <summary>
    /// 验证请求体签名（使用 HMAC-SHA256 算法）
    /// </summary>
    /// <param name="timestamp">请求时间戳</param>
    /// <param name="nonce">随机数</param>
    /// <param name="encryptData">加密的请求数据</param>
    /// <param name="encryptKey">加密密钥</param>
    /// <param name="expectedSignature">期望的签名值（可选，当为 null 时跳过比较）</param>
    /// <returns>如果签名验证通过或配置禁用验证时返回 true，否则返回 false</returns>
    /// <remarks>
    /// 签名计算方式：HMAC-SHA256(encryptKey, timestamp + "\n" + nonce + "\n" + encryptData)
    /// 当 EnableBodySignatureValidation 为 false 时跳过验证并返回 true
    /// 当 expectedSignature 为 null 或空时，记录警告并返回 true（兼容旧版本）
    /// </remarks>
    Task<bool> ValidateBodySignatureAsync(long timestamp, string nonce, string encryptData, string encryptKey, string? expectedSignature = null);
}