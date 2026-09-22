// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// P1-5 / P1-5b 回归测试：释放语义的确定性（入口守卫 + 信号量不释放，I9）。
/// </summary>
/// <remarks>
/// 改造前：<c>Dispose()</c> 会释放内部 <c>SemaphoreSlim</c>，而在途的 <c>Release()</c> / 租约归还
/// 会对已释放信号量抛出 <see cref="ObjectDisposedException"/>；同时公开 API 在 Dispose 后
/// 表现为"随机抛异常或偶发成功"。
/// <para>
/// 改造后：① 公开 API 入口确定性抛 <see cref="ObjectDisposedException"/>；
/// ② 信号量一律不释放（未访问 <c>AvailableWaitHandle</c>，无 OS 句柄泄漏），彻底消除释放竞态。
/// </para>
/// </remarks>
[Trait("Category", "Concurrency")]
public class DisposeRaceTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();

    public DisposeRaceTests()
    {
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);
    }

    private WebSocketConnectionManager CreateManager()
        => new(NullLogger<WebSocketConnectionManager>.Instance,
            new FeishuWebSocketOptions(),
            _loggerFactoryMock.Object);

    #region P1-5：入口守卫

    [Fact]
    public async Task ConnectAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // 说明：URL 使用默认白名单内的主机（*.feishu.cn），确保异常来自"已释放"守卫
        // 而非 P2-15 的主机白名单校验（后者先于入口守卫执行）
        var manager = CreateManager();
        manager.Dispose();

        var act = () => manager.ConnectAsync("wss://gateway.feishu.cn/ws");

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task DisconnectAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        var manager = CreateManager();
        manager.Dispose();

        var act = () => manager.DisconnectAsync();

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task SendMessageAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        var manager = CreateManager();
        await manager.DisposeAsync();

        var act = () => manager.SendMessageAsync("hello");

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task SendBinaryMessageAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        var manager = CreateManager();
        await manager.DisposeAsync();

        var act = () => manager.SendBinaryMessageAsync(new byte[] { 1, 2, 3 });

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent_WhenCalledConcurrently()
    {
        var manager = CreateManager();

        var act = () => Parallel.For(0, 32, _ => manager.Dispose());

        act.Should().NotThrow();
    }

    #endregion

    #region P1-5：二进制处理器

    [Fact]
    public async Task ProcessBinaryDataAsync_AfterDispose_ShouldReturnWithoutThrowing()
    {
        // Arrange
        var options = new FeishuWebSocketOptions();
        var manager = new WebSocketConnectionManager(
            NullLogger<WebSocketConnectionManager>.Instance, options, _loggerFactoryMock.Object);
        var router = new MessageRouter(NullLogger<MessageRouter>.Instance, options);
        var processor = new BinaryMessageProcessor(
            NullLogger<BinaryMessageProcessor>.Instance, manager, options, router);

        processor.Dispose();

        // Act：Dispose 之后不得再访问已释放/正在释放的内部状态
        var act = () => processor.ProcessBinaryDataAsync(new byte[] { 1, 2, 3, 4 }, 0, 4, true);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region P1-5b：并发服务（租约归还不得因信号量被释放而抛异常）

    /// <summary>
    /// 构造使用真实 OptionsMonitor 的并发服务（<c>OnChange</c> 是扩展方法，无法用 Moq 设置）。
    /// </summary>
    private static FeishuWebSocketConcurrencyService CreateConcurrencyService(int maxConcurrent)
    {
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(o => o.MaxConcurrentHandlers = maxConcurrent);
        var optionsMonitor = services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        return new FeishuWebSocketConcurrencyService(
            optionsMonitor, NullLogger<FeishuWebSocketConcurrencyService>.Instance);
    }

    [Fact]
    public async Task ConcurrencyService_LeaseReleaseAfterDispose_ShouldNotThrow()
    {
        // Arrange
        var service = CreateConcurrencyService(maxConcurrent: 2);
        var lease = await service.AcquireAsync();

        // Act：先释放服务，再归还在途租约（此前会对已释放信号量 Release → ObjectDisposedException）
        await service.DisposeAsync();
        var act = () => lease.Dispose();

        // Assert
        act.Should().NotThrow("I9：信号量不随 Dispose 释放，在途租约归还必须安全");
    }

    [Fact]
    public async Task ConcurrencyService_AcquireAfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange：AcquireAsync 在 Dispose 后访问已释放的 shutdown CTS，按契约抛 ObjectDisposedException；
        // 调用方（FeishuWebSocketClient.AcquireConcurrencyLeaseAsync）会捕获并丢弃该帧，不污染接收循环。
        var service = CreateConcurrencyService(maxConcurrent: 2);
        await service.DisposeAsync();

        // Act
        var act = () => service.AcquireAsync();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task ConcurrencyService_DisposeAsync_ShouldBeIdempotent()
    {
        var service = CreateConcurrencyService(maxConcurrent: 1);

        var act = async () =>
        {
            await service.DisposeAsync();
            await service.DisposeAsync();
        };

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region WS2-06 ② / I9 例外：旧信号量延迟释放阈值的动态公式

    /// <summary>
    /// 阈值公式必须与 Webhook 侧一致：<c>max(60s, 2 × MessageHandlerTimeoutMs)</c>。
    /// </summary>
    /// <remarks>
    /// 改造前是硬编码 60 秒：当 <c>MessageHandlerTimeoutMs &gt; 30s</c> 时"租约最长合法持有时间"
    /// 超过 60 秒，慢处理器在热更新后归还租约时会命中已释放的信号量并抛
    /// <see cref="ObjectDisposedException"/>——配置越合理越容易触发。
    /// <para>
    /// 之所以把公式抽成 <c>internal static</c> 纯函数：**测试无法在运行期触发
    /// <c>IOptionsMonitor.OnChange</c> 回调**（现有 <c>CreateConcurrencyService</c> 用
    /// <c>ServiceCollection + Configure</c> 构造的 Monitor 没有可变的配置源）。
    /// 用纯函数覆盖公式 + 既有"归还租约不抛 ODE"用例做时序兜底，二者组合才是可落地的方案
    /// （强行伪造热更新回调会引入 flaky）。
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(30000, 60)]      // 2×30s = 60s，恰好等于下限
    [InlineData(120000, 240)]    // 2×120s = 240s > 60s → 取动态值
    [InlineData(45000, 90)]      // 2×45s = 90s
    [InlineData(0, 60)]          // 0 = 不限制处理时长 → 无法推导上界，回落下限
    [InlineData(-1, 60)]         // 负值同样回落下限
    [InlineData(1000, 60)]       // 2×1s = 2s < 60s → 取下限（保持改造前行为）
    public void ResolveLegacySemaphoreRetention_ShouldFollowWebhookFormula(int messageHandlerTimeoutMs, int expectedSeconds)
    {
        var retention = FeishuWebSocketConcurrencyService.ResolveLegacySemaphoreRetention(messageHandlerTimeoutMs);

        retention.Should().Be(TimeSpan.FromSeconds(expectedSeconds),
            "I9 例外登记：旧信号量保留时长必须 = max(60s, 2 × MessageHandlerTimeoutMs)（与 Webhook 侧公式一致），" +
            "使'租约最长合法持有时间'与'信号量回收时机'重新绑定");
    }

    [Fact]
    public void ResolveLegacySemaphoreRetention_ShouldAlwaysBeAtLeastSixtySeconds()
    {
        foreach (var timeout in new[] { int.MinValue, -1, 0, 1, 29_999, 30_000 })
        {
            FeishuWebSocketConcurrencyService.ResolveLegacySemaphoreRetention(timeout)
                .Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(60),
                    "下限保证短超时配置下的行为与改造前完全一致（不引入新的资源滞留差异）");
        }
    }

    #endregion
}
