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
using Mud.Feishu.Abstractions;
using Mud.Feishu.WebSocket.Handlers;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Mud.Feishu.WebSocket.Tests.Handlers;

/// <summary>
/// ScopedFeishuEventHandlerFactory 契约测试（M3-1 / M3-2）
/// </summary>
public class ScopedFeishuEventHandlerFactoryTests
{
    private sealed class DispatchProbeHandler : IFeishuEventHandler
    {
        public string SupportedEventType => "probe.dispatch";
        public int InvokeCount;

        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref InvokeCount);
            return Task.CompletedTask;
        }
    }

    private static (ScopedFeishuEventHandlerFactory Factory, DispatchProbeHandler Probe) CreateFactory(
        bool ignoreUnknown = false)
    {
        var probe = new DispatchProbeHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IFeishuEventHandler>(probe);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var factory = new ScopedFeishuEventHandlerFactory(
            NullLogger<ScopedFeishuEventHandlerFactory>.Instance,
            scopeFactory,
            handlerTypes: [],
            defaultHandlerType: null,
            handlerInstances: [probe],
            ignoreUnknownEventTypes: ignoreUnknown);
        return (factory, probe);
    }

    [Fact]
    public async Task RegisterHandler_ShouldNotAffectDispatch()
    {
        var (factory, probe) = CreateFactory();

        var runtime = new Mock<IFeishuEventHandler>();
        runtime.Setup(h => h.SupportedEventType).Returns("probe.dispatch");
        factory.RegisterHandler(runtime.Object);

        await factory.HandleEventParallelAsync("probe.dispatch", new EventData
        {
            EventId = "e1",
            EventType = "probe.dispatch"
        });

        // P2-2：运行期注册不得改变分发路径
        probe.InvokeCount.Should().Be(1);
        runtime.Verify(h => h.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventParallelAsync_ShouldSkipUnregisteredEvent_WhenGateEnabled()
    {
        var (factory, probe) = CreateFactory(ignoreUnknown: true);

        await factory.HandleEventParallelAsync("unknown.type", new EventData
        {
            EventId = "e2",
            EventType = "unknown.type"
        });

        // 未注册类型且门控开启：实例 probe 不应被调用（本工厂集合中无 unknown.type）
        // probe 注册的是 probe.dispatch；unknown.type 未注册 → 门控应短路
        // 注意：IsHandlerRegistered 基于 inspection/dispatch factory；unknown 未注册
        factory.IsHandlerRegistered("unknown.type").Should().BeFalse();
        probe.InvokeCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleEventParallelAsync_ShouldFallbackToDefaultHandler_WhenGateDisabled()
    {
        var probe = new DispatchProbeHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IFeishuEventHandler>(probe);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        // 门控关闭 + 仅注册 probe：工厂会把首个解析到的处理器当作默认兜底，
        // unknown.type 会回退到默认处理器（与 WS 历史行为一致）而不是静默忽略。
        var factory = new ScopedFeishuEventHandlerFactory(
            NullLogger<ScopedFeishuEventHandlerFactory>.Instance,
            scopeFactory,
            handlerTypes: [],
            defaultHandlerType: null,
            handlerInstances: [probe],
            ignoreUnknownEventTypes: false);

        var act = () => factory.HandleEventParallelAsync("unknown.type", new EventData
        {
            EventId = "e3",
            EventType = "unknown.type"
        });

        await act.Should().NotThrowAsync();
        probe.InvokeCount.Should().Be(1, "门控关闭时未知事件回退默认处理器（本工厂默认=唯一实例 probe）");
    }

    /// <summary>P1-3 测试替身：可变 CurrentValue 的 Options Monitor</summary>
    private sealed class FakeOptionsMonitor : IOptionsMonitor<FeishuWebSocketOptions>
    {
        private FeishuWebSocketOptions _value;
        public FakeOptionsMonitor(FeishuWebSocketOptions value) => _value = value;
        public FeishuWebSocketOptions CurrentValue => _value;
        public FeishuWebSocketOptions Get(string? name) => _value;
        public IDisposable? OnChange(Action<FeishuWebSocketOptions, string?> listener) => null;
        public void Set(FeishuWebSocketOptions value) => _value = value;
    }

    // P1-3（R2）：门控必须读 IOptionsMonitor 实时值，配置热更在同一工厂实例上生效；
    // 初始 false 阶段同时覆盖 E8（ScopedFactory Monitor 路径的 IgnoreUnknownFalse 回退）。
    [Fact]
    public async Task HandleEventParallelAsync_Gate_ShouldFollowOptionsHotUpdate_WithoutRecreatingFactory()
    {
        var probe = new DispatchProbeHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IFeishuEventHandler>(probe);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var options = new FeishuWebSocketOptions { AppKey = "ws-app-hot", IgnoreUnknownEventTypes = false };
        var monitor = new FakeOptionsMonitor(options);

        // bool 快照恒为 false，门控完全由 Monitor 实时值驱动
        var factory = new ScopedFeishuEventHandlerFactory(
            NullLogger<ScopedFeishuEventHandlerFactory>.Instance,
            scopeFactory,
            handlerTypes: [],
            defaultHandlerType: null,
            handlerInstances: [probe],
            ignoreUnknownEventTypes: false,
            optionsMonitor: monitor);

        // Phase 1（E8 等价用例）：Monitor 值 false → 未注册事件回退默认处理器
        await factory.HandleEventParallelAsync("unknown.type", new EventData
        {
            EventId = "e-hot-1",
            EventType = "unknown.type"
        });
        probe.InvokeCount.Should().Be(1, "Monitor 值 false 时未注册事件回退默认处理器");

        // Phase 2：热更把 IgnoreUnknownEventTypes 翻为 true → 同一工厂实例上
        // 下一次分发走 unhandled 路径（handler 0 次调用）
        options.IgnoreUnknownEventTypes = true;

        // P2-3：用 MeterListener 捕获 feishu.event.handling，断言 unhandled 维度为 options.AppKey。
        // Meter 为进程级静态实例，并行测试可能并发记录 → 用「存在匹配项」而非精确计数断言。
        // MeasurementEventCallback 的 tags 形状在 net8+ 为 ReadOnlySpan，与 net6/7 不同——
        // 指标断言仅在 NET8+ 编译（net6/7 只保留行为断言）。
#if NET8_0_OR_GREATER
        // 采集缓冲必须**线程安全**且在断言前取快照：Meter 是进程级静态实例，
        // 同程序集其它用例会在各自线程上并发记录同一仪表，回调也随之在那些线程上执行。
        // 用 List 采集会在断言枚举期间被并发追加，实测偶发
        // System.InvalidOperationException: Collection was modified（net8.0 门禁）。
        var matching = new ConcurrentQueue<KeyValuePair<string, object?>[]>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == Mud.Feishu.Abstractions.Metrics.FeishuMetrics.MeterName &&
                    instrument.Name == "feishu.event.handling")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };
        // 只通过 EnableMeasurementEvents 启用 "feishu.event.handling" 一个仪表，
        // 因此回调收到的测量值均属于该仪表，无需再按仪表名过滤；
        // 但需按本用例唯一 AppKey 过滤，隔离其它用例的并发测量（同 FeishuEventMessageHandlerTests）。
        listener.SetMeasurementEventCallback<long>((_, measurement, tags, _) =>
        {
            var copy = new KeyValuePair<string, object?>[tags.Length];
            var isOwnMeasurement = false;
            for (var i = 0; i < tags.Length; i++)
            {
                copy[i] = tags[i];
                if (tags[i].Key == Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.AppKey &&
                    (string?)tags[i].Value == options.AppKey)
                {
                    isOwnMeasurement = true;
                }
            }

            if (isOwnMeasurement)
                matching.Enqueue(copy);
        });
        listener.Start();
#endif

        await factory.HandleEventParallelAsync("unknown.type", new EventData
        {
            EventId = "e-hot-2",
            EventType = "unknown.type"
        });

        probe.InvokeCount.Should().Be(1, "热更为 true 后同一工厂实例的后续分发走 unhandled 路径");

#if NET8_0_OR_GREATER
        // 断言前取快照：ConcurrentQueue.ToArray() 在并发入队下是安全读取
        var matchingSnapshot = matching.ToArray();
        matchingSnapshot.Should().Contain(tags => tags.Any(t =>
                t.Key == Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.AppKey && (string?)t.Value == "ws-app-hot") &&
            tags.Any(t => t.Key == Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.ErrorType && (string?)t.Value == "unhandled"),
            "unhandled 指标维度必须为通道 AppKey（P2-3），而非事件的 app_id");
#endif
    }
}
