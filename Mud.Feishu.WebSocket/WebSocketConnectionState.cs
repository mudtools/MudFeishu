// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net.WebSockets;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// WebSocket连接状态详细信息
/// </summary>
public record WebSocketConnectionState
{
    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// WebSocket状态
    /// </summary>
    public WebSocketState State { get; private set; }

    /// <summary>
    /// 连接建立时间
    /// </summary>
    public DateTime ConnectedTime { get; private set; } = DateTime.MinValue;

    /// <summary>
    /// 连接持续时间
    /// </summary>
    public TimeSpan ConnectionDuration => ConnectedTime == DateTime.MinValue
        ? TimeSpan.Zero
        : DateTime.UtcNow - ConnectedTime;

    /// <summary>
    /// 重连次数
    /// </summary>
    public int ReconnectCount { get; set; }

    /// <summary>
    /// 最后一次错误
    /// </summary>
    public Exception? LastError { get; set; }

    /// <summary>
    /// 最后一次错误时间
    /// </summary>
    public DateTime? LastErrorTime { get; private set; }

    /// <summary>
    /// 是否正在重连
    /// </summary>
    public bool IsReconnecting { get; private set; }

    /// <summary>
    /// 创建已连接状态
    /// </summary>
    public static WebSocketConnectionState Connected(DateTime connectedTime, int reconnectCount = 0) =>
        new()
        {
            IsConnected = true,
            State = WebSocketState.Open,
            ConnectedTime = connectedTime,
            ReconnectCount = reconnectCount
        };

    /// <summary>
    /// 创建断开连接状态
    /// </summary>
    public static WebSocketConnectionState Disconnected(Exception? lastError = null) =>
        new()
        {
            IsConnected = false,
            State = WebSocketState.Closed,
            LastError = lastError,
            LastErrorTime = lastError != null ? DateTime.UtcNow : null
        };

    /// <summary>
    /// 创建连接中状态
    /// </summary>
    public static WebSocketConnectionState Connecting =>
        new()
        {
            IsConnected = false,
            State = WebSocketState.Connecting,
            IsReconnecting = false
        };

    /// <summary>
    /// 创建重连中状态
    /// </summary>
    public static WebSocketConnectionState Reconnecting =>
        new()
        {
            IsConnected = false,
            State = WebSocketState.Connecting,
            IsReconnecting = true
        };
}