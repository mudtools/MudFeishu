// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Services;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书 Webhook 健康检查。
/// 基于并发槽位可用性和配置有效性判定健康状态。
/// </summary>
public class FeishuWebhookHealthCheck : IHealthCheck
{
    private readonly IOptionsMonitor<FeishuWebhookOptions> _options;
    private readonly FeishuWebhookConcurrencyService _concurrencyService;
    private readonly IFailedEventStore? _failedEventStore;

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="options">Webhook 配置选项监控器</param>
    /// <param name="concurrencyService">并发控制服务</param>
    /// <param name="failedEventStore">失败事件存储（可选，用于积压检测）</param>
    public FeishuWebhookHealthCheck(
        IOptionsMonitor<FeishuWebhookOptions> options,
        FeishuWebhookConcurrencyService concurrencyService,
        IFailedEventStore? failedEventStore = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _concurrencyService = concurrencyService ?? throw new ArgumentNullException(nameof(concurrencyService));
        _failedEventStore = failedEventStore;
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        var availableSlots = _concurrencyService.AvailableCount;
        var maxConcurrent = options.MaxConcurrentEvents;

        var data = new Dictionary<string, object>
        {
            ["max_concurrent_events"] = maxConcurrent,
            ["available_concurrent_slots"] = availableSlots,
            ["timeout_ms"] = options.EventHandlingTimeoutMs,
            ["concurrent_utilization_pct"] = maxConcurrent > 0
                ? Math.Round((1.0 - (double)availableSlots / maxConcurrent) * 100, 1)
                : 0,
        };

        // 并发槽位耗尽 → Unhealthy
        if (availableSlots <= 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Webhook 并发槽位已耗尽 (0/{maxConcurrent})，事件可能被拒绝",
                null,
                data));
        }

        // 并发利用率 ≥ 80% → Degraded
        var utilization = maxConcurrent > 0
            ? 1.0 - (double)availableSlots / maxConcurrent
            : 0;
        if (utilization >= 0.8)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Webhook 并发利用率 {utilization:P1}，接近上限",
                null,
                data));
        }

        // 失败事件积压检测
        if (_failedEventStore is InMemoryFailedEventStore memStore)
        {
            var backlog = memStore.GetFailedEventCount();
            data["failed_event_backlog"] = backlog;

            var degradedThreshold = 100;
            var unhealthyThreshold = 500;

            if (backlog >= unhealthyThreshold)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"失败事件积压 {backlog} 超过硬阈值 {unhealthyThreshold}",
                    null,
                    data));
            }

            if (backlog >= degradedThreshold)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"失败事件积压 {backlog} 超过软阈值 {degradedThreshold}",
                    null,
                    data));
            }
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Webhook 服务正常 (并发: {availableSlots}/{maxConcurrent})",
            data));
    }
}
