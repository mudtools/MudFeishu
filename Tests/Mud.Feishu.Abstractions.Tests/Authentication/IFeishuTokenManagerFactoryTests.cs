// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;
using Mud.HttpUtils;
using static Mud.Feishu.Abstractions.Tests.Helpers.TestDataFactory;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// IFeishuTokenManagerFactory / DefaultFeishuTokenManagerFactory 单元测试
/// </summary>
/// <remarks>
/// 覆盖 MA-02 修复引入的工厂抽象层：
/// - 工厂方法 Create 应正确创建 TenantTokenManager / AppTokenManager / UserTokenManager 三元组
/// - 自定义工厂实现可通过 DI 替换默认实现
/// </remarks>
public class IFeishuTokenManagerFactoryTests
{
    private static FeishuAppConfig CreateConfig() => new()
    {
        AppKey = AppConfigs.AppKeys.Default,
        AppId = AppConfigs.AppIds.Default,
        AppSecret = AppConfigs.Secrets.Valid
    };

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddSingleton<IFeishuCurrentUserContext, CurrentUserContext>();
        services.AddSingleton(_ => HttpClientExtensions.GetDefaultJsonSerializerOptions());
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Constructor_WhenServiceProviderIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new DefaultFeishuTokenManagerFactory(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Create_WhenConfigIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var factory = new DefaultFeishuTokenManagerFactory(provider);
        var authApi = new Mock<IFeishuAuthentication>().Object;
        var tokenStore = new Mock<ITokenStore>().Object;

        // Act
        var act = () => factory.Create(null!, authApi, tokenStore, null);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("config");
    }

    [Fact]
    public void Create_WhenAuthenticationApiIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var factory = new DefaultFeishuTokenManagerFactory(provider);
        var config = CreateConfig();
        var tokenStore = new Mock<ITokenStore>().Object;

        // Act
        var act = () => factory.Create(config, null!, tokenStore, null);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("authenticationApi");
    }

    [Fact]
    public void Create_WhenTokenStoreIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var factory = new DefaultFeishuTokenManagerFactory(provider);
        var config = CreateConfig();
        var authApi = new Mock<IFeishuAuthentication>().Object;

        // Act
        var act = () => factory.Create(config, authApi, null!, null);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tokenStore");
    }

    [Fact]
    public void Create_WithValidInputs_ShouldReturnAllThreeManagers()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var factory = new DefaultFeishuTokenManagerFactory(provider);
        var config = CreateConfig();
        var authApi = new Mock<IFeishuAuthentication>().Object;
        var tokenStore = new Mock<ITokenStore>().Object;
        var userTokenStore = new Mock<IUserTokenStore>().Object;

        // Act
        var (tenantManager, appManager, userTokenManager) = factory.Create(config, authApi, tokenStore, userTokenStore);

        // Assert
        tenantManager.Should().NotBeNull("应创建 TenantTokenManager 实例");
        appManager.Should().NotBeNull("应创建 AppTokenManager 实例");
        userTokenManager.Should().NotBeNull("应创建 UserTokenManager 实例");

        tenantManager.Should().BeOfType<TenantTokenManager>();
        appManager.Should().BeOfType<AppTokenManager>();
        userTokenManager.Should().BeOfType<UserTokenManager>();
    }

    [Fact]
    public void Create_WithNullUserTokenStore_ShouldStillCreateUserTokenManager()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var factory = new DefaultFeishuTokenManagerFactory(provider);
        var config = CreateConfig();
        var authApi = new Mock<IFeishuAuthentication>().Object;
        var tokenStore = new Mock<ITokenStore>().Object;

        // Act
        var (_, _, userTokenManager) = factory.Create(config, authApi, tokenStore, null);

        // Assert: UserTokenManager 仍应创建（内部使用 null 容错）
        userTokenManager.Should().NotBeNull();
    }

    [Fact]
    public void Create_ShouldProduceDifferentInstancesForEachCall()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var factory = new DefaultFeishuTokenManagerFactory(provider);
        var config = CreateConfig();
        var authApi = new Mock<IFeishuAuthentication>().Object;
        var tokenStore = new Mock<ITokenStore>().Object;

        // Act
        var result1 = factory.Create(config, authApi, tokenStore, null);
        var result2 = factory.Create(config, authApi, tokenStore, null);

        // Assert: 每次调用应创建新实例（非缓存）
        result1.TenantTokenManager.Should().NotBeSameAs(result2.TenantTokenManager);
        result1.AppTokenManager.Should().NotBeSameAs(result2.AppTokenManager);
        result1.UserTokenManager.Should().NotBeSameAs(result2.UserTokenManager);
    }

    [Fact]
    public void Factory_ShouldBeRegisteredAsSingleton_InDIContainer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddFeishuApp(new List<FeishuAppConfig>
        {
            CreateConfig()
        });

        // Act
        using var provider = services.BuildServiceProvider();
        var factory1 = provider.GetService<IFeishuTokenManagerFactory>();
        var factory2 = provider.GetService<IFeishuTokenManagerFactory>();

        // Assert
        factory1.Should().NotBeNull();
        factory1.Should().BeOfType<DefaultFeishuTokenManagerFactory>();
        factory1.Should().BeSameAs(factory2, "IFeishuTokenManagerFactory 应注册为 Singleton");
    }

    [Fact]
    public void Factory_ShouldBeReplaceable_ByCustomImplementation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

        // 预注册自定义工厂（应在 AddFeishuApp 之前）
        var customFactory = new Mock<IFeishuTokenManagerFactory>().Object;
        services.AddSingleton(customFactory);

        services.AddFeishuApp(new List<FeishuAppConfig>
        {
            CreateConfig()
        });

        // Act
        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<IFeishuTokenManagerFactory>();

        // Assert: TryAddSingleton 语义，自定义注册应优先
        resolved.Should().BeSameAs(customFactory, "预注册的自定义工厂应优先于默认实现");
    }
}
