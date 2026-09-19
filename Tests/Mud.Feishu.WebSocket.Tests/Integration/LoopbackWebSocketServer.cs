// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET8_0_OR_GREATER

using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace Mud.Feishu.WebSocket.Tests.Integration;

// 说明：本命名空间位于 Mud.Feishu.WebSocket.* 之下，裸写 WebSocket 会绑定到外层命名空间
// Mud.Feishu.WebSocket（CS0118），故在命名空间内声明别名（编译单元级别名仍会被外层命名空间抢先匹配）。
using WebSocket = System.Net.WebSockets.WebSocket;

/// <summary>
/// 回环 WebSocket 测试服务端（W5 测试基建）。
/// </summary>
/// <remarks>
/// 目的：摆脱"<c>ClientWebSocket</c> 为 sealed、只能靠反射伪造状态"的测试瓶颈，用真实握手覆盖
/// 连接/关闭/分片重组等只能在真实协议交互中验证的路径。
/// <para>
/// 仅启用 <c>net8.0+</c>（条件编译），并以 <c>Category=Integration</c> 标记，便于在受限 CI 环境按类过滤。
/// </para>
/// </remarks>
internal sealed class LoopbackWebSocketServer : IAsyncDisposable
{
    /// <summary>
    /// 服务端行为模式。
    /// </summary>
    public enum ServerMode
    {
        /// <summary>接受连接后只完成关闭握手（等待客户端主动关闭）。</summary>
        Idle = 0,

        /// <summary>接受连接后立即主动下发关闭帧（用于验证"服务端主动关闭"路径）。</summary>
        CloseImmediately = 1,

        /// <summary>接受连接后把一条文本消息拆成两个分片发送（用于验证分片重组）。</summary>
        SendFragmentedText = 2,

        /// <summary>
        /// 接受连接后读到客户端的关闭帧但<b>不应答</b>关闭握手（保持 TCP 半开），
        /// 用于验证客户端"服务端不应答关闭"时必须依靠超时 + Abort 收尾（P2-11）。
        /// </summary>
        IgnoreCloseHandshake = 3
    }

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _acceptLoop;
    private readonly string _fragmentFirst;
    private readonly string _fragmentSecond;

    // P2-14 集成断言用：捕获服务端收到的完整二进制帧（ACK 载荷）
    private readonly object _receivedFramesLock = new();
    private readonly List<byte[]> _receivedBinaryFrames = new();
    private readonly TaskCompletionSource<byte[]> _binaryFrameReceived =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private LoopbackWebSocketServer(int port, ServerMode mode, string fragmentFirst, string fragmentSecond)
    {
        Port = port;
        Mode = mode;
        _fragmentFirst = fragmentFirst;
        _fragmentSecond = fragmentSecond;
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/ws/");
        // 必须先启动监听器再启动接受循环：否则 GetContextAsync 会因"监听器未启动"抛异常并退出循环，
        // 表现为客户端握手一直挂起（连接超时）。
        _listener.Start();
        _acceptLoop = Task.Run(AcceptLoopAsync);
    }

    /// <summary>监听端口。</summary>
    public int Port { get; }

    /// <summary>行为模式。</summary>
    public ServerMode Mode { get; }

    /// <summary>客户端连接地址（ws://，需在客户端开启 <c>AllowInsecureWebSocket</c>）。</summary>
    public string Url => $"ws://127.0.0.1:{Port}/ws/";

    /// <summary>
    /// 服务端已收到的完整二进制帧快照（P2-14 ACK 断言用；仅捕获单帧内完成的二进制消息）。
    /// </summary>
    public byte[][] ReceivedBinaryFrames
    {
        get
        {
            lock (_receivedFramesLock)
            {
                return _receivedBinaryFrames.ToArray();
            }
        }
    }

    /// <summary>
    /// 等待服务端收到首个完整二进制帧（P2-14 ACK 断言用）。
    /// </summary>
    public Task<byte[]> BinaryFrameReceived => _binaryFrameReceived.Task;

    /// <summary>
    /// 启动一个回环服务端（自动挑选空闲端口，失败重试 3 次）。
    /// </summary>
    public static LoopbackWebSocketServer Start(
        ServerMode mode,
        string fragmentFirst = "{\"part\":\"1",
        string fragmentSecond = "\"}")
    {
        Exception? lastError = null;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var port = GetFreePort();
            try
            {
                // 构造即启动监听（端口冲突等异常在此抛出并触发重试）
                return new LoopbackWebSocketServer(port, mode, fragmentFirst, fragmentSecond);
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException("无法在回环地址上启动测试用 WebSocket 服务端", lastError);
    }

    /// <summary>
    /// 获取一个当前空闲的 TCP 端口。
    /// </summary>
    public static int GetFreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private async Task AcceptLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                return; // 监听器已停止
            }

