// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.DataModels.WsEndpoint;
using ProtoBuf;
using System.Reflection;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// HeartbeatManager ProtoBuf 心跳和 ClientConfig 动态配置测试
/// </summary>
public class HeartbeatManagerProtoBufTests
{
    private static HeartbeatManager CreateHeartbeatManager(
        FeishuWebSocketOptions? options = null,
        Func<byte[], CancellationToken, Task>? sendCallback = null)
    {
        options ??= new Mud.Feishu.WebSocket.FeishuWebSocketOptions { };
        sendCallback ??= (_, _) => Task.CompletedTask;

        return new HeartbeatManager(
            NullLogger<HeartbeatManager>.Instance,
            options,
            sendCallback);
    }

    #region ProtoBuf 二进制心跳

    [Fact]
    public async Task StartHeartbeatAsync_ShouldSendProtoBufBinaryPing_WhenConnected()
    {
        // Arrange
        byte[]? sentData = null;
        var pingSent = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var options = new FeishuWebSocketOptions
        {
            HeartbeatIntervalMs = 5000
        };

        var manager = new HeartbeatManager(
            NullLogger<HeartbeatManager>.Instance,
            options,
            (data, _) => { pingSent.TrySetResult(data); return Task.CompletedTask; });

        manager.SetServiceId(1001);

        using var cts = new CancellationTokenSource();
        var heartbeatTask = manager.StartHeartbeatAsync(cts.Token);

        // 等待心跳发送（间隔 5000ms）：用有界条件等待替代固定 Task.Delay(6000)——
        // 固定延时在 CI 负载下可能早于心跳触发（计时器节拍 + 线程池排队），造成偶发空引用。
        // 上界取 20s（间隔的 4 倍）：本用例真正要防的是"心跳续体被调度延迟"，
        // 而 CI runner 在满载时单个计时器续体可能被推迟数秒；通过路径在 Ping 送达时立即返回，
        // 故预算放大不增加正常耗时。续体"上下文捕获"这一确定性问题由
        // StartHeartbeatAsync_ShouldNotCaptureCallerSynchronizationContext 守卫。
        var completed = await Task.WhenAny(pingSent.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        completed.Should().BeSameAs(pingSent.Task,
            "心跳必须按 HeartbeatIntervalMs 触发（上界 20s 仅用于吸收调度延迟，间隔本身是 5s）");

        // Act
        sentData = await pingSent.Task;
        cts.Cancel();
        await heartbeatTask;

        // Assert
        sentData.Should().NotBeNull();
        var frame = Serializer.Deserialize<EventProtoData>(new MemoryStream(sentData!));
        frame.Service.Should().Be(1001);
        frame.Method.Should().Be(FrameBuilder.MethodControl);
        frame.MessageType.Should().Be(MessageType.Ping);
    }

    /// <summary>
    /// CI 事故回归：心跳循环是<b>后台循环</b>，不得把"发 Ping"的续体 Post 回调用方的
    /// <see cref="SynchronizationContext"/>。
    /// </summary>
    /// <remarks>
    /// 真实故障形态（GitHub Actions windows-latest：net8.0 与 net10.0 两个 TFM 同时失败）：
    /// xUnit 的最大并发闸门（<c>MaxConcurrencySyncContext</c>）被长阻塞用例占满时，
    /// <c>await Task.Delay(心跳间隔)</c> 的续体会排队等闸门配额 —— 5 秒间隔在 10 秒预算内
    /// 仍未送达。同一路径在生产等价于"心跳在调用方的 UI / 请求上下文上发送"。
    /// <para>
    /// 本用例把该风险<b>确定性化</b>：用一个"只计数、不执行"的门控上下文替换调用方上下文，
    /// 只要心跳续体经过 <c>Post</c>，Ping 就永远送不到（与闸门被占满的最坏情形同构），
    /// 无需依赖 CI 负载碰运气。
    /// </para>
    /// </remarks>
    [Fact]
    public async Task StartHeartbeatAsync_ShouldNotCaptureCallerSynchronizationContext()
    {
        // Arrange
        var pingSent = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var manager = CreateHeartbeatManager(
            new FeishuWebSocketOptions(),
            (data, _) => { pingSent.TrySetResult(data); return Task.CompletedTask; });
        manager.SetServiceId(1001);

        // 本用例不验证间隔值（Options 下界 5000 属 Validate 硬约束，另有专门用例覆盖），
        // 只验证"续体落在哪个上下文"，故直接注入短间隔以免用例白等 5 秒。
        SetHeartbeatIntervalMs(manager, 200);

        var gate = new GatedSynchronizationContext();
        var originalContext = SynchronizationContext.Current;
        using var cts = new CancellationTokenSource();

        Task heartbeatTask;
        SynchronizationContext.SetSynchronizationContext(gate);
        try
        {
            // Act：在"调用方上下文已被占满"的上下文内启动心跳循环。
            // StartHeartbeatAsync 的同步段在此处捕获上下文并注册首个 await（Task.Delay），
            // 因此随后即可恢复原上下文——被测行为（续体是否 Post 回捕获的上下文）已经定型，
            // 用例自身的等待不再需要落在门控上（否则失败路径会挂死而不是失败）。
            heartbeatTask = manager.StartHeartbeatAsync(cts.Token);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }

        var completed = await Task.WhenAny(pingSent.Task, Task.Delay(TimeSpan.FromSeconds(5)));

        // 收尾（不放行门控）：断言不依赖心跳循环是否已退出
        cts.Cancel();
        await Task.WhenAny(heartbeatTask, Task.Delay(TimeSpan.FromSeconds(2)));

        // Assert
        completed.Should().BeSameAs(pingSent.Task,
            "后台心跳循环必须与调用方的 SynchronizationContext 解耦：续体一旦 Post 回被占满的上下文，" +
            "Ping 就会被推迟到远超 HeartbeatIntervalMs（CI 中 5 秒间隔 + 10 秒预算仍未送达即此形态）");
        gate.PostCount.Should().Be(0, "心跳路径不得向调用方上下文 Post 任何续体");
    }

    /// <summary>
    /// 覆盖私有心跳间隔字段（Options 的 setter 会把间隔钳制到 ≥5000ms，构造函数不校验）。
    /// </summary>
    private static void SetHeartbeatIntervalMs(HeartbeatManager manager, int intervalMs)
    {
        var field = typeof(HeartbeatManager).GetField("_heartbeatIntervalMs", BindingFlags.NonPublic | BindingFlags.Instance);
        field.Should().NotBeNull();
        field!.SetValue(manager, intervalMs);
    }

    /// <summary>
    /// 门控 <see cref="SynchronizationContext"/>：续体只计数、不执行，用于模拟
    /// "调用方上下文已被占满、短期内不可能回到该上下文"的最坏情形。
    /// </summary>
    private sealed class GatedSynchronizationContext : SynchronizationContext
    {
        private int _postCount;

        /// <summary>被吞掉的 <c>Post</c> 次数（&gt;0 即说明有续体依赖调用方上下文）。</summary>
        public int PostCount => Volatile.Read(ref _postCount);

        public override void Post(SendOrPostCallback d, object? state) => Interlocked.Increment(ref _postCount);
    }

    [Fact]
    public void SetServiceId_ShouldStoreServiceId()
    {
        // Arrange
        var manager = CreateHeartbeatManager();

        // Act
        manager.SetServiceId(2002);

        // Assert - 验证通过反射
        var serviceIdField = typeof(HeartbeatManager).GetField("_serviceId", BindingFlags.NonPublic | BindingFlags.Instance);
        serviceIdField.Should().NotBeNull();
        var value = serviceIdField!.GetValue(manager);
        value.Should().Be(2002);
    }

    #endregion

    #region PongReceived + ClientConfig 动态配置

    [Fact]
    public void OnPongReceived_WithClientConfig_ShouldUpdateHeartbeatInterval()
    {
        // Arrange
        var options = new FeishuWebSocketOptions
        {
            HeartbeatIntervalMs = 25000
        };
        var manager = CreateHeartbeatManager(options);

        var config = new ClientConfigInfo
        {
            PingInterval = 120,  // 秒 → 钳制到 30 秒上界
            ReconnectInterval = 60,
            ReconnectCount = 3,
            ReconnectNonce = 10
        };

        // Act
        manager.OnPongReceived(config);

        // Assert - WS-07 修复：PingInterval 被钳制到 5~30 秒区间，写入私有字段 _heartbeatIntervalMs
        // 120 秒被钳制为 30 秒 = 30000 毫秒，不再回写 Options
        var heartbeatField = typeof(HeartbeatManager).GetField("_heartbeatIntervalMs", BindingFlags.NonPublic | BindingFlags.Instance);
        heartbeatField.Should().NotBeNull();
        heartbeatField!.GetValue(manager).Should().Be(30000);
        // Options 实例不被运行时改写
        options.HeartbeatIntervalMs.Should().Be(25000);
    }

    [Fact]
    public void OnPongReceived_WithNullConfig_ShouldNotUpdateOptions()
    {
        // Arrange
        var options = new FeishuWebSocketOptions
        {
            HeartbeatIntervalMs = 25000
        };
        var manager = CreateHeartbeatManager(options);

        // Act
        manager.OnPongReceived(null);

        // Assert - 配置不变
        options.HeartbeatIntervalMs.Should().Be(25000);
    }

    [Fact]
    public void OnPongReceived_WithClientConfig_ShouldNotUpdateReconnectDelay()
    {
        // Arrange - WS-07 修复：ReconnectDelayMs 属于本地运维策略，禁止被运行时改写
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions { Reconnect = new Mud.Feishu.WebSocket.WebSocketReconnectOptions { BaseDelayMs = 5000 } };
        var manager = CreateHeartbeatManager(options);

        var config = new ClientConfigInfo
        {
            ReconnectInterval = 120 // 秒
        };

        // Act
        manager.OnPongReceived(config);

        // Assert - 服务端建议的重连间隔被忽略，使用本地配置
        options.Reconnect.BaseDelayMs.Should().Be(5000);
    }

    [Fact]
    public void OnPongReceived_WithReconnectCountMinusOne_ShouldNotUpdateMaxReconnectAttempts()
    {
        // Arrange - WS-07 修复：MaxReconnectAttempts 属于本地运维策略，禁止被运行时改写
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions { Reconnect = new Mud.Feishu.WebSocket.WebSocketReconnectOptions { MaxAttempts = 5 } };
        var manager = CreateHeartbeatManager(options);

        var config = new ClientConfigInfo
        {
            ReconnectCount = -1 // Java SDK: 无限重连
        };

        // Act
        manager.OnPongReceived(config);

        // Assert - 服务端建议的重连次数被忽略，使用本地配置
        options.Reconnect.MaxAttempts.Should().Be(5);
    }

    [Fact]
    public void OnPongReceived_WithReconnectCountPositive_ShouldNotUpdateMaxReconnectAttempts()
    {
        // Arrange - WS-07 修复：MaxReconnectAttempts 属于本地运维策略，禁止被运行时改写
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions { Reconnect = new Mud.Feishu.WebSocket.WebSocketReconnectOptions { MaxAttempts = 5 } };
        var manager = CreateHeartbeatManager(options);

        var config = new ClientConfigInfo
        {
            ReconnectCount = 10
        };

        // Act
        manager.OnPongReceived(config);

        // Assert - 服务端建议的重连次数被忽略，使用本地配置
        options.Reconnect.MaxAttempts.Should().Be(5);
    }

    #endregion

    #region 构造函数验证

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenSendBinaryCallbackIsNull()
    {
        var act = () => new HeartbeatManager(
            NullLogger<HeartbeatManager>.Instance,
            new FeishuWebSocketOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("sendBinaryCallback");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        var act = () => new HeartbeatManager(
            NullLogger<HeartbeatManager>.Instance,
            null!,
            (_, _) => Task.CompletedTask);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    #endregion
}
