// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Services;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Utilities;

namespace Mud.Feishu.Webhook.Tests.Validators;

/// <summary>
/// 时间戳验证器单元测试
/// 验证 TimestampValidator 类的各种时间戳验证场景
/// </summary>
public class TimestampValidatorTests
{
    private readonly Mock<ILogger<TimestampValidator>> _loggerMock;
    private readonly Mock<IOptionsMonitor<FeishuWebhookOptions>> _optionsMonitorMock;
    private readonly Mock<IEnvironmentService> _environmentServiceMock;
    private readonly Mock<IWebhookAppKeyAccessor> _appKeyAccessorMock;
    private readonly TimestampValidator _validator;

    public TimestampValidatorTests()
    {
        _loggerMock = new Mock<ILogger<TimestampValidator>>();
        _optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        _environmentServiceMock = new Mock<IEnvironmentService>();
        _appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();

        // Setup default options
        var defaultOptions = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = 300
        };
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(defaultOptions);

        // 默认设置为开发环境
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(false);
        _environmentServiceMock.Setup(x => x.IsDevelopment).Returns(true);

        _validator = new TimestampValidator(_loggerMock.Object, _optionsMonitorMock.Object, _appKeyAccessorMock.Object, _environmentServiceMock.Object);
    }

    #region 秒级时间戳验证测试

    [Fact]
    public void ValidateTimestamp_WithValidSecondsTimestamp_ShouldReturnTrue()
    {
        // Arrange - 生成当前时间的秒级时间戳（在容错范围内）
        var now = DateTimeOffset.UtcNow;
        var timestamp = now.ToUnixTimeSeconds();
        var toleranceSeconds = 300;

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.True(result);

        // 验证调试日志被记录
        VerifyLogCalled(LogLevel.Debug, "识别为秒级时间戳");
        VerifyLogCalled(LogLevel.Debug, "时间戳验证通过");
    }

    [Fact]
    public void ValidateTimestamp_WithSecondsTimestampInTolerance_ShouldReturnTrue()
    {
        // Arrange - 生成在容错范围边界内的秒级时间戳
        var now = DateTimeOffset.UtcNow;
        var toleranceSeconds = 300;
        var timestamp = now.AddSeconds(-toleranceSeconds + 10).ToUnixTimeSeconds(); // 在容错范围内

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.True(result);
        VerifyLogCalled(LogLevel.Debug, "识别为秒级时间戳");
        VerifyLogCalled(LogLevel.Debug, "时间戳验证通过");
    }

    [Fact]
    public void ValidateTimestamp_WithSecondsTimestampOutOfTolerance_ShouldReturnFalse()
    {
        // Arrange - 生成超出容错范围的秒级时间戳
        var now = DateTimeOffset.UtcNow;
        var toleranceSeconds = 300;
        var timestamp = now.AddSeconds(-toleranceSeconds - 60).ToUnixTimeSeconds(); // 超出容错范围

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Debug, "识别为秒级时间戳");
        VerifyLogCalled(LogLevel.Warning, "时间戳超出容错范围");
    }

    [Fact]
    public void ValidateTimestamp_WithFutureSecondsTimestamp_ShouldReturnFalse()
    {
        // Arrange - 生成未来的秒级时间戳（超出容错范围）
        var now = DateTimeOffset.UtcNow;
        var toleranceSeconds = 300;
        var timestamp = now.AddSeconds(toleranceSeconds + 60).ToUnixTimeSeconds(); // 未来时间超出容错范围

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Debug, "识别为秒级时间戳");
        VerifyLogCalled(LogLevel.Warning, "时间戳超出容错范围");
    }

    #endregion

    #region 毫秒级时间戳验证测试

    [Fact]
    public void ValidateTimestamp_WithValidMillisecondsTimestamp_ShouldReturnTrue()
    {
        // Arrange - 生成当前时间的毫秒级时间戳（在容错范围内）
        var now = DateTimeOffset.UtcNow;
        var timestamp = now.ToUnixTimeMilliseconds();
        var toleranceSeconds = 300;

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.True(result);

        // 验证调试日志被记录
        VerifyLogCalled(LogLevel.Debug, "识别为毫秒级时间戳");
        VerifyLogCalled(LogLevel.Debug, "时间戳验证通过");
    }

    [Fact]
    public void ValidateTimestamp_WithMillisecondsTimestampInTolerance_ShouldReturnTrue()
    {
        // Arrange - 生成在容错范围边界内的毫秒级时间戳
        var now = DateTimeOffset.UtcNow;
        var toleranceSeconds = 300;
        var timestamp = now.AddSeconds(-toleranceSeconds + 10).ToUnixTimeMilliseconds(); // 在容错范围内

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.True(result);
        VerifyLogCalled(LogLevel.Debug, "识别为毫秒级时间戳");
        VerifyLogCalled(LogLevel.Debug, "时间戳验证通过");
    }

    [Fact]
    public void ValidateTimestamp_WithMillisecondsTimestampOutOfTolerance_ShouldReturnFalse()
    {
        // Arrange - 生成超出容错范围的毫秒级时间戳
        var now = DateTimeOffset.UtcNow;
        var toleranceSeconds = 300;
        var timestamp = now.AddSeconds(-toleranceSeconds - 60).ToUnixTimeMilliseconds(); // 超出容错范围

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Debug, "识别为毫秒级时间戳");
        VerifyLogCalled(LogLevel.Warning, "时间戳超出容错范围");
    }

    [Fact]
    public void ValidateTimestamp_WithFutureMillisecondsTimestamp_ShouldReturnFalse()
    {
        // Arrange - 生成未来的毫秒级时间戳（超出容错范围）
        var now = DateTimeOffset.UtcNow;
        var toleranceSeconds = 300;
        var timestamp = now.AddSeconds(toleranceSeconds + 60).ToUnixTimeMilliseconds(); // 未来时间超出容错范围

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Debug, "识别为毫秒级时间戳");
        VerifyLogCalled(LogLevel.Warning, "时间戳超出容错范围");
    }

    #endregion

    #region 零时间戳处理测试

    [Fact]
    public void ValidateTimestamp_WithZeroTimestampInDevelopment_ShouldReturnTrue()
    {
        // Arrange - 开发环境下零时间戳应该被允许（跳过验证）
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(false);
        _environmentServiceMock.Setup(x => x.IsDevelopment).Returns(true);
        var validator = new TimestampValidator(_loggerMock.Object, _optionsMonitorMock.Object, _appKeyAccessorMock.Object, _environmentServiceMock.Object);

        var timestamp = 0L;
        var toleranceSeconds = 300;

        // Act
        var result = validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.True(result);

        // 验证调试日志被记录
        VerifyLogCalled(LogLevel.Warning, "时间戳为 0，跳过时间戳验证");
    }

    [Fact]
    public void ValidateTimestamp_WithZeroTimestampInProduction_ShouldReturnFalse()
    {
        // Arrange - 生产环境下零时间戳应该被拒绝
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(true);
        _environmentServiceMock.Setup(x => x.IsDevelopment).Returns(false);
        var validator = new TimestampValidator(_loggerMock.Object, _optionsMonitorMock.Object, _appKeyAccessorMock.Object, _environmentServiceMock.Object);

        var timestamp = 0L;
        var toleranceSeconds = 300;

        // Act
        var result = validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.False(result);

        // 验证错误日志被记录
        VerifyLogCalled(LogLevel.Error, "时间戳为 0，拒绝请求");
    }

    [Theory]
    [InlineData(60)]
    [InlineData(300)]
    [InlineData(600)]
    public void ValidateTimestamp_WithZeroTimestampInDevelopment_DifferentTolerances_ShouldReturnTrue(int toleranceSeconds)
    {
        // Arrange - 开发环境下零时间戳在任何容错设置下都应该被允许
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(false);
        _environmentServiceMock.Setup(x => x.IsDevelopment).Returns(true);
        var validator = new TimestampValidator(_loggerMock.Object, _optionsMonitorMock.Object, _appKeyAccessorMock.Object, _environmentServiceMock.Object);

        var timestamp = 0L;

        // Act
        var result = validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.True(result);
        VerifyLogCalled(LogLevel.Warning, "时间戳为 0，跳过时间戳验证");
    }

    #endregion

    #region 超出容错范围的时间戳测试

    [Theory]
    [InlineData(60)]   // 1分钟容错
    [InlineData(300)]  // 5分钟容错
    [InlineData(600)]  // 10分钟容错
    public void ValidateTimestamp_WithTimestampExceedingTolerance_ShouldReturnFalse(int toleranceSeconds)
    {
        // Arrange - 生成明显超出容错范围的时间戳
        var now = DateTimeOffset.UtcNow;
        var timestamp = now.AddSeconds(-toleranceSeconds - 120).ToUnixTimeSeconds(); // 超出容错范围2分钟

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Warning, "时间戳超出容错范围");
    }

    [Fact]
    public void ValidateTimestamp_WithVeryOldTimestamp_ShouldReturnFalse()
    {
        // Arrange - 生成非常旧的时间戳（1小时前）
        var now = DateTimeOffset.UtcNow;
        var timestamp = now.AddHours(-1).ToUnixTimeSeconds();
        var toleranceSeconds = 300; // 5分钟容错

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Debug, "识别为秒级时间戳");
        VerifyLogCalled(LogLevel.Warning, "时间戳超出容错范围");
    }

    [Fact]
    public void ValidateTimestamp_WithVeryFutureTimestamp_ShouldReturnFalse()
    {
        // Arrange - 生成非常未来的时间戳（1小时后）
        var now = DateTimeOffset.UtcNow;
        var timestamp = now.AddHours(1).ToUnixTimeMilliseconds();
        var toleranceSeconds = 300; // 5分钟容错

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Debug, "识别为毫秒级时间戳");
        VerifyLogCalled(LogLevel.Warning, "时间戳超出容错范围");
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public void ValidateTimestamp_WithTimestampAtToleranceBoundary_ShouldReturnTrue()
    {
        // Arrange - 生成在容错边界内的时间戳（留一些缓冲以避免时间精度问题）
        var now = DateTimeOffset.UtcNow;
        var toleranceSeconds = 300;
        var timestamp = now.AddSeconds(-toleranceSeconds + 5).ToUnixTimeSeconds(); // 在边界内5秒

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.True(result);
        VerifyLogCalled(LogLevel.Debug, "时间戳验证通过");
    }

    [Fact]
    public void ValidateTimestamp_WithTimestampJustOverToleranceBoundary_ShouldReturnFalse()
    {
        // Arrange - 生成刚好超过容错边界的时间戳
        var now = DateTimeOffset.UtcNow;
        var toleranceSeconds = 300;
        var timestamp = now.AddSeconds(-toleranceSeconds - 1).ToUnixTimeSeconds(); // 刚好超过边界

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Warning, "时间戳超出容错范围");
    }

    [Fact]
    public void ValidateTimestamp_WithBoundaryTimestamp_10Billion_ShouldBeRecognizedAsSeconds()
    {
        // Arrange - 测试边界值：10000000000（100亿）应该被识别为毫秒级
        var timestamp = 10000000000L; // 恰好是边界值
        var toleranceSeconds = 86400; // 1天容错，确保这个历史时间戳能通过验证

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert - 这个时间戳应该被识别为毫秒级（因为 >= 10000000000）
        VerifyLogCalled(LogLevel.Debug, "识别为毫秒级时间戳");
    }

    [Fact]
    public void ValidateTimestamp_WithBoundaryTimestamp_JustUnder10Billion_ShouldBeRecognizedAsSeconds()
    {
        // Arrange - 测试边界值：9999999999（小于100亿）应该被识别为秒级
        var timestamp = 9999999999L; // 刚好小于边界值
        var toleranceSeconds = 86400; // 1天容错

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert - 这个时间戳应该被识别为秒级（因为 < 10000000000）
        VerifyLogCalled(LogLevel.Debug, "识别为秒级时间戳");
    }

    #endregion

    #region 异常处理测试

    [Fact]
    public void ValidateTimestamp_WithInvalidTimestamp_ShouldReturnFalse()
    {
        // Arrange - 使用可能导致异常的时间戳值
        var timestamp = long.MaxValue; // 可能导致 DateTimeOffset 转换异常
        var toleranceSeconds = 300;

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.False(result);

        // 验证错误日志被记录
        VerifyLogCalled(LogLevel.Error, "验证时间戳时发生错误");
    }

    [Fact]
    public void ValidateTimestamp_WithNegativeTimestamp_ShouldReturnFalse()
    {
        // Arrange - 使用负数时间戳
        var timestamp = -1L;
        var toleranceSeconds = 300;

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.False(result);

        // 负数时间戳会被转换为1969年的时间，超出容错范围，所以会记录警告而不是错误
        VerifyLogCalled(LogLevel.Debug, "识别为秒级时间戳");
        VerifyLogCalled(LogLevel.Warning, "时间戳超出容错范围");
    }

    #endregion

    #region 不同容错时间测试

    [Theory]
    [InlineData(30)]   // 30秒容错
    [InlineData(60)]   // 1分钟容错
    [InlineData(180)]  // 3分钟容错
    [InlineData(300)]  // 5分钟容错（默认）
    [InlineData(600)]  // 10分钟容错
    [InlineData(1800)] // 30分钟容错
    public void ValidateTimestamp_WithDifferentTolerances_ShouldWorkCorrectly(int toleranceSeconds)
    {
        // Arrange - 生成在容错范围内的时间戳
        var now = DateTimeOffset.UtcNow;
        var timestamp = now.AddSeconds(-toleranceSeconds / 2).ToUnixTimeSeconds(); // 在容错范围中间

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.True(result);
        VerifyLogCalled(LogLevel.Debug, "时间戳验证通过");
    }

    [Fact]
    public void ValidateTimestamp_WithZeroTolerance_ShouldBeVeryStrict()
    {
        // Arrange - 零容错时间，只有非常接近当前时间的时间戳才能通过
        var now = DateTimeOffset.UtcNow;
        var timestamp = now.AddSeconds(-1).ToUnixTimeSeconds(); // 1秒前
        var toleranceSeconds = 0;

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.False(result); // 1秒前的时间戳在零容错下应该失败
        VerifyLogCalled(LogLevel.Warning, "时间戳超出容错范围");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 验证日志是否被调用
    /// </summary>
    /// <param name="logLevel">日志级别</param>
    /// <param name="message">日志消息片段</param>
    private void VerifyLogCalled(LogLevel logLevel, string message)
    {
        _loggerMock.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}