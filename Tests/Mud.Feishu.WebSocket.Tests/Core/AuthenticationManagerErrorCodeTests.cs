// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证 位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// AuthenticationManager 错误码处理测试类
/// </summary>
public class AuthenticationManagerErrorCodeTests
{
    private readonly Mock<ILogger<AuthenticationManager>> _loggerMock;

    public AuthenticationManagerErrorCodeTests()
    {
        _loggerMock = new Mock<ILogger<AuthenticationManager>>();
    }

    /// <summary>
    /// 使用反射辅助调用私有方法
    /// </summary>
    private static void InvokeLogDetailedAuthError(AuthenticationManager manager, int? errorCode, string? errorMessage)
    {
        var method = typeof(AuthenticationManager).GetMethod(
            "LogDetailedAuthError",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(manager, new object[] { errorCode!, errorMessage! });
    }

    /// <summary>
    /// 使用反射辅助获取私有字段值
    /// </summary>
    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (T)field!.GetValue(instance)!;
    }

    private AuthenticationManager CreateManager()
    {
        return new AuthenticationManager(
            _loggerMock.Object,
            new FeishuWebSocketOptions(),
            msg => Task.CompletedTask);
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleTokenExpired_ErrorCode10009()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert - Should not throw
        InvokeLogDetailedAuthError(manager, 10009, "Token expired");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleTokenInvalid_ErrorCode10010()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 10010, "Token invalid");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleInsufficientPermissions_ErrorCode10011()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 10011, "Insufficient permissions");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleParameterError_ErrorCode10012()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 10012, "Parameter error");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleSystemBusy_ErrorCode10013()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 10013, "System busy");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleVersionNotSupported_ErrorCode10014()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 10014, "Version not supported");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleSessionIdInvalid_ErrorCode10015()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 10015, "Session ID invalid");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleBotDisabled_ErrorCode99991663()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 99991663, "Bot disabled");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleBotNotInGroup_ErrorCode99991664()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 99991664, "Bot not in group");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleBotRemoved_ErrorCode99991665()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 99991665, "Bot removed from group");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleRequestParameterError_ErrorCode12340001()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 12340001, "Request parameter error");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleRequestBodyTooLarge_ErrorCode12340002()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 12340002, "Request body too large");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleFileUploadFailed_ErrorCode12340003()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 12340003, "File upload failed");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleServerInternalError_ErrorCode12350001()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 12350001, "Server internal error");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleServerTimeout_ErrorCode12350002()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 12350002, "Server timeout");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleServerRateLimit_ErrorCode12350003()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 12350003, "Server rate limit");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleNetworkConnectionFailed_ErrorCode12360001()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 12360001, "Network connection failed");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleServiceUnavailable_ErrorCode12360002()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        InvokeLogDetailedAuthError(manager, 12360002, "Service unavailable");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleUnknownErrorCode()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert - Should not throw
        InvokeLogDetailedAuthError(manager, 99999999, "Unknown error");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleNullErrorCode()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert - Should not throw
        InvokeLogDetailedAuthError(manager, null, "Some error message");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleNullErrorMessage()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert - Should not throw
        InvokeLogDetailedAuthError(manager, 10009, null);
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleErrorCodeInAuthRange_10000To20000()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert - 测试 10000-20000 范围内的错误码
        InvokeLogDetailedAuthError(manager, 10000, "Test error");
        InvokeLogDetailedAuthError(manager, 15000, "Test error");
        InvokeLogDetailedAuthError(manager, 19999, "Test error");
    }

    [Fact]
    public void LogDetailedAuthError_ShouldHandleErrorCodeInBotRange_99990000To100000000()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert - 测试 99990000-100000000 范围内的错误码
        InvokeLogDetailedAuthError(manager, 99990000, "Test bot error");
        InvokeLogDetailedAuthError(manager, 99995000, "Test bot error");
        InvokeLogDetailedAuthError(manager, 99999999, "Test bot error");
    }

    [Fact]
    public void HandleAuthResponse_ShouldSetIsAuthenticated_ToTrue_WhenCodeIs0()
    {
        // Arrange
        var manager = CreateManager();
        var authResponse = "{\"code\":0,\"message\":\"success\"}";

        // Act
        manager.HandleAuthResponse(authResponse);

        // Assert
        manager.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void HandleAuthResponse_ShouldSetIsAuthenticated_ToFalse_WhenCodeIsNot0()
    {
        // Arrange
        var manager = CreateManager();
        var authResponse = "{\"code\":10010,\"message\":\"Token invalid\"}";

        // Act
        manager.HandleAuthResponse(authResponse);

        // Assert
        manager.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void HandleAuthResponse_ShouldIncrementTotalAuthFailures_WhenCodeIsNot0()
    {
        // Arrange
        var manager = CreateManager();
        var authResponse = "{\"code\":10010,\"message\":\"Token invalid\"}";

        // Act
        manager.HandleAuthResponse(authResponse);
        var totalFailures = manager.TotalAuthFailures;

        // Assert
        totalFailures.Should().BeGreaterThan(0);
    }

    [Fact]
    public void HandleAuthResponse_ShouldTriggerAuthenticationFailedEvent_WhenCodeIsNot0()
    {
        // Arrange
        var manager = CreateManager();
        var eventTriggered = false;
        manager.AuthenticationFailed += (sender, args) => eventTriggered = true;

        var authResponse = "{\"code\":10010,\"message\":\"Token invalid\"}";

        // Act
        manager.HandleAuthResponse(authResponse);

        // Assert
        eventTriggered.Should().BeTrue();
    }

    [Fact]
    public void HandleAuthResponse_ShouldTriggerAuthenticatedEvent_WhenCodeIs0()
    {
        // Arrange
        var manager = CreateManager();
        var eventTriggered = false;
        manager.Authenticated += (sender, args) => eventTriggered = true;

        var authResponse = "{\"code\":0,\"message\":\"success\"}";

        // Act
        manager.HandleAuthResponse(authResponse);

        // Assert
        eventTriggered.Should().BeTrue();
    }

    [Fact]
    public void HandleAuthResponse_ShouldHandleInvalidJson()
    {
        // Arrange
        var manager = CreateManager();
        var eventTriggered = false;
        manager.AuthenticationFailed += (sender, args) => eventTriggered = true;

        var invalidJson = "not valid json";

        // Act
        manager.HandleAuthResponse(invalidJson);

        // Assert
        eventTriggered.Should().BeTrue();
    }

    [Fact]
    public void ResetAuthentication_ShouldResetIsAuthenticated_ToFalse()
    {
        // Arrange
        var manager = CreateManager();
        manager.HandleAuthResponse("{\"code\":0,\"message\":\"success\"}");
        manager.IsAuthenticated.Should().BeTrue();

        // Act
        manager.ResetAuthentication();

        // Assert
        manager.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AuthenticationManager(
            null!,
            new FeishuWebSocketOptions(),
            msg => Task.CompletedTask));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenSendMessageCallbackIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AuthenticationManager(
            _loggerMock.Object,
            new FeishuWebSocketOptions(),
            null!));
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldThrowArgumentException_WhenTokenIsEmpty()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.AuthenticateAsync(""));
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldThrowArgumentException_WhenTokenIsNull()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.AuthenticateAsync(null!));
    }
}
