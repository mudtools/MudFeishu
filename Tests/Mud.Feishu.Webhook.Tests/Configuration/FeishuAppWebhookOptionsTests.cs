// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Configuration;

public class FeishuAppWebhookOptionsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        var options = new FeishuAppWebhookOptions();

        options.AppKey.Should().BeEmpty();
        options.VerificationToken.Should().BeEmpty();
        options.EncryptKey.Should().BeEmpty();
        options.Description.Should().BeNull();
        options.TimestampToleranceSeconds.Should().Be(-1);
        options.EventHandlingTimeoutMs.Should().Be(-1);
        options.EnforceHeaderSignatureValidation.Should().BeNull();
        options.EnableBodySignatureValidation.Should().BeNull();
        options.EnableExceptionHandling.Should().BeNull();
        options.EnablePerformanceMonitoring.Should().BeNull();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenAppKeyIsEmpty()
    {
        var options = new FeishuAppWebhookOptions
        {
            VerificationToken = "token",
            EncryptKey = "12345678901234567890123456789012"
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*AppKey*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenVerificationTokenIsEmpty()
    {
        var options = new FeishuAppWebhookOptions
        {
            AppKey = "test-app",
            EncryptKey = "12345678901234567890123456789012"
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*VerificationToken*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEncryptKeyIsEmpty()
    {
        var options = new FeishuAppWebhookOptions
        {
            AppKey = "test-app",
            VerificationToken = "token"
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*EncryptKey*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEncryptKeyLengthIsNot32()
    {
        var options = new FeishuAppWebhookOptions
        {
            AppKey = "test-app",
            VerificationToken = "token",
            EncryptKey = "short"
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*32*");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenAllRequiredFieldsAreValid()
    {
        var options = new FeishuAppWebhookOptions
        {
            AppKey = "test-app",
            VerificationToken = "token",
            EncryptKey = "12345678901234567890123456789012"
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEventHandlingTimeoutMsIsLessThanMinus1()
    {
        var options = new FeishuAppWebhookOptions
        {
            AppKey = "test-app",
            VerificationToken = "token",
            EncryptKey = "12345678901234567890123456789012",
            EventHandlingTimeoutMs = -2
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*EventHandlingTimeoutMs*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEventHandlingTimeoutMsIsBetween0And1000()
    {
        var options = new FeishuAppWebhookOptions
        {
            AppKey = "test-app",
            VerificationToken = "token",
            EncryptKey = "12345678901234567890123456789012",
            EventHandlingTimeoutMs = 500
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*EventHandlingTimeoutMs*");
    }

    [Fact]
    public void Validate_ShouldAcceptMinus1AsEventHandlingTimeoutMs()
    {
        var options = new FeishuAppWebhookOptions
        {
            AppKey = "test-app",
            VerificationToken = "token",
            EncryptKey = "12345678901234567890123456789012",
            EventHandlingTimeoutMs = -1
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void GetEffectiveTimestampTolerance_ShouldReturnLocalValue_WhenPositive()
    {
        var options = new FeishuAppWebhookOptions { TimestampToleranceSeconds = 120 };

        options.GetEffectiveTimestampTolerance(30).Should().Be(120);
    }

    [Fact]
    public void GetEffectiveTimestampTolerance_ShouldReturnGlobalValue_WhenMinus1()
    {
        var options = new FeishuAppWebhookOptions { TimestampToleranceSeconds = -1 };

        options.GetEffectiveTimestampTolerance(30).Should().Be(30);
    }

    [Fact]
    public void GetEffectiveTimestampTolerance_ShouldReturnGlobalValue_WhenZero()
    {
        var options = new FeishuAppWebhookOptions { TimestampToleranceSeconds = 0 };

        options.GetEffectiveTimestampTolerance(30).Should().Be(30);
    }

    [Fact]
    public void GetEffectiveEventHandlingTimeout_ShouldReturnLocalValue_WhenPositive()
    {
        var options = new FeishuAppWebhookOptions { EventHandlingTimeoutMs = 5000 };

        options.GetEffectiveEventHandlingTimeout(30000).Should().Be(5000);
    }

    [Fact]
    public void GetEffectiveEventHandlingTimeout_ShouldReturnGlobalValue_WhenMinus1()
    {
        var options = new FeishuAppWebhookOptions { EventHandlingTimeoutMs = -1 };

        options.GetEffectiveEventHandlingTimeout(30000).Should().Be(30000);
    }

    [Fact]
    public void GetEffectiveEnableExceptionHandling_ShouldReturnLocalValue_WhenSet()
    {
        var options = new FeishuAppWebhookOptions { EnableExceptionHandling = false };

        options.GetEffectiveEnableExceptionHandling(true).Should().BeFalse();
    }

    [Fact]
    public void GetEffectiveEnableExceptionHandling_ShouldReturnGlobalValue_WhenNull()
    {
        var options = new FeishuAppWebhookOptions { EnableExceptionHandling = null };

        options.GetEffectiveEnableExceptionHandling(true).Should().BeTrue();
    }

    [Fact]
    public void GetEffectiveEnablePerformanceMonitoring_ShouldReturnLocalValue_WhenSet()
    {
        var options = new FeishuAppWebhookOptions { EnablePerformanceMonitoring = true };

        options.GetEffectiveEnablePerformanceMonitoring(false).Should().BeTrue();
    }

    [Fact]
    public void GetEffectiveEnablePerformanceMonitoring_ShouldReturnGlobalValue_WhenNull()
    {
        var options = new FeishuAppWebhookOptions { EnablePerformanceMonitoring = null };

        options.GetEffectiveEnablePerformanceMonitoring(false).Should().BeFalse();
    }

    [Fact]
    public void GetEffectiveEnableBodySignatureValidation_ShouldReturnLocalValue_WhenSet()
    {
        var options = new FeishuAppWebhookOptions { EnableBodySignatureValidation = false };

        options.GetEffectiveEnableBodySignatureValidation(true).Should().BeFalse();
    }

    [Fact]
    public void GetEffectiveEnableBodySignatureValidation_ShouldReturnGlobalValue_WhenNull()
    {
        var options = new FeishuAppWebhookOptions { EnableBodySignatureValidation = null };

        options.GetEffectiveEnableBodySignatureValidation(true).Should().BeTrue();
    }

    [Fact]
    public void GetEffectiveEnableBodySignatureValidation_ShouldReturnGlobalFalse_WhenNullAndGlobalFalse()
    {
        var options = new FeishuAppWebhookOptions { EnableBodySignatureValidation = null };

        options.GetEffectiveEnableBodySignatureValidation(false).Should().BeFalse();
    }

    [Fact]
    public void GetEffectiveEnforceHeaderSignatureValidation_ShouldReturnLocalValue_WhenSetToTrue()
    {
        var options = new FeishuAppWebhookOptions { EnforceHeaderSignatureValidation = true };

        options.GetEffectiveEnforceHeaderSignatureValidation(false).Should().BeTrue();
    }

    [Fact]
    public void GetEffectiveEnforceHeaderSignatureValidation_ShouldReturnLocalValue_WhenSetToFalse()
    {
        var options = new FeishuAppWebhookOptions { EnforceHeaderSignatureValidation = false };

        options.GetEffectiveEnforceHeaderSignatureValidation(true).Should().BeFalse();
    }

    [Fact]
    public void GetEffectiveEnforceHeaderSignatureValidation_ShouldReturnGlobalValue_WhenNull()
    {
        var options = new FeishuAppWebhookOptions { EnforceHeaderSignatureValidation = null };

        options.GetEffectiveEnforceHeaderSignatureValidation(true).Should().BeTrue();
    }

    [Fact]
    public void GetEffectiveEnforceHeaderSignatureValidation_ShouldReturnGlobalFalse_WhenNullAndGlobalFalse()
    {
        var options = new FeishuAppWebhookOptions { EnforceHeaderSignatureValidation = null };

        options.GetEffectiveEnforceHeaderSignatureValidation(false).Should().BeFalse();
    }
}
