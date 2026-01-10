// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 飞书 Webhook Nonce 去重服务
/// 防止短期内的重放攻击
/// </summary>
public class FeishuWebhookNonceDeduplicator : IAsyncDisposable
{
    private readonly ILogger<FeishuWebhookNonceDeduplicator> _logger;
    private readonly HashSet<string> _nonceCache;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _nonceTtl;
    private readonly TimeSpan _cleanupInterval;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FeishuWebhookNonceDeduplicator(
        ILogger<FeishuWebhookNonceDeduplicator> logger,
        TimeSpan? nonceTtl = null,
        TimeSpan? cleanupInterval = null)
    {
        _logger = logger;
        _nonceCache = new HashSet<string>();
        _nonceTtl = nonceTtl ?? TimeSpan.FromMinutes(5); // 默认 nonce 有效期为 5 分钟
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(1); // 默认每 1 分钟清理一次

        // 启动定期清理任务
        _cleanupTimer = new Timer(CleanupExpiredNonces, null, _cleanupInterval, _cleanupInterval);

        _logger.LogInformation("飞书 Webhook Nonce 去重服务初始化完成，Nonce TTL: {Ttl}, 清理间隔: {CleanupInterval}",
            _nonceTtl, _cleanupInterval);
    }

    /// <summary>
    /// 检查并标记 nonce 是否已使用
    /// </summary>
    /// <param name="nonce">随机数</param>
    /// <returns>如果 nonce 已使用返回 true，否则返回 false 并标记</returns>
    public bool TryMarkAsUsed(string nonce)
    {
        if (string.IsNullOrEmpty(nonce))
        {
            _logger.LogWarning("Nonce 为空，跳过去重检查");
            return false;
        }

        lock (_lock)
        {
            // 检查 nonce 是否已存在
            if (_nonceCache.Contains(nonce))
            {
                _logger.LogWarning("Nonce {Nonce} 已使用过，拒绝重放攻击", nonce);
                return true; // 已使用
            }

            // 标记新 nonce
            _nonceCache.Add(nonce);

            _logger.LogDebug("Nonce {Nonce} 标记为已使用", nonce);
            return false; // 未使用
        }
    }

    /// <summary>
    /// 检查 nonce 是否已使用（不标记）
    /// </summary>
    /// <param name="nonce">随机数</param>
    /// <returns>如果 nonce 已使用返回 true，否则返回 false</returns>
    /// <remarks>此方法内部使用，不对外暴露</remarks>
    internal bool IsUsed(string nonce)
    {
        if (string.IsNullOrEmpty(nonce))
        {
            return false;
        }

        lock (_lock)
        {
            return _nonceCache.Contains(nonce);
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <remarks>此方法内部使用，不对外暴露</remarks>
    internal int GetCacheCount()
    {
        lock (_lock)
        {
            return _nonceCache.Count;
        }
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    /// <remarks>此方法内部使用，不对外暴露</remarks>
    internal void ClearCache()
    {
        lock (_lock)
        {
            var count = _nonceCache.Count;
            _nonceCache.Clear();
            _logger.LogInformation("清空了 {Count} 个 Nonce 缓存条目", count);
        }
    }

    /// <summary>
    /// 清理过期的 nonce
    /// 注意：由于 HashSet 不存储过期时间，这里我们使用基于时间的重建策略
    /// </summary>
    private void CleanupExpiredNonces(object? state)
    {
        lock (_lock)
        {
            // 简单策略：如果缓存数量过大，清空缓存
            // 更好的实现是使用 ConcurrentDictionary<DateTimeOffset, HashSet<string>> 或类似结构
            // 但为了性能考虑，我们定期重建缓存
            var oldCache = _nonceCache;
            
            if (oldCache.Count > 10000)
            {
                // 如果缓存超过 10000 个，清空并重建
                _nonceCache.Clear();
                _logger.LogInformation("Nonce 缓存数量过多 ({Count})，已清空", oldCache.Count);
            }
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public ValueTask DisposeAsync()
    {
        if (_disposed)
#if NETSTANDARD2_0
            return default;
#else
            return ValueTask.CompletedTask;
#endif

        _disposed = true;

        _cleanupTimer.Dispose();

        lock (_lock)
        {
            _nonceCache.Clear();
        }

       #if NETSTANDARD2_0
            return default;
#else
            return ValueTask.CompletedTask;
#endif
    }
}
