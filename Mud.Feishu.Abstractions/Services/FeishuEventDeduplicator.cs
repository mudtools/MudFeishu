// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Configuration;

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 飞书事件去重服务实现
/// 使用内存缓存 + 滑动窗口实现事件幂等性
/// </summary>
/// <remarks>
/// 此实现基于内存缓存，适用于单实例场景。
/// 对于分布式场景，建议使用基于 Redis 等外部存储的分布式去重实现。
/// </remarks>
public class FeishuEventDeduplicator : MemoryDeduplicator<string>, IFeishuEventDeduplicator, IDisposable
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="cacheExpiration">缓存过期时间</param>
    /// <param name="cleanupInterval">清理间隔时间</param>
    /// <param name="processingTimeout">处理中超时时间</param>
    /// <param name="maxCacheSize">最大缓存大小</param>
    public FeishuEventDeduplicator(
        ILogger<FeishuEventDeduplicator>? logger = null,
        TimeSpan? cacheExpiration = null,
        TimeSpan? cleanupInterval = null,
        TimeSpan? processingTimeout = null,
        int maxCacheSize = 100000)
        : base(logger, cacheExpiration, cleanupInterval, processingTimeout, maxCacheSize)
    {
        if (logger != null && logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("飞书事件去重服务初始化完成，缓存过期时间: {Expiration}, 清理间隔: {CleanupInterval}, 处理中超时: {ProcessingTimeout}",
                cacheExpiration ?? TimeSpan.FromHours(24), cleanupInterval ?? TimeSpan.FromMinutes(5), processingTimeout ?? TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// 使用统一配置构造
    /// </summary>
    /// <param name="options">去重配置选项</param>
    /// <param name="logger">日志记录器（可选）</param>
    public FeishuEventDeduplicator(DeduplicationOptions options, ILogger<FeishuEventDeduplicator>? logger = null)
        : base(logger, options.CacheExpiration, options.CleanupInterval, options.ProcessingTimeout, options.MaxCacheSize)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (logger != null && logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("飞书事件去重服务初始化完成（使用统一配置），缓存过期时间: {Expiration}, 清理间隔: {CleanupInterval}, 处理中超时: {ProcessingTimeout}",
                options.CacheExpiration, options.CleanupInterval, options.ProcessingTimeout);
    }

    /// <inheritdoc/>
    public override bool TryMarkAsProcessed(string key, string? appKey = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            Logger?.LogWarning("事件ID为空，跳过去重检查");
            return false;
        }
        return base.TryMarkAsProcessed(key, appKey);
    }

    /// <inheritdoc/>
    public override bool TryMarkAsProcessing(string key, string? appKey = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            Logger?.LogWarning("事件ID为空，跳过去重检查");
            return false;
        }
        return base.TryMarkAsProcessing(key, appKey);
    }

    /// <inheritdoc/>
    public override void MarkAsCompleted(string key, string? appKey = null)
    {
        if (string.IsNullOrEmpty(key))
            return;
        base.MarkAsCompleted(key, appKey);
    }

    /// <inheritdoc/>
    public override void RollbackProcessing(string key, string? appKey = null)
    {
        if (string.IsNullOrEmpty(key))
            return;
        base.RollbackProcessing(key, appKey);
    }

    /// <inheritdoc/>
    public override bool IsProcessed(string key, string? appKey = null)
    {
        if (string.IsNullOrEmpty(key))
            return false;
        return base.IsProcessed(key, appKey);
    }

    /// <inheritdoc/>
    public override DeduplicationStatus GetStatus(string key, string? appKey = null)
    {
        if (string.IsNullOrEmpty(key))
            return DeduplicationStatus.Pending;
        return base.GetStatus(key, appKey);
    }

    /// <inheritdoc/>
    public Task<DeduplicationResult> TryMarkAsProcessingAsync(string eventId, string? appKey = null, TimeSpan? ttl = null, TimeSpan? processingTimeout = null, CancellationToken cancellationToken = default)
    {
        var isDuplicate = TryMarkAsProcessing(eventId, appKey);
        var result = isDuplicate
            ? DeduplicationResult.Duplicate(eventId, false, GetStatus(eventId, appKey))
            : DeduplicationResult.Success(eventId);
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task MarkAsCompletedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        MarkAsCompleted(eventId, appKey);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RollbackProcessingAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        RollbackProcessing(eventId, appKey);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> IsProcessedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IsProcessed(eventId, appKey));
    }

    /// <inheritdoc/>
    public Task<DeduplicationStatus> GetStatusAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetStatus(eventId, appKey));
    }

    /// <inheritdoc/>
    public Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CleanupExpired());
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>总缓存数量和过期数量</returns>
    public (int TotalCached, int ExpiredCount) GetCacheStats()
    {
        var stats = base.GetCacheStats();
        return (stats.Total, stats.Expired);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
