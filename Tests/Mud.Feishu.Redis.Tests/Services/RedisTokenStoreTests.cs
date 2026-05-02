// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Mud.Feishu.Redis.Services;

namespace Mud.Feishu.Redis.Tests.Services;

public class RedisTokenStoreTests
{
    private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<ILogger<RedisTokenStore>> _loggerMock;

    public RedisTokenStoreTests()
    {
        _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _loggerMock = new Mock<ILogger<RedisTokenStore>>();

        _connectionMultiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        _databaseMock
            .Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _databaseMock
            .Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(2L);
    }

    [Fact]
    public void Constructor_WhenRedisIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RedisTokenStore(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RedisTokenStore(_connectionMultiplexerMock.Object, null!));
    }

    [Fact]
    public void Constructor_WithCustomKeyPrefix_ShouldUseCustomPrefix()
    {
        var store = new RedisTokenStore(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object,
            "custom:prefix");

        Assert.NotNull(store);
    }

    [Fact]
    public void Constructor_WithNullKeyPrefix_ShouldUseDefaultPrefix()
    {
        var store = new RedisTokenStore(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object,
            null!);

        Assert.NotNull(store);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenTokenExists_ShouldReturnToken()
    {
        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("test_access_token");

        var store = new RedisTokenStore(_connectionMultiplexerMock.Object, _loggerMock.Object);

        var result = await store.GetAccessTokenAsync("app");

        Assert.Equal("test_access_token", result);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenTokenNotExists_ShouldReturnNull()
    {
        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var store = new RedisTokenStore(_connectionMultiplexerMock.Object, _loggerMock.Object);

        var result = await store.GetAccessTokenAsync("app");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldNotThrow()
    {
        var store = new RedisTokenStore(_connectionMultiplexerMock.Object, _loggerMock.Object);

        await store.SetAccessTokenAsync("app", "new_token", 7200);
    }

    [Fact]
    public async Task GetRefreshTokenAsync_WhenTokenExists_ShouldReturnToken()
    {
        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("test_refresh_token");

        var store = new RedisTokenStore(_connectionMultiplexerMock.Object, _loggerMock.Object);

        var result = await store.GetRefreshTokenAsync("app");

        Assert.Equal("test_refresh_token", result);
    }

    [Fact]
    public async Task GetRefreshTokenAsync_WhenTokenNotExists_ShouldReturnNull()
    {
        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var store = new RedisTokenStore(_connectionMultiplexerMock.Object, _loggerMock.Object);

        var result = await store.GetRefreshTokenAsync("app");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetRefreshTokenAsync_ShouldNotThrow()
    {
        var store = new RedisTokenStore(_connectionMultiplexerMock.Object, _loggerMock.Object);

        await store.SetRefreshTokenAsync("app", "refresh_token");
    }

    [Fact]
    public async Task RemoveAsync_ShouldNotThrow()
    {
        var store = new RedisTokenStore(_connectionMultiplexerMock.Object, _loggerMock.Object);

        await store.RemoveAsync("app");
    }

    [Fact]
    public async Task SetAndGetAccessToken_ShouldRoundTrip()
    {
        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("saved_token");

        var store = new RedisTokenStore(_connectionMultiplexerMock.Object, _loggerMock.Object);

        await store.SetAccessTokenAsync("app", "saved_token", 7200);
        var result = await store.GetAccessTokenAsync("app");

        Assert.Equal("saved_token", result);
    }
}
