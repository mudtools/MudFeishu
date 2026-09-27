// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.Redis.Extensions;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Tests.Conversations;

/// <summary>
/// <see cref="RedisConversationStore"/>：编码格式、TMA-15「无时间戳视为 miss」、
/// 服务器 TTL 双写、DI 替换语义（Phase 0 §5/§7）。
/// </summary>
public class RedisConversationStoreTests
{
    private static (RedisConversationStore Store, Mock<IDatabase> Database) CreateStore(TimeSpan? ttl = null)
    {
        var database = new Mock<IDatabase>();
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(database.Object);

        var store = new RedisConversationStore(
            redis.Object,
            ttl ?? TimeSpan.FromHours(1),
            NullLogger<RedisConversationStore>.Instance);

        return (store, database);
    }

    private static string AnyKey() => ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_1");

    [Fact]
    public async Task SaveAsync_ShouldStoreEncodedPayload_WithServerSideTtl()
    {
        var (store, database) = CreateStore(ttl: TimeSpan.FromSeconds(30));
        RedisValue captured = default;
        TimeSpan? capturedExpiry = null;
        database.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
                It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, TimeSpan?, When, CommandFlags>((_, v, expiry, _, _) =>
            {
                captured = v;
                capturedExpiry = expiry;
            })
            .ReturnsAsync(true);

        await store.SaveAsync(AnyKey(), "{\"s\":1}");

        capturedExpiry.Should().Be(TimeSpan.FromSeconds(30), "服务器键 TTL 与载荷时间戳双写");
        captured.ToString().Should().EndWith("|{\"s\":1}", "存储值必须是 {expireTimestampMs}|{payload} 编码格式");

        SessionStoreEncoding.TryDecode(captured.ToString(), out var payload, out var expireAtMs)
            .Should().BeTrue();
        payload.Should().Be("{\"s\":1}");
        expireAtMs.Should().BeGreaterThan(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task GetAsync_ShouldReturnPayload_WhenValidStoredValue()
    {
        var (store, database) = CreateStore();
        var stored = SessionStoreEncoding.Encode("{\"s\":1}", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 60_000);
        database.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)stored);

        var payload = await store.GetAsync(AnyKey());

        payload.Should().Be("{\"s\":1}");
    }

    [Fact]
    public async Task GetAsync_ShouldTreatAsMiss_WhenNoTimestamp()
    {
        // TMA-15：无过期时间戳的旧格式/损坏数据视为 miss，调用方重建会话。
        var (store, database) = CreateStore();
        database.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)"raw-payload-without-timestamp");

        var payload = await store.GetAsync(AnyKey());

        payload.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ShouldTreatAsMiss_WhenExpired()
    {
        var (store, database) = CreateStore();
        var stored = SessionStoreEncoding.Encode("{\"s\":1}", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1);
        database.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)stored);

        var payload = await store.GetAsync(AnyKey());

        payload.Should().BeNull("读侧时间戳是有效性权威（服务器 TTL 仅兜底）");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenKeyMissing()
    {
        var (store, database) = CreateStore();
        database.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        (await store.GetAsync(AnyKey())).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteKey()
    {
        var (store, database) = CreateStore();
        database.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await store.DeleteAsync(AnyKey());

        database.Verify(d => d.KeyDeleteAsync(
            It.Is<RedisKey>(k => k.ToString().StartsWith("feishu:conversation:", StringComparison.Ordinal)),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public void Ctor_ShouldRejectNonPositiveTtl()
    {
        var redis = new Mock<IConnectionMultiplexer>();
        var act = () => new RedisConversationStore(redis.Object, TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// DI 接线：AddFeishuRedisConversationStore 必须显式覆盖默认（内存）注册，
    /// 且 TTL 单一阈值源取 FeishuConversationOptions.SessionTtl（D9 同源精神）。
    /// 本测试工程不引用 Mud.Feishu.AI（纵向引用治理），以桩实现模拟默认注册。
    /// </summary>
    [Fact]
    public void AddFeishuRedisConversationStore_ShouldReplaceDefaultStore()
    {
        var database = new Mock<IDatabase>();
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(database.Object);

        var services = new ServiceCollection();
        services.AddSingleton<IConnectionMultiplexer>(redis.Object);
        // 模拟 AddFeishuAgent 的默认内存存储注册（AI 类型不入本测试工程）。
        services.AddSingleton<IConversationStore>(new StubConversationStore());
        services.AddOptions<FeishuConversationOptions>()
            .Configure(o => o.SessionTtl = TimeSpan.FromMinutes(3));
        services.AddFeishuRedisConversationStore();

        using var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IConversationStore>();
        store.Should().BeOfType<RedisConversationStore>();
    }

    /// <summary>
    /// 未调用 AddFeishuAgent 时，AddFeishuRedisConversationStore 以默认值兜底注册
    /// FeishuConversationOptions（AddOptions 幂等，默认 TTL 24h）。
    /// </summary>
    [Fact]
    public void AddFeishuRedisConversationStore_ShouldFallbackRegisterOptions_WhenAgentAbsent()
    {
        var database = new Mock<IDatabase>();
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(database.Object);

        var services = new ServiceCollection();
        services.AddSingleton<IConnectionMultiplexer>(redis.Object);
        services.AddFeishuRedisConversationStore();

        using var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IConversationStore>();
        store.Should().BeOfType<RedisConversationStore>();
        provider.GetRequiredService<IOptions<FeishuConversationOptions>>()
            .Value.SessionTtl.Should().Be(TimeSpan.FromHours(24), "默认 TTL 与 FeishuConversationOptions 同源");
    }

    private sealed class StubConversationStore : IConversationStore
    {
        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task SaveAsync(string key, string serializedSession, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
