// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions;

namespace Mud.Feishu.Webhook.Tests.ContractGuards;

/// <summary>
/// W-05：实例化注册（<c>AddHandler(instance)</c> / <c>AddInterceptor(instance)</c>）的生命周期契约。
/// </summary>
/// <remarks>
/// <para>
/// <b>缺陷形态</b>：以 <c>AddScoped&lt;T&gt;(_ =&gt; instance)</c> 注册共享实例时，DI 会对该实例执行
/// <c>CaptureDisposable</c>——<b>首个请求的 scope 结束就把它 Dispose 掉</b>，后续请求拿到已释放对象
/// （处理器实现 <see cref="IDisposable"/> 时症状最明显：ObjectDisposedException 或静默失效）。
/// </para>
/// <para>
/// <b>契约</b>：实例注册的语义只能是"进程级共享（所有权移交容器）"，因此必须是 Singleton；
/// 需要按请求隔离状态的调用方应改用类型注册 <c>AddHandler&lt;T&gt;()</c>。
/// </para>
/// <para>
/// 本守卫只调用 <c>AddHandler</c>/<c>AddInterceptor</c>（它们立即写入 <see cref="IServiceCollection"/>），
/// <b>不调用 <c>Build()</c></b>——后者会引入启动校验、D1 生产锁等与被测契约无关的装配面。
/// </para>
/// </remarks>
public class InstanceRegistrationLifetimeContractGuards
{
    private static ServiceCollection CreateServices() => new();

    [Fact]
    public void AddHandler_WithInstance_ShouldRegisterSingletonDescriptors()
    {
        var services = CreateServices();
        var handler = new DisposableCountingHandler();

        services.CreateFeishuWebhookServiceBuilder(_ => { }).AddHandler(handler);

        var interfaceDescriptors = services
            .Where(d => d.ServiceType == typeof(IFeishuEventHandler) && ReferenceEquals(d.ImplementationInstance, handler))
            .ToList();

        interfaceDescriptors.Should().NotBeEmpty("实例必须注册到 IFeishuEventHandler（否则处理器工厂解析不到）");
        interfaceDescriptors.Should().OnlyContain(d => d.Lifetime == ServiceLifetime.Singleton,
            "实例注册必须是 Singleton：Scoped 会在首个请求结束时释放共享实例（W-05）");

        var selfDescriptors = services
            .Where(d => d.ServiceType == typeof(DisposableCountingHandler) && ReferenceEquals(d.ImplementationInstance, handler))
            .ToList();

        selfDescriptors.Should().NotBeEmpty("实例也必须以自身类型注册（GetRequiredService(handlerType) 路径）");
        selfDescriptors.Should().OnlyContain(d => d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddInterceptor_WithInstance_ShouldRegisterSingletonDescriptors()
    {
        var services = CreateServices();
        var interceptor = new CountingInterceptor();

        services.CreateFeishuWebhookServiceBuilder(_ => { }).AddInterceptor(interceptor);

        var descriptors = services
            .Where(d => d.ServiceType == typeof(IFeishuEventInterceptor) && ReferenceEquals(d.ImplementationInstance, interceptor))
            .ToList();

        descriptors.Should().NotBeEmpty();
        descriptors.Should().OnlyContain(d => d.Lifetime == ServiceLifetime.Singleton,
            "拦截器实例注册同样是进程级共享（W-05）");
    }

    [Fact]
    public void AddHandler_WithInstance_ShouldSurviveScopeDisposal()
    {
        // 行为级复现：scoped 注册下，第一个 scope 释放即 Dispose 掉共享实例。
        var services = CreateServices();
        var handler = new DisposableCountingHandler();
        services.CreateFeishuWebhookServiceBuilder(_ => { }).AddHandler(handler);

        using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IFeishuEventHandler>()
                .Should().BeSameAs(handler);
        }

        handler.DisposeCount.Should().Be(0,
            "请求 scope 结束不得释放共享实例（Scoped 注册的缺陷正是这里被 Dispose）");

        using (var scope2 = provider.CreateScope())
        {
            scope2.ServiceProvider.GetRequiredService<IFeishuEventHandler>()
                .Should().BeSameAs(handler, "再次解析必须仍是同一可用实例");
        }
    }

    [Fact]
    public void MiddlewareTypes_ShouldNotBeRegisteredInContainer()
    {
        // W-09：UseMiddleware 恒以 ActivatorUtilities 构造管道实例，容器注册会制造"幻影实例"
        //（重复的 OnChange 订阅 + 孤儿 Timer），故不得注册。
        var services = CreateServices();
        services.CreateFeishuWebhookServiceBuilder(_ => { }).AddHandler(new CountingHandler());

        services.Should().NotContain(d => d.ServiceType == typeof(FeishuMultiAppMiddleware),
            "中间件由 UseMiddleware 创建，容器注册只会产生第二个实例（W-09）");
        services.Should().NotContain(d => d.ServiceType == typeof(FeishuRateLimitMiddleware),
            "同上：幻影实例会额外持有一份配置变更订阅（W-09）");
    }

    // ────────────────────────────────────────────────────────────────────
    // 测试替身
    // ────────────────────────────────────────────────────────────────────

    private sealed class DisposableCountingHandler : IFeishuEventHandler, IDisposable
    {
        public int DisposeCount { get; private set; }

        public string SupportedEventType => string.Empty;

        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Dispose() => DisposeCount++;
    }

    private sealed class CountingHandler : IFeishuEventHandler
    {
        public string SupportedEventType => string.Empty;

        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class CountingInterceptor : IFeishuEventInterceptor
    {
        public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
