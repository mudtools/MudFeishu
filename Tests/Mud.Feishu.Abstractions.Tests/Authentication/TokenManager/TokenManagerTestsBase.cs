// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.TokenManager;
using Mud.HttpUtils;

namespace Mud.Feishu.Tests.Authentication.TokenManager;

/// <summary>
/// 令牌管理器测试基类
/// </summary>
/// <remarks>
/// 提供测试所需的公共基础设施，包括 Mock 对象和 FeishuAppContext 实例。
/// </remarks>
public abstract class TokenManagerTestsBase : IDisposable
{
    protected readonly Mock<IFeishuAuthentication> _authenticationApiMock;
    protected readonly Mock<ITokenCache> _tokenCacheMock;
    protected readonly Mock<IUserTokenCache> _userTokenCacheMock;
    protected readonly Mock<IEnhancedHttpClient> _httpClientMock;
    protected readonly FeishuAppConfig Config;
    protected readonly FeishuAppContext AppContext;

    protected TokenManagerTestsBase()
    {
        _authenticationApiMock = new Mock<IFeishuAuthentication>();
        _tokenCacheMock = new Mock<ITokenCache>();
        _userTokenCacheMock = new Mock<IUserTokenCache>();
        _httpClientMock = new Mock<IEnhancedHttpClient>();

        Config = new FeishuAppConfig
        {
            AppKey = "test",
            AppId = "test_app_id_1234567890",
            AppSecret = "test_app_secret_123456",
            TokenRefreshThreshold = 300
        };

        var loggerMock = new Mock<ILogger<TokenManagerWithCache>>();
        var userTokenManagerLoggerMock = new Mock<ILogger<Mud.Feishu.TokenManager.UserTokenManager>>();
        var currentUserContextMock = new Mock<ICurrentUserContext>();
        var optionsMock = new Mock<IOptions<FeishuAppConfig>>();
        optionsMock.Setup(x => x.Value).Returns(Config);

        var tenantTokenManager = new TenantTokenManager(
            _authenticationApiMock.Object,
            optionsMock.Object,
            loggerMock.Object,
            _tokenCacheMock.Object);

        var appTokenManager = new AppTokenManager(
            _authenticationApiMock.Object,
            optionsMock.Object,
            loggerMock.Object,
            _tokenCacheMock.Object);

        var userTokenManager = new Mud.Feishu.TokenManager.UserTokenManager(
            _authenticationApiMock.Object,
            currentUserContextMock.Object,
            optionsMock.Object,
            userTokenManagerLoggerMock.Object,
            _userTokenCacheMock.Object);

        AppContext = new FeishuAppContext(
            Config,
            tenantTokenManager,
            appTokenManager,
            userTokenManager,
            _authenticationApiMock.Object,
            _tokenCacheMock.Object,
            _httpClientMock.Object);

        _tokenCacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _tokenCacheMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _tokenCacheMock.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0));

        UserTokenInfo? nullToken = null;
        _userTokenCacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(nullToken);
        _userTokenCacheMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<UserTokenInfo>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userTokenCacheMock.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0));
    }

    public void Dispose()
    {
        AppContext?.Dispose();
    }
}
