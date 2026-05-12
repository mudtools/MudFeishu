// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Authentication;
using Xunit;

namespace Mud.Feishu.Authentication.Tests.Configuration;

public class FeishuUserAuthenticationOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var options = new FeishuUserAuthenticationOptions();

        options.OpenIdClaimType.Should().Be("open_id");
        options.OpenIdFallbackClaimType.Should().Be(System.Security.Claims.ClaimTypes.NameIdentifier);
        options.UnionIdClaimType.Should().Be("union_id");
        options.UserIdClaimType.Should().Be("user_id");
        options.NameClaimType.Should().Be(System.Security.Claims.ClaimTypes.Name);
        options.EnableDistributedTracing.Should().BeTrue();
        options.EnableSensitiveLog.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidOptions_ShouldReturnSuccess()
    {
        var options = new FeishuUserAuthenticationOptions();

        var result = options.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullOpenIdClaimType_ShouldReturnFail()
    {
        var options = new FeishuUserAuthenticationOptions { OpenIdClaimType = "" };

        var result = options.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("OpenIdClaimType");
    }

    [Fact]
    public void Validate_WithNullOpenIdFallbackClaimType_ShouldReturnFail()
    {
        var options = new FeishuUserAuthenticationOptions { OpenIdFallbackClaimType = "" };

        var result = options.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("OpenIdFallbackClaimType");
    }

    [Fact]
    public void Validate_WithNullUnionIdClaimType_ShouldReturnFail()
    {
        var options = new FeishuUserAuthenticationOptions { UnionIdClaimType = "" };

        var result = options.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("UnionIdClaimType");
    }

    [Fact]
    public void Validate_WithNullUserIdClaimType_ShouldReturnFail()
    {
        var options = new FeishuUserAuthenticationOptions { UserIdClaimType = "" };

        var result = options.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("UserIdClaimType");
    }

    [Fact]
    public void Validate_WithNullNameClaimType_ShouldReturnFail()
    {
        var options = new FeishuUserAuthenticationOptions { NameClaimType = "" };

        var result = options.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("NameClaimType");
    }

    [Fact]
    public void Validate_WithCustomClaimTypes_ShouldReturnSuccess()
    {
        var options = new FeishuUserAuthenticationOptions
        {
            OpenIdClaimType = "custom_open_id",
            OpenIdFallbackClaimType = "custom_fallback",
            UnionIdClaimType = "custom_union_id",
            UserIdClaimType = "custom_user_id",
            NameClaimType = "custom_name"
        };

        var result = options.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void EnableSensitiveLog_DefaultShouldBeFalse()
    {
        var options = new FeishuUserAuthenticationOptions();

        options.EnableSensitiveLog.Should().BeFalse();
    }

    [Fact]
    public void EnableDistributedTracing_DefaultShouldBeTrue()
    {
        var options = new FeishuUserAuthenticationOptions();

        options.EnableDistributedTracing.Should().BeTrue();
    }
}
