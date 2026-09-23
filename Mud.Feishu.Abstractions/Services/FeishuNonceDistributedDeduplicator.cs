// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 基于内存的分布式 Nonce 去重服务实现
/// 适用于单机部署或开发测试环境
/// 对于分布式部署，建议使用 Redis 等外部存储实现
/// </summary>
public sealed class FeishuNonceDistributedDeduplicator : MemoryDeduplicator<string>, IFeishuNonceDistributedDeduplicator
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public FeishuNonceDistributedDeduplicator(
        ILogger<FeishuNonceDistributedDeduplicator>? logger = null,
        TimeSpan? nonceTtl = null,
        TimeSpan? cleanupInterval = null)
        : base(logger,
            nonceTtl ?? TimeSpan.FromMinutes(5),
            cleanupInterval ?? TimeSpan.FromMinutes(1),
            processingTimeout: null,
            // R3-P2-1：内存 Nonce 去重此前**无容量上限**（maxCacheSize 默认 0）。
            // 非生产 + EnforceHeaderSignatureValidation=false 的组合下，每个请求都会写入一条
            // Nonce 记录而永不淘汰 → 无界内存增长。此处套用与其他内存去重一致的默认上限。
            maxCacheSize: Consts.DefaultMaxCacheSize)
    {
        logger?.LogInformation(
            "飞书分布式 Nonce 去重服务初始化完成，Nonce TTL: {Ttl}, 清理间隔: {CleanupInterval}, 最大缓存: {MaxCacheSize}",
            nonceTtl ?? TimeSpan.FromMinutes(5),
            cleanupInterval ?? TimeSpan.FromMinutes(1),
            Consts.DefaultMaxCacheSize);
    }

    /// <inheritdoc />
    public Task<bool> TryMarkAsUsedAsync(string nonce, string? appKey = null, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(nonce))
        {
            Logger?.LogWarning("Nonce 为空，跳过去重检查");
            return Task.FromResult(false);
        }

        var result = TryMarkAsProcessed(nonce, appKey);

        if (result)
        {
            Logger?.LogWarning("Nonce {Nonce} (AppKey: {AppKey}) 已使用过，拒绝重放攻击", LogSanitizer.Clean(nonce), appKey ?? "default");
        }
        else
        {
            Logger?.LogDebug("Nonce {Nonce} (AppKey: {AppKey}) 标记为已使用", LogSanitizer.Clean(nonce), appKey ?? "default");
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<bool> IsUsedAsync(string nonce, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(nonce))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(IsProcessed(nonce, appKey));
    }

    /// <inheritdoc />
    public Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CleanupExpired());
    }
}
