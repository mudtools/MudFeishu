// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Models.DTOs;

namespace TaskManageDemo.Backend.Services.History;

/// <summary>
/// 任务历史服务接口
/// </summary>
public interface ITaskHistoryService
{
    /// <summary>
    /// 记录任务创建历史
    /// </summary>
    Task RecordTaskCreatedAsync(int taskId, string operatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录任务更新历史
    /// </summary>
    Task RecordTaskUpdatedAsync(int taskId, string operatorId, string fieldName, string? oldValue, string? newValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录任务状态变更历史
    /// </summary>
    Task RecordTaskStatusChangedAsync(int taskId, string operatorId, string? oldStatus, string? newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录任务完成历史
    /// </summary>
    Task RecordTaskCompletedAsync(int taskId, string operatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录任务分配历史
    /// </summary>
    Task RecordTaskAssignedAsync(int taskId, string operatorId, List<string> assigneeIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取任务历史记录
    /// </summary>
    Task<List<TaskHistoryDto>> GetTaskHistoryAsync(int taskId, CancellationToken cancellationToken = default);
}
