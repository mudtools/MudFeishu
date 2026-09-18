// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Redis.Services;

namespace Mud.Feishu.Redis.Tests.Services;

/// <summary>
/// RedisFeishuEventDistributedDeduplicator 单元测试
/// </summary>
public class RedisFeishuEventDistributedDeduplicatorTests
{
    private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<RedisFeishuEventDistributedDeduplicator>> _loggerMock;

    public RedisFeishuEventDistributedDeduplicatorTests()
    {
        _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<RedisFeishuEventDistributedDeduplicator>>();

        _connectionMultiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenFirstEvent_ShouldReturnSuccess()
    {
        // Arrange - Lua script returns 0 for new event
        _databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(0L));

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsProcessingAsync("test_event_123");

        // Assert
        Assert.False(result.IsDuplicate);
        Assert.Equal("test_event_123", result.EventId);
        _databaseMock.Verify(x => x.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.IsAny<RedisKey[]>(),
            It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenEventCompleted_ShouldReturnDuplicate()
    {
        // Arrange - Lua script returns 1 for completed event
        _databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(1L));

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsProcessingAsync("test_event_123");

        // Assert
        Assert.True(result.IsDuplicate);
        Assert.False(result.WasProcessing);
        Assert.Equal(DeduplicationStatus.Completed, result.Status);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenEventProcessing_ShouldReturnDuplicate()
    {
        // Arrange - Lua script returns 2 for processing event
        _databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(2L));

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsProcessingAsync("test_event_123");

        // Assert
        Assert.True(result.IsDuplicate);
        Assert.True(result.WasProcessing);
        Assert.Equal(DeduplicationStatus.Processing, result.Status);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenProcessingTimeout_ShouldReturnTimeoutRecoverable()
    {
        // Arrange - Lua script returns 3 for timeout recoverable
        _databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(3L));

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsProcessingAsync("test_event_123");

        // Assert
        Assert.False(result.IsDuplicate);
        Assert.Equal("test_event_123", result.EventId);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenRedisFails_ShouldThrowFeishuRedisException()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Redis connection failed"));

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act & Assert
        // R-04：Redis 异常统一包装为 FeishuRedisException（InvalidOperationException 子类）
        await Assert.ThrowsAsync<FeishuRedisException>(
            async () => await deduplicator.TryMarkAsProcessingAsync("test_event_123"));
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenRedisConnectionFails_ShouldThrowFeishuRedisException()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<FeishuRedisException>(
            async () => await deduplicator.TryMarkAsProcessingAsync("test_event_123"));
    }

    [Fact]
    public async Task IsProcessedAsync_WhenEventExists_ShouldReturnTrue()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("completed");

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.IsProcessedAsync("test_event_123");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenEventNotExists_ShouldReturnFalse()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.IsProcessedAsync("test_event_123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.CleanupExpiredAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WithEmptyEventId_ShouldReturnSuccess()
    {
        // Arrange
        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsProcessingAsync("");

        // Assert
        Assert.False(result.IsDuplicate);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WithCustomTtl_ShouldUseCustomTtl()
    {
        // Arrange
        var customTtl = TimeSpan.FromMinutes(10);
        _databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(0L));

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsProcessingAsync("test_event_123", null, customTtl);

        // Assert
        Assert.False(result.IsDuplicate);
        _databaseMock.Verify(x => x.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.IsAny<RedisKey[]>(),
            It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsCompletedAsync_ShouldRunCompletionLuaScript()
    {
        // Arrange
        // ADR-2 / R-05：MarkAsCompleted 已改为 Lua 脚本实现（键不存在则不创建）。
        // 脚本返回 1 表示标记成功；0 表示键不存在（best-effort 不创建）。
        _databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(1L));

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        await deduplicator.MarkAsCompletedAsync("test_event_123");

        // Assert
        _databaseMock.Verify(x => x.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.IsAny<RedisKey[]>(),
            It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsCompletedAsync_WhenRedisFails_ShouldThrowFeishuRedisException()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Redis error"));

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<FeishuRedisException>(
            async () => await deduplicator.MarkAsCompletedAsync("test_event_123"));
    }

    #region WHF-11（R-15 完全兑现）：时钟统一为 Redis 服务端 TIME

    [Fact]
    public async Task MarkAsCompletedAsync_ShouldUseServerSideClock_AndPassNoTimestampArgument()
    {
        // Arrange - 捕获实际下发的脚本与参数
        string? capturedScript = null;
        RedisValue[]? capturedValues = null;
        _databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .Callback((string script, RedisKey[] _, RedisValue[] values, CommandFlags _) =>
            {
                capturedScript = script;
                capturedValues = values;
            })
            .ReturnsAsync(RedisResult.Create(1L));

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        await deduplicator.MarkAsCompletedAsync("whf11_event");

        // Assert
        Assert.NotNull(capturedScript);
        Assert.Contains("redis.call('TIME')", capturedScript!);
        // WHF-11：时间戳必须取自 Redis 服务端时钟，消除与 C# DateTimeOffset.UtcNow 的漂移
        // MarkAsCompleted 只传 TTL，不再有 C# 端时间戳参数
        Assert.Equal(1, capturedValues!.Length);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_ShouldUseServerSideClock_AndPassNoTimestampArgument()
    {
        // Arrange - TryMark 脚本同样以 redis.call('TIME') 为唯一时钟源（ARGV 仅有超时与 TTL）
        string? capturedScript = null;
        RedisValue[]? capturedValues = null;
        _databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .Callback((string script, RedisKey[] _, RedisValue[] values, CommandFlags _) =>
            {
                capturedScript = script;
                capturedValues = values;
            })
            .ReturnsAsync(RedisResult.Create(0L));

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        await deduplicator.TryMarkAsProcessingAsync("whf11_event");

        // Assert
        Assert.NotNull(capturedScript);
        Assert.Contains("redis.call('TIME')", capturedScript!);
        // TryMark 仅传 processingTimeout 与 ttl 两个参数，无 C# 端时间戳
        Assert.Equal(2, capturedValues!.Length);
    }

    #endregion
}
