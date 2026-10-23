// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录下的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;

namespace Mud.Feishu.Tests.Authentication.TokenManager;

/// <summary>
/// 用户访问令牌失效必须落持久层（内存墓碑只覆盖本实例）。
/// </summary>
/// <remarks>
/// 修复前 <c>InvalidateUserTokenAsync</c> 仅清内存镜像，持久层访问令牌仍可被本实例之外的
/// 读穿透或冷启动恢复，使失效被静默撤销。本类锁定：原始 store 提供
/// <see cref="IUserTokenStoreAccessTokenInvalidation"/> 时删除 access 键、保留 refresh 键，
/// 且持久层失败不改变方法的非抛出契约。
/// </remarks>
public class UserTokenInvalidationPersistenceTests
{
    private const long AccessLifetimeMs = 3_600_000;
    private const long RefreshLifetimeMs = 2_592_000_000;

    private static long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static FeishuAppConfig CreateConfig(string appKey) => new()
    {
        AppKey = appKey,
        AppId = "cli_" + appKey,
        AppSecret = "secret_" + appKey,
        TokenRefreshThreshold = 300
    };

    private static UserTokenManager CreateUserManager(FeishuAppConfig config, IUserTokenStore store)
        => new(
            currentUserContext: null,
            authenticationApi: new Mock<IFeishuAuthentication>().Object,
            options: Options.Create(config),
            logger: NullLogger<UserTokenManager>.Instance,
            userTokenStore: store);

    [Fact]
    public async Task InvalidateUserTokenAsync_ShouldRemovePersistedAccessKey_AndKeepRefresh()
    {
        // Arrange：真实内存 store，持久层同时持有 access + refresh。
        var appKey = "invalidate-" + Guid.NewGuid().ToString("N");
        var tokenType = $"UserAccessToken:{appKey}";
        const string userId = "ou_persist";

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var tenantStore = new FeishuTokenStore(cache, appKey);
        var userStore = new FeishuUserTokenStore(tenantStore, cache, appKey);

        await userStore.SetAccessTokenAsync(
            userId, tokenType, TokenStoreHelper.EncodeStoredToken("rejected-access", NowMs + AccessLifetimeMs), 3600);
        await userStore.SetRefreshTokenAsync(
            userId, tokenType, TokenStoreHelper.EncodeStoredToken("refresh-1", NowMs + RefreshLifetimeMs));

        var manager = CreateUserManager(CreateConfig(appKey), userStore);

        // Act
        await manager.InvalidateUserTokenAsync(userId);

        // Assert：持久层访问令牌被删除（其他实例 / 冷启动不再复活）；refresh 保留供续期。
        (await userStore.GetAccessTokenAsync(userId, tokenType))
            .Should().BeNull("失效必须落持久层，否则多实例场景下被拒绝的访问令牌仍可被恢复");
        (await userStore.GetRefreshTokenAsync(userId, tokenType))
            .Should().NotBeNull("refresh_token 是唯一续期凭据，失效不得连带删除");
    }

    [Fact]
    public async Task InvalidateUserTokenAsync_ShouldDegradeWithoutThrowing_WhenPersistedInvalidationFails()
    {
        // Arrange：store 声明能力但删除抛错（存储抖动）。
        var appKey = "invalidate-fault-" + Guid.NewGuid().ToString("N");
        var tokenType = $"UserAccessToken:{appKey}";
        const string userId = "ou_fault";

        var store = new Mock<IUserTokenStore>();
        var invalidatable = store.As<IUserTokenStoreAccessTokenInvalidation>();
        invalidatable
            .Setup(x => x.InvalidateAccessTokenAsync(userId, tokenType, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("store down"));

        var manager = CreateUserManager(CreateConfig(appKey), store.Object);

        // Act / Assert：失效属尽力而为，存储故障不得冒泡给业务调用方。
        var act = () => manager.InvalidateUserTokenAsync(userId);
        await act.Should().NotThrowAsync();

        invalidatable.Verify(
            x => x.InvalidateAccessTokenAsync(userId, tokenType, It.IsAny<CancellationToken>()),
            Times.Once,
            "能力探测命中后必须实际调用持久层失效");
    }
}
