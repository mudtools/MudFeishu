// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.Exceptions;

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 租户令牌管理器
/// </summary>
/// <remarks>
/// 负责租户访问令牌（Tenant Access Token）的获取、缓存和管理。
/// 租户令牌用于租户级别的权限验证，通过AppId和AppSecret获取。
/// 继承 Mud.HttpUtils v2.0 的 TokenManagerBase，获得内置并发安全、自动清理、重试等能力。
/// 可选注入 ITokenStore 实现分布式令牌持久化（如 Redis）。
/// </remarks>
internal class TenantTokenManager : FeishuAppTokenManagerBase, ITenantTokenManager
{
    /// <summary>
    /// 初始化 TenantTokenManager 实例
    /// </summary>
    /// <param name="authenticationApi">飞书认证API接口</param>
    /// <param name="options">飞书配置选项</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="tokenStore">令牌持久化存储（可选，用于分布式部署）</param>
    public TenantTokenManager(
        IFeishuAuthentication authenticationApi,
        IOptions<FeishuAppConfig> options,
        ILogger<TenantTokenManager> logger,
        ITokenStore? tokenStore = null)
        : base(authenticationApi, options, logger, tokenStore, "TenantAccessToken")
    {
    }

    protected override async Task<(string? AccessToken, int ExpireSeconds)> RefreshTokenFromApiAsync(CancellationToken cancellationToken)
    {
        var credentials = new AppCredentials
        {
            AppId = Options.AppId,
            AppSecret = Options.AppSecret
        };

        var res = await AuthenticationApi.GetTenantAccessTokenAsync(credentials, cancellationToken);

        if (res == null || res.Code != 0)
        {
            throw new FeishuException(res?.Code ?? 500, $"获取 TenantAccessToken 失败: {res?.Msg ?? "返回结果为null"}");
        }

        if (string.IsNullOrEmpty(res.TenantAccessToken))
        {
            throw new FeishuException(443, "获取 TenantAccessToken 失败: AccessToken为空");
        }

        return (res.TenantAccessToken, res.Expire > 0 ? res.Expire : 7200);
    }
}
