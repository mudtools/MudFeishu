// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 空实现的事件去重器
/// 当去重模式设置为 None 时使用，所有事件均直接处理，不进行去重
/// </summary>
public class NoopFeishuEventDeduplicator : IFeishuEventDeduplicator
{
    private readonly ILogger _logger;

    /// <summary>
    /// 初始化 <see cref="NoopFeishuEventDeduplicator"/> 的新实例
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public NoopFeishuEventDeduplicator(ILogger logger) => _logger = logger;

    /// <inheritdoc />
    public Task<DeduplicationResult> TryMarkAsProcessingAsync(string eventId, string? appKey = null, TimeSpan? ttl = null, TimeSpan? processingTimeout = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("事件去重已禁用（Mode=None），事件 {EventId} 直接处理", eventId);
        return Task.FromResult(DeduplicationResult.Success(eventId));
    }

    /// <inheritdoc />
    public Task MarkAsCompletedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("事件去重已禁用（Mode=None），事件 {EventId} 标记完成被跳过", eventId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RollbackProcessingAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("事件去重已禁用（Mode=None），事件 {EventId} 回滚被跳过", eventId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> IsProcessedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        // 去重禁用，所有事件均视为未处理
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<DeduplicationStatus> GetStatusAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        // 去重禁用，所有事件均返回 Pending
        return Task.FromResult(DeduplicationStatus.Pending);
    }

    /// <inheritdoc />
    public Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        // 无缓存，无需清理
        return Task.FromResult(0);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // 无需释放资源
        return new ValueTask();
    }
}
