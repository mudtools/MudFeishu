// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket;
using Mud.Feishu.WebSocket.SocketEventArgs;
using Mud.Feishu.WebSocket.Demo.Controllers;

namespace Mud.Feishu.WebSocket.Demo.Services;

/// <summary>
/// 心跳监控服务
/// </summary>
public class HeartbeatMonitorService : IHostedService, IDisposable
{
    private readonly ILogger<HeartbeatMonitorService> _logger;
    private readonly IFeishuWebSocketManager _webSocketManager;
    private readonly List<DateTime> _heartbeatTimestamps = new();
    private readonly Timer _heartbeatCheckTimer;
    private bool _disposed = false;

    /// <summary>
    /// 最后心跳时间
    /// </summary>
    public DateTime? LastHeartbeatTime { get; private set; }

    /// <summary>
    /// 心跳间隔（秒）
    /// </summary>
    public int? HeartbeatInterval { get; private set; }

    /// <summary>
    /// 心跳状态
    /// </summary>
    public string? HeartbeatStatus { get; private set; }

    /// <summary>
    /// 总心跳次数
    /// </summary>
    public int TotalHeartbeats { get; private set; }

    public HeartbeatMonitorService(
        ILogger<HeartbeatMonitorService> logger,
        IFeishuWebSocketManager webSocketManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
        
        // 设置心跳检查定时器，每30秒检查一次心跳状态
        _heartbeatCheckTimer = new Timer(CheckHeartbeatStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// 启动服务
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("心跳监控服务已启动");
        
        // 订阅心跳事件
        _webSocketManager.HeartbeatReceived += OnHeartbeatReceived;
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止服务
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("心跳监控服务已停止");
        
        // 取消订阅心跳事件
        _webSocketManager.HeartbeatReceived -= OnHeartbeatReceived;
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// 处理心跳事件
    /// </summary>
    private void OnHeartbeatReceived(object? sender, WebSocketHeartbeatEventArgs e)
    {
        try
        {
            LastHeartbeatTime = DateTime.UtcNow;
            HeartbeatInterval = e.Interval;
            HeartbeatStatus = e.Status;
            TotalHeartbeats++;

            _heartbeatTimestamps.Add(LastHeartbeatTime.Value);

            _logger.LogInformation("💗 收到心跳消息 - 时间戳: {Timestamp}, 间隔: {Interval}s, 状态: {Status}, 总次数: {TotalCount}",
                e.Timestamp, e.Interval, e.Status, TotalHeartbeats);

            // 分析心跳模式
            AnalyzeHeartbeatPattern();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理心跳事件时发生错误");
        }
    }

    /// <summary>
    /// 分析心跳模式
    /// </summary>
    private void AnalyzeHeartbeatPattern()
    {
        if (_heartbeatTimestamps.Count < 2)
            return;

        // 只保留最近10次心跳时间戳进行分析
        var recentTimestamps = _heartbeatTimestamps.TakeLast(10).ToList();
        var intervals = new List<double>();

        for (int i = 1; i < recentTimestamps.Count; i++)
        {
            var interval = (recentTimestamps[i] - recentTimestamps[i - 1]).TotalSeconds;
            intervals.Add(interval);
        }

        if (intervals.Any())
        {
            var averageInterval = intervals.Average();
            var variance = intervals.Select(x => Math.Pow(x - averageInterval, 2)).Average();
            var standardDeviation = Math.Sqrt(variance);

            _logger.LogDebug("心跳分析 - 平均间隔: {Average:F2}s, 标准差: {StdDev:F2}s", averageInterval, standardDeviation);

            // 如果标准差过大，可能表示心跳不稳定
            if (standardDeviation > 5.0)
            {
                _logger.LogWarning("检测到心跳间隔不稳定，可能存在连接问题");
            }
        }
    }

    /// <summary>
    /// 定期检查心跳状态
    /// </summary>
    private void CheckHeartbeatStatus(object? state)
    {
        try
        {
            if (!LastHeartbeatTime.HasValue)
            {
                _logger.LogDebug("尚未收到心跳消息");
                return;
            }

            var timeSinceLastHeartbeat = DateTime.UtcNow - LastHeartbeatTime.Value;
            var threshold = TimeSpan.FromMinutes(2); // 2分钟无心跳视为异常

            if (timeSinceLastHeartbeat > threshold)
            {
                _logger.LogWarning("心跳检测超时 - 最后心跳: {LastHeartbeat}, 已超时: {TimeSinceLastHeartbeat:mm\\:ss}",
                    LastHeartbeatTime, timeSinceLastHeartbeat);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查心跳状态时发生错误");
        }
    }

    /// <summary>
    /// 获取心跳统计信息
    /// </summary>
    public HeartbeatStatistics GetStatistics()
    {
        var recentHeartbeats = _heartbeatTimestamps
            .TakeLast(20)
            .Select((timestamp, index) => new HeartbeatInfo
            {
                Timestamp = timestamp,
                Interval = index > 0 ? (int?)(timestamp - _heartbeatTimestamps[_heartbeatTimestamps.Count - 20 + index - 1]).TotalSeconds : null,
                Status = HeartbeatStatus
            })
            .ToList();

        return new HeartbeatStatistics
        {
            TotalHeartbeats = TotalHeartbeats,
            RecentHeartbeats = recentHeartbeats,
            LastHeartbeatTime = LastHeartbeatTime,
            AverageInterval = CalculateAverageInterval(recentHeartbeats)
        };
    }

    private static double? CalculateAverageInterval(List<HeartbeatInfo> heartbeats)
    {
        if (heartbeats.Count < 2) return null;

        var intervals = heartbeats.Where(h => h.Interval.HasValue).Select(h => h.Interval!.Value).ToList();
        return intervals.Any() ? intervals.Average() : null;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _heartbeatCheckTimer?.Dispose();
            _disposed = true;
        }
    }
}