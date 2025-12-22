// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书事件验证服务接口
/// </summary>
public interface IFeishuEventValidator
{
    /// <summary>
    /// 验证事件订阅请求
    /// </summary>
    /// <param name="request">验证请求</param>
    /// <param name="expectedToken">期望的验证 Token</param>
    /// <returns>是否验证通过</returns>
    bool ValidateSubscriptionRequest(EventVerificationRequest request, string expectedToken);

    /// <summary>
    /// 验证请求签名
    /// </summary>
    /// <param name="timestamp">时间戳</param>
    /// <param name="nonce">随机数</param>
    /// <param name="encrypt">加密数据</param>
    /// <param name="signature">签名</param>
    /// <param name="encryptKey">加密密钥</param>
    /// <returns>是否验证通过</returns>
    bool ValidateSignature(long timestamp, string nonce, string encrypt, string signature, string encryptKey);

    /// <summary>
    /// 验证时间戳是否在有效范围内
    /// </summary>
    /// <param name="timestamp">时间戳</param>
    /// <param name="toleranceSeconds">容错秒数</param>
    /// <returns>是否有效</returns>
    bool ValidateTimestamp(long timestamp, int toleranceSeconds = 300);
}