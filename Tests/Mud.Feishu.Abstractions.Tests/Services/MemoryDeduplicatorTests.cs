// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

// 测试同步去重方法，这些方法已标记为 [Obsolete] 但测试仍需验证其功能
#pragma warning disable CS0618

namespace Mud.Feishu.Abstractions.Tests.Services;

/// <summary>
/// MemoryDeduplicator 基类单元测试
/// 测试泛型去重基类的核心功能，独立于 FeishuEventDeduplicator 的业务逻辑
/// </summary>
public class MemoryDeduplicatorTests
{
    private readonly Mock<ILogger<MemoryDeduplicator<string>>> _loggerMock;

    public MemoryDeduplicatorTests()
    {
        _loggerMock = new Mock<ILogger<MemoryDeduplicator<string>>>();
    }

    [Fact]
    public void TryMarkAsProcessed_WhenFirstKey_ShouldReturnFalse()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);
        var key = "key_001";

        // Act
        var result = deduplicator.TryMarkAsProcessed(key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryMarkAsProcessed_WhenDuplicateKey_ShouldReturnTrue()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);
        var key = "key_001";

        // Act
        deduplicator.TryMarkAsProcessed(key);
        var result = deduplicator.TryMarkAsProcessed(key);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryMarkAsProcessing_WhenFirstKey_ShouldReturnFalseAndSetStatusProcessing()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);
        var key = "key_001";

        // Act
        var result = deduplicator.TryMarkAsProcessing(key);

        // Assert
        Assert.False(result);
        Assert.Equal(DeduplicationStatus.Processing, deduplicator.GetStatus(key));
    }

    [Fact]
    public void TryMarkAsProcessing_WhenKeyAlreadyProcessing_ShouldReturnTrue()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);
        var key = "key_001";

        // Act
        deduplicator.TryMarkAsProcessing(key);
        var result = deduplicator.TryMarkAsProcessing(key);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryMarkAsProcessing_WhenKeyCompleted_ShouldReturnTrue()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);
        var key = "key_001";

        // Act
        deduplicator.TryMarkAsProcessing(key);
        deduplicator.MarkAsCompleted(key);
        var result = deduplicator.TryMarkAsProcessing(key);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TryMarkAsProcessing_WhenProcessingTimeout_ShouldAllowReprocessing()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(
            _loggerMock.Object,
            processingTimeout: TimeSpan.FromMilliseconds(20));
        var key = "key_timeout";

        // Act
        deduplicator.TryMarkAsProcessing(key);
        await Task.Delay(30);
        var result = deduplicator.TryMarkAsProcessing(key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void MarkAsCompleted_ShouldUpdateStatusToCompleted()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);
        var key = "key_001";

        // Act
        deduplicator.TryMarkAsProcessing(key);
        deduplicator.MarkAsCompleted(key);

        // Assert
        Assert.Equal(DeduplicationStatus.Completed, deduplicator.GetStatus(key));
        Assert.True(deduplicator.IsProcessed(key));
    }

    [Fact]
    public void MarkAsCompleted_WhenKeyNotExists_ShouldNotThrow()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);

        // Act & Assert
        deduplicator.MarkAsCompleted("nonexistent_key");
    }

    [Fact]
    public void RollbackProcessing_ShouldRemoveProcessingStatus()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);
        var key = "key_001";

        // Act
        deduplicator.TryMarkAsProcessing(key);
        deduplicator.RollbackProcessing(key);

        // Assert
        Assert.Equal(DeduplicationStatus.Pending, deduplicator.GetStatus(key));
        Assert.False(deduplicator.IsProcessed(key));
    }

    [Fact]
    public void RollbackProcessing_WhenKeyCompleted_ShouldNotRollback()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);
        var key = "key_001";

        // Act
        deduplicator.TryMarkAsProcessing(key);
        deduplicator.MarkAsCompleted(key);
        deduplicator.RollbackProcessing(key);

        // Assert
        Assert.Equal(DeduplicationStatus.Completed, deduplicator.GetStatus(key));
    }

    [Fact]
    public void IsProcessed_WhenKeyNotExists_ShouldReturnFalse()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);

        // Act
        var result = deduplicator.IsProcessed("nonexistent_key");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsProcessed_WhenKeyProcessing_ShouldReturnFalse()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);
        var key = "key_001";

        // Act
        deduplicator.TryMarkAsProcessing(key);
        var result = deduplicator.IsProcessed(key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetStatus_WhenKeyNotExists_ShouldReturnPending()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);

        // Act
        var result = deduplicator.GetStatus("nonexistent_key");

        // Assert
        Assert.Equal(DeduplicationStatus.Pending, result);
    }

    [Fact]
    public void ClearCache_ShouldRemoveAllEntries()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);
        deduplicator.TryMarkAsProcessed("key1");
        deduplicator.TryMarkAsProcessed("key2");
        deduplicator.TryMarkAsProcessed("key3");

        // Act
        deduplicator.ClearCache();

        // Assert
        Assert.Equal(0, deduplicator.Count);
    }

    [Fact]
    public void GetCacheStats_ShouldReturnCorrectCounts()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);
        deduplicator.TryMarkAsProcessed("key1");
        deduplicator.TryMarkAsProcessed("key2");
        deduplicator.TryMarkAsProcessed("key3");

        // Act
        var (total, expired) = deduplicator.GetCacheStats();

        // Assert
        Assert.Equal(3, total);
        Assert.Equal(0, expired);
    }

    [Fact]
    public async Task DisposeAsync_ShouldCleanupResources()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);
        deduplicator.TryMarkAsProcessed("key1");

        // Act
        await deduplicator.DisposeAsync();

        // Assert
        Assert.Equal(0, deduplicator.Count);
    }

    [Fact]
    public void WithAppKey_ShouldIsolateEntries()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);
        var key = "shared_key";
        var appKey1 = "app-001";
        var appKey2 = "app-002";

        // Act
        var result1 = deduplicator.TryMarkAsProcessed(key, appKey1);
        var result2 = deduplicator.TryMarkAsProcessed(key, appKey2);
        var result1Again = deduplicator.TryMarkAsProcessed(key, appKey1);

        // Assert
        Assert.False(result1);
        Assert.False(result2);
        Assert.True(result1Again);
    }

    [Fact]
    public void MaxCacheSize_ShouldEvictOldestWhenFull()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(
            _loggerMock.Object,
            maxCacheSize: 3);

        // Act
        deduplicator.TryMarkAsProcessed("key1");
        deduplicator.TryMarkAsProcessed("key2");
        deduplicator.TryMarkAsProcessed("key3");
        deduplicator.TryMarkAsProcessed("key4");

        // Assert - key1 should have been evicted
        Assert.False(deduplicator.IsProcessed("key1"));
        Assert.True(deduplicator.IsProcessed("key4"));
    }

    [Fact]
    public void CleanupExpired_ShouldRemoveExpiredEntries()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(
            _loggerMock.Object,
            cacheExpiration: TimeSpan.FromMilliseconds(20));
        deduplicator.TryMarkAsProcessed("key1");

        // Act - wait for expiration
        Thread.Sleep(30);
        var removed = deduplicator.CleanupExpired();

        // Assert
        Assert.Equal(1, removed);
        Assert.Equal(0, deduplicator.Count);
    }

    [Fact]
    public void TryMarkAsProcessed_WithNullKey_ShouldReturnFalse()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);

        // Act
        var result = deduplicator.TryMarkAsProcessed(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Count_ShouldReturnCurrentCacheSize()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<string>(_loggerMock.Object);

        // Act
        deduplicator.TryMarkAsProcessed("key1");
        deduplicator.TryMarkAsProcessed("key2");

        // Assert
        Assert.Equal(2, deduplicator.Count);
    }
}

