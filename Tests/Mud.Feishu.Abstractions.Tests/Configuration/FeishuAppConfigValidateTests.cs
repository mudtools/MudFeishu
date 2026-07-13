// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Abstractions.Configuration;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Configuration;

public class FeishuAppConfigValidateTests
{
    private static FeishuAppConfig CreateValidConfig() => new()
    {
        AppKey = "test-app",
        AppId = "cli_test123456789012",
        AppSecret = "test_secret_key_12345"
    };

    [Fact]
    public void Validate_ShouldNotThrow_WhenConfigIsValid()
    {
        var config = CreateValidConfig();

        var act = () => config.Validate();

        act.Should().NotThrow();
    }

    /// <summary>
    /// REF-9: RetryDelayMs 可以大于 TimeOut（两者概念独立）
    /// </summary>
    [Fact]
    public void Validate_ShouldAcceptRetryDelayGreaterThanTimeout()
    {
        var config = CreateValidConfig();
        config.TimeOut = 3;
        config.RetryDelayMs = 5000;
        config.RetryCount = 3;

        var act = () => config.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenRetryDelayMsEqualsTimeOut()
    {
        var config = CreateValidConfig();
        config.TimeOut = 1;
        config.RetryDelayMs = 1000;
        config.RetryCount = 3;

        var act = () => config.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenRetryCountIsZeroEvenIfRetryDelayMsExceedsTimeOut()
    {
        var config = CreateValidConfig();
        config.TimeOut = 5;
        config.RetryDelayMs = 10000;
        config.RetryCount = 0;

        var act = () => config.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTimeOutIsZero()
    {
        var config = CreateValidConfig();
        config.TimeOut = 0;

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TimeOut*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTimeOutExceeds300()
    {
        var config = CreateValidConfig();
        config.TimeOut = 301;

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TimeOut*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenRetryCountExceeds10()
    {
        var config = CreateValidConfig();
        config.RetryCount = 11;

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RetryCount*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenRetryDelayMsIsLessThan100()
    {
        var config = CreateValidConfig();
        config.RetryDelayMs = 50;

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RetryDelayMs*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCircuitBreakerFailureThresholdIsZero()
    {
        var config = CreateValidConfig();
        config.CircuitBreakerFailureThreshold = 0;

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CircuitBreakerFailureThreshold*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCircuitBreakerMinimumThroughputIsLessThan2()
    {
        var config = CreateValidConfig();
        config.CircuitBreakerMinimumThroughput = 1;

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CircuitBreakerMinimumThroughput*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenAppKeyIsEmpty()
    {
        var config = CreateValidConfig();
        config.AppKey = "";

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AppKey*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenAppIdFormatIsInvalid()
    {
        var config = CreateValidConfig();
        config.AppId = "invalid_id";

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AppId*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenAppSecretIsTooShort()
    {
        var config = CreateValidConfig();
        config.AppSecret = "short";

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AppSecret*");
    }

    /// <summary>
    /// CFG-6: 熔断器禁用且有默认子配置时不应抛异常
    /// </summary>
    [Fact]
    public void Validate_ShouldNotThrow_WhenCircuitBreakerDisabledWithDefaults()
    {
        var config = CreateValidConfig();
        config.CircuitBreakerEnabled = false;
        // 熔断器子配置保持默认值（未修改）

        var act = () => config.Validate();

        act.Should().NotThrow();
    }

    /// <summary>
    /// CFG-6: 熔断器禁用但子配置在有效范围内时不应抛异常（宽松策略，与 RateLimit/Retry 一致）
    /// </summary>
    [Fact]
    public void Validate_ShouldNotThrow_WhenCircuitBreakerDisabledButSubConfigInValidRange()
    {
        var config = CreateValidConfig();
        config.CircuitBreakerEnabled = false;
        // 修改熔断器子配置为非默认值，但在有效范围内
        config.CircuitBreakerFailureThreshold = 30; // 默认为20，30 在 1-100 范围内

        var act = () => config.Validate();

        // 宽松策略：禁用时仅校验范围，不强制要求等于默认值
        act.Should().NotThrow();
    }

    /// <summary>
    /// CFG-6: 熔断器禁用但子配置超出有效范围时应抛异常（保留范围校验）
    /// </summary>
    [Fact]
    public void Validate_ShouldThrow_WhenCircuitBreakerDisabledButSubConfigOutOfRange()
    {
        var config = CreateValidConfig();
        config.CircuitBreakerEnabled = false;
        config.CircuitBreakerFailureThreshold = 150; // 超出范围 1-100

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CircuitBreakerFailureThreshold*");
    }

    /// <summary>
    /// CFG-6: 熔断器启用时应验证子配置范围
    /// </summary>
    [Fact]
    public void Validate_ShouldThrow_WhenCircuitBreakerEnabledAndSubConfigOutOfRange()
    {
        var config = CreateValidConfig();
        config.CircuitBreakerEnabled = true;
        config.CircuitBreakerFailureThreshold = 150; // 超出范围 1-100

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CircuitBreakerFailureThreshold*");
    }

    /// <summary>
    /// BaseUrl 仅允许 HTTPS 协议，HTTP 应抛异常
    /// </summary>
    [Fact]
    public void Validate_ShouldThrow_WhenBaseUrlIsHttp()
    {
        var config = CreateValidConfig();
        config.BaseUrl = "http://open.feishu.cn";

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*HTTPS*");
    }

    /// <summary>
    /// BaseUrl 域名不在飞书官方白名单中且 AllowCustomBaseUrl=false 时应抛异常
    /// </summary>
    [Fact]
    public void Validate_ShouldThrow_WhenDomainNotInWhitelist()
    {
        var config = CreateValidConfig();
        config.BaseUrl = "https://evil.example.com";

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*白名单*");
    }

    /// <summary>
    /// AllowCustomBaseUrl=true 时允许使用自定义域名
    /// </summary>
    [Fact]
    public void Validate_ShouldNotThrow_WhenAllowCustomBaseUrlTrue()
    {
        var config = CreateValidConfig();
        config.BaseUrl = "https://internal.proxy.com";
        config.AllowCustomBaseUrl = true;

        var act = () => config.Validate();

        act.Should().NotThrow();
    }

    /// <summary>
    /// BaseUrl 为飞书官方域名时应通过验证
    /// </summary>
    [Fact]
    public void Validate_ShouldNotThrow_WhenDomainIsFeishuOfficial()
    {
        var config = CreateValidConfig();
        config.BaseUrl = "https://open.feishu.cn";

        var act = () => config.Validate();

        act.Should().NotThrow();
    }

    /// <summary>
    /// BaseUrl 不是有效的绝对 URI 时应抛异常
    /// </summary>
    [Fact]
    public void Validate_ShouldThrow_WhenBaseUrlIsInvalidUri()
    {
        var config = CreateValidConfig();
        config.BaseUrl = "not a url";

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BaseUrl*URI*");
    }
}
