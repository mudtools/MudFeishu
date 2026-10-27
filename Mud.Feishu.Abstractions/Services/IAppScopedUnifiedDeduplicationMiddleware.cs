// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 支持应用维度隔离的统一去重中间件（可选能力）。
/// </summary>
/// <remarks>
/// 事件去重器本身以 <c>appKey</c> 为键的一部分隔离多应用；本接口让统一去重路径也能透传该维度，
/// 避免多应用共用同一进程时不同应用的同名 EventId 互相判重。
/// 实现方必须保持与 <see cref="IUnifiedDeduplicationMiddleware"/> 相同的检查/完成/回滚语义，
/// 仅在去重器调用中透传 <c>appKey</c>；调用方应通过
/// <see cref="AppScopedDeduplicationMiddlewareExtensions"/> 调用以兼容仅实现旧接口的中间件。
/// </remarks>
public interface IAppScopedUnifiedDeduplicationMiddleware
{
    /// <summary>
    /// 执行统一去重检查。
    /// </summary>
    /// <param name="eventId">事件 ID（可选）</param>
    /// <param name="seqId">消息序列号（可选）</param>
    /// <param name="appKey">应用键（可选；为空时退化为跨应用共享命名空间）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>去重检查结果</returns>
    Task<UnifiedDeduplicationResult> CheckAsync(string? eventId, ulong? seqId, string? appKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记处理完成。
    /// </summary>
    /// <param name="eventId">事件 ID（可选）</param>
    /// <param name="seqId">消息序列号（可选）</param>
    /// <param name="appKey">应用键（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task MarkCompletedAsync(string? eventId, ulong? seqId, string? appKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚处理状态（处理失败时调用）。
    /// </summary>
    /// <param name="eventId">事件 ID（可选）</param>
    /// <param name="seqId">消息序列号（可选）</param>
    /// <param name="appKey">应用键（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RollbackAsync(string? eventId, ulong? seqId, string? appKey, CancellationToken cancellationToken = default);
}
