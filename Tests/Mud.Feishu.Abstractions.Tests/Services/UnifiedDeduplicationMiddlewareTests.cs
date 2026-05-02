// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Configuration;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Services;

public class UnifiedDeduplicationMiddlewareTests
{
#pragma warning disable CS0618 // IFeishuEventDistributedDeduplicator 已废弃，测试仍需覆盖
    private readonly Mock<IFeishuEventDistributedDeduplicator> _eventDeduplicatorMock;
#pragma warning restore CS0618 // IFeishuEventDistributedDeduplicator 已废弃，测试仍需覆盖
    private readonly Mock<IFeishuSeqIDDeduplicator> _seqIdDeduplicatorMock;
    private readonly Mock<ILogger<UnifiedDeduplicationMiddleware>> _loggerMock;
    private readonly DeduplicationOptions _options;

    public UnifiedDeduplicationMiddlewareTests()
    {
#pragma warning disable CS0618 // IFeishuEventDistributedDeduplicator 已废弃，测试仍需覆盖
        _eventDeduplicatorMock = new Mock<IFeishuEventDistributedDeduplicator>();
#pragma warning restore CS0618 // IFeishuEventDistributedDeduplicator 已废弃，测试仍需覆盖
        _seqIdDeduplicatorMock = new Mock<IFeishuSeqIDDeduplicator>();
        _loggerMock = new Mock<ILogger<UnifiedDeduplicationMiddleware>>();
        _options = new DeduplicationOptions();
    }

    [Fact]
    public async Task CheckAsync_WithNoDeduplicators_ShouldReturnContinue()
    {
        var middleware = new UnifiedDeduplicationMiddleware(
            eventDeduplicator: null,
            seqIdDeduplicator: null,
            options: _options,
            logger: _loggerMock.Object);

        var result = await middleware.CheckAsync("event-123", 100);

        Assert.False(result.ShouldSkip);
        Assert.Equal(DeduplicationIdentifierType.None, result.IdentifierType);
    }

    [Fact]
    public async Task CheckAsync_WhenSeqIdAlreadyProcessed_ShouldReturnSkip()
    {
        _seqIdDeduplicatorMock
            .Setup(x => x.IsProcessedAsync(100))
            .ReturnsAsync(true);

        var middleware = new UnifiedDeduplicationMiddleware(
            eventDeduplicator: _eventDeduplicatorMock.Object,
            seqIdDeduplicator: _seqIdDeduplicatorMock.Object,
            options: _options,
            logger: _loggerMock.Object);

        var result = await middleware.CheckAsync("event-123", 100);

        Assert.True(result.ShouldSkip);
        Assert.Equal(DeduplicationIdentifierType.SeqId, result.IdentifierType);
        Assert.Contains("100", result.IdentifierValue);
    }

    [Fact]
    public async Task CheckAsync_WhenEventIdIsDuplicate_ShouldReturnSkip()
    {
        _seqIdDeduplicatorMock
            .Setup(x => x.IsProcessedAsync(It.IsAny<ulong>()))
            .ReturnsAsync(false);

        _eventDeduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync("event-123", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DeduplicationResult.Duplicate("event-123", false, DeduplicationStatus.Completed));

        var middleware = new UnifiedDeduplicationMiddleware(
            eventDeduplicator: _eventDeduplicatorMock.Object,
            seqIdDeduplicator: _seqIdDeduplicatorMock.Object,
            options: _options,
            logger: _loggerMock.Object);

        var result = await middleware.CheckAsync("event-123", 100);

        Assert.True(result.ShouldSkip);
        Assert.Equal(DeduplicationIdentifierType.EventId, result.IdentifierType);
        Assert.Equal("event-123", result.IdentifierValue);
    }

    [Fact]
    public async Task CheckAsync_WhenNotDuplicate_ShouldReturnContinueAndMarkSeqId()
    {
        _seqIdDeduplicatorMock
            .Setup(x => x.IsProcessedAsync(100))
            .ReturnsAsync(false);

        _seqIdDeduplicatorMock
            .Setup(x => x.TryMarkAsProcessedAsync(100))
            .ReturnsAsync(true);

        _eventDeduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync("event-123", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DeduplicationResult.Success("event-123"));

        var middleware = new UnifiedDeduplicationMiddleware(
            eventDeduplicator: _eventDeduplicatorMock.Object,
            seqIdDeduplicator: _seqIdDeduplicatorMock.Object,
            options: _options,
            logger: _loggerMock.Object);

        var result = await middleware.CheckAsync("event-123", 100);

        Assert.False(result.ShouldSkip);
        _seqIdDeduplicatorMock.Verify(x => x.TryMarkAsProcessedAsync(100), Times.Once);
    }

