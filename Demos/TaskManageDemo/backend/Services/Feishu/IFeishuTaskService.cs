// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Tasks;
using Mud.Feishu.DataModels.TasksList;
using TaskManageDemo.Backend.Data;

namespace TaskManageDemo.Backend.Services.Feishu;

/// <summary>
/// 飞书任务服务接口
/// </summary>
public interface IFeishuTaskService
{
    /// <summary>
    /// 创建任务
    /// </summary>
    Task<string?> CreateTaskAsync(
        string summary,
        string? description,
        List<string>? assignees,
        DateTime? dueTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新任务
    /// </summary>
    Task<bool> UpdateTaskAsync(
        string taskGuid,
        string? summary,
        string? description,
        bool? isCompleted,
        DateTime? dueTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取任务详情
    /// </summary>
    Task<TaskSync?> GetTaskByIdAsync(string taskGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除任务
    /// </summary>
    Task<bool> DeleteTaskAsync(string taskGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加任务成员
    /// </summary>
    Task<bool> AddMembersAsync(
        string taskGuid,
        List<string> assigneeIds,
        List<string> followerIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除任务成员
    /// </summary>
    Task<bool> RemoveMembersAsync(
        string taskGuid,
        List<string> memberIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建子任务
    /// </summary>
    Task<string?> CreateSubTaskAsync(
        string parentTaskGuid,
        string summary,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加任务提醒
    /// </summary>
    Task<bool> AddTaskReminderAsync(
        string taskGuid,
        int relativeFireMinute,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取清单下的所有任务
    /// </summary>
    Task<List<TaskSummary>> GetTaskListTasksAsync(
        string taskListGuid,
        bool? completed = null,
        CancellationToken cancellationToken = default);
}
