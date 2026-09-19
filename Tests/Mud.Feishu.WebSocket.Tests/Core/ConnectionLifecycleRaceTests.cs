// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net.WebSockets;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mud.Feishu.WebSocket.SocketEventArgs;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// P1-2 / P1-3 回归测试：断线声明绑定连接代次（socket 身份）+ 失败握手不得吞事件/误报事件。
/// </summary>
/// <remarks>
/// 改造前 <c>_disconnectedFired</c> 是实例级唯一标志：旧接收循环在新连接建立后抛出的非取消异常
/// 会声明成功 → 误报"新连接已断开"、连接计数漂移，并吞掉后续真实断线。
/// <para>
/// 改造后 <c>NotifyDisconnected(args, owner)</c> 只接受"当前 socket"的断线声明。
/// 因 <c>ClientWebSocket</c> 为 sealed 且无法注入已连接替身，本类用反射构造"已连接/已替换"的状态
/// （与既有 <c>P0P1FixRegressionTests</c> 的范式一致）；真实握手路径由 W5 的回环集成测试覆盖。
/// </para>
/// </remarks>
[Trait("Category", "Concurrency")]
public class ConnectionLifecycleRaceTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();

    public ConnectionLifecycleRaceTests()
    {
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);
    }

    private WebSocketConnectionManager CreateManager(int connectionTimeoutMs = 2000)
        => new(NullLogger<WebSocketConnectionManager>.Instance,
            new FeishuWebSocketOptions
            {
                ConnectionTimeoutMs = connectionTimeoutMs,
                // 本类用例聚焦"生命周期"语义，关闭 P2-15 的主机白名单（默认 *.feishu.cn;*.larksuite.com
                // 会先于连接失败抛出 ArgumentException，遮蔽被测路径）；白名单语义见 HostAllowListTests
                AllowedHostSuffixes = string.Empty
            },
            _loggerFactoryMock.Object);

    private static void SetField(object target, string fieldName, object? value)
    {
        typeof(WebSocketConnectionManager)
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(target, value);
    }

    private static void InvokeNotifyDisconnected(
        WebSocketConnectionManager manager, WebSocketCloseEventArgs args, ClientWebSocket? owner)
    {
        var method = typeof(WebSocketConnectionManager)
            .GetMethod("NotifyDisconnected", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // P1-2 改造后为双参数（args, owner）；MethodInfo.Invoke 参数个数必须精确匹配
        method.Invoke(manager, new object?[] { args, owner });
    }

    [Fact]
    public void NotifyDisconnected_ShouldIgnoreStaleOwner_WhenSocketReplaced()
    {
        // Arrange：模拟"旧连接已关、新连接已建立"的状态
        var manager = CreateManager();
        var fired = 0;
        manager.Disconnected += (_, _) => Interlocked.Increment(ref fired);

        var staleSocket = new ClientWebSocket();
        var currentSocket = new ClientWebSocket();

        try
        {
            SetField(manager, "_webSocket", currentSocket);
            SetField(manager, "_disconnectedFired", 0);
            SetField(manager, "_connectionCount", 1);

            // Act：旧 socket 的迟到断线声明
            InvokeNotifyDisconnected(manager, new WebSocketCloseEventArgs(), staleSocket);

            // Assert：必须被丢弃（不触发事件、不递减计数、不吞掉后续真实断线）
            fired.Should().Be(0, "非当前 socket 的迟到声明必须被忽略");
            manager.ConnectionCount.Should().Be(1, "计数不得因过期声明而漂移");

            // Act：当前 socket 的真实断线声明
            InvokeNotifyDisconnected(manager, new WebSocketCloseEventArgs(), currentSocket);

            // Assert：仍能正常触发（未被过期声明提前占用原子标志）
            fired.Should().Be(1);
            manager.ConnectionCount.Should().Be(0);
        }
        finally
        {
            staleSocket.Dispose();
            currentSocket.Dispose();
        }
    }

    [Fact]
    public void NotifyDisconnected_ShouldNotFilter_WhenOwnerIsNull()
    {
        // Arrange：owner = null 表示"不做代次过滤"（向后兼容语义，既有反射用例依赖该行为）
        var manager = CreateManager();
        var fired = 0;
        manager.Disconnected += (_, _) => Interlocked.Increment(ref fired);

        SetField(manager, "_disconnectedFired", 0);
        SetField(manager, "_connectionCount", 1);

        // Act
        InvokeNotifyDisconnected(manager, new WebSocketCloseEventArgs(), null);

        // Assert
        fired.Should().Be(1);
        manager.ConnectionCount.Should().Be(0);
    }

    [Fact]
    public void ConnectionCount_ShouldNotDrift_WhenStaleNotifyRaces()
    {
        // Arrange
        var manager = CreateManager();
        var fired = 0;
        manager.Disconnected += (_, _) => Interlocked.Increment(ref fired);

        var staleSocket = new ClientWebSocket();
        var currentSocket = new ClientWebSocket();

        try
        {
            SetField(manager, "_webSocket", currentSocket);
            SetField(manager, "_disconnectedFired", 0);
            SetField(manager, "_connectionCount", 1);

            // Act：16 个过期声明 + 16 个当前声明并发
            Parallel.For(0, 32, i =>
            {
                var owner = i % 2 == 0 ? staleSocket : currentSocket;
                InvokeNotifyDisconnected(manager, new WebSocketCloseEventArgs(), owner);
            });

            // Assert：过期声明全部被丢弃，当前连接恰好声明一次
            fired.Should().Be(1);
            manager.ConnectionCount.Should().Be(0, "连接计数不得因过期声明漂移（不得为负或残留）");
        }
        finally
        {
            staleSocket.Dispose();
            currentSocket.Dispose();
        }
    }

    [Fact]
    public async Task ConnectAsync_ShouldNotRaiseDisconnected_WhenNoPreviousConnection()
    {
        // Arrange：此前从未连接过（无待派发的 Disconnected）
        var manager = CreateManager();
        var fired = 0;
        manager.Disconnected += (_, _) => Interlocked.Increment(ref fired);

        // Act：连接一个必然拒绝的端口（回环端口 9）
        var act = () => manager.ConnectAsync("wss://127.0.0.1:9/never");

        // Assert：异常必须照常抛出（P1-3 不得吞掉失败），且不得误报 Disconnected
        await act.Should().ThrowAsync<Exception>();
        fired.Should().Be(0, "没有旧连接的失败握手不得误报 Disconnected");
        manager.ConnectionCount.Should().Be(0);
    }

    [Fact]
    public async Task ConnectAsync_ShouldRejectInsecureScheme_WhenNotAllowed()
    {
        // 回归护栏：入口守卫（P1-5）不得改变既有的 URL 校验顺序
        var manager = CreateManager();

        var act = () => manager.ConnectAsync("ws://127.0.0.1:9/never");

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*ws://*");
    }
}
