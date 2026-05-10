// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.Entities;
using TaskManageDemo.Backend.Services.Feishu;
using TaskManageDemo.Backend.Services.Sync;

namespace TaskManageDemo.Backend.Services.Background;

/// <summary>
/// 定时任务服务实现
/// </summary>
public class ScheduledTaskService : IScheduledTaskService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly IFeishuNotificationService _notificationService;
    private readonly ITaskSyncService _syncService;
    private readonly ILogger<ScheduledTaskService> _logger;

    /// <summary>
    /// 初始化定时任务服务
    /// </summary>
    public ScheduledTaskService(
        TaskManageDbContext dbContext,
        IFeishuNotificationService notificationService,
        ITaskSyncService syncService,
        ILogger<ScheduledTaskService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _syncService = syncService;
        _logger = logger;
    }

    /// <summary>
    /// 发送任务截止提醒
    /// </summary>
    public async Task SendDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始执行任务截止提醒检查");

        var now = DateTime.UtcNow;
        var reminderThreshold = now.AddHours(24);

        var dueTasks = await _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .Where(t => !t.IsCompleted &&
                        t.DueTime.HasValue &&
                        t.DueTime.Value <= reminderThreshold &&
                        t.DueTime.Value > now)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("找到 {Count} 个即将截止的任务", dueTasks.Count);

        foreach (var task in dueTasks)
        {
            var assignees = task.Members
                .Where(m => m.Role == TaskMemberRoles.Assignee && m.User != null)
                .Select(m => m.User!.FeishuId)
                .ToList();

            if (assignees.Count == 0) continue;

            foreach (var assignee in assignees)
            {
                await _notificationService.SendTaskDueReminderAsync(
                    assignee,
                    task.Summary,
                    task.TaskGuid,
                    task.DueTime!.Value,
                    cancellationToken);
            }
        }

        _logger.LogInformation("任务截止提醒发送完成");
    }

    /// <summary>
    /// 执行全量同步
    /// </summary>
    public async Task PerformFullSyncAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始执行全量同步");

        try
        {
            var taskLists = await _dbContext.TaskLists.ToListAsync(cancellationToken);
            var totalSynced = 0;

            foreach (var taskList in taskLists)
            {
                var count = await _syncService.SyncTaskListTasksAsync(taskList.TaskListGuid, cancellationToken);
                totalSynced += count;
            }

            _logger.LogInformation("全量同步完成，共同步 {Count} 个任务", totalSynced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "全量同步失败");
        }
    }

    /// <summary>
    /// 重试失败的事件
    /// <para>注意：使用 IdempotentFeishuEventHandler 基类时，事件重试由框架内置的 IFeishuEventDeduplicator 处理</para>
    /// </summary>
    public async Task RetryFailedEventsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("事件重试由框架内置的幂等性处理器自动管理");
        await Task.CompletedTask;
    }
}
