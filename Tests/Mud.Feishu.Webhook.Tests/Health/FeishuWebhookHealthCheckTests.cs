// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Webhook.Configuration;

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

    [Fact]
    public async Task CheckHealthAsync_WithValidOptions_ShouldReturnHealthy()
    {
        // Arrange
        _optionsMock.Setup(x => x.CurrentValue).Returns(_options);

        // 使用真实的 FeishuWebhookConcurrencyService 实例
        var concurrencyService = new FeishuWebhookConcurrencyService(_optionsMock.Object, Mock.Of<ILogger<FeishuWebhookConcurrencyService>>());

        var healthCheck = new FeishuWebhookHealthCheck(
            _optionsMock.Object,
            concurrencyService);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("飞书 Webhook 服务运行正常");
        result.Data.Should().ContainKey("configuration_valid");
        result.Data["configuration_valid"].Should().Be(true);
        result.Data["max_concurrent_events"].Should().Be(10);
        result.Data["timeout_ms"].Should().Be(30000);
    }

    [Fact]
    public async Task CheckHealthAsync_WithDifferentConcurrencySettings_ShouldReturnCorrectData()
    {
        // Arrange
        var customOptions = new FeishuWebhookOptions
        {
            MaxConcurrentEvents = 50,
            EventHandlingTimeoutMs = 60000
        };
        _optionsMock.Setup(x => x.CurrentValue).Returns(customOptions);

        // 使用真实的 FeishuWebhookConcurrencyService 实例
        var concurrencyService = new FeishuWebhookConcurrencyService(_optionsMock.Object, Mock.Of<ILogger<FeishuWebhookConcurrencyService>>());

        var healthCheck = new FeishuWebhookHealthCheck(
            _optionsMock.Object,
            concurrencyService);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data["max_concurrent_events"].Should().Be(50);
        result.Data["timeout_ms"].Should().Be(60000);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldContainHealthCheckNotes()
    {
        // Arrange
        _optionsMock.Setup(x => x.CurrentValue).Returns(_options);

        // 使用真实的 FeishuWebhookConcurrencyService 实例
        var concurrencyService = new FeishuWebhookConcurrencyService(_optionsMock.Object, Mock.Of<ILogger<FeishuWebhookConcurrencyService>>());

        var healthCheck = new FeishuWebhookHealthCheck(
            _optionsMock.Object,
            concurrencyService);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey("health_check_notes");
        result.Data["health_check_notes"].ToString().Should().Contain("TelemetryEventInterceptor");
    }
}
