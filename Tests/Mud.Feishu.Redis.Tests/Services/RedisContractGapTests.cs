// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.Redis.Extensions;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Tests.Services;

/// <summary>
/// R2 轮补齐的契约与边界用例（T-R2-15 / T-R2-16）。
/// </summary>
/// <remarks>
/// 覆盖评审中"无测试"的高风险项：非正/亚秒 TTL、Lua 未知返回值 fail-closed、`appKey` 三态键、
/// 服务端时钟读侧、取消令牌、`scopeKey` 容量参数、清理模式同源（R2-02 单测级回归）、
/// 令牌非正过期、`AddFeishuRedisTokenStore` 装配语义、健康检查开关、指标上报。
/// </remarks>
public class RedisContractGapTests
{
    private const string EventPrefix = "feishu:event:";
    private const string SeqIdPrefix = "feishu:seqid:";

    /// <summary>
    /// 构造事件去重器：Lua 固定返回 0（Success），供 TTL/生命周期/指标等不关心返回值的用例使用。
    /// </summary>
    private static (RedisFeishuEventDistributedDeduplicator Sut, Mock<IDatabase> Db) BuildEventDeduplicator()
    {
        var db = new Mock<IDatabase>();
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        db.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(0L));

        var sut = new RedisFeishuEventDistributedDeduplicator(redis.Object, NullLogger<RedisFeishuEventDistributedDeduplicator>.Instance);
        return (sut, db);
    }

    #region 事件去重：TTL 边界

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task EventDeduplicator_Should_Throw_When_TtlIsNotPositive(int ttlSeconds)
    {
        var (sut, _) = BuildEventDeduplicator();

        var act = () => sut.TryMarkAsProcessingAsync("evt1", ttl: TimeSpan.FromSeconds(ttlSeconds));

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*TTL*");
    }

    [Fact]
    public async Task EventDeduplicator_Should_Clamp_SubSecondTtl_To_OneSecond_InLuaArgs()
    {
        // R2-03：亚秒 TTL 不抛异常，而是 Math.Max(1, TotalSeconds) 后下发（origin: R-04 护栏）
        RedisValue[]? capturedValues = null;
        var db = new Mock<IDatabase>();
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        db.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Callback<string, RedisKey[], RedisValue[], CommandFlags>((_, _, values, _) => capturedValues = values)
            .ReturnsAsync(RedisResult.Create(0L));

        var sut = new RedisFeishuEventDistributedDeduplicator(redis.Object, NullLogger<RedisFeishuEventDistributedDeduplicator>.Instance);

        await sut.TryMarkAsProcessingAsync("evt1", ttl: TimeSpan.FromMilliseconds(500));

        capturedValues.Should().NotBeNull();
        capturedValues![1].ToString().Should().Be("1", "亚秒 TTL 必须被钳制为 1 秒（EXPIRE 0 会立即删键、静默禁用去重）");
    }

    #endregion

    #region 事件去重：fail-closed 与键构造

    [Fact]
    public async Task EventDeduplicator_Should_FailClosed_When_LuaReturnsUnknownValue()
    {
        var (sut, db) = BuildEventDeduplicator();
        db.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(99L));

        var act = () => sut.TryMarkAsProcessingAsync("evt1");

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*fail-closed*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task EventDeduplicator_Should_OmitAppKeySegment_When_AppKeyIsNullOrEmpty(string? appKey)
    {
        RedisKey capturedKey = default;
        var db = new Mock<IDatabase>();
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        db.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Callback<string, RedisKey[], RedisValue[], CommandFlags>((_, keys, _, _) => capturedKey = keys[0])
            .ReturnsAsync(RedisResult.Create(0L));

        var sut = new RedisFeishuEventDistributedDeduplicator(redis.Object, NullLogger<RedisFeishuEventDistributedDeduplicator>.Instance);

        await sut.TryMarkAsProcessingAsync("evt1", appKey);

        capturedKey.ToString().Should().Be("feishu:event::evt1",
            "null 与空串都必须退化为无 appKey 段的键（二者不可产生不同键空间）");
    }

    [Fact]
    public async Task EventDeduplicator_Should_EscapeColonInAppKey_ToAvoidKeyCollision()
    {
        RedisKey capturedKey = default;
        var db = new Mock<IDatabase>();
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        db.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Callback<string, RedisKey[], RedisValue[], CommandFlags>((_, keys, _, _) => capturedKey = keys[0])
            .ReturnsAsync(RedisResult.Create(0L));

        var sut = new RedisFeishuEventDistributedDeduplicator(redis.Object, NullLogger<RedisFeishuEventDistributedDeduplicator>.Instance);

        await sut.TryMarkAsProcessingAsync("c", appKey: "a:b");

        capturedKey.ToString().Should().Be(@"feishu:event::a\:b:c",
            "段内 ':' 必须转义，否则 appKey=\"a:b\"+eventId=\"c\" 会与 appKey=\"a\"+eventId=\"b:c\" 碰撞");
    }

    #endregion

    #region 事件去重：读侧服务端时钟（R2-10）

    [Fact]
    public async Task EventDeduplicator_GetStatusAsync_Should_Use_ServerTime_Not_ClientClock()
    {
        // 服务端时间构造为"很久以前"的时间戳 → 即使客户端此刻认为很新，也应判定为超时（Pending）
        const long serverNowSeconds = 1_800_000_000;
        var db = new Mock<IDatabase>();
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        db.Setup(d => d.ExecuteAsync("TIME", It.IsAny<object[]>()))
            .ReturnsAsync(RedisResult.Create(new RedisValue[] { serverNowSeconds.ToString(), "0" }));
        db.Setup(d => d.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new[]
            {
                new HashEntry("status", "processing"),
                new HashEntry("timestamp", (serverNowSeconds - 600).ToString()),
                new HashEntry("timeout", "300")
            });

        var sut = new RedisFeishuEventDistributedDeduplicator(
            redis.Object,
            NullLogger<RedisFeishuEventDistributedDeduplicator>.Instance,
            cacheExpiration: TimeSpan.FromHours(1),
            processingTimeout: TimeSpan.FromMinutes(5));

        var status = await sut.GetStatusAsync("evt1");

        status.Should().Be(DeduplicationStatus.Pending,
            "服务端时间下已超时（600s > 300s timeout）→ Pending；若用客户端时钟则会误判为 Processing");
    }

    [Fact]
    public async Task EventDeduplicator_GetStatusAsync_Should_FallBack_When_TimeCommandUnavailable()
    {
        var db = new Mock<IDatabase>();
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        db.Setup(d => d.ExecuteAsync("TIME", It.IsAny<object[]>()))
            .ThrowsAsync(new RedisServerException("TIME is disabled by ACL"));
        db.Setup(d => d.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new[]
            {
                new HashEntry("status", "processing"),
                new HashEntry("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new HashEntry("timeout", "300")
            });

        var sut = new RedisFeishuEventDistributedDeduplicator(redis.Object, NullLogger<RedisFeishuEventDistributedDeduplicator>.Instance);

        var status = await sut.GetStatusAsync("evt1");

        status.Should().Be(DeduplicationStatus.Processing, "TIME 不可用时必须回落客户端时间而不是整体失败");
    }

    #endregion

    #region 生命周期与取消

    [Fact]
    public async Task EventDeduplicator_Dispose_ThenCall_Should_Throw_ObjectDisposed()
    {
        var (sut, _) = BuildEventDeduplicator();

        sut.Dispose();

        var act = () => sut.TryMarkAsProcessingAsync("evt1");
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task NonceDeduplicator_Should_Honor_Cancellation()
    {
        var db = new Mock<IDatabase>();
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        var sut = new RedisFeishuNonceDistributedDeduplicator(redis.Object, NullLogger<RedisFeishuNonceDistributedDeduplicator>.Instance);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsyncCompat();

        var mark = () => sut.TryMarkAsUsedAsync("n1", cancellationToken: cts.Token);
        var check = () => sut.IsUsedAsync("n1", cancellationToken: cts.Token);

        await mark.Should().ThrowAsync<OperationCanceledException>();
        await check.Should().ThrowAsync<OperationCanceledException>();

        db.Verify(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Never,
            "取消后不得发出任何 Redis 命令");
    }

    #endregion

    #region SeqID：容量参数与清理模式同源（R2-01 / R2-02）

    [Fact]
    public async Task SeqIdDeduplicator_Should_PassWindowCapacity_AsThirdLuaArgument()
    {
        RedisValue[]? capturedValues = null;
        var db = new Mock<IDatabase>();
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        db.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Callback<string, RedisKey[], RedisValue[], CommandFlags>((_, _, values, _) => capturedValues = values)
            .ReturnsAsync(RedisResult.Create(0L));

        var sut = new RedisFeishuSeqIDDeduplicator(
            redis.Object, NullLogger<RedisFeishuSeqIDDeduplicator>.Instance,
            scopeKey: "test-scope", windowCapacity: 500);

        await sut.TryMarkAsProcessedAsync(42);

        capturedValues.Should().HaveCount(3);
        capturedValues![2].ToString().Should().Be("500", "容量窗口必须作为 ARGV[3] 下发（ADR-10）");
    }

    [Fact]
    public async Task SeqIdDeduplicator_ClearCacheAsync_Should_Use_PatternFromKeyBuilder()
    {
        // R2-02 单测级回归：模式必须由 RedisKeyBuilder.Pattern 产出（与键同源），且以 ":*" 结尾
        RedisValue capturedPattern = RedisValue.Null;
        var deletedKeys = new List<RedisKey>();

        var db = new Mock<IDatabase>();
        db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey[], CommandFlags>((keys, _) => deletedKeys.AddRange(keys))
            .ReturnsAsync(2L);

        var server = new Mock<IServer>();
        server.Setup(s => s.IsConnected).Returns(true);
        server.Setup(s => s.IsReplica).Returns(false);
        server.Setup(s => s.KeysAsync(
                It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(),
                It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Callback<int, RedisValue, int, long, int, CommandFlags>((_, pattern, _, _, _, _) => capturedPattern = pattern)
            .Returns(() => ToAsyncKeys(
            [
                (RedisKey)"feishu:seqid::test-scope:1",
                (RedisKey)"feishu:seqid::test-scope:set"
            ]));

        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        redis.Setup(r => r.GetEndPoints(It.IsAny<bool>()))
            .Returns(new System.Net.EndPoint[] { new System.Net.DnsEndPoint("localhost", 6379) });
        redis.Setup(r => r.GetServer(It.IsAny<System.Net.EndPoint>(), It.IsAny<object>())).Returns(server.Object);

        var sut = new RedisFeishuSeqIDDeduplicator(
            redis.Object, NullLogger<RedisFeishuSeqIDDeduplicator>.Instance,
            keyPrefix: SeqIdPrefix, scopeKey: "test-scope");

        await sut.ClearCacheAsync();

        capturedPattern.ToString().Should().Be(RedisKeyBuilder.Pattern(SeqIdPrefix, "test-scope"),
            "SCAN 模式必须由 RedisKeyBuilder 产出，否则会像 R2-02 一样永不匹配实际键");
        capturedPattern.ToString().Should().Be("feishu:seqid::test-scope:*",
            "模式必须以分隔符 + '*' 结尾（段级精确）");
        deletedKeys.Should().HaveCount(2, "命中键必须被真实删除（恒删 0 个即 R2-02 的失效形态）");
    }

    #endregion

    #region 令牌存储边界

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RedisUserTokenStore_Should_Throw_When_ExpiryIsNotPositive(long expiresInSeconds)
    {
        var store = new RedisUserTokenStore(
            new RedisTokenStore(new Mock<IConnectionMultiplexer>().Object, NullLogger<RedisTokenStore>.Instance),
            new Mock<IConnectionMultiplexer>().Object);

        var act = () => store.SetAccessTokenAsync("u1", "t1", "token", expiresInSeconds);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*过期时间*");
    }

    #endregion

    #region DI 装配语义

    [Fact]
    public void AddFeishuRedisTokenStore_Should_Throw_When_AddFeishuAppAlreadyCalled()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IFeishuAppManager>());

        var act = () => services.AddFeishuRedisTokenStore();

        act.Should().Throw<InvalidOperationException>().WithMessage("*必须在 AddFeishuApp 之前调用*");
    }

    [Fact]
    public void AddFeishuRedisTokenStore_Should_RegisterMultiplexer_When_CalledStandalone()
    {
        // 独立调用（未先 AddFeishuRedis）也必须自举连接注册（R-24）
        var services = new ServiceCollection();

        services.AddFeishuRedisTokenStore();

        services.Any(s => s.ServiceType == typeof(IConnectionMultiplexer))
            .Should().BeTrue("AddFeishuRedisTokenStore 必须幂等补注册 IConnectionMultiplexer（R-24）");
        services.Any(s => s.ServiceType == typeof(IFeishuTokenStoreFactory))
            .Should().BeTrue("必须注册 per-app 令牌存储工厂");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddFeishuRedisDeduplicators_Should_Honor_HealthCheck_Switch(bool registerHealthCheck)
    {
        var services = new ServiceCollection();

        services.AddFeishuRedisDeduplicators(_ => { }, registerHealthCheck);

        var healthCheckRegistered = services.Any(s =>
            s.ServiceType.FullName == "Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService");

        healthCheckRegistered.Should().Be(registerHealthCheck,
            registerHealthCheck
                ? "默认必须保留既有的健康检查自动注册行为（非破坏）"
                : "registerHealthCheck=false 时不得调用 AddHealthChecks()（避免对未使用健康检查的宿主隐式注册）");
    }

    #endregion

    #region 指标（R2-21）

    [Fact]
    public async Task RedisOperationMetric_Should_Be_Emitted_On_Successful_TryMark()
    {
        var measurements = new List<long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == FeishuMetrics.MeterName)
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name == "feishu.redis.operation")
                measurements.Add(value);
        });
        listener.Start();

        var (sut, _) = BuildEventDeduplicator();
        await sut.TryMarkAsProcessingAsync("evt1");

        measurements.Should().NotBeEmpty("Redis 操作必须产生 feishu.redis.operation 指标（否则失败分类不可观测）");
    }

    #endregion

    /// <summary>
    /// 把键集合包装为 <c>IAsyncEnumerable&lt;RedisKey&gt;</c>（KeysAsync 桩用）。
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

/// <summary>
/// 取消适配辅助（net6.0 无 <c>CancellationTokenSource.CancelAsync</c>）。
/// </summary>
internal static class CancellationTokenSourceCompatibilityExtensions
{
    /// <summary>取消令牌源（所有 TFM 可用）。</summary>
    public static Task CancelAsyncCompat(this CancellationTokenSource source)
    {
        source.Cancel();
        return Task.CompletedTask;
    }
}
