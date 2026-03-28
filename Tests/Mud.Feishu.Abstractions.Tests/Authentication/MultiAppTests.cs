// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.Authentication;
using Mud.Feishu.TokenManager;
using Mud.HttpUtils;

namespace Mud.Feishu.Abstractions.Tests;

using static Mud.Feishu.Abstractions.Tests.Helpers.TestDataFactory;

/// <summary>
/// 多应用功能测试
/// </summary>
public class MultiAppTests
{

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
            IsDefault = false  // 这应该被自动推断为 true
        };

        // Act
        config.Validate();

        // Assert - Validate() 方法应该自动将 IsDefault 设置为 true
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

        // Act - Validate() 方法应该自动推断
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
            RetryDelayMs = 50  // 低于最小值 100
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

        // Assert - VerySecret = "very_secret_key_12345", 掩码后为 "ve****45"
        Assert.Contains("ve****45", configString);
        Assert.DoesNotContain(AppConfigs.Secrets.VerySecret, configString);
    }

    [Fact]
    public void MultiApp_DuplicateAppKeys_ShouldOverwrite()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.AddSingleton<ICurrentUserContext, CurrentUserContext>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

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
                AppKey = AppConfigs.AppKeys.App1,  // 重复的AppKey - 会覆盖第一个
                AppId = AppConfigs.AppIds.App2,
                AppSecret = AppConfigs.Secrets.App2,
                IsDefault = true
            }
        };

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act - 重复的 AppKey 会被覆盖，所以只有一个应用
        var app = appManager.GetApp(AppConfigs.AppKeys.App1);

        // Assert - 应该是第二个配置（被覆盖后）
        Assert.NotNull(app);
        Assert.Equal(AppConfigs.AppIds.App2, app.Config.AppId);
    }

    [Fact]
    public void MultiApp_NoDefaultApp_ShouldThrowOnRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.AddSingleton<ICurrentUserContext, CurrentUserContext>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

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

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();

        // Act & Assert - 解析服务时才会抛出异常
        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IFeishuAppManager>());
    }

    [Fact]
    public void MultiApp_GetApp_ShouldReturnCorrectApp()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.AddSingleton<ICurrentUserContext, CurrentUserContext>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

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

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
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
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.AddSingleton<ICurrentUserContext, CurrentUserContext>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

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

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => appManager.GetApp("non-existent-app"));
    }

    [Fact]
    public void MultiApp_GetAllApps_ShouldReturnAllRegisteredApps()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.AddSingleton<ICurrentUserContext, CurrentUserContext>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

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

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
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
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.AddSingleton<ICurrentUserContext, CurrentUserContext>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

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

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
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
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.AddSingleton<ICurrentUserContext, CurrentUserContext>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

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

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
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
        Assert.NotNull(defaultApp.GetTokenManager(TokenType.TenantAccessToken));
        Assert.NotNull(defaultApp.GetTokenManager(TokenType.AppAccessToken));
        Assert.NotNull(defaultApp.GetTokenManager(TokenType.UserAccessToken));
        Assert.NotNull(defaultApp.HttpClient);

        // 验证HttpClient隔离
        Assert.NotEqual(defaultApp.HttpClient.GetHashCode(), hrApp.HttpClient.GetHashCode());
    }

    [Fact]
    public void MultiApp_RemoveDefaultApp_ShouldFailWhenMultipleApps()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.AddSingleton<ICurrentUserContext, CurrentUserContext>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

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

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => appManager.RemoveApp(AppConfigs.AppKeys.Default));
    }
}
