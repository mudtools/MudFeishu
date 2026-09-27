// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;
using Xunit;

namespace Mud.Feishu.Redis.Tests.Services;

/// <summary>
/// Redis 分布式会话闸门测试（AI-FD-D12 P2D-1）：SET NX 语义（获取/TTL/快速失败/重试）、
/// 释放走比较删除（只删自己的租约）。
/// </summary>
public class RedisConversationGateTests
{
    private static (Mock<IConnectionMultiplexer> Redis, Mock<IDatabase> Db, RedisConversationGate Gate) CreateGate(
        TimeSpan? ttl = null)
    {
        var db = new Mock<IDatabase>();
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        var options = Options.Create(new FeishuConversationOptions { SessionTtl = ttl ?? TimeSpan.FromMinutes(5) });
        var gate = new RedisConversationGate(redis.Object, options, NullLogger<RedisConversationGate>.Instance);
        return (redis, db, gate);
    }

    [Fact]
    public async Task AcquireAsync_ShouldUseSetNx_WithLeaseTtl()
    {
        var (_, db, gate) = CreateGate();
        db.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        using var handle = await gate.AcquireAsync("feishu:app-a:conversation:chat:oc_1");

        Assert.NotNull(handle);
        db.Verify(d => d.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString().Contains("conversation:gate")),
                It.IsAny<RedisValue>(),
                TimeSpan.FromMinutes(5),
                When.NotExists,
                It.IsAny<CommandFlags>()),
            Times.Once, "租约键经命名空间组合、TTL 单一阈值源（SessionTtl）、SET NX 语义");
    }

    [Fact]
    public async Task AcquireAsync_ShouldFailFast_WithConversationBusyException_WhenLeaseOccupied()
    {
        var (_, db, gate) = CreateGate();
        db.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<ConversationBusyException>(
            () => gate.AcquireAsync("feishu:app-a:conversation:chat:oc_busy"));

        Assert.Contains("快速失败", exception.Message, StringComparison.Ordinal);
        Assert.Equal("feishu:app-a:conversation:chat:oc_busy", exception.ConversationKey);
    }

    [Fact]
    public async Task AcquireAsync_ShouldSucceed_WhenLeaseReleasedByRetry()
    {
        var (_, db, gate) = CreateGate();
        var attempts = 0;
        db.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(() =>
            {
                attempts++;
                return attempts >= 3; // 前两次被占，第三次获取成功。
            });

        using var handle = await gate.AcquireAsync("feishu:app-a:conversation:chat:oc_retry");

        Assert.NotNull(handle);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Dispose_ShouldReleaseLease_WithCompareAndDelete()
    {
        var (_, db, gate) = CreateGate();
        db.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        RedisValue token = default;
        db.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Callback((string _, RedisKey[] _, RedisValue[] values, CommandFlags _) => token = values[0])
            .ReturnsAsync(RedisResult.Create(1));

        var handle = await gate.AcquireAsync("feishu:app-a:conversation:chat:oc_release");
        handle.Dispose();

        // 同步 Dispose 契约下后台释放；轮询等待脚本调用。
        for (var i = 0; i < 50 && token.IsNull; i++)
        {
            await Task.Delay(20);
        }

        Assert.False(token.IsNull, "释放经 Lua 比较删除（只删自己的租约，防 TTL 过期后误删他人租约）");
    }

    [Fact]
    public async Task AcquireAsync_ShouldRejectEmptyKey()
    {
        var (_, _, gate) = CreateGate();
        await Assert.ThrowsAsync<ArgumentException>(() => gate.AcquireAsync(" "));
    }
}
