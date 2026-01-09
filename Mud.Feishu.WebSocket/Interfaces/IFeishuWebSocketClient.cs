
// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.WsEndpoint;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Net.WebSockets;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket客户端接口，提供连接管理、消息处理和事件订阅功能
/// </summary>
public interface IFeishuWebSocketClient : IDisposable
{
    /// <summary>
    /// WebSocket连接状态
    /// </summary>
    WebSocketState State { get; }

    /// <summary>
    /// 是否已认证
    /// </summary>
    bool IsAuthenticated { get; }

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
    /// 认证成功事件
    /// </summary>
    event EventHandler<EventArgs>? Authenticated;

    /// <summary>
    /// 接收到二进制消息事件
    /// </summary>
    event EventHandler<WebSocketBinaryMessageEventArgs>? BinaryMessageReceived;


    /// <summary>
    /// 建立WebSocket连接
    /// </summary>
    /// <param name="endpoint">WebSocket端点信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接任务</returns>
    Task ConnectAsync(WsEndpointResult endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// 建立WebSocket连接并进行认证
    /// </summary>
    /// <param name="endpoint">WebSocket端点信息</param>
    /// <param name="appAccessToken">应用访问令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接任务</returns>
    Task ConnectAsync(WsEndpointResult endpoint, string appAccessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开WebSocket连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>断开连接任务</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="message">要发送的消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送任务</returns>
    Task SendMessageAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 开始接收消息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接收任务</returns>
    Task StartReceivingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 注册消息处理器
    /// </summary>
    /// <param name="processor">消息处理器</param>
    void RegisterMessageProcessor(Func<string, Task> processor);

    /// <summary>
    /// 移除消息处理器
    /// </summary>
    /// <param name="processor">要移除的消息处理器</param>
    /// <returns>是否成功移除</returns>
    bool UnregisterMessageProcessor(Func<string, Task> processor);
}
