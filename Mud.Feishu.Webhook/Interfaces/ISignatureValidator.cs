// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书事件签名验证器接口
/// 负责验证请求头签名（X-Lark-Signature），使用 SHA-256 算法
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
    /// <returns>
    /// 签名验证通过返回 true；<b>已判定签名无效</b>返回 false（终态，调用方应回 4xx）。
    /// </returns>
    /// <remarks>
    /// 签名计算方式：SHA-256(timestamp + nonce + encryptKey + body)
    /// 如果 headerSignature 为空且配置允许，可能跳过验证
    /// </remarks>
    /// <exception cref="Mud.Feishu.Webhook.Exceptions.FeishuWebhookUnavailableException">
    /// <b>无法完成判定</b>（配置构建失败、算法/实现内部异常）。语义 = 本请求未被消费，
    /// 调用方应回 503 + <c>Retry-After</c>；<b>不得</b>压缩成 <c>false</c>（那会被映射为 403 终态）。
    /// </exception>
    /// <exception cref="System.OperationCanceledException">调用方取消。</exception>
    Task<bool> ValidateHeaderSignatureAsync(long timestamp, string nonce, string body, string? headerSignature, string encryptKey);

    /// <summary>
    /// 验证请求头中的签名（使用 SHA-256 算法）——带 CancellationToken 的重载（WHF-R2/B3）。
    /// </summary>
    /// <param name="timestamp">请求时间戳</param>
    /// <param name="nonce">随机数</param>
    /// <param name="body">请求体内容</param>
    /// <param name="headerSignature">请求头 X-Lark-Signature 中的签名</param>
    /// <param name="encryptKey">加密密钥</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>
    /// 签名验证通过返回 true；<b>已判定签名无效</b>返回 false（终态，调用方应回 4xx）。
    /// </returns>
    /// <exception cref="Mud.Feishu.Webhook.Exceptions.FeishuWebhookUnavailableException">
    /// <b>无法完成判定</b>（配置构建失败、算法/实现内部异常）。语义 = 本请求未被消费，
    /// 调用方应回 503 + <c>Retry-After</c>；<b>不得</b>压缩成 <c>false</c>（那会被映射为 403 终态）。
    /// </exception>
    /// <exception cref="System.OperationCanceledException">调用方取消。</exception>
    Task<bool> ValidateHeaderSignatureAsync(long timestamp, string nonce, string body, string? headerSignature, string encryptKey, CancellationToken cancellationToken);
}