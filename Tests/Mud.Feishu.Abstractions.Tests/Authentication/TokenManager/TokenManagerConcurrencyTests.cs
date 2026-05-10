// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.DataModels;

namespace Mud.Feishu.Tests.Authentication.TokenManager;

public class AppTokenManagerConcurrencyTests : IDisposable
{
    private readonly Mock<IFeishuAuthentication> _authenticationApiMock;
    private readonly Mock<IEnhancedHttpClient> _httpClientMock;
    private readonly FeishuAppConfig _config;
    private readonly FeishuAppContext _appContext;
    private readonly IAppTokenManager _appTokenManager;

    public AppTokenManagerConcurrencyTests()
    {
        _authenticationApiMock = new Mock<IFeishuAuthentication>();
        _httpClientMock = new Mock<IEnhancedHttpClient>();

        _config = new FeishuAppConfig
        {
            AppKey = "concurrency_test",
            AppId = "test_app_id_concurrency",
            AppSecret = "test_app_secret_concurrency",
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
            loggerMock.Object);

        var appTokenManager = new AppTokenManager(
            _authenticationApiMock.Object,
            optionsMock.Object,
            appTokenManagerLoggerMock.Object);

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
    public async Task GetTokenAsync_ShouldOnlyCallApiOnce_WhenConcurrentRequests()
    {
        var expectedToken = "concurrent-app-token";
        var callCount = 0;

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return new AppCredentialResult
                {
                    AppAccessToken = expectedToken,
                    Expire = 7200,
                    Code = 0,
                    Msg = "ok"
                };
            });

        var tasks = new Task<string>[5];
        for (var i = 0; i < 5; i++)
        {
            tasks[i] = _appTokenManager.GetTokenAsync(CancellationToken.None);
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(expectedToken, r));
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldHandleHighConcurrency_When20ConcurrentRequests()
    {
        var expectedToken = "high-concurrency-app-token";
        var callCount = 0;

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                Task.Delay(50).Wait();
                return new AppCredentialResult
                {
                    AppAccessToken = expectedToken,
                    Expire = 7200,
                    Code = 0,
                    Msg = "ok"
                };
            });

        var tasks = new Task<string>[20];
        for (var i = 0; i < 20; i++)
        {
            tasks[i] = _appTokenManager.GetTokenAsync(CancellationToken.None);
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(expectedToken, r));
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task InvalidateTokenAsync_ShouldForceRefreshOnNextCall()
    {
        var firstToken = "first-app-token";
        var secondToken = "second-app-token";
        var callCount = 0;

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new AppCredentialResult
                {
                    AppAccessToken = callCount == 1 ? firstToken : secondToken,
                    Expire = 7200,
                    Code = 0,
                    Msg = "ok"
                };
            });

        var result1 = await _appTokenManager.GetTokenAsync(CancellationToken.None);
        Assert.Equal(firstToken, result1);

        await _appTokenManager.InvalidateTokenAsync(cancellationToken: CancellationToken.None);

        var result2 = await _appTokenManager.GetTokenAsync(CancellationToken.None);
        Assert.Equal(secondToken, result2);

        _authenticationApiMock.Verify(
            x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task InvalidateTokenAsync_ShouldNotAffectConcurrentReads_WhenCalledDuringAccess()
    {
        var expectedToken = "stable-app-token";
        var callCount = 0;

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return new AppCredentialResult
                {
                    AppAccessToken = expectedToken,
                    Expire = 7200,
                    Code = 0,
                    Msg = "ok"
                };
            });

        var result1 = await _appTokenManager.GetTokenAsync(CancellationToken.None);
        Assert.Equal(expectedToken, result1);

        var invalidateTask = _appTokenManager.InvalidateTokenAsync(cancellationToken: CancellationToken.None);
        var readTask = _appTokenManager.GetTokenAsync(CancellationToken.None);

        await Task.WhenAll(invalidateTask, readTask);

        _authenticationApiMock.Verify(
            x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    public async Task GetTokenAsync_ShouldRefreshToken_WhenTokenIsExpired()
    {
        var firstToken = "expired-app-token";
        var refreshedToken = "refreshed-app-token";
        var callCount = 0;

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new AppCredentialResult
                {
                    AppAccessToken = callCount == 1 ? firstToken : refreshedToken,
                    Expire = callCount == 1 ? 1 : 7200,
                    Code = 0,
                    Msg = "ok"
                };
            });

        var result1 = await _appTokenManager.GetTokenAsync(CancellationToken.None);
        Assert.Equal(firstToken, result1);

        var result2 = await _appTokenManager.GetTokenAsync(CancellationToken.None);
        Assert.Equal(refreshedToken, result2);

        _authenticationApiMock.Verify(
            x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnSameToken_WhenCalledSequentially()
    {
        var expectedToken = "sequential-app-token";

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppCredentialResult
            {
                AppAccessToken = expectedToken,
                Expire = 7200,
                Code = 0,
                Msg = "ok"
            });

        for (int i = 0; i < 10; i++)
        {
            var result = await _appTokenManager.GetTokenAsync(CancellationToken.None);
            Assert.Equal(expectedToken, result);
        }

        _authenticationApiMock.Verify(
            x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldHandleApiLatency_WhenConcurrentRequests()
    {
        var expectedToken = "latency-app-token";
        var callCount = 0;

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                Task.Delay(200).Wait();
                return new AppCredentialResult
                {
                    AppAccessToken = expectedToken,
                    Expire = 7200,
                    Code = 0,
                    Msg = "ok"
                };
            });

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _appTokenManager.GetTokenAsync(CancellationToken.None))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(expectedToken, r));
        Assert.Equal(1, callCount);
    }
}

