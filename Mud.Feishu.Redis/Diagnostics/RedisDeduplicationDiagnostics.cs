// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Diagnostics;

/// <summary>
/// <see cref="IRedisDeduplicationDiagnostics"/> 的默认实现（R2-22）。
/// </summary>
/// <remarks>
/// 依赖解析策略：按**接口**取三个去重器（而非具体类型），再做实现类型嗅探——
/// 宿主若替换为非 Redis 实现（或未注册），对应 <c>*Available</c> 为 <c>false</c>，不抛异常。
/// 这使门面在"Redis 已装但去重器被替换"的宿主中也安全可用。
/// </remarks>
internal sealed class RedisDeduplicationDiagnostics : IRedisDeduplicationDiagnostics
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionMultiplexer _redis;

    /// <summary>
    /// 初始化诊断门面。
    /// </summary>
    /// <param name="serviceProvider">服务提供者（按接口解析去重器）</param>
    /// <param name="redis">Redis 连接复用器（读取服务端时间）</param>
    public RedisDeduplicationDiagnostics(IServiceProvider serviceProvider, IConnectionMultiplexer redis)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    }

    /// <inheritdoc />
    public async Task<RedisDeduplicationDiagnosticsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var serverTimeSeconds = await RedisStoreHelper
            .GetServerTimeSecondsAsync(_redis.GetDatabase(), cancellationToken)
            .ConfigureAwait(false);

        var eventAvailable = false;
        var eventCount = 0L;
        if (_serviceProvider.GetService<IFeishuEventDeduplicator>() is RedisFeishuEventDistributedDeduplicator eventDeduplicator)
        {
            eventAvailable = true;
            eventCount = await eventDeduplicator.GetCachedCountAsync().ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var nonceAvailable = false;
        var nonceCount = 0L;
        if (_serviceProvider.GetService<IFeishuNonceDistributedDeduplicator>() is RedisFeishuNonceDistributedDeduplicator nonceDeduplicator)
        {
            nonceAvailable = true;
            nonceCount = await nonceDeduplicator.GetCachedCountAsync().ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var seqIdAvailable = false;
        var seqIdCount = 0;
        var seqIdMax = 0UL;
        string? seqIdScopeKey = null;
        if (_serviceProvider.GetService<IFeishuSeqIDDeduplicator>() is RedisFeishuSeqIDDeduplicator seqIdDeduplicator)
        {
            seqIdAvailable = true;
            seqIdCount = seqIdDeduplicator.GetCacheCount();
            seqIdMax = seqIdDeduplicator.GetMaxProcessedSeqId();
            seqIdScopeKey = seqIdDeduplicator.ScopeKey;
        }

        return new RedisDeduplicationDiagnosticsSnapshot(
            serverTimeSeconds: serverTimeSeconds,
            eventDeduplicatorAvailable: eventAvailable,
            eventCachedCount: eventCount,
            nonceDeduplicatorAvailable: nonceAvailable,
            nonceCachedCount: nonceCount,
            seqIdDeduplicatorAvailable: seqIdAvailable,
            seqIdCacheCount: seqIdCount,
            seqIdMaxProcessed: seqIdMax,
            seqIdScopeKey: seqIdScopeKey);
    }
}
