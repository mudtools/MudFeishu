// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.DataModels;

namespace Mud.Feishu.Tests.Authentication.TokenManager;

/// <summary>
/// U-7（上游 3.0.10 Batch 3）：TMF-05 清库路径的用户维度 CAS 接入测试。
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>store 提供原子能力（<see cref="IUserTokenStoreCompareExchange"/>）⇒ 走官方 CAS（单次原子调用）；</item>
/// <item>store 无原子能力 ⇒ 保留 C-2「解码明文比较 + 删除」路径
/// （<b>不得</b>改走官方非原子回退：其按「存储原值 vs 期望明文」整值比较，对飞书编码值恒失配）；</item>
/// <item>门装饰器<b>刻意不</b>声明 CAS 能力（避免假命中，能力探测必须落在原始 store 上）。</item>
/// </list>
/// </remarks>
public class UserTokenManagerCompareExchangeTests
{
    private const string TokenTypeKey = "UserAccessToken:test";

    private static long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static FeishuAppConfig CreateConfig() => new()
    {
        AppKey = "test",
        AppId = "test_app_id_1234567890",
        AppSecret = "test_app_secret_123456"
    };

    private static Mock<IFeishuAuthentication> CreateUnretryableAuthMock()
    {
        var mock = new Mock<IFeishuAuthentication>();
        mock.Setup(x => x.GetOAuthenRefreshAccessTokenAsync(
                It.IsAny<OAuthRefreshTokenRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthCredentialsResult { Code = 99991664, Msg = "refresh token has been revoked" });
        return mock;
    }

    private static UserTokenManager CreateManager(Mock<IFeishuAuthentication> auth, IUserTokenStore store)
        => new(
            null,
            auth.Object,
            Options.Create(CreateConfig()),
            new Mock<ILogger<UserTokenManager>>().Object,
            store);

    [Fact]
    public async Task RefreshUserTokenAsync_ShouldUseAtomicCas_WhenStoreProvidesCapability()
    {
        var store = new AtomicUserTokenStore
        {
            EncodedRefresh = TokenStoreHelper.EncodeStoredToken("stored-refresh", NowMs + 30L * 24 * 3600 * 1000)
        };
        var auth = CreateUnretryableAuthMock();
        using var manager = CreateManager(auth, store);

        var result = await manager.RefreshUserTokenAsync("user1", CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(1, store.CompareExchangeRefreshCalls);
        Assert.Equal("stored-refresh", store.LastExpectedToken);
        Assert.Null(store.LastNewToken);
        Assert.Equal(TokenTypeKey, store.LastTokenType);
        Assert.Equal(0, store.RemoveCalls);
    }

    [Fact]
    public async Task RefreshUserTokenAsync_ShouldFallBackToPlaintextCompare_WhenStoreLacksCapability()
    {
        // 无能力 store：既有 C-2 明文比较 + 删除路径（与本次改动前语义一致）。
        var store = new PlainUserTokenStore
        {
            EncodedRefresh = TokenStoreHelper.EncodeStoredToken("stored-refresh", NowMs + 30L * 24 * 3600 * 1000)
        };
        var auth = CreateUnretryableAuthMock();
        using var manager = CreateManager(auth, store);

        var result = await manager.RefreshUserTokenAsync("user1", CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(1, store.RemoveCalls);
    }

    [Fact]
    public async Task RefreshUserTokenAsync_ShouldKeepNewerToken_WhenPlaintextDiffers()
    {
        // 并发刷新场景：第 1 次读（候选加载）返回本次尝试的旧令牌；第 2 次读（CAS 比对）返回
        // 并发刷新刚持久化的新令牌（明文不同）⇒ 不得误删。
        var store = new PlainUserTokenStore
        {
            RefreshReads =
            [
                TokenStoreHelper.EncodeStoredToken("old-refresh", NowMs + 30L * 24 * 3600 * 1000),
                TokenStoreHelper.EncodeStoredToken("newer-refresh", NowMs + 30L * 24 * 3600 * 1000)
            ]
        };
        var auth = CreateUnretryableAuthMock();
        using var manager = CreateManager(auth, store);

        await manager.RefreshUserTokenAsync("user1", CancellationToken.None);

        Assert.Equal(0, store.RemoveCalls);
    }

    [Fact]
    public void PurgeGateUserTokenStoreDecorator_ShouldNotDeclareCasCapability()
    {
        // 设计守卫：装饰器若声明能力接口，管理器的能力探测会在内层未实现时"假命中"，
        // 官方非原子回退随即以整值比较恒失配 ⇒ 清库静默失效。故必须保持不声明。
        var decoratorType = typeof(UserTokenManager).Assembly
            .GetType("Mud.Feishu.Abstractions.Authentication.PurgeGateUserTokenStoreDecorator");

        Assert.NotNull(decoratorType);
        Assert.False(typeof(IUserTokenStoreCompareExchange).IsAssignableFrom(decoratorType!));
    }

    /// <summary>无原子能力的最小用户 store 替身（仅记录删除调用）。</summary>
    private class PlainUserTokenStore : IUserTokenStore
    {
        private int _refreshReadIndex;

        public string? EncodedRefresh { get; set; }

        /// <summary>按读取次序返回的 refresh 存储值序列（超出后重复最后一项）。</summary>
        public IReadOnlyList<string>? RefreshReads { get; set; }

        public int RemoveCalls { get; private set; }

        public Task<string?> GetAccessTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task SetAccessTokenAsync(string userId, string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<string?> GetRefreshTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
        {
            if (RefreshReads is { Count: > 0 })
            {
                var index = Math.Min(_refreshReadIndex++, RefreshReads.Count - 1);
                return Task.FromResult<string?>(RefreshReads[index]);
            }

            return Task.FromResult(EncodedRefresh);
        }

        public Task SetRefreshTokenAsync(string userId, string tokenType, string refreshToken, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
        {
            RemoveCalls++;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<string>> GetTokenTypesAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<string>());

        public Task ClearUserAsync(string userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        // ---- 租户维度成员（上游 IUserTokenStore : ITokenStore，用户维度 store 亦承载应用维度面）----

        public Task<string?> GetAccessTokenAsync(string tokenType, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task SetAccessTokenAsync(string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<string?> GetRefreshTokenAsync(string tokenType, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task SetRefreshTokenAsync(string tokenType, string refreshToken, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAsync(string tokenType, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IEnumerable<string>> GetTokenTypesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<string>());

        public Task ClearAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    /// <summary>带原子 CAS 能力的用户 store 替身。</summary>
    private sealed class AtomicUserTokenStore : PlainUserTokenStore, IUserTokenStoreCompareExchange
    {
        public int CompareExchangeRefreshCalls { get; private set; }

        public string? LastExpectedToken { get; private set; }

        public string? LastNewToken { get; private set; }

        public string? LastTokenType { get; private set; }

        public Task<bool> CompareExchangeAccessTokenAsync(
            string userId, string tokenType, string expectedToken, string? newToken,
            long expiresInSeconds = 0, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<bool> CompareExchangeRefreshTokenAsync(
            string userId, string tokenType, string expectedToken, string? newToken,
            CancellationToken cancellationToken = default)
        {
            CompareExchangeRefreshCalls++;
            LastExpectedToken = expectedToken;
            LastNewToken = newToken;
            LastTokenType = tokenType;
            return Task.FromResult(true);
        }
    }
}
