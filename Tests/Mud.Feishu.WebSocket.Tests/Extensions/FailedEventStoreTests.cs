// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Interceptors;
using Mud.Feishu.WebSocket.Handlers;

namespace Mud.Feishu.WebSocket.Tests.Extensions;

/// <summary>
/// R-E1/E-P1-3：WS 接入 IFailedEventStore（阶段一：仅落盘 + 指标）测试
/// </summary>
public class FailedEventStoreTests
{
    private readonly Mock<ILogger<FeishuEventMessageHandler>> _loggerMock = new();
    private readonly Mock<IFeishuEventHandlerFactory> _handlerFactoryMock = new();

    // ===== 建造者注册语义 =====

    [Fact]
    public void AddFailedEventStore_Generic_ShouldRegisterSingleton()
    {
        var services = new ServiceCollection();
        services.CreateFeishuWebSocketServiceBuilder(_ => { })
            .AddHandler<TestEventHandler>()
            .AddFailedEventStore<TestFailedEventStore>()
            .Build();

        using var sp = services.BuildServiceProvider();

        sp.GetService<IFailedEventStore>().Should().BeOfType<TestFailedEventStore>();
    }

    [Fact]
    public void AddFailedEventStore_Instance_ShouldRegisterSingleton()
    {
        var services = new ServiceCollection();
        var store = new TestFailedEventStore();
        services.CreateFeishuWebSocketServiceBuilder(_ => { })
            .AddHandler<TestEventHandler>()
            .AddFailedEventStore(store)
            .Build();

        using var sp = services.BuildServiceProvider();

        sp.GetService<IFailedEventStore>().Should().BeSameAs(store);
    }

    [Fact]
    public void AddFailedEventStore_ShouldNotOverrideHostRegistration_TryAddSemantics()
    {
        var services = new ServiceCollection();
        var hostStore = new TestFailedEventStore();
        services.AddSingleton<IFailedEventStore>(hostStore);

        services.CreateFeishuWebSocketServiceBuilder(_ => { })
            .AddHandler<TestEventHandler>()
            .AddFailedEventStore<TestFailedEventStore>()
            .Build();

        using var sp = services.BuildServiceProvider();

        sp.GetService<IFailedEventStore>().Should().BeSameAs(hostStore,
            "TryAdd 语义：宿主已注册时建造者不得覆盖");
    }

    [Fact]
    public void WithoutAddFailedEventStore_ShouldNotResolveStore_ZeroBehaviorChange()
    {
        var services = new ServiceCollection();
        services.CreateFeishuWebSocketServiceBuilder(_ => { })
            .AddHandler<TestEventHandler>()
            .Build();

        using var sp = services.BuildServiceProvider();

        sp.GetService<IFailedEventStore>().Should().BeNull(
            "WS 不默认注册失败事件存储（与 Webhook 不同），未注册时行为与未引入前一致");
    }

    // ===== 处理器失败路径落盘 =====

    [Fact]
    public async Task HandleAsync_WhenBusinessFails_ShouldStoreFailedEvent_AndStillRethrow()
    {
        var storeMock = new Mock<IFailedEventStore>();
        _handlerFactoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("业务失败"));

        var handler = CreateHandler(storeMock.Object);

        var act = () => handler.HandleAsync(V2Message("evt_store_fail"));

        // ACK 语义不变：异常必须照常传播（服务端重发主路径不受落盘影响）
        await act.Should().ThrowAsync<InvalidOperationException>();

