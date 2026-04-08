// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FsCheck;
using FsCheck.Xunit;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Services;
using System.Security.Cryptography;
using System.Text;

namespace Mud.Feishu.Webhook.Tests.Propertys;

/// <summary>
/// 签名验证器属性测试
/// 使用 FsCheck 进行基于属性的测试，验证签名验证器的正确性属性
/// </summary>
[Trait("Feature", "feishu-validator-refactoring")]
public class SignatureValidatorProperties
{
    private readonly Mock<ILogger<SignatureValidator>> _loggerMock;
    private readonly Mock<ISecurityAuditService> _auditServiceMock;
    private readonly Mock<IOptionsMonitor<FeishuWebhookOptions>> _optionsMonitorMock;
    private readonly Mock<IWebhookAppKeyAccessor> _appKeyAccessorMock;
    private readonly FeishuWebhookOptions _options;

    public SignatureValidatorProperties()
    {
        _loggerMock = new Mock<ILogger<SignatureValidator>>();
        _auditServiceMock = new Mock<ISecurityAuditService>();
        _optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        _appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();

        _options = new FeishuWebhookOptions
        {
            EnforceHeaderSignatureValidation = true,
            TimestampToleranceSeconds = 300
        };

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_options);

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
    /// 属性 1: 签名算法支持
    /// **验证需求: 1.2**
    /// 对于任何有效的签名验证请求，SignatureValidator 应该能够正确处理 HMAC-SHA256 和 SHA-256 两种算法的签名验证
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "1: 签名算法支持")]
    public Property SignatureValidator_ShouldSupportBothAlgorithms()
    {
        return Prop.ForAll(
            GenerateValidSignatureData(),
            data =>
            {
                // Arrange
                var validator = new SignatureValidator(_loggerMock.Object, _optionsMonitorMock.Object, _appKeyAccessorMock.Object, _auditServiceMock.Object);

                // 生成 HMAC-SHA256 签名（用于 ValidateSignatureAsync）
                var hmacSignString = $"{data.Timestamp}\n{data.Nonce}\n{data.Encrypt}";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(data.EncryptKey));
                var hmacHashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(hmacSignString));
                var hmacSignature = Convert.ToBase64String(hmacHashBytes);

                // 生成 SHA-256 签名（用于 ValidateHeaderSignatureAsync）
                var sha256SignString = $"{data.Timestamp}{data.Nonce}{data.EncryptKey}{data.Body}";
                using var sha256 = SHA256.Create();
                var sha256HashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sha256SignString));
                var sha256Signature = BitConverter.ToString(sha256HashBytes).Replace("-", "").ToLower();

                // Act & Assert
                var hmacResult = validator.ValidateSignatureAsync(data.Timestamp, data.Nonce, data.Encrypt, hmacSignature, data.EncryptKey).Result;
                var sha256Result = validator.ValidateHeaderSignatureAsync(data.Timestamp, data.Nonce, data.Body, sha256Signature, data.EncryptKey).Result;

                return hmacResult && sha256Result;
            });
    }

    /// <summary>
    /// 属性 2: 签名验证失败审计
    /// **验证需求: 1.4**
    /// 对于任何签名验证失败的情况，系统应该调用安全审计服务记录失败事件
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "2: 签名验证失败审计")]
    public Property SignatureValidator_ShouldAuditFailures()
    {
        return Prop.ForAll(
            GenerateInvalidSignatureData(),
            data =>
            {
                // Arrange
                var auditMock = new Mock<ISecurityAuditService>();
                var validator = new SignatureValidator(_loggerMock.Object, _optionsMonitorMock.Object, _appKeyAccessorMock.Object, auditMock.Object);

                // Act
                var result = validator.ValidateSignatureAsync(data.Timestamp, data.Nonce, data.Encrypt, data.InvalidSignature, data.EncryptKey).Result;

                // Assert
                var auditCalled = !result; // 如果验证失败，应该调用审计
                if (auditCalled)
                {
                    auditMock.Verify(x => x.LogSecurityFailureAsync(
                        SecurityEventType.SignatureValidation,
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()), Times.AtLeastOnce);
                }

                return !result; // 无效签名应该返回 false
            });
    }

    /// <summary>
    /// 属性 3: 生产环境参数检查
    /// **验证需求: 1.5**
    /// 对于任何在生产环境中缺少必要参数的请求，系统应该拒绝请求并记录安全事件
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "3: 生产环境参数检查")]
    public Property SignatureValidator_ShouldRejectInvalidParametersInProduction()
    {
        return Prop.ForAll(
            GenerateInvalidParameterData(),
            data =>
            {
                // Arrange
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
                var auditMock = new Mock<ISecurityAuditService>();
                var validator = new SignatureValidator(_loggerMock.Object, _optionsMonitorMock.Object, _appKeyAccessorMock.Object, auditMock.Object);

                try
                {
                    // Act
                    var result = validator.ValidateSignatureAsync(data.Timestamp, data.Nonce, data.Encrypt, data.Signature, data.EncryptKey).Result;

                    // Assert
                    if (data.Timestamp == 0 || string.IsNullOrEmpty(data.Nonce))
                    {
                        // 生产环境下，缺少必要参数应该拒绝请求
                        var shouldReject = !result;
                        if (shouldReject)
                        {
                            auditMock.Verify(x => x.LogSecurityFailureAsync(
                                SecurityEventType.SignatureValidation,
                                It.IsAny<string>(),
                                It.IsAny<string>(),
                                It.IsAny<string>(),
                                It.IsAny<string>(),
                                It.IsAny<string>()), Times.AtLeastOnce);
                        }
                        return shouldReject;
                    }

                    return true; // 有效参数的情况下，属性仍然成立
                }
                finally
                {
                    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
                }
            });
    }

    /// <summary>
    /// 生成有效的签名数据
    /// </summary>
    private static Arbitrary<SignatureTestData> GenerateValidSignatureData()
    {
        return Arb.From(
            from timestamp in Gen.Choose((int)DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds(), (int)DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()).Select(x => (long)x)
            from nonce in Gen.Elements("test-nonce-1", "test-nonce-2", "valid-nonce", "another-nonce")
            from encrypt in Gen.Elements("encrypted-data-1", "encrypted-data-2", "test-encrypt")
            from body in Gen.Elements("request-body-1", "request-body-2", "test-body")
            from encryptKey in Gen.Elements("test-key-32-bytes-long-enough!!", "another-key-32-bytes-long-key!!")
            select new SignatureTestData
            {
                Timestamp = timestamp,
                Nonce = nonce,
                Encrypt = encrypt,
                Body = body,
                EncryptKey = encryptKey
            });
    }

    /// <summary>
    /// 生成无效的签名数据
    /// </summary>
    private static Arbitrary<InvalidSignatureTestData> GenerateInvalidSignatureData()
    {
        return Arb.From(
            from timestamp in Gen.Choose((int)DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds(), (int)DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()).Select(x => (long)x)
            from nonce in Gen.Elements("test-nonce-1", "test-nonce-2", "valid-nonce")
            from encrypt in Gen.Elements("encrypted-data-1", "encrypted-data-2")
            from encryptKey in Gen.Elements("test-key-32-bytes-long-enough!!", "another-key-32-bytes-long-key!!")
            from invalidSignature in Gen.Elements("invalid-signature", "wrong-signature", "fake-signature", "")
            select new InvalidSignatureTestData
            {
                Timestamp = timestamp,
                Nonce = nonce,
                Encrypt = encrypt,
                EncryptKey = encryptKey,
                InvalidSignature = invalidSignature
            });
    }

    /// <summary>
    /// 生成无效的参数数据
    /// </summary>
    private static Arbitrary<InvalidParameterTestData> GenerateInvalidParameterData()
    {
        return Arb.From(
            from timestamp in Gen.Elements(0L, DateTimeOffset.UtcNow.ToUnixTimeSeconds()) // 包含无效的 0 时间戳
            from nonce in Gen.Elements("", null, "valid-nonce") // 包含无效的空 nonce
            from encrypt in Gen.Elements("encrypted-data")
            from encryptKey in Gen.Elements("test-key-32-bytes-long-enough!!")
            from signature in Gen.Elements("test-signature")
            select new InvalidParameterTestData
            {
                Timestamp = timestamp,
                Nonce = nonce ?? "",
                Encrypt = encrypt,
                EncryptKey = encryptKey,
                Signature = signature
            });
    }

    /// <summary>
    /// 签名测试数据
    /// </summary>
    public class SignatureTestData
    {
        public long Timestamp { get; set; }
        public string Nonce { get; set; } = "";
        public string Encrypt { get; set; } = "";
        public string Body { get; set; } = "";
        public string EncryptKey { get; set; } = "";
    }

    /// <summary>
    /// 无效签名测试数据
    /// </summary>
    public class InvalidSignatureTestData
    {
        public long Timestamp { get; set; }
        public string Nonce { get; set; } = "";
        public string Encrypt { get; set; } = "";
        public string EncryptKey { get; set; } = "";
        public string InvalidSignature { get; set; } = "";
    }

    /// <summary>
    /// 无效参数测试数据
    /// </summary>
    public class InvalidParameterTestData
    {
        public long Timestamp { get; set; }
        public string Nonce { get; set; } = "";
        public string Encrypt { get; set; } = "";
        public string EncryptKey { get; set; } = "";
        public string Signature { get; set; } = "";
    }
}