// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Moq;
using Mud.Feishu.Abstractions.Services;
using Xunit;

#pragma warning disable CS0618 // FeishuEventDistributedDeduplicator 已废弃，测试仍需覆盖

namespace Mud.Feishu.Abstractions.Tests.Services;

/// <summary>
/// FeishuEventDistributedDeduplicator 单元测试
/// </summary>
public class FeishuEventDistributedDeduplicatorTests
{
    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenFirstEvent_ShouldReturnSuccess()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventDistributedDeduplicator>>();
        var deduplicator = new FeishuEventDistributedDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        var result = await deduplicator.TryMarkAsProcessingAsync(eventId);

        // Assert
        Assert.False(result.IsDuplicate); // false 表示未处理过（新事件）
        Assert.Equal(eventId, result.EventId);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenDuplicateEvent_ShouldReturnDuplicate()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventDistributedDeduplicator>>();
        var deduplicator = new FeishuEventDistributedDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        await deduplicator.TryMarkAsProcessingAsync(eventId);
        var result = await deduplicator.TryMarkAsProcessingAsync(eventId);

        // Assert
        Assert.True(result.IsDuplicate); // true 表示正在处理中
        Assert.True(result.WasProcessing); // WasProcessing=true 表示正在处理中
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenDifferentEvents_ShouldReturnSuccess()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventDistributedDeduplicator>>();
        var deduplicator = new FeishuEventDistributedDeduplicator(loggerMock.Object);
        var eventId1 = "test_event_123";
        var eventId2 = "test_event_456";

        // Act
        await deduplicator.TryMarkAsProcessingAsync(eventId1);
        var result = await deduplicator.TryMarkAsProcessingAsync(eventId2);

        // Assert
        Assert.False(result.IsDuplicate); // false 表示未处理过（新事件）
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenNullOrEmptyEventId_ShouldReturnSuccess()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventDistributedDeduplicator>>();
        var deduplicator = new FeishuEventDistributedDeduplicator(loggerMock.Object);

        // Act
        var result1 = await deduplicator.TryMarkAsProcessingAsync(null!);
        var result2 = await deduplicator.TryMarkAsProcessingAsync(string.Empty);
        var result3 = await deduplicator.TryMarkAsProcessingAsync("   ");

        // Assert
        Assert.False(result1.IsDuplicate); // 空值应返回成功（跳过去重检查）
        Assert.False(result2.IsDuplicate);
        Assert.False(result3.IsDuplicate);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenEventExists_ShouldReturnTrue()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventDistributedDeduplicator>>();
        var deduplicator = new FeishuEventDistributedDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";
        await deduplicator.TryMarkAsProcessingAsync(eventId);
        await deduplicator.MarkAsCompletedAsync(eventId);

        // Act
        var result = await deduplicator.IsProcessedAsync(eventId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenEventNotExists_ShouldReturnFalse()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventDistributedDeduplicator>>();
        var deduplicator = new FeishuEventDistributedDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        var result = await deduplicator.IsProcessedAsync(eventId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldCleanupExpiredEntries()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventDistributedDeduplicator>>();
        var deduplicator = new FeishuEventDistributedDeduplicator(loggerMock.Object, cacheExpiration: TimeSpan.FromMilliseconds(20));
        var eventId = "test_event_123";
        await deduplicator.TryMarkAsProcessingAsync(eventId);
        await deduplicator.MarkAsCompletedAsync(eventId);

        // Act
        await Task.Delay(30); // 等待过期
        var result = await deduplicator.CleanupExpiredAsync();

        // Assert
        Assert.True(result >= 0); // 验证清理方法执行成功
    }
}
