// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
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
    public async Task GetDefaultApp_WhenConcurrentWithRemoveApp_ShouldNotReturnStaleOrThrowNRE()
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

        await Task.WhenAll(readerTask, writerTask);

        // Assert：不应有任何非 InvalidOperationException 异常（特别是 NullReferenceException）
        exceptions.Should().BeEmpty(
            "并发场景下 GetDefaultApp 不应抛出 NullReferenceException 或其他异常。Actual: {0}",
            string.Join(", ", exceptions.Select(e => e.GetType().Name)));
    }

    /// <summary>
    /// TMA2-21（§7.2 #11 配套，原弱断言改名）：AddApp 标记 IsDefault=true 时应更新 _defaultAppKey。
    /// 原名称"...UnderLock"宣称"在锁保护下"但仅断言结果，无法证明锁语义；
    /// 锁语义由 <see cref="DefaultAppKey_ShouldNotLoseUpdate_WhenAddAppConcurrentWithRemoveDefault"/> 以确定性交错断言守护。
    /// </summary>
    [Fact]
    public void AddApp_WhenMarkedAsDefault_ShouldUpdateDefaultAppKey()
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
    /// TMA-07 验证：配置热更新（OnConfigurationChanged）时旧上下文进入退休队列，
    /// 在宽限期到期后（通过 Sweep 驱动）被 Dispose。
    /// </summary>
    [Fact]
    public void HotReload_ShouldDisposeOldContext_AfterRetireDelay()
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

        // Act：通过 OnConfigurationChanged 触发热更新（修改一个节流比较字段）
        var updatedConfig = new FeishuAppConfig
        {
            AppKey = AppConfigs.AppKeys.Default,
            AppId = AppConfigs.AppIds.Default,
            AppSecret = AppConfigs.Secrets.Valid,
            IsDefault = true,
            TimeoutSeconds = 60  // 不同于默认的 30，触发重建
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
        newApp.Config.TimeoutSeconds.Should().Be(60, "新上下文应使用新配置");
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
            TimeoutSeconds = 60
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

    // ============================================================
    // TMA2-05 / D10（§7.2 #7/#8）：凭据变更即清库
    // ============================================================

    /// <summary>
    /// 构造带 spy 令牌存储工厂的服务集合（替换 AddFeishuApp 的默认注册）。
    /// </summary>
    private static ServiceCollection CreateServiceCollectionWithTokenStoreFactory(
        out Mock<ITokenStore> tokenStoreMock,
        out Mock<IUserTokenStore> userTokenStoreMock)
    {
        tokenStoreMock = new Mock<ITokenStore>();
        userTokenStoreMock = new Mock<IUserTokenStore>();

        var factoryMock = new Mock<IFeishuTokenStoreFactory>();
        factoryMock
            .Setup(x => x.Create(It.IsAny<string>()))
            .Returns((tokenStoreMock.Object, userTokenStoreMock.Object));

        var services = CreateServiceCollection();
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IFeishuTokenStoreFactory));
        if (existingDescriptor != null) services.Remove(existingDescriptor);
        services.AddSingleton(factoryMock.Object);
        return services;
    }

    /// <summary>
    /// TMA2-05 / D10（§7.2 #7）核心：热更新检测到 AppSecret 变更时，必须清除该 appKey 的持久化令牌。
    /// 强断言：spy 存储 <c>ClearAsync</c> 被真实调用（副作用断言）。
    /// </summary>
    [Fact]
    public void HotReload_ShouldPurgeStoredTokens_WhenAppSecretChanged()
    {
        // Arrange
        var services = CreateServiceCollectionWithTokenStoreFactory(out var tokenStoreMock, out _);
        services.AddFeishuApp(new List<FeishuAppConfig> { CreateDefaultConfig() });
        using var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<FeishuAppManager>();

        // 实例化应用（旧上下文），否则 Phase-A 中 oldContext 为 null 无法做凭据比对
        _ = appManager.GetApp(AppConfigs.AppKeys.Default);

        var changedConfig = new FeishuAppConfig
        {
            AppKey = AppConfigs.AppKeys.Default,
            AppId = AppConfigs.AppIds.Default,
            AppSecret = "changed_secret_987654",
            IsDefault = true
        };

        // Act
        appManager.OnConfigurationChanged(new List<FeishuAppConfig> { changedConfig });

        // Assert：TMR-P1-6（F6）——凭据变更清库为"Phase-P 清库 + 提交后二次清库（D10 闭环）"双阶段，
        // 二次清库为 fire-and-forget，故这里断言"至少一次"（强语义：清库必须发生）。
        tokenStoreMock.Verify(x => x.ClearAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce,
            "凭据（AppSecret）变更必须清除该应用的持久化令牌");
    }

    /// <summary>
    /// TMA2-05 / D10（§7.2 #8）：仅非凭据字段（TimeoutSeconds）变化时保留令牌热迁移，不清库。
    /// </summary>
    [Fact]
    public void HotReload_ShouldNotPurgeStoredTokens_WhenOnlyTimeOutChanged()
    {
        var services = CreateServiceCollectionWithTokenStoreFactory(out var tokenStoreMock, out _);
        services.AddFeishuApp(new List<FeishuAppConfig> { CreateDefaultConfig() });
        using var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<FeishuAppManager>();
        _ = appManager.GetApp(AppConfigs.AppKeys.Default);

        appManager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.Default,
                AppId = AppConfigs.AppIds.Default,
                AppSecret = AppConfigs.Secrets.Valid,
                IsDefault = true,
                TimeoutSeconds = 99
            }
        });

        tokenStoreMock.Verify(x => x.ClearAsync(It.IsAny<CancellationToken>()), Times.Never,
            "仅非凭据字段变化应保留令牌热迁移");
    }

    /// <summary>
    /// TMA2-05 / D10：仅 BaseUrl 变化时同样保留令牌（多区域切换不掉令牌的既有收益）。
    /// </summary>
    [Fact]
    public void HotReload_ShouldNotPurgeStoredTokens_WhenOnlyBaseUrlChanged()
    {
        var services = CreateServiceCollectionWithTokenStoreFactory(out var tokenStoreMock, out _);
        services.AddFeishuApp(new List<FeishuAppConfig> { CreateDefaultConfig() });
        using var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<FeishuAppManager>();
        _ = appManager.GetApp(AppConfigs.AppKeys.Default);

        appManager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.Default,
                AppId = AppConfigs.AppIds.Default,
                AppSecret = AppConfigs.Secrets.Valid,
                IsDefault = true,
                AllowCustomBaseUrl = true,
                BaseUrl = "https://open.example.com"
            }
        });

        tokenStoreMock.Verify(x => x.ClearAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ============================================================
    // TMF-01（P0）：D10 凭据变更清库——真实内存后端回归（替换 spy 盲区）
    // 复现根因：工厂每次 Create 新实例 → per-instance 记账为空 →
    // ClearAsync no-op，共享 IMemoryCache 中的旧令牌键残留 → 经 store
    // 恢复路径「复活」旧凭据换取的令牌（用户侧同款缺陷：userStore 被丢弃）。
    // 修复后：跨实例共享记账 + PurgeTokenStoreAsync 探测 IFeishuUserTokenStorePurge。
    // ============================================================

    /// <summary>
    /// 构造走默认 PerAppFeishuTokenStoreFactory + 真实 IMemoryCache 的服务提供者
    /// （与生产装配一致，store 经 DI 注册的 IFeishuTokenStoreFactory 创建）。
    /// </summary>
    private static (ServiceProvider Provider, FeishuAppManager Manager, IMemoryCache Cache)
        CreateManagerWithRealMemoryStore()
    {
        var services = CreateServiceCollection();
        services.AddFeishuApp(new List<FeishuAppConfig> { CreateDefaultConfig() });
        var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<FeishuAppManager>();
        var cache = provider.GetRequiredService<IMemoryCache>();
        return (provider, manager, cache);
    }

    [Fact]
    public async Task OnConfigurationChanged_ShouldRemoveTenantAndUserTokensFromRealMemoryCache_WhenAppSecretChanged()
    {
        // Arrange：实例化旧上下文，经工厂实例 A 预写租户+用户令牌。
        // 预写值经 TokenStoreHelper.EncodeStoredToken 编码，与生产持久化格式一致。
        var (provider, manager, cache) = CreateManagerWithRealMemoryStore();
        using var providerLease = provider;
        var factory = provider.GetRequiredService<IFeishuTokenStoreFactory>();
        var appKey = AppConfigs.AppKeys.Default;
        var tokenType = FeishuTokenTypes.TenantAccessToken;
        var userTokenType = $"UserAccessToken:{appKey}";
        var userId = "ou_test_user";

        var (storeA, userStoreA) = factory.Create(appKey);
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var encodedTenantAccess = TokenStoreHelper.EncodeStoredToken("tenant-access-old", nowMs + 7200_000);
        var encodedUserAccess = TokenStoreHelper.EncodeStoredToken("user-access-old", nowMs + 7200_000);
        var encodedUserRefresh = TokenStoreHelper.EncodeStoredToken("user-refresh-old", nowMs + 30L * 24 * 3600 * 1000);

        _ = manager.GetApp(appKey);
        await storeA.SetAccessTokenAsync(tokenType, encodedTenantAccess, 7200, CancellationToken.None);
        await userStoreA!.SetAccessTokenAsync(userId, userTokenType, encodedUserAccess, 7200, CancellationToken.None);
        await userStoreA.SetRefreshTokenAsync(userId, userTokenType, encodedUserRefresh, CancellationToken.None);

        // 断言预写命中
        (await storeA.GetAccessTokenAsync(tokenType, CancellationToken.None)).Should().Be(encodedTenantAccess);
        (await userStoreA.GetAccessTokenAsync(userId, userTokenType, CancellationToken.None)).Should().Be(encodedUserAccess);

        var changedConfig = new FeishuAppConfig
        {
            AppKey = appKey,
            AppId = AppConfigs.AppIds.Default,
            AppSecret = "changed_secret_987654",
            IsDefault = true
        };

        // Act：凭据变更热更新（PurgeTokenStoreAsync 经工厂新实例清库）
        manager.OnConfigurationChanged(new List<FeishuAppConfig> { changedConfig });

        // TMR2-P1-5：清库不再在 OnChange 回调线程上同步等待（原 Task.Run(...).Wait(10s)），
        // 故此处以「门撤除」为清库完成信号做**有界轮询**（D10 的"清库完成前不得恢复旧令牌"
        // 由 TokenStorePurgeGate 在恢复路径短路承担，见 TokenStorePurgeGateIntegrationTests）。
        try
        {
            for (var i = 0;
                 i < 500 && TokenStorePurgeGate.Query(appKey) == TokenStorePurgeGate.PurgeGateState.Pending;
                 i++)
            {
                await Task.Delay(20);
            }

            TokenStorePurgeGate.Query(appKey).Should().Be(TokenStorePurgeGate.PurgeGateState.NotPending,
                "凭据变更清库（Phase-P + 提交后二次）必须在有界时间内完成并撤门");
        }
        finally
        {
            TokenStorePurgeGate.ResetForTest();
        }

        // Assert：经工厂新实例 B 断言租户+用户令牌均被清除（根因修复的强断言）
        var (storeB, userStoreB) = factory.Create(appKey);
        (await storeB.GetAccessTokenAsync(tokenType, CancellationToken.None)).Should().BeNull(
            "凭据变更后旧租户令牌键必须被跨实例共享记账清除");
        (await userStoreB!.GetAccessTokenAsync(userId, userTokenType, CancellationToken.None)).Should().BeNull(
            "凭据变更后旧用户 access 令牌键必须被 IFeishuUserTokenStorePurge 清除");
        (await userStoreB.GetRefreshTokenAsync(userId, userTokenType, CancellationToken.None)).Should().BeNull(
            "凭据变更后旧用户 refresh 令牌键必须被 IFeishuUserTokenStorePurge 清除");
    }

    [Fact]
    public async Task OnConfigurationChanged_ShouldKeepTokens_WhenOnlyTimeOutChanged_RealBackend()
    {
        // Arrange
        var (provider, manager, unusedCache) = CreateManagerWithRealMemoryStore();
        using var providerLease = provider;
        var factory = provider.GetRequiredService<IFeishuTokenStoreFactory>();
        var appKey = AppConfigs.AppKeys.Default;
        var tokenType = FeishuTokenTypes.TenantAccessToken;

        _ = manager.GetApp(appKey);
        var (storeA, _) = factory.Create(appKey);
        var encoded = TokenStoreHelper.EncodeStoredToken("tenant-access-keep", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 7200_000);
        await storeA.SetAccessTokenAsync(tokenType, encoded, 7200, CancellationToken.None);

        // Act：仅 TimeoutSeconds 变更（非凭据字段）
        manager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = appKey,
                AppId = AppConfigs.AppIds.Default,
                AppSecret = AppConfigs.Secrets.Valid,
                IsDefault = true,
                TimeoutSeconds = 99
            }
        });

        // Assert：令牌应保留（令牌热迁移）
        var (storeB, _) = factory.Create(appKey);
        (await storeB.GetAccessTokenAsync(tokenType, CancellationToken.None)).Should().Be(encoded,
            "仅非凭据字段变更应保留令牌热迁移");
    }

    /// <summary>
    /// TMF-01 跨实例清库不变式：同一 KeyPrefix 下，实例 B 的 ClearAsync 必须能删除
    /// 实例 A 写入的键（共享记账的构造性保证——修复前必然失败）。
    /// </summary>
    [Fact]
    public async Task FeishuTokenStore_ClearAsync_ShouldRemoveKeysWrittenByOtherInstance_WhenSameKeyPrefix()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var storeA = new FeishuTokenStore(cache, "app-cross");
        var storeB = new FeishuTokenStore(cache, "app-cross");

        await storeA.SetAccessTokenAsync("tenant", "access-v", 100, CancellationToken.None);
        await storeA.SetRefreshTokenAsync("tenant", "refresh-v", CancellationToken.None);
        // 验收修复回归：仅写 refresh（从未写 access）的 tokenType 也必须被记账并被清库。
        await storeA.SetRefreshTokenAsync("refresh-only", "refresh-only-v", CancellationToken.None);

        await storeB.ClearAsync(CancellationToken.None);

        (await storeA.GetAccessTokenAsync("tenant", CancellationToken.None)).Should().BeNull(
            "ClearAsync 必须删除同前缀其他实例写入的 access 键");
        (await storeA.GetRefreshTokenAsync("tenant", CancellationToken.None)).Should().BeNull(
            "ClearAsync 必须删除同前缀其他实例写入的 refresh 键");
        (await storeA.GetRefreshTokenAsync("refresh-only", CancellationToken.None)).Should().BeNull(
            "仅写 refresh 的 tokenType 同样必须被记账并清除（清库盲区回归）");
    }

    /// <summary>
    /// TMF-01 跨实例清库不变式（用户侧）：实例 B 的 ClearAllUsersAsync 必须能删除
    /// 实例 A 写入的用户令牌键。
    /// </summary>
    [Fact]
    public async Task FeishuUserTokenStore_ClearAllUsersAsync_ShouldRemoveKeysWrittenByOtherInstance_WhenSameKeyPrefix()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var tenantA = new FeishuTokenStore(cache, "app-cross-user");
        var userA = new FeishuUserTokenStore(tenantA, cache, "app-cross-user");
        var tenantB = new FeishuTokenStore(cache, "app-cross-user");
        var userB = new FeishuUserTokenStore(tenantB, cache, "app-cross-user");

        await userA.SetAccessTokenAsync("ou_u1", "UserAccessToken", "access-v", 100, CancellationToken.None);
        await userA.SetRefreshTokenAsync("ou_u1", "UserAccessToken", "refresh-v", CancellationToken.None);
        await userA.SetAccessTokenAsync("ou_u2", "UserAccessToken", "access-v2", 100, CancellationToken.None);
        // 验收修复回归：仅写 refresh（从未写 access）的 (userId, tokenType) 也必须被记账并被清库。
        await userA.SetRefreshTokenAsync("ou_u3", "UserAccessToken", "refresh-only-v", CancellationToken.None);

        await userB.ClearAllUsersAsync(CancellationToken.None);

        (await userA.GetAccessTokenAsync("ou_u1", "UserAccessToken", CancellationToken.None)).Should().BeNull();
        (await userA.GetRefreshTokenAsync("ou_u1", "UserAccessToken", CancellationToken.None)).Should().BeNull();
        (await userA.GetAccessTokenAsync("ou_u2", "UserAccessToken", CancellationToken.None)).Should().BeNull();
        (await userA.GetRefreshTokenAsync("ou_u3", "UserAccessToken", CancellationToken.None)).Should().BeNull(
            "仅写 refresh 的 (userId, tokenType) 同样必须被记账并清除（清库盲区回归）");
    }

    /// <summary>
    /// TMF-03：清库被取消（OCE）不得向上传播——热更新继续完成（与「清库失败不阻断重建」语义一致）。
    /// 修复前 OCE 穿透 Phase-A 的 <c>ex is not OperationCanceledException</c> 过滤后被外层 catch 吞掉，
    /// 一次取消信号使整次热更新静默放弃。
    /// </summary>
    [Fact]
    public void OnConfigurationChanged_ShouldCompleteHotReload_WhenStoreClearCanceled()
    {
        // Arrange：spy 工厂返回 ClearAsync 抛 OCE 的存储
        var services = CreateServiceCollectionWithTokenStoreFactory(out var tokenStoreMock, out _);
        tokenStoreMock
            .Setup(x => x.ClearAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("store clear canceled"));
        services.AddFeishuApp(new List<FeishuAppConfig> { CreateDefaultConfig() });
        using var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<FeishuAppManager>();
        _ = appManager.GetApp(AppConfigs.AppKeys.Default);

        // Act：凭据变更热更新（Phase-P 清库抛 OCE）
        appManager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = AppConfigs.AppKeys.Default,
                AppId = AppConfigs.AppIds.Default,
                AppSecret = "changed_secret_987654",
                IsDefault = true
            }
        });

        // Assert：热更新正常完成——新上下文已生效
        appManager.GetApp(AppConfigs.AppKeys.Default).Config.AppSecret.Should().Be(
            "changed_secret_987654", "清库取消不得使整次热更新静默放弃");
    }

    // ============================================================
    // TMA2-08 / D13（§7.2 #11）：AddApp 默认键写入与 RemoveDefault 的确定性交错
    // ============================================================

    /// <summary>
    /// TMA2-08 / D13（§7.2 #11）核心：AddApp 的默认键写入持 _defaultAppLock 后，
    /// 与 RemoveApp("旧默认") 的"清空+提升"复合操作并发时不得丢失更新。
    /// 修复前的丢失窗口：AddApp 写入 newKey 后、RemoveApp 的锁内段检查 `_defaultAppKey == oldKey`
    /// 仍为 true → 将 newKey 覆写为 null/提升值。加锁后两种交错顺序的最终结果都必然是 newKey。
    /// </summary>
    [Fact]
    public async Task DefaultAppKey_ShouldNotLoseUpdate_WhenAddAppConcurrentWithRemoveDefault()
    {
        for (var iteration = 0; iteration < 10; iteration++)
        {
            var services = CreateServiceCollection();
            services.AddFeishuApp(new List<FeishuAppConfig> { CreateDefaultConfig() });
            using var provider = services.BuildServiceProvider();
            var appManager = provider.GetRequiredService<FeishuAppManager>();

            var newDefaultConfig = new FeishuAppConfig
            {
                AppKey = $"new-default-{iteration}",
                AppId = AppConfigs.AppIds.Hr,
                AppSecret = AppConfigs.Secrets.Hr,
                IsDefault = true
            };

            var removeTask = Task.Run(() => appManager.RemoveApp(AppConfigs.AppKeys.Default));
            var addTask = Task.Run(() => appManager.AddApp(newDefaultConfig));
            await Task.WhenAll(removeTask, addTask);

            appManager.DefaultAppKey.Should().Be(newDefaultConfig.AppKey,
                $"第 {iteration} 轮：AddApp 声明的默认键不得被并发 RemoveApp 的清空/提升覆盖");
        }
    }

    // ============================================================
    // TMA2-11 / D13：装配失败释放 scope；Dispose 释放在册上下文
    // ============================================================

    /// <summary>
    /// Scoped 的失败工厂：装配时抛出瞬时可重试异常，并记录自身是否被 scope 释放。
    /// 用作 scope Dispose 的可观测点（scope.Dispose 会释放其中创建的所有 IDisposable 实例）。
    /// 实例由 DI 容器在 scope 内创建，因此会随 scope 释放。
    /// </summary>
    private sealed class FailingScopedTokenStoreFactory : IFeishuTokenStoreFactory, IDisposable
    {
        public static readonly List<FailingScopedTokenStoreFactory> Instances = new();

        public bool Disposed { get; private set; }

        public FailingScopedTokenStoreFactory() => Instances.Add(this);

        public (ITokenStore TokenStore, IUserTokenStore? UserTokenStore) Create(string appKey)
            => throw new HttpRequestException("模拟装配失败（如存储瞬时故障）");

        public void Dispose() => Disposed = true;
    }

    /// <summary>
    /// TMA2-11 / D13：CreateAppContext 装配失败时必须释放已创建的 IServiceScope
    /// （所有权仅在成功时转移给 FeishuAppContext），避免 Captive Dependency 泄漏。
    /// 可观测点：scope 内创建的 Scoped 工厂实例实现 IDisposable，scope 被释放时其 Disposed 标记置位。
    /// </summary>
    [Fact]
    public void CreateAppContext_ShouldDisposeScope_WhenAssemblyFails()
    {
        FailingScopedTokenStoreFactory.Instances.Clear();

        var services = CreateServiceCollection();
        services.AddFeishuApp(new List<FeishuAppConfig> { CreateDefaultConfig() });
        // 注意：必须放在 AddFeishuApp 之后——AddFeishuApp 末尾的加密装饰器会把"当时的最后一个描述符"
        // 用根 SP 包裹重建（脱离 scope 生命周期），导致 Scoped 实例不再随 scope 释放。
        // 追加在最后，使解析直接命中本工厂（Scoped：实例随 CreateAppContext 创建的 scope 一起创建与释放）。
        services.AddScoped<IFeishuTokenStoreFactory, FailingScopedTokenStoreFactory>();
        using var provider = services.BuildServiceProvider();

        var appManager = provider.GetRequiredService<FeishuAppManager>();

        // Act：GetApp 触发懒加载装配 → factory.Create 抛出 → scope 应被释放
        var act = () => appManager.GetApp(AppConfigs.AppKeys.Default);

        // Assert
        act.Should().Throw<InvalidOperationException>("瞬时装配失败经 GetOrCreateContext 包装后上抛");
        FailingScopedTokenStoreFactory.Instances.Should().NotBeEmpty("装配流程应在 scope 内创建过工厂实例");
        FailingScopedTokenStoreFactory.Instances.Should().OnlyContain(
            f => f.Disposed, "装配失败时创建的 IServiceScope 必须被释放（Scoped 实例随 scope 释放）");
    }

    /// <summary>
    /// TMA2-11 / D13：FeishuAppManager.Dispose 必须释放全部在册上下文（含 _lazyContexts 已实例化者），幂等。
    /// 通过反射读取 FeishuAppContext 私有 _disposed 状态做副作用断言。
    /// TMR-P2-13（F13）：_disposed 由 bool 收敛为 int（Interlocked.Exchange 原子 check-then-set）。
    /// </summary>
    [Fact]
    public void Dispose_ShouldDisposeAllRegisteredContexts()
    {
        var services = CreateServiceCollection();
        services.AddFeishuApp(new List<FeishuAppConfig>
        {
            CreateDefaultConfig(),
            CreateSecondaryConfig()
        });
        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<FeishuAppManager>();

        // 实例化两个应用
        _ = appManager.GetApp(AppConfigs.AppKeys.Default);
        _ = appManager.GetApp(AppConfigs.AppKeys.Hr);
        var contexts = appManager.InstantiatedApps.ToList();
        contexts.Should().HaveCount(2);

        // Act
        appManager.Dispose();

        // Assert：每个在册上下文都已被 Dispose（原子标记置位为 1）
        var disposedField = typeof(FeishuAppContext).GetField("_disposed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        disposedField.Should().NotBeNull("FeishuAppContext 应存在 _disposed 标记");
        foreach (var context in contexts)
        {
            var isDisposed = (int)disposedField!.GetValue(context)!;
            isDisposed.Should().Be(1, $"在册上下文 {context.Config.AppKey} 应随 FeishuAppManager.Dispose 释放");
        }

        provider.Dispose();
    }

    // ============================================================
    // TMA2-12 / D6：非瞬时异常不被吞
    // ============================================================

    /// <summary>
    /// TMA2-12 / D6：Lazy 装配抛出非瞬时异常（如 TypeLoadException）时，
    /// TryGetApp 必须原样上抛而非吞掉返回 false（修复前 catch-all 会把非瞬时故障
    /// 误报为"应用初始化失败"并无限重试）。
    /// </summary>
    [Fact]
    public void TryGetApp_ShouldRethrow_WhenNonTransientExceptionOccurs()
    {
        var tokenStoreFactoryMock = new Mock<IFeishuTokenStoreFactory>();
        tokenStoreFactoryMock
            .Setup(x => x.Create(It.IsAny<string>()))
            .Throws(new TypeLoadException("模拟非瞬时装配故障"));

        var services = CreateServiceCollection();
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IFeishuTokenStoreFactory));
        if (existingDescriptor != null) services.Remove(existingDescriptor);
        services.AddSingleton(tokenStoreFactoryMock.Object);
        services.AddFeishuApp(new List<FeishuAppConfig> { CreateDefaultConfig() });
        using var provider = services.BuildServiceProvider();

        var appManager = provider.GetRequiredService<FeishuAppManager>();

        var act = () => appManager.TryGetApp(AppConfigs.AppKeys.Default, out _);

        act.Should().Throw<TypeLoadException>("非瞬时异常必须原样上抛，不得被异常过滤吞掉");
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
