// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.WsEndpoint;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket客户端接口，提供连接管理、消息处理和事件订阅功能
/// </summary>
public interface IFeishuWebSocketClient : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// WebSocket连接状态
    /// </summary>
    WebSocketState State { get; }

    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }

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
    /// 接收到消息事件（**观测钩子**，非处理入口）
    /// </summary>
    /// <remarks>
    /// <b>线程契约（WS2-08 行为变更，请勿沿用旧假设）</b>：
    /// <list type="bullet">
    /// <item>本事件<b>不在接收循环线程上派发</b>，而是在取得并发租约后的处理任务体内派发；</item>
    /// <item>因此<b>可能并发执行</b>（受 <see cref="FeishuWebSocketOptions.MaxConcurrentHandlers"/> 约束），
    /// 且<b>不保证与帧到达顺序一致</b>；</item>
    /// <item>回调内可安全执行同步阻塞代码——它只会占用一个并发槽位（形成反压），
    /// 不会阻塞接收管道（这是本变更的目的）。</item>
    /// </list>
    /// <para>
    /// <b>需要顺序或需要受 <c>MessageHandlerTimeoutMs</c> 保护的业务处理时，请改用
    /// <c>IMessageHandler</c></b>（经 <c>MessageRouter</c> 分发）；本事件只应用于日志、指标等观测用途。
    /// </para>
    /// </remarks>
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
    /// <param name="cancellationToken">
    /// <b>仅约束建连阶段</b>（握手 + 认证），<b>不构成连接生命周期</b>（架构不变量 I15）：
    /// 连接成功后再取消该令牌不会中断接收循环或心跳。终止连接请调用
    /// <see cref="DisconnectAsync"/> 或 <c>DisposeAsync</c>。
    /// </param>
    /// <returns>连接任务</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    Task ConnectAsync(WsEndpointResult endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// 建立WebSocket连接并进行认证
    /// </summary>
    /// <param name="endpoint">WebSocket端点信息</param>
    /// <param name="appAccessToken">应用访问令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接任务</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
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
    /// 开始接收消息（**已弃用**：接收循环由 <see cref="ConnectAsync(WsEndpointResult, CancellationToken)"/> 统一管理）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接收任务</returns>
    /// <remarks>
    /// WS2-02 收口：未连接时抛 <see cref="InvalidOperationException"/>；已有接收循环在运行时为
    /// 幂等 no-op 并记录告警。需要立即恢复接收循环请调用
    /// <c>IFeishuWebSocketManager.ReconnectAsync</c>。
    /// </remarks>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    [Obsolete("接收循环由 ConnectAsync 统一管理，无需显式调用。若需重连请调用 IFeishuWebSocketManager.ReconnectAsync。")]
    Task StartReceivingAsync(CancellationToken cancellationToken = default);

}
