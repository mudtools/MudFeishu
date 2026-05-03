// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Configuration;

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 统一去重中间件实现
/// 提供多级去重检查，支持 EventId 和 SeqID 双重去重
/// </summary>
/// <remarks>
/// 此中间件实现了文档建议的双重去重策略：
/// 1. 首先检查 SeqID（如果提供）
/// 2. 然后检查 EventId（如果提供）
/// 3. 任一标识符触发去重则跳过处理
/// </remarks>
public class UnifiedDeduplicationMiddleware : IUnifiedDeduplicationMiddleware, IAsyncDisposable
{
    private readonly IFeishuEventDeduplicator? _eventDeduplicator;
    private readonly IFeishuSeqIDDeduplicator? _seqIdDeduplicator;
    private readonly DeduplicationOptions _options;
    private readonly ILogger<UnifiedDeduplicationMiddleware>? _logger;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventDeduplicator">事件去重器（可选）</param>
    /// <param name="seqIdDeduplicator">SeqID 去重器（可选）</param>
    /// <param name="options">去重配置选项</param>
    /// <param name="logger">日志记录器（可选）</param>
    public UnifiedDeduplicationMiddleware(
        IFeishuEventDeduplicator? eventDeduplicator = null,
        IFeishuSeqIDDeduplicator? seqIdDeduplicator = null,
        DeduplicationOptions? options = null,
        ILogger<UnifiedDeduplicationMiddleware>? logger = null)
    {
        _eventDeduplicator = eventDeduplicator;
        _seqIdDeduplicator = seqIdDeduplicator;
        _options = options ?? DeduplicationOptions.Default;
        _logger = logger;

        _logger?.LogInformation("统一去重中间件初始化完成，EventDeduplicator: {HasEventDedup}, SeqIdDeduplicator: {HasSeqIdDedup}",
            eventDeduplicator != null, seqIdDeduplicator != null);
    }

    /// <inheritdoc />
    public async Task<UnifiedDeduplicationResult> CheckAsync(string? eventId, ulong? seqId, CancellationToken cancellationToken = default)
    {
        if (_options.EnableVerboseLogging)
        {
            _logger?.LogDebug("执行统一去重检查: EventId={EventId}, SeqId={SeqId}", eventId, seqId);
        }

        if (seqId.HasValue && _seqIdDeduplicator != null)
        {
            var seqIdProcessed = await _seqIdDeduplicator.IsProcessedAsync(seqId.Value);
            if (seqIdProcessed)
            {
                _logger?.LogDebug("SeqID {SeqId} 已处理过，跳过", seqId.Value);
                return UnifiedDeduplicationResult.Skip(DeduplicationIdentifierType.SeqId, seqId.Value.ToString(), $"SeqID {seqId.Value} 已处理过");
            }
        }

        if (!string.IsNullOrEmpty(eventId) && _eventDeduplicator != null)
        {
            var eventResult = await _eventDeduplicator.TryMarkAsProcessingAsync(eventId!, cancellationToken: cancellationToken);
            if (eventResult.IsDuplicate)
            {
                _logger?.LogDebug("EventId {EventId} 已在处理中或已处理，跳过 (WasProcessing: {WasProcessing}, Status: {Status})",
                    eventId, eventResult.WasProcessing, eventResult.Status);
                return UnifiedDeduplicationResult.Skip(DeduplicationIdentifierType.EventId, eventId!, $"EventId {eventId} 已处理过");
            }
        }

        if (seqId.HasValue && _seqIdDeduplicator != null)
        {
            await _seqIdDeduplicator.TryMarkAsProcessedAsync(seqId.Value);
        }

        return UnifiedDeduplicationResult.Continue();
    }

    /// <inheritdoc />
    public async Task MarkCompletedAsync(string? eventId, ulong? seqId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(eventId) && _eventDeduplicator != null)
        {
            await _eventDeduplicator.MarkAsCompletedAsync(eventId!, cancellationToken: cancellationToken);
            _logger?.LogDebug("EventId {EventId} 标记为已完成", eventId);
        }

        if (_options.EnableVerboseLogging)
        {
            _logger?.LogDebug("去重标记完成: EventId={EventId}, SeqId={SeqId}", eventId, seqId);
        }
    }

    /// <inheritdoc />
    public async Task RollbackAsync(string? eventId, ulong? seqId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(eventId) && _eventDeduplicator != null)
        {
            await _eventDeduplicator.RollbackProcessingAsync(eventId!, cancellationToken: cancellationToken);
            _logger?.LogDebug("EventId {EventId} 处理状态已回滚", eventId);
        }

        if (_options.EnableVerboseLogging)
        {
            _logger?.LogDebug("去重状态回滚: EventId={EventId}, SeqId={SeqId}", eventId, seqId);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_eventDeduplicator != null)
        {
            await _eventDeduplicator.DisposeAsync();
        }

        if (_seqIdDeduplicator != null)
        {
            await _seqIdDeduplicator.DisposeAsync();
        }
    }
}
