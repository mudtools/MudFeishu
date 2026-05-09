// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;

namespace Mud.Feishu.WebSocket.Tests.Configuration;

/// <summary>
/// FeishuWebSocketOptions 配置测试类
/// </summary>
public class FeishuWebSocketOptionsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();

        // Assert
        options.AutoReconnect.Should().BeTrue();
        options.MaxReconnectAttempts.Should().Be(5);
        options.ReconnectDelayMs.Should().Be(5000);
        options.MaxReconnectDelayMs.Should().Be(30000);
        options.InitialReceiveBufferSize.Should().Be(4096);
        options.HeartbeatIntervalMs.Should().Be(25000);
        options.ConnectionTimeoutMs.Should().Be(10000);
        options.EnableLogging.Should().BeTrue();
        options.MessageSizeLimits.MaxTextMessageSize.Should().Be(1024 * 1024); // 1MB
        options.EnableMessageQueue.Should().BeTrue();
        options.MessageQueueCapacity.Should().Be(1000);
        options.EmptyQueueCheckIntervalMs.Should().Be(100);
        options.MessageSizeLimits.MaxBinaryMessageSize.Should().Be(10 * 1024 * 1024); // 10MB
        options.HealthCheckIntervalMs.Should().Be(60000);
        options.EventDeduplication.Mode.Should().Be(Mud.Feishu.WebSocket.EventDeduplicationMode.InMemory);
        options.EventDeduplication.CacheExpirationMs.Should().Be(48 * 60 * 60 * 1000);
        options.EventDeduplication.CleanupIntervalMs.Should().Be(5 * 60 * 1000);
        options.MaxConcurrentMessageProcessing.Should().Be(10);
    }

    [Fact]
    public void ReconnectDelayMs_ShouldEnforceMinimumValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();

        // Act
        options.ReconnectDelayMs = 500;

        // Assert
        options.ReconnectDelayMs.Should().Be(1000, "minimum value should be enforced");
    }

    [Fact]
    public void ReconnectDelayMs_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        var expectedValue = 8000;

        // Act
        options.ReconnectDelayMs = expectedValue;

        // Assert
        options.ReconnectDelayMs.Should().Be(expectedValue);
    }

    [Fact]
    public void MaxReconnectDelayMs_ShouldEnforceMinimumValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        options.ReconnectDelayMs = 10000;

        // Act
        options.MaxReconnectDelayMs = 5000;

        // Assert
        options.MaxReconnectDelayMs.Should().Be(options.ReconnectDelayMs, "should be at least ReconnectDelayMs");
    }

    [Fact]
    public void MaxReconnectDelayMs_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        var expectedValue = 60000;

        // Act
        options.MaxReconnectDelayMs = expectedValue;

        // Assert
        options.MaxReconnectDelayMs.Should().Be(expectedValue);
    }

    [Fact]
    public void HeartbeatIntervalMs_ShouldEnforceMinimumValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();

        // Act
        options.HeartbeatIntervalMs = 500;

        // Assert
        options.HeartbeatIntervalMs.Should().Be(5000, "minimum value should be enforced to 5000ms for improved stability");
    }

    [Fact]
    public void HeartbeatIntervalMs_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        var expectedValue = 60000;

        // Act
        options.HeartbeatIntervalMs = expectedValue;

        // Assert
        options.HeartbeatIntervalMs.Should().Be(expectedValue);
    }

    [Fact]
    public void EmptyQueueCheckIntervalMs_ShouldEnforceMinimumValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();

        // Act
        options.EmptyQueueCheckIntervalMs = 5;

        // Assert
        options.EmptyQueueCheckIntervalMs.Should().Be(10, "minimum value should be enforced");
    }

    [Fact]
    public void EmptyQueueCheckIntervalMs_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        var expectedValue = 200;

        // Act
        options.EmptyQueueCheckIntervalMs = expectedValue;

        // Assert
        options.EmptyQueueCheckIntervalMs.Should().Be(expectedValue);
    }

    [Fact]
    public void HealthCheckIntervalMs_ShouldEnforceMinimumValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();

        // Act
        options.HealthCheckIntervalMs = 500;

        // Assert
        options.HealthCheckIntervalMs.Should().Be(1000, "minimum value should be enforced");
    }

    [Fact]
    public void HealthCheckIntervalMs_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        var expectedValue = 120000;

        // Act
        options.HealthCheckIntervalMs = expectedValue;

        // Assert
        options.HealthCheckIntervalMs.Should().Be(expectedValue);
    }

    [Fact]
    public void MaxConcurrentMessageProcessing_ShouldEnforceMinimumValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();

        // Act
        options.MaxConcurrentMessageProcessing = 0;

        // Assert
        options.MaxConcurrentMessageProcessing.Should().Be(1, "minimum value should be enforced");
    }

    [Fact]
    public void MaxConcurrentMessageProcessing_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        var expectedValue = 20;

        // Act
        options.MaxConcurrentMessageProcessing = expectedValue;

        // Assert
        options.MaxConcurrentMessageProcessing.Should().Be(expectedValue);
    }

    [Fact]
    public void EventDeduplicationCacheExpirationMs_ShouldEnforceMinimumValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();

        // Act
        options.EventDeduplication.CacheExpirationMs = 30000;

        // Assert
        options.EventDeduplication.CacheExpirationMs.Should().Be(60000, "minimum value should be enforced");
    }

    [Fact]
    public void EventDeduplicationCacheExpirationMs_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        var expectedValue = 12 * 60 * 60 * 1000; // 12 hours

        // Act
        options.EventDeduplication.CacheExpirationMs = expectedValue;

        // Assert
        options.EventDeduplication.CacheExpirationMs.Should().Be(expectedValue);
    }

    [Fact]
    public void EventDeduplicationCleanupIntervalMs_ShouldEnforceMinimumValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();

        // Act
        options.EventDeduplication.CleanupIntervalMs = 30000;

        // Assert
        options.EventDeduplication.CleanupIntervalMs.Should().Be(60000, "minimum value should be enforced");
    }

    [Fact]
    public void EventDeduplicationCleanupIntervalMs_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        var expectedValue = 10 * 60 * 1000; // 10 minutes

        // Act
        options.EventDeduplication.CleanupIntervalMs = expectedValue;

        // Assert
        options.EventDeduplication.CleanupIntervalMs.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BooleanProperties_ShouldAcceptBothValues(bool value)
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();

        // Act
        options.AutoReconnect = value;
        options.EnableLogging = value;
        options.EnableMessageQueue = value;

        // Assert
        options.AutoReconnect.Should().Be(value);
        options.EnableLogging.Should().Be(value);
        options.EnableMessageQueue.Should().Be(value);
    }

    [Theory]
    [InlineData(Mud.Feishu.WebSocket.EventDeduplicationMode.None)]
    [InlineData(Mud.Feishu.WebSocket.EventDeduplicationMode.InMemory)]
    [InlineData(Mud.Feishu.WebSocket.EventDeduplicationMode.Distributed)]
    public void EventDeduplicationMode_ShouldAcceptAllValues(Mud.Feishu.WebSocket.EventDeduplicationMode mode)
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();

        // Act
        options.EventDeduplication.Mode = mode;

        // Assert
        options.EventDeduplication.Mode.Should().Be(mode);
    }

    [Fact]
    public void IntegerProperties_ShouldAcceptVariousValues()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();

        // Act
        options.MaxReconnectAttempts = 10;
        options.InitialReceiveBufferSize = 8192;
        options.ConnectionTimeoutMs = 20000;
        options.MessageSizeLimits.MaxTextMessageSize = 2 * 1024 * 1024; // 2MB
        options.MessageQueueCapacity = 2000;
        options.MessageSizeLimits.MaxBinaryMessageSize = 20 * 1024 * 1024; // 20MB

        // Assert
        options.MaxReconnectAttempts.Should().Be(10);
        options.InitialReceiveBufferSize.Should().Be(8192);
        options.ConnectionTimeoutMs.Should().Be(20000);
        options.MessageSizeLimits.MaxTextMessageSize.Should().Be(2 * 1024 * 1024);
        options.MessageQueueCapacity.Should().Be(2000);
        options.MessageSizeLimits.MaxBinaryMessageSize.Should().Be(20 * 1024 * 1024);
    }
}
