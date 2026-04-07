// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Tests.Services;

/// <summary>
/// FeishuEventDeduplicator 单元测试
/// </summary>
public class FeishuEventDeduplicatorTests
{
    [Fact]
    public void TryMarkAsProcessed_WhenFirstEvent_ShouldReturnFalse()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        var result = deduplicator.TryMarkAsProcessed(eventId);

        // Assert
        Assert.False(result); // false 表示未处理过（新事件）
    }

    [Fact]
    public void TryMarkAsProcessed_WhenDuplicateEvent_ShouldReturnTrue()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        deduplicator.TryMarkAsProcessed(eventId);
        var result = deduplicator.TryMarkAsProcessed(eventId);

        // Assert
        Assert.True(result); // true 表示已处理过（重复事件）
    }

    [Fact]
    public void TryMarkAsProcessing_WhenFirstEvent_ShouldReturnFalse()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        var result = deduplicator.TryMarkAsProcessing(eventId);

        // Assert
        Assert.False(result); // false 表示未处理过（新事件）
        Assert.Equal(DeduplicationStatus.Processing, deduplicator.GetStatus(eventId));
    }

    [Fact]
    public void TryMarkAsProcessing_WhenDuplicateEvent_ShouldReturnTrue()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        deduplicator.TryMarkAsProcessing(eventId);
        var result = deduplicator.TryMarkAsProcessing(eventId);

        // Assert
        Assert.True(result); // true 表示已在处理中（重复事件）
    }

    [Fact]
    public void MarkAsCompleted_ShouldUpdateStatusToCompleted()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        deduplicator.TryMarkAsProcessing(eventId);
        deduplicator.MarkAsCompleted(eventId);

        // Assert
        Assert.Equal(DeduplicationStatus.Completed, deduplicator.GetStatus(eventId));
        Assert.True(deduplicator.IsProcessed(eventId));
    }

    [Fact]
    public void RollbackProcessing_ShouldRemoveProcessingStatus()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        deduplicator.TryMarkAsProcessing(eventId);
        deduplicator.RollbackProcessing(eventId);

        // Assert
        Assert.Equal(DeduplicationStatus.Pending, deduplicator.GetStatus(eventId));
        Assert.False(deduplicator.IsProcessed(eventId));
    }

    [Fact]
    public void IsProcessed_WhenEventCompleted_ShouldReturnTrue()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        deduplicator.TryMarkAsProcessed(eventId);

        // Assert
        Assert.True(deduplicator.IsProcessed(eventId));
    }

    [Fact]
    public void IsProcessed_WhenEventNotExists_ShouldReturnFalse()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        var result = deduplicator.IsProcessed(eventId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetStatus_WhenEventNotExists_ShouldReturnPending()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        var result = deduplicator.GetStatus(eventId);

        // Assert
        Assert.Equal(DeduplicationStatus.Pending, result);
    }

    [Fact]
    public async Task TryMarkAsProcessing_WhenProcessingTimeout_ShouldAllowReprocessing()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(
            loggerMock.Object,
            processingTimeout: TimeSpan.FromMilliseconds(20));
        var eventId = "test_event_123";

        // Act
        deduplicator.TryMarkAsProcessing(eventId);
        await Task.Delay(30); // 等待超时
        var result = deduplicator.TryMarkAsProcessing(eventId);

        // Assert
        Assert.False(result); // 超时后应该允许重新处理
    }

    [Fact]
    public void GetCacheStats_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(loggerMock.Object);

        // Act
        deduplicator.TryMarkAsProcessed("event1");
        deduplicator.TryMarkAsProcessed("event2");
        deduplicator.TryMarkAsProcessed("event3");
        var (totalCached, expiredCount) = deduplicator.GetCacheStats();

        // Assert
        Assert.Equal(3, totalCached);
        Assert.Equal(0, expiredCount);
    }

    [Fact]
    public void ClearCache_ShouldRemoveAllEntries()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(loggerMock.Object);
        deduplicator.TryMarkAsProcessed("event1");
        deduplicator.TryMarkAsProcessed("event2");

        // Act
        deduplicator.ClearCache();
        var (totalCached, _) = deduplicator.GetCacheStats();

        // Assert
        Assert.Equal(0, totalCached);
    }

    [Fact]
    public void TryMarkAsProcessed_WhenNullOrEmptyEventId_ShouldReturnFalse()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(loggerMock.Object);

        // Act
        var result1 = deduplicator.TryMarkAsProcessed(null!);
        var result2 = deduplicator.TryMarkAsProcessed(string.Empty);
        var result3 = deduplicator.TryMarkAsProcessed("   ");

        // Assert
        Assert.False(result1);
        Assert.False(result2);
        Assert.False(result3);
    }

    [Fact]
    public async Task DisposeAsync_ShouldCleanupResources()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(loggerMock.Object);
        deduplicator.TryMarkAsProcessed("event1");

        // Act
        await deduplicator.DisposeAsync();
        var (totalCached, _) = deduplicator.GetCacheStats();

        // Assert
        Assert.Equal(0, totalCached);
    }

    [Fact]
    public void RollbackProcessing_WhenEventIsCompleted_ShouldNotRollback()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FeishuEventDeduplicator>>();
        var deduplicator = new FeishuEventDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        deduplicator.TryMarkAsProcessing(eventId);
        deduplicator.MarkAsCompleted(eventId);
        deduplicator.RollbackProcessing(eventId);

        // Assert
        Assert.Equal(DeduplicationStatus.Completed, deduplicator.GetStatus(eventId));
    }
}
