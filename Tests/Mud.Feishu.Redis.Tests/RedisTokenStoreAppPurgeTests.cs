// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net;
using FluentAssertions;
using Moq;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;
using Xunit;

namespace Mud.Feishu.Redis.Tests;

/// <summary>
/// U-4（上游 3.0.10 Batch 2）：<see cref="RedisTokenStore"/> 的 <see cref="ITokenStoreAppPurge"/>
/// 能力面测试 —— EnumerateKeysAsync 快照枚举与 PurgeAppAsync 以实例键空间为边界的清库。
/// </summary>
public class RedisTokenStoreAppPurgeTests
{
    private static (RedisTokenStore Store, List<string> DeletedKeys) BuildStore(
        IEnumerable<string>? serverKeys = null,
        bool failOnDelete = false)
    {
        var deletedKeys = new List<string>();

        var db = new Mock<IDatabase>();
        db.Setup(d => d.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        if (failOnDelete)
        {
            db.Setup(d => d.KeyDeleteAsync(
                    It.IsAny<RedisKey[]>(),
                    It.IsAny<CommandFlags>()))
                .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.SocketFailure, "node down"));
        }
        else
        {
            db.Setup(d => d.KeyDeleteAsync(
                    It.IsAny<RedisKey[]>(),
                    It.IsAny<CommandFlags>()))
                .Callback<RedisKey[], CommandFlags>((keys, _) => deletedKeys.AddRange(keys.Select(k => k.ToString())))
                .ReturnsAsync((RedisKey[] keys, CommandFlags _) => (long)keys.Length);
        }

        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        if (serverKeys != null)
        {
            // ClearAsync / EnumerateKeysAsync / PurgeAppAsync 走 GetServer(...).KeysAsync(...)。
            // 首次枚举返回 serverKeys，之后为空 —— 模拟「清库后键空间已空」的真实语义。
            var scanCount = 0;
            var server = new Mock<IServer>();
            server.Setup(s => s.IsConnected).Returns(true);
            server.Setup(s => s.IsReplica).Returns(false);
            server.Setup(s => s.KeysAsync(
                    It.IsAny<int>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<int>(),
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<CommandFlags>()))
                .Returns(() => ToAsyncKeys(scanCount++ == 0
                    ? serverKeys.Select(k => (RedisKey)k)
                    : Enumerable.Empty<RedisKey>()));

            var endpoint = new DnsEndPoint("localhost", 6379);
            redis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns(new EndPoint[] { endpoint });
            redis.Setup(r => r.GetServer(It.IsAny<EndPoint>(), It.IsAny<object>())).Returns(server.Object);
        }

        var store = (RedisTokenStore)new PerAppRedisTokenStoreFactory(redis.Object).Create("cli_a").TokenStore;
        return (store, deletedKeys);
    }

    private static async IAsyncEnumerable<RedisKey> ToAsyncKeys(IEnumerable<RedisKey> keys)
    {
        foreach (var key in keys)
        {
            await Task.Yield();
            yield return key;
        }
    }

    /// <summary>
    /// U-4 契约：EnumerateKeysAsync 必须返回本实例键空间的<b>物理键快照</b>（含跨节点去重），
    /// 而非 GetTokenTypesAsync 的 tokenType 归一化面。
    /// </summary>
    [Fact]
    public async Task EnumerateKeysAsync_ShouldReturnDistinctPhysicalKeys()
    {
        var (store, _) = BuildStore(new[]
        {
            "feishu:cli_a:token:TenantAccessToken:cli_a:access",
            "feishu:cli_a:token:TenantAccessToken:cli_a:refresh",
            "feishu:cli_a:token:TenantAccessToken:cli_a:access",   // SCAN 重复面：必须去重
            "feishu:cli_a:token:UserAccessToken:u1:refresh",
        });

        var keys = await store.EnumerateKeysAsync("cli_a");

        keys.Should().HaveCount(3);
        keys.Should().AllSatisfy(k => k.Should().StartWith("feishu:cli_a:token:"));
    }

    /// <summary>
    /// U-4 契约：PurgeAppAsync 以实例键空间为边界（KeyPrefix 已绑定单应用），
    /// 清空全部扫描键并返回<b>实际删除键数</b>。
    /// </summary>
    [Fact]
    public async Task PurgeAppAsync_ShouldDeleteAllScannedKeys_AndReturnDeletedCount()
    {
        var (store, deleted) = BuildStore(new[]
        {
            "feishu:cli_a:token:TenantAccessToken:cli_a:access",
            "feishu:cli_a:token:TenantAccessToken:cli_a:refresh",
            "feishu:cli_a:token:UserAccessToken:u1:refresh",
        });

        var deletedCount = await store.PurgeAppAsync("cli_a");

        deletedCount.Should().Be(3);
        deleted.Should().HaveCount(3);
    }

    /// <summary>
    /// U-4 契约：节点失败必须直接上抛 —— 不得静默半删。
    /// </summary>
    [Fact]
    public async Task PurgeAppAsync_ShouldPropagateNodeFailure()
    {
        var (store, _) = BuildStore(new[]
        {
            "feishu:cli_a:token:TenantAccessToken:cli_a:access",
        }, failOnDelete: true);

        await Assert.ThrowsAsync<RedisConnectionException>(
            () => store.PurgeAppAsync("cli_a"));
    }

    /// <summary>
    /// U-4 能力透传：PurgeAppAsync 与 ClearAsync 共用同一清库核心 ——
    /// 二者对同一键空间的清库语义逐位一致（U-4 落地前 ClearAsync 是唯一入口）。
    /// </summary>
    [Fact]
    public async Task PurgeAppAsync_ShouldShareClearSemantics()
    {
        var (store, _) = BuildStore(new[]
        {
            "feishu:cli_a:token:TenantAccessToken:cli_a:access",
            "feishu:cli_a:token:UserAccessToken:u1:refresh",
        });

        await store.ClearAsync();
        var keysAfterClear = await store.EnumerateKeysAsync("cli_a");

        keysAfterClear.Should().BeEmpty();
    }
}
