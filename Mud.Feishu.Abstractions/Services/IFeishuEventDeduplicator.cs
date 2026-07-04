// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 事件去重状态枚举
/// </summary>
public enum DeduplicationStatus
{
    /// <summary>
    /// 未处理
    /// </summary>
    Pending,

    /// <summary>
    /// 处理中
    /// </summary>
    Processing,

    /// <summary>
    /// 已处理
    /// </summary>
    Completed
}

/// <summary>
/// 飞书事件去重服务接口
/// 用于防止重复事件的处理，保证事件处理的幂等性
/// </summary>
public interface IFeishuEventDeduplicator : IAsyncDisposable
{
    /// <summary>
    /// 尝试将事件标记为处理中（异步版本，适用于分布式场景）
    /// </summary>
    /// <param name="eventId">事件唯一标识符</param>
    /// <param name="appKey">应用键（用于多应用场景，避免跨应用冲突）</param>
    /// <param name="ttl">过期时间（可选），不指定则使用默认值</param>
    /// <param name="processingTimeout">处理中超时时间（可选），超时后允许重新处理</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>
    /// 返回去重结果：
    /// - IsDuplicate=false: 成功标记为处理中，可以开始处理事件
    /// - IsDuplicate=true &amp; WasProcessing=false: 事件已完成，跳过
    /// - IsDuplicate=true &amp; WasProcessing=true: 事件正在处理中，跳过
    /// </returns>
    Task<DeduplicationResult> TryMarkAsProcessingAsync(string eventId, string? appKey = null, TimeSpan? ttl = null, TimeSpan? processingTimeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将处理中的事件标记为已完成（异步版本，适用于分布式场景）
    /// </summary>
    /// <param name="eventId">事件唯一标识符</param>
    /// <param name="appKey">应用键（用于多应用场景，避免跨应用冲突）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task MarkAsCompletedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚处理中的状态（异步版本，适用于分布式场景）
    /// </summary>
    /// <param name="eventId">事件唯一标识符</param>
    /// <param name="appKey">应用键（用于多应用场景，避免跨应用冲突）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RollbackProcessingAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查事件是否已被处理（异步版本，适用于分布式场景）
    /// </summary>
    /// <param name="eventId">事件唯一标识符</param>
    /// <param name="appKey">应用键（用于多应用场景，避免跨应用冲突）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果事件已被处理返回 true，否则返回 false</returns>
    Task<bool> IsProcessedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取事件的处理状态（异步版本，适用于分布式场景）
    /// </summary>
    /// <param name="eventId">事件唯一标识符</param>
    /// <param name="appKey">应用键（用于多应用场景，避免跨应用冲突）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>事件处理状态</returns>
    Task<DeduplicationStatus> GetStatusAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 手动清理过期条目
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的过期条目数量</returns>
    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}