            _ = Task.Run(() => ServeAsync(context), CancellationToken.None);
        }
    }

    private async Task ServeAsync(HttpListenerContext context)
    {
        WebSocket? socket = null;
        try
        {
    var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);
            socket = webSocketContext.WebSocket;

            if (Mode == ServerMode.CloseImmediately)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "server-close", _cts.Token)
                    .ConfigureAwait(false);
                await DrainUntilCloseAsync(socket).ConfigureAwait(false);
                return;
            }

            if (Mode == ServerMode.SendFragmentedText)
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(Encoding.UTF8.GetBytes(_fragmentFirst)),
                    WebSocketMessageType.Text,
                    endOfMessage: false,
                    _cts.Token).ConfigureAwait(false);

                await Task.Delay(30, _cts.Token).ConfigureAwait(false);

                await socket.SendAsync(
                    new ArraySegment<byte>(Encoding.UTF8.GetBytes(_fragmentSecond)),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    _cts.Token).ConfigureAwait(false);
            }

            if (Mode == ServerMode.IgnoreCloseHandshake)
            {
                // P2-11：读到客户端的关闭帧后不应答（保持 TCP 半开），
                // 迫使客户端的关闭握手只能依靠超时 + Abort 收尾。
                await DrainUntilCloseFrameWithoutAckAsync(socket).ConfigureAwait(false);
                return;
            }

            await DrainUntilCloseAsync(socket).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // 测试夹具：服务端异常不阻断用例（由客户端侧断言兜底）
        }
        finally
        {
            socket?.Dispose();
        }
    }

    /// <summary>
    /// 读到关闭帧后完成关闭握手并退出（避免客户端 <c>CloseAsync</c> 等待对端关闭帧而超时）。
    /// 期间收到的完整二进制帧会被记录（P2-14 ACK 断言用）。
    /// </summary>
    private async Task DrainUntilCloseAsync(WebSocket socket)
    {
        var buffer = new byte[4096];

        try
        {
            while (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseSent)
            {
                var result = await socket
                    .ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
                    .ConfigureAwait(false);

                CaptureBinaryFrame(result, buffer);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    try
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "server-ack", CancellationToken.None)
                            .ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // 对端可能已断开，忽略
                    }

                    return;
                }
            }
        }
        catch (Exception)
        {
            // 对端断开，忽略
        }
    }

    /// <summary>
    /// 读到关闭帧后<b>不应答</b>并挂起（P2-11：保持客户端的关闭握手悬挂，直到服务端释放）。
    /// </summary>
    private async Task DrainUntilCloseFrameWithoutAckAsync(WebSocket socket)
    {
        var buffer = new byte[4096];

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket
                    .ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
                    .ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    // 不回发关闭帧：挂起直到服务端释放（测试结束 / 客户端 Abort），
                    // 使客户端的 CloseAsync 始终等不到对端关闭帧。
                    await Task.Delay(Timeout.Infinite, _cts.Token).ConfigureAwait(false);
                    return;
                }
            }
        }
        catch (Exception)
        {
            // 对端断开（客户端 Abort）或服务端释放，忽略
        }
    }

    /// <summary>
    /// 记录单帧内完成的二进制消息（ACK 帧小于缓冲区，不会分片）。
    /// </summary>
    private void CaptureBinaryFrame(WebSocketReceiveResult result, byte[] buffer)
    {
        if (result.MessageType != WebSocketMessageType.Binary ||
            !result.EndOfMessage ||
            result.Count <= 0 ||
            result.Count > buffer.Length)
        {
            return;
        }

        var captured = new byte[result.Count];
        Array.Copy(buffer, captured, result.Count);

        lock (_receivedFramesLock)
        {
            _receivedBinaryFrames.Add(captured);
        }

        _binaryFrameReceived.TrySetResult(captured);
    }

    /// <summary>
    /// 等待服务端完成至少一次接受（用于避免用例与服务端启动竞态）。
    /// </summary>
    public async Task WaitForAcceptAsync()
    {
        var deadline = DateTime.UtcNow + DefaultTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (_listener.IsListening)
            {
                await Task.Delay(20).ConfigureAwait(false);
                return;
            }

            await Task.Delay(20).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch (Exception)
        {
            // 忽略
        }

        try
        {
            await Task.WhenAny(_acceptLoop, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // 忽略
        }

        _cts.Dispose();
    }
}

#endif
