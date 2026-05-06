// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.DataModels;
using Mud.Feishu.Exceptions;
using Mud.Feishu.Abstractions.Authentication;
using Mud.HttpUtils;

namespace Mud.Feishu.Tests.Authentication.TokenManager;

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

        var result = await _appTokenManager.GetTokenAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expectedToken, result);
        _authenticationApiMock.Verify(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnNull_WhenApiReturnsNull()
    {
        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppCredentialResult?)null);

        await Assert.ThrowsAsync<FeishuException>(() => _appTokenManager.GetTokenAsync(CancellationToken.None));
        _authenticationApiMock.Verify(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowFeishuException_WhenApiReturnsError()
    {
        var apiResult = new AppCredentialResult
        {
            Code = 400,
            Msg = "Invalid app credentials"
        };

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        var exception = await Assert.ThrowsAsync<FeishuException>(() => _appTokenManager.GetTokenAsync(CancellationToken.None));
        Assert.Equal(400, exception.ErrorCode);
        Assert.Contains("Invalid app credentials", exception.Message);
        _authenticationApiMock.Verify(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldCacheToken_WhenTokenIsValid()
    {
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

        var result1 = await _appTokenManager.GetTokenAsync(CancellationToken.None);
        var result2 = await _appTokenManager.GetTokenAsync(CancellationToken.None);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(expectedToken, result1);
        Assert.Equal(result1, result2);

        _authenticationApiMock.Verify(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowTaskCanceledException_WhenNetworkTimeout()
    {
        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        await Assert.ThrowsAsync<TaskCanceledException>(() => _appTokenManager.GetTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowHttpRequestException_WhenNetworkError()
    {
        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        await Assert.ThrowsAsync<HttpRequestException>(() => _appTokenManager.GetTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowFeishuException_WhenHttp502()
    {
        var apiResult = new AppCredentialResult
        {
            Code = 502,
            Msg = "Bad Gateway"
        };

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        var exception = await Assert.ThrowsAsync<FeishuException>(() => _appTokenManager.GetTokenAsync(CancellationToken.None));
        Assert.Equal(502, exception.ErrorCode);
        Assert.Contains("Bad Gateway", exception.Message);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowFeishuException_WhenHttp503()
    {
        var apiResult = new AppCredentialResult
        {
            Code = 503,
            Msg = "Service Unavailable"
        };

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        var exception = await Assert.ThrowsAsync<FeishuException>(() => _appTokenManager.GetTokenAsync(CancellationToken.None));
        Assert.Equal(503, exception.ErrorCode);
        Assert.Contains("Service Unavailable", exception.Message);
    }
}

public class AppTokenManagerWithStoreTests : IDisposable
{
    private readonly Mock<IFeishuAuthentication> _authenticationApiMock;
    private readonly Mock<IEnhancedHttpClient> _httpClientMock;
    private readonly Mock<ITokenStore> _tokenStoreMock;
    private readonly FeishuAppConfig _config;
    private readonly FeishuAppContext _appContext;
    private readonly IAppTokenManager _appTokenManager;

    public AppTokenManagerWithStoreTests()
    {
        _authenticationApiMock = new Mock<IFeishuAuthentication>();
        _httpClientMock = new Mock<IEnhancedHttpClient>();
        _tokenStoreMock = new Mock<ITokenStore>();

        _config = new FeishuAppConfig
        {
            AppKey = "test",
            AppId = "test_app_id_1234567890",
            AppSecret = "test_app_secret_123456",
            TokenRefreshThreshold = 300
        };

        var loggerMock = new Mock<ILogger<TenantTokenManager>>();
        var appTokenManagerLoggerMock = new Mock<ILogger<AppTokenManager>>();
        var userTokenManagerLoggerMock = new Mock<ILogger<UserTokenManager>>();
        var currentUserContextMock = new Mock<IFeishuCurrentUserContext>();
        var optionsMock = new Mock<IOptions<FeishuAppConfig>>();
        optionsMock.Setup(x => x.Value).Returns(_config);

        var tenantTokenManager = new TenantTokenManager(
            _authenticationApiMock.Object,
            optionsMock.Object,
            loggerMock.Object,
            _tokenStoreMock.Object);

        var appTokenManager = new AppTokenManager(
            _authenticationApiMock.Object,
            optionsMock.Object,
            appTokenManagerLoggerMock.Object,
            _tokenStoreMock.Object);

        var userTokenManager = new UserTokenManager(
            currentUserContextMock.Object,
            _authenticationApiMock.Object,
            optionsMock.Object,
            userTokenManagerLoggerMock.Object);

        _appContext = new FeishuAppContext(
            _config,
            tenantTokenManager,
            appTokenManager,
            userTokenManager,
            _authenticationApiMock.Object,
            _httpClientMock.Object);

        _appTokenManager = _appContext.AppTokenManager;
    }

    public void Dispose()
    {
        _appContext?.Dispose();
    }

    [Fact]
    public async Task GetTokenAsync_ShouldRestoreFromStore_WhenStoreHasToken()
    {
        var storedToken = "stored-app-token";
        _tokenStoreMock
            .Setup(x => x.GetAccessTokenAsync("AppAccessToken:test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppCredentialResult
            {
                AppAccessToken = "api-app-token",
                Expire = 7200,
                Code = 0,
                Msg = "ok"
            });

        var result = await _appTokenManager.GetTokenAsync(CancellationToken.None);

        Assert.Equal(storedToken, result);
        _authenticationApiMock.Verify(
            x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldCallApi_WhenStoreReturnsNull()
    {
        _tokenStoreMock
            .Setup(x => x.GetAccessTokenAsync("AppAccessToken:test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var expectedToken = "api-app-token";
        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppCredentialResult
            {
                AppAccessToken = expectedToken,
                Expire = 7200,
                Code = 0,
                Msg = "ok"
            });

        var result = await _appTokenManager.GetTokenAsync(CancellationToken.None);

        Assert.Equal(expectedToken, result);
        _authenticationApiMock.Verify(
            x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldCallApi_WhenStoreThrowsException()
    {
        _tokenStoreMock
            .Setup(x => x.GetAccessTokenAsync("AppAccessToken:test", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Store error"));

        var expectedToken = "api-app-token";
        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppCredentialResult
            {
                AppAccessToken = expectedToken,
                Expire = 7200,
                Code = 0,
                Msg = "ok"
            });

        var result = await _appTokenManager.GetTokenAsync(CancellationToken.None);

        Assert.Equal(expectedToken, result);
        _authenticationApiMock.Verify(
            x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldPersistToken_WhenApiReturnsValidToken()
    {
        var expectedToken = "api-app-token";

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppCredentialResult
            {
                AppAccessToken = expectedToken,
                Expire = 7200,
                Code = 0,
                Msg = "ok"
            });

        await _appTokenManager.GetTokenAsync(CancellationToken.None);

        _tokenStoreMock.Verify(
            x => x.SetAccessTokenAsync("AppAccessToken:test", expectedToken, 7200, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
