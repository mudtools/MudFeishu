// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Configuration;

/// <summary>
/// 去重配置选项（通用内存/高级参数）。
/// </summary>
/// <remarks>
/// <para>
/// R4：主路径仅消费 <see cref="CacheExpiration"/> / <see cref="ProcessingTimeout"/> /
/// <see cref="CleanupInterval"/> / <see cref="KeyPrefix"/> / <see cref="MaxCacheSize"/>。
/// 分布式失败语义（AllowProcessingOnFallback / 重试延迟等）在 Redis 主路径<strong>不消费</strong>，
/// 已从公共 API 移除；事件失败重试请使用 <c>FailedEventRetryOptions</c>。
/// </para>
/// <para>推荐配置入口：<c>FeishuDeduplication</c> 统一节（见 FeishuDeduplicationOptions）。</para>
/// </remarks>
public class DeduplicationOptions
{
    /// <summary>
    /// 缓存过期时间（默认 48h，引用 Consts）
    /// </summary>
    public TimeSpan CacheExpiration
    {
        get => _cacheExpiration;
        set => _cacheExpiration = value >= TimeSpan.FromMinutes(1) ? value : TimeSpan.FromMinutes(1);
    }
    private TimeSpan _cacheExpiration = TimeSpan.FromMilliseconds(Consts.DefaultCacheExpirationMs);

    /// <summary>
    /// 处理中超时时间（默认 10min，引用 Consts）
    /// </summary>
    public TimeSpan ProcessingTimeout
    {
        get => _processingTimeout;
        set => _processingTimeout = value >= TimeSpan.FromSeconds(10) ? value : TimeSpan.FromSeconds(10);
    }
    private TimeSpan _processingTimeout = TimeSpan.FromMilliseconds(Consts.DefaultProcessingTimeoutMs);

    /// <summary>
    /// 缓存清理间隔（仅内存模式，默认 5min）
    /// </summary>
    public TimeSpan CleanupInterval
    {
        get => _cleanupInterval;
        set => _cleanupInterval = value >= TimeSpan.FromSeconds(30) ? value : TimeSpan.FromSeconds(30);
    }
    private TimeSpan _cleanupInterval = TimeSpan.FromMilliseconds(Consts.DefaultCleanupIntervalMs);

    /// <summary>
    /// Redis/事件键前缀（默认 Consts.DefaultEventKeyPrefix）
    /// </summary>
    public string KeyPrefix
    {
        get => _keyPrefix;
        set => _keyPrefix = string.IsNullOrEmpty(value) ? Consts.DefaultEventKeyPrefix : value;
    }
    private string _keyPrefix = Consts.DefaultEventKeyPrefix;

    /// <summary>
    /// 最大缓存容量（仅内存模式，默认 100000）
    /// </summary>
    public int MaxCacheSize
    {
        get => _maxCacheSize;
        set => _maxCacheSize = Math.Max(0, value);
    }
    private int _maxCacheSize = Consts.DefaultMaxCacheSize;

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static DeduplicationOptions Default => new();

    /// <summary>
    /// 高可靠性预设：更长 TTL、更短处理超时。
    /// </summary>
    /// <remarks>
    /// 仅覆盖主路径已消费字段；分布式失败语义不在本预设范围内。
    /// </remarks>
    public static DeduplicationOptions HighReliability => new()
    {
        CacheExpiration = TimeSpan.FromHours(72),
        ProcessingTimeout = TimeSpan.FromMinutes(5)
    };

    /// <summary>
    /// 高可用预设：标准 TTL、更长处理超时。
    /// </summary>
    public static DeduplicationOptions HighAvailability => new()
    {
        CacheExpiration = TimeSpan.FromHours(48),
        ProcessingTimeout = TimeSpan.FromMinutes(15)
    };
}
