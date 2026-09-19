// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Redis.Configuration;
using Mud.Feishu.Redis.Extensions;
using StackExchange.Redis;


#pragma warning disable CS0618 // R5/X6: tests reference Obsolete dual-read fallback base
namespace Mud.Feishu.Redis.Tests.Configuration;

/// <summary>
/// R5.2–R5.3 / X6 + X13：去重旧键（<c>FeishuRedis:Event* / Nonce* / SeqId*</c>）的双读语义。
/// </summary>
/// <remarks>
/// <para>
/// 旧键在统一节缺失时是<b>真正生效</b>的回落基座（因此 R5 改判为「不加 <c>[Obsolete]</c>，
/// 以运行时精确告警替代编译期警告」）。本测试锁定三条不变量：
/// </para>
/// <list type="number">
/// <item>旧键仍可从配置绑定（Obsolete/收口不得切断 appsettings 兼容）；</item>
/// <item>统一节存在时按「字段级优先」覆盖旧键（有效值以统一节为准）；</item>
/// <item>旧键被统一节覆盖时输出精确 <c>Warning</c>（情形 B）——此前完全静默。</item>
/// </list>
/// </remarks>
public class DeduplicationLegacyKeyTests
{
    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }

    // ────────────────────────────────────────────────────────────────────
    // 1) 旧键仍可绑定
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void LegacyRedisDeduplicationKeys_ShouldStillBind_FromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuRedis:Connection:ServerAddress"] = "localhost:6379",
                ["FeishuRedis:EventCacheExpiration"] = "01:00:00",
                ["FeishuRedis:EventKeyPrefix"] = "tenant-a:feishu:event:",
                ["FeishuRedis:NonceTtl"] = "00:10:00",
                ["FeishuRedis:SeqIdCacheExpiration"] = "02:00:00"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuRedisDeduplicators(configuration);

        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<RedisOptions>();
        options.EventCacheExpiration.Should().Be(TimeSpan.FromHours(1));
        options.EventKeyPrefix.Should().Be("tenant-a:feishu:event:");
        options.NonceTtl.Should().Be(TimeSpan.FromMinutes(10));
        options.SeqIdCacheExpiration.Should().Be(TimeSpan.FromHours(2));
    }

    // ────────────────────────────────────────────────────────────────────
    // 2) 统一节字段级优先（有效值以 FeishuDeduplication 为准）
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UnifiedEventTtlAndPrefix_ShouldOverrideLegacyKeys()
    {
        RedisKey capturedKey = default;
        var databaseMock = new Mock<IDatabase>();
        databaseMock
            .Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Callback<string, RedisKey[], RedisValue[], CommandFlags>((_, keys, _, _) => capturedKey = keys[0])
            .ReturnsAsync(RedisResult.Create(0L));

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(databaseMock.Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuRedis:Connection:ServerAddress"] = "localhost:6379",
                ["FeishuRedis:EventKeyPrefix"] = "legacy:event:"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(l => l.AddProvider(NullLoggerProvider.Instance));
        services.AddFeishuRedisDeduplicators(configuration);

        services.RemoveAll<IOptions<FeishuDeduplicationOptions>>();
        services.AddSingleton(Options.Create(new FeishuDeduplicationOptions
        {
            IsConfiguredFromConfiguration = true,
            Event = new DeduplicationEntryOptions
            {
                Ttl = TimeSpan.FromMinutes(30),
                KeyPrefix = "unified:event:"
            }
        }));

        services.RemoveAll<IConnectionMultiplexer>();
        services.AddSingleton(redisMock.Object);

        // 去重器仅实现 IAsyncDisposable，容器须异步释放
        await using var provider = services.BuildServiceProvider();
        var deduplicator = provider.GetRequiredService<IFeishuEventDeduplicator>();

        await deduplicator.TryMarkAsProcessingAsync("evt-1", "app-1");

        capturedKey.ToString().Should().StartWith("unified:event:",
            "统一节存在时按字段级优先：旧键 FeishuRedis:EventKeyPrefix 不再生效（X6/X13）");
    }

    // ────────────────────────────────────────────────────────────────────
    // 3) 情形 B 告警：统一节覆盖显式配置的旧键
    // ────────────────────────────────────────────────────────────────────

    private static FeishuDeduplicationOptions UnifiedWith(TimeSpan? eventTtl, string? eventPrefix) => new()
    {
        IsConfiguredFromConfiguration = true,
        Event = new DeduplicationEntryOptions
        {
            Ttl = eventTtl,
            KeyPrefix = eventPrefix
        }
    };

    [Fact]
    public void Warn_ShouldFire_WhenUnifiedTtlOverridesExplicitLegacyTtl()
    {
        var logger = new CapturingLogger();
        // 48h 恰为 Consts 默认值（默认值不算「显式配置」），故用 2h 体现显式旧键
        var redisOptions = new RedisOptions { EventCacheExpiration = TimeSpan.FromHours(2) };

        RedisFeishuServiceBuilderExtensions.WarnIfDeduplicationKeysAreIneffective(
            logger,
            redisOptions,
            new DeduplicationOptions(),
            UnifiedWith(TimeSpan.FromMinutes(30), null));

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Warning
            && e.Message.Contains("FeishuRedis:EventCacheExpiration")
            && e.Message.Contains("已被 FeishuDeduplication:Event:Ttl"),
            "统一节生效时显式配置的旧键被字段级覆盖，必须告警指明覆盖关系（R5.2 情形 B）");
    }

    [Fact]
    public void Warn_ShouldFire_WhenUnifiedPrefixOverridesExplicitLegacyPrefix()
    {
        var logger = new CapturingLogger();
        var redisOptions = new RedisOptions { EventKeyPrefix = "legacy:event:" };

        RedisFeishuServiceBuilderExtensions.WarnIfDeduplicationKeysAreIneffective(
            logger,
            redisOptions,
            new DeduplicationOptions(),
            UnifiedWith(null, "unified:event:"));

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Warning
            && e.Message.Contains("FeishuRedis:EventKeyPrefix")
            && e.Message.Contains("已被 FeishuDeduplication:Event:KeyPrefix"),
            "旧前缀被统一节覆盖时同样必须告警");
    }

    [Fact]
    public void Warn_ShouldNotFire_WhenUnifiedActive_ButLegacyKeysAreAtDefaults()
    {
        var logger = new CapturingLogger();

        RedisFeishuServiceBuilderExtensions.WarnIfDeduplicationKeysAreIneffective(
            logger,
            new RedisOptions(),
            new DeduplicationOptions(),
            UnifiedWith(TimeSpan.FromMinutes(30), "unified:event:"));

        logger.Entries.Should().BeEmpty("旧键保持默认时没有「被覆盖」的事实，告警只会制造噪音");
    }

    [Fact]
    public void Warn_ShouldNotFire_WhenUnifiedActive_ButUnifiedFieldAbsent()
    {
        var logger = new CapturingLogger();
        var redisOptions = new RedisOptions { EventCacheExpiration = TimeSpan.FromHours(2) };

        // 统一节存在但未提供 Event.Ttl → 旧键仍是实际生效值，不算「无效」
        RedisFeishuServiceBuilderExtensions.WarnIfDeduplicationKeysAreIneffective(
            logger,
            redisOptions,
            new DeduplicationOptions(),
            UnifiedWith(null, null));

        logger.Entries.Should().BeEmpty(
            "统一节未提供该字段时旧键仍在生效，告警会误导用户（情形 B 的判定边界）");
    }
}
