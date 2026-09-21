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

    #region WHF-15：快照语义（深拷贝，消除共享可变引用）

    [Fact]
    public async Task GetPendingRetryEventsAsync_ShouldReturnSnapshot_MutationDoesNotAffectStore()
    {
        // Arrange
        var eventData = CreateEventData("snapshot-001", 0);
        await _store.StoreFailedEventAsync(eventData, new Exception("Error"));

        // Act - 取出快照并修改
        var snapshot = (await _store.GetPendingRetryEventsAsync(DateTimeOffset.UtcNow, 10)).First();
        snapshot.RetryCount = 99;
        snapshot.ExceptionMessage = "mutated";

        // Assert - 存储内条目不受影响（须通过 UpdateFailedEventAsync 写回）
        var fresh = (await _store.GetPendingRetryEventsAsync(DateTimeOffset.UtcNow, 10)).First();
        fresh.RetryCount.Should().Be(0, "Get 返回的是深拷贝快照，调用方修改不得影响存储");
        fresh.ExceptionMessage.Should().NotBe("mutated");
    }

    [Fact]
    public async Task GetFailedEventsForRetryAsync_ShouldReturnIndependentSnapshots_AcrossCalls()
    {
        // Arrange
        var eventData = CreateEventData("snapshot-002", 0);
        await _store.StoreFailedEventAsync(eventData, new Exception("Error"));

        // Act
        var first = (await _store.GetFailedEventsForRetryAsync(5)).First();
        first.RetryCount = 42;
        var second = (await _store.GetFailedEventsForRetryAsync(5)).First();

        // Assert - 多次 Get 之间互不可见
        second.RetryCount.Should().Be(0, "两次 Get 之间互不可见（WHF-15 快照语义）");
    }

    [Fact]
    public async Task UpdateFailedEventAsync_ShouldWriteBack_ThroughSnapshot()
    {
        // Arrange - 快照修改后经 UpdateFailedEventAsync 写回是唯一合法变更路径
        var eventData = CreateEventData("snapshot-003", 0);
        await _store.StoreFailedEventAsync(eventData, new Exception("Error"));

        var snapshot = (await _store.GetPendingRetryEventsAsync(DateTimeOffset.UtcNow, 10)).First();
        snapshot.RetryCount = 3;
        snapshot.NextRetryAt = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        await _store.UpdateFailedEventAsync(snapshot);

        // Assert
        var fresh = (await _store.GetFailedEventsForRetryAsync(5)).First();
        fresh.RetryCount.Should().Be(3);
        fresh.NextRetryAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(5));
    }

    #endregion

    // ===== M3-3 / M3-4 =====

    [Fact]
    public async Task StoreFailedEventAsync_ShouldPreserveHeader_WhenHeaderPresent()
    {
        var header = new Mud.Feishu.Abstractions.FeishuEventHeader
        {
            Schema = "2.0",
            EventId = "evt-header",
            EventType = "drive.file.edit_v1",
            AppId = "cli_app",
            TenantKey = "tk"
        };
        var eventData = new Mud.Feishu.Abstractions.EventData
        {
            EventId = "evt-header",
            EventType = "drive.file.edit_v1",
            Header = header
        };

        await _store.StoreFailedEventAsync(eventData, new Exception("err"), appKey: "app1", DateTimeOffset.UtcNow);

        var stored = (await _store.GetFailedEventsForRetryAsync(5)).Single();
        stored.SerializedHeader.Should().NotBeNullOrEmpty();
        stored.SerializedHeader.Should().Contain("cli_app");
        stored.StoreKey.Should().Be("evt-header");
    }

    [Fact]
    public async Task StoreFailedEventAsync_ShouldNotCollapseEvents_WhenEventIdEmpty()
    {
        var e1 = new Mud.Feishu.Abstractions.EventData { EventId = "", EventType = "t.a" };
        var e2 = new Mud.Feishu.Abstractions.EventData { EventId = "", EventType = "t.b" };

        await _store.StoreFailedEventAsync(e1, new Exception("1"));
        await _store.StoreFailedEventAsync(e2, new Exception("2"));

        var all = (await _store.GetFailedEventsForRetryAsync(5)).ToList();
        all.Count.Should().Be(2, "空 EventId 必须用 StoreKey 兜底，不得相互覆盖");
        all.Select(x => x.StoreKey).Should().OnlyHaveUniqueItems();
        all.All(x => x.StoreKey != null && x.StoreKey.StartsWith("no-event-id:")).Should().BeTrue();
    }
}
