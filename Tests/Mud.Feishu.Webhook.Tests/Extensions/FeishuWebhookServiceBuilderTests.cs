// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Utils;

namespace Mud.Feishu.Webhook.Tests.Extensions;

/// <summary>
/// FeishuWebhookServiceBuilder 契约测试（P1-1/R2）：
/// PostConfigure 随 IOptionsMonitor 缓存重建重放，注册+冻结必须只在首次执行。
/// </summary>
public class FeishuWebhookServiceBuilderTests
{
    private sealed class TestAppHandler : IFeishuEventHandler
    {
        public string SupportedEventType => "test.event";
        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// 全局处理器桩（R3-P1-4）：仅“全局注册”的处理器可充当默认处理器，
    /// <c>Build()</c> 现在强制要求至少一个（应用专属处理器不得充当全局默认处理器）。
    /// </summary>
    private sealed class TestGlobalHandler : IFeishuEventHandler
    {
        public string SupportedEventType => "global.event";
        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// 手动触发的 Options 变更令牌源（模拟配置热更）。
    /// ConfigurationReloadToken 为单次性令牌：触发后必须换发新实例，
    /// 否则 OptionsManager 在热更重注册时会命中已消费的令牌导致运行中止。
    /// </summary>
    private sealed class ManualChangeTokenSource : IOptionsChangeTokenSource<FeishuWebhookOptions>
    {
        private ConfigurationReloadToken _token = new();
        public string Name => Options.DefaultName;
        public IChangeToken GetChangeToken() => _token;
        public void FireChange() => Interlocked.Exchange(ref _token, new ConfigurationReloadToken())?.OnReload();
    }

    /// <summary>首次 Options 构建时合法、第二次（热更重建）注入非法值的配置器</summary>
    private sealed class SecondBuildInvalidatingConfigurer : IConfigureOptions<FeishuWebhookOptions>
    {
        private int _runs;
        public void Configure(FeishuWebhookOptions options)
        {
            if (Interlocked.Increment(ref _runs) >= 2)
                options.EventHandlingTimeoutMs = 100; // Validate 要求 >= 1000
        }
    }

    [Fact]
    public void Build_RegistryRegistration_ShouldRunExactlyOnce_AcrossOptionsRebuilds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var changeTokenSource = new ManualChangeTokenSource();
        services.AddSingleton<IOptionsChangeTokenSource<FeishuWebhookOptions>>(changeTokenSource);

        services.CreateFeishuWebhookServiceBuilder(options =>
            {
                options.EventHandlingTimeoutMs = 5000;

                // R3-P0-1/D1：本套件运行于“生产”判定（EnvironmentService 在
                // ASPNETCORE_ENVIRONMENT 缺失时默认 Production），内存 Nonce 去重会被启动期阻断。
                // 本用例关注注册表/校验重放行为，与 Nonce 形态无关，故显式豁免。
                options.AllowInMemoryNonceDedupInProduction = true;
            })
            .AddHandler<TestGlobalHandler>()      // R3-P1-4：全局处理器（默认处理器来源）
            .AddHandler<TestAppHandler>("app-001")
            .Build();

        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<FeishuWebhookOptions>>();
        var handlerRegistry = provider.GetRequiredService<FeishuWebhookHandlerRegistry>();

        // 首次 Options 构建：注册+冻结执行（首个 CurrentValue 访问触发）
        monitor.CurrentValue.Should().NotBeNull();
        handlerRegistry.HasAny("app-001").Should().BeTrue("首次 Options 构建应完成应用处理器注册");
        handlerRegistry.GetAll("app-001").Should().ContainSingle(t => t == typeof(TestAppHandler));

        // Act：模拟热更——change token 触发缓存失效，下一次 CurrentValue 重建并重放 PostConfigure
        changeTokenSource.FireChange();

        // Assert：修复前此处抛 InvalidOperationException("注册表已冻结...")，热更后所有请求 500；
        // 修复后 CurrentValue 正常解析
        monitor.CurrentValue.Should().NotBeNull();

        // 注册表内容不变：未被重复注册、未被解冻绕过（仍只有 1 个处理器类型）
        handlerRegistry.GetAll("app-001").Should().ContainSingle(t => t == typeof(TestAppHandler),
            "热更重放不得重复注册处理器");

        // 再次热更仍稳定
        changeTokenSource.FireChange();
        monitor.CurrentValue.Should().NotBeNull();
        handlerRegistry.GetAll("app-001").Should().ContainSingle(t => t == typeof(TestAppHandler));
    }

