// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.WebSocket;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Net.WebSockets;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// FeishuWebSocketHostedService 单元测试
/// </summary>
public class FeishuWebSocketHostedServiceTests
{
    private readonly Mock<ILogger<FeishuWebSocketHostedService>> _loggerMock;
    private readonly Mock<IFeishuWebSocketManager> _managerMock;
    private readonly Mock<IReconnectionOrchestrator> _orchestratorMock;
    private readonly FeishuWebSocketOptions _options;
    private readonly Mock<IOptionsMonitor<FeishuWebSocketOptions>> _optionsMonitorMock;

    public FeishuWebSocketHostedServiceTests()
    {
        _loggerMock = new Mock<ILogger<FeishuWebSocketHostedService>>();
        _managerMock = new Mock<IFeishuWebSocketManager>();
        _orchestratorMock = new Mock<IReconnectionOrchestrator>();
        _options = new FeishuWebSocketOptions
        {
            Reconnect = new WebSocketReconnectOptions
            {
                Auto = true,
                MaxAttempts = 3,
                BaseDelayMs = 1000,
                MaxDelayMs = 5000
            },
            HealthCheckIntervalMs = 60000,
            HeartbeatIntervalMs = 30000
        };
        _optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebSocketOptions>>();
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_options);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        var action = () => new FeishuWebSocketHostedService(
            null!,
            _managerMock.Object,
            _orchestratorMock.Object,
            _optionsMonitorMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullManager_ShouldThrowArgumentNullException()
    {
        var action = () => new FeishuWebSocketHostedService(
            _loggerMock.Object,
            null!,
            _orchestratorMock.Object,
            _optionsMonitorMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("webSocketManager");
    }

    [Fact]
    public void Constructor_WithNullOrchestrator_ShouldThrowArgumentNullException()
    {
        var action = () => new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            null!,
            _optionsMonitorMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("reconnectionOrchestrator");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        var action = () => new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            _orchestratorMock.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            _orchestratorMock.Object,
            _optionsMonitorMock.Object);

        service.Should().NotBeNull();
    }

    [Fact]
    public void GetConnectionStats_ShouldReturnManagerStats()
    {
        var expectedStats = (TimeSpan.FromMinutes(10), 2, (Exception?)null);
        _managerMock.Setup(x => x.GetConnectionStats()).Returns(expectedStats);

        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            _orchestratorMock.Object,
            _optionsMonitorMock.Object);

        var stats = service.GetConnectionStats();

        stats.Should().Be(expectedStats);
        _managerMock.Verify(x => x.GetConnectionStats(), Times.Once);
    }

    [Fact]
    public void GetConnectionState_ShouldReturnManagerState()
    {
        var expectedState = WebSocketConnectionState.Connected(DateTime.UtcNow, 0);
        _managerMock.Setup(x => x.GetConnectionState()).Returns(expectedState);

        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            _orchestratorMock.Object,
            _optionsMonitorMock.Object);

        var state = service.GetConnectionState();

