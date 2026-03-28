// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------
using Mud.HttpUtils;

namespace Mud.Feishu.Abstractions.Tests.Utilities;

/// <summary>
/// UrlValidator 单元测试
/// </summary>
public class UrlValidatorTests
{
    #region ValidateBaseUrl Tests

    [Fact]
    public void ValidateBaseUrl_WithOfficialFeishuDomain_ShouldPass()
    {
        // Arrange
        var validUrls = new[]
        {
            "https://open.feishu.cn",
            "https://open.feishu.cn/",
            "https://open.feishu.cn/open-apis",
            "https://open.larksuite.com",
            "https://open.larksuite.com/",
            "https://open.larksuite.com/open-apis",
            "https://feishu.cn",
            "https://larksuite.com"
        };

        // Act & Assert
        foreach (var url in validUrls)
        {
            var exception = Record.Exception(() => UrlValidator.ValidateBaseUrl(url, false));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void ValidateBaseUrl_WithNonHttpsProtocol_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidUrls = new[]
        {
            "http://open.feishu.cn",
            "ftp://open.feishu.cn",
            "ws://open.feishu.cn"
        };

        // Act & Assert
        foreach (var url in invalidUrls)
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                UrlValidator.ValidateBaseUrl(url, false));
            Assert.Contains("仅允许 HTTPS 协议", exception.Message);
        }
    }

    [Fact]
    public void ValidateBaseUrl_WithEmptyUrl_ShouldReturnSilently()
    {
        // Arrange - ValidateBaseUrl 对空 URL 返回（不抛出异常）
        // Act & Assert
        var exception = Record.Exception(() => UrlValidator.ValidateBaseUrl("", false));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateBaseUrl_WithNullUrl_ShouldReturnSilently()
    {
        // Arrange - ValidateBaseUrl 对 null URL 返回（不抛出异常）
        // Act & Assert
        var exception = Record.Exception(() => UrlValidator.ValidateBaseUrl(null, false));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateBaseUrl_WithInvalidUrlFormat_ShouldThrow()
    {
        // Arrange
        var invalidUrls = new[]
        {
            "not-a-url",
            "://open.feishu.cn",
            "https://",
            "httpx://open.feishu.cn"
        };

        // Act & Assert - 这些无效 URL 格式会抛出异常
        foreach (var url in invalidUrls)
        {
            var exception = Record.Exception(() => UrlValidator.ValidateBaseUrl(url, false));
            Assert.NotNull(exception);
        }
    }

    #endregion

    #region ValidateUrl Tests

    [Fact]
    public void ValidateUrl_WithOfficialFeishuUrl_ShouldPass()
    {
        // Arrange
        var validUrls = new[]
        {
            "https://open.feishu.cn/open-apis/auth/v3/app_access_token/internal",
            "https://open.feishu.cn/open-apis/bot/v3/info",
            "https://feishu.cn/api",
            "https://larksuite.com/api"
        };

        // Act & Assert
        foreach (var url in validUrls)
        {
            var exception = Record.Exception(() =>
                UrlValidator.ValidateUrl(url, false));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void ValidateUrl_WithNonHttps_ShouldThrow()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            UrlValidator.ValidateUrl("http://open.feishu.cn/api", false));
        Assert.Contains("仅允许 HTTPS 协议", exception.Message);
    }

    [Fact]
    public void ValidateUrl_WithNullUrl_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            UrlValidator.ValidateUrl(null!, false));
    }

    [Fact]
    public void ValidateUrl_WithEmptyUrl_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            UrlValidator.ValidateUrl("", false));
    }

    [Fact]
    public void ValidateUrl_WithRelativePath_ShouldThrow()
    {
        // Act & Assert - ValidateUrl only validates absolute URLs
        Assert.Throws<ArgumentException>(() =>
            UrlValidator.ValidateUrl("/api", false));
    }

    [Fact]
    public void ValidateUrl_WithCustomDomain_WhenNotAllowed_ShouldThrow()
    {
        // Arrange
        var customUrls = new[]
        {
            "https://api.example.com",
            "https://custom.feishu.com",
            "https://example.com"
        };

        // Act & Assert
        foreach (var url in customUrls)
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                UrlValidator.ValidateUrl(url, false));
            Assert.Contains("不在飞书官方白名单中", exception.Message);
        }
    }

    [Fact]
    public void ValidateUrl_WithCustomDomain_WhenAllowed_ShouldPass()
    {
        // Arrange - 使用公共域名，避免被解析为私有 IP
        var customUrls = new[]
        {
            "https://api.github.com",
            "https://example.org"
        };

        // Act & Assert
        foreach (var url in customUrls)
        {
            var exception = Record.Exception(() =>
                UrlValidator.ValidateUrl(url, true));
            Assert.Null(exception);
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ValidateUrl_WithFullFeishuApiUrl_ShouldPass()
    {
        // Arrange
        var fullApiUrl = "https://open.feishu.cn/open-apis/auth/v3/app_access_token/internal?app_id=xxx&app_secret=yyy";

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateUrl(fullApiUrl, false));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateBaseUrl_WithFeishuDomainAndPort_ShouldPass()
    {
        // Arrange -飞书域名 + 端口
        var url = "https://open.feishu.cn:443";

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateBaseUrl(url, false));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateBaseUrl_WithFeishuDomainAndPath_ShouldPass()
    {
        // Arrange
        var url = "https://open.feishu.cn/open-apis";

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateBaseUrl(url, false));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateUrl_WithTrailingSlash_ShouldPass()
    {
        // Arrange
        var url = "https://open.feishu.cn/open-apis/";

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateUrl(url, false));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateUrl_WithQueryParams_ShouldPass()
    {
        // Arrange
        var url = "https://open.feishu.cn/open-apis/auth/v3/app_access_token/internal?app_id=test";

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateUrl(url, false));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateUrl_WithFragment_ShouldPass()
    {
        // Arrange
        var url = "https://open.feishu.cn/open-apis/#section";

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateUrl(url, false));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateUrl_WithEncodedCharacters_ShouldPass()
    {
        // Arrange
        var url = "https://open.feishu.cn/open-apis?param=value%20with%20spaces";

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateUrl(url, false));
        Assert.Null(exception);
    }

    #endregion
}
