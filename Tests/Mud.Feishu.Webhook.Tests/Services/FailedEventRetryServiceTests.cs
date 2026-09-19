// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会顺序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Services;

/// <summary>
/// FailedEventRetryService 单元测试
/// </summary>
public class FailedEventRetryServiceTests
{
    private readonly Mock<ILogger<FailedEventRetryService>> _loggerMock;
    private readonly Mock<IFeishuWebhookService> _webhookServiceMock;
    private readonly FailedEventRetryOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;

    public FailedEventRetryServiceTests()
    {
        _loggerMock = new Mock<ILogger<FailedEventRetryService>>();
        _webhookServiceMock = new Mock<IFeishuWebhookService>();
        _webhookServiceMock.Setup(x => x.SetCurrentAppKey(It.IsAny<string>())).Callback<string>(_ => { });
        _options = new FailedEventRetryOptions
        {
            EnableRetry = true,
            MaxRetryCount = 3,
            MaxRetryPerPoll = 10,
            RetryPollIntervalSeconds = 1
        };

        // 构建 scope factory，使其在 CreateScope 后返回 mock webhook service
        var services = new ServiceCollection();
        services.AddScoped(_ => _webhookServiceMock.Object);
        var provider = services.BuildServiceProvider();
        _scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
    }

    /// <summary>
    /// R5/X3：失败事件重试配置的唯一真相源是 <see cref="FeishuWebhookOptions.Retry"/>
    /// （此前服务注入 <c>IOptions&lt;FailedEventRetryOptions&gt;</c>，而该 Options 从未被注册/绑定，
    /// 导致 <c>FeishuWebhook:Retry:*</c> 的 6 个字段恒为默认值）。
    /// </summary>
    private static IOptionsMonitor<FeishuWebhookOptions> CreateWebhookOptionsMonitor(FailedEventRetryOptions retry)
    {
        var monitor = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        monitor.SetupGet(x => x.CurrentValue).Returns(new FeishuWebhookOptions { Retry = retry });
        return monitor.Object;
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var optionsMock = CreateWebhookOptionsMonitor(_options);

        // Act
        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _scopeFactory);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithRetryEnabledButNoEventStore_ShouldLogWarning()
    {
        // Arrange
        var options = new FailedEventRetryOptions
        {
            EnableRetry = true,
            MaxRetryCount = 3,
            MaxRetryPerPoll = 10,
            RetryPollIntervalSeconds = 1
        };
        var optionsMock = CreateWebhookOptionsMonitor(options);

        // Act
        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _scopeFactory,
            null);

        // Assert - Should not throw, but should log warning
        service.Should().NotBeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("未配置失败事件存储")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithRetryDisabled_ShouldNotLogWarning()
    {
        // Arrange
        var options = new FailedEventRetryOptions
        {
            EnableRetry = false,
            RetryPollIntervalSeconds = 1
        };
        var optionsMock = CreateWebhookOptionsMonitor(options);

        // Act
        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _scopeFactory,
            null);

