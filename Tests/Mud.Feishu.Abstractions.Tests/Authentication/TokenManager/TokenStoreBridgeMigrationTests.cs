// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.DataModels;

namespace Mud.Feishu.Tests.Authentication.TokenManager;

/// <summary>
/// 令牌持久化桥接改造（对接 Mud.HttpUtils <c>TokenStoreBackedTokenCache&lt;T&gt;</c>）的下游侧守护测试。
/// </summary>
/// <remarks>
/// <para>
/// 与 <c>UserTokenManagerTmrTests</c> / <c>TokenManagerWithStoreConcurrencyTests</c> 的分工：
/// 后者锁定「恢复阈值 / 持久化 / 读故障降级」的既有行为，本类锁定<b>改造引入的新一致性契约</b>：
/// </remarks>
/// <list type="number">
/// <item>D1：<c>InvalidateUserTokenAsync</c> 保留持久层 refresh_token，且不删除持久层槽位。</item>
/// <item>F2：镜像冷启动下失效<b>不得</b>被桥接器读穿透复活（否则失效被静默撤销）。</item>
/// <item>F6：失效后的镜像墓碑不得作为令牌信息返回给调用方。</item>
/// <item>F1：持久层读故障降级为「未命中」，不冒泡给业务调用方。</item>
/// <item>F5：访问令牌剩余 ≤ 0 时跳过 access 写穿（仅写 refresh）。</item>
/// <item>BD-4：清库门只拦读族，写族与 <c>ClearAllUsersAsync</c> 必须透传。</item>
/// </list>
/// <para>
/// 隔离约束：<see cref="TokenStorePurgeGate"/> 是<b>进程内静态</b>状态，而 xUnit 默认按测试类并行执行，
/// 故本类所有用例使用 <c>Guid</c> 生成的一次性 appKey，绝不依赖 <c>ResetForTest</c> 做跨类隔离。
/// </para>
/// </remarks>
public class TokenStoreBridgeMigrationTests
{
    private const long AccessLifetimeMs = 3_600_000;
    private const long RefreshLifetimeMs = 2_592_000_000;   // 30 天

    private static long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static string NewAppKey() => "bridge-" + Guid.NewGuid().ToString("N");

    private static FeishuAppConfig CreateConfig(string appKey) => new()
    {
        AppKey = appKey,
        AppId = "cli_" + appKey,
        AppSecret = "secret_" + appKey,
        TokenRefreshThreshold = 300
    };

    private static string TokenTypeOf(string appKey) => $"UserAccessToken:{appKey}";

    private static string Encode(string token, long expireAtMs) => TokenStoreHelper.EncodeStoredToken(token, expireAtMs);

    private static UserTokenManager CreateUserManager(
        FeishuAppConfig config,
        IUserTokenStore? store,
        IFeishuAuthentication? authenticationApi = null,
        ILogger<UserTokenManager>? logger = null)
        => new(
            currentUserContext: null,
            authenticationApi: authenticationApi ?? new Mock<IFeishuAuthentication>().Object,
            options: Options.Create(config),
            logger: logger ?? NullLogger<UserTokenManager>.Instance,
            userTokenStore: store);

