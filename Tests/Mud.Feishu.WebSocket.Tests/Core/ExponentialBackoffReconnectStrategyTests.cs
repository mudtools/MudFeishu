// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
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
        _options = new FeishuWebSocketOptions
        {
            MaxReconnectAttempts = 10,
            ReconnectDelayMs = 1000,
            MaxReconnectDelayMs = 60000,
            MaxTotalReconnectTime = TimeSpan.FromMinutes(30)
        };
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
    public void CalculateDelay_WithAttemptCount1_ShouldReturnBaseDelay()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var delay = strategy.CalculateDelay(1);

        delay.Should().Be(TimeSpan.FromMilliseconds(_options.ReconnectDelayMs));
    }

    [Fact]
    public void CalculateDelay_WithAttemptCount2_ShouldReturnDoubleDelay()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var delay = strategy.CalculateDelay(2);

        delay.Should().Be(TimeSpan.FromMilliseconds(_options.ReconnectDelayMs * 2));
    }

    [Fact]
    public void CalculateDelay_WithAttemptCount3_ShouldReturnQuadrupleDelay()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var delay = strategy.CalculateDelay(3);

        delay.Should().Be(TimeSpan.FromMilliseconds(_options.ReconnectDelayMs * 4));
    }

    [Fact]
    public void CalculateDelay_WhenExceedsMaxDelay_ShouldReturnMaxDelay()
    {
        _options.ReconnectDelayMs = 1000;
        _options.MaxReconnectDelayMs = 5000;
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var delay = strategy.CalculateDelay(10);

        delay.Should().Be(TimeSpan.FromMilliseconds(_options.MaxReconnectDelayMs));
    }

    [Fact]
    public void CalculateDelay_WithZeroAttemptCount_ShouldThrowArgumentOutOfRangeException()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options);

        var action = () => strategy.CalculateDelay(0);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("attemptCount");
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

        var result = strategy.ShouldContinueReconnect(_options.MaxReconnectAttempts + 1, TimeSpan.FromMinutes(1));

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldContinueReconnect_WhenExceedsMaxTime_ShouldReturnFalse()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var result = strategy.ShouldContinueReconnect(1, _options.MaxTotalReconnectTime + TimeSpan.FromMinutes(1));

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldContinueReconnect_WhenAtExactLimit_ShouldReturnTrue()
    {
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var result = strategy.ShouldContinueReconnect(_options.MaxReconnectAttempts, _options.MaxTotalReconnectTime);

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldContinueReconnect_WhenInfiniteReconnect_ShouldAlwaysReturnTrueForAttempts()
    {
        // -1 表示无限重连
        _options.MaxReconnectAttempts = -1;
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        // 即使尝试次数很大，也应该返回 true（仅受时间限制）
        var result = strategy.ShouldContinueReconnect(10000, TimeSpan.FromMinutes(1));

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldContinueReconnect_WhenInfiniteReconnectButExceedsTime_ShouldReturnFalse()
    {
        _options.MaxReconnectAttempts = -1;
        var strategy = new ExponentialBackoffReconnectStrategy(_options, _loggerMock.Object);

        var result = strategy.ShouldContinueReconnect(1, _options.MaxTotalReconnectTime + TimeSpan.FromMinutes(1));

        result.Should().BeFalse();
    }
}
