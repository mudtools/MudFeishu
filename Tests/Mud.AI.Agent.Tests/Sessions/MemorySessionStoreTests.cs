// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tests.Sessions;

/// <summary>
/// 中立内存会话存储（<see cref="MemorySessionStore"/>）的不变量用例。
/// </summary>
/// <remarks>
/// N2-g：随类型自产品侧迁入（Q1「测试随类型走」）。覆盖三件事——TTL 软过期、写入侧惰性清扫的
/// 节流与单次上界（R2-5/R3-06 的语义，不清扫会让未读键永久驻留）、以及默认 TTL 的中立性
/// （产品必须显式注入自己的阈值源，默认值仅供中立侧独立使用）。
/// </remarks>
public class MemorySessionStoreTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenKeyMissing()
    {
        var store = new MemorySessionStore();

        (await store.GetAsync("nope")).Should().BeNull("miss 必须返回 null（调用方据此重建会话）");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnPayload_WithinTtl()
    {
        var now = DateTimeOffset.UnixEpoch;
        var store = new MemorySessionStore(TimeSpan.FromSeconds(10), () => now);

        await store.SaveAsync("k", "{\"v\":1}");

        (await store.GetAsync("k")).Should().Be("{\"v\":1}");
    }

    [Fact]
    public async Task GetAsync_ShouldTreatExpiredEntryAsMiss()
    {
        var now = DateTimeOffset.UnixEpoch;
        var store = new MemorySessionStore(TimeSpan.FromSeconds(10), () => now);
        await store.SaveAsync("k", "{\"v\":1}");

        now += TimeSpan.FromSeconds(11);

        (await store.GetAsync("k")).Should().BeNull("软过期：过期即视为 miss（与 Redis 读侧判定同源）");
    }

    [Fact]
    public async Task SaveAsync_ShouldRejectEmptyPayload()
    {
        var store = new MemorySessionStore();

        var act = async () => await store.SaveAsync("k", string.Empty);

        await act.Should().ThrowAsync<ArgumentException>("空载荷写入会让后续反序列化必然失败（早失败优于晚失败）");
    }

    [Fact]
    public void Constructor_ShouldRejectNonPositiveTtl()
    {
        var act = () => new MemorySessionStore(TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldBeIdempotent()
    {
        var store = new MemorySessionStore();
        await store.SaveAsync("k", "{\"v\":1}");

        await store.DeleteAsync("k");
        await store.DeleteAsync("k");

        (await store.GetAsync("k")).Should().BeNull();
        store.Count.Should().Be(0);
    }

    /// <summary>
    /// 未读键必须能被回收（R2-5）：写入路径按阈值分摊触发清扫——这是「未读键永不回收」的唯一解。
    /// </summary>
    [Fact]
    public async Task SaveAsync_ShouldSweepExpiredEntries_ThatAreNeverRead()
    {
        var now = DateTimeOffset.UnixEpoch;
        var store = new MemorySessionStore(TimeSpan.FromSeconds(10), () => now);

        for (var i = 0; i < MemorySessionStore.SweepThreshold; i++)
        {
            await store.SaveAsync($"k{i}", "{\"v\":1}");
        }

        store.Count.Should().Be(MemorySessionStore.SweepThreshold);
        now += TimeSpan.FromSeconds(11);

        // 阈值已达成，且写入次数达到节流窗口 ⇒ 下一次写入触发一次清扫（单次回收上限 SweepMaxRemoval）。
        for (var i = 0; i < MemorySessionStore.SweepEveryWrites; i++)
        {
            await store.SaveAsync($"trigger{i}", "{\"v\":1}");
        }

        store.SweepCallCount.Should().BeGreaterThan(0, "写入路径必须真的触发过清扫（否则未读键永不释放）");
        store.Count.Should().BeLessThan(MemorySessionStore.SweepThreshold,
            "过期条目必须被回收（单次上限由 SweepMaxRemoval 约束）");
        (await store.GetAsync("k0")).Should().BeNull();
    }

    /// <summary>未达阈值时不得扫（均摊策略：不把 O(n) 成本摊到每次写入）。</summary>
    [Fact]
    public async Task SaveAsync_ShouldNotSweep_BelowThreshold()
    {
        var now = DateTimeOffset.UnixEpoch;
        var store = new MemorySessionStore(TimeSpan.FromSeconds(10), () => now);

        await store.SaveAsync("k0", "{\"v\":1}");
        await store.SaveAsync("k1", "{\"v\":1}");

        store.SweepCallCount.Should().Be(0);
        store.Count.Should().Be(2);
    }

    /// <summary>中立默认 TTL 只服务"中立侧独立使用"——产品必须显式注入自己的阈值源（①式）。</summary>
    [Fact]
    public void DefaultTtl_ShouldBeTheNeutralFallback()
        => MemorySessionStore.DefaultTtl.Should().Be(TimeSpan.FromHours(24));
}
