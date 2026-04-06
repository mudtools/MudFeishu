// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 分布式去重操作结果
/// </summary>
public class DeduplicationResult
{
    /// <summary>
    /// 是否为重复事件（true 表示事件已处理过或正在处理中，应跳过）
    /// </summary>
    public bool IsDuplicate { get; set; }

    /// <summary>
    /// 事件之前是否处于处理中状态（true 表示之前标记为处理中但未完成）
    /// </summary>
    public bool WasProcessing { get; set; }

    /// <summary>
    /// 事件ID
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// 当前状态
    /// </summary>
    public DeduplicationStatus Status { get; set; }

    /// <summary>
    /// 创建成功标记的结果（新事件，已标记为处理中）
    /// </summary>
    public static DeduplicationResult Success(string eventId) => new()
    {
        IsDuplicate = false,
        WasProcessing = false,
        EventId = eventId,
        Status = DeduplicationStatus.Processing
    };

    /// <summary>
    /// 创建重复事件的结果
    /// </summary>
    public static DeduplicationResult Duplicate(string eventId, bool wasProcessing = false, DeduplicationStatus status = DeduplicationStatus.Completed) => new()
    {
        IsDuplicate = true,
        WasProcessing = wasProcessing,
        EventId = eventId,
        Status = status
    };

    /// <summary>
    /// 创建处理中超时后可重新处理的结果
    /// </summary>
    public static DeduplicationResult TimeoutRecoverable(string eventId) => new()
    {
        IsDuplicate = false,
        WasProcessing = true,
        EventId = eventId,
        Status = DeduplicationStatus.Processing
    };
}
