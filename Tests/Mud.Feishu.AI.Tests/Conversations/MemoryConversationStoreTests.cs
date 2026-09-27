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
}
