// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 基于 Hash Set 的 WebSocket 消息 SeqID 去重服务实现
/// <para>使用内存 HashSet 存储已处理的 SeqID，自动清理过期数据</para>
/// <para>适用于单实例场景，多实例部署建议使用 Redis 实现</para>
/// </summary>
public sealed class FeishuSeqIDDeduplicator : MemoryDeduplicator<ulong>, IFeishuSeqIDDeduplicator
{
    private ulong _maxProcessedSeqId = 0;
    private readonly object _maxIdLock = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public FeishuSeqIDDeduplicator(
        ILogger<FeishuSeqIDDeduplicator>? logger = null,
        TimeSpan? cacheExpiration = null,
        TimeSpan? cleanupInterval = null)
        : base(logger, cacheExpiration, cleanupInterval)
    {
        logger?.LogInformation("飞书 SeqID 去重服务初始化完成，缓存过期时间: {Expiration}, 清理间隔: {CleanupInterval}",
            cacheExpiration ?? TimeSpan.FromHours(24),
            cleanupInterval ?? TimeSpan.FromMinutes(5));
    }

    /// <inheritdoc />
    public Task<bool> TryMarkAsProcessedAsync(ulong seqId)
    {
        var result = base.TryMarkAsProcessed(seqId);

        if (result)
        {
            Logger?.LogDebug("SeqID {SeqId} 已处理过，跳过", seqId);
        }
        else
        {
            lock (_maxIdLock)
            {
                if (seqId > _maxProcessedSeqId)
                {
                    _maxProcessedSeqId = seqId;
                }
            }
            Logger?.LogDebug("SeqID {SeqId} 标记为已处理，当前最大 SeqID: {MaxSeqId}", seqId, _maxProcessedSeqId);
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<bool> IsProcessedAsync(ulong seqId)
    {
        return Task.FromResult(base.IsProcessed(seqId));
    }

    /// <inheritdoc />
    public Task ClearCacheAsync()
    {
        var count = Count;
        base.ClearCache();

        lock (_maxIdLock)
        {
            _maxProcessedSeqId = 0;
        }

        Logger?.LogInformation("清空了 {Count} 个 SeqID 缓存条目", count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public int GetCacheCount()
    {
        return Count;
    }

    /// <inheritdoc />
    public ulong GetMaxProcessedSeqId()
    {
        lock (_maxIdLock)
        {
            return _maxProcessedSeqId;
        }
    }
}
