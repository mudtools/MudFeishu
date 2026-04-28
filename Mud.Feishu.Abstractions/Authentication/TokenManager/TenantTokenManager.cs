// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Exceptions;

namespace Mud.Feishu.TokenManager;

/// <summary>
/// 租户令牌管理器
/// </summary>
/// <remarks>
/// 负责租户访问令牌（Tenant Access Token）的获取、缓存和管理。
/// 租户令牌用于租户级别的权限验证，通过AppId和AppSecret获取。
/// 继承 Mud.HttpUtils v2.0 的 TokenManagerBase，获得内置并发安全、自动清理、重试等能力。
/// 可选注入 ITokenStore 实现分布式令牌持久化（如 Redis）。
/// </remarks>
internal class TenantTokenManager : TokenManagerBase, ITenantTokenManager
{
    private readonly IFeishuAuthentication _authenticationApi;
    private readonly FeishuAppConfig _options;
    private readonly ILogger<TenantTokenManager> _logger;
    private readonly ITokenStore? _tokenStore;

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
    {
        _authenticationApi = authenticationApi ?? throw new ArgumentNullException(nameof(authenticationApi));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenStore = tokenStore;
    }

    /// <summary>
    /// 令牌过期提前量（秒），提前刷新令牌
    /// </summary>
    protected override int ExpireThresholdSeconds => _options.TokenRefreshThreshold;

    /// <inheritdoc />
    public override async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return await GetOrRefreshTokenAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 刷新令牌的核心实现
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>凭证令牌</returns>
    protected override async Task<CredentialToken> RefreshTokenCoreAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing TenantAccessToken for AppId: {AppId}", _options.AppId);

        var credentials = new AppCredentials
        {
            AppId = _options.AppId,
            AppSecret = _options.AppSecret
        };

        var res = await _authenticationApi.GetTenantAccessTokenAsync(credentials, cancellationToken);

        if (res == null || res.Code != 0)
        {
            throw new FeishuException(res?.Code ?? 500, $"获取 TenantAccessToken 失败: {res?.Msg ?? "返回结果为null"}");
        }

        if (string.IsNullOrEmpty(res.TenantAccessToken))
        {
            throw new FeishuException(443, "获取 TenantAccessToken 失败: AccessToken为空");
        }

        var token = new CredentialToken
        {
            AccessToken = res.TenantAccessToken,
            Expire = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ((res.Expire > 0 ? res.Expire : 7200) * 1000L)
        };

        await PersistTokenAsync("TenantAccessToken", token.AccessToken, res.Expire > 0 ? res.Expire : 7200, cancellationToken).ConfigureAwait(false);

        return token;
    }

    private async Task PersistTokenAsync(string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken)
    {
        if (_tokenStore == null)
            return;

        try
        {
            await _tokenStore.SetAccessTokenAsync(tokenType, accessToken, expiresInSeconds, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist token to ITokenStore for tokenType: {TokenType}", tokenType);
        }
    }
}
