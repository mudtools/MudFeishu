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
        IgnoreCloseHandshake = 3,

        /// <summary>
        /// 发送一条<b>首帧即超限</b>的分片消息（<c>endOfMessage=false</c>），
        /// 随后补齐剩余分片并以一条合法消息收尾（R2/WS2-03 首帧分支）。
        /// </summary>
        SendOversizeFirstFragment = 4,

        /// <summary>
        /// 发送一条<b>累积超限</b>的分片消息（每帧都不超限，但累计超过上限），
        /// 随后补齐剩余分片并以一条合法消息收尾（R2/WS2-03 累积分支 + 消息边界守护）。
        /// </summary>
        SendOversizeAccumulatedFragments = 5,

        /// <summary>
        /// 持续发送<b>永不结束</b>的分片消息（始终 <c>endOfMessage=false</c>），
        /// 用于验证排空上界（帧数/字节）会把"恶意/异常对端"转为主动断连（R2/WS2-03 方案 B）。
        /// </summary>
        SendNeverEndingFragments = 6
    }

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _acceptLoop;
    private readonly string _fragmentFirst;
    private readonly string _fragmentSecond;
    private readonly int _fragmentSize;
    private readonly int _fragmentCount;
    private readonly string _afterDrainMessage;

    /// <summary>WS2-03 用例中"排空之后应当被正常派发"的合法消息。</summary>
    public const string PostDrainValidMessage = "{\"type\":\"probe\",\"seq\":1}";

    /// <summary>
    /// 已接受的服务端 socket（供用例按需主动投递帧）。
    /// </summary>
    /// <remarks>
    /// R2 新增：生命周期/存活/认证闸门类用例需要"连接建立后再投一帧"，而固定行为模式
    /// （<see cref="ServerMode"/>）无法表达"由用例决定投递时机"。完成赋值前调用
    /// <see cref="SendTextAsync"/>/<see cref="SendBinaryAsync"/> 会等待接受完成。
    /// </remarks>
    private readonly TaskCompletionSource<WebSocket> _acceptedSocket =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    // P2-14 集成断言用：捕获服务端收到的完整二进制帧（ACK 载荷）
    private readonly object _receivedFramesLock = new();
    private readonly List<byte[]> _receivedBinaryFrames = new();
    private readonly TaskCompletionSource<byte[]> _binaryFrameReceived =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private LoopbackWebSocketServer(
        int port,
        ServerMode mode,
        string fragmentFirst,
        string fragmentSecond,
        int fragmentSize,
        int fragmentCount,
        string afterDrainMessage)
    {
        Port = port;
        Mode = mode;
        _fragmentFirst = fragmentFirst;
        _fragmentSecond = fragmentSecond;
        _fragmentSize = fragmentSize;
        _fragmentCount = fragmentCount;
        _afterDrainMessage = afterDrainMessage;
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
        => Start(mode, fragmentFirst, fragmentSecond, fragmentSize: 2048, fragmentCount: 4);

    /// <summary>
    /// 启动一个回环服务端（可指定分片参数，用于 R2/WS2-03 的超限分片场景）。
    /// </summary>
    /// <param name="mode">服务端行为模式</param>
    /// <param name="fragmentFirst">文本分片模式的第一片内容</param>
    /// <param name="fragmentSecond">文本分片模式的第二片内容</param>
    /// <param name="fragmentSize">
    /// 超限分片场景中每片的字节数（二进制帧）。
    /// <b>须大于 0 且小于客户端上限</b>才能构造出"累积超限"；要构造"首帧即超限"则须大于客户端上限。
    /// </param>
    /// <param name="fragmentCount">超限分片场景的分片数量（<see cref="ServerMode.SendNeverEndingFragments"/> 时即总帧数）</param>
    /// <param name="afterDrainMessage">排空之后发送的合法消息（用于验证消息边界未被污染）</param>
    public static LoopbackWebSocketServer Start(
        ServerMode mode,
        string fragmentFirst,
        string fragmentSecond,
        int fragmentSize,
        int fragmentCount,
        string afterDrainMessage = PostDrainValidMessage)
    {
        Exception? lastError = null;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var port = GetFreePort();
            try
            {
                // 构造即启动监听（端口冲突等异常在此抛出并触发重试）
                return new LoopbackWebSocketServer(
                    port, mode, fragmentFirst, fragmentSecond, fragmentSize, fragmentCount, afterDrainMessage);
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
            _acceptedSocket.TrySetResult(socket);

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

            if (Mode == ServerMode.SendOversizeFirstFragment)
            {
                // 首帧即超限：客户端应在"写入内存流之前"拦截，并在排空剩余分片后继续处理后续消息
                await SendOversizeFragmentsAsync(socket, firstFragmentOversized: true).ConfigureAwait(false);
            }
            else if (Mode == ServerMode.SendOversizeAccumulatedFragments)
            {
                // 每帧都不超限但累积超限：客户端应在"写入前"拦截并排空
                await SendOversizeFragmentsAsync(socket, firstFragmentOversized: false).ConfigureAwait(false);
            }
            else if (Mode == ServerMode.SendNeverEndingFragments)
            {
                // 永不结束的分片流：客户端排空达上界后必须主动 Abort（否则排空会无穷进行）
                await SendNeverEndingFragmentsAsync(socket).ConfigureAwait(false);
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
    /// 发送一条超限的分片消息（首帧超限或累积超限），随后补齐分片并以合法消息收尾。
    /// </summary>
    /// <param name="socket">已接受的 WebSocket</param>
    /// <param name="firstFragmentOversized">
    /// <c>true</c> = 首帧即超限（命中客户端"首帧校验"分支）；
    /// <c>false</c> = 每帧都不超限、累积超限（命中客户端"写入前拦截"分支）。
    /// </param>
    /// <remarks>
    /// 用 <see cref="WebSocketMessageType.Binary"/> 而非 Text：二进制上限（<c>MaxBinaryMessageSize</c>）
    /// 可直接被测试调小，从而用很小的分片（几 KB）构造超限场景，避免用例传输数十 MB 数据。
    /// <para>
    /// 末尾**必须**再发一条合法消息：这才能验证"排空后消息边界未污染"——
    /// 若客户端在超限时直接 return（旧行为），被丢弃消息的尾部会被当作独立消息送入解析链路，
    /// 合法消息要么被吞掉、要么伴随解析错误出现。
    /// </para>
    /// </remarks>
    private async Task SendOversizeFragmentsAsync(WebSocket socket, bool firstFragmentOversized)
    {
        var firstSize = firstFragmentOversized ? Math.Max(_fragmentSize, 1) : Math.Min(_fragmentSize, 1024);
        var tailSize = firstFragmentOversized ? Math.Min(_fragmentSize, 1024) : Math.Max(_fragmentSize, 1);

        for (var i = 0; i < _fragmentCount; i++)
        {
            var size = i == 0 ? firstSize : tailSize;
            await socket.SendAsync(
                new ArraySegment<byte>(new byte[size]),
                WebSocketMessageType.Binary,
                endOfMessage: false,
                _cts.Token).ConfigureAwait(false);
        }

        // 结束本条超限消息
        await socket.SendAsync(
            new ArraySegment<byte>(new byte[1]),
            WebSocketMessageType.Binary,
            endOfMessage: true,
            _cts.Token).ConfigureAwait(false);

        await Task.Delay(50, _cts.Token).ConfigureAwait(false);

        // 排空之后必须能正常收到这条消息
        await socket.SendAsync(
            new ArraySegment<byte>(Encoding.UTF8.GetBytes(_afterDrainMessage)),
            WebSocketMessageType.Text,
            endOfMessage: true,
            _cts.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// 持续发送永不结束的分片（始终 <c>endOfMessage=false</c>），直到被要求停止。
    /// </summary>
    private async Task SendNeverEndingFragmentsAsync(WebSocket socket)
    {
        var payload = new byte[Math.Max(_fragmentSize, 1)];

        for (var i = 0; i < _fragmentCount && socket.State == WebSocketState.Open; i++)
        {
            try
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(payload),
                    WebSocketMessageType.Binary,
                    endOfMessage: false,
                    _cts.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // 客户端已按排空上界主动 Abort：发送必然失败，用例由客户端侧断言兜底
                return;
            }
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
    /// 主动向客户端投递一条文本消息（等待接受完成后发送）。
    /// </summary>
    /// <param name="text">消息内容</param>
    /// <returns>表示发送操作的任务</returns>
    /// <remarks>用于"连接建立后由用例决定投递时机"的存活/收发契约用例（R2）。</remarks>
    public async Task SendTextAsync(string text)
    {
        var socket = await WaitForAcceptedSocketAsync().ConfigureAwait(false);
        await socket.SendAsync(
            new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)),
            WebSocketMessageType.Text,
            endOfMessage: true,
            _cts.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// 主动向客户端投递一条二进制帧（等待接受完成后发送）。
    /// </summary>
    /// <param name="data">帧内容（例如序列化后的 <c>EventProtoData</c>）</param>
    /// <returns>表示发送操作的任务</returns>
    /// <remarks>用于认证闸门（<c>AuthGateTimeoutMs</c>）等"二进制帧是否进入处理链路"的用例（R2）。</remarks>
    public async Task SendBinaryAsync(byte[] data)
    {
        var socket = await WaitForAcceptedSocketAsync().ConfigureAwait(false);
        await socket.SendAsync(
            new ArraySegment<byte>(data),
            WebSocketMessageType.Binary,
            endOfMessage: true,
            _cts.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// 在给定窗口内等待服务端收到一条完整二进制帧（ACK 断言用）；超时返回 <c>null</c>。
    /// </summary>
    /// <param name="timeout">等待窗口</param>
    /// <returns>收到的帧；窗口内未收到则为 <c>null</c>。</returns>
    /// <remarks>
    /// R2 新增：用于"闸门超时 ⇒ 不应有 ACK"这类**负向断言**——
    /// 负向断言不能用 <c>Task.WhenAny</c> 无限等待，必须有确定的上界。
    /// </remarks>
    public async Task<byte[]?> TryWaitBinaryFrameAsync(TimeSpan timeout)
    {
        var completed = await Task.WhenAny(_binaryFrameReceived.Task, Task.Delay(timeout)).ConfigureAwait(false);
        return completed == _binaryFrameReceived.Task ? await _binaryFrameReceived.Task.ConfigureAwait(false) : null;
    }

    private async Task<WebSocket> WaitForAcceptedSocketAsync()
    {
        var completed = await Task.WhenAny(_acceptedSocket.Task, Task.Delay(DefaultTimeout)).ConfigureAwait(false);
        if (completed != _acceptedSocket.Task)
        {
            throw new TimeoutException("等待回环服务端接受连接超时");
        }

        return await _acceptedSocket.Task.ConfigureAwait(false);
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
