// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels;
using Mud.Feishu.Exceptions;

namespace Mud.Feishu.Tests.Authentication.TokenManager;

/// <summary>
/// 租户令牌管理器测试（通过 FeishuAppContext 接口测试）
/// </summary>
/// <remarks>
/// 由于 TenantTokenManager 现在是 internal 类，测试通过 FeishuAppContext 公开的 ITenantTokenManager 接口进行测试。
/// </remarks>
public class TenantTokenManagerTests : TokenManagerTestsBase
{
    private readonly ITenantTokenManager _tenantTokenManager;

    public TenantTokenManagerTests() : base()
    {
        _tenantTokenManager = AppContext.TenantTokenManager;
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnBearerToken_WhenApiReturnsValidToken()
    {
        // Arrange
        var expectedToken = "test-tenant-access-token";
        var tokenExpire = 7200;
        var apiResult = new TenantAppCredentialResult
        {
            TenantAccessToken = expectedToken,
            Expire = tokenExpire,
            Code = 0,
            Msg = "ok"
        };

        _authenticationApiMock
            .Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        // Act
        var result = await _tenantTokenManager.GetTokenAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedToken, result);
        _authenticationApiMock.Verify(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnNull_WhenApiReturnsNull()
    {
        // Arrange
        _authenticationApiMock
            .Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantAppCredentialResult?)null);

        // Act & Assert
        await Assert.ThrowsAsync<FeishuException>(() => _tenantTokenManager.GetTokenAsync(CancellationToken.None));
        _authenticationApiMock.Verify(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowFeishuException_WhenApiReturnsError()
    {
        // Arrange
        var apiResult = new TenantAppCredentialResult
        {
            Code = 400,
            Msg = "Invalid app credentials"
        };

        _authenticationApiMock
            .Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FeishuException>(() => _tenantTokenManager.GetTokenAsync(CancellationToken.None));
        Assert.Equal(400, exception.ErrorCode);
        Assert.Contains("Invalid app credentials", exception.Message);
        _authenticationApiMock.Verify(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldCacheToken_WhenTokenIsValid()
    {
        // Arrange
        var expectedToken = "test-tenant-access-token";
        var tokenExpire = 7200;
        var apiResult = new TenantAppCredentialResult
        {
            TenantAccessToken = expectedToken,
            Expire = tokenExpire,
            Code = 0,
            Msg = "ok"
        };

        var callCount = 0;
        _authenticationApiMock
            .Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return apiResult;
            });

        // Act - First call should get new token
        var result1 = await _tenantTokenManager.GetTokenAsync(CancellationToken.None);

        // Act - Second call should use cached token
        var result2 = await _tenantTokenManager.GetTokenAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(expectedToken, result1);
        Assert.Equal(result1, result2);

        // Verify API was only called once (second call used cache)
        _authenticationApiMock.Verify(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldRefreshToken_WhenTokenIsExpired()
    {
        var firstToken = "expired-tenant-token";
        var refreshedToken = "refreshed-tenant-token";
        var callCount = 0;

        _authenticationApiMock
            .Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new TenantAppCredentialResult
                {
                    TenantAccessToken = callCount == 1 ? firstToken : refreshedToken,
                    Expire = callCount == 1 ? 1 : 7200,
                    Code = 0,
                    Msg = "ok"
                };
            });

        var result1 = await _tenantTokenManager.GetTokenAsync(CancellationToken.None);
        Assert.Equal(firstToken, result1);

        var result2 = await _tenantTokenManager.GetTokenAsync(CancellationToken.None);
        Assert.Equal(refreshedToken, result2);

        _authenticationApiMock.Verify(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetTokenAsync_ShouldOnlyCallApiOnce_WhenConcurrentRequests()
    {
        var expectedToken = "concurrent-tenant-token";
        var callCount = 0;
        var tcs = new TaskCompletionSource<TenantAppCredentialResult>();

        _authenticationApiMock
            .Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return new TenantAppCredentialResult
                {
                    TenantAccessToken = expectedToken,
                    Expire = 7200,
                    Code = 0,
                    Msg = "ok"
                };
            });

        var tasks = new Task<string>[5];
        for (var i = 0; i < 5; i++)
        {
            tasks[i] = _tenantTokenManager.GetTokenAsync(CancellationToken.None);
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(expectedToken, r));
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task InvalidateTokenAsync_ShouldForceRefreshOnNextCall()
    {
        var firstToken = "first-tenant-token";
        var secondToken = "second-tenant-token";
        var callCount = 0;

        _authenticationApiMock
            .Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new TenantAppCredentialResult
                {
                    TenantAccessToken = callCount == 1 ? firstToken : secondToken,
                    Expire = 7200,
                    Code = 0,
                    Msg = "ok"
                };
            });

        var result1 = await _tenantTokenManager.GetTokenAsync(CancellationToken.None);
        Assert.Equal(firstToken, result1);

        await _tenantTokenManager.InvalidateTokenAsync(cancellationToken: CancellationToken.None);

        var result2 = await _tenantTokenManager.GetTokenAsync(CancellationToken.None);
        Assert.Equal(secondToken, result2);

        _authenticationApiMock.Verify(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
