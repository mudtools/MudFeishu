// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 事件去重配置
/// </summary>
public class EventDeduplicationOptions
{
    /// <summary>
    /// 默认缓存过期时间：48 小时
    /// </summary>
    public static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMilliseconds(Mud.Feishu.Abstractions.Consts.DefaultCacheExpirationMs);

    /// <summary>
    /// 默认缓存清理间隔：5 分钟
    /// </summary>
    public static readonly TimeSpan DefaultCleanupInterval = TimeSpan.FromMilliseconds(Mud.Feishu.Abstractions.Consts.DefaultCleanupIntervalMs);

    /// <summary>
    /// 去重模式，默认为内存去重
    /// </summary>
    public EventDeduplicationMode Mode { get; set; } = EventDeduplicationMode.InMemory;

    /// <summary>
    /// 缓存过期时间，默认为48小时
    /// <para>建议设置为与飞书官方事件重试窗口期一致，避免长延时场景下的重复处理</para>
    /// <para>最小值为 60 秒</para>
    /// </summary>
    public TimeSpan CacheExpiration
    {
        get => _cacheExpiration;
        set => _cacheExpiration = value < TimeSpan.FromSeconds(60) ? TimeSpan.FromSeconds(60) : value;
    }
    private TimeSpan _cacheExpiration = DefaultCacheExpiration;

    /// <summary>
    /// 缓存清理间隔，默认为5分钟
    /// <para>最小值为 60 秒</para>
    /// </summary>
    public TimeSpan CleanupInterval
    {
        get => _cleanupInterval;
        set => _cleanupInterval = value < TimeSpan.FromSeconds(60) ? TimeSpan.FromSeconds(60) : value;
    }
    private TimeSpan _cleanupInterval = DefaultCleanupInterval;

    /// <summary>
    /// 默认处理中超时时间：10 分钟
    /// </summary>
    public static readonly TimeSpan DefaultProcessingTimeout = TimeSpan.FromMilliseconds(Mud.Feishu.Abstractions.Consts.DefaultProcessingTimeoutMs);

    /// <summary>
    /// 处理中超时时间，超时后允许重新处理事件
    /// <para>默认为 10 分钟，最小值为 10 秒</para>
    /// </summary>
    public TimeSpan ProcessingTimeout
    {
        get => _processingTimeout;
        set => _processingTimeout = value < TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(10) : value;
    }
    private TimeSpan _processingTimeout = DefaultProcessingTimeout;

    /// <summary>
    /// 内存缓存最大条目数，0 表示不限制
    /// <para>默认为 100000，与 Abstractions 层 DeduplicationOptions.MaxCacheSize 对齐</para>
    /// </summary>
    public int MaxCacheSize
    {
        get => _maxCacheSize;
        set => _maxCacheSize = Math.Max(0, value);
    }
    private int _maxCacheSize = Mud.Feishu.Abstractions.Consts.DefaultMaxCacheSize;
}
