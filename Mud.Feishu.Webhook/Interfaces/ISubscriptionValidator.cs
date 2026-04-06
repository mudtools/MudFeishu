// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Models;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书事件订阅验证器接口
/// 负责验证飞书事件订阅请求的有效性，包括请求类型、Token 和 Challenge 字段
/// </summary>
public interface ISubscriptionValidator
{
    /// <summary>
    /// 设置当前应用键（多应用场景）
    /// </summary>
    /// <param name="appKey">应用键，用于多应用场景下的上下文标识</param>
    void SetCurrentAppKey(string appKey);

    /// <summary>
    /// 验证事件订阅请求
    /// </summary>
    /// <param name="request">事件验证请求对象</param>
    /// <param name="expectedToken">期望的验证 Token</param>
    /// <returns>如果验证通过返回 true，否则返回 false</returns>
    /// <remarks>
    /// 验证规则：
    /// <para>1. 请求类型必须为 "url_verification"</para>
    /// <para>2. Token 字段不能为空且必须与期望值匹配</para>
    /// <para>3. Challenge 字段不能为空</para>
    /// <para>验证失败时会记录相应的警告日志</para>
    /// </remarks>
    [Obsolete("建议使用异步版本 ValidateSubscriptionRequestAsync")]
    bool ValidateSubscriptionRequest(EventVerificationRequest request, string expectedToken);

    /// <summary>
    /// 异步验证事件订阅请求
    /// </summary>
    /// <param name="request">事件验证请求对象</param>
    /// <param name="expectedToken">期望的验证 Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果验证通过返回 true，否则返回 false</returns>
    /// <remarks>
    /// 验证规则：
    /// <para>1. 请求类型必须为 "url_verification"</para>
    /// <para>2. Token 字段不能为空且必须与期望值匹配</para>
    /// <para>3. Challenge 字段不能为空</para>
    /// <para>验证失败时会记录相应的警告日志</para>
    /// </remarks>
    Task<bool> ValidateSubscriptionRequestAsync(EventVerificationRequest request, string expectedToken, CancellationToken cancellationToken = default);
}