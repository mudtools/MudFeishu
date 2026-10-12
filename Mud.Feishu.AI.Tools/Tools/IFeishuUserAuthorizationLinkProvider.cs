// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 用户授权入口链接提供者（T1 / D1，软缺席契约）。
/// </summary>
/// <remarks>
/// <para>
/// 宿主可选注册：当 <c>user_auth_required</c> 错误需要附带授权入口链接时，
/// 宿主实现本接口以提供 OAuth 授权页 URL。
/// </para>
/// <para>
/// <b>软缺席</b>：未注册时错误文案只有文字指引（「请转告用户完成飞书授权」），
/// SDK <b>不拼</b>授权页 URL——授权入口是宿主的 UX 决策（对齐 <see cref="IFeishuAttachmentStager"/> 同款机制）。
/// </para>
/// <para>
/// <b>凭据安全</b>：返回的 URL 不得包含 access_token / refresh_token；
/// 可包含 app_id / redirect_uri / state 等 OAuth 参数。
/// </para>
/// </remarks>
public interface IFeishuUserAuthorizationLinkProvider
{
    /// <summary>
    /// 获取用户授权入口链接。
    /// </summary>
    /// <param name="appKey">应用键（用于解析 app_id / redirect_uri）。</param>
    /// <param name="userId">用户标识（open_id，用于 state 关联；可空）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>授权页 URL；返回 null 表示不提供链接（降级为纯文字指引）。</returns>
    Task<string?> GetAuthorizationLinkAsync(string appKey, string? userId, CancellationToken cancellationToken = default);
}
