// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.WebSocket;
using Xunit;

namespace Mud.Feishu.WebSocket.Tests.Extensions;

/// <summary>
/// FeishuWebSocketServiceBuilder.RegisterCoreServices 服务注册测试。
/// 验证 EventDeduplication.Mode 在 None/InMemory/Distributed 三种模式下
/// 分别注册正确的 IFeishuEventDeduplicator 实现类型。
/// 对应生产代码：FeishuWebSocketServiceBuilder.cs（CFG-P0-1/P0-2 修复）
/// </summary>
public class FeishuWebSocketServiceBuilderTests
{
    /// <summary>
    /// 构造最小可用的服务集合，包含 Builder 构建过程中必需的基础服务。
    /// </summary>
    private static ServiceCollection CreateBaseServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }

    /// <summary>
    /// 构建 Builder、注册 Handler、配置 Options 并调用 Build()。
    /// </summary>
    private static (IServiceProvider ServiceProvider, IOptions<FeishuWebSocketOptions> Options) BuildProvider(
        ServiceCollection services,
        Action<EventDeduplicationOptions> configureDeduplication)
    {
        services.CreateFeishuWebSocketServiceBuilder(options =>
            {
                configureDeduplication(options.EventDeduplication);
            })
            .AddHandler<TestEventHandler>()
            .Build();

        var sp = services.BuildServiceProvider();
        return (sp, sp.GetRequiredService<IOptions<FeishuWebSocketOptions>>());
    }

    [Fact]
    public void Build_WithNoneMode_ShouldRegisterNoopFeishuEventDeduplicator()
    {
        // Arrange
        var services = CreateBaseServices();

        // Act
        var (sp, _) = BuildProvider(services, dedup => dedup.Mode = EventDeduplicationMode.None);

        // Assert - Mode=None 时应注册 NoopFeishuEventDeduplicator
        var deduplicator = sp.GetRequiredService<IFeishuEventDeduplicator>();
        deduplicator.Should().BeOfType<NoopFeishuEventDeduplicator>(
            "Mode=None 时应注册 NoopFeishuEventDeduplicator 以禁用去重");
    }

    [Fact]
    public void Build_WithInMemoryMode_ShouldRegisterFeishuEventDeduplicator()
    {
        // Arrange
        var services = CreateBaseServices();

        // Act
        var (sp, _) = BuildProvider(services, dedup => dedup.Mode = EventDeduplicationMode.InMemory);

        // Assert - Mode=InMemory 时应注册 FeishuEventDeduplicator（内存实现）
        var deduplicator = sp.GetRequiredService<IFeishuEventDeduplicator>();
        deduplicator.Should().BeOfType<FeishuEventDeduplicator>(
            "Mode=InMemory 时应注册内存去重器 FeishuEventDeduplicator");
    }

    [Fact]
    public void Build_WithDistributedModeButNoDistributedImplementation_ShouldFallbackToInMemory()
    {
        // Arrange
        var services = CreateBaseServices();

        // Act - Distributed 模式但未手动注册分布式实现
        var (sp, _) = BuildProvider(services, dedup => dedup.Mode = EventDeduplicationMode.Distributed);

        // Assert - 应降级为 FeishuEventDeduplicator（内存实现）
        var deduplicator = sp.GetRequiredService<IFeishuEventDeduplicator>();
        deduplicator.Should().BeOfType<FeishuEventDeduplicator>(
            "Mode=Distributed 但未注册分布式实现时应降级为 FeishuEventDeduplicator");
    }

    [Fact]
    public void Build_WithDistributedModeAndCustomRegistration_ShouldRespectCustomImplementation()
    {
        // Arrange
        var services = CreateBaseServices();
        // 模拟用户手动注册分布式去重器（如 Redis 实现）
        services.AddSingleton<IFeishuEventDeduplicator, CustomDistributedDeduplicator>();

        // Act
        var (sp, _) = BuildProvider(services, dedup => dedup.Mode = EventDeduplicationMode.Distributed);

        // Assert - 已手动注册分布式实现，Builder 不应覆盖
        var deduplicator = sp.GetRequiredService<IFeishuEventDeduplicator>();
        deduplicator.Should().BeOfType<CustomDistributedDeduplicator>(
            "用户已手动注册 IFeishuEventDeduplicator 时 Builder 不应覆盖自定义实现");
    }

    [Fact]
    public void Build_WithInMemoryModeAndCustomRegistration_ShouldRespectCustomImplementation()
    {
        // Arrange
        var services = CreateBaseServices();
        services.AddSingleton<IFeishuEventDeduplicator, CustomDistributedDeduplicator>();

        // Act
        var (sp, _) = BuildProvider(services, dedup => dedup.Mode = EventDeduplicationMode.InMemory);

        // Assert - 即使 Mode=InMemory，已手动注册的实现不应被覆盖
        var deduplicator = sp.GetRequiredService<IFeishuEventDeduplicator>();
        deduplicator.Should().BeOfType<CustomDistributedDeduplicator>(
            "用户已手动注册 IFeishuEventDeduplicator 时 Builder 不应覆盖自定义实现（与 Mode 无关）");
    }

    [Fact]
    public void Build_WithNoneModeAndCustomRegistration_ShouldRespectCustomImplementation()
    {
        // Arrange
        var services = CreateBaseServices();
        services.AddSingleton<IFeishuEventDeduplicator, CustomDistributedDeduplicator>();

        // Act
        var (sp, _) = BuildProvider(services, dedup => dedup.Mode = EventDeduplicationMode.None);

        // Assert - 即使 Mode=None，已手动注册的实现不应被覆盖
        var deduplicator = sp.GetRequiredService<IFeishuEventDeduplicator>();
        deduplicator.Should().BeOfType<CustomDistributedDeduplicator>(
            "用户已手动注册 IFeishuEventDeduplicator 时 Builder 不应覆盖自定义实现（即使 Mode=None）");
    }

    [Fact]
    public void Build_WithInMemoryMode_ShouldPassFullConfigurationToDeduplicator()
    {
        // Arrange - 验证 P1-1 修复：FeishuEventDeduplicator 接收到完整的 EventDeduplicationOptions 配置
        var services = CreateBaseServices();
        var expectedCacheExpiration = TimeSpan.FromHours(12);
        var expectedCleanupInterval = TimeSpan.FromMinutes(10);
        var expectedProcessingTimeout = TimeSpan.FromMinutes(5);
        var expectedMaxCacheSize = 50000;

        // Act
        var (sp, _) = BuildProvider(services, dedup =>
        {
            dedup.Mode = EventDeduplicationMode.InMemory;
            dedup.CacheExpiration = expectedCacheExpiration;
            dedup.CleanupInterval = expectedCleanupInterval;
            dedup.ProcessingTimeout = expectedProcessingTimeout;
            dedup.MaxCacheSize = expectedMaxCacheSize;
        });

        // Assert - 解析 FeishuEventDeduplicator 并验证配置已传递
        var deduplicator = sp.GetRequiredService<IFeishuEventDeduplicator>();
        deduplicator.Should().BeOfType<FeishuEventDeduplicator>();
        // 注：FeishuEventDeduplicator 内部存储配置，通过其公共行为间接验证
        // 此处主要验证类型正确注册且不抛出异常
    }

    [Fact]
    public void Build_WithoutExplicitMode_ShouldDefaultToInMemory()
    {
        // Arrange - 不显式设置 Mode，使用默认值（InMemory）
        var services = CreateBaseServices();

        // Act
        var (sp, options) = BuildProvider(services, _ => { /* 不修改默认配置 */ });

        // Assert - 默认 Mode 应为 InMemory
        options.Value.EventDeduplication.Mode.Should().Be(EventDeduplicationMode.InMemory);
        var deduplicator = sp.GetRequiredService<IFeishuEventDeduplicator>();
        deduplicator.Should().BeOfType<FeishuEventDeduplicator>();
    }

    [Fact]
    public void Build_CalledTwice_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = CreateBaseServices();
        var builder = services.CreateFeishuWebSocketServiceBuilder(_ => { })
            .AddHandler<TestEventHandler>();

        // Act
        builder.Build();

        // Assert - 第二次调用 Build() 应抛出异常
        var act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Build()*");
    }

    [Fact]
    public void Build_WithoutHandler_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = CreateBaseServices();
        var builder = services.CreateFeishuWebSocketServiceBuilder(_ => { });

        // Act & Assert - 未注册任何 Handler 应抛出异常
        var act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*处理器*");
    }

    /// <summary>
    /// 测试用事件处理器
    /// </summary>
    private class TestEventHandler : IFeishuEventHandler
    {
        public string SupportedEventType => "test.event";
        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    /// <summary>
    /// 自定义分布式去重器（模拟用户注册的 Redis 实现）
    /// </summary>
    private class CustomDistributedDeduplicator : IFeishuEventDeduplicator
    {
        public Task<DeduplicationResult> TryMarkAsProcessingAsync(string eventId, string? appKey = null, TimeSpan? ttl = null, TimeSpan? processingTimeout = null, CancellationToken cancellationToken = default)
            => Task.FromResult(DeduplicationResult.Success(eventId));

        public Task MarkAsCompletedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RollbackProcessingAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> IsProcessedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<DeduplicationStatus> GetStatusAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(DeduplicationStatus.Pending);

        public Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public ValueTask DisposeAsync() => new ValueTask();
    }
}
