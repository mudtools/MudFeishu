// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions.Authentication;
using Mud.HttpUtils;
using static Mud.Feishu.Abstractions.Tests.Helpers.TestDataFactory;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// FeishuAppManager 单元测试
/// </summary>
/// <remarks>
/// 覆盖以下修复点：
/// - MA-01：GetDefaultApp 使用 override 而非 new（里氏替换原则）
/// - MA-03：移除默认应用后自动提升第一个剩余应用为新默认
/// - MA-05：检测重复 AppKey 并发出警告
/// </remarks>
public class FeishuAppManagerTests
{
    private static ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        services.AddSingleton(httpClientFactoryMock.Object);
        services.AddSingleton<IFeishuAuthentication>(new Mock<IFeishuAuthentication>().Object);
        services.AddSingleton<IFeishuCurrentUserContext, CurrentUserContext>();
        services.AddSingleton(_ => HttpClientExtensions.GetDefaultJsonSerializerOptions());
        services.AddLogging();
        services.AddMemoryCache();
        services.AddTransient<IHttpRequestExecutor>(sp => new Mock<IHttpRequestExecutor>().Object);
        services.TryAddSingleton<IFeishuTokenStoreFactory, PerAppFeishuTokenStoreFactory>();
        // MA-02 修复回归：FeishuAppManager.CreateAppContext 通过 IFeishuTokenManagerFactory 创建令牌管理器
        services.TryAddSingleton<IFeishuTokenManagerFactory, DefaultFeishuTokenManagerFactory>();

        return services;
    }

    private static FeishuAppConfig CreateDefaultConfig() => new()
    {
        AppKey = AppConfigs.AppKeys.Default,
        AppId = AppConfigs.AppIds.Default,
        AppSecret = AppConfigs.Secrets.Valid,
        IsDefault = true
    };

    private static FeishuAppConfig CreateSecondaryConfig() => new()
    {
        AppKey = AppConfigs.AppKeys.Hr,
        AppId = AppConfigs.AppIds.Hr,
        AppSecret = AppConfigs.Secrets.Hr,
        IsDefault = false
    };

    // ============================================================
    // MA-01：GetDefaultApp 使用 override 保持多态一致性
    // ============================================================

    [Fact]
    public void GetDefaultApp_WhenCalledViaBaseClass_ShouldExecuteDerivedImplementation()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig> { CreateDefaultConfig() };
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act: 通过 IFeishuAppManager 接口（映射到基类 DefaultAppManager）调用 GetDefaultApp
        var defaultApp = appManager.GetDefaultApp();

        // Assert: MA-01 修复前，使用 new 隐藏时通过基类引用调用会执行基类版本（抛异常）
        // MA-01 修复后，使用 override 保证多态一致性
        defaultApp.Should().NotBeNull();
        defaultApp.Config.AppKey.Should().Be(AppConfigs.AppKeys.Default);
    }

    [Fact]
    public void GetDefaultApp_WhenCalledDirectly_ShouldReturnDefaultApp()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig> { CreateDefaultConfig() };
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var appManager = provider.GetRequiredService<FeishuAppManager>();

        // Act
        var defaultApp = appManager.GetDefaultApp();

        // Assert
        defaultApp.Should().NotBeNull();
        defaultApp.Config.AppKey.Should().Be(AppConfigs.AppKeys.Default);
    }

    // ============================================================
    // MA-03：移除默认应用后自动提升另一个应用为新默认
    // ============================================================

    [Fact]
    public void RemoveApp_WhenRemovingDefaultApp_ShouldPromoteFirstRemainingAsNewDefault()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            CreateDefaultConfig(),       // 默认应用
            CreateSecondaryConfig()      // 非默认应用
        };
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var appManager = provider.GetRequiredService<FeishuAppManager>();

        // Act: 移除默认应用
        var removed = appManager.RemoveApp(AppConfigs.AppKeys.Default);

        // Assert: 应自动提升 hr-app 为新默认
        removed.Should().BeTrue();
        var newDefault = appManager.GetDefaultApp();
        newDefault.Config.AppKey.Should().Be(AppConfigs.AppKeys.Hr,
            "移除默认应用后应自动提升第一个剩余应用为新默认");
    }

    [Fact]
    public void RemoveApp_WhenRemovingNonDefaultApp_ShouldKeepOriginalDefault()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            CreateDefaultConfig(),
            CreateSecondaryConfig()
        };
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var appManager = provider.GetRequiredService<FeishuAppManager>();

        // Act: 移除非默认应用
        var removed = appManager.RemoveApp(AppConfigs.AppKeys.Hr);

        // Assert: 默认应用应保持不变
        removed.Should().BeTrue();
        var defaultApp = appManager.GetDefaultApp();
        defaultApp.Config.AppKey.Should().Be(AppConfigs.AppKeys.Default);
    }

    [Fact]
    public void GetDefaultApp_WhenAllAppsRemoved_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig> { CreateDefaultConfig() };
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var appManager = provider.GetRequiredService<FeishuAppManager>();
        appManager.RemoveApp(AppConfigs.AppKeys.Default);

        // Act
        var act = () => appManager.GetDefaultApp();

        // Assert: 所有应用移除后，_defaultAppKey 为 null，应抛出明确异常
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*未设置默认应用*");
    }

    // ============================================================
    // MA-05：检测重复 AppKey 并发出警告
    // ============================================================

    [Fact]
    public void Constructor_WhenDuplicateAppKeyExists_ShouldLogWarning()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = AppConfigs.AppKeys.Default,
                AppId = AppConfigs.AppIds.Default,
                AppSecret = AppConfigs.Secrets.Valid,
                IsDefault = true
            },
            new()
            {
                AppKey = AppConfigs.AppKeys.Default,  // 重复 AppKey
                AppId = AppConfigs.AppIds.Hr,
                AppSecret = AppConfigs.Secrets.Hr
            }
        };

        // 使用可验证的 Logger
        var loggerProvider = new TestLoggerProvider();
        services.AddLogging(builder => builder.AddProvider(loggerProvider));

        // Act
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        // 触发 FeishuAppManager 创建
        _ = provider.GetRequiredService<IFeishuAppManager>();

        // Assert: 应记录警告日志
        loggerProvider.LogEntries.Should().Contain(
            e => e.LogLevel == LogLevel.Warning && e.Message.Contains("重复的 AppKey"),
            "检测到重复 AppKey 时应发出警告日志");
    }

    [Fact]
    public void Constructor_WithUniqueAppKeys_ShouldNotLogDuplicateWarning()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            CreateDefaultConfig(),
            CreateSecondaryConfig()
        };

        var loggerProvider = new TestLoggerProvider();
        services.AddLogging(builder => builder.AddProvider(loggerProvider));

        // Act
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IFeishuAppManager>();

        // Assert: 不应有重复 AppKey 警告
        loggerProvider.LogEntries.Should().NotContain(
            e => e.LogLevel == LogLevel.Warning && e.Message.Contains("重复的 AppKey"));
    }
}

/// <summary>
/// 测试用 Logger 提供器，捕获日志消息供断言
/// </summary>
internal class TestLoggerProvider : ILoggerProvider
{
    public List<(LogLevel LogLevel, string Message)> LogEntries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new TestLogger(this);

    public void Dispose() { }

    private class TestLogger : ILogger
    {
        private readonly TestLoggerProvider _provider;

        public TestLogger(TestLoggerProvider provider) => _provider = provider;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _provider.LogEntries.Add((logLevel, formatter(state, exception)));
        }
    }
}
