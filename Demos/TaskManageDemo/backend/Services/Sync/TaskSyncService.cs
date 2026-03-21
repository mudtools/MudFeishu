// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.Entities;
using TaskManageDemo.Backend.Services.Feishu;

namespace TaskManageDemo.Backend.Services.Sync;

/// <summary>
/// 任务同步服务实现
/// </summary>
public class TaskSyncService : ITaskSyncService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly IFeishuTaskService _feishuTaskService;
    private readonly ILogger<TaskSyncService> _logger;

    /// <summary>
    /// 初始化任务同步服务
    /// </summary>
    public TaskSyncService(
        TaskManageDbContext dbContext,
        IFeishuTaskService feishuTaskService,
        ILogger<TaskSyncService> logger)
    {
        _dbContext = dbContext;
        _feishuTaskService = feishuTaskService;
        _logger = logger;
    }

    /// <summary>
    /// 同步单个任务
    /// </summary>
    public async Task<TaskSync?> SyncTaskAsync(string taskGuid, CancellationToken cancellationToken = default)
    {
        var taskData = await _feishuTaskService.GetTaskByIdAsync(taskGuid, cancellationToken);
        if (taskData == null)
        {
            _logger.LogWarning("无法获取飞书任务: {TaskGuid}", taskGuid);
            return null;
        }

        return await SaveTaskAsync(taskData, cancellationToken);
    }

    /// <summary>
    /// 同步清单下的所有任务
    /// </summary>
    public async Task<int> SyncTaskListTasksAsync(string taskListGuid, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始同步清单任务: {TaskListGuid}", taskListGuid);

        var taskSummaries = await _feishuTaskService.GetTaskListTasksAsync(taskListGuid, cancellationToken: cancellationToken);
        var syncCount = 0;

        foreach (var taskSummary in taskSummaries)
        {
            if (string.IsNullOrEmpty(taskSummary.Guid))
            {
                continue;
            }

            var taskData = await _feishuTaskService.GetTaskByIdAsync(taskSummary.Guid, cancellationToken);
            if (taskData != null)
            {
                await SaveTaskAsync(taskData, cancellationToken);
                syncCount++;
            }
        }

        _logger.LogInformation("清单任务同步完成: {TaskListGuid}, 同步任务数: {Count}", taskListGuid, syncCount);
        return syncCount;
    }

    /// <summary>
    /// 保存任务到本地数据库
    /// </summary>
    public async Task<TaskSync> SaveTaskAsync(TaskSync task, CancellationToken cancellationToken = default)
    {
        var existingTask = await _dbContext.Tasks
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.TaskGuid == task.TaskGuid, cancellationToken);

        if (existingTask != null)
        {
            existingTask.Summary = task.Summary;
            existingTask.Description = task.Description;
            existingTask.Status = task.Status;
            existingTask.IsCompleted = task.IsCompleted;
            existingTask.Priority = task.Priority;
            existingTask.DueTime = task.DueTime;
            existingTask.StartTime = task.StartTime;
            existingTask.CompletedTime = task.CompletedTime;
            existingTask.UpdatedAt = DateTime.UtcNow;
            existingTask.LastSyncedAt = DateTime.UtcNow;

            await SyncMembersAsync(existingTask, task.Members, cancellationToken);

            _dbContext.Tasks.Update(existingTask);
            _logger.LogDebug("更新任务: {TaskGuid}", task.TaskGuid);

            await RecordHistoryAsync(
                existingTask.Id,
                "UPDATE",
                null,
                null,
                null,
                null,
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return existingTask;
        }
        else
        {
            task.LastSyncedAt = DateTime.UtcNow;
            _dbContext.Tasks.Add(task);
            _logger.LogDebug("创建新任务: {TaskGuid}", task.TaskGuid);

            await _dbContext.SaveChangesAsync(cancellationToken);

            if (task.Members.Count > 0)
            {
                foreach (var member in task.Members)
                {
                    member.TaskSyncId = task.Id;
                }
                _dbContext.TaskMembers.AddRange(task.Members);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await RecordHistoryAsync(
                task.Id,
                "CREATE",
                null,
                null,
                null,
                null,
                cancellationToken);

            return task;
        }
    }

    /// <summary>
    /// 同步任务成员
    /// </summary>
    private async Task SyncMembersAsync(
        TaskSync existingTask,
        ICollection<TaskMemberEntity> newMembers,
        CancellationToken cancellationToken)
    {
        var existingMembers = existingTask.Members.ToList();

        var membersToRemove = existingMembers
            .Where(em => !newMembers.Any(nm => nm.FeishuUserId == em.FeishuUserId))
            .ToList();

        foreach (var member in membersToRemove)
        {
            _dbContext.TaskMembers.Remove(member);
        }

        foreach (var newMember in newMembers)
        {
            var existingMember = existingMembers
                .FirstOrDefault(em => em.FeishuUserId == newMember.FeishuUserId);

            if (existingMember != null)
            {
                existingMember.Role = newMember.Role;
            }
            else
            {
                newMember.TaskSyncId = existingTask.Id;
                _dbContext.TaskMembers.Add(newMember);
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 记录任务历史
    /// </summary>
    public async Task RecordHistoryAsync(
        int taskSyncId,
        string actionType,
        string? fieldName,
        string? oldValue,
        string? newValue,
        string? operatorId,
        CancellationToken cancellationToken = default)
    {
        var history = new TaskHistory
        {
            TaskSyncId = taskSyncId,
            ActionType = actionType,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            OperatorId = operatorId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TaskHistories.Add(history);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
