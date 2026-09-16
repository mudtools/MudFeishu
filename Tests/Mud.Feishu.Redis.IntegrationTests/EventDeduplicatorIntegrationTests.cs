// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  本项目基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Redis.IntegrationTests;

/// <summary>
/// 事件去重状态机 v2 集成测试——覆盖 R-04/R-05/R-06/R-15/R-17/R-19
/// </summary>
[Collection("Redis")]
public class EventDeduplicatorIntegrationTests : RedisIntegrationTestBase
{
    private readonly RedisFeishuEventDistributedDeduplicator _sut;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromSeconds(5);

    public EventDeduplicatorIntegrationTests(RedisFixture fixture) : base(fixture)
    {
        _sut = new RedisFeishuEventDistributedDeduplicator(
            Fixture.Redis,
            NullLogger<RedisFeishuEventDistributedDeduplicator>.Instance,
            DefaultTtl,
            ProcessingTimeout,
            "feishu:event:");
    }

    [Fact]
    public async Task TryMarkAsProcessing_Should_Persist_Key_With_Ttl_When_Default_Ttl()
    {
        if (ShouldSkip()) return;

        // Arrange
        var eventId = Guid.NewGuid().ToString("N");

        // Act
        var result = await _sut.TryMarkAsProcessingAsync(eventId);

        // Assert
        result.Status.Should().Be(DeduplicationStatus.Processing);

        // 键应存在并有 TTL
        var key = $"feishu:event:{eventId}";
        var ttl = await Fixture.Database.KeyTimeToLiveAsync(key);
        ttl.HasValue.Should().BeTrue();
        ttl!.Value.TotalSeconds.Should().BeGreaterThan(0);
        ttl!.Value.TotalSeconds.Should().BeGreaterThanOrEqualTo((long)DefaultTtl.TotalSeconds - 10);

        // Hash 应含 status=processing
        var status = await Fixture.Database.HashGetAsync(key, "status");
        status.ToString().Should().Be("processing");
    }

    [Fact]
    public async Task TryMarkAsProcessing_With_SubSecond_Ttl_Should_Throw_And_Not_Disable_Dedup()
    {
        if (ShouldSkip()) return;

        // Arrange
        var eventId = Guid.NewGuid().ToString("N");
        var subSecondTtl = TimeSpan.FromMilliseconds(500);

        // Act
        var act = () => _sut.TryMarkAsProcessingAsync(eventId, ttl: subSecondTtl);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*TTL*");

        // 键不应存在（未执行 Lua）
        var key = $"feishu:event:{eventId}";
        (await Fixture.Database.KeyExistsAsync(key)).Should().BeFalse();
    }

    [Fact]
    public async Task TryMarkAsProcessing_Should_Be_Atomic_Across_Instances()
    {
        if (ShouldSkip()) return;

        // Arrange
        var eventId = Guid.NewGuid().ToString("N");
        var sut2 = new RedisFeishuEventDistributedDeduplicator(
            Fixture.Redis, NullLogger<RedisFeishuEventDistributedDeduplicator>.Instance,
            DefaultTtl, ProcessingTimeout, "feishu:event:");

        // Act
        var tasks = new[]
        {
            _sut.TryMarkAsProcessingAsync(eventId),
            sut2.TryMarkAsProcessingAsync(eventId)
        };
        var results = await Task.WhenAll(tasks);

        // Assert：恰一个成功标记，其余重复
        var successCount = results.Count(r => !r.IsDuplicate);
        var duplicateCount = results.Count(r => r.IsDuplicate);
        (successCount + duplicateCount).Should().Be(2);
        successCount.Should().Be(1);
        duplicateCount.Should().Be(1);
    }

    [Fact]
    public async Task MarkAsCompleted_On_Missing_Key_Should_Not_Create_Key()
    {
        if (ShouldSkip()) return;

        // Arrange
        var eventId = Guid.NewGuid().ToString("N");
        var key = $"feishu:event:{eventId}";

        // 确保 key 不存在
        await Fixture.Database.KeyDeleteAsync(key);

        // Act
        await _sut.MarkAsCompletedAsync(eventId);

        // Assert
        (await Fixture.Database.KeyExistsAsync(key)).Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsCompleted_Should_Refresh_Ttl()
    {
        if (ShouldSkip()) return;

        // Arrange
        var eventId = Guid.NewGuid().ToString("N");
        var key = $"feishu:event:{eventId}";
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

    [Fact]
    public async Task Rollback_Should_Not_Delete_Completed_When_Racing_With_MarkAsCompleted()
    {
        if (ShouldSkip()) return;

        // Arrange
        var eventId = Guid.NewGuid().ToString("N");
        var key = $"feishu:event:{eventId}";
        await _sut.TryMarkAsProcessingAsync(eventId);

        // Act：并发执行 Rollback 和 MarkAsCompleted × 50 轮
        for (var i = 0; i < 50; i++)
        {
            await Task.WhenAll(
                _sut.RollbackProcessingAsync(eventId),
                _sut.MarkAsCompletedAsync(eventId)
            );
        }

        // Assert：终态应为 Completed（键存在）或 Pending（键被 Rollback 删除后 MarkAsCompleted 未创建）
        // 关键：不应出现"Completed 后被 Rollback 误删"的竞态
        var status = await Fixture.Database.HashGetAsync(key, "status");
        if (status.HasValue)
        {
            // 如果键存在，状态不应是 processing 被 rollback 后的残留
            status.ToString().Should().Be("completed");
        }
    }

    [Fact]
    public async Task Processing_Timeout_Should_Recover_With_Server_Clock()
    {
        if (ShouldSkip()) return;

        // Arrange
        var eventId = Guid.NewGuid().ToString("N");
        var key = $"feishu:event:{eventId}";
        await _sut.TryMarkAsProcessingAsync(eventId);

        // 手工修改 timestamp 为过去值（模拟超时）
        var oldTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 100;
        await Fixture.Database.HashSetAsync(key, "timestamp", oldTimestamp.ToString());

        // Act
        var result = await _sut.TryMarkAsProcessingAsync(eventId);

        // Assert：应返回可恢复（IsDuplicate=false, WasProcessing=true）
        result.IsDuplicate.Should().BeFalse();
        result.WasProcessing.Should().BeTrue();
    }

    [Fact]
    public async Task GetStatus_Should_Return_Processing_Then_Completed()
    {
        if (ShouldSkip()) return;

        // Arrange
        var eventId = Guid.NewGuid().ToString("N");

        // Act & Assert: Pending → Processing → Completed
        (await _sut.GetStatusAsync(eventId)).Should().Be(DeduplicationStatus.Pending);

        await _sut.TryMarkAsProcessingAsync(eventId);
        (await _sut.GetStatusAsync(eventId)).Should().Be(DeduplicationStatus.Processing);

        await _sut.MarkAsCompletedAsync(eventId);
        (await _sut.GetStatusAsync(eventId)).Should().Be(DeduplicationStatus.Completed);
    }

    [Fact]
    public async Task Dispose_Then_Call_Should_Throw_ObjectDisposed()
    {
        if (ShouldSkip()) return;

        // Arrange
        var sut = new RedisFeishuEventDistributedDeduplicator(
            Fixture.Redis, NullLogger<RedisFeishuEventDistributedDeduplicator>.Instance,
            DefaultTtl, ProcessingTimeout, "feishu:event:");

        // Act
        sut.Dispose();

        // Assert
        var act = () => sut.TryMarkAsProcessingAsync("test");
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }
}
