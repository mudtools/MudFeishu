// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
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
    private int _heartbeatIntervalMs = 25000;
    private int _reconnectDelayMs = 5000;
    private int _maxReconnectDelayMs = 30000;
    private int _healthCheckIntervalMs = 60000;
    private int _messageHandlerTimeoutMs = 30000; // 默认消息处理超时30秒

    /// <summary>
    /// 飞书应用 AppKey，用于指标维度区分。
    /// </summary>
    public string AppKey { get; set; } = "default";

    /// <summary>
    /// 自动重连，默认为true
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// 最大重连次数，默认为5次。
    /// 设为 0 表示无限重连（仅受 MaxTotalReconnectTime 限制）。
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
    /// 健康检查间隔（毫秒），默认为60000毫秒
    /// </summary>
    public int HealthCheckIntervalMs
    {
        get => _healthCheckIntervalMs;
        set => _healthCheckIntervalMs = Math.Max(1000, value);
    }


    /// <summary>
    /// 单条消息处理超时时间（毫秒），默认为30000毫秒（30秒）
    /// <para>当消息处理器执行时间超过此阈值时，将取消处理并记录警告日志</para>
    /// <para>设为 0 表示不限制超时</para>
    /// </summary>
    public int MessageHandlerTimeoutMs
    {
        get => _messageHandlerTimeoutMs;
        set => _messageHandlerTimeoutMs = Math.Max(0, value);
    }

    /// <summary>
    /// 是否允许不安全的 WebSocket 连接（ws://），默认为 false。
    /// 生产环境应始终使用 wss://，仅在开发/测试环境启用此项。
    /// </summary>
    public bool AllowInsecureWebSocket { get; set; } = false;

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
    /// <remarks>
    /// 此属性仅支持代码配置，无法通过 JSON 配置文件（appsettings.json）设置。
    /// 请通过 <c>ConfigureOptions</c> 或 <c>ConfigureFrom</c> 后的代码配置方式设置。
    /// </remarks>
    public System.Net.Security.RemoteCertificateValidationCallback? CustomCertificateValidationCallback { get; set; }

    /// <summary>
    /// 消息序号跳跃阈值，超过此值认为消息丢失。
    /// <para>飞书 SeqID 是全局计数器，不同应用/连接共享同一序号空间，
    /// 因此序号跳跃是正常现象（跳跃量可达数万甚至数十万）。</para>
    /// <para>设为 0 表示禁用跳跃检测（推荐），仅保留重复检测和回退检测。</para>
    /// 默认为 0（禁用）。
    /// </summary>
    public ulong SequenceGapThreshold { get; set; } = 0;

    /// <summary>
    /// 事件去重配置
    /// </summary>
    public EventDeduplicationOptions EventDeduplication { get; set; } = new();


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

        if (HeartbeatIntervalMs > 30000)
            throw new InvalidOperationException("HeartbeatIntervalMs不应超过30000毫秒，飞书服务端可能在此时间内断开连接");

        if (ConnectionTimeoutMs < 1000)
            throw new InvalidOperationException("ConnectionTimeoutMs必须至少为1000毫秒");

        // 注：ReconnectDelayMs 与 ConnectionTimeoutMs 语义独立（前者为两次重连尝试间的等待，后者为单次 TCP 握手超时），
        // 不存在必然的约束关系，故移除交叉校验。


        // 验证消息大小限制配置
        if (MessageSizeLimits.MaxTextMessageSize < 1024)
            throw new InvalidOperationException("MessageSizeLimits.MaxTextMessageSize必须至少为1024字符");

        if (MessageSizeLimits.MaxBinaryMessageSize < 1024)
            throw new InvalidOperationException("MessageSizeLimits.MaxBinaryMessageSize必须至少为1024字节");

        // 去重配置验证
        if (EventDeduplication.Mode == EventDeduplicationMode.None)
        {
            var hasCustomCacheSettings = EventDeduplication.CacheExpiration != EventDeduplicationOptions.DefaultCacheExpiration
                || EventDeduplication.CleanupInterval != EventDeduplicationOptions.DefaultCleanupInterval;

            if (hasCustomCacheSettings)
                throw new InvalidOperationException(
                    "EventDeduplication.Mode 设置为 None 时，CacheExpiration 和 CleanupInterval 配置不会生效。" +
                    "请移除缓存配置，或将 Mode 设置为 InMemory 或 Redis。");

            // None 模式不阻止启动，仅在文档中建议生产环境启用去重
            // 如需强制警告，可使用日志而非异常
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

