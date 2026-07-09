// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书令牌管理器解析器接口
/// </summary>
/// <remarks>
/// 提供在多应用模式下按应用键解析令牌管理器的统一入口。
/// 这是多应用模式下获取令牌管理器的推荐方式，替代直接通过 DI 注入
/// <see cref="ITenantTokenManager"/> / <see cref="IAppTokenManager"/> / <see cref="IUserTokenManager"/>
/// （后者仅暴露默认应用的令牌管理器，无法切换到非默认应用）。
///
/// 使用示例：
/// <code>
/// public class MyService
/// {
///     private readonly IFeishuTokenManagerResolver _resolver;
///
///     public MyService(IFeishuTokenManagerResolver resolver)
///     {
///         _resolver = resolver;
///     }
///
///     // 使用默认应用的租户令牌
///     public async Task DoWithDefaultAppAsync()
///     {
///         var token = await _resolver.GetTenantTokenManager().GetTokenAsync();
///     }
///
///     // 使用指定应用的租户令牌
///     public async Task DoWithHrAppAsync()
///     {
///         var token = await _resolver.GetTenantTokenManager("hr-app").GetTokenAsync();
///     }
/// }
/// </code>
/// </remarks>
public interface IFeishuTokenManagerResolver
{
    /// <summary>
    /// 获取租户令牌管理器
    /// </summary>
    /// <param name="appKey">应用键，为 null 时使用默认应用</param>
    /// <returns>指定应用的租户令牌管理器</returns>
    /// <exception cref="InvalidOperationException">当指定应用不存在时抛出</exception>
    ITenantTokenManager GetTenantTokenManager(string? appKey = null);

    /// <summary>
    /// 获取应用令牌管理器
    /// </summary>
    /// <param name="appKey">应用键，为 null 时使用默认应用</param>
    /// <returns>指定应用的应用令牌管理器</returns>
    /// <exception cref="InvalidOperationException">当指定应用不存在时抛出</exception>
    IAppTokenManager GetAppTokenManager(string? appKey = null);

    /// <summary>
    /// 获取用户令牌管理器
    /// </summary>
    /// <param name="appKey">应用键，为 null 时使用默认应用</param>
    /// <returns>指定应用的用户令牌管理器</returns>
    /// <exception cref="InvalidOperationException">当指定应用不存在时抛出</exception>
    IFeishuUserTokenManager GetUserTokenManager(string? appKey = null);
}
