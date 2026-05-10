// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Redis.Services;

namespace Mud.Feishu.Redis.Tests.Services;

/// <summary>
/// RedisFeishuNonceDistributedDeduplicator 单元测试
/// </summary>
public class RedisFeishuNonceDistributedDeduplicatorTests
{
    private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<ILogger<RedisFeishuNonceDistributedDeduplicator>> _loggerMock;

    public RedisFeishuNonceDistributedDeduplicatorTests()
    {
        _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _loggerMock = new Mock<ILogger<RedisFeishuNonceDistributedDeduplicator>>();

        _connectionMultiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
    }

    [Fact]
    public async Task TryMarkAsUsedAsync_WhenFirstNonce_ShouldReturnFalse()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(),
                When.NotExists
                ))
            .ReturnsAsync(true);

        var deduplicator = new RedisFeishuNonceDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsUsedAsync("test_nonce_123");

        // Assert
        Assert.False(result); // StringSetAsync 返回 true（设置成功），表示新 Nonce，所以 TryMarkAsUsedAsync 返回 false
        _databaseMock.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            When.NotExists), Times.Once);
    }

    [Fact]
    public async Task TryMarkAsUsedAsync_WhenDuplicateNonce_ShouldReturnTrue()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(),
                When.NotExists
                ))
            .ReturnsAsync(false);

        var deduplicator = new RedisFeishuNonceDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsUsedAsync("test_nonce_123");

        // Assert
        Assert.True(result); // StringSetAsync 返回 false（键已存在），表示重复 Nonce，所以 TryMarkAsUsedAsync 返回 true
    }

    [Fact]
    public async Task TryMarkAsUsedAsync_WithEmptyNonce_ShouldReturnFalse()
    {
        // Arrange
        var deduplicator = new RedisFeishuNonceDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsUsedAsync("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryMarkAsUsedAsync_WithCustomTtl_ShouldUseCustomTtl()
    {
        // Arrange
        var customTtl = TimeSpan.FromMinutes(10);
        _databaseMock
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                customTtl,
                When.NotExists
                ))
            .ReturnsAsync(true);

        var deduplicator = new RedisFeishuNonceDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsUsedAsync("test_nonce_123", null, customTtl);

        // Assert
        Assert.False(result); // StringSetAsync 返回 true 表示设置成功(新Nonce)，所以 TryMarkAsUsedAsync 应返回 false
        _databaseMock.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            customTtl,
            When.NotExists), Times.Once);
    }

    [Fact]
    public async Task IsUsedAsync_WhenNonceExists_ShouldReturnTrue()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var deduplicator = new RedisFeishuNonceDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.IsUsedAsync("test_nonce_123");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsUsedAsync_WhenNonceNotExists_ShouldReturnFalse()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var deduplicator = new RedisFeishuNonceDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.IsUsedAsync("test_nonce_123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsUsedAsync_WithEmptyNonce_ShouldReturnFalse()
    {
        // Arrange
        var deduplicator = new RedisFeishuNonceDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.IsUsedAsync("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryMarkAsUsedAsync_WhenRedisConnectionFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(),
                When.NotExists
                ))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var deduplicator = new RedisFeishuNonceDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await deduplicator.TryMarkAsUsedAsync("test_nonce_123"));
    }

    [Fact]
    public async Task TryMarkAsUsedAsync_WhenRedisTimeout_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(),
                When.NotExists
                ))
            .ThrowsAsync(new RedisTimeoutException("Timeout", CommandStatus.Unknown));

        var deduplicator = new RedisFeishuNonceDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await deduplicator.TryMarkAsUsedAsync("test_nonce_123"));
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var deduplicator = new RedisFeishuNonceDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.CleanupExpiredAsync();

        // Assert
        Assert.Equal(0, result); // Redis 自动清理，返回 0
    }

    [Fact]
    public async Task RemoveAsync_WhenNonceExists_ShouldReturnTrue()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var deduplicator = new RedisFeishuNonceDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.RemoveAsync("test_nonce_123");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RemoveAsync_WithEmptyNonce_ShouldReturnFalse()
    {
        // Arrange
        var deduplicator = new RedisFeishuNonceDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.RemoveAsync("");

        // Assert
        Assert.False(result);
    }
}
