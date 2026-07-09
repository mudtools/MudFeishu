// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using Mud.Feishu.Abstractions.Authentication;
using Mud.HttpUtils;

namespace Mud.Feishu.Tests.Authentication.TokenManager;

public class FeishuTokenStoreTests : IDisposable
{
    private readonly MemoryCache _cache;
    private readonly FeishuTokenStore _sut;

    public FeishuTokenStoreTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new FeishuTokenStore(_cache);
    }

    public void Dispose()
    {
        _cache?.Dispose();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCacheIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new FeishuTokenStore(null!));
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldReturnNull_WhenTokenNotExists()
    {
        var result = await _sut.GetAccessTokenAsync("TenantAccessToken");
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldStoreToken()
    {
        await _sut.SetAccessTokenAsync("TenantAccessToken", "test-token-123", 7200);

        var result = await _sut.GetAccessTokenAsync("TenantAccessToken");
        Assert.Equal("test-token-123", result);
    }

    [Fact]
    public async Task GetRefreshTokenAsync_ShouldReturnNull_WhenTokenNotExists()
    {
        var result = await _sut.GetRefreshTokenAsync("TenantAccessToken");
        Assert.Null(result);
    }

    [Fact]
    public async Task SetRefreshTokenAsync_ShouldStoreToken()
    {
        await _sut.SetRefreshTokenAsync("TenantAccessToken", "refresh-token-456");

        var result = await _sut.GetRefreshTokenAsync("TenantAccessToken");
        Assert.Equal("refresh-token-456", result);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveBothAccessAndRefreshTokens()
    {
        await _sut.SetAccessTokenAsync("TenantAccessToken", "access-token", 7200);
        await _sut.SetRefreshTokenAsync("TenantAccessToken", "refresh-token");

        await _sut.RemoveAsync("TenantAccessToken");

        var accessResult = await _sut.GetAccessTokenAsync("TenantAccessToken");
        var refreshResult = await _sut.GetRefreshTokenAsync("TenantAccessToken");
        Assert.Null(accessResult);
        Assert.Null(refreshResult);
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldIsolateDifferentTokenTypes()
    {
        await _sut.SetAccessTokenAsync("TenantAccessToken", "tenant-token", 7200);
        await _sut.SetAccessTokenAsync("AppAccessToken", "app-token", 7200);

        var tenantResult = await _sut.GetAccessTokenAsync("TenantAccessToken");
        var appResult = await _sut.GetAccessTokenAsync("AppAccessToken");
        Assert.Equal("tenant-token", tenantResult);
        Assert.Equal("app-token", appResult);
    }

    [Fact]
    public async Task RemoveAsync_ShouldOnlyRemoveSpecifiedTokenType()
    {
        await _sut.SetAccessTokenAsync("TenantAccessToken", "tenant-token", 7200);
        await _sut.SetAccessTokenAsync("AppAccessToken", "app-token", 7200);

        await _sut.RemoveAsync("TenantAccessToken");

        var tenantResult = await _sut.GetAccessTokenAsync("TenantAccessToken");
        var appResult = await _sut.GetAccessTokenAsync("AppAccessToken");
        Assert.Null(tenantResult);
        Assert.Equal("app-token", appResult);
    }

    [Fact]
    public async Task GetTokenTypesAsync_ShouldReturnEmpty_WhenNoTokensStored()
    {
        var result = await _sut.GetTokenTypesAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTokenTypesAsync_ShouldReturnStoredTokenTypes()
    {
        await _sut.SetAccessTokenAsync("TenantAccessToken", "tenant-token", 7200);
        await _sut.SetAccessTokenAsync("AppAccessToken", "app-token", 7200);

        var result = await _sut.GetTokenTypesAsync();
        Assert.Equal(2, result.Count());
        Assert.Contains("TenantAccessToken", result);
        Assert.Contains("AppAccessToken", result);
    }

    [Fact]
    public async Task GetTokenTypesAsync_ShouldNotReturnRemovedTokenTypes()
    {
        await _sut.SetAccessTokenAsync("TenantAccessToken", "tenant-token", 7200);
        await _sut.SetAccessTokenAsync("AppAccessToken", "app-token", 7200);
        await _sut.RemoveAsync("TenantAccessToken");

        var result = await _sut.GetTokenTypesAsync();
        Assert.Single(result);
        Assert.Contains("AppAccessToken", result);
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllTokens()
    {
        await _sut.SetAccessTokenAsync("TenantAccessToken", "tenant-token", 7200);
        await _sut.SetAccessTokenAsync("AppAccessToken", "app-token", 7200);
        await _sut.SetRefreshTokenAsync("TenantAccessToken", "refresh-token");

        await _sut.ClearAsync();

        var tenantAccess = await _sut.GetAccessTokenAsync("TenantAccessToken");
        var appAccess = await _sut.GetAccessTokenAsync("AppAccessToken");
        var tenantRefresh = await _sut.GetRefreshTokenAsync("TenantAccessToken");
        Assert.Null(tenantAccess);
        Assert.Null(appAccess);
        Assert.Null(tenantRefresh);
    }

    [Fact]
    public async Task ClearAsync_ShouldClearTokenTypes()
    {
        await _sut.SetAccessTokenAsync("TenantAccessToken", "tenant-token", 7200);
        await _sut.ClearAsync();

        var result = await _sut.GetTokenTypesAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldThrowArgumentOutOfRangeException_WhenExpiresInSecondsIsZero()
    {
        // M-3 修复：非法 expiry 值（0 或负数）应抛出 ArgumentOutOfRangeException，保留原始 API 契约。
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.SetAccessTokenAsync("TenantAccessToken", "token", 0));
        Assert.Equal("expiresInSeconds", ex.ParamName);
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldThrowArgumentOutOfRangeException_WhenExpiresInSecondsIsNegative()
    {
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.SetAccessTokenAsync("TenantAccessToken", "token", -100));
        Assert.Equal("expiresInSeconds", ex.ParamName);
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldSucceed_WhenExpiresInSecondsIsOne()
    {
        // M-3 修复边界：expiresInSeconds=1 时 (long)(1 * 0.9)=0，Math.Max(1, ...) 防止 IMemoryCache 抛异常。
        await _sut.SetAccessTokenAsync("TenantAccessToken", "boundary-token", 1);

        var result = await _sut.GetAccessTokenAsync("TenantAccessToken");
        Assert.Equal("boundary-token", result);
    }
}

public class FeishuUserTokenStoreTests : IDisposable
{
    private readonly MemoryCache _cache;
    private readonly FeishuTokenStore _innerStore;
    private readonly FeishuUserTokenStore _sut;

    public FeishuUserTokenStoreTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _innerStore = new FeishuTokenStore(_cache);
        _sut = new FeishuUserTokenStore(_innerStore, _cache);
    }

    public void Dispose()
    {
        _cache?.Dispose();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCacheIsNull()
    {
        var innerStore = new FeishuTokenStore(new MemoryCache(new MemoryCacheOptions()));
        Assert.Throws<ArgumentNullException>(() => new FeishuUserTokenStore(innerStore, null!));
    }

    [Fact]
    public async Task GetAccessTokenAsync_ShouldReturnNull_WhenTokenNotExists()
    {
        var result = await _sut.GetAccessTokenAsync("UserAccessToken");
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAccessTokenAsync_ShouldStoreToken()
    {
        await _sut.SetAccessTokenAsync("UserAccessToken", "test-token-123", 7200);

        var result = await _sut.GetAccessTokenAsync("UserAccessToken");
        Assert.Equal("test-token-123", result);
    }

    [Fact]
    public async Task GetRefreshTokenAsync_ShouldReturnNull_WhenTokenNotExists()
    {
        var result = await _sut.GetRefreshTokenAsync("UserAccessToken");
        Assert.Null(result);
    }

    [Fact]
    public async Task SetRefreshTokenAsync_ShouldStoreToken()
    {
        await _sut.SetRefreshTokenAsync("UserAccessToken", "refresh-token-456");

        var result = await _sut.GetRefreshTokenAsync("UserAccessToken");
        Assert.Equal("refresh-token-456", result);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveBothAccessAndRefreshTokens()
    {
        await _sut.SetAccessTokenAsync("UserAccessToken", "access-token", 7200);
        await _sut.SetRefreshTokenAsync("UserAccessToken", "refresh-token");

        await _sut.RemoveAsync("UserAccessToken");

        var accessResult = await _sut.GetAccessTokenAsync("UserAccessToken");
        var refreshResult = await _sut.GetRefreshTokenAsync("UserAccessToken");
        Assert.Null(accessResult);
        Assert.Null(refreshResult);
    }

    [Fact]
    public async Task GetUserAccessTokenAsync_ShouldReturnNull_WhenTokenNotExists()
    {
        var result = await _sut.GetAccessTokenAsync("user1", "UserAccessToken");
        Assert.Null(result);
    }

    [Fact]
    public async Task SetUserAccessTokenAsync_ShouldStoreToken()
    {
        await _sut.SetAccessTokenAsync("user1", "UserAccessToken", "user-access-123", 7200);

        var result = await _sut.GetAccessTokenAsync("user1", "UserAccessToken");
        Assert.Equal("user-access-123", result);
    }

    [Fact]
    public async Task GetUserRefreshTokenAsync_ShouldReturnNull_WhenTokenNotExists()
    {
        var result = await _sut.GetRefreshTokenAsync("user1", "UserAccessToken");
        Assert.Null(result);
    }

    [Fact]
    public async Task SetUserRefreshTokenAsync_ShouldStoreToken()
    {
        await _sut.SetRefreshTokenAsync("user1", "UserAccessToken", "user-refresh-456");

        var result = await _sut.GetRefreshTokenAsync("user1", "UserAccessToken");
        Assert.Equal("user-refresh-456", result);
    }

    [Fact]
    public async Task RemoveUserAsync_ShouldRemoveBothAccessAndRefreshTokens()
    {
        await _sut.SetAccessTokenAsync("user1", "UserAccessToken", "access-token", 7200);
        await _sut.SetRefreshTokenAsync("user1", "UserAccessToken", "refresh-token");

        await _sut.RemoveAsync("user1", "UserAccessToken");

        var accessResult = await _sut.GetAccessTokenAsync("user1", "UserAccessToken");
        var refreshResult = await _sut.GetRefreshTokenAsync("user1", "UserAccessToken");
        Assert.Null(accessResult);
        Assert.Null(refreshResult);
    }

    [Fact]
    public async Task UserTokens_ShouldBeIsolatedByUserId()
    {
        await _sut.SetAccessTokenAsync("user1", "UserAccessToken", "user1-token", 7200);
        await _sut.SetAccessTokenAsync("user2", "UserAccessToken", "user2-token", 7200);

        var user1Result = await _sut.GetAccessTokenAsync("user1", "UserAccessToken");
        var user2Result = await _sut.GetAccessTokenAsync("user2", "UserAccessToken");
        Assert.Equal("user1-token", user1Result);
        Assert.Equal("user2-token", user2Result);
    }

    [Fact]
    public async Task RemoveUserAsync_ShouldOnlyRemoveSpecifiedUser()
    {
        await _sut.SetAccessTokenAsync("user1", "UserAccessToken", "user1-token", 7200);
        await _sut.SetAccessTokenAsync("user2", "UserAccessToken", "user2-token", 7200);

        await _sut.RemoveAsync("user1", "UserAccessToken");

        var user1Result = await _sut.GetAccessTokenAsync("user1", "UserAccessToken");
        var user2Result = await _sut.GetAccessTokenAsync("user2", "UserAccessToken");
        Assert.Null(user1Result);
        Assert.Equal("user2-token", user2Result);
    }

    [Fact]
    public async Task GetTokenTypesAsync_ShouldReturnEmpty_WhenNoUserTokensStored()
    {
        var result = await _sut.GetTokenTypesAsync("user1");
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTokenTypesAsync_ShouldReturnStoredTokenTypesForUser()
    {
        await _sut.SetAccessTokenAsync("user1", "UserAccessToken", "token1", 7200);
        await _sut.SetAccessTokenAsync("user1", "CustomToken", "token2", 7200);

        var result = await _sut.GetTokenTypesAsync("user1");
        Assert.Equal(2, result.Count());
        Assert.Contains("UserAccessToken", result);
        Assert.Contains("CustomToken", result);
    }

    [Fact]
    public async Task GetTokenTypesAsync_ShouldIsolateByUser()
    {
        await _sut.SetAccessTokenAsync("user1", "UserAccessToken", "token1", 7200);
        await _sut.SetAccessTokenAsync("user2", "UserAccessToken", "token2", 7200);

        var user1Result = await _sut.GetTokenTypesAsync("user1");
        var user2Result = await _sut.GetTokenTypesAsync("user2");
        Assert.Single(user1Result);
        Assert.Single(user2Result);
    }

    [Fact]
    public async Task ClearUserAsync_ShouldRemoveAllTokensForUser()
    {
        await _sut.SetAccessTokenAsync("user1", "UserAccessToken", "access-token", 7200);
        await _sut.SetRefreshTokenAsync("user1", "UserAccessToken", "refresh-token");
        await _sut.SetAccessTokenAsync("user2", "UserAccessToken", "user2-token", 7200);

        await _sut.ClearUserAsync("user1");

        var user1Access = await _sut.GetAccessTokenAsync("user1", "UserAccessToken");
        var user1Refresh = await _sut.GetRefreshTokenAsync("user1", "UserAccessToken");
        var user2Access = await _sut.GetAccessTokenAsync("user2", "UserAccessToken");
        Assert.Null(user1Access);
        Assert.Null(user1Refresh);
        Assert.Equal("user2-token", user2Access);
    }

    [Fact]
    public async Task ClearUserAsync_ShouldClearTokenTypesForUser()
    {
        await _sut.SetAccessTokenAsync("user1", "UserAccessToken", "token", 7200);
        await _sut.ClearUserAsync("user1");

        var result = await _sut.GetTokenTypesAsync("user1");
        Assert.Empty(result);
    }
}
