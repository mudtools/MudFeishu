// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Services.Feishu;

namespace TaskManageDemo.Backend.Services.Sync;

/// <summary>
/// 任务同步服务接口
/// </summary>
public interface ITaskSyncService
{
    /// <summary>
    /// 同步单个任务
    /// </summary>
    Task<TaskSync?> SyncTaskAsync(string taskGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步清单下的所有任务
    /// </summary>
    Task<int> SyncTaskListTasksAsync(string taskListGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存任务到本地数据库
    /// </summary>
    Task<TaskSync> SaveTaskAsync(TaskSync task, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录任务历史
    /// </summary>
    Task RecordHistoryAsync(
        int taskSyncId,
        string actionType,
        string? fieldName,
        string? oldValue,
        string? newValue,
        string? operatorId,
        CancellationToken cancellationToken = default);
}

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
        return 0;
    }

    /// <summary>
    /// 保存任务到本地数据库
    /// </summary>
    public async Task<TaskSync> SaveTaskAsync(TaskSync task, CancellationToken cancellationToken = default)
    {
        var existingTask = await _dbContext.Tasks
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.TaskGuid == task.TaskGuid, cancellationToken);

        if (existingTask == null)
        {
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;
            task.LastSyncedAt = DateTime.UtcNow;

            _dbContext.Tasks.Add(task);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("新增任务: {TaskGuid}", task.TaskGuid);
            return task;
        }

        existingTask.Summary = task.Summary;
        existingTask.Description = task.Description;
        existingTask.Status = task.Status;
        existingTask.IsCompleted = task.IsCompleted;
        existingTask.Priority = task.Priority;
        existingTask.StartTime = task.StartTime;
        existingTask.DueTime = task.DueTime;
        existingTask.CompletedTime = task.CompletedTime;
        existingTask.CreatorId = task.CreatorId;
        existingTask.TaskListGuid = task.TaskListGuid;
        existingTask.UpdatedAt = DateTime.UtcNow;
        existingTask.LastSyncedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("更新任务: {TaskGuid}", task.TaskGuid);
        return existingTask;
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