        // Assert
        service.Should().NotBeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("未配置失败事件存储")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldCreateInstance()
    {
        // Arrange
        var eventStoreMock = new Mock<IFailedEventStore>();
        var optionsMock = CreateWebhookOptionsMonitor(_options);

        // Act
        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _scopeFactory,
            eventStoreMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithRetryDisabled_ShouldNotProcessEvent()
    {
        // Arrange
        var options = new FailedEventRetryOptions
        {
            EnableRetry = false,
            RetryPollIntervalSeconds = 1
        };
        var optionsMock = CreateWebhookOptionsMonitor(options);
        var eventStoreMock = new Mock<IFailedEventStore>();

        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _scopeFactory,
            eventStoreMock.Object);

        using var cts = new CancellationTokenSource(500);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(600);
        await service.StopAsync(CancellationToken.None);

        // Assert - 不应该调用事件存储
        eventStoreMock.Verify(
            x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoFailedEvents_ShouldNotProcess()
    {
        // Arrange
        var optionsMock = CreateWebhookOptionsMonitor(_options);
        var eventStoreMock = new Mock<IFailedEventStore>();
        eventStoreMock
            .Setup(x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FailedEventInfo>());

        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _scopeFactory,
            eventStoreMock.Object);

        using var cts = new CancellationTokenSource(500);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(600);
        await service.StopAsync(CancellationToken.None);

        // Assert
        eventStoreMock.Verify(
            x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedEvent_ShouldRetryAndSucceed()
    {
        // Arrange
        var optionsMock = CreateWebhookOptionsMonitor(_options);
        var eventStoreMock = new Mock<IFailedEventStore>();

        var failedEvent = new FailedEventInfo
        {
            EventId = "event-001",
            EventType = "test.event",
            SerializedEventData = "{\"eventId\":\"event-001\",\"eventType\":\"test.event\"}",
            RetryCount = 0,
            FailedAt = DateTime.UtcNow,
            NextRetryAt = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(1))
        };

        eventStoreMock
            .Setup(x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FailedEventInfo> { failedEvent });

        _webhookServiceMock
            .Setup(x => x.HandleEventAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (string?)null));

        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _scopeFactory,
            eventStoreMock.Object);

        using var cts = new CancellationTokenSource(1000);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(1200);
        await service.StopAsync(CancellationToken.None);

        // Assert
        eventStoreMock.Verify(
            x => x.RemoveFailedEventAsync("event-001", It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedEvent_ShouldRetryAndFail()
    {
        // Arrange
        var optionsMock = CreateWebhookOptionsMonitor(_options);
        var eventStoreMock = new Mock<IFailedEventStore>();

        var failedEvent = new FailedEventInfo
        {
            EventId = "event-002",
            EventType = "test.event",
            SerializedEventData = "{\"eventId\":\"event-002\",\"eventType\":\"test.event\"}",
            RetryCount = 0,
            FailedAt = DateTime.UtcNow,
            NextRetryAt = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(1))
        };

        eventStoreMock
            .Setup(x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FailedEventInfo> { failedEvent });

        _webhookServiceMock
            .Setup(x => x.HandleEventAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "处理失败"));

        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _scopeFactory,
            eventStoreMock.Object);

        using var cts = new CancellationTokenSource(1000);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(1200);
        await service.StopAsync(CancellationToken.None);

        // Assert
        eventStoreMock.Verify(
            x => x.UpdateFailedEventAsync(It.IsAny<FailedEventInfo>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WithMaxRetryExceeded_ShouldRemoveEvent()
    {
        // Arrange
        var optionsMock = CreateWebhookOptionsMonitor(_options);
        var eventStoreMock = new Mock<IFailedEventStore>();

        var failedEvent = new FailedEventInfo
        {
            EventId = "event-003",
            EventType = "test.event",
            SerializedEventData = "{\"eventId\":\"event-003\",\"eventType\":\"test.event\"}",
            RetryCount = 3, // 已达到最大重试次数
            FailedAt = DateTime.UtcNow,
            NextRetryAt = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(1))
        };

        eventStoreMock
            .Setup(x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FailedEventInfo> { failedEvent });

        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _scopeFactory,
            eventStoreMock.Object);

        using var cts = new CancellationTokenSource(1000);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(1200);
        await service.StopAsync(CancellationToken.None);

        // Assert - 应该移除事件，因为已达到最大重试次数
        eventStoreMock.Verify(
            x => x.RemoveFailedEventAsync("event-003", It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidJsonData_ShouldUpdateRetryCount()
    {
        // Arrange
        var optionsMock = CreateWebhookOptionsMonitor(_options);
        var eventStoreMock = new Mock<IFailedEventStore>();

        var failedEvent = new FailedEventInfo
        {
            EventId = "event-004",
            EventType = "test.event",
            SerializedEventData = "invalid_json_data",
            RetryCount = 0,
            FailedAt = DateTime.UtcNow,
            NextRetryAt = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(1))
        };

        eventStoreMock
            .Setup(x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FailedEventInfo> { failedEvent });

        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _scopeFactory,
            eventStoreMock.Object);

        using var cts = new CancellationTokenSource(1000);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(1200);
        await service.StopAsync(CancellationToken.None);

        // Assert - 无效 JSON 会抛出异常，进入 catch 块，更新重试次数
        eventStoreMock.Verify(
            x => x.UpdateFailedEventAsync(It.Is<FailedEventInfo>(e => e.EventId == "event-004" && e.RetryCount > 0), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WithException_ShouldUpdateRetryCount()
    {
        // Arrange
        var optionsMock = CreateWebhookOptionsMonitor(_options);
        var eventStoreMock = new Mock<IFailedEventStore>();

        var failedEvent = new FailedEventInfo
        {
            EventId = "event-005",
            EventType = "test.event",
            SerializedEventData = "{\"eventId\":\"event-005\",\"eventType\":\"test.event\"}",
            RetryCount = 0,
            FailedAt = DateTime.UtcNow,
            NextRetryAt = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(1))
        };

        eventStoreMock
            .Setup(x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FailedEventInfo> { failedEvent });

        _webhookServiceMock
            .Setup(x => x.HandleEventAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("测试异常"));

        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _scopeFactory,
            eventStoreMock.Object);

        using var cts = new CancellationTokenSource(1000);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(1200);
        await service.StopAsync(CancellationToken.None);

        // Assert - 应该更新重试次数
        eventStoreMock.Verify(
            x => x.UpdateFailedEventAsync(It.Is<FailedEventInfo>(e => e.RetryCount > 0), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPreserveHeader_AfterRetryRoundTrip()
    {
        // Arrange - P2-8/M3-3：失败事件重放必须保留 v2.0 Header（schema/app_id/tenant_key 等），
        // 否则重试路径下的行为与首次投递不一致（如 IdempotentFeishuEventHandler<T,THeader>
        // 的强类型 Header 注入、依赖 header.app_id 做的多租户判断会静默退化）。
        var optionsMock = CreateWebhookOptionsMonitor(_options);
        var eventStoreMock = new Mock<IFailedEventStore>();

        var header = new Mud.Feishu.Abstractions.FeishuEventHeader
        {
            Schema = "2.0",
            EventId = "event-hdr",
            EventType = "test.event",
            AppId = "cli_roundtrip",
            TenantKey = "tk_roundtrip"
        };

        var failedEvent = new FailedEventInfo
        {
            EventId = "event-hdr",
            EventType = "test.event",
            // 与 InMemoryFailedEventStore 同一路径序列化（反序列化后 Header 为 null，
            // 才能触发"从 SerializedHeader 回填"的分支）
            SerializedEventData = Mud.Feishu.Abstractions.Utilities.FeishuJsonAot.Serialize(
                new EventData { EventId = "event-hdr", EventType = "test.event" },
                Mud.Feishu.Abstractions.Utilities.FeishuJsonDefaults.SerializerOptions),
            SerializedHeader = Mud.Feishu.Abstractions.Utilities.FeishuJsonAot.Serialize(
                header, Mud.Feishu.Abstractions.Utilities.FeishuJsonDefaults.SerializerOptions),
            RetryCount = 0,
            FailedAt = DateTime.UtcNow,
            NextRetryAt = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(1))
        };

        eventStoreMock
            .Setup(x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FailedEventInfo> { failedEvent });

        EventData? replayed = null;
        _webhookServiceMock
            .Setup(x => x.HandleEventAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Callback<EventData, CancellationToken>((e, _) => replayed = e)
            .ReturnsAsync((true, (string?)null));

        var service = new TestableRetryService(
            optionsMock,
            _loggerMock.Object,
            _scopeFactory,
            eventStoreMock.Object);

        using var cts = new CancellationTokenSource(1500);

        // Act
        var executeTask = service.ExecuteCoreForTest(cts.Token);
        await Record.ExceptionAsync(() => executeTask);

        // Assert
        replayed.Should().NotBeNull("重试轮询应至少实际调用一次事件处理");
        replayed!.Header.Should().NotBeNull("P2-8：重放必须回填序列化时保存的 Header");
        replayed.Header!.Schema.Should().Be("2.0");
        replayed.Header.AppId.Should().Be("cli_roundtrip");
        replayed.Header.TenantKey.Should().Be("tk_roundtrip");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotBlockRetry_WhenSerializedHeaderIsCorrupted()
    {
        // Arrange - P2-8 健壮性：Header 反序列化失败只告警、不得阻断重试（Header 置空继续处理）
        var optionsMock = CreateWebhookOptionsMonitor(_options);
        var eventStoreMock = new Mock<IFailedEventStore>();

        var failedEvent = new FailedEventInfo
        {
            EventId = "event-bad-hdr",
            EventType = "test.event",
            SerializedEventData = Mud.Feishu.Abstractions.Utilities.FeishuJsonAot.Serialize(
                new EventData { EventId = "event-bad-hdr", EventType = "test.event" },
                Mud.Feishu.Abstractions.Utilities.FeishuJsonDefaults.SerializerOptions),
            SerializedHeader = "{ this is not valid json",
            RetryCount = 0,
            FailedAt = DateTime.UtcNow,
            NextRetryAt = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(1))
        };

        eventStoreMock
            .Setup(x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FailedEventInfo> { failedEvent });

        EventData? replayed = null;
        _webhookServiceMock
            .Setup(x => x.HandleEventAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Callback<EventData, CancellationToken>((e, _) => replayed = e)
            .ReturnsAsync((true, (string?)null));

        var service = new TestableRetryService(
            optionsMock,
            _loggerMock.Object,
            _scopeFactory,
            eventStoreMock.Object);

        using var cts = new CancellationTokenSource(1500);

        // Act
        var executeTask = service.ExecuteCoreForTest(cts.Token);
        await Record.ExceptionAsync(() => executeTask);

        // Assert
        replayed.Should().NotBeNull("损坏的 Header 不得阻断事件重试");
        replayed!.EventId.Should().Be("event-bad-hdr");
    }

    #region WHF-14：热更新与优雅关停

    /// <summary>
    /// 可变 IOptionsMonitor 桩：模拟 FeishuWebhookOptions.Retry.EnableRetry 的运行期热更新
    /// </summary>
    private sealed class MutableWebhookOptionsMonitor : IOptionsMonitor<FeishuWebhookOptions>
    {
        public FeishuWebhookOptions CurrentValue { get; set; } = new();

        public FeishuWebhookOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<FeishuWebhookOptions, string?> listener) => null;
    }

    /// <summary>
    /// 测试用子类：暴露受保护的 ExecuteAsync，绕过 BackgroundService.StartAsync 的
    /// Task.Run 排队竞态（stop 早于委托启动时任务合法地进入 Canceled 态且委托不运行——
    /// 框架语义，与被测的优雅退出逻辑无关）。
    /// </summary>
    private sealed class TestableRetryService : FailedEventRetryService
    {
        public TestableRetryService(
            IOptionsMonitor<FeishuWebhookOptions> webhookOptions,
            ILogger<FailedEventRetryService> logger,
            IServiceScopeFactory scopeFactory,
            IFailedEventStore? failedEventStore = null)
            : base(webhookOptions, logger, scopeFactory, failedEventStore)
        {
        }

        public Task ExecuteCoreForTest(CancellationToken stoppingToken) => ExecuteAsync(stoppingToken);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRetryEnabledAtRuntime_ShouldResumeProcessing()
    {
        // Arrange - WHF-14：启动期禁用 → 运行期启用 → 重试服务应恢复工作（不再永久退出）
        var webhookOptions = new FeishuWebhookOptions
        {
            // R5/X3：重试参数唯一真相源是 FeishuWebhookOptions.Retry——本用例同时验证
            // FeishuWebhook:Retry:RetryPollIntervalSeconds 等字段现在真的能驱动轮询节奏。
            Retry = new FailedEventRetryOptions
            {
                EnableRetry = false,
                MaxRetryCount = _options.MaxRetryCount,
                MaxRetryPerPoll = _options.MaxRetryPerPoll,
                RetryPollIntervalSeconds = 1
            }
        };
        var monitor = new MutableWebhookOptionsMonitor { CurrentValue = webhookOptions };

        var eventStoreMock = new Mock<IFailedEventStore>();
        eventStoreMock
            .Setup(x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FailedEventInfo>());

        var service = new TestableRetryService(
            monitor,
            _loggerMock.Object,
            _scopeFactory,
            eventStoreMock.Object);

        // Act - 直接驱动 ExecuteAsync：禁用态运行一轮（1s 轮询间隔），300ms 时热更新为启用，
        // 下一轮（约 1s 处）应恢复轮询；2.2s 处取消并验证优雅退出
        using var cts = new CancellationTokenSource(2200);
        var executeTask = service.ExecuteCoreForTest(cts.Token);
        await Task.Delay(300);
        monitor.CurrentValue.Retry.EnableRetry = true;
        var exception = await Record.ExceptionAsync(() => executeTask);

        // Assert - 禁用期间不轮询存储，启用后恢复轮询，且关停无 OCE 逃逸
        exception.Should().BeNull("WHF-14：关停时 OCE 应被捕获并优雅退出");
        eventStoreMock.Verify(
            x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "运行期启用后重试服务应恢复工作（WHF-14 热更新）");
    }

    [Fact]
    public async Task StopAsync_WhileRunning_ShouldExitGracefully_WithoutFaultedTask()
    {
        // Arrange - WHF-14：关停时 Task.Delay 的 OCE 应被捕获并优雅退出（不异常逃逸）
        var loggerMock = new Mock<ILogger<FailedEventRetryService>>();
        var eventStoreMock = new Mock<IFailedEventStore>();
        eventStoreMock
            .Setup(x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FailedEventInfo>());

        var service = new TestableRetryService(
            CreateWebhookOptionsMonitor(_options),
            loggerMock.Object,
            _scopeFactory,
            eventStoreMock.Object);

        // Act - 直接驱动 ExecuteAsync，300ms 后关停（远小于 1s 轮询间隔，取消落在 Delay 内）
        using var cts = new CancellationTokenSource(300);
        var executeTask = service.ExecuteCoreForTest(cts.Token);
        var exception = await Record.ExceptionAsync(() => executeTask);

        // Assert - ExecuteAsync 正常完成（OCE 被捕获、无逃逸），且走到退出日志
        exception.Should().BeNull("WHF-14：关停时 OCE 应被捕获并优雅退出，BackgroundService 不产生异常告警");
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("失败事件重试服务已停止")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "WHF-14：关停后应写入停止日志，证明 ExecuteAsync 走到了正常退出路径");
    }

    #endregion
}
