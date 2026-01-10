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
    private readonly Dictionary<string, DateTimeOffset> _nonceCache;
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
        _nonceCache = new Dictionary<string, DateTimeOffset>();
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
            // 先清理已过期的 nonce
            CleanupExpiredNoncesLocked();

            // 检查 nonce 是否已存在且未过期
            if (_nonceCache.TryGetValue(nonce, out var createdAt))
            {
                // 检查是否已过期
                if (DateTimeOffset.UtcNow - createdAt <= _nonceTtl)
                {
                    _logger.LogWarning("Nonce {Nonce} 已使用过，拒绝重放攻击", nonce);
                    return true; // 已使用且未过期
                }
                // 已过期，删除并继续处理
                _nonceCache.Remove(nonce);
            }

            // 标记新 nonce（使用当前时间）
            _nonceCache[nonce] = DateTimeOffset.UtcNow;

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
            if (!_nonceCache.TryGetValue(nonce, out var createdAt))
                return false;

            // 检查是否已过期
            return (DateTimeOffset.UtcNow - createdAt) <= _nonceTtl;
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
    /// 使用 Dictionary&lt;string, DateTimeOffset&gt; 存储创建时间，精确清理过期条目
    /// </summary>
    private void CleanupExpiredNonces(object? state)
    {
        lock (_lock)
        {
            CleanupExpiredNoncesLocked();
        }
    }

    /// <summary>
    /// 清理过期的 nonce（内部方法，需要在锁内调用）
    /// </summary>
    private void CleanupExpiredNoncesLocked()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredKeys = _nonceCache
            .Where(kvp => (now - kvp.Value) > _nonceTtl)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _nonceCache.Remove(key);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug("清理了 {Count} 个过期的 Nonce 缓存条目", expiredKeys.Count);
        }

        // 记录当前缓存统计
        if (_nonceCache.Count > 1000)
        {
            _logger.LogInformation("Nonce 缓存当前有 {Count} 个条目，TTL: {Ttl}",
                _nonceCache.Count, _nonceTtl);
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
