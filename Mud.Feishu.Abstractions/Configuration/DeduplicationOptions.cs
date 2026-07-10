// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Configuration;

/// <summary>
/// 去重配置选项
/// 集中管理事件去重相关的配置参数
/// </summary>
/// <remarks>
/// 此配置类统一管理内存去重和分布式去重的通用配置。
/// 建议根据实际业务场景调整以下参数：
/// <list type="bullet">
///   <item><description>CacheExpiration: 应大于飞书官方事件重试窗口期（通常 1-24 小时）</description></item>
///   <item><description>ProcessingTimeout: 应根据业务最长处理时间设置</description></item>
///   <item><description>AllowProcessingOnFallback: 高可靠性场景建议设为 false</description></item>
/// </list>
/// </remarks>
public class DeduplicationOptions
{
    /// <summary>
    /// 缓存过期时间
    /// <para>此配置统一应用于内存去重和 Redis 分布式去重。
    /// RedisOptions.EventCacheExpiration / SeqIdCacheExpiration 已标记为 [Obsolete]，
    /// 建议通过此属性统一配置。</para>
    /// </summary>
    /// <remarks>
    /// 建议设置为大于飞书官方事件重试窗口期，避免长延时场景下的重复处理。
    /// 飞书官方重试窗口通常为 1-24 小时，建议设置为 48 小时。
    /// </remarks>
    public TimeSpan CacheExpiration
    {
        get => _cacheExpiration;
        set => _cacheExpiration = value >= TimeSpan.FromMinutes(1) ? value : TimeSpan.FromMinutes(1);
    }
    private TimeSpan _cacheExpiration = TimeSpan.FromMilliseconds(Consts.DefaultCacheExpirationMs);

    /// <summary>
    /// 处理中超时时间
    /// </summary>
    /// <remarks>
    /// 当事件标记为"处理中"后，超过此时间未完成，将允许重新处理。
    /// 应根据业务最长处理时间设置，建议设置为业务处理时间的 2-3 倍。
    /// </remarks>
    public TimeSpan ProcessingTimeout
    {
        get => _processingTimeout;
        set => _processingTimeout = value >= TimeSpan.FromSeconds(10) ? value : TimeSpan.FromSeconds(10);
    }
    private TimeSpan _processingTimeout = TimeSpan.FromMilliseconds(Consts.DefaultProcessingTimeoutMs);

    /// <summary>
    /// 缓存清理间隔（仅内存模式）
    /// </summary>
    /// <remarks>
    /// 定期清理过期的缓存条目，避免内存泄漏。
    /// </remarks>
    public TimeSpan CleanupInterval
    {
        get => _cleanupInterval;
        set => _cleanupInterval = value >= TimeSpan.FromSeconds(30) ? value : TimeSpan.FromSeconds(30);
    }
    private TimeSpan _cleanupInterval = TimeSpan.FromMilliseconds(Consts.DefaultCleanupIntervalMs);

    /// <summary>
    /// 降级策略：是否在 Redis 失败时允许处理事件（仅分布式模式）
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>true: Redis 失败时降级到内存去重并允许处理（高可用性，可能重复处理）</description></item>
    ///   <item><description>false: Redis 失败时拒绝处理（高可靠性，可能丢失事件）</description></item>
    /// </list>
    /// 对于关键业务，建议设为 false 以确保数据一致性。
    /// </remarks>
    public bool AllowProcessingOnFallback { get; set; } = true;

    /// <summary>
    /// 最大重试次数（仅分布式模式）
    /// </summary>
    /// <remarks>
    /// Redis 操作失败时的最大重试次数。
    /// </remarks>
    public int MaxRetryCount
    {
        get => _maxRetryCount;
        set => _maxRetryCount = Math.Max(0, Math.Min(value, 10));
    }
    private int _maxRetryCount = Consts.DefaultDeduplicationRetryCount;

