// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Configuration;

/// <summary>
/// FeishuAppConfigValidator 单元测试
/// </summary>
public class FeishuAppConfigValidatorTests
{
    private readonly FeishuAppConfigValidator _validator;

    public FeishuAppConfigValidatorTests()
    {
        _validator = new FeishuAppConfigValidator();
    }

    [Fact]
    public void Validate_WithNullOptions_ShouldReturnFail()
    {
        var result = _validator.Validate(null, null!);

        Assert.True(result.Failed);
        Assert.Contains("不能为 null", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithValidOptions_ShouldReturnSuccess()
    {
        var options = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = "cli_test123456789012",
            AppSecret = "test_secret_key_12345"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WithEmptyAppKey_ShouldReturnFail()
    {
        var options = new FeishuAppConfig
        {
            AppKey = "",
            AppId = "cli_test123456789012",
            AppSecret = "test_secret_key_12345"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("AppKey", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithInvalidAppIdFormat_ShouldReturnFail()
    {
        var options = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = "invalid_app_id",
            AppSecret = "test_secret_key_12345"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("AppId", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithShortAppSecret_ShouldReturnFail()
    {
        var options = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = "cli_test123456789012",
            AppSecret = "short"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("AppSecret", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithInvalidTimeout_ShouldReturnFail()
    {
        var options = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = "cli_test123456789012",
            AppSecret = "test_secret_key_12345",
            TimeOut = 0
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("TimeOut", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithInvalidRetryCount_ShouldReturnFail()
    {
        var options = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = "cli_test123456789012",
            AppSecret = "test_secret_key_12345",
            RetryCount = -1
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("RetryCount", result.FailureMessage);
    }
}
