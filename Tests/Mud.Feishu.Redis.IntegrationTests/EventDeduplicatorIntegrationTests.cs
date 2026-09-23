// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Redis.IntegrationTests;

/// <summary>
/// 事件去重状态机 v2 集成测试——覆盖 R-04/R-05/R-06/R-15/R-17/R-19，以及 R2 轮的
/// 亚秒 TTL 钳制语义（R2-03）与 Lua 非数字 timestamp 护栏（R2-13）。
/// </summary>
/// <remarks>
/// 键一律由 <see cref="RedisKeyBuilder.Combine"/> 产出，不写字面量——
/// 键布局曾因"用例写死单冒号、实现产出双冒号"造成潜伏漂移（T-R2-03）。
/// </remarks>
[Collection("Redis")]
public class EventDeduplicatorIntegrationTests : RedisIntegrationTestBase
{
    private const string KeyPrefix = "feishu:event:";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromSeconds(5);

    private readonly RedisFeishuEventDistributedDeduplicator _sut;

    /// <summary>
    /// 初始化测试（SUT 与实现同构：所有参数显式给出）。
    /// </summary>
    /// <param name="fixture">共享 Redis 夹具。</param>
    public EventDeduplicatorIntegrationTests(RedisFixture fixture) : base(fixture)
    {
        _sut = new RedisFeishuEventDistributedDeduplicator(
            Fixture.Redis,
            NullLogger<RedisFeishuEventDistributedDeduplicator>.Instance,
            DefaultTtl,
            ProcessingTimeout,
            KeyPrefix);
    }

    /// <summary>事件键（无 appKey 时 = <c>feishu:event::{eventId}</c>）。</summary>
    private static string EventKey(string eventId) => RedisKeyBuilder.Combine(KeyPrefix, eventId);

    [RedisFact]
    public async Task TryMarkAsProcessing_Should_Persist_Key_With_Ttl_When_Default_Ttl()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString("N");

        // Act
        var result = await _sut.TryMarkAsProcessingAsync(eventId);

        // Assert
        result.Status.Should().Be(DeduplicationStatus.Processing);

        var key = EventKey(eventId);
        var ttl = await Fixture.Database.KeyTimeToLiveAsync(key);
        ttl.HasValue.Should().BeTrue();
        ttl!.Value.TotalSeconds.Should().BeGreaterThan(0);
        ttl!.Value.TotalSeconds.Should().BeGreaterThanOrEqualTo((long)DefaultTtl.TotalSeconds - 10);