    [Fact]
    public void Build_OptionsValidation_ShouldRunOnEveryOptionsRebuild()
    {
        // Arrange：验证逻辑必须留在一次性守卫之外——每次重建都应执行 options.Validate()
        var services = new ServiceCollection();
        services.AddLogging();
        var changeTokenSource = new ManualChangeTokenSource();
        services.AddSingleton<IOptionsChangeTokenSource<FeishuWebhookOptions>>(changeTokenSource);

        services.CreateFeishuWebhookServiceBuilder(options =>
            {
                options.EventHandlingTimeoutMs = 5000;

                // R3-P0-1/D1：本套件运行于“生产”判定（EnvironmentService 在
                // ASPNETCORE_ENVIRONMENT 缺失时默认 Production），内存 Nonce 去重会被启动期阻断。
                // 本用例关注注册表/校验重放行为，与 Nonce 形态无关，故显式豁免。
                options.AllowInMemoryNonceDedupInProduction = true;
            })
            .AddHandler<TestGlobalHandler>()      // R3-P1-4：全局处理器（默认处理器来源）
            .AddHandler<TestAppHandler>("app-001")
            .Build();

        // 注意：必须注册在 Build() 之后——builder 的 configure lambda 在 Build() 时才进入
        // ServiceCollection；本配置器若先注册，会在热更重建时被 builder lambda 覆盖回合法值。
        services.AddSingleton<IConfigureOptions<FeishuWebhookOptions>, SecondBuildInvalidatingConfigurer>();

        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<FeishuWebhookOptions>>();

        // 首次构建：配置合法
        monitor.CurrentValue.Should().NotBeNull();

        // Act：热更触发第二次构建（配置器注入非法 EventHandlingTimeoutMs）
        // 注：容器内存在 OnChange 监听器时，OptionsMonitor 会在失效回调中急切重建，
        // 校验异常因此同步浮出于 FireChange（AggregateException 包裹）；否则延迟到
        // 下一次 CurrentValue 访问。两种浮出位置语义等价——重建必触发校验。
        var fireEx = Record.Exception(changeTokenSource.FireChange);

        // Assert：Validate 在热更路径上仍被执行——非法配置被拒绝
        if (fireEx is null)
        {
            var act = () => monitor.CurrentValue;
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*EventHandlingTimeoutMs*");
        }
        else
        {
            fireEx.Should().BeOfType<AggregateException>(
                "急切重建路径上，校验异常经 ChangeToken 回调聚合后浮出");
            fireEx.InnerException.Should().BeOfType<InvalidOperationException>()
                .Which.Message.Should().Contain("EventHandlingTimeoutMs");
        }
    }

    // ============================================================
    // R3-P0-1（D1）：Nonce 去重「实现形态」必须与部署形态显式绑定
    // ============================================================

    /// <summary>记录 Warning 的日志提供程序替身。</summary>
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public readonly List<string> Messages = new();
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);
        public void Dispose() { }

