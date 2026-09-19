// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// P0-1 回归测试：重连事件的"锁外派发 + 闸门快速失败"语义。
/// </summary>
/// <remarks>
/// 改造前 <see cref="ReconnectionOrchestrator.TryReconnectAsync"/> 在持有
/// <c>_reconnectLock</c>（不可重入）期间同步触发用户事件，订阅者若在回调内<b>阻塞等待</b>重入调用
/// 即永久挂起，且 <c>finally</c> 无法执行 → 闸门/锁永久占用，重连彻底失效。
/// <para>
/// 本类同时守卫 §0.2 M1：闸门持有期内派发事件，订阅者重入只能"快速返回 false"，
/// 不得开启嵌套重连轮次。
/// </para>
/// </remarks>
[Trait("Category", "Concurrency")]
public class ReconnectionOrchestratorReentrancyTests
{
    private readonly Mock<ILogger<ReconnectionOrchestrator>> _loggerMock = new();
    private readonly Mock<IReconnectStrategy> _strategyMock = new();
    private readonly Mock<IFeishuWebSocketManager> _managerMock = new();

    /// <summary>
    /// 构造配置。<paramref name="reconnectCooldownTime"/> 默认 0（免冷却），
    /// 以便"闸门"成为唯一能阻止重入的机制，否则用例可能因冷却期而"假绿"。
    /// </summary>
    private static FeishuWebSocketOptions CreateOptions(TimeSpan? reconnectCooldownTime = null) => new()
    {
        AutoReconnect = true,
        MaxReconnectAttempts = 5,
        ReconnectDelayMs = 1000,
        MaxReconnectDelayMs = 1000,
        MaxTotalReconnectTime = TimeSpan.FromMinutes(30),
        ReconnectCooldownTime = reconnectCooldownTime ?? TimeSpan.Zero,
        EnableLogging = false
    };

    private ReconnectionOrchestrator CreateOrchestrator(FeishuWebSocketOptions? options = null)
        => new(_loggerMock.Object, _strategyMock.Object, _managerMock.Object, options ?? CreateOptions());

