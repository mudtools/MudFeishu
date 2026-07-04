// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
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
        options.CacheExpiration.Should().Be(TimeSpan.FromHours(48)); // 48 hours
        options.CleanupInterval.Should().Be(TimeSpan.FromMinutes(5)); // 5 minutes
    }

    [Fact]
    public void CacheExpiration_ShouldEnforceMinimumValue()
    {
        // Arrange
        var options = new EventDeduplicationOptions();

        // Act
        options.CacheExpiration = TimeSpan.FromSeconds(30); // 30 seconds

        // Assert
        options.CacheExpiration.Should().Be(TimeSpan.FromSeconds(60), "minimum value should be enforced");
    }

    [Fact]
    public void CacheExpiration_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new EventDeduplicationOptions();
        var expectedValue = TimeSpan.FromHours(12); // 12 hours

        // Act
        options.CacheExpiration = expectedValue;

        // Assert
        options.CacheExpiration.Should().Be(expectedValue);
    }

    [Fact]
    public void CleanupInterval_ShouldEnforceMinimumValue()
    {
        // Arrange
        var options = new EventDeduplicationOptions();

        // Act
        options.CleanupInterval = TimeSpan.FromSeconds(30); // 30 seconds

        // Assert
        options.CleanupInterval.Should().Be(TimeSpan.FromSeconds(60), "minimum value should be enforced");
    }

    [Fact]
    public void CleanupInterval_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new EventDeduplicationOptions();
        var expectedValue = TimeSpan.FromMinutes(10); // 10 minutes

        // Act
        options.CleanupInterval = expectedValue;

        // Assert
        options.CleanupInterval.Should().Be(expectedValue);
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
    public void CacheExpiration_ShouldHandleLargeValues()
    {
        // Arrange
        var options = new EventDeduplicationOptions();
        var largeValue = TimeSpan.FromDays(7); // 7 days

        // Act
        options.CacheExpiration = largeValue;

        // Assert
        options.CacheExpiration.Should().Be(largeValue);
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
