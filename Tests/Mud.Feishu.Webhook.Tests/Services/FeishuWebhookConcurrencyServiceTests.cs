// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Services;

/// <summary>
/// Webhook 并发服务测试类
/// 测试限流策略、并发控制和 URL 验证相关功能
/// </summary>
public class FeishuWebhookConcurrencyServiceTests
{
    private readonly Mock<ILogger<FeishuWebhookConcurrencyService>> _loggerMock;
    private readonly Mock<IOptionsMonitor<FeishuWebhookOptions>> _optionsMonitorMock;
    private readonly FeishuWebhookOptions _options;

    public FeishuWebhookConcurrencyServiceTests()
    {
        _loggerMock = new Mock<ILogger<FeishuWebhookConcurrencyService>>();
        _optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();

        _options = new FeishuWebhookOptions
        {
            MaxConcurrentEvents = 3, // 限制并发数为 3
            EventHandlingTimeoutMs = 5000
        };

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_options);
    }

    /// <summary>
    /// 测试不同并发限制场景下的信号量获取逻辑
    /// 业务场景：验证正常限制、无限制、负限制和大限制等不同场景下的并发控制
    /// </summary>
    /// <param name="maxConcurrentEvents">最大并发数限制</param>
    /// <param name="acquireCount">尝试获取的信号量数量</param>
    /// <param name="expectedFinalResult">最后一次尝试的预期结果</param>
    /// <param name="scenarioDescription">场景描述</param>
    [Theory]
    [InlineData(3, 1, true, "正常限制，获取1个信号量")]
    [InlineData(3, 3, true, "正常限制，获取3个信号量（达到限制）")]
    [InlineData(3, 4, false, "正常限制，获取4个信号量（超过限制）")]
    [InlineData(0, 10, true, "无限制（0），获取10个信号量")]
    [InlineData(-1, 1, true, "负限制（视为无限制），获取1个信号量")]
    [InlineData(100, 50, true, "大限制，获取50个信号量")]
    public async Task AcquireAsync_ShouldHandleDifferentLimitScenarios(
        int maxConcurrentEvents,
        int acquireCount,
        bool expectedFinalResult,
        string scenarioDescription)
    {
        // Arrange - 设置不同的并发限制
        var options = new FeishuWebhookOptions
        {
            MaxConcurrentEvents = maxConcurrentEvents,
            EventHandlingTimeoutMs = 5000
        };
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);

        var service = CreateService();

        // Act - 尝试获取指定数量的信号量
        var leases = new List<IDisposable>();
        var results = new List<bool>();

        for (int i = 0; i < acquireCount; i++)
        {
            try
            {
                // 尝试获取信号量，设置较短的超时时间
                var lease = await service.AcquireAsync(new CancellationTokenSource(100).Token);
                leases.Add(lease);
                results.Add(true);
            }
            catch (OperationCanceledException)
            {
                // 超时，视为获取失败
                results.Add(false);
            }
        }

        // 清理：释放所有获取的信号量
        foreach (var lease in leases)
        {
            lease.Dispose();
        }

        // Assert - 检查最后一次尝试的结果
        results.Last().Should().Be(expectedFinalResult, $"场景: {scenarioDescription}");

        // 对于无限制或大限制的情况，所有尝试都应该成功
        if (maxConcurrentEvents == 0 || (maxConcurrentEvents > 0 && acquireCount <= maxConcurrentEvents))
        {
            results.All(r => r).Should().BeTrue($"场景: {scenarioDescription}");
        }
    }

    /// <summary>
    /// 测试信号量释放后的可获取性
    /// 业务场景：验证释放信号量后，是否可以重新获取信号量
    /// </summary>
    [Fact]
    public async Task AcquireAsync_ShouldAllowReacquisitionAfterRelease()
    {
        // Arrange
        var service = CreateService();

        // Act - 先获取两个信号量
        var lease1 = await service.AcquireAsync();
        var lease2 = await service.AcquireAsync();

        // 释放一个信号量
        lease1.Dispose();

        // 再次尝试获取
        var result = false;
        try
        {
            var lease3 = await service.AcquireAsync(new CancellationTokenSource(100).Token);
            result = true;
            lease3.Dispose();
        }
        catch (OperationCanceledException)
        {
            result = false;
        }

        // 清理
        lease2.Dispose();

        // Assert
        result.Should().BeTrue(); // 应该能够获取，因为释放了一个信号量
    }

    /// <summary>
    /// 测试信号量的可用性
    /// 业务场景：验证信号量的可用数量是否正确反映当前状态
    /// </summary>
    [Fact]
    public async Task AcquireAsync_ShouldRespectSemaphoreLimits()
    {
        // Arrange
        var service = CreateService();

        // Act - 获取两个信号量
        var lease1 = await service.AcquireAsync();
        var lease2 = await service.AcquireAsync();

        // 尝试获取第三个信号量（应该成功）
        var lease3 = await service.AcquireAsync();

        // 尝试获取第四个信号量（应该失败，因为超过限制）
        var result = false;
        try
        {
            var lease4 = await service.AcquireAsync(new CancellationTokenSource(100).Token);
            result = true;
            lease4.Dispose();
        }
        catch (OperationCanceledException)
        {
            result = false;
        }

        // 清理
        lease1.Dispose();
        lease2.Dispose();
        lease3.Dispose();

        // Assert - 第四个信号量应该获取失败
        result.Should().BeFalse();
    }

    private FeishuWebhookConcurrencyService CreateService()
    {
        return new FeishuWebhookConcurrencyService(
            _optionsMonitorMock.Object,
            _loggerMock.Object);
    }

}
