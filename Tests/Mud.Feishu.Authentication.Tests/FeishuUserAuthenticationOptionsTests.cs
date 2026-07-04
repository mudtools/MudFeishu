// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.Authentication;
using Xunit;

namespace Mud.Feishu.Authentication.Tests;

/// <summary>
/// FeishuUserAuthenticationOptions.Validate 单元测试
/// </summary>
public class FeishuUserAuthenticationOptionsTests
{
    private readonly FeishuUserAuthenticationOptions _sut;

    public FeishuUserAuthenticationOptionsTests()
    {
        _sut = new FeishuUserAuthenticationOptions();
    }

    private static FeishuUserAuthenticationOptions CreateOptions()
    {
        return new FeishuUserAuthenticationOptions();
    }

    [Fact]
    public void Validate_ShouldReturnSuccess_WithDefaultValues()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_ShouldReturnFail_WhenOpenIdClaimTypeEmpty()
    {
        // Arrange
        var options = CreateOptions();
        options.OpenIdClaimType = string.Empty;

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.False(result.Succeeded);
        Assert.Contains("OpenIdClaimType", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldReturnFail_WhenOpenIdClaimTypeWhitespace()
    {
        // Arrange
        var options = CreateOptions();
        options.OpenIdClaimType = "   ";

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.False(result.Succeeded);
        Assert.Contains("OpenIdClaimType", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldReturnFail_WhenOpenIdFallbackClaimTypeEmpty()
    {
        // Arrange
        var options = CreateOptions();
        options.OpenIdFallbackClaimType = string.Empty;

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.False(result.Succeeded);
        Assert.Contains("OpenIdFallbackClaimType", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldReturnFail_WhenUnionIdClaimTypeEmpty()
    {
        // Arrange
        var options = CreateOptions();
        options.UnionIdClaimType = string.Empty;

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.False(result.Succeeded);
        Assert.Contains("UnionIdClaimType", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldReturnFail_WhenUserIdClaimTypeEmpty()
    {
        // Arrange
        var options = CreateOptions();
        options.UserIdClaimType = string.Empty;

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.False(result.Succeeded);
        Assert.Contains("UserIdClaimType", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldReturnFail_WhenNameClaimTypeEmpty()
    {
        // Arrange
        var options = CreateOptions();
        options.NameClaimType = string.Empty;

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.False(result.Succeeded);
        Assert.Contains("NameClaimType", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldReturnFail_WhenAllClaimTypesEmpty()
    {
        // Arrange
        var options = CreateOptions();
        options.OpenIdClaimType = string.Empty;
        options.OpenIdFallbackClaimType = string.Empty;
        options.UnionIdClaimType = string.Empty;
        options.UserIdClaimType = string.Empty;
        options.NameClaimType = string.Empty;

        // Act
        var result = _sut.Validate(null, options);

        // Assert - 验证短路逻辑，第一个失败的属性（OpenIdClaimType）会被返回
        Assert.True(result.Failed);
        Assert.False(result.Succeeded);
        Assert.Contains("OpenIdClaimType", result.FailureMessage);
    }
}
