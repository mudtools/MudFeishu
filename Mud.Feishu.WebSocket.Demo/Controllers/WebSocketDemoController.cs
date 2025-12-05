// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.Abstractions;
using Mud.Feishu.WebSocket.Services;

namespace Mud.Feishu.WebSocket.Demo.Controllers;

/// <summary>
/// WebSocket演示控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WebSocketDemoController : ControllerBase
{
    private readonly ILogger<WebSocketDemoController> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DemoEventService _demoEventService;
    private readonly List<HeartbeatInfo> _heartbeatHistory = new();

    public WebSocketDemoController(
        ILogger<WebSocketDemoController> logger,
        IServiceProvider serviceProvider,
        DemoEventService demoEventService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _demoEventService = demoEventService ?? throw new ArgumentNullException(nameof(demoEventService));
    }

    /// <summary>
    /// 获取WebSocket连接状态
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<WebSocketStatus>> GetStatusAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var webSocketManager = scope.ServiceProvider.GetRequiredService<IFeishuWebSocketManager>();

            var connectionState = webSocketManager.GetConnectionState();
            var statistics = _demoEventService.GetStatistics();

            var status = new WebSocketStatus
            {
                IsConnected = webSocketManager.IsConnected,
                ConnectionState = connectionState,
                Statistics = statistics,
                ServerTime = DateTime.UtcNow
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取WebSocket状态失败");
            return StatusCode(500, new { error = "获取状态失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 启动WebSocket连接
    /// </summary>
    [HttpPost("connect")]
    public async Task<ActionResult> ConnectAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var webSocketManager = scope.ServiceProvider.GetRequiredService<IFeishuWebSocketManager>();

            if (webSocketManager.IsConnected)
            {
                return Ok(new { message = "WebSocket已连接" });
            }

            await webSocketManager.StartAsync();

            _logger.LogInformation("🚀 [API] 手动启动WebSocket连接");
            return Ok(new { message = "WebSocket连接已启动" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动WebSocket连接失败");
            return StatusCode(500, new { error = "启动连接失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 断开WebSocket连接
    /// </summary>
    [HttpPost("disconnect")]
    public async Task<ActionResult> DisconnectAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var webSocketManager = scope.ServiceProvider.GetRequiredService<IFeishuWebSocketManager>();

            if (!webSocketManager.IsConnected)
            {
                return Ok(new { message = "WebSocket已断开" });
            }

            await webSocketManager.StopAsync();

            _logger.LogInformation("🛑 [API] 手动断开WebSocket连接");
            return Ok(new { message = "WebSocket连接已断开" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "断开WebSocket连接失败");
            return StatusCode(500, new { error = "断开连接失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 发送测试消息
    /// </summary>
    [HttpPost("send-message")]
    public async Task<ActionResult> SendMessageAsync([FromBody] SendMessageRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "消息内容不能为空" });
            }

            using var scope = _serviceProvider.CreateScope();
            var webSocketManager = scope.ServiceProvider.GetRequiredService<IFeishuWebSocketManager>();

            if (!webSocketManager.IsConnected)
            {
                return BadRequest(new { error = "WebSocket未连接" });
            }

            await webSocketManager.SendMessageAsync(request.Message);

            _logger.LogInformation("📤 [API] 发送测试消息: {Message}", request.Message);
            return Ok(new { message = "消息发送成功", content = request.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送消息失败");
            return StatusCode(500, new { error = "发送消息失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 生成模拟用户事件
    /// </summary>
    [HttpPost("generate-user-event")]
    public async Task<ActionResult<EventData>> GenerateUserEventAsync()
    {
        try
        {
            var eventData = _demoEventService.GenerateMockUserEvent();

            //_logger.LogInformation("👤 [API] 生成用户事件: {EventId}", eventData.EventId);
            return Ok(eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成用户事件失败");
            return StatusCode(500, new { error = "生成用户事件失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 生成模拟部门事件
    /// </summary>
    [HttpPost("generate-department-event")]
    public async Task<ActionResult<EventData>> GenerateDepartmentEventAsync()
    {
        try
        {
            var eventData = _demoEventService.GenerateMockDepartmentEvent();

            //_logger.LogInformation("🏢 [API] 生成部门事件: {EventId}", eventData.EventId);
            return Ok(eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成部门事件失败");
            return StatusCode(500, new { error = "生成部门事件失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 生成模拟审批事件
    /// </summary>
    [HttpPost("generate-approval-event")]
    public async Task<ActionResult<EventData>> GenerateApprovalEventAsync()
    {
        try
        {
            var eventData = _demoEventService.GenerateMockApprovalEvent();

            //_logger.LogInformation("✅ [API] 生成审批事件: {EventId}", eventData.EventId);
            return Ok(eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成审批事件失败");
            return StatusCode(500, new { error = "生成审批事件失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取事件统计信息
    /// </summary>
    [HttpGet("statistics")]
    public ActionResult<EventStatistics> GetStatisticsAsync()
    {
        try
        {
            var statistics = _demoEventService.GetStatistics();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取统计信息失败");
            return StatusCode(500, new { error = "获取统计信息失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取最近的事件记录
    /// </summary>
    [HttpGet("recent-events")]
    public ActionResult<RecentEvents> GetRecentEventsAsync([FromQuery] int count = 10)
    {
        try
        {
            var recentEvents = _demoEventService.GetRecentEvents(Math.Min(count, 50));
            return Ok(recentEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最近事件失败");
            return StatusCode(500, new { error = "获取最近事件失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 清空所有事件记录
    /// </summary>
    [HttpDelete("clear-events")]
    public ActionResult ClearEventsAsync()
    {
        try
        {
            _demoEventService.ClearAllEvents();

            _logger.LogInformation("🗑️ [API] 已清空所有事件记录");
            return Ok(new { message = "事件记录已清空" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空事件记录失败");
            return StatusCode(500, new { error = "清空事件记录失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 重新连接WebSocket
    /// </summary>
    [HttpPost("reconnect")]
    public async Task<ActionResult> ReconnectAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var webSocketManager = scope.ServiceProvider.GetRequiredService<IFeishuWebSocketManager>();

            await webSocketManager.ReconnectAsync();

            _logger.LogInformation("🔄 [API] 手动重新连接WebSocket");
            return Ok(new { message = "WebSocket重新连接已启动" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新连接WebSocket失败");
            return StatusCode(500, new { error = "重新连接失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取心跳统计信息
    /// </summary>
    [HttpGet("heartbeat-statistics")]
    public ActionResult<HeartbeatStatistics> GetHeartbeatStatisticsAsync()
    {
        try
        {
            var recentHeartbeats = _heartbeatHistory.TakeLast(20).ToList();
            var statistics = new HeartbeatStatistics
            {
                TotalHeartbeats = _heartbeatHistory.Count,
                RecentHeartbeats = recentHeartbeats,
                LastHeartbeatTime = recentHeartbeats.LastOrDefault()?.Timestamp,
                AverageInterval = CalculateAverageInterval(recentHeartbeats)
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取心跳统计信息失败");
            return StatusCode(500, new { error = "获取心跳统计信息失败", message = ex.Message });
        }
    }

    private static double? CalculateAverageInterval(List<HeartbeatInfo> heartbeats)
    {
        if (heartbeats.Count < 2) return null;

        var intervals = new List<double>();
        for (int i = 1; i < heartbeats.Count; i++)
        {
            var interval = (heartbeats[i].Timestamp - heartbeats[i - 1].Timestamp).TotalSeconds;
            intervals.Add(interval);
        }

        return intervals.Average();
    }
}

/// <summary>
/// WebSocket状态响应
/// </summary>
public class WebSocketStatus
{
    public bool IsConnected { get; init; }
    public WebSocketConnectionState ConnectionState { get; init; } = null!;
    public EventStatistics Statistics { get; init; } = null!;
    public DateTime ServerTime { get; init; }
}

/// <summary>
/// 发送消息请求
/// </summary>
public class SendMessageRequest
{
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// 心跳信息
/// </summary>
public class HeartbeatInfo
{
    public DateTime Timestamp { get; set; }
    public int? Interval { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// 心跳统计信息
/// </summary>
public class HeartbeatStatistics
{
    /// <summary>
    /// 总心跳次数
    /// </summary>
    public int TotalHeartbeats { get; set; }

    /// <summary>
    /// 最近的心跳记录
    /// </summary>
    public List<HeartbeatInfo> RecentHeartbeats { get; set; } = new();

    /// <summary>
    /// 最后一次心跳时间
    /// </summary>
    public DateTime? LastHeartbeatTime { get; set; }

    /// <summary>
    /// 平均心跳间隔（秒）
    /// </summary>
    public double? AverageInterval { get; set; }
}