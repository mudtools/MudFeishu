// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels;

namespace Mud.Feishu.Tests.Authentication.TokenManager;

public class UserTokenManagerTests : TokenManagerTestsBase
{
    private readonly IFeishuUserTokenManager _userTokenManager;

    public UserTokenManagerTests() : base()
    {
        _userTokenManager = AppContext.UserTokenManager;
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowArgumentException_WhenUserIdIsNull()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _userTokenManager.GetTokenAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnNull_WhenNoTokenInCache()
    {
        var result = await _userTokenManager.GetTokenAsync("user123", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task HasValidTokenAsync_ShouldReturnFalse_WhenNoTokenInCache()
    {
        var result = await _userTokenManager.HasValidTokenAsync("user123", CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserTokenWithCodeAsync_ShouldThrowFeishuException_WhenApiReturnsNull()
    {
        _authenticationApiMock
            .Setup(x => x.GetOAuthenAccessTokenAsync(It.IsAny<OAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthCredentialsResult?)null);

        var exception = await Assert.ThrowsAsync<FeishuException>(() => 
            _userTokenManager.GetUserTokenWithCodeAsync("test-code", "https://example.com/callback", CancellationToken.None));

        Assert.Contains("返回结果为null", exception.Message);
    }
}
