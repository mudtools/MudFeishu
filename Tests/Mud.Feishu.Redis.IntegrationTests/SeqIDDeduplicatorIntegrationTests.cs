// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Redis.IntegrationTests;

/// <summary>
/// SeqID 去重集成测试——覆盖 R-07（隔离）、ADR-10（容量窗口，R2-01）、
/// R2-02（清理模式与键同源）与 R-01 护栏。
/// </summary>
/// <remarks>
/// 键与模式一律由 <see cref="RedisKeyBuilder"/> 产出：历史用例写死
/// <c>feishu:seqid:app1\:host1:set</c>（单冒号 + 多余转义），与实际键
/// <c>feishu:seqid::app1|host1:set</c>（双冒号）不符，属潜伏漂移。
/// </remarks>
[Collection("Redis")]
public class SeqIDDeduplicatorIntegrationTests : RedisIntegrationTestBase
{
    private const string KeyPrefix = "feishu:seqid:";
    private const string ScopeKey = "app1|host1";
    private const string OtherScopeKey = "app2|host1";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    private readonly RedisFeishuSeqIDDeduplicator _sut;

    /// <summary>
    /// 初始化测试。
    /// </summary>
    /// <param name="fixture">共享 Redis 夹具。</param>
    public SeqIDDeduplicatorIntegrationTests(RedisFixture fixture) : base(fixture)
    {
        _sut = new RedisFeishuSeqIDDeduplicator(
            Fixture.Redis,
            NullLogger<RedisFeishuSeqIDDeduplicator>.Instance,
            DefaultTtl,
            KeyPrefix,
            ScopeKey);
    }

    private static string SeqIdKey(string scopeKey, ulong seqId)
        => RedisKeyBuilder.Combine(KeyPrefix, scopeKey, seqId.ToString());

    private static string SortedSetKey(string scopeKey)
        => RedisKeyBuilder.Combine(KeyPrefix, scopeKey, "set");

    private RedisFeishuSeqIDDeduplicator CreateSut(string scopeKey, int capacity)
        => new(Fixture.Redis,
            NullLogger<RedisFeishuSeqIDDeduplicator>.Instance,
            DefaultTtl,
            KeyPrefix,
            scopeKey,
            capacity);

    [RedisFact]
    public async Task SeqID_SortedSet_Should_Have_Ttl_And_Bounded_Size()
    {
        // Arrange：键由构造器产出，并同时锁定金标准布局（ADR-11）
        var sortedSetKey = SortedSetKey(ScopeKey);
        sortedSetKey.Should().Be("feishu:seqid::app1|host1:set", "键布局已被 ADR-11 锁定");

        // Act：写入 10 个 SeqID（默认容量 100000 不会触发裁剪）
        for (ulong i = 1; i <= 10; i++)
        {
            await _sut.TryMarkAsProcessedAsync(i);
        }

        // Assert
        var ttl = await Fixture.Database.KeyTimeToLiveAsync(sortedSetKey);
        ttl.HasValue.Should().BeTrue();
        ttl!.Value.TotalSeconds.Should().BeGreaterThan(0);

        (await Fixture.Database.SortedSetLengthAsync(sortedSetKey)).Should().Be(10);
        _sut.GetCacheCount().Should().Be(10);
    }

    [RedisFact]
    public async Task SortedSet_After_2N_Writes_Should_Keep_Capacity_And_Ttl()
    {
        // I-1（R2-01 回归）：容量窗口语义——写 2N 条后仅保留分数最大的 N 条。
        const int capacity = 5;
        var sut = CreateSut(ScopeKey, capacity);
        var sortedSetKey = SortedSetKey(ScopeKey);

        for (ulong i = 1; i <= 10; i++)
        {
            await sut.TryMarkAsProcessedAsync(i);
        }

        (await Fixture.Database.SortedSetLengthAsync(sortedSetKey)).Should().Be(capacity);

        var ttl = await Fixture.Database.KeyTimeToLiveAsync(sortedSetKey);
        ttl.HasValue.Should().BeTrue();
        ttl!.Value.TotalSeconds.Should().BeGreaterThan(0);

        var members = await Fixture.Database.SortedSetRangeByRankAsync(sortedSetKey);
        members.Select(m => m.ToString()).Should().BeEquivalentTo(new[] { "6", "7", "8", "9", "10" });

        // 历史缺陷（时间阈值比较 SeqID 分数）下该值恒为 0
        sut.GetCacheCount().Should().Be(capacity);
    }

