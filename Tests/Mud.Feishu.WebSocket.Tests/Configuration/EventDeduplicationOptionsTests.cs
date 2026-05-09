// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;

namespace Mud.Feishu.WebSocket.Tests.Configuration;

/// <summary>
/// EventDeduplicationOptions 和 MessageSizeLimits 配置测试类
/// </summary>
public class EventDeduplicationOptionsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var options = new EventDeduplicationOptions();

        // Assert
        options.Mode.Should().Be(EventDeduplicationMode.InMemory);
        options.CacheExpirationMs.Should().Be(48 * 60 * 60 * 1000); // 48 hours
        options.CleanupIntervalMs.Should().Be(5 * 60 * 1000); // 5 minutes
    }

    [Fact]
    public void CacheExpirationMs_ShouldEnforceMinimumValue()
    {
        // Arrange
        var options = new EventDeduplicationOptions();

        // Act
        options.CacheExpirationMs = 30000; // 30 seconds

        // Assert
        options.CacheExpirationMs.Should().Be(60000, "minimum value should be enforced");
    }

    [Fact]
    public void CacheExpirationMs_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new EventDeduplicationOptions();
        var expectedValue = 12 * 60 * 60 * 1000; // 12 hours

        // Act
        options.CacheExpirationMs = expectedValue;

        // Assert
        options.CacheExpirationMs.Should().Be(expectedValue);
    }

    [Fact]
    public void CleanupIntervalMs_ShouldEnforceMinimumValue()
    {
        // Arrange
        var options = new EventDeduplicationOptions();

        // Act
        options.CleanupIntervalMs = 30000; // 30 seconds

        // Assert
        options.CleanupIntervalMs.Should().Be(60000, "minimum value should be enforced");
    }

    [Fact]
    public void CleanupIntervalMs_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new EventDeduplicationOptions();
        var expectedValue = 10 * 60 * 1000; // 10 minutes

        // Act
        options.CleanupIntervalMs = expectedValue;

        // Assert
        options.CleanupIntervalMs.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(EventDeduplicationMode.None)]
    [InlineData(EventDeduplicationMode.InMemory)]
    [InlineData(EventDeduplicationMode.Distributed)]
    public void Mode_ShouldAcceptAllValues(EventDeduplicationMode mode)
    {
        // Arrange
        var options = new EventDeduplicationOptions();

        // Act
        options.Mode = mode;

        // Assert
        options.Mode.Should().Be(mode);
    }

    [Fact]
    public void CacheExpirationMs_ShouldHandleLargeValues()
    {
        // Arrange
        var options = new EventDeduplicationOptions();
        var largeValue = 7 * 24 * 60 * 60 * 1000; // 7 days

        // Act
        options.CacheExpirationMs = largeValue;

        // Assert
        options.CacheExpirationMs.Should().Be(largeValue);
    }
}

/// <summary>
/// MessageSizeLimits 测试类
/// </summary>
public class MessageSizeLimitsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var limits = new MessageSizeLimits();

        // Assert
        limits.MaxTextMessageSize.Should().Be(1024 * 1024); // 1MB
        limits.MaxBinaryMessageSize.Should().Be(10 * 1024 * 1024); // 10MB
    }

    [Fact]
    public void MaxTextMessageSize_ShouldAcceptValidValue()
    {
        // Arrange
        var limits = new MessageSizeLimits();
        var expectedValue = 2 * 1024 * 1024; // 2MB

        // Act
        limits.MaxTextMessageSize = expectedValue;

        // Assert
        limits.MaxTextMessageSize.Should().Be(expectedValue);
    }

    [Fact]
    public void MaxBinaryMessageSize_ShouldAcceptValidValue()
    {
        // Arrange
        var limits = new MessageSizeLimits();
        var expectedValue = 20 * 1024 * 1024; // 20MB

        // Act
        limits.MaxBinaryMessageSize = expectedValue;

        // Assert
        limits.MaxBinaryMessageSize.Should().Be(expectedValue);
    }

    [Fact]
    public void MaxTextMessageSize_ShouldAcceptSmallValue()
    {
        // Arrange
        var limits = new MessageSizeLimits();
        var smallValue = 1024; // 1KB

        // Act
        limits.MaxTextMessageSize = smallValue;

        // Assert
        limits.MaxTextMessageSize.Should().Be(smallValue);
    }

    [Fact]
    public void MaxBinaryMessageSize_ShouldAcceptSmallValue()
    {
        // Arrange
        var limits = new MessageSizeLimits();
        var smallValue = 1024; // 1KB

        // Act
        limits.MaxBinaryMessageSize = smallValue;

        // Assert
        limits.MaxBinaryMessageSize.Should().Be(smallValue);
    }

    [Fact]
    public void MaxTextMessageSize_ShouldAcceptLargeValue()
    {
        // Arrange
        var limits = new MessageSizeLimits();
        var largeValue = 100 * 1024 * 1024; // 100MB

        // Act
        limits.MaxTextMessageSize = largeValue;

        // Assert
        limits.MaxTextMessageSize.Should().Be(largeValue);
    }

    [Fact]
    public void MaxBinaryMessageSize_ShouldAcceptLargeValue()
    {
        // Arrange
        var limits = new MessageSizeLimits();
        var largeValue = 100 * 1024 * 1024; // 100MB

        // Act
        limits.MaxBinaryMessageSize = largeValue;

        // Assert
        limits.MaxBinaryMessageSize.Should().Be(largeValue);
    }
}
