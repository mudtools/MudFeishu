// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Middleware;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services.Feishu;
using TaskManageDemo.Backend.Services.History;
using TaskManageDemo.Backend.Services.Sync;
using TaskManageDemo.Backend.Services.Transaction;

namespace TaskManageDemo.Backend.Controllers;

/// <summary>
/// 任务控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TasksController : BaseController
{
    private readonly TaskManageDbContext _dbContext;
    private readonly IFeishuTaskService _feishuTaskService;
    private readonly ITaskSyncService _taskSyncService;
    private readonly IFeishuNotificationService _notificationService;
    private readonly ITransactionService _transactionService;
    private readonly ITaskHistoryService _taskHistoryService;
    private readonly ILogger<TasksController> _logger;

    /// <summary>
    /// 初始化任务控制器
    /// </summary>
    public TasksController(
        TaskManageDbContext dbContext,
        IFeishuTaskService feishuTaskService,
        ITaskSyncService taskSyncService,
        IFeishuNotificationService notificationService,
        ITransactionService transactionService,
        ITaskHistoryService taskHistoryService,
        ILogger<TasksController> logger)
    {
        _dbContext = dbContext;
        _feishuTaskService = feishuTaskService;
        _taskSyncService = taskSyncService;
        _notificationService = notificationService;
        _transactionService = transactionService;
        _taskHistoryService = taskHistoryService;
        _logger = logger;
    }

    /// <summary>
    /// 获取任务列表
    /// </summary>
    [HttpGet]
    [RequirePermission("task:read")]
    public async Task<ActionResult<ApiResponse<PagedResponse<TaskDto>>>> GetTasks(
        [FromQuery] TaskQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .AsQueryable();

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
            query = query.Where(t => t.Members.Any(m => m.User != null && m.User.FeishuId == parameters.AssigneeId && m.Role == "assignee"));
        }

        if (!string.IsNullOrEmpty(parameters.Keyword))
        {
            query = query.Where(t => t.Summary.Contains(parameters.Keyword) ||
                                     (t.Description != null && t.Description.Contains(parameters.Keyword)));
        }

        if (parameters.DueTimeFrom.HasValue)
        {
            query = query.Where(t => t.DueTime >= parameters.DueTimeFrom.Value);
        }

        if (parameters.DueTimeTo.HasValue)
        {
            query = query.Where(t => t.DueTime <= parameters.DueTimeTo.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        query = parameters.SortBy?.ToLower() switch
        {
            "duedate" => parameters.IsDescending ? query.OrderByDescending(t => t.DueTime) : query.OrderBy(t => t.DueTime),
            "priority" => parameters.IsDescending ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
            "createdat" => parameters.IsDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            _ => parameters.IsDescending ? query.OrderByDescending(t => t.UpdatedAt) : query.OrderBy(t => t.UpdatedAt)
        };

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

        return Paged(items, total, parameters.Page, parameters.PageSize);
    }

    /// <summary>
    /// 获取任务详情
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("task:read")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> GetTask(int id, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task == null)
        {
            return NotFoundResult<TaskDto>("任务不存在");
        }

        var dto = new TaskDto
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

        return Success(dto);
    }

    /// <summary>
    /// 创建任务
    /// </summary>
    [HttpPost]
    [RequirePermission("task:create")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> CreateTask(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        string? taskGuid = null;

        try
        {
            // 在飞书创建任务
            taskGuid = await _feishuTaskService.CreateTaskAsync(
                request.Summary,
                request.Description,
                request.AssigneeIds,
                request.DueTime,
                cancellationToken);

            if (string.IsNullOrEmpty(taskGuid))
            {
                return Fail<TaskDto>("创建任务失败：飞书 API 返回空值");
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

            var dto = new TaskDto
            {
                Id = result.Id,
                TaskGuid = result.TaskGuid,
                Summary = result.Summary,
                Description = result.Description,
                Status = result.Status,
                IsCompleted = result.IsCompleted,
                Priority = result.Priority,
                StartTime = result.StartTime,
                DueTime = result.DueTime,
                CreatedAt = result.CreatedAt,
                CreatorId = result.CreatorId,
                TaskListGuid = result.TaskListGuid
            };

            // 记录任务创建历史
            var userId = GetCurrentUserId();
            await _taskHistoryService.RecordTaskCreatedAsync(result.Id, userId, cancellationToken);

            return Created(dto, "任务创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建任务失败，TaskGuid: {TaskGuid}", taskGuid);

            // 如果飞书任务已创建但本地同步失败，尝试清理飞书任务
            if (!string.IsNullOrEmpty(taskGuid))
            {
                try
                {
                    await _feishuTaskService.DeleteTaskAsync(taskGuid, cancellationToken);
                    _logger.LogWarning("已回滚删除飞书任务: {TaskGuid}", taskGuid);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogError(deleteEx, "回滚删除飞书任务失败，TaskGuid: {TaskGuid}，需要手动清理", taskGuid);
                }
            }

            return Fail<TaskDto>($"创建任务失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新任务
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("task:update")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateTask(
        int id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks.FindAsync([id], cancellationToken);
        if (task == null)
        {
            return NotFoundResult<TaskDto>("任务不存在");
        }

        try
        {
            // 在飞书更新任务
            var success = await _feishuTaskService.UpdateTaskAsync(
                task.TaskGuid,
                request.Summary,
                request.Description,
                request.IsCompleted,
                request.DueTime,
                cancellationToken);

            if (!success)
            {
                return Fail<TaskDto>("更新任务失败：飞书 API 返回失败");
            }

            _logger.LogInformation("飞书任务更新成功，TaskGuid: {TaskGuid}", task.TaskGuid);

            // 使用事务同步到本地数据库
            var result = await _transactionService.ExecuteAsync(async () =>
            {
                var syncedTask = await _taskSyncService.SyncTaskAsync(task.TaskGuid, cancellationToken);
                if (syncedTask == null)
                {
                    throw new InvalidOperationException("任务同步到本地数据库失败");
                }
                return syncedTask;
            }, cancellationToken);

            var dto = new TaskDto
            {
                Id = result.Id,
                TaskGuid = result.TaskGuid,
                Summary = result.Summary,
                Description = result.Description,
                Status = result.Status,
                IsCompleted = result.IsCompleted,
                Priority = result.Priority,
                StartTime = result.StartTime,
                DueTime = result.DueTime,
                CompletedTime = result.CompletedTime,
                CreatedAt = result.CreatedAt,
                CreatorId = result.CreatorId,
                TaskListGuid = result.TaskListGuid
            };

            return Updated(dto, "任务更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新任务失败，TaskId: {TaskId}, TaskGuid: {TaskGuid}", id, task.TaskGuid);
            return Fail<TaskDto>($"更新任务失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除任务
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("task:delete")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTask(int id, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks.FindAsync([id], cancellationToken);
        if (task == null)
        {
            return NotFoundResult<bool>("任务不存在");
        }

        try
        {
            // 在飞书删除任务
            var success = await _feishuTaskService.DeleteTaskAsync(task.TaskGuid, cancellationToken);
            if (!success)
            {
                return Fail<bool>("删除任务失败：飞书 API 返回失败");
            }

            _logger.LogInformation("飞书任务删除成功，TaskGuid: {TaskGuid}", task.TaskGuid);

            // 使用事务删除本地数据库记录
            await _transactionService.ExecuteAsync(async () =>
            {
                _dbContext.Tasks.Remove(task);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }, cancellationToken);

            return Deleted("任务删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除任务失败，TaskId: {TaskId}, TaskGuid: {TaskGuid}", id, task.TaskGuid);

            // 注意：飞书任务已删除，无法回滚，记录警告日志
            _logger.LogWarning("飞书任务已删除但本地数据库操作失败，TaskGuid: {TaskGuid}，需要手动清理", task.TaskGuid);

            return Fail<bool>($"删除任务失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 分配任务
    /// </summary>
    [HttpPost("{id}/assign")]
    [RequirePermission("task:update")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignTask(
        int id,
        [FromBody] AssignTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task == null)
        {
            return NotFoundResult<bool>("任务不存在");
        }

        var success = await _feishuTaskService.AddMembersAsync(
            task.TaskGuid,
            request.AssigneeIds,
            request.FollowerIds,
            cancellationToken);

        if (!success)
        {
            return Fail<bool>("分配任务失败");
        }

        foreach (var assigneeId in request.AssigneeIds)
        {
            await _notificationService.SendTaskAssignedNotificationAsync(
                assigneeId,
                task.Summary,
                task.TaskGuid,
                cancellationToken);
        }

        return Success(true, "任务分配成功");
    }

    /// <summary>
    /// 更新任务状态
    /// </summary>
    [HttpPut("{id}/status")]
    [RequirePermission("task:update")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateTaskStatus(
        int id,
        [FromBody] UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks.FindAsync([id], cancellationToken);
        if (task == null)
        {
            return NotFoundResult<bool>("任务不存在");
        }

        var success = await _feishuTaskService.UpdateTaskAsync(
            task.TaskGuid,
            null,
            null,
            request.IsCompleted,
            null,
            cancellationToken);

        if (!success)
        {
            return Fail<bool>("更新状态失败");
        }

        await _taskSyncService.SyncTaskAsync(task.TaskGuid, cancellationToken);

        return Success(true, "状态更新成功");
    }
}
