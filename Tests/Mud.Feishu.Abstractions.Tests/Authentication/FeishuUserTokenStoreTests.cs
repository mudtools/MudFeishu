// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Mud.Feishu.Abstractions.Authentication;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// TMR-P3-16a（F16a）用户令牌存储空条目修剪回归测试。
/// </summary>
/// <remarks>
/// 原实现刻意保留空外层 (KeyPrefix → userId) 条目（防孤儿字典），代价是每历史用户
/// ~64B 空字典长期驻留。现改为：ClearUserAsync / UntrackUserTokenType 后内层为空即修剪外层。
/// 与 TrackUserTokenType 的 GetOrAdd 并发交错的残余窗口被容忍（孤儿注册最坏导致一次
/// 无害 no-op Remove，且 Memory 后端键自带 TTL）。
/// </remarks>
public class FeishuUserTokenStoreTests
{
    private static (FeishuUserTokenStore Store, IMemoryCache Cache) CreateStore(string appKey)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var inner = new FeishuTokenStore(cache, appKey);
        return (new FeishuUserTokenStore(inner, cache, appKey), cache);
    }

    private static ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> GetSharedUsers(string appKey)
    {
        var field = typeof(FeishuUserTokenStore).GetField(
            "SharedUserTypes",
            BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull("SharedUserTypes 是跨实例共享记账的载体");
        var shared = (ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>>)field!.GetValue(null)!;
        return shared.GetOrAdd($"feishu:{appKey}:token", _ => new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>());
    }

    [Fact]
    public async Task ClearUser_ShouldPruneEmptyUserEntry()
    {
        // Arrange（独立 appKey 隔离静态记账，避免测试间串扰）
        const string appKey = "prune-clear-user";
        var (store, _) = CreateStore(appKey);

        await store.SetAccessTokenAsync("u1", "t1", "token", 3600);
        GetSharedUsers(appKey).ContainsKey("u1").Should().BeTrue("写入后应已记账");

        // Act
        await store.ClearUserAsync("u1");

        // Assert：外层空条目被修剪（修复前保留 ~64B 空字典驻留）。
        GetSharedUsers(appKey).ContainsKey("u1").Should().BeFalse("ClearUserAsync 后空外层条目应被修剪");
    }

    [Fact]
    public async Task RemoveToken_ShouldPruneEmptyUserEntry_WhenLastTokenTypeRemoved()
    {
        // Arrange
        const string appKey = "prune-remove-user";
        var (store, _) = CreateStore(appKey);

        await store.SetAccessTokenAsync("u1", "t1", "token", 3600);
        await store.RemoveAsync("u1", "t1");

        // Assert：UntrackUserTokenType 后内层为空 → 外层修剪。
        GetSharedUsers(appKey).ContainsKey("u1").Should().BeFalse("最后一个 tokenType 反注册后空外层条目应被修剪");
    }

    [Fact]
    public async Task RemoveToken_ShouldKeepEntry_WhenOtherTokenTypesRemain()
    {
        // Arrange
        const string appKey = "prune-keep-user";
        var (store, _) = CreateStore(appKey);

        await store.SetAccessTokenAsync("u1", "t1", "token1", 3600);
        await store.SetAccessTokenAsync("u1", "t2", "token2", 3600);

        // Act：仅移除其中一个 tokenType。
        await store.RemoveAsync("u1", "t1");

        // Assert：内层仍有 t2 → 外层条目保留。
        GetSharedUsers(appKey).ContainsKey("u1").Should().BeTrue("仍有存活的 tokenType 时不得修剪");
        (await store.GetTokenTypesAsync("u1")).Should().Contain("t2");
    }

    [Fact]
    public async Task ClearUser_ShouldTolerateTrackRebuild_AfterPrune()
    {
        // Arrange：修剪后再次写入——Track 侧 GetOrAdd 重建，行为不受影响。
        const string appKey = "prune-rebuild";
        var (store, cache) = CreateStore(appKey);

        await store.SetAccessTokenAsync("u1", "t1", "token", 3600);
        await store.ClearUserAsync("u1");
        GetSharedUsers(appKey).ContainsKey("u1").Should().BeFalse();

        // Act：修剪后重新写入并再次清库（D10 语义回归——重建的注册必须可见）。
        await store.SetAccessTokenAsync("u1", "t1", "token-again", 3600);
        await store.ClearUserAsync("u1");

        // Assert：重建后的记账被 ClearUserAsync 正确消费（键被删除）。
        cache.Get<string>(TokenKeyBuilder.UserAccessKey($"feishu:{appKey}:token", "u1", "t1"))
            .Should().BeNull("修剪重建后 ClearUserAsync 仍应删除已记账的键");
    }
}