        storeMock.Verify(
            s => s.StoreFailedEventAsync(
                It.Is<EventData>(e => e.EventId == "evt_store_fail"),
                It.Is<InvalidOperationException>(ex => ex.Message == "业务失败"),
                "default",
                It.Is<DateTimeOffset>(t => t > DateTimeOffset.UtcNow.AddSeconds(5)),
                It.IsAny<CancellationToken>()),
            Times.Once, "业务失败必须落盘（NextRetryAt = now + 初始延迟）");
    }

    [Fact]
    public async Task HandleAsync_WhenStoreThrows_ShouldNotReplaceOriginalException()
    {
        var storeMock = new Mock<IFailedEventStore>();
        storeMock
            .Setup(s => s.StoreFailedEventAsync(It.IsAny<EventData>(), It.IsAny<Exception>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("存储故障"));

        _handlerFactoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("原始业务异常"));

        var handler = CreateHandler(storeMock.Object);

        var act = () => handler.HandleAsync(V2Message("evt_store_broken"));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Be("原始业务异常", "落盘失败不得替换/吞没原始业务异常（WHF-07 口径）");
    }

    [Fact]
    public async Task HandleAsync_WhenCancelled_ShouldNotStoreFailedEvent()
    {
        var storeMock = new Mock<IFailedEventStore>();
        _handlerFactoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("外部取消"));

        var handler = CreateHandler(storeMock.Object);

        var act = () => handler.HandleAsync(V2Message("evt_cancel_nostore"));

        await act.Should().ThrowAsync<OperationCanceledException>();

        storeMock.Verify(
            s => s.StoreFailedEventAsync(It.IsAny<EventData>(), It.IsAny<Exception>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never, "取消路径不落盘（服务端必然重发，落盘是噪音）");
    }

    [Fact]
    public async Task HandleAsync_WhenInterceptorBlocks_ShouldNotStoreFailedEvent()
    {
        var storeMock = new Mock<IFailedEventStore>();
        var interceptorMock = new Mock<IFeishuEventInterceptor>();
        interceptorMock
            .Setup(i => i.BeforeHandleAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler(storeMock.Object, [interceptorMock.Object]);

        var act = () => handler.HandleAsync(V2Message("evt_intercept_nostore"));

        await act.Should().ThrowAsync<EventHandlingOutcomeException>();

        storeMock.Verify(
            s => s.StoreFailedEventAsync(It.IsAny<EventData>(), It.IsAny<Exception>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never, "拦截终态不落盘（重发后拦截器重新决策）");
    }

    [Fact]
    public async Task HandleAsync_WithoutStore_ShouldNotThrow_AndProcessNormally()
    {
        // 零破坏：未注册存储时行为与未引入前一致
        _handlerFactoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(null);

        var act = () => handler.HandleAsync(V2Message("evt_no_store"));

        await act.Should().NotThrowAsync();
        _handlerFactoryMock.Verify(
            f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void FailedEventInitialRetryDelaySeconds_NonPositive_ShouldFallBackToDefault()
    {
        var options = new FeishuWebSocketOptions { FailedEventInitialRetryDelaySeconds = 0 };

        options.FailedEventInitialRetryDelaySeconds.Should().Be(10,
            "非正数回退 Consts.DefaultEventRetryInitialRetryDelaySeconds（默认 10）");
    }

    // ===== 辅助 =====

    private FeishuEventMessageHandler CreateHandler(IFailedEventStore? store, IFeishuEventInterceptor[]? interceptors = null)
    {
        return new FeishuEventMessageHandler(
            _loggerMock.Object,
            _handlerFactoryMock.Object,
            null,
            interceptors,
            new FeishuWebSocketOptions { AppKey = "default" },
            null,
            store);
    }

    private static string V2Message(string eventId) =>
        $"{{\"schema\":\"2.0\",\"header\":{{\"event_id\":\"{eventId}\",\"event_type\":\"drive.file.edit_v1\",\"tenant_key\":\"tk\",\"app_id\":\"app\"}},\"event\":{{}}}}";

    private class TestEventHandler : IFeishuEventHandler
    {
        public string SupportedEventType => "test.event";

        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private class TestFailedEventStore : IFailedEventStore
    {
        public Task StoreFailedEventAsync(EventData eventData, Exception exception, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task StoreFailedEventAsync(EventData eventData, Exception exception, string? appKey, DateTimeOffset nextRetryAt, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IEnumerable<FailedEventInfo>> GetFailedEventsForRetryAsync(int maxRetryCount, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<FailedEventInfo>>([]);

        public Task<List<FailedEventInfo>> GetPendingRetryEventsAsync(DateTimeOffset beforeTime, int maxCount, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<FailedEventInfo>());

        public Task UpdateRetryCountAsync(string eventId, int retryCount, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateFailedEventAsync(FailedEventInfo eventInfo, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveFailedEventAsync(string eventId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