    /// <summary>构造「持久层已有有效 access + refresh」的存储桩。</summary>
    private static Mock<IUserTokenStore> CreateStoreWithTokens(string userId, string tokenType, string access, string refresh)
    {
        var store = new Mock<IUserTokenStore>();
        store.Setup(x => x.GetAccessTokenAsync(userId, tokenType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encode(access, NowMs + AccessLifetimeMs));
        store.Setup(x => x.GetRefreshTokenAsync(userId, tokenType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encode(refresh, NowMs + RefreshLifetimeMs));
        return store;
    }

    // =====================================================================
    // D1 / F2 / F6：失效语义（镜像冷启动）
    // =====================================================================

    [Fact]
    public async Task InvalidateUserTokenAsync_ShouldKeepStoreRefresh_AndNotResurrectRejectedAccessToken()
    {
        // Arrange：持久层持有「有效但已被 IdP 拒绝」的访问令牌 + 可续期的 refresh；
        // 本进程镜像为空（多实例 / 冷启动场景）。
        var appKey = NewAppKey();
        var tokenType = TokenTypeOf(appKey);
        const string userId = "ou_invalidate";
        var store = CreateStoreWithTokens(userId, tokenType, "rejected-access", "refresh-1");
        var manager = CreateUserManager(CreateConfig(appKey), store.Object);

        // Act
        await manager.InvalidateUserTokenAsync(userId);

        store.Invocations.Clear();   // 只观察「失效之后」的持久层访问

        // Assert（F6）：失效墓碑不得作为令牌信息返回
        (await manager.GetTokenInfoAsync(userId)).Should().BeNull(
            "失效后的空访问令牌墓碑不得返回给调用方（F6）");

        // Assert（F2）：失效后读路径必须被墓碑短路 —— 不得从持久层复活被拒绝的访问令牌
        store.Verify(
            x => x.GetAccessTokenAsync(userId, tokenType, It.IsAny<CancellationToken>()),
            Times.Never,
            "冷镜像失效后读路径必须短路持久层，否则失效被读穿透静默撤销（F2）");

        // Assert（D1）：失效不得删除持久层槽位（refresh 是 401 恢复的唯一续期凭据）
        store.Verify(
            x => x.RemoveAsync(userId, tokenType, It.IsAny<CancellationToken>()),
            Times.Never,
            "InvalidateUserTokenAsync 不得清除持久层 refresh_token（D1）");

        // Assert（D1/F5）：空访问令牌墓碑对持久层必须零写入（不得把 refresh 覆盖丢失）
        store.Verify(
            x => x.SetRefreshTokenAsync(userId, tokenType, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "墓碑（access/refresh 皆空）不得产生持久层写入");

        // Assert（D1 实质）：refresh 仍可达 ⇒ 401 恢复链路可续期
        (await manager.CanRefreshTokenAsync(userId)).Should().BeTrue(
            "失效后 refresh 必须仍可经持久层读到，供 OAuth 续期（D1）");
    }

    [Fact]
    public async Task InvalidateUserTokenAsync_ShouldInvalidateMirrorEntry_WhenMirrorIsWarm()
    {
        // Arrange：先读一次使镜像命中（warm），再失效
        var appKey = NewAppKey();
        var tokenType = TokenTypeOf(appKey);
        const string userId = "ou_warm";
        var store = CreateStoreWithTokens(userId, tokenType, "live-access", "refresh-1");
        var manager = CreateUserManager(CreateConfig(appKey), store.Object);

        var restored = await manager.GetTokenInfoAsync(userId);
        restored.Should().NotBeNull();
        restored!.AccessToken.Should().Be("live-access");

        // Act
        await manager.InvalidateUserTokenAsync(userId);

        store.Invocations.Clear();

        // Assert：镜像条目被置空 ⇒ 不返回、不回落持久层、不删持久层
        (await manager.GetTokenInfoAsync(userId)).Should().BeNull("失效后镜像条目不得再作为有效令牌返回");
        store.Verify(
            x => x.GetAccessTokenAsync(userId, tokenType, It.IsAny<CancellationToken>()),
            Times.Never,
            "镜像已记录失效，不得再回落持久层");
        store.Verify(
            x => x.RemoveAsync(userId, tokenType, It.IsAny<CancellationToken>()),
            Times.Never,
            "D1：不得删除持久层槽位");
    }

    [Fact]
    public async Task GetTokenAsync_ShouldRefreshViaOAuth_AfterInvalidate_WhenMirrorIsCold()
    {
        // Arrange：冷镜像 + 持久层持有「有效但被拒绝」的访问令牌。
        // 改造前的正确行为是：管线下一次必然走 OAuth 续期；桥接下的错误行为是返回同一个被拒令牌。
        var appKey = NewAppKey();
        var tokenType = TokenTypeOf(appKey);
        const string userId = "ou_recover";
        var store = CreateStoreWithTokens(userId, tokenType, "rejected-access", "refresh-1");

        var authenticationApi = new Mock<IFeishuAuthentication>();
        authenticationApi
            .Setup(x => x.GetOAuthenRefreshAccessTokenAsync(It.IsAny<OAuthRefreshTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthCredentialsResult
            {
                Code = 0,
                Msg = "ok",
                AccessToken = "new-access",
                RefreshToken = "refresh-2",
                ExpiresIn = 7200,
                RefreshTokenExpiresIn = 2592000
            });

        var manager = CreateUserManager(CreateConfig(appKey), store.Object, authenticationApi.Object);

        // Act
        await manager.InvalidateUserTokenAsync(userId);
        var token = await manager.GetTokenAsync(userId);

        // Assert
        token.Should().Be("new-access",
            "失效后必须经 OAuth 续期，而不是把持久层里已被拒绝的访问令牌返回给调用方（F2）");
        authenticationApi.Verify(
            x => x.GetOAuthenRefreshAccessTokenAsync(It.IsAny<OAuthRefreshTokenRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =====================================================================
    // F1：持久层读故障降级（用户侧）
    // =====================================================================

    [Fact]
    public async Task GetTokenInfoAsync_ShouldDegradeToMiss_InsteadOfThrowing_WhenStoreReadFails()
    {
        // Arrange：装饰层必须兜住 store 读异常 —— 上游桥接器不做此兜底，
        // 若不兜底，存储抖动会从「回落 API 刷新」恶化为「把异常抛给业务调用方」（F1）。
        var appKey = NewAppKey();
        var tokenType = TokenTypeOf(appKey);
        var store = new Mock<IUserTokenStore>();
        store.Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), tokenType, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("store down"));
        store.Setup(x => x.GetRefreshTokenAsync(It.IsAny<string>(), tokenType, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("store down"));
        var manager = CreateUserManager(CreateConfig(appKey), store.Object);

        // Act / Assert
        var info = await manager.GetTokenInfoAsync("ou_fault");
        info.Should().BeNull("持久层读故障必须降级为未命中，不得冒泡");

        var canRefresh = await manager.CanRefreshTokenAsync("ou_fault");
        canRefresh.Should().BeFalse("持久层不可用时 refresh 不可达，且不得抛异常");
    }

    // =====================================================================
    // F5：访问令牌剩余 ≤ 0 时不写 access 槽位（TTL 边界）
    // =====================================================================

    [Fact]
    public async Task StoreUserTokenAsync_ShouldSkipAccessSlot_WhenAccessTokenAlreadyExpired()
    {
        // Arrange：真实 store（内存版）以便断言「物理槽位是否写入」。
        var appKey = NewAppKey();
        var tokenType = TokenTypeOf(appKey);
        const string userId = "ou_expired";

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var tenantStore = new FeishuTokenStore(cache, appKey);
        var userStore = new FeishuUserTokenStore(tenantStore, cache, appKey);
        var manager = CreateUserManager(CreateConfig(appKey), userStore);

        var now = NowMs;
        await manager.StoreUserTokenAsync(userId, new UserTokenInfo
        {
            UserId = userId,
            AccessToken = "expired-access",
            RefreshToken = "refresh-only",
            AccessTokenExpireTime = now - 1_000,                 // 已过期
            RefreshTokenExpireTime = now + RefreshLifetimeMs
        });

        // Assert：与改造前 PersistUserTokenAsync 的「剩余 ≤ 0 则只写 refresh」逐条等价，
        // 且不得写成 1 秒 TTL 的垃圾条目（F5）。
        (await userStore.GetAccessTokenAsync(userId, tokenType)).Should().BeNull(
            "剩余 ≤ 0 的访问令牌必须跳过写穿（F5）");
        (await userStore.GetRefreshTokenAsync(userId, tokenType)).Should().NotBeNull(
            "refresh 槽位仍须写入");
    }

    // =====================================================================
    // BD-4：清库门只拦读族；写族 / ClearAllUsersAsync 一律透传
    // =====================================================================

    [Fact]
    public async Task PurgeGateDecorator_ShouldShortCircuitReads_ButPassThroughWritesAndPurge()
    {
        // Arrange：装饰层是本次改造风险最高的部件（漏转发 = 静默失效），此处直测其转发语义。
        var appKey = NewAppKey();
        var tokenType = TokenTypeOf(appKey);
        const string userId = "ou_gate";

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var tenantStore = new FeishuTokenStore(cache, appKey);
        var userStore = new FeishuUserTokenStore(tenantStore, cache, appKey);
        await userStore.SetAccessTokenAsync(userId, tokenType, Encode("live-access", NowMs + AccessLifetimeMs), 3600);
        await userStore.SetRefreshTokenAsync(userId, tokenType, Encode("refresh-1", NowMs + RefreshLifetimeMs));

        var decorated = new PurgeGateUserTokenStoreDecorator(userStore, appKey, NullLogger.Instance);

        try
        {
            TokenStorePurgeGate.Mark(appKey);

            // 读族：门挂起 ⇒ 短路返回未命中（不触达内层，等价于改造前管理器内门判定）
            (await decorated.GetAccessTokenAsync(userId, tokenType)).Should().BeNull(
                "清库门挂起期间读族必须短路（D10）");
            (await decorated.GetRefreshTokenAsync(userId, tokenType)).Should().BeNull(
                "清库门挂起期间读族必须短路（D10）");

            // 写族：一律透传（清库期间的残留由「提交后二次清库」覆盖）
            await decorated.SetAccessTokenAsync(userId, tokenType, Encode("written-while-pending", NowMs + AccessLifetimeMs), 3600);
            (await userStore.GetAccessTokenAsync(userId, tokenType)).Should().NotBeNull(
                "写路径绝不能被清库门拦截（BD-4）");
        }
        finally
        {
            TokenStorePurgeGate.Release(appKey);
        }

        // 门撤除后读恢复
        (await decorated.GetAccessTokenAsync(userId, tokenType)).Should().NotBeNull(
            "撤门后读族必须恢复（fail-open 的对偶：撤门即恢复）");

        // 清库族：透传（被拦会使凭据变更清库静默失效）
        await decorated.ClearAllUsersAsync();
        (await userStore.GetAccessTokenAsync(userId, tokenType)).Should().BeNull(
            "ClearAllUsersAsync 必须透传到内层（BD-4）");
        (await userStore.GetRefreshTokenAsync(userId, tokenType)).Should().BeNull(
            "ClearAllUsersAsync 必须清空用户全部物理键");
    }
}
