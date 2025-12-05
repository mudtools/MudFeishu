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
/// WebSocket连接管理器
/// </summary>
public class WebSocketConnectionManager : IDisposable
{
    private readonly ILogger<WebSocketConnectionManager> _logger;
    private readonly FeishuWebSocketOptions _options;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed = false;

    public event EventHandler<EventArgs>? Connected;
    public event EventHandler<WebSocketCloseEventArgs>? Disconnected;
    public event EventHandler<WebSocketErrorEventArgs>? Error;

    public WebSocketState State => _webSocket?.State ?? WebSocketState.None;
    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

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

                _logger.LogInformation("已连接到飞书WebSocket服务: {Url}", url);
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

            _logger.LogInformation("已断开飞书WebSocket连接");

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
    /// 发送消息
    /// </summary>
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

            _logger.LogDebug("已发送消息: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送消息时发生错误");
            OnError(ex, "发送消息错误");
            throw;
        }
    }

    /// <summary>
    /// 开始接收消息
    /// </summary>
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
    /// 处理关闭消息
    /// </summary>
    private async Task HandleCloseMessageAsync(WebSocketReceiveResult result)
    {
        _logger.LogInformation("服务器请求关闭连接: {Status} - {Description}",
            result.CloseStatus, result.CloseStatusDescription);

        Disconnected?.Invoke(this, new WebSocketCloseEventArgs
        {
            CloseStatus = result.CloseStatus,
            CloseStatusDescription = result.CloseStatusDescription,
            IsServerInitiated = true
        });

        if (_options.AutoReconnect)
        {
            // 触发重连逻辑，这里简化处理
            _logger.LogInformation("准备自动重连...");
        }
    }

    /// <summary>
    /// 触发错误事件
    /// </summary>
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

    /// <inheritdoc/>
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
    }
}