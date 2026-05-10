// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mud.Feishu.Webhook.Tests.Services;

/// <summary>
/// InMemoryFailedEventStore 单元测试
/// </summary>
public class InMemoryFailedEventStoreTests
{
    private readonly Mock<ILogger<InMemoryFailedEventStore>> _loggerMock;
    private readonly InMemoryFailedEventStore _store;

    public InMemoryFailedEventStoreTests()
    {
        _loggerMock = new Mock<ILogger<InMemoryFailedEventStore>>();
        _store = new InMemoryFailedEventStore(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new InMemoryFailedEventStore(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task StoreFailedEventAsync_WithValidEvent_ShouldStoreSuccessfully()
    {
        // Arrange
        var eventData = new EventData
        {
            EventId = "event-001",
            EventType = "test.event",
            TenantKey = "test-tenant"
        };
        var exception = new InvalidOperationException("Test error");

        // Act
        await _store.StoreFailedEventAsync(eventData, exception);

        // Assert
        var count = _store.GetFailedEventCount();
        count.Should().Be(1);
    }

    [Fact]
    public async Task StoreFailedEventAsync_WithDuplicateEventId_ShouldUpdateExisting()
    {
        // Arrange
        var eventData = new EventData
        {
            EventId = "event-001",
            EventType = "test.event",
            TenantKey = "test-tenant"
        };
        var exception1 = new InvalidOperationException("First error");
        var exception2 = new ArgumentException("Second error");

        // Act
        await _store.StoreFailedEventAsync(eventData, exception1);
        await _store.StoreFailedEventAsync(eventData, exception2);

        // Assert
        var count = _store.GetFailedEventCount();
        count.Should().Be(1); // Should update, not add new
    }

    [Fact]
    public async Task GetFailedEventsForRetryAsync_WithEventsUnderLimit_ShouldReturnAll()
    {
        // Arrange
        var event1 = CreateEventData("event-001", 0);
        var event2 = CreateEventData("event-002", 1);
        await _store.StoreFailedEventAsync(event1, new Exception("Error 1"));
        await _store.StoreFailedEventAsync(event2, new Exception("Error 2"));

        // Act
        var result = await _store.GetFailedEventsForRetryAsync(5);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPendingRetryEventsAsync_WithTimeFilter_ShouldReturnExpiredEvents()
    {
        // Arrange
        var event1 = CreateEventData("event-001", 0);
        await _store.StoreFailedEventAsync(event1, new Exception("Error 1"));

        // Act
        var result = await _store.GetPendingRetryEventsAsync(DateTimeOffset.UtcNow.AddHours(1), 10);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateRetryCountAsync_WithExistingEvent_ShouldUpdateCount()
    {
        // Arrange
        var eventData = CreateEventData("event-001", 0);
        await _store.StoreFailedEventAsync(eventData, new Exception("Error"));

        // Act
        await _store.UpdateRetryCountAsync("event-001", 3);

        // Assert
        var result = await _store.GetFailedEventsForRetryAsync(5);
        result.First().RetryCount.Should().Be(3);
    }

    [Fact]
    public async Task UpdateRetryCountAsync_WithNonExistentEvent_ShouldDoNothing()
    {
        // Arrange
        var eventData = CreateEventData("event-001", 0);
        await _store.StoreFailedEventAsync(eventData, new Exception("Error"));

        // Act
        await _store.UpdateRetryCountAsync("non-existent", 5);

        // Assert - should not throw
        var count = _store.GetFailedEventCount();
        count.Should().Be(1);
    }

    [Fact]
    public async Task RemoveFailedEventAsync_WithExistingEvent_ShouldRemove()
    {
        // Arrange
        var eventData = CreateEventData("event-001", 0);
        await _store.StoreFailedEventAsync(eventData, new Exception("Error"));

        // Act
        await _store.RemoveFailedEventAsync("event-001");

        // Assert
        var count = _store.GetFailedEventCount();
        count.Should().Be(0);
    }

    [Fact]
    public async Task RemoveFailedEventAsync_WithNonExistentEvent_ShouldDoNothing()
    {
        // Arrange
        var eventData = CreateEventData("event-001", 0);
        await _store.StoreFailedEventAsync(eventData, new Exception("Error"));

        // Act
        await _store.RemoveFailedEventAsync("non-existent");

        // Assert - should not throw
        var count = _store.GetFailedEventCount();
        count.Should().Be(1);
    }

    [Fact]
    public async Task UpdateFailedEventAsync_WithExistingEvent_ShouldUpdate()
    {
        // Arrange
        var eventData = CreateEventData("event-001", 0);
        await _store.StoreFailedEventAsync(eventData, new Exception("Original Error"));

        var failedEvent = new FailedEventInfo
        {
            EventId = "event-001",
            RetryCount = 2,
            ExceptionMessage = "Updated Error",
            FailedAt = DateTime.UtcNow
        };

        // Act
        await _store.UpdateFailedEventAsync(failedEvent);

        // Assert
        var result = await _store.GetFailedEventsForRetryAsync(5);
        result.First().RetryCount.Should().Be(2);
    }

    private static EventData CreateEventData(string eventId, int retryCount)
    {
        return new EventData
        {
            EventId = eventId,
            EventType = "test.event",
            TenantKey = "test-tenant"
        };
    }
}
