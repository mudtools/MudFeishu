// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels;
using Mud.Feishu.Exceptions;

namespace Mud.Feishu.Tests.Authentication.TokenManager;

public class UserTokenManagerTests : TokenManagerTestsBase
{
    private readonly IFeishuUserTokenManager _userTokenManager;

    public UserTokenManagerTests() : base()
    {
        _userTokenManager = AppContext.UserTokenManager;
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowArgumentException_WhenUserIdIsNull()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _userTokenManager.GetTokenAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnNull_WhenNoTokenInCache()
    {
        var result = await _userTokenManager.GetTokenAsync("user123", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task HasValidTokenAsync_ShouldReturnFalse_WhenNoTokenInCache()
    {
        var result = await _userTokenManager.HasValidTokenAsync("user123", CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task CanRefreshTokenAsync_ShouldReturnFalse_WhenNoTokenInCache()
    {
        var result = await _userTokenManager.CanRefreshTokenAsync("user123", CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task GetTokenInfoAsync_ShouldReturnNull_WhenNoTokenInCache()
    {
        var result = await _userTokenManager.GetTokenInfoAsync("user123", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTokenInfoAsync_ShouldReturnNull_WhenUserIdIsEmpty()
    {
        var result = await _userTokenManager.GetTokenInfoAsync("", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserTokenWithCodeAsync_ShouldThrowFeishuException_WhenApiReturnsNull()
    {
        _authenticationApiMock
            .Setup(x => x.GetOAuthenAccessTokenAsync(It.IsAny<OAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthCredentialsResult?)null);

        var exception = await Assert.ThrowsAsync<FeishuException>(() =>
            _userTokenManager.GetUserTokenWithCodeAsync("test-code", "https://example.com/callback", CancellationToken.None));

        Assert.Contains("返回结果为null", exception.Message);
    }

    [Fact]
    public async Task GetUserTokenWithCodeAsync_ShouldThrowFeishuException_WhenApiReturnsError()
    {
        _authenticationApiMock
            .Setup(x => x.GetOAuthenAccessTokenAsync(It.IsAny<OAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthCredentialsResult { Code = 400, Msg = "Invalid code" });

        var exception = await Assert.ThrowsAsync<FeishuException>(() =>
            _userTokenManager.GetUserTokenWithCodeAsync("test-code", "https://example.com/callback", CancellationToken.None));

        Assert.Equal(400, exception.ErrorCode);
    }

    [Fact]
    public async Task GetUserTokenWithCodeAsync_ShouldReturnTokenInfo_WhenApiSucceeds()
    {
        var apiResult = new OAuthCredentialsResult
        {
            AccessToken = "user-access-token-123",
            RefreshToken = "user-refresh-token-456",
            ExpiresIn = 7200,
            RefreshTokenExpiresIn = 2592000,
            Code = 0,
            Msg = "ok",
            OpenId = "test-open-id",
            UnionId = "test-union-id"
        };

        _authenticationApiMock
            .Setup(x => x.GetOAuthenAccessTokenAsync(It.IsAny<OAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        var result = await _userTokenManager.GetUserTokenWithCodeAsync("test-code", "https://example.com/callback", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("user-access-token-123", result.AccessToken);
        Assert.Equal("user-refresh-token-456", result.RefreshToken);
    }

    /// <summary>
    /// MT-07 / TMX-22（Mud.HttpUtils 2.0.5）：换取的令牌必须填充 IssuedAt。
    /// 组件仅在 IssuedAt &gt; 0 时才启用「TTL 感知的过期提前量」min(配置阈值, ttl/2)；
    /// 缺失会退化为纯配置阈值，使短 TTL 令牌"刚签发即被判为需刷新"、缓存永不命中。
    /// </summary>
    [Fact]
    public async Task GetUserTokenWithCodeAsync_ShouldPopulateIssuedAt_MatchingAccessTokenExpiry()
    {
        var apiResult = new OAuthCredentialsResult
        {
            AccessToken = "issued-at-access",
            RefreshToken = "issued-at-refresh",
            ExpiresIn = 7200,
            RefreshTokenExpiresIn = 2592000,
            Code = 0,
            Msg = "ok",
            OpenId = "issued-at-open-id"
        };

        _authenticationApiMock
            .Setup(x => x.GetOAuthenAccessTokenAsync(It.IsAny<OAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = await _userTokenManager.GetUserTokenWithCodeAsync("test-code", "https://example.com/callback", CancellationToken.None);
        var after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        Assert.NotNull(result);
        Assert.InRange(result.IssuedAt, before, after);
        Assert.Equal(result.IssuedAt + 7200_000L, result.AccessTokenExpireTime);
    }

    /// <summary>
    /// MT-07 / TMX-22：TTL 短于配置阈值时，TTL 感知阈值必须生效——
    /// ttl=120s、阈值=300s 时提前量被钳位为 60s，签发瞬间令牌仍是"有效"的。
    /// 若 IssuedAt 未填充（= 0），提前量恒为 300s &gt; ttl，该断言必然失败。
    /// </summary>
    [Fact]
    public async Task GetUserTokenWithCodeAsync_ShouldKeepShortTtlTokenValid_RightAfterIssuance()
    {
        var apiResult = new OAuthCredentialsResult
        {
            AccessToken = "short-ttl-access",
            RefreshToken = "short-ttl-refresh",
            ExpiresIn = 120,
            RefreshTokenExpiresIn = 2592000,
            Code = 0,
            Msg = "ok",
            OpenId = "short-ttl-open-id"
        };

        _authenticationApiMock
            .Setup(x => x.GetOAuthenAccessTokenAsync(It.IsAny<OAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        var result = await _userTokenManager.GetUserTokenWithCodeAsync("test-code", "https://example.com/callback", CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IssuedAt > 0, "短 TTL 令牌必须填充 IssuedAt，否则 TTL 感知阈值失效");
        Assert.True(result.IsAccessTokenValid(300),
            "ttl=120s 的令牌在签发瞬间应判定为有效（提前量被钳位为 ttl/2=60s）");
    }

    [Fact]
    public async Task RemoveTokenAsync_ShouldReturnFalse_WhenUserIdIsEmpty()
    {
        var result = await _userTokenManager.RemoveTokenAsync("", CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveTokenAsync_ShouldReturnTrue_WhenUserIdIsValid()
    {
        var tokenInfo = new UserTokenInfo
        {
            AccessToken = "token-to-remove",
            RefreshToken = "refresh-to-remove",
            AccessTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 7200000L,
            RefreshTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 2592000000L
        };

        await _userTokenManager.StoreUserTokenAsync("user1", tokenInfo, CancellationToken.None);

        var removeResult = await _userTokenManager.RemoveTokenAsync("user1", CancellationToken.None);
        Assert.True(removeResult);

        var infoAfterRemove = await _userTokenManager.GetTokenInfoAsync("user1", CancellationToken.None);
        Assert.Null(infoAfterRemove);
    }

    [Fact]
    public async Task RefreshUserTokenAsync_ShouldReturnNull_WhenNoCachedToken()
    {
        var result = await _userTokenManager.RefreshUserTokenAsync("unknown-user", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshUserTokenAsync_ShouldReturnNull_WhenNoRefreshToken()
    {
        var tokenInfo = new UserTokenInfo
        {
            AccessToken = "access-no-refresh",
            RefreshToken = null,
            AccessTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 7200000L
        };

        await _userTokenManager.StoreUserTokenAsync("user-no-refresh", tokenInfo, CancellationToken.None);

        var result = await _userTokenManager.RefreshUserTokenAsync("user-no-refresh", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshUserTokenAsync_ShouldRefreshToken_WhenRefreshTokenExists()
    {
        var tokenInfo = new UserTokenInfo
        {
            UserId = "user1",
            AccessToken = "initial-access",
            RefreshToken = "initial-refresh",
            AccessTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 7200000L,
            RefreshTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 2592000000L
        };

        var refreshedResult = new OAuthCredentialsResult
        {
            AccessToken = "refreshed-access",
            RefreshToken = "refreshed-refresh",
            ExpiresIn = 7200,
            RefreshTokenExpiresIn = 2592000,
            Code = 0,
            Msg = "ok"
        };

        _authenticationApiMock
            .Setup(x => x.GetOAuthenRefreshAccessTokenAsync(It.IsAny<OAuthRefreshTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshedResult);

        await _userTokenManager.StoreUserTokenAsync("user1", tokenInfo, CancellationToken.None);

        var result = await _userTokenManager.RefreshUserTokenAsync("user1", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("refreshed-access", result.AccessToken);
        Assert.Equal("refreshed-refresh", result.RefreshToken);
    }

    [Fact]
    public async Task StoreUserTokenAsync_ShouldCacheToken_ForSubsequentRetrieval()
    {
        var tokenInfo = new UserTokenInfo
        {
            UserId = "user1",
            AccessToken = "stored-access",
            RefreshToken = "stored-refresh",
            AccessTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 7200000L,
            RefreshTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 2592000000L
        };

        await _userTokenManager.StoreUserTokenAsync("user1", tokenInfo, CancellationToken.None);

        var retrieved = await _userTokenManager.GetTokenInfoAsync("user1", CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal("stored-access", retrieved.AccessToken);
        Assert.Equal("stored-refresh", retrieved.RefreshToken);
    }

    [Fact]
    public async Task HasValidTokenAsync_ShouldReturnTrue_WhenTokenIsCached()
    {
        var tokenInfo = new UserTokenInfo
        {
            AccessToken = "valid-access",
            RefreshToken = "valid-refresh",
            AccessTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 7200000L,
            RefreshTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 2592000000L
        };

        await _userTokenManager.StoreUserTokenAsync("user1", tokenInfo, CancellationToken.None);

        var result = await _userTokenManager.HasValidTokenAsync("user1", CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public async Task CanRefreshTokenAsync_ShouldReturnTrue_WhenRefreshTokenExists()
    {
        var tokenInfo = new UserTokenInfo
        {
            AccessToken = "valid-access",
            RefreshToken = "valid-refresh",
            AccessTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 7200000L,
            RefreshTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 2592000000L
        };

        await _userTokenManager.StoreUserTokenAsync("user1", tokenInfo, CancellationToken.None);

        var result = await _userTokenManager.CanRefreshTokenAsync("user1", CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public async Task GetUserTokenWithCodeAsync_ShouldAutoResolveOpenId_WhenOAuthV2NotReturnOpenId()
    {
        // OAuth v2 端点不返回 OpenId
        var apiResult = new OAuthCredentialsResult
        {
            AccessToken = "v2-access-token",
            RefreshToken = "v2-refresh-token",
            ExpiresIn = 7200,
            RefreshTokenExpiresIn = 2592000,
            Code = 0,
            Msg = "ok",
            OpenId = null,
            UnionId = null
        };

        // 用户信息 API 返回 OpenId
        var userInfoResult = new FeishuApiResult<GetUserDataResult>
        {
            Code = 0,
            Msg = "ok",
            Data = new GetUserDataResult
            {
                OpenId = "resolved-open-id",
                UnionId = "resolved-union-id",
                Name = "Test User"
            }
        };

        _authenticationApiMock
            .Setup(x => x.GetOAuthenAccessTokenAsync(It.IsAny<OAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);
        _authenticationApiMock
            .Setup(x => x.GetUserInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfoResult);

        var result = await _userTokenManager.GetUserTokenWithCodeAsync("test-code", "https://example.com/callback", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("v2-access-token", result.AccessToken);
        Assert.Equal("resolved-open-id", result.OpenId);
        Assert.Equal("resolved-union-id", result.UnionId);
        Assert.Equal("resolved-open-id", result.UserId);

        // 验证令牌已缓存
        var cachedInfo = await _userTokenManager.GetTokenInfoAsync("resolved-open-id", CancellationToken.None);
        Assert.NotNull(cachedInfo);
        Assert.Equal("v2-access-token", cachedInfo.AccessToken);
    }

    [Fact]
    public async Task GetUserTokenWithCodeAsync_ShouldThrowFeishuException_WhenUserInfoApiFailsAndOpenIdIsEmpty()
    {
        // OAuth v2 端点不返回 OpenId
        var apiResult = new OAuthCredentialsResult
        {
            AccessToken = "v2-access-token",
            RefreshToken = "v2-refresh-token",
            ExpiresIn = 7200,
            RefreshTokenExpiresIn = 2592000,
            Code = 0,
            Msg = "ok",
            OpenId = null
        };

        _authenticationApiMock
            .Setup(x => x.GetOAuthenAccessTokenAsync(It.IsAny<OAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);
        _authenticationApiMock
            .Setup(x => x.GetUserInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeishuApiResult<GetUserDataResult>?)null);

        var exception = await Assert.ThrowsAsync<FeishuException>(() =>
            _userTokenManager.GetUserTokenWithCodeAsync("test-code", "https://example.com/callback", CancellationToken.None));

        Assert.Contains("获取用户信息失败", exception.Message);
    }

    [Fact]
    public async Task GetTokenAsync_WithoutUserId_ShouldThrowInvalidOperationException_WhenUserNotAuthenticated()
    {
        CurrentUserContextMock.Setup(x => x.IsAuthenticated).Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userTokenManager.GetTokenAsync(CancellationToken.None));
    }
}
