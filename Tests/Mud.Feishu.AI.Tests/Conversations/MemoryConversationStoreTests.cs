// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tests.Conversations;

/// <summary>
/// <see cref="MemoryConversationStore"/>：存取回灌、TTL 软过期、删除（Phase 0 §5 DoD）。
/// </summary>
public class MemoryConversationStoreTests
{
    private static DateTimeOffset Clock { get; } = DateTimeOffset.UnixEpoch;

    [Fact]
    public async Task SaveGet_ShouldRoundTrip()
    {
        var store = new MemoryConversationStore(ttl: TimeSpan.FromHours(1));
        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_1");

        await store.SaveAsync(key, "{\"s\":1}");
        var loaded = await store.GetAsync(key);

        loaded.Should().Be("{\"s\":1}");
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenMissing()
    {
        var store = new MemoryConversationStore();
        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_missing");

        (await store.GetAsync(key)).Should().BeNull();
    }

    [Fact]
    public async Task Get_ShouldSoftExpire_WhenTtlElapsed()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new MemoryConversationStore(ttl: TimeSpan.FromSeconds(10), utcNow: () => now);
        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_1");

        await store.SaveAsync(key, "{\"s\":1}");

        // TTL 内可读。
        (await store.GetAsync(key)).Should().NotBeNull();

        // 越过 TTL：软过期 → miss（读侧时间戳判定，TMA-15 同源语义）。
        now += TimeSpan.FromSeconds(11);
        (await store.GetAsync(key)).Should().BeNull();

        // 过期后重新保存：TTL 刷新，重新可读。
        await store.SaveAsync(key, "{\"s\":2}");
        (await store.GetAsync(key)).Should().Be("{\"s\":2}");
    }

    [Fact]
    public async Task Save_ShouldRefreshTtl()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new MemoryConversationStore(ttl: TimeSpan.FromSeconds(10), utcNow: () => now);
        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_1");

        await store.SaveAsync(key, "{\"v\":1}");
        now += TimeSpan.FromSeconds(6);
        await store.SaveAsync(key, "{\"v\":2}");

        now += TimeSpan.FromSeconds(6); // 距第一次保存 12s、距第二次 6s
        (await store.GetAsync(key)).Should().Be("{\"v\":2}", "保存必须刷新 TTL");
    }

    [Fact]
    public async Task Delete_ShouldRemoveEntry()
    {
        var store = new MemoryConversationStore();
        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_1");

        await store.SaveAsync(key, "{\"s\":1}");
        await store.DeleteAsync(key);

        (await store.GetAsync(key)).Should().BeNull();
        // 重复删除静默成功。
        await store.DeleteAsync(key);
    }

    [Fact]
    public void Ctor_ShouldRejectNonPositiveTtl()
    {
        var act = () => new MemoryConversationStore(ttl: TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Save_ShouldRejectEmptyPayload()
    {
        var store = new MemoryConversationStore();
        var act = () => store.SaveAsync("feishu:app-a:conversation:chat:oc_1", "");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ===== R2-5：未再被读取的过期键必须被惰性分摊清扫回收 =====

    [Fact]
    public async Task ExpiredEntries_ShouldBeReclaimed_AfterThreshold()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new MemoryConversationStore(ttl: TimeSpan.FromSeconds(10), utcNow: () => now);

        // 写入到阈值：全部条目随后过期，且**不再被读取**（读侧软过期永远够不到它们）。
        for (var i = 0; i < MemoryConversationStore.SweepThreshold; i++)
        {
            await store.SaveAsync($"k{i}", "{\"s\":1}");
        }

        store.Count.Should().Be(MemoryConversationStore.SweepThreshold);

        now += TimeSpan.FromSeconds(11);

        // R3-06：节流触发——需写入 SweepEveryWrites 次才触发一次清扫。
        // 且 SweepMaxRemoval 限制单次回收 256 条，需多轮清扫才能全量回收。
        var remaining = MemoryConversationStore.SweepThreshold;
        while (remaining > 0)
        {
            for (var i = 0; i < MemoryConversationStore.SweepEveryWrites; i++)
            {
                await store.SaveAsync($"trigger_{remaining}_{i}", "{\"s\":1}");
            }
            remaining -= MemoryConversationStore.SweepMaxRemoval;
        }

        // 全部过期条目已回收，仅剩新鲜 trigger 条目。
        store.Count.Should().BeLessThanOrEqualTo(MemoryConversationStore.SweepThreshold,
            "达到清扫阈值并经过节流写入后，过期条目必须被回收（否则未读键永不释放）");
        (await store.GetAsync("k0")).Should().BeNull();
    }

    [Fact]
    public async Task SweepExpired_ShouldNotRemoveFreshEntries()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new MemoryConversationStore(ttl: TimeSpan.FromSeconds(10), utcNow: () => now);

        for (var i = 0; i < MemoryConversationStore.SweepThreshold; i++)
        {
            await store.SaveAsync($"k{i}", "{\"s\":1}");
        }

        // 触发清扫但条目仍新鲜：一个都不许删。
        await store.SaveAsync("trigger", "{\"s\":1}");

        store.Count.Should().Be(MemoryConversationStore.SweepThreshold + 1, "未过期条目不得被清扫误删");
        (await store.GetAsync("k0")).Should().Be("{\"s\":1}");
    }

    [Fact]
    public async Task SweepExpired_ShouldNotRemoveSameKeyNewerValue()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new MemoryConversationStore(ttl: TimeSpan.FromSeconds(10), utcNow: () => now);

        // 构造「同键旧值已过期、新值新鲜」：清扫期间的同键覆盖写入不得被旧 KVP 值匹配误删。
        await store.SaveAsync("dup", "{\"v\":1}");
        for (var i = 0; i < MemoryConversationStore.SweepThreshold - 1; i++)
        {
            await store.SaveAsync($"k{i}", "{\"s\":1}");
        }

        now += TimeSpan.FromSeconds(11);

        // R3-06：节流触发——需写入 SweepEveryWrites 次才触发清扫。
        for (var i = 0; i < MemoryConversationStore.SweepEveryWrites; i++)
        {
            await store.SaveAsync($"trigger_{i}", "{\"s\":1}");
        }
        await store.SaveAsync("dup", "{\"v\":2}");

        (await store.GetAsync("dup")).Should().Be("{\"v\":2}", "同键的新值不得被旧条目的值匹配删除误伤");
    }

    [Fact]
    public async Task SweepExpired_ShouldNotRun_BelowThreshold()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new MemoryConversationStore(ttl: TimeSpan.FromSeconds(10), utcNow: () => now);

        await store.SaveAsync("k0", "{\"s\":1}");
        now += TimeSpan.FromSeconds(11);
        await store.SaveAsync("k1", "{\"s\":1}");

        store.Count.Should().Be(2, "未达阈值不触发清扫（均摊策略：不把 O(n) 成本摊到每次写入）");
    }
}
