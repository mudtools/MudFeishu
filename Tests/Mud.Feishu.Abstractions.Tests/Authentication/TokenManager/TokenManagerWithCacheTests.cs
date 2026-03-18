// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.Exceptions;
using Mud.Feishu.TokenManager;
using Mud.HttpUtils;

namespace Mud.Feishu.Abstractions.Tests.Authentication.TokenManager;

/// <summary>
/// TokenManagerWithCache 基类测试
/// </summary>
/// <remarks>
/// 测试 TokenManagerWithCache 的核心功能，包括缓存、重试、统计等。
/// 由于 TokenManagerWithCache 是抽象类，通过测试实现类来验证其行为。
/// </remarks>
public class TokenManagerWithCacheTests
{
    // 测试常量
    private const string TestAppKey = "test";
    private const string TestAppId = "test_app_id_1234567890";
    private const string TestAppSecret = "test_app_secret_123456";
    private const int TestTokenRefreshThreshold = 300;

    private readonly Mock<IFeishuAuthentication> _authenticationApiMock;
    private readonly Mock<IOptions<FeishuAppConfig>> _optionsMock;
    private readonly Mock<ILogger<TokenManagerWithCache>> _loggerMock;
    private readonly Mock<ITokenCache> _tokenCacheMock;
    private readonly TestTokenManager _testTokenManager;

    public TokenManagerWithCacheTests()
    {
        _authenticationApiMock = new Mock<IFeishuAuthentication>();
        _loggerMock = new Mock<ILogger<TokenManagerWithCache>>();
        _tokenCacheMock = new Mock<ITokenCache>();

        // 创建测试用的 FeishuAppConfig
        var config = new FeishuAppConfig
        {
            AppKey = TestAppKey,
            AppId = TestAppId,
            AppSecret = TestAppSecret,
            TokenRefreshThreshold = TestTokenRefreshThreshold
        };
        _optionsMock = new Mock<IOptions<FeishuAppConfig>>();
        _optionsMock.Setup(x => x.Value).Returns(config);
        _tokenCacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _tokenCacheMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _tokenCacheMock.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0));

        _testTokenManager = new TestTokenManager(
            _authenticationApiMock.Object,
            _optionsMock.Object,
            _loggerMock.Object,
            _tokenCacheMock.Object);
    }

    // 测试用的具体实现类
    private class TestTokenManager : TokenManagerWithCache
    {
        public TestTokenManager(
            IFeishuAuthentication authenticationApi,
            IOptions<FeishuAppConfig> options,
            ILogger<TokenManagerWithCache> logger,
            ITokenCache tokenCache)
            : base(authenticationApi, options, logger, tokenCache, Mud.HttpUtils.TokenType.AppAccessToken)
        { }

        protected override async Task<CredentialToken?> AcquireNewTokenAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(10); // 模拟异步延迟
            return new CredentialToken
            {
                AccessToken = "test-access-token",
                Expire = 7200, // 2小时后过期（相对时间，秒）
                Code = 0,
                Msg = "ok"
            };
        }
    }

    [Fact]
    public async Task GetTokenAsync_ShouldHandleRetries_WhenAcquireNewTokenFails()
    {
        // Arrange
        var testTokenManager = new FailingTestTokenManager(
            _authenticationApiMock.Object,
            _optionsMock.Object,
            _loggerMock.Object,
            _tokenCacheMock.Object);

        // Act & Assert
        // TokenManagerWithCache should wrap exception in FeishuException after retries
        var exception = await Assert.ThrowsAsync<FeishuException>(() => testTokenManager.GetTokenAsync(CancellationToken.None));
        Assert.Equal(500, exception.ErrorCode);
        Assert.Contains("Failed to acquire AppAccessToken", exception.Message);
    }

    [Fact]
    public async Task GetCacheStatistics_ShouldReturnCorrectCounts()
    {
        // Arrange
        _tokenCacheMock.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((5, 2));

        // Act
        var stats = await _testTokenManager.GetCacheStatisticsAsync();

        // Assert
        Assert.Equal(5, stats.Total);
        Assert.Equal(2, stats.Expired);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnCachedToken_WhenCacheHasValidToken()
    {
        // Arrange
        var cachedToken = "cached-token"; // 缓存中存储不带前缀的原始token
        _tokenCacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedToken);

        // Act
        var token = await _testTokenManager.GetTokenAsync(CancellationToken.None);

        // Assert - 返回时应带有 Bearer 前缀
        Assert.Equal($"Bearer {cachedToken}", token);
        _tokenCacheMock.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldAcquireNewToken_WhenCacheIsEmpty()
    {
        // Arrange
        _tokenCacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var token = await _testTokenManager.GetTokenAsync(CancellationToken.None);

        // Assert - 返回时应带有 Bearer 前缀，缓存存储不带前缀
        Assert.NotNull(token);
        Assert.StartsWith("Bearer ", token);
        Assert.Contains("test-access-token", token);
        _tokenCacheMock.Verify(x => x.SetAsync(It.IsAny<string>(), "test-access-token", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // 用于测试重试机制的失败令牌管理器
    private class FailingTestTokenManager : TokenManagerWithCache
    {
        private int _retryCount = 0;

        public FailingTestTokenManager(
            IFeishuAuthentication authenticationApi,
            IOptions<FeishuAppConfig> options,
            ILogger<TokenManagerWithCache> logger,
            ITokenCache tokenCache)
            : base(authenticationApi, options, logger, tokenCache, Mud.HttpUtils.TokenType.AppAccessToken)
        { }

        protected override async Task<CredentialToken?> AcquireNewTokenAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(10);
            _retryCount++;
            throw new Exception("Simulated token acquisition failure");
        }
    }
}