public class TenantTokenManagerConcurrencyTests : IDisposable
{
    private readonly Mock<IFeishuAuthentication> _authenticationApiMock;
    private readonly Mock<IEnhancedHttpClient> _httpClientMock;
    private readonly FeishuAppConfig _config;
    private readonly FeishuAppContext _appContext;
    private readonly ITenantTokenManager _tenantTokenManager;

    public TenantTokenManagerConcurrencyTests()
    {
        _authenticationApiMock = new Mock<IFeishuAuthentication>();
        _httpClientMock = new Mock<IEnhancedHttpClient>();

        _config = new FeishuAppConfig
        {
            AppKey = "tenant_concurrency_test",
            AppId = "test_app_id_tenant_concurrency",
            AppSecret = "test_app_secret_tenant_concurrency",
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
            loggerMock.Object);

        var appTokenManager = new AppTokenManager(
            _authenticationApiMock.Object,
            optionsMock.Object,
            appTokenManagerLoggerMock.Object);

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

        _tenantTokenManager = _appContext.TenantTokenManager;
    }

    public void Dispose()
    {
        _appContext?.Dispose();
    }

    [Fact]
    public async Task GetTokenAsync_ShouldHandleHighConcurrency_When20ConcurrentRequests()
    {
        var expectedToken = "high-concurrency-tenant-token";
        var callCount = 0;

        _authenticationApiMock
            .Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                Task.Delay(50).Wait();
                return new TenantAppCredentialResult
                {
                    TenantAccessToken = expectedToken,
                    Expire = 7200,
                    Code = 0,
                    Msg = "ok"
                };
            });

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => _tenantTokenManager.GetTokenAsync(CancellationToken.None))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(expectedToken, r));
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task InvalidateTokenAsync_ShouldHandleRapidInvalidations()
    {
        var expectedToken = "rapid-invalidation-tenant-token";
        var callCount = 0;

        _authenticationApiMock
            .Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return new TenantAppCredentialResult
                {
                    TenantAccessToken = $"{expectedToken}-{callCount}",
                    Expire = 7200,
                    Code = 0,
                    Msg = "ok"
                };
            });

        await _tenantTokenManager.GetTokenAsync(CancellationToken.None);

        for (int i = 0; i < 5; i++)
        {
            await _tenantTokenManager.InvalidateTokenAsync(cancellationToken: CancellationToken.None);
        }

        var result = await _tenantTokenManager.GetTokenAsync(CancellationToken.None);
        Assert.NotNull(result);

        _authenticationApiMock.Verify(
            x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    public async Task GetTokenAsync_ShouldHandleApiLatency_WhenConcurrentRequests()
    {
        var expectedToken = "latency-tenant-token";
        var callCount = 0;

        _authenticationApiMock
            .Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                Task.Delay(200).Wait();
                return new TenantAppCredentialResult
                {
                    TenantAccessToken = expectedToken,
                    Expire = 7200,
                    Code = 0,
                    Msg = "ok"
                };
            });

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _tenantTokenManager.GetTokenAsync(CancellationToken.None))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(expectedToken, r));
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldRefreshToken_WhenTokenExpiresDuringOperation()
    {
        var firstToken = "expiring-tenant-token";
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
    }

    [Fact]
    public async Task InvalidateAndRefresh_ShouldNotLoseToken_WhenCalledRapidly()
    {
        var tokenPrefix = "rapid-refresh-token";
        var callCount = 0;

        _authenticationApiMock
            .Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return new TenantAppCredentialResult
                {
                    TenantAccessToken = $"{tokenPrefix}-{callCount}",
                    Expire = 7200,
                    Code = 0,
                    Msg = "ok"
                };
            });

        var tasks = new List<Task<string>>();
        for (int i = 0; i < 3; i++)
        {
            tasks.Add(_tenantTokenManager.GetTokenAsync(CancellationToken.None));
            tasks.Add(_tenantTokenManager.InvalidateTokenAsync(cancellationToken: CancellationToken.None).ContinueWith(_ => _tenantTokenManager.GetTokenAsync(CancellationToken.None)).Unwrap());
        }

        var results = await Task.WhenAll(tasks);
        Assert.All(results, r => Assert.NotNull(r));
    }
}

