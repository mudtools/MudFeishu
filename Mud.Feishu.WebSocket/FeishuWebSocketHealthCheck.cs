// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书 WebSocket 连接健康检查
/// </summary>
/// <remarks>
/// 提供 ASP.NET Core 健康检查集成，报告 WebSocket 连接状态。
/// 在 Startup/Program 中通过 <c>services.AddHealthChecks().AddCheck&lt;FeishuWebSocketHealthCheck&gt;("feishu_websocket")</c> 注册。
/// </remarks>
public class FeishuWebSocketHealthCheck : IHealthCheck
{
    private readonly FeishuWebSocketHostedService _hostedService;
    private readonly IReconnectionOrchestrator _reconnectionOrchestrator;
    private readonly ILogger<FeishuWebSocketHealthCheck>? _logger;

    /// <summary>
    /// 初始化健康检查实例
    /// </summary>
    /// <param name="hostedService">WebSocket 后台服务</param>
    /// <param name="reconnectionOrchestrator">重连协调器</param>
    /// <param name="logger">日志记录器（可选）</param>
    public FeishuWebSocketHealthCheck(
        FeishuWebSocketHostedService hostedService,
        IReconnectionOrchestrator reconnectionOrchestrator,
        ILogger<FeishuWebSocketHealthCheck>? logger = null)
    {
        _hostedService = hostedService ?? throw new ArgumentNullException(nameof(hostedService));
        _reconnectionOrchestrator = reconnectionOrchestrator ?? throw new ArgumentNullException(nameof(reconnectionOrchestrator));
        _logger = logger;
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    /// <param name="context">健康检查上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果</returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = _hostedService.GetConnectionState();
            var stats = _hostedService.GetConnectionStats();

            var reconnectState = _reconnectionOrchestrator.GetReconnectState();

            // F1/F2（P0-1 的结构性兜底）：把"接收循环是否存活 / 距上次收帧多久"变成可观测事实。
            // 仅凭 state.IsConnected（= WebSocketState.Open）无法区分"连接可用"与"接收管道已死"。
            var liveness = _hostedService.GetConnectionLiveness();

            var data = new Dictionary<string, object>
            {
                ["connected"] = state.IsConnected,
                ["uptime"] = stats.Uptime.ToString(),
                ["reconnectCount"] = stats.ReconnectCount,
                ["lastError"] = stats.LastError?.Message ?? "none",
                ["is_reconnecting"] = reconnectState.IsReconnecting,
                ["is_circuit_open"] = reconnectState.IsCircuitOpen,
                // F1/F2 新增维度（-1 表示尚无收帧样本，不代表静默）
                ["receive_loop_alive"] = liveness.ReceiveLoopAlive,
                ["last_receive_utc"] = liveness.LastReceiveUtc?.ToString("O") ?? "never",
                ["idle_ms"] = liveness.IdleMs,
                ["is_zombie"] = liveness.IsZombie
            };

            // F2：僵尸态 = "被告知已连接，但没有任何循环在读帧"——后续事件永远不会被消费，
            // 而所有基于 IsConnected 的判定都会因为 State==Open 而拒绝恢复，必须判 Unhealthy。
            if (liveness.IsZombie)
            {
                _logger?.LogWarning("WebSocket健康检查: Unhealthy (僵尸连接 — 接收循环已结束但连接仍为 Open)");
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "WebSocket 连接看似正常（State=Open）但接收循环已结束，无法再接收任何事件；" +
                    "已由 HostedService 的周期性检查触发重连，若持续出现请检查是否存在外部取消/释放连接的行为",
                    null,
                    data));
            }

            // F2 修复：补充并发指标，对齐 Webhook 健康检查
            var concurrencyService = _hostedService.GetConcurrencyService();
            if (concurrencyService != null)
            {
                var maxConcurrent = concurrencyService.MaxConcurrentHandlers;
                var available = concurrencyService.AvailableCount;
                var pending = concurrencyService.PendingCount;
                var utilizationPct = maxConcurrent > 0
                    ? Math.Round((1.0 - (double)available / maxConcurrent) * 100, 1)
                    : 0;

                data["max_concurrent_handlers"] = maxConcurrent;
                data["available_concurrent_slots"] = available;
                data["backlog"] = pending;
                data["concurrent_utilization_pct"] = utilizationPct;

                // F2：槽位耗尽 → Unhealthy
                // P1-1 后语义同步（§0.4）：槽位耗尽不再意味着"事件被拒绝/丢弃"——
                // 租约在接收路径获取，耗尽时接收循环被阻塞以施加 TCP 反压，事件只会延迟处理不会被拒绝；
                // 判定本身仍成立（持续 Unhealthy 表明消费速度长期跟不上投递速度，需排查慢处理器或扩容）。
                if (maxConcurrent > 0 && available <= 0)
                {
                    _logger?.LogWarning("WebSocket健康检查: Unhealthy (并发槽位耗尽 0/{MaxConcurrent})", maxConcurrent);
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        $"WebSocket并发槽位已耗尽 (0/{maxConcurrent})，接收循环被反压阻塞（事件延迟处理，不会被拒绝；请排查慢处理器或调大 MaxConcurrentHandlers）",
                        null,
                        data));
                }

                // F2：利用率 ≥ 90% → Degraded
                if (maxConcurrent > 0 && utilizationPct >= 90)
                {
                    _logger?.LogWarning("WebSocket健康检查: Degraded (并发利用率 {UtilizationPct}%)", utilizationPct);
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"WebSocket并发利用率 {utilizationPct}%，接近上限",
                        null,
                        data));
                }
            }

            if (state.IsConnected)
            {
                _logger?.LogDebug("WebSocket健康检查: Healthy (连接时间: {Uptime}, 重连次数: {ReconnectCount})",
                    stats.Uptime, stats.ReconnectCount);
                return Task.FromResult(HealthCheckResult.Healthy(
                    $"WebSocket连接正常 (已运行: {stats.Uptime}, 重连次数: {stats.ReconnectCount})",
                    data));
            }

            // F3：熔断器打开时返回 Degraded 而非 Unhealthy，
            // 表明系统已主动停止重连以保护飞书侧
            if (reconnectState.IsCircuitOpen)
            {
                _logger?.LogWarning("WebSocket健康检查: Degraded (重连熔断器已打开)");
                return Task.FromResult(HealthCheckResult.Degraded(
                    "WebSocket未连接且重连熔断器已打开，需人工介入或等待配置变更",
                    stats.LastError,
                    data));
            }

            _logger?.LogWarning("WebSocket健康检查: Unhealthy (最后错误: {LastError})",
                stats.LastError?.Message ?? "未知");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"WebSocket未连接 (最后错误: {stats.LastError?.Message ?? "未知"})",
                stats.LastError,
                data));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "WebSocket健康检查执行异常");
            return Task.FromResult(HealthCheckResult.Unhealthy("WebSocket健康检查执行异常", ex));
        }
    }
}
