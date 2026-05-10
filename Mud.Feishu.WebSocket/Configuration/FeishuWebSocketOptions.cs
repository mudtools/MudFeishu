// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 消息队列背压策略
/// </summary>
public enum QueueBackpressureStrategy
{
    /// <summary>
    /// 丢弃最旧的消息（默认）
    /// </summary>
    DropOldest,

    /// <summary>
    /// 丢弃新消息
    /// </summary>
    DropNewest,

    /// <summary>
    /// 阻塞等待直到队列有空间（可能导致延迟）
    /// </summary>
    Block
}

/// <summary>
/// 飞书WebSocket客户端配置选项
/// </summary>
public class FeishuWebSocketOptions
{
    private int _heartbeatIntervalMs = 25000;
    private int _reconnectDelayMs = 5000;
    private int _emptyQueueCheckIntervalMs = 100;
    private int _maxReconnectDelayMs = 30000;
    private int _healthCheckIntervalMs = 60000;
    private int _maxConcurrentMessageProcessing = 10; // 默认最大并发消息处理数

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
    /// 最大重连总时间，默认为30分钟
    /// <para>超过此时间后将停止重连尝试</para>
    /// </summary>
    public TimeSpan MaxTotalReconnectTime { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 重连冷却时间，默认为5秒
    /// <para>两次重连尝试之间的最小间隔时间，防止过于频繁的重连</para>
    /// </summary>
    public TimeSpan ReconnectCooldownTime { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 是否启用重连指标收集，默认为true
    /// </summary>
    public bool EnableReconnectMetrics { get; set; } = true;

    /// <summary>
    /// 初始接收缓冲区大小（字节），默认为4KB
    /// <para>仅用于初始化WebSocket接收缓冲区，实际消息大小会动态调整</para>
    /// </summary>
    public int InitialReceiveBufferSize { get; set; } = 4096;

    /// <summary>
    /// 心跳间隔时间（毫秒），默认为25000毫秒（飞书建议25秒内），最小为5000毫秒
    /// </summary>
    public int HeartbeatIntervalMs
    {
        get => _heartbeatIntervalMs;
        set => _heartbeatIntervalMs = Math.Max(5000, value); // 最小5秒，避免过于频繁的心跳
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
    /// 消息大小限制配置
    /// </summary>
    public MessageSizeLimits MessageSizeLimits { get; set; } = new();

    /// <summary>
    /// 是否启用消息队列处理，默认为true
    /// </summary>
    public bool EnableMessageQueue { get; set; } = true;

    /// <summary>
    /// 消息队列最大容量，默认为1000条
    /// </summary>
    public int MessageQueueCapacity { get; set; } = 1000;

    /// <summary>
    /// 消息队列背压策略，默认为丢弃最旧的消息
    /// </summary>
    public QueueBackpressureStrategy BackpressureStrategy { get; set; } = QueueBackpressureStrategy.DropOldest;

    /// <summary>
    /// 背压阻塞等待超时时间（毫秒），仅当 BackpressureStrategy 为 Block 时有效，默认为5000毫秒
    /// </summary>
    public int BackpressureBlockTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// 空队列检查间隔（毫秒），默认为100毫秒
    /// </summary>
    public int EmptyQueueCheckIntervalMs
    {
        get => _emptyQueueCheckIntervalMs;
        set => _emptyQueueCheckIntervalMs = Math.Max(10, value);
    }

    /// <summary>
    /// 健康检查间隔（毫秒），默认为60000毫秒
    /// </summary>
    public int HealthCheckIntervalMs
    {
        get => _healthCheckIntervalMs;
        set => _healthCheckIntervalMs = Math.Max(1000, value);
    }

    /// <summary>
    /// 最大并发消息处理数，默认为10
    /// <para>用于控制同时处理的消息数量，防止线程池耗尽</para>
    /// </summary>
    public int MaxConcurrentMessageProcessing
    {
        get => _maxConcurrentMessageProcessing;
        set => _maxConcurrentMessageProcessing = Math.Max(1, value);
    }

    /// <summary>
    /// 是否验证SSL证书，默认为true（生产环境建议为true）
    /// </summary>
    public bool ValidateServerCertificate { get; set; } = true;

    /// <summary>
    /// 是否允许自签名证书，默认为false（生产环境建议为false）
    /// </summary>
    public bool AllowSelfSignedCertificates { get; set; } = false;

    /// <summary>
    /// 自定义证书验证回调（可选）
    /// <para>如果设置，将使用此回调进行证书验证</para>
    /// </summary>
    public System.Net.Security.RemoteCertificateValidationCallback? CustomCertificateValidationCallback { get; set; }

    /// <summary>
    /// 事件去重配置
    /// </summary>
    public EventDeduplicationOptions EventDeduplication { get; set; } = new();

    /// <summary>
    /// 访问令牌的有效期，默认为2小时
    /// <para>飞书 TenantAccessToken 默认有效期为2小时</para>
    /// </summary>
    public TimeSpan? TokenRefreshInterval { get; set; }

    /// <summary>
    /// 提前刷新令牌的时间，默认为5分钟
    /// <para>在令牌过期前提前刷新，避免使用即将过期的令牌</para>
    /// </summary>
    public TimeSpan? TokenRefreshAhead { get; set; }

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

        if (InitialReceiveBufferSize < 1024)
            throw new InvalidOperationException("InitialReceiveBufferSize必须至少为1024字节");

        if (HeartbeatIntervalMs < 5000)
            throw new InvalidOperationException("HeartbeatIntervalMs必须至少为5000毫秒");

        if (ConnectionTimeoutMs < 1000)
            throw new InvalidOperationException("ConnectionTimeoutMs必须至少为1000毫秒");

        if (AutoReconnect && ReconnectDelayMs > ConnectionTimeoutMs)
            throw new InvalidOperationException("ReconnectDelayMs不应大于ConnectionTimeoutMs，否则重连将在连接超时后才触发");

        if (MessageQueueCapacity < 1)
            throw new InvalidOperationException("MessageQueueCapacity必须至少为1");

        if (MaxConcurrentMessageProcessing < 1)
            throw new InvalidOperationException("MaxConcurrentMessageProcessing必须至少为1");

        // 验证消息大小限制配置
        if (MessageSizeLimits.MaxTextMessageSize < 1024)
            throw new InvalidOperationException("MessageSizeLimits.MaxTextMessageSize必须至少为1024字符");

        if (MessageSizeLimits.MaxBinaryMessageSize < 1024)
            throw new InvalidOperationException("MessageSizeLimits.MaxBinaryMessageSize必须至少为1024字节");

        // 去重配置验证
        if (EventDeduplication.Mode == EventDeduplicationMode.None)
        {
            var hasCustomCacheSettings = EventDeduplication.CacheExpirationMs != EventDeduplicationOptions.DefaultCacheExpirationMs
                || EventDeduplication.CleanupIntervalMs != EventDeduplicationOptions.DefaultCleanupIntervalMs;

            var message = "EventDeduplication.Mode 设置为 None，事件去重功能已关闭，可能导致重复处理事件。";

            if (hasCustomCacheSettings)
                message += " 同时，CacheExpirationMs 和 CleanupIntervalMs 配置不会生效。";

            message += "生产环境强烈建议启用至少一种去重机制（InMemory 或 Redis）以避免重复处理事件。";

            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// 返回配置的字符串表示
    /// </summary>
    public override string ToString()
    {
        return $"FeishuWebSocketOptions {{ AutoReconnect: {AutoReconnect}, MaxReconnectAttempts: {MaxReconnectAttempts}, ReconnectDelayMs: {ReconnectDelayMs}, MaxTotalReconnectTime: {MaxTotalReconnectTime}, ReconnectCooldownTime: {ReconnectCooldownTime}, HeartbeatIntervalMs: {HeartbeatIntervalMs}, EnableLogging: {EnableLogging}, EventDeduplicationMode: {EventDeduplication.Mode} }}";
    }
}

