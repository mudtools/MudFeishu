// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 空实现的 SeqID 去重器。
/// 当统一去重节 <c>FeishuDeduplication:Mode=None</c> 时使用：不进行 SeqID 去重，
/// 所有消息均视为新消息。
/// </summary>
public sealed class NoopFeishuSeqIdDeduplicator : IFeishuSeqIDDeduplicator
{
    /// <inheritdoc />
    public Task<bool> TryMarkAsProcessedAsync(ulong seqId) => Task.FromResult(false);

    /// <inheritdoc />
    public Task RollbackAsync(ulong seqId) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<bool> IsProcessedAsync(ulong seqId) => Task.FromResult(false);

    /// <inheritdoc />
    public Task ClearCacheAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public int GetCacheCount() => 0;

    /// <inheritdoc />
    public Task<int> GetCacheCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

    /// <inheritdoc />
    public ulong GetMaxProcessedSeqId() => 0UL;

    /// <inheritdoc />
    public Task<ulong> GetMaxProcessedSeqIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(0UL);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => new();
}