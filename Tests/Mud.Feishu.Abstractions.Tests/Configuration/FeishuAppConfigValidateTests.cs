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

    [Fact]
    public void Validate_ShouldThrow_WhenRetryDelayMsGreaterThanTimeOut()
    {
        var config = CreateValidConfig();
        config.TimeOut = 5;
        config.RetryDelayMs = 10000;
        config.RetryCount = 3;

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RetryDelayMs*TimeOut*");
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
}