    /// <summary>
    /// 初始重试延迟（仅分布式模式）
    /// </summary>
    /// <remarks>
    /// 首次重试前的等待时间，后续重试将使用指数退避策略。
    /// </remarks>
    public TimeSpan InitialRetryDelay
    {
        get => _initialRetryDelay;
        set => _initialRetryDelay = value >= TimeSpan.FromMilliseconds(100) ? value : TimeSpan.FromMilliseconds(100);
    }
    private TimeSpan _initialRetryDelay = TimeSpan.FromMilliseconds(Consts.DefaultDeduplicationInitialRetryDelayMs);

    /// <summary>
    /// 最大重试延迟（仅分布式模式）
    /// </summary>
    /// <remarks>
    /// 指数退避策略的最大延迟时间上限。
    /// </remarks>
    public TimeSpan MaxRetryDelay
    {
        get => _maxRetryDelay;
        set => _maxRetryDelay = value >= TimeSpan.FromSeconds(1) ? value : TimeSpan.FromSeconds(1);
    }
    private TimeSpan _maxRetryDelay = TimeSpan.FromMilliseconds(Consts.DefaultDeduplicationMaxRetryDelayMs);

    /// <summary>
    /// Redis 键前缀（仅分布式模式）
    /// <para>此配置统一应用于 Redis 去重键前缀。
    /// RedisOptions.EventKeyPrefix 已标记为 [Obsolete]，
    /// 建议通过此属性统一配置。</para>
    /// </summary>
    /// <remarks>
    /// 用于区分不同应用或环境的 Redis 键。
    /// 格式：{prefix}{appKey}:{eventId}
    /// </remarks>
    public string KeyPrefix
    {
        get => _keyPrefix;
        set => _keyPrefix = string.IsNullOrEmpty(value) ? Consts.DefaultEventKeyPrefix : value;
    }
    private string _keyPrefix = Consts.DefaultEventKeyPrefix;

    /// <summary>
    /// 最大缓存容量（仅内存模式）
    /// </summary>
    /// <remarks>
    /// 内存缓存的最大条目数，超过此数量将触发清理。
    /// 设置为 0 表示不限制。
    /// </remarks>
    public int MaxCacheSize
    {
        get => _maxCacheSize;
        set => _maxCacheSize = Math.Max(0, value);
    }
    private int _maxCacheSize = Consts.DefaultMaxCacheSize;

    /// <summary>
    /// 是否启用详细日志
    /// </summary>
    /// <remarks>
    /// 启用后将记录每次去重检查的详细日志，用于调试。
    /// 生产环境建议关闭以减少日志量。
    /// </remarks>
    public bool EnableVerboseLogging { get; set; } = false;

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static DeduplicationOptions Default => new();

    /// <summary>
    /// 创建高可靠性配置
    /// </summary>
    /// <remarks>
    /// 适用于关键业务场景：
    /// - 更长的缓存过期时间
    /// - 更短的处理超时
    /// - Redis 失败时拒绝处理
    /// </remarks>
    public static DeduplicationOptions HighReliability => new()
    {
        CacheExpiration = TimeSpan.FromHours(72),
        ProcessingTimeout = TimeSpan.FromMinutes(5),
        AllowProcessingOnFallback = false,
        MaxRetryCount = 5,
        EnableVerboseLogging = false
    };

    /// <summary>
    /// 创建高可用性配置
    /// </summary>
    /// <remarks>
    /// 适用于高吞吐场景：
    /// - 标准缓存过期时间
    /// - 较长的处理超时
    /// - Redis 失败时降级处理
    /// </remarks>
    public static DeduplicationOptions HighAvailability => new()
    {
        CacheExpiration = TimeSpan.FromHours(48),
        ProcessingTimeout = TimeSpan.FromMinutes(15),
        AllowProcessingOnFallback = true,
        MaxRetryCount = 3,
        EnableVerboseLogging = false
    };
}
