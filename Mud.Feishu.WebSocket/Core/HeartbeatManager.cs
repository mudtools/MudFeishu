// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.DataModels;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Net.WebSockets;
using System.Text.Json;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 心跳管理器 - 负责WebSocket心跳检测和超时处理
/// </summary>
public class HeartbeatManager
{
    private readonly ILogger<HeartbeatManager> _logger;
    private readonly FeishuWebSocketOptions _options;
    private readonly Func<string, Task> _sendMessageCallback;
    private readonly Func<bool> _isConnectedCallback;

    private DateTime _lastPongTime = DateTime.MinValue;
    private int _heartbeatMissedCount = 0;
    private readonly object _heartbeatLock = new();

    /// <summary>
    /// 心跳超时阈值，连续超过此次数将触发重连
    /// </summary>
    private const int HeartbeatTimeoutThreshold = 3;

    /// <summary>
    /// 心跳超时事件，当连续心跳超时达到阈值时触发
    /// </summary>
    public event EventHandler<WebSocketCloseEventArgs>? HeartbeatTimeout;

    /// <summary>
    /// 连接断开事件（心跳检测到连接断开时触发）
    /// </summary>
    public event EventHandler<WebSocketCloseEventArgs>? ConnectionLost;

    /// <summary>
    /// 初始化心跳管理器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">WebSocket配置选项</param>
    /// <param name="sendMessageCallback">发送消息回调</param>
    /// <param name="isConnectedCallback">检查连接状态回调</param>
    public HeartbeatManager(
        ILogger<HeartbeatManager> logger,
        FeishuWebSocketOptions options,
        Func<string, Task> sendMessageCallback,
        Func<bool> isConnectedCallback)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _sendMessageCallback = sendMessageCallback ?? throw new ArgumentNullException(nameof(sendMessageCallback));
        _isConnectedCallback = isConnectedCallback ?? throw new ArgumentNullException(nameof(isConnectedCallback));
    }

    /// <summary>
    /// 启动心跳循环
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task StartHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            lock (_heartbeatLock)
            {
                _lastPongTime = DateTime.UtcNow;
                _heartbeatMissedCount = 0;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_options.HeartbeatIntervalMs, cancellationToken);

                if (!_isConnectedCallback())
                {
                    if (_options.AutoReconnect)
                    {
                        _logger.LogDebug("连接已断开，触发重连事件...");
                        ConnectionLost?.Invoke(this, new WebSocketCloseEventArgs
                        {
                            CloseStatus = WebSocketCloseStatus.NormalClosure,
                            CloseStatusDescription = "心跳检测到连接断开，准备重连",
                            IsServerInitiated = false,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    lock (_heartbeatLock)
                    {
                        _lastPongTime = DateTime.UtcNow;
                        _heartbeatMissedCount = 0;
                    }
                    continue;
                }

                try
                {
                    var pingMessage = new PingMessage
                    {
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    var heartbeatMessage = JsonSerializer.Serialize(pingMessage, JsonOptions.Default);
                    await _sendMessageCallback(heartbeatMessage);

                    if (_options.EnableLogging)
                        _logger.LogDebug("已发送心跳");

                    CheckHeartbeatTimeout();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发送心跳时发生错误");

                    if (_options.AutoReconnect)
                    {
                        _logger.LogWarning("心跳发送失败，触发心跳超时事件...");
                        HeartbeatTimeout?.Invoke(this, new WebSocketCloseEventArgs
                        {
                            CloseStatus = WebSocketCloseStatus.EndpointUnavailable,
                            CloseStatusDescription = "心跳发送失败，触发重连",
                            IsServerInitiated = false,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// 通知收到Pong响应，重置心跳超时计数
    /// </summary>
    public void OnPongReceived()
    {
        lock (_heartbeatLock)
        {
            _lastPongTime = DateTime.UtcNow;
            _heartbeatMissedCount = 0;
        }

        if (_options.EnableLogging)
            _logger.LogDebug("已更新最后一次Pong时间");
    }

    /// <summary>
    /// 检查心跳超时
    /// </summary>
    private void CheckHeartbeatTimeout()
    {
        bool shouldTriggerReconnect = false;
        int currentMissedCount = 0;
        double timeSinceLastPongMs = 0;

        lock (_heartbeatLock)
        {
            timeSinceLastPongMs = (DateTime.UtcNow - _lastPongTime).TotalMilliseconds;
            var heartbeatTimeoutMs = _options.HeartbeatIntervalMs * 2;

            if (timeSinceLastPongMs > heartbeatTimeoutMs)
            {
                _heartbeatMissedCount++;
                currentMissedCount = _heartbeatMissedCount;

                if (_heartbeatMissedCount >= HeartbeatTimeoutThreshold && _options.AutoReconnect)
                {
                    shouldTriggerReconnect = true;
                }
            }
            else
            {
                _heartbeatMissedCount = 0;
            }
        }

        if (currentMissedCount > 0)
        {
            _logger.LogWarning("心跳超时：{TimeSinceLastPong}ms 未收到响应，超时次数：{MissedCount}",
                timeSinceLastPongMs, currentMissedCount);
        }

        if (shouldTriggerReconnect)
        {
            _logger.LogError("连续 {MissedCount} 次心跳超时，触发重连", currentMissedCount);
            HeartbeatTimeout?.Invoke(this, new WebSocketCloseEventArgs
            {
                CloseStatus = WebSocketCloseStatus.EndpointUnavailable,
                CloseStatusDescription = "心跳超时，触发重连",
                IsServerInitiated = false,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
