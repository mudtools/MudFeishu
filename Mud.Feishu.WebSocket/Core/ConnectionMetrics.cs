// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Metrics;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 连接监控指标管理器
/// </summary>
public class ConnectionMetrics
{
    private readonly ILogger<ConnectionMetrics> _logger;
    private readonly object _lock = new();

    // 基础统计
    private long _messagesSent = 0;
    private long _messagesReceived = 0;
    private long _bytesSent = 0;
    private long _bytesReceived = 0;

    // 时间统计
    private DateTime _connectionStartTime = DateTime.MinValue;
    private long _totalProcessingTimeMs = 0;
    private long _messageProcessingCount = 0;

    // 错误统计
    private long _connectionErrors = 0;
    private long _authenticationErrors = 0;

    // 性能统计
    private readonly ConcurrentQueue<double> _messageProcessingTimes = new();
    private const int MaxProcessingTimeSamples = 100;

    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    public event EventHandler<ConnectionMetricsEventArgs>? MetricsUpdated;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConnectionMetrics(ILogger<ConnectionMetrics> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 重置统计
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _messagesSent = 0;
            _messagesReceived = 0;
            _bytesSent = 0;
            _bytesReceived = 0;
            _connectionStartTime = DateTime.MinValue;
            _totalProcessingTimeMs = 0;
            _messageProcessingCount = 0;
            _connectionErrors = 0;
            _authenticationErrors = 0;
            // ConcurrentQueue没有Clear方法，清空队列的方式是重新创建
            while (_messageProcessingTimes.TryDequeue(out _)) { }
        }
    }

    /// <summary>
    /// 记录连接建立
    /// </summary>
    public void RecordConnectionEstablished()
    {
        lock (_lock)
        {
            _connectionStartTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 记录连接断开
    /// </summary>
    public void RecordConnectionClosed()
    {
        lock (_lock)
        {
            _connectionErrors++;
        }

        // 使用 FeishuMetricsHelper 记录指标
        FeishuMetricsHelper.RecordWebSocketConnectionError();
    }

    /// <summary>
    /// 记录消息发送
    /// </summary>
    public void RecordMessageSent(int bytes = 0)
    {
        lock (_lock)
        {
            _messagesSent++;
            _bytesSent += bytes;
        }

        // 使用 FeishuMetricsHelper 记录指标
        FeishuMetricsHelper.RecordWebSocketMessageSent(bytes);
    }

    /// <summary>
    /// 记录消息接收
    /// </summary>
    public void RecordMessageReceived(int bytes = 0)
    {
        lock (_lock)
        {
            _messagesReceived++;
            _bytesReceived += bytes;
        }

        // 使用 FeishuMetricsHelper 记录指标
        FeishuMetricsHelper.RecordWebSocketMessageReceived(bytes);
    }

    /// <summary>
    /// 记录消息处理开始
    /// </summary>
    public Stopwatch StartMessageProcessing()
    {
        return Stopwatch.StartNew();
    }

    /// <summary>
    /// 记录消息处理完成
    /// </summary>
    public void EndMessageProcessing(Stopwatch stopwatch)
    {
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        lock (_lock)
        {
            _totalProcessingTimeMs += elapsedMs;
            _messageProcessingCount++;

            // 记录处理时间样本
            _messageProcessingTimes.Enqueue(elapsedMs);
            if (_messageProcessingTimes.Count > MaxProcessingTimeSamples)
            {
                _messageProcessingTimes.TryDequeue(out _);
            }

            // 每处理100条消息触发一次更新事件
            if (_messageProcessingCount % 100 == 0)
            {
                TriggerMetricsUpdate();
            }
        }
    }

    /// <summary>
    /// 记录认证失败
    /// </summary>
    public void RecordAuthenticationError()
    {
        lock (_lock)
        {
            _authenticationErrors++;
        }

        // 使用 FeishuMetricsHelper 记录指标
        FeishuMetricsHelper.RecordWebSocketAuthenticationError();
    }

    /// <summary>
    /// 触发指标更新事件
    /// </summary>
    private void TriggerMetricsUpdate()
    {
        var stats = GetCurrentStats();
        MetricsUpdated?.Invoke(this, new ConnectionMetricsEventArgs { Statistics = stats });

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("连接统计更新: Sent={Sent}, Received={Received}, " +
                "BytesSent={BytesSent}, BytesReceived={BytesReceived}, Errors={Errors}, " +
                "AuthErrors={AuthErrors}, AvgProcessingTimeMs={AvgTime}",
                stats.MessagesSent, stats.MessagesReceived,
                stats.BytesSent, stats.BytesReceived, stats.ConnectionErrors, stats.AuthenticationErrors,
                stats.AverageProcessingTimeMs);
        }
    }

    /// <summary>
    /// 获取当前统计信息
    /// </summary>
    public ConnectionStatistics GetCurrentStats()
    {
        lock (_lock)
        {
            var avgProcessingTime = _messageProcessingCount > 0
                ? _totalProcessingTimeMs / _messageProcessingCount
                : 0;

            var uptime = _connectionStartTime != DateTime.MinValue
                ? DateTime.UtcNow - _connectionStartTime
                : TimeSpan.Zero;

            return new ConnectionStatistics
            {
                MessagesSent = _messagesSent,
                MessagesReceived = _messagesReceived,
                BytesSent = _bytesSent,
                BytesReceived = _bytesReceived,
                ConnectionErrors = _connectionErrors,
                AuthenticationErrors = _authenticationErrors,
                AverageProcessingTimeMs = avgProcessingTime,
                Uptime = uptime,
                UptimeSeconds = uptime.TotalSeconds,
                MessagesPerSecond = uptime.TotalSeconds > 0 ? (double)_messagesReceived / uptime.TotalSeconds : 0,
                BytesPerSecond = uptime.TotalSeconds > 0 ? (double)_bytesReceived / uptime.TotalSeconds : 0
            };
        }
    }
}

/// <summary>
/// 连接统计数据
/// </summary>
public class ConnectionStatistics
{
    /// <summary>
    /// 发送的消息数
    /// </summary>
    public long MessagesSent { get; set; }

    /// <summary>
    /// 接收的消息数（有效）
    /// </summary>
    public long MessagesReceived { get; set; }

    /// <summary>
    /// 发送的字节数
    /// </summary>
    public long BytesSent { get; set; }

    /// <summary>
    /// 接收的字节数
    /// </summary>
    public long BytesReceived { get; set; }

    /// <summary>
    /// 连接错误数
    /// </summary>
    public long ConnectionErrors { get; set; }

    /// <summary>
    /// 认证错误数
    /// </summary>
    public long AuthenticationErrors { get; set; }

    /// <summary>
    /// 平均处理时间（毫秒）
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// 连接时长
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// 连接时长（秒）
    /// </summary>
    public double UptimeSeconds { get; set; }

    /// <summary>
    /// 每秒消息数
    /// </summary>
    public double MessagesPerSecond { get; set; }

    /// <summary>
    /// 每秒字节数
    /// </summary>
    public double BytesPerSecond { get; set; }
}

/// <summary>
/// 连接指标事件参数
/// </summary>
public class ConnectionMetricsEventArgs : EventArgs
{
    /// <summary>
    /// 连接统计
    /// </summary>
    public ConnectionStatistics Statistics { get; set; } = new();
}