    [Fact]
    public async Task CheckAsync_WithOnlyEventId_ShouldCheckEventDeduplication()
    {
        _eventDeduplicatorMock
            .Setup(x => x.TryMarkAsProcessingAsync("event-123", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DeduplicationResult.Success("event-123"));

        var middleware = new UnifiedDeduplicationMiddleware(
            eventDeduplicator: _eventDeduplicatorMock.Object,
            seqIdDeduplicator: null,
            options: _options,
            logger: _loggerMock.Object);

        var result = await middleware.CheckAsync("event-123", null);

        Assert.False(result.ShouldSkip);
        _eventDeduplicatorMock.Verify(
            x => x.TryMarkAsProcessingAsync("event-123", null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckAsync_WithOnlySeqId_ShouldCheckSeqIdDeduplication()
    {
        _seqIdDeduplicatorMock
            .Setup(x => x.IsProcessedAsync(100))
            .ReturnsAsync(false);

        _seqIdDeduplicatorMock
            .Setup(x => x.TryMarkAsProcessedAsync(100))
            .ReturnsAsync(true);

        var middleware = new UnifiedDeduplicationMiddleware(
            eventDeduplicator: null,
            seqIdDeduplicator: _seqIdDeduplicatorMock.Object,
            options: _options,
            logger: _loggerMock.Object);

        var result = await middleware.CheckAsync(null, 100);

        Assert.False(result.ShouldSkip);
        _seqIdDeduplicatorMock.Verify(x => x.IsProcessedAsync(100), Times.Once);
        _seqIdDeduplicatorMock.Verify(x => x.TryMarkAsProcessedAsync(100), Times.Once);
    }

    [Fact]
    public async Task MarkCompletedAsync_ShouldCallEventDeduplicator()
    {
        _eventDeduplicatorMock
            .Setup(x => x.MarkAsCompletedAsync("event-123", null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var middleware = new UnifiedDeduplicationMiddleware(
            eventDeduplicator: _eventDeduplicatorMock.Object,
            seqIdDeduplicator: _seqIdDeduplicatorMock.Object,
            options: _options,
            logger: _loggerMock.Object);

        await middleware.MarkCompletedAsync("event-123", 100);

        _eventDeduplicatorMock.Verify(
            x => x.MarkAsCompletedAsync("event-123", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkCompletedAsync_WithNoEventDeduplicator_ShouldNotThrow()
    {
        var middleware = new UnifiedDeduplicationMiddleware(
            eventDeduplicator: null,
            seqIdDeduplicator: _seqIdDeduplicatorMock.Object,
            options: _options,
            logger: _loggerMock.Object);

        var exception = await Record.ExceptionAsync(() => middleware.MarkCompletedAsync("event-123", 100));

        Assert.Null(exception);
    }

    [Fact]
    public async Task RollbackAsync_ShouldCallEventDeduplicator()
    {
        _eventDeduplicatorMock
            .Setup(x => x.RollbackProcessingAsync("event-123", null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var middleware = new UnifiedDeduplicationMiddleware(
            eventDeduplicator: _eventDeduplicatorMock.Object,
            seqIdDeduplicator: _seqIdDeduplicatorMock.Object,
            options: _options,
            logger: _loggerMock.Object);

        await middleware.RollbackAsync("event-123", 100);

        _eventDeduplicatorMock.Verify(
            x => x.RollbackProcessingAsync("event-123", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_WithNoEventDeduplicator_ShouldNotThrow()
    {
        var middleware = new UnifiedDeduplicationMiddleware(
            eventDeduplicator: null,
            seqIdDeduplicator: _seqIdDeduplicatorMock.Object,
            options: _options,
            logger: _loggerMock.Object);

        var exception = await Record.ExceptionAsync(() => middleware.RollbackAsync("event-123", 100));

        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeDeduplicators()
    {
        _eventDeduplicatorMock
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        _seqIdDeduplicatorMock
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var middleware = new UnifiedDeduplicationMiddleware(
            eventDeduplicator: _eventDeduplicatorMock.Object,
            seqIdDeduplicator: _seqIdDeduplicatorMock.Object,
            options: _options,
            logger: _loggerMock.Object);

        await middleware.DisposeAsync();

        _eventDeduplicatorMock.Verify(x => x.DisposeAsync(), Times.Once);
        _seqIdDeduplicatorMock.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_ShouldNotThrow()
    {
        _eventDeduplicatorMock
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        _seqIdDeduplicatorMock
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var middleware = new UnifiedDeduplicationMiddleware(
            eventDeduplicator: _eventDeduplicatorMock.Object,
            seqIdDeduplicator: _seqIdDeduplicatorMock.Object,
            options: _options,
            logger: _loggerMock.Object);

        await middleware.DisposeAsync();
        await middleware.DisposeAsync();

        _eventDeduplicatorMock.Verify(x => x.DisposeAsync(), Times.Once);
        _seqIdDeduplicatorMock.Verify(x => x.DisposeAsync(), Times.Once);
    }
}
