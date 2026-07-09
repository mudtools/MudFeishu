// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// FeishuTokenTypes 常量类单元测试。
/// 验证飞书令牌类型常量的值正确性和使用场景。
/// </summary>
public class FeishuTokenTypesTests
{
    // ============================================================
    // 常量值验证
    // ============================================================

    /// <summary>
    /// TenantAccessToken 常量值应为 "TenantAccessToken"。
    /// </summary>
    [Fact]
    public void TenantAccessToken_ShouldHaveCorrectValue()
    {
        const string expected = "TenantAccessToken";
        Assert.Equal(expected, FeishuTokenTypes.TenantAccessToken);
    }

    /// <summary>
    /// AppAccessToken 常量值应为 "AppAccessToken"。
    /// </summary>
    [Fact]
    public void AppAccessToken_ShouldHaveCorrectValue()
    {
        const string expected = "AppAccessToken";
        Assert.Equal(expected, FeishuTokenTypes.AppAccessToken);
    }

    /// <summary>
    /// UserAccessToken 常量值应为 "UserAccessToken"。
    /// </summary>
    [Fact]
    public void UserAccessToken_ShouldHaveCorrectValue()
    {
        const string expected = "UserAccessToken";
        Assert.Equal(expected, FeishuTokenTypes.UserAccessToken);
    }

    // ============================================================
    // 常量唯一性验证
    // ============================================================

    /// <summary>
    /// 三种令牌类型常量应互不相同，避免查找冲突。
    /// </summary>
    [Fact]
    public void AllTokenTypes_ShouldBeDistinct()
    {
        var values = new[]
        {
            FeishuTokenTypes.TenantAccessToken,
            FeishuTokenTypes.AppAccessToken,
            FeishuTokenTypes.UserAccessToken
        };

        var distinctCount = values.Distinct().Count();
        Assert.Equal(values.Length, distinctCount);
    }

    /// <summary>
    /// 所有常量应为非空非空白字符串。
    /// </summary>
    [Fact]
    public void AllTokenTypes_ShouldBeNonEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(FeishuTokenTypes.TenantAccessToken));
        Assert.False(string.IsNullOrWhiteSpace(FeishuTokenTypes.AppAccessToken));
        Assert.False(string.IsNullOrWhiteSpace(FeishuTokenTypes.UserAccessToken));
    }

    // ============================================================
    // 与 TokenAttribute 集成验证
    // ============================================================

    /// <summary>
    /// FeishuTokenTypes 常量应可直接用于 TokenAttribute 构造函数。
    /// </summary>
    [Fact]
    public void FeishuTokenTypes_ShouldBeUsableWithTokenAttribute()
    {
        // 验证常量可作为 TokenAttribute 的 TokenType 参数
        var tenantAttr = new Mud.HttpUtils.Attributes.TokenAttribute(FeishuTokenTypes.TenantAccessToken);
        Assert.Equal(FeishuTokenTypes.TenantAccessToken, tenantAttr.TokenType);

        var appAttr = new Mud.HttpUtils.Attributes.TokenAttribute(FeishuTokenTypes.AppAccessToken);
        Assert.Equal(FeishuTokenTypes.AppAccessToken, appAttr.TokenType);

        var userAttr = new Mud.HttpUtils.Attributes.TokenAttribute(FeishuTokenTypes.UserAccessToken);
        Assert.Equal(FeishuTokenTypes.UserAccessToken, userAttr.TokenType);
    }

    // ============================================================
    // 与 FeishuAppContext.GetTokenManager 集成验证
    // ============================================================

    /// <summary>
    /// FeishuTokenTypes 常量应与 FeishuAppContext.GetTokenManager 兼容，
    /// 即传入常量值能正确解析到对应的令牌管理器。
    /// </summary>
    [Fact]
    public void FeishuTokenTypes_ShouldBeCompatibleWithGetTokenManager()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = "test-app-key",
            AppId = "cli_test_app_id_1234567890",
            AppSecret = "test_secret_12345678"
        };

        var authMock = new Mock<IFeishuAuthentication>();
        var httpMock = new Mock<IEnhancedHttpClient>();
        var optionsMock = new Mock<IOptions<FeishuAppConfig>>();
        optionsMock.Setup(x => x.Value).Returns(config);

        var tenantTokenManager = new TenantTokenManager(
            authMock.Object, optionsMock.Object, new Mock<ILogger<TenantTokenManager>>().Object);
        var appTokenManager = new AppTokenManager(
            authMock.Object, optionsMock.Object, new Mock<ILogger<AppTokenManager>>().Object);
        var userTokenManager = new UserTokenManager(
            new Mock<IFeishuCurrentUserContext>().Object,
            authMock.Object, optionsMock.Object, new Mock<ILogger<UserTokenManager>>().Object);

        var appContext = new FeishuAppContext(
            config, tenantTokenManager, appTokenManager, userTokenManager,
            authMock.Object, httpMock.Object);

        // Act & Assert - 每种常量应返回对应的令牌管理器
        var tenantResult = appContext.GetTokenManager(FeishuTokenTypes.TenantAccessToken);
        Assert.Same(tenantTokenManager, tenantResult);

        var appResult = appContext.GetTokenManager(FeishuTokenTypes.AppAccessToken);
        Assert.Same(appTokenManager, appResult);

        var userResult = appContext.GetTokenManager(FeishuTokenTypes.UserAccessToken);
        Assert.Same(userTokenManager, userResult);
    }

    /// <summary>
    /// GetTokenManager 应支持大小写不敏感匹配（使用 OrdinalIgnoreCase）。
    /// </summary>
    [Fact]
    public void GetTokenManager_ShouldBeCaseInsensitive_WhenUsingFeishuTokenTypes()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = "test-app-key",
            AppId = "cli_test_app_id_1234567890",
            AppSecret = "test_secret_12345678"
        };

        var authMock = new Mock<IFeishuAuthentication>();
        var httpMock = new Mock<IEnhancedHttpClient>();
        var optionsMock = new Mock<IOptions<FeishuAppConfig>>();
        optionsMock.Setup(x => x.Value).Returns(config);

        var tenantTokenManager = new TenantTokenManager(
            authMock.Object, optionsMock.Object, new Mock<ILogger<TenantTokenManager>>().Object);
        var appTokenManager = new AppTokenManager(
            authMock.Object, optionsMock.Object, new Mock<ILogger<AppTokenManager>>().Object);
        var userTokenManager = new UserTokenManager(
            new Mock<IFeishuCurrentUserContext>().Object,
            authMock.Object, optionsMock.Object, new Mock<ILogger<UserTokenManager>>().Object);

        var appContext = new FeishuAppContext(
            config, tenantTokenManager, appTokenManager, userTokenManager,
            authMock.Object, httpMock.Object);

        // Act & Assert - 小写形式也应能匹配
        var tenantResult = appContext.GetTokenManager(FeishuTokenTypes.TenantAccessToken.ToLowerInvariant());
        Assert.Same(tenantTokenManager, tenantResult);

        var appResult = appContext.GetTokenManager(FeishuTokenTypes.AppAccessToken.ToLowerInvariant());
        Assert.Same(appTokenManager, appResult);

        var userResult = appContext.GetTokenManager(FeishuTokenTypes.UserAccessToken.ToLowerInvariant());
        Assert.Same(userTokenManager, userResult);
    }
}
