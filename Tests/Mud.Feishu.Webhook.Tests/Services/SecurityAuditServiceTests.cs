// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mud.Feishu.Webhook.Tests.Services;

/// <summary>
/// SecurityAuditService 单元测试
/// </summary>
public class SecurityAuditServiceTests
{
    private readonly Mock<ILogger<SecurityAuditService>> _loggerMock;
    private readonly SecurityAuditService _service;

    public SecurityAuditServiceTests()
    {
        _loggerMock = new Mock<ILogger<SecurityAuditService>>();
        _service = new SecurityAuditService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new SecurityAuditService(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task LogSecurityFailureAsync_WithAllParameters_ShouldLogWarning()
    {
        // Arrange
        var eventType = SecurityEventType.SignatureValidation;
        var clientIp = "192.168.1.100";
        var requestPath = "/webhook";
        var details = "签名验证失败";
        var requestId = "req-123";
        var appKey = "app-test";

        // Act
        await _service.LogSecurityFailureAsync(eventType, clientIp, requestPath, details, requestId, appKey);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("安全验证失败") &&
                    v.ToString()!.Contains("SignatureValidation") &&
                    v.ToString()!.Contains("192.168.1.100") &&
                    v.ToString()!.Contains("req-123") &&
                    v.ToString()!.Contains("app-test")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogSecurityFailureAsync_WithoutOptionalParameters_ShouldLogWarning()
    {
        // Arrange
        var eventType = SecurityEventType.TimestampValidation;
        var clientIp = "10.0.0.1";
        var requestPath = "/api/webhook";
        var details = "时间戳过期";

        // Act
        await _service.LogSecurityFailureAsync(eventType, clientIp, requestPath, details);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("安全验证失败") &&
                    v.ToString()!.Contains("TimestampValidation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogSecuritySuccessAsync_WithAllParameters_ShouldLogInformation()
    {
        // Arrange
        var eventType = SecurityEventType.SignatureValidation;
        var clientIp = "192.168.1.100";
        var requestPath = "/webhook";
        var details = "签名验证成功";
        var requestId = "req-456";
        var appKey = "app-prod";

        // Act
        await _service.LogSecuritySuccessAsync(eventType, clientIp, requestPath, details, requestId, appKey);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("安全验证成功") &&
                    v.ToString()!.Contains("SignatureValidation") &&
                    v.ToString()!.Contains("req-456") &&
                    v.ToString()!.Contains("app-prod")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogSecuritySuccessAsync_WithoutOptionalParameters_ShouldLogInformation()
    {
        // Arrange
        var eventType = SecurityEventType.SubscriptionValidation;
        var clientIp = "172.16.0.50";
        var requestPath = "/callback";
        var details = "订阅验证通过";

        // Act
        await _service.LogSecuritySuccessAsync(eventType, clientIp, requestPath, details);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("安全验证成功") &&
                    v.ToString()!.Contains("SubscriptionValidation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
