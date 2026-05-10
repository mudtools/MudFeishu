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

public class RateLimitOptionsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        var options = new RateLimitOptions();

        options.EnableRateLimit.Should().BeFalse();
        options.WindowSizeSeconds.Should().Be(60);
        options.MaxRequestsPerWindow.Should().Be(100);
        options.EnableIpRateLimit.Should().BeTrue();
        options.TooManyRequestsStatusCode.Should().Be(429);
        options.TooManyRequestsMessage.Should().Be("请求过于频繁，请稍后再试");
        options.WhitelistIPs.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenRateLimitIsDisabled()
    {
        var options = new RateLimitOptions { EnableRateLimit = false, WindowSizeSeconds = 0 };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenRateLimitIsEnabledWithValidConfig()
    {
        var options = new RateLimitOptions
        {
            EnableRateLimit = true,
            WindowSizeSeconds = 60,
            MaxRequestsPerWindow = 100,
            TooManyRequestsStatusCode = 429
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenWindowSizeSecondsIsZero()
    {
        var options = new RateLimitOptions
        {
            EnableRateLimit = true,
            WindowSizeSeconds = 0
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*WindowSizeSeconds*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMaxRequestsPerWindowIsZero()
    {
        var options = new RateLimitOptions
        {
            EnableRateLimit = true,
            MaxRequestsPerWindow = 0
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxRequestsPerWindow*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenStatusCodeIsBelow400()
    {
        var options = new RateLimitOptions
        {
            EnableRateLimit = true,
            TooManyRequestsStatusCode = 399
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*TooManyRequestsStatusCode*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenStatusCodeIsAbove599()
    {
        var options = new RateLimitOptions
        {
            EnableRateLimit = true,
            TooManyRequestsStatusCode = 600
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*TooManyRequestsStatusCode*");
    }

    [Fact]
    public void Validate_ShouldAcceptBoundaryStatusCode400()
    {
        var options = new RateLimitOptions
        {
            EnableRateLimit = true,
            TooManyRequestsStatusCode = 400
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldAcceptBoundaryStatusCode599()
    {
        var options = new RateLimitOptions
        {
            EnableRateLimit = true,
            TooManyRequestsStatusCode = 599
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void WhitelistIPs_ShouldAcceptMultipleIPs()
    {
        var options = new RateLimitOptions
        {
            WhitelistIPs = new HashSet<string> { "192.168.1.1", "10.0.0.1" }
        };

        options.WhitelistIPs.Should().Contain("192.168.1.1");
        options.WhitelistIPs.Should().Contain("10.0.0.1");
        options.WhitelistIPs.Should().HaveCount(2);
    }
}
