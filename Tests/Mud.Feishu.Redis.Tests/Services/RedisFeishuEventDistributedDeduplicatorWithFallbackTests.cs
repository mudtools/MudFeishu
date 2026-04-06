// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Redis.Services;

namespace Mud.Feishu.Redis.Tests.Services;

/// <summary>
/// RedisFeishuEventDistributedDeduplicatorWithFallback 单元测试
/// </summary>
public class RedisFeishuEventDistributedDeduplicatorWithFallbackTests
{
    private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<ILogger<RedisFeishuEventDistributedDeduplicatorWithFallback>> _loggerMock;

    public RedisFeishuEventDistributedDeduplicatorWithFallbackTests()
    {
        _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _loggerMock = new Mock<ILogger<RedisFeishuEventDistributedDeduplicatorWithFallback>>();

        _connectionMultiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenRedisAvailable_ShouldUseRedis()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.HashGetAllAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(Array.Empty<HashEntry>());

        var deduplicator = new RedisFeishuEventDistributedDeduplicatorWithFallback(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsProcessedAsync("test_event_123");

        // Assert
        Assert.False(result);
        Assert.True(deduplicator.IsUsingRedis);
        Assert.Equal(0, deduplicator.ConsecutiveFailures);
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenRedisFails_ShouldFallbackToMemory()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.HashGetAllAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var deduplicator = new RedisFeishuEventDistributedDeduplicatorWithFallback(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object,
            maxRetryCount: 1);

        // Act
        var result = await deduplicator.TryMarkAsProcessedAsync("test_event_123");

        // Assert
        Assert.False(result); // 降级到内存，新事件
        Assert.Equal(1, deduplicator.ConsecutiveFailures);
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenConsecutiveFailures_ShouldMarkRedisUnavailable()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.HashGetAllAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var deduplicator = new RedisFeishuEventDistributedDeduplicatorWithFallback(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object,
            maxRetryCount: 1);

        // Act - 触发 3 次失败
        await deduplicator.TryMarkAsProcessedAsync("test_event_1");
        await deduplicator.TryMarkAsProcessedAsync("test_event_2");
        await deduplicator.TryMarkAsProcessedAsync("test_event_3");

        // Assert
        Assert.False(deduplicator.IsUsingRedis);
        Assert.Equal(3, deduplicator.ConsecutiveFailures);
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenRedisRecovered_ShouldResetFailureCount()
    {
        // Arrange
        var callCount = 0;
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed");
                return Array.Empty<HashEntry>();
            });

        var deduplicator = new RedisFeishuEventDistributedDeduplicatorWithFallback(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object,
            maxRetryCount: 1);

        // Act
        await deduplicator.TryMarkAsProcessedAsync("test_event_1"); // 失败
        await deduplicator.TryMarkAsProcessedAsync("test_event_2"); // 成功

        // Assert
        Assert.True(deduplicator.IsUsingRedis);
        Assert.Equal(0, deduplicator.ConsecutiveFailures);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenRedisAvailable_ShouldUseRedis()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("completed");

        var deduplicator = new RedisFeishuEventDistributedDeduplicatorWithFallback(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.IsProcessedAsync("test_event_123");

        // Assert
        Assert.True(result);
        Assert.True(deduplicator.IsUsingRedis);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenRedisFails_ShouldFallbackToMemory()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var deduplicator = new RedisFeishuEventDistributedDeduplicatorWithFallback(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object,
            maxRetryCount: 1);

        // Act
        var result = await deduplicator.IsProcessedAsync("test_event_123");

        // Assert
        Assert.False(result); // 降级到内存，未找到
        Assert.Equal(1, deduplicator.ConsecutiveFailures);
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WithEmptyEventId_ShouldReturnFalse()
    {
        // Arrange
        var deduplicator = new RedisFeishuEventDistributedDeduplicatorWithFallback(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsProcessedAsync("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WithRetry_ShouldRetryOnTimeout()
    {
        // Arrange
        var callCount = 0;
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 2)
                    throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed");
                return Array.Empty<HashEntry>();
            });

        var deduplicator = new RedisFeishuEventDistributedDeduplicatorWithFallback(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object,
            maxRetryCount: 3,
            initialRetryDelay: TimeSpan.FromMilliseconds(10));

        // Act
        var result = await deduplicator.TryMarkAsProcessedAsync("test_event_123");

        // Assert
        Assert.False(result); // 重试成功，新事件
        Assert.Equal(2, callCount); // 第一次失败，第二次成功
        Assert.True(deduplicator.IsUsingRedis);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldCleanupFallbackDeduplicator()
    {
        // Arrange
        var deduplicator = new RedisFeishuEventDistributedDeduplicatorWithFallback(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.CleanupExpiredAsync();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenRedisUnavailable_ShouldUseFallbackDirectly()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var deduplicator = new RedisFeishuEventDistributedDeduplicatorWithFallback(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object,
            maxRetryCount: 1);

        // 触发 3 次失败，标记 Redis 不可用
        await deduplicator.TryMarkAsProcessedAsync("test_event_1");
        await deduplicator.TryMarkAsProcessedAsync("test_event_2");
        await deduplicator.TryMarkAsProcessedAsync("test_event_3");

        Assert.False(deduplicator.IsUsingRedis);

        // Act - 再次调用应该直接使用降级，不再尝试 Redis
        var result = await deduplicator.TryMarkAsProcessedAsync("test_event_4");

        // Assert
        Assert.False(result); // 新事件
        Assert.False(deduplicator.IsUsingRedis);
    }
}
