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
/// 应用令牌管理器测试（通过 FeishuAppContext 接口测试）
/// </summary>
/// <remarks>
/// 由于 AppTokenManager 现在是 internal 类，测试通过 FeishuAppContext 公开的 IAppTokenManager 接口进行测试。
/// </remarks>
public class AppTokenManagerTests : TokenManagerTestsBase
{
    private readonly IAppTokenManager _appTokenManager;

    public AppTokenManagerTests() : base()
    {
        _appTokenManager = AppContext.AppTokenManager;
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnBearerToken_WhenApiReturnsValidToken()
    {
        // Arrange
        var expectedToken = "test-app-access-token";
        var tokenExpire = 7200;
        var apiResult = new AppCredentialResult
        {
            AppAccessToken = expectedToken,
            Expire = tokenExpire,
            Code = 0,
            Msg = "ok"
        };

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        // Act
        var result = await _appTokenManager.GetTokenAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedToken, result);
        _authenticationApiMock.Verify(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnNull_WhenApiReturnsNull()
    {
        // Arrange
        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppCredentialResult?)null);

        // Act & Assert
        await Assert.ThrowsAsync<FeishuException>(() => _appTokenManager.GetTokenAsync(CancellationToken.None));
        _authenticationApiMock.Verify(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowFeishuException_WhenApiReturnsError()
    {
        // Arrange
        var apiResult = new AppCredentialResult
        {
            Code = 400,
            Msg = "Invalid app credentials"
        };

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FeishuException>(() => _appTokenManager.GetTokenAsync(CancellationToken.None));
        Assert.Equal(400, exception.ErrorCode);
        Assert.Contains("Invalid app credentials", exception.Message);
        _authenticationApiMock.Verify(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldCacheToken_WhenTokenIsValid()
    {
        // Arrange
        var expectedToken = "test-app-access-token";
        var tokenExpire = 7200;
        var apiResult = new AppCredentialResult
        {
            AppAccessToken = expectedToken,
            Expire = tokenExpire,
            Code = 0,
            Msg = "ok"
        };

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        // Act - First call should get new token
        var result1 = await _appTokenManager.GetTokenAsync(CancellationToken.None);

        // Act - Second call should use cached token
        var result2 = await _appTokenManager.GetTokenAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(expectedToken, result1);
        Assert.Equal(result1, result2);

        // Verify API was only called once (second call used cache)
        _authenticationApiMock.Verify(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowTaskCanceledException_WhenNetworkTimeout()
    {
        // Arrange
        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => _appTokenManager.GetTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowHttpRequestException_WhenNetworkError()
    {
        // Arrange
        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _appTokenManager.GetTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowFeishuException_WhenHttp502()
    {
        // Arrange
        var apiResult = new AppCredentialResult
        {
            Code = 502,
            Msg = "Bad Gateway"
        };

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FeishuException>(() => _appTokenManager.GetTokenAsync(CancellationToken.None));
        Assert.Equal(502, exception.ErrorCode);
        Assert.Contains("Bad Gateway", exception.Message);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowFeishuException_WhenHttp503()
    {
        // Arrange
        var apiResult = new AppCredentialResult
        {
            Code = 503,
            Msg = "Service Unavailable"
        };

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FeishuException>(() => _appTokenManager.GetTokenAsync(CancellationToken.None));
        Assert.Equal(503, exception.ErrorCode);
        Assert.Contains("Service Unavailable", exception.Message);
    }
}
