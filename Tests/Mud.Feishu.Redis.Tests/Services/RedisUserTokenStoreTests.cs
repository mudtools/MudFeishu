// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Mud.Feishu.Redis.Services;

namespace Mud.Feishu.Redis.Tests.Services;

public class RedisUserTokenStoreTests
{
    private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<ILogger<RedisTokenStore>> _tokenStoreLoggerMock;
    private readonly Mock<ILogger<RedisUserTokenStore>> _loggerMock;

    public RedisUserTokenStoreTests()
    {
        _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _tokenStoreLoggerMock = new Mock<ILogger<RedisTokenStore>>();
        _loggerMock = new Mock<ILogger<RedisUserTokenStore>>();

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

    private RedisTokenStore CreateInnerStore() =>
        new RedisTokenStore(_connectionMultiplexerMock.Object, _tokenStoreLoggerMock.Object);

    private RedisUserTokenStore CreateSut() =>
        new RedisUserTokenStore(CreateInnerStore(), _connectionMultiplexerMock.Object, _loggerMock.Object);

    [Fact]
    public void Constructor_WhenInnerStoreIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RedisUserTokenStore(null!, _connectionMultiplexerMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WhenRedisIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RedisUserTokenStore(CreateInnerStore(), null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RedisUserTokenStore(CreateInnerStore(), _connectionMultiplexerMock.Object, null!));
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithTokenTypeOnly_WhenTokenExists_ShouldReturnToken()
    {
        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("app_access_token");

        var store = CreateSut();

        var result = await store.GetAccessTokenAsync("app");

        Assert.Equal("app_access_token", result);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithTokenTypeOnly_WhenTokenNotExists_ShouldReturnNull()
    {
        var store = CreateSut();

        var result = await store.GetAccessTokenAsync("app");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAccessTokenAsync_WithTokenTypeOnly_ShouldNotThrow()
    {
        var store = CreateSut();

        await store.SetAccessTokenAsync("app", "token", 7200);
    }

    [Fact]
    public async Task GetRefreshTokenAsync_WithTokenTypeOnly_WhenTokenExists_ShouldReturnToken()
    {
        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("app_refresh_token");

        var store = CreateSut();

        var result = await store.GetRefreshTokenAsync("app");

        Assert.Equal("app_refresh_token", result);
    }

    [Fact]
    public async Task SetRefreshTokenAsync_WithTokenTypeOnly_ShouldNotThrow()
    {
        var store = CreateSut();

        await store.SetRefreshTokenAsync("app", "refresh_token");
    }

    [Fact]
    public async Task RemoveAsync_WithTokenTypeOnly_ShouldNotThrow()
    {
        var store = CreateSut();

        await store.RemoveAsync("app");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithUserIdAndTokenType_WhenTokenExists_ShouldReturnToken()
    {
        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("user_access_token");

        var store = CreateSut();

        var result = await store.GetAccessTokenAsync("ou_xxx", "user");

        Assert.Equal("user_access_token", result);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithUserIdAndTokenType_WhenTokenNotExists_ShouldReturnNull()
    {
        var store = CreateSut();

        var result = await store.GetAccessTokenAsync("ou_xxx", "user");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAccessTokenAsync_WithUserIdAndTokenType_ShouldNotThrow()
    {
        var store = CreateSut();

        await store.SetAccessTokenAsync("ou_xxx", "user", "user_token", 3600);
    }

    [Fact]
    public async Task GetRefreshTokenAsync_WithUserIdAndTokenType_WhenTokenExists_ShouldReturnToken()
    {
        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("user_refresh_token");

        var store = CreateSut();

        var result = await store.GetRefreshTokenAsync("ou_xxx", "user");

        Assert.Equal("user_refresh_token", result);
    }

    [Fact]
    public async Task SetRefreshTokenAsync_WithUserIdAndTokenType_ShouldNotThrow()
    {
        var store = CreateSut();

        await store.SetRefreshTokenAsync("ou_xxx", "user", "user_refresh");
    }

    [Fact]
    public async Task RemoveAsync_WithUserIdAndTokenType_ShouldNotThrow()
    {
        var store = CreateSut();

        await store.RemoveAsync("ou_xxx", "user");
    }
}
