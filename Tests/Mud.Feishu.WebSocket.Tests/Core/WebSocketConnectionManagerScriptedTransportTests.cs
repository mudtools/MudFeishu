// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net.WebSockets;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.WebSocket.SocketEventArgs;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// 经"脚本化传输"覆盖分片/排空语义的确定性单元测试（FU-4 / R1 TD-1）。
/// </summary>
/// <remarks>
/// <b>为什么需要第二套手段（已有回环集成测试）</b>：回环服务端只能观测"对客户端可见的效果"
/// （是否断连、是否派发、是否回 ACK），无法断言**内部动作**——尤其是"排空上界触发时是否真的调用了
/// <c>Abort</c>"、以及"排空究竟消费了哪几帧"。而这两点正是 D3 方案 B 与 WS2-03 的核心契约。
/// <para>
/// 脚本化传输（<see cref="ScriptedWebSocket"/>）直接替换 <see cref="ClientWebSocket"/>，
/// 因此：① 无端口、无内核 socket（不受 CI 防火墙/端口占用影响）；② 可断言 Abort 与帧消费；
/// ③ 帧序列完全由用例决定（可构造"永不结束"这类回环服务端需要靠计数模拟的形态）。
/// 它与回环集成测试是**互补**关系：前者锁定内部动作，后者锁定端到端行为。
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
public class WebSocketConnectionManagerScriptedTransportTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    /// <summary>客户端接收缓冲（<see cref="FeishuWebSocketOptions.InitialReceiveBufferSize"/> 默认 4096）。</summary>
    private const int ReceiveBufferSize = 4096;

    /// <summary>二进制消息上限（调小以便用少量字节构造超限）。</summary>
    private const int MaxBinaryMessageSize = 1024;

    private static WebSocketConnectionManager CreateManager(ScriptedWebSocket socket)
        => new(
            NullLogger<WebSocketConnectionManager>.Instance,
            new FeishuWebSocketOptions
            {
                Certificate = new WebSocketCertificateOptions { AllowInsecureWebSocket = true },
                AllowedHostSuffixes = "127.0.0.1",
                ConnectionTimeoutMs = 3000,
                MessageSizeLimits = new MessageSizeLimits { MaxBinaryMessageSize = MaxBinaryMessageSize }
            },
            NullLoggerFactory.Instance,
            socketFactory: () => socket,
            hostEnvironment: null);

    private static async Task WaitUntilAsync(Func<bool> condition, string because)
    {
        var started = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - started > WaitTimeout)
            {
                throw new TimeoutException($"等待条件超时：{because}");
            }

            await Task.Delay(10);
        }
    }

    /// <summary>
    /// 首帧超限：必须**排空至消息边界**（消费掉被丢弃消息的剩余分片）后继续处理后续消息。
    /// </summary>
    /// <remarks>
    /// 与回环集成用例 <c>HandleFragmentedMessageAsync_ShouldRejectFirstFrame_WhenFirstFragmentExceedsLimit</c>
    /// 的差别：本用例额外断言"排空恰好消费了剩余 2 帧 + 结束帧"，
    /// 即**消息边界**被真正推进到 <c>EndOfMessage</c>，而不只是"后续消息恰好也能到达"。
    /// </remarks>
    [Fact]
    public async Task HandleFragmentedMessageAsync_ShouldDrainDroppedMessage_WhenFirstFrameExceedsLimit()
    {
        // Arrange：超限首帧 + 2 个尾部片段（均 endOfMessage=false）+ 结束帧 + 一条合法文本消息
        var socket = new ScriptedWebSocket();
        socket.EnqueueBinary(new byte[ReceiveBufferSize], endOfMessage: false);   // 首帧 4096 > 上限 1024
        socket.EnqueueBinary(new byte[64], endOfMessage: false);
        socket.EnqueueBinary(new byte[64], endOfMessage: false);
        socket.EnqueueBinary(Array.Empty<byte>(), endOfMessage: true);            // 本条消息的边界
        socket.EnqueueText("{\"type\":\"probe\"}");

        var manager = CreateManager(socket);
        var delivered = new List<string>();
        var errors = new List<WebSocketErrorEventArgs>();
        manager.Error += (_, e) => { lock (delivered) { errors.Add(e); } };

        try
        {
            await manager.ConnectAsync("ws://127.0.0.1:9/ws/");

            // Act
            var receiveTask = manager.StartReceivingAsync((buffer, result) =>
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    lock (delivered)
                    {
                        delivered.Add(System.Text.Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
                    }
                }

                return Task.CompletedTask;
            });

            await WaitUntilAsync(
                () => { lock (delivered) { return delivered.Count > 0; } },
                "排空超限消息后，后续合法消息必须被派发");

            // Assert
            lock (delivered)
            {
                delivered.Should().ContainSingle().Which.Should().Be("{\"type\":\"probe\"}");
                errors.Should().Contain(e => e.ErrorType == "FragmentSizeExceeded");
            }

            socket.ConsumedFrameCount.Should().Be(5,
                "排空必须消费到本条消息的 EndOfMessage（4 帧）后继续消费下一条消息（1 帧）——" +
                "若只丢弃首帧而不排空，被丢消息的尾部会被当作独立消息送入解析链路");

            socket.AbortCalled.Should().BeFalse("单条消息超限属受控丢弃，不得中止连接");

            await manager.DisconnectAsync();
            await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    /// <summary>
    /// 排空上界：对端持续投递永不结束的分片时，必须**主动 Abort**（D3 方案 B）。
    /// </summary>
    /// <remarks>
    /// 这是本用例存在的核心理由：回环服务端只能间接验证（等待 Disconnected 事件），
    /// 而"是否真的调用了 <c>Abort</c>"是方案 B 的字面契约——脚本化传输可以直接断言它。
    /// 同时验证上界命中的是**帧数**上界（<c>MaxDrainFrames</c>）而非只靠对端先放弃。
    /// </remarks>
    [Fact]
    public async Task HandleFragmentedMessageAsync_ShouldAbortConnection_WhenDrainExceedsFrameBound()
    {
        // Arrange：首帧即超限 → 进入排空；随后持续投递 1024+ 帧永不结束的分片
        const int totalFrames = 1100;
        var socket = new ScriptedWebSocket();
        socket.EnqueueBinary(new byte[ReceiveBufferSize], endOfMessage: false);   // 触发首帧超限

        for (var i = 0; i < totalFrames; i++)
        {
            socket.EnqueueBinary(new byte[128], endOfMessage: false);             // 永不结束
        }

        var manager = CreateManager(socket);
        var disconnected = new TaskCompletionSource<WebSocketCloseEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        manager.Disconnected += (_, e) => disconnected.TrySetResult(e);

        try
        {
            await manager.ConnectAsync("ws://127.0.0.1:9/ws/");
            var receiveTask = manager.StartReceivingAsync((_, _) => Task.CompletedTask);

            // Act
            var completed = await Task.WhenAny(disconnected.Task, Task.Delay(WaitTimeout));

            // Assert
            completed.Should().BeSameAs(disconnected.Task, "排空达到上界必须中止连接并产生断线声明");
            socket.AbortCalled.Should().BeTrue(
                "D3 方案 B 的字面契约：达到排空上界时必须调用 Abort（让接收循环以异常退出并触发重连），" +
                "而不是继续无限排空");
            manager.ConnectionCount.Should().Be(0);

            await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    /// <summary>
    /// 正常分片重组：完整消息必须被组装后一次性派发（不因排空逻辑引入而回归）。
    /// </summary>
    [Fact]
    public async Task HandleFragmentedMessageAsync_ShouldReassembleAndDeliver_WhenMessageCompletes()
    {
        var socket = new ScriptedWebSocket();
        socket.EnqueueText("{\"part\":\"1", endOfMessage: false);
        socket.EnqueueText("\"}");
        socket.EnqueueText("{\"type\":\"probe\"}");

        var manager = CreateManager(socket);
        var delivered = new List<string>();

        try
        {
            await manager.ConnectAsync("ws://127.0.0.1:9/ws/");
            var receiveTask = manager.StartReceivingAsync((buffer, result) =>
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    lock (delivered)
                    {
                        delivered.Add(System.Text.Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
                    }
                }

                return Task.CompletedTask;
            });

            await WaitUntilAsync(() => { lock (delivered) { return delivered.Count >= 2; } },
                "重组后的完整消息与后续消息都必须被派发");

            lock (delivered)
            {
                delivered.Should().Equal(new[] { "{\"part\":\"1\"}", "{\"type\":\"probe\"}" },
                    "分片必须按消息边界重组后整体派发");
            }

            socket.AbortCalled.Should().BeFalse();

            await manager.DisconnectAsync();
            await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    /// <summary>
    /// 脚本化传输替身（FU-4）：按预设帧序列应答，并记录发送/中止动作。
    /// </summary>
    /// <remarks>
    /// <b>为什么继承抽象 <see cref="WebSocket"/> 而不是 <see cref="ClientWebSocket"/></b>：
    /// 后者在 .NET Core 3.0+ 已被 <c>sealed</c>（无法派生）；这也正是连接管理器把
    /// <c>_webSocket</c> 与 socket 工厂收敛到抽象基类的原因——依赖抽象是 TD-1 的本意。
    /// <para>
    /// 只覆盖 <see cref="WebSocketConnectionManager"/> 实际使用的成员
    /// （<c>ConnectAsync</c>/<c>ReceiveAsync</c>/<c>SendAsync</c>/<c>CloseAsync</c>/<c>CloseOutputAsync</c>/<c>Abort</c>/<c>State</c>/<c>CloseStatus</c>/<c>Dispose</c>）。
    /// 帧队列耗尽后返回 <see cref="WebSocketMessageType.Close"/>：让接收循环走正常关闭路径，
    /// 避免测试因"挂起的 ReceiveAsync"而依赖超时（<b>不引入 flaky 时间窗</b>）。
    /// </para>
    /// </remarks>
    private sealed class ScriptedWebSocket : System.Net.WebSockets.WebSocket
    {
        private readonly Queue<(byte[] Data, WebSocketMessageType Type, bool EndOfMessage)> _inbound = new();
        private readonly List<(byte[] Data, WebSocketMessageType Type, bool EndOfMessage)> _sent = new();
        private int _consumed;
        private int _abortCalled;

        // 内部 seam 契约：替身由工厂直接交付"已连接"实例（抽象 WebSocket 无 ConnectAsync）
        private WebSocketState _state = WebSocketState.Open;

        /// <summary>是否已调用 <see cref="Abort"/>。</summary>
        public bool AbortCalled => Volatile.Read(ref _abortCalled) == 1;

        /// <summary>已被接收循环取走的帧数（用于断言"排空消费了多少"）。</summary>
        public int ConsumedFrameCount => Volatile.Read(ref _consumed);

        /// <summary>已发送的帧（用于断言 ACK/心跳等出站行为）。</summary>
        public IReadOnlyList<(byte[] Data, WebSocketMessageType Type, bool EndOfMessage)> SentFrames
        {
            get
            {
                lock (_sent)
                {
                    return _sent.ToArray();
                }
            }
        }

        public void EnqueueText(string payload, bool endOfMessage = true)
            => Enqueue(System.Text.Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, endOfMessage);

        public void EnqueueBinary(byte[] data, bool endOfMessage = true)
            => Enqueue(data, WebSocketMessageType.Binary, endOfMessage);

        private void Enqueue(byte[] data, WebSocketMessageType type, bool endOfMessage)
        {
            lock (_inbound)
            {
                _inbound.Enqueue((data, type, endOfMessage));
            }
        }

        public override WebSocketState State => _state;

        public override WebSocketCloseStatus? CloseStatus => null;

        public override string? CloseStatusDescription => null;

        public override string? SubProtocol => null;

        public override Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_state != WebSocketState.Open)
            {
                return Task.FromException<WebSocketReceiveResult>(
                    new WebSocketException(WebSocketError.ConnectionClosedPrematurely, "脚本传输已停止"));
            }

            (byte[] Data, WebSocketMessageType Type, bool EndOfMessage) frame;

            lock (_inbound)
            {
                if (_inbound.Count == 0)
                {
                    // 脚本耗尽：返回关闭帧，让接收循环走正常关闭路径（不制造挂起）
                    _state = WebSocketState.CloseReceived;
                    return Task.FromResult(new WebSocketReceiveResult(
                        0, WebSocketMessageType.Close, true));
                }

                frame = _inbound.Dequeue();
            }

            Interlocked.Increment(ref _consumed);

            // 模拟真实 socket：一次 ReceiveAsync 最多产出接收缓冲大小的数据
            var count = Math.Min(frame.Data.Length, buffer.Count);
            if (count > 0)
            {
                Array.Copy(frame.Data, 0, buffer.Array!, buffer.Offset, count);
            }

            // 被截断的帧在语义上仍是"分片未完"（真实 socket 亦如此）
            var endOfMessage = frame.EndOfMessage && count == frame.Data.Length;

            return Task.FromResult(new WebSocketReceiveResult(count, frame.Type, endOfMessage));
        }

        public override Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var copy = new byte[buffer.Count];
            if (buffer.Count > 0)
            {
                Array.Copy(buffer.Array!, buffer.Offset, copy, 0, buffer.Count);
            }

            lock (_sent)
            {
                _sent.Add((copy, messageType, endOfMessage));
            }

            return Task.CompletedTask;
        }

        public override Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
        {
            _state = WebSocketState.CloseSent;
            return Task.CompletedTask;
        }

        public override void Abort()
        {
            Interlocked.Exchange(ref _abortCalled, 1);
            _state = WebSocketState.Aborted;
        }

        /// <remarks>
        /// <c>System.Net.WebSockets.WebSocket.Dispose()</c> 是 <c>abstract</c>（非 <c>Dispose(bool)</c> 模式），
        /// 且**不含** <c>ConnectAsync</c>——该成员只在客户端实现（<c>ClientWebSocket</c>）上存在，
        /// 这正是连接管理器需要显式收敛到具体类型的原因。
        /// </remarks>
        public override void Dispose()
        {
            if (_state != WebSocketState.Aborted)
            {
                _state = WebSocketState.Closed;
            }
        }
    }
}
