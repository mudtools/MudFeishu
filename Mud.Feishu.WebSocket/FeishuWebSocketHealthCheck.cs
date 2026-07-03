// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET8_0_OR_GREATER
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
    private readonly ILogger<FeishuWebSocketHealthCheck>? _logger;

    /// <summary>
    /// 初始化健康检查实例
    /// </summary>
    /// <param name="hostedService">WebSocket 后台服务</param>
    /// <param name="logger">日志记录器（可选）</param>
    public FeishuWebSocketHealthCheck(
        FeishuWebSocketHostedService hostedService,
        ILogger<FeishuWebSocketHealthCheck>? logger = null)
    {
        _hostedService = hostedService ?? throw new ArgumentNullException(nameof(hostedService));
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

            var data = new Dictionary<string, object>
            {
                ["connected"] = state.IsConnected,
                ["uptime"] = stats.Uptime.ToString(),
                ["reconnectCount"] = stats.ReconnectCount,
                ["lastError"] = stats.LastError?.Message ?? "none"
            };

            if (state.IsConnected)
            {
                _logger?.LogDebug("WebSocket健康检查: Healthy (连接时间: {Uptime}, 重连次数: {ReconnectCount})",
                    stats.Uptime, stats.ReconnectCount);
                return Task.FromResult(HealthCheckResult.Healthy(
                    $"WebSocket连接正常 (已运行: {stats.Uptime}, 重连次数: {stats.ReconnectCount})",
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
#endif