    private void SetupAlwaysSucceed(bool connected = true)
    {
        _strategyMock.Setup(x => x.ShouldContinueReconnect(It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(true);
        _strategyMock.Setup(x => x.CalculateDelay(It.IsAny<int>()))
            .Returns(TimeSpan.FromMilliseconds(1));
        _managerMock.Setup(x => x.ReconnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _managerMock.Setup(x => x.IsConnected).Returns(connected);
    }

    /// <summary>
    /// P0-1 核心用例：订阅者在 ReconnectSucceeded 回调内<b>阻塞等待</b>重入调用，不得死锁。
    /// </summary>
    [Fact]
    public async Task TryReconnectAsync_ShouldNotDeadlock_WhenSucceededHandlerReenters()
    {
        // Arrange
        SetupAlwaysSucceed();
        var orchestrator = CreateOrchestrator();

        bool? nestedResult = null;
        orchestrator.ReconnectSucceeded += (_, _) =>
        {
            // 同步阻塞等待重入结果——改造前这里会永久挂起（锁不可重入），finally 永不执行
            nestedResult = orchestrator.TryReconnectAsync("nested-from-handler").GetAwaiter().GetResult();
        };

        // Act
        var outer = orchestrator.TryReconnectAsync("outer");
        var completed = await Task.WhenAny(outer, Task.Delay(TimeSpan.FromSeconds(3)));

        // Assert
        completed.Should().BeSameAs(outer, "事件回调内的重入不得造成死锁");
        (await outer).Should().BeTrue();
        nestedResult.Should().BeFalse("重连闸门持有期内重入必须立即返回 false");
    }

    /// <summary>
    /// P0-1：熔断（达到重连上限）回调内重入同样不得阻塞。
    /// </summary>
    [Fact]
    public async Task TryReconnectAsync_ShouldNotDeadlock_WhenLimitReachedHandlerReenters()
    {
        // Arrange：策略首次即判定"不再继续" → 触发 ReconnectLimitReached
        _strategyMock.Setup(x => x.ShouldContinueReconnect(It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(false);

        var orchestrator = CreateOrchestrator();

        bool? nestedResult = null;
        orchestrator.ReconnectLimitReached += (_, _) =>
        {
            nestedResult = orchestrator.TryReconnectAsync("nested-from-handler").GetAwaiter().GetResult();
        };

        // Act
        var outer = orchestrator.TryReconnectAsync("outer");
        var completed = await Task.WhenAny(outer, Task.Delay(TimeSpan.FromSeconds(3)));

        // Assert
        completed.Should().BeSameAs(outer, "熔断回调内的重入不得造成死锁");
        (await outer).Should().BeFalse();
        nestedResult.Should().BeFalse("熔断器已打开，重入必须立即返回 false（且不阻塞）");
    }

    /// <summary>
    /// M1 守卫：订阅者重入<b>不得</b>开启嵌套重连轮次（否则会在"已连上"状态下再调 ReconnectAsync）。
    /// </summary>
    [Fact]
    public async Task TryReconnectAsync_ShouldNotStartNestedRound_WhenSucceededHandlerReenters()
    {
        // Arrange
        SetupAlwaysSucceed();
        var orchestrator = CreateOrchestrator();

        orchestrator.ReconnectSucceeded += (_, _) =>
        {
            _ = orchestrator.TryReconnectAsync("nested-fire-and-forget");
        };

        // Act
        (await orchestrator.TryReconnectAsync("outer")).Should().BeTrue();
        await Task.Delay(200); // 给"嵌套轮次"足够时间暴露（若存在）

        // Assert
        _managerMock.Verify(x => x.ReconnectAsync(It.IsAny<CancellationToken>()), Times.Once,
            "闸门持有期内派发事件，重入不得开启嵌套重连轮次");
    }

    /// <summary>
    /// 闸门：一轮重连在途时，第 2 个调用必须"立即"返回 false（不排队等锁）。
    /// </summary>
    [Fact]
    public async Task TryReconnectAsync_ShouldReturnFalseImmediately_WhenAlreadyReconnecting()
    {
        // Arrange：第 1 次调用被 TCS 卡住，保持在途状态
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _strategyMock.Setup(x => x.ShouldContinueReconnect(It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(true);
        _strategyMock.Setup(x => x.CalculateDelay(It.IsAny<int>()))
            .Returns(TimeSpan.FromMilliseconds(1));
        _managerMock.Setup(x => x.ReconnectAsync(It.IsAny<CancellationToken>()))
            .Returns(gate.Task);
        _managerMock.Setup(x => x.IsConnected).Returns(true);

        var orchestrator = CreateOrchestrator();

        var first = orchestrator.TryReconnectAsync("first");
        await WaitUntilAsync(() => orchestrator.GetReconnectState().IsReconnecting, TimeSpan.FromSeconds(2));

        // Act
        var stopwatch = Stopwatch.StartNew();
        var second = await orchestrator.TryReconnectAsync("second");
        stopwatch.Stop();

        // Assert
        second.Should().BeFalse();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500),
            "重复请求必须快速失败而非排队等待锁");

        gate.SetResult(true);
        (await first).Should().BeTrue();
    }

    /// <summary>
    /// I12：<see cref="ReconnectState.IsReconnecting"/> 由闸门派生，在途期间必须为 true。
    /// </summary>
    [Fact]
    public async Task GetReconnectState_ShouldReportReconnecting_WhileRoundInFlight()
    {
        // Arrange
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _strategyMock.Setup(x => x.ShouldContinueReconnect(It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(true);
        _strategyMock.Setup(x => x.CalculateDelay(It.IsAny<int>()))
            .Returns(TimeSpan.FromMilliseconds(1));
        _managerMock.Setup(x => x.ReconnectAsync(It.IsAny<CancellationToken>()))
            .Returns(gate.Task);
        _managerMock.Setup(x => x.IsConnected).Returns(true);

        var orchestrator = CreateOrchestrator();
        var first = orchestrator.TryReconnectAsync("in-flight");

        // Act & Assert
        await WaitUntilAsync(() => orchestrator.GetReconnectState().IsReconnecting, TimeSpan.FromSeconds(2));

        gate.SetResult(true);
        await first;

        await WaitUntilAsync(() => !orchestrator.GetReconnectState().IsReconnecting, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// I11：事件回调抛异常时闸门不得泄漏（后续重连必须仍可进入）。
    /// </summary>
    [Fact]
    public async Task TryReconnectAsync_ShouldResetGate_WhenEventHandlerThrows()
    {
        // Arrange：冷却期为 0，第 2 次调用唯一可能被拦截的原因就是"闸门泄漏"
        SetupAlwaysSucceed();
        var orchestrator = CreateOrchestrator();

        orchestrator.ReconnectSucceeded += (_, _) => throw new InvalidOperationException("订阅者故障");

        // Act：两次调用都必须能进入重连（异常按既有语义向调用方传播）
        await Assert.ThrowsAsync<InvalidOperationException>(() => orchestrator.TryReconnectAsync("first"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => orchestrator.TryReconnectAsync("second"));

        // Assert
        orchestrator.GetReconnectState().IsReconnecting.Should().BeFalse();
        _managerMock.Verify(x => x.ReconnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(2),
            "闸门必须在异常路径被复位，否则第 2 次重连会被永久拒绝");
    }

    /// <summary>
    /// I11：管理器抛异常导致整轮失败时，闸门不得泄漏。
    /// </summary>
    [Fact]
    public async Task TryReconnectAsync_ShouldResetGate_WhenManagerThrows()
    {
        // Arrange：每轮"继续 → 失败 → 判定终止（触发熔断）"，两轮之间用 ResetReconnectCounter 清熔断
        var decisions = new Queue<bool>(new[] { true, false, true, false });
        _strategyMock.Setup(x => x.ShouldContinueReconnect(It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(() => decisions.Count > 0 && decisions.Dequeue());
        _strategyMock.Setup(x => x.CalculateDelay(It.IsAny<int>()))
            .Returns(TimeSpan.FromMilliseconds(1));
        _managerMock.Setup(x => x.ReconnectAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("连接失败"));
        _managerMock.Setup(x => x.IsConnected).Returns(false);

        var orchestrator = CreateOrchestrator();

        // Act：第 1 轮
        (await orchestrator.TryReconnectAsync("first")).Should().BeFalse();
        orchestrator.GetReconnectState().IsReconnecting.Should().BeFalse();

        // 模拟"连接成功"清除熔断，使第 2 轮仅受闸门约束
        orchestrator.ResetReconnectCounter();

        // 第 2 轮
        (await orchestrator.TryReconnectAsync("second")).Should().BeFalse();

        // Assert
        _managerMock.Verify(x => x.ReconnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(2),
            "闸门必须在失败轮次后被复位，否则第 2 轮会被永久拒绝（管理器的第 2 次调用不会发生）");
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > timeout)
                throw new TimeoutException("等待条件超时");

            await Task.Delay(10);
        }
    }
}
