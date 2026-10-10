// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tests.Agents;

/// <summary>
/// R7 / C4a：待确认快照（HITL 基础设施）——<b>过期语义</b>、<b>幂等消费</b>、
/// <b>租户隔离</b>与<b>凭据零落库</b>的行为断言。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么用确定性时钟</b>：过期是本项的核心语义，而"等到 TTL 结束"的墙钟断言在并行负载下必然抖动
/// （历史教训：删除"首次调用耗时 &lt; 800ms"类断言）。故时钟由构造器注入。
/// </para>
/// <para>
/// <b>为什么单列"凭据零落库"用例</b>：快照会被宿主持久化到库/缓存，一旦有人顺手把令牌或原始入参
/// 加进快照，凭据就离开了进程——这是"批准只能来自宿主、SDK 不签发凭据"这条铁律的<b>结构性</b>防线。
/// </para>
/// </remarks>
public class PendingApprovalStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private static PendingApprovalSnapshot Snapshot(
        string requestId = "ficc_call_1",
        string appKey = "cli_app_a",
        DateTimeOffset? createdAt = null,
        TimeSpan? ttl = null)
    {
        var created = createdAt ?? Now;
        return new PendingApprovalSnapshot(
            RequestId: requestId,
            ToolName: "im.send_message",
            AppKey: appKey,
            UserId: "ou_user",
            ConversationKey: "feishu.agent.cli_app_a.p2p.ou_user",
            ChatId: "oc_chat",
            ToolCallId: "call_1",
            ArgumentsDigest: "receive_id=<masked>;text=<len=12>",
            RequiredScopes: ["im:message:send_as_bot"],
            CreatedAt: created,
            ExpiresAt: created + (ttl ?? PendingApprovalSnapshot.DefaultTtl));
    }

    private static InMemoryPendingApprovalStore Store(DateTimeOffset now)
    {
        var current = now;
        return new InMemoryPendingApprovalStore(() => current);
    }

    /// <summary>
    /// 用真实时钟的存储：接线用例（<c>NotifyAsync</c> 内部以 <c>UtcNow</c> 建快照）必须与之一致——
    /// 否则固定时钟（用例的"未来/过去"基准）会把刚登记的快照判成过期，用例变成假红。
    /// </summary>
    private static InMemoryPendingApprovalStore RealClockStore() => new();

    private static Task<bool> ConsumeAsync(InMemoryPendingApprovalStore store, string appKey = "cli_app_a", string requestId = "ficc_call_1")
        => store.TryConsumeAsync(appKey, requestId);

    // ───────────────────── 登记 / 查询 / 租户隔离 ─────────────────────

    [Fact]
    public async Task SaveThenList_ShouldReturnPending_ForThatAppOnly()
    {
        var store = Store(Now);
        await store.SaveAsync(Snapshot());
        await store.SaveAsync(Snapshot(requestId: "ficc_call_2", appKey: "cli_app_b"));

        var pending = await store.ListPendingAsync("cli_app_a");

        pending.Should().ContainSingle();
        pending[0].RequestId.Should().Be("ficc_call_1");
        pending[0].AppKey.Should().Be("cli_app_a");
    }

    /// <summary>
    /// <b>跨租户隔离</b>：不同应用用了同一个框架请求标识时不得互相覆盖、互相关联
    /// （键是 <c>(AppKey, RequestId)</c>）——否则 A 租户的批准可能放行 B 租户的写操作。
    /// </summary>
    [Fact]
    public async Task SameRequestId_DifferentApps_ShouldNotCollide()
    {
        var store = Store(Now);
        await store.SaveAsync(Snapshot(appKey: "cli_app_a"));
        await store.SaveAsync(Snapshot(appKey: "cli_app_b"));

        (await store.FindAsync("cli_app_a", "ficc_call_1")).Should().NotBeNull();
        (await store.FindAsync("cli_app_b", "ficc_call_1")).Should().NotBeNull();

        (await ConsumeAsync(store, "cli_app_a")).Should().BeTrue();
        (await store.FindAsync("cli_app_b", "ficc_call_1")).Should().NotBeNull(
            "消费 A 租户的快照不得影响 B 租户同标识的快照");
    }

    [Fact]
    public async Task Find_ShouldReturnNull_ForUnknownRequest()
    {
        var store = Store(Now);
        (await store.FindAsync("cli_app_a", "missing")).Should().BeNull();
    }

    // ───────────────────── 过期语义 ─────────────────────

    [Fact]
    public async Task ExpiredSnapshot_ShouldNotBeListed_AndFindReturnsNull()
    {
        var store = Store(Now + TimeSpan.FromMinutes(11));
        await store.SaveAsync(Snapshot(createdAt: Now));

        (await store.ListPendingAsync("cli_app_a")).Should().BeEmpty("过期项不得出现在待办列表");
        (await store.FindAsync("cli_app_a", "ficc_call_1")).Should().BeNull(
            "过期项不得可查——否则宿主会对已放弃的请求发起批准");
    }

    /// <summary><b>过期即放弃</b>：过期快照的批准必须失败，且该快照被摘除。</summary>
    [Fact]
    public async Task TryConsume_ShouldFail_ForExpiredSnapshot_AndDropIt()
    {
        var store = Store(Now + TimeSpan.FromMinutes(11));
        await store.SaveAsync(Snapshot(createdAt: Now));

        (await ConsumeAsync(store)).Should().BeFalse("过期的批准绝不生效（不得出现『过期后迟到批准仍放行』）");
        (await store.FindAsync("cli_app_a", "ficc_call_1")).Should().BeNull("失败消费同时摘除过期项");
    }

    [Fact]
    public async Task RemoveExpired_ShouldReturnCountAndLeaveLiveItems()
    {
        var store = Store(Now + TimeSpan.FromMinutes(11));
        await store.SaveAsync(Snapshot(createdAt: Now));                                    // 已过期
        await store.SaveAsync(Snapshot(requestId: "ficc_call_2", createdAt: Now + TimeSpan.FromMinutes(10))); // 仍有效

        (await store.RemoveExpiredAsync()).Should().Be(1);

        var pending = await store.ListPendingAsync("cli_app_a");
        pending.Should().ContainSingle();
        pending[0].RequestId.Should().Be("ficc_call_2");
    }

    // ───────────────────── 幂等消费与取消 ─────────────────────

    [Fact]
    public async Task TryConsume_ShouldSucceedOnce_ThenFail()
    {
        var store = Store(Now);
        await store.SaveAsync(Snapshot());

        (await ConsumeAsync(store)).Should().BeTrue("首次消费（未过期）必须成功");
        (await ConsumeAsync(store)).Should().BeFalse(
            "重复回灌批准必须失败——同一次写操作不得被执行两次");
        (await store.ListPendingAsync("cli_app_a")).Should().BeEmpty();
    }

    [Fact]
    public async Task Remove_ShouldReportWhetherAnythingWasRemoved()
    {
        var store = Store(Now);
        await store.SaveAsync(Snapshot());

        (await store.RemoveAsync("cli_app_a", "ficc_call_1")).Should().BeTrue();
        (await store.RemoveAsync("cli_app_a", "ficc_call_1")).Should().BeFalse();
        (await ConsumeAsync(store)).Should().BeFalse("取消后不得再被消费（撤回了该次写操作）");
    }

    [Fact]
    public async Task Save_ShouldBeIdempotent_ForKey()
    {
        var store = Store(Now);
        await store.SaveAsync(Snapshot());
        await store.SaveAsync(Snapshot(createdAt: Now + TimeSpan.FromSeconds(5)));

        var pending = await store.ListPendingAsync("cli_app_a");
        pending.Should().ContainSingle("同 (AppKey, RequestId) 重复登记是覆盖语义（框架重投递幂等）");
        pending[0].CreatedAt.Should().Be(Now + TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ListPending_ShouldRejectEmptyAppKey()
        => await ((Func<Task>)(() => Store(Now).ListPendingAsync(string.Empty)))
            .Should().ThrowAsync<ArgumentException>();

    // ───────────────────── 快照构造与凭据边界 ─────────────────────

    [Fact]
    public void FromRequest_ShouldCarryFacts_AndApplyDefaultTtl()
    {
        var request = new FrameworkToolApprovalRequest(
            RequestId: "ficc_call_9",
            ToolName: "drive.transfer_owner",
            ToolCallId: "call_9",
            AppKey: "cli_app_a",
            UserId: "ou_user",
            ConversationKey: "conv",
            ArgumentsDigest: "file_token=<masked>",
            RequiredScopes: ["drive:drive"],
            ChatId: "oc_chat");

        var snapshot = PendingApprovalSnapshot.FromRequest(request, Now);

        snapshot.RequestId.Should().Be("ficc_call_9");
        snapshot.ToolName.Should().Be("drive.transfer_owner");
        snapshot.ExpiresAt.Should().Be(Now + PendingApprovalSnapshot.DefaultTtl);
        snapshot.IsExpired(Now).Should().BeFalse();
        snapshot.IsExpired(Now + PendingApprovalSnapshot.DefaultTtl).Should().BeTrue(
            "到期时刻即视为过期（边界取『达到』而非『超过』）");

        PendingApprovalSnapshot.FromRequest(request, Now, TimeSpan.FromMinutes(-1)).ExpiresAt
            .Should().Be(Now + PendingApprovalSnapshot.DefaultTtl, "非正 TTL 视作无效并回落默认值");
    }

    /// <summary>
    /// <b>凭据零落库</b>：快照的公开字段名与内容都不得出现凭据语义
    /// （SDK 不签发批准凭据；快照会被宿主持久化到库/缓存）。
    /// </summary>
    [Fact]
    public void Snapshot_ShouldNotCarryCredentialLikeMembers()
    {
        var forbidden = new[] { "token", "secret", "password", "credential", "signature", "apikey" };

        var members = typeof(PendingApprovalSnapshot)
            .GetProperties()
            .Select(static p => p.Name)
            .ToArray();

        members.Should().NotBeEmpty("反射面为空说明用例本身失效（假绿）");
        members.Should().NotContain(name =>
            forbidden.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase)));

        // 值侧同样断言：构造的快照不得含明文批准凭据（此处以"字段名干净 + 值来自投影"为界）。
        var snapshot = Snapshot();
        snapshot.ArgumentsDigest.Should().NotContain("Bearer");
    }

    // ───────────────────── 执行链接线（NotifyAsync 落库） ─────────────────────

    [Fact]
    public async Task NotifyAsync_ShouldPersistSnapshots_BeforeHandingToChannel()
    {
        var store = RealClockStore();
        var channel = new RecordingApprovalChannel();

        await FeishuApprovalRequestProjector.NotifyAsync(
            channel,
            store,
            logger: null,
            [Request("ficc_call_1")],
            CancellationToken.None);

        channel.Notified.Should().ContainSingle("通道仍必须收到通知（宿主主路径不变）");
        var pending = await store.ListPendingAsync("cli_app_a");
        pending.Should().ContainSingle("执行链必须把待确认项落成可查询快照（C4a）");
        pending[0].RequestId.Should().Be("ficc_call_1");
    }

    /// <summary>通道缺席（未注册宿主批准通道）时<b>仍应落库</b>——否则"未注册通道"等于"待办不可见"。</summary>
    [Fact]
    public async Task NotifyAsync_WithoutChannel_ShouldStillPersist()
    {
        var store = RealClockStore();

        await FeishuApprovalRequestProjector.NotifyAsync(
            channel: null,
            store,
            logger: null,
            [Request("ficc_call_2")],
            CancellationToken.None);

        (await store.ListPendingAsync("cli_app_a")).Should().ContainSingle();
    }

    /// <summary>落库失败<b>不得</b>阻断通知（best-effort），也不得自动放行写工具。</summary>
    [Fact]
    public async Task NotifyAsync_WhenStoreThrows_ShouldStillNotifyChannel()
    {
        var channel = new RecordingApprovalChannel();

        await FeishuApprovalRequestProjector.NotifyAsync(
            channel,
            new ThrowingPendingApprovalStore(),
            logger: null,
            [Request("ficc_call_3")],
            CancellationToken.None);

        channel.Notified.Should().ContainSingle("快照存储故障只失去可查询能力，不得阻断宿主通知");
    }

    private static FrameworkToolApprovalRequest Request(string requestId)
        => new(
            RequestId: requestId,
            ToolName: "im.send_message",
            ToolCallId: "call_1",
            AppKey: "cli_app_a",
            UserId: "ou_user",
            ConversationKey: "conv",
            ArgumentsDigest: "text=<len=12>",
            RequiredScopes: ["im:message:send_as_bot"],
            ChatId: "oc_chat");

    private sealed class RecordingApprovalChannel : IFeishuToolApprovalChannel
    {
        public List<FrameworkToolApprovalRequest> Notified { get; } = [];

        public Task<string?> RequestApprovalAsync(ToolApprovalRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("approval-1");

        public Task<string?> RequestFrameworkApprovalAsync(
            FrameworkToolApprovalRequest request,
            CancellationToken cancellationToken = default)
        {
            Notified.Add(request);
            return Task.FromResult<string?>("approval-1");
        }
    }

    private sealed class ThrowingPendingApprovalStore : IFeishuPendingApprovalStore
    {
        public Task SaveAsync(PendingApprovalSnapshot snapshot, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("模拟持久化后端故障");

        public Task<IReadOnlyList<PendingApprovalSnapshot>> ListPendingAsync(string appKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PendingApprovalSnapshot>>([]);

        public Task<PendingApprovalSnapshot?> FindAsync(string appKey, string requestId, CancellationToken cancellationToken = default)
            => Task.FromResult<PendingApprovalSnapshot?>(null);

        public Task<bool> TryConsumeAsync(string appKey, string requestId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<bool> RemoveAsync(string appKey, string requestId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<int> RemoveExpiredAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }
}
