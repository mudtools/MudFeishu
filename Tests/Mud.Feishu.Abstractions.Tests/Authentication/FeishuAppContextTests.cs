// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.Authentication;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

using static Mud.Feishu.Abstractions.Tests.Helpers.TestDataFactory;

/// <summary>
/// FeishuAppContext 单元测试
/// </summary>
public class FeishuAppContextTests : IDisposable
{
    private readonly Mock<IFeishuAuthentication> _authenticationApiMock;
    private readonly Mock<IEnhancedHttpClient> _httpClientMock;
    private readonly FeishuAppConfig _config;
    private readonly FeishuAppContext _appContext;

    public FeishuAppContextTests()
    {
        _authenticationApiMock = new Mock<IFeishuAuthentication>();
        _httpClientMock = new Mock<IEnhancedHttpClient>();

        _config = new FeishuAppConfig
        {
            AppKey = AppConfigs.AppKeys.Default,
            AppId = AppConfigs.AppIds.Default,
            AppSecret = AppConfigs.Secrets.Valid
        };

        var optionsMock = new Mock<IOptions<FeishuAppConfig>>();
        optionsMock.Setup(x => x.Value).Returns(_config);

        var tenantTokenManager = new TenantTokenManager(
            _authenticationApiMock.Object,
            optionsMock.Object,
            new Mock<ILogger<TenantTokenManager>>().Object);

        var appTokenManager = new AppTokenManager(
            _authenticationApiMock.Object,
            optionsMock.Object,
            new Mock<ILogger<AppTokenManager>>().Object);

        var userTokenManager = new UserTokenManager(
            new Mock<IFeishuCurrentUserContext>().Object,
            _authenticationApiMock.Object,
            optionsMock.Object,
            new Mock<ILogger<UserTokenManager>>().Object);

        _appContext = new FeishuAppContext(
            _config,
            tenantTokenManager,
            appTokenManager,
            userTokenManager,
            _authenticationApiMock.Object,
            _httpClientMock.Object);
    }

    /// <summary>
    /// 直接持有的服务应通过快速路径返回正确实例
    /// </summary>
    [Fact]
    public void GetService_ShouldReturnKnownService_WhenTypeIsDirectlyHeld()
    {
        // Act & Assert - 验证所有已知服务类型走快速路径
        var auth = _appContext.GetService<IFeishuAuthentication>();
        Assert.NotNull(auth);
        Assert.Same(_authenticationApiMock.Object, auth);

        var httpClient = _appContext.GetService<IEnhancedHttpClient>();
        Assert.NotNull(httpClient);
        Assert.Same(_httpClientMock.Object, httpClient);

        var tenantTokenManager = _appContext.GetService<ITenantTokenManager>();
        Assert.NotNull(tenantTokenManager);
        Assert.Same(_appContext.TenantTokenManager, tenantTokenManager);

        var appTokenManager = _appContext.GetService<IAppTokenManager>();
        Assert.NotNull(appTokenManager);
        Assert.Same(_appContext.AppTokenManager, appTokenManager);

        var userTokenManager = _appContext.GetService<IFeishuUserTokenManager>();
        Assert.NotNull(userTokenManager);
        Assert.Same(_appContext.UserTokenManager, userTokenManager);
    }

