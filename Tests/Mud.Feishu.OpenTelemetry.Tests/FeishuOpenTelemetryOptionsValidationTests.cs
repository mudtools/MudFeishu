// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mud.Feishu.OpenTelemetry.Tests;

/// <summary>
/// FeishuOpenTelemetryOptions IValidateOptions 集成测试
/// 验证配置校验逻辑在 DI 容器中的正确性
/// </summary>
public class FeishuOpenTelemetryOptionsValidationTests
{
    [Fact]
    public void Validate_ValidOptions_ShouldReturnSuccess()
    {
        // Arrange
        var validator = new FeishuOpenTelemetryOptions();
        var options = new FeishuOpenTelemetryOptions
        {
            SamplingRatio = 0.5,
            ServiceName = "my-app",
            ServiceVersion = "1.0.0",
            DeploymentEnvironment = "staging"
        };

        // Act
        var result = validator.Validate(nameof(FeishuOpenTelemetryOptions), options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_SamplingRatioAboveOne_ShouldFail()
    {
        // Arrange
        var validator = new FeishuOpenTelemetryOptions();
        var options = new FeishuOpenTelemetryOptions
        {
            SamplingRatio = 1.5
        };

        // Act
        var result = validator.Validate(nameof(FeishuOpenTelemetryOptions), options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("SamplingRatio");
        result.FailureMessage.Should().Contain("0.0~1.0");
    }

    [Fact]
    public void Validate_SamplingRatioNegative_ShouldFail()
    {
        // Arrange
        var validator = new FeishuOpenTelemetryOptions();
        var options = new FeishuOpenTelemetryOptions
        {
            SamplingRatio = -0.1
        };

        // Act
        var result = validator.Validate(nameof(FeishuOpenTelemetryOptions), options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("SamplingRatio");
    }

    [Fact]
    public void Validate_EmptyServiceName_ShouldFail()
    {
        // Arrange
        var validator = new FeishuOpenTelemetryOptions();
        var options = new FeishuOpenTelemetryOptions
        {
            ServiceName = ""
        };

        // Act
        var result = validator.Validate(nameof(FeishuOpenTelemetryOptions), options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ServiceName");
    }

    [Fact]
    public void Validate_NullServiceVersion_ShouldFail()
    {
        // Arrange
        var validator = new FeishuOpenTelemetryOptions();
        var options = new FeishuOpenTelemetryOptions
        {
            ServiceVersion = null!
        };

        // Act
        var result = validator.Validate(nameof(FeishuOpenTelemetryOptions), options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ServiceVersion");
    }

    [Fact]
    public void Validate_EmptyDeploymentEnvironment_ShouldFail()
    {
        // Arrange
        var validator = new FeishuOpenTelemetryOptions();
        var options = new FeishuOpenTelemetryOptions
        {
            DeploymentEnvironment = "   "
        };

        // Act
        var result = validator.Validate(nameof(FeishuOpenTelemetryOptions), options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DeploymentEnvironment");
    }

    [Fact]
    public void Validate_RelativeOtlpEndpoint_ShouldFail()
    {
        // Arrange
        var validator = new FeishuOpenTelemetryOptions();
        var options = new FeishuOpenTelemetryOptions
        {
            OtlpEndpoint = new Uri("/relative/path", UriKind.Relative)
        };

        // Act
        var result = validator.Validate(nameof(FeishuOpenTelemetryOptions), options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("OtlpEndpoint");
        result.FailureMessage.Should().Contain("绝对 URI");
    }

    [Fact]
    public void Validate_NullOtlpEndpoint_ShouldSucceed()
    {
        // Arrange - null OtlpEndpoint 表示不配置 OTLP 导出器，是合法值
        var validator = new FeishuOpenTelemetryOptions();
        var options = new FeishuOpenTelemetryOptions
        {
            OtlpEndpoint = null
        };

        // Act
        var result = validator.Validate(nameof(FeishuOpenTelemetryOptions), options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void AddFeishuOpenTelemetry_ShouldRegisterIValidateOptions()
    {
        // Arrange & Act
        var services = new ServiceCollection();
        services.AddFeishuOpenTelemetry();
        var sp = services.BuildServiceProvider();

        // Assert
        var validator = sp.GetService<IValidateOptions<FeishuOpenTelemetryOptions>>();
        validator.Should().NotBeNull();
    }

    [Fact]
    public void AddFeishuOpenTelemetry_ShouldRegisterIOptions()
    {
        // Arrange & Act
        var services = new ServiceCollection();
        services.AddFeishuOpenTelemetry(options =>
        {
            options.ServiceName = "test-app";
            options.SamplingRatio = 0.1;
        });
        var sp = services.BuildServiceProvider();

        // Assert
        var options = sp.GetService<IOptions<FeishuOpenTelemetryOptions>>()?.Value;
        options.Should().NotBeNull();
        options!.ServiceName.Should().Be("test-app");
        options.SamplingRatio.Should().Be(0.1);
    }
}