    [RedisFact]
    public async Task GetMaxProcessedSeqId_Should_Return_True_Max_When_Trimmed()
    {
        // I-2：乱序写入下最大值仍为真实最大值
        var sut = CreateSut(ScopeKey, 5);

        await sut.TryMarkAsProcessedAsync(5);
        await sut.TryMarkAsProcessedAsync(100);
        await sut.TryMarkAsProcessedAsync(99);

        sut.GetMaxProcessedSeqId().Should().Be(100UL);
    }

    [RedisFact]
    public async Task ClearCacheAsync_Should_Delete_Own_ScopeKeys_And_Keep_Others()
    {
        // I-3（R2-02 回归）：正向断言"目标键确实被删"，同时保证不越界删除。
        var other = CreateSut(OtherScopeKey, 100000);

        await _sut.TryMarkAsProcessedAsync(1);
        await other.TryMarkAsProcessedAsync(1);

        var ownStringKey = SeqIdKey(ScopeKey, 1);
        var ownSetKey = SortedSetKey(ScopeKey);
        var otherStringKey = SeqIdKey(OtherScopeKey, 1);
        var otherSetKey = SortedSetKey(OtherScopeKey);

        (await Fixture.Database.KeyExistsAsync(ownStringKey)).Should().BeTrue();
        (await Fixture.Database.KeyExistsAsync(ownSetKey)).Should().BeTrue();

        await _sut.ClearCacheAsync();

        (await Fixture.Database.KeyExistsAsync(ownStringKey)).Should().BeFalse("本 scopeKey 的 String 键必须被删除");
        (await Fixture.Database.KeyExistsAsync(ownSetKey)).Should().BeFalse("本 scopeKey 的 Sorted Set 键必须被删除");
        (await Fixture.Database.KeyExistsAsync(otherStringKey)).Should().BeTrue("其它 scopeKey 的键不得被删除");
        (await Fixture.Database.KeyExistsAsync(otherSetKey)).Should().BeTrue("其它 scopeKey 的键不得被删除");
    }

    [RedisFact]
    public async Task SeqID_ClearCache_With_Default_Prefix_Should_Only_Delete_SeqId_Keys()
    {
        // R2-02：在 I-3 的正向断言之外，额外确认不会误删非 SeqID 前缀的键
        await _sut.TryMarkAsProcessedAsync(1);
        await Fixture.Database.StringSetAsync("feishu:nonce:testkey", "1");
        await Fixture.Database.StringSetAsync("other:key", "1");

        var stringKey = SeqIdKey(ScopeKey, 1);
        var setKey = SortedSetKey(ScopeKey);

        await _sut.ClearCacheAsync();

        (await Fixture.Database.KeyExistsAsync(stringKey)).Should().BeFalse();
        (await Fixture.Database.KeyExistsAsync(setKey)).Should().BeFalse();
        (await Fixture.Database.KeyExistsAsync("feishu:nonce:testkey")).Should().BeTrue();
        (await Fixture.Database.KeyExistsAsync("other:key")).Should().BeTrue();
    }

    [RedisFact]
    public async Task SeqID_With_Different_ScopeKey_Should_Not_Dedup_Each_Other()
    {
        // R-07：不同 scopeKey 不互相判重
        var sut1 = CreateSut("scopeA", 100000);
        var sut2 = CreateSut("scopeB", 100000);

        var r1 = await sut1.TryMarkAsProcessedAsync(1);
        var r2 = await sut2.TryMarkAsProcessedAsync(1);

        r1.Should().BeFalse("scopeA 的 seqId=1 是新消息");
        r2.Should().BeFalse("scopeB 的 seqId=1 是新消息");

        var r1b = await sut1.TryMarkAsProcessedAsync(1);
        r1b.Should().BeTrue("scopeA 的 seqId=1 已处理过");
    }

    [RedisFact]
    public void SeqID_Constructor_With_Empty_ScopeKey_Should_Throw()
    {
        var act = () => new RedisFeishuSeqIDDeduplicator(
            Fixture.Redis, NullLogger<RedisFeishuSeqIDDeduplicator>.Instance,
            DefaultTtl, KeyPrefix, "");

        act.Should().Throw<ArgumentException>().WithMessage("*scopeKey*");
    }

    [RedisFact]
    public async Task SeqID_GetMaxProcessedSeqId_Should_Return_Window_Max()
    {
        for (ulong i = 1; i <= 5; i++)
        {
            await _sut.TryMarkAsProcessedAsync(i);
        }

        _sut.GetMaxProcessedSeqId().Should().Be(5UL);
    }
}
