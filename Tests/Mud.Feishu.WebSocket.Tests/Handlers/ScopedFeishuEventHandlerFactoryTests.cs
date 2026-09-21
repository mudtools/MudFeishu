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
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.WebSocket.Handlers;

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
}
