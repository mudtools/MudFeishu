// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Mud.Feishu.Abstractions.Authentication;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// 验证 Memory 路径下多应用令牌键隔离（OBS-1 / TOK-1）。
/// </summary>
/// <remarks>
/// <para>
/// <b>背景</b>：Redis 路径（TOK-1）原先忽略 appKey，多应用令牌写入同一键空间并互相覆盖；
/// 而 Memory 路径（<see cref="FeishuTokenStore"/> / <see cref="FeishuUserTokenStore"/>）
/// 的键布局本已包含 appKey，这也是 OBS-1 得出「现有键设计正确」的原因——该结论仅对 Memory 路径成立。
/// </para>
/// <para>
/// <b>本用例的作用</b>：把这一「已正确的行为」固化为回归契约。后续若有人为对齐 Redis
/// 或做键布局重构而移除 appKey 维度，将在此处立即失败，而不是以「随机 401 / 令牌串号」的形式暴露到线上。
/// </para>
/// </remarks>
public class MultiAppTokenKeyIsolationTests
{
    private const string AppA = "cli_a";
    private const string AppB = "cli_b";

    private static (PerAppFeishuTokenStoreFactory Factory, MemoryCache Cache) CreateFactory()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        return (new PerAppFeishuTokenStoreFactory(cache), cache);
    }

    /// <summary>
    /// 同一 tokenType 下，不同应用的租户令牌必须互不覆盖。
    /// </summary>
    [Fact]
    public async Task TenantToken_ShouldNotCollide_WhenDifferentAppsUseSameTokenType()
    {
        var (factory, cache) = CreateFactory();
        using var cacheScope = cache;

        var (storeA, _) = factory.Create(AppA);
        var (storeB, _) = factory.Create(AppB);

        await storeA.SetAccessTokenAsync("tenant", "token-a", 3600);
        await storeB.SetAccessTokenAsync("tenant", "token-b", 3600);

        (await storeA.GetAccessTokenAsync("tenant")).Should().Be("token-a");
        (await storeB.GetAccessTokenAsync("tenant")).Should().Be("token-b");
    }

    /// <summary>
    /// 租户令牌的物理键必须形如 <c>feishu:{appKey}:token:{tokenType}:access</c>
    /// （键布局即隔离机制本身，直接断言键名而非仅断言读写结果）。
    /// </summary>
    [Theory]
    [InlineData(AppA, "token-a")]
    [InlineData(AppB, "token-b")]
    public async Task TenantToken_ShouldUseKeyWithAppKeyDimension(string appKey, string value)
    {
        var (factory, cache) = CreateFactory();
        using var cacheScope = cache;

        var (store, _) = factory.Create(appKey);
        await store.SetAccessTokenAsync("tenant", value, 3600);

        cache.TryGetValue<string>($"feishu:{appKey}:token:tenant:access", out var cached).Should().BeTrue();
        cached.Should().Be(value);
    }

    /// <summary>
    /// 同一用户在不同应用下的用户令牌必须互不覆盖（用户令牌同样需要 appKey 维度）。
    /// </summary>
    [Fact]
    public async Task UserToken_ShouldNotCollide_WhenSameUserAcrossApps()
    {
        var (factory, cache) = CreateFactory();
        using var cacheScope = cache;

        var (_, userA) = factory.Create(AppA);
        var (_, userB) = factory.Create(AppB);

        userA.Should().NotBeNull();
        userB.Should().NotBeNull();

        await userA!.SetAccessTokenAsync("ou_1", "user", "user-token-a", 3600);
        await userB!.SetAccessTokenAsync("ou_1", "user", "user-token-b", 3600);

        (await userA.GetAccessTokenAsync("ou_1", "user")).Should().Be("user-token-a");
        (await userB.GetAccessTokenAsync("ou_1", "user")).Should().Be("user-token-b");
    }

    /// <summary>
    /// 用户令牌的物理键必须形如 <c>feishu:{appKey}:token:user:{userId}:{tokenType}:access</c>。
    /// </summary>
    [Fact]
    public async Task UserToken_ShouldUseKeyWithAppKeyDimension()
    {
        var (factory, cache) = CreateFactory();
        using var cacheScope = cache;

        var (_, userA) = factory.Create(AppA);
        await userA!.SetAccessTokenAsync("ou_1", "user", "ut", 3600);

        cache.TryGetValue<string>($"feishu:{AppA}:token:user:ou_1:user:access", out var cached).Should().BeTrue();
        cached.Should().Be("ut");
    }

    /// <summary>
    /// 刷新令牌同样按 appKey 隔离。
    /// </summary>
    [Fact]
    public async Task RefreshToken_ShouldNotCollide_WhenDifferentAppsUseSameTokenType()
    {
        var (factory, cache) = CreateFactory();
        using var cacheScope = cache;

        var (storeA, _) = factory.Create(AppA);
        var (storeB, _) = factory.Create(AppB);

        await storeA.SetRefreshTokenAsync("tenant", "refresh-a");
        await storeB.SetRefreshTokenAsync("tenant", "refresh-b");

        (await storeA.GetRefreshTokenAsync("tenant")).Should().Be("refresh-a");
        (await storeB.GetRefreshTokenAsync("tenant")).Should().Be("refresh-b");
    }

    /// <summary>
    /// tokenType 内含 ':'（如 <c>tenant:cli_xxx</c>）时不得被截断——用户令牌键由
    /// 前缀 + userId + tokenType 拼接，tokenType 参与键构造，必须完整保留。
    /// </summary>
    [Fact]
    public async Task UserToken_ShouldPreserveTokenType_WhenTokenTypeContainsColon()
    {
        var (factory, cache) = CreateFactory();
        using var cacheScope = cache;

        var (_, userA) = factory.Create(AppA);
        await userA!.SetAccessTokenAsync("ou_1", "tenant:cli_a", "v", 3600);

        (await userA.GetAccessTokenAsync("ou_1", "tenant:cli_a")).Should().Be("v");
        // TMA2-02: tokenType 中的 ':' 被转义为 '\:'
        cache.TryGetValue<string>($"feishu:{AppA}:token:user:ou_1:tenant\\:cli_a:access", out _).Should().BeTrue();
    }

    /// <summary>
    /// 不同应用必须拿到彼此独立的存储实例（AppKey 是隔离维度的载体）。
    /// </summary>
    [Fact]
    public void Create_ShouldReturnDistinctStores_PerApp()
    {
        var (factory, cache) = CreateFactory();
        using var cacheScope = cache;

        var (storeA, userA) = factory.Create(AppA);
        var (storeB, userB) = factory.Create(AppB);

        storeA.Should().NotBeSameAs(storeB);
        userA.Should().NotBeSameAs(userB);
    }

    /// <summary>
    /// 应用被移除（配置热更新删除应用）后，重新创建同名应用仍应复用既有键空间，
    /// 使令牌可被继续读取——这是 ARC-1「重建上下文后令牌天然热迁移」的键侧前提。
    /// </summary>
    [Fact]
    public async Task Token_ShouldSurviveStoreRecreation_ForSameAppKey()
    {
        var (factory, cache) = CreateFactory();
        using var cacheScope = cache;

        var (store, _) = factory.Create(AppA);
        await store.SetAccessTokenAsync("tenant", "persisted", 3600);

        // 模拟配置热更新后重建的 per-app 存储（同一 appKey → 同一键空间）
        var (recreated, _) = factory.Create(AppA);

        (await recreated.GetAccessTokenAsync("tenant")).Should().Be("persisted");
    }
}