public class TokenManagerWithStoreConcurrencyTests : IDisposable
{
    private readonly Mock<IFeishuAuthentication> _authenticationApiMock;
    private readonly Mock<IEnhancedHttpClient> _httpClientMock;
    private readonly Mock<ITokenStore> _tokenStoreMock;
    private readonly FeishuAppConfig _config;
    private readonly FeishuAppContext _appContext;
    private readonly IAppTokenManager _appTokenManager;
    private readonly ITenantTokenManager _tenantTokenManager;

    public TokenManagerWithStoreConcurrencyTests()
    {
        _authenticationApiMock = new Mock<IFeishuAuthentication>();
        _httpClientMock = new Mock<IEnhancedHttpClient>();
        _tokenStoreMock = new Mock<ITokenStore>();

        _config = new FeishuAppConfig
        {
            AppKey = "store_concurrency_test",
            AppId = "test_app_id_store_concurrency",
            AppSecret = "test_app_secret_store_concurrency",
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
        _tenantTokenManager = _appContext.TenantTokenManager;
    }

    public void Dispose()
    {
        _appContext?.Dispose();
    }

    [Fact]
    public async Task GetTokenAsync_ShouldRestoreFromStore_WhenConcurrentRequests()
    {
        var storedToken = "stored-concurrent-app-token";
        _tokenStoreMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _appTokenManager.GetTokenAsync(CancellationToken.None))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(storedToken, r));
        _authenticationApiMock.Verify(
            x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldCallApi_WhenStoreFailsConcurrently()
    {
        _tokenStoreMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Store connection failed"));

        var expectedToken = "api-fallback-concurrent-token";
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
        _tokenStoreMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var expectedToken = "persist-concurrent-token";
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
            x => x.SetAccessTokenAsync(It.IsAny<string>(), It.Is<string>(s => s.EndsWith("|" + expectedToken)), 7200, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldNotThrow_WhenStorePersistFails()
    {
        _tokenStoreMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _tokenStoreMock
            .Setup(x => x.SetAccessTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Persist failed"));

        var expectedToken = "persist-fail-token";
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
    }

    [Fact]
    public async Task GetTokenAsync_ShouldRestoreTenantTokenFromStore_WhenConcurrentRequests()
    {
        var storedToken = "stored-concurrent-tenant-token";
        _tokenStoreMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _tenantTokenManager.GetTokenAsync(CancellationToken.None))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(storedToken, r));
        _authenticationApiMock.Verify(
            x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InvalidateTokenAsync_ShouldTriggerApiCallOnNextRequest()
    {
        var storedToken = "stored-before-invalidate";
        _tokenStoreMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        var result1 = await _tenantTokenManager.GetTokenAsync(CancellationToken.None);
        Assert.Equal(storedToken, result1);

        await _tenantTokenManager.InvalidateTokenAsync(cancellationToken: CancellationToken.None);

        var newToken = "new-api-token";
        _tokenStoreMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _authenticationApiMock
            .Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantAppCredentialResult
            {
                TenantAccessToken = newToken,
                Expire = 7200,
                Code = 0,
                Msg = "ok"
            });

        var result2 = await _tenantTokenManager.GetTokenAsync(CancellationToken.None);
        Assert.Equal(newToken, result2);
    }
}

public class FeishuTokenStoreBoundaryTests : IDisposable
{
    private readonly MemoryCache _cache;
    private readonly FeishuTokenStore _sut;

    public FeishuTokenStoreBoundaryTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new FeishuTokenStore(_cache);
    }

    public void Dispose()
    {
        _cache?.Dispose();
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldThrowArgumentOutOfRangeException_WhenZeroExpiry()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _sut.SetAccessTokenAsync("zero-expiry", "token-zero", 0));
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldThrowArgumentOutOfRangeException_WhenNegativeExpiry()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _sut.SetAccessTokenAsync("negative-expiry", "token-negative", -1));
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldHandleVeryLongExpiry()
    {
        await _sut.SetAccessTokenAsync("long-expiry", "token-long", int.MaxValue);

        var result = await _sut.GetAccessTokenAsync("long-expiry");
        Assert.Equal("token-long", result);
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldOverwriteExistingToken()
    {
        await _sut.SetAccessTokenAsync("overwrite", "token-v1", 7200);
        await _sut.SetAccessTokenAsync("overwrite", "token-v2", 7200);

        var result = await _sut.GetAccessTokenAsync("overwrite");
        Assert.Equal("token-v2", result);
    }

    [Fact]
    public async Task RemoveAsync_ShouldNotThrow_WhenTokenNotExists()
    {
        await _sut.RemoveAsync("nonexistent");
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldHandleEmptyTokenValue()
    {
        await _sut.SetAccessTokenAsync("empty-token", "", 7200);

        var result = await _sut.GetAccessTokenAsync("empty-token");
        Assert.Equal("", result);
    }

    [Fact]
    public async Task SetRefreshTokenAsync_ShouldOverwriteExistingToken()
    {
        await _sut.SetRefreshTokenAsync("overwrite-refresh", "refresh-v1");
        await _sut.SetRefreshTokenAsync("overwrite-refresh", "refresh-v2");

        var result = await _sut.GetRefreshTokenAsync("overwrite-refresh");
        Assert.Equal("refresh-v2", result);
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldNotCorruptData()
    {
        const int iterations = 100;

        var setTasks = Enumerable.Range(0, iterations)
            .Select(i => _sut.SetAccessTokenAsync($"key-{i % 10}", $"token-{i}", 7200))
            .ToArray();

        await Task.WhenAll(setTasks);

        var getTasks = Enumerable.Range(0, iterations)
            .Select(i => _sut.GetAccessTokenAsync($"key-{i % 10}"))
            .ToArray();

        var results = await Task.WhenAll(getTasks);

        Assert.All(results, r => Assert.NotNull(r));
    }
}

public class FeishuUserTokenStoreBoundaryTests : IDisposable
{
    private readonly MemoryCache _cache;
    private readonly FeishuTokenStore _innerStore;
    private readonly FeishuUserTokenStore _sut;

    public FeishuUserTokenStoreBoundaryTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _innerStore = new FeishuTokenStore(_cache);
        _sut = new FeishuUserTokenStore(_innerStore, _cache);
    }

    public void Dispose()
    {
        _cache?.Dispose();
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldThrowArgumentOutOfRangeException_WhenZeroExpiry()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _sut.SetAccessTokenAsync("user1", "zero-expiry", "token-zero", 0));
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldHandleVeryLongExpiry()
    {
        await _sut.SetAccessTokenAsync("user1", "long-expiry", "token-long", int.MaxValue);

        var result = await _sut.GetAccessTokenAsync("user1", "long-expiry");
        Assert.Equal("token-long", result);
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldOverwriteExistingUserToken()
    {
        await _sut.SetAccessTokenAsync("user1", "UserAccessToken", "token-v1", 7200);
        await _sut.SetAccessTokenAsync("user1", "UserAccessToken", "token-v2", 7200);

        var result = await _sut.GetAccessTokenAsync("user1", "UserAccessToken");
        Assert.Equal("token-v2", result);
    }

    [Fact]
    public async Task RemoveAsync_ShouldNotThrow_WhenUserTokenNotExists()
    {
        await _sut.RemoveAsync("nonexistent-user", "UserAccessToken");
    }

    [Fact]
    public async Task ConcurrentUserAccess_ShouldNotCorruptData()
    {
        const int iterations = 50;

        var setTasks = Enumerable.Range(0, iterations)
            .Select(i => _sut.SetAccessTokenAsync($"user-{i % 10}", "UserAccessToken", $"token-{i}", 7200))
            .ToArray();

        await Task.WhenAll(setTasks);

        var getTasks = Enumerable.Range(0, iterations)
            .Select(i => _sut.GetAccessTokenAsync($"user-{i % 10}", "UserAccessToken"))
            .ToArray();

        var results = await Task.WhenAll(getTasks);

        Assert.All(results, r => Assert.NotNull(r));
    }

    [Fact]
    public async Task MultipleUsers_ShouldHaveIsolatedTokens()
    {
        for (int i = 0; i < 10; i++)
        {
            await _sut.SetAccessTokenAsync($"user-{i}", "UserAccessToken", $"access-{i}", 7200);
            await _sut.SetRefreshTokenAsync($"user-{i}", "UserAccessToken", $"refresh-{i}");
        }

        for (int i = 0; i < 10; i++)
        {
            var access = await _sut.GetAccessTokenAsync($"user-{i}", "UserAccessToken");
            var refresh = await _sut.GetRefreshTokenAsync($"user-{i}", "UserAccessToken");
            Assert.Equal($"access-{i}", access);
            Assert.Equal($"refresh-{i}", refresh);
        }
    }

    [Fact]
    public async Task RemoveAsync_ShouldOnlyAffectTargetUser()
    {
        for (int i = 0; i < 5; i++)
        {
            await _sut.SetAccessTokenAsync($"user-{i}", "UserAccessToken", $"access-{i}", 7200);
        }

        await _sut.RemoveAsync("user-2", "UserAccessToken");

        Assert.Null(await _sut.GetAccessTokenAsync("user-2", "UserAccessToken"));
        for (int i = 0; i < 5; i++)
        {
            if (i == 2) continue;
            Assert.Equal($"access-{i}", await _sut.GetAccessTokenAsync($"user-{i}", "UserAccessToken"));
        }
    }
}