/// <summary>
/// MemoryDeduplicator 泛型测试 - 使用 ulong 键类型
/// 验证基类对不同键类型的支持
/// </summary>
public class MemoryDeduplicatorULongTests
{
    private readonly Mock<ILogger<MemoryDeduplicator<ulong>>> _loggerMock;

    public MemoryDeduplicatorULongTests()
    {
        _loggerMock = new Mock<ILogger<MemoryDeduplicator<ulong>>>();
    }

    [Fact]
    public void TryMarkAsProcessed_WhenFirstKey_ShouldReturnFalse()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<ulong>(_loggerMock.Object);

        // Act
        var result = deduplicator.TryMarkAsProcessed(12345UL);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryMarkAsProcessed_WhenDuplicateKey_ShouldReturnTrue()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<ulong>(_loggerMock.Object);

        // Act
        deduplicator.TryMarkAsProcessed(12345UL);
        var result = deduplicator.TryMarkAsProcessed(12345UL);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryMarkAsProcessing_WhenFirstKey_ShouldReturnFalse()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<ulong>(_loggerMock.Object);

        // Act
        var result = deduplicator.TryMarkAsProcessing(67890UL);

        // Assert
        Assert.False(result);
        Assert.Equal(DeduplicationStatus.Processing, deduplicator.GetStatus(67890UL));
    }

    [Fact]
    public void WithAppKey_ShouldNotAffectValueTypeKeys()
    {
        // Arrange - ulong 是值类型，appKey 不会影响缓存键
        var deduplicator = new MemoryDeduplicator<ulong>(_loggerMock.Object);

        // Act
        var result1 = deduplicator.TryMarkAsProcessed(999UL, "app1");
        var result2 = deduplicator.TryMarkAsProcessed(999UL, "app2");

        // Assert - 值类型键不受 appKey 影响，app2 应该检测到重复
        Assert.False(result1);
        Assert.True(result2);
    }

    [Fact]
    public void MarkAsCompleted_ShouldUpdateStatus()
    {
        // Arrange
        var deduplicator = new MemoryDeduplicator<ulong>(_loggerMock.Object);

        // Act
        deduplicator.TryMarkAsProcessing(111UL);
        deduplicator.MarkAsCompleted(111UL);

        // Assert
        Assert.Equal(DeduplicationStatus.Completed, deduplicator.GetStatus(111UL));
        Assert.True(deduplicator.IsProcessed(111UL));
    }
}
