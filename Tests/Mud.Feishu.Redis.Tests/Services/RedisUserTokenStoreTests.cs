// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
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

    public RedisUserTokenStoreTests()
    {
        _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _tokenStoreLoggerMock = new Mock<ILogger<RedisTokenStore>>();

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
        new RedisUserTokenStore(CreateInnerStore(), _connectionMultiplexerMock.Object);

    [Fact]
    public void Constructor_WhenInnerStoreIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RedisUserTokenStore(null!, _connectionMultiplexerMock.Object));
    }

    [Fact]
    public void Constructor_WhenRedisIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RedisUserTokenStore(CreateInnerStore(), null!));
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

    /// <summary>
    /// TMF-01（验收补测）：ClearAllUsersAsync 以 TokenKeyBuilder.AllUsersScanPattern
    /// 产出的全用户键模式 SCAN（D8 单一出口）并逐键删除，Cluster 化遍历模式与 ClearUserAsync 一致。
    /// SCAN mock 遵循 ADR-4 约定：StackExchange.Redis 2.10 的 Keys(...) 在 Moq 代理上
    /// 被拦截的是 6 参数重载（database, pattern, pageSize, cursor, pageOffset, flags）。
    /// </summary>
    [Fact]
    public async Task ClearAllUsersAsync_ShouldScanAllUsersPattern_AndDeleteReturnedKeys()
    {
        RedisValue capturedPattern = RedisValue.Null;
        var deletedKeys = new List<string>();

        var db = new Mock<IDatabase>();
        db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey, CommandFlags>((key, _) => deletedKeys.Add(key.ToString()))
            .ReturnsAsync(true);

        var server = new Mock<IServer>();
        server.Setup(s => s.IsConnected).Returns(true);
        server.Setup(s => s.IsReplica).Returns(false);
        // R2-08：实现已改为异步 SCAN（KeysAsync）+ 分批删除（KeyDeleteAsync(RedisKey[])）。
        // 桩必须按 IServer.KeysAsync(int database, RedisValue pattern, int pageSize,
        // long cursor, int pageOffset, CommandFlags flags) 的 6 参数签名做。
        server.Setup(s => s.KeysAsync(
                It.IsAny<int>(),
                It.IsAny<RedisValue>(),
                It.IsAny<int>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CommandFlags>()))
            .Callback<int, RedisValue, int, long, int, CommandFlags>((_, pattern, _, _, _, _) => capturedPattern = pattern)
            .Returns(() => ToAsyncKeys(
            [
                (RedisKey)"feishu:token:user:ou_1:UserAccessToken:access",
                (RedisKey)"feishu:token:user:ou_1:UserAccessToken:refresh",
                (RedisKey)"feishu:token:user:ou_2:CustomToken:access"
            ]));

        // 批删走 KeyDeleteAsync(RedisKey[]) 重载
        db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey[], CommandFlags>((keys, _) =>
            {
                foreach (var key in keys)
                    deletedKeys.Add(key.ToString());
            })
            .ReturnsAsync(3L);

        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        var endpoint = new System.Net.DnsEndPoint("localhost", 6379);
        redis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns(new System.Net.EndPoint[] { endpoint });
        redis.Setup(r => r.GetServer(It.IsAny<System.Net.EndPoint>(), It.IsAny<object>())).Returns(server.Object);

        var store = new RedisUserTokenStore(
            new RedisTokenStore(redis.Object, Mock.Of<ILogger<RedisTokenStore>>()),
            redis.Object);

        await store.ClearAllUsersAsync();

        Assert.Equal("feishu:token:user:*", capturedPattern.ToString());
        Assert.Equal(
            new[]
            {
                "feishu:token:user:ou_1:UserAccessToken:access",
                "feishu:token:user:ou_1:UserAccessToken:refresh",
                "feishu:token:user:ou_2:CustomToken:access"
            },
            deletedKeys);
    }

    /// <summary>
    /// 把键集合包装为 <c>IAsyncEnumerable&lt;RedisKey&gt;</c>（R2-08：KeysAsync 桩用）。
    /// </summary>
    private static async IAsyncEnumerable<RedisKey> ToAsyncKeys(IEnumerable<RedisKey> keys)
    {
        await Task.CompletedTask;
        foreach (var key in keys)
        {
            yield return key;
        }
    }
}
