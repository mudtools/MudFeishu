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
/// ReconnectionOrchestrator 单元测试
/// </summary>
public class ReconnectionOrchestratorTests
{
    private readonly Mock<ILogger<ReconnectionOrchestrator>> _loggerMock;
    private readonly Mock<IReconnectStrategy> _strategyMock;
    private readonly Mock<IFeishuWebSocketManager> _managerMock;
    private readonly FeishuWebSocketOptions _options;

    public ReconnectionOrchestratorTests()
    {
        _loggerMock = new Mock<ILogger<ReconnectionOrchestrator>>();
        _strategyMock = new Mock<IReconnectStrategy>();
        _managerMock = new Mock<IFeishuWebSocketManager>();
        _options = new FeishuWebSocketOptions
        {
            AutoReconnect = true,
            MaxReconnectAttempts = 5,
            ReconnectDelayMs = 100,
            MaxReconnectDelayMs = 1000,
            MaxTotalReconnectTime = TimeSpan.FromMinutes(30),
            ReconnectCooldownTime = TimeSpan.FromMilliseconds(10)
        };
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        var action = () => new ReconnectionOrchestrator(
            null!,
            _strategyMock.Object,
            _managerMock.Object,
            _options);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullStrategy_ShouldThrowArgumentNullException()
    {
        var action = () => new ReconnectionOrchestrator(
            _loggerMock.Object,
            null!,
            _managerMock.Object,
            _options);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("strategy");
    }

    [Fact]
    public void Constructor_WithNullManager_ShouldThrowArgumentNullException()
    {
        var action = () => new ReconnectionOrchestrator(
            _loggerMock.Object,
            _strategyMock.Object,
            null!,
            _options);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("webSocketManager");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        var action = () => new ReconnectionOrchestrator(
            _loggerMock.Object,
            _strategyMock.Object,
            _managerMock.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        var orchestrator = new ReconnectionOrchestrator(
            _loggerMock.Object,
            _strategyMock.Object,
            _managerMock.Object,
            _options);

        orchestrator.Should().NotBeNull();
    }

    [Fact]
    public async Task TryReconnectAsync_WhenAutoReconnectDisabled_ShouldReturnFalse()
    {
        _options.AutoReconnect = false;

        var orchestrator = CreateOrchestrator();

        var result = await orchestrator.TryReconnectAsync("test");

        result.Should().BeFalse();
        _managerMock.Verify(x => x.ReconnectAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TryReconnectAsync_WhenReconnectSucceeds_ShouldReturnTrue()
    {
        _strategyMock.Setup(x => x.ShouldContinueReconnect(It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(true);
        _strategyMock.Setup(x => x.CalculateDelay(It.IsAny<int>()))
            .Returns(TimeSpan.FromMilliseconds(1));
        _managerMock.Setup(x => x.IsConnected).Returns(false);
        _managerMock.Setup(x => x.ReconnectAsync(It.IsAny<CancellationToken>()))
            .Callback(() => _managerMock.Setup(x => x.IsConnected).Returns(true))
            .Returns(Task.CompletedTask);

        var orchestrator = CreateOrchestrator();

        var result = await orchestrator.TryReconnectAsync("test");

        result.Should().BeTrue();
        _managerMock.Verify(x => x.ReconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryReconnectAsync_WhenReconnectFails_ShouldReturnFalse()
    {
        _strategyMock.Setup(x => x.ShouldContinueReconnect(It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(false);

        var orchestrator = CreateOrchestrator();

        var result = await orchestrator.TryReconnectAsync("test");

        result.Should().BeFalse();
        _managerMock.Verify(x => x.ReconnectAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TryReconnectAsync_WhenReconnectSucceeds_ShouldRaiseReconnectSucceededEvent()
    {
        _strategyMock.Setup(x => x.ShouldContinueReconnect(It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(true);
        _strategyMock.Setup(x => x.CalculateDelay(It.IsAny<int>()))
            .Returns(TimeSpan.FromMilliseconds(1));
        _managerMock.Setup(x => x.IsConnected).Returns(false);
        _managerMock.Setup(x => x.ReconnectAsync(It.IsAny<CancellationToken>()))
            .Callback(() => _managerMock.Setup(x => x.IsConnected).Returns(true))
            .Returns(Task.CompletedTask);

        var orchestrator = CreateOrchestrator();
        ReconnectSuccessEventArgs? eventArgs = null;
        orchestrator.ReconnectSucceeded += (sender, e) => eventArgs = e;

        await orchestrator.TryReconnectAsync("test");

        eventArgs.Should().NotBeNull();
        eventArgs!.AttemptCount.Should().Be(1);
        eventArgs.TotalReconnectCount.Should().Be(1);
    }

    [Fact]
    public async Task TryReconnectAsync_WhenStrategySaysStop_ShouldRaiseReconnectLimitReachedEvent()
    {
        _strategyMock.Setup(x => x.ShouldContinueReconnect(It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(false);

        var orchestrator = CreateOrchestrator();
        ReconnectLimitReachedEventArgs? eventArgs = null;
        orchestrator.ReconnectLimitReached += (sender, e) => eventArgs = e;

        await orchestrator.TryReconnectAsync("test");

        eventArgs.Should().NotBeNull();
        eventArgs!.TotalAttempts.Should().Be(1);
    }

    [Fact]
    public void ResetReconnectCounter_ShouldResetState()
    {
        var orchestrator = CreateOrchestrator();

        orchestrator.ResetReconnectCounter();

        var state = orchestrator.GetReconnectState();
        state.CurrentAttempt.Should().Be(0);
        state.ReconnectStartTime.Should().BeNull();
        state.LastError.Should().BeNull();
    }

    [Fact]
    public void GetReconnectState_ShouldReturnCurrentState()
    {
        var orchestrator = CreateOrchestrator();

        var state = orchestrator.GetReconnectState();

        state.Should().NotBeNull();
        state.IsReconnecting.Should().BeFalse();
        state.CurrentAttempt.Should().Be(0);
        state.TotalReconnectCount.Should().Be(0);
    }

    [Fact]
    public async Task TryReconnectAsync_WhenAlreadyReconnecting_ShouldReturnFalse()
    {
        var callCount = 0;
        _strategyMock.Setup(x => x.ShouldContinueReconnect(It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(() =>
            {
                callCount++;
                return callCount <= 3;
            });
        _strategyMock.Setup(x => x.CalculateDelay(It.IsAny<int>()))
            .Returns(TimeSpan.FromMilliseconds(10));
        _managerMock.Setup(x => x.ReconnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _managerMock.Setup(x => x.IsConnected).Returns(false);

        var orchestrator = CreateOrchestrator();

        var task1 = orchestrator.TryReconnectAsync("test1");
        await Task.Delay(5);
        var task2 = orchestrator.TryReconnectAsync("test2");

        var results = await Task.WhenAll(task1, task2);

        results[1].Should().BeFalse();
    }

    [Fact]
    public async Task TryReconnectAsync_WhenAlreadyConnected_ShouldReturnTrueWithoutReconnecting()
    {
        _managerMock.Setup(x => x.IsConnected).Returns(true);

        var orchestrator = CreateOrchestrator();

        var result = await orchestrator.TryReconnectAsync("test");

        result.Should().BeTrue();
        _managerMock.Verify(x => x.ReconnectAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var orchestrator = CreateOrchestrator();

        var action = () => orchestrator.Dispose();

        action.Should().NotThrow();
    }

    private ReconnectionOrchestrator CreateOrchestrator()
    {
        return new ReconnectionOrchestrator(
            _loggerMock.Object,
            _strategyMock.Object,
            _managerMock.Object,
            _options);
    }
}
