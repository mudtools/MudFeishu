// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
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
        // ADR-4：写入路径已改为原子化 Lua 脚本（SETNX + ZADD + 裁剪 + EXPIRE）。
        // 脚本返回 0 表示 SETNX 成功（新 SeqID）。
        _databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(0L));

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object,
            scopeKey: "test-scope");

        // Act
        var result = await deduplicator.TryMarkAsProcessedAsync(12345);

        // Assert
        Assert.False(result); // Lua 脚本返回 0（SETNX 成功），表示新 SeqID
        _databaseMock.Verify(x => x.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.IsAny<RedisKey[]>(),
            It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenDuplicateSeqId_ShouldReturnTrue()
    {
        // Arrange
        // ADR-4：脚本返回 1 表示键已存在（SETNX 失败）→ 重复 SeqID
        _databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(1L));

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object,
            scopeKey: "test-scope");

        // Act
        var result = await deduplicator.TryMarkAsProcessedAsync(12345);

        // Assert
        Assert.True(result);
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
            _loggerMock.Object,
            scopeKey: "test-scope");

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
            _loggerMock.Object,
            scopeKey: "test-scope");

        // Act
        var result = await deduplicator.IsProcessedAsync(12345);

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
            _loggerMock.Object,
            scopeKey: "test-scope");

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
            _loggerMock.Object,
            scopeKey: "test-scope");

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
            _loggerMock.Object,
            scopeKey: "test-scope");

        // Act
        var result = deduplicator.GetMaxProcessedSeqId();

        // Assert
        Assert.Equal(0UL, result);
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenRedisConnectionFails_ShouldThrowFeishuRedisException()
    {
        // Arrange
        // ADR-4 + R-04：Redis 异常统一包装为 FeishuRedisException（InvalidOperationException 子类）
        _databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var deduplicator = new RedisFeishuSeqIDDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object,
            scopeKey: "test-scope");

        // Act & Assert
        await Assert.ThrowsAsync<FeishuRedisException>(
            async () => await deduplicator.TryMarkAsProcessedAsync(12345));
    }

}
