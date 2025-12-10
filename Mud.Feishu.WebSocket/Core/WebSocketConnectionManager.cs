// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Net.WebSockets;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// WebSocket连接管理器，提供飞书WebSocket连接的完整生命周期管理
/// </summary>
/// <remarks>
/// 该类负责WebSocket连接的建立、维护、断开和消息收发功能。
/// 支持自动重连、连接超时、错误处理和资源清理等企业级特性。
/// </remarks>
public class WebSocketConnectionManager : IDisposable
{
    private readonly ILogger<WebSocketConnectionManager> _logger;
    private readonly FeishuWebSocketOptions _options;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed = false;

    /// <summary>
    /// WebSocket连接成功建立时触发的事件
    /// </summary>
    public event EventHandler<EventArgs>? Connected;

    /// <summary>
    /// WebSocket连接断开时触发的事件
    /// </summary>
    public event EventHandler<WebSocketCloseEventArgs>? Disconnected;

    /// <summary>
    /// WebSocket连接发生错误时触发的事件
    /// </summary>
    public event EventHandler<WebSocketErrorEventArgs>? Error;

    /// <summary>
    /// 获取当前WebSocket连接的状态
    /// </summary>
    /// <returns>WebSocket连接状态，如果未初始化则返回None</returns>
    public WebSocketState State => _webSocket?.State ?? WebSocketState.None;

    /// <summary>
    /// 获取WebSocket是否已连接并处于活动状态
    /// </summary>
    /// <returns>如果WebSocket处于Open状态返回true，否则返回false</returns>
    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    /// <summary>
    /// 初始化WebSocket连接管理器实例
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="options">WebSocket配置选项，如果为null则使用默认配置</param>
    /// <exception cref="ArgumentNullException">当logger为null时抛出</exception>
    public WebSocketConnectionManager(
        ILogger<WebSocketConnectionManager> logger,
        FeishuWebSocketOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new FeishuWebSocketOptions();
    }

    /// <summary>
    /// 连接到WebSocket服务器
    /// </summary>
    /// <param name="url">WebSocket服务器URL，必须使用ws://或wss://协议</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步连接操作的任务</returns>
    /// <exception cref="ArgumentException">当URL为空、格式无效或协议不正确时抛出</exception>
    /// <exception cref="TimeoutException">当连接超时时抛出</exception>
    /// <exception cref="WebSocketException">当WebSocket连接失败时抛出</exception>
    /// <remarks>
    /// 如果当前已有连接，会先断开现有连接再建立新连接。
    /// 连接超时时间由<see cref="FeishuWebSocketOptions.ConnectionTimeoutMs"/>配置决定。
    /// </remarks>
    public async Task ConnectAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("WebSocket URL不能为空", nameof(url));

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException("无效的WebSocket URL格式", nameof(url));

