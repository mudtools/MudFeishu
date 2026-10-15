// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET6_0_OR_GREATER

using Microsoft.Extensions.Logging;
using Moq;
using Mud.Feishu.Abstractions.Authentication;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// U-4（上游 3.0.10 Batch 2）：<see cref="EncryptedTokenStore"/> 的
/// <see cref="ITokenStoreAppPurge"/> 能力透传测试 ——
/// 加密只作用于值、不改变键布局，官方统一入口
/// <c>TokenStorePurgeExtensions.PurgeAppTokensAsync</c> 经能力探测可穿透本装饰器。
/// </summary>
public class EncryptedTokenStorePurgeCapabilityTests
{
    private static EncryptedTokenStore CreateStore(ITokenStore inner)
        => new(inner, new Mock<IEncryptionProvider>().Object, new Mock<ILogger>().Object);

    /// <summary>
    /// 内层实现能力面：加密装饰器必须原样透传（EnumerateKeys / PurgeApp 均直达内层）。
    /// </summary>
    [Fact]
    public async Task PurgeCapability_ShouldDelegate_ToInner_WhenInnerImplementsIt()
    {
        var inner = new PurgeableInnerStore(purgeResult: 7, keys: new[] { "k1", "k2" });
        var store = CreateStore(inner);

        var keys = await store.EnumerateKeysAsync("app");
        var deleted = await store.PurgeAppAsync("app");

        Assert.Equal(new[] { "k1", "k2" }, keys);
        Assert.Equal(7, deleted);
        Assert.Equal(1, inner.PurgeCalls);
        Assert.Equal(1, inner.EnumerateCalls);
    }

    /// <summary>
    /// 内层未实现能力面：EnumerateKeys 回退 tokenType 面，PurgeApp 经官方统一入口回退
    /// （GetTokenTypesAsync → 逐类型 RemoveAsync，计数 = tokenType 数）。
    /// </summary>
    [Fact]
    public async Task PurgeCapability_ShouldFallBack_WhenInnerLacksIt()
    {
        var inner = new Mock<ITokenStore>();
        inner.Setup(s => s.GetTokenTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "TenantAccessToken:app", "AppAccessToken:app" });
        inner.Setup(s => s.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var store = CreateStore(inner.Object);

        var keys = await store.EnumerateKeysAsync("app");
        var deleted = await store.PurgeAppAsync("app");

        Assert.Equal(new[] { "TenantAccessToken:app", "AppAccessToken:app" }, keys);
        Assert.Equal(2, deleted);
        inner.Verify(s => s.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    /// <summary>
    /// 可清库内层替身：实现 <see cref="ITokenStore"/> + <see cref="ITokenStoreAppPurge"/>，
    /// 记录调用次数供透传断言。
    /// </summary>
    private sealed class PurgeableInnerStore : ITokenStore, ITokenStoreAppPurge
    {
        private readonly int _purgeResult;
        private readonly IReadOnlyCollection<string> _keys;

        public PurgeableInnerStore(int purgeResult, IReadOnlyCollection<string> keys)
        {
            _purgeResult = purgeResult;
            _keys = keys;
        }

        public int PurgeCalls { get; private set; }

        public int EnumerateCalls { get; private set; }

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
            => Task.FromResult<IEnumerable<string>>(_keys);

        public Task ClearAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyCollection<string>> EnumerateKeysAsync(string appKey, CancellationToken cancellationToken = default)
        {
            EnumerateCalls++;
            return Task.FromResult(_keys);
        }

        public Task<int> PurgeAppAsync(string appKey, CancellationToken cancellationToken = default)
        {
            PurgeCalls++;
            return Task.FromResult(_purgeResult);
        }
    }
}

#endif
