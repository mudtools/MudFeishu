// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Models.Entities;
using TaskManageDemo.Backend.Services.Feishu;
using TaskManageDemo.Backend.Services.History;
using TaskManageDemo.Backend.Services.Sync;
using TaskManageDemo.Backend.Services.Transaction;

namespace TaskManageDemo.Backend.Services;

/// <summary>
/// 任务服务实现
/// </summary>
public class TaskService : ITaskService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly IFeishuTaskService _feishuTaskService;
    private readonly ITaskSyncService _taskSyncService;
    private readonly IFeishuNotificationService _notificationService;
    private readonly ITransactionService _transactionService;
    private readonly ITaskHistoryService _taskHistoryService;
    private readonly ILogger<TaskService> _logger;

    /// <summary>
    /// 初始化任务服务
    /// </summary>
    public TaskService(
        TaskManageDbContext dbContext,
        IFeishuTaskService feishuTaskService,
        ITaskSyncService taskSyncService,
        IFeishuNotificationService notificationService,
        ITransactionService transactionService,
        ITaskHistoryService taskHistoryService,
        ILogger<TaskService> logger)
    {
        _dbContext = dbContext;
        _feishuTaskService = feishuTaskService;
        _taskSyncService = taskSyncService;
        _notificationService = notificationService;
        _transactionService = transactionService;
        _taskHistoryService = taskHistoryService;
        _logger = logger;
    }

    #region 查询任务

    /// <summary>
    /// 获取任务列表（分页）
    /// </summary>
    public async Task<PagedResponse<TaskDto>> GetTasksAsync(
        TaskQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .AsQueryable();

        // 应用搜索条件
        if (parameters.IsCompleted.HasValue)
        {
            query = query.Where(t => t.IsCompleted == parameters.IsCompleted.Value);
        }

        if (parameters.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == parameters.Priority.Value);
        }

        if (!string.IsNullOrEmpty(parameters.AssigneeId))
        {
            query = query.Where(t => t.Members.Any(m => m.User != null && m.User.FeishuId == parameters.AssigneeId && m.Role == TaskMemberRoles.Assignee));
        }

        if (!string.IsNullOrEmpty(parameters.Keyword))
        {
            // 清理输入防止SQL注入和LIKE通配符滥用
            var sanitizedKeyword = SanitizeSearchKeyword(parameters.Keyword);
            query = query.Where(t => t.Summary.Contains(sanitizedKeyword) ||
                                     (t.Description != null && t.Description.Contains(sanitizedKeyword)));
        }

        if (parameters.DueTimeFrom.HasValue)
        {
            query = query.Where(t => t.DueTime >= parameters.DueTimeFrom.Value);
        }

        if (parameters.DueTimeTo.HasValue)
        {
            query = query.Where(t => t.DueTime <= parameters.DueTimeTo.Value);
        }

        if (!string.IsNullOrEmpty(parameters.TaskListGuid))
        {
            query = query.Where(t => t.TaskListGuid == parameters.TaskListGuid);
        }

        // 排序
        query = parameters.SortBy?.ToLower() switch
        {
            "duedate" => parameters.IsDescending ? query.OrderByDescending(t => t.DueTime) : query.OrderBy(t => t.DueTime),
            "priority" => parameters.IsDescending ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
            "createdat" => parameters.IsDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            _ => parameters.IsDescending ? query.OrderByDescending(t => t.UpdatedAt) : query.OrderBy(t => t.UpdatedAt)
        };

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                TaskGuid = t.TaskGuid,
                Summary = t.Summary,
                Description = t.Description,
                Status = t.Status,
                IsCompleted = t.IsCompleted,
                Priority = t.Priority,
                StartTime = t.StartTime,
                DueTime = t.DueTime,
                CompletedTime = t.CompletedTime,
                CreatedAt = t.CreatedAt,
                CreatorId = t.CreatorId,
                TaskListGuid = t.TaskListGuid,
                Members = t.Members.Where(m => m.User != null).Select(m => new TaskMemberDto
                {
                    FeishuId = m.User!.FeishuId,
                    Name = m.User.Name,
                    AvatarUrl = m.User.AvatarUrl,
                    Role = m.Role
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<TaskDto>
        {
            Items = items,
            Total = total,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    /// <summary>
    /// 获取任务详情
    /// </summary>
    public async Task<TaskDto?> GetTaskByIdAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task == null)
        {
            return null;
        }

        return MapToTaskDto(task);
    }

    #endregion

    #region 任务操作

    /// <summary>
    /// 创建任务
    /// </summary>
    public async Task<TaskDto> CreateTaskAsync(
        CreateTaskRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        // 在飞书创建任务
        var taskGuid = await _feishuTaskService.CreateTaskAsync(
            request.Summary,
            request.Description,
            request.AssigneeIds,
            request.DueTime,
            cancellationToken);

        if (string.IsNullOrEmpty(taskGuid))
        {
            throw new InvalidOperationException("创建任务失败：飞书 API 返回空值");
        }

        _logger.LogInformation("飞书任务创建成功，TaskGuid: {TaskGuid}", taskGuid);

        // 使用事务同步到本地数据库
        var result = await _transactionService.ExecuteAsync(async () =>
        {
            var syncedTask = await _taskSyncService.SyncTaskAsync(taskGuid!, cancellationToken);
            if (syncedTask == null)
            {
                throw new InvalidOperationException("任务同步到本地数据库失败");
            }
            return syncedTask;
        }, cancellationToken);

        // 发送通知
        if (request.AssigneeIds != null && request.AssigneeIds.Count > 0)
        {
            foreach (var assigneeId in request.AssigneeIds)
            {
                try
                {
                    await _notificationService.SendTaskAssignedNotificationAsync(
                        assigneeId,
                        request.Summary,
                        taskGuid,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发送任务分配通知失败，AssigneeId: {AssigneeId}", assigneeId);
                }
            }
        }

        // 记录任务创建历史
        await _taskHistoryService.RecordTaskCreatedAsync(result.Id, userId, cancellationToken);

        return MapToTaskDto(result);
    }

    /// <summary>
    /// 更新任务
    /// </summary>
    public async Task<TaskDto?> UpdateTaskAsync(
        int taskId,
        UpdateTaskRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.Tasks.FindAsync(new object[] { taskId }, cancellationToken);
        if (task == null)
        {
            return null;
        }

        var oldSummary = task.Summary;
        var oldDescription = task.Description;
        var oldIsCompleted = task.IsCompleted;
        var oldDueTime = task.DueTime;

        var success = await _feishuTaskService.UpdateTaskAsync(
            task.TaskGuid,
            request.Summary,
            request.Description,
            request.IsCompleted,
            request.DueTime,
            cancellationToken);

        if (!success)
        {
            throw new InvalidOperationException("更新任务失败：飞书 API 返回失败");
        }

        _logger.LogInformation("飞书任务更新成功，TaskGuid: {TaskGuid}", task.TaskGuid);

        var result = await _transactionService.ExecuteAsync(async () =>
        {
            var syncedTask = await _taskSyncService.SyncTaskAsync(task.TaskGuid, cancellationToken);
            if (syncedTask == null)
            {
                throw new InvalidOperationException("任务同步到本地数据库失败");
            }
            return syncedTask;
        }, cancellationToken);

        if (request.Summary != null && request.Summary != oldSummary)
        {
            await _taskHistoryService.RecordTaskUpdatedAsync(result.Id, userId, "Summary", oldSummary, request.Summary, cancellationToken);
        }

        if (request.Description != null && request.Description != oldDescription)
        {
            await _taskHistoryService.RecordTaskUpdatedAsync(result.Id, userId, "Description", oldDescription, request.Description, cancellationToken);
        }

        if (request.IsCompleted.HasValue && request.IsCompleted != oldIsCompleted)
        {
            await _taskHistoryService.RecordTaskStatusChangedAsync(
                result.Id, 
                userId, 
                oldIsCompleted ? "completed" : "pending", 
                request.IsCompleted.Value ? "completed" : "pending", 
                cancellationToken);
        }

        if (request.DueTime != oldDueTime)
        {
            await _taskHistoryService.RecordTaskUpdatedAsync(
                result.Id, 
                userId, 
                "DueTime", 
                oldDueTime?.ToString("yyyy-MM-dd HH:mm:ss"), 
                request.DueTime?.ToString("yyyy-MM-dd HH:mm:ss"), 
                cancellationToken);
        }

        return MapToTaskDto(result);
    }

    /// <summary>
    /// 删除任务
    /// </summary>
    public async Task<bool> DeleteTaskAsync(
        int taskId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.Tasks.FindAsync(new object[] { taskId }, cancellationToken);
        if (task == null)
        {
            return false;
        }

        await _taskHistoryService.RecordTaskUpdatedAsync(
            taskId,
            userId,
            "TaskDeleted",
            task.Summary,
            null,
            cancellationToken);

        var success = await _feishuTaskService.DeleteTaskAsync(task.TaskGuid, cancellationToken);
        if (!success)
        {
            throw new InvalidOperationException("删除任务失败：飞书 API 返回失败");
        }

        _logger.LogInformation("飞书任务删除成功，TaskGuid: {TaskGuid}", task.TaskGuid);

        await _transactionService.ExecuteAsync(async () =>
        {
            _dbContext.Tasks.Remove(task);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return true;
    }

    /// <summary>
    /// 分配任务成员
    /// </summary>
    public async Task<bool> AssignTaskAsync(
        int taskId,
        AssignTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task == null)
        {
            return false;
        }

        // 在飞书添加成员
        var success = await _feishuTaskService.AddMembersAsync(
            task.TaskGuid,
            request.AssigneeIds,
            request.FollowerIds,
            cancellationToken);

        if (!success)
        {
            throw new InvalidOperationException("添加任务成员失败");
        }

        // 同步成员到本地数据库
        await SyncTaskMembersAsync(task.Id, request.AssigneeIds, TaskMemberRoles.Assignee, cancellationToken);
        await SyncTaskMembersAsync(task.Id, request.FollowerIds, TaskMemberRoles.Follower, cancellationToken);

        // 发送通知
        foreach (var assigneeId in request.AssigneeIds)
        {
            try
            {
                await _notificationService.SendTaskAssignedNotificationAsync(
                    assigneeId,
                    task.Summary,
                    task.TaskGuid,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送任务分配通知失败，AssigneeId: {AssigneeId}", assigneeId);
            }
        }

        return true;
    }

    /// <summary>
    /// 更新任务状态
    /// </summary>
    public async Task<bool> UpdateTaskStatusAsync(
        int taskId,
        bool isCompleted,
        CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.Tasks.FindAsync(new object[] { taskId }, cancellationToken);
        if (task == null)
        {
            return false;
        }

        // 在飞书更新任务状态
        var success = await _feishuTaskService.UpdateTaskAsync(
            task.TaskGuid,
            null,
            null,
            isCompleted,
            null,
            cancellationToken);

        if (!success)
        {
            throw new InvalidOperationException("更新任务状态失败");
        }

        // 同步到本地数据库
        await _taskSyncService.SyncTaskAsync(task.TaskGuid, cancellationToken);

        return true;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 清理搜索关键词，防止SQL注入和LIKE通配符滥用
    /// </summary>
    private static string SanitizeSearchKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return string.Empty;

        // 限制长度
        var trimmed = keyword.Trim();
        if (trimmed.Length > 100)
            trimmed = trimmed.Substring(0, 100);

        // 转义LIKE通配符
        return trimmed
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_")
            .Replace("[", "\\[");
    }

    /// <summary>
    /// 将 TaskSync 实体映射为 TaskDto
    /// </summary>
    private TaskDto MapToTaskDto(TaskSync task)
    {
        return new TaskDto
        {
            Id = task.Id,
            TaskGuid = task.TaskGuid,
            Summary = task.Summary,
            Description = task.Description,
            Status = task.Status,
            IsCompleted = task.IsCompleted,
            Priority = task.Priority,
            StartTime = task.StartTime,
            DueTime = task.DueTime,
            CompletedTime = task.CompletedTime,
            CreatedAt = task.CreatedAt,
            CreatorId = task.CreatorId,
            TaskListGuid = task.TaskListGuid,
            Members = task.Members.Where(m => m.User != null).Select(m => new TaskMemberDto
            {
                FeishuId = m.User!.FeishuId,
                Name = m.User.Name,
                AvatarUrl = m.User.AvatarUrl,
                Role = m.Role
            }).ToList()
        };
    }

    /// <summary>
    /// 同步任务成员到本地数据库
    /// </summary>
    private async Task SyncTaskMembersAsync(
        int taskId,
        List<string> feishuIds,
        string role,
        CancellationToken cancellationToken = default)
    {
        foreach (var feishuId in feishuIds)
        {
            // 获取或创建用户
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.FeishuId == feishuId, cancellationToken);
            if (user == null)
            {
                user = new User
                {
                    FeishuId = feishuId,
                    Name = feishuId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastSyncedAt = DateTime.UtcNow
                };
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // 检查成员是否已存在
            var existingMember = await _dbContext.TaskMembers
                .FirstOrDefaultAsync(m => m.TaskSyncId == taskId && m.UserId == user.Id && m.Role == role, cancellationToken);

            if (existingMember == null)
            {
                var member = new TaskMemberEntity
                {
                    TaskSyncId = taskId,
                    UserId = user.Id,
                    Role = role,
                    JoinedAt = DateTime.UtcNow
                };
                _dbContext.TaskMembers.Add(member);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    #endregion
}
