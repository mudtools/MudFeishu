// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET8_0_OR_GREATER

using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.DataModels.WsEndpoint;
using Mud.Feishu.WebSocket.Tests.Integration;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// 压力与长稳冒烟（R2 / WS2-18）。标记 <c>Category=Stress</c>，**不进全量门禁**。
/// </summary>
/// <remarks>
/// xUnit 的 <c>Trait</c> <b>不会</b>自动排除用例——<c>scripts/verify-build.ps1</c> 步骤 4 已显式传入
/// <c>--filter "Category!=Stress"</c>。手工执行：
/// <code>dotnet test Tests/Mud.Feishu.WebSocket.Tests -c Release -f net8.0 --filter "Category=Stress"</code>
/// <para>
/// 这两条用例的价值在于验证**不变量在持续负载与长时间静默下依然成立**，
/// 而普通单测只覆盖单次交互：
/// <list type="bullet">
/// <item>背压上界必须是"排队量"而非仅"处理中数量"（P1-1 修复的语义）；</item>
/// <item>"长时间无帧"**不得**被误判为僵尸（否则会周期性误报 Unhealthy 并触发无谓重连）。</item>
/// </list>
/// </para>
/// </remarks>
[Trait("Category", "Stress")]
public class FeishuWebSocketStressTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(60);

    private const int BurstFrameCount = 5000;
    private const int MaxConcurrentHandlers = 8;

    private static FeishuWebSocketConcurrencyService CreateConcurrencyService()
    {
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(o => o.MaxConcurrentHandlers = MaxConcurrentHandlers);
        var optionsMonitor = services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        return new FeishuWebSocketConcurrencyService(
            optionsMonitor, NullLogger<FeishuWebSocketConcurrencyService>.Instance);
    }

    private static FeishuWebSocketClient CreateClient(FeishuWebSocketConcurrencyService concurrencyService)
    {
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        factoryMock.Setup(x => x.GetHandler(It.IsAny<string>())).Returns(Mock.Of<IFeishuEventHandler>());
        factoryMock.Setup(x => x.GetHandlers(It.IsAny<string>())).Returns(new List<IFeishuEventHandler>());

        return new FeishuWebSocketClient(
            NullLogger<FeishuWebSocketClient>.Instance,
            factoryMock.Object,
            NullLoggerFactory.Instance,
            options: new FeishuWebSocketOptions
            {
                Certificate = new WebSocketCertificateOptions { AllowInsecureWebSocket = true },
                AllowedHostSuffixes = "127.0.0.1",
                ConnectionTimeoutMs = 3000,
                MaxConcurrentHandlers = MaxConcurrentHandlers
            },
            concurrencyService: concurrencyService);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string because)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > WaitTimeout)
            {
                throw new TimeoutException($"等待条件超时：{because}");
            }

            await Task.Delay(50);
        }
    }

    /// <summary>
    /// 5000 帧突发下，**积压量峰值**必须受 <c>MaxConcurrentHandlers</c> 约束。
    /// </summary>
    /// <remarks>
    /// 关键口径：租约在**接收路径**获取（P1-1 修复），因此"已排队任务 + 已排队缓冲"一并受上界约束；
    /// 改造前租约在处理任务内获取，只限制"同时处理数"，排队量与消息副本无上界
    /// （单帧上限 10MB ⇒ 慢消费时内存可被线性放大）。
    /// </remarks>
    [Fact]
    public async Task Burst_ShouldBoundBacklog_UnderSustainedLoad()
    {
        // Arrange
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        await using var concurrencyService = CreateConcurrencyService();
        var client = CreateClient(concurrencyService);

        var dispatched = 0;
        var peakBacklog = 0;
        var backlogSamplerCancelled = false;

        client.MessageReceived += (_, _) => Interlocked.Increment(ref dispatched);

        try
        {
            await client.ConnectAsync(new WsEndpointResult { Url = server.Url });

            // 采样积压峰值（独立线程，避免与接收循环争用同一线程）
            var sampler = Task.Run(async () =>
            {
                while (!Volatile.Read(ref backlogSamplerCancelled))
                {
                    var current = concurrencyService.PendingCount;
                    if (current > Volatile.Read(ref peakBacklog))
                    {
                        Volatile.Write(ref peakBacklog, current);
                    }

                    await Task.Delay(5);
                }
            });

            // Act：持续投递突发帧（服务端发送会因客户端反压而自然变慢，这正是要验证的语义）
            var payload = "{\"type\":\"probe\",\"payload\":\"" + new string('x', 512) + "\"}";
            _ = Task.Run(async () =>
            {
                for (var i = 0; i < BurstFrameCount; i++)
                {
                    try
                    {
                        await server.SendTextAsync(payload);
                    }
                    catch
                    {
                        return;
                    }
                }
            });

            await WaitUntilAsync(() => Volatile.Read(ref dispatched) >= BurstFrameCount,
                $"{BurstFrameCount} 帧突发必须全部被派发（不得因反压而丢帧）");

            Volatile.Write(ref backlogSamplerCancelled, true);
            await sampler;

            // Assert
            Volatile.Read(ref peakBacklog).Should().BeLessThanOrEqualTo(MaxConcurrentHandlers,
                "背压闸门必须把**排队量**也约束在 MaxConcurrentHandlers 之内" +
                "（改造前租约在处理任务内获取，排队无上界）");
        }
        finally
        {
            Volatile.Write(ref backlogSamplerCancelled, true);
            await client.DisposeAsync();
        }
    }

    /// <summary>
    /// 长时间静默（对端不发任何帧）**不得**被判为僵尸态——否则会造成周期性误报与无谓重连。
    /// </summary>
    /// <remarks>
    /// 这是 F1 判定口径的守护：飞书长连接在空闲时段本就没有事件帧
    /// （客户端心跳是**单向**发往服务端），因此 <c>IdleMs</c> 很大是正常现象。
    /// 只有"<c>State == Open</c> 且接收循环已结束"这一**确定性矛盾**才允许判死。
    /// </remarks>
    [Fact]
    public async Task Liveness_ShouldNotReportZombie_WhenConnectionIsSimplyIdle()
    {
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        await using var concurrencyService = CreateConcurrencyService();
        var client = CreateClient(concurrencyService);

        try
        {
            await client.ConnectAsync(new WsEndpointResult { Url = server.Url });

            // 先投一帧，让 IdleMs 有样本
            await server.SendTextAsync("{\"type\":\"probe\",\"seq\":0}");
            await WaitUntilAsync(() => client.Liveness.LastReceiveUtc.HasValue, "必须记录最近收帧时刻");

            // Act：静默 3 秒（不发任何帧）
            // 注意：静默时长用 Stopwatch 实测，而 IdleMs 由 DateTime.UtcNow 差值计算——
            // Task.Delay 基于计时器节拍，相对 UtcNow 可提前约 1ms 触发
            // （ubuntu-latest 上实测出现过 IdleMs=2999 ⇒ 不得断言 IdleMs >= 3000）。
            var idleStopwatch = Stopwatch.StartNew();
            await Task.Delay(TimeSpan.FromSeconds(3));

            // 只取一次快照：多次读取会各自重算 UtcNow，无法作为同一事实的两个观测点
            var liveness = client.Liveness;

            // Assert（结构性断言优先，计时断言仅作量级守护且带容差）
            liveness.ReceiveLoopAlive.Should().BeTrue("静默期间接收循环仍应在运行");
            liveness.IsZombie.Should().BeFalse(
                "空闲不等于僵尸：只有'State==Open 且接收循环已结束'才允许判死");
            client.IsConnected.Should().BeTrue();

            liveness.LastReceiveUtc.Should().NotBeNull();
            liveness.IdleMs.Should().BeGreaterThanOrEqualTo(0,
                "-1 表示尚无收帧样本；这里已经收到过一帧，必须有样本");
            liveness.IdleMs.Should().BeGreaterThanOrEqualTo(
                (long)idleStopwatch.Elapsed.TotalMilliseconds - 100,
                "IdleMs 必须如实反映静默时长（容差 100ms 吸收计时器节拍/时钟粒度差异；" +
                "若该值恒为 0 或远小于实测静默时长，说明收帧时间戳没有被刷新）");
        }
        finally
        {
            await client.DisposeAsync();
        }
    }
}

#endif
