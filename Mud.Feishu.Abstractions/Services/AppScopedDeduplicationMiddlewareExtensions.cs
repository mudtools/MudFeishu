// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 带应用维度的统一去重调用扩展。
/// </summary>
/// <remarks>
/// 能力探测式调用：中间件实现 <see cref="IAppScopedUnifiedDeduplicationMiddleware"/> 时透传 <c>appKey</c>，
/// 否则回退到旧接口（无应用维度）。该形态保证旧实现二进制兼容，同时让多应用宿主无需替换中间件即可获得隔离。
/// </remarks>
public static class AppScopedDeduplicationMiddlewareExtensions
{
    /// <summary>执行统一去重检查，应用维度可用时透传 <paramref name="appKey"/>。</summary>
    public static Task<UnifiedDeduplicationResult> CheckAsync(
        this IUnifiedDeduplicationMiddleware middleware,
        string? eventId,
        ulong? seqId,
        string? appKey,
        CancellationToken cancellationToken = default)
        => middleware is IAppScopedUnifiedDeduplicationMiddleware appScoped
            ? appScoped.CheckAsync(eventId, seqId, appKey, cancellationToken)
            : middleware.CheckAsync(eventId, seqId, cancellationToken);

    /// <summary>标记处理完成，应用维度可用时透传 <paramref name="appKey"/>。</summary>
    public static Task MarkCompletedAsync(
        this IUnifiedDeduplicationMiddleware middleware,
        string? eventId,
        ulong? seqId,
        string? appKey,
        CancellationToken cancellationToken = default)
        => middleware is IAppScopedUnifiedDeduplicationMiddleware appScoped
            ? appScoped.MarkCompletedAsync(eventId, seqId, appKey, cancellationToken)
            : middleware.MarkCompletedAsync(eventId, seqId, cancellationToken);

    /// <summary>回滚处理状态，应用维度可用时透传 <paramref name="appKey"/>。</summary>
    public static Task RollbackAsync(
        this IUnifiedDeduplicationMiddleware middleware,
        string? eventId,
        ulong? seqId,
        string? appKey,
        CancellationToken cancellationToken = default)
        => middleware is IAppScopedUnifiedDeduplicationMiddleware appScoped
            ? appScoped.RollbackAsync(eventId, seqId, appKey, cancellationToken)
            : middleware.RollbackAsync(eventId, seqId, cancellationToken);
}