        var status = await Fixture.Database.HashGetAsync(key, "status");
        status.ToString().Should().Be("processing");
    }

    [RedisFact]
    public async Task TryMarkAsProcessing_With_SubSecond_Ttl_Should_Clamp_To_One_Second()
    {
        // T-R2-03：原用例断言"500ms 抛 ArgumentOutOfRangeException"与实现相反——
        // 实现（与整改方案 T-M2-1 一致）对**正值亚秒 TTL** 做 Math.Max(1, TotalSeconds) 钳制，
        // 仅 ttl <= TimeSpan.Zero 才抛。本用例固化"钳制为 1 秒"的真实语义。
        var eventId = Guid.NewGuid().ToString("N");
        var subSecondTtl = TimeSpan.FromMilliseconds(500);
        var key = EventKey(eventId);

        // Act：不抛
        var act = () => _sut.TryMarkAsProcessingAsync(eventId, ttl: subSecondTtl);
        await act.Should().NotThrowAsync();

        // Assert：键存在且 TTL 被钳制到 1 秒
        (await Fixture.Database.KeyExistsAsync(key)).Should().BeTrue();
        var ttl = await Fixture.Database.KeyTimeToLiveAsync(key);
        ttl.HasValue.Should().BeTrue();
        ttl!.Value.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(2));

        // 去重仍然生效（未被静默禁用）
        var second = await _sut.TryMarkAsProcessingAsync(eventId, ttl: subSecondTtl);
        second.IsDuplicate.Should().BeTrue();
        second.Status.Should().Be(DeduplicationStatus.Processing);
    }

    [RedisFact]
    public async Task TryMarkAsProcessing_Should_Be_Atomic_Across_Instances()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString("N");
        var sut2 = new RedisFeishuEventDistributedDeduplicator(
            Fixture.Redis, NullLogger<RedisFeishuEventDistributedDeduplicator>.Instance,
            DefaultTtl, ProcessingTimeout, KeyPrefix);

        // Act
        var results = await Task.WhenAll(
            _sut.TryMarkAsProcessingAsync(eventId),
            sut2.TryMarkAsProcessingAsync(eventId));

        // Assert：恰一个成功标记，其余重复
        var successCount = results.Count(r => !r.IsDuplicate);
        var duplicateCount = results.Count(r => r.IsDuplicate);
        (successCount + duplicateCount).Should().Be(2);
        successCount.Should().Be(1);
        duplicateCount.Should().Be(1);
    }

    [RedisFact]
    public async Task MarkAsCompleted_On_Missing_Key_Should_Not_Create_Key()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString("N");
        var key = EventKey(eventId);
        await Fixture.Database.KeyDeleteAsync(key);

        // Act
        await _sut.MarkAsCompletedAsync(eventId);

        // Assert：R-05 修复行为——不创建永久键
        (await Fixture.Database.KeyExistsAsync(key)).Should().BeFalse();
    }

    [RedisFact]
    public async Task MarkAsCompleted_Should_Refresh_Ttl()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString("N");
        var key = EventKey(eventId);
        await _sut.TryMarkAsProcessingAsync(eventId);

        // Act
        await _sut.MarkAsCompletedAsync(eventId);

        // Assert
        var ttl = await Fixture.Database.KeyTimeToLiveAsync(key);
        ttl.HasValue.Should().BeTrue();
        ttl!.Value.TotalSeconds.Should().BeGreaterThan(0);

        var status = await Fixture.Database.HashGetAsync(key, "status");
        status.ToString().Should().Be("completed");
    }

    [RedisFact]
    public async Task Rollback_Should_Not_Delete_Completed_When_Racing_With_MarkAsCompleted()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString("N");
        var key = EventKey(eventId);
        await _sut.TryMarkAsProcessingAsync(eventId);

        // Act：并发执行 Rollback 与 MarkAsCompleted × 50 轮
        for (var i = 0; i < 50; i++)
        {
            await Task.WhenAll(
                _sut.RollbackProcessingAsync(eventId),
                _sut.MarkAsCompletedAsync(eventId));
        }

        // Assert：终态若键存在则必为 Completed（R-06 原子化修复）
        var status = await Fixture.Database.HashGetAsync(key, "status");
        if (status.HasValue)
        {
            status.ToString().Should().Be("completed");
        }
    }

    [RedisFact]
    public async Task Processing_Timeout_Should_Recover_With_Server_Clock()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString("N");
        var key = EventKey(eventId);
        await _sut.TryMarkAsProcessingAsync(eventId);

        // 手工把 timestamp 改到过去（模拟超时；与 Lua 判定同为 Unix 秒）
        var oldTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 100;
        await Fixture.Database.HashSetAsync(key, "timestamp", oldTimestamp.ToString());

        // Act
        var result = await _sut.TryMarkAsProcessingAsync(eventId);

        // Assert：应返回可恢复（IsDuplicate=false, WasProcessing=true）
        result.IsDuplicate.Should().BeFalse();
        result.WasProcessing.Should().BeTrue();
    }

    [RedisFact]
    public async Task TryMarkAsProcessing_Should_NotThrow_When_Timestamp_Is_NonNumeric()
    {
        // T-R2-13 / R2-11：timestamp 为 ISO 字符串（历史存量键或外部误写）时，
        // Lua 不得因 now - nil 抛运行时错误（那会被包装为不可降级的 Server 类异常）。
        var eventId = Guid.NewGuid().ToString("N");
        var key = EventKey(eventId);
        (await _sut.TryMarkAsProcessingAsync(eventId)).Status.Should().Be(DeduplicationStatus.Processing);

        await Fixture.Database.HashSetAsync(key, "timestamp", "2026-01-01T00:00:00Z");

        var act = () => _sut.TryMarkAsProcessingAsync(eventId);
        await act.Should().NotThrowAsync();

        // 语义：无法解析时间戳 → 视为"仍在处理中"（fail-safe，不抢占、不重复）
        var result = await _sut.TryMarkAsProcessingAsync(eventId);
        result.IsDuplicate.Should().BeTrue();
        result.Status.Should().Be(DeduplicationStatus.Processing);
    }

    [RedisFact]
    public async Task GetStatus_Should_Return_Processing_Then_Completed()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString("N");

        // Act & Assert: Pending → Processing → Completed
        (await _sut.GetStatusAsync(eventId)).Should().Be(DeduplicationStatus.Pending);

        await _sut.TryMarkAsProcessingAsync(eventId);
        (await _sut.GetStatusAsync(eventId)).Should().Be(DeduplicationStatus.Processing);

        await _sut.MarkAsCompletedAsync(eventId);
        (await _sut.GetStatusAsync(eventId)).Should().Be(DeduplicationStatus.Completed);
    }

    [RedisFact]
    public async Task Dispose_Then_Call_Should_Throw_ObjectDisposed()
    {
        // Arrange
        var sut = new RedisFeishuEventDistributedDeduplicator(
            Fixture.Redis, NullLogger<RedisFeishuEventDistributedDeduplicator>.Instance,
            DefaultTtl, ProcessingTimeout, KeyPrefix);

        // Act
        sut.Dispose();

        // Assert
        var act = () => sut.TryMarkAsProcessingAsync("test");
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }
}
