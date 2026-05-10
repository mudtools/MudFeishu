// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 统一去重检查结果
/// </summary>
public class UnifiedDeduplicationResult
{
    /// <summary>
    /// 是否应该跳过处理
    /// </summary>
    public bool ShouldSkip { get; set; }

    /// <summary>
    /// 触发去重的标识符类型
    /// </summary>
    public DeduplicationIdentifierType IdentifierType { get; set; }

    /// <summary>
    /// 触发去重的标识符值
    /// </summary>
    public string IdentifierValue { get; set; } = string.Empty;

    /// <summary>
    /// 去重原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 创建跳过处理的结果
    /// </summary>
    public static UnifiedDeduplicationResult Skip(DeduplicationIdentifierType type, string value, string reason) => new()
    {
        ShouldSkip = true,
        IdentifierType = type,
        IdentifierValue = value,
        Reason = reason
    };

    /// <summary>
    /// 创建继续处理的结果
    /// </summary>
    public static UnifiedDeduplicationResult Continue() => new()
    {
        ShouldSkip = false,
        IdentifierType = DeduplicationIdentifierType.None,
        IdentifierValue = string.Empty,
        Reason = string.Empty
    };
}

/// <summary>
/// 去重标识符类型
/// </summary>
public enum DeduplicationIdentifierType
{
    /// <summary>
    /// 无（未触发去重）
    /// </summary>
    None,

    /// <summary>
    /// EventId 去重
    /// </summary>
    EventId,

    /// <summary>
    /// SeqID 去重
    /// </summary>
    SeqId,

    /// <summary>
    /// 双重去重（同时匹配 EventId 和 SeqID）
    /// </summary>
    Both
}

/// <summary>
/// 统一去重中间件接口
/// 提供多级去重检查，支持 EventId 和 SeqID 双重去重
/// </summary>
public interface IUnifiedDeduplicationMiddleware
{
    /// <summary>
    /// 执行统一去重检查
    /// </summary>
    /// <param name="eventId">事件ID（可选）</param>
    /// <param name="seqId">消息序列号（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>去重检查结果</returns>
    Task<UnifiedDeduplicationResult> CheckAsync(string? eventId, ulong? seqId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记处理完成
    /// </summary>
    /// <param name="eventId">事件ID（可选）</param>
    /// <param name="seqId">消息序列号（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task MarkCompletedAsync(string? eventId, ulong? seqId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚处理状态（处理失败时调用）
    /// </summary>
    /// <param name="eventId">事件ID（可选）</param>
    /// <param name="seqId">消息序列号（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RollbackAsync(string? eventId, ulong? seqId, CancellationToken cancellationToken = default);
}
