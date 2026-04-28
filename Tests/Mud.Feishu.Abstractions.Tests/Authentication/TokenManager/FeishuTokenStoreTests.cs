// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
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
}

public class FeishuUserTokenStoreTests : IDisposable
{
    private readonly MemoryCache _cache;
    private readonly FeishuUserTokenStore _sut;

    public FeishuUserTokenStoreTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new FeishuUserTokenStore(_cache);
    }

    public void Dispose()
    {
        _cache?.Dispose();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCacheIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new FeishuUserTokenStore(null!));
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
}
