// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Utils;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Configuration;

/// <summary>
/// FeishuWebhookOptionsValidator 单元测试
/// 重点覆盖 WHF-01：生产环境下应用级 EnforceHeaderSignatureValidation=false 必须被拒绝，
/// 与 SignatureValidator 的 GetEffectiveEnforceHeaderSignatureValidation 运行时解析路径对齐
/// </summary>
public class FeishuWebhookOptionsValidatorTests
{
    private static FeishuWebhookOptions CreateValidOptions(Action<FeishuWebhookOptions>? configure = null)
    {
        var options = new FeishuWebhookOptions();
        configure?.Invoke(options);
        return options;
    }

    private static FeishuWebhookOptionsValidator CreateValidator(bool isProduction)
    {
        var environmentMock = new Mock<IEnvironmentService>();
        environmentMock.Setup(x => x.IsProduction).Returns(isProduction);
        return new FeishuWebhookOptionsValidator(environmentMock.Object);
    }

    // ────────────────────────────────────────────────────────────────────
    // R5.2/X8：应用级校验的「接线」断言。
    // 此前 FeishuAppWebhookOptions.Validate() 在生产代码中**零调用**，导致下列两条
    // 文档化不变量从未生效；现在 FeishuWebhookOptions.Validate() 会逐应用调用它。
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_ShouldFail_WhenAppLevelTimestampToleranceExceedsReplayWindowCap()
    {
        // Arrange - WHF-03：应用级正整数同样受重放窗口上限（≤300 秒）约束
        var options = CreateValidOptions(o =>
        {
            o.Apps["app-a"] = new FeishuAppWebhookOptions
            {
                VerificationToken = "token_a",
                EncryptKey = "12345678901234567890123456789012",
                TimestampToleranceSeconds = FeishuWebhookOptions.MaxTimestampToleranceSeconds + 1
            };
        });
        var validator = CreateValidator(isProduction: false);

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed, "应用级时间戳容差必须在启动期被拒绝，而不是运行期无声放行");
        Assert.Contains("TimestampToleranceSeconds", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAppLevelEventHandlingTimeoutIsBelowMinimum()
    {
        // Arrange - 应用级正整数但 < 1000ms（0/-1 表示继承全局，属合法）
        var options = CreateValidOptions(o =>
        {
            o.Apps["app-a"] = new FeishuAppWebhookOptions
            {
                VerificationToken = "token_a",
                EncryptKey = "12345678901234567890123456789012",
                EventHandlingTimeoutMs = 500
            };
        });
        var validator = CreateValidator(isProduction: false);

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("EventHandlingTimeoutMs", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldDeriveAppKeyFromDictionaryKey_IgnoringConfiguredValue()
    {
        // Arrange - R5.2/X8：AppKey 一律由 Apps 字典键派生（路由只认字典键，配置值不得使其分叉）
        var options = CreateValidOptions(o =>
        {
            o.Apps["app-a"] = new FeishuAppWebhookOptions
            {
                AppKey = "diverged-value",
                VerificationToken = "token_a",
                EncryptKey = "12345678901234567890123456789012"
            };
        });

        // Act
        options.Validate();

        // Assert
        Assert.Equal("app-a", options.Apps["app-a"].AppKey);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAppLevelEnforceHeaderSignatureValidationFalseInProduction()
    {
        // Arrange - WHF-01 核心断言：应用级显式 false 在生产环境必须被拒绝
        var options = CreateValidOptions(o =>
        {
            o.EnforceHeaderSignatureValidation = true;
            o.Apps["app-a"] = new FeishuAppWebhookOptions
            {
                AppKey = "app-a",
                VerificationToken = "token_a",
                EncryptKey = "12345678901234567890123456789012",
                EnforceHeaderSignatureValidation = false // 应用级覆盖：绕过签名强制验证
            };
        });
        var validator = CreateValidator(isProduction: true);

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("app-a", result.FailureMessage);
        Assert.Contains("EnforceHeaderSignatureValidation=false", result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldPass_WhenAppLevelEnforceNull_InheritsGlobalTrue()
    {
        // Arrange - 继承路径回归：应用级 null 继承全局 true，生产环境放行
        var options = CreateValidOptions(o =>
        {
            o.EnforceHeaderSignatureValidation = true;
            o.Apps["app-a"] = new FeishuAppWebhookOptions
            {
                AppKey = "app-a",
                VerificationToken = "token_a",
                EncryptKey = "12345678901234567890123456789012",
                EnforceHeaderSignatureValidation = null
            };
        });
        var validator = CreateValidator(isProduction: true);

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ShouldPass_WhenAppLevelEnforceFalse_InNonProduction()
    {
        // Arrange - 非生产环境保留逃生口：应用级 false 允许
        var options = CreateValidOptions(o =>
        {
            o.EnforceHeaderSignatureValidation = true;
            o.Apps["app-a"] = new FeishuAppWebhookOptions
            {
                AppKey = "app-a",
                VerificationToken = "token_a",
                EncryptKey = "12345678901234567890123456789012",
                EnforceHeaderSignatureValidation = false
            };
        });
        var validator = CreateValidator(isProduction: false);

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ShouldPass_WhenAppLevelEnforceTrue_InProduction()
    {
        // Arrange - 应用级显式 true 在生产环境合法
        var options = CreateValidOptions(o =>
        {
            o.EnforceHeaderSignatureValidation = true;
            o.Apps["app-a"] = new FeishuAppWebhookOptions
            {
                AppKey = "app-a",
                VerificationToken = "token_a",
                EncryptKey = "12345678901234567890123456789012",
                EnforceHeaderSignatureValidation = true
            };
        });
        var validator = CreateValidator(isProduction: true);

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ShouldFail_WhenGlobalEnforceFalse_InProduction_ExistingBehavior()
    {
        // Arrange - 既有全局锁定行为回归
        var options = CreateValidOptions(o => o.EnforceHeaderSignatureValidation = false);
        var validator = CreateValidator(isProduction: true);

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("生产环境禁止 EnforceHeaderSignatureValidation=false", result.FailureMessage);
    }
}
