// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// IFeishuTokenManagerResolver TryGet* 方法单元测试
/// </summary>
public class FeishuTokenManagerResolverTests
{
    private static ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }

    private static List<FeishuAppConfig> CreateMultiAppConfigs() => new()
    {
        new FeishuAppConfig
        {
            AppKey = "default",
            AppId = "cli_default_id_1234567890",
            AppSecret = "default_secret_123456",
            IsDefault = true
        },
        new FeishuAppConfig
        {
            AppKey = "hr-app",
            AppId = "cli_hr_app_1234567890",
            AppSecret = "hr_secret_123456"
        }
    };

    /// <summary>
    /// TryGetTenantTokenManager 使用 null appKey 应返回默认应用的租户令牌管理器
    /// </summary>
    [Fact]
    public void TryGetTenantTokenManager_WithNullAppKey_ShouldReturnDefault()
    {
        var services = CreateServiceCollection();
        services.AddFeishuApp(CreateMultiAppConfigs());
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        var result = resolver.TryGetTenantTokenManager();
        result.Should().NotBeNull();
        result.Should().BeSameAs(appManager.DefaultTenantTokenManager);
    }

    /// <summary>
    /// TMA2-10 / D6（§7.2 #13）核心：默认应用被移除后，TryGet* 必须返回 null 而非抛出异常。
    /// 修复前 Try* 通过 <c>DefaultConfig</c>（经 GetDefaultApp）读取默认键，默认应用缺失时抛
    /// InvalidOperationException，破坏"Try* 永不抛出，缺失返回 null"的契约。
    /// </summary>
    [Fact]
    public void TryGet_ShouldReturnNull_WhenDefaultAppRemoved()
    {
        var services = CreateServiceCollection();
        services.AddFeishuApp(new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = "solo",
                AppId = "cli_solo_app_id_123456",
                AppSecret = "solo_secret_123456",
                IsDefault = true
            }
        });
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // 前置：默认应用存在且可解析（显式实例化，确保注册链路完整）
        appManager.GetDefaultApp();
        resolver.TryGetTenantTokenManager().Should().NotBeNull();

        // Act：移除默认应用（唯一应用，无提升候选）
        appManager.RemoveApp("solo").Should().BeTrue();

        // Assert：三个 Try* 都不抛、都返回 null
        resolver.TryGetTenantTokenManager().Should().BeNull();
        resolver.TryGetAppTokenManager().Should().BeNull();
        resolver.TryGetUserTokenManager().Should().BeNull();
    }

    /// <summary>
    /// TryGetTenantTokenManager 使用存在的 appKey 应返回对应应用的租户令牌管理器
    /// </summary>
    [Fact]
    public void TryGetTenantTokenManager_WithExistingAppKey_ShouldReturnManager()
    {
        var services = CreateServiceCollection();
        services.AddFeishuApp(CreateMultiAppConfigs());
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        var result = resolver.TryGetTenantTokenManager("hr-app");
        result.Should().NotBeNull();
        result.Should().BeSameAs(appManager.GetApp("hr-app").TenantTokenManager);
    }

    /// <summary>
    /// TryGetTenantTokenManager 使用不存在的 appKey 应返回 null
    /// </summary>
    [Fact]
    public void TryGetTenantTokenManager_WithNonExistentAppKey_ShouldReturnNull()
    {
        var services = CreateServiceCollection();
        services.AddFeishuApp(CreateMultiAppConfigs());
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();

        var result = resolver.TryGetTenantTokenManager("non-existent-app");
        result.Should().BeNull();
    }

    /// <summary>
    /// TryGetAppTokenManager 使用 null appKey 应返回默认应用的应用令牌管理器
    /// </summary>
    [Fact]
    public void TryGetAppTokenManager_WithNullAppKey_ShouldReturnDefault()
    {
        var services = CreateServiceCollection();
        services.AddFeishuApp(CreateMultiAppConfigs());
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        var result = resolver.TryGetAppTokenManager();
        result.Should().NotBeNull();
        result.Should().BeSameAs(appManager.DefaultAppTokenManager);
    }

    /// <summary>
    /// TryGetAppTokenManager 使用不存在的 appKey 应返回 null
    /// </summary>
    [Fact]
    public void TryGetAppTokenManager_WithNonExistentAppKey_ShouldReturnNull()
    {
        var services = CreateServiceCollection();
        services.AddFeishuApp(CreateMultiAppConfigs());
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();

        var result = resolver.TryGetAppTokenManager("non-existent-app");
        result.Should().BeNull();
    }

    /// <summary>
    /// TryGetUserTokenManager 使用 null appKey 应返回默认应用的用户令牌管理器
    /// </summary>
    [Fact]
    public void TryGetUserTokenManager_WithNullAppKey_ShouldReturnDefault()
    {
        var services = CreateServiceCollection();
        services.AddFeishuApp(CreateMultiAppConfigs());
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        var result = resolver.TryGetUserTokenManager();
        result.Should().NotBeNull();
        result.Should().BeSameAs(appManager.DefaultUserTokenManager);
    }

    /// <summary>
    /// TryGetUserTokenManager 使用不存在的 appKey 应返回 null
    /// </summary>
    [Fact]
    public void TryGetUserTokenManager_WithNonExistentAppKey_ShouldReturnNull()
    {
        var services = CreateServiceCollection();
        services.AddFeishuApp(CreateMultiAppConfigs());
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();

        var result = resolver.TryGetUserTokenManager("non-existent-app");
        result.Should().BeNull();
    }

    /// <summary>
    /// TryGet* 方法与 Get* 方法在应用存在时应返回相同实例
    /// </summary>
    [Fact]
    public void TryGet_ShouldReturnSameInstanceAsGet_WhenAppExists()
    {
        var services = CreateServiceCollection();
        services.AddFeishuApp(CreateMultiAppConfigs());
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();

        resolver.TryGetTenantTokenManager("hr-app").Should().BeSameAs(resolver.GetTenantTokenManager("hr-app"));
        resolver.TryGetAppTokenManager("hr-app").Should().BeSameAs(resolver.GetAppTokenManager("hr-app"));
        resolver.TryGetUserTokenManager("hr-app").Should().BeSameAs(resolver.GetUserTokenManager("hr-app"));
    }

    /// <summary>
    /// Get* 方法在应用不存在时应抛出异常，而 TryGet* 应返回 null
    /// </summary>
    [Fact]
    public void TryGet_ShouldReturnNull_WhileGet_Throws_WhenAppNotExists()
    {
        var services = CreateServiceCollection();
        services.AddFeishuApp(CreateMultiAppConfigs());
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();

        // TryGet 应返回 null
        resolver.TryGetTenantTokenManager("non-existent").Should().BeNull();
        resolver.TryGetAppTokenManager("non-existent").Should().BeNull();
        resolver.TryGetUserTokenManager("non-existent").Should().BeNull();

        // Get 应抛出异常
        Assert.Throws<InvalidOperationException>(() => resolver.GetTenantTokenManager("non-existent"));
        Assert.Throws<InvalidOperationException>(() => resolver.GetAppTokenManager("non-existent"));
        Assert.Throws<InvalidOperationException>(() => resolver.GetUserTokenManager("non-existent"));
    }
}
