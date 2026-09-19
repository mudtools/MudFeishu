// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mud.Feishu.WebSocket;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// ExponentialBackoffReconnectStrategy 单元测试
/// </summary>
public class ExponentialBackoffReconnectStrategyTests
{
    private readonly FeishuWebSocketOptions _options;
    private readonly Mock<ILogger<ExponentialBackoffReconnectStrategy>> _loggerMock;

    public ExponentialBackoffReconnectStrategyTests()
    {
        _options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions { Reconnect = new Mud.Feishu.WebSocket.WebSocketReconnectOptions { MaxAttempts = 10, BaseDelayMs = 1000, MaxDelayMs = 60000, TotalBudget = TimeSpan.FromMinutes(30) } };
        _loggerMock = new Mock<ILogger<ExponentialBackoffReconnectStrategy>>();
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        var action = () => new ExponentialBackoffReconnectStrategy(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldCreateInstance()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options);

        strategy.Should().NotBeNull();
    }

    [Fact]
    public void CalculateDelay_WithAttemptCount1_ShouldReturnBaseDelayWithJitter()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var delay = strategy.CalculateDelay(1);

        // 抖动范围：baseDelay ~ baseDelay * 1.25
        var baseDelay = TimeSpan.FromMilliseconds(_options.Reconnect.BaseDelayMs);
        var maxDelayWithJitter = TimeSpan.FromMilliseconds(_options.Reconnect.BaseDelayMs * 1.25);
        delay.Should().BeGreaterThanOrEqualTo(baseDelay).And.BeLessThanOrEqualTo(maxDelayWithJitter);
    }

    [Fact]
    public void CalculateDelay_WithAttemptCount2_ShouldReturnDoubleDelayWithJitter()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var delay = strategy.CalculateDelay(2);

        // 抖动范围：baseDelay*2 ~ baseDelay*2 * 1.25
        var baseDelay = TimeSpan.FromMilliseconds(_options.Reconnect.BaseDelayMs * 2);
        var maxDelayWithJitter = TimeSpan.FromMilliseconds(_options.Reconnect.BaseDelayMs * 2 * 1.25);
        delay.Should().BeGreaterThanOrEqualTo(baseDelay).And.BeLessThanOrEqualTo(maxDelayWithJitter);
    }

    [Fact]
    public void CalculateDelay_WithAttemptCount3_ShouldReturnQuadrupleDelayWithJitter()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var delay = strategy.CalculateDelay(3);

        // 抖动范围：baseDelay*4 ~ baseDelay*4 * 1.25
        var baseDelay = TimeSpan.FromMilliseconds(_options.Reconnect.BaseDelayMs * 4);
        var maxDelayWithJitter = TimeSpan.FromMilliseconds(_options.Reconnect.BaseDelayMs * 4 * 1.25);
        delay.Should().BeGreaterThanOrEqualTo(baseDelay).And.BeLessThanOrEqualTo(maxDelayWithJitter);
    }

    [Fact]
    public void CalculateDelay_WhenExceedsMaxDelay_ShouldReturnMaxDelayWithJitter()
    {
        _options.Reconnect.BaseDelayMs = 1000;
        _options.Reconnect.MaxDelayMs = 5000;
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var delay = strategy.CalculateDelay(10);

        // 抖动在封顶后添加，范围：maxDelay ~ maxDelay * 1.25
        var maxDelay = TimeSpan.FromMilliseconds(_options.Reconnect.MaxDelayMs);
        var maxDelayWithJitter = TimeSpan.FromMilliseconds(_options.Reconnect.MaxDelayMs * 1.25);
        delay.Should().BeGreaterThanOrEqualTo(maxDelay).And.BeLessThanOrEqualTo(maxDelayWithJitter);
    }

    [Fact]
    public void CalculateDelay_WithZeroAttemptCount_ShouldThrowArgumentOutOfRangeException()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options);

        var action = () => strategy.CalculateDelay(0);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("attemptCount");
    }

    [Theory]
    [InlineData(1026)]
    [InlineData(2000)]
    [InlineData(int.MaxValue)]
    public void CalculateDelay_ShouldNotOverflow_WhenAttemptCountExceeds1024(int attemptCount)
    {
        // P2-6 回归：Math.Pow(2, attemptCount - 1) 在指数 > 1024 时得到 double.PositiveInfinity，
        // TimeSpan.FromMilliseconds(∞) 抛 OverflowException，使整轮重连被异常中止
        // （触发条件：MaxReconnectAttempts = 0 无限重连 + 长时断网）。
        var options = new FeishuWebSocketOptions
        {
            Reconnect = new WebSocketReconnectOptions
            {
                BaseDelayMs = 1000,
                MaxDelayMs = 30000
            }
        };
        var strategy = new ExponentialBackoffReconnectStrategy(options, _loggerMock.Object);

        // Act：指数被钳制（MaxExponent=30）后必然被 MaxReconnectDelayMs 截断，
        // 再叠加 0~25% 抖动 → 结果必须落在 [30s, 37.5s) 且不得抛 OverflowException
        var delay = strategy.CalculateDelay(attemptCount);

        // Assert
        delay.Should()
            .BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(30000))
            .And.BeLessThanOrEqualTo(TimeSpan.FromMilliseconds(37500),
                "钳制后的指数延迟必然超过 30s 上限被截断，抖动范围为 0~25%（P2-6）");
    }

    [Fact]
    public void CalculateDelay_WithNegativeAttemptCount_ShouldThrowArgumentOutOfRangeException()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options);

        var action = () => strategy.CalculateDelay(-1);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("attemptCount");
    }

    [Fact]
    public void ShouldContinueReconnect_WhenUnderLimits_ShouldReturnTrue()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var result = strategy.ShouldContinueReconnect(1, TimeSpan.FromMinutes(1));

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldContinueReconnect_WhenExceedsMaxAttempts_ShouldReturnFalse()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var result = strategy.ShouldContinueReconnect(_options.Reconnect.MaxAttempts + 1, TimeSpan.FromMinutes(1));

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldContinueReconnect_WhenExceedsMaxTime_ShouldReturnFalse()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var result = strategy.ShouldContinueReconnect(1, _options.Reconnect.TotalBudget + TimeSpan.FromMinutes(1));

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldContinueReconnect_WhenAtExactLimit_ShouldReturnTrue()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var result = strategy.ShouldContinueReconnect(_options.Reconnect.MaxAttempts, _options.Reconnect.TotalBudget);

        result.Should().BeTrue();
    }
}
