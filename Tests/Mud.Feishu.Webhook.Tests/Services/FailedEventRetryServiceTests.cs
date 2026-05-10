// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会顺序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
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

    public FailedEventRetryServiceTests()
    {
        _loggerMock = new Mock<ILogger<FailedEventRetryService>>();
        _webhookServiceMock = new Mock<IFeishuWebhookService>();
        _options = new FailedEventRetryOptions
        {
            EnableRetry = true,
            MaxRetryCount = 3,
            MaxRetryPerPoll = 10,
            RetryPollIntervalSeconds = 1
        };
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var optionsMock = Options.Create(_options);

        // Act
        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _webhookServiceMock.Object);

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
        var optionsMock = Options.Create(options);

        // Act
        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _webhookServiceMock.Object,
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
        var optionsMock = Options.Create(options);

        // Act
        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _webhookServiceMock.Object,
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
        var optionsMock = Options.Create(_options);

        // Act
        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _webhookServiceMock.Object,
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
        var optionsMock = Options.Create(options);
        var eventStoreMock = new Mock<IFailedEventStore>();

        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _webhookServiceMock.Object,
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
        var optionsMock = Options.Create(_options);
        var eventStoreMock = new Mock<IFailedEventStore>();
        eventStoreMock
            .Setup(x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FailedEventInfo>());

        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _webhookServiceMock.Object,
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
        var optionsMock = Options.Create(_options);
        var eventStoreMock = new Mock<IFailedEventStore>();

        var failedEvent = new FailedEventInfo
        {
            EventId = "event-001",
            EventType = "test.event",
            SerializedEventData = "{\"eventId\":\"event-001\",\"eventType\":\"test.event\"}",
            RetryCount = 0,
            FailedAt = DateTime.UtcNow
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
            _webhookServiceMock.Object,
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
        var optionsMock = Options.Create(_options);
        var eventStoreMock = new Mock<IFailedEventStore>();

        var failedEvent = new FailedEventInfo
        {
            EventId = "event-002",
            EventType = "test.event",
            SerializedEventData = "{\"eventId\":\"event-002\",\"eventType\":\"test.event\"}",
            RetryCount = 0,
            FailedAt = DateTime.UtcNow
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
            _webhookServiceMock.Object,
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
        var optionsMock = Options.Create(_options);
        var eventStoreMock = new Mock<IFailedEventStore>();

        var failedEvent = new FailedEventInfo
        {
            EventId = "event-003",
            EventType = "test.event",
            SerializedEventData = "{\"eventId\":\"event-003\",\"eventType\":\"test.event\"}",
            RetryCount = 3, // 已达到最大重试次数
            FailedAt = DateTime.UtcNow
        };

        eventStoreMock
            .Setup(x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FailedEventInfo> { failedEvent });

        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _webhookServiceMock.Object,
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
        var optionsMock = Options.Create(_options);
        var eventStoreMock = new Mock<IFailedEventStore>();

        var failedEvent = new FailedEventInfo
        {
            EventId = "event-004",
            EventType = "test.event",
            SerializedEventData = "invalid_json_data",
            RetryCount = 0,
            FailedAt = DateTime.UtcNow
        };

        eventStoreMock
            .Setup(x => x.GetPendingRetryEventsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FailedEventInfo> { failedEvent });

        var service = new FailedEventRetryService(
            optionsMock,
            _loggerMock.Object,
            _webhookServiceMock.Object,
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
        var optionsMock = Options.Create(_options);
        var eventStoreMock = new Mock<IFailedEventStore>();

        var failedEvent = new FailedEventInfo
        {
            EventId = "event-005",
            EventType = "test.event",
            SerializedEventData = "{\"eventId\":\"event-005\",\"eventType\":\"test.event\"}",
            RetryCount = 0,
            FailedAt = DateTime.UtcNow
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
            _webhookServiceMock.Object,
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
}
