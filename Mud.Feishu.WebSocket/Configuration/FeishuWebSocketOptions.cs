// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket客户端配置选项
/// </summary>
public class FeishuWebSocketOptions
{
    private int _heartbeatIntervalMs = 30000;
    private int _reconnectDelayMs = 5000;
    private int _emptyQueueCheckIntervalMs = 100;
    private int _maxReconnectDelayMs = 30000;
    private int _healthCheckIntervalMs = 60000;
    private int _eventDeduplicationCacheExpirationMs = 30 * 60 * 1000;
    private int _eventDeduplicationCleanupIntervalMs = 5 * 60 * 1000;

    /// <summary>
    /// 自动重连，默认为true
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// 最大重连次数，默认为5次
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 5;

    /// <summary>
    /// 重连延迟时间（毫秒），默认为5000毫秒，最小为1000毫秒
    /// </summary>
    public int ReconnectDelayMs
    {
        get => _reconnectDelayMs;
        set => _reconnectDelayMs = Math.Max(1000, value);
    }

    /// <summary>
    /// 最大重连延迟时间（毫秒），默认为30000毫秒
    /// </summary>
    public int MaxReconnectDelayMs
    {
        get => _maxReconnectDelayMs;
        set => _maxReconnectDelayMs = Math.Max(_reconnectDelayMs, value);
    }

    /// <summary>
    /// 接收缓冲区大小（字节），默认为4KB
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 4096;

    /// <summary>
    /// 心跳间隔时间（毫秒），默认为30000毫秒，最小为1000毫秒
    /// </summary>
    public int HeartbeatIntervalMs
    {
        get => _heartbeatIntervalMs;
        set => _heartbeatIntervalMs = Math.Max(1000, value);
    }

    /// <summary>
    /// 连接超时时间（毫秒），默认为10000毫秒
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 10000;

    /// <summary>
    /// 是否启用日志记录，默认为true
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// 最大消息大小（字符数），默认为1MB
    /// </summary>
    public int MaxMessageSize { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// 多处理器模式下是否并行执行，默认为true
    /// 注意：此配置项暂未使用，预留用于未来功能扩展
    /// </summary>
    public bool ParallelMultiHandlers { get; set; } = true;

    /// <summary>
    /// 是否启用消息队列处理，默认为true
    /// </summary>
    public bool EnableMessageQueue { get; set; } = true;

    /// <summary>
    /// 消息队列最大容量，默认为1000条
    /// </summary>
    public int MessageQueueCapacity { get; set; } = 1000;

    /// <summary>
    /// 空队列检查间隔（毫秒），默认为100毫秒
    /// </summary>
    public int EmptyQueueCheckIntervalMs
    {
        get => _emptyQueueCheckIntervalMs;
        set => _emptyQueueCheckIntervalMs = Math.Max(10, value);
    }

    /// <summary>
    /// 最大二进制消息大小（字节），默认为10MB
    /// </summary>
    public long MaxBinaryMessageSize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// 健康检查间隔（毫秒），默认为60000毫秒
    /// </summary>
    public int HealthCheckIntervalMs
    {
        get => _healthCheckIntervalMs;
        set => _healthCheckIntervalMs = Math.Max(1000, value);
    }

    /// <summary>
    /// 是否启用事件去重，默认为true
    /// </summary>
    public bool EnableEventDeduplication { get; set; } = true;

    /// <summary>
    /// 是否启用分布式去重（需配置 IFeishuEventDistributedDeduplicator），默认为false
    /// </summary>
    public bool EnableDistributedDeduplication { get; set; } = false;

    /// <summary>
    /// 事件去重缓存过期时间（毫秒），默认为30分钟
    /// </summary>
    public int EventDeduplicationCacheExpirationMs
    {
        get => _eventDeduplicationCacheExpirationMs;
        set => _eventDeduplicationCacheExpirationMs = Math.Max(60000, value);
    }

    /// <summary>
    /// 事件去重缓存清理间隔（毫秒），默认为5分钟
    /// </summary>
    public int EventDeduplicationCleanupIntervalMs
    {
        get => _eventDeduplicationCleanupIntervalMs;
        set => _eventDeduplicationCleanupIntervalMs = Math.Max(60000, value);
    }

    /// <summary>
    /// 验证配置项的有效性
    /// </summary>
    /// <exception cref="InvalidOperationException">当配置项无效时抛出</exception>
    public void Validate()
    {
        if (MaxReconnectAttempts < 0)
            throw new InvalidOperationException("MaxReconnectAttempts必须大于等于0");

        if (ReconnectDelayMs < 1000)
            throw new InvalidOperationException("ReconnectDelayMs必须至少为1000毫秒");

        if (MaxReconnectDelayMs < ReconnectDelayMs)
            throw new InvalidOperationException("MaxReconnectDelayMs必须大于等于ReconnectDelayMs");

        if (ReceiveBufferSize < 1024)
            throw new InvalidOperationException("ReceiveBufferSize必须至少为1024字节");

        if (HeartbeatIntervalMs < 1000)
            throw new InvalidOperationException("HeartbeatIntervalMs必须至少为1000毫秒");

        if (ConnectionTimeoutMs < 1000)
            throw new InvalidOperationException("ConnectionTimeoutMs必须至少为1000毫秒");

        if (MaxMessageSize < 1024)
            throw new InvalidOperationException("MaxMessageSize必须至少为1024字符");

        if (MessageQueueCapacity < 1)
            throw new InvalidOperationException("MessageQueueCapacity必须至少为1");

        if (MaxBinaryMessageSize < 1024)
            throw new InvalidOperationException("MaxBinaryMessageSize必须至少为1024字节");

        if (EventDeduplicationCacheExpirationMs < 60000)
            throw new InvalidOperationException("EventDeduplicationCacheExpirationMs必须至少为60000毫秒");

        if (EventDeduplicationCleanupIntervalMs < 60000)
            throw new InvalidOperationException("EventDeduplicationCleanupIntervalMs必须至少为60000毫秒");
    }
}

