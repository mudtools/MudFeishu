// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket客户端配置选项
/// </summary>
/// <remarks>
/// R4：重连使用 <see cref="Reconnect"/>，证书/协议安全使用 <see cref="Certificate"/>。
/// 日志请使用 <c>Logging:LogLevel:Mud.Feishu.WebSocket</c>，不再提供 EnableLogging 开关。
/// </remarks>
public class FeishuWebSocketOptions
{
    private int _heartbeatIntervalMs = 25000;
    private int _healthCheckIntervalMs = 60000;
    private int _messageHandlerTimeoutMs = 30000;
    private int _authTimeoutMs = DefaultAuthTimeoutMs;

    /// <summary>认证响应超时默认值（毫秒）：30 秒</summary>
    public const int DefaultAuthTimeoutMs = 30000;

    /// <summary>
    /// 认证响应超时时间（毫秒），默认 30000；0 或负数回退默认值。
    /// </summary>
    public int AuthTimeoutMs
    {
        get => _authTimeoutMs;
        set => _authTimeoutMs = value <= 0 ? DefaultAuthTimeoutMs : value;
    }

    /// <summary>飞书应用 AppKey，用于指标维度区分</summary>
    public string AppKey { get; set; } = "default";

    /// <summary>重连嵌套配置</summary>
    public WebSocketReconnectOptions Reconnect { get; set; } = new();

    /// <summary>证书与协议安全嵌套配置</summary>
    public WebSocketCertificateOptions Certificate { get; set; } = new();

    /// <summary>是否启用重连指标收集，默认 true</summary>
    public bool EnableReconnectMetrics { get; set; } = true;

    /// <summary>初始接收缓冲区大小（字节），默认 4096（实现旋钮，文档主路径不展示）</summary>
    public int InitialReceiveBufferSize { get; set; } = 4096;

    /// <summary>心跳间隔（毫秒），默认 25000，最小 5000</summary>
    public int HeartbeatIntervalMs
    {
        get => _heartbeatIntervalMs;
        set => _heartbeatIntervalMs = Math.Max(5000, value);
    }

    /// <summary>连接超时（毫秒），默认 10000</summary>
    public int ConnectionTimeoutMs { get; set; } = 10000;

    /// <summary>消息大小限制</summary>
    public MessageSizeLimits MessageSizeLimits { get; set; } = new();

    /// <summary>健康检查间隔（毫秒），默认 60000，最小 1000</summary>
    public int HealthCheckIntervalMs
    {
        get => _healthCheckIntervalMs;
        set => _healthCheckIntervalMs = Math.Max(1000, value);
    }

    /// <summary>单条消息处理超时（毫秒），默认 30000；0 表示不限制</summary>
    public int MessageHandlerTimeoutMs
    {
        get => _messageHandlerTimeoutMs;
        set => _messageHandlerTimeoutMs = Math.Max(0, value);
    }

    /// <summary>认证闸门等待上限（毫秒），默认 0（关闭）</summary>
    public int AuthGateTimeoutMs
    {
        get => _authGateTimeoutMs;
        set => _authGateTimeoutMs = Math.Max(0, value);
    }
    private int _authGateTimeoutMs;

    /// <summary>消息序号跳跃阈值，0=禁用跳跃检测（推荐）</summary>
    public ulong SequenceGapThreshold { get; set; }

    /// <summary>
    /// WebSocket 主机白名单（P2-15 修复引入：防御服务端下发端点被篡改导致的 SSRF）。
    /// <para>分号分隔；支持 <c>*.feishu.cn</c> 通配后缀（匹配任意层级子域）与精确主机名；大小写不敏感。</para>
    /// <para>
    /// 默认 <c>*.feishu.cn;*.larksuite.com</c>（飞书/飞书海外域名，端点由服务端 API 下发）。
    /// 设为<b>空字符串或仅空白</b>表示不限制（恢复历史行为），例如连接本地网关、自建代理或测试桩时。
    /// </para>
    /// </summary>
    public string AllowedHostSuffixes { get; set; } = "*.feishu.cn;*.larksuite.com";

    /// <summary>最大并发事件处理器数量，默认 32；0/负数=无限制</summary>
    public int MaxConcurrentHandlers { get; set; } = 32;

