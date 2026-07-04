// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Tests.Services;

/// <summary>
/// NoopFeishuEventDeduplicator 单元测试。
/// 验证 Mode=None 时空实现的行为语义：所有方法均不执行实际去重，
/// TryMarkAsProcessingAsync 始终返回 Success，IsProcessedAsync 始终返回 false。
/// 对应生产代码：NoopFeishuEventDeduplicator.cs（CFG-P0-1/P0-2 修复）
/// </summary>
public class NoopFeishuEventDeduplicatorTests
{
    private readonly NoopFeishuEventDeduplicator _sut;
    private readonly Mock<ILogger> _loggerMock;

    public NoopFeishuEventDeduplicatorTests()
    {
        _loggerMock = new Mock<ILogger>();
        _sut = new NoopFeishuEventDeduplicator(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NoopFeishuEventDeduplicator(null!));
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_ShouldReturnSuccess_WhenCalled()
    {
        // Arrange
        var eventId = "test_event_123";

        // Act
        var result = await _sut.TryMarkAsProcessingAsync(eventId);

        // Assert - Success 语义：IsDuplicate=false, WasProcessing=false, Status=Processing
        Assert.NotNull(result);
        Assert.Equal(eventId, result.EventId);
        Assert.False(result.IsDuplicate);
        Assert.False(result.WasProcessing);
        Assert.Equal(DeduplicationStatus.Processing, result.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("app_key_1")]
    public async Task TryMarkAsProcessingAsync_ShouldReturnSuccess_RegardlessOfAppKey(string? appKey)
    {
        // Arrange
        var eventId = "test_event_123";

        // Act
        var result = await _sut.TryMarkAsProcessingAsync(eventId, appKey);

        // Assert
        Assert.False(result.IsDuplicate);
        Assert.Equal(eventId, result.EventId);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_ShouldReturnSuccess_EvenWhenCalledMultipleTimes()
    {
        // Arrange - 同一事件多次调用，Noop 不应记住状态，每次都返回 Success
        var eventId = "test_event_123";

        // Act
        var first = await _sut.TryMarkAsProcessingAsync(eventId);
        var second = await _sut.TryMarkAsProcessingAsync(eventId);
        var third = await _sut.TryMarkAsProcessingAsync(eventId);

        // Assert - 每次都允许处理（不视为重复）
        Assert.False(first.IsDuplicate);
        Assert.False(second.IsDuplicate);
        Assert.False(third.IsDuplicate);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_ShouldAcceptOptionalParameters()
    {
        // Arrange
        var eventId = "test_event_123";
        var ttl = TimeSpan.FromHours(1);
        var processingTimeout = TimeSpan.FromMinutes(10);
        using var cts = new CancellationTokenSource();

        // Act - 不应抛出异常
        var result = await _sut.TryMarkAsProcessingAsync(eventId, "app", ttl, processingTimeout, cts.Token);

        // Assert
        Assert.False(result.IsDuplicate);
    }

    [Fact]
    public async Task MarkAsCompletedAsync_ShouldNotThrow()
    {
        // Arrange
        var eventId = "test_event_123";

        // Act & Assert - Noop 实现，不应抛出异常
        await _sut.MarkAsCompletedAsync(eventId);
    }

    [Fact]
    public async Task MarkAsCompletedAsync_ShouldNotThrow_WhenEventNeverMarked()
    {
        // Arrange - 没有先调用 TryMarkAsProcessingAsync，直接完成
        var eventId = "test_event_123";

        // Act & Assert - Noop 不依赖前置状态
        await _sut.MarkAsCompletedAsync(eventId);
    }

    [Fact]
    public async Task RollbackProcessingAsync_ShouldNotThrow()
    {
        // Arrange
        var eventId = "test_event_123";

        // Act & Assert
        await _sut.RollbackProcessingAsync(eventId);
    }

    [Fact]
    public async Task RollbackProcessingAsync_ShouldNotThrow_WhenEventNeverMarked()
    {
        // Arrange - 没有先调用 TryMarkAsProcessingAsync，直接回滚
        var eventId = "test_event_123";

        // Act & Assert - Noop 不依赖前置状态
        await _sut.RollbackProcessingAsync(eventId);
    }

    [Fact]
    public async Task IsProcessedAsync_ShouldAlwaysReturnFalse()
    {
        // Arrange
        var eventId = "test_event_123";

        // Act - 即使先标记为处理中，IsProcessed 也应返回 false
        await _sut.TryMarkAsProcessingAsync(eventId);
        var result = await _sut.IsProcessedAsync(eventId);

        // Assert - Noop 不保留状态，所有事件均视为未处理
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("app_key_1")]
    public async Task IsProcessedAsync_ShouldReturnFalse_RegardlessOfAppKey(string? appKey)
    {
        // Arrange
        var eventId = "test_event_123";

        // Act
        var result = await _sut.IsProcessedAsync(eventId, appKey);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetStatusAsync_ShouldAlwaysReturnPending()
    {
        // Arrange
        var eventId = "test_event_123";

        // Act - 即使先标记为处理中，GetStatus 也应返回 Pending
        await _sut.TryMarkAsProcessingAsync(eventId);
        var status = await _sut.GetStatusAsync(eventId);

        // Assert - Noop 不保留状态，所有事件均视为未开始处理
        Assert.Equal(DeduplicationStatus.Pending, status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("app_key_1")]
    public async Task GetStatusAsync_ShouldReturnPending_RegardlessOfAppKey(string? appKey)
    {
        // Arrange
        var eventId = "test_event_123";

        // Act
        var status = await _sut.GetStatusAsync(eventId, appKey);

        // Assert
        Assert.Equal(DeduplicationStatus.Pending, status);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldReturnZero()
    {
        // Arrange - Noop 无缓存，无需清理

        // Act
        var count = await _sut.CleanupExpiredAsync();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldAcceptCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var count = await _sut.CleanupExpiredAsync(cts.Token);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task DisposeAsync_ShouldCompleteWithoutThrowing()
    {
        // Arrange
        var noop = new NoopFeishuEventDeduplicator(_loggerMock.Object);

        // Act & Assert - 无资源需要释放
        await noop.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ShouldBeIdempotent()
    {
        // Arrange
        var noop = new NoopFeishuEventDeduplicator(_loggerMock.Object);

        // Act & Assert - 多次释放不应抛出异常
        await noop.DisposeAsync();
        await noop.DisposeAsync();
        await noop.DisposeAsync();
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_ShouldLogDebugMessage()
    {
        // Arrange
        var eventId = "test_event_with_logging";
        var loggerMock = new Mock<ILogger>();
        loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var noop = new NoopFeishuEventDeduplicator(loggerMock.Object);

        // Act
        await noop.TryMarkAsProcessingAsync(eventId);

        // Assert - 应记录 Debug 级别日志，便于运维观测
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(eventId)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task FullLifecycle_ShouldNeverMarkEventAsDuplicate()
    {
        // Arrange - 模拟完整事件处理生命周期：标记处理中 -> 完成 -> 检查状态
        // Noop 实现应保证整个生命周期内不会误判为重复
        var eventId = "lifecycle_test_event";

        // Act
        var markResult = await _sut.TryMarkAsProcessingAsync(eventId);
        await _sut.MarkAsCompletedAsync(eventId);
        var isProcessedAfterComplete = await _sut.IsProcessedAsync(eventId);
        var statusAfterComplete = await _sut.GetStatusAsync(eventId);

        // 再次处理同一事件 - 应允许
        var reprocessResult = await _sut.TryMarkAsProcessingAsync(eventId);

        // Assert
        Assert.False(markResult.IsDuplicate);
        Assert.False(isProcessedAfterComplete);
        Assert.Equal(DeduplicationStatus.Pending, statusAfterComplete);
        Assert.False(reprocessResult.IsDuplicate);
    }
}