    /// <summary>
    /// 当 _serviceProvider 为 null 时，未知类型应返回 null（优雅降级）
    /// </summary>
    [Fact]
    public void GetService_ShouldReturnNull_WhenServiceProviderIsNullAndTypeIsUnknown()
    {
        // Arrange - _appContext 在构造时未传入 serviceProvider

        // Act
        var result = _appContext.GetService<ITestDiService>();

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 当 _serviceProvider 不为 null 时，未知类型应回退到 DI 容器解析
    /// </summary>
    [Fact]
    public void GetService_ShouldResolveFromDiContainer_WhenServiceProviderIsNotNull()
    {
        // Arrange
        var testService = new TestDiService();
        var services = new ServiceCollection();
        services.AddSingleton<ITestDiService>(testService);
        var provider = services.BuildServiceProvider();

        var appContextWithDi = CreateAppContextWithServiceProvider(provider);

        // Act
        var result = appContextWithDi.GetService<ITestDiService>();

        // Assert
        Assert.NotNull(result);
        Assert.Same(testService, result);
    }

    /// <summary>
    /// 即使有 DI 容器，已知服务仍应走快速路径（不经过 DI 容器）
    /// </summary>
    [Fact]
    public void GetService_ShouldUseFastPath_WhenTypeIsKnownEvenWithDiProvider()
    {
        // Arrange
        // 在 DI 容器中注册一个不同的 IFeishuAuthentication 实例
        var diAuthMock = new Mock<IFeishuAuthentication>();
        var services = new ServiceCollection();
        services.AddSingleton<IFeishuAuthentication>(diAuthMock.Object);
        var provider = services.BuildServiceProvider();

        var appContextWithDi = CreateAppContextWithServiceProvider(provider);

        // Act
        var result = appContextWithDi.GetService<IFeishuAuthentication>();

        // Assert - 应返回上下文直接持有的实例，而非 DI 容器中的实例
        Assert.NotNull(result);
        Assert.Same(_authenticationApiMock.Object, result);
        Assert.NotSame(diAuthMock.Object, result);
    }

    /// <summary>
    /// DI 容器中未注册的服务应返回 null
    /// </summary>
    [Fact]
    public void GetService_ShouldReturnNull_WhenServiceNotRegisteredInDiContainer()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var appContextWithDi = CreateAppContextWithServiceProvider(provider);

        // Act
        var result = appContextWithDi.GetService<IUnregisteredService>();

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 使用实际 FeishuAppManager 验证 GetService 能解析 DI 容器中的服务
    /// </summary>
    [Fact]
    public void GetService_ShouldResolveFromDiContainer_WhenCreatedViaAppManager()
    {
        // Arrange
        var services = new ServiceCollection();

        // 方案 A 重构后，CreateAppContext 直接使用 IHttpClientFactory。
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        services.AddSingleton(httpClientFactoryMock.Object);
        services.AddSingleton<IFeishuAuthentication>(new Mock<IFeishuAuthentication>().Object);
        services.AddSingleton<IFeishuCurrentUserContext, CurrentUserContext>();
        services.AddSingleton(_ => HttpClientExtensions.GetDefaultJsonSerializerOptions());
        services.AddLogging();
        // M-1 修复回归：CreateAppContext 通过 _serviceProvider.GetRequiredService<IMemoryCache>() 创建 per-app TokenStore，
        // 测试基础设施必须注册 IMemoryCache，否则 GetApp 触发 Lazy 创建时会抛 InvalidOperationException。
        services.AddMemoryCache();
        // P2-2: 生成代码构造函数通过 DI 注入 IHttpRequestExecutor，测试需注册 mock 以支持 ActivatorUtilities.CreateInstance
        services.AddTransient<IHttpRequestExecutor>(sp => new Mock<IHttpRequestExecutor>().Object);
        // S-3 修复回归：CreateAppContext 通过 GetRequiredService<IFeishuTokenStoreFactory> 创建 per-app TokenStore，
        // 测试基础设施必须注册工厂，否则 GetApp 触发 Lazy 创建时会抛 InvalidOperationException。
        services.TryAddSingleton<IFeishuTokenStoreFactory, PerAppFeishuTokenStoreFactory>();

        // 注册一个自定义服务到 DI 容器
        var customService = new TestDiService();
        services.AddSingleton<ITestDiService>(customService);

        var configs = new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = AppConfigs.AppKeys.Default,
                AppId = AppConfigs.AppIds.Default,
                AppSecret = AppConfigs.Secrets.Valid,
                IsDefault = true
            }
        };

        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act
        var defaultApp = appManager.GetApp(AppConfigs.AppKeys.Default);
        var result = defaultApp.GetService<ITestDiService>();

        // Assert
        Assert.NotNull(result);
        Assert.Same(customService, result);
    }

    private FeishuAppContext CreateAppContextWithServiceProvider(IServiceProvider serviceProvider)
    {
        var optionsMock = new Mock<IOptions<FeishuAppConfig>>();
        optionsMock.Setup(x => x.Value).Returns(_config);

        var tenantTokenManager = new TenantTokenManager(
            _authenticationApiMock.Object,
            optionsMock.Object,
            new Mock<ILogger<TenantTokenManager>>().Object);

        var appTokenManager = new AppTokenManager(
            _authenticationApiMock.Object,
            optionsMock.Object,
            new Mock<ILogger<AppTokenManager>>().Object);

        var userTokenManager = new UserTokenManager(
            new Mock<IFeishuCurrentUserContext>().Object,
            _authenticationApiMock.Object,
            optionsMock.Object,
            new Mock<ILogger<UserTokenManager>>().Object);

        return new FeishuAppContext(
            _config,
            tenantTokenManager,
            appTokenManager,
            userTokenManager,
            _authenticationApiMock.Object,
            _httpClientMock.Object,
            serviceProvider);
    }

    public void Dispose()
    {
        _appContext?.Dispose();
    }

    /// <summary>
    /// 测试用 DI 服务接口
    /// </summary>
    public interface ITestDiService
    {
        string GetValue() => "test";
    }

    /// <summary>
    /// 测试用 DI 服务实现
    /// </summary>
    public class TestDiService : ITestDiService;

    /// <summary>
    /// 未注册的测试服务接口
    /// </summary>
    public interface IUnregisteredService;
}
