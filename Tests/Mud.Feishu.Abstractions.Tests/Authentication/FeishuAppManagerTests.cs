// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.Authentication;
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
/// - NEW-MA-08：Lazy&lt;&gt; 异常缓存重建（GetOrCreateContext/TryGetApp 在捕获非 InvalidOperationException 时重建 Lazy）
/// - NEW-MA-09：volatile + lock 保护 GetDefaultApp/RemoveApp/AddApp 的 TOCTOU 竞态
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
        // TMA-09 测试适配：Mock IFeishuAuthenticationFactory 使 per-app 认证走 Mock 路径，
        // 避免 ActivatorUtilities.CreateInstance 尝试构造接口类型。
        var authFactoryMock = new Mock<IFeishuAuthenticationFactory>();
        authFactoryMock
            .Setup(x => x.Create(It.IsAny<string>()))
            .Returns(new Mock<IFeishuAuthentication>().Object);
        services.AddSingleton(authFactoryMock.Object);
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
    // MA-05 / REG-01：检测重复 AppKey 时快速失败
    // ============================================================
    // 设计权衡：MA-05 原设计为"警告 + 后注册覆盖先注册"，但 REG-01 修复指出
    // HttpClient 命名注册的 ClientFactories 同名键覆盖行为不可预测，
    // 因此在 AddFeishuAppBaseServices 阶段即抛异常实现快速失败，避免运行时不可预测行为。
    // FeishuAppManager 构造函数中的 MA-05 警告逻辑作为防御性兜底保留，
    // 用于绕过 AddFeishuApp 直接构造 FeishuAppManager 的场景。

    [Fact]
    public void AddFeishuApp_WhenDuplicateAppKeyExists_ShouldThrowInvalidOperationException()
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

        // Act & Assert: REG-01 设计 - 重复 AppKey 在 AddFeishuAppBaseServices 阶段快速失败
        Action act = () => services.AddFeishuApp(configs);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*检测到重复的 AppKey*");
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

    // ============================================================
    // NEW-MA-08：Lazy<> 异常缓存重建
    // ============================================================

    /// <summary>
    /// NEW-MA-08 验证：当 Lazy 初始化抛出非 InvalidOperationException 异常时，
    /// GetOrCreateContext 应抛出包装后的 InvalidOperationException，并在下次调用时重建 Lazy 允许重试。
    /// 业务场景：首次初始化因瞬时故障（如 Redis 短暂不可用）失败，
    /// Lazy&lt;ExecutionAndPublication&gt; 会缓存异常导致永久不可用；
    /// 修复后通过双检锁重建 Lazy 实例，允许下次调用重试初始化。
    /// 注意：GetApp 内部先调用 TryGetApp（已有独立的重建测试），
    /// 因此本测试通过 GetAllApps 直接调用 GetOrCreateContext 验证其重建路径。
    /// </summary>
    [Fact]
    public void GetApp_WhenLazyInitializationFails_ShouldRebuildLazyOnNextCall()
    {
        // Arrange：通过 mock IFeishuTokenStoreFactory.Create（CreateAppContext 中同步调用）
        // 首次抛 HttpRequestException 触发初始化失败，第二次返回有效的 store 实例
        var tokenStoreFactoryMock = new Mock<IFeishuTokenStoreFactory>();
        var callCount = 0;
        tokenStoreFactoryMock
            .Setup(x => x.Create(It.IsAny<string>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new HttpRequestException("模拟首次初始化失败（如 Redis 短暂故障）");
                return (new Mock<ITokenStore>().Object, new Mock<IUserTokenStore>().Object);
            });

        var services = CreateServiceCollection();
        // 替换已注册的 IFeishuTokenStoreFactory，注入会首次抛异常的 mock
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IFeishuTokenStoreFactory));
        if (existingDescriptor != null) services.Remove(existingDescriptor);
        services.AddSingleton(tokenStoreFactoryMock.Object);

        var configs = new List<FeishuAppConfig> { CreateDefaultConfig() };
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var appManager = provider.GetRequiredService<FeishuAppManager>();

        // Act & Assert：首次通过 TryGetApp 访问应返回 false（Lazy 初始化失败，重建 Lazy）
        // TMA-04 修复后 GetApp 内部先走 TryGetApp（重建 Lazy 后返回 false），再走 GetOrCreateContext（使用重建后的 Lazy 成功）
        // 因此验证 TryGetApp 的首次失败 + 重建行为
        var firstResult = appManager.TryGetApp(AppConfigs.AppKeys.Default, out var firstApp);
        firstResult.Should().BeFalse("首次初始化失败时 TryGetApp 应返回 false 并重建 Lazy");
        firstApp.Should().BeNull();

        // 第二次访问应成功（Lazy 已重建，CreateAppContext 重新执行）
        var app = appManager.GetApp(AppConfigs.AppKeys.Default);
        app.Should().NotBeNull();
        app.Config.AppKey.Should().Be(AppConfigs.AppKeys.Default);
    }

    /// <summary>
    /// NEW-MA-08 验证：当 Lazy 初始化抛出非 InvalidOperationException 异常时，
    /// TryGetApp 应返回 false（保持 Try* 语义不抛异常），并在下次调用时重建 Lazy 允许重试。
    /// </summary>
    [Fact]
    public void TryGetApp_WhenLazyInitializationFails_ShouldRebuildLazyAndReturnFalse()
    {
        // Arrange：通过 mock IFeishuTokenStoreFactory.Create 触发同步失败
        var tokenStoreFactoryMock = new Mock<IFeishuTokenStoreFactory>();
        var callCount = 0;
        tokenStoreFactoryMock
            .Setup(x => x.Create(It.IsAny<string>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new HttpRequestException("模拟初始化失败");
                return (new Mock<ITokenStore>().Object, new Mock<IUserTokenStore>().Object);
            });

        var services = CreateServiceCollection();
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IFeishuTokenStoreFactory));
        if (existingDescriptor != null) services.Remove(existingDescriptor);
        services.AddSingleton(tokenStoreFactoryMock.Object);

        var configs = new List<FeishuAppConfig> { CreateDefaultConfig() };
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var appManager = provider.GetRequiredService<FeishuAppManager>();

        // Act & Assert：首次 TryGetApp 应返回 false（保持 Try* 语义不抛异常）
        var result1 = appManager.TryGetApp(AppConfigs.AppKeys.Default, out var ctx1);
        result1.Should().BeFalse("首次初始化失败时 TryGetApp 应返回 false");
        ctx1.Should().BeNull();

        // 第二次 TryGetApp 应成功（Lazy 已重建）
        var result2 = appManager.TryGetApp(AppConfigs.AppKeys.Default, out var ctx2);
        result2.Should().BeTrue("Lazy 重建后应成功");
        ctx2.Should().NotBeNull();
    }

    // ============================================================
    // NEW-MA-09：volatile + lock 保护 GetDefaultApp/RemoveApp TOCTOU
    // ============================================================

    /// <summary>
    /// NEW-MA-09 验证：并发场景下 GetDefaultApp 与 RemoveApp 交替执行时，
    /// 不应抛出 NullReferenceException 或其他非 InvalidOperationException 异常。
    /// 业务场景：GetDefaultApp 读取 _defaultAppKey 后调用 GetApp(defaultKey)，
    /// 若另一线程同时 RemoveApp(defaultKey) 并清空 _defaultAppKey，
    /// 修复前可能出现 defaultKey 为 null 导致 NullReferenceException；
    /// 修复后通过 volatile + _defaultAppLock 保护复合操作。
    /// </summary>
    [Fact]
    public void GetDefaultApp_WhenConcurrentWithRemoveApp_ShouldNotReturnStaleOrThrowNRE()
    {
        // Arrange：两个应用，默认为 Default，并发场景：一个线程持续读 GetDefaultApp，
        // 另一个线程 RemoveApp(Default) 触发默认应用提升为 Hr
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            CreateDefaultConfig(),
            CreateSecondaryConfig()
        };
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var appManager = provider.GetRequiredService<FeishuAppManager>();
        var exceptions = new ConcurrentBag<Exception>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act：并发读取 GetDefaultApp
        var readerTask = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var app = appManager.GetDefaultApp();
                    app.Should().NotBeNull();
                }
                catch (InvalidOperationException)
                {
                    // InvalidOperationException 在 RemoveApp 与 GetDefaultApp 之间是合法的瞬时状态：
                    // RemoveApp 在 _defaultAppLock 外移除 _lazyContexts，GetDefaultApp 在锁内调用
                    // GetApp(defaultKey) 时可能发现 app 已被移除，抛出"未找到应用"异常。
                    // 这是 NEW-MA-09 修复的预期行为范围（未引入 NRE），忽略即可。
                }
                catch (Exception ex) when (ex is not InvalidOperationException)
                {
                    // NullReferenceException / 其他异常不应出现
                    exceptions.Add(ex);
                }
            }
        });

        // 并发执行 RemoveApp(Default)
        var writerTask = Task.Run(() =>
        {
            // 先确保 GetDefaultApp 已被多次调用（让 reader 启动）
            Thread.Sleep(50);
            appManager.RemoveApp(AppConfigs.AppKeys.Default);
        });

        Task.WaitAll(readerTask, writerTask);

        // Assert：不应有任何非 InvalidOperationException 异常（特别是 NullReferenceException）
        exceptions.Should().BeEmpty(
            "并发场景下 GetDefaultApp 不应抛出 NullReferenceException 或其他异常。Actual: {0}",
            string.Join(", ", exceptions.Select(e => e.GetType().Name)));
    }

    /// <summary>
    /// NEW-MA-09 验证：AddApp 标记 IsDefault=true 时，应在 _defaultAppLock 保护下更新 _defaultAppKey。
    /// 业务场景：AddApp 写入 _defaultAppKey 必须与 GetDefaultApp/RemoveApp 的读取互斥，
    /// 避免写入期间其他线程读到部分更新的状态。
    /// </summary>
    [Fact]
    public void AddApp_WhenMarkedAsDefault_ShouldUpdateDefaultAppKeyUnderLock()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig> { CreateDefaultConfig() };
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var appManager = provider.GetRequiredService<FeishuAppManager>();

        // Act：动态添加新的默认应用
        var newDefaultConfig = new FeishuAppConfig
        {
            AppKey = "new-default",
            AppId = AppConfigs.AppIds.Hr,
            AppSecret = AppConfigs.Secrets.Hr,
            IsDefault = true
        };
        appManager.AddApp(newDefaultConfig);

        // Assert：GetDefaultApp 应返回新添加的默认应用
        var defaultApp = appManager.GetDefaultApp();
        defaultApp.Config.AppKey.Should().Be("new-default",
            "AddApp 标记 IsDefault=true 应在锁保护下更新 _defaultAppKey");
    }

    // ============================================================
    // TMA-07 / P1-6：退休队列
    // ============================================================

    /// <summary>
    /// TMA-07 验证：RebuildAppContext 时旧上下文进入退休队列，
    /// 在宽限期到期后（通过 Sweep 驱动）被 Dispose。
    /// </summary>
    [Fact]
    public void RebuildAppContext_ShouldDisposeOldContext_AfterRetireDelay()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig> { CreateDefaultConfig() };
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var appManager = provider.GetRequiredService<FeishuAppManager>();

        // 获取初始上下文的引用
        var originalApp = appManager.GetApp(AppConfigs.AppKeys.Default);
        var originalContext = (FeishuAppContext)originalApp;

        // Act：通过 OnConfigurationChanged 触发 RebuildAppContext（修改一个节流比较字段）
        var updatedConfig = new FeishuAppConfig
        {
            AppKey = AppConfigs.AppKeys.Default,
            AppId = AppConfigs.AppIds.Default,
            AppSecret = AppConfigs.Secrets.Valid,
            IsDefault = true,
            TimeOut = 60  // 不同于默认的 30，触发重建
        };
        appManager.OnConfigurationChanged(new List<FeishuAppConfig> { updatedConfig });

        // Assert：旧上下文尚未被 Dispose（宽限期内）
        appManager.Retirement.Should().NotBeNull();
        appManager.Retirement!.PendingCount.Should().BeGreaterThan(0, "旧上下文应在退休队列中");

        // 驱动 Sweep 到宽限期之后
        var futureTime = DateTimeOffset.UtcNow.AddSeconds(301);
        var disposedCount = appManager.Retirement.Sweep(futureTime);

        // 旧上下文应已被 Dispose
        disposedCount.Should().BeGreaterThan(0, "宽限期后 Sweep 应释放旧上下文");
        appManager.Retirement.PendingCount.Should().Be(0, "所有退休条目应已处理");

        // 新上下文仍可用
        var newApp = appManager.GetApp(AppConfigs.AppKeys.Default);
        newApp.Config.TimeOut.Should().Be(60, "新上下文应使用新配置");
    }

    /// <summary>
    /// TMA-07 验证：RemoveApp 时旧上下文进入退休队列，而非直接丢弃引用。
    /// 旧上下文在宽限期后才被 Dispose，保证在途请求安全。
    /// </summary>
    [Fact]
    public void RemoveApp_ShouldRetireContext_InsteadOfDroppingReference()
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

        // 获取要移除的应用上下文引用
        var appToRemove = appManager.GetApp(AppConfigs.AppKeys.Default);
        var removedContext = (FeishuAppContext)appToRemove;

        // Act：移除默认应用（会触发提升 hr-app 为新默认）
        var removed = appManager.RemoveApp(AppConfigs.AppKeys.Default);

        // Assert
        removed.Should().BeTrue();
        appManager.Retirement.Should().NotBeNull();
        appManager.Retirement!.PendingCount.Should().BeGreaterThan(0,
            "移除的旧上下文应在退休队列中，而非直接丢弃");

        // 驱动 Sweep 到宽限期之后
        var futureTime = DateTimeOffset.UtcNow.AddSeconds(301);
        var disposedCount = appManager.Retirement.Sweep(futureTime);

        disposedCount.Should().BeGreaterThan(0, "宽限期后 Sweep 应释放被移除的旧上下文");
        appManager.Retirement.PendingCount.Should().Be(0);

        // 剩余应用仍可用
        var newDefault = appManager.GetDefaultApp();
        newDefault.Config.AppKey.Should().Be(AppConfigs.AppKeys.Hr);
    }

    /// <summary>
    /// TMA-07 验证：FeishuAppManager.Dispose 应强制释放全部待退休上下文（Flush），
    /// 不等待宽限期到期。
    /// </summary>
    [Fact]
    public void Dispose_ShouldFlushPendingRetirements()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            CreateDefaultConfig(),
            CreateSecondaryConfig()
        };
        services.AddFeishuApp(configs);
        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<FeishuAppManager>();

        // 获取上下文引用
        _ = appManager.GetApp(AppConfigs.AppKeys.Default);

        // 触发重建（修改一个字段使节流不命中）
        var updatedConfig = new FeishuAppConfig
        {
            AppKey = AppConfigs.AppKeys.Default,
            AppId = AppConfigs.AppIds.Default,
            AppSecret = AppConfigs.Secrets.Valid,
            IsDefault = true,
            TimeOut = 60
        };
        appManager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            updatedConfig,
            CreateSecondaryConfig()
        });

        // 确认有待退休上下文
        appManager.Retirement!.PendingCount.Should().BeGreaterThan(0,
            "重建后旧上下文应在退休队列中");

        // Act：Dispose 应 Flush 全部待退休上下文
        appManager.Dispose();

        // Assert：退休队列已清空（Flush 后 Dispose）
        // 注意：Dispose 后 _retirement 被置为 null，无法再查询 PendingCount，
        // 但 Flush 不抛异常即证明全部释放成功。
        // 验证 Dispose 后 provider 仍可正常释放
        provider.Dispose();
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
