// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Utils;

namespace Mud.Feishu.Webhook.Tests.Health;

/// <summary>
/// FeishuWebhookHealthCheck 单元测试
/// </summary>
public class FeishuWebhookHealthCheckTests
{
    private readonly Mock<IOptionsMonitor<FeishuWebhookOptions>> _optionsMock;
    private readonly FeishuWebhookOptions _options;

    public FeishuWebhookHealthCheckTests()
    {
        _optionsMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();

        _options = new FeishuWebhookOptions
        {
            MaxConcurrentEvents = 10,
            EventHandlingTimeoutMs = 30000
        };
    }

    private FeishuWebhookConcurrencyService CreateConcurrencyService(int maxConcurrent)
    {
        var opts = new FeishuWebhookOptions
        {
            MaxConcurrentEvents = maxConcurrent,
            EventHandlingTimeoutMs = 30000
        };
        _optionsMock.Setup(x => x.CurrentValue).Returns(opts);
        return new FeishuWebhookConcurrencyService(_optionsMock.Object, Mock.Of<ILogger<FeishuWebhookConcurrencyService>>());
    }

    [Fact]
    public async Task CheckHealthAsync_WithAvailableSlots_ShouldReturnHealthy()
    {
        // Arrange
        var concurrencyService = CreateConcurrencyService(10);

        var healthCheck = new FeishuWebhookHealthCheck(
            _optionsMock.Object,
            concurrencyService);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("max_concurrent_events");
        result.Data["max_concurrent_events"].Should().Be(10);
        result.Data.Should().ContainKey("available_concurrent_slots");
        result.Data["timeout_ms"].Should().Be(30000);
    }

    [Fact]
    public async Task CheckHealthAsync_WithDifferentConcurrencySettings_ShouldReturnCorrectData()
    {
        // Arrange
        var concurrencyService = CreateConcurrencyService(50);

        var healthCheck = new FeishuWebhookHealthCheck(
            _optionsMock.Object,
            concurrencyService);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data["max_concurrent_events"].Should().Be(50);
        result.Data["timeout_ms"].Should().Be(30000);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSlotsExhausted_ShouldReturnUnhealthy()
    {
        // Arrange - 占满所有并发槽位
        var concurrencyService = CreateConcurrencyService(2);
        await concurrencyService.AcquireAsync(CancellationToken.None);
        await concurrencyService.AcquireAsync(CancellationToken.None);

        var healthCheck = new FeishuWebhookHealthCheck(
            _optionsMock.Object,
            concurrencyService);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("并发槽位已耗尽");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUtilizationAbove80Percent_ShouldReturnDegraded()
    {
        // Arrange - 10 个槽位，占用 9 个（90% 利用率）
        var concurrencyService = CreateConcurrencyService(10);
        for (int i = 0; i < 9; i++)
        {
            await concurrencyService.AcquireAsync(CancellationToken.None);
        }

        var healthCheck = new FeishuWebhookHealthCheck(
            _optionsMock.Object,
            concurrencyService);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("并发利用率");
    }

    #region R3-FEAT-4：重放防护形态（nonceDedup）运维可见

    private sealed class StubEnvironmentService : IEnvironmentService
    {
        public StubEnvironmentService(bool isProduction) => IsProduction = isProduction;
        public bool IsProduction { get; }
        public bool IsDevelopment => !IsProduction;
        public bool IsStaging => false;
        public string EnvironmentName => IsProduction ? "Production" : "Development";
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldExposeNonceDedupForm()
    {
        // Arrange - R3-FEAT-4：形态必须在运行期可见（与启动期阻断/启动 Summary 构成三层）
        var concurrencyService = CreateConcurrencyService(10);

        var healthCheck = new FeishuWebhookHealthCheck(
            _optionsMock.Object,
            concurrencyService,
            nonceDeduplicator: new FeishuNonceDistributedDeduplicator());   // 内存实现

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey("nonceDedup");
        result.Data["nonceDedup"].Should().Be("InMemory");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenProductionWithInMemoryNonce_ShouldReturnDegraded()
    {
        // Arrange
        var concurrencyService = CreateConcurrencyService(10);

        var healthCheck = new FeishuWebhookHealthCheck(
            _optionsMock.Object,
            concurrencyService,
            nonceDeduplicator: new FeishuNonceDistributedDeduplicator(),
            environment: new StubEnvironmentService(isProduction: true));

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded,
            "生产 + 内存 Nonce 去重 = 静默的安全退化，必须被监控系统发现");
        result.Description.Should().Contain("跨实例重放");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenProductionWithDistributedNonce_ShouldReturnHealthy()
    {
        // Arrange
        var concurrencyService = CreateConcurrencyService(10);

        var healthCheck = new FeishuWebhookHealthCheck(
            _optionsMock.Object,
            concurrencyService,
            nonceDeduplicator: Mock.Of<IFeishuNonceDistributedDeduplicator>(),   // 分布式实现
            environment: new StubEnvironmentService(isProduction: true));

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["nonceDedup"].Should().Be("Distributed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDevelopmentWithInMemoryNonce_ShouldReturnHealthy()
    {
        // Arrange - 非生产不降级（与 R3-P0-1 的"非生产仅告警"口径一致）
        var concurrencyService = CreateConcurrencyService(10);

        var healthCheck = new FeishuWebhookHealthCheck(
            _optionsMock.Object,
            concurrencyService,
            nonceDeduplicator: new FeishuNonceDistributedDeduplicator(),
            environment: new StubEnvironmentService(isProduction: false));

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["nonceDedup"].Should().Be("InMemory");
    }

    #endregion
}
