// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Webhook.Models;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书 Webhook 服务接口
/// </summary>
public interface IFeishuWebhookService
{
    /// <summary>
    /// 设置当前应用键（多应用场景）
    /// </summary>
    /// <param name="appKey">应用键</param>
    void SetCurrentAppKey(string appKey);

    /// <summary>
    /// 验证飞书事件订阅请求
    /// </summary>
    /// <param name="request">验证请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证响应</returns>
    Task<EventVerificationResponse?> VerifyEventSubscriptionAsync(EventVerificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理飞书事件推送（已解密）
    /// </summary>
    /// <param name="eventData">已解密的事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    /// <remarks>
    /// 使用场景：
    /// 1. 单元测试：直接传入解密后的 EventData 进行测试
    /// 2. 已解密场景：如果事件数据已经通过其他方式解密，可以直接调用此方法
    /// 3. 批量处理：处理已解密的事件队列
    ///
    /// 注意：此方法不会验证签名，仅处理已解密的数据。如需签名验证，
    /// 请使用 HandleEventAsync(FeishuWebhookRequest, string) 方法。
    /// </remarks>
    Task<(bool Success, string? ErrorReason)> HandleEventAsync(EventData eventData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证请求签名（使用 HMAC-SHA256 算法）
    /// </summary>
    /// <param name="request">Webhook 请求</param>
    /// <returns>是否验证通过</returns>
    [Obsolete("此方法使用 HMAC-SHA256 算法，与飞书官方签名算法（SHA-256）不一致，将在未来版本中移除。请使用 HandleEventAsync(FeishuWebhookRequest, string) 替代。")]
    Task<bool> ValidateRequestSignature(FeishuWebhookRequest request);

    /// <summary>
    /// 验证请求头签名（使用飞书官方 SHA-256 算法）
    /// </summary>
    /// <param name="request">Webhook 请求</param>
    /// <param name="body">原始请求体</param>
    /// <returns>签名验证是否通过</returns>
    Task<bool> HandleEventAsync(FeishuWebhookRequest request, string body);

    /// <summary>
    /// 解密事件数据
    /// </summary>
    /// <param name="encryptedData">加密数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解密后的事件数据</returns>
    Task<EventData?> DecryptEventAsync(string encryptedData, CancellationToken cancellationToken = default);
}