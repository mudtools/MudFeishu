// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.WebSocket;
using Mud.Feishu.WebSocket.SocketEventArgs;

namespace Mud.Feishu.WebSocket.Demo.Controllers;

/// <summary>
/// 心跳功能测试控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HeartbeatTestController : ControllerBase
{
    private readonly ILogger<HeartbeatTestController> _logger;
    private readonly IServiceProvider _serviceProvider;
    private static int _heartbeatCount = 0;
    private static readonly List<DateTime> _heartbeatTimestamps = new();

    public HeartbeatTestController(
        ILogger<HeartbeatTestController> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// 启用心跳监听
    /// </summary>
    [HttpPost("start-listening")]
    public ActionResult StartListening()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var webSocketManager = scope.ServiceProvider.GetRequiredService<IFeishuWebSocketManager>();

            // 订阅心跳事件
            webSocketManager.HeartbeatReceived += OnHeartbeatReceived;

            _logger.LogInformation("🫀 [API] 已启动心跳事件监听");
            return Ok(new { message = "心跳监听已启动", currentCount = _heartbeatCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动心跳监听失败");
            return StatusCode(500, new { error = "启动心跳监听失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 停止心跳监听
    /// </summary>
    [HttpPost("stop-listening")]
    public ActionResult StopListening()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var webSocketManager = scope.ServiceProvider.GetRequiredService<IFeishuWebSocketManager>();

            // 取消订阅心跳事件
            webSocketManager.HeartbeatReceived -= OnHeartbeatReceived;

            _logger.LogInformation("🛑 [API] 已停止心跳事件监听");
            return Ok(new { message = "心跳监听已停止", finalCount = _heartbeatCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止心跳监听失败");
            return StatusCode(500, new { error = "停止心跳监听失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取心跳统计
    /// </summary>
    [HttpGet("statistics")]
    public ActionResult GetStatistics()
    {
        try
        {
            var recentTimestamps = _heartbeatTimestamps.TakeLast(10).ToList();
            var averageInterval = recentTimestamps.Count >= 2 
                ? recentTimestamps.Zip(recentTimestamps.Skip(1), (prev, curr) => (curr - prev).TotalSeconds).Average()
                : (double?)null;

            var statistics = new
            {
                TotalCount = _heartbeatCount,
                RecentCount = recentTimestamps.Count,
                RecentTimestamps = recentTimestamps,
                AverageInterval = averageInterval,
                LastHeartbeat = recentTimestamps.LastOrDefault(),
                IsListening = _heartbeatCount > 0
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取心跳统计失败");
            return StatusCode(500, new { error = "获取心跳统计失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 重置心跳统计
    /// </summary>
    [HttpPost("reset")]
    public ActionResult ResetStatistics()
    {
        try
        {
            lock (_heartbeatTimestamps)
            {
                _heartbeatCount = 0;
                _heartbeatTimestamps.Clear();
            }

            _logger.LogInformation("🔄 [API] 已重置心跳统计");
            return Ok(new { message = "心跳统计已重置" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置心跳统计失败");
            return StatusCode(500, new { error = "重置心跳统计失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 心跳事件处理
    /// </summary>
    private void OnHeartbeatReceived(object? sender, WebSocketHeartbeatEventArgs e)
    {
        lock (_heartbeatTimestamps)
        {
            _heartbeatCount++;
            var now = DateTime.UtcNow;
            _heartbeatTimestamps.Add(now);

            // 只保留最近50条记录
            if (_heartbeatTimestamps.Count > 50)
            {
                _heartbeatTimestamps.RemoveAt(0);
            }

            _logger.LogInformation("🫀 [Heartbeat] #{Count} - 时间: {Timestamp}, 间隔: {Interval}s, 状态: {Status}",
                _heartbeatCount, e.Timestamp, e.Interval, e.Status);
        }
    }
}