        private sealed class CapturingLogger : ILogger
        {
            private readonly List<string> _sink;
            public CapturingLogger(List<string> sink) => _sink = sink;
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NoopScope();
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (logLevel is LogLevel.Warning or LogLevel.Error or LogLevel.Critical)
                    _sink.Add(formatter(state, exception));
            }
            private sealed class NoopScope : IDisposable { public void Dispose() { } }
        }
    }

    private sealed class StubEnvironmentService : IEnvironmentService
    {
        public StubEnvironmentService(bool isProduction) => IsProduction = isProduction;
        public bool IsProduction { get; }
        public bool IsDevelopment => !IsProduction;
        public bool IsStaging => false;
        public string EnvironmentName => IsProduction ? "Production" : "Development";
    }

    private static IServiceCollection CreateBaseServices(
        IEnvironmentService environment,
        CapturingLoggerProvider? loggerProvider = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            if (loggerProvider != null)
                builder.AddProvider(loggerProvider);
        });
        services.AddSingleton(environment);
        return services;
    }

    [Fact]
    public void MemoryNonce_InProductionWithoutDistributedMode_ShouldThrow_WhenNoExplicitOptIn()
    {
        // Arrange：生产 + 不配置 FeishuDeduplication 节 + 未注册 Redis（= 默认路径）
        var services = CreateBaseServices(new StubEnvironmentService(isProduction: true));

        services.CreateFeishuWebhookServiceBuilder(options =>
            {
                options.EventHandlingTimeoutMs = 5000;
            })
            .AddHandler<TestGlobalHandler>()
            .Build();

        using var provider = services.BuildServiceProvider();

        // Act / Assert：默认路径也必须阻断（v1.0 前：isDistributedIntent=false → 静默放行）
        var act = () => provider.GetRequiredService<IOptions<FeishuWebhookOptions>>().Value;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AddFeishuRedisDeduplicators*")
            .WithMessage("*AllowInMemoryNonceDedupInProduction*");
    }

    [Fact]
    public void MemoryNonce_InProduction_WhenExplicitOptIn_ShouldNotThrowAndLogWarning()
    {
        // Arrange
        var loggerProvider = new CapturingLoggerProvider();
        var services = CreateBaseServices(new StubEnvironmentService(isProduction: true), loggerProvider);

        services.CreateFeishuWebhookServiceBuilder(options =>
            {
                options.EventHandlingTimeoutMs = 5000;
                options.AllowInMemoryNonceDedupInProduction = true;
            })
            .AddHandler<TestGlobalHandler>()
            .Build();

        using var provider = services.BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<IOptions<FeishuWebhookOptions>>().Value;

        // Assert：不阻断，但仍保留可观测性
        act.Should().NotThrow();
        loggerProvider.Messages.Should().Contain(m => m.Contains("单实例"),
            "显式豁免也必须留下 Warning，便于审计发现“生产在用内存 Nonce”");
    }

    [Fact]
    public void MemoryNonce_InProduction_WhenDistributedNonceRegistered_ShouldNotThrow()
    {
        // Arrange：预注册分布式 Nonce 实现（模拟已 AddFeishuRedisDeduplicators）
        var loggerProvider = new CapturingLoggerProvider();
        var services = CreateBaseServices(new StubEnvironmentService(isProduction: true), loggerProvider);
        services.AddSingleton(Mock.Of<IFeishuNonceDistributedDeduplicator>());

        services.CreateFeishuWebhookServiceBuilder(options =>
            {
                options.EventHandlingTimeoutMs = 5000;
            })
            .AddHandler<TestGlobalHandler>()
            .Build();

        using var provider = services.BuildServiceProvider();

        // Act / Assert
        var act = () => provider.GetRequiredService<IOptions<FeishuWebhookOptions>>().Value;
        act.Should().NotThrow();
        loggerProvider.Messages.Should().NotContain(m => m.Contains("进程内内存 Nonce 去重"),
            "已接入分布式实现时不该再有降级告警");
    }

    [Fact]
    public void MemoryNonce_InDevelopment_ShouldWarnButNotThrow()
    {
        // Arrange
        var loggerProvider = new CapturingLoggerProvider();
        var services = CreateBaseServices(new StubEnvironmentService(isProduction: false), loggerProvider);

        services.CreateFeishuWebhookServiceBuilder(options =>
            {
                options.EventHandlingTimeoutMs = 5000;
            })
            .AddHandler<TestGlobalHandler>()
            .Build();

        using var provider = services.BuildServiceProvider();

        // Act / Assert
        var act = () => provider.GetRequiredService<IOptions<FeishuWebhookOptions>>().Value;
        act.Should().NotThrow("非生产环境不阻断，仅告警");
        loggerProvider.Messages.Should().Contain(m => m.Contains("进程内内存"));
    }

    // ============================================================
    // R3-P0-4：去重工厂不得自解析 IFeishuEventDeduplicator（递归 → StackOverflow）
    // ============================================================

    [Fact]
    public void DeduplicatorFactory_ShouldNotResolveItself()
    {
        // Arrange：Mode=Distributed + 未注册 Redis 实现——正是原实现会自解析递归的组合。
        // 修复前：工厂内 `sp.GetService<IFeishuEventDeduplicator>() is null` 强制先求值 GetService，
        // 而 TryAddSingleton 仅在该服务无其它注册时才注册本工厂 → 无限递归 → StackOverflowException
        // （不可捕获，进程终止）。因此本用例“能跑完”本身就是回归证明。
        var services = CreateBaseServices(new StubEnvironmentService(isProduction: false));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuDeduplication:Mode"] = "Distributed"
            })
            .Build());

        services.CreateFeishuWebhookServiceBuilder(options =>
            {
                options.EventHandlingTimeoutMs = 5000;
                options.AllowInMemoryNonceDedupInProduction = true;
            })
            .AddHandler<TestGlobalHandler>()
            .Build();

        using var provider = services.BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<IFeishuEventDeduplicator>();

        // Assert
        act.Should().NotThrow();
        provider.GetRequiredService<IFeishuEventDeduplicator>().Should().BeOfType<FeishuEventDeduplicator>();
    }

    [Fact]
    public void DistributedModeWithoutRedis_ShouldLogWarningAndNotThrow()
    {
        // Arrange
        var loggerProvider = new CapturingLoggerProvider();
        var services = CreateBaseServices(new StubEnvironmentService(isProduction: false), loggerProvider);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuDeduplication:Mode"] = "Distributed"
            })
            .Build());

        services.CreateFeishuWebhookServiceBuilder(options =>
            {
                options.EventHandlingTimeoutMs = 5000;
                options.AllowInMemoryNonceDedupInProduction = true;
            })
            .AddHandler<TestGlobalHandler>()
            .Build();

        using var provider = services.BuildServiceProvider();

        // Act：构建 Options 触发形态检查
        _ = provider.GetRequiredService<IOptions<FeishuWebhookOptions>>().Value;

        // Assert：v1.0 中该告警**永不触发**（因为自解析先崩溃）；现在必须可见
        loggerProvider.Messages.Should().Contain(m => m.Contains("事件去重"),
            "事件去重形态告警必须从工厂下沉到 PostConfigure 并真正生效");
    }

    // ============================================================
    // R3-P1-4：默认处理器只能是「全局注册」的处理器
    // ============================================================

    [Fact]
    public void AppOnlyHandlers_WithoutGlobalHandler_ShouldThrowOnBuild()
    {
        // Arrange
        var services = CreateBaseServices(new StubEnvironmentService(isProduction: false));

        // Act
        var act = () => services.CreateFeishuWebhookServiceBuilder(options =>
            {
                options.EventHandlingTimeoutMs = 5000;
            })
            .AddHandler<TestAppHandler>("app-001")
            .Build();

        // Assert：v1.0 前静默选 AppHandler 作默认处理器 → 跨应用事件串扰
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*全局*")
            .WithMessage("*跨应用*");
    }

    [Fact]
    public void DefaultHandler_ShouldBeFirstGlobalHandler_NotAppSpecific()
    {
        // Arrange：先注册应用专属，再注册全局——默认处理器仍必须是全局的那个
        var services = CreateBaseServices(new StubEnvironmentService(isProduction: false));

        services.CreateFeishuWebhookServiceBuilder(options =>
            {
                options.EventHandlingTimeoutMs = 5000;
                options.AllowInMemoryNonceDedupInProduction = true;
            })
            .AddHandler<TestAppHandler>("app-001")
            .AddHandler<TestGlobalHandler>()
            .Build();

        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IOptions<FeishuWebhookOptions>>().Value;
        using var scope = provider.CreateScope();

        // Act：未注册事件类型 → 工厂回退默认处理器
        var factory = scope.ServiceProvider.GetRequiredService<IFeishuEventHandlerFactory>();
        var defaultHandler = factory.GetHandler("some.unregistered.event.type");

        // Assert：默认处理器必须是全局注册的那个，不得是应用专属处理器
        defaultHandler.Should().BeOfType<TestGlobalHandler>(
            "默认处理器不得落到应用专属处理器——否则其它应用的事件会被 A 应用处理器处理（跨应用串扰）");
    }

    // ============================================================
    // R3-P0-5：启动期校验必须显式注册（不得依赖 ConcurrencyService 的隐式副作用）
    // ============================================================

    [Fact]
    public void Build_ShouldRegisterStartupOptionsValidator_AsHostedService()
    {
        // Arrange
        var services = CreateBaseServices(new StubEnvironmentService(isProduction: false));

        services.CreateFeishuWebhookServiceBuilder(options =>
            {
                options.EventHandlingTimeoutMs = 5000;
                options.AllowInMemoryNonceDedupInProduction = true;
            })
            .AddHandler<TestGlobalHandler>()
            .Build();

        // Assert：宿主启动期必须有显式机制触发 Options 构建与校验
        services.Should().Contain(d =>
            d.ServiceType == typeof(IHostedService) &&
            d.ImplementationFactory != null,
            "R3-P0-5：ValidateOnStart 在 net6.0 下有 CS0121 二义性，改用显式 HostedService");
    }
}
