// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 飞书应用级令牌管理器基类
/// </summary>
/// <remarks>
/// 提取 TenantTokenManager 和 AppTokenManager 的公共逻辑，
/// 包括令牌恢复、持久化、刷新等通用流程。
/// 继承 Mud.HttpUtils v2.0 的 TokenManagerBase，获得内置并发安全、自动清理、重试等能力。
/// </remarks>
internal abstract class FeishuAppTokenManagerBase : TokenManagerBase
{
    private readonly IFeishuAuthentication _authenticationApi;
    private readonly FeishuAppConfig _options;
    private readonly ILogger _logger;
    private readonly ITokenStore? _tokenStore;
    private readonly string _tokenTypeKey;

    protected IFeishuAuthentication AuthenticationApi => _authenticationApi;
    protected FeishuAppConfig Options => _options;
    protected string TokenTypeKey => _tokenTypeKey;

    protected FeishuAppTokenManagerBase(
        IFeishuAuthentication authenticationApi,
        IOptions<FeishuAppConfig> options,
        ILogger logger,
        ITokenStore? tokenStore,
        string tokenTypeKeyPrefix)
    {
        _authenticationApi = authenticationApi ?? throw new ArgumentNullException(nameof(authenticationApi));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenStore = tokenStore;
        _tokenTypeKey = $"{tokenTypeKeyPrefix}:{_options.AppKey}";
    }

    protected override int ExpireThresholdSeconds => _options.TokenRefreshThreshold;

    public override async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return await GetOrRefreshTokenAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task<CredentialToken> RefreshTokenCoreAsync(CancellationToken cancellationToken)
    {
        var restoredToken = await TryRestoreFromStoreAsync(cancellationToken).ConfigureAwait(false);
        if (restoredToken != null)
            return restoredToken;

        if (_options.EnableLogging)
            _logger.LogInformation("Refreshing {TokenType} for AppId: {AppId}", _tokenTypeKey, _options.AppId);

        var result = await RefreshTokenFromApiAsync(cancellationToken).ConfigureAwait(false);

        // v1.1 修复 P0：飞书 API 异常响应时 result.AccessToken 可能为 null，
        // 此时 PersistTokenAsync 会抛 ArgumentNullException（语义不准）。
        // 在此提前校验，抛 InvalidOperationException 并附带上下文信息，便于诊断。
        if (string.IsNullOrEmpty(result.AccessToken))
        {
            throw new InvalidOperationException(
                $"飞书 API 刷新 {_tokenTypeKey} 令牌失败：返回的 AccessToken 为空。AppId: {_options.AppId}");
        }

        await PersistTokenAsync(_tokenTypeKey, result.AccessToken, result.ExpireSeconds, cancellationToken).ConfigureAwait(false);

        return new CredentialToken
        {
            AccessToken = result.AccessToken,
            Expire = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (result.ExpireSeconds * 1000L)
        };
    }

    protected abstract Task<(string? AccessToken, int ExpireSeconds)> RefreshTokenFromApiAsync(CancellationToken cancellationToken);

    private async Task<CredentialToken?> TryRestoreFromStoreAsync(CancellationToken cancellationToken)
    {
        if (_tokenStore == null)
            return null;

        try
        {
            var storedValue = await _tokenStore.GetAccessTokenAsync(_tokenTypeKey, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(storedValue))
                return null;

            var (accessToken, expireTimestampMs) = TokenStoreHelper.DecodeStoredToken(storedValue!);

            if (expireTimestampMs > 0)
            {
                var remainingMs = expireTimestampMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (remainingMs <= _options.TokenRefreshThreshold * 1000L)
                {
                    if (_options.EnableLogging)
                        _logger.LogDebug("Restored token from ITokenStore is near expiration for AppId: {AppId}, skipping", _options.AppId);
                    return null;
                }

                if (_options.EnableLogging)
                    _logger.LogDebug("Restored token from ITokenStore for AppId: {AppId}, TokenType: {TokenType}", _options.AppId, _tokenTypeKey);
                return new CredentialToken
                {
                    AccessToken = accessToken,
                    Expire = expireTimestampMs
                };
            }

            if (_options.EnableLogging)
                _logger.LogDebug("Restored token from ITokenStore (no expiration info) for AppId: {AppId}, TokenType: {TokenType}", _options.AppId, _tokenTypeKey);
            // T-3 修复：原值为 TokenRefreshThreshold + 60，扣除刷新阈值后有效时间仅 60 秒，过于保守导致频繁刷新。
            // 飞书 tenant/app token 有效期通常为 2 小时（7200 秒），恢复无过期信息的令牌时使用 30 分钟（1800 秒）作为合理默认。
            // 扣除 TokenRefreshThreshold 后仍有充足有效窗口，避免不必要的令牌刷新。
            var safeExpireSeconds = Math.Max(_options.TokenRefreshThreshold + 300, 1800);
            return new CredentialToken
            {
                AccessToken = accessToken,
                Expire = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (safeExpireSeconds * 1000L)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore token from ITokenStore for AppId: {AppId}", _options.AppId);
        }

        return null;
    }

    private async Task PersistTokenAsync(string tokenType, string? accessToken, long expiresInSeconds, CancellationToken cancellationToken)
    {
        // v1.1 修正：参数为空属于状态无效而非参数传递错误，抛 InvalidOperationException 更准确。
        // 正常流程下 RefreshTokenCoreAsync 已在上游校验，此处为防御性检查。
        if (string.IsNullOrEmpty(accessToken))
            throw new InvalidOperationException($"持久化令牌失败：accessToken 为空。TokenType: {tokenType}");
        if (_tokenStore == null)
            return;

        try
        {
            var expireTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (expiresInSeconds * 1000L);
            var encodedValue = TokenStoreHelper.EncodeStoredToken(accessToken!, expireTimestampMs);
            await _tokenStore.SetAccessTokenAsync(tokenType, encodedValue, expiresInSeconds, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist token to ITokenStore for tokenType: {TokenType}", tokenType);
        }
    }

}
