// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书当前用户上下文接口
/// </summary>
/// <remarks>
/// 提供当前请求的飞书用户身份信息访问能力。基于 AsyncLocal 实现，确保在异步上下文中正确传递用户信息。
/// 继承自 <see cref="ICurrentUserContext"/>，同时提供飞书特有的 OpenId、UnionId 等属性。
/// <para>UserId 回退机制：</para>
/// <see cref="ICurrentUserContext.UserId"/> 属性被源生成器用作用户令牌的缓存查找键。
/// 在飞书生态中，UserTokenManager 使用 <see cref="OpenId"/> 作为令牌缓存键。
/// 因此在 <see cref="SetUser"/> 时若 userId 参数未显式提供（null 或空白），
/// 实现类会自动将其回退到 openId，确保令牌查找键与存储键一致。
/// <para>典型使用场景：</para>
/// <list type="bullet">
///   <item><description>在业务服务中获取当前用户ID，用于查询用户令牌</description></item>
///   <item><description>在 API 控制器中获取当前用户信息进行权限验证</description></item>
///   <item><description>在后台任务中传递用户上下文</description></item>
/// </list>
/// <para>注意事项：</para>
/// <list type="bullet">
///   <item><description>此接口注册为 Singleton，内部使用 AsyncLocal 保证线程安全</description></item>
///   <item><description>用户信息在请求结束后自动清除，无需手动调用 Clear()</description></item>
/// </list>
/// </remarks>
public interface IFeishuCurrentUserContext : ICurrentUserContext
{
    /// <summary>
    /// 飞书用户 OpenId
    /// </summary>
    /// <remarks>
    /// 用户在当前应用下的唯一标识，用于调用飞书 API 时标识用户身份。
    /// </remarks>
    string? OpenId { get; }

    /// <summary>
    /// 飞书用户 UnionId
    /// </summary>
    /// <remarks>
    /// 用户在同一企业下的唯一标识，可用于跨应用识别用户。
    /// </remarks>
    string? UnionId { get; }


    /// <summary>
    /// 用户名称
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// 用户是否已认证
    /// </summary>
    /// <remarks>
    /// 当 OpenId 不为空时返回 true。
    /// </remarks>
    bool IsAuthenticated { get; }

    /// <summary>
    /// 设置用户信息
    /// </summary>
    /// <param name="openId">飞书用户 OpenId（必需，不能为 null、空字符串或纯空白字符）</param>
    /// <param name="unionId">飞书用户 UnionId（可选）</param>
    /// <param name="userId">业务系统用户ID（可选）。若未提供（null 或空白），将自动回退到 openId 作为令牌缓存键。仅当确需使用非 OpenId 的令牌缓存键时才显式设置。</param>
    /// <param name="name">用户名称（可选）</param>
    /// <remarks>
    /// 通常由中间件自动调用，业务代码不应直接调用此方法。
    /// <para>参数验证：</para>
    /// <list type="bullet">
    ///   <item><description>openId 不能为 null、空字符串或纯空白字符，否则抛出 ArgumentException</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentException">当 openId 为 null、空字符串或纯空白字符时抛出</exception>
    void SetUser(string openId, string? unionId = null, string? userId = null, string? name = null);

    /// <summary>
    /// 清除用户信息
    /// </summary>
    /// <remarks>
    /// 通常由中间件在请求结束时自动调用，业务代码不应直接调用此方法。
    /// </remarks>
    void Clear();
}
