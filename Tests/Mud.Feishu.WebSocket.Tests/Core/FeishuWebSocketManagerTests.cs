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
using Mud.Feishu.Abstractions;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.WsEndpoint;
using System.Net.WebSockets;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// FeishuWebSocketManager 单元测试
/// </summary>
public class FeishuWebSocketManagerTests
{
    private readonly Mock<ILogger<FeishuWebSocketManager>> _loggerMock;
    private readonly Mock<IFeishuAppContext> _appContextMock;
    private readonly Mock<IFeishuWebSocketClient> _clientMock;
    private readonly FeishuWebSocketOptions _options;
    private readonly Mock<IOptionsMonitor<FeishuWebSocketOptions>> _optionsMonitorMock;

    public FeishuWebSocketManagerTests()
    {
        _loggerMock = new Mock<ILogger<FeishuWebSocketManager>>();
        _appContextMock = new Mock<IFeishuAppContext>();
        _clientMock = new Mock<IFeishuWebSocketClient>();
        _options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions { };
        _optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebSocketOptions>>();
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_options);

        SetupAppContextMock();
    }

    private void SetupAppContextMock()
    {
        var config = new Mud.Feishu.Abstractions.FeishuAppConfig { AppKey = "test_app_key", AppId = "cli_test_app_id", AppSecret = "test_app_secret_key_123", TimeoutSeconds = 30, HttpRetry = new Mud.Feishu.Abstractions.Configuration.HttpRetryOptions { MaxAttempts = 3, DelayMs = 1000 } };

        _appContextMock.Setup(x => x.Config).Returns(config);

        var authMock = new Mock<IFeishuAuthentication>();
        var wsEndpointResult = new WsEndpointResult
        {
            Url = "wss://test.feishu.com/ws"
        };
        var wsEndpointResponse = new FeishuApiResult<WsEndpointResult>
        {
            Code = 0,
            Msg = "success",
            Data = wsEndpointResult
        };
        authMock.Setup(x => x.GetWebSocketEndpointAsync(It.IsAny<WsAppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(wsEndpointResponse);

        _appContextMock.Setup(x => x.Authentication).Returns(authMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuWebSocketManager(
            null!,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullAppContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuWebSocketManager(
            _loggerMock.Object,
            null!,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("appContext");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            null!,
            _clientMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("webSocketOptions");
    }

    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("webSocketClient");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Assert
        manager.Should().NotBeNull();
        manager.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Client_ShouldReturnWebSocketClient()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act
        var client = manager.Client;

        // Assert
        client.Should().NotBeNull();
        client.Should().Be(_clientMock.Object);
    }

    [Fact]
    public void IsConnected_WhenClientNotConnected_ShouldReturnFalse()
    {
        // Arrange
        _clientMock.Setup(x => x.State).Returns(WebSocketState.None);
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act & Assert
        manager.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void IsConnected_WhenClientConnected_ShouldReturnTrue()
    {
        // Arrange
        _clientMock.Setup(x => x.State).Returns(WebSocketState.Open);
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act & Assert
        manager.IsConnected.Should().BeTrue();
    }

    [Fact]
    public void GetConnectionStats_WhenNotStarted_ShouldReturnZeroStats()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act
        var stats = manager.GetConnectionStats();

        // Assert
        stats.Uptime.Should().Be(TimeSpan.Zero);
        stats.ReconnectCount.Should().Be(0);
        stats.LastError.Should().BeNull();
    }

    [Fact]
    public void GetConnectionState_WhenNotConnected_ShouldReturnDisconnectedState()
    {
        // Arrange
        _clientMock.Setup(x => x.State).Returns(WebSocketState.Closed);
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act
        var state = manager.GetConnectionState();

        // Assert
        state.IsConnected.Should().BeFalse();
        state.State.Should().Be(WebSocketState.Closed);
    }

    [Fact]
    public void GetConnectionState_WhenConnected_ShouldReturnConnectedState()
    {
        // Arrange
        _clientMock.Setup(x => x.State).Returns(WebSocketState.Open);
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act
        var state = manager.GetConnectionState();

        // Assert
        state.IsConnected.Should().BeTrue();
        state.State.Should().Be(WebSocketState.Open);
    }

    [Fact]
    public async Task SendMessageAsync_WhenNotConnected_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _clientMock.Setup(x => x.State).Returns(WebSocketState.Closed);
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act & Assert
        var action = () => manager.SendMessageAsync("test message");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*未连接*");
    }

    [Fact]
    public async Task SendMessageAsync_WhenConnected_ShouldCallClientSendMessage()
    {
        // Arrange
        _clientMock.Setup(x => x.State).Returns(WebSocketState.Open);
        _clientMock.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act
        await manager.SendMessageAsync("test message");

        // Assert
        _clientMock.Verify(x => x.SendMessageAsync("test message", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_ShouldReturnWithoutError()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act & Assert
        var action = () => manager.StopAsync();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public void Connected_Event_CanBeSubscribed()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);
        var eventRaised = false;
        manager.Connected += (s, e) => eventRaised = true;

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void Disconnected_Event_CanBeSubscribed()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);
        var eventRaised = false;
        manager.Disconnected += (s, e) => eventRaised = true;

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void MessageReceived_Event_CanBeSubscribed()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);
        var eventRaised = false;
        manager.MessageReceived += (s, e) => eventRaised = true;

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void Error_Event_CanBeSubscribed()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);
        var eventRaised = false;
        manager.Error += (s, e) => eventRaised = true;

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void Dispose_ShouldNotThrowWhenCalledMultipleTimes()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act & Assert
        var action = () =>
        {
            manager.Dispose();
            manager.Dispose();
            manager.Dispose();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldDisposeClient()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act
        manager.Dispose();

        // Assert
        _clientMock.Verify(x => x.Dispose(), Times.Once);
    }

    #region WS2-06 / I9：_startStopLock 不得随 Dispose 释放

    /// <summary>
    /// WS2-06 ①（I9）：释放后再次进入公开入口不得抛 <see cref="ObjectDisposedException"/>。
    /// </summary>
    /// <remarks>
    /// 可观测契约形式：<c>_startStopLock</c> 是 private，仅反射读字段无法证明"未释放"。
    /// 而 <see cref="FeishuWebSocketManager.StopAsync"/> 的第一件事就是 <c>_startStopLock.WaitAsync(...)</c>——
    /// **若信号量已被释放，该调用必然抛 <see cref="ObjectDisposedException"/>**；
    /// 反之（未释放）在 <c>_isRunning == false</c> 时它会直接返回。
    /// 因此"Dispose 后再调 StopAsync 不抛 ODE"是等价且强的断言。
    /// </remarks>
    [Fact]
    public async Task DisposeAsync_ShouldNotDisposeStartStopLock()
    {
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        await manager.DisposeAsync();

        var act = async () => await manager.StopAsync();

        await act.Should().NotThrowAsync<ObjectDisposedException>(
            "I9：信号量不随 Dispose 释放（未访问 AvailableWaitHandle，无 OS 句柄泄漏；释放会与在途 WaitAsync/Release 构成竞态）");
    }

    [Fact]
    public async Task Dispose_ShouldNotDisposeStartStopLock()
    {
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        manager.Dispose();

        var act = async () => await manager.StopAsync();

        await act.Should().NotThrowAsync<ObjectDisposedException>("I9：同步释放路径同样不得释放该信号量");
    }

    #endregion

    #region WS2-05 / D5：入站报文不得全文入日志

    /// <summary>
    /// WS2-05（D5）：<c>OnClientMessageReceived</c> 的日志必须是"结构化字段 + 脱敏截断预览"。
    /// </summary>
    [Fact]
    public void OnClientMessageReceived_ShouldLogSanitizedPreview_WhenMessageContainsPii()
    {
        // Arrange：构造一个含敏感键值 + PII 的报文（旧实现会把整串打进 Debug 日志）
        const string secret = "sensitive-ticket-value-9f8e7d6c";
        const string pii = "13800138000";
        var payload = $"{{\"ticket\":\"{secret}\",\"header\":{{\"token\":\"t-123\"}},\"mobile\":\"{pii}\"}}";

        var logger = new CapturingLogger<FeishuWebSocketManager>();

        var manager = new FeishuWebSocketManager(
            logger,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act：经客户端事件触发（与生产路径一致）
        _clientMock.Raise(
            x => x.MessageReceived += null,
            _clientMock.Object,
            new Mud.Feishu.WebSocket.SocketEventArgs.WebSocketMessageEventArgs
            {
                Message = payload,
                MessageType = WebSocketMessageType.Text,
                EndOfMessage = true,
                MessageSize = payload.Length
            });

        // Assert
        logger.Messages.Should().NotBeEmpty("OnClientMessageReceived 必须产生日志（用于排障）");
        logger.Messages.Should().Contain(line => line.Contains("长度="),
            "D5：日志必须包含**结构化字段**（长度），而不是只留一句无信息量的摘要");

        logger.Messages.Should().NotContain(line => line.Contains(payload),
            "D5：不得把入站报文全文写入日志");
    }

    /// <summary>
    /// 日志捕获替身：直接实现 <see cref="ILogger{T}"/>，避免 Moq 的 callbacks 泛型管道噪音。
    /// </summary>
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        /// <summary>已记录的日志文本（已由 formatter 渲染，含占位符替换结果）。</summary>
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (Messages)
            {
                Messages.Add(formatter(state, exception));
            }
        }
    }

    #endregion

    #region 误报守护：重连期不得发布 Disconnected

    /// <summary>
    /// 误报守护（附录 B.2 #3）：重连过程中的主动断开必须被抑制，不得转发 <c>Disconnected</c>。
    /// </summary>
    /// <remarks>
    /// 该抑制窗口是"防重连风暴"的关键：<c>ReconnectAsync → DisconnectAsync → Disconnected → 触发新重连</c>
    /// 会形成级联。R1 声称已修复但无回归用例，本用例补上。
    /// <para>
    /// 实现要点：<c>DisconnectAsync</c> 必须返回**未完成**的任务，让 <c>ReconnectAsync</c> 悬停在
    /// <c>await</c> 上（即"重连仍在进行中"），否则抑制窗口尚未被观测就已关闭，用例会恒假通过。
    /// </para>
    /// </remarks>
    [Fact]
    public async Task ReconnectAsync_ShouldNotPublishDisconnected_WhenReconnecting()
    {
        // Arrange
        var disconnectGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _clientMock.Setup(x => x.State).Returns(WebSocketState.Open);
        _clientMock.Setup(x => x.DisconnectAsync(It.IsAny<CancellationToken>()))
            .Returns(disconnectGate.Task);

        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        var disconnectedCount = 0;
        manager.Disconnected += (_, _) => Interlocked.Increment(ref disconnectedCount);

        // Act：重连先断旧连接（此处悬停），期间客户端上报 Disconnected 必须被抑制
        var reconnectTask = manager.ReconnectAsync();
        reconnectTask.IsCompleted.Should().BeFalse("前置条件：重连必须仍在进行中（悬停在 DisconnectAsync 上）");

        _clientMock.Raise(
            x => x.Disconnected += null,
            _clientMock.Object,
            new Mud.Feishu.WebSocket.SocketEventArgs.WebSocketCloseEventArgs
            {
                CloseStatus = WebSocketCloseStatus.NormalClosure,
                CloseStatusDescription = "客户端主动断开连接",
                IsServerInitiated = false
            });

        disconnectedCount.Should().Be(0,
            "重连窗口内必须抑制 Disconnected 转发，否则会形成 Reconnect→Disconnect→Disconnected→Reconnect 级联风暴");

        // 收尾：放行并观察异常（本替身环境下 StartAsync 必然失败，与断言无关）
        disconnectGate.TrySetResult(true);
        try
        {
            await reconnectTask;
        }
        catch
        {
            // 忽略：仅用于避免 UnobservedTaskException
        }
    }

    /// <summary>
    /// 抑制窗口之外：<c>Disconnected</c> 必须正常转发（防止"把抑制写成永久吞掉"的反向回归）。
    /// </summary>
    [Fact]
    public void OnClientDisconnected_ShouldPublishDisconnected_WhenNotReconnecting()
    {
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        var published = 0;
        manager.Disconnected += (_, _) => Interlocked.Increment(ref published);

        _clientMock.Raise(
            x => x.Disconnected += null,
            _clientMock.Object,
            new Mud.Feishu.WebSocket.SocketEventArgs.WebSocketCloseEventArgs
            {
                CloseStatus = WebSocketCloseStatus.EndpointUnavailable,
                CloseStatusDescription = "服务端不可达",
                IsServerInitiated = true
            });

        published.Should().Be(1, "非重连期（抑制窗口之外）的断线必须如实转发");
    }

    #endregion
}
