// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 基于 Hash Set 的 WebSocket 消息 SeqID 去重服务实现
/// <para>使用内存 HashSet 存储已处理的 SeqID，自动清理过期数据</para>
/// <para>适用于单实例场景，多实例部署建议使用 Redis 实现</para>
/// </summary>
public class FeishuSeqIDDeduplicator : IFeishuSeqIDDeduplicator, IAsyncDisposable
{
    private readonly ILogger<FeishuSeqIDDeduplicator>? _logger;
    private readonly HashSet<ulong> _processedSeqIds;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _cacheExpiration;
    private readonly TimeSpan _cleanupInterval;
    private readonly Dictionary<ulong, DateTimeOffset> _seqIdTimestamps; // 用于跟踪 SeqID 的时间戳
    private readonly object _lock = new();
    private bool _disposed;
    private ulong _maxProcessedSeqId = 0;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="cacheExpiration">缓存过期时间，默认 24 小时</param>
    /// <param name="cleanupInterval">清理间隔时间，默认 5 分钟</param>
    public FeishuSeqIDDeduplicator(
        ILogger<FeishuSeqIDDeduplicator>? logger = null,
        TimeSpan? cacheExpiration = null,
        TimeSpan? cleanupInterval = null)
    {
        _logger = logger;
        _processedSeqIds = new HashSet<ulong>();
        _seqIdTimestamps = new Dictionary<ulong, DateTimeOffset>();
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromHours(24);
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(5);

        // 启动定期清理任务
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, _cleanupInterval, _cleanupInterval);

        _logger?.LogInformation("飞书 SeqID 去重服务初始化完成，缓存过期时间: {Expiration}, 清理间隔: {CleanupInterval}",
            _cacheExpiration, _cleanupInterval);
    }

    /// <inheritdoc />
    public bool TryMarkAsProcessed(ulong seqId)
    {
        lock (_lock)
        {
            // 检查是否已存在
            if (_processedSeqIds.Contains(seqId))
            {
                _logger?.LogDebug("SeqID {SeqId} 已处理过，跳过", seqId);
                return true; // 已处理
            }

            // 记录新 SeqID
            _processedSeqIds.Add(seqId);
            _seqIdTimestamps[seqId] = DateTimeOffset.UtcNow;

            // 更新最大 SeqID
            if (seqId > _maxProcessedSeqId)
            {
                _maxProcessedSeqId = seqId;
            }

            _logger?.LogDebug("SeqID {SeqId} 标记为已处理，当前最大 SeqID: {MaxSeqId}", seqId, _maxProcessedSeqId);
            return false; // 未处理，新消息
        }
    }

    /// <inheritdoc />
    public bool IsProcessed(ulong seqId)
    {
        lock (_lock)
        {
            return _processedSeqIds.Contains(seqId);
        }
    }

    /// <inheritdoc />
    public Task<bool> IsProcessedAsync(ulong seqId)
    {
        lock (_lock)
        {
            return Task.FromResult(_processedSeqIds.Contains(seqId));
        }
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        lock (_lock)
        {
            var count = _processedSeqIds.Count;
            _processedSeqIds.Clear();
            _seqIdTimestamps.Clear();
            _maxProcessedSeqId = 0;
            _logger?.LogInformation("清空了 {Count} 个 SeqID 缓存条目", count);
        }
    }

    /// <inheritdoc />
    public int GetCacheCount()
    {
        lock (_lock)
        {
            return _processedSeqIds.Count;
        }
    }

    /// <inheritdoc />
    public ulong GetMaxProcessedSeqId()
    {
        lock (_lock)
        {
            return _maxProcessedSeqId;
        }
    }

    /// <summary>
    /// 清理过期条目
    /// </summary>
    private void CleanupExpiredEntries(object? state)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            var expiredKeys = _seqIdTimestamps
                .Where(kvp => (now - kvp.Value) > _cacheExpiration)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _processedSeqIds.Remove(key);
                _seqIdTimestamps.Remove(key);
            }

            if (expiredKeys.Count > 0)
            {
                _logger?.LogDebug("清理了 {Count} 个过期的 SeqID 缓存条目", expiredKeys.Count);

                // 如果清理后当前最大 SeqID 已过期，需要重新计算
                if (_seqIdTimestamps.TryGetValue(_maxProcessedSeqId, out var timestamp))
                {
                    if ((now - timestamp) > _cacheExpiration)
                    {
                        _maxProcessedSeqId = _seqIdTimestamps.Count > 0 ? _seqIdTimestamps.Keys.Max() : 0;
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cleanupTimer.Dispose();

        lock (_lock)
        {
            _processedSeqIds.Clear();
            _seqIdTimestamps.Clear();
        }

        await Task.CompletedTask;
    }
}