    /// <summary>协议 Ping/Pong 保活间隔，默认 20 秒；0=禁用；允许 0 或 5–300 秒</summary>
    public TimeSpan ProtocolKeepAliveInterval { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>事件去重配置（WebSocket 传输侧；统一节存在时 FeishuDeduplication 优先）</summary>
    public EventDeduplicationOptions EventDeduplication { get; set; } = new();

    /// <summary>
    /// 事件 EventId 为空/缺失时是否拒绝处理（true=fail-closed），默认 true。
    /// </summary>
    /// <remarks>
    /// 空 EventId 无法有效去重：内存后端对 null 跳过去重、对空字符串作为同键碰撞
    /// （首条标记后后续空 ID 事件被静默跳过），服务端重发将失去幂等保护。
    /// 飞书正常事件必带 event_id，空值只出现在畸形或恶意流量中。
    /// 对齐 Webhook 通道 <c>FeishuWebhookOptions.RejectEmptyIdentifiers</c>（WHF-05）。
    /// </remarks>
    public bool RejectEmptyEventIds { get; set; } = true;

    /// <summary>
    /// 未注册 eventType 的事件是否静默忽略（记 Debug + unhandled 指标），默认 <c>false</c>（保守，保持 WS 现状回退默认处理器）。
    /// </summary>
    /// <remarks>
    /// 对齐 Webhook 通道 <c>FeishuWebhookOptions.IgnoreUnknownEventTypes</c>（WHF-09，默认 true）。
    /// WS 默认 false 是行为兼容选择；推荐新宿主设为 true 以与 Webhook 一致。
    /// </remarks>
    public bool IgnoreUnknownEventTypes { get; set; }

    /// <summary>从配置节回填 R3 前的扁平连接/证书键（仅 JSON 兼容）</summary>
    public void ApplyLegacyFlatKeys(IConfigurationSection section)
    {
        if (section is null || !section.Exists())
            return;

        Reconnect ??= new WebSocketReconnectOptions();
        Certificate ??= new WebSocketCertificateOptions();

        if (bool.TryParse(section["AutoReconnect"], out var auto))
            Reconnect.Auto = auto;
        if (int.TryParse(section["MaxReconnectAttempts"], out var maxAttempts))
            Reconnect.MaxAttempts = maxAttempts;
        if (int.TryParse(section["MaxAuthRetryAttempts"], out var maxAuth))
            Reconnect.MaxAuthRetryAttempts = maxAuth;
        if (int.TryParse(section["ReconnectDelayMs"], out var delay))
            Reconnect.BaseDelayMs = Math.Max(1000, delay);
        if (int.TryParse(section["MaxReconnectDelayMs"], out var maxDelay))
            Reconnect.MaxDelayMs = Math.Max(Reconnect.BaseDelayMs, maxDelay);
        if (TimeSpan.TryParse(section["MaxTotalReconnectTime"], out var budget))
            Reconnect.TotalBudget = budget;
        if (TimeSpan.TryParse(section["ReconnectCooldownTime"], out var cooldown))
            Reconnect.Cooldown = cooldown;
        if (bool.TryParse(section["AllowInsecureWebSocket"], out var insecure))
            Certificate.AllowInsecureWebSocket = insecure;
        if (bool.TryParse(section["ValidateServerCertificate"], out var validate))
            Certificate.ValidateServerCertificate = validate;
        if (bool.TryParse(section["AllowSelfSignedCertificates"], out var selfSigned))
            Certificate.AllowSelfSignedCertificates = selfSigned;
        if (bool.TryParse(section["AllowCertificateNameMismatch"], out var nameMismatch))
            Certificate.AllowCertificateNameMismatch = nameMismatch;
        if (bool.TryParse(section["EnableLogging"], out _))
        {
            // EnableLogging 已移除；兼容读取但忽略（日志由 ILogger 级别控制）。
        }
    }

    /// <summary>
    /// 验证配置项的有效性
    /// </summary>
    /// <exception cref="InvalidOperationException">当配置项无效时抛出</exception>
    public void Validate()
    {
        Reconnect ??= new WebSocketReconnectOptions();
        Certificate ??= new WebSocketCertificateOptions();
        MessageSizeLimits ??= new MessageSizeLimits();

        if (MaxConcurrentHandlers < -1)
            throw new InvalidOperationException("MaxConcurrentHandlers必须为-1（无限制）或非负整数");

        if (Reconnect.MaxAttempts < 0)
            throw new InvalidOperationException("Reconnect.MaxAttempts (原 MaxReconnectAttempts) 必须大于等于0");

        if (Reconnect.BaseDelayMs < 1000)
            throw new InvalidOperationException("Reconnect.BaseDelayMs (原 ReconnectDelayMs) 必须至少为1000毫秒");

        if (Reconnect.MaxDelayMs < Reconnect.BaseDelayMs)
            throw new InvalidOperationException("Reconnect.MaxDelayMs (原 MaxReconnectDelayMs) 必须大于等于Reconnect.BaseDelayMs");

        if (InitialReceiveBufferSize < 1024)
            throw new InvalidOperationException("InitialReceiveBufferSize必须至少为1024字节");

        if (InitialReceiveBufferSize > 1024 * 1024)
            throw new InvalidOperationException("InitialReceiveBufferSize不应超过1MB，过大将造成不必要的内存占用");

        if (Reconnect.TotalBudget <= TimeSpan.Zero)
            throw new InvalidOperationException("Reconnect.TotalBudget必须大于0");

        if (Reconnect.Cooldown < TimeSpan.Zero)
            throw new InvalidOperationException("Reconnect.Cooldown不能为负数");

        if (Reconnect.MaxAuthRetryAttempts < 0)
            throw new InvalidOperationException("Reconnect.MaxAuthRetryAttempts必须大于等于0");

        if (HeartbeatIntervalMs < 5000)
            throw new InvalidOperationException("HeartbeatIntervalMs必须至少为5000毫秒");

        if (HeartbeatIntervalMs > 30000)
            throw new InvalidOperationException("HeartbeatIntervalMs不应超过30000毫秒，飞书服务端可能在此时间内断开连接");

        if (ConnectionTimeoutMs < 1000)
            throw new InvalidOperationException("ConnectionTimeoutMs必须至少为1000毫秒");

        if (ProtocolKeepAliveInterval != TimeSpan.Zero)
        {
            if (ProtocolKeepAliveInterval < TimeSpan.FromSeconds(5))
                throw new InvalidOperationException("ProtocolKeepAliveInterval 必须为 0（禁用）或至少 5 秒");
            if (ProtocolKeepAliveInterval > TimeSpan.FromSeconds(300))
                throw new InvalidOperationException("ProtocolKeepAliveInterval 不应超过 300 秒");
        }

        if (MessageSizeLimits.MaxTextMessageSize < 1024)
            throw new InvalidOperationException("MessageSizeLimits.MaxTextMessageSize必须至少为1024字符");

        if (MessageSizeLimits.MaxBinaryMessageSize < 1024)
            throw new InvalidOperationException("MessageSizeLimits.MaxBinaryMessageSize必须至少为1024字节");

        // P1-4 修复：新增字节维度的边界校验（0 = 按 3 × MaxTextMessageSize 自动推导）
        if (MessageSizeLimits.MaxTextMessageBytes < 0)
            throw new InvalidOperationException(
                "MessageSizeLimits.MaxTextMessageBytes必须为非负整数（0 表示按 3 × MaxTextMessageSize 自动推导）");

        // 去重配置验证
        if (EventDeduplication.Mode == EventDeduplicationMode.None)
        {
            var hasCustomCacheSettings = EventDeduplication.CacheExpiration != EventDeduplicationOptions.DefaultCacheExpiration
                || EventDeduplication.CleanupInterval != EventDeduplicationOptions.DefaultCleanupInterval;

            if (hasCustomCacheSettings)
                throw new InvalidOperationException(
                    "EventDeduplication.Mode 设置为 None 时，CacheExpiration 和 CleanupInterval 配置不会生效。" +
                    "请移除缓存配置，或将 Mode 设置为 InMemory 或 Redis。");
        }

        ValidateCertificateOptions();
    }

    /// <summary>
    /// 校验证书/协议安全组合（C3/R4）。
    /// </summary>
    public void ValidateCertificateOptions()
    {
        Certificate ??= new WebSocketCertificateOptions();

        if (Certificate.Mode == CertificateValidationMode.Strict && Certificate.AllowSelfSignedCertificates)
            throw new InvalidOperationException(
                "Certificate.Mode=Strict 时不得 AllowSelfSignedCertificates=true。请改 Mode=Dev 或关闭 AllowSelfSignedCertificates。");

        if (Certificate.Mode == CertificateValidationMode.Strict && Certificate.AllowCertificateNameMismatch)
            throw new InvalidOperationException(
                "Certificate.Mode=Strict 时不得 AllowCertificateNameMismatch=true。请改 Mode=Dev 或关闭该开关。");

        if (Certificate.Mode == CertificateValidationMode.Custom && Certificate.CustomCallback is null)
            throw new InvalidOperationException(
                "Certificate.Mode=Custom 时必须提供 Certificate.CustomCallback。");
    }

    public override string ToString()
    {
        return $"FeishuWebSocketOptions {{ Reconnect.Auto: {Reconnect?.Auto}, Reconnect.MaxAttempts: {Reconnect?.MaxAttempts}, Reconnect.BaseDelayMs: {Reconnect?.BaseDelayMs}, Reconnect.TotalBudget: {Reconnect?.TotalBudget}, Certificate.Mode: {Certificate?.Mode}, HeartbeatIntervalMs: {HeartbeatIntervalMs}, EventDeduplicationMode: {EventDeduplication?.Mode} }}";
    }
}
