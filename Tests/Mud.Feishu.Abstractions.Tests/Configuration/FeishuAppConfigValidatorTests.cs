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
        var result = _validator.Validate(null, (FeishuAppConfig)null!);

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

    [Fact]
    public void Validate_WithHttpBaseUrl_ShouldReturnFail()
    {
        var options = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = "cli_test123456789012",
            AppSecret = "test_secret_key_12345",
            BaseUrl = "http://open.feishu.cn"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("HTTPS", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithCustomDomainWithoutAllowFlag_ShouldReturnFail()
    {
        var options = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = "cli_test123456789012",
            AppSecret = "test_secret_key_12345",
            BaseUrl = "https://custom-api.example.com",
            AllowCustomBaseUrl = false
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("白名单", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithCustomDomainWithAllowFlag_ShouldReturnSuccess()
    {
        var options = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = "cli_test123456789012",
            AppSecret = "test_secret_key_12345",
            BaseUrl = "https://custom-api.example.com",
            AllowCustomBaseUrl = true
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WithLarkSuiteBaseUrl_ShouldReturnSuccess()
    {
        var options = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = "cli_test123456789012",
            AppSecret = "test_secret_key_12345",
            BaseUrl = "https://open.larksuite.com"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }
}

/// <summary>
/// FeishuAppConfigValidator 对 List&lt;FeishuAppConfig&gt; 的验证测试
/// </summary>
public class FeishuAppConfigListValidatorTests
{
    private readonly FeishuAppConfigValidator _validator;

    public FeishuAppConfigListValidatorTests()
    {
        _validator = new FeishuAppConfigValidator();
    }

    [Fact]
    public void ValidateList_WithNullList_ShouldReturnFail()
    {
        var result = _validator.Validate(null, (List<FeishuAppConfig>)null!);

        Assert.True(result.Failed);
        Assert.Contains("不能为 null 或空", result.FailureMessage);
    }

    [Fact]
    public void ValidateList_WithEmptyList_ShouldReturnFail()
    {
        var result = _validator.Validate(null, new List<FeishuAppConfig>());

        Assert.True(result.Failed);
        Assert.Contains("不能为 null 或空", result.FailureMessage);
    }

    [Fact]
    public void ValidateList_WithValidSingleConfig_ShouldReturnSuccess()
    {
        var configs = new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = "test-app",
                AppId = "cli_test123456789012",
                AppSecret = "test_secret_key_12345"
            }
        };

        var result = _validator.Validate(null, configs);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateList_WithValidMultipleConfigs_ShouldReturnSuccess()
    {
        var configs = new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = "app1",
                AppId = "cli_test123456789012",
                AppSecret = "test_secret_key_12345"
            },
            new()
            {
                AppKey = "app2",
                AppId = "cli_another123456789",
                AppSecret = "another_secret_key_12"
            }
        };

        var result = _validator.Validate(null, configs);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateList_WithOneInvalidConfig_ShouldReturnFailWithIndex()
    {
        var configs = new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = "app1",
                AppId = "cli_test123456789012",
                AppSecret = "test_secret_key_12345"
            },
            new()
            {
                AppKey = "",
                AppId = "cli_another123456789",
                AppSecret = "another_secret_key_12"
            }
        };

        var result = _validator.Validate(null, configs);

        Assert.True(result.Failed);
        Assert.Contains("[1]", result.FailureMessage);
        Assert.Contains("AppKey", result.FailureMessage);
    }

    [Fact]
    public void ValidateList_WithMultipleInvalidConfigs_ShouldReturnAllErrors()
    {
        var configs = new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = "",
                AppId = "cli_test123456789012",
                AppSecret = "test_secret_key_12345"
            },
            new()
            {
                AppKey = "app2",
                AppId = "invalid",
                AppSecret = "another_secret_key_12"
            }
        };

        var result = _validator.Validate(null, configs);

        Assert.True(result.Failed);
        Assert.Contains("[0]", result.FailureMessage);
        Assert.Contains("[1]", result.FailureMessage);
    }
}
