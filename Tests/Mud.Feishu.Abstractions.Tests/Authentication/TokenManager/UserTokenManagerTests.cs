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
            Msg = "ok"
        };

        _authenticationApiMock
            .Setup(x => x.GetOAuthenAccessTokenAsync(It.IsAny<OAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        var result = await _userTokenManager.GetUserTokenWithCodeAsync("test-code", "https://example.com/callback", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("user-access-token-123", result.AccessToken);
        Assert.Equal("user-refresh-token-456", result.RefreshToken);
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
}
