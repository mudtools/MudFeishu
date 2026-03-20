// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services.Feishu;
using TaskManageDemo.Backend.Services.Sync;

namespace TaskManageDemo.Backend.Controllers;

/// <summary>
/// 任务控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly TaskManageDbContext _dbContext;
    private readonly IFeishuTaskService _feishuTaskService;
    private readonly ITaskSyncService _taskSyncService;
    private readonly IFeishuNotificationService _notificationService;
    private readonly ILogger<TasksController> _logger;

    /// <summary>
    /// 初始化任务控制器
    /// </summary>
    public TasksController(
        TaskManageDbContext dbContext,
        IFeishuTaskService feishuTaskService,
        ITaskSyncService taskSyncService,
        IFeishuNotificationService notificationService,
        ILogger<TasksController> logger)
    {
        _dbContext = dbContext;
        _feishuTaskService = feishuTaskService;
        _taskSyncService = taskSyncService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// 获取任务列表
    /// </summary>
    [HttpGet]
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

        var response = new PagedResponse<TaskDto>
        {
            Items = items,
            Total = total,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };

        return ApiResponse<PagedResponse<TaskDto>>.Ok(response);
    }

    /// <summary>
    /// 获取任务详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> GetTask(int id, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task == null)
        {
            return ApiResponse<TaskDto>.Fail("任务不存在");
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

        return ApiResponse<TaskDto>.Ok(dto);
    }

    /// <summary>
    /// 创建任务
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TaskDto>>> CreateTask(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var taskGuid = await _feishuTaskService.CreateTaskAsync(
            request.Summary,
            request.Description,
            request.AssigneeIds,
            request.DueTime,
            cancellationToken);

        if (string.IsNullOrEmpty(taskGuid))
        {
            return ApiResponse<TaskDto>.Fail("创建任务失败");
        }

        var syncedTask = await _taskSyncService.SyncTaskAsync(taskGuid, cancellationToken);
        if (syncedTask == null)
        {
            return ApiResponse<TaskDto>.Fail("任务同步失败");
        }

        if (request.AssigneeIds != null && request.AssigneeIds.Count > 0)
        {
            foreach (var assigneeId in request.AssigneeIds)
            {
                await _notificationService.SendTaskAssignedNotificationAsync(
                    assigneeId,
                    request.Summary,
                    taskGuid,
                    cancellationToken);
            }
        }

        var dto = new TaskDto
        {
            Id = syncedTask.Id,
            TaskGuid = syncedTask.TaskGuid,
            Summary = syncedTask.Summary,
            Description = syncedTask.Description,
            Status = syncedTask.Status,
            IsCompleted = syncedTask.IsCompleted,
            Priority = syncedTask.Priority,
            StartTime = syncedTask.StartTime,
            DueTime = syncedTask.DueTime,
            CreatedAt = syncedTask.CreatedAt,
            CreatorId = syncedTask.CreatorId,
            TaskListGuid = syncedTask.TaskListGuid
        };

        return ApiResponse<TaskDto>.Ok(dto, "任务创建成功");
    }

    /// <summary>
    /// 更新任务
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateTask(
        int id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks.FindAsync([id], cancellationToken);
        if (task == null)
        {
            return ApiResponse<TaskDto>.Fail("任务不存在");
        }

        var success = await _feishuTaskService.UpdateTaskAsync(
            task.TaskGuid,
            request.Summary,
            request.Description,
            request.IsCompleted,
            request.DueTime,
            cancellationToken);

        if (!success)
        {
            return ApiResponse<TaskDto>.Fail("更新任务失败");
        }

        var syncedTask = await _taskSyncService.SyncTaskAsync(task.TaskGuid, cancellationToken);
        if (syncedTask == null)
        {
            return ApiResponse<TaskDto>.Fail("任务同步失败");
        }

        var dto = new TaskDto
        {
            Id = syncedTask.Id,
            TaskGuid = syncedTask.TaskGuid,
            Summary = syncedTask.Summary,
            Description = syncedTask.Description,
            Status = syncedTask.Status,
            IsCompleted = syncedTask.IsCompleted,
            Priority = syncedTask.Priority,
            StartTime = syncedTask.StartTime,
            DueTime = syncedTask.DueTime,
            CompletedTime = syncedTask.CompletedTime,
            CreatedAt = syncedTask.CreatedAt,
            CreatorId = syncedTask.CreatorId,
            TaskListGuid = syncedTask.TaskListGuid
        };

        return ApiResponse<TaskDto>.Ok(dto, "任务更新成功");
    }

    /// <summary>
    /// 删除任务
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTask(int id, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks.FindAsync([id], cancellationToken);
        if (task == null)
        {
            return ApiResponse<bool>.Fail("任务不存在");
        }

        var success = await _feishuTaskService.DeleteTaskAsync(task.TaskGuid, cancellationToken);
        if (!success)
        {
            return ApiResponse<bool>.Fail("删除任务失败");
        }

        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "任务删除成功");
    }

    /// <summary>
    /// 分配任务
    /// </summary>
    [HttpPost("{id}/assign")]
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
            return ApiResponse<bool>.Fail("任务不存在");
        }

        var success = await _feishuTaskService.AddMembersAsync(
            task.TaskGuid,
            request.AssigneeIds,
            request.FollowerIds,
            cancellationToken);

        if (!success)
        {
            return ApiResponse<bool>.Fail("分配任务失败");
        }

        foreach (var assigneeId in request.AssigneeIds)
        {
            await _notificationService.SendTaskAssignedNotificationAsync(
                assigneeId,
                task.Summary,
                task.TaskGuid,
                cancellationToken);
        }

        return ApiResponse<bool>.Ok(true, "任务分配成功");
    }

    /// <summary>
    /// 更新任务状态
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateTaskStatus(
        int id,
        [FromBody] UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks.FindAsync([id], cancellationToken);
        if (task == null)
        {
            return ApiResponse<bool>.Fail("任务不存在");
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
            return ApiResponse<bool>.Fail("更新状态失败");
        }

        await _taskSyncService.SyncTaskAsync(task.TaskGuid, cancellationToken);

        return ApiResponse<bool>.Ok(true, "状态更新成功");
    }
}
