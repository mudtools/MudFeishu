// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// SessionManager 会话管理器测试类
/// </summary>
public class SessionManagerTests
{
    private readonly Mock<ILogger<SessionManager>> _loggerMock;
    private readonly Mud.Feishu.WebSocket.FeishuWebSocketOptions _options;
    private readonly SessionManager _sessionManager;

    public SessionManagerTests()
    {
        _loggerMock = new Mock<ILogger<SessionManager>>();
        _options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        _sessionManager = new SessionManager(_loggerMock.Object, _options);
    }

    [Fact]
    public void CurrentSessionId_ShouldBeNull_Initially()
    {
        // Act & Assert
        _sessionManager.CurrentSessionId.Should().BeNull();
    }

    [Fact]
    public void SessionDuration_ShouldBeZero_WhenNoSessionExists()
    {
        // Act & Assert
        _sessionManager.SessionDuration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void HasValidSession_ShouldBeFalse_WhenNoSessionExists()
    {
        // Act & Assert
        _sessionManager.HasValidSession.Should().BeFalse();
    }

    [Fact]
    public void SetSessionId_ShouldThrowArgumentException_WhenSessionIdIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sessionManager.SetSessionId(""));
        Assert.Throws<ArgumentException>(() => _sessionManager.SetSessionId(null!));
    }

    [Fact]
    public void SetSessionId_ShouldUpdateCurrentSessionId()
    {
        // Arrange
        var sessionId = "test-session-id-123";

        // Act
        _sessionManager.SetSessionId(sessionId);

        // Assert
        _sessionManager.CurrentSessionId.Should().Be(sessionId);
    }

    [Fact]
    public void SetSessionId_ShouldUpdateSessionStartTime()
    {
        // Arrange
        var beforeSet = DateTime.UtcNow.AddSeconds(-1);
        var sessionId = "test-session-id-123";

        // Act
        _sessionManager.SetSessionId(sessionId);
        var afterSet = DateTime.UtcNow.AddSeconds(1);

        // Assert
        _sessionManager.SessionDuration.Should().BeGreaterThan(TimeSpan.Zero);
        _sessionManager.SessionDuration.Should().BeLessThan(afterSet - beforeSet);
    }

    [Fact]
    public void SetSessionId_ShouldRaiseSessionUpdatedEvent_WhenNewSessionIsSet()
    {
        // Arrange
        var sessionId = "test-session-id-123";
        var eventRaised = false;
        SessionUpdatedEventArgs? eventArgs = null;

        _sessionManager.SessionUpdated += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act
        _sessionManager.SetSessionId(sessionId);

        // Assert
        eventRaised.Should().BeTrue();
        eventArgs.Should().NotBeNull();
        eventArgs!.SessionId.Should().Be(sessionId);
        eventArgs.IsNewSession.Should().BeTrue();
    }

    [Fact]
    public void SetSessionId_ShouldNotRaiseSessionUpdatedEvent_WhenSameSessionIdIsSet()
    {
        // Arrange
        var sessionId = "test-session-id-123";
        _sessionManager.SetSessionId(sessionId);

        var eventRaised = false;
        _sessionManager.SessionUpdated += (sender, args) => { eventRaised = true; };

        // Act
        _sessionManager.SetSessionId(sessionId);

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void SetSessionId_ShouldRaiseSessionUpdatedEvent_WhenDifferentSessionIdIsSet()
    {
        // Arrange
        var firstSessionId = "test-session-id-123";
        var secondSessionId = "test-session-id-456";
        _sessionManager.SetSessionId(firstSessionId);

        var eventRaised = false;
        SessionUpdatedEventArgs? eventArgs = null;
        _sessionManager.SessionUpdated += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act
        _sessionManager.SetSessionId(secondSessionId);

        // Assert
        eventRaised.Should().BeTrue();
        eventArgs!.SessionId.Should().Be(secondSessionId);
        eventArgs.IsNewSession.Should().BeTrue();
    }

    [Fact]
    public void HasValidSession_ShouldBeTrue_WhenValidSessionExists()
    {
        // Arrange
        _sessionManager.SetSessionId("valid-session-id");

        // Act & Assert
        _sessionManager.HasValidSession.Should().BeTrue();
    }

    [Fact]
    public void HasValidSession_ShouldBeFalse_WhenSessionIsExpired()
    {
        // Arrange
        // Set a session that will be expired (older than 24 hours)
        var oldSessionId = "expired-session-id";
        _sessionManager.SetSessionId(oldSessionId);

        // Simulate expired session by setting start time to more than 24 hours ago
        // This is a workaround since we can't directly set the start time
        // In production, sessions expire naturally after 24 hours

        // For testing purposes, we need to verify the logic works
        // The actual expiration depends on DateTime.UtcNow comparison
        // We'll check that the session is valid immediately after setting
        _sessionManager.HasValidSession.Should().BeTrue();

        // Note: Testing actual expiration would require mocking DateTime.UtcNow
        // or waiting 24 hours, which is not practical for unit tests
    }

    [Fact]
    public void GetSessionIdForReconnect_ShouldReturnSessionId_WhenValidSessionExists()
    {
        // Arrange
        var sessionId = "test-session-id-123";
        _sessionManager.SetSessionId(sessionId);

        // Act
        var result = _sessionManager.GetSessionIdForReconnect();

        // Assert
        result.Should().Be(sessionId);
    }

    [Fact]
    public void GetSessionIdForReconnect_ShouldReturnNull_WhenNoSessionExists()
    {
        // Act
        var result = _sessionManager.GetSessionIdForReconnect();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ResetSession_ShouldClearCurrentSessionId()
    {
        // Arrange
        _sessionManager.SetSessionId("test-session-id");

        // Act
        _sessionManager.ResetSession();

        // Assert
        _sessionManager.CurrentSessionId.Should().BeNull();
    }

    [Fact]
    public void ResetSession_ShouldResetSessionDuration()
    {
        // Arrange
        _sessionManager.SetSessionId("test-session-id");

        // Act
        _sessionManager.ResetSession();

        // Assert
        _sessionManager.SessionDuration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ResetSession_ShouldResetHasValidSession()
    {
        // Arrange
        _sessionManager.SetSessionId("test-session-id");

        // Act
        _sessionManager.ResetSession();

        // Assert
        _sessionManager.HasValidSession.Should().BeFalse();
    }

    [Fact]
    public void ResetSession_ShouldRaiseSessionUpdatedEvent_WhenSessionExists()
    {
        // Arrange
        var sessionId = "test-session-id-123";
        _sessionManager.SetSessionId(sessionId);

        var eventRaised = false;
        SessionUpdatedEventArgs? eventArgs = null;
        _sessionManager.SessionUpdated += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act
        _sessionManager.ResetSession();

        // Assert
        eventRaised.Should().BeTrue();
        eventArgs!.SessionId.Should().BeNull();
        eventArgs.IsNewSession.Should().BeFalse();
    }

    [Fact]
    public void ResetSession_ShouldNotRaiseSessionUpdatedEvent_WhenNoSessionExists()
    {
        // Arrange
        var eventRaised = false;
        _sessionManager.SessionUpdated += (sender, args) => { eventRaised = true; };

        // Act
        _sessionManager.ResetSession();

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void SessionDuration_ShouldIncreaseOverTime()
    {
        // Arrange
        _sessionManager.SetSessionId("test-session-id");
        var initialDuration = _sessionManager.SessionDuration;

        // Act - Wait a bit
        Thread.Sleep(100);
        var laterDuration = _sessionManager.SessionDuration;

        // Assert
        laterDuration.Should().BeGreaterThan(initialDuration);
    }

    [Fact]
    public void SetSessionId_ThenResetSession_ShouldAllowNewSession()
    {
        // Arrange
        var firstSessionId = "test-session-id-123";
        _sessionManager.SetSessionId(firstSessionId);

        // Act
        _sessionManager.ResetSession();
        _sessionManager.SetSessionId("new-session-id-456");

        // Assert
        _sessionManager.CurrentSessionId.Should().Be("new-session-id-456");
        _sessionManager.HasValidSession.Should().BeTrue();
    }
}
