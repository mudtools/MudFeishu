// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Moq;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;
using Xunit;

namespace Mud.Feishu.Redis.Tests;

/// <summary>
/// 验证多应用 Redis 令牌键隔离（TOK-1）。
/// </summary>
/// <remarks>
/// 修复前 <c>SingletonFeishuTokenStoreFactory</c> 忽略 appKey，所有应用共享
/// <c>feishu:token:*</c> 键空间，导致多应用令牌互相覆盖（随机 401 / 令牌串号）。
/// </remarks>
public class PerAppRedisTokenStoreKeyIsolationTests
{
    private static (PerAppRedisTokenStoreFactory Factory, List<string> Keys) BuildFactory(IEnumerable<string>? serverKeys = null)
    {
        var accessedKeys = new List<string>();

        var db = new Mock<IDatabase>();
        db.Setup(d => d.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisKey, CommandFlags>((key, _) => accessedKeys.Add(key.ToString()))
            .ReturnsAsync(RedisValue.Null);

        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        if (serverKeys != null)
        {
            // GetTokenTypesAsync / ClearAsync 走 GetServer(...).Keys(...)，需要额外的服务器端桩。
            var server = new Mock<IServer>();
            server.Setup(s => s.IsConnected).Returns(true);
            server.Setup(s => s.IsReplica).Returns(false);
            // StackExchange.Redis 2.10 的 4 参数 Keys(...) 重载内部转发到 6 参数重载
            // （database, pattern, pageSize, cursor, pageOffset, flags），
            // 因此在 Moq 代理上被拦截的是 6 参数版本，必须按该签名做桩。
            server.Setup(s => s.Keys(
                    It.IsAny<int>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<int>(),
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<CommandFlags>()))
                .Returns(serverKeys.Select(k => (RedisKey)k));

            var endpoint = new System.Net.DnsEndPoint("localhost", 6379);
            redis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns(new System.Net.EndPoint[] { endpoint });
            redis.Setup(r => r.GetServer(It.IsAny<System.Net.EndPoint>(), It.IsAny<object>())).Returns(server.Object);
        }

        return (new PerAppRedisTokenStoreFactory(redis.Object), accessedKeys);
    }

    [Theory]
    [InlineData("cli_a", "feishu:cli_a:token")]
    [InlineData("cli_b", "feishu:cli_b:token")]
    [InlineData("", "feishu:default:token")]
    public void BuildKeyPrefix_ShouldContainAppKey(string appKey, string expected)
    {
        PerAppRedisTokenStoreFactory.BuildKeyPrefix(appKey).Should().Be(expected);
    }

    /// <summary>
    /// 不同应用访问令牌必须落到不同的 Redis 键（修复前两者完全相同）。
    /// </summary>
    [Fact]
    public async Task GetAccessTokenAsync_ShouldUseDistinctKeys_PerApp()
    {
        var (factory, keys) = BuildFactory();

        var (storeA, _) = factory.Create("cli_a");
        var (storeB, _) = factory.Create("cli_b");

        await storeA.GetAccessTokenAsync("tenant:cli_a");
        await storeB.GetAccessTokenAsync("tenant:cli_b");

        keys.Should().HaveCount(2);
        keys[0].Should().NotBe(keys[1]);
        keys[0].Should().Contain("cli_a");
        keys[1].Should().Contain("cli_b");
    }

    /// <summary>
    /// 不同应用的用户令牌也必须隔离（修复前用户令牌键完全不含 appKey）。
    /// </summary>
    [Fact]
    public async Task UserTokenStore_ShouldUseDistinctKeys_PerApp()
    {
        var (factory, keys) = BuildFactory();

        var (_, userA) = factory.Create("cli_a");
        var (_, userB) = factory.Create("cli_b");

        await userA!.GetAccessTokenAsync("ou_1", "user");
        await userB!.GetAccessTokenAsync("ou_1", "user");

        keys.Should().HaveCount(2);
        keys[0].Should().NotBe(keys[1]);
        keys[0].Should().Contain("cli_a");
        keys[1].Should().Contain("cli_b");
    }

    /// <summary>
    /// 同一应用重复 Create 应返回同一实例，避免 per-app 实例膨胀。
    /// </summary>
    [Fact]
    public void Create_ShouldReturnSameInstance_ForSameAppKey()
    {
        var (factory, _) = BuildFactory();

        var (first, _) = factory.Create("cli_a");
        var (second, _) = factory.Create("cli_a");

        first.Should().BeSameAs(second);
    }

    /// <summary>
    /// tokenType 自带 ':' 时键仍需正确（TM-04 回归，原实现用 Split(':') 会截断）。
    /// </summary>
    [Fact]
    public async Task GetAccessTokenAsync_ShouldPreserveTokenType_WhenTokenTypeContainsColon()
    {
        var (factory, keys) = BuildFactory();

        var (store, _) = factory.Create("cli_a");
        await store.GetAccessTokenAsync("tenant:cli_a");

        keys[0].Should().Be("feishu:cli_a:token:tenant:cli_a:access");
    }

    /// <summary>
    /// TM-04 回归：<c>GetTokenTypesAsync</c> 必须返回**完整** tokenType。
    /// 原实现按 <c>Split(':')[0]</c> 取首段，会把 <c>tenant:cli_a</c> 截断为 <c>tenant</c>，
    /// 导致上层无法按 tokenType 精确失效/刷新令牌。修复方式为剥离已知前缀与 <c>:access</c> 后缀。
    /// </summary>
    /// <remarks>
    /// 这里必须走 <c>GetServer(...).Keys(...)</c> 路径（而非仅校验键构造），
    /// 否则修复本身不会被覆盖：键构造一直是对的，出问题的是键 → tokenType 的反解。
    /// </remarks>
    [Fact]
    public async Task GetTokenTypesAsync_ShouldReturnFullTokenType_WhenTokenTypeContainsColon()
    {
        var (factory, _) = BuildFactory(new[]
        {
            "feishu:cli_a:token:tenant:cli_a:access",   // 期望：tenant:cli_a
            "feishu:cli_a:token:tenant:cli_a:refresh",  // refresh 键应被忽略
            "feishu:cli_a:token:app:cli_a:access"       // 期望：app:cli_a
        });

        var (store, _) = factory.Create("cli_a");
        var tokenTypes = (await store.GetTokenTypesAsync()).ToList();

        tokenTypes.Should().BeEquivalentTo(new[] { "tenant:cli_a", "app:cli_a" });
    }

    /// <summary>
    /// TM-04 回归（用户令牌路径）：<c>RedisUserTokenStore.GetTokenTypesAsync</c> 同样不得截断 tokenType。
    /// </summary>
    [Fact]
    public async Task UserTokenStore_GetTokenTypesAsync_ShouldReturnFullTokenType_WhenTokenTypeContainsColon()
    {
        var (factory, _) = BuildFactory(new[]
        {
            "feishu:cli_a:token:user:ou_1:user:cli_a:access"
        });

        var (_, userStore) = factory.Create("cli_a");
        var tokenTypes = (await userStore!.GetTokenTypesAsync("ou_1")).ToList();

        tokenTypes.Should().ContainSingle().Which.Should().Be("user:cli_a");
    }
}