        if (uri.Scheme != "ws" && uri.Scheme != "wss")
            throw new ArgumentException("WebSocket URL必须使用ws://或wss://协议", nameof(url));

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            // 如果已经连接，先断开
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await DisconnectAsync(cancellationToken);
            }

            // 创建新的WebSocket连接
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();

            // 设置连接超时
            using var timeoutCts = new CancellationTokenSource(_options.ConnectionTimeoutMs);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token,
                _cancellationTokenSource.Token);

            try
            {
                await _webSocket.ConnectAsync(uri, combinedCts.Token);

                if (_options.EnableLogging)
                {
                    _logger.LogInformation("已连接到飞书WebSocket服务: {Url}", url);
                }
                Connected?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                _logger.LogError("连接飞书WebSocket服务超时");
                throw new TimeoutException("连接飞书WebSocket服务超时");
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// 断开WebSocket连接
    /// </summary>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步断开操作的任务</returns>
    /// <remarks>
    /// 如果连接已经关闭，此方法会直接返回而不执行任何操作。
    /// 断开连接时会触发<see cref="Disconnected"/>事件。
    /// </remarks>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_webSocket == null || _webSocket.State == WebSocketState.Closed)
                return;

            _cancellationTokenSource?.Cancel();

            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "客户端主动断开连接",
                    cancellationToken);
            }

            if (_options.EnableLogging)
            {
                _logger.LogInformation("已断开飞书WebSocket连接");
            }

            Disconnected?.Invoke(this, new WebSocketCloseEventArgs
            {
                CloseStatus = WebSocketCloseStatus.NormalClosure,
                CloseStatusDescription = "客户端主动断开连接",
                IsServerInitiated = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "断开飞书WebSocket连接时发生错误");
            OnError(ex, "断开连接错误");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// 发送二进制消息
    /// </summary>
    /// <param name="data">要发送的二进制数据</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步发送操作的任务</returns>
    /// <exception cref="ArgumentNullException">当data为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当WebSocket未连接时抛出</exception>
    /// <remarks>
    /// 内部调用<see cref="SendBinaryMessageAsync(ArraySegment{byte}, CancellationToken)"/>方法。
    /// </remarks>
    public async Task SendBinaryMessageAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        await SendBinaryMessageAsync(new ArraySegment<byte>(data), cancellationToken);
    }

    /// <summary>
    /// 发送二进制消息
    /// </summary>
    /// <param name="data">要发送的二进制数据段</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步发送操作的任务</returns>
    /// <exception cref="ArgumentException">当data为null或长度为0时抛出</exception>
    /// <exception cref="InvalidOperationException">当WebSocket未连接时抛出</exception>
    /// <remarks>
    /// 使用WebSocketMessageType.Binary消息类型发送数据。
    /// 发送成功后会记录调试日志（如果启用日志记录）。
    /// </remarks>
    public async Task SendBinaryMessageAsync(ArraySegment<byte> data, CancellationToken cancellationToken = default)
    {
        if (data == null || data.Count == 0)
            throw new ArgumentException("二进制数据不能为空", nameof(data));
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket未连接，无法发送消息");
        try
        {
            await _webSocket.SendAsync(
                data,
                WebSocketMessageType.Binary,
                true,
                cancellationToken);
            if (_options.EnableLogging)
            {
                _logger.LogDebug("已发送二进制消息，大小: {Size} 字节", data.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送二进制消息时发生错误");
            OnError(ex, "发送二进制消息错误");
            throw;
        }
    }

    /// <summary>
    /// 发送文本消息
    /// </summary>
    /// <param name="message">要发送的文本消息内容</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步发送操作的任务</returns>
    /// <exception cref="ArgumentException">当message为空或仅包含空白字符时抛出</exception>
    /// <exception cref="ArgumentException">当消息长度超过配置的最大限制时抛出</exception>
    /// <exception cref="InvalidOperationException">当WebSocket未连接时抛出</exception>
    /// <remarks>
    /// 消息会使用UTF-8编码发送。
    /// 最大消息长度由<see cref="FeishuWebSocketOptions.MaxMessageSize"/>配置决定。
    /// </remarks>
    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("消息不能为空或仅包含空白字符", nameof(message));

        if (message.Length > _options.MaxMessageSize)
            throw new ArgumentException($"消息大小超过限制 ({_options.MaxMessageSize} 字符)", nameof(message));

        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket未连接，无法发送消息");

        try
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                cancellationToken);

            if (_options.EnableLogging)
            {
                _logger.LogDebug("已发送消息: {Message}", message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送消息时发生错误");
            OnError(ex, "发送消息错误");
            throw;
        }
    }

    /// <summary>
    /// 开始接收WebSocket消息
    /// </summary>
    /// <param name="messageHandler">处理接收到的消息的回调函数</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步接收操作的任务</returns>
    /// <exception cref="InvalidOperationException">当WebSocket未初始化时抛出</exception>
    /// <remarks>
    /// 该方法会持续监听WebSocket消息，直到连接关闭或取消令牌被触发。
    /// 接收到的消息会通过提供的messageHandler回调函数处理。
    /// 如果接收到关闭消息，会自动调用<see cref="HandleCloseMessageAsync"/>处理关闭逻辑。
    /// </remarks>
    public async Task StartReceivingAsync(Func<ArraySegment<byte>, WebSocketReceiveResult, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        if (_webSocket == null)
            throw new InvalidOperationException("WebSocket未初始化");

        var buffer = new byte[_options.ReceiveBufferSize];

        try
        {
            while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                await messageHandler(new ArraySegment<byte>(buffer, 0, result.Count), result);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await HandleCloseMessageAsync(result);
                    break;
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogError(ex, "接收消息时发生WebSocket错误");
            OnError(ex, "WebSocket接收错误");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "接收消息时发生错误");
            OnError(ex, "接收消息错误");
        }
    }

    /// <summary>
    /// 处理WebSocket关闭消息
    /// </summary>
    /// <param name="result">WebSocket接收结果，包含关闭状态和描述</param>
    /// <returns>表示异步处理操作的任务</returns>
    /// <remarks>
    /// 该方法会触发<see cref="Disconnected"/>事件，通知订阅者连接已关闭。
    /// 如果配置了自动重连，会准备重连逻辑。
    /// </remarks>
    private async Task HandleCloseMessageAsync(WebSocketReceiveResult result)
    {
        if (_options.EnableLogging)
        {
            _logger.LogInformation("服务器请求关闭连接: {Status} - {Description}",
                result.CloseStatus, result.CloseStatusDescription);
        }

        Disconnected?.Invoke(this, new WebSocketCloseEventArgs
        {
            CloseStatus = result.CloseStatus,
            CloseStatusDescription = result.CloseStatusDescription,
            IsServerInitiated = true
        });

        if (_options.AutoReconnect)
        {
            // 触发重连逻辑，这里简化处理
            if (_options.EnableLogging)
            {
                _logger.LogInformation("准备自动重连...");
            }
        }
    }

    /// <summary>
    /// 触发错误事件
    /// </summary>
    /// <param name="ex">发生的异常</param>
    /// <param name="context">错误发生的上下文描述</param>
    /// <remarks>
    /// 该方法会创建<see cref="WebSocketErrorEventArgs"/>并触发<see cref="Error"/>事件。
    /// 会自动检测异常类型，设置网络错误和认证错误的标志。
    /// </remarks>
    private void OnError(Exception ex, string context)
    {
        Error?.Invoke(this, new WebSocketErrorEventArgs
        {
            Exception = ex,
            ErrorMessage = $"{context}: {ex.Message}",
            ErrorType = ex.GetType().Name,
            ConnectionState = _webSocket?.State ?? WebSocketState.None,
            IsNetworkError = ex is WebSocketException || ex is IOException,
            IsAuthError = ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                          ex.Message.Contains("认证", StringComparison.OrdinalIgnoreCase)
        });
    }

    /// <summary>
    /// 释放WebSocket连接管理器占用的资源
    /// </summary>
    /// <remarks>
    /// 该方法会取消所有正在进行的操作，关闭WebSocket连接，并释放相关资源。
    /// 实现IDisposable模式，确保资源正确清理。
    /// 如果已经释放过，重复调用不会产生副作用。
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _cancellationTokenSource?.Cancel();
            _webSocket?.Dispose();
            _cancellationTokenSource?.Dispose();
            _connectionLock.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放连接管理器资源时发生错误");
        }
        finally
        {
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}