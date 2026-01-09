// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.WebSocket.SocketEventArgs;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket管理器接口，用于管理WebSocket连接的生命周期
/// </summary>
public interface IFeishuWebSocketManager : IDisposable
{
    /// <summary>
    /// WebSocket客户端实例
    /// </summary>
    IFeishuWebSocketClient Client { get; }

    /// <summary>
    /// 连接状态
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 连接建立事件
    /// </summary>
    event EventHandler<EventArgs>? Connected;

    /// <summary>
    /// 连接断开事件
    /// </summary>
    event EventHandler<WebSocketCloseEventArgs>? Disconnected;

    /// <summary>
    /// 接收到消息事件
    /// </summary>
    event EventHandler<WebSocketMessageEventArgs>? MessageReceived;

    /// <summary>
    /// 连接错误事件
    /// </summary>
    event EventHandler<WebSocketErrorEventArgs>? Error;

    /// <summary>
    /// 启动WebSocket连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启动任务</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止WebSocket连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>停止任务</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="message">要发送的消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送任务</returns>
    Task SendMessageAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重新连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重连任务</returns>
    Task ReconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    /// <returns>连接统计信息</returns>
    (TimeSpan Uptime, int ReconnectCount, Exception? LastError) GetConnectionStats();

    /// <summary>
    /// 获取连接状态详情
    /// </summary>
    /// <returns>连接状态详情</returns>
    WebSocketConnectionState GetConnectionState();
}