        state.Should().Be(expectedState);
        _managerMock.Verify(x => x.GetConnectionState(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldCallManagerStopAsync()
    {
        _managerMock.Setup(x => x.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            _orchestratorMock.Object,
            _optionsMonitorMock.Object);

        await service.StopAsync(CancellationToken.None);

        _managerMock.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Dispose_ShouldNotThrowWhenCalledMultipleTimes()
    {
        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            _orchestratorMock.Object,
            _optionsMonitorMock.Object);

        var action = () =>
        {
            service.Dispose();
            service.Dispose();
            service.Dispose();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ShouldSubscribeToManagerEvents()
    {
        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            _orchestratorMock.Object,
            _optionsMonitorMock.Object);

        _managerMock.VerifyAdd(x => x.Connected += It.IsAny<EventHandler<EventArgs>>(), Times.Once);
        _managerMock.VerifyAdd(x => x.Disconnected += It.IsAny<EventHandler<WebSocketCloseEventArgs>>(), Times.Once);
        _managerMock.VerifyAdd(x => x.Error += It.IsAny<EventHandler<WebSocketErrorEventArgs>>(), Times.Once);
    }

    [Fact]
    public void Constructor_ShouldSubscribeToOrchestratorEvents()
    {
        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            _orchestratorMock.Object,
            _optionsMonitorMock.Object);

        _orchestratorMock.VerifyAdd(x => x.ReconnectSucceeded += It.IsAny<EventHandler<ReconnectSuccessEventArgs>>(), Times.Once);
        _orchestratorMock.VerifyAdd(x => x.ReconnectFailed += It.IsAny<EventHandler<ReconnectFailedEventArgs>>(), Times.Once);
        _orchestratorMock.VerifyAdd(x => x.ReconnectLimitReached += It.IsAny<EventHandler<ReconnectLimitReachedEventArgs>>(), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldUnsubscribeFromManagerEvents()
    {
        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            _orchestratorMock.Object,
            _optionsMonitorMock.Object);

        service.Dispose();

        _managerMock.VerifyRemove(x => x.Connected -= It.IsAny<EventHandler<EventArgs>>(), Times.Once);
        _managerMock.VerifyRemove(x => x.Disconnected -= It.IsAny<EventHandler<WebSocketCloseEventArgs>>(), Times.Once);
        _managerMock.VerifyRemove(x => x.Error -= It.IsAny<EventHandler<WebSocketErrorEventArgs>>(), Times.Once);
    }

    #region WS2-04 / I16：重连窗口必须钳制

    /// <summary>
    /// 超长 <c>Reconnect.TotalBudget</c> 不得让重连"完全不执行"（WS2-04 / I16）。
    /// </summary>
    /// <remarks>
    /// <b>为什么取 100 天</b>：这条用例的取值决定它是不是"修复前为红"。
    /// <c>CancellationTokenSource(TimeSpan)</c> 的上界**不是**按 <c>int</c> 推断的 24.8 天——
    /// .NET Core 实测上界为 <c>uint.MaxValue-1</c> ≈ **49.7 天**（24.8 天是 .NET Framework 实现）。
    /// 若取 30 天，未钳制的实现**不会**抛异常，用例恒绿（假绿）。取 100 天才能同时越过两条实现路径的上界。
    /// <para>
    /// 可观测契约：异常发生在本方法内的 fire-and-forget 任务体里并被通用 <c>catch</c> 吞掉
    /// ⇒ 编排器**一次都不会被调用**。因此"编排器被调用"就是钳制生效的端到端证据。
    /// </para>
    /// </remarks>
    [Fact]
    public async Task TryTriggerReconnect_ShouldClampWindow_WhenTotalBudgetExceedsCancellationTokenSourceLimit()
    {
        // Arrange
        _options.Reconnect.TotalBudget = TimeSpan.FromDays(100);

        var reconnected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _orchestratorMock
            .Setup(x => x.TryReconnectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((_, _) =>
            {
                reconnected.TrySetResult(true);
                return Task.FromResult(true);
            });

        using var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            _orchestratorMock.Object,
            _optionsMonitorMock.Object);

        // Act：经"连接断开事件"触发重连窗口构造（TryTriggerReconnect 为 private，事件是其唯一入口之一）
        _managerMock.Raise(
            x => x.Disconnected += null,
            _managerMock.Object,
            new WebSocketCloseEventArgs
            {
                CloseStatus = WebSocketCloseStatus.EndpointUnavailable,
                CloseStatusDescription = "测试触发",
                IsServerInitiated = true
            });

        // Assert
        var act = async () => await reconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await act.Should().NotThrowAsync(
            "超长预算必须被钳制到 CancellationTokenSource 的可表示区间——" +
            "否则 CTS 构造抛出的异常会被 fire-and-forget 的通用 catch 吞掉，本轮重连**完全不执行**");
    }

    /// <summary>
    /// 常规预算（30 分钟）同样必须正常触发（防止钳制改动把正常路径改坏）。
    /// </summary>
    [Fact]
    public async Task TryTriggerReconnect_ShouldTrigger_WhenTotalBudgetIsWithinBounds()
    {
        _options.Reconnect.TotalBudget = TimeSpan.FromMinutes(30);

        var reconnected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _orchestratorMock
            .Setup(x => x.TryReconnectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((_, _) =>
            {
                reconnected.TrySetResult(true);
                return Task.FromResult(true);
            });

        using var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            _orchestratorMock.Object,
            _optionsMonitorMock.Object);

        _managerMock.Raise(
            x => x.Disconnected += null,
            _managerMock.Object,
            new WebSocketCloseEventArgs
            {
                CloseStatus = WebSocketCloseStatus.NormalClosure,
                CloseStatusDescription = "测试触发",
                IsServerInitiated = true
            });

        var act = async () => await reconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await act.Should().NotThrowAsync();
    }

    #endregion

    [Fact]
    public void Dispose_ShouldUnsubscribeFromOrchestratorEvents()
    {
        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            _orchestratorMock.Object,
            _optionsMonitorMock.Object);

        service.Dispose();

        _orchestratorMock.VerifyRemove(x => x.ReconnectSucceeded -= It.IsAny<EventHandler<ReconnectSuccessEventArgs>>(), Times.Once);
        _orchestratorMock.VerifyRemove(x => x.ReconnectFailed -= It.IsAny<EventHandler<ReconnectFailedEventArgs>>(), Times.Once);
        _orchestratorMock.VerifyRemove(x => x.ReconnectLimitReached -= It.IsAny<EventHandler<ReconnectLimitReachedEventArgs>>(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldStopManagerAndDispose()
    {
        _managerMock.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _managerMock.Setup(x => x.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _managerMock.Setup(x => x.IsConnected).Returns(true);

        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            _orchestratorMock.Object,
            _optionsMonitorMock.Object);

        await service.StartAsync(CancellationToken.None);

        await Task.Delay(50);

        await service.StopAsync(CancellationToken.None);

        _managerMock.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }
}
