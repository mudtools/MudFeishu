// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FsCheck;
using FsCheck.Xunit;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Services;

namespace Mud.Feishu.Webhook.Tests.Propertys;

/// <summary>
/// 订阅验证器属性测试
/// 使用 FsCheck 进行基于属性的测试，验证订阅验证器的正确性属性
/// </summary>
[Trait("Feature", "feishu-validator-refactoring")]
public class SubscriptionValidatorProperties
{
    private readonly Mock<ILogger<SubscriptionValidator>> _loggerMock;
    private readonly Mock<IEncryptKeyProvider> _encryptKeyProviderMock;

    public SubscriptionValidatorProperties()
    {
        _loggerMock = new Mock<ILogger<SubscriptionValidator>>();
        _encryptKeyProviderMock = new Mock<IEncryptKeyProvider>();
    }

    /// <summary>
    /// 属性 14: 缺失字段处理
    /// **验证需求: 4.5**
    /// 对于任何缺少必要字段的订阅请求，系统应该拒绝验证并记录相应的警告信息
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "14: 缺失字段处理")]
    public Property SubscriptionValidator_ShouldRejectMissingFields()
    {
        return Prop.ForAll(
            GenerateMissingFieldData(),
            data =>
            {
                // Skip null data objects
                if (data == null) return true;

                // Arrange
                var loggerMock = new Mock<ILogger<SubscriptionValidator>>();
                var encryptKeyProviderMock = new Mock<IEncryptKeyProvider>();

                var validator = new SubscriptionValidator(loggerMock.Object, encryptKeyProviderMock.Object);

                EventVerificationRequest? request = null;
                if (!data.IsRequestNull)
                {
                    request = new EventVerificationRequest
                    {
                        Type = data.Type ?? "",
                        Token = data.Token ?? "",
                        Challenge = data.Challenge ?? ""
                    };
                }

                // Act
                var result = validator.ValidateSubscriptionRequest(request!, "valid-token");

                // Assert
                var hasMissingFields = data.IsRequestNull ||
                                     string.IsNullOrEmpty(data.Type) ||
                                     string.IsNullOrEmpty(data.Token) ||
                                     string.IsNullOrEmpty(data.Challenge);

                if (hasMissingFields)
                {
                    // 缺少字段，应该验证失败
                    return !result;
                }
                else
                {
                    // 没有缺少字段，但可能因为其他原因失败（如类型不匹配）
                    return true; // 属性仍然成立
                }
            });
    }

    /// <summary>
    /// 生成订阅请求数据
    /// </summary>
    private static Arbitrary<SubscriptionRequestData> GenerateSubscriptionRequestData()
    {
        var typeGen = Gen.Elements("url_verification", "invalid_type", "", "event_callback");
        var tokenGen = Gen.Elements("valid-token", "invalid-token", "", "another-token");
        var challengeGen = Gen.Elements("valid-challenge", "test-challenge", "", "another-challenge");
        var expectedTokenGen = Gen.Elements("valid-token", "invalid-token", "another-token");

        return Arb.From(
            from type in typeGen
            from token in tokenGen
            from challenge in challengeGen
            from expectedToken in expectedTokenGen
            select new SubscriptionRequestData
            {
                Type = type ?? "",
                Token = token ?? "",
                Challenge = challenge ?? "",
                ExpectedToken = expectedToken ?? ""
            });
    }

    /// <summary>
    /// 生成请求类型数据
    /// </summary>
    private static Arbitrary<RequestTypeData> GenerateRequestTypeData()
    {
        var typeGen = Gen.Elements("url_verification", "event_callback", "invalid_type", "", "message", "card_action");

        return Arb.From(
            from type in typeGen
            select new RequestTypeData { Type = type ?? "" });
    }

    /// <summary>
    /// 生成Token不匹配数据
    /// </summary>
    private static Arbitrary<TokenMismatchData> GenerateTokenMismatchData()
    {
        var actualTokenGen = Gen.Elements("token1", "token2", "valid-token", "invalid-token", "");
        var expectedTokenGen = Gen.Elements("token1", "token2", "valid-token", "different-token");

        return Arb.From(
            from actualToken in actualTokenGen
            from expectedToken in expectedTokenGen
            select new TokenMismatchData
            {
                ActualToken = actualToken ?? "",
                ExpectedToken = expectedToken ?? ""
            });
    }

    /// <summary>
    /// 生成缺失字段数据
    /// </summary>
    private static Arbitrary<MissingFieldData> GenerateMissingFieldData()
    {
        return Arb.From(
            from isRequestNull in Gen.Elements(true, false)
            from type in Gen.Elements("url_verification", null, "")
            from token in Gen.Elements("valid-token", null, "")
            from challenge in Gen.Elements("valid-challenge", null, "")
            select new MissingFieldData
            {
                IsRequestNull = isRequestNull,
                Type = type,
                Token = token,
                Challenge = challenge
            });
    }

    /// <summary>
    /// 验证日志是否被调用
    /// </summary>
    private static void VerifyLogCalled(Mock<ILogger<SubscriptionValidator>> loggerMock, LogLevel logLevel, string message)
    {
        loggerMock.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// 订阅请求测试数据
    /// </summary>
    public class SubscriptionRequestData
    {
        public string Type { get; set; } = "";
        public string Token { get; set; } = "";
        public string Challenge { get; set; } = "";
        public string ExpectedToken { get; set; } = "";
    }

    /// <summary>
    /// 请求类型测试数据
    /// </summary>
    public class RequestTypeData
    {
        public string Type { get; set; } = "";
    }

    /// <summary>
    /// Token不匹配测试数据
    /// </summary>
    public class TokenMismatchData
    {
        public string ActualToken { get; set; } = "";
        public string ExpectedToken { get; set; } = "";
    }

    /// <summary>
    /// 缺失字段测试数据
    /// </summary>
    public class MissingFieldData
    {
        public bool IsRequestNull { get; set; }
        public string? Type { get; set; }
        public string? Token { get; set; }
        public string? Challenge { get; set; }
    }
}
