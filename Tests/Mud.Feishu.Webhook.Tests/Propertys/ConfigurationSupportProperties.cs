// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FsCheck;
using FsCheck.Xunit;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Services;

namespace Mud.Feishu.Webhook.Tests.Propertys;

/// <summary>
/// 配置支持属性测试
/// 使用 FsCheck 进行基于属性的测试，验证配置读取和多应用支持的正确性属性
/// </summary>
[Trait("Feature", "feishu-validator-refactoring")]
public class ConfigurationSupportProperties
{
    private readonly Mock<IWebhookAppKeyAccessor> _appKeyAccessorMock;

    public ConfigurationSupportProperties()
    {
        _appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();

        // 设置 _appKeyAccessorMock 使 SetAppKey 方法能够更新 CurrentAppKey 属性
        string? currentAppKey = null;
        _appKeyAccessorMock
            .Setup(x => x.SetAppKey(It.IsAny<string>()))
            .Callback<string>(appKey => currentAppKey = appKey);
        _appKeyAccessorMock
            .Setup(x => x.CurrentAppKey)
            .Returns(() => currentAppKey);
    }

    /// <summary>
    /// 属性 20: 配置读取正确性
    /// **验证需求: 6.4**
    /// 对于任何验证器实例，应该能够正确读取和使用 FeishuWebhookOptions 配置
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "20: 配置读取正确性")]
    public Property ConfigurationSupport_ShouldReadConfigurationCorrectly()
    {
        return Prop.ForAll(
            GenerateConfigurationTestData(),
            data =>
            {
                // Arrange
                var optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
                var loggerMock = new Mock<ILogger<TimestampValidator>>();

                var options = new FeishuWebhookOptions
                {
                    TimestampToleranceSeconds = data.TimestampToleranceSeconds,
                    EnforceHeaderSignatureValidation = data.EnforceHeaderSignatureValidation
                };

                optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);

                // Act
                var timestampValidator = new TimestampValidator(loggerMock.Object, optionsMonitorMock.Object, _appKeyAccessorMock.Object);

                // 测试配置读取 - 使用默认参数时应该从配置读取
                var result = timestampValidator.ValidateTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                // Assert - 验证配置被正确读取和使用
                optionsMonitorMock.Verify(x => x.CurrentValue, Times.AtLeastOnce);
                return result; // 当前时间戳应该总是有效的
            });
    }

    /// <summary>
    /// 属性 21: 多应用配置支持
    /// **验证需求: 6.5**
    /// 对于任何多应用场景，验证器应该能够从应用特定配置中获取正确的验证参数
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "21: 多应用配置支持")]
    public Property ConfigurationSupport_ShouldSupportMultiAppConfiguration()
    {
        return Prop.ForAll(
            GenerateMultiAppConfigurationTestData(),
            data =>
            {
                // Arrange
                var encryptKeyProviderMock = new Mock<IEncryptKeyProvider>();
                var loggerMock = new Mock<ILogger<SubscriptionValidator>>();

                encryptKeyProviderMock
                    .Setup(x => x.GetVerificationTokenAsync(data.AppKey, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(data.AppToken);

                // Act
                var subscriptionValidator = new SubscriptionValidator(loggerMock.Object, encryptKeyProviderMock.Object, _appKeyAccessorMock.Object);
                subscriptionValidator.SetCurrentAppKey(data.AppKey);

                var request = new EventVerificationRequest
                {
                    Type = "url_verification",
                    Token = data.AppToken, // 使用应用特定的 Token
                    Challenge = "test_challenge"
                };

                // 验证应该使用应用特定配置而不是全局配置
                var result = subscriptionValidator.ValidateSubscriptionRequestAsync(request, data.AppToken).GetAwaiter().GetResult();

                // Assert - 应该使用应用特定的 Token，所以验证应该成功
                return result;
            });
    }

    #region 测试数据生成器

    /// <summary>
    /// 生成配置测试数据
    /// </summary>
    private static Arbitrary<ConfigurationTestData> GenerateConfigurationTestData()
    {
        return Arb.From(
            from timestampTolerance in Gen.Choose(1, 3600) // 1秒到1小时
            from enforceValidation in Arb.Generate<bool>()
            from token in Gen.Elements("token1", "token2", "verification_token_123")
            from encryptKey in Gen.Elements(
                "12345678901234567890123456789012", // 32字符
                "abcdefghijklmnopqrstuvwxyz123456", // 32字符
                "test_encrypt_key_32_characters!!")  // 32字符
            select new ConfigurationTestData
            {
                TimestampToleranceSeconds = timestampTolerance,
                EnforceHeaderSignatureValidation = enforceValidation,
                VerificationToken = token,
                EncryptKey = encryptKey
            });
    }

    /// <summary>
    /// 生成多应用配置测试数据
    /// </summary>
    private static Arbitrary<MultiAppConfigurationTestData> GenerateMultiAppConfigurationTestData()
    {
        return Arb.From(
            from appKey in Gen.Elements("app1", "app2", "test-app", "demo-application")
            from globalToken in Gen.Elements("global_token_1", "global_token_2")
            from appToken in Gen.Elements("app_token_1", "app_token_2", "specific_app_token")
            from appEncryptKey in Gen.Elements(
                "app_encrypt_key_32_characters!!", // 32字符
                "specific_key_for_app_validation", // 32字符
                "12345678901234567890123456789012") // 32字符
            where globalToken != appToken // 确保全局和应用 Token 不同
            select new MultiAppConfigurationTestData
            {
                AppKey = appKey,
                GlobalToken = globalToken,
                AppToken = appToken,
                AppEncryptKey = appEncryptKey
            });
    }

    #endregion

    #region 测试数据类

    /// <summary>
    /// 配置测试数据
    /// </summary>
    public class ConfigurationTestData
    {
        public int TimestampToleranceSeconds { get; set; }
        public bool EnforceHeaderSignatureValidation { get; set; }
        public string VerificationToken { get; set; } = "";
        public string EncryptKey { get; set; } = "";
    }

    /// <summary>
    /// 多应用配置测试数据
    /// </summary>
    public class MultiAppConfigurationTestData
    {
        public string AppKey { get; set; } = "";
        public string GlobalToken { get; set; } = "";
        public string AppToken { get; set; } = "";
        public string AppEncryptKey { get; set; } = "";
    }

    #endregion
}
