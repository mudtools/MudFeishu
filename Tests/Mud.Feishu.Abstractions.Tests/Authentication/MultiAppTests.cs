// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.Authentication;
using Mud.Feishu.Abstractions.Authentication;
using Mud.HttpUtils;

namespace Mud.Feishu.Abstractions.Tests;

using static Mud.Feishu.Abstractions.Tests.Helpers.TestDataFactory;

/// <summary>
/// 多应用功能测试
/// </summary>
public class MultiAppTests
{
    private static ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();

        var httpClientResolverMock = new Mock<IHttpClientResolver>();
        httpClientResolverMock
            .Setup(x => x.GetClient(It.IsAny<string>()))
            .Returns<string>(clientName => new Mock<IEnhancedHttpClient>().Object);

        services.AddSingleton(httpClientResolverMock.Object);
        services.AddSingleton<IFeishuAuthentication>(new Mock<IFeishuAuthentication>().Object);
        services.AddSingleton<IFeishuCurrentUserContext, CurrentUserContext>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());
        services.AddLogging();

        return services;
    }

    [Fact]
    public void MultiApp_ConfigurationValidation_ShouldThrowOnInvalidConfig()
    {
        // Arrange
        var invalidConfig = new FeishuAppConfig
        {
            AppKey = "",
            AppId = "",
            AppSecret = AppConfigs.Secrets.Empty
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => invalidConfig.Validate());
    }

    [Fact]
    public void MultiApp_DefaultAppKey_ShouldAutoSetIsDefault()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = AppConfigs.AppKeys.Default,
            AppId = AppConfigs.AppIds.Default,
            AppSecret = AppConfigs.Secrets.Valid,
            IsDefault = false
        };

        // Act
        config.Validate();

        // Assert
        Assert.True(config.IsDefault);
    }

    [Fact]
    public void MultiApp_DefaultAppKey_AutoInference_ShouldWork()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = AppConfigs.AppKeys.Default,
            AppId = AppConfigs.AppIds.Default,
            AppSecret = AppConfigs.Secrets.Valid
        };

        // Act
        config.Validate();

        // Assert
        Assert.True(config.IsDefault);
    }

    [Fact]
    public void MultiApp_ValidConfiguration_ShouldPassValidation()
    {
        // Arrange
        var validConfig = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = AppConfigs.AppIds.Default,
            AppSecret = AppConfigs.Secrets.Valid,
            BaseUrl = "https://open.feishu.cn",
            TimeOut = 30,
            RetryCount = 3,
            RetryDelayMs = 1000,
            TokenRefreshThreshold = 300,
            EnableLogging = true,
            IsDefault = true
        };

        // Act & Assert
        var exception = Record.Exception(() => validConfig.Validate());
        Assert.Null(exception);
    }

    [Fact]
    public void MultiApp_InvalidRetryDelayMs_ShouldThrow()
    {
        // Arrange
        var invalidConfig = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = AppConfigs.AppIds.Default,
            AppSecret = AppConfigs.Secrets.Valid,
            RetryDelayMs = 50
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => invalidConfig.Validate());
        Assert.Contains("RetryDelayMs", ex.Message);
    }

    [Fact]
    public void MultiApp_SensitiveDataToString_ShouldMaskSecret()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = AppConfigs.AppIds.Default,
            AppSecret = AppConfigs.Secrets.VerySecret
        };

        // Act
        var configString = config.ToString();

        // Assert
        Assert.Contains("ve****45", configString);
        Assert.DoesNotContain(AppConfigs.Secrets.VerySecret, configString);
    }

    [Fact]
    public void MultiApp_DuplicateAppKeys_ShouldOverwrite()
    {
        // Arrange
        var services = CreateServiceCollection();

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.App1,
                AppId = AppConfigs.AppIds.App1,
                AppSecret = AppConfigs.Secrets.App1,
                IsDefault = true
            },
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.App1,
                AppId = AppConfigs.AppIds.App2,
                AppSecret = AppConfigs.Secrets.App2,
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
        var app = appManager.GetApp(AppConfigs.AppKeys.App1);

        // Assert
        Assert.NotNull(app);
        Assert.Equal(AppConfigs.AppIds.App2, app.Config.AppId);
    }

    [Fact]
    public void MultiApp_NoDefaultApp_ShouldThrowOnRegistration()
    {
        // Arrange
        var services = CreateServiceCollection();

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.App1,
                AppId = AppConfigs.AppIds.App1,
                AppSecret = AppConfigs.Secrets.App1,
                IsDefault = false
            }
        };

        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IFeishuAppManager>());
    }

    [Fact]
    public void MultiApp_GetApp_ShouldReturnCorrectApp()
    {
        // Arrange
        var services = CreateServiceCollection();

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.Default,
                AppId = AppConfigs.AppIds.Default,
                AppSecret = AppConfigs.Secrets.Default,
                IsDefault = true
            },
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.Hr,
                AppId = AppConfigs.AppIds.Hr,
                AppSecret = AppConfigs.Secrets.Hr
            }
        };

        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act
        var hrApp = appManager.GetApp(AppConfigs.AppKeys.Hr);

        // Assert
        Assert.NotNull(hrApp);
    }

    [Fact]
    public void MultiApp_GetNonExistentApp_ShouldThrow()
    {
        // Arrange
        var services = CreateServiceCollection();

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = "default",
                AppId = "cli_default_id_1234567890",
                AppSecret = "default_secret_123456",
                IsDefault = true
            }
        };

        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => appManager.GetApp("non-existent-app"));
    }

    [Fact]
    public void MultiApp_GetAllApps_ShouldReturnAllRegisteredApps()
    {
        // Arrange
        var services = CreateServiceCollection();

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.Default,
                AppId = AppConfigs.AppIds.Default,
                AppSecret = AppConfigs.Secrets.Default,
                IsDefault = true
            },
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.Hr,
                AppId = AppConfigs.AppIds.Hr,
                AppSecret = AppConfigs.Secrets.Hr
            },
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.Finance,
                AppId = AppConfigs.AppIds.Finance,
                AppSecret = AppConfigs.Secrets.Finance
            }
        };

        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act
        var allApps = appManager.GetAllApps();

        // Assert
        Assert.Equal(3, allApps.Count());
    }

    [Fact]
    public void MultiApp_HasApp_ShouldReturnCorrectStatus()
    {
        // Arrange
        var services = CreateServiceCollection();

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = "default",
                AppId = "cli_default_id_1234567890",
                AppSecret = "default_secret_123456",
                IsDefault = true
            }
        };

        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act & Assert
        Assert.True(appManager.HasApp("default"));
        Assert.False(appManager.HasApp("non-existent"));
    }

    [Fact]
    public void MultiApp_AppContextProperties_ShouldBeIsolated()
    {
        // Arrange
        var services = CreateServiceCollection();

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.Default,
                AppId = AppConfigs.AppIds.Default,
                AppSecret = AppConfigs.Secrets.Default,
                IsDefault = true
            },
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.Hr,
                AppId = AppConfigs.AppIds.Hr,
                AppSecret = AppConfigs.Secrets.Hr
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
        var hrApp = appManager.GetApp(AppConfigs.AppKeys.Hr);

        // Assert
        Assert.NotNull(defaultApp.GetTokenManager("TenantAccessToken"));
        Assert.NotNull(defaultApp.GetTokenManager("AppAccessToken"));
        Assert.NotNull(defaultApp.GetTokenManager("UserAccessToken"));
        Assert.NotNull(defaultApp.HttpClient);

        Assert.NotEqual(defaultApp.HttpClient.GetHashCode(), hrApp.HttpClient.GetHashCode());
    }

    [Fact]
    public void MultiApp_RemoveDefaultApp_ShouldClearDefault()
    {
        // Arrange
        var services = CreateServiceCollection();

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.Default,
                AppId = AppConfigs.AppIds.Default,
                AppSecret = AppConfigs.Secrets.Default,
                IsDefault = true
            },
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.Hr,
                AppId = AppConfigs.AppIds.Hr,
                AppSecret = AppConfigs.Secrets.Hr
            }
        };

        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act
        var result = appManager.RemoveApp(AppConfigs.AppKeys.Default);

        // Assert
        Assert.True(result);
        Assert.True(appManager.HasApp(AppConfigs.AppKeys.Hr));
        Assert.False(appManager.HasApp(AppConfigs.AppKeys.Default));
    }
}
