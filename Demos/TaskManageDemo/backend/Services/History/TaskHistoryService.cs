// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Models.Entities;

namespace TaskManageDemo.Backend.Services.History;

/// <summary>
/// 任务历史服务实现
/// </summary>
public class TaskHistoryService : ITaskHistoryService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly ILogger<TaskHistoryService> _logger;

    public TaskHistoryService(
        TaskManageDbContext dbContext,
        ILogger<TaskHistoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task RecordTaskCreatedAsync(int taskId, string operatorId, CancellationToken cancellationToken = default)
    {
        var history = new TaskHistory
        {
            TaskSyncId = taskId,
            ActionType = "created",
            OperatorId = operatorId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TaskHistories.Add(history);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("记录任务创建历史，TaskId: {TaskId}, OperatorId: {OperatorId}", taskId, operatorId);
    }

    public async Task RecordTaskUpdatedAsync(int taskId, string operatorId, string fieldName, string? oldValue, string? newValue, CancellationToken cancellationToken = default)
    {
        var history = new TaskHistory
        {
            TaskSyncId = taskId,
            ActionType = "updated",
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            OperatorId = operatorId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TaskHistories.Add(history);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("记录任务更新历史，TaskId: {TaskId}, Field: {Field}, OperatorId: {OperatorId}", taskId, fieldName, operatorId);
    }

    public async Task RecordTaskStatusChangedAsync(int taskId, string operatorId, string? oldStatus, string? newStatus, CancellationToken cancellationToken = default)
    {
        var history = new TaskHistory
        {
            TaskSyncId = taskId,
            ActionType = "status_changed",
            FieldName = "Status",
            OldValue = oldStatus,
            NewValue = newStatus,
            OperatorId = operatorId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TaskHistories.Add(history);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("记录任务状态变更历史，TaskId: {TaskId}, OldStatus: {OldStatus}, NewStatus: {NewStatus}", taskId, oldStatus, newStatus);
    }

    public async Task RecordTaskCompletedAsync(int taskId, string operatorId, CancellationToken cancellationToken = default)
    {
        var history = new TaskHistory
        {
            TaskSyncId = taskId,
            ActionType = "completed",
            OperatorId = operatorId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TaskHistories.Add(history);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("记录任务完成历史，TaskId: {TaskId}, OperatorId: {OperatorId}", taskId, operatorId);
    }

    public async Task RecordTaskAssignedAsync(int taskId, string operatorId, List<string> assigneeIds, CancellationToken cancellationToken = default)
    {
        var history = new TaskHistory
        {
            TaskSyncId = taskId,
            ActionType = "assigned",
            FieldName = "Assignees",
            NewValue = string.Join(", ", assigneeIds),
            OperatorId = operatorId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TaskHistories.Add(history);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("记录任务分配历史，TaskId: {TaskId}, Assignees: {Assignees}, OperatorId: {OperatorId}", taskId, string.Join(", ", assigneeIds), operatorId);
    }

    public async Task<List<TaskHistoryDto>> GetTaskHistoryAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var histories = await _dbContext.TaskHistories
            .Include(h => h.Task)
            .Where(h => h.TaskSyncId == taskId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);

        // 获取用户名
        var userIds = histories
            .Where(h => !string.IsNullOrEmpty(h.OperatorId))
            .Select(h => h.OperatorId!)
            .Distinct()
            .ToList();

        var users = await _dbContext.Users
            .Where(u => userIds.Contains(u.FeishuId))
            .ToDictionaryAsync(u => u.FeishuId, u => u.Name, cancellationToken);

        return histories.Select(h => new TaskHistoryDto
        {
            Id = h.Id,
            TaskSyncId = h.TaskSyncId,
            ActionType = h.ActionType,
            FieldName = h.FieldName,
            OldValue = h.OldValue,
            NewValue = h.NewValue,
            UserId = null, // Task.CreatorId 是飞书ID，不是本地用户ID
            UserName = h.OperatorId != null && users.TryGetValue(h.OperatorId, out var name) ? name : "系统",
            CreatedAt = h.CreatedAt
        }).ToList();
    }
}
