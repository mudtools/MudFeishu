// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Utilities;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Utilities;

/// <summary>
/// TimestampHelper 单元测试
/// </summary>
public class TimestampHelperTests
{
    #region ToDateTimeOffset 测试

    [Fact]
    public void ToDateTimeOffset_WithSecondsTimestamp_ShouldReturnCorrectDateTime()
    {
        // Arrange
        var expected = DateTimeOffset.FromUnixTimeSeconds(1609459200); // 2021-01-01 00:00:00 UTC
        var timestamp = 1609459200;

        // Act
        var result = TimestampHelper.ToDateTimeOffset(timestamp);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDateTimeOffset_WithMillisecondsTimestamp_ShouldReturnCorrectDateTime()
    {
        // Arrange
        var expected = DateTimeOffset.FromUnixTimeMilliseconds(1609459200000); // 2021-01-01 00:00:00 UTC
        var timestamp = 1609459200000L;

        // Act
        var result = TimestampHelper.ToDateTimeOffset(timestamp);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDateTimeOffset_WithZeroTimestamp_ShouldReturnEpoch()
    {
        // Arrange
        var expected = DateTimeOffset.FromUnixTimeSeconds(0);
        var timestamp = 0;

        // Act
        var result = TimestampHelper.ToDateTimeOffset(timestamp);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region NormalizeToSeconds 测试

    [Fact]
    public void NormalizeToSeconds_WithSecondsTimestamp_ShouldReturnSameValue()
    {
        // Arrange
        var timestamp = 1609459200;

        // Act
        var result = TimestampHelper.NormalizeToSeconds(timestamp);

        // Assert
        Assert.Equal(timestamp, result);
    }

    [Fact]
    public void NormalizeToSeconds_WithMillisecondsTimestamp_ShouldReturnSeconds()
    {
        // Arrange
        var timestamp = 1609459200000L;
        var expected = 1609459200;

        // Act
        var result = TimestampHelper.NormalizeToSeconds(timestamp);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region NormalizeToMilliseconds 测试

    [Fact]
    public void NormalizeToMilliseconds_WithSecondsTimestamp_ShouldReturnMilliseconds()
    {
        // Arrange
        var timestamp = 1609459200;
        var expected = 1609459200000L;

        // Act
        var result = TimestampHelper.NormalizeToMilliseconds(timestamp);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeToMilliseconds_WithMillisecondsTimestamp_ShouldReturnSameValue()
    {
        // Arrange
        var timestamp = 1609459200000L;

        // Act
        var result = TimestampHelper.NormalizeToMilliseconds(timestamp);

        // Assert
        Assert.Equal(timestamp, result);
    }

    #endregion

    #region IsMilliseconds 测试

    [Fact]
    public void IsMilliseconds_WithSecondsTimestamp_ShouldReturnFalse()
    {
        // Arrange
        var timestamp = 1609459200;

        // Act
        var result = TimestampHelper.IsMilliseconds(timestamp);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMilliseconds_WithMillisecondsTimestamp_ShouldReturnTrue()
    {
        // Arrange
        var timestamp = 1609459200000L;

        // Act
        var result = TimestampHelper.IsMilliseconds(timestamp);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMilliseconds_WithThresholdValue_ShouldReturnFalse()
    {
        // Arrange - 刚好是阈值 10000000000 (约 2286-11-20)
        var timestamp = 10000000000L;

        // Act
        var result = TimestampHelper.IsMilliseconds(timestamp);

        // Assert
        Assert.True(result); // 等于阈值时认为是毫秒级
    }

    #endregion
}
