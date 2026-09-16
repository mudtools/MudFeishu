// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  本项目基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Redis.IntegrationTests;

/// <summary>
/// SeqID 去重集成测试——覆盖 R-07（隔离）、R-08（Sorted Set 生命周期）、R-01（护栏）
/// </summary>
[Collection("Redis")]
public class SeqIDDeduplicatorIntegrationTests : RedisIntegrationTestBase
{
    private readonly RedisFeishuSeqIDDeduplicator _sut;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    public SeqIDDeduplicatorIntegrationTests(RedisFixture fixture) : base(fixture)
    {
        _sut = new RedisFeishuSeqIDDeduplicator(
            Fixture.Redis,
            NullLogger<RedisFeishuSeqIDDeduplicator>.Instance,
            DefaultTtl,
            "feishu:seqid:",
            "app1|host1");
    }

    [Fact]
    public async Task SeqID_SortedSet_Should_Have_Ttl_And_Bounded_Size()
    {
        if (ShouldSkip()) return;

        // Arrange
        var sortedSetKey = $"feishu:seqid:app1\\:host1:set";

        // Act：写入 10 个 SeqID
        for (ulong i = 1; i <= 10; i++)
        {
            await _sut.TryMarkAsProcessedAsync(i);
        }

        // Assert
        // Sorted Set 应有 TTL
        var ttl = await Fixture.Database.KeyTimeToLiveAsync(sortedSetKey);
        ttl.HasValue.Should().BeTrue();
        ttl!.Value.TotalSeconds.Should().BeGreaterThan(0);

        // Sorted Set 应有 10 个成员
        var count = (long)await Fixture.Database.SortedSetLengthAsync(sortedSetKey);
        count.Should().Be(10);
    }

    [Fact]
    public async Task SeqID_With_Different_ScopeKey_Should_Not_Dedup_Each_Other()
    {
        if (ShouldSkip()) return;

        // Arrange
        var sut1 = new RedisFeishuSeqIDDeduplicator(
            Fixture.Redis, NullLogger<RedisFeishuSeqIDDeduplicator>.Instance,
            DefaultTtl, "feishu:seqid:", "scopeA");
        var sut2 = new RedisFeishuSeqIDDeduplicator(
            Fixture.Redis, NullLogger<RedisFeishuSeqIDDeduplicator>.Instance,
            DefaultTtl, "feishu:seqid:", "scopeB");

        // Act & Assert：两个 scope 各自标记 seqId=1 均应返回"新消息"
        var r1 = await sut1.TryMarkAsProcessedAsync(1);
        var r2 = await sut2.TryMarkAsProcessedAsync(1);

        r1.Should().BeFalse("scopeA 的 seqId=1 是新消息");
        r2.Should().BeFalse("scopeB 的 seqId=1 是新消息");

        // 同 scope 二次标记应返回"已处理"
        var r1b = await sut1.TryMarkAsProcessedAsync(1);
        r1b.Should().BeTrue("scopeA 的 seqId=1 已处理过");
    }

    [Fact]
    public async Task SeqID_ClearCache_With_Default_Prefix_Should_Only_Delete_SeqId_Keys()
    {
        if (ShouldSkip()) return;

        // Arrange
        await Fixture.Database.StringSetAsync("feishu:nonce:testkey", "1");
        await Fixture.Database.StringSetAsync("other:key", "1");

        // Act
        await _sut.ClearCacheAsync();

        // Assert：非 SeqID 前缀的键不应被删除
        (await Fixture.Database.KeyExistsAsync("feishu:nonce:testkey")).Should().BeTrue();
        (await Fixture.Database.KeyExistsAsync("other:key")).Should().BeTrue();
    }

    [Fact]
    public void SeqID_Constructor_With_Empty_ScopeKey_Should_Throw()
    {
        if (ShouldSkip()) return;

        // Act
        var act = () => new RedisFeishuSeqIDDeduplicator(
            Fixture.Redis, NullLogger<RedisFeishuSeqIDDeduplicator>.Instance,
            DefaultTtl, "feishu:seqid:", "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*scopeKey*");
    }

    [Fact]
    public async Task SeqID_GetMaxProcessedSeqId_Should_Return_Window_Max()
    {
        if (ShouldSkip()) return;

        // Arrange
        for (ulong i = 1; i <= 5; i++)
        {
            await _sut.TryMarkAsProcessedAsync(i);
        }

        // Act
        var max = _sut.GetMaxProcessedSeqId();

        // Assert
        max.Should().Be(5);
    }
}
