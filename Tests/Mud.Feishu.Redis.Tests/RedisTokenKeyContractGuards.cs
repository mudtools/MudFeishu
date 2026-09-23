// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;
using Xunit;

namespace Mud.Feishu.Redis.Tests;

/// <summary>
/// TMR2-P1-2 契约守卫：Redis 令牌键布局与 SCAN pattern 语义（守卫 7 / 7b / 7c）。
/// </summary>
/// <remarks>
/// <para>
/// 修复前的双重缺陷：
/// ① <c>PerAppRedisTokenStoreFactory.BuildKeyPrefix</c> 预转义一次（<c>:</c> → <c>\:</c>），
/// <c>TokenKeyBuilder</c> 又转义一次（<c>\</c> → <c>\\</c>）⇒ Memory 与 Redis 键布局不再逐字节一致（D8 失真）；
/// ② SCAN pattern 与键由同一份双重转义前缀产出，而 Redis glob 把 <c>\\</c> 解释为<b>单个</b>反斜杠
/// ⇒ pattern 与键互不匹配，<c>ClearAsync</c> / <c>GetTokenTypesAsync</c> / <c>ClearAllUsersAsync</c>
/// 在含 <c>:</c>/<c>\</c> 的 appKey 下<b>永不命中</b>（凭据变更清库 D10 静默失效）。
/// </para>
/// <para>
/// 三条守卫在当前（修复前）代码上均会失败——必须与修复同一提交落地。
/// </para>
/// </remarks>
public class RedisTokenKeyContractGuards
{
    /// <summary>
    /// 含转义字符、通配元字符与普通取值的 appKey 覆盖集。
    /// <c>FeishuAppConfig.Validate()</c> 不限制 AppKey 字符集（仅要求非空），
    /// 既有契约守卫自身即以 <c>app:with:colon</c> 为合法值。
    /// </summary>
    public static IEnumerable<object[]> AppKeyCases()
    {
        yield return new object[] { "hr-app" };
        yield return new object[] { "app:with:colon" };
        yield return new object[] { @"a\b" };
        yield return new object[] { "a*b" };
        yield return new object[] { "a?b" };
        yield return new object[] { "a[0]b" };
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 7：转义唯一归口——BuildKeyPrefix 不得预转义
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 守卫 7：Redis 键前缀必须是<b>未转义</b>的 <c>feishu:{appKey}:token</c>，
    /// 转义（<c>:</c> → <c>\:</c>、<c>\</c> → <c>\\</c>）唯一归口 <c>TokenKeyBuilder</c>。
    /// </summary>
    /// <remarks>修复前经 <c>RedisKeyBuilder.Combine</c> 产出 <c>feishu:app\:with\:colon:token</c>，本守卫失败。</remarks>
    [Theory]
    [MemberData(nameof(AppKeyCases))]
    public void BuildKeyPrefix_ShouldNotPreEscape_ForKeySegmentsEscapedByTokenKeyBuilder(string appKey)
    {
        PerAppRedisTokenStoreFactory.BuildKeyPrefix(appKey).Should().Be(
            $"feishu:{appKey}:token",
            "前缀必须与 Memory 路径同源（未转义）；转义由 TokenKeyBuilder 单点负责（TMR2-P1-2）");
    }

    /// <summary>
    /// 守卫 7（续）：完整键只允许出现<b>单层</b>转义——tokenType 中的 <c>:</c> 写作 <c>\:</c>，
    /// 不得出现 <c>\\:</c>（二次转义的特征）。
    /// </summary>
    [Fact]
    public void TenantAccessKey_ShouldEscapeExactlyOnce_ForAppKeyWithColon()
    {
        var prefix = PerAppRedisTokenStoreFactory.BuildKeyPrefix("app:with:colon");

        TokenKeyBuilder.TenantAccessKey(prefix, "tenant:app:with:colon").Should().Be(
            @"feishu:app:with:colon:token:tenant\:app\:with\:colon:access");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 7b：Memory 与 Redis 键布局逐字节一致（D8）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 守卫 7b：同一 appKey 下，Redis 实际写入的键必须能被 Memory 后端（真实 <see cref="MemoryCache"/>）
    /// 命中——以「真实内存缓存探针」验证两后端布局逐字节一致。
    /// </summary>
    /// <remarks>
    /// 修复前 Redis 侧键含 <c>\\:</c> 而 Memory 侧为 <c>:</c>，探针必然落空。
    /// </remarks>
    [Theory]
    [MemberData(nameof(AppKeyCases))]
    public async Task Memory_And_Redis_KeyLayout_ShouldBeByteIdentical(string appKey)
    {
        const string tokenType = "tenant:cli_x";

        // Arrange：Memory 后端用真实 IMemoryCache 写入
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var memoryStore = new FeishuTokenStore(cache, appKey);
        await memoryStore.SetAccessTokenAsync(tokenType, "memory-value", 3600);

        // Arrange：Redis 后端——捕获其读写所用的物理键（读/写共用 BuildAccessTokenKey）
        var redisKeys = new List<string>();
        var (factory, _) = BuildFactory(keyCapture: redisKeys);
        var (redisStore, _) = factory.Create(appKey);
        await redisStore.GetAccessTokenAsync(tokenType);

        // Assert：Redis 侧物理键必须能命中 Memory 侧写入的键（逐字节一致，D8）
        redisKeys.Should().ContainSingle();
        cache.TryGetValue(redisKeys[0], out var hit).Should().BeTrue(
            $"Redis 物理键 '{redisKeys[0]}' 必须与 Memory 键布局逐字节一致（D8：键构造单一真相）");
        hit.Should().Be("memory-value");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 7c：SCAN pattern 必须命中由同一前缀构建的键
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 守卫 7c：<c>ClearAsync</c> / <c>GetTokenTypesAsync</c> 交给 Redis 的 SCAN pattern
    /// 必须能命中由同一存储实例写入的键（以 Redis <c>stringmatchlen</c> 语义的匹配器断言）。
    /// </summary>
    /// <remarks>
    /// 修复前 pattern 中的 <c>\\</c> 要求键上只有单个反斜杠，而键上是两个 ⇒ 永不命中（D10 清库静默失效）。
    /// </remarks>
    [Theory]
    [MemberData(nameof(AppKeyCases))]
    public async Task ScanPattern_ShouldMatchKeysWrittenBySameStore(string appKey)
    {
        const string tokenType = "tenant:cli_x";

        var observedPatterns = new List<string>();
        var redisKeys = new List<string>();
        var (factory, _) = BuildFactory(observedPatterns: observedPatterns, keyCapture: redisKeys);

        var (store, _) = factory.Create(appKey);
        await store.GetAccessTokenAsync(tokenType);

        // 触发 SCAN（server.Keys 的 pattern 被捕获；返回空集不影响断言）
        await store.ClearAsync();

        redisKeys.Should().ContainSingle();
        observedPatterns.Should().ContainSingle();

        RedisGlobMatcher.IsMatch(observedPatterns[0], redisKeys[0]).Should().BeTrue(
            $"SCAN pattern '{observedPatterns[0]}' 必须命中键 '{redisKeys[0]}'（否则清库/枚举静默失效）");
    }

    // ────────────────────────────────────────────────────────────────────
    // 测试夹具
    // ────────────────────────────────────────────────────────────────────

    private static (PerAppRedisTokenStoreFactory Factory, List<string> ObservedKeys) BuildFactory(
        List<string>? observedPatterns = null,
        List<string>? keyCapture = null,
        IEnumerable<string>? serverKeys = null)
    {
        var observedReadKeys = new List<string>();

        var db = new Mock<IDatabase>();
        db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey, CommandFlags>((key, _) =>
            {
                observedReadKeys.Add(key.ToString());
                keyCapture?.Add(key.ToString());
            })
            .ReturnsAsync(RedisValue.Null);

        db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1L);

        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        var server = new Mock<IServer>();
        server.Setup(s => s.IsConnected).Returns(true);
        server.Setup(s => s.IsReplica).Returns(false);
        // R2-08（合并对齐）：令牌 SCAN 已由同步 Keys(...) 改为异步 KeysAsync(...)——
        // IServer.KeysAsync 只有 (int database, RedisValue pattern, int pageSize,
        // long cursor, int pageOffset, CommandFlags flags) 一个重载，必须按该签名打桩；
        // 未打桩时 Moq 返回 null，`await foreach` 会抛 NullReferenceException（假红）。
        server.Setup(s => s.KeysAsync(
                It.IsAny<int>(),
                It.IsAny<RedisValue>(),
                It.IsAny<int>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CommandFlags>()))
            .Callback<int, RedisValue, int, long, int, CommandFlags>(
                (_, pattern, _, _, _, _) => observedPatterns?.Add(pattern.ToString()))
            .Returns(() => ToAsyncKeys((serverKeys ?? Enumerable.Empty<string>()).Select(k => (RedisKey)k)));

        var endpoint = new System.Net.DnsEndPoint("localhost", 6379);
        redis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns(new System.Net.EndPoint[] { endpoint });
        redis.Setup(r => r.GetServer(It.IsAny<System.Net.EndPoint>(), It.IsAny<object>())).Returns(server.Object);

        return (new PerAppRedisTokenStoreFactory(redis.Object), observedReadKeys);
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
