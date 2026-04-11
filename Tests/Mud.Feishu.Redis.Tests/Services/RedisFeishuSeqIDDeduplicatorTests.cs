// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Redis.Services;

namespace Mud.Feishu.Redis.Tests.Services;

/// <summary>
/// RedisFeishuSeqIDDeduplicator 单元测试
/// </summary>
public class RedisFeishuSeqIDDeduplicatorTests
{
    private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<ILogger<RedisFeishuSeqIDDeduplicator>> _loggerMock;

    public RedisFeishuSeqIDDeduplicatorTests()
    {
        _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _loggerMock = new Mock<ILogger<RedisFeishuSeqIDDeduplicator>>();

        _connectionMultiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenFirstSeqId_ShouldReturnFalse()
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
        _databaseMock
            .Setup(x => x.SortedSetAddAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<double>(),
                It.IsAny<SortedSetWhen>(),
                It.IsAny<CommandFlags>()
                ))
            .ReturnsAsync(true);

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsProcessedAsync(12345);

        // Assert
        Assert.False(result); // StringSetAsync 返回 true（设置成功），表示新 SeqID，所以 TryMarkAsProcessedAsync 返回 false
        _databaseMock.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            When.NotExists), Times.Once);
        _databaseMock.Verify(x => x.SortedSetAddAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<double>(),
            It.IsAny<SortedSetWhen>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenDuplicateSeqId_ShouldReturnTrue()
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

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsProcessedAsync(12345);

        // Assert
        Assert.True(result); // StringSetAsync 返回 false（键已存在），表示重复 SeqID，所以 TryMarkAsProcessedAsync 返回 true
    }

    [Fact]
    public void TryMarkAsProcessed_WhenFirstSeqId_ShouldReturnFalse()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringSet(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(),
                When.NotExists
                ))
            .Returns(true);
        _databaseMock
            .Setup(x => x.SortedSetAdd(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), It.IsAny<SortedSetWhen>(), It.IsAny<CommandFlags>()))
            .Returns(true);

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = deduplicator.TryMarkAsProcessed(12345);

        // Assert
        Assert.False(result); // StringSet 返回 true（设置成功），表示新 SeqID，所以 TryMarkAsProcessed 返回 false
    }

    [Fact]
    public void TryMarkAsProcessed_WhenDuplicateSeqId_ShouldReturnTrue()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringSet(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(),
                When.NotExists
                ))
            .Returns(false);

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = deduplicator.TryMarkAsProcessed(12345);

        // Assert
        Assert.True(result); // StringSet 返回 false（键已存在），表示重复 SeqID，所以 TryMarkAsProcessed 返回 true
    }

    [Fact]
    public async Task IsProcessedAsync_WhenSeqIdExists_ShouldReturnTrue()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.IsProcessedAsync(12345);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenSeqIdNotExists_ShouldReturnFalse()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.IsProcessedAsync(12345);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsProcessed_WhenSeqIdExists_ShouldReturnTrue()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.KeyExists(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(true);

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = deduplicator.IsProcessed(12345);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsProcessed_WhenSeqIdNotExists_ShouldReturnFalse()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.KeyExists(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(false);

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = deduplicator.IsProcessed(12345);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetCacheCount_ShouldReturnCount()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.SortedSetLength(It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<Exclude>(), It.IsAny<CommandFlags>()))
            .Returns(5);

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = deduplicator.GetCacheCount();

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void GetMaxProcessedSeqId_WhenSeqIdsExist_ShouldReturnMaxSeqId()
    {
        // Arrange
        var maxSeqId = 99999UL;
        _databaseMock
            .Setup(x => x.SortedSetRangeByScore(
                It.IsAny<RedisKey>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<Exclude>(),
                It.IsAny<Order>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<CommandFlags>()
                ))
            .Returns(new RedisValue[] { maxSeqId.ToString() });

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = deduplicator.GetMaxProcessedSeqId();

        // Assert
        Assert.Equal(maxSeqId, result);
    }

    [Fact]
    public void GetMaxProcessedSeqId_WhenNoSeqIds_ShouldReturnZero()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.SortedSetRangeByScore(
                It.IsAny<RedisKey>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<Exclude>(),
                It.IsAny<Order>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<CommandFlags>()
                ))
            .Returns(Array.Empty<RedisValue>());

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = deduplicator.GetMaxProcessedSeqId();

        // Assert
        Assert.Equal(0UL, result);
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenRedisConnectionFails_ShouldThrowInvalidOperationException()
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

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await deduplicator.TryMarkAsProcessedAsync(12345));
    }

    [Fact]
    public void TryMarkAsProcessed_WhenRedisConnectionFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringSet(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<When>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => deduplicator.TryMarkAsProcessed(12345));
    }
}
