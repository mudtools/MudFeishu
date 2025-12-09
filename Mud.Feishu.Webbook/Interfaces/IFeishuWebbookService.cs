// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Webbook.Configuration;

namespace Mud.Feishu.Webbook;

/// <summary>
/// 飞书 Webbook 服务接口
/// </summary>
public interface IFeishuWebbookService
{
    /// <summary>
    /// 验证飞书事件订阅请求
    /// </summary>
    /// <param name="request">验证请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证响应</returns>
    Task<EventVerificationResponse?> VerifyEventSubscriptionAsync(EventVerificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理飞书事件推送
    /// </summary>
    /// <param name="request">Webbook 请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    Task<bool> HandleEventAsync(FeishuWebbookRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证请求签名
    /// </summary>
    /// <param name="request">Webbook 请求</param>
    /// <returns>是否验证通过</returns>
    bool ValidateRequestSignature(FeishuWebbookRequest request);

    /// <summary>
    /// 解密事件数据
    /// </summary>
    /// <param name="encryptedData">加密数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解密后的事件数据</returns>
    Task<EventData?> DecryptEventAsync(string encryptedData, CancellationToken cancellationToken = default);
}