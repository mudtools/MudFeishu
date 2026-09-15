// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Tests.Helpers;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// 验证令牌存储加密装饰器（ENH-1）。
/// </summary>
public class EncryptedTokenStoreTests
{
    // ---------------------------------------------------------------- 装饰器本体

    [Fact]
    public async Task SetAccessTokenAsync_ShouldStoreCipherText_WhenEncryptionEnabled()
    {
        var inner = new InMemoryTokenStore();
        var store = new EncryptedTokenStore(inner, new TestEncryptionProvider());

        await store.SetAccessTokenAsync("tenant:app1", "plain_access_token", 3600);

        inner.RawAccessToken("tenant:app1").Should().NotBe("plain_access_token",
            "令牌不得以明文写入存储后端");
        inner.RawAccessToken("tenant:app1").Should().StartWith("enc:");
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldReturnPlainText_WhenCipherTextStored()
    {
        var store = new EncryptedTokenStore(new InMemoryTokenStore(), new TestEncryptionProvider());

        await store.SetAccessTokenAsync("tenant:app1", "plain_access_token", 3600);

        (await store.GetAccessTokenAsync("tenant:app1")).Should().Be("plain_access_token");
    }

    [Fact]
    public async Task RefreshToken_ShouldRoundTrip_ThroughEncryption()
    {
        var inner = new InMemoryTokenStore();
        var store = new EncryptedTokenStore(inner, new TestEncryptionProvider());

        await store.SetRefreshTokenAsync("tenant:app1", "plain_refresh_token");

        inner.RawRefreshToken("tenant:app1").Should().StartWith("enc:");
        (await store.GetRefreshTokenAsync("tenant:app1")).Should().Be("plain_refresh_token");
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldReturnNull_WhenCipherTextCannotBeDecrypted()
    {
        var inner = new InMemoryTokenStore();
        // 模拟旧明文数据 / 密钥轮换：底层存的是无法解密的明文
        await inner.SetAccessTokenAsync("tenant:app1", "legacy_plain_text", 3600);

        var store = new EncryptedTokenStore(inner, new TestEncryptionProvider());

        (await store.GetAccessTokenAsync("tenant:app1")).Should().BeNull(
            "解密失败应按缓存未命中处理，使上层回退到重新获取令牌，而不是让请求链路抛异常");
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldReturnNull_WhenTokenMissing()
    {
        var store = new EncryptedTokenStore(new InMemoryTokenStore(), new TestEncryptionProvider());

        (await store.GetAccessTokenAsync("tenant:missing")).Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenArgumentsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() => new EncryptedTokenStore(null!, new TestEncryptionProvider()));
        Assert.Throws<ArgumentNullException>(() => new EncryptedTokenStore(new InMemoryTokenStore(), null!));
    }

    // ---------------------------------------------------------------- 工厂装饰器

    [Fact]
    public async Task Factory_ShouldEncryptBothTenantAndUserTokens()
    {
        var innerTenant = new InMemoryTokenStore();
        var innerUser = new InMemoryUserTokenStore();
        var factory = new EncryptedFeishuTokenStoreFactory(
            new FakeTokenStoreFactory(innerTenant, innerUser),
            new TestEncryptionProvider());

        var (tokenStore, userTokenStore) = factory.Create("cli_a");

        tokenStore.Should().BeOfType<EncryptedTokenStore>();
        userTokenStore.Should().BeOfType<EncryptedUserTokenStore>();

        await tokenStore.SetAccessTokenAsync("tenant:cli_a", "tenant_secret", 3600);
        await userTokenStore!.SetAccessTokenAsync("ou_1", "user", "user_secret", 3600);

        innerTenant.RawAccessToken("tenant:cli_a").Should().StartWith("enc:");
        innerUser.RawAccessToken("ou_1", "user").Should().StartWith("enc:");

        (await tokenStore.GetAccessTokenAsync("tenant:cli_a")).Should().Be("tenant_secret");
        (await userTokenStore.GetAccessTokenAsync("ou_1", "user")).Should().Be("user_secret");
    }

    [Fact]
    public async Task Factory_UserTokenStore_ShouldRouteInheritedTokenStoreMembersToEncryptedTenantStore()
    {
        var innerTenant = new InMemoryTokenStore();
        var factory = new EncryptedFeishuTokenStoreFactory(
            new FakeTokenStoreFactory(innerTenant, new InMemoryUserTokenStore()),
            new TestEncryptionProvider());

        var (_, userTokenStore) = factory.Create("cli_a");

        // IUserTokenStore 继承 ITokenStore；这些租户维度成员必须同样经过加密，
        // 否则同一存储会出现「一处加密、一处明文」的不一致。
        await userTokenStore!.SetAccessTokenAsync("app:cli_a", "app_secret", 3600);

        innerTenant.RawAccessToken("app:cli_a").Should().StartWith("enc:");
        (await userTokenStore.GetAccessTokenAsync("app:cli_a")).Should().Be("app_secret");
    }

    [Fact]
    public void Factory_ShouldReturnNullUserStore_WhenInnerHasNoUserStore()
    {
        var factory = new EncryptedFeishuTokenStoreFactory(
            new FakeTokenStoreFactory(new InMemoryTokenStore(), null),
            new TestEncryptionProvider());

        var (_, userTokenStore) = factory.Create("cli_a");

        userTokenStore.Should().BeNull();
    }

    // ---------------------------------------------------------------- 选项与 DI 接线

    [Fact]
    public void FeishuAppOptions_ShouldDefaultToEncryptionDisabled()
    {
        new FeishuAppOptions().EnableTokenEncryption.Should().BeFalse(
            "加密属存储策略选择，默认关闭以避免对现有部署产生非预期影响");
    }

    [Fact]
    public void DI_ShouldWrapFactory_WhenEncryptionEnabledAndProviderRegistered()
    {
        using var provider = BuildProvider(
            configureOptions: o => o.EnableTokenEncryption = true,
            registerEncryption: true);

        provider.GetRequiredService<IFeishuTokenStoreFactory>()
            .Should().BeOfType<EncryptedFeishuTokenStoreFactory>();

        var (tokenStore, userTokenStore) = provider.GetRequiredService<IFeishuTokenStoreFactory>().Create("cli_test");
        tokenStore.Should().BeOfType<EncryptedTokenStore>();
        userTokenStore.Should().BeOfType<EncryptedUserTokenStore>();
    }

    [Fact]
    public void DI_ShouldNotWrapFactory_WhenEncryptionDisabled()
    {
        using var provider = BuildProvider(
            configureOptions: null,
            registerEncryption: true);

        provider.GetRequiredService<IFeishuTokenStoreFactory>()
            .Should().NotBeOfType<EncryptedFeishuTokenStoreFactory>();
    }

    [Fact]
    public void DI_ShouldNotWrapFactory_WhenProviderNotRegistered()
    {
        using var provider = BuildProvider(
            configureOptions: o => o.EnableTokenEncryption = true,
            registerEncryption: false);

        // 未注册 IEncryptionProvider 时降级为明文（并记录警告），而不是抛异常阻断启动
        provider.GetRequiredService<IFeishuTokenStoreFactory>()
            .Should().NotBeOfType<EncryptedFeishuTokenStoreFactory>();
    }

    private static ServiceProvider BuildProvider(
        Action<FeishuAppOptions>? configureOptions,
        bool registerEncryption)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = TestDataFactory.AppConfigs.AppKeys.Default,
                AppId = TestDataFactory.AppConfigs.AppIds.Default,
                AppSecret = TestDataFactory.AppConfigs.Secrets.Valid,
                IsDefault = true
            }
        });

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        if (registerEncryption)
        {
            services.AddSingleton<IEncryptionProvider>(new TestEncryptionProvider());
        }

        return services.BuildServiceProvider();
    }

    // ---------------------------------------------------------------- 测试替身

    /// <summary>可逆的假加密提供程序：密文以 <c>enc:</c> 前缀标识，非该前缀视为「无法解密」。</summary>
    private sealed class TestEncryptionProvider : IEncryptionProvider
    {
        private const string Prefix = "enc:";

        public string Encrypt(string plainText) =>
            Prefix + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText ?? string.Empty));

        public string Decrypt(string cipherText)
        {
            if (cipherText == null || !cipherText.StartsWith(Prefix, StringComparison.Ordinal))
            {
                throw new FormatException("不是本提供程序产生的密文");
            }

            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cipherText.Substring(Prefix.Length)));
        }

        public byte[] EncryptBytes(byte[] data) => data ?? Array.Empty<byte>();

        public byte[] DecryptBytes(byte[] encryptedData) => encryptedData ?? Array.Empty<byte>();
    }

    private sealed class InMemoryTokenStore : ITokenStore
    {
        private readonly Dictionary<string, string> _access = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _refresh = new(StringComparer.Ordinal);

        public string? RawAccessToken(string tokenType) => _access.TryGetValue(tokenType, out var v) ? v : null;

        public string? RawRefreshToken(string tokenType) => _refresh.TryGetValue(tokenType, out var v) ? v : null;

        public Task<string?> GetAccessTokenAsync(string tokenType, CancellationToken cancellationToken = default)
            => Task.FromResult(RawAccessToken(tokenType));

        public Task SetAccessTokenAsync(string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
        {
            _access[tokenType] = accessToken;
            return Task.CompletedTask;
        }

        public Task<string?> GetRefreshTokenAsync(string tokenType, CancellationToken cancellationToken = default)
            => Task.FromResult(RawRefreshToken(tokenType));

        public Task SetRefreshTokenAsync(string tokenType, string refreshToken, CancellationToken cancellationToken = default)
        {
            _refresh[tokenType] = refreshToken;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string tokenType, CancellationToken cancellationToken = default)
        {
            _access.Remove(tokenType);
            _refresh.Remove(tokenType);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<string>> GetTokenTypesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<string>>(_access.Keys.ToList());

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _access.Clear();
            _refresh.Clear();
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryUserTokenStore : IUserTokenStore
    {
        private readonly Dictionary<(string UserId, string TokenType), string> _access = new();

        public string? RawAccessToken(string userId, string tokenType)
            => _access.TryGetValue((userId, tokenType), out var v) ? v : null;

        public Task<string?> GetAccessTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
            => Task.FromResult(RawAccessToken(userId, tokenType));

        public Task SetAccessTokenAsync(string userId, string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
        {
            _access[(userId, tokenType)] = accessToken;
            return Task.CompletedTask;
        }

        public Task<string?> GetRefreshTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task SetRefreshTokenAsync(string userId, string tokenType, string refreshToken, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
        {
            _access.Remove((userId, tokenType));
            return Task.CompletedTask;
        }

        public Task<IEnumerable<string>> GetTokenTypesAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<string>>(Array.Empty<string>());

        public Task ClearUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            foreach (var key in _access.Keys.Where(k => k.UserId == userId).ToList())
            {
                _access.Remove(key);
            }

            return Task.CompletedTask;
        }

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
            => Task.FromResult<IEnumerable<string>>(Array.Empty<string>());

        public Task ClearAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeTokenStoreFactory : IFeishuTokenStoreFactory
    {
        private readonly ITokenStore _tenant;
        private readonly IUserTokenStore? _user;

        public FakeTokenStoreFactory(ITokenStore tenant, IUserTokenStore? user)
        {
            _tenant = tenant;
            _user = user;
        }

        public (ITokenStore TokenStore, IUserTokenStore? UserTokenStore) Create(string appKey) => (_tenant, _user);
    }
}
