// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Services;
using Mud.Feishu.Webhook.Utils;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书 Webhook 健康检查。
/// 基于并发槽位可用性、配置有效性与**重放防护形态**判定健康状态。
/// </summary>
/// <remarks>
/// R3-FEAT-4：新增"重放防护形态"（<c>nonceDedup</c>）数据项——把 R3-P0-1 的**启动期阻断**
/// 延伸到**运行期可见**。生产环境使用进程内内存 Nonce 去重时判定为 <c>Degraded</c>，
/// 使"多实例部署悄悄失去防重放能力"这一静默退化能被监控系统发现。
/// </remarks>
public class FeishuWebhookHealthCheck : IHealthCheck
{
    private readonly IOptionsMonitor<FeishuWebhookOptions> _options;
    private readonly FeishuWebhookConcurrencyService _concurrencyService;
    private readonly IFailedEventStore? _failedEventStore;
    private readonly IFeishuNonceDistributedDeduplicator? _nonceDeduplicator;
    private readonly IEnvironmentService? _environment;

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="options">Webhook 配置选项监控器</param>
    /// <param name="concurrencyService">并发控制服务</param>
    /// <param name="failedEventStore">失败事件存储（可选，用于积压检测）</param>
    /// <param name="nonceDeduplicator">Nonce 去重实现（可选，用于重放防护形态判定）</param>
    /// <param name="environment">环境服务（可选，用于生产判定）</param>
    public FeishuWebhookHealthCheck(
        IOptionsMonitor<FeishuWebhookOptions> options,
        FeishuWebhookConcurrencyService concurrencyService,
        IFailedEventStore? failedEventStore = null,
        IFeishuNonceDistributedDeduplicator? nonceDeduplicator = null,
        IEnvironmentService? environment = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _concurrencyService = concurrencyService ?? throw new ArgumentNullException(nameof(concurrencyService));
        _failedEventStore = failedEventStore;
        _nonceDeduplicator = nonceDeduplicator;
        _environment = environment;
    }

    /// <summary>
    /// 判定当前 Nonce 去重实现的部署形态。
    /// </summary>
    /// <remarks>
    /// 与 <c>FeishuWebhookServiceBuilder</c> 的形态判定保持同形：
    /// 未注册实现或仍为内存实现 → <c>InMemory</c>；否则视为分布式（<c>Redis</c>）。
    /// </remarks>
    private string NonceDedupForm =>
        _nonceDeduplicator is null or FeishuNonceDistributedDeduplicator ? "InMemory" : "Distributed";

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        var availableSlots = _concurrencyService.AvailableCount;
        var maxConcurrent = options.MaxConcurrentEvents;

        var nonceDedupForm = NonceDedupForm;
        var isProduction = _environment?.IsProduction == true;

        var data = new Dictionary<string, object>
        {
            ["max_concurrent_events"] = maxConcurrent,
            ["available_concurrent_slots"] = availableSlots,
            ["timeout_ms"] = options.EventHandlingTimeoutMs,
            ["concurrent_utilization_pct"] = maxConcurrent > 0
                ? Math.Round((1.0 - (double)availableSlots / maxConcurrent) * 100, 1)
                : 0,

            // R3-FEAT-4：重放防护形态——运行期可见（与启动期阻断 / 启动 Summary 日志构成三层）
            ["nonceDedup"] = nonceDedupForm,
            ["timestamp_tolerance_seconds"] = options.TimestampToleranceSeconds,
        };

        // 并发槽位耗尽 → Unhealthy（最严重，优先判定）
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

        // R3-FEAT-4：生产 + 内存 Nonce 去重 = 静默的安全退化 → Degraded
        // （启动期已由 R3-P0-1 阻断；此处覆盖"显式豁免"与热更新后的形态回退）
        if (isProduction && nonceDedupForm == "InMemory")
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "生产环境使用进程内内存 Nonce 去重：多实例部署下跨实例重放攻击不可检测。" +
                "请注册 AddFeishuRedisDeduplicators() 或确认本部署为单实例",
                null,
                data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Webhook 服务正常 (并发: {availableSlots}/{maxConcurrent}, Nonce 去重: {nonceDedupForm})",
            data));
    }
}
