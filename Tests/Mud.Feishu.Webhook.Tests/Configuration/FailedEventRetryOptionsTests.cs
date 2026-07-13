// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Configuration;

public class FailedEventRetryOptionsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        var options = new FailedEventRetryOptions();

        options.EnableRetry.Should().BeFalse();
        options.MaxRetryCount.Should().Be(3);
        options.InitialRetryDelaySeconds.Should().Be(10);
        options.RetryDelayMultiplier.Should().Be(2.0);
        options.MaxRetryDelaySeconds.Should().Be(300);
        options.RetryPollIntervalSeconds.Should().Be(30);
        options.MaxRetryPerPoll.Should().Be(10);
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenAllValuesAreValid()
    {
        var options = new FailedEventRetryOptions();

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMaxRetryCountIsNegative()
    {
        var options = new FailedEventRetryOptions { MaxRetryCount = -1 };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxRetryCount*");
    }

    [Fact]
    public void Validate_ShouldAcceptZeroMaxRetryCount()
    {
        var options = new FailedEventRetryOptions { EnableRetry = true, MaxRetryCount = 0 };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenInitialRetryDelaySecondsIsZero()
    {
        var options = new FailedEventRetryOptions { InitialRetryDelaySeconds = 0 };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*InitialRetryDelaySeconds*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenRetryDelayMultiplierIsBelow1()
    {
        var options = new FailedEventRetryOptions { RetryDelayMultiplier = 0.5 };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*RetryDelayMultiplier*");
    }

    [Fact]
    public void Validate_ShouldAcceptRetryDelayMultiplierExactly1()
    {
        var options = new FailedEventRetryOptions { EnableRetry = true, RetryDelayMultiplier = 1.0 };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMaxRetryDelaySecondsLessThanInitial()
    {
        var options = new FailedEventRetryOptions
        {
            InitialRetryDelaySeconds = 100,
            MaxRetryDelaySeconds = 50
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxRetryDelaySeconds*");
    }

    [Fact]
    public void Validate_ShouldAcceptMaxRetryDelaySecondsEqualToInitial()
    {
        var options = new FailedEventRetryOptions
        {
            EnableRetry = true,
            InitialRetryDelaySeconds = 10,
            MaxRetryDelaySeconds = 10
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenRetryPollIntervalSecondsIsZero()
    {
        var options = new FailedEventRetryOptions { RetryPollIntervalSeconds = 0 };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*RetryPollIntervalSeconds*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMaxRetryPerPollIsZero()
    {
        var options = new FailedEventRetryOptions { MaxRetryPerPoll = 0 };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxRetryPerPoll*");
    }

    [Fact]
    public void Validate_ShouldAcceptCustomValidValues()
    {
        var options = new FailedEventRetryOptions
        {
            EnableRetry = true,
            MaxRetryCount = 5,
            InitialRetryDelaySeconds = 30,
            RetryDelayMultiplier = 3.0,
            MaxRetryDelaySeconds = 600,
            RetryPollIntervalSeconds = 60,
            MaxRetryPerPoll = 20
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    // ========== EnableRetry=false 时宽松验证测试 ==========
    // 实现已改为宽松验证：EnableRetry=false 时仅校验基本范围，不强制要求子配置等于默认值。
    // 这允许在配置热更新或动态切换时更灵活的处理。

    [Fact]
    public void Validate_ShouldNotThrow_WhenEnableRetryFalseAndAllDefaults()
    {
        // EnableRetry=false 但所有子配置均为默认值，应通过校验
        var options = new FailedEventRetryOptions { EnableRetry = false };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenEnableRetryFalseButMaxRetryCountNonDefault()
    {
        // 宽松验证：EnableRetry=false 时，MaxRetryCount 只要在有效范围内（>= 0）即通过
        var options = new FailedEventRetryOptions { EnableRetry = false, MaxRetryCount = 5 };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenEnableRetryFalseButInitialRetryDelaySecondsNonDefault()
    {
        // 宽松验证：EnableRetry=false 时，InitialRetryDelaySeconds 只要在有效范围内（>= 1）即通过
        var options = new FailedEventRetryOptions { EnableRetry = false, InitialRetryDelaySeconds = 30 };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenEnableRetryFalseButRetryDelayMultiplierNonDefault()
    {
        // 宽松验证：EnableRetry=false 时，RetryDelayMultiplier 只要在有效范围内（>= 1.0）即通过
        var options = new FailedEventRetryOptions { EnableRetry = false, RetryDelayMultiplier = 3.0 };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenEnableRetryFalseButMaxRetryDelaySecondsNonDefault()
    {
        // 宽松验证：EnableRetry=false 时，MaxRetryDelaySeconds 只要 >= InitialRetryDelaySeconds 即通过
        var options = new FailedEventRetryOptions { EnableRetry = false, MaxRetryDelaySeconds = 600 };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenEnableRetryFalseButRetryPollIntervalSecondsNonDefault()
    {
        // 宽松验证：EnableRetry=false 时，RetryPollIntervalSeconds 只要在有效范围内（>= 1）即通过
        var options = new FailedEventRetryOptions { EnableRetry = false, RetryPollIntervalSeconds = 60 };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenEnableRetryFalseButMaxRetryPerPollNonDefault()
    {
        // 宽松验证：EnableRetry=false 时，MaxRetryPerPoll 只要在有效范围内（>= 1）即通过
        var options = new FailedEventRetryOptions { EnableRetry = false, MaxRetryPerPoll = 20 };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }
}
