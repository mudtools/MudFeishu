// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
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
        options.MessageSizeLimits.MaxBinaryMessageSize.Should().Be(10 * 1024 * 1024); // 10MB
        options.HealthCheckIntervalMs.Should().Be(60000);
        options.EventDeduplication.Mode.Should().Be(Mud.Feishu.WebSocket.EventDeduplicationMode.InMemory);
        options.EventDeduplication.CacheExpiration.Should().Be(TimeSpan.FromHours(48));
        options.EventDeduplication.CleanupInterval.Should().Be(TimeSpan.FromMinutes(5));
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
    public void EventDeduplicationCacheExpiration_ShouldEnforceMinimumValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();

        // Act
        options.EventDeduplication.CacheExpiration = TimeSpan.FromMilliseconds(30000);

        // Assert
        options.EventDeduplication.CacheExpiration.Should().Be(TimeSpan.FromMilliseconds(60000), "minimum value should be enforced");
    }

    [Fact]
    public void EventDeduplicationCacheExpiration_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        var expectedValue = TimeSpan.FromHours(12);

        // Act
        options.EventDeduplication.CacheExpiration = expectedValue;

        // Assert
        options.EventDeduplication.CacheExpiration.Should().Be(expectedValue);
    }

    [Fact]
    public void EventDeduplicationCleanupInterval_ShouldEnforceMinimumValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();

        // Act
        options.EventDeduplication.CleanupInterval = TimeSpan.FromMilliseconds(30000);

        // Assert
        options.EventDeduplication.CleanupInterval.Should().Be(TimeSpan.FromMilliseconds(60000), "minimum value should be enforced");
    }

    [Fact]
    public void EventDeduplicationCleanupInterval_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        var expectedValue = TimeSpan.FromMinutes(10);

        // Act
        options.EventDeduplication.CleanupInterval = expectedValue;

        // Assert
        options.EventDeduplication.CleanupInterval.Should().Be(expectedValue);
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

        // Assert
        options.AutoReconnect.Should().Be(value);
        options.EnableLogging.Should().Be(value);
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
        options.MessageSizeLimits.MaxBinaryMessageSize = 20 * 1024 * 1024; // 20MB

        // Assert
        options.MaxReconnectAttempts.Should().Be(10);
        options.InitialReceiveBufferSize.Should().Be(8192);
        options.ConnectionTimeoutMs.Should().Be(20000);
        options.MessageSizeLimits.MaxTextMessageSize.Should().Be(2 * 1024 * 1024);
        options.MessageSizeLimits.MaxBinaryMessageSize.Should().Be(20 * 1024 * 1024);
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenAllValuesAreDefault()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMaxReconnectAttemptsIsNegative()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions
        {
            MaxReconnectAttempts = -1
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxReconnectAttempts*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenReconnectDelayMsLessThan1000()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        typeof(Mud.Feishu.WebSocket.FeishuWebSocketOptions)
            .GetField("_reconnectDelayMs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(options, 500);

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*ReconnectDelayMs*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMaxReconnectDelayMsLessThanReconnectDelayMs()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions
        {
            ReconnectDelayMs = 10000
        };
        typeof(Mud.Feishu.WebSocket.FeishuWebSocketOptions)
            .GetField("_maxReconnectDelayMs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(options, 5000);

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxReconnectDelayMs*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenInitialReceiveBufferSizeLessThan1024()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions
        {
            InitialReceiveBufferSize = 512
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*InitialReceiveBufferSize*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenHeartbeatIntervalMsLessThan5000()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        typeof(Mud.Feishu.WebSocket.FeishuWebSocketOptions)
            .GetField("_heartbeatIntervalMs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(options, 3000);

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*HeartbeatIntervalMs*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenConnectionTimeoutMsLessThan1000()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions
        {
            ConnectionTimeoutMs = 500
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionTimeoutMs*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenReconnectDelayMsGreaterThanConnectionTimeoutMs()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions
        {
            AutoReconnect = true,
            ReconnectDelayMs = 15000,
            ConnectionTimeoutMs = 10000
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*ReconnectDelayMs*ConnectionTimeoutMs*");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenReconnectDelayMsGreaterThanConnectionTimeoutMsButAutoReconnectFalse()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions
        {
            AutoReconnect = false,
            ReconnectDelayMs = 15000,
            ConnectionTimeoutMs = 10000
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMaxTextMessageSizeLessThan1024()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions
        {
            MessageSizeLimits = new MessageSizeLimits { MaxTextMessageSize = 512, MaxBinaryMessageSize = 1024 }
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxTextMessageSize*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMaxBinaryMessageSizeLessThan1024()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions
        {
            MessageSizeLimits = new MessageSizeLimits { MaxTextMessageSize = 1024, MaxBinaryMessageSize = 512 }
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxBinaryMessageSize*");
    }

    /// <summary>
    /// BUG-4: None 模式不应阻止启动（默认配置时不应抛异常）
    /// </summary>
    [Fact]
    public void Validate_WithNoneMode_ShouldNotThrow_WhenDefaultValues()
    {
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions
        {
            EventDeduplication = new EventDeduplicationOptions
            {
                Mode = EventDeduplicationMode.None,
                CacheExpiration = EventDeduplicationOptions.DefaultCacheExpiration,
                CleanupInterval = EventDeduplicationOptions.DefaultCleanupInterval
            }
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    /// <summary>
    /// BUG-4: None 模式下有自定义缓存配置时仍应抛异常
    /// </summary>
    [Fact]
    public void Validate_WithNoneMode_ShouldThrow_WhenCustomCacheExpiration()
    {
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions
        {
            EventDeduplication = new EventDeduplicationOptions
            {
                Mode = EventDeduplicationMode.None,
                CacheExpiration = TimeSpan.FromHours(24)
            }
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*None*CacheExpiration*");
    }
}